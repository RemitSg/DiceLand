# Agent 玩法接口（开发中）

正式游戏和 AI 调用同一个 `BattleButton` 回合流程、技能、伤害、状态、遗物和结算实现。接口适配层只负责创建测试会话、验证命令、驱动回合和描述状态，不另写玩法公式。

当前后端是 **Unity / 团结编辑器**，可以无窗口、无图形运行。它还不是脱离 Unity 的独立 C# 模拟器。先通过这个入口建立可复现测试，再逐模块把规则迁出 Unity，正式游戏和接口一起改为调用迁移后的模块。

## 启动

在当前项目的编辑器菜单中选择 `Tools > Agent Battle > Start HTTP Server`，默认地址为：

```text
POST http://127.0.0.1:8091/v1/command
Content-Type: application/json
```

也可以通过 Unity MCP 的 `execute_code` 执行 `return AgentBattleServer.Start(8091);`。停止时使用菜单中的 `Stop HTTP Server` 或执行 `AgentBattleServer.Stop();`。

真正无窗口运行时，在关闭占用此项目的编辑器后，从 PowerShell 启动：

```powershell
& 'C:\Program Files\Tuanjie 2022.3.62t6\Editor\Tuanjie.exe' `
  -batchmode -nographics -projectPath 'C:\DiceLand' `
  -executeMethod AgentBattleServer.RunBatch -agentBattlePort 8091 `
  -logFile 'C:\DiceLand\outputs\agent-battle\batch.log'
```

请先创建日志目录。不要加 `-quit`，服务需要持续处理请求。需要引擎的正常授权及项目依赖。同一项目不能同时被两个编辑器进程打开。无窗口启动命令与编辑器内 HTTP 服务共用代码；本阶段的端到端验证在现有编辑器进程中完成，没有为此关闭用户编辑器。

接口在 Edit Mode 运行，不进入 Play Mode，也不加载正式游戏场景。会话临时加载专用沙盒场景，关闭会话后丢弃；创建时恢复原活动场景，且不保存用户场景。由于现有规则仍有全局状态，本阶段只允许一个会话，已有战斗对象或遭遇时拒绝创建。脚本重新编译、退出编辑器或进入 Play Mode 会关闭服务与会话。

## 快速试玩

客户端只需要 Python 标准库。以下命令假设 `python` 指向可用的 Python 3：

```powershell
python tools/agent_battle_client.py health
python tools/agent_battle_client.py demo --output outputs/agent-battle/slime-replay.json
python tools/agent_battle_client.py replay outputs/agent-battle/slime-replay.json
python tools/agent_adventure_client.py demo --max-moves 2 --output outputs/agent-battle/adventure-trace.json

python tools/agent_battle_client.py demo --weapon '熔火铁剑' --enemy '浪灵' `
  --relic '女孩的火柴' --relic '液化燃料' --output outputs/agent-battle/burn-replay.json
```

演示客户端仅使用“当前默认技能，按合法目标连接，再提交回合”的简单策略，用来验证接口，不代表高水平打法。任何能发 HTTP 请求的 agent 都可以实现自己的策略，无需使用该客户端。OpenAPI 说明见同目录 `openapi.json`。

## 单场战斗命令协议

| op | 必填参数 | 行为 |
|---|---|---|
| `health` | 无 | 检查服务和当前后端范围 |
| `catalog` | 无 | 获取角色、武器、敌人、遗物的稳定资产 GUID、名称和说明 |
| `create` | `party`、`enemies` | 创建一场战斗并推进到首次决策点 |
| `observe` | `sessionId` | 获取当前局面和合法行动，不推进游戏 |
| `set_skill` | 会话参数、`actorId`、`buttonIndex`、`skillIndex` | 修改指定友方速度槽的技能 |
| `target` | 会话参数、`actorId`、`buttonIndex`、`targetId`、`targetButtonIndex` | 使用正式游戏的速度与拼点连线规则连接目标 |
| `commit` | 会话参数 | 执行整回合，再推进到下一次决策点或终局 |
| `choose_relic` | 会话参数、`choiceIndex` | 在奖励阶段选择合法遗物，完成后才允许返回冒险 |
| `refresh_rewards` | 会话参数 | 使用剩余奖励刷新次数，并排除当前展示的选项 |
| `replay` | `sessionId` | 导出开局配置、已接受的行动、内容指纹和引擎版本 |
| `close` | `sessionId` | 销毁会话对象、清理遭遇和沙盒场景 |

