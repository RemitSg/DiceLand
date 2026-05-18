# Special Dungeon Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add event-triggered special dungeons: linear temporary maps with configured battles, shops, treasures, events, and exits that return to the originating main-map event tile.

**Architecture:** Add a data-first `SpecialDungeon_SO`, focused builder/services for dungeon boards and fixed treasure/shop rewards, then integrate `RunGameManager` with a temporary dungeon context stored in `MapRunSnapshot`. Existing battle, merchant, event, and relic-choice flows are reused wherever possible; new logic is isolated behind special dungeon services to avoid bloating the map manager further.

**Tech Stack:** Unity C#, ScriptableObject assets, existing map runtime (`RunState`, `BoardTileData`, `BoardManager`, `MapRunRuntime`), NUnit EditMode tests via `Assembly-CSharp-Editor.csproj`, build verification with `dotnet build MobileGame.sln --no-restore`.

---

## File Structure

- Create `Assets/Scripts/ScriptableObject/SpecialDungeon_SO.cs`: ScriptableObject definitions for dungeon routes, merchant offers, and fixed treasure options.
- Create `Assets/Scripts/Shared/SpecialDungeonBoardBuilder.cs`: Pure builder that converts `SpecialDungeon_SO` into linear `BoardTileData` with exits and configured tile state.
- Create `Assets/Scripts/Shared/SpecialDungeonTreasureService.cs`: Converts and applies fixed dungeon treasure options, including fixed relic bags.
- Create `Assets/Scripts/Shared/SpecialDungeonRuntimeContext.cs`: Serializable runtime context for active dungeon, main-map snapshot, return coordinate, and delayed calamities.
- Modify `Assets/Scripts/MapScene/Manager/BoardTileData.cs`: Add `MapTileType.SpecialDungeonExit` plus dungeon-specific optional tile data fields.
- Modify `Assets/Scripts/ScriptableObject/MapEvent_SO.cs`: Add `StartSpecialDungeon` built-in effect type and `SpecialDungeon_SO specialDungeon`.
- Modify `Assets/Scripts/Shared/MapEventBuiltInEffectExecutorService.cs`: Validate/start special dungeon effect and expose pending dungeon result.
- Modify `Assets/Scripts/Shared/MapEventEffectSequenceBuilder.cs`: Ensure primary/additional built-in effect conversion includes `specialDungeon`.
- Modify `Assets/Scripts/MapScene/Manager/RunGameManager.cs`: Enter/exit dungeon, route special dungeon tiles, defer calamity presentation, and use fixed dungeon treasure/shop behavior.
- Modify `Assets/Scripts/MapScene/Manager/MapRunRuntime.cs`: Snapshot active special dungeon context and apply battle/rest-like results while inside dungeon.
- Modify `Assets/Scripts/MainMenu/MainMenuRunSaveSystem.cs`: Serialize/deserialize dungeon context and special dungeon tile data.
- Modify `Assets/Scripts/Shared/RunSaveAssetCatalogBuilder.cs`: Include `SpecialDungeon_SO` referenced assets in save catalog.
- Modify `Assets/Scripts/MapScene/Manager/BoardManager.cs`: Add icon/color fallback for `SpecialDungeonExit`.
- Modify `Assets/Scripts/MapScene/Manager/TileResolver.cs`: Resolve exit tiles and avoid treating them as empty.
- Modify `Assets/Scripts/MapScene/Controller/MapHUD.cs`: Display exit label/description.
- Modify `Assets/Scripts/MapScene/Controller/MapTreasureOverlay.cs` only if fixed treasure choices need a display model beyond existing `TreasureRewardChoice`.
- Add tests under `Assets/Tests/EditMode/Editor/`:
  - `SpecialDungeonBoardBuilderTests.cs`
  - `SpecialDungeonTreasureServiceTests.cs`
  - `SpecialDungeonRuntimeContextTests.cs`
  - `SpecialDungeonEventEffectTests.cs`
  - Extend `MapHUDTilePresentationTests.cs` and `MapMovementServiceTests` if present; otherwise add focused tests.

---

### Task 1: Add Special Dungeon Data Model

**Files:**
- Create: `Assets/Scripts/ScriptableObject/SpecialDungeon_SO.cs`
- Create: `Assets/Scripts/ScriptableObject/SpecialDungeon_SO.cs.meta`
- Modify: `Assembly-CSharp.csproj`
- Test: `Assets/Tests/EditMode/Editor/SpecialDungeonDataModelTests.cs`
- Modify: `Assembly-CSharp-Editor.csproj`

- [ ] **Step 1: Write failing tests for valid entries**

Create `Assets/Tests/EditMode/Editor/SpecialDungeonDataModelTests.cs`:

```csharp
using NUnit.Framework;
using UnityEngine;

public class SpecialDungeonDataModelTests
{
    [Test]
    public void GetValidEntries_FiltersNullEntriesAndUnsupportedEmptyTiles()
    {
        SpecialDungeon_SO dungeon = ScriptableObject.CreateInstance<SpecialDungeon_SO>();
        dungeon.entries.Add(null);
        dungeon.entries.Add(new SpecialDungeonTileEntry { tileType = MapTileType.Empty });
        dungeon.entries.Add(new SpecialDungeonTileEntry { tileType = MapTileType.Battle });

        Assert.That(dungeon.GetValidEntries(), Has.Count.EqualTo(1));
        Assert.AreEqual(MapTileType.Battle, dungeon.GetValidEntries()[0].tileType);

        Object.DestroyImmediate(dungeon);
    }

    [Test]
    public void TreasureOption_IsAvailableForConfiguredMoneyBlueprintRelicAndRelicBag()
    {
        SpecialDungeonTreasureOptionEntry money = new SpecialDungeonTreasureOptionEntry
        {
            rewardType = SpecialDungeonTreasureRewardType.Money,
            moneyAmount = 25
        };

        Assert.IsTrue(money.IsConfigured);
    }
}
```

- [ ] **Step 2: Run compile and confirm failure**

Run: `dotnet build Assembly-CSharp-Editor.csproj --no-restore`

Expected: FAIL with missing `SpecialDungeon_SO`, `SpecialDungeonTileEntry`, and `SpecialDungeonTreasureOptionEntry`.

- [ ] **Step 3: Implement ScriptableObject model**

Create `Assets/Scripts/ScriptableObject/SpecialDungeon_SO.cs`:

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;

public enum SpecialDungeonTreasureRewardType
{
    Money = 0,
    Blueprint = 1,
    Relic = 2,
    RelicBag = 3
}

[Serializable]
public sealed class SpecialDungeonMerchantOfferEntry
{
    public MerchantItemType itemType;
    [Min(0)] public int price;
    public LootItem_SO lootItem;
    public Weapon_SO weaponData;
    public CraftingBlueprint_SO blueprintData;
    public Relic_SO relicData;

    public bool IsConfigured
    {
        get
        {
            switch (itemType)
            {
                case MerchantItemType.Loot:
                    return lootItem != null;
                case MerchantItemType.Weapon:
                    return weaponData != null;
                case MerchantItemType.Blueprint:
                    return blueprintData != null;
                case MerchantItemType.Relic:
                    return relicData != null;
                default:
                    return false;
            }
        }
    }
}

[Serializable]
public sealed class SpecialDungeonTreasureOptionEntry
{
    public string title = "Reward";
    [TextArea(2, 5)] public string description;
    public SpecialDungeonTreasureRewardType rewardType = SpecialDungeonTreasureRewardType.Money;
    public Sprite iconOverride;
    [Min(0)] public int moneyAmount;
    public CraftingBlueprint_SO blueprintData;
    public Relic_SO relicData;
    public RelicTable_SO relicTable;
    public LootRarity relicBagRarity = LootRarity.Common;
    [Min(1)] public int relicBagChoiceCount = 3;

