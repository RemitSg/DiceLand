using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public static partial class RelicSystem
{
    private static void HandlePreserveRandomRelicMaterialOnReforge(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if (context.trigger != Relic_SO.RelicTrigger.OnRelicReforge)
            return;
        if (context.firstReforgeRelicMaterial == null || context.secondReforgeRelicMaterial == null)
            return;
        if (context.preservedReforgeMaterialSlot >= 0)
            return;

        context.preservedReforgeMaterialSlot = context.firstReforgeRelicMaterial == context.secondReforgeRelicMaterial
            ? 0
            : Random.Range(0, 2);

        if (context.feedbackMessages != null)
        {
            Relic_SO preservedRelic = context.preservedReforgeMaterialSlot == 0
                ? context.firstReforgeRelicMaterial
                : context.secondReforgeRelicMaterial;

            if (preservedRelic != null)
                AddRelicFeedback(context, relicData, $"遗物锻造时保留了 {GetRelicDisplayName(preservedRelic)}。");
        }
        return;
    }

    private static void HandleGrantRandomLootOfSameRarityOnLootReforge(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if (context.trigger != Relic_SO.RelicTrigger.OnLootReforge)
            return;
        if (context.reforgeResultLoot == null || context.lootReforgePool == null || context.lootReforgePool.Count == 0)
            return;

        int bonusLootCount = Mathf.Max(1, effect.amount * Mathf.Max(1, relicState.amount));
        List<LootItem_SO> sameRarityCandidates = new List<LootItem_SO>();
        for (int lootIndex = 0; lootIndex < context.lootReforgePool.Count; lootIndex++)
        {
            LootItem_SO candidate = context.lootReforgePool[lootIndex];
            if (candidate == null || candidate.rarity != context.reforgeResultLoot.rarity)
                continue;

            sameRarityCandidates.Add(candidate);
        }

        if (sameRarityCandidates.Count <= 0)
            return;

        if (context.bonusLootReforgeRewards == null)
            context.bonusLootReforgeRewards = new List<LootItem_SO>();

        for (int rewardIndex = 0; rewardIndex < bonusLootCount; rewardIndex++)
        {
            LootItem_SO bonusLoot = sameRarityCandidates[Random.Range(0, sameRarityCandidates.Count)];
            if (bonusLoot == null)
                continue;

            context.bonusLootReforgeRewards.Add(bonusLoot);
            AddRelicFeedback(context, relicData, $"额外获得了 {GetLootDisplayName(bonusLoot)}。");
        }
        return;
    }

    private static void HandleIncreaseRestReforgeLimit(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if (context.trigger != Relic_SO.RelicTrigger.OnRestReforgeLimitCalculated)
            return;

        int addedReforgeCount = Mathf.Max(0, effect.amount * Mathf.Max(1, relicState.amount));
        if (addedReforgeCount <= 0)
            return;

        context.additionalRestReforgeCount += addedReforgeCount;
        return;
    }

    private static void HandleGrantExtraRandomLootOfSameRarityAfterBattle(
        Relic_SO relicData,
        RunRelicState relicState,
        RelicExecutionContext context,
        Relic_SO.RelicBuiltInEffectEntry effect)
    {
        if (context.trigger != Relic_SO.RelicTrigger.OnBattleLootRewardGranted)
            return;
        if (context.grantedBattleLoot == null || context.battleLootPool == null || context.battleLootPool.Count == 0)
            return;

        List<LootItem_SO> sameRarityLootCandidates = new List<LootItem_SO>();
        for (int candidateIndex = 0; candidateIndex < context.battleLootPool.Count; candidateIndex++)
        {
            LootItem_SO candidate = context.battleLootPool[candidateIndex];
            if (candidate != null && candidate.rarity == context.grantedBattleLoot.rarity)
                sameRarityLootCandidates.Add(candidate);
        }

        if (sameRarityLootCandidates.Count <= 0)
            return;

        if (context.bonusBattleLootRewards == null)
            context.bonusBattleLootRewards = new List<LootItem_SO>();

        int extraBattleLootCount = Mathf.Max(1, effect.amount * Mathf.Max(1, relicState.amount));
        for (int rewardIndex = 0; rewardIndex < extraBattleLootCount; rewardIndex++)
        {
            LootItem_SO bonusLoot = sameRarityLootCandidates[Random.Range(0, sameRarityLootCandidates.Count)];
            if (bonusLoot != null)
                context.bonusBattleLootRewards.Add(bonusLoot);
        }
        return;
    }
}
