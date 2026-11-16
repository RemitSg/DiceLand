# DiceLand 完整图鉴

双击 [DiceLand 完整图鉴](../../outputs/encyclopedia/DiceLand完整图鉴.html) 即可离线使用。顶部可在武器、图纸、掉落物、遗物、敌人五个页签之间切换。条目、图标、搜索和详情都包含在 HTML 内，可以单独复制分享。遗物 Excel、敌人完整清单和单独的特效预览仍从仓库对应目录打开。

武器可按名称、技能、效果或编号检索，图纸可按锻造产物或材料检索，掉落物可按素材、关联敌人或卷轴检索。这三个页签均支持稀有度筛选、排序及卡片和列表切换。武器的「对应卷轴」和图纸的「锻造产物」可在卡片、列表和详情顶部直接点击。点击卷轴所需材料可查掉落，点击敌人档案里的「掉落物」可查看并打开素材。素材详情可反查敌人和使用它的卷轴。复制地址栏可保留当前页签及已打开的条目。遗物和敌人页签保留各自的筛选、收藏和技能详情。

## 数据来源

2026-11-16 的快照包含 26 个 `Weapon_SO`、20 个 `CraftingBlueprint_SO` 和 27 个 `LootItem_SO`。团结使用 `AgentBattleAssetReader` 导出技能和有效材料，以资产 GUID 关联产物和素材，并读取主图纸池、普通宝藏池和普通商店池。20 张图纸均出现在三个池中。「曦誓之卷」当前缺少有效材料，「棺材钉」当前未配置物品图标。

掉落物页签覆盖 27 种战利品素材，并标明普通商店投放。敌人素材掉落核对 53 个敌人、47 个敌群、21 张结算掉落表及事件战斗配置。22 个敌人关联至少一条素材判定；小猫咪的第三回合撤退表只有遗物项。敌群统一结算素材，每条基础概率和数量独立展示。事件金币、遗物与经验在已记录的敌人来源和遗物档案中可查。

遗物与敌人页签使用各自已生成的正式图鉴。敌人的属性与技能沿用 2026-10-26 历史快照与后续增补，覆盖细节可在页签底部查看。总图鉴按快照生成，配置更新后可重新导出和生成页面。

## 重新生成

武器、图纸、配方、素材或战斗掉落变更时，将 `EquipmentCatalogExporter.cs` 复制到团结项目的 `Assets/Scripts/Editor/`。推荐使用隔离的项目副本以便清理导出脚本和团结的导入缓存。设置环境变量 `DICELAND_EQUIPMENT_OUTPUT` 为主仓库 `outputs/encyclopedia/equipment.json` 的绝对路径，以 `-batchmode -nographics -executeMethod EquipmentCatalogExporter.Export -quit` 启动团结。刷新 `outputs/catalog-snapshots.json` 的 `equipmentCatalog` 快照记录。

敌人或遗物图鉴更新时，先按各自 README 重新生成相应图鉴。然后从仓库根目录运行以下命令。

```powershell
node tools/encyclopedia/build-catalog.mjs
node tools/encyclopedia/check-catalog.mjs
```

生成需要 Node.js 和 Sharp。检查需要 Playwright 和 Chromium 浏览器，默认使用 Microsoft Edge，可用 `BROWSER_EXECUTABLE` 指定路径。可复用 `tools/relic-catalog/` 的依赖。
