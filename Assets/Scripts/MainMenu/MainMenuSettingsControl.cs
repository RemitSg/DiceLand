using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Unscaled focus feedback and a persistent thumb for native toggle controls.</summary>
public sealed class MainMenuSettingsControl : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
{
    private Selectable control;
    private MainMenuSettingsGraphic face;
    private RectTransform thumb;
    private Text stateText;
    private CanvasGroup appearance;
    private bool hovered,pressed,focused,hasState,lastState;
    private float brightness=1,scale=1;

    public void Bind(Selectable selectable,MainMenuSettingsGraphic graphic,RectTransform grip=null,Text state=null)
    {
        control=selectable; face=graphic; thumb=grip; stateText=state;
        appearance=GetComponent<CanvasGroup>()??gameObject.AddComponent<CanvasGroup>();
        Refresh(true);
    }

    public void Refresh(bool immediate=false)
    {
        if(control==null) return;
        if(appearance!=null) appearance.alpha=control.interactable?1:.52f;
        Toggle toggle=control as Toggle;
        if(toggle==null || thumb==null) return;
        bool on=toggle.isOn;
        if(!hasState || lastState!=on)
        {
            hasState=true; lastState=on; face.SetState(on,true);
            if(stateText!=null) { stateText.text=on?"开启":"关闭"; stateText.color=on?new Color32(232,236,200,255):new Color32(167,184,161,255); }
        }
        float half=thumb.rect.width*.5f+6;
        float x=on?((RectTransform)transform).rect.width-half:half;
        float ease=immediate?1:1-Mathf.Exp(-24*Time.unscaledDeltaTime);
        thumb.anchoredPosition=Vector2.Lerp(thumb.anchoredPosition,new Vector2(x,0),ease);
        if(stateText!=null) stateText.rectTransform.anchoredPosition=new Vector2(on?-18:18,0);
    }

    private void OnEnable() { hovered=pressed=focused=false; brightness=scale=1; Refresh(true); }
    private void OnDisable()
    {
        if(thumb!=null) thumb.localScale=Vector3.one;
        if(face!=null) face.canvasRenderer.SetColor(Color.white);
    }
    private void Update()
    {
        if(control==null) return;
        Refresh();
        bool active=control.IsInteractable();
        float ease=1-Mathf.Exp(-22*Time.unscaledDeltaTime);
        brightness=Mathf.Lerp(brightness,active&&(hovered||focused)?1.15f:1,ease);
        if(face!=null) face.canvasRenderer.SetColor(new Color(brightness,brightness,brightness,1));
        scale=Mathf.Lerp(scale,active&&pressed ? .94f : active&&(hovered||focused) ? 1.04f : 1,ease);
        if(thumb!=null) thumb.localScale=Vector3.one*scale;
    }
    public void OnPointerEnter(PointerEventData data) { hovered=true; }
    public void OnPointerExit(PointerEventData data) { hovered=pressed=false; }
    public void OnPointerDown(PointerEventData data) { if(data.button==PointerEventData.InputButton.Left) pressed=true; }
    public void OnPointerUp(PointerEventData data) { pressed=false; }
    public void OnSelect(BaseEventData data) { focused=true; }
    public void OnDeselect(BaseEventData data) { focused=pressed=false; }
}
