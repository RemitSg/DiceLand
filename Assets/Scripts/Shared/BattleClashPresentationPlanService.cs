public enum BattleClashHitFxSource
{
    Ally,
    Enemy
}

public enum BattleClashPostLogType
{
    AllyAttackWinFollowUp,
    EnemyAttackWinFollowUp,
    AttackTie,
    AllyAttackEnemyDefense,
    AllyDefenseEnemyAttack,
    BothDefense
}

public struct BattleClashPresentationPlan
{
    public BattleClashHitFxSource HitFxSource;
    public BattleClashPostLogType PostLogType;
    public bool AttackBreaksDefense;
}

public static class BattleClashPresentationPlanService
{
    public static BattleClashPresentationPlan Resolve(BattleClashResult clashResult)
    {
        switch (clashResult.Outcome)
        {
            case BattleClashOutcome.AllyAttackWins:
                return new BattleClashPresentationPlan
                {
                    HitFxSource = BattleClashHitFxSource.Ally,
                    PostLogType = BattleClashPostLogType.AllyAttackWinFollowUp
                };

            case BattleClashOutcome.EnemyAttackWins:
                return new BattleClashPresentationPlan
                {
                    HitFxSource = BattleClashHitFxSource.Enemy,
                    PostLogType = BattleClashPostLogType.EnemyAttackWinFollowUp
                };

            case BattleClashOutcome.AttackTie:
                return new BattleClashPresentationPlan
                {
                    HitFxSource = BattleClashHitFxSource.Ally,
                    PostLogType = BattleClashPostLogType.AttackTie
                };

            case BattleClashOutcome.AllyBreaksEnemyDefense:
                return new BattleClashPresentationPlan
                {
                    HitFxSource = BattleClashHitFxSource.Ally,
                    PostLogType = BattleClashPostLogType.AllyAttackEnemyDefense,
                    AttackBreaksDefense = true
                };

            case BattleClashOutcome.EnemyDefenseWins:
            case BattleClashOutcome.EnemyDefenseTies:
                return new BattleClashPresentationPlan
                {
                    HitFxSource = BattleClashHitFxSource.Ally,
                    PostLogType = BattleClashPostLogType.AllyAttackEnemyDefense
                };

            case BattleClashOutcome.EnemyBreaksAllyDefense:
                return new BattleClashPresentationPlan
                {
                    HitFxSource = BattleClashHitFxSource.Enemy,
                    PostLogType = BattleClashPostLogType.AllyDefenseEnemyAttack,
                    AttackBreaksDefense = true
                };

            case BattleClashOutcome.AllyDefenseWins:
            case BattleClashOutcome.AllyDefenseTies:
                return new BattleClashPresentationPlan
                {
                    HitFxSource = BattleClashHitFxSource.Enemy,
                    PostLogType = BattleClashPostLogType.AllyDefenseEnemyAttack
                };

            case BattleClashOutcome.BothDefenseAllyWins:
            case BattleClashOutcome.BothDefenseEnemyWins:
            case BattleClashOutcome.BothDefenseTie:
            default:
                return new BattleClashPresentationPlan
                {
                    HitFxSource = BattleClashHitFxSource.Ally,
                    PostLogType = BattleClashPostLogType.BothDefense
                };
        }
    }
}
