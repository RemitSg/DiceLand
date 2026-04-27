using System;
using System.Collections.Generic;

public sealed class RunSaveAssetCatalogBuildRequest
{
    public DifficultyConfig_SO difficultyConfig;
    public DifficultyConfig_SO activeDifficultyConfig;
    public DifficultyConfig_SO selectedDifficultyConfig;
    public IReadOnlyList<RunGameManager.StartingPartyMemberDefinition> startingPartyMembers;
    public IReadOnlyList<RunGameManager.StartingLootDefinition> startingLoot;
    public IReadOnlyList<RunGameManager.StartingWeaponDefinition> startingWeapons;
    public IReadOnlyList<RunGameManager.StartingBlueprintDefinition> startingBlueprints;
    public IReadOnlyList<RunGameManager.StartingRelicDefinition> startingRelics;
    public CharacterRoster_SO recruitCharacterRoster;
    public EnemyRoster_SO enemyRoster;
    public MapEventPool_SO eventPool;
    public MerchantCatalog_SO merchantCatalog;
    public CalamityPool_SO calamityPool;
    public RunState runState;
    public BoardManager boardManager;
}

public static class RunSaveAssetCatalogBuilder
{
    public static MainMenuSaveAssetCatalog Build(RunSaveAssetCatalogBuildRequest request)
    {
        MainMenuSaveAssetCatalog catalog = new MainMenuSaveAssetCatalog();
        if (request == null)
            return catalog;

        AddDifficultyAssets(catalog, request.difficultyConfig);
        AddDifficultyAssets(catalog, request.activeDifficultyConfig);
        AddDifficultyAssets(catalog, request.selectedDifficultyConfig);
        AddStartingAssets(catalog, request);
        AddRecruitRosterAssets(catalog, request.recruitCharacterRoster);
        AddEnemyRosterAssets(catalog, request.enemyRoster);
        AddEventPoolAssets(catalog, request.eventPool);
        AddMerchantCatalogAssets(catalog, request.merchantCatalog);
        AddCalamityPoolAssets(catalog, request.calamityPool);
        AddRunStateAssets(catalog, request.runState);
        AddBoardAssets(catalog, request.boardManager);
        return catalog;
    }

    private static void AddStartingAssets(MainMenuSaveAssetCatalog catalog, RunSaveAssetCatalogBuildRequest request)
    {
        if (request.startingPartyMembers != null)
        {
            for (int i = 0; i < request.startingPartyMembers.Count; i++)
            {
                RunGameManager.StartingPartyMemberDefinition definition = request.startingPartyMembers[i];
                if (definition == null)
                    continue;

                AddCharacterAssets(catalog, definition.characterData);
                AddWeaponAssets(catalog, definition.equippedWeapon);
            }
        }

        if (request.startingLoot != null)
        {
            for (int i = 0; i < request.startingLoot.Count; i++)
            {
                RunGameManager.StartingLootDefinition definition = request.startingLoot[i];
                if (definition != null)
                    catalog.Add(definition.lootItem);
            }
        }

        if (request.startingWeapons != null)
        {
            for (int i = 0; i < request.startingWeapons.Count; i++)
            {
                RunGameManager.StartingWeaponDefinition definition = request.startingWeapons[i];
                if (definition != null)
                    AddWeaponAssets(catalog, definition.weaponData);
            }
        }

        if (request.startingBlueprints != null)
        {
            for (int i = 0; i < request.startingBlueprints.Count; i++)
            {
                RunGameManager.StartingBlueprintDefinition definition = request.startingBlueprints[i];
                if (definition != null)
                    AddBlueprintAssets(catalog, definition.blueprintData);
            }
        }

        if (request.startingRelics != null)
        {
            for (int i = 0; i < request.startingRelics.Count; i++)
            {
                RunGameManager.StartingRelicDefinition definition = request.startingRelics[i];
                if (definition != null)
                    catalog.Add(definition.relicData);
            }
        }
    }

    private static void AddRecruitRosterAssets(MainMenuSaveAssetCatalog catalog, CharacterRoster_SO recruitCharacterRoster)
    {
        if (recruitCharacterRoster == null || recruitCharacterRoster.Characters == null)
            return;

        for (int i = 0; i < recruitCharacterRoster.Characters.Count; i++)
            AddCharacterAssets(catalog, recruitCharacterRoster.Characters[i]);
    }

    private static void AddEnemyRosterAssets(MainMenuSaveAssetCatalog catalog, EnemyRoster_SO enemyRoster)
    {
        if (enemyRoster == null)
            return;

        IReadOnlyList<EnemyRoster_SO.DangerPool> pools = enemyRoster.Pools;
        if (pools != null)
        {
            for (int i = 0; i < pools.Count; i++)
            {
                EnemyRoster_SO.DangerPool pool = pools[i];
                if (pool == null)
                    continue;

                AddEnemyGroupList(catalog, pool.standardGroups);
                AddEnemyGroupList(catalog, pool.eliteGroups);
            }
        }

        AddEnemyGroupList(catalog, enemyRoster.BossGroups);
    }

