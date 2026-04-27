using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

public class RelicSystemHighRiskTriggerTests
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
    public void StatusAppliedRelic_AddsStacksOnlyForPlayerSourceAndMatchingStatus()
    {
        BattleStatus_SO poison = CreateStatus("poison", "Poison");
        BattleStatus_SO burn = CreateStatus("burn", "Burn");
        Relic_SO relic = CreateRelic(
            "stack-amplifier",
            Relic_SO.RelicTrigger.OnStatusApplied,
            Relic_SO.RelicBuiltInEffectType.AddExtraStatusStacksOnApply,
            effect =>
            {
                effect.statusData = poison;
                effect.amount = 2;
            });
        List<RunRelicState> relicInventory = CreateInventory(relic);

        RelicExecutionContext playerMatchingContext = RelicExecutionContext.CreateStatusApplied(
            null,
            null,
            poison,
            1,
            true,
            null,
            null);
        RelicSystem.Process(relicInventory, playerMatchingContext);

        RelicExecutionContext enemySourceContext = RelicExecutionContext.CreateStatusApplied(
            null,
            null,
            poison,
            1,
            false,
            null,
            null);
        RelicSystem.Process(relicInventory, enemySourceContext);

        RelicExecutionContext mismatchedStatusContext = RelicExecutionContext.CreateStatusApplied(
            null,
            null,
            burn,
            1,
            true,
            null,
            null);
        RelicSystem.Process(relicInventory, mismatchedStatusContext);

        Assert.AreEqual(2, playerMatchingContext.additionalStatusStacks);
        Assert.AreEqual(0, enemySourceContext.additionalStatusStacks);
        Assert.AreEqual(0, mismatchedStatusContext.additionalStatusStacks);
    }

    [Test]
    public void RoundStartStatusRelic_QueuesDelayedEnemyStatusOnlyOnConfiguredRound()
    {
        BattleEnemyController enemy = CreateEnemy("DelayedTarget", 12, 3);
        BattleStatus_SO chill = CreateStatus("chill", "Chill");
        Relic_SO relic = CreateRelic(
            "delayed-chill",
            Relic_SO.RelicTrigger.OnBattleRoundStart,
            Relic_SO.RelicBuiltInEffectType.ApplyStatusToAllEnemiesOnSpecificRound,
            effect =>
            {
                effect.statusData = chill;
                effect.amount = 2;
                effect.requiredRound = 2;
                effect.statusApplyTiming = Relic_SO.RelicStatusApplyTiming.NextRoundStart;
                effect.applyAfterRoundStarts = 1;
            });
        List<RunRelicState> relicInventory = CreateInventory(relic);

        RelicExecutionContext roundOneContext = RelicExecutionContext.CreateBattleRoundStart(
            null,
            1,
            Array.Empty<BattleAllyController>(),
            new[] { enemy });
        RelicSystem.Process(relicInventory, roundOneContext);
        enemy.ConsumeRoundStartStatusEffects();

        RelicExecutionContext roundTwoContext = RelicExecutionContext.CreateBattleRoundStart(
            null,
            2,
            Array.Empty<BattleAllyController>(),
            new[] { enemy });
        RelicSystem.Process(relicInventory, roundTwoContext);

        Assert.AreEqual(0, enemy.GetStatusStacks(chill));

        enemy.ConsumeRoundStartStatusEffects();

        Assert.AreEqual(2, enemy.GetStatusStacks(chill));
    }

    [Test]
    public void DamageRelics_KeepSingleAttackAndAttackDamageModifiersOnTheirOwnTriggers()
    {
        BattleAllyController ally = CreateAlly("Target", 18, 18, 5);
        BattleEnemyController slowerEnemy = CreateEnemy("SlowEnemy", 20, 2);
        Relic_SO relic = CreateRelic(
            "damage-boundary",
            new RelicEffectConfig
            {
                trigger = Relic_SO.RelicTrigger.OnPlayerAboutToTakeSingleAttackDamage,
                effectType = Relic_SO.RelicBuiltInEffectType.ReduceIncomingSingleAttackDamageFlat,
                configure = effect => effect.amount = 3
            },
            new RelicEffectConfig
            {
                trigger = Relic_SO.RelicTrigger.OnPlayerAboutToTakeAttackDamage,
                effectType = Relic_SO.RelicBuiltInEffectType.ReduceIncomingAttackDamagePercentWhenAttackerSlowerThanTarget,
                configure = effect => effect.amount = 20
            });
        List<RunRelicState> relicInventory = CreateInventory(relic);

        RelicExecutionContext singleAttackContext = RelicExecutionContext.CreatePlayerAboutToTakeSingleAttackDamage(
            null,
            null,
            0,
            10,
            slowerEnemy.Speed,
            ally.Speed,
            slowerEnemy,
            ally);
        RelicSystem.Process(relicInventory, singleAttackContext);

        RelicExecutionContext attackContext = RelicExecutionContext.CreatePlayerAboutToTakeAttackDamage(
            null,
            null,
            0,
            10,
            slowerEnemy.Speed,
            ally.Speed,
            slowerEnemy,
            ally);
        RelicSystem.Process(relicInventory, attackContext);

        Assert.AreEqual(-3, singleAttackContext.incomingDamageFlatModifier);
        Assert.AreEqual(0, singleAttackContext.incomingDamagePercentModifier);
        Assert.AreEqual(0, attackContext.incomingDamageFlatModifier);
        Assert.AreEqual(20, attackContext.incomingDamagePercentModifier);
    }

    [Test]
    public void ClashDiceRelics_OnlyModifyClashPreparationWhenConditionsMatch()
    {
        BattleAllyController fastAlly = CreateAlly("FastAlly", 12, 12, 6);
        BattleEnemyController markedEnemy = CreateEnemy("MarkedEnemy", 20, 3);
        EnemySkill_SO skill = CreateSkill("Strike");
        BattleStatus_SO mark = CreateStatus("mark", "Mark");
        markedEnemy.AddStatus(mark, mark.StatusId, mark.DisplayName, 1);
        Relic_SO relic = CreateRelic(
            "clash-boundary",
            new RelicEffectConfig
            {
                trigger = Relic_SO.RelicTrigger.OnPlayerDicePrepared,
                effectType = Relic_SO.RelicBuiltInEffectType.ModifyPlayerClashDiceWhenFasterThanTarget,
                configure = effect => effect.powerAmount = 2
            },
            new RelicEffectConfig
            {
                trigger = Relic_SO.RelicTrigger.OnPlayerDicePrepared,
                effectType = Relic_SO.RelicBuiltInEffectType.ReduceTargetClashMinPowerWhenTargetHasStatus,
                configure = effect =>
                {
                    effect.conditionStatusData = mark;
                    effect.requiredStatusStacks = 1;
                    effect.amount = 1;
                }
            });
        List<RunRelicState> relicInventory = CreateInventory(relic);

        RelicExecutionContext nonClashContext = RelicExecutionContext.CreatePlayerDicePrepared(
            null,
            skill,
            0,
            false,
            fastAlly,
            markedEnemy);
        RelicSystem.Process(relicInventory, nonClashContext);

        RelicExecutionContext clashContext = RelicExecutionContext.CreatePlayerDicePrepared(
            null,
            skill,
            0,
            true,
            fastAlly,
            markedEnemy);
        RelicSystem.Process(relicInventory, clashContext);

        Assert.AreEqual(0, nonClashContext.additionalPowerBonus);
        Assert.AreEqual(0, nonClashContext.additionalTargetMinPowerBonus);
        Assert.AreEqual(2, clashContext.additionalPowerBonus);
        Assert.AreEqual(-1, clashContext.additionalTargetMinPowerBonus);
    }

    [Test]
    public void RewardRelics_GrantOnlySameRarityBonusLootForReforgeAndBattleRewards()
    {
        LootItem_SO commonResult = CreateLoot("CommonResult", LootRarity.Common);
        LootItem_SO commonA = CreateLoot("CommonA", LootRarity.Common);
        LootItem_SO commonB = CreateLoot("CommonB", LootRarity.Common);
        LootItem_SO rare = CreateLoot("Rare", LootRarity.Rare);
        List<LootItem_SO> lootPool = new List<LootItem_SO> { commonA, rare, commonB };
        Relic_SO relic = CreateRelic(
            "same-rarity-rewards",
            new RelicEffectConfig
            {
                trigger = Relic_SO.RelicTrigger.OnLootReforge,
                effectType = Relic_SO.RelicBuiltInEffectType.GrantRandomLootOfSameRarityOnLootReforge,
                configure = effect => effect.amount = 2
            },
            new RelicEffectConfig
            {
                trigger = Relic_SO.RelicTrigger.OnBattleLootRewardGranted,
                effectType = Relic_SO.RelicBuiltInEffectType.GrantExtraRandomLootOfSameRarityAfterBattle,
                configure = effect => effect.amount = 2
            });
        List<RunRelicState> relicInventory = CreateInventory(relic);

        RelicExecutionContext reforgeContext = RelicExecutionContext.CreateLootReforge(
            null,
            commonA,
            commonB,
            commonResult,
            lootPool);
        RelicSystem.Process(relicInventory, reforgeContext);

        RelicExecutionContext battleRewardContext = RelicExecutionContext.CreateBattleLootRewardGranted(
            null,
            commonResult,
            lootPool);
        RelicSystem.Process(relicInventory, battleRewardContext);

        Assert.AreEqual(2, reforgeContext.bonusLootReforgeRewards.Count);
        Assert.AreEqual(2, battleRewardContext.bonusBattleLootRewards.Count);
        Assert.IsTrue(reforgeContext.bonusLootReforgeRewards.TrueForAll(loot => loot != null && loot.rarity == LootRarity.Common));
        Assert.IsTrue(battleRewardContext.bonusBattleLootRewards.TrueForAll(loot => loot != null && loot.rarity == LootRarity.Common));
    }

    private BattleAllyController CreateAlly(string name, int maxHp, int currentHp, int speed)
    {
        GameObject go = new GameObject(name);
        createdObjects.Add(go);

        BattleAllyController ally = go.AddComponent<BattleAllyController>();
        CharacterData_SO data = ScriptableObject.CreateInstance<CharacterData_SO>();
        data.characterName = name;
        data.baseHP = maxHp;
        data.minSpeed = speed;
        data.maxSpeed = speed;
        createdObjects.Add(data);

        ally.ApplyRuntimeState(new RunPartyMemberState
        {
            characterData = data,
            maxHP = maxHp,
            currentHP = currentHp,
            level = 1
        });
        ally.GenerateRandomSpeed(speed, speed);
        return ally;
    }

    private BattleEnemyController CreateEnemy(string name, int hp, int speed)
    {
        GameObject go = new GameObject(name);
        createdObjects.Add(go);

        EnemyData_SO enemyData = ScriptableObject.CreateInstance<EnemyData_SO>();
        enemyData.enemyName = name;
        enemyData.baseHP = hp;
        enemyData.minSpeed = speed;
        enemyData.maxSpeed = speed;
        createdObjects.Add(enemyData);

        BattleEnemyController enemy = go.AddComponent<BattleEnemyController>();
        enemy.ApplyEnemyData(enemyData);
        enemy.GenerateRandomSpeed(speed, speed);
        return enemy;
    }

    private EnemySkill_SO CreateSkill(string name)
    {
        EnemySkill_SO skill = ScriptableObject.CreateInstance<EnemySkill_SO>();
        skill.skillName = name;
        skill.diceCount = 1;
        skill.skillCategory = EnemySkill_SO.SkillCategory.Attack;
        skill.dicePowerRanges = new[]
        {
            new EnemySkill_SO.DicePowerRange { minPower = 1, maxPower = 3 }
        };
        createdObjects.Add(skill);
        return skill;
    }

    private BattleStatus_SO CreateStatus(string statusId, string displayName)
    {
        BattleStatus_SO status = ScriptableObject.CreateInstance<BattleStatus_SO>();
        SetPrivateField(status, "statusId", statusId);
        SetPrivateField(status, "displayName", displayName);
        SetPrivateField(status, "behaviorType", BattleStatus_SO.StatusBehaviorType.None);
        createdObjects.Add(status);
        return status;
    }

    private LootItem_SO CreateLoot(string name, LootRarity rarity)
    {
        LootItem_SO loot = ScriptableObject.CreateInstance<LootItem_SO>();
        loot.lootName = name;
        loot.rarity = rarity;
        createdObjects.Add(loot);
        return loot;
    }

    private Relic_SO CreateRelic(
        string relicId,
        Relic_SO.RelicTrigger trigger,
        Relic_SO.RelicBuiltInEffectType effectType,
        Action<Relic_SO.RelicBuiltInEffectEntry> configure = null)
    {
        return CreateRelic(relicId, new RelicEffectConfig
        {
            trigger = trigger,
            effectType = effectType,
            configure = configure
        });
    }

    private Relic_SO CreateRelic(string relicId, params RelicEffectConfig[] effectConfigs)
    {
        Relic_SO relic = ScriptableObject.CreateInstance<Relic_SO>();
        relic.relicId = relicId;
        relic.relicName = relicId;
        relic.builtInEffects = new List<Relic_SO.RelicBuiltInEffectEntry>();
        for (int i = 0; i < effectConfigs.Length; i++)
        {
            RelicEffectConfig config = effectConfigs[i];
            Relic_SO.RelicBuiltInEffectEntry effect = new Relic_SO.RelicBuiltInEffectEntry
            {
                trigger = config.trigger,
                effectType = config.effectType,
                chance = 1f,
                amount = 1,
                maxStacks = 3,
                requiredStatusStacks = 1,
                requiredRound = 1,
                requiredRoundMin = 1,
                requiredRoundMax = 1
            };
            config.configure?.Invoke(effect);
            relic.builtInEffects.Add(effect);
        }

        createdObjects.Add(relic);
        return relic;
    }

    private static List<RunRelicState> CreateInventory(params Relic_SO[] relics)
    {
        List<RunRelicState> inventory = new List<RunRelicState>();
        for (int i = 0; i < relics.Length; i++)
        {
            inventory.Add(new RunRelicState
            {
                relicData = relics[i],
                amount = 1
            });
        }

        return inventory;
    }

    private static void SetPrivateField<TTarget, TValue>(TTarget target, string fieldName, TValue value)
    {
        FieldInfo field = typeof(TTarget).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"Field {fieldName} was not found on {typeof(TTarget).Name}.");
        field.SetValue(target, value);
    }

    private struct RelicEffectConfig
    {
        public Relic_SO.RelicTrigger trigger;
        public Relic_SO.RelicBuiltInEffectType effectType;
        public Action<Relic_SO.RelicBuiltInEffectEntry> configure;
    }
}
