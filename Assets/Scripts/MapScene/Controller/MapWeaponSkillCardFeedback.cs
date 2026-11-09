using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>Reading cards brighten on hover without moving the text or implying a cast action.</summary>
public sealed class MapWeaponSkillCardFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public MapWeaponDetailGraphic face;
    private bool hovered;
    private float focus;
    private void OnDisable() { hovered = false; focus = 0; if (face != null) face.SetFocus(0); }
    private void Update()
    {
        focus = Mathf.Lerp(focus, hovered ? 1 : 0, 1 - Mathf.Exp(-16 * Time.unscaledDeltaTime));
        if (face != null) face.SetFocus(focus);
    }
    public void OnPointerEnter(PointerEventData data) { hovered = true; }
    public void OnPointerExit(PointerEventData data) { hovered = false; }
}
