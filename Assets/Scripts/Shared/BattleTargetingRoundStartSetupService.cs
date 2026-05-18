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
        int currentRound,
        IReadOnlyList<BattleAllyController> opposingAllies = null)
    {
        List<string> logs = new List<string>();
        if (enemies == null)
            return logs;

        int previousAllyDiceTotal = CountPreviousRoundSpeedDice(opposingAllies);
        List<EnemySkill_SO> previousAllySkills = CollectPreviousRoundSkills(opposingAllies);
        for (int enemyIndex = 0; enemyIndex < enemies.Count; enemyIndex++)
        {
            BattleEnemyController enemy = enemies[enemyIndex];
            if (enemy == null || enemy.IsDead)
                continue;

            ApplyReflectionSpeedButtonPassive(enemy, previousAllyDiceTotal, currentRound);
            enemy.GenerateRandomSpeed();
            enemy.SelectRandomSkills(currentRound);
            ApplyReflectionSkillPassive(enemy, previousAllySkills);
            logs.Add(BattleTargetingPhaseLogService.BuildEnemySpeedLog(
                enemy.GetDebugDisplayName(),
                enemy.Speed,
                enemy.CurrentHP,
                enemy.MaxHP));
        }

        return logs;
    }

    private static void ApplyReflectionSpeedButtonPassive(BattleEnemyController enemy, int previousAllyDiceTotal, int currentRound)
    {
        if (enemy == null || enemy.enemyData == null || !enemy.enemyData.copyOpponentLastRoundSpeedDiceCount)
            return;

        if (currentRound <= 1)
        {
            enemy.SetTemporarySpeedButtonCountOverride(-1);
            return;
        }

        enemy.SetTemporarySpeedButtonCountOverride(Math.Max(1, previousAllyDiceTotal));
    }

    private static void ApplyReflectionSkillPassive(BattleEnemyController enemy, IReadOnlyList<EnemySkill_SO> previousAllySkills)
    {
        if (enemy == null
            || enemy.enemyData == null
            || !enemy.enemyData.copyOpponentLastRoundSkills
            || enemy.enemyData.reflectionSkillTemplate == null)
        {
            return;
        }

        List<EnemySkill_SO> copies = BattleReflectionSkillCopyService.CreateRuntimeCopies(
            enemy.enemyData.reflectionSkillTemplate,
            previousAllySkills,
            Math.Max(1, enemy.SpeedButtons.Count));
        enemy.SetRuntimeCopiedSkills(copies);
    }

    private static int CountPreviousRoundSpeedDice(IReadOnlyList<BattleAllyController> allies)
    {
        if (allies == null)
            return 0;

        int count = 0;
        for (int i = 0; i < allies.Count; i++)
        {
            BattleAllyController ally = allies[i];
            if (ally == null || ally.IsDead)
                continue;

            count += Math.Max(0, ally.LastRoundSpeedDiceCount);
        }

        return count;
    }

    private static List<EnemySkill_SO> CollectPreviousRoundSkills(IReadOnlyList<BattleAllyController> allies)
    {
        List<EnemySkill_SO> skills = new List<EnemySkill_SO>();
        if (allies == null)
            return skills;

        for (int i = 0; i < allies.Count; i++)
        {
            BattleAllyController ally = allies[i];
            if (ally == null || ally.IsDead)
                continue;

            IReadOnlyList<EnemySkill_SO> lastRoundSkills = ally.LastRoundUsedSkills;
            if (lastRoundSkills != null && lastRoundSkills.Count > 0)
            {
                for (int skillIndex = 0; skillIndex < lastRoundSkills.Count; skillIndex++)
                {
                    if (lastRoundSkills[skillIndex] != null)
                        skills.Add(lastRoundSkills[skillIndex]);
                }
            }
            else if (ally.LastRoundUsedSkill != null)
            {
                skills.Add(ally.LastRoundUsedSkill);
            }
        }

        return skills;
    }
}
