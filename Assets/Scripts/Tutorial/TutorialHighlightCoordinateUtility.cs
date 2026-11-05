using UnityEngine;

public static class TutorialHighlightCoordinateUtility
{
    private static readonly Vector3[] corners = new Vector3[4];

    public static bool TryGetScreenRect(RectTransform target, out Rect result)
    {
        result = default;
        if (target == null || !target.gameObject.activeInHierarchy) return false;
        Canvas canvas = target.GetComponentInParent<Canvas>();
        if (canvas != null && (!canvas.isActiveAndEnabled || !canvas.rootCanvas.isActiveAndEnabled)) return false;
        Camera camera = canvas != null && canvas.rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null
            : canvas != null && canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
        if (!Project(target, camera, out result)) return false;
        for (Transform parent = target.parent; parent != null; parent = parent.parent)
        {
            var mask = parent.GetComponent<UnityEngine.UI.RectMask2D>();
            var stencil = parent.GetComponent<UnityEngine.UI.Mask>();
            if ((mask != null && mask.isActiveAndEnabled) || (stencil != null && stencil.isActiveAndEnabled))
            {
                if (!Project((RectTransform)parent, camera, out Rect clip) || !Intersect(result, clip, out result)) return false;
            }
        }
        Rect viewport = camera == null ? new Rect(0, 0, Screen.width, Screen.height)
            : camera.targetTexture != null ? new Rect(0, 0, camera.targetTexture.width, camera.targetTexture.height) : camera.pixelRect;
        return Intersect(result, viewport, out result);
    }

    private static bool Project(RectTransform target, Camera camera, out Rect result)
    {
        target.GetWorldCorners(corners);
        Vector2 min = Vector2.positiveInfinity, max = Vector2.negativeInfinity;
        foreach (Vector3 corner in corners)
        {
            if (camera != null && camera.WorldToScreenPoint(corner).z <= 0) { result = default; return false; }
            Vector2 point = RectTransformUtility.WorldToScreenPoint(camera, corner);
            min = Vector2.Min(min, point); max = Vector2.Max(max, point);
        }
        result = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        return result.width > .5f && result.height > .5f;
    }

    public static bool TryScreenRectToLocal(RectTransform destination, Rect screen, out Rect result)
    {
        result = default;
        if (destination == null || screen.width <= .5f || screen.height <= .5f) return false;
        Canvas canvas = destination.GetComponentInParent<Canvas>();
        Camera camera = canvas != null && canvas.rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        Vector2 min = Vector2.positiveInfinity, max = Vector2.negativeInfinity;
        for (int i = 0; i < 4; i++)
        {
            Vector2 point = new Vector2(i % 2 == 0 ? screen.xMin : screen.xMax, i < 2 ? screen.yMin : screen.yMax);
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(destination, point, camera, out Vector2 local)) return false;
            min = Vector2.Min(min, local); max = Vector2.Max(max, local);
        }
        result = Rect.MinMaxRect(min.x, min.y, max.x, max.y); return true;
    }

    public static bool Intersect(Rect a, Rect b, out Rect intersection)
    {
        intersection = Rect.MinMaxRect(Mathf.Max(a.xMin, b.xMin), Mathf.Max(a.yMin, b.yMin), Mathf.Min(a.xMax, b.xMax), Mathf.Min(a.yMax, b.yMax));
        return intersection.width > .5f && intersection.height > .5f;
    }

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
