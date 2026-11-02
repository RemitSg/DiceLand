using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore;

/// <summary>Presentation-only resource terms. Raw text, reward messages and save data stay unchanged.</summary>
public static class ResourceKeywordText
{
    public const string MoneyColor = "F0CC6B";
    public const string ActionPointColor = "9EDBB8";
    public const string KeyColor = "B8D9E6";
    private static readonly string[] Words = { "金币", "金钱", "行动点", "钥匙" };
    private static readonly string[] Colors = { MoneyColor, MoneyColor, ActionPointColor, KeyColor };
    private static readonly string[] Icons = { "DL_Money", "DL_Money", "DL_ActionPoints", "DL_Keys" };
    private static TMP_SpriteAsset spriteAsset;
    private static readonly Dictionary<TMP_SpriteAsset, TMP_SpriteAsset> spriteVariants = new Dictionary<TMP_SpriteAsset, TMP_SpriteAsset>();

    public static bool ContainsKeyword(string source)
    {
        if (string.IsNullOrEmpty(source)) return false;
        foreach (string word in Words) if (source.IndexOf(word, StringComparison.Ordinal) >= 0) return true;
        return false;
    }

    public static Sprite IconFor(string source)
    {
        if (string.IsNullOrEmpty(source)) return null;
        for (int i = 0; i < Words.Length; i++)
            if (source.IndexOf(Words[i], StringComparison.Ordinal) >= 0) return MapForestArt.Get(i < 2 ? "Coins" : i == 2 ? "Footprint" : "Key");
        return null;
    }

    public static Color ColorFor(string source, Color fallback)
    {
        if (string.IsNullOrEmpty(source)) return fallback;
        for (int i = 0; i < Words.Length; i++)
            if (source.IndexOf(Words[i], StringComparison.Ordinal) >= 0 && ColorUtility.TryParseHtmlString("#" + Colors[i], out Color color)) return color;
        return fallback;
    }

    public static void Attach(TMP_Text text, bool includeIcons = true)
    {
        if (text == null) return;
        // This HUD already has its own icons, sizes and resource colours.
        for (Transform parent = text.transform; parent != null; parent = parent.parent)
            if (parent.name == "TopLeftPanel") return;
        if (text.textPreprocessor is Preprocessor current)
            current.includeIcons = includeIcons;
        else
            text.textPreprocessor = new Preprocessor(text.textPreprocessor, includeIcons);
        text.richText = true;
        if (includeIcons) text.spriteAsset = WithResourceSprites(text.spriteAsset);
        text.SetAllDirty();
    }

    public static string Format(string source, bool includeIcons = true)
    {
        if (!ContainsKeyword(source)) return source;
        StringBuilder output = new StringBuilder(source.Length + 96);
        int noParseDepth = 0, resourceLinkDepth = 0;
        for (int i = 0; i < source.Length;)
        {
            if (source[i] == '<' && TryTagEnd(source, i, out int end))
            {
                string tag = source.Substring(i, end - i + 1);
                if (tag.Equals("<noparse>", StringComparison.OrdinalIgnoreCase)) noParseDepth++;
                else if (tag.Equals("</noparse>", StringComparison.OrdinalIgnoreCase)) noParseDepth = Math.Max(0, noParseDepth - 1);
                else if (noParseDepth == 0)
                {
                    if (tag.StartsWith("<link=\"dl-resource-", StringComparison.Ordinal)) resourceLinkDepth++;
                    else if (resourceLinkDepth > 0 && tag.StartsWith("<link", StringComparison.OrdinalIgnoreCase)) resourceLinkDepth++;
                    else if (tag.Equals("</link>", StringComparison.OrdinalIgnoreCase)) resourceLinkDepth = Math.Max(0, resourceLinkDepth - 1);
                }
                output.Append(tag);
                i = end + 1;
                continue;
            }
            bool matched = false;
            if (noParseDepth == 0 && resourceLinkDepth == 0)
                for (int word = 0; word < Words.Length; word++)
                {
                    string value = Words[word];
                    if (i + value.Length > source.Length || string.CompareOrdinal(source, i, value, 0, value.Length) != 0) continue;
                    output.Append("<nobr><link=\"dl-resource-").Append(Icons[word]).Append("\"><color=#").Append(Colors[word]).Append('>');
                    if (includeIcons) output.Append("<sprite name=\"").Append(Icons[word]).Append("\" tint=0>");
                    output.Append(value).Append("</color></link></nobr>");
                    i += value.Length;
                    matched = true;
                    break;
                }
            if (!matched) output.Append(source[i++]);
        }
        return output.ToString();
    }

