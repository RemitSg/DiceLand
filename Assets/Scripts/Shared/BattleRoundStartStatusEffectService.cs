using System;
using System.Collections.Generic;

public static class BattleRoundStartStatusEffectService
{
    public static void Dispatch<TAlly, TEnemy>(
        IEnumerable<TAlly> allies,
        IEnumerable<TEnemy> enemies,
        Func<TAlly, bool> shouldTickAlly,
        Action<TAlly> tickAlly,
        Func<TEnemy, bool> shouldTickEnemy,
        Action<TEnemy> tickEnemy,
        Func<TEnemy, bool> shouldAttemptEnemyTransition,
        Action<TEnemy> transitionEnemy)
    {
        if (allies != null)
        {
            foreach (TAlly ally in allies)
            {
                if (shouldTickAlly != null && !shouldTickAlly(ally))
                    continue;

                tickAlly?.Invoke(ally);
            }
        }

        if (enemies == null)
            return;

        foreach (TEnemy enemy in enemies)
        {
            if (shouldTickEnemy != null && !shouldTickEnemy(enemy))
                continue;

            tickEnemy?.Invoke(enemy);
        }

        foreach (TEnemy enemy in enemies)
        {
            if (shouldAttemptEnemyTransition != null && !shouldAttemptEnemyTransition(enemy))
                continue;

            transitionEnemy?.Invoke(enemy);
        }
    }
}
