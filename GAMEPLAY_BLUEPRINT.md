# Gameplay Blueprint

## Goal

Build the out-of-combat roguelite loop around the existing battle system.

Core run flow:

1. Generate a square grid board based on difficulty.
2. Spawn the player at a random edge tile.
3. Place the Boss on the center tile.
4. Let the player spend action points to move and resolve tiles.
5. Collect keys, experience, materials, and weapon templates.
6. Trigger calamities whenever action points are exhausted.
7. Unlock the Boss tile after collecting enough keys.
8. Defeat the Boss to clear the run.

Failure condition:

- Lose a battle and restart the whole run.

Run structure:

- Single-run roguelite
- No long-term save progression for now

## Confirmed Design

### Board Size By Difficulty

- Easy: `7x7`
- Normal: `9x9`
- Hard: `11x11`
- Extreme: `13x13`

These should be driven by config data, not hardcoded in game flow logic.

### Board Rules

- Player spawn tile: random
- Boss tile: always the exact center tile
- Boss cannot be challenged immediately
- Key tiles must be cleared/visited before Boss unlocks
- Enemy difficulty scales with distance to center
- The closer a battle tile is to the center, the harder the encounter
- The farther a battle tile is from the center, the easier the encounter

Suggested rule:

- Generate more key tiles than required
- Example: spawn `3` key tiles, require `2`

This gives route choice and prevents the run from feeling too linear.

### Tile Visibility

- Tile type is always visible on the board
- Battle tiles reveal partial preview only
- Exact encounter content is hidden until the tile is entered

Battle tile preview can initially expose:

- Enemy count
- Danger rating
- Encounter family/tag

Danger rating should reflect center-distance danger scaling so the player can read map risk at a glance.

### Action Points

- Start with a fixed amount based on difficulty
- Moving to an adjacent tile consumes action points
- Extra action points are only granted after surviving a calamity cycle

### Calamity

- Triggers when action points reach zero
- Calamity has difficulty tiers
- Each tier has its own calamity pool
- Current calamity tier determines which pool is used

Suggested base rule:

1. Action points reach zero
2. Calamity tier increases
3. Draw one calamity from the corresponding pool
4. Apply the calamity
5. Start the next cycle with increased action points

### Growth

Character growth:

- Gain experience by killing enemies in battle tiles
- Level up only increases max HP for now

Weapon growth:

- Weapon type is fixed by template
- Weapon template is mainly obtained from event tiles
- Weapon materials are mainly obtained from battle rewards
- Material combination changes weapon attributes

### Tile Frequency

Initial weight target:

- Battle: `4`
- Event: `4`
- Rest: `1`

Do not hardcode this into logic. Make tile generation weight-based so more tile types can be added later.

## Recommended Tile Types

Initial tile set:

- `Battle`
- `Event`
- `Rest`
- `Key`
- `Boss`

Possible future expansion:

- `EliteBattle`
- `Shop`
- `Forge`
- `Treasure`
- `Curse`
- `Story`

## Boss Unlock Rule

Recommended MVP implementation:

- Boss tile exists from the start
- Boss tile UI always shows required keys
- Boss battle can only start if `currentKeys >= requiredKeys`

If keys are insufficient:

- Entering the Boss tile is blocked, or
- The player can stand on the tile but cannot trigger the Boss battle

Suggested clearer version:

- Let the player move onto the center tile
- Show "Need X / Y Keys"
- Do not open battle until requirements are met

## Core Systems

### 1. RunGameManager

Responsibility:

- Own the current run state
- Control phase switching
- Restart the run on defeat
- Trigger victory flow on Boss clear

### 2. DifficultyConfig

Responsibility:

- Define board size
- Define starting action points
- Define key requirements
- Define tile weights
- Define starting calamity tier
- Define Boss setup

Recommended asset:

- `DifficultyConfig_SO`

### 3. BoardManager

Responsibility:

