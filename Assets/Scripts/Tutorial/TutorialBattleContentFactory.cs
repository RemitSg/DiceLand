using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class TutorialEnemyVisualTemplateSet
{
    public EnemyData_SO openingPrimaryTemplate;
    public EnemyData_SO openingSecondaryTemplate;
    public EnemyData_SO eliteTemplate;
    public EnemyData_SO bossTemplate;
    public BattleStatus_SO burnStatusTemplate;
}

public static class TutorialBattleContentFactory
{
    public static TutorialBattleDefinition_SO CreateRuntimeDefinition(TutorialBattleStageId stageId)
    {
        switch (stageId)
        {
            case TutorialBattleStageId.Battle01:
                return CreateBattle01Definition();
            case TutorialBattleStageId.Battle02:
                return CreateBattle02Definition();
            case TutorialBattleStageId.Battle03:
                return CreateBattle03Definition();
            case TutorialBattleStageId.Battle04:
                return CreateBattle04Definition();
            case TutorialBattleStageId.Battle05:
                return CreateMergedBattle05Definition();
            case TutorialBattleStageId.Battle06:
                return CreateMergedBattle05Definition();
            case TutorialBattleStageId.Battle07:
                return CreateBattle07Definition();
            default:
                return null;
        }
    }

    public static EnemyGroup_SO CreateRuntimeEnemyGroup(TutorialBattleStageId stageId, TutorialEnemyVisualTemplateSet visualTemplates = null)
    {
        BattleStatus_SO burnStatus = visualTemplates != null ? visualTemplates.burnStatusTemplate : null;
        switch (stageId)
        {
            case TutorialBattleStageId.Battle01:
                return CreateBattle01EnemyGroup(visualTemplates);
            case TutorialBattleStageId.Battle02:
                return CreateBattle02EnemyGroup(visualTemplates);
            case TutorialBattleStageId.Battle03:
                return CreateBattle03EnemyGroup(visualTemplates);
            case TutorialBattleStageId.Battle04:
                return CreateBattle04EnemyGroup(visualTemplates);
            case TutorialBattleStageId.Battle05:
                return CreateMergedBattle05EnemyGroup(visualTemplates);
            case TutorialBattleStageId.Battle06:
                return CreateMergedBattle05EnemyGroup(visualTemplates);
            case TutorialBattleStageId.Battle07:
                return CreateBattle07EnemyGroup(visualTemplates, burnStatus);
            default:
                return null;
        }
    }

    private static TutorialBattleDefinition_SO CreateBattle01Definition()
    {
        TutorialBattleDefinition_SO definition = CreateDefinition("tutorial_battle_01_speed_and_clash", "战斗教程 01：速度、读招与拼点");
        definition.Steps.Add(Step(
            "battle01_read_enemy",
            0,
            "先看两颗敌方速度按钮",
            "这一战先学最核心的开局习惯：每回合开始时先读敌人的速度与技能，再决定你的线要接到哪里。\n\n速度不只是“谁先动”，它还会改变你应该优先处理哪一个目标。场上同时出现多颗敌方速度按钮时，通常先看更快、也会更早结算的那一颗。",
            TutorialBattleEventType.EnemySkillViewed,
            TutorialBattleInputPolicy.RequireBeforeCombat,
            "先点开敌方速度按钮，确认两条行动分别是什么。"));
        definition.Steps.Add(Step(
            "battle01_draw_line",
            10,
            "把线接到你想优先处理的那颗按钮上",
            "请主动从我方速度按钮连到敌方的一颗速度按钮。教程想让你意识到：拼点对象不是自动决定的，而是由你接线时自己选出来的。",
            TutorialBattleEventType.LineCreated,
            TutorialBattleInputPolicy.RequireBeforeCombat,
            "先把我方速度按钮连到你想处理的敌方按钮。"));
        definition.Steps.Add(Step(
            "battle01_start_round",
            20,
            "开始这一回合",
            "开战后注意两件事：被你接住的那条线会进入正面对撞，没接住的那条线会照常结算。速度越快的动作，越经常值得你优先去接。",
            TutorialBattleEventType.CombatExecutePressed,
            TutorialBattleInputPolicy.RequireBeforeCombat,
            "先完成读招和连线，再开始回合。"));
        definition.Reactions.Add(Reaction(
            "battle01_clash_explained",
            0,
            TutorialBattleEventType.ClashResolved,
            "这就是拼点",
            "只有双方行动真的对上时，才会进入拼点。以后看到多颗敌方速度按钮时，先判断哪一条更快、更危险，再决定让谁去接。"));
        definition.Reactions.Add(Reaction(
            "battle01_enemy_single_explained",
            10,
            TutorialBattleEventType.EnemyOneSidedAttackResolved,
            "没接住的线会变成敌方单方面攻击",
            "你没有接住的敌方行动，不会自动消失，而是会直接结算。速度会影响你最该先接哪一条线，这就是它在实战里的价值。"));
        definition.Reactions.Add(Reaction(
            "battle01_victory",
            20,
            TutorialBattleEventType.BattleWon,
            "第一战结束",
            "你已经完成了读招、看速度、自己选择拼点对象这三个基础动作。后面会继续教你什么时候该接，什么时候可以放。"));
        return definition;
    }

