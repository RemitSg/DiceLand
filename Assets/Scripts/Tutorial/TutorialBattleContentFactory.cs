using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class TutorialEnemyVisualTemplateSet
{
    public EnemyData_SO openingPrimaryTemplate;
    public EnemyData_SO openingSecondaryTemplate;
    public EnemyData_SO eliteTemplate;
    public EnemyData_SO bossTemplate;
}

public static class TutorialBattleContentFactory
{
    public static TutorialBattleDefinition_SO CreateRuntimeDefinition(MapTileType tileType)
    {
        switch (tileType)
        {
            case MapTileType.Elite:
                return CreateEliteDefinition();
            case MapTileType.Boss:
                return CreateBossDefinition();
            case MapTileType.Battle:
            default:
                return CreateOpeningDefinition();
        }
    }

    public static EnemyGroup_SO CreateRuntimeEnemyGroup(MapTileType tileType, TutorialEnemyVisualTemplateSet visualTemplates = null)
    {
        switch (tileType)
        {
            case MapTileType.Elite:
                return CreateEliteEnemyGroup(visualTemplates);
            case MapTileType.Boss:
                return CreateBossEnemyGroup(visualTemplates);
            case MapTileType.Battle:
            default:
                return CreateOpeningEnemyGroup(visualTemplates);
        }
    }

