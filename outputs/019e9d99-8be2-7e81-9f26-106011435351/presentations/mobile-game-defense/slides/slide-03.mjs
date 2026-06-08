import { base, hbar, metric, node, rect, text, C } from "./common.mjs";

export async function slide03(presentation) {
  const slide = presentation.slides.add();
  base(slide, 3, "核心闭环由探索选择、战斗验证和成长奖励三段驱动", "02 核心玩法闭环");
  node(slide, "地图探索", "节点推进、事件选择、商店与奖励", 68, 210, 220, 138, C.gold);
  node(slide, "遭遇战斗", "速度骰生成行动窗口，技能与目标决定收益", 340, 210, 260, 138, C.cyan);
  node(slide, "结算掉落", "金币、遗物、武器与下一步路线资源", 652, 210, 250, 138, C.green);
  node(slide, "构筑变化", "遗物和状态改变后续回合规则", 954, 210, 230, 138, C.violet);
  hbar(slide, 288, 278, 340, C.gold);
  hbar(slide, 600, 278, 652, C.cyan);
  hbar(slide, 902, 278, 954, C.green);
  rect(slide, 118, 448, 1040, 72, "#151e2d", C.line, 8);
  text(slide, "每一次战斗既是路线成本，也是构筑验证；每一个奖励又会改变下一轮战斗的决策空间。", 150, 466, 980, 42, {
    size: 24,
    color: C.ink,
    bold: true,
    fill: "#151e2d",
    align: "center",
    valign: "middle",
  });
  metric(slide, "短局", "适配移动端节奏", 160, 555, 230, 80, C.gold);
  metric(slide, "随机", "提升复玩差异", 524, 555, 230, 80, C.cyan);
  metric(slide, "可解释", "回合策略透明", 888, 555, 230, 80, C.green);
  return slide;
}
