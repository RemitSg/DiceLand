using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public sealed class BattleDeathCameraPlayModeTests
{
    private GameObject host, actor;
    private Component presentation, feedback;
    private Camera overview, closeup;
    private BattleDeathCameraFrameProbe probe;
    private Sprite sprite;
    private Texture2D texture;
    private float originalTime;
    private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private static Type TypeOf(string name) => AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name)).First(t => t != null);
    private static object Get(object target, string name) => target.GetType().GetProperty(name, Fields)?.GetValue(target) ?? target.GetType().GetField(name, Fields)?.GetValue(target);
    private static void Set(object target, string name, object value) => target.GetType().GetField(name, Fields).SetValue(target, value);
    private static object Call(object target, string name, params object[] args) => target.GetType().GetMethod(name, Fields).Invoke(target, args);
    private static Component Effect() => Object.FindObjectsOfType(TypeOf("BattleDeathVfx")).Cast<Component>().First();
    private Component Motion => host.GetComponent(TypeOf("BattleDeathCameraMotion"));

    [SetUp]
    public void SetUp()
    {
        originalTime = Time.timeScale; Time.timeScale = 1;
        host = new GameObject("Live death camera fixture");
        overview = new GameObject("Overview").AddComponent<Camera>(); overview.transform.SetParent(host.transform);
        overview.transform.position = new Vector3(0, 1, -12); overview.fieldOfView = 45;
        overview.gameObject.AddComponent<AudioListener>();
        probe = host.AddComponent<BattleDeathCameraFrameProbe>(); probe.observed = overview;
        closeup = new GameObject("Closeup").AddComponent<Camera>(); closeup.transform.SetParent(host.transform); closeup.enabled = false;
        actor = new GameObject("Defeated target"); actor.transform.SetParent(host.transform); actor.transform.position = new Vector3(2, 1, 0);
        texture = new Texture2D(8, 16); texture.SetPixels(Enumerable.Repeat(Color.white, 128).ToArray()); texture.Apply();
        sprite = Sprite.Create(texture, new Rect(0, 0, 8, 16), Vector2.one * 0.5f, 8); actor.AddComponent<SpriteRenderer>().sprite = sprite;
        feedback = host.AddComponent(TypeOf("BattleFeedbackSystem")); Set(feedback, "enableDeathSlowMotion", false);
        presentation = host.AddComponent(TypeOf("BattlePresentationSystem")); Set(presentation, "battleCamera", overview); Set(presentation, "closeupCamera", closeup);
    }

    private Component Kill()
    {
        Component controller = actor.AddComponent(TypeOf("BattleEnemyController"));
        Call(controller, "RegisterLastHitDirection", Vector3.right);
        Array allies = Array.CreateInstance(TypeOf("BattleAllyController"), 0);
        Array enemies = Array.CreateInstance(TypeOf("BattleEnemyController"), 1); enemies.SetValue(controller, 0);
        Call(feedback, "BindCombatants", allies, enemies); Call(controller, "KillInstantly"); return Effect();
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        Time.timeScale = originalTime; Object.Destroy(host); Object.Destroy(sprite); Object.Destroy(texture);
        foreach (Component effect in Object.FindObjectsOfType(TypeOf("BattleDeathVfx")).Cast<Component>()) Object.Destroy(effect.gameObject);
        yield return null;
    }

    [UnityTest]
    public IEnumerator RealDeathDrivesTheOverviewCameraAndPauseFreezesThePose()
    {
        Vector3 start = overview.transform.position; Component effect = Kill();
        Assert.NotNull(Motion); Assert.IsTrue((bool)Get(presentation, "IsPlayingDeathCamera"));
        yield return new WaitForSecondsRealtime(0.12f);
        Assert.Greater(Vector3.Distance(start, probe.position), 0.01f);
        Time.timeScale = 0; yield return null; yield return null;
        Vector3 paused = probe.position; float lens = probe.lens;
        yield return new WaitForSecondsRealtime(0.12f);
        Assert.Less(Vector3.Distance(paused, probe.position), 0.0001f); Assert.AreEqual(lens, probe.lens, 0.0001f);
        Time.timeScale = 1; Call(effect, "Advance", 3f); Call(Motion, "Advance", 0.5f); yield return null;
        Assert.IsFalse((bool)Get(presentation, "IsPlayingDeathCamera")); Assert.Less(Vector3.Distance(start, overview.transform.position), 0.0001f);
    }

    [UnityTest]
    public IEnumerator CloseupDoesNotCutAwayBeforeTheLastParticleAndCameraRecovery()
    {
        Transform attacker = new GameObject("Attacker").transform; attacker.SetParent(host.transform); attacker.position = Vector3.left;
        int shot = (int)Call(presentation, "StartCloseupShot", attacker, actor.transform, Vector3.zero, 3f);
        Component effect = Kill();
        IEnumerator end = (IEnumerator)Call(presentation, "EndCloseupShot", shot, 0f);
        ((MonoBehaviour)presentation).StartCoroutine(end); yield return null;
        Assert.IsTrue(closeup.enabled); Assert.IsTrue((bool)Get(presentation, "IsPlayingDeathCamera"));
        Call(effect, "Advance", 3f); Call(Motion, "Advance", 0.5f); yield return null; yield return null;
        Assert.IsFalse(closeup.enabled); Assert.IsTrue(overview.enabled);
    }

    [UnityTest]
    public IEnumerator SettlementWaitsForTheCompleteDeathShotAndCancelDoesNotReopenIt()
    {
        Component effect = Kill(); Component overlay = host.AddComponent(TypeOf("BattleSettlementOverlay"));
        Object theme = Resources.Load("TreasureUI/Theme");
        Call(overlay, "ConfigureFonts", Get(theme, "headingFont"), Get(theme, "bodyFont"));
        object data = Activator.CreateInstance(TypeOf("BattleSettlementData")); Set(data, "isVictory", false);
        Call(overlay, "ShowResult", data); yield return null;
        Assert.IsFalse((bool)Get(overlay, "IsVisible"));
        Call(effect, "Advance", 3f); Call(Motion, "Advance", 0.5f); yield return null; yield return null;
        Assert.IsTrue((bool)Get(overlay, "IsVisible"));
        Call(overlay, "HideResult"); yield return null; Assert.IsFalse((bool)Get(overlay, "IsVisible"));
    }
}
