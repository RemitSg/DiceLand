using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public sealed class BattleSettlementOverlayTests
{
    private GameObject host;
    private BattleSettlementOverlay overlay;
    private BattleSettlementData data;
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    [SetUp]
    public void SetUp()
    {
        host = new GameObject("SettlementTest", typeof(EventSystem));
        overlay = host.AddComponent<BattleSettlementOverlay>();
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath("c6e7143cd2df45b45a784ef8c61a210f"));
        overlay.ConfigureFonts(font, font);
        data = new BattleSettlementData { isVictory = true, moneyGained = 23, startingMoney = 150, hasMoneyBalance = true, experienceGained = 13 };
        for (int i = 0; i < 2; i++)
            data.partyEntries.Add(new BattleSettlementEntry { displayName = i == 0 ? "Chara" : "Man", isAlive = i == 0,
                level = 1, currentExperience = 8, maxExperience = 10, experienceGained = 13,
                currentHP = i == 0 ? 8 : 0, maxHP = 20, resultingHP = i == 0 ? 20 : 0, resultingMaxHP = 24,
                resultingLevel = 2, resultingExperience = 11, resultingMaxExperience = 20, experienceRequirements = new List<int> { 10, 20 } });
        for (int i = 0; i < 6; i++) data.lootEntries.Add(new BattleSettlementLootEntry { displayName = "物品" + i, amount = i + 1, rarity = (LootRarity)(i % 5) });
        overlay.ShowResult(data);
    }

    [TearDown] public void TearDown() { Object.DestroyImmediate(host); }
    private T Field<T>(string name) => (T)typeof(BattleSettlementOverlay).GetField(name, Private).GetValue(overlay);
    private TMP_Text Label(string name) => Field<Canvas>("overlayCanvas").GetComponentsInChildren<TMP_Text>(true).First(t => t.name == name);
    private IList Rows => Field<IList>("rowViews");
    private static T Row<T>(object row, string name) => (T)row.GetType().GetField(name).GetValue(row);

    [Test]
    public void CountersStartTogetherAndKeepCharacterExperienceUnchangedUntilFlightsArrive()
    {
        Assert.AreEqual("金币 +0", Label("MoneyReward").text);
        Assert.AreEqual("经验值 +0", Label("Exp").text);
        Assert.AreEqual("战利品 ×0", Label("LootCount").text);
        overlay.AdvancePresentation(0.35f);
        Assert.AreEqual(RewardPhase.Counting, (RewardPhase)(int)overlay.PresentationPhase);
        StringAssert.DoesNotEndWith("+0", Label("MoneyReward").text);
        StringAssert.DoesNotEndWith("+0", Label("Exp").text);
        Assert.AreEqual(0, overlay.ActiveExperienceFlights);
        Assert.AreEqual(8, Row<int>(Rows[0], "shownExperience"));
        overlay.AdvancePresentation(0.4f);
        Assert.AreEqual(BattleSettlementOverlay.RewardPresentationPhase.Transferring, overlay.PresentationPhase);
        Assert.AreEqual("金币 +23", Label("MoneyReward").text);
        Assert.AreEqual("经验值 +13", Label("Exp").text);
        Assert.AreEqual("战利品 ×6", Label("LootCount").text);
        Assert.AreEqual(16, overlay.ActiveExperienceFlights);
        Assert.AreEqual(8, Row<int>(Rows[1], "shownExperience"));
    }
    private enum RewardPhase { Hidden, Counting, Transferring, Complete }

    [Test]
    public void TransfersCreditEveryCharacterIncludingFallenAndKeepActualFinalThresholds()
    {
        overlay.AdvancePresentation(0.75f);
        IList flights = Field<IList>("experienceFlights");
        foreach (object row in Rows)
            Assert.AreEqual(13, flights.Cast<object>().Where(f => Row<object>(f, "row") == row).Sum(f => Row<int>(f, "credit")));
        overlay.AdvancePresentation(0.8f);
        Assert.Greater(Row<float>(Rows[0], "displayedCredit"), 0);
        Assert.Greater(Row<float>(Rows[1], "displayedCredit"), 0);
        overlay.AdvancePresentation(3);
        Assert.AreEqual(BattleSettlementOverlay.RewardPresentationPhase.Complete, overlay.PresentationPhase);
        foreach (object row in Rows)
        {
            Assert.AreEqual(2, Row<int>(row, "shownLevel"));
            Assert.AreEqual(11, Row<int>(row, "shownExperience"));
            Assert.AreEqual(20, Row<int>(row, "shownMaxExperience"));
        }
        StringAssert.Contains("倒下 0 / 24", Row<TMP_Text>(Rows[1], "hpText").text);
        Assert.AreEqual(8, data.partyEntries[0].currentExperience);
        Assert.AreEqual(150, data.startingMoney);
    }

    [Test]
    public void WalletUsesMapFeedbackCountsOnArrivalAndRetractsAfterFinishing()
    {
        overlay.AdvancePresentation(0.78f);
        Assert.IsTrue(Field<RectTransform>("walletRoot").gameObject.activeSelf);
        Assert.AreEqual("150", Label("Money").text);
        Assert.Less(Field<RectTransform>("walletRoot").anchoredPosition.y, 135);
        overlay.AdvancePresentation(0.5f);
        MapResourceFeedback feedback = Field<MapResourceFeedback>("walletFeedback");
        Assert.IsFalse(feedback.AutomaticUpdate);
        Assert.IsTrue(feedback.IsMoneyAnimating);
        Assert.AreEqual(150, feedback.DisplayedMoney);
        overlay.AdvancePresentation(0.7f);
        Assert.AreEqual(173, feedback.DisplayedMoney);
        overlay.AdvancePresentation(1);
        Assert.IsFalse(Field<RectTransform>("walletRoot").gameObject.activeSelf);
        Assert.IsFalse(feedback.gameObject.activeSelf);
    }

    [Test]
    public void LootIsRevealedInOrderAndNamesAndAmountsRemainVisible()
    {
        CanvasGroup[] reveals = Field<RectTransform>("lootGridRoot").GetComponentsInChildren<CanvasGroup>(true);
        Assert.IsTrue(reveals.All(g => g.alpha == 0));
        overlay.AdvancePresentation(1.02f);
        Assert.Greater(reveals[0].alpha, reveals[1].alpha);
        Assert.AreEqual(0, reveals[5].alpha);
        overlay.AdvancePresentation(3);
        Assert.IsTrue(reveals.All(g => g.alpha == 1));
        Assert.AreEqual(3, Field<RectTransform>("lootGridRoot").GetComponent<GridLayoutGroup>().constraintCount);
        Assert.AreEqual(6, Field<RectTransform>("lootGridRoot").GetComponentsInChildren<Button>().Length);
        Assert.IsTrue(Field<RectTransform>("lootGridRoot").GetComponentsInChildren<TMP_Text>().Any(t => t.text == "×6"));
    }

    [Test]
    public void MultiLevelAndMaximumLevelUseCapturedRequirements()
    {
        BattleSettlementEntry entry = data.partyEntries[0];
        entry.experienceGained = 42; entry.resultingLevel = 4; entry.resultingExperience = 5;
        entry.resultingMaxExperience = 0; entry.experienceRequirements = new List<int> { 10, 15, 20, 0 };
        overlay.ShowResult(data); overlay.AdvancePresentation(4);
        Assert.AreEqual(4, Row<int>(Rows[0], "shownLevel"));
        Assert.AreEqual(5, Row<int>(Rows[0], "shownExperience"));
        Assert.AreEqual(0, Row<int>(Rows[0], "shownMaxExperience"));
        StringAssert.Contains("MAX", Row<TMP_Text>(Rows[0], "expText").text);
    }

    [Test]
    public void ReopeningAndHidingRetireOldRowsAndEffectsWithoutDuplicatingCanvasOrRewards()
    {
        overlay.AdvancePresentation(1.15f);
        overlay.ShowResult(data);
        Assert.AreEqual(2, Field<RectTransform>("summaryRoot").childCount);
        Assert.AreEqual(6, Field<RectTransform>("lootGridRoot").childCount);
        Assert.AreEqual(0, overlay.ActiveExperienceFlights);
        Assert.AreEqual("金币 +0", Label("MoneyReward").text);
        overlay.AdvancePresentation(1.1f); overlay.HideResult();
        Assert.IsFalse(overlay.IsVisible);
        Assert.AreEqual(0, overlay.ActiveExperienceFlights);
        Assert.IsFalse(Field<MapResourceFeedback>("walletFeedback").gameObject.activeSelf);
        overlay.ShowResult(data); overlay.CompletePresentation();
        Assert.IsFalse(overlay.IsPresenting);
        Assert.AreEqual("金币 +23", Label("MoneyReward").text);
    }

    [TestCase(1920, 1080, 0)]
    [TestCase(2340, 1080, 80)]
    [TestCase(1280, 720, 0)]
    [TestCase(1024, 768, 20)]
    public void PanelFitsInsideSafeAreaAndLargeCollectionsScroll(int width, int height, int inset)
    {
        for (int i = 0; i < 7; i++) { data.partyEntries.Add(data.partyEntries[0]); data.lootEntries.Add(data.lootEntries[0]); }
        overlay.ShowResult(data);
        Rect safe = new Rect(inset, 0, width - inset * 2, height);
        overlay.ApplyViewport(new Vector2(width, height), safe);
        RectTransform panel = Field<RectTransform>("panelRoot");
        Assert.LessOrEqual(panel.sizeDelta.x * panel.localScale.x, safe.width * 1080f / height);
        Assert.LessOrEqual(panel.sizeDelta.y * panel.localScale.y, 1080);
        Assert.Greater(Field<RectTransform>("summaryRoot").rect.height, 438);
        Assert.Greater(Field<RectTransform>("lootGridRoot").rect.height, 438);
        Assert.IsFalse(Field<ScrollRect>("partyScroll").horizontal);
        Assert.IsFalse(Field<ScrollRect>("settlementLootScroll").horizontal);
        overlay.CompletePresentation();
    }

    [Test]
    public void DefeatAndEmptyRewardsFinishWithNoPhantomWalletOrLoot()
    {
        data.isVictory = false; data.moneyGained = data.experienceGained = 0;
        data.partyEntries.Clear(); data.lootEntries.Clear();
        overlay.ShowResult(data); overlay.AdvancePresentation(2);
        Assert.AreEqual("战斗失败", Label("Title").text);
        Assert.IsTrue(Label("EmptyLoot").gameObject.activeSelf);
        Assert.IsFalse(Field<RectTransform>("walletRoot").gameObject.activeSelf);
        Assert.AreEqual(BattleSettlementOverlay.RewardPresentationPhase.Complete, overlay.PresentationPhase);
    }
}
