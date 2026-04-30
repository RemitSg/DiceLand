# BattleButton Refactor Master Plan Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans or superpowers:subagent-driven-development to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 将 `BattleButton` 从集中承载战斗流程、目标选择、状态、伤害、遗物、结算和 UI 副作用的巨型控制器，收窄为“战斗流程编排器”，让核心规则进入可测试的小服务。

**Architecture:** 保留 `BattleButton` 作为 Unity 场景入口、协程驱动入口和真实对象副作用闭包持有者；将纯规则、计划生成、目标选择、状态结算、伤害管线、完成判定和奖励构建继续迁移到 `Assets/Scripts/Shared/Battle*.cs`。每一段重构必须先补 EditMode 或 PlayMode 覆盖，再做最小接入，确保行为不变。

**Tech Stack:** Unity C#, NUnit EditMode/PlayMode Tests, Unity MCP Test Runner, `dotnet build`, existing `Assembly-CSharp` / `Assembly-CSharp-Editor` projects.

---

## 当前基线

- 项目目录：`L:\gameProject\MobileGame`
- 当前主文件：`Assets/Scripts/BattleScene/Controller/BattleButton.cs`
- 当前规模：`BattleButton.cs` 约 4616 行。
- 当前服务规模：`Assets/Scripts/Shared` 下已有约 116 个 `Battle*.cs` 服务文件。
- 当前重构进度：已完成到 `Stage 435`。
- 当前已知验证基线：`dotnet build Assembly-CSharp-Editor.csproj --no-restore --no-incremental` 通过，`dotnet build MobileGame.PlayModeTests.csproj --no-restore --no-incremental` 通过，Unity PlayMode `9/9` 通过，Unity EditMode `494/494` 通过，Console `0 error`。
- 当前工作树注意事项：`.plastic/plastic.changes` 与 `.plastic/plastic.wktree` 属于无关污染文件，不纳入 BattleButton 重构提交。

## 最终边界

`BattleButton` 最终只保留这些职责：

- Unity 组件引用、Inspector 配置、场景生命周期。
- 点击按钮后在 Ready / Targeting / Resolving / Settlement 间编排流程。
- 创建真实协程、调用动画、特效、UI、对象销毁等 Unity 副作用。
- 将真实战斗对象包装成服务需要的输入，并执行服务返回的 plan。
- 作为兼容层保留少量 legacy private wrapper，直到所有调用迁移完成。

`BattleButton` 不应继续承载这些职责：

- 目标选择规则。
- 骰子、威力、威力修正、命中、未命中、拼点结果、单方面攻击结果的规则计算。
- 状态施加、状态 tick、状态层数转移、状态触发文字。
- 遗物触发上下文构建和奖励候选构建。
- 精英奖励、战斗奖励、战斗完成计划的核心判断。
- 回合开始、战斗开始、阶段切换中的复杂副作用排序规则。

## 文件结构

继续修改：

- `Assets/Scripts/BattleScene/Controller/BattleButton.cs`：只做小步接入和删除迁出的私有方法。
- `PROJECT_PROGRESS.md`：每个阶段完成后追加中文阶段记录，不整体重写，避免触碰已有乱码段落。

继续新增或扩展：

- `Assets/Scripts/Shared/BattleRoundStartStatusEffectService.cs`：承接 `ApplyRoundStartStatusEffectsGeneric` 的状态 tick 与日志计划。
- `Assets/Scripts/Shared/BattleRoundStartCompletionGuardService.cs`：承接回合开始时撤退结算与战斗完成前置 guard。
- `Assets/Scripts/Shared/BattleTargetSelectionService.cs`：收束实例与静态目标选择入口。
- `Assets/Scripts/Shared/BattleSkillStatusEffectResolutionService.cs`：收束友方/敌方 status-only 技能词条路径。
- `Assets/Scripts/Shared/BattleEnemyBuiltInEffectResolutionService.cs`：继续拆 `ApplyEnemyBuiltInEffects` 剩余胶水。
- `Assets/Scripts/Shared/BattleHitResolutionFacade.cs`：统一命中、零伤、成功命中、遗物触发和技能后续效果入口。
- `Assets/Scripts/Shared/BattleCombatPresentationCoroutinePlanService.cs`：将表现协程和规则 plan 边界显式化。
- `Assets/Scripts/Shared/BattleButtonUiOverlayStateService.cs`：收束 overlay / tooltip / settlement / tutorial 阻塞判断。

