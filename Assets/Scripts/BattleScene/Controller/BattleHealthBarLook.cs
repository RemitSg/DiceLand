using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Styles the existing health rectangles without taking ownership of HP or their animation.</summary>
public static class BattleHealthBarLook
{
    public static Vector2 ReadableSize(Vector2 configured) => Vector2.Max(configured, new Vector2(132f,22f));

    public static void Apply(RectTransform root, Image border, Image background, Image buffer, Image fill,
        TextMeshProUGUI label)
    {
        Color frontColor = fill.color;
        Color bufferColor = buffer.color;
        border.color = background.color = fill.color = buffer.color = Color.clear;
        Add("Gilded health socket",border.rectTransform,BattleHudOrnament.Shape.HealthFrame,Color.white);
        Add("Health glass",fill.rectTransform,BattleHudOrnament.Shape.HealthFill,frontColor);
        Add("Damage memory glass",buffer.rectTransform,BattleHudOrnament.Shape.HealthFill,bufferColor);
        label.color = new Color(1f,0.97f,0.85f);
        label.enableAutoSizing = true;
        label.fontSizeMax = Mathf.Max(16f,label.fontSize);
        label.fontSizeMin = 11f;
        label.enableWordWrapping = false;
        label.outlineWidth = 0.17f;
        label.outlineColor = new Color32(5,12,12,255);
        foreach (Graphic graphic in root.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
    }

    private static void Add(string name, RectTransform parent, BattleHudOrnament.Shape shape, Color color)
    {
        var child = new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer));
        child.transform.SetParent(parent,false);
        var rect = child.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        var ornament = child.AddComponent<BattleHudOrnament>();
        ornament.kind = shape;
        ornament.color = color;
        ornament.raycastTarget = false;
    }
}
