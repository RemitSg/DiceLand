import path from "node:path";

const C = {
  bg: "#151925",
  bg2: "#202636",
  panel: "#252b3b",
  panel2: "#30374b",
  text: "#F6F7FB",
  sub: "#B7C0D8",
  dim: "#7D879F",
  cyan: "#4CC9F0",
  gold: "#F3C34D",
  green: "#74D99F",
  red: "#F26D6D",
  purple: "#9B7EDE",
  line: "#49546D",
};

const ROOT = "L:/gameProject/MobileGame";
const IMG = {
  menu: path.join(ROOT, "Assets/Screenshots/codex_menu_probe.png"),
  map: path.join(ROOT, "Assets/Screenshots/codex_screen_capture_after_fix.png"),
  mapClean: path.join(ROOT, "Assets/Screenshots/codex_map_tutorial_after_fix.png"),
};

function rect(ctx, slide, x, y, w, h, fill = C.panel, line = C.line) {
  return ctx.addShape(slide, { x, y, w, h, fill, line: ctx.line(line, 1) });
}

function rule(ctx, slide, x, y, w, color = C.cyan) {
  return ctx.addShape(slide, { x, y, w, h: 3, fill: color, line: ctx.line(color, 0) });
}

function text(ctx, slide, value, x, y, w, h, opt = {}) {
  return ctx.addText(slide, {
    text: value,
    x, y, w, h,
    fontSize: opt.size ?? 24,
    color: opt.color ?? C.text,
    bold: opt.bold ?? false,
    typeface: opt.face ?? "Microsoft YaHei",
    align: opt.align ?? "left",
    valign: opt.valign ?? "top",
    fill: opt.fill ?? "#00000000",
    line: ctx.line("#00000000", 0),
    insets: opt.insets ?? { left: 0, right: 0, top: 0, bottom: 0 },
  });
}

function title(ctx, slide, kicker, headline, sub) {
  text(ctx, slide, kicker, 58, 35, 440, 28, { size: 16, color: C.gold, bold: true });
  text(ctx, slide, headline, 58, 64, 760, 74, { size: 34, bold: true });
  if (sub) text(ctx, slide, sub, 60, 136, 760, 38, { size: 17, color: C.sub });
  rule(ctx, slide, 58, 179, 96, C.cyan);
}

function footer(ctx, slide, n) {
  text(ctx, slide, `答辩PPT · MobileGame · ${String(n).padStart(2, "0")}`, 58, 674, 460, 22, {
    size: 11,
    color: C.dim,
  });
}

function metric(ctx, slide, value, label, x, y, w = 148, accent = C.cyan) {
  rect(ctx, slide, x, y, w, 86, "#1B2030", "#384157");
  text(ctx, slide, value, x + 16, y + 12, w - 32, 34, { size: 28, bold: true, color: accent });
  text(ctx, slide, label, x + 16, y + 50, w - 32, 20, { size: 13, color: C.sub });
}

function bulletBlock(ctx, slide, items, x, y, w, h, opt = {}) {
  const body = items.map((item) => `• ${item}`).join("\n");
  text(ctx, slide, body, x, y, w, h, {
    size: opt.size ?? 18,
    color: opt.color ?? C.sub,
    insets: { left: 0, right: 0, top: 0, bottom: 0 },
  });
}

function pill(ctx, slide, label, x, y, w, color = C.cyan) {
  rect(ctx, slide, x, y, w, 34, "#1B2030", color);
  text(ctx, slide, label, x + 10, y + 7, w - 20, 18, { size: 13, color, bold: true, align: "center" });
}

function node(ctx, slide, label, body, x, y, w, h, accent = C.cyan) {
  rect(ctx, slide, x, y, w, h, C.panel, "#3C465D");
  rule(ctx, slide, x, y, w, accent);
  text(ctx, slide, label, x + 16, y + 16, w - 32, 24, { size: 17, bold: true });
  text(ctx, slide, body, x + 16, y + 47, w - 32, h - 60, { size: 13.5, color: C.sub });
}

function hline(ctx, slide, x, y, w, color = C.line) {
  ctx.addShape(slide, { x, y, w, h: 2, fill: color, line: ctx.line(color, 0) });
}

function vline(ctx, slide, x, y, h, color = C.line) {
  ctx.addShape(slide, { x, y, w: 2, h, fill: color, line: ctx.line(color, 0) });
}

