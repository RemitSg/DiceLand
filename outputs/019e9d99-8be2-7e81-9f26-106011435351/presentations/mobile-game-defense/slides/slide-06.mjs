import { base, bulletList, codePanel, hbar, node, rect, text, C } from "./common.mjs";

export async function slide06(presentation) {
  const slide = presentation.slides.add();
  base(slide, 6, "回合战斗系统用阶段调度把规则计算和表现播放解耦", "05 回合战斗系统");
  node(slide, "回合开始", "状态结算、遗物触发、速度生成", 74, 198, 220, 122, C.gold);
  node(slide, "行动收集", "按钮、目标、技能与敌方意图", 346, 198, 220, 122, C.cyan);
  node(slide, "结果解析", "命中、伤害、状态、召唤和死亡", 618, 198, 220, 122, C.green);
  node(slide, "表现计划", "动画、飘字、UI 刷新与收尾", 890, 198, 220, 122, C.violet);
  hbar(slide, 294, 258, 346, C.gold);
  hbar(slide, 566, 258, 618, C.cyan);
  hbar(slide, 838, 258, 890, C.green);
  rect(slide, 78, 390, 470, 150, "#151e2d", C.line, 8);
  text(slide, "战斗可维护性的来源", 102, 414, 260, 30, { size: 22, color: C.cyan, bold: true, fill: "#151e2d" });
  bulletList(slide, ["计算服务独立于 Unity 表现对象", "阶段计划先生成，再由控制器播放", "测试可以直接覆盖 Shared 规则"], 100, 454, 390, 64, { size: 18, fill: "#151e2d" });
  codePanel(slide, [
    "BattleCombatActionCollectorService.cs",
    "BattleCombatActionExecutionDispatcher.cs",
    "BattleCombatPhaseCompletionPlanService.cs",
    "BattleClashResultService.cs",
  ], 620, 390, 520, 150, "Shared combat services");
  return slide;
}
