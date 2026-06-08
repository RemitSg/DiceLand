using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class GameMusicResolverTests
{
    private readonly List<Object> createdObjects = new List<Object>();

    [TearDown]
    public void TearDown()
    {
        for (int i = createdObjects.Count - 1; i >= 0; i--)
        {
            if (createdObjects[i] != null)
                Object.DestroyImmediate(createdObjects[i]);
        }

        createdObjects.Clear();
    }

    [Test]
    public void ResolveBattleMusic_UsesEnemyGroupOverrideBeforeFallback()
    {
        AudioClip overrideClip = CreateClip("Override");
        AudioClip fallbackClip = CreateClip("Outer Standard");
        GameMusicConfig_SO config = CreateConfig();
        config.outerStandardBattleMusic = fallbackClip;

        EnemyGroup_SO group = CreateGroup();
        group.battleMusicOverride = overrideClip;

        BattleEncounterData encounter = new BattleEncounterData
        {
            enemyGroup = group,
            tileType = MapTileType.Battle,
            dangerBand = MapDangerBand.Outer
        };

        AudioClip resolved = GameMusicResolver.ResolveBattleMusic(config, encounter);

        Assert.AreSame(overrideClip, resolved);
    }

    [Test]
    public void ResolveBattleMusic_UsesBossFallbackWhenGroupHasNoOverride()
    {
        AudioClip bossClip = CreateClip("Boss");
        GameMusicConfig_SO config = CreateConfig();
        config.outerStandardBattleMusic = CreateClip("Outer Standard");
        config.bossBattleMusic = bossClip;

        BattleEncounterData encounter = new BattleEncounterData
        {
            enemyGroup = CreateGroup(),
            tileType = MapTileType.Boss,
            dangerBand = MapDangerBand.Core
        };

        AudioClip resolved = GameMusicResolver.ResolveBattleMusic(config, encounter);

        Assert.AreSame(bossClip, resolved);
    }

    [Test]
    public void ResolveBattleMusic_UsesDangerAndEliteFallback()
    {
        AudioClip midEliteClip = CreateClip("Mid Elite");
        GameMusicConfig_SO config = CreateConfig();
        config.midEliteBattleMusic = midEliteClip;

        BattleEncounterData encounter = new BattleEncounterData
        {
            enemyGroup = CreateGroup(),
            tileType = MapTileType.Elite,
            dangerBand = MapDangerBand.Mid
        };

        AudioClip resolved = GameMusicResolver.ResolveBattleMusic(config, encounter);

        Assert.AreSame(midEliteClip, resolved);
    }

    [Test]
    public void ResolveBattleMusic_UsesCoreStandardFallback()
    {
        AudioClip coreStandardClip = CreateClip("Core Standard");
        GameMusicConfig_SO config = CreateConfig();
        config.coreStandardBattleMusic = coreStandardClip;

        BattleEncounterData encounter = new BattleEncounterData
        {
            enemyGroup = CreateGroup(),
            tileType = MapTileType.Battle,
            dangerBand = MapDangerBand.Core
        };

        AudioClip resolved = GameMusicResolver.ResolveBattleMusic(config, encounter);

        Assert.AreSame(coreStandardClip, resolved);
    }

    private AudioClip CreateClip(string clipName)
    {
        AudioClip clip = AudioClip.Create(clipName, 4410, 1, 44100, false);
        createdObjects.Add(clip);
        return clip;
    }

    private GameMusicConfig_SO CreateConfig()
    {
        GameMusicConfig_SO config = ScriptableObject.CreateInstance<GameMusicConfig_SO>();
        createdObjects.Add(config);
        return config;
    }

    private EnemyGroup_SO CreateGroup()
    {
        EnemyGroup_SO group = ScriptableObject.CreateInstance<EnemyGroup_SO>();
        createdObjects.Add(group);
        return group;
    }
}
