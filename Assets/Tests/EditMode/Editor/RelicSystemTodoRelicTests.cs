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

    [Test]
    public void DefenseSkillRelic_IncreasesDefenseSkillMaxPowerOnly()
    {
        BattleAllyController ally = CreateAlly("Guarder", 12, 12);
        BattleEnemyController enemy = CreateEnemy("Enemy", 15);
        EnemySkill_SO defenseSkill = CreateSkill("Guard", minPower: 2, maxPower: 4, isDefenseSkill: true);
        EnemySkill_SO attackSkill = CreateSkill("Slash", minPower: 2, maxPower: 4, isDefenseSkill: false);

        ally.GrantTemporarySkill(defenseSkill, true);
        ally.GrantTemporarySkill(attackSkill, false);
        Assert.IsTrue(ally.SetSelectedSkillIndex(0));

        Relic_SO relic = CreateRelic(
            "iron-wall-badge",
            Relic_SO.RelicTrigger.OnPlayerDicePrepared,
            (Relic_SO.RelicBuiltInEffectType)47,
            effect => effect.maxPowerAmount = 1);

        List<RunRelicState> relicInventory = new List<RunRelicState>
        {
            new RunRelicState { relicData = relic, amount = 1 }
        };

        RelicExecutionContext defenseContext = RelicExecutionContext.CreatePlayerDicePrepared(
            null,
            defenseSkill,
            0,
            true,
            ally,
            enemy);
        RelicSystem.Process(relicInventory, defenseContext);
        Assert.AreEqual(1, defenseContext.additionalMaxPowerBonus);

        Assert.IsTrue(ally.SetSelectedSkillIndex(1));
        RelicExecutionContext attackContext = RelicExecutionContext.CreatePlayerDicePrepared(
            null,
            attackSkill,
            0,
            true,
            ally,
            enemy);
        RelicSystem.Process(relicInventory, attackContext);
        Assert.AreEqual(0, attackContext.additionalMaxPowerBonus);
    }

    [Test]
    public void TravelerWaterFlask_HealsPartyWhenEnteringUnvisitedMerchantTile()
    {
        RunState runState = new RunState();
        RunPartyMemberState firstMember = new RunPartyMemberState
        {
            characterData = CreateCharacter("TravelerA", 20),
            level = 1,
            experience = 0,
            maxHP = 20,
            currentHP = 11
        };
        RunPartyMemberState secondMember = new RunPartyMemberState
        {
            characterData = CreateCharacter("TravelerB", 30),
            level = 1,
            experience = 0,
            maxHP = 30,
            currentHP = 19
        };
        runState.partyMembers.Add(firstMember);
        runState.partyMembers.Add(secondMember);

        BoardTileData merchantTile = new BoardTileData
        {
            tileType = MapTileType.Merchant,
            isVisited = false
        };

        Relic_SO relic = CreateRelic(
            "traveler-flask",
            Relic_SO.RelicTrigger.OnMoveCostAboutToSpend,
            (Relic_SO.RelicBuiltInEffectType)30,
            effect =>
            {
                effect.amount = 10;
                effect.requireUnvisitedTile = true;
            });

        List<RunRelicState> relicInventory = new List<RunRelicState>
        {
            new RunRelicState { relicData = relic, amount = 1 }
        };

        RelicExecutionContext context = RelicExecutionContext.CreateMove(runState, null, merchantTile, 1, true);
        RelicSystem.Process(relicInventory, context);

        Assert.AreEqual(13, firstMember.currentHP);
        Assert.AreEqual(22, secondMember.currentHP);
    }

    [Test]
    public void FrostCharm_ReducesIncomingAttackDamageWhenAttackerHasChill()
    {
        BattleAllyController ally = CreateAlly("Target", 18, 18);
        BattleEnemyController enemy = CreateEnemy("ChilledEnemy", 20);
        BattleStatus_SO chill = CreateStatus("chill", "寒冷", BattleStatus_SO.StatusBehaviorType.None);
        enemy.AddStatus(chill, chill.StatusId, chill.DisplayName, 1);

        Relic_SO relic = CreateRelic(
            "frost-charm",
            Relic_SO.RelicTrigger.OnPlayerAboutToTakeAttackDamage,
            (Relic_SO.RelicBuiltInEffectType)31,
            effect =>
            {
                effect.amount = 20;
                effect.conditionStatusData = chill;
                effect.requiredStatusStacks = 1;
            });

        List<RunRelicState> relicInventory = new List<RunRelicState>
        {
            new RunRelicState { relicData = relic, amount = 1 }
        };

        RelicExecutionContext context = RelicExecutionContext.CreatePlayerAboutToTakeAttackDamage(
            null,
            null,
            0,
            10,
            enemy.Speed,
            ally.Speed,
            enemy,
            ally);
        RelicSystem.Process(relicInventory, context);

        Assert.AreEqual(20, context.incomingDamagePercentModifier);
    }

    [Test]
    public void EnergyArmor_ReducesIncomingDamageByEnergyStacksUpToCap()
    {
        BattleAllyController ally = CreateAlly("EnergyUser", 20, 20);
        BattleStatus_SO energy = CreateStatus("energy", "蓄能", BattleStatus_SO.StatusBehaviorType.None);
        ally.AddStatus(energy, energy.StatusId, energy.DisplayName, 5);

        Relic_SO relic = CreateRelic(
            "dragon-scale-armor",
            Relic_SO.RelicTrigger.OnPlayerAboutToTakeDamage,
            (Relic_SO.RelicBuiltInEffectType)34,
            effect =>
            {
                effect.amount = 1;
                effect.conditionStatusData = energy;
                effect.requiredStatusStacks = 1;
                effect.maxStacks = 2;
            });

        List<RunRelicState> relicInventory = new List<RunRelicState>
        {
            new RunRelicState { relicData = relic, amount = 1 }
        };

        RelicExecutionContext context = RelicExecutionContext.CreatePlayerAboutToTakeDamage(
            null,
            null,
            0,
            9,
            ally.Speed,
            null,
            ally);
        RelicSystem.Process(relicInventory, context);

        Assert.AreEqual(-2, context.incomingDamageFlatModifier);
    }

    [Test]
    public void TideRelic_AppliesStatusToAllAlliesOnlyOnOddRounds()
    {
        BattleAllyController firstAlly = CreateAlly("AllyA", 10, 10);
        BattleAllyController secondAlly = CreateAlly("AllyB", 10, 10);
        BattleStatus_SO swiftness = CreateStatus("swiftness", "急速", BattleStatus_SO.StatusBehaviorType.None);

        Relic_SO relic = CreateRelic(
            "tide",
            Relic_SO.RelicTrigger.OnBattleRoundStart,
            (Relic_SO.RelicBuiltInEffectType)33,
            effect =>
            {
                effect.statusData = swiftness;
                effect.amount = 1;
            });

        List<RunRelicState> relicInventory = new List<RunRelicState>
        {
            new RunRelicState { relicData = relic, amount = 1 }
        };

        RelicExecutionContext oddRoundContext = RelicExecutionContext.CreateBattleRoundStart(
            null,
            1,
            new[] { firstAlly, secondAlly },
            Array.Empty<BattleEnemyController>());
        RelicSystem.Process(relicInventory, oddRoundContext);
        Assert.AreEqual(1, firstAlly.GetStatusStacks(swiftness));
        Assert.AreEqual(1, secondAlly.GetStatusStacks(swiftness));

        RelicExecutionContext evenRoundContext = RelicExecutionContext.CreateBattleRoundStart(
            null,
            2,
            new[] { firstAlly, secondAlly },
            Array.Empty<BattleEnemyController>());
        RelicSystem.Process(relicInventory, evenRoundContext);
        Assert.AreEqual(1, firstAlly.GetStatusStacks(swiftness));
        Assert.AreEqual(1, secondAlly.GetStatusStacks(swiftness));
    }

    [Test]
    public void PathfinderManual_RestoresHalfMaxActionPointsWhenCollectingKey()
    {
        RunState runState = new RunState();
        GameObject go = new GameObject("ActionPoints");
        createdObjects.Add(go);

        ActionPointSystem actionPoints = go.AddComponent<ActionPointSystem>();
        actionPoints.Initialize(6);
        actionPoints.TrySpend(5);

        Relic_SO relic = CreateRelic(
            "pathfinder-manual",
            (Relic_SO.RelicTrigger)19,
            (Relic_SO.RelicBuiltInEffectType)32,
            effect => effect.amount = 50);

        List<RunRelicState> relicInventory = new List<RunRelicState>
        {
            new RunRelicState { relicData = relic, amount = 1 }
        };

        RelicExecutionContext context = RelicExecutionContext.CreateKeyCollected(
            runState,
            null,
            new BoardTileData { tileType = MapTileType.Key },
            actionPoints);
        RelicSystem.Process(relicInventory, context);

        Assert.AreEqual(4, actionPoints.CurrentPoints);
    }

    [Test]
    public void AdaptiveVest_QueuesStrengthAndGuardForNextBattleOnCalamity()
    {
        RunState runState = new RunState();
        BattleStatus_SO strength = CreateStatus("strength", "强壮", BattleStatus_SO.StatusBehaviorType.None);
        BattleStatus_SO guard = CreateStatus("guard", "重防", BattleStatus_SO.StatusBehaviorType.None);

        Relic_SO relic = CreateRelic(
            "adaptive-vest",
            new RelicEffectConfig
            {
                trigger = Relic_SO.RelicTrigger.OnCalamityApplied,
                effectType = (Relic_SO.RelicBuiltInEffectType)41,
                configure = effect =>
                {
                    effect.statusData = strength;
                    effect.amount = 1;
                }
            },
            new RelicEffectConfig
            {
                trigger = Relic_SO.RelicTrigger.OnCalamityApplied,
                effectType = (Relic_SO.RelicBuiltInEffectType)41,
                configure = effect =>
                {
                    effect.statusData = guard;
                    effect.amount = 1;
                }
            });

        List<RunRelicState> relicInventory = new List<RunRelicState>
        {
            new RunRelicState { relicData = relic, amount = 1 }
        };

        RelicExecutionContext context = RelicExecutionContext.CreateCalamityApplied(runState, null, null, 1);
        RelicSystem.Process(relicInventory, context);

        List<PendingBattleStatusGrant> pendingGrants = runState.ConsumePendingNextBattleAllyStatusGrants();
        Assert.AreEqual(2, pendingGrants.Count);
        Assert.AreEqual(1, pendingGrants.Find(grant => grant.statusData == strength)?.stacks ?? 0);
        Assert.AreEqual(1, pendingGrants.Find(grant => grant.statusData == guard)?.stacks ?? 0);
    }

    [Test]
    public void CatastropheBanner_IncreasesPartyMaxHpAndHealsLivingMembersOnCalamity()
    {
        RunState runState = new RunState();
        RunPartyMemberState firstMember = new RunPartyMemberState
        {
            characterData = CreateCharacter("BannerA", 10),
            level = 1,
            maxHP = 10,
            currentHP = 6
        };
        RunPartyMemberState secondMember = new RunPartyMemberState
        {
            characterData = CreateCharacter("BannerB", 8),
            level = 1,
            maxHP = 8,
            currentHP = 0
        };
        runState.partyMembers.Add(firstMember);
        runState.partyMembers.Add(secondMember);

        Relic_SO relic = CreateRelic(
            "catastrophe-banner",
            Relic_SO.RelicTrigger.OnCalamityApplied,
            (Relic_SO.RelicBuiltInEffectType)42,
            effect => effect.amount = 4);

        List<RunRelicState> relicInventory = new List<RunRelicState>
        {
            new RunRelicState { relicData = relic, amount = 1 }
        };

        RelicExecutionContext context = RelicExecutionContext.CreateCalamityApplied(runState, null, null, 1);
        RelicSystem.Process(relicInventory, context);

        Assert.AreEqual(14, firstMember.maxHP);
        Assert.AreEqual(10, firstMember.currentHP);
        Assert.AreEqual(12, secondMember.maxHP);
        Assert.AreEqual(0, secondMember.currentHP);
    }

    [Test]
    public void SymbioticColony_TransfersConfiguredStatusesToHighestHpLivingAllyOnDeath()
    {
        BattleAllyController source = CreateAlly("Source", 10, 10);
        BattleAllyController highestHpAlly = CreateAlly("Highest", 16, 12);
        BattleAllyController lowerHpAlly = CreateAlly("Lower", 14, 8);
        BattleStatus_SO swiftness = CreateStatus("swiftness", "急速", BattleStatus_SO.StatusBehaviorType.None);
        BattleStatus_SO strength = CreateStatus("strength", "强壮", BattleStatus_SO.StatusBehaviorType.None);
        BattleStatus_SO guard = CreateStatus("guard", "重防", BattleStatus_SO.StatusBehaviorType.None);

        source.AddStatus(swiftness, swiftness.StatusId, swiftness.DisplayName, 2);
        source.AddStatus(strength, strength.StatusId, strength.DisplayName, 1);
        source.AddStatus(guard, guard.StatusId, guard.DisplayName, 3);
        source.TakeDamage(999);

        Relic_SO relic = CreateRelic(
            "symbiotic-colony",
            new RelicEffectConfig
            {
                trigger = Relic_SO.RelicTrigger.OnPlayerDeath,
                effectType = (Relic_SO.RelicBuiltInEffectType)43,
                configure = effect => effect.statusData = swiftness
            },
            new RelicEffectConfig
            {
                trigger = Relic_SO.RelicTrigger.OnPlayerDeath,
                effectType = (Relic_SO.RelicBuiltInEffectType)43,
                configure = effect => effect.statusData = strength
            },
            new RelicEffectConfig
            {
                trigger = Relic_SO.RelicTrigger.OnPlayerDeath,
                effectType = (Relic_SO.RelicBuiltInEffectType)43,
                configure = effect => effect.statusData = guard
            });

        List<RunRelicState> relicInventory = new List<RunRelicState>
        {
            new RunRelicState { relicData = relic, amount = 1 }
        };

        RelicExecutionContext context = RelicExecutionContext.CreatePlayerDeath(
            null,
            source,
            new[] { source, highestHpAlly, lowerHpAlly });
        RelicSystem.Process(relicInventory, context);

        Assert.AreEqual(2, highestHpAlly.GetStatusStacks(swiftness));
        Assert.AreEqual(1, highestHpAlly.GetStatusStacks(strength));
        Assert.AreEqual(3, highestHpAlly.GetStatusStacks(guard));
        Assert.AreEqual(0, lowerHpAlly.GetStatusStacks(swiftness));
        Assert.AreEqual(0, lowerHpAlly.GetStatusStacks(strength));
        Assert.AreEqual(0, lowerHpAlly.GetStatusStacks(guard));
    }

    [Test]
    public void WindCloudPlate_BattleSetupAddsTemporaryExtraSpeedButtonsToContext()
    {
        Relic_SO relic = CreateRelic(
            "wind-cloud-plate",
            Relic_SO.RelicTrigger.OnBattleSetup,
            (Relic_SO.RelicBuiltInEffectType)45,
            effect => effect.amount = 1);

        List<RunRelicState> relicInventory = new List<RunRelicState>
        {
            new RunRelicState { relicData = relic, amount = 1 }
        };

        RelicExecutionContext context = RelicExecutionContext.CreateBattleSetup(
            null,
            1,
            Array.Empty<BattleAllyController>(),
            Array.Empty<BattleEnemyController>());
        RelicSystem.Process(relicInventory, context);

        Assert.AreEqual(1, context.additionalTemporaryAllySpeedButtons);
    }

    [Test]
    public void MartyrRelic_TransfersInheritedSpeedButtonsToLowestHpLivingAllyOnDeath()
    {
        BattleAllyController source = CreateAlly("Source", 12, 12);
        BattleAllyController lowestHpAlly = CreateAlly("Lowest", 12, 4);
        BattleAllyController higherHpAlly = CreateAlly("Higher", 12, 9);

        source.RegisterSpeedButtons(new[]
        {
            CreateSpeedButton(source.transform, "SourceButton1"),
            CreateSpeedButton(source.transform, "SourceButton2")
        });
        lowestHpAlly.RegisterSpeedButtons(new[]
        {
            CreateSpeedButton(lowestHpAlly.transform, "LowestButton1")
        });
        higherHpAlly.RegisterSpeedButtons(new[]
        {
            CreateSpeedButton(higherHpAlly.transform, "HigherButton1")
        });

        source.SetInheritedExtraSpeedButtons(1);
        source.TakeDamage(999);

        Relic_SO relic = CreateRelic(
            "martyr",
            Relic_SO.RelicTrigger.OnPlayerDeath,
            (Relic_SO.RelicBuiltInEffectType)46);

        List<RunRelicState> relicInventory = new List<RunRelicState>
        {
            new RunRelicState { relicData = relic, amount = 1 }
        };

        RelicExecutionContext context = RelicExecutionContext.CreatePlayerDeath(
            null,
            source,
            new[] { source, lowestHpAlly, higherHpAlly });
        RelicSystem.Process(relicInventory, context);

        Assert.AreEqual(3, lowestHpAlly.SpeedButtons.Count);
        Assert.AreEqual(1, higherHpAlly.SpeedButtons.Count);
    }

    [Test]
    public void EnergyCrystal_RestoresSharedEnergyAtRoundStartInEliteBattle()
    {
        GameObject buttonObject = new GameObject("BattleButton");
        createdObjects.Add(buttonObject);

        BattleButton battleButton = buttonObject.AddComponent<BattleButton>();
        RectTransform sharedEnergyRoot = GetPrivateField<RectTransform>(battleButton, "sharedEnergyRoot");
        if (sharedEnergyRoot != null)
            createdObjects.Add(sharedEnergyRoot.gameObject);
        SetPrivateField(battleButton, "currentSharedEnergyMax", 10);
        SetPrivateField(battleButton, "currentSharedEnergy", 4);

        Relic_SO relic = CreateRelic(
            "energy-crystal",
            Relic_SO.RelicTrigger.OnBattleRoundStart,
            (Relic_SO.RelicBuiltInEffectType)44,
            effect => effect.amount = 1);

        BattleEncounterData encounter = new BattleEncounterData
        {
            tileType = MapTileType.Elite,
            relicInventory = new List<RunRelicState>
            {
                new RunRelicState { relicData = relic, amount = 1 }
            }
        };
        BattleEncounterRuntime.SetEncounter(encounter);
        BattleEncounterRuntime.ConsumeEncounter();

        InvokePrivateMethod(
            battleButton,
            "ApplyBattleRoundStartRelics",
            new object[] { Array.Empty<BattleAllyController>(), Array.Empty<BattleEnemyController>() });

        Assert.AreEqual(5, GetPrivateField<int>(battleButton, "currentSharedEnergy"));
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

    private SpeedButton CreateSpeedButton(Transform parent, string name)
    {
        GameObject buttonObject = new GameObject(name);
        createdObjects.Add(buttonObject);
        buttonObject.transform.SetParent(parent, false);
        return buttonObject.AddComponent<SpeedButton>();
    }

    private EnemySkill_SO CreateSkill(string name, int minPower = 1, int maxPower = 2, bool isDefenseSkill = false)
    {
        EnemySkill_SO skill = ScriptableObject.CreateInstance<EnemySkill_SO>();
        skill.skillName = name;
        skill.diceCount = 1;
        SetPrivateField(skill, "isDefenseSkill", isDefenseSkill);
        skill.dicePowerRanges = new[]
        {
            new EnemySkill_SO.DicePowerRange { minPower = minPower, maxPower = maxPower }
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

    private static TField GetPrivateField<TField>(object target, string fieldName)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"Field {fieldName} was not found on {target.GetType().Name}.");
        return (TField)field.GetValue(target);
    }

    private static object InvokePrivateMethod(object target, string methodName, object[] args)
    {
        MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(method, $"Method {methodName} was not found on {target.GetType().Name}.");
        return method.Invoke(target, args);
    }
}
