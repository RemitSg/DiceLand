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

每次新局重新读取目录。资产 `id` 是接口提供的 GUID；局内队员 `party.id`、战斗 `actors.id`、槽 `index`、状态 `statuses.id` 各有自己的作用域，不能互相替换。当前双语显示名、库存名与描述可能来自不同展示路径，按 ID 关联，名称只用于阅读。不要复用第五局的物价、Boss 血量、遗物数值或固定角色顺序。

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
| `map` | 从实际 `run_move` 列表选择目的地，获得斜向移动能力后可包含斜向格；`run_interact` 使用当前位置；`run_equip` 装备已拥有的兼容武器 |
| `calamity` | 阅读 `calamityChoices`，执行 `run_choose_calamity` |
| `merchant` / `forge` | 查看商品/刷新价格、材料和 `craftingCost`，选择购买、刷新、锻造或离开 |
| `rest` | 查看回血/AP比例、真实招募费用及候选；可先 `run_equip`，再选择恢复、招募或进入重铸 |
| `reforge` | 逐步选择模式、材料、属性、确认；首次成功后按正式访问锁定规则继续或结束 |
| `event` / `event_result` | 阅读可选项、费用和描述；可用 `run_equip` 检查换装，选择后按 `run_continue_event` 继续，可能进入战斗或副本 |
| `treasure` / `treasure_relic` / `treasure_result` | 选择奖励、遗物袋选项或刷新，完成后继续 |
| `starting_relic` | 由已解锁能力产生的真实开局遗物袋；选择/刷新后进入地图 |
| `battle` | 从外层 `run_battle` 选择嵌套战斗动作；结果及遗物奖励完成后执行 `run_return` |
| `victory_pending_settlement` / `defeat_pending_settlement` | 执行 `run_settle`，读取正式计分和实际能力点 |
| `finished` | 可购买/开关能力，再 `run_next`；或关闭会话 |
| `faulted` | 导出诊断日志、关闭会话；持久档案可恢复最后成功提交的行动 |

以 `legalActions` 为准，不拼造尚未提供的交互。`configurationIssues` 会公开缺少敌群等正式内容问题；不可用格不会被自动计为清除。副本用同一组地图命令，检查 `dungeon.active`，出入口和外层返回位置都在观察中。可选嵌套对象有时会序列化成默认字段对象，应检查阶段及 `active` / `completed` 标记。

`run_equip` 使用正式的自动兼容队员选择；不需要也不能用 `choiceIndex` 指定队员。地图、事件选项/结果页及营地大厅可换装；战斗和已打开的重铸模态不提供此操作。换装不消费营地、不选择事件选项，也不代表角色已获得经验或治疗。

## 当前自主游玩流程（2026-11-09 更新）

1. **固定本局环境。** 记录引擎、提交、实际未提交内容、`contentHash`、语言环境、种子与档案初始进度。开发仍在进行时用隔离宿主固定快照，启动完成后才创建一次新局。旧局的存档和回放不能直接拿到不同内容版本中继续或验证；原生重放使用原种子，不算新局。
2. **观察后单步判断。** 模型读取最新状态、完整合法动作和当前阶段的资料，选择一条明确动作，写简要依据后执行。每次行动重新读取响应；阶段、随机候选、地图或敌人意图变化后停止旧行动序列，重新判断。正常试玩不读取隐藏抽取结果、不注入资源，不用读档重掷覆盖失败。
3. **按目标规划成长。** 用当前目录比较队员兼容武器、全部技能/能耗、完整配方和遗物联动；建立若干可替换的装备目标，再根据已知商店、真实掉落和重铸候选补缺口。每次购买/制作后重新扣除已消费材料，分别预留招募、制作与恢复预算。先考虑部分金币对后续招募的价值，避免只在一次奖励立刻凑齐门槛时才重视金币。
4. **按有效作用比较收益。** 商店以当前 `price`、`canPurchase` 和合法动作作准，不能仅用“价格≤金币”否定遗物带来的特殊购买规则。重铸先阅读同阶限制、`possibleResults` / `resultPreview` 及次数；不按候选首项自动确认。特殊副本限定、满血、蓄能、状态转换、每角色/每目标次数上限等条件必须纳入判断。界面的获取展示、图鉴和教程提示不代表还要再调用一次发奖。
5. **进入战斗前核对队伍。** 统计存活者与实际等级、生命、装备、灾厄和恢复路线，不把钥匙齐全当作准备完成。保留营地或其他恢复机会，也检查临近升级的经验收益；不要把 `party` 的总人数当作存活人数，不预设零 HP 队员会在胜利后自动复活。营地、遗物或升级是否改变其状态，以当次实际响应为准。
6. **逐轮核对而非固定连招。** 读取全部存活行动槽、继承/额外槽、状态、延迟死亡、当前阶段和敌方每个 `selectedSkill.description`。逐骰检查攻防、当前预览、使用/命中/拼点触发、全队伤害、延迟效果、状态替换/免疫；比较共享能量预算和施法后剩余蓄能。每次改技能/连线后检查实际目标与 `contested`，最后才提交该轮。
7. **按真实结果结算。** 战斗期间生命取 `battle.actors`，外层 `party` 要等 `run_return` 才回写。`commit` 响应可能已含下一轮开始效果，日志保留事件自己的回合号。奖励、返回、冒险待结算、`run_settle` 各按合法动作推进；只有正式 `runCleared` / 完成结算才记为通关。完成后导出原生日志、关闭会话并验证重放，保留失败与疑点，不自动开下一局。

