using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class RelicSystem079To094Tests
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
    public void AcquireRelic_GrantsConfiguredMoneyAndPartyMaxHp()
    {
        Relic_SO coin = CreateRelic(
            "ancient-coin",
            Relic_SO.RelicTrigger.OnRelicAcquired,
            Relic_SO.RelicBuiltInEffectType.GrantMoneyOnAcquire,
            effect => effect.amount = 70);
        Relic_SO strawberry = CreateRelic(
            "strawberry",
            Relic_SO.RelicTrigger.OnRelicAcquired,
            Relic_SO.RelicBuiltInEffectType.IncreasePartyMaxHpOnAcquire,
            effect => effect.amount = 3);
        RunState runState = new RunState
        {
            partyMembers = new List<RunPartyMemberState>
            {
                new RunPartyMemberState { memberId = "a", maxHP = 10, currentHP = 7 },
                new RunPartyMemberState { memberId = "b", maxHP = 20, currentHP = 20 }
            }
        };

        runState.AddRelic(coin, 1);
        runState.AddRelic(strawberry, 1);

        Assert.AreEqual(70, runState.money);
        Assert.AreEqual(13, runState.partyMembers[0].maxHP);
        Assert.AreEqual(10, runState.partyMembers[0].currentHP);
        Assert.AreEqual(23, runState.partyMembers[1].maxHP);
        Assert.AreEqual(23, runState.partyMembers[1].currentHP);
    }

    [Test]
    public void MerchantWhiteSlip_AllowsPurchaseWhenMoneyPositiveAndSpendsRemainingMoney()
    {
        LootItem_SO loot = CreateLoot("expensive");
        Relic_SO whiteSlip = CreateRelic(
            "white-slip",
            Relic_SO.RelicTrigger.OnMerchantPurchaseAffordabilityChecked,
            Relic_SO.RelicBuiltInEffectType.AllowMerchantPurchaseWithPositiveMoney,
            effect => effect.amount = 1);
        RunState runState = new RunState { money = 1 };
        runState.AddRelic(whiteSlip, 1);
        BoardTileData tile = new BoardTileData { tileType = MapTileType.Merchant };
        MerchantOfferState offer = new MerchantOfferState
        {
            itemType = MerchantItemType.Loot,
            lootItem = loot,
            price = 99
        };

        MerchantPurchaseResult result = MerchantPurchaseService.TryPurchaseOffer(runState, tile, offer);

        Assert.IsTrue(result.success);
        Assert.AreEqual(0, runState.money);
        Assert.AreEqual(1, runState.GetLootAmount(loot));
        Assert.IsTrue(offer.isSoldOut);
    }

    [Test]
    public void HoardingPermit_ForcesMerchantStockToNineWhenEnoughGoodsExist()
    {
        Relic_SO permit = CreateRelic(
            "hoarding-permit",
            Relic_SO.RelicTrigger.OnMerchantStockCountCalculated,
            Relic_SO.RelicBuiltInEffectType.SetMerchantInitialStockCount,
            effect => effect.amount = 9);
        RunState runState = new RunState();
        runState.AddRelic(permit, 1);
        MerchantCatalog_SO catalog = CreateCatalog(3, 3);
        for (int i = 0; i < 9; i++)
            catalog.lootEntries.Add(new MerchantCatalog_SO.LootGoodsEntry { lootItem = CreateLoot($"loot-{i}"), minPrice = 1, maxPrice = 1, weight = 1 });
        BoardTileData tile = new BoardTileData { tileType = MapTileType.Merchant };

        bool generated = MerchantStockService.TryGenerateInitialStock(
            tile,
            catalog,
            runState,
            CreateDifficulty(),
            new System.Random(1));

        Assert.IsTrue(generated);
        Assert.AreEqual(9, tile.merchantOffers.Count);
    }

    [Test]
    public void WaveSong_AddsOutgoingDamagePercentFromTargetErodeStacks()
    {
        BattleAllyController ally = CreateAlly("Ally", 10, 10);
        BattleEnemyController enemy = CreateEnemy("Enemy", 30);
        BattleStatus_SO erode = CreateStatus("erode", "Water Erode", BattleStatus_SO.StatusBehaviorType.ApplyLinkedStatusAtRoundStart);
        enemy.AddStatus(erode, erode.StatusId, erode.DisplayName, 3);
        Relic_SO waveSong = CreateRelic(
            "wave-song",
            Relic_SO.RelicTrigger.OnPlayerAboutToDealDamage,
            Relic_SO.RelicBuiltInEffectType.ModifyOutgoingDamagePercentByTargetStatusStacks,
            effect =>
            {
                effect.conditionStatusData = erode;
                effect.amount = 10;
            });
        List<RunRelicState> inventory = new List<RunRelicState>
        {
            new RunRelicState { relicData = waveSong, amount = 1 }
        };
        RelicExecutionContext context = RelicExecutionContext.CreatePlayerAboutToDealDamage(
            null,
            null,
            0,
            10,
            ally,
            enemy);

        RelicSystem.Process(inventory, context);

        Assert.AreEqual(30, context.outgoingDamagePercentModifier);
    }

    [Test]
    public void BlueBull_RestoresEnergyOnlyOnThirdRound()
    {
        Relic_SO relic = CreateRelic(
            "blue-bull",
            Relic_SO.RelicTrigger.OnBattleRoundStart,
            Relic_SO.RelicBuiltInEffectType.RestoreSharedEnergyOnSpecificRound,
            effect =>
            {
                effect.requiredRound = 3;
                effect.amount = 5;
            });
        List<RunRelicState> inventory = new List<RunRelicState> { new RunRelicState { relicData = relic, amount = 1 } };

        RelicExecutionContext roundTwo = RelicExecutionContext.CreateBattleRoundStart(null, 2, new BattleAllyController[0], new BattleEnemyController[0]);
        RelicSystem.Process(inventory, roundTwo);
        RelicExecutionContext roundThree = RelicExecutionContext.CreateBattleRoundStart(null, 3, new BattleAllyController[0], new BattleEnemyController[0]);
        RelicSystem.Process(inventory, roundThree);

        Assert.AreEqual(0, roundTwo.additionalSharedEnergyRecovery);
        Assert.AreEqual(5, roundThree.additionalSharedEnergyRecovery);
    }

    [Test]
    public void StatueRelics_ApplyStatusesToMatchingHpRatioAllies()
    {
        BattleStatus_SO strength = CreateStatus("strength", "Strength", BattleStatus_SO.StatusBehaviorType.PowerBonusFlat);
        BattleStatus_SO guard = CreateStatus("guard", "Guard", BattleStatus_SO.StatusBehaviorType.DefensePowerBonusFlat);
        BattleAllyController full = CreateAlly("Full", 10, 10);
        BattleAllyController low = CreateAlly("Low", 10, 1);
        BattleAllyController middle = CreateAlly("Middle", 10, 5);
        Relic_SO pride = CreateRelic(
            "pride-statue",
            Relic_SO.RelicTrigger.OnBattleRoundStart,
            Relic_SO.RelicBuiltInEffectType.ApplyStatusToFullHpAlliesOnBattleRoundStart,
            effect =>
            {
                effect.statusData = strength;
                effect.amount = 1;
            });
        Relic_SO inferiority = CreateRelic(
            "inferiority-statue",
            Relic_SO.RelicTrigger.OnBattleRoundStart,
            Relic_SO.RelicBuiltInEffectType.ApplyStatusToLowHpAlliesOnBattleRoundStart,
            effect =>
            {
                effect.statusData = guard;
                effect.requiredStatusStacks = 20;
                effect.amount = 1;
            });
        List<RunRelicState> inventory = new List<RunRelicState>
        {
            new RunRelicState { relicData = pride, amount = 1 },
            new RunRelicState { relicData = inferiority, amount = 1 }
        };
        RelicExecutionContext context = RelicExecutionContext.CreateBattleRoundStart(
            null,
            1,
            new[] { full, low, middle },
            new BattleEnemyController[0]);

        RelicSystem.Process(inventory, context);

        Assert.AreEqual(1, full.GetStatusStacks(strength));
        Assert.AreEqual(0, middle.GetStatusStacks(strength));
        Assert.AreEqual(1, low.GetStatusStacks(guard));
        Assert.AreEqual(0, middle.GetStatusStacks(guard));
    }

    [Test]
    public void Collapse_QueuesEnemyStatusForNextRoundWhenAllyDies()
    {
        BattleAllyController defeated = CreateAlly("Defeated", 10, 0);
        BattleEnemyController enemy = CreateEnemy("Enemy", 20);
        BattleStatus_SO stun = CreateStatus("stun", "Stun", BattleStatus_SO.StatusBehaviorType.SpeedButtonPenalty);
        Relic_SO collapse = CreateRelic(
            "collapse",
            Relic_SO.RelicTrigger.OnPlayerDeath,
            Relic_SO.RelicBuiltInEffectType.QueueStatusToAllEnemiesOnPlayerDeathNextRound,
            effect =>
            {
                effect.statusData = stun;
                effect.amount = 3;
                effect.statusApplyTiming = Relic_SO.RelicStatusApplyTiming.NextRoundStart;
                effect.applyAfterRoundStarts = 1;
            });
        List<RunRelicState> inventory = new List<RunRelicState> { new RunRelicState { relicData = collapse, amount = 1 } };
        RelicExecutionContext context = RelicExecutionContext.CreatePlayerDeath(null, defeated, new[] { defeated }, new[] { enemy });

        RelicSystem.Process(inventory, context);
        enemy.ConsumeRoundStartStatusEffects();

        Assert.AreEqual(3, enemy.GetStatusStacks(stun));
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

    private LootItem_SO CreateLoot(string name)
    {
        LootItem_SO loot = ScriptableObject.CreateInstance<LootItem_SO>();
        loot.lootName = name;
        createdObjects.Add(loot);
        return loot;
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

    private MerchantCatalog_SO CreateCatalog(int minCount, int maxCount)
    {
        MerchantCatalog_SO catalog = ScriptableObject.CreateInstance<MerchantCatalog_SO>();
        catalog.minTotalGoodsCount = minCount;
        catalog.maxTotalGoodsCount = maxCount;
        createdObjects.Add(catalog);
        return catalog;
    }

    private DifficultyConfig_SO CreateDifficulty()
    {
        DifficultyConfig_SO config = ScriptableObject.CreateInstance<DifficultyConfig_SO>();
        createdObjects.Add(config);
        return config;
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
            requiredRoundMax = 1
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
