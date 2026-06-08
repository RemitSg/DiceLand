using System.Reflection;
using NUnit.Framework;

public class TutorialBattleStageRoutingTests
{
    private const TutorialBattleStageId OutOfRangeBattleStage = (TutorialBattleStageId)999;

    [TestCase(TutorialBattleStageId.None, false)]
    [TestCase(TutorialBattleStageId.Battle01, true)]
    [TestCase(TutorialBattleStageId.Battle07, true)]
    [TestCase(OutOfRangeBattleStage, false)]
    public void IsBattleStage_UsesDefinedBattleRange(TutorialBattleStageId stageId, bool expected)
    {
        Assert.That(TutorialBattleStageUtility.IsBattleStage(stageId), Is.EqualTo(expected));
    }

    [TestCase(TutorialBattleStageId.None, false)]
    [TestCase(TutorialBattleStageId.Battle01, false)]
    [TestCase(TutorialBattleStageId.Battle03, true)]
    [TestCase(TutorialBattleStageId.Battle07, false)]
    public void OnlyBattle03IsFinalStage(TutorialBattleStageId stageId, bool expected)
    {
        Assert.That(TutorialBattleStageUtility.IsFinalStage(stageId), Is.EqualTo(expected));
    }

    [TestCase(1, TutorialBattleStageId.Battle01)]
    [TestCase(2, TutorialBattleStageId.Battle02)]
    [TestCase(3, TutorialBattleStageId.None)]
    [TestCase(4, TutorialBattleStageId.None)]
    [TestCase(5, TutorialBattleStageId.Battle03)]
    [TestCase(6, TutorialBattleStageId.None)]
    public void ResolveTutorialStageId_MapsTutorialRouteIndices(int sequenceIndex, TutorialBattleStageId expected)
    {
        MethodInfo method = typeof(RunGameManager).GetMethod("ResolveTutorialStageId", BindingFlags.Static | BindingFlags.NonPublic);

        Assert.IsNotNull(method);
        Assert.That((TutorialBattleStageId)method.Invoke(null, new object[] { sequenceIndex }), Is.EqualTo(expected));
    }

    [Test]
    public void BattleEncounterData_CanCarryTutorialStageIdAndFinalStageFlag()
    {
        BattleEncounterData data = new BattleEncounterData
        {
            isTutorialEncounter = true,
            tutorialBattleStageId = TutorialBattleStageId.Battle03,
            returnToMainMenuOnVictory = true
        };

        Assert.That(data.tutorialBattleStageId, Is.EqualTo(TutorialBattleStageId.Battle03));
        Assert.That(data.returnToMainMenuOnVictory, Is.True);
    }
}
