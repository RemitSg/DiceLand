using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

public sealed partial class RelicExecutionContext
{
    internal void AddPlayerDiceModifier(Relic_SO relic, int min = 0, int max = 0, int power = 0, int targetMin = 0, bool clashOnlyPower = false)
    {
        additionalMinPowerBonus += min;
        additionalMaxPowerBonus += max;
        additionalPowerBonus += power;
        additionalTargetMinPowerBonus += targetMin;
        if (previewDiceBreakdown == null)
            return;

        string name = relic.LocalizedName;
        if (previewDiceForTarget)
        {
            previewDiceBreakdown.Add(name, relic.icon, targetMin, 0);
            return;
        }

        int sharedPower = clashOnlyPower ? 0 : power;
        previewDiceBreakdown.Add(name, relic.icon, min + sharedPower, max + sharedPower);
        if (clashOnlyPower)
            previewDiceBreakdown.Add(name, relic.icon, power, power, clashOnly: true);
    }

    public static RelicExecutionContext CreatePlayerDicePrepared(
        RunState runState,
        EnemySkill_SO skillData,
        int diceIndex,
        bool isClashPreparation,
        Object source,
        Object target)
    {
        return new RelicExecutionContext
        {
            trigger = Relic_SO.RelicTrigger.OnPlayerDicePrepared,
            runState = runState,
            sourceIsPlayer = true,
            source = source,
            target = target,
            skillData = skillData,
            diceIndex = diceIndex,
            isClashPreparation = isClashPreparation,
            feedbackMessages = new List<string>()
        };
    }

    public static RelicExecutionContext CreatePlayerDefenseClashWin(
        RunState runState,
        EnemySkill_SO skillData,
        int diceIndex,
        int currentRound,
        Object source,
        Object target)
    {
        return new RelicExecutionContext
        {
            trigger = Relic_SO.RelicTrigger.OnPlayerDefenseClashWin,
            runState = runState,
            sourceIsPlayer = true,
            source = source,
            target = target,
            skillData = skillData,
            diceIndex = diceIndex,
            currentRound = Mathf.Max(1, currentRound),
            feedbackMessages = new List<string>()
        };
    }
}
