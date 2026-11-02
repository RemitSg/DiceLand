using System.Collections.Generic;
using NUnit.Framework;

public class BattleSettlementDataFactoryTests
{
    [TestCase(true, 23, 23, 13)]
    [TestCase(true, -5, 0, 13)]
    [TestCase(false, 23, 0, 0)]
    public void CompletionShowsFinalizedRewardsAndReadsBalanceWithoutCreditingTheRun(bool victory, int reward, int shownMoney, int shownExperience)
    {
        var field = typeof(MapRunRuntime).GetField("currentSnapshot", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        object previous = field.GetValue(null);
        try
        {
            MapRunRuntime.RestoreSnapshot(new MapRunSnapshot { sceneName = "SettlementTestMap", runState = new RunState { money = -8 } });
            var encounter = new BattleEncounterData { sourceSceneName = "SettlementTestMap", moneyGained = reward };
            BattleSettlementData data = BattleSettlementDataFactory.BuildCompletionData(victory, encounter, 2, 13, "BattleScene", null, null, null);
            Assert.AreEqual(shownMoney, data.moneyGained);
            Assert.AreEqual(shownExperience, data.experienceGained);
            Assert.IsTrue(data.hasMoneyBalance);
            Assert.AreEqual(-8, data.startingMoney);
            Assert.IsTrue(MapRunRuntime.TryGetMoney("settlementtestmap", out int unchanged));
            Assert.AreEqual(-8, unchanged);
            Assert.IsFalse(MapRunRuntime.TryGetMoney("AnotherMap", out _));
            BattleSettlementData retreat = BattleSettlementDataFactory.BuildForcedRetreatData(encounter, "脱离战斗", 13, "BattleScene", null, null, null);
            Assert.AreEqual(System.Math.Max(0, reward), retreat.moneyGained);
            Assert.AreEqual(-8, retreat.startingMoney);
        }
        finally { field.SetValue(null, previous); }
    }

    [Test]
    public void MissingEncounterDoesNotInventAMapBalance()
    {
        BattleSettlementData data = BattleSettlementDataFactory.BuildCompletionData(true, null, 1, 7, "BattleScene", null, null, null);
        Assert.AreEqual(0, data.moneyGained);
        Assert.IsFalse(data.hasMoneyBalance);
    }

    [Test]
    public void BuildCompletionData_UsesTutorialReturnSceneOnVictory()
    {
        BattleEncounterData encounter = new BattleEncounterData
        {
            sourceSceneName = "MapScene",
            returnToMainMenuOnVictory = true,
            tutorialCompletionSceneName = "Menu Selection"
        };

        BattleSettlementData data = BattleSettlementDataFactory.BuildCompletionData(
            true,
            encounter,
            2,
            12,
            "BattleScene",
            new List<BattleSettlementEntry>(),
            new List<BattleSettlementLootEntry>(),
            null);

        Assert.AreEqual("Victory", data.title);
        Assert.AreEqual("Menu Selection", data.returnSceneName);
        Assert.AreEqual(12, data.experienceGained);
    }

    [Test]
    public void BuildCompletionData_UsesTutorialDefeatTitleAndSubtitle()
    {
        BattleSettlementData data = BattleSettlementDataFactory.BuildCompletionData(
            false,
            new BattleEncounterData { isTutorialEncounter = true, sourceSceneName = "MapScene" },
            0,
            0,
            "BattleScene",
            new List<BattleSettlementEntry>(),
            new List<BattleSettlementLootEntry>(),
            null);

        Assert.AreEqual("Tutorial Failed", data.title);
        StringAssert.Contains("教学", data.subtitle);
    }

    [Test]
    public void BuildForcedRetreatData_UsesRetreatSubtitle()
    {
        List<BattleSettlementLootEntry> lootEntries = new List<BattleSettlementLootEntry>();
        BattleSettlementData data = BattleSettlementDataFactory.BuildForcedRetreatData(
            new BattleEncounterData { sourceSceneName = "MapScene" },
            "逃跑者 脱离了战斗，剩余可行动角色：1",
            10,
            "BattleScene",
            new List<BattleSettlementEntry>(),
            lootEntries,
            null);

        Assert.IsTrue(data.isVictory);
        Assert.AreEqual("Victory", data.title);
        Assert.AreEqual("逃跑者 脱离了战斗，剩余可行动角色：1", data.subtitle);
        Assert.AreSame(lootEntries, data.lootEntries);
    }
}