    private static TutorialBattleDefinition_SO CreateOpeningDefinition()
    {
        TutorialBattleDefinition_SO definition = CreateDefinition("tutorial_battle_opening", "战斗教程：威力修正");
        definition.Steps.Add(Step(
            "opening_intro",
            0,
            "先看敌人的技能标签",
            "这一战重点是分清三种威力修正：\n\n1. [ 使用该技能时 ]：结算这颗骰子时就会生效，拼点和单方面攻击都吃得到。\n2. [ 拼点时 ]：只有连线被对上、真的进入拼点时才会生效。\n3. [ 单方面攻击时 ]：只有没人拦住、直接打出去时才会生效。\n\n先点开任意一名敌人的速度按钮，确认它这回合打算用什么技能，以及技能面板上写的是哪一种标签。",
            TutorialBattleEventType.EnemySkillViewed,
            TutorialBattleInputPolicy.RequireBeforeCombat,
            "先查看一名敌人的本回合技能。"));
        definition.Steps.Add(Step(
            "inspect_ally_modifiers",
            10,
            "对比我方三种修正",
            "再点开我方技能列表，重点比较这三种临时技能：\n\n- 先手试探：偏向 [ 使用该技能时 ]，任何出手方式都比较稳定。\n- 迎击断招：偏向 [ 拼点时 ]，适合拿去接正面对撞。\n- 追击重击：偏向 [ 单方面攻击时 ]，更适合打没人拦的目标。\n\n先把这三招的标签都看清楚，再继续。",
            TutorialBattleEventType.AllySkillViewed,
            TutorialBattleInputPolicy.RequireBeforeCombat,
            "先查看我方技能说明。"));
        definition.Steps.Add(Step(
            "switch_skill",
            20,
            "先做一次技能切换",
            "请先切一次技能。\n\n这里要开始形成判断：\n- 你想去接一条需要拼点的线，就优先考虑带 [ 拼点时 ] 修正的技能。\n- 你准备放掉某条线、转而去打空门，就优先考虑带 [ 单方面攻击时 ] 修正的技能。\n- 看不准时，带 [ 使用该技能时 ] 修正的技能最稳。\n\n先完成一次切换，确认你能主动选技能。",
            TutorialBattleEventType.SkillSwitched,
            TutorialBattleInputPolicy.RequireBeforeCombat,
            "先切换一次技能。"));
        definition.Steps.Add(Step(
            "reserve_energy",
            30,
            "再试一次带能量消耗的连线",
            "现在把技能切到【追击重击】，再完成一次连线。\n\n这一步要同时观察两件事：\n- 顶部能量会先进入“预留”，不是立刻扣除。\n- 如果这条线最后变成拼点，【单方面攻击时】修正不会生效；如果它顺利直击目标，这个修正才会吃满。\n\n先用这招连一次线。",
            TutorialBattleEventType.EnergyLineCreated,
            TutorialBattleInputPolicy.RequireBeforeCombat,
            "先用会消耗能量的技能完成一次连线。"));
        definition.Steps.Add(Step(
            "start_round",
            40,
            "开始结算并观察结果",
            "现在按下战斗按钮进入结算。\n\n结算时请重点观察：\n- 真正发生拼点的那条线，谁吃到了 [ 拼点时 ] 修正。\n- 没人拦住的那条线，谁吃到了 [ 单方面攻击时 ] 修正。\n- 不管是拼点还是单方面攻击，[ 使用该技能时 ] 修正都会先算进去。\n\n看完这一轮，你就要开始判断：哪条线值得接，哪条线可以放。",
            TutorialBattleEventType.CombatExecutePressed,
            TutorialBattleInputPolicy.RequireBeforeCombat,
            "再按一次战斗按钮开始结算。"));

        definition.Reactions.Add(Reaction(
            "clash_explained",
            0,
            TutorialBattleEventType.ClashResolved,
            "拼点时会吃到什么修正",
            "进入拼点后，先参与比较的是这颗骰子的拼点威力。\n\n这时会生效的是：\n- [ 使用该技能时 ] 修正\n- [ 拼点时 ] 修正\n\n不会生效的是 [ 单方面攻击时 ] 修正。\n\n所以看见敌人写着高额 [ 拼点时 ] 修正时，最好用【迎击断招】这类拼点技能去接；如果你拿【追击重击】去拼点，它最重要的加成反而吃不到。"));
        definition.Reactions.Add(Reaction(
            "ally_one_sided_explained",
            10,
            TutorialBattleEventType.AllyOneSidedAttackResolved,
            "我方单方面攻击时会吃到什么修正",
            "当你的线没有被挡住时，就会进入单方面攻击。\n\n这时会生效的是：\n- [ 使用该技能时 ] 修正\n- [ 单方面攻击时 ] 修正\n\n不会生效的是 [ 拼点时 ] 修正。\n\n这就是为什么【追击重击】适合拿去打空门，而【迎击断招】更适合拿去对撞。选技能前，先判断这条线大概率会不会被拦。"));
        definition.Reactions.Add(Reaction(
            "enemy_one_sided_explained",
            20,
            TutorialBattleEventType.EnemyOneSidedAttackResolved,
            "什么时候可以放掉一条线",
            "不是每条敌人攻击都必须硬接。\n\n判断顺序可以是：\n- 先看敌人有没有高额 [ 单方面攻击时 ] 修正。如果有，这条线通常更危险，优先考虑拦下。\n- 如果敌人主要是 [ 拼点时 ] 修正，而你这回合又很难拼赢，有时可以把它放掉，转去处理另一条更危险或更值钱的线。\n- 只有 [ 使用该技能时 ] 修正的技能通常更稳定，但爆发不一定最高。\n\n教程战斗后面几回合也会按固定顺序继续演示这些差别。"));
        definition.Reactions.Add(Reaction(
            "opening_status_hint",
            30,
            TutorialBattleEventType.BattleWon,
            "继续往下看",
            "下一战会把双按钮敌人、状态与技能词条一起放进同一套教学里。重点还是同一个原则：先看标签，再判断这条线要不要拼。"));
        return definition;
    }

