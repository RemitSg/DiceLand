using UnityEngine;

public static partial class RelicSystem
{
    private static void HandleModifyPlayerDiceWhenSelfHasStatus(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if (context.trigger != Relic_SO.RelicTrigger.OnPlayerDicePrepared || !context.sourceIsPlayer)
            return;

        BattleAllyController ally = context.source as BattleAllyController;
        if (ally == null || effect.conditionStatusData == null)
            return;

        int currentStacks = ally.GetStatusStacks(effect.conditionStatusData);
        if (currentStacks < Mathf.Max(1, effect.requiredStatusStacks))
            return;

        int relicAmount = Mathf.Max(1, relicState.amount);
        context.AddPlayerDiceModifier(relicData, effect.minPowerAmount * relicAmount,
            effect.maxPowerAmount * relicAmount, effect.powerAmount * relicAmount);
        return;
    }

    private static void HandleModifyPlayerDiceBasedOnLastRoundSkillMatch(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if (context.trigger != Relic_SO.RelicTrigger.OnPlayerDicePrepared || !context.sourceIsPlayer)
            return;

        BattleAllyController sourceAlly = context.source as BattleAllyController;
        if (sourceAlly == null || context.skillData == null)
            return;

        int magnitude = Mathf.Max(0, effect.amount * Mathf.Max(1, relicState.amount));
        if (magnitude <= 0)
            return;

        bool sameAsLastRound = sourceAlly.LastRoundUsedSkill != null && sourceAlly.LastRoundUsedSkill == context.skillData;
        context.AddPlayerDiceModifier(relicData, power: sameAsLastRound ? -magnitude : magnitude);
        return;
    }

    private static void HandleModifyPlayerClashDiceWhenFasterThanTarget(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if (context.trigger != Relic_SO.RelicTrigger.OnPlayerDicePrepared || !context.sourceIsPlayer || !context.isClashPreparation)
            return;

        BattleAllyController fasterSourceAlly = context.source as BattleAllyController;
        BattleEnemyController slowerTargetEnemy = context.target as BattleEnemyController;
        if (fasterSourceAlly == null || slowerTargetEnemy == null)
            return;

        if (fasterSourceAlly.Speed <= slowerTargetEnemy.Speed)
            return;

        int clashBonus = effect.powerAmount * Mathf.Max(1, relicState.amount);
        if (clashBonus <= 0)
            return;

        context.AddPlayerDiceModifier(relicData, power: clashBonus, clashOnlyPower: true);
        return;
    }

    private static void HandleReduceTargetClashMinPowerWhenTargetHasStatus(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if (context.trigger != Relic_SO.RelicTrigger.OnPlayerDicePrepared || !context.sourceIsPlayer || !context.isClashPreparation)
            return;

        BattleEnemyController statusTargetEnemy = context.target as BattleEnemyController;
        if (statusTargetEnemy == null || effect.conditionStatusData == null)
            return;

        int requiredTargetStatusStacks = Mathf.Max(1, effect.requiredStatusStacks);
        if (statusTargetEnemy.GetStatusStacks(effect.conditionStatusData) < requiredTargetStatusStacks)
            return;

        int targetMinReduction = Mathf.Max(0, effect.amount * Mathf.Max(1, relicState.amount));
        if (targetMinReduction <= 0)
            return;

        context.AddPlayerDiceModifier(relicData, targetMin: -targetMinReduction);
        return;
    }

    private static void HandleModifyPlayerDiceWhenSkillUsedConsecutively(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if (context.trigger != Relic_SO.RelicTrigger.OnPlayerDicePrepared || !context.sourceIsPlayer)
            return;

        BattleAllyController skillChainAlly = context.source as BattleAllyController;
        if (skillChainAlly == null || context.skillData == null)
            return;

        int completedUses = context.previewRoundRelicProcs != null
            ? context.previewConsecutiveSkillUses
            : skillChainAlly.GetCompletedConsecutiveSkillUses(context.skillData);
        if (completedUses < Mathf.Max(1, effect.requiredConsecutiveSkillUses))
            return;

        int skillChainPowerBonus = Mathf.Max(0, effect.powerAmount * Mathf.Max(1, relicState.amount));
        if (skillChainPowerBonus <= 0)
            return;

        context.AddPlayerDiceModifier(relicData, power: skillChainPowerBonus);
        if (context.previewRoundRelicProcs == null)
            skillChainAlly.MarkConsecutiveSkillResetAfterCurrentUse(context.skillData);
        else
            context.previewResetsConsecutiveSkill = true;
        return;
    }

    private static void HandleModifyDefenseSkillMaxPower(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if (context.trigger != Relic_SO.RelicTrigger.OnPlayerDicePrepared || !context.sourceIsPlayer)
            return;
        if (context.skillData == null || !context.skillData.IsDefenseSkill)
            return;

        int defenseMaxBonus = effect.maxPowerAmount * Mathf.Max(1, relicState.amount);
        if (defenseMaxBonus == 0)
            defenseMaxBonus = effect.amount * Mathf.Max(1, relicState.amount);
        if (defenseMaxBonus <= 0)
            return;

        context.AddPlayerDiceModifier(relicData, max: defenseMaxBonus);
        return;
    }
}