    public bool IsConfigured
    {
        get
        {
            switch (rewardType)
            {
                case SpecialDungeonTreasureRewardType.Money:
                    return moneyAmount >= 0;
                case SpecialDungeonTreasureRewardType.Blueprint:
                    return blueprintData != null;
                case SpecialDungeonTreasureRewardType.Relic:
                    return relicData != null;
                case SpecialDungeonTreasureRewardType.RelicBag:
                    return relicTable != null && relicTable.IsValid && relicBagChoiceCount > 0;
                default:
                    return false;
            }
        }
    }
}

[Serializable]
public sealed class SpecialDungeonTileEntry
{
    public MapTileType tileType = MapTileType.Battle;
    public string previewTag;
    public EnemyGroup_SO enemyGroup;
    public List<SpecialDungeonMerchantOfferEntry> merchantOffers = new List<SpecialDungeonMerchantOfferEntry>();
    public List<SpecialDungeonTreasureOptionEntry> treasureOptions = new List<SpecialDungeonTreasureOptionEntry>();
    public MapEvent_SO eventData;

    public bool IsSupportedTileType
    {
        get
        {
            return tileType == MapTileType.Battle
                || tileType == MapTileType.Elite
                || tileType == MapTileType.Merchant
                || tileType == MapTileType.Treasure
                || tileType == MapTileType.Event;
        }
    }
}

[CreateAssetMenu(fileName = "SpecialDungeon", menuName = "Events/Special Dungeon")]
public class SpecialDungeon_SO : ScriptableObject
{
    public string dungeonId = "SpecialDungeon";
    public string dungeonName = "Special Dungeon";
    [TextArea(3, 8)] public string description;
    public List<SpecialDungeonTileEntry> entries = new List<SpecialDungeonTileEntry>();

    public string DisplayName => string.IsNullOrWhiteSpace(dungeonName) ? name : dungeonName;

    public IReadOnlyList<SpecialDungeonTileEntry> GetValidEntries()
    {
        if (entries == null || entries.Count == 0)
            return Array.Empty<SpecialDungeonTileEntry>();

        List<SpecialDungeonTileEntry> valid = new List<SpecialDungeonTileEntry>();
        for (int i = 0; i < entries.Count; i++)
        {
            SpecialDungeonTileEntry entry = entries[i];
            if (entry != null && entry.IsSupportedTileType)
                valid.Add(entry);
        }

        return valid;
    }

    private void OnValidate()
    {
        if (entries == null)
            entries = new List<SpecialDungeonTileEntry>();

        for (int i = 0; i < entries.Count; i++)
        {
            SpecialDungeonTileEntry entry = entries[i];
            if (entry == null)
                continue;

            if (entry.merchantOffers == null)
                entry.merchantOffers = new List<SpecialDungeonMerchantOfferEntry>();
            if (entry.treasureOptions == null)
                entry.treasureOptions = new List<SpecialDungeonTreasureOptionEntry>();

            for (int optionIndex = 0; optionIndex < entry.treasureOptions.Count; optionIndex++)
            {
                SpecialDungeonTreasureOptionEntry option = entry.treasureOptions[optionIndex];
                if (option != null)
                    option.relicBagChoiceCount = Mathf.Max(1, option.relicBagChoiceCount);
            }

            for (int offerIndex = 0; offerIndex < entry.merchantOffers.Count; offerIndex++)
            {
                SpecialDungeonMerchantOfferEntry offer = entry.merchantOffers[offerIndex];
                if (offer != null)
                    offer.price = Mathf.Max(0, offer.price);
            }
        }
    }
}
```

Create `Assets/Scripts/ScriptableObject/SpecialDungeon_SO.cs.meta` with a new Unity meta GUID.

Modify `Assembly-CSharp.csproj`:

```xml
<Compile Include="Assets\Scripts\ScriptableObject\SpecialDungeon_SO.cs" />
```

Modify `Assembly-CSharp-Editor.csproj`:

```xml
<Compile Include="Assets\Tests\EditMode\Editor\SpecialDungeonDataModelTests.cs" />
```

- [ ] **Step 4: Run tests/build**

Run: `dotnet build Assembly-CSharp-Editor.csproj --no-restore`

Expected: PASS compile, 0 errors.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/ScriptableObject/SpecialDungeon_SO.cs Assets/Scripts/ScriptableObject/SpecialDungeon_SO.cs.meta Assets/Tests/EditMode/Editor/SpecialDungeonDataModelTests.cs Assembly-CSharp.csproj Assembly-CSharp-Editor.csproj
git commit -m "feat: add special dungeon data model"
```

---

### Task 2: Build Linear Special Dungeon Boards

**Files:**
- Create: `Assets/Scripts/Shared/SpecialDungeonBoardBuilder.cs`
- Create: `Assets/Scripts/Shared/SpecialDungeonBoardBuilder.cs.meta`
- Modify: `Assets/Scripts/MapScene/Manager/BoardTileData.cs`
- Modify: `Assembly-CSharp.csproj`
- Test: `Assets/Tests/EditMode/Editor/SpecialDungeonBoardBuilderTests.cs`
- Modify: `Assembly-CSharp-Editor.csproj`

- [ ] **Step 1: Write failing board builder tests**

Create `Assets/Tests/EditMode/Editor/SpecialDungeonBoardBuilderTests.cs`:

```csharp
using NUnit.Framework;
using UnityEngine;

public class SpecialDungeonBoardBuilderTests
{
    [Test]
    public void BuildBoard_AddsLeftExitSpawnConfiguredTilesAndRightExit()
    {
        SpecialDungeon_SO dungeon = ScriptableObject.CreateInstance<SpecialDungeon_SO>();
        dungeon.entries.Add(new SpecialDungeonTileEntry { tileType = MapTileType.Battle, previewTag = "Guard" });
        dungeon.entries.Add(new SpecialDungeonTileEntry { tileType = MapTileType.Event });

        SpecialDungeonBoardBuildResult result = SpecialDungeonBoardBuilder.Build(dungeon, 123);

        Assert.AreEqual(5, result.boardSize);
        Assert.AreEqual(new Vector2Int(1, 1), result.spawnCoord);
        Assert.AreEqual(new Vector2Int(4, 1), result.rightExitCoord);
        Assert.AreEqual(MapTileType.SpecialDungeonExit, result.tiles[0].tileType);
        Assert.AreEqual(MapTileType.Empty, result.tiles[1].tileType);
        Assert.IsTrue(result.tiles[1].isSpawnTile);
        Assert.AreEqual(MapTileType.Battle, result.tiles[2].tileType);
        Assert.AreEqual("Guard", result.tiles[2].previewTag);
        Assert.AreEqual(MapTileType.Event, result.tiles[3].tileType);
        Assert.AreEqual(MapTileType.SpecialDungeonExit, result.tiles[4].tileType);

        Object.DestroyImmediate(dungeon);
    }

    [Test]
    public void BuildBoard_MapsMerchantOffersAndTreasureOptionsToTileData()
    {
        SpecialDungeon_SO dungeon = ScriptableObject.CreateInstance<SpecialDungeon_SO>();
        dungeon.entries.Add(new SpecialDungeonTileEntry
        {
            tileType = MapTileType.Merchant,
            merchantOffers =
            {
                new SpecialDungeonMerchantOfferEntry { itemType = MerchantItemType.Loot, price = 7 }
            }
        });
        dungeon.entries.Add(new SpecialDungeonTileEntry
        {
            tileType = MapTileType.Treasure,
            treasureOptions =
            {
                new SpecialDungeonTreasureOptionEntry { rewardType = SpecialDungeonTreasureRewardType.Money, moneyAmount = 20 }
            }
        });

        SpecialDungeonBoardBuildResult result = SpecialDungeonBoardBuilder.Build(dungeon, 123);

        Assert.That(result.tiles[2].merchantOffers, Has.Count.EqualTo(1));
        Assert.AreEqual(7, result.tiles[2].merchantOffers[0].price);
        Assert.That(result.tiles[3].specialDungeonTreasureOptions, Has.Count.EqualTo(1));
        Assert.AreEqual(20, result.tiles[3].specialDungeonTreasureOptions[0].moneyAmount);

        Object.DestroyImmediate(dungeon);
    }
}
```