    private static TutorialBattleDefinition_SO CreateEliteDefinition()
    {
        TutorialBattleDefinition_SO definition = CreateDefinition("tutorial_battle_elite", "战斗教程：词条与状态");
        definition.Steps.Add(Step(
            "elite_intro",
            0,
            "先看我方技能说明",
            "这一战的敌人有两个速度按钮，而且会按固定顺序先点燃、再追击。\n\n先看一次我方技能面板，继续熟悉：\n- [ 使用该技能时 ] / [ 拼点时 ] / [ 单方面攻击时 ] 的差别\n- 技能消耗的能量\n- 技能词条写的是加威力、上状态，还是消耗状态\n\n看完后继续。",
            TutorialBattleEventType.AllySkillViewed,
            TutorialBattleInputPolicy.RequireBeforeCombat,
            "先查看一次我方技能说明。"));
        definition.Steps.Add(Step(
            "inspect_enemy_detail",
            10,
            "看懂敌人的双按钮顺序",
            "再打开敌人详情。\n\n这场战斗建议重点看：\n- 哪个按钮负责先挂状态\n- 哪个按钮负责吃状态收益\n- 哪一招更适合你去拼点，哪一招更应该避免放成单方面攻击\n\n敌人的出招顺序是固定的，所以你可以把这场当成读招练习。",
            TutorialBattleEventType.EnemyDetailViewed,
            TutorialBattleInputPolicy.RequireBeforeCombat,
            "先查看敌人详情。"));
        definition.Steps.Add(Step(
            "status_round",
            20,
            "实际观察状态落地",
            "开始结算后，留意状态是如何进入战场的：\n- 谁先附加状态\n- 状态有几层\n- 下一个技能是怎么利用这些状态的\n\n只要战斗里出现一次明确的状态变化，这一步就算完成。",
            TutorialBattleEventType.StatusApplied,
            TutorialBattleInputPolicy.Informational,
            "先让战斗里出现一次状态变化。"));

        definition.Reactions.Add(Reaction(
            "status_explained",
            0,
            TutorialBattleEventType.StatusApplied,
            "状态与词条是连着看的",
            "这一战的敌人会按固定顺序先挂状态、再利用状态。\n\n读招时建议一起看三件事：\n- 技能本身带什么修正标签\n- 命中后会附加什么状态\n- 后续技能会不会因为目标身上的状态再拿到额外收益\n\n同样的威力区间，因为标签和状态触发时机不同，实际危险度会差很多。"));
        definition.Reactions.Add(Reaction(
            "status_observed",
            10,
            TutorialBattleEventType.StatusObserved,
            "读状态时重点看什么",
            "打开技能或敌人详情时，优先看彩色词条、触发时机和层数。\n\n如果一招写的是 [ 单方面攻击时 ] 再引爆状态，那它最怕的就是被你拉进拼点；如果一招写的是 [ 拼点时 ] 才加威力，它就更适合用来顶正面。"));
        return definition;
    }

    private static TutorialBattleDefinition_SO CreateBossDefinition()
    {
        TutorialBattleDefinition_SO definition = CreateDefinition("tutorial_battle_boss", "战斗教程：综合演练");
        definition.Steps.Add(Step(
            "boss_intro",
            0,
            "综合判断哪条线该接",
            "最后一战的 Boss 也会按固定顺序出两招。\n\n开始前先自己做一次完整判断：\n- 先看两条线分别是什么技能、各自带哪种威力修正\n- 再判断哪条线值得拼点、哪条线可以放掉\n- 最后确认自己的技能是否选对，能量是否够用\n\n准备好后开始结算。",
            TutorialBattleEventType.CombatExecutePressed,
            TutorialBattleInputPolicy.RequireBeforeCombat,
            "准备好后再开始最终战。"));

        definition.Reactions.Add(Reaction(
            "boss_victory_hint",
            0,
            TutorialBattleEventType.BattleWon,
            "教程完成",
            "你已经完成了战斗系统教程。\n\n如果之后要继续改教学顺序、文案或敌人行为，优先改这一套教程定义和教程敌人的脚本化出招规则即可，不需要去改正常战斗流程。"));
        return definition;
    }

