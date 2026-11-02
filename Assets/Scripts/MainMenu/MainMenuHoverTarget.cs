using UnityEngine;
using UnityEngine.EventSystems;

[AddComponentMenu("")]
public sealed class MainMenuHoverTarget : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler, IPointerDownHandler, IPointerUpHandler
{
    public MainMenuForestView owner;
    public MainMenuSceneController.MenuAction action;
    public void OnPointerEnter(PointerEventData data) { if (owner != null) owner.Hover(action); }
    public void OnPointerDown(PointerEventData data)
    {
        if (owner != null && data.button == PointerEventData.InputButton.Left) owner.SetPressed(action, true);
    }
    public void OnPointerUp(PointerEventData data)
    {
        if (owner != null && data.button == PointerEventData.InputButton.Left) owner.SetPressed(action, false);
    }
    public void OnSelect(BaseEventData data) { if (owner != null) owner.Hover(action); }
    public void OnPointerExit(PointerEventData data) { if (owner != null) owner.LeaveHover(action); }
    public void OnDeselect(BaseEventData data) { if (owner != null) owner.LeaveHover(action); }
}