    private static TutorialBattleDefinition_SO CreateBattle02Definition()
    {
        TutorialBattleDefinition_SO definition = CreateDefinition("tutorial_battle_02_one_sided", "战斗教程 02：单方面攻击与取舍");
        definition.Steps.Add(Step(
            "battle02_read_enemy",
            0,
            "先看清两条敌方行动",
            "这一战开始练“取舍”。敌人会给出一条更危险的线和一条相对温和的线，你不需要每条都硬接，而是要先看清谁更值得处理。",
            TutorialBattleEventType.EnemySkillViewed,
            TutorialBattleInputPolicy.RequireBeforeCombat,
            "先查看敌人的技能说明，再决定这回合怎么分配线。"));
        definition.Steps.Add(Step(
            "battle02_start_round",
            10,
            "保住关键线，再开始回合",
            "这一回合不要求你把所有线都接满。重点是练习判断：危险的线优先处理，收益较低的线有时可以放掉。",
            TutorialBattleEventType.CombatExecutePressed,
            TutorialBattleInputPolicy.RequireBeforeCombat,
            "先看清敌方两条行动，再开始回合。"));
        definition.Reactions.Add(Reaction(
            "battle02_ally_single",
            0,
            TutorialBattleEventType.AllyOneSidedAttackResolved,
            "我方也能打出单方面攻击",
            "当你的行动没有被敌人拦住时，就会形成我方单方面攻击。以后看到适合打空线的技能，可以把这种机会留给它。"));
        definition.Reactions.Add(Reaction(
            "battle02_enemy_single",
            10,
            TutorialBattleEventType.EnemyOneSidedAttackResolved,
            "敌方单方面攻击意味着你放掉了一条线",
            "单方面攻击并不一定要完全避免，但你要知道自己为什么放掉它。教程后面会教你看技能词条，判断哪些线绝对不能放。"));
        definition.Reactions.Add(Reaction(
            "battle02_victory",
            20,
            TutorialBattleEventType.BattleWon,
            "第二战结束",
            "现在你已经知道：不是所有线都必须接，关键是先看速度和威胁，再决定资源给谁。"));
        return definition;
    }

    private static TutorialBattleDefinition_SO CreateBattle03Definition()
    {
        TutorialBattleDefinition_SO definition = CreateDefinition("tutorial_battle_03_skill_and_energy", "战斗教程 03：技能切换与能量");
        definition.Steps.Add(Step(
            "battle03_view_ally_skill",
            0,
            "先查看我方技能",
            "从这一战开始，技能选择本身就是决策的一部分。先看清楚当前有哪些技能、它们分别消耗多少能量、适合用来做什么。",
            TutorialBattleEventType.AllySkillViewed,
            TutorialBattleInputPolicy.RequireBeforeCombat,
            "先点开我方技能说明。"));
        definition.Steps.Add(Step(
            "battle03_switch_skill",
            10,
            "先切换一次技能",
            "请主动切一次技能。教程想强调的是：连线之前就要先决定你打算用哪一招，因为强力技能通常会占用更多能量。",
            TutorialBattleEventType.SkillSwitched,
            TutorialBattleInputPolicy.RequireBeforeCombat,
            "先切换一次技能，再继续操作。"));
        definition.Steps.Add(Step(
            "battle03_reserve_energy",
            20,
            "用消耗能量的技能完成连线",
            "当你用有能耗的技能去接线时，能量会在连线阶段先被预留，而不是等结算时才突然扣掉。能量不够时，就不能随便把高费技能到处接出去。",
            TutorialBattleEventType.EnergyLineCreated,
            TutorialBattleInputPolicy.RequireBeforeCombat,
            "请用一招会消耗能量的技能完成连线。"));
        definition.Reactions.Add(Reaction(
            "battle03_victory",
            0,
            TutorialBattleEventType.BattleWon,
            "第三战结束",
            "从现在开始，你做的每一次接线，都要同时考虑速度、威胁、技能类型和能量。"));
        return definition;
    }

    private static TutorialBattleDefinition_SO CreateBattle04Definition()
    {
        TutorialBattleDefinition_SO definition = CreateDefinition("tutorial_battle_04_modifier_intro", "战斗教程 04：三种威力修正何时生效");
        definition.Steps.Add(Step(
            "battle04_view_ally_keywords",
            0,
            "先看技能说明里的三种触发时机",
            "这一战开始正式讲三个最重要的关键词：\n\n[使用时]：只要这招开始结算就会生效。\n[拼点时]：只有真正进入拼点时才会生效。\n[单方面攻击时]：只有没有被拦住、直接打出去时才会生效。\n\n先点开我方技能说明，把三种写法都看一遍。",
            TutorialBattleEventType.AllySkillViewed,
            TutorialBattleInputPolicy.RequireBeforeCombat,
            "先查看我方技能说明，分清三种威力修正。"));
        definition.Steps.Add(Step(
            "battle04_switch_for_plan",
            10,
            "按你的计划切技能",
            "如果你准备拿去拼点，就优先想 [拼点时]；如果你准备打空线，就优先想 [单方面攻击时]；如果局势不稳定，[使用时] 往往最稳。",
            TutorialBattleEventType.SkillSwitched,
            TutorialBattleInputPolicy.RequireBeforeCombat,
            "先切一次技能，再决定这回合准备怎么打。"));
        definition.Steps.Add(Step(
            "battle04_start_round",
            20,
            "开始回合并对照结果",
            "开战后请对照实际发生的结算去理解词条：哪条线进了拼点，哪条线变成了单方面攻击。只有把“词条写的时机”和“这一条线真正发生的结算”对上，才算读懂。",
            TutorialBattleEventType.CombatExecutePressed,
            TutorialBattleInputPolicy.RequireBeforeCombat,
            "先看清技能词条并切好技能，再开始回合。"));
        definition.Reactions.Add(Reaction(
            "battle04_clash",
            0,
            TutorialBattleEventType.ClashResolved,
            "拼点时的威力修正只作用于拼点",
            "这是最容易混淆的一点：[拼点时] 只会在真正进入拼点时生效。如果同一招最后打成了单方面攻击，这一条威力修正不会自动跟过去。"));
        definition.Reactions.Add(Reaction(
            "battle04_ally_single",
            10,
            TutorialBattleEventType.AllyOneSidedAttackResolved,
            "打空线时吃到的是另一套加成",
            "当你的行动没有被拦住时，会稳定吃到 [使用时]，并且只有写着 [单方面攻击时] 的那部分才会额外生效。拿错技能去打空线，收益会明显变差。"));
        definition.Reactions.Add(Reaction(
            "battle04_victory",
            20,
            TutorialBattleEventType.BattleWon,
            "第四战结束",
            "现在你已经知道三种威力修正分别在哪个结算里生效。接下来会用更明确的敌方脚本让你反复练这个判断。"));
        return definition;
    }

