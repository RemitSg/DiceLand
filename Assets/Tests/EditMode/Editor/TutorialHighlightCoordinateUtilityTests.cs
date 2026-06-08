using NUnit.Framework;
using UnityEngine;

public class TutorialHighlightCoordinateUtilityTests
{
    [Test]
    public void ConvertScreenRectToReferenceRect_UsesCanvasScalerMatchScale()
    {
        Rect result = TutorialHighlightCoordinateUtility.ConvertScreenRectToReferenceRect(
            Rect.MinMaxRect(0f, 0f, 1080f, 2400f),
            new Vector2(1080f, 2400f),
            new Vector2(1920f, 1080f),
            0.5f);

        Assert.AreEqual(0f, result.xMin, 0.01f);
        Assert.AreEqual(0f, result.yMin, 0.01f);
        Assert.AreEqual(966.461f, result.xMax, 0.01f);
        Assert.AreEqual(2146.625f, result.yMax, 0.01f);
    }

    [Test]
    public void ConvertScreenRectToReferenceRect_ExpandsByBleedPixels()
    {
        Rect result = TutorialHighlightCoordinateUtility.ConvertScreenRectToReferenceRect(
            Rect.MinMaxRect(100f, 200f, 500f, 600f),
            new Vector2(1080f, 2400f),
            new Vector2(1920f, 1080f),
            0.5f,
            2f);

        Assert.AreEqual(87.443f, result.xMin, 0.01f);
        Assert.AreEqual(176.885f, result.yMin, 0.01f);
        Assert.AreEqual(449.214f, result.xMax, 0.01f);
        Assert.AreEqual(538.656f, result.yMax, 0.01f);
    }
}
