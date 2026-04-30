using System;
using System.Collections.Generic;

public static class BattleSpeedButtonListService
{
    public static IReadOnlyList<SpeedButton> GetAllyButtons(BattleAllyController ally)
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

    public static IReadOnlyList<SpeedButton> GetEnemyButtons(BattleEnemyController enemy)
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
}
