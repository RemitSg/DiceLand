using NUnit.Framework;

public class BattleEliteRelicChoiceUtilityTests
{
    [Test]
    public void GetDefaultRarityWeight_UsesConfiguredWeights()
    {
        Assert.AreEqual(20f, BattleEliteRelicChoiceUtility.GetDefaultRarityWeight(LootRarity.Common));
        Assert.AreEqual(10f, BattleEliteRelicChoiceUtility.GetDefaultRarityWeight(LootRarity.Uncommon));
        Assert.AreEqual(5f, BattleEliteRelicChoiceUtility.GetDefaultRarityWeight(LootRarity.Rare));
        Assert.AreEqual(2f, BattleEliteRelicChoiceUtility.GetDefaultRarityWeight(LootRarity.Epic));
        Assert.AreEqual(1f, BattleEliteRelicChoiceUtility.GetDefaultRarityWeight(LootRarity.Legendary));
    }

    [Test]
    public void TryGetBucketForChoice_FallsBackToClosestLowerRarityFirst()
    {
        System.Collections.Generic.Dictionary<LootRarity, System.Collections.Generic.List<Relic_SO>> buckets =
            new System.Collections.Generic.Dictionary<LootRarity, System.Collections.Generic.List<Relic_SO>>
            {
                { LootRarity.Common, new System.Collections.Generic.List<Relic_SO> { null, null, null } },
                { LootRarity.Rare, new System.Collections.Generic.List<Relic_SO> { null, null, null } }
            };

        Assert.IsTrue(BattleEliteRelicChoiceUtility.TryGetBucketForChoice(buckets, LootRarity.Uncommon, 3, out System.Collections.Generic.List<Relic_SO> bucket));
        Assert.AreSame(buckets[LootRarity.Common], bucket);
    }
}