    private static TutorialBattleDefinition_SO CreateBattle05Definition()
    {
        TutorialBattleDefinition_SO mergedDefinition = CreateDefinition("tutorial_battle_05_merged_planning", "战斗教程 05：综合判断与路线规划");
        mergedDefinition.Steps.Add(Step(
            "battle05_read_enemy",
            0,
            "先把敌方两条行动完整看完",
            "这一战把原来的第五战和第六战合成一场。你要一次性同时判断三件事：哪条线更快、哪条线更危险、你手里的技能更适合拿去拼点还是拿去打空线。\n\n先观察敌方两条行动，再开始规划。",
            TutorialBattleEventType.EnemySkillViewed,
            TutorialBattleInputPolicy.RequireBeforeCombat,
            "先查看敌方技能说明，再决定这一回合怎么接线。"));
        mergedDefinition.Steps.Add(Step(
            "battle05_switch_skill",
            10,
            "先切到你准备执行计划的技能",
            "请先切换一次技能，再开始回合。\n\n如果你准备去正面拦截高优先级目标，就优先考虑写着 [拼点时] 的技能；如果你准备留下空线打单方面攻击，就优先考虑写着 [单方面攻击时] 的技能；如果你还不确定局势，写着 [使用时] 的技能通常更稳。",
            TutorialBattleEventType.SkillSwitched,
            TutorialBattleInputPolicy.RequireBeforeCombat,
            "先切到适合当前目标的技能。"));
        mergedDefinition.Steps.Add(Step(
            "battle05_start_round",
            20,
            "开始回合并核对你的判断是否正确",
            "开战后请对照实际结算去复盘：你优先处理的那条线是不是因为更快或更危险？你切过去的技能是不是吃到了自己真正需要的那一段威力修正？\n\n这一战的目标不是单纯打赢，而是把“速度、能量、词条时机”放进同一套判断里。",
            TutorialBattleEventType.CombatExecutePressed,
            TutorialBattleInputPolicy.RequireBeforeCombat,
            "先完成读招和切技能，再开始回合。"));
        mergedDefinition.Reactions.Add(Reaction(
            "battle05_clash",
            0,
            TutorialBattleEventType.ClashResolved,
            "这条线证明你是在正面处理威胁",
            "当你把角色派去拼点时，通常是在用行动额度换取更稳定的拦截效果。写着 [拼点时] 的技能只有在这里才会完整发挥。\n\n如果一条更快的敌方行动会改变你的拼点对象，那它往往就该被优先考虑。"));
        mergedDefinition.Reactions.Add(Reaction(
            "battle05_ally_single",
            10,
            TutorialBattleEventType.AllyOneSidedAttackResolved,
            "打空线时要确认你是不是故意这样做的",
            "如果你把一招留给空线，就要确保它真的更适合 [单方面攻击时] 的结算。否则你虽然成功出手了，但最关键的加成可能根本没有吃到。"));
        mergedDefinition.Reactions.Add(Reaction(
            "battle05_enemy_single",
            20,
            TutorialBattleEventType.EnemyOneSidedAttackResolved,
            "被放掉的线，要知道自己为什么放",
            "如果你放掉的是更快、更危险，或者写着 [单方面攻击时] 的敌方行动，那代价通常会很高。以后每次决定放线，都先确认它到底依赖哪种结算时机。"));
        mergedDefinition.Reactions.Add(Reaction(
            "battle05_victory",
            30,
            TutorialBattleEventType.BattleWon,
            "第五战结束",
            "现在你已经能把速度、拼点对象、技能能量和威力修正放在一起判断。原本分成两场讲的内容，已经在这一战里合并练完了。"));
        return mergedDefinition;
#if false

        TutorialBattleDefinition_SO definition = CreateDefinition("tutorial_battle_05_modifier_practice", "战斗教程 05：看词条决定拼哪条线");
        definition.Steps.Add(Step(
            "battle05_read_enemy",
            0,
            "先看敌方词条和速度",
            "这一战不再只是认识关键词，而是要靠它做决定。先看敌方两条线：哪一条更适合用拼点技能去接，哪一条放掉以后代价更高。",
            TutorialBattleEventType.EnemySkillViewed,
            TutorialBattleInputPolicy.RequireBeforeCombat,
            "先查看敌方技能说明，再决定这回合的连线方案。"));
        definition.Steps.Add(Step(
            "battle05_switch_skill",
            10,
            "切成适合当前目标的技能",
            "请先切技能，再开始回合。你要根据计划好的目标来选招：准备对撞，就拿拼点技能；准备收空线，就拿单方面攻击技能。",
            TutorialBattleEventType.SkillSwitched,
            TutorialBattleInputPolicy.RequireBeforeCombat,
            "先切到适合当前目标的技能。"));
        definition.Steps.Add(Step(
            "battle05_start_round",
            20,
            "开始回合并验证你的判断",
            "开战后观察自己拿到的加成是否符合预期。如果一招本来是写给拼点用的，却被你拿去打空线，那它最关键的威力修正就会浪费掉。",
            TutorialBattleEventType.CombatExecutePressed,
            TutorialBattleInputPolicy.RequireBeforeCombat,
            "先完成读招和切技能，再开始回合。"));
        definition.Reactions.Add(Reaction(
            "battle05_clash",
            0,
            TutorialBattleEventType.ClashResolved,
            "拿对技能去拼点",
            "拼点时最稳定会吃到 [使用时] 和 [拼点时]。因此看到高威胁正面对撞线时，优先找写着 [拼点时] 的技能去接。"));
        definition.Reactions.Add(Reaction(
            "battle05_enemy_single",
            10,
            TutorialBattleEventType.EnemyOneSidedAttackResolved,
            "放线前先看对方依赖哪种触发时机",
            "如果敌人的爆发写在 [单方面攻击时]，那这条线的放弃代价通常更高；如果主要收益写在 [拼点时]，放掉它反而可能更安全。"));
        definition.Reactions.Add(Reaction(
            "battle05_victory",
            20,
            TutorialBattleEventType.BattleWon,
            "第五战结束",
            "你现在已经不是单纯在接线，而是在根据词条和结算类型，主动给每一招找最合适的用途。"));
        return definition;
#endif
    }

