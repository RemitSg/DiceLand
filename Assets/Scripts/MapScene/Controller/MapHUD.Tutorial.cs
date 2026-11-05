using UnityEngine;

public enum MapTutorialFocusPart { Resources, ActionPoints, Money, TilePanel, TileHeader, TileState, TileCost, TileDescription }

public partial class MapHUD
{
    private RectTransform forestResourcePanel, forestActionCell, forestMoneyCell;

    public bool TryGetTutorialTargetScreenRect(MapTutorialFocusPart part, out Rect result)
    {
        result = default;
        if (canvas == null) return false;
        switch (part)
        {
            case MapTutorialFocusPart.ActionPoints:
                return TutorialHighlightCoordinateUtility.TryGetScreenRect(forestActionCell, out result);
            case MapTutorialFocusPart.Money:
                return TutorialHighlightCoordinateUtility.TryGetScreenRect(forestMoneyCell, out result);
            case MapTutorialFocusPart.Resources:
                return TutorialHighlightCoordinateUtility.TryGetScreenRect(forestResourcePanel, out result);
            case MapTutorialFocusPart.TileHeader:
                return Union(tileTitleText != null ? tileTitleText.rectTransform : null,
                    forestCoordinates != null ? forestCoordinates.rectTransform.parent as RectTransform : null, out result);
            case MapTutorialFocusPart.TileState:
                return Union(forestExploredPanel != null ? forestExploredPanel.rectTransform : null,
                    forestResolvedPanel != null ? forestResolvedPanel.rectTransform : null, out result);
            case MapTutorialFocusPart.TileCost:
                return TutorialHighlightCoordinateUtility.TryGetScreenRect(forestCostStrip, out result);
            case MapTutorialFocusPart.TileDescription:
                return TutorialHighlightCoordinateUtility.TryGetScreenRect(forestTileDescriptionViewport, out result);
            default:
                return TutorialHighlightCoordinateUtility.TryGetScreenRect(tilePanelRect, out result);
        }
    }

    private static bool Union(RectTransform first, RectTransform second, out Rect result)
    {
        result = default;
        if (!TutorialHighlightCoordinateUtility.TryGetScreenRect(first, out Rect a) ||
            !TutorialHighlightCoordinateUtility.TryGetScreenRect(second, out Rect b)) return false;
        result = Rect.MinMaxRect(Mathf.Min(a.xMin, b.xMin), Mathf.Min(a.yMin, b.yMin), Mathf.Max(a.xMax, b.xMax), Mathf.Max(a.yMax, b.yMax));
        return true;
    }
}