### 模型决策的执行工具

正式的单步执行入口为 `tools/agent_play_client.py`。先用原始命令创建/恢复会话，再绑定返回的真实 session ID；创建新局前按前述偏好登记未使用种子。以下工具不挑候选、不评分、不计算路线、不提交整轮默认动作，每次 `act` 只执行一条模型明确给出的选择：

```powershell
python tools/agent_play_client.py --log-dir outputs/agent-battle/MY-RUN attach SESSION_ID
python tools/agent_play_client.py --log-dir outputs/agent-battle/MY-RUN observe
'{"op":"run_move","x":1,"y":1}' | python tools/agent_play_client.py --log-dir outputs/agent-battle/MY-RUN act - --reason "依据当前地图和预算选择这条合法移动"
'{"op":"run_battle","battleAction":{"op":"target","actorId":"ally_0","buttonIndex":0,"targetId":"enemy_0","targetButtonIndex":1}}' | python tools/agent_play_client.py --log-dir outputs/agent-battle/MY-RUN act - --reason "当前技能与来袭目标适合反制这个行动槽"
python tools/agent_play_client.py --log-dir outputs/agent-battle/MY-RUN export MY-REPLAY-SLOT
```

示例的坐标和 ID 只是格式，必须替换为当前观察提供的值。选择器必须恰好匹配一个合法动作，不能省略字段后随便取第一个。工具会在执行前重新观察，复制完整合法动作并附加最新版本，先把完整请求/理由记到 `observations.jsonl` 和待处理请求文件，再发送。`session.json` 保存当前状态及未确认请求，`catalogue.json` 保存绑定时目录；可据此整理逐步、逐轮报告，原始日志不只保留摘要。

若响应超时，在同一宿主仍有效时用 `retry` 重发磁盘上的同一个请求；工具不自动重试，也不生成新 ID 代替未确认的请求。成功后再次观察，避免把缓存的历史响应当成现在的状态。若服务器明确拒绝参数、缓存过期或宿主重启，按下文故障流程核对状态，保留原目录的未确认记录并使用原始命令处理；这个工具不自动恢复、清除失败或重新开局。`run_next` 在此入口被阻止，避免遗漏新种子而默认重复 seed 1；要另行登记并显式创建下一局。

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

战斗单位新增只读字段：`pendingDelayedDeath` 表示已触发“不屈”、下回合开始仍会死亡，治疗不取消该标记；`stageName` 为当前阶段名，`passives` 为正式资产中的被动说明。这些说明不代替实际状态与结算。协议文档版本为 0.8.6，线上 `apiVersion` 仍为 1。

材料计划示例：`python tools/agent_adventure_client.py plan-equipment SESSION_ID 图纸ID`。它逐项比较公开配方与持有材料，并输出完整缺料、金币缺口；到铁匠后以实际 `canCraft` 和费用为准。空配方不视为免费制作。

