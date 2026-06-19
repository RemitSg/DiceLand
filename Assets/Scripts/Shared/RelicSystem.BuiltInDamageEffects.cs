using UnityEngine;

public static partial class RelicSystem
{
    private static void HandleReduceIncomingSingleAttackDamageFlat(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if (context.trigger != Relic_SO.RelicTrigger.OnPlayerAboutToTakeSingleAttackDamage)
            return;

        BattleAllyController targetAlly = context.target as BattleAllyController;
        if (targetAlly == null)
            return;

        int reductionAmount = Mathf.Max(0, effect.amount * Mathf.Max(1, relicState.amount));
        if (reductionAmount <= 0)
            return;

        context.incomingDamageFlatModifier -= reductionAmount;
        context.AddIncomingDamageFlatModifierSource(-reductionAmount, GetRelicDisplayName(relicData));
        AddRelicFeedback(context, relicData, $"本次单方面攻击伤害 -{reductionAmount}。");
        return;
    }

    private static void HandleReduceIncomingSingleAttackDamagePercentWhenAttackerSlowerThanTarget(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if (context.trigger != Relic_SO.RelicTrigger.OnPlayerAboutToTakeSingleAttackDamage)
            return;

        BattleAllyController percentReductionTarget = context.target as BattleAllyController;
        BattleEnemyController percentReductionSource = context.source as BattleEnemyController;
        if (percentReductionTarget == null || percentReductionSource == null)
            return;

        if (context.incomingSourceSpeed >= context.incomingTargetSpeed)
            return;

        int reductionPercent = Mathf.Max(0, effect.amount * Mathf.Max(1, relicState.amount));
        if (reductionPercent <= 0)
            return;

        context.incomingDamagePercentModifier += reductionPercent;
        context.AddIncomingDamagePercentModifierSource(-reductionPercent, GetRelicDisplayName(relicData));
        AddRelicFeedback(
            context,
            relicData,
            $"{percentReductionSource.GetDebugDisplayName()} 速度低于 {percentReductionTarget.GetDebugDisplayName()}，本次即将到来的伤害 -{reductionPercent}% 。");
        return;
    }

    private static void HandleReduceIncomingAttackDamagePercentWhenAttackerSlowerThanTarget(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if (context.trigger != Relic_SO.RelicTrigger.OnPlayerAboutToTakeAttackDamage)
            return;

        BattleAllyController attackReductionTarget = context.target as BattleAllyController;
        BattleEnemyController attackReductionSource = context.source as BattleEnemyController;
        if (attackReductionTarget == null || attackReductionSource == null)
            return;

        if (context.incomingSourceSpeed >= context.incomingTargetSpeed)
            return;

        int attackReductionPercent = Mathf.Max(0, effect.amount * Mathf.Max(1, relicState.amount));
        if (attackReductionPercent <= 0)
            return;

        context.incomingDamagePercentModifier += attackReductionPercent;
        context.AddIncomingDamagePercentModifierSource(-attackReductionPercent, GetRelicDisplayName(relicData));
        AddRelicFeedback(
            context,
            relicData,
            $"{attackReductionSource.GetDebugDisplayName()} 速度低于 {attackReductionTarget.GetDebugDisplayName()}，本次即将到来的伤害 -{attackReductionPercent}%。");
        return;
    }

    private static void HandleReduceIncomingDamagePercentWhenSpeedGreaterThan(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if (context.trigger != Relic_SO.RelicTrigger.OnPlayerAboutToTakeDamage)
            return;

        BattleAllyController speedProtectedAlly = context.target as BattleAllyController;
        if (speedProtectedAlly == null)
            return;

        if (context.currentSpeed <= Mathf.Max(0, effect.requiredSpeed))
            return;

        int generalReductionPercent = Mathf.Max(0, effect.amount * Mathf.Max(1, relicState.amount));
        if (generalReductionPercent <= 0)
            return;

        context.incomingDamagePercentModifier += generalReductionPercent;
        context.AddIncomingDamagePercentModifierSource(-generalReductionPercent, GetRelicDisplayName(relicData));
        return;
    }

