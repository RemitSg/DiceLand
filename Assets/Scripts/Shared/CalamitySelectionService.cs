using System;
using System.Collections.Generic;
using UnityEngine;

public static class CalamitySelectionService
{
    public static HashSet<string> BuildExcludedCalamityIds(RunState runState)
    {
        HashSet<string> excludedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (runState == null || runState.drawnCalamityIds == null)
            return excludedIds;

        for (int i = 0; i < runState.drawnCalamityIds.Count; i++)
        {
            string id = runState.drawnCalamityIds[i];
            if (!string.IsNullOrWhiteSpace(id))
                excludedIds.Add(id);
        }

        return excludedIds;
    }

    public static List<Calamity_SO> RollChoices(
        CalamityPool_SO calamityPool,
        RunState runState,
        int calamityTier,
        int choiceCount = 2,
        System.Random rng = null)
    {
        if (calamityPool == null)
            return new List<Calamity_SO>();

        HashSet<string> excludedIds = BuildExcludedCalamityIds(runState);
        System.Random resolvedRng = rng ?? CreateDefaultRandom(runState, calamityTier, excludedIds.Count);
        return calamityPool.RollChoices(calamityTier, excludedIds, choiceCount, resolvedRng);
    }

    public static void MarkChoicesDrawn(RunState runState, IReadOnlyList<Calamity_SO> choices)
    {
        if (runState == null || choices == null)
            return;

        for (int i = 0; i < choices.Count; i++)
            runState.MarkCalamityDrawn(choices[i]);
    }

    public static void ApplyChosenCalamity(RunState runState, Calamity_SO calamity, int calamityTier)
    {
        if (runState == null || calamity == null)
            return;

        runState.AddCalamity(calamity, calamityTier);
    }

    private static System.Random CreateDefaultRandom(RunState runState, int calamityTier, int excludedCount)
    {
        int seed = unchecked(Environment.TickCount ^ (calamityTier * 397) ^ (runState != null ? runState.currentCycle * 811 : 0) ^ excludedCount);
        return new System.Random(seed);
    }
    public static List<string> ApplyBuiltInMapEffects(Calamity_SO calamity, RunState runState, BoardManager boardManager, EnemyRoster_SO enemyRoster, Action<BoardTileData> showDestination = null)
    {
        List<string> feedback = new List<string>();
        if (calamity == null)
            return feedback;

        IReadOnlyList<CalamityBuiltInEffectEntry> effects = calamity.GetValidEffects();
        for (int i = 0; i < effects.Count; i++)
        {
            CalamityBuiltInEffectEntry effect = effects[i];
            if (effect == null)
                continue;

            switch (effect.effectType)
            {
                case CalamityBuiltInEffectType.TeleportToRandomOuterUnvisitedTile:
                    if (TryTeleportToRandomOuterUnvisitedTile(runState, boardManager, out BoardTileData destination))
                    {
                        showDestination?.Invoke(destination);
                        feedback.Add($"灾厄效果：你被传送至外围未探索格子 [{destination.coord.x},{destination.coord.y}]。");
                    }
                    else
                        feedback.Add("灾厄效果：没有可传送的外围未探索格子。");
                    break;
                case CalamityBuiltInEffectType.LoseRandomRelics:
                    feedback.Add(RemoveRandomRelics(runState, Mathf.Max(0, effect.effectAmount)));
                    break;
                case CalamityBuiltInEffectType.ConvertUnvisitedRestTilesToBattles:
                    int converted = ConvertRandomUnvisitedRestTilesToBattles(boardManager, enemyRoster, Mathf.Max(0, effect.effectAmount));
                    feedback.Add(converted > 0
                        ? $"灾厄效果：{converted} 个未探索休息格变为了普通战斗。"
                        : "灾厄效果：没有可替换的未探索休息格。");
                    break;
            }
        }

        return feedback;
    }

    private static bool TryTeleportToRandomOuterUnvisitedTile(RunState runState, BoardManager boardManager, out BoardTileData destination)
    {
        destination = null;
        if (boardManager == null || runState == null)
            return false;

        List<BoardTileData> candidates = new List<BoardTileData>();
        foreach (BoardTileData tile in boardManager.GetAllTiles())
        {
            if (tile == null
                || tile.isVisited
                || tile.isSpawnTile
                || tile.dangerBand != MapDangerBand.Outer
                || tile.tileType == MapTileType.Boss
                || tile.tileType == MapTileType.SpecialDungeonExit)
            {
                continue;
            }

            candidates.Add(tile);
        }

        if (candidates.Count <= 0)
            return false;

        destination = candidates[UnityEngine.Random.Range(0, candidates.Count)];
        runState.playerGridPosition = destination.coord;
        boardManager.SetSelectedTile(destination.coord);
        boardManager.RefreshAllViews();
        return true;
    }

    private static string RemoveRandomRelics(RunState runState, int amount)
    {
        if (amount <= 0)
            return "灾厄效果：没有失去遗物。";
        if (runState == null || runState.relicInventory == null || runState.relicInventory.Count == 0)
            return "灾厄效果：没有可失去的遗物。";

        List<string> removedNames = new List<string>();
        for (int i = 0; i < amount; i++)
        {
            List<RunRelicState> candidates = new List<RunRelicState>();
            for (int stackIndex = 0; stackIndex < runState.relicInventory.Count; stackIndex++)
            {
                RunRelicState stack = runState.relicInventory[stackIndex];
                if (stack != null && stack.relicData != null && stack.amount > 0)
                    candidates.Add(stack);
            }

            if (candidates.Count <= 0)
                break;

            RunRelicState selected = candidates[UnityEngine.Random.Range(0, candidates.Count)];
            Relic_SO relicData = selected.relicData;
            string relicName = relicData.DisplayName;
            if (runState.TryConsumeRelic(relicData, 1))
                removedNames.Add(relicName);
        }

        return removedNames.Count > 0
            ? $"灾厄效果：失去遗物 {string.Join("、", removedNames)}。"
            : "灾厄效果：没有可失去的遗物。";
    }

    private static int ConvertRandomUnvisitedRestTilesToBattles(BoardManager boardManager, EnemyRoster_SO enemyRoster, int amount)
    {
        if (amount <= 0 || boardManager == null)
            return 0;

        List<BoardTileData> candidates = new List<BoardTileData>();
        foreach (BoardTileData tile in boardManager.GetAllTiles())
        {
            if (tile != null && tile.tileType == MapTileType.Rest && !tile.isVisited)
                candidates.Add(tile);
        }

        int converted = 0;
        while (converted < amount && candidates.Count > 0)
        {
            int index = UnityEngine.Random.Range(0, candidates.Count);
            BoardTileData tile = candidates[index];
            candidates.RemoveAt(index);
            if (tile == null)
                continue;

            tile.tileType = MapTileType.Battle;
            tile.isResolved = false;
            tile.assignedEnemyGroup = null;
            tile.assignedEvent = null;
            tile.previewTag = string.Empty;
            tile.previewEnemyCount = 0;
            MapBattleEncounterService.EnsureTileAssignedEnemyGroup(enemyRoster, tile);
            tile.previewDangerRating = MapDangerRatingService.ResolveRating(tile.tileType, tile.dangerBand, tile.assignedEnemyGroup);
            boardManager.RefreshTile(tile.coord);
            converted++;
        }

        if (converted > 0)
            boardManager.RefreshAllViews();
        return converted;
    }

}