    private static bool TryTagEnd(string source, int start, out int end)
    {
        end = start;
        int first = start + 1;
        if (first >= source.Length) return false;
        if (source[first] == '/') first++;
        if (first >= source.Length || !(source[first] >= 'A' && source[first] <= 'Z' || source[first] >= 'a' && source[first] <= 'z' || source[first] == '#')) return false;
        char quote = '\0';
        for (int i = first; i < source.Length; i++)
        {
            char c = source[i];
            if (quote != '\0') { if (c == quote) quote = '\0'; }
            else if (c == '\"' || c == '\'') quote = c;
            else if (c == '>') { end = i; return true; }
        }
        return false;
    }

    public static TMP_SpriteAsset Sprites
    {
        get
        {
            if (spriteAsset != null) return spriteAsset;
            string[] sources = { "Coins", "Footprint", "Key" }, names = { "DL_Money", "DL_ActionPoints", "DL_Keys" };
            Sprite first = MapForestArt.Get(sources[0]);
            if (first == null) throw new InvalidOperationException("Packaged map resource icons are missing.");
            TMP_SpriteAsset asset = ScriptableObject.CreateInstance<TMP_SpriteAsset>();
            asset.name = "DiceLandResourceKeywords";
            asset.hideFlags = HideFlags.DontSave;
            asset.spriteSheet = first.texture;
            Shader shader = Shader.Find("TextMeshPro/Sprite");
            if (shader == null) throw new InvalidOperationException("TextMeshPro/Sprite shader is missing.");
            asset.material = new Material(shader) { name = asset.name + " Material", mainTexture = first.texture, hideFlags = HideFlags.DontSave };
            // TMP 3.0 exposes its schema setters internally. Initialize its supported legacy
            // migration while the tables are empty, before adding current-format glyphs.
            asset.spriteInfoList = new List<TMP_Sprite>();
            asset.UpdateLookupTables();
            for (int i = 0; i < sources.Length; i++)
            {
                Sprite sprite = MapForestArt.Get(sources[i]);
                if (sprite == null || sprite.texture != first.texture) throw new InvalidOperationException("Resource icon atlas is inconsistent.");
                float width = 100f * sprite.rect.width / sprite.rect.height;
                TMP_SpriteGlyph glyph = new TMP_SpriteGlyph { index = (uint)i, glyphRect = new GlyphRect(sprite.rect),
                    metrics = new GlyphMetrics(width, 100f, 0f, 82f, width + 7f), scale = 1f, sprite = sprite };
                asset.spriteGlyphTable.Add(glyph);
                asset.spriteCharacterTable.Add(new TMP_SpriteCharacter((uint)(0xE100 + i), glyph) { name = names[i], scale = 1f });
            }
            asset.UpdateLookupTables();
            spriteAsset = asset;
            return spriteAsset;
        }
    }

    private static TMP_SpriteAsset WithResourceSprites(TMP_SpriteAsset existing)
    {
        TMP_SpriteAsset resources = Sprites;
        if (existing == null || existing == resources) return resources;
        foreach (TMP_SpriteAsset value in spriteVariants.Values) if (existing == value) return value;
        if (spriteVariants.TryGetValue(existing, out TMP_SpriteAsset variant)) return variant;
        variant = UnityEngine.Object.Instantiate(existing);
        variant.name = existing.name + " with DiceLand resources";
        variant.hideFlags = HideFlags.DontSave;
        variant.fallbackSpriteAssets = existing.fallbackSpriteAssets != null ? new List<TMP_SpriteAsset>(existing.fallbackSpriteAssets) : new List<TMP_SpriteAsset>();
        variant.fallbackSpriteAssets.Add(resources);
        variant.UpdateLookupTables();
        spriteVariants[existing] = variant;
        return variant;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOwnedAssets()
    {
        foreach (TMP_SpriteAsset variant in spriteVariants.Values) DestroyOwned(variant);
        spriteVariants.Clear();
        if (spriteAsset != null) { DestroyOwned(spriteAsset.material); DestroyOwned(spriteAsset); }
        spriteAsset = null;
    }

    private static void DestroyOwned(UnityEngine.Object value)
    {
        if (value == null) return;
        if (Application.isPlaying) UnityEngine.Object.Destroy(value);
        else UnityEngine.Object.DestroyImmediate(value);
    }

    private sealed class Preprocessor : ITextPreprocessor
    {
        private readonly ITextPreprocessor previous;
        public bool includeIcons;
        private string lastInput, lastOutput;
        private bool lastIcons;
        public Preprocessor(ITextPreprocessor previous, bool includeIcons) { this.previous = previous; this.includeIcons = includeIcons; }
        public string PreprocessText(string text)
        {
            string source = previous != null ? previous.PreprocessText(text) : text;
            if (source == lastInput && lastIcons == includeIcons && lastOutput != null) return lastOutput;
            lastInput = source;
            lastIcons = includeIcons;
            return lastOutput = Format(source, includeIcons);
        }
    }
}
