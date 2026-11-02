using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Captures an action's visible resource text before its callback replaces the UI.</summary>
public static class MapFeedbackOrigin
{
    public static Vector2? Current { get; private set; }

    public static IDisposable FromText(TMP_Text text, RectTransform fallback = null)
    {
        Vector2? origin = TryGetMoneyPosition(text, out Vector2 position) ? position
            : fallback != null ? NormalizedPosition(fallback, fallback.rect.center) : (Vector2?)null;
        return new OriginScope(origin);
    }

    public static Vector2 NormalizedPosition(RectTransform rect, Vector3 localPosition)
    {
        Canvas canvas = rect.GetComponentInParent<Canvas>();
        Canvas root = canvas != null ? canvas.rootCanvas : null;
        Camera camera = root != null && root.renderMode != RenderMode.ScreenSpaceOverlay ? root.worldCamera : null;
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(localPosition));
        float width = camera != null && camera.targetTexture != null ? camera.targetTexture.width : Screen.width;
        float height = camera != null && camera.targetTexture != null ? camera.targetTexture.height : Screen.height;
        return new Vector2(Mathf.Clamp01(screen.x / Mathf.Max(1f, width)), Mathf.Clamp01(screen.y / Mathf.Max(1f, height)));
    }

    public static bool TryGetMoneyPosition(TMP_Text text, out Vector2 position)
    {
        position = default;
        if (text == null || !text.isActiveAndEnabled) return false;
        text.ForceMeshUpdate();
        TMP_TextInfo info = text.textInfo;
        for (int i = 0; i + 1 < info.characterCount; i++)
        {
            TMP_CharacterInfo first = info.characterInfo[i], second = info.characterInfo[i + 1];
            if (first.character != '金' || (second.character != '币' && second.character != '钱') || !first.isVisible) continue;
            Vector3 center = (first.bottomLeft + second.topRight) * 0.5f;
            Vector3 world = text.transform.TransformPoint(center);
            bool visible = true;
            // A scrolled-off reward must not become the particle source.
            foreach (RectMask2D mask in text.GetComponentsInParent<RectMask2D>())
                if (mask.isActiveAndEnabled && !mask.rectTransform.rect.Contains(mask.rectTransform.InverseTransformPoint(world)))
                { visible = false; break; }
            if (!visible) continue;
            position = NormalizedPosition(text.rectTransform, center);
            return true;
        }
        return false;
    }

    private sealed class OriginScope : IDisposable
    {
        private readonly Vector2? previous;
        private bool disposed;
        public OriginScope(Vector2? position) { previous = Current; Current = position; }
        public void Dispose()
        {
            if (disposed) return;
            Current = previous;
            disposed = true;
        }
    }
}
