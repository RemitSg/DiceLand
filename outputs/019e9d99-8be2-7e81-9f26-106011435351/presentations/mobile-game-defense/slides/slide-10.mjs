import { base, bulletList, codePanel, hbar, node, rect, text, C } from "./common.mjs";

export async function slide10(presentation) {
  const slide = presentation.slides.add();
  base(slide, 10, "教程系统把新手引导拆成地图、主菜单和战斗三个上下文", "09 教程与新手引导");
  node(slide, "主菜单引导", "入口、按钮说明和首次流程提示", 82, 190, 250, 120, C.gold);
  node(slide, "地图引导", "节点选择、事件面板和商店流程", 442, 190, 250, 120, C.cyan);
  node(slide, "战斗引导", "速度按钮、技能选择、目标与结算反馈", 802, 190, 280, 120, C.green);
  hbar(slide, 332, 250, 442, C.gold);
  hbar(slide, 692, 250, 802, C.cyan);
  rect(slide, 88, 386, 460, 150, "#151e2d", C.line, 8);
  text(slide, "设计取舍", 112, 410, 130, 30, { size: 22, color: C.gold, bold: true, fill: "#151e2d" });
  bulletList(slide, ["教程逻辑不直接塞进核心战斗服务", "高亮坐标工具处理不同 UI 层级的定位", "战斗教程可通过 Definition SO 配置阶段"], 110, 450, 380, 70, { size: 18, fill: "#151e2d" });
  codePanel(slide, [
    "MainMenuTutorialOverlay.cs",
    "MapTutorialFlowService.cs",
    "TutorialBattleDefinition_SO.cs",
    "TutorialHighlightCoordinateUtility.cs",
  ], 620, 386, 500, 150, "Tutorial evidence");
  return slide;
}
