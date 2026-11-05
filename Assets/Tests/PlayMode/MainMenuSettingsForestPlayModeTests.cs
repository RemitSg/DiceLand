#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object=UnityEngine.Object;

public sealed class MainMenuSettingsForestPlayModeTests
{
    private const BindingFlags Members=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    private GameObject host;
    private Component overlay;
    private EventSystem events;
    private object settings,saved;
    private int saves;
    private float previousScale;

    [SetUp]
    public void SetUp()
    {
        previousScale=Time.timeScale; Time.timeScale=0; saves=0; saved=null;
        host=new GameObject("SettingsPlayFixture"); overlay=host.AddComponent(TypeOf("MainMenuSettingsOverlay"));
        settings=Activator.CreateInstance(TypeOf("MainMenuSettingsData")); Set(settings,"musicVolume",.6f); Set(settings,"sfxVolume",.8f); Set(settings,"vibrationEnabled",false);
        Type database=TypeOf("UnityEditor.AssetDatabase");
        Font font=(Font)database.GetMethod("LoadAssetAtPath",new[]{typeof(string),typeof(Type)}).Invoke(null,new object[]{"Assets/Assets/Fonts/Hi字楷体简.ttf",typeof(Font)});
        Invoke(overlay,"ConfigureForestFont",font);
        Delegate callback=(Delegate)GetType().GetMethod("MakeCallback",Members).MakeGenericMethod(settings.GetType()).Invoke(this,null);
        Invoke(overlay,"Show",settings,callback); events=EventSystem.current;
        // Native Dropdown places its popup at 30000; keep the fixture below that canvas.
        ((Canvas)Get(overlay,"PresentationCanvas")).sortingOrder=29900;
    }
    private Action<T> MakeCallback<T>() => value=>{ saves++; saved=value; };
    [UnityTearDown]
    public IEnumerator TearDown() { Time.timeScale=previousScale; Object.Destroy(host); yield return null; yield return null; }

    [UnityTest]
    public IEnumerator PausedEntranceRejectsEarlySaveAndNativeSaveRunsOnlyOnceThroughTheClose()
    {
        Button save=(Button)Get(overlay,"saveButton"); save.onClick.Invoke(); Assert.AreEqual(0,saves);
        yield return Settled();
        Assert.Greater(((Canvas)Get(overlay,"PresentationCanvas")).GetComponent<CanvasGroup>().alpha,.99f);
        Click(save.transform); Click(save.transform);
        Assert.AreEqual(1,saves); Assert.IsTrue((bool)Get(overlay,"IsVisible"));
        Assert.AreEqual("SettingsBackdrop",Hits(new Vector2(3,3))[0].gameObject.name);
        yield return new WaitForSecondsRealtime(.45f);
        Assert.IsFalse((bool)Get(overlay,"IsVisible"));
        Assert.AreEqual(.6f,(float)Get(saved,"musicVolume"),.001f);
    }

