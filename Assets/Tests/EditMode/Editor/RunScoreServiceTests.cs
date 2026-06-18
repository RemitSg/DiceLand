using NUnit.Framework;
using UnityEngine;

public class RunScoreServiceTests
{
    [Test]
    public void CalculateScore_SumsConfiguredSettlementCategories()
    {
        RunState runState = CreateRunState();
        runState.settlementStats.runStartedUnixSeconds = 0;
        runState.settlementStats.normalBattleOuterCleared = 2;
        runState.settlementStats.normalBattleMidCleared = 1;
        runState.settlementStats.normalBattleInnerCleared = 1;
        runState.settlementStats.normalBattleCoreCleared = 1;
        runState.settlementStats.eliteBattleOuterCleared = 1;
        runState.settlementStats.eliteBattleMidCleared = 1;
        runState.settlementStats.eliteBattleInnerCleared = 1;
        runState.settlementStats.eliteBattleCoreCleared = 1;
        runState.settlementStats.specialDungeonCleared = 2;
        runState.settlementStats.specialDungeonScoreTotal = 450;
        runState.settlementStats.totalMoneyGained = 125;
        runState.settlementStats.craftedWeaponCount = 2;
        runState.settlementStats.eventTilesCompleted = 3;
        runState.settlementStats.eventBattlesCompleted = 2;
        runState.settlementStats.unexploredTilesVisited = 10;
        runState.settlementStats.keysCollected = 2;
        runState.settlementStats.bossesDefeated = 1;
        runState.calamityTier = 4;
        runState.partyMembers.Add(new RunPartyMemberState());
        runState.partyMembers.Add(new RunPartyMemberState());
        runState.partyMembers.Add(new RunPartyMemberState());
        runState.relicInventory.Add(new RunRelicState { amount = 2 });
        runState.relicInventory.Add(new RunRelicState { amount = 1 });

        RunSettlementScoreResult result = RunScoreService.Calculate(runState, RunScoreConfig_SO.CreateDefaultRuntimeConfig(), true, 3600);

        Assert.AreEqual(4505, result.finalScore);
        Assert.AreEqual(4, result.gainedAbilityPoints);
        Assert.AreEqual(3600, result.displayStats.durationSeconds);
        Assert.AreEqual(3, result.displayStats.partyMemberCount);
        Assert.AreEqual(3, result.displayStats.relicCount);
        Assert.That(result.debugLines, Has.Some.Contains("Calamity"));
    }

    [Test]
    public void CalculateScore_AppliesShortRunMultiplierAtFiveMinutes()
    {
        RunState runState = CreateRunState();
        runState.settlementStats.runStartedUnixSeconds = 0;
        runState.settlementStats.bossesDefeated = 1;

        RunSettlementScoreResult result = RunScoreService.Calculate(runState, RunScoreConfig_SO.CreateDefaultRuntimeConfig(), true, 300);

        Assert.AreEqual(100, result.finalScore);
        Assert.AreEqual(0, result.gainedAbilityPoints);
    }

    [Test]
    public void CalculateScore_SkipsCalamityScoreWhenRunFailed()
    {
        RunState runState = CreateRunState();
        runState.settlementStats.runStartedUnixSeconds = 0;
        runState.settlementStats.normalBattleOuterCleared = 1;
        runState.calamityTier = 6;

        RunSettlementScoreResult result = RunScoreService.Calculate(runState, RunScoreConfig_SO.CreateDefaultRuntimeConfig(), false, 3600);

        Assert.AreEqual(20, result.finalScore);
        Assert.That(result.debugLines, Has.Some.Contains("skipped"));
    }

    [Test]
    public void CalculateScore_UsesIntegerAbilityPointDivision()
    {
        RunState runState = CreateRunState();
        runState.settlementStats.runStartedUnixSeconds = 0;
        runState.settlementStats.totalMoneyGained = 4999;

        RunSettlementScoreResult result = RunScoreService.Calculate(runState, RunScoreConfig_SO.CreateDefaultRuntimeConfig(), false, 3600);

        Assert.AreEqual(999, result.finalScore);
        Assert.AreEqual(0, result.gainedAbilityPoints);
    }

    private static RunState CreateRunState()
    {
        return new RunState
        {
            settlementStats = new RunSettlementStats(),
            partyMembers = new System.Collections.Generic.List<RunPartyMemberState>(),
            relicInventory = new System.Collections.Generic.List<RunRelicState>()
        };
    }
}
