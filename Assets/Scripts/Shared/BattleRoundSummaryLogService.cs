using System.Collections.Generic;

public static class BattleRoundSummaryLogService
{
    public static IReadOnlyList<string> BuildSummaryLines(
        BattleAllyController[] allies,
        BattleEnemyController[] enemies)
    {
        List<string> lines = new List<string>
        {
            "\u2500\u2500 \u56de\u5408\u7ed3\u7b97\u6c47\u603b \u2500\u2500"
        };

        if (allies != null)
        {
            foreach (BattleAllyController ally in allies)
            {
                if (ally != null && !ally.IsDead)
                    lines.Add($"\u53cb\u65b9 {ally.GetDebugDisplayName()} HP {ally.CurrentHP}/{ally.MaxHP}");
            }
        }

        if (enemies != null)
        {
            foreach (BattleEnemyController enemy in enemies)
            {
                if (enemy != null && !enemy.IsDead)
                    lines.Add($"\u654c\u65b9 {enemy.GetDebugDisplayName()} HP {enemy.CurrentHP}/{enemy.MaxHP}");
            }
        }

        return lines;
    }
}
