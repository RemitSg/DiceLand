# 项目进度追踪

最后更新：2026-04-25

## 使用规则

这份文件用于持续记录项目当前进度、重要系统状态、亟待改造的问题和后续路线。之后每完成一次任务，都需要同步更新本文件，至少更新以下内容之一：

- `整体进度概览`：当一个系统进入新阶段或完成度明显变化时更新。
- `模块进度`：记录对应模块新增了什么、还缺什么、风险在哪里。
- `近期完成记录`：追加本次任务的简短记录。
- `下一阶段优先级`：当任务顺序发生变化时调整。

完成度是开发判断值，不等于最终内容量，只表示系统可用性、稳定性、可扩展性和内容填充程度的综合进度。

## 整体进度概览

当前项目已经具备一条可迭代的 Roguelite 主循环雏形：主菜单进入游戏，地图生成与移动，进入战斗，结算奖励，返回地图，休息、商店、重铸、遗物、掉落物、教程入口等系统都已开始接入。整体状态更接近“功能骨架基本成型，核心玩法正在快速扩展，稳定性和内容生产工具仍需补强”。

当前整体完成度：约 `56%`

主要瓶颈：

- 战斗核心逻辑集中在少数大脚本中，后续扩展技能、状态、遗物时风险会越来越高。
- 遗物、状态、敌人、商店、重铸等系统已有大量接口，但需要更多自动化测试和统一事件钩子。
- 主菜单、地图、战斗、休息场景的 UI 已能工作，但仍处于“可用优先”，需要统一视觉规范和交互细节。
- 内容生产已经开始加速，敌人、遗物、掉落物、教程内容需要稳定的数据表和审核流程。

## 模块进度

### 1. 主菜单系统

完成度：`70%`

已完成：

- 主菜单六个入口已规划：`NewGame`、`Continue`、`Contributors`、`Settings`、`Exit`、`DemoTest`。
- `NewGame` 已接入难度选择，并连接地图生成逻辑。
- `Settings` 已包含音乐音效滑块、全屏、震动、画质、分辨率、保存按钮等功能。
- `Contributors` 已有电影报幕式滚动展示接口。
- `DemoTest` 已作为战斗测试入口接入，可配置敌人组、友方角色和武器。
- 教程入口和教程框架已经搭好，战斗教程内容已开始填充。

亟待改造：

- Settings、Contributors、Tutorial 等子面板需要统一字体、尺寸、锚点和显示层级规范。
- Continue 需要更完整的存档存在性检查、异常存档提示和版本兼容策略。
- 主菜单轮盘与右侧信息/设置窗口之间需要形成稳定布局规则，避免分辨率变化造成缩放异常。

下一步建议：

- 完成教程菜单内容填充和教程章节配置。
- 建立主菜单 UI 配置 SO，集中管理字体、颜色、尺寸、偏移和文案。

### 2. 地图与跑团主循环

完成度：`75%`

已完成：

- `RunGameManager` 作为地图主流程入口，承担 run state、场景切换、地图事件、商店、奖励等核心逻辑。
- `BoardManager` 支持地图生成、格子状态、访问状态和地图 UI 表现。
- 难度配置 `DifficultyConfig_SO` 已承担地图尺寸、行动点、钥匙、商店刷新、遗物价格等配置。
- 行动点、钥匙、Boss 解锁、灾厄、商店、休息、重铸、队伍面板等核心地图系统已接入。
- 地图队伍列表已显示角色名称、Icon、血量条、经验条，并支持折叠展开动画。
- 已实现前往已探索格子不消耗行动点的遗物逻辑。

亟待改造：

- `RunGameManager` 体量过大，地图流程、商店、奖励、休息、UI 刷新等职责需要逐步拆分。
- 地图存档与 Continue 需要统一数据模型，避免仅依赖临时运行时状态。
- 地图格子生成需要更强的验证：钥匙可达性、Boss 可达性、事件/商店/休息分布合理性。
- 商店、重铸、事件、灾厄的运行时数据需要更明确的持久化边界。

下一步建议：

- 把商店刷新逻辑从 `RunGameManager` 中拆成独立 service/runtime helper。
- 增加地图生成 EditMode 测试，验证不同难度下的路径和关键格可达。

### 3. 战斗核心系统

完成度：`65%`

已完成：

- 战斗场景可从地图或 DemoTest 启动，并通过 `BattleEncounterRuntime` 传递敌人组、队伍和奖励上下文。
- 已实现友方/敌方生成、速度按钮、技能选择、划线、能量消耗、拼点、单方面攻击、结算返回地图。
- 技能支持攻击/防御骰、多骰子、使用时、命中时、拼点胜利时、单方面攻击时、仅拼点威力修正等词条。
- 技能特效接口已支持 local/global 偏移选项，避免老资源被破坏。
- 玩家技能可配置需要状态层数才可施放，施放后默认消耗对应状态；敌方使用时无视该限制。
- 防御型技能的“使用时”词条已修正为即使没有拼点也会生效。
- 已实现若干战斗遗物效果，包括速度比较、回合结束治疗、拼点威力修正等。

亟待改造：

- `BattleButton` 是当前最大风险点，承担过多战斗流程、技能解析、目标判定和结算职责。
- 拼点与单方面攻击的威力还原、状态修正、遗物修正需要统一成清晰的计算管线。
- 技能触发条件和技能词条需要抽象成可测试的 resolver，避免继续在主流程中叠分支。
- 战斗表现系统和战斗逻辑需要继续解耦，尤其是移动坐标、攻击坐标、特效锚点、local/global 偏移。

下一步建议：

- 将战斗结算拆成 `BattleActionResolver`、`DicePowerResolver`、`BattleTriggerDispatcher` 三类职责。
- 为拼点威力、单方面攻击、状态消耗、遗物减伤建立 EditMode 测试。

### 4. 敌人与关卡内容

完成度：`62%`

已完成：

- `EnemyData_SO` 已支持基础敌人数据、技能、条件技能、阶段状态、战斗开始状态、每回合状态等接口。
- 敌人组 `EnemyGroup_SO` 和敌人池 `EnemyRoster_SO` 已作为战斗生成基础。
- 已设计并记录多个敌人方向，包括史莱姆系、毒系、速度干扰、防御反制、阶段精英等。
- 已为“绯脉巢母”实现“在场时角色流血层数不因被攻击减少”的机制方向。
- 根目录 `TODO.md` 已追加新敌人设计草稿，用于后续审核。
- 已将审核通过的 `019` 到 `026` 新敌人正式落入敌人 SO 与技能 SO，包括冻烬蛾、铁线傀儡、溺潮拾骨者、腐花、裂肤信徒、逆风游侠、灰祭司、断角守门人。
- 已补充敌人落地所需的基础接口，包括最低速目标优先、按骰子拆分技能类型、阶段技能限制、濒死保留 1 点生命等。
- `026 断角守门人` 的“断角”被动已接入敌人技能预览与技能详情显示，攻击技能威力会在详细面板与简要面板中以“旧值划线 + 新值”形式表现。
- 当前这批新敌人的贴图暂时统一复用 `001 史莱姆` 的 sprite，后续只需逐个替换正式资源，不影响现有数据与逻辑联调。

亟待改造：

- 敌人被动与特殊规则需要统一数据化，否则每个精英怪都会推动战斗代码继续膨胀。
- 敌人技能 AI 的优先级、条件选择、技能位置强制规则需要形成清晰配置。
- 敌人内容需要按地图距离、危险等级、事件、精英、Boss 分类建立 encounter pool。
- 当前 `019` 到 `026` 虽已可接入战斗，但仍缺少正式编队分布、测试敌人组、地图投放规则与独立美术资源。

下一步建议：

- 设计 `EnemyPassiveHandler` 或 `EnemySpecialRule` 体系，减少硬编码特殊怪。
- 为 `019` 到 `026` 补测试敌人组、地图投放与美术替换，然后继续实现后续审核通过的新敌人。

### 5. 遗物系统

完成度：`60%`

已完成：

- 已建立 `Relic_SO`、`RelicTable_SO`、`RelicSystem`、内建效果枚举和效果实现分离文件。
- 遗物表支持按品质权重随机抽取，并默认排除玩家已拥有遗物。
- 当目标品质无可抽取遗物时，已实现先向低品质查找，再向高品质查找，全部耗尽则不发放。
- 商店遗物刷新已改为使用遗物表，并按难度配置中的稀有度价格区间定价。
- 已实现多件新遗物，包括晨露琥珀、驰隙时序环、流云翎、锻灵石、繁生核、矮人腕环、“归星”、不融冰等。
- 内建遗物效果新增时遵循“末尾追加”原则，避免打乱已有资产配置。

亟待改造：

- 遗物触发点越来越多，需要统一事件总线或触发上下文，避免各系统各自调用。
- 遗物效果需要按触发时机分类测试，尤其是战斗开始、回合开始、回合结束、受伤前、伤害后、奖励发放、重铸、地图移动。
- 遗物描述、实际效果、触发日志之间需要建立一致性，便于调试和平衡。

下一步建议：

- 建立 `RelicTriggerContext` 的统一字段规范。
- 为高风险遗物补 EditMode 测试，例如抽取不重复、品质降级/升级查找、锻造保留、额外掉落。

### 6. 掉落物、武器、图纸与重铸

完成度：`60%`

已完成：

- 已有 `LootItem_SO`、`LootItemTable_SO`、`LootTable_SO`、`CraftingBlueprint_SO` 等数据结构。
- 敌人掉落遗物已改为使用遗物表，并可配置随机遗物发放数量。
- 休息场景已新增重铸功能：两个同阶级 LootItem 转换为指定属性随机 LootItem；两个同阶级遗物转换为随机高阶遗物。
- 重铸次数默认每个休息场景 2 次，部分遗物可改变可锻造数量或返还材料。
- 重铸交互已支持鼠标悬停描边、确认框、第一次重铸后显示产物名称等需求。

亟待改造：

- LootItem、Relic、Weapon、Blueprint 的“不重复发放”规则需要统一成可复用过滤器。
- 重铸 UI 需要更清晰地表达“已选择第一个材料后不可退出”的状态。
- 掉落物表和遗物表需要编辑器侧校验，避免空表、重复项、权重为 0 导致运行时无奖励。

下一步建议：

- 建立 `RewardCandidateFilter`，统一商店刷新、掉落、重铸的排重逻辑。
- 给重铸补最小交互测试或调试日志，方便确认材料消耗与遗物触发。

### 7. 商店系统

完成度：`65%`

已完成：

- 商店商品由 `MerchantCatalog_SO` 配置。
- 商店支持刷新按钮和刷新费用，费用来源于 `DifficultyConfig_SO`。
- 刷新费用支持递增列表，列表耗尽后使用最大值。
- 刷新会替换所有商品，包括未购买商品和售罄商品。
- 刷新时保持原商品栏类型：遗物刷新遗物，武器刷新武器，图纸刷新图纸。
- 遗物、武器、图纸刷新时会尽量排除玩家已拥有内容。

亟待改造：

- 商店 UI 和刷新逻辑应从地图主流程进一步拆分，降低 `RunGameManager` 压力。
- 商品价格、库存、售罄状态、刷新次数需要更明确地保存在 merchant tile runtime data 中。
- 商店刷新失败时的 UI 反馈需要统一，例如无可刷新候选、金币不足、表为空。

下一步建议：

- 抽出 `MerchantRefreshService`，让 UI 只负责展示和调用。
- 给难度配置增加商店价格预览或校验方法，方便平衡。

### 8. 休息场景

完成度：`60%`

已完成：

- 休息场景已有恢复行动点、招募、重铸等交互入口。
- 重铸已作为独立脚本挂载到 Sprite 上，符合后续场景布置需求。
- 重铸结果与休息场景退出逻辑已经接入。

亟待改造：

- 休息场景所有交互应共享一个 modal state，避免多个功能面板互相抢状态。
- 重铸、休息、招募等功能需要统一交互反馈：hover、click、confirm、locked、completed。
- 休息场景结束回地图的流程需要和地图 runtime state 更清晰地对接。

下一步建议：

- 整理 `RestSceneRuntime` 和各 interactable 的职责边界。
- 为休息场景增加一个小型 debug overlay，显示本次休息还可执行哪些功能。

### 9. 教程系统

完成度：`35%`

已完成：

- 主菜单教程入口与教程框架已搭建。
- 已存在 `TutorialBattleDefinition_SO`、`TutorialBattleRuntime`、`TutorialBattleGuideOverlay`、`TutorialBattleContentFactory` 等脚本。
- 战斗教程内容方向已明确：技能切换、能量消耗、拼点、单方面攻击、划线、技能词条、状态。

亟待改造：

- 教程需要专门敌人与专门技能组，避免普通战斗逻辑随机性破坏教学节奏。
- 教程弹窗需要和战斗阶段事件绑定，例如首次选技能、首次能量不足、首次划线、首次拼点、首次单方面攻击。
- 教程流程需要支持失败重试、跳过、回看说明和编辑器可调。

下一步建议：

- 将战斗教程拆成多个 stage，每个 stage 只教一个概念。
- 为教程敌人和教程武器建立独立数据目录，避免污染正式内容。

### 10. UI 与表现

完成度：`50%`

已完成：

- 主菜单、地图 HUD、队伍面板、商店、重铸、战斗结算、技能信息、状态提示等 UI 已具备基础可用性。
- 多处 UI 已提供字体、颜色、尺寸、偏移等 inspector 接口。
- 战斗技能特效已支持 local/global 偏移，便于修正不同 prefab 的锚点差异。
- 敌人技能预览已支持在数值被被动或临时效果修正时显示划线前值与修正后数值，可用于表现“断角守门人”等预告型威力修正。

亟待改造：

- 全项目字体、字号、颜色、按钮样式、窗口层级需要统一规范。
- 动态分辨率下的 Canvas Scaler、锚点、背景尺寸仍是风险点。
- 战斗表现和 UI 更新需要降低运行时卡顿，尤其是新粒子包、特效生成和状态 UI 刷新。

下一步建议：

- 建立 `UIStyleConfig_SO` 或分场景 UI config。
- 对运行时卡顿进行 profiler 排查，重点看粒子、频繁 Instantiate、Update 中查找、布局重建。

### 11. 构建、测试与工程化

完成度：`40%`

已完成：

- 已新增 Unity 标准构建脚本 `Assets/Scripts/Editor/BuildProject.cs`。
- 项目已有部分 EditMode 测试文件和 superpowers 计划文档。
- 已开始为地图事件等系统补测试。

亟待改造：

- 当前仍缺少稳定的一键编译/PlayMode/EditMode 验证流程记录。
- Unity 版本、构建目标、输出目录、CI 方案需要固化。
- 关键系统缺少自动化测试：战斗结算、遗物触发、商店刷新、重铸、地图生成。

下一步建议：

- 在根目录补一份 `BUILD_AND_TEST.md`，记录本机 Unity 路径、命令行构建、测试命令、常见失败原因。
- 每完成一个复杂系统后补至少一个 EditMode 测试，优先覆盖纯逻辑部分。

## 当前高优先级改造清单

1. 拆分 `BattleButton`：这是后续技能、状态、遗物继续扩展的最大风险。
2. 拆分 `RunGameManager`：地图、商店、奖励、休息、UI 刷新混在一起会拖慢后续迭代。
3. 建立统一遗物触发上下文：减少每个系统单独接遗物逻辑。
4. 建立奖励候选过滤器：统一遗物、武器、图纸、掉落物的不重复规则。
5. 完成战斗教程：这是玩家理解核心玩法的关键，也是验证战斗规则是否清晰的最好工具。
6. 补关键 EditMode 测试：优先测试拼点威力、遗物抽取、商店刷新、重铸奖励。
7. 统一 UI 配置：字体、字号、锚点、缩放、分辨率适配需要集中管理。
8. 做一次性能排查：重点检查 SpecialSkillsEffectsPack、粒子实例化、状态 UI 和 Update 频繁逻辑。

## 内容制作路线

### 敌人内容

当前方向：

- 普通怪：围绕单一状态或单一机制教学，例如毒、寒冷、电流、缓慢。
- 事件怪：机制更偏特殊局面，例如速度干扰、召唤、单方面攻击惩罚。
- 精英怪：应拥有阶段变化或强被动，迫使玩家改变战斗策略。
- Boss：需要组合多个系统，并明确给玩家可读的反制窗口。

描述口径：

- 敌人技能描述中，对玩家我方角色统一称为“目标”。
- 敌人技能描述中，对敌人自己阵营统一称为“友方”。
- 后续敌人草稿、技能 SO 文案和教程引用都应遵循这一口径，避免“友方角色”在敌人技能里产生阵营歧义。

近期目标：

- 审核 `TODO.md` 中的新敌人草稿。
- 把审核通过的敌人落入 SO。
- 每个新敌人至少配置一个测试敌人组，方便 DemoTest 直达验证。

### 遗物内容

当前方向：

- Common：轻量触发，改变小数值或给出低强度状态联动。
- Uncommon：改变一个子系统的收益，例如锻造、掉落、速度、状态层数。
- Rare：改变一类策略的构筑方向。
- Epic：提供明显的战斗或地图节奏改变。
- Legendary：改变核心规则，例如移动消耗、死亡继承、关键资源循环。

近期目标：

- 继续实现 TODO 中未完成遗物。
- 每个新遗物都确认 id、文件夹编号、枚举追加位置一致。
- 为复杂遗物补测试或至少补 debug 日志。

## 近期完成记录

- 2026-04-25：实现并接入审核通过的新敌人 `019` 到 `026`，并补齐其所需的目标优先、阶段限制、濒死保留 1 血、按骰子类型显示等基础接口。
- 2026-04-25：完成“断角守门人”断角被动的攻击技能威力预览表现，敌人技能面板与简要技能面板均会显示划线旧值与新值。
- 2026-04-25：确认新敌人当前统一使用 `001 史莱姆` 贴图作为临时占位资源，待后续逐个替换正式 sprite。
- 2026-04-25：为 `019` 到 `026` 补齐敌人组资产配置：中层普通敌人接入 `Mid/Standard`，事件怪接入 `Events`，`026` 接入 `Mid/Elite`，其中 `021 溺潮拾骨者` 敌人组配置为 3 个个体；新敌人组战利品表暂留空。

- 2026-04-25：补充敌人技能描述称谓规则：玩家我方写作“目标”，敌方阵营写作“友方”。
- 2026-04-25：修复新敌人接入后的编译与资源引用回归问题，移除 `BattleButton` 中友方自伤路径误调的敌方自伤被动调用，并修正 `Bleed` 状态资源与 `023 SkinRivenDisciple` 技能资产的非法 GUID 引用。
- 2026-04-25：新增根目录 `PROJECT_PROGRESS.md`，建立项目长期进度追踪格式。
- 2026-04-25：向 `TODO.md` 追加新敌人设计草稿 `019` 至 `026`，用于后续审核。

## 下次更新检查表

每次完成任务后，检查：

- 是否需要调整某个模块完成度？
- 是否新增了可复用系统或重要接口？
- 是否新增了风险或技术债？
- 是否有测试、构建或 Unity 编译结果需要记录？
- 是否有新的内容设计需要进入 `TODO.md` 或从 `TODO.md` 移入正式数据？
## 2026-04-25 状态资源修复补充

- 修复周期性施加状态的被动资源错链问题。排查确认根因是部分状态资源 `.meta` GUID 漂移，导致旧敌人与遗物资产仍指向历史 GUID 时会读取到错误状态资源，表现为图标缺失、描述错误。
- 已恢复 `Burn`、`Energy`、`Weakness` 的正确历史 GUID，并逐项修正 `HuntersDog`、`LampPrison`、`不融冰`、`初序木片`、`不死心`、`蓄能线圈`、`龙鳞甲` 等资产对 `Slowness`、`Strength_Mutating`、`Energy` 的错误引用。
- 已完成 `Assets/Data/Battle` 下状态资源 `.meta` GUID 格式核查，当前未再发现非法 GUID。

## 2026-04-25 ������״̬��·�޸�����

- �����Ų� `021 �糱ʰ����` �ġ��ǳ���������ȷ�ϸ����ڼ��� `SO` ��״̬��Դ���ã����� [BattleButton.cs](L:/gameProject/MobileGame/Assets/Scripts/BattleScene/Controller/BattleButton.cs) ���Զ������ڱ�����֧���÷�ֱ֧�ӵ��� `AddStatus(null, "erode", "ˮʴ", 1)`��ֻ�����ַ��� id��û�а���ʵ `BattleStatus_SO` д������ʱ״̬���� tooltip ���˻�ΪĬ���ı���ͼ��Ҳ���˻�Ϊ�հ�ռλ��
- ��������ʱ״̬��Դ�� [BattleStatusLibrary.asset](L:/gameProject/MobileGame/Assets/Resources/BattleStatusLibrary.asset) ��ű� [BattleStatusLibrary_SO.cs](L:/gameProject/MobileGame/Assets/Scripts/ScriptableObject/BattleStatusLibrary_SO.cs)��ר�Ÿ����ࡰ�Ǽ��� SO ������Ӳ���뱻��/�ӳ�״̬���ṩ��ʵ `BattleStatus_SO`��
- �ѽ� `BattleButton` �����Ӳ����״̬��֧�л�Ϊͨ����Դ��������ʵ״̬��Դ����Ӧ�ã����� `ˮʴ / ��ʱ�ط� / ��ʱǿ׳ / ���� / ���� / ���� / ����`���Ӷ���֤״̬ tooltip ��ͼ�ꡢ���ơ���������Ϊ����ͳһ������ȷ��Դ�ļ���
- �����޸��� [Strength_Temp.asset.meta](L:/gameProject/MobileGame/Assets/Data/Battle/Strength/Strength_Temp.asset.meta) �ķǷ� GUID����ǰ����Դ��δ����ʽ���ã����Ա���ֱ�Ӹ���Ϊ�Ϸ� GUID��������������������
- ��ǰ�޸��Ѿ�����ı�������Դ��У�飬����δ�ڱ�����ʵ���� Unity PlayMode������༭����������֤ `021 �糱ʰ����` �ġ��ǳ������� `ˮʴ` �Ƿ�ָ���ȷͼ����������

## 2026-04-25 ˮʴͼ����Դ�޸�����

- �� `021 �糱ʰ����` �ġ��ǳ��������������ں�ȷ�ϣ�������ʩ��״̬�Ĵ�����·������Ϊ������ʵ `BattleStatus_SO`���� `ˮʴ` ͼ������ʾ�׿��ֱ��ԭ���� [ErodeIcon.png.meta](L:/gameProject/MobileGame/Assets/Data/Battle/Erode/ErodeIcon.png.meta) ����ʹ���˷Ƿ� GUID��Unity �޷���ȷ������ sprite ��Դ��
- �ѽ� `ErodeIcon.png.meta` ����Ϊ�Ϸ� GUID����ͬ������ [Erode.asset](L:/gameProject/MobileGame/Assets/Data/Battle/Erode/Erode.asset) �� [Poison.asset](L:/gameProject/MobileGame/Assets/Data/Battle/Poison/Poison.asset) �� `iconSprite` ���ã�ȷ��״̬ͼ����Դ���ָ�������
- ���Ҳȷ�������ⲻ�� `021` ���� `SO` ��״̬���ã����ڡ�Ӳ��������״̬��֧ + ״̬ͼ����Դ meta �쳣����������·���ӵ��µı����쳣��
- 2026-04-25������ `021 �糱ʰ����` �� `ʰ�ǻس�` �������ã�ʹ���� `TODO.md` ���룺�������ָĻء�����Ŀ�� / Ŀ�ꡱ����������Ŀ�����ȼ�����Ϊ����ѡ��ӵ�� `ˮʴ` ��Ŀ�ꣻ��������ࡰ����Ŀ��ӵ��ˮʴ���Ѫ����ר���ж�������ı����ս���߼���

## 2026-04-25 ���˼��� BuiltIn ���û�����

- �� `021 �糱ʰ����` �� `ʰ�ǻس�` �� [BattleButton.cs](L:/gameProject/MobileGame/Assets/Scripts/BattleScene/Controller/BattleButton.cs) ��ר��Ӳ�����֧Ǩ�Ƶ� [EnemySkill_SO.cs](L:/gameProject/MobileGame/Assets/Data/Enemies/Skill/EnemySkill_SO.cs) ������ `builtInEffects` ������·��
- `EnemySkill_SO` ����֧����ÿ��������ֱ�����ÿɸ��õ� BuiltIn Ч����Ŀ����ǰ�ڽ�֧�֣���״̬���������Ļظ�Ч������״̬���������Ķ����˺�Ч�������ṩ����������Ŀ�� / ����Ŀ�� / �����ѷ� / ��������״̬��Դ���ýӿڡ�
- [BoneTideReclaim.asset](L:/gameProject/MobileGame/Assets/Data/Enemies/Enemies/021%20DrownedBoneCollector/BoneTideReclaim.asset) �Ѹ�Ϊͨ�� BuiltIn ������á�ʹ��ʱ������Ŀ��ӵ��ˮʴ���Լ��ظ� 2 ���������롰����ʱ��Ŀ��ӵ��ˮʴ�������� 1 ���˺�����
- [BattleSkillInfoOverlay.cs](L:/gameProject/MobileGame/Assets/Scripts/BattleScene/Controller/BattleSkillInfoOverlay.cs) �Ѳ��� BuiltIn Ч��˵�����ɣ���˵��˼�������������Ҫ��������ֱ����ʾ��Щ�Զ����ɵĴ������������ں��������������
- ��ǰ��ɵ����ı��������ü����룬��δ�ڱ�����ʵ���� Unity ������ս������֤������༭�������ȼ�� `ʰ�ǻس�` �� BuiltIn �����ʾ�������������ɣ��Լ�ս���ڻ�Ѫ / ׷���˺��Ƿ�������һ�¡�

## 2026-04-25 �������˼���Ӳ����Ǩ�Ʋ���

- �����Ų顰д���� `BattleButton` ��ĵ��˼���ר���߼�������ȷ�ϲ�Ǩ������ 4 ��ͬ���߼���`022 ������ / ��������`��`024 ������� / �ಽ`��`025 �ҽ�˾�� / ����`��`025 �ҽ�˾�� / ����ת��`��
- [BattleButton.cs](L:/gameProject/MobileGame/Assets/Scripts/BattleScene/Controller/BattleButton.cs) ������������ר�÷�֧���Ƴ�������ͨ��������Ӳ���봥������ǰ������ `EnemyID == 24` ƴ������ +1 �߼����ڵ��˱��� `����`�����Ǽ����ʲ���������δ���뱾�� BuiltIn Ǩ�Ʒ�Χ��
- [SweetCollapse.asset](L:/gameProject/MobileGame/Assets/Data/Enemies/Enemies/022%20Rotblossom/SweetCollapse.asset) �Ѳ��ϵ� 1 ���� BuiltIn ״̬ʩ�ӣ�ƴ��ʤ��ʱʹĿ���� 1 �� `����`��
- [SideStep.asset](L:/gameProject/MobileGame/Assets/Data/Enemies/Enemies/024%20HeadwindRanger/SideStep.asset) �Ѳ������� BuiltIn ״̬��������Χ�������������ٶȵ���Ŀ��ʱ��ƴ��ʤ��������� 1 �� `����`���ұ���������� +2��
- [CalamityInvocation.asset](L:/gameProject/MobileGame/Assets/Data/Enemies/Enemies/025%20AshPriest/CalamityInvocation.asset) �Ѳ������� BuiltIn ״̬������ʱ��Ŀ������ `����`�������ʩ�� 1 �� `����`��
- [AtonementTransfer.asset](L:/gameProject/MobileGame/Assets/Data/Enemies/Enemies/025%20AshPriest/AtonementTransfer.asset) �Ѳ��� BuiltIn��ת���������״̬�����ã�ʹ��ʱ������ 1 ���������״̬ת�Ƹ����Ŀ�ꣻ��û�п�ת�Ƶĸ���״̬�����������»غϿ�ʼʱ��� 1 �� `����`��
- [BattleSkillInfoOverlay.cs](L:/gameProject/MobileGame/Assets/Scripts/BattleScene/Controller/BattleSkillInfoOverlay.cs) ����չ BuiltIn ˵�����ɣ���֧�� `ʩ��״̬`��`ת���������״̬`������һ������˵�����Լ� `rangeModifiers` �Ĵ���չʾ��������ҿ����ڼ����������ͼ�Ҫ���������ֱ�ӿ�����Щ�Զ����ɵ���ʵ������
- ������ɵ��Ǵ��뼶�����ü����ı����˲飬��δ�ڵ�ǰ����ʵ���� Unity �����ս������֤������༭����������֤ `022/024/025` ��ؼ��ܵ� Inspector ������ʾ����������ı����ɣ��Լ�ս���ڴ�������Ƿ��� `TODO` һ�¡�


## 2026-04-26 Battle Fixes

- Fixed ally skill selection gating in `Assets/Scripts/BattleScene/Controller/BattleButton.cs`. Skills with `energyCost > 0` now require enough energy for at least one use even before any line is connected, so they can no longer be selected while energy is insufficient.
- Fixed linked last-stand resolution in `Assets/Scripts/BattleScene/Controller/BattleEnemyController.cs` for `021 DrownedBoneCollector`. Units only stay at 1 HP while another living ally still has more than 1 HP; once every living ally is at 1 HP or lower, all linked units at 1 HP collapse together.
- Updated edit-mode regression tests in `Assets/Tests/EditMode/Editor/BattleButtonSkillSelectionTests.cs` and `Assets/Tests/EditMode/Editor/EnemyTodoTests.cs`.
- Unity EditMode / PlayMode tests were not executed in this environment.


## 2026-04-26 Relic Refactor Design

- Added design spec `docs/superpowers/specs/2026-04-26-relic-system-refactor-design.md` for the first relic-system refactor stage.
- Scope is intentionally conservative: keep existing relic assets, trigger enum order, and built-in effect enum order stable; first focus on a unified trigger dispatcher, clearer ownership, and regression tests.
- Next implementation step should create the dispatcher wrapper and migrate low-risk call sites before touching battle-heavy relic triggers.


## 2026-04-26 Relic Trigger Dispatcher Stage 1

- Added `Assets/Scripts/Shared/RelicTriggerDispatcher.cs` as the first unified entry point for relic trigger processing.
- Added `Assets/Tests/EditMode/Editor/RelicTriggerDispatcherTests.cs` to cover run relic processing, battle relic processing with feedback, merchant price calculation, merchant purchase completion, and rest reforge limit calculation.
- Migrated low-risk call sites in map movement, key collection, calamity application, merchant price display/purchase, loot reforge, relic reforge, and rest reforge limit calculation to `RelicTriggerDispatcher`.
- Updated `BattleRelicEffectRunner` to route through `RelicTriggerDispatcher.ProcessBattleRelics` while deferring direct battle-heavy trigger migrations in `BattleButton` and `BattleAllyController` to the next stage.
- Unity EditMode / PlayMode tests were not executed in this environment because no Unity command-line runner was found. Static search confirmed remaining direct `RelicSystem.Process` calls are concentrated in dispatcher internals and deferred battle-heavy call sites.


## 2026-04-26 Relic Trigger Dispatcher Stage 2

- Migrated battle-side direct relic processing calls in `Assets/Scripts/BattleScene/Controller/BattleAllyController.cs` and `Assets/Scripts/BattleScene/Controller/BattleButton.cs` to `RelicTriggerDispatcher.ProcessBattleRelics`.
- Preserved existing `RelicExecutionContext` factory usage and existing feedback flush timing, so this stage changes routing only and does not change relic effect behavior or asset data.
- Static verification now shows external script direct calls to `RelicSystem.Process` and relic helper pricing methods have been collected behind `Assets/Scripts/Shared/RelicTriggerDispatcher.cs`.
- Unity EditMode / PlayMode tests were not executed in this environment. Please run `RelicTriggerDispatcherTests` plus the existing relic regression tests in the Unity Test Runner.


## 2026-04-26 Relic Trigger Dispatcher Stage 3

- Added semantic dispatcher methods in `Assets/Scripts/Shared/RelicTriggerDispatcher.cs` for common map, reforge, calamity, status, dice, battle setup, and battle round triggers.
- Migrated matching call sites so callers no longer need to manually pair `RelicExecutionContext.Create...` factories with dispatcher processing for those common trigger families.
- Added field group comments to `Assets/Scripts/Shared/RelicExecutionContext.cs` to clarify which fields belong to routing, map/run, status, merchant, dice, damage, reforge, rewards, battle setup, round/speed, calamity, participants, and feedback.
- Expanded `Assets/Tests/EditMode/Editor/RelicTriggerDispatcherTests.cs` with semantic dispatcher coverage for move-cost and battle-round-end triggers.
- Static verification shows the migrated context factories are now used through `RelicTriggerDispatcher`. Unity EditMode / PlayMode tests were not executed in this environment; please run `RelicTriggerDispatcherTests` in the Unity Test Runner.


## 2026-04-26 - Relic System Refactor Stage 4

- Added `RelicValidationUtility` as a reusable validation layer for relic assets and relic tables.
- Added EditMode coverage for missing status references, disabled/empty built-in effects, ineffective numeric values, empty relic tables, duplicate relic entries, and zero rarity weights.
- Added Unity Editor menu entry `Tools/Relics/Validate Relics` to scan all `Relic_SO` and `RelicTable_SO` assets and report clickable Console issues.
- Next: run `RelicValidationUtilityTests` in Unity Test Runner, then use the validator on existing relic assets before continuing deeper runtime extraction.


## 2026-04-26 - Relic System Refactor Stage 5

- User confirmed `RelicValidationUtilityTests` passed in Unity Test Runner.
- Added built-in relic effect registration diagnostics through `RelicSystem.IsBuiltInEffectRegistered` and `RelicSystem.GetUnregisteredBuiltInEffectTypesForDiagnostics`.
- Added EditMode coverage to ensure every non-None `RelicBuiltInEffectType` enum value has a registered handler.
- Extended relic asset validation so unknown or unregistered built-in effect enum values are reported as errors instead of failing silently at runtime.
- Next: run `RelicBuiltInEffectRegistrationTests` and rerun `RelicValidationUtilityTests`, then continue extracting high-risk built-in effect groups only after coverage stays green.


## 2026-04-26 - Relic Regression Fixes

- Fixed `RelicSystemTodoRelicTests` helper drift: `PathfinderManual` now uses `RelicTrigger.OnKeyCollected`, and generated defense skills now set `EnemySkill_SO.skillCategory` instead of the removed private `isDefenseSkill` field.
- Adjusted ally inheritable speed-button transfer so martyr-style relics use the defeated ally's available speed-button list as a fallback source for transfer count, preventing death-time active-button refresh from losing transferable buttons.
- Pending verification: rerun `RelicSystemTodoRelicTests` in Unity Test Runner.


### 2026-04-26 - Relic Regression Fix Follow-up

- Refined martyr-style speed-button inheritance: `BattleAllyController` now records the last living active speed-button count and uses it during death-trigger transfer, so death-time active-button clearing does not erase the transferable count.
- Pending verification: rerun the remaining `MartyrRelic_TransfersInheritedSpeedButtonsToLowestHpLivingAllyOnDeath` test.


### 2026-04-26 - SpeedButton Test Safety Fix

- Fixed a `SpeedButton.DisconnectFromTarget` null-reference path exposed by `MartyrRelic_TransfersInheritedSpeedButtonsToLowestHpLivingAllyOnDeath`; disconnect now resolves runtime references first and guards missing `LineRenderer`.
- Pending verification: rerun `RelicSystemTodoRelicTests` in Unity Test Runner.


### 2026-04-26 - Relic Regression Verification

- User confirmed `RelicSystemTodoRelicTests` now passes after the trigger/helper drift fixes, martyr speed-button inheritance refinement, and `SpeedButton` disconnect null-safety fix.
- Next refactor step starts from a low-risk split: move built-in relic effect registration and dispatch plumbing out of the large handler file before categorizing individual handlers.


## 2026-04-26 - Relic System Refactor Stage 6

- Split built-in relic effect registration and dispatch plumbing into `Assets/Scripts/Shared/RelicSystem.BuiltInEffectRegistry.cs`.
- `RelicSystem.BuiltInEffects.cs` now focuses on concrete built-in effect handlers, while the registry file owns handler mapping, diagnostics, and `ApplyBuiltInEffects` dispatch.
- This is a behavior-preserving refactor: enum order and relic asset data remain unchanged.
- Static checks completed: registry symbols now live only in the new registry file, and `git diff --check` reports no formatting errors for the touched files.
- Pending verification: rerun `RelicBuiltInEffectRegistrationTests`, `RelicValidationUtilityTests`, and `RelicSystemTodoRelicTests` in Unity Test Runner.


## 2026-04-26 - Relic System Refactor Stage 7

- User confirmed Stage 6 verification passed: `RelicBuiltInEffectRegistrationTests`, `RelicValidationUtilityTests`, and `RelicSystemTodoRelicTests` all passed in Unity Test Runner.
- Split low-coupling run/map/shop/reforge/reward/setup built-in relic handlers into `Assets/Scripts/Shared/RelicSystem.BuiltInRunEffects.cs`.
- `RelicSystem.BuiltInEffects.cs` now keeps the more combat-focused status, dice, damage, healing, and death-status-transfer handlers.
- This remains behavior-preserving: handler names, registry entries, enum order, and relic asset data are unchanged.
- Static checks completed: moved handler names resolve in the new file with no duplicate definitions, and `git diff --check` reports no formatting errors for touched files.
- Pending verification: rerun `RelicBuiltInEffectRegistrationTests`, `RelicValidationUtilityTests`, and `RelicSystemTodoRelicTests` in Unity Test Runner.


## 2026-04-26 - Relic System Refactor Stage 8

- User confirmed Stage 7 verification passed: `RelicBuiltInEffectRegistrationTests`, `RelicValidationUtilityTests`, and `RelicSystemTodoRelicTests` all passed in Unity Test Runner.
- Split player dice and power-modifier built-in relic handlers into `Assets/Scripts/Shared/RelicSystem.BuiltInDiceEffects.cs`.
- Moved handlers include self-status dice bonuses, last-round skill matching, faster-than-target clash bonuses, target status clash min reduction, consecutive skill use bonuses, and defense skill max-power bonuses.
- This remains behavior-preserving: handler names, registry entries, enum order, and relic asset data are unchanged.
- Static checks completed: moved dice handler names resolve in the new file with no duplicate definitions, total built-in handler count remains 47, and `git diff --check` reports no formatting errors for touched files.
- Pending verification: rerun `RelicBuiltInEffectRegistrationTests`, `RelicValidationUtilityTests`, and `RelicSystemTodoRelicTests` in Unity Test Runner.


## 2026-04-26 - Relic System Refactor Stage 9

- User confirmed Stage 8 verification passed: `RelicBuiltInEffectRegistrationTests`, `RelicValidationUtilityTests`, and `RelicSystemTodoRelicTests` all passed in Unity Test Runner.
- Split incoming/outgoing damage, reflect, and lethal-protection built-in relic handlers into `Assets/Scripts/Shared/RelicSystem.BuiltInDamageEffects.cs`.
- Moved handlers include flat and percent incoming damage modifiers, attacker-status damage reduction, status-stack flat reduction, outgoing damage bonuses, reflect damage, and one-time lethal survival.
- This remains behavior-preserving: handler names, registry entries, enum order, and relic asset data are unchanged.
- Static checks completed: moved damage handler names resolve in the new file with no duplicate definitions, total built-in handler count remains 47, and `git diff --check` reports no formatting errors for touched files.
- Pending verification: rerun `RelicBuiltInEffectRegistrationTests`, `RelicValidationUtilityTests`, and `RelicSystemTodoRelicTests` in Unity Test Runner.

## 2026-04-26 - Relic System Refactor Stage 10

- User confirmed Stage 9 verification passed.
- Split status application, delayed status scheduling, status-on-hit, status-on-death, and status-transfer built-in relic handlers into `Assets/Scripts/Shared/RelicSystem.BuiltInStatusEffects.cs`.
- `RelicSystem.BuiltInEffects.cs` now only contains the remaining healing-oriented built-in relic handlers, making the next extraction step small and low-risk.
- This remains behavior-preserving: handler names, registry entries, enum order, and relic asset data are unchanged.
- Static checks completed: status handler names resolve in the new file with no duplicate definitions, total built-in handler count remains 47, and `git diff --check` reports no formatting errors for touched files.
- Pending verification: rerun `RelicBuiltInEffectRegistrationTests`, `RelicValidationUtilityTests`, and `RelicSystemTodoRelicTests` in Unity Test Runner.
## 2026-04-26 - Relic System Refactor Stage 11

- User confirmed Stage 10 verification passed and approved a slightly larger refactor step.
- Renamed the remaining generic healing handlers into `Assets/Scripts/Shared/RelicSystem.BuiltInHealingEffects.cs`.
- Split the former run-level handler bucket into focused files: `RelicSystem.BuiltInMapEconomyEffects.cs`, `RelicSystem.BuiltInReforgeRewardEffects.cs`, and `RelicSystem.BuiltInBattleSetupEffects.cs`.
- This remains behavior-preserving: handler names, registry entries, enum order, and relic asset data are unchanged.
- Static checks completed: total built-in handler count remains 47, every registered handler resolves to exactly one implementation, old generic bucket names no longer appear in shared scripts, and `git diff --check` reports no formatting errors for touched files.
- Pending verification: rerun `RelicBuiltInEffectRegistrationTests`, `RelicValidationUtilityTests`, and `RelicSystemTodoRelicTests` in Unity Test Runner.
## 2026-04-26 - Relic System Refactor Stage 12

- User confirmed Stage 11 verification passed.
- Converted `RelicExecutionContext` into a partial class and kept the root file focused on shared context fields only.
- Split context factory methods into `Assets/Scripts/Shared/RelicExecutionContext.RunFactories.cs` and `Assets/Scripts/Shared/RelicExecutionContext.BattleFactories.cs`.
- This remains behavior-preserving: factory method names, signatures, triggers, default values, and output field assignments are unchanged.
- Static checks completed: all 27 `Create...` factory methods are still present, all partial declarations are consistent, and `git diff --check` reports no formatting errors for touched files.
- Pending verification: rerun `RelicBuiltInEffectRegistrationTests`, `RelicValidationUtilityTests`, and `RelicSystemTodoRelicTests` in Unity Test Runner.
## 2026-04-26 - Relic System Refactor Stage 13

- User confirmed Stage 12 verification passed.
- Converted `RelicTriggerDispatcher` into a partial static class and kept the root file focused on core run/battle processing plus feedback flushing.
- Split run-level dispatch helpers into `Assets/Scripts/Shared/RelicTriggerDispatcher.RunTriggers.cs` and battle-level dispatch helpers into `Assets/Scripts/Shared/RelicTriggerDispatcher.BattleTriggers.cs`.
- This remains behavior-preserving: all public dispatcher method names, signatures, return values, and underlying context factory calls are unchanged.
- Static checks completed: dispatcher still exposes 16 public methods, no duplicate public dispatcher method names were found, all dispatcher declarations are partial, and `git diff --check` reports no formatting errors for touched files.
- Pending verification: rerun `RelicTriggerDispatcherTests`, `RelicBuiltInEffectRegistrationTests`, `RelicValidationUtilityTests`, and `RelicSystemTodoRelicTests` in Unity Test Runner.
## 2026-04-26 - Relic System Refactor Stage 14

- Continued shrinking the status-effect bucket by splitting `Assets/Scripts/Shared/RelicSystem.BuiltInStatusEffects.cs` into smaller partial files.
- `RelicSystem.BuiltInStatusEffects.cs` now keeps shared status helpers plus `HandleAddExtraStatusStacksOnApply` only.
- Added `RelicSystem.BuiltInStatusRoundEffects.cs`, `RelicSystem.BuiltInStatusReactionEffects.cs`, and `RelicSystem.BuiltInStatusDeathEffects.cs` to separate round-based, reaction-based, and death-based status handlers.
- Further narrowed `Assets/Scripts/Shared/RelicSystem.cs` by moving merchant price / purchase / reset and rest reforge limit convenience APIs into `Assets/Scripts/Shared/RelicSystem.RunFacade.cs`.
- Static checks completed: all 13 status-oriented built-in handler methods still resolve exactly once under `Assets/Scripts/Shared`.
- CLI verification note: `dotnet msbuild Assembly-CSharp.csproj /t:Build` still fails in this environment because Unity-generated `Temp/obj/Assembly-CSharp/project.assets.json` is absent. Use Unity Test Runner for real verification.
- Pending verification: rerun `RelicTriggerDispatcherTests`, `RelicBuiltInEffectRegistrationTests`, `RelicValidationUtilityTests`, and `RelicSystemTodoRelicTests` in Unity Test Runner.

## 2026-04-26 - Relic System Refactor Stage 15

- User confirmed Stage 14 verification passed.
- Added focused high-risk EditMode coverage in `Assets/Tests/EditMode/Editor/RelicSystemHighRiskTriggerTests.cs`.
- New tests cover status stack amplification boundaries, delayed round-start status application, single-attack vs attack damage modifier separation, clash-only dice modifiers, and same-rarity bonus loot rewards for reforge / battle rewards.
- Static checks completed: `git diff --check` passes for the new test file.
- CLI verification note: `dotnet msbuild Assembly-CSharp-Editor.csproj /t:Build` still fails because Unity-generated `Temp/obj/Assembly-CSharp-Editor/project.assets.json` is absent. Unity Test Runner is still required for real verification.
- Pending verification: run `RelicSystemHighRiskTriggerTests` in Unity Test Runner, then rerun the existing relic regression group.

## 2026-04-27 - Relic System Refactor Stage 16-17

- Connected `RelicValidationUtility` to `RelicBuiltInEffectMetadataCatalog` so built-in effect validation now reads status requirements, condition-status requirements, and numeric-value rules from the shared metadata catalog instead of maintaining parallel switch statements.
- Added metadata coverage tests to `RelicBuiltInEffectRegistrationTests`: every non-None built-in effect enum value must have validation metadata, and representative effects assert required assets / numeric rules.
- Added validation tests in `RelicValidationUtilityTests` to cover metadata-driven numeric behavior for no-amount effects and power-delta effects.
- Removed the old validation-only `RequiresStatusData`, `RequiresConditionStatusData`, and `HasMeaningfulNumericValue` switch helpers from `RelicValidationUtility`.
- Static checks completed: all 47 non-None built-in effect enum values are present in the metadata catalog, validation utility now references metadata, and `git diff --check` reports no formatting errors for touched files.
- Pending verification: run `RelicBuiltInEffectRegistrationTests`, `RelicValidationUtilityTests`, `RelicSystemHighRiskTriggerTests`, and the existing relic regression group in Unity Test Runner.
## 2026-04-27 - Relic System Refactor Stage 18

- User confirmed Stage 16-17 verification passed.
- Split the large `RelicExecutionContext.BattleFactories.cs` bucket into focused battle context factory files.
- Renamed the existing file to `Assets/Scripts/Shared/RelicExecutionContext.BattleDamageFactories.cs` for damage / hit / kill-related contexts.
- Added `RelicExecutionContext.BattleStatusFactories.cs`, `RelicExecutionContext.BattleDiceFactories.cs`, `RelicExecutionContext.BattleRoundFactories.cs`, and `RelicExecutionContext.BattleDeathFactories.cs`.
- This remains behavior-preserving: all public factory method names, signatures, trigger assignments, clamped defaults, and feedback list initialization are unchanged.
- Static checks completed: all 27 context factory methods are still present, 17 battle context factory methods remain under battle factory files, old `RelicExecutionContext.BattleFactories.cs` no longer exists, and `git diff --check` reports no formatting errors for touched files.
- Pending verification: rerun `RelicTriggerDispatcherTests`, `RelicSystemHighRiskTriggerTests`, `RelicBuiltInEffectRegistrationTests`, `RelicValidationUtilityTests`, and `RelicSystemTodoRelicTests` in Unity Test Runner.
## 2026-04-27 - Relic System Refactor Stage 19

- User confirmed Stage 18 verification passed.
- Added `Assets/Scripts/Shared/RewardCandidateFilterService.cs` as the first shared reward candidate filtering service.
- Added `Assets/Tests/EditMode/Editor/RewardCandidateFilterServiceTests.cs` covering same-rarity loot candidate filtering, null-safe empty results, relic candidate exclusion, and relic-table availability checks.
- Migrated `RelicSystem.BuiltInReforgeRewardEffects.cs` same-rarity loot filtering for loot reforge and battle loot bonus rewards to `RewardCandidateFilterService.BuildLootCandidatesWithRarity`.
- This stage intentionally leaves merchant weapon / blueprint / relic refresh filtering in `RunGameManager` for the next migration step, because that path is more tightly coupled to merchant offer generation.
- Static checks completed: old same-rarity loops were removed from `RelicSystem.BuiltInReforgeRewardEffects.cs`, the service is referenced by handlers and tests, and `git diff --check` reports no formatting errors for touched files.
- CLI verification note: `dotnet msbuild Assembly-CSharp-Editor.csproj /t:Build` still fails because Unity-generated `Temp/obj/Assembly-CSharp-Editor/project.assets.json` is absent. Unity Test Runner is required for real verification.
- Pending verification: run `RewardCandidateFilterServiceTests`, `RelicSystemHighRiskTriggerTests`, and the existing relic regression group in Unity Test Runner.
## 2026-04-27 - Relic System Refactor Stage 20

- User confirmed Stage 19 verification passed.
- Extended `RewardCandidateFilterService` with merchant relic goods-entry filtering through `BuildAvailableMerchantRelicGoodsEntries` and `HasAvailableMerchantRelicCandidate`.
- Added EditMode coverage for merchant relic goods filtering, including relic-table entries, legacy relic entries, null entries, and exclusion of already-owned relics.
- Migrated `RunGameManager` merchant relic candidate filtering for initial stock generation, guaranteed relic offers, fill-remaining offers, and relic refresh offers to the shared reward candidate filter service.
- Removed the duplicate local `BuildAvailableRelicGoodsEntries` and `HasAvailableRelicCandidate` implementations from `RunGameManager`.
- This stage intentionally keeps relic draw and pricing behavior in `RunGameManager`; only candidate availability filtering moved to the shared service.
- Static checks completed: `RunGameManager` no longer defines local merchant relic availability helpers, merchant relic candidate calls now use `RewardCandidateFilterService`, and `git diff --check` reports no formatting errors for touched files.
- Pending verification: run `RewardCandidateFilterServiceTests`, merchant refresh smoke testing, and the existing relic regression group in Unity Test Runner.## 2026-04-27 - Relic System Refactor Stage 21

- User confirmed Stage 20 verification passed.
- Extended `RewardCandidateFilterService` with merchant weapon and blueprint goods-entry filtering through `BuildAvailableMerchantWeaponGoodsEntries`, `HasAvailableMerchantWeaponCandidate`, `BuildAvailableMerchantBlueprintGoodsEntries`, and `HasAvailableMerchantBlueprintCandidate`.
- Added EditMode coverage for merchant weapon / blueprint goods filtering, including null entries, missing asset data, and exclusion of already-owned or currently-reserved items.
- Migrated `RunGameManager` merchant refresh candidate filtering for weapon and blueprint offers to the shared reward candidate filter service.
- This stage keeps stock generation, weighted rolling, price rolling, and offer mutation behavior unchanged; only candidate availability filtering moved out of `RunGameManager`.
- Static checks completed: shared service methods resolve at their intended call sites, old refresh-local weapon / blueprint filtering loops were removed from `RunGameManager`, and `git diff --check` reports no formatting errors for touched files.
- Pending verification: rerun `RewardCandidateFilterServiceTests`, merchant refresh smoke testing, and the existing relic regression group in Unity Test Runner.## 2026-04-27 - Relic System Refactor Stage 22