- Generate the board
- Assign tile types
- Place spawn, keys, and Boss
- Validate pathing
- Track visited/revealed/resolved state

Important requirement:

- Must guarantee the player can reach enough key tiles and then reach the Boss

### 4. PlayerMapController

Responsibility:

- Hold current board position
- Move to adjacent tiles
- Spend action points
- Ask tile resolver to resolve entered tiles

### 5. TileResolver

Responsibility:

- Execute tile content when entered
- Open battle
- Trigger event
- Trigger rest
- Grant key
- Check Boss unlock state

### 6. ActionPointSystem

Responsibility:

- Track current action points
- Consume action points
- Detect exhaustion
- Hand off to calamity flow

### 7. CalamitySystem

Responsibility:

- Increase calamity tier
- Draw a calamity from the correct pool
- Apply enemy buffs / player debuffs / world modifiers
- Grant next cycle action points

### 8. RewardSystem

Responsibility:

- Grant EXP
- Handle leveling
- Grant weapon materials
- Grant templates
- Grant keys when applicable

### 9. CraftingSystem

Responsibility:

- Validate templates
- Consume materials
- Generate weapon attributes
- Return a finished weapon instance

### 10. BossUnlockSystem

Responsibility:

- Track required and collected keys
- Report whether Boss is unlocked
- Update Boss tile state and UI

## Recommended Data Model

### RunState

```csharp
public class RunState
{
    public int difficultyId;
    public int boardSeed;
    public Vector2Int playerGridPosition;
    public int currentActionPoints;
    public int bonusActionPoints;
    public int calamityTier;
    public int currentKeys;
    public int requiredKeys;
    public int playerLevel;
    public int playerExp;
    public bool isBossUnlocked;
}
```

### BoardTileData

```csharp
public class BoardTileData
{
    public Vector2Int coord;
    public TileType tileType;
    public bool isVisited;
    public bool isResolved;
    public int encounterSeed;
    public string previewId;
}
```

### TileType

```csharp
public enum TileType
{
    Battle,
    Event,
    Rest,
    Key,
    Boss
}
```

### DifficultyConfig_SO

Recommended fields:

- `difficultyName`
- `boardSize`
- `startingActionPoints`
- `requiredKeys`
- `generatedKeyTileCount`
- `startingCalamityTier`
- `tileWeights`
- `bossEncounterId`

### CalamityData

Recommended fields:

- `tier`
- `effectType`
- `effectValue`
- `description`
- `weight`

### Weapon Material Data

Recommended fields:

- `materialId`
- `displayName`
- `rarity`
- `attributeModifiers`
- `specialTags`

### Weapon Template Data

Recommended fields:

- `weaponId`
- `weaponName`
- `allowedMaterialSlots`
- `resultWeaponType`
- `baseSkillPool`

## Board Generation Plan

Recommended order:

1. Read selected difficulty config
2. Create an empty square board
3. Mark the center tile as `Boss`
4. Pick a random spawn tile with a minimum distance from center
5. Place key tiles
6. Calculate a danger band for each tile from its distance to center
7. Fill remaining tiles using weighted generation
8. Assign battle encounter difficulty using the tile's danger band
9. Run path validation
10. If invalid, regenerate

### Danger Band Rule

Enemy difficulty should increase as the player moves closer to the center.

Recommended first implementation:

- Outer ring: easy encounters
- Mid ring: normal encounters
- Inner ring: hard encounters
- Center-adjacent ring: very hard encounters

This danger band can later affect:

- Encounter pool
- Enemy stat multipliers
- Reward quality
- Battle tile preview danger rating

### Path Validation Requirements

Need to ensure:

- Spawn can reach enough key tiles
- Spawn can reach center Boss tile
- Paths are not trivially blocked by future tile rules

If tiles themselves do not block movement, path validation is simpler, but still useful for later-proofing.

## Tile Resolution Rules

### Battle Tile

