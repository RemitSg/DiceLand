using NUnit.Framework;

public class MapBackgroundControllerTests
{
    [Test]
    public void ResolveActionPointBackgroundPhase_UsesMorningAtSixtyPercentOrHigher()
    {
        Assert.AreEqual(MapBackgroundPhase.Morning, MapBackgroundController.ResolveActionPointBackgroundPhase(6, 10));
        Assert.AreEqual(MapBackgroundPhase.Morning, MapBackgroundController.ResolveActionPointBackgroundPhase(10, 10));
    }

    [Test]
    public void ResolveActionPointBackgroundPhase_UsesNoonAtTwentyPercentOrHigher()
    {
        Assert.AreEqual(MapBackgroundPhase.Noon, MapBackgroundController.ResolveActionPointBackgroundPhase(2, 10));
        Assert.AreEqual(MapBackgroundPhase.Noon, MapBackgroundController.ResolveActionPointBackgroundPhase(5, 10));
    }

    [Test]
    public void ResolveActionPointBackgroundPhase_UsesDuskAboveZeroBelowTwentyPercent()
    {
        Assert.AreEqual(MapBackgroundPhase.Dusk, MapBackgroundController.ResolveActionPointBackgroundPhase(1, 10));
    }

    [Test]
    public void CalculateEdgeVignetteAlpha_KeepsCenterClearAndDarkensEdges()
    {
        float centerAlpha = MapBackgroundController.CalculateEdgeVignetteAlpha(0f);
        float midAlpha = MapBackgroundController.CalculateEdgeVignetteAlpha(0.65f);
        float edgeAlpha = MapBackgroundController.CalculateEdgeVignetteAlpha(1f);

        Assert.Less(centerAlpha, 0.05f);
        Assert.Greater(midAlpha, centerAlpha);
        Assert.Greater(edgeAlpha, midAlpha);
    }

    [Test]
    public void CalculateCameraCoverWorldSize_UsesWideCameraWidthWhenLargerThanBoardFallback()
    {
        float coverSize = MapBackgroundController.CalculateCameraCoverWorldSize(20f, 10f, 16f / 9f, 2f);

        Assert.GreaterOrEqual(coverSize, 39.5f);
    }

    [Test]
    public void CalculateViewLockedScaleRatio_GrowsWithCameraOrthographicSize()
    {
        Assert.AreEqual(1f, MapBackgroundController.CalculateViewLockedScaleRatio(8f, 8f));
        Assert.AreEqual(0.5f, MapBackgroundController.CalculateViewLockedScaleRatio(8f, 4f));
        Assert.AreEqual(2f, MapBackgroundController.CalculateViewLockedScaleRatio(8f, 16f));
    }
}
