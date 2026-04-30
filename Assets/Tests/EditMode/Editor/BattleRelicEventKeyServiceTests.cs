using NUnit.Framework;

public class BattleRelicEventKeyServiceTests
{
    [Test]
    public void BuildAllySkillUseKey_NullAttackerUsesStableNoneKey()
    {
        Assert.AreEqual("ally:none:3", BattleRelicEventKeyService.BuildAllySkillUseKey(null, currentRound: 3));
    }

    [Test]
    public void BuildSingleAttackTakenKey_UsesNoneWhenParticipantsMissing()
    {
        Assert.AreEqual(
            "enemy:none:ally:none:4:2",
            BattleRelicEventKeyService.BuildSingleAttackTakenKey(null, null, currentRound: 4, diceIndex: 2));
    }
}