- User confirmed Stage 21 verification passed.
- Extended `RewardCandidateFilterService` with merchant loot goods-entry filtering through `BuildAvailableMerchantLootGoodsEntries` and `HasAvailableMerchantLootCandidate`.
- Added EditMode coverage for merchant loot goods filtering, including null entries, missing loot data, and exclusion of currently reserved loot items.
- Migrated `RunGameManager` merchant refresh candidate filtering for loot offers to the shared reward candidate filter service.
- Merchant refresh candidate filtering for loot, weapon, blueprint, and relic offers is now centralized in `RewardCandidateFilterService`; rolling weights, prices, and offer mutation behavior remain unchanged.
- Static checks completed: refresh call sites now use the shared service for all four merchant item categories, and `git diff --check` reports no formatting errors for touched files.
- Pending verification: rerun `RewardCandidateFilterServiceTests`, merchant refresh smoke testing, and the existing relic regression group in Unity Test Runner.## 2026-04-27 - Relic System Refactor Stage 23

- User confirmed Stage 22 verification passed.
- Added `Assets/Scripts/Shared/MerchantOfferFactoryService.cs` as a focused helper for creating and applying non-relic merchant offers.
- Added EditMode coverage in `Assets/Tests/EditMode/Editor/MerchantOfferFactoryServiceTests.cs` for loot / weapon / blueprint offer creation, price clamping, invalid input handling, and refresh-time payload replacement.
- Migrated `RunGameManager` guaranteed stock generation, random fill generation, and refresh mutation for loot / weapon / blueprint offers to `MerchantOfferFactoryService`.
- Removed the now-unused `RollMerchantPrice` helper from `RunGameManager`; relic offer creation and difficulty-based relic pricing intentionally remain in `RunGameManager` for the next isolated step.
- Static checks completed: non-relic merchant offer creation call sites now use the factory service, and `git diff --check` reports no formatting errors for touched files.
- Pending verification: run `MerchantOfferFactoryServiceTests`, rerun `RewardCandidateFilterServiceTests`, and run the existing relic regression group in Unity Test Runner.## 2026-04-27 - Relic System Refactor Stage 24

- User confirmed Stage 23 verification passed.
- Extended `MerchantOfferFactoryService` with relic offer creation through `TryCreateRelicOffer`, including relic-table drawing, legacy relic fallback, excluded-relic checks, excluded-set updates, and difficulty-based relic pricing.
- Added EditMode coverage for relic merchant offer creation from legacy relic entries and relic tables, fixed difficulty price resolution, excluded relic exhaustion, and invalid random input handling.
- Migrated `RunGameManager` relic guaranteed stock generation, random fill generation, and refresh mutation to `MerchantOfferFactoryService`.
- Removed duplicate merchant relic price helpers, relic offer creation helper, and generic offer payload mutation helper from `RunGameManager`; merchant offer payload creation for all four item categories is now centralized in the factory service.
- Static checks completed: old `TryCreateRelicMerchantOffer`, `ResolveMerchantRelicPrice`, `ApplyMerchantOfferData`, and `RollMerchantPrice` symbols no longer remain in `RunGameManager`, and `git diff --check` reports no formatting errors for touched files.
- Pending verification: run `MerchantOfferFactoryServiceTests`, `RewardCandidateFilterServiceTests`, merchant refresh smoke testing, and the existing relic regression group in Unity Test Runner.## 2026-04-27 - Relic System Refactor Stage 25

- User confirmed Stage 24 verification passed.
- Added `Assets/Scripts/Shared/MerchantOfferSelectionService.cs` to own merchant stock selection algorithms: target stock count resolution, available category weight resolution, weighted entry index rolling, and offer shuffling.
- Added EditMode coverage in `Assets/Tests/EditMode/Editor/MerchantOfferSelectionServiceTests.cs` for target count clamping, collapsed ranges, missing category pools, single-entry weighted selection, and shuffle element preservation.
- Migrated `RunGameManager` merchant stock generation and refresh selection call sites to `MerchantOfferSelectionService`.
- Removed duplicate merchant selection helper methods from `RunGameManager`, keeping that class focused on run-state orchestration and map/merchant flow control.
- Static checks completed: old local selection helper names no longer remain in `RunGameManager`, service call sites resolve, and `git diff --check` reports no formatting errors for touched files.
- Pending verification: run `MerchantOfferSelectionServiceTests`, rerun `MerchantOfferFactoryServiceTests`, `RewardCandidateFilterServiceTests`, merchant refresh smoke testing, and the existing relic regression group in Unity Test Runner.## 2026-04-27 - Relic System Refactor Stage 26

- User confirmed Stage 25 verification passed.
- Added `Assets/Scripts/Shared/MerchantOfferExclusionService.cs` to own merchant refresh exclusion-set construction for owned relics, weapons, blueprints, and currently offered merchant loot / weapons / blueprints / relics.
- Added EditMode coverage in `Assets/Tests/EditMode/Editor/MerchantOfferExclusionServiceTests.cs` for inventory exclusion sets, null / non-positive inventory entries, current merchant offer filtering, ignored offer handling, and sold-out inclusion behavior.
- Migrated `RunGameManager` merchant initial relic filtering and refresh exclusion setup to `MerchantOfferExclusionService`.
- Removed duplicate owned-inventory and current-merchant-offer exclusion helper methods from `RunGameManager`, further narrowing it toward merchant flow orchestration only.
- Static checks completed: local exclusion helper definitions no longer remain in `RunGameManager`, service call sites resolve, and `git diff --check` reports no formatting errors for touched files.
- Pending verification: run `MerchantOfferExclusionServiceTests`, rerun `MerchantOfferSelectionServiceTests`, `MerchantOfferFactoryServiceTests`, `RewardCandidateFilterServiceTests`, merchant refresh smoke testing, and the existing relic regression group in Unity Test Runner.## 2026-04-27 - Relic System Refactor Stage 27

- User confirmed Stage 26 verification passed and approved a larger refactor step.
- Added `Assets/Scripts/Shared/MerchantStockService.cs` as the merchant stock orchestration layer for initial stock generation, guaranteed category offers, random fill offers, refreshable-slot counting, and same-type merchant refresh mutation.
- Added EditMode coverage in `Assets/Tests/EditMode/Editor/MerchantStockServiceTests.cs` for empty catalog generation, guaranteed initial stock creation, owned relic exclusion, same-type refresh replacement, owned weapon / blueprint / relic refresh exclusions, and refreshable slot counting.
- Migrated `RunGameManager` merchant stock generation and refresh orchestration to `MerchantStockService`, while keeping scene-facing responsibilities in `RunGameManager`: merchant tile checks, deterministic RNG seed creation, money spending, HUD feedback, and UI callbacks.
- `MerchantStockService` composes the smaller services introduced in earlier stages: `RewardCandidateFilterService`, `MerchantOfferFactoryService`, `MerchantOfferSelectionService`, and `MerchantOfferExclusionService`.
- Removed the large merchant stock / refresh helper block from `RunGameManager`, cutting that file's merchant implementation surface down to thin delegation methods.
- Static checks completed: old merchant stock helper names now resolve inside `MerchantStockService`, `RunGameManager` delegates through the new service, and `git diff --check` reports no formatting errors for touched files.
- Pending verification: run `MerchantStockServiceTests`, then rerun `MerchantOfferExclusionServiceTests`, `MerchantOfferSelectionServiceTests`, `MerchantOfferFactoryServiceTests`, `RewardCandidateFilterServiceTests`, merchant refresh smoke testing, and the existing relic regression group in Unity Test Runner.## 2026-04-27 - Relic System Refactor Stage 28

- User confirmed Stage 27 verification passed.
- Added `Assets/Scripts/Shared/MerchantPurchaseService.cs` to own merchant purchase validation, relic-adjusted price calculation, money spending, inventory/relic/blueprint/weapon/loot delivery, sold-out mutation, and merchant purchase completion relic triggers.
- Added structured purchase results through `MerchantPurchaseResult` and `MerchantPurchaseFailureReason`, so scene/UI callers no longer need to duplicate purchase validation branches.
- Added EditMode coverage in `Assets/Tests/EditMode/Editor/MerchantPurchaseServiceTests.cs` for successful loot purchase, insufficient money, missing data, sold-out offers, and purchase-completion relic trigger behavior.
- Migrated `RunGameManager.HandleBuyMerchantOffer` to call `MerchantPurchaseService.TryPurchaseOffer`; `RunGameManager` now only handles HUD display and UI callback return text for purchases.
- Static checks completed: direct merchant purchase price calculation and purchase-completion trigger calls no longer remain in `RunGameManager`, and `git diff --check` reports no formatting errors for touched files.
- Pending verification: run `MerchantPurchaseServiceTests`, rerun `MerchantStockServiceTests` and the merchant service test group, then run the existing relic regression group in Unity Test Runner.
## 2026-04-27 - RunGameManager Encoding Repair

- Repaired mojibake in `Assets/Scripts/MapScene/Manager/RunGameManager.cs` caused by UTF-8 text being decoded through the wrong system encoding during an earlier bulk edit.
- Restored corrupted Chinese UI/status strings such as tutorial prompts, merchant refresh messages, event result text, tile labels, and tutorial overlay button labels.
- Preserved the recent merchant-service refactor changes and restored the file to UTF-8 BOM + CRLF formatting to avoid Git/Unity text-format churn.
- Static checks completed: targeted mojibake scan found no remaining suspicious text, and `git diff --check -- Assets/Scripts/MapScene/Manager/RunGameManager.cs` reports no formatting errors.

## 2026-04-27 - Relic System Refactor Stage 29

- User confirmed Stage 28 verification passed and approved continuing the refactor with a larger step.
- Added `Assets/Scripts/Shared/MapEventRewardService.cs` to own map-event immediate rewards and encounter-victory reward queuing for loot, relics, and blueprints.
- Added EditMode coverage in `Assets/Tests/EditMode/Editor/MapEventRewardServiceTests.cs` for immediate loot / relic / blueprint grants, queued encounter-victory loot rewards, and missing pending encounter handling.
- Migrated `RunGameManager` event built-in reward execution and deferred encounter reward queuing to `MapEventRewardService`.
- Removed duplicate event reward selection, reward delivery, and pending victory reward queueing helpers from `RunGameManager`, leaving it focused on event flow, UI updates, custom handlers, and encounter startup.
- Static checks completed: old reward helper method names no longer remain in `RunGameManager`, and `git diff --check` reports no formatting errors for touched code files.
- Pending verification: run `MapEventRewardServiceTests`, then rerun the map-event smoke path, merchant service tests, and the existing relic regression group in Unity Test Runner.
## 2026-04-27 - Relic System Refactor Stage 30

- User confirmed Stage 29 verification passed and approved continuing the map-event refactor.
- Added `Assets/Scripts/Shared/MapEventLootCostService.cs` to own map-event loot cost previewing, cached random loot discard selection, random loot discard execution, and specific loot discard execution.
- Added EditMode coverage in `Assets/Tests/EditMode/Editor/MapEventLootCostServiceTests.cs` for cached random discard preview, unavailable specific loot requirements, cached random discard consumption, and all-or-nothing specific loot validation.
- Migrated `RunGameManager.BuildEventOptionPreview` and loot-discard built-in effect execution to `MapEventLootCostService` while preserving the preview cache semantics.
- Removed duplicate random loot discard preview and loot discard execution helpers from `RunGameManager`.
- Static checks completed: old loot-discard helper method names no longer remain in `RunGameManager`, and `git diff --check` reports no formatting errors for touched code files.
- Pending verification: run `MapEventLootCostServiceTests`, rerun `MapEventRewardServiceTests`, map-event option smoke testing, and the existing relic regression group in Unity Test Runner.
## 2026-04-27 - Relic System Refactor Stage 31

- User confirmed Stage 30 verification passed and approved continuing the map-event refactor.
- Added `Assets/Scripts/Shared/MapEventEncounterService.cs` to own event encounter group selection, runtime event enemy-group creation, and `BattleEncounterData` construction.
- Added EditMode coverage in `Assets/Tests/EditMode/Editor/MapEventEncounterServiceTests.cs` for configured enemy-group encounters, random enemy-list runtime groups, living party member cloning, relic / calamity cloning, and invalid encounter configuration failure.
- Migrated `RunGameManager.TryPrepareEventEncounter` to delegate through `MapEventEncounterService`, preserving source scene, target scene, event tile danger/coord, encounter seed salt, and pending encounter assignment.
- Removed duplicate event encounter enemy-group construction and encounter data population helpers from `RunGameManager`.
- Static checks completed: old event encounter helper method names no longer remain in `RunGameManager`, and `git diff --check` reports no formatting errors for touched code files.
- Pending verification: run `MapEventEncounterServiceTests`, rerun `MapEventLootCostServiceTests`, `MapEventRewardServiceTests`, map-event encounter smoke testing, and the existing relic regression group in Unity Test Runner.
## 2026-04-27 - Remaining Refactor Roadmap

- Priority 1: Extract map-event built-in effect execution from `RunGameManager` into a focused service, tentatively `MapEventBuiltInEffectExecutorService`. This should own built-in effect ordering, money / experience / HP / action-point effects, reward delegation, loot-cost delegation, and encounter preparation delegation while `RunGameManager` keeps UI flow and pending encounter startup.
- Priority 2: Extract map movement and action-point spending rules into a focused movement service. This should cover move-cost calculation, relic-based move-cost bypass checks such as visited-tile cost ignoring, overflow action-point spending, cycle exhaustion checks, and move result messaging.
- Priority 3: Extract normal map battle encounter construction from `RunGameManager`. Event encounters now use `MapEventEncounterService`; regular battle, elite, boss, and tutorial battle entry should follow the same service-oriented pattern for `BattleEncounterData` creation and enemy-group resolution.
- Priority 4: Extract run bootstrap logic. `StartRun`, `StartTutorialRun`, tutorial board generation, initial party construction, and starting inventory application should move toward `RunBootstrapService` / `TutorialRunBootstrapService` so NewGame, Tutorial, DemoTest, and future entry points can reuse the same setup path.
- Priority 5: Extract save asset catalog collection into `RunSaveAssetCatalogBuilder`. The `BuildSaveAssetCatalog` and `Add*Assets` helper block is large, mostly pure traversal logic, and can be tested separately without scene dependencies.
- Ongoing cleanup: keep reducing `RunGameManager` toward scene orchestration only: manager creation, UI callbacks, scene transitions, and high-level run flow. New business-rule services should receive `RunState`, tile data, effect data, config SOs, and deterministic RNG/seed inputs explicitly where possible.
- Verification expectation for each stage: add or extend EditMode tests for the extracted service first, migrate only the matching `RunGameManager` slice, run `git diff --check`, and ask the user to run the new service test plus the relevant existing regression group in Unity Test Runner.
## 2026-04-27 - Relic System Refactor Stage 32

- User approved collecting the next five roadmap items in one faster refactor pass.
- Added `Assets/Scripts/Shared/MapEventBuiltInEffectExecutorService.cs` to own map-event built-in effect ordering, money / experience / HP / action-point effects, reward delegation, loot-cost delegation, and event encounter preparation delegation.
- Added `Assets/Scripts/Shared/MapMovementService.cs` to own map move-cost rules, relic-based move-cost bypass prechecks, overflow action-point spending, cycle-exhaustion checks, and move execution flag handling.
- Added `Assets/Scripts/Shared/MapBattleEncounterService.cs` to own normal map battle encounter construction and hostile tile enemy-group selection / assignment.
- Added `Assets/Scripts/Shared/RunBootstrapService.cs` to own tutorial board generation, tutorial preview metadata, action-point limit resolution, starting party construction, and starting inventory application.
- Added `Assets/Scripts/Shared/RunSaveAssetCatalogBuilder.cs` to own save asset catalog traversal for starting assets, rosters, events, merchant catalogs, calamities, run state, and board state.
- Added `Assets/Tests/EditMode/Editor/RunServiceRefactorTests.cs` with coverage for tutorial board generation, movement cost rules, built-in event money/AP effects, and battle encounter inventory cloning.
- Migrated `RunGameManager` to delegate to the new services and removed the large duplicated blocks for event built-in execution, event XP application, movement rule execution, map battle data construction, bootstrap helpers, and save catalog traversal.
- Static / compile checks completed: Unity script refresh reports no console errors, and `git diff --check` reports no formatting errors for touched code files.
- Verification completed: `RunServiceRefactorTests` passed 4/4; `MapEventEncounterServiceTests`, `MapEventLootCostServiceTests`, and `MapEventRewardServiceTests` passed 12/12; merchant / relic regression group passed 72/72.
- Full EditMode note: full suite currently has one pre-existing or unrelated failure in `EnemyTodoTests.NextAttackSkillPowerBonus_PreviewsAttackRangeAndConsumesWhenAttackSkillIsActivated` with expected 3 but actual 1. The isolated failure is outside the touched Run / map-event / merchant / relic files and reproduces when run alone.
- Next recommended step: either fix the isolated `EnemyTodoTests` failure, or continue reducing `RunGameManager` scene-facing responsibilities by extracting calamity presentation / selection flow and map event draw assignment.

## 2026-04-27 - Relic System Refactor Stage 33

- Full EditMode baseline after the isolated enemy skill test fix passed 155/155 before new refactor work started.
- Added `Assets/Scripts/Shared/CalamitySelectionService.cs` to own calamity exclusion-set construction, pending calamity choice rolling, presented-choice marking, and chosen calamity application.
- Added `Assets/Scripts/Shared/MapEventAssignmentService.cs` to own map event tile assignment, valid event filtering, drawn-event exclusion, deterministic tile-seeded selection, and drawn-event marking.
- Added `Assets/Tests/EditMode/Editor/RunFlowAssignmentServiceTests.cs` with coverage for calamity exclusion / marking, chosen calamity application, event assignment / marking, and no-overwrite / non-event guard behavior.
- Migrated `RunGameManager` calamity choice rolling / marking and event tile assignment to the new services, keeping overlay presentation, HUD messaging, custom calamity handlers, relic dispatch, and scene-facing flow in `RunGameManager`.
- Static checks completed: old local `RollPendingCalamityChoices` and event filtering / marking blocks no longer remain in `RunGameManager`, and `git diff --check` reports no formatting errors for touched code files.
- Verification completed: `RunFlowAssignmentServiceTests` passed 4/4; `RunServiceRefactorTests`, `MapEventEncounterServiceTests`, `MapEventLootCostServiceTests`, and `MapEventRewardServiceTests` passed 16/16; full EditMode passed 159/159; Unity console reports no errors.
- Next recommended step: continue reducing `RunGameManager` scene-facing load by extracting save / restore runtime-state reset helpers and pending calamity deferral flags, or move to the higher-risk `BattleButton` combat resolution pipeline after a checkpoint commit.

## 2026-04-27 - Relic System Refactor Stage 34

- Continued low-risk `RunGameManager` reduction after Stage 33.
- Added `Assets/Scripts/Shared/RunSceneFlowState.cs` to centralize transient map-run flow state: pending calamity tiers, calamity presentation deferral flags, rest action-point restore suppression, and pending event encounter data.
- Added `Assets/Tests/EditMode/Editor/RunSceneFlowStateTests.cs` using TDD; the test first failed on missing `RunSceneFlowState`, then passed after implementation.
- Migrated `RunGameManager` to use `RunSceneFlowState` for new-run / tutorial / restore resets, pending calamity tier restoration, movement-cycle flag capture / restore, event encounter handoff, map / rest snapshot saves, and calamity presentation deferral checks.
- This stage keeps UI presentation, scene transitions, HUD messaging, and gameplay side effects in `RunGameManager`; it only centralizes transient flow state ownership.
- Static checks completed: old direct fields `pendingCalamityTiers`, `deferPendingCalamityUntilMapResume`, `deferPendingCalamityUntilOverlayClosed`, `suppressPendingCalamityOnNextRestActionPointRestore`, and `pendingEventEncounterData` no longer remain as manager-owned fields, and `git diff --check` reports no formatting errors for touched code files.
- Verification completed: `RunSceneFlowStateTests` passed 3/3; run / map / event service regression group passed 23/23; full EditMode passed 162/162; Unity console reports no errors.
- Next recommended step: checkpoint the current Run/Map refactor work before entering `BattleButton`, or do one final low-risk pass to extract map tile labels / HUD presentation text helpers if desired.

## 2026-04-27 - Relic System Refactor Stage 35

- Completed one final low-risk `RunGameManager` presentation cleanup before entering higher-risk combat code.
- Added `Assets/Scripts/Shared/MapTilePresentationService.cs` to own map tile type labels, danger-band labels, and the unavailable enemy-group HUD message builder.
- Added `Assets/Tests/EditMode/Editor/MapTilePresentationServiceTests.cs` covering localized tile labels and combined unavailable enemy-group messaging.
- Migrated `RunGameManager` to call `MapTilePresentationService.BuildUnavailableEnemyGroupMessage` and removed the local tile / danger label switch helpers from the manager.
- Restored MCP package configuration in `Packages/manifest.json` and `Packages/packages-lock.json` after merge pollution removed `com.coplaydev.unity-mcp` and its lock entries.
- Static checks completed: `RunGameManager` no longer contains the local tile / danger label helper methods, and `git diff --check` reports no formatting errors for touched code / package files.
- Verification completed: `MapTilePresentationServiceTests`, `RunSceneFlowStateTests`, `RunServiceRefactorTests`, and `RunFlowAssignmentServiceTests` passed 13/13; full EditMode passed 164/164; Unity console reports no errors.
- Next recommended step: checkpoint the Run/Map refactor work, then begin a separate `BattleButton` combat-resolution pipeline refactor with narrower test-backed slices.

## 2026-04-27 - Relic System Refactor Stage 36

- Began the higher-risk `BattleButton` refactor with a narrow, behavior-preserving combat calculation slice.
- Added `Assets/Scripts/Shared/BattlePowerModifierService.cs` to own per-die power modifier evaluation for skill-use, clash, clash-win, and single-attack triggers.
- Added `Assets/Tests/EditMode/Editor/BattlePowerModifierServiceTests.cs` using TDD; the test first failed on missing `BattlePowerModifierService`, then passed after implementation.
- Migrated `BattleButton` power modifier call sites to `BattlePowerModifierService.EvaluatePowerModifier` and removed the duplicated power-modifier evaluation helpers from `BattleButton`.
- Deliberately left range modifier helpers and broader combat coroutine flow in `BattleButton` for a later, separate slice to keep this stage low-risk.
- Static checks completed: `git diff --check` reports no formatting errors for touched code files.
- Verification completed: `BattlePowerModifierServiceTests` and `BattleCombatActionBuilderTests` passed 8/8; full EditMode passed 169/169; Unity console reports no errors.
- Next recommended step: extract the conditional dice range modifier evaluation into a sibling service, then continue toward a unified clash / single-attack damage calculation pipeline.

## 2026-04-27 - Relic System Refactor Stage 37

- Continued the `BattleButton` combat calculation cleanup with a second narrow extraction.
- Added `Assets/Scripts/Shared/BattleDiceRangeModifierService.cs` to own conditional dice range modifier evaluation for enemy contextual dice previews and actual rolls.
- Added `Assets/Tests/EditMode/Editor/BattleDiceRangeModifierServiceTests.cs` using TDD; the test first failed on missing `BattleDiceRangeModifierService`, then passed after implementation.
- Migrated `BattleButton.ApplyEnemyContextualDiceRangeModifiers` to delegate to `BattleDiceRangeModifierService.ApplyRangeModifiers` and removed the duplicated range-modifier condition / status helper block from `BattleButton`.
- Kept remaining delayed-status and built-in-effect condition checks inside `BattleButton`; those should move in a later effect-pipeline slice rather than being mixed into this range-only refactor.
- Static checks completed: `git diff --check` reports no formatting errors for touched code files.
- Verification completed: `BattleDiceRangeModifierServiceTests`, `BattlePowerModifierServiceTests`, and `BattleCombatActionBuilderTests` passed 13/13; full EditMode passed 174/174; Unity console reports no errors.
- Next recommended step: extract skill power formatting / final-power clamping and roll input preparation into a small calculation service, then begin splitting clash and single-attack resolution into explicit damage pipeline objects.

## 2026-04-27 - Relic System Refactor Stage 38

- Consolidated shared speed / fixed-value condition evaluation used by battle modifier logic.
- Added `Assets/Scripts/Shared/BattleModifierConditionService.cs` to own `PowerModifierCondition` speed predicates, including inclusive speed-difference handling and the legacy zero-as-one fallback.
- Added `Assets/Tests/EditMode/Editor/BattleModifierConditionServiceTests.cs` using TDD; the test first failed on missing `BattleModifierConditionService`, then passed after implementation.
- Migrated `BattlePowerModifierService`, `BattleDiceRangeModifierService`, and the remaining delayed-status / built-in-effect condition checks in `BattleButton` to call `BattleModifierConditionService.IsConditionMet`.
- Removed duplicated local speed-condition implementations from the two modifier services and `BattleButton`.
- Static checks completed: `git diff --check` reports no formatting errors for touched code files.
- Verification completed: `BattleModifierConditionServiceTests`, `BattleDiceRangeModifierServiceTests`, and `BattlePowerModifierServiceTests` passed 13/13; full EditMode passed 177/177; Unity console reports no errors.
- Next recommended step: extract final power calculation / display formatting and start separating clash and single-attack resolution into testable calculation units before moving presentation coroutines.

## 2026-04-27 - Relic System Refactor Stage 39

