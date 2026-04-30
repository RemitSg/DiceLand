using System;
using System.Collections.Generic;

public static class BattleSharedEnergyListenerBindingService
{
    public static void Bind(
        IReadOnlyList<BattleAllyController> allies,
        IReadOnlyList<BattleEnemyController> enemies,
        Action<BattleAllyController> allySelectedSkillChanged,
        Action<BattleAllyController> allyStatusChanged,
        Action<SpeedButton> allyConnectionChanged,
        Action<BattleEnemyController> enemyStatusChanged)
    {
        BindAllies(allies, allySelectedSkillChanged, allyStatusChanged, allyConnectionChanged);
        BindEnemies(enemies, enemyStatusChanged);
    }

    public static void Unbind(
        IReadOnlyList<BattleAllyController> allies,
        IReadOnlyList<BattleEnemyController> enemies,
        Action<BattleAllyController> allySelectedSkillChanged,
        Action<BattleAllyController> allyStatusChanged,
        Action<SpeedButton> allyConnectionChanged,
        Action<BattleEnemyController> enemyStatusChanged)
    {
        UnbindAllies(allies, allySelectedSkillChanged, allyStatusChanged, allyConnectionChanged);
        UnbindEnemies(enemies, enemyStatusChanged);
    }

    private static void BindAllies(
        IReadOnlyList<BattleAllyController> allies,
        Action<BattleAllyController> selectedSkillChanged,
        Action<BattleAllyController> statusChanged,
        Action<SpeedButton> connectionChanged)
    {
        if (allies == null)
            return;

        for (int i = 0; i < allies.Count; i++)
        {
            BattleAllyController ally = allies[i];
            if (ally == null)
                continue;

            if (selectedSkillChanged != null)
            {
                ally.SelectedSkillChanged -= selectedSkillChanged;
                ally.SelectedSkillChanged += selectedSkillChanged;
            }

            if (statusChanged != null)
            {
                ally.StatusChanged -= statusChanged;
                ally.StatusChanged += statusChanged;
            }

            BindAllyButtonConnections(ally, connectionChanged);
        }
    }

    private static void UnbindAllies(
        IReadOnlyList<BattleAllyController> allies,
        Action<BattleAllyController> selectedSkillChanged,
        Action<BattleAllyController> statusChanged,
        Action<SpeedButton> connectionChanged)
    {
        if (allies == null)
            return;

        for (int i = 0; i < allies.Count; i++)
        {
            BattleAllyController ally = allies[i];
            if (ally == null)
                continue;

            if (selectedSkillChanged != null)
                ally.SelectedSkillChanged -= selectedSkillChanged;
            if (statusChanged != null)
                ally.StatusChanged -= statusChanged;

            UnbindAllyButtonConnections(ally, connectionChanged);
        }
    }

    private static void BindAllyButtonConnections(BattleAllyController ally, Action<SpeedButton> connectionChanged)
    {
        if (connectionChanged == null)
            return;

        IReadOnlyList<SpeedButton> allyButtons = BattleSpeedButtonListService.GetAllyButtons(ally);
        for (int i = 0; i < allyButtons.Count; i++)
        {
            SpeedButton allyButton = allyButtons[i];
            if (allyButton == null)
                continue;

            allyButton.ConnectionChanged -= connectionChanged;
            allyButton.ConnectionChanged += connectionChanged;
        }
    }

    private static void UnbindAllyButtonConnections(BattleAllyController ally, Action<SpeedButton> connectionChanged)
    {
        if (connectionChanged == null)
            return;

        IReadOnlyList<SpeedButton> allyButtons = BattleSpeedButtonListService.GetAllyButtons(ally);
        for (int i = 0; i < allyButtons.Count; i++)
        {
            SpeedButton allyButton = allyButtons[i];
            if (allyButton != null)
                allyButton.ConnectionChanged -= connectionChanged;
        }
    }

    private static void BindEnemies(IReadOnlyList<BattleEnemyController> enemies, Action<BattleEnemyController> statusChanged)
    {
        if (enemies == null || statusChanged == null)
            return;

        for (int i = 0; i < enemies.Count; i++)
        {
            BattleEnemyController enemy = enemies[i];
            if (enemy == null)
                continue;

            enemy.StatusChanged -= statusChanged;
            enemy.StatusChanged += statusChanged;
        }
    }

    private static void UnbindEnemies(IReadOnlyList<BattleEnemyController> enemies, Action<BattleEnemyController> statusChanged)
    {
        if (enemies == null || statusChanged == null)
            return;

        for (int i = 0; i < enemies.Count; i++)
        {
            BattleEnemyController enemy = enemies[i];
            if (enemy != null)
                enemy.StatusChanged -= statusChanged;
        }
    }
}
