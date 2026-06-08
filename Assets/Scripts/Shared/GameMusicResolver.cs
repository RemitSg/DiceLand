using UnityEngine;

public static class GameMusicResolver
{
    public static AudioClip ResolveBattleMusic(GameMusicConfig_SO config, BattleEncounterData encounter)
    {
        AudioClip groupOverride = encounter != null && encounter.enemyGroup != null
            ? encounter.enemyGroup.battleMusicOverride
            : null;
        if (groupOverride != null)
            return groupOverride;

        if (config == null || encounter == null)
            return null;

        return config.GetBattleFallback(encounter.tileType, encounter.dangerBand);
    }

    public static AudioClip ResolveMapMusic(GameMusicConfig_SO config)
    {
        return config != null ? config.mapMusic : null;
    }
}
