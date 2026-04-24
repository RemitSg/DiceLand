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

        foreach (BattleAllyController ally in allies)
        {
            if (ally.IsDead)
                continue;

            bool hasAnyAction = false;
            IReadOnlyList<SpeedButton> allyButtons = GetAllySpeedButtons(ally);
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

        foreach (BattleEnemyController enemy in enemies)
        {
            if (enemy.IsDead)
                continue;

            IReadOnlyList<SpeedButton> enemyButtons = GetEnemySpeedButtons(enemy);
            for (int i = 0; i < enemyButtons.Count; i++)
            {
                SpeedButton enemyButton = enemyButtons[i];
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

        actions.Sort(Compare);
        return actions;
    }

    public static int Compare(BattleCombatAction a, BattleCombatAction b)
    {
        int speedCompare = b.PrioritySpeed.CompareTo(a.PrioritySpeed);
        if (speedCompare != 0)
            return speedCompare;

        int tieCompare = a.TiePriority.CompareTo(b.TiePriority);
        if (tieCompare != 0)
            return tieCompare;

        return a.SequenceIndex.CompareTo(b.SequenceIndex);
    }

    private static IReadOnlyList<SpeedButton> GetEnemySpeedButtons(BattleEnemyController enemy)
    {
        if (enemy == null)
            return Array.Empty<SpeedButton>();

        IReadOnlyList<SpeedButton> buttons = enemy.SpeedButtons;
        if (buttons != null && buttons.Count > 0)
            return buttons;

        return enemy.speedButton != null
            ? new[] { enemy.speedButton }
            : Array.Empty<SpeedButton>();
    }

    private static IReadOnlyList<SpeedButton> GetAllySpeedButtons(BattleAllyController ally)
    {
        if (ally == null)
            return Array.Empty<SpeedButton>();

        IReadOnlyList<SpeedButton> buttons = ally.SpeedButtons;
        if (buttons != null && buttons.Count > 0)
            return buttons;

        return ally.speedButton != null
            ? new[] { ally.speedButton }
            : Array.Empty<SpeedButton>();
    }
}
