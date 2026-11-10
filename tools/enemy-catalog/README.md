# 敌人与技能图鉴

双击 [敌人与技能完整图鉴](../../outputs/enemy-catalog/敌人与技能完整图鉴.html) 即可离线使用。按左栏筛选遭遇，或在顶部按敌群属性继续缩小范围。搜索框覆盖敌人名、技能、词条、被动、投放和资产来源。

输入 `006` 可查目录编号，输入 `037–040` 可查编号范围，输入 `id:3001` 可查实际 enemyID。用空格连接关键词可叠加条件。按 `/` 聚焦搜索框。点击卡片打开详情，命中的技能会展开并定位到对应词条。方向键可在当前筛选结果中切换敌人，Esc 关闭详情。卡片、列表和收藏均可从页面切换，浏览器允许本地存储时会记住收藏。底部可打印当前结果或打开完整清单。

## 重新生成

```powershell
node tools/enemy-catalog/build-catalog.mjs
node tools/enemy-catalog/check-catalog.mjs
```

生成器读取正式 `outputs/enemy-catalog/catalog.json` 和 `outputs/catalog-snapshots.json`，以同一份数据生成 HTML。Sprite 路径来自团结 AssetDatabase 按快照引用的实际 GUID 与 fileID 核对，路径登记在 `portraits.json`。48 个引用均为完整单图 Sprite，生成时裁去透明留白、缩小并内嵌 WebP。更新快照的 Sprite 引用时，也应核对这份路径表。

生成需要 Node.js 和 Sharp。检查需要 Playwright 和 Chromium 系浏览器，默认使用本机 Microsoft Edge，可用 `BROWSER_EXECUTABLE` 指定路径。两个脚本可复用 `tools/relic-catalog/` 现有依赖。查看已生成的 HTML 只需要浏览器。

## 2026-11-09 内容增补

2026-11-09 事件入口增量复核：031「王储的遗产」／副本「王储的复仇」已移出 MapEventPool 随机事件池。041 血骸王储、042 叛变的新王的事件与副本资产引用保留，目录单列入口下架说明；未据此重写其他历史属性或技能值。

新增 **044 枯枝王庭（enemyID=44）** 与 **4401 新芽（enemyID=4401）**，包括七个技能的完整配置、被动名称与颜色、两阶段、延迟强化、召唤上限、个人第四回合断枝及 Boss 池引用。新芽仅在战斗中召唤，不作为敌群初始成员。正式 JSON、摘要索引、完整清单、离线 HTML 与 `outputs/catalog-snapshots.json` 同步为 **52 个敌人资产、136 个关联技能**。

基础快照日期仍为 **2026-10-26**，页面单列最新内容增补的日期与覆盖范围；其他历史条目未在本次完整重导出，血孢子等已知历史缺项继续保留。游戏内图鉴通过 `EncyclopediaCatalogBuilder.Rebuild()` 加入两个新敌人。

MCP EditMode **24/24** 通过，覆盖实际资产、阶段阈值、周期选技、限次、召唤容量、延迟状态、逐次命中治疗、断枝与免疫。实际战斗流程运行 **8 回合定向检查**，验证第 1/4/7 回合召唤、次回合转阶段、新芽第四个可行动回合断枝与次回合返还剩余生命。此检查未验证正常通关难度和平衡。

重新生成后的 Edge 离线页面验证 **195/195** 通过，覆盖全部 52 个敌人详情与 136 个关联技能，包含筛选、检索、排序与移动布局；远程请求与页面脚本异常均为 0。

立绘由内置 ImageGen 生成透明 PNG，提示词保存在 `image-prompts/044-withered-court.txt` 与 `image-prompts/4401-new-bud.txt`，正式源图和 Unity Sprite 元数据位于 `Assets/Data/Enemies/Enemies/044 WitheredCourt/`。

### 枯枝王庭与新芽的技能姿态图

2026-11-09 补齐七张透明技能姿态图，配置到八颗骰子的 `dicePresentationSettings.skillSpriteOverride`。每次切换持续 0.5 秒，按原立绘的高度等比校正缩放，位置与旋转偏移为 0；分解两颗骰子共用其专属姿态。技能数值、阶段与触发机制保持原配置。

| 技能 | 正式图片（相对 `Assets/Data/Enemies/Enemies/044 WitheredCourt/`） | 使用骰子 |
| --- | --- | --- |
| 生长 | `044_Growth.png` | 第 1 防御骰 |
| 野长 | `044_WildGrowth.png` | 第 1 防御骰 |
| 茂盛 | `044_Flourish.png` | 第 1 攻击骰 |
| 繁茂 | `044_Lush.png` | 第 1 攻击骰 |
| 分解 | `044_Decomposition.png` | 第 1、2 攻击骰 |
| 成长 | `4401 NewBud/4401_Budding.png` | 第 1 攻击骰 |
| 断枝 | `4401 NewBud/4401_SeverBranch.png` | 第 1 攻击骰 |

