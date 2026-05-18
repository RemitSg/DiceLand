using UnityEngine;

public static class BattleSummonEffectService
{
    public static bool ShouldProcess(
        EnemySkill_SO.SummonEnemyEffectEntry effect,
        EnemySkill_SO.DiceEffectTrigger trigger)
    {
        return effect.trigger == trigger && effect.enemyData != null;
    }

    public static int NormalizeSummonCount(int summonCount)
    {
        return Mathf.Max(1, summonCount);
    }

    public static string BuildLabel(EnemySkill_SO.SummonEnemyEffectEntry effect, int effectIndex)
    {
        if (!string.IsNullOrWhiteSpace(effect.effectName))
            return effect.effectName;
        if (effect.enemyData != null && !string.IsNullOrWhiteSpace(effect.enemyData.enemyName))
            return effect.enemyData.enemyName;
        return $"閸欘剙鏁滈弫鍫熺亯#{effectIndex + 1}";
    }

    public static BattleEnemyController FindNewlySpawnedEnemy(
        BattleEnemyController[] beforeSpawn,
        BattleEnemyController[] afterSpawn,
        EnemyData_SO enemyData)
    {
        if (afterSpawn == null || enemyData == null)
            return null;

        for (int i = afterSpawn.Length - 1; i >= 0; i--)
        {
            BattleEnemyController candidate = afterSpawn[i];
            if (candidate == null || candidate.IsDead || candidate.enemyData != enemyData)
                continue;

            if (!ContainsEnemy(beforeSpawn, candidate))
                return candidate;
        }

        return null;
    }

    private static bool ContainsEnemy(BattleEnemyController[] enemies, BattleEnemyController target)
    {
        if (enemies == null || target == null)
            return false;

        for (int i = 0; i < enemies.Length; i++)
        {
            if (enemies[i] == target)
                return true;
        }

        return false;
    }
}
