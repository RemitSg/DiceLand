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
