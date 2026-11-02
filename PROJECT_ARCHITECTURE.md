# Project Architecture Guide

## Purpose

This document is a quick orientation guide for new developers joining this Unity project.

It focuses on:
- where the major gameplay systems live
- which scripts are the main entry points
- where to look when changing a specific feature
- how runtime state moves between the map scene and battle scene

This is not a full API reference. It is a practical "where should I look first?" guide.

## Agent Project Conventions

All agents working in this repository must follow [AGENTS.md](AGENTS.md).
Adding enemies or relics, or changing their documented skills, stats or effects, requires matching detailed catalog updates under `outputs/enemy-catalog` or `outputs/relic-catalog` in the same task. Preserve the established formats and keep the text, visual documents, JSON indexes and snapshot metadata consistent; see `AGENTS.md` for the required fields and completion criteria.

## High-Level Structure

Main code and data are split into these areas:

- `Assets/Scripts/MapScene`
  - world map exploration, tile interaction, inventory, crafting, merchant, run state
- `Assets/Scripts/BattleScene`
  - battle flow, combat resolution, effects, status system, skill UI, settlement
- `Assets/Scripts/ScriptableObject`
  - shared gameplay data definitions such as enemies, groups, loot, merchants, blueprints
- `Assets/Data`
  - actual game content assets created from the scriptable object classes
- `Assets/Scripts/Shared`
  - scene transition and shared utility scripts

## Runtime Flow

Core runtime loop:

1. `MapScene` starts through `RunGameManager`.
2. `BoardManager` builds the board and binds tile data.
3. `RunState` stores the current run: party, inventory, money, AP, keys, map progress.
4. Entering a hostile tile packages encounter data into `BattleEncounterRuntime`.
5. `BattleSceneEncounterBootstrap` reads that runtime data and spawns allies and enemies.
6. `BattleButton` resolves combat.
7. Rewards, HP, EXP, money, and cleared tile state are written back into map runtime state.
8. Returning to the map restores the previous board instead of rebuilding from scratch.

## Main Folders

### `Assets/Scripts/MapScene/Manager`

This folder contains the map-side game logic.

Important scripts:

- `RunGameManager.cs`
  - top-level controller for the map loop
  - owns current `RunState`
  - coordinates map startup, entering tiles, scene transitions, crafting, merchant, inventory refresh
  - first place to inspect when changing overall run flow

- `RunState.cs`
  - persistent run data model
  - stores party members, HP, EXP, levels, inventories, blueprints, money, AP, keys
  - edit this when a new resource or inventory category needs to survive scene changes

- `MapRunRuntime.cs`
  - temporary runtime bridge that keeps the current map state alive when entering battle and returning
  - edit this when new map-side state must survive battle transitions

- `BoardManager.cs`
  - board generation and tile visuals
  - decides how tiles appear, collapse, persist, or stay visible after being visited
  - edit this when adding new tile types or changing tile presentation rules

- `BoardTileData.cs`
  - runtime tile model
  - stores tile type, coordinates, visited state, encounter info, merchant data, etc.
  - edit this when a tile needs new persistent per-tile data

- `DifficultyConfig_SO.cs`
  - board-size and tile-weight configuration for each difficulty
  - edit this when changing generation weights, AP, key counts, special tile ratios

- `TileResolver.cs`
  - resolves what happens when the player enters a tile
  - battle, event, rest, key, forge, merchant, boss
  - edit this when changing tile-enter behavior

- `PlayerMapController.cs`
  - map pawn movement and movement animation
  - edit here when changing movement feel, jump, landing effects, or map pawn visuals

- `ActionPointSystem.cs`
  - action point tracking and round refresh behavior
  - edit this when changing AP formulas

- `MerchantRuntimeData.cs`
  - persistent merchant inventory data once a merchant tile has been visited
  - edit this when extending merchant stock behavior

- `MapSceneBootstrap.cs`
  - helper bootstrap for map scene setup

### `Assets/Scripts/MapScene/Controller`

