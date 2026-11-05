using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Keeps legacy Text APIs/fonts, using a TMP child only when inline resource icons are needed.</summary>
public sealed class ResourceKeywordLegacyText : Text
{
    public bool IncludeIcons { get; set; } = true;
    private TextMeshProUGUI resourceText;
    private bool synchronizing;
    private static readonly Dictionary<Font, TMP_FontAsset> fonts = new Dictionary<Font, TMP_FontAsset>();

    public override void SetVerticesDirty()
    {
        base.SetVerticesDirty();
        Synchronize();
    }

    private void Synchronize()
    {
        if (synchronizing) return;
        synchronizing = true;
        try
        {
            bool show = isActiveAndEnabled && font != null && ResourceKeywordText.ContainsKeyword(text);
            if (!show) { if (resourceText != null) resourceText.gameObject.SetActive(false); return; }
            if (!fonts.TryGetValue(font, out TMP_FontAsset face) || face == null)
            {
                face = TMP_FontAsset.CreateFontAsset(font);
                if (face == null) return;
                face.name = font.name + " Resource UI";
                face.hideFlags = HideFlags.DontSave;
                fonts[font] = face;
            }
            if (resourceText == null)
            {
                GameObject go = new GameObject("ResourceKeywordDisplay", typeof(RectTransform));
                go.transform.SetParent(transform, false);
                RectTransform rect = (RectTransform)go.transform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = rect.offsetMax = Vector2.zero;
                resourceText = go.AddComponent<TextMeshProUGUI>();
                resourceText.raycastTarget = false;
                ResourceKeywordText.Attach(resourceText, IncludeIcons);
            }
            resourceText.gameObject.SetActive(true);
            resourceText.font = face;
            resourceText.fontSize = fontSize;
            resourceText.enableAutoSizing = resizeTextForBestFit;
            resourceText.fontSizeMin = resizeTextMinSize;
            resourceText.fontSizeMax = resizeTextMaxSize;
            resourceText.color = color;
            resourceText.text = text;
            resourceText.fontStyle = fontStyle == FontStyle.Bold ? FontStyles.Bold : fontStyle == FontStyle.Italic ? FontStyles.Italic
                : fontStyle == FontStyle.BoldAndItalic ? FontStyles.Bold | FontStyles.Italic : FontStyles.Normal;
            resourceText.alignment = Alignment(alignment);
            resourceText.enableWordWrapping = horizontalOverflow == HorizontalWrapMode.Wrap;
            resourceText.overflowMode = verticalOverflow == VerticalWrapMode.Overflow ? TextOverflowModes.Overflow : TextOverflowModes.Truncate;
            resourceText.lineSpacing = (lineSpacing - 1f) * 100f;
        }
        finally { synchronizing = false; }
    }

    protected override void OnEnable() { base.OnEnable(); Synchronize(); }
    protected override void OnDisable() { if (resourceText != null) resourceText.gameObject.SetActive(false); base.OnDisable(); }

    protected override void OnPopulateMesh(VertexHelper toFill)
    {
        if (resourceText != null && resourceText.gameObject.activeSelf) toFill.Clear();
        else base.OnPopulateMesh(toFill);
    }

    public override float preferredHeight => resourceText != null && resourceText.gameObject.activeSelf
        ? resourceText.GetPreferredValues(Mathf.Max(1f, rectTransform.rect.width), 0f).y : base.preferredHeight;
    public override float preferredWidth => resourceText != null && resourceText.gameObject.activeSelf
        ? resourceText.GetPreferredValues().x : base.preferredWidth;

    private static TextAlignmentOptions Alignment(TextAnchor value)
    {
        switch (value)
        {
            case TextAnchor.UpperLeft: return TextAlignmentOptions.TopLeft;
            case TextAnchor.UpperCenter: return TextAlignmentOptions.Top;
            case TextAnchor.UpperRight: return TextAlignmentOptions.TopRight;
            case TextAnchor.MiddleLeft: return TextAlignmentOptions.Left;
            case TextAnchor.MiddleCenter: return TextAlignmentOptions.Center;
            case TextAnchor.MiddleRight: return TextAlignmentOptions.Right;
            case TextAnchor.LowerLeft: return TextAlignmentOptions.BottomLeft;
            case TextAnchor.LowerCenter: return TextAlignmentOptions.Bottom;
            default: return TextAlignmentOptions.BottomRight;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOwnedFonts()
    {
        foreach (TMP_FontAsset face in fonts.Values)
        {
            if (face == null) continue;
            foreach (Texture2D texture in face.atlasTextures) DestroyOwned(texture);
            DestroyOwned(face.material);
            DestroyOwned(face);
        }
        fonts.Clear();
    }

    private static void DestroyOwned(Object value)
    {
        if (value == null) return;
        if (Application.isPlaying) Destroy(value);
        else DestroyImmediate(value);
    }
}
