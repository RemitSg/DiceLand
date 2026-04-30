using System.Collections.Generic;
using NUnit.Framework;

public class BattleSettlementDataFactoryTests
{
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