function background(ctx, slide) {
  slide.background.fill = C.bg;
  rect(ctx, slide, 0, 0, 1280, 720, C.bg, C.bg);
  ctx.addShape(slide, { x: 1010, y: -90, w: 360, h: 360, fill: "#263149", line: ctx.line("#263149", 0) });
  ctx.addShape(slide, { x: -140, y: 560, w: 360, h: 260, fill: "#1C2233", line: ctx.line("#1C2233", 0) });
}

async function slide01(presentation, ctx) {
  const slide = presentation.slides.add();
  background(ctx, slide);
  await ctx.addImage(slide, { path: IMG.map, x: 680, y: 60, w: 540, h: 360, fit: "cover" });
  rect(ctx, slide, 680, 60, 540, 360, "#00000000", C.cyan);
  text(ctx, slide, "移动端 Roguelite\n回合制游戏项目", 60, 82, 560, 150, { size: 48, bold: true });
  text(ctx, slide, "基于 Unity/Tuanjie 的地图探索、速度骰战斗、遗物构筑与数据驱动内容系统", 62, 252, 520, 58, { size: 21, color: C.sub });
  metric(ctx, slide, "110", "遗物资产目录", 62, 390, 150, C.gold);
  metric(ctx, slide, "37", "敌人资产目录", 232, 390, 150, C.cyan);
  metric(ctx, slide, "196", "测试脚本", 402, 390, 150, C.green);
  text(ctx, slide, "答辩人：项目开发者    日期：2026-06", 62, 625, 560, 26, { size: 15, color: C.dim });
  footer(ctx, slide, 1);
  return slide;
}

async function slide02(presentation, ctx) {
  const slide = presentation.slides.add();
  background(ctx, slide);
  title(ctx, slide, "01 / 选题背景", "项目目标不是单场战斗，而是一条可反复迭代的移动端主循环", "用地图决策承接战斗压力，用构筑系统放大每次选择的差异。");
  node(ctx, slide, "移动端场景", "需要短局、低输入成本、状态反馈清晰；地图、战斗、结算都要能被快速理解。", 70, 250, 330, 170, C.cyan);
  node(ctx, slide, "Roguelite 结构", "通过行动点、灾厄、钥匙、Boss 解锁，把路线选择和风险收益绑定在一起。", 475, 210, 330, 210, C.gold);
  node(ctx, slide, "回合制战斗", "用速度按钮、拼点和技能词条制造可解释的战术判断，而不是单纯数值堆叠。", 880, 250, 330, 170, C.green);
  hline(ctx, slide, 400, 335, 75, C.line);
  hline(ctx, slide, 805, 335, 75, C.line);
  text(ctx, slide, "答辩主张：本项目的价值在于把“地图压力、战斗策略、内容构筑”做成同一条可扩展管线。", 158, 510, 965, 48, { size: 24, bold: true, align: "center" });
  footer(ctx, slide, 2);
  return slide;
}

async function slide03(presentation, ctx) {
  const slide = presentation.slides.add();
  background(ctx, slide);
  title(ctx, slide, "02 / 核心玩法闭环", "主循环围绕“探索—遭遇—成长—更高压力”滚动", "每一次移动都会改变战斗资源、路线安全性和最终 Boss 准备程度。");
  const steps = [
    ["生成地图", "按难度配置网格、Boss 中心、钥匙与事件分布", 520, 190, C.cyan],
    ["路线选择", "行动点消耗与已探索格免消耗遗物形成路线策略", 810, 270, C.gold],
    ["进入战斗", "敌人组、奖励、队伍状态通过 Runtime 桥接", 700, 500, C.green],
    ["结算成长", "经验、血量、掉落物、遗物选择回写 RunState", 330, 500, C.purple],
    ["灾厄推进", "行动点耗尽后提高压力，推动玩家更快决策", 210, 270, C.red],
  ];
  for (const [label, body, x, y, color] of steps) node(ctx, slide, label, body, x, y, 250, 112, color);
  hline(ctx, slide, 460, 246, 60, C.line); hline(ctx, slide, 770, 326, 40, C.line);
  hline(ctx, slide, 605, 556, 95, C.line); hline(ctx, slide, 460, 556, -1, C.line);
  text(ctx, slide, "RunState 是循环的记忆：队伍、背包、行动点、钥匙、地图进度都围绕它回写。", 112, 620, 1056, 34, { size: 20, color: C.sub, align: "center" });
  footer(ctx, slide, 3);
  return slide;
}

