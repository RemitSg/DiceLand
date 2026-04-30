using NUnit.Framework;

public class BattleSharedEnergyReservationServiceTests
{
    [Test]
    public void CalculateSkillReservation_UsesConnectedCountAndEnergyCost()
    {
        Assert.AreEqual(6, BattleSharedEnergyReservationService.CalculateSkillReservation(3, 2));
    }

    [Test]
    public void CalculateSkillSelectionRequirement_RequiresAtLeastOneUse()
    {
        Assert.AreEqual(2, BattleSharedEnergyReservationService.CalculateSkillSelectionRequirement(0, 2));
        Assert.AreEqual(6, BattleSharedEnergyReservationService.CalculateSkillSelectionRequirement(3, 2));
    }

    [Test]
    public void CalculateAvailableEnergyForAlly_RemovesOtherReservationsOnly()
    {
        Assert.AreEqual(7, BattleSharedEnergyReservationService.CalculateAvailableEnergyForAlly(10, 5, 2));
    }

    [Test]
    public void HasEnoughEnergyForSkill_ChecksRequirementAgainstAvailableEnergy()
    {
        Assert.IsTrue(BattleSharedEnergyReservationService.HasEnoughEnergyForSkill(6, 2, 3));
        Assert.IsFalse(BattleSharedEnergyReservationService.HasEnoughEnergyForSkill(5, 2, 3));
    }
}
