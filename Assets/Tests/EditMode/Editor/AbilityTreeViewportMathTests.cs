using NUnit.Framework;
using UnityEngine;

public class AbilityTreeViewportMathTests
{
    [Test]
    public void ClampOffset_ClampsLargeContentInsideReachableRange()
    {
        Rect bounds = Rect.MinMaxRect(-200f, -100f, 1200f, 100f);

        Vector2 clamped = AbilityTreeViewportMath.ClampOffset(new Vector2(0f, 0f), bounds, new Vector2(1000f, 600f), 80f);

        Assert.AreEqual(-220f, clamped.x);
        Assert.AreEqual(0f, clamped.y);
    }

    [Test]
    public void ClampOffset_CentersContentWhenItFitsViewport()
    {
        Rect bounds = Rect.MinMaxRect(200f, -50f, 400f, 150f);

        Vector2 clamped = AbilityTreeViewportMath.ClampOffset(new Vector2(320f, -120f), bounds, new Vector2(1000f, 600f), 80f);

        Assert.AreEqual(-300f, clamped.x);
        Assert.AreEqual(-50f, clamped.y);
    }
}