全部通过内置 ImageGen 以原立绘为参照生成，完整提示词、参考图与输出路径保存在 [`image-prompts/044-skill-poses.json`](image-prompts/044-skill-poses.json)。离线 HTML 在对应技能详情中内嵌姿态图，展开即可查看，不依赖项目图片目录或网络连接。

通过 MCP 使用实际资产与 `BattlePresentationSystem` 的切图协程检查全部八颗骰子：技能图正确显示，原立绘、缩放、位置、朝向及排序在结束后恢复；以 EditMode 手动推进等待段，不代表对实时动画节奏的完整试玩。

补图后全图鉴回归 **195/195** 通过；另逐一解码七张内嵌技能图，检查分解的双骰关联及 1512px、390px、320px 下的详情布局，远程请求和页面异常均为 0。

### 分解命中特效样例：枯根撕裂（ImageGen 历史初稿）

本节记录最初的 ImageGen 原型。当前分解双骰使用下节的 Blender 版本；本节资产保留，供比较与回溯。

2026-11-09 为分解的两颗攻击骰配置专属命中特效：木质根须爪痕带毒绿色边缘光，命中后散成腐叶、绿色与淡紫色微粒。内置 ImageGen 以已有分解姿态为风格参考，生成一张 4×4、16 帧的透明图集，原始 PNG 保持完整。提示词记录在 [`image-prompts/044-decomposition-root-rend-vfx.txt`](image-prompts/044-decomposition-root-rend-vfx.txt)。

正式资产位于 `Assets/Data/Enemies/Enemies/044 WitheredCourt/VFX/`：

- `044_Decomposition_RootRend_4x4.png`：原始透明图集，Unity 以 Multiple Sprite 按左到右、上到下等分导入，保留固定中心与完整帧矩形。
- `DecompositionRootRend.anim`、`.controller`、`.prefab`：32 FPS、0.5 秒单次动画，末帧透明；不循环。完整单帧高度为 2.6 世界单位，适配现有 `hitEffectPrefab` 路径，每颗骰子在受击者位置各播放一次；命中回调与受击反馈延迟均为 0.125 秒，排序偏移 +25，保留原技能姿态图。

可双击 [分解特效离线预览](../../outputs/enemy-vfx/044-分解-枯根撕裂.html)，或从图鉴的分解详情进入。支持纯特效／施放示意、播放暂停、逐帧定位、速度与尺寸、深浅背景与透明网格。PNG 与角色图片内嵌，不依赖项目目录或网络服务。施放示意用于检查构图；实际游戏由每颗骰子的命中流程触发。

目录 JSON、摘要索引、完整清单、HTML 与快照说明同步记录本次资产、实际 GUID、帧矩形及表现延迟；伤害与治疗等技能机制保留。重建预览：

```powershell
node tools/enemy-catalog/build-catalog.mjs
node tools/enemy-catalog/build-vfx-preview.mjs
```

MCP 实际预制体与动画采样 **21/21** 通过（16 帧、尺寸、透明收尾、材质与双骰配置）；离线预览 **21/21** 通过，含播放、逐帧、背景、速度、尺寸及 1280／390／320px 布局；全图鉴回归 **195/195** 通过。预览远程请求与脚本异常均为 0。采样检查未替代完整战斗试玩。

### 当前接入：Blender 动漫风格版本

2026-11-09 根据现有王庭立绘、技能姿态与战斗场景配色，在 **Blender 5.2.3 LTS** 中原生搭建三道树皮内芯斩痕、弯钩、描边、苔绿色刃缘、象牙高光、飞叶与淡紫碎屑。共 39 个网格；斩痕用逐帧形态键沿曲线划出及从根部擦除，树皮采用程序化纹理。工作在独立的 `DiceLand_RootRend_Anime` 场景中，保留用户原场景。

可编辑源文件：[`source-art/enemies/044-root-rend/RootRendAnime.blend`](../../source-art/enemies/044-root-rend/RootRendAnime.blend)。当前采用 **24 帧、48 FPS、0.5 秒**，EEVEE 64 样本、正交相机、512×512 RGBA 渲染；经 Lanczos3 缩至每帧 384px，打包为 6×4 图集。团结运行时以现有 SpriteRenderer / Animator 特效预制体播放；`Decomposition.asset` 的两颗攻击骰均引用新预制体，命中及受击延迟仍为 0.125 秒。