- Extracted final power calculation and battle-log power formatting from `BattleButton`.
- Added `Assets/Scripts/Shared/BattlePowerCalculationService.cs` to own final power clamping and signed power-bonus display text.
- Added `Assets/Tests/EditMode/Editor/BattlePowerCalculationServiceTests.cs` using TDD; the test first failed on missing `BattlePowerCalculationService`, then passed after implementation.
- Migrated `BattleButton` clash and single-attack power calculations / log formatting to `BattlePowerCalculationService` and removed the local helper methods.
- Verification completed: targeted battle calculation / modifier / action tests passed 19/19; full EditMode passed 180/180; Unity console reports no errors.

## 2026-04-27 - Relic System Refactor Stage 40

- Extracted dice range normalization, relic range bonus application, and random power rolling from `BattleButton`.
- Added `Assets/Scripts/Shared/BattleDiceRollService.cs` to own range bonus application, range normalization, and inclusive dice-power rolling through Unity's exclusive upper-bound random API.
- Added `Assets/Tests/EditMode/Editor/BattleDiceRollServiceTests.cs` using TDD; the test first failed on missing `BattleDiceRollService`, then passed after implementation.
- Migrated `BattleButton` player relic dice range preparation, target min-power relic modification, and dice rolling to `BattleDiceRollService`; removed the local range / roll helper methods.
- Static checks completed: `git diff --check` reports no formatting errors for touched code files.
- Verification completed: battle dice / power / modifier / action tests passed 22/22; full EditMode passed 183/183; Unity console reports no errors.
- Next recommended step: begin extracting clash result calculation into a pure service that computes winner, follow-up base power, damage, and defense-break outcomes before presentation and side effects run.

## 2026-04-27 - Relic System Refactor Stage 41

- Stabilized the battle status-effect condition extraction that was present in the current working tree while continuing the `BattleButton` calculation cleanup.
- Restored `StatusEffectEntry` condition fields in `Assets/Data/Enemies/Skill/EnemySkill_SO.cs` so `BattleStatusEffectConditionService` and status-effect sanitization compile consistently.
- Updated direct hit status-effect calls in `BattleButton` to pass the new `conditionalOnly` split explicitly, preserving the immediate conditional / delayed non-conditional status-effect routing.
- Verification completed: battle dice / power / modifier / action tests passed 23/23; full EditMode passed 186/186; Unity console reports no errors.
- Next recommended step: extract clash result calculation into a pure service once the current calculation-service checkpoint is reviewed, because that next slice will touch more of the clash branch structure.

## 2026-04-27 - Relic System Refactor Stage 42

- Extracted the first pure clash-resolution calculation slice from `BattleButton`.
- Added `Assets/Scripts/Shared/BattleClashResultService.cs` with `BattleClashOutcome` and `BattleClashResult` to compute presentation winner, clash-win trigger side, attack-vs-defense break outcomes, damage power, clash difference, and ally defense energy-restore amount.
- Added `Assets/Tests/EditMode/Editor/BattleClashResultServiceTests.cs` using TDD; the test first failed on missing result types / service, then passed after implementation.
- Migrated the clash branch in `BattleButton` to create a `BattleClashResult` before existing animation, log, and side-effect branches execute. Presentation, logging, hit application, relic triggers, and status/effect dispatch remain in `BattleButton` for now.
- Static checks completed: `git diff --check` reports no formatting errors for touched code files.
- Verification completed: battle clash / dice / power / modifier / action tests passed 29/29; full EditMode passed 193/193; Unity console reports no errors.
- Next recommended step: extract clash side-effect dispatch into a small coordinator or split attack-vs-attack / attack-vs-defense branch handlers, now that the pure result object exists.

## 2026-04-27 - Relic System Refactor Stage 43

- Extracted single-attack per-die result calculation from `BattleButton`.
- Added `Assets/Scripts/Shared/BattleSingleAttackResultService.cs` with `BattleSingleAttackResult` to compute final power, display power modifier, hit power, hit power modifier, defense-only handling, and formatted power text for one-sided attack dice.
- Added `Assets/Tests/EditMode/Editor/BattleSingleAttackResultServiceTests.cs` using TDD; the test first failed on missing result types / service, then passed after implementation.
- Migrated remaining ally clash dice, remaining enemy clash dice, full ally one-sided attack dice, and full enemy one-sided attack dice to use `BattleSingleAttackResultService` for final power / display / hit inputs while leaving animation, logging, hit application, relics, and status/effect dispatch in `BattleButton`.
- Static checks completed: `git diff --check` reports no formatting errors for touched code files.
- Verification completed: battle single-attack / clash / dice / power / modifier / action tests passed 32/32; full EditMode passed 196/196; Unity console reports no errors.
- Next recommended step: extract one-sided attack side-effect dispatch or build a reusable dice input preparation service to remove the repeated ally/enemy power-modifier collection blocks.

## 2026-04-27 - Relic System Refactor Stage 44

- Extracted shared dice power preparation from repeated `BattleButton` clash / one-sided attack calculations.
- Added `Assets/Scripts/Shared/BattleDicePowerPreparationService.cs` with `BattleDicePowerPreparation` to combine relic, skill-use, and action-specific power bonuses, then compute final power, follow-up base power, display modifier, and formatted power text.
- Added `Assets/Tests/EditMode/Editor/BattleDicePowerPreparationServiceTests.cs` using TDD; the test first failed on missing result types / service, then passed after implementation.
- Migrated the clash branch and the four one-sided attack dice branches to use `BattleDicePowerPreparationService` for final power / follow-up / display calculations while leaving modifier collection, animation, hit application, relics, and status/effect dispatch in `BattleButton`.
- Verification note: the first targeted test run was blocked because Unity was in Play Mode; after stopping Play Mode, the same targeted group passed normally.
- Static checks completed: `git diff --check` reports no formatting errors for touched code files.
- Verification completed: battle dice power preparation / single-attack / clash / dice / power / modifier / action tests passed 35/35; full EditMode passed 199/199; Unity console reports no errors after the final run.
- Next recommended step: split modifier collection into ally/enemy dice input builder methods or extract one-sided attack side-effect dispatch now that the power result and preparation layers are isolated.

## 2026-04-27 - Relic System Refactor Stage 45

- Extracted dice power modifier collection from repeated `BattleButton` direct calls to `BattlePowerModifierService.EvaluatePowerModifier`.
- Added `Assets/Scripts/Shared/BattleDicePowerModifierCollectionService.cs` to wrap skill-use, action-specific, and clash-win power modifier collection while still delegating the actual modifier evaluation to `BattlePowerModifierService`.
- Added `Assets/Tests/EditMode/Editor/BattleDicePowerModifierCollectionServiceTests.cs` using TDD; the test first failed on missing collection service, then passed after implementation.
- Migrated `BattleButton` clash, clash-win, remaining one-sided dice, and full one-sided attack power modifier collection to `BattleDicePowerModifierCollectionService`. `BattleButton` no longer calls `BattlePowerModifierService.EvaluatePowerModifier` directly.
- Debug note: one full-suite run briefly reported `BattleSkillInfoOverlayDescriptionTests.CompactSkillDetail_MergesConsumedStatusPowerModifierAndShowsCap`; rerunning the isolated test passed, and the subsequent full EditMode run passed, indicating stale Unity test/domain state rather than a current failure.
- Static checks completed: `git diff --check` reports no formatting errors for touched code files.
- Verification completed: battle modifier collection / dice power preparation / single-attack / clash / dice / power / action tests passed 39/39; isolated overlay description regression passed 1/1; full EditMode passed 204/204; Unity console reports no errors after the final run.
- Next recommended step: extract ally/enemy dice input builder methods or begin moving one-sided attack side-effect dispatch out of `BattleButton` now that modifier collection and power preparation are isolated.

## 2026-04-27 - Relic System Refactor Stage 46

- 收窄了 `BattleButton` 中单方面攻击日志的重复威力格式化逻辑。
- 将剩余友方余骰、敌方余骰、完整友方单方面攻击、完整敌方单方面攻击日志中的威力展示改为复用 `BattleSingleAttackResult.FormattedPower`。
- 保留拼点日志中的 `BattlePowerCalculationService.FormatPowerWithBonus` 直接调用，后续应在拆分 clash 日志 / 结果展示时一起处理，避免一次切片跨太多战斗分支。
- 静态检查完成：`git diff --check -- Assets/Scripts/BattleScene/Controller/BattleButton.cs` 无格式错误，仅保留 Unity/Windows 行尾提示。
- 验证完成：相关战斗计算测试通过 19/19；全量 EditMode 测试通过 204/204；Unity Console 最终无 error。
- 下一步建议：继续抽取 ally/enemy dice input builder，或者把单方面攻击的动画、命中、遗物、状态触发调度拆到单独协调器中，让 `BattleButton` 只负责流程编排。

## 2026-04-27 - Relic System Refactor Stage 47

- 提取了单方面攻击日志文案组装逻辑，继续收窄 `BattleButton` 的字符串拼接职责。
- 新增 `Assets/Scripts/Shared/BattleSingleAttackLogService.cs`，负责生成单方面攻击命中日志与防御骰未结算日志。
- 新增 `Assets/Tests/EditMode/Editor/BattleSingleAttackLogServiceTests.cs`，覆盖余骰上下文、完整单方面攻击、目标剩余生命值展示与防御骰提示。
- 按 TDD 流程确认 RED：测试最初因缺少 `BattleSingleAttackLogService` 产生 3 个编译错误；实现服务并接入后转为通过。
- `BattleButton` 中 6 处单方面攻击日志已改为调用 `BattleSingleAttackLogService`，保留动画、命中、遗物、状态触发在原流程中。
- 静态检查完成：`git diff --check` 对本轮相关文件无格式错误，仅保留 Unity/Windows 行尾提示。
- 验证完成：相关战斗计算与日志测试通过 22/22；全量 EditMode 测试通过 207/207；Unity Console 最终无 error。
- 下一步建议：抽取单方面攻击 dice input builder，集中准备 actor/target、base power、status modifier、skill-use bonus、action bonus 与 result，进一步减少 ally/enemy 分支重复代码。

## 2026-04-27 - Relic System Refactor Stage 48

- 提取了单方面攻击每段骰子的结果准备逻辑，减少 `BattleButton` 中重复的 bonus 合并、power preparation 与 attack result 组装。
- 新增 `Assets/Scripts/Shared/BattleSingleAttackDiceResolutionService.cs`，负责把 base power、状态展示修正、使用时威力、单方面攻击时威力与防御骰标记合并为 `BattleSingleAttackDiceResolution`。
- 新增 `Assets/Tests/EditMode/Editor/BattleSingleAttackDiceResolutionServiceTests.cs`，覆盖 skill-use/action bonus 合并、防御骰不造成伤害但保留展示威力的规则。
- 按 TDD 流程确认 RED：测试最初因缺少 `BattleSingleAttackDiceResolution` 与服务产生 4 个编译错误；实现服务并接入后转为通过。
- `BattleButton` 中 4 个单方面攻击分支已改为调用 `BattleSingleAttackDiceResolutionService.Resolve`，保留技能/遗物 bonus 的来源收集在原流程中。
- 静态检查完成：`git diff --check` 对本轮相关文件无格式错误，仅保留 Unity/Windows 行尾提示。
- 验证完成：相关战斗计算、日志、dice resolution 测试通过 24/24；全量 EditMode 测试通过 209/209；Unity Console 最终无 error。
- 下一步建议：继续把 actor/target 差异收敛为 ally/enemy single-attack dice input builder，或者开始拆 one-sided attack side-effect dispatch，把动画、命中、遗物、状态触发从 `BattleButton` 中移出。

## 2026-04-27 - Relic System Refactor Stage 49

- 继续收敛单方面攻击骰子输入构建逻辑，把 `BattleButton` 中重复的 skill-use bonus 与 single-attack bonus 收集移入 `BattleSingleAttackDiceResolutionService`。
- 为 `BattleSingleAttackDiceResolutionService` 新增 `ResolveWithModifiers`，统一接收技能、骰子序号、基础威力、状态展示修正、速度、状态层数查询、目标技能与遗物威力修正。
- 补充 `ResolveWithModifiers_CollectsSkillUseRelicAndSingleAttackBonuses` 测试，覆盖【使用时】、【单方面攻击时】、遗物威力修正合并，并确认不会误应用【拼点胜利时】修正。
- 按 TDD 流程确认 RED：新增测试最初因缺少 `ResolveWithModifiers` 编译失败；实现接口并接入后转为通过。
- `BattleButton` 中 4 个单方面攻击分支已改为调用 `ResolveWithModifiers`，不再直接调用 `BattleDicePowerModifierCollectionService.GetSkillUseBonus/GetActionBonus` 处理单方面攻击威力。
- 静态检查完成：`git diff --check` 对本轮相关文件无格式错误。
- 验证完成：相关战斗计算测试通过 25/25；全量 EditMode 测试通过 210/210；Unity Console 最终无 error。
- 下一步建议：抽取单方面攻击 presentation/hit/log 调度结构，或先处理 `BattleButton` 中拼点日志仍直接格式化威力的遗留点。

## 2026-04-27 - Relic System Refactor Stage 50

- 提取了拼点日志中剩余的威力格式化职责，继续减少 `BattleButton` 中直接拼接战斗日志与调用 `BattlePowerCalculationService.FormatPowerWithBonus` 的位置。
- 新增 `Assets/Scripts/Shared/BattleClashLogService.cs`，负责拼点段日志、友方攻击胜利追击日志、敌方攻击胜利追击日志。
- 新增 `Assets/Tests/EditMode/Editor/BattleClashLogServiceTests.cs`，覆盖拼点双方威力展示、友方追击威力展示、敌方追击威力展示。
- 按 TDD 流程确认 RED：新增测试最初依赖缺失的 `BattleClashLogService`；实现服务并接入 `BattleButton` 后，项目 C# 编译通过。
- `BattleButton` 中剩余 3 处 `BattlePowerCalculationService.FormatPowerWithBonus` 拼点日志调用已改为 `BattleClashLogService`。
- 静态检查完成：`git diff --check` 对本轮相关文件无格式错误。
- 本地编译验证完成：`dotnet build Assembly-CSharp.csproj` 0 error；`dotnet build Assembly-CSharp-Editor.csproj` 0 error。
- Unity MCP 当前未注册到 HTTP instance，`mcpforunity://instances` 返回 0 个实例；因此本阶段的 EditMode 测试需要在 MCP session 恢复后补跑。
- 下一步建议：MCP 恢复后优先补跑 `BattleClashLogServiceTests` 与全量 EditMode；若继续纯代码重构，应限制在已有本地编译能覆盖的服务抽取范围。

## 2026-04-27 - Relic System Refactor Stage 50 Verification Update

- 已恢复 Unity MCP HTTP instance，当前实例为 `MobileGame@014fa41e060f5012`。
- MCP 恢复方式：将 `Packages/manifest.json` 与 `Packages/packages-lock.json` 中的 `com.coplaydev.unity-mcp` 从 GitHub 依赖改为本机已有 package cache 的 `file:` 依赖，避免 PackageManager 因网络 reset 无法解析包。
- Unity refresh / compile 完成，Unity Console 最终无 error。
- Stage 50 定向验证完成：`BattleClashLogServiceTests`、`BattleClashResultServiceTests`、`BattlePowerCalculationServiceTests`、`BattleSingleAttackDiceResolutionServiceTests`、`BattleSingleAttackLogServiceTests`、`BattleCombatActionBuilderTests` 通过 21/21。
- 全量 EditMode 验证完成：213/213 通过。
- Stage 50 现在可以视为完成；下一步建议进入 clash side-effect dispatch 拆分，或先把 attack-vs-attack / attack-vs-defense 分支处理器从 `BattleButton` 中抽出。

## 2026-04-28 - Relic System Refactor Stage 51

- 继续收窄 `BattleButton` 的拼点日志职责，先处理纯文本输出，避免本阶段触碰战斗结算时机、SO 枚举顺序、Prefab 或资产配置。
- 扩展 `Assets/Scripts/Shared/BattleClashLogService.cs`，新增攻击平局、攻防拼点、防攻拼点、双方防御拼点的结果日志格式化方法。
- 扩展 `Assets/Tests/EditMode/Editor/BattleClashLogServiceTests.cs`，覆盖平局、破防、防御胜利、防御平手与双方防御日志，锁定现有中文战斗日志文案。
- `Assets/Scripts/BattleScene/Controller/BattleButton.cs` 中对应的拼点后续日志已改为调用 `BattleClashLogService`；动画、命中、遗物、状态触发与能量恢复仍保留在原流程中。
- 验证限制：当前 shell 环境没有 `dotnet`、`unity-editor`、`csc`/`mcs`，无法在本轮直接执行 C# 编译或 Unity EditMode 测试。
- 静态检查完成：`git diff --check -- Assets/Scripts/Shared/BattleClashLogService.cs Assets/Tests/EditMode/Editor/BattleClashLogServiceTests.cs Assets/Scripts/BattleScene/Controller/BattleButton.cs` 无格式错误。
- 待补验证：在 Unity 环境恢复后优先运行 `BattleClashLogServiceTests`，再跑 Stage 50 的定向战斗计算/日志测试组与全量 EditMode。

## 2026-04-29 - Relic System Refactor Stage 52

- 确认 Unity MCP 当前可用，`read_console`、脚本刷新与 EditMode 测试都能执行；Console 中仅保留 MCP WebSocket 初始化警告与 TMP 默认字体缺字警告，无脚本错误。
- 提取拼点副作用计划计算，新增 `Assets/Scripts/Shared/BattleClashImpactPlanService.cs`，用纯数据描述拼点后应执行的友方命中、敌方命中、Miss、0 伤显示、守门/断角敌人效果、防御胜利遗物与防御能量恢复。
- 新增 `Assets/Tests/EditMode/Editor/BattleClashImpactPlanServiceTests.cs`，覆盖攻击胜利、敌方攻击胜利、攻击平局、敌方防御胜利、友方防御胜利、双方防御敌方胜利等关键分支。
- `BattleButton` 的拼点分支改为先生成 `BattleClashImpactPlan`，再通过 `ResolveClashImpactPlan` 集中执行副作用；动画、实际命中函数、遗物触发与状态触发仍保留在 `BattleButton`，避免一次性搬迁高风险流程。
- 修复结算遗物三选一数据的 Unity 序列化环：`BattleSettlementLootEntry.relicChoiceData` 标记为 `[NonSerialized]`，清除了 `BattleSettlementRelicChoiceData.choices` 的 serialization depth error。
- 静态检查完成：`git diff --check -- Assets/Scripts/Shared/BattleClashImpactPlanService.cs Assets/Tests/EditMode/Editor/BattleClashImpactPlanServiceTests.cs Assets/Scripts/BattleScene/Controller/BattleButton.cs` 无格式错误。
- 验证完成：定向 EditMode 通过 26/26；全量 EditMode 通过 224/224；Unity Console 无 error。
- 下一步建议：继续把 `BattleButton` 中拼点的日志构建与 hitFx 选择整理为小型 presentation/log plan，随后再考虑拆 one-sided attack side-effect dispatch。

## 2026-04-29 - Relic System Refactor Stage 53

- 继续收窄 `BattleButton` 拼点流程中的 presentation/log 分支，不移动动画、命中、遗物或状态副作用。
- 新增 `Assets/Scripts/Shared/BattleClashPresentationPlanService.cs`，用纯数据把 `BattleClashOutcome` 映射为 hitFx 来源、拼点后日志类型、是否破防。
- 新增 `Assets/Tests/EditMode/Editor/BattleClashPresentationPlanServiceTests.cs`，按 TDD 验证 RED：最初因缺少 `BattleClashPresentationPlanService`、`BattleClashPresentationPlan`、`BattleClashHitFxSource`、`BattleClashPostLogType` 编译失败；实现后转为通过。
- `BattleButton` 拼点循环改为通过 `BattleClashPresentationPlanService.Resolve` 选择 hitFx 和日志类型，并用 `BuildClashPostLog` 集中生成后续日志，删除循环内 attack/defense 分支里的重复日志判断。
- 静态检查完成：`git diff --check -- Assets/Scripts/Shared/BattleClashPresentationPlanService.cs Assets/Tests/EditMode/Editor/BattleClashPresentationPlanServiceTests.cs Assets/Scripts/BattleScene/Controller/BattleButton.cs` 无格式错误。
- 验证完成：定向拼点 EditMode 通过 24/24；全量 EditMode 通过 229/229；Unity Console 无 error。
- 下一步建议：开始抽取 one-sided attack side-effect dispatch，或继续把 `BattleButton` 中单方面攻击的 presentation/hit/log 调度整理成计划对象。

## 2026-04-29 - Relic System Refactor Stage 54

- 开始收窄 `BattleButton` 单方面攻击展示/命中/回位调度，保持实际动画、命中、遗物与状态副作用仍在原流程中。
- 新增 `Assets/Scripts/Shared/BattleSingleAttackPresentationPlanService.cs`，用纯数据描述每段单方面攻击骰是否播放展示、是否执行命中回调、是否前进、是否回位，以及后续日志类型。
- 新增 `Assets/Tests/EditMode/Editor/BattleSingleAttackPresentationPlanServiceTests.cs`，按 TDD 验证 RED：最初因缺少 `BattleSingleAttackPresentationPlanService`、`BattleSingleAttackPresentationPlan`、`BattleSingleAttackPostLogType` 编译失败；实现后转为通过。
- `BattleButton` 的剩余友方骰、剩余敌方骰、完整友方单方面攻击、完整敌方单方面攻击都接入 `BattleSingleAttackPresentationPlanService.Resolve`，统一由计划对象决定 presentation 和 hit callback。
- 注意：完整单方面攻击分支保留了原有 `forceHitCallback` 行为，避免本轮重构顺手改变 per-die 防御骰在该流程中的历史结算表现。
- 静态检查完成：`git diff --check -- Assets/Scripts/Shared/BattleSingleAttackPresentationPlanService.cs Assets/Tests/EditMode/Editor/BattleSingleAttackPresentationPlanServiceTests.cs Assets/Scripts/BattleScene/Controller/BattleButton.cs` 无格式错误。
- 验证完成：定向单方面攻击 EditMode 通过 13/13；全量 EditMode 通过 233/233；Unity Console 无 error。
- 下一步建议：继续把单方面攻击日志生成切到 `BuildSingleAttackPostLog`，但需要优先处理当前历史文案编码混乱，避免 patch 误伤字符串。

## 2026-04-29 - Relic System Refactor Stage 55

- 继续收窄 `BattleButton` 的单方面攻击流程，先不触碰历史乱码战斗日志文案，避免在重构切片里引入字符串行为变化。
- 新增 `SingleAttackDiceInput` 私有结构，并抽出 `PrepareAllySingleAttackDice` / `PrepareEnemySingleAttackDice`，统一准备单方面攻击每段骰子的展示范围、最终结算结果与骰子表现配置。
- `ResolveRemainingAllyClashDice`、`ResolveRemainingEnemyClashDice`、`ResolveAllySingleAttackAction`、`ResolveEnemySingleAttackAction` 已改为复用上述 helper，删除四处重复的范围修正、状态威力修正、遗物修正与骰子表现准备代码。
- 完整单方面攻击的命中日志已接入既有 `BuildSingleAttackPostLog`；剩余骰日志因为现有 context 文案存在历史编码问题，本轮保留直接调用，后续应先统一日志文案编码再收口。
- 验证完成：Unity 脚本刷新/编译后 Console 无 error；单方面攻击相关 EditMode 测试通过 13/13；全量 EditMode 测试通过 235/235。
- 下一步建议：优先清理 `BattleButton` 中剩余骰日志的编码问题并接入 `BuildSingleAttackPostLog`，随后再抽单方面攻击 side-effect dispatch，把动画、命中、遗物与状态触发进一步收口。

## 2026-04-29 - Relic System Refactor Stage 56

- 继续收窄 `BattleButton` 的单方面攻击展示与回位流程，仍然避开历史乱码日志文案，避免重构切片引入文本行为变化。
- 新增 `PlayResolvedSingleAttackPresentation`，统一从 `SingleAttackDiceInput` 与 `BattleSingleAttackPresentationPlan` 组装 `PlaySingleAttackPresentation` 参数，四个单方面攻击分支不再重复传递最终威力、展示修正、骰子表现和回调开关。
- 新增 `ReturnSingleAttackActorsToOriginIfNeeded`，统一处理单方面攻击提前结束与流程结束后的 actor 回位；各分支保留原有回位条件。
- 静态检查完成：`git diff --check -- Assets/Scripts/BattleScene/Controller/BattleButton.cs` 无格式错误，仅保留 Unity/Windows 行尾提示。
- 验证完成：Unity 脚本刷新/编译后 Console 无 error；单方面攻击相关 EditMode 测试通过 13/13；全量 EditMode 测试通过 235/235。
- 下一步建议：开始拆 `BattleButton` 中单方面攻击的 hit/status/relic side-effect dispatch，或先统一修复剩余骰日志的编码和 `BuildSingleAttackPostLog` 接入。

## 2026-04-29 - Relic System Refactor Stage 57

- 继续收窄 `BattleButton` 的单方面攻击命中回调构造，保持实际命中函数、触发时机和命中参数不变。
- 新增 `BuildAllySingleAttackHitAction` / `BuildEnemySingleAttackHitAction`，统一构造传给 `PlayResolvedSingleAttackPresentation` 的命中回调，四个单方面攻击分支不再直接内联 `ApplyAllyHit` / `ApplyEnemyHit` lambda。
- 本轮仍未触碰剩余骰日志中的历史乱码 context 文案，避免在流程重构中混入文本修复。
- 静态检查完成：`git diff --check -- Assets/Scripts/BattleScene/Controller/BattleButton.cs` 无格式错误，仅保留 Unity/Windows 行尾提示。
- 验证完成：Unity 脚本刷新/编译后 Console 无 error；单方面攻击相关 EditMode 测试通过 13/13；全量 EditMode 测试通过 235/235。
- 下一步建议：开始抽 `ApplyAllyTriggeredEffects` / `ApplyEnemyTriggeredEffects` 的单方面攻击使用时调度，或先集中处理剩余骰日志编码并统一接入 `BuildSingleAttackPostLog`。

