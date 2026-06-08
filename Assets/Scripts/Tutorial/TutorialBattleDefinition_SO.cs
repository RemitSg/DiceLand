using System;
using System.Collections.Generic;
using UnityEngine;

public enum TutorialBattleEventType
{
    None = 0,
    ContinuePressed = 1,
    AllySkillViewed = 2,
    EnemySkillViewed = 3,
    EnemyDetailViewed = 4,
    SkillSwitched = 5,
    LineCreated = 6,
    EnergyLineCreated = 7,
    CombatExecutePressed = 8,
    ClashResolved = 9,
    AllyOneSidedAttackResolved = 10,
    EnemyOneSidedAttackResolved = 11,
    StatusObserved = 12,
    StatusApplied = 13,
    BattleWon = 14,
    BattleLost = 15
}

public enum TutorialBattleInputPolicy
{
    Informational = 0,
    ModalBlock = 1,
    RequireBeforeCombat = 2
}

public enum TutorialBattleHighlightTarget
{
    None = 0,
    BattleButton = 1,
    FirstEnemySpeedButton = 2,
    FirstAllySpeedButton = 3
}

[Serializable]
public class TutorialBattleStepDefinition
{
    public string stepId = "step";
    public string title = "Tutorial Step";
    [TextArea(2, 8)] public string body;
    public int sortOrder;
    public TutorialBattleEventType completionEvent = TutorialBattleEventType.ContinuePressed;
    public TutorialBattleInputPolicy inputPolicy = TutorialBattleInputPolicy.ModalBlock;
    public bool autoShow = true;
    public string continueButtonLabel = "继续";
    public string blockedMessage = "请先完成当前教程提示。";
    public TutorialBattleHighlightTarget highlightAfterContinue = TutorialBattleHighlightTarget.None;
}

[Serializable]
public class TutorialBattleReactionDefinition
{
    public string reactionId = "reaction";
    public string title = "Battle Hint";
    [TextArea(2, 8)] public string body;
    public int sortOrder;
    public TutorialBattleEventType triggerEvent = TutorialBattleEventType.None;
    public bool pauseForAcknowledgement = true;
    public TutorialBattleHighlightTarget highlightAfterContinue = TutorialBattleHighlightTarget.None;
    public string continueButtonLabel = "明白了";
}

[CreateAssetMenu(fileName = "TutorialBattleDefinition", menuName = "Tutorial/Tutorial Battle Definition")]
public class TutorialBattleDefinition_SO : ScriptableObject
{
    public string tutorialBattleId = "tutorial_battle";
    public string displayName = "Tutorial Battle";

    [SerializeField] private List<TutorialBattleStepDefinition> steps = new List<TutorialBattleStepDefinition>();
    [SerializeField] private List<TutorialBattleReactionDefinition> reactions = new List<TutorialBattleReactionDefinition>();

    public List<TutorialBattleStepDefinition> Steps => steps;
    public List<TutorialBattleReactionDefinition> Reactions => reactions;

    public IReadOnlyList<TutorialBattleStepDefinition> GetOrderedSteps()
    {
        List<TutorialBattleStepDefinition> ordered = new List<TutorialBattleStepDefinition>();
        if (steps != null)
        {
            for (int i = 0; i < steps.Count; i++)
            {
                if (steps[i] != null)
                    ordered.Add(steps[i]);
            }
        }

        ordered.Sort((left, right) =>
        {
            int compare = left.sortOrder.CompareTo(right.sortOrder);
            if (compare != 0)
                return compare;

            return string.CompareOrdinal(left.stepId ?? string.Empty, right.stepId ?? string.Empty);
        });

        return ordered;
    }

    public IReadOnlyList<TutorialBattleReactionDefinition> GetOrderedReactions()
    {
        List<TutorialBattleReactionDefinition> ordered = new List<TutorialBattleReactionDefinition>();
        if (reactions != null)
        {
            for (int i = 0; i < reactions.Count; i++)
            {
                if (reactions[i] != null)
                    ordered.Add(reactions[i]);
            }
        }

        ordered.Sort((left, right) =>
        {
            int compare = left.sortOrder.CompareTo(right.sortOrder);
            if (compare != 0)
                return compare;

            return string.CompareOrdinal(left.reactionId ?? string.Empty, right.reactionId ?? string.Empty);
        });

        return ordered;
    }

    public void ForceValidateForTests()
    {
        OnValidate();
    }

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(tutorialBattleId))
            tutorialBattleId = "tutorial_battle";
        if (string.IsNullOrWhiteSpace(displayName))
            displayName = "Tutorial Battle";
        if (steps == null)
            steps = new List<TutorialBattleStepDefinition>();
        if (reactions == null)
            reactions = new List<TutorialBattleReactionDefinition>();

        for (int i = 0; i < steps.Count; i++)
        {
            TutorialBattleStepDefinition step = steps[i];
            if (step == null)
                continue;

            if (string.IsNullOrWhiteSpace(step.stepId))
                step.stepId = $"step_{i + 1}";
            if (string.IsNullOrWhiteSpace(step.title))
                step.title = "Tutorial Step";
            if (string.IsNullOrWhiteSpace(step.continueButtonLabel))
                step.continueButtonLabel = "继续";
            if (string.IsNullOrWhiteSpace(step.blockedMessage))
                step.blockedMessage = "请先完成当前教程提示。";
        }

        for (int i = 0; i < reactions.Count; i++)
        {
            TutorialBattleReactionDefinition reaction = reactions[i];
            if (reaction == null)
                continue;

            if (string.IsNullOrWhiteSpace(reaction.reactionId))
                reaction.reactionId = $"reaction_{i + 1}";
            if (string.IsNullOrWhiteSpace(reaction.title))
                reaction.title = "Battle Hint";
            if (string.IsNullOrWhiteSpace(reaction.continueButtonLabel))
                reaction.continueButtonLabel = "明白了";
        }
    }
}
