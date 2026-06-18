using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class BattleAllyMaxHealthBonusTests
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
    public void IncreaseMaxHealthAndHeal_AddsMaxHealthAndRestoresSameAmount()
    {
        BattleAllyController ally = CreateAlly("Ally", maxHp: 20, currentHp: 12);

        int applied = ally.IncreaseMaxHealthAndHeal(5);

        Assert.AreEqual(5, applied);
        Assert.AreEqual(25, ally.MaxHP);
        Assert.AreEqual(17, ally.CurrentHP);
    }

    private BattleAllyController CreateAlly(string name, int maxHp, int currentHp)
    {
        GameObject go = new GameObject(name);
        createdObjects.Add(go);

        CharacterData_SO character = ScriptableObject.CreateInstance<CharacterData_SO>();
        character.characterName = name;
        character.baseHP = maxHp;
        createdObjects.Add(character);

        BattleAllyController ally = go.AddComponent<BattleAllyController>();
        ally.ApplyRuntimeState(new RunPartyMemberState
        {
            characterData = character,
            maxHP = maxHp,
            currentHP = currentHp
        });
        return ally;
    }
}
