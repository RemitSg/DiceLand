using NUnit.Framework;

public class TutorialBattleStageRoutingTests
{
    [Test]
    public void OnlyBattle07IsFinalStage()
    {
        Assert.IsFalse(TutorialBattleStageUtility.IsFinalStage(TutorialBattleStageId.None));
        Assert.IsFalse(TutorialBattleStageUtility.IsFinalStage(TutorialBattleStageId.Battle01));
        Assert.IsFalse(TutorialBattleStageUtility.IsFinalStage(TutorialBattleStageId.Battle02));
        Assert.IsFalse(TutorialBattleStageUtility.IsFinalStage(TutorialBattleStageId.Battle03));
        Assert.IsFalse(TutorialBattleStageUtility.IsFinalStage(TutorialBattleStageId.Battle04));
        Assert.IsFalse(TutorialBattleStageUtility.IsFinalStage(TutorialBattleStageId.Battle05));
        Assert.IsFalse(TutorialBattleStageUtility.IsFinalStage(TutorialBattleStageId.Battle06));
        Assert.IsTrue(TutorialBattleStageUtility.IsFinalStage(TutorialBattleStageId.Battle07));
    }
}
