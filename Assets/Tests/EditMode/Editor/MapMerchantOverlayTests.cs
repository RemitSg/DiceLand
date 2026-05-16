using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public class MapMerchantOverlayTests
{
    private GameObject overlayObject;
    private LootItem_SO loot;
    private Relic_SO creditSlip;

    [TearDown]
    public void TearDown()
    {
        if (overlayObject != null)
            Object.DestroyImmediate(overlayObject);
        if (loot != null)
            Object.DestroyImmediate(loot);
        if (creditSlip != null)
            Object.DestroyImmediate(creditSlip);
    }

    [Test]
    public void Show_WithCreditSlipKeepsBuyButtonInteractableWhenMoneyIsPositiveButInsufficient()
    {
        loot = ScriptableObject.CreateInstance<LootItem_SO>();
        loot.lootName = "Shard";
        creditSlip = ScriptableObject.CreateInstance<Relic_SO>();
        creditSlip.relicName = "Credit Slip";
        creditSlip.builtInEffects.Add(new Relic_SO.RelicBuiltInEffectEntry
        {
            trigger = Relic_SO.RelicTrigger.OnMerchantPurchaseAffordabilityChecked,
            effectType = Relic_SO.RelicBuiltInEffectType.AllowMerchantPurchaseWithPositiveMoney
        });

        RunState runState = new RunState { money = 2 };
        runState.AddRelic(creditSlip, 1);
        BoardTileData tile = new BoardTileData { tileType = MapTileType.Merchant };
        tile.merchantOffers.Add(new MerchantOfferState
        {
            itemType = MerchantItemType.Loot,
            lootItem = loot,
            price = 5
        });

        overlayObject = new GameObject("MerchantOverlay");
        MapMerchantOverlay overlay = overlayObject.AddComponent<MapMerchantOverlay>();
        overlay.Show(runState, tile, (_, _) => string.Empty, _ => string.Empty, _ => 0);

        Button buyButton = GetPrivateField<Button>(overlay, "buyButton");

        Assert.IsNotNull(buyButton);
        Assert.IsTrue(buyButton.interactable);
    }

    [Test]
    public void Show_WithCreditSlipDisablesBuyButtonWhenMoneyIsNegative()
    {
        loot = ScriptableObject.CreateInstance<LootItem_SO>();
        loot.lootName = "Shard";
        creditSlip = ScriptableObject.CreateInstance<Relic_SO>();
        creditSlip.relicName = "Credit Slip";
        creditSlip.builtInEffects.Add(new Relic_SO.RelicBuiltInEffectEntry
        {
            trigger = Relic_SO.RelicTrigger.OnMerchantPurchaseAffordabilityChecked,
            effectType = Relic_SO.RelicBuiltInEffectType.AllowMerchantPurchaseWithPositiveMoney
        });

        RunState runState = new RunState { money = -1 };
        runState.AddRelic(creditSlip, 1);
        BoardTileData tile = new BoardTileData { tileType = MapTileType.Merchant };
        tile.merchantOffers.Add(new MerchantOfferState
        {
            itemType = MerchantItemType.Loot,
            lootItem = loot,
            price = 0
        });

        overlayObject = new GameObject("MerchantOverlay");
        MapMerchantOverlay overlay = overlayObject.AddComponent<MapMerchantOverlay>();
        overlay.Show(runState, tile, (_, _) => string.Empty, _ => string.Empty, _ => 0);

        Button buyButton = GetPrivateField<Button>(overlay, "buyButton");

        Assert.IsNotNull(buyButton);
        Assert.IsFalse(buyButton.interactable);
    }

    private static T GetPrivateField<T>(object target, string fieldName) where T : class
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field);
        return field.GetValue(target) as T;
    }
}
