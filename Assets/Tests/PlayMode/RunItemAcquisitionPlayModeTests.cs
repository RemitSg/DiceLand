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
using Object = UnityEngine.Object;

public sealed class RunItemAcquisitionPlayModeTests
{
    private const BindingFlags Members = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    private GameObject host;
    private Component overlay, detail, globalOverlay;
    private object state, font;
    private ScriptableObject relic, blueprint;
    private EventSystem events;
    private float originalTimeScale;

    [SetUp]
    public void SetUp()
    {
        originalTimeScale = Time.timeScale; Time.timeScale = 0;
        host = new GameObject("Acquisition play fixture"); overlay = host.AddComponent(TypeOf("RunItemAcquisitionOverlay"));
        state = Activator.CreateInstance(TypeOf("RunState"));
        relic = ScriptableObject.CreateInstance(TypeOf("Relic_SO")); Set(relic,"relicName","新获得的遗物");
        Set(relic,"description",string.Join("\n",Enumerable.Repeat("遗物完整效果与触发条件。",25)));
        Set(relic,"additionalDescription","完整补充条件。");
        blueprint = ScriptableObject.CreateInstance(TypeOf("CraftingBlueprint_SO")); Set(blueprint,"blueprintName","新获得的图纸");
        Invoke(state,"AddRelic",relic,2,true); Invoke(state,"AddBlueprint",blueprint,1);
        Type database = TypeOf("UnityEditor.AssetDatabase");
        string path=(string)database.GetMethod("GUIDToAssetPath",new[]{typeof(string)}).Invoke(null,new object[]{"c6e7143cd2df45b45a784ef8c61a210f"});
        font=database.GetMethod("LoadAssetAtPath",new[]{typeof(string),typeof(Type)}).Invoke(null,new object[]{path,TypeOf("TMPro.TMP_FontAsset")});
        Invoke(overlay,"Configure",state,font,font); Invoke(overlay,"ProcessPending");
        detail=(Component)Get(overlay,"DetailView"); events=EventSystem.current;
        ((Canvas)Get(overlay,"PresentationCanvas")).sortingOrder=31000;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        Time.timeScale=originalTimeScale;
        if(globalOverlay!=null) Object.Destroy(globalOverlay.gameObject);
        Object.Destroy(host); Object.Destroy(relic); Object.Destroy(blueprint); yield return null;
    }

    [UnityTest]
    public IEnumerator PausedOpeningGuardsAgainstClickThroughAndClosingShowsEveryReward()
    {
        Invoke(detail,"Hide");
        Assert.AreEqual(2,Pending()); Assert.IsTrue((bool)Get(overlay,"IsVisible"));
        var panel = (CanvasGroup)Get(detail,"panelGroup");
        yield return WaitForPresentation(() => panel.alpha > .99f, "The relic should finish opening while paused.");
        Assert.AreEqual("获得遗物",Text("Heading"));
        Click(Find("CloseButton"));
        Assert.AreEqual(2,Pending());
        Assert.AreEqual("AcquiredItemDetailModal",Hits(new Vector2(3,3))[0].gameObject.name);
        yield return WaitForPresentation(() => Pending() == 1, "Closing the relic should present the next reward.");
        Assert.AreEqual(1,Pending()); Assert.AreEqual("获得图纸",Text("Heading"));
        Assert.AreEqual(2,Invoke(state,"GetRelicAmount",relic)); Assert.AreEqual(1,Invoke(state,"GetBlueprintAmount",blueprint));
        yield return WaitForPresentation(() => panel.alpha > .99f, "The blueprint should finish opening before it is clicked.");
        Click(Find("CloseButton"));
        yield return WaitForPresentation(() => Pending() == 0, "Closing the blueprint should acknowledge the last reward.");
        Assert.AreEqual(0,Pending()); Assert.IsFalse((bool)Get(overlay,"IsVisible"));
    }

    [UnityTest]
    public IEnumerator OutsideClickAndDescriptionScrollingCannotAccidentallyAcknowledge()
    {
        yield return new WaitForSecondsRealtime(.32f);
        List<RaycastResult> outside=Hits(new Vector2(3,3));
        ExecuteEvents.ExecuteHierarchy(outside[0].gameObject,new PointerEventData(events){button=PointerEventData.InputButton.Left},ExecuteEvents.pointerClickHandler);
        ScrollRect scroll=(ScrollRect)Get(detail,"descriptionScroll");
        scroll.OnScroll(new PointerEventData(events){scrollDelta=new Vector2(0,-8)}); yield return null;
        Assert.AreEqual(2,Pending()); Assert.IsTrue((bool)Get(overlay,"IsVisible")); Assert.Less(scroll.verticalNormalizedPosition,.99f);
        Assert.IsFalse(((Button)Get(detail,"actionButton")).gameObject.activeSelf);
    }

