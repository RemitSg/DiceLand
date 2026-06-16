using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public sealed class MainMenuSaveAssetCatalog
{
    private readonly Dictionary<string, CharacterData_SO> characters = new Dictionary<string, CharacterData_SO>(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Weapon_SO> weapons = new Dictionary<string, Weapon_SO>(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, LootItem_SO> lootItems = new Dictionary<string, LootItem_SO>(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, CraftingBlueprint_SO> blueprints = new Dictionary<string, CraftingBlueprint_SO>(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Relic_SO> relics = new Dictionary<string, Relic_SO>(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, RelicTable_SO> relicTables = new Dictionary<string, RelicTable_SO>(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, TreasureRewardTable_SO> treasureRewardTables = new Dictionary<string, TreasureRewardTable_SO>(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Calamity_SO> calamities = new Dictionary<string, Calamity_SO>(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, EnemyGroup_SO> enemyGroups = new Dictionary<string, EnemyGroup_SO>(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, MapEvent_SO> events = new Dictionary<string, MapEvent_SO>(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, SpecialDungeon_SO> specialDungeons = new Dictionary<string, SpecialDungeon_SO>(StringComparer.OrdinalIgnoreCase);

    public void Add(CharacterData_SO character)
    {
        Add(characters, MainMenuRunSaveSystem.GetCharacterId(character), character);
    }

    public void Add(Weapon_SO weapon)
    {
        Add(weapons, MainMenuRunSaveSystem.GetWeaponId(weapon), weapon);
    }

    public void Add(LootItem_SO loot)
    {
        Add(lootItems, MainMenuRunSaveSystem.GetLootId(loot), loot);
    }

    public void Add(CraftingBlueprint_SO blueprint)
    {
        Add(blueprints, MainMenuRunSaveSystem.GetBlueprintId(blueprint), blueprint);
    }

    public void Add(Relic_SO relic)
    {
        Add(relics, MainMenuRunSaveSystem.GetRelicId(relic), relic);
    }

    public void Add(RelicTable_SO relicTable)
    {
        Add(relicTables, MainMenuRunSaveSystem.GetRelicTableId(relicTable), relicTable);
    }

    public void Add(TreasureRewardTable_SO treasureRewardTable)
    {
        Add(treasureRewardTables, MainMenuRunSaveSystem.GetTreasureRewardTableId(treasureRewardTable), treasureRewardTable);
    }

    public void Add(Calamity_SO calamity)
    {
        Add(calamities, MainMenuRunSaveSystem.GetCalamityId(calamity), calamity);
    }

    public void Add(EnemyGroup_SO enemyGroup)
    {
        Add(enemyGroups, MainMenuRunSaveSystem.GetEnemyGroupId(enemyGroup), enemyGroup);
    }

    public void Add(MapEvent_SO eventData)
    {
        Add(events, MainMenuRunSaveSystem.GetEventId(eventData), eventData);
    }

    public void Add(SpecialDungeon_SO dungeon)
    {
        Add(specialDungeons, MainMenuRunSaveSystem.GetSpecialDungeonId(dungeon), dungeon);
    }

    public CharacterData_SO ResolveCharacter(string id) => Resolve(characters, id);
    public Weapon_SO ResolveWeapon(string id) => Resolve(weapons, id);
    public LootItem_SO ResolveLoot(string id) => Resolve(lootItems, id);
    public CraftingBlueprint_SO ResolveBlueprint(string id) => Resolve(blueprints, id);
    public Relic_SO ResolveRelic(string id) => Resolve(relics, id);
    public RelicTable_SO ResolveRelicTable(string id) => Resolve(relicTables, id);
    public TreasureRewardTable_SO ResolveTreasureRewardTable(string id) => Resolve(treasureRewardTables, id);
    public Calamity_SO ResolveCalamity(string id) => Resolve(calamities, id);
    public EnemyGroup_SO ResolveEnemyGroup(string id) => Resolve(enemyGroups, id);
    public MapEvent_SO ResolveEvent(string id) => Resolve(events, id);
    public SpecialDungeon_SO ResolveSpecialDungeon(string id) => Resolve(specialDungeons, id);

    private static void Add<T>(Dictionary<string, T> dictionary, string id, T value) where T : UnityEngine.Object
    {
        if (value == null || string.IsNullOrWhiteSpace(id))
            return;

        if (!dictionary.ContainsKey(id))
            dictionary.Add(id, value);
    }

    private static T Resolve<T>(Dictionary<string, T> dictionary, string id) where T : UnityEngine.Object
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;

        dictionary.TryGetValue(id, out T value);
        return value;
    }
}

[Serializable]
public sealed class MainMenuRunSaveMetadata
{
    public string sceneName;
    public string difficultyName;
    public string savedAtUtc;
}

[Serializable]
internal sealed class SavedRunData
{
    public MainMenuRunSaveMetadata metadata = new MainMenuRunSaveMetadata();
    public SavedRunSnapshot snapshot = new SavedRunSnapshot();
}

[Serializable]
internal sealed class SavedRunSnapshot
{
    public string sceneName;
    public SavedRunState runState = new SavedRunState();
    public List<SavedBoardTileData> tiles = new List<SavedBoardTileData>();
    public List<int> pendingCalamityTiers = new List<int>();
    public bool suppressOnePendingCalamityOnActionPointRestore;
    public int boardSize;
    public SavedVector2Int spawnCoord;
    public SavedVector2Int bossCoord;
    public SavedVector2Int selectedCoord;
    public int actionPointBase;
    public int actionPointBonus;
    public int actionPointCurrent;
    public SavedSpecialDungeonRuntimeContext specialDungeonContext;
}

[Serializable]
internal sealed class SavedMainMapSnapshot
{
    public string sceneName;
    public SavedRunState runState = new SavedRunState();
    public List<SavedBoardTileData> tiles = new List<SavedBoardTileData>();
    public List<int> pendingCalamityTiers = new List<int>();
    public bool suppressOnePendingCalamityOnActionPointRestore;
    public int boardSize;
    public SavedVector2Int spawnCoord;
    public SavedVector2Int bossCoord;
    public SavedVector2Int selectedCoord;
    public int actionPointBase;
    public int actionPointBonus;
    public int actionPointCurrent;
}

[Serializable]
internal sealed class SavedRunState
{
    public string difficultyName;
    public int boardSize;
    public SavedVector2Int playerGridPosition;
    public int money;
    public int recruitedMemberCount;
    public int currentKeys;
    public int requiredKeys;
    public int playerLevel;
    public int playerExperience;
    public int calamityTier;
    public int currentCycle;
    public List<SavedPartyMemberState> partyMembers = new List<SavedPartyMemberState>();
    public List<SavedItemStack> lootInventory = new List<SavedItemStack>();
    public List<SavedWeaponStack> weaponInventory = new List<SavedWeaponStack>();
    public List<SavedBlueprintStack> blueprintInventory = new List<SavedBlueprintStack>();
    public List<SavedRelicStack> relicInventory = new List<SavedRelicStack>();
    public List<SavedCalamityState> activeCalamities = new List<SavedCalamityState>();
    public List<string> drawnCalamityIds = new List<string>();
    public List<string> drawnEventIds = new List<string>();
    public int abilityMerchantFreeRefreshPerShop;
    public bool abilityFirstMerchantPurchaseHalfDiscountAvailable;
    public bool abilityFirstMerchantPurchaseHalfDiscountConsumed;
    public int abilityBattleStartTemporaryEnergy;
    public int abilityBattleStartEnergyMaxFlat;
    public int abilityBattleStartMaxHealthFlat;
    public int abilityRelicChoiceRefreshCount;
    public string abilityTreasureRewardTableId;
    public RunSettlementStats settlementStats = new RunSettlementStats();
}

[Serializable]
internal sealed class SavedPartyMemberState
{
    public string memberId;
    public string characterId;
    public string weaponId;
    public int level;
    public int experience;
    public int maxHPFlatBonus;
    public int preCalamityMaxHP;
    public int maxHP;
    public int currentHP;
}

[Serializable]
internal sealed class SavedItemStack
{
    public string lootId;
    public int amount;
}

[Serializable]
internal sealed class SavedWeaponStack
{
    public string weaponId;
    public int amount;
}

[Serializable]
internal sealed class SavedBlueprintStack
{
    public string blueprintId;
    public int amount;
}

[Serializable]
internal sealed class SavedRelicStack
{
    public string relicId;
    public int amount;
}

[Serializable]
internal sealed class SavedCalamityState
{
    public string calamityId;
    public int acquiredAtTier;
}

[Serializable]
internal sealed class SavedBoardTileData
{
    public SavedVector2Int coord;
    public int tileType;
    public int dangerBand;
    public bool isVisited;
    public bool isResolved;
    public bool isSpawnTile;
    public int encounterSeed;
    public int previewEnemyCount;
    public int previewDangerRating;
    public string previewTag;
    public string assignedEnemyGroupId;
    public string assignedEventId;
    public bool merchantStockGenerated;
    public int merchantRefreshCount;
    public int merchantPurchaseDiscountStacks;
    public List<SavedMerchantOfferState> merchantOffers = new List<SavedMerchantOfferState>();
    public string sourceSpecialDungeonId;
    public int specialDungeonEntryIndex = -1;
    public List<SavedSpecialDungeonTreasureOptionEntry> specialDungeonTreasureOptions = new List<SavedSpecialDungeonTreasureOptionEntry>();
}

[Serializable]
internal sealed class SavedMerchantOfferState
{
    public int itemType;
    public string lootId;
    public string weaponId;
    public string blueprintId;
    public string relicId;
    public int price;
    public bool isSoldOut;
}

[Serializable]
internal sealed class SavedSpecialDungeonRuntimeContext
{
    public bool isActive;
    public string activeDungeonId;
    public SavedVector2Int returnEventCoord;
    public SavedMainMapSnapshot mainMapSnapshot;
    public List<int> deferredCalamityTiers = new List<int>();
}

[Serializable]
internal sealed class SavedSpecialDungeonTreasureOptionEntry
{
    public string title;
    public string description;
    public int rewardType;
    public int moneyAmount;
    public string blueprintId;
    public string relicId;
    public string relicTableId;
    public int relicBagRarity;
    public int relicBagChoiceCount;
}

[Serializable]
internal struct SavedVector2Int
{
    public int x;
    public int y;

    public SavedVector2Int(Vector2Int value)
    {
        x = value.x;
        y = value.y;
    }

    public Vector2Int ToVector2Int()
    {
        return new Vector2Int(x, y);
    }
}

public static class MainMenuRunSaveSystem
{
    private const string SaveFileName = "menu_continue_save.json";

    private static string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

    public static bool HasSavedRun()
    {
        return File.Exists(SavePath);
    }

    public static void DeleteSavedRun()
    {
        try
        {
            if (File.Exists(SavePath))
                File.Delete(SavePath);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[MainMenuRunSaveSystem] Failed to delete save: {ex.Message}");
        }
    }

    public static bool TryReadMetadata(out MainMenuRunSaveMetadata metadata)
    {
        metadata = null;
        if (!TryReadSaveData(out SavedRunData saveData) || saveData?.metadata == null)
            return false;

        metadata = saveData.metadata;
        return true;
    }

    public static void SaveSnapshot(
        string sceneName,
        RunState runState,
        BoardManager boardManager,
        ActionPointSystem actionPointSystem,
        IEnumerable<int> pendingCalamityTiers = null,
        bool suppressOnePendingCalamityOnActionPointRestore = false,
        SpecialDungeonRuntimeContext specialDungeonContext = null)
    {
        if (runState == null || boardManager == null || actionPointSystem == null || string.IsNullOrWhiteSpace(sceneName))
            return;

        SavedRunData saveData = new SavedRunData
        {
            metadata = new MainMenuRunSaveMetadata
            {
                sceneName = sceneName,
                difficultyName = runState.difficultyName,
                savedAtUtc = DateTime.UtcNow.ToString("o")
            },
            snapshot = new SavedRunSnapshot
            {
                sceneName = sceneName,
                runState = SerializeRunState(runState),
                boardSize = boardManager.BoardSize,
                spawnCoord = new SavedVector2Int(boardManager.SpawnCoord),
                bossCoord = new SavedVector2Int(boardManager.BossCoord),
                selectedCoord = new SavedVector2Int(boardManager.SelectedCoord),
                actionPointBase = actionPointSystem.BasePoints,
                actionPointBonus = actionPointSystem.BonusPoints,
                actionPointCurrent = actionPointSystem.CurrentPoints,
                suppressOnePendingCalamityOnActionPointRestore = suppressOnePendingCalamityOnActionPointRestore,
                specialDungeonContext = SerializeSpecialDungeonContext(specialDungeonContext != null ? specialDungeonContext.Clone() : null)
            }
        };

        foreach (BoardTileData tile in boardManager.GetAllTiles())
        {
            if (tile == null)
                continue;

            saveData.snapshot.tiles.Add(SerializeTile(tile));
        }

        if (pendingCalamityTiers != null)
            saveData.snapshot.pendingCalamityTiers.AddRange(pendingCalamityTiers);

        try
        {
            string directory = Path.GetDirectoryName(SavePath);
            if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            File.WriteAllText(SavePath, JsonUtility.ToJson(saveData, true));
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[MainMenuRunSaveSystem] Failed to save run: {ex.Message}");
        }
    }

    public static bool TryLoadSnapshot(string sceneName, MainMenuSaveAssetCatalog catalog, out MapRunSnapshot snapshot)
    {
        snapshot = null;
        if (!TryReadSaveData(out SavedRunData saveData) || saveData?.snapshot == null)
            return false;

        if (!string.IsNullOrWhiteSpace(sceneName) &&
            !string.Equals(saveData.snapshot.sceneName, sceneName, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        snapshot = DeserializeSnapshot(saveData.snapshot, catalog);
        return snapshot != null;
    }

    private static bool TryReadSaveData(out SavedRunData saveData)
    {
        saveData = null;
        if (!File.Exists(SavePath))
            return false;

        try
        {
            string json = File.ReadAllText(SavePath);
            saveData = JsonUtility.FromJson<SavedRunData>(json);
            return saveData != null;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[MainMenuRunSaveSystem] Failed to read save: {ex.Message}");
            return false;
        }
    }

    private static SavedRunState SerializeRunState(RunState runState)
    {
        SavedRunState savedState = new SavedRunState
        {
            difficultyName = runState.difficultyName,
            boardSize = runState.boardSize,
            playerGridPosition = new SavedVector2Int(runState.playerGridPosition),
            money = runState.money,
            recruitedMemberCount = runState.recruitedMemberCount,
            currentKeys = runState.currentKeys,
            requiredKeys = runState.requiredKeys,
            playerLevel = runState.playerLevel,
            playerExperience = runState.playerExperience,
            calamityTier = runState.calamityTier,
            currentCycle = runState.currentCycle,
            abilityMerchantFreeRefreshPerShop = runState.abilityMerchantFreeRefreshPerShop,
            abilityFirstMerchantPurchaseHalfDiscountAvailable = runState.abilityFirstMerchantPurchaseHalfDiscountAvailable,
            abilityFirstMerchantPurchaseHalfDiscountConsumed = runState.abilityFirstMerchantPurchaseHalfDiscountConsumed,
            abilityBattleStartTemporaryEnergy = runState.abilityBattleStartTemporaryEnergy,
            abilityBattleStartEnergyMaxFlat = runState.abilityBattleStartEnergyMaxFlat,
            abilityBattleStartMaxHealthFlat = runState.abilityBattleStartMaxHealthFlat,
            abilityRelicChoiceRefreshCount = runState.abilityRelicChoiceRefreshCount,
            abilityTreasureRewardTableId = GetTreasureRewardTableId(runState.abilityTreasureRewardTableOverride),
            settlementStats = runState.settlementStats != null ? runState.settlementStats.Clone() : new RunSettlementStats()
        };

        if (runState.partyMembers != null)
        {
            for (int i = 0; i < runState.partyMembers.Count; i++)
            {
                RunPartyMemberState member = runState.partyMembers[i];
                if (member == null)
                    continue;

                savedState.partyMembers.Add(new SavedPartyMemberState
                {
                    memberId = member.memberId,
                    characterId = GetCharacterId(member.characterData),
                    weaponId = GetWeaponId(member.weaponData),
                    level = member.level,
                    experience = member.experience,
                    maxHPFlatBonus = member.maxHPFlatBonus,
                    preCalamityMaxHP = RunPartyMemberMaxHpService.ResolvePreCalamityMaxHP(member),
                    maxHP = member.maxHP,
                    currentHP = member.currentHP
                });
            }
        }

        if (runState.lootInventory != null)
        {
            for (int i = 0; i < runState.lootInventory.Count; i++)
            {
                RunLootState loot = runState.lootInventory[i];
                if (loot == null)
                    continue;

                savedState.lootInventory.Add(new SavedItemStack
                {
                    lootId = GetLootId(loot.lootItem),
                    amount = loot.amount
                });
            }
        }

        if (runState.weaponInventory != null)
        {
            for (int i = 0; i < runState.weaponInventory.Count; i++)
            {
                RunWeaponState weapon = runState.weaponInventory[i];
                if (weapon == null)
                    continue;

                savedState.weaponInventory.Add(new SavedWeaponStack
                {
                    weaponId = GetWeaponId(weapon.weaponData),
                    amount = weapon.amount
                });
            }
        }

        if (runState.blueprintInventory != null)
        {
            for (int i = 0; i < runState.blueprintInventory.Count; i++)
            {
                RunBlueprintState blueprint = runState.blueprintInventory[i];
                if (blueprint == null)
                    continue;

                savedState.blueprintInventory.Add(new SavedBlueprintStack
                {
                    blueprintId = GetBlueprintId(blueprint.blueprintData),
                    amount = blueprint.amount
                });
            }
        }

        if (runState.relicInventory != null)
        {
            for (int i = 0; i < runState.relicInventory.Count; i++)
            {
                RunRelicState relic = runState.relicInventory[i];
                if (relic == null)
                    continue;

                savedState.relicInventory.Add(new SavedRelicStack
                {
                    relicId = GetRelicId(relic.relicData),
                    amount = relic.amount
                });
            }
        }

        if (runState.activeCalamities != null)
        {
            for (int i = 0; i < runState.activeCalamities.Count; i++)
            {
                RunCalamityState calamity = runState.activeCalamities[i];
                if (calamity == null)
                    continue;

                savedState.activeCalamities.Add(new SavedCalamityState
                {
                    calamityId = GetCalamityId(calamity.calamityData),
                    acquiredAtTier = calamity.acquiredAtTier
                });
            }
        }

        if (runState.drawnCalamityIds != null)
            savedState.drawnCalamityIds = new List<string>(runState.drawnCalamityIds);
        if (runState.drawnEventIds != null)
            savedState.drawnEventIds = new List<string>(runState.drawnEventIds);

        return savedState;
    }

    private static SavedBoardTileData SerializeTile(BoardTileData tile)
    {
        SavedBoardTileData savedTile = new SavedBoardTileData
        {
            coord = new SavedVector2Int(tile.coord),
            tileType = (int)tile.tileType,
            dangerBand = (int)tile.dangerBand,
            isVisited = tile.isVisited,
            isResolved = tile.isResolved,
            isSpawnTile = tile.isSpawnTile,
            encounterSeed = tile.encounterSeed,
            previewEnemyCount = tile.previewEnemyCount,
            previewDangerRating = tile.previewDangerRating,
            previewTag = tile.previewTag,
            assignedEnemyGroupId = GetEnemyGroupId(tile.assignedEnemyGroup),
            assignedEventId = GetEventId(tile.assignedEvent),
            merchantStockGenerated = tile.merchantStockGenerated,
            merchantRefreshCount = tile.merchantRefreshCount,
            merchantPurchaseDiscountStacks = tile.merchantPurchaseDiscountStacks,
            sourceSpecialDungeonId = GetSpecialDungeonId(tile.sourceSpecialDungeon),
            specialDungeonEntryIndex = tile.specialDungeonEntryIndex
        };

        if (tile.merchantOffers != null)
        {
            for (int i = 0; i < tile.merchantOffers.Count; i++)
            {
                MerchantOfferState offer = tile.merchantOffers[i];
                if (offer == null)
                    continue;

                savedTile.merchantOffers.Add(new SavedMerchantOfferState
                {
                    itemType = (int)offer.itemType,
                    lootId = GetLootId(offer.lootItem),
                    weaponId = GetWeaponId(offer.weaponData),
                    blueprintId = GetBlueprintId(offer.blueprintData),
                    relicId = GetRelicId(offer.relicData),
                    price = offer.price,
                    isSoldOut = offer.isSoldOut
                });
            }
        }

        if (tile.specialDungeonTreasureOptions != null)
        {
            for (int i = 0; i < tile.specialDungeonTreasureOptions.Count; i++)
            {
                SpecialDungeonTreasureOptionEntry option = tile.specialDungeonTreasureOptions[i];
                if (option != null)
                    savedTile.specialDungeonTreasureOptions.Add(SerializeSpecialDungeonTreasureOption(option));
            }
        }

        return savedTile;
    }

    private static SavedSpecialDungeonRuntimeContext SerializeSpecialDungeonContext(SpecialDungeonRuntimeContext context)
    {
        if (context == null || !context.isActive)
            return null;

        SavedSpecialDungeonRuntimeContext saved = new SavedSpecialDungeonRuntimeContext
        {
            isActive = context.isActive,
            activeDungeonId = !string.IsNullOrWhiteSpace(context.activeDungeonId)
                ? context.activeDungeonId
                : GetSpecialDungeonId(context.activeDungeon),
            returnEventCoord = new SavedVector2Int(context.returnEventCoord),
            mainMapSnapshot = SerializeMainMapSnapshot(context.mainMapSnapshot)
        };

        if (context.deferredCalamityTiers != null)
            saved.deferredCalamityTiers.AddRange(context.deferredCalamityTiers);

        return saved;
    }

    private static SavedRunSnapshot SerializeSnapshot(MapRunSnapshot snapshot, bool includeSpecialDungeonContext = true)
    {
        if (snapshot == null)
            return null;

        SavedRunSnapshot saved = new SavedRunSnapshot
        {
            sceneName = snapshot.sceneName,
            runState = SerializeRunState(snapshot.runState),
            boardSize = snapshot.boardSize,
            spawnCoord = new SavedVector2Int(snapshot.spawnCoord),
            bossCoord = new SavedVector2Int(snapshot.bossCoord),
            selectedCoord = new SavedVector2Int(snapshot.selectedCoord),
            actionPointBase = snapshot.actionPointBase,
            actionPointBonus = snapshot.actionPointBonus,
            actionPointCurrent = snapshot.actionPointCurrent,
            suppressOnePendingCalamityOnActionPointRestore = snapshot.suppressOnePendingCalamityOnActionPointRestore,
            specialDungeonContext = includeSpecialDungeonContext
                ? SerializeSpecialDungeonContext(snapshot.specialDungeonContext)
                : null
        };

        if (snapshot.pendingCalamityTiers != null)
            saved.pendingCalamityTiers.AddRange(snapshot.pendingCalamityTiers);
        if (snapshot.tiles != null)
        {
            for (int i = 0; i < snapshot.tiles.Count; i++)
            {
                BoardTileData tile = snapshot.tiles[i];
                if (tile != null)
                    saved.tiles.Add(SerializeTile(tile));
            }
        }

        return saved;
    }

    private static SavedMainMapSnapshot SerializeMainMapSnapshot(MapRunSnapshot snapshot)
    {
        if (snapshot == null)
            return null;

        SavedMainMapSnapshot saved = new SavedMainMapSnapshot
        {
            sceneName = snapshot.sceneName,
            runState = SerializeRunState(snapshot.runState),
            boardSize = snapshot.boardSize,
            spawnCoord = new SavedVector2Int(snapshot.spawnCoord),
            bossCoord = new SavedVector2Int(snapshot.bossCoord),
            selectedCoord = new SavedVector2Int(snapshot.selectedCoord),
            actionPointBase = snapshot.actionPointBase,
            actionPointBonus = snapshot.actionPointBonus,
            actionPointCurrent = snapshot.actionPointCurrent,
            suppressOnePendingCalamityOnActionPointRestore = snapshot.suppressOnePendingCalamityOnActionPointRestore
        };

        if (snapshot.pendingCalamityTiers != null)
            saved.pendingCalamityTiers.AddRange(snapshot.pendingCalamityTiers);
        if (snapshot.tiles != null)
        {
            for (int i = 0; i < snapshot.tiles.Count; i++)
            {
                BoardTileData tile = snapshot.tiles[i];
                if (tile != null)
                    saved.tiles.Add(SerializeTile(tile));
            }
        }

        return saved;
    }

    private static SavedSpecialDungeonTreasureOptionEntry SerializeSpecialDungeonTreasureOption(SpecialDungeonTreasureOptionEntry option)
    {
        if (option == null)
            return null;

        return new SavedSpecialDungeonTreasureOptionEntry
        {
            title = option.title,
            description = option.description,
            rewardType = (int)option.rewardType,
            moneyAmount = option.moneyAmount,
            blueprintId = GetBlueprintId(option.blueprintData),
            relicId = GetRelicId(option.relicData),
            relicTableId = GetRelicTableId(option.relicTable),
            relicBagRarity = (int)option.relicBagRarity,
            relicBagChoiceCount = option.relicBagChoiceCount
        };
    }

    private static MapRunSnapshot DeserializeSnapshot(SavedRunSnapshot savedSnapshot, MainMenuSaveAssetCatalog catalog)
    {
        if (savedSnapshot == null || catalog == null)
            return null;

        MapRunSnapshot snapshot = new MapRunSnapshot
        {
            sceneName = savedSnapshot.sceneName,
            runState = DeserializeRunState(savedSnapshot.runState, catalog),
            boardSize = savedSnapshot.boardSize,
            spawnCoord = savedSnapshot.spawnCoord.ToVector2Int(),
            bossCoord = savedSnapshot.bossCoord.ToVector2Int(),
            selectedCoord = savedSnapshot.selectedCoord.ToVector2Int(),
            actionPointBase = savedSnapshot.actionPointBase,
            actionPointBonus = savedSnapshot.actionPointBonus,
            actionPointCurrent = savedSnapshot.actionPointCurrent,
            suppressOnePendingCalamityOnActionPointRestore = savedSnapshot.suppressOnePendingCalamityOnActionPointRestore,
            specialDungeonContext = DeserializeSpecialDungeonContext(savedSnapshot.specialDungeonContext, catalog)
        };

        if (savedSnapshot.pendingCalamityTiers != null)
            snapshot.pendingCalamityTiers = new List<int>(savedSnapshot.pendingCalamityTiers);

        if (savedSnapshot.tiles != null)
        {
            for (int i = 0; i < savedSnapshot.tiles.Count; i++)
            {
                SavedBoardTileData savedTile = savedSnapshot.tiles[i];
                if (savedTile == null)
                    continue;

                snapshot.tiles.Add(DeserializeTile(savedTile, catalog));
            }
        }

        return snapshot;
    }

    private static MapRunSnapshot DeserializeMainMapSnapshot(SavedMainMapSnapshot savedSnapshot, MainMenuSaveAssetCatalog catalog)
    {
        if (savedSnapshot == null || catalog == null)
            return null;

        MapRunSnapshot snapshot = new MapRunSnapshot
        {
            sceneName = savedSnapshot.sceneName,
            runState = DeserializeRunState(savedSnapshot.runState, catalog),
            boardSize = savedSnapshot.boardSize,
            spawnCoord = savedSnapshot.spawnCoord.ToVector2Int(),
            bossCoord = savedSnapshot.bossCoord.ToVector2Int(),
            selectedCoord = savedSnapshot.selectedCoord.ToVector2Int(),
            actionPointBase = savedSnapshot.actionPointBase,
            actionPointBonus = savedSnapshot.actionPointBonus,
            actionPointCurrent = savedSnapshot.actionPointCurrent,
            suppressOnePendingCalamityOnActionPointRestore = savedSnapshot.suppressOnePendingCalamityOnActionPointRestore,
            specialDungeonContext = null
        };

        if (savedSnapshot.pendingCalamityTiers != null)
            snapshot.pendingCalamityTiers = new List<int>(savedSnapshot.pendingCalamityTiers);

        if (savedSnapshot.tiles != null)
        {
            for (int i = 0; i < savedSnapshot.tiles.Count; i++)
            {
                SavedBoardTileData savedTile = savedSnapshot.tiles[i];
                if (savedTile == null)
                    continue;

                snapshot.tiles.Add(DeserializeTile(savedTile, catalog));
            }
        }

        return snapshot;
    }

    private static RunState DeserializeRunState(SavedRunState savedState, MainMenuSaveAssetCatalog catalog)
    {
        if (savedState == null || catalog == null)
            return null;

        RunState runState = new RunState
        {
            difficultyName = savedState.difficultyName,
            boardSize = savedState.boardSize,
            playerGridPosition = savedState.playerGridPosition.ToVector2Int(),
            money = savedState.money,
            recruitedMemberCount = savedState.recruitedMemberCount,
            currentKeys = savedState.currentKeys,
            requiredKeys = savedState.requiredKeys,
            playerLevel = savedState.playerLevel,
            playerExperience = savedState.playerExperience,
            calamityTier = savedState.calamityTier,
            currentCycle = savedState.currentCycle,
            abilityMerchantFreeRefreshPerShop = savedState.abilityMerchantFreeRefreshPerShop,
            abilityFirstMerchantPurchaseHalfDiscountAvailable = savedState.abilityFirstMerchantPurchaseHalfDiscountAvailable,
            abilityFirstMerchantPurchaseHalfDiscountConsumed = savedState.abilityFirstMerchantPurchaseHalfDiscountConsumed,
            abilityBattleStartTemporaryEnergy = savedState.abilityBattleStartTemporaryEnergy,
            abilityBattleStartEnergyMaxFlat = savedState.abilityBattleStartEnergyMaxFlat,
            abilityBattleStartMaxHealthFlat = savedState.abilityBattleStartMaxHealthFlat,
            abilityRelicChoiceRefreshCount = savedState.abilityRelicChoiceRefreshCount,
            abilityTreasureRewardTableOverride = catalog.ResolveTreasureRewardTable(savedState.abilityTreasureRewardTableId),
            settlementStats = savedState.settlementStats != null ? savedState.settlementStats.Clone() : new RunSettlementStats()
        };

        if (savedState.partyMembers != null)
        {
            for (int i = 0; i < savedState.partyMembers.Count; i++)
            {
                SavedPartyMemberState member = savedState.partyMembers[i];
                if (member == null)
                    continue;

                runState.partyMembers.Add(new RunPartyMemberState
                {
                    memberId = member.memberId,
                    characterData = catalog.ResolveCharacter(member.characterId),
                    weaponData = catalog.ResolveWeapon(member.weaponId),
                    level = member.level,
                    experience = member.experience,
                    maxHPFlatBonus = member.maxHPFlatBonus,
                    preCalamityMaxHP = member.preCalamityMaxHP > 0 ? member.preCalamityMaxHP : member.maxHP,
                    maxHP = member.maxHP,
                    currentHP = member.currentHP
                });
            }
        }

        if (savedState.lootInventory != null)
        {
            for (int i = 0; i < savedState.lootInventory.Count; i++)
            {
                SavedItemStack loot = savedState.lootInventory[i];
                LootItem_SO lootItem = catalog.ResolveLoot(loot != null ? loot.lootId : string.Empty);
                if (lootItem == null || loot == null || loot.amount <= 0)
                    continue;

                runState.lootInventory.Add(new RunLootState
                {
                    lootItem = lootItem,
                    amount = loot.amount
                });
            }
        }

        if (savedState.weaponInventory != null)
        {
            for (int i = 0; i < savedState.weaponInventory.Count; i++)
            {
                SavedWeaponStack weapon = savedState.weaponInventory[i];
                Weapon_SO weaponData = catalog.ResolveWeapon(weapon != null ? weapon.weaponId : string.Empty);
                if (weaponData == null || weapon == null || weapon.amount <= 0)
                    continue;

                runState.weaponInventory.Add(new RunWeaponState
                {
                    weaponData = weaponData,
                    amount = weapon.amount
                });
            }
        }

        if (savedState.blueprintInventory != null)
        {
            for (int i = 0; i < savedState.blueprintInventory.Count; i++)
            {
                SavedBlueprintStack blueprint = savedState.blueprintInventory[i];
                CraftingBlueprint_SO blueprintData = catalog.ResolveBlueprint(blueprint != null ? blueprint.blueprintId : string.Empty);
                if (blueprintData == null || blueprint == null || blueprint.amount <= 0)
                    continue;

                runState.blueprintInventory.Add(new RunBlueprintState
                {
                    blueprintData = blueprintData,
                    amount = blueprint.amount
                });
            }
        }

        if (savedState.relicInventory != null)
        {
            for (int i = 0; i < savedState.relicInventory.Count; i++)
            {
                SavedRelicStack relic = savedState.relicInventory[i];
                Relic_SO relicData = catalog.ResolveRelic(relic != null ? relic.relicId : string.Empty);
                if (relicData == null || relic == null || relic.amount <= 0)
                    continue;

                runState.relicInventory.Add(new RunRelicState
                {
                    relicData = relicData,
                    amount = relic.amount
                });
            }
        }

        if (savedState.activeCalamities != null)
        {
            for (int i = 0; i < savedState.activeCalamities.Count; i++)
            {
                SavedCalamityState calamity = savedState.activeCalamities[i];
                Calamity_SO calamityData = catalog.ResolveCalamity(calamity != null ? calamity.calamityId : string.Empty);
                if (calamityData == null || calamity == null)
                    continue;

                runState.activeCalamities.Add(new RunCalamityState
                {
                    calamityData = calamityData,
                    acquiredAtTier = calamity.acquiredAtTier
                });
            }
        }

        if (savedState.drawnCalamityIds != null)
            runState.drawnCalamityIds = new List<string>(savedState.drawnCalamityIds);
        if (savedState.drawnEventIds != null)
            runState.drawnEventIds = new List<string>(savedState.drawnEventIds);

        runState.SyncLegacyPlayerFieldsFromParty();
        return runState;
    }

    private static BoardTileData DeserializeTile(SavedBoardTileData savedTile, MainMenuSaveAssetCatalog catalog)
    {
        BoardTileData tile = new BoardTileData
        {
            coord = savedTile.coord.ToVector2Int(),
            tileType = (MapTileType)savedTile.tileType,
            dangerBand = (MapDangerBand)savedTile.dangerBand,
            isVisited = savedTile.isVisited,
            isResolved = savedTile.isResolved,
            isSpawnTile = savedTile.isSpawnTile,
            encounterSeed = savedTile.encounterSeed,
            previewEnemyCount = savedTile.previewEnemyCount,
            previewDangerRating = savedTile.previewDangerRating,
            previewTag = savedTile.previewTag,
            assignedEnemyGroup = catalog.ResolveEnemyGroup(savedTile.assignedEnemyGroupId),
            assignedEvent = catalog.ResolveEvent(savedTile.assignedEventId),
            merchantStockGenerated = savedTile.merchantStockGenerated,
            merchantRefreshCount = savedTile.merchantRefreshCount,
            merchantPurchaseDiscountStacks = savedTile.merchantPurchaseDiscountStacks,
            sourceSpecialDungeon = catalog.ResolveSpecialDungeon(savedTile.sourceSpecialDungeonId),
            specialDungeonEntryIndex = savedTile.specialDungeonEntryIndex
        };

        if (savedTile.merchantOffers != null)
        {
            for (int i = 0; i < savedTile.merchantOffers.Count; i++)
            {
                SavedMerchantOfferState savedOffer = savedTile.merchantOffers[i];
                if (savedOffer == null)
                    continue;

                tile.merchantOffers.Add(new MerchantOfferState
                {
                    itemType = (MerchantItemType)savedOffer.itemType,
                    lootItem = catalog.ResolveLoot(savedOffer.lootId),
                    weaponData = catalog.ResolveWeapon(savedOffer.weaponId),
                    blueprintData = catalog.ResolveBlueprint(savedOffer.blueprintId),
                    relicData = catalog.ResolveRelic(savedOffer.relicId),
                    price = savedOffer.price,
                    isSoldOut = savedOffer.isSoldOut
                });
            }
        }

        if (savedTile.specialDungeonTreasureOptions != null)
        {
            for (int i = 0; i < savedTile.specialDungeonTreasureOptions.Count; i++)
            {
                SpecialDungeonTreasureOptionEntry option = DeserializeSpecialDungeonTreasureOption(savedTile.specialDungeonTreasureOptions[i], catalog);
                if (option != null)
                    tile.specialDungeonTreasureOptions.Add(option);
            }
        }

        RestoreSpecialDungeonTreasureOptionsFromSource(tile);
        return tile;
    }

    private static void RestoreSpecialDungeonTreasureOptionsFromSource(BoardTileData tile)
    {
        if (tile == null || tile.sourceSpecialDungeon == null || tile.specialDungeonEntryIndex < 0)
            return;

        IReadOnlyList<SpecialDungeonTileEntry> entries = tile.sourceSpecialDungeon.GetValidEntries();
        if (entries == null || tile.specialDungeonEntryIndex >= entries.Count)
            return;

        SpecialDungeonTileEntry entry = entries[tile.specialDungeonEntryIndex];
        if (entry == null || entry.treasureOptions == null || entry.treasureOptions.Count == 0)
            return;

        tile.specialDungeonTreasureOptions.Clear();
        for (int i = 0; i < entry.treasureOptions.Count; i++)
        {
            SpecialDungeonTreasureOptionEntry option = entry.treasureOptions[i];
            if (option != null)
                tile.specialDungeonTreasureOptions.Add(option);
        }
    }

    private static SpecialDungeonRuntimeContext DeserializeSpecialDungeonContext(
        SavedSpecialDungeonRuntimeContext saved,
        MainMenuSaveAssetCatalog catalog)
    {
        if (saved == null || !saved.isActive || catalog == null)
            return null;

        SpecialDungeonRuntimeContext context = new SpecialDungeonRuntimeContext
        {
            isActive = saved.isActive,
            activeDungeonId = saved.activeDungeonId,
            activeDungeon = catalog.ResolveSpecialDungeon(saved.activeDungeonId),
            returnEventCoord = saved.returnEventCoord.ToVector2Int(),
            mainMapSnapshot = DeserializeMainMapSnapshot(saved.mainMapSnapshot, catalog)
        };

        if (saved.deferredCalamityTiers != null)
            context.deferredCalamityTiers = new List<int>(saved.deferredCalamityTiers);

        return context;
    }

    private static SpecialDungeonTreasureOptionEntry DeserializeSpecialDungeonTreasureOption(
        SavedSpecialDungeonTreasureOptionEntry saved,
        MainMenuSaveAssetCatalog catalog)
    {
        if (saved == null || catalog == null)
            return null;

        return new SpecialDungeonTreasureOptionEntry
        {
            title = saved.title,
            description = saved.description,
            rewardType = (SpecialDungeonTreasureRewardType)saved.rewardType,
            moneyAmount = saved.moneyAmount,
            blueprintData = catalog.ResolveBlueprint(saved.blueprintId),
            relicData = catalog.ResolveRelic(saved.relicId),
            relicTable = catalog.ResolveRelicTable(saved.relicTableId),
            relicBagRarity = (LootRarity)saved.relicBagRarity,
            relicBagChoiceCount = Mathf.Max(1, saved.relicBagChoiceCount)
        };
    }

    public static string GetCharacterId(CharacterData_SO character)
    {
        if (character == null)
            return string.Empty;
        return !string.IsNullOrWhiteSpace(character.characterName) ? character.characterName : character.name;
    }

    public static string GetWeaponId(Weapon_SO weapon)
    {
        if (weapon == null)
            return string.Empty;
        return weapon.id > 0 ? weapon.id.ToString() : (!string.IsNullOrWhiteSpace(weapon.weaponName) ? weapon.weaponName : weapon.name);
    }

    public static string GetLootId(LootItem_SO loot)
    {
        if (loot == null)
            return string.Empty;
        return loot.lootID > 0 ? loot.lootID.ToString() : (!string.IsNullOrWhiteSpace(loot.lootName) ? loot.lootName : loot.name);
    }

    public static string GetBlueprintId(CraftingBlueprint_SO blueprint)
    {
        if (blueprint == null)
            return string.Empty;
        if (blueprint.blueprintID > 0)
            return blueprint.blueprintID.ToString();
        if (!string.IsNullOrWhiteSpace(blueprint.blueprintName))
            return blueprint.blueprintName;
        return blueprint.name;
    }

    public static string GetRelicId(Relic_SO relic)
    {
        if (relic == null)
            return string.Empty;
        return !string.IsNullOrWhiteSpace(relic.relicId) ? relic.relicId : relic.name;
    }

    public static string GetRelicTableId(RelicTable_SO relicTable)
    {
        if (relicTable == null)
            return string.Empty;
        return relicTable.name;
    }

    public static string GetTreasureRewardTableId(TreasureRewardTable_SO treasureRewardTable)
    {
        if (treasureRewardTable == null)
            return string.Empty;
        return treasureRewardTable.name;
    }

    public static string GetCalamityId(Calamity_SO calamity)
    {
        if (calamity == null)
            return string.Empty;
        return !string.IsNullOrWhiteSpace(calamity.UniqueId) ? calamity.UniqueId : calamity.name;
    }

    public static string GetEnemyGroupId(EnemyGroup_SO enemyGroup)
    {
        if (enemyGroup == null)
            return string.Empty;
        return enemyGroup.groupID > 0 ? enemyGroup.groupID.ToString() : (!string.IsNullOrWhiteSpace(enemyGroup.groupName) ? enemyGroup.groupName : enemyGroup.name);
    }

    public static string GetEventId(MapEvent_SO eventData)
    {
        if (eventData == null)
            return string.Empty;
        return !string.IsNullOrWhiteSpace(eventData.eventId) ? eventData.eventId : eventData.name;
    }

    public static string GetSpecialDungeonId(SpecialDungeon_SO dungeon)
    {
        if (dungeon == null)
            return string.Empty;
        return !string.IsNullOrWhiteSpace(dungeon.dungeonId) ? dungeon.dungeonId : dungeon.name;
    }
}
