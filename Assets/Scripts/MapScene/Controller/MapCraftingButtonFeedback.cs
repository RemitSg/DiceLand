using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Short, unscaled tactile feedback for both mouse and touch.</summary>
[RequireComponent(typeof(Button))]
public sealed class MapCraftingButtonFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
{
    private Button button;
    private bool hovered, pressed, selected;
    private float amount = 1;

    private void Awake() { button = GetComponent<Button>(); }
    private void OnEnable() { hovered = pressed = selected = false; amount = 1; transform.localScale = Vector3.one; }
    private void OnDisable() { transform.localScale = Vector3.one; }
    private void Update()
    {
        bool active = button != null && button.IsInteractable();
        float target = active ? (pressed ? 0.965f : hovered || selected ? 1.018f : 1) : 1;
        amount = Mathf.Lerp(amount, target, 1 - Mathf.Exp(-22 * Time.unscaledDeltaTime));
        transform.localScale = Vector3.one * amount;
        if (button != null && button.targetGraphic != null)
        {
            float brightness = active && (hovered || selected) && !pressed ? 1.12f : 1;
            button.targetGraphic.canvasRenderer.SetColor(new Color(brightness, brightness, brightness, 1));
        }
    }
    public void OnPointerEnter(PointerEventData data) { hovered = true; }
    public void OnPointerExit(PointerEventData data) { hovered = pressed = false; }
    public void OnPointerDown(PointerEventData data) { if (data.button == PointerEventData.InputButton.Left) pressed = true; }
    public void OnPointerUp(PointerEventData data) { pressed = false; }
    public void OnSelect(BaseEventData data) { selected = true; }
    public void OnDeselect(BaseEventData data) { selected = pressed = false; }
}
