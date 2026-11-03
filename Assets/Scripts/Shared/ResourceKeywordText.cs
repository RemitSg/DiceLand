using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore;

/// <summary>Presentation-only resource, attribute and health terms. Raw text and game data stay unchanged.</summary>
public static partial class ResourceKeywordText
{
    public const string MoneyColor = "F0CC6B";
    public const string ActionPointColor = "9EDBB8";
    public const string KeyColor = "B8D9E6";
    public const string ExperienceColor = "B8C9F4";
    private static TMP_SpriteAsset spriteAsset;
    private static TMP_SpriteAsset experienceSprites;
    private static readonly Dictionary<TMP_SpriteAsset, TMP_SpriteAsset> spriteVariants = new Dictionary<TMP_SpriteAsset, TMP_SpriteAsset>();

    public static bool ContainsKeyword(string source)
    {
        if (string.IsNullOrEmpty(source)) return false;
        for (int i = 0; i < Keywords.Length; i++) if (FindWord(source, i) >= 0) return true;
        return false;
    }

    public static Sprite IconFor(string source)
    {
        if (string.IsNullOrEmpty(source)) return null;
        for (int i = 0; i < Keywords.Length; i++)
            if (FindWord(source, i) >= 0) return IconFor(Keywords[i]);
        return null;
    }

    public static Color ColorFor(string source, Color fallback)
    {
        if (string.IsNullOrEmpty(source)) return fallback;
        for (int i = 0; i < Keywords.Length; i++)
            if (FindWord(source, i) >= 0 && ColorUtility.TryParseHtmlString("#" + Keywords[i].Color, out Color color)) return color;
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
                for (int word = 0; word < Keywords.Length; word++)
                {
                    Keyword keyword = Keywords[word];
                    string value = keyword.Word;
                    if (!MatchesWord(source, i, word)) continue;
                    output.Append("<nobr><link=\"dl-resource-").Append(keyword.IconName).Append("\"><color=#").Append(keyword.Color).Append('>');
                    if (includeIcons) output.Append("<sprite name=\"").Append(keyword.IconName).Append("\" tint=0>");
                    output.Append(source, i, value.Length).Append("</color></link></nobr>");
                    i += value.Length;
                    matched = true;
                    break;
                }
            if (!matched) output.Append(source[i++]);
        }
        return output.ToString();
    }

    public static Sprite ExperienceIcon => Resources.Load<Sprite>("MapUI/ExperienceStar");

    private static bool MatchesWord(string source, int at, int word)
    {
        Keyword keyword = Keywords[word];
        string value = keyword.Word;
        bool english = value == "EXP" || value == "HP";
        if (at + value.Length > source.Length || string.Compare(source, at, value, 0, value.Length,
            english ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal) != 0) return false;
        if (keyword.Attribute) return IsAttributeMention(source, at);
        if (!english) return true;
        // Numeric forms such as 13EXP / 20HP are valid; identifiers such as Expert / HPool are not.
        return (at == 0 || !IsEnglishLetter(source[at - 1])) &&
            (at + value.Length == source.Length || !IsEnglishLetter(source[at + value.Length]));
    }

    private static bool IsEnglishLetter(char value) => value >= 'a' && value <= 'z' || value >= 'A' && value <= 'Z' || value == '_';

    private static int FindWord(string source, int word)
    {
        for (int at = 0; at < source.Length; at++) if (MatchesWord(source, at, word)) return at;
        return -1;
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
            asset.fallbackSpriteAssets = new List<TMP_SpriteAsset> { CreateExperienceSprites(shader), CreateAttributeAndHealthSprites(shader) };
            spriteAsset = asset;
            return spriteAsset;
        }
    }

    private static TMP_SpriteAsset CreateExperienceSprites(Shader shader)
    {
        if (experienceSprites != null) return experienceSprites;
        Sprite sprite = ExperienceIcon;
        if (sprite == null) throw new InvalidOperationException("Packaged experience resource icon is missing.");
        TMP_SpriteAsset asset = ScriptableObject.CreateInstance<TMP_SpriteAsset>();
        asset.name = "DiceLandExperienceKeyword";
        asset.hideFlags = HideFlags.DontSave;
        asset.spriteSheet = sprite.texture;
        asset.material = new Material(shader) { name = asset.name + " Material", mainTexture = sprite.texture, hideFlags = HideFlags.DontSave };
        asset.spriteInfoList = new List<TMP_Sprite>();
        asset.UpdateLookupTables();
        float width = 100f * sprite.rect.width / sprite.rect.height;
        TMP_SpriteGlyph glyph = new TMP_SpriteGlyph { index = 0, glyphRect = new GlyphRect(sprite.rect),
            metrics = new GlyphMetrics(width, 100f, 0f, 82f, width + 7f), scale = 1f, sprite = sprite };
        asset.spriteGlyphTable.Add(glyph);
        asset.spriteCharacterTable.Add(new TMP_SpriteCharacter(0xE103, glyph) { name = "DL_Experience", scale = 1.3f });
        asset.UpdateLookupTables();
        return experienceSprites = asset;
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
        if (experienceSprites != null) { DestroyOwned(experienceSprites.material); DestroyOwned(experienceSprites); }
        spriteAsset = null;
        experienceSprites = null;
        ResetAttributeAndHealthSprites();
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
