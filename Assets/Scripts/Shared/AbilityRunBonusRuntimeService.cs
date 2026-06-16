using System.Collections.Generic;
using UnityEngine;

public static class AbilityRunBonusRuntimeService
{
    private const int StartingLootSeedSalt = unchecked((int)0xA81117);
    private const int StartingBlueprintSeedSalt = unchecked((int)0xB17E);
    private const int StartingRelicBagSeedSalt = unchecked((int)0x7E11CB46);

    public static void ApplyPersistentRunBonuses(RunState runState, AbilityRunBonus bonus)
    {
        if (runState == null || bonus == null)
            return;

        runState.abilityMerchantFreeRefreshPerShop = Mathf.Max(0, bonus.merchantFreeRefreshPerShop);
        runState.abilityFirstMerchantPurchaseHalfDiscountAvailable = bonus.firstMerchantPurchaseHalfDiscount;
        runState.abilityFirstMerchantPurchaseHalfDiscountConsumed = false;
        runState.abilityBattleStartTemporaryEnergy = Mathf.Max(0, bonus.battleStartTemporaryEnergy);
        runState.abilityBattleStartEnergyMaxFlat = Mathf.Max(0, bonus.battleStartEnergyMaxFlat);
        ApplyMaxHealthBonus(runState, Mathf.Max(0, bonus.battleStartMaxHealthFlat));
        runState.abilityBattleStartMaxHealthFlat = 0;
        runState.abilityRelicChoiceRefreshCount = Mathf.Max(0, bonus.relicChoiceRefreshCount);
        runState.abilityTreasureRewardTableOverride = bonus.treasureRewardTableOverride;
    }

    private static void ApplyMaxHealthBonus(RunState runState, int maxHealthBonus)
    {
        if (runState == null || maxHealthBonus <= 0 || runState.partyMembers == null)
            return;

        for (int i = 0; i < runState.partyMembers.Count; i++)
            RunPartyMemberMaxHpService.AddPreCalamityMaxHp(runState.partyMembers[i], maxHealthBonus, runState.activeCalamities);
    }

    public static int ResolveMerchantRefreshCost(RunState runState, BoardTileData tile, int baseCost)
    {
        int safeBaseCost = Mathf.Max(0, baseCost);
        if (runState == null || tile == null)
            return safeBaseCost;

        int freeRefreshes = Mathf.Max(0, runState.abilityMerchantFreeRefreshPerShop);
        return Mathf.Max(0, tile.merchantRefreshCount) < freeRefreshes ? 0 : safeBaseCost;
    }

    public static int ResolveMerchantPurchasePrice(RunState runState, int basePrice)
    {
        int safeBasePrice = Mathf.Max(0, basePrice);
        if (runState == null)
            return safeBasePrice;

        if (!runState.abilityFirstMerchantPurchaseHalfDiscountAvailable || runState.abilityFirstMerchantPurchaseHalfDiscountConsumed)
            return safeBasePrice;

        return Mathf.Max(0, Mathf.CeilToInt(safeBasePrice * 0.5f));
    }

    public static void NotifyMerchantPurchaseCompleted(RunState runState, int paidPrice)
    {
        if (runState == null)
            return;
        if (!runState.abilityFirstMerchantPurchaseHalfDiscountAvailable || runState.abilityFirstMerchantPurchaseHalfDiscountConsumed)
            return;

        runState.abilityFirstMerchantPurchaseHalfDiscountConsumed = true;
    }

    public static TreasureRewardTable_SO ResolveTreasureRewardTable(RunState runState, TreasureRewardTable_SO fallback)
    {
        if (runState != null && runState.abilityTreasureRewardTableOverride != null)
            return runState.abilityTreasureRewardTableOverride;

        return fallback;
    }

    public static void ApplyRelicChoiceRefreshBonus(RunState runState, BattleSettlementRelicChoiceData relicChoiceData)
    {
        if (runState == null || relicChoiceData == null)
            return;

        relicChoiceData.refreshesRemaining += Mathf.Max(0, runState.abilityRelicChoiceRefreshCount);
    }

