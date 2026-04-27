using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

public class MerchantPurchaseServiceTests
{
    private LootItem_SO loot;
    private Weapon_SO weapon;
    private Relic_SO relic;

    [TearDown]
    public void TearDown()
    {
        if (loot != null)
            Object.DestroyImmediate(loot);
        if (weapon != null)
            Object.DestroyImmediate(weapon);
        if (relic != null)
            Object.DestroyImmediate(relic);
    }

    [Test]
    public void TryPurchaseOffer_SpendsMoneyAddsLootAndMarksSoldOut()
    {
        loot = ScriptableObject.CreateInstance<LootItem_SO>();
        loot.lootName = "Shard";
        RunState runState = new RunState { money = 20 };
        BoardTileData tile = new BoardTileData { tileType = MapTileType.Merchant };
        MerchantOfferState offer = new MerchantOfferState
        {
            itemType = MerchantItemType.Loot,
            lootItem = loot,
            price = 7
        };

        MerchantPurchaseResult result = MerchantPurchaseService.TryPurchaseOffer(runState, tile, offer);

        Assert.IsTrue(result.success);
        Assert.AreEqual(7, result.price);
        Assert.AreEqual("Shard", result.displayName);
        Assert.AreEqual(13, runState.money);
        Assert.AreEqual(1, runState.GetLootAmount(loot));
        Assert.IsTrue(offer.isSoldOut);
    }

    [Test]
    public void TryPurchaseOffer_InsufficientMoneyDoesNotChangeInventoryOrOffer()
    {
        weapon = ScriptableObject.CreateInstance<Weapon_SO>();
        RunState runState = new RunState { money = 2 };
        BoardTileData tile = new BoardTileData { tileType = MapTileType.Merchant };
        MerchantOfferState offer = new MerchantOfferState
        {
            itemType = MerchantItemType.Weapon,
            weaponData = weapon,
            price = 5
        };

        MerchantPurchaseResult result = MerchantPurchaseService.TryPurchaseOffer(runState, tile, offer);

        Assert.IsFalse(result.success);
        Assert.AreEqual(MerchantPurchaseFailureReason.InsufficientMoney, result.failureReason);
        Assert.AreEqual(2, runState.money);
        Assert.AreEqual(0, runState.GetWeaponAmount(weapon));
        Assert.IsFalse(offer.isSoldOut);
    }

    [Test]
    public void TryPurchaseOffer_InvalidOrSoldOutOfferFails()
    {
        RunState runState = new RunState { money = 20 };
        BoardTileData tile = new BoardTileData { tileType = MapTileType.Merchant };
        MerchantOfferState missingLoot = new MerchantOfferState
        {
            itemType = MerchantItemType.Loot,
            price = 1
        };
        MerchantOfferState soldOut = new MerchantOfferState
        {
            itemType = MerchantItemType.Relic,
            isSoldOut = true
        };

        MerchantPurchaseResult missingResult = MerchantPurchaseService.TryPurchaseOffer(runState, tile, missingLoot);
        MerchantPurchaseResult soldOutResult = MerchantPurchaseService.TryPurchaseOffer(runState, tile, soldOut);

        Assert.IsFalse(missingResult.success);
        Assert.AreEqual(MerchantPurchaseFailureReason.MissingLootData, missingResult.failureReason);
        Assert.IsFalse(soldOutResult.success);
        Assert.AreEqual(MerchantPurchaseFailureReason.SoldOut, soldOutResult.failureReason);
    }

    [Test]
    public void TryPurchaseOffer_NotifiesRelicPurchaseCompletion()
    {
        relic = ScriptableObject.CreateInstance<Relic_SO>();
        relic.relicName = "Coupon";
        relic.builtInEffects.Add(new Relic_SO.RelicBuiltInEffectEntry
        {
            trigger = Relic_SO.RelicTrigger.OnMerchantPurchaseCompleted,
            effectType = Relic_SO.RelicBuiltInEffectType.AddMerchantPurchaseDiscountStackOnPurchase,
            amount = 1
        });
        RunState runState = new RunState { money = 20 };
        runState.AddRelic(relic, 1);
        BoardTileData tile = new BoardTileData { tileType = MapTileType.Merchant };
        MerchantOfferState offer = new MerchantOfferState
        {
            itemType = MerchantItemType.Relic,
            relicData = relic,
            price = 1
        };

        MerchantPurchaseResult result = MerchantPurchaseService.TryPurchaseOffer(runState, tile, offer);

        Assert.IsTrue(result.success);
        Assert.AreEqual(1, tile.merchantPurchaseDiscountStacks);
    }
}