async function slide04(presentation, ctx) {
  const slide = presentation.slides.add();
  background(ctx, slide);
  title(ctx, slide, "03 / 系统架构", "项目按场景职责拆分，核心数据通过 Runtime 与 SO 资产流动", "架构重点是把可变运行状态和可配置内容资产分开管理。");
  node(ctx, slide, "MapScene", "RunGameManager\nBoardManager\nMapHUD\nMapEventOverlay", 70, 230, 240, 240, C.gold);
  node(ctx, slide, "Shared Services", "RelicSystem\nCalamitySystem\nMapMovementService\nBattleTargeting*Service", 375, 190, 270, 320, C.cyan);
  node(ctx, slide, "BattleScene", "BattleSceneEncounterBootstrap\nBattleButton\nBattleFeedbackSystem\nBattleAlly/EnemyController", 710, 230, 270, 240, C.green);
  node(ctx, slide, "ScriptableObject + Data", "Relic_SO / EnemyData_SO\nLootTable_SO / Weapon_SO\nDifficultyConfig_SO", 1015, 190, 210, 320, C.purple);
  hline(ctx, slide, 310, 350, 65, C.line); hline(ctx, slide, 645, 350, 65, C.line); hline(ctx, slide, 980, 350, 35, C.line);
  text(ctx, slide, "关键边界", 78, 538, 160, 22, { size: 15, color: C.gold, bold: true });
  bulletBlock(ctx, slide, [
    "MapRunRuntime 保留地图返回状态",
    "BattleEncounterRuntime 承载战斗上下文",
    "SO 资产让敌人、遗物、掉落与难度可编辑"
  ], 78, 570, 870, 70, { size: 17 });
  footer(ctx, slide, 4);
  return slide;
}

async function slide05(presentation, ctx) {
  const slide = presentation.slides.add();
  background(ctx, slide);
  title(ctx, slide, "04 / 地图探索", "地图不是菜单，而是战斗前的风险定价器", "行动点、钥匙、商店、休息、重铸和灾厄共同决定玩家路线。");
  await ctx.addImage(slide, { path: IMG.map, x: 65, y: 210, w: 690, h: 388, fit: "cover" });
  rect(ctx, slide, 65, 210, 690, 388, "#00000000", "#3D4863");
  node(ctx, slide, "格子类型", "Battle / Event / Rest / Key / Boss\n后续可扩展商店、宝藏、诅咒、剧情格。", 800, 218, 370, 110, C.cyan);
  node(ctx, slide, "难度配置", "7x7 到 13x13 网格；行动点、钥匙需求、商店刷新、Boss 配置由 SO 驱动。", 800, 358, 370, 126, C.gold);
  node(ctx, slide, "路线压力", "已探索格、行动点耗尽、灾厄层级共同形成“绕路是否值得”的判断。", 800, 514, 370, 110, C.red);
  footer(ctx, slide, 5);
  return slide;
}

async function slide06(presentation, ctx) {
  const slide = presentation.slides.add();
  background(ctx, slide);
  title(ctx, slide, "05 / 战斗流程", "速度按钮把回合制战斗拆成可读、可连线、可反制的阶段", "战斗不是自动结算：玩家通过技能选择和目标连接改变拼点结果。");
  const phases = [
    ["回合开始", "状态结算\n遗物触发", C.gold],
    ["速度生成", "友方/敌方\n速度按钮", C.cyan],
    ["目标连接", "拖拽连线\n敌方自动锁定", C.green],
    ["拼点解析", "骰子威力\n攻防胜负", C.purple],
    ["结算返回", "掉落/EXP\n写回地图", C.red],
  ];
  phases.forEach(([a,b,c], i) => {
    const x = 80 + i * 232;
    metric(ctx, slide, `0${i + 1}`, a, x, 245, 86, c);
    text(ctx, slide, b, x + 98, 256, 126, 58, { size: 16, color: C.sub });
    if (i < phases.length - 1) hline(ctx, slide, x + 205, 288, 45, C.line);
  });
  rect(ctx, slide, 92, 430, 1094, 120, "#1B2030", "#364159");
  text(ctx, slide, "BattleButton 目前承担主流程，多个 BattleTargeting / BattleStatus / BattleRelic 服务正在逐步拆分纯逻辑。", 125, 457, 1020, 32, { size: 24, bold: true, align: "center" });
  text(ctx, slide, "这让战斗规则可以被 EditMode 测试覆盖，也让表现系统与结算逻辑逐步解耦。", 125, 500, 1020, 26, { size: 18, color: C.sub, align: "center" });
  footer(ctx, slide, 6);
  return slide;
}

