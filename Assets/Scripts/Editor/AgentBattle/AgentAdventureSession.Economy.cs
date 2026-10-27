using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed partial class AgentAdventureSession
{
    private BoardTileData CurrentTile
    {
        get { map.Board.TryGetTile(map.Snapshot.runState.playerGridPosition, out BoardTileData tile); return tile; }
    }

    private bool OpenEconomyLocation(BoardTileData tile)
    {
        if (tile.tileType != MapTileType.Merchant && tile.tileType != MapTileType.Forge) return false;
        tile.isVisited = true;
        phase = tile.tileType == MapTileType.Merchant ? "merchant" : "forge";
        if (phase == "merchant") map.WithRandom(() =>
        {
            MerchantStockService.EnsureStock(tile, map.Configuration.merchant, map.Snapshot.runState, map.Configuration.starting.difficulty);
            return true;
        });
        message = phase == "merchant" ? "选择商品或刷新商店。" : "选择已拥有的图纸进行锻造。";
        return true;
    }

    private MerchantPurchaseResult Quote(MerchantOfferState offer)
    {
        return map.PreviewWithRandom(() =>
        {
            RunState state = map.Snapshot.runState.Clone(); BoardTileData tile = CurrentTile.Clone();
            MerchantPurchaseResult quote = MerchantPurchaseService.EvaluateOffer(state, tile, offer?.Clone());
            if (offer != null && offer.isSoldOut) quote.price = RelicMerchantService.CalculateOfferPrice(state, tile, offer.Clone());
            return quote;
        });
    }

    private AgentAdventureMerchantOffer[] DescribeMerchant()
    {
        if (phase != "merchant" || CurrentTile.merchantOffers == null) return new AgentAdventureMerchantOffer[0];
        return CurrentTile.merchantOffers.Select((offer, index) =>
        {
            if (offer == null) return new AgentAdventureMerchantOffer { index = index, soldOut = true };
            var quote = Quote(offer);
            UnityEngine.Object data = offer.itemType == MerchantItemType.Loot ? (UnityEngine.Object)offer.lootItem : offer.itemType == MerchantItemType.Weapon ? offer.weaponData
                : offer.itemType == MerchantItemType.Blueprint ? offer.blueprintData : (UnityEngine.Object)offer.relicData;
            return new AgentAdventureMerchantOffer { index = index, id = AssetId(data), name = offer.DisplayName, description = offer.Description,
                kind = offer.itemType.ToString(), price = quote.price, soldOut = offer.isSoldOut, canPurchase = quote.success };
        }).ToArray();
    }

    private IEnumerable<CraftingBlueprint_SO> OwnedRecipes()
    {
        return map.Snapshot.runState.blueprintInventory.Where(s => s?.blueprintData != null && s.amount > 0).Select(s => s.blueprintData).Distinct();
    }
    private AgentAdventureRecipe[] DescribeRecipes()
    {
        RunState state = map.Snapshot.runState;
        return OwnedRecipes().Select(b => new AgentAdventureRecipe { id = AssetId(b), name = b.DisplayName, description = b.description,
            weapon = AssetId(b.targetWeapon), cost = RunCraftingService.Cost(state), canCraft = phase == "forge" && RunCraftingService.CanCraft(state, b, out _),
            materials = RunCraftingService.GetRequirements(b).Select(r => new AgentAdventureMaterial { id = AssetId(r.lootItem), name = r.lootItem.lootName,
                required = r.amount, available = state.GetLootAmount(r.lootItem) }).ToArray() }).ToArray();
    }

    private IEnumerable<AgentAdventureAction> EconomyActions()
    {
        if (phase == "merchant")
        {
            foreach (var offer in DescribeMerchant()) if (offer.canPurchase) yield return new AgentAdventureAction { op = "run_buy", choiceIndex = offer.index };
            bool canRefresh = map.PreviewWithRandom(() => MerchantStockService.TryRefreshStock(map.Snapshot.runState.Clone(), CurrentTile.Clone(),
                map.Configuration.merchant, map.Configuration.starting.difficulty, out _));
            if (canRefresh) yield return new AgentAdventureAction { op = "run_refresh_shop" };
        }
        else if (phase == "forge")
            foreach (var blueprint in OwnedRecipes())
                if (RunCraftingService.CanCraft(map.Snapshot.runState, blueprint, out _)) yield return new AgentAdventureAction { op = "run_craft", assetId = AssetId(blueprint) };
    }

    private void ExecuteEconomyAction(AgentAdventureRequest request)
    {
        map.WithRandom(() =>
        {
            if (request.op == "run_buy")
            {
                var result = MerchantPurchaseService.TryPurchaseOffer(map.Snapshot.runState, CurrentTile, CurrentTile.merchantOffers[request.choiceIndex]);
                if (!result.success) throw new InvalidOperationException(result.message);
                message = result.message;
            }
            else if (request.op == "run_refresh_shop")
            {
                if (!MerchantStockService.TryRefreshStock(map.Snapshot.runState, CurrentTile, map.Configuration.merchant, map.Configuration.starting.difficulty, out message))
                    throw new InvalidOperationException(message);
            }
            else
            {
                CraftingBlueprint_SO blueprint = OwnedRecipes().Single(b => AssetId(b) == request.assetId);
                if (!RunCraftingService.TryCraft(map.Snapshot.runState, blueprint, out message)) throw new InvalidOperationException(message);
            }
            return true;
        });
    }
}
