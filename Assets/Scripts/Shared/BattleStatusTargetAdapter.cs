using System;

public readonly struct BattleStatusTargetAdapter
{
    private readonly Func<int> getCurrentHP;
    private readonly Func<int> getMaxHP;
    private readonly Func<BattleStatus_SO, int> getStacksByData;
    private readonly Func<string, int> getStacksById;
    private readonly Action<int, int> takeDamage;
    private readonly Action<BattleStatus_SO, string, string> clearStatus;
    private readonly Func<BattleStatus_SO, string, int, string, int> removeStatusStacks;

    public BattleStatusTargetAdapter(
        string displayName,
        Func<int> getCurrentHP,
        Func<int> getMaxHP,
        Func<BattleStatus_SO, int> getStacksByData,
        Func<string, int> getStacksById,
        Action<int, int> takeDamage,
        Action<BattleStatus_SO, string, string> clearStatus,
        Func<BattleStatus_SO, string, int, string, int> removeStatusStacks)
    {
        DisplayName = displayName;
        this.getCurrentHP = getCurrentHP;
        this.getMaxHP = getMaxHP;
        this.getStacksByData = getStacksByData;
        this.getStacksById = getStacksById;
        this.takeDamage = takeDamage;
        this.clearStatus = clearStatus;
        this.removeStatusStacks = removeStatusStacks;
    }

    public string DisplayName { get; }
    public int CurrentHP => getCurrentHP != null ? getCurrentHP() : 0;
    public int MaxHP => getMaxHP != null ? getMaxHP() : 0;

    public int GetStacks(BattleStatus_SO statusData, string statusId)
    {
        return statusData != null
            ? Math.Max(0, getStacksByData != null ? getStacksByData(statusData) : 0)
            : Math.Max(0, getStacksById != null ? getStacksById(statusId) : 0);
    }

    public void TakeDamage(int modifiedDamage, int originalDamage)
    {
        takeDamage?.Invoke(modifiedDamage, originalDamage);
    }

    public void ClearStatus(BattleStatus_SO statusData, string statusId, string statusName)
    {
        clearStatus?.Invoke(statusData, statusId, statusName);
    }

    public int RemoveStatusStacks(BattleStatus_SO statusData, string statusId, int stackAmount, string statusName)
    {
        return Math.Max(0, removeStatusStacks != null ? removeStatusStacks(statusData, statusId, stackAmount, statusName) : 0);
    }
}