测试文件：

- `Assets/Tests/EditMode/Editor/BattleRoundStartStatusEffectServiceTests.cs`
- `Assets/Tests/EditMode/Editor/BattleRoundStartCompletionGuardServiceTests.cs`
- `Assets/Tests/EditMode/Editor/BattleTargetSelectionServiceTests.cs`
- `Assets/Tests/EditMode/Editor/BattleSkillStatusEffectResolutionServiceTests.cs`
- `Assets/Tests/EditMode/Editor/BattleEnemyBuiltInEffectResolutionServiceTests.cs`
- `Assets/Tests/EditMode/Editor/BattleHitResolutionFacadeTests.cs`
- `Assets/Tests/EditMode/Editor/BattleButtonUiOverlayStateServiceTests.cs`
- `Assets/Tests/PlayMode/BattleFlowPlayModeTests.cs`
- `Assets/Tests/PlayMode/BattleFlowPlayModeFixture.cs`

---

## 已完成阶段总览

### Stage 1-230：测试护栏与战斗外层流程收口

已完成内容：

- 建立战斗流程 PlayMode 回归框架。
- 将战斗结算、奖励构建、精英三选一遗物、遗物掉落、经验、掉落物等逻辑大量迁出到共享服务。
- 增加战斗完成 plan / round end plan / settlement data factory 等服务。
- 初步抽出战斗按钮点击 guard、战斗阶段 begin / completion plan。

后续要求：

- 不再回头大改这些服务，除非新测试暴露行为差异。
- 新阶段如需读取奖励或完成状态，优先复用现有 service，不新增平行实现。

### Stage 231-285：`ExecuteCombatPhaseRoutine` 执行循环拆分

已完成内容：

- 新增 `BattleCombatPhaseCompletionDispatcher`。
- 新增 `BattleCombatActionExecutionDispatcher`。
- 新增 `BattleCombatActionExecutionResult`。
- 新增 `BattleCombatActionExecutionContext`。
- `BattleButton.ExecuteCombatPhaseRoutine` 不再直接维护 action resolver lambdas 和 `battleCompletedDuringAction` 本地状态，而是读执行上下文结果。

后续要求：

- 继续保持“执行循环只分派，不计算规则”的方向。
- 后续命中、状态、表现协程迁出时，不把 dispatcher 再扩大成规则类。

### Stage 286-295：`BuildCombatActions` 周边拆分

已完成内容：

- 新增 `BattleCombatActionOrderingService`。
- 新增 `BattleCombatActionCollectorService`。
- 新增 `BattleSpeedButtonListService`。
- `BattleCombatActionBuilder` 变为组合入口，排序和收集规则单独测试。

后续要求：

- 后续目标选择统一时，action collector 只调用目标选择服务，不直接复制选择规则。

### Stage 296-330：目标指定阶段与共享能量拆分

已完成内容：

- 新增 `BattleTargetingSpeedButtonStateService`。
- 新增 `BattleSharedEnergyRoundStartService`。
- 新增 `BattleSharedEnergyReservationCollectorService`。
- 新增 `BattleAllySkillEnergyAvailabilityService`。
- 新增 `BattleTargetingEnemyAutoTargetService`。
- 新增 `BattleTargetingPhaseLogService`。

后续要求：

- 所有速度按钮显示、隐藏、交互开关规则继续放在 speed button state 服务，不在 `BattleButton` 新增分支。
- 能量相关判断继续复用 shared energy 服务，避免技能切换和划线预占各写一套。

### Stage 331-340：事件绑定拆分

已完成内容：

- 新增 `BattleActiveRoundCombatantEventBindingService`。
- 新增 `BattleSharedEnergyListenerBindingService`。
- 将 active round 死亡事件和共享能量监听绑定、解绑迁出。

后续要求：

- 任何新增回合内事件监听必须有“重复绑定不重复触发、解绑后不触发”的 EditMode 覆盖。

### Stage 341-355：回合开始准备拆分

已完成内容：

- 新增 `BattleTargetingRoundStartSetupService`。
- 友方速度生成、回合开始遗物触发、默认技能选择、速度日志构建迁出。
- 敌方速度生成、随机技能选择、速度日志构建迁出。

后续要求：

- 继续保持 `SetupTargetingPhase` 的顺序不变：友方准备、共享能量监听和预占刷新、敌方准备、敌方自动指向。

