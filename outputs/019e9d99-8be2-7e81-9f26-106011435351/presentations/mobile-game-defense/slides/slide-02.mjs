import { base, bulletList, hbar, node, rect, text, C } from "./common.mjs";

export async function slide02(presentation) {
  const slide = presentation.slides.add();
  base(slide, 2, "选题价值在于把随机成长压缩成可反复游玩的移动端短局", "01 选题背景与意义");
  node(slide, "移动端场景", "碎片时间、单局节奏短、反馈要即时", 76, 182, 260, 150, C.gold);
  node(slide, "Roguelite 构筑", "随机事件、遗物组合、敌人变化带来复玩价值", 392, 182, 300, 150, C.cyan);
  node(slide, "回合制策略", "速度骰、技能选择和目标管理提供可解释决策", 748, 182, 330, 150, C.green);
  hbar(slide, 336, 254, 392, C.gold);
  hbar(slide, 692, 254, 748, C.cyan);
  rect(slide, 80, 405, 490, 150, "#151e2d", C.line, 8);
  text(slide, "设计目标", 104, 426, 160, 30, { size: 22, color: C.gold, bold: true, fill: "#151e2d" });
  bulletList(slide, ["让玩家每一回合都有可见收益和风险判断", "让内容通过数据资产持续扩展", "让核心规则可测试、可复用、可定位问题"], 102, 465, 420, 74, { size: 19, fill: "#151e2d" });
  rect(slide, 650, 405, 500, 150, "#151e2d", C.line, 8);
  text(slide, "答辩关注点", 674, 426, 160, 30, { size: 22, color: C.cyan, bold: true, fill: "#151e2d" });
  bulletList(slide, ["玩法闭环是否完整", "系统架构是否支撑扩展", "工程质量是否能保障复杂规则稳定"], 672, 465, 410, 74, { size: 19, fill: "#151e2d" });
  return slide;
}
