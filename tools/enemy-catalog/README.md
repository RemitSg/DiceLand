# 敌人与技能图鉴

双击 [敌人与技能完整图鉴](../../outputs/enemy-catalog/敌人与技能完整图鉴.html) 即可离线使用。按左栏筛选遭遇，或在顶部按敌群属性继续缩小范围。搜索框覆盖敌人名、技能、词条、被动、投放和资产来源。

输入 `006` 可查目录编号，输入 `037–040` 可查编号范围，输入 `id:3001` 可查实际 enemyID。用空格连接关键词可叠加条件。按 `/` 聚焦搜索框。点击卡片打开详情，命中的技能会展开并定位到对应词条。方向键可在当前筛选结果中切换敌人，Esc 关闭详情。卡片、列表和收藏均可从页面切换，浏览器允许本地存储时会记住收藏。底部可打印当前结果或打开完整清单。

## 重新生成

```powershell
node tools/enemy-catalog/build-catalog.mjs
node tools/enemy-catalog/check-catalog.mjs
```

生成器读取正式 `outputs/enemy-catalog/catalog.json` 和 `outputs/catalog-snapshots.json`，以同一份数据生成 HTML。Sprite 路径来自团结 AssetDatabase 按快照引用的实际 GUID 与 fileID 核对，路径登记在 `portraits.json`。46 个引用均为完整单图 Sprite，生成时裁去透明留白、缩小并内嵌 WebP。更新快照的 Sprite 引用时，也应核对这份路径表。

生成需要 Node.js 和 Sharp。检查需要 Playwright 和 Chromium 系浏览器，默认使用本机 Microsoft Edge，可用 `BROWSER_EXECUTABLE` 指定路径。两个脚本可复用 `tools/relic-catalog/` 现有依赖。查看已生成的 HTML 只需要浏览器。

## 2026-11-05 页面验证

页面采用与遗物图鉴一致的分类栏、卡片和详情抽屉。数据来自 **2026-10-26** 历史快照，共 **50** 个敌人资产、**129** 个去重技能，血孢子沿用该快照的数值。

Edge 离线验证 **189/189** 通过。逐个核对了 50 份详情、形态数量和 131 处敌人与技能的关联文本，验证了编号范围、真实 ID、同名资产、技能命中、组合筛选、排序、收藏、前后切换、旧锚点与打印。查看了桌面卡片和列表、技能详情、390px 手机布局，并检查 320px 宽度下的页面与抽屉。无网络请求或页面脚本异常。检查截图与结果在 Git 已忽略的 `outputs/agent-battle/enemy-catalog-ui-20261105/`。
