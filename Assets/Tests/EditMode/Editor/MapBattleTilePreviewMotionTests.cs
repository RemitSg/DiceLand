using NUnit.Framework;

public class MapBattleTilePreviewMotionTests
{
    [TestCase(0f, 0f)]
    [TestCase(0.25f, 0.15625f)]
    [TestCase(0.5f, 0.5f)]
    [TestCase(0.75f, 0.84375f)]
    [TestCase(1f, 1f)]
    public void Evaluate_ReturnsCubicBezierProgress(float t, float expected)
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
}
