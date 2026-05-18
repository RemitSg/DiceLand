using UnityEngine;

public static partial class RelicSystem
{
    private static void HandleQueueStatusToPlayerOnClashLostAgainstStatusTarget(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if (context.trigger != Relic_SO.RelicTrigger.OnPlayerClashLost || effect.statusData == null || effect.conditionStatusData == null)
            return;

        BattleAllyController ally = context.source as BattleAllyController;
        BattleEnemyController target = context.target as BattleEnemyController;
        if (ally == null || target == null || target.GetStatusStacks(effect.conditionStatusData) < Mathf.Max(1, effect.requiredStatusStacks))
            return;

        int stacks = CalculateScaledStatusStacks(relicState, effect);
        if (TryApplyStatus(ally, effect, stacks))
            AddRelicFeedback(context, relicData, $"{ally.GetDebugDisplayName()} 将获得 {stacks} 层 {GetStatusDisplayName(effect.statusData)}。");
    }

    private static void HandleIncreaseSharedEnergyMaxAtBattleStart(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if (context.trigger != Relic_SO.RelicTrigger.OnBattleSetup || context.currentRound != 1)
            return;

        context.additionalSharedEnergyMax += Mathf.Max(0, effect.amount * Mathf.Max(1, relicState.amount));
    }

    private static void HandleGrantMoney(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if (context.runState == null)
            return;

        int money = Mathf.Max(0, effect.amount * Mathf.Max(1, relicState.amount));
        if (money <= 0)
            return;

        switch (context.trigger)
        {
            case Relic_SO.RelicTrigger.OnRestActionPointRestored:
            case Relic_SO.RelicTrigger.OnRestReforgeCompleted:
            case Relic_SO.RelicTrigger.OnRestHealCompleted:
                context.runState.AddMoney(money);
                break;
        }
    }

    private static void HandleIgnoreMoveCostWhenDestinationUnvisitedRestTile(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if (context.trigger != Relic_SO.RelicTrigger.OnMoveCostAboutToSpend)
            return;
        if (context.tile == null || context.tile.tileType != MapTileType.Rest || !context.isDestinationUnvisited)
            return;

        context.ignoreMoveCost = true;
        AddRelicFeedback(context, relicData, "前往未探索的休息格不消耗行动点。");
    }

    private static void HandleGrantTemporarySharedEnergyOnSpecificRound(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if (context.trigger != Relic_SO.RelicTrigger.OnBattleRoundStart)
            return;
        if (!IsCurrentRoundWithinRange(context, effect, out _, out _))
            return;

        context.additionalTemporarySharedEnergy += Mathf.Max(0, effect.amount * Mathf.Max(1, relicState.amount));
    }

    private static void HandleGrantTemporarySharedEnergyEachRound(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if (context.trigger != Relic_SO.RelicTrigger.OnBattleRoundStart)
            return;

        context.additionalTemporarySharedEnergy += Mathf.Max(0, effect.amount * Mathf.Max(1, relicState.amount));
    }

    private static void HandleApplyStatusToTemporarySkillUser(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if (context.trigger != Relic_SO.RelicTrigger.OnPlayerSkillUsed || effect.statusData == null)
            return;

        BattleAllyController ally = context.source as BattleAllyController;
        if (ally == null || !ally.IsTemporarySkill(context.skillData))
            return;

        int stacks = CalculateScaledStatusStacks(relicState, effect);
        if (TryApplyStatus(ally, effect, stacks))
            AddRelicFeedback(context, relicData, $"{ally.GetDebugDisplayName()} 使用临时技能，获得 {stacks} 层 {GetStatusDisplayName(effect.statusData)}。");
    }

    private static void HandlePreventBattleDefeatAndFullHealPartyOncePerRun(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if (context.trigger != Relic_SO.RelicTrigger.OnBattleDefeat || relicState == null || relicState.amount <= 0)
            return;

        relicState.amount = 0;
        context.preventBattleDefeat = true;
        context.fullHealPartyOnDefeat = true;
        context.forceReturnToMapOnDefeat = true;
        HealAllBattleAlliesToFull(context.allies, reviveDead: true);
        AddRelicFeedback(context, relicData, "重生发动：队伍恢复并返回地图。");
    }

    private static void HandleModifyBattleExperiencePercentAndHealMissingHp(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if (context.trigger != Relic_SO.RelicTrigger.OnBattleCompletion || !context.battleVictory)
            return;

        int percentDelta = effect.powerAmount * Mathf.Max(1, relicState.amount);
        if (percentDelta != 0)
        {
            float multiplier = Mathf.Max(0f, 1f + percentDelta / 100f);
            context.modifiedBattleExperience = Mathf.Max(0, Mathf.FloorToInt(context.modifiedBattleExperience * multiplier));
        }

        HealAllBattleAlliesByMissingHpPercent(context.allies, Mathf.Max(0, effect.amount * Mathf.Max(1, relicState.amount)));
    }

    private static void HandleSummonTemporaryAllyFromEnemyDataAtBattleStart(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if (context.trigger != Relic_SO.RelicTrigger.OnBattleSetup || context.currentRound != 1 || effect.enemyData == null)
            return;

        context.summonAllyEnemyData = effect.enemyData;
    }

    private static void HandleQueueNextRoundMinPowerBonusOnMinRollOncePerRound(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if (context.trigger != Relic_SO.RelicTrigger.OnPlayerDiceRolled || !context.rolledDiceWasMinimum)
            return;

        BattleAllyController ally = context.source as BattleAllyController;
        if (ally == null)
            return;

        ally.QueueNextRoundMinPowerBonusIfUnused(Mathf.Max(0, effect.amount * Mathf.Max(1, relicState.amount)));
    }

    private static void HandleHealPartyPercentAfterBattleInSpecialDungeon(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if (context.trigger != Relic_SO.RelicTrigger.OnBattleCompletion || !context.battleVictory || !context.inSpecialDungeon)
            return;

        HealAllBattleAlliesByMaxHpPercent(context.allies, Mathf.Max(0, effect.amount * Mathf.Max(1, relicState.amount)));
    }

    private static void HealAllBattleAlliesToFull(BattleAllyController[] allies, bool reviveDead)
    {
        if (allies == null)
            return;

        for (int i = 0; i < allies.Length; i++)
        {
            BattleAllyController ally = allies[i];
            if (ally == null)
                continue;

            ally.RestoreToFullHealth(reviveDead);
        }
    }

    private static void HealAllBattleAlliesByMissingHpPercent(BattleAllyController[] allies, int percent)
    {
        if (allies == null || percent <= 0)
            return;

        for (int i = 0; i < allies.Length; i++)
        {
            BattleAllyController ally = allies[i];
            if (ally == null || ally.IsDead || ally.MaxHP <= 0)
                continue;

            int missingHp = Mathf.Max(0, ally.MaxHP - ally.CurrentHP);
            if (missingHp <= 0)
                continue;

            ally.Heal(Mathf.Max(1, Mathf.CeilToInt(missingHp * (percent / 100f))));
        }
    }

    private static void HealAllBattleAlliesByMaxHpPercent(BattleAllyController[] allies, int percent)
    {
        if (allies == null || percent <= 0)
            return;

        for (int i = 0; i < allies.Length; i++)
        {
            BattleAllyController ally = allies[i];
            if (ally == null || ally.IsDead || ally.MaxHP <= 0)
                continue;

            ally.Heal(Mathf.Max(1, Mathf.CeilToInt(ally.MaxHP * (percent / 100f))));
        }
    }
}
