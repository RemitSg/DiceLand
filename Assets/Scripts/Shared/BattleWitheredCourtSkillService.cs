using System;
using System.Collections.Generic;
using UnityEngine;

public static class BattleWitheredCourtSkillService
{
    public static void Apply(BattleEnemyController attacker, EnemySkill_SO skill,
        EnemySkill_SO.DiceEffectTrigger trigger, int dealtDamage, IReadOnlyList<BattleEnemyController> enemies,
        Action<string> log = null)
    {
        if (attacker == null || attacker.IsDead || skill == null || skill.mechanicEnemyData == null)
            return;

        switch (skill.mechanic)
        {
            case BattleSkillMechanic.WitheredCourtFlourish:
                if (trigger != EnemySkill_SO.DiceEffectTrigger.OnSkillUse || skill.mechanicStatusData == null)
                    return;
                foreach (BattleEnemyController target in enemies)
                {
                    if (target == null || target.IsDead || target.enemyData != skill.mechanicEnemyData)
                        continue;
                    BattleStatus_SO status = skill.mechanicStatusData;
                    target.QueueStatusForRoundStart(status, status.StatusId, status.DisplayName, 1, 1, 1);
                }
                log?.Invoke($"{attacker.GetDebugDisplayName()}[{skill.LocalizedName}] 使存活的{skill.mechanicEnemyData.LocalizedName}在下回合获得 1 层{skill.mechanicStatusData.DisplayName}。");
                break;
            case BattleSkillMechanic.WitheredCourtDecomposition:
                if (trigger != EnemySkill_SO.DiceEffectTrigger.OnHit || dealtDamage <= 0)
                    return;
                foreach (BattleEnemyController target in enemies)
                {
                    if (target != null && !target.IsDead && target.enemyData == skill.mechanicEnemyData)
                        target.Heal(dealtDamage);
                }
                break;
            case BattleSkillMechanic.BudBranchSacrifice:
                if (trigger == EnemySkill_SO.DiceEffectTrigger.OnSkillUse)
                {
                    attacker.QueueBranchSacrifice(skill.mechanicEnemyData, skill.mechanicStatusData);
                    log?.Invoke($"{attacker.GetDebugDisplayName()}[断枝] 将在下回合开始时死亡，将剩余生命返还给{skill.mechanicEnemyData.LocalizedName}。");
                }
                break;
        }
    }

    public static void ResolvePendingSacrifice(BattleEnemyController source, IReadOnlyList<BattleEnemyController> enemies)
    {
        if (source == null || !source.TryConsumeBranchSacrifice(out EnemyData_SO beneficiary, out BattleStatus_SO status))
            return;

        int remainingHealth = source.CurrentHP;
        source.KillInstantly();
        foreach (BattleEnemyController target in enemies)
        {
            if (target == null || target.IsDead || target.enemyData != beneficiary)
                continue;
            target.Heal(remainingHealth);
            if (status != null)
                target.AddStatus(status, status.StatusId, status.DisplayName, 1);
        }
    }

    public static IEnumerable<string> Describe(EnemySkill_SO skill, int diceIndex)
    {
        if (skill == null)
            yield break;
        if (diceIndex == 0 && skill.maxUsesPerEnemyRound > 0)
            yield return $"每名敌人每回合最多使用 {skill.maxUsesPerEnemyRound} 次。";

        string target = skill.mechanicEnemyData != null ? skill.mechanicEnemyData.LocalizedName : "指定敌人";
        string status = skill.mechanicStatusData != null ? skill.mechanicStatusData.DisplayName : "指定状态";
        switch (skill.mechanic)
        {
            case BattleSkillMechanic.WitheredCourtFlourish:
                yield return $"[使用时] 使场上所有存活的{target}在下回合开始时获得 1 层{status}。";
                break;
            case BattleSkillMechanic.WitheredCourtDecomposition:
                yield return $"第 {diceIndex + 1} 攻击骰：[命中时] 为所有存活的{target}恢复相当于该次实际伤害量的生命，最高恢复至生命上限。";
                break;
            case BattleSkillMechanic.BudBranchSacrifice:
                yield return $"[使用时] 下回合开始时死亡，为存活的{target}恢复死亡前剩余生命值，并使其获得 1 层{status}。提前被击杀时献祭失效。";
                break;
        }
    }
}
