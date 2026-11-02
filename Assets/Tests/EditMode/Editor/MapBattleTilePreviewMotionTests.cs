using NUnit.Framework;

public class MapBattleTilePreviewMotionTests
{
    [TestCase(0f, 0f)]
    [TestCase(0.25f, 0.103515625f)]
    [TestCase(0.5f, 0.5f)]
    [TestCase(0.75f, 0.896484375f)]
    [TestCase(1f, 1f)]
    public void Evaluate_ReturnsSmoothProgress(float t, float expected)
    {
        Assert.AreEqual(expected, MapBattleTilePreviewMotion.Evaluate(t), 0.000001f);
    }

    [Test]
    public void Evaluate_ClampsValuesOutsideUnitInterval()
    {
        Assert.AreEqual(0f, MapBattleTilePreviewMotion.Evaluate(-1f));
        Assert.AreEqual(1f, MapBattleTilePreviewMotion.Evaluate(2f));
    }

    [Test]
    public void Evaluate_StartAndEndStepsAreSmallerThanMiddleStep()
    {
        float startStep = MapBattleTilePreviewMotion.Evaluate(0.01f) - MapBattleTilePreviewMotion.Evaluate(0f);
        float middleStep = MapBattleTilePreviewMotion.Evaluate(0.505f) - MapBattleTilePreviewMotion.Evaluate(0.495f);
        float endStep = MapBattleTilePreviewMotion.Evaluate(1f) - MapBattleTilePreviewMotion.Evaluate(0.99f);

        Assert.Less(startStep, middleStep);
        Assert.Less(endStep, middleStep);
    }

    [Test]
    public void Evaluate_EasesAccelerationAtBothEndsWithoutOvershoot()
    {
        const float step = 0.001f;
        float startAcceleration = (MapBattleTilePreviewMotion.Evaluate(2f * step)
            - 2f * MapBattleTilePreviewMotion.Evaluate(step)) / (step * step);
        float endAcceleration = (MapBattleTilePreviewMotion.Evaluate(1f - 2f * step)
            - 2f * MapBattleTilePreviewMotion.Evaluate(1f - step) + 1f) / (step * step);
        Assert.Less(System.Math.Abs(startAcceleration), 0.2f);
        Assert.Less(System.Math.Abs(endAcceleration), 3f); // Float precision near 1.

        float previous = 0f;
        for (int i = 1; i <= 100; i++)
        {
            float value = MapBattleTilePreviewMotion.Evaluate(i / 100f);
            Assert.That(value, Is.InRange(previous, 1f));
            previous = value;
        }
    }
}
