using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class BattleSettlementPartyMemberFactoryTests
{
    private readonly List<GameObject> createdObjects = new List<GameObject>();

    [TearDown]
    public void TearDown()
    {
        for (int i = 0; i < createdObjects.Count; i++)
        {
            if (createdObjects[i] != null)
                Object.DestroyImmediate(createdObjects[i]);
        }

        createdObjects.Clear();
    }

    [Test]
    public void BuildRunState_UsesDisplayNameFallback()
    {
        RunPartyMemberState state = BattleSettlementPartyMemberFactory.BuildRunState(
            displayName: "",
            fallbackName: "Fallback",
            characterData: null,
            weaponData: null,
            level: 2,
            experience: 3,
            maxHP: 10,
            currentHP: 4);

        Assert.AreEqual("Fallback", state.memberId);
        Assert.AreEqual(2, state.level);
        Assert.AreEqual(4, state.currentHP);
    }

    [Test]
    public void BuildSettlementEntry_MapsInitialAndResultingValues()
    {
        BattleSettlementEntry entry = BattleSettlementPartyMemberFactory.BuildSettlementEntry(
            displayName: "A",
            initialCurrentHP: 1,
            initialMaxHP: 2,
            initialLevel: 3,
            isAlive: true,
            initialExperience: 4,
            maxExperience: 5,
            gainedExperience: 6,
            finalCurrentHP: 7,
            finalMaxHP: 8,
            finalLevel: 9,
            finalExperience: 10);

        Assert.AreEqual("A", entry.displayName);
        Assert.AreEqual(1, entry.currentHP);
        Assert.AreEqual(7, entry.resultingHP);
        Assert.AreEqual(10, entry.resultingExperience);
    }

    [Test]
    public void BuildRunStatesAndSettlementEntries_MapSharedProgressionList()
    {
        List<BattleSettlementPartyProgression> progressions = new List<BattleSettlementPartyProgression>
        {
            new BattleSettlementPartyProgression
            {
                DisplayName = "",
                FallbackName = "Fallback",
                InitialCurrentHP = 3,
                InitialMaxHP = 8,
                InitialLevel = 1,
                InitialExperience = 4,
                MaxExperience = 10,
                IsAlive = true,
                FinalCurrentHP = 5,
                FinalMaxHP = 9,
                FinalLevel = 2,
                FinalExperience = 1
            }
        };

        List<RunPartyMemberState> runStates = BattleSettlementPartyMemberFactory.BuildRunStates(progressions);
        List<BattleSettlementEntry> settlementEntries = BattleSettlementPartyMemberFactory.BuildSettlementEntries(progressions, gainedExperience: 6);

        Assert.AreEqual(1, runStates.Count);
        Assert.AreEqual("Fallback", runStates[0].memberId);
        Assert.AreEqual(2, runStates[0].level);
        Assert.AreEqual(5, runStates[0].currentHP);
        Assert.AreEqual(1, settlementEntries.Count);
        Assert.AreEqual(4, settlementEntries[0].currentExperience);
        Assert.AreEqual(6, settlementEntries[0].experienceGained);
        Assert.AreEqual(1, settlementEntries[0].resultingExperience);
    }

    [Test]
    public void BuildProgressions_MapsBattleAllyRuntimeStateAndAppliesExperience()
    {
        GameObject go = new GameObject("Ally Host");
        createdObjects.Add(go);
        BattleAllyController ally = go.AddComponent<BattleAllyController>();
        ally.ApplyRuntimeState(new RunPartyMemberState
        {
            memberId = "Ally One",
            level = 1,
            experience = 8,
            maxHP = 10,
            currentHP = 4
        });

        List<BattleSettlementPartyProgression> progressions = BattleSettlementPartyMemberFactory.BuildProgressions(
            new[] { ally },
            experienceReward: 5,
            resolveExperienceToNextLevel: _ => 10,
            resolveMaxHPForLevel: (_, level, fallbackMaxHP) => fallbackMaxHP + level);

        Assert.AreEqual(1, progressions.Count);
        Assert.AreEqual(1, progressions[0].InitialLevel);
        Assert.AreEqual(8, progressions[0].InitialExperience);
        Assert.AreEqual(2, progressions[0].FinalLevel);
        Assert.AreEqual(3, progressions[0].FinalExperience);
        Assert.AreEqual(12, progressions[0].FinalMaxHP);
    }

    [Test]
    public void BuildProgressions_UsesCharacterNameWithoutWeaponForSettlementDisplay()
    {
        GameObject go = new GameObject("Ally Host");
        createdObjects.Add(go);

        BattleAllyController ally = go.AddComponent<BattleAllyController>();
        CharacterData_SO character = ScriptableObject.CreateInstance<CharacterData_SO>();
        character.characterName = "新手木剑";
        Weapon_SO weapon = ScriptableObject.CreateInstance<Weapon_SO>();
        weapon.weaponName = "曦誓剑";

        ally.ApplyRuntimeState(new RunPartyMemberState
        {
            memberId = "Ally One",
            characterData = character,
            weaponData = weapon,
            level = 1,
            experience = 0,
            maxHP = 10,
            currentHP = 10
        });

        List<BattleSettlementPartyProgression> progressions = BattleSettlementPartyMemberFactory.BuildProgressions(
            new[] { ally },
            experienceReward: 0,
            resolveExperienceToNextLevel: _ => 10,
            resolveMaxHPForLevel: (_, _, fallbackMaxHP) => fallbackMaxHP);

        Assert.AreEqual("新手木剑", progressions[0].DisplayName);
        StringAssert.DoesNotContain("[", progressions[0].DisplayName);
        Object.DestroyImmediate(character);
        Object.DestroyImmediate(weapon);
    }
}