    private static EnemyGroup_SO CreateOpeningEnemyGroup(TutorialEnemyVisualTemplateSet visualTemplates)
    {
        EnemySkill_SO teacherClash = CreateSkill("正面压制", "主要依赖 [ 拼点时 ] 威力修正的正面对撞技能。", 0, 2, 4);
        SetPowerModifiers(teacherClash, CreateFlatPowerModifier(EnemySkill_SO.DiceEffectTrigger.OnClash, 2, "正面压制"));

        EnemySkill_SO teacherSteady = CreateSkill("稳压挥击", "只带小额 [ 使用该技能时 ] 修正，稳定但爆发一般。", 0, 2, 4);
        SetPowerModifiers(teacherSteady, CreateFlatPowerModifier(EnemySkill_SO.DiceEffectTrigger.OnSkillUse, 1, "稳压起手"));

        EnemySkill_SO strikerProbe = CreateSkill("疾行试探", "速度更快，但只靠 [ 使用该技能时 ] 修正稳定出手。", 0, 1, 2);
        SetPowerModifiers(strikerProbe, CreateFlatPowerModifier(EnemySkill_SO.DiceEffectTrigger.OnSkillUse, 1, "快步起手"));

        EnemySkill_SO strikerExecution = CreateSkill("处决突刺", "如果没被拦下，会靠 [ 单方面攻击时 ] 修正明显变强。", 0, 2, 4);
        SetPowerModifiers(strikerExecution, CreateFlatPowerModifier(EnemySkill_SO.DiceEffectTrigger.OnSingleAttack, 3, "无人阻挡"));

        EnemySkill_SO allyStable = CreateSkill("先手试探", "带 [ 使用该技能时 ] 威力修正，拼点和单方面攻击都能吃到。", 0, 2, 4);
        SetPowerModifiers(allyStable, CreateFlatPowerModifier(EnemySkill_SO.DiceEffectTrigger.OnSkillUse, 1, "稳定起手"));

        EnemySkill_SO allyClash = CreateSkill("迎击断招", "带高额 [ 拼点时 ] 威力修正，专门用来接正面拼点。", 0, 2, 4);
        SetPowerModifiers(allyClash, CreateFlatPowerModifier(EnemySkill_SO.DiceEffectTrigger.OnClash, 3, "迎击发力"));

        EnemySkill_SO allySingle = CreateSkill("追击重击", "消耗 1 点能量，主要依赖 [ 单方面攻击时 ] 威力修正打空门。", 1, 3, 6);
        SetPowerModifiers(allySingle, CreateFlatPowerModifier(EnemySkill_SO.DiceEffectTrigger.OnSingleAttack, 4, "追击加压"));

        EnemyData_SO teacher = CreateEnemy(
            "训练监督者",
            22,
            3,
            5,
            1,
            new[] { teacherClash, teacherSteady },
            new[] { allyStable, allyClash, allySingle });
        teacher.enemyDescription = "固定按顺序示范“拼点技能”和“稳定技能”的区别。";
        AddRoundSequenceRule(teacher, "Round 1: Clash Demo", teacherClash, 1, 200);
        AddRoundSequenceRule(teacher, "Round 2: Stable Demo", teacherSteady, 2, 200);
        AddRoundSequenceRule(teacher, "Round 3: Clash Demo", teacherClash, 3, 200);

        EnemyData_SO striker = CreateEnemy(
            "快袭陪练",
            18,
            5,
            7,
            1,
            new[] { strikerProbe, strikerExecution },
            null);
        striker.enemyDescription = "固定按顺序示范“低威胁放行”和“危险单方面攻击”的区别。";
        AddRoundSequenceRule(striker, "Round 1: Probe", strikerProbe, 1, 200);
        AddRoundSequenceRule(striker, "Round 2: Execution", strikerExecution, 2, 200);
        AddRoundSequenceRule(striker, "Round 3: Probe", strikerProbe, 3, 200);

        ApplyVisualTemplate(teacher, visualTemplates != null ? visualTemplates.openingPrimaryTemplate : null);
        ApplyVisualTemplate(striker, visualTemplates != null ? visualTemplates.openingSecondaryTemplate : null);

        EnemyGroup_SO group = ScriptableObject.CreateInstance<EnemyGroup_SO>();
        PrepareRuntimeObject(group, "TutorialBattleGroup_Opening");
        group.groupID = 9101;
        group.groupName = "基础战斗教学";
        group.description = "用于教学威力修正、技能切换、能量预留与接线取舍。";
        group.enemies = new List<EnemyData_SO> { teacher, striker };
        return group;
    }

