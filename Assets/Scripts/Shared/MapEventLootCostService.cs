using System.Collections.Generic;
using UnityEngine;

public sealed class MapEventLootDiscardPreview
{
    public LootItem_SO lootItem;
    public int amount;
}

public sealed class MapEventLootCostPreviewData
{
    public bool isSelectable = true;
    public string additionalDetail;
    public string unavailableReason;
}

public static class MapEventLootCostService
{
    public static MapEventLootCostPreviewData BuildPreview(
        RunState runState,
        MapEventOptionData optionData,
        IDictionary<string, MapEventLootDiscardPreview> randomLootDiscardPreviewCache)
    {
        MapEventLootCostPreviewData preview = new MapEventLootCostPreviewData();
        if (optionData == null || runState == null)
            return preview;

        List<MapEventBuiltInEffectData> allEffects = MapEventEffectSequenceBuilder.BuildAllEffects(optionData);
        List<string> detailLines = new List<string>();

        for (int i = 0; i < allEffects.Count; i++)
        {
            MapEventBuiltInEffectData effectData = allEffects[i];
            if (effectData == null)
                continue;

            switch (effectData.effectType)
            {
                case MapEventOptionBuiltInEffectType.LoseOwnedRandomLoot:
                {
                    MapEventLootDiscardPreview discardPreview = GetOrCreateRandomLootDiscardPreview(
                        runState,
                        optionData,
                        i,
                        effectData,
                        randomLootDiscardPreviewCache);

                    if (discardPreview == null || discardPreview.lootItem == null || discardPreview.amount <= 0)
                    {
                        preview.isSelectable = false;
                        preview.unavailableReason = "背包中没有可丢弃的战利品。";
                        break;
                    }

                    detailLines.Add($"将随机丢弃：{discardPreview.lootItem.lootName} x{discardPreview.amount}");
                    break;
                }
                case MapEventOptionBuiltInEffectType.LoseSpecificLoot:
                    AppendSpecificLootPreview(runState, effectData, detailLines, preview);
                    break;
            }
        }

        preview.additionalDetail = detailLines.Count > 0 ? string.Join("\n", detailLines) : string.Empty;
        return preview;
    }

    public static MapEventLootDiscardPreview GetOrCreateRandomLootDiscardPreview(
        RunState runState,
        MapEventOptionData optionData,
        int effectIndex,
        MapEventBuiltInEffectData effectData,
        IDictionary<string, MapEventLootDiscardPreview> randomLootDiscardPreviewCache)
    {
        string cacheKey = MapEventEffectSequenceBuilder.BuildRandomLootDiscardPreviewKey(optionData, effectIndex);
        if (randomLootDiscardPreviewCache != null &&
            randomLootDiscardPreviewCache.TryGetValue(cacheKey, out MapEventLootDiscardPreview cachedPreview))
        {
            if (cachedPreview != null &&
                cachedPreview.lootItem != null &&
                runState != null &&
                runState.GetLootAmount(cachedPreview.lootItem) >= cachedPreview.amount)
            {
                return cachedPreview;
            }

            randomLootDiscardPreviewCache.Remove(cacheKey);
        }

        if (runState == null || runState.lootInventory == null || effectData == null)
            return null;

        List<RunLootState> ownedLoot = new List<RunLootState>();
        for (int i = 0; i < runState.lootInventory.Count; i++)
        {
            RunLootState lootState = runState.lootInventory[i];
            if (lootState == null || lootState.lootItem == null || lootState.amount <= 0)
                continue;

            ownedLoot.Add(lootState);
        }

        if (ownedLoot.Count == 0)
            return null;

        RunLootState selectedLoot = ownedLoot[UnityEngine.Random.Range(0, ownedLoot.Count)];
        int amountMin = Mathf.Max(1, effectData.randomOwnedLootAmountMin);
        int amountMax = Mathf.Max(amountMin, effectData.randomOwnedLootAmountMax);
        int amount = Mathf.Min(selectedLoot.amount, UnityEngine.Random.Range(amountMin, amountMax + 1));
        amount = Mathf.Max(1, amount);

        MapEventLootDiscardPreview preview = new MapEventLootDiscardPreview
        {
            lootItem = selectedLoot.lootItem,
            amount = amount
        };
        randomLootDiscardPreviewCache?.Add(cacheKey, preview);
        return preview;
    }

