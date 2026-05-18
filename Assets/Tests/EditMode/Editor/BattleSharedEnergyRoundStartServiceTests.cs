using NUnit.Framework;

public class BattleSharedEnergyRoundStartServiceTests
{
    [Test]
    public void Resolve_RecoversOneEnergyAndClearsReservation()
    {
        BattleSharedEnergyRoundStartState state = BattleSharedEnergyRoundStartService.Resolve(
            currentSharedEnergy: 4,
            temporarySharedEnergy: 0,
            currentSharedEnergyMax: 10);

        Assert.AreEqual(5, state.CurrentSharedEnergy);
        Assert.AreEqual(0, state.TemporarySharedEnergy);
        Assert.AreEqual(0, state.ReservedSharedEnergy);
    }

    [Test]
    public void Resolve_ClampsRecoveryToMaxEnergy()
    {
        BattleSharedEnergyRoundStartState state = BattleSharedEnergyRoundStartService.Resolve(
            currentSharedEnergy: 10,
            temporarySharedEnergy: 0,
            currentSharedEnergyMax: 10);

        Assert.AreEqual(10, state.CurrentSharedEnergy);
        Assert.AreEqual(0, state.TemporarySharedEnergy);
        Assert.AreEqual(0, state.ReservedSharedEnergy);
    }

    [Test]
    public void Resolve_ClearsTemporaryEnergyAtRoundStart()
    {
        BattleSharedEnergyRoundStartState state = BattleSharedEnergyRoundStartService.Resolve(
            currentSharedEnergy: 4,
            temporarySharedEnergy: 3,
            currentSharedEnergyMax: 10);

        Assert.AreEqual(5, state.CurrentSharedEnergy);
        Assert.AreEqual(0, state.TemporarySharedEnergy);
        Assert.AreEqual(0, state.ReservedSharedEnergy);
    }
}
