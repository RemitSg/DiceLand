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
    public void CalculateAvailableEnergyForAlly_IncludesTemporaryEnergy()
    {
        Assert.AreEqual(10, BattleSharedEnergyReservationService.CalculateAvailableEnergyForAlly(
            currentSharedEnergy: 8,
            temporarySharedEnergy: 3,
            reservedSharedEnergy: 5,
            allyReservedEnergy: 4));
    }

    [Test]
    public void CalculateTotalAvailableEnergy_IncludesTemporaryEnergyWithoutMaxClamp()
    {
        Assert.AreEqual(13, BattleSharedEnergyReservationService.CalculateTotalAvailableEnergy(
            currentSharedEnergy: 10,
            temporarySharedEnergy: 3));
    }

    [Test]
    public void HasEnoughEnergyForSkill_ChecksRequirementAgainstAvailableEnergy()
    {
        Assert.IsTrue(BattleSharedEnergyReservationService.HasEnoughEnergyForSkill(6, 2, 3));
        Assert.IsFalse(BattleSharedEnergyReservationService.HasEnoughEnergyForSkill(5, 2, 3));
    }

    [Test]
    public void SpendReservedEnergy_ConsumesTemporaryEnergyBeforeNormalEnergy()
    {
        BattleSharedEnergySpendResult result = BattleSharedEnergyReservationService.SpendReservedEnergy(
            currentSharedEnergy: 5,
            temporarySharedEnergy: 3,
            reservedSharedEnergy: 6,
            maxSharedEnergy: 10);

        Assert.AreEqual(2, result.CurrentSharedEnergy);
        Assert.AreEqual(0, result.TemporarySharedEnergy);
        Assert.AreEqual(0, result.ReservedSharedEnergy);
    }

    [Test]
    public void SpendReservedEnergy_ReportsTemporaryAndNormalSpentAmounts()
    {
        BattleSharedEnergySpendResult result = BattleSharedEnergyReservationService.SpendReservedEnergy(
            currentSharedEnergy: 5,
            temporarySharedEnergy: 3,
            reservedSharedEnergy: 6,
            maxSharedEnergy: 10);

        Assert.AreEqual(3, result.SpentTemporarySharedEnergy);
        Assert.AreEqual(3, result.SpentSharedEnergy);
    }
}
