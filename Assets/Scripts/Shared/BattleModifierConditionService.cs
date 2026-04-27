using UnityEngine;

public static class BattleModifierConditionService
{
    public static bool IsConditionMet(
        EnemySkill_SO.PowerModifierCondition condition,
        int selfSpeed,
        int targetSpeed,
        int requiredSpeedValue = 0,
        int requiredSpeedDifference = 0)
    {
        switch (condition)
        {
            case EnemySkill_SO.PowerModifierCondition.SelfSpeedHigherThanTarget:
                return selfSpeed - targetSpeed >= Mathf.Max(1, requiredSpeedDifference);
            case EnemySkill_SO.PowerModifierCondition.SelfSpeedLowerThanTarget:
                return targetSpeed - selfSpeed >= Mathf.Max(1, requiredSpeedDifference);
            case EnemySkill_SO.PowerModifierCondition.SelfSpeedEqualToTarget:
                return selfSpeed == targetSpeed;
            case EnemySkill_SO.PowerModifierCondition.SelfSpeedHigherThanValue:
                return selfSpeed >= requiredSpeedValue;
            case EnemySkill_SO.PowerModifierCondition.SelfSpeedLowerThanValue:
                return selfSpeed <= requiredSpeedValue;
            default:
                return false;
        }
    }
}
