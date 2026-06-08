using UnityEngine;

public static class TutorialHighlightCoordinateUtility
{
    public static Rect ConvertScreenRectToReferenceRect(
        Rect screenRect,
        Vector2 screenSize,
        Vector2 referenceResolution,
        float matchWidthOrHeight,
        float bleedPixels = 0f)
    {
        float scaleFactor = CalculateCanvasScaleFactor(screenSize, referenceResolution, matchWidthOrHeight);
        float bleed = Mathf.Max(0f, bleedPixels);
        float xMin = (screenRect.xMin / scaleFactor) - bleed;
        float xMax = (screenRect.xMax / scaleFactor) + bleed;
        float yMin = (screenRect.yMin / scaleFactor) - bleed;
        float yMax = (screenRect.yMax / scaleFactor) + bleed;
        return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
    }

    public static Vector2 CalculateCanvasReferenceSize(
        Vector2 screenSize,
        Vector2 referenceResolution,
        float matchWidthOrHeight)
    {
        float scaleFactor = CalculateCanvasScaleFactor(screenSize, referenceResolution, matchWidthOrHeight);
        return new Vector2(
            Mathf.Max(1f, screenSize.x) / scaleFactor,
            Mathf.Max(1f, screenSize.y) / scaleFactor);
    }

    public static float CalculateCanvasScaleFactor(
        Vector2 screenSize,
        Vector2 referenceResolution,
        float matchWidthOrHeight)
    {
        float screenWidth = Mathf.Max(1f, screenSize.x);
        float screenHeight = Mathf.Max(1f, screenSize.y);
        float referenceWidth = Mathf.Max(1f, referenceResolution.x);
        float referenceHeight = Mathf.Max(1f, referenceResolution.y);
        float match = Mathf.Clamp01(matchWidthOrHeight);

        float logWidth = Mathf.Log(screenWidth / referenceWidth, 2f);
        float logHeight = Mathf.Log(screenHeight / referenceHeight, 2f);
        return Mathf.Pow(2f, Mathf.Lerp(logWidth, logHeight, match));
    }
}
