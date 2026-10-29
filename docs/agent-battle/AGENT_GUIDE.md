# Agent 接入指南

接口允许外部 agent 通过 JSON 玩一局完整冒险：探索、构筑、战斗、胜负结算、局外升级和下一局。正式界面与接口使用相同玩法服务。任何支持 HTTP 的 agent 都能调用，不依赖特定模型、MCP、Python 或图形界面。

后端目前需要有授权的团结/Unity 编辑器，可用 `-batchmode -nographics` 启动。每个宿主同时只能存在一个单场战斗或冒险会话；不支持同宿主并行对局。启动方法见 [README](README.md)。

请求地址：`POST http://127.0.0.1:8091/v1/command`，请求头 `Content-Type: application/json`。下文 JSON 每段都是一次独立请求；ID 和版本来自实际响应。

## 建立或继续冒险

```json
{"op":"health"}
```

```json
{"op":"catalog"}
```

成功响应均有 `success: true`。`catalogue.difficulties` 是正式菜单可选难度，`isDefault` 标记 API 默认 Normal（当前显示名 Medium）。`weaponDetails`、`characterDetails`、`blueprintDetails` 可关联装备、技能、材料和初始武器。

```json
{"op":"create_run","seed":55,"profileId":"my-agent"}
```

可附加目录中的 `difficultyId`；省略时选 Normal。`profileId` 是隔离的 agent 档案，不读取玩家存档。首次使用该名字从零开始，后续局外进度保留；省略名字时为临时进度。**继续已有冒险用恢复命令**，再次 `create_run` 会替换该档案可恢复的冒险。

```json
{"op":"run_recover","profileId":"my-agent"}
```

恢复命令要求没有活动会话，返回新的 `state.sessionId`。档案每条已接受行动都会自动提交，因此恢复不要求预先手动保存。宿主重启后也能恢复；引擎、API 和已保存的内容版本必须匹配。

## 每次决策

读取 `state.phase`、队伍、背包、地图和 `state.legalActions`。从合法列表取完整行动，附加外层 `sessionId`、**最新** `revision` 和一个新的 `requestId`。先把完整请求保存到自己的记录，再发送；成功后使用响应中的新状态。

例如，合法列表给出了 `{"op":"run_move","x":7,"y":0,...}`：

```json
{"op":"run_move","x":7,"y":0,"sessionId":"FROM_RESPONSE","revision":0,"requestId":"unique-action-1"}
```

需要重新观察时调用，不消耗行动或推进随机数：

```json
{"op":"observe_run","sessionId":"FROM_RESPONSE"}
```

| 当前阶段 | 决策方法 |
|---|---|
| `map` | 从 `run_move` 选择相邻格；`run_interact` 使用当前位置；`run_equip` 装备已拥有的兼容武器 |
| `calamity` | 阅读 `calamityChoices`，执行 `run_choose_calamity` |
| `merchant` / `forge` | 查看商品/刷新价格、材料和 `craftingCost`，选择购买、刷新、锻造或离开 |
| `rest` | 查看回血/AP比例、真实招募费用及候选；选择恢复、招募或进入重铸 |
| `reforge` | 逐步选择模式、材料、属性、确认；首次成功后按正式访问锁定规则继续或结束 |
| `event` / `event_result` | 阅读可选项、费用和描述；选择后按 `run_continue_event` 继续，可能进入战斗或副本 |
| `treasure` / `treasure_relic` / `treasure_result` | 选择奖励、遗物袋选项或刷新，完成后继续 |
| `starting_relic` | 由已解锁能力产生的真实开局遗物袋；选择/刷新后进入地图 |
| `battle` | 从外层 `run_battle` 选择嵌套战斗动作；结果及遗物奖励完成后执行 `run_return` |
| `victory_pending_settlement` / `defeat_pending_settlement` | 执行 `run_settle`，读取正式计分和实际能力点 |
| `finished` | 可购买/开关能力，再 `run_next`；或关闭会话 |
| `faulted` | 导出诊断日志、关闭会话；持久档案可恢复最后成功提交的行动 |

