using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore;

public static partial class ResourceKeywordText
{
    public const string HealthColor = "F09BA5";
    public const string FlameColor = "F4A261";
    public const string WaterColor = "79BFF2";
    public const string PoisonColor = "9BCB78";
    public const string BloodColor = "E98595";
    public const string DarkColor = "C3A0E5";
    public const string ElectricityColor = "F3DA78";
    public const string LightColor = "F6EAC8";
    public const string SpeedColor = "E9C17B";
    public const string NeutralColor = "C8CDD1";

    private sealed class Keyword
    {
        public readonly string Word, Color, IconName, Source;
        public readonly bool Attribute;
        public readonly bool Status;
        public Keyword(string word, string color, string iconName, string source, bool attribute = false, bool status = false)
        { Word = word; Color = color; IconName = iconName; Source = source; Attribute = attribute; Status = status; }
    }

    // Longer health aliases precede their shorter forms. Health is resolved before the single blood attribute.
    private static readonly Keyword[] ResourceKeywords =
    {
        new Keyword("金币", MoneyColor, "DL_Money", "Coins"),
        new Keyword("金钱", MoneyColor, "DL_Money", "Coins"),
        new Keyword("行动点", ActionPointColor, "DL_ActionPoints", "Footprint"),
        new Keyword("钥匙", KeyColor, "DL_Keys", "Key"),
        new Keyword("经验值", ExperienceColor, "DL_Experience", "ExperienceStar"),
        new Keyword("经验", ExperienceColor, "DL_Experience", "ExperienceStar"),
        new Keyword("EXP", ExperienceColor, "DL_Experience", "ExperienceStar"),
        new Keyword("最大生命值", HealthColor, "DL_Health", "Health"),
        new Keyword("当前生命值", HealthColor, "DL_Health", "Health"),
        new Keyword("剩余生命值", HealthColor, "DL_Health", "Health"),
        new Keyword("生命值上限", HealthColor, "DL_Health", "Health"),
        new Keyword("血量上限", HealthColor, "DL_Health", "Health"),
        new Keyword("生命上限", HealthColor, "DL_Health", "Health"),
        new Keyword("最大生命", HealthColor, "DL_Health", "Health"),
        new Keyword("当前生命", HealthColor, "DL_Health", "Health"),
        new Keyword("剩余血量", HealthColor, "DL_Health", "Health"),
        new Keyword("生命值", HealthColor, "DL_Health", "Health"),
        new Keyword("血量", HealthColor, "DL_Health", "Health"),
        new Keyword("生命", HealthColor, "DL_Health", "Health"),
        new Keyword("回血", HealthColor, "DL_Health", "Health"),
        new Keyword("掉血", HealthColor, "DL_Health", "Health"),
        new Keyword("扣血", HealthColor, "DL_Health", "Health"),
        new Keyword("满血", HealthColor, "DL_Health", "Health"),
        new Keyword("残血", HealthColor, "DL_Health", "Health"),
        new Keyword("HP", HealthColor, "DL_Health", "Health"),
        new Keyword("炎", FlameColor, "DL_Attribute_Flame", "Flame", true),
        new Keyword("水", WaterColor, "DL_Attribute_Water", "Water", true),
        new Keyword("毒", PoisonColor, "DL_Attribute_Poison", "Poison", true),
        new Keyword("血", BloodColor, "DL_Attribute_Blood", "Blood", true),
        new Keyword("暗", DarkColor, "DL_Attribute_Dark", "Dark", true),
        new Keyword("电", ElectricityColor, "DL_Attribute_Electricity", "Electricity", true),
        new Keyword("光", LightColor, "DL_Attribute_Light", "Light", true),
        new Keyword("速", SpeedColor, "DL_Attribute_Speed", "Speed", true),
        new Keyword("无", NeutralColor, "DL_Attribute_Neutral", "Neutral", true)
    };

    private static readonly string[] AttributeAndHealthSources =
        { "Flame", "Water", "Poison", "Blood", "Dark", "Electricity", "Light", "Speed", "Neutral", "Health" };
    private static readonly string[] AttributeSuffixes = { "属性", "系", "伤害", "抗性", "元素" };
    private static readonly Dictionary<string, Sprite> keywordIcons = new Dictionary<string, Sprite>();
    private static TMP_SpriteAsset attributeAndHealthSprites;

