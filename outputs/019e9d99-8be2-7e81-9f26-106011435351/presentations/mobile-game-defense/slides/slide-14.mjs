import { base, bulletList, hbar, node, rect, text, C } from "./common.mjs";

export async function slide14(presentation) {
  const slide = presentation.slides.add();
  base(slide, 14, "后续优化会沿着内容规模、移动端体验和工具化三条线推进", "13 后续优化与扩展规划");
  node(slide, "内容扩展", "补充敌人组、事件池、遗物联动和 Boss 机制", 80, 182, 260, 128, C.gold);
  node(slide, "体验打磨", "移动端触控、信息密度、性能与加载节奏", 430, 182, 270, 128, C.cyan);
  node(slide, "工具化", "关卡/遗物校验器、自动截图、平衡性报表", 790, 182, 280, 128, C.green);
  hbar(slide, 340, 246, 430, C.gold);
  hbar(slide, 700, 246, 790, C.cyan);
  rect(slide, 90, 398, 990, 122, "#151e2d", C.line, 8);
  text(slide, "优先级判断", 118, 420, 160, 30, { size: 22, color: C.gold, bold: true, fill: "#151e2d" });
  bulletList(slide, ["先保障核心战斗规则稳定，再扩大内容池", "先提升移动端可读性，再增加复杂机制", "把重复人工检查沉淀为编辑器工具或测试"], 116, 462, 860, 46, { size: 19, fill: "#151e2d" });
  text(slide, "规划目标：让项目从“可玩原型”继续走向“可长期迭代的移动端游戏工程”。", 120, 575, 1000, 36, {
    size: 25,
    color: C.ink,
    bold: true,
  });
  return slide;
}
