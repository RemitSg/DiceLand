# 面试复习：真实 Bug 故事、能力树与灾厄系统

## 一、真实 Bug 故事：商店刷新出玩家已经不需要的图纸

### 1. 一句话版本

我在商店系统里修过一个“图纸重复刷新”的问题：商店会刷出玩家背包里已有的图纸，或者刷出玩家已经拥有对应武器的图纸。最后定位到候选池过滤规则只按商品自身去重，没有把“图纸 -> 目标武器”这个跨背包关系纳入排除集，于是我把商店刷新前的过滤逻辑抽成统一服务，并补了候选池测试。

### 2. 现象

玩家进入商店后，偶尔会看到已经拥有的图纸再次出现在商品列表中。

更隐蔽的情况是：玩家虽然没有某张图纸，但已经通过其它方式获得了这张图纸对应的武器，商店仍然会刷新出这张图纸。对玩家来说，这个商品已经没有实际价值，还会占用商店刷新位。

### 3. 定位过程

我先从现象反推链路：商店商品不是 UI 写死的，而是在地图商店格进入时生成，刷新按钮也会重新生成部分商品。

主要排查路径是：

- `RunGameManager` 打开商店时会调用 `EnsureMerchantStock`。
- 初始商店库存由 `MerchantStockService.TryGenerateInitialStock` 生成。
- 刷新商店走 `MerchantStockService.RefreshOffers`，然后按商品类型进入 `TryRefreshLootOffer`、`TryRefreshWeaponOffer`、`TryRefreshBlueprintOffer` 等分支。
- 商品表来自 `MerchantCatalog_SO`，其中图纸商品入口是 `BlueprintGoodsEntry.blueprintData`。
- 玩家当前拥有物在 `RunState` 里，图纸是 `blueprintInventory`，武器是 `weaponInventory`。

排查后发现：商店已经有“当前货架去重”和“已有遗物排除”的思路，但图纸排除集不完整。它只知道排除图纸本身，没有进一步判断 `CraftingBlueprint_SO.targetWeapon` 是否已经在玩家武器背包中。

### 4. 根因

这个 bug 的根因不是随机算法，而是业务语义没有统一：

- 从数据角度看，图纸和武器是两个不同背包字段：`blueprintInventory` 和 `weaponInventory`。
- 从玩法角度看，“拥有图纸”和“拥有图纸对应的武器”都意味着这张图纸不应该再作为商店商品出现。
- 原逻辑更偏“同类型物品排重”，没有把这种跨类型关系抽象出来，所以初始生成和刷新流程都可能漏掉。

### 5. 修复方案

我把“商店刷新时哪些东西不可再出现”抽成明确的排除集构建逻辑：

- `MerchantOfferExclusionService.BuildOwnedBlueprintSet(runState)`：收集玩家已有图纸。
- `MerchantOfferExclusionService.BuildOwnedWeaponSet(runState)`：收集玩家已有武器。
- `MerchantOfferExclusionService.BuildUnavailableBlueprintSet(runState, validBlueprintEntries)`：先加入已有图纸，再遍历所有图纸候选，如果图纸的 `targetWeapon` 已经存在于玩家武器背包，也加入不可用集合。
- `RewardCandidateFilterService.BuildAvailableMerchantBlueprintGoodsEntries(...)`：根据不可用集合过滤真正可刷新的图纸商品。

然后在两个入口都使用这套规则：

- 初始生成：`MerchantStockService.TryGenerateInitialStock`
- 刷新商品：`MerchantStockService.RefreshOffers`

刷新时还要额外把当前货架上的图纸也加入排除集，避免同一次刷新里出现重复商品。

### 6. 验证

我补了几类测试来覆盖边界：

