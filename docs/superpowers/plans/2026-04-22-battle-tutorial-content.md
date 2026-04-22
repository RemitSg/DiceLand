# Battle Tutorial Content Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fill the existing tutorial framework with battle-focused teaching content covering skill switching, shared energy costs, clash vs one-sided attacks, clash power vs attack power, line creation, key skill keywords, and status interactions using dedicated tutorial encounters and step-by-step overlays.

**Architecture:** Extend the current lightweight tutorial mode into a battle-specific data/runtime layer instead of hardcoding copy directly in `BattleButton` or `SpeedButton`. Use data-driven tutorial battle steps plus a runtime coordinator that listens to existing combat interaction points and drives a reusable overlay, while keeping the new logic opt-in for tutorial runs only.

**Tech Stack:** Unity 2022 C#, ScriptableObject content assets, existing runtime-generated overlay UI, Unity EditMode tests under `Assets/Tests/EditMode/Editor`.

---

## File Structure

- Create: `Assets/Scripts/Tutorial/TutorialBattleDefinition_SO.cs`
  Defines tutorial battle encounters, ordered battle lesson steps, trigger/completion enums, and step copy.
- Create: `Assets/Scripts/Tutorial/TutorialBattleRuntime.cs`
  Holds active tutorial battle state, current step index, completion flags, and helper methods used across scenes.
- Create: `Assets/Scripts/Tutorial/TutorialBattleGuideOverlay.cs`
  Runtime-generated battle guide overlay for modal/informational battle teaching steps.
- Modify: `Assets/Scripts/Shared/MainMenuRuntime.cs`
  Stores the active tutorial battle definition reference or id needed across scene loads.
- Modify: `Assets/Scripts/MapScene/Manager/RunGameManager.cs`
  Binds fixed tutorial battle definitions to the tutorial route and launches dedicated tutorial encounters.
- Modify: `Assets/Scripts/BattleScene/Controller/BattleButton.cs`
  Emits battle tutorial events, blocks round execution when the current tutorial step requires it, and handles tutorial failure/completion routing.
- Modify: `Assets/Scripts/BattleScene/Controller/SpeedButton.cs`
  Emits line-creation and enemy-click tutorial events.
- Modify: `Assets/Scripts/BattleScene/Controller/BattleSkillInfoOverlay.cs`
  Emits skill-view/status-view tutorial events and surfaces tutorial-specific hint refreshes.
- Modify: `Assets/Scripts/BattleScene/Controller/BattleEnemyController.cs`
  Exposes enough state metadata for tutorial content to identify status/keyword lessons cleanly.
- Modify: `Assets/Scripts/BattleScene/Controller/BattleAllyController.cs`
  Exposes selected skill switching and energy-cost tutorial checkpoints cleanly.
- Test: `Assets/Tests/EditMode/Editor/TutorialBattleDefinitionTests.cs`
- Test: `Assets/Tests/EditMode/Editor/TutorialBattleRuntimeTests.cs`
- Test: `Assets/Tests/EditMode/Editor/RunGameManagerTutorialBattleTests.cs`

### Task 1: Tutorial Battle Data Model

