using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using TMPro;

public sealed class LuckyMazeDungeonTests
{
    private const string Folder = "Assets/Data/Events/033 幸运游戏（其二）/";
    private readonly List<Object> temporary = new List<Object>();
    private SpecialDungeon_SO Dungeon => AssetDatabase.LoadAssetAtPath<SpecialDungeon_SO>(Folder + "033 幸运迷宫.asset");

    [TearDown]
    public void Cleanup()
    {
        foreach (Object item in temporary.AsEnumerable().Reverse()) if (item != null) Object.DestroyImmediate(item);
        temporary.Clear();
    }

    [Test]
    public void Assets_KeepLuckyMazeInPoolAndRemoveRoyalRevengeEntrance()
    {
        var pool = AssetDatabase.LoadAssetAtPath<MapEventPool_SO>("Assets/Data/Events/MapEventPool.asset");
        Assert.IsFalse(pool.GetValidEvents().Any(e => e.eventId == "031"));
        Assert.IsTrue(pool.GetValidEvents().Any(e => e.eventId == "033"));
        Assert.IsTrue(Dungeon.eventDrivenNavigation && Dungeon.eventRewardsOnce);
        Assert.AreEqual(10, Dungeon.GetValidEntries().Count);
        foreach (SpecialDungeonTileEntry room in Dungeon.GetValidEntries())
        {
            Assert.IsTrue(room.eventData.hideConsequences);
            foreach (MapEventOptionData option in room.eventData.options)
            {
                Assert.IsNull(option.nextEvent, room.eventData.eventId);
                Assert.IsFalse(option.resolveEvent);
                Assert.IsTrue(SpecialDungeonEventNavigationService.IsValidDestination(Tile(room.eventData), option.specialDungeonDestinationEntryIndex));
                Assert.AreNotEqual(-1, option.specialDungeonDestinationEntryIndex);
            }
        }
        foreach (MapEventOptionData option in Event("033 幸运游戏（其二）.asset").options) Assert.IsTrue(option.resolveEvent);
    }

    [Test]
    public void EachMazeRoomResolvesItsOwnIllustration()
    {
        Sprite entrance = MapEventSceneArt.Resolve(Event("033 幸运游戏（其二）.asset"));
        var illustrations = new HashSet<Sprite>();
        foreach (SpecialDungeonTileEntry entry in Dungeon.GetValidEntries())
        {
            MapEvent_SO room = entry.eventData;
            Sprite illustration = MapEventSceneArt.Resolve(room);
            Assert.IsNotNull(illustration, room.eventId);
            Assert.AreNotSame(entrance, illustration, room.eventId);
            Assert.IsTrue(illustrations.Add(illustration), $"Duplicate room illustration: {room.eventId}");
        }
    }

    [Test]
    public void InitialMovement_OnlyReachesFirstEvent_AndStaysLockedAfterReturningToSpawn()
    {
        var built = SpecialDungeonBoardBuilder.Build(Dungeon, 123);
        var context = new SpecialDungeonRuntimeContext { isActive = true, activeDungeon = Dungeon };
        BoardTileData first = built.tiles.Single(t => t.specialDungeonEntryIndex == 0);
        Assert.IsTrue(SpecialDungeonEventNavigationService.CanPlayerMove(context, built.spawnCoord, first));
        Assert.IsFalse(SpecialDungeonEventNavigationService.CanPlayerMove(context, built.spawnCoord, built.tiles[0]));
        Assert.IsFalse(SpecialDungeonEventNavigationService.CanPlayerMove(context, first.coord, built.tiles.Single(t => t.specialDungeonEntryIndex == 1)));
        SpecialDungeonEventNavigationService.MarkEventStarted(context, first);
        Assert.IsFalse(SpecialDungeonEventNavigationService.CanPlayerMove(context.Clone(), built.spawnCoord, first));
    }

    [Test]
    public void RevisitedOption_AwardsMoneyOnce_WhileAnotherRoomStillAwardsItsOwnReward()
    {
        RunState state = State();
        MapEvent_SO room = Event("033_A_02 根号一.asset");
        BoardTileData tile = Tile(room);
        Execute(state, tile, room.options[1]); Assert.AreEqual(120, state.money);
        Execute(state, tile.Clone(), room.options[1]); Assert.AreEqual(120, state.money);
        MapEvent_SO other = Event("033_B_02 根号二.asset");
        Execute(state, Tile(other), other.options[0]); Assert.AreEqual(140, state.money);
        Assert.AreEqual(1, tile.claimedEventOptionRewards.Count);
    }