- `MerchantOfferExclusionServiceTests.BuildUnavailableBlueprintSet_IncludesBlueprintsForOwnedWeapons`：验证“已有武器对应图纸”会被排除。
- `RewardCandidateFilterServiceTests.BuildAvailableMerchantBlueprintGoodsEntries_FiltersMissingAndExcludedBlueprints`：验证候选过滤会排除空配置和不可用图纸。
- `MerchantStockServiceTests`：覆盖初始库存生成和刷新时不会再把已有图纸、已有武器对应图纸刷出来。

这个修复比较关键的一点是：没有只在 UI 或某个刷新分支里临时判断，而是把规则沉到候选池服务里，这样初始生成、刷新、后续其它奖励系统都更容易复用同一套语义。

### 7. 学到什么

这次问题让我意识到，背包系统里的“重复”不一定是同一个对象引用重复，也可能是玩法意义上的重复。图纸和武器在数据结构上是两类物品，但在商店投放规则里需要按“是否还对玩家有价值”来判断。

后续我会更倾向于先把奖励、商店、掉落、重铸这类系统的候选池过滤规则收敛到统一服务，再让具体系统调用，而不是每个系统各写一段局部过滤逻辑。这样更容易测试，也能减少内容扩展后的边界 bug。

## 二、能力树系统简要说明

### 1. 系统目标

能力树是局外成长系统。玩家在一局结束后根据结算得分获得能力点，然后在主菜单的能力界面里解锁节点。解锁后的能力会在下一次普通开局时转化为起始资源、商店优惠、战斗开局加成、初始图纸或遗物袋等奖励。

### 2. 核心数据结构

能力树使用 ScriptableObject 配置，运行时只读取配置和玩家存档：

- `AbilityTreeConfig_SO`：能力树总配置，内部是节点列表。
- `AbilityNodeDefinition`：单个节点，包含 `nodeId`、显示名、描述、消耗、UI 坐标、前置节点、是否可开关、互斥组和效果列表。
- `AbilityNodeEffect`：节点效果，核心字段是 `effectType`，其余字段按效果类型选择使用，例如 `intValue`、`blueprintData`、`blueprintTable`、`lootTable`、`relicTable`、`characterData`、`weaponData`。
- `AbilityEffectType`：效果枚举，例如起始金币、起始行动点上限、起始图纸、商店免费刷新、首次购买半价、战斗开局能量、初始队伍人数、初始武器替换等。

这种设计的好处是：能力树节点本身不写死逻辑，只描述“这个节点带来哪些效果”；真正的效果聚合和应用由 service 负责。

### 3. 解锁与持久化

能力点和节点进度是分开存的：

- `AbilityPointWallet` 管理能力点，落盘到 `ability_points.json`。
- `AbilityProgressionService` 管理解锁节点和开关状态，落盘到 `ability_progression.json`。

解锁时大致流程是：

1. 主菜单通过 `MainMenuSceneController` 打开 `AbilityMenuOverlay`。
2. `AbilityMenuOverlay` 创建 `AbilityProgressionService`。
3. 点击节点时调用 `GetNodeState` 判断是锁定、可解锁还是已解锁。
4. `TryUnlock` 检查前置节点、检查能力点是否足够，然后扣点并保存解锁 ID。
5. 如果节点是可开关节点，`SetNodeEnabled` 会记录启用状态；同一互斥组内只保留一个启用节点。

### 4. 开局时如何生效

开始普通 run 时，`RunGameManager` 会通过 `AbilityProgressionService.BuildRunBonus()` 把所有已解锁且启用的节点聚合成一个 `AbilityRunBonus`。

之后分两层应用：

- 持续型局内字段：`AbilityRunBonusRuntimeService.ApplyPersistentRunBonuses` 写入 `RunState`，例如商店免费刷新次数、首次购买半价、战斗开局能量、遗物选择刷新次数、宝藏奖励表替换等。
- 一次性开局奖励：`ApplyStartingBlueprints`、`ApplyStartingLootTableRolls`、`BuildStartingRelicBagChoices` 等把图纸、材料、遗物袋等奖励发到当前 run。