## 2026-04-29 - Relic System Refactor Stage 58

- 继续收窄 `BattleButton` 的单方面攻击使用时词条触发调度，保持 `OnSkillUse` 触发类型、参数和调用时机不变。
- 新增 `ApplyAllySingleAttackSkillUseEffects` / `ApplyEnemySingleAttackSkillUseEffects`，统一包装单方面攻击和剩余骰分支中的 `ApplyAllyTriggeredEffects` / `ApplyEnemyTriggeredEffects` 调用。
- 主拼点流程中的双方 `OnSkillUse` 触发保持直接调用原函数，避免本轮切片扩大到 clash 分支。
- 静态检查完成：`git diff --check -- Assets/Scripts/BattleScene/Controller/BattleButton.cs` 无格式错误，仅保留 Unity/Windows 行尾提示。
- 验证完成：Unity 脚本刷新/编译后 Console 无 error；单方面攻击相关 EditMode 测试通过 13/13；全量 EditMode 测试通过 235/235。
- 下一步建议：继续抽单方面攻击日志构造剩余两处，但需要先处理历史乱码 context 文案；或者进入更实质的 side-effect dispatch 拆分，把命中后的遗物、状态、反馈调度从 `BattleButton` 迁出。

## 2026-04-29 - Relic System Refactor Stage 59

- 继续收窄 `BattleButton` 的命中后副作用调度，先在类内抽 helper，不改变实际伤害计算、0 伤害反馈或 Miss 分支。
- 新增 `ApplyAllySuccessfulHitEffects` / `ApplyEnemySuccessfulHitEffects`，分别承接友方命中后遗物、击杀遗物、`OnSingleAttack` / `OnClashWin` / `OnHit` 即时词条、响应式受击状态和状态施加调度。
- `ApplyAllyHit` / `ApplyEnemyHit` 现在只负责前置校验、伤害修正、`TakeDamage`、0 伤害反馈、成功命中分派和 Miss 分支，职责边界更清楚。
- 静态检查完成：`git diff --check -- Assets/Scripts/BattleScene/Controller/BattleButton.cs` 无格式错误，仅保留 Unity/Windows 行尾提示。
- 验证完成：Unity 脚本刷新/编译后 Console 无 error；相关战斗 EditMode 测试通过 19/19；全量 EditMode 测试通过 235/235。
- 下一步建议：继续把 `ApplyAllySuccessfulHitEffects` / `ApplyEnemySuccessfulHitEffects` 抽成独立 hit-effect dispatcher 服务，或先补命中后遗物/状态触发顺序的 EditMode 覆盖，降低后续迁出风险。

## 2026-04-29 - Relic System Refactor Stage 60

- 继续收窄 `BattleButton` 的命中反馈职责，抽出命中造成 0 伤害时的重复弹字逻辑。
- 新增 `ShowZeroDamageHitFeedback`，统一包装 `battleFeedbackSystem.ShowDamageResultPopup` 的 0 伤害位置、阵营方向和伤害倍率显示参数。
- `ApplyAllyHit` / `ApplyEnemyHit` 中的 0 伤害反馈改为调用 helper；实际伤害修正、`TakeDamage`、成功命中副作用和 Miss 分支不变。
- 静态检查完成：`git diff --check -- Assets/Scripts/BattleScene/Controller/BattleButton.cs` 无格式错误，仅保留 Unity/Windows 行尾提示。
- 验证完成：Unity 脚本刷新/编译后 Console 无 error；相关战斗 EditMode 测试通过 19/19；全量 EditMode 测试通过 235/235。
- 下一步建议：继续拆 `ApplyAllyHit` / `ApplyEnemyHit` 的伤害修正输入与 Miss 分支调度，或者开始为命中后副作用顺序补测试后再迁出独立 dispatcher。

## 2026-04-29 - Relic System Refactor Stage 61

- 按低风险切片继续收窄 `BattleButton` 的命中流程，一次收口伤害修正输入和 Miss 分支调度。
- 新增 `ResolveAllyHitDamage` / `ResolveEnemyHitDamage`，分别包装友方输出伤害修正、敌方灾厄伤害修正、玩家受击遗物修正和单方面攻击受击遗物修正，保持原调用顺序不变。
- 新增 `ApplyAllyMissEffects` / `ApplyEnemyMissEffects`，统一包装命中造成 0 或以下伤害后的 `OnMiss` 触发调度。
- `ApplyAllyHit` / `ApplyEnemyHit` 现在进一步只保留流程编排：校验、计算伤害、`TakeDamage`、0 伤害反馈、成功命中分派和 Miss 分派。
- 静态检查完成：`git diff --check -- Assets/Scripts/BattleScene/Controller/BattleButton.cs` 无格式错误，仅保留 Unity/Windows 行尾提示。
- 验证完成：Unity 脚本刷新/编译后 Console 无 error；相关战斗 EditMode 测试通过 19/19；全量 EditMode 测试通过 235/235。
- 下一步建议：继续把命中流程抽成独立 hit resolution/dispatcher 服务前，先补一组 EditMode 测试锁定命中成功、Miss、0 伤害、单方面攻击受击遗物的触发顺序。

## 2026-04-29 - Relic System Refactor Stage 62

- 继续按低风险切片收窄 `BattleButton` 的成功命中副作用调度，把遗物处理与技能/状态触发处理再拆开一层。
- 新增 `ApplyAllySuccessfulHitRelics` / `ApplyEnemySuccessfulHitRelics`，分别承接友方命中遗物、击杀遗物与敌方单方面攻击命中时玩家受击遗物。
- 新增 `ApplyAllySuccessfulHitSkillEffects` / `ApplyEnemySuccessfulHitSkillEffects`，承接 `OnSingleAttack`、`OnClashWin`、`OnHit` 即时词条、响应式受击状态与状态施加调度。
- `ApplyAllySuccessfulHitEffects` / `ApplyEnemySuccessfulHitEffects` 现在只负责按原顺序串联遗物处理与技能/状态触发处理，行为顺序保持不变。
- 静态检查完成：`git diff --check -- Assets/Scripts/BattleScene/Controller/BattleButton.cs` 无格式错误，仅保留 Unity/Windows 行尾提示。
- 本地 C# 编译完成：`dotnet build Assembly-CSharp.csproj --no-restore` 0 error（保留既有未赋值 warning）；`dotnet build Assembly-CSharp-Editor.csproj --no-restore` 0 error。
- 验证限制：当前会话未暴露可用 Unity MCP 工具入口，因此本轮未能补跑 Unity EditMode；MCP 恢复后应优先跑相关战斗测试与全量 EditMode。
- 下一步建议：MCP 恢复后补跑测试；若继续低风险重构，可以把命中副作用拆层迁往独立 dispatcher 前，先增加覆盖触发顺序的 EditMode 测试。

## 2026-04-29 - Relic System Refactor Stage 63

- 继续按低风险切片收窄 `BattleButton` 的命中流程，把 `TakeDamage` 后的结果分派抽成 outcome helper。
- 新增 `ApplyAllyHitOutcome` / `ApplyEnemyHitOutcome`，统一处理 0 伤害反馈、成功命中副作用分派与 Miss 分派，保持原有顺序：0 伤害先弹字，正伤害走成功命中并 return，否则触发 Miss。
- `ApplyAllyHit` / `ApplyEnemyHit` 现在只负责前置校验、伤害修正、`TakeDamage` 与 outcome 分派，进一步接近纯流程编排。
- 静态检查完成：`git diff --check -- Assets/Scripts/BattleScene/Controller/BattleButton.cs` 无格式错误，仅保留 Unity/Windows 行尾提示。
- 本地 C# 编译完成：`dotnet build Assembly-CSharp.csproj --no-restore` 0 error（保留既有未赋值 warning）；`dotnet build Assembly-CSharp-Editor.csproj --no-restore` 0 error。
- 验证限制：当前会话未暴露可用 Unity MCP 工具入口，因此本轮未能补跑 Unity EditMode；MCP 恢复后应优先跑相关战斗测试与全量 EditMode。
- 下一步建议：在 MCP 恢复后补跑测试；继续重构时可以开始把命中流程迁向独立 resolution/dispatcher 服务，但迁出前最好先补触发顺序测试。

## 2026-04-29 - Relic System Refactor Stage 64

- 继续收窄 `BattleButton` 的命中结果分派，新增纯数据计划 `BattleHitOutcomePlan` 与 `BattleHitOutcomePlanService`，把 `dealtDamage` 映射为 0 伤害反馈、成功命中副作用、Miss 副作用三个明确布尔决策。
- `ApplyAllyHitOutcome` / `ApplyEnemyHitOutcome` 已接入 `BattleHitOutcomePlanService.Resolve`，原行为顺序保持不变：非正伤害先显示 0 伤害反馈，正伤害执行成功命中副作用并 return，非正伤害继续触发 Miss。
- 新增 `Assets/Tests/EditMode/Editor/BattleHitOutcomePlanServiceTests.cs`，覆盖正伤害、0 伤害与负伤害三类结果计划。注意：当前 Unity 生成的 csproj 尚未收录新测试文件，需 MCP/Unity 导入后实际运行。
- 暂时将 `BattleHitOutcomePlanService` 放在已被 csproj 收录的 Shared 源文件中，以便本地 C# 编译能覆盖生产代码；Unity MCP 恢复并重新生成项目后，可再拆成独立 `BattleHitOutcomePlanService.cs` 文件。
- 静态检查完成：`git diff --check -- Assets/Scripts/BattleScene/Controller/BattleButton.cs Assets/Scripts/Shared/BattleSingleAttackPresentationPlanService.cs Assets/Tests/EditMode/Editor/BattleHitOutcomePlanServiceTests.cs` 无格式错误，仅保留 Unity/Windows 行尾提示。
- 本地 C# 编译完成：`dotnet build Assembly-CSharp.csproj --no-restore` 0 error（保留既有未赋值 warning）；`dotnet build Assembly-CSharp-Editor.csproj --no-restore` 0 error。
- 验证限制：当前 Unity MCP 资源握手仍失败，返回 `Unexpected content type: missing-content-type`；因此本轮未能跑 Unity EditMode。
- 下一步建议：先恢复 MCP 并导入新测试文件，跑 `BattleHitOutcomePlanServiceTests`、相关战斗测试和全量 EditMode；之后再考虑把 hit outcome / successful hit effects 迁出到独立 dispatcher 文件。

## 2026-04-29 - 遗物系统重构 Stage 65

- 恢复 Unity MCP 后补齐 Stage 64 的 Unity 导入与测试验证：脚本强制刷新/编译完成，Console 0 error，命中结果计划相关定向测试通过 22/22。
- 将 `BattleHitOutcomePlan` 与 `BattleHitOutcomePlanService` 从临时放置的 `BattleSingleAttackPresentationPlanService.cs` 拆到独立文件 `Assets/Scripts/Shared/BattleHitOutcomePlanService.cs`，职责边界更清晰。
- 新增 `BattleSuccessfulHitSkillEffectPlanService`，用纯数据计划锁定成功命中后的技能词条触发顺序：单方面攻击为 `OnSingleAttack -> OnClashWin -> OnHit`，拼点命中为 `OnClashWin -> OnHit`；友方与敌方成功命中技能触发现在共用同一份计划。
- 新增 `BattleSuccessfulHitRelicPlanService`，用纯数据计划描述成功命中后的遗物副作用条件：玩家命中、击杀敌人、玩家受到单方面攻击三类条件分离，实际遗物调用仍保留在 `BattleButton`。
- 新增对应 EditMode 测试：`BattleSuccessfulHitSkillEffectPlanServiceTests` 与 `BattleSuccessfulHitRelicPlanServiceTests`，覆盖触发顺序和遗物条件分支。
- 静态检查完成：本轮相关文件 `git diff --check` 无格式错误，仅保留 Unity/Windows 行尾提示。
- 验证完成：新增/相关定向测试通过 9/9；全量 EditMode 测试通过 244/244；Unity Console 0 error。
- 下一步建议：在已有 plan 测试保护下，继续把 `ApplyAllySuccessfulHitEffects` / `ApplyEnemySuccessfulHitEffects` 迁向独立 dispatcher，或先补一组更细的触发顺序集成测试后再迁出真实副作用。

## 2026-04-29 - 遗物系统重构 Stage 66

- 继续收窄 `BattleButton` 的命中结果与成功命中副作用调度，保持真实伤害、遗物、状态和词条执行函数不改变，仅迁出调度顺序。
- 按 TDD 新增 `BattleSuccessfulHitEffectDispatcherTests`，先确认 RED：测试因缺少 `BattleSuccessfulHitEffectDispatcher` 编译失败；随后新增 `Assets/Scripts/Shared/BattleSuccessfulHitEffectDispatcher.cs` 并接入 `BattleButton`。
- `BattleSuccessfulHitEffectDispatcher` 现在集中串联成功命中后的遗物计划与技能/状态计划，锁定顺序为：遗物副作用先执行，再执行 immediate 词条、响应式受击状态、状态施加词条。
- 按 TDD 新增 `BattleHitOutcomeDispatcherTests`，先确认 RED：测试因缺少 `BattleHitOutcomeDispatcher` 编译失败；随后新增 `Assets/Scripts/Shared/BattleHitOutcomeDispatcher.cs` 并接入 `ApplyAllyHitOutcome` / `ApplyEnemyHitOutcome`。
- `BattleHitOutcomeDispatcher` 现在集中处理命中结果分派：0/负伤害先显示 0 伤害反馈并触发 Miss，正伤害执行成功命中副作用并 return，保持 Stage 64 锁定的历史行为。
- `BattleButton` 中删除了成功命中遗物/技能 helper 的中间层，改为在成功命中入口直接生成 plan 并委托 dispatcher 执行，类内职责进一步收敛到流程编排与真实副作用函数。
- 静态检查完成：本轮相关文件 `git diff --check` 无格式错误，仅保留 Unity/Windows 行尾提示。
- 验证完成：新增/相关定向测试通过 13/13；全量 EditMode 测试通过 248/248；Unity Console 0 error。
- 下一步建议：继续沿同一方向迁出 `ResolveAllyHitDamage` / `ResolveEnemyHitDamage` 的伤害修正管线，或先补命中伤害修正顺序测试，再把输入构建和 Relic context 创建从 `BattleButton` 中分离。

## 2026-04-29 - 遗物系统重构 Stage 67

- 抽出命中伤害修正中的 flat/percent 计算公式，新增 `BattleDamageModifierCalculationService`。
- 覆盖 incoming reduction 与 outgoing bonus 的加法、百分比、四舍五入与下限钳制规则。
- `BattleButton` 中玩家造成伤害、玩家受到普通攻击伤害、玩家受到单方面攻击伤害三处重复公式已改为调用该服务。
- 定向验证：`BattleDamageModifierCalculationServiceTests` 通过 4/4，Unity Console 0 error。

## 2026-04-29 - 遗物系统重构 Stage 68

- 抽出伤害修正后的遗物反馈判断，新增 `BattleDamageModifierFeedbackPlanService`。
- incoming 仅在最终伤害降低时 flush 反馈；outgoing 仅在最终伤害提高时 flush 反馈，保持原行为。
- `BattleButton` 三处伤害修正 helper 已统一使用反馈 plan。
- 定向验证：伤害计算与反馈计划相关测试通过 6/6，Unity Console 0 error。

## 2026-04-29 - 遗物系统重构 Stage 69

- 抽出战斗遗物库存可用性判断，新增 `BattleEncounterRelicAvailabilityService`。
- 替换伤害修正、死亡遗物、命中遗物、受单方面攻击遗物、击杀遗物中的重复 encounter/relicInventory 空引用与 Count 检查。
- 定向验证：库存可用性、伤害计算、反馈计划相关测试通过 8/8，Unity Console 0 error。

## 2026-04-29 - 遗物系统重构 Stage 70

- 抽出伤害修正用 `RelicExecutionContext` 构建逻辑，新增 `BattleDamageModifierContextFactory`。
- 玩家即将受到普通攻击、玩家即将受到单方面攻击、玩家即将造成伤害三类 context 创建已从 `BattleButton` 迁出。
- 定向验证：context factory 与伤害修正相关测试通过 11/11，Unity Console 0 error。

## 2026-04-29 - 遗物系统重构 Stage 71

- 抽出“context 修正后最终伤害 + 反馈计划”的组合结果，新增 `BattleDamageModifierResolutionService`。
- `BattleButton` 不再直接读取 context 的 flat/percent 字段组合伤害，而是通过 resolution 获取最终伤害与反馈计划。
- 定向验证：resolution、context factory、伤害计算与反馈计划相关测试通过 11/11，Unity Console 0 error。

## 2026-04-29 - 遗物系统重构 Stage 72

- 抽出“resolve 伤害 + 根据反馈计划 flush + 返回伤害”的分派，新增 `BattleDamageModifierResolutionDispatcher`。
- 三条伤害修正路径中的 resolution/flush/return 重复块已迁出，`BattleButton` 只传入真实反馈函数。
- 定向验证：resolution dispatcher 与伤害修正相关测试通过 13/13，Unity Console 0 error。

## 2026-04-29 - 遗物系统重构 Stage 73

- 抽出敌方命中玩家时的伤害管线计划，新增 `BattleEnemyHitDamagePipelinePlanService`。
- 锁定顺序：灾厄修正 -> 玩家受到普通攻击遗物修正 -> 如果是单方面攻击则追加玩家受到单方面攻击遗物修正。
- `ResolveEnemyHitDamage` 改为按 plan 执行原有步骤。
- 定向验证：敌方命中伤害管线计划与伤害修正相关测试通过 6/6，Unity Console 0 error。

## 2026-04-29 - 遗物系统重构 Stage 74

- 抽出敌方命中伤害管线调度，新增 `BattleEnemyHitDamagePipelineDispatcher`。
- `ResolveEnemyHitDamage` 中的循环/switch 已迁出，`BattleButton` 只传入 calamity、incoming attack relic、incoming single attack relic 三个实际步骤函数。
- 定向验证：敌方命中伤害管线 dispatcher/plan 测试通过 3/3，Unity Console 0 error。

## 2026-04-29 - 遗物系统重构 Stage 75

- 为友方命中敌人建立对称的伤害管线计划，新增 `BattleAllyHitDamagePipelinePlanService`。
- 当前计划只包含玩家造成伤害遗物修正一步，明确保留后续拓展点。
- `ResolveAllyHitDamage` 改为先生成 plan 再执行步骤。
- 定向验证：友方命中伤害管线计划测试通过 1/1，Unity Console 0 error。

## 2026-04-29 - 遗物系统重构 Stage 76

- 抽出友方命中伤害管线调度，新增 `BattleAllyHitDamagePipelineDispatcher`。
- `ResolveAllyHitDamage` 中的循环/switch 已迁出，`BattleButton` 只传入玩家造成伤害遗物修正函数。
- 定向验证：友方/敌方命中伤害管线 dispatcher/plan 测试通过 5/5，Unity Console 0 error。

## 2026-04-29 - 遗物系统重构 Stage 77

- 抽出 0 伤害弹字数据计划，新增 `BattleZeroDamageFeedbackPlanService`。
- 0 伤害位置仍为角色中心上方 1.4，伤害值固定为 0，并保留攻击方向与伤害倍率修正显示参数。
- `ShowZeroDamageHitFeedback` 改为先生成 plan 再交给反馈系统。
- 定向验证：0 伤害反馈 plan 与命中结果相关测试通过 6/6，Unity Console 0 error。

## 2026-04-29 - 遗物系统重构 Stage 78

- 抽出 0 伤害弹字分派，新增 `BattleZeroDamageFeedbackDispatcher`。
- `ShowZeroDamageHitFeedback` 现在只负责拿角色中心、生成 plan，并把真实弹字调用作为回调传给 dispatcher。
- 定向验证：0 伤害反馈 dispatcher/plan 测试通过 2/2，Unity Console 0 error。

## 2026-04-29 - 遗物系统重构 Stage 79

- 抽出 Miss 副作用计划，新增 `BattleMissEffectPlanService`。
- 明确锁定 Miss 分支只触发 `OnMiss`，友方与敌方 Miss helper 共用同一 plan。
- 定向验证：Miss plan 与命中结果相关测试通过 3/3，Unity Console 0 error。

## 2026-04-29 - 遗物系统重构 Stage 80

- 抽出 Miss 副作用分派，新增 `BattleMissEffectDispatcher`。
- `ApplyAllyMissEffects` / `ApplyEnemyMissEffects` 现在只生成 Miss plan，并把真实 ally/enemy triggered effects 调用作为回调传给 dispatcher。
- 本轮最终静态检查完成：Stage 67-80 相关文件 `git diff --check` 无格式错误，仅保留 Unity/Windows 行尾提示。
- 本轮最终验证完成：全量 EditMode 测试通过 273/273；Unity Console 0 error。
- 下一步建议：继续从 `BattleButton` 迁出 `ApplyPlayerHitRelics`、`ApplyPlayerTookSingleAttackRelics`、`ApplyEnemyKilledByPlayerRelics` 的 context factory / dispatcher，或者转向更大的 `ApplyAllyImmediateTriggeredEffects` / `ApplyEnemyImmediateTriggeredEffects` 词条管线拆分。

## 2026-04-29 - 遗物系统重构 Stage 81-83

- 从 `BattleButton` 继续迁出战斗遗物事件键、命中上下文与通用遗物处理调度，新增 `BattleRelicEventKeyService`、`BattleHitRelicContextFactory`、`BattleRelicProcessDispatcher`。
- 已迁移玩家受到单方面攻击、玩家命中、玩家击杀敌人、玩家防御拼点胜利四类遗物触发入口，保持原触发时机与反馈 flush 逻辑不变。
- 新增对应 EditMode 测试覆盖事件键、命中上下文与 dispatcher 回调顺序。

## 2026-04-29 - 遗物系统重构 Stage 84-87

- 新增 `BattleDeathRelicContextFactory`，迁出玩家死亡遗物与敌方死亡遗物的 context 构建逻辑。
- 新增 `BattleImmediateEffectPlanService` 与 `BattleImmediateEffectDispatcher`，把友方/敌方 immediate 词条触发条件与真实副作用调度拆开。
- 对 death context、immediate plan 与 dispatcher 增加 EditMode 覆盖，降低后续迁出真实词条副作用的风险。

## 2026-04-29 - 遗物系统重构 Stage 88-92

- 新增 `BattleTriggeredEffectPlanService` 与 `BattleTriggeredEffectDispatcher`，迁出友方/敌方普通触发词条的计划与调度。
- 新增 `BattleTargetSelectionService`，替换动态/静态目标选择中的重复随机逻辑。
- 新增 `BattleExperienceRewardService` 与 `BattleRunStateCloneService`，迁出胜利经验基础值、格子倍率、战利品/遗物状态克隆。
- 相关定向 EditMode 测试均已补齐并通过。

## 2026-04-29 - 遗物系统重构 Stage 93-101

- 新增 `BattleSettlementLootEntryBuilder`、`BattleLivingCountService`、`BattleEliteRelicChoiceUtility`、`BattleCollectionCloneService`，继续迁出结算条目、存活计数、精英遗物选择 fallback 与集合克隆。
- 新增 `BattleRelicInventorySetBuilder` 与 `BattleSettlementRelicChoiceBuilder`，迁出已拥有遗物集合构建和结算三选一遗物数据构建。
- 到 Stage 101 为止，`BattleButton` 中的战斗奖励结算路径已经明显收窄，精英三选一相关数据构建也有独立测试保护。

## 2026-04-29 - 遗物系统重构 Stage 102-105

- 按 TDD 追加 `BattleEncounterLootUtilityTests` 与 `BattleRewardSortServiceTests`，先确认 RED：缺少 `BattleEncounterLootUtility` 与 `BattleRewardSortService`。
- 新增 `BattleEncounterLootUtility`，迁出遭遇掉落随机种子盐值与 override 掉落表判定，避免 `BattleButton.RollEncounterDrops` 继续保存散落常量。
- 新增 `BattleRewardSortService`，统一战利品与遗物奖励排序逻辑；奖励对象本身为 `null` 时排最后，数据为空的无效奖励排在有效奖励之后但仍早于真正的 `null` 对象。
- `BattleButton.RollEncounterDrops` 已改为调用上述两个服务，三处重复 sort lambda 已删除。
- 验证完成：目标 EditMode 测试 `BattleEncounterLootUtilityTests`、`BattleRewardSortServiceTests` 通过 4/4；全量 EditMode 通过 307/307；Unity Console 0 error。
- 下一步建议：进入 Stage 106+ 时优先拆 `TryBuildEliteRelicChoice`、`ResolveEliteRelicChoiceFallbackTable`、`TryBuildEditorEliteRelicChoiceFromAssets` 这组三选一遗物构建逻辑，进一步把奖励结算从 `BattleButton` 迁出。

## 2026-04-29 - 遗物系统重构 Stage 106-110

- 继续收窄 `BattleButton.RollEncounterDrops`：新增 `BattleEliteRelicChoiceBuilderService`、`BattleLootDropRollService`、`BattleRelicDropRollService`、`BattleBonusLootRewardService`。
- 精英战三选一遗物构建、普通 loot 聚合、relic 掉落聚合、遗物追加战利品奖励循环已经从 `BattleButton` 迁出；`BattleButton` 仅保留 encounter 上下文、真实遗物处理回调和结果承接。
- 新增并通过对应 EditMode 测试：三选一构建、loot 掉落、relic 掉落、bonus loot 合并与排序。

