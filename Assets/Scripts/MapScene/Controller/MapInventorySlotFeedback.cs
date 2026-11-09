using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Keeps the slot in its grid while animating the icon and its brass rim.</summary>
public sealed class MapInventorySlotFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
{
    public RectTransform icon;
    public MapInventoryGraphic rim;
    private bool hovered, pressed, selected;
    private float focus;
    private Button button;

    private void Awake() { button = GetComponent<Button>(); }
    private void OnDisable()
    {
        hovered = pressed = selected = false;
        focus = 0;
        if (icon != null) icon.localScale = Vector3.one;
        if (rim != null) rim.SetFocus(0);
    }
    private void Update()
    {
        bool active = button == null || button.IsInteractable();
        focus = Mathf.Lerp(focus, active && (hovered || selected) ? 1f : 0f, 1f - Mathf.Exp(-18f * Time.unscaledDeltaTime));
        if (rim != null) rim.SetFocus(focus);
        if (icon != null)
            icon.localScale = Vector3.Lerp(icon.localScale, Vector3.one * (pressed && active ? 0.96f : 1f + focus * 0.045f),
                1f - Mathf.Exp(-24f * Time.unscaledDeltaTime));
    }
    public void OnPointerEnter(PointerEventData e) { hovered = true; }
    public void OnPointerExit(PointerEventData e) { hovered = pressed = false; }
    public void OnPointerDown(PointerEventData e) { if (e.button == PointerEventData.InputButton.Left) pressed = true; }
    public void OnPointerUp(PointerEventData e) { pressed = false; }
    public void OnSelect(BaseEventData e) { selected = true; }
    public void OnDeselect(BaseEventData e) { selected = pressed = false; }
}
