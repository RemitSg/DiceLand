using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
public class RunGameManager : MonoBehaviour
{
    private const int TutorialBoardSize = 12;
    private const int TutorialEncounterSeedBase = 26000;
    private const string TutorialCatalogResourcePath = "TutorialMenuCatalog";
    private static readonly MapTileType[] TutorialTileSequence =
    {
        MapTileType.Empty,
        MapTileType.Empty,
        MapTileType.Battle,
        MapTileType.Battle,
        MapTileType.Rest,
        MapTileType.Battle,
        MapTileType.Merchant,
        MapTileType.Battle,
        MapTileType.Forge,
        MapTileType.Battle,
        MapTileType.Key,
        MapTileType.Boss
    };

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

    [System.Serializable]
    private class TutorialBattleStageContentOverride
    {
        public TutorialBattleStageId stageId = TutorialBattleStageId.None;
        public EnemyGroup_SO enemyGroup = null;
        public TutorialBattleDefinition_SO battleDefinition = null;
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
    [SerializeField] private List<StartingPartyMemberDefinition> tutorialStartingPartyMembers = new List<StartingPartyMemberDefinition>();
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

    [Header("Tutorial Battle Content")]
    [SerializeField] private EnemyGroup_SO tutorialOpeningEnemyGroup;
    [SerializeField] private EnemyGroup_SO tutorialEliteEnemyGroup;
    [SerializeField] private EnemyGroup_SO tutorialBossEnemyGroup;
    [SerializeField] private TutorialBattleDefinition_SO tutorialOpeningBattleDefinition;
    [SerializeField] private TutorialBattleDefinition_SO tutorialEliteBattleDefinition;
    [SerializeField] private TutorialBattleDefinition_SO tutorialBossBattleDefinition;
    [SerializeField] private List<TutorialBattleStageContentOverride> tutorialBattleStageOverrides = new List<TutorialBattleStageContentOverride>();
    [SerializeField] private string tutorialCompletionSceneName = "Menu Selection";
    [SerializeField] private TutorialEnemyVisualTemplateSet tutorialEnemyVisualTemplates = new TutorialEnemyVisualTemplateSet();

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
    private readonly Dictionary<string, MapEventLootDiscardPreview> eventRandomLootDiscardPreviewCache = new Dictionary<string, MapEventLootDiscardPreview>(StringComparer.Ordinal);
    private bool isTutorialRun;
    private MainMenuTutorialOverlay tutorialOverlay;

