using UnityEngine;

public enum BattleClashOutcome
{
    AttackTie,
    AllyAttackWins,
    EnemyAttackWins,
    AllyBreaksEnemyDefense,
    EnemyDefenseWins,
    EnemyDefenseTies,
    EnemyBreaksAllyDefense,
    AllyDefenseWins,
    AllyDefenseTies,
    BothDefenseAllyWins,
    BothDefenseEnemyWins,
    BothDefenseTie
}

public struct BattleClashResult
{
    public BattleClashOutcome Outcome;
    public bool HasPresentationWinner;
    public bool AllyWonPresentation;
    public bool AllyTriggersClashWin;
    public bool EnemyTriggersClashWin;
    public int AllyHitPower;
    public int EnemyHitPower;
    public int PresentationHitPower;
    public int ClashDifference;
    public int DefenseEnergyRestoreAmount;
}

public static class BattleClashResultService
{
    public static BattleClashResult Resolve(
        int allyPower,
        int enemyPower,
        bool allyDefenseSkill,
        bool enemyDefenseSkill,
        int allyFollowUpBasePower,
        int enemyFollowUpBasePower,
        int allyWinAttackBonus,
        int enemyWinAttackBonus)
    {
        if (!allyDefenseSkill && !enemyDefenseSkill)
            return ResolveAttackVsAttack(allyPower, enemyPower, allyFollowUpBasePower, enemyFollowUpBasePower, allyWinAttackBonus, enemyWinAttackBonus);

        if (!allyDefenseSkill && enemyDefenseSkill)
            return ResolveAllyAttackVsEnemyDefense(allyPower, enemyPower, allyFollowUpBasePower, enemyFollowUpBasePower);

        if (allyDefenseSkill && !enemyDefenseSkill)
            return ResolveAllyDefenseVsEnemyAttack(allyPower, enemyPower, allyFollowUpBasePower, enemyFollowUpBasePower);

        return ResolveBothDefense(allyPower, enemyPower);
    }

    private static BattleClashResult ResolveAttackVsAttack(
        int allyPower,
        int enemyPower,
        int allyFollowUpBasePower,
        int enemyFollowUpBasePower,
        int allyWinAttackBonus,
        int enemyWinAttackBonus)
    {
        int allyHitPower = Mathf.Max(1, allyFollowUpBasePower + allyWinAttackBonus);
        int enemyHitPower = Mathf.Max(1, enemyFollowUpBasePower + enemyWinAttackBonus);

        if (allyPower > enemyPower)
        {
            return new BattleClashResult
            {
                Outcome = BattleClashOutcome.AllyAttackWins,
                HasPresentationWinner = true,
                AllyWonPresentation = true,
                AllyTriggersClashWin = true,
                AllyHitPower = allyHitPower,
                EnemyHitPower = enemyHitPower,
                PresentationHitPower = allyHitPower,
                ClashDifference = allyPower - enemyPower
            };
        }

        if (enemyPower > allyPower)
        {
            return new BattleClashResult
            {
                Outcome = BattleClashOutcome.EnemyAttackWins,
                HasPresentationWinner = true,
                AllyWonPresentation = false,
                EnemyTriggersClashWin = true,
                AllyHitPower = allyHitPower,
                EnemyHitPower = enemyHitPower,
                PresentationHitPower = enemyHitPower,
                ClashDifference = enemyPower - allyPower
            };
        }

        return new BattleClashResult
        {
            Outcome = BattleClashOutcome.AttackTie,
            AllyHitPower = Mathf.Max(1, allyFollowUpBasePower),
            EnemyHitPower = Mathf.Max(1, enemyFollowUpBasePower),
            PresentationHitPower = Mathf.Max(1, enemyFollowUpBasePower)
        };
    }

    private static BattleClashResult ResolveAllyAttackVsEnemyDefense(
        int allyPower,
        int enemyPower,
        int allyFollowUpBasePower,
        int enemyFollowUpBasePower)
    {
        int clashDifference = Mathf.Abs(allyPower - enemyPower);
        if (allyPower > enemyPower)
        {
            int hitPower = Mathf.Max(1, allyFollowUpBasePower - enemyFollowUpBasePower);
            return new BattleClashResult
            {
                Outcome = BattleClashOutcome.AllyBreaksEnemyDefense,
                HasPresentationWinner = true,
                AllyWonPresentation = true,
                AllyTriggersClashWin = true,
                AllyHitPower = hitPower,
                PresentationHitPower = hitPower,
                ClashDifference = clashDifference
            };
        }

        return new BattleClashResult
        {
            Outcome = enemyPower > allyPower ? BattleClashOutcome.EnemyDefenseWins : BattleClashOutcome.EnemyDefenseTies,
            EnemyTriggersClashWin = enemyPower > allyPower,
            ClashDifference = clashDifference
        };
    }

    private static BattleClashResult ResolveAllyDefenseVsEnemyAttack(
        int allyPower,
        int enemyPower,
        int allyFollowUpBasePower,
        int enemyFollowUpBasePower)
    {
        int clashDifference = Mathf.Abs(allyPower - enemyPower);
        if (enemyPower > allyPower)
        {
            int hitPower = Mathf.Max(1, enemyFollowUpBasePower - allyFollowUpBasePower);
            return new BattleClashResult
            {
                Outcome = BattleClashOutcome.EnemyBreaksAllyDefense,
                HasPresentationWinner = true,
                AllyWonPresentation = false,
                EnemyTriggersClashWin = true,
                EnemyHitPower = hitPower,
                PresentationHitPower = hitPower,
                ClashDifference = clashDifference
            };
        }

        return new BattleClashResult
        {
            Outcome = allyPower > enemyPower ? BattleClashOutcome.AllyDefenseWins : BattleClashOutcome.AllyDefenseTies,
            AllyTriggersClashWin = allyPower > enemyPower,
            ClashDifference = clashDifference,
            DefenseEnergyRestoreAmount = allyPower > enemyPower ? allyPower - enemyPower : 0
        };
    }

    private static BattleClashResult ResolveBothDefense(int allyPower, int enemyPower)
    {
        if (allyPower > enemyPower)
        {
            return new BattleClashResult
            {
                Outcome = BattleClashOutcome.BothDefenseAllyWins,
                AllyTriggersClashWin = true,
                ClashDifference = allyPower - enemyPower
            };
        }

        if (enemyPower > allyPower)
        {
            return new BattleClashResult
            {
                Outcome = BattleClashOutcome.BothDefenseEnemyWins,
                EnemyTriggersClashWin = true,
                ClashDifference = enemyPower - allyPower
            };
        }

        return new BattleClashResult
        {
            Outcome = BattleClashOutcome.BothDefenseTie
        };
    }
}
