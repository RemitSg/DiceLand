using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>The same painted backpack entry on the map, events and rest visits.</summary>
public static class MapInventoryEntry
{
    public static Button Create(RectTransform parent, UnityAction open)
    {
        GameObject go = new GameObject("InventoryButton", typeof(RectTransform), typeof(Image), typeof(Button), typeof(MapCraftingButtonFeedback));
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one;
        rect.anchoredPosition = new Vector2(-24f, -24f);
        rect.sizeDelta = new Vector2(88f, 88f);
        Image image = go.GetComponent<Image>();
        image.sprite = Resources.Load<Sprite>("InventoryUI/EntryButton");
        image.preserveAspect = true;
        image.color = Color.white;
        Button button = go.GetComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.None;
        button.onClick.AddListener(open);
        return button;
    }
}