以 `legalActions` 为准，不拼造尚未提供的交互。`configurationIssues` 会公开缺少敌群等正式内容问题；不可用格不会被自动计为清除。副本用同一组地图命令，检查 `dungeon.active`，出入口和外层返回位置都在观察中。可选嵌套对象有时会序列化成默认字段对象，应检查阶段及 `active` / `completed` 标记。

## 战斗

`state.battle` 提供双方意图、速度槽、技能、状态、能量和遗物。`dice` 是技能基础骰，`displayedDice` 为正式角色控制器当前显示的预览；它没有预先掷骰，也不保证全部条件触发后的结果。

冒险中的战斗动作必须包在 `run_battle`，外层维护全部版本。复制外层合法动作，例如：

```json
{
  "op":"run_battle",
  "battleAction":{"op":"set_skill","actorId":"ally_0","buttonIndex":0,"skillIndex":1},
  "sessionId":"FROM_RESPONSE","revision":20,"requestId":"unique-action-21"
}
```

之后通过同样方式选择 `battleAction.op: target`，最后选择 `commit`。每条选招/连线也递增外层版本。技能共用队伍能量；单个技能分别合法不代表所有行动槽合计可支付。`commit` 已执行回合结束和下回合开始结算，响应就是下一个决策点。

已有连线可以使用提供的 `battleAction.op: disconnect`，携带 `actorId` 和 `buttonIndex` 取消。它调用正式界面的断线方法，释放对应能量预留并恢复被接管的敌方连线；同样支持外层版本检查、相同请求重试、保存恢复与原生重放。重新连线后请读取实际 `targetId` / `contested`；同速（我方速度 ≥ 敌方）也可能接管拼点。

战斗单位新增只读字段：`pendingDelayedDeath` 表示已触发“不屈”、下回合开始仍会死亡，治疗不取消该标记；`stageName` 为当前阶段名，`passives` 为正式资产中的被动说明。这些说明不代替实际状态与结算。协议文档版本为 0.8.5，线上 `apiVersion` 仍为 1。

材料计划示例：`python tools/agent_adventure_client.py plan-equipment SESSION_ID 图纸ID`。它逐项比较公开配方与持有材料，并输出完整缺料、金币缺口；到铁匠后以实际 `canCraft` 和费用为准。空配方不视为免费制作。

准备评分 `preparation_assessment(state, encounter_type, catalogue, starting_loadout)` 的最后一项为可选的「角色ID → 实际起始武器ID」映射。请在新建会话的第一份状态中记录；目录初始武器可能为空，而正式开局会使用兜底武器。恢复/读档时不可把当前装备当作初始装备；没有可信记录时省略，返回的 `unknownWeaponBaselines` 会明确指出未知基线。评分是准备提醒，不预测胜利，不能绕过低血量提醒就声称准备充分。

自主整局试玩的持久偏好：每次从头新开使用不同种子，先查阅并更新 `PLAYTEST_SEEDS.json`，完整日志记录实际种子。原生回放、明确的定向复现与单元测试保留其记录种子，另行标记，不能计作新开一局。

`battle.outcome: victory` 可能仍在遗物奖励阶段，先完成合法的选择/刷新，再 `run_return`。普通战斗胜利返回地图；正式 Boss 胜利返回 `victory_pending_settlement`。回合上限、撤退及配置错误都不能当作通关。

## 结算、能力和下一局

```json
{"op":"run_settle","sessionId":"FROM_RESPONSE","revision":100,"requestId":"unique-settlement"}
```

以 `settlement.completed`、`settlement.data` 和 `meta.abilityPoints` 判断正式结果；计分使用实际 UTC 持续时长，恢复期间的离线时间也计入。地图放弃按正式规则不发奖，战斗放弃按未通关计分。`run_abandon` 仅在提供该动作的地图/战斗阶段可用，`run_close` 不会隐式放弃或结算。