    private static void AddEventPoolAssets(MainMenuSaveAssetCatalog catalog, MapEventPool_SO eventPool)
    {
        if (eventPool == null)
            return;

        IReadOnlyList<MapEvent_SO> events = eventPool.GetValidEvents();
        for (int i = 0; i < events.Count; i++)
            AddEventAssets(catalog, events[i]);
    }

    private static void AddCalamityPoolAssets(MainMenuSaveAssetCatalog catalog, CalamityPool_SO calamityPool)
    {
        if (calamityPool == null || calamityPool.CalamityTiers == null)
            return;

        for (int i = 0; i < calamityPool.CalamityTiers.Count; i++)
        {
            CalamityPool_SO.CalamityTierEntry tierEntry = calamityPool.CalamityTiers[i];
            if (tierEntry == null || tierEntry.calamities == null)
                continue;

            for (int calamityIndex = 0; calamityIndex < tierEntry.calamities.Count; calamityIndex++)
                catalog.Add(tierEntry.calamities[calamityIndex]);
        }
    }

    private static void AddRunStateAssets(MainMenuSaveAssetCatalog catalog, RunState runState)
    {
        if (runState == null)
            return;

        AddRunPartyAssets(catalog, runState.partyMembers);
        AddRunLootAssets(catalog, runState.lootInventory);
        AddRunWeaponAssets(catalog, runState.weaponInventory);
        AddRunBlueprintAssets(catalog, runState.blueprintInventory);
        AddRunRelicAssets(catalog, runState.relicInventory);
        AddRunCalamityAssets(catalog, runState.activeCalamities);
    }

    private static void AddBoardAssets(MainMenuSaveAssetCatalog catalog, BoardManager boardManager)
    {
        if (boardManager == null)
            return;

        foreach (BoardTileData tile in boardManager.GetAllTiles())
        {
            if (tile == null)
                continue;

            catalog.Add(tile.assignedEnemyGroup);
            catalog.Add(tile.assignedEvent);

            if (tile.merchantOffers == null)
                continue;

            for (int i = 0; i < tile.merchantOffers.Count; i++)
                AddMerchantOfferAssets(catalog, tile.merchantOffers[i]);
        }
    }

    private static void AddRunPartyAssets(MainMenuSaveAssetCatalog catalog, IReadOnlyList<RunPartyMemberState> partyMembers)
    {
        if (partyMembers == null)
            return;

        for (int i = 0; i < partyMembers.Count; i++)
        {
            RunPartyMemberState member = partyMembers[i];
            if (member == null)
                continue;

            AddCharacterAssets(catalog, member.characterData);
            AddWeaponAssets(catalog, member.weaponData);
        }
    }

    private static void AddRunLootAssets(MainMenuSaveAssetCatalog catalog, IReadOnlyList<RunLootState> lootInventory)
    {
        if (lootInventory == null)
            return;

        for (int i = 0; i < lootInventory.Count; i++)
        {
            RunLootState loot = lootInventory[i];
            if (loot != null)
                catalog.Add(loot.lootItem);
        }
    }

    private static void AddRunWeaponAssets(MainMenuSaveAssetCatalog catalog, IReadOnlyList<RunWeaponState> weaponInventory)
    {
        if (weaponInventory == null)
            return;

        for (int i = 0; i < weaponInventory.Count; i++)
        {
            RunWeaponState weapon = weaponInventory[i];
            if (weapon != null)
                AddWeaponAssets(catalog, weapon.weaponData);
        }
    }

    private static void AddRunBlueprintAssets(MainMenuSaveAssetCatalog catalog, IReadOnlyList<RunBlueprintState> blueprintInventory)
    {
        if (blueprintInventory == null)
            return;

        for (int i = 0; i < blueprintInventory.Count; i++)
        {
            RunBlueprintState blueprint = blueprintInventory[i];
            if (blueprint != null)
                AddBlueprintAssets(catalog, blueprint.blueprintData);
        }
    }

    private static void AddRunRelicAssets(MainMenuSaveAssetCatalog catalog, IReadOnlyList<RunRelicState> relicInventory)
    {
        if (relicInventory == null)
            return;

        for (int i = 0; i < relicInventory.Count; i++)
        {
            RunRelicState relic = relicInventory[i];
            if (relic != null)
                catalog.Add(relic.relicData);
        }
    }

    private static void AddRunCalamityAssets(MainMenuSaveAssetCatalog catalog, IReadOnlyList<RunCalamityState> activeCalamities)
    {
        if (activeCalamities == null)
            return;

        for (int i = 0; i < activeCalamities.Count; i++)
        {
            RunCalamityState calamity = activeCalamities[i];
            if (calamity != null)
                catalog.Add(calamity.calamityData);
        }
    }

