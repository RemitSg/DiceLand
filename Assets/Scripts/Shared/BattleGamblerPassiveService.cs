using System;
using UnityEngine;

public enum BattleGamblerDamageSource
{
    Normal = 0,
    GambleLoss = 1
}

public static class BattleGamblerPassiveService
{
    public const int GamblerEnemyId = 36;
    public const string DebtStatusId = "debt";
    public const int DebtInstantDeathThreshold = 3;
    private const string GambleSkillName = "\u8D4C\u535A";

    public static bool ShouldBlockIncomingDamage(BattleEnemyController target, BattleGamblerDamageSource damageSource)
    {
        return target != null
            && target.EnemyID == GamblerEnemyId
            && damageSource != BattleGamblerDamageSource.GambleLoss;
    }

    public static bool TryApplyPlayerGambleClashWinDamage(BattleAllyController attacker, BattleEnemyController target, out int dealtDamage, Action<string> log)
    {
        dealtDamage = 0;
        if (attacker == null || target == null || target.IsDead || target.EnemyID != GamblerEnemyId)
            return false;
        if (!IsGambleSkill(attacker.SelectedSkill))
            return false;

        dealtDamage = target.TakeDamage(1, 0, 1, BattleGamblerDamageSource.GambleLoss);
        if (dealtDamage > 0)
            log?.Invoke($"被动触发：{target.GetDebugDisplayName()}[愿赌服输] 被玩家方【赌博】拼点胜利，受到 {dealtDamage} 点伤害");
        return dealtDamage > 0;
    }

    public static bool TryApplyDebtInstantDeath(BattleAllyController target, BattleStatus_SO debtStatus, Action<string> log)
    {
        return TryApplyDebtInstantDeath(target, debtStatus, debtStatus != null ? debtStatus.StatusId : DebtStatusId, log);
    }

    public static bool TryApplyDebtInstantDeath(BattleAllyController target, BattleStatus_SO statusData, string fallbackStatusId, Action<string> log)
    {
        if (target == null || target.IsDead || !IsDebtStatus(statusData, fallbackStatusId))
            return false;
        if (target.GetStatusStacks(statusData) < DebtInstantDeathThreshold && target.GetStatusStacks(DebtStatusId) < DebtInstantDeathThreshold)
            return false;

        string targetName = target.GetDebugDisplayName();
        target.KillInstantly();
        log?.Invoke($"被动触发：{targetName}[偿还] 负债达到 {DebtInstantDeathThreshold} 层，立即死亡");
        return true;
    }

    public static bool TryApplyDebtInstantDeath(BattleEnemyController target, BattleStatus_SO statusData, string fallbackStatusId, Action<string> log)
    {
        if (target == null || target.IsDead || !IsDebtStatus(statusData, fallbackStatusId))
            return false;
        if (target.GetStatusStacks(statusData) < DebtInstantDeathThreshold && target.GetStatusStacks(DebtStatusId) < DebtInstantDeathThreshold)
            return false;

        string targetName = target.GetDebugDisplayName();
        target.KillInstantly();
        log?.Invoke($"被动触发：{targetName}[偿还] 负债达到 {DebtInstantDeathThreshold} 层，立即死亡");
        return true;
    }

    public static bool IsGambleSkill(EnemySkill_SO skill)
    {
        return skill != null && string.Equals(skill.skillName, GambleSkillName, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsDebtStatus(BattleStatus_SO statusData, string fallbackStatusId)
    {
        string statusId = statusData != null ? statusData.StatusId : fallbackStatusId;
        return string.Equals(statusId, DebtStatusId, StringComparison.OrdinalIgnoreCase);
    }
}
