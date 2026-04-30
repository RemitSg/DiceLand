public struct BattleClashImpactPlan
{
    public bool ApplyAllyHit;
    public bool ApplyEnemyHit;
    public bool ApplyAllyMiss;
    public bool ApplyEnemyMiss;
    public bool ApplyHornedGatekeeperClashWin;
    public bool ApplyPlayerDefenseClashWinRelics;
    public bool ShowZeroDamageOnAlly;
    public bool ShowZeroDamageOnEnemy;
    public bool RestoreAllyDefenseEnergy;
    public int AllyHitPower;
    public int EnemyHitPower;
    public int AllyHitPowerModifier;
    public int EnemyHitPowerModifier;
    public int HornedGatekeeperClashDifference;
    public int AllyZeroDamagePowerModifier;
    public int EnemyZeroDamagePowerModifier;
    public int DefenseEnergyRestoreAmount;
}

public static class BattleClashImpactPlanService
{
    public static BattleClashImpactPlan Resolve(
        BattleClashResult clashResult,
        int allyPower,
        int enemyPower,
        int allyBonusPower,
        int enemyBonusPower,
        int allySkillUseBonus,
        int enemySkillUseBonus,
        int allyWinAttackBonus,
        int enemyWinAttackBonus)
    {
        switch (clashResult.Outcome)
        {
            case BattleClashOutcome.AllyAttackWins:
                return new BattleClashImpactPlan
                {
                    ApplyAllyHit = true,
                    ApplyEnemyMiss = true,
                    AllyHitPower = clashResult.AllyHitPower,
                    AllyHitPowerModifier = allySkillUseBonus + allyWinAttackBonus
                };

            case BattleClashOutcome.EnemyAttackWins:
                return new BattleClashImpactPlan
                {
                    ApplyEnemyHit = true,
                    ApplyAllyMiss = true,
                    ApplyHornedGatekeeperClashWin = true,
                    EnemyHitPower = clashResult.EnemyHitPower,
                    EnemyHitPowerModifier = enemySkillUseBonus + enemyWinAttackBonus,
                    HornedGatekeeperClashDifference = clashResult.EnemyHitPower
                };

            case BattleClashOutcome.AttackTie:
                return new BattleClashImpactPlan
                {
                    ApplyAllyMiss = true,
                    ApplyEnemyMiss = true
                };

            case BattleClashOutcome.AllyBreaksEnemyDefense:
                return new BattleClashImpactPlan
                {
                    ApplyAllyHit = clashResult.AllyHitPower > 0,
                    AllyHitPower = clashResult.AllyHitPower,
                    AllyHitPowerModifier = allySkillUseBonus
                };

            case BattleClashOutcome.EnemyDefenseWins:
                return new BattleClashImpactPlan
                {
                    ApplyHornedGatekeeperClashWin = true,
                    ShowZeroDamageOnEnemy = true,
                    HornedGatekeeperClashDifference = enemyPower - allyPower,
                    EnemyZeroDamagePowerModifier = allyBonusPower
                };

            case BattleClashOutcome.EnemyDefenseTies:
                return new BattleClashImpactPlan
                {
                    ShowZeroDamageOnEnemy = true,
                    EnemyZeroDamagePowerModifier = allyBonusPower
                };

            case BattleClashOutcome.EnemyBreaksAllyDefense:
                return new BattleClashImpactPlan
                {
                    ApplyEnemyHit = clashResult.EnemyHitPower > 0,
                    ApplyHornedGatekeeperClashWin = clashResult.EnemyHitPower > 0,
                    EnemyHitPower = clashResult.EnemyHitPower,
                    EnemyHitPowerModifier = enemySkillUseBonus,
                    HornedGatekeeperClashDifference = clashResult.EnemyHitPower
                };

            case BattleClashOutcome.AllyDefenseWins:
                return new BattleClashImpactPlan
                {
                    ApplyPlayerDefenseClashWinRelics = true,
                    ShowZeroDamageOnAlly = true,
                    RestoreAllyDefenseEnergy = true,
                    AllyZeroDamagePowerModifier = enemyBonusPower,
                    DefenseEnergyRestoreAmount = clashResult.DefenseEnergyRestoreAmount
                };

            case BattleClashOutcome.AllyDefenseTies:
                return new BattleClashImpactPlan
                {
                    ShowZeroDamageOnAlly = true,
                    AllyZeroDamagePowerModifier = enemyBonusPower
                };

            case BattleClashOutcome.BothDefenseAllyWins:
                return new BattleClashImpactPlan
                {
                    ApplyPlayerDefenseClashWinRelics = true
                };

            case BattleClashOutcome.BothDefenseEnemyWins:
            case BattleClashOutcome.BothDefenseTie:
            default:
                return new BattleClashImpactPlan();
        }
    }
}
