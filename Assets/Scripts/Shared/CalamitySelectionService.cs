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
}