### Stage 356-435：目标指定阶段副作用调度拆分

已完成内容：

- 新增 `BattlePendingBattleStartAllyStatusGrantService`。
- 新增 `BattleEnemyBattleStartSkillGrantService`。
- 新增 `BattleEnemyTargetingStatusGainPhaseService`。
- 新增 `BattleTargetingPhaseRelicService`。
- 战斗开始友方状态授予、敌方临时技能授予、敌方 battle-start / repeat round-start 状态授予、battle setup / round-start 遗物调度迁出。

后续要求：

- 下一阶段从 `ApplyRoundStartStatusEffectsGeneric` 和 completion guard 开始，不再扩展 `SetupTargetingPhase` 主体。

---

## 剩余总计划

### Task 1: Stage 436-455，拆出回合开始状态 tick

**Files:**

- Modify: `Assets/Scripts/BattleScene/Controller/BattleButton.cs`
- Create: `Assets/Scripts/Shared/BattleRoundStartStatusEffectService.cs`
- Test: `Assets/Tests/EditMode/Editor/BattleRoundStartStatusEffectServiceTests.cs`

- [ ] **Step 1: 写 RED 测试，锁定友方和敌方回合开始状态 tick 顺序**

在 `BattleRoundStartStatusEffectServiceTests.cs` 中覆盖：

```csharp
[Test]
public void BuildPlan_TicksAllLivingAlliesBeforeEnemies()
{
    var calls = new List<string>();
    var plan = BattleRoundStartStatusEffectService.BuildPlan(
        allies: new[] { "ally-a", "ally-b" },
        enemies: new[] { "enemy-a" },
        isAlive: id => true,
        tick: id => calls.Add(id));

    plan.Apply();

    CollectionAssert.AreEqual(new[] { "ally-a", "ally-b", "enemy-a" }, calls);
}
```

Run:

```powershell
dotnet build Assembly-CSharp-Editor.csproj --no-restore --no-incremental
```

Expected: FAIL because `BattleRoundStartStatusEffectService` does not exist.

- [ ] **Step 2: 实现最小服务**

新增服务时只接收委托和集合，不直接依赖 `BattleButton` 私有字段：

```csharp
public static class BattleRoundStartStatusEffectService
{
    public sealed class Plan
    {
        private readonly Action apply;

        public Plan(Action apply)
        {
            this.apply = apply ?? (() => { });
        }

        public void Apply()
        {
            apply();
        }
    }

    public static Plan BuildPlan<TAlly, TEnemy>(
        IReadOnlyList<TAlly> allies,
        IReadOnlyList<TEnemy> enemies,
        Func<object, bool> isAlive,
        Action<object> tick)
    {
        return new Plan(() =>
        {
            foreach (var ally in allies)
            {
                if (isAlive(ally))
                    tick(ally);
            }

            foreach (var enemy in enemies)
            {
                if (isAlive(enemy))
                    tick(enemy);
            }
        });
    }
}
```

- [ ] **Step 3: 接入 `BattleButton.ApplyRoundStartStatusEffectsGeneric`**

保留原方法名作为兼容 wrapper，内部创建 plan 并调用真实状态 tick 闭包：

```csharp
private void ApplyRoundStartStatusEffectsGeneric(BattleAllyController[] allies, BattleEnemyController[] enemies)
{
    BattleRoundStartStatusEffectService
        .BuildPlan<BattleAllyController, BattleEnemyController>(
            allies,
            enemies,
            combatant => combatant switch
            {
                BattleAllyController ally => ally != null && !ally.IsDead,
                BattleEnemyController enemy => enemy != null && !enemy.IsDead,
                _ => false
            },
            combatant => ApplyRoundStartStatusEffectsForCombatant(combatant))
        .Apply();
}
```

- [ ] **Step 4: 验证**

Run:

```powershell
dotnet build Assembly-CSharp-Editor.csproj --no-restore --no-incremental
dotnet build MobileGame.PlayModeTests.csproj --no-restore --no-incremental
```

Unity MCP:

```text
Run EditMode: BattleRoundStartStatusEffectServiceTests
Run PlayMode: BattleFlowPlayModeTests
Read Console: expect 0 error
```

Expected: targeted tests pass, PlayMode pass, console 0 error.

### Task 2: Stage 456-475，拆出回合开始提前结算 guard

**Files:**