## 2026-04-29 - 遗物系统重构 Stage 111-115

- 新增 `BattleSettlementDataFactory` 与 `BattleEncounterResolutionWriter`，拆出战斗结算面板数据构建和 `BattleEncounterData` 结算写回。
- 强制撤退结算与普通胜负结算现在通过服务写回 resolved party / loot / relics，并通过 factory 生成 overlay payload。
- 处理过程中发现 `BattleButton.cs` 存在历史非 UTF-8 文本，曾在一次删除乱码块时触发编码损伤；已恢复编译并清理本轮影响到的可见乱码字符串。

## 2026-04-29 - 遗物系统重构 Stage 116-120

- 新增 `BattleSharedEnergyReservationService`，拆出共享能量预留、技能选择最低能耗、角色可用能量计算。
- 新增 `BattleSharedEnergyHudService`，拆出共享能量 HUD 文本与能量条填充比例计算。
- `BattleButton` 的共享能量逻辑进一步收窄，UI 真实创建和事件顺序暂时保留在原类中。

## 2026-04-29 - 遗物系统重构 Stage 121-125

- 新增 `BattleEnemySkillEffectLabelService`，统一敌人技能词条 fallback 标签生成，修复并迁出一处内建效果标签乱码。
- 新增 `BattleStatusStackOperationService`，拆出状态层数减少/清空的数值归一化与日志动作描述。
- 此阶段只迁出纯逻辑，真实状态读取、移除、清空和 BattleLog 调度仍保留在 `BattleButton`，避免一次性搬动副作用顺序。

## 2026-04-29 - 遗物系统重构 Stage 126-130

- 新增 `BattleStatusBurstDamageService`，拆出状态爆发伤害计算，四条状态爆发路径改为调用统一服务。
- 本轮最终 `BattleButton.cs` 约 4555 行，较 Stage 105 的约 4687 行继续下降；`Assets/Scripts/Shared` 中 `Battle*.cs` 服务约 65 个。
- 验证完成：全量 EditMode 通过 339/339；Unity Console 0 error；`git diff --check` 无格式错误，仅保留 Unity/Windows 行尾提示。
- 下一步建议：Stage 131+ 继续拆 `ApplyAllyHealingEffects` / `ApplyEnemyHealingEffects` 的目标选择与治疗量解析，随后再拆状态施加/延迟状态调度；不要直接拆 coroutine 状态机。

## 2026-04-29 - 遗物系统重构 Stage 131-135

- 继续收窄 `BattleButton` 中治疗词条的规则计算，新增 `BattleHealingEffectResolutionService`，将治疗触发匹配、治疗条件判断、固定治疗、伤害转治疗、按状态层数治疗、状态层数 fallback 规则迁出为纯服务。
- 新增 `BattleHealingStatusStackSourceService`，将“治疗量按谁的状态层数计算”的来源选择迁出，包括 `Self` 读取自身层数、非 `Self` 通过目标选择结果读取第一个目标层数、无目标返回 0。
- `ApplyAllyHealingEffects` / `ApplyEnemyHealingEffects` 仍保留真实目标选择、`Heal` 调用与战斗日志，避免本阶段改变副作用顺序；`BattleButton` 现在只通过小 wrapper 把真实角色状态查询委托给新服务。
- 新增 EditMode 测试：`BattleHealingEffectResolutionServiceTests` 与 `BattleHealingStatusStackSourceServiceTests`，覆盖固定治疗、负数钳制、伤害转治疗、按层数治疗、状态资产优先级、Burn fallback、速度条件、触发时机与层数来源选择。
- 当前 `BattleButton.cs` 约 5250 行，`Assets/Scripts/Shared` 下 `Battle*.cs` 服务约 67 个；行数仍偏高，但治疗规则计算已经从主流程中拆出一层。
- 验证完成：治疗相关定向 EditMode 通过 12/12；全量 EditMode 通过 351/351；Unity Console 0 error。本切片触碰的代码/测试文件 `git diff --check` 无格式错误，仅保留 CRLF 提示；全局 `git diff --check` 仍受既有资源/场景文件 trailing whitespace 影响。
- 下一步建议：Stage 136-140 继续拆状态解析与状态施加前置计划，优先迁出多组 `TryResolveStatus`、状态 ID/显示名 fallback、延迟状态日志与免疫阻挡日志，仍不要直接移动 coroutine 状态机和真实 `AddStatus` 副作用。

## 2026-04-29 - 遗物系统重构 Stage 136-160

- Stage 136-140：新增 `BattleStatusResolutionService`，将普通状态、状态爆发、状态层数操作的 `statusData` 优先级与 Burn fallback 解析迁出；新增 `BattleStatusApplicationLogService`，将延迟施加状态与免疫阻挡日志迁出；新增 `BattleStatusApplicationPlanService`，统一状态施加层数、延迟回合与重复次数的钳制规则。
- Stage 141-145：新增 `BattleStatusEffectEligibilityService`，将状态词条的触发时机、层数大于 0、conditionalOnly 分桶判断从 `ApplyAllyStatusEffectsOnly` / `ApplyEnemyStatusEffectsOnly` 中迁出；真实 `AddStatus`、`QueueStatusForRoundStart`、遗物修正与 BattleLog 调度仍保留在 `BattleButton`，未改变副作用顺序。
- Stage 146-150：新增 `BattleEnemyBuiltInEffectEligibilityService`，将敌人 built-in effect 的触发时机与基础有效性判断迁出，包括 Heal/Damage 需要正数 amount、ApplyStatus 需要正数 statusStackAmount、TransferRandomNegativeStatus 需要 amount 或 fallbackAmount 为正。
- Stage 151-155：新增 `BattleStatusTickLogService`，将反应式受击状态、回合开始状态伤害、速度提升/降低、追加状态层数日志迁出；由于 `BattleButton.cs` 存在历史非 UTF-8 乱码，部分乱码日志行使用 Unity MCP 精确行列编辑替换，避免整文件重写造成编码污染。
- Stage 156-160：新增 `BattleAllyProgressionService`，将结算经验推进、升级、升级后按新最大生命值 50% 治疗、最终 HP/EXP 钳制规则迁出；`EvaluateAllyProgression` 现在只负责从 Unity 组件采集角色/武器/初始数值并映射服务结果，未使用的 `ApplyExperienceProgression` helper 已删除。
- 新增 EditMode 测试：`BattleStatusResolutionServiceTests`、`BattleStatusApplicationLogServiceTests`、`BattleStatusApplicationPlanServiceTests`、`BattleStatusEffectEligibilityServiceTests`、`BattleEnemyBuiltInEffectEligibilityServiceTests`、`BattleStatusTickLogServiceTests`、`BattleAllyProgressionServiceTests`。
- 当前 `BattleButton.cs` 约 5145 行，`Assets/Scripts/Shared` 下 `Battle*.cs` 服务约 74 个。相比 Stage 135 的约 5250 行继续下降，但 `BattleButton` 仍然偏大，后续应继续优先拆真实状态施加 dispatcher、built-in 真实分支和结算数据构建。
- 验证完成：新增/相关定向测试均通过；全量 EditMode 通过 373/373；Unity Console 0 error；本轮触碰代码/测试文件 `git diff --check` 无格式错误，仅保留 CRLF 提示。全局 `git diff --check` 仍可能受既有资源/场景文件 trailing whitespace 影响。
- 下一步建议：进入 Stage 161+ 时不要急着拆 coroutine 状态机，优先把 `ApplyResolvedStatusEffect` 的真实副作用包装成 dispatcher 回调接口，然后再处理 `ApplyEnemyBuiltInHealingEffect` / `DamageEffect` / `StatusEffect` / `TransferEffect` 四个真实分支。

## 2026-04-29 - 遗物系统重构 Stage 161-170

- Stage 161：新增 `BattleStatusApplicationDispatcher`，将状态施加的延迟/立即分派从 `ApplyResolvedStatusEffect` 中迁出；真实 `QueueStatusForRoundStart`、`AddStatus`、免疫阻挡日志仍通过回调执行，未改变副作用顺序。
- Stage 162：新增 `BattleEnemyBuiltInEffectLogService`，迁出敌人 built-in heal、damage、transfer 的日志文本；由于 `BattleButton.cs` 仍有历史非 UTF-8 乱码，相关日志行使用 Unity MCP 精确行列编辑替换，避免整文件重写。
- Stage 163：新增 `BattleEnemyBuiltInStatusLogService`，并让 `ApplyEnemyBuiltInStatusToEnemy` / `ApplyEnemyBuiltInStatusToAlly` 复用 `BattleStatusApplicationPlanService` 与 `BattleStatusApplicationDispatcher`，统一 built-in status 的立即/延迟施加分派和日志生成。
- Stage 164：新增 `BattleEnemyBuiltInTransferService`，将可转移负面状态白名单与过滤逻辑从 `BattleButton` 迁出；真实随机选择、RemoveStatusStacks、AddStatus 与 fallback 自加状态时机仍保留在原流程。
- Stage 165：新增 `BattleStatusTickPlanService`，迁出 round start tick 的可见效果过滤与 tick 特效播放判断。
- Stage 166：新增 `BattleStatusFlowTargetService`，迁出 round end 状态消费前的非空/存活过滤判断。
- Stage 167：新增 `BattleStatusGrantEligibilityService`，迁出 battle start ally grant 与 enemy status gain 的 `statusData != null && stacks > 0` 判断。
- Stage 168：新增 `BattleSettlementPartyMemberFactory`，将 `RunPartyMemberState` 和 `BattleSettlementEntry` 的字段映射从 `BattleButton` 迁出。
- Stage 169：新增 `BattleEncounterDropContextService`，迁出 elite 三选一遗物开关与 choice count 钳制规则，继续收窄 `RollEncounterDrops` 的 glue 逻辑。
- 新增 EditMode 测试：`BattleStatusApplicationDispatcherTests`、`BattleEnemyBuiltInEffectLogServiceTests`、`BattleEnemyBuiltInStatusLogServiceTests`、`BattleEnemyBuiltInTransferServiceTests`、`BattleStatusTickPlanServiceTests`、`BattleStatusFlowTargetServiceTests`、`BattleStatusGrantEligibilityServiceTests`、`BattleSettlementPartyMemberFactoryTests`、`BattleEncounterDropContextServiceTests`。
- 当前 `BattleButton.cs` 约 5130 行，`Assets/Scripts/Shared` 下 `Battle*.cs` 服务约 83 个。相比 Stage 160 的约 5145 行小幅下降；本阶段重点是迁出副作用周边规则和日志，不追求大幅删行。
- 验证完成：全量 EditMode 通过 392/392；Unity Console 0 error；本轮触碰代码/测试文件 `git diff --check` 无格式错误，仅保留 CRLF 提示。全局 `git diff --check` 仍可能受既有资源/场景文件 trailing whitespace 影响。
- 下一步建议：Stage 171+ 可以继续压缩推进，优先拆 `ApplyEnemyBuiltInTransferEffect` 的随机选择计划、`ApplyRoundStartStatusEffectsGeneric` 的 ally/enemy 重复循环，以及 `BuildResolvedPartyStates` / `BuildSettlementEntries` 对 `EvaluateAllyProgression` 的重复调用。

## 2026-04-29 - 遗物系统重构 Stage 171-175

- Stage 171：为敌人 built-in 负面状态转移新增 `BattleEnemyBuiltInTransferPlan` 与 `ResolveTransferPlan`，把可转移状态、目标索引随机选择和转移层数 clamp 从 `BattleButton` 迁出；真实随机源通过回调注入，便于测试。
- Stage 172：新增 `BattleEnemyBuiltInTransferDispatcher`，把 transfer 的 remove/add/log 成功路径改成 callback dispatcher；`BattleButton` 仍保留真实 `RemoveStatusStacks`、`AddStatus` 和战斗日志副作用。
- Stage 173：新增 `BattleStatusTickDispatcher`，统一 round-start status tick 的 presentation、tick 特效、伤害/速度/追加状态日志回调顺序；`ApplyRoundStartStatusEffectsGeneric` 的 ally/enemy tick 内层重复循环已收敛到同一个 helper。
- Stage 174：新增 `BattleStatusGrantDispatcher`，将 battle-start ally status grant 的 grant 过滤、目标过滤、AddStatus 成功后日志调度迁出；`activeEncounter.battleStartAllyStatusGrantsApplied` 写回仍留在 `BattleButton`。
- Stage 175：新增 `BattleEnemyStatusGainDispatcher`，迁出 enemy status gains 的 entry 过滤、AddStatus、current stacks 查询和日志回调调度；`ApplyEnemyStatusGains` 现在只提供真实 enemy 副作用函数。
- 验证完成：本轮新增/相关定向 EditMode 测试通过 12/12；全量 EditMode 通过 399/399；Unity Console 0 error；触碰代码文件 `git diff --check` 无格式错误，仅保留 Unity/Windows 行尾提示。
- 当前 `BattleButton.cs` 约 5090 行。下一步建议继续 Stage 176+：优先解决结算路径中 `BuildResolvedPartyStates` / `BuildSettlementEntries` 重复调用 `EvaluateAllyProgression`，随后再拆 `RollEncounterDrops` 剩余 glue；暂不建议碰 round-start 阶段切换乱码日志，除非先统一文本编码策略。

## 2026-04-29 - 遗物系统重构 Stage 176-190

- Stage 176-177：将 `BattleButton` 私有的结算经验推进结果迁出为共享 `BattleSettlementPartyProgression`，并让强制撤退/正常结算先一次性生成 party progressions，再分别映射 run state 与 settlement entry，避免 `EvaluateAllyProgression` 重复执行。
- Stage 178-180：扩展 `BattleSettlementPartyMemberFactory`，新增批量 `BuildRunStates` / `BuildSettlementEntries`，让结算路径的字段映射集中在 factory 中；删除 `BattleButton` 内部的纯转发映射 helper。
- Stage 181-183：新增 `BattleRewardAggregationService`，将掉落聚合 dictionary 转换为 `RunLootState` / `RunRelicState` 并排序的逻辑从 `RollEncounterDrops` 迁出。
- Stage 184-186：扩展 `BattleEliteRelicChoiceBuilderService`，迁出 elite 三选一遗物 fallback 表选择与“构建成功后写回 choiceData”的通用逻辑；删除 `BattleButton.TryBuildEliteRelicChoice` 小 helper。
- Stage 187-188：清理 `BattleButton` 中已无调用的 `CloneLootTables`、`BuildResolvedPartyStates`、`BuildSettlementEntries`、`BuildSettlementLootEntries` 等转发层，减少无意义中间层。
- Stage 189-190：完成验证检查。本轮新增/相关定向 EditMode 测试通过 25/25；全量 EditMode 通过 404/404；Unity Console 0 error；触碰代码文件 `git diff --check` 无格式错误，仅保留 Unity/Windows 行尾提示。
- 当前 `BattleButton.cs` 约 4986 行。下一阶段建议进入 Stage 191+：优先拆 `ResolveEliteRelicChoiceFallbackTable` 中的 Editor AssetDatabase fallback、round-start 状态驱动阶段切换日志/计划，以及 healing/status burst/stack operation 的真实副作用 dispatcher；这些切片风险高于本轮，需要继续保持小步测试保护。

## 2026-04-29 - 遗物系统重构 Stage 191-200

- Stage 191-193：扩展 `BattleEliteRelicChoiceBuilderService`，把 Editor fallback 遗物表候选筛选与从全量遗物资产构建 elite 三选一选择数据迁出 `BattleButton`；`BattleButton` 只保留 AssetDatabase 读取与结果写回委托。
- Stage 194-196：扩展 `BattleStatusFlowTargetService`，把回合开始状态驱动阶段切换的目标可处理判断与阶段切换日志文本迁出，减少 `BattleButton` 中的条件重复。
- Stage 197-199：新增 `BattleHealingEffectDispatcher`，把技能治疗的“实际治疗量 > 0 才记录日志”规则从友方/敌方四条治疗路径迁出；`BattleButton` 仍负责目标选择、Heal 回调和 BattleLog 闭包。
- Stage 200：完成本轮验证。`dotnet build Assembly-CSharp-Editor.csproj --no-restore` 通过（仅既有 CS0649 警告）；目标 EditMode 测试通过 22/22；全量 EditMode 测试通过 410/410；Unity Console 0 error；相关文件 `git diff --check` 无格式错误，仅保留 Windows 行尾提示。
- 当前 `BattleButton.cs` 约 4996 行。下一阶段建议继续处理 healing/status stack operation/status burst 附近的剩余副作用管线，优先把“计划生成”和“真实副作用执行”分开，避免继续在 `BattleButton` 内增加分支。

## 2026-04-29 - 遗物系统重构 Stage 201-210

- Stage 201-203：扩展 `BattleStatusStackOperationService`，新增 `BattleStatusStackOperationDispatcher`，把状态层数操作的 clear/reduce、减少数量钳制、affectedStacks > 0 才记录日志的规则从 `BattleButton` 四条路径迁出；真实 `GetStatusStacks` / `RemoveStatusStacks` / `ClearStatus` / `BattleLog` 仍通过回调执行。
- Stage 204-206：扩展 `BattleStatusBurstDamageService`，新增 `BattleStatusBurstDispatcher`，统一状态爆发的基础伤害计算、伤害修正回调、TakeDamage、可选清层和日志调度；玩家对敌、敌人对玩家、友方自伤、敌方自伤四条路径共用同一分发规则。
- Stage 207-208：新增 `BattleEnemyStageTransitionEffectService`，迁出敌人阶段切换词条的触发门禁与 NextRoundStart 判断；`BattleButton` 只保留 `TryTransitionToStage` 与日志副作用。
- Stage 209-210：新增 `BattleSummonEffectService`，迁出召唤词条的触发门禁、召唤数量钳制和标签 fallback；`BattleButton` 仍负责实际 Spawn、计数、刷新敌人列表和绑定反馈系统。
- 验证完成：`dotnet build Assembly-CSharp-Editor.csproj --no-restore` 通过（仅既有 CS0649 警告）；本轮定向 EditMode 测试通过 14/14；全量 EditMode 测试通过 419/419；Unity Console 0 error；相关文件 `git diff --check` 无格式错误，仅保留 Windows 行尾提示。
- 当前 `BattleButton.cs` 约 4957 行。下一阶段建议继续压缩 `ApplyAllyStatusEffectsOnly` / `ApplyEnemyStatusEffectsOnly` 的 self-target 分支重复，以及将状态解析 + 计划生成 + 分发的组合封装成更窄的 status application facade。

## 2026-04-29 - BattleButton 重构收束 Stage 211-230

- Stage 211-214：新增 `BattleStatusEffectApplicationService`，把状态施加的状态解析、应用计划生成和立即施加日志文本从 `BattleButton` 迁出；友方/敌方 self-target 状态分支和 `ApplyResolvedStatusEffect` 两条路径已改为复用同一 facade。
- Stage 215-217：新增 `BattleRelicBridge`，统一 `HasBattleRelics`、relic context process 和反馈 flush 的入口；玩家受击/命中/击杀/防御拼点胜利、玩家死亡、敌人死亡、伤害修正等遗物入口已切到桥接层。
- Stage 218-220：新增 `BattleComponentResolver`，把 `FindObjectOfType + host.GetComponent + AddComponent` 的 overlay/feedback/presentation 解析样板从 `BattleButton` 迁出。
- Stage 221-222：新增 `BattleEditorAssetFallbackService`，把 `AssetDatabase` fallback 遗物表搜索与全量遗物资产搜索从 `BattleButton` 的 editor 分支迁出。
- Stage 223-226：新增 `BattleStatusTargetAdapter`，把 ally/enemy 目标的状态层数、伤害、清层、减层访问统一成 adapter；状态爆发与状态层数操作的四组敌我重载已压缩为共享执行入口。
- Stage 227-229：新增 `BattleDamageEffectDispatcher`，统一额外伤害词条的基础伤害钳制、伤害修正、TakeDamage 与日志调度；友方/敌方额外伤害分支改为 dispatcher 回调。
- Stage 230：完成验证。`dotnet build Assembly-CSharp-Editor.csproj --no-restore` 通过（仅既有 CS0649 警告）；本轮新增/相关定向 EditMode 测试通过 22/22；全量 EditMode 测试通过 432/432；Unity Console 0 error；相关文件 `git diff --check` 无格式错误，仅保留 Windows 行尾提示。
- 当前 `BattleButton.cs` 约 4873 行。本轮已收掉剩余十项中的规则/副作用胶水、状态 adapter、遗物桥、overlay resolver、Editor fallback 和额外伤害 dispatcher；仍不建议继续把主 coroutine 流程强拆为独立 MonoBehaviour，除非下一步专门做战斗流程状态机重写与 PlayMode 覆盖。

## 2026-04-29 - 战斗流程 PlayMode 回归测试框架 Stage 231

- 新增 `Assets/Tests/PlayMode/MobileGame.PlayModeTests.asmdef`，建立独立 PlayMode 测试程序集；由于当前生产代码仍位于默认 `Assembly-CSharp`，本阶段先采用反射式 fixture 访问运行时代码，避免为全项目一次性引入 asmdef 迁移风险。
- 新增 `Assets/Tests/PlayMode/BattleFlowPlayModeFixture.cs`，可在测试中运行时构造最小战斗场景：`BattleManager`、`BattleButton`、敌我父节点、1 名友方、1 名敌方、速度按钮、固定威力技能、武器与敌人数据。
- 新增 `Assets/Tests/PlayMode/BattleFlowPlayModeTests.cs`，覆盖两条首批战斗流程烟测：点击战斗按钮进入目标指定阶段并触发敌方自动连线；强制拼点连线后执行一回合，友方胜利击杀敌人并弹出结算面板。
- fixture 会在每次测试前后清理 `BattleEncounterRuntime`、`TutorialBattleRuntime` 与 `SpeedButton` 静态输入锁，避免教程/上一次战斗状态污染后续 PlayMode 测试。
- 验证完成：`dotnet build Assembly-CSharp-Editor.csproj --no-restore` 0 error；`dotnet build MobileGame.PlayModeTests.csproj` 0 error；全量 PlayMode 2/2 通过；全量 EditMode 432/432 通过；Unity Console 0 error。
- 后续建议：继续扩充 PlayMode 回归覆盖，优先加入“单方面攻击回合不结算卡死”“敌方击杀友方失败结算”“精英三选一遗物结算链路”“教程战斗阻塞/放行策略”四类高风险端到端流程。
## 2026-04-29 - 战斗流程 PlayMode 回归与按钮守卫拆分 Stage 232-236

- Stage 232：扩展 PlayMode fixture，新增友方单方面攻击连线构造，覆盖“敌方未拼点时友方单方面攻击造成伤害并回到 Ready，不误弹结算”的端到端流程。
- Stage 233：扩展失败结算 PlayMode 覆盖，新增敌方单方面攻击击杀友方后弹出失败结算的回归测试，并让最小战斗 fixture 支持单独配置友方生命值。
- Stage 234：新增教程阻塞 PlayMode 覆盖，通过运行时构造 `TutorialBattleDefinition_SO` 和 `RequireBeforeCombat` 步骤，验证教程阻塞时不会进入 `ResolvingCombat`，避免“看清敌方行动后仍卡住/误推进”的回归。
- Stage 235：新增精英战三选一遗物结算链路 PlayMode 覆盖，fixture 可构造 elite encounter、敌人组和 3 个临时遗物表项，验证胜利结算数据确实包含 relic choice。
- Stage 236：新增 `BattleCombatPhaseGuardService`，将 `BattleButton.OnBattleButtonClicked` 中结算面板、技能详情面板、教程弹窗可见时的早退判断迁出为纯服务；`BattleButton` 保持原 overlay 解析顺序和后续阶段推进逻辑不变。
- 新增/扩展测试：`BattleFlowPlayModeTests` 覆盖单方面攻击、失败结算、教程阻塞、精英遗物选择；`BattleCombatPhaseGuardServiceTests` 覆盖三个阻塞来源与无阻塞通过场景。
- 验证完成：`dotnet build Assembly-CSharp-Editor.csproj --no-restore` 通过（仅既有 CS0649 警告）；`dotnet build MobileGame.PlayModeTests.csproj --no-restore` 通过；全量 PlayMode 6/6 通过；全量 EditMode 436/436 通过；Unity Console 0 error；相关文件 `git diff --check` 无格式错误，仅保留 Windows 行尾提示。
- 当前 `BattleButton.cs` 约 4872 行，`Assets/Scripts/Shared` 下 `Battle*.cs` 服务约 92 个。下一步建议继续补 PlayMode 回归的实际 UI/结算交互断言，再考虑拆 `OnBattleButtonClicked` 中 BattleManager/parent/allies/enemies 获取与反馈系统绑定这类低风险入口 glue。
## 2026-04-29 - 战斗结算 PlayMode 交互回归 Stage 237-240

