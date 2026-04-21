using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
public class RunGameManager : MonoBehaviour
{
    [System.Serializable]
    private class StartingPartyMemberDefinition
    {
        public string memberId = "PartyMember";
        public CharacterData_SO characterData;
        public Weapon_SO equippedWeapon;
        [Min(1)] public int startingLevel = 1;
        [Min(0)] public int startingExperience;
        [Min(0)] public int startingMaxHP;
        [Min(0)] public int startingCurrentHP;
    }

    [System.Serializable]
    private class StartingLootDefinition
    {
        public LootItem_SO lootItem;
        [Min(1)] public int amount = 1;
    }

    [System.Serializable]
    private class StartingWeaponDefinition
    {
        public Weapon_SO weaponData;
        [Min(1)] public int amount = 1;
    }

    [System.Serializable]
    private class StartingBlueprintDefinition
    {
        public CraftingBlueprint_SO blueprintData;
        [Min(1)] public int amount = 1;
    }

    [System.Serializable]
    private class StartingRelicDefinition
    {
        public Relic_SO relicData;
        [Min(1)] public int amount = 1;
    }

    private sealed class EventRandomLootDiscardPreview
    {
        public LootItem_SO lootItem;
        public int amount;
    }

    [Header("Config")]
    [SerializeField] private DifficultyConfig_SO difficultyConfig;
    [SerializeField] private EnemyRoster_SO enemyRoster;
    [SerializeField] private CharacterRoster_SO recruitCharacterRoster;
    [SerializeField] private MapEventPool_SO eventPool;
    [SerializeField] private CalamityPool_SO calamityPool;
    [SerializeField] private MoneyRewardTable_SO moneyRewardTable;
    [SerializeField] private MerchantCatalog_SO merchantCatalog;
    [SerializeField] private LevelTable_SO levelTable;
    [SerializeField] [Min(0)] private int startingMoney;
    [SerializeField] private List<StartingPartyMemberDefinition> startingPartyMembers = new List<StartingPartyMemberDefinition>();
    [SerializeField] private List<StartingLootDefinition> startingLoot = new List<StartingLootDefinition>();
    [SerializeField] private List<StartingWeaponDefinition> startingWeapons = new List<StartingWeaponDefinition>();
    [SerializeField] private List<StartingBlueprintDefinition> startingBlueprints = new List<StartingBlueprintDefinition>();
    [SerializeField] private List<StartingRelicDefinition> startingRelics = new List<StartingRelicDefinition>();
    [SerializeField] private bool autoStartOnAwake = true;
    [SerializeField] private string battleSceneName = "SampleScene";
    [SerializeField] private string restSceneName = "RestScene";

    [Header("Scene Refs")]
    [SerializeField] private Camera mapCamera;
    [SerializeField] private Transform backgroundRoot;
    [SerializeField] private Transform boardRoot;
    [SerializeField] private Transform unitsRoot;
    [SerializeField] private Transform uiRoot;

    [Header("UI")]
    [FormerlySerializedAs("mapUiFont")]
    [SerializeField] private TMP_FontAsset mapKeywordFont;
    [SerializeField] private TMP_FontAsset mapBodyFont;
    [SerializeField] private TMP_FontAsset mapAttributeFont;

    [Header("Managers")]
    [SerializeField] private BoardManager boardManager;
    [SerializeField] private PlayerMapController playerMapController;
    [SerializeField] private ActionPointSystem actionPointSystem;
    [SerializeField] private TileResolver tileResolver;
    [SerializeField] private MapHUD mapHUD;
    [SerializeField] private MapCraftingOverlay mapCraftingOverlay;
    [SerializeField] private MapMerchantOverlay mapMerchantOverlay;
    [SerializeField] private MapEventOverlay mapEventOverlay;
    [SerializeField] private MapCalamityOverlay mapCalamityOverlay;
    [SerializeField] private MapViewController mapViewController;
    [SerializeField] private MapBackgroundController mapBackgroundController;
    [SerializeField] private SceneCurtainTransitionController sceneTransitionController;

    [Header("Camera")]
    [SerializeField] private Color cameraBackgroundColor = new Color(0.08f, 0.11f, 0.16f, 1f);
    [SerializeField] private float cameraPadding = 2.8f;

    private RunState runState;
    private bool subscriptionsBound;
    private DifficultyConfig_SO activeDifficultyConfig;
    private readonly Queue<int> pendingCalamityTiers = new Queue<int>();
    private bool deferPendingCalamityUntilMapResume;
    private bool deferPendingCalamityUntilOverlayClosed;
    private bool suppressPendingCalamityOnNextRestActionPointRestore;
    private BattleEncounterData pendingEventEncounterData;
    private BoardTileData currentEventExecutionTile;
    private string pendingMoveRelicStatusMessage;
    private readonly Dictionary<string, EventRandomLootDiscardPreview> eventRandomLootDiscardPreviewCache = new Dictionary<string, EventRandomLootDiscardPreview>(StringComparer.Ordinal);

    private void Awake()
    {
        EnsureSetup();
        if (autoStartOnAwake)
        {
            if (!TryRestoreRun())
                StartRun();
        }
    }

    private void OnDestroy()
    {
        if (boardManager != null)
            boardManager.TileClicked -= HandleTileClicked;
        if (playerMapController != null)
            playerMapController.PlayerMovedToTile -= HandlePlayerMovedToTile;
        if (actionPointSystem != null)
            actionPointSystem.ActionPointsExhausted -= HandleActionPointsExhausted;
        if (tileResolver != null)
            tileResolver.TileResolved -= HandleTileResolved;
        if (mapViewController != null)
            mapViewController.TileLongPressed -= HandleTileLongPressed;
        if (mapEventOverlay != null)
            mapEventOverlay.Hidden -= HandleMapEventOverlayHidden;
        if (mapMerchantOverlay != null)
            mapMerchantOverlay.Hidden -= HandleMapMerchantOverlayHidden;
        if (mapCraftingOverlay != null)
            mapCraftingOverlay.Hidden -= HandleMapCraftingOverlayHidden;
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
            PersistCurrentRunSave();
    }

    private void OnApplicationQuit()
    {
        PersistCurrentRunSave();
    }

    public void StartRun()
    {
        EnsureSetup();
        MapRunRuntime.Clear(SceneManager.GetActiveScene().name);
        RestSceneRuntime.Clear();
        pendingCalamityTiers.Clear();
        deferPendingCalamityUntilMapResume = false;
        deferPendingCalamityUntilOverlayClosed = false;
        suppressPendingCalamityOnNextRestActionPointRestore = false;
        pendingEventEncounterData = null;

        DifficultyConfig_SO config = ResolvePreferredDifficultyForNewRun();
        activeDifficultyConfig = config;
        runState = new RunState
        {
            difficultyName = config.difficultyName,
            boardSize = config.BoardSize,
            requiredKeys = config.requiredKeys,
            currentCycle = 1,
            money = Mathf.Max(0, startingMoney),
            partyMembers = BuildStartingPartyMembers()
        };

        ApplyStartingInventory(runState);
        runState.SyncLegacyPlayerFieldsFromParty();

        actionPointSystem.Initialize(ResolveActionPointLimitForTier(config, 0));
        boardManager.GenerateBoard(config, UnityEngine.Random.Range(int.MinValue, int.MaxValue));
        AssignEnemyGroupsToHostileTiles();
        mapBackgroundController.Initialize(backgroundRoot, mapCamera, boardManager);
        playerMapController.Initialize(unitsRoot, boardManager, actionPointSystem, tileResolver, runState);

        if (boardManager.TryGetTile(boardManager.SpawnCoord, out BoardTileData spawnTile))
        {
            spawnTile.isVisited = true;
            playerMapController.SetSpawn(spawnTile);
            boardManager.SetSelectedTile(spawnTile.coord);
            boardManager.RefreshAllViews();
            mapHUD.ShowTile(spawnTile, 0);
        }

        RefreshHUD("地图已生成，点击任意格子可查看详情。");
        mapViewController.Initialize(mapCamera, boardManager, cameraPadding);
        mapViewController.SetInputBlockedEvaluator(IsMapViewInputBlocked);
        mapViewController.PlaySpawnIntro(boardManager.GridToWorld(boardManager.SpawnCoord));
    }

    private bool TryRestoreRun()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        bool hasRuntimeSnapshot = MapRunRuntime.TryGetSnapshot(sceneName, out MapRunSnapshot snapshot) && snapshot != null;
        if (!hasRuntimeSnapshot)
        {
            bool continueRequested = MainMenuRuntime.ConsumeContinueRequest();
            if (!continueRequested)
                return false;

            if (!MainMenuRunSaveSystem.TryLoadSnapshot(sceneName, BuildSaveAssetCatalog(), out snapshot) || snapshot == null)
                return false;
        }

        EnsureSetup();
        RestSceneRuntime.Clear();
        pendingCalamityTiers.Clear();
        deferPendingCalamityUntilMapResume = false;
        deferPendingCalamityUntilOverlayClosed = false;
        suppressPendingCalamityOnNextRestActionPointRestore = false;
        pendingEventEncounterData = null;

        runState = snapshot.runState != null ? snapshot.runState.Clone() : null;
        if (runState == null)
            return false;

        activeDifficultyConfig = ResolvePreferredDifficultyForRestore(runState != null ? runState.difficultyName : string.Empty);

        boardManager.LoadBoard(
            snapshot.boardSize,
            snapshot.spawnCoord,
            snapshot.bossCoord,
            snapshot.selectedCoord,
            snapshot.tiles);
        AssignEnemyGroupsToHostileTiles();

        actionPointSystem.RestoreState(
            snapshot.actionPointBase,
            snapshot.actionPointBonus,
            snapshot.actionPointCurrent);

        if (snapshot.pendingCalamityTiers != null)
        {
            for (int i = 0; i < snapshot.pendingCalamityTiers.Count; i++)
                pendingCalamityTiers.Enqueue(Mathf.Max(1, snapshot.pendingCalamityTiers[i]));
        }

        mapBackgroundController.Initialize(backgroundRoot, mapCamera, boardManager);
        playerMapController.Initialize(unitsRoot, boardManager, actionPointSystem, tileResolver, runState);

        if (boardManager.TryGetTile(runState.playerGridPosition, out BoardTileData currentTile))
        {
            playerMapController.SetPosition(currentTile, false);
            Vector2Int selected = snapshot.selectedCoord.x != int.MinValue ? snapshot.selectedCoord : currentTile.coord;
            boardManager.SetSelectedTile(selected);
            boardManager.RefreshAllViews();
            if (boardManager.TryGetTile(selected, out BoardTileData selectedTile))
                mapHUD.ShowTile(selectedTile, GetMoveCost(selectedTile));
            else
                mapHUD.ShowTile(currentTile, GetMoveCost(currentTile));
        }

