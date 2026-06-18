using System;
using System.Collections.Generic;
using UnityEngine;

public static class RunScoreService
{
    public static RunSettlementScoreResult Calculate(
        RunState runState,
        RunScoreConfig_SO config,
        bool runCleared,
        long settlementUnixSeconds)
    {
        config = config != null ? config : RunScoreConfig_SO.CreateDefaultRuntimeConfig();
        RunSettlementStats stats = runState != null ? runState.settlementStats : null;
        if (stats == null)
            stats = new RunSettlementStats();

        RunSettlementScoreResult result = new RunSettlementScoreResult();
        result.displayStats = BuildDisplayStats(runState, stats, runCleared, settlementUnixSeconds);
        List<string> debug = result.debugLines;

        int baseScore = 0;
        AddCategory(ref baseScore, debug, "Normal Battle Outer", stats.normalBattleOuterCleared, config.normalBattleOuterScore);
        AddCategory(ref baseScore, debug, "Normal Battle Mid", stats.normalBattleMidCleared, config.normalBattleMidScore);
        AddCategory(ref baseScore, debug, "Normal Battle Inner", stats.normalBattleInnerCleared, config.normalBattleInnerScore);
        AddCategory(ref baseScore, debug, "Normal Battle Core", stats.normalBattleCoreCleared, config.normalBattleCoreScore);
        AddCategory(ref baseScore, debug, "Elite Battle Outer", stats.eliteBattleOuterCleared, config.eliteBattleOuterScore);
        AddCategory(ref baseScore, debug, "Elite Battle Mid", stats.eliteBattleMidCleared, config.eliteBattleMidScore);
        AddCategory(ref baseScore, debug, "Elite Battle Inner", stats.eliteBattleInnerCleared, config.eliteBattleInnerScore);
        AddCategory(ref baseScore, debug, "Elite Battle Core", stats.eliteBattleCoreCleared, config.eliteBattleCoreScore);

        baseScore += Mathf.Max(0, stats.specialDungeonScoreTotal);
        debug.Add($"Special Dungeon: cleared={stats.specialDungeonCleared}, score={Mathf.Max(0, stats.specialDungeonScoreTotal)}");

        int partyScore = result.displayStats.partyMemberCount >= config.minimumPartyMembersForScore
            ? result.displayStats.partyMemberCount * Mathf.Max(0, config.partyMemberScore)
            : 0;
        baseScore += partyScore;
        debug.Add($"Party Members: count={result.displayStats.partyMemberCount}, score={partyScore}");

        int moneyScore = Mathf.FloorToInt(Mathf.Max(0, stats.totalMoneyGained) * Mathf.Max(0f, config.moneyScoreMultiplier));
        baseScore += moneyScore;
        debug.Add($"Money: gained={stats.totalMoneyGained}, multiplier={config.moneyScoreMultiplier}, score={moneyScore}");

        AddCategory(ref baseScore, debug, "Crafted Weapons", stats.craftedWeaponCount, config.craftedWeaponScore);
        AddCategory(ref baseScore, debug, "Event Tiles", stats.eventTilesCompleted, config.completedEventTileScore);
        AddCategory(ref baseScore, debug, "Event Battles", stats.eventBattlesCompleted, config.completedEventBattleScore);
        AddCategory(ref baseScore, debug, "Unexplored Tiles", stats.unexploredTilesVisited, config.unexploredTileScore);
        AddCategory(ref baseScore, debug, "Relics", result.displayStats.relicCount, config.relicScore);
        AddCategory(ref baseScore, debug, "Keys", stats.keysCollected, config.keyScore);

        if (runCleared)
        {
            int calamityScore = config.GetCalamityScore(result.displayStats.calamityTier);
            baseScore += calamityScore;
            debug.Add($"Calamity: tier={result.displayStats.calamityTier}, score={calamityScore}");
        }
        else
        {
            debug.Add($"Calamity skipped: tier={result.displayStats.calamityTier}, score=0");
        }

        AddCategory(ref baseScore, debug, "Bosses", stats.bossesDefeated, config.bossDefeatScore);

        result.baseScore = baseScore;
        result.finalMultiplier = result.displayStats.durationSeconds <= config.shortRunThresholdSeconds
            ? Mathf.Max(0f, config.shortRunMultiplier)
            : 1f;
        result.finalScore = Mathf.FloorToInt(baseScore * result.finalMultiplier);
        result.gainedAbilityPoints = config.abilityPointDivisor > 0 ? result.finalScore / config.abilityPointDivisor : 0;

        debug.Add($"Final: base={baseScore}, duration={result.displayStats.durationSeconds}, multiplier={result.finalMultiplier}, final={result.finalScore}, abilityPoints={result.gainedAbilityPoints}");
        return result;
    }

    public static void LogDebugLines(RunSettlementScoreResult result)
    {
        if (result?.debugLines == null)
            return;

        for (int i = 0; i < result.debugLines.Count; i++)
            Debug.Log($"[RunScore] {result.debugLines[i]}");
    }

    private static RunSettlementDisplayStats BuildDisplayStats(
        RunState runState,
        RunSettlementStats stats,
        bool runCleared,
        long settlementUnixSeconds)
    {
        return new RunSettlementDisplayStats
        {
            durationSeconds = Mathf.Max(0, (int)(settlementUnixSeconds - stats.runStartedUnixSeconds)),
            normalBattleOuterCleared = stats.normalBattleOuterCleared,
            normalBattleMidCleared = stats.normalBattleMidCleared,
            normalBattleInnerCleared = stats.normalBattleInnerCleared,
            normalBattleCoreCleared = stats.normalBattleCoreCleared,
            eliteBattleOuterCleared = stats.eliteBattleOuterCleared,
            eliteBattleMidCleared = stats.eliteBattleMidCleared,
            eliteBattleInnerCleared = stats.eliteBattleInnerCleared,
            eliteBattleCoreCleared = stats.eliteBattleCoreCleared,
            specialDungeonCleared = stats.specialDungeonCleared,
            partyMemberCount = runState != null ? runState.GetPartyMemberCount() : 0,
            totalMoneyGained = stats.totalMoneyGained,
            craftedWeaponCount = stats.craftedWeaponCount,
            eventTilesCompleted = stats.eventTilesCompleted,
            eventBattlesCompleted = stats.eventBattlesCompleted,
            unexploredTilesVisited = stats.unexploredTilesVisited,
            relicCount = CountRelics(runState),
            keysCollected = stats.keysCollected,
            calamityTier = runState != null ? Mathf.Max(0, runState.calamityTier) : 0,
            bossesDefeated = stats.bossesDefeated,
            calamityScoreCounted = runCleared
        };
    }

    private static int CountRelics(RunState runState)
    {
        if (runState?.relicInventory == null)
            return 0;

        int total = 0;
        for (int i = 0; i < runState.relicInventory.Count; i++)
        {
            RunRelicState relic = runState.relicInventory[i];
            if (relic != null)
                total += Mathf.Max(0, relic.amount);
        }

        return total;
    }

    private static void AddCategory(ref int baseScore, List<string> debug, string label, int count, int scoreEach)
    {
        int score = Mathf.Max(0, count) * Mathf.Max(0, scoreEach);
        baseScore += score;
        debug.Add($"{label}: count={count}, each={scoreEach}, score={score}");
    }
}