- Modify: `Assets/Scripts/BattleScene/Controller/BattleButton.cs`
- Create: `Assets/Scripts/Shared/BattleRoundStartCompletionGuardService.cs`
- Test: `Assets/Tests/EditMode/Editor/BattleRoundStartCompletionGuardServiceTests.cs`

- [ ] **Step 1: 写 RED 测试，锁定撤退优先于普通完成判定**

```csharp
[Test]
public void Resolve_WhenRetreatSettlementHandled_DoesNotCheckBattleCompletion()
{
    var completionChecked = false;

    var result = BattleRoundStartCompletionGuardService.Resolve(
        handleRetreatSettlement: () => true,
        handleBattleCompletion: () =>
        {
            completionChecked = true;
            return false;
        });

    Assert.IsTrue(result.ShouldStopTargetingSetup);
    Assert.IsFalse(completionChecked);
}
```

Run:

```powershell
dotnet build Assembly-CSharp-Editor.csproj --no-restore --no-incremental
```

Expected: FAIL because service does not exist.

- [ ] **Step 2: 实现 guard 服务**

```csharp
public static class BattleRoundStartCompletionGuardService
{
    public readonly struct Result
    {
        public Result(bool shouldStopTargetingSetup)
        {
            ShouldStopTargetingSetup = shouldStopTargetingSetup;
        }

        public bool ShouldStopTargetingSetup { get; }
    }

    public static Result Resolve(Func<bool> handleRetreatSettlement, Func<bool> handleBattleCompletion)
    {
        if (handleRetreatSettlement != null && handleRetreatSettlement())
            return new Result(true);

        if (handleBattleCompletion != null && handleBattleCompletion())
            return new Result(true);

        return new Result(false);
    }
}
```

- [ ] **Step 3: 接入 `SetupTargetingPhase`**

把当前连续调用：

```csharp
if (TryHandleRoundStartRetreatSettlement(allies, enemies))
    return;
if (TryHandleBattleCompletion(allies, enemies))
    return;
```

替换为：

```csharp
var completionGuard = BattleRoundStartCompletionGuardService.Resolve(
    () => TryHandleRoundStartRetreatSettlement(allies, enemies),
    () => TryHandleBattleCompletion(allies, enemies));
if (completionGuard.ShouldStopTargetingSetup)
    return;
```

- [ ] **Step 4: 验证**

Run full build and targeted tests:

```powershell
dotnet build Assembly-CSharp-Editor.csproj --no-restore --no-incremental
dotnet build MobileGame.PlayModeTests.csproj --no-restore --no-incremental
```

Unity MCP:

```text
Run EditMode: BattleRoundStartCompletionGuardServiceTests
Run PlayMode: BattleFlowPlayModeTests
```

Expected: all pass.

### Task 3: Stage 476-500，统一目标选择入口

**Files:**

- Modify: `Assets/Scripts/BattleScene/Controller/BattleButton.cs`
- Modify: `Assets/Scripts/Shared/BattleTargetSelectionService.cs`
- Test: `Assets/Tests/EditMode/Editor/BattleTargetSelectionServiceTests.cs`

- [ ] **Step 1: 补齐目标选择测试**

覆盖 `CurrentTarget`、`All`、`Random`、死亡目标过滤、随机数量超过候选数量时返回所有候选：

```csharp
[Test]
public void SelectTargets_RandomCountLargerThanCandidates_ReturnsAllLivingCandidates()
{
    var candidates = new[] { "a", "b" };

    var selected = BattleTargetSelectionService.Select(
        EnemySkill_SO.DamageTargetMode.Random,
        currentTarget: "a",
        candidates,
        randomTargetCount: 99,
        isAlive: target => true,
        randomOrder: list => list);

    CollectionAssert.AreEquivalent(candidates, selected);
}
```

- [ ] **Step 2: 扩展 `BattleTargetSelectionService`**

实现一个泛型入口：

```csharp
public static List<T> Select<T>(
    EnemySkill_SO.DamageTargetMode targetMode,
    T currentTarget,
    IEnumerable<T> candidates,
    int randomTargetCount,
    Func<T, bool> isAlive,
    Func<List<T>, List<T>> randomOrder)
```

规则：

- `CurrentTarget`：当前目标非空且存活时只返回当前目标，否则返回空列表。
- `All`：返回全部存活候选。
- `Random`：从存活候选中按 `randomOrder` 后截取 `Mathf.Max(1, randomTargetCount)` 个。
- 其他模式：保持现有项目里的默认语义，不扩大功能。

