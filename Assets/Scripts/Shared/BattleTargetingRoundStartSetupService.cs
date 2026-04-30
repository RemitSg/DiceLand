using System;
using System.Collections.Generic;

public static class BattleTargetingRoundStartSetupService
{
    public static List<string> PrepareAllies(
        IReadOnlyList<BattleAllyController> allies,
        int speedMin,
        int speedMax,
        int currentRound,
        Action<BattleAllyController> applyRoundStartRelics)
    {
        List<string> logs = new List<string>();
        if (allies == null)
            return logs;

        for (int allyIndex = 0; allyIndex < allies.Count; allyIndex++)
        {
            BattleAllyController ally = allies[allyIndex];
            if (ally == null || ally.IsDead)
                continue;

            ally.GenerateRandomSpeed(speedMin, speedMax);
            applyRoundStartRelics?.Invoke(ally);
            ally.SelectDefaultSkill(currentRound);
            logs.Add(BattleTargetingPhaseLogService.BuildAllySpeedLog(
                ally.GetDebugDisplayName(),
                ally.Speed,
                ally.CurrentHP,
                ally.MaxHP));
        }

        return logs;
    }

    public static List<string> PrepareEnemies(
        IReadOnlyList<BattleEnemyController> enemies,
        int currentRound)
    {
        List<string> logs = new List<string>();
        if (enemies == null)
            return logs;

        for (int enemyIndex = 0; enemyIndex < enemies.Count; enemyIndex++)
        {
            BattleEnemyController enemy = enemies[enemyIndex];
            if (enemy == null || enemy.IsDead)
                continue;

            enemy.GenerateRandomSpeed();
            enemy.SelectRandomSkills(currentRound);
            logs.Add(BattleTargetingPhaseLogService.BuildEnemySpeedLog(
                enemy.GetDebugDisplayName(),
                enemy.Speed,
                enemy.CurrentHP,
                enemy.MaxHP));
        }

        return logs;
    }
}
