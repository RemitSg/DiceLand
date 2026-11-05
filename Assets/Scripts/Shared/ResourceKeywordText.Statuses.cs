using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore;

/// <summary>Full battle-status names share the same sprite/text pipeline as coins; asset descriptions stay raw.</summary>
public static partial class ResourceKeywordText
{
    private static Keyword[] combinedKeywords;
    private static readonly Dictionary<string, Sprite> statusIcons = new Dictionary<string, Sprite>(StringComparer.Ordinal);
    private static TMP_SpriteAsset statusSprites;
    private static Keyword[] Keywords => combinedKeywords ?? (combinedKeywords = BuildCombinedKeywords());

    private static Keyword[] BuildCombinedKeywords()
    {
        var words = new List<Keyword>(ResourceKeywords);
        words.Add(new Keyword("流血", "E98595", "DL_Status_Bleed", "Bleed", status: true));
        words.Add(new Keyword("燃烧", "F4A261", "DL_Status_Burn", "Burn", status: true));
        words.Add(new Keyword("寒冷", "9CD8ED", "DL_Status_Chill", "Chill", status: true));
        words.Add(new Keyword("电流", "F3DA78", "DL_Status_Current", "Current", status: true));
        words.Add(new Keyword("黑暗", "C3A0E5", "DL_Status_Darkness", "Darkness", status: true));
        words.Add(new Keyword("负债", "D9B987", "DL_Status_Debt", "Debt", status: true));
        words.Add(new Keyword("蓄能", "F2CB6F", "DL_Status_Energy", "Energy", status: true));
        words.Add(new Keyword("水蚀", "79BFF2", "DL_Status_Erode", "Erode", status: true));
        words.Add(new Keyword("重防", "9EDBBB", "DL_Status_Guard", "Guard", status: true));
        words.Add(new Keyword("破防", "E8AF8B", "DL_Status_Guardbreak", "Guardbreak", status: true));
        words.Add(new Keyword("免疫", "F6EAC8", "DL_Status_Immune", "Immune", status: true));
        words.Add(new Keyword("孵化", "B6BE9A", "DL_Status_Incubation", "Incubation", status: true));
        words.Add(new Keyword("幸运", "A7D48C", "DL_Status_Luck", "Luck", status: true));
        words.Add(new Keyword("厄运", "C4A0DA", "DL_Status_Misfortune", "Misfortune", status: true));
        words.Add(new Keyword("中毒", "9BCB78", "DL_Status_Poison", "Poison", status: true));
        words.Add(new Keyword("背面", "A6BCE7", "DL_Status_PrismBack", "PrismBack", status: true));
        words.Add(new Keyword("照面", "EDE0B0", "DL_Status_PrismFront", "PrismFront", status: true));
        words.Add(new Keyword("折光印", "D7C5FA", "DL_Status_RefractedMark", "RefractedMark", status: true));
        words.Add(new Keyword("抵抗", "ABDBC5", "DL_Status_Resistance", "Resistance", status: true));
        words.Add(new Keyword("护盾", "A4D8E8", "DL_Status_Shield", "Shield", status: true));
        words.Add(new Keyword("缓慢", "B7BA96", "DL_Status_Slowness", "Slowness", status: true));
        words.Add(new Keyword("强壮", "EAC17B", "DL_Status_Strength", "Strength", status: true));
        words.Add(new Keyword("眩晕", "E6D88A", "DL_Status_Stun", "Stun", status: true));
        words.Add(new Keyword("急速", "DAD291", "DL_Status_Swiftness", "Swiftness", status: true));
        words.Add(new Keyword("标记", "F0B08E", "DL_Status_Target", "Target", status: true));
        words.Add(new Keyword("易伤", "F0AAA3", "DL_Status_Vulnerable", "Vulnerable", status: true));
        words.Add(new Keyword("虚弱", "C3AEC3", "DL_Status_Weakness", "Weakness", status: true));
        words.Add(new Keyword("临时强壮", "EAC17B", "DL_Status_Strength", "Strength", status: true));
        words.Add(new Keyword("临时重防", "9EDBBB", "DL_Status_Guard", "Guard", status: true));
        words.Add(new Keyword("临时破防", "E8AF8B", "DL_Status_Guardbreak", "Guardbreak", status: true));
        words.Add(new Keyword("临时虚弱", "C3AEC3", "DL_Status_Weakness", "Weakness", status: true));
        words.Add(new Keyword("临时标记", "F0B08E", "DL_Status_Target", "Target", status: true));
        // Full temporary-state names win over their shorter base names, and over single-character attributes.
        words.Sort((left, right) => right.Word.Length.CompareTo(left.Word.Length));
        return words.ToArray();
    }

    private static Sprite StatusIcon(string key)
    {
        if (statusIcons.Count == 0)
            foreach (Sprite sprite in Resources.LoadAll<Sprite>("StatusUI/StatusIcons")) statusIcons[sprite.name] = sprite;
        return statusIcons.TryGetValue(key, out Sprite spriteValue) ? spriteValue : null;
    }

    private static TMP_SpriteAsset CreateStatusSprites(Shader shader)
    {
        if (statusSprites != null) return statusSprites;
        Sprite[] sprites = Resources.LoadAll<Sprite>("StatusUI/StatusIcons");
        if (sprites.Length == 0) return null;
        Array.Sort(sprites, (a, b) => string.CompareOrdinal(a.name, b.name));
        Texture2D atlas = sprites[0].texture;
        TMP_SpriteAsset asset = ScriptableObject.CreateInstance<TMP_SpriteAsset>();
        asset.name = "DiceLandBattleStatusKeywords"; asset.hideFlags = HideFlags.DontSave;
        asset.spriteSheet = atlas;
        asset.material = new Material(shader) { name = asset.name + " Material", mainTexture = atlas, hideFlags = HideFlags.DontSave };
        asset.spriteInfoList = new List<TMP_Sprite>(); asset.UpdateLookupTables();
        for (int i = 0; i < sprites.Length; i++)
        {
            Sprite sprite = sprites[i];
            if (sprite.texture != atlas) throw new InvalidOperationException("Status keyword atlas is inconsistent: " + sprite.name);
            TMP_SpriteGlyph glyph = new TMP_SpriteGlyph { index = (uint)i, glyphRect = new GlyphRect(sprite.rect),
                metrics = new GlyphMetrics(100, 100, 0, 82, 107), scale = 1, sprite = sprite };
            asset.spriteGlyphTable.Add(glyph);
            asset.spriteCharacterTable.Add(new TMP_SpriteCharacter((uint)(0xE200 + i), glyph) { name = "DL_Status_" + sprite.name, scale = 1.08f });
        }
        asset.UpdateLookupTables(); return statusSprites = asset;
    }

    private static void ResetStatusSprites()
    {
        if (statusSprites != null) { DestroyOwned(statusSprites.material); DestroyOwned(statusSprites); }
        statusSprites = null; statusIcons.Clear();
    }
}