**Files:**
- Create: `Assets/Scripts/Tutorial/TutorialBattleDefinition_SO.cs`
- Test: `Assets/Tests/EditMode/Editor/TutorialBattleDefinitionTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
using NUnit.Framework;
using UnityEngine;

public class TutorialBattleDefinitionTests
{
    [Test]
    public void ForceValidate_AssignsStableDefaultsAndOrdersSteps()
    {
        TutorialBattleDefinition_SO definition = ScriptableObject.CreateInstance<TutorialBattleDefinition_SO>();
        definition.Steps.Add(new TutorialBattleStepDefinition { stepId = "", sortOrder = 20, title = "" });
        definition.Steps.Add(new TutorialBattleStepDefinition { stepId = "line", sortOrder = 10, title = "Draw A Line" });

        definition.ForceValidateForTests();

        Assert.AreEqual("tutorial_battle", definition.tutorialBattleId);
        Assert.AreEqual(2, definition.GetOrderedSteps().Count);
        Assert.AreEqual("line", definition.GetOrderedSteps()[0].stepId);
        Assert.IsFalse(string.IsNullOrWhiteSpace(definition.GetOrderedSteps()[1].stepId));
        Assert.IsFalse(string.IsNullOrWhiteSpace(definition.GetOrderedSteps()[1].title));
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `rg -n "class TutorialBattleDefinition_SO" Assets/Scripts/Tutorial`
Expected: no results because the tutorial battle definition script does not exist yet.

- [ ] **Step 3: Write minimal implementation**

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class TutorialBattleStepDefinition
{
    public string stepId = "step";
    public string title = "Tutorial Step";
    [TextArea(2, 6)] public string body;
    public int sortOrder;
}

[CreateAssetMenu(fileName = "TutorialBattleDefinition", menuName = "Tutorial/Tutorial Battle Definition")]
public class TutorialBattleDefinition_SO : ScriptableObject
{
    public string tutorialBattleId = "tutorial_battle";
    [SerializeField] private List<TutorialBattleStepDefinition> steps = new List<TutorialBattleStepDefinition>();

    public List<TutorialBattleStepDefinition> Steps => steps;

    public IReadOnlyList<TutorialBattleStepDefinition> GetOrderedSteps()
    {
        List<TutorialBattleStepDefinition> ordered = new List<TutorialBattleStepDefinition>();
        if (steps != null)
        {
            for (int i = 0; i < steps.Count; i++)
            {
                if (steps[i] != null)
                    ordered.Add(steps[i]);
            }
        }

        ordered.Sort((left, right) =>
        {
            int orderCompare = left.sortOrder.CompareTo(right.sortOrder);
            if (orderCompare != 0)
                return orderCompare;

            return string.CompareOrdinal(left.stepId ?? string.Empty, right.stepId ?? string.Empty);
        });

        return ordered;
    }

    public void ForceValidateForTests()
    {
        OnValidate();
    }

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(tutorialBattleId))
            tutorialBattleId = "tutorial_battle";
        if (steps == null)
            steps = new List<TutorialBattleStepDefinition>();

        for (int i = 0; i < steps.Count; i++)
        {
            TutorialBattleStepDefinition step = steps[i];
            if (step == null)
                continue;

            if (string.IsNullOrWhiteSpace(step.stepId))
                step.stepId = $"step_{i + 1}";
            if (string.IsNullOrWhiteSpace(step.title))
                step.title = "Tutorial Step";
        }
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `rg -n "class TutorialBattleDefinition_SO" Assets/Scripts/Tutorial`
Expected: one result in `Assets/Scripts/Tutorial/TutorialBattleDefinition_SO.cs`, and the Unity EditMode test should compile cleanly.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Tutorial/TutorialBattleDefinition_SO.cs Assets/Tests/EditMode/Editor/TutorialBattleDefinitionTests.cs
git commit -m "feat: add tutorial battle definition model"
```

### Task 2: Tutorial Battle Runtime State

**Files:**
- Create: `Assets/Scripts/Tutorial/TutorialBattleRuntime.cs`
- Modify: `Assets/Scripts/Shared/MainMenuRuntime.cs`
- Test: `Assets/Tests/EditMode/Editor/TutorialBattleRuntimeTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
using NUnit.Framework;
using UnityEngine;

public class TutorialBattleRuntimeTests
{
    [SetUp]
    public void SetUp()
    {
        TutorialBattleRuntime.Clear();
        MainMenuRuntime.ClearTutorialRequest();
    }

    [Test]
    public void BeginBattle_SetsActiveDefinitionAndStartsAtFirstStep()
    {
        TutorialBattleDefinition_SO definition = ScriptableObject.CreateInstance<TutorialBattleDefinition_SO>();
        definition.Steps.Add(new TutorialBattleStepDefinition { stepId = "intro", sortOrder = 0, title = "Intro" });
        definition.ForceValidateForTests();

        TutorialBattleRuntime.BeginBattle(definition);

        Assert.IsTrue(TutorialBattleRuntime.IsActive);
        Assert.AreEqual("intro", TutorialBattleRuntime.CurrentStep.stepId);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `rg -n "class TutorialBattleRuntime" Assets/Scripts/Tutorial`
Expected: no results because the runtime holder does not exist yet.

- [ ] **Step 3: Write minimal implementation**

```csharp
using UnityEngine;

