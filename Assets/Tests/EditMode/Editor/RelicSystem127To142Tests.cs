using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class RelicSystem127To142Tests
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
    public void Relay_ReducesClearingCurrentBurstToHalfStacksRoundedDown()
    {
        BattleEnemyController enemy = CreateEnemy("Enemy", 30);
        BattleStatus_SO current = CreateStatus("current", "Current", BattleStatus_SO.StatusBehaviorType.None);
        Relic_SO relic = CreateRelic(
            "relay",
            Relic_SO.RelicTrigger.OnBattleSetup,
            Relic_SO.RelicBuiltInEffectType.SetStatusBurstConsumptionPercent,
            effect =>
            {
                effect.conditionStatusData = current;
                effect.amount = 50;
            });
        ActivateEncounter(CreateEncounter(relic));
        enemy.AddStatus(current, current.StatusId, current.DisplayName, 5);

        int damage = RelicSystem.DispatchPlayerStatusBurst(
            attacker: null,
            target: enemy,
            statusData: current,
            statusId: current.StatusId,
            statusName: current.DisplayName,
            damagePerStack: 1,
            clearStacksAfterBurst: true);

        Assert.AreEqual(5, damage);
        Assert.AreEqual(3, enemy.GetStatusStacks(current));
    }

    [Test]
    public void ElectromagneticAccelerator_TreatsAlliesWithCurrentAsOneSwiftnessAtRoundStart()
    {
        BattleAllyController ally = CreateAlly("Ally", 20, 20);
        BattleStatus_SO current = CreateStatus("current", "Current", BattleStatus_SO.StatusBehaviorType.None);
        Relic_SO relic = CreateRelic(
            "accelerator",
            Relic_SO.RelicTrigger.OnBattleSetup,
            Relic_SO.RelicBuiltInEffectType.TreatAllyWithStatusAsSpeedBonusAtRoundStart,
            effect =>
            {
                effect.conditionStatusData = current;
                effect.amount = 1;
            });
        ActivateEncounter(CreateEncounter(relic));
        ally.AddStatus(current, current.StatusId, current.DisplayName, 2);

        ally.ConsumeRoundStartStatusEffects();
        ally.GenerateRandomSpeed(2, 2);

        Assert.AreEqual(2, ally.Speed);
    }

    [Test]
    public void OscillationCircuit_SpreadsHalfBurstDamageToOtherEnemies()
    {
        BattleAllyController ally = CreateAlly("Ally", 20, 20);
        BattleEnemyController target = CreateEnemy("Target", 30);
        BattleEnemyController other = CreateEnemy("Other", 30);
        BattleStatus_SO current = CreateStatus("current", "Current", BattleStatus_SO.StatusBehaviorType.None);
        Relic_SO relic = CreateRelic(
            "oscillation-circuit",
            Relic_SO.RelicTrigger.OnBattleSetup,
            Relic_SO.RelicBuiltInEffectType.SpreadPlayerStatusBurstDamageToOtherEnemies,
            effect =>
            {
                effect.conditionStatusData = current;
                effect.amount = 50;
            });
        ActivateEncounter(CreateEncounter(relic));
        target.AddStatus(current, current.StatusId, current.DisplayName, 4);

        int damage = RelicSystem.DispatchPlayerStatusBurst(
            ally,
            target,
            current,
            current.StatusId,
            current.DisplayName,
            damagePerStack: 1,
            clearStacksAfterBurst: false);

        Assert.AreEqual(4, damage);
        Assert.AreEqual(26, target.CurrentHP);
        Assert.AreEqual(28, other.CurrentHP);
    }

    [Test]
    public void ElectricBurstShield_GrantsShieldEqualToPlayerBurstDamage()
    {
        BattleAllyController ally = CreateAlly("Ally", 20, 20);
        BattleEnemyController enemy = CreateEnemy("Enemy", 30);
        BattleStatus_SO current = CreateStatus("current", "Current", BattleStatus_SO.StatusBehaviorType.None);
        BattleStatus_SO shield = CreateStatus("shield", "Shield", BattleStatus_SO.StatusBehaviorType.Shield);
        Relic_SO relic = CreateRelic(
            "electric-burst-shield",
            Relic_SO.RelicTrigger.OnBattleSetup,
            Relic_SO.RelicBuiltInEffectType.ApplyStatusToSourceOnPlayerStatusBurstDamage,
            effect =>
            {
                effect.statusData = shield;
                effect.conditionStatusData = current;
                effect.amount = 1;
            });
        ActivateEncounter(CreateEncounter(relic));
        enemy.AddStatus(current, current.StatusId, current.DisplayName, 3);

        RelicSystem.DispatchPlayerStatusBurst(
            ally,
            enemy,
            current,
            current.StatusId,
            current.DisplayName,
            damagePerStack: 2,
            clearStacksAfterBurst: false);

        Assert.AreEqual(6, ally.GetStatusStacks(shield));
    }

    [Test]
    public void LightningStrike_DealsFixedRoundEndDamageToEnemiesWithHighCurrent()
    {
        BattleEnemyController first = CreateEnemy("First", 30);
        BattleEnemyController second = CreateEnemy("Second", 30);
        BattleStatus_SO current = CreateStatus("current", "Current", BattleStatus_SO.StatusBehaviorType.None);
        Relic_SO relic = CreateRelic(
            "lightning-strike",
            Relic_SO.RelicTrigger.OnBattleRoundEnd,
            Relic_SO.RelicBuiltInEffectType.DealDamageToEnemiesWithStatusStacksAtLeastOnBattleRoundEnd,
            effect =>
            {
                effect.conditionStatusData = current;
                effect.requiredStatusStacks = 10;
                effect.amount = 10;
            });
        first.AddStatus(current, current.StatusId, current.DisplayName, 10);
        second.AddStatus(current, current.StatusId, current.DisplayName, 9);
        RelicExecutionContext context = RelicExecutionContext.CreateBattleRoundEnd(
            null,
            1,
            new BattleAllyController[0],
            new[] { first, second });

        RelicSystem.Process(CreateInventory(relic), context);

        Assert.AreEqual(20, first.CurrentHP);
        Assert.AreEqual(30, second.CurrentHP);
    }

    [Test]
    public void HeartShield_QueuesShieldForNextBattleOnlyOnAcquire()
    {
        BattleStatus_SO shield = CreateStatus("shield", "Shield", BattleStatus_SO.StatusBehaviorType.Shield);
        Relic_SO relic = CreateRelic(
            "heart-shield",
            Relic_SO.RelicTrigger.OnRelicAcquired,
            Relic_SO.RelicBuiltInEffectType.QueueNextBattleAllyStatusOnAcquire,
            effect =>
            {
                effect.statusData = shield;
                effect.amount = 10;
            });
        RunState runState = new RunState();
        runState.relicInventory = CreateInventory(relic);

        RelicExecutionContext acquireContext = RelicExecutionContext.CreateRelicAcquired(runState, relic, 1);
        RelicSystem.Process(runState.relicInventory, acquireContext);

        List<PendingBattleStatusGrant> firstBattleGrants = runState.ConsumePendingNextBattleAllyStatusGrants();
        List<PendingBattleStatusGrant> secondBattleGrants = runState.ConsumePendingNextBattleAllyStatusGrants();

        Assert.AreEqual(1, firstBattleGrants.Count);
        Assert.AreEqual(shield, firstBattleGrants[0].statusData);
        Assert.AreEqual(10, firstBattleGrants[0].stacks);
        Assert.AreEqual(0, secondBattleGrants.Count);
    }

    [Test]
    public void Pacemaker_PreventsLethalDamageByConsumingCurrentOncePerAllyEachBattle()
    {
        BattleAllyController ally = CreateAlly("Ally", 20, 4);
        BattleStatus_SO current = CreateStatus("current", "Current", BattleStatus_SO.StatusBehaviorType.None);
        BattleStatus_SO immune = CreateStatus("immune", "Immune", BattleStatus_SO.StatusBehaviorType.Immune);
        SetPrivateField(immune, "roundEndDecayMode", BattleStatus_SO.StackDecayMode.LoseOne);
        Relic_SO relic = CreateRelic(
            "pacemaker",
            Relic_SO.RelicTrigger.OnPlayerAboutToTakeLethalDamage,
            Relic_SO.RelicBuiltInEffectType.PreventLethalDamageByConsumingAllyStatusOncePerBattle,
            effect =>
            {
                effect.statusData = immune;
                effect.conditionStatusData = current;
                effect.requiredStatusStacks = 5;
                effect.amount = 5;
                effect.maxStacks = 1;
            });
        ActivateEncounter(CreateEncounter(relic));
        ally.AddStatus(current, current.StatusId, current.DisplayName, 5);

        int firstDamage = ally.TakeDamage(10);

        Assert.AreEqual(3, firstDamage);
        Assert.AreEqual(0, ally.GetStatusStacks(current));
        Assert.AreEqual(1, ally.GetStatusStacks(immune));
        Assert.AreEqual(1, ally.CurrentHP);
        ally.ConsumeRoundEndStatusEffects();
        int secondDamage = ally.TakeDamage(10);
        Assert.AreEqual(1, secondDamage);
        Assert.IsTrue(ally.IsDead);
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
        GameObject go = new GameObject(name);
        createdObjects.Add(go);

        BattleEnemyController enemy = go.AddComponent<BattleEnemyController>();
        enemy.ApplyEnemyData(CreateEnemyData(name, hp));
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
