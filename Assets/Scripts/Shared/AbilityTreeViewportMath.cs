using UnityEngine;

public static class AbilityTreeViewportMath
{
    public static Vector2 ClampOffset(Vector2 offset, Rect contentBounds, Vector2 viewportSize, float padding)
    {
        return new Vector2(
            ClampAxis(offset.x, contentBounds.xMin, contentBounds.xMax, viewportSize.x, padding),
            ClampAxis(offset.y, contentBounds.yMin, contentBounds.yMax, viewportSize.y, padding));
    }

    private static float ClampAxis(float offset, float contentMin, float contentMax, float viewportSize, float padding)
    {
        if (viewportSize <= 0f || contentMax < contentMin)
            return 0f;

        float contentSize = contentMax - contentMin;
        if (contentSize + padding * 2f <= viewportSize)
            return -(contentMin + contentMax) * 0.5f;

        float halfViewport = viewportSize * 0.5f;
        float minOffset = halfViewport - padding - contentMax;
        float maxOffset = -halfViewport + padding - contentMin;
        if (minOffset > maxOffset)
            return (minOffset + maxOffset) * 0.5f;

        return Mathf.Clamp(offset, minOffset, maxOffset);
    }
}
