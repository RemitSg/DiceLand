# Battle Flow Regression And Refactor Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 先补齐战斗流程 PlayMode 回归覆盖，再继续低风险收窄 `BattleButton` 的流程胶水。

**Architecture:** 不立即拆主 coroutine 状态机。先把 PlayMode fixture 扩展成能构造单方面攻击、失败结算、精英遗物选择、教程阻塞等场景；每增加一类端到端保障，再抽一个不改变副作用顺序的小服务。

**Tech Stack:** Unity PlayMode Tests, NUnit, Unity MCP, C# default `Assembly-CSharp`, reflection-based PlayMode fixture.

---

### Task 1: 扩展 PlayMode Fixture

**Files:**
- Modify: `Assets/Tests/PlayMode/BattleFlowPlayModeFixture.cs`
- Test: `Assets/Tests/PlayMode/BattleFlowPlayModeTests.cs`

- [ ] **Step 1: 写失败测试**

在 `BattleFlowPlayModeTests.cs` 增加单方面攻击测试，先调用尚未存在的 fixture 方法：

```csharp
[UnityTest]
public IEnumerator AllySingleAttackRound_WhenEnemyDoesNotContest_DamagesEnemyAndReturnsReady()
{
    fixture.BuildOneOnOneBattle(allyPower: 3, enemyPower: 1, enemyHp: 12);

    fixture.ClickBattleButton();
    yield return null;

    fixture.ForceAllySingleAttackConnection();
    fixture.ClickBattleButton();
    yield return fixture.WaitUntilCombatSettles();

    Assert.AreEqual("Ready", fixture.CurrentBattlePhase);
    Assert.Greater(fixture.EnemyCurrentHp, 0);
    Assert.Less(fixture.EnemyCurrentHp, 12);
    Assert.IsFalse(fixture.SettlementOverlayVisible);
}
```

- [ ] **Step 2: 验证 RED**

Run: `mcp__unityMCP__.run_tests(mode="PlayMode", test_names=["BattleFlowPlayModeTests.AllySingleAttackRound_WhenEnemyDoesNotContest_DamagesEnemyAndReturnsReady"])`

Expected: FAIL because `ForceAllySingleAttackConnection` is missing.

- [ ] **Step 3: 实现 fixture 方法**

在 `BattleFlowPlayModeFixture` 增加：

```csharp
public void ForceAllySingleAttackConnection()
{
    SetField(AllyButton, "isConnected", true);
    SetField(AllyButton, "connectedButton", EnemyButton);
    SetField(AllyButton, "absorbedEnemyButton", null);
}
```

- [ ] **Step 4: 验证 GREEN**

Run: targeted PlayMode test above.

Expected: PASS.

---

### Task 2: 覆盖失败结算

**Files:**
- Modify: `Assets/Tests/PlayMode/BattleFlowPlayModeFixture.cs`
- Modify: `Assets/Tests/PlayMode/BattleFlowPlayModeTests.cs`

- [ ] **Step 1: 写失败测试**

增加敌方单方面击杀友方的 PlayMode 测试：

```csharp
[UnityTest]
public IEnumerator EnemySingleAttackRound_WhenAllyDies_ShowsDefeatSettlement()
{
    fixture.BuildOneOnOneBattle(allyPower: 1, enemyPower: 30, enemyHp: 20, allyHp: 6);

    fixture.ClickBattleButton();
    yield return null;

    fixture.ForceEnemySingleAttackConnection();
    fixture.ClickBattleButton();
    yield return fixture.WaitUntilCombatSettles();

    Assert.IsTrue(fixture.AllyIsDead);
    Assert.IsTrue(fixture.SettlementOverlayVisible);
}
```

- [ ] **Step 2: 验证 RED**

Run targeted PlayMode test.

Expected: FAIL because overloaded `BuildOneOnOneBattle(... allyHp)` or `ForceEnemySingleAttackConnection` is missing.

- [ ] **Step 3: 最小实现**

给 fixture 增加 `allyHp` 可选参数，并增加：

```csharp
public void ForceEnemySingleAttackConnection()
{
    SetField(EnemyButton, "isConnected", true);
    SetField(EnemyButton, "connectedButton", AllyButton);
}

public bool AllyIsDead => (bool)GetProperty(Ally, "IsDead");
```

- [ ] **Step 4: 验证 GREEN**

Run targeted PlayMode test, then full PlayMode.

Expected: PASS.

---

### Task 3: 覆盖教程阻塞/放行

**Files:**
- Modify: `Assets/Tests/PlayMode/BattleFlowPlayModeFixture.cs`
- Modify: `Assets/Tests/PlayMode/BattleFlowPlayModeTests.cs`

- [ ] **Step 1: 写失败测试**

增加教程阻塞测试，目标是防止第三战那类“看清敌方行动后仍卡住”的问题回归：

