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

2026-10-29 导出 159 件当前配置，新增目录序号 143–159 共 17 件及历史序号 12、19、30、59、112、134、138 已核对主要机制与有效引用。其余历史 135 件沿用原机制备注，未重新逐项审计全部代码。此前设计反馈修复经 9 项新增与 27 项相关既有 EditMode 测试验证；本次五件减伤调整经 10 个编辑器运行条件满足／不满足检查验证，未完整试玩。

- 凰：敌方燃烧总层数大于 5 时可反复保命，消耗对应燃烧层数，无每场次数上限。
- 反击刺刃：改用效果类型 `ApplyStatusToSelfOnDefenseClashWinOncePerActorPerRound`，每角色每回合只排队一次下回合状态。冥龟甲继续使用不限次的原有效果类型。
- 福灵剂：每名存活友方每回合独立判定一次 25%，跳过整队统一概率门控。
- 当头棒：文案改为首次伤害 +100%，保留与其他百分比增伤相加的计算。
- 永别：按设计保留初始队首；阵亡队员不应因后续战斗结算而从队伍丢失，结算已改为按成员身份合并并保留原顺序。
- 能量保险：不同角色拥有的技能视为不同技能，分别拥有每回合返还能量次数，保留现有实现。
- 护心盾：获得时登记下一战护盾，下一战使用后清空待发放状态；已更正过期的无消耗标记备注。
- 减伤调整：屏蔽笼、寒霜护符、疾病为 10%；幽影斗篷、驰隙时序环为 20%。屏蔽笼的触发时机由通用受伤改为攻击伤害，与处理器匹配。
- 阴阳鱼：固定重铸产物，实际字符串 ID；当前无战斗效果、自定义处理器或图标，也无 RelicTable_SO 直接引用。页面以占位符展示图标。
- “被某遗物表直接引用”仅描述资产引用关系，不能据此承诺在所有商店或所有奖励中出现。
