public enum TutorialBattleStageId
{
    None = 0,
    Battle01 = 1,
    Battle02 = 2,
    Battle03 = 3,
    Battle04 = 4,
    Battle05 = 5,
    Battle06 = 6,
    Battle07 = 7
}

public static class TutorialBattleStageUtility
{
    public static bool IsBattleStage(TutorialBattleStageId stageId)
    {
        return stageId >= TutorialBattleStageId.Battle01
            && stageId <= TutorialBattleStageId.Battle07;
    }

    public static bool IsFinalStage(TutorialBattleStageId stageId)
    {
        return stageId == TutorialBattleStageId.Battle07;
    }
}