“会话参数”为 `sessionId`、最新的 `revision` 和唯一 `requestId`。推荐直接从 `legalActions` 选行动，再附加这三个字段。修改技能和连接目标也会递增 revision，因此提交回合时必须使用最新响应中的版本。

创建示例中的 GUID 从 catalog 返回值中选取：

```json
{
  "op": "create",
  "seed": 1234,
  "maxRounds": 100,
  "party": [{"character": "角色GUID", "weapon": "武器GUID"}],
  "enemies": ["敌人GUID"],
  "relics": ["遗物GUID"]
}
```

每场支持 1–4 名初始友方、1–8 名初始敌人和至多 32 个遗物。友方以角色基础生命、1 级创建。速度槽、敌人选招、能量、骰子、状态和遗物的战斗触发由现有游戏规则决定。

```json
{
  "op": "target",
  "sessionId": "响应中的会话ID",
  "revision": 0,
  "requestId": "agent-action-001",
  "actorId": "ally_0",
  "buttonIndex": 0,
  "targetId": "enemy_0",
  "targetButtonIndex": 0
}
```

`commit` 的响应已经包含下一回合的开始结算、敌方意图和新的合法行动，不要额外调用“开始下一回合”。可以不连接友方目标而提交，这与正式游戏放弃相应行动一致。

## 状态、错误与重放

- `state.phase` 为 `targeting`、`reward`、`terminal` 或 `faulted`。奖励阶段可读取 `rewards` 中的经验、金币、战利品、遗物选项及刷新次数。
- 正常终局结果包括 `victory`、`defeat`、`retreat` 和 `defeat_prevented`。达到回合上限为 `round_limit`，不当作胜利。
- `actors` 包含双方生命、状态、速度槽、当前技能和目标；友方 `skills` 列出可切换技能。技能说明复用正式游戏的效果文案生成器。`relics` 列出本场遗物。
- `events` 保留流程文本和结构化的伤害、状态变化、死亡通知。它不是完整的底层规则事件模型；新召唤单位在下一个观察点登记，其此前的信息可能只出现在流程文本中。
- 非法行动、过期 revision 和错误资产 ID 在执行前拒绝。最近 32 次成功动作可通过相同 requestId 和相同参数取回原响应，不会再次执行；更早的请求仍拒绝重复执行，但需要重新 observe 获取状态。
- 若同一 requestId 携带不同参数，直接拒绝。执行过程中发生异常时，会话进入 faulted，不能继续提交动作，应导出重放后关闭。
- 单次命令体至多 64 KiB，待处理请求至多 32 个；单场最多 1000 回合、10000 条修改命令和 20000 条事件。
- 固定内容、引擎版本、seed 和行动序列可复现结果。每次驱动后恢复编辑器原来的 Unity 随机数状态。客户端重放前比较内容指纹和引擎版本，版本变化时拒绝宣称重放一致。

## 当前边界

- 公开 HTTP 已提供正常冒险开局、地图移动、钥匙、行动点/灾厄、战斗返回、商店购买/刷新、锻造、营地回血/行动点恢复、招募、装备和两种重铸。事件、宝藏和特殊副本现已接入；隔离局外成长和最终结算已接入；完整保存/重放与正常胜利审计仍待完成。
- 遗物按已携带的战斗装备处理。具有 `OnRelicAcquired` 效果的遗物暂时明确拒绝，catalog 中标为 `acquisition_not_supported`，避免草莓等获得时效果被静默遗漏。地图或经济触发在单场战斗中不会发生。
- 不支持任意局面 snapshot/restore。当前可导出并从开局重放行动；这与恢复任意中间状态不同。
- 并非全部敌人、技能与遗物组合都已完成回归验证；它们会走正式实现，原有配置缺陷也会如实影响结果，不会被替换成简化效果。
- 此接口验证战斗逻辑，无法判断画面可读性、动画节奏和点击手感。

