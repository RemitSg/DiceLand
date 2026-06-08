import { base, bulletList, codePanel, hbar, metric, node, C } from "./common.mjs";

export async function slide07(presentation) {
  const slide = presentation.slides.add();
  base(slide, 7, "速度骰和技能系统把战斗选择转化为可计算的行动窗口", "06 速度骰 / 技能系统");
  node(slide, "基础速度", "角色、敌人和状态提供速度来源", 78, 180, 230, 118, C.gold);
  node(slide, "骰子修正", "遗物与状态改变骰子范围或点数", 388, 180, 240, 118, C.cyan);
  node(slide, "按钮可用性", "能量、目标、速度按钮状态决定可行动作", 708, 180, 280, 118, C.green);
  hbar(slide, 308, 238, 388, C.gold);
  hbar(slide, 628, 238, 708, C.cyan);
  metric(slide, "21", "武器目录", 88, 390, 180, 100, C.gold);
  metric(slide, "89", "武器 asset 文件", 300, 390, 210, 100, C.cyan);
  metric(slide, "Dice", "规则服务独立测试", 542, 390, 240, 100, C.green);
  bulletList(slide, ["速度不是单一数值，而是影响回合内行动槽位", "技能通过目标和效果标签进入统一解析流程", "遗物可以在骰子生成前后插入规则修改"], 820, 376, 330, 130, { size: 18 });
  codePanel(slide, [
    "BattleDiceRollService.cs",
    "BattleDiceRangeModifierService.cs",
    "BattleDicePowerPreparationService.cs",
    "BattleAllySkillEnergyAvailabilityService.cs",
  ], 100, 540, 1040, 96, "Dice / skill services");
  return slide;
}
