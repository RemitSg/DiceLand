using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class RelicRoundStartSpeedStatusTests
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
    public void BattleRoundStartAllAllySpeedStatus_AffectsCurrentRoundSpeedRoll()
    {
        BattleAllyController ally = CreateAlly("Ally", speed: 2);
        BattleStatus_SO swiftness = CreateStatus(
            "swiftness",
            "Swiftness",
            BattleStatus_SO.StatusBehaviorType.SpeedBonusAtRoundStart,
            powerPerStack: 1);
        Relic_SO relic = CreateRelic(
            Relic_SO.RelicBuiltInEffectType.ApplyStatusToAllAlliesOnSpecificRound,
            effect =>
            {
                effect.statusData = swiftness;
                effect.amount = 1;
                effect.requiredRoundMin = 1;
                effect.requiredRoundMax = 3;
            });
        List<RunRelicState> inventory = new List<RunRelicState>
        {
            new RunRelicState { relicData = relic, amount = 1 }
        };
        RelicExecutionContext context = RelicExecutionContext.CreateBattleRoundStart(
            null,
            currentRound: 1,
            allies: new[] { ally },
            enemies: new BattleEnemyController[0]);

        RelicSystem.Process(inventory, context);
        ally.GenerateRandomSpeed();

        Assert.AreEqual(1, ally.GetStatusStacks(swiftness));
        Assert.AreEqual(3, ally.Speed);
    }

    private BattleAllyController CreateAlly(string name, int speed)
    {
        GameObject go = new GameObject(name);
        createdObjects.Add(go);

        BattleAllyController ally = go.AddComponent<BattleAllyController>();
        CharacterData_SO character = ScriptableObject.CreateInstance<CharacterData_SO>();
        character.characterName = name;
        character.baseHP = 10;
        character.minSpeed = speed;
        character.maxSpeed = speed;
        createdObjects.Add(character);

        ally.ApplyRuntimeState(new RunPartyMemberState
        {
            characterData = character,
            weaponData = CreateWeapon($"{name} Weapon"),
            level = 1,
            maxHP = 10,
            currentHP = 10
        });
        return ally;
    }

    private Weapon_SO CreateWeapon(string name)
    {
        Weapon_SO weapon = ScriptableObject.CreateInstance<Weapon_SO>();
        weapon.weaponName = name;
        createdObjects.Add(weapon);
        return weapon;
    }

    private BattleStatus_SO CreateStatus(
        string statusId,
        string displayName,
        BattleStatus_SO.StatusBehaviorType behaviorType,
        int powerPerStack)
    {
        BattleStatus_SO status = ScriptableObject.CreateInstance<BattleStatus_SO>();
        SetPrivateField(status, "statusId", statusId);
        SetPrivateField(status, "displayName", displayName);
        SetPrivateField(status, "behaviorType", behaviorType);
        SetPrivateField(status, "powerPerStack", powerPerStack);
        createdObjects.Add(status);
        return status;
    }

    private Relic_SO CreateRelic(
        Relic_SO.RelicBuiltInEffectType effectType,
        System.Action<Relic_SO.RelicBuiltInEffectEntry> configure)
    {
        Relic_SO relic = ScriptableObject.CreateInstance<Relic_SO>();
        relic.builtInEffects = new List<Relic_SO.RelicBuiltInEffectEntry>
        {
            new Relic_SO.RelicBuiltInEffectEntry
            {
                trigger = Relic_SO.RelicTrigger.OnBattleRoundStart,
                effectType = effectType,
                amount = 1,
                chance = 1f
            }
        };
        configure?.Invoke(relic.builtInEffects[0]);
        createdObjects.Add(relic);
        return relic;
    }

    private static void SetPrivateField<T>(object target, string fieldName, T value)
    {
        System.Reflection.FieldInfo field = target.GetType().GetField(
            fieldName,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        Assert.IsNotNull(field, fieldName);
        field.SetValue(target, value);
    }
}