- [ ] **Step 3: 替换 `BattleButton` 的实例和静态选择方法**

将这些方法改为 wrapper：

```csharp
private List<BattleEnemyController> SelectEnemyTargets(...)
private List<BattleAllyController> SelectAllyTargets(...)
private static List<BattleEnemyController> SelectEnemyTargetsStatic(...)
private static List<BattleAllyController> SelectAllyTargetsStatic(...)
```

wrapper 内只调用 `BattleTargetSelectionService.Select`。

- [ ] **Step 4: 验证**

Run:

```powershell
dotnet build Assembly-CSharp-Editor.csproj --no-restore --no-incremental
dotnet build MobileGame.PlayModeTests.csproj --no-restore --no-incremental
```

Unity MCP:

```text
Run EditMode: BattleTargetSelectionServiceTests
Run EditMode: full
Run PlayMode: full
```

Expected: EditMode count increases from current 494 baseline, all pass.

### Task 4: Stage 501-530，拆友方和敌方 status-only 技能词条路径

**Files:**

- Modify: `Assets/Scripts/BattleScene/Controller/BattleButton.cs`
- Create: `Assets/Scripts/Shared/BattleSkillStatusEffectResolutionService.cs`
- Test: `Assets/Tests/EditMode/Editor/BattleSkillStatusEffectResolutionServiceTests.cs`

- [ ] **Step 1: 写 RED 测试，锁定 conditionalOnly 过滤**

```csharp
[Test]
public void BuildApplications_WhenConditionalOnly_SkipsNonConditionalEffects()
{
    var effects = new[]
    {
        new TestEffect("normal", conditional: false),
        new TestEffect("conditional", conditional: true)
    };

    var result = BattleSkillStatusEffectResolutionService.FilterEffects(
        effects,
        conditionalOnly: true,
        effect => effect.Conditional);

    CollectionAssert.AreEqual(new[] { "conditional" }, result.Select(effect => effect.Id));
}
```

- [ ] **Step 2: 实现服务**

服务输出 application plan，不直接施加状态：

```csharp
public static IReadOnlyList<TStatusEffect> FilterEffects<TStatusEffect>(
    IEnumerable<TStatusEffect> effects,
    bool conditionalOnly,
    Func<TStatusEffect, bool> isConditional)
```

- [ ] **Step 3: 接入 `ApplyAllyStatusEffectsOnly` 和 `ApplyEnemyStatusEffectsOnly`**

保留这两个 private 方法名，内部把“过滤、目标选择、日志文本构建”迁入服务；真实 `target.ApplyStatus(...)` 仍由闭包执行。

- [ ] **Step 4: 验证**

Run:

```powershell
dotnet build Assembly-CSharp-Editor.csproj --no-restore --no-incremental
```

Unity MCP:

```text
Run EditMode: BattleSkillStatusEffectResolutionServiceTests
Run PlayMode: BattleFlowPlayModeTests
Read Console: expect 0 error
```

### Task 5: Stage 531-560，拆敌方 built-in effect 剩余胶水

**Files:**

- Modify: `Assets/Scripts/BattleScene/Controller/BattleButton.cs`
- Create: `Assets/Scripts/Shared/BattleEnemyBuiltInEffectResolutionService.cs`
- Test: `Assets/Tests/EditMode/Editor/BattleEnemyBuiltInEffectResolutionServiceTests.cs`

- [ ] **Step 1: 写 RED 测试，锁定 built-in effect 按 trigger 筛选**

```csharp
[Test]
public void Resolve_OnlyIncludesEffectsMatchingTrigger()
{
    var effects = new[]
    {
        new TestBuiltInEffect("on-use", EnemySkill_SO.DiceEffectTrigger.OnUse),
        new TestBuiltInEffect("on-hit", EnemySkill_SO.DiceEffectTrigger.OnHit)
    };

    var result = BattleEnemyBuiltInEffectResolutionService.FilterByTrigger(
        effects,
        EnemySkill_SO.DiceEffectTrigger.OnHit,
        effect => effect.Trigger);

    CollectionAssert.AreEqual(new[] { "on-hit" }, result.Select(effect => effect.Id));
}
```

- [ ] **Step 2: 实现服务**

服务只做筛选、目标选择参数构建、effect 类型分派计划：

```csharp
public enum BattleEnemyBuiltInEffectKind
{
    Damage,
    Heal,
    Status,
    Transfer,
    Summon
}
```

