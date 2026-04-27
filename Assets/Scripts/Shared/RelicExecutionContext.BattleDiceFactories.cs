using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

public sealed partial class RelicExecutionContext
{
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
