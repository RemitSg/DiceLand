# Relic Trigger Dispatcher Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a conservative relic trigger dispatcher that becomes the preferred entry point for relic processing without changing existing relic assets or enum values.

**Architecture:** Add `RelicTriggerDispatcher` as a thin shared wrapper over `RelicSystem` and battle relic processing. Keep `RelicSystem` behavior intact, then migrate low-risk callers to the new dispatcher behind regression tests.

**Tech Stack:** Unity C#, ScriptableObject data, NUnit EditMode tests.

---

### Task 1: Dispatcher Core

**Files:**
- Create: `Assets/Scripts/Shared/RelicTriggerDispatcher.cs`
- Test: `Assets/Tests/EditMode/Editor/RelicTriggerDispatcherTests.cs`

- [x] **Step 1: Write failing tests for run and battle processing**

Create `RelicTriggerDispatcherTests.cs` with tests that call the future dispatcher API:

```csharp
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class RelicTriggerDispatcherTests
{
    private readonly List<Object> createdObjects = new List<Object>();

    [TearDown]
    public void TearDown()
    {
        for (int i = createdObjects.Count - 1; i >= 0; i--)
        {
            if (createdObjects[i] != null)
                Object.DestroyImmediate(createdObjects[i]);
        }

        createdObjects.Clear();
    }

    [Test]
    public void ProcessRunRelics_AppliesRelicInventoryToContext()
    {
        RunState runState = new RunState();
        Relic_SO relic = CreateRelic("dispatcher-move", Relic_SO.RelicTrigger.OnMoveCostAboutToSpend, Relic_SO.RelicBuiltInEffectType.ChanceToIgnoreMoveCost);
        runState.AddRelic(relic, 1);

        RelicExecutionContext context = RelicExecutionContext.CreateMove(runState, null, null, 1, true);

        RelicTriggerDispatcher.ProcessRunRelics(runState, context);

        Assert.IsTrue(context.ignoreMoveCost);
    }

    [Test]
    public void ProcessBattleRelics_AppliesEncounterRelicsAndFlushesFeedback()
    {
        Relic_SO relic = CreateRelic("dispatcher-battle-end", Relic_SO.RelicTrigger.OnBattleRoundEnd, Relic_SO.RelicBuiltInEffectType.HealRandomDamagedAllyOnBattleRoundEnd, effect => effect.amount = 1);
        BattleEncounterData encounter = new BattleEncounterData
        {
            relicInventory = new List<RunRelicState>
            {
                new RunRelicState { relicData = relic, amount = 1 }
            }
        };
        BattleAllyController ally = CreateAlly("Damaged", 5, 2);
        RelicExecutionContext context = RelicExecutionContext.CreateBattleRoundEnd(null, 1, new[] { ally }, new BattleEnemyController[0]);
        List<string> logs = new List<string>();

        RelicTriggerDispatcher.ProcessBattleRelics(encounter, context, logs.Add);

        Assert.AreEqual(3, ally.CurrentHP);
        Assert.GreaterOrEqual(logs.Count, 1);
    }

    private Relic_SO CreateRelic(string id, Relic_SO.RelicTrigger trigger, Relic_SO.RelicBuiltInEffectType effectType, System.Action<Relic_SO.RelicBuiltInEffectEntry> configure = null)
    {
        Relic_SO relic = ScriptableObject.CreateInstance<Relic_SO>();
        relic.relicId = id;
        relic.relicName = id;
        relic.builtInEffects = new List<Relic_SO.RelicBuiltInEffectEntry>
        {
            new Relic_SO.RelicBuiltInEffectEntry
            {
                trigger = trigger,
                effectType = effectType,
                chance = 1f,
                amount = 1
            }
        };
        configure?.Invoke(relic.builtInEffects[0]);
        createdObjects.Add(relic);
        return relic;
    }

    private BattleAllyController CreateAlly(string name, int maxHp, int currentHp)
    {
        GameObject go = new GameObject(name);
        createdObjects.Add(go);
        BattleAllyController ally = go.AddComponent<BattleAllyController>();
        CharacterData_SO data = ScriptableObject.CreateInstance<CharacterData_SO>();
        data.characterName = name;
        data.baseHP = maxHp;
        data.minSpeed = 1;
        data.maxSpeed = 1;
        createdObjects.Add(data);
        ally.ApplyRuntimeState(new RunPartyMemberState
        {
            characterData = data,
            maxHP = maxHp,
            currentHP = currentHp,
            level = 1
        });
        return ally;
    }
}
```

- [x] **Step 2: Run tests and confirm missing dispatcher failure**

Attempted to locate a Unity command-line runner in the current environment. No Unity executable was available, so the red step was verified by the compile-time missing symbol: `RelicTriggerDispatcher` did not exist when the test was written.