This folder contains map UI and presentation.

Important scripts:

- `MapHUD.cs`
  - main map HUD
  - top-left player info, money, bottom-right tile detail, inventory tabs, bag UI
  - many UI hooks live here: sprites, fonts, offsets, colors
  - first place to inspect for map-side UI layout issues

- `MapInventoryDetailView.cs`
  - runtime inventory item detail panel on the map
  - used for loot, weapons, blueprints

- `MapCraftingOverlay.cs`
  - forge UI
  - blueprint selection, material requirement display, crafting button logic

- `MapMerchantOverlay.cs`
  - merchant UI
  - shows generated merchant stock and delegates buying / refresh to the existing callbacks and purchase service
  - `MapMerchantOverlay.View.cs` builds the forest-green / brass modal, two-column stock cards, independent detail scrolling, fixed purchase summary, and safe-area layout
  - `MapMerchantFrameGraphic.cs` and `MapMerchantCardGraphic.cs` draw icon wells, layered brass edges, badges, ornaments, textured surfaces, and selection states
  - item names in cards and details retain their actual rarity colours across selection and sold-out states; visual handoff: `docs/visuals/merchant-panel-ui.md`

- `MapViewController.cs`
  - map camera input
  - drag, zoom, mobile touch handling, intro camera movement

- `MapBackgroundController.cs`
  - runtime-generated decorative background and particles for the map scene

- `BoardTileView.cs`
  - visual wrapper for one tile
  - icon, frame, glow, selection state, visited state visuals

### `Assets/Scripts/BattleScene/Manager`

This folder contains battle-scene startup and managers.

Important scripts:

- `BattleSceneEncounterBootstrap.cs`
  - battle scene spawn pipeline
  - spawns allies, enemies, speed buttons, weapons, click handlers
  - applies materials, scaling, offsets, and anchor alignment
  - first place to inspect when spawn position or spawn-time visuals are wrong

- `BattleEncounterRuntime.cs`
  - runtime container passed from map to battle
  - stores party state, encounter enemy group, reward context, scene target data

- `BattleManager.cs`
  - battle-scene object reference hub
  - holds roots, spawn anchors, materials, shared references such as the level table

- `GenericManager.cs`
  - generic helper manager used by the battle scene

### `Assets/Scripts/BattleScene/Controller`

This folder contains almost all combat resolution and battle UI.

Important scripts:

- `BattleButton.cs`
  - central battle flow script
  - targeting phase, round execution, clash logic, damage, healing, statuses, rewards
  - if combat behavior is wrong, start here
  - this is the main script for skill trigger timing

- `BattlePresentationSystem.cs`
  - camera closeups, movement, clash effects, hit effects, custom effect prefabs, status tick effects
  - edit this for combat presentation and cinematic timing

- `BattleCloseupVisibility.cs`
  - per-shot spectator renderer suppression, including weapons and newly spawned visuals; restores original culling flags on replacement, completion, disable and destruction
  - presentation owns the lifetime; feedback hides spectator unit UI and contact shadows follow the same visual state without disabling combatants

- `BattleFeedbackSystem.cs`
  - health bars, status icons, status tooltips, damage popups, heal popups, death feedback
  - edit this for in-battle UI feedback

- `BattleHealthBarLook.cs`
  - presentation of the existing health/bar-buffer rectangles and numeric text; retains configured team fill colors
  - minimum readable dimensions, metal sockets and owned TMP outlines without changing HP or status input

- `BattleHudLook.cs` / `BattleHudOrnament.cs`
  - safe-area energy/round layout, sliced painted panel sprites and native UGUI glass/gem geometry
  - Resources/BattleUI/ForestEnergyPlate and ForestRoundPlate are blank backgrounds; text and energy segments remain dynamic
  - sliced-frame scaling accounts for Image.pixelsPerUnit as well as imported sprite height, preserving the caps after texture resizing
  - shared energy services still own reservations and temporary/overflow segments; decorations do not block input

