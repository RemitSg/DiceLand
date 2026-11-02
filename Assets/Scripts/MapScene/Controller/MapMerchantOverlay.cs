using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public partial class MapMerchantOverlay : MonoBehaviour
{
    public event Action Hidden;

    [Header("Fonts")]
    [SerializeField] private TMP_FontAsset keywordFont;
    [SerializeField] private TMP_FontAsset bodyFont;
    [SerializeField] private int titleFontSize = 52;
    [SerializeField] private int bodyFontSize = 30;
    [SerializeField] private int buttonFontSize = 30;

    [Header("Panel")]
    [SerializeField] private Vector2 panelSize = new Vector2(1540f, 950f);
    [SerializeField] private Vector2 panelOffset;
    [SerializeField] private Sprite fallbackIconSprite;
    [SerializeField] private string refreshButtonLabel = "刷新商品";
    [SerializeField] private string buyButtonLabel = "购买";
    [SerializeField] private string closeButtonLabel = "离开商店";

    private RunState currentRunState;
    private BoardTileData currentTile;
    private MerchantOfferState selectedOffer;
    private Func<BoardTileData, MerchantOfferState, string> buyHandler;
    private Func<BoardTileData, string> refreshHandler;
    private Func<BoardTileData, int> refreshCostResolver;
    private bool handlingAction;
    private readonly List<Button> offerButtons = new List<Button>();
    private readonly Dictionary<Button, MerchantOfferState> offerLookup = new Dictionary<Button, MerchantOfferState>();

    public bool IsVisible => canvas != null && canvas.gameObject.activeInHierarchy;

    public void ConfigureFonts(TMP_FontAsset keyword, TMP_FontAsset body)
    {
        if (keyword != null) keywordFont = keyword;
        if (body != null) bodyFont = body;
        ApplyFonts();
        layoutDirty = true;
    }

    public void Show(RunState runState, BoardTileData tile,
        Func<BoardTileData, MerchantOfferState, string> onBuy,
        Func<BoardTileData, string> onRefresh, Func<BoardTileData, int> onResolveRefreshCost)
    {
        EnsureUi();
        currentRunState = runState;
        currentTile = tile;
        buyHandler = onBuy;
        refreshHandler = onRefresh;
        refreshCostResolver = onResolveRefreshCost;
        gameObject.SetActive(true);
        canvas.gameObject.SetActive(true);
        statusText.text = string.Empty;
        RefreshOfferList();
        SelectOffer(ResolveSelectableOffer(selectedOffer));
        ApplyViewport(new Vector2(Screen.width, Screen.height), Screen.safeArea);
        RefreshLayout();
    }

    public void Hide()
    {
        if (!IsVisible) return;
        canvas.gameObject.SetActive(false);
        Hidden?.Invoke();
    }

    private void RefreshOfferList()
    {
        foreach (OfferCard card in cards)
        {
            card.button.gameObject.SetActive(false);
            if (Application.isPlaying) Destroy(card.button.gameObject);
            else DestroyImmediate(card.button.gameObject);
        }
        cards.Clear();
        offerButtons.Clear();
        offerLookup.Clear();
        moneyText.text = $"{(currentRunState != null ? currentRunState.money : 0)} 金币";
        if (currentTile?.merchantOffers != null)
            foreach (MerchantOfferState offer in currentTile.merchantOffers)
            {
                if (offer == null) continue;
                OfferCard card = BuildOfferCard(offer);
                cards.Add(card);
                offerButtons.Add(card.button);
                offerLookup[card.button] = offer;
            }
        emptyText.gameObject.SetActive(cards.Count == 0);
        RefreshRefreshButtonState();
        RefreshLayout();
        ResetScroll(offerScrollRect);
    }

    private void SelectOffer(MerchantOfferState offer)
    {
        selectedOffer = offer;
        statusText.text = string.Empty;
        foreach (OfferCard card in cards)
        {
            card.face.SetState(card.offer == selectedOffer, !card.offer.isSoldOut);
            card.iconWell.SetState(card.offer == selectedOffer, !card.offer.isSoldOut);
            // Selection is expressed by the frame. It never replaces the rarity colour.
            card.name.color = ResolveOfferNameColor(card.offer);
            card.icon.color = new Color(1f, 1f, 1f, card.offer.isSoldOut ? 0.45f : 1f);
        }
        RefreshDetail();
        ResetScroll(detailScrollRect);
    }

    private void RefreshDetail()
    {
        bool hasOffer = selectedOffer != null;
        iconImage.sprite = hasOffer ? selectedOffer.DisplayIcon ?? fallbackIconSprite : fallbackIconSprite;
        iconImage.enabled = iconImage.sprite != null;
        goodsNameText.text = hasOffer ? selectedOffer.DisplayName : "未选择商品";
        goodsNameText.color = hasOffer ? ResolveOfferNameColor(selectedOffer) : Ivory;
        metaText.text = hasOffer ? $"{GetGoodsTypeLabel(selectedOffer.itemType)} · {ResolveOfferRarityLabel(selectedOffer)}" : string.Empty;
        descriptionText.text = hasOffer ? (string.IsNullOrWhiteSpace(selectedOffer.Description) ? "暂无商品描述。" : selectedOffer.Description) : "请选择一个商品来查看详情。";
        flavorText.text = hasOffer && selectedOffer.relicData != null ? selectedOffer.relicData.additionalDescription ?? string.Empty : string.Empty;
        MerchantPurchaseResult availability = MerchantPurchaseService.EvaluateOffer(currentRunState, currentTile, selectedOffer);
        availabilityText.text = hasOffer && !availability.success ? availability.message : string.Empty;
        priceText.text = hasOffer ? FormatPrice(selectedOffer, false) : "—";
        buyButton.interactable = hasOffer && buyHandler != null && availability.success;
        buyButton.GetComponentInChildren<TMP_Text>().text = hasOffer && selectedOffer.isSoldOut ? "已售罄" : buyButtonLabel;
        RefreshRefreshButtonState();
        layoutDirty = true;
        RefreshLayout();
    }

    private void OnBuyClicked()
    {
        if (handlingAction || buyHandler == null || !CanPurchaseSelectedOffer()) return;
        handlingAction = true;
        try
        {
            string result = buyHandler(currentTile, selectedOffer);
            RefreshAfterAction(result);
        }
        finally { handlingAction = false; }
    }

    private void OnRefreshClicked()
    {
        if (handlingAction || refreshHandler == null || currentTile == null || !refreshButton.interactable) return;
        handlingAction = true;
        try { RefreshAfterAction(refreshHandler(currentTile)); }
        finally { handlingAction = false; }
    }

    private void RefreshAfterAction(string message)
    {
        MerchantOfferState retained = selectedOffer;
        RefreshOfferList();
        SelectOffer(ResolveSelectableOffer(retained));
        statusText.text = message ?? string.Empty;
        RefreshLayout();
    }

    private void RefreshRefreshButtonState()
    {
        int cost = ResolveRefreshCost();
        refreshButton.GetComponentInChildren<TMP_Text>().text = cost > 0 ? $"{refreshButtonLabel}    <color=#F0CC6B>{cost} 金币</color>" : refreshButtonLabel;
        refreshButton.interactable = refreshHandler != null && currentTile != null && cards.Count > 0 && currentRunState != null && currentRunState.money >= cost;
    }

    private int ResolveRefreshCost() => refreshCostResolver != null && currentTile != null ? Mathf.Max(0, refreshCostResolver(currentTile)) : 0;

    private MerchantOfferState ResolveSelectableOffer(MerchantOfferState preferred)
    {
        if (currentTile?.merchantOffers == null) return null;
        if (preferred != null && currentTile.merchantOffers.Contains(preferred)) return preferred;
        foreach (MerchantOfferState offer in currentTile.merchantOffers) if (offer != null) return offer;
        return null;
    }

    private string FormatPrice(MerchantOfferState offer, bool multiline)
    {
        int price = ResolveDisplayPrice(offer), original = Mathf.Max(0, offer.price);
        if (price == original) return $"{price} 金币";
        return $"<size=75%><color=#C2937B><s>{original} 金币</s></color></size>" + (multiline ? "\n" : "  ") + $"{price} 金币";
    }


    private static string GetGoodsTypeLabel(MerchantItemType itemType)
    {
        switch (itemType)
        {
            case MerchantItemType.Loot:
                return "战利品";
            case MerchantItemType.Weapon:
                return "武器";
            case MerchantItemType.Blueprint:
                return "图纸";
            case MerchantItemType.Relic:
                return "遗物";
            default:
                return "商品";
        }
    }

    private int ResolveDisplayPrice(MerchantOfferState offer)
    {
        return currentRunState != null && offer != null
            ? RelicMerchantService.CalculateOfferPrice(currentRunState, currentTile, offer)
            : Mathf.Max(0, offer != null ? offer.price : 0);
    }

    private bool CanPurchaseSelectedOffer()
    {
        return MerchantPurchaseService.EvaluateOffer(currentRunState, currentTile, selectedOffer).success;
    }

    private static string ResolveOfferRarityLabel(MerchantOfferState offer)
    {
        if (offer == null)
            return string.Empty;

        switch (offer.itemType)
        {
            case MerchantItemType.Weapon:
                return GetWeaponRarityLabel(offer.weaponData != null ? offer.weaponData.rarity : Rarity.Common);
            case MerchantItemType.Loot:
            case MerchantItemType.Blueprint:
            case MerchantItemType.Relic:
                return GetLootRarityLabel(ResolveOfferLootRarity(offer));
            default:
                return string.Empty;
        }
    }

    private static Color ResolveOfferNameColor(MerchantOfferState offer)
    {
        if (offer == null)
            return Color.white;

        switch (offer.itemType)
        {
            case MerchantItemType.Weapon:
                return GetWeaponRarityColor(offer.weaponData != null ? offer.weaponData.rarity : Rarity.Common);
            case MerchantItemType.Loot:
            case MerchantItemType.Blueprint:
            case MerchantItemType.Relic:
                return GetLootRarityColor(ResolveOfferLootRarity(offer));
            default:
                return Color.white;
        }
    }

    private static LootRarity ResolveOfferLootRarity(MerchantOfferState offer)
    {
        if (offer == null)
            return LootRarity.Common;

        switch (offer.itemType)
        {
            case MerchantItemType.Loot:
                return offer.lootItem != null ? offer.lootItem.rarity : LootRarity.Common;
            case MerchantItemType.Blueprint:
                return offer.blueprintData != null ? offer.blueprintData.rarity : LootRarity.Common;
            case MerchantItemType.Relic:
                return offer.relicData != null ? offer.relicData.rarity : LootRarity.Common;
            default:
                return LootRarity.Common;
        }
    }

    private static string GetLootRarityLabel(LootRarity rarity)
    {
        switch (rarity)
        {
            case LootRarity.Trash:
                return "Trash";
            case LootRarity.Common:
                return "普通";
            case LootRarity.Uncommon:
                return "优秀";
            case LootRarity.Rare:
                return "稀有";
            case LootRarity.Epic:
                return "史诗";
            case LootRarity.Legendary:
                return "传说";
            default:
                return rarity.ToString();
        }
    }

    private static string GetWeaponRarityLabel(Rarity rarity)
    {
        switch (rarity)
        {
            case Rarity.Common:
                return "普通";
            case Rarity.Uncommon:
                return "优秀";
            case Rarity.Rare:
                return "稀有";
            case Rarity.Epic:
                return "史诗";
            case Rarity.Legendary:
                return "传说";
            default:
                return rarity.ToString();
        }
    }

    private static Color GetLootRarityColor(LootRarity rarity)
    {
        switch (rarity)
        {
            case LootRarity.Trash:
                return new Color32(126, 83, 48, 255);
            case LootRarity.Common:
                return new Color32(128, 128, 128, 255);
            case LootRarity.Uncommon:
                return new Color32(56, 178, 86, 255);
            case LootRarity.Rare:
                return new Color32(64, 126, 255, 255);
            case LootRarity.Epic:
                return new Color32(163, 92, 255, 255);
            case LootRarity.Legendary:
                return new Color32(230, 180, 44, 255);
            default:
                return Color.white;
        }
    }

    private static Color GetWeaponRarityColor(Rarity rarity)
    {
        switch (rarity)
        {
            case Rarity.Common:
                return new Color32(128, 128, 128, 255);
            case Rarity.Uncommon:
                return new Color32(56, 178, 86, 255);
            case Rarity.Rare:
                return new Color32(64, 126, 255, 255);
            case Rarity.Epic:
                return new Color32(163, 92, 255, 255);
            case Rarity.Legendary:
                return new Color32(230, 180, 44, 255);
            default:
                return Color.white;
        }
    }

}
