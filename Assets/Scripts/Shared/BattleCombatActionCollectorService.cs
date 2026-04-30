using System;
using System.Collections.Generic;
using UnityEngine;

public static class BattleCombatActionCollectorService
{
    public static List<BattleCombatAction> CollectAllyActions(
        IReadOnlyList<BattleAllyController> allies,
        HashSet<SpeedButton> clashEnemyButtons,
        ref int sequenceIndex,
        Action<string> logSkippedAlly = null)
    {
        List<BattleCombatAction> actions = new List<BattleCombatAction>();
        if (allies == null)
            return actions;

        if (clashEnemyButtons == null)
            clashEnemyButtons = new HashSet<SpeedButton>();

        for (int allyIndex = 0; allyIndex < allies.Count; allyIndex++)
        {
            BattleAllyController ally = allies[allyIndex];
            if (ally == null || ally.IsDead)
                continue;

            bool hasAnyAction = false;
            IReadOnlyList<SpeedButton> allyButtons = BattleSpeedButtonListService.GetAllyButtons(ally);
            for (int buttonIndex = 0; buttonIndex < allyButtons.Count; buttonIndex++)
            {
                SpeedButton allyButton = allyButtons[buttonIndex];
                if (allyButton == null)
                    continue;

                SpeedButton enemyButton = allyButton.ConnectedTarget;
                if (enemyButton == null)
                    continue;

                BattleEnemyController enemy = enemyButton.GetComponentInParent<BattleEnemyController>();
                if (enemy == null || enemy.IsDead)
                    continue;

                BattleCombatAction action = new BattleCombatAction
                {
                    Ally = ally,
                    Enemy = enemy,
                    EnemyButton = enemyButton,
                    SequenceIndex = sequenceIndex++
                };

                if (allyButton.IsContestedConnection && !clashEnemyButtons.Contains(enemyButton))
                {
                    action.Type = BattleCombatActionType.Clash;
                    action.PrioritySpeed = Mathf.Max(ally.Speed, enemy.Speed);
                    action.TiePriority = 0;
                    clashEnemyButtons.Add(enemyButton);
                }
                else
                {
                    action.Type = BattleCombatActionType.AllySingleAttack;
                    action.PrioritySpeed = ally.Speed;
                    action.TiePriority = 1;
                }

                actions.Add(action);
                hasAnyAction = true;
            }

            if (!hasAnyAction)
                logSkippedAlly?.Invoke($"{ally.GetDebugDisplayName()} \u672a\u6307\u5b9a\u76ee\u6807\uff0c\u672c\u56de\u5408\u8df3\u8fc7\u3002");
        }

        return actions;
    }

    public static List<BattleCombatAction> CollectEnemyActions(
        IReadOnlyList<BattleEnemyController> enemies,
        HashSet<SpeedButton> clashEnemyButtons,
        ref int sequenceIndex)
    {
        List<BattleCombatAction> actions = new List<BattleCombatAction>();
        if (enemies == null)
            return actions;

        if (clashEnemyButtons == null)
            clashEnemyButtons = new HashSet<SpeedButton>();

        for (int enemyIndex = 0; enemyIndex < enemies.Count; enemyIndex++)
        {
            BattleEnemyController enemy = enemies[enemyIndex];
            if (enemy == null || enemy.IsDead)
                continue;

            IReadOnlyList<SpeedButton> enemyButtons = BattleSpeedButtonListService.GetEnemyButtons(enemy);
            for (int buttonIndex = 0; buttonIndex < enemyButtons.Count; buttonIndex++)
            {
                SpeedButton enemyButton = enemyButtons[buttonIndex];
                if (enemyButton == null || clashEnemyButtons.Contains(enemyButton))
                    continue;

                SpeedButton targetAllyButton = enemyButton.ConnectedTarget;
                if (targetAllyButton == null)
                    continue;

                BattleAllyController targetAlly = targetAllyButton.GetComponentInParent<BattleAllyController>();
                if (targetAlly == null || targetAlly.IsDead)
                    continue;

                actions.Add(new BattleCombatAction
                {
                    Type = BattleCombatActionType.EnemySingleAttack,
                    Enemy = enemy,
                    EnemyButton = enemyButton,
                    TargetAlly = targetAlly,
                    PrioritySpeed = enemy.Speed,
                    TiePriority = 1,
                    SequenceIndex = sequenceIndex++
                });
            }
        }

        return actions;
    }

}
