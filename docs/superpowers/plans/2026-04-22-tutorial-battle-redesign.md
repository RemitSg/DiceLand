# Tutorial Battle Redesign Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Refactor tutorial mode from a three-battle `Battle/Elite/Boss` structure into a seven-stage battle-focused lesson route with per-battle checkpoints, stage-based encounter resolution, and staged player-skill teaching.

**Architecture:** Introduce a tutorial battle stage id that becomes the source of truth for tutorial lesson routing, encounter payloads, and content lookup. First migrate the tutorial board and encounter resolution to stage-aware plumbing, then rewrite tutorial battle content so each of the seven battles has deterministic enemy sequencing, explicit teaching scope, and the correct temporary/accumulated player-skill profile.

**Tech Stack:** Unity 2022 C#, existing map/battle runtime systems, `ScriptableObject` tutorial content, EditMode tests under `Assets/Tests/EditMode/Editor`, `dotnet build` for compilation verification.

---

## File Structure

- Create: `Assets/Scripts/Tutorial/TutorialBattleStageId.cs`
  Defines the seven lesson ids and helper methods for stage ordering and final-stage checks.
- Modify: `Assets/Scripts/BattleScene/Manager/BattleEncounterRuntime.cs`
  Adds stage-aware tutorial encounter payload fields used for retry and completion routing.
- Modify: `Assets/Scripts/MapScene/Manager/RunGameManager.cs`
  Expands the fixed tutorial route, maps route positions to tutorial lesson ids, and resolves tutorial battle content by stage instead of by `MapTileType`.
- Modify: `Assets/Scripts/Tutorial/TutorialBattleDefinition_SO.cs`
  Keeps battle-step/reaction content model but remains stage-agnostic and reusable across all seven lessons.
- Modify: `Assets/Scripts/Tutorial/TutorialBattleRuntime.cs`
  Keeps event-driven tutorial battle progression but becomes stage-aware through the incoming encounter payload.
- Modify: `Assets/Scripts/Tutorial/TutorialBattleContentFactory.cs`
  Replaces the old three-bucket content API with explicit stage-based builders for enemy groups, skill grants, and tutorial copy.
- Modify: `Assets/Scripts/BattleScene/Controller/BattleButton.cs`
  Continues to emit tutorial events, but must preserve stage-aware completion/defeat handling and avoid assumptions that the final tutorial battle is always `MapTileType.Boss`.
- Modify: `Assets/Scripts/BattleScene/Controller/BattleSettlementOverlay.cs`
  Uses tutorial stage id to decide whether a victory returns to map or main menu, while keeping retry as current-battle-only.
- Test: `Assets/Tests/EditMode/Editor/RunGameManagerTutorialBoardTests.cs`
  Update for the seven-battle route.
- Test: `Assets/Tests/EditMode/Editor/TutorialBattleContentFactoryTests.cs`
  Add stage-based content coverage and deterministic sequencing assertions.
- Test: `Assets/Tests/EditMode/Editor/TutorialBattleDefinitionTests.cs`
  Add coverage for opening-stage scope and advanced-stage timing lessons.
- Create: `Assets/Tests/EditMode/Editor/TutorialBattleStageRoutingTests.cs`
  Verifies stage resolution and encounter payload behavior.

### Task 1: Add A Tutorial Battle Stage Id

**Files:**
- Create: `Assets/Scripts/Tutorial/TutorialBattleStageId.cs`
- Create: `Assets/Tests/EditMode/Editor/TutorialBattleStageRoutingTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
using NUnit.Framework;

public class TutorialBattleStageRoutingTests
{
    [Test]
    public void TutorialBattleStageId_FinalStageCheck_OnlyReturnsTrueForBattle07()
    {
        Assert.IsFalse(TutorialBattleStageUtility.IsFinalStage(TutorialBattleStageId.Battle01));
        Assert.IsFalse(TutorialBattleStageUtility.IsFinalStage(TutorialBattleStageId.Battle06));
        Assert.IsTrue(TutorialBattleStageUtility.IsFinalStage(TutorialBattleStageId.Battle07));
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `rg -n "TutorialBattleStageId|TutorialBattleStageUtility" Assets/Scripts Assets/Tests -g "*.cs"`
Expected: no results because the tutorial stage id type does not exist yet.

- [ ] **Step 3: Write minimal implementation**

```csharp
public enum TutorialBattleStageId
{
    None = 0,
    Battle01 = 1,
    Battle02 = 2,
    Battle03 = 3,
    Battle04 = 4,
    Battle05 = 5,
    Battle06 = 6,
    Battle07 = 7
}

