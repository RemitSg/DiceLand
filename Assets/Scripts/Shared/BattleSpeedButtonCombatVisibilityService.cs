using System.Collections.Generic;

public static class BattleSpeedButtonCombatVisibilityService
{
    public static void ApplyCombatVisibility(
        BattleAllyController[] allies,
        BattleEnemyController[] enemies,
        bool disableAllyButtons,
        bool hideAllyButtons,
        bool hideEnemyButtons)
    {
        if (hideAllyButtons || disableAllyButtons)
            ApplyAllyCombatVisibility(allies, disableAllyButtons, hideAllyButtons);

        if (hideEnemyButtons)
            ApplyEnemyCombatVisibility(enemies);
    }

    private static void ApplyAllyCombatVisibility(
        BattleAllyController[] allies,
        bool disableButtons,
        bool hideButtons)
    {
        if (allies == null)
            return;

        foreach (BattleAllyController ally in allies)
        {
            IReadOnlyList<SpeedButton> buttons = ally != null ? ally.SpeedButtons : null;
            if (buttons == null || buttons.Count == 0)
                continue;

            for (int i = 0; i < buttons.Count; i++)
            {
                SpeedButton button = buttons[i];
                if (button == null)
                    continue;

                if (disableButtons)
                    button.IsInteractable = false;
                if (hideButtons)
                    button.HideForCombat();
            }
        }
    }

    private static void ApplyEnemyCombatVisibility(BattleEnemyController[] enemies)
    {
        if (enemies == null)
            return;

        foreach (BattleEnemyController enemy in enemies)
        {
            IReadOnlyList<SpeedButton> buttons = enemy != null ? enemy.SpeedButtons : null;
            if (buttons == null || buttons.Count == 0)
                continue;

            for (int i = 0; i < buttons.Count; i++)
            {
                SpeedButton button = buttons[i];
                if (button != null)
                    button.HideForCombat();
            }
        }
    }
}
