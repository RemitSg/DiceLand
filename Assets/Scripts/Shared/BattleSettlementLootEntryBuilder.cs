using System.Collections.Generic;

public static class BattleSettlementLootEntryBuilder
{
    public static List<BattleSettlementLootEntry> Build(List<RunLootState> lootRewards, List<RunRelicState> relicRewards)
    {
        List<BattleSettlementLootEntry> entries = new List<BattleSettlementLootEntry>();
        if (lootRewards != null)
        {
            for (int i = 0; i < lootRewards.Count; i++)
            {
                RunLootState loot = lootRewards[i];
                if (loot == null || loot.lootItem == null || loot.amount <= 0)
                    continue;

                entries.Add(new BattleSettlementLootEntry
                {
                    lootItem = loot.lootItem,
                    amount = loot.amount,
                    displayName = string.IsNullOrWhiteSpace(loot.lootItem.lootName) ? loot.DisplayName : loot.lootItem.lootName,
                    description = loot.lootItem.description ?? string.Empty,
                    rarity = loot.lootItem.rarity,
                    attribute = loot.lootItem.attribute,
                    icon = loot.lootItem.icon
                });
            }
        }

        if (relicRewards != null)
        {
            for (int i = 0; i < relicRewards.Count; i++)
            {
                RunRelicState relic = relicRewards[i];
                if (relic == null || relic.relicData == null || relic.amount <= 0)
                    continue;

                entries.Add(new BattleSettlementLootEntry
                {
                    relicData = relic.relicData,
                    amount = relic.amount,
                    displayName = relic.relicData.DisplayName,
                    description = relic.relicData.description ?? string.Empty,
                    rarity = relic.relicData.rarity,
                    attribute = null,
                    icon = relic.relicData.icon
                });
            }
        }

        return entries;
    }
}
