using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

public class MapEventEncounterServiceTests
{
    private EnemyData_SO enemyA;
    private EnemyData_SO enemyB;
    private EnemyData_SO enemyC;
    private EnemyGroup_SO enemyGroup;
    private CharacterData_SO character;
    private Relic_SO relic;
    private Calamity_SO calamity;

    [TearDown]
    public void TearDown()
    {
        DestroyIfNotNull(enemyA);
        DestroyIfNotNull(enemyB);
        DestroyIfNotNull(enemyC);
        DestroyIfNotNull(enemyGroup);
        DestroyIfNotNull(character);
        DestroyIfNotNull(relic);
        DestroyIfNotNull(calamity);
    }

    [Test]
    public void TryPrepareEncounter_FromEnemyGroupBuildsBattleEncounterData()
    {
        enemyA = CreateEnemy("Watcher");
        enemyGroup = ScriptableObject.CreateInstance<EnemyGroup_SO>();
        enemyGroup.groupName = "Gate";
        enemyGroup.enemies.Add(enemyA);
        character = ScriptableObject.CreateInstance<CharacterData_SO>();
        character.characterName = "Hero";
        relic = ScriptableObject.CreateInstance<Relic_SO>();
        relic.relicName = "Charm";
        calamity = ScriptableObject.CreateInstance<Calamity_SO>();
        calamity.calamityName = "Storm";
        RunState runState = new RunState
        {
            currentCycle = 2,
            playerGridPosition = new Vector2Int(1, 1)
        };
        runState.partyMembers.Add(new RunPartyMemberState
        {
            memberId = "hero",
            characterData = character,
            maxHP = 10,
            currentHP = 7
        });
        runState.partyMembers.Add(new RunPartyMemberState { memberId = "fallen", maxHP = 8, currentHP = 0 });
        runState.relicInventory.Add(new RunRelicState { relicData = relic, amount = 1 });
        runState.activeCalamities.Add(new RunCalamityState { calamityData = calamity, acquiredAtTier = 3 });
        BoardTileData tile = new BoardTileData
        {
            tileType = MapTileType.Event,
            dangerBand = MapDangerBand.Inner,
            coord = new Vector2Int(4, 5),
            encounterSeed = 99
        };
        MapEventBuiltInEffectData effect = new MapEventBuiltInEffectData
        {
            encounterSelectionMode = MapEventEncounterSelectionMode.RandomEnemyGroup,
            encounterEnemyGroups = { enemyGroup }
        };

        bool result = MapEventEncounterService.TryPrepareEncounter(
            runState,
            tile,
            effect,
            "MapScene",
            "BattleScene",
            1234,
            out BattleEncounterData encounter,
            out string message);

        Assert.IsTrue(result);
        Assert.AreEqual("将触发遭遇战：Gate", message);
        Assert.AreEqual("MapScene", encounter.sourceSceneName);
        Assert.AreEqual("BattleScene", encounter.targetSceneName);
        Assert.AreEqual("事件遭遇：Gate", encounter.encounterLabel);
        Assert.AreEqual(MapTileType.Event, encounter.tileType);
        Assert.AreEqual(MapDangerBand.Inner, encounter.dangerBand);
        Assert.AreEqual(new Vector2Int(4, 5), encounter.sourceCoord);
        Assert.AreSame(enemyGroup, encounter.enemyGroup);
        Assert.AreEqual(1, encounter.partyMembers.Count);
        Assert.AreEqual("hero", encounter.partyMembers[0].memberId);
        Assert.AreNotSame(runState.partyMembers[0], encounter.partyMembers[0]);
        Assert.AreEqual(1, encounter.relicInventory.Count);
        Assert.AreNotSame(runState.relicInventory[0], encounter.relicInventory[0]);
        Assert.AreEqual(1, encounter.activeCalamities.Count);
        Assert.AreNotSame(runState.activeCalamities[0], encounter.activeCalamities[0]);
    }

    [Test]
    public void TryPrepareEncounter_FromEnemyListCreatesRuntimeGroup()
    {
        enemyA = CreateEnemy("A");
        enemyB = CreateEnemy("B");
        enemyC = CreateEnemy("C");
        RunState runState = new RunState();
        runState.partyMembers.Add(new RunPartyMemberState { memberId = "hero", maxHP = 10, currentHP = 10 });
        MapEventBuiltInEffectData effect = new MapEventBuiltInEffectData
        {
            encounterSelectionMode = MapEventEncounterSelectionMode.RandomEnemies,
            encounterEnemyCountMin = 2,
            encounterEnemyCountMax = 2,
            encounterEnemies = { enemyA, enemyB, enemyC }
        };

        bool result = MapEventEncounterService.TryPrepareEncounter(
            runState,
            null,
            effect,
            "MapScene",
            "BattleScene",
            0,
            out BattleEncounterData encounter,
            out string message);

        Assert.IsTrue(result);
        Assert.AreEqual("将触发遭遇战：事件遭遇", message);
        Assert.IsNotNull(encounter.enemyGroup);
        Assert.AreEqual("事件遭遇", encounter.enemyGroup.groupName);
        Assert.AreEqual(2, encounter.enemyGroup.Count);
        Object.DestroyImmediate(encounter.enemyGroup);
    }

    [Test]
    public void TryPrepareEncounter_MissingValidEnemiesFails()
    {
        RunState runState = new RunState();
        MapEventBuiltInEffectData effect = new MapEventBuiltInEffectData
        {
            encounterSelectionMode = MapEventEncounterSelectionMode.RandomEnemyGroup
        };

        bool result = MapEventEncounterService.TryPrepareEncounter(
            runState,
            null,
            effect,
            "MapScene",
            "BattleScene",
            0,
            out BattleEncounterData encounter,
            out string message);

        Assert.IsFalse(result);
        Assert.IsNull(encounter);
        Assert.AreEqual("该事件未配置有效的遭遇敌人。", message);
    }

    private static EnemyData_SO CreateEnemy(string enemyName)
    {
        EnemyData_SO enemy = ScriptableObject.CreateInstance<EnemyData_SO>();
        enemy.enemyName = enemyName;
        enemy.baseHP = 10;
        return enemy;
    }

    private static void DestroyIfNotNull(Object target)
    {
        if (target != null)
            Object.DestroyImmediate(target);
    }
}
