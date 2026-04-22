# Tutorial Battle Redesign

## Goal

Replace the current three-battle tutorial structure with a seven-stage battle-focused tutorial route that teaches combat in smaller, ordered lessons.

The redesign must satisfy these constraints:

- Tutorial always starts from the beginning.
- Tutorial does not use or overwrite normal save/continue flow.
- Tutorial remains on a fixed map route.
- Tutorial is battle-heavy, with only a few support tiles between lessons.
- Every tutorial battle has its own checkpoint.
- Early battles use temporary skills that can change per lesson.
- Later battles gradually accumulate previously taught skills.
- Advanced concepts such as power modifier timing must not appear in the opening battle.

## Why Change

The current tutorial battle content is still organized around `Battle`, `Elite`, and `Boss`. That structure is too coarse for detailed teaching:

- Too many mechanics get packed into the first battle.
- Advanced combat concepts appear before the player has learned the prerequisites.
- Battle rarity ends up controlling teaching order, which makes iteration awkward.
- Three battles are not enough to teach speed, targeting, clash flow, one-sided attacks, energy, status, and advanced modifier timing in a low-pressure way.

The tutorial should instead be driven by **lesson order**, not by normal encounter tier.

## Recommended Structure

Use a seven-stage battle curriculum:

1. `Battle_01`: speed and reading enemy intent
2. `Battle_02`: targeting, connecting lines, and what creates a clash
3. `Battle_03`: one-sided attacks and deciding which line to block
4. `Battle_04`: skill switching, energy cost, and energy reservation
5. `Battle_05`: statuses and skill keywords
6. `Battle_06`: advanced power modifier timing
7. `Battle_07`: mixed final exercise

This keeps each battle focused on one primary concept while lightly reviewing previous lessons.

## Player Route

The tutorial map should remain linear, but it should stop treating battle lessons as only three major fights.

Recommended route:

`Spawn -> Empty -> Battle_01 -> Battle_02 -> Rest -> Battle_03 -> Shop -> Battle_04 -> Forge -> Battle_05 -> Battle_06 -> Key -> Battle_07 -> End`

Notes:

- `Empty` still introduces movement and tile entry.
- `Rest`, `Shop`, and `Forge` remain present so the tutorial still touches connected systems.
- `Key` can remain as a low-pressure transition tile before the final exercise.
- `End` can still be represented by returning to the main menu after the final tutorial battle.

## Lesson Breakdown

### 1. Battle_01: Speed And Intent

Purpose:

- Teach what speed means.
- Teach that speed buttons represent separate actions.
- Teach the player to inspect enemy intent before acting.

Must teach:

- What the speed value does in this game's combat flow.
- Why a faster action is more dangerous if left unblocked.
- How to inspect enemy skills from the speed button or enemy panel.

Must avoid:

- Advanced power modifier timing.
- Multi-layer status explanations.

Enemy design:

- Single-button enemy or very low-complexity setup.
- Fixed skill order.
- Clear contrast between a slower action and a faster action.

Player skills:

- Temporary, lesson-specific skills only.
- Small pool, easy to compare.

### 2. Battle_02: Targeting And Clash

Purpose:

- Teach manual targeting.
- Teach what line connection means.
- Teach that a clash happens only when actions oppose each other.

Must teach:

- How to draw a line.
- What creates a clash.
- Why not every line becomes a clash.

Enemy design:

- Fixed actions arranged so the player can reliably create at least one clash.

Player skills:

- Temporary lesson-specific skills.
- No advanced conditional wording yet.

### 3. Battle_03: One-Sided Attacks

Purpose:

- Teach what a one-sided attack is.
- Teach that not every enemy line must be blocked.
- Teach the first version of "which line should I take?"

Must teach:

- Ally one-sided attack.
- Enemy one-sided attack.
- Basic risk evaluation: dangerous line vs low-priority line.

Enemy design:

- One action worth blocking and one action acceptable to leave alone.
- Fixed order so the teaching moment always happens.