async function slide07(presentation, ctx) {
  const slide = presentation.slides.add();
  background(ctx, slide);
  title(ctx, slide, "06 / 速度与技能", "速度在本项目中同时承担顺序、收益和目标选择三重角色", "这使战斗策略从“点技能”提升为“读速度、连目标、控状态”。");
  node(ctx, slide, "行动顺序", "速度越高越容易获得先手窗口；速度按钮作为玩家可见的战术单位。", 90, 240, 310, 150, C.cyan);
  node(ctx, slide, "拼点资源", "部分遗物/技能根据速度差修改威力、减伤或额外状态。", 485, 190, 310, 150, C.gold);
  node(ctx, slide, "目标信号", "敌方可优先攻击最低速、被标记或特定状态目标。", 880, 240, 310, 150, C.green);
  hline(ctx, slide, 400, 315, 85, C.line); hline(ctx, slide, 795, 265, 85, C.line);
  rect(ctx, slide, 225, 500, 830, 82, "#1B2030", "#334059");
  text(ctx, slide, "设计收益：速度体系天然连接敌人 AI、遗物构筑、状态系统与 UI 交互，是本项目最能形成差异化的战斗核心。", 255, 526, 770, 28, { size: 20, align: "center", color: C.text, bold: true });
  footer(ctx, slide, 7);
  return slide;
}

async function slide08(presentation, ctx) {
  const slide = presentation.slides.add();
  background(ctx, slide);
  title(ctx, slide, "07 / 遗物与状态", "遗物系统已经从单点效果发展为事件驱动的构筑层", "不同触发时机共享 RelicExecutionContext，降低新遗物接入成本。");
  node(ctx, slide, "触发入口", "OnBattleSetup\nOnBattleRoundStart\nOnPlayerDicePrepared\nOnBattleRoundEnd", 78, 236, 250, 230, C.gold);
  node(ctx, slide, "统一上下文", "RelicExecutionContext\nallies / enemies\ncurrentRound / speed\nstatus / damage fields", 393, 206, 280, 290, C.cyan);
  node(ctx, slide, "效果实现", "RelicSystem.BuiltIn*\nTryApplyStatus\nfeedbackMessages\nstate mutations", 738, 236, 250, 230, C.green);
  node(ctx, slide, "内容资产", "Relic_SO\nRelicTable_SO\n110 个遗物目录\n按稀有度抽取", 1045, 206, 170, 290, C.purple);
  hline(ctx, slide, 328, 350, 65, C.line); hline(ctx, slide, 673, 350, 65, C.line); hline(ctx, slide, 988, 350, 57, C.line);
  text(ctx, slide, "案例：009「疾时纺梭」在前 3 回合给全体友方急速，暴露并修复了“回合开始状态与速度生成时序”的典型集成问题。", 112, 560, 1050, 48, { size: 20, color: C.sub, align: "center" });
  footer(ctx, slide, 8);
  return slide;
}

async function slide09(presentation, ctx) {
  const slide = presentation.slides.add();
  background(ctx, slide);
  title(ctx, slide, "08 / 内容生产", "内容资产已经形成可扩展的敌人、武器、遗物矩阵", "数据资产数量说明项目不是只停留在框架，而是进入内容填充阶段。");
  const rows = [
    ["遗物", "110", "地图、经济、重铸、速度、燃烧、寒冷、水蚀、死亡继承等构筑方向"],
    ["敌人", "37", "基础、炎、水/寒、电、血、毒、速度/标记、暗/灾厄、防御等系列"],
    ["武器", "21", "技能池、骰子数、威力范围、状态词条与特效资源"],
    ["测试", "196", "EditMode/PlayMode 覆盖地图、战斗、遗物、教程、服务层逻辑"],
  ];
  rows.forEach((r, i) => {
    const y = 210 + i * 92;
    rect(ctx, slide, 90, y, 1100, 68, i % 2 === 0 ? "#202638" : "#1B2030", "#384157");
    text(ctx, slide, r[0], 122, y + 19, 140, 24, { size: 19, bold: true, color: C.gold });
    text(ctx, slide, r[1], 280, y + 11, 110, 34, { size: 30, bold: true, color: C.cyan, align: "right" });
    text(ctx, slide, r[2], 430, y + 18, 690, 25, { size: 17, color: C.sub });
  });
  text(ctx, slide, "统计来自本地 Assets/Data 与 Assets/Tests 目录，作为内容规模与工程投入的答辩证据。", 138, 620, 990, 28, { size: 16, color: C.dim, align: "center" });
  footer(ctx, slide, 9);
  return slide;
}