- Enter tile
- Generate encounter from preview seed
- Encounter strength is chosen from the tile's center-distance danger band
- Run battle
- On victory: gain EXP and materials
- On defeat: restart run

### Event Tile

- Enter tile
- Show event and choices
- Resolve rewards, tradeoffs, or risks
- Can grant templates or temporary modifiers

### Rest Tile

- Enter tile
- Restore HP
- Future option: remove a debuff or gain a minor buff

### Key Tile

- Enter tile
- Grant one key
- Mark tile resolved
- Re-check Boss unlock

### Boss Tile

- If keys are insufficient: show locked state
- If keys are sufficient: start final battle

## Calamity Design

### Structure

Each calamity tier owns its own pool.

Example:

- Tier 1: light pressure
- Tier 2: stronger enemy buffs
- Tier 3: severe player debuffs
- Tier 4+: mixed or special calamities

### Example Calamity Effects

- Enemies gain bonus HP
- Enemies gain bonus speed
- Enemies gain extra dice power
- Player max HP temporarily reduced
- Rest tile healing reduced
- Event outcomes become harsher
- More battle tiles become dangerous

### Action Point Growth

Recommended first implementation:

- Every time a calamity cycle completes, next cycle action points increase by `+1`

This keeps the run expanding while pressure also rises.

## Progression Design

### Character Leveling

Initial implementation:

- EXP from battle kills
- Level up increases max HP only

Future expansion:

- Passive traits
- Extra action point thresholds
- Calamity resistance

### Weapon Crafting

Initial implementation:

- Template determines the weapon identity
- Materials determine resulting attributes
- No weapon-type randomness

Suggested forging flow:

1. Choose a template
2. Insert materials
3. Preview resulting stats
4. Confirm forge
5. Receive final weapon

## MVP Scope

Build this first:

1. Difficulty config with one playable difficulty
2. Board generation
3. Movement and action point spending
4. `Battle / Rest / Key / Boss` tile flow
5. Battle victory reward hookup
6. Key collection and Boss unlock
7. Basic calamity trigger
8. Run restart on defeat

This is enough to create a complete playable run.

## Recommended Development Order

### Phase 1: Map Foundation

- `DifficultyConfig_SO`
- `RunState`
- `BoardTileData`
- `BoardManager`
- `PlayerMapController`

### Phase 2: Tile Loop

- Tile generation
- Tile preview UI
- Tile entry and resolution flow
- Key tile and Boss lock logic

### Phase 3: Battle Integration

- Enter battle from map
- Return battle results
- Grant EXP and materials
- Restart on defeat

### Phase 4: Calamity

- Action point exhaustion
- Calamity tiers
- Calamity draw and resolution
- Next-cycle action point growth

### Phase 5: Crafting

- Material inventory
- Template inventory
- Forge UI
- Weapon result generation

### Phase 6: Boss Run Completion

- Boss unlock
- Boss battle
- Win screen / run clear flow

## Suggested First Script List

- `RunGameManager.cs`
- `RunState.cs`
- `DifficultyConfig_SO.cs`
- `BoardManager.cs`
- `BoardTileData.cs`
- `PlayerMapController.cs`
- `TileResolver.cs`
- `ActionPointSystem.cs`
- `CalamitySystem.cs`
- `BossUnlockSystem.cs`
- `RewardSystem.cs`
- `CraftingSystem.cs`

## Open Questions To Resolve Later

- Does stepping onto a resolved tile consume action points the same way
- Can key tiles also contain combat or are they pure pickups
- Should event tiles sometimes consume action points without movement
- Does rest tile healing scale with level or remain fixed
- Should Boss unlock require all keys or only a threshold
- Should calamities persist for the whole run or only one cycle

## Immediate Next Recommendation

Start with the smallest vertical slice:

1. One difficulty config
2. One board generator
3. One player pawn on the map
4. Action point movement
5. One battle tile
6. One key tile
7. One Boss lock check

Once that slice works, expand tile variety and calamity depth.
