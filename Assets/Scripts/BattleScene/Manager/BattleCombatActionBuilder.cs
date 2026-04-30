using System;
using System.Collections.Generic;
using UnityEngine;

public enum BattleCombatActionType
{
    Clash,
    AllySingleAttack,
    EnemySingleAttack
}

public sealed class BattleCombatAction
{
    public BattleCombatActionType Type;
    public BattleAllyController Ally;
    public BattleEnemyController Enemy;
    public SpeedButton EnemyButton;
    public BattleAllyController TargetAlly;
    public int PrioritySpeed;
    public int TiePriority;
    public int SequenceIndex;
}

public static class BattleCombatActionBuilder
{
    public static List<BattleCombatAction> Build(
        BattleAllyController[] allies,
        BattleEnemyController[] enemies,
        Action<string> logSkippedAlly = null)
    {
        List<BattleCombatAction> actions = new List<BattleCombatAction>();
        HashSet<SpeedButton> clashEnemyButtons = new HashSet<SpeedButton>();
        int sequenceIndex = 0;

        actions.AddRange(BattleCombatActionCollectorService.CollectAllyActions(
            allies,
            clashEnemyButtons,
            ref sequenceIndex,
            logSkippedAlly));
        actions.AddRange(BattleCombatActionCollectorService.CollectEnemyActions(
            enemies,
            clashEnemyButtons,
            ref sequenceIndex));

        BattleCombatActionOrderingService.Sort(actions);
        return actions;
    }

    public static int Compare(BattleCombatAction a, BattleCombatAction b)
    {
        return BattleCombatActionOrderingService.Compare(a, b);
    }

}