这样能力树不会直接依赖具体 UI 或战斗按钮，而是把结果先转成 `RunState` 或开局奖励数据，再由地图、商店、战斗、结算等系统自然读取。

### 5. 编辑器与校验

能力树还有编辑器工具：

- `AbilityTreeConfigEditor`：在 Inspector 中做节点配置和校验。
- `AbilityTreeEditorWindow`：可视化编辑节点位置和连线。
- `AbilityProgressionService.ValidateTree`：校验重复 ID、空节点、负消耗、缺失前置、循环依赖等问题。

这里比较重要的是运行时和编辑器共用同一套校验服务，避免编辑器看起来合法、运行时却读不动。

### 6. 面试可讲的设计点

能力树的架构重点是“配置、进度、运行时加成”三层拆开：

- 配置层只负责描述节点和效果。
- 进度层只负责玩家解锁了什么、启用了什么、剩余多少能力点。
- 运行时层只负责把已解锁效果聚合成 `AbilityRunBonus`，再注入新 run。

这样做的好处是新增节点大多只需要配表；新增效果时只扩展 `AbilityEffectType`、聚合逻辑和对应运行时应用点，不需要改 UI 的基础流程。

## 三、灾厄系统简要说明

### 1. 系统目标

灾厄系统是地图探索层的压力机制。玩家在地图上移动会消耗行动点；行动点耗尽后不会直接失败，而是提高灾厄等级，让玩家从若干灾厄中选择一个承受。灾厄会持续影响后续地图、商店、休息、锻造和战斗。

### 2. 核心数据结构

灾厄系统主要由三类数据组成：

- `Calamity_SO`：单个灾厄配置，包含 `calamityId`、名称、等级、描述、稀有度、图标、内置效果列表和可选自定义 handler。
- `CalamityBuiltInEffectEntry`：灾厄内置效果条目，由 `effectType`、数值和状态数据组成。
- `CalamityPool_SO`：按灾厄等级组织灾厄候选池，负责按等级抽取选择项。

运行时状态存在 `RunState` 中：

- `calamityTier`：当前灾厄等级。
- `activeCalamities`：已经生效的灾厄列表，每个元素是 `RunCalamityState`。
- `drawnCalamityIds`：已经展示过的灾厄 ID，用于避免重复抽到。

### 3. 触发流程

灾厄触发入口在地图行动点系统：

1. 玩家移动消耗行动点。
2. 行动点耗尽后触发 `RunGameManager.HandleActionPointsExhausted`。
3. `RunState.calamityTier` 和 `currentCycle` 增加。
4. 新的灾厄等级进入 `PendingCalamityTiers` 队列。
5. 如果当前没有战斗、商店、事件、宝藏、锻造、教程弹窗等遮挡，就调用 `TryPresentPendingCalamity`。
6. `CalamitySelectionService.RollChoices` 从 `CalamityPool_SO` 中抽取候选。
7. `MapCalamityOverlay` 展示两个左右的灾厄选择卡。
8. 玩家选择后，`HandleCalamityChosen` 把灾厄加入 `RunState.activeCalamities`，执行地图即时效果、自定义 handler 和遗物触发。

这里使用了 `DeferPendingCalamityUntilMapResume` 和 `DeferPendingCalamityUntilOverlayClosed` 之类的状态，避免玩家刚进入战斗、休息、商店或事件面板时，灾厄弹窗打断当前流程。

### 4. 灾厄效果如何生效

灾厄不是每个系统自己去遍历配置，而是集中通过 `CalamitySystem` 提供查询和修正方法。

典型接口包括：

