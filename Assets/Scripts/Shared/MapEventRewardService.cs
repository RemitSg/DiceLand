using System.Collections.Generic;
using UnityEngine;

public static class MapEventRewardService
{
    public static bool TryExecuteLootReward(RunState runState, MapEventBuiltInEffectData effectData, out string message)
    {
        if (!TryBuildAvailableLootRewards(effectData, out List<MapEventLootRewardEntry> availableRewards, out message))
            return false;

        int pickCount = RollPickCount(effectData.lootPickCountMin, effectData.lootPickCountMax, availableRewards.Count);
        List<string> gainedLootDescriptions = new List<string>(pickCount);

        for (int i = 0; i < pickCount; i++)
        {
            int selectedIndex = Random.Range(0, availableRewards.Count);
            MapEventLootRewardEntry selectedReward = availableRewards[selectedIndex];
            availableRewards.RemoveAt(selectedIndex);

            int amount = RollLootAmount(selectedReward);
            runState?.AddLoot(selectedReward.lootItem, amount);
            gainedLootDescriptions.Add($"{selectedReward.lootItem.lootName} x{amount}");
        }

        message = gainedLootDescriptions.Count > 0
            ? $"\u83b7\u5f97\u6218\u5229\u54c1\uff1a{string.Join("\uff0c", gainedLootDescriptions)}\u3002"
            : "\u672a\u83b7\u5f97\u4efb\u4f55\u6218\u5229\u54c1\u3002";
        return true;
    }

    public static bool TryExecuteRelicReward(RunState runState, MapEventBuiltInEffectData effectData, out string message)
    {
        if (effectData != null && effectData.relicTable == null && TryExecuteExplicitRelicRewards(runState, effectData, out message))
            return true;

        if (!TryBuildAvailableRelicRewards(effectData, out List<Relic_SO> availableRewards, out message))
            return false;

        int pickCount = RollPickCount(effectData.relicPickCountMin, effectData.relicPickCountMax, availableRewards.Count);
        List<string> gainedRelicDescriptions = new List<string>(pickCount);

        for (int i = 0; i < pickCount; i++)
        {
            Relic_SO selectedRelic = DrawRelicReward(availableRewards, effectData.relicRarityWeightTable, effectData.relicTable);
            if (selectedRelic == null)
                continue;

            runState?.AddRelic(selectedRelic, 1);
            gainedRelicDescriptions.Add(selectedRelic.DisplayName);
        }

        message = gainedRelicDescriptions.Count > 0
            ? $"\u83b7\u5f97\u9057\u7269\uff1a{string.Join("\uff0c", gainedRelicDescriptions)}\u3002"
            : "\u672a\u83b7\u5f97\u4efb\u4f55\u9057\u7269\u3002";
        return true;
    }

    public static bool TryExecuteBlueprintReward(RunState runState, MapEventBuiltInEffectData effectData, out string message)
    {
        if (!TryBuildAvailableBlueprintRewards(effectData, out List<CraftingBlueprint_SO> availableRewards, out message))
            return false;

        int pickCount = RollPickCount(effectData.blueprintPickCountMin, effectData.blueprintPickCountMax, availableRewards.Count);
        List<string> gainedBlueprintDescriptions = new List<string>(pickCount);

        for (int i = 0; i < pickCount; i++)
        {
            CraftingBlueprint_SO selectedBlueprint = DrawBlueprintReward(availableRewards, effectData.blueprintRarityWeightTable);
            if (selectedBlueprint == null)
                continue;

            runState?.AddBlueprint(selectedBlueprint, 1);
            gainedBlueprintDescriptions.Add(selectedBlueprint.DisplayName);
        }

        message = gainedBlueprintDescriptions.Count > 0
            ? $"\u83b7\u5f97\u56fe\u7eb8\uff1a{string.Join("\uff0c", gainedBlueprintDescriptions)}\u3002"
            : "\u672a\u83b7\u5f97\u4efb\u4f55\u56fe\u7eb8\u3002";
        return true;
    }