async function slide10(presentation, ctx) {
  const slide = presentation.slides.add();
  background(ctx, slide);
  title(ctx, slide, "09 / 教程系统", "教程不是说明书，而是把复杂战斗拆成可验证的小目标", "项目已有 TutorialBattleDefinition / Runtime / GuideOverlay / ContentFactory 的骨架。");
  const ladders = [
    ["1", "观察地图", "学习拖动画面、缩放、查看格子详情"],
    ["2", "准备阶段", "理解速度按钮、技能切换、能量消耗"],
    ["3", "连线目标", "第一次拖拽连线并阅读敌方意图"],
    ["4", "拼点与单方面攻击", "区分攻防胜负、速度差和伤害结算"],
    ["5", "状态与遗物", "通过案例理解急速、燃烧、水蚀等跨回合效果"],
  ];
  ladders.forEach((r, i) => {
    const x = 95 + i * 225;
    metric(ctx, slide, r[0], r[1], x, 248, 92, i % 2 ? C.gold : C.cyan);
    text(ctx, slide, r[2], x, 350, 190, 62, { size: 15, color: C.sub });
    if (i < ladders.length - 1) hline(ctx, slide, x + 118, 292, 98, C.line);
  });
  rect(ctx, slide, 195, 525, 890, 64, "#1B2030", "#35405A");
  text(ctx, slide, "答辩价值：教程系统反过来验证规则是否清晰，能帮助发现“玩家看不懂”的设计债。", 222, 546, 840, 22, { size: 20, bold: true, align: "center" });
  footer(ctx, slide, 10);
  return slide;
}

async function slide11(presentation, ctx) {
  const slide = presentation.slides.add();
  background(ctx, slide);
  title(ctx, slide, "10 / UI 与反馈", "项目已经建立从地图 HUD 到战斗反馈的玩家可读层", "后续重点是统一字体、层级、面板状态和移动端适配。");
  await ctx.addImage(slide, { path: IMG.menu, x: 78, y: 210, w: 460, h: 286, fit: "cover" });
  rect(ctx, slide, 78, 210, 460, 286, "#00000000", "#384157");
  node(ctx, slide, "地图侧反馈", "行动点、金币、钥匙、灾厄、队伍血量、格子详情与教程提示。", 595, 210, 275, 126, C.gold);
  node(ctx, slide, "战斗侧反馈", "血条、状态图标、技能详情、伤害跳字、结算动画、敌方意图。", 910, 210, 275, 126, C.cyan);
  node(ctx, slide, "表现与音频", "BattlePresentationSystem、SFX Player、GameMusicConfig 逐步接入。", 595, 390, 275, 126, C.green);
  node(ctx, slide, "待统一项", "Canvas 层级、字号、按钮状态、tooltip、分辨率缩放与性能。", 910, 390, 275, 126, C.red);
  footer(ctx, slide, 11);
  return slide;
}

async function slide12(presentation, ctx) {
  const slide = presentation.slides.add();
  background(ctx, slide);
  title(ctx, slide, "11 / 工程质量", "测试和服务层拆分让复杂规则从大脚本中逐步落地", "项目现在的重点不是堆功能，而是把高风险规则变成可验证单元。");
  metric(ctx, slide, "194", "EditMode 测试脚本", 90, 245, 190, C.green);
  metric(ctx, slide, "2", "PlayMode 测试脚本", 320, 245, 190, C.cyan);
  metric(ctx, slide, "40+", "Shared 服务类", 550, 245, 190, C.gold);
  node(ctx, slide, "已覆盖方向", "地图移动、敌人自动选目标、召唤落点、遗物触发、状态结算、教程流程、音乐解析。", 820, 220, 340, 135, C.green);
  node(ctx, slide, "近期质量案例", "事件弹窗层级、召唤敌人重叠、疾时纺梭时序，均通过定位根因后补测试/补丁。", 820, 395, 340, 135, C.cyan);
  rect(ctx, slide, 110, 430, 610, 102, "#1B2030", "#35405A");
  text(ctx, slide, "工程策略", 136, 452, 120, 22, { size: 18, color: C.gold, bold: true });
  bulletBlock(ctx, slide, [
    "把纯逻辑从 BattleButton / RunGameManager 中抽成 service",
    "优先为跨场景、跨回合、跨资产的规则补 EditMode 测试",
    "PlayMode 用于验证真实战斗流程和 UI 交互"
  ], 260, 447, 420, 76, { size: 15 });
  footer(ctx, slide, 12);
  return slide;
}

