using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class BattleButtonSkillSelectionTests
{
    private readonly List<Object> createdObjects = new List<Object>();

    [TearDown]
    public void TearDown()
    {
        for (int i = createdObjects.Count - 1; i >= 0; i--)
        {
            Object created = createdObjects[i];
            if (created != null)
                Object.DestroyImmediate(created);
        }

        createdObjects.Clear();
    }

    [Test]
    public void GetAllySkillUnavailableReason_ReturnsEnergyInsufficientWhenSkillCannotBeAffordedEvenWithoutConnections()
    {
        BattleButton battleButton = CreateBattleButton();
        BattleAllyController ally = CreateAlly("Tester");
        EnemySkill_SO costlySkill = CreateSkill("Costly Skill", 2);

        SetPrivateField(battleButton, "currentSharedEnergy", 1);
        SetPrivateField(battleButton, "reservedSharedEnergy", 0);

        string unavailableReason = battleButton.GetAllySkillUnavailableReason(ally, costlySkill);

        Assert.IsFalse(string.IsNullOrEmpty(unavailableReason));
        Assert.IsFalse(battleButton.CanSelectSkillForAlly(ally, costlySkill));
    }

    [Test]
    public void GetAllySkillUnavailableReason_AllowsSelectionWhenAtLeastOneUseCanBeAfforded()
    {
        BattleButton battleButton = CreateBattleButton();
        BattleAllyController ally = CreateAlly("Tester");
        EnemySkill_SO skill = CreateSkill("Affordable Skill", 2);

        SetPrivateField(battleButton, "currentSharedEnergy", 2);
        SetPrivateField(battleButton, "reservedSharedEnergy", 0);

        string unavailableReason = battleButton.GetAllySkillUnavailableReason(ally, skill);

        Assert.IsTrue(string.IsNullOrEmpty(unavailableReason));
        Assert.IsTrue(battleButton.CanSelectSkillForAlly(ally, skill));
    }

    private BattleButton CreateBattleButton()
    {
        GameObject go = new GameObject("BattleButton");
        createdObjects.Add(go);
        return go.AddComponent<BattleButton>();
    }

    private BattleAllyController CreateAlly(string name)
    {
        GameObject go = new GameObject(name);
        createdObjects.Add(go);

        BattleAllyController ally = go.AddComponent<BattleAllyController>();
        CharacterData_SO character = ScriptableObject.CreateInstance<CharacterData_SO>();
        character.characterName = name;
        character.baseHP = 10;
        character.minSpeed = 3;
        character.maxSpeed = 3;
        createdObjects.Add(character);

        ally.ApplyRuntimeState(new RunPartyMemberState
        {
            characterData = character,
            level = 1,
            maxHP = 10,
            currentHP = 10
        });
        ally.RegisterSpeedButtons(new SpeedButton[0]);
        return ally;
    }

    private EnemySkill_SO CreateSkill(string name, int energyCost)
    {
        EnemySkill_SO skill = ScriptableObject.CreateInstance<EnemySkill_SO>();
        skill.skillName = name;
        skill.energyCost = energyCost;
        skill.diceCount = 1;
        skill.dicePowerRanges = new[]
        {
            new EnemySkill_SO.DicePowerRange
            {
                minPower = 1,
                maxPower = 2
            }
        };
        createdObjects.Add(skill);
        return skill;
    }

    private static void SetPrivateField<TValue>(object target, string fieldName, TValue value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"Field {fieldName} was not found on {target.GetType().Name}.");
        field.SetValue(target, value);
    }
}