    public static bool TryQueueEncounterVictoryReward(BattleEncounterData encounterData, MapEventBuiltInEffectData effectData, out string message)
    {
        if (encounterData == null)
        {
            message = "\u672a\u627e\u5230\u5f85\u89e6\u53d1\u7684\u4e8b\u4ef6\u906d\u9047\u3002";
            return false;
        }

        switch (effectData.effectType)
        {
            case MapEventOptionBuiltInEffectType.GainLoot:
            case MapEventOptionBuiltInEffectType.GainRandomLootFromList:
                return TryQueueEncounterVictoryLootRewards(encounterData, effectData, out message);
            case MapEventOptionBuiltInEffectType.GainRelics:
                return TryQueueEncounterVictoryRelicRewards(encounterData, effectData, out message);
            case MapEventOptionBuiltInEffectType.GainBlueprints:
                return TryQueueEncounterVictoryBlueprintRewards(encounterData, effectData, out message);
            case MapEventOptionBuiltInEffectType.GainWeapons:
                return TryQueueEncounterVictoryWeaponRewards(encounterData, effectData, out message);
            default:
                message = string.Empty;
                return true;
        }
    }

    private static bool TryQueueEncounterVictoryLootRewards(BattleEncounterData encounterData, MapEventBuiltInEffectData effectData, out string message)
    {
        if (!TryBuildAvailableLootRewards(effectData, out List<MapEventLootRewardEntry> availableRewards, out message))
            return false;

        int pickCount = RollPickCount(effectData.lootPickCountMin, effectData.lootPickCountMax, availableRewards.Count);
        List<string> descriptions = new List<string>(pickCount);

        for (int i = 0; i < pickCount; i++)
        {
            int selectedIndex = Random.Range(0, availableRewards.Count);
            MapEventLootRewardEntry selectedReward = availableRewards[selectedIndex];
            availableRewards.RemoveAt(selectedIndex);

            int amount = RollLootAmount(selectedReward);
            encounterData.pendingVictoryLootRewards.Add(new RunLootState
            {
                lootItem = selectedReward.lootItem,
                amount = amount
            });
            descriptions.Add($"{selectedReward.lootItem.lootName} x{amount}");
        }

        message = descriptions.Count > 0
            ? $"\u6218\u6597\u80dc\u5229\u540e\u5c06\u83b7\u5f97\u6218\u5229\u54c1\uff1a{string.Join("\uff0c", descriptions)}\u3002"
            : "\u6218\u6597\u80dc\u5229\u540e\u672a\u914d\u7f6e\u4efb\u4f55\u6218\u5229\u54c1\u5956\u52b1\u3002";
        return true;
    }

    private static bool TryQueueEncounterVictoryRelicRewards(BattleEncounterData encounterData, MapEventBuiltInEffectData effectData, out string message)
    {
        if (effectData != null && effectData.relicTable == null && TryQueueExplicitRelicRewards(encounterData, effectData, out message))
            return true;

        if (!TryBuildAvailableRelicRewards(effectData, out List<Relic_SO> availableRewards, out message))
            return false;

        int pickCount = RollPickCount(effectData.relicPickCountMin, effectData.relicPickCountMax, availableRewards.Count);
        List<string> descriptions = new List<string>(pickCount);

        for (int i = 0; i < pickCount; i++)
        {
            Relic_SO selectedRelic = DrawRelicReward(availableRewards, effectData.relicRarityWeightTable, effectData.relicTable);
            if (selectedRelic == null)
                continue;

            encounterData.pendingVictoryRelicRewards.Add(new RunRelicState
            {
                relicData = selectedRelic,
                amount = 1
            });
            descriptions.Add(selectedRelic.DisplayName);
        }

        message = descriptions.Count > 0
            ? $"\u6218\u6597\u80dc\u5229\u540e\u5c06\u83b7\u5f97\u9057\u7269\uff1a{string.Join("\uff0c", descriptions)}\u3002"
            : "\u6218\u6597\u80dc\u5229\u540e\u672a\u914d\u7f6e\u4efb\u4f55\u9057\u7269\u5956\u52b1\u3002";
        return true;
    }

