using NUnit.Framework;

public class BattlePresentationRangePulseServiceTests
{
    [Test]
    public void Evaluate_WhenFixedPulseExpired_ReturnsZero()
    {
        float pulse = BattlePresentationRangePulseService.Evaluate(
            elapsedSeconds: 2f,
            durationSeconds: 0.5f,
            frequency: 2.1f,
            continuous: false);

        Assert.AreEqual(0f, pulse);
    }

    [Test]
    public void Evaluate_WhenContinuousPulseOutlivesInitialDuration_KeepsPulsing()
    {
        float pulse = BattlePresentationRangePulseService.Evaluate(
            elapsedSeconds: 10.25f,
            durationSeconds: 0.5f,
            frequency: 2.1f,
            continuous: true);

        Assert.Greater(pulse, 0f);
    }
}
