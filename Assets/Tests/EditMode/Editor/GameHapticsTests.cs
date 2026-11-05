using NUnit.Framework;

public sealed class GameHapticsTests
{
    [Test]
    public void CoinClashEnemyDeathAndAllyDeathHaveIncreasingWeightAndBoundedLength()
    {
        float previousStrength = 0f;
        int previousLength = 0;
        foreach (GameHapticCue cue in new[] { GameHapticCue.CoinArrived, GameHapticCue.Clash,
                     GameHapticCue.EnemyDefeated, GameHapticCue.AllyDefeated })
        {
            GameHapticPulse pulse = GameHaptics.GetPulse(cue);
            Assert.Greater(pulse.intensity, previousStrength);
            Assert.Greater(pulse.DurationMilliseconds, previousLength);
            Assert.LessOrEqual(pulse.DurationMilliseconds, 150);
            Assert.That(pulse.intensity, Is.InRange(0.01f, 1f));
            previousStrength = pulse.intensity;
            previousLength = pulse.DurationMilliseconds;
        }
    }

    [Test]
    public void DenseCoinArrivalsAreMergedWithoutBuildingADeferredQueue()
    {
        var gate = new GameHapticGate();
        GameHapticPulse coin = GameHaptics.GetPulse(GameHapticCue.CoinArrived);
        Assert.IsTrue(gate.TryAccept(coin, 10, 1));
        Assert.IsFalse(gate.TryAccept(coin, 10, 1));
        Assert.IsFalse(gate.TryAccept(coin, 10.03, 2));
        Assert.IsTrue(gate.TryAccept(coin, 10.08, 3));
    }

    [Test]
    public void DeathOverridesClashAndCoinsCannotInterruptTheDeathTail()
    {
        var gate = new GameHapticGate();
        Assert.IsTrue(gate.TryAccept(GameHaptics.GetPulse(GameHapticCue.Clash), 10, 1));
        Assert.IsTrue(gate.TryAccept(GameHaptics.GetPulse(GameHapticCue.EnemyDefeated), 10, 1));
        Assert.IsFalse(gate.TryAccept(GameHaptics.GetPulse(GameHapticCue.CoinArrived), 10.08, 2));
        Assert.IsTrue(gate.TryAccept(GameHaptics.GetPulse(GameHapticCue.AllyDefeated), 10.08, 2));
        Assert.IsFalse(gate.TryAccept(GameHaptics.GetPulse(GameHapticCue.EnemyDefeated), 10.1, 3));
        Assert.IsTrue(gate.TryAccept(GameHaptics.GetPulse(GameHapticCue.CoinArrived), 10.31, 4));
    }

    [Test]
    public void CancelClearsTheCooldownAndInvalidTimesNeverPoisonIt()
    {
        var gate = new GameHapticGate();
        GameHapticPulse pulse = GameHaptics.GetPulse(GameHapticCue.AllyDefeated);
        Assert.IsFalse(gate.TryAccept(pulse, double.NaN, 1));
        Assert.IsFalse(gate.TryAccept(pulse, double.PositiveInfinity, 1));
        Assert.IsTrue(gate.TryAccept(pulse, 10, 1));
        gate.Reset();
        Assert.IsTrue(gate.TryAccept(GameHaptics.GetPulse(GameHapticCue.CoinArrived), 10, 1));
    }
}
