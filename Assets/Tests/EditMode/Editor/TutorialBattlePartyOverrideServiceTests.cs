using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class TutorialBattlePartyOverrideServiceTests
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
    public void BuildEncounter_TutorialBattle02UsesConfiguredMollyVisualAsset()
    {
        EnemyData_SO enemy = ScriptableObject.CreateInstance<EnemyData_SO>();
        EnemyGroup_SO group = ScriptableObject.CreateInstance<EnemyGroup_SO>();
        CharacterData_SO molly = ScriptableObject.CreateInstance<CharacterData_SO>();
        GameObject mollyPrefab = new GameObject("Molly Visual");
        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, 2f, 2f), new Vector2(0.5f, 0.5f), 1f);
        createdObjects.Add(enemy);
        createdObjects.Add(group);
        createdObjects.Add(molly);
        createdObjects.Add(mollyPrefab);
        createdObjects.Add(sprite);
        createdObjects.Add(texture);

        group.groupName = "Tutorial Group";
        group.enemies = new List<EnemyData_SO> { enemy };
        molly.characterName = "Molly";
        molly.baseHP = 16;
        molly.minSpeed = 3;
        molly.maxSpeed = 6;
        molly.battleSprite = sprite;
        molly.characterPrefab = mollyPrefab;

        BattleEncounterData encounter = MapBattleEncounterService.BuildEncounter(
            new MapBattleEncounterBuildRequest
            {
                runState = new RunState
                {
                    partyMembers = new List<RunPartyMemberState>
                    {
                        new RunPartyMemberState { memberId = "Chara", maxHP = 24, currentHP = 24 }
                    }
                },
                tile = new BoardTileData { coord = new Vector2Int(2, 3), tileType = MapTileType.Battle, encounterSeed = 26002 },
                encounterGroup = group,
                battleSceneName = "Battle",
                isTutorialEncounter = true,
                tutorialBattleStageId = TutorialBattleStageId.Battle02,
                tutorialBattle02MollyGuestCharacter = molly
            });

        Assert.IsNotNull(encounter);
        Assert.AreSame(sprite, encounter.partyMembers[0].BattleSprite);
        Assert.AreSame(mollyPrefab, encounter.partyMembers[0].characterData.characterPrefab);
        Assert.AreEqual(1, encounter.partyMembers[0].characterData.minSpeed);
        Assert.AreEqual(1, encounter.partyMembers[0].characterData.maxSpeed);
        Assert.AreEqual(1, encounter.partyMembers[0].currentHP);
    }
}