    private static TutorialBattleDefinition_SO CreateBattle06Definition()
    {
        TutorialBattleDefinition_SO definition = CreateDefinition("tutorial_battle_06_planning", "战斗教程 06：速度、能量与词条一起判断");
        definition.Steps.Add(Step(
            "battle06_read_plan",
            0,
            "按完整顺序做一次回合规划",
            "这一战把前面的知识合在一起。开始前请按这个顺序判断：\n1. 先看哪条敌方行动更快、更先结算。\n2. 再看哪条线更危险。\n3. 然后判断自己手里的技能更适合拼点还是打空线。\n4. 最后确认能量够不够。",
            TutorialBattleEventType.EnemySkillViewed,
            TutorialBattleInputPolicy.RequireBeforeCombat,
            "先查看敌方技能说明，完成这一轮规划。"));
        definition.Steps.Add(Step(
            "battle06_switch_skill",
            10,
            "切技能后再开始回合",
            "请先切技能，再开始回合。你现在练的是完整流程：速度决定优先级，词条决定用途，能量决定你能不能真的把这招接出去。",
            TutorialBattleEventType.SkillSwitched,
            TutorialBattleInputPolicy.RequireBeforeCombat,
            "先切一次技能，再进入结算。"));
        definition.Steps.Add(Step(
            "battle06_start_round",
            20,
            "开始回合并检查你的规划是否成立",
            "进入结算后，看清楚你原本以为会发生的事有没有真的发生：该拼点的有没有拼到，该打空线的有没有成功打出去，能量有没有被安排对。",
            TutorialBattleEventType.CombatExecutePressed,
            TutorialBattleInputPolicy.RequireBeforeCombat,
            "先完成这一轮规划，再开始回合。"));
        definition.Reactions.Add(Reaction(
            "battle06_clash",
            0,
            TutorialBattleEventType.ClashResolved,
            "速度决定你优先保护哪条线",
            "如果两条线你都想处理，通常先接更快、更先结算的那一条。速度会直接改变你的拼点对象选择，这一点要和词条判断一起看。"));
        definition.Reactions.Add(Reaction(
            "battle06_ally_single",
            10,
            TutorialBattleEventType.AllyOneSidedAttackResolved,
            "单方面攻击是规划出来的，不是碰运气",
            "当你故意把适合打空线的技能留给未被拦截的目标时，这就是一次成功的规划。单方面攻击的收益，要靠你在连线前就想清楚。"));
        definition.Reactions.Add(Reaction(
            "battle06_victory",
            20,
            TutorialBattleEventType.BattleWon,
            "第六战结束",
            "最后一战不会再手把手要求动作，而是让你自己把速度、接线、能量和技能词条全部串起来。"));
        return definition;
    }

    private static TutorialBattleDefinition_SO CreateBattle07Definition()
    {
        TutorialBattleDefinition_SO definition = CreateDefinition("tutorial_battle_07_final", "战斗教程 07：综合演练");
        definition.Steps.Add(Step(
            "battle07_read_enemy",
            0,
            "先自己做一次完整判断",
            "这是最终演练。开始前请自己完成一次完整判断：\n1. 先读敌方速度和行动。\n2. 判断哪条线必须接，哪条线可以放。\n3. 给每一招找最合适的用途。\n4. 确认能量够不够。\n\n这一战里会顺带出现状态图标，但不再单独拆成一整课；你只需要像正式战斗那样读懂技能说明和图标即可。",
            TutorialBattleEventType.EnemySkillViewed,
            TutorialBattleInputPolicy.RequireBeforeCombat,
            "先查看敌方技能说明，再开始最终演练。"));
        definition.Steps.Add(Step(
            "battle07_start_round",
            10,
            "开始最终演练",
            "这一战不再逐步要求你完成每个小动作，把前六战学到的内容一起用出来即可。",
            TutorialBattleEventType.CombatExecutePressed,
            TutorialBattleInputPolicy.RequireBeforeCombat,
            "先完成判断，再开始最终演练。"));
        definition.Reactions.Add(Reaction(
            "battle07_status",
            0,
            TutorialBattleEventType.StatusApplied,
            "状态显示与正式战斗一致",
            "这里出现的状态会直接使用正式资源，所以图标、名称和描述都应与正式战斗一致。教程不再单独拆状态课，但你仍然可以按正式规则去读取它。"));
        definition.Reactions.Add(Reaction(
            "battle07_victory",
            10,
            TutorialBattleEventType.BattleWon,
            "战斗教程完成",
            "你已经完成了战斗系统教程。后续如果要继续改文案、调整敌人脚本或增删阶段，可以直接在这七战定义上迭代。"));
        return definition;
    }

    private static EnemyGroup_SO CreateBattle01EnemyGroup(TutorialEnemyVisualTemplateSet visualTemplates)
    {
        EnemySkill_SO enemyFast = CreateSkill("抢先压步", "高速起手，用来逼你先观察速度按钮。", 0, 2, 3);
        EnemySkill_SO enemySlow = CreateSkill("迟滞补击", "较慢但会正常结算，用来说明没接住的线依然会出手。", 0, 1, 2);

        EnemySkill_SO allyGuard = CreateSkill("稳步拦截", "稳定的基础技能，适合拿来接最优先的那条线。", 0, 2, 3);
        EnemySkill_SO allyProbe = CreateSkill("试探突刺", "较长的基础攻击，用来感受手动连线。", 0, 1, 4);

        EnemyData_SO enemy = CreateScriptedTutorialEnemy(
            "速度教官",
            12,
            5,
            7,
            2,
            new[] { enemyFast, enemySlow },
            new[] { allyGuard, allyProbe },
            ("Round 1 Slot 1", enemyFast, 1, 200, 1),
            ("Round 1 Slot 2", enemySlow, 1, 100, 1),
            ("Round 2 Slot 1", enemyFast, 2, 200, 1),
            ("Round 2 Slot 2", enemySlow, 2, 100, 1));

        enemy.enemyDescription = "总是同时给出快慢两条线，迫使玩家先看速度再决定接哪一条。";
        ApplyVisualTemplate(enemy, visualTemplates != null ? visualTemplates.openingPrimaryTemplate : null);
        return CreateGroup("TutorialBattleGroup_01", 9201, "教程战 01", "速度、读招与拼点", enemy);
    }

