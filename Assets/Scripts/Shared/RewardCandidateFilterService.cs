using System.Collections.Generic;

public static class RewardCandidateFilterService
{
    public static List<LootItem_SO> BuildLootCandidatesWithRarity(
        IReadOnlyList<LootItem_SO> source,
        LootRarity rarity)
    {
        List<LootItem_SO> candidates = new List<LootItem_SO>();
        if (source == null || source.Count == 0)
            return candidates;

        for (int i = 0; i < source.Count; i++)
        {
            LootItem_SO candidate = source[i];
            if (candidate == null || candidate.rarity != rarity)
                continue;

            candidates.Add(candidate);
        }

        return candidates;
    }

    public static List<Relic_SO> BuildRelicCandidates(
        IReadOnlyList<Relic_SO> source,
        ICollection<Relic_SO> excludedRelics)
    {
        List<Relic_SO> candidates = new List<Relic_SO>();
        if (source == null || source.Count == 0)
            return candidates;

        for (int i = 0; i < source.Count; i++)
        {
            Relic_SO candidate = source[i];
            if (candidate == null)
                continue;
            if (excludedRelics != null && excludedRelics.Contains(candidate))
                continue;

            candidates.Add(candidate);
        }

        return candidates;
    }

    public static bool HasAvailableRelicCandidate(
        RelicTable_SO relicTable,
        ICollection<Relic_SO> excludedRelics)
    {
        if (relicTable == null)
            return false;

        return BuildRelicCandidates(relicTable.Relics, excludedRelics).Count > 0;
    }

    public static List<MerchantCatalog_SO.RelicGoodsEntry> BuildAvailableMerchantRelicGoodsEntries(
        IReadOnlyList<MerchantCatalog_SO.RelicGoodsEntry> source,
        ICollection<Relic_SO> excludedRelics)
    {
        List<MerchantCatalog_SO.RelicGoodsEntry> candidates = new List<MerchantCatalog_SO.RelicGoodsEntry>();
        if (source == null || source.Count == 0)
            return candidates;

        for (int i = 0; i < source.Count; i++)
        {
            MerchantCatalog_SO.RelicGoodsEntry entry = source[i];
            if (!HasAvailableMerchantRelicCandidate(entry, excludedRelics))
                continue;

            candidates.Add(entry);
        }

        return candidates;
    }

    public static bool HasAvailableMerchantRelicCandidate(
        MerchantCatalog_SO.RelicGoodsEntry entry,
        ICollection<Relic_SO> excludedRelics)
    {
        if (entry == null)
            return false;

        if (entry.relicTable != null && entry.relicTable.IsValid)
            return HasAvailableRelicCandidate(entry.relicTable, excludedRelics);

        Relic_SO legacyRelic = entry.LegacyRelicData;
        return legacyRelic != null && (excludedRelics == null || !excludedRelics.Contains(legacyRelic));
    }
}