    private static bool TryExecuteExplicitRelicRewards(RunState runState, MapEventBuiltInEffectData effectData, out string message)
    {
        message = string.Empty;
        if (!HasExplicitRelicRewards(effectData))
            return false;

        List<string> descriptions = new List<string>();
        for (int i = 0; i < effectData.relicRewards.Count; i++)
        {
            MapEventRelicRewardEntry reward = effectData.relicRewards[i];
            if (reward == null || reward.relicData == null)
                continue;

            int amount = Mathf.Max(1, reward.amount);
            runState?.AddRelic(reward.relicData, amount);
            descriptions.Add(amount > 1 ? $"{reward.relicData.DisplayName} x{amount}" : reward.relicData.DisplayName);
        }

        message = descriptions.Count > 0
            ? $"获得遗物：{string.Join("，", descriptions)}。"
            : "未获得任何遗物。";
        return true;
    }

    private static bool TryQueueExplicitRelicRewards(BattleEncounterData encounterData, MapEventBuiltInEffectData effectData, out string message)
    {
        message = string.Empty;
        if (!HasExplicitRelicRewards(effectData))
            return false;

        List<string> descriptions = new List<string>();
        for (int i = 0; i < effectData.relicRewards.Count; i++)
        {
            MapEventRelicRewardEntry reward = effectData.relicRewards[i];
            if (reward == null || reward.relicData == null)
                continue;

            int amount = Mathf.Max(1, reward.amount);
            encounterData.pendingVictoryRelicRewards.Add(new RunRelicState
            {
                relicData = reward.relicData,
                amount = amount
            });
            descriptions.Add(amount > 1 ? $"{reward.relicData.DisplayName} x{amount}" : reward.relicData.DisplayName);
        }

        message = descriptions.Count > 0
            ? $"战斗胜利后将获得遗物：{string.Join("，", descriptions)}。"
            : "战斗胜利后未配置任何遗物奖励。";
        return true;
    }

    private static bool HasExplicitRelicRewards(MapEventBuiltInEffectData effectData)
    {
        if (effectData == null || effectData.relicRewards == null)
            return false;

        for (int i = 0; i < effectData.relicRewards.Count; i++)
        {
            MapEventRelicRewardEntry reward = effectData.relicRewards[i];
            if (reward != null && reward.relicData != null)
                return true;
        }

        return false;
    }

    private static bool TryQueueEncounterVictoryBlueprintRewards(BattleEncounterData encounterData, MapEventBuiltInEffectData effectData, out string message)
    {
        if (!TryBuildAvailableBlueprintRewards(effectData, out List<CraftingBlueprint_SO> availableRewards, out message))
            return false;

        int pickCount = RollPickCount(effectData.blueprintPickCountMin, effectData.blueprintPickCountMax, availableRewards.Count);
        List<string> descriptions = new List<string>(pickCount);

        for (int i = 0; i < pickCount; i++)
        {
            CraftingBlueprint_SO selectedBlueprint = DrawBlueprintReward(availableRewards, effectData.blueprintRarityWeightTable);
            if (selectedBlueprint == null)
                continue;

            encounterData.pendingVictoryBlueprintRewards.Add(new RunBlueprintState
            {
                blueprintData = selectedBlueprint,
                amount = 1
            });
            descriptions.Add(selectedBlueprint.DisplayName);
        }

        message = descriptions.Count > 0
            ? $"\u6218\u6597\u80dc\u5229\u540e\u5c06\u83b7\u5f97\u56fe\u7eb8\uff1a{string.Join("\uff0c", descriptions)}\u3002"
            : "\u6218\u6597\u80dc\u5229\u540e\u672a\u914d\u7f6e\u4efb\u4f55\u56fe\u7eb8\u5956\u52b1\u3002";
        return true;
    }

