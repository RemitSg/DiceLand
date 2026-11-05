# 商店实时配置页

这是直接连接本项目 `MerchantCatalog_SO` 的本地 HTML 编辑器。Unity 编辑器提供实际资产、GUID 解析、保存和撤销；没有另存一份需要人工合并的商品 JSON。

## 打开

1. 在团结／Unity 2022.3 中打开本项目，等待编译完成。
2. 选择 **Tools → Economy → Open Live Merchant Editor**。
3. 浏览器打开 <http://127.0.0.1:8092/>。端口仅监听本机；无需 Node.js、数据库或联网资源。

正式页面为 `tools/merchant-catalog/dist/index.html`，同目录 JS、CSS 必须保留。双击 HTML 会显示连接入口，双向同步使用上述 HTTP 页面。Unity 运行期间服务保持可用，脚本重编译后在当前编辑器会话自动恢复；关闭 Unity 后需要重新从菜单打开。停止服务使用 **Tools → Economy → Stop Live Merchant Editor**。

## 双向同步

- **配置 → 页面**：约每秒读取实际资产。Inspector 中尚未落盘的修改也会显示；外部 `.asset` 文件变化会触发定向重新导入。名称、稀有度、描述、ID 和 GUID 从关联资产读取，新增资产也会进入“加入商品”列表。
- **页面 → 配置**：有效输入停止约 0.6 秒后自动保存，也可点击“立即保存”。商品总数、四类保底数量／补位权重、商品标识、价格区间、抽取权重和商品池成员可编辑。
- 所有写入都在 Unity 主线程执行，只保存 `Assets/Data/Economy/MerchantCatalog.asset`，不会调用全工程 `SaveAssets`。可在 Unity 中 Undo；撤销后的内存值会同步到页面，按 Unity 正常流程保存即可落盘。
- 请求先在临时副本完整校验，再一次性应用。价格须为正整数且下限不大于上限，商品权重至少为 1。类型补位权重允许为 0，但保底数量仍生效。
- 每次保存校验配置版本和“类型＋行位置＋实际资产 GUID”；不以已有重复的 `goodsId` 识别资产。发生外部修改时保留输入并提示冲突，可选择最新配置，或仅将自己编辑过的字段重新应用。连接中断不会丢弃当前页输入；离开页面时有未保存提示。
- 页面中的池内占比是该类型第一次抽取的权重比例；实际商店先做类型保底、再做类型补位，且无放回抽取，并受遗物、持有排除等规则影响。
- 遗物表条目的价格字段对应商品池原字段；抽出实际遗物的价格仍由 `MerchantOfferFactoryService` 和难度品质价格规则决定。此页不编辑遗物效果、遗物表内部成员或难度价格。

**生效范围**：新商店和后续刷新读取最新商品池。已生成的商店库存属于局内保存数据，不会被配置页强制重抽；教程和特殊副本的固定库存不由此池替代。

## 2026-11-05 补齐掉落物

按实际 GUID 核对 27 种 `LootItem_SO`，补入下列 7 种，原有 20 条以及武器、图纸、遗物表条目保持原值：

| 实际掉落物 ID | 名称 | 价格范围 | 权重 | 参考 |
|---|---|---|---|---|
| 011 | 秽蚀尖牙 | 8–12 | 10 | 普通掉落物主流配置 |
| 012 | 血肉轴承 | 18–26 | 4 | 罕见掉落物主流配置 |
| 022 | 棺钉 | 18–26 | 4 | 同上 |
| 023 | 瓶装洋流 | 18–26 | 4 | 同上 |
| 025 | 引电鬃毛 | 18–26 | 4 | 同上 |
| 026 | 交辉棱片 | 30–40 | 2 | 首个稀有掉落物：价格参考现有稀有商品蚀夜之卷；权重 2 为低于罕见 4 的暂定值 |
| 027 | 耀光水晶 | 18–26 | 4 | 罕见掉落物主流配置 |

ID 024 当前没有对应资产，不虚构条目。新商品 `goodsId` 使用 `loot_011` 等独立标识。现有史莱姆粘液、铁矿等特殊价格／权重不调整；未改掉落物基础资产、敌人、技能、遗物机制及历史内容目录快照。

## 实现和验证

- `Assets/Scripts/Editor/MerchantCatalogLiveData.cs`：实际资产映射、版本、原子校验和有针对性的保存。
- `Assets/Scripts/Editor/MerchantCatalogLiveServer.cs`：本机 HTTP、主线程队列、重载恢复和磁盘变更导入。
- `Assets/Tests/EditMode/Editor/MerchantCatalogLiveEditorTests.cs`：四类型写回、外部冲突、错误 GUID、重复商品标识、添加／移除、非法范围和批次原子性回归。
- 页面采用纯 HTML／CSS／JavaScript，没有游戏运行时依赖；编辑器代码不进入玩家构建。

在 Test Runner 运行 `MerchantCatalogLiveEditorTests`，并按变更范围运行既有 `MerchantStockServiceTests`、`MerchantOfferSelectionServiceTests` 和 `MerchantOfferFactoryServiceTests`。网页回归和真实渲染使用 `check-editor.mjs`；检查期间会短暂修改再恢复商店配置，勿与人工修改此资产同时执行。截图与日志放在忽略目录 `outputs/agent-battle/merchant-live-editor-20261105/`。

2026-11-05 验证：团结 2022.3.62t6 相关 EditMode **39/39** 通过；真实编辑器服务的网页端到端检查 **48/48** 通过，另有 **4/4** 窄屏布局检查。验证了网页写回真实 `.asset`、外部磁盘文件修改后自动导入并更新页面、同一行外部字段与本地字段的冲突合并、选择最新配置、非法数值不写入、断线后保留并补存输入、商品移出／加入、四类型切换和库存数量修改。浏览器无脚本异常或外网请求，检查后恢复商品配置原始字节。已查看桌面和 390px 窄屏渲染。

另使用正式 `MerchantStockService` 和实际商品池模拟 **512** 个种子：观察到全部 **27** 种掉落物，包含新增 7 种；掉落物商品价格均落在对应配置范围内，无生成或价格异常。模拟后恢复 Unity 随机数状态。未进行整局试玩或发行构建。

网页验证命令为 `node tools/merchant-catalog/check-editor.mjs`，需运行中的编辑器服务及已安装 Playwright；可用 `PLAYWRIGHT_PACKAGE_PATH` 指定现有包位置，`BROWSER_EXECUTABLE` 指定本机 Chromium／Edge，`MERCHANT_EDITOR_URL` 指定端口。生产编辑页本身不需要这些测试依赖。