- `ApplyEnemyMaxHpModifiers`
- `ApplyEnemyDamageModifiers`
- `ApplyBattleExperienceModifiers`
- `ApplyBattleMoneyModifiers`
- `ApplyMerchantPriceModifiers`
- `ApplyForgeCostModifiers`
- `ResolveRestHealPercent`
- `ResolveRestActionPointRestorePercent`
- `GetEnemyBattleStartStatusEffects`
- `ResolveVisitedEmptyTileAmbushChance`

战斗系统、商店系统、休息系统、锻造系统只需要在对应计算点调用这些接口，传入当前 `activeCalamities`，就能拿到灾厄修正后的数值。

例如：

- 战斗里敌人伤害、敌人最大生命、友方伤害、敌人受伤倍率都通过灾厄修正。
- 商店价格会经过灾厄价格倍率修正。
- 休息恢复量和行动点恢复比例可以被灾厄覆盖。
- 地图空地可能因为灾厄触发伏击。

### 5. 内置效果和自定义效果

灾厄效果分两层：

- 常规效果走 `CalamityBuiltInEffectType`，例如百分比加成、固定数值、开局状态、延迟死亡、地图格子转换等。
- 特殊效果走 `CalamityEffectHandler_SO`，它提供 `OnCalamityApplied` 和若干 `Modify...` 虚方法，适合处理少数不适合枚举化的复杂效果。

这样大部分灾厄可以通过配表完成，少数特殊灾厄才需要写代码扩展。

### 6. 抽取和排重策略

灾厄抽取时会先根据 `drawnCalamityIds` 构建排除集，保证已经展示过的灾厄不会反复出现。

`CalamityPool_SO.RollChoices` 会优先从请求等级抽取；如果当前等级没有可用灾厄，就向低等级回退。它不会为了凑满数量跨多个等级混抽，这样可以保证一次选择里的灾厄强度比较一致。

### 7. 面试可讲的设计点

灾厄系统的架构重点是“地图触发、状态持久化、统一修正器”：

- 地图层负责触发节奏：行动点耗尽后推进灾厄等级。
- `RunState` 负责保存已经承受的灾厄，场景切换和存档都可以恢复。
- `CalamitySelectionService` 负责抽取、排重和应用选择，避免逻辑散在 UI 里。
- `CalamitySystem` 作为统一修正器，让商店、战斗、休息、锻造等系统只依赖一个查询入口，而不是直接耦合灾厄配置结构。

这种做法降低了系统间耦合：战斗系统不需要知道灾厄怎么抽，商店系统不需要知道灾厄弹窗怎么显示，灾厄系统也不需要直接控制每个 UI。大家通过 `RunState.activeCalamities` 和 `CalamitySystem` 这个统一接口协作。

## 四、可直接背的压缩版

真实 bug 我会讲商店图纸重复刷新：玩家已有图纸或已经拥有对应武器时，商店仍可能刷出那张图纸。定位时我沿着 `RunGameManager -> MerchantStockService -> RewardCandidateFilterService` 查候选池，发现原逻辑只按图纸本身过滤，没有把 `blueprint.targetWeapon` 和玩家 `weaponInventory` 的关系纳入排除集。修复时我把不可用商品集合抽到 `MerchantOfferExclusionService`，把已有图纸、当前货架图纸、已有武器对应图纸统一排除，再让初始生成和刷新共用这套过滤。这个 bug 让我意识到奖励系统里的“重复”不只是对象重复，还包括玩法语义重复，所以候选池规则应该服务化、可测试、可复用。

能力树是局外成长系统，核心是 `AbilityTreeConfig_SO` 配节点，`AbilityProgressionService` 管解锁和持久化，`AbilityRunBonusRuntimeService` 在新 run 开始时把已解锁能力聚合成开局资源和局内加成。灾厄系统是地图压力系统，行动点耗尽后提升灾厄等级并进入待选择队列，玩家从 `CalamityPool_SO` 抽出的灾厄里选择一个，结果写入 `RunState.activeCalamities`，后续战斗、商店、休息和锻造都通过 `CalamitySystem` 统一读取灾厄修正。