    private static bool TryQueueEncounterVictoryWeaponRewards(BattleEncounterData encounterData, MapEventBuiltInEffectData effectData, out string message)
    {
        if (!TryDrawWeaponRewards(effectData, out List<Weapon_SO> selectedWeapons, out message))
            return false;

        List<string> descriptions = new List<string>(selectedWeapons.Count);
        for (int i = 0; i < selectedWeapons.Count; i++)
        {
            Weapon_SO weapon = selectedWeapons[i];
            if (weapon == null)
                continue;

            encounterData.pendingVictoryWeaponRewards.Add(new RunWeaponState
            {
                weaponData = weapon,
                amount = 1
            });
            descriptions.Add(weapon.weaponName);
        }

        message = descriptions.Count > 0
            ? $"战斗胜利后将获得武器：{string.Join("，", descriptions)}。"
            : "战斗胜利后未配置任何武器奖励。";
        return true;
    }

    public static bool TryExecuteWeaponReward(RunState runState, MapEventBuiltInEffectData effectData, out string message)
    {
        if (!TryDrawWeaponRewards(effectData, out List<Weapon_SO> selectedWeapons, out message))
            return false;

        List<string> descriptions = new List<string>(selectedWeapons.Count);
        for (int i = 0; i < selectedWeapons.Count; i++)
        {
            Weapon_SO weapon = selectedWeapons[i];
            if (weapon == null)
                continue;

            runState?.AddWeapon(weapon, 1);
            descriptions.Add(weapon.weaponName);
        }

        message = descriptions.Count > 0
            ? $"获得武器：{string.Join("，", descriptions)}。"
            : "未获得任何武器。";
        return true;
    }

    private static bool TryDrawWeaponRewards(MapEventBuiltInEffectData effectData, out List<Weapon_SO> selectedWeapons, out string message)
    {
        selectedWeapons = new List<Weapon_SO>();
        if (effectData == null || effectData.weaponCatalog == null)
        {
            message = "该事件效果未配置武器目录。";
            return false;
        }

        IReadOnlyList<Weapon_SO> catalogWeapons = effectData.weaponCatalog.GetWeaponsByRarity(effectData.weaponRarity);
        List<Weapon_SO> candidates = new List<Weapon_SO>();
        if (catalogWeapons != null)
        {
            for (int i = 0; i < catalogWeapons.Count; i++)
            {
                Weapon_SO weapon = catalogWeapons[i];
                if (weapon != null && !candidates.Contains(weapon))
                    candidates.Add(weapon);
            }
        }

        if (candidates.Count == 0)
        {
            message = $"武器目录中没有 {effectData.weaponRarity} 品质武器。";
            return false;
        }

        int minPick = Mathf.Max(1, effectData.weaponPickCountMin);
        int maxPick = Mathf.Max(minPick, effectData.weaponPickCountMax);
        int pickCount = Mathf.Min(Random.Range(minPick, maxPick + 1), candidates.Count);
        for (int i = 0; i < pickCount; i++)
        {
            int selectedIndex = Random.Range(0, candidates.Count);
            selectedWeapons.Add(candidates[selectedIndex]);
            candidates.RemoveAt(selectedIndex);
        }

        message = string.Empty;
        return true;
    }

    private static bool TryBuildAvailableLootRewards(
        MapEventBuiltInEffectData effectData,
        out List<MapEventLootRewardEntry> availableRewards,
        out string message)
    {
        availableRewards = new List<MapEventLootRewardEntry>();
        if (effectData == null || effectData.lootRewards == null || effectData.lootRewards.Count == 0)
        {
            message = "\u8be5\u4e8b\u4ef6\u6548\u679c\u672a\u914d\u7f6e\u6218\u5229\u54c1\u5217\u8868\u3002";
            return false;
        }

        for (int i = 0; i < effectData.lootRewards.Count; i++)
        {
            MapEventLootRewardEntry reward = effectData.lootRewards[i];
            if (reward != null && reward.lootItem != null)
                availableRewards.Add(reward);
        }

        if (availableRewards.Count == 0)
        {
            message = "\u8be5\u4e8b\u4ef6\u6548\u679c\u672a\u914d\u7f6e\u6709\u6548\u7684\u6218\u5229\u54c1\u3002";
            return false;
        }

        message = string.Empty;
        return true;
    }

