import { base, bulletList, codePanel, node, rect, text, C } from "./common.mjs";

export async function slide05(presentation) {
  const slide = presentation.slides.add();
  base(slide, 5, "地图探索系统负责把随机内容组织成可选择的路线压力", "04 地图探索系统");
  node(slide, "节点生成", "战斗、事件、商店、奖励等节点类型", 70, 178, 240, 126, C.gold);
  node(slide, "玩家选择", "路线推进与资源消耗形成分支", 370, 178, 240, 126, C.cyan);
  node(slide, "事件反馈", "弹窗、奖励、状态变动和场景切换", 670, 178, 240, 126, C.green);
  node(slide, "进入战斗", "将遭遇配置传递给 BattleScene", 970, 178, 220, 126, C.violet);
  rect(slide, 70, 360, 500, 180, "#151e2d", C.line, 8);
  text(slide, "实现重点", 96, 384, 150, 30, { size: 22, color: C.gold, bold: true, fill: "#151e2d" });
  bulletList(slide, ["事件面板需要高于 HUD 排序，避免遮挡关键选择", "地图教程独立服务，降低新手引导对地图流程的侵入", "地图事件与经济奖励由数据和共享服务承接"], 94, 425, 420, 86, { size: 18, fill: "#151e2d" });
  codePanel(slide, [
    "MapEventOverlay.cs",
    "MapTutorialFlowService.cs",
    "MapTutorialGuideOverlay.cs",
    "MapTutorialMerchantStockService.cs",
  ], 640, 360, 500, 180, "MapScene evidence");
  return slide;
}
