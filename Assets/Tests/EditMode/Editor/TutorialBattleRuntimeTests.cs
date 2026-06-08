using NUnit.Framework;
using UnityEngine;

public class TutorialBattleRuntimeTests
{
    [SetUp]
    public void SetUp()
    {
        TutorialBattleRuntime.Clear();
        MainMenuRuntime.ClearTutorialRequest();
    }

    [Test]
    public void BeginBattle_QueuesFirstStepPopupAndStartsAtFirstStep()
    {
        TutorialBattleDefinition_SO definition = ScriptableObject.CreateInstance<TutorialBattleDefinition_SO>();
        definition.Steps.Add(new TutorialBattleStepDefinition
        {
            stepId = "intro",
            sortOrder = 0,
            title = "Intro",
            completionEvent = TutorialBattleEventType.ContinuePressed
        });
        definition.ForceValidateForTests();

        TutorialBattleRuntime.BeginBattle(definition);

        Assert.IsTrue(TutorialBattleRuntime.IsActive);
        Assert.AreEqual("intro", TutorialBattleRuntime.CurrentStep.stepId);

        TutorialBattleStepDefinition popup = TutorialBattleRuntime.ConsumePendingStepPopup();
        Assert.IsNotNull(popup);
        Assert.AreEqual("intro", popup.stepId);
    }

    [Test]
    public void NotifyEvent_CompletesCurrentStepAndQueuesNextPopup()
    {
        TutorialBattleDefinition_SO definition = ScriptableObject.CreateInstance<TutorialBattleDefinition_SO>();
        definition.Steps.Add(new TutorialBattleStepDefinition
        {
            stepId = "inspect_enemy",
            sortOrder = 0,
            title = "Inspect Enemy",
            completionEvent = TutorialBattleEventType.EnemySkillViewed
        });
        definition.Steps.Add(new TutorialBattleStepDefinition
        {
            stepId = "switch_skill",
            sortOrder = 10,
            title = "Switch Skill",
            completionEvent = TutorialBattleEventType.SkillSwitched
        });
        definition.ForceValidateForTests();

        TutorialBattleRuntime.BeginBattle(definition);
        TutorialBattleRuntime.ConsumePendingStepPopup();

        TutorialBattleProgressResult result = TutorialBattleRuntime.NotifyEvent(TutorialBattleEventType.EnemySkillViewed);

        Assert.IsTrue(result.stepCompleted);
        Assert.AreEqual("switch_skill", TutorialBattleRuntime.CurrentStep.stepId);
        Assert.AreEqual("switch_skill", TutorialBattleRuntime.ConsumePendingStepPopup().stepId);
    }

    [Test]
    public void TryRequestCombatExecution_BlocksUntilRequiredLessonActionCompletes()
    {
        TutorialBattleDefinition_SO definition = ScriptableObject.CreateInstance<TutorialBattleDefinition_SO>();
        definition.Steps.Add(new TutorialBattleStepDefinition
        {
            stepId = "line",
            sortOrder = 0,
            title = "Create Line",
            completionEvent = TutorialBattleEventType.LineCreated,
            inputPolicy = TutorialBattleInputPolicy.RequireBeforeCombat
        });
        definition.ForceValidateForTests();

        TutorialBattleRuntime.BeginBattle(definition);

        Assert.IsFalse(TutorialBattleRuntime.TryRequestCombatExecution(out string blockedMessage));
        Assert.IsFalse(string.IsNullOrWhiteSpace(blockedMessage));

        TutorialBattleRuntime.NotifyEvent(TutorialBattleEventType.LineCreated);

        Assert.IsTrue(TutorialBattleRuntime.TryRequestCombatExecution(out _));
    }

    [Test]
    public void TryRequestCombatExecution_AllowsExecutionWhenCurrentStepRequiresCombatButton()
    {
        TutorialBattleDefinition_SO definition = ScriptableObject.CreateInstance<TutorialBattleDefinition_SO>();
        definition.Steps.Add(new TutorialBattleStepDefinition
        {
            stepId = "press_combat",
            sortOrder = 0,
            title = "Press Combat",
            completionEvent = TutorialBattleEventType.CombatExecutePressed,
            inputPolicy = TutorialBattleInputPolicy.RequireBeforeCombat
        });
        definition.Steps.Add(new TutorialBattleStepDefinition
        {
            stepId = "view_skill",
            sortOrder = 10,
            title = "View Skill",
            completionEvent = TutorialBattleEventType.AllySkillViewed,
            inputPolicy = TutorialBattleInputPolicy.RequireBeforeCombat,
            blockedMessage = "View skill first"
        });
        definition.ForceValidateForTests();

        TutorialBattleRuntime.BeginBattle(definition);

        Assert.IsTrue(TutorialBattleRuntime.TryRequestCombatExecution(out string blockedMessage));
        Assert.IsTrue(string.IsNullOrWhiteSpace(blockedMessage));
        Assert.AreEqual("view_skill", TutorialBattleRuntime.CurrentStep.stepId);
    }

    [Test]
    public void NotifyEvent_ReturnsConfiguredReactionOnlyOnce()
    {
        TutorialBattleDefinition_SO definition = ScriptableObject.CreateInstance<TutorialBattleDefinition_SO>();
        definition.Reactions.Add(new TutorialBattleReactionDefinition
        {
            reactionId = "clash",
            title = "Clash",
            triggerEvent = TutorialBattleEventType.ClashResolved
        });
        definition.ForceValidateForTests();

        TutorialBattleRuntime.BeginBattle(definition);

        TutorialBattleProgressResult first = TutorialBattleRuntime.NotifyEvent(TutorialBattleEventType.ClashResolved);
        TutorialBattleProgressResult second = TutorialBattleRuntime.NotifyEvent(TutorialBattleEventType.ClashResolved);

        Assert.IsNotNull(first.reactionToShow);
        Assert.AreEqual("clash", first.reactionToShow.reactionId);
        Assert.IsNull(second.reactionToShow);
    }
}