    private static bool TryBuildAvailableRelicRewards(
        MapEventBuiltInEffectData effectData,
        out List<Relic_SO> availableRewards,
        out string message)
    {
        availableRewards = new List<Relic_SO>();
        if (effectData == null || effectData.relicTable == null)
        {
            message = "\u8be5\u4e8b\u4ef6\u6548\u679c\u672a\u914d\u7f6e\u9057\u7269\u8868\u3002";
            return false;
        }

        IReadOnlyList<Relic_SO> relics = effectData.relicTable.Relics;
        if (relics == null)
        {
            message = "\u8be5\u4e8b\u4ef6\u6548\u679c\u672a\u914d\u7f6e\u6709\u6548\u7684\u9057\u7269\u3002";
            return false;
        }

        for (int i = 0; i < relics.Count; i++)
        {
            Relic_SO relic = relics[i];
            if (relic != null && !availableRewards.Contains(relic))
                availableRewards.Add(relic);
        }

        if (availableRewards.Count == 0)
        {
            message = "\u8be5\u4e8b\u4ef6\u6548\u679c\u672a\u914d\u7f6e\u6709\u6548\u7684\u9057\u7269\u3002";
            return false;
        }

        message = string.Empty;
        return true;
    }

    private static Relic_SO DrawRelicReward(
        List<Relic_SO> availableRewards,
        LootRarityWeightTable_SO rarityWeightTable,
        RelicTable_SO relicTable)
    {
        if (availableRewards == null || availableRewards.Count == 0)
            return null;

        Dictionary<LootRarity, List<Relic_SO>> rarityBuckets = BuildRelicRarityBuckets(availableRewards);
        if (rarityWeightTable != null && rarityWeightTable.HasPositiveWeights)
        {
            LootRarity selectedRarity = SelectWeightedRewardRarity(rarityWeightTable);
            List<Relic_SO> selectedBucket = ResolveRelicBucketWithRarityFallback(rarityBuckets, selectedRarity);
            if (selectedBucket != null && selectedBucket.Count > 0)
            {
                int bucketIndex = Random.Range(0, selectedBucket.Count);
                Relic_SO selectedRelic = selectedBucket[bucketIndex];
                availableRewards.Remove(selectedRelic);
                return selectedRelic;
            }
        }

        Relic_SO tableWeightedRelic = relicTable != null
            ? relicTable.DrawRandomRelic(new System.Random(Random.Range(int.MinValue, int.MaxValue)), BuildExcludedRelics(relicTable, availableRewards))
            : null;
        if (tableWeightedRelic != null && availableRewards.Remove(tableWeightedRelic))
            return tableWeightedRelic;

        int selectedIndex = Random.Range(0, availableRewards.Count);
        Relic_SO fallbackRelic = availableRewards[selectedIndex];
        availableRewards.RemoveAt(selectedIndex);
        return fallbackRelic;
    }

    private static HashSet<Relic_SO> BuildExcludedRelics(RelicTable_SO relicTable, List<Relic_SO> availableRewards)
    {
        HashSet<Relic_SO> excludedRelics = new HashSet<Relic_SO>();
        if (relicTable == null || relicTable.Relics == null)
            return excludedRelics;

        HashSet<Relic_SO> availableSet = new HashSet<Relic_SO>(availableRewards);
        IReadOnlyList<Relic_SO> relics = relicTable.Relics;
        for (int i = 0; i < relics.Count; i++)
        {
            Relic_SO relic = relics[i];
            if (relic != null && !availableSet.Contains(relic))
                excludedRelics.Add(relic);
        }

        return excludedRelics;
    }

    private static Dictionary<LootRarity, List<Relic_SO>> BuildRelicRarityBuckets(List<Relic_SO> availableRewards)
    {
        Dictionary<LootRarity, List<Relic_SO>> rarityBuckets = new Dictionary<LootRarity, List<Relic_SO>>();
        for (int i = 0; i < availableRewards.Count; i++)
        {
            Relic_SO relic = availableRewards[i];
            if (relic == null)
                continue;

            if (!rarityBuckets.TryGetValue(relic.rarity, out List<Relic_SO> bucket))
            {
                bucket = new List<Relic_SO>();
                rarityBuckets.Add(relic.rarity, bucket);
            }

            bucket.Add(relic);
        }

        return rarityBuckets;
    }