- [ ] **Step 2: Run compile and confirm failure**

Run: `dotnet build Assembly-CSharp-Editor.csproj --no-restore`

Expected: FAIL with missing `SpecialDungeonBoardBuilder`, `SpecialDungeonBoardBuildResult`, `MapTileType.SpecialDungeonExit`, and `specialDungeonTreasureOptions`.

- [ ] **Step 3: Extend tile data**

Modify `Assets/Scripts/MapScene/Manager/BoardTileData.cs`:

```csharp
public enum MapTileType
{
    Empty = 0,
    Battle = 1,
    Elite = 2,
    Event = 3,
    Merchant = 4,
    Forge = 5,
    Rest = 6,
    Key = 7,
    Boss = 8,
    Treasure = 9,
    SpecialDungeonExit = 10
}
```

Add fields to `BoardTileData`:

```csharp
public SpecialDungeon_SO sourceSpecialDungeon;
public int specialDungeonEntryIndex = -1;
public List<SpecialDungeonTreasureOptionEntry> specialDungeonTreasureOptions = new List<SpecialDungeonTreasureOptionEntry>();
```

Update `Clone()`:

```csharp
sourceSpecialDungeon = sourceSpecialDungeon,
specialDungeonEntryIndex = specialDungeonEntryIndex,
specialDungeonTreasureOptions = CloneSpecialDungeonTreasureOptions()
```

Add:

```csharp
private List<SpecialDungeonTreasureOptionEntry> CloneSpecialDungeonTreasureOptions()
{
    List<SpecialDungeonTreasureOptionEntry> clone = new List<SpecialDungeonTreasureOptionEntry>();
    if (specialDungeonTreasureOptions == null)
        return clone;

    for (int i = 0; i < specialDungeonTreasureOptions.Count; i++)
    {
        SpecialDungeonTreasureOptionEntry option = specialDungeonTreasureOptions[i];
        if (option != null)
            clone.Add(option);
    }

    return clone;
}
```

- [ ] **Step 4: Implement board builder**

Create `Assets/Scripts/Shared/SpecialDungeonBoardBuilder.cs`:

```csharp
using System.Collections.Generic;
using UnityEngine;

public sealed class SpecialDungeonBoardBuildResult
{
    public int boardSize;
    public Vector2Int leftExitCoord;
    public Vector2Int spawnCoord;
    public Vector2Int rightExitCoord;
    public List<BoardTileData> tiles = new List<BoardTileData>();
}

public static class SpecialDungeonBoardBuilder
{
    public static SpecialDungeonBoardBuildResult Build(SpecialDungeon_SO dungeon, int seed)
    {
        IReadOnlyList<SpecialDungeonTileEntry> entries = dungeon != null
            ? dungeon.GetValidEntries()
            : System.Array.Empty<SpecialDungeonTileEntry>();
        int totalTiles = entries.Count + 3;
        int row = Mathf.Max(1, totalTiles / 2);

        SpecialDungeonBoardBuildResult result = new SpecialDungeonBoardBuildResult
        {
            boardSize = totalTiles,
            leftExitCoord = new Vector2Int(0, row),
            spawnCoord = new Vector2Int(1, row),
            rightExitCoord = new Vector2Int(totalTiles - 1, row)
        };

        result.tiles.Add(CreateExit(result.leftExitCoord, dungeon, seed));
        result.tiles.Add(new BoardTileData
        {
            coord = result.spawnCoord,
            tileType = MapTileType.Empty,
            dangerBand = MapDangerBand.Outer,
            isVisited = true,
            isResolved = true,
            isSpawnTile = true,
            encounterSeed = seed ^ 0x51A7,
            previewTag = "副本起点",
            sourceSpecialDungeon = dungeon,
            specialDungeonEntryIndex = -1
        });

        for (int i = 0; i < entries.Count; i++)
            result.tiles.Add(CreateConfiguredTile(entries[i], dungeon, i, new Vector2Int(i + 2, row), seed));

        result.tiles.Add(CreateExit(result.rightExitCoord, dungeon, seed ^ 0x7E17));
        return result;
    }

    private static BoardTileData CreateExit(Vector2Int coord, SpecialDungeon_SO dungeon, int seed)
    {
        return new BoardTileData
        {
            coord = coord,
            tileType = MapTileType.SpecialDungeonExit,
            dangerBand = MapDangerBand.Outer,
            isResolved = true,
            encounterSeed = seed,
            previewTag = "副本出口",
            sourceSpecialDungeon = dungeon,
            specialDungeonEntryIndex = -1
        };
    }

    private static BoardTileData CreateConfiguredTile(
        SpecialDungeonTileEntry entry,
        SpecialDungeon_SO dungeon,
        int entryIndex,
        Vector2Int coord,
        int seed)
    {
        BoardTileData tile = new BoardTileData
        {
            coord = coord,
            tileType = entry.tileType,
            dangerBand = ResolveDangerBand(entryIndex),
            encounterSeed = seed ^ ((entryIndex + 1) * 397),
            previewTag = entry.previewTag,
            assignedEnemyGroup = entry.enemyGroup,
            assignedEvent = entry.eventData,
            sourceSpecialDungeon = dungeon,
            specialDungeonEntryIndex = entryIndex
        };

        if (entry.merchantOffers != null)
        {
            for (int i = 0; i < entry.merchantOffers.Count; i++)
            {
                SpecialDungeonMerchantOfferEntry offer = entry.merchantOffers[i];
                if (offer == null)
                    continue;

                tile.merchantOffers.Add(new MerchantOfferState
                {
                    itemType = offer.itemType,
                    lootItem = offer.lootItem,
                    weaponData = offer.weaponData,
                    blueprintData = offer.blueprintData,
                    relicData = offer.relicData,
                    price = Mathf.Max(0, offer.price)
                });
            }
        }

        if (entry.treasureOptions != null)
        {
            for (int i = 0; i < entry.treasureOptions.Count; i++)
            {
                SpecialDungeonTreasureOptionEntry option = entry.treasureOptions[i];
                if (option != null)
                    tile.specialDungeonTreasureOptions.Add(option);
            }
        }

        return tile;
    }

    private static MapDangerBand ResolveDangerBand(int entryIndex)
    {
        if (entryIndex <= 1)
            return MapDangerBand.Outer;
        if (entryIndex <= 3)
            return MapDangerBand.Mid;
        if (entryIndex <= 5)
            return MapDangerBand.Inner;
        return MapDangerBand.Core;
    }
}
```

Add compile includes for new files and test.

- [ ] **Step 5: Run tests/build**

Run: `dotnet build Assembly-CSharp-Editor.csproj --no-restore`

Expected: PASS compile, 0 errors.

- [ ] **Step 6: Commit**

```bash
git add Assets/Scripts/Shared/SpecialDungeonBoardBuilder.cs Assets/Scripts/Shared/SpecialDungeonBoardBuilder.cs.meta Assets/Scripts/MapScene/Manager/BoardTileData.cs Assets/Tests/EditMode/Editor/SpecialDungeonBoardBuilderTests.cs Assembly-CSharp.csproj Assembly-CSharp-Editor.csproj
git commit -m "feat: build special dungeon boards"
```

