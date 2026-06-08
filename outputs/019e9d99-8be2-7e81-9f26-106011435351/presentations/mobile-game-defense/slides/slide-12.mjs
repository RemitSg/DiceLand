import { base, bulletList, codePanel, metric, node, C } from "./common.mjs";

export async function slide12(presentation) {
  const slide = presentation.slides.add();
  base(slide, 12, "测试体系覆盖核心规则，保证复杂效果迭代后仍可回归", "11 工程质量与测试体系");
  metric(slide, "194", "EditMode 测试脚本", 80, 180, 230, 116, C.green);
  metric(slide, "2", "PlayMode 测试脚本", 350, 180, 220, 116, C.cyan);
  metric(slide, "Rules", "Shared 服务优先测试", 610, 180, 250, 116, C.gold);
  node(slide, "单元级规则", "骰子、遗物、召唤、状态和奖励计算", 80, 380, 270, 120, C.green);
  node(slide, "回归案例", "把玩家反馈转成可复现测试", 405, 380, 270, 120, C.cyan);
  node(slide, "编辑器测试", "不依赖完整运行场景即可验证核心服务", 730, 380, 300, 120, C.gold);
  bulletList(slide, ["测试分布说明项目不是只完成表现层，而是沉淀了可验证规则层", "新增修复会补充对应 EditMode 测试，降低同类问题反复出现"], 80, 535, 1050, 58, { size: 19 });
  codePanel(slide, [
    "MapEventOverlayTests.cs",
    "BattleSummonEffectServiceTests.cs",
    "RelicRoundStartSpeedStatusTests.cs",
  ], 80, 605, 1050, 52, "Recent regression tests");
  return slide;
}