    private static EnemyGroup_SO CreateEliteEnemyGroup(TutorialEnemyVisualTemplateSet visualTemplates)
    {
        EnemySkill_SO ignite = CreateSkill("引燃符印", "先手挂燃烧，并带小额 [ 使用该技能时 ] 修正。", 0, 2, 4);
        SetPowerModifiers(ignite, CreateFlatPowerModifier(EnemySkill_SO.DiceEffectTrigger.OnSkillUse, 1, "符印起手"));
        SetStatusEffects(ignite, new EnemySkill_SO.StatusEffectEntry
        {
            trigger = EnemySkill_SO.DiceEffectTrigger.OnHit,
            targetMode = EnemySkill_SO.DamageTargetMode.CurrentTarget,
            statusType = EnemySkill_SO.DiceStatusType.Burn,
            applyTiming = EnemySkill_SO.StatusApplyTiming.Immediate,
            stackAmount = 2,
            randomTargetCount = 1
        });

        EnemySkill_SO clashPressure = CreateSkill("灼痕逼迫", "适合拿来对撞的 [ 拼点时 ] 技能。", 0, 3, 5);
        SetPowerModifiers(clashPressure, CreateFlatPowerModifier(EnemySkill_SO.DiceEffectTrigger.OnClash, 2, "逼迫拆招"));

        EnemySkill_SO burnBurst = CreateSkill("余烬追击", "更适合放成单方面攻击，并会利用目标身上的燃烧。", 0, 3, 5);
        SetPowerModifiers(burnBurst, CreateFlatPowerModifier(EnemySkill_SO.DiceEffectTrigger.OnSingleAttack, 2, "追击压制"));
        SetStatusBurstEffects(burnBurst, new EnemySkill_SO.StatusBurstDamageEffectEntry
        {
            trigger = EnemySkill_SO.DiceEffectTrigger.OnHit,
            targetMode = EnemySkill_SO.DamageTargetMode.CurrentTarget,
            statusType = EnemySkill_SO.DiceStatusType.Burn,
            damagePerStack = 1,
            randomTargetCount = 1,
            clearStacksAfterBurst = false
        });

        EnemySkill_SO allyStable = CreateSkill("稳态处理", "带 [ 使用该技能时 ] 修正，适合在读招不准时保底。", 0, 2, 4);
        SetPowerModifiers(allyStable, CreateFlatPowerModifier(EnemySkill_SO.DiceEffectTrigger.OnSkillUse, 1, "稳态起手"));

        EnemySkill_SO allyClash = CreateSkill("观测拆招", "带 [ 拼点时 ] 修正，适合去接会挂状态的那一招。", 0, 2, 4);
        SetPowerModifiers(allyClash, CreateFlatPowerModifier(EnemySkill_SO.DiceEffectTrigger.OnClash, 3, "抢先拆招"));

        EnemySkill_SO allySingle = CreateSkill("压制射击", "消耗 1 点能量，带 [ 单方面攻击时 ] 修正，适合打没被挡住的按钮。", 1, 3, 6);
        SetPowerModifiers(allySingle, CreateFlatPowerModifier(EnemySkill_SO.DiceEffectTrigger.OnSingleAttack, 3, "压制追击"));

        EnemyData_SO caster = CreateEnemy(
            "施灼术士",
            30,
            4,
            6,
            2,
            new[] { ignite, clashPressure, burnBurst },
            new[] { allyStable, allyClash, allySingle });
        caster.enemyDescription = "双按钮固定顺序：先挂燃烧，再用另一招逼你判断要不要拼。";
        caster.passiveDescriptions = new List<EnemyData_SO.PassiveDescriptionEntry>
        {
            new EnemyData_SO.PassiveDescriptionEntry
            {
                passiveName = "固定教学顺序",
                displayColorType = EnemyData_SO.PassiveDisplayColorType.Yellow,
                description = "第一回合先点燃再对压，第二回合先追击再补点燃，方便你稳定观察状态与修正。"
            }
        };
        AddRoundSequenceRule(caster, "Round 1 Slot 1: Ignite", ignite, 1, 200, 1);
        AddRoundSequenceRule(caster, "Round 1 Slot 2: Clash Pressure", clashPressure, 1, 100, 1);
        AddRoundSequenceRule(caster, "Round 2 Slot 1: Burn Burst", burnBurst, 2, 200, 1);
        AddRoundSequenceRule(caster, "Round 2 Slot 2: Ignite", ignite, 2, 100, 1);
        AddRoundSequenceRule(caster, "Round 3 Slot 1: Clash Pressure", clashPressure, 3, 200, 1);
        AddRoundSequenceRule(caster, "Round 3 Slot 2: Burn Burst", burnBurst, 3, 100, 1);

        ApplyVisualTemplate(caster, visualTemplates != null ? visualTemplates.eliteTemplate : null);

        EnemyGroup_SO group = ScriptableObject.CreateInstance<EnemyGroup_SO>();
        PrepareRuntimeObject(group, "TutorialBattleGroup_Elite");
        group.groupID = 9102;
        group.groupName = "状态教学战";
        group.description = "用于教学双按钮敌人、状态施加与状态利用。";
        group.enemies = new List<EnemyData_SO> { caster };
        return group;
    }

