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
        return DispatchBattleSetup(
            encounter,
            currentRound,
            allies,
            enemies,
            processBattleSetup,
            null,
            null,
            flushFeedback);
    }

    public static int DispatchBattleSetup(
        BattleEncounterData encounter,
        int currentRound,
        BattleAllyController[] allies,
        BattleEnemyController[] enemies,
        Func<BattleEncounterData, int, BattleAllyController[], BattleEnemyController[], RelicExecutionContext> processBattleSetup,
        Action<int> increaseSharedEnergyMax,
        Action<RelicExecutionContext> summonTemporaryAllies,
        Action<RelicExecutionContext> flushFeedback)
    {
        if (!HasRelics(encounter) || processBattleSetup == null)
            return 0;

        RelicExecutionContext context = processBattleSetup(encounter, currentRound, allies, enemies);
        int extraButtonsPerAlly = currentRound == 1 && context != null
            ? Mathf.Max(0, context.additionalTemporaryAllySpeedButtons)
            : 0;

        if (context != null)
        {
            if (currentRound == 1 && context.additionalSharedEnergyMax > 0)
                increaseSharedEnergyMax?.Invoke(context.additionalSharedEnergyMax);
            if (currentRound == 1)
                summonTemporaryAllies?.Invoke(context);
            flushFeedback?.Invoke(context);
        }

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
        DispatchRoundStart(
            encounter,
            currentRound,
            allies,
            enemies,
            processRoundStart,
            restoreSharedEnergy,
            null,
            flushFeedback);
    }

    public static void DispatchRoundStart(
        BattleEncounterData encounter,
        int currentRound,
        BattleAllyController[] allies,
        BattleEnemyController[] enemies,
        Func<BattleEncounterData, int, BattleAllyController[], BattleEnemyController[], RelicExecutionContext> processRoundStart,
        Action<int> restoreSharedEnergy,
        Action<int> addTemporarySharedEnergy,
        Action<RelicExecutionContext> flushFeedback)
    {
        if (!HasRelics(encounter) || processRoundStart == null)
            return;

        RelicExecutionContext context = processRoundStart(encounter, currentRound, allies, enemies);
        if (context == null)
            return;

        if (context.additionalSharedEnergyRecovery > 0)
            restoreSharedEnergy?.Invoke(context.additionalSharedEnergyRecovery);
        if (context.additionalTemporarySharedEnergy > 0)
            addTemporarySharedEnergy?.Invoke(context.additionalTemporarySharedEnergy);

        flushFeedback?.Invoke(context);
    }

    private static bool HasRelics(BattleEncounterData encounter)
    {
        return encounter != null
            && encounter.relicInventory != null
            && encounter.relicInventory.Count > 0;
    }
}
