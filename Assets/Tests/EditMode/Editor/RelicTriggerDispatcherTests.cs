using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

public class RelicTriggerDispatcherTests
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
    public void ProcessRunRelics_AppliesRelicInventoryToContext()
    {
        RunState runState = new RunState();
        Relic_SO relic = CreateRelic(
            "dispatcher-move",
            Relic_SO.RelicTrigger.OnMoveCostAboutToSpend,
            Relic_SO.RelicBuiltInEffectType.ChanceToIgnoreMoveCost);
        runState.AddRelic(relic, 1);

        RelicExecutionContext context = RelicExecutionContext.CreateMove(runState, null, null, 1, true);

        RelicTriggerDispatcher.ProcessRunRelics(runState, context);

        Assert.IsTrue(context.ignoreMoveCost);
    }

    [Test]
    public void ProcessBattleRelics_AppliesEncounterRelicsAndFlushesFeedback()
    {
        Relic_SO relic = CreateRelic(
            "dispatcher-battle-end",
            Relic_SO.RelicTrigger.OnBattleRoundEnd,
            Relic_SO.RelicBuiltInEffectType.HealRandomDamagedAllyOnBattleRoundEnd,
            effect => effect.amount = 1);
        BattleEncounterData encounter = new BattleEncounterData
        {
            relicInventory = new List<RunRelicState>
            {
                new RunRelicState { relicData = relic, amount = 1 }
            }
        };
        BattleAllyController ally = CreateAlly("Damaged", 5, 2);
        RelicExecutionContext context = RelicExecutionContext.CreateBattleRoundEnd(
            null,
            1,
            new[] { ally },
            new BattleEnemyController[0]);
        List<string> logs = new List<string>();

        RelicTriggerDispatcher.ProcessBattleRelics(encounter, context, logs.Add);

        Assert.AreEqual(3, ally.CurrentHP);
        Assert.GreaterOrEqual(logs.Count, 1);
    }

    [Test]
    public void CalculateMerchantOfferPrice_UsesExistingRelicPricingRules()
    {
        RunState runState = new RunState();
        BoardTileData tile = new BoardTileData();
        MerchantOfferState offer = new MerchantOfferState
        {
            itemType = MerchantItemType.Relic,
            price = 100
        };
        Relic_SO coupon = CreateRelic(
            "dispatcher-coupon",
            Relic_SO.RelicTrigger.OnMerchantPriceCalculated,
            Relic_SO.RelicBuiltInEffectType.ReduceMerchantPricePercent,
            effect => effect.amount = 25);
        runState.AddRelic(coupon, 1);

        int price = RelicTriggerDispatcher.CalculateMerchantOfferPrice(runState, tile, offer);

        Assert.AreEqual(75, price);
    }

    [Test]
    public void NotifyMerchantPurchaseCompleted_UsesExistingPurchaseRules()
    {
        RunState runState = new RunState();
        BoardTileData tile = new BoardTileData();
        MerchantOfferState offer = new MerchantOfferState
        {
            itemType = MerchantItemType.Relic,
            price = 100
        };
        Relic_SO coupon = CreateRelic(
            "dispatcher-coupon-stack",
            Relic_SO.RelicTrigger.OnMerchantPurchaseCompleted,
            Relic_SO.RelicBuiltInEffectType.AddMerchantPurchaseDiscountStackOnPurchase,
            effect => effect.maxStacks = 3);
        runState.AddRelic(coupon, 1);

        RelicTriggerDispatcher.NotifyMerchantPurchaseCompleted(runState, tile, offer);

        Assert.AreEqual(1, tile.merchantPurchaseDiscountStacks);
    }

    [Test]
    public void CalculateRestReforgeLimit_UsesExistingLimitRules()
    {
        RunState runState = new RunState();
        Relic_SO relic = CreateRelic(
            "dispatcher-reforge-limit",
            Relic_SO.RelicTrigger.OnRestReforgeLimitCalculated,
            Relic_SO.RelicBuiltInEffectType.IncreaseRestReforgeLimit,
            effect => effect.amount = 1);
        runState.AddRelic(relic, 1);

        int limit = RelicTriggerDispatcher.CalculateRestReforgeLimit(runState, 2);

        Assert.AreEqual(3, limit);
    }

    [Test]
    public void ProcessMoveCost_CreatesMoveContextAndAppliesRelics()
    {
        RunState runState = new RunState();
        BoardTileData tile = new BoardTileData { isVisited = false };
        Relic_SO relic = CreateRelic(
            "dispatcher-semantic-move",
            Relic_SO.RelicTrigger.OnMoveCostAboutToSpend,
            Relic_SO.RelicBuiltInEffectType.ChanceToIgnoreMoveCost);
        runState.AddRelic(relic, 1);

        RelicExecutionContext context = RelicTriggerDispatcher.ProcessMoveCost(runState, null, tile, 2);

        Assert.AreEqual(Relic_SO.RelicTrigger.OnMoveCostAboutToSpend, context.trigger);
        Assert.AreEqual(2, context.moveCost);
        Assert.IsTrue(context.isDestinationUnvisited);
        Assert.IsTrue(context.ignoreMoveCost);
    }

    [Test]
    public void ProcessBattleRoundEnd_CreatesBattleRoundEndContextAndFlushesFeedback()
    {
        Relic_SO relic = CreateRelic(
            "dispatcher-semantic-round-end",
            Relic_SO.RelicTrigger.OnBattleRoundEnd,
            Relic_SO.RelicBuiltInEffectType.HealRandomDamagedAllyOnBattleRoundEnd,
            effect => effect.amount = 1);
        BattleEncounterData encounter = new BattleEncounterData
        {
            relicInventory = new List<RunRelicState>
            {
                new RunRelicState { relicData = relic, amount = 1 }
            }
        };
        BattleAllyController ally = CreateAlly("RoundEndDamaged", 5, 2);
        List<string> logs = new List<string>();

        RelicExecutionContext context = RelicTriggerDispatcher.ProcessBattleRoundEnd(
            encounter,
            null,
            3,
            new[] { ally },
            new BattleEnemyController[0],
            logs.Add);

        Assert.AreEqual(Relic_SO.RelicTrigger.OnBattleRoundEnd, context.trigger);
        Assert.AreEqual(3, context.currentRound);
        Assert.AreEqual(3, ally.CurrentHP);
        Assert.GreaterOrEqual(logs.Count, 1);
    }

    private Relic_SO CreateRelic(
        string id,
        Relic_SO.RelicTrigger trigger,
        Relic_SO.RelicBuiltInEffectType effectType,
        System.Action<Relic_SO.RelicBuiltInEffectEntry> configure = null)
    {
        Relic_SO relic = ScriptableObject.CreateInstance<Relic_SO>();
        relic.relicId = id;
        relic.relicName = id;
        relic.builtInEffects = new List<Relic_SO.RelicBuiltInEffectEntry>
        {
            new Relic_SO.RelicBuiltInEffectEntry
            {
                trigger = trigger,
                effectType = effectType,
                chance = 1f,
                amount = 1
            }
        };
        configure?.Invoke(relic.builtInEffects[0]);
        createdObjects.Add(relic);
        return relic;
    }

    private BattleAllyController CreateAlly(string name, int maxHp, int currentHp)
    {
        GameObject go = new GameObject(name);
        createdObjects.Add(go);

        BattleAllyController ally = go.AddComponent<BattleAllyController>();
        CharacterData_SO data = ScriptableObject.CreateInstance<CharacterData_SO>();
        data.characterName = name;
        data.baseHP = maxHp;
        data.minSpeed = 1;
        data.maxSpeed = 1;
        createdObjects.Add(data);

        ally.ApplyRuntimeState(new RunPartyMemberState
        {
            characterData = data,
            maxHP = maxHp,
            currentHP = currentHp,
            level = 1
        });

        return ally;
    }
}