    private static void AddMerchantOfferAssets(MainMenuSaveAssetCatalog catalog, MerchantOfferState offer)
    {
        if (offer == null)
            return;

        catalog.Add(offer.lootItem);
        AddWeaponAssets(catalog, offer.weaponData);
        AddBlueprintAssets(catalog, offer.blueprintData);
        catalog.Add(offer.relicData);
    }

    private static void AddDifficultyAssets(MainMenuSaveAssetCatalog catalog, DifficultyConfig_SO config)
    {
        if (catalog == null || config == null)
            return;

        if (!string.IsNullOrWhiteSpace(config.difficultyName) && config.difficultyName.Equals("Easy", StringComparison.OrdinalIgnoreCase))
        {
            // no-op, kept to preserve previous catalog ordering behavior.
        }
    }

    private static void AddCharacterAssets(MainMenuSaveAssetCatalog catalog, CharacterData_SO characterData)
    {
        if (catalog == null || characterData == null)
            return;

        catalog.Add(characterData);
        AddWeaponAssets(catalog, characterData.initialWeaponData);
    }

    private static void AddWeaponAssets(MainMenuSaveAssetCatalog catalog, Weapon_SO weaponData)
    {
        if (catalog == null || weaponData == null)
            return;

        catalog.Add(weaponData);
    }

    private static void AddBlueprintAssets(MainMenuSaveAssetCatalog catalog, CraftingBlueprint_SO blueprintData)
    {
        if (catalog == null || blueprintData == null)
            return;

        catalog.Add(blueprintData);
        AddWeaponAssets(catalog, blueprintData.targetWeapon);

        if (blueprintData.materialRequirements == null)
            return;

        for (int i = 0; i < blueprintData.materialRequirements.Count; i++)
        {
            CraftingMaterialRequirement requirement = blueprintData.materialRequirements[i];
            if (requirement != null)
                catalog.Add(requirement.lootItem);
        }
    }

    private static void AddEnemyGroupList(MainMenuSaveAssetCatalog catalog, IReadOnlyList<EnemyGroup_SO> groups)
    {
        if (groups == null)
            return;

        for (int i = 0; i < groups.Count; i++)
            AddEnemyGroupAssets(catalog, groups[i]);
    }

    private static void AddEnemyGroupAssets(MainMenuSaveAssetCatalog catalog, EnemyGroup_SO enemyGroup)
    {
        if (catalog == null || enemyGroup == null)
            return;

        catalog.Add(enemyGroup);

        IReadOnlyList<EnemyData_SO> enemies = enemyGroup.GetEnemies();
        for (int i = 0; i < enemies.Count; i++)
            AddEnemyDataAssets(catalog, enemies[i]);

        IReadOnlyList<LootTable_SO> lootTables = enemyGroup.GetLootTables();
        for (int i = 0; i < lootTables.Count; i++)
            AddLootTableAssets(catalog, lootTables[i]);
    }

    private static void AddEnemyDataAssets(MainMenuSaveAssetCatalog catalog, EnemyData_SO enemyData)
    {
        if (enemyData == null)
            return;

        if (enemyData.onSpecificEnemyDeathStatusGains == null)
            return;

        for (int i = 0; i < enemyData.onSpecificEnemyDeathStatusGains.Count; i++)
        {
            EnemyData_SO.OnSpecificEnemyDeathStatusGainEntry entry = enemyData.onSpecificEnemyDeathStatusGains[i];
            AddEnemyDataAssets(catalog, entry.watchedEnemyData);
        }
    }

    private static void AddLootTableAssets(MainMenuSaveAssetCatalog catalog, LootTable_SO lootTable)
    {
        if (catalog == null || lootTable == null)
            return;

        IReadOnlyList<LootTable_SO.LootDropEntry> drops = lootTable.Drops;
        for (int i = 0; i < drops.Count; i++)
        {
            LootTable_SO.LootDropEntry entry = drops[i];
            if (entry != null)
                catalog.Add(entry.lootItem);
        }

        IReadOnlyList<LootTable_SO.RelicDropEntry> relicDrops = lootTable.RelicDrops;
        for (int i = 0; i < relicDrops.Count; i++)
        {
            LootTable_SO.RelicDropEntry entry = relicDrops[i];
            if (entry == null || entry.relicTable == null)
                continue;

            IReadOnlyList<Relic_SO> relics = entry.relicTable.Relics;
            for (int relicIndex = 0; relicIndex < relics.Count; relicIndex++)
                catalog.Add(relics[relicIndex]);
        }
    }

