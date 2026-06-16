# Ability Tree Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a configurable meta-progression ability tree opened from the main menu, with unlock costs, prerequisites, persistent unlocks, and start-of-run buffs.

**Architecture:** Store ability definitions in `AbilityTreeConfig_SO`, persist unlocked node IDs separately from the existing `AbilityPointWallet`, and expose behavior through `AbilityProgressionService`. The main menu creates a runtime uGUI overlay from config data, while `RunGameManager` applies aggregated unlocked bonuses to new non-tutorial runs.

**Tech Stack:** Unity C#, ScriptableObject config, JsonUtility persistence, uGUI runtime overlay, NUnit EditMode tests, Unity custom inspector.

---

### Task 1: Core Progression Model

**Files:**
- Create: `Assets/Scripts/Shared/AbilityEffectType.cs`
- Create: `Assets/Scripts/Shared/AbilityNodeEffect.cs`
- Create: `Assets/Scripts/Shared/AbilityNodeDefinition.cs`
- Create: `Assets/Scripts/Shared/AbilityTreeConfig_SO.cs`
- Create: `Assets/Scripts/Shared/AbilityProgressionService.cs`
- Modify: `Assets/Scripts/Shared/AbilityPointWallet.cs`
- Test: `Assets/Tests/EditMode/Editor/AbilityProgressionServiceTests.cs`
- Test: `Assets/Tests/EditMode/Editor/AbilityPointWalletTests.cs`

- [ ] **Step 1: Write failing tests** for node state, prerequisite blocking, point spending, persistence, bonus aggregation, validation, and wallet spending.
- [ ] **Step 2: Run EditMode tests** and verify they fail because the new API does not exist.
- [ ] **Step 3: Implement the model and service** with only the behavior tested.
- [ ] **Step 4: Run EditMode tests** and verify the progression tests pass.

### Task 2: Main Menu Ability Overlay

**Files:**
- Create: `Assets/Scripts/MainMenu/AbilityMenuOverlay.cs`
- Modify: `Assets/Scripts/MainMenu/MainMenuSceneController.cs`
- Test: `Assets/Tests/EditMode/Editor/MainMenuSceneControllerTutorialTests.cs`

- [ ] **Step 1: Add tests** proving the menu action resolver recognizes `Ability`, `ability`, and `能力`.
- [ ] **Step 2: Add an Ability menu item** by cloning the same runtime pattern used for tutorial.
- [ ] **Step 3: Show `AbilityMenuOverlay`** from the new menu action, passing `AbilityTreeConfig_SO` and `AbilityPointWallet`.
- [ ] **Step 4: Ensure blocking overlay behavior** disables gallery input and Escape closes the overlay.

### Task 3: Apply Buffs To New Runs

**Files:**
- Modify: `Assets/Scripts/MapScene/Manager/RunGameManager.cs`

- [ ] **Step 1: Add an ability config field** to `RunGameManager`.
- [ ] **Step 2: Aggregate unlocked bonuses** when starting a normal run.
- [ ] **Step 3: Add starting money and action point limit bonuses** to the run initialization.
- [ ] **Step 4: Leave tutorial runs unchanged** for first version.

### Task 4: Custom Inspector

**Files:**
- Create: `Assets/Scripts/Editor/AbilityTreeConfigEditor.cs`

- [ ] **Step 1: Draw node list fields** for ID, name, description, cost, UI position, prerequisites, and effects.
- [ ] **Step 2: Add buttons** for validation, sample tree creation, missing ID generation, and invalid prerequisite cleanup.
- [ ] **Step 3: Use `AbilityProgressionService.ValidateTree`** so runtime and editor validation share rules.

### Task 5: Unity Verification

**Files:**
- Existing Unity assets and scripts.

- [ ] **Step 1: Run targeted EditMode tests** for wallet, ability progression, and main menu resolver.
- [ ] **Step 2: Check Unity console** for compile errors.
- [ ] **Step 3: If MCP is available, verify project state is `MobileGame@014fa41e060f5012`** before editor operations.
