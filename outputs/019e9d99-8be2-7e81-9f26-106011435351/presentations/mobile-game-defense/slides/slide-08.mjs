import { base, bulletList, codePanel, hbar, metric, node, C } from "./common.mjs";

export async function slide08(presentation) {
  const slide = presentation.slides.add();
  base(slide, 8, "遗物与状态系统是可扩展构筑的核心规则层", "07 遗物与状态系统");
  metric(slide, "111", "遗物目录", 76, 178, 180, 104, C.gold);
  metric(slide, "115", "遗物 asset 文件", 292, 178, 210, 104, C.cyan);
  metric(slide, "Built-in", "效果注册表", 538, 178, 210, 104, C.green);
  node(slide, "触发时机", "获得、战斗开始、回合开始、命中、死亡等", 78, 350, 250, 120, C.gold);
  node(slide, "效果实现", "内置效果按领域拆分到多个 partial 文件", 386, 350, 270, 120, C.cyan);
  node(slide, "执行上下文", "通过上下文访问战斗、地图、奖励和反馈接口", 714, 350, 310, 120, C.green);
  hbar(slide, 328, 410, 386, C.gold);
  hbar(slide, 656, 410, 714, C.cyan);
  bulletList(slide, ["核心不是把每个遗物写死在场景里，而是让效果接入统一生命周期", "状态效果和遗物效果共享战斗上下文，便于组合和测试"], 78, 510, 1060, 70, { size: 19 });
  codePanel(slide, [
    "RelicSystem.BuiltInEffectRegistry.cs",
    "RelicSystem.BuiltInDiceEffects.cs",
    "RelicSystem.BuiltInStatusEffects.cs",
    "RelicSystem.BuiltInRoundStartStatueEffects.cs",
  ], 78, 590, 1060, 70, "Relic system evidence");
  return slide;
}
