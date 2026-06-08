import { base, bulletList, hbar, node, rect, text, C } from "./common.mjs";

export async function slide13(presentation) {
  const slide = presentation.slides.add();
  base(slide, 13, "技术攻关的关键是把玩家现象还原到战斗时序和状态生命周期", "12 技术难点与问题攻关");
  node(slide, "玩家现象", "遗物 009 偶发看起来没有触发", 70, 182, 240, 116, C.red);
  node(slide, "根因定位", "回合开始状态消费早于遗物触发", 370, 182, 260, 116, C.gold);
  node(slide, "最小修复", "调整急速状态生命周期，使其参与本回合速度生成", 690, 182, 300, 116, C.cyan);
  hbar(slide, 310, 240, 370, C.red);
  hbar(slide, 630, 240, 690, C.gold);
  rect(slide, 84, 358, 1010, 94, "#151e2d", C.line, 8);
  text(slide, "原流程：消费回合开始状态 → 触发回合开始遗物 → 生成速度", 112, 382, 960, 28, {
    size: 22,
    color: C.muted,
    fill: "#151e2d",
  });
  text(slide, "表现问题：刚加上的 Swiftness 来不及影响速度，回合后又被清掉", 112, 414, 960, 28, {
    size: 22,
    color: C.gold,
    bold: true,
    fill: "#151e2d",
  });
  bulletList(slide, ["同时修复事件面板排序：Canvas sortingOrder 7990 → 9040", "修复敌人召唤重合：召唤位置服务避开已有敌人占位", "每个修复都补充 EditMode 回归测试"], 100, 505, 980, 80, { size: 20 });
  return slide;
}