    [Test]
    public void BathroomSelfLoop_HealsOnce_AndStillAllowsTheSameRouteAgain()
    {
        RunState state = State(); state.partyMembers[0].currentHP = 40;
        MapEvent_SO room = Event("033_A_00 盥洗室.asset"); BoardTileData tile = Tile(room);
        Assert.AreEqual(0, Execute(state, tile, room.options[2]).pendingSpecialDungeonEntryIndex);
        Assert.AreEqual(100, state.partyMembers[0].currentHP);
        state.partyMembers[0].currentHP = 50;
        Assert.AreEqual(0, Execute(state, tile, room.options[2]).pendingSpecialDungeonEntryIndex);
        Assert.AreEqual(50, state.partyMembers[0].currentHP);
    }

    [Test]
    public void RevisitedFailure_StillAppliesItsMoneyAndHealthCosts()
    {
        RunState state = State(); MapEvent_SO room = Event("033_A_01 科目一.asset"); BoardTileData tile = Tile(room);
        Execute(state, tile, room.options[1]); Execute(state, tile, room.options[1]);
        Assert.AreEqual(60, state.money); Assert.AreEqual(60, state.partyMembers[0].currentHP);
    }

    [Test]
    public void RevisitedLootAndFinalRelicOptions_CannotDuplicateInventoryRewards()
    {
        RunState state = State(); MapEvent_SO lesson = Event("033_A_01 科目一.asset"); BoardTileData tile = Tile(lesson);
        Execute(state, tile, lesson.options[0]); int loot = state.lootInventory.Sum(x => x.amount);
        Assert.Greater(loot, 0); Execute(state, tile, lesson.options[0]); Assert.AreEqual(loot, state.lootInventory.Sum(x => x.amount));
        MapEvent_SO end = Event("033_E_01 休息室.asset"); tile = Tile(end);
        Assert.AreEqual(-2, Execute(state, tile, end.options[1]).pendingSpecialDungeonEntryIndex);
        int relics = state.relicInventory.Sum(x => x.amount); Assert.Greater(relics, 0);
        Execute(state, tile, end.options[1]); Assert.AreEqual(relics, state.relicInventory.Sum(x => x.amount));
    }

    [Test]
    public void RewardReceiptsAndMovementLock_SurviveActualSaveSerializationAndSnapshotCloning()
    {
        RunState state = State(); MapEvent_SO room = Event("033_A_02 根号一.asset"); BoardTileData tile = Tile(room);
        Execute(state, tile, room.options[1]);
        var catalog = new MainMenuSaveAssetCatalog(); catalog.Add(Dungeon); catalog.Add(room);
        object saved = SaveCall("SerializeTile", tile);
        object decoded = JsonUtility.FromJson(JsonUtility.ToJson(saved), saved.GetType());
        var restored = (BoardTileData)SaveCall("DeserializeTile", decoded, catalog);
        Execute(state, restored, room.options[1]); Assert.AreEqual(120, state.money);
        restored.claimedEventOptionRewards.Add("extra"); Assert.AreEqual(1, tile.claimedEventOptionRewards.Count);
        var context = new SpecialDungeonRuntimeContext { isActive = true, activeDungeon = Dungeon, activeDungeonId = Dungeon.dungeonId,
            eventNavigationStarted = true, mainMapSnapshot = new MapRunSnapshot { runState = State() } };
        saved = SaveCall("SerializeSpecialDungeonContext", context);
        decoded = JsonUtility.FromJson(JsonUtility.ToJson(saved), saved.GetType());
        var loaded = (SpecialDungeonRuntimeContext)SaveCall("DeserializeSpecialDungeonContext", decoded, catalog);
        Assert.IsTrue(loaded.eventNavigationStarted); Assert.IsTrue(loaded.Clone().eventNavigationStarted);
        CollectionAssert.AreEqual(tile.claimedEventOptionRewards, tile.Clone().claimedEventOptionRewards);
    }