async function slide13(presentation, ctx) {
  const slide = presentation.slides.add();
  background(ctx, slide);
  title(ctx, slide, "12 / 技术难点", "最大挑战来自跨系统时序，而不是单个功能实现", "地图、战斗、状态、遗物、UI 表现之间的执行顺序必须可解释。");
  const risks = [
    ["BattleButton 体量大", "逐步拆出 Resolver / Dispatcher / Service，降低技能扩展风险", C.red],
    ["RunGameManager 职责多", "地图、商店、事件、奖励、UI 刷新继续拆分 runtime/service", C.gold],
    ["状态与遗物时序", "通过 RelicExecutionContext 和 round-start 测试锁住边界", C.cyan],
    ["移动端 UI 适配", "统一 Canvas 层级、字体规格、tooltip 与弹窗排序", C.green],
  ];
  risks.forEach((r, i) => {
    const y = 210 + i * 88;
    rect(ctx, slide, 110, y, 1060, 62, i % 2 ? "#1B2030" : "#202638", "#384157");
    text(ctx, slide, r[0], 138, y + 18, 250, 22, { size: 18, bold: true, color: r[2] });
    text(ctx, slide, r[1], 420, y + 17, 685, 24, { size: 17, color: C.sub });
  });
  text(ctx, slide, "难点处理原则：先定位真实根因，再用最小补丁和回归测试固定行为。", 145, 600, 990, 30, { size: 22, bold: true, align: "center" });
  footer(ctx, slide, 13);
  return slide;
}

async function slide14(presentation, ctx) {
  const slide = presentation.slides.add();
  background(ctx, slide);
  title(ctx, slide, "13 / 后续规划", "后续路线从稳定核心、扩展内容到移动端体验收束", "每个阶段都围绕可验证交付，而不是一次性重写。");
  const phases = [
    ["近期：稳定核心", "跑通测试命令\n拆分 BattleButton 高风险逻辑\n补齐疾时纺梭等时序测试", C.cyan],
    ["中期：内容扩展", "敌人池分层投放\n燃烧/寒冷/水蚀构筑校准\n商店/重铸奖励过滤统一", C.gold],
    ["后期：体验打磨", "教程章节完善\nUI 风格配置化\n移动端性能与触控适配", C.green],
  ];
  phases.forEach((p, i) => {
    const x = 102 + i * 370;
    node(ctx, slide, p[0], p[1], x, 240, 300, 230, p[2]);
    if (i < phases.length - 1) hline(ctx, slide, x + 300, 350, 70, C.line);
  });
  text(ctx, slide, "里程碑目标：从“功能骨架可运行”推进到“可稳定演示、可持续生产内容、可移动端发布”。", 165, 560, 950, 34, { size: 21, bold: true, align: "center" });
  footer(ctx, slide, 14);
  return slide;
}

async function slide15(presentation, ctx) {
  const slide = presentation.slides.add();
  background(ctx, slide);
  text(ctx, slide, "总结", 60, 54, 200, 50, { size: 24, color: C.gold, bold: true });
  text(ctx, slide, "本项目已经形成可演示、可扩展、可继续工程化的游戏原型", 60, 112, 900, 94, { size: 38, bold: true });
  const claims = [
    ["玩法闭环", "地图探索、行动点、灾厄、战斗结算和奖励回写已经串成完整 Roguelite 流程。"],
    ["系统深度", "速度按钮、拼点、状态、遗物和敌人规则形成可构筑的回合制战斗核心。"],
    ["工程基础", "数据资产、服务层拆分与测试体系为后续内容扩展和移动端打磨提供支撑。"],
  ];
  claims.forEach((c, i) => node(ctx, slide, c[0], c[1], 95 + i * 370, 285, 300, 155, [C.cyan, C.gold, C.green][i]));
  rect(ctx, slide, 225, 520, 830, 72, "#1B2030", "#384157");
  text(ctx, slide, "答辩结束，欢迎老师和同学提问。", 225, 543, 830, 30, { size: 26, bold: true, align: "center" });
  footer(ctx, slide, 15);
  return slide;
}

export const renderers = [
  slide01, slide02, slide03, slide04, slide05,
  slide06, slide07, slide08, slide09, slide10,
  slide11, slide12, slide13, slide14, slide15,
];

export async function renderSlide(presentation, ctx, index) {
  return renderers[index - 1](presentation, ctx);
}