---

### Task 3: Add Fixed Special Dungeon Treasure Rewards

**Files:**
- Create: `Assets/Scripts/Shared/SpecialDungeonTreasureService.cs`
- Create: `Assets/Scripts/Shared/SpecialDungeonTreasureService.cs.meta`
- Modify: `Assets/Scripts/MapScene/Controller/MapTreasureOverlay.cs`
- Modify: `Assets/Scripts/MapScene/Manager/RunGameManager.cs`
- Modify: `Assembly-CSharp.csproj`
- Test: `Assets/Tests/EditMode/Editor/SpecialDungeonTreasureServiceTests.cs`
- Modify: `Assembly-CSharp-Editor.csproj`

- [ ] **Step 1: Write failing treasure service tests**

Create `Assets/Tests/EditMode/Editor/SpecialDungeonTreasureServiceTests.cs`:

```csharp
using NUnit.Framework;
using UnityEngine;

public class SpecialDungeonTreasureServiceTests
{
    [Test]
    public void BuildChoices_UsesConfiguredMoneyBlueprintAndRelicOptions()
    {
        BoardTileData tile = new BoardTileData { tileType = MapTileType.Treasure };
        tile.specialDungeonTreasureOptions.Add(new SpecialDungeonTreasureOptionEntry
        {
            title = "Coins",
            rewardType = SpecialDungeonTreasureRewardType.Money,
            moneyAmount = 12
        });

        var choices = SpecialDungeonTreasureService.BuildChoices(tile);

        Assert.That(choices, Has.Count.EqualTo(1));
        Assert.AreEqual(TreasureRewardType.Wealth, choices[0].rewardType);
        Assert.AreEqual("Coins", choices[0].title);
        Assert.AreEqual(12, choices[0].moneyAmount);
    }

    [Test]
    public void ApplyChoice_FixedRelicAddsRelicAndReturnsMessage()
    {
        Relic_SO relic = ScriptableObject.CreateInstance<Relic_SO>();
        RunState runState = new RunState();
        SpecialDungeonTreasureOptionEntry option = new SpecialDungeonTreasureOptionEntry
        {
            rewardType = SpecialDungeonTreasureRewardType.Relic,
            relicData = relic
        };

        TreasureRewardResult result = SpecialDungeonTreasureService.ApplyChoice(runState, option, 10);

        Assert.IsTrue(result.success);
        Assert.That(runState.relicInventory, Has.Count.EqualTo(1));
        Assert.AreSame(relic, runState.relicInventory[0].relicData);

        Object.DestroyImmediate(relic);
    }
}
```

- [ ] **Step 2: Run compile and confirm failure**

Run: `dotnet build Assembly-CSharp-Editor.csproj --no-restore`

Expected: FAIL with missing `SpecialDungeonTreasureService`.

- [ ] **Step 3: Implement fixed treasure service**

Create `Assets/Scripts/Shared/SpecialDungeonTreasureService.cs`:

```csharp
using System.Collections.Generic;
using UnityEngine;

public static class SpecialDungeonTreasureService
{
    public static IReadOnlyList<TreasureRewardChoice> BuildChoices(BoardTileData tile)
    {
        List<TreasureRewardChoice> choices = new List<TreasureRewardChoice>();
        if (tile == null || tile.specialDungeonTreasureOptions == null)
            return choices;

        for (int i = 0; i < tile.specialDungeonTreasureOptions.Count; i++)
        {
            SpecialDungeonTreasureOptionEntry option = tile.specialDungeonTreasureOptions[i];
            if (option == null)
                continue;

            choices.Add(BuildChoice(option));
        }

        return choices;
    }

    public static TreasureRewardResult ApplyChoice(RunState runState, SpecialDungeonTreasureOptionEntry option, int seed)
    {
        if (runState == null || option == null)
            return Fail("特殊副本宝藏领取失败。");
        if (!option.IsConfigured)
            return Fail("该宝藏选项配置不完整。");

        switch (option.rewardType)
        {
            case SpecialDungeonTreasureRewardType.Money:
                int money = Mathf.Max(0, option.moneyAmount);
                runState.AddMoney(money);
                return new TreasureRewardResult { success = true, moneyAmount = money, message = $"获得 {money} 金钱。" };
            case SpecialDungeonTreasureRewardType.Blueprint:
                runState.AddBlueprint(option.blueprintData, 1);
                return new TreasureRewardResult { success = true, blueprint = option.blueprintData, message = $"获得图纸：{option.blueprintData.DisplayName}。" };
            case SpecialDungeonTreasureRewardType.Relic:
                runState.AddRelic(option.relicData, 1);
                return new TreasureRewardResult { success = true, relic = option.relicData, message = $"获得遗物：{option.relicData.DisplayName}。" };
            case SpecialDungeonTreasureRewardType.RelicBag:
                return BuildRelicBagChoice(runState, option, seed);
            default:
                return Fail("未知的特殊副本宝藏奖励。");
        }
    }

    public static SpecialDungeonTreasureOptionEntry ResolveOption(BoardTileData tile, TreasureRewardChoice choice)
    {
        int index = choice != null ? choice.specialDungeonOptionIndex : -1;
        if (tile == null || tile.specialDungeonTreasureOptions == null || index < 0 || index >= tile.specialDungeonTreasureOptions.Count)
            return null;

        return tile.specialDungeonTreasureOptions[index];
    }

    private static TreasureRewardChoice BuildChoice(SpecialDungeonTreasureOptionEntry option)
    {
        TreasureRewardChoice choice = new TreasureRewardChoice
        {
            title = string.IsNullOrWhiteSpace(option.title) ? option.rewardType.ToString() : option.title,
            description = option.description,
            isAvailable = option.IsConfigured,
            unavailableReason = option.IsConfigured ? string.Empty : "该宝藏选项配置不完整。"
        };

        switch (option.rewardType)
        {
            case SpecialDungeonTreasureRewardType.Money:
                choice.rewardType = TreasureRewardType.Wealth;
                choice.moneyAmount = Mathf.Max(0, option.moneyAmount);
                if (string.IsNullOrWhiteSpace(choice.description))
                    choice.description = $"获得 {choice.moneyAmount} 金钱";
                break;
            case SpecialDungeonTreasureRewardType.Blueprint:
                choice.rewardType = TreasureRewardType.Knowledge;
                choice.blueprint = option.blueprintData;
                if (string.IsNullOrWhiteSpace(choice.description))
                    choice.description = option.blueprintData != null ? $"获得图纸：{option.blueprintData.DisplayName}" : "获得图纸";
                break;
            case SpecialDungeonTreasureRewardType.Relic:
                choice.rewardType = TreasureRewardType.Power;
                choice.relic = option.relicData;
                if (string.IsNullOrWhiteSpace(choice.description))
                    choice.description = option.relicData != null ? $"获得遗物：{option.relicData.DisplayName}" : "获得遗物";
                break;
            case SpecialDungeonTreasureRewardType.RelicBag:
                choice.rewardType = TreasureRewardType.Power;
                choice.relicBagRarity = option.relicBagRarity;
                if (string.IsNullOrWhiteSpace(choice.description))
                    choice.description = $"打开{TreasureRewardService.GetRarityLabel(option.relicBagRarity)}遗物袋";
                break;
        }

        return choice;
    }

    private static TreasureRewardResult BuildRelicBagChoice(RunState runState, SpecialDungeonTreasureOptionEntry option, int seed)
    {
        HashSet<Relic_SO> ownedRelics = BattleRelicInventorySetBuilder.BuildOwnedRelicSet(runState != null ? runState.relicInventory : null);
        System.Random rng = new System.Random(seed ^ 0x5D6E7A);
        if (!BattleEliteRelicChoiceBuilderService.TryBuildFromRelicsWithRarity(
                out BattleSettlementRelicChoiceData choiceData,
                option.relicTable.Relics,
                option.relicBagRarity,
                rng,
                ownedRelics,
                Mathf.Max(1, option.relicBagChoiceCount)))
        {
            return Fail("未找到可从遗物袋中获得的遗物。");
        }

        choiceData.title = $"{TreasureRewardService.GetRarityLabel(option.relicBagRarity)}遗物袋：选择 1 个遗物";
        choiceData.requiredMessage = "请先从遗物袋中选择一个遗物。";
        return new TreasureRewardResult
        {
            success = true,
            relicChoiceData = choiceData,
            message = "打开遗物袋，请选择 1 个遗物。"
        };
    }

    private static TreasureRewardResult Fail(string message)
    {
        return new TreasureRewardResult { success = false, message = message };
    }
}
```