    [Test]
    public void RoomJumps_SupportMoreThanSixteenTransitions_AndReachTheExitWithoutAPSpending()
    {
        var go = new GameObject("LuckyMazeRouting"); temporary.Add(go);
        var board = go.AddComponent<BoardManager>(); board.ConfigureHeadlessExecution();
        var mover = go.AddComponent<PlayerMapController>(); mover.ConfigureHeadlessExecution();
        var built = SpecialDungeonBoardBuilder.Build(Dungeon, 91);
        RunState state = State(); state.boardSize = built.boardSize; state.playerGridPosition = built.spawnCoord;
        board.LoadBoard(built.boardSize, built.spawnCoord, built.rightExitCoord, built.spawnCoord, built.tiles);
        mover.Initialize(go.transform, board, null, null, state);
        var context = new SpecialDungeonRuntimeContext { isActive = true, activeDungeon = Dungeon };
        for (int i = 0; i < 20; i++)
        {
            Assert.IsTrue(SpecialDungeonEventNavigationService.TryNavigate(context, i % 2, state, board, mover, out BoardTileData target));
            Assert.AreEqual(target.coord, state.playerGridPosition);
            Assert.IsFalse(target.isResolved);
        }
        Assert.IsTrue(SpecialDungeonEventNavigationService.TryNavigate(context, -2, state, board, mover, out BoardTileData exit));
        Assert.AreEqual(MapTileType.SpecialDungeonExit, exit.tileType); Assert.AreEqual(built.rightExitCoord, state.playerGridPosition);
    }

    [Test]
    public void HiddenConsequences_AreAbsentFromChoicesAndBothFooterSteps_AndNormalEventsRestoreThem()
    {
        var go = new GameObject("HiddenLuckyConsequences"); temporary.Add(go);
        var overlay = go.AddComponent<MapEventOverlay>(); MapEvent_SO room = Event("033_D_01 科目四.asset");
        overlay.Show(Tile(room), room, null);
        Assert.IsFalse(Field<TMP_Text>(overlay, "consequenceTitleText").gameObject.activeInHierarchy);
        Assert.IsFalse(Field<UnityEngine.UI.ScrollRect>(overlay, "consequenceScrollRect").gameObject.activeInHierarchy);
        Assert.IsTrue(go.GetComponentsInChildren<TMP_Text>(true).Where(t => t.gameObject.name == "Detail").All(t => !t.gameObject.activeSelf && string.IsNullOrEmpty(t.text)));
        typeof(MapEventOverlay).GetMethod("ShowResultStep", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(overlay, new object[] { room.options[1], "获得奖励", "故事结果", null });
        Assert.IsFalse(Field<UnityEngine.UI.ScrollRect>(overlay, "consequenceScrollRect").gameObject.activeInHierarchy);
        var normal = ScriptableObject.CreateInstance<MapEvent_SO>(); temporary.Add(normal);
        normal.options.Add(new MapEventOptionData { consequenceDescription = "正常后果预览" });
        overlay.Show(Tile(normal), normal, null);
        Assert.IsTrue(Field<UnityEngine.UI.ScrollRect>(overlay, "consequenceScrollRect").gameObject.activeInHierarchy);
    }

    private static MapEvent_SO Event(string filename) => AssetDatabase.LoadAssetAtPath<MapEvent_SO>(Folder + filename);
    private BoardTileData Tile(MapEvent_SO room) => new BoardTileData { tileType = MapTileType.Event, assignedEvent = room, sourceSpecialDungeon = Dungeon };
    private static RunState State()
    {
        var state = new RunState { money = 100 };
        state.partyMembers.Add(new RunPartyMemberState { maxHP = 100, currentHP = 100 }); return state;
    }
    private static MapEventBuiltInEffectExecutionResult Execute(RunState state, BoardTileData tile, MapEventOptionData option)
    {
        Assert.IsTrue(MapEventOptionService.TryExecute(new MapEventBuiltInEffectExecutionRequest { runState = state, currentEventTile = tile, optionData = option },
            tile.assignedEvent, null, out MapEventBuiltInEffectExecutionResult result), result.message);
        return result;
    }
    private static object SaveCall(string name, params object[] args) => typeof(MainMenuRunSaveSystem)
        .GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, args);
    private static T Field<T>(object target, string name) => (T)target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(target);
}
