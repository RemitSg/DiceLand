using UnityEngine;

public static class BattleDamageModifierContextFactory
{
    public static RelicExecutionContext CreateIncomingAttack(
        EnemySkill_SO skillData,
        int diceIndex,
        int incomingDamage,
        int sourceSpeed,
        int targetSpeed,
        Object source,
        Object target)
    {
        return RelicExecutionContext.CreatePlayerAboutToTakeAttackDamage(
            null,
            skillData,
            diceIndex,
            incomingDamage,
            sourceSpeed,
            targetSpeed,
            source,
            target);
    }

    public static RelicExecutionContext CreateIncomingSingleAttack(
        EnemySkill_SO skillData,
        int diceIndex,
        int incomingDamage,
        int sourceSpeed,
        int targetSpeed,
        Object source,
        Object target)
    {
        return RelicExecutionContext.CreatePlayerAboutToTakeSingleAttackDamage(
            null,
            skillData,
            diceIndex,
            incomingDamage,
            sourceSpeed,
            targetSpeed,
            source,
            target);
    }

    public static RelicExecutionContext CreateOutgoing(
        EnemySkill_SO skillData,
        int diceIndex,
        int outgoingDamage,
        Object source,
        Object target)
    {
        return RelicExecutionContext.CreatePlayerAboutToDealDamage(
            null,
            skillData,
            diceIndex,
            outgoingDamage,
            source,
            target);
    }
}