    public static Sprite HealthIcon => KeywordIcon("Health");

    private static Sprite IconFor(Keyword word)
    {
        if (word.Status) return StatusIcon(word.Source);
        if (word.Source == "ExperienceStar") return ExperienceIcon;
        if (word.Attribute || word.Source == "Health") return KeywordIcon(word.Source);
        return MapForestArt.Get(word.Source);
    }

    private static Sprite KeywordIcon(string source)
    {
        if (keywordIcons.Count == 0)
            foreach (Sprite sprite in Resources.LoadAll<Sprite>("MapUI/KeywordIcons")) keywordIcons[sprite.name] = sprite;
        return keywordIcons.TryGetValue(source, out Sprite value) ? value : null;
    }

    private static bool IsAttributeMention(string source, int at)
    {
        int after = NextVisibleIndex(source, at + 1);
        // Explicit attribute phrases remain recognisable inside a Chinese sentence.
        foreach (string suffix in AttributeSuffixes)
            if (after + suffix.Length <= source.Length && string.CompareOrdinal(source, after, suffix, 0, suffix.Length) == 0) return true;
        int before = PreviousVisibleIndex(source, at - 1);
        // Single-character attribute names are tokens, not substrings of 无法 / 速度 / 水壶 / 流血 / 中毒.
        return (before < 0 || !IsWordCharacter(source[before])) &&
            (after >= source.Length || !IsWordCharacter(source[after]));
    }

    private static bool IsWordCharacter(char value) => char.IsLetterOrDigit(value) || value == '_';

    private static int NextVisibleIndex(string source, int at)
    {
        while (at < source.Length && source[at] == '<' && TryTagEnd(source, at, out int end)) at = end + 1;
        return at;
    }

    private static int PreviousVisibleIndex(string source, int at)
    {
        while (at >= 0 && source[at] == '>')
        {
            int start = source.LastIndexOf('<', at);
            if (start < 0 || !TryTagEnd(source, start, out int end) || end != at) break;
            at = start - 1;
        }
        return at;
    }

    private static TMP_SpriteAsset CreateAttributeAndHealthSprites(Shader shader)
    {
        if (attributeAndHealthSprites != null) return attributeAndHealthSprites;
        Sprite first = KeywordIcon(AttributeAndHealthSources[0]);
        if (first == null) throw new InvalidOperationException("Packaged attribute keyword icons are missing.");
        TMP_SpriteAsset asset = ScriptableObject.CreateInstance<TMP_SpriteAsset>();
        asset.name = "DiceLandAttributeAndHealthKeywords";
        asset.hideFlags = HideFlags.DontSave;
        asset.spriteSheet = first.texture;
        asset.material = new Material(shader) { name = asset.name + " Material", mainTexture = first.texture, hideFlags = HideFlags.DontSave };
        asset.spriteInfoList = new List<TMP_Sprite>();
        asset.UpdateLookupTables();
        for (int i = 0; i < AttributeAndHealthSources.Length; i++)
        {
            string source = AttributeAndHealthSources[i];
            Sprite sprite = KeywordIcon(source);
            if (sprite == null || sprite.texture != first.texture) throw new InvalidOperationException("Attribute keyword atlas is inconsistent: " + source);
            float width = 100f * sprite.rect.width / sprite.rect.height;
            TMP_SpriteGlyph glyph = new TMP_SpriteGlyph { index = (uint)i, glyphRect = new GlyphRect(sprite.rect),
                metrics = new GlyphMetrics(width, 100f, 0f, 82f, width + 7f), scale = 1f, sprite = sprite };
            asset.spriteGlyphTable.Add(glyph);
            asset.spriteCharacterTable.Add(new TMP_SpriteCharacter((uint)(0xE110 + i), glyph)
                { name = source == "Health" ? "DL_Health" : "DL_Attribute_" + source, scale = 1f });
        }
        asset.UpdateLookupTables();
        return attributeAndHealthSprites = asset;
    }

    private static void ResetAttributeAndHealthSprites()
    {
        if (attributeAndHealthSprites != null)
        { DestroyOwned(attributeAndHealthSprites.material); DestroyOwned(attributeAndHealthSprites); }
        attributeAndHealthSprites = null;
        keywordIcons.Clear();
    }
}
