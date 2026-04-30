# BattleButton 轻量编排器完成计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 将 `Assets/Scripts/BattleScene/Controller/BattleButton.cs` 收束为轻量战斗流程编排器，只保留 Unity 场景引用、协程入口、真实对象副作用闭包和少量兼容入口。

**Architecture:** 后续不再继续无边界地新增小服务，而是按高风险职责域收口：敌方 built-in 执行、技能效果执行、结算/掉落、UI/教程 glue、组件解析与 HUD。纯规则和副作用调度进入 `Assets/Scripts/Shared/Battle*.cs`，`BattleButton` 只传入回调执行真实 `TakeDamage`、`Heal`、`AddStatus`、`BattleLog`、overlay 显示和协程播放。每个阶段先补 EditMode 覆盖，涉及完整战斗流程时补 PlayMode 回归。

**Tech Stack:** Unity C#、NUnit EditMode/PlayMode、Unity MCP Test Runner、`dotnet build`、现有 `Assembly-CSharp` / `Assembly-CSharp-Editor` / `MobileGame.PlayModeTests`。

---

## 当前基线

- 当前提交基线：`b2beffb6 v0.0.48f`。
- 当前进度：`Stage 710` 已完成。
- 当前 `BattleButton.cs` 规模：约 `4018` 行。
- 当前 `Assets/Scripts/Shared/Battle*.cs` 服务：约 `123` 个。
- 最近完整验证：`dotnet build Assembly-CSharp-Editor.csproj --no-restore --no-incremental` 通过；`dotnet build MobileGame.PlayModeTests.csproj --no-restore --no-incremental` 通过；Unity EditMode `508/508` 通过；Unity PlayMode `12/12` 通过；Unity Console `0 error`。
- 工作树注意事项：`.plastic/plastic.changes`、`.plastic/plastic.wktree`、`TODO.md` 不属于本计划提交范围，除非用户单独要求。

## 最终验收边界

`BattleButton` 应保留：

- `Awake`、`OnDisable`、`OnBattleButtonClicked`、`SetupTargetingPhase`、`ExecuteCombatPhaseRoutine` 这类流程入口。
- Unity Inspector 字段、overlay / presentation / feedback / tutorial / settlement 的对象引用。
- 调用真实 Unity 对象副作用的闭包：`TakeDamage`、`Heal`、`AddStatus`、`RemoveStatusStacks`、`QueueStatusForRoundStart`、`BattleLog`、`StartCoroutine`。
- 少量历史兼容 wrapper，但这些 wrapper 不能再包含规则分支。

`BattleButton` 不应保留：

- 敌方 built-in effect 的分支执行规则。
- 技能 healing / status burst / status stack operation 的目标遍历和效果执行分支。
- 结算数据、掉落数据、精英遗物三选一 fallback 的构建规则。
- 共享能量 HUD GameObject 结构创建规则。
- 教程/技能详情 overlay 的事件解释规则。
- 可测试的字符串、日志、标签、条件判断规则。

## 阶段 711-760：敌方 built-in effect 真实执行收口

**Files:**

- Modify: `Assets/Scripts/BattleScene/Controller/BattleButton.cs`
- Create: `Assets/Scripts/Shared/BattleEnemyBuiltInEffectDispatcher.cs`
- Test: `Assets/Tests/EditMode/Editor/BattleEnemyBuiltInEffectDispatcherTests.cs`

- [ ] **Step 1: 写 RED 测试**

覆盖四个路径：`Heal`、`Damage`、`ApplyStatus`、`TransferRandomNegativeStatus`。测试只验证 dispatcher 调用顺序和回调参数，不直接依赖 Unity 场景。

Run:

```powershell
dotnet build Assembly-CSharp-Editor.csproj --no-restore --no-incremental
```

Expected: 因缺少 `BattleEnemyBuiltInEffectDispatcher` 编译失败。

- [ ] **Step 2: 实现 dispatcher**

新增 dispatcher 只接收 effect、targets、status resolver、damage/heal/status/transfer 回调，返回 `BattleEnemyBuiltInEffectDispatchResult`。真实 `Heal`、`TakeDamage`、`AddStatus` 仍由 `BattleButton` 闭包执行。

- [ ] **Step 3: 接入 `BattleButton.ApplyEnemyBuiltInEffects`**