- [ ] **Step 3: 接入 `ApplyEnemyBuiltInEffects`**

`BattleButton` 内部只保留真实副作用执行：

- 伤害：调用现有 damage service / target damage API。
- 治疗：调用现有 healing service。
- 状态：调用 status application dispatcher。
- 转移：调用 `BattleEnemyBuiltInTransferService`。
- 召唤：调用 `BattleSummonEffectService`。

- [ ] **Step 4: 验证**

Run:

```powershell
dotnet build Assembly-CSharp-Editor.csproj --no-restore --no-incremental
dotnet build MobileGame.PlayModeTests.csproj --no-restore --no-incremental
```

Unity MCP:

```text
Run EditMode: BattleEnemyBuiltInEffectResolutionServiceTests
Run EditMode: full
Run PlayMode: full
```

### Task 6: Stage 561-590，统一命中、未命中、零伤和成功命中管线

**Files:**

- Modify: `Assets/Scripts/BattleScene/Controller/BattleButton.cs`
- Create: `Assets/Scripts/Shared/BattleHitResolutionFacade.cs`
- Test: `Assets/Tests/EditMode/Editor/BattleHitResolutionFacadeTests.cs`

- [ ] **Step 1: 写 RED 测试，锁定零伤不触发成功命中**

```csharp
[Test]
public void Resolve_WhenDamageIsZero_DoesNotRunSuccessfulHit()
{
    var successfulHitCalled = false;
    var zeroDamageCalled = false;

    BattleHitResolutionFacade.Resolve(
        dealtDamage: 0,
        onZeroDamage: () => zeroDamageCalled = true,
        onSuccessfulHit: () => successfulHitCalled = true);

    Assert.IsTrue(zeroDamageCalled);
    Assert.IsFalse(successfulHitCalled);
}
```

- [ ] **Step 2: 实现 facade**

Facade 只决定调用顺序：

```csharp
public static void Resolve(int dealtDamage, Action onZeroDamage, Action onSuccessfulHit)
{
    if (dealtDamage <= 0)
    {
        onZeroDamage?.Invoke();
        return;
    }

    onSuccessfulHit?.Invoke();
}
```

- [ ] **Step 3: 接入 hit outcome**

替换这些区域的重复判断：

- `ApplyAllyHit`
- `ApplyEnemyHit`
- `ApplyAllyHitOutcome`
- `ApplyEnemyHitOutcome`
- `ApplyAllySuccessfulHitEffects`
- `ApplyEnemySuccessfulHitEffects`

要求 `BattleButton` 仍负责真实伤害调用和动画，facade 只决定“零伤路径还是成功命中路径”。

- [ ] **Step 4: 验证**

Run:

```powershell
dotnet build Assembly-CSharp-Editor.csproj --no-restore --no-incremental
dotnet build MobileGame.PlayModeTests.csproj --no-restore --no-incremental
```

Unity MCP:

```text
Run EditMode: BattleHitResolutionFacadeTests
Run PlayMode: BattleFlowPlayModeTests
Run Console check
```

### Task 7: Stage 591-620，拆规则 plan 和表现协程边界

**Files:**

- Modify: `Assets/Scripts/BattleScene/Controller/BattleButton.cs`
- Create: `Assets/Scripts/Shared/BattleCombatPresentationCoroutinePlanService.cs`
- Test: `Assets/Tests/EditMode/Editor/BattleCombatPresentationCoroutinePlanServiceTests.cs`

- [ ] **Step 1: 写 RED 测试，锁定 clash 与 single attack 表现步骤顺序**

```csharp
[Test]
public void BuildClashPresentation_ReturnsMoveRollImpactReturnOrder()
{
    var steps = BattleCombatPresentationCoroutinePlanService.BuildClashPresentation();

    CollectionAssert.AreEqual(
        new[]
        {
            BattlePresentationStep.MoveToTarget,
            BattlePresentationStep.RollDice,
            BattlePresentationStep.ApplyImpact,
            BattlePresentationStep.ReturnToOrigin
        },
        steps);
}
```

- [ ] **Step 2: 实现表现 plan 服务**

定义：

```csharp
public enum BattlePresentationStep
{
    MoveToTarget,
    RollDice,
    ApplyImpact,
    ReturnToOrigin
}
```

- [ ] **Step 3: 接入协程**

让这些协程先读取表现步骤，再逐步执行已有 Unity 副作用：

