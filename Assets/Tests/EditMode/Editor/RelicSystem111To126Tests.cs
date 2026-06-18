using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class RelicSystem111To126Tests
{
    private readonly List<Object> createdObjects = new List<Object>();

    [TearDown]
    public void TearDown()
    {
        BattleEncounterRuntime.Clear();

        for (int i = createdObjects.Count - 1; i >= 0; i--)
        {
            if (createdObjects[i] != null)
                Object.DestroyImmediate(createdObjects[i]);
        }

        createdObjects.Clear();
    }

    [Test]
    public void RefinedPoison_DealsFixedDamageOncePerEnemyEachRoundWhenPoisonReachesThreshold()
    {
        BattleEnemyController enemy = CreateEnemy("Enemy", 20);
        BattleStatus_SO poison = CreateStatus("poison", "Poison", BattleStatus_SO.StatusBehaviorType.None);
        Relic_SO relic = CreateRelic(
            "refined-poison",
            Relic_SO.RelicTrigger.OnStatusApplied,
            Relic_SO.RelicBuiltInEffectType.DealDamageWhenEnemyStatusReachesStacksOncePerRound,
            effect =>
            {
                effect.conditionStatusData = poison;
                effect.requiredStatusStacks = 2;
                effect.amount = 2;
            });
        ActivateEncounter(CreateEncounter(relic));

        Assert.IsTrue(enemy.AddStatus(poison, poison.StatusId, poison.DisplayName, 2));
        Assert.AreEqual(2, enemy.GetStatusStacks(poison));
        Assert.AreEqual(18, enemy.CurrentHP);

        Assert.IsTrue(enemy.AddStatus(poison, poison.StatusId, poison.DisplayName, 1));
        Assert.AreEqual(18, enemy.CurrentHP);
    }

    [Test]
    public void ChronicPoison_ChangesEnemyPoisonRoundEndDecayToHalfRoundedDown()
    {
        BattleEnemyController enemy = CreateEnemy("Enemy", 20);
        BattleStatus_SO poison = CreateStatus("poison", "Poison", BattleStatus_SO.StatusBehaviorType.None);
        SetPrivateField(poison, "roundEndDecayMode", BattleStatus_SO.StackDecayMode.LoseAll);
        Relic_SO relic = CreateRelic(
            "chronic-poison",
            Relic_SO.RelicTrigger.OnBattleSetup,
            Relic_SO.RelicBuiltInEffectType.SetEnemyStatusRoundEndDecayPercent,
            effect =>
            {
                effect.conditionStatusData = poison;
                effect.amount = 50;
            });
        ActivateEncounter(CreateEncounter(relic));

        enemy.AddStatus(poison, poison.StatusId, poison.DisplayName, 5);
        enemy.ConsumeRoundEndStatusEffects();

        Assert.AreEqual(2, enemy.GetStatusStacks(poison));
    }

    [Test]
    public void TumiFlowerBed_PreservesOnePoisonAtRoundEndWhenAllLivingEnemiesArePoisoned()
    {
        BattleEnemyController first = CreateEnemy("First", 20);
        BattleEnemyController second = CreateEnemy("Second", 20);
        BattleStatus_SO poison = CreateStatus("poison", "Poison", BattleStatus_SO.StatusBehaviorType.None);
        SetPrivateField(poison, "roundEndDecayMode", BattleStatus_SO.StackDecayMode.LoseAll);
        Relic_SO relic = CreateRelic(
            "flower-bed",
            Relic_SO.RelicTrigger.OnBattleRoundEnd,
            Relic_SO.RelicBuiltInEffectType.PreserveEnemyStatusWhenAllLivingEnemiesHaveStatus,
            effect =>
            {
                effect.conditionStatusData = poison;
                effect.amount = 1;
            });
        ActivateEncounter(CreateEncounter(relic));

        first.AddStatus(poison, poison.StatusId, poison.DisplayName, 1);
        second.AddStatus(poison, poison.StatusId, poison.DisplayName, 3);
        first.ConsumeRoundEndStatusEffects();
        second.ConsumeRoundEndStatusEffects();

        Assert.AreEqual(1, first.GetStatusStacks(poison));
        Assert.AreEqual(1, second.GetStatusStacks(poison));
    }

    [Test]
    public void Hopeless_PreventsPoisonedEnemiesFromHealing()
    {
        BattleEnemyController enemy = CreateEnemy("Enemy", 20, 10);
        BattleStatus_SO poison = CreateStatus("poison", "Poison", BattleStatus_SO.StatusBehaviorType.None);
        Relic_SO relic = CreateRelic(
            "hopeless",
            Relic_SO.RelicTrigger.OnBattleSetup,
            Relic_SO.RelicBuiltInEffectType.PreventEnemyHealingWhenHasStatus,
            effect => effect.conditionStatusData = poison);
        ActivateEncounter(CreateEncounter(relic));
        enemy.AddStatus(poison, poison.StatusId, poison.DisplayName, 1);

        int healed = enemy.Heal(5);

        Assert.AreEqual(0, healed);
        Assert.AreEqual(10, enemy.CurrentHP);
    }

    [Test]
    public void PathologyNotes_AddsFixedPercentByPoisonThresholdsInsteadOfScalingPerStack()
    {
        BattleAllyController ally = CreateAlly("Ally", 20, 20);
        BattleEnemyController enemy = CreateEnemy("Enemy", 20);
        BattleStatus_SO poison = CreateStatus("poison", "Poison", BattleStatus_SO.StatusBehaviorType.None);
        enemy.AddStatus(poison, poison.StatusId, poison.DisplayName, 3);
        Relic_SO relic = CreateRelic(
            "pathology-notes",
            Relic_SO.RelicTrigger.OnPlayerAboutToDealDamage,
            Relic_SO.RelicBuiltInEffectType.ModifyOutgoingDamagePercentWhenTargetHasStatus,
            effect =>
            {
                effect.conditionStatusData = poison;
                effect.amount = 20;
            });
        relic.builtInEffects.Add(new Relic_SO.RelicBuiltInEffectEntry
        {
            trigger = Relic_SO.RelicTrigger.OnPlayerAboutToDealDamage,
            effectType = Relic_SO.RelicBuiltInEffectType.ModifyOutgoingDamagePercentWhenTargetHasStatus,
            chance = 1f,
            conditionStatusData = poison,
            amount = 20,
            requiredStatusStacks = 3,
            requiredRound = 1,
            requiredRoundMin = 1,
            requiredRoundMax = 1,
            applyAfterRoundStarts = 1,
            maxStacks = 1
        });
        List<RunRelicState> inventory = CreateInventory(relic);
        RelicExecutionContext context = RelicExecutionContext.CreatePlayerAboutToDealDamage(
            null,
            null,
            0,
            10,
            ally,
            enemy);

        RelicSystem.Process(inventory, context);

        Assert.AreEqual(40, context.outgoingDamagePercentModifier);
    }

    [Test]
    public void RevivalPill_PreventsLethalDamageByTransferringHalfHpFromHighestRatioAlly()
    {
        BattleAllyController dying = CreateAlly("Dying", 20, 4);
        BattleAllyController donor = CreateAlly("Donor", 30, 30);
        BattleEnemyController enemy = CreateEnemy("Enemy", 20);
        BattleStatus_SO poison = CreateStatus("poison", "Poison", BattleStatus_SO.StatusBehaviorType.None);
        BattleStatus_SO strength = CreateStatus("strength", "Strength", BattleStatus_SO.StatusBehaviorType.PowerBonusFlat);
        BattleStatus_SO guard = CreateStatus("guard", "Guard", BattleStatus_SO.StatusBehaviorType.DefensePowerBonusFlat);
        enemy.AddStatus(poison, poison.StatusId, poison.DisplayName, 2);
        Relic_SO relic = CreateRelic(
            "revival-pill",
            Relic_SO.RelicTrigger.OnPlayerAboutToTakeLethalDamage,
            Relic_SO.RelicBuiltInEffectType.PreventLethalDamageByTransferringHighestHpAllyHealth,
            effect =>
            {
                effect.conditionStatusData = poison;
                effect.statusData = strength;
                effect.amount = 1;
                effect.requiredStatusStacks = 2;
                effect.maxStacks = 1;
            });
        relic.builtInEffects.Add(new Relic_SO.RelicBuiltInEffectEntry
        {
            trigger = Relic_SO.RelicTrigger.OnPlayerAboutToTakeLethalDamage,
            effectType = Relic_SO.RelicBuiltInEffectType.ApplyStatusOnTransferredLethalProtectionWhenEnemyStatusAtLeast,
            chance = 1f,
            statusData = guard,
            conditionStatusData = poison,
            amount = 1,
            requiredStatusStacks = 2,
            maxStacks = 1,
            requiredRound = 1,
            requiredRoundMin = 1,
            requiredRoundMax = 1,
            applyAfterRoundStarts = 1
        });
        ActivateEncounter(CreateEncounter(relic));

        int dealt = dying.TakeDamage(10);

        Assert.AreEqual(3, dealt);
        Assert.AreEqual(1, dying.CurrentHP);
        Assert.AreEqual(15, donor.CurrentHP);
        Assert.AreEqual(1, dying.GetStatusStacks(strength));
        Assert.AreEqual(1, dying.GetStatusStacks(guard));
    }

    [Test]
    public void RevivalPill_CanTriggerOnlyOncePerBattle()
    {
        BattleAllyController firstDying = CreateAlly("FirstDying", 20, 4);
        BattleAllyController secondDying = CreateAlly("SecondDying", 20, 4);
        BattleAllyController donor = CreateAlly("Donor", 40, 40);
        BattleEnemyController enemy = CreateEnemy("Enemy", 20);
        BattleStatus_SO poison = CreateStatus("poison", "Poison", BattleStatus_SO.StatusBehaviorType.None);
        BattleStatus_SO strength = CreateStatus("strength", "Strength", BattleStatus_SO.StatusBehaviorType.PowerBonusFlat);
        enemy.AddStatus(poison, poison.StatusId, poison.DisplayName, 2);
        Relic_SO relic = CreateRelic(
            "revival-pill",
            Relic_SO.RelicTrigger.OnPlayerAboutToTakeLethalDamage,
            Relic_SO.RelicBuiltInEffectType.PreventLethalDamageByTransferringHighestHpAllyHealth,
            effect =>
            {
                effect.conditionStatusData = poison;
                effect.statusData = strength;
                effect.amount = 1;
                effect.requiredStatusStacks = 2;
            });
        ActivateEncounter(CreateEncounter(relic));

        int firstDealt = firstDying.TakeDamage(10);
        int secondDealt = secondDying.TakeDamage(10);

        Assert.AreEqual(3, firstDealt);
        Assert.AreEqual(1, firstDying.CurrentHP);
        Assert.AreEqual(4, secondDealt);
        Assert.IsTrue(secondDying.IsDead);
    }

    private BattleEncounterData CreateEncounter(Relic_SO relic)
    {
        BattleEncounterData encounter = new BattleEncounterData();
        encounter.relicInventory = CreateInventory(relic);
        return encounter;
    }

    private static void ActivateEncounter(BattleEncounterData encounter)
    {
        BattleEncounterRuntime.SetEncounter(encounter);
        BattleEncounterRuntime.ConsumeEncounter();
    }

    private List<RunRelicState> CreateInventory(params Relic_SO[] relics)
    {
        List<RunRelicState> inventory = new List<RunRelicState>();
        for (int i = 0; i < relics.Length; i++)
            inventory.Add(new RunRelicState { relicData = relics[i], amount = 1 });
        return inventory;
    }

    private BattleAllyController CreateAlly(string name, int maxHp, int currentHp)
    {
        GameObject go = new GameObject(name);
        createdObjects.Add(go);

        BattleAllyController ally = go.AddComponent<BattleAllyController>();
        ally.ApplyRuntimeState(new RunPartyMemberState
        {
            characterData = CreateCharacter(name, maxHp),
            maxHP = maxHp,
            currentHP = currentHp
        });
        return ally;
    }

    private BattleEnemyController CreateEnemy(string name, int hp)
    {
        return CreateEnemy(name, hp, hp);
    }

    private BattleEnemyController CreateEnemy(string name, int maxHp, int currentHp)
    {
        GameObject go = new GameObject(name);
        createdObjects.Add(go);

        BattleEnemyController enemy = go.AddComponent<BattleEnemyController>();
        enemy.ApplyEnemyData(CreateEnemyData(name, maxHp));
        if (currentHp < maxHp)
            enemy.TakeDamage(maxHp - currentHp, 0, maxHp - currentHp);
        return enemy;
    }

    private CharacterData_SO CreateCharacter(string name, int baseHp)
    {
        CharacterData_SO character = ScriptableObject.CreateInstance<CharacterData_SO>();
        character.characterName = name;
        character.baseHP = baseHp;
        character.minSpeed = 1;
        character.maxSpeed = 1;
        createdObjects.Add(character);
        return character;
    }

    private EnemyData_SO CreateEnemyData(string name, int hp)
    {
        EnemyData_SO enemyData = ScriptableObject.CreateInstance<EnemyData_SO>();
        enemyData.enemyName = name;
        enemyData.baseHP = hp;
        createdObjects.Add(enemyData);
        return enemyData;
    }

    private BattleStatus_SO CreateStatus(string statusId, string displayName, BattleStatus_SO.StatusBehaviorType behaviorType)
    {
        BattleStatus_SO status = ScriptableObject.CreateInstance<BattleStatus_SO>();
        SetPrivateField(status, "statusId", statusId);
        SetPrivateField(status, "displayName", displayName);
        SetPrivateField(status, "behaviorType", behaviorType);
        SetPrivateField(status, "powerPerStack", 1);
        createdObjects.Add(status);
        return status;
    }

    private Relic_SO CreateRelic(
        string relicId,
        Relic_SO.RelicTrigger trigger,
        Relic_SO.RelicBuiltInEffectType effectType,
        System.Action<Relic_SO.RelicBuiltInEffectEntry> configure = null)
    {
        Relic_SO relic = ScriptableObject.CreateInstance<Relic_SO>();
        relic.relicId = relicId;
        relic.relicName = relicId;
        relic.builtInEffects = new List<Relic_SO.RelicBuiltInEffectEntry>();
        Relic_SO.RelicBuiltInEffectEntry effect = new Relic_SO.RelicBuiltInEffectEntry
        {
            trigger = trigger,
            effectType = effectType,
            chance = 1f,
            amount = 1,
            requiredStatusStacks = 1,
            requiredRound = 1,
            requiredRoundMin = 1,
            requiredRoundMax = 1,
            applyAfterRoundStarts = 1,
            maxStacks = 1
        };
        configure?.Invoke(effect);
        relic.builtInEffects.Add(effect);
        createdObjects.Add(relic);
        return relic;
    }

    private static void SetPrivateField<T>(object target, string fieldName, T value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"Missing field {fieldName}.");
        field.SetValue(target, value);
    }
}