## B002：冒险战斗衔接

内部 `AgentAdventureBattleBridge` 从独立 `MapRunSnapshot` 和实际敌群构造正式遭遇，保留队伍生命、等级、经验、装备、遗物、灾厄与开战状态；不替换正式界面的全局地图快照。战斗结束后，通过正式 `MapRunRuntime.ApplyEncounterResult(snapshot, encounter)` 回写结果，且桥接层保证重复返回不重复发奖。

遗物选择和刷新已提取为正式结算面板与接口共同使用的服务。精英奖励需要选择后才可返回；草莓、礼包等获得效果在回写背包时执行一次。礼包抽取改用可捕获的随机种子。持久队员 ID 与结算显示名称分别保留，修正了原结算把 ID 替换成角色名的问题。

`create` 仍使用上文的单场配置，不会凭空生成奖励阶段；B003b 的 `create_run` 会从正常地图遭遇产生奖励。内部冒险战斗拒绝导出缺少携带状态的单场重放，完整冒险重放由后续批次负责。

B002 相关定向测试 46/46 通过，覆盖连续两场战斗、等级与金币、失败、Boss 钥匙门槛、奖励选择/刷新、重复请求、礼包随机和全局快照隔离；新单场 HTTP 对战重放一致。全量 EditMode 为 828 项、804 通过、24 失败：原有 23 项之外，另观察到结算按钮测试的 EventSystem.current 日志错误，单独运行仍复现；未将全量称为通过，详情见 `B002-validation.json`。测试中的小型敌群不作为完整冒险通关证据。原有 PlayMode 限制继续保留。

## B003：正常冒险 HTTP 入口

B003a（`e515b6dd`）提供内部 `AgentAdventureConfiguration` 与 `AgentAdventureMapRuntime`：读取保存的正式地图配置，调用共用开局构造器、地图生成器和移动服务，跳过格子显示、棋子动画和落地效果。配置读取使用当前引擎内部预览加载 API，版本不兼容时明确报错。正常难度使用实际 `NormalConfig.asset`（显示名 Medium），而不是场景的 Easy 回退配置；局外加成从隔离的空白进度开始，不读取用户成长存档。

B003b 在相同 HTTP 地址提供以下命令，协议见 `adventure-openapi.json`：

