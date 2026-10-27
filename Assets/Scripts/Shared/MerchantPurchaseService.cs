using UnityEngine;

public enum MerchantPurchaseFailureReason
{
    None = 0,
    InvalidContext = 1,
    SoldOut = 2,
    MissingLootData = 3,
    MissingWeaponData = 4,
    MissingBlueprintData = 5,
    MissingRelicData = 6,
    UnknownItemType = 7,
    InsufficientMoney = 8
}

public struct MerchantPurchaseResult
{
    public bool success;
    public MerchantPurchaseFailureReason failureReason;
    public int price;
    public string displayName;
    public string message;
    public bool usesPositiveMoneyOverride;
}

public static class MerchantPurchaseService
{
    public static MerchantPurchaseResult EvaluateOffer(RunState runState, BoardTileData tile, MerchantOfferState offer)
    {
        MerchantPurchaseResult invalidResult = ValidatePurchaseContext(runState, tile, offer);
        if (!invalidResult.success)
            return invalidResult;

        int price = Mathf.Max(0, RelicMerchantService.CalculateOfferPrice(runState, tile, offer));
        string displayName = offer.DisplayName;

        if (runState.money < 0)
        {
            return Failure(
                MerchantPurchaseFailureReason.InsufficientMoney,
                price,
                displayName,
                $"金钱不足，需要 {price} 金钱。");
        }

        bool canUsePositiveMoneyOverride = price > 0 && runState.money < price &&
            RelicMerchantService.CanPurchaseWithPositiveMoneyOverride(runState, tile, offer, price);
        if (!canUsePositiveMoneyOverride && runState.money < price)
        {
            return Failure(
                MerchantPurchaseFailureReason.InsufficientMoney,
                price,
                displayName,
                $"金钱不足，需要 {price} 金钱。");
        }

        return new MerchantPurchaseResult { success = true, price = price, displayName = displayName,
            usesPositiveMoneyOverride = canUsePositiveMoneyOverride };
    }

    public static MerchantPurchaseResult TryPurchaseOffer(RunState runState, BoardTileData tile, MerchantOfferState offer)
    {
        MerchantPurchaseResult evaluation = EvaluateOffer(runState, tile, offer);
        if (!evaluation.success) return evaluation;
        int price = evaluation.price;
        string displayName = evaluation.displayName;
        if (evaluation.usesPositiveMoneyOverride) runState.money -= price;
        else if (!runState.TrySpendMoney(price))
            return Failure(MerchantPurchaseFailureReason.InsufficientMoney, price, displayName, $"金钱不足，需要 {price} 金钱。");

        switch (offer.itemType)
        {
            case MerchantItemType.Loot:
                runState.AddLoot(offer.lootItem, 1);
                break;
            case MerchantItemType.Weapon:
                runState.AddWeapon(offer.weaponData, 1);
                break;
            case MerchantItemType.Blueprint:
                runState.AddBlueprint(offer.blueprintData, 1);
                break;
            case MerchantItemType.Relic:
                runState.AddRelic(offer.relicData, 1);
                break;
            default:
                return Failure(MerchantPurchaseFailureReason.UnknownItemType, price, displayName, "未知商品类型。");
        }

        offer.isSoldOut = true;
        RelicMerchantService.NotifyPurchaseCompleted(runState, tile, offer);
        return new MerchantPurchaseResult
        {
            success = true,
            failureReason = MerchantPurchaseFailureReason.None,
            price = price,
            displayName = displayName,
            message = $"购买成功：{displayName}"
        };
    }

    private static MerchantPurchaseResult ValidatePurchaseContext(RunState runState, BoardTileData tile, MerchantOfferState offer)
    {
        if (runState == null || tile == null || offer == null)
            return Failure(MerchantPurchaseFailureReason.InvalidContext, 0, string.Empty, "无法购买该商品。");

        if (offer.isSoldOut)
            return Failure(MerchantPurchaseFailureReason.SoldOut, 0, offer.DisplayName, "该商品已售罄。");

        switch (offer.itemType)
        {
            case MerchantItemType.Loot:
                if (offer.lootItem == null)
                    return Failure(MerchantPurchaseFailureReason.MissingLootData, 0, offer.DisplayName, "商品未配置战利品。");
                break;
            case MerchantItemType.Weapon:
                if (offer.weaponData == null)
                    return Failure(MerchantPurchaseFailureReason.MissingWeaponData, 0, offer.DisplayName, "商品未配置武器。");
                break;
            case MerchantItemType.Blueprint:
                if (offer.blueprintData == null)
                    return Failure(MerchantPurchaseFailureReason.MissingBlueprintData, 0, offer.DisplayName, "商品未配置图纸。");
                break;
            case MerchantItemType.Relic:
                if (offer.relicData == null)
                    return Failure(MerchantPurchaseFailureReason.MissingRelicData, 0, offer.DisplayName, "商品未配置遗物。");
                break;
            default:
                return Failure(MerchantPurchaseFailureReason.UnknownItemType, 0, offer.DisplayName, "未知商品类型。");
        }

        return new MerchantPurchaseResult
        {
            success = true,
            failureReason = MerchantPurchaseFailureReason.None,
            displayName = offer.DisplayName
        };
    }

    private static MerchantPurchaseResult Failure(
        MerchantPurchaseFailureReason reason,
        int price,
        string displayName,
        string message)
    {
        return new MerchantPurchaseResult
        {
            success = false,
            failureReason = reason,
            price = Mathf.Max(0, price),
            displayName = displayName,
            message = message
        };
    }
}
