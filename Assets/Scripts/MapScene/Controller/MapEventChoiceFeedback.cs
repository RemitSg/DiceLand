using UnityEngine;
using UnityEngine.EventSystems;

public sealed class MapEventChoiceFeedback : MonoBehaviour, IPointerEnterHandler, ISelectHandler
{
    private MapEventOverlay owner;
    private MapEventOptionData option;
    public void Initialize(MapEventOverlay overlay, MapEventOptionData data) { owner = overlay; option = data; }
    public void OnPointerEnter(PointerEventData data) => owner?.PreviewOption(option);
    public void OnSelect(BaseEventData data) => owner?.PreviewOption(option);
}