    private void Awake()
    {
        EnsureSetup();
        if (autoStartOnAwake)
        {
            if (MainMenuRuntime.ConsumeTutorialRequest())
            {
                StartTutorialRun();
                return;
            }

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
        if (tileResolver != null)
            tileResolver.KeyCollected -= HandleKeyCollected;
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
        isTutorialRun = false;
        MainMenuRuntime.ClearTutorialRequest();
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
            partyMembers = BuildStartingPartyMembers(startingPartyMembers)
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

    private void StartTutorialRun()
    {
        EnsureSetup();
        isTutorialRun = true;
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
            difficultyName = string.IsNullOrEmpty(config != null ? config.difficultyName : null) ? "Tutorial" : config.difficultyName,
            boardSize = TutorialBoardSize,
            requiredKeys = 1,
            currentCycle = 1,
            money = Mathf.Max(0, startingMoney),
            partyMembers = BuildTutorialStartingPartyMembers()
        };

        ApplyStartingInventory(runState);
        runState.SyncLegacyPlayerFieldsFromParty();

        actionPointSystem.Initialize(ResolveActionPointLimitForTier(config, 0));

        List<BoardTileData> tutorialTiles = BuildTutorialBoardTiles(TutorialBoardSize);
        Vector2Int spawnCoord = tutorialTiles[0].coord;
        Vector2Int bossCoord = tutorialTiles[tutorialTiles.Count - 1].coord;
        boardManager.LoadBoard(TutorialBoardSize, spawnCoord, bossCoord, spawnCoord, tutorialTiles);
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

        RefreshHUD("教程已开始：先点击相邻格子查看详情，再次点击即可沿固定路线推进。");
        mapViewController.Initialize(mapCamera, boardManager, cameraPadding);
        mapViewController.SetInputBlockedEvaluator(IsMapViewInputBlocked);
        mapViewController.PlaySpawnIntro(boardManager.GridToWorld(boardManager.SpawnCoord));
        ShowTutorialGuideOverlay();
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
        isTutorialRun = MainMenuRuntime.TutorialActive;

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

    private static List<BoardTileData> BuildTutorialBoardTiles(int boardSize)
    {
        int row = Mathf.Max(1, boardSize / 2);
        List<BoardTileData> tiles = new List<BoardTileData>(TutorialTileSequence.Length);
        for (int i = 0; i < TutorialTileSequence.Length; i++)
        {
            MapTileType tileType = TutorialTileSequence[i];
            TutorialBattleStageId stageId = ResolveTutorialStageId(i);
            tiles.Add(new BoardTileData
            {
                coord = new Vector2Int(i, row),
                tileType = tileType,
                dangerBand = ResolveTutorialDangerBand(i),
                isVisited = i == 0,
                isResolved = tileType == MapTileType.Empty,
                isSpawnTile = i == 0,
                encounterSeed = TutorialEncounterSeedBase + i,
                previewEnemyCount = ResolveTutorialPreviewEnemyCount(tileType, stageId),
                previewDangerRating = ResolveTutorialPreviewDanger(tileType, stageId),
                previewTag = ResolveTutorialPreviewTag(tileType, stageId)
            });
        }

        return tiles;
    }

    private static TutorialBattleStageId ResolveTutorialStageId(int sequenceIndex)
    {
        switch (sequenceIndex)
        {
            case 2:
                return TutorialBattleStageId.Battle01;
            case 3:
                return TutorialBattleStageId.Battle02;
            case 5:
                return TutorialBattleStageId.Battle03;
            case 7:
                return TutorialBattleStageId.Battle04;
            case 9:
                return TutorialBattleStageId.Battle05;
            case 11:
                return TutorialBattleStageId.Battle07;
            default:
                return TutorialBattleStageId.None;
        }
    }

    private static MapDangerBand ResolveTutorialDangerBand(int sequenceIndex)
    {
        if (sequenceIndex <= 2)
            return MapDangerBand.Outer;
        if (sequenceIndex <= 5)
            return MapDangerBand.Mid;
        if (sequenceIndex <= 7)
            return MapDangerBand.Inner;
        return MapDangerBand.Core;
    }

    private static int ResolveTutorialPreviewEnemyCount(MapTileType tileType, TutorialBattleStageId stageId)
    {
        return TutorialBattleStageUtility.IsBattleStage(stageId) || tileType == MapTileType.Boss ? 1 : 0;
    }

    private static int ResolveTutorialPreviewDanger(MapTileType tileType, TutorialBattleStageId stageId)
    {
        switch (stageId)
        {
            case TutorialBattleStageId.Battle01:
            case TutorialBattleStageId.Battle02:
                return 1;
            case TutorialBattleStageId.Battle03:
            case TutorialBattleStageId.Battle04:
                return 2;
            case TutorialBattleStageId.Battle05:
                return 4;
            case TutorialBattleStageId.Battle07:
                return 5;
            default:
                return 0;
        }
    }

    private static string ResolveTutorialPreviewTag(MapTileType tileType, TutorialBattleStageId stageId)
    {
        if (TutorialBattleStageUtility.IsBattleStage(stageId))
            return TutorialBattleStageUtility.IsFinalStage(stageId) ? "教程终战" : "教程战斗";

        switch (tileType)
        {
            case MapTileType.Rest:
                return "教程营地";
            case MapTileType.Merchant:
                return "教程商店";
            case MapTileType.Forge:
                return "教程锻造";
            case MapTileType.Key:
                return "教程钥匙";
            case MapTileType.Boss:
                return "教程首领";
            default:
                return "教程路线";
        }
    }

    private void ShowTutorialGuideOverlay()
    {
        TutorialMenuCatalog_SO catalog = Resources.Load<TutorialMenuCatalog_SO>(TutorialCatalogResourcePath);
        if (catalog == null)
            return;

        if (tutorialOverlay == null)
            tutorialOverlay = gameObject.GetComponent<MainMenuTutorialOverlay>() ?? gameObject.AddComponent<MainMenuTutorialOverlay>();

        tutorialOverlay.Show(
            catalog,
            () => tutorialOverlay.Hide(),
            null,
            "继续",
            "关闭");
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

    private void HandleKeyCollected(BoardTileData tile)
    {
        if (tile == null || runState == null || actionPointSystem == null)
            return;

        RelicExecutionContext context = RelicTriggerDispatcher.ProcessKeyCollected(
            runState,
            this,
            tile,
            actionPointSystem);
        if (context.feedbackMessages != null && context.feedbackMessages.Count > 0)
            RefreshHUD(string.Join("\n", context.feedbackMessages));
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
        MerchantPurchaseResult result = MerchantPurchaseService.TryPurchaseOffer(runState, tile, offer);
        if (!result.success)
            return result.message;

        RefreshHUD($"购买成功：{result.displayName} - {result.price} 金钱");
        return result.message;
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
            tileResolver.KeyCollected += HandleKeyCollected;
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

        RelicExecutionContext relicContext = RelicTriggerDispatcher.ProcessMoveCost(
            runState,
            this,
            tile,
            moveCost);

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

        TutorialBattleStageId tutorialBattleStageId = isTutorialRun
            ? ResolveTutorialStageId(tile.coord.x)
            : TutorialBattleStageId.None;
        EnemyGroup_SO encounterGroup = isTutorialRun
            ? ResolveTutorialEnemyGroup(tile, tutorialBattleStageId)
            : tile.assignedEnemyGroup;
        TutorialBattleDefinition_SO tutorialBattleDefinition = null;
        bool tutorialReturnsToMainMenu = false;
        if (!isTutorialRun && (encounterGroup == null || !encounterGroup.IsValid))
        {
            encounterGroup = SelectEnemyGroupForTile(tile);
            tile.assignedEnemyGroup = encounterGroup;
        }
        else if (isTutorialRun)
        {
            tile.assignedEnemyGroup = encounterGroup;
        }

        if (isTutorialRun)
        {
            tutorialBattleDefinition = ResolveTutorialBattleDefinition(tile.tileType, tutorialBattleStageId);
            tutorialReturnsToMainMenu = TutorialBattleStageUtility.IsFinalStage(tutorialBattleStageId);
        }

        if (encounterGroup == null || !encounterGroup.IsValid)
        {
            deferPendingCalamityUntilMapResume = false;
            RefreshHUD($"{GetTileTypeLabel(tile.tileType)} / {GetDangerBandLabel(tile.dangerBand)} 没有可用敌人组。");
            return false;
        }

        tile.isVisited = true;
        boardManager.RefreshTile(tile.coord);
        List<PendingBattleStatusGrant> pendingBattleStartGrants = runState.ConsumePendingNextBattleAllyStatusGrants();
        MapRunRuntime.SaveSnapshot(SceneManager.GetActiveScene().name, runState, boardManager, actionPointSystem, pendingCalamityTiers);

        BattleEncounterRuntime.SetEncounter(new BattleEncounterData
        {
            sourceSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
            targetSceneName = battleSceneName,
            tutorialCompletionSceneName = ResolveTutorialCompletionSceneName(),
            encounterLabel = $"{tile.tileType} [{tile.coord.x},{tile.coord.y}]",
            tutorialBattleStageId = tutorialBattleStageId,
            tileType = tile.tileType,
            dangerBand = tile.dangerBand,
            sourceCoord = tile.coord,
            enemyGroup = encounterGroup,
            tutorialBattleDefinition = tutorialBattleDefinition,
            partyMembers = new List<RunPartyMemberState>(runState.GetLivingPartyMembers()),
            relicInventory = runState.relicInventory != null ? new List<RunRelicState>(runState.relicInventory.Count) : new List<RunRelicState>(),
            activeCalamities = runState.activeCalamities != null ? new List<RunCalamityState>(runState.activeCalamities.Count) : new List<RunCalamityState>(),
            pendingBattleStartAllyStatusGrants = pendingBattleStartGrants,
            seed = tile.encounterSeed,
            moneyGained = moneyRewardTable != null ? moneyRewardTable.RollReward(tile.tileType, tile.dangerBand, tile.encounterSeed) : 0,
            isTutorialEncounter = isTutorialRun,
            returnToMainMenuOnVictory = tutorialReturnsToMainMenu,
            restartBattleOnDefeat = isTutorialRun
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

    private EnemyGroup_SO ResolveTutorialEnemyGroup(BoardTileData tile, TutorialBattleStageId stageId)
    {
        if (tile == null)
            return null;

        if (TryResolveTutorialStageOverride(stageId, out TutorialBattleStageContentOverride stageOverride)
            && stageOverride.enemyGroup != null
            && stageOverride.enemyGroup.IsValid)
        {
            return stageOverride.enemyGroup;
        }

        switch (tile.tileType)
        {
            case MapTileType.Battle:
                if (stageId == TutorialBattleStageId.Battle01
                    && tutorialOpeningEnemyGroup != null
                    && tutorialOpeningEnemyGroup.IsValid)
                {
                    return tutorialOpeningEnemyGroup;
                }

                return TutorialBattleContentFactory.CreateRuntimeEnemyGroup(stageId, tutorialEnemyVisualTemplates);
            case MapTileType.Boss:
                if (tutorialBossEnemyGroup != null && tutorialBossEnemyGroup.IsValid)
                    return tutorialBossEnemyGroup;

                return TutorialBattleContentFactory.CreateRuntimeEnemyGroup(stageId, tutorialEnemyVisualTemplates);
            default:
                return SelectEnemyGroupForTile(tile);
        }
    }

    private TutorialBattleDefinition_SO ResolveTutorialBattleDefinition(MapTileType tileType, TutorialBattleStageId stageId)
    {
        if (TryResolveTutorialStageOverride(stageId, out TutorialBattleStageContentOverride stageOverride)
            && stageOverride.battleDefinition != null)
        {
            return stageOverride.battleDefinition;
        }

        switch (tileType)
        {
            case MapTileType.Battle:
                if (stageId == TutorialBattleStageId.Battle01 && tutorialOpeningBattleDefinition != null)
                    return tutorialOpeningBattleDefinition;

                return TutorialBattleContentFactory.CreateRuntimeDefinition(stageId);
            case MapTileType.Boss:
                if (tutorialBossBattleDefinition != null)
                    return tutorialBossBattleDefinition;

                return TutorialBattleContentFactory.CreateRuntimeDefinition(stageId);
            default:
                return null;
        }
    }

    private bool TryResolveTutorialStageOverride(TutorialBattleStageId stageId, out TutorialBattleStageContentOverride stageOverride)
    {
        if (tutorialBattleStageOverrides != null)
        {
            for (int i = 0; i < tutorialBattleStageOverrides.Count; i++)
            {
                TutorialBattleStageContentOverride candidate = tutorialBattleStageOverrides[i];
                if (candidate == null || candidate.stageId != stageId)
                    continue;

                stageOverride = candidate;
                return true;
            }
        }

        stageOverride = null;
        return false;
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

        MapEventLootCostPreviewData lootCostPreview =
            MapEventLootCostService.BuildPreview(runState, optionData, eventRandomLootDiscardPreviewCache);
        preview.isSelectable = lootCostPreview.isSelectable;
        preview.additionalDetail = lootCostPreview.additionalDetail;
        preview.unavailableReason = lootCostPreview.unavailableReason;
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
        List<MapEventBuiltInEffectData> allEffects = MapEventEffectSequenceBuilder.BuildAllEffects(optionData);

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

            if (encounterEffectIndex >= 0 && MapEventEffectSequenceBuilder.IsDeferredEncounterVictoryRewardEffect(effectData.effectType))
            {
                if (!MapEventRewardService.TryQueueEncounterVictoryReward(pendingEventEncounterData, effectData, out string deferredMessage))
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
                return MapEventRewardService.TryExecuteLootReward(runState, effectData, out message);
            case MapEventOptionBuiltInEffectType.GainRelics:
                return MapEventRewardService.TryExecuteRelicReward(runState, effectData, out message);
            case MapEventOptionBuiltInEffectType.GainBlueprints:
                return MapEventRewardService.TryExecuteBlueprintReward(runState, effectData, out message);
            case MapEventOptionBuiltInEffectType.LoseOwnedRandomLoot:
                return MapEventLootCostService.TryExecuteRandomLootDiscard(
                    runState,
                    optionData,
                    effectIndex,
                    effectData,
                    eventRandomLootDiscardPreviewCache,
                    out message);
            case MapEventOptionBuiltInEffectType.LoseSpecificLoot:
                return MapEventLootCostService.TryExecuteSpecificLootDiscard(runState, effectData, out message);
            case MapEventOptionBuiltInEffectType.StartEncounter:
                return TryPrepareEventEncounter(effectData, out message);
            default:
                message = "未知事件效果。";
                return false;
        }
    }

    private bool TryPrepareEventEncounter(MapEventBuiltInEffectData effectData, out string message)
    {
        int seedSalt = Environment.TickCount;
        return MapEventEncounterService.TryPrepareEncounter(
            runState,
            currentEventExecutionTile,
            effectData,
            SceneManager.GetActiveScene().name,
            battleSceneName,
            seedSalt,
            out pendingEventEncounterData,
            out message);
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
        if (runState != null)
            encounterData.pendingBattleStartAllyStatusGrants = runState.ConsumePendingNextBattleAllyStatusGrants();

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
        if (boardManager != null && runState != null && boardManager.TryGetTile(runState.playerGridPosition, out BoardTileData currentTile) && currentTile.tileType == MapTileType.Merchant)
            RelicMerchantService.ResetVisitState(currentTile);

        PersistCurrentRunSave();
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
        RelicExecutionContext context = RelicTriggerDispatcher.ProcessCalamityApplied(runState, this, calamity, calamityTier);

        string calamityMessage = $"\u5df2\u627f\u53d7\u707e\u5384\uff1a{calamity.DisplayName}";
        if (context.feedbackMessages != null && context.feedbackMessages.Count > 0)
            calamityMessage = $"{calamityMessage}\n{string.Join("\n", context.feedbackMessages)}";

        RefreshHUD(calamityMessage);
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

        System.Random rng = CreateMerchantRandom(tile, tile.merchantRefreshCount);
        MerchantStockService.TryGenerateInitialStock(
            tile,
            merchantCatalog,
            runState,
            ResolveMerchantDifficultyConfig(),
            rng);
    }

    private int CountRefreshableMerchantOffers(BoardTileData tile)
    {
        return MerchantStockService.CountRefreshableOffers(tile);
    }

    private int RefreshMerchantOffers(BoardTileData tile, System.Random rng)
    {
        return MerchantStockService.RefreshOffers(tile, merchantCatalog, runState, ResolveMerchantDifficultyConfig(), rng);
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

    private string ResolveTutorialCompletionSceneName()
    {
        if (!string.IsNullOrWhiteSpace(MainMenuRuntime.TutorialReturnSceneName))
            return MainMenuRuntime.TutorialReturnSceneName;

        return tutorialCompletionSceneName;
    }

    private List<RunPartyMemberState> BuildTutorialStartingPartyMembers()
    {
        List<StartingPartyMemberDefinition> source = tutorialStartingPartyMembers;
        if (source == null || source.Count == 0)
            source = startingPartyMembers;

        return BuildStartingPartyMembers(source);
    }

    private List<RunPartyMemberState> BuildStartingPartyMembers(List<StartingPartyMemberDefinition> sourceDefinitions)
    {
        List<RunPartyMemberState> result = new List<RunPartyMemberState>();
        if (sourceDefinitions != null)
        {
            for (int i = 0; i < sourceDefinitions.Count; i++)
            {
                StartingPartyMemberDefinition definition = sourceDefinitions[i];
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

        if (isTutorialRun || MainMenuRuntime.TutorialActive)
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


