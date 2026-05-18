using System;
using System.Collections.Generic;
using UnityEngine;

public static class BattlePuppetPassiveService
{
    public const int PuppetMasterEnemyId = 35;
    public const int SoullessPuppetEnemyId = 3501;

    public static int ResolveSoullessPuppetUnmarkedTargetPowerPenalty(
        BattleEnemyController attacker,
        BattleAllyController target,
        BattleStatus_SO markStatus)
    {
        if (attacker == null || target == null || attacker.EnemyID != SoullessPuppetEnemyId)
            return 0;

        bool targetMarked = markStatus != null
            ? target.GetStatusStacks(markStatus) > 0
            : target.GetStatusStacks("target") > 0;
        return targetMarked ? 0 : -1;
    }

    public static bool TryApplyPuppetMasterDamageRedirect(
        BattleEnemyController target,
        int incomingDamage,
        IReadOnlyList<BattleEnemyController> enemies,
        Func<int, int> randomIndex,
        out int dealtDamage,
        Action<string> log)
    {
        dealtDamage = 0;
        if (target == null || target.EnemyID != PuppetMasterEnemyId || incomingDamage <= 0)
            return false;

        List<BattleEnemyController> puppets = BuildLivingSoullessPuppets(target, enemies);
        if (puppets.Count == 0)
            return false;

        int selectedIndex = Mathf.Clamp(randomIndex != null ? randomIndex(puppets.Count) : UnityEngine.Random.Range(0, puppets.Count), 0, puppets.Count - 1);
        BattleEnemyController puppet = puppets[selectedIndex];
        dealtDamage = puppet.TakeDamage(incomingDamage, 0, incomingDamage);
        log?.Invoke($"被动触发：{target.GetDebugDisplayName()}[牵丝] 将 {incomingDamage} 点伤害转移给 {puppet.GetDebugDisplayName()}，实际造成 {dealtDamage} 点伤害");
        return true;
    }

    private static List<BattleEnemyController> BuildLivingSoullessPuppets(
        BattleEnemyController target,
        IReadOnlyList<BattleEnemyController> enemies)
    {
        List<BattleEnemyController> puppets = new List<BattleEnemyController>();
        if (enemies == null)
            return puppets;

        for (int i = 0; i < enemies.Count; i++)
        {
            BattleEnemyController enemy = enemies[i];
            if (enemy == null || enemy == target || enemy.IsDead || enemy.EnemyID != SoullessPuppetEnemyId)
                continue;

            puppets.Add(enemy);
        }

        return puppets;
    }
}