    [UnityTest]
    public IEnumerator ANewGrantDuringClosingRemainsQueuedAndDisablingNeverAcknowledgesIt()
    {
        yield return new WaitForSecondsRealtime(.32f);
        Click(Find("CloseButton")); Invoke(state,"AddRelic",relic,1,true);
        yield return new WaitForSecondsRealtime(.24f);
        Assert.AreEqual("获得图纸",Text("Heading")); Assert.AreEqual(2,Pending());
        host.SetActive(false);
        Assert.AreEqual(2,Pending());
        host.SetActive(true); Invoke(overlay,"ProcessPending");
        Assert.AreEqual("获得图纸",Text("Heading"));
        var panel = (CanvasGroup)Get(detail,"panelGroup");
        yield return WaitForPresentation(() => panel.alpha > .99f, "The reopened blueprint must finish opening before it is clicked.");
        yield return WaitForPresentation(() => ((Func<bool>)Get(detail,"DismissAllowed"))(), "The reopened reward must finish its click-through protection before dismissal.");
        Click(Find("CloseButton"));
        yield return WaitForPresentation(() => Text("Heading") == "获得遗物", "Closing the blueprint should show the relic granted during the previous close.");
        Assert.AreEqual("获得遗物",Text("Heading")); Assert.AreEqual("本次获得 ×1",Text("Quantity"));
        Assert.AreEqual(3,Invoke(state,"GetRelicAmount",relic));
    }

    [UnityTest]
    public IEnumerator GlobalPresenterWaitsForMapModalAndKeepsReceiptsAcrossOwnerAndSnapshotChanges()
    {
        ((Behaviour)overlay).enabled=false;
        EventTrigger owner=host.AddComponent<EventTrigger>();
        bool canPresent=false;
        Type presenter=TypeOf("RunItemAcquisitionOverlay");
        presenter.GetMethod("BindToMap").Invoke(null,new object[]{owner,state,font,font,(Func<bool>)(()=>canPresent)});
        globalOverlay=(Component)presenter.GetField("instance",BindingFlags.NonPublic|BindingFlags.Static).GetValue(null);
        Invoke(globalOverlay,"ProcessPending");
        Assert.IsFalse((bool)Get(globalOverlay,"IsVisible")); Assert.AreEqual(2,Pending());
        canPresent=true; Invoke(globalOverlay,"ProcessPending");
        Assert.IsTrue((bool)presenter.GetProperty("IsBlockingInput").GetValue(null));
        object snapshot=Invoke(state,"Clone");
        presenter.GetMethod("BindToMap").Invoke(null,new object[]{owner,snapshot,font,font,(Func<bool>)(()=>canPresent)});
        Invoke(globalOverlay,"ProcessPending");
        Assert.IsTrue((bool)Get(globalOverlay,"IsVisible")); Assert.AreEqual(2,Pending());
        owner.enabled=false; Invoke(globalOverlay,"ProcessPending");
        Assert.IsFalse((bool)Get(globalOverlay,"IsVisible")); Assert.AreEqual(2,Pending());
        owner.enabled=true; Invoke(globalOverlay,"ProcessPending");
        yield return new WaitForSecondsRealtime(.3f);
        Assert.IsTrue((bool)Get(globalOverlay,"IsVisible")); Assert.AreEqual(2,Pending());
    }

    private int Pending() => (int)Get(Get(state,"ItemAcquisitions"),"PendingCount");
    private static IEnumerator WaitForPresentation(Func<bool> condition, string failure)
    {
        float deadline = Time.realtimeSinceStartup + 3f;
        while (!condition() && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.IsTrue(condition(), failure);
    }

    private Transform Find(string name) => ((RectTransform)Get(detail,"ModalRoot")).GetComponentsInChildren<Transform>(true).First(t=>t.name==name);
    private string Text(string name) => (string)Get(Find(name).GetComponent(TypeOf("TMPro.TMP_Text")),"text");
    private void Click(Transform target)
    {
        Canvas.ForceUpdateCanvases(); RectTransform rect=(RectTransform)target;
        Vector2 point=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center));
        var hits=Hits(point); Assert.IsNotEmpty(hits);
        Assert.IsTrue(hits[0].gameObject.transform==target || hits[0].gameObject.transform.IsChildOf(target),hits[0].gameObject.name);
        ExecuteEvents.ExecuteHierarchy(hits[0].gameObject,new PointerEventData(events){position=point,button=PointerEventData.InputButton.Left},ExecuteEvents.pointerClickHandler);
    }
    private List<RaycastResult> Hits(Vector2 point) { var hits=new List<RaycastResult>(); events.RaycastAll(new PointerEventData(events){position=point},hits); return hits; }
    private static Type TypeOf(string name) => AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(name)).First(t=>t!=null);
    private static object Invoke(object target,string method,params object[] args) => target.GetType().GetMethod(method,Members).Invoke(target,args);
    private static object Get(object target,string name) { var property=target.GetType().GetProperty(name,Members); return property!=null ? property.GetValue(target) : target.GetType().GetField(name,Members).GetValue(target); }
    private static void Set(object target,string name,object value) => target.GetType().GetField(name,Members).SetValue(target,value);
}
#endif
