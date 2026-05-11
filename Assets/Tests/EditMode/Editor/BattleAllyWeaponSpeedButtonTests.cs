using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class BattleAllyWeaponSpeedButtonTests
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
    public void EquippedWeaponSpeedButtonCount_SetsAllyBaseSpeedButtonCount()
    {
        BattleAllyController ally = CreateAllyWithWeaponSpeedButtons(3);

        Assert.AreEqual(3, ally.BaseSpeedButtonCount);
        Assert.AreEqual(3, ally.EffectiveSpeedButtonCount);
        Assert.AreEqual(3, ally.SpeedButtons.Count);
    }

    [Test]
    public void WeaponSpeedButtonCount_DefaultsToOne()
    {
        Weapon_SO weapon = ScriptableObject.CreateInstance<Weapon_SO>();
        createdObjects.Add(weapon);

        Assert.AreEqual(1, weapon.speedButtonCount);
    }

    [Test]
    public void StatusPenalty_ReducesWeaponGrantedSpeedButtons()
    {
        BattleAllyController ally = CreateAllyWithWeaponSpeedButtons(3);
        BattleStatus_SO stun = CreateSpeedButtonPenaltyStatus();

        ally.AddStatus(stun, stun.StatusId, stun.DisplayName, 2);

        Assert.AreEqual(3, ally.BaseSpeedButtonCount);
        Assert.AreEqual(1, ally.EffectiveSpeedButtonCount);
        Assert.AreEqual(1, ally.SpeedButtons.Count);
    }

    private BattleAllyController CreateAllyWithWeaponSpeedButtons(int speedButtonCount)
    {
        GameObject go = new GameObject("Weapon Speed Ally");
        createdObjects.Add(go);

        Weapon_SO weapon = ScriptableObject.CreateInstance<Weapon_SO>();
        weapon.weaponName = "Multi Button Weapon";
        weapon.speedButtonCount = speedButtonCount;
        createdObjects.Add(weapon);

        CharacterData_SO character = ScriptableObject.CreateInstance<CharacterData_SO>();
        character.characterName = "Tester";
        character.baseHP = 10;
        character.minSpeed = 3;
        character.maxSpeed = 3;
        createdObjects.Add(character);

        BattleAllyController ally = go.AddComponent<BattleAllyController>();
        ally.ApplyRuntimeState(new RunPartyMemberState
        {
            characterData = character,
            weaponData = weapon,
            level = 1,
            maxHP = 10,
            currentHP = 10
        });

        ally.RegisterSpeedButtons(new[]
        {
            CreateSpeedButton(go.transform, "SpeedButton_1")
        });
        return ally;
    }

    private SpeedButton CreateSpeedButton(Transform parent, string name)
    {
        GameObject buttonObject = new GameObject(name);
        buttonObject.transform.SetParent(parent, false);
        createdObjects.Add(buttonObject);
        return buttonObject.AddComponent<SpeedButton>();
    }

    private BattleStatus_SO CreateSpeedButtonPenaltyStatus()
    {
        BattleStatus_SO status = ScriptableObject.CreateInstance<BattleStatus_SO>();
        SetPrivateField(status, "statusId", "stun");
        SetPrivateField(status, "displayName", "Stun");
        SetPrivateField(status, "behaviorType", BattleStatus_SO.StatusBehaviorType.SpeedButtonPenalty);
        SetPrivateField(status, "powerPerStack", 1);
        createdObjects.Add(status);
        return status;
    }

    private static void SetPrivateField<TValue>(object target, string fieldName, TValue value)
    {
        System.Reflection.FieldInfo field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"Field {fieldName} was not found on {target.GetType().Name}.");
        field.SetValue(target, value);
    }
}