Modify `TreasureRewardChoice` in `Assets/Scripts/Shared/TreasureRewardService.cs`:

```csharp
public int specialDungeonOptionIndex = -1;
```

Make `GetRarityLabel` public if it is currently private:

```csharp
public static string GetRarityLabel(LootRarity rarity)
```

Set `specialDungeonOptionIndex` when building choices in `BuildChoices`.

- [ ] **Step 4: Route special dungeon treasure in RunGameManager**

In `OpenTreasurePanel`, choose fixed choices when active tile has configured dungeon options:

```csharp
IReadOnlyList<TreasureRewardChoice> choices = IsInSpecialDungeon && tile.specialDungeonTreasureOptions != null && tile.specialDungeonTreasureOptions.Count > 0
    ? SpecialDungeonTreasureService.BuildChoices(tile)
    : TreasureRewardService.BuildChoices(treasureRewardTable, tile.encounterSeed);
```

In `HandleTreasureOptionSelected`, resolve dungeon option first:

```csharp
if (IsInSpecialDungeon && tile.specialDungeonTreasureOptions != null && tile.specialDungeonTreasureOptions.Count > 0)
{
    SpecialDungeonTreasureOptionEntry fixedOption = SpecialDungeonTreasureService.ResolveOption(tile, choice);
    result = SpecialDungeonTreasureService.ApplyChoice(runState, fixedOption, tile.encounterSeed);
}
else
{
    result = TreasureRewardService.ApplyChoice(runState, treasureRewardTable, choice, tile.encounterSeed);
}
```

- [ ] **Step 5: Run tests/build**

Run: `dotnet build Assembly-CSharp-Editor.csproj --no-restore`

Expected: PASS compile, 0 errors.

- [ ] **Step 6: Commit**

```bash
git add Assets/Scripts/Shared/SpecialDungeonTreasureService.cs Assets/Scripts/Shared/SpecialDungeonTreasureService.cs.meta Assets/Scripts/Shared/TreasureRewardService.cs Assets/Scripts/MapScene/Manager/RunGameManager.cs Assets/Tests/EditMode/Editor/SpecialDungeonTreasureServiceTests.cs Assembly-CSharp.csproj Assembly-CSharp-Editor.csproj
git commit -m "feat: add fixed special dungeon treasure rewards"
```

---

### Task 4: Add Runtime Context And Snapshot Support

**Files:**
- Create: `Assets/Scripts/Shared/SpecialDungeonRuntimeContext.cs`
- Create: `Assets/Scripts/Shared/SpecialDungeonRuntimeContext.cs.meta`
- Modify: `Assets/Scripts/MapScene/Manager/MapRunRuntime.cs`
- Modify: `Assets/Scripts/MainMenu/MainMenuRunSaveSystem.cs`
- Modify: `Assets/Scripts/Shared/RunSaveAssetCatalogBuilder.cs`
- Modify: `Assembly-CSharp.csproj`
- Test: `Assets/Tests/EditMode/Editor/SpecialDungeonRuntimeContextTests.cs`
- Modify: `Assembly-CSharp-Editor.csproj`

- [ ] **Step 1: Write failing context tests**

Create `Assets/Tests/EditMode/Editor/SpecialDungeonRuntimeContextTests.cs`:

```csharp
using NUnit.Framework;
using UnityEngine;

public class SpecialDungeonRuntimeContextTests
{
    [Test]
    public void Clone_CopiesMainMapSnapshotReturnCoordAndDelayedCalamities()
    {
        SpecialDungeonRuntimeContext context = new SpecialDungeonRuntimeContext
        {
            isActive = true,
            activeDungeonId = "d1",
            returnEventCoord = new Vector2Int(3, 4),
            mainMapSnapshot = new MapRunSnapshot { sceneName = "Map" }
        };
        context.deferredCalamityTiers.Add(2);

        SpecialDungeonRuntimeContext clone = context.Clone();

        Assert.IsTrue(clone.isActive);
        Assert.AreEqual("d1", clone.activeDungeonId);
        Assert.AreEqual(new Vector2Int(3, 4), clone.returnEventCoord);
        Assert.AreEqual(1, clone.deferredCalamityTiers.Count);
        Assert.AreNotSame(context.mainMapSnapshot, clone.mainMapSnapshot);
    }
}
```

- [ ] **Step 2: Run compile and confirm failure**

Run: `dotnet build Assembly-CSharp-Editor.csproj --no-restore`

Expected: FAIL with missing `SpecialDungeonRuntimeContext`.

- [ ] **Step 3: Implement runtime context**

Create `Assets/Scripts/Shared/SpecialDungeonRuntimeContext.cs`:

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class SpecialDungeonRuntimeContext
{
    public bool isActive;
    public string activeDungeonId;
    public SpecialDungeon_SO activeDungeon;
    public Vector2Int returnEventCoord;
    public MapRunSnapshot mainMapSnapshot;
    public List<int> deferredCalamityTiers = new List<int>();

    public SpecialDungeonRuntimeContext Clone()
    {
        return new SpecialDungeonRuntimeContext
        {
            isActive = isActive,
            activeDungeonId = activeDungeonId,
            activeDungeon = activeDungeon,
            returnEventCoord = returnEventCoord,
            mainMapSnapshot = mainMapSnapshot != null ? mainMapSnapshot.Clone() : null,
            deferredCalamityTiers = deferredCalamityTiers != null ? new List<int>(deferredCalamityTiers) : new List<int>()
        };
    }
}
```

Modify `MapRunSnapshot`:

```csharp
public SpecialDungeonRuntimeContext specialDungeonContext;
```

In `Clone()`:

```csharp
specialDungeonContext = specialDungeonContext != null ? specialDungeonContext.Clone() : null
```

Modify `MapRunRuntime.SaveSnapshot` to accept optional context:

```csharp
public static void SaveSnapshot(
    string sceneName,
    RunState runState,
    BoardManager boardManager,
    ActionPointSystem actionPointSystem,
    IEnumerable<int> pendingCalamityTiers = null,
    bool suppressOnePendingCalamityOnActionPointRestore = false,
    SpecialDungeonRuntimeContext specialDungeonContext = null)