Player skills:

- Temporary lesson-specific skills.
- Still tightly controlled.

### 4. Battle_04: Skill Switching And Energy

Purpose:

- Teach changing skills deliberately.
- Teach energy cost and reservation.

Must teach:

- Why a skill may be strong but not worth using this turn.
- That energy is reserved on connection and consumed during resolution.
- That skill choice should happen before line planning is finalized.

Enemy design:

- Fixed actions that create a reason to swap skills.

Player skills:

- Temporary lesson-specific skills.
- This is the first battle where energy management matters.

### 5. Battle_05: Statuses And Keywords

Purpose:

- Teach how to read status-related keywords.
- Teach how statuses are applied and later consumed or leveraged.

Must teach:

- Where to read status application text.
- Who receives the status.
- When the status triggers.
- How many stacks are applied.

Enemy design:

- Two-button enemy is appropriate here.
- One action applies status, another action benefits from it.
- Fully fixed sequence.

Player skills:

- Begin hybrid model.
- Keep a few lesson-specific skills, but also start retaining selected earlier tutorial skills.

### 6. Battle_06: Advanced Power Modifier Timing

Purpose:

- Teach the difference between:
  - `[拼点时]`
  - `[使用该技能时]`
  - `[单方面攻击时]`

This is intentionally delayed until the player already understands:

- speed
- intent reading
- targeting
- clash creation
- one-sided attacks
- skill switching
- energy
- status reading

Must teach:

- Which modifiers apply in clash only.
- Which modifiers apply in one-sided attacks only.
- Which modifiers apply in both because they trigger on skill use.
- How this changes the decision of whether to contest or to let a line through.

Enemy design:

- Fixed sequence built specifically to demonstrate all three timing cases.
- At least one clean clash case and one clean one-sided case.

Player skills:

- Hybrid model continues.
- Temporary skills for this lesson can replace or augment earlier skills as needed.

### 7. Battle_07: Final Exercise

Purpose:

- Combine speed, targeting, clash choice, one-sided attack choice, energy, statuses, and advanced modifier timing.
- Require the player to make decisions without constant step-by-step enforcement.

Must teach:

- Full read-plan-execute loop.
- Prioritization between multiple enemy actions.

Enemy design:

- Two-button enemy.
- Fixed scripted sequence.
- Uses status pressure and timing-dependent power modifiers together.

Player skills:

- Accumulated tutorial pool with limited cleanup.
- Should feel like a simplified but real combat test.

## Checkpoint Rule

Tutorial battles should use **per-battle checkpoints**.

Rules:

- Losing `Battle_01` restarts `Battle_01`.
- Losing `Battle_02` restarts `Battle_02`.
- And so on.

Reasoning:

- Tutorial should be low-pressure.
- Each lesson should be repeatable in isolation.
- Iterating on lesson difficulty is easier when failure boundaries are narrow.

## Enemy Behavior Rule

Tutorial enemies should not rely on random skill selection for lesson-critical turns.

Required behavior:

- Every tutorial battle uses fixed scripted enemy action order.
- Scripted order should be defined in tutorial content, not hardcoded into general combat logic.
- Existing conditional skill systems may be reused if they are sufficient.

Preferred implementation direction:

- Reuse `EnemyData_SO.conditionalSkillRules` with round-sequence rules where possible.
- Keep tutorial-specific sequencing authored by tutorial content factories or dedicated tutorial assets.

## Player Skill Progression Rule

Tutorial player skills should follow a phased model:

- Early battles: temporary lesson-specific skill sets, freely replaceable per battle.
- Later battles: selected earlier skills begin to accumulate.
- Lesson-specific skills can still override or replace parts of the pool when the current lesson requires tighter control.

This supports two competing needs:

- strict teaching control in early lessons
- synthesis and player judgment in later lessons

## Content Model Changes

The tutorial battle content model should stop keying directly off `MapTileType.Battle / Elite / Boss` for lesson flow.

