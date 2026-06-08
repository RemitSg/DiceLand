import { base, bulletList, metric, rect, text, C } from "./common.mjs";

export async function slide15(presentation) {
  const slide = presentation.slides.add();
  base(slide, 15, "项目完成了从玩法闭环到工程验证的完整落地", "14 总结");
  metric(slide, "闭环", "地图探索 → 战斗 → 奖励构筑", 88, 178, 270, 120, C.gold);
  metric(slide, "架构", "场景表现 / 共享规则 / 数据资产", 408, 178, 300, 120, C.cyan);
  metric(slide, "质量", "194 个 EditMode 测试支撑回归", 758, 178, 310, 120, C.green);
  rect(slide, 104, 380, 980, 134, "#151e2d", C.line, 8);
  bulletList(slide, ["完成移动端 Roguelite 回合制的核心系统实现", "通过 ScriptableObject 和 Assets/Data 建立可扩展内容管线", "通过问题攻关与测试补充，证明项目具备继续迭代的工程基础"], 138, 410, 910, 72, { size: 22, fill: "#151e2d" });
  text(slide, "谢谢老师，请批评指正", 348, 565, 560, 58, {
    size: 34,
    color: C.gold,
    bold: true,
    align: "center",
  });
  return slide;
}