    private static EnemyGroup_SO CreateBattle02EnemyGroup(TutorialEnemyVisualTemplateSet visualTemplates)
    {
        EnemySkill_SO enemyDanger = CreateSkill("前压重击", "较危险的一条线，适合拿来练习优先处理。", 0, 2, 4);
        EnemySkill_SO enemyLight = CreateSkill("牵制试探", "威胁较低，可以作为取舍练习的对象。", 0, 1, 2);

        EnemySkill_SO allyStable = CreateSkill("观察切入", "稳妥的基础技能。", 0, 2, 3);
        EnemySkill_SO allySingle = CreateSkill("贴身追击", "较适合拿去打没有被拦住的目标。", 0, 2, 4);

        EnemyData_SO enemy = CreateScriptedTutorialEnemy(
            "取舍教官",
            16,
            5,
            6,
            2,
            new[] { enemyDanger, enemyLight },
            new[] { allyStable, allySingle },
            ("Round 1 Slot 1", enemyDanger, 1, 200, 1),
            ("Round 1 Slot 2", enemyLight, 1, 100, 1),
            ("Round 2 Slot 1", enemyLight, 2, 200, 1),
            ("Round 2 Slot 2", enemyDanger, 2, 100, 1));

        enemy.enemyDescription = "一条线更危险，一条线更温和，专门用来练习该接哪条、该放哪条。";
        ApplyVisualTemplate(enemy, visualTemplates != null ? visualTemplates.openingSecondaryTemplate : null);
        return CreateGroup("TutorialBattleGroup_02", 9202, "教程战 02", "单方面攻击与取舍", enemy);
    }

    private static EnemyGroup_SO CreateBattle03EnemyGroup(TutorialEnemyVisualTemplateSet visualTemplates)
    {
        EnemySkill_SO enemyClash = CreateSkill("逼迫对撞", "迫使玩家考虑切成更适合当前回合的技能。", 0, 2, 4);
        EnemySkill_SO enemyGap = CreateSkill("侧向压进", "提醒玩家并不是每条线都要用同一种技能去处理。", 0, 1, 3);

        EnemySkill_SO allyStable = CreateSkill("稳步挥击", "0 费基础技能。", 0, 2, 3);
        EnemySkill_SO allyClash = CreateSkill("前压拦截", "适合正面对撞的 0 费技能。", 0, 2, 4);
        EnemySkill_SO allyHeavy = CreateSkill("蓄力重斩", "高威力但需要 1 点能量。", 1, 3, 6);

        EnemyData_SO enemy = CreateScriptedTutorialEnemy(
            "能量教官",
            18,
            5,
            6,
            2,
            new[] { enemyClash, enemyGap },
            new[] { allyStable, allyClash, allyHeavy },
            ("Round 1 Slot 1", enemyClash, 1, 200, 1),
            ("Round 1 Slot 2", enemyGap, 1, 100, 1),
            ("Round 2 Slot 1", enemyGap, 2, 200, 1),
            ("Round 2 Slot 2", enemyClash, 2, 100, 1));

        enemy.enemyDescription = "会迫使玩家在连线前先决定技能与能量分配。";
        ApplyVisualTemplate(enemy, visualTemplates != null ? visualTemplates.openingSecondaryTemplate : null);
        return CreateGroup("TutorialBattleGroup_03", 9203, "教程战 03", "技能切换与能量", enemy);
    }

    private static EnemyGroup_SO CreateBattle04EnemyGroup(TutorialEnemyVisualTemplateSet visualTemplates)
    {
        EnemySkill_SO enemyClash = CreateClashSkill("正面拆招", "这条线更适合拿 [拼点时] 技能去接。", 0, 2, 4, 2);
        EnemySkill_SO enemySingle = CreateSingleAttackSkill("越位追击", "放掉后会以单方面攻击结算。", 0, 2, 4, 2);

        EnemySkill_SO allyUse = CreateSkill("稳态斩击", "写着 [使用时] 的保底技能。", 0, 2, 4);
        SetPowerModifiers(allyUse, CreateFlatPowerModifier(EnemySkill_SO.DiceEffectTrigger.OnSkillUse, 1, "使用时 +1"));

        EnemySkill_SO allyClash = CreateClashSkill("迎击斩", "写着 [拼点时]，更适合拿去对撞。", 0, 2, 4, 3);
        EnemySkill_SO allySingle = CreateSingleAttackSkill("追身斩", "写着 [单方面攻击时]，更适合拿去打空线。", 1, 2, 4, 3);

        EnemyData_SO enemy = CreateScriptedTutorialEnemy(
            "词条教官",
            18,
            5,
            6,
            2,
            new[] { enemyClash, enemySingle },
            new[] { allyUse, allyClash, allySingle },
            ("Round 1 Slot 1", enemyClash, 1, 200, 1),
            ("Round 1 Slot 2", enemySingle, 1, 100, 1),
            ("Round 2 Slot 1", enemySingle, 2, 200, 1),
            ("Round 2 Slot 2", enemyClash, 2, 100, 1));

        enemy.enemyDescription = "专门用来拆开三种威力修正何时生效。";
        ApplyVisualTemplate(enemy, visualTemplates != null ? visualTemplates.eliteTemplate : null);
        return CreateGroup("TutorialBattleGroup_04", 9204, "教程战 04", "三种威力修正何时生效", enemy);
    }

