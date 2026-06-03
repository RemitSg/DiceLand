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
    public TreasureRewardTable_SO treasureRewardTable;
    public RunState runState;
    public BoardManager boardManager;
    public IReadOnlyList<BoardTileData> boardTiles;
    public SpecialDungeonRuntimeContext specialDungeonContext;
}

public static class RunSaveAssetCatalogBuilder
{
    private const int MaxNestedEventAssetDepth = 16;

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
        AddTreasureRewardTableAssets(catalog, request.treasureRewardTable);
        AddRunStateAssets(catalog, request.runState);
        AddBoardAssets(catalog, request.boardManager);
        AddBoardAssets(catalog, request.boardTiles);
        AddSpecialDungeonContextAssets(catalog, request.specialDungeonContext);
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

    private static void AddTreasureRewardTableAssets(MainMenuSaveAssetCatalog catalog, TreasureRewardTable_SO treasureRewardTable)
    {
        if (catalog == null || treasureRewardTable == null)
            return;

        AddEventBlueprintRewardAssets(catalog, treasureRewardTable.BlueprintTable);
        AddEventRelicRewardAssets(catalog, treasureRewardTable.RelicTable);
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
            AddBoardTileAssets(catalog, tile);
    }

    private static void AddBoardAssets(MainMenuSaveAssetCatalog catalog, IReadOnlyList<BoardTileData> boardTiles)
    {
        if (boardTiles == null)
            return;

        for (int i = 0; i < boardTiles.Count; i++)
            AddBoardTileAssets(catalog, boardTiles[i]);
    }

    private static void AddSnapshotAssets(MainMenuSaveAssetCatalog catalog, MapRunSnapshot snapshot, bool includeSpecialDungeonContext = true)
    {
        if (catalog == null || snapshot == null)
            return;

        AddRunStateAssets(catalog, snapshot.runState);
        if (snapshot.tiles != null)
        {
            for (int i = 0; i < snapshot.tiles.Count; i++)
                AddBoardTileAssets(catalog, snapshot.tiles[i]);
        }

        if (includeSpecialDungeonContext)
            AddSpecialDungeonContextAssets(catalog, snapshot.specialDungeonContext);
    }

    private static void AddBoardTileAssets(MainMenuSaveAssetCatalog catalog, BoardTileData tile)
    {
        if (catalog == null || tile == null)
            return;

        catalog.Add(tile.assignedEnemyGroup);
        catalog.Add(tile.assignedEvent);
        AddSpecialDungeonAssets(catalog, tile.sourceSpecialDungeon);

        if (tile.specialDungeonTreasureOptions != null)
        {
            for (int i = 0; i < tile.specialDungeonTreasureOptions.Count; i++)
                AddSpecialDungeonTreasureOptionAssets(catalog, tile.specialDungeonTreasureOptions[i]);
        }

        if (tile.merchantOffers != null)
        {
            for (int i = 0; i < tile.merchantOffers.Count; i++)
                AddMerchantOfferAssets(catalog, tile.merchantOffers[i]);
        }
    }

    private static void AddSpecialDungeonContextAssets(MainMenuSaveAssetCatalog catalog, SpecialDungeonRuntimeContext context)
    {
        if (catalog == null || context == null)
            return;

        AddSpecialDungeonAssets(catalog, context.activeDungeon);
        AddSnapshotAssets(catalog, context.mainMapSnapshot, includeSpecialDungeonContext: false);
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

        HashSet<EnemyData_SO> visitedEnemyData = new HashSet<EnemyData_SO>();
        IReadOnlyList<EnemyData_SO> enemies = enemyGroup.GetEnemies();
        for (int i = 0; i < enemies.Count; i++)
            AddEnemyDataAssets(catalog, enemies[i], visitedEnemyData);

        IReadOnlyList<LootTable_SO> lootTables = enemyGroup.GetLootTables();
        for (int i = 0; i < lootTables.Count; i++)
            AddLootTableAssets(catalog, lootTables[i]);
    }

