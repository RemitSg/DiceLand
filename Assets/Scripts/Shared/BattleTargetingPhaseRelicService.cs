using System;
using System.Collections.Generic;
using UnityEngine;

public static class BattleTargetingPhaseRelicService
{
    public static int DispatchBattleSetup(
        BattleEncounterData encounter,
        int currentRound,
        BattleAllyController[] allies,
        BattleEnemyController[] enemies,
        Func<BattleEncounterData, int, BattleAllyController[], BattleEnemyController[], RelicExecutionContext> processBattleSetup,
        Action<RelicExecutionContext> flushFeedback)
    {
        if (!HasRelics(encounter) || processBattleSetup == null)
            return 0;

        RelicExecutionContext context = processBattleSetup(encounter, currentRound, allies, enemies);
        int extraButtonsPerAlly = currentRound == 1 && context != null
            ? Mathf.Max(0, context.additionalTemporaryAllySpeedButtons)
            : 0;

        if (context != null)
            flushFeedback?.Invoke(context);

        return extraButtonsPerAlly;
    }

    public static void ApplyTemporarySpeedButtons(
        IReadOnlyList<BattleAllyController> allies,
        int extraButtonsPerAlly)
    {
        if (allies == null)
            return;

        int resolvedExtraButtons = Mathf.Max(0, extraButtonsPerAlly);
        for (int i = 0; i < allies.Count; i++)
        {
            if (allies[i] != null)
                allies[i].SetTemporaryExtraSpeedButtons(resolvedExtraButtons);
        }
    }

    public static void DispatchRoundStart(
        BattleEncounterData encounter,
        int currentRound,
        BattleAllyController[] allies,
        BattleEnemyController[] enemies,
        Func<BattleEncounterData, int, BattleAllyController[], BattleEnemyController[], RelicExecutionContext> processRoundStart,
        Action<int> restoreSharedEnergy,
        Action<RelicExecutionContext> flushFeedback)
    {
        if (!HasRelics(encounter) || processRoundStart == null)
            return;

        RelicExecutionContext context = processRoundStart(encounter, currentRound, allies, enemies);
        if (context == null)
            return;

        if (context.additionalSharedEnergyRecovery > 0)
            restoreSharedEnergy?.Invoke(context.additionalSharedEnergyRecovery);

        flushFeedback?.Invoke(context);
    }

    private static bool HasRelics(BattleEncounterData encounter)
    {
        return encounter != null
            && encounter.relicInventory != null
            && encounter.relicInventory.Count > 0;
    }
}
