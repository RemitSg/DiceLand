using NUnit.Framework;
using UnityEngine;

public class TutorialBattleDefinitionTests
{
    [Test]
    public void ForceValidate_AssignsStableDefaultsAndOrdersSteps()
    {
        TutorialBattleDefinition_SO definition = ScriptableObject.CreateInstance<TutorialBattleDefinition_SO>();
        definition.Steps.Add(new TutorialBattleStepDefinition
        {
            stepId = string.Empty,
            sortOrder = 20,
            title = string.Empty
        });
        definition.Steps.Add(new TutorialBattleStepDefinition
        {
            stepId = "line",
            sortOrder = 10,
            title = "Draw A Line"
        });

        definition.ForceValidateForTests();

        Assert.AreEqual("tutorial_battle", definition.tutorialBattleId);
        Assert.AreEqual(2, definition.GetOrderedSteps().Count);
        Assert.AreEqual("line", definition.GetOrderedSteps()[0].stepId);
        Assert.IsFalse(string.IsNullOrWhiteSpace(definition.GetOrderedSteps()[1].stepId));
        Assert.IsFalse(string.IsNullOrWhiteSpace(definition.GetOrderedSteps()[1].title));
    }

    [Test]
    public void CreateRuntimeDefault_ForEliteBattle_IncludesStatusTeachingContent()
    {
        TutorialBattleDefinition_SO definition = TutorialBattleContentFactory.CreateRuntimeDefinition(MapTileType.Elite);

        Assert.IsNotNull(definition);
        Assert.AreEqual("tutorial_battle_elite", definition.tutorialBattleId);
        Assert.GreaterOrEqual(definition.GetOrderedSteps().Count, 3);
        Assert.IsTrue(definition.GetOrderedReactions().Count > 0);
        Assert.IsTrue(ContainsText(definition, "状态"));
        Assert.IsTrue(ContainsText(definition, "技能"));
    }

    [Test]
    public void CreateRuntimeDefault_ForOpeningBattle_TeachesModifierTimingThroughInspectAndCombatEvents()
    {
        TutorialBattleDefinition_SO definition = TutorialBattleContentFactory.CreateRuntimeDefinition(MapTileType.Battle);

        Assert.IsNotNull(definition);

        bool hasAllyInspectStep = false;
        bool hasClashReaction = false;
        bool hasAllySingleReaction = false;
        bool hasEnemySingleReaction = false;

        var steps = definition.GetOrderedSteps();
        for (int i = 0; i < steps.Count; i++)
        {
            TutorialBattleStepDefinition step = steps[i];
            if (step != null && step.completionEvent == TutorialBattleEventType.AllySkillViewed)
                hasAllyInspectStep = true;
        }

        var reactions = definition.GetOrderedReactions();
        for (int i = 0; i < reactions.Count; i++)
        {
            TutorialBattleReactionDefinition reaction = reactions[i];
            if (reaction == null)
                continue;

            hasClashReaction |= reaction.triggerEvent == TutorialBattleEventType.ClashResolved;
            hasAllySingleReaction |= reaction.triggerEvent == TutorialBattleEventType.AllyOneSidedAttackResolved;
            hasEnemySingleReaction |= reaction.triggerEvent == TutorialBattleEventType.EnemyOneSidedAttackResolved;
        }

        Assert.IsTrue(hasAllyInspectStep);
        Assert.IsTrue(hasClashReaction);
        Assert.IsTrue(hasAllySingleReaction);
        Assert.IsTrue(hasEnemySingleReaction);
    }

    private static bool ContainsText(TutorialBattleDefinition_SO definition, string text)
    {
        if (definition == null || string.IsNullOrWhiteSpace(text))
            return false;

        var steps = definition.GetOrderedSteps();
        for (int i = 0; i < steps.Count; i++)
        {
            TutorialBattleStepDefinition step = steps[i];
            if (step == null)
                continue;

            if ((!string.IsNullOrWhiteSpace(step.title) && step.title.Contains(text))
                || (!string.IsNullOrWhiteSpace(step.body) && step.body.Contains(text)))
            {
                return true;
            }
        }

        var reactions = definition.GetOrderedReactions();
        for (int i = 0; i < reactions.Count; i++)
        {
            TutorialBattleReactionDefinition reaction = reactions[i];
            if (reaction == null)
                continue;

            if ((!string.IsNullOrWhiteSpace(reaction.title) && reaction.title.Contains(text))
                || (!string.IsNullOrWhiteSpace(reaction.body) && reaction.body.Contains(text)))
            {
                return true;
            }
        }

        return false;
    }
}