Recommended change:

- Introduce a tutorial battle stage id or lesson id.
- Resolve tutorial content by lesson id, not by encounter tier.

Examples:

- `TutorialBattleStageId.Battle01`
- `TutorialBattleStageId.Battle02`
- ...
- `TutorialBattleStageId.Battle07`

This id should drive:

- enemy group selection
- tutorial battle definition selection
- checkpoint labeling
- optional temporary skill profile selection

## Map Data Changes

The fixed tutorial board needs lesson-aware tile metadata.

Recommended additions at the tutorial layer:

- tutorial tile id
- tutorial battle stage id
- optional checkpoint label
- optional lesson title

Do not overload normal procedural map generation logic more than necessary.

Preferred approach:

- Keep using normal `BoardTileData`
- Add tutorial-specific resolution through deterministic tile sequencing in `RunGameManager`
- Map sequence index to tutorial lesson metadata in a dedicated helper/config layer

## Runtime Architecture Changes

### RunGameManager

Current problem:

- Tutorial battle resolution is still branching by normal tile type.

Required change:

- Build tutorial board with multiple battle lesson tiles.
- Resolve tutorial encounter content by lesson id or tutorial sequence index.
- Keep `Rest`, `Shop`, `Forge`, and `Key` behavior unchanged where possible.

### TutorialBattleContentFactory

Current problem:

- It assumes only three lesson buckets.

Required change:

- Replace `opening / elite / boss` content assumptions with explicit lesson-stage builders.
- Keep tutorial enemy and tutorial text generation centralized here unless a later asset-based authoring pass replaces it.

### TutorialBattleDefinition_SO / Runtime

Keep:

- event-driven step definitions
- reaction popups
- blocking and informational step policies

Extend:

- lesson-specific definitions for each stage
- softer guidance in later battles, stricter blocking in early ones

### BattleEncounterRuntime / Settlement

Keep:

- tutorial battle retry behavior
- return to main menu after final tutorial victory

Extend:

- lesson-aware checkpoint identity
- no dependence on normal encounter tier for tutorial retry behavior

## UI And Teaching Style

Teaching style should also change with the lesson stage:

- Early battles: explicit, blocking, step-by-step prompts
- Mid battles: blocking only at key actions
- Final battle: mostly reminder popups and reaction callouts

Important teaching principle:

- The tutorial should explain **why** a choice is good or bad, not just tell the player which button to click.

This matters especially for:

- speed priority
- deciding which line to contest
- deciding when to allow one-sided attacks
- choosing between clash-focused and one-sided-focused skills

## Testing Strategy

Add or update edit mode tests for:

- fixed tutorial route contains seven battle lesson positions in the expected order
- tutorial battle content resolves by lesson id rather than only `Battle / Elite / Boss`
- each lesson has a deterministic enemy sequencing rule
- early lessons use replaceable temporary skill sets
- later lessons expose accumulated tutorial skill behavior
- final lesson returns to main menu on victory
- each tutorial battle restarts only its own lesson on defeat

Manual validation in Unity should cover:

- every battle launches the intended lesson
- enemy skill order is deterministic
- speed teaching appears before clash-depth teaching
- advanced modifier timing does not appear in opening lessons
- temporary player skill pools change as designed
- retry after defeat returns to the correct lesson

## Implementation Boundary

This redesign should not require rewriting the normal combat system.

Allowed scope:

- tutorial route generation
- tutorial encounter resolution
- tutorial content definitions
- tutorial enemy content
- tutorial checkpoint wiring

Avoid:

- changing normal non-tutorial encounter selection rules
- forking core combat flow just for tutorial sequencing
- introducing tutorial-only battle scenes

## Recommendation

Implement the redesign in two layers:

1. First, refactor tutorial progression to resolve lesson content by stage id and expand the tutorial map to seven battles.
2. Then rewrite lesson content battle-by-battle in ascending complexity.

This keeps structural changes separate from copy and encounter tuning, which will make iteration easier.
