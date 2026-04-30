using System.Collections.Generic;

public static class BattleTargetingEnemyAutoTargetService
{
    public static List<string> AutoConnectAndBuildLogEntries(
        IReadOnlyList<BattleEnemyController> enemies,
        List<SpeedButton> interactableAllyButtons)
    {
        if (enemies != null)
        {
            for (int i = 0; i < enemies.Count; i++)
            {
                BattleEnemyController enemy = enemies[i];
                if (enemy == null || enemy.IsDead)
                    continue;

                enemy.AutoConnectToRandomAlly(interactableAllyButtons);
            }
        }

        return BuildLogEntries(enemies);
    }

    public static List<string> BuildLogEntries(IReadOnlyList<BattleEnemyController> enemies)
    {
        List<string> logs = new List<string>();
        if (enemies == null)
            return logs;

        for (int enemyIndex = 0; enemyIndex < enemies.Count; enemyIndex++)
        {
            BattleEnemyController enemy = enemies[enemyIndex];
            if (enemy == null || enemy.IsDead)
                continue;

            IReadOnlyList<SpeedButton> enemyButtons = BattleSpeedButtonListService.GetEnemyButtons(enemy);
            for (int buttonIndex = 0; buttonIndex < enemyButtons.Count; buttonIndex++)
            {
                SpeedButton enemyButton = enemyButtons[buttonIndex];
                if (enemyButton == null || enemyButton.ConnectedTarget == null)
                    continue;

                BattleAllyController targetAlly = enemyButton.ConnectedTarget.GetComponentInParent<BattleAllyController>();
                string targetName = targetAlly != null ? targetAlly.GetDebugDisplayName() : enemyButton.ConnectedTarget.gameObject.name;
                string buttonLabel = enemyButtons.Count > 1 ? $" [按钮 {buttonIndex + 1}]" : string.Empty;
                logs.Add($"敌方 {enemy.GetDebugDisplayName()}{buttonLabel} 已锁定目标 -> {targetName} | 技能 {enemy.GetSelectedSkillDebugSummary(enemyButton)}");
            }
        }

        return logs;
    }
}