在 `finished` 阶段，从 `meta.nodes` 阅读前置、费用和状态，复制合法的 `run_unlock_ability` / `run_set_ability`。花费真实钱包能力点后，下一局通过正式开局服务应用效果。`run_next` 可附加新 seed 和目录 difficultyId；省略难度沿用当前模式，省略 seed 使用默认 1。

## 保存、重放和重试

| 操作 | 请求字段 | 用途 |
|---|---|---|
| `run_save` | `sessionId`, `slotId` | 保存可继续游玩的命名检查点；不增加 revision |
| `run_load` | `slotId` | 无活动会话时重建检查点，返回新会话 |
| `run_recover` | `profileId` | 无活动会话时恢复持久档案最后提交 |
| `run_replay` | `sessionId` | 只读返回完整行动日志 |
| `run_export` | `sessionId`, `slotId` | 返回日志并保存到宿主的命名日志槽 |
| `run_verify` | `slotId` | 无活动会话时，按记录 UTC 在临时档案中重放所有观察，验证后不留下可玩会话 |
| `run_close` | `sessionId` | 释放会话、沙盒和档案锁，保留已提交持久进度 |

保存槽与日志槽是两个独立目录，`run_export` 不能替代 `run_save`。它们都在宿主项目 `Library/AgentAdventure` 下；删除 Library 会删除这些 agent 数据。名字仅支持 1–64 个 ASCII 字母、数字、下划线或连字符，拒绝 Windows 设备名，不接受文件路径。

结算、放弃和能力修改都会推进持久档案代次。之前的旧存档不能回滚这些局外变化，遇到 `Stale save` 用 `run_recover`；尚未提交局外变化的探索检查点可以分支，恢复后该分支成为新的自动恢复位置。

响应丢失时不要创建新 requestId 重发同一动作：

1. 宿主仍在运行且会话有效：重试原完整请求，参数全部相同。
2. 宿主已重启：`run_recover`，在原请求中只替换新的 `sessionId`，保留原 requestId/revision/参数，再重试。
3. 最近 32 次成功请求可返回缓存结果；更早的重复 ID 会拒绝执行，用 `observe_run` 查看当前状态。同一 ID 携带不同参数也会拒绝。

相同请求的缓存结果可能是历史状态；重试确认后先观察当前状态，再决定新动作。修改命令执行前验证版本和合法性；持久档案进度与完整日志以一次文件替换提交，提交完成后才发送成功响应。该机制已验证提交前后进程中断，不承诺任意硬件断电或人工篡改恢复。

HTTP 400 / `invalid_request` 需检查参数、合法列表和版本；500 / `execution_error` 应检查会话并按上述故障流程恢复。还有 64 KiB 请求体、32 个排队请求、10000 条修改行动等限制。保存或恢复使用完整重放，长局耗时会增长，不应把超时直接当作命令未执行。

## 现成客户端与已验证范围

Python 示例只用标准库，可替换策略；`demo` 是有限步试玩，不能保证任意地图自主通关：

```powershell
python tools/agent_adventure_client.py difficulties
python tools/agent_adventure_client.py demo --seed 55 --profile my-agent --max-moves 2 --save-slot latest
python tools/agent_adventure_client.py demo --recover-profile my-agent --max-moves 2 --save-slot latest
```

原生协议定义：[冒险 OpenAPI](adventure-openapi.json)、[单场及公共目录 OpenAPI](openapi.json)。接口不提供任意状态注入、奖励伪造或引擎外第二套规则。

已验证正式 Normal/Medium 种子 55 的 Boss 胜利、11784 分/11 点真实收益、发奖后崩溃重试、七个付费能力和下一局，完整 739 条行动重放一致。策略用了合法检查点的有限分支搜索；最后一回合另由示例策略自动复现胜利，这不等同于通用 demo 从开局自动稳定通关。其他路线覆盖商店、锻造、重铸、事件、宝藏、副本、失败和放弃，详见 [验收矩阵](ACCEPTANCE.md)。

当前仍有既有 EditMode/PlayMode 失败及部分正式配置缺陷，不能把接口闭环验收当作所有游戏内容无缺陷。无窗口接口验证规则和结果，画面、动画和手感仍需正式客户端测试。
