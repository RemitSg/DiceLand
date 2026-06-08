import { base, bulletList, node, rect, text, C } from "./common.mjs";

export async function slide11(presentation) {
  const slide = presentation.slides.add();
  base(slide, 11, "UI 与反馈层围绕战斗可读性服务，而不是单纯堆特效", "10 UI / 反馈 / 音频表现");
  rect(slide, 70, 170, 360, 220, "#151e2d", C.line, 8);
  rect(slide, 92, 190, 316, 176, "#24344a", C.line, 4);
  rect(slide, 92, 190, 316, 58, "#355f43", "none");
  rect(slide, 92, 248, 316, 118, "#6f654e", "none");
  rect(slide, 112, 210, 52, 52, "#111827", C.gold, 26);
  rect(slide, 333, 288, 44, 44, "#10233d", C.cyan, 22);
  rect(slide, 284, 318, 42, 42, "#6f1622", C.red, 21);
  rect(slide, 206, 226, 84, 28, "#0f1420", C.gold, 6);
  text(slide, "Chapter 2-3", 208, 229, 80, 18, { size: 12, color: C.ink, bold: true, align: "center", fill: "#0f1420" });
  text(slide, "可读性优先的战斗 HUD 示意", 94, 364, 310, 22, { size: 14, color: C.muted, fill: "#151e2d", align: "center" });
  node(slide, "战斗反馈", "伤害飘字、命中反馈、状态提示和镜头/特效", 500, 178, 280, 116, C.gold);
  node(slide, "信息面板", "技能说明、敌方意图、速度按钮可见性", 830, 178, 280, 116, C.cyan);
  node(slide, "音频与节奏", "按钮、战斗结算、奖励获得增强回合确认感", 500, 356, 280, 116, C.green);
  node(slide, "层级管理", "HUD、弹窗和教程遮罩需要明确排序策略", 830, 356, 280, 116, C.violet);
  bulletList(slide, ["答辩重点：反馈要让玩家明白为什么发生，而不是只知道发生了什么", "事件弹窗排序修复说明 UI 层级是移动端项目中真实的工程风险"], 80, 540, 1040, 60, { size: 19 });
  return slide;
}