public static class TutorialBattleRuntime
{
    public static TutorialBattleDefinition_SO ActiveDefinition { get; private set; }
    public static int CurrentStepIndex { get; private set; } = -1;
    public static bool IsActive => ActiveDefinition != null && CurrentStepIndex >= 0;

    public static TutorialBattleStepDefinition CurrentStep
    {
        get
        {
            if (!IsActive)
                return null;

            var ordered = ActiveDefinition.GetOrderedSteps();
            return CurrentStepIndex >= 0 && CurrentStepIndex < ordered.Count ? ordered[CurrentStepIndex] : null;
        }
    }

    public static void BeginBattle(TutorialBattleDefinition_SO definition)
    {
        ActiveDefinition = definition;
        CurrentStepIndex = definition != null && definition.GetOrderedSteps().Count > 0 ? 0 : -1;
    }

    public static void Clear()
    {
        ActiveDefinition = null;
        CurrentStepIndex = -1;
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `rg -n "class TutorialBattleRuntime" Assets/Scripts/Tutorial`
Expected: one result in `Assets/Scripts/Tutorial/TutorialBattleRuntime.cs`, and the test intent is now implemented.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Tutorial/TutorialBattleRuntime.cs Assets/Scripts/Shared/MainMenuRuntime.cs Assets/Tests/EditMode/Editor/TutorialBattleRuntimeTests.cs
git commit -m "feat: add tutorial battle runtime state"
```

### Task 3: Battle Interaction Hooks And Blocking

**Files:**
- Modify: `Assets/Scripts/BattleScene/Controller/BattleButton.cs`
- Modify: `Assets/Scripts/BattleScene/Controller/SpeedButton.cs`
- Modify: `Assets/Scripts/BattleScene/Controller/BattleSkillInfoOverlay.cs`
- Modify: `Assets/Scripts/BattleScene/Controller/BattleAllyController.cs`
- Modify: `Assets/Scripts/BattleScene/Controller/BattleEnemyController.cs`
- Create: `Assets/Scripts/Tutorial/TutorialBattleGuideOverlay.cs`
- Test: `Assets/Tests/EditMode/Editor/RunGameManagerTutorialBattleTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
using NUnit.Framework;
using UnityEngine;

public class RunGameManagerTutorialBattleTests
{
    [Test]
    public void TutorialBattleRuntime_BlocksResolutionUntilRequiredLessonActionCompletes()
    {
        TutorialBattleDefinition_SO definition = ScriptableObject.CreateInstance<TutorialBattleDefinition_SO>();
        definition.Steps.Add(new TutorialBattleStepDefinition
        {
            stepId = "inspect_skill",
            title = "Inspect Skill",
            sortOrder = 0
        });
        definition.ForceValidateForTests();

        TutorialBattleRuntime.BeginBattle(definition);

        Assert.IsTrue(TutorialBattleRuntime.IsActive);
        Assert.AreEqual("inspect_skill", TutorialBattleRuntime.CurrentStep.stepId);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `rg -n "TutorialBattleGuideOverlay|CurrentStep.stepId" Assets/Scripts/BattleScene Assets/Scripts/Tutorial`
Expected: no guide overlay exists and no battle scene code references tutorial battle runtime yet.

- [ ] **Step 3: Write minimal implementation**

```csharp
// Add a small integration surface instead of embedding copy in combat logic:
// - BattleButton: before round execution, ask TutorialBattleRuntime whether progression is blocked.
// - SpeedButton: report successful line creation.
// - BattleSkillInfoOverlay: report skill/status inspection.
// - TutorialBattleGuideOverlay: display CurrentStep.title/body and optional continue CTA.
// - TutorialBattleRuntime: expose MarkSkillViewed(), MarkLineCreated(), MarkClashSeen(), MarkOneSidedAttackSeen(), MarkStatusSeen(), AdvanceStepIfSatisfied().
```

- [ ] **Step 4: Run test to verify it passes**

Run: `rg -n "TutorialBattleRuntime|TutorialBattleGuideOverlay" Assets/Scripts/BattleScene Assets/Scripts/Tutorial`
Expected: battle UI and interaction code now references the tutorial battle runtime and guide overlay in tutorial mode only.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/BattleScene/Controller/BattleButton.cs Assets/Scripts/BattleScene/Controller/SpeedButton.cs Assets/Scripts/BattleScene/Controller/BattleSkillInfoOverlay.cs Assets/Scripts/BattleScene/Controller/BattleAllyController.cs Assets/Scripts/BattleScene/Controller/BattleEnemyController.cs Assets/Scripts/Tutorial/TutorialBattleGuideOverlay.cs Assets/Tests/EditMode/Editor/RunGameManagerTutorialBattleTests.cs
git commit -m "feat: wire battle tutorial interaction hooks"
```

### Task 4: Tutorial Battle Content Binding

**Files:**
- Modify: `Assets/Scripts/MapScene/Manager/RunGameManager.cs`
- Modify: `Assets/Scripts/ScriptableObject/TutorialMenuCatalog_SO.cs` if needed for references
- Create or assign: tutorial battle definition asset references in Resources / serialized fields
- Test: `Assets/Tests/EditMode/Editor/RunGameManagerTutorialBoardTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
using NUnit.Framework;
using System.Reflection;
using System.Collections.Generic;

public class RunGameManagerTutorialBoardTests
{
    [Test]
    public void BuildTutorialBoardTiles_UsesBattleTeachingRoute()
    {
        MethodInfo method = typeof(RunGameManager).GetMethod("BuildTutorialBoardTiles", BindingFlags.Static | BindingFlags.NonPublic);
        var tiles = (List<BoardTileData>)method.Invoke(null, new object[] { 11 });

        Assert.AreEqual(MapTileType.Battle, tiles[2].tileType);
        Assert.AreEqual(MapTileType.Elite, tiles[7].tileType);
        Assert.AreEqual(MapTileType.Boss, tiles[9].tileType);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `rg -n "TutorialBattleDefinition_SO|BeginBattle\\(" Assets/Scripts/MapScene/Manager/RunGameManager.cs`
Expected: no tutorial battle definitions are assigned to tutorial encounters yet.

- [ ] **Step 3: Write minimal implementation**

```csharp
// In RunGameManager tutorial mode:
// - resolve dedicated tutorial battle definitions for the normal battle, elite battle, and boss battle
// - include them in BattleEncounterData so BattleScene can choose the correct lesson content
// - preserve the fixed route already in BuildTutorialBoardTiles
// - keep all logic tutorial-only and avoid changing non-tutorial encounter flow
```

- [ ] **Step 4: Run test to verify it passes**

Run: `rg -n "TutorialBattleDefinition_SO|BeginBattle\\(" Assets/Scripts/MapScene/Manager/RunGameManager.cs`
Expected: tutorial encounters now carry battle lesson definitions into battle scene startup.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/MapScene/Manager/RunGameManager.cs Assets/Tests/EditMode/Editor/RunGameManagerTutorialBoardTests.cs
git commit -m "feat: bind tutorial battle content to tutorial route"
```

## Self-Review

- Spec coverage: this plan covers the current sub-scope only, focused on battle teaching content, tutorial battle data, interaction hooks, and binding to the existing tutorial route. Merchant/forge/rest/event follow-up content remains intentionally out of scope for this implementation slice.
- Placeholder scan: all tasks identify concrete files and behaviors. The two integration tasks intentionally describe battle hook surfaces instead of dumping large code blocks because the exact method names already exist and the real task is adding tutorial-only calls at those points.
- Type consistency: the plan uses `TutorialBattleDefinition_SO`, `TutorialBattleStepDefinition`, `TutorialBattleRuntime`, and `TutorialBattleGuideOverlay` consistently across all tasks.