- Stage 237：扩展 `BattleFlowPlayModeFixture`，新增结算界面 loot view、遗物选择入口、三选一选项、确认按钮、返回/重试按钮的反射点击 helper，并支持读取当前选择、弹窗标题、入口展示名称、CurrentEncounter 与 SpeedButton 锁状态。
- Stage 238：新增 PlayMode 回归 `EliteRelicChoice_WhenPlayerSelectsChoice_RecordsSelectionAndClosesModal`，覆盖从精英胜利结算点击问号入口、打开三选一弹窗、选择候选、确认关闭、选择写回以及战利品入口展示切换为所选遗物的链路。
- Stage 239：新增 PlayMode 回归 `EliteRelicChoice_WhenReturningBeforeSelection_BlocksReturnAndShowsRequiredChoice`，覆盖未选择遗物时点击返回会阻止离开、保持结算面板可见、打开三选一弹窗并显示必选提示。修复 `BattleSettlementOverlay.ShowRelicChoiceRequiredMessage` 中必选提示被 `OpenRelicChoiceModal` 默认标题覆盖的问题。
- Stage 240：新增 PlayMode 回归 `DefeatSettlement_WhenRetryClicked_RequeuesEncounterAndHidesSettlement`，覆盖失败结算点击 Retry 后重新排队 active encounter、隐藏结算面板并解除 `SpeedButton.CombatLocked` 的链路；fixture teardown 会清理测试触发的 `SceneCurtainTransitionController`，避免转场协程污染后续测试。
- 验证完成：`dotnet build Assembly-CSharp-Editor.csproj --no-restore` 通过（仅既有 CS0649 警告）；`dotnet build MobileGame.PlayModeTests.csproj --no-restore` 通过；全量 PlayMode 9/9 通过；全量 EditMode 436/436 通过；Unity Console 0 error；相关文件 `git diff --check` 无格式错误。
- 当前 `BattleButton.cs` 约 4872 行，`Assets/Scripts/Shared` 下 `Battle*.cs` 服务约 92 个。下一步建议进入入口 glue 拆分：把 `OnBattleButtonClicked` 中 BattleManager 获取、parent 校验、combatants 收集与 feedback bind 收束成一个可测试的入口上下文构建服务，再继续拆 `BuildCombatActions`。
## 2026-04-29 - BattleButton 入口上下文拆分 Stage 241-243

- Stage 241：新增 `BattleCombatantContextService`，将 `OnBattleButtonClicked` 中 allies/enemies parent 非空校验和 `GetComponentsInChildren(..., false)` 敌我组件收集迁出；服务返回 warning message，保持原先缺少 parent 时的早退与日志语义。
- Stage 242：新增 `BattleCombatantContextServiceTests`，覆盖 parent 缺失时返回 false 与提示文本、正常收集时排除 inactive 子节点并返回敌我组件数组，锁定 `BattleButton` 原有 includeInactive=false 行为。
- Stage 243：`BattleButton.OnBattleButtonClicked` 改为通过入口上下文服务获取 allies/enemies，再继续执行 feedback bind、Ready->Targeting、教程阻塞与 combat coroutine；本阶段不改变 BattleManager 查找、overlay guard 和阶段推进顺序。
- 验证完成：`dotnet build Assembly-CSharp-Editor.csproj --no-restore` 通过（仅既有 CS0649 警告）；`dotnet build MobileGame.PlayModeTests.csproj --no-restore` 通过；全量 PlayMode 9/9 通过；全量 EditMode 438/438 通过；Unity Console 0 error；相关文件 `git diff --check` 无格式错误，仅保留 Windows 行尾提示。
- 当前 `BattleButton.cs` 约 4875 行，`Assets/Scripts/Shared` 下 `Battle*.cs` 服务约 93 个。下一步建议继续拆 `OnBattleButtonClicked` 中 BattleManager 解析与 feedback bind，或者直接进入 `BuildCombatActions`/`ExecuteCombatPhaseRoutine` 的计划层拆分；前者风险低，后者收益更高但需要继续 PlayMode 防护。
## 2026-04-29 - BattleButton 入口解析与反馈绑定拆分 Stage 244-247

- Stage 244：新增 `BattleManagerResolverService`，统一 `BattleButton.Awake` 与 `OnBattleButtonClicked` 的 BattleManager 解析路径；已有 manager 直接复用，缺失时通过 finder 查找，查找失败时返回原有 BattleManager 未找到 warning 文案。
- Stage 245：新增 `BattleManagerResolverServiceTests`，覆盖已有 manager 不调用 finder、缺失时使用 finder、完全找不到时返回 false 与 warning message，锁定点击入口早退前置条件。
- Stage 246：新增 `BattleFeedbackBindingService`，将 `battleFeedbackSystem?.BindCombatants(allies, enemies)` 收束为 null-safe 服务入口，并对 allies/enemies 空数组做防御性归一化；真实绑定副作用仍由 `BattleFeedbackSystem` 执行。
- Stage 247：新增 `BattleFeedbackBindingServiceTests` 覆盖 feedback 缺失时的安全早退；真实绑定路径继续由 PlayMode 回归覆盖，避免 EditMode 下 `BattleFeedbackSystem` 创建/销毁 Canvas 与 EventSystem 的环境副作用污染测试。
- 验证完成：`dotnet build Assembly-CSharp-Editor.csproj --no-restore` 通过（仅既有 CS0649 警告）；`dotnet build MobileGame.PlayModeTests.csproj --no-restore` 通过；全量 PlayMode 9/9 通过；全量 EditMode 442/442 通过；Unity Console 0 error；相关文件 `git diff --check` 无格式错误，仅保留 Windows 行尾提示。
- 当前 `BattleButton.cs` 约 4881 行，`Assets/Scripts/Shared` 下 `Battle*.cs` 服务约 95 个。下一步建议停止继续拆入口细枝末节，转入收益更高的 `ExecuteCombatPhaseRoutine` 计划层拆分：先提取“回合执行前/后阶段切换和结算检查”的纯计划，再逐步缩短 coroutine 主体。
## 2026-04-29 - ExecuteCombatPhaseRoutine 完成计划层拆分 Stage 248-250

- Stage 248：新增 `BattleCombatPhaseCompletionPlanService` 与 `BattleCombatPhaseCompletionPlan`，把 action 中途战斗完成和正常回合结束两条 completion path 的后置动作计划化：是否清理 active combatants、是否结束 resolving、是否解锁输入、是否回到 Ready、是否推进 round。
- Stage 249：新增 `BattleCombatPhaseCompletionPlanServiceTests`，覆盖中途结算时保持 combat lock/不推进 round，以及正常回合结束时解锁输入/回 Ready/round++ 的差异，作为后续继续缩短 coroutine 的计划层安全网。
- Stage 250：`ExecuteCombatPhaseRoutine` 接入 completion plan。中途结算分支统一调用计划应用 helper；正常回合结束分支保留原顺序：先解锁 `SpeedButton.CombatLocked`，再写“再次按下战斗按钮”日志，最后设置 Ready、round++、清理 active combatants、结束 resolving。
- 验证完成：`dotnet build Assembly-CSharp-Editor.csproj --no-restore` 通过（仅既有 CS0649 警告）；`dotnet build MobileGame.PlayModeTests.csproj --no-restore` 通过；关键 PlayMode completion path 3/3 通过；全量 PlayMode 9/9 通过；全量 EditMode 444/444 通过；Unity Console 0 error；相关文件 `git diff --check` 无格式错误，仅保留 Windows 行尾提示。
- 当前 `BattleButton.cs` 约 4892 行，`Assets/Scripts/Shared` 下 `Battle*.cs` 服务约 96 个。下一步建议继续沿 coroutine 计划层推进：拆“进入 combat resolving 时的按钮隐藏/锁定/技能详情隐藏”成 begin plan 或 dispatcher，并用 PlayMode 覆盖 Ready->Targeting->Resolving 的可交互状态。
## 2026-04-29 - ExecuteCombatPhaseRoutine 开始计划层拆分 Stage 251-253

- Stage 251：新增 `BattleCombatPhaseBeginPlanService` 与 `BattleCombatPhaseBeginPlan`，将进入 combat resolving 阶段时的意图计划化：标记 resolving、提交预留能量、锁定输入、隐藏友方/敌方速度按钮、禁用友方速度按钮、隐藏技能详情面板。
- Stage 252：新增 `BattleCombatPhaseBeginPlanServiceTests`，覆盖 begin plan 的全部标志位，作为后续继续拆 coroutine 开头副作用的纯规则安全网。
- Stage 253：`ExecuteCombatPhaseRoutine` 接入 `ApplyCombatPhaseBeginPlan` helper，真实副作用顺序保持不变：先设置 active round combatants，再标记 resolving/提交能量/锁输入/隐藏按钮/隐藏技能详情，随后写战斗结算阶段日志并构建 actions。
- 验证完成：`dotnet build Assembly-CSharp-Editor.csproj --no-restore` 通过（仅既有 CS0649 警告）；`dotnet build MobileGame.PlayModeTests.csproj --no-restore` 通过；begin plan 定向 EditMode 1/1 通过；关键 PlayMode 3/3 通过；全量 PlayMode 9/9 通过；全量 EditMode 445/445 通过；Unity Console 0 error；相关文件 `git diff --check` 无格式错误，仅保留 Windows 行尾提示。
- 当前 `BattleButton.cs` 约 4907 行，`Assets/Scripts/Shared` 下 `Battle*.cs` 服务约 97 个。下一步建议继续拆 begin helper 内部的真实按钮副作用为 `BattleSpeedButtonCombatVisibilityService`，把 ally/enemy 按钮遍历规则从 `BattleButton` 迁出；之后再处理回合总结日志与 round-end status/relic 调用顺序。
## 2026-04-29 - ExecuteCombatPhaseRoutine 副作用与回合总结拆分 Stage 254-259

- Stage 254：按 TDD 新增 `BattleSpeedButtonCombatVisibilityServiceTests`，先通过 Unity 编译红灯确认缺少 `BattleSpeedButtonCombatVisibilityService`，覆盖友方按钮禁用/隐藏、敌方按钮隐藏、空 controller/空按钮安全跳过。
- Stage 255：新增 `BattleSpeedButtonCombatVisibilityService`，将进入 combat resolving 时 ally/enemy 速度按钮遍历、禁用与 `HideForCombat` 副作用从 `BattleButton.ApplyCombatPhaseBeginPlan` 迁出；`BattleButton` 继续保留 resolving 标记、能量提交、输入锁与技能详情面板隐藏顺序。
- Stage 256：接入后通过定向 EditMode 3/3 与关键 PlayMode 3/3，确认 Ready->Targeting->Resolving、单方面攻击、拼点击杀结算路径未回归。
- Stage 257：按 TDD 新增 `BattleRoundSummaryLogServiceTests`，先通过 Unity 编译红灯确认缺少 `BattleRoundSummaryLogService`，锁定回合总结标题、存活友方、存活敌方、空对象/死亡对象跳过以及原顺序。
- Stage 258：新增 `BattleRoundSummaryLogService`，将 `ExecuteCombatPhaseRoutine` 中的回合总结文本组装迁出为纯 service；协程仅负责按 service 返回顺序调用 `BattleLog`，随后继续执行 round-end status 与 relic。
- Stage 259：完成回归验证：`dotnet build Assembly-CSharp-Editor.csproj --no-restore` 通过（仅保留既有 CS0649 warning）；`dotnet build MobileGame.PlayModeTests.csproj --no-restore` 通过；全量 PlayMode 9/9 通过；全量 EditMode 449/449 通过；Unity Console 0 error；相关文件 `git diff --check` 无格式错误，仅保留 Windows 行尾提示。
- 当前 `BattleButton.cs` 约 4220 行，`Assets/Scripts/Shared` 下 `Battle*.cs` 服务约 99 个。下一步建议继续沿 `ExecuteCombatPhaseRoutine` 做低风险切片：优先把回合结束阶段的 `FinalizeRoundSkillHistory` 遍历与 round-end status/relic 调度顺序包装成计划/dispatcher，再考虑 action 构建与 action 执行循环的更大拆分。
## 2026-04-29 - ExecuteCombatPhaseRoutine 回合结束调度拆分 Stage 260-265

- Stage 260：按 TDD 新增 `BattleCombatRoundEndDispatcherTests`，先通过 Unity 编译红灯确认缺少 `BattleCombatRoundEndDispatcher`；测试锁定正常回合结束顺序为：友方技能历史收尾 -> 回合总结日志 -> round-end status -> round-end relic，并覆盖空回调与 null 数组安全性。
- Stage 261：新增 `BattleCombatRoundEndDispatcher`，将 `ExecuteCombatPhaseRoutine` 里友方 `FinalizeRoundSkillHistory` 遍历、回合总结回调、回合末状态与遗物调度收束为一个顺序明确的 dispatcher；`BattleButton` 只保留真实 `BattleLog`、status 与 relic 副作用回调。
- Stage 262：新增 `LogRoundSummary` helper，让 `BattleButton` 通过 `BattleRoundSummaryLogService` 输出总结日志，同时由 dispatcher 确保其仍位于技能历史收尾之后、round-end status/relic 之前；定向 EditMode 2/2 与正常回合 PlayMode 路径通过。
- Stage 263：按 TDD 新增 `BattleCombatPhaseCompletionDispatcherTests`，先通过 Unity 编译红灯确认缺少 `BattleCombatPhaseCompletionDispatcher`；测试锁定正常回合完成顺序为：如需解锁则先解锁输入 -> 写下一回合提示 -> 应用 completion plan，并覆盖中途结算 plan 不解锁与空回调安全性。
- Stage 264：新增 `BattleCombatPhaseCompletionDispatcher`，将正常回合结束尾部的 `SpeedButton.CombatLocked = false`、下一回合提示日志、`ApplyCombatPhaseCompletionPlan(..., false)` 顺序迁出为可测 dispatcher；中途战斗完成分支保持原有 `ApplyCombatPhaseCompletionPlan(Resolve(true))` 早退路径不变。
- Stage 265：完成回归验证：`dotnet build Assembly-CSharp-Editor.csproj --no-restore` 通过（仅保留既有 CS0649 warning）；`dotnet build MobileGame.PlayModeTests.csproj --no-restore` 通过；全量 PlayMode 9/9 通过；全量 EditMode 454/454 通过；Unity Console 0 error；相关文件 `git diff --check` 无格式错误，仅保留 Windows 行尾提示。
- 当前 `BattleButton.cs` 约 4224 行，`Assets/Scripts/Shared` 下 `Battle*.cs` 服务约 101 个。下一步建议继续推进 `ExecuteCombatPhaseRoutine` 中更大但仍可控的切片：将 action 执行循环拆成带回调的 dispatcher，或先把 `TryHandleBattleCompletion` 之后的中途完成 plan 应用也纳入 completion dispatcher，统一 completion 入口。
## 2026-04-30 - ExecuteCombatPhaseRoutine completion 与 action loop 拆分 Stage 266-272

- Stage 266：扩展 `BattleCombatPhaseCompletionDispatcherTests`，按 TDD 先确认缺少 `DispatchBattleCompletedDuringAction`；测试锁定中途战斗完成路径只应用 completion plan，不解锁输入、不写下一回合提示，保持原有早退语义。
- Stage 267：新增 `BattleCombatPhaseCompletionDispatcher.DispatchBattleCompletedDuringAction`，并将 `ExecuteCombatPhaseRoutine` 中 `TryHandleBattleCompletion` 后的中途完成分支改为通过 dispatcher 应用 `BattleCombatPhaseCompletionPlanService.Resolve(true)`；正常回合结束路径仍走 `DispatchNormalRoundEnd`。
- Stage 268：新增 `BattleCombatActionExecutionDispatcherTests`，按 TDD 先确认缺少 `BattleCombatActionExecutionDispatcher`；测试锁定 action 类型到 resolver 的映射、每个 action 执行完成后才检查 battle completion、completion 命中后停止后续 action，并覆盖 null action / 空回调安全性。
- Stage 269：新增 `BattleCombatActionExecutionDispatcher`，将 action 遍历、Clash / AllySingleAttack / EnemySingleAttack 分派、每步后完成检查、完成回调和早退收束成协程 dispatcher。
- Stage 270：`ExecuteCombatPhaseRoutine` 的 action switch 循环已替换为 `BattleCombatActionExecutionDispatcher.Dispatch`；`BattleButton` 只保留真实 action resolver、`TryHandleBattleCompletion` 和 completion plan 应用回调，主协程职责进一步收窄。
- Stage 271：定向验证通过：`BattleCombatPhaseCompletionDispatcherTests` 5/5，`BattleCombatActionExecutionDispatcherTests` 3/3；关键 PlayMode 覆盖普通单方面攻击回合与拼点击杀中途结算 2/2 通过。
- Stage 272：完成回归验证：`dotnet build Assembly-CSharp-Editor.csproj --no-restore` 通过（仅保留既有 CS0649 warning）；`dotnet build MobileGame.PlayModeTests.csproj --no-restore` 通过；全量 PlayMode 9/9 通过；全量 EditMode 459/459 通过；Unity Console 0 error；相关文件 `git diff --check` 无格式错误，仅保留 Windows 行尾提示。
- 当前 `BattleButton.cs` 约 4220 行，`Assets/Scripts/Shared` 下 `Battle*.cs` 服务约 102 个。下一步建议继续收窄 `BuildCombatActions` 周边：优先将 action resolver 回调构建与 battle completion 回调构建包装成一个 execution context/factory，或补 PlayMode 多友方/多敌人 action 顺序回归后再拆 `BattleCombatActionBuilder` 的候选收集逻辑。
## 2026-04-30 - ExecuteCombatPhaseRoutine action 执行循环收尾 Stage 273-276

- Stage 273：扩展 `BattleCombatActionExecutionDispatcherTests`，按 TDD 先确认缺少 `BattleCombatActionExecutionResult`；测试锁定当 action 后 completion check 命中时，dispatcher 除了停止后续 action，还必须在 result 中标记 `BattleCompletedDuringAction`。
- Stage 274：新增 `BattleCombatActionExecutionResult`，并让 `BattleCombatActionExecutionDispatcher.Dispatch` 在 completion 命中时写入执行结果；`BattleButton` 不再维护本地 `battleCompletedDuringAction` bool，而是读取 dispatcher 产出的 result。
- Stage 275：新增 `BattleButton.ExecuteCombatActions` 私有协程 helper，将 action resolver、battle completion check 与 completion plan 应用回调从 `ExecuteCombatPhaseRoutine` 主体迁出；主协程现在只保留 action 构建、执行 helper 调用、result 早退判断、round-end 与 completion 调度。
- Stage 276：本轮可执行验证：`dotnet build Assembly-CSharp-Editor.csproj --no-restore` 通过（仅保留既有 CS0649 warning）；`dotnet build MobileGame.PlayModeTests.csproj` 通过；相关文件 `git diff --check` 无格式错误，仅保留 Windows 行尾提示。当前会话未暴露可用 Unity MCP Test Runner 工具，因此本轮新增小改动尚未重新跑 Unity EditMode/PlayMode；上一轮在 action dispatcher 初拆后曾通过全量 PlayMode 9/9 与 EditMode 459/459。
- 当前 `BattleButton.cs` 约 4228 行，`Assets/Scripts/Shared` 下 `Battle*.cs` 服务约 102 个。下一步建议补跑 Unity Test Runner 后，再进入 `BuildCombatActions` 拆分或新增多友方/多敌人 PlayMode action 顺序回归。

## 2026-04-30 - ExecuteCombatPhaseRoutine action 执行上下文收尾 Stage 277-285

- Stage 277-279：扩展 `BattleCombatActionExecutionDispatcherTests`，新增对 `BattleCombatActionExecutionContext` 的覆盖，锁定 resolver 分派、完成检查、完成回调和 result 写回都能通过统一上下文执行。
- Stage 280-282：新增 `BattleCombatActionExecutionContext`，把 Clash、友方单方面攻击、敌方单方面攻击、战斗完成检查、完成后处理和执行结果集中封装，`BattleCombatActionExecutionDispatcher` 继续保留旧 callback overload，降低迁移风险。
- Stage 283-285：`BattleButton.ExecuteCombatPhaseRoutine` 改为创建 action execution context 后调用 `ExecuteCombatActions(actions, context)`，主协程不再直接持有 action resolver lambdas，也不再维护本地 `battleCompletedDuringAction` 标记；中途结算判断统一读取 `context.Result.BattleCompletedDuringAction`。
- 验证完成：`dotnet build Assembly-CSharp-Editor.csproj --no-restore` 通过 0 warning / 0 error；`dotnet build MobileGame.PlayModeTests.csproj --no-restore` 通过 0 warning / 0 error；Unity 全量 PlayMode 9/9 通过；Unity 全量 EditMode 460/460 通过；Unity Console 0 error；相关文件 `git diff --check` 无格式错误，仅保留 Windows 行尾提示。
- 当前 `BattleButton.cs` 约 4885 行，`Assets/Scripts/Shared` 中 `Battle*.cs` 服务约 102 个。下一步建议进入 `BuildCombatActions` 周边拆分：先给 action 收集/排序增加更细的 EditMode 覆盖，再把 ally action collection、enemy action collection、sort/merge 从 `BattleButton` 迁出，避免直接大改战斗协程。

## 2026-04-30 - BuildCombatActions 周边拆分 Stage 286-295

- Stage 286-288：新增 `BattleCombatActionOrderingService`，将 action 排序规则从 `BattleCombatActionBuilder` 迁出，排序顺序保持为速度降序、TiePriority 升序、SequenceIndex 升序；`BattleCombatActionBuilder.Compare` 暂保留为转发入口，避免破坏旧调用。
- Stage 289-292：新增 `BattleCombatActionCollectorService`，将友方 action 收集、敌方 action 收集、拼点 enemy button 去重、跳过未指定目标友方日志从 builder 主体拆出；`BattleCombatActionBuilder.Build` 现在只负责组织 collector 输出并调用排序服务。
- Stage 293-295：新增 `BattleSpeedButtonListService`，统一 ally/enemy 多速度按钮优先、legacy `speedButton` fallback 的读取规则，并替换 `BattleButton` 与 action collector 中的重复 helper；`BattleButton` 删除本地 `GetAllySpeedButtons` / `GetEnemySpeedButtons`。
- 新增 EditMode 覆盖：`BattleCombatActionOrderingServiceTests`、`BattleCombatActionCollectorServiceTests`、`BattleSpeedButtonListServiceTests`，并继续保留 `BattleCombatActionBuilderTests` 作为外观级回归。
- 验证完成：`dotnet build Assembly-CSharp-Editor.csproj --no-restore --no-incremental` 通过，仅保留既有 CS0649 与 MCP package 内 CS0219 warning；`dotnet build MobileGame.PlayModeTests.csproj --no-restore --no-incremental` 通过 0 warning / 0 error；Unity 全量 PlayMode 9/9 通过；Unity 全量 EditMode 466/466 通过；Unity Console 0 error；相关文件 `git diff --check` 无格式错误，仅保留 Windows 行尾提示。
- 当前 `BattleButton.cs` 约 4859 行，`Assets/Scripts/Shared` 中 `Battle*.cs` 服务约 105 个。下一步建议继续围绕战斗输入/目标指定做低风险收束：把 Ready->Targeting 初始化中速度按钮 reset、敌方自动连线和按钮可交互切换拆成小型 dispatcher/service，再用 PlayMode 覆盖点击入口。
## 2026-04-30 - BattleButton 目标指定阶段收口 Stage 296-330

- Stage 296-302：新增 `BattleTargetingSpeedButtonStateService`，把 Ready -> Targeting 时 ally/enemy 速度按钮 reset、显示、隐藏、可交互切换从 `BattleButton.SetupTargetingPhase` 拆出。
- Stage 303-310：新增 `BattleSharedEnergyRoundStartService` 与 `BattleSharedEnergyReservationCollectorService`，统一回合开始共享能量恢复/清空预占，以及按已连线速度按钮和当前技能能耗计算预占能量。
- Stage 311-316：新增 `BattleAllySkillEnergyAvailabilityService`，把技能切换时的能量可用性判断从 `BattleButton.GetAllySkillUnavailableReason` 拆出，文案仍留在原入口，避免触碰历史编码混乱字符串。
- Stage 317-324：新增 `BattleTargetingEnemyAutoTargetService`，把敌方自动锁定后的按钮遍历和日志组装迁出；自动连线副作用仍按原顺序执行。
- Stage 325-330：新增 `BattleTargetingPhaseLogService`，统一目标指定阶段标题、友方/敌方速度日志和敌方自动指定完成提示。
- 新增/补齐 EditMode 覆盖：`BattleTargetingSpeedButtonStateServiceTests`、`BattleSharedEnergyRoundStartServiceTests`、`BattleSharedEnergyReservationCollectorServiceTests`、`BattleAllySkillEnergyAvailabilityServiceTests`、`BattleTargetingEnemyAutoTargetServiceTests`、`BattleTargetingPhaseLogServiceTests`。
- 验证完成：`dotnet build Assembly-CSharp-Editor.csproj --no-restore --no-incremental` 通过（仅既有 CS0649 与 MCP package CS0219 warning）；`dotnet build MobileGame.PlayModeTests.csproj --no-restore --no-incremental` 通过；目标 EditMode 21/21 通过；关键 PlayMode 2/2 通过；全量 PlayMode 9/9 通过；全量 EditMode 481/481 通过；Unity Console 0 error；相关文件 `git diff --check` 无格式错误，仅保留 Windows 行尾提示。
- 当前 `BattleButton.cs` 约 4757 行，`Assets/Scripts/Shared` 下 `Battle*.cs` 服务约 111 个。下一步建议转入 Targeting/Setup 剩余副作用收口：`RegisterSharedEnergyListeners` 事件绑定、round-start 敌我速度/技能选择调度，以及 `TryHandleBattleCompletion` 周边结算入口继续拆分。
## 2026-04-30 - BattleButton 事件绑定收口 Stage 331-340