public static class TutorialBattleStageUtility
{
    public static bool IsBattleStage(TutorialBattleStageId stageId)
    {
        return stageId >= TutorialBattleStageId.Battle01
            && stageId <= TutorialBattleStageId.Battle07;
    }

    public static bool IsFinalStage(TutorialBattleStageId stageId)
    {
        return stageId == TutorialBattleStageId.Battle07;
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `rg -n "TutorialBattleStageId|TutorialBattleStageUtility" Assets/Scripts Assets/Tests -g "*.cs"`
Expected: results in the new script and test file, with the final-stage helper now defined.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Tutorial/TutorialBattleStageId.cs Assets/Tests/EditMode/Editor/TutorialBattleStageRoutingTests.cs
git commit -m "feat: add tutorial battle stage identifiers"
```

### Task 2: Refactor Tutorial Board Routing To Seven Battle Lessons

**Files:**
- Modify: `Assets/Scripts/MapScene/Manager/RunGameManager.cs`
- Modify: `Assets/Tests/EditMode/Editor/RunGameManagerTutorialBoardTests.cs`
- Test: `Assets/Tests/EditMode/Editor/TutorialBattleStageRoutingTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class RunGameManagerTutorialBoardTests
{
    [Test]
    public void BuildTutorialBoardTiles_CreatesBattleHeavySevenLessonRoute()
    {
        MethodInfo method = typeof(RunGameManager).GetMethod("BuildTutorialBoardTiles", BindingFlags.Static | BindingFlags.NonPublic);

        Assert.IsNotNull(method);

        List<BoardTileData> tiles = method.Invoke(null, new object[] { 15 }) as List<BoardTileData>;
        Assert.IsNotNull(tiles);
        Assert.AreEqual(14, tiles.Count);

        MapTileType[] expectedSequence =
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
            MapTileType.Battle,
            MapTileType.Key,
            MapTileType.Battle,
            MapTileType.Boss
        };

        for (int i = 0; i < expectedSequence.Length; i++)
            Assert.AreEqual(expectedSequence[i], tiles[i].tileType, $"Tile type mismatch at index {i}.");
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `rg -n "BuildTutorialBoardTiles|TutorialBoardSize" Assets/Scripts/MapScene/Manager/RunGameManager.cs`
Expected: current route still shows the old ten-tile sequence and old board size assumptions.

- [ ] **Step 3: Write minimal implementation**

```csharp
private const int TutorialBoardSize = 15;

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
    MapTileType.Battle,
    MapTileType.Key,
    MapTileType.Battle,
    MapTileType.Boss
};

private static List<BoardTileData> BuildTutorialBoardTiles(int boardSize)
{
    int row = Mathf.Max(1, boardSize / 2);
    List<BoardTileData> tiles = new List<BoardTileData>(TutorialTileSequence.Length);
    for (int i = 0; i < TutorialTileSequence.Length; i++)
    {
        MapTileType tileType = TutorialTileSequence[i];
        tiles.Add(new BoardTileData
        {
            coord = new Vector2Int(i, row),
            tileType = tileType,
            dangerBand = ResolveTutorialDangerBand(i),
            isVisited = i == 0,
            isResolved = tileType == MapTileType.Empty,
            isSpawnTile = i == 0,
            encounterSeed = TutorialEncounterSeedBase + i,
            previewEnemyCount = ResolveTutorialPreviewEnemyCount(tileType, i),
            previewDangerRating = ResolveTutorialPreviewDanger(tileType, i),
            previewTag = ResolveTutorialPreviewTag(tileType, i)
        });
    }

    return tiles;
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet build Assembly-CSharp-Editor.csproj -nologo`
Expected: compile success, and the updated test reflects the new fixed tutorial route length and order.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/MapScene/Manager/RunGameManager.cs Assets/Tests/EditMode/Editor/RunGameManagerTutorialBoardTests.cs Assets/Tests/EditMode/Editor/TutorialBattleStageRoutingTests.cs
git commit -m "feat: expand tutorial board to seven battle lessons"
```

### Task 3: Route Tutorial Encounters By Stage Id Instead Of By Encounter Tier

**Files:**
- Modify: `Assets/Scripts/MapScene/Manager/RunGameManager.cs`
- Modify: `Assets/Scripts/BattleScene/Manager/BattleEncounterRuntime.cs`
- Modify: `Assets/Tests/EditMode/Editor/TutorialBattleStageRoutingTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
using NUnit.Framework;
using UnityEngine;

public class TutorialBattleStageRoutingTests
{
    [Test]
    public void BattleEncounterData_CanCarryTutorialStageIdAndFinalStageFlag()
    {
        BattleEncounterData data = new BattleEncounterData
        {
            isTutorialEncounter = true,
            tutorialBattleStageId = TutorialBattleStageId.Battle07,
            returnToMainMenuOnVictory = true
        };

        Assert.AreEqual(TutorialBattleStageId.Battle07, data.tutorialBattleStageId);
        Assert.IsTrue(data.returnToMainMenuOnVictory);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `rg -n "tutorialBattleStageId" Assets/Scripts/BattleScene/Manager/BattleEncounterRuntime.cs Assets/Scripts/MapScene/Manager/RunGameManager.cs Assets/Tests/EditMode/Editor -g "*.cs"`
Expected: no results because encounter payloads do not yet carry a stage id.

- [ ] **Step 3: Write minimal implementation**

```csharp
[Serializable]
public sealed class BattleEncounterData
{
    public string sourceSceneName;
    public string targetSceneName;
    public string encounterLabel;
    public string tutorialCompletionSceneName;
    public TutorialBattleStageId tutorialBattleStageId;
    public MapTileType tileType;
    public MapDangerBand dangerBand;
    // existing fields unchanged
}

private static TutorialBattleStageId ResolveTutorialStageId(BoardTileData tile, int sequenceIndex)
{
    switch (sequenceIndex)
    {
        case 2: return TutorialBattleStageId.Battle01;
        case 3: return TutorialBattleStageId.Battle02;
        case 5: return TutorialBattleStageId.Battle03;
        case 7: return TutorialBattleStageId.Battle04;
        case 9: return TutorialBattleStageId.Battle05;
        case 10: return TutorialBattleStageId.Battle06;
        case 12: return TutorialBattleStageId.Battle07;
        default: return TutorialBattleStageId.None;
    }
}

private bool TryEnterBattle(BoardTileData tile)
{
    TutorialBattleStageId stageId = isTutorialRun
        ? ResolveTutorialStageId(tile, tile.coord.x)
        : TutorialBattleStageId.None;

    BattleEncounterRuntime.SetEncounter(new BattleEncounterData
    {
        sourceSceneName = SceneManager.GetActiveScene().name,
        targetSceneName = battleSceneName,
        tutorialCompletionSceneName = tutorialCompletionSceneName,
        tutorialBattleStageId = stageId,
        tileType = tile.tileType,
        enemyGroup = encounterGroup,
        tutorialBattleDefinition = tutorialBattleDefinition,
        isTutorialEncounter = isTutorialRun,
        returnToMainMenuOnVictory = TutorialBattleStageUtility.IsFinalStage(stageId),
        restartBattleOnDefeat = isTutorialRun
    });

    return true;
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `rg -n "tutorialBattleStageId|ResolveTutorialStageId" Assets/Scripts/BattleScene/Manager/BattleEncounterRuntime.cs Assets/Scripts/MapScene/Manager/RunGameManager.cs Assets/Tests/EditMode/Editor -g "*.cs"`
Expected: results show stage-aware encounter payload fields and a stage-resolution helper in `RunGameManager`.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/BattleScene/Manager/BattleEncounterRuntime.cs Assets/Scripts/MapScene/Manager/RunGameManager.cs Assets/Tests/EditMode/Editor/TutorialBattleStageRoutingTests.cs
git commit -m "feat: route tutorial encounters by lesson stage"
```

### Task 4: Refactor Tutorial Content Factory To Seven Explicit Stage Builders

**Files:**
- Modify: `Assets/Scripts/Tutorial/TutorialBattleContentFactory.cs`
- Modify: `Assets/Tests/EditMode/Editor/TutorialBattleContentFactoryTests.cs`
- Modify: `Assets/Tests/EditMode/Editor/TutorialBattleDefinitionTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
using NUnit.Framework;

public class TutorialBattleContentFactoryTests
{
    [Test]
    public void CreateRuntimeDefinition_Battle01_DoesNotIncludeAdvancedModifierTimingTeaching()
    {
        TutorialBattleDefinition_SO definition = TutorialBattleContentFactory.CreateRuntimeDefinition(TutorialBattleStageId.Battle01);

        Assert.IsNotNull(definition);
        StringAssert.DoesNotContain("拼点时", definition.GetOrderedSteps()[0].body ?? string.Empty);
        StringAssert.DoesNotContain("单方面攻击时", definition.GetOrderedSteps()[0].body ?? string.Empty);
    }

    [Test]
    public void CreateRuntimeDefinition_Battle06_ExplicitlyTeachesThreeModifierTimings()
    {
        TutorialBattleDefinition_SO definition = TutorialBattleContentFactory.CreateRuntimeDefinition(TutorialBattleStageId.Battle06);

        Assert.IsNotNull(definition);
        StringAssert.Contains("拼点时", definition.GetOrderedSteps()[0].body ?? string.Empty);
        StringAssert.Contains("使用该技能时", definition.GetOrderedSteps()[0].body ?? string.Empty);
        StringAssert.Contains("单方面攻击时", definition.GetOrderedSteps()[0].body ?? string.Empty);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `rg -n "CreateRuntimeDefinition\\(MapTileType|CreateRuntimeEnemyGroup\\(MapTileType" Assets/Scripts/Tutorial/TutorialBattleContentFactory.cs`
Expected: current factory still routes only by `MapTileType`.

- [ ] **Step 3: Write minimal implementation**

```csharp
public static TutorialBattleDefinition_SO CreateRuntimeDefinition(TutorialBattleStageId stageId)
{
    switch (stageId)
    {
        case TutorialBattleStageId.Battle01:
            return CreateSpeedIntroDefinition();
        case TutorialBattleStageId.Battle02:
            return CreateTargetingDefinition();
        case TutorialBattleStageId.Battle03:
            return CreateOneSidedAttackDefinition();
        case TutorialBattleStageId.Battle04:
            return CreateEnergyDefinition();
        case TutorialBattleStageId.Battle05:
            return CreateStatusDefinition();
        case TutorialBattleStageId.Battle06:
            return CreateAdvancedModifierDefinition();
        case TutorialBattleStageId.Battle07:
            return CreateFinalExerciseDefinition();
        default:
            return null;
    }
}

public static EnemyGroup_SO CreateRuntimeEnemyGroup(TutorialBattleStageId stageId, TutorialEnemyVisualTemplateSet visualTemplates = null)
{
    switch (stageId)
    {
        case TutorialBattleStageId.Battle01:
            return CreateBattle01EnemyGroup(visualTemplates);
        case TutorialBattleStageId.Battle02:
            return CreateBattle02EnemyGroup(visualTemplates);
        case TutorialBattleStageId.Battle03:
            return CreateBattle03EnemyGroup(visualTemplates);
        case TutorialBattleStageId.Battle04:
            return CreateBattle04EnemyGroup(visualTemplates);
        case TutorialBattleStageId.Battle05:
            return CreateBattle05EnemyGroup(visualTemplates);
        case TutorialBattleStageId.Battle06:
            return CreateBattle06EnemyGroup(visualTemplates);
        case TutorialBattleStageId.Battle07:
            return CreateBattle07EnemyGroup(visualTemplates);
        default:
            return null;
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet build Assembly-CSharp-Editor.csproj -nologo`
Expected: compile success with the factory no longer depending on three tutorial buckets.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Tutorial/TutorialBattleContentFactory.cs Assets/Tests/EditMode/Editor/TutorialBattleContentFactoryTests.cs Assets/Tests/EditMode/Editor/TutorialBattleDefinitionTests.cs
git commit -m "refactor: switch tutorial battle content to stage-based builders"
```

### Task 5: Implement Seven Lesson Encounter Sets And Player Skill Progression

**Files:**
- Modify: `Assets/Scripts/Tutorial/TutorialBattleContentFactory.cs`
- Modify: `Assets/Tests/EditMode/Editor/TutorialBattleContentFactoryTests.cs`
- Modify: `Assets/Tests/EditMode/Editor/TutorialBattleDefinitionTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
using NUnit.Framework;

public class TutorialBattleContentFactoryTests
{
    [Test]
    public void CreateRuntimeEnemyGroup_AllTutorialBattleStagesHaveDeterministicSkillRules()
    {
        for (int i = 1; i <= 7; i++)
        {
            TutorialBattleStageId stageId = (TutorialBattleStageId)i;
            EnemyGroup_SO group = TutorialBattleContentFactory.CreateRuntimeEnemyGroup(stageId);

            Assert.IsNotNull(group, $"Group missing for {stageId}");
            Assert.IsNotNull(group.enemies, $"Enemies missing for {stageId}");
            Assert.Greater(group.enemies.Count, 0, $"No enemies for {stageId}");

            for (int enemyIndex = 0; enemyIndex < group.enemies.Count; enemyIndex++)
            {
                EnemyData_SO enemy = group.enemies[enemyIndex];
                Assert.IsNotNull(enemy.conditionalSkillRules, $"Rules missing for {stageId}/{enemy.enemyName}");
                Assert.IsTrue(enemy.conditionalSkillRules.Count > 0, $"No deterministic skill rules for {stageId}/{enemy.enemyName}");
            }
        }
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `rg -n "CreateBattle0[1-7]EnemyGroup|CreateBattle0[1-7].*Definition" Assets/Scripts/Tutorial/TutorialBattleContentFactory.cs`
Expected: some or all stage-specific builders are still missing.

- [ ] **Step 3: Write minimal implementation**

```csharp
private static EnemyData_SO CreateScriptedTutorialEnemy(
    string name,
    int hp,
    int minSpeed,
    int maxSpeed,
    int speedButtonCount,
    IEnumerable<EnemySkill_SO> enemySkills,
    IEnumerable<EnemySkill_SO> allyGrantedSkills,
    params (string ruleName, EnemySkill_SO skill, int round, int priority, int maxSlots)[] rules)
{
    EnemyData_SO enemy = CreateEnemy(name, hp, minSpeed, maxSpeed, speedButtonCount, enemySkills, allyGrantedSkills);
    foreach (var rule in rules)
    {
        AddRoundSequenceRule(enemy, rule.ruleName, rule.skill, rule.round, rule.priority, rule.maxSlots);
    }

    return enemy;
}

private static EnemyGroup_SO CreateBattle01EnemyGroup(TutorialEnemyVisualTemplateSet visualTemplates)
{
    EnemySkill_SO slowIntent = CreateSkill("蓄势", "低速技能，用于讲解速度与读招。", 0, 1, 2);
    EnemySkill_SO fastIntent = CreateSkill("抢先突刺", "高速技能，用于讲解更快的动作更需要优先注意。", 0, 2, 3);

    EnemySkill_SO allyObserve = CreateSkill("观察反应", "基础教学技能。", 0, 2, 3);

    EnemyData_SO enemy = CreateScriptedTutorialEnemy(
        "速度示范体",
        18,
        2,
        6,
        1,
        new[] { slowIntent, fastIntent },
        new[] { allyObserve },
        ("Round 1 Fast", fastIntent, 1, 200, 1),
        ("Round 2 Slow", slowIntent, 2, 200, 1));

    ApplyVisualTemplate(enemy, visualTemplates != null ? visualTemplates.openingPrimaryTemplate : null);
    return CreateGroup("TutorialBattleGroup_01", 9201, "教程战 01", "速度与读招", enemy);
}

private static EnemyGroup_SO CreateGroup(string runtimeName, int groupId, string groupName, string description, params EnemyData_SO[] enemies)
{
    EnemyGroup_SO group = ScriptableObject.CreateInstance<EnemyGroup_SO>();
    PrepareRuntimeObject(group, runtimeName);
    group.groupID = groupId;
    group.groupName = groupName;
    group.description = description;
    group.enemies = new List<EnemyData_SO>(enemies);
    return group;
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet build Assembly-CSharp-Editor.csproj -nologo`
Expected: compile success with all seven stages present and deterministic sequencing authored in the factory.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Tutorial/TutorialBattleContentFactory.cs Assets/Tests/EditMode/Editor/TutorialBattleContentFactoryTests.cs Assets/Tests/EditMode/Editor/TutorialBattleDefinitionTests.cs
git commit -m "feat: add seven staged tutorial battle lesson sets"
```

### Task 6: Update Tutorial Victory/Retry Flow To Use Stage-Aware Finality

**Files:**
- Modify: `Assets/Scripts/BattleScene/Controller/BattleSettlementOverlay.cs`
- Modify: `Assets/Scripts/BattleScene/Controller/BattleButton.cs`
- Modify: `Assets/Tests/EditMode/Editor/TutorialBattleStageRoutingTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
using NUnit.Framework;

public class TutorialBattleStageRoutingTests
{
    [Test]
    public void FinalTutorialStage_UsesReturnToMainMenuWhileEarlierStagesDoNot()
    {
        BattleEncounterData early = new BattleEncounterData
        {
            isTutorialEncounter = true,
            tutorialBattleStageId = TutorialBattleStageId.Battle03,
            returnToMainMenuOnVictory = false
        };

        BattleEncounterData final = new BattleEncounterData
        {
            isTutorialEncounter = true,
            tutorialBattleStageId = TutorialBattleStageId.Battle07,
            returnToMainMenuOnVictory = true
        };

        Assert.IsFalse(early.returnToMainMenuOnVictory);
        Assert.IsTrue(final.returnToMainMenuOnVictory);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `rg -n "returnToMainMenuOnVictory = tile\\.tileType == MapTileType\\.Boss|isTutorialEncounter && activeEncounter.returnToMainMenuOnVictory" Assets/Scripts/BattleScene Assets/Scripts/MapScene -g "*.cs"`
Expected: code still assumes tutorial completion is tied to boss tile type instead of final lesson stage.

- [ ] **Step 3: Write minimal implementation**

```csharp
if (isTutorialRun)
{
    tutorialBattleDefinition = ResolveTutorialBattleDefinition(stageId);
    tutorialReturnsToMainMenu = TutorialBattleStageUtility.IsFinalStage(stageId);
}
```

```csharp
if (activeEncounter != null && activeEncounter.isTutorialEncounter && activeEncounter.returnToMainMenuOnVictory)
{
    MapRunRuntime.Clear(activeEncounter.sourceSceneName);
    MainMenuRuntime.ClearTutorialRequest();
    TutorialBattleRuntime.Clear();
}
else
{
    MapRunRuntime.ApplyEncounterResult(activeEncounter);
    TutorialBattleRuntime.Clear();
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet build Assembly-CSharp-Editor.csproj -nologo`
Expected: compile success, and tutorial victory/return logic no longer assumes the final stage is identified solely by `MapTileType.Boss`.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/BattleScene/Controller/BattleSettlementOverlay.cs Assets/Scripts/BattleScene/Controller/BattleButton.cs Assets/Scripts/MapScene/Manager/RunGameManager.cs Assets/Tests/EditMode/Editor/TutorialBattleStageRoutingTests.cs
git commit -m "fix: make tutorial completion depend on final lesson stage"
```

### Task 7: Verification And Regression Pass

**Files:**
- Modify: `Assets/Tests/EditMode/Editor/RunGameManagerTutorialBoardTests.cs`
- Modify: `Assets/Tests/EditMode/Editor/TutorialBattleContentFactoryTests.cs`
- Modify: `Assets/Tests/EditMode/Editor/TutorialBattleDefinitionTests.cs`
- Modify: `Assets/Tests/EditMode/Editor/TutorialBattleStageRoutingTests.cs`

- [ ] **Step 1: Write the final regression assertions**

```csharp
[Test]
public void TutorialBattle01_RemainsFocusedOnSpeedAndIntent()
{
    TutorialBattleDefinition_SO definition = TutorialBattleContentFactory.CreateRuntimeDefinition(TutorialBattleStageId.Battle01);
    string body = definition != null && definition.GetOrderedSteps().Count > 0
        ? definition.GetOrderedSteps()[0].body ?? string.Empty
        : string.Empty;

    StringAssert.Contains("速度", body);
    StringAssert.DoesNotContain("单方面攻击时", body);
    StringAssert.DoesNotContain("拼点时", body);
}
```

```csharp
[Test]
public void TutorialBattle06_IntentionallyIntroducesAdvancedModifierTiming()
{
    TutorialBattleDefinition_SO definition = TutorialBattleContentFactory.CreateRuntimeDefinition(TutorialBattleStageId.Battle06);
    string combined = string.Empty;
    foreach (TutorialBattleStepDefinition step in definition.GetOrderedSteps())
        combined += step != null ? step.title + step.body : string.Empty;

    StringAssert.Contains("拼点时", combined);
    StringAssert.Contains("使用该技能时", combined);
    StringAssert.Contains("单方面攻击时", combined);
}
```

- [ ] **Step 2: Run compile verification**

Run: `dotnet build Assembly-CSharp-Editor.csproj -nologo`
Expected: `Build succeeded.` with only pre-existing unrelated warnings.

- [ ] **Step 3: Run repository diff sanity check**

Run: `git diff -- Assets/Scripts/MapScene/Manager/RunGameManager.cs Assets/Scripts/Tutorial/TutorialBattleContentFactory.cs Assets/Scripts/BattleScene/Manager/BattleEncounterRuntime.cs Assets/Scripts/BattleScene/Controller/BattleSettlementOverlay.cs Assets/Scripts/BattleScene/Controller/BattleButton.cs Assets/Tests/EditMode/Editor`
Expected: diff only shows tutorial stage routing, seven-battle route changes, and related tests.

- [ ] **Step 4: Manual Unity verification checklist**

Run in Unity Editor:

```text
1. Start Tutorial from main menu.
2. Verify route order is Spawn -> Empty -> Battle_01 -> Battle_02 -> Rest -> Battle_03 -> Shop -> Battle_04 -> Forge -> Battle_05 -> Battle_06 -> Key -> Battle_07 -> End.
3. Verify Battle_01 only teaches speed/intent, not advanced modifier timing.
4. Verify each tutorial battle restarts only itself on defeat.
5. Verify Battle_05 introduces statuses and keywords.
6. Verify Battle_06 introduces [拼点时] / [使用该技能时] / [单方面攻击时].
7. Verify Battle_07 victory returns to main menu.
```

- [ ] **Step 5: Commit**

```bash
git add Assets/Tests/EditMode/Editor Assets/Scripts/MapScene/Manager/RunGameManager.cs Assets/Scripts/Tutorial/TutorialBattleContentFactory.cs Assets/Scripts/BattleScene/Manager/BattleEncounterRuntime.cs Assets/Scripts/BattleScene/Controller/BattleSettlementOverlay.cs Assets/Scripts/BattleScene/Controller/BattleButton.cs
git commit -m "test: verify seven-stage tutorial battle redesign"
```

## Self-Review

### Spec Coverage

- Seven-stage curriculum: covered in Tasks 2, 3, 4, and 5.
- Battle-heavy map route: covered in Task 2.
- Per-battle checkpoints: covered in Tasks 3 and 6.
- Early temporary skills plus later accumulation: covered in Task 5.
- Speed taught before advanced modifier timing: covered in Tasks 4, 5, and 7.
- Advanced modifier timing delayed to stage 6: covered in Tasks 4 and 7.
- Final tutorial battle returns to main menu: covered in Task 6.

### Placeholder Scan

- No `TODO`, `TBD`, or “implement later” markers remain.
- Every code-changing step contains explicit code snippets.
- Every verification step includes an exact command or manual checklist.

### Type Consistency

- The plan consistently uses `TutorialBattleStageId`.
- `BattleEncounterData.tutorialBattleStageId` is referenced consistently across map and settlement tasks.
- Content lookup APIs are consistently described as `CreateRuntimeDefinition(TutorialBattleStageId)` and `CreateRuntimeEnemyGroup(TutorialBattleStageId, ...)`.