```

Set:

```csharp
specialDungeonContext = specialDungeonContext != null ? specialDungeonContext.Clone() : null
```

- [ ] **Step 4: Add save catalog hooks**

In `RunSaveAssetCatalogBuilder.AddEventEffectAssets`, add:

```csharp
AddSpecialDungeonAssets(catalog, effectData.specialDungeon);
```

Add method:

```csharp
private static void AddSpecialDungeonAssets(MainMenuSaveAssetCatalog catalog, SpecialDungeon_SO dungeon)
{
    if (catalog == null || dungeon == null)
        return;

    IReadOnlyList<SpecialDungeonTileEntry> entries = dungeon.GetValidEntries();
    for (int i = 0; i < entries.Count; i++)
    {
        SpecialDungeonTileEntry entry = entries[i];
        if (entry == null)
            continue;

        AddEnemyGroup(catalog, entry.enemyGroup);
        catalog.Add(entry.eventData);
        AddSpecialDungeonMerchantAssets(catalog, entry.merchantOffers);
        AddSpecialDungeonTreasureAssets(catalog, entry.treasureOptions);
    }
}
```

Add helper methods for merchant and treasure assets using existing `catalog.Add(...)`, `AddEventAssets`, and relic table traversal.

- [ ] **Step 5: Add save serialization**

In `MainMenuRunSaveSystem`, add saved context classes:

```csharp
[Serializable]
internal sealed class SavedSpecialDungeonRuntimeContext
{
    public bool isActive;
    public string activeDungeonId;
    public SavedVector2Int returnEventCoord;
    public SavedRunSnapshot mainMapSnapshot;
    public List<int> deferredCalamityTiers = new List<int>();
}
```

Add to `SavedRunSnapshot`:

```csharp
public SavedSpecialDungeonRuntimeContext specialDungeonContext;
```

Serialize/deserialize via `SerializeSpecialDungeonContext` and `DeserializeSpecialDungeonContext`. The active dungeon asset can resolve by `activeDungeonId` if the catalog stores dungeon assets; if catalog does not yet support this, store only the ID in the saved context and restore board state from snapshot.

- [ ] **Step 6: Run tests/build**

Run: `dotnet build Assembly-CSharp-Editor.csproj --no-restore`

Expected: PASS compile, 0 errors.

- [ ] **Step 7: Commit**

```bash
git add Assets/Scripts/Shared/SpecialDungeonRuntimeContext.cs Assets/Scripts/Shared/SpecialDungeonRuntimeContext.cs.meta Assets/Scripts/MapScene/Manager/MapRunRuntime.cs Assets/Scripts/MainMenu/MainMenuRunSaveSystem.cs Assets/Scripts/Shared/RunSaveAssetCatalogBuilder.cs Assets/Tests/EditMode/Editor/SpecialDungeonRuntimeContextTests.cs Assembly-CSharp.csproj Assembly-CSharp-Editor.csproj
git commit -m "feat: persist special dungeon runtime context"
```

---

### Task 5: Add Event Built-In Effect For Entering Special Dungeons

**Files:**
- Modify: `Assets/Scripts/ScriptableObject/MapEvent_SO.cs`
- Modify: `Assets/Scripts/MapScene/Manager/MapEventEffectSequenceBuilder.cs`
- Modify: `Assets/Scripts/Shared/MapEventBuiltInEffectExecutorService.cs`
- Modify: `Assets/Scripts/MapScene/Manager/RunSceneFlowState.cs`
- Test: `Assets/Tests/EditMode/Editor/SpecialDungeonEventEffectTests.cs`

- [ ] **Step 1: Write failing effect tests**

Create `Assets/Tests/EditMode/Editor/SpecialDungeonEventEffectTests.cs`:

```csharp
using NUnit.Framework;
using UnityEngine;

public class SpecialDungeonEventEffectTests
{
    [Test]
    public void TryExecute_StartSpecialDungeonReturnsPendingDungeon()
    {
        SpecialDungeon_SO dungeon = ScriptableObject.CreateInstance<SpecialDungeon_SO>();
        RunState runState = new RunState();
        MapEventOptionData option = new MapEventOptionData
        {
            builtInEffectType = MapEventOptionBuiltInEffectType.StartSpecialDungeon
        };
        option.specialDungeon = dungeon;

        bool success = MapEventBuiltInEffectExecutorService.TryExecute(
            new MapEventBuiltInEffectExecutionRequest
            {
                runState = runState,
                optionData = option,
                currentEventTile = new BoardTileData { coord = new Vector2Int(2, 2), tileType = MapTileType.Event }
            },
            out MapEventBuiltInEffectExecutionResult result);

        Assert.IsTrue(success);
        Assert.AreSame(dungeon, result.pendingSpecialDungeon);

        Object.DestroyImmediate(dungeon);
    }
}
```

- [ ] **Step 2: Run compile and confirm failure**

Run: `dotnet build Assembly-CSharp-Editor.csproj --no-restore`

Expected: FAIL with missing enum value, `option.specialDungeon`, or `pendingSpecialDungeon`.

- [ ] **Step 3: Extend event data**

In `MapEventOptionBuiltInEffectType` add:

```csharp
StartSpecialDungeon = 17
```

In `MapEventBuiltInEffectData` add:

```csharp
public SpecialDungeon_SO specialDungeon;
```

In `MapEventOptionData` add:

```csharp
public SpecialDungeon_SO specialDungeon;
```

In `MapEventEffectSequenceBuilder.BuildPrimaryEffect`, copy:

```csharp
specialDungeon = optionData.specialDungeon
```

- [ ] **Step 4: Extend executor result**

In `MapEventBuiltInEffectExecutionResult` add:

```csharp
public SpecialDungeon_SO pendingSpecialDungeon;
```

In `TryExecuteEffect` add case:

```csharp
case MapEventOptionBuiltInEffectType.StartSpecialDungeon:
    return ExecuteStartSpecialDungeon(effectData, result, out message);
```

Add:

```csharp
private static bool ExecuteStartSpecialDungeon(
    MapEventBuiltInEffectData effectData,
    MapEventBuiltInEffectExecutionResult result,
    out string message)
{
    if (effectData == null || effectData.specialDungeon == null)
    {
        message = "特殊副本未配置。";
        return false;
    }

    result.pendingSpecialDungeon = effectData.specialDungeon;
    message = $"进入特殊副本：{effectData.specialDungeon.DisplayName}";
    return true;
}
```

- [ ] **Step 5: Run tests/build**

Run: `dotnet build Assembly-CSharp-Editor.csproj --no-restore`

Expected: PASS compile, 0 errors.

- [ ] **Step 6: Commit**

```bash
git add Assets/Scripts/ScriptableObject/MapEvent_SO.cs Assets/Scripts/MapScene/Manager/MapEventEffectSequenceBuilder.cs Assets/Scripts/Shared/MapEventBuiltInEffectExecutorService.cs Assets/Tests/EditMode/Editor/SpecialDungeonEventEffectTests.cs Assembly-CSharp-Editor.csproj
git commit -m "feat: add special dungeon event effect"
```

---

### Task 6: Enter And Exit Special Dungeons In RunGameManager

**Files:**
- Modify: `Assets/Scripts/MapScene/Manager/RunSceneFlowState.cs`
- Modify: `Assets/Scripts/MapScene/Manager/RunGameManager.cs`
- Modify: `Assets/Scripts/MapScene/Manager/PlayerMapController.cs`
- Test: `Assets/Tests/EditMode/Editor/SpecialDungeonFlowServiceTests.cs`

- [ ] **Step 1: Write failing flow tests using a small pure helper**

Create helper service `SpecialDungeonExitService` if needed; write test:

```csharp
using NUnit.Framework;
using UnityEngine;