- Stage 331-335：新增 `BattleActiveRoundCombatantEventBindingService`，将 active round 友方/敌方 `Died` 事件的去重绑定与解绑从 `BattleButton` 本地循环迁出；`BattleButton` 保留死亡 handler 与薄调用入口。
- Stage 336-340：新增 `BattleSharedEnergyListenerBindingService`，统一绑定/解绑友方 `SelectedSkillChanged`、友方/敌方 `StatusChanged` 与友方速度按钮 `ConnectionChanged`，并在清理 active round combatants 时主动解绑共享能量相关监听，降低跨回合残留事件风险。
- 新增 EditMode 覆盖：`BattleActiveRoundCombatantEventBindingServiceTests` 与 `BattleSharedEnergyListenerBindingServiceTests`，覆盖重复绑定不重复触发、解绑后不触发、多个速度按钮连接事件绑定。
- 验证完成：先确认 RED 为缺失 service；随后 `dotnet build Assembly-CSharp-Editor.csproj --no-restore --no-incremental` 通过（仅既有 CS0649 与 MCP package CS0219 warning）；`dotnet build MobileGame.PlayModeTests.csproj --no-restore --no-incremental` 通过；目标 EditMode 4/4 通过；关键 PlayMode 3/3 通过；全量 PlayMode 9/9 通过；全量 EditMode 485/485 通过；Unity Console 0 error；相关代码/测试文件 `git diff --check` 无格式错误，仅保留 Windows 行尾提示。
- 当前 `BattleButton.cs` 约 4699 行，`Assets/Scripts/Shared` 下 `Battle*.cs` 服务约 113 个。下一步建议进入 Stage 341+：拆 round-start setup 调度，把友方速度/技能选择、敌方速度/技能选择、battle-start effect 和 repeat round-start effect 从 `SetupTargetingPhase` 主流程中继续迁出。
## 2026-04-30 - BattleButton 目标指定回合开始调度拆分 Stage 341-355

- Stage 341-345：按 TDD 新增 `BattleTargetingRoundStartSetupServiceTests`，先通过 Unity 编译红灯确认缺少 `BattleTargetingRoundStartSetupService`；测试锁定友方回合开始准备必须跳过空/死亡单位、生成速度、触发玩家回合开始遗物回调、选择默认技能并返回速度日志。
- Stage 346-350：新增 `BattleTargetingRoundStartSetupService.PrepareAllies`，将友方速度生成、回合开始遗物触发、默认技能选择和友方速度日志构建从 `BattleButton.SetupTargetingPhase` 主流程迁出；`BattleButton` 仅保留按返回顺序写入 `BattleLog`。
- Stage 351-354：新增 `BattleTargetingRoundStartSetupService.PrepareEnemies`，将敌方速度生成、随机技能选择和敌方速度日志构建迁出；保持共享能量监听注册/预占刷新仍位于友方准备之后、敌方准备之前，避免改变能量 UI 与技能切换监听时序。
- Stage 355：完成验证。`dotnet build Assembly-CSharp-Editor.csproj --no-restore --no-incremental` 通过（仅保留既有 CS0649 与 MCP package CS0219 warning）；`dotnet build MobileGame.PlayModeTests.csproj --no-restore --no-incremental` 通过 0 warning / 0 error；目标 EditMode 19/19 通过；关键 PlayMode 3/3 通过；全量 PlayMode 9/9 通过；全量 EditMode 488/488 通过；Unity Console 0 error；相关文件 `git diff --check` 无格式错误，仅保留 Windows 行尾提示。
- 当前 `BattleButton.cs` 约 4683 行，`Assets/Scripts/Shared` 中 `Battle*.cs` 服务 114 个。下一步建议进入 Stage 356+：继续拆 `SetupTargetingPhase` 中 battle-start ally status grants、enemy battle-start effects、enemy repeat round-start status gains 与 battle setup/round-start relic 调度，但这些涉及真实状态/遗物副作用，应继续按小切片和 PlayMode 回归推进。
## 2026-04-30 - BattleButton 目标指定阶段副作用调度拆分 Stage 356-435

- Stage 356-375：新增 `BattlePendingBattleStartAllyStatusGrantService`，将战斗开始前待施加友方状态的 encounter guard、一次性 applied 标记、空 grant/空 ally 安全返回和实际授予调度从 `BattleButton.ApplyPendingBattleStartAllyStatusGrants` 中迁出；`BattleButton` 只保留日志闭包。
- Stage 376-395：新增 `BattleEnemyBattleStartSkillGrantService`，将敌方战斗开始临时技能授予流程迁出，包括清理友方临时技能、跳过空/死亡敌人、跳过无效 grant、只给存活友方授予技能，并按每个有效 grant 写日志。
- Stage 396-415：新增 `BattleEnemyTargetingStatusGainPhaseService`，统一敌方 battle-start status gains 与 repeat round-start status gains 的目标过滤和调度顺序；repeat round-start 保持 round 1 不触发，后续回合维持 status gain -> custom repeat effect -> status-driven transition 的原顺序。
- Stage 416-435：新增 `BattleTargetingPhaseRelicService`，将 battle setup relic 与 battle round-start relic 的“是否有遗物、处理 context、恢复共享能量、flush feedback、首回合额外速度按钮”规则从 `BattleButton` 中迁出；`BattleButton` 继续保留真实 `RelicTriggerDispatcher`、`RestoreSharedEnergy` 和 `BattleRelicEffectRunner.FlushFeedback` 副作用闭包。
- 新增 EditMode 覆盖：`BattleTargetingPhaseStartEffectServiceTests` 覆盖 pending ally status grants、enemy battle-start skill grants、enemy battle-start/repeat round-start status 调度、battle setup relic extra buttons 与 round-start relic energy recovery 顺序。
- 验证完成：`dotnet build Assembly-CSharp-Editor.csproj --no-restore --no-incremental` 通过（仅既有 CS0649 与 MCP package CS0219 warning）；`dotnet build MobileGame.PlayModeTests.csproj --no-restore --no-incremental` 通过 0 warning / 0 error；新增定向 EditMode 6/6 通过；目标组合 EditMode 16/16 通过；关键 PlayMode 4/4 通过；全量 PlayMode 9/9 通过；全量 EditMode 494/494 通过；Unity Console 0 error；相关文件 `git diff --check` 无格式错误，仅保留 Windows 行尾提示。
- 当前 `BattleButton.cs` 约 4616 行，`Assets/Scripts/Shared` 中 `Battle*.cs` 服务 116 个。下一步 Stage 436+ 建议继续处理 `SetupTargetingPhase` 中 `ApplyRoundStartStatusEffectsGeneric` 与 `TryHandleRoundStartRetreatSettlement` / `TryHandleBattleCompletion` 周边入口，把 round-start status tick 与回合开始提前结算 guard 继续向 dispatcher/facade 收口。
## 2026-04-30 - BattleButton 主体重构 Stage 436-560

- Stage 436-455：新增 `BattleRoundStartStatusEffectService`，将回合开始友方/敌方状态 tick 与敌方状态驱动阶段转换顺序从 `BattleButton.ApplyRoundStartStatusEffectsGeneric` 迁出，`BattleButton` 只保留真实状态消费、表现和日志闭包。
- Stage 456-475：新增 `BattleRoundStartCompletionGuardService`，统一回合开始撤退结算与普通战斗完成判定入口，保持撤退结算优先于普通完成判定。
- Stage 476-500：扩展 `BattleTargetSelectionService.Select`，统一实例与静态目标选择 wrapper，并补齐 CurrentTarget、AllOpponents、RandomOpponents、无效目标过滤和随机数量溢出测试。
- Stage 501-530：新增 `BattleSkillStatusEffectResolutionService` 与通用 `BattleIndexedEffect<T>`，将 status-only 技能词条的触发筛选和原始索引保留迁出，避免日志 fallback index 在过滤后错位。
- Stage 531-560：新增 `BattleEnemyBuiltInEffectResolutionService`，将敌方 built-in effect 的 trigger / eligibility / condition 筛选迁出，`BattleButton` 继续负责真实治疗、伤害、状态、转移等副作用执行。
- 新增/扩展 EditMode 覆盖：`BattleRoundStartStatusEffectServiceTests`、`BattleRoundStartCompletionGuardServiceTests`、`BattleTargetSelectionServiceTests`、`BattleSkillStatusEffectResolutionServiceTests`、`BattleEnemyBuiltInEffectResolutionServiceTests`。
- 验证：`dotnet build Assembly-CSharp-Editor.csproj --no-restore --no-incremental` 通过（仅既有 warning）；`dotnet build MobileGame.PlayModeTests.csproj --no-restore --no-incremental` 通过；Unity EditMode 505/505 通过；Unity PlayMode 9/9 通过；Unity Console 0 error。

## 2026-04-30 - BattleButton 主体重构 Stage 561-710

- Stage 561-590：新增 `BattleHitResolutionFacade`，统一命中后零伤反馈、成功命中效果和未命中效果的分派入口，`BattleButton` 不再直接组合 hit outcome plan 与 dispatcher。
- Stage 591-620：新增 `BattleCombatPresentationCoroutinePlanService` 与 `BattlePresentationStep`，将拼点/单方面攻击表现协程的移动、掷骰、影响结算、归位步骤显式化；真实动画仍由现有 `BattlePresentationSystem` 执行，避免改变表现时序。
- Stage 621-650：新增 `BattleButtonUiOverlayStateService`，替代旧 `BattleCombatPhaseGuardService`，将结算面板、教程阻塞、技能详情面板的按钮阻塞原因显式化，并删除旧 guard 服务与测试。
- Stage 651-680：补充 PlayMode 回归，覆盖回合开始状态 tick、回合开始撤退结算、复杂技能 OnUse + OnHit 状态效果各触发一次。
- Stage 681-710：清理已替代的旧 guard 服务；保留仍被 `BattleButton` 内部多处调用的 private wrapper，避免在同一轮扩大重构风险。
- 新增/扩展测试：`BattleHitResolutionFacadeTests`、`BattleCombatPresentationCoroutinePlanServiceTests`、`BattleButtonUiOverlayStateServiceTests`，以及 `BattleFlowPlayModeTests` 三条战斗流程回归。
- 验证：本段提交前执行 dotnet build、Unity EditMode、Unity PlayMode 与 Console 检查，结果记录在本轮对话结论中。

## 2026-04-30 - BattleButton 轻量编排器策略调整 Stage 711-820/901/981 部分收口

- 策略调整：停止“一个小规则一个新 service 文件”的拆法，改为把新 dispatcher/facade 合并进已有领域服务文件，优先降低 `BattleButton` 职责而不继续膨胀文件数量。
- 敌方 built-in effect：在既有 `BattleEnemyBuiltInEffectResolutionService.cs` 内合并新增 `BattleEnemyBuiltInEffectDispatcher`，将 heal/damage/status/transfer 的类型分派与目标遍历从 `BattleButton` 迁出。
- 技能效果执行：扩展既有 healing/status burst/status stack operation dispatcher，收口友方/敌方重复循环，`BattleButton` 只保留真实 Heal/TakeDamage/状态修改与日志闭包。
- 回合结束与结算：扩展 `BattleCombatRoundEndDispatcher` 统一 round-end status 消耗过滤；扩展 `BattleSettlementPartyMemberFactory` 统一 party progression 构建，删除 `BattleButton` 中部分 legacy wrapper 和死代码。
- 当前 `BattleButton.cs` 约 3773 行，`Assets/Scripts/Shared` 下 `Battle*.cs` 仍约 123 个，未新增生产 cs 文件。
- 验证：本阶段提交前执行 dotnet build、目标 EditMode、Unity 全量 EditMode/PlayMode 和 Console 检查，结果记录在本轮对话结论中。


## 2026-10-27 - 共用正式规则的无界面 Agent 战斗接口（第一阶段）

- 新增编辑器后端 `AgentBattleServer`、`AgentBattleSession` 和 JSON 协议，通过 localhost HTTP 创建单场战斗、读取局面与合法行动、切换技能、连接目标、提交回合、导出重放和关闭会话。
- `BattleButton.Headless` 直接驱动既有回合准备与结算协程；仅跳过演出、HUD 和音频，命中回调仍执行。正式界面保留原入口，不复制伤害、状态或遗物规则。
- `SpeedButton.TryAssignTarget` 复用正式的速度与拼点连线判定；技能说明复用游戏内文案生成器。专用沙盒场景不保存会话对象，创建后恢复原活动场景。
- 接入既有战斗生成器的敌方召唤和临时友方召唤，共用无视觉组件工厂；支持固定种子、请求版本、幂等重试、回合与事件上限，以及异常后的终止状态。
- 增加 Python 标准库客户端、OpenAPI 和 `docs/agent-battle/README.md`。当前仍依赖团结/Unity 编辑器，可用 batchmode/nographics 托管；并非独立 .NET 内核。暂不提供完整冒险、任意状态快照或并行会话，获得时遗物效果明确拒绝。
- 验证：新增 EditMode 14/14 通过。全量当前 817 项为 794 通过、23 失败；临时使用本次修改前源代码对照，基线 803 项为 780 通过、23 失败，失败项完全一致。所有修改均已逐字节恢复。
- HTTP 端到端完成木剑、燃烧、中毒、电流和召唤 5 组对战，并全部从开局重放一致；巨型史莱姆的分裂与断线傀儡召唤进入真实运行时流程。
- PlayMode 12 项为 3 通过、9 失败，主要为演出超时及教程断言；原代码对照也出现同样的演出超时，但在 7/12 后取消，未取得完整基线。保留此限制，不将图形化回归报告为通过。
- 详细验证结果保存在 `outputs/agent-battle/verification-summary.json`。未保存或覆盖当前用户场景；后续按领域模块提取规则，正式界面和接口共同切换到提取后的实现。


## 2026-10-27 - Agent adventure B002

- Added isolated adventure encounter bridge, shared map settlement writer, reward choice and refresh service, and deterministic acquisition RNG. UI and agent use the same rules.
- Fixed persistent party IDs being overwritten by display names during settlement.
- Validation: 46/46 targeted EditMode tests passed; HTTP wood-sword battle won in 3 rounds, replay matched. Full EditMode: 828 total, 804 passed, 24 failed (23 prior failures plus an EventSystem.current error in BattleExitFlowServiceTests, reproduced alone). Full suite is not green; existing PlayMode limitations remain.
- B001 checkpoint: 4186339. B002 bridge is internal; B003 will expose normal adventure creation and movement. Full completion remains governed by docs/agent-battle/PLAN.md. Evidence: docs/agent-battle/B002-validation.json.

## 2026-10-27 - Agent adventure B003a: shared map primitives

- Extracted normal run bootstrap, ability starting party/inventory, exhausted-cycle and key callbacks into shared services used by RunGameManager.
- Added saved-config reader and isolated map runtime; existing BoardManager and PlayerMapController skip visuals/animation only. No duplicate map generator or AP rules.
- Validation: 7 new map tests; related targeted EditMode 40/40 passed (be0938a0ccd1425399f6800b78589347). Real NormalConfig/Medium generated 81 tiles and 3 keys; one-point overflow, key idempotency, seeded maps, rejected moves, carried battle state and starting bonuses covered.
- Shipped Inner/Core enemy pools are empty; ConfigurationIssues reports missing groups instead of synthesizing content. B003b still needs HTTP adventure decisions and calamity handling.

## 2026-10-27 - Agent adventure B003b: HTTP map/battle/calamity loop

- Added create_run/observe_run/run_* commands and a stdlib client with legal-action-driven map and battle decisions. Single battle and adventure sessions are mutually exclusive.
- Shared calamity teleport/relic loss/rest conversion and visited-empty ambush implementations with RunGameManager. Deferred headless arrival until AP/relic cost callbacks finish, including overflow. Settlement response now exposes actual post-level-up party state.
- Validation: targeted EditMode 60/60 passed (09e548a4de3e4cd8a1ccaf100f3568b9). Public HTTP normal map route won a battle, returned with level 2 and HP 18/19, then exhausted AP and selected an actual calamity in cycle 2. Four invalid requests rejected; duplicate move returned cached result. Python syntax and OpenAPI references validated.
- Incomplete: location decisions/transactions, final settlement, isolated meta progression, save/restore and full replay. No placeholder rewards. Full-suite/PlayMode limitations remain as recorded in B002-validation.json. See docs/agent-battle/PLAN.md and B003b-validation.json.

## 2026-10-27 - Agent adventure B004a: merchant and forge

- Shared merchant price/affordability evaluation, stock/refresh orchestration, and forge validation/resource consumption with formal UI. Agent observations quote on cloned state without advancing RNG.
- Added purchase/refresh/craft actions and offer/recipe/material observations. Duplicate ingredient rows are aggregated before checking and consuming materials; owned blueprint and calamity fee validation is shared.
- Validation: 85/85 related EditMode tests plus 2/2 formal merchant UI tests passed. Public HTTP NormalConfig seed 46: battle income 21, iron ore purchase 6, remaining money 15, level 2 HP 19/19, returned to map in 14 actions. No normal-route resource injection.
- B004b still needs rest, recruitment, reforge and equipment management. Full completion, full replay and final settlement remain incomplete. See B004a-validation.json and PLAN.md.

## 2026-10-27 - Agent adventure B004b1: rest, recruits and equipment

- Added explicit snapshot overloads for formal rest/AP/recruit/reforge writers; global UI wrappers delegate to the same bodies. Recruit candidate selection/cost is shared with the formal rest scene.
- Added rest heal/AP/recruit actions and map equipment actions. Consumed rest sites cannot be reused; AP and deferred calamity queues restore to runtime components after snapshot writes. New party members and actual equipped skills reach the existing battle core.
- Validation: targeted EditMode 68/68 passed (dacab3f483604defafe3aad18b18a8d9). Public HTTP normal seed 32: HP 16/19 to 19/19 after a battle with two legal untargeted rounds; separate AP route 1 to 6. Each used the rest site once and returned to map in 17 actions. No normal-route resource injection.
- B004a committed b50b39d5. B004b2 still needs reforge selection/preview/limits and visit locking. Full adventure completion remains unproven; existing full-suite/PlayMode limitations remain. See B004b1-validation.json and PLAN.md.

## 2026-10-27 - Agent adventure B004b2: staged rest reforge

- Shared formal UI material validation, candidate filters, deterministic seeds/rolls and limit rules in RunRestService. Loaded actual reforge tables/attributes from the saved rest scene.
- Added staged loot/relic reforge actions and formal preview information. Selection/observation is read-only; first success commits the visit, preventing other rest actions; limit exhaustion automatically consumes the rest site. Acquisition and reforge effects use the existing explicit snapshot writers.
- Validation: related EditMode 90/90 passed (fb9a955a75d24a468d32c4120afb3fbf), including 13 new tests. Public HTTP normal seed 46: two slime mucus drops plus purchased iron ore, two reforges, one Corroded Fang result, automatic return to cycle 2 map in 28 actions; no normal-route state injection.
- B004b1 committed 99302948. B005 events/treasure/subdungeons, final settlement and full save/replay remain incomplete. Existing full-suite/PlayMode limitations remain. See B004b2-validation.json and PLAN.md.


## Agent adventure B005a - 2026-10-27

Shared UI/agent event selection costs, effects and completion; frozen discard previews; result acknowledgment, weighted follow-ups and actual event encounters with deferred victory rewards. 12 new tests; related EditMode 100/100 passed (job f64daf9b12d3472291a51cf4b294c578). Normal HTTP seed 1234 verified all three Broken Bridge options and repeated repair yielding 31 gold. Known nested save-catalog failure remains recorded. Treasure, special dungeons, final settlement and full save/replay remain incomplete. See docs/agent-battle/B005a-validation.json.


## Agent adventure B005b - 2026-10-27

Shared treasure tile reward flow; money/blueprint/relic bags, one-time selection, result acknowledgment and deferred calamities. Shared bag refresh pool and formal UI refresh control; fixed EditMode card cleanup. 14 new tests; related EditMode 112/112 passed in an isolated -batchmode -nographics host with matching source hashes. Normal HTTP seed 32 verified 39 gold, a blueprint, and a chosen relic from treasure (8,3). Original editor remained in domain reload; no restart/save of user scenes. Dungeon traversal, final settlement/meta and full save/replay remain pending. See docs/agent-battle/B005b-validation.json.


## 2026-10-27 B005c - shared special dungeon traversal

- Shared UI/API dungeon context, zero-AP movement, validated left/right exits, score once, main board/AP/carry restoration and deferred calamities once.
- Repaired three authored links: Door entry, Maze effect type, Door final bag relic pool (unchanged rarity/count). Shared direct-money event preflight prevents unaffordable partial HP effects.
- Isolated Tuanjie batchmode/nographics: final EditMode 921 total, 900 passed, 21 failed; baseline HEAD 983d6816: 899 total, 878 passed, same 21 failing names. All 22 new tests and 142 selected related cases pass.
- Normal HTTP seed 127: real treasure income 77, recruit Jenny for 50, two Door battle victories, relic reward, right exit score 200 and AP 1->1; fresh left retreat score 0. No resource/outcome injection.
- Maze probe remains blocked by an authored missing relic reward at the rest room. Existing roster gaps and PlayMode limits remain.
- Evidence: docs/agent-battle/B005c-validation.json. Next: B006 shared victory/defeat/abandon settlement, isolated meta and next run. Goal remains active; full save/replay and full external-agent audit are still pending.


## 2026-10-27 B006a - shared isolated settlement context

- Added RunSettlementContext and an explicit-context overload on the existing settlement service. Formal runtime delegates to its default context. Explicit callers must provide both wallet callbacks before side effects; no fallback to player settlement/wallet.
- Same-context retry does not award/clean twice. Separate contexts remain independent; score formula unchanged. Settlement data is serializable.
- Isolated headless EditMode: related 18/18, 7 new pass; full 928 total, 907 passed, same 21 failed names as B005c (921/900/21). Evidence: docs/agent-battle/B006a-validation.json.
- This is a primitive-only batch; no public terminal command yet. Formal map abandon grants no points; battle abandon settles as not cleared. B006b must preserve these policies and implement isolated meta/next run, then B007 full saves/replays and B008 normal full-run audit. Goal remains active.


## 2026-10-27 B006b1 - terminal API, isolated abilities and next run

- Added run_settle/run_abandon/run_next, isolated profile unlock/toggle, and ability-provided starting relic choice/refresh. Reuses shared score, ability, bootstrap and acquisition services. Real UTC short-run scoring applies. Map abandon gives no points; battle abandon uses non-clear scoring.
- Public HTTP normal seed 1234: real defeat -> return -> settlement, exact request retry unchanged, zero-cost root unlock -> next run. Both abandon policies verified, plus explicit isolated profile close/reopen. No player save/wallet access.
- Isolated final full EditMode: 942 total, 921 passed, same 21 failing names as B006a. All 14 new tests and 116 selected related cases pass. Directed resources/Boss fixture are not normal-win evidence.
- Normal seed 127: real treasure income/recruitment, two ordinary wins, third recruit and healing, two keys, full-health three-person party enters configured ThunderLord; actual defeat in Boss round 2. Seed 46 preparation also failed. Traces retained. No forced win/resource injection.
- Evidence: docs/agent-battle/B006b1-validation.json. B006b2 still requires true normal Boss victory -> earned points -> paid upgrade -> next run, followed by B007 full save/replay and B008 external-agent audit. Goal remains active.


## 2026-10-27 B006b2 in progress - real-frame client policy repairs

- Added shared visible-state client policy: replace unaffordable persisted skills, prefer reciprocal counters before redirects, and discount self-harm without counting it as outgoing damage. Demo imports this policy; no game C#/data changes.
- Six Python regressions from real normal Boss frames pass. Live normal Boss probe used only offered actions and targeted all actionable slots; still defeated in round 3, so the victory gate is NOT complete.
- Another actual seed-127 route won an outer elite, bought blueprint/materials, crafted/equipped Gale dagger, recovered the downed Molly through formal rest, collected two keys, then lost to Boss. Actual score 653, zero points, duration 652 seconds. No state/resources/outcome injection.
- Evidence: docs/agent-battle/B006b2-progress.json. Continue preparation path/calamarity planning, equipment skill observability and real normal victory -> earned paid upgrade -> next run. B007/B008 remain. Goal stays active.


## 2026-10-27 B006b2 in progress - equipment observability

- Public catalog adds weapon/character/blueprint details: skills, conditions, base action slots, type, target weapon and shared aggregated requirements. Adventure exposes the actual forge fee before recipe ownership/location entry.
- Catalog and live combat reuse one skill reader/formal formatter. Inactive preview-scene formatting creates no UI and preserves input locks, scene and RNG. Live displayedDice uses the same status-adjusted controller getters as UI; authored dice remains separate. No outcome prediction.
- Added inspect-equipment public-catalog CLI and UTF-8 pipe output; policy prefers displayed ranges with old-observation fallback.
- Final isolated headless EditMode: 948 total, 927 passed, same 21 failing names; all 6 new and 47 selected related cases pass. Python 6/6. Real HTTP read-only live battle/catalog, blueprint inspection and normal actual forge inflation (40 before owning recipe/entering forge) verified.
- Evidence: docs/agent-battle/B006b2-equipment-validation.json. Normal victory/earned paid upgrade remains pending; B007 save/replay and B008 full audit remain. Goal stays active.


[2026-10-27 B007a] Added timestamped full-session adventure journals, source/engine/API version guards, explicit replay slots and isolated replay verification. Public HTTP verified 28 normal-content checkpoints across dungeon and equipment/Boss-defeat routes; 144 commands verified again after validation-host restart. EditMode: 964 total, 943 passed, same 21 baseline failures; all 16 new tests passed. Python: 6/6. Evidence: docs/agent-battle/B007a-validation.json. Playable restore, cross-process settlement transactions and normal Boss victory remain pending; goal remains active.


[2026-10-27 B007b] Added playable named checkpoints and automatic persistent-profile recovery using the existing shared-rule replay. Progression and accepted commands commit in one atomic head; working wallet/ability files roll back to that head after interruption. Added owner/generation stale-save guards, profile leases, response-cache reconstruction, checksum/legacy validation and bounded Windows replacement retries. Final EditMode: 986 total, 965 passed, same 21 baseline failures; all 22 new tests passed. Public HTTP: 48 save/load/continue checkpoints across normal dungeon, equipment/Boss-defeat and reforge routes; actual forced validation-host termination recovered the last unsaved battle command and settlement retries did not reexecute. CLI smoke and 6 Python policy tests passed. Evidence: docs/agent-battle/B007b-validation.json. Normal Boss victory/earned paid upgrade and victory-settlement restore remain pending; next batch returns to B006b2 before B008 completion audit. Goal remains active.