        mapViewController.Initialize(mapCamera, boardManager, cameraPadding);
        mapViewController.SetInputBlockedEvaluator(IsMapViewInputBlocked);
        RefreshHUD("已恢复地图进度。");
        TryPresentPendingCalamity();
        return true;
    }

    private void HandleTileClicked(BoardTileData tile)
    {
        if (tile == null || runState == null)
            return;

        EnsureTileAssignedEnemyGroup(tile);
        bool isRepeatedSelection = boardManager.SelectedCoord == tile.coord;
        boardManager.SetSelectedTile(tile.coord);
        mapHUD.ShowTile(tile, GetMoveCost(tile), true);

        if (!isRepeatedSelection)
        {
            mapHUD.ShowStatus("正在查看格子详情。再次点击该格子可尝试进入。");
            return;
        }

        if (tile.coord == runState.playerGridPosition)
        {
            if (tile.IsHostileTile)
            {
                if (!TryEnterBattle(tile))
                    TryPresentPendingCalamity();
                return;
            }

            if (tile.tileType == MapTileType.Rest)
            {
                if (!TryEnterRestScene(tile))
                    TryPresentPendingCalamity();
                return;
            }

            if (tile.tileType == MapTileType.Event && !tile.isResolved)
            {
                if (!OpenEventPanel(tile))
                {
                    deferPendingCalamityUntilOverlayClosed = false;
                    TryPresentPendingCalamity();
                }
                return;
            }

            string currentTileMessage = tileResolver.ResolveTile(tile, runState);
            mapHUD.ShowTile(tile, GetMoveCost(tile), true);
            RefreshHUD(currentTileMessage);
            boardManager.RefreshTile(tile.coord);
            if (tile.tileType == MapTileType.Forge)
                OpenForgePanel(tile);
            else if (tile.tileType == MapTileType.Merchant)
            {
                if (!OpenMerchantPanel(tile))
                {
                    deferPendingCalamityUntilOverlayClosed = false;
                    TryPresentPendingCalamity();
                }
            }
            return;
        }

        if (!boardManager.IsAdjacent(runState.playerGridPosition, tile.coord))
        {
            mapHUD.ShowStatus("该格子不在可进入范围内，已显示详细信息。");
            return;
        }

        int selectedTileMoveCost = GetMoveCost(tile);
        if (!CanAttemptMove(tile, selectedTileMoveCost))
        {
            RefreshHUD($"行动点不足，需要 {selectedTileMoveCost} 点。");
            return;
        }

        if (!TryExecuteMove(tile, selectedTileMoveCost))
            RefreshHUD("本次移动未能完成。");
        return;
    }

    private void HandlePlayerMovedToTile(BoardTileData tile, string message)
    {
        if (tile == null)
            return;

        EnsureTileAssignedEnemyGroup(tile);
        if (tile.IsHostileTile)
        {
            boardManager.SetSelectedTile(tile.coord);
            boardManager.RefreshAllViews();
            mapHUD.ShowTile(tile, GetMoveCost(tile));
            ShowPendingMoveRelicStatusIfAny();
            if (!TryEnterBattle(tile))
                TryPresentPendingCalamity();
            return;
        }

        if (tile.tileType == MapTileType.Rest)
        {
            boardManager.SetSelectedTile(tile.coord);
            boardManager.RefreshAllViews();
            mapHUD.ShowTile(tile, GetMoveCost(tile));
            ShowPendingMoveRelicStatusIfAny();
            if (!TryEnterRestScene(tile))
                TryPresentPendingCalamity();
            return;
        }

        if (tile.tileType == MapTileType.Event && !tile.isResolved)
        {
            boardManager.SetSelectedTile(tile.coord);
            boardManager.RefreshAllViews();
            mapHUD.ShowTile(tile, GetMoveCost(tile));
            ShowPendingMoveRelicStatusIfAny();
            if (!OpenEventPanel(tile))
            {
                deferPendingCalamityUntilOverlayClosed = false;
                TryPresentPendingCalamity();
            }
            return;
        }

        boardManager.SetSelectedTile(tile.coord);
        boardManager.RefreshAllViews();
        mapHUD.ShowTile(tile, GetMoveCost(tile));
        RefreshHUD(message);
        ShowPendingMoveRelicStatusIfAny();
        if (tile.tileType == MapTileType.Forge)
            OpenForgePanel(tile);
        else if (tile.tileType == MapTileType.Merchant)
        {
            if (!OpenMerchantPanel(tile))
            {
                deferPendingCalamityUntilOverlayClosed = false;
                TryPresentPendingCalamity();
            }
        }
    }

    private void HandleTileResolved(BoardTileData tile, string message)
    {
        if (tile == null)
            return;

        EnsureTileAssignedEnemyGroup(tile);
        boardManager.RefreshTile(tile.coord);
        mapHUD.ShowTile(tile, GetMoveCost(tile));
        RefreshHUD(message);
    }

    private void HandleTileLongPressed(BoardTileData tile)
    {
        if (tile == null)
            return;

        EnsureTileAssignedEnemyGroup(tile);
        boardManager.SetSelectedTile(tile.coord);
        boardManager.RefreshAllViews();
        mapHUD.ShowTile(tile, GetMoveCost(tile), true);
        mapHUD.ShowStatus("正在查看格子详情。");
    }

    private void OpenForgePanel(BoardTileData tile)
    {
        if (runState == null || tile == null || mapCraftingOverlay == null)
            return;

        tile.isVisited = true;
        boardManager?.RefreshTile(tile.coord);
        mapHUD?.ShowTile(tile, GetMoveCost(tile), tile.coord == runState.playerGridPosition);

        mapCraftingOverlay.Show(runState, HandleCraftBlueprint);
    }

    private bool OpenMerchantPanel(BoardTileData tile)
    {
        if (runState == null || tile == null || mapMerchantOverlay == null)
            return false;

        tile.isVisited = true;
        boardManager?.RefreshTile(tile.coord);
        mapHUD?.ShowTile(tile, GetMoveCost(tile), tile.coord == runState.playerGridPosition);

        EnsureMerchantStock(tile);
        mapMerchantOverlay.Show(runState, tile, HandleBuyMerchantOffer, HandleRefreshMerchantOffers, ResolveMerchantRefreshCost);
        return true;
    }

    private string HandleCraftBlueprint(CraftingBlueprint_SO blueprint)
    {
        if (runState == null || blueprint == null)
            return "\u65e0\u6cd5\u8fdb\u884c\u953b\u9020\u3002";

        if (blueprint.targetWeapon == null)
            return "\u8be5\u56fe\u7eb8\u672a\u914d\u7f6e\u76ee\u6807\u6b66\u5668\u3002";

        if (blueprint.materialRequirements == null || blueprint.materialRequirements.Count == 0)
            return "\u8be5\u56fe\u7eb8\u672a\u914d\u7f6e\u6750\u6599\u9700\u6c42\u3002";

        for (int i = 0; i < blueprint.materialRequirements.Count; i++)
        {
            CraftingMaterialRequirement requirement = blueprint.materialRequirements[i];
            if (requirement == null || requirement.lootItem == null)
                continue;

            if (runState.GetLootAmount(requirement.lootItem) < requirement.amount)
                return $"\u6750\u6599\u4e0d\u8db3\uff1a{requirement.lootItem.lootName} x{requirement.amount}";
        }

        for (int i = 0; i < blueprint.materialRequirements.Count; i++)
        {
            CraftingMaterialRequirement requirement = blueprint.materialRequirements[i];
            if (requirement == null || requirement.lootItem == null)
                continue;

            runState.TryConsumeLoot(requirement.lootItem, requirement.amount);
        }

        runState.AddWeapon(blueprint.targetWeapon, 1);
        string successMessage = $"\u6210\u529f\u953b\u9020\uff1a{blueprint.targetWeapon.weaponName}";
        RefreshHUD(successMessage);
        return successMessage;
    }

    private string HandleBuyMerchantOffer(BoardTileData tile, MerchantOfferState offer)
    {
        if (runState == null || tile == null || offer == null)
            return "无法购买该商品。";

        if (offer.isSoldOut)
            return "该商品已售罄。";

        if (offer.itemType == MerchantItemType.Loot && offer.lootItem == null)
            return "商品未配置战利品。";
        if (offer.itemType == MerchantItemType.Weapon && offer.weaponData == null)
            return "商品未配置武器。";
        if (offer.itemType == MerchantItemType.Blueprint && offer.blueprintData == null)
            return "商品未配置图纸。";
        if (offer.itemType == MerchantItemType.Relic && offer.relicData == null)
            return "商品未配置遗物。";

        int price = Mathf.Max(0, RelicSystem.CalculateMerchantOfferPrice(runState, offer));
        if (!runState.TrySpendMoney(price))
            return $"金钱不足，需要 {price} 金钱。";

        switch (offer.itemType)
        {
            case MerchantItemType.Loot:
                runState.AddLoot(offer.lootItem, 1);
                break;
            case MerchantItemType.Weapon:
                runState.AddWeapon(offer.weaponData, 1);
                break;
            case MerchantItemType.Blueprint:
                runState.AddBlueprint(offer.blueprintData, 1);
                break;
            case MerchantItemType.Relic:
                runState.AddRelic(offer.relicData, 1);
                break;
            default:
                return "未知商品类型。";
        }

        offer.isSoldOut = true;
        RefreshHUD($"购买成功：{offer.DisplayName} - {price} 金钱");
        return $"购买成功：{offer.DisplayName}";
    }

    private int ResolveMerchantRefreshCost(BoardTileData tile)
    {
        DifficultyConfig_SO config = activeDifficultyConfig != null ? activeDifficultyConfig : difficultyConfig;
        int refreshCount = tile != null ? Mathf.Max(0, tile.merchantRefreshCount) : 0;
        return config != null ? Mathf.Max(0, config.GetMerchantRefreshCost(refreshCount)) : 0;
    }

    private DifficultyConfig_SO ResolveMerchantDifficultyConfig()
    {
        if (activeDifficultyConfig != null)
            return activeDifficultyConfig;
        if (difficultyConfig != null)
            return difficultyConfig;

        return CreateFallbackDifficulty();
    }

    private int ResolveMerchantRelicPrice(Relic_SO relicData, System.Random rng)
    {
        return ResolveMerchantRelicPrice(ResolveMerchantDifficultyConfig(), relicData, rng);
    }

    private static int ResolveMerchantRelicPrice(DifficultyConfig_SO config, Relic_SO relicData, System.Random rng)
    {
        if (relicData == null)
            return 0;

        return config != null ? Mathf.Max(0, config.GetMerchantRelicPrice(relicData.rarity, rng)) : 0;
    }

    private string HandleRefreshMerchantOffers(BoardTileData tile)
    {
        if (runState == null || tile == null || tile.tileType != MapTileType.Merchant || merchantCatalog == null)
            return "当前无法刷新商店。";

        if (tile.merchantOffers == null || tile.merchantOffers.Count == 0)
            return "当前没有可刷新的商品。";

        int refreshCost = ResolveMerchantRefreshCost(tile);
        if (runState.money < refreshCost)
            return $"金钱不足，需要 {refreshCost} 金钱。";

        System.Random rng = CreateMerchantRandom(tile, tile.merchantRefreshCount + 1);
        int refreshableCount = CountRefreshableMerchantOffers(tile);
        int refreshedCount = RefreshMerchantOffers(tile, rng);
        if (refreshedCount <= 0)
            return "当前没有可用的新商品可刷新。";

        if (!runState.TrySpendMoney(refreshCost))
            return $"金钱不足，需要 {refreshCost} 金钱。";

        tile.merchantRefreshCount += 1;
        string hudMessage = refreshedCount >= refreshableCount
            ? $"商店已刷新 - {refreshCost} 金钱"
            : $"商店部分刷新：{refreshedCount}/{refreshableCount} - {refreshCost} 金钱";
        RefreshHUD(hudMessage);

        return refreshedCount >= refreshableCount
            ? $"刷新成功：替换了 {refreshedCount} 个商品。"
            : $"部分刷新成功：替换了 {refreshedCount}/{refreshableCount} 个商品。";
    }

    private void HandleActionPointsExhausted()
    {
        if (runState == null)
            return;

        runState.calamityTier += 1;
        runState.currentCycle += 1;
        actionPointSystem.SetBasePoints(ResolveActionPointLimitForCurrentTier());
        actionPointSystem.AdvanceCycle(0);
        pendingCalamityTiers.Enqueue(Mathf.Max(1, runState.calamityTier));
        RefreshHUD($"\u707e\u5384\u6c14\u606f\u6fc0\u5316\uff0c\u5f53\u524d\u707e\u5384\u7b49\u7ea7 {runState.calamityTier}\u3002");
        if (deferPendingCalamityUntilMapResume || deferPendingCalamityUntilOverlayClosed)
            return;
        TryPresentPendingCalamity();
    }

    private void RefreshHUD(string status)
    {
        mapHUD.Initialize();
        mapHUD.Refresh(runState, actionPointSystem);
        if (!string.IsNullOrEmpty(status))
            mapHUD.ShowStatus(status);

        PersistCurrentRunSave();
    }

    private void EnsureSetup()
    {
        EnsureRoots();
        EnsureCamera();

        boardManager = GetOrCreateManager(boardManager);
        playerMapController = GetOrCreateManager(playerMapController);
        actionPointSystem = GetOrCreateManager(actionPointSystem);
        tileResolver = GetOrCreateManager(tileResolver);
        mapHUD = GetOrCreateHud();
        if (mapKeywordFont != null || mapBodyFont != null || mapAttributeFont != null)
            mapHUD.ConfigureFonts(mapKeywordFont, mapBodyFont, mapAttributeFont);
        mapHUD.ConfigureLevelTable(levelTable);
        mapCraftingOverlay = GetOrCreateCraftingOverlay();
        if (mapKeywordFont != null || mapBodyFont != null)
            mapCraftingOverlay.ConfigureFonts(mapKeywordFont, mapBodyFont);
        mapCraftingOverlay.Hidden -= HandleMapCraftingOverlayHidden;
        mapCraftingOverlay.Hidden += HandleMapCraftingOverlayHidden;
        mapMerchantOverlay = GetOrCreateMerchantOverlay();
        if (mapKeywordFont != null || mapBodyFont != null)
            mapMerchantOverlay.ConfigureFonts(mapKeywordFont, mapBodyFont);
        mapMerchantOverlay.Hidden -= HandleMapMerchantOverlayHidden;
        mapMerchantOverlay.Hidden += HandleMapMerchantOverlayHidden;
        mapEventOverlay = GetOrCreateEventOverlay();
        if (mapKeywordFont != null || mapBodyFont != null)
            mapEventOverlay.ConfigureFonts(mapKeywordFont, mapBodyFont);
        mapEventOverlay.Hidden -= HandleMapEventOverlayHidden;
        mapEventOverlay.Hidden += HandleMapEventOverlayHidden;
        mapCalamityOverlay = GetOrCreateCalamityOverlay();
        if (mapKeywordFont != null || mapBodyFont != null)
            mapCalamityOverlay.ConfigureFonts(mapKeywordFont, mapBodyFont);
        mapViewController = GetOrCreateManager(mapViewController);
        mapBackgroundController = GetOrCreateManager(mapBackgroundController);
        if (sceneTransitionController == null)
            sceneTransitionController = FindObjectOfType<SceneCurtainTransitionController>(true);
        if (sceneTransitionController == null)
            sceneTransitionController = SceneCurtainTransitionController.Instance;

        boardManager.Initialize(boardRoot);
        tileResolver.ConfigureMoneyRewardTable(moneyRewardTable);
        mapHUD.Initialize();

        if (!subscriptionsBound)
        {
            boardManager.TileClicked += HandleTileClicked;
            playerMapController.PlayerMovedToTile += HandlePlayerMovedToTile;
            actionPointSystem.ActionPointsExhausted += HandleActionPointsExhausted;
            tileResolver.TileResolved += HandleTileResolved;
            mapViewController.TileLongPressed += HandleTileLongPressed;
            subscriptionsBound = true;
        }
    }

    private void EnsureRoots()
    {
        if (backgroundRoot == null)
            backgroundRoot = CreateRoot("BackgroundRoot");
        if (boardRoot == null)
            boardRoot = CreateRoot("BoardRoot");
        if (unitsRoot == null)
            unitsRoot = CreateRoot("UnitsRoot");
        if (uiRoot == null)
            uiRoot = CreateRoot("UIRoot");
    }

    private void EnsureCamera()
    {
        if (mapCamera == null)
            mapCamera = Camera.main;

        if (mapCamera == null)
        {
            GameObject cameraGo = new GameObject("Map Camera");
            mapCamera = cameraGo.AddComponent<Camera>();
            cameraGo.tag = "MainCamera";
        }

        mapCamera.orthographic = true;
        mapCamera.backgroundColor = cameraBackgroundColor;
        mapCamera.clearFlags = CameraClearFlags.SolidColor;
    }

    private Transform CreateRoot(string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform, false);
        return go.transform;
    }

    private T GetOrCreateManager<T>(T current) where T : Component
    {
        if (current != null)
            return current;

        T found = GetComponent<T>();
        if (found != null)
            return found;

        return gameObject.AddComponent<T>();
    }

    private MapHUD GetOrCreateHud()
    {
        if (mapHUD != null)
            return mapHUD;

        MapHUD found = uiRoot != null ? uiRoot.GetComponent<MapHUD>() : null;
        if (found == null && uiRoot != null)
            found = uiRoot.GetComponentInChildren<MapHUD>(true);
        if (found == null)
            found = FindObjectOfType<MapHUD>(true);
        if (found != null)
        {
            mapHUD = found;
            return found;
        }

        GameObject go = new GameObject("MapHUD");
        go.transform.SetParent(uiRoot, false);
        mapHUD = go.AddComponent<MapHUD>();
        return mapHUD;
    }

    private MapCraftingOverlay GetOrCreateCraftingOverlay()
    {
        if (mapCraftingOverlay != null)
            return mapCraftingOverlay;

        MapCraftingOverlay found = uiRoot != null ? uiRoot.GetComponent<MapCraftingOverlay>() : null;
        if (found == null && uiRoot != null)
            found = uiRoot.GetComponentInChildren<MapCraftingOverlay>(true);
        if (found == null)
            found = FindObjectOfType<MapCraftingOverlay>(true);
        if (found != null)
        {
            mapCraftingOverlay = found;
            return found;
        }

        GameObject go = new GameObject("MapCraftingOverlay");
        go.transform.SetParent(uiRoot, false);
        mapCraftingOverlay = go.AddComponent<MapCraftingOverlay>();
        return mapCraftingOverlay;
    }

    private MapMerchantOverlay GetOrCreateMerchantOverlay()
    {
        if (mapMerchantOverlay != null)
            return mapMerchantOverlay;

        MapMerchantOverlay found = uiRoot != null ? uiRoot.GetComponent<MapMerchantOverlay>() : null;
        if (found == null && uiRoot != null)
            found = uiRoot.GetComponentInChildren<MapMerchantOverlay>(true);
        if (found == null)
            found = FindObjectOfType<MapMerchantOverlay>(true);
        if (found != null)
        {
            mapMerchantOverlay = found;
            return found;
        }

        GameObject go = new GameObject("MapMerchantOverlay");
        go.transform.SetParent(uiRoot, false);
        mapMerchantOverlay = go.AddComponent<MapMerchantOverlay>();
        return mapMerchantOverlay;
    }

    private MapEventOverlay GetOrCreateEventOverlay()
    {
        if (mapEventOverlay != null)
            return mapEventOverlay;

        MapEventOverlay found = uiRoot != null ? uiRoot.GetComponent<MapEventOverlay>() : null;
        if (found == null && uiRoot != null)
            found = uiRoot.GetComponentInChildren<MapEventOverlay>(true);
        if (found == null)
            found = FindObjectOfType<MapEventOverlay>(true);
        if (found != null)
        {
            mapEventOverlay = found;
            return found;
        }

        GameObject go = new GameObject("MapEventOverlay");
        go.transform.SetParent(uiRoot, false);
        mapEventOverlay = go.AddComponent<MapEventOverlay>();
        return mapEventOverlay;
    }

    private MapCalamityOverlay GetOrCreateCalamityOverlay()
    {
        if (mapCalamityOverlay != null)
            return mapCalamityOverlay;

        MapCalamityOverlay found = uiRoot != null ? uiRoot.GetComponent<MapCalamityOverlay>() : null;
        if (found == null && uiRoot != null)
            found = uiRoot.GetComponentInChildren<MapCalamityOverlay>(true);
        if (found == null)
            found = FindObjectOfType<MapCalamityOverlay>(true);
        if (found != null)
        {
            mapCalamityOverlay = found;
            return found;
        }

        GameObject go = new GameObject("MapCalamityOverlay");
        go.transform.SetParent(uiRoot, false);
        mapCalamityOverlay = go.AddComponent<MapCalamityOverlay>();
        return mapCalamityOverlay;
    }

    private DifficultyConfig_SO CreateFallbackDifficulty()
    {
        DifficultyConfig_SO fallback = ScriptableObject.CreateInstance<DifficultyConfig_SO>();
        fallback.difficultyName = "Normal";
        fallback.boardSize = 9;
        fallback.startingActionPoints = 6;
        fallback.actionPointTiers = new List<DifficultyConfig_SO.ActionPointTierEntry>
        {
            new DifficultyConfig_SO.ActionPointTierEntry { calamityTier = 0, actionPointLimit = 6 }
        };
        fallback.requiredKeys = 2;
        fallback.generatedKeyTileCount = 3;
        fallback.merchantRefreshCostTiers = new List<int> { 30, 45, 60 };
        fallback.merchantRelicPrices = new List<DifficultyConfig_SO.MerchantRelicPriceEntry>
        {
            new DifficultyConfig_SO.MerchantRelicPriceEntry { rarity = LootRarity.Common, minPrice = 14, maxPrice = 20 },
            new DifficultyConfig_SO.MerchantRelicPriceEntry { rarity = LootRarity.Uncommon, minPrice = 18, maxPrice = 26 },
            new DifficultyConfig_SO.MerchantRelicPriceEntry { rarity = LootRarity.Rare, minPrice = 26, maxPrice = 34 },
            new DifficultyConfig_SO.MerchantRelicPriceEntry { rarity = LootRarity.Epic, minPrice = 34, maxPrice = 42 },
            new DifficultyConfig_SO.MerchantRelicPriceEntry { rarity = LootRarity.Legendary, minPrice = 42, maxPrice = 52 }
        };
        fallback.tileWeights = new List<DifficultyConfig_SO.TileWeightEntry>
        {
            new DifficultyConfig_SO.TileWeightEntry { tileType = MapTileType.Battle, weight = 4 },
            new DifficultyConfig_SO.TileWeightEntry { tileType = MapTileType.Elite, weight = 1 },
            new DifficultyConfig_SO.TileWeightEntry { tileType = MapTileType.Event, weight = 4 },
            new DifficultyConfig_SO.TileWeightEntry { tileType = MapTileType.Merchant, weight = 1 },
            new DifficultyConfig_SO.TileWeightEntry { tileType = MapTileType.Forge, weight = 1 },
            new DifficultyConfig_SO.TileWeightEntry { tileType = MapTileType.Rest, weight = 1 }
        };
        return fallback;
    }

    private DifficultyConfig_SO ResolvePreferredDifficultyForNewRun()
    {
        DifficultyConfig_SO selectedConfig = MainMenuRuntime.SelectedDifficultyConfig;
        if (selectedConfig != null)
            return selectedConfig;

        return difficultyConfig != null ? difficultyConfig : CreateFallbackDifficulty();
    }

    private DifficultyConfig_SO ResolvePreferredDifficultyForRestore(string difficultyName)
    {
        DifficultyConfig_SO selectedConfig = MainMenuRuntime.SelectedDifficultyConfig;
        if (selectedConfig != null)
        {
            if (string.IsNullOrWhiteSpace(difficultyName) ||
                string.Equals(selectedConfig.difficultyName, difficultyName, StringComparison.OrdinalIgnoreCase))
            {
                return selectedConfig;
            }
        }

        return difficultyConfig != null ? difficultyConfig : CreateFallbackDifficulty();
    }

    private int ResolveActionPointLimitForCurrentTier()
    {
        return ResolveActionPointLimitForTier(activeDifficultyConfig != null ? activeDifficultyConfig : difficultyConfig, runState != null ? runState.calamityTier : 0);
    }

    private static int ResolveActionPointLimitForTier(DifficultyConfig_SO config, int calamityTier)
    {
        if (config == null)
            return 6;

        return Mathf.Max(1, config.GetActionPointLimitForCalamityTier(calamityTier));
    }

    private int GetMoveCost(BoardTileData tile)
    {
        if (tile == null || runState == null)
            return 0;

        if (tile.coord == runState.playerGridPosition)
            return 0;

        return tile.isVisited ? 1 : 2;
    }

    private bool CanAttemptMove(BoardTileData tile, int moveCost)
    {
        if (actionPointSystem == null)
            return false;

        if (actionPointSystem.CanSpend(moveCost))
            return true;

        if (moveCost == 2 && actionPointSystem.CurrentPoints == 1)
            return true;

        return CanPotentiallyIgnoreMoveCostViaRelic(tile);
    }

    private bool CanPotentiallyIgnoreMoveCostViaRelic(BoardTileData tile)
    {
        if (runState == null || tile == null || runState.relicInventory == null)
            return false;

        bool requireUnvisited = !tile.isVisited;
        for (int i = 0; i < runState.relicInventory.Count; i++)
        {
            RunRelicState relicState = runState.relicInventory[i];
            if (relicState == null || relicState.relicData == null || relicState.amount <= 0)
                continue;

            List<Relic_SO.RelicBuiltInEffectEntry> effects = relicState.relicData.builtInEffects;
            if (effects == null)
                continue;

            for (int effectIndex = 0; effectIndex < effects.Count; effectIndex++)
            {
                Relic_SO.RelicBuiltInEffectEntry effect = effects[effectIndex];
                if (effect == null)
                    continue;
                if (effect.trigger != Relic_SO.RelicTrigger.OnMoveCostAboutToSpend)
                    continue;
                bool canIgnoreMoveCost =
                    effect.effectType == Relic_SO.RelicBuiltInEffectType.ChanceToIgnoreMoveCost &&
                    (!effect.requireUnvisitedTile || requireUnvisited);

                if (!canIgnoreMoveCost &&
                    effect.effectType == Relic_SO.RelicBuiltInEffectType.IgnoreMoveCostWhenDestinationVisited &&
                    !requireUnvisited)
                {
                    canIgnoreMoveCost = true;
                }

                if (!canIgnoreMoveCost)
                    continue;
                if (effect.chance <= 0f)
                    continue;

                return true;
            }
        }

        return false;
    }

    private bool TryExecuteMove(BoardTileData tile, int moveCost)
    {
        if (tile == null || playerMapController == null || actionPointSystem == null)
            return false;

        bool exhaustsCurrentCycle = WillSpendExhaustCurrentCycle(moveCost);
        bool shouldDeferCalamityForBattleOrRest = exhaustsCurrentCycle && (tile.IsHostileTile || tile.tileType == MapTileType.Rest);
        bool shouldDeferCalamityForOverlay = exhaustsCurrentCycle && (tile.tileType == MapTileType.Event || tile.tileType == MapTileType.Merchant || tile.tileType == MapTileType.Forge);
        bool shouldSuppressCalamityOnRestActionPointRestore = tile.tileType == MapTileType.Rest && exhaustsCurrentCycle && actionPointSystem.CanSpend(moveCost);
        bool previousDeferredResumeState = deferPendingCalamityUntilMapResume;
        bool previousDeferredOverlayState = deferPendingCalamityUntilOverlayClosed;
        bool previousSuppressRestCalamityState = suppressPendingCalamityOnNextRestActionPointRestore;

        RelicExecutionContext relicContext = RelicExecutionContext.CreateMove(
            runState,
            this,
            tile,
            moveCost,
            !tile.isVisited);
        RelicSystem.Process(runState, Relic_SO.RelicTrigger.OnMoveCostAboutToSpend, relicContext);

        if (relicContext.ignoreMoveCost)
        {
            bool movedWithoutCost = playerMapController.TryMoveTo(tile, 0);
            if (movedWithoutCost && relicContext.feedbackMessages != null && relicContext.feedbackMessages.Count > 0)
                pendingMoveRelicStatusMessage = string.Join("\n", relicContext.feedbackMessages);
            return movedWithoutCost;
        }

        if (shouldDeferCalamityForBattleOrRest)
            deferPendingCalamityUntilMapResume = true;
        if (shouldDeferCalamityForOverlay)
            deferPendingCalamityUntilOverlayClosed = true;
        if (tile.tileType == MapTileType.Rest)
            suppressPendingCalamityOnNextRestActionPointRestore = shouldSuppressCalamityOnRestActionPointRestore;

        if (actionPointSystem.CanSpend(moveCost))
        {
            bool moved = playerMapController.TryMoveTo(tile, moveCost);
            if (!moved)
            {
                deferPendingCalamityUntilMapResume = previousDeferredResumeState;
                deferPendingCalamityUntilOverlayClosed = previousDeferredOverlayState;
                suppressPendingCalamityOnNextRestActionPointRestore = previousSuppressRestCalamityState;
            }
            return moved;
        }

        if (moveCost != 2 || actionPointSystem.CurrentPoints != 1)
        {
            deferPendingCalamityUntilMapResume = previousDeferredResumeState;
            deferPendingCalamityUntilOverlayClosed = previousDeferredOverlayState;
            suppressPendingCalamityOnNextRestActionPointRestore = previousSuppressRestCalamityState;
            return false;
        }

        if (!actionPointSystem.TrySpend(1))
        {
            deferPendingCalamityUntilMapResume = previousDeferredResumeState;
            deferPendingCalamityUntilOverlayClosed = previousDeferredOverlayState;
            suppressPendingCalamityOnNextRestActionPointRestore = previousSuppressRestCalamityState;
            return false;
        }

        if (!playerMapController.TryMoveTo(tile, 0))
        {
            deferPendingCalamityUntilMapResume = previousDeferredResumeState;
            deferPendingCalamityUntilOverlayClosed = previousDeferredOverlayState;
            suppressPendingCalamityOnNextRestActionPointRestore = previousSuppressRestCalamityState;
            return false;
        }

        bool spentOverflowPoint = actionPointSystem.TrySpend(1);
        if (spentOverflowPoint)
            RefreshHUD(string.Empty);
        else
        {
            deferPendingCalamityUntilMapResume = previousDeferredResumeState;
            deferPendingCalamityUntilOverlayClosed = previousDeferredOverlayState;
            suppressPendingCalamityOnNextRestActionPointRestore = previousSuppressRestCalamityState;
        }
        return spentOverflowPoint;
    }

    private bool TryEnterBattle(BoardTileData tile)
    {
        if (tile == null)
            return false;
        if (sceneTransitionController != null && sceneTransitionController.IsTransitioning)
        {
            deferPendingCalamityUntilMapResume = false;
            return false;
        }

        if (tile.tileType == MapTileType.Boss && !runState.IsBossUnlocked)
        {
            deferPendingCalamityUntilMapResume = false;
            string lockedMessage = $"Boss 未解锁。当前钥匙 {runState.currentKeys}/{runState.requiredKeys}。";
            RefreshHUD(lockedMessage);
            boardManager.RefreshTile(tile.coord);
            return false;
        }

        if (enemyRoster == null)
        {
            deferPendingCalamityUntilMapResume = false;
            RefreshHUD("RunGameManager 尚未配置敌人列表。");
            return false;
        }

        EnemyGroup_SO encounterGroup = tile.assignedEnemyGroup;
        if (encounterGroup == null || !encounterGroup.IsValid)
        {
            encounterGroup = SelectEnemyGroupForTile(tile);
            tile.assignedEnemyGroup = encounterGroup;
        }

        if (encounterGroup == null || !encounterGroup.IsValid)
        {
            deferPendingCalamityUntilMapResume = false;
            RefreshHUD($"{GetTileTypeLabel(tile.tileType)} / {GetDangerBandLabel(tile.dangerBand)} 没有可用敌人组。");
            return false;
        }

        tile.isVisited = true;
        boardManager.RefreshTile(tile.coord);
        MapRunRuntime.SaveSnapshot(SceneManager.GetActiveScene().name, runState, boardManager, actionPointSystem, pendingCalamityTiers);

        BattleEncounterRuntime.SetEncounter(new BattleEncounterData
        {
            sourceSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
            targetSceneName = battleSceneName,
            encounterLabel = $"{tile.tileType} [{tile.coord.x},{tile.coord.y}]",
            tileType = tile.tileType,
            dangerBand = tile.dangerBand,
            sourceCoord = tile.coord,
            enemyGroup = encounterGroup,
            partyMembers = new List<RunPartyMemberState>(runState.GetLivingPartyMembers()),
            relicInventory = runState.relicInventory != null ? new List<RunRelicState>(runState.relicInventory.Count) : new List<RunRelicState>(),
            activeCalamities = runState.activeCalamities != null ? new List<RunCalamityState>(runState.activeCalamities.Count) : new List<RunCalamityState>(),
            seed = tile.encounterSeed,
            moneyGained = moneyRewardTable != null ? moneyRewardTable.RollReward(tile.tileType, tile.dangerBand, tile.encounterSeed) : 0
        });

        if (runState.relicInventory != null)
        {
            for (int i = 0; i < runState.relicInventory.Count; i++)
            {
                RunRelicState relicState = runState.relicInventory[i];
                if (relicState != null)
                    BattleEncounterRuntime.CurrentEncounter.relicInventory.Add(relicState.Clone());
            }
        }

        if (runState.activeCalamities != null)
        {
            for (int i = 0; i < runState.activeCalamities.Count; i++)
            {
                RunCalamityState calamityState = runState.activeCalamities[i];
                if (calamityState != null)
                    BattleEncounterRuntime.CurrentEncounter.activeCalamities.Add(calamityState.Clone());
            }
        }

        RefreshHUD($"进入战斗：{encounterGroup.groupName}");
        sceneTransitionController.TransitionToScene(battleSceneName);
        return true;
    }

    private bool TryEnterRestScene(BoardTileData tile)
    {
        if (tile == null || tile.tileType != MapTileType.Rest)
            return false;
        if (tile.isResolved)
        {
            deferPendingCalamityUntilMapResume = false;
            suppressPendingCalamityOnNextRestActionPointRestore = false;
            RefreshHUD("这个休息格已经使用过了。");
            return false;
        }
        if (sceneTransitionController != null && sceneTransitionController.IsTransitioning)
        {
            deferPendingCalamityUntilMapResume = false;
            suppressPendingCalamityOnNextRestActionPointRestore = false;
            return false;
        }
        if (string.IsNullOrWhiteSpace(restSceneName))
        {
            deferPendingCalamityUntilMapResume = false;
            suppressPendingCalamityOnNextRestActionPointRestore = false;
            RefreshHUD("尚未配置休息场景。");
            return false;
        }
        if (runState == null)
        {
            deferPendingCalamityUntilMapResume = false;
            suppressPendingCalamityOnNextRestActionPointRestore = false;
            return false;
        }

        tile.isVisited = true;
        boardManager.SetSelectedTile(tile.coord);
        boardManager.RefreshTile(tile.coord);
        MapRunRuntime.SaveSnapshot(SceneManager.GetActiveScene().name, runState, boardManager, actionPointSystem, pendingCalamityTiers, suppressPendingCalamityOnNextRestActionPointRestore);
        RestSceneRuntime.BeginVisit(SceneManager.GetActiveScene().name, tile.coord, recruitCharacterRoster, activeDifficultyConfig);
        RefreshHUD("进入休息场景。");
        sceneTransitionController.TransitionToScene(restSceneName);
        return true;
    }

    private bool OpenEventPanel(BoardTileData tile)
    {
        if (tile == null || tile.tileType != MapTileType.Event || runState == null || mapEventOverlay == null)
            return false;
        if (tile.isResolved)
            return false;

        tile.isVisited = true;
        EnsureTileAssignedEvent(tile);
        boardManager.RefreshTile(tile.coord);
        mapHUD.ShowTile(tile, GetMoveCost(tile), true);

        if (tile.assignedEvent == null)
        {
            deferPendingCalamityUntilOverlayClosed = false;
            RefreshHUD("当前未配置可触发事件。");
            return false;
        }

        eventRandomLootDiscardPreviewCache.Clear();
        mapEventOverlay.Show(tile, tile.assignedEvent, HandleEventOptionSelected, BuildEventOptionPreview);
        RefreshHUD($"触发事件：{tile.assignedEvent.DisplayName}");
        return true;
    }

    private MapEventOverlay.EventOptionPreviewData BuildEventOptionPreview(BoardTileData tile, MapEvent_SO eventData, MapEventOptionData optionData)
    {
        MapEventOverlay.EventOptionPreviewData preview = new MapEventOverlay.EventOptionPreviewData();
        if (optionData == null || runState == null)
            return preview;

        List<MapEventBuiltInEffectData> allEffects = BuildAllEventEffects(optionData);
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
                    EventRandomLootDiscardPreview discardPreview = GetOrCreateRandomLootDiscardPreview(optionData, i, effectData);
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
                            int requiredAmount = GetFixedLootDiscardAmount(reward);
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

                    break;
                }
            }
        }

        preview.additionalDetail = detailLines.Count > 0 ? string.Join("\n", detailLines) : string.Empty;
        return preview;
    }

    private bool HandleEventOptionSelected(BoardTileData tile, MapEvent_SO eventData, MapEventOptionData optionData, out string message)
    {
        if (tile == null || eventData == null || optionData == null || runState == null)
        {
            message = "事件执行失败。";
            return false;
        }

        int requiredMoney = Mathf.Max(0, optionData.requiredMoney);
        bool spentSelectionMoney = false;
        if (requiredMoney > 0)
        {
            if (!runState.TrySpendMoney(requiredMoney))
            {
                message = $"金钱不足，需要 {requiredMoney} 金钱。";
                return false;
            }

            spentSelectionMoney = true;
        }

        currentEventExecutionTile = tile;
        if (!ExecuteEventOptionBuiltInEffects(optionData, out message))
        {
            if (spentSelectionMoney)
                runState.AddMoney(requiredMoney);
            currentEventExecutionTile = null;
            pendingEventEncounterData = null;
            return false;
        }

        if (optionData.customHandler != null)
        {
            MapEventExecutionContext context = new MapEventExecutionContext
            {
                runState = runState,
                actionPointSystem = actionPointSystem,
                tile = tile,
                eventData = eventData,
                optionData = optionData,
                runGameManager = this
            };

            if (!optionData.customHandler.Execute(context, out string customMessage))
            {
                if (spentSelectionMoney)
                    runState.AddMoney(requiredMoney);
                currentEventExecutionTile = null;
                pendingEventEncounterData = null;
                message = string.IsNullOrWhiteSpace(customMessage) ? "事件的自定义效果未能执行。" : customMessage;
                return false;
            }

            if (!string.IsNullOrWhiteSpace(customMessage))
                message = customMessage;
        }

        tile.isResolved = true;
        tile.assignedEvent = null;
        currentEventExecutionTile = null;

        if (spentSelectionMoney)
        {
            message = string.IsNullOrWhiteSpace(message)
                ? $"支付 {requiredMoney} 金钱。"
                : $"支付 {requiredMoney} 金钱。\n{message}";
        }

        boardManager.RefreshTile(tile.coord);
        mapHUD.ShowTile(tile, GetMoveCost(tile), true);
        RefreshHUD(message);
        return true;
    }

    private bool ExecuteEventOptionBuiltInEffects(MapEventOptionData optionData, out string message)
    {
        List<string> effectMessages = new List<string>();
        List<MapEventBuiltInEffectData> allEffects = BuildAllEventEffects(optionData);

        int encounterEffectIndex = -1;
        for (int i = 0; i < allEffects.Count; i++)
        {
            if (allEffects[i] != null && allEffects[i].effectType == MapEventOptionBuiltInEffectType.StartEncounter)
            {
                encounterEffectIndex = i;
                break;
            }
        }

        if (encounterEffectIndex >= 0)
        {
            if (!ExecuteEventBuiltInEffect(optionData, encounterEffectIndex, allEffects[encounterEffectIndex], out string encounterMessage))
            {
                message = encounterMessage;
                return false;
            }

            if (!string.IsNullOrWhiteSpace(encounterMessage))
                effectMessages.Add(encounterMessage);
        }

        for (int i = 0; i < allEffects.Count; i++)
        {
            MapEventBuiltInEffectData effectData = allEffects[i];
            if (effectData == null)
                continue;
            if (i == encounterEffectIndex)
                continue;

            if (encounterEffectIndex >= 0 && IsDeferredEncounterVictoryRewardEffect(effectData.effectType))
            {
                if (!QueueEncounterVictoryRewardEffect(effectData, out string deferredMessage))
                {
                    message = deferredMessage;
                    return false;
                }

                if (!string.IsNullOrWhiteSpace(deferredMessage))
                    effectMessages.Add(deferredMessage);
                continue;
            }

            if (!ExecuteEventBuiltInEffect(optionData, i, effectData, out string effectMessage))
            {
                message = effectMessage;
                return false;
            }

            if (!string.IsNullOrWhiteSpace(effectMessage))
                effectMessages.Add(effectMessage);
        }

        if (effectMessages.Count > 0)
        {
            message = string.Join("\n", effectMessages);
        }
        else
        {
            message = string.IsNullOrWhiteSpace(optionData.consequenceDescription)
                ? $"已选择：{optionData.DisplayLabel}"
                : optionData.consequenceDescription;
        }

        return true;
    }

    private List<MapEventBuiltInEffectData> BuildAllEventEffects(MapEventOptionData optionData)
    {
        List<MapEventBuiltInEffectData> allEffects = new List<MapEventBuiltInEffectData>();
        MapEventBuiltInEffectData primaryEffect = BuildPrimaryEventEffectData(optionData);
        if (primaryEffect != null)
            allEffects.Add(primaryEffect);

        if (optionData != null && optionData.additionalBuiltInEffects != null)
        {
            for (int i = 0; i < optionData.additionalBuiltInEffects.Count; i++)
            {
                MapEventBuiltInEffectData effectData = optionData.additionalBuiltInEffects[i];
                if (effectData != null)
                    allEffects.Add(effectData);
            }
        }

        return allEffects;
    }

    private static int GetFixedLootDiscardAmount(MapEventLootRewardEntry reward)
    {
        if (reward == null)
            return 0;

        return Mathf.Max(1, reward.amountMin);
    }

    private static string BuildEventRandomLootDiscardPreviewKey(MapEventOptionData optionData, int effectIndex)
    {
        string optionId = optionData != null && !string.IsNullOrWhiteSpace(optionData.optionId) ? optionData.optionId : "Option";
        return $"{optionId}::{effectIndex}";
    }

    private EventRandomLootDiscardPreview GetOrCreateRandomLootDiscardPreview(MapEventOptionData optionData, int effectIndex, MapEventBuiltInEffectData effectData)
    {
        string cacheKey = BuildEventRandomLootDiscardPreviewKey(optionData, effectIndex);
        if (eventRandomLootDiscardPreviewCache.TryGetValue(cacheKey, out EventRandomLootDiscardPreview cachedPreview))
        {
            if (cachedPreview != null &&
                cachedPreview.lootItem != null &&
                runState.GetLootAmount(cachedPreview.lootItem) >= cachedPreview.amount)
            {
                return cachedPreview;
            }

            eventRandomLootDiscardPreviewCache.Remove(cacheKey);
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

        EventRandomLootDiscardPreview preview = new EventRandomLootDiscardPreview
        {
            lootItem = selectedLoot.lootItem,
            amount = amount
        };
        eventRandomLootDiscardPreviewCache[cacheKey] = preview;
        return preview;
    }

    private MapEventBuiltInEffectData BuildPrimaryEventEffectData(MapEventOptionData optionData)
    {
        return new MapEventBuiltInEffectData
        {
            effectType = optionData.builtInEffectType,
            effectAmount = optionData.effectAmount,
            randomMoneyMin = optionData.randomMoneyMin,
            randomMoneyMax = optionData.randomMoneyMax,
            randomOwnedLootAmountMin = optionData.randomOwnedLootAmountMin,
            randomOwnedLootAmountMax = optionData.randomOwnedLootAmountMax,
            lootRewards = optionData.lootRewards != null ? new List<MapEventLootRewardEntry>(optionData.lootRewards) : new List<MapEventLootRewardEntry>(),
            lootPickCountMin = optionData.lootPickCountMin,
            lootPickCountMax = optionData.lootPickCountMax,
            relicRewards = optionData.relicRewards != null ? new List<MapEventRelicRewardEntry>(optionData.relicRewards) : new List<MapEventRelicRewardEntry>(),
            relicPickCountMin = optionData.relicPickCountMin,
            relicPickCountMax = optionData.relicPickCountMax,
            blueprintRewards = optionData.blueprintRewards != null ? new List<MapEventBlueprintRewardEntry>(optionData.blueprintRewards) : new List<MapEventBlueprintRewardEntry>(),
            blueprintPickCountMin = optionData.blueprintPickCountMin,
            blueprintPickCountMax = optionData.blueprintPickCountMax,
            encounterSelectionMode = optionData.encounterSelectionMode,
            encounterEnemyGroups = optionData.encounterEnemyGroups != null ? new List<EnemyGroup_SO>(optionData.encounterEnemyGroups) : new List<EnemyGroup_SO>(),
            encounterEnemies = optionData.encounterEnemies != null ? new List<EnemyData_SO>(optionData.encounterEnemies) : new List<EnemyData_SO>(),
            encounterEnemyCountMin = optionData.encounterEnemyCountMin,
            encounterEnemyCountMax = optionData.encounterEnemyCountMax
        };
    }

    private bool ExecuteEventBuiltInEffect(MapEventOptionData optionData, int effectIndex, MapEventBuiltInEffectData effectData, out string message)
    {
        if (effectData == null)
        {
            message = string.Empty;
            return true;
        }

        switch (effectData.effectType)
        {
            case MapEventOptionBuiltInEffectType.None:
                message = string.Empty;
                return true;
            case MapEventOptionBuiltInEffectType.GainMoney:
                if (effectData.effectAmount > 0)
                    runState.AddMoney(effectData.effectAmount);
                message = $"获得 {Mathf.Max(0, effectData.effectAmount)} 金钱。";
                return true;
            case MapEventOptionBuiltInEffectType.GainRandomMoney:
            {
                int minAmount = Mathf.Max(0, effectData.randomMoneyMin);
                int maxAmount = Mathf.Max(minAmount, effectData.randomMoneyMax);
                int gainedAmount = UnityEngine.Random.Range(minAmount, maxAmount + 1);
                if (gainedAmount > 0)
                    runState.AddMoney(gainedAmount);
                message = $"获得 {gainedAmount} 金钱。";
                return true;
            }
            case MapEventOptionBuiltInEffectType.GainExperience:
            {
                int gainedExperience = Mathf.Max(0, effectData.effectAmount);
                ApplyExperienceToAllPartyMembers(gainedExperience);
                message = $"所有角色获得 {gainedExperience} 经验。";
                return true;
            }
            case MapEventOptionBuiltInEffectType.LoseMoney:
            {
                int cost = Mathf.Max(0, effectData.effectAmount);
                if (!runState.TrySpendMoney(cost))
                {
                    message = $"金钱不足，需要 {cost} 金钱。";
                    return false;
                }

                message = $"失去 {cost} 金钱。";
                return true;
            }
            case MapEventOptionBuiltInEffectType.HealAllPartyMembers:
            {
                if (runState.partyMembers != null)
                {
                    for (int i = 0; i < runState.partyMembers.Count; i++)
                    {
                        RunPartyMemberState member = runState.partyMembers[i];
                        if (member == null)
                            continue;

                        member.currentHP = Mathf.Max(0, member.maxHP);
                    }
                }

                message = "所有角色已恢复至满血。";
                return true;
            }
            case MapEventOptionBuiltInEffectType.AdjustAllPartyMembersHpByMaxHpPercent:
            {
                int percent = effectData.effectAmount;
                if (runState.partyMembers != null)
                {
                    for (int i = 0; i < runState.partyMembers.Count; i++)
                    {
                        RunPartyMemberState member = runState.partyMembers[i];
                        if (member == null || member.maxHP <= 0)
                            continue;

                        int hpDelta = Mathf.FloorToInt(member.maxHP * (Mathf.Abs(percent) / 100f));
                        if (hpDelta <= 0)
                            continue;

                        if (percent >= 0)
                            member.currentHP = Mathf.Clamp(member.currentHP + hpDelta, 0, member.maxHP);
                        else
                            member.currentHP = Mathf.Clamp(member.currentHP - hpDelta, 0, member.maxHP);
                    }
                }

                message = percent >= 0
                    ? $"所有角色恢复了最大生命值的 {percent}% 。"
                    : $"所有角色失去了最大生命值的 {Mathf.Abs(percent)}% 。";
                return true;
            }
            case MapEventOptionBuiltInEffectType.GainActionPoints:
            {
                int gainAmount = Mathf.Max(0, effectData.effectAmount);
                if (actionPointSystem == null)
                {
                    message = "当前未找到行动点系统。";
                    return false;
                }

                int restoredPoints = Mathf.Clamp(actionPointSystem.CurrentPoints + gainAmount, 0, actionPointSystem.CurrentMaxPoints);
                actionPointSystem.RestoreState(actionPointSystem.BasePoints, actionPointSystem.BonusPoints, restoredPoints);
                message = $"恢复 {gainAmount} 点行动点。";
                return true;
            }
            case MapEventOptionBuiltInEffectType.LoseActionPoints:
            {
                int loseAmount = Mathf.Max(0, effectData.effectAmount);
                if (actionPointSystem == null)
                {
                    message = "当前未找到行动点系统。";
                    return false;
                }

                SpendActionPointsWithOverflow(loseAmount);
                message = $"失去 {loseAmount} 点行动点。";
                return true;
            }
            case MapEventOptionBuiltInEffectType.RestoreAllActionPoints:
                if (actionPointSystem != null)
                    actionPointSystem.RestoreState(actionPointSystem.BasePoints, actionPointSystem.BonusPoints, actionPointSystem.CurrentMaxPoints);
                message = "行动点已恢复至当前上限。";
                return true;
            case MapEventOptionBuiltInEffectType.GainLoot:
            case MapEventOptionBuiltInEffectType.GainRandomLootFromList:
                return TryExecuteLootRewardEffect(effectData, out message);
            case MapEventOptionBuiltInEffectType.GainRelics:
                return TryExecuteRelicRewardEffect(effectData, out message);
            case MapEventOptionBuiltInEffectType.GainBlueprints:
                return TryExecuteBlueprintRewardEffect(effectData, out message);
            case MapEventOptionBuiltInEffectType.LoseOwnedRandomLoot:
                return TryExecuteRandomLootDiscardEffect(optionData, effectIndex, effectData, out message);
            case MapEventOptionBuiltInEffectType.LoseSpecificLoot:
                return TryExecuteSpecificLootDiscardEffect(effectData, out message);
            case MapEventOptionBuiltInEffectType.StartEncounter:
                return TryPrepareEventEncounter(effectData, out message);
            default:
                message = "未知事件效果。";
                return false;
        }
    }

    private bool TryExecuteRandomLootDiscardEffect(MapEventOptionData optionData, int effectIndex, MapEventBuiltInEffectData effectData, out string message)
    {
        EventRandomLootDiscardPreview preview = GetOrCreateRandomLootDiscardPreview(optionData, effectIndex, effectData);
        if (preview == null || preview.lootItem == null || preview.amount <= 0)
        {
            message = "背包中没有可丢弃的战利品。";
            return false;
        }

        if (!runState.TryConsumeLoot(preview.lootItem, preview.amount))
        {
            message = $"无法丢弃 {preview.lootItem.lootName} x{preview.amount}。";
            return false;
        }

        message = $"失去战利品：{preview.lootItem.lootName} x{preview.amount}。";
        eventRandomLootDiscardPreviewCache.Remove(BuildEventRandomLootDiscardPreviewKey(optionData, effectIndex));
        return true;
    }

    private bool TryExecuteSpecificLootDiscardEffect(MapEventBuiltInEffectData effectData, out string message)
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

            int requiredAmount = GetFixedLootDiscardAmount(reward);
            if (runState.GetLootAmount(reward.lootItem) < requiredAmount)
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

            int requiredAmount = GetFixedLootDiscardAmount(reward);
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

    private bool TryPrepareEventEncounter(MapEventBuiltInEffectData effectData, out string message)
    {
        if (runState == null)
        {
            message = "当前未找到运行时队伍数据。";
            return false;
        }

        EnemyGroup_SO encounterGroup = null;
        switch (effectData.encounterSelectionMode)
        {
            case MapEventEncounterSelectionMode.RandomEnemyGroup:
                encounterGroup = BuildEncounterGroupFromEventGroups(effectData);
                break;
            case MapEventEncounterSelectionMode.RandomEnemies:
                encounterGroup = BuildEncounterGroupFromEventEnemies(effectData);
                break;
            default:
                message = "未知事件遭遇配置。";
                return false;
        }

        if (encounterGroup == null || !encounterGroup.IsValid)
        {
            message = "该事件未配置有效的遭遇敌人。";
            return false;
        }

        int encounterSeed = unchecked(Environment.TickCount ^ runState.currentCycle * 397);
        if (currentEventExecutionTile != null)
            encounterSeed ^= currentEventExecutionTile.encounterSeed;

        pendingEventEncounterData = new BattleEncounterData
        {
            sourceSceneName = SceneManager.GetActiveScene().name,
            targetSceneName = battleSceneName,
            encounterLabel = $"事件遭遇：{encounterGroup.groupName}",
            tileType = MapTileType.Event,
            dangerBand = currentEventExecutionTile != null ? currentEventExecutionTile.dangerBand : MapDangerBand.Outer,
            sourceCoord = currentEventExecutionTile != null ? currentEventExecutionTile.coord : runState.playerGridPosition,
            enemyGroup = encounterGroup,
            partyMembers = new List<RunPartyMemberState>(runState.GetLivingPartyMembers()),
            seed = encounterSeed,
            moneyGained = 0
        };

        if (runState.relicInventory != null)
        {
            for (int i = 0; i < runState.relicInventory.Count; i++)
            {
                RunRelicState relicState = runState.relicInventory[i];
                if (relicState != null)
                    pendingEventEncounterData.relicInventory.Add(relicState.Clone());
            }
        }

        if (runState.activeCalamities != null)
        {
            for (int i = 0; i < runState.activeCalamities.Count; i++)
            {
                RunCalamityState calamityState = runState.activeCalamities[i];
                if (calamityState != null)
                    pendingEventEncounterData.activeCalamities.Add(calamityState.Clone());
            }
        }

        message = $"将触发遭遇战：{encounterGroup.groupName}";
        return true;
    }

    private EnemyGroup_SO BuildEncounterGroupFromEventGroups(MapEventBuiltInEffectData effectData)
    {
        if (effectData == null || effectData.encounterEnemyGroups == null || effectData.encounterEnemyGroups.Count == 0)
            return null;

        List<EnemyGroup_SO> candidates = new List<EnemyGroup_SO>();
        for (int i = 0; i < effectData.encounterEnemyGroups.Count; i++)
        {
            EnemyGroup_SO group = effectData.encounterEnemyGroups[i];
            if (group != null && group.IsValid)
                candidates.Add(group);
        }

        if (candidates.Count == 0)
            return null;

        int index = UnityEngine.Random.Range(0, candidates.Count);
        return candidates[index];
    }

    private EnemyGroup_SO BuildEncounterGroupFromEventEnemies(MapEventBuiltInEffectData effectData)
    {
        if (effectData == null || effectData.encounterEnemies == null || effectData.encounterEnemies.Count == 0)
            return null;

        List<EnemyData_SO> candidates = new List<EnemyData_SO>();
        for (int i = 0; i < effectData.encounterEnemies.Count; i++)
        {
            EnemyData_SO enemy = effectData.encounterEnemies[i];
            if (enemy != null)
                candidates.Add(enemy);
        }

        if (candidates.Count == 0)
            return null;

        int minCount = Mathf.Max(1, effectData.encounterEnemyCountMin);
        int maxCount = Mathf.Max(minCount, effectData.encounterEnemyCountMax);
        int pickCount = Mathf.Min(UnityEngine.Random.Range(minCount, maxCount + 1), candidates.Count);
        if (pickCount <= 0)
            return null;

        EnemyGroup_SO runtimeGroup = ScriptableObject.CreateInstance<EnemyGroup_SO>();
        runtimeGroup.groupName = "事件遭遇";
        runtimeGroup.description = "事件触发的随机遭遇。";
        runtimeGroup.enemies = new List<EnemyData_SO>();

        for (int i = 0; i < pickCount; i++)
        {
            int selectedIndex = UnityEngine.Random.Range(0, candidates.Count);
            runtimeGroup.enemies.Add(candidates[selectedIndex]);
            candidates.RemoveAt(selectedIndex);
        }

        return runtimeGroup;
    }

    private bool TryExecuteLootRewardEffect(MapEventBuiltInEffectData effectData, out string message)
    {
        if (effectData.lootRewards == null || effectData.lootRewards.Count == 0)
        {
            message = "该事件效果未配置战利品列表。";
            return false;
        }

        List<MapEventLootRewardEntry> availableRewards = new List<MapEventLootRewardEntry>(effectData.lootRewards.Count);
        for (int i = 0; i < effectData.lootRewards.Count; i++)
        {
            MapEventLootRewardEntry reward = effectData.lootRewards[i];
            if (reward != null && reward.lootItem != null)
                availableRewards.Add(reward);
        }

        if (availableRewards.Count == 0)
        {
            message = "该事件效果未配置有效的战利品。";
            return false;
        }

        int minPickCount = Mathf.Max(1, effectData.lootPickCountMin);
        int maxPickCount = Mathf.Max(minPickCount, effectData.lootPickCountMax);
        int pickCount = UnityEngine.Random.Range(minPickCount, maxPickCount + 1);
        pickCount = Mathf.Min(pickCount, availableRewards.Count);
        List<string> gainedLootDescriptions = new List<string>(pickCount);

        for (int i = 0; i < pickCount; i++)
        {
            int selectedIndex = UnityEngine.Random.Range(0, availableRewards.Count);
            MapEventLootRewardEntry selectedReward = availableRewards[selectedIndex];
            availableRewards.RemoveAt(selectedIndex);

            int amountMin = Mathf.Max(1, selectedReward.amountMin);
            int amountMax = Mathf.Max(amountMin, selectedReward.amountMax);
            int amount = UnityEngine.Random.Range(amountMin, amountMax + 1);
            runState.AddLoot(selectedReward.lootItem, amount);
            gainedLootDescriptions.Add($"{selectedReward.lootItem.lootName} x{amount}");
        }

        message = gainedLootDescriptions.Count > 0
            ? $"获得战利品：{string.Join("，", gainedLootDescriptions)}。"
            : "未获得任何战利品。";
        return true;
    }

    private bool TryExecuteRelicRewardEffect(MapEventBuiltInEffectData effectData, out string message)
    {
        if (effectData.relicRewards == null || effectData.relicRewards.Count == 0)
        {
            message = "该事件效果未配置遗物列表。";
            return false;
        }

        List<MapEventRelicRewardEntry> availableRewards = new List<MapEventRelicRewardEntry>(effectData.relicRewards.Count);
        for (int i = 0; i < effectData.relicRewards.Count; i++)
        {
            MapEventRelicRewardEntry reward = effectData.relicRewards[i];
            if (reward != null && reward.relicData != null)
                availableRewards.Add(reward);
        }

        if (availableRewards.Count == 0)
        {
            message = "该事件效果未配置有效的遗物。";
            return false;
        }

        int minPickCount = Mathf.Max(1, effectData.relicPickCountMin);
        int maxPickCount = Mathf.Max(minPickCount, effectData.relicPickCountMax);
        int pickCount = UnityEngine.Random.Range(minPickCount, maxPickCount + 1);
        pickCount = Mathf.Min(pickCount, availableRewards.Count);
        List<string> gainedRelicDescriptions = new List<string>(pickCount);

        for (int i = 0; i < pickCount; i++)
        {
            int selectedIndex = UnityEngine.Random.Range(0, availableRewards.Count);
            MapEventRelicRewardEntry selectedReward = availableRewards[selectedIndex];
            availableRewards.RemoveAt(selectedIndex);

            runState.AddRelic(selectedReward.relicData, 1);
            gainedRelicDescriptions.Add(selectedReward.relicData.DisplayName);
        }

        message = gainedRelicDescriptions.Count > 0
            ? $"获得遗物：{string.Join("，", gainedRelicDescriptions)}。"
            : "未获得任何遗物。";
        return true;
    }

    private bool TryExecuteBlueprintRewardEffect(MapEventBuiltInEffectData effectData, out string message)
    {
        if (effectData.blueprintRewards == null || effectData.blueprintRewards.Count == 0)
        {
            message = "该事件效果未配置图纸列表。";
            return false;
        }

        List<MapEventBlueprintRewardEntry> availableRewards = new List<MapEventBlueprintRewardEntry>(effectData.blueprintRewards.Count);
        for (int i = 0; i < effectData.blueprintRewards.Count; i++)
        {
            MapEventBlueprintRewardEntry reward = effectData.blueprintRewards[i];
            if (reward != null && reward.blueprintData != null)
                availableRewards.Add(reward);
        }

        if (availableRewards.Count == 0)
        {
            message = "该事件效果未配置有效的图纸。";
            return false;
        }

        int minPickCount = Mathf.Max(1, effectData.blueprintPickCountMin);
        int maxPickCount = Mathf.Max(minPickCount, effectData.blueprintPickCountMax);
        int pickCount = UnityEngine.Random.Range(minPickCount, maxPickCount + 1);
        pickCount = Mathf.Min(pickCount, availableRewards.Count);
        List<string> gainedBlueprintDescriptions = new List<string>(pickCount);

        for (int i = 0; i < pickCount; i++)
        {
            int selectedIndex = UnityEngine.Random.Range(0, availableRewards.Count);
            MapEventBlueprintRewardEntry selectedReward = availableRewards[selectedIndex];
            availableRewards.RemoveAt(selectedIndex);

            runState.AddBlueprint(selectedReward.blueprintData, 1);
            gainedBlueprintDescriptions.Add(selectedReward.blueprintData.DisplayName);
        }

        message = gainedBlueprintDescriptions.Count > 0
            ? $"获得图纸：{string.Join("，", gainedBlueprintDescriptions)}。"
            : "未获得任何图纸。";
        return true;
    }

    private static bool IsDeferredEncounterVictoryRewardEffect(MapEventOptionBuiltInEffectType effectType)
    {
        switch (effectType)
        {
            case MapEventOptionBuiltInEffectType.GainLoot:
            case MapEventOptionBuiltInEffectType.GainRandomLootFromList:
            case MapEventOptionBuiltInEffectType.GainRelics:
            case MapEventOptionBuiltInEffectType.GainBlueprints:
                return true;
            default:
                return false;
        }
    }

    private bool QueueEncounterVictoryRewardEffect(MapEventBuiltInEffectData effectData, out string message)
    {
        if (pendingEventEncounterData == null)
        {
            message = "未找到待触发的事件遭遇。";
            return false;
        }

        switch (effectData.effectType)
        {
            case MapEventOptionBuiltInEffectType.GainLoot:
            case MapEventOptionBuiltInEffectType.GainRandomLootFromList:
                return TryQueueEncounterVictoryLootRewards(effectData, out message);
            case MapEventOptionBuiltInEffectType.GainRelics:
                return TryQueueEncounterVictoryRelicRewards(effectData, out message);
            case MapEventOptionBuiltInEffectType.GainBlueprints:
                return TryQueueEncounterVictoryBlueprintRewards(effectData, out message);
            default:
                message = string.Empty;
                return true;
        }
    }

    private bool TryQueueEncounterVictoryLootRewards(MapEventBuiltInEffectData effectData, out string message)
    {
        if (effectData.lootRewards == null || effectData.lootRewards.Count == 0)
        {
            message = "该事件效果未配置战利品列表。";
            return false;
        }

        List<MapEventLootRewardEntry> availableRewards = new List<MapEventLootRewardEntry>(effectData.lootRewards.Count);
        for (int i = 0; i < effectData.lootRewards.Count; i++)
        {
            MapEventLootRewardEntry reward = effectData.lootRewards[i];
            if (reward != null && reward.lootItem != null)
                availableRewards.Add(reward);
        }

        if (availableRewards.Count == 0)
        {
            message = "该事件效果未配置有效的战利品。";
            return false;
        }

        int minPickCount = Mathf.Max(1, effectData.lootPickCountMin);
        int maxPickCount = Mathf.Max(minPickCount, effectData.lootPickCountMax);
        int pickCount = Mathf.Min(UnityEngine.Random.Range(minPickCount, maxPickCount + 1), availableRewards.Count);
        List<string> descriptions = new List<string>(pickCount);

        for (int i = 0; i < pickCount; i++)
        {
            int selectedIndex = UnityEngine.Random.Range(0, availableRewards.Count);
            MapEventLootRewardEntry selectedReward = availableRewards[selectedIndex];
            availableRewards.RemoveAt(selectedIndex);

            int amountMin = Mathf.Max(1, selectedReward.amountMin);
            int amountMax = Mathf.Max(amountMin, selectedReward.amountMax);
            int amount = UnityEngine.Random.Range(amountMin, amountMax + 1);
            pendingEventEncounterData.pendingVictoryLootRewards.Add(new RunLootState
            {
                lootItem = selectedReward.lootItem,
                amount = amount
            });
            descriptions.Add($"{selectedReward.lootItem.lootName} x{amount}");
        }

        message = descriptions.Count > 0
            ? $"战斗胜利后将获得战利品：{string.Join("，", descriptions)}。"
            : "战斗胜利后未配置任何战利品奖励。";
        return true;
    }

    private bool TryQueueEncounterVictoryRelicRewards(MapEventBuiltInEffectData effectData, out string message)
    {
        if (effectData.relicRewards == null || effectData.relicRewards.Count == 0)
        {
            message = "该事件效果未配置遗物列表。";
            return false;
        }

        List<MapEventRelicRewardEntry> availableRewards = new List<MapEventRelicRewardEntry>(effectData.relicRewards.Count);
        for (int i = 0; i < effectData.relicRewards.Count; i++)
        {
            MapEventRelicRewardEntry reward = effectData.relicRewards[i];
            if (reward != null && reward.relicData != null)
                availableRewards.Add(reward);
        }

        if (availableRewards.Count == 0)
        {
            message = "该事件效果未配置有效的遗物。";
            return false;
        }

        int minPickCount = Mathf.Max(1, effectData.relicPickCountMin);
        int maxPickCount = Mathf.Max(minPickCount, effectData.relicPickCountMax);
        int pickCount = Mathf.Min(UnityEngine.Random.Range(minPickCount, maxPickCount + 1), availableRewards.Count);
        List<string> descriptions = new List<string>(pickCount);

        for (int i = 0; i < pickCount; i++)
        {
            int selectedIndex = UnityEngine.Random.Range(0, availableRewards.Count);
            MapEventRelicRewardEntry selectedReward = availableRewards[selectedIndex];
            availableRewards.RemoveAt(selectedIndex);

            pendingEventEncounterData.pendingVictoryRelicRewards.Add(new RunRelicState
            {
                relicData = selectedReward.relicData,
                amount = 1
            });
            descriptions.Add(selectedReward.relicData.DisplayName);
        }

        message = descriptions.Count > 0
            ? $"战斗胜利后将获得遗物：{string.Join("，", descriptions)}。"
            : "战斗胜利后未配置任何遗物奖励。";
        return true;
    }

    private bool TryQueueEncounterVictoryBlueprintRewards(MapEventBuiltInEffectData effectData, out string message)
    {
        if (effectData.blueprintRewards == null || effectData.blueprintRewards.Count == 0)
        {
            message = "该事件效果未配置图纸列表。";
            return false;
        }

        List<MapEventBlueprintRewardEntry> availableRewards = new List<MapEventBlueprintRewardEntry>(effectData.blueprintRewards.Count);
        for (int i = 0; i < effectData.blueprintRewards.Count; i++)
        {
            MapEventBlueprintRewardEntry reward = effectData.blueprintRewards[i];
            if (reward != null && reward.blueprintData != null)
                availableRewards.Add(reward);
        }

        if (availableRewards.Count == 0)
        {
            message = "该事件效果未配置有效的图纸。";
            return false;
        }

        int minPickCount = Mathf.Max(1, effectData.blueprintPickCountMin);
        int maxPickCount = Mathf.Max(minPickCount, effectData.blueprintPickCountMax);
        int pickCount = Mathf.Min(UnityEngine.Random.Range(minPickCount, maxPickCount + 1), availableRewards.Count);
        List<string> descriptions = new List<string>(pickCount);

        for (int i = 0; i < pickCount; i++)
        {
            int selectedIndex = UnityEngine.Random.Range(0, availableRewards.Count);
            MapEventBlueprintRewardEntry selectedReward = availableRewards[selectedIndex];
            availableRewards.RemoveAt(selectedIndex);

            pendingEventEncounterData.pendingVictoryBlueprintRewards.Add(new RunBlueprintState
            {
                blueprintData = selectedReward.blueprintData,
                amount = 1
            });
            descriptions.Add(selectedReward.blueprintData.DisplayName);
        }

        message = descriptions.Count > 0
            ? $"战斗胜利后将获得图纸：{string.Join("，", descriptions)}。"
            : "战斗胜利后未配置任何图纸奖励。";
        return true;
    }

    private void SpendActionPointsWithOverflow(int amount)
    {
        if (actionPointSystem == null)
            return;

        int remaining = Mathf.Max(0, amount);
        int guard = 0;
        while (remaining > 0 && guard < 10000)
        {
            guard++;
            int currentPoints = actionPointSystem.CurrentPoints;
            if (currentPoints <= 0)
                break;

            int spendAmount = Mathf.Min(currentPoints, remaining);
            if (!actionPointSystem.TrySpend(spendAmount))
                break;

            remaining -= spendAmount;
        }
    }

    private void HandleMapEventOverlayHidden()
    {
        eventRandomLootDiscardPreviewCache.Clear();
        deferPendingCalamityUntilOverlayClosed = false;
        if (pendingEventEncounterData != null)
        {
            StartPendingEventEncounter();
            return;
        }
        TryPresentPendingCalamity();
    }

    private void StartPendingEventEncounter()
    {
        if (pendingEventEncounterData == null)
            return;

        if (sceneTransitionController != null && sceneTransitionController.IsTransitioning)
            return;

        BattleEncounterData encounterData = pendingEventEncounterData;
        pendingEventEncounterData = null;

        MapRunRuntime.SaveSnapshot(SceneManager.GetActiveScene().name, runState, boardManager, actionPointSystem, pendingCalamityTiers);
        BattleEncounterRuntime.SetEncounter(encounterData);
        if (pendingCalamityTiers.Count > 0)
            deferPendingCalamityUntilMapResume = true;

        RefreshHUD($"进入战斗：{encounterData.encounterLabel}");
        if (sceneTransitionController != null)
            sceneTransitionController.TransitionToScene(battleSceneName);
        else
            SceneManager.LoadScene(battleSceneName);
    }

    private void HandleMapMerchantOverlayHidden()
    {
        deferPendingCalamityUntilOverlayClosed = false;
        TryPresentPendingCalamity();
    }

    private void HandleMapCraftingOverlayHidden()
    {
        deferPendingCalamityUntilOverlayClosed = false;
        TryPresentPendingCalamity();
    }

    private bool IsMapViewInputBlocked()
    {
        if (mapEventOverlay != null && mapEventOverlay.IsVisible)
            return true;
        if (mapMerchantOverlay != null && mapMerchantOverlay.IsVisible)
            return true;
        if (mapCraftingOverlay != null && mapCraftingOverlay.IsVisible)
            return true;
        if (mapCalamityOverlay != null && mapCalamityOverlay.IsVisible)
            return true;

        return false;
    }

    private void TryPresentPendingCalamity()
    {
        if (runState == null || mapCalamityOverlay == null)
            return;
        if (deferPendingCalamityUntilMapResume)
            return;
        if (deferPendingCalamityUntilOverlayClosed)
            return;
        if (mapCalamityOverlay.IsVisible)
            return;
        if (mapEventOverlay != null && mapEventOverlay.IsVisible)
            return;
        if (mapMerchantOverlay != null && mapMerchantOverlay.IsVisible)
            return;
        if (mapCraftingOverlay != null && mapCraftingOverlay.IsVisible)
            return;

        while (pendingCalamityTiers.Count > 0)
        {
            int calamityTier = Mathf.Max(1, pendingCalamityTiers.Dequeue());
            List<Calamity_SO> choices = RollPendingCalamityChoices(calamityTier);
            if (choices.Count <= 0)
            {
                RefreshHUD($"\u707e\u5384\u7b49\u7ea7 {calamityTier} \u5df2\u65e0\u53ef\u7528\u707e\u5384\uff0c\u8df3\u8fc7\u672c\u6b21\u707e\u5384\u9009\u62e9\u3002");
                continue;
            }

            for (int i = 0; i < choices.Count; i++)
                runState.MarkCalamityDrawn(choices[i]);

            RefreshHUD($"\u707e\u5384\u964d\u4e34\uff0c\u8bf7\u4ece {choices.Count} \u4e2a\u9009\u9879\u4e2d\u9009\u62e9\u5176\u4e00\u3002");
            mapCalamityOverlay.Show(calamityTier, choices, calamity => HandleCalamityChosen(calamityTier, calamity));
            return;
        }
    }

    private List<Calamity_SO> RollPendingCalamityChoices(int calamityTier)
    {
        if (calamityPool == null)
            return new List<Calamity_SO>();

        HashSet<string> excludedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (runState != null && runState.drawnCalamityIds != null)
        {
            for (int i = 0; i < runState.drawnCalamityIds.Count; i++)
            {
                string id = runState.drawnCalamityIds[i];
                if (!string.IsNullOrWhiteSpace(id))
                    excludedIds.Add(id);
            }
        }

        int seed = unchecked(Environment.TickCount ^ (calamityTier * 397) ^ (runState != null ? runState.currentCycle * 811 : 0) ^ excludedIds.Count);
        System.Random rng = new System.Random(seed);
        return calamityPool.RollChoices(calamityTier, excludedIds, 2, rng);
    }

    private void HandleCalamityChosen(int calamityTier, Calamity_SO calamity)
    {
        if (runState == null || calamity == null)
        {
            TryPresentPendingCalamity();
            return;
        }

        runState.AddCalamity(calamity, calamityTier);
        calamity.customEffectHandler?.OnCalamityApplied(runState, this);
        RefreshHUD($"\u5df2\u627f\u53d7\u707e\u5384\uff1a{calamity.DisplayName}");
        TryPresentPendingCalamity();
    }

    private bool WillSpendExhaustCurrentCycle(int moveCost)
    {
        if (actionPointSystem == null)
            return false;

        int safeMoveCost = Mathf.Max(0, moveCost);
        if (safeMoveCost <= 0)
            return false;

        return actionPointSystem.CurrentPoints <= safeMoveCost;
    }

    private EnemyGroup_SO SelectEnemyGroupForTile(BoardTileData tile)
    {
        if (tile == null || enemyRoster == null)
            return null;

        System.Random rng = new System.Random(tile.encounterSeed);
        switch (tile.tileType)
        {
            case MapTileType.Elite:
                return enemyRoster.GetRandomGroup(tile.dangerBand, EnemyPoolKind.Elite, rng);
            case MapTileType.Boss:
                return enemyRoster.GetRandomBossGroup(rng);
            case MapTileType.Battle:
            default:
                return enemyRoster.GetRandomGroup(tile.dangerBand, EnemyPoolKind.Standard, rng);
        }
    }

    private void EnsureTileAssignedEnemyGroup(BoardTileData tile)
    {
        if (tile == null || !tile.IsHostileTile)
            return;
        if (tile.assignedEnemyGroup != null && tile.assignedEnemyGroup.IsValid)
            return;
        if (enemyRoster == null)
            return;

        tile.assignedEnemyGroup = SelectEnemyGroupForTile(tile);
    }

    private void EnsureTileAssignedEvent(BoardTileData tile)
    {
        if (tile == null || tile.tileType != MapTileType.Event)
            return;
        if (tile.assignedEvent != null)
            return;
        if (eventPool == null)
            return;

        System.Random rng = new System.Random(tile.encounterSeed ^ 0x53A4E71);
        IReadOnlyList<MapEvent_SO> validEvents = eventPool.GetValidEvents();
        if (validEvents == null || validEvents.Count == 0)
            return;

        List<MapEvent_SO> availableEvents = new List<MapEvent_SO>(validEvents.Count);
        for (int i = 0; i < validEvents.Count; i++)
        {
            MapEvent_SO eventData = validEvents[i];
            if (eventData == null)
                continue;
            if (runState != null && runState.HasDrawnEvent(eventData))
                continue;

            availableEvents.Add(eventData);
        }

        if (availableEvents.Count == 0)
            return;

        tile.assignedEvent = availableEvents[rng.Next(0, availableEvents.Count)];
        runState?.MarkEventDrawn(tile.assignedEvent);
    }

    private void ShowPendingMoveRelicStatusIfAny()
    {
        if (string.IsNullOrWhiteSpace(pendingMoveRelicStatusMessage) || mapHUD == null)
            return;

        mapHUD.ShowStatus(pendingMoveRelicStatusMessage);
        pendingMoveRelicStatusMessage = null;
    }

    private void AssignEnemyGroupsToHostileTiles()
    {
        if (boardManager == null || enemyRoster == null)
            return;

        foreach (BoardTileData tile in boardManager.GetAllTiles())
            EnsureTileAssignedEnemyGroup(tile);
    }

    private void EnsureMerchantStock(BoardTileData tile)
    {
        if (tile == null || tile.tileType != MapTileType.Merchant || tile.merchantStockGenerated || merchantCatalog == null)
            return;

        IReadOnlyList<MerchantCatalog_SO.LootGoodsEntry> validLootEntries = merchantCatalog.GetValidLootEntries();
        IReadOnlyList<MerchantCatalog_SO.WeaponGoodsEntry> validWeaponEntries = merchantCatalog.GetValidWeaponEntries();
        IReadOnlyList<MerchantCatalog_SO.BlueprintGoodsEntry> validBlueprintEntries = merchantCatalog.GetValidBlueprintEntries();
        IReadOnlyList<MerchantCatalog_SO.RelicGoodsEntry> validRelicEntries = merchantCatalog.GetValidRelicEntries();
        List<MerchantCatalog_SO.RelicGoodsEntry> filteredRelicEntries = FilterOwnedRelicEntries(validRelicEntries);

        bool hasAnyValidEntries =
            (validLootEntries != null && validLootEntries.Count > 0) ||
            (validWeaponEntries != null && validWeaponEntries.Count > 0) ||
            (validBlueprintEntries != null && validBlueprintEntries.Count > 0) ||
            (filteredRelicEntries != null && filteredRelicEntries.Count > 0);

        if (!hasAnyValidEntries)
        {
            tile.merchantOffers = new List<MerchantOfferState>();
            tile.merchantStockGenerated = true;
            return;
        }

        System.Random rng = CreateMerchantRandom(tile, tile.merchantRefreshCount);
        List<MerchantOfferState> generated = new List<MerchantOfferState>();
        List<MerchantCatalog_SO.LootGoodsEntry> remainingLootEntries = new List<MerchantCatalog_SO.LootGoodsEntry>(validLootEntries);
        List<MerchantCatalog_SO.WeaponGoodsEntry> remainingWeaponEntries = new List<MerchantCatalog_SO.WeaponGoodsEntry>(validWeaponEntries);
        List<MerchantCatalog_SO.BlueprintGoodsEntry> remainingBlueprintEntries = new List<MerchantCatalog_SO.BlueprintGoodsEntry>(validBlueprintEntries);
        List<MerchantCatalog_SO.RelicGoodsEntry> remainingRelicEntries = filteredRelicEntries != null
            ? new List<MerchantCatalog_SO.RelicGoodsEntry>(filteredRelicEntries)
            : new List<MerchantCatalog_SO.RelicGoodsEntry>();
        HashSet<Relic_SO> excludedRelics = BuildOwnedRelicSet();
        DifficultyConfig_SO merchantDifficulty = ResolveMerchantDifficultyConfig();

        int targetTotalCount = ResolveMerchantTargetTotalCount(
            merchantCatalog,
            remainingLootEntries.Count + remainingWeaponEntries.Count + remainingBlueprintEntries.Count + remainingRelicEntries.Count,
            rng);

        AddGuaranteedMerchantOffersFromLootPool(generated, remainingLootEntries, merchantCatalog.lootMinimumCount, rng);
        AddGuaranteedMerchantOffersFromWeaponPool(generated, remainingWeaponEntries, merchantCatalog.weaponMinimumCount, rng);
        AddGuaranteedMerchantOffersFromBlueprintPool(generated, remainingBlueprintEntries, merchantCatalog.blueprintMinimumCount, rng);
        AddGuaranteedMerchantOffersFromRelicPool(generated, remainingRelicEntries, merchantCatalog.relicMinimumCount, excludedRelics, merchantDifficulty, rng);

        FillRemainingMerchantOffers(
            generated,
            remainingLootEntries,
            remainingWeaponEntries,
            remainingBlueprintEntries,
            remainingRelicEntries,
            merchantCatalog,
            targetTotalCount,
            excludedRelics,
            merchantDifficulty,
            rng);

        ShuffleMerchantOffers(generated, rng);

        tile.merchantOffers = generated;
        tile.merchantStockGenerated = true;
    }

    private List<MerchantCatalog_SO.RelicGoodsEntry> FilterOwnedRelicEntries(IReadOnlyList<MerchantCatalog_SO.RelicGoodsEntry> validRelicEntries)
    {
        List<MerchantCatalog_SO.RelicGoodsEntry> filtered = new List<MerchantCatalog_SO.RelicGoodsEntry>();
        if (validRelicEntries == null || validRelicEntries.Count == 0)
            return filtered;

        HashSet<Relic_SO> excludedRelics = BuildOwnedRelicSet();
        for (int i = 0; i < validRelicEntries.Count; i++)
        {
            MerchantCatalog_SO.RelicGoodsEntry entry = validRelicEntries[i];
            if (!HasAvailableRelicCandidate(entry, excludedRelics))
                continue;

            filtered.Add(entry);
        }

        return filtered;
    }

    private static void AddGuaranteedMerchantOffersFromLootPool(
        List<MerchantOfferState> generated,
        List<MerchantCatalog_SO.LootGoodsEntry> remainingEntries,
        MerchantCatalog_SO.CategoryMinimumEntry minimumEntry,
        System.Random rng)
    {
        if (generated == null || remainingEntries == null || remainingEntries.Count == 0 || minimumEntry == null)
            return;

        int targetCount = Mathf.Clamp(minimumEntry.ResolveMinimumCount(), 0, remainingEntries.Count);
        for (int i = 0; i < targetCount && remainingEntries.Count > 0; i++)
        {
            int selectedIndex = RollWeightedEntryIndex(remainingEntries, entry => entry.weight, rng);
            MerchantCatalog_SO.LootGoodsEntry goods = remainingEntries[selectedIndex];
            remainingEntries.RemoveAt(selectedIndex);
            generated.Add(new MerchantOfferState
            {
                itemType = MerchantItemType.Loot,
                lootItem = goods.lootItem,
                price = RollMerchantPrice(goods.minPrice, goods.maxPrice, rng),
                isSoldOut = false
            });
        }
    }

    private static void AddGuaranteedMerchantOffersFromWeaponPool(
        List<MerchantOfferState> generated,
        List<MerchantCatalog_SO.WeaponGoodsEntry> remainingEntries,
        MerchantCatalog_SO.CategoryMinimumEntry minimumEntry,
        System.Random rng)
    {
        if (generated == null || remainingEntries == null || remainingEntries.Count == 0 || minimumEntry == null)
            return;

        int targetCount = Mathf.Clamp(minimumEntry.ResolveMinimumCount(), 0, remainingEntries.Count);
        for (int i = 0; i < targetCount && remainingEntries.Count > 0; i++)
        {
            int selectedIndex = RollWeightedEntryIndex(remainingEntries, entry => entry.weight, rng);
            MerchantCatalog_SO.WeaponGoodsEntry goods = remainingEntries[selectedIndex];
            remainingEntries.RemoveAt(selectedIndex);
            generated.Add(new MerchantOfferState
            {
                itemType = MerchantItemType.Weapon,
                weaponData = goods.weaponData,
                price = RollMerchantPrice(goods.minPrice, goods.maxPrice, rng),
                isSoldOut = false
            });
        }
    }

    private static void AddGuaranteedMerchantOffersFromBlueprintPool(
        List<MerchantOfferState> generated,
        List<MerchantCatalog_SO.BlueprintGoodsEntry> remainingEntries,
        MerchantCatalog_SO.CategoryMinimumEntry minimumEntry,
        System.Random rng)
    {
        if (generated == null || remainingEntries == null || remainingEntries.Count == 0 || minimumEntry == null)
            return;

        int targetCount = Mathf.Clamp(minimumEntry.ResolveMinimumCount(), 0, remainingEntries.Count);
        for (int i = 0; i < targetCount && remainingEntries.Count > 0; i++)
        {
            int selectedIndex = RollWeightedEntryIndex(remainingEntries, entry => entry.weight, rng);
            MerchantCatalog_SO.BlueprintGoodsEntry goods = remainingEntries[selectedIndex];
            remainingEntries.RemoveAt(selectedIndex);
            generated.Add(new MerchantOfferState
            {
                itemType = MerchantItemType.Blueprint,
                blueprintData = goods.blueprintData,
                price = RollMerchantPrice(goods.minPrice, goods.maxPrice, rng),
                isSoldOut = false
            });
        }
    }

    private static void AddGuaranteedMerchantOffersFromRelicPool(
        List<MerchantOfferState> generated,
        List<MerchantCatalog_SO.RelicGoodsEntry> remainingEntries,
        MerchantCatalog_SO.CategoryMinimumEntry minimumEntry,
        HashSet<Relic_SO> excludedRelics,
        DifficultyConfig_SO merchantDifficulty,
        System.Random rng)
    {
        if (generated == null || remainingEntries == null || remainingEntries.Count == 0 || minimumEntry == null)
            return;

        int targetCount = Mathf.Clamp(minimumEntry.ResolveMinimumCount(), 0, remainingEntries.Count);
        for (int i = 0; i < targetCount && remainingEntries.Count > 0; i++)
        {
            List<MerchantCatalog_SO.RelicGoodsEntry> availableEntries = BuildAvailableRelicGoodsEntries(remainingEntries, excludedRelics);
            if (availableEntries.Count <= 0)
                break;

            int selectedIndex = RollWeightedEntryIndex(availableEntries, entry => entry.weight, rng);
            MerchantCatalog_SO.RelicGoodsEntry goods = availableEntries[selectedIndex];
            remainingEntries.Remove(goods);

            if (TryCreateRelicMerchantOffer(goods, excludedRelics, merchantDifficulty, rng, out MerchantOfferState relicOffer))
                generated.Add(relicOffer);
        }
    }

    private static void FillRemainingMerchantOffers(
        List<MerchantOfferState> generated,
        List<MerchantCatalog_SO.LootGoodsEntry> remainingLootEntries,
        List<MerchantCatalog_SO.WeaponGoodsEntry> remainingWeaponEntries,
        List<MerchantCatalog_SO.BlueprintGoodsEntry> remainingBlueprintEntries,
        List<MerchantCatalog_SO.RelicGoodsEntry> remainingRelicEntries,
        MerchantCatalog_SO catalog,
        int targetTotalCount,
        HashSet<Relic_SO> excludedRelics,
        DifficultyConfig_SO merchantDifficulty,
        System.Random rng)
    {
        if (generated == null || rng == null)
            return;

        while (generated.Count < targetTotalCount)
        {
            List<MerchantCatalog_SO.RelicGoodsEntry> availableRelicEntries = BuildAvailableRelicGoodsEntries(remainingRelicEntries, excludedRelics);
            int totalWeight = 0;
            int lootCategoryWeight = GetAvailableCategoryWeight(remainingLootEntries, catalog != null ? catalog.lootMinimumCount : null);
            int weaponCategoryWeight = GetAvailableCategoryWeight(remainingWeaponEntries, catalog != null ? catalog.weaponMinimumCount : null);
            int blueprintCategoryWeight = GetAvailableCategoryWeight(remainingBlueprintEntries, catalog != null ? catalog.blueprintMinimumCount : null);
            int relicCategoryWeight = GetAvailableCategoryWeight(availableRelicEntries, catalog != null ? catalog.relicMinimumCount : null);
            totalWeight += lootCategoryWeight;
            totalWeight += weaponCategoryWeight;
            totalWeight += blueprintCategoryWeight;
            totalWeight += relicCategoryWeight;

            if (totalWeight <= 0)
                break;

            int roll = rng.Next(0, totalWeight);
            if (roll < lootCategoryWeight)
            {
                int selectedIndex = RollWeightedEntryIndex(remainingLootEntries, entry => entry.weight, rng);
                MerchantCatalog_SO.LootGoodsEntry goods = remainingLootEntries[selectedIndex];
                remainingLootEntries.RemoveAt(selectedIndex);
                generated.Add(new MerchantOfferState
                {
                    itemType = MerchantItemType.Loot,
                    lootItem = goods.lootItem,
                    price = RollMerchantPrice(goods.minPrice, goods.maxPrice, rng),
                    isSoldOut = false
                });
                continue;
            }

            roll -= lootCategoryWeight;
            if (roll < weaponCategoryWeight)
            {
                int selectedIndex = RollWeightedEntryIndex(remainingWeaponEntries, entry => entry.weight, rng);
                MerchantCatalog_SO.WeaponGoodsEntry goods = remainingWeaponEntries[selectedIndex];
                remainingWeaponEntries.RemoveAt(selectedIndex);
                generated.Add(new MerchantOfferState
                {
                    itemType = MerchantItemType.Weapon,
                    weaponData = goods.weaponData,
                    price = RollMerchantPrice(goods.minPrice, goods.maxPrice, rng),
                    isSoldOut = false
                });
                continue;
            }

            roll -= weaponCategoryWeight;
            if (roll < blueprintCategoryWeight)
            {
                int selectedIndex = RollWeightedEntryIndex(remainingBlueprintEntries, entry => entry.weight, rng);
                MerchantCatalog_SO.BlueprintGoodsEntry goods = remainingBlueprintEntries[selectedIndex];
                remainingBlueprintEntries.RemoveAt(selectedIndex);
                generated.Add(new MerchantOfferState
                {
                    itemType = MerchantItemType.Blueprint,
                    blueprintData = goods.blueprintData,
                    price = RollMerchantPrice(goods.minPrice, goods.maxPrice, rng),
                    isSoldOut = false
                });
                continue;
            }

            roll -= blueprintCategoryWeight;
            if (roll >= relicCategoryWeight || availableRelicEntries == null || availableRelicEntries.Count <= 0)
                break;

            int selectedRelicIndex = RollWeightedEntryIndex(availableRelicEntries, entry => entry.weight, rng);
            MerchantCatalog_SO.RelicGoodsEntry relicGoods = availableRelicEntries[selectedRelicIndex];
            remainingRelicEntries.Remove(relicGoods);
            if (TryCreateRelicMerchantOffer(relicGoods, excludedRelics, merchantDifficulty, rng, out MerchantOfferState relicOffer))
                generated.Add(relicOffer);
        }
    }

    private int CountRefreshableMerchantOffers(BoardTileData tile)
    {
        if (tile == null || tile.merchantOffers == null)
            return 0;

        int count = 0;
        for (int i = 0; i < tile.merchantOffers.Count; i++)
        {
            if (tile.merchantOffers[i] != null)
                count += 1;
        }

        return count;
    }

    private int RefreshMerchantOffers(BoardTileData tile, System.Random rng)
    {
        if (tile == null || tile.merchantOffers == null || rng == null)
            return 0;

        HashSet<LootItem_SO> excludedLoot = BuildCurrentMerchantLootSet(tile, null, true);
        HashSet<Weapon_SO> excludedWeapons = BuildOwnedWeaponSet();
        AppendCurrentMerchantWeapons(tile, excludedWeapons, null, true);
        HashSet<CraftingBlueprint_SO> excludedBlueprints = BuildOwnedBlueprintSet();
        AppendCurrentMerchantBlueprints(tile, excludedBlueprints, null, true);
        HashSet<Relic_SO> excludedRelics = BuildOwnedRelicSet();
        AppendCurrentMerchantRelics(tile, excludedRelics, null, true);

        int refreshedCount = 0;
        for (int i = 0; i < tile.merchantOffers.Count; i++)
        {
            MerchantOfferState offer = tile.merchantOffers[i];
            if (offer == null)
                continue;

            if (TryRefreshMerchantOffer(tile, offer, excludedLoot, excludedWeapons, excludedBlueprints, excludedRelics, rng))
                refreshedCount += 1;
        }

        return refreshedCount;
    }

    private bool TryRefreshMerchantOffer(
        BoardTileData tile,
        MerchantOfferState offer,
        HashSet<LootItem_SO> excludedLoot,
        HashSet<Weapon_SO> excludedWeapons,
        HashSet<CraftingBlueprint_SO> excludedBlueprints,
        HashSet<Relic_SO> excludedRelics,
        System.Random rng)
    {
        if (tile == null || offer == null || rng == null || merchantCatalog == null)
            return false;

        switch (offer.itemType)
        {
            case MerchantItemType.Loot:
                return TryRefreshLootMerchantOffer(offer, excludedLoot, rng);
            case MerchantItemType.Weapon:
                return TryRefreshWeaponMerchantOffer(offer, excludedWeapons, rng);
            case MerchantItemType.Blueprint:
                return TryRefreshBlueprintMerchantOffer(offer, excludedBlueprints, rng);
            case MerchantItemType.Relic:
                return TryRefreshRelicMerchantOffer(offer, excludedRelics, rng);
            default:
                return false;
        }
    }

    private bool TryRefreshLootMerchantOffer(MerchantOfferState offer, HashSet<LootItem_SO> excludedLoot, System.Random rng)
    {
        IReadOnlyList<MerchantCatalog_SO.LootGoodsEntry> validEntries = merchantCatalog.GetValidLootEntries();
        if (validEntries == null || validEntries.Count == 0)
            return false;

        List<MerchantCatalog_SO.LootGoodsEntry> candidates = new List<MerchantCatalog_SO.LootGoodsEntry>();
        for (int i = 0; i < validEntries.Count; i++)
        {
            MerchantCatalog_SO.LootGoodsEntry entry = validEntries[i];
            if (entry == null || entry.lootItem == null || (excludedLoot != null && excludedLoot.Contains(entry.lootItem)))
                continue;

            candidates.Add(entry);
        }

        if (candidates.Count <= 0)
            return false;

        MerchantCatalog_SO.LootGoodsEntry selected = candidates[RollWeightedEntryIndex(candidates, entry => entry.weight, rng)];
        ApplyMerchantOfferData(offer, MerchantItemType.Loot, RollMerchantPrice(selected.minPrice, selected.maxPrice, rng), selected.lootItem, null, null, null);
        excludedLoot?.Add(selected.lootItem);
        return true;
    }

    private bool TryRefreshWeaponMerchantOffer(MerchantOfferState offer, HashSet<Weapon_SO> excludedWeapons, System.Random rng)
    {
        IReadOnlyList<MerchantCatalog_SO.WeaponGoodsEntry> validEntries = merchantCatalog.GetValidWeaponEntries();
        if (validEntries == null || validEntries.Count == 0)
            return false;

        List<MerchantCatalog_SO.WeaponGoodsEntry> candidates = new List<MerchantCatalog_SO.WeaponGoodsEntry>();
        for (int i = 0; i < validEntries.Count; i++)
        {
            MerchantCatalog_SO.WeaponGoodsEntry entry = validEntries[i];
            if (entry == null || entry.weaponData == null || (excludedWeapons != null && excludedWeapons.Contains(entry.weaponData)))
                continue;

            candidates.Add(entry);
        }

        if (candidates.Count <= 0)
            return false;

        MerchantCatalog_SO.WeaponGoodsEntry selected = candidates[RollWeightedEntryIndex(candidates, entry => entry.weight, rng)];
        ApplyMerchantOfferData(offer, MerchantItemType.Weapon, RollMerchantPrice(selected.minPrice, selected.maxPrice, rng), null, selected.weaponData, null, null);
        excludedWeapons?.Add(selected.weaponData);
        return true;
    }

    private bool TryRefreshBlueprintMerchantOffer(MerchantOfferState offer, HashSet<CraftingBlueprint_SO> excludedBlueprints, System.Random rng)
    {
        IReadOnlyList<MerchantCatalog_SO.BlueprintGoodsEntry> validEntries = merchantCatalog.GetValidBlueprintEntries();
        if (validEntries == null || validEntries.Count == 0)
            return false;

        List<MerchantCatalog_SO.BlueprintGoodsEntry> candidates = new List<MerchantCatalog_SO.BlueprintGoodsEntry>();
        for (int i = 0; i < validEntries.Count; i++)
        {
            MerchantCatalog_SO.BlueprintGoodsEntry entry = validEntries[i];
            if (entry == null || entry.blueprintData == null || (excludedBlueprints != null && excludedBlueprints.Contains(entry.blueprintData)))
                continue;

            candidates.Add(entry);
        }

        if (candidates.Count <= 0)
            return false;

        MerchantCatalog_SO.BlueprintGoodsEntry selected = candidates[RollWeightedEntryIndex(candidates, entry => entry.weight, rng)];
        ApplyMerchantOfferData(offer, MerchantItemType.Blueprint, RollMerchantPrice(selected.minPrice, selected.maxPrice, rng), null, null, selected.blueprintData, null);
        excludedBlueprints?.Add(selected.blueprintData);
        return true;
    }

    private bool TryRefreshRelicMerchantOffer(MerchantOfferState offer, HashSet<Relic_SO> excludedRelics, System.Random rng)
    {
        IReadOnlyList<MerchantCatalog_SO.RelicGoodsEntry> validEntries = merchantCatalog.GetValidRelicEntries();
        if (validEntries == null || validEntries.Count == 0)
            return false;

        List<MerchantCatalog_SO.RelicGoodsEntry> candidates = BuildAvailableRelicGoodsEntries(validEntries, excludedRelics);
        if (candidates.Count <= 0)
            return false;

        MerchantCatalog_SO.RelicGoodsEntry selected = candidates[RollWeightedEntryIndex(candidates, entry => entry.weight, rng)];
        Relic_SO selectedRelic = null;
        if (selected.relicTable != null && selected.relicTable.IsValid)
            selectedRelic = selected.relicTable.DrawRandomRelic(rng, excludedRelics);
        else
            selectedRelic = selected.LegacyRelicData;

        if (selectedRelic == null)
            return false;
        if (excludedRelics.Contains(selectedRelic))
            return false;

        ApplyMerchantOfferData(offer, MerchantItemType.Relic, ResolveMerchantRelicPrice(selectedRelic, rng), null, null, null, selectedRelic);
        excludedRelics?.Add(selectedRelic);
        return true;
    }

    private static void ApplyMerchantOfferData(
        MerchantOfferState offer,
        MerchantItemType itemType,
        int price,
        LootItem_SO lootItem,
        Weapon_SO weaponData,
        CraftingBlueprint_SO blueprintData,
        Relic_SO relicData)
    {
        if (offer == null)
            return;

        offer.itemType = itemType;
        offer.lootItem = lootItem;
        offer.weaponData = weaponData;
        offer.blueprintData = blueprintData;
        offer.relicData = relicData;
        offer.price = Mathf.Max(0, price);
        offer.isSoldOut = false;
    }

    private static System.Random CreateMerchantRandom(BoardTileData tile, int refreshSalt)
    {
        int baseSeed = tile != null ? tile.encounterSeed : 0;
        unchecked
        {
            int combinedSeed = (baseSeed * 397) ^ 0x2A57C9 ^ (refreshSalt * 486187739);
            return new System.Random(combinedSeed);
        }
    }

    private HashSet<Relic_SO> BuildOwnedRelicSet()
    {
        HashSet<Relic_SO> owned = new HashSet<Relic_SO>();
        if (runState == null || runState.relicInventory == null)
            return owned;

        for (int i = 0; i < runState.relicInventory.Count; i++)
        {
            RunRelicState relic = runState.relicInventory[i];
            if (relic == null || relic.relicData == null || relic.amount <= 0)
                continue;

            owned.Add(relic.relicData);
        }

        return owned;
    }

    private HashSet<Weapon_SO> BuildOwnedWeaponSet()
    {
        HashSet<Weapon_SO> owned = new HashSet<Weapon_SO>();
        if (runState == null || runState.weaponInventory == null)
            return owned;

        for (int i = 0; i < runState.weaponInventory.Count; i++)
        {
            RunWeaponState weapon = runState.weaponInventory[i];
            if (weapon == null || weapon.weaponData == null || weapon.amount <= 0)
                continue;

            owned.Add(weapon.weaponData);
        }

        return owned;
    }

    private HashSet<CraftingBlueprint_SO> BuildOwnedBlueprintSet()
    {
        HashSet<CraftingBlueprint_SO> owned = new HashSet<CraftingBlueprint_SO>();
        if (runState == null || runState.blueprintInventory == null)
            return owned;

        for (int i = 0; i < runState.blueprintInventory.Count; i++)
        {
            RunBlueprintState blueprint = runState.blueprintInventory[i];
            if (blueprint == null || blueprint.blueprintData == null || blueprint.amount <= 0)
                continue;

            owned.Add(blueprint.blueprintData);
        }

        return owned;
    }

    private static HashSet<LootItem_SO> BuildCurrentMerchantLootSet(BoardTileData tile, MerchantOfferState ignoreOffer, bool includeSoldOut = false)
    {
        HashSet<LootItem_SO> offered = new HashSet<LootItem_SO>();
        if (tile == null || tile.merchantOffers == null)
            return offered;

        for (int i = 0; i < tile.merchantOffers.Count; i++)
        {
            MerchantOfferState offer = tile.merchantOffers[i];
            if (offer == null || offer == ignoreOffer || (!includeSoldOut && offer.isSoldOut) || offer.itemType != MerchantItemType.Loot || offer.lootItem == null)
                continue;

            offered.Add(offer.lootItem);
        }

        return offered;
    }

    private static void AppendCurrentMerchantWeapons(BoardTileData tile, HashSet<Weapon_SO> targetSet, MerchantOfferState ignoreOffer, bool includeSoldOut = false)
    {
        if (tile == null || tile.merchantOffers == null || targetSet == null)
            return;

        for (int i = 0; i < tile.merchantOffers.Count; i++)
        {
            MerchantOfferState offer = tile.merchantOffers[i];
            if (offer == null || offer == ignoreOffer || (!includeSoldOut && offer.isSoldOut) || offer.itemType != MerchantItemType.Weapon || offer.weaponData == null)
                continue;

            targetSet.Add(offer.weaponData);
        }
    }

    private static void AppendCurrentMerchantBlueprints(BoardTileData tile, HashSet<CraftingBlueprint_SO> targetSet, MerchantOfferState ignoreOffer, bool includeSoldOut = false)
    {
        if (tile == null || tile.merchantOffers == null || targetSet == null)
            return;

        for (int i = 0; i < tile.merchantOffers.Count; i++)
        {
            MerchantOfferState offer = tile.merchantOffers[i];
            if (offer == null || offer == ignoreOffer || (!includeSoldOut && offer.isSoldOut) || offer.itemType != MerchantItemType.Blueprint || offer.blueprintData == null)
                continue;

            targetSet.Add(offer.blueprintData);
        }
    }

    private static void AppendCurrentMerchantRelics(BoardTileData tile, HashSet<Relic_SO> targetSet, MerchantOfferState ignoreOffer, bool includeSoldOut = false)
    {
        if (tile == null || tile.merchantOffers == null || targetSet == null)
            return;

        for (int i = 0; i < tile.merchantOffers.Count; i++)
        {
            MerchantOfferState offer = tile.merchantOffers[i];
            if (offer == null || offer == ignoreOffer || (!includeSoldOut && offer.isSoldOut) || offer.itemType != MerchantItemType.Relic || offer.relicData == null)
                continue;

            targetSet.Add(offer.relicData);
        }
    }

    private static List<MerchantCatalog_SO.RelicGoodsEntry> BuildAvailableRelicGoodsEntries(IReadOnlyList<MerchantCatalog_SO.RelicGoodsEntry> source, ICollection<Relic_SO> excludedRelics)
    {
        List<MerchantCatalog_SO.RelicGoodsEntry> available = new List<MerchantCatalog_SO.RelicGoodsEntry>();
        if (source == null)
            return available;

        for (int i = 0; i < source.Count; i++)
        {
            MerchantCatalog_SO.RelicGoodsEntry entry = source[i];
            if (!HasAvailableRelicCandidate(entry, excludedRelics))
                continue;

            available.Add(entry);
        }

        return available;
    }

    private static bool HasAvailableRelicCandidate(MerchantCatalog_SO.RelicGoodsEntry entry, ICollection<Relic_SO> excludedRelics)
    {
        if (entry == null)
            return false;

        if (entry.relicTable != null && entry.relicTable.IsValid)
        {
            IReadOnlyList<Relic_SO> relics = entry.relicTable.Relics;
            if (relics == null || relics.Count == 0)
                return false;

            for (int i = 0; i < relics.Count; i++)
            {
                Relic_SO relic = relics[i];
                if (relic == null)
                    continue;

                if (excludedRelics != null && excludedRelics.Contains(relic))
                    continue;

                return true;
            }

            return false;
        }

        return entry.LegacyRelicData != null && (excludedRelics == null || !excludedRelics.Contains(entry.LegacyRelicData));
    }

    private static bool TryCreateRelicMerchantOffer(
        MerchantCatalog_SO.RelicGoodsEntry goods,
        HashSet<Relic_SO> excludedRelics,
        DifficultyConfig_SO merchantDifficulty,
        System.Random rng,
        out MerchantOfferState offer)
    {
        offer = null;
        if (goods == null || rng == null)
            return false;

        Relic_SO relicData = null;
        if (goods.relicTable != null && goods.relicTable.IsValid)
            relicData = goods.relicTable.DrawRandomRelic(rng, excludedRelics);
        else
            relicData = goods.LegacyRelicData;

        if (relicData == null)
            return false;

        if (excludedRelics != null && excludedRelics.Contains(relicData))
            return false;

        if (excludedRelics != null)
            excludedRelics.Add(relicData);

        offer = new MerchantOfferState
        {
            itemType = MerchantItemType.Relic,
            relicData = relicData,
            price = ResolveMerchantRelicPrice(merchantDifficulty, relicData, rng),
            isSoldOut = false
        };
        return true;
    }

    private static int ResolveMerchantTargetTotalCount(MerchantCatalog_SO catalog, int totalAvailableCount, System.Random rng)
    {
        if (catalog == null || totalAvailableCount <= 0)
            return 0;

        int minimumRequiredCount =
            Mathf.Max(0, catalog.lootMinimumCount != null ? catalog.lootMinimumCount.ResolveMinimumCount() : 0) +
            Mathf.Max(0, catalog.weaponMinimumCount != null ? catalog.weaponMinimumCount.ResolveMinimumCount() : 0) +
            Mathf.Max(0, catalog.blueprintMinimumCount != null ? catalog.blueprintMinimumCount.ResolveMinimumCount() : 0) +
            Mathf.Max(0, catalog.relicMinimumCount != null ? catalog.relicMinimumCount.ResolveMinimumCount() : 0);

        int minCount = Mathf.Clamp(Mathf.Max(minimumRequiredCount, catalog.minTotalGoodsCount), 0, totalAvailableCount);
        int maxCount = Mathf.Clamp(Mathf.Max(minCount, catalog.maxTotalGoodsCount), minCount, totalAvailableCount);
        return maxCount <= minCount ? minCount : rng.Next(minCount, maxCount + 1);
    }

    private static int GetAvailableCategoryWeight<TEntry>(IReadOnlyList<TEntry> pool, MerchantCatalog_SO.CategoryMinimumEntry categoryEntry)
    {
        if (pool == null || pool.Count == 0 || categoryEntry == null)
            return 0;

        return Mathf.Max(0, categoryEntry.ResolveRandomWeight());
    }

    private static int RollWeightedEntryIndex<TEntry>(IReadOnlyList<TEntry> pool, Func<TEntry, int> weightSelector, System.Random rng)
    {
        int totalWeight = 0;
        for (int i = 0; i < pool.Count; i++)
            totalWeight += Mathf.Max(1, weightSelector(pool[i]));

        int roll = rng.Next(0, totalWeight);
        for (int i = 0; i < pool.Count; i++)
        {
            roll -= Mathf.Max(1, weightSelector(pool[i]));
            if (roll < 0)
                return i;
        }

        return Mathf.Clamp(pool.Count - 1, 0, int.MaxValue);
    }

    private static void ShuffleMerchantOffers(List<MerchantOfferState> offers, System.Random rng)
    {
        if (offers == null || rng == null)
            return;

        for (int i = offers.Count - 1; i > 0; i--)
        {
            int swapIndex = rng.Next(0, i + 1);
            MerchantOfferState temp = offers[i];
            offers[i] = offers[swapIndex];
            offers[swapIndex] = temp;
        }
    }

    private static int RollMerchantPrice(int minPrice, int maxPrice, System.Random rng)
    {
        int min = Mathf.Max(1, minPrice);
        int max = Mathf.Max(min, maxPrice);
        return max <= min ? min : rng.Next(min, max + 1);
    }

    private void ApplyExperienceToAllPartyMembers(int amount)
    {
        if (runState == null || runState.partyMembers == null || amount <= 0)
            return;

        for (int i = 0; i < runState.partyMembers.Count; i++)
        {
            RunPartyMemberState member = runState.partyMembers[i];
            if (member == null)
                continue;

            ApplyExperienceToPartyMember(member, amount);
        }

        runState.SyncLegacyPlayerFieldsFromParty();
    }

    private void ApplyExperienceToPartyMember(RunPartyMemberState member, int amount)
    {
        if (member == null || amount <= 0)
            return;

        int currentLevel = Mathf.Max(1, member.level);
        int currentExperience = Mathf.Max(0, member.experience);
        int currentMaxHP = Mathf.Max(1, member.maxHP);
        int currentHP = Mathf.Clamp(member.currentHP, 0, currentMaxHP);
        int remaining = Mathf.Max(0, amount);

        while (remaining > 0)
        {
            int required = ResolveEventExperienceToNextLevel(currentLevel);
            if (required <= 0)
                break;

            currentExperience = Mathf.Clamp(currentExperience, 0, required);
            int needed = Mathf.Max(0, required - currentExperience);
            if (remaining < needed)
            {
                currentExperience += remaining;
                remaining = 0;
                break;
            }

            remaining -= needed;
            currentLevel += 1;
            currentExperience = 0;

            int upgradedMaxHP = ResolveEventMaxHPForLevel(member.characterData, currentLevel, currentMaxHP);
            currentMaxHP = upgradedMaxHP;
            if (currentHP > 0)
            {
                int healAmount = Mathf.CeilToInt(upgradedMaxHP * 0.5f);
                currentHP = Mathf.Clamp(currentHP + healAmount, 0, upgradedMaxHP);
            }
        }

        member.level = currentLevel;
        member.experience = currentExperience;
        member.maxHP = currentMaxHP;
        member.currentHP = Mathf.Clamp(currentHP, 0, currentMaxHP);
    }

    private int ResolveEventExperienceToNextLevel(int level)
    {
        if (levelTable == null)
            return 100;

        return Mathf.Max(0, levelTable.GetExperienceToNextLevel(level));
    }

    private int ResolveEventMaxHPForLevel(CharacterData_SO characterData, int level, int fallbackMaxHP)
    {
        if (levelTable == null)
            return Mathf.Max(1, fallbackMaxHP);

        return Mathf.Max(1, levelTable.GetMaxHP(characterData, level, fallbackMaxHP));
    }

    private List<RunPartyMemberState> BuildStartingPartyMembers()
    {
        List<RunPartyMemberState> result = new List<RunPartyMemberState>();
        for (int i = 0; i < startingPartyMembers.Count; i++)
        {
            StartingPartyMemberDefinition definition = startingPartyMembers[i];
            if (definition == null || definition.characterData == null)
                continue;

            int baseHP = Mathf.Max(1, definition.characterData.baseHP);
            Weapon_SO resolvedStartingWeapon = definition.equippedWeapon != null
                ? definition.equippedWeapon
                : definition.characterData.initialWeaponData;
            result.Add(new RunPartyMemberState
            {
                memberId = string.IsNullOrWhiteSpace(definition.memberId) ? definition.characterData.characterName : definition.memberId,
                characterData = definition.characterData,
                weaponData = resolvedStartingWeapon,
                level = Mathf.Max(1, definition.startingLevel),
                experience = Mathf.Max(0, definition.startingExperience),
                maxHP = definition.startingMaxHP > 0 ? Mathf.Max(1, definition.startingMaxHP) : baseHP,
                currentHP = definition.startingCurrentHP > 0
                    ? Mathf.Clamp(definition.startingCurrentHP, 0, definition.startingMaxHP > 0 ? Mathf.Max(1, definition.startingMaxHP) : baseHP)
                    : (definition.startingMaxHP > 0 ? Mathf.Max(1, definition.startingMaxHP) : baseHP)
            });
        }

        if (result.Count == 0)
        {
            result.Add(new RunPartyMemberState
            {
                memberId = "DefaultPartyMember",
                level = 1,
                experience = 0,
                maxHP = 20,
                currentHP = 20
            });
        }

        return result;
    }

    private void ApplyStartingInventory(RunState targetRunState)
    {
        if (targetRunState == null)
            return;

        if (startingLoot != null)
        {
            for (int i = 0; i < startingLoot.Count; i++)
            {
                StartingLootDefinition definition = startingLoot[i];
                if (definition == null || definition.lootItem == null || definition.amount <= 0)
                    continue;

                targetRunState.AddLoot(definition.lootItem, definition.amount);
            }
        }

        if (startingWeapons != null)
        {
            for (int i = 0; i < startingWeapons.Count; i++)
            {
                StartingWeaponDefinition definition = startingWeapons[i];
                if (definition == null || definition.weaponData == null || definition.amount <= 0)
                    continue;

                targetRunState.AddWeapon(definition.weaponData, definition.amount);
            }
        }

        if (startingBlueprints != null)
        {
            for (int i = 0; i < startingBlueprints.Count; i++)
            {
                StartingBlueprintDefinition definition = startingBlueprints[i];
                if (definition == null || definition.blueprintData == null || definition.amount <= 0)
                    continue;

                targetRunState.AddBlueprint(definition.blueprintData, definition.amount);
            }
        }

        if (startingRelics != null)
        {
            for (int i = 0; i < startingRelics.Count; i++)
            {
                StartingRelicDefinition definition = startingRelics[i];
                if (definition == null || definition.relicData == null || definition.amount <= 0)
                    continue;

                targetRunState.AddRelic(definition.relicData, definition.amount);
            }
        }

        if (targetRunState.partyMembers == null)
            return;

        for (int i = 0; i < targetRunState.partyMembers.Count; i++)
        {
            RunPartyMemberState member = targetRunState.partyMembers[i];
            if (member == null || member.weaponData == null)
                continue;

            targetRunState.AddWeapon(member.weaponData, 1);
        }
    }

    private void PersistCurrentRunSave()
    {
        if (runState == null || boardManager == null || actionPointSystem == null)
            return;

        if (!SceneManager.GetActiveScene().name.Equals("MapScene", StringComparison.OrdinalIgnoreCase))
            return;

        MainMenuRunSaveSystem.SaveSnapshot(
            SceneManager.GetActiveScene().name,
            runState,
            boardManager,
            actionPointSystem,
            pendingCalamityTiers,
            suppressPendingCalamityOnNextRestActionPointRestore);
    }

    private MainMenuSaveAssetCatalog BuildSaveAssetCatalog()
    {
        MainMenuSaveAssetCatalog catalog = new MainMenuSaveAssetCatalog();

        DifficultyConfig_SO selectedDifficulty = MainMenuRuntime.SelectedDifficultyConfig;
        AddDifficultyAssets(catalog, difficultyConfig);
        AddDifficultyAssets(catalog, activeDifficultyConfig);
        AddDifficultyAssets(catalog, selectedDifficulty);

        if (startingPartyMembers != null)
        {
            for (int i = 0; i < startingPartyMembers.Count; i++)
            {
                StartingPartyMemberDefinition definition = startingPartyMembers[i];
                if (definition == null)
                    continue;

                AddCharacterAssets(catalog, definition.characterData);
                AddWeaponAssets(catalog, definition.equippedWeapon);
            }
        }

        if (startingLoot != null)
        {
            for (int i = 0; i < startingLoot.Count; i++)
            {
                StartingLootDefinition definition = startingLoot[i];
                if (definition != null)
                    catalog.Add(definition.lootItem);
            }
        }

        if (startingWeapons != null)
        {
            for (int i = 0; i < startingWeapons.Count; i++)
            {
                StartingWeaponDefinition definition = startingWeapons[i];
                if (definition != null)
                    AddWeaponAssets(catalog, definition.weaponData);
            }
        }

        if (startingBlueprints != null)
        {
            for (int i = 0; i < startingBlueprints.Count; i++)
            {
                StartingBlueprintDefinition definition = startingBlueprints[i];
                if (definition != null)
                    AddBlueprintAssets(catalog, definition.blueprintData);
            }
        }

        if (startingRelics != null)
        {
            for (int i = 0; i < startingRelics.Count; i++)
            {
                StartingRelicDefinition definition = startingRelics[i];
                if (definition != null)
                    catalog.Add(definition.relicData);
            }
        }

        if (recruitCharacterRoster != null && recruitCharacterRoster.Characters != null)
        {
            for (int i = 0; i < recruitCharacterRoster.Characters.Count; i++)
                AddCharacterAssets(catalog, recruitCharacterRoster.Characters[i]);
        }

        if (enemyRoster != null)
        {
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

        if (eventPool != null)
        {
            IReadOnlyList<MapEvent_SO> events = eventPool.GetValidEvents();
            for (int i = 0; i < events.Count; i++)
                AddEventAssets(catalog, events[i]);
        }

        if (merchantCatalog != null)
            AddMerchantCatalogAssets(catalog, merchantCatalog);

        if (calamityPool != null && calamityPool.CalamityTiers != null)
        {
            for (int i = 0; i < calamityPool.CalamityTiers.Count; i++)
            {
                CalamityPool_SO.CalamityTierEntry tierEntry = calamityPool.CalamityTiers[i];
                if (tierEntry == null || tierEntry.calamities == null)
                    continue;

                for (int calamityIndex = 0; calamityIndex < tierEntry.calamities.Count; calamityIndex++)
                    catalog.Add(tierEntry.calamities[calamityIndex]);
            }
        }

        if (runState != null)
        {
            if (runState.partyMembers != null)
            {
                for (int i = 0; i < runState.partyMembers.Count; i++)
                {
                    RunPartyMemberState member = runState.partyMembers[i];
                    if (member == null)
                        continue;

                    AddCharacterAssets(catalog, member.characterData);
                    AddWeaponAssets(catalog, member.weaponData);
                }
            }

            if (runState.lootInventory != null)
            {
                for (int i = 0; i < runState.lootInventory.Count; i++)
                {
                    RunLootState loot = runState.lootInventory[i];
                    if (loot != null)
                        catalog.Add(loot.lootItem);
                }
            }

            if (runState.weaponInventory != null)
            {
                for (int i = 0; i < runState.weaponInventory.Count; i++)
                {
                    RunWeaponState weapon = runState.weaponInventory[i];
                    if (weapon != null)
                        AddWeaponAssets(catalog, weapon.weaponData);
                }
            }

            if (runState.blueprintInventory != null)
            {
                for (int i = 0; i < runState.blueprintInventory.Count; i++)
                {
                    RunBlueprintState blueprint = runState.blueprintInventory[i];
                    if (blueprint != null)
                        AddBlueprintAssets(catalog, blueprint.blueprintData);
                }
            }

            if (runState.relicInventory != null)
            {
                for (int i = 0; i < runState.relicInventory.Count; i++)
                {
                    RunRelicState relic = runState.relicInventory[i];
                    if (relic != null)
                        catalog.Add(relic.relicData);
                }
            }

            if (runState.activeCalamities != null)
            {
                for (int i = 0; i < runState.activeCalamities.Count; i++)
                {
                    RunCalamityState calamity = runState.activeCalamities[i];
                    if (calamity != null)
                        catalog.Add(calamity.calamityData);
                }
            }
        }

        if (boardManager != null)
        {
            foreach (BoardTileData tile in boardManager.GetAllTiles())
            {
                if (tile == null)
                    continue;

                catalog.Add(tile.assignedEnemyGroup);
                catalog.Add(tile.assignedEvent);

                if (tile.merchantOffers == null)
                    continue;

                for (int i = 0; i < tile.merchantOffers.Count; i++)
                {
                    MerchantOfferState offer = tile.merchantOffers[i];
                    if (offer == null)
                        continue;

                    catalog.Add(offer.lootItem);
                    AddWeaponAssets(catalog, offer.weaponData);
                    AddBlueprintAssets(catalog, offer.blueprintData);
                    catalog.Add(offer.relicData);
                }
            }
        }

        return catalog;
    }

    private void AddDifficultyAssets(MainMenuSaveAssetCatalog catalog, DifficultyConfig_SO config)
    {
        if (catalog == null || config == null)
            return;

        if (!string.IsNullOrWhiteSpace(config.difficultyName) && config.difficultyName.Equals("Easy", StringComparison.OrdinalIgnoreCase))
        {
            // no-op, kept to ensure config is considered in ordering by the menu runtime
        }
    }

    private void AddCharacterAssets(MainMenuSaveAssetCatalog catalog, CharacterData_SO characterData)
    {
        if (catalog == null || characterData == null)
            return;

        catalog.Add(characterData);
        AddWeaponAssets(catalog, characterData.initialWeaponData);
    }

    private void AddWeaponAssets(MainMenuSaveAssetCatalog catalog, Weapon_SO weaponData)
    {
        if (catalog == null || weaponData == null)
            return;

        catalog.Add(weaponData);
    }

    private void AddBlueprintAssets(MainMenuSaveAssetCatalog catalog, CraftingBlueprint_SO blueprintData)
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

    private void AddEnemyGroupList(MainMenuSaveAssetCatalog catalog, IReadOnlyList<EnemyGroup_SO> groups)
    {
        if (groups == null)
            return;

        for (int i = 0; i < groups.Count; i++)
            AddEnemyGroupAssets(catalog, groups[i]);
    }

    private void AddEnemyGroupAssets(MainMenuSaveAssetCatalog catalog, EnemyGroup_SO enemyGroup)
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

    private void AddEnemyDataAssets(MainMenuSaveAssetCatalog catalog, EnemyData_SO enemyData)
    {
        if (enemyData == null)
            return;

        if (enemyData.onSpecificEnemyDeathStatusGains != null)
        {
            for (int i = 0; i < enemyData.onSpecificEnemyDeathStatusGains.Count; i++)
            {
                EnemyData_SO.OnSpecificEnemyDeathStatusGainEntry entry = enemyData.onSpecificEnemyDeathStatusGains[i];
                AddEnemyDataAssets(catalog, entry.watchedEnemyData);
            }
        }
    }

    private void AddLootTableAssets(MainMenuSaveAssetCatalog catalog, LootTable_SO lootTable)
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

    private void AddEventAssets(MainMenuSaveAssetCatalog catalog, MapEvent_SO eventData)
    {
        if (catalog == null || eventData == null)
            return;

        catalog.Add(eventData);
        IReadOnlyList<MapEventOptionData> options = eventData.GetValidOptions();
        for (int i = 0; i < options.Count; i++)
            AddEventOptionAssets(catalog, options[i]);
    }

    private void AddEventOptionAssets(MainMenuSaveAssetCatalog catalog, MapEventOptionData optionData)
    {
        if (catalog == null || optionData == null)
            return;

        AddEventEffectAssets(catalog, BuildPrimaryEventEffect(optionData));

        if (optionData.additionalBuiltInEffects == null)
            return;

        for (int i = 0; i < optionData.additionalBuiltInEffects.Count; i++)
            AddEventEffectAssets(catalog, optionData.additionalBuiltInEffects[i]);
    }

    private MapEventBuiltInEffectData BuildPrimaryEventEffect(MapEventOptionData optionData)
    {
        if (optionData == null)
            return null;

        return new MapEventBuiltInEffectData
        {
            effectType = optionData.builtInEffectType,
            effectAmount = optionData.effectAmount,
            randomMoneyMin = optionData.randomMoneyMin,
            randomMoneyMax = optionData.randomMoneyMax,
            randomOwnedLootAmountMin = optionData.randomOwnedLootAmountMin,
            randomOwnedLootAmountMax = optionData.randomOwnedLootAmountMax,
            lootRewards = optionData.lootRewards,
            lootPickCountMin = optionData.lootPickCountMin,
            lootPickCountMax = optionData.lootPickCountMax,
            relicRewards = optionData.relicRewards,
            relicPickCountMin = optionData.relicPickCountMin,
            relicPickCountMax = optionData.relicPickCountMax,
            blueprintRewards = optionData.blueprintRewards,
            blueprintPickCountMin = optionData.blueprintPickCountMin,
            blueprintPickCountMax = optionData.blueprintPickCountMax,
            encounterSelectionMode = optionData.encounterSelectionMode,
            encounterEnemyGroups = optionData.encounterEnemyGroups,
            encounterEnemies = optionData.encounterEnemies,
            encounterEnemyCountMin = optionData.encounterEnemyCountMin,
            encounterEnemyCountMax = optionData.encounterEnemyCountMax
        };
    }

    private void AddEventEffectAssets(MainMenuSaveAssetCatalog catalog, MapEventBuiltInEffectData effectData)
    {
        if (catalog == null || effectData == null)
            return;

        if (effectData.lootRewards != null)
        {
            for (int i = 0; i < effectData.lootRewards.Count; i++)
            {
                MapEventLootRewardEntry entry = effectData.lootRewards[i];
                if (entry != null)
                    catalog.Add(entry.lootItem);
            }
        }

        if (effectData.relicRewards != null)
        {
            for (int i = 0; i < effectData.relicRewards.Count; i++)
            {
                MapEventRelicRewardEntry entry = effectData.relicRewards[i];
                if (entry != null)
                    catalog.Add(entry.relicData);
            }
        }

        if (effectData.blueprintRewards != null)
        {
            for (int i = 0; i < effectData.blueprintRewards.Count; i++)
            {
                MapEventBlueprintRewardEntry entry = effectData.blueprintRewards[i];
                if (entry != null)
                    AddBlueprintAssets(catalog, entry.blueprintData);
            }
        }

        if (effectData.encounterEnemyGroups != null)
        {
            for (int i = 0; i < effectData.encounterEnemyGroups.Count; i++)
                AddEnemyGroupAssets(catalog, effectData.encounterEnemyGroups[i]);
        }

        if (effectData.encounterEnemies != null)
        {
            for (int i = 0; i < effectData.encounterEnemies.Count; i++)
                AddEnemyDataAssets(catalog, effectData.encounterEnemies[i]);
        }
    }

    private void AddMerchantCatalogAssets(MainMenuSaveAssetCatalog catalog, MerchantCatalog_SO merchantCatalogData)
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
        {
            MerchantCatalog_SO.RelicGoodsEntry entry = relicEntries[i];
            if (entry == null)
                continue;

            if (entry.relicTable != null && entry.relicTable.Relics != null)
            {
                IReadOnlyList<Relic_SO> relics = entry.relicTable.Relics;
                for (int relicIndex = 0; relicIndex < relics.Count; relicIndex++)
                    catalog.Add(relics[relicIndex]);
                continue;
            }

            catalog.Add(entry.LegacyRelicData);
        }
    }

    private static string GetTileTypeLabel(MapTileType tileType)
    {
        switch (tileType)
        {
            case MapTileType.Empty:
                return "空白";
            case MapTileType.Battle:
                return "战斗";
            case MapTileType.Elite:
                return "精英";
            case MapTileType.Event:
                return "事件";
            case MapTileType.Merchant:
                return "商人";
            case MapTileType.Forge:
                return "锻造";
            case MapTileType.Rest:
                return "休息";
            case MapTileType.Key:
                return "钥匙";
            case MapTileType.Boss:
                return "Boss";
            default:
                return "未知";
        }
    }

    private static string GetDangerBandLabel(MapDangerBand dangerBand)
    {
        switch (dangerBand)
        {
            case MapDangerBand.Outer:
                return "外围";
            case MapDangerBand.Mid:
                return "中层";
            case MapDangerBand.Inner:
                return "内层";
            case MapDangerBand.Core:
                return "核心";
            default:
                return "未知";
        }
    }
}