- [x] **Step 3: Add minimal dispatcher implementation**

Create `RelicTriggerDispatcher.cs`:

```csharp
using System;

public static class RelicTriggerDispatcher
{
    public static void ProcessRunRelics(RunState runState, RelicExecutionContext context, Action<string> logFeedback = null)
    {
        if (runState == null || context == null)
            return;

        if (context.runState == null)
            context.runState = runState;

        RelicSystem.Process(runState.relicInventory, context);
        FlushFeedback(context, logFeedback);
    }

    public static void ProcessBattleRelics(BattleEncounterData encounter, RelicExecutionContext context, Action<string> logFeedback = null)
    {
        if (encounter == null || context == null)
            return;

        RelicSystem.Process(encounter.relicInventory, context);
        FlushFeedback(context, logFeedback);
    }

    public static void FlushFeedback(RelicExecutionContext context, Action<string> logFeedback)
    {
        if (context == null || context.feedbackMessages == null || logFeedback == null)
            return;

        for (int i = 0; i < context.feedbackMessages.Count; i++)
        {
            if (!string.IsNullOrWhiteSpace(context.feedbackMessages[i]))
                logFeedback(context.feedbackMessages[i]);
        }
    }
}
```

- [x] **Step 4: Run dispatcher tests**

Unity EditMode tests could not be executed in this environment because no Unity command-line runner was found.

### Task 2: Low-Risk Convenience APIs

**Files:**
- Modify: `Assets/Scripts/Shared/RelicTriggerDispatcher.cs`
- Modify: `Assets/Tests/EditMode/Editor/RelicTriggerDispatcherTests.cs`

- [x] **Step 1: Write failing tests for merchant price and rest reforge limit**

Add tests that call:

```csharp
RelicTriggerDispatcher.CalculateMerchantOfferPrice(runState, tile, offer);
RelicTriggerDispatcher.NotifyMerchantPurchaseCompleted(runState, tile, offer);
RelicTriggerDispatcher.CalculateRestReforgeLimit(runState, 2);
```

Expected behavior must match current `RelicSystem` public helpers.

- [x] **Step 2: Run tests and confirm methods are missing**

Expected: compile fails because the convenience methods do not exist. Unity EditMode tests could not be executed in this environment because no Unity command-line runner was found.

- [x] **Step 3: Add convenience methods**

Add methods that delegate to the existing `RelicSystem` helpers:

```csharp
public static int CalculateMerchantOfferPrice(RunState runState, BoardTileData tile, MerchantOfferState offer)
{
    return RelicSystem.CalculateMerchantOfferPrice(runState, tile, offer);
}

public static void NotifyMerchantPurchaseCompleted(RunState runState, BoardTileData tile, MerchantOfferState offer)
{
    RelicSystem.NotifyMerchantPurchaseCompleted(runState, tile, offer);
}

public static int CalculateRestReforgeLimit(RunState runState, int baseLimit)
{
    return RelicSystem.CalculateRestReforgeLimit(runState, baseLimit);
}
```

- [x] **Step 4: Run dispatcher tests**

Unity EditMode tests could not be executed in this environment because no Unity command-line runner was found.

### Task 3: Migrate Low-Risk Call Sites

**Files:**
- Modify: `Assets/Scripts/MapScene/Manager/RunGameManager.cs`
- Modify: `Assets/Scripts/MapScene/Controller/MapMerchantOverlay.cs`
- Modify: `Assets/Scripts/RestScene/Controller/RestSceneReforgeInteractable.cs`
- Modify: `Assets/Scripts/MapScene/Manager/MapRunRuntime.cs`

- [x] **Step 1: Replace helper calls**

Replace:

```csharp
RelicSystem.CalculateMerchantOfferPrice(...)
RelicSystem.NotifyMerchantPurchaseCompleted(...)
RelicSystem.CalculateRestReforgeLimit(...)
```

with:

```csharp
RelicTriggerDispatcher.CalculateMerchantOfferPrice(...)
RelicTriggerDispatcher.NotifyMerchantPurchaseCompleted(...)
RelicTriggerDispatcher.CalculateRestReforgeLimit(...)
```

- [x] **Step 2: Replace direct low-risk process calls**

For map movement, key collected, calamity applied, loot reforge, and relic reforge contexts, replace:

```csharp
RelicSystem.Process(runState, trigger, context);
```

with:

```csharp
RelicTriggerDispatcher.ProcessRunRelics(runState, context);
```

Only migrate call sites where the context factory already sets the trigger.

- [x] **Step 3: Run focused tests**

