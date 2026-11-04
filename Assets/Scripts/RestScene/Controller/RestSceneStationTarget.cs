using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>A real UGUI target for a painted station. Dispatches only through the existing rest controllers.</summary>
[AddComponentMenu("")]
public sealed class RestSceneStationTarget : Button
{
    [SerializeField] private RestSceneForestView owner;
    [SerializeField] private RestSceneForestView.Station station;
    private bool pointerOver, selected, pressed;

    public RestSceneForestView Owner => owner;
    public RestSceneForestView.Station Station => station;
    public bool HasFocus => pointerOver || selected;
    public bool IsPointerPressed => pressed;

    public void Configure(RestSceneForestView view, RestSceneForestView.Station id)
    {
        owner = view;
        station = id;
        transition = Transition.None;
    }

    protected override void Awake()
    {
        base.Awake();
        onClick.AddListener(Dispatch);
    }

    private void Dispatch()
    {
        if (owner != null)
            owner.TryActivateStation(station);
    }

    public override void OnPointerEnter(PointerEventData eventData) { base.OnPointerEnter(eventData); pointerOver = true; }
    public override void OnPointerExit(PointerEventData eventData) { base.OnPointerExit(eventData); pointerOver = false; pressed = false; }
    public override void OnPointerDown(PointerEventData eventData)
    {
        base.OnPointerDown(eventData);
        if (eventData.button == PointerEventData.InputButton.Left && IsInteractable()) pressed = true;
    }
    public override void OnPointerUp(PointerEventData eventData) { base.OnPointerUp(eventData); pressed = false; }
    public override void OnSelect(BaseEventData eventData) { base.OnSelect(eventData); selected = !(eventData is PointerEventData); }
    public override void OnDeselect(BaseEventData eventData) { base.OnDeselect(eventData); selected = false; }
    protected override void OnDisable()
    {
        pointerOver = selected = pressed = false;
        base.OnDisable();
    }
}