将 `ApplyEnemyBuiltInHealingEffect`、`ApplyEnemyBuiltInDamageEffect`、`ApplyEnemyBuiltInStatusEffect`、`ApplyEnemyBuiltInTransferEffect` 的分支主体迁入 dispatcher。`BattleButton` 保留同名 wrapper 或删除无调用 wrapper，视编译结果决定。

- [ ] **Step 4: 验证**

Run:

```powershell
dotnet build Assembly-CSharp-Editor.csproj --no-restore --no-incremental
```

Run Unity EditMode target:

```text
BattleEnemyBuiltInEffectDispatcherTests
```

Expected: build 通过；target tests 全部通过；Console `0 error`。

## 阶段 761-820：技能 healing / status burst / status stack operation 执行收口

**Files:**

- Modify: `Assets/Scripts/BattleScene/Controller/BattleButton.cs`
- Create: `Assets/Scripts/Shared/BattleSkillEffectExecutionDispatcher.cs`
- Modify: `Assets/Scripts/Shared/BattleHealingEffectDispatcher.cs`
- Modify: `Assets/Scripts/Shared/BattleStatusBurstDispatcher.cs`
- Modify: `Assets/Scripts/Shared/BattleStatusStackOperationDispatcher.cs`
- Test: `Assets/Tests/EditMode/Editor/BattleSkillEffectExecutionDispatcherTests.cs`
- Test: existing dispatcher tests under `Assets/Tests/EditMode/Editor`

- [ ] **Step 1: 锁定现有行为测试**

新增测试覆盖友方 healing、敌方 healing、status burst 清层/不清层、status stack operation 的 clear/reduce，以及玩家遗物 outgoing damage modifier 只作用于玩家对敌方 status burst。

- [ ] **Step 2: 新增组合 dispatcher**

`BattleSkillEffectExecutionDispatcher` 负责按触发时机执行 healing、status effect、status burst、status stack operation、summon、stage transition 的组合调度。它不做真实伤害和状态修改，只串联已有 dispatcher 和回调。

- [ ] **Step 3: 接入命中后路径**

替换 `ApplyAllySuccessfulHitEffects`、`ApplyEnemySuccessfulHitEffects`、`ApplyAllyTriggeredEffects`、`ApplyEnemyTriggeredEffects` 中的本地分支组合。保留 `BattleButton` 对 `ApplyPlayerRelicOutgoingDamageModifiers`、`ApplyEnemyCalamityDamageModifiers` 的闭包注入。

- [ ] **Step 4: 验证**

Run:

```powershell
dotnet build Assembly-CSharp-Editor.csproj --no-restore --no-incremental
dotnet build MobileGame.PlayModeTests.csproj --no-restore --no-incremental
```

Run Unity target EditMode for all changed dispatcher tests. If any hit flow changed, run full PlayMode.

## 阶段 821-860：结算、经验、掉落与精英遗物三选一收口

**Files:**

- Modify: `Assets/Scripts/BattleScene/Controller/BattleButton.cs`
- Create: `Assets/Scripts/Shared/BattleSettlementFlowService.cs`
- Create: `Assets/Scripts/Shared/BattleEncounterRewardFlowService.cs`
- Modify: `Assets/Scripts/Shared/BattleEliteRelicChoiceBuilderService.cs`
- Test: `Assets/Tests/EditMode/Editor/BattleSettlementFlowServiceTests.cs`
- Test: `Assets/Tests/EditMode/Editor/BattleEncounterRewardFlowServiceTests.cs`
- Modify: `Assets/Tests/PlayMode/BattleFlowPlayModeTests.cs`

- [ ] **Step 1: 结算 flow 测试**

覆盖胜利、失败、撤退三种结算入口，验证 exp、money、loot、relic choice、run state 更新数据由 service 统一构建。

- [ ] **Step 2: 掉落 flow 测试**

覆盖普通掉落、精英遗物三选一、fallback table、Editor asset fallback 禁用/启用路径。Editor fallback 的 AssetDatabase 读取仍留在 `BattleButton` 或 `BattleEditorAssetFallbackService`，规则判断迁入 `BattleEncounterRewardFlowService`。

- [ ] **Step 3: 接入 `TryHandleBattleCompletion` 和 `TryHandleRoundStartRetreatSettlement`**

