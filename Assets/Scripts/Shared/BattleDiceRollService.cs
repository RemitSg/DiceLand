using System;
using UnityEngine;

public static class BattleDiceRollService
{
    public static EnemySkill_SO.DicePowerRange ApplyRangeBonus(
        EnemySkill_SO.DicePowerRange baseRange,
        int minPowerBonus,
        int maxPowerBonus)
    {
        int minPower = Mathf.Max(1, baseRange.minPower + minPowerBonus);
        int maxPower = Mathf.Max(minPower, baseRange.maxPower + maxPowerBonus);
        return new EnemySkill_SO.DicePowerRange
        {
            minPower = minPower,
            maxPower = maxPower
        };
    }

    public static EnemySkill_SO.DicePowerRange NormalizeRange(EnemySkill_SO.DicePowerRange range)
    {
        int minPower = Mathf.Max(1, range.minPower);
        int maxPower = Mathf.Max(minPower, range.maxPower);
        return new EnemySkill_SO.DicePowerRange
        {
            minPower = minPower,
            maxPower = maxPower
        };
    }

    public static int RollPower(EnemySkill_SO.DicePowerRange range, Func<int, int, int> randomRange = null)
    {
        EnemySkill_SO.DicePowerRange normalized = NormalizeRange(range);
        Func<int, int, int> roll = randomRange ?? UnityEngine.Random.Range;
        return roll(normalized.minPower, normalized.maxPower + 1);
    }
}
