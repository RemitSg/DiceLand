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
    private const int TutorialFirstMoveSequenceIndex = 1;
    private const int TutorialFirstBattleSequenceIndex = 2;
    private static readonly Vector2 TutorialTopLeftStatusHighlightAnchor = new Vector2(14f, -14f);
    private static readonly Vector2 TutorialTopLeftStatusHighlightSize = new Vector2(438f, 236f);
    private const string DefaultTreasureRewardTableResourcePath = "DefaultTreasureRewardTable";
    private const string TreasureBlueprintTableResourcePath = "TreasureBlueprintTable";
    private const string TreasureRelicTableResourcePath = "TreasureRelicTable";
    private const string TreasureRelicBagRarityWeightTableResourcePath = "TreasureRelicBagRarityWeightTable";
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

    private enum MapTutorialGuideStep
    {
        None,
        WaitingForViewNavigation,
        WaitingForBattleInspect,
        WaitingForFirstMoveSelect,
        WaitingForFirstMoveConfirm,
        WaitingForFirstMoveResolve,
        WaitingForBattleEntry
    }

    [System.Serializable]
    public class StartingPartyMemberDefinition
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
    public class StartingLootDefinition
    {
        public LootItem_SO lootItem;
        [Min(1)] public int amount = 1;
    }

    [System.Serializable]
    public class StartingWeaponDefinition
    {
        public Weapon_SO weaponData;
        [Min(1)] public int amount = 1;
    }

    [System.Serializable]
    public class StartingBlueprintDefinition
    {
        public CraftingBlueprint_SO blueprintData;
        [Min(1)] public int amount = 1;
    }

    [System.Serializable]
    public class StartingRelicDefinition
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
    [SerializeField] private TreasureRewardTable_SO treasureRewardTable;
    [SerializeField] private RelicTable_SO eliteRelicChoiceTable;
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
    [SerializeField] private string mainMenuSceneName = BattleExitFlowService.DefaultMainMenuSceneName;

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

    [Header("Treasure UI")]
    [SerializeField] private Sprite treasurePanelBackground;
    [SerializeField] private Sprite treasureOptionBackground;
    [SerializeField] private Sprite treasureDisabledOptionBackground;
    [SerializeField] private Sprite treasureMoneyIcon;
    [SerializeField] private Sprite treasureRelicBagIcon;
    [SerializeField] private Sprite treasureRelicBagCommonIcon;
    [SerializeField] private Sprite treasureRelicBagUncommonIcon;
    [SerializeField] private Sprite treasureRelicBagRareIcon;
    [SerializeField] private Sprite treasureRelicBagEpicIcon;
    [SerializeField] private Sprite treasureRelicBagLegendaryIcon;

    [Header("Managers")]
    [SerializeField] private BoardManager boardManager;
    [SerializeField] private PlayerMapController playerMapController;
    [SerializeField] private ActionPointSystem actionPointSystem;
    [SerializeField] private TileResolver tileResolver;
    [SerializeField] private MapHUD mapHUD;
    [SerializeField] private MapCraftingOverlay mapCraftingOverlay;
    [SerializeField] private MapMerchantOverlay mapMerchantOverlay;
    [SerializeField] private MapTreasureOverlay mapTreasureOverlay;
    [SerializeField] private MapEventOverlay mapEventOverlay;
    [SerializeField] private MapCalamityOverlay mapCalamityOverlay;
    [SerializeField] private MapPauseMenuOverlay mapPauseMenuOverlay;
    [SerializeField] private MapTutorialGuideOverlay mapTutorialGuideOverlay;
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

    [Header("Tutorial Map Guide")]
    [SerializeField] [Min(0.05f)] private float tutorialViewNavigationSettleSeconds = 0.8f;

    [Header("Camera")]
    [SerializeField] private Color cameraBackgroundColor = new Color(0.08f, 0.11f, 0.16f, 1f);
    [SerializeField] private float cameraPadding = 2.8f;

    private RunState runState;
    private bool subscriptionsBound;
    private DifficultyConfig_SO activeDifficultyConfig;
    private readonly RunSceneFlowState runFlowState = new RunSceneFlowState();
    private string pendingMoveRelicStatusMessage;
    private bool movingToPreviouslyExploredEmptyTile;
    private readonly Dictionary<string, MapEventLootDiscardPreview> eventRandomLootDiscardPreviewCache = new Dictionary<string, MapEventLootDiscardPreview>(StringComparer.Ordinal);
    private bool isTutorialRun;
    private MapTutorialGuideStep mapTutorialGuideStep = MapTutorialGuideStep.None;
    private bool tutorialMapIntroSequenceActive;
    private bool tutorialBattleInspectSelectionAllowed;
    private bool tutorialViewNavigationObserved;
    private float tutorialLastViewNavigationTime;
    private bool tutorialViewPointerTracking;
    private Vector2 tutorialViewPointerLastScreenPosition;
    [NonSerialized] private SpecialDungeonRuntimeContext activeSpecialDungeonContext;

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
        if (mapViewController != null)
            mapViewController.ViewNavigationPerformed -= HandleMapViewNavigationPerformed;
        if (mapEventOverlay != null)
            mapEventOverlay.Hidden -= HandleMapEventOverlayHidden;
        if (mapMerchantOverlay != null)
            mapMerchantOverlay.Hidden -= HandleMapMerchantOverlayHidden;
        if (mapCraftingOverlay != null)
            mapCraftingOverlay.Hidden -= HandleMapCraftingOverlayHidden;
        if (mapTreasureOverlay != null)
            mapTreasureOverlay.Hidden -= HandleMapTreasureOverlayHidden;
    }

    private void Update()
    {
        UpdateMapTutorialGuide();
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
        tutorialMapIntroSequenceActive = false;
        tutorialBattleInspectSelectionAllowed = false;
        MainMenuRuntime.ClearTutorialRequest();
        MapRunRuntime.Clear(SceneManager.GetActiveScene().name);
        RestSceneRuntime.Clear();
        runFlowState.Reset();
        activeSpecialDungeonContext = null;

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
        tutorialMapIntroSequenceActive = false;
        tutorialBattleInspectSelectionAllowed = false;
        MapRunRuntime.Clear(SceneManager.GetActiveScene().name);
        RestSceneRuntime.Clear();
        runFlowState.Reset();
        activeSpecialDungeonContext = null;

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
        StartMapTutorialGuide();
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
        runFlowState.Reset();
        activeSpecialDungeonContext = snapshot.specialDungeonContext != null && snapshot.specialDungeonContext.isActive
            ? snapshot.specialDungeonContext.Clone()
            : null;

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

        runFlowState.RestorePendingCalamityTiers(snapshot.pendingCalamityTiers);

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
        return RunBootstrapService.BuildTutorialBoardTiles(boardSize);
    }

    private static TutorialBattleStageId ResolveTutorialStageId(int sequenceIndex)
    {
        return RunBootstrapService.ResolveTutorialStageId(sequenceIndex);
    }

    private static MapDangerBand ResolveTutorialDangerBand(int sequenceIndex)
    {
        return RunBootstrapService.ResolveTutorialDangerBand(sequenceIndex);
    }

    private static int ResolveTutorialPreviewEnemyCount(MapTileType tileType, TutorialBattleStageId stageId)
    {
        return RunBootstrapService.ResolveTutorialPreviewEnemyCount(tileType, stageId);
    }

    private static string ResolveTutorialPreviewTag(MapTileType tileType, TutorialBattleStageId stageId)
    {
        return RunBootstrapService.ResolveTutorialPreviewTag(tileType, stageId);
    }

    private void HandleTileClicked(BoardTileData tile)
    {
        if (tile == null || runState == null)
            return;

        if (!CanHandleTutorialTileClick(tile))
            return;

        EnsureTileAssignedEnemyGroup(tile);
        bool isRepeatedSelection = boardManager.SelectedCoord == tile.coord;
        boardManager.SetSelectedTile(tile.coord);
        mapHUD.ShowTile(tile, GetMoveCost(tile), true);

        if (!isRepeatedSelection)
        {
            mapHUD.ShowStatus("正在查看格子详情。再次点击该格子可尝试进入。");
            if (isTutorialRun && tutorialBattleInspectSelectionAllowed && IsTutorialFirstBattleTile(tile))
            {
                tutorialBattleInspectSelectionAllowed = false;
                ContinueAfterBattleTileInspected();
            }
            else if (isTutorialRun && IsTutorialFirstMoveTile(tile) && mapTutorialGuideStep == MapTutorialGuideStep.WaitingForFirstMoveSelect)
                ContinueAfterFirstMoveSelected();
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
                    runFlowState.DeferPendingCalamityUntilOverlayClosed = false;
                    TryPresentPendingCalamity();
                }
                return;
            }

            if (tile.tileType == MapTileType.Treasure && !tile.isResolved)
            {
                if (!OpenTreasurePanel(tile))
                {
                    runFlowState.DeferPendingCalamityUntilOverlayClosed = false;
                    TryPresentPendingCalamity();
                }
                return;
            }

            if (tile.tileType == MapTileType.SpecialDungeonExit)
            {
                ExitSpecialDungeon();
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
                    runFlowState.DeferPendingCalamityUntilOverlayClosed = false;
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

        bool canTriggerVisitedEmptyAmbush = movingToPreviouslyExploredEmptyTile;
        movingToPreviouslyExploredEmptyTile = false;
        if (canTriggerVisitedEmptyAmbush && TryTriggerVisitedEmptyTileAmbush(tile, message))
            return;

        EnsureTileAssignedEnemyGroup(tile);
        if (tile.IsHostileTile)
        {
            boardManager.SetSelectedTile(tile.coord);
            boardManager.RefreshAllViews();
            mapHUD.ShowTile(tile, GetMoveCost(tile));
            ShowPendingMoveRelicStatusIfAny();
            if (isTutorialRun && mapTutorialGuideStep == MapTutorialGuideStep.WaitingForBattleEntry && IsTutorialFirstBattleTile(tile))
                mapTutorialGuideStep = MapTutorialGuideStep.None;
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
                runFlowState.DeferPendingCalamityUntilOverlayClosed = false;
                TryPresentPendingCalamity();
            }
            return;
        }

        if (tile.tileType == MapTileType.Treasure && !tile.isResolved)
        {
            boardManager.SetSelectedTile(tile.coord);
            boardManager.RefreshAllViews();
            mapHUD.ShowTile(tile, GetMoveCost(tile));
            ShowPendingMoveRelicStatusIfAny();
            if (!OpenTreasurePanel(tile))
            {
                runFlowState.DeferPendingCalamityUntilOverlayClosed = false;
                TryPresentPendingCalamity();
            }
            return;
        }

        if (tile.tileType == MapTileType.SpecialDungeonExit)
        {
            boardManager.SetSelectedTile(tile.coord);
            boardManager.RefreshAllViews();
            mapHUD.ShowTile(tile, GetMoveCost(tile));
            ShowPendingMoveRelicStatusIfAny();
            ExitSpecialDungeon();
            return;
        }

        boardManager.SetSelectedTile(tile.coord);
        boardManager.RefreshAllViews();
        mapHUD.ShowTile(tile, GetMoveCost(tile));
        RefreshHUD(message);
        ShowPendingMoveRelicStatusIfAny();
        if (isTutorialRun && mapTutorialGuideStep == MapTutorialGuideStep.WaitingForFirstMoveResolve && IsTutorialFirstMoveTile(tile))
        {
            mapTutorialGuideStep = MapTutorialGuideStep.None;
            ContinueAfterFirstMoveCompleted();
        }
        if (tile.tileType == MapTileType.Forge)
            OpenForgePanel(tile);
        else if (tile.tileType == MapTileType.Merchant)
        {
            if (!OpenMerchantPanel(tile))
            {
                runFlowState.DeferPendingCalamityUntilOverlayClosed = false;
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

    private bool TryTriggerVisitedEmptyTileAmbush(BoardTileData tile, string fallbackMessage)
    {
        if (runState == null
            || boardManager == null
            || enemyRoster == null
            || tile == null
            || tile.tileType != MapTileType.Empty
            || !tile.isVisited
            || !tile.isResolved)
        {
            return false;
        }

        int ambushChance = CalamitySystem.ResolveVisitedEmptyTileAmbushChance(runState.activeCalamities);
        if (ambushChance <= 0 || UnityEngine.Random.Range(0, 100) >= ambushChance)
            return false;

        tile.tileType = MapTileType.Battle;
        tile.isResolved = false;
        tile.assignedEnemyGroup = null;
        tile.assignedEvent = null;
        tile.previewTag = string.Empty;
        tile.encounterSeed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
        EnsureTileAssignedEnemyGroup(tile);
        tile.previewDangerRating = MapDangerRatingService.ResolveRating(tile.tileType, tile.dangerBand, tile.assignedEnemyGroup);

        boardManager.SetSelectedTile(tile.coord);
        boardManager.RefreshAllViews();
        mapHUD.ShowTile(tile, GetMoveCost(tile));
        ShowPendingMoveRelicStatusIfAny();

        string message = string.IsNullOrWhiteSpace(fallbackMessage)
            ? "灾厄效果：伏击触发，空地中出现了敌人。"
            : $"{fallbackMessage}\n灾厄效果：伏击触发，空地中出现了敌人。";
        RefreshHUD(message);
        if (!TryEnterBattle(tile))
            TryPresentPendingCalamity();
        return true;
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

        int forgeCost = CalamitySystem.ApplyForgeCostModifiers(0, runState.activeCalamities);
        if (forgeCost > 0 && runState.money < forgeCost)
            return $"金币不足：需要 {forgeCost} 金币";

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

        if (forgeCost > 0)
            runState.TrySpendMoney(forgeCost);

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

        if (tile.sourceSpecialDungeon != null)
            return "特殊副本商店商品固定，无法刷新。";

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
        runFlowState.PendingCalamityTiers.Enqueue(Mathf.Max(1, runState.calamityTier));
        RefreshHUD($"\u707e\u5384\u6c14\u606f\u6fc0\u5316\uff0c\u5f53\u524d\u707e\u5384\u7b49\u7ea7 {runState.calamityTier}\u3002");
        if (runFlowState.DeferPendingCalamityUntilMapResume || runFlowState.DeferPendingCalamityUntilOverlayClosed)
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
        mapTreasureOverlay = GetOrCreateTreasureOverlay();
        if (mapKeywordFont != null || mapBodyFont != null)
            mapTreasureOverlay.ConfigureFonts(mapKeywordFont, mapBodyFont);
        mapTreasureOverlay.ConfigureBackgroundSprites(treasurePanelBackground, treasureOptionBackground, treasureDisabledOptionBackground);
        mapTreasureOverlay.ConfigureRewardIcons(treasureMoneyIcon, treasureRelicBagIcon);
        mapTreasureOverlay.ConfigureRelicBagIcons(
            treasureRelicBagCommonIcon,
            treasureRelicBagUncommonIcon,
            treasureRelicBagRareIcon,
            treasureRelicBagEpicIcon,
            treasureRelicBagLegendaryIcon);
        mapTreasureOverlay.Hidden -= HandleMapTreasureOverlayHidden;
        mapTreasureOverlay.Hidden += HandleMapTreasureOverlayHidden;
        mapCalamityOverlay = GetOrCreateCalamityOverlay();
        if (mapKeywordFont != null || mapBodyFont != null)
            mapCalamityOverlay.ConfigureFonts(mapKeywordFont, mapBodyFont);
        mapPauseMenuOverlay = GetOrCreatePauseMenuOverlay();
        mapPauseMenuOverlay.Configure(CanOpenMapPauseMenu, HandlePauseMenuSaveAndReturn, HandlePauseMenuAbandon);
        mapTutorialGuideOverlay = GetOrCreateTutorialGuideOverlay();
        mapViewController = GetOrCreateManager(mapViewController);
        mapBackgroundController = GetOrCreateManager(mapBackgroundController);
        if (sceneTransitionController == null)
            sceneTransitionController = FindObjectOfType<SceneCurtainTransitionController>(true);
        if (sceneTransitionController == null)
            sceneTransitionController = SceneCurtainTransitionController.Instance;

        boardManager.Initialize(boardRoot);
        tileResolver.ConfigureMoneyRewardTable(moneyRewardTable);
        if (treasureRewardTable == null)
            treasureRewardTable = Resources.Load<TreasureRewardTable_SO>(DefaultTreasureRewardTableResourcePath);
        ConfigureTreasureRewardFallbacks();
        mapHUD.Initialize();

        if (!subscriptionsBound)
        {
            boardManager.TileClicked += HandleTileClicked;
            playerMapController.PlayerMovedToTile += HandlePlayerMovedToTile;
            actionPointSystem.ActionPointsExhausted += HandleActionPointsExhausted;
            tileResolver.TileResolved += HandleTileResolved;
            tileResolver.KeyCollected += HandleKeyCollected;
            mapViewController.TileLongPressed += HandleTileLongPressed;
            mapViewController.ViewNavigationPerformed += HandleMapViewNavigationPerformed;
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

    private MapTreasureOverlay GetOrCreateTreasureOverlay()
    {
        if (mapTreasureOverlay != null)
            return mapTreasureOverlay;

        MapTreasureOverlay found = uiRoot != null ? uiRoot.GetComponent<MapTreasureOverlay>() : null;
        if (found == null && uiRoot != null)
            found = uiRoot.GetComponentInChildren<MapTreasureOverlay>(true);
        if (found == null)
            found = FindObjectOfType<MapTreasureOverlay>(true);
        if (found != null)
        {
            mapTreasureOverlay = found;
            return found;
        }

        GameObject go = new GameObject("MapTreasureOverlay");
        go.transform.SetParent(uiRoot, false);
        mapTreasureOverlay = go.AddComponent<MapTreasureOverlay>();
        return mapTreasureOverlay;
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

    private MapPauseMenuOverlay GetOrCreatePauseMenuOverlay()
    {
        if (mapPauseMenuOverlay != null)
            return mapPauseMenuOverlay;

        MapPauseMenuOverlay found = uiRoot != null ? uiRoot.GetComponent<MapPauseMenuOverlay>() : null;
        if (found == null && uiRoot != null)
            found = uiRoot.GetComponentInChildren<MapPauseMenuOverlay>(true);
        if (found == null)
            found = FindObjectOfType<MapPauseMenuOverlay>(true);
        if (found != null)
        {
            mapPauseMenuOverlay = found;
            return found;
        }

        GameObject go = new GameObject("MapPauseMenuOverlay");
        go.transform.SetParent(uiRoot, false);
        mapPauseMenuOverlay = go.AddComponent<MapPauseMenuOverlay>();
        return mapPauseMenuOverlay;
    }

    private MapTutorialGuideOverlay GetOrCreateTutorialGuideOverlay()
    {
        if (mapTutorialGuideOverlay != null)
        {
            PrepareTutorialGuideOverlay(mapTutorialGuideOverlay);
            return mapTutorialGuideOverlay;
        }

        MapTutorialGuideOverlay found = uiRoot != null ? uiRoot.GetComponent<MapTutorialGuideOverlay>() : null;
        if (found == null && uiRoot != null)
            found = uiRoot.GetComponentInChildren<MapTutorialGuideOverlay>(true);
        if (found == null)
            found = FindObjectOfType<MapTutorialGuideOverlay>(true);
        if (found != null)
        {
            PrepareTutorialGuideOverlay(found);
            mapTutorialGuideOverlay = found;
            return found;
        }

        GameObject go = new GameObject("MapTutorialGuideOverlay");
        go.transform.SetParent(uiRoot, false);
        mapTutorialGuideOverlay = go.AddComponent<MapTutorialGuideOverlay>();
        return mapTutorialGuideOverlay;
    }

    private void PrepareTutorialGuideOverlay(MapTutorialGuideOverlay overlay)
    {
        if (overlay == null)
            return;

        if (uiRoot != null && overlay.transform.parent != uiRoot)
            overlay.transform.SetParent(uiRoot, false);

        Transform current = overlay.transform.parent;
        while (current != null)
        {
            if (!current.gameObject.activeSelf)
                current.gameObject.SetActive(true);
            current = current.parent;
        }

        if (!overlay.gameObject.activeSelf)
            overlay.gameObject.SetActive(true);
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
            new DifficultyConfig_SO.TileWeightEntry { tileType = MapTileType.Treasure, weight = 1 },
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
        return RunBootstrapService.ResolveActionPointLimitForTier(config, calamityTier);
    }

    private int GetMoveCost(BoardTileData tile)
    {
        if (IsInSpecialDungeon())
            return 0;

        return MapMovementService.GetMoveCost(runState, tile);
    }

    private bool CanAttemptMove(BoardTileData tile, int moveCost)
    {
        return MapMovementService.CanAttemptMove(runState, actionPointSystem, tile, moveCost);
    }

    private bool CanPotentiallyIgnoreMoveCostViaRelic(BoardTileData tile)
    {
        return MapMovementService.CanPotentiallyIgnoreMoveCostViaRelic(runState, tile);
    }

    private bool TryExecuteMove(BoardTileData tile, int moveCost)
    {
        MapMovementCycleFlags previousFlags = CaptureMovementCycleFlags();
        bool wasPreviouslyExploredEmptyTile = tile != null
            && tile.tileType == MapTileType.Empty
            && tile.isVisited
            && tile.isResolved;
        movingToPreviouslyExploredEmptyTile = wasPreviouslyExploredEmptyTile;
        bool moved = MapMovementService.TryExecuteMove(
            new MapMovementExecutionRequest
            {
                runState = runState,
                runGameManager = this,
                tile = tile,
                moveCost = moveCost,
                actionPointSystem = actionPointSystem,
                playerMapController = playerMapController,
                previousCycleFlags = previousFlags,
                applyCycleFlags = ApplyMovementCycleFlags
            },
            out MapMovementExecutionResult result);

        if (moved && !string.IsNullOrWhiteSpace(result.pendingRelicStatusMessage))
            pendingMoveRelicStatusMessage = result.pendingRelicStatusMessage;
        if (moved && result.shouldRefreshHudWithEmptyStatus)
            RefreshHUD(string.Empty);

        if (!moved)
            movingToPreviouslyExploredEmptyTile = false;

        return moved;
    }

    private MapMovementCycleFlags CaptureMovementCycleFlags()
    {
        return runFlowState.ToMovementCycleFlags();
    }

    private void ApplyMovementCycleFlags(MapMovementCycleFlags flags)
    {
        runFlowState.ApplyMovementCycleFlags(flags);
    }

    private bool TryEnterBattle(BoardTileData tile)
    {
        if (tile == null)
            return false;
        if (sceneTransitionController != null && sceneTransitionController.IsTransitioning)
        {
            runFlowState.DeferPendingCalamityUntilMapResume = false;
            return false;
        }

        if (tile.tileType == MapTileType.Boss && !runState.IsBossUnlocked)
        {
            runFlowState.DeferPendingCalamityUntilMapResume = false;
            string lockedMessage = $"Boss 未解锁。当前钥匙 {runState.currentKeys}/{runState.requiredKeys}。";
            RefreshHUD(lockedMessage);
            boardManager.RefreshTile(tile.coord);
            return false;
        }

        if (enemyRoster == null)
        {
            runFlowState.DeferPendingCalamityUntilMapResume = false;
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
            runFlowState.DeferPendingCalamityUntilMapResume = false;
            RefreshHUD(MapTilePresentationService.BuildUnavailableEnemyGroupMessage(tile.tileType, tile.dangerBand));
            return false;
        }

        tile.previewDangerRating = MapDangerRatingService.ResolveRating(
            tile.tileType,
            tile.dangerBand,
            encounterGroup,
            tile.sourceSpecialDungeon != null ? tile.previewDangerRating : 0);

        tile.isVisited = true;
        boardManager.RefreshTile(tile.coord);
        List<PendingBattleStatusGrant> pendingBattleStartGrants = runState.ConsumePendingNextBattleAllyStatusGrants();
        MapRunRuntime.SaveSnapshot(
            SceneManager.GetActiveScene().name,
            runState,
            boardManager,
            actionPointSystem,
            runFlowState.PendingCalamityTiers,
            false,
            activeSpecialDungeonContext);

        BattleEncounterData encounterData = MapBattleEncounterService.BuildEncounter(
            new MapBattleEncounterBuildRequest
            {
                runState = runState,
                tile = tile,
                encounterGroup = encounterGroup,
                moneyRewardTable = moneyRewardTable,
                eliteRelicChoiceTable = eliteRelicChoiceTable,
                sourceSceneName = SceneManager.GetActiveScene().name,
                battleSceneName = battleSceneName,
                tutorialCompletionSceneName = ResolveTutorialCompletionSceneName(),
                tutorialBattleStageId = tutorialBattleStageId,
                tutorialBattleDefinition = tutorialBattleDefinition,
                pendingBattleStartAllyStatusGrants = pendingBattleStartGrants,
                isTutorialEncounter = isTutorialRun,
                returnToMainMenuOnVictory = tutorialReturnsToMainMenu,
                restartBattleOnDefeat = isTutorialRun
            });
        BattleEncounterRuntime.SetEncounter(encounterData);

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
            runFlowState.DeferPendingCalamityUntilMapResume = false;
            runFlowState.SuppressPendingCalamityOnNextRestActionPointRestore = false;
            RefreshHUD("这个休息格已经使用过了。");
            return false;
        }
        if (sceneTransitionController != null && sceneTransitionController.IsTransitioning)
        {
            runFlowState.DeferPendingCalamityUntilMapResume = false;
            runFlowState.SuppressPendingCalamityOnNextRestActionPointRestore = false;
            return false;
        }
        if (string.IsNullOrWhiteSpace(restSceneName))
        {
            runFlowState.DeferPendingCalamityUntilMapResume = false;
            runFlowState.SuppressPendingCalamityOnNextRestActionPointRestore = false;
            RefreshHUD("尚未配置休息场景。");
            return false;
        }
        if (runState == null)
        {
            runFlowState.DeferPendingCalamityUntilMapResume = false;
            runFlowState.SuppressPendingCalamityOnNextRestActionPointRestore = false;
            return false;
        }

        tile.isVisited = true;
        boardManager.SetSelectedTile(tile.coord);
        boardManager.RefreshTile(tile.coord);
        MapRunRuntime.SaveSnapshot(
            SceneManager.GetActiveScene().name,
            runState,
            boardManager,
            actionPointSystem,
            runFlowState.PendingCalamityTiers,
            runFlowState.SuppressPendingCalamityOnNextRestActionPointRestore,
            activeSpecialDungeonContext);
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
            runFlowState.DeferPendingCalamityUntilOverlayClosed = false;
            RefreshHUD("当前未配置可触发事件。");
            return false;
        }

        eventRandomLootDiscardPreviewCache.Clear();
        mapEventOverlay.Show(tile, tile.assignedEvent, HandleEventOptionSelected, BuildEventOptionPreview);
        RefreshHUD($"触发事件：{tile.assignedEvent.DisplayName}");
        return true;
    }

    private bool OpenTreasurePanel(BoardTileData tile)
    {
        if (tile == null || tile.tileType != MapTileType.Treasure || runState == null || mapTreasureOverlay == null)
            return false;
        if (tile.isResolved)
            return false;

        tile.isVisited = true;
        boardManager?.RefreshTile(tile.coord);
        mapHUD?.ShowTile(tile, GetMoveCost(tile), tile.coord == runState.playerGridPosition);

        IReadOnlyList<TreasureRewardChoice> choices = IsSpecialDungeonTreasure(tile)
            ? SpecialDungeonTreasureService.BuildChoices(tile)
            : TreasureRewardService.BuildChoices(treasureRewardTable, tile.encounterSeed);
        mapTreasureOverlay.Show(tile, choices, HandleTreasureOptionSelected, HandleTreasureRelicChoiceConfirmed);
        RefreshHUD("\u53d1\u73b0\u5b9d\u85cf\uff0c\u8bf7\u9009\u62e9\u4e00\u4efd\u5956\u52b1\u3002");
        return true;
    }

    private bool HandleTreasureOptionSelected(BoardTileData tile, TreasureRewardChoice choice, out TreasureRewardResult result)
    {
        result = null;
        if (tile == null || choice == null || runState == null)
        {
            result = new TreasureRewardResult
            {
                success = false,
                message = "\u5b9d\u85cf\u5956\u52b1\u9886\u53d6\u5931\u8d25\u3002"
            };
            return false;
        }

        result = IsSpecialDungeonTreasure(tile)
            ? SpecialDungeonTreasureService.ApplyChoice(
                runState,
                SpecialDungeonTreasureService.ResolveOption(tile, choice),
                tile.encounterSeed)
            : TreasureRewardService.ApplyChoice(runState, treasureRewardTable, choice, tile.encounterSeed);
        if (result == null || !result.success)
            return false;
        if (result.relicChoiceData != null && result.relicChoiceData.HasChoices)
        {
            RefreshHUD(result.message);
            return true;
        }

        tile.isVisited = true;
        tile.isResolved = true;
        boardManager?.RefreshTile(tile.coord);
        mapHUD?.ShowTile(tile, GetMoveCost(tile), true);
        RefreshHUD(result.message);
        return true;
    }

    private bool HandleTreasureRelicChoiceConfirmed(BoardTileData tile, BattleSettlementRelicChoiceData relicChoiceData, out string message)
    {
        if (tile == null || relicChoiceData == null || runState == null)
        {
            message = "\u9057\u7269\u9886\u53d6\u5931\u8d25\u3002";
            return false;
        }

        TreasureRewardResult result = TreasureRewardService.ApplyRelicChoice(runState, relicChoiceData);
        message = result != null ? result.message : "\u9057\u7269\u9886\u53d6\u5931\u8d25\u3002";
        if (result == null || !result.success)
            return false;

        tile.isVisited = true;
        tile.isResolved = true;
        boardManager?.RefreshTile(tile.coord);
        mapHUD?.ShowTile(tile, GetMoveCost(tile), true);
        RefreshHUD(message);
        return true;
    }

    private MapEventOverlay.EventOptionPreviewData BuildEventOptionPreview(BoardTileData tile, MapEvent_SO eventData, MapEventOptionData optionData)
    {
        MapEventOverlay.EventOptionPreviewData preview = new MapEventOverlay.EventOptionPreviewData();
        if (optionData == null || runState == null)
            return preview;

        int requiredMoney = CalculateRequiredEventMoney(optionData, runState.money);
        MapEventLootCostPreviewData lootCostPreview =
            MapEventLootCostService.BuildPreview(runState, optionData, eventRandomLootDiscardPreviewCache);
        preview.isSelectable = lootCostPreview.isSelectable && runState.money >= requiredMoney;
        preview.additionalDetail = lootCostPreview.additionalDetail;
        preview.unavailableReason = lootCostPreview.unavailableReason;
        if (runState.money < requiredMoney)
        {
            preview.unavailableReason = $"金钱不足，需要 {requiredMoney} 金钱。";
        }
        return preview;
    }

    private bool HandleEventOptionSelected(BoardTileData tile, MapEvent_SO eventData, MapEventOptionData optionData, out string message, out string resultContent, out MapEvent_SO nextEventOverride)
    {
        resultContent = string.Empty;
        nextEventOverride = null;
        if (tile == null || eventData == null || optionData == null || runState == null)
        {
            message = "事件执行失败。";
            return false;
        }

        int moneyBeforeSelection = Mathf.Max(0, runState.money);
        int requiredMoney = CalculateRequiredEventMoney(optionData, moneyBeforeSelection);
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

        if (!MapEventBuiltInEffectExecutorService.TryExecute(
                new MapEventBuiltInEffectExecutionRequest
                {
                    runState = runState,
                    actionPointSystem = actionPointSystem,
                    currentEventTile = tile,
                    optionData = optionData,
                    levelTable = levelTable,
                    sourceSceneName = SceneManager.GetActiveScene().name,
                    battleSceneName = battleSceneName,
                    encounterSeedSalt = Environment.TickCount,
                    randomLootDiscardPreviewCache = eventRandomLootDiscardPreviewCache,
                    moneyBeforeSelection = moneyBeforeSelection,
                    selectionMoneyCost = requiredMoney
                },
                out MapEventBuiltInEffectExecutionResult builtInResult))
        {
            if (spentSelectionMoney)
                runState.AddMoney(requiredMoney);
            runFlowState.PendingEventEncounterData = null;
            message = builtInResult != null ? builtInResult.message : "事件执行失败。";
            return false;
        }

        message = builtInResult.message;
        resultContent = builtInResult.resultContent;
        nextEventOverride = builtInResult.nextEventOverride;
        runFlowState.PendingEventEncounterData = builtInResult.pendingEncounterData;
        runFlowState.PendingSpecialDungeon = builtInResult.pendingSpecialDungeon;

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
                runFlowState.PendingEventEncounterData = null;
                runFlowState.PendingSpecialDungeon = null;
                message = string.IsNullOrWhiteSpace(customMessage) ? "事件的自定义效果未能执行。" : customMessage;
                return false;
            }

            if (!string.IsNullOrWhiteSpace(customMessage))
                message = customMessage;
        }

        bool hasFollowUpEvent = (builtInResult.nextEventOverride != null) || optionData.nextEvent != null;
        if (!hasFollowUpEvent)
        {
            bool shouldResolveEvent = builtInResult.hasResolveEventOverride
                ? builtInResult.resolveEventOverride
                : optionData.resolveEvent;
            tile.isResolved = shouldResolveEvent;
            if (shouldResolveEvent)
                tile.assignedEvent = null;
        }

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

    private static int CalculateRequiredEventMoney(MapEventOptionData optionData, int currentMoney)
    {
        if (optionData == null)
            return 0;

        int fixedCost = Mathf.Max(0, optionData.requiredMoney);
        int percentCost = Mathf.FloorToInt(Mathf.Max(0, currentMoney) * (Mathf.Max(0, optionData.requiredMoneyPercent) / 100f));
        return fixedCost + percentCost;
    }

    private void HandleMapEventOverlayHidden()
    {
        eventRandomLootDiscardPreviewCache.Clear();
        runFlowState.DeferPendingCalamityUntilOverlayClosed = false;
        if (runFlowState.PendingEventEncounterData != null)
        {
            StartPendingEventEncounter();
            return;
        }
        if (runFlowState.PendingSpecialDungeon != null)
        {
            EnterPendingSpecialDungeon();
            return;
        }
        TryPresentPendingCalamity();
    }

    private void HandleMapTreasureOverlayHidden()
    {
        runFlowState.DeferPendingCalamityUntilOverlayClosed = false;
        TryPresentPendingCalamity();
    }

    private void StartPendingEventEncounter()
    {
        if (runFlowState.PendingEventEncounterData == null)
            return;

        if (sceneTransitionController != null && sceneTransitionController.IsTransitioning)
            return;

        BattleEncounterData encounterData = runFlowState.PendingEventEncounterData;
        runFlowState.PendingEventEncounterData = null;
        if (runState != null)
            encounterData.pendingBattleStartAllyStatusGrants = runState.ConsumePendingNextBattleAllyStatusGrants();

        MapRunRuntime.SaveSnapshot(
            SceneManager.GetActiveScene().name,
            runState,
            boardManager,
            actionPointSystem,
            runFlowState.PendingCalamityTiers,
            false,
            activeSpecialDungeonContext);
        BattleEncounterRuntime.SetEncounter(encounterData);
        if (runFlowState.PendingCalamityTiers.Count > 0)
            runFlowState.DeferPendingCalamityUntilMapResume = true;

        RefreshHUD($"进入战斗：{encounterData.encounterLabel}");
        if (sceneTransitionController != null)
            sceneTransitionController.TransitionToScene(battleSceneName);
        else
            SceneManager.LoadScene(battleSceneName);
    }

    private void EnterPendingSpecialDungeon()
    {
        SpecialDungeon_SO dungeon = runFlowState.PendingSpecialDungeon;
        runFlowState.PendingSpecialDungeon = null;
        if (dungeon == null || runState == null || boardManager == null || actionPointSystem == null)
            return;

        if (IsInSpecialDungeon())
        {
            RefreshHUD("当前已经在特殊副本中，无法重复进入。");
            return;
        }

        Vector2Int returnCoord = runState.playerGridPosition;
        MapRunSnapshot mainMapSnapshot = CaptureCurrentMapSnapshot(null);
        if (mainMapSnapshot == null)
        {
            RefreshHUD("进入特殊副本失败：无法保存当前地图。");
            return;
        }

        List<int> deferredCalamities = runFlowState.DrainPendingCalamityTiers();
        activeSpecialDungeonContext = new SpecialDungeonRuntimeContext
        {
            isActive = true,
            activeDungeon = dungeon,
            activeDungeonId = !string.IsNullOrWhiteSpace(dungeon.dungeonId) ? dungeon.dungeonId : dungeon.name,
            returnEventCoord = returnCoord,
            mainMapSnapshot = mainMapSnapshot,
            deferredCalamityTiers = deferredCalamities
        };
        mainMapSnapshot.specialDungeonContext = null;

        SpecialDungeonBoardBuildResult buildResult = SpecialDungeonBoardBuilder.Build(dungeon, Environment.TickCount);
        runState.boardSize = buildResult.boardSize;
        runState.playerGridPosition = buildResult.spawnCoord;
        boardManager.LoadBoard(
            buildResult.boardSize,
            buildResult.spawnCoord,
            buildResult.rightExitCoord,
            buildResult.spawnCoord,
            buildResult.tiles);
        mapBackgroundController.Initialize(backgroundRoot, mapCamera, boardManager);
        playerMapController.Initialize(unitsRoot, boardManager, actionPointSystem, tileResolver, runState);
        if (boardManager.TryGetTile(buildResult.spawnCoord, out BoardTileData spawnTile))
        {
            playerMapController.SetPosition(spawnTile, true);
            boardManager.SetSelectedTile(spawnTile.coord);
            mapHUD.ShowTile(spawnTile, 0);
        }

        mapViewController.Initialize(mapCamera, boardManager, cameraPadding);
        mapViewController.SetInputBlockedEvaluator(IsMapViewInputBlocked);
        mapViewController.PlaySpawnIntro(boardManager.GridToWorld(buildResult.spawnCoord));
        PersistCurrentRunSave();
        RefreshHUD($"进入特殊副本：{dungeon.DisplayName}。副本内移动不消耗行动点。");
    }

    private void ExitSpecialDungeon()
    {
        if (!IsInSpecialDungeon())
        {
            RefreshHUD("当前不在特殊副本中。");
            return;
        }

        MapRunSnapshot restored = SpecialDungeonExitService.BuildReturnedMainMapSnapshot(activeSpecialDungeonContext, runState);
        if (restored == null || restored.runState == null)
        {
            RefreshHUD("离开特殊副本失败：无法恢复主地图。");
            return;
        }

        List<int> deferredCalamities = activeSpecialDungeonContext.deferredCalamityTiers != null
            ? new List<int>(activeSpecialDungeonContext.deferredCalamityTiers)
            : new List<int>();
        activeSpecialDungeonContext = null;
        runState = restored.runState.Clone();
        boardManager.LoadBoard(
            restored.boardSize,
            restored.spawnCoord,
            restored.bossCoord,
            restored.selectedCoord,
            restored.tiles);
        AssignEnemyGroupsToHostileTiles();
        actionPointSystem.RestoreState(
            restored.actionPointBase,
            restored.actionPointBonus,
            restored.actionPointCurrent);
        runFlowState.RestorePendingCalamityTiers(restored.pendingCalamityTiers);
        runFlowState.EnqueuePendingCalamityTiers(deferredCalamities);

        mapBackgroundController.Initialize(backgroundRoot, mapCamera, boardManager);
        playerMapController.Initialize(unitsRoot, boardManager, actionPointSystem, tileResolver, runState);
        if (boardManager.TryGetTile(runState.playerGridPosition, out BoardTileData returnTile))
        {
            playerMapController.SetPosition(returnTile, true);
            boardManager.SetSelectedTile(returnTile.coord);
            mapHUD.ShowTile(returnTile, GetMoveCost(returnTile), true);
        }

        mapViewController.Initialize(mapCamera, boardManager, cameraPadding);
        mapViewController.SetInputBlockedEvaluator(IsMapViewInputBlocked);
        MapRunRuntime.SaveSnapshot(
            SceneManager.GetActiveScene().name,
            runState,
            boardManager,
            actionPointSystem,
            runFlowState.PendingCalamityTiers,
            runFlowState.SuppressPendingCalamityOnNextRestActionPointRestore,
            null);
        RefreshHUD("已离开特殊副本，回到主地图。");
        TryPresentPendingCalamity();
    }

    private void HandleMapMerchantOverlayHidden()
    {
        if (boardManager != null && runState != null && boardManager.TryGetTile(runState.playerGridPosition, out BoardTileData currentTile) && currentTile.tileType == MapTileType.Merchant)
            RelicMerchantService.ResetVisitState(currentTile);

        PersistCurrentRunSave();
        runFlowState.DeferPendingCalamityUntilOverlayClosed = false;
        TryPresentPendingCalamity();
    }

    private void HandleMapCraftingOverlayHidden()
    {
        runFlowState.DeferPendingCalamityUntilOverlayClosed = false;
        TryPresentPendingCalamity();
    }

    private bool CanOpenMapPauseMenu()
    {
        return runState != null && !IsGameplayOverlayVisible();
    }

    private void HandlePauseMenuSaveAndReturn()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        PersistCurrentRunSave();
        MapRunRuntime.Clear(sceneName);
        RestSceneRuntime.Clear();
        BattleEncounterRuntime.Clear();
        MainMenuRuntime.ClearContinueRequest();
        MainMenuRuntime.ClearTutorialRequest();
        TransitionToMainMenu();
    }

    private void HandlePauseMenuAbandon()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        MainMenuRunSaveSystem.DeleteSavedRun();
        MapRunRuntime.Clear(sceneName);
        RestSceneRuntime.Clear();
        BattleEncounterRuntime.Clear();
        MainMenuRuntime.ClearContinueRequest();
        MainMenuRuntime.ClearTutorialRequest();
        TransitionToMainMenu();
    }

    private void TransitionToMainMenu()
    {
        string targetSceneName = BattleExitFlowService.ResolveMainMenuSceneName(mainMenuSceneName);
        if (sceneTransitionController != null)
        {
            sceneTransitionController.TransitionToScene(targetSceneName);
            return;
        }

        if (SceneCurtainTransitionController.Instance != null)
        {
            SceneCurtainTransitionController.Instance.TransitionToScene(targetSceneName);
            return;
        }

        SceneManager.LoadScene(targetSceneName);
    }

    private bool IsMapViewInputBlocked()
    {
        if (mapPauseMenuOverlay != null && mapPauseMenuOverlay.IsVisible)
            return true;
        if (mapTutorialGuideOverlay != null && mapTutorialGuideOverlay.IsVisible)
            return true;

        return IsGameplayOverlayVisible();
    }

    private bool IsGameplayOverlayVisible()
    {
        if (mapEventOverlay != null && mapEventOverlay.IsVisible)
            return true;
        if (mapMerchantOverlay != null && mapMerchantOverlay.IsVisible)
            return true;
        if (mapTreasureOverlay != null && mapTreasureOverlay.IsVisible)
            return true;
        if (mapCraftingOverlay != null && mapCraftingOverlay.IsVisible)
            return true;
        if (mapCalamityOverlay != null && mapCalamityOverlay.IsVisible)
            return true;
        if (mapTutorialGuideOverlay != null && mapTutorialGuideOverlay.IsVisible)
            return true;

        return false;
    }

    private void StartMapTutorialGuide()
    {
        if (!isTutorialRun)
            return;

        if (mapTutorialGuideOverlay == null)
            mapTutorialGuideOverlay = GetOrCreateTutorialGuideOverlay();

        if (mapTutorialGuideOverlay == null)
            return;

        tutorialMapIntroSequenceActive = false;
        mapTutorialGuideStep = MapTutorialGuideStep.None;
        ShowMapTutorialMessage(
            "欢迎来到骰掷大陆",
            "这里会带你学会进入地图后的基本操作。首先试着拖动画面、滚动鼠标滚轮，或用双指缩放来观察地图。",
            BeginWaitForViewNavigation,
            "开始观察");
        tutorialMapIntroSequenceActive = mapTutorialGuideOverlay != null && mapTutorialGuideOverlay.IsVisible;
    }

    private void BeginWaitForViewNavigation()
    {
        mapTutorialGuideStep = MapTutorialGuideStep.WaitingForViewNavigation;
        tutorialViewNavigationObserved = false;
        tutorialLastViewNavigationTime = 0f;
        tutorialViewPointerTracking = false;
        tutorialViewPointerLastScreenPosition = Vector2.zero;
        mapHUD?.ShowStatus("请先拖动或缩放地图。完成后教程会继续。");
    }

    private void HandleMapViewNavigationPerformed()
    {
        if (!isTutorialRun || mapTutorialGuideStep != MapTutorialGuideStep.WaitingForViewNavigation)
            return;

        tutorialViewNavigationObserved = true;
        tutorialLastViewNavigationTime = Time.unscaledTime;
        mapHUD?.ShowStatus("很好，松开操作并稍等片刻，教程会继续。");
    }

    private void UpdateMapTutorialGuide()
    {
        if (!isTutorialRun || mapTutorialGuideStep != MapTutorialGuideStep.WaitingForViewNavigation)
            return;

        DetectTutorialViewNavigationFallback();

        if (!tutorialViewNavigationObserved)
            return;

        if (Time.unscaledTime - tutorialLastViewNavigationTime < tutorialViewNavigationSettleSeconds)
            return;

        tutorialViewNavigationObserved = false;
        tutorialViewPointerTracking = false;
        mapTutorialGuideStep = MapTutorialGuideStep.None;
        ShowMapActionPointGuide();
    }

    private void DetectTutorialViewNavigationFallback()
    {
        if (mapTutorialGuideOverlay != null && mapTutorialGuideOverlay.IsVisible)
            return;

        if (Input.touchCount >= 2)
        {
            MarkTutorialViewNavigationObserved();
            return;
        }

        if (Mathf.Abs(Input.mouseScrollDelta.y) > 0.001f)
        {
            MarkTutorialViewNavigationObserved();
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            tutorialViewPointerTracking = true;
            tutorialViewPointerLastScreenPosition = Input.mousePosition;
            return;
        }

        if (Input.GetMouseButton(0) && tutorialViewPointerTracking)
        {
            Vector2 currentScreenPosition = Input.mousePosition;
            if ((currentScreenPosition - tutorialViewPointerLastScreenPosition).sqrMagnitude >= 36f)
                MarkTutorialViewNavigationObserved();
            tutorialViewPointerLastScreenPosition = currentScreenPosition;
            return;
        }

        if (Input.GetMouseButtonUp(0))
        {
            tutorialViewPointerTracking = false;
            tutorialViewPointerLastScreenPosition = Vector2.zero;
        }
    }

    private void MarkTutorialViewNavigationObserved()
    {
        tutorialViewNavigationObserved = true;
        tutorialLastViewNavigationTime = Time.unscaledTime;
        mapHUD?.ShowStatus("很好，松开操作并稍等片刻，教程会继续。");
    }

    private void ShowMapActionPointGuide()
    {
        ShowMapTutorialMessage(
            "行动点",
            "左上角的状态栏显示了你当前的行动点，移动到其它格子都会消耗你的行动点。",
            ShowMapMoneyGuide,
            "继续",
            true);
    }

    private void ShowMapMoneyGuide()
    {
        ShowMapTutorialMessage(
            "金币",
            "金币是游戏的必要资源！你可以用金币在商店或事件中换取你需要的资源。",
            ShowMapKeyGuide,
            "继续",
            true);
    }

    private void ShowMapKeyGuide()
    {
        ShowMapTutorialMessage(
            "钥匙",
            "钥匙是开启 Boss 战、完成游戏的必经之路。左边的数字代表你目前拥有的钥匙数量，右边的数字代表所需钥匙数量。你可以在“钥匙格”拾取钥匙；地图通常会生成多余所需数量的钥匙格，所以不必完整探索地图，而是找到属于自己的路线。",
            ShowMapTileInspectIntro,
            "继续",
            true);
    }

    private void ShowMapTileInspectIntro()
    {
        ShowMapTutorialMessage(
            "查看格子详情",
            "点击任意一个地图格子可以查看该格子的详情。",
            BeginWaitForBattleInspect);
    }

    private void BeginWaitForBattleInspect()
    {
        tutorialMapIntroSequenceActive = false;
        tutorialBattleInspectSelectionAllowed = true;
        mapTutorialGuideStep = MapTutorialGuideStep.WaitingForBattleInspect;
        mapHUD?.ShowStatus("请点击右侧的战斗格查看详情。");
    }

    private bool CanHandleTutorialTileClick(BoardTileData tile)
    {
        if (isTutorialRun && tutorialMapIntroSequenceActive)
        {
            mapHUD?.ShowStatus("请先阅读并完成当前教程提示。");
            return false;
        }

        if (!isTutorialRun || mapTutorialGuideStep == MapTutorialGuideStep.None)
            return true;

        if (mapTutorialGuideOverlay != null && mapTutorialGuideOverlay.IsVisible)
            return false;

        switch (mapTutorialGuideStep)
        {
            case MapTutorialGuideStep.WaitingForViewNavigation:
                mapHUD?.ShowStatus("请先拖动或缩放地图。");
                return false;
            case MapTutorialGuideStep.WaitingForBattleInspect:
                if (!IsTutorialFirstBattleTile(tile))
                {
                    mapHUD?.ShowStatus("请先点击高亮的战斗格。");
                    return false;
                }
                mapTutorialGuideStep = MapTutorialGuideStep.None;
                return true;
            case MapTutorialGuideStep.WaitingForFirstMoveSelect:
                if (!IsTutorialFirstMoveTile(tile))
                {
                    mapHUD?.ShowStatus("请点击角色右侧相邻的空白格。");
                    return false;
                }
                return true;
            case MapTutorialGuideStep.WaitingForFirstMoveConfirm:
                if (!IsTutorialFirstMoveTile(tile))
                {
                    mapHUD?.ShowStatus("请再次点击已选中的空白格来移动。");
                    return false;
                }
                mapTutorialGuideStep = MapTutorialGuideStep.WaitingForFirstMoveResolve;
                return true;
            case MapTutorialGuideStep.WaitingForFirstMoveResolve:
                mapHUD?.ShowStatus("正在移动，请稍候。");
                return false;
            case MapTutorialGuideStep.WaitingForBattleEntry:
                if (!IsTutorialFirstBattleTile(tile))
                {
                    mapHUD?.ShowStatus("请继续前往右侧的战斗格。");
                    return false;
                }
                return true;
            default:
                return true;
        }
    }

    private void ContinueAfterBattleTileInspected()
    {
        ShowMapTutorialMessage(
            "格子详情",
            "第一行会显示坐标与格子类型。“战斗”代表进入该格后会开启一场战斗。第二行记录探索状态，完成格子内容后会变为已解决。",
            ShowMapTileMovementCostGuide);
    }

    private void ShowMapTileMovementCostGuide()
    {
        ShowMapTutorialMessage(
            "移动消耗",
            "详情中还会显示进入格子需要的行动点。通常探索新格子消耗 2 点，移动到已探索格子只消耗 1 点。",
            ShowMapBattleTileDetailGuide);
    }

    private void ShowMapBattleTileDetailGuide()
    {
        ShowMapTutorialMessage(
            "战斗信息",
            "战斗格会显示敌人属性、危险度与区域。提前观察这些信息，可以帮助你规划路线和准备装备。",
            ShowMapFirstMoveIntro);
    }

    private void ShowMapFirstMoveIntro()
    {
        ShowMapTutorialMessage(
            "试着移动",
            "你已经知道如何查看格子了。接下来点击角色右侧相邻的空白格来选中它。",
            BeginWaitForFirstMoveSelection);
    }

    private void BeginWaitForFirstMoveSelection()
    {
        mapTutorialGuideStep = MapTutorialGuideStep.WaitingForFirstMoveSelect;
        mapHUD?.ShowStatus("请点击右侧相邻的空白格。");
    }

    private void ContinueAfterFirstMoveSelected()
    {
        ShowMapTutorialMessage(
            "确认移动",
            "第一次点击会查看并选中格子。现在再次点击同一个格子，角色就会移动过去。",
            () =>
            {
                mapTutorialGuideStep = MapTutorialGuideStep.WaitingForFirstMoveConfirm;
                mapHUD?.ShowStatus("请再次点击已选中的空白格来移动。");
            });
    }

    private void ContinueAfterFirstMoveCompleted()
    {
        ShowMapTutorialMessage(
            "准备进入战斗",
            "你完成了一次移动。下一格会遭遇一场轻松的战斗，你已经提前看过它的详情了。继续点击右侧战斗格，进入第一场教程战斗。",
            () =>
            {
                mapTutorialGuideStep = MapTutorialGuideStep.WaitingForBattleEntry;
                if (TryGetTutorialFirstBattleTile(out BoardTileData battleTile))
                {
                    boardManager.SetSelectedTile(battleTile.coord);
                    boardManager.RefreshAllViews();
                }
                mapHUD?.ShowStatus("请点击右侧战斗格进入教程战斗。");
            });
    }

    private void ShowMapTutorialMessage(string title, string body, Action onContinue, string buttonLabel = "继续", bool highlightTopLeftStatus = false)
    {
        mapTutorialGuideStep = MapTutorialGuideStep.None;
        if (mapTutorialGuideOverlay == null)
            return;

        if (highlightTopLeftStatus)
        {
            mapTutorialGuideOverlay.Show(
                title,
                body,
                onContinue,
                buttonLabel,
                true,
                TutorialTopLeftStatusHighlightAnchor,
                TutorialTopLeftStatusHighlightSize);
            return;
        }

        mapTutorialGuideOverlay.Show(title, body, onContinue, buttonLabel);
    }

    private bool TryGetTutorialFirstMoveTile(out BoardTileData tile)
    {
        return boardManager.TryGetTile(new Vector2Int(TutorialFirstMoveSequenceIndex, TutorialBoardSize / 2), out tile);
    }

    private bool TryGetTutorialFirstBattleTile(out BoardTileData tile)
    {
        return boardManager.TryGetTile(new Vector2Int(TutorialFirstBattleSequenceIndex, TutorialBoardSize / 2), out tile);
    }

    private bool IsTutorialFirstMoveTile(BoardTileData tile)
    {
        return tile != null && tile.coord == new Vector2Int(TutorialFirstMoveSequenceIndex, TutorialBoardSize / 2);
    }

    private bool IsTutorialFirstBattleTile(BoardTileData tile)
    {
        return tile != null && tile.coord == new Vector2Int(TutorialFirstBattleSequenceIndex, TutorialBoardSize / 2);
    }

    private void TryPresentPendingCalamity()
    {
        if (runState == null || mapCalamityOverlay == null)
            return;
        if (runFlowState.DeferPendingCalamityUntilMapResume)
            return;
        if (runFlowState.DeferPendingCalamityUntilOverlayClosed)
            return;
        if (mapCalamityOverlay.IsVisible)
            return;
        if (mapEventOverlay != null && mapEventOverlay.IsVisible)
            return;
        if (mapMerchantOverlay != null && mapMerchantOverlay.IsVisible)
            return;
        if (mapTreasureOverlay != null && mapTreasureOverlay.IsVisible)
            return;
        if (mapCraftingOverlay != null && mapCraftingOverlay.IsVisible)
            return;
        if (mapPauseMenuOverlay != null && mapPauseMenuOverlay.IsVisible)
            return;
        if (mapTutorialGuideOverlay != null && mapTutorialGuideOverlay.IsVisible)
            return;

        while (runFlowState.PendingCalamityTiers.Count > 0)
        {
            int calamityTier = Mathf.Max(1, runFlowState.PendingCalamityTiers.Dequeue());
            List<Calamity_SO> choices = CalamitySelectionService.RollChoices(calamityPool, runState, calamityTier);
            if (choices.Count <= 0)
            {
                RefreshHUD($"\u707e\u5384\u7b49\u7ea7 {calamityTier} \u5df2\u65e0\u53ef\u7528\u707e\u5384\uff0c\u8df3\u8fc7\u672c\u6b21\u707e\u5384\u9009\u62e9\u3002");
                continue;
            }

            CalamitySelectionService.MarkChoicesDrawn(runState, choices);

            RefreshHUD($"\u707e\u5384\u964d\u4e34\uff0c\u8bf7\u4ece {choices.Count} \u4e2a\u9009\u9879\u4e2d\u9009\u62e9\u5176\u4e00\u3002");
            mapCalamityOverlay.Show(calamityTier, choices, calamity => HandleCalamityChosen(calamityTier, calamity));
            return;
        }
    }

    private void HandleCalamityChosen(int calamityTier, Calamity_SO calamity)
    {
        if (runState == null || calamity == null)
        {
            TryPresentPendingCalamity();
            return;
        }

        CalamitySelectionService.ApplyChosenCalamity(runState, calamity, calamityTier);
        List<string> builtInFeedback = ApplyChosenCalamityBuiltInMapEffects(calamity);
        calamity.customEffectHandler?.OnCalamityApplied(runState, this);
        RelicExecutionContext context = RelicTriggerDispatcher.ProcessCalamityApplied(runState, this, calamity, calamityTier);

        string calamityMessage = $"\u5df2\u627f\u53d7\u707e\u5384\uff1a{calamity.DisplayName}";
        if (builtInFeedback.Count > 0)
            calamityMessage = $"{calamityMessage}\n{string.Join("\n", builtInFeedback)}";
        if (context.feedbackMessages != null && context.feedbackMessages.Count > 0)
            calamityMessage = $"{calamityMessage}\n{string.Join("\n", context.feedbackMessages)}";

        RefreshHUD(calamityMessage);
        TryPresentPendingCalamity();
    }

    private List<string> ApplyChosenCalamityBuiltInMapEffects(Calamity_SO calamity)
    {
        List<string> feedback = new List<string>();
        if (calamity == null)
            return feedback;

        IReadOnlyList<CalamityBuiltInEffectEntry> effects = calamity.GetValidEffects();
        for (int i = 0; i < effects.Count; i++)
        {
            CalamityBuiltInEffectEntry effect = effects[i];
            if (effect == null)
                continue;

            switch (effect.effectType)
            {
                case CalamityBuiltInEffectType.TeleportToRandomOuterUnvisitedTile:
                    if (TryTeleportToRandomOuterUnvisitedTile(out BoardTileData destination))
                        feedback.Add($"灾厄效果：你被传送至外围未探索格子 [{destination.coord.x},{destination.coord.y}]。");
                    else
                        feedback.Add("灾厄效果：没有可传送的外围未探索格子。");
                    break;
                case CalamityBuiltInEffectType.LoseRandomRelics:
                    feedback.Add(RemoveRandomRelics(Mathf.Max(0, effect.effectAmount)));
                    break;
                case CalamityBuiltInEffectType.ConvertUnvisitedRestTilesToBattles:
                    int converted = ConvertRandomUnvisitedRestTilesToBattles(Mathf.Max(0, effect.effectAmount));
                    feedback.Add(converted > 0
                        ? $"灾厄效果：{converted} 个未探索休息格变为了普通战斗。"
                        : "灾厄效果：没有可替换的未探索休息格。");
                    break;
            }
        }

        return feedback;
    }

    private bool TryTeleportToRandomOuterUnvisitedTile(out BoardTileData destination)
    {
        destination = null;
        if (boardManager == null || runState == null)
            return false;

        List<BoardTileData> candidates = new List<BoardTileData>();
        foreach (BoardTileData tile in boardManager.GetAllTiles())
        {
            if (tile == null
                || tile.isVisited
                || tile.isSpawnTile
                || tile.dangerBand != MapDangerBand.Outer
                || tile.tileType == MapTileType.Boss
                || tile.tileType == MapTileType.SpecialDungeonExit)
            {
                continue;
            }

            candidates.Add(tile);
        }

        if (candidates.Count <= 0)
            return false;

        destination = candidates[UnityEngine.Random.Range(0, candidates.Count)];
        runState.playerGridPosition = destination.coord;
        boardManager.SetSelectedTile(destination.coord);
        boardManager.RefreshAllViews();
        mapHUD?.ShowTile(destination, GetMoveCost(destination), true);
        return true;
    }

    private string RemoveRandomRelics(int amount)
    {
        if (amount <= 0)
            return "灾厄效果：没有失去遗物。";
        if (runState == null || runState.relicInventory == null || runState.relicInventory.Count == 0)
            return "灾厄效果：没有可失去的遗物。";

        List<string> removedNames = new List<string>();
        for (int i = 0; i < amount; i++)
        {
            List<RunRelicState> candidates = new List<RunRelicState>();
            for (int stackIndex = 0; stackIndex < runState.relicInventory.Count; stackIndex++)
            {
                RunRelicState stack = runState.relicInventory[stackIndex];
                if (stack != null && stack.relicData != null && stack.amount > 0)
                    candidates.Add(stack);
            }

            if (candidates.Count <= 0)
                break;

            RunRelicState selected = candidates[UnityEngine.Random.Range(0, candidates.Count)];
            Relic_SO relicData = selected.relicData;
            string relicName = relicData.DisplayName;
            if (runState.TryConsumeRelic(relicData, 1))
                removedNames.Add(relicName);
        }

        return removedNames.Count > 0
            ? $"灾厄效果：失去遗物 {string.Join("、", removedNames)}。"
            : "灾厄效果：没有可失去的遗物。";
    }

    private int ConvertRandomUnvisitedRestTilesToBattles(int amount)
    {
        if (amount <= 0 || boardManager == null)
            return 0;

        List<BoardTileData> candidates = new List<BoardTileData>();
        foreach (BoardTileData tile in boardManager.GetAllTiles())
        {
            if (tile != null && tile.tileType == MapTileType.Rest && !tile.isVisited)
                candidates.Add(tile);
        }

        int converted = 0;
        while (converted < amount && candidates.Count > 0)
        {
            int index = UnityEngine.Random.Range(0, candidates.Count);
            BoardTileData tile = candidates[index];
            candidates.RemoveAt(index);
            if (tile == null)
                continue;

            tile.tileType = MapTileType.Battle;
            tile.isResolved = false;
            tile.assignedEnemyGroup = null;
            tile.assignedEvent = null;
            tile.previewTag = string.Empty;
            tile.previewEnemyCount = 0;
            EnsureTileAssignedEnemyGroup(tile);
            tile.previewDangerRating = MapDangerRatingService.ResolveRating(tile.tileType, tile.dangerBand, tile.assignedEnemyGroup);
            boardManager.RefreshTile(tile.coord);
            converted++;
        }

        if (converted > 0)
            boardManager.RefreshAllViews();
        return converted;
    }

    private bool WillSpendExhaustCurrentCycle(int moveCost)
    {
        return MapMovementService.WillSpendExhaustCurrentCycle(actionPointSystem, moveCost);
    }

    private bool IsInSpecialDungeon()
    {
        return activeSpecialDungeonContext != null && activeSpecialDungeonContext.isActive;
    }

    private static bool IsSpecialDungeonTreasure(BoardTileData tile)
    {
        return tile != null &&
            tile.sourceSpecialDungeon != null &&
            tile.specialDungeonTreasureOptions != null &&
            tile.specialDungeonTreasureOptions.Count > 0;
    }

    private MapRunSnapshot CaptureCurrentMapSnapshot(SpecialDungeonRuntimeContext specialDungeonContext)
    {
        MapRunRuntime.SaveSnapshot(
            SceneManager.GetActiveScene().name,
            runState,
            boardManager,
            actionPointSystem,
            runFlowState.PendingCalamityTiers,
            runFlowState.SuppressPendingCalamityOnNextRestActionPointRestore,
            specialDungeonContext);

        return MapRunRuntime.TryGetSnapshot(SceneManager.GetActiveScene().name, out MapRunSnapshot snapshot)
            ? snapshot
            : null;
    }

    private EnemyGroup_SO SelectEnemyGroupForTile(BoardTileData tile)
    {
        return MapBattleEncounterService.SelectEnemyGroupForTile(enemyRoster, tile);
    }

    private void EnsureTileAssignedEnemyGroup(BoardTileData tile)
    {
        MapBattleEncounterService.EnsureTileAssignedEnemyGroup(enemyRoster, tile);
        if (tile != null && tile.IsHostileTile)
        {
            tile.previewDangerRating = MapDangerRatingService.ResolveRating(
                tile.tileType,
                tile.dangerBand,
                tile.assignedEnemyGroup,
                tile.sourceSpecialDungeon != null ? tile.previewDangerRating : 0);
        }
    }

    private void EnsureTileAssignedEvent(BoardTileData tile)
    {
        MapEventAssignmentService.EnsureTileAssignedEvent(eventPool, runState, tile);
    }

    private void ConfigureTreasureRewardFallbacks()
    {
        if (treasureRewardTable == null)
            return;

        treasureRewardTable.ConfigureRuntimeDefaults(
            Resources.Load<BlueprintTable_SO>(TreasureBlueprintTableResourcePath) ?? FindRuntimeAssetByName<BlueprintTable_SO>("MainBlueprintTable"),
            Resources.Load<LootRarityWeightTable_SO>(TreasureRelicBagRarityWeightTableResourcePath),
            Resources.Load<RelicTable_SO>(TreasureRelicTableResourcePath) ?? eliteRelicChoiceTable ?? FindRuntimeAssetByName<RelicTable_SO>("OuterEliteRelicLoot"),
            Resources.Load<LootRarityWeightTable_SO>(TreasureRelicBagRarityWeightTableResourcePath));
    }

    private static T FindRuntimeAssetByName<T>(string assetName) where T : UnityEngine.Object
    {
        if (string.IsNullOrWhiteSpace(assetName))
            return null;

        T[] assets = Resources.FindObjectsOfTypeAll<T>();
        if (assets == null)
            return null;

        for (int i = 0; i < assets.Length; i++)
        {
            T asset = assets[i];
            if (asset != null && string.Equals(asset.name, assetName, StringComparison.Ordinal))
                return asset;
        }

        return null;
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

        if (tile.sourceSpecialDungeon != null)
        {
            tile.merchantStockGenerated = true;
            return;
        }

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

    private string ResolveTutorialCompletionSceneName()
    {
        if (!string.IsNullOrWhiteSpace(MainMenuRuntime.TutorialReturnSceneName))
            return MainMenuRuntime.TutorialReturnSceneName;

        return tutorialCompletionSceneName;
    }

    private List<RunPartyMemberState> BuildTutorialStartingPartyMembers()
    {
        return RunBootstrapService.BuildTutorialStartingPartyMembers(tutorialStartingPartyMembers, startingPartyMembers);
    }

    private List<RunPartyMemberState> BuildStartingPartyMembers(List<StartingPartyMemberDefinition> sourceDefinitions)
    {
        return RunBootstrapService.BuildStartingPartyMembers(sourceDefinitions);
    }

    private void ApplyStartingInventory(RunState targetRunState)
    {
        RunBootstrapService.ApplyStartingInventory(
            targetRunState,
            new RunStartingInventoryDefinition
            {
                startingLoot = startingLoot,
                startingWeapons = startingWeapons,
                startingBlueprints = startingBlueprints,
                startingRelics = startingRelics
            });
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
            runFlowState.PendingCalamityTiers,
            runFlowState.SuppressPendingCalamityOnNextRestActionPointRestore,
            activeSpecialDungeonContext);
    }

    private MainMenuSaveAssetCatalog BuildSaveAssetCatalog()
    {
        return RunSaveAssetCatalogBuilder.Build(
            new RunSaveAssetCatalogBuildRequest
            {
                difficultyConfig = difficultyConfig,
                activeDifficultyConfig = activeDifficultyConfig,
                selectedDifficultyConfig = MainMenuRuntime.SelectedDifficultyConfig,
                startingPartyMembers = startingPartyMembers,
                startingLoot = startingLoot,
                startingWeapons = startingWeapons,
                startingBlueprints = startingBlueprints,
                startingRelics = startingRelics,
                recruitCharacterRoster = recruitCharacterRoster,
                enemyRoster = enemyRoster,
                eventPool = eventPool,
                merchantCatalog = merchantCatalog,
                calamityPool = calamityPool,
                treasureRewardTable = treasureRewardTable,
                runState = runState,
                boardManager = boardManager,
                specialDungeonContext = activeSpecialDungeonContext
            });
    }

}