    private static EnemyGroup_SO CreateBossEnemyGroup(TutorialEnemyVisualTemplateSet visualTemplates)
    {
        EnemySkill_SO bossClash = CreateSkill("正面处刑", "高额 [ 拼点时 ] 修正，专门逼你正面判断。", 0, 4, 7);
        SetPowerModifiers(bossClash, CreateFlatPowerModifier(EnemySkill_SO.DiceEffectTrigger.OnClash, 3, "正面碾压"));

        EnemySkill_SO bossSingle = CreateSkill("压制追猎", "高额 [ 单方面攻击时 ] 修正，放掉会很痛。", 0, 3, 6);
        SetPowerModifiers(bossSingle, CreateFlatPowerModifier(EnemySkill_SO.DiceEffectTrigger.OnSingleAttack, 3, "追猎处决"));

        EnemySkill_SO bossBurn = CreateSkill("灼烙号令", "小额 [ 使用该技能时 ] 修正，命中后补燃烧。", 0, 3, 5);
        SetPowerModifiers(bossBurn, CreateFlatPowerModifier(EnemySkill_SO.DiceEffectTrigger.OnSkillUse, 1, "号令起手"));
        SetStatusEffects(bossBurn, new EnemySkill_SO.StatusEffectEntry
        {
            trigger = EnemySkill_SO.DiceEffectTrigger.OnHit,
            targetMode = EnemySkill_SO.DamageTargetMode.CurrentTarget,
            statusType = EnemySkill_SO.DiceStatusType.Burn,
            applyTiming = EnemySkill_SO.StatusApplyTiming.Immediate,
            stackAmount = 1,
            randomTargetCount = 1
        });

        EnemySkill_SO allyStable = CreateSkill("稳态迎击", "带 [ 使用该技能时 ] 修正，适合保底。", 0, 2, 4);
        SetPowerModifiers(allyStable, CreateFlatPowerModifier(EnemySkill_SO.DiceEffectTrigger.OnSkillUse, 1, "稳定迎击"));

        EnemySkill_SO allyClash = CreateSkill("拆招重斩", "带高额 [ 拼点时 ] 修正，适合去接 Boss 的正面对撞。", 0, 3, 5);
        SetPowerModifiers(allyClash, CreateFlatPowerModifier(EnemySkill_SO.DiceEffectTrigger.OnClash, 3, "拆招重压"));

        EnemySkill_SO allySingle = CreateSkill("处决贯穿", "消耗 1 点能量，带高额 [ 单方面攻击时 ] 修正。", 1, 3, 6);
        SetPowerModifiers(allySingle, CreateFlatPowerModifier(EnemySkill_SO.DiceEffectTrigger.OnSingleAttack, 4, "贯穿追击"));

        EnemyData_SO boss = CreateEnemy(
            "教学终端",
            44,
            4,
            7,
            2,
            new[] { bossClash, bossSingle, bossBurn },
            new[] { allyStable, allyClash, allySingle });
        boss.enemyDescription = "双按钮固定顺序的综合演练敌人，要求你同时判断拼点、放行与状态压力。";
        AddRoundSequenceRule(boss, "Round 1 Slot 1: Clash", bossClash, 1, 200, 1);
        AddRoundSequenceRule(boss, "Round 1 Slot 2: Single", bossSingle, 1, 100, 1);
        AddRoundSequenceRule(boss, "Round 2 Slot 1: Burn", bossBurn, 2, 200, 1);
        AddRoundSequenceRule(boss, "Round 2 Slot 2: Clash", bossClash, 2, 100, 1);
        AddRoundSequenceRule(boss, "Round 3 Slot 1: Single", bossSingle, 3, 200, 1);
        AddRoundSequenceRule(boss, "Round 3 Slot 2: Burn", bossBurn, 3, 100, 1);

        ApplyVisualTemplate(boss, visualTemplates != null ? visualTemplates.bossTemplate : null);

        EnemyGroup_SO group = ScriptableObject.CreateInstance<EnemyGroup_SO>();
        PrepareRuntimeObject(group, "TutorialBattleGroup_Boss");
        group.groupID = 9103;
        group.groupName = "战斗教程终战";
        group.description = "用于总结威力修正、状态与双按钮读招。";
        group.enemies = new List<EnemyData_SO> { boss };
        return group;
    }

