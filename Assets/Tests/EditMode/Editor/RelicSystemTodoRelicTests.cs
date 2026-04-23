using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class RelicSystemTodoRelicTests
{
    private readonly List<UnityEngine.Object> createdObjects = new List<UnityEngine.Object>();

    [TearDown]
    public void TearDown()
    {
        BattleEncounterRuntime.Clear();

        for (int i = createdObjects.Count - 1; i >= 0; i--)
        {
            UnityEngine.Object created = createdObjects[i];
            if (created != null)
                UnityEngine.Object.DestroyImmediate(created);
        }

        createdObjects.Clear();
    }

    [Test]
    public void CouponRelic_StacksDiscountPerPurchaseAndResetsOnLeave()
    {
        RunState runState = new RunState();
        BoardTileData tile = new BoardTileData();
        MerchantOfferState offer = new MerchantOfferState
        {
            itemType = MerchantItemType.Relic,
            price = 100
        };

        Relic_SO memberCard = CreateRelic(
            "member-card",
            Relic_SO.RelicTrigger.OnMerchantPriceCalculated,
            Relic_SO.RelicBuiltInEffectType.ReduceMerchantPricePercent,
            effect => effect.amount = 30);
        Relic_SO coupon = CreateRelic(
            "coupon",
            new RelicEffectConfig
            {
                trigger = Relic_SO.RelicTrigger.OnMerchantPurchaseCompleted,
                effectType = Relic_SO.RelicBuiltInEffectType.AddMerchantPurchaseDiscountStackOnPurchase,
                configure = effect => effect.maxStacks = 3
            },
            new RelicEffectConfig
            {
                trigger = Relic_SO.RelicTrigger.OnMerchantPriceCalculated,
                effectType = Relic_SO.RelicBuiltInEffectType.ReduceMerchantPricePercentPerPurchaseStack,
                configure = effect =>
                {
                    effect.amount = 5;
                    effect.maxStacks = 3;
                }
            });

        runState.AddRelic(memberCard, 1);
        runState.AddRelic(coupon, 1);

        Assert.AreEqual(70, RelicSystem.CalculateMerchantOfferPrice(runState, tile, offer));

        RelicSystem.NotifyMerchantPurchaseCompleted(runState, tile, offer);
        Assert.AreEqual(1, tile.merchantPurchaseDiscountStacks);
        Assert.AreEqual(67, RelicSystem.CalculateMerchantOfferPrice(runState, tile, offer));

        RelicSystem.NotifyMerchantPurchaseCompleted(runState, tile, offer);
        Assert.AreEqual(2, tile.merchantPurchaseDiscountStacks);
        Assert.AreEqual(63, RelicSystem.CalculateMerchantOfferPrice(runState, tile, offer));

        RelicSystem.ResetMerchantVisitState(tile);
        Assert.AreEqual(0, tile.merchantPurchaseDiscountStacks);
        Assert.AreEqual(70, RelicSystem.CalculateMerchantOfferPrice(runState, tile, offer));
    }

    [Test]
    public void EnemyDeathRelic_HealsAllLivingAllies()
    {
        BattleAllyController firstAlly = CreateAlly("AllyA", 10, 4);
        BattleAllyController secondAlly = CreateAlly("AllyB", 10, 7);
        BattleEnemyController enemy = CreateEnemy("Enemy", 12);

        Relic_SO relic = CreateRelic(
            "fallen-petal",
            Relic_SO.RelicTrigger.OnEnemyDeath,
            Relic_SO.RelicBuiltInEffectType.HealAllAlliesOnEnemyDeath,
            effect => effect.amount = 2);

        List<RunRelicState> relicInventory = new List<RunRelicState>
        {
            new RunRelicState { relicData = relic, amount = 1 }
        };

        RelicExecutionContext context = RelicExecutionContext.CreateEnemyDeath(
            null,
            enemy,
            new[] { firstAlly, secondAlly },
            new[] { enemy });
        RelicSystem.Process(relicInventory, context);

        Assert.AreEqual(6, firstAlly.CurrentHP);
        Assert.AreEqual(9, secondAlly.CurrentHP);
    }

    [Test]
    public void StubbornRelic_PreventsFirstLethalDamageOnlyOncePerBattle()
    {
        BattleAllyController ally = CreateAlly("Stubborn", 10, 10);
        BattleStatus_SO tempStrength = CreateStatus("temp-strength", "临时强壮", BattleStatus_SO.StatusBehaviorType.PowerBonusFlat);
        Relic_SO relic = CreateRelic(
            "stubborn-heart",
            Relic_SO.RelicTrigger.OnPlayerAboutToTakeLethalDamage,
            Relic_SO.RelicBuiltInEffectType.SurviveLethalDamageOncePerBattleAndApplyStatus,
            effect =>
            {
                effect.amount = 1;
                effect.statusData = tempStrength;
            });

        ActivateEncounterWithRelics(relic);

        int firstDamage = ally.TakeDamage(20);
        int secondDamage = ally.TakeDamage(20);

        Assert.AreEqual(9, firstDamage);
        Assert.AreEqual(1, ally.GetStatusStacks(tempStrength));
        Assert.AreEqual(1, secondDamage);
        Assert.IsTrue(ally.IsDead);
    }

    [Test]
    public void ConsecutiveSkillAndTargetStatusRelics_ModifyPreparedDiceAndOutgoingDamage()
    {
        BattleAllyController ally = CreateAlly("BladeUser", 10, 10);
        BattleEnemyController enemy = CreateEnemy("MarkedEnemy", 20);
        EnemySkill_SO skill = CreateSkill("SameSkill");
        BattleStatus_SO marked = CreateStatus("marked", "标记", BattleStatus_SO.StatusBehaviorType.None);
        BattleStatus_SO burn = CreateStatus("burn-test", "燃烧", BattleStatus_SO.StatusBehaviorType.None);
        BattleStatus_SO bleed = CreateStatus("bleed-test", "流血", BattleStatus_SO.StatusBehaviorType.None);

        ally.GrantTemporarySkill(skill, true);
        Assert.IsTrue(ally.SetSelectedSkillIndex(0));
        ally.MarkCurrentRoundSkillUsed();
        ally.FinalizeRoundSkillHistory();
        Assert.IsTrue(ally.SetSelectedSkillIndex(0));
        ally.MarkCurrentRoundSkillUsed();
        ally.FinalizeRoundSkillHistory();

        enemy.AddStatus(marked, marked.StatusId, marked.DisplayName, 1);
        enemy.AddStatus(burn, burn.StatusId, burn.DisplayName, 1);
        enemy.AddStatus(bleed, bleed.StatusId, bleed.DisplayName, 1);

        Relic_SO bladeStone = CreateRelic(
            "blade-stone",
            Relic_SO.RelicTrigger.OnPlayerDicePrepared,
            Relic_SO.RelicBuiltInEffectType.ModifyPlayerDiceWhenSkillUsedConsecutively,
            effect =>
            {
                effect.powerAmount = 1;
                effect.requiredConsecutiveSkillUses = 2;
            });
        Relic_SO cocktail = CreateRelic(
            "cocktail",
            Relic_SO.RelicTrigger.OnPlayerAboutToDealDamage,
            Relic_SO.RelicBuiltInEffectType.ModifyOutgoingDamagePercentWhenTargetHasDistinctStatuses,
            effect =>
            {
                effect.amount = 30;
                effect.requiredDistinctStatusCount = 3;
            });
        Relic_SO hunterMark = CreateRelic(
            "hunter-mark",
            Relic_SO.RelicTrigger.OnPlayerAboutToDealDamage,
            Relic_SO.RelicBuiltInEffectType.ModifyOutgoingDamageFlatWhenTargetHasStatus,
            effect =>
            {
                effect.amount = 1;
                effect.conditionStatusData = marked;
                effect.requiredStatusStacks = 1;
            });

        List<RunRelicState> relicInventory = new List<RunRelicState>
        {
            new RunRelicState { relicData = bladeStone, amount = 1 },
            new RunRelicState { relicData = cocktail, amount = 1 },
            new RunRelicState { relicData = hunterMark, amount = 1 }
        };

        RelicExecutionContext diceContext = RelicExecutionContext.CreatePlayerDicePrepared(
            null,
            skill,
            0,
            true,
            ally,
            enemy);
        RelicSystem.Process(relicInventory, diceContext);

        RelicExecutionContext damageContext = RelicExecutionContext.CreatePlayerAboutToDealDamage(
            null,
            skill,
            0,
            10,
            ally,
            enemy);
        RelicSystem.Process(relicInventory, damageContext);

        Assert.AreEqual(1, diceContext.additionalPowerBonus);
        Assert.AreEqual(1, damageContext.outgoingDamageFlatModifier);
        Assert.AreEqual(30, damageContext.outgoingDamagePercentModifier);

        ally.MarkCurrentRoundSkillUsed();
        ally.FinalizeRoundSkillHistory();

        RelicExecutionContext nextDiceContext = RelicExecutionContext.CreatePlayerDicePrepared(
            null,
            skill,
            0,
            true,
            ally,
            enemy);
        RelicSystem.Process(relicInventory, nextDiceContext);
        Assert.AreEqual(0, nextDiceContext.additionalPowerBonus);
    }

    private BattleAllyController CreateAlly(string name, int maxHp, int currentHp)
    {
        GameObject go = new GameObject(name);
        createdObjects.Add(go);

        BattleAllyController ally = go.AddComponent<BattleAllyController>();
        RunPartyMemberState runtimeState = new RunPartyMemberState
        {
            characterData = CreateCharacter(name, maxHp),
            level = 1,
            experience = 0,
            maxHP = maxHp,
            currentHP = currentHp
        };
        ally.ApplyRuntimeState(runtimeState);
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
            new EnemySkill_SO.DicePowerRange { minPower = 1, maxPower = 2 }
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

    private Relic_SO CreateRelic(string relicId, Relic_SO.RelicTrigger trigger, Relic_SO.RelicBuiltInEffectType effectType, System.Action<Relic_SO.RelicBuiltInEffectEntry> configure = null)
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
                requiredDistinctStatusCount = 3,
                requiredConsecutiveSkillUses = 2
            };
            config.configure?.Invoke(effect);
            relic.builtInEffects.Add(effect);
        }

        createdObjects.Add(relic);
        return relic;
    }

    private void ActivateEncounterWithRelics(params Relic_SO[] relics)
    {
        BattleEncounterData encounter = new BattleEncounterData();
        for (int i = 0; i < relics.Length; i++)
        {
            encounter.relicInventory.Add(new RunRelicState
            {
                relicData = relics[i],
                amount = 1
            });
        }

        BattleEncounterRuntime.SetEncounter(encounter);
        BattleEncounterRuntime.ConsumeEncounter();
    }

    private struct RelicEffectConfig
    {
        public Relic_SO.RelicTrigger trigger;
        public Relic_SO.RelicBuiltInEffectType effectType;
        public Action<Relic_SO.RelicBuiltInEffectEntry> configure;
    }

    private static void SetPrivateField<TTarget, TValue>(TTarget target, string fieldName, TValue value)
    {
        FieldInfo field = typeof(TTarget).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"Field {fieldName} was not found on {typeof(TTarget).Name}.");
        field.SetValue(target, value);
    }
}
