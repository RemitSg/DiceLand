public struct BattleSuccessfulHitRelicPlan
{
    public bool ApplyPlayerHitRelics;
    public bool ApplyEnemyKilledByPlayerRelics;
    public bool ApplyPlayerTookSingleAttackRelics;
}

public static class BattleSuccessfulHitRelicPlanService
{
    public static BattleSuccessfulHitRelicPlan ResolveAllyHit(bool targetIsDead)
    {
        return new BattleSuccessfulHitRelicPlan
        {
            ApplyPlayerHitRelics = true,
            ApplyEnemyKilledByPlayerRelics = targetIsDead,
            ApplyPlayerTookSingleAttackRelics = false
        };
    }

    public static BattleSuccessfulHitRelicPlan ResolveEnemyHit(bool isSingleAttack)
    {
        return new BattleSuccessfulHitRelicPlan
        {
            ApplyPlayerHitRelics = false,
            ApplyEnemyKilledByPlayerRelics = false,
            ApplyPlayerTookSingleAttackRelics = isSingleAttack
        };
    }
}