| op | 行为 |
|---|---|
| `create_run` | 使用 `seed` 和可选 `maxRounds` 创建正常冒险 |
| `observe_run` | 使用 `sessionId` 观察冒险，保留地图、队伍、物品、灾厄、内层战斗和合法动作 |
| `run_move` | 使用合法动作中的 `x/y` 移动；未探索/已探索花费、溢出周期和遗物触发沿用正式移动服务 |
| `run_interact` | 在当前位置重新交互，仍检查 Boss 钥匙门槛与敌群配置 |
| `run_battle` | 原样复制合法动作中的 `battleAction`，执行对应战斗行动或奖励选择 |
| `run_return` | 战斗和必选奖励完成后回写正式结算，并恢复地图或进入待结算终局 |
| `run_choose_calamity` | 使用合法 `choiceIndex` 应用灾厄；地图内置效果与正式界面共用实现 |
| `run_buy` | 使用合法 `choiceIndex` 购买商店商品，执行正式的价格、折扣、售罄与获得效果 |
| `run_refresh_shop` | 按当前刷新费用和次数档位刷新商品；特殊副本固定商品不可刷新 |
| `run_craft` | 使用合法 `assetId` 锻造已拥有的图纸，检查材料总量与灾厄附加费用 |
| `run_leave_location` | 关闭当前商店/锻造入口；商店离开时重置本次访问的购买折扣层数 |
| `run_rest_heal` | 在营地按正式灾厄和遗物规则恢复队伍生命，消耗这次营地并返回地图 |
| `run_rest_restore_ap` | 在营地恢复行动点，沿用正式的延后灾厄消除规则，消耗营地并返回地图 |
| `run_recruit` | 使用合法 `choiceIndex` 招募本次营地候选角色，扣除正式费用、检查招募上限并返回地图 |
| `run_equip` | 在地图阶段使用合法武器 `assetId`，调用正式的兼容队员选择与装备方法，不消耗行动点 |
| `run_open_reforge` | 从营地打开重铸；打开与选择本身不消耗材料或营地 |
| `run_reforge_loot` / `run_reforge_relic` | 切换重铸类型，保留各类型的选择，沿用正式种子输入 |
| `run_reforge_first` / `run_reforge_second` | 使用合法 `assetId` 分步选择材料；同堆叠需至少两件，两件材料需同阶 |
| `run_reforge_attribute` | 战利品重铸选择合法属性 `assetId` |
| `run_reforge_reset` | 清空当前类型的选择，不扣材料 |
| `run_reforge_confirm` | 执行共享的抽取与写入，扣除材料、触发遗物效果并增加成功次数 |
| `run_reforge_cancel` | 首次成功前关闭重铸，回到营地而不消耗本次访问 |
| `run_reforge_finish` | 首次成功后结束重铸，消耗营地并返回地图；达到次数上限时自动执行 |
| `run_choose_event` | 按合法 `choiceIndex` 支付事件费用并执行正式效果，进入结果步骤 |
| `run_continue_event` | 确认事件结果，进入后续事件/战斗/特殊副本或返回地图 |
| `run_choose_treasure` | 按合法 `choiceIndex` 选择金币、图纸或遗物袋；副本宝藏也支持直接遗物 |
| `run_choose_treasure_relic` | 从已打开的袋中选择一个遗物，调用正式获得效果并完成宝藏 |
| `run_refresh_treasure_relics` | 有剩余次数和候选时刷新遗物袋；排除已持有与当前选项，保留实际品质 |
| `run_continue_treasure` | 确认已获得的宝藏结果，再返回地图并处理延后的灾厄 |
| `run_close` | 任意阶段销毁临时冒险；目前不代表放弃结算、不发放局外奖励 |

所有修改动作都需要外层冒险的 `sessionId`、最新 `revision` 和唯一 `requestId`。原样复制一个 `state.legalActions` 项，再添加这三个字段即可；嵌套战斗的版本和 ID 由适配层处理。重复、非法和过期请求不重复扣费。一场冒险和一场单独战斗不能同时创建。

`state.party` 是地图携带状态；战斗中当前生命在 `state.battle.actors`，战斗结束并升级后的生命和经验在 `state.battle.rewards.party`，`run_return` 才将它写回地图队伍。灾厄、战斗和地图移动使用会话随机状态，执行后恢复编辑器的外部随机状态。

商店阶段的 `merchantOffers` 给出商品种类、说明、实际报价、售罄与可购买状态，`merchantRefreshCost` 给出当前刷新费用。`recipes` 列出已拥有图纸的目标武器、金币费用与合并后的材料需求/持有量。读取报价与合法行动不会修改冒险状态或推进随机数；执行购买和锻造后才扣除资源。正式锻造界面与接口共用材料合并和归属校验，避免重复材料行导致扣除不足仍获得武器。

营地阶段的 `rest` 给出回血/AP 恢复比例、招募费用与次数上限、最多两位本次访问的候选角色和可招募状态。回血、恢复 AP、招募或重铸择一；重铸首次成功后只能继续重铸或离开，不能再转为其他营地动作。恢复 AP 会同步实际行动点组件与延后的灾厄队列。候选抽取与正式营地共用规则，以正式地图逻辑名称参与确定性种子；不会因为 Agent 会话 ID 不同而重新抽取。装备沿用正式界面的自动兼容队员选择，不在战斗中切换。

