using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class RelicSystem095To110Tests
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
    public void PoisonShield_QueuesShieldForAllyWhoLosesClashAgainstPoisonedTarget()
    {
        BattleAllyController ally = CreateAlly("Ally", 12, 12);
        BattleEnemyController enemy = CreateEnemy("Enemy", 20);
        BattleStatus_SO poison = CreateStatus("poison", "Poison", BattleStatus_SO.StatusBehaviorType.None);
        BattleStatus_SO shield = CreateStatus("shield", "Shield", BattleStatus_SO.StatusBehaviorType.Shield);
        enemy.AddStatus(poison, poison.StatusId, poison.DisplayName, 1);
        Relic_SO relic = CreateRelic(
            "poison-shield",
            Relic_SO.RelicTrigger.OnPlayerClashLost,
            Relic_SO.RelicBuiltInEffectType.QueueStatusToPlayerOnClashLostAgainstStatusTarget,
            effect =>
            {
                effect.statusData = shield;
                effect.conditionStatusData = poison;
                effect.statusApplyTiming = Relic_SO.RelicStatusApplyTiming.NextRoundStart;
                effect.applyAfterRoundStarts = 1;
            });
        List<RunRelicState> inventory = CreateInventory(relic);

        RelicExecutionContext context = RelicExecutionContext.CreatePlayerClashLost(
            null,
            null,
            0,
            ally,
            enemy);
        RelicSystem.Process(inventory, context);

        Assert.AreEqual(0, ally.GetStatusStacks(shield));
        ally.ConsumeRoundStartStatusEffects();
        Assert.AreEqual(1, ally.GetStatusStacks(shield));
    }

    [Test]
    public void BattleEnergyRelics_AddMaxEnergyAndTemporaryEnergy()
    {
        Relic_SO sleepingBag = CreateRelic(
            "energy-sleeping-bag",
            Relic_SO.RelicTrigger.OnBattleSetup,
            Relic_SO.RelicBuiltInEffectType.IncreaseSharedEnergyMaxAtBattleStart,
            effect => effect.amount = 5);
        Relic_SO moon = CreateRelic(
            "moon-in-water",
            Relic_SO.RelicTrigger.OnBattleRoundStart,
            Relic_SO.RelicBuiltInEffectType.GrantTemporarySharedEnergyOnSpecificRound,
            effect =>
            {
                effect.requiredRound = 3;
                effect.amount = 10;
            });
        Relic_SO flower = CreateRelic(
            "flower-in-mirror",
            Relic_SO.RelicTrigger.OnBattleRoundStart,
            Relic_SO.RelicBuiltInEffectType.GrantTemporarySharedEnergyEachRound,
            effect => effect.amount = 1);
        List<RunRelicState> inventory = CreateInventory(sleepingBag, moon, flower);

        RelicExecutionContext setup = RelicExecutionContext.CreateBattleSetup(null, 1, null, null);
        RelicSystem.Process(inventory, setup);
        RelicExecutionContext roundTwo = RelicExecutionContext.CreateBattleRoundStart(null, 2, null, null);
        RelicSystem.Process(inventory, roundTwo);
        RelicExecutionContext roundThree = RelicExecutionContext.CreateBattleRoundStart(null, 3, null, null);
        RelicSystem.Process(inventory, roundThree);

        Assert.AreEqual(5, setup.additionalSharedEnergyMax);
        Assert.AreEqual(1, roundTwo.additionalTemporarySharedEnergy);
        Assert.AreEqual(11, roundThree.additionalTemporarySharedEnergy);
    }

    [Test]
    public void RestCoinRelics_GrantMoneyOnMatchingRestActions()
    {
        RunState runState = new RunState();
        Relic_SO hope = CreateRelic("hope-coin", Relic_SO.RelicTrigger.OnRestActionPointRestored, Relic_SO.RelicBuiltInEffectType.GrantMoney, effect => effect.amount = 40);
        Relic_SO explore = CreateRelic("explore-coin", Relic_SO.RelicTrigger.OnRestReforgeCompleted, Relic_SO.RelicBuiltInEffectType.GrantMoney, effect => effect.amount = 20);
        Relic_SO peace = CreateRelic("peace-coin", Relic_SO.RelicTrigger.OnRestHealCompleted, Relic_SO.RelicBuiltInEffectType.GrantMoney, effect => effect.amount = 40);
        runState.AddRelic(hope, 1);
        runState.AddRelic(explore, 1);
        runState.AddRelic(peace, 1);

        RelicTriggerDispatcher.ProcessRestActionPointRestored(runState);
        RelicTriggerDispatcher.ProcessRestReforgeCompleted(runState);
        RelicTriggerDispatcher.ProcessRestHealCompleted(runState);

        Assert.AreEqual(100, runState.money);
    }

    [Test]
    public void TemporarySkillRelic_AppliesShieldWhenTemporarySkillIsUsed()
    {
        BattleAllyController ally = CreateAlly("Ally", 12, 12);
        EnemySkill_SO temporarySkill = CreateSkill("Temporary");
        ally.GrantTemporarySkill(temporarySkill, true);
        ally.SetSelectedSkillIndex(0);
        BattleStatus_SO shield = CreateStatus("shield", "Shield", BattleStatus_SO.StatusBehaviorType.Shield);
        Relic_SO relic = CreateRelic(
            "no-self",
            Relic_SO.RelicTrigger.OnPlayerSkillUsed,
            Relic_SO.RelicBuiltInEffectType.ApplyStatusToTemporarySkillUser,
            effect =>
            {
                effect.statusData = shield;
                effect.amount = 3;
            });
        List<RunRelicState> inventory = CreateInventory(relic);

        RelicExecutionContext context = RelicExecutionContext.CreatePlayerSkillUsed(null, temporarySkill, ally, null);
        RelicSystem.Process(inventory, context);

        Assert.AreEqual(3, ally.GetStatusStacks(shield));
    }

    [Test]
    public void ForbiddenKnowledge_AdjustsExperienceAndHealsMissingHpAfterBattle()
    {
        BattleAllyController ally = CreateAlly("Ally", 20, 10);
        Relic_SO relic = CreateRelic(
            "forbidden-knowledge",
            Relic_SO.RelicTrigger.OnBattleCompletion,
            Relic_SO.RelicBuiltInEffectType.ModifyBattleExperiencePercentAndHealMissingHp,
            effect =>
            {
                effect.amount = 30;
                effect.powerAmount = -50;
            });
        List<RunRelicState> inventory = CreateInventory(relic);
        RelicExecutionContext context = RelicExecutionContext.CreateBattleCompletion(
            null,
            true,
            20,
            new[] { ally },
            null,
            false);

        RelicSystem.Process(inventory, context);

        Assert.AreEqual(10, context.modifiedBattleExperience);
        Assert.AreEqual(13, ally.CurrentHP);
    }

    [Test]
    public void GamblerDice_RecordsOnlyOneNextRoundMinPowerBonusPerAllyEachRound()
    {
        BattleAllyController ally = CreateAlly("Ally", 12, 12);

        ally.QueueNextRoundMinPowerBonusIfUnused(1);
        ally.QueueNextRoundMinPowerBonusIfUnused(1);
        ally.BeginRelicDiceBonusRound();

        Assert.AreEqual(1, ally.ConsumeCurrentRoundMinPowerBonus());
        Assert.AreEqual(0, ally.ConsumeCurrentRoundMinPowerBonus());
    }

    [Test]
    public void TrashRarity_IsAppendedAndHasBrownPresentation()
    {
        Assert.AreEqual(5, (int)LootRarity.Trash);
        Assert.AreEqual("Trash", TreasureRewardService.GetRarityLabel(LootRarity.Trash));
        Color color = TreasureRewardService.GetRarityColor(LootRarity.Trash);
        Assert.Greater(color.r, color.b);
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

    private EnemySkill_SO CreateSkill(string name)
    {
        EnemySkill_SO skill = ScriptableObject.CreateInstance<EnemySkill_SO>();
        skill.skillName = name;
        skill.diceCount = 1;
        skill.dicePowerRanges = new[]
        {
            new EnemySkill_SO.DicePowerRange { minPower = 1, maxPower = 3 }
        };
        createdObjects.Add(skill);
        return skill;
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
            applyAfterRoundStarts = 1
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
