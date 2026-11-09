#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public sealed class MapWeaponDetailPlayModeTests
{
    private const BindingFlags Members = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    private GameObject host;
    private Component hud, view;
    private object state, member;
    private ScriptableObject weapon, skill, character;
    private float oldTimeScale;

    [UnitySetUp] public IEnumerator SetUp()
    {
        oldTimeScale=Time.timeScale;
        host=new GameObject("WeaponDetailsPlayTests");
        if(EventSystem.current==null) host.AddComponent<EventSystem>();
        hud=host.AddComponent(TypeOf("MapHUD")); Call(hud,"InitializeInventoryOnly");
        var source=(ScriptableObject)AssetDatabase.LoadAssetAtPath("Assets/Data/Weapons/006 曦誓剑/006 曦誓剑.asset",TypeOf("Weapon_SO"));
        weapon=Object.Instantiate(source);
        Array skills=(Array)((Array)Get(weapon,"skillPool")).Clone();
        skill=Object.Instantiate((ScriptableObject)skills.GetValue(0));
        Set(skill,"localizationKey","weapon-details-play-long-test");
        Set(skill,"description",string.Join("\n",Enumerable.Repeat("保留完整的技能与词条说明，滚动查看不会影响装备操作。",32)));
        skills.SetValue(skill,0); Set(weapon,"skillPool",skills);
        state=Activator.CreateInstance(TypeOf("RunState")); Call(state,"AddWeapon",weapon,1);
        character=ScriptableObject.CreateInstance(TypeOf("CharacterData_SO"));
        Set(character,"characterName","Tester"); Set(character,"compatibleWeaponType",Get(weapon,"weaponType"));
        member=Activator.CreateInstance(TypeOf("RunPartyMemberState"));
        Set(member,"memberId","WeaponDetailsTester"); Set(member,"characterData",character); Set(member,"maxHP",20); Set(member,"currentHP",20);
        ((IList)Get(state,"partyMembers")).Add(member);
        Call(hud,"Refresh",state,null); Call(hud,"ShowInventoryPanel",-1);
        ((Button)Get(hud,"weaponTabButton")).onClick.Invoke();
        yield return new WaitForSecondsRealtime(.3f);
        Click(host.GetComponentsInChildren<Button>().Single(button=>button.name=="Slot_0").transform);
        view=(Component)Get(hud,"runtimeInventoryDetailView");
        yield return new WaitForSecondsRealtime(.3f);
        object card=((IList)Get(view,"weaponCards"))[0];
        Click((RectTransform)Get(card,"rect"));
        yield return new WaitForSecondsRealtime(.24f);
    }

    [UnityTearDown] public IEnumerator TearDown()
    {
        Time.timeScale=oldTimeScale;
        Object.Destroy(host); Object.Destroy(weapon); Object.Destroy(skill); Object.Destroy(character);
        yield return null;
    }

    [UnityTest] public IEnumerator PausedWheelAndTouchScrollReadLongCardsAndCloseReturnsToInventory()
    {
        Time.timeScale=0;
        ScrollRect scroll=(ScrollRect)Get(view,"weaponScroll");
        Assert.Greater(scroll.content.rect.height,scroll.viewport.rect.height);
        Canvas.ForceUpdateCanvases();
        Vector2 point=RectTransformUtility.WorldToScreenPoint(null,scroll.viewport.TransformPoint(scroll.viewport.rect.center));
        var pointer=new PointerEventData(EventSystem.current){position=point,scrollDelta=new Vector2(0,-4),button=PointerEventData.InputButton.Left};
        scroll.OnScroll(pointer); yield return null;
        Assert.Greater(scroll.content.anchoredPosition.y,0);
        scroll.OnInitializePotentialDrag(pointer); scroll.OnBeginDrag(pointer);
        pointer.position+=new Vector2(0,80); scroll.OnDrag(pointer); scroll.OnEndDrag(pointer);
        yield return new WaitForSecondsRealtime(.08f);
        Assert.IsTrue((bool)Get(view,"IsVisible"));
        Click(view.transform.Find("CloseButton"));
        Assert.IsTrue((bool)Get(view,"IsVisible"));
        yield return new WaitForSecondsRealtime(.22f);
        Assert.IsFalse((bool)Get(view,"IsVisible"));
        Assert.IsTrue((bool)Get(hud,"IsInventoryVisible"));
        Assert.IsFalse(((RectTransform)Get(view,"modalRoot")).gameObject.activeSelf);
    }

    [UnityTest] public IEnumerator EquipmentButtonUsesOriginalInventoryActionAndPreservesTheReadingPosition()
    {
        ScrollRect scroll=(ScrollRect)Get(view,"weaponScroll");
        scroll.StopMovement(); scroll.verticalNormalizedPosition=.25f; Canvas.ForceUpdateCanvases();
        float position=scroll.verticalNormalizedPosition;
        Assert.IsNull(Get(member,"weaponData"));
        Button action=(Button)Get(view,"actionButton"); Assert.IsTrue(action.interactable);
        Click(action.transform); yield return null;
        Assert.AreSame(weapon,Get(member,"weaponData"));
        Assert.AreEqual(1,Call(state,"GetWeaponAmount",weapon));
        Assert.IsFalse(action.interactable);
        Assert.That(scroll.verticalNormalizedPosition,Is.EqualTo(position).Within(.02f));
        StringAssert.Contains("Tester",Get(Get(view,"actionStatusText"),"text").ToString());
        Click(view.transform.Find("CloseXButton")); yield return new WaitForSecondsRealtime(.22f);
        Assert.IsFalse((bool)Get(view,"IsVisible")); Assert.IsTrue((bool)Get(hud,"IsInventoryVisible"));
    }

    private static void Click(Transform target)
    {
        Canvas.ForceUpdateCanvases(); var rect=(RectTransform)target;
        Vector2 point=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center));
        var pointer=new PointerEventData(EventSystem.current){position=point,button=PointerEventData.InputButton.Left};
        var hits=new List<RaycastResult>(); EventSystem.current.RaycastAll(pointer,hits); Assert.IsNotEmpty(hits);
        ExecuteEvents.ExecuteHierarchy(hits[0].gameObject,pointer,ExecuteEvents.pointerClickHandler);
    }
    private static Type TypeOf(string name)=>AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(name)).First(t=>t!=null);
    private static object Get(object target,string name)=>target.GetType().GetField(name,Members)?.GetValue(target)??target.GetType().GetProperty(name,Members)?.GetValue(target);
    private static void Set(object target,string name,object value)=>target.GetType().GetField(name,Members).SetValue(target,value);
    private static object Call(object target,string method,params object[] args)=>target.GetType().GetMethods(Members).Single(m=>m.Name==method&&m.GetParameters().Length==args.Length).Invoke(target,args);
}
#endif
