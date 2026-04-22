# Tutorial Mode Design

## Goal

Add a `Tutorial` entry to the main menu and implement a dedicated tutorial run mode that reuses the existing `MapScene`, `BattleScene`, and `RestScene` flow while guiding the player through core systems with fixed content, step-by-step overlays, and enforced interaction checkpoints. The tutorial must always start from the beginning, must not use or overwrite normal continue saves, and must return to the main menu when completed, exited, or failed.

## Scope

This design covers:

- Main menu entry for tutorial mode
- Tutorial runtime state and lifecycle
- Fixed tutorial map generation and sequencing
- Data-driven tutorial step definitions
- Shared tutorial guide overlay and blocking behavior
- Battle tutorial hooks for key mechanics
- Tutorial-specific failure, exit, and completion rules

This design does not cover:

- Final tutorial writing polish for every line of copy
- Additional art assets beyond reusing existing UI patterns
- Separate tutorial-only battle or map scenes

## Design Principles

1. Reuse the production game loop wherever possible.
2. Keep tutorial content data-driven so step order and copy can change without rewriting logic.
3. Add tutorial-specific hooks around existing systems instead of forking entire systems.
4. Keep the tutorial linear and deterministic.
5. Prevent tutorial state from leaking into normal run saves and continue flow.

## User Experience Summary

The player selects `Tutorial` from the main menu. The game starts a tutorial run and loads `MapScene`. Instead of a random board, the player sees a fixed linear path:

`Spawn -> Empty -> Battle -> Event -> Rest -> Merchant -> Forge -> Elite -> Key -> Boss -> End`

Each segment introduces one or more mechanics. A tutorial guide overlay appears at controlled moments to explain what to do next. Important concepts such as clash, one-sided attack, clash power, attack power, skill keywords, and status interactions are taught inside real battle flow. The player must perform key actions manually. If the player fails a tutorial battle, the tutorial does not enter normal settlement and instead restarts from the current tutorial checkpoint. Exiting the tutorial returns to the main menu. Finishing the boss battle returns to the main menu and records only a lightweight completion flag.

## Architecture

### 1. Tutorial Runtime Layer

Add a dedicated runtime holder responsible for whether the game is currently inside tutorial mode and which tutorial definition and step are active.

Responsibilities:

- Start tutorial mode from the main menu
- Store active tutorial definition reference
- Store current step id / step index
- Track current tutorial checkpoint
- Expose helper queries such as `IsActive`, `CurrentStep`, `IsInputRestricted`, `CanUseNormalSave`
- Clear itself on completion, exit, or abort

This runtime layer should be independent from normal run save state and should not serialize to `MainMenuRunSaveSystem`.

### 2. Tutorial Definition Layer

Tutorial content should live in data assets instead of being embedded directly in control code.

Recommended asset structure:

- `TutorialDefinition_SO`
  - tutorial id
  - display name
  - fixed map tile list
  - tutorial step list
  - default start party / inventory
  - tutorial battle encounter references
  - optional completion flag key

- `TutorialStepDefinition`
  - `stepId`
  - `title`
  - `body`
  - `contextType`
  - `triggerType`
  - `completionType`
  - `inputPolicy`
  - `advanceMode`
  - optional target tile coord
  - optional target battle id
  - optional highlight target key
  - optional follow-up step id

The content asset should be the primary place for tutorial sequencing and copy changes.

### 3. Tutorial Coordinator Layer

Add a runtime coordinator that listens to map, battle, shop, forge, event, and rest interactions and decides when to show tutorial steps or mark them complete.

Responsibilities:

- React to scene transitions
- Bind to map and battle events
- Show or hide guide overlay
- Validate whether required player actions occurred
- Enforce restrictions for hard tutorial steps
- Advance the tutorial step state
- Trigger checkpoint save and restart behavior for tutorial battles

This coordinator should be an orchestration layer, not a container for game mechanics.

### 4. Tutorial Guide Overlay

Add a shared overlay that works in all relevant scenes.

Responsibilities:

- Show title and body copy
- Optional continue button
- Optional blocked input mode
- Optional soft highlight target binding
- Optional non-blocking hint mode

The overlay should follow the existing runtime-generated overlay style used in menu/event/settlement UI so it can be adjusted later without depending on scene-authored UI.

## Main Menu Integration

`MainMenuSceneController` should gain a new `Tutorial` action alongside `NewGame`, `Continue`, and `DemoTest`.

Expected behavior:

- Selecting `Tutorial` clears normal continue request state
- Clears runtime battle/rest/map state
- Starts tutorial runtime with the configured tutorial definition
- Loads `MapScene`

The tutorial entry should not affect `Continue` availability and should not create a normal save.

## Tutorial Map Design

### Map Shape

Use a fixed linear board rather than random generation. The board should still be represented as normal `BoardTileData` entries and loaded through the same `BoardManager.LoadBoard(...)` path used by restored runs rather than by special-casing `BoardManager.GenerateBoard(...)`.