    private static List<Relic_SO> ResolveRelicBucketWithRarityFallback(
        Dictionary<LootRarity, List<Relic_SO>> rarityBuckets,
        LootRarity selectedRarity)
    {
        if (TryGetNonEmptyRelicBucket(rarityBuckets, selectedRarity, out List<Relic_SO> directBucket))
            return directBucket;

        for (int rarityIndex = (int)selectedRarity - 1; rarityIndex >= (int)LootRarity.Common; rarityIndex--)
        {
            if (TryGetNonEmptyRelicBucket(rarityBuckets, (LootRarity)rarityIndex, out List<Relic_SO> lowerBucket))
                return lowerBucket;
        }

        for (int rarityIndex = (int)selectedRarity + 1; rarityIndex <= (int)LootRarity.Legendary; rarityIndex++)
        {
            if (TryGetNonEmptyRelicBucket(rarityBuckets, (LootRarity)rarityIndex, out List<Relic_SO> higherBucket))
                return higherBucket;
        }

        return null;
    }

    private static bool TryGetNonEmptyRelicBucket(
        Dictionary<LootRarity, List<Relic_SO>> rarityBuckets,
        LootRarity rarity,
        out List<Relic_SO> bucket)
    {
        bucket = null;
        if (rarityBuckets == null)
            return false;

        if (!rarityBuckets.TryGetValue(rarity, out List<Relic_SO> foundBucket) || foundBucket == null || foundBucket.Count == 0)
            return false;

        bucket = foundBucket;
        return true;
    }

    private static bool TryBuildAvailableBlueprintRewards(
        MapEventBuiltInEffectData effectData,
        out List<CraftingBlueprint_SO> availableRewards,
        out string message)
    {
        availableRewards = new List<CraftingBlueprint_SO>();
        if (effectData == null || effectData.blueprintTable == null)
        {
            message = "\u8be5\u4e8b\u4ef6\u6548\u679c\u672a\u914d\u7f6e\u56fe\u7eb8\u8868\u3002";
            return false;
        }

        IReadOnlyList<CraftingBlueprint_SO> blueprints = effectData.blueprintTable.GetValidBlueprints();
        for (int i = 0; i < blueprints.Count; i++)
        {
            CraftingBlueprint_SO blueprint = blueprints[i];
            if (blueprint != null && !availableRewards.Contains(blueprint))
                availableRewards.Add(blueprint);
        }

        if (availableRewards.Count == 0)
        {
            message = "\u8be5\u4e8b\u4ef6\u6548\u679c\u672a\u914d\u7f6e\u6709\u6548\u7684\u56fe\u7eb8\u3002";
            return false;
        }

        message = string.Empty;
        return true;
    }

    private static CraftingBlueprint_SO DrawBlueprintReward(
        List<CraftingBlueprint_SO> availableRewards,
        LootRarityWeightTable_SO rarityWeightTable)
    {
        if (availableRewards == null || availableRewards.Count == 0)
            return null;

        Dictionary<LootRarity, List<CraftingBlueprint_SO>> rarityBuckets = BuildBlueprintRarityBuckets(availableRewards);
        if (rarityWeightTable != null && rarityWeightTable.HasPositiveWeights)
        {
            LootRarity selectedRarity = SelectWeightedRewardRarity(rarityWeightTable);
            List<CraftingBlueprint_SO> selectedBucket = ResolveBlueprintBucketWithRarityFallback(rarityBuckets, selectedRarity);
            if (selectedBucket != null && selectedBucket.Count > 0)
            {
                int bucketIndex = Random.Range(0, selectedBucket.Count);
                CraftingBlueprint_SO selectedBlueprint = selectedBucket[bucketIndex];
                availableRewards.Remove(selectedBlueprint);
                return selectedBlueprint;
            }
        }

        int selectedIndex = Random.Range(0, availableRewards.Count);
        CraftingBlueprint_SO fallbackBlueprint = availableRewards[selectedIndex];
        availableRewards.RemoveAt(selectedIndex);
        return fallbackBlueprint;
    }

