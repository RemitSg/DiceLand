using UnityEngine;

public static class BattleHitRelicContextFactory
{
    public static RelicExecutionContext CreatePlayerTookSingleAttack(
        EnemySkill_SO skillData,
        int diceIndex,
        int dealtDamage,
        int sourceSpeed,
        int targetSpeed,
        int currentRound,
        string eventInstanceKey,
        Object source,
        Object target)
    {
        return RelicExecutionContext.CreatePlayerTookSingleAttackDamage(
            null,
            skillData,
            diceIndex,
            dealtDamage,
            sourceSpeed,
            targetSpeed,
            currentRound,
            eventInstanceKey,
            source,
            target);
    }

    public static RelicExecutionContext CreatePlayerHitEnemy(
        EnemySkill_SO skillData,
        int diceIndex,
        int dealtDamage,
        int sourceSpeed,
        int targetSpeed,
        int currentRound,
        string eventInstanceKey,
        Object source,
        Object target)
    {
        return RelicExecutionContext.CreatePlayerHitEnemy(
            null,
            skillData,
            diceIndex,
            dealtDamage,
            sourceSpeed,
            targetSpeed,
            currentRound,
            eventInstanceKey,
            source,
            target);
    }

    public static RelicExecutionContext CreateEnemyKilledByPlayer(
        EnemySkill_SO skillData,
        int diceIndex,
        int dealtDamage,
        Object source,
        Object target)
    {
        return RelicExecutionContext.CreateEnemyKilledByPlayer(
            null,
            skillData,
            diceIndex,
            dealtDamage,
            source,
            target);
    }

    public static RelicExecutionContext CreatePlayerDefenseClashWin(
        EnemySkill_SO skillData,
        int diceIndex,
        int currentRound,
        Object source,
        Object target)
    {
        return RelicExecutionContext.CreatePlayerDefenseClashWin(
            null,
            skillData,
            diceIndex,
            currentRound,
            source,
            target);
    }
}