`BattleButton` 只调用 flow service 获得 settlement data 和 runtime state patch，再执行 `activeEncounter.ApplyBattleResult`、`battleSettlementOverlay.ShowResult`、`BattleLog`。

- [ ] **Step 4: PlayMode 回归**

新增或扩展 PlayMode：精英战胜利后生成问号战利品并携带三选一数据；选择遗物后 run state 拥有该遗物；撤退结算不会重复发放奖励。

## 阶段 861-900：共享能量 HUD 与技能详情/教程 overlay glue 收口

**Files:**

- Modify: `Assets/Scripts/BattleScene/Controller/BattleButton.cs`
- Create: `Assets/Scripts/Shared/BattleSharedEnergyHudViewFactory.cs`
- Create: `Assets/Scripts/Shared/BattleSkillInfoOverlayEventService.cs`
- Create: `Assets/Scripts/Shared/TutorialBattleOverlayFlowService.cs`
- Test: `Assets/Tests/EditMode/Editor/BattleSharedEnergyHudViewFactoryTests.cs`
- Test: `Assets/Tests/EditMode/Editor/BattleSkillInfoOverlayEventServiceTests.cs`
- Test: `Assets/Tests/EditMode/Editor/TutorialBattleOverlayFlowServiceTests.cs`

- [ ] **Step 1: HUD factory 测试**

锁定 Canvas、panel、bar、text、round counter 的结构参数与 resize 规则。测试不创建场景，只验证配置对象和 layout plan。

- [ ] **Step 2: HUD factory 接入**

`BattleButton.EnsureSharedEnergyUi` 改为调用 factory plan，然后由 `BattleButton` 创建真实 GameObject。`RefreshSharedEnergyUi` 继续使用 `BattleSharedEnergyHudService`。

- [ ] **Step 3: overlay event service**

将 `HandleSkillInfoOverlayHidden`、`HandleAllySkillSelectionShown`、`HandleEnemySkillInfoShown`、`HandleEnemyDetailShown`、`HandleTutorialProgress` 中的事件解释迁入 service。`BattleButton` 只负责订阅事件和显示 overlay。

- [ ] **Step 4: 验证**

Run EditMode target tests；再跑 PlayMode 中教程/技能详情相关流程。

## 阶段 901-940：回合开始和回合结束残留状态流收口

**Files:**

- Modify: `Assets/Scripts/BattleScene/Controller/BattleButton.cs`
- Create: `Assets/Scripts/Shared/BattleRoundStatusFlowDispatcher.cs`
- Modify: `Assets/Scripts/Shared/BattleRoundStartStatusEffectService.cs`
- Modify: `Assets/Scripts/Shared/BattleCombatRoundEndDispatcher.cs`
- Test: `Assets/Tests/EditMode/Editor/BattleRoundStatusFlowDispatcherTests.cs`
- Modify: `Assets/Tests/PlayMode/BattleFlowPlayModeTests.cs`

- [ ] **Step 1: 测试 round-start 与 round-end 顺序**

覆盖状态 tick、状态驱动阶段切换、撤退结算 guard、round-end 状态消耗、round-end relic 的顺序。

- [ ] **Step 2: 新增 round status dispatcher**

统一 `ApplyRoundStartStatusEffectsGeneric`、`ApplyRoundStartStatusEffects`、`ApplyRoundEndStatusEffectsGeneric` 的调度入口。真实 `ConsumeRoundStartStatusEffects`、`ConsumeRoundEndStatusEffects` 和表现播放仍由闭包执行。

- [ ] **Step 3: 接入主流程**

`SetupTargetingPhase` 和 `ExecuteCombatPhaseRoutine` 只调用 round status dispatcher，不再直接遍历 ally/enemy 执行状态消耗。

- [ ] **Step 4: 验证**

Run full PlayMode，因为该阶段触碰战斗主循环时序。

## 阶段 941-980：组件解析、事件绑定和生命周期清理

**Files:**

- Modify: `Assets/Scripts/BattleScene/Controller/BattleButton.cs`
- Create: `Assets/Scripts/Shared/BattleButtonDependencyResolver.cs`
- Create: `Assets/Scripts/Shared/BattleButtonLifecycleBindingService.cs`
- Test: `Assets/Tests/EditMode/Editor/BattleButtonDependencyResolverTests.cs`
- Test: `Assets/Tests/EditMode/Editor/BattleButtonLifecycleBindingServiceTests.cs`