    public static void ApplyRelicChoiceRefreshBonus(BattleEncounterData encounter, BattleSettlementRelicChoiceData relicChoiceData)
    {
        if (encounter == null || relicChoiceData == null)
            return;

        relicChoiceData.refreshesRemaining += Mathf.Max(0, encounter.abilityRelicChoiceRefreshCount);
    }

    public static void ApplyStartingBlueprints(RunState runState, AbilityRunBonus bonus, int seed = 0)
    {
        if (runState == null || bonus == null || bonus.startingBlueprints == null)
            return;

        for (int i = 0; i < bonus.startingBlueprints.Count; i++)
        {
            AbilityStartingBlueprintBonus entry = bonus.startingBlueprints[i];
            if (entry == null)
                continue;

            if (entry.blueprintTable != null)
            {
                RollBlueprintTableIntoRun(runState, entry.blueprintTable, Mathf.Max(1, entry.rollCount), CreateBlueprintRandom(seed, i));
            }
            else if (entry.blueprintData != null)
            {
                runState.AddBlueprint(entry.blueprintData, Mathf.Max(1, entry.amount));
            }
        }
    }

    public static void ApplyStartingLootTableRolls(RunState runState, AbilityRunBonus bonus, int seed)
    {
        if (runState == null || bonus == null || bonus.startingLootTableRolls == null)
            return;

        for (int i = 0; i < bonus.startingLootTableRolls.Count; i++)
        {
            AbilityStartingLootTableRollBonus entry = bonus.startingLootTableRolls[i];
            if (entry == null || entry.lootTable == null)
                continue;

            int rollCount = Mathf.Max(1, entry.rollCount);
            for (int rollIndex = 0; rollIndex < rollCount; rollIndex++)
                RollLootTableIntoRun(runState, entry.lootTable, CreateRandom(seed, i, rollIndex));
        }
    }

    public static List<BattleSettlementRelicChoiceData> BuildStartingRelicBagChoices(RunState runState, AbilityRunBonus bonus, int seed)
    {
        List<BattleSettlementRelicChoiceData> choices = new List<BattleSettlementRelicChoiceData>();
        if (runState == null || bonus == null || bonus.startingRelicBags == null)
            return choices;

        for (int i = 0; i < bonus.startingRelicBags.Count; i++)
        {
            AbilityStartingRelicBagBonus entry = bonus.startingRelicBags[i];
            if (entry == null || entry.relicTable == null)
                continue;

            int bagCount = Mathf.Max(1, entry.bagCount);
            int choiceCount = Mathf.Max(1, entry.choiceCount);
            for (int bagIndex = 0; bagIndex < bagCount; bagIndex++)
            {
                System.Random rng = CreateRelicBagRandom(seed, i, bagIndex);
                BattleSettlementRelicChoiceData choiceData = BuildStartingRelicBagChoice(runState, entry, rng, choiceCount);
                if (choiceData == null || !choiceData.HasChoices)
                    continue;

                choiceData.title = "初始遗物袋：选择 1 个遗物";
                choiceData.requiredMessage = "请先从初始遗物袋中选择一个遗物。";
                ApplyRelicChoiceRefreshBonus(runState, choiceData);
                choices.Add(choiceData);
            }
        }

        return choices;
    }

    private static void RollLootTableIntoRun(RunState runState, LootTable_SO lootTable, System.Random rng)
    {
        Dictionary<LootItem_SO, int> aggregatedLoot = new Dictionary<LootItem_SO, int>();
        List<LootItem_SO> lootPool = new List<LootItem_SO>();
        BattleLootDropRollService.RollTableLootDrops(lootTable, rng, aggregatedLoot, lootPool);
        foreach (KeyValuePair<LootItem_SO, int> entry in aggregatedLoot)
            runState.AddLoot(entry.Key, entry.Value);

        Dictionary<Relic_SO, int> aggregatedRelics = new Dictionary<Relic_SO, int>();
        BattleRelicDropRollService.RollTableRelicDrops(
            lootTable,
            rng,
            false,
            false,
            BattleRelicInventorySetBuilder.BuildOwnedRelicSet(runState.relicInventory),
            3,
            aggregatedRelics,
            null);
        foreach (KeyValuePair<Relic_SO, int> entry in aggregatedRelics)
            runState.AddRelic(entry.Key, entry.Value);
    }