重铸阶段的 `reforge` 给出类型、当前选择阶段、材料库存、属性、成功/剩余次数和可确认状态。战利品的 `possibleResults` 是同阶指定属性的候选列表，`resultPreview` 留空；遗物的 `resultPreview` 沿用正式界面的确定预览。材料、筛选、抽取种子与次数规则均由正式界面和接口共同调用 `RunRestService`；实际背包效果仍由共用快照写入处理。首次成功前可取消，基础上限两次并支持遗物加成；达到上限自动消耗营地。`lastReforgeResult` 保留最近一次获得物，即使自动返回地图也可读取。读取/选择不会推进随机数或获得物品，重复确认请求不会再次扣除或获得奖励。

事件阶段的 `mapEvent` 提供正文、选项说明、实际金币费用、丢弃预览和不可选原因。`run_choose_event` 与正式界面共用 `MapEventOptionService` 扣费、效果、自定义调用和完成统计；选择后进入 `event_result`，通过 `run_continue_event` 展示后续事件或开始实际遭遇。不会在观察时执行或预知加权结果。丢弃预览只在选项显示时生成，观察不推进随机数。事件战斗保留延后到胜利才给出的奖励，战斗奖励观察包含其战利品、遗物、图纸和武器，返回时才回写冒险；失败不发放。随机遗物改为使用作用域随机源生成抽取种子，去除了该事件路径的墙钟随机。自定义处理器当前资产引用为零；未来有依赖正式界面的处理器时会明确标记不可选，需提供适配。

宝藏阶段的 `treasure.options` 显示已确定的金币数额、图纸和遗物袋品质，不提前展示袋内选项。选择袋后进入 `treasure_relic`，显示候选遗物与刷新次数；必须先选择一个遗物才能进入 `treasure_result` 并继续。正式界面与接口共同调用 `TreasureRewardService.BuildTileChoices/ApplyTileChoice/ApplyTileRelicChoice`，发奖、次数加成和完成标记在同一实现中。刷新使用 `BattleRelicChoiceSelectionService`，本批同时补上宝藏界面的刷新按钮和遗物袋候选池；普通战斗的原有表格刷新规则保持一致。没有可刷新候选时不消耗次数。重复请求不会再次发奖，已完成宝藏不能重领。

特殊副本通过事件的 `run_continue_event` 进入，普通 `run_move` 驱动其中的线性棋盘；检查 **`dungeon.active`**，不能用对象是否存在来判断，因为当前传输可能把空对象序列化成默认字段。副本内移动成本为 0，入口保存外层棋盘/AP 与延后的灾厄；左侧出口撤退，右侧出口将正式副本分数写入 `progress`，然后恢复外层棋盘与携带队伍、背包。固定商店不能刷新，宝藏与战斗继续使用同一组既有服务，事件遭遇也携带副本上下文。重复出口请求不重复计分；非法位置和嵌套副本入口不能替换外层上下文。`previewTag`、`sourceDungeon` 和 `dungeonEntryIndex` 标明格子来自哪个实际配置。

**当前仍未完成整局闭环验收。**Boss 胜利或普通失败进入 `victory_pending_settlement` / `defeat_pending_settlement`，通过 `run_settle` 完成正式计分和隔离能力点写入。地图原内容的 Inner/Core 敌群池为空，`configurationIssues` 列出相应格子；缺敌群的格子不会被假结算为胜利。演示客户端会处理已支持的宝藏、事件、商店、锻造、营地和新装备，增加 `--reforge` 可优先尝试重铸；仍只记录行动 trace，尚不提供完整冒险保存或重放保证。

B003b 定向测试 60/60 通过。真实 HTTP 路线从正常地图进入战斗、胜利升级、携带 HP/金币返回，随后在已探索格移动耗尽行动点并完成一次实际灾厄选择，进入第 2 周期；未修改这条路线的资产或状态。详见 `B003b-validation.json`。原有全量回归失败记录继续保留。

