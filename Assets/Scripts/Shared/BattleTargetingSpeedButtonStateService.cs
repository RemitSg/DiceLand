using System.Collections.Generic;

public static class BattleTargetingSpeedButtonStateService
{
    public static void PrepareForTargeting(
        IReadOnlyList<BattleAllyController> allies,
        IReadOnlyList<BattleEnemyController> enemies)
    {
        PrepareAllyButtons(allies);
        PrepareEnemyButtons(enemies);
    }

    public static List<SpeedButton> EnableLivingAllyButtons(IReadOnlyList<BattleAllyController> allies)
    {
        List<SpeedButton> interactableButtons = new List<SpeedButton>();
        if (allies == null)
            return interactableButtons;

        for (int allyIndex = 0; allyIndex < allies.Count; allyIndex++)
        {
            BattleAllyController ally = allies[allyIndex];
            if (ally == null || ally.IsDead)
                continue;

            IReadOnlyList<SpeedButton> allyButtons = BattleSpeedButtonListService.GetAllyButtons(ally);
            for (int buttonIndex = 0; buttonIndex < allyButtons.Count; buttonIndex++)
            {
                SpeedButton allyButton = allyButtons[buttonIndex];
                if (allyButton == null)
                    continue;

                allyButton.IsInteractable = true;
                interactableButtons.Add(allyButton);
            }
        }

        return interactableButtons;
    }

    private static void PrepareAllyButtons(IReadOnlyList<BattleAllyController> allies)
    {
        if (allies == null)
            return;

        for (int allyIndex = 0; allyIndex < allies.Count; allyIndex++)
        {
            BattleAllyController ally = allies[allyIndex];
            if (ally == null)
                continue;

            IReadOnlyList<SpeedButton> allyButtons = BattleSpeedButtonListService.GetAllyButtons(ally);
            if (allyButtons.Count == 0)
                continue;

            for (int buttonIndex = 0; buttonIndex < allyButtons.Count; buttonIndex++)
            {
                SpeedButton allyButton = allyButtons[buttonIndex];
                if (allyButton == null)
                    continue;

                allyButton.ResetConnection();
                if (ally.IsDead)
                {
                    allyButton.HideForCombat();
                    allyButton.IsInteractable = false;
                }
                else
                {
                    allyButton.ShowForTargeting();
                }
            }
        }
    }

    private static void PrepareEnemyButtons(IReadOnlyList<BattleEnemyController> enemies)
    {
        if (enemies == null)
            return;

        for (int enemyIndex = 0; enemyIndex < enemies.Count; enemyIndex++)
        {
            BattleEnemyController enemy = enemies[enemyIndex];
            if (enemy == null)
                continue;

            IReadOnlyList<SpeedButton> enemyButtons = BattleSpeedButtonListService.GetEnemyButtons(enemy);
            if (enemyButtons.Count == 0)
                continue;

            for (int buttonIndex = 0; buttonIndex < enemyButtons.Count; buttonIndex++)
            {
                SpeedButton enemyButton = enemyButtons[buttonIndex];
                if (enemyButton == null)
                    continue;

                if (enemy.IsDead)
                {
                    enemyButton.HideForCombat();
                }
                else
                {
                    enemyButton.ShowForTargeting();
                    enemyButton.ResetConnection();
                }
            }
        }
    }
}