    private static BattleSettlementRelicChoiceData BuildStartingRelicBagChoice(
        RunState runState,
        AbilityStartingRelicBagBonus entry,
        System.Random rng,
        int choiceCount)
    {
        HashSet<Relic_SO> ownedRelics = BattleRelicInventorySetBuilder.BuildOwnedRelicSet(runState.relicInventory);
        if (entry.rarityWeightTable != null && entry.rarityWeightTable.HasPositiveWeights)
        {
            LootRarity rarity = SelectWeightedRarity(entry.rarityWeightTable, rng);
            if (BattleEliteRelicChoiceBuilderService.TryBuildFromRelicsWithRarity(
                    out BattleSettlementRelicChoiceData weightedChoiceData,
                    entry.relicTable.Relics,
                    rarity,
                    rng,
                    ownedRelics,
                    choiceCount))
                return weightedChoiceData;
        }

        return BattleEliteRelicChoiceBuilderService.TryBuildFromTable(
            out BattleSettlementRelicChoiceData choiceData,
            entry.relicTable,
            rng,
            ownedRelics,
            choiceCount)
            ? choiceData
            : null;
    }

    private static LootRarity SelectWeightedRarity(LootRarityWeightTable_SO weightTable, System.Random rng)
    {
        if (rng == null)
            rng = new System.Random();
        if (weightTable == null || !weightTable.HasPositiveWeights)
            return LootRarity.Common;

        float totalWeight = 0f;
        List<LootRarity> rarities = new List<LootRarity>();
        List<float> cumulative = new List<float>();
        for (int i = (int)LootRarity.Common; i <= (int)LootRarity.Legendary; i++)
        {
            LootRarity rarity = (LootRarity)i;
            float weight = Mathf.Max(0f, weightTable.GetWeight(rarity));
            if (weight <= 0f)
                continue;

            totalWeight += weight;
            rarities.Add(rarity);
            cumulative.Add(totalWeight);
        }

        if (totalWeight <= 0f || rarities.Count == 0)
            return LootRarity.Common;

        double roll = rng.NextDouble() * totalWeight;
        for (int i = 0; i < cumulative.Count; i++)
        {
            if (roll <= cumulative[i])
                return rarities[i];
        }

        return rarities[rarities.Count - 1];
    }

    private static void RollBlueprintTableIntoRun(RunState runState, BlueprintTable_SO blueprintTable, int rollCount, System.Random rng)
    {
        if (runState == null || blueprintTable == null || rng == null || rollCount <= 0)
            return;

        IReadOnlyList<CraftingBlueprint_SO> blueprints = blueprintTable.GetValidBlueprints();
        if (blueprints == null || blueprints.Count <= 0)
            return;

        for (int i = 0; i < rollCount; i++)
        {
            CraftingBlueprint_SO selected = blueprints[rng.Next(blueprints.Count)];
            if (selected != null)
                runState.AddBlueprint(selected, 1);
        }
    }

    private static System.Random CreateBlueprintRandom(int seed, int tableIndex)
    {
        unchecked
        {
            return new System.Random(seed ^ StartingBlueprintSeedSalt ^ (tableIndex * 1543));
        }
    }

    private static System.Random CreateRandom(int seed, int tableIndex, int rollIndex)
    {
        unchecked
        {
            return new System.Random(seed ^ StartingLootSeedSalt ^ (tableIndex * 397) ^ (rollIndex * 7919));
        }
    }

    private static System.Random CreateRelicBagRandom(int seed, int tableIndex, int bagIndex)
    {
        unchecked
        {
            return new System.Random(seed ^ StartingRelicBagSeedSalt ^ (tableIndex * 1549) ^ (bagIndex * 3221));
        }
    }
}