B004a 相关测试 85/85、正式商店界面测试 2/2 通过。正常难度种子 46 的 HTTP 路线战斗取得 21 金币，进入实际商店花费 6 金币购买铁矿，剩余 15 金币并返回地图。锻造、刷新、概率折扣下的只读观察和重复请求采用定向测试验证；这些定向测试注入的地点/资源不作为整局正常通关证据。详见 `B004a-validation.json`。

B004b1 相关测试 68/68 通过，覆盖营地一次性消耗、灾厄回血比例、AP 跨周期与延后灾厄、招募费用/上限、新队友进入战斗、新武器的实际技能以及全局 UI 快照隔离。正常难度种子 32 的 HTTP 路线在战斗中通过两个合法的不攻击回合承受伤害，胜利后于实际营地从 16/19 恢复到 19/19；另一条路线将 AP 从 1 恢复到 6。未修改路线的资产或状态。招募和装备采用定向资源测试，尚不作为整局通关证据。详见 `B004b1-validation.json`。

B004b2 相关测试 90/90 通过，包含 13 项新增重铸测试及正式界面的候选筛选回归。正常难度种子 46 的 HTTP 路线通过战斗获得两份史莱姆粘液、购买一份铁矿，在实际营地完成两次重铸，最终获得一份秽蚀尖牙并自动消耗营地返回第 2 周期地图。此路线仅使用公开命令和原配置；遗物升阶、礼包获得效果、额外次数、非法材料和快照隔离采用定向测试。详见 `B004b2-validation.json`，原有全量/PlayMode 失败记录继续保留。

B005a 相关测试 100/100 通过，包含 12 项新增事件测试。正常难度种子 1234 通过 HTTP 实际进入“断桥”，分别验证三个选项：快速通过 HP 16→15、AP 4→6；绕路 AP 4→3；修桥 AP 4→2 并获得 31 金币，再次从相同种子执行结果一致。均确认结果后返回地图且事件消耗。首次事件专项 40 项中 39 项通过，另有既有 `MapEventNestedFlowTests.SaveAssetCatalog_IncludesNestedFollowUpEvents` 失败，已在原基线与 B002 记录；不把它报告为通过。详见 `B005a-validation.json`。

B005b 在隔离的 Tuanjie `-batchmode -nographics` 工程中运行，112/112 相关 EditMode 测试通过，包含 14 项新增测试。正式宝藏界面同时修复了 EditMode 重建卡片时延迟销毁导致的两项既有失败。正常种子 32 的三条 HTTP 路线经实际营地恢复 AP 后到达 `(8,3)` 宝藏：分别获得 39 金币、图纸“蚀毒之卷”、从袋中选择遗物“焦火绒”，均消费宝藏并返回地图。宿主日志确认 `NullGfxDevice`，未打开游戏窗口。原编辑器域重载停滞时采用隔离副本验证，核心源码与测试文件哈希一致；未对用户原场景执行保存或重启。详见 `B005b-validation.json`。这不是完整冒险通关证据。

B005c 已在隔离的无图形宿主验证特殊副本全程：正常种子 127 通过两个真实宝藏取得 77 金币，在营地花 50 招募 Jenny，再进入“反复出现的门”，两场战斗均胜利、领取遗物并从右出口恢复 9×9 主地图；AP 1→1，Chara/Jenny 剩余 HP 5/3，副本计分 200。同种子左撤恢复原入口且不计分。此路线包含三个共用内容连接修复：026 入口缺少副本、033 已有副本选项类型误为 None、026 末尾罕见遗物袋缺少遗物表（接正式宝藏既有池，保留品质和数量）。接口没有注入资源或胜负。事件内置金币效果现在也在正式选项服务预检，零金币时阻止先扣生命再因费用失败；随机结果仍在选择后抽取。

