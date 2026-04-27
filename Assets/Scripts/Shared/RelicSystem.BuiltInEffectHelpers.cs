using System;
using System.Collections.Generic;
using UnityEngine;

public static partial class RelicSystem
{
    private static string BuildRoundRangeLabel(int minRound, int maxRound)
    {
        return minRound == maxRound
            ? $"第 {minRound} 回合"
            : $"第 {minRound}-{maxRound} 回合";
    }

    private static int CountDistinctStatuses(IReadOnlyList<BattleStatusRuntimeEntry> statusEntries)
    {
        if (statusEntries == null || statusEntries.Count == 0)
            return 0;

        int count = 0;
        for (int i = 0; i < statusEntries.Count; i++)
        {
            BattleStatusRuntimeEntry entry = statusEntries[i];
            if (entry == null || entry.stacks <= 0)
                continue;

            string key = entry.Key;
            if (string.IsNullOrWhiteSpace(key))
                continue;

            count += 1;
        }

        return count;
    }

    private static bool TryRegisterRoundScopedProc(Relic_SO relicData, string suffix)
    {
        BattleEncounterData activeEncounter = BattleEncounterRuntime.ActiveEncounter;
        if (activeEncounter == null)
            return true;

        string relicIdentity = relicData != null && !string.IsNullOrWhiteSpace(relicData.relicId)
            ? relicData.relicId
            : (relicData != null ? relicData.name : "relic");
        return activeEncounter.TryRegisterRoundRelicProc($"{relicIdentity}:{suffix}");
    }

    private static BattleAllyController ResolveHighestHpLivingAlly(BattleAllyController[] allies, BattleAllyController excluded)
    {
        if (allies == null || allies.Length == 0)
            return null;

        BattleAllyController best = null;
        for (int i = 0; i < allies.Length; i++)
        {
            BattleAllyController candidate = allies[i];
            if (candidate == null || candidate.IsDead || candidate == excluded)
                continue;

            if (best == null
                || candidate.CurrentHP > best.CurrentHP
                || (candidate.CurrentHP == best.CurrentHP && candidate.MaxHP > best.MaxHP))
            {
                best = candidate;
            }
        }

        return best;
    }

    private static BattleAllyController ResolveLowestHpLivingAlly(BattleAllyController[] allies, BattleAllyController excluded)
    {
        if (allies == null || allies.Length == 0)
            return null;

        BattleAllyController best = null;
        for (int i = 0; i < allies.Length; i++)
        {
            BattleAllyController candidate = allies[i];
            if (candidate == null || candidate.IsDead || candidate == excluded)
                continue;

            if (best == null
                || candidate.CurrentHP < best.CurrentHP
                || (candidate.CurrentHP == best.CurrentHP && candidate.MaxHP < best.MaxHP))
            {
                best = candidate;
            }
        }

        return best;
    }
}
