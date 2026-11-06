public static partial class RelicSystem
{
    private static void AddRelicFeedback(RelicExecutionContext context, Relic_SO relicData, string detail)
    {
        if (context == null || context.feedbackMessages == null || string.IsNullOrWhiteSpace(detail))
            return;

        context.feedbackMessages.Add(GameText.Format("relic.feedback.add_relic_feedback", "{relic} 生效：{detail}", ("relic", GetRelicDisplayName(relicData)), ("detail", detail)));
    }

    private static string GetRelicDisplayName(Relic_SO relicData)
    {
        if (relicData == null)
            return GameText.Get("relic.feedback.get_relic_display_name", "遗物");

        return relicData.LocalizedName;
    }

    private static string GetStatusDisplayName(BattleStatus_SO statusData)
    {
        if (statusData == null)
            return GameText.Get("relic.feedback.get_status_display_name", "状态");

        return statusData.LocalizedName;
    }

    private static string GetLootDisplayName(LootItem_SO lootData)
    {
        if (lootData == null)
            return GameText.Get("relic.feedback.get_loot_display_name.2", "掉落物");

        return lootData.LocalizedName;
    }

    private static string GetActorDisplayName(BattleAllyController actor) => actor.GetSettlementDisplayName();

    private static string GetActorDisplayName(BattleEnemyController actor) =>
        actor.enemyData != null ? actor.enemyData.LocalizedName : actor.name;
}
