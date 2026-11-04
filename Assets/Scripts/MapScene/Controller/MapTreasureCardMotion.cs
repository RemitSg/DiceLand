using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Unscaled card entrance, hover lift and selection rim, shared by mouse, touch and keyboard.</summary>
[RequireComponent(typeof(CanvasGroup))]
public sealed class MapTreasureCardMotion : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
{
    private RectTransform rect;
    private CanvasGroup group;
    private Outline rim;
    private Vector2 resting;
    private float age, delay, focus;
    private bool hovered, pressed, selected, available = true;

    public void Configure(int index, bool enabledChoice)
    {
        rect = (RectTransform)transform;
        group = GetComponent<CanvasGroup>();
        resting = rect.anchoredPosition;
        delay = Mathf.Min(index, 5) * 0.045f;
        available = enabledChoice;
        rim = gameObject.AddComponent<Outline>();
        rim.effectDistance = new Vector2(1.5f, -1.5f);
        rim.effectColor = new Color(1f, 0.79f, 0.33f, 0f);
        rim.useGraphicAlpha = true;
        age = Application.isPlaying ? 0f : 1f;
        Apply(0f);
    }

    public void SetSelected(bool value) { selected = value; Apply(0f); }
    private void Update() { if (Application.isPlaying) Apply(Mathf.Min(Time.unscaledDeltaTime, 0.05f)); }

    private void Apply(float delta)
    {
        if (rect == null) return;
        age += delta;
        float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((age - delay) / 0.23f));
        float target = available && (hovered || selected) ? 1f : 0f;
        focus = delta > 0 ? Mathf.MoveTowards(focus, target, delta * 8f) : target;
        group.alpha = t;
        rect.anchoredPosition = resting + new Vector2(0f, -18f * (1f - t) + 3f * focus);
        rect.localScale = Vector3.one * (Mathf.Lerp(0.965f, 1f, t) + focus * 0.004f - (pressed && available ? 0.008f : 0f));
        if (rim != null)
        {
            rim.enabled = focus > 0.001f;
            rim.effectColor = new Color(1f, 0.81f, 0.39f, (selected ? 0.8f : 0.34f) * focus);
        }
    }

    public void OnPointerEnter(PointerEventData data) { hovered = true; }
    public void OnPointerExit(PointerEventData data) { hovered = pressed = false; }
    public void OnPointerDown(PointerEventData data) { if (data.button == PointerEventData.InputButton.Left) pressed = true; }
    public void OnPointerUp(PointerEventData data) { pressed = false; }
    public void OnSelect(BaseEventData data) { hovered = true; }
    public void OnDeselect(BaseEventData data) { hovered = pressed = false; }
}