public class SpecialDungeonFlowServiceTests
{
    [Test]
    public void ExitDungeon_RestoresMainMapEventTileAsResolved()
    {
        MapRunSnapshot main = new MapRunSnapshot
        {
            sceneName = "Map",
            runState = new RunState { playerGridPosition = new Vector2Int(4, 4) },
            boardSize = 6,
            spawnCoord = Vector2Int.zero,
            bossCoord = new Vector2Int(5, 5),
            selectedCoord = new Vector2Int(4, 4),
            actionPointBase = 6,
            actionPointCurrent = 3
        };
        main.tiles.Add(new BoardTileData { coord = new Vector2Int(4, 4), tileType = MapTileType.Event });

        SpecialDungeonRuntimeContext context = new SpecialDungeonRuntimeContext
        {
            isActive = true,
            returnEventCoord = new Vector2Int(4, 4),
            mainMapSnapshot = main
        };

        MapRunSnapshot restored = SpecialDungeonExitService.BuildReturnedMainMapSnapshot(context, new RunState());

        Assert.AreEqual(new Vector2Int(4, 4), restored.runState.playerGridPosition);
        Assert.IsTrue(restored.tiles[0].isResolved);
        Assert.IsNull(restored.tiles[0].assignedEvent);
    }
}
```

- [ ] **Step 2: Implement pure exit helper**

Create `Assets/Scripts/Shared/SpecialDungeonExitService.cs`:

```csharp
using UnityEngine;

public static class SpecialDungeonExitService
{
    public static MapRunSnapshot BuildReturnedMainMapSnapshot(SpecialDungeonRuntimeContext context, RunState latestRunState)
    {
        if (context == null || context.mainMapSnapshot == null)
            return null;

        MapRunSnapshot restored = context.mainMapSnapshot.Clone();
        if (latestRunState != null)
            restored.runState = latestRunState.Clone();
        if (restored.runState != null)
            restored.runState.playerGridPosition = context.returnEventCoord;

        for (int i = 0; i < restored.tiles.Count; i++)
        {
            BoardTileData tile = restored.tiles[i];
            if (tile == null || tile.coord != context.returnEventCoord)
                continue;

            tile.isVisited = true;
            tile.isResolved = true;
            tile.assignedEvent = null;
            break;
        }

        restored.selectedCoord = context.returnEventCoord;
        restored.specialDungeonContext = null;
        return restored;
    }
}
```

- [ ] **Step 3: Add RunSceneFlowState pending dungeon**

In `RunSceneFlowState` add:

```csharp
public SpecialDungeon_SO PendingSpecialDungeon { get; set; }
```

Reset it in `Reset()`.

- [ ] **Step 4: Capture pending dungeon from event result**

In `RunGameManager.HandleEventOptionSelected`:

```csharp
runFlowState.PendingSpecialDungeon = builtInResult.pendingSpecialDungeon;
```

In `HandleMapEventOverlayHidden`, before calamity presentation:

```csharp
if (runFlowState.PendingSpecialDungeon != null)
{
    EnterPendingSpecialDungeon();
    return;
}
```

- [ ] **Step 5: Implement EnterPendingSpecialDungeon**

Add to `RunGameManager`:

```csharp
private bool IsInSpecialDungeon => activeSpecialDungeonContext != null && activeSpecialDungeonContext.isActive;
private SpecialDungeonRuntimeContext activeSpecialDungeonContext;

private void EnterPendingSpecialDungeon()
{
    SpecialDungeon_SO dungeon = runFlowState.PendingSpecialDungeon;
    runFlowState.PendingSpecialDungeon = null;
    if (dungeon == null || runState == null || boardManager == null || actionPointSystem == null)
        return;

    Vector2Int returnCoord = runState.playerGridPosition;
    MapRunRuntime.SaveSnapshot(SceneManager.GetActiveScene().name, runState, boardManager, actionPointSystem, runFlowState.PendingCalamityTiers);
    MapRunRuntime.TryGetSnapshot(SceneManager.GetActiveScene().name, out MapRunSnapshot mainSnapshot);

    activeSpecialDungeonContext = new SpecialDungeonRuntimeContext
    {
        isActive = true,
        activeDungeonId = dungeon.dungeonId,
        activeDungeon = dungeon,
        returnEventCoord = returnCoord,
        mainMapSnapshot = mainSnapshot,
        deferredCalamityTiers = new List<int>(runFlowState.PendingCalamityTiers)
    };
    runFlowState.ClearPendingCalamityTiers();

    SpecialDungeonBoardBuildResult board = SpecialDungeonBoardBuilder.Build(dungeon, UnityEngine.Random.Range(int.MinValue, int.MaxValue));
    runState.boardSize = board.boardSize;
    runState.playerGridPosition = board.spawnCoord;
    boardManager.LoadBoard(board.boardSize, board.spawnCoord, board.rightExitCoord, board.spawnCoord, board.tiles);
    playerMapController.SetSpawn(board.tiles[1]);
    boardManager.SetSelectedTile(board.spawnCoord);
    boardManager.RefreshAllViews();
    mapHUD.ShowTile(board.tiles[1], 0);
    RefreshHUD($"进入特殊副本：{dungeon.DisplayName}");
    mapViewController.Initialize(mapCamera, boardManager, cameraPadding);
}
```

If `RunSceneFlowState` lacks `ClearPendingCalamityTiers`, add a method that clears the pending list safely.

- [ ] **Step 6: Route exit tile clicks**

In `HandleTileClicked` and `HandlePlayerMovedToTile`, before normal tile logic:

```csharp
if (tile.tileType == MapTileType.SpecialDungeonExit)
{
    ExitSpecialDungeon();
    return;
}
```

Implement `ExitSpecialDungeon`:

```csharp
private void ExitSpecialDungeon()
{
    if (!IsInSpecialDungeon)
        return;

    MapRunSnapshot restored = SpecialDungeonExitService.BuildReturnedMainMapSnapshot(activeSpecialDungeonContext, runState);
    if (restored == null)
        return;

    activeSpecialDungeonContext = null;
    runState = restored.runState;
    boardManager.LoadBoard(restored.boardSize, restored.spawnCoord, restored.bossCoord, restored.selectedCoord, restored.tiles);
    actionPointSystem.RestoreState(restored.actionPointBase, restored.actionPointBonus, restored.actionPointCurrent);
    playerMapController.Initialize(unitsRoot, boardManager, actionPointSystem, tileResolver, runState);
    if (boardManager.TryGetTile(runState.playerGridPosition, out BoardTileData returnTile))
        playerMapController.SetPosition(returnTile, false);

    RefreshHUD("已离开特殊副本，返回主地图。");
    mapViewController.Initialize(mapCamera, boardManager, cameraPadding);
    TryPresentPendingCalamity();
}
```

- [ ] **Step 7: Run tests/build**

Run: `dotnet build Assembly-CSharp-Editor.csproj --no-restore`

Expected: PASS compile, 0 errors.

- [ ] **Step 8: Commit**

```bash
git add Assets/Scripts/Shared/SpecialDungeonExitService.cs Assets/Scripts/Shared/SpecialDungeonExitService.cs.meta Assets/Scripts/MapScene/Manager/RunSceneFlowState.cs Assets/Scripts/MapScene/Manager/RunGameManager.cs Assets/Tests/EditMode/Editor/SpecialDungeonFlowServiceTests.cs Assembly-CSharp.csproj Assembly-CSharp-Editor.csproj
git commit -m "feat: enter and exit special dungeons"
```

---

### Task 7: Route Dungeon Tile Behaviors And Movement Cost

**Files:**
- Modify: `Assets/Scripts/MapScene/Manager/RunGameManager.cs`
- Modify: `Assets/Scripts/MapScene/Manager/PlayerMapController.cs`
- Modify: `Assets/Scripts/Shared/MapMovementService.cs`
- Modify: `Assets/Scripts/Shared/MapBattleEncounterService.cs`
- Test: `Assets/Tests/EditMode/Editor/SpecialDungeonMovementTests.cs`

- [ ] **Step 1: Write failing movement cost tests**

Create `Assets/Tests/EditMode/Editor/SpecialDungeonMovementTests.cs`:

```csharp
using NUnit.Framework;

