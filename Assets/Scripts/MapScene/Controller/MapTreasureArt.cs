using TMPro;
using UnityEngine;

/// <summary>Presentation assets only. Reward identities, amounts and rolls come from TreasureRewardService.</summary>
public sealed class MapTreasureArt : ScriptableObject
{
    public Sprite panelFrame, card, header, button, coins;
    public Sprite[] relicBags = new Sprite[5];
    public TMP_FontAsset headingFont, bodyFont;
    public static MapTreasureArt Load() => Resources.Load<MapTreasureArt>("TreasureUI/Theme");
}
