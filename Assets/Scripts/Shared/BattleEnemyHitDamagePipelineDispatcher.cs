using System;

public static class BattleEnemyHitDamagePipelineDispatcher
{
    public static int Dispatch(
        int baseDamage,
        BattleEnemyHitDamagePipelinePlan plan,
        Func<int, int> applyCalamityModifiers,
        Func<int, int> applyIncomingAttackRelics,
        Func<int, int> applyIncomingSingleAttackRelics)
    {
        int damage = baseDamage;
        for (int i = 0; plan.Steps != null && i < plan.Steps.Length; i++)
        {
            switch (plan.Steps[i])
            {
                case BattleEnemyHitDamagePipelineStep.ApplyCalamityModifiers:
                    damage = applyCalamityModifiers != null ? applyCalamityModifiers(damage) : damage;
                    break;
                case BattleEnemyHitDamagePipelineStep.ApplyIncomingAttackRelics:
                    damage = applyIncomingAttackRelics != null ? applyIncomingAttackRelics(damage) : damage;
                    break;
                case BattleEnemyHitDamagePipelineStep.ApplyIncomingSingleAttackRelics:
                    damage = applyIncomingSingleAttackRelics != null ? applyIncomingSingleAttackRelics(damage) : damage;
                    break;
            }
        }

        return damage;
    }
}
