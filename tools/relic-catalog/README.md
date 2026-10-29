# 遗物目录维护

正式入口是 `outputs/relic-catalog/遗物完整图鉴.html`，可直接双击离线打开。页面内嵌当前 JSON 数据与已有遗物图标，不需要服务器或网络；Markdown、Excel 下载入口使用同目录文件。不要移动单个文件后期待这些附属链接仍有效。

## 数据更新

1. 先读仓库根目录 `AGENTS.md`，检查当前遗物目录已有格式。
2. 在打开本项目的团结编辑器中，通过 Unity MCP 的 `execute_code` 执行 `export-relics.cs` 文件正文。它只读取 `Relic_SO` 与直接引用它们的 `RelicTable_SO`，并导出到 `outputs/relic-catalog/relics-raw.json`。此原始导出属于本地中间文件，不提交；其中对象 instanceID 不能作为长期标识。
3. 对新增遗物及受影响条目核对配置和实际运行处理器。补充 `curation.json` 的分类、定位、标签、机制备注、实际条件／目标／次数／叠加、关联序号和已确认的文案差异。旧条目已有字段保存在正式 `catalog.json` 中，不要丢弃旧分类与备注。旧条目发生修改时也可在 `curation.json` 中覆盖对应序号的受影响字段。
4. 在 `build-catalog.mjs` 中更新本次日期、新增范围与覆盖说明。正常数字 relicId 用作目录序号；特殊字符串 ID 必须建立明确映射，且保留真实 ID。本次阴阳鱼的目录序号 159 对应实际 ID `yin_yang_fish`。新增分类需要同时加入正式 JSON 的 groups，不能仅填一个未注册的分类名称。
5. 在仓库根目录依次运行：

   ```powershell
   node tools/relic-catalog/build-catalog.mjs
   node tools/relic-catalog/update-workbook.mjs
   node tools/relic-catalog/check-catalog.mjs
   python -X utf8 -B tools/relic-catalog/verify-data.py
   ```

   第一项统一生成正式 JSON、Markdown 和自包含 HTML。第二项通过 `@oai/artifact-tool` 导入并更新现有 Excel，保留原有两张工作表、列结构、宽度和表样式，扩展数据表范围。第三项通过 Playwright 验证离线页面的筛选、排序、收藏、详情、导出及窄屏布局。使用 Spreadsheets 技能的 agent 还应按该技能要求，在首次编辑工作簿前记录 artifact 操作并查看编辑前预览；`inspect-workbook.mjs` 可生成原表预览。
6. 同步 `outputs/catalog-snapshots.json` 的遗物日期、数量与真实覆盖范围，以及相关项目进度。不要把敌人历史快照的日期一起刷新。
7. 检查各正式输出一致、工作簿表范围和数值正确；查看桌面／手机 HTML 截图和 Excel 更新区域截图。将正式目录与生成源码放在同一提交。

## 运行依赖与预览

使用当前环境提供的 Node.js、`sharp`、`playwright`、`@oai/artifact-tool`，不在游戏工程引入运行时依赖。可以在本目录临时建立指向已安装依赖的 `node_modules` junction，并通过 Git 本地 exclude 忽略它；不要提交该目录或机器专属路径。浏览器检查默认使用 Windows Edge，也可设置 `BROWSER_EXECUTABLE` 指向其他已安装的 Chromium 浏览器。

`node tools/relic-catalog/preview.mjs` 仅在 `127.0.0.1` 启动只读预览，终端打印实际地址；结束预览时终止该进程。页面本身仍可离线使用。

原始导出、`*.inspect.ndjson`、`*-preview.png` 和 `outputs/merge-dev-relic-catalog-*` 检查文件应按项目约定仅保留本地。正式文件为 HTML、Markdown、Excel、catalog.json 及目录快照元数据。

## 当前核对范围

2026-10-29 导出 159 件当前配置，新增目录序号 143–159 共 17 件已核对主要机制与有效引用。历史 142 件沿用原分类与机制备注，未重新逐项审计其全部代码；本次没有实战测试。

- 反击刺刃：文案“每个角色每回合限 1 次”在当前处理器和排队状态流程中未实现，已作为差异显式标记。
- 福灵剂：每回合统一判定一次 25%，成功后给全部存活友方施加幸运。
- 阴阳鱼：固定重铸产物，实际字符串 ID；当前无战斗效果、自定义处理器或图标，也无 RelicTable_SO 直接引用。页面以占位符展示图标。
- “被某遗物表直接引用”仅描述资产引用关系，不能据此承诺在所有商店或所有奖励中出现。