This keeps tutorial content decoupled from random map generation and makes future step order changes cheap.

### Default Tutorial Route

Use this initial sequence:

- `Spawn`
- `Empty`
- `Battle`
- `Event`
- `Rest`
- `Merchant`
- `Forge`
- `Elite`
- `Key`
- `Boss`
- `End`

Implementation detail:

- `End` can be represented as tutorial completion logic attached to boss victory rather than a visible tile if that is simpler.
- The `Empty` tile exists to teach movement, selection, entering a tile, and action point consumption before more complex content begins.

### Tile Metadata

Tutorial tiles should support extra metadata beyond standard tile type:

- tutorial tile id
- fixed assigned enemy group
- fixed assigned event
- optional step id to trigger on entering
- optional gate requiring completion before movement continues
- optional checkpoint marker

This metadata should live in tutorial definition data rather than on the global `BoardTileData` type unless a minimal extension is clearly cleaner.

## Tutorial Run State

The tutorial still needs a `RunState`, but it should be initialized from tutorial defaults rather than normal difficulty start rules.

Requirements:

- Fixed starting party suited to teaching battle
- Fixed starting inventory suitable for merchant / forge / rest lessons
- Normal systems such as relics and calamities should remain compatible, but tutorial can start with a minimal controlled subset
- Tutorial checkpoints should capture enough data to restart the current lesson without writing a normal save

The recommended approach is to keep using `MapRunRuntime`-style in-memory snapshots for tutorial checkpoints while skipping `MainMenuRunSaveSystem`.

## Scene Flow

### Map Scene

`RunGameManager` should detect tutorial mode early and branch into tutorial initialization.

Tutorial mode behavior in map scene:

- Build run state from tutorial definition
- Build fixed board from tutorial definition
- Assign fixed enemy groups and events from tutorial data
- Initialize player position and UI as normal
- Suppress normal save persistence and normal continue restoration
- Register tutorial-specific map event callbacks

### Battle Scene

`BattleSceneEncounterBootstrap` should continue to consume `BattleEncounterRuntime` normally. Tutorial behavior should come from attached runtime context, not from a separate scene.

Battle scene tutorial logic should:

- Detect the current tutorial battle phase
- Show onboarding overlay on entry
- Emit tutorial events as the player interacts
- Restrict advancement until mandatory interactions occur
- Mark checkpoints before important tutorial battles

### Rest Scene

`RestSceneRuntime` should continue to be used, but tutorial mode should overlay step validation on top of existing recruit / restore / reforge flows.

Tutorial rules:

- The required tutorial rest action must be completed before the step advances
- Exiting the scene early returns to the current tutorial map checkpoint or main menu depending on chosen exit path

## Tutorial Step System

### Trigger Types

Tutorial steps should be able to react to concrete runtime events such as:

- entered map scene
- selected tile
- moved to tile
- opened event overlay
- confirmed event option
- opened merchant overlay
- bought merchant item
- refreshed merchant offers
- opened forge overlay
- completed forge craft
- entered rest scene
- completed rest action
- battle scene ready
- targeting phase started
- enemy button clicked
- connection created
- clash resolved
- one-sided attack resolved
- skill info viewed
- status info viewed
- status applied
- battle won
- battle lost
- tutorial exit requested

### Completion Types

Tutorial steps should support completion conditions such as:

- press continue
- perform action once
- perform action on required target
- win current battle
- inspect required UI
- purchase any valid item
- craft required item
- complete current scene objective

### Input Policies

Support three policies:

- `Informational`: show guidance but do not block gameplay
- `ModalBlock`: block gameplay until user acknowledges the step
- `RestrictedAction`: allow only required actions until the step completes

`RestrictedAction` should be used sparingly, mainly for first-time critical lessons.

## Battle Tutorial Design

### Teaching Goals

Battle tutorial must explicitly teach:

- speed
- target selection / line creation
- clash
- one-sided attack
- clash power
- attack power
- skill details
- important skill keywords
- status-related concepts

### Battle Lesson Structure

Use at least three tutorial battles:

1. **Opening battle**
   - Introduce buttons, targeting, clash, one-sided attack, clash power, attack power
   - Require the player to create at least one valid connection manually

2. **Follow-up battle**
   - Introduce skill details, important keywords, and status effects
   - Require at least one skill info inspection before round resolution

3. **Boss tutorial battle**
   - Act as a recap and completion check

### Battle Hooks

Add non-invasive tutorial event emission around existing battle systems:

- `BattleButton`
  - battle scene ready
  - targeting phase started
  - clash resolved
  - one-sided attack resolved
  - battle won
  - battle lost

- `SpeedButton`
  - enemy clicked
  - connection created / changed / removed

- `BattleSkillInfoOverlay` or related presenter
  - skill info opened
  - status / keyword info viewed

- `BattleEnemyController` and `BattleAllyController`
  - status applied or key mechanic surfaced where needed

