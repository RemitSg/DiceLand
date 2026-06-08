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
        TutorialBattleDefinition_SO definition = CreateDefinition("tutorial_battle_01_first_combat", "战斗教程 01：敌人、速度与拼点");
        definition.Steps.Add(Step(
            "battle01_intro_enemy_detail",
            0,
            "第一场战斗",
            "接下来是激动人心的战斗内容！你将学会如何与敌人战斗。\n\n先点击敌人的本体，查看当前敌人的基础信息。",
            TutorialBattleEventType.EnemyDetailViewed,
            TutorialBattleInputPolicy.RequireBeforeCombat,
            "请先点击敌人的本体，查看敌人详情。"));
        definition.Steps.Add(Step(
            "battle01_press_prepare",
            10,
            "准备开始战斗",
            "你已经了解了当前敌人的基础信息，接下来准备开始战斗。\n\n点击屏幕下方的准备按钮，让双方生成本回合的速度按钮。",
            TutorialBattleEventType.CombatExecutePressed,
            TutorialBattleInputPolicy.RequireBeforeCombat,
            "请先点击准备按钮，进入本回合的目标指定阶段。",
            TutorialBattleHighlightTarget.BattleButton));
        definition.Steps.Add(Step(
            "battle01_read_enemy_skill",
            20,
            "读取敌人的行动",
            "敌人头顶的六边形按钮就是它的速度按钮，按钮上方的数字代表速度。\n\n速度按钮也可以理解为技能槽：速度按钮越多，敌人一回合能做的事就越多。现在点击敌人的速度按钮，查看它准备用什么技能。",
            TutorialBattleEventType.EnemySkillViewed,
            TutorialBattleInputPolicy.RequireBeforeCombat,
            "请点击敌人的速度按钮，查看敌人技能。",
            TutorialBattleHighlightTarget.FirstEnemySpeedButton));
        definition.Steps.Add(Step(
            "battle01_start_enemy_attack",
            30,
            "先看一次敌人的攻击",
            "敌人的技能由若干颗骰子组成。攻击骰子会造成伤害，防御骰子会抵挡即将到来的伤害。\n\n“1 - 2” 这样的数值范围叫做威力范围，代表结算时会在 1 和 2 之间随机。现在关闭技能面板，再点击准备按钮。",
            TutorialBattleEventType.CombatExecutePressed,
            TutorialBattleInputPolicy.RequireBeforeCombat,
            "请先查看敌人速度按钮上的技能，再点击准备按钮开始结算。"));
        definition.Steps.Add(Step(
            "battle01_prepare_counter",
            40,
            "准备反击",
            "敌人的攻击已经结束，下一回合轮到我们主动处理它。\n\n再次点击准备按钮，生成新一回合的速度按钮。",
            TutorialBattleEventType.CombatExecutePressed,
            TutorialBattleInputPolicy.RequireBeforeCombat,
            "请先点击准备按钮，生成新一回合的速度按钮。",
            TutorialBattleHighlightTarget.BattleButton));
        definition.Steps.Add(Step(
            "battle01_view_ally_skill",
            50,
            "轮到我们反击",
            "我们不能这样坐以待毙！展开有效反击来化解敌人的攻击。\n\n点击友方角色的速度按钮，查看我们有什么技能可以使用。",
            TutorialBattleEventType.AllySkillViewed,
            TutorialBattleInputPolicy.RequireBeforeCombat,
            "请点击友方角色的速度按钮，查看友方技能。",
            TutorialBattleHighlightTarget.FirstAllySpeedButton));
        definition.Steps.Add(Step(
            "battle01_draw_clash_line",
            60,
            "指定攻击目标",
            "左侧列出了该名友方角色拥有的技能，右侧面板会显示当前技能的详细信息。\n\n选择并了解技能后，关闭技能面板。随后拖拽友方角色的速度按钮，把目标指定到敌人的速度按钮上。",
            TutorialBattleEventType.LineCreated,
            TutorialBattleInputPolicy.RequireBeforeCombat,
            "请拖拽友方速度按钮，连到敌人的速度按钮。"));
        definition.Steps.Add(Step(
            "battle01_start_clash",
            70,
            "开始拼点",
            "如果敌人的攻击线和友方角色的攻击线正好对上，线会变成黄色，这意味着双方将展开一次拼点。\n\n现在万事俱备，点击准备按钮进行结算。",
            TutorialBattleEventType.CombatExecutePressed,
            TutorialBattleInputPolicy.RequireBeforeCombat,
            "请先完成连线，再点击准备按钮开始拼点结算。",
            TutorialBattleHighlightTarget.BattleButton));
        definition.Steps.RemoveAll(step => step != null && step.stepId == "battle01_prepare_counter");
        definition.Reactions.Add(Reaction(
            "battle01_clash_explained",
            0,
            TutorialBattleEventType.ClashResolved,
            "这就是拼点",
            "可以看到我方角色的威力范围更高，这意味着这次拼点非常稳定。\n\n在攻击骰子拼点中，胜利的一方会摧毁失败方的骰子，并打出自己骰子的伤害。拼点可以化解对手攻击，也可能让你失去进攻机会，需要仔细斟酌。"));
        definition.Reactions.Add(Reaction(
            "battle01_enemy_single_explained",
            10,
            TutorialBattleEventType.EnemyOneSidedAttackResolved,
            "敌人的单方面攻击",
            "敌人刚才对友方角色造成了伤害。伤害取决于骰子的威力，威力越高，伤害越高。\n\n下一回合我们会主动连线，用拼点来化解这类攻击。"));
        definition.Reactions.Add(Reaction(
            "battle01_victory",
            20,
            TutorialBattleEventType.BattleWon,
            "恭喜你！",
            "你已经了解了基础战斗玩法：查看敌人、读取速度按钮、观察威力范围、拖拽连线并进行拼点。\n\n接下来的战斗就交给你自己了！"));
        TutorialBattleReactionDefinition enemySingleReaction = definition.Reactions.Find(reaction =>
            reaction != null && reaction.reactionId == "battle01_enemy_single_explained");
        if (enemySingleReaction != null)
        {
            enemySingleReaction.body = "\u654c\u4eba\u521a\u624d\u5bf9\u53cb\u65b9\u89d2\u8272\u9020\u6210\u4e86\u4f24\u5bb3\u3002\u4f24\u5bb3\u53d6\u51b3\u4e8e\u9ab0\u5b50\u7684\u5a01\u529b\uff0c\u5a01\u529b\u8d8a\u9ad8\uff0c\u4f24\u5bb3\u8d8a\u9ad8\u3002";
            enemySingleReaction.highlightAfterContinue = TutorialBattleHighlightTarget.BattleButton;
        }

        return definition;
    }

    private static TutorialBattleDefinition_SO CreateBattle02Definition()
    {
        TutorialBattleDefinition_SO definition = CreateDefinition("tutorial_battle_02_guard_and_redirect", "战斗教程 02：速度、守护与目标转移");
        definition.Steps.Add(Step(
            "battle02_press_prepare",
            -10,
            "先准备这一回合",
            "Molly 已经残血了，敌人的斩杀会优先寻找血量最低的目标。\n\n先点击准备按钮，生成本回合双方的速度按钮，再查看敌人和队友的行动内容。",
            TutorialBattleEventType.CombatExecutePressed,
            TutorialBattleInputPolicy.RequireBeforeCombat,
            "请先点击准备按钮，生成本回合的速度按钮。",
            TutorialBattleHighlightTarget.BattleButton));
        definition.Steps.Add(Step(
            "battle02_read_execution",
            0,
            "救下残血的同伴",
            "快看，Molly 已经残血了，而敌人的斩杀即将指向血量最低的目标。\n\n先点击敌人的速度按钮，看看它准备用什么技能。",
            TutorialBattleEventType.EnemySkillViewed,
            TutorialBattleInputPolicy.RequireBeforeCombat,
            "请先点击敌人的速度按钮，查看斩杀的目标偏好。",
            TutorialBattleHighlightTarget.FirstEnemySpeedButton));
        definition.Steps.Add(Step(
            "battle02_view_molly_skill",
            10,
            "Molly 的反击不够稳",
            "斩杀会优先攻击血量最低的目标。现在 Molly 只有 1 点生命，如果不处理这条线，她很可能会被击倒。\n\n先点击 Molly 的速度按钮，查看她能不能自己挡住这次攻击。",
            TutorialBattleEventType.AllySkillViewed,
            TutorialBattleInputPolicy.RequireBeforeCombat,
            "请点击 Molly 的速度按钮，查看轻击的威力范围。",
            TutorialBattleHighlightTarget.FirstAllySpeedButton));
        definition.Steps.Add(Step(
            "battle02_probe_with_molly",
            20,
            "先试着让 Molly 接敌人",
            "Molly 的轻击最多只能投出 2 点，而敌人的斩杀威力是 2 - 4。直接让 Molly 接上去并不可靠。\n\n先拖拽 Molly 的速度按钮连到敌人的速度按钮，观察当前目标关系。",
            TutorialBattleEventType.LineCreated,
            TutorialBattleInputPolicy.RequireBeforeCombat,
            "请先用 Molly 的速度按钮连到敌人的速度按钮。"));
        definition.Steps.Add(Step(
            "battle02_view_chara_guard",
            30,
            "看看 Chara 的守护",
            "这样下去还不够稳。再看看 Chara 的技能能不能破局。\n\n点击 Chara 的速度按钮，查看守护技能。",
            TutorialBattleEventType.AllySkillViewed,
            TutorialBattleInputPolicy.RequireBeforeCombat,
            "请点击 Chara 的速度按钮，查看守护。"));
        definition.Steps.Add(Step(
            "battle02_redirect_with_guard",
            40,
            "用高速角色接管目标",
            "守护是防御技能，适合拿来拦截敌人的攻击。\n\n用 Chara 的速度按钮连到敌人的速度按钮。因为 Chara 的速度更高，她可以把敌人的目标从残血 Molly 转移到自己身上。",
            TutorialBattleEventType.LineCreated,
            TutorialBattleInputPolicy.RequireBeforeCombat,
            "请用 Chara 的速度按钮连到敌人的速度按钮。"));
        definition.Steps.Add(Step(
            "battle02_execute_guard",
            50,
            "开始结算",
            "注意到了吗？敌人的斩杀原本会找血量最低的 Molly，但现在目标已经被更快的 Chara 接走。\n\n熟练运用高速度角色的技能拉取目标，可以在关键时刻保护脆弱队友。现在点击准备按钮进行结算。",
            TutorialBattleEventType.CombatExecutePressed,
            TutorialBattleInputPolicy.RequireBeforeCombat,
            "请先完成守护连线，再点击准备按钮开始结算。",
            TutorialBattleHighlightTarget.BattleButton));
        definition.Reactions.Add(Reaction(
            "battle02_guard_clash",
            0,
            TutorialBattleEventType.ClashResolved,
            "目标已经被转移",
            "原本会与残血 Molly 拼点的敌人，被高速的 Chara 接走了。\n\n这就是速度的另一个重要作用：速度更高的一方，可以更主动地改变目标关系，让危险攻击打到更适合承受的人身上。"));
        definition.Reactions.Add(Reaction(
            "battle02_victory",
            10,
            TutorialBattleEventType.BattleWon,
            "恭喜你！",
            "你已经理解了速度在目标转移中的作用。接下来遇到危险攻击时，先看谁被锁定，再判断有没有更快、更适合的人可以接走它。"));
        return definition;
    }

    private static TutorialBattleDefinition_SO CreateBattle03Definition()
    {
        TutorialBattleDefinition_SO definition = CreateDefinition("tutorial_battle_03_skill_and_energy", "战斗教程 03：技能切换与能量");
        definition.Steps.Add(Step(
            "battle03_press_prepare",
            -10,
            "先准备这一回合",
            "这一战会学习技能选择和能量消耗。\n\n先点击准备按钮，生成本回合的速度按钮，再查看我方角色当前可用的技能。",
            TutorialBattleEventType.CombatExecutePressed,
            TutorialBattleInputPolicy.RequireBeforeCombat,
            "请先点击准备按钮，生成本回合的速度按钮。",
            TutorialBattleHighlightTarget.BattleButton));
        definition.Steps.Add(Step(
            "battle03_view_ally_skill",
            0,
            "查看我方技能",
            "从现在开始，友方角色的技能选择应该成为决策的一部分。点击我方角色的速度按钮，看清当前有哪些技能、它们分别消耗多少能量、适合用来做什么。",
            TutorialBattleEventType.AllySkillViewed,
            TutorialBattleInputPolicy.RequireBeforeCombat,
            "先点开我方技能列表。"));
        definition.Steps.Add(Step(
            "battle03_switch_skill",
            10,
            "先切换一次技能",
            "请主动切换一次技能。连线之前就要先决定你打算用哪一招，强力技能通常会占用更多能量。",
            TutorialBattleEventType.SkillSwitched,
            TutorialBattleInputPolicy.RequireBeforeCombat,
            "先切换一次技能，再继续操作。"));
        definition.Steps.Add(Step(
            "battle03_reserve_energy",
            20,
            "用消耗能量的技能完成连线",
            "当你用有能耗的技能去接线时，能量会被消耗。能量不够时，就不能使用更高费用的技能。队伍的能量是共享的，因此需要仔细考虑应该让谁打出强力的技能，谁来负责能量的续航，保存更多能量。能量默认会在每回合开始时回复一点",
            TutorialBattleEventType.EnergyLineCreated,
            TutorialBattleInputPolicy.RequireBeforeCombat,
            "请用一招会消耗能量的技能完成连线。"));
        definition.Reactions.Add(Reaction(
            "battle03_victory",
            0,
            TutorialBattleEventType.BattleWon,
            "恭喜你！",
            "你学会了查看友方的技能和能量，选择更合适的技能策略。"));
        return definition;
    }

    private static TutorialBattleDefinition_SO CreateBattle04Definition()
    {
        TutorialBattleDefinition_SO definition = CreateDefinition("tutorial_battle_04_modifier_intro", "战斗教程 04：三种威力修正何时生效");
        definition.Steps.Add(Step(
            "battle04_view_ally_keywords",
            0,
            "注意技能说明里的三种前缀词条",
            "现在你需要了解三个词条中最重要的关键词：\n\n[使用时]：只要这招开始结算就会生效。\n[拼点时]：只有真正进入拼点时才会生效。\n[单方面攻击时]：只有没有拼点、直接打出去时才会生效。\n\n点开我方技能说明，把三种前缀词条都看一遍。",
            TutorialBattleEventType.AllySkillViewed,
            TutorialBattleInputPolicy.RequireBeforeCombat,
            "先查看我方技能说明，分清三种技能词条。"));
        definition.Steps.Add(Step(
            "battle04_switch_for_plan",
            10,
            "按你的计划切技能",
            "如果你准备拿去拼点，就优先想 [拼点时]；如果你准备单方面攻击，就优先想 [单方面攻击时]；如果局势不稳定，[使用时] 往往最稳定，最普适。",
            TutorialBattleEventType.SkillSwitched,
            TutorialBattleInputPolicy.RequireBeforeCombat,
            "先切一次技能，再决定这回合准备怎么打。"));
        definition.Steps.Add(Step(
            "battle04_start_round",
            20,
            "开始回合并对照结果",
            "开战后请对照实际发生的结算去理解词条：哪条线进了拼点，哪条线变成了单方面攻击。",
            TutorialBattleEventType.CombatExecutePressed,
            TutorialBattleInputPolicy.RequireBeforeCombat,
            "先看清技能词条并切好技能，再开始回合。"));
        definition.Reactions.Add(Reaction(
            "battle04_clash",
            0,
            TutorialBattleEventType.ClashResolved,
            "拼点时的威力修正只作用于拼点",
            "[拼点时] 只会在真正进入拼点时生效。如果同一招最后拼点胜利打出伤害，这条威力修正不会生效。但是你可以一直在拼点或追击时获得[使用时]的加成。"));
        definition.Reactions.Add(Reaction(
            "battle04_ally_single",
            10,
            TutorialBattleEventType.AllyOneSidedAttackResolved,
            "单方面攻击时吃到的是另一套加成",
            "当你的行动没有被拦住时，写着 [单方面攻击时] 的那部分会生效。同样的，单方面攻击时也可以稳定获得[使用时]的加成。使用带有错误词条的技能，收益会明显变差。"));
        definition.Reactions.Add(Reaction(
            "battle04_victory",
            20,
            TutorialBattleEventType.BattleWon,
            "恭喜你！",
            "现在你已经掌握三种技能词条分别在哪个结算里生效了。"));
        return definition;
    }

    private static TutorialBattleDefinition_SO CreateBattle05Definition()
    {
        TutorialBattleDefinition_SO mergedDefinition = CreateDefinition("tutorial_battle_05_merged_planning", "战斗教程 05：综合判断与路线规划");
        mergedDefinition.Steps.Add(Step(
            "battle05_read_enemy",
            0,
            "将敌方两条行动完整看完",
            "你要一次性同时判断三件事：哪条线更快、哪条线更危险、你手里的技能更适合拿去拼点还是单方面攻击。\n\n先观察敌方两条行动，再开始规划。\n\n这场战斗不再详细指引你应该怎么做，而是由你自己做出判断。",
            TutorialBattleEventType.EnemySkillViewed,
            TutorialBattleInputPolicy.RequireBeforeCombat,
            "先查看敌方技能说明，再决定这一回合怎么连线。"));
        mergedDefinition.Reactions.Add(Reaction(
            "battle05_victory",
            30,
            TutorialBattleEventType.BattleWon,
            "恭喜你！",
            "你已经熟练掌握了前面的所有内容。"));
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
            "状态",
            "这一战中你会遇到带有状态的技能词条，但别担心，这些状态的效果都很直观，只要养成读技能说明的习惯，状态就不会成为理解战斗的障碍。"));
        definition.Reactions.Add(Reaction(
            "battle07_victory",
            10,
            TutorialBattleEventType.BattleWon,
            "恭喜你！",
            "你完成了战斗教程！"));
        return definition;
    }

    private static EnemyGroup_SO CreateBattle01EnemyGroup(TutorialEnemyVisualTemplateSet visualTemplates)
    {
        EnemySkill_SO enemyLight = CreateSkill("轻击", "基础攻击。用来演示敌方单方面攻击与拼点。", 0, 1, 2);

        EnemySkill_SO allyHeavy = CreateSkill("重击", "稳定而有力的基础攻击。威力范围高于轻击，适合第一次学习拼点。", 0, 3, 4);

        EnemyData_SO enemy = CreateScriptedTutorialEnemy(
            "木桩鼠",
            10,
            2,
            2,
            1,
            new[] { enemyLight },
            new[] { allyHeavy },
            ("Round 1 Slot 1", enemyLight, 1, 200, 1),
            ("Round 2 Slot 1", enemyLight, 2, 200, 1),
            ("Round 3 Slot 1", enemyLight, 3, 200, 1));

        enemy.enemyDescription = "用于第一场教学战的低威胁敌人，只会使用轻击。";
        ApplyVisualTemplate(enemy, visualTemplates != null ? visualTemplates.openingPrimaryTemplate : null);
        return CreateGroup("TutorialBattleGroup_01", 9201, "教程战 01", "基础战斗与拼点", enemy);
    }

    private static EnemyGroup_SO CreateBattle02EnemyGroup(TutorialEnemyVisualTemplateSet visualTemplates)
    {
        EnemySkill_SO enemyExecute = CreateSkill("斩杀", "始终优先指向血量最低的目标。", 0, 2, 4);
        enemyExecute.targetPriorityMode = EnemySkill_SO.TargetPriorityMode.PreferLowestHP;

        EnemySkill_SO mollyLight = CreateSkill("轻击", "Molly 的基础攻击。", 0, 1, 2);
        EnemySkill_SO charaGuard = CreateSkill("守护", "Chara 用来转移危险攻击目标的防御技能。", 0, 2, 3);
        charaGuard.skillCategory = EnemySkill_SO.SkillCategory.Defense;

        EnemyData_SO enemy = CreateScriptedTutorialEnemy(
            "斩杀鼠",
            6,
            2,
            2,
            1,
            new[] { enemyExecute },
            new[] { mollyLight, charaGuard },
            ("Round 1 Slot 1", enemyExecute, 1, 200, 1),
            ("Round 2 Slot 1", enemyExecute, 2, 200, 1),
            ("Round 3 Slot 1", enemyExecute, 3, 200, 1));

        if (enemy.battleStartGrantedAllySkills != null && enemy.battleStartGrantedAllySkills.Count >= 2)
        {
            EnemyData_SO.BattleStartAllySkillGrantEntry mollyGrant = enemy.battleStartGrantedAllySkills[0];
            mollyGrant.targetAllyCharacterName = "Molly";
            enemy.battleStartGrantedAllySkills[0] = mollyGrant;

            EnemyData_SO.BattleStartAllySkillGrantEntry charaGrant = enemy.battleStartGrantedAllySkills[1];
            charaGrant.targetAllyCharacterName = "Chara";
            enemy.battleStartGrantedAllySkills[1] = charaGrant;
        }

        enemy.enemyDescription = "始终盯住血量最低的目标，用来教学高速守护与目标转移。";
        ApplyVisualTemplate(enemy, visualTemplates != null ? visualTemplates.openingSecondaryTemplate : null);
        return CreateGroup("TutorialBattleGroup_02", 9202, "教程战 02", "速度、守护与目标转移", enemy);
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
        string blockedMessage,
        TutorialBattleHighlightTarget highlightAfterContinue = TutorialBattleHighlightTarget.None)
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
            autoShow = true,
            highlightAfterContinue = highlightAfterContinue
        };
    }

    private static TutorialBattleReactionDefinition Reaction(
        string id,
        int sortOrder,
        TutorialBattleEventType triggerEvent,
        string title,
        string body,
        TutorialBattleHighlightTarget highlightAfterContinue = TutorialBattleHighlightTarget.None)
    {
        return new TutorialBattleReactionDefinition
        {
            reactionId = id,
            sortOrder = sortOrder,
            triggerEvent = triggerEvent,
            title = title,
            body = body,
            continueButtonLabel = "明白了",
            pauseForAcknowledgement = true,
            highlightAfterContinue = highlightAfterContinue
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
