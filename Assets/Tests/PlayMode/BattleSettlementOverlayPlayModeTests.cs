using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public sealed class BattleSettlementOverlayPlayModeTests
{
    private GameObject host;
    private Component overlay;
    private object data;
    private EventSystem events;
    private float previousTimeScale;
    private readonly List<ScriptableObject> owned = new List<ScriptableObject>();
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    [SetUp]
    public void SetUp()
    {
        previousTimeScale = Time.timeScale;
        Time.timeScale = 0;
        host = new GameObject("SettlementPlayTest");
        events = EventSystem.current != null ? EventSystem.current : host.AddComponent<EventSystem>();
        overlay = host.AddComponent(TypeOf("BattleSettlementOverlay"));
        Type database = TypeOf("UnityEditor.AssetDatabase");
        string path = (string)database.GetMethod("GUIDToAssetPath", new[] { typeof(string) }).Invoke(null, new object[] { "c6e7143cd2df45b45a784ef8c61a210f" });
        object font = database.GetMethod("LoadAssetAtPath", new[] { typeof(string), typeof(Type) }).Invoke(null, new object[] { path, TypeOf("TMPro.TMP_FontAsset") });
        Invoke("ConfigureFonts", font, font);
        data = New("BattleSettlementData"); Set(data, "isVictory", true); Set(data, "moneyGained", 23); Set(data, "experienceGained", 13);
        Set(data, "startingMoney", 150); Set(data, "hasMoneyBalance", true);
        for (int i = 0; i < 2; i++)
        {
            object entry = New("BattleSettlementEntry");
            Set(entry, "displayName", "Party " + i); Set(entry, "isAlive", true); Set(entry, "level", 1);
            Set(entry, "maxHP", 20); Set(entry, "currentHP", 20); Set(entry, "resultingMaxHP", 20); Set(entry, "resultingHP", 20);
            Set(entry, "maxExperience", 10); Set(entry, "experienceGained", 13); Set(entry, "resultingLevel", 2);
            Set(entry, "resultingExperience", 3); Set(entry, "resultingMaxExperience", 20);
            ((IList)Get(entry, "experienceRequirements")).Add(10); ((IList)Get(entry, "experienceRequirements")).Add(20);
            ((IList)Get(data, "partyEntries")).Add(entry);
        }
        for (int i = 0; i < 6; i++)
        {
            ScriptableObject item = ScriptableObject.CreateInstance(TypeOf("LootItem_SO")); owned.Add(item);
            Set(item, "lootName", "Loot " + i); Set(item, "description", "获得金币、经验值。查看完整战利品说明。");
            object entry = New("BattleSettlementLootEntry"); Set(entry, "lootItem", item); Set(entry, "displayName", "Loot " + i); Set(entry, "amount", 1);
            ((IList)Get(data, "lootEntries")).Add(entry);
        }
        Invoke("ShowResult", data);
    }

    [TearDown]
    public void TearDown()
    {
        Time.timeScale = previousTimeScale;
        Object.DestroyImmediate(host);
        foreach (ScriptableObject asset in owned) Object.DestroyImmediate(asset);
        owned.Clear();
    }

    [UnityTest]
    public IEnumerator PausedGameNaturallyFinishesRewardsWalletAndEveryCharacter()
    {
        Assert.AreEqual("金币 +0", Text("MoneyReward"));
        Assert.IsTrue((bool)Get(overlay, "IsPresenting"));
        float deadline = Time.realtimeSinceStartup + 8f;
        while ((bool)Get(overlay, "IsPresenting") && Time.realtimeSinceStartup < deadline)
        {
            // Editor stalls can skip a fixed observation time. Assert the phase invariant
            // whenever counting is observed, then wait for actual unscaled completion.
            if (Get(overlay, "PresentationPhase").ToString() == "Counting")
            {
                Assert.AreEqual(0, (int)Get(overlay, "ActiveExperienceFlights"));
            }
            yield return null;
        }
        Assert.AreEqual("Complete", Get(overlay, "PresentationPhase").ToString());
        Assert.AreEqual("金币 +23", Text("MoneyReward"));
        Assert.AreEqual("经验值 +13", Text("Exp"));
        Assert.AreEqual(173, Get(Get(overlay, "walletFeedback"), "DisplayedMoney"));
        Assert.IsFalse(Find("SettlementWallet").gameObject.activeSelf);
        foreach (object row in (IList)Get(overlay, "rowViews"))
        { Assert.AreEqual(2, Get(row, "shownLevel")); Assert.AreEqual(3, Get(row, "shownExperience")); }
    }

    [UnityTest]
    public IEnumerator NativeLootClickOpensActualDetailsAndFlightGraphicsDoNotInterceptButtons()
    {
        yield return new WaitForSecondsRealtime(1.15f);
        AssertHit(Find("ReturnButton"));
        foreach (Image image in ((RectTransform)Get(overlay, "experienceFlightRoot")).GetComponentsInChildren<Image>(true)) Assert.IsFalse(image.raycastTarget);
        yield return new WaitForSecondsRealtime(1.5f);
        Click(Find("Loot_Loot 0"));
        Assert.IsTrue(((RectTransform)Get(overlay, "lootDetailModalRoot")).gameObject.activeSelf);
        Assert.IsTrue(((RectTransform)Get(overlay, "lootDetailModalRoot")).GetComponentsInChildren<Component>()
            .Where(c => c.GetType().Name == "TextMeshProUGUI").Any(t => (string)Get(t, "text") == "Loot 0"));
        Invoke("HideLootDetailModal");
        AssertHit(Find("ReturnButton"));
    }

    [UnityTest]
    public IEnumerator EarlyReturnCompletesVisualsButStillRequiresTheRelicChoice()
    {
        object choice = New("BattleSettlementRelicChoiceData");
        ScriptableObject relic = ScriptableObject.CreateInstance(TypeOf("Relic_SO")); owned.Add(relic); Set(relic, "relicName", "Required relic");
        object option = New("BattleSettlementLootEntry"); Set(option, "displayName", "Required relic"); Set(option, "relicData", relic); Set(option, "amount", 1);
        ((IList)Get(choice, "choices")).Add(option); Set(data, "relicChoice", choice);
        Invoke("ShowResult", data);
        yield return null;
        Click(Find("ReturnButton"));
        Assert.AreEqual("Complete", Get(overlay, "PresentationPhase").ToString());
        Assert.IsTrue((bool)Get(overlay, "IsVisible"));
        Component sharedPanel = Get(overlay, "sharedRelicPanel") as Component;
        bool sharedVisible = sharedPanel != null && (bool)Get(sharedPanel, "IsVisible");
        bool legacyVisible = ((RectTransform)Get(overlay, "relicChoiceModalRoot")).gameObject.activeInHierarchy;
        Assert.IsTrue(sharedVisible || legacyVisible, "The required relic selection must remain visible.");
        Assert.IsFalse((bool)Get(choice, "rewardApplied"));
        Assert.AreEqual(0, (int)Get(overlay, "ActiveExperienceFlights"));
        Assert.AreEqual("金币 +23", Text("MoneyReward"));
    }

    [UnityTest]
    public IEnumerator HidingAndReopeningDropsStaleEffectsAndLeavesOneSetOfActiveCards()
    {
        yield return new WaitForSecondsRealtime(1.2f);
        Invoke("HideResult");
        Assert.IsFalse((bool)Get(overlay, "IsVisible"));
        Assert.IsFalse(((Component)Get(overlay, "walletFeedback")).gameObject.activeSelf);
        Invoke("ShowResult", data);
        yield return null;
        Assert.AreEqual(2, ((RectTransform)Get(overlay, "summaryRoot")).GetComponentsInChildren<Transform>().Count(t => t.name.StartsWith("SummaryRow_")));
        Assert.AreEqual(6, ((RectTransform)Get(overlay, "lootGridRoot")).GetComponentsInChildren<Button>().Length);
        Invoke("CompletePresentation");
        Assert.AreEqual(0, (int)Get(overlay, "ActiveExperienceFlights"));
        Assert.IsFalse(Find("SettlementWallet").gameObject.activeSelf);
    }

    private Transform Find(string name) => ((Canvas)Get(overlay, "overlayCanvas")).GetComponentsInChildren<Transform>(true).First(t => t.name == name);
    private string Text(string name) => (string)Get(Find(name).GetComponent(TypeOf("TMPro.TMP_Text")), "text");
    private object Invoke(string method, params object[] args) => overlay.GetType().GetMethod(method, Members).Invoke(overlay, args);
    private static object New(string name) => Activator.CreateInstance(TypeOf(name));
    private static Type TypeOf(string name) => AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name)).First(t => t != null);
    private static void Set(object target, string field, object value) => target.GetType().GetField(field, Members).SetValue(target, value);
    private static object Get(object target, string name) { FieldInfo field = target.GetType().GetField(name, Members); return field != null ? field.GetValue(target) : target.GetType().GetProperty(name, Members).GetValue(target); }
    private RaycastResult AssertHit(Transform target)
    {
        Canvas.ForceUpdateCanvases(); RectTransform rect = (RectTransform)target;
        Vector2 point = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
        List<RaycastResult> hits = new List<RaycastResult>(); events.RaycastAll(new PointerEventData(events) { position = point }, hits);
        Assert.IsNotEmpty(hits);
        Assert.IsTrue(hits[0].gameObject.transform == target || hits[0].gameObject.transform.IsChildOf(target), hits[0].gameObject.name);
        return hits[0];
    }
    private void Click(Transform target)
    {
        RaycastResult hit = AssertHit(target);
        ExecuteEvents.ExecuteHierarchy(hit.gameObject, new PointerEventData(events) { button = PointerEventData.InputButton.Left }, ExecuteEvents.pointerClickHandler);
    }
}