- `ResolveClashAction`
- `ResolveAllySingleAttackAction`
- `ResolveEnemySingleAttackAction`

不改变动画等待时间、不改变日志顺序、不改变伤害施加时机。

- [ ] **Step 4: 验证**

Run targeted and full PlayMode:

```text
Run PlayMode: BattleFlowPlayModeTests
Run PlayMode: full
```

Expected: 拼点、友方单方面攻击、敌方单方面攻击回归通过。

### Task 8: Stage 621-650，收束 UI / overlay / tooltip 阻塞判断

**Files:**

- Modify: `Assets/Scripts/BattleScene/Controller/BattleButton.cs`
- Create: `Assets/Scripts/Shared/BattleButtonUiOverlayStateService.cs`
- Test: `Assets/Tests/EditMode/Editor/BattleButtonUiOverlayStateServiceTests.cs`

- [ ] **Step 1: 写 RED 测试，锁定阻塞优先级**

```csharp
[Test]
public void ResolveClickBlock_WhenSettlementVisible_BlocksBattleButton()
{
    var result = BattleButtonUiOverlayStateService.ResolveClickBlock(
        settlementVisible: true,
        tutorialBlocking: false,
        skillInfoVisible: false);

    Assert.IsTrue(result.ShouldBlock);
    Assert.AreEqual(BattleButtonClickBlockReason.SettlementVisible, result.Reason);
}
```

- [ ] **Step 2: 实现服务**

定义：

```csharp
public enum BattleButtonClickBlockReason
{
    None,
    SettlementVisible,
    TutorialBlocking,
    SkillInfoVisible
}
```

- [ ] **Step 3: 接入 `OnBattleButtonClicked`**

将 overlay / tutorial / skill info 早退判断改为读取服务结果。日志和 UI 提示仍保留在 `BattleButton`。

- [ ] **Step 4: 验证**

Run:

```powershell
dotnet build Assembly-CSharp-Editor.csproj --no-restore --no-incremental
```

Unity MCP:

```text
Run EditMode: BattleButtonUiOverlayStateServiceTests
Run PlayMode: BattleFlowPlayModeTests
```

### Task 9: Stage 651-680，补齐关键 PlayMode 回归

**Files:**

- Modify: `Assets/Tests/PlayMode/BattleFlowPlayModeFixture.cs`
- Modify: `Assets/Tests/PlayMode/BattleFlowPlayModeTests.cs`

- [ ] **Step 1: 增加状态 tick 回归**

新增测试名：

```csharp
RoundStart_WhenBurningStatusExists_AppliesTickBeforeTargetingInput
```

断言：

- 点击进入目标指定阶段后，带燃烧的目标先扣血。
- 若 tick 导致死亡，进入完成判定，不允许继续划线。

- [ ] **Step 2: 增加撤退结算回归**

新增测试名：

```csharp
RoundStart_WhenAllEnemiesRetreat_ShowsVictorySettlementWithoutTargetingInput
```

断言：

- 回合开始触发撤退后直接显示胜利结算。
- 不显示敌方速度按钮交互。

- [ ] **Step 3: 增加复杂技能效果回归**

新增测试名：

```csharp
ClashVictory_WhenSkillHasOnUseOnHitAndStatusEffects_AppliesEffectsOnce
```

断言：

- 使用时效果只触发一次。
- 命中时效果只触发一次。
- 状态层数符合配置。

- [ ] **Step 4: 跑全量**

Run:

```powershell
dotnet build MobileGame.PlayModeTests.csproj --no-restore --no-incremental
```

Unity MCP:

```text
Run PlayMode: full
Run EditMode: full
Read Console: expect 0 error
```

Expected: PlayMode 数量从当前 9 增长到至少 12，全部通过。

### Task 10: Stage 681-710，清理 legacy wrapper 和未使用代码

**Files:**

- Modify: `Assets/Scripts/BattleScene/Controller/BattleButton.cs`
- Modify: `PROJECT_PROGRESS.md`

- [ ] **Step 1: 查找只剩转发的私有方法**

Run:

```powershell
rg -n "private .*SelectEnemyTargets|private .*SelectAllyTargets|private .*ApplyRoundStartStatusEffectsGeneric|private .*ApplyAllyStatusEffectsOnly|private .*ApplyEnemyStatusEffectsOnly|private .*ApplyEnemyBuiltInEffects" "Assets/Scripts/BattleScene/Controller/BattleButton.cs"
```

