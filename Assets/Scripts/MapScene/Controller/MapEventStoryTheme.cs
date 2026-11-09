using TMPro;
using UnityEngine;

/// <summary>Dedicated reading fonts keep story presentation independent of the map's compact status typography.</summary>
public sealed class MapEventStoryTheme : ScriptableObject
{
    public TMP_FontAsset headingFont, bodyFont;
    public static MapEventStoryTheme Load() => Resources.Load<MapEventStoryTheme>("EventUI/Theme");
}