public class SpecialDungeonMovementTests
{
    [Test]
    public void GetMoveCost_InSpecialDungeonReturnsZero()
    {
        BoardTileData tile = new BoardTileData { tileType = MapTileType.Battle };

        int cost = MapMovementService.GetMoveCost(tile, true);

        Assert.AreEqual(0, cost);
    }
}
```

- [ ] **Step 2: Add movement cost overload**

In `MapMovementService`, add:

```csharp
public static int GetMoveCost(BoardTileData tile, bool isInSpecialDungeon)
{
    if (isInSpecialDungeon)
        return 0;

    return GetMoveCost(tile);
}
```

Update `RunGameManager.GetMoveCost(tile)` to call this overload with `IsInSpecialDungeon`.

- [ ] **Step 3: Ensure special dungeon hostile tiles use configured group**

In `TryEnterBattle`, before normal enemy selection:

```csharp
if (IsInSpecialDungeon && tile.assignedEnemyGroup != null && tile.assignedEnemyGroup.IsValid)
    encounterGroup = tile.assignedEnemyGroup;
```

When saving snapshot before battle, pass `activeSpecialDungeonContext`.

```csharp
MapRunRuntime.SaveSnapshot(..., activeSpecialDungeonContext);
```

- [ ] **Step 4: Ensure merchant stock is not regenerated for fixed dungeon shops**

In `OpenMerchantPanel` or `EnsureMerchantStockGenerated`, when `IsInSpecialDungeon && tile.merchantOffers.Count > 0`, set:

```csharp
tile.merchantStockGenerated = true;
```

and skip `MerchantStockService`.

- [ ] **Step 5: Reject nested special dungeons**

In `HandleEventOptionSelected`, if `IsInSpecialDungeon && builtInResult.pendingSpecialDungeon != null`, fail:

```csharp
message = "特殊副本内暂不支持进入新的特殊副本。";
return false;
```

- [ ] **Step 6: Run build**

Run: `dotnet build MobileGame.sln --no-restore`

Expected: PASS, 0 errors.

- [ ] **Step 7: Commit**

```bash
git add Assets/Scripts/MapScene/Manager/RunGameManager.cs Assets/Scripts/Shared/MapMovementService.cs Assets/Tests/EditMode/Editor/SpecialDungeonMovementTests.cs Assembly-CSharp-Editor.csproj
git commit -m "feat: route special dungeon tile behavior"
```

---

### Task 8: UI Presentation For Special Dungeon Exit

**Files:**
- Modify: `Assets/Scripts/MapScene/Manager/BoardManager.cs`
- Modify: `Assets/Scripts/MapScene/Manager/TileResolver.cs`
- Modify: `Assets/Scripts/MapScene/Controller/MapHUD.cs`
- Test: `Assets/Tests/EditMode/Editor/MapHUDTilePresentationTests.cs`

- [ ] **Step 1: Write failing HUD tests**

Add to `MapHUDTilePresentationTests.cs`:

```csharp
[Test]
public void ShowTile_SpecialDungeonExitDisplaysExitLabel()
{
    BoardTileData tile = new BoardTileData
    {
        coord = new Vector2Int(0, 1),
        tileType = MapTileType.SpecialDungeonExit
    };

    string label = InvokeGetTileTypeLabel(tile.tileType);
    string description = InvokeGetTileDescription(tile.tileType);

    Assert.AreEqual("副本出口", label);
    Assert.That(description, Does.Contain("离开副本"));
}
```

- [ ] **Step 2: Add BoardManager visual support**

In `BoardManager`, add serialized field:

```csharp
[SerializeField] private Sprite specialDungeonExitIcon;
```

In icon lookup switch, return:

```csharp
case MapTileType.SpecialDungeonExit:
    return specialDungeonExitIcon != null ? specialDungeonExitIcon : defaultTileSprite;
```

In color switch, give exit a readable tint:

```csharp
case MapTileType.SpecialDungeonExit:
    baseColor *= new Color(0.8f, 1.1f, 1.2f, 1f);
    break;
```

- [ ] **Step 3: Add TileResolver support**

In `TileResolver.ResolveTile`:

```csharp
case MapTileType.SpecialDungeonExit:
    tile.isVisited = true;
    return "离开特殊副本。";
```

- [ ] **Step 4: Add HUD label and description**

In `MapHUD.GetTileTypeLabel`:

```csharp
case MapTileType.SpecialDungeonExit:
    return "副本出口";
```

In `MapHUD.GetTileDescription`:

```csharp
case MapTileType.SpecialDungeonExit:
    return "离开副本并返回主地图事件格。";
```

- [ ] **Step 5: Run build**

Run: `dotnet build Assembly-CSharp-Editor.csproj --no-restore`

Expected: PASS compile, 0 errors.

- [ ] **Step 6: Commit**

```bash
git add Assets/Scripts/MapScene/Manager/BoardManager.cs Assets/Scripts/MapScene/Manager/TileResolver.cs Assets/Scripts/MapScene/Controller/MapHUD.cs Assets/Tests/EditMode/Editor/MapHUDTilePresentationTests.cs
git commit -m "feat: present special dungeon exits"
```

---

### Task 9: Full Verification And Cleanup

**Files:**
- Modify only files needed to fix verification issues.

- [ ] **Step 1: Run full build**

Run: `dotnet build MobileGame.sln --no-restore`

Expected: PASS, 0 errors. Existing warnings are acceptable only if unrelated and already present.

- [ ] **Step 2: Run available Unity edit-mode tests if supported**

Run: `dotnet test Assembly-CSharp-Editor.csproj --no-build --filter FullyQualifiedName~SpecialDungeon`

Expected: If the Unity-generated project supports discovery, tests pass. If no output is produced, record that `dotnet test` does not execute Unity NUnit tests in this project and rely on compile verification plus Unity Editor manual test.

- [ ] **Step 3: Manual Unity validation checklist**

In Unity Editor:

- Create a `SpecialDungeon_SO` asset with Battle, Merchant, Treasure, Event tiles.
- Create a `MapEvent_SO` option with `LoseActionPoints` followed by `StartSpecialDungeon`.
- Trigger the event on a map Event tile.
- Confirm the map becomes a straight line with left exit, spawn, configured tiles, right exit.
- Confirm movement inside the dungeon costs 0 action points.
- Confirm fixed treasure options apply money, blueprint, relic, and relic bag rewards.
- Confirm exiting returns to the original Event tile and the tile cannot be triggered again.
- Confirm a dungeon battle defeat follows the normal defeat flow.

- [ ] **Step 4: Check git status**

Run: `git status --short`

Expected: only intentional files modified. Do not stage unrelated `TODO.md` unless the user explicitly asks.

- [ ] **Step 5: Commit verification fixes**

If fixes were needed:

```bash
git add <fixed-files>
git commit -m "fix: complete special dungeon verification"
```

If no fixes were needed, do not create an empty commit.

---

## Self-Review

Spec coverage:

- Event built-in effect is covered by Task 5.
- `SpecialDungeon_SO` and configured tile list are covered by Task 1 and Task 2.
- Linear map with left exit, spawn, configured tiles, right exit is covered by Task 2 and Task 6.
- Returning to main event tile and resolving it is covered by Task 4 and Task 6.
- Entry action point cost and delayed calamity handling is covered by Task 6 and Task 7.
- Dungeon movement cost 0 is covered by Task 7.
- Battle failure same as main map is covered by Task 7 and manual verification in Task 9.
- Fixed treasure options including relic bags are covered by Task 3.
- Save/restore is covered by Task 4.
- HUD and exit presentation are covered by Task 8.

No placeholders remain intentionally. Any code snippets using existing method names must be adjusted to exact local signatures while executing each task, with compile checks after every task.

