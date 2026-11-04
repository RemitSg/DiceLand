using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public sealed class BattleRelicPanelReuseTests
{
    private GameObject host;
    private BattleSettlementOverlay battle;
    private BattleSettlementData settlement;
    private BattleSettlementRelicChoiceData choice;
    private readonly List<Relic_SO> relics = new List<Relic_SO>();
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    [SetUp]
    public void SetUp()
    {
        for (int i = 0; i < 4; i++)
        {
            Relic_SO relic = ScriptableObject.CreateInstance<Relic_SO>(); relic.relicName = "遗物 " + i; relic.rarity = LootRarity.Epic; relics.Add(relic);
        }
        choice = BattleSettlementRelicChoiceBuilder.Build(relics.Take(2).ToArray());
        choice.refreshPool = new List<Relic_SO>(relics); choice.refreshesRemaining = 1; choice.choiceCount = 2;
        settlement = new BattleSettlementData { isVictory = true, relicChoice = choice };
        host = new GameObject("BattleRelicReuseTest"); battle = host.AddComponent<BattleSettlementOverlay>();
        MapTreasureArt theme = MapTreasureArt.Load(); battle.ConfigureFonts(theme.headingFont, theme.bodyFont);
        battle.ShowResult(settlement); battle.AdvancePresentation(4);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(host);
        foreach (Relic_SO relic in relics) Object.DestroyImmediate(relic);
        relics.Clear();
    }

    private MapTreasureOverlay Open()
    {
        typeof(BattleSettlementOverlay).GetMethod("OpenRelicChoiceModal", Private).Invoke(battle, new object[] { choice });
        return host.GetComponentInChildren<MapTreasureOverlay>(true);
    }
    private static Transform Find(Component panel, string name) => panel.GetComponentsInChildren<Transform>(true).First(t => t.name == name);

    [Test]
    public void EliteBagOpensTheSamePanelAboveSettlementAndKeepsTheSameChoiceData()
    {
        MapTreasureOverlay panel = Open(); Assert.NotNull(panel); Assert.IsTrue(panel.IsVisible);
        Assert.AreSame(choice, typeof(MapTreasureOverlay).GetField("activeRelicChoiceData", Private).GetValue(panel));
        Canvas settlementCanvas = (Canvas)typeof(BattleSettlementOverlay).GetField("overlayCanvas", Private).GetValue(battle);
        Assert.Greater(panel.GetComponentInChildren<Canvas>(true).sortingOrder, settlementCanvas.sortingOrder);
        Assert.IsFalse(((RectTransform)typeof(BattleSettlementOverlay).GetField("relicChoiceModalRoot", Private).GetValue(battle)).gameObject.activeSelf);
    }

    [Test]
    public void ConfirmClosesThePanelButOnlyTheExistingReturnServiceGrantsTheRelic()
    {
        MapTreasureOverlay panel = Open();
        Find(panel, "TreasureRelicChoice_0").GetComponent<Button>().onClick.Invoke();
        Find(panel, "ConfirmRelicChoiceButton").GetComponent<Button>().onClick.Invoke();
        Assert.IsFalse(panel.IsVisible); Assert.NotNull(choice.selectedChoice); Assert.IsFalse(choice.rewardApplied);
        BattleEncounterData encounter = new BattleEncounterData { tileType = MapTileType.Elite };
        Assert.IsTrue(BattleRelicChoiceSelectionService.TryApply(encounter, choice, out _));
        Assert.IsTrue(BattleRelicChoiceSelectionService.TryApply(encounter, choice, out _));
        Assert.AreEqual(1, encounter.resolvedRelics.Count); Assert.AreEqual(1, encounter.resolvedRelics[0].amount);
        Assert.AreSame(choice.selectedChoice.relicData, encounter.resolvedRelics[0].relicData);
    }

    [Test]
    public void ReopeningPreservesTheSelectionAndReusesOneCanvas()
    {
        MapTreasureOverlay first = Open();
        Find(first, "TreasureRelicChoice_1").GetComponent<Button>().onClick.Invoke();
        Find(first, "ConfirmRelicChoiceButton").GetComponent<Button>().onClick.Invoke();
        BattleSettlementLootEntry selected = choice.selectedChoice;
        MapTreasureOverlay again = Open();
        Assert.AreSame(first, again); Assert.AreSame(selected, choice.selectedChoice);
        Assert.AreEqual(1, again.GetComponentsInChildren<Canvas>(true).Length);
        Assert.IsTrue(Find(again, "ConfirmRelicChoiceButton").GetComponent<Button>().interactable);
    }

    [Test]
    public void EliteRefreshStillRequiresDeselectingAndPreservesItsExistingPoolAndLimit()
    {
        MapTreasureOverlay panel = Open(); Button refresh = Find(panel, "RefreshRelicChoiceButton").GetComponent<Button>();
        List<Relic_SO> originalPool = choice.refreshPool;
        Button card = Find(panel, "TreasureRelicChoice_0").GetComponent<Button>(); card.onClick.Invoke();
        Assert.IsFalse(refresh.interactable); refresh.onClick.Invoke(); Assert.AreEqual(1, choice.refreshesRemaining);
        card.onClick.Invoke(); Assert.IsNull(choice.selectedChoice); Assert.IsTrue(refresh.interactable);
        List<Relic_SO> before = choice.choices.Select(c => c.relicData).ToList();
        refresh.onClick.Invoke(); Assert.AreEqual(0, choice.refreshesRemaining); Assert.IsNull(choice.selectedChoice);
        Assert.IsTrue(choice.choices.All(c => !before.Contains(c.relicData))); Assert.AreSame(originalPool, choice.refreshPool);
    }

    [Test]
    public void HidingSettlementDismissesTheSharedPanelAndOldConfirmCannotGrantAnything()
    {
        MapTreasureOverlay panel = Open(); Find(panel, "TreasureRelicChoice_0").GetComponent<Button>().onClick.Invoke();
        Button confirm = Find(panel, "ConfirmRelicChoiceButton").GetComponent<Button>();
        battle.HideResult(); confirm.onClick.Invoke();
        Assert.IsFalse(panel.IsVisible); Assert.IsFalse(choice.rewardApplied);
        Assert.IsNull(typeof(MapTreasureOverlay).GetField("activeRelicChoiceData", Private).GetValue(panel));
    }

    [Test]
    public void ReturningWithoutASelectionUsesTheSharedPanelAndDoesNotCommit()
    {
        bool applied = (bool)typeof(BattleSettlementOverlay).GetMethod("TryApplyRequiredRelicChoice", Private).Invoke(battle, new object[] { settlement });
        Assert.IsFalse(applied); Assert.IsFalse(choice.rewardApplied); Assert.IsTrue(host.GetComponentInChildren<MapTreasureOverlay>().IsVisible);
    }
}