    private static EnemyGroup_SO CreateBattle05EnemyGroup(TutorialEnemyVisualTemplateSet visualTemplates)
    {
        EnemySkill_SO enemyClash = CreateClashSkill("压线对撞", "拼点时更强，逼你尽快找对撞技能去接。", 0, 2, 4, 2);
        EnemySkill_SO enemySingle = CreateSingleAttackSkill("后手追杀", "如果放掉，会在单方面攻击时更危险。", 0, 2, 4, 3);
        EnemySkill_SO enemyUse = CreateSkill("稳步横扫", "无论哪种结算都会吃到 [使用时] 的稳定收益。", 0, 2, 4);
        SetPowerModifiers(enemyUse, CreateFlatPowerModifier(EnemySkill_SO.DiceEffectTrigger.OnSkillUse, 1, "使用时 +1"));

        EnemySkill_SO allyUse = CreateSkill("稳态推进", "写着 [使用时]，适合保底。", 0, 2, 4);
        SetPowerModifiers(allyUse, CreateFlatPowerModifier(EnemySkill_SO.DiceEffectTrigger.OnSkillUse, 1, "使用时 +1"));

        EnemySkill_SO allyClash = CreateClashSkill("拆招重击", "写着 [拼点时]，适合处理正面对撞线。", 0, 2, 4, 3);
        EnemySkill_SO allySingle = CreateSingleAttackSkill("穿插追击", "写着 [单方面攻击时]，适合留给空线。", 1, 2, 4, 3);

        EnemyData_SO enemy = CreateScriptedTutorialEnemy(
            "修正考官",
            20,
            5,
            6,
            2,
            new[] { enemyClash, enemySingle, enemyUse },
            new[] { allyUse, allyClash, allySingle },
            ("Round 1 Slot 1", enemyClash, 1, 200, 1),
            ("Round 1 Slot 2", enemySingle, 1, 100, 1),
            ("Round 2 Slot 1", enemyUse, 2, 200, 1),
            ("Round 2 Slot 2", enemySingle, 2, 100, 1),
            ("Round 3 Slot 1", enemyClash, 3, 200, 1),
            ("Round 3 Slot 2", enemyUse, 3, 100, 1));

        enemy.enemyDescription = "固定顺序演示该拼点、该放线和该保底的不同判断。";
        ApplyVisualTemplate(enemy, visualTemplates != null ? visualTemplates.eliteTemplate : null);
        return CreateGroup("TutorialBattleGroup_05", 9205, "教程战 05", "看词条决定拼哪条线", enemy);
    }

    private static EnemyGroup_SO CreateBattle06EnemyGroup(TutorialEnemyVisualTemplateSet visualTemplates)
    {
        EnemySkill_SO enemyFast = CreateClashSkill("抢先压制", "更快的一条线，适合用来练“速度改变拼点对象”。", 0, 2, 4, 2);
        EnemySkill_SO enemyGap = CreateSingleAttackSkill("空档追咬", "放掉时会以单方面攻击结算。", 0, 2, 4, 2);
        EnemySkill_SO enemyStable = CreateSkill("稳步推进", "无论哪种结算都会稳定吃到 [使用时]。", 0, 2, 4);
        SetPowerModifiers(enemyStable, CreateFlatPowerModifier(EnemySkill_SO.DiceEffectTrigger.OnSkillUse, 1, "使用时 +1"));

        EnemySkill_SO allyUse = CreateSkill("稳态斜切", "更适合局势不确定时保底。", 0, 2, 4);
        SetPowerModifiers(allyUse, CreateFlatPowerModifier(EnemySkill_SO.DiceEffectTrigger.OnSkillUse, 1, "使用时 +1"));

        EnemySkill_SO allyClash = CreateClashSkill("迎锋拦截", "适合去接高优先级拼点目标。", 0, 2, 4, 3);
        EnemySkill_SO allySingle = CreateSingleAttackSkill("穿位追斩", "适合保留给空线。", 1, 2, 4, 3);

        EnemyData_SO enemy = CreateScriptedTutorialEnemy(
            "规划考官",
            20,
            5,
            7,
            2,
            new[] { enemyFast, enemyGap, enemyStable },
            new[] { allyUse, allyClash, allySingle },
            ("Round 1 Slot 1", enemyFast, 1, 200, 1),
            ("Round 1 Slot 2", enemyGap, 1, 100, 1),
            ("Round 2 Slot 1", enemyStable, 2, 200, 1),
            ("Round 2 Slot 2", enemyGap, 2, 100, 1),
            ("Round 3 Slot 1", enemyFast, 3, 200, 1),
            ("Round 3 Slot 2", enemyStable, 3, 100, 1));

        enemy.enemyDescription = "把速度、能量和技能词条放到同一个判断里。";
        ApplyVisualTemplate(enemy, visualTemplates != null ? visualTemplates.eliteTemplate : null);
        return CreateGroup("TutorialBattleGroup_06", 9206, "教程战 06", "速度、能量与词条一起判断", enemy);
    }

