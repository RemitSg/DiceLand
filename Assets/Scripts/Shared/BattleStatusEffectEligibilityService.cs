public static class BattleStatusEffectEligibilityService
{
    public static bool ShouldProcess(
        EnemySkill_SO.StatusEffectEntry effect,
        EnemySkill_SO.DiceEffectTrigger trigger,
        bool conditionalOnly)
    {
        return effect.trigger == trigger
            && effect.stackAmount > 0
            && effect.useCondition == conditionalOnly;
    }
}