- `BattleActionButtonLook.cs`
  - applies the two Resources/BattleUI ready-button sprites through the existing sprite switcher; restores original art and rect when disabled
  - proportional safe-area layout with bottom clearance; hides only the button's canvas group while BattleButton.IsResolvingCombat is true and restores its previous visibility/input flags afterward
  - BattleButton exposes a read-only resolving flag; action execution and input handlers retain their existing owners

- `BattleSettlementOverlay.cs`
  - victory/defeat settlement UI
  - EXP animation, loot list, party HP summary, retry/return buttons

- `BattleSkillInfoOverlay.cs`
  - reusable detail overlay
  - used for ally skill switching, enemy skill info, enemy detail info
  - supports scrolling content for long descriptions and passives

- `SpeedButton.cs`
  - one speed slot interaction unit
  - drag targeting, long press, enemy skill inspection, ally skill switching trigger

- `BattleAllyController.cs`
  - runtime ally combat state
  - HP, statuses, healing, damage intake, weapon binding, selected skill

- `BattleEnemyController.cs`
  - runtime enemy combat state
  - HP, statuses, conditional skills, multi-slot skill selection, passive rule tracking

- `BattleStatusRuntimeEntry.cs`
  - runtime status stack container

- `BattleActorVisualUtility.cs`
  - utility for visual bounds and effect anchor calculations
  - used by closeup framing and effect positioning
  - resolves actual actor roots through runtime spawn containers; also supports visual-only preview actors

- `BattleActorLightingController.cs`
  - forest illustration surface lighting based on current sprite world bounds
  - owns temporary per-sprite materials, preserves external overrides and releases them on disable or preview cleanup
  - changes visual shading only; see `docs/visuals/forest-battle-look.md`

- `Assets/Assets/Shaders/BattleForestSpriteLighting.hlsl` / `Lit.shader`
  - broad painted surface normals, warm key and cool/ground indirect fill for forest actor materials
  - indirect fill uses a saturating luminance response gated by forest surface weight; no added texture samples or passes
  - forest pigment response lifts low/mid albedo with hue-preserving gain and peak headroom, while reducing the legacy fixed dark floor to recover fold contrast
  - `_ForestPigmentStrength` defaults to 1 but is gated by the forest/surface weight; 0 reproduces the prior response, with identical fields in all four material buffers
  - `_ForestHighlightStrength` defaults to 1 and gates a hue-preserving HDR shoulder above peak 0.9 before scene tonemapping; dark lighting and the separate narrow alpha-edge light retain their existing response
  - shared lighting guards zero brightness before normalization so pure black does not produce NaN
  - the two-sided sprite ShadowCaster faces its bias normal toward the light before URP ApplyShadowBias, avoiding backlit self-shadow stripes while retaining alpha-clipped and mirrored cast silhouettes

- `BattleLookController.cs`
  - per-camera actor focus, aspect-aware overview framing and the sharp world-UI overlay camera
  - overview Gaussian focus includes the assigned midground tree/ruin renderer, then falls off over twice the actor-focus distance; distant scenery softens while the midground stays readable
  - desktop High closeups retain actor-focused Bokeh; mobile/Medium closeups use Gaussian. Per-camera focus updates keep scenery out of closeup focus
  - saved overview pitch is now 3 degrees; the authoring helper and foreground heights match this shallow battle-lane composition, while the distant painting retains its authored world pose

- `BattleForestStageController.cs`
  - owns decorative terrain and the forest lighting/opening shader globals, restoring earlier values on release
  - shares projected canopy-opening coordinates with `BattleLookPreviewTools` so floor/actor light pools and authored shafts agree
  - saved/default forest-lighting strength 0.82 softens the warm/cool light-pool contrast; the cinematic profile uses exposure 0.18, contrast 9 and temperature 3