    private static EnemyGroup_SO CreateBattle07EnemyGroup(TutorialEnemyVisualTemplateSet visualTemplates, BattleStatus_SO burnStatus)
    {
        EnemySkill_SO enemyBurn = CreateSkill("烙印火痕", "命中后施加 1 层燃烧。", 0, 2, 3);
        SetStatusEffects(enemyBurn, CreateStatusEffect(
            EnemySkill_SO.DiceEffectTrigger.OnHit,
            EnemySkill_SO.DamageTargetMode.CurrentTarget,
            burnStatus,
            EnemySkill_SO.DiceStatusType.Burn,
            1,
            "最终战燃烧"));

        EnemySkill_SO enemyClash = CreateClashSkill("正面断招", "拼点时更强。", 0, 2, 4, 3);
        EnemySkill_SO enemySingle = CreateSingleAttackSkill("裂隙追击", "放掉后会在单方面攻击时更危险。", 0, 2, 4, 3);

        EnemySkill_SO allyUse = CreateSkill("稳态推进", "适合保底。", 0, 2, 4);
        SetPowerModifiers(allyUse, CreateFlatPowerModifier(EnemySkill_SO.DiceEffectTrigger.OnSkillUse, 1, "使用时 +1"));

        EnemySkill_SO allyClash = CreateClashSkill("拆招重压", "适合接正面对撞线。", 0, 2, 4, 3);
        EnemySkill_SO allySingle = CreateSingleAttackSkill("穿刺追击", "适合处理空线。", 1, 2, 4, 3);
        EnemySkill_SO allyBurn = CreateSkill("火种压制", "命中后施加 1 层燃烧。", 0, 1, 3);
        SetStatusEffects(allyBurn, CreateStatusEffect(
            EnemySkill_SO.DiceEffectTrigger.OnHit,
            EnemySkill_SO.DamageTargetMode.CurrentTarget,
            burnStatus,
            EnemySkill_SO.DiceStatusType.Burn,
            1,
            "最终战燃烧"));

        EnemyData_SO enemy = CreateScriptedTutorialEnemy(
            "教程终端",
            24,
            6,
            7,
            2,
            new[] { enemyBurn, enemyClash, enemySingle },
            new[] { allyUse, allyClash, allySingle, allyBurn },
            ("Round 1 Slot 1", enemyBurn, 1, 200, 1),
            ("Round 1 Slot 2", enemyClash, 1, 100, 1),
            ("Round 2 Slot 1", enemySingle, 2, 200, 1),
            ("Round 2 Slot 2", enemyBurn, 2, 100, 1),
            ("Round 3 Slot 1", enemyClash, 3, 200, 1),
            ("Round 3 Slot 2", enemySingle, 3, 100, 1));

        enemy.enemyDescription = "综合使用速度、拼点、单方面攻击与状态图标的最终演练敌人。";
        ApplyVisualTemplate(enemy, visualTemplates != null ? visualTemplates.bossTemplate : null);
        return CreateGroup("TutorialBattleGroup_07", 9207, "教程战 07", "综合演练", enemy);
    }

    private static TutorialBattleDefinition_SO CreateMergedBattle05Definition()
    {
        TutorialBattleDefinition_SO definition = CreateDefinition("tutorial_battle_05_merged_planning", "战斗教程 05：综合判断与路线规划");
        definition.Steps.Add(Step(
            "battle05_read_enemy",
            0,
            "先把敌方两条行动完整看完",
            "这一战把原来的第五战和第六战合成一场。你要同时判断三件事：哪条线更快，哪条线更危险，你手里的技能更适合拿去拼点还是拿去打空线。\n\n先看清敌方两条行动，再开始规划。",
            TutorialBattleEventType.EnemySkillViewed,
            TutorialBattleInputPolicy.RequireBeforeCombat,
            "先查看敌方技能说明，再决定这一回合怎么接线。"));
        definition.Steps.Add(Step(
            "battle05_switch_skill",
            10,
            "先切到适合当前计划的技能",
            "请先切换一次技能，再开始回合。\n\n如果你准备正面拦截高优先级目标，就优先考虑写着 [拼点时] 的技能；如果你准备把一招留给空线，就优先考虑写着 [单方面攻击时] 的技能；如果局势还不稳定，写着 [使用时] 的技能通常更稳。",
            TutorialBattleEventType.SkillSwitched,
            TutorialBattleInputPolicy.RequireBeforeCombat,
            "先切到适合当前目标的技能。"));
        definition.Steps.Add(Step(
            "battle05_start_round",
            20,
            "开始回合并核对你的判断是否正确",
            "开战后请对照实际结算去复盘：你优先处理的那条线是不是因为更快或更危险？你切过去的技能是不是吃到了自己真正需要的那一段威力修正？\n\n这一战的目标不是单纯打赢，而是把“速度、能量、词条时机”放进同一套判断里。",
            TutorialBattleEventType.CombatExecutePressed,
            TutorialBattleInputPolicy.RequireBeforeCombat,
            "先完成读招和切技能，再开始回合。"));
        definition.Reactions.Add(Reaction(
            "battle05_clash",
            0,
            TutorialBattleEventType.ClashResolved,
            "更快的敌方行动会改变你该拼谁",
            "当你把角色派去拼点时，通常是在用行动额度换更稳定的拦截效果。写着 [拼点时] 的技能只有在这里才会完整发挥。\n\n如果一条更快的敌方行动会改变你的拼点对象，那它往往就该被优先考虑。"));
        definition.Reactions.Add(Reaction(
            "battle05_ally_single",
            10,
            TutorialBattleEventType.AllyOneSidedAttackResolved,
            "打空线时要确认这是你故意这样做的",
            "如果你把一招留给空线，就要确保它真的更适合 [单方面攻击时] 的结算。否则你虽然成功出手了，但最关键的加成可能根本没有吃到。"));
        definition.Reactions.Add(Reaction(
            "battle05_enemy_single",
            20,
            TutorialBattleEventType.EnemyOneSidedAttackResolved,
            "被放掉的线，要知道自己为什么放",
            "如果你放掉的是更快、更危险，或者写着 [单方面攻击时] 的敌方行动，那代价通常会很高。以后每次决定放线，都先确认它到底依赖哪种结算时机。"));
        definition.Reactions.Add(Reaction(
            "battle05_victory",
            30,
            TutorialBattleEventType.BattleWon,
            "第五战结束",
            "现在你已经能把速度、拼点对象、技能能量和威力修正放在一起判断。原本拆成两战讲的内容，已经在这一战里合并练完了。"));
        return definition;
    }

