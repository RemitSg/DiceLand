using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public sealed class BattleDeathVfxPlayModeTests
{
    private GameObject root, actor;
    private Texture2D texture;
    private Sprite sprite;
    private float originalTimeScale;
    private static Type RuntimeType(string name) => AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name)).First(t => t != null);
    private static Component[] Effects() => Object.FindObjectsOfType(RuntimeType("BattleDeathVfx")).Cast<Component>().ToArray();

    [SetUp]
    public void SetUp()
    {
        originalTimeScale = Time.timeScale; Time.timeScale = 1;
        root = new GameObject("Live death visual fixture"); root.SetActive(false);
        actor = new GameObject("Dying actor"); actor.transform.SetParent(root.transform);
        texture = new Texture2D(8, 16); texture.SetPixels(Enumerable.Repeat(Color.white, 128).ToArray()); texture.Apply();
        sprite = Sprite.Create(texture, new Rect(0, 0, 8, 16), Vector2.one * 0.5f, 8);
        actor.AddComponent<SpriteRenderer>().sprite = sprite; actor.AddComponent<BoxCollider>();
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        Time.timeScale = originalTimeScale;
        foreach (Component effect in Effects()) Object.Destroy(effect.gameObject);
        Object.Destroy(root); Object.Destroy(sprite); Object.Destroy(texture); yield return null;
    }

    private Component Bind(bool enemy)
    {
        Component controller = actor.AddComponent(RuntimeType(enemy ? "BattleEnemyController" : "BattleAllyController"));
        Component feedback = root.AddComponent(RuntimeType("BattleFeedbackSystem"));
        feedback.GetType().GetField("enableDeathSlowMotion", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(feedback, false);
        root.SetActive(true);
        Array allies = Array.CreateInstance(RuntimeType("BattleAllyController"), enemy ? 0 : 1);
        Array enemies = Array.CreateInstance(RuntimeType("BattleEnemyController"), enemy ? 1 : 0);
        (enemy ? enemies : allies).SetValue(controller, 0);
        feedback.GetType().GetMethod("BindCombatants").Invoke(feedback, new object[] { allies, enemies });
        return controller;
    }

    private static void Kill(Component controller) => controller.GetType().GetMethod("KillInstantly").Invoke(controller, null);
    private static IEnumerator UntilGone()
    {
        float deadline = Time.realtimeSinceStartup + 7;
        while (Effects().Length > 0) { Assert.Less(Time.realtimeSinceStartup, deadline, "Death VFX was not released"); yield return null; }
        yield return null;
    }

    [UnityTest]
    public IEnumerator FriendlyDeathUsesItsRealBindingAndReleasesAllNativeResources()
    {
        Component controller = Bind(false); Kill(controller); Kill(controller);
        Assert.AreEqual(1, Effects().Length); Component effect = Effects()[0];
        Assert.IsFalse(actor.GetComponent<SpriteRenderer>().enabled); Assert.IsFalse(actor.GetComponent<BoxCollider>().enabled);
        Assert.IsTrue((bool)controller.GetType().GetProperty("IsDead").GetValue(controller));
        Mesh mesh = effect.GetComponent<MeshFilter>().sharedMesh;
        Material[] materials = effect.GetComponentsInChildren<Renderer>().Select(r => r.sharedMaterial).ToArray();
        yield return new WaitForSecondsRealtime(0.3f);
        Assert.Greater(mesh.triangles.Length, 0);
        yield return UntilGone();
        Assert.IsTrue(mesh == null); Assert.IsTrue(materials.All(m => m == null));
    }

    [UnityTest]
    public IEnumerator EnemyDeathPausesWithTheBattleAndResumesToCompletion()
    {
        Component controller = Bind(true); Time.timeScale = 0; Kill(controller);
        Component effect = Effects().Single();
        yield return new WaitForSecondsRealtime(0.2f);
        Assert.AreEqual(0f, (float)effect.GetType().GetProperty("Elapsed").GetValue(effect));
        Assert.AreEqual(0, Time.timeScale); Assert.IsFalse(actor.GetComponent<SpriteRenderer>().enabled);
        Time.timeScale = 1; yield return UntilGone(); Assert.IsTrue(effect == null);
    }

    [UnityTest]
    public IEnumerator LethalClashClearsBothRollsOnTheDeathFrame()
    {
        yield return CheckDiceClearOnEnemyDeath(clash: true);
    }

    [UnityTest]
    public IEnumerator LethalSingleAttackClearsTheSurvivingAttackersRoll()
    {
        yield return CheckDiceClearOnEnemyDeath(clash: false);
    }

    private IEnumerator CheckDiceClearOnEnemyDeath(bool clash)
    {
        Component enemy = Bind(true);
        Component feedback = root.GetComponent(RuntimeType("BattleFeedbackSystem"));
        Camera camera = new GameObject("Dice camera").AddComponent<Camera>();
        camera.transform.SetParent(root.transform, false);
        camera.transform.position = Vector3.back * 10f;
        feedback.GetType().GetMethod("SetPresentationCamera").Invoke(feedback, new object[] { camera, 1f });
        Transform attacker = new GameObject("Surviving attacker").transform;
        attacker.SetParent(root.transform, false);
        attacker.position = Vector3.left;

        RectTransform attackerRoll = ShowRolledDice(feedback, attacker, false);
        RectTransform defenderRoll = clash ? ShowRolledDice(feedback, actor.transform, true) : null;
        yield return new WaitForSeconds(0.22f);
        Assert.IsTrue(attackerRoll.gameObject.activeInHierarchy);
        if (clash) Assert.IsTrue(defenderRoll.gameObject.activeInHierarchy);

        feedback.GetType().GetMethod("ShowDamageResultPopup").Invoke(feedback, new object[] { actor.transform.position, 3, true, 75, 0 });
        Transform damage = attackerRoll.parent.Find("DamagePopup");
        Assert.IsNotNull(damage);
        Time.timeScale = 0f;
        Kill(enemy);

        Assert.IsFalse(attackerRoll.gameObject.activeSelf);
        if (clash) Assert.IsFalse(defenderRoll.gameObject.activeSelf);
        Assert.IsTrue(damage.gameObject.activeInHierarchy, "The hit's own damage feedback should stay visible.");
        Assert.AreEqual(1, Effects().Length, "The death effect should play independently of the dice labels.");
        yield return null;
        Assert.IsTrue(attackerRoll == null);
        Assert.IsTrue(defenderRoll == null);
    }

    private static RectTransform ShowRolledDice(Component feedback, Transform owner, bool enemy)
    {
        Type rangeType = RuntimeType("EnemySkill_SO+DicePowerRange");
        object range = Activator.CreateInstance(rangeType);
        rangeType.GetField("minPower").SetValue(range, 1);
        rangeType.GetField("maxPower").SetValue(range, 4);
        object handle = feedback.GetType().GetMethod("ShowClashDiceRange").Invoke(feedback, new[] { (object)owner, enemy, range, null });
        feedback.GetType().GetMethod("RevealClashDiceValue").Invoke(feedback, new[] { handle, 3, 1 });
        return (RectTransform)handle.GetType().GetField("root", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(handle);
    }

    [UnityTest]
    public IEnumerator RemovingTheDeadActorDoesNotCancelItsIndependentDissolution()
    {
        Component controller = Bind(false); Kill(controller); Component effect = Effects().Single();
        Object.Destroy(actor); yield return null;
        Assert.IsNotNull(effect); Assert.IsNotNull(effect.GetComponentInChildren<SpriteRenderer>());
        yield return UntilGone(); Assert.IsTrue(effect == null);
    }

    [UnityTest]
    public IEnumerator FatalDamageAlreadyHasTheCurrentHitDirectionBeforeTheReactionCoroutineRuns()
    {
        Component controller = Bind(true);
        controller.GetType().GetMethod("RegisterLastHitDirection").Invoke(controller, new object[] { Vector3.left });
        Component presentation = root.AddComponent(RuntimeType("BattlePresentationSystem"));
        presentation.GetType().GetField("defaultHitReactionDelay", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(presentation, 0.05f);
        object fx = Activator.CreateInstance(RuntimeType("EnemySkill_SO+DicePresentationSettings"));
        bool resolved = false;
        Vector3 atDamage = Vector3.zero;
        Action fatalDamage = () =>
        {
            atDamage = (Vector3)controller.GetType().GetProperty("LastHitDirection").GetValue(controller);
            Kill(controller); resolved = true;
        };
        IEnumerator moment = (IEnumerator)presentation.GetType().GetMethod("PlayHitMoment", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(presentation, new object[] { actor.transform, Vector3.zero, Vector3.right, Color.white, 3f, fx, false, null, Vector3.left * 3, 0f, fatalDamage });
        ((MonoBehaviour)presentation).StartCoroutine(moment);
        float deadline = Time.realtimeSinceStartup + 3;
        while (!resolved) { Assert.Less(Time.realtimeSinceStartup, deadline); yield return null; }
        Assert.Greater(atDamage.x, 0.99f);
        Component effect = Effects().Single();
        Assert.Greater(((Vector3)effect.GetType().GetProperty("TravelDirection").GetValue(effect)).x, 0.99f);
        yield return UntilGone();
    }
}
