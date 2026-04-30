public static class BattleEnemyStageTransitionEffectService
{
    public static bool ShouldProcess(
        EnemySkill_SO.EnemyStageTransitionEffectEntry effect,
        EnemySkill_SO.DiceEffectTrigger trigger)
    {
        return effect.trigger == trigger
            && effect.targetMode == EnemySkill_SO.EnemyStageTransitionTargetMode.Self;
    }

    public static bool ShouldApplyNextRoundStart(EnemySkill_SO.EnemyStageTransitionEffectEntry effect)
    {
        return effect.applyTiming == EnemySkill_SO.EnemyStageTransitionApplyTiming.NextRoundStart;
    }
}
