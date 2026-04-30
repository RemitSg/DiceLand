using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class BattleBonusLootRewardServiceTests
{
    [Test]
    public void ApplyBonusLootRewards_DoesNothingWithoutRelicInventory()
    {
        LootItem_SO baseLoot = CreateLoot("Base", LootRarity.Common);
        LootItem_SO bonusLoot = CreateLoot("Bonus", LootRarity.Rare);
        List<RunLootState> rewards = new List<RunLootState>
        {
            new RunLootState { lootItem = baseLoot, amount = 1 }
        };

        BattleBonusLootRewardService.ApplyBonusLootRewards(
            rewards,
            new List<LootItem_SO> { baseLoot, bonusLoot },
            new BattleEncounterData(),
            (_, _) => Assert.Fail("Relics should not be processed without inventory."),
            _ => Assert.Fail("Feedback should not flush without processing."));

        Assert.AreEqual(1, rewards.Count);
    }

    [Test]
    public void ApplyBonusLootRewards_ProcessesEachRewardAmountAndMergesBonuses()
    {
        LootItem_SO baseLoot = CreateLoot("Base", LootRarity.Common);
        LootItem_SO bonusLoot = CreateLoot("Bonus", LootRarity.Common);
        List<RunLootState> rewards = new List<RunLootState>
        {
            new RunLootState { lootItem = baseLoot, amount = 2 }
        };
        BattleEncounterData encounter = CreateEncounterWithRelicInventory();
        int processCount = 0;
        int flushCount = 0;

        BattleBonusLootRewardService.ApplyBonusLootRewards(
            rewards,
            new List<LootItem_SO> { baseLoot, bonusLoot },
            encounter,
            (_, context) =>
            {
                processCount++;
                context.bonusBattleLootRewards.Add(bonusLoot);
            },
            _ => flushCount++);

        Assert.AreEqual(2, processCount);
        Assert.AreEqual(2, flushCount);
        Assert.AreEqual(2, rewards.Count);
        Assert.AreEqual(2, rewards.Find(reward => reward.lootItem == bonusLoot).amount);
    }

    [Test]
    public void ApplyBonusLootRewards_SortsAfterAddingBonuses()
    {
        LootItem_SO commonLoot = CreateLoot("Common", LootRarity.Common);
        LootItem_SO rareLoot = CreateLoot("Rare", LootRarity.Rare);
        List<RunLootState> rewards = new List<RunLootState>
        {
            new RunLootState { lootItem = commonLoot, amount = 1 }
        };

        BattleBonusLootRewardService.ApplyBonusLootRewards(
            rewards,
            new List<LootItem_SO> { commonLoot, rareLoot },
            CreateEncounterWithRelicInventory(),
            (_, context) => context.bonusBattleLootRewards.Add(rareLoot),
            _ => { });

        Assert.AreSame(rareLoot, rewards[0].lootItem);
    }

    private static BattleEncounterData CreateEncounterWithRelicInventory()
    {
        return new BattleEncounterData
        {
            relicInventory = new List<RunRelicState>
            {
                new RunRelicState { relicData = ScriptableObject.CreateInstance<Relic_SO>(), amount = 1 }
            }
        };
    }

    private static LootItem_SO CreateLoot(string name, LootRarity rarity)
    {
        LootItem_SO loot = ScriptableObject.CreateInstance<LootItem_SO>();
        loot.lootName = name;
        loot.rarity = rarity;
        return loot;
    }
}