    private static TutorialBattleDefinition_SO CreateDefinition(string id, string displayName)
    {
        TutorialBattleDefinition_SO definition = ScriptableObject.CreateInstance<TutorialBattleDefinition_SO>();
        PrepareRuntimeObject(definition, $"Definition_{id}");
        definition.tutorialBattleId = id;
        definition.displayName = displayName;
        return definition;
    }

    private static TutorialBattleStepDefinition Step(
        string id,
        int sortOrder,
        string title,
        string body,
        TutorialBattleEventType completionEvent,
        TutorialBattleInputPolicy inputPolicy,
        string blockedMessage)
    {
        return new TutorialBattleStepDefinition
        {
            stepId = id,
            sortOrder = sortOrder,
            title = title,
            body = body,
            completionEvent = completionEvent,
            inputPolicy = inputPolicy,
            blockedMessage = blockedMessage,
            continueButtonLabel = "继续",
            autoShow = true
        };
    }

    private static TutorialBattleReactionDefinition Reaction(
        string id,
        int sortOrder,
        TutorialBattleEventType triggerEvent,
        string title,
        string body)
    {
        return new TutorialBattleReactionDefinition
        {
            reactionId = id,
            sortOrder = sortOrder,
            triggerEvent = triggerEvent,
            title = title,
            body = body,
            continueButtonLabel = "明白了",
            pauseForAcknowledgement = true
        };
    }

    private static EnemySkill_SO CreateSkill(string name, string description, int energyCost, int minPower, int maxPower)
    {
        EnemySkill_SO skill = ScriptableObject.CreateInstance<EnemySkill_SO>();
        PrepareRuntimeObject(skill, $"Skill_{name}");
        skill.skillName = name;
        skill.description = description;
        skill.energyCost = energyCost;
        skill.minPower = minPower;
        skill.maxPower = maxPower;
        skill.diceCount = 1;
        skill.skillCategory = EnemySkill_SO.SkillCategory.Attack;
        return skill;
    }

    private static EnemyData_SO CreateEnemy(
        string name,
        int hp,
        int minSpeed,
        int maxSpeed,
        int speedButtonCount,
        IEnumerable<EnemySkill_SO> enemySkills,
        IEnumerable<EnemySkill_SO> allyGrantedSkills)
    {
        EnemyData_SO enemy = ScriptableObject.CreateInstance<EnemyData_SO>();
        PrepareRuntimeObject(enemy, $"Enemy_{name}");
        enemy.enemyName = name;
        enemy.baseHP = hp;
        enemy.minSpeed = minSpeed;
        enemy.maxSpeed = maxSpeed;
        enemy.baseSpeedButtonCount = speedButtonCount;
        enemy.skillPool = new List<EnemySkill_SO>();
        enemy.conditionalSkillRules = new List<EnemyData_SO.ConditionalSkillRule>();
        enemy.battleStartGrantedAllySkills = new List<EnemyData_SO.BattleStartAllySkillGrantEntry>();

        if (enemySkills != null)
        {
            foreach (EnemySkill_SO skill in enemySkills)
            {
                if (skill == null)
                    continue;

                enemy.skillPool.Add(skill);
            }
        }

        if (allyGrantedSkills != null)
        {
            foreach (EnemySkill_SO skill in allyGrantedSkills)
            {
                if (skill == null)
                    continue;

                enemy.battleStartGrantedAllySkills.Add(new EnemyData_SO.BattleStartAllySkillGrantEntry
                {
                    skill = skill,
                    replaceWeaponSkills = true
                });
            }
        }

        return enemy;
    }