- `Assets/Assets/Shaders/BattleForestFoliage.shader`
  - preserves the authored cutout/depth and breeze, with optional painted shadow fill; zero alpha keeps other foliage materials neutral
  - the ancient-tree layer uses `SunlitCanopyTerracesDaylight.png`, position (0.2, 7.00425, 12), scale (1.65, 1.85, 1.65) and bottom height -1.7; broader daylight masses and subtler spiral marks replace the earlier orange-edged art
  - keep its saved scene, Forest Foliage material and `BattleLookPreviewTools` composition/fill settings aligned; original art remains available
  - the shared ancient-tree/outer-grove material uses highlight gain 1 and olive-grey painted shadow fill (0.36, 0.40, 0.32, 0.30); near and margin foliage retain their separate materials
  - both near foreground clusters and both outer groves use `MossFernForegroundLow.png`, a low painted moss/fern/blue-mushroom cluster; `BattleLookPreviewTools.ForegroundPath` must match all four saved SpriteRenderer references
  - this sibling retains the original 1536x1024 dimensions, PPU100, world bounds and import settings; near softness/cutout are 6/0.5 and near material tint is (0.84, 0.90, 0.78), reducing the blue mushrooms' visual weight. Original `MossFernForeground.png` is preserved; built-in image edit inputs and exact prompts are in `docs/visuals/forest-low-foreground-prompt.txt` and `docs/visuals/forest-canopy-daylight-prompt.txt`

- `Assets/Assets/Shaders/BattleForestSurface.hlsl`
  - stone terrace and woodland share world-space paving scale, earthen-island mask and lighting across their join
  - `ForestTerracePavingSoft.png` supplies quieter painted stone faces; the previous paving asset remains available
  - both saved surface materials and `BattleLookPreviewTools` use paving scale 0.12 and growth 0.62, reducing slab size and increasing local earth/moss openings; growth is a noise-mask parameter, not a coverage percentage. Shader defaults 0.07/0 retain the earlier unbroken layout
  - both use paving texture weight 0.8 and stone color (0.50, 0.47, 0.36), softening the painted paving contrast; keep all shared top-surface colors, textures and scalar values identical across the terrace/woodland join now that the fallback color contributes
  - the stone shader uses `_LowerStepGain` (default 1, saved terrace material 0.35) below the combat plane, blending across world heights -0.24 to 0.045; the combat-plane lighting remains unchanged

- `Assets/Assets/Shaders/BattleForestBackdrop.shader`
  - mirrored distant painting, local aerial perspective and feathered painted waterfall flow
  - the saved backdrop is shifted 12 world units left at its original depth, rotation and scale, exposing a cascade through the twin-tree opening; keep that composition offset in `BattleLookPreviewTools`
  - waterfall UV regions belong to the current painting; keep its material and `BattleLookPreviewTools` settings aligned when replacing it

- `Assets/Assets/Shaders/BattleForestAtmosphere.shader`
  - mode 0 adds optional `_ShaftStriation` (default 0): three unequal soft ribbons with spacing seeded from each shaft's object origin, retaining the existing longitudinal fade and depth intersection. Camera cuts do not reseed that spacing
  - `Canopy Light Shafts.mat` and `BattleLookPreviewTools` use detail 0.6 and alpha 0.18; zero detail reproduces the old broad shape. The same four meshes, sun alignment and shared ground-opening coordinates are retained; extra ALU cost remains unmeasured on devices
  - modes 0/1/2 retain shafts, low mist and dust; mode 3 draws a curved, feathered leaf silhouette with two lit facets and a vein, using existing depth softness
  - leaf flutter phase uses stable particle vertex blue, so camera cuts do not reseed it; no leaf texture is required
  - `BattleLookPreviewTools.AddFallingLeaves` authors the saved `Sunlit drifting leaves` system and `Sunlit Falling Leaves.mat`: world simulation, seed 20261101, prewarm, 0.75/s, lifetime 10–14s and a 12-particle cap
  - this adds a particle renderer and leaf-branch work; device performance remains to be measured

