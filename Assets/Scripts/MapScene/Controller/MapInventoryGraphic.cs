using UnityEngine;
using UnityEngine.UI;

/// <summary>Inventory leather, parchment tabs and brass slot rims, drawn at the current UI size.</summary>
public sealed class MapInventoryGraphic : MapMerchantFrameGraphic
{
    public enum InventoryStyle { Tab, Slot, Well }
    public InventoryStyle inventoryStyle;
    private float focus;

    public void SetFocus(float value)
    {
        if (Mathf.Abs(focus - value) < 0.001f) return;
        focus = value;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect r = GetPixelAdjustedRect();
        if (r.width < 2 || r.height < 2) return;
        Color gold = new Color(0.87f, 0.65f, 0.29f);
        bool parchment = inventoryStyle == InventoryStyle.Tab && selected;
        Surface(vh, r, 10, parchment ? new Color(1f, 0.88f, 0.62f) : new Color(0.035f + focus * 0.025f, 0.125f + focus * 0.04f, 0.073f),
            parchment ? new Color(0.82f, 0.63f, 0.33f) : new Color(0.015f, 0.055f, 0.029f));
        Band(vh, Inset(r, 1), 9, inventoryStyle == InventoryStyle.Tab ? 3f : 1.9f,
            Color.Lerp(gold * (inventoryStyle == InventoryStyle.Well ? 0.64f : 0.95f), new Color(1f, 0.86f, 0.42f), focus), true);
        Band(vh, Inset(r, 5), 6, 0.8f, parchment ? new Color(0.54f, 0.35f, 0.11f) : new Color(0.32f, 0.34f, 0.16f));
        if (inventoryStyle != InventoryStyle.Well)
            CornerCaps(vh, Inset(r, 5), inventoryStyle == InventoryStyle.Tab ? 16 : 10, Color.Lerp(gold, new Color(1f, 0.94f, 0.64f), focus));
        if (focus > 0.01f)
            Band(vh, Inset(r, 2), 8, 1.5f, new Color(1f, 0.91f, 0.48f, focus * 0.8f));
    }
}
