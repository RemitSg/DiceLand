import { base, bulletList, codePanel, metric, node, C } from "./common.mjs";

export async function slide09(presentation) {
  const slide = presentation.slides.add();
  base(slide, 9, "敌人与关卡内容采用数据驱动，减少新增内容的代码成本", "08 敌人与关卡数据设计");
  metric(slide, "37", "主要敌人目录", 70, 178, 185, 108, C.gold);
  metric(slide, "191", "敌人相关 asset", 285, 178, 205, 108, C.cyan);
  metric(slide, "3", "敌人数据分区", 520, 178, 185, 108, C.green);
  node(slide, "Enemies", "单个敌人的数值、技能和掉落配置", 78, 350, 260, 122, C.gold);
  node(slide, "EnemyGroup", "Boss、Elite、Standard、事件遭遇组", 390, 350, 280, 122, C.cyan);
  node(slide, "Skill", "敌方技能效果、描述和行为参数", 722, 350, 250, 122, C.green);
  bulletList(slide, ["新增敌人时，优先扩展 SO 数据和技能配置", "遭遇组把关卡节奏与单体敌人定义解耦", "战斗启动器负责把 encounter 数据转为场景对象"], 78, 515, 500, 92, { size: 18 });
  codePanel(slide, [
    "Assets/Data/Enemies/Enemies/001 GreenSlime",
    "Assets/Data/Enemies/Enemies/040 InterlockingPrismServants",
    "Assets/Data/Enemies/EnemyGroup/Boss",
    "BattleSceneEncounterBootstrap.cs",
  ], 640, 515, 500, 92, "Enemy content evidence");
  return slide;
}