最终全量 EditMode 921 项、900 通过、21 失败，失败名称与同环境提交前 899 项/878 通过/21 失败完全一致；新增 22 项全部通过，从最终全量中选出的 142 项相关用例全部通过。幸运迷宫额外探测到“休息室”后遇到原资产未配置遗物奖励，未通过；王储副本与 Inner/Core 的缺失敌群继续报告。详见 `B005c-validation.json`，原有 PlayMode 限制保留，整局结算/存档/重放仍待后续批次。

B006a 为同一个 `RunSettlementFlowService` 增加显式 `RunSettlementContext`。正式场景沿用默认上下文；接口将使用独立上下文并必须传入读写钱包回调，缺少回调会在任何副作用前拒绝，不能落回玩家钱包。上下文已有结果时只返回原结果，不重复发奖或清理。18/18 相关测试与新增 7 项全部通过；全量 928 项中 907 通过，21 项失败名称与 B005c 一致。此批只提供隔离原语，公开终局、局外成长和下一局仍待 B006b；详见 `B006a-validation.json`。

## B006b1：终局、能力树和下一局

| 命令 | 行为 |
|---|---|
| `run_settle` | 仅在实际胜利/失败等待结算时可用；调用共用计分和一次性结算服务，进入 `finished` |
| `run_abandon` | 仅地图/战斗阶段可用；地图放弃不发能力点，战斗放弃调用正式未通关结算，不领取未确认的战斗奖励 |
| `run_unlock_ability` | 在 `finished` 使用 `meta.nodes` 的节点 id；费用/前置条件由正式能力服务执行 |
| `run_set_ability` | 在 `finished` 开关已解锁的可开关节点；复制合法动作中的节点 id 和 choiceIndex |
| `run_next` | 从 `finished` 开始下一局；附加 `seed` 指定新种子（省略默认 1），保留初始 maxRounds 和隔离进度，重新应用正式开局加成 |
| `run_choose_starting_relic` / `run_refresh_starting_relics` | 处理能力加成产生的开局遗物袋；必须完成选择才能移动，获得效果走正式服务 |

`settlement.completed` 表示本局已结束；`policy` 分别为 `shared_score` 或 `map_abandon_no_rewards`。空对象的默认字段不表示有结算奖励。观察不改变时间或发奖；计分使用真实创建/结算 UTC，沿用正式短局倍率。`runIndex` 随下一局递增，外层 revision 和 requestId 缓存跨局保留，重试旧请求不会再次放弃/开局/发奖。`run_close` 只清理会话，不代替正式结算。

`create_run` 省略 `profileId` 时使用独立临时进度，关闭后清理；显式传入 1–64 位字母、数字、下划线或连字符可在同一项目/宿主重开档案，禁止路径与 Windows 设备名。文件位于 `Library/AgentAdventure/Profiles/<id>`，删除 Library 也会删除这些测试档案。默认临时档案位于该目录体系的 Sessions 下；两者均不使用玩家钱包/能力树存档。能力修改只在终局后允许，并于下一局生效。Python 演示客户端支持 `--profile`、开局选择和终局结算；完整保存/重放还未提供。

无图形全量 942 项、921 通过、21 失败，失败名称与 B006a 的 928 项/907 通过/21 失败一致。从最终全量中选出的 116 项相关测试全部通过。包含 14 项新增终局测试，通过定向状态验证胜利分支、能力点发放、付费解锁影响下一局、隔离与幂等；这些定向资源和合成敌群不是正常通关证据。公开 HTTP 正常路线已验证真实败北、两类放弃、免费节点解锁、下一局和显式档案重开。种子 46 的探索准备失败，种子 127 以 Chara/Jenny/Man 三人满血、两把钥匙进入正式雷寰霸主战，但第 2 回合战败。B006b2 继续验证正常胜利→赚点→付费升级→下一局，当前不宣称该门槛已通过。详见 `B006b1-validation.json`。

