using System.Collections.Generic;
using UnityEngine;

public static class BattleSummonEffectService
{
    private const float AnchorOccupancyToleranceSqr = 0.25f;

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

    public static int FindFirstOpenEnemyAnchorSlot(
        IReadOnlyList<Transform> anchors,
        BattleEnemyController[] existingEnemies)
    {
        int anchorCount = anchors != null ? anchors.Count : 0;
        if (anchorCount <= 0)
            return -1;

        bool[] occupiedSlots = new bool[anchorCount];
        for (int i = 0; existingEnemies != null && i < existingEnemies.Length; i++)
        {
            BattleEnemyController enemy = existingEnemies[i];
            if (enemy == null || enemy.IsDead)
                continue;

            int slotIndex = enemy.FormationSlotIndex;
            if (slotIndex < 0 || slotIndex >= anchorCount)
                slotIndex = FindNearestOccupiedAnchorSlot(anchors, enemy.transform.position);

            if (slotIndex >= 0 && slotIndex < anchorCount)
                occupiedSlots[slotIndex] = true;
        }

        for (int i = 0; i < occupiedSlots.Length; i++)
        {
            if (!occupiedSlots[i])
                return i;
        }

        return -1;
    }

    public static int FindFirstOpenFallbackFormationSlot(BattleEnemyController[] existingEnemies)
    {
        int aliveCount = 0;
        bool hasIndexedEnemy = false;
        HashSet<int> occupiedSlots = new HashSet<int>();

        for (int i = 0; existingEnemies != null && i < existingEnemies.Length; i++)
        {
            BattleEnemyController enemy = existingEnemies[i];
            if (enemy == null || enemy.IsDead)
                continue;

            aliveCount++;
            if (enemy.FormationSlotIndex < 0)
                continue;

            hasIndexedEnemy = true;
            occupiedSlots.Add(enemy.FormationSlotIndex);
        }

        if (!hasIndexedEnemy)
            return aliveCount;

        int slotIndex = 0;
        while (occupiedSlots.Contains(slotIndex))
            slotIndex++;

        return slotIndex;
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

    private static int FindNearestOccupiedAnchorSlot(IReadOnlyList<Transform> anchors, Vector3 position)
    {
        int bestIndex = -1;
        float bestDistanceSqr = AnchorOccupancyToleranceSqr;

        for (int i = 0; anchors != null && i < anchors.Count; i++)
        {
            Transform anchor = anchors[i];
            if (anchor == null)
                continue;

            float distanceSqr = (anchor.position - position).sqrMagnitude;
            if (distanceSqr <= bestDistanceSqr)
            {
                bestDistanceSqr = distanceSqr;
                bestIndex = i;
            }
        }

        return bestIndex;
    }
}