These hooks should emit tutorial events into the coordinator rather than embedding tutorial branching directly inside combat resolution code.

### Battle Restrictions

Examples of allowed restrictions:

- Prevent progressing to combat resolution until a required tutorial action has been performed
- Require skill detail inspection before first round execution
- Show an explanatory modal immediately after the first clash
- Show an explanatory modal immediately after the first one-sided attack

The system should not auto-play required actions for the player. The player must still perform them.

## Merchant, Forge, Event, and Rest Lessons

### Event

Tutorial event should use a fixed event assignment and teach:

- reading event text
- reviewing option preview / consequence preview
- confirming an option

### Merchant

Tutorial merchant step should teach:

- price display
- buying an item
- optionally refreshing stock if tutorial inventory and pacing support it

The step definition should decide whether refresh is mandatory or only explained.

### Forge

Tutorial forge step should teach:

- blueprint requirement
- material requirement
- crafting result

The tutorial run should guarantee the player has the needed blueprint/materials by the time this tile is reached.

### Rest

Tutorial rest should teach at least one actual rest action through the real rest scene flow. Recruitment and reforge can also be included if the current tutorial definition wants them, but the initial implementation should require at least one concrete successful rest interaction rather than only visiting the scene.

## Failure, Exit, and Completion Rules

### Failure

Tutorial battle loss should not flow into normal defeat progression.

Instead:

- battle loss triggers tutorial failure handling
- the current tutorial checkpoint is restored
- the player is returned to the current tutorial segment or battle pre-state

### Exit

If the player exits tutorial mode from a tutorial menu or failure prompt:

- clear tutorial runtime
- clear tutorial temporary checkpoints
- clear battle/rest/map runtime leftovers
- return to main menu

### Completion

On boss tutorial victory:

- mark tutorial as complete in a lightweight persistent flag store
- clear tutorial runtime
- return to main menu

The completion flag should be distinct from run save data. It can be stored in a tiny separate settings/progress file or in existing menu settings persistence if that is clean and scoped.

## Save and Persistence Rules

Tutorial mode must not:

- write to `MainMenuRunSaveSystem`
- create a `Continue` snapshot
- restore from `Continue`

Tutorial mode may:

- use in-memory tutorial checkpoint snapshots
- write a simple `tutorialCompleted` flag

## Testing Strategy

### Unit / Logic-Level Tests

Add tests for:

- tutorial runtime start / clear / checkpoint rules
- tutorial fixed board creation
- tutorial step trigger and completion transitions
- tutorial exclusion from normal save flow

### Integration / Scene-Level Verification

Manually verify:

1. Main menu shows `Tutorial`
2. Tutorial always starts from the beginning
3. Tutorial does not enable normal continue flow
4. Fixed map order matches tutorial definition
5. Each tile type appears and teaches its intended function
6. Battle lessons require real user interaction
7. First clash explanation appears at the right time
8. First one-sided attack explanation appears at the right time
9. Skill/keyword/status guidance appears in battle
10. Tutorial battle loss restarts from tutorial checkpoint rather than normal defeat flow
11. Completing boss returns to main menu
12. Tutorial exit returns to main menu without leftover runtime state

## File Impact Summary

Expected modified areas:

- `Assets/Scripts/MainMenu/MainMenuSceneController.cs`
- `Assets/Scripts/Shared/MainMenuRuntime.cs` or adjacent runtime entry point if tutorial flag belongs elsewhere
- `Assets/Scripts/MapScene/Manager/RunGameManager.cs`
- `Assets/Scripts/MapScene/Manager/BoardManager.cs` only if a minimal helper is needed
- `Assets/Scripts/BattleScene/Controller/BattleButton.cs`
- `Assets/Scripts/BattleScene/Controller/SpeedButton.cs`
- `Assets/Scripts/BattleScene/Controller/BattleSkillInfoOverlay.cs` or related viewer
- `Assets/Scripts/RestScene/Manager/RestSceneRuntime.cs`

Expected new files:

- tutorial runtime holder
- tutorial definition asset type
- tutorial coordinator
- tutorial guide overlay
- tutorial fixed board builder / loader helper
- tutorial event / trigger enums
- tests for tutorial runtime and step progression

## Open Choices Resolved

The following product decisions are fixed by this design:

- Tutorial route starts from the beginning every time
- Tutorial does not use normal save / continue
- Tutorial failure restarts from tutorial checkpoint
- Tutorial completion returns to the main menu
- Tutorial content is data-driven
- Tutorial reuses existing scenes instead of adding a separate tutorial-only scene
- Initial default route is linear with all major tile types represented

## Implementation Recommendation

Implement in this order:

1. Main menu tutorial entry and tutorial runtime bootstrapping
2. Fixed tutorial map loading in `MapScene`
3. Tutorial overlay and step engine
4. Map/event/merchant/forge/rest triggers
5. Battle tutorial hooks and restrictions
6. Failure/completion/return-to-menu flow
7. Tests and manual verification pass