B006b2 进行中：演示客户端改为共用 `tools/agent_adventure_policy.py` 的可见状态策略。若当前技能不再出现在合法选择中，会切换到可支付技能；目标选择优先反制已指向本行动格的攻击，随后考虑可拦截的未覆盖威胁，且降低自伤技能偏好。6 项标准库测试来自真实正常 Boss 观察帧；独立 HTTP 复查确认所有决策都来自合法列表、能指定目标的行动格没有被漏掉。原三人 Boss 路线仍第 3 回合败北；另一条真实路线虽赢精英、购买图纸材料并锻造/装备掠风短匕，仍因准备期间的灾厄与构筑不足而失败，得到 653 分/0 能力点。只修改客户端策略，没有调整游戏难度或结算。正常胜利→赚点→付费升级→下一局依然未通过，记录见 `B006b2-progress.json`。

B006b2 的装备可观察性已补齐：公开 `catalog` 保留原有列表，新增 `weaponDetails`、`characterDetails` 和 `blueprintDetails`。可以把商店图纸的 id 连到目标武器、材料、武器类型和技能；招募候选的初始武器也可查完整技能与基础行动格数量。技能说明直接调用正式界面的格式化方法，不维护第二份技能文案。`craftingCost` 在尚无图纸、尚未进入锻造格时也能读取当前灾厄修正后的费用。

战斗中的 `skills/selectedSkill.displayedDice` 读取正式角色控制器的界面预览范围，原有 `dice` 保留资产基础范围。目录是静态定义，`displayedDice` 为空；状态、临时技能、能量、每个行动格的当前选择必须以本局观察和 `legalActions` 为准。预览没有预先掷骰，也不保证目标条件触发后的最终结果。目录查询使用独立预览场景中的非激活组件，不创建 UI，也不改当前场景、输入锁或随机数。正常获胜验收仍未完成；装备信息补齐只是该批的一部分。

## 后续迁移原则

1. 用重放和正式游戏回归测试固定可观察行为。
2. 按完整领域模块提取规则，例如骰子、伤害、状态或回合调度，不为每个小条件新增服务。
3. 正式游戏与 AgentBattleSession 同时改为调用提取后的模块，删除旧位置的规则实现。
4. 模块移除 Unity 对象、场景搜索和全局随机数依赖后，再增加独立 .NET 宿主及真正的并行会话。

不复制战斗控制器，也不另外维护一份遗物或技能效果实现。

## 本次验证（2026-10-27）

| 验证 | 结果 |
|---|---|
| 新增接口 EditMode 测试 | 14/14 通过，包括真实伤害、非法行动、幂等、版本检查、随机数隔离、燃烧、技能说明和双方召唤 |
| 修改前全量 EditMode 基线 | 803 项，780 通过，23 失败 |
| 当前全量 EditMode | 817 项，794 通过，23 失败；失败名称与基线完全一致 |
| HTTP 完整对战和从开局重放 | 木剑、燃烧、中毒、电流、召唤共 5 组，全部重放一致 |
| 正式游戏 PlayMode 回归 | 12 项，3 通过、9 失败，主要为演出超时及教程断言，不报告为通过 |
| 原代码 PlayMode 对照 | 也观察到同样的演出超时；运行在 7/12 后取消，完整基线未完成，不能据此确认所有 PlayMode 失败均为历史问题 |

详细记录见 `outputs/agent-battle/verification-summary.json` 与 `http-verification.json`。示例策略的胜负仅验证接口流程，不用于评价流派强度。临时对照期间使用的源文件已逐字节恢复，测试临时修改的时间倍率与进入 Play Mode 设置也已恢复；当前用户场景的未保存变更保留原样。

装备检查示例：`python -B tools/agent_adventure_client.py --url http://127.0.0.1:8091/v1/command inspect-equipment f3b07049a74cf51409a7e4127e06d57f`。返回该图纸的材料和目标武器完整技能，支持资产 id 或唯一精确名称。客户端管道输出固定 UTF-8。最终全量 948 项中 927 通过，21 项失败名称与原基线一致；6 项新增测试、47 项相关测试及 6 项 Python 回归通过。HTTP 正常路线的只读检查和实际费用观察记录见 `B006b2-equipment-validation.json`，并非正常通关证据。