    private static void HandleModifyOutgoingDamagePercentWhenTargetHasDistinctStatuses(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if (context.trigger != Relic_SO.RelicTrigger.OnPlayerAboutToDealDamage)
            return;

        BattleEnemyController outgoingPercentTarget = context.target as BattleEnemyController;
        if (outgoingPercentTarget == null)
            return;

        if (CountDistinctStatuses(outgoingPercentTarget.StatusEntries) < Mathf.Max(1, effect.requiredDistinctStatusCount))
            return;

        int outgoingPercentBonus = Mathf.Max(0, effect.amount * Mathf.Max(1, relicState.amount));
        if (outgoingPercentBonus <= 0)
            return;

        context.outgoingDamagePercentModifier += outgoingPercentBonus;
        context.AddOutgoingDamagePercentModifierSource(outgoingPercentBonus, GetRelicDisplayName(relicData));
        return;
    }

    private static void HandleModifyOutgoingDamageFlatWhenTargetHasStatus(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if (context.trigger != Relic_SO.RelicTrigger.OnPlayerAboutToDealDamage)
            return;

        if (!IsSkillDiceDamageContext(context))
            return;

        BattleEnemyController outgoingFlatTarget = context.target as BattleEnemyController;
        if (outgoingFlatTarget == null || effect.conditionStatusData == null)
            return;

        if (!TargetHasRequiredStatusStacks(outgoingFlatTarget, effect.conditionStatusData, Mathf.Max(1, effect.requiredStatusStacks)))
            return;

        int outgoingFlatBonus = Mathf.Max(0, effect.amount * Mathf.Max(1, relicState.amount));
        if (outgoingFlatBonus <= 0)
            return;

        context.outgoingDamageFlatModifier += outgoingFlatBonus;
        context.AddOutgoingDamageFlatModifierSource(outgoingFlatBonus, GetRelicDisplayName(relicData));
        return;
    }

    private static bool IsSkillDiceDamageContext(RelicExecutionContext context)
    {
        return context != null && context.skillData != null && context.diceIndex >= 0;
    }

    private static bool TargetHasRequiredStatusStacks(BattleEnemyController target, BattleStatus_SO statusData, int requiredStacks)
    {
        if (target == null || statusData == null)
            return false;

        if (target.GetStatusStacks(statusData) >= requiredStacks)
            return true;

        if (string.Equals(statusData.StatusId, "target", System.StringComparison.OrdinalIgnoreCase)
            && target.GetStatusStacks("targetTemp") >= requiredStacks)
        {
            return true;
        }

        return false;
    }

    private static void HandleSurviveLethalDamageOncePerBattleAndApplyStatus(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if (context.trigger != Relic_SO.RelicTrigger.OnPlayerAboutToTakeLethalDamage)
            return;

        BattleAllyController lethalTarget = context.target as BattleAllyController;
        if (lethalTarget == null)
            return;

        string relicId = !string.IsNullOrWhiteSpace(relicData.relicId) ? relicData.relicId : relicData.name;
        if (lethalTarget.HasConsumedLethalProtection(relicId))
            return;

        if (!lethalTarget.TryConsumeLethalProtection(relicId))
            return;

        context.preventLethalDamage = true;
        context.minimumSurvivingHp = Mathf.Max(1, context.minimumSurvivingHp);

        int lethalStatusStacks = Mathf.Max(0, effect.amount * Mathf.Max(1, relicState.amount));
        if (effect.statusData != null && lethalStatusStacks > 0)
        {
            lethalTarget.AddStatus(
                effect.statusData,
                effect.statusData.StatusId,
                effect.statusData.DisplayName,
                lethalStatusStacks);
        }

        AddRelicFeedback(context, relicData, $"{lethalTarget.GetDebugDisplayName()} 以 {context.minimumSurvivingHp} 点生命幸存。");
        return;
    }