- [ ] **Step 2: 删除无外部依赖的 wrapper**

只删除满足这些条件的方法：

- 只有 `BattleButton` 内部调用。
- 调用点已经可以直接调用 service。
- 删除后 `dotnet build` 能立刻发现所有遗漏调用。

- [ ] **Step 3: 清理 using 和字段**

Run:

```powershell
dotnet build Assembly-CSharp-Editor.csproj --no-restore --no-incremental
```

根据编译器提示删除未使用字段和 using。不要批量格式化 `BattleButton.cs`。

- [ ] **Step 4: 更新进度文档**

在 `PROJECT_PROGRESS.md` 末尾追加中文记录：

```markdown
## 2026-04-30 - BattleButton 主体重构收口 Stage 436-710

- 完成回合开始状态 tick、提前结算 guard、目标选择、status-only 技能词条、敌方 built-in effect、命中管线、表现协程边界、UI 阻塞判断的服务化。
- 新增对应 EditMode 覆盖，并补齐状态 tick、撤退结算、复杂技能效果 PlayMode 回归。
- `BattleButton` 保留 Unity 场景编排、真实副作用闭包和兼容入口，核心规则继续迁入 `Assets/Scripts/Shared/Battle*.cs`。
- 验证：记录本轮 dotnet build、Unity EditMode、Unity PlayMode 和 Console 结果。
```

- [ ] **Step 5: 最终验证**

Run:

```powershell
git diff --check -- "Assets/Scripts/BattleScene/Controller/BattleButton.cs" "Assets/Scripts/Shared" "Assets/Tests/EditMode/Editor" "Assets/Tests/PlayMode"
dotnet build Assembly-CSharp-Editor.csproj --no-restore --no-incremental
dotnet build MobileGame.PlayModeTests.csproj --no-restore --no-incremental
```

Unity MCP:

```text
Run EditMode: full
Run PlayMode: full
Read Console: expect 0 error
```

Expected: EditMode 全量通过，PlayMode 全量通过，Console 0 error。

---

## 提交流程

每完成 1-2 个 task 提交一次，避免一个提交跨太多风险点。

推荐提交节奏：

- `v0.0.48e`：Stage 436-475，回合开始状态 tick 与提前结算 guard。
- `v0.0.48f`：Stage 476-530，目标选择和 status-only 技能词条。
- `v0.0.48g`：Stage 531-590，敌方 built-in effect 和命中管线。
- `v0.0.48h`：Stage 591-650，表现协程边界和 UI 阻塞判断。
- `v0.0.48i`：Stage 651-710，PlayMode 补强、legacy wrapper 清理、进度文档。

每次提交只 stage 本轮相关文件：

```powershell
git status --short
git add "Assets/Scripts/BattleScene/Controller/BattleButton.cs" "Assets/Scripts/Shared" "Assets/Tests/EditMode/Editor" "Assets/Tests/PlayMode" "PROJECT_PROGRESS.md"
git commit -m "v0.0.48e"
git push
```

如果 push 失败并出现 `Recv failure: Connection was reset`，本地提交保留，记录失败原因，下一轮继续尝试 push。

## 风险控制

- 不对 `BattleButton.cs` 做全文件格式化，避免历史编码和中文乱码造成大 diff。
- 不修改无关 `.plastic` 文件。
- 不把真实 Unity 对象副作用塞进纯服务；纯服务只返回 plan、result、selection、filtered effect。
- 每次拆分只迁一个职责，不在同一阶段同时改规则和表现。
- 如果 PlayMode 失败，优先回看最近迁出的服务排序，不直接回滚所有重构。
- 如果 Unity MCP 暂时不可用，至少完成 `dotnet build` 和定向 EditMode 编译；恢复 MCP 后补跑全量 Unity tests。

## Self-Review

- Spec coverage: 本计划覆盖 `BattleButton` 已完成 Stage 1-435 的重构历史，也覆盖剩余高风险点：回合开始状态、提前结算、目标选择、status-only 技能、敌方 built-in effect、命中管线、表现协程、UI 阻塞和 PlayMode 回归。
- Placeholder scan: 文档未使用“之后再说”式占位；每个剩余 task 都有明确文件、测试、接入点和验证命令。
- Type consistency: 新服务命名全部使用现有 `Battle*.cs` 风格，测试文件位于现有 EditMode / PlayMode 目录，提交版本号延续 `v0.0.48*`。
