public struct BattleSuccessfulHitSkillEffectPlan
{
    public EnemySkill_SO.DiceEffectTrigger[] ImmediateTriggers;
    public bool ApplyReactiveOnHitStatusEffects;
    public EnemySkill_SO.DiceEffectTrigger[] StatusEffectTriggers;
}

public static class BattleSuccessfulHitSkillEffectPlanService
{
    public static BattleSuccessfulHitSkillEffectPlan Resolve(bool isSingleAttack)
    {
        EnemySkill_SO.DiceEffectTrigger[] triggers = isSingleAttack
            ? new[]
            {
                EnemySkill_SO.DiceEffectTrigger.OnSingleAttack,
                EnemySkill_SO.DiceEffectTrigger.OnClashWin,
                EnemySkill_SO.DiceEffectTrigger.OnHit
            }
            : new[]
            {
                EnemySkill_SO.DiceEffectTrigger.OnClashWin,
                EnemySkill_SO.DiceEffectTrigger.OnHit
            };

        return new BattleSuccessfulHitSkillEffectPlan
        {
            ImmediateTriggers = triggers,
            ApplyReactiveOnHitStatusEffects = true,
            StatusEffectTriggers = triggers
        };
    }
}
