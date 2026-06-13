# Run Settlement Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a full run settlement flow that awards ability points after Boss victory or accepted defeat, while tracking run statistics and keeping score details out of the player UI.

**Architecture:** Add serializable run-stat facts to `RunState`, calculate score through a pure `RunScoreService`, and pass one immutable settlement payload through `RunSettlementRuntime` into a dedicated settlement scene/controller. Existing map and battle flows only collect facts at source events and call the new end-run service when the run is over.

**Tech Stack:** Unity 2022.3, C#, Unity Test Framework EditMode tests, uGUI scene/controller, local JSON persistence through `Application.persistentDataPath`.

---

### Task 1: Scoring Model And Pure Score Service

**Files:**
- Create: `Assets/Scripts/Shared/RunSettlementStats.cs`
- Create: `Assets/Scripts/Shared/RunScoreConfig_SO.cs`
- Create: `Assets/Scripts/Shared/RunSettlementScoreResult.cs`
- Create: `Assets/Scripts/Shared/RunScoreService.cs`
- Modify: `Assets/Scripts/MapScene/Manager/RunState.cs`
- Test: `Assets/Tests/EditMode/Editor/RunScoreServiceTests.cs`

- [ ] Write failing EditMode tests for score categories, short-run multiplier, failed calamity exclusion, and ability point division.
- [ ] Run `RunScoreServiceTests` and verify they fail because the new types do not exist.
- [ ] Implement the stats model, config defaults, result object, and score service.
- [ ] Add `RunState.settlementStats`, initialize it, and clone it in `RunState.Clone()`.
- [ ] Run `RunScoreServiceTests` and verify they pass.

### Task 2: Ability Point Wallet

**Files:**
- Create: `Assets/Scripts/Shared/AbilityPointWallet.cs`
- Create: `Assets/Scripts/Editor/AbilityPointWalletEditor.cs`
- Test: `Assets/Tests/EditMode/Editor/AbilityPointWalletTests.cs`

- [ ] Write failing tests using a temporary wallet path for get, add, set, clear, and clamping negative inputs to zero.
- [ ] Run `AbilityPointWalletTests` and verify they fail because `AbilityPointWallet` does not exist.
- [ ] Implement JSON persistence with a version field and injectable path for tests.
- [ ] Add editor menu items: show current points, clear points, set points window.
- [ ] Run `AbilityPointWalletTests` and verify they pass.

### Task 3: Run State Save/Load And Statistic Collection

**Files:**
- Modify: `Assets/Scripts/MainMenu/MainMenuRunSaveSystem.cs`
- Modify: `Assets/Scripts/BattleScene/Manager/BattleEncounterRuntime.cs`
- Modify: `Assets/Scripts/MapScene/Manager/MapRunRuntime.cs`
- Modify: `Assets/Scripts/MapScene/Manager/RunGameManager.cs`
- Modify: `Assets/Scripts/Shared/MapMovementService.cs` only if movement result data is needed
- Modify: `Assets/Scripts/ScriptableObject/SpecialDungeon_SO.cs`
- Test: `Assets/Tests/EditMode/Editor/RunSettlementStatsPersistenceTests.cs`
- Test: `Assets/Tests/EditMode/Editor/MapRunRuntimeSettlementStatsTests.cs`

- [ ] Write failing tests for `RunState.Clone()` preserving stats and saved run data preserving stats.
- [ ] Write failing tests for `MapRunRuntime.ApplyEncounterResult()` incrementing normal, elite, event battle, and Boss counters only on victory.
- [ ] Run the new tests and verify expected failures.
- [ ] Serialize stats through `MainMenuRunSaveSystem`.
- [ ] Mark event-created encounters with `isEventEncounter`.
- [ ] Increment unexplored tile, event tile, key, craft, money gained, special dungeon, and battle counters at their source events.
- [ ] Add `SpecialDungeon_SO.settlementScoreValue = 200`.
- [ ] Run the new tests and verify they pass.

### Task 4: Settlement Runtime And End-Run Flow

**Files:**
- Create: `Assets/Scripts/Shared/RunSettlementData.cs`
- Create: `Assets/Scripts/Shared/RunSettlementRuntime.cs`
- Create: `Assets/Scripts/Shared/RunSettlementFlowService.cs`
- Modify: `Assets/Scripts/BattleScene/Controller/BattleSettlementOverlay.cs`
- Modify: `Assets/Scripts/BattleScene/Manager/BattleEncounterRuntime.cs` if return scene/config fields are needed
- Test: `Assets/Tests/EditMode/Editor/RunSettlementFlowServiceTests.cs`

- [ ] Write failing tests proving settlement flow awards ability points once, clears run/runtime state, deletes saved runs via injected callbacks, and stores settlement data.
- [ ] Run `RunSettlementFlowServiceTests` and verify failure because the flow service does not exist.
- [ ] Implement settlement data/runtime and flow service with injectable callbacks for tests.
- [ ] Change accepted defeat to transition to `RunSettlementScene`.
- [ ] Change Boss victory return to apply the result, build settlement with `runCleared = true`, and transition to `RunSettlementScene`.
- [ ] Keep non-Boss victory and special defeat-return behavior unchanged.
- [ ] Run settlement flow tests and existing `BattleExitFlowServiceTests`.

### Task 5: Settlement Scene And Player UI

**Files:**
- Create: `Assets/Scripts/RunSettlementScene/RunSettlementSceneController.cs`
- Create/modify Unity scene: `Assets/Scenes/RunSettlementScene.unity`
- Modify build settings to include `RunSettlementScene`
- Test: `Assets/Tests/EditMode/Editor/RunSettlementSceneControllerTests.cs` if UI text binding can be tested without a scene

- [ ] Create a controller that binds `RunSettlementRuntime.Current` into the approved left-summary/right-stats layout.
- [ ] Ensure the UI contains no debug panel, debug button, debug explanation text, or per-category score values.
- [ ] Add one `返回主菜单` button that returns to `Menu Selection`.
- [ ] Create the scene with Camera, EventSystem, Canvas, and controller.
- [ ] Add scene to build settings.
- [ ] Open/validate the scene through Unity and check the console for errors.

### Task 6: Verification Pass

**Files:**
- No new files unless fixes are required.

- [ ] Run focused EditMode tests: `RunScoreServiceTests`, `AbilityPointWalletTests`, `RunSettlementStatsPersistenceTests`, `MapRunRuntimeSettlementStatsTests`, `RunSettlementFlowServiceTests`, and `BattleExitFlowServiceTests`.
- [ ] Run a broader EditMode pass if focused tests pass and time allows.
- [ ] Validate changed C# scripts or check Unity console after compilation.
- [ ] Summarize known limitations and manual test steps for Boss victory and accepted defeat.