    private static Dictionary<LootRarity, List<CraftingBlueprint_SO>> BuildBlueprintRarityBuckets(List<CraftingBlueprint_SO> availableRewards)
    {
        Dictionary<LootRarity, List<CraftingBlueprint_SO>> rarityBuckets = new Dictionary<LootRarity, List<CraftingBlueprint_SO>>();
        for (int i = 0; i < availableRewards.Count; i++)
        {
            CraftingBlueprint_SO blueprint = availableRewards[i];
            if (blueprint == null)
                continue;

            if (!rarityBuckets.TryGetValue(blueprint.rarity, out List<CraftingBlueprint_SO> bucket))
            {
                bucket = new List<CraftingBlueprint_SO>();
                rarityBuckets.Add(blueprint.rarity, bucket);
            }

            bucket.Add(blueprint);
        }

        return rarityBuckets;
    }

    private static LootRarity SelectWeightedRewardRarity(LootRarityWeightTable_SO rarityWeightTable)
    {
        float totalWeight = 0f;
        List<LootRarity> weightedRarities = new List<LootRarity>();
        List<float> cumulativeWeights = new List<float>();
        for (int rarityIndex = (int)LootRarity.Common; rarityIndex <= (int)LootRarity.Legendary; rarityIndex++)
        {
            LootRarity rarity = (LootRarity)rarityIndex;
            float weight = rarityWeightTable != null ? Mathf.Max(0f, rarityWeightTable.GetWeight(rarity)) : 0f;
            if (weight <= 0f)
                continue;

            totalWeight += weight;
            weightedRarities.Add(rarity);
            cumulativeWeights.Add(totalWeight);
        }

        if (totalWeight <= 0f)
            return LootRarity.Common;

        float roll = Random.Range(0f, totalWeight);
        for (int i = 0; i < cumulativeWeights.Count; i++)
        {
            if (roll <= cumulativeWeights[i])
                return weightedRarities[i];
        }

        return weightedRarities[weightedRarities.Count - 1];
    }

    private static List<CraftingBlueprint_SO> ResolveBlueprintBucketWithRarityFallback(
        Dictionary<LootRarity, List<CraftingBlueprint_SO>> rarityBuckets,
        LootRarity selectedRarity)
    {
        if (TryGetNonEmptyBlueprintBucket(rarityBuckets, selectedRarity, out List<CraftingBlueprint_SO> directBucket))
            return directBucket;

        for (int rarityIndex = (int)selectedRarity - 1; rarityIndex >= (int)LootRarity.Common; rarityIndex--)
        {
            if (TryGetNonEmptyBlueprintBucket(rarityBuckets, (LootRarity)rarityIndex, out List<CraftingBlueprint_SO> lowerBucket))
                return lowerBucket;
        }

        for (int rarityIndex = (int)selectedRarity + 1; rarityIndex <= (int)LootRarity.Legendary; rarityIndex++)
        {
            if (TryGetNonEmptyBlueprintBucket(rarityBuckets, (LootRarity)rarityIndex, out List<CraftingBlueprint_SO> higherBucket))
                return higherBucket;
        }

        return null;
    }

    private static bool TryGetNonEmptyBlueprintBucket(
        Dictionary<LootRarity, List<CraftingBlueprint_SO>> rarityBuckets,
        LootRarity rarity,
        out List<CraftingBlueprint_SO> bucket)
    {
        bucket = null;
        if (rarityBuckets == null)
            return false;

        if (!rarityBuckets.TryGetValue(rarity, out List<CraftingBlueprint_SO> foundBucket) || foundBucket == null || foundBucket.Count == 0)
            return false;

        bucket = foundBucket;
        return true;
    }

    private static int RollPickCount(int minPickCount, int maxPickCount, int availableCount)
    {
        int min = Mathf.Max(1, minPickCount);
        int max = Mathf.Max(min, maxPickCount);
        return Mathf.Min(Random.Range(min, max + 1), Mathf.Max(0, availableCount));
    }

    private static int RollLootAmount(MapEventLootRewardEntry reward)
    {
        int amountMin = Mathf.Max(1, reward.amountMin);
        int amountMax = Mathf.Max(amountMin, reward.amountMax);
        return Random.Range(amountMin, amountMax + 1);
    }
}