    private static void AddEnemyDataAssets(MainMenuSaveAssetCatalog catalog, EnemyData_SO enemyData)
    {
        AddEnemyDataAssets(catalog, enemyData, new HashSet<EnemyData_SO>());
    }

    private static void AddEnemyDataAssets(
        MainMenuSaveAssetCatalog catalog,
        EnemyData_SO enemyData,
        HashSet<EnemyData_SO> visitedEnemyData)
    {
        if (catalog == null || enemyData == null)
            return;

        if (visitedEnemyData == null)
            visitedEnemyData = new HashSet<EnemyData_SO>();

        if (!visitedEnemyData.Add(enemyData))
            return;

        AddEnemyDataAssets(catalog, enemyData.transformToEnemyDataWhenStatusMissing, visitedEnemyData);

        if (enemyData.onSpecificEnemyDeathStatusGains == null)
            return;

        for (int i = 0; i < enemyData.onSpecificEnemyDeathStatusGains.Count; i++)
        {
            EnemyData_SO.OnSpecificEnemyDeathStatusGainEntry entry = enemyData.onSpecificEnemyDeathStatusGains[i];
            AddEnemyDataAssets(catalog, entry.watchedEnemyData, visitedEnemyData);
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
        AddEventAssets(catalog, eventData, 0, new HashSet<MapEvent_SO>());
    }

    private static void AddEventAssets(
        MainMenuSaveAssetCatalog catalog,
        MapEvent_SO eventData,
        int depth,
        HashSet<MapEvent_SO> visitedEvents)
    {
        if (catalog == null || eventData == null)
            return;
        if (depth > MaxNestedEventAssetDepth)
            return;
        if (visitedEvents == null)
            visitedEvents = new HashSet<MapEvent_SO>();
        if (!visitedEvents.Add(eventData))
            return;

        catalog.Add(eventData);
        IReadOnlyList<MapEventOptionData> options = eventData.GetValidOptions();
        for (int i = 0; i < options.Count; i++)
            AddEventOptionAssets(catalog, options[i], depth, visitedEvents);
    }

    private static void AddEventOptionAssets(MainMenuSaveAssetCatalog catalog, MapEventOptionData optionData)
    {
        AddEventOptionAssets(catalog, optionData, 0, new HashSet<MapEvent_SO>());
    }

    private static void AddEventOptionAssets(
        MainMenuSaveAssetCatalog catalog,
        MapEventOptionData optionData,
        int depth,
        HashSet<MapEvent_SO> visitedEvents)
    {
        if (catalog == null || optionData == null)
            return;

        AddEventEffectAssets(catalog, MapEventEffectSequenceBuilder.BuildPrimaryEffect(optionData));
        AddEventAssets(catalog, optionData.nextEvent, depth + 1, visitedEvents);

        if (optionData.additionalBuiltInEffects != null)
        {
            for (int i = 0; i < optionData.additionalBuiltInEffects.Count; i++)
                AddEventEffectAssets(catalog, optionData.additionalBuiltInEffects[i]);
        }

        if (optionData.possibleResults == null)
            return;

        for (int i = 0; i < optionData.possibleResults.Count; i++)
        {
            MapEventPossibleResultData possibleResult = optionData.possibleResults[i];
            if (possibleResult == null)
                continue;

            AddEventAssets(catalog, possibleResult.nextEvent, depth + 1, visitedEvents);
            List<MapEventBuiltInEffectData> resultEffects = MapEventEffectSequenceBuilder.BuildAllEffects(possibleResult);
            for (int j = 0; j < resultEffects.Count; j++)
                AddEventEffectAssets(catalog, resultEffects[j]);
        }
    }

    private static void AddEventEffectAssets(MainMenuSaveAssetCatalog catalog, MapEventBuiltInEffectData effectData)
    {
        if (catalog == null || effectData == null)
            return;

        AddEventLootRewardAssets(catalog, effectData.lootRewards);
        AddEventRelicRewardAssets(catalog, effectData.relicTable);
        AddExplicitEventRelicRewardAssets(catalog, effectData.relicRewards);
        AddEventBlueprintRewardAssets(catalog, effectData.blueprintTable);
        AddEventWeaponCatalogAssets(catalog, effectData.weaponCatalog);
        AddEnemyGroupList(catalog, effectData.encounterEnemyGroups);
        AddSpecialDungeonAssets(catalog, effectData.specialDungeon);

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

    private static void AddEventRelicRewardAssets(MainMenuSaveAssetCatalog catalog, RelicTable_SO relicTable)
    {
        if (relicTable == null || relicTable.Relics == null)
            return;

        IReadOnlyList<Relic_SO> relics = relicTable.Relics;
        for (int i = 0; i < relics.Count; i++)
            catalog.Add(relics[i]);
    }

    private static void AddExplicitEventRelicRewardAssets(MainMenuSaveAssetCatalog catalog, IReadOnlyList<MapEventRelicRewardEntry> rewards)
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

    private static void AddEventWeaponCatalogAssets(MainMenuSaveAssetCatalog catalog, WeaponCatalog_SO weaponCatalog)
    {
        if (catalog == null || weaponCatalog == null || weaponCatalog.Weapons == null)
            return;

        IReadOnlyList<WeaponCatalog_SO.WeaponEntry> entries = weaponCatalog.Weapons;
        for (int i = 0; i < entries.Count; i++)
        {
            WeaponCatalog_SO.WeaponEntry entry = entries[i];
            if (entry != null)
                AddWeaponAssets(catalog, entry.weapon);
        }
    }

    private static void AddEventBlueprintRewardAssets(MainMenuSaveAssetCatalog catalog, BlueprintTable_SO blueprintTable)
    {
        if (blueprintTable == null)
            return;

        IReadOnlyList<CraftingBlueprint_SO> blueprints = blueprintTable.GetValidBlueprints();
        for (int i = 0; i < blueprints.Count; i++)
            AddBlueprintAssets(catalog, blueprints[i]);
    }

    private static void AddSpecialDungeonAssets(MainMenuSaveAssetCatalog catalog, SpecialDungeon_SO dungeon)
    {
        if (catalog == null || dungeon == null)
            return;

        catalog.Add(dungeon);
        IReadOnlyList<SpecialDungeonTileEntry> entries = dungeon.GetValidEntries();
        for (int i = 0; i < entries.Count; i++)
            AddSpecialDungeonEntryAssets(catalog, entries[i]);
    }

    private static void AddSpecialDungeonEntryAssets(MainMenuSaveAssetCatalog catalog, SpecialDungeonTileEntry entry)
    {
        if (catalog == null || entry == null)
            return;

        AddEnemyGroupAssets(catalog, entry.enemyGroup);
        AddEventAssets(catalog, entry.eventData);

        if (entry.merchantOffers != null)
        {
            for (int i = 0; i < entry.merchantOffers.Count; i++)
                AddSpecialDungeonMerchantOfferAssets(catalog, entry.merchantOffers[i]);
        }

        if (entry.treasureOptions != null)
        {
            for (int i = 0; i < entry.treasureOptions.Count; i++)
                AddSpecialDungeonTreasureOptionAssets(catalog, entry.treasureOptions[i]);
        }
    }

    private static void AddSpecialDungeonMerchantOfferAssets(MainMenuSaveAssetCatalog catalog, SpecialDungeonMerchantOfferEntry offer)
    {
        if (offer == null)
            return;

        catalog.Add(offer.lootItem);
        AddWeaponAssets(catalog, offer.weaponData);
        AddBlueprintAssets(catalog, offer.blueprintData);
        catalog.Add(offer.relicData);
    }

    private static void AddSpecialDungeonTreasureOptionAssets(MainMenuSaveAssetCatalog catalog, SpecialDungeonTreasureOptionEntry option)
    {
        if (option == null)
            return;

        AddBlueprintAssets(catalog, option.blueprintData);
        catalog.Add(option.relicData);
        catalog.Add(option.relicTable);
        AddEventRelicRewardAssets(catalog, option.relicTable);
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
