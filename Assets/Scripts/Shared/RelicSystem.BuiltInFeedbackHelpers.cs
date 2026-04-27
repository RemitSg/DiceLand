public static partial class RelicSystem
{
    private static void AddRelicFeedback(RelicExecutionContext context, Relic_SO relicData, string detail)
    {
        if (context == null || context.feedbackMessages == null || string.IsNullOrWhiteSpace(detail))
            return;

        context.feedbackMessages.Add($"{GetRelicDisplayName(relicData)} 生效：{detail}");
    }

    private static string GetRelicDisplayName(Relic_SO relicData)
    {
        if (relicData == null)
            return "遗物";

        return relicData.DisplayName;
    }

    private static string GetStatusDisplayName(BattleStatus_SO statusData)
    {
        if (statusData == null)
            return "状态";

        return statusData.DisplayName;
    }

    private static string GetLootDisplayName(LootItem_SO lootData)
    {
        if (lootData == null)
            return "掉落物";

        return !string.IsNullOrWhiteSpace(lootData.lootName) ? lootData.lootName : lootData.name;
    }
}
