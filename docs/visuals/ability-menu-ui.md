# 局外能力页面 UI

日期：2026-10-29。实现入口：`Assets/Scripts/MainMenu/AbilityMenuOverlay.cs`。

## 设计与交互

依据用户提供的森林遗迹能力页参考，重制正式主菜单中打开的能力页面。背景、可拉伸装饰边框、节点、文字和连线是独立层，页面没有把整张参考图当作交互界面使用。

- 森林遗迹、冷色空气透视与局部暖光背景；深蓝树图区、暗金详情区、鎏金藤叶边框。
- 原始配置的全部 33 个能力节点及前置关系保留；视图只调整位置的显示比例，不改配置坐标、价格、效果、ID、前置或互斥规则。
- 树图区支持鼠标或触摸拖动、滚轮平移、55%–130% 缩放和定位起点。初次打开对齐根节点，后续内容通过拖动查看。
- 节点显示锁定、可解锁、已解锁及可切换能力的启用状态；当前选中节点单独高亮。连线根据真实前置解锁状态改变亮度。
- 右侧显示名称、配置描述、效果、费用和状态。长描述与多条效果各自支持纵向滚动。切换节点重置说明滚动位置。
- 能力点不足时禁用购买并显示缺少点数；服务仍独立校验费用与前置条件。成功解锁／启用／关闭的结果保留在状态区。
- 沿用 `AbilityProgressionService` 与 `AbilityPointWallet` 的正式存档，默认入口及钱包位置不变。可选的独立进度路径供隔离预览与测试使用。
- 背景填满屏幕；主体按安全区等比适配，兼容横屏 16:9、宽屏及有刘海的设备。真机触摸手感、显存和帧率仍以正式移动包为准。

## 素材与导入

三张图片均使用内置 imagegen 生成，再原样复制到项目；不需要 API 密钥。最终资源：

- `Assets/Resources/AbilityMenu/ForestSanctuary.png`
- `Assets/Resources/AbilityMenu/GildedNodeFrame.png`
- `Assets/Resources/AbilityMenu/GildedPanelFrame.png`

背景为普通纹理；两张边框为带真实透明通道的单 Sprite，并配置九宫格边界。资源随项目打包，运行时不依赖临时文件或联网。

菜单 `Tools > Ability Visuals > Configure Art Imports` 只配置上述三张图片，不注册全项目纹理后处理器。边框、字体、颜色和显示比例可在对应脚本调整。

装饰树纹、分隔线和连线菱形使用原生 UGUI 网格绘制，随画布缩放；装饰和文字不拦截点击。

## 取图与验证

`Tools > Ability Visuals > Capture Talent Page` 使用真实 Overlay、正式能力配置和独立预览档案生成页面截图。预览场景销毁后恢复原活动场景，不保存或替换用户打开的场景，不读写玩家能力存档。截图及预览档案位于本地忽略目录 `outputs/agent-battle/ability-ui-20261029/`。

自动化验证包括：全部节点绑定、零点免费根节点、实际扣费、重开后的解锁状态、点数不足、可切换能力、远端节点可达、缩放恢复、长说明滚动、返回回调及 UI 生命周期清理。测试和取图的最终结果见同目录验证记录。

2026-10-29 验证结果：相关 EditMode **30/30**（含新增 UI 测试 7/7）通过；1920×1080、2400×1080、1600×1200 的独立场景实渲染图已逐张检查。正式主菜单 Play Mode 验证了全部 33 个节点绑定、射线命中节点并切换选择、缩放至 115%、拖动且不误触点击、射线命中返回按钮、关闭后再次打开。结束后恢复编辑模式与原来未保存的菜单场景；玩家能力钱包和能力进度文件内容前后完全一致，残留能力页 Canvas 数为 0，最终控制台错误数为 0。

高分辨率设计预览使用真实 Overlay 和隔离档案，并非玩家当前进度。正式 Game view 当时只有 556×306，其三倍截图仅用于入口和交互检查，不代表高分辨率或真机画质。未重跑整个项目测试集，未打新移动包。完整局部证据：`outputs/agent-battle/ability-ui-20261029/validation.json`。

## 生成提示词

### 森林背景（以用户参考图为编辑输入）

```text
Use case: precise-object-edit. Asset type: production 16:9 landscape background for an actual fantasy RPG talent menu. Input image is an art-direction and scene reference. Reconstruct ONLY the full forest sanctuary environment behind the interface. Remove ALL user-interface elements completely: all text and numbers, every rectangular panel, all gold frames and buttons, connecting talent lines, emblems and icons. Repaint the hidden scenery naturally: ancient mossy stone arches receding into a deep blue-green forest, quiet open glade and stone steps, a few candles on ruined walls near the edges, warm golden shafts of sunlight from upper right, dark leafy foreground corners, tiny blue flowers low in the frame. Preserve the reference's rich painterly 2.5D fantasy game style, atmospheric depth, natural color and beautiful lighting. Keep the middle of the composition quieter and darker for an interface to overlay. Wide 16:9 landscape, polished production asset, no people, NO typography, NO symbols, NO framing, NO UI, no watermark.
```

### 节点框（透明背景）

```text
Use case: stylized-concept. Asset type: ONE reusable transparent ornate rectangular frame sprite for a hand-painted fantasy RPG talent menu. A single horizontal rectangle, approximately 3:1 aspect ratio, centered with small transparent margin, straight-on flat view. An elegant thin double bevel frame of aged dark bronze with fine illuminated gold edges and subtle carved wood grooves. The four corners are intricately hand-forged angular filigree with a few tiny deep green ivy leaves and one subtle blue-green inset gemstone on the center of each short side. The long middle sections of all four borders must remain SIMPLE straight gold lines, suitable for 9-slice scaling in a game. All decoration clustered near the corners and ends, keep plenty of empty center. The entire interior is FULLY TRANSPARENT, and the entire outside is transparent, actual alpha background. Not a solid panel. Fine detailed painted texture, tactile old gold, shadowed bevels, enchanted forest ruins style, restrained leaves. No text, no icon, no letter, no number, no additional frame, no scene, no checkerboard baked into pixels, no cast shadow outside the frame, no watermark.
```

### 面板框（透明背景）

```text
Use case: stylized-concept. Asset type: one transparent 9-slice square panel border sprite for an actual fantasy RPG UI. One square frame filling most of a square canvas, tiny transparent margin. Thin straight double aged-bronze rails with delicate bright gold bevels. Ornate angular carved-gold and dark wooden filigree ONLY in the FOUR CORNERS, with a few tiny deep-green ivy leaves and a tiny muted teal stone in each corner. All FOUR SIDES between the corner ornaments must be completely SIMPLE, straight uniform thin rails, with absolutely NO mid-side gems, NO medallions or leaves in the middle of the sides, so they can stretch cleanly horizontally or vertically. Subtle tactile hand-painted material detail, old magical forest sanctuary aesthetic, elegant restrained gold, no neon. Entire center must be FULLY TRANSPARENT, entire outside also actual transparency. Empty frame only, no solid interior, no scene, no lettering, no symbols, no additional frames, no checkerboard, no watermark. Straight-on flat view, centered, square.
```