    private static void AddRoundSequenceRule(
        EnemyData_SO enemy,
        string ruleName,
        EnemySkill_SO skill,
        int firstTriggerRound,
        int priority,
        int maxSlotsPerRound = 1)
    {
        if (enemy == null || skill == null)
            return;

        if (enemy.conditionalSkillRules == null)
            enemy.conditionalSkillRules = new List<EnemyData_SO.ConditionalSkillRule>();

        enemy.conditionalSkillRules.Add(new EnemyData_SO.ConditionalSkillRule
        {
            ruleName = ruleName,
            skill = skill,
            conditionType = EnemyData_SO.ConditionalSkillConditionType.RoundSequence,
            firstTriggerRound = Mathf.Max(1, firstTriggerRound),
            repeatEveryNRounds = 1,
            priority = Mathf.Max(0, priority),
            maxSlotsPerRound = Mathf.Max(1, maxSlotsPerRound),
            useOncePerBattle = true
        });
    }

    private static EnemySkill_SO.ConditionalPowerModifierEntry CreateFlatPowerModifier(
        EnemySkill_SO.DiceEffectTrigger trigger,
        int powerBonus,
        string effectName)
    {
        return new EnemySkill_SO.ConditionalPowerModifierEntry
        {
            effectName = effectName,
            trigger = trigger,
            useCondition = false,
            powerBonus = powerBonus
        };
    }

    private static void SetPowerModifiers(EnemySkill_SO skill, params EnemySkill_SO.ConditionalPowerModifierEntry[] modifiers)
    {
        if (skill == null || modifiers == null || modifiers.Length == 0)
            return;

        EnemySkill_SO.DiceEffectSettings[] settingsArray = EnsureDiceEffectSettings(skill);
        EnemySkill_SO.DiceEffectSettings settings = settingsArray[0];
        settings.powerModifiers = modifiers;
        settingsArray[0] = settings;
        skill.diceEffectSettings = settingsArray;
    }

    private static void SetStatusEffects(EnemySkill_SO skill, params EnemySkill_SO.StatusEffectEntry[] effects)
    {
        if (skill == null || effects == null || effects.Length == 0)
            return;

        EnemySkill_SO.DiceEffectSettings[] settingsArray = EnsureDiceEffectSettings(skill);
        EnemySkill_SO.DiceEffectSettings settings = settingsArray[0];
        settings.statusEffects = effects;
        settingsArray[0] = settings;
        skill.diceEffectSettings = settingsArray;
    }

    private static void SetStatusBurstEffects(EnemySkill_SO skill, params EnemySkill_SO.StatusBurstDamageEffectEntry[] effects)
    {
        if (skill == null || effects == null || effects.Length == 0)
            return;

        EnemySkill_SO.DiceEffectSettings[] settingsArray = EnsureDiceEffectSettings(skill);
        EnemySkill_SO.DiceEffectSettings settings = settingsArray[0];
        settings.statusBurstEffects = effects;
        settingsArray[0] = settings;
        skill.diceEffectSettings = settingsArray;
    }

    private static EnemySkill_SO.DiceEffectSettings[] EnsureDiceEffectSettings(EnemySkill_SO skill)
    {
        if (skill.diceEffectSettings == null || skill.diceEffectSettings.Length == 0)
            skill.diceEffectSettings = new[] { new EnemySkill_SO.DiceEffectSettings() };

        return skill.diceEffectSettings;
    }

    private static void ApplyVisualTemplate(EnemyData_SO target, EnemyData_SO template)
    {
        if (target == null || template == null)
            return;

        target.enemySprite = template.enemySprite;
        target.enemyVisualScale = template.enemyVisualScale;
        target.enemyVisualOffset = template.enemyVisualOffset;
        target.speedButtonOffset = template.speedButtonOffset;

        if (template.stageSprites != null && template.stageSprites.Count > 0)
        {
            target.stageSprites = new List<EnemyData_SO.EnemyStageEntry>(template.stageSprites.Count);
            for (int i = 0; i < template.stageSprites.Count; i++)
            {
                EnemyData_SO.EnemyStageEntry stage = template.stageSprites[i];
                target.stageSprites.Add(new EnemyData_SO.EnemyStageEntry
                {
                    stageName = stage.stageName,
                    stageSprite = stage.stageSprite,
                    minSpeedOverride = 0,
                    maxSpeedOverride = 0,
                    speedButtonCountOverride = 0
                });
            }
        }
    }

    private static void PrepareRuntimeObject(Object target, string name)
    {
        if (target == null)
            return;

        target.name = name;
        target.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
    }
}