正式运行资产位于 `Assets/Data/Enemies/Enemies/044 WitheredCourt/VFX/Blender/`：`044_RootRend_Blender_6x4.png`、`DecompositionRootRendBlender.anim`、`.controller`、`.prefab`，附完整 Unity 元数据。单帧完整矩形高度 2.6 世界单位、末帧透明、不循环。原 ImageGen 版本归入 JSON 的原型历史，不再作为双骰当前引用。

制作与重建流程：

1. 在 Blender 中通过 MCP 执行 [`blender/create-root-rend-anime.py`](blender/create-root-rend-anime.py) 建立源场景；已有同名场景时脚本停止，保护手工修改。
2. 在源场景中执行 [`blender/render-root-rend-anime.py`](blender/render-root-rend-anime.py)；可修改首尾帧分批渲染。中间 PNG 输出到 Git 已忽略的 `outputs/agent-battle/blender-root-rend-20261109/frames/`。
3. 执行 `node tools/enemy-catalog/blender/build-root-rend-atlas.mjs`，以源帧重建正式图集。当前团结中的切片与动画配置已保存为正式资产。
4. 更新对应目录后执行 `build-catalog.mjs`、`build-vfx-preview.mjs`；[离线动画预览](../../outputs/enemy-vfx/044-分解-枯根撕裂.html)使用真实帧数、FPS、来源及图集，支持逐帧和速度检查。

验证：团结实际预制体采样 **29/29** 通过，覆盖全部 24 帧、尺寸、材质、透明收尾与双骰引用；离线预览 **21/21**、全图鉴 **195/195** 通过。额外以项目真实战斗场景、角色、材质、后处理与当前技能姿态捕获构图，检查了受击位置的大小、颜色和遮挡；临时打开的战斗场景及 UI 状态均恢复。此项为编辑器采样与构图检查，不代表完整战斗试玩。源帧首尾完全透明，全部 24 帧均留有安全边距。

## 2026-11-10 大体型单方面攻击的遮挡修正

枯枝王庭的立绘宽度超过原先单方面攻击的固定停靠距离，导致近身后覆盖受击角色。`BattlePresentationSystem.PlaySingleAttackSequence` 现复用拼点的立绘边界间距计算：把双方朝向对手的身体宽度与最小净空相加，原有停靠距离作为下限。身体边界沿用现有规则，排除武器和速度按钮；本场景最小净空为 0.4 世界单位。小体型仍可使用原有停靠距离。

本次修改通用战斗表现逻辑，敌人、技能和特效资产的数值与引用保持原配置，目录数据及历史快照范围沿用已有记录。MCP EditMode 镜头与体型回归 25/25 通过。随后在实际 `SampleScene` Play Mode 中定向调用正式战斗流程录制分解：1280×720、24 FPS、134 帧，双方身体边界最小水平净空为 0.4，两次命中、两次 Blender 特效均捕获，新芽生命由 3 恢复至 9；逐帧缩略图核对了角色与特效的可见性。摄像机投影后的整张立绘外接矩形仍可交叠，该矩形不代表不透明轮廓遮挡。录制结束后恢复编辑器与视口状态，并移除临时录制脚本；中间画面保留在已忽略的 `outputs/agent-battle/decomposition-clearance-record-20261110/`。

## 2026-11-05 页面验证

2026-11-09 增量修订 012 耀光骑士「耀光突进」、034 回响「静默」、041 血骸王储「千丝血缚」的眩晕机制说明。每层使一个速度按钮失效，保留按钮与受击目标，禁止选技、主动连线和行动，回合结束恢复。JSON、摘要索引、清单与离线 HTML 使用同一说明；其他数值、投放和已知历史缺项保留原快照范围。

页面采用与遗物图鉴一致的分类栏、卡片和详情抽屉。数据来自 **2026-10-26** 历史快照，共 **50** 个敌人资产、**129** 个去重技能，血孢子沿用该快照的数值。

Edge 离线验证 **189/189** 通过。逐个核对了 50 份详情、形态数量和 131 处敌人与技能的关联文本，验证了编号范围、真实 ID、同名资产、技能命中、组合筛选、排序、收藏、前后切换、旧锚点与打印。查看了桌面卡片和列表、技能详情、390px 手机布局，并检查 320px 宽度下的页面与抽屉。无网络请求或页面脚本异常。检查截图与结果在 Git 已忽略的 `outputs/agent-battle/enemy-catalog-ui-20261105/`。