    private static EnemyGroup_SO CreateMergedBattle05EnemyGroup(TutorialEnemyVisualTemplateSet visualTemplates)
    {
        EnemySkill_SO enemyFastClash = CreateClashSkill("抢先压制", "更快的正面对撞线，会迫使你优先决定拼点对象。", 0, 2, 4, 2);
        EnemySkill_SO enemyGapPunish = CreateSingleAttackSkill("空档追咬", "如果被放掉，会在单方面攻击时变得更危险。", 0, 2, 4, 2);
        EnemySkill_SO enemyStableUse = CreateSkill("稳步推进", "无论哪种结算都会稳定吃到 [使用时] 的收益。", 0, 2, 4);
        SetPowerModifiers(enemyStableUse, CreateFlatPowerModifier(EnemySkill_SO.DiceEffectTrigger.OnSkillUse, 1, "使用时 +1"));

        EnemySkill_SO allyUse = CreateSkill("稳态斜切", "更适合局势还不确定时保底。", 0, 2, 4);
        SetPowerModifiers(allyUse, CreateFlatPowerModifier(EnemySkill_SO.DiceEffectTrigger.OnSkillUse, 1, "使用时 +1"));

        EnemySkill_SO allyClash = CreateClashSkill("迎锋拦截", "更适合拿去正面拼点，处理高优先级目标。", 0, 2, 4, 3);
        EnemySkill_SO allySingle = CreateSingleAttackSkill("穿位追斩", "更适合故意留下来打空线。", 1, 2, 4, 3);

        EnemyData_SO enemy = CreateScriptedTutorialEnemy(
            "综合考官",
            20,
            5,
            7,
            2,
            new[] { enemyFastClash, enemyGapPunish, enemyStableUse },
            new[] { allyUse, allyClash, allySingle },
            ("Round 1 Slot 1", enemyFastClash, 1, 200, 1),
            ("Round 1 Slot 2", enemyGapPunish, 1, 100, 1),
            ("Round 2 Slot 1", enemyStableUse, 2, 200, 1),
            ("Round 2 Slot 2", enemyGapPunish, 2, 100, 1),
            ("Round 3 Slot 1", enemyFastClash, 3, 200, 1),
            ("Round 3 Slot 2", enemyStableUse, 3, 100, 1));

        enemy.enemyDescription = "固定顺序演示速度优先级、拼点对象切换与空线取舍。";
        ApplyVisualTemplate(enemy, visualTemplates != null ? visualTemplates.eliteTemplate : null);
        return CreateGroup("TutorialBattleGroup_05", 9205, "教程战 05", "综合练习速度、词条时机与路线规划", enemy);
    }

    private static TutorialBattleDefinition_SO CreateDefinition(string id, string displayName)
    {
        TutorialBattleDefinition_SO definition = ScriptableObject.CreateInstance<TutorialBattleDefinition_SO>();
        PrepareRuntimeObject(definition, $"Definition_{id}");
        definition.tutorialBattleId = id;
        definition.displayName = displayName;
        return definition;
    }

    private static EnemyGroup_SO CreateGroup(string runtimeName, int groupId, string groupName, string description, params EnemyData_SO[] enemies)
    {
        EnemyGroup_SO group = ScriptableObject.CreateInstance<EnemyGroup_SO>();
        PrepareRuntimeObject(group, runtimeName);
        group.groupID = groupId;
        group.groupName = groupName;
        group.description = description;
        group.enemies = new List<EnemyData_SO>(enemies);
        return group;
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

    private static EnemySkill_SO CreateClashSkill(string name, string description, int energyCost, int minPower, int maxPower, int powerBonus)
    {
        EnemySkill_SO skill = CreateSkill(name, description, energyCost, minPower, maxPower);
        SetPowerModifiers(skill, CreateFlatPowerModifier(EnemySkill_SO.DiceEffectTrigger.OnClash, powerBonus, "拼点时加成"));
        return skill;
    }

    private static EnemySkill_SO CreateSingleAttackSkill(string name, string description, int energyCost, int minPower, int maxPower, int powerBonus)
    {
        EnemySkill_SO skill = CreateSkill(name, description, energyCost, minPower, maxPower);
        SetPowerModifiers(skill, CreateFlatPowerModifier(EnemySkill_SO.DiceEffectTrigger.OnSingleAttack, powerBonus, "单方面攻击时加成"));
        return skill;
    }

    private static EnemyData_SO CreateScriptedTutorialEnemy(
        string name,
        int hp,
        int minSpeed,
        int maxSpeed,
        int speedButtonCount,
        IEnumerable<EnemySkill_SO> enemySkills,
        IEnumerable<EnemySkill_SO> allyGrantedSkills,
        params (string ruleName, EnemySkill_SO skill, int round, int priority, int maxSlots)[] rules)
    {
        EnemyData_SO enemy = CreateEnemy(name, hp, minSpeed, maxSpeed, speedButtonCount, enemySkills, allyGrantedSkills);
        if (rules != null)
        {
            for (int i = 0; i < rules.Length; i++)
            {
                (string ruleName, EnemySkill_SO skill, int round, int priority, int maxSlots) rule = rules[i];
                AddRoundSequenceRule(enemy, rule.ruleName, rule.skill, rule.round, rule.priority, rule.maxSlots);
            }
        }

        return enemy;
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
                if (skill != null)
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

    private static EnemySkill_SO.StatusEffectEntry CreateStatusEffect(
        EnemySkill_SO.DiceEffectTrigger trigger,
        EnemySkill_SO.DamageTargetMode targetMode,
        BattleStatus_SO statusData,
        EnemySkill_SO.DiceStatusType fallbackStatusType,
        int stackAmount,
        string effectName)
    {
        return new EnemySkill_SO.StatusEffectEntry
        {
            effectName = effectName,
            trigger = trigger,
            targetMode = targetMode,
            statusData = statusData,
            statusType = fallbackStatusType,
            applyTiming = EnemySkill_SO.StatusApplyTiming.Immediate,
            stackAmount = Mathf.Max(1, stackAmount),
            randomTargetCount = 1
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