Unity EditMode tests could not be executed in this environment because no Unity command-line runner was found. Static search confirmed the migrated low-risk call sites now reference `RelicTriggerDispatcher`.

### Task 4: Progress Update

**Files:**
- Modify: `PROJECT_PROGRESS.md`

- [x] **Step 1: Append progress note**

Append an ASCII-safe note documenting:

- Dispatcher created.
- Low-risk call sites migrated.
- Battle-heavy call sites intentionally deferred.
- Test result.

- [x] **Step 2: Final verification**

Run `rg -n "RelicSystem\\.Process\\(|CalculateMerchantOfferPrice|CalculateRestReforgeLimit|NotifyMerchantPurchaseCompleted" Assets/Scripts` and confirm remaining direct calls are either inside `RelicSystem`, inside dispatcher, or intentionally deferred battle-heavy call sites.

### Task 5: Battle Call-Site Migration

**Files:**
- Modify: `Assets/Scripts/BattleScene/Controller/BattleAllyController.cs`
- Modify: `Assets/Scripts/BattleScene/Controller/BattleButton.cs`

- [x] **Step 1: Migrate direct battle processing calls**

Replaced direct calls in battle controllers:

```csharp
RelicSystem.Process(activeEncounter.relicInventory, context);
RelicSystem.Process(encounter.relicInventory, lootContext);
```

with:

```csharp
RelicTriggerDispatcher.ProcessBattleRelics(activeEncounter, context);
RelicTriggerDispatcher.ProcessBattleRelics(encounter, lootContext);
```

The existing context factories and feedback flushing positions were preserved.

- [x] **Step 2: Verify remaining direct processing calls**

Run:

```powershell
rg -n "RelicSystem\.Process\(|RelicSystem\.(CalculateMerchantOfferPrice|NotifyMerchantPurchaseCompleted|CalculateRestReforgeLimit)" Assets/Scripts -g "*.cs"
```

Observed result: remaining calls are only inside `Assets/Scripts/Shared/RelicTriggerDispatcher.cs`.

- [x] **Step 3: Test status**

Unity EditMode tests could not be executed in this environment because no Unity command-line runner was found. The next editor-side verification should run `RelicTriggerDispatcherTests` and the existing relic test suite.

### Task 6: Semantic Dispatcher Methods

**Files:**
- Modify: `Assets/Scripts/Shared/RelicTriggerDispatcher.cs`
- Modify: `Assets/Scripts/Shared/RelicExecutionContext.cs`
- Modify: `Assets/Tests/EditMode/Editor/RelicTriggerDispatcherTests.cs`
- Modify: `Assets/Scripts/MapScene/Manager/MapRunRuntime.cs`
- Modify: `Assets/Scripts/MapScene/Manager/RunGameManager.cs`
- Modify: `Assets/Scripts/BattleScene/Controller/BattleButton.cs`

- [x] **Step 1: Add tests for semantic dispatcher methods**

Added tests for:

```csharp
RelicTriggerDispatcher.ProcessMoveCost(...)
RelicTriggerDispatcher.ProcessBattleRoundEnd(...)
```

These tests assert that dispatcher methods create the expected context, process relic effects, and return the context for follow-up logic.

- [x] **Step 2: Add semantic dispatcher methods**

Added named methods for common trigger families:

```csharp
ProcessMoveCost
ProcessKeyCollected
ProcessCalamityApplied
ProcessLootReforge
ProcessRelicReforge
ProcessStatusApplied
ProcessPlayerDicePrepared
ProcessBattleRoundStart
ProcessBattleSetup
ProcessBattleRoundEnd
```

Each method still delegates to the existing `RelicExecutionContext` factory and `ProcessRunRelics` / `ProcessBattleRelics`, preserving existing behavior.

- [x] **Step 3: Migrate matching call sites**

Moved map movement, key collection, calamity, loot reforge, relic reforge, status application, player dice preparation, battle setup, battle round start, and battle round end call sites to the semantic methods.

- [x] **Step 4: Document context field groups**

Added field group comments in `RelicExecutionContext` for routing, map/run, status, source/target, merchant, dice, damage, lethal prevention, reforge, rewards, setup, round/speed, calamity, participants, and feedback.

- [x] **Step 5: Static verification**

Run:

```powershell
rg -n "RelicExecutionContext\.Create(Move|KeyCollected|CalamityApplied|LootReforge|RelicReforge|BattleRoundStart|BattleRoundEnd|BattleSetup|PlayerDicePrepared|StatusApplied)" Assets/Scripts -g "*.cs"
```

Observed result: these factories are now used through `RelicTriggerDispatcher`.

Unity EditMode tests could not be executed in this environment because no Unity command-line runner was found. The next editor-side verification should run `RelicTriggerDispatcherTests`.