    [UnityTest]
    public IEnumerator NativeSliderDragAndToggleClickUpdateReadoutsWithoutSaving()
    {
        yield return Settled();
        Slider slider=(Slider)Get(overlay,"musicSlider"); RectTransform rect=(RectTransform)slider.transform;
        Vector2 point=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(new Vector2(rect.rect.xMin+rect.rect.width*.28f,0)));
        var pointer=new PointerEventData(events){position=point,button=PointerEventData.InputButton.Left};
        ExecuteEvents.Execute(slider.gameObject,pointer,ExecuteEvents.pointerDownHandler);
        pointer.position=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(new Vector2(rect.rect.xMin+rect.rect.width*.74f,0)));
        ExecuteEvents.Execute(slider.gameObject,pointer,ExecuteEvents.dragHandler); ExecuteEvents.Execute(slider.gameObject,pointer,ExecuteEvents.pointerUpHandler);
        Assert.Greater(slider.value,.7f); Assert.Less(slider.value,.82f);
        Assert.AreEqual(Mathf.RoundToInt(slider.value*100)+"%",((Text)Get(overlay,"musicValueText")).text);
        Toggle toggle=(Toggle)Get(overlay,"vibrationToggle"); Assert.IsFalse(toggle.isOn);
        RectTransform thumb=(RectTransform)toggle.transform.Find("Thumb"); float before=thumb.anchoredPosition.x;
        Click(toggle.transform); yield return new WaitForSecondsRealtime(.18f);
        Assert.IsTrue(toggle.isOn); Assert.AreEqual("开启",toggle.transform.Find("State").GetComponent<Text>().text);
        Assert.Greater(thumb.anchoredPosition.x,before+50); Assert.AreEqual(0,saves);
    }

    [UnityTest]
    public IEnumerator NativeDropdownScrollAndSelectionKeepTheFullFrameRateChoices()
    {
        yield return Settled();
        Dropdown field=(Dropdown)Get(overlay,"frameRateDropdown"); Click(field.transform);
        yield return new WaitForSecondsRealtime(.2f);
        Transform popup=field.transform.Find("Dropdown List"); Assert.IsNotNull(popup);
        ScrollRect scroll=popup.GetComponent<ScrollRect>();
        scroll.verticalNormalizedPosition=0; Canvas.ForceUpdateCanvases(); yield return null;
        Toggle last=popup.GetComponentsInChildren<Toggle>().First(t=>t.GetComponentInChildren<Text>().text=="144 FPS");
        Click(last.transform); yield return new WaitForSecondsRealtime(.2f);
        Assert.AreEqual("144 FPS",field.options[field.value].text); Assert.AreEqual(6,field.options.Count); Assert.AreEqual(0,saves);
        Click(((Button)Get(overlay,"saveButton")).transform);
        Assert.AreEqual(1,saves); Assert.AreEqual(144,(int)Get(saved,"targetFrameRate"));
    }

    [UnityTest]
    public IEnumerator ReturnReopensOriginalDraftAndFocusFeedbackStaysCenteredWhilePaused()
    {
        yield return Settled();
        ((Slider)Get(overlay,"musicSlider")).value=.1f;
        Transform close=((Button)Get(overlay,"closeButton")).transform;
        Vector3 center=close.TransformPoint(((RectTransform)close).rect.center);
        ExecuteEvents.Execute(close.gameObject,new PointerEventData(events),ExecuteEvents.pointerEnterHandler);
        yield return new WaitForSecondsRealtime(.16f); Assert.Greater(close.localScale.x,1.01f);
        Assert.Less(Vector3.Distance(center,close.TransformPoint(((RectTransform)close).rect.center)),.01f);
        Click(close); yield return new WaitForSecondsRealtime(.45f); Assert.AreEqual(0,saves);
        Invoke(overlay,"Show",settings,null); yield return Settled();
        Assert.AreEqual(.6f,((Slider)Get(overlay,"musicSlider")).value,.001f);
        Assert.IsTrue((bool)Get(overlay,"IsVisible"));
    }

    private IEnumerator Settled() { float deadline=Time.realtimeSinceStartup+3; while(Time.realtimeSinceStartup<deadline) { var motion=((Canvas)Get(overlay,"PresentationCanvas")).GetComponent(TypeOf("MainMenuPageMotion")); if(motion!=null&&!(bool)Get(motion,"IsTransitioning")) yield break; yield return null; } Assert.Fail("Settings page did not finish its unscaled transition."); }
    private void Click(Transform target)
    {
        Canvas.ForceUpdateCanvases(); RectTransform rect=(RectTransform)target; Vector2 point=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center));
        var hits=Hits(point); Assert.IsNotEmpty(hits);
        Assert.IsTrue(hits[0].gameObject.transform==target||hits[0].gameObject.transform.IsChildOf(target),hits[0].gameObject.name);
        ExecuteEvents.ExecuteHierarchy(hits[0].gameObject,new PointerEventData(events){position=point,button=PointerEventData.InputButton.Left},ExecuteEvents.pointerClickHandler);
    }
    private List<RaycastResult> Hits(Vector2 point) { var result=new List<RaycastResult>(); events.RaycastAll(new PointerEventData(events){position=point},result); return result; }
    private static Type TypeOf(string name) => AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(name)).First(t=>t!=null);
    private static object Invoke(object value,string method,params object[] args) => value.GetType().GetMethod(method,Members).Invoke(value,args);
    private static object Get(object value,string name) { var field=value.GetType().GetField(name,Members); return field!=null?field.GetValue(value):value.GetType().GetProperty(name,Members).GetValue(value); }
    private static void Set(object value,string name,object fieldValue) => value.GetType().GetField(name,Members).SetValue(value,fieldValue);
}
#endif
