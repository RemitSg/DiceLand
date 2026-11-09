using System.Collections.Generic;

public enum TutorialBattleInteraction
{
    None,
    EnemyDetail,
    EnemySkill,
    AllySkill,
    Prepare,
    Execute,
    SwitchSkill,
    Connect,
    Redirect,
    EnergyConnect,
    CloseEnemyDetail,
    CloseEnemySkill,
    CloseAllySkill
}

/// <summary>First-use guidance lasts for one tutorial run, including its battle transitions.</summary>
public static class TutorialBattleInteractionGuidance
{
    private static readonly HashSet<TutorialBattleInteraction> completed = new HashSet<TutorialBattleInteraction>();

    public static bool NeedsGuidance(TutorialBattleInteraction interaction)
    {
        return interaction != TutorialBattleInteraction.None && !completed.Contains(interaction);
    }

    public static void Complete(TutorialBattleInteraction interaction)
    {
        if (interaction != TutorialBattleInteraction.None)
            completed.Add(interaction);
    }

    public static void Reset()
    {
        completed.Clear();
    }

    public static TutorialBattleInteraction ForStep(TutorialBattleStepDefinition step, bool preparing)
    {
        if (step == null) return TutorialBattleInteraction.None;
        switch (step.completionEvent)
        {
            case TutorialBattleEventType.EnemyDetailViewed: return TutorialBattleInteraction.EnemyDetail;
            case TutorialBattleEventType.EnemySkillViewed: return TutorialBattleInteraction.EnemySkill;
            case TutorialBattleEventType.AllySkillViewed: return TutorialBattleInteraction.AllySkill;
            case TutorialBattleEventType.SkillSwitched: return TutorialBattleInteraction.SwitchSkill;
            case TutorialBattleEventType.EnergyLineCreated: return TutorialBattleInteraction.EnergyConnect;
            case TutorialBattleEventType.LineCreated:
                return step.stepId == "battle02_redirect_with_guard"
                    ? TutorialBattleInteraction.Redirect : TutorialBattleInteraction.Connect;
            case TutorialBattleEventType.CombatExecutePressed:
                return preparing ? TutorialBattleInteraction.Prepare : TutorialBattleInteraction.Execute;
            default: return TutorialBattleInteraction.None;
        }
    }
}