- `BattleContactShadowController.cs`
  - independent soft ground ellipses for each actual combatant, including units inside spawn containers
  - follows movement and visibility; releases runtime shadow geometry on disable or preview cleanup

- `BattleEnemyDetailClickHandler.cs`
  - click-to-open enemy detail panel on battle sprites

- `AllyController.cs`
  - lower-level ally battle representation and weapon hook handling

### `Assets/Scripts/BattleScene/Equipments`

Weapon and armor runtime/data classes.

Important scripts:

- `Weapon_SO.cs`
  - weapon data
  - prefab reference, icon, weapon category, skills

- `WeaponCatalog_SO.cs`
  - weapon catalog and weighted selection list

- `Weapon.cs`
  - runtime weapon component used on ally prefabs

- `Armor.cs`
  - armor runtime base class

### `Assets/Scripts/ScriptableObject`

Shared data definitions used by `Assets/Data`.

Important scripts:

- `EnemyData_SO.cs`
  - one enemy definition
  - stats, sprite, prefab info, visual scale/offset, descriptions, passives, conditional skill rules

- `EnemyGroup_SO.cs`
  - one encounter group
  - list of enemies, group attributes, loot tables

- `EnemyRoster_SO.cs`
  - encounter pool selector for battle tiles
  - standard and elite groups by danger band, separate boss groups

- `LootAttribute_SO.cs`
  - extensible loot/enemy attribute definition
  - includes display color

- `LootItem_SO.cs`
  - loot item definition

- `LootTable_SO.cs`
  - weighted loot entries per table

- `CraftingBlueprint_SO.cs`
  - forge recipe data
  - result weapon, material list, rarity, icon, description

- `MerchantCatalog_SO.cs`
  - all merchant-sellable goods
  - supports loot, weapons, blueprints, with min/max price and weight

- `MoneyRewardTable_SO.cs`
  - money reward ranges by tile type and danger band

### `Assets/Data`

This is the content layer, not just code.

Important subfolders:

- `Assets/Data/Battle`
  - battle status assets such as Burn, Bleed, Current, Swiftness, Vulnerable, Erode

- `Assets/Data/Enemies/Enemies`
  - enemy data assets and their skills/effect prefabs

- `Assets/Data/Enemies/EnemyGroup`
  - encounter group assets and roster asset

- `Assets/Data/Enemies/Skill`
  - shared enemy skill data definitions

- `Assets/Data/Loots`
  - loot attributes, loot items, loot tables

- `Assets/Data/Weapons`
  - weapon assets and catalog

- `Assets/Data/Scroll`
  - current blueprint assets

- `Assets/Data/Economy`
  - merchant catalog and money reward table

- `Assets/Data/Game`
  - level table and difficulty configs

- `Assets/Data/Characters`
  - ally character data assets

## Key Data Models

### Party and Run Data

Relevant files:

- `RunState.cs`
- `CharacterData_SO.cs`
- `Weapon_SO.cs`
- `LevelTable_SO.cs`

This layer controls:
- party members
- current HP / max HP
- level / EXP
- equipped weapon
- inventories
- AP / money / keys

### Encounter Data

Relevant files:

- `EnemyRoster_SO.cs`
- `EnemyGroup_SO.cs`
- `EnemyData_SO.cs`
- `BattleEncounterRuntime.cs`

This layer controls:
- which enemy group appears on a map tile
- what battle scene spawns
- what rewards are attached to that battle

### Skill Data

Relevant files:

- `EnemySkill_SO.cs`
- `BattleButton.cs`
- `BattleSkillInfoOverlay.cs`

This layer controls:
- dice ranges
- trigger timing
- damage effects
- status effects
- delayed status application
- healing effects
- power modifiers
- status bursts
- status stack operations

### Status Data

Relevant files:

- `BattleStatus_SO.cs`
- `BattleStatusRuntimeEntry.cs`
- `BattleAllyController.cs`
- `BattleEnemyController.cs`