目录和材料计划只辅助计算，不替模型选装备。非铁匠阶段即使有 `recipes`，其中 `canCraft=false` 也可能只表示当前不能执行锻造；计划工具此时返回 `canCraft: null`，只报告材料/金币核对，抵达铁匠再判定。背包图纸名称可能与目录不同，使用图纸 GUID 查配方。

准备评分 `preparation_assessment(state, encounter_type, catalogue, starting_loadout)` 的最后一项为可选的「角色ID → 实际起始武器ID」映射。请在新建会话的第一份状态中记录；目录初始武器可能为空，而正式开局会使用兜底武器。恢复/读档时不可把当前装备当作初始装备；没有可信记录时省略，返回的 `unknownWeaponBaselines` 会明确指出未知基线。评分是准备提醒，不预测胜利，不能绕过低血量提醒就声称准备充分。

自主整局试玩的持久偏好：每次从头新开使用不同种子，先查阅并更新 `PLAYTEST_SEEDS.json`，完整日志记录实际种子。原生回放、明确的定向复现与单元测试保留其记录种子，另行标记，不能计作新开一局。

自主整局试玩默认由模型逐个决策点选择行动；脚本仅传输、匹配合法动作、辅助计算和记录。不要在这类试玩中调用 `choose_battle_action` 等评分选择器、自动选路/事件选择或自动回合循环。每条请求前记录模型的简要依据，保留真实失误，事后批注另外记录。明确要求的脚本回归可继续用策略函数，但必须如实区分，不能把脚本评分理由描述成模型逐步判断。第五局按此方式从零通关，详见 `Easy-20261029-run5-validation.json`；单局成功不代表任意种子均能稳定通关。

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

Python 示例只用标准库。`agent_play_client.py` 为模型逐步决策入口；旧 `agent_adventure_client.py demo` 是依赖文本和固定评分的脚本回归样例，输出标明 `decisionSource: legacy-script-heuristic`，不应用来代表当前默认自主试玩，也不能保证任意地图通关或英文环境表现：

```powershell
python tools/agent_adventure_client.py difficulties
python tools/agent_adventure_client.py demo --seed 55 --profile my-agent --max-moves 2 --save-slot latest
python tools/agent_adventure_client.py demo --recover-profile my-agent --max-moves 2 --save-slot latest
```

原生协议定义：[冒险 OpenAPI](adventure-openapi.json)、[单场及公共目录 OpenAPI](openapi.json)。接口不提供任意状态注入、奖励伪造或引擎外第二套规则。

2026-11-09 流程更新验证：4 个新增用例在旧适配层全部复现失败，修复后相关 EditMode 124/124、Python 66/66 通过；两份 OpenAPI 的 54 DTO / 446 字段及操作枚举与源码一致。详见 [本次验证记录](Workflow-20261109-validation.json)。本次没有新开整局，没有重跑全量 EditMode、PlayMode 或界面测试。

已验证正式 Normal/Medium 种子 55 的 Boss 胜利、11784 分/11 点真实收益、发奖后崩溃重试、七个付费能力和下一局，完整 739 条行动重放一致。策略用了合法检查点的有限分支搜索；最后一回合另由示例策略自动复现胜利，这不等同于通用 demo 从开局自动稳定通关。其他路线覆盖商店、锻造、重铸、事件、宝藏、副本、失败和放弃，详见 [验收矩阵](ACCEPTANCE.md)。

当前仍有既有 EditMode/PlayMode 失败及部分正式配置缺陷，不能把接口闭环验收当作所有游戏内容无缺陷。无窗口接口验证规则和结果，画面、动画和手感仍需正式客户端测试。

当前接口还没有统一的结构化效果条件/次数/状态转换目录，技能和遗物的细节仍须结合当次完整说明与实际观察阅读；旧 Python 的中文关键词评分没有因此变成跨语言规则。`AgentAdventureReplayService.ContentHash` 尚未把 `Assets/Localization` 纳入根目录，回放也未记录语言切换；应固定并单独记录本局语言环境，在原内容快照中核验，不宣称跨语言重放已通过。新教程、背包动画、获取提示与图鉴属于正式界面覆盖，仍需另行 UI 验证。