```csharp
[UnityTest]
public IEnumerator TutorialPolicy_WhenCombatBlocked_DoesNotEnterResolvingCombat()
{
    fixture.BuildOneOnOneBattle(allyPower: 8, enemyPower: 1, enemyHp: 4);
    fixture.BeginBlockingTutorialStep();

    fixture.ClickBattleButton();
    yield return null;

    fixture.ForceContestedConnection();
    fixture.ClickBattleButton();
    yield return null;

    Assert.IsFalse(fixture.IsResolvingCombat);
    Assert.AreEqual("Targeting", fixture.CurrentBattlePhase);
}
```

- [ ] **Step 2: 验证 RED**

Run targeted PlayMode test.

Expected: FAIL because `BeginBlockingTutorialStep` is missing.

- [ ] **Step 3: 实现测试定义构造**

在 fixture 中用反射创建 `TutorialBattleDefinition_SO` 与一个 `TutorialBattleStepDefinition`，设置 `inputPolicy = RequireBeforeCombat`，然后调用 `TutorialBattleRuntime.BeginBattle(definition)`。

- [ ] **Step 4: 验证 GREEN**

Run targeted PlayMode test.

Expected: PASS.

---

### Task 4: 覆盖精英三选一结算链路

**Files:**
- Modify: `Assets/Tests/PlayMode/BattleFlowPlayModeFixture.cs`
- Modify: `Assets/Tests/PlayMode/BattleFlowPlayModeTests.cs`

- [ ] **Step 1: 写失败测试**

增加精英胜利后结算数据包含遗物选择的 PlayMode 测试：

```csharp
[UnityTest]
public IEnumerator EliteVictory_WhenRelicChoiceEnabled_SettlementContainsRelicChoice()
{
    fixture.BuildEliteBattleWithRelicChoices(allyPower: 8, enemyPower: 1, enemyHp: 4);

    fixture.ClickBattleButton();
    yield return null;

    fixture.ForceContestedConnection();
    fixture.ClickBattleButton();
    yield return fixture.WaitUntilCombatSettles();

    Assert.IsTrue(fixture.SettlementOverlayVisible);
    Assert.IsTrue(fixture.LastSettlementHasRelicChoice);
}
```

- [ ] **Step 2: 验证 RED**

Run targeted PlayMode test.

Expected: FAIL because fixture does not yet create elite encounter/relic table data.

- [ ] **Step 3: 最小实现**

在 fixture 中构造 `BattleEncounterData`，设置 elite reward choice 所需字段与 3 个临时 `Relic_SO`，调用 `BattleEncounterRuntime.SetActiveEncounter` 或项目现有等价入口，并通过反射读取 `BattleSettlementOverlay` 当前展示数据。

- [ ] **Step 4: 验证 GREEN**

Run targeted PlayMode test.

Expected: PASS.

---

### Task 5: 继续低风险拆 `BattleButton`

**Files:**
- Modify: `Assets/Scripts/BattleScene/Controller/BattleButton.cs`
- Create or modify: `Assets/Scripts/Shared/BattleCombatPhaseGuardService.cs`
- Test: `Assets/Tests/EditMode/Editor/BattleCombatPhaseGuardServiceTests.cs`

- [ ] **Step 1: 写纯服务失败测试**

新增服务测试，覆盖 overlay/tutorial/skill-info 可见时是否阻止按钮进入下一阶段。

- [ ] **Step 2: 验证 RED**

Run targeted EditMode test.

Expected: FAIL because service is missing.

- [ ] **Step 3: 实现纯服务**

实现只返回 `bool shouldBlock` 的小服务，不接触 Unity UI 真实副作用。

- [ ] **Step 4: 接入 `BattleButton.OnBattleButtonClicked`**

把当前 overlay/tutorial/skill info 的早退判断替换为服务判断，保持原顺序。

- [ ] **Step 5: 验证**

Run:

```powershell
dotnet build Assembly-CSharp-Editor.csproj --no-restore
```

Run Unity tests:

```text
PlayMode all
EditMode all
Unity Console errors
```

Expected: all pass, console 0 error.

---

### Task 6: 更新进度文档

**Files:**
- Modify: `PROJECT_PROGRESS.md`

- [ ] **Step 1: 追加中文进度**

追加 Stage 232+ 条目，记录新增 PlayMode 覆盖、验证结果、仍未拆的高风险点。

- [ ] **Step 2: 验证文档可读**

Run:

```powershell
Get-Content -Path PROJECT_PROGRESS.md -Tail 40 -Encoding UTF8
```

Expected: 新条目可正常显示。

---

## Self-Review

- Spec coverage: 覆盖当前最缺的 PlayMode 回归、失败结算、教程阻塞、精英遗物选择和下一步低风险拆分。
- Placeholder scan: 无 TBD/TODO/类似占位。
- Type consistency: 所有新增方法均限定在 PlayMode fixture 或新增纯服务中，生产路径只在 Task 5 低风险接入。
