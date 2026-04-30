using System;

public static class BattleSuccessfulHitEffectDispatcher
{
    public static void DispatchAllyHit(
        BattleSuccessfulHitRelicPlan relicPlan,
        BattleSuccessfulHitSkillEffectPlan skillPlan,
        Action applyPlayerHitRelics,
        Action applyEnemyKilledByPlayerRelics,
        Action<EnemySkill_SO.DiceEffectTrigger> applyImmediateTrigger,
        Action applyReactiveOnHitStatusEffects,
        Action<EnemySkill_SO.DiceEffectTrigger> applyStatusTrigger)
    {
        if (relicPlan.ApplyPlayerHitRelics)
            applyPlayerHitRelics?.Invoke();
        if (relicPlan.ApplyEnemyKilledByPlayerRelics)
            applyEnemyKilledByPlayerRelics?.Invoke();

        DispatchSkillEffects(skillPlan, applyImmediateTrigger, applyReactiveOnHitStatusEffects, applyStatusTrigger);
    }

    public static void DispatchEnemyHit(
        BattleSuccessfulHitRelicPlan relicPlan,
        BattleSuccessfulHitSkillEffectPlan skillPlan,
        Action applyPlayerTookSingleAttackRelics,
        Action<EnemySkill_SO.DiceEffectTrigger> applyImmediateTrigger,
        Action applyReactiveOnHitStatusEffects,
        Action<EnemySkill_SO.DiceEffectTrigger> applyStatusTrigger)
    {
        if (relicPlan.ApplyPlayerTookSingleAttackRelics)
            applyPlayerTookSingleAttackRelics?.Invoke();

        DispatchSkillEffects(skillPlan, applyImmediateTrigger, applyReactiveOnHitStatusEffects, applyStatusTrigger);
    }

    private static void DispatchSkillEffects(
        BattleSuccessfulHitSkillEffectPlan skillPlan,
        Action<EnemySkill_SO.DiceEffectTrigger> applyImmediateTrigger,
        Action applyReactiveOnHitStatusEffects,
        Action<EnemySkill_SO.DiceEffectTrigger> applyStatusTrigger)
    {
        if (skillPlan.ImmediateTriggers != null)
        {
            foreach (EnemySkill_SO.DiceEffectTrigger trigger in skillPlan.ImmediateTriggers)
                applyImmediateTrigger?.Invoke(trigger);
        }

        if (skillPlan.ApplyReactiveOnHitStatusEffects)
            applyReactiveOnHitStatusEffects?.Invoke();

        if (skillPlan.StatusEffectTriggers == null)
            return;

        foreach (EnemySkill_SO.DiceEffectTrigger trigger in skillPlan.StatusEffectTriggers)
            applyStatusTrigger?.Invoke(trigger);
    }
}