    private static void AddEventAssets(MainMenuSaveAssetCatalog catalog, MapEvent_SO eventData)
    {
        if (catalog == null || eventData == null)
            return;

        catalog.Add(eventData);
        IReadOnlyList<MapEventOptionData> options = eventData.GetValidOptions();
        for (int i = 0; i < options.Count; i++)
            AddEventOptionAssets(catalog, options[i]);
    }

    private static void AddEventOptionAssets(MainMenuSaveAssetCatalog catalog, MapEventOptionData optionData)
    {
        if (catalog == null || optionData == null)
            return;

        AddEventEffectAssets(catalog, MapEventEffectSequenceBuilder.BuildPrimaryEffect(optionData));

        if (optionData.additionalBuiltInEffects == null)
            return;

        for (int i = 0; i < optionData.additionalBuiltInEffects.Count; i++)
            AddEventEffectAssets(catalog, optionData.additionalBuiltInEffects[i]);
    }

    private static void AddEventEffectAssets(MainMenuSaveAssetCatalog catalog, MapEventBuiltInEffectData effectData)
    {
        if (catalog == null || effectData == null)
            return;

        AddEventLootRewardAssets(catalog, effectData.lootRewards);
        AddEventRelicRewardAssets(catalog, effectData.relicRewards);
        AddEventBlueprintRewardAssets(catalog, effectData.blueprintRewards);
        AddEnemyGroupList(catalog, effectData.encounterEnemyGroups);

        if (effectData.encounterEnemies == null)
            return;

        for (int i = 0; i < effectData.encounterEnemies.Count; i++)
            AddEnemyDataAssets(catalog, effectData.encounterEnemies[i]);
    }

    private static void AddEventLootRewardAssets(MainMenuSaveAssetCatalog catalog, IReadOnlyList<MapEventLootRewardEntry> rewards)
    {
        if (rewards == null)
            return;

        for (int i = 0; i < rewards.Count; i++)
        {
            MapEventLootRewardEntry entry = rewards[i];
            if (entry != null)
                catalog.Add(entry.lootItem);
        }
    }

    private static void AddEventRelicRewardAssets(MainMenuSaveAssetCatalog catalog, IReadOnlyList<MapEventRelicRewardEntry> rewards)
    {
        if (rewards == null)
            return;

        for (int i = 0; i < rewards.Count; i++)
        {
            MapEventRelicRewardEntry entry = rewards[i];
            if (entry != null)
                catalog.Add(entry.relicData);
        }
    }

    private static void AddEventBlueprintRewardAssets(MainMenuSaveAssetCatalog catalog, IReadOnlyList<MapEventBlueprintRewardEntry> rewards)
    {
        if (rewards == null)
            return;

        for (int i = 0; i < rewards.Count; i++)
        {
            MapEventBlueprintRewardEntry entry = rewards[i];
            if (entry != null)
                AddBlueprintAssets(catalog, entry.blueprintData);
        }
    }

    private static void AddMerchantCatalogAssets(MainMenuSaveAssetCatalog catalog, MerchantCatalog_SO merchantCatalogData)
    {
        if (catalog == null || merchantCatalogData == null)
            return;

        IReadOnlyList<MerchantCatalog_SO.LootGoodsEntry> lootEntries = merchantCatalogData.GetValidLootEntries();
        for (int i = 0; i < lootEntries.Count; i++)
            catalog.Add(lootEntries[i].lootItem);

        IReadOnlyList<MerchantCatalog_SO.WeaponGoodsEntry> weaponEntries = merchantCatalogData.GetValidWeaponEntries();
        for (int i = 0; i < weaponEntries.Count; i++)
            AddWeaponAssets(catalog, weaponEntries[i].weaponData);

        IReadOnlyList<MerchantCatalog_SO.BlueprintGoodsEntry> blueprintEntries = merchantCatalogData.GetValidBlueprintEntries();
        for (int i = 0; i < blueprintEntries.Count; i++)
            AddBlueprintAssets(catalog, blueprintEntries[i].blueprintData);

        IReadOnlyList<MerchantCatalog_SO.RelicGoodsEntry> relicEntries = merchantCatalogData.GetValidRelicEntries();
        for (int i = 0; i < relicEntries.Count; i++)
            AddMerchantRelicEntryAssets(catalog, relicEntries[i]);
    }

    private static void AddMerchantRelicEntryAssets(MainMenuSaveAssetCatalog catalog, MerchantCatalog_SO.RelicGoodsEntry entry)
    {
        if (entry == null)
            return;

        if (entry.relicTable != null && entry.relicTable.Relics != null)
        {
            IReadOnlyList<Relic_SO> relics = entry.relicTable.Relics;
            for (int relicIndex = 0; relicIndex < relics.Count; relicIndex++)
                catalog.Add(relics[relicIndex]);
            return;
        }

        catalog.Add(entry.LegacyRelicData);
    }
}
