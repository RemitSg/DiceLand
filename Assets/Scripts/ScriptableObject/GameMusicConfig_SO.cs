using UnityEngine;

[CreateAssetMenu(fileName = "GameMusicConfig", menuName = "Audio/Game Music Config")]
public class GameMusicConfig_SO : ScriptableObject
{
    [Header("Map")]
    public AudioClip mapMusic;

    [Header("Battle - Outer")]
    public AudioClip outerStandardBattleMusic;
    public AudioClip outerEliteBattleMusic;

    [Header("Battle - Mid")]
    public AudioClip midStandardBattleMusic;
    public AudioClip midEliteBattleMusic;

    [Header("Battle - Inner")]
    public AudioClip innerStandardBattleMusic;
    public AudioClip innerEliteBattleMusic;

    [Header("Battle - Core")]
    public AudioClip coreStandardBattleMusic;
    public AudioClip coreEliteBattleMusic;

    [Header("Battle - Boss")]
    public AudioClip bossBattleMusic;

    public AudioClip GetBattleFallback(MapTileType tileType, MapDangerBand dangerBand)
    {
        if (tileType == MapTileType.Boss)
            return bossBattleMusic;

        bool isElite = tileType == MapTileType.Elite;
        switch (dangerBand)
        {
            case MapDangerBand.Outer:
                return isElite ? outerEliteBattleMusic : outerStandardBattleMusic;
            case MapDangerBand.Mid:
                return isElite ? midEliteBattleMusic : midStandardBattleMusic;
            case MapDangerBand.Inner:
                return isElite ? innerEliteBattleMusic : innerStandardBattleMusic;
            case MapDangerBand.Core:
            default:
                return isElite ? coreEliteBattleMusic : coreStandardBattleMusic;
        }
    }
}
