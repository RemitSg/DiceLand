import { base, bulletList, codePanel, metric, rect, tag, text, C } from "./common.mjs";

export async function slide01(presentation) {
  const slide = presentation.slides.add();
  base(slide, 1, "移动端 Roguelite 回合制游戏的设计与实现", "答辩汇报");
  rect(slide, 690, 118, 440, 430, "#151e2d", C.line, 10);
  rect(slide, 748, 156, 188, 188, "#202b3f", C.gold, 8);
  rect(slide, 802, 184, 36, 124, C.gold, "none", 4);
  rect(slide, 770, 278, 106, 26, "#8b5a2b", "none", 4);
  rect(slide, 882, 174, 24, 138, "#d7a44a", "none", 4);
  text(slide, "武器", 790, 318, 108, 22, { size: 16, color: C.gold, bold: true, align: "center", fill: "#202b3f" });
  rect(slide, 970, 174, 90, 90, "#202b3f", C.red, 8);
  text(slide, "EN", 984, 198, 62, 30, { size: 25, color: C.red, bold: true, align: "center", fill: "#202b3f" });
  rect(slide, 970, 302, 90, 90, "#202b3f", C.cyan, 8);
  text(slide, "MAP", 982, 326, 66, 30, { size: 21, color: C.cyan, bold: true, align: "center", fill: "#202b3f" });
  text(slide, "探索地图 + 回合战斗 + 遗物构筑", 730, 390, 350, 40, { size: 24, bold: true, fill: "#151e2d" });
  bulletList(slide, ["Unity / Tuanjie 2022.3.62t6", "数据驱动内容扩展", "面向移动端的轻量回合体验"], 730, 438, 350, 86, { size: 18, fill: "#151e2d" });
  metric(slide, "111", "遗物数据资产", 70, 192, 180, 112, C.gold);
  metric(slide, "37", "主要敌人目录", 275, 192, 180, 112, C.cyan);
  metric(slide, "194", "EditMode 测试脚本", 480, 192, 180, 112, C.green);
  tag(slide, "Roguelite", 78, 360, 130, C.gold);
  tag(slide, "Turn-Based", 226, 360, 140, C.cyan);
  tag(slide, "ScriptableObject", 384, 360, 180, C.green);
  codePanel(slide, [
    "Assets/Scripts/MapScene",
    "Assets/Scripts/BattleScene",
    "Assets/Scripts/Shared",
    "Assets/Scripts/ScriptableObject",
    "Assets/Data/Relics · Enemies · Weapons",
  ], 70, 432, 570, 150, "Project evidence");
  return slide;
}
