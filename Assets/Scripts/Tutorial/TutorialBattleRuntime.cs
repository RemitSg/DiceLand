using System;
using System.Collections.Generic;

public sealed class TutorialBattleProgressResult
{
    public bool stepCompleted;
    public TutorialBattleReactionDefinition reactionToShow;
}

public static class TutorialBattleRuntime
{
    private static readonly HashSet<string> shownReactionIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    public static TutorialBattleDefinition_SO ActiveDefinition { get; private set; }
    public static int CurrentStepIndex { get; private set; } = -1;
    public static bool IsActive => ActiveDefinition != null;

    private static IReadOnlyList<TutorialBattleStepDefinition> OrderedSteps =>
        ActiveDefinition != null ? ActiveDefinition.GetOrderedSteps() : Array.Empty<TutorialBattleStepDefinition>();

    public static TutorialBattleStepDefinition CurrentStep
    {
        get
        {
            IReadOnlyList<TutorialBattleStepDefinition> steps = OrderedSteps;
            return CurrentStepIndex >= 0 && CurrentStepIndex < steps.Count ? steps[CurrentStepIndex] : null;
        }
    }

    public static bool HasPendingStepPopup { get; private set; }

    public static void BeginBattle(TutorialBattleDefinition_SO definition)
    {
        Clear();
        ActiveDefinition = definition;
        if (definition == null)
            return;

        CurrentStepIndex = OrderedSteps.Count > 0 ? 0 : -1;
        HasPendingStepPopup = CurrentStep != null && CurrentStep.autoShow;
    }

    public static TutorialBattleStepDefinition ConsumePendingStepPopup()
    {
        if (!HasPendingStepPopup)
            return null;

        HasPendingStepPopup = false;
        return CurrentStep;
    }

    public static TutorialBattleProgressResult NotifyEvent(TutorialBattleEventType eventType)
    {
        TutorialBattleProgressResult result = new TutorialBattleProgressResult();
        if (!IsActive)
            return result;

        IReadOnlyList<TutorialBattleReactionDefinition> reactions = ActiveDefinition.GetOrderedReactions();
        for (int i = 0; i < reactions.Count; i++)
        {
            TutorialBattleReactionDefinition reaction = reactions[i];
            if (reaction == null
                || reaction.triggerEvent != eventType
                || shownReactionIds.Contains(reaction.reactionId))
            {
                continue;
            }

            shownReactionIds.Add(reaction.reactionId);
            result.reactionToShow = reaction;
            break;
        }

        TutorialBattleStepDefinition step = CurrentStep;
        if (step != null && step.completionEvent == eventType)
        {
            result.stepCompleted = true;
            AdvanceStep();
        }

        return result;
    }

    public static bool TryRequestCombatExecution(out string blockedMessage)
    {
        blockedMessage = null;
        if (!IsActive)
            return true;

        TutorialBattleStepDefinition stepBeforeEvent = CurrentStep;
        NotifyEvent(TutorialBattleEventType.CombatExecutePressed);
        if (stepBeforeEvent != null
            && stepBeforeEvent.completionEvent == TutorialBattleEventType.CombatExecutePressed)
        {
            return true;
        }

        TutorialBattleStepDefinition step = CurrentStep;
        if (step == null || step.inputPolicy != TutorialBattleInputPolicy.RequireBeforeCombat)
            return true;

        blockedMessage = string.IsNullOrWhiteSpace(step.blockedMessage)
            ? "请先完成当前教程提示。"
            : step.blockedMessage;
        HasPendingStepPopup |= step.autoShow;
        return false;
    }

    public static void Clear()
    {
        ActiveDefinition = null;
        CurrentStepIndex = -1;
        HasPendingStepPopup = false;
        shownReactionIds.Clear();
    }

    private static void AdvanceStep()
    {
        IReadOnlyList<TutorialBattleStepDefinition> steps = OrderedSteps;
        if (steps.Count == 0)
        {
            CurrentStepIndex = -1;
            HasPendingStepPopup = false;
            return;
        }

        CurrentStepIndex++;
        if (CurrentStepIndex < 0 || CurrentStepIndex >= steps.Count)
        {
            CurrentStepIndex = -1;
            HasPendingStepPopup = false;
            return;
        }

        HasPendingStepPopup = CurrentStep != null && CurrentStep.autoShow;
    }
}
