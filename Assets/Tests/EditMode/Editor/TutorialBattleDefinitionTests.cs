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
    public void CreateRuntimeDefinition_Battle04_IncludesModifierTimingTeaching()
    {
        TutorialBattleDefinition_SO definition = TutorialBattleContentFactory.CreateRuntimeDefinition(TutorialBattleStageId.Battle04);

        Assert.IsNotNull(definition);
        Assert.AreEqual("tutorial_battle_04_modifier_intro", definition.tutorialBattleId);
        Assert.GreaterOrEqual(definition.GetOrderedSteps().Count, 3);
        Assert.IsTrue(definition.GetOrderedReactions().Count > 0);
        Assert.IsTrue(ContainsText(definition, "[拼点时]"));
        Assert.IsTrue(ContainsText(definition, "[单方面攻击时]"));
    }

    [Test]
    public void CreateRuntimeDefinition_Battle01_FocusesOnSpeedBeforeAdvancedTiming()
    {
        TutorialBattleDefinition_SO definition = TutorialBattleContentFactory.CreateRuntimeDefinition(TutorialBattleStageId.Battle01);

        Assert.IsNotNull(definition);
        Assert.IsTrue(ContainsText(definition, "速度"));
        Assert.IsFalse(ContainsText(definition, "[拼点时]"));
        Assert.IsFalse(ContainsText(definition, "[单方面攻击时]"));
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
