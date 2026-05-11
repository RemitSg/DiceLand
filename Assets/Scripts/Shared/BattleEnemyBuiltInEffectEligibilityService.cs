public static class BattleEnemyBuiltInEffectEligibilityService
{
    public static bool ShouldProcess(
        EnemySkill_SO.BuiltInEffectEntry effect,
        EnemySkill_SO.DiceEffectTrigger trigger)
    {
        if (effect.trigger != trigger)
            return false;

        switch (effect.effectType)
        {
            case EnemySkill_SO.BuiltInEffectType.Heal:
            case EnemySkill_SO.BuiltInEffectType.Damage:
                return effect.amount > 0;
            case EnemySkill_SO.BuiltInEffectType.ApplyStatus:
                return effect.statusStackAmount > 0;
            case EnemySkill_SO.BuiltInEffectType.TransferRandomNegativeStatus:
                return effect.amount > 0 || effect.fallbackAmount > 0;
            case EnemySkill_SO.BuiltInEffectType.QueueNextDicePowerBonus:
                return effect.amount != 0;
            case EnemySkill_SO.BuiltInEffectType.DamageByTargetStatusStacksDivided:
                return effect.amount > 0;
            default:
                return false;
        }
    }
}
