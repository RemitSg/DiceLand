using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class BattleClashReadabilityPlayModeTests
{
    private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private GameObject root;
    private float originalTimeScale;

    [SetUp]
    public void SetUp()
    {
        originalTimeScale = Time.timeScale;
        Time.timeScale = 1;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        if (root != null)
            UnityEngine.Object.Destroy(root);
        yield return null;
        Time.timeScale = originalTimeScale;
    }

    [UnityTest]
    public IEnumerator RangeAndRevealedValueKeepTheirScreenSizeAcrossLiveCameraChanges()
    {
        root = new GameObject("Live dice readability test");
        Camera overview = Child("Overview").AddComponent<Camera>();
        Camera closeup = Child("Closeup").AddComponent<Camera>();
        Transform actor = Child("Actor").transform;
        actor.position = Vector3.forward * 10f;
        Component feedback = root.AddComponent(RuntimeType("BattleFeedbackSystem"));
        Invoke(feedback, "SetPresentationCamera", overview, 1f);
        Type rangeType = RuntimeType("EnemySkill_SO+DicePowerRange");
        object range = Activator.CreateInstance(rangeType);
        rangeType.GetField("minPower").SetValue(range, 2);
        rangeType.GetField("maxPower").SetValue(range, 19);
        object handle = Invoke(feedback, "ShowClashDiceRange", actor, false, range, null);
        yield return new WaitForSecondsRealtime(0.18f);
        RectTransform label = (RectTransform)Field(handle, "root");
        Canvas canvas = label.GetComponentInParent<Canvas>();
        float fraction = label.localScale.x * canvas.scaleFactor / Screen.height;
        Assert.That(fraction, Is.EqualTo(1f / 1080f).Within(0.00001f));
        Assert.That(TextProperty(handle, "rangeText", "text"), Is.EqualTo("2~19"));
        Assert.That(TextProperty(handle, "rangeText", "fontSize"), Is.EqualTo(64f));

        closeup.fieldOfView = 65f;
        Invoke(feedback, "SetPresentationCamera", closeup, 2.5f);
        yield return null;
        Assert.That(label.localScale.x * canvas.scaleFactor / Screen.height, Is.EqualTo(fraction).Within(0.00001f));
        Invoke(feedback, "RevealClashDiceValue", handle, 17, 2);
        yield return new WaitForSecondsRealtime(0.24f);
        Assert.That(TextProperty(handle, "valueText", "text"), Is.EqualTo("17"));
        Assert.That(TextProperty(handle, "valueText", "fontSize"), Is.EqualTo(96f));
        Assert.That(TextProperty(handle, "modifierText", "text"), Is.EqualTo("+2"));
        Assert.That(label.localScale.x * canvas.scaleFactor / Screen.height, Is.EqualTo(fraction).Within(0.00001f));
        closeup.fieldOfView = 18f;
        Invoke(feedback, "SetPresentationCamera", overview, 0.75f);
        yield return null;
        Assert.That(label.localScale.x * canvas.scaleFactor / Screen.height, Is.EqualTo(fraction).Within(0.00001f));
        Invoke(feedback, "HideClashDiceDisplay", handle);
        for (float deadline = Time.realtimeSinceStartup + 2f; label != null && Time.realtimeSinceStartup < deadline;)
            yield return null;
        Assert.IsTrue(label == null, "Completed dice UI must be released.");
    }

    [UnityTest]
    public IEnumerator CloseupImpactMovesTheCameraKicksTheLensAndReturnsWithoutDrift()
    {
        root = new GameObject("Live impact recovery test");
        Camera overview = Child("Overview").AddComponent<Camera>();
        Camera closeup = Child("Closeup").AddComponent<Camera>();
        closeup.transform.position = new Vector3(4f, 5f, -20f);
        closeup.fieldOfView = 36f;
        Vector3 originalCloseupPosition = closeup.transform.position;
        Transform first = Child("Ally").transform;
        Transform second = Child("Enemy").transform;
        first.position = Vector3.left;
        second.position = Vector3.right;
        Component presentation = root.AddComponent(RuntimeType("BattlePresentationSystem"));
        Set(presentation, "battleCamera", overview);
        Set(presentation, "closeupCamera", closeup);
        int shot = (int)Invoke(presentation, "StartCloseupShot", first, second, Vector3.zero, 12f);
        yield return new WaitForSecondsRealtime(0.05f);
        Vector3 stablePosition = closeup.transform.position;
        float stableLens = closeup.fieldOfView;
        Invoke(presentation, "TriggerCameraImpact", Vector3.zero, 12f, false, null);
        float displacement = 0f;
        float minimumLens = stableLens;
        for (float end = Time.realtimeSinceStartup + 0.35f; Time.realtimeSinceStartup < end;)
        {
            displacement = Mathf.Max(displacement, Vector3.Distance(stablePosition, closeup.transform.position));
            minimumLens = Mathf.Min(minimumLens, closeup.fieldOfView);
            yield return null;
        }
        Assert.Greater(displacement, 0.005f, "Impact must survive the camera's aim/follow calculation.");
        Assert.Less(minimumLens, stableLens - 0.01f, "Impact should visibly tighten the lens.");
        Assert.That(Vector3.Distance(stablePosition, closeup.transform.position), Is.LessThan(0.001f));
        Assert.That(closeup.fieldOfView, Is.EqualTo(stableLens).Within(0.001f));
        yield return (IEnumerator)Invoke(presentation, "EndCloseupShot", shot, 0f);
        Assert.IsTrue(overview.enabled);
        Assert.IsFalse(closeup.enabled);
        Assert.That(Vector3.Distance(originalCloseupPosition, closeup.transform.position), Is.LessThan(0.001f));
        Assert.That(closeup.fieldOfView, Is.EqualTo(36f).Within(0.001f));
    }

    [UnityTest]
    public IEnumerator LowPowerImpactsRemainVisibleAndHitsAreStrongerThanClashes()
    {
        root = new GameObject("Low-power impact screen displacement test");
        Component presentation = CreatePresentation();
        // Compare force at a steady envelope; oscillation and recovery are covered by the live camera test.
        Set(presentation, "cameraShakeFrequency", 0.01f);
        Set(presentation, "cameraShakeDecayCurve", AnimationCurve.Constant(0, 1, 1));
        Camera camera = (Camera)Field(presentation, "closeupCamera");
        Transform first = Child("Ally").transform;
        Transform second = Child("Enemy").transform;
        first.position = Vector3.left;
        second.position = Vector3.right;
        Invoke(presentation, "StartCloseupShot", first, second, Vector3.zero, 2f);
        yield return new WaitForSecondsRealtime(0.06f);
        float clashPeak = 0f, hitPeak = 0f;
        foreach (bool hit in new[] { false, true })
        {
            Vector3 stable = camera.WorldToViewportPoint(Vector3.zero);
            Invoke(presentation, "TriggerCameraImpact", Vector3.zero, 2f, hit, null);
            float peak = 0f;
            for (float end = Time.realtimeSinceStartup + 0.42f; Time.realtimeSinceStartup < end;)
            {
                Vector3 delta = camera.WorldToViewportPoint(Vector3.zero) - stable;
                peak = Mathf.Max(peak, new Vector2(delta.x * camera.aspect, delta.y).magnitude);
                yield return null;
            }
            Assert.That(peak, Is.GreaterThan(0.008f), "Even a small roll must move more than 0.8% of frame height.");
            Assert.That((camera.WorldToViewportPoint(Vector3.zero) - stable).magnitude, Is.LessThan(0.001f));
            if (hit) hitPeak = peak; else clashPeak = peak;
        }
        Assert.That(hitPeak, Is.GreaterThan(clashPeak * 1.12f));
        Invoke(presentation, "TriggerCameraImpact", Vector3.zero, 12f, true, null);
        Invoke(presentation, "TriggerCameraImpact", Vector3.zero, 1f, false, null);
        Assert.IsTrue((bool)Field(presentation, "closeupImpactIsHit"), "A weak clash must not replace a heavy hit's remaining beat.");
    }

    [UnityTest]
    public IEnumerator HitCameraAndContactBurstBeginWithTheDelayedDamageCallback()
    {
        root = new GameObject("Hit timing regression");
        Component presentation = CreatePresentation();
        Transform attacker = Child("Attacker").transform;
        Transform defender = Child("Defender").transform;
        attacker.position = Vector3.left;
        defender.position = Vector3.right;
        int shot = (int)Invoke(presentation, "StartCloseupShot", attacker, defender, Vector3.zero, 3f);
        Set(presentation, "defaultHitReactionDelay", 0.25f);
        bool resolved = false, impulseAtDamage = false;
        Type contactType = RuntimeType("BattleContactVfx");
        int burstsAtDamage = 0;
        Action damage = () =>
        {
            resolved = true;
            impulseAtDamage = (float)Field(presentation, "closeupShakeRemaining") > 0f
                && (bool)Field(presentation, "closeupImpactIsHit");
            burstsAtDamage = root.GetComponentsInChildren(contactType, true).Length;
        };
        object fx = Activator.CreateInstance(RuntimeType("EnemySkill_SO+DicePresentationSettings"));
        IEnumerator moment = (IEnumerator)Invoke(presentation, "PlayHitMoment", defender, Vector3.zero, Vector3.right,
            Color.white, 3f, fx, false, null, attacker.position, 0f, damage);
        ((MonoBehaviour)presentation).StartCoroutine(moment);
        Assert.IsFalse(resolved);
        Assert.AreEqual(0, root.GetComponentsInChildren(contactType, true).Length,
            "Contact sparks belong to the damage frame, after anticipation.");
        Assert.That((float)Field(presentation, "closeupShakeRemaining"), Is.Zero,
            "The beat must not be spent during anticipation before damage lands.");
        for (float end = Time.realtimeSinceStartup + 2f; !resolved && Time.realtimeSinceStartup < end;)
            yield return null;
        Assert.IsTrue(resolved);
        Assert.IsTrue(impulseAtDamage);
        Assert.AreEqual(1, burstsAtDamage, "One shared directional burst should land with damage.");
        Camera closeup = (Camera)Field(presentation, "closeupCamera");
        ((MonoBehaviour)presentation).StartCoroutine((IEnumerator)Invoke(presentation, "EndCloseupShot", shot, 0f));
        yield return null;
        Assert.IsTrue(closeup.enabled, "The shot should hold for the first landing after the burst.");
        for (float end = Time.realtimeSinceStartup + 2f; closeup.enabled && Time.realtimeSinceStartup < end;)
            yield return null;
        Assert.IsFalse(closeup.enabled, "The shot should release after the bulk of the debris has landed.");
    }

    [UnityTest]
    public IEnumerator SpectatorsAndNewWeaponsAreCulledWithoutDisablingCombatants()
    {
        root = new GameObject("Closeup spectator state test");
        Component feedback = root.AddComponent(RuntimeType("BattleFeedbackSystem"));
        Component presentation = CreatePresentation();
        Transform ally = Combatant("Participant ally", false);
        Transform enemy = Combatant("Participant enemy", true);
        Transform spectator = Combatant("Spectator", false);
        SpriteRenderer body = spectator.GetComponent<SpriteRenderer>();
        SpriteRenderer weapon = Visual("Oversized weapon", spectator);
        SpriteRenderer alreadyHidden = Visual("Previously culled visual", spectator);
        alreadyHidden.forceRenderingOff = true;
        SpriteRenderer disabled = Visual("Disabled visual", spectator);
        disabled.enabled = false;
        Collider2D collider = spectator.gameObject.AddComponent<BoxCollider2D>();
        Behaviour controller = spectator.GetComponent<MonoBehaviour>();
        bool controllerEnabled = controller.enabled;
        Invoke(feedback, "RegisterAlly", controller);
        Invoke(feedback, "RegisterAlly", ally.GetComponent(RuntimeType("BattleAllyController")));
        IDictionary bars = (IDictionary)Field(feedback, "allyUIs");
        RectTransform spectatorBar = (RectTransform)Field(bars[controller], "root");
        RectTransform participantBar = (RectTransform)Field(bars[ally.GetComponent(RuntimeType("BattleAllyController"))], "root");

        int shot = (int)Invoke(presentation, "StartCloseupShot", ally, enemy, Vector3.zero, 5f);
        Assert.IsTrue(body.forceRenderingOff);
        Assert.IsTrue(weapon.forceRenderingOff);
        Assert.IsFalse(ally.GetComponent<SpriteRenderer>().forceRenderingOff);
        Assert.IsFalse(enemy.GetComponent<SpriteRenderer>().forceRenderingOff);
        Assert.IsTrue(spectator.gameObject.activeInHierarchy);
        Assert.AreEqual(controllerEnabled, controller.enabled);
        Assert.IsTrue(collider.enabled);
        Assert.IsTrue(body.enabled);

        SpriteRenderer addedWeapon = Visual("Weapon added during shot", spectator);
        Transform summoned = Combatant("Summoned spectator", true);
        yield return null;
        yield return null;
        Assert.IsTrue(addedWeapon.forceRenderingOff);
        Assert.IsTrue(summoned.GetComponent<SpriteRenderer>().forceRenderingOff);
        Assert.IsFalse(spectatorBar.gameObject.activeSelf, "A hidden spectator must not leave a floating health bar.");
        Assert.IsTrue(participantBar.gameObject.activeSelf);
        addedWeapon.transform.SetParent(ally, false);
        body.enabled = false; // Death/other presentation changes must survive restoration.
        UnityEngine.Object.Destroy(weapon.gameObject);
        yield return null;
        yield return null;
        Assert.IsFalse(addedWeapon.forceRenderingOff, "A visual transferred to a participant must be released.");

        yield return (IEnumerator)Invoke(presentation, "EndCloseupShot", shot, 0f);
        yield return null;
        yield return null;
        Assert.IsFalse(body.forceRenderingOff);
        Assert.IsFalse(body.enabled);
        Assert.IsFalse(disabled.enabled);
        Assert.IsFalse(disabled.forceRenderingOff);
        Assert.IsTrue(alreadyHidden.forceRenderingOff);
        Assert.IsFalse(summoned.GetComponent<SpriteRenderer>().forceRenderingOff);
        Assert.IsTrue(collider.enabled);
        Assert.AreEqual(controllerEnabled, controller.enabled);
        Assert.IsTrue(spectatorBar.gameObject.activeSelf, "Overview UI must return with the spectator.");
    }

    [UnityTest]
    public IEnumerator ReplacingAndInterruptingShotsRestoresSpectatorsAndRejectsAnOldHold()
    {
        root = new GameObject("Closeup replacement test");
        Component presentation = CreatePresentation();
        Transform first = Combatant("First attacker", false);
        Transform second = Combatant("Defender", true);
        Transform replacement = Combatant("Replacement attacker", false);
        int oldShot = (int)Invoke(presentation, "StartCloseupShot", first, second, Vector3.zero, 5f);
        IEnumerator oldHold = (IEnumerator)Invoke(presentation, "EndCloseupShot", oldShot, 0.1f);
        Assert.IsTrue(oldHold.MoveNext());
        Invoke(presentation, "StartCloseupShot", replacement, second, Vector3.zero, 5f);
        Assert.IsFalse(replacement.GetComponent<SpriteRenderer>().forceRenderingOff);
        Assert.IsTrue(first.GetComponent<SpriteRenderer>().forceRenderingOff);
        yield return new WaitForSecondsRealtime(0.12f);
        Assert.IsFalse(oldHold.MoveNext());
        Assert.IsTrue(((Camera)Field(presentation, "closeupCamera")).enabled);
        Assert.IsTrue(first.GetComponent<SpriteRenderer>().forceRenderingOff);

        ((Behaviour)presentation).enabled = false;
        Assert.IsFalse(first.GetComponent<SpriteRenderer>().forceRenderingOff);
        Assert.IsFalse(replacement.GetComponent<SpriteRenderer>().forceRenderingOff);
        ((Behaviour)presentation).enabled = true;
        Invoke(presentation, "StartCloseupShot", first, second, Vector3.zero, 5f);
        Assert.IsTrue(replacement.GetComponent<SpriteRenderer>().forceRenderingOff);
        UnityEngine.Object.Destroy(presentation);
        yield return null;
        Assert.IsFalse(replacement.GetComponent<SpriteRenderer>().forceRenderingOff);
    }

    private Component CreatePresentation()
    {
        Component presentation = root.AddComponent(RuntimeType("BattlePresentationSystem"));
        Camera overview = Child("Overview").AddComponent<Camera>();
        overview.transform.position = Vector3.back * 20f;
        Set(presentation, "battleCamera", overview);
        Set(presentation, "closeupCamera", Child("Closeup").AddComponent<Camera>());
        return presentation;
    }

    private Transform Combatant(string name, bool enemy)
    {
        GameObject actor = Child(name);
        actor.AddComponent(RuntimeType(enemy ? "BattleEnemyController" : "BattleAllyController"));
        actor.AddComponent<SpriteRenderer>();
        return actor.transform;
    }

    private SpriteRenderer Visual(string name, Transform parent)
    {
        GameObject visual = Child(name);
        visual.transform.SetParent(parent, false);
        return visual.AddComponent<SpriteRenderer>();
    }

    private GameObject Child(string name)
    {
        GameObject item = new GameObject(name);
        item.transform.SetParent(root.transform, false);
        return item;
    }

    private static Type RuntimeType(string name) => Type.GetType(name + ", Assembly-CSharp", true);
    private static object Field(object target, string name) => target.GetType().GetField(name, Fields).GetValue(target);
    private static void Set(object target, string name, object value) => target.GetType().GetField(name, Fields).SetValue(target, value);
    private static object Invoke(object target, string name, params object[] args) => target.GetType().GetMethod(name, Fields).Invoke(target, args);
    private static object TextProperty(object handle, string field, string property)
    {
        object text = Field(handle, field);
        return text.GetType().GetProperty(property).GetValue(text, null);
    }
}
