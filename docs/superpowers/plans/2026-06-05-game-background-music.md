# Game Background Music Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add configurable map and battle background music with enemy-group overrides, nine battle fallback slots, looping playback, and slow-motion audio treatment.

**Architecture:** Enemy groups expose an optional battle BGM override. A shared `GameMusicConfig_SO` stores map music plus the nine fallback battle clips, and a focused resolver maps `BattleEncounterData` to a clip. A persistent runtime `GameMusicPlayer` owns `AudioSource` playback, applies settings volume, loops music, and lowers pitch/filter cutoff while battle death slow motion is active.

**Tech Stack:** Unity C#, ScriptableObject configuration, `AudioClip`, `AudioSource`, `AudioLowPassFilter`, NUnit EditMode tests, `dotnet build`.

---

### Task 1: Battle Music Resolution

**Files:**
- Modify: `Assets/Scripts/ScriptableObject/EnemyGroup_SO.cs`
- Create: `Assets/Scripts/ScriptableObject/GameMusicConfig_SO.cs`
- Create: `Assets/Scripts/Shared/GameMusicResolver.cs`
- Test: `Assets/Tests/EditMode/Editor/GameMusicResolverTests.cs`

- [ ] Write failing tests for group override, boss fallback, and danger/type fallback.
- [ ] Implement `EnemyGroup_SO.battleMusicOverride`.
- [ ] Implement `GameMusicConfig_SO` with map clip plus nine fallback battle clip fields.
- [ ] Implement `GameMusicResolver.ResolveBattleMusic(config, encounter)`.
- [ ] Run EditMode-capable verification where available, then `dotnet build MobileGame.sln --no-restore -v:minimal`.

### Task 2: Runtime Music Player

**Files:**
- Create: `Assets/Scripts/Shared/GameMusicPlayer.cs`
- Modify: `Assets/Scripts/Shared/GameMusicResolver.cs`

- [ ] Add `GameMusicPlayer.GetOrCreate()` with persistent singleton semantics.
- [ ] Add `Play(AudioClip clip)` that assigns `AudioSource.clip`, enables `loop`, applies music volume, and avoids restarting the same clip.
- [ ] Add `PlayMapMusic(config)` and `PlayBattleMusic(config, encounter)` helpers.
- [ ] Add slow-motion treatment using `AudioSource.pitch` and `AudioLowPassFilter.cutoffFrequency`, based on `Time.timeScale`.

### Task 3: Scene Hooks

**Files:**
- Modify: `Assets/Scripts/BattleScene/Manager/BattleSceneEncounterBootstrap.cs`
- Modify: `Assets/Scripts/MapScene/Manager/RunGameManager.cs`
- Modify: `Assets/Scripts/MapScene/Manager/MapSceneBootstrap.cs`

- [ ] Add serialized `GameMusicConfig_SO` references where scene inspectors can configure music.
- [ ] On map scene startup, play map music if a config is present.
- [ ] On battle encounter spawn, play resolved battle music if a config is present.
- [ ] When runtime-created scene roots lack a config, fall back to `Resources.Load<GameMusicConfig_SO>("GameMusicConfig")`.

### Task 4: Verification

**Files:**
- Verify only.

- [ ] Run `dotnet build MobileGame.sln --no-restore -v:minimal`.
- [ ] Report Unity MCP/editor limitation clearly if Unity Test Runner cannot be executed.
