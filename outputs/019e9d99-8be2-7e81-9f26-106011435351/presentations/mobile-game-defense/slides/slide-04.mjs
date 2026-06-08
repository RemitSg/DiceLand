import { base, codePanel, hbar, node, rect, text, C } from "./common.mjs";

export async function slide04(presentation) {
  const slide = presentation.slides.add();
  base(slide, 4, "架构把场景表现、共享规则和数据资产拆开，降低迭代成本", "03 系统总体架构");
  node(slide, "MapScene", "地图节点、事件弹窗、路线推进、教程引导", 68, 182, 240, 118, C.gold);
  node(slide, "BattleScene", "战斗控制器、表现桥接、按钮与目标选择", 68, 360, 240, 118, C.cyan);
  node(slide, "Shared", "骰子、状态、遗物、结算、伤害和召唤规则", 470, 270, 280, 136, C.green);
  node(slide, "ScriptableObject", "敌人、武器、遗物、教程和关卡数据定义", 906, 182, 250, 118, C.violet);
  node(slide, "Assets/Data", "内容资产目录，支撑横向扩展", 906, 360, 250, 118, C.gold);
  hbar(slide, 308, 240, 470, C.gold);
  hbar(slide, 308, 418, 470, C.cyan);
  hbar(slide, 750, 318, 906, C.green);
  codePanel(slide, [
    "Assets/Scripts/MapScene        // exploration flow",
    "Assets/Scripts/BattleScene     // presentation/controller",
    "Assets/Scripts/Shared          // reusable combat rules",
    "Assets/Scripts/ScriptableObject// data contracts",
    "Assets/Data                    // authored content",
  ], 105, 540, 1010, 96, "Layering in repository");
  text(slide, "核心原则：表现层只承接场景交互，规则层沉淀可测试服务，内容层通过 SO 资产配置。", 98, 494, 1040, 32, {
    size: 20,
    color: C.muted,
  });
  return slide;
}
