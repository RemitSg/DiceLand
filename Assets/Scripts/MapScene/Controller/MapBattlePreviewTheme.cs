using TMPro;
using UnityEngine;

/// <summary>Reuses the project's existing display faces without copying font atlases into Resources.</summary>
[CreateAssetMenu(menuName = "UI/Map Battle Preview Theme")]
public sealed class MapBattlePreviewTheme : ScriptableObject
{
    public TMP_FontAsset titleFont;
    public TMP_FontAsset actionFont;
}
