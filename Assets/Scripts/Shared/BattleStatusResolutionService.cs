public readonly struct BattleResolvedStatus
{
    public BattleResolvedStatus(BattleStatus_SO statusData, string statusId, string statusName)
    {
        StatusData = statusData;
        StatusId = statusId ?? string.Empty;
        StatusName = statusName ?? string.Empty;
    }

    public BattleStatus_SO StatusData { get; }
    public string StatusId { get; }
    public string StatusName { get; }
}

public static class BattleStatusResolutionService
{
    public static bool TryResolve(BattleStatus_SO statusData, EnemySkill_SO.DiceStatusType statusType, out BattleResolvedStatus result)
    {
        if (statusData != null)
        {
            result = new BattleResolvedStatus(statusData, statusData.StatusId, statusData.DisplayName);
            return true;
        }

        if (statusType == EnemySkill_SO.DiceStatusType.Burn)
        {
            result = new BattleResolvedStatus(null, "burn", "燃烧");
            return true;
        }

        result = new BattleResolvedStatus(null, string.Empty, string.Empty);
        return false;
    }
}