This layer controls:
- stackable statuses
- start-of-round behavior
- end-of-round decay
- on-hit behavior
- damage amplification
- linked-status generation
- status icon and tick effect presentation

## Common "Where To Change It" Guide

### Add a new tile type

Check:

- `BoardTileData.cs`
- `DifficultyConfig_SO.cs`
- `BoardManager.cs`
- `TileResolver.cs`
- `MapHUD.cs`

### Add a new enemy status

Check:

- create a new `BattleStatus_SO` asset in `Assets/Data/Battle`
- `BattleStatus_SO.cs`
- `BattleAllyController.cs`
- `BattleEnemyController.cs`
- `BattleButton.cs`
- `BattleSkillInfoOverlay.cs`

### Add a new skill effect or trigger

Check:

- `EnemySkill_SO.cs`
- `BattleButton.cs`
- `BattleSkillInfoOverlay.cs`

### Add a new reward resource

Check:

- `RunState.cs`
- `MapRunRuntime.cs`
- `BattleEncounterRuntime.cs`
- `BattleButton.cs`
- `MapHUD.cs`

### Change battle camera or hit effects

Check:

- `BattlePresentationSystem.cs`
- `BattleActorVisualUtility.cs`

### Change HP bars, status icons, damage popups, healing popups

Check:

- `BattleFeedbackSystem.cs`

### Change ally or enemy spawn placement

Check:

- `BattleSceneEncounterBootstrap.cs`
- `BattleManager.cs`

### Change settlement screen

Check:

- `BattleSettlementOverlay.cs`
- `BattleButton.cs`

### Change map HUD, bag, tile detail panel, fonts, or sprite hooks

Check:

- `MapHUD.cs`
- `MapInventoryDetailView.cs`

### Change crafting behavior

Check:

- `CraftingBlueprint_SO.cs`
- `MapCraftingOverlay.cs`
- `RunState.cs`
- `RunGameManager.cs`

### Change merchant behavior

Check:

- `MerchantCatalog_SO.cs`
- `MerchantRuntimeData.cs`
- `MapMerchantOverlay.cs`
- `RunGameManager.cs`

### Change money gain rules

Check:

- `MoneyRewardTable_SO.cs`
- `TileResolver.cs`
- `BattleButton.cs`

## Scene Notes

### Map Scene

Usually centered around:
- `RunGameManager`
- `BoardManager`
- `MapHUD`
- `MapViewController`

### Battle Scene

Usually centered around:
- `BattleManager`
- `BattleSceneEncounterBootstrap`
- `BattleButton`
- `BattlePresentationSystem`
- `BattleFeedbackSystem`
- `BattleSettlementOverlay`
- `BattleSkillInfoOverlay`

## Recommended Reading Order For New Developers

If you are new to the project, read in this order:

1. `RunGameManager.cs`
2. `RunState.cs`
3. `BoardManager.cs`
4. `BattleEncounterRuntime.cs`
5. `BattleSceneEncounterBootstrap.cs`
6. `BattleButton.cs`
7. `BattlePresentationSystem.cs`
8. `BattleFeedbackSystem.cs`
9. `EnemySkill_SO.cs`
10. `BattleStatus_SO.cs`

This order gives the fastest path from:
- map generation
- to entering battle
- to combat resolution
- to returning rewards back into the run

## Maintenance Notes

- Many systems are intentionally data-driven through `ScriptableObject`.
- When adding new gameplay content, prefer creating new assets before hardcoding new cases.
- When adding new combat triggers, keep `EnemySkill_SO.cs`, `BattleButton.cs`, and `BattleSkillInfoOverlay.cs` in sync.
- When adding new persistent resources, update both `RunState.cs` and `MapRunRuntime.cs`.
- When adding new map tiles, update both board generation and tile-enter resolution.

## Short Summary

If you only remember three files:

- `RunGameManager.cs`: map-side main loop
- `BattleButton.cs`: battle-side main logic
- `EnemySkill_SO.cs`: data model for most combat effects

Those three files lead to most of the important systems in the project.