- [ ] **Step 1: 依赖解析测试**

覆盖 presentation、feedback、settlement、skill info、tutorial guide 的 scene-or-host 查找策略。

- [ ] **Step 2: 生命周期绑定测试**

覆盖 `OnDisable` 时 active round combatants、shared energy listener、skill info overlay event 全部解绑。

- [ ] **Step 3: 接入 `Awake` / `OnDisable`**

`BattleButton` 的 `Resolve*` 方法压缩为一次 dependency resolver 调用；`OnDisable` 调用 lifecycle binding service 清理事件。

- [ ] **Step 4: 验证**

Run target EditMode tests；再跑 full EditMode。

## 阶段 981-1020：删除 legacy wrapper 与文件体积收口

**Files:**

- Modify: `Assets/Scripts/BattleScene/Controller/BattleButton.cs`
- Modify: `PROJECT_PROGRESS.md`

- [ ] **Step 1: 查找只转发 wrapper**

Run:

```powershell
rg -n "private .*\(|static .*\(" "Assets/Scripts/BattleScene/Controller/BattleButton.cs"
```

人工标记满足以下条件的方法：只有内部调用、方法体只调用 service、没有日志/闭包/Unity 对象副作用。

- [ ] **Step 2: 删除 wrapper**

逐个删除，删除后立即运行：

```powershell
dotnet build Assembly-CSharp-Editor.csproj --no-restore --no-incremental
```

Expected: 每轮删除后 build 通过；如果失败，只恢复该 wrapper 或替换调用点。

- [ ] **Step 3: 最终规模检查**

Run:

```powershell
(Get-Content -Path "Assets/Scripts/BattleScene/Controller/BattleButton.cs" | Measure-Object -Line).Lines
```

目标：`BattleButton.cs` 降到 `2500-3000` 行区间。若仍高于 `3000` 行，保留现状并记录剩余大块，不继续冒险压缩。

- [ ] **Step 4: 全量验证**

Run:

```powershell
git diff --check -- "Assets/Scripts/BattleScene/Controller/BattleButton.cs" "Assets/Scripts/Shared" "Assets/Tests/EditMode/Editor" "Assets/Tests/PlayMode" "PROJECT_PROGRESS.md"
dotnet build Assembly-CSharp-Editor.csproj --no-restore --no-incremental
dotnet build MobileGame.PlayModeTests.csproj --no-restore --no-incremental
```

Run Unity:

```text
Full EditMode
Full PlayMode
Console error check
```

Expected: EditMode 全部通过；PlayMode 全部通过；Console `0 error`。

## 建议提交节奏

- `v0.0.48g`：Stage 711-760，敌方 built-in effect 执行收口。
- `v0.0.48h`：Stage 761-820，技能效果执行收口。
- `v0.0.48i`：Stage 821-860，结算/掉落/精英遗物选择收口。
- `v0.0.48j`：Stage 861-900，HUD 与 overlay glue 收口。
- `v0.0.48k`：Stage 901-940，round status flow 收口。
- `v0.0.48l`：Stage 941-1020，生命周期、legacy wrapper、最终验证。

每次提交只 stage 本阶段相关文件，不纳入 `.plastic` 和无关设计文档。

## 风险控制

- 不全文件格式化 `BattleButton.cs`。
- 不整体重写 `PROJECT_PROGRESS.md`，只追加中文记录。
- 不在纯服务中直接引用 Unity 场景对象；真实副作用通过闭包注入。
- 每个阶段至少一个目标 EditMode 测试；触碰主循环、结算或教程流程时必须跑 PlayMode。
- 如果某阶段发现服务抽象导致回调过多且可读性下降，停止继续拆该块，保留 `BattleButton` glue 并记录原因。

## Self-Review

- Spec coverage: 覆盖轻量编排器目标下的剩余主要职责域：敌方 built-in、技能效果、结算奖励、HUD/overlay、round status、生命周期和 wrapper 清理。
- Placeholder scan: 文档没有使用待补占位；每个阶段都有明确文件、测试、验证命令和完成边界。
- Type consistency: 新服务命名延续现有 `Battle*.cs` 风格；测试路径延续 `Assets/Tests/EditMode/Editor` 与 `Assets/Tests/PlayMode`。
