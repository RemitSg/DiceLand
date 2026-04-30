using System;

public static class BattleAllyHitDamagePipelineDispatcher
{
    public static int Dispatch(
        int baseDamage,
        BattleAllyHitDamagePipelinePlan plan,
        Func<int, int> applyOutgoingRelics)
    {
        int damage = baseDamage;
        for (int i = 0; plan.Steps != null && i < plan.Steps.Length; i++)
        {
            switch (plan.Steps[i])
            {
                case BattleAllyHitDamagePipelineStep.ApplyOutgoingRelics:
                    damage = applyOutgoingRelics != null ? applyOutgoingRelics(damage) : damage;
                    break;
            }
        }

        return damage;
    }
}