    public static bool TryExecuteRandomLootDiscard(
        RunState runState,
        MapEventOptionData optionData,
        int effectIndex,
        MapEventBuiltInEffectData effectData,
        IDictionary<string, MapEventLootDiscardPreview> randomLootDiscardPreviewCache,
        out string message)
    {
        MapEventLootDiscardPreview preview = GetOrCreateRandomLootDiscardPreview(
            runState,
            optionData,
            effectIndex,
            effectData,
            randomLootDiscardPreviewCache);

        if (preview == null || preview.lootItem == null || preview.amount <= 0)
        {
            message = "背包中没有可丢弃的战利品。";
            return false;
        }

        if (runState == null || !runState.TryConsumeLoot(preview.lootItem, preview.amount))
        {
            message = $"无法丢弃 {preview.lootItem.lootName} x{preview.amount}。";
            return false;
        }

        message = $"失去战利品：{preview.lootItem.lootName} x{preview.amount}。";
        randomLootDiscardPreviewCache?.Remove(MapEventEffectSequenceBuilder.BuildRandomLootDiscardPreviewKey(optionData, effectIndex));
        return true;
    }

    public static bool TryExecuteSpecificLootDiscard(RunState runState, MapEventBuiltInEffectData effectData, out string message)
    {
        if (effectData == null || effectData.lootRewards == null || effectData.lootRewards.Count == 0)
        {
            message = "该事件效果未配置可丢弃的战利品。";
            return false;
        }

        List<string> descriptions = new List<string>();
        for (int i = 0; i < effectData.lootRewards.Count; i++)
        {
            MapEventLootRewardEntry reward = effectData.lootRewards[i];
            if (reward == null || reward.lootItem == null)
                continue;

            int requiredAmount = MapEventEffectSequenceBuilder.GetFixedLootDiscardAmount(reward);
            if (runState == null || runState.GetLootAmount(reward.lootItem) < requiredAmount)
            {
                message = $"背包中缺少 {reward.lootItem.lootName} x{requiredAmount}。";
                return false;
            }
        }

        bool consumedAny = false;
        for (int i = 0; i < effectData.lootRewards.Count; i++)
        {
            MapEventLootRewardEntry reward = effectData.lootRewards[i];
            if (reward == null || reward.lootItem == null)
                continue;

            int requiredAmount = MapEventEffectSequenceBuilder.GetFixedLootDiscardAmount(reward);
            if (!runState.TryConsumeLoot(reward.lootItem, requiredAmount))
            {
                message = $"无法丢弃 {reward.lootItem.lootName} x{requiredAmount}。";
                return false;
            }

            consumedAny = true;
            descriptions.Add($"{reward.lootItem.lootName} x{requiredAmount}");
        }

        if (!consumedAny)
        {
            message = "该事件效果未配置有效的战利品。";
            return false;
        }

        message = $"失去战利品：{string.Join("，", descriptions)}。";
        return true;
    }

    private static void AppendSpecificLootPreview(
        RunState runState,
        MapEventBuiltInEffectData effectData,
        List<string> detailLines,
        MapEventLootCostPreviewData preview)
    {
        bool hasValidRequirement = false;
        bool requirementsSatisfied = true;
        if (effectData.lootRewards != null)
        {
            for (int rewardIndex = 0; rewardIndex < effectData.lootRewards.Count; rewardIndex++)
            {
                MapEventLootRewardEntry reward = effectData.lootRewards[rewardIndex];
                if (reward == null || reward.lootItem == null)
                    continue;

                hasValidRequirement = true;
                int requiredAmount = MapEventEffectSequenceBuilder.GetFixedLootDiscardAmount(reward);
                detailLines.Add($"将失去：{reward.lootItem.lootName} x{requiredAmount}");
                if (runState.GetLootAmount(reward.lootItem) < requiredAmount)
                    requirementsSatisfied = false;
            }
        }

        if (!hasValidRequirement)
        {
            preview.isSelectable = false;
            preview.unavailableReason = "该选项未配置可丢弃的战利品。";
        }
        else if (!requirementsSatisfied)
        {
            preview.isSelectable = false;
            preview.unavailableReason = "背包中缺少所需的指定战利品。";
        }
    }
}