    private static void HandleReduceIncomingAttackDamagePercentWhenAttackerHasStatus(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if (context.trigger != Relic_SO.RelicTrigger.OnPlayerAboutToTakeAttackDamage)
            return;

        BattleEnemyController statusAttacker = context.source as BattleEnemyController;
        if (statusAttacker == null || effect.conditionStatusData == null)
            return;

        if (statusAttacker.GetStatusStacks(effect.conditionStatusData) < Mathf.Max(1, effect.requiredStatusStacks))
            return;

        int attackStatusReductionPercent = Mathf.Max(0, effect.amount * Mathf.Max(1, relicState.amount));
        if (attackStatusReductionPercent <= 0)
            return;

        context.incomingDamagePercentModifier += attackStatusReductionPercent;
        context.AddIncomingDamagePercentModifierSource(-attackStatusReductionPercent, GetRelicDisplayName(relicData));
        return;
    }

    private static void HandleReduceIncomingAttackDamageFlatWhenAttackerHasStatus(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if (context.trigger != Relic_SO.RelicTrigger.OnPlayerAboutToTakeAttackDamage)
            return;

        BattleEnemyController statusAttacker = context.source as BattleEnemyController;
        if (statusAttacker == null || effect.conditionStatusData == null)
            return;

        if (statusAttacker.GetStatusStacks(effect.conditionStatusData) < Mathf.Max(1, effect.requiredStatusStacks))
            return;

        int attackStatusReductionFlat = Mathf.Max(0, effect.amount * Mathf.Max(1, relicState.amount));
        if (attackStatusReductionFlat <= 0)
            return;

        context.incomingDamageFlatModifier -= attackStatusReductionFlat;
        context.AddIncomingDamageFlatModifierSource(-attackStatusReductionFlat, GetRelicDisplayName(relicData));
        return;
    }

    private static void HandleReduceIncomingDamageFlatPerStatusStacks(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if (context.trigger != Relic_SO.RelicTrigger.OnPlayerAboutToTakeDamage)
            return;

        BattleAllyController flatReductionTarget = context.target as BattleAllyController;
        if (flatReductionTarget == null || effect.conditionStatusData == null)
            return;

        int statusStacks = flatReductionTarget.GetStatusStacks(effect.conditionStatusData);
        if (statusStacks < Mathf.Max(1, effect.requiredStatusStacks))
            return;

        int effectiveStacks = Mathf.Min(statusStacks, Mathf.Max(1, effect.maxStacks));
        int flatReductionAmount = Mathf.Max(0, effectiveStacks * Mathf.Max(0, effect.amount) * Mathf.Max(1, relicState.amount));
        if (flatReductionAmount <= 0)
            return;

        context.incomingDamageFlatModifier -= flatReductionAmount;
        context.AddIncomingDamageFlatModifierSource(-flatReductionAmount, GetRelicDisplayName(relicData));
        return;
    }

    private static void HandleReflectSingleAttackDamageToAttackerPercent(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if (context.trigger != Relic_SO.RelicTrigger.OnPlayerTookSingleAttackDamage)
            return;

        BattleEnemyController reflectedAttacker = context.source as BattleEnemyController;
        if (reflectedAttacker == null || context.resolvedDamage <= 0)
            return;

        int reflectPercent = Mathf.Max(0, effect.amount * Mathf.Max(1, relicState.amount));
        if (reflectPercent <= 0)
            return;

        int reflectedDamage = Mathf.FloorToInt((context.resolvedDamage * (reflectPercent / 100f)) + 0.5f);
        reflectedDamage = Mathf.Min(Mathf.Max(0, effect.maxStacks), reflectedDamage);
        if (reflectedDamage <= 0)
            return;

        int actualReflectedDamage = reflectedAttacker.TakeDamage(reflectedDamage, 0, reflectedDamage);
        if (actualReflectedDamage <= 0)
            return;

        AddRelicFeedback(context, relicData, $"反弹 {actualReflectedDamage} 点伤害给 {reflectedAttacker.GetDebugDisplayName()}。");
        return;
    }
}
