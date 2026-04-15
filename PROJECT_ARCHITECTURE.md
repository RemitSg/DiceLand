# Project Architecture Guide

## Purpose

This document is a quick orientation guide for new developers joining this Unity project.

It focuses on:
- where the major gameplay systems live
- which scripts are the main entry points
- where to look when changing a specific feature
- how runtime state moves between the map scene and battle scene

This is not a full API reference. It is a practical "where should I look first?" guide.

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
  - shows generated merchant stock and handles buying

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

- `BattleFeedbackSystem.cs`
  - health bars, status icons, status tooltips, damage popups, heal popups, death feedback
  - edit this for in-battle UI feedback

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
