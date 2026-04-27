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

    public static List<MerchantCatalog_SO.LootGoodsEntry> BuildAvailableMerchantLootGoodsEntries(
        IReadOnlyList<MerchantCatalog_SO.LootGoodsEntry> source,
        ICollection<LootItem_SO> excludedLoot)
    {
        List<MerchantCatalog_SO.LootGoodsEntry> candidates = new List<MerchantCatalog_SO.LootGoodsEntry>();
        if (source == null || source.Count == 0)
            return candidates;

        for (int i = 0; i < source.Count; i++)
        {
            MerchantCatalog_SO.LootGoodsEntry entry = source[i];
            if (!HasAvailableMerchantLootCandidate(entry, excludedLoot))
                continue;

            candidates.Add(entry);
        }

        return candidates;
    }

    public static bool HasAvailableMerchantLootCandidate(
        MerchantCatalog_SO.LootGoodsEntry entry,
        ICollection<LootItem_SO> excludedLoot)
    {
        if (entry == null || entry.lootItem == null)
            return false;

        return excludedLoot == null || !excludedLoot.Contains(entry.lootItem);
    }

    public static List<MerchantCatalog_SO.WeaponGoodsEntry> BuildAvailableMerchantWeaponGoodsEntries(
        IReadOnlyList<MerchantCatalog_SO.WeaponGoodsEntry> source,
        ICollection<Weapon_SO> excludedWeapons)
    {
        List<MerchantCatalog_SO.WeaponGoodsEntry> candidates = new List<MerchantCatalog_SO.WeaponGoodsEntry>();
        if (source == null || source.Count == 0)
            return candidates;

        for (int i = 0; i < source.Count; i++)
        {
            MerchantCatalog_SO.WeaponGoodsEntry entry = source[i];
            if (!HasAvailableMerchantWeaponCandidate(entry, excludedWeapons))
                continue;

            candidates.Add(entry);
        }

        return candidates;
    }

    public static bool HasAvailableMerchantWeaponCandidate(
        MerchantCatalog_SO.WeaponGoodsEntry entry,
        ICollection<Weapon_SO> excludedWeapons)
    {
        if (entry == null || entry.weaponData == null)
            return false;

        return excludedWeapons == null || !excludedWeapons.Contains(entry.weaponData);
    }

    public static List<MerchantCatalog_SO.BlueprintGoodsEntry> BuildAvailableMerchantBlueprintGoodsEntries(
        IReadOnlyList<MerchantCatalog_SO.BlueprintGoodsEntry> source,
        ICollection<CraftingBlueprint_SO> excludedBlueprints)
    {
        List<MerchantCatalog_SO.BlueprintGoodsEntry> candidates = new List<MerchantCatalog_SO.BlueprintGoodsEntry>();
        if (source == null || source.Count == 0)
            return candidates;

        for (int i = 0; i < source.Count; i++)
        {
            MerchantCatalog_SO.BlueprintGoodsEntry entry = source[i];
            if (!HasAvailableMerchantBlueprintCandidate(entry, excludedBlueprints))
                continue;

            candidates.Add(entry);
        }

        return candidates;
    }

    public static bool HasAvailableMerchantBlueprintCandidate(
        MerchantCatalog_SO.BlueprintGoodsEntry entry,
        ICollection<CraftingBlueprint_SO> excludedBlueprints)
    {
        if (entry == null || entry.blueprintData == null)
            return false;

        return excludedBlueprints == null || !excludedBlueprints.Contains(entry.blueprintData);
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
