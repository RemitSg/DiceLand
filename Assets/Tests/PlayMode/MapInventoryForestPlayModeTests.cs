#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public sealed class MapInventoryForestPlayModeTests
{
    private const BindingFlags Members = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    private GameObject host;
    private Component hud;
    private float timeScale;
    private ScriptableObject loot;
    private object savedSnapshot;
    private Scene restScene;
    private FieldInfo Snapshot => TypeOf("MapRunRuntime").GetField("currentSnapshot", BindingFlags.NonPublic | BindingFlags.Static);

    [SetUp] public void SetUp()
    {
        timeScale = Time.timeScale; savedSnapshot = Snapshot.GetValue(null);
        host = new GameObject("InventoryMotionTests");
        if (EventSystem.current == null) host.AddComponent<EventSystem>();
        hud = host.AddComponent(TypeOf("MapHUD")); Call(hud, "InitializeInventoryOnly");
        object state = Activator.CreateInstance(TypeOf("RunState"));
        loot = ScriptableObject.CreateInstance(TypeOf("LootItem_SO")); Set(loot, "lootName", "很长的资源名称与完整后缀");
        Call(state, "AddLoot", loot, 12345);
        Call(hud, "Refresh", state, null); Call(hud, "ShowInventoryPanel", -1);
    }

    [UnityTearDown] public IEnumerator TearDown()
    {
        Time.timeScale = timeScale;
        if (restScene.IsValid() && restScene.isLoaded) yield return SceneManager.UnloadSceneAsync(restScene);
        Static("RestSceneRuntime", "Clear"); Snapshot.SetValue(null, savedSnapshot);
        Object.Destroy(host); Object.Destroy(loot); yield return null;
    }

    [UnityTest] public IEnumerator PausedOpenAndCloseKeepBackdropUntilCompleteAndReleaseItOnce()
    {
        Time.timeScale = 0;
        yield return new WaitForSecondsRealtime(0.3f);
        Assert.Greater(((CanvasGroup)Get(hud, "inventoryGroup")).alpha, 0.99f);
        int hidden = 0; hud.GetType().GetEvent("InventoryHidden").AddEventHandler(hud, (Action)(() => hidden++));
        Call(hud, "CloseInventoryPanel");
        Assert.IsTrue((bool)Get(hud, "IsInventoryVisible")); Assert.AreEqual(0, hidden);
        var hits = new List<RaycastResult>();
        EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = new Vector2(2, 2) }, hits);
        Assert.IsTrue(hits.Any(hit => hit.gameObject.name == "InventoryModal"));
        yield return new WaitForSecondsRealtime(0.22f);
        Assert.IsFalse((bool)Get(hud, "IsInventoryVisible")); Assert.AreEqual(1, hidden);
        Call(hud, "HideInventoryPanel"); Assert.AreEqual(1, hidden);
        Assert.IsFalse(((RectTransform)Get(hud, "inventoryLayer")).gameObject.activeSelf);
    }

    [UnityTest] public IEnumerator ReopenCancelsCloseAndDetailsReturnToTheSameCollection()
    {
        yield return new WaitForSecondsRealtime(0.12f);
        Call(hud, "CloseInventoryPanel"); Call(hud, "ShowInventoryPanel", -1);
        yield return new WaitForSecondsRealtime(0.28f);
        Assert.IsTrue((bool)Get(hud, "IsInventoryVisible"));
        Assert.Greater(((CanvasGroup)Get(hud, "inventoryGroup")).alpha, 0.99f);
        host.GetComponentsInChildren<Button>().Single(b => b.name == "Slot_0").onClick.Invoke();
        yield return new WaitForSecondsRealtime(0.28f);
        Component detail = (Component)Get(hud, "runtimeInventoryDetailView");
        Assert.IsTrue((bool)Get(detail, "IsVisible"));
        Call(hud, "HideInventoryDetail"); yield return new WaitForSecondsRealtime(0.22f);
        Assert.IsFalse((bool)Get(detail, "IsVisible"));
        Assert.IsTrue((bool)Get(hud, "IsInventoryVisible"));
        Assert.AreEqual(1, ((IList)Get(hud, "inventorySlotObjects")).Count);
    }

    [UnityTest] public IEnumerator AuthoredRestSceneEntrySharesModalLockAndPersistsEquipmentWithoutConsumingRest()
    {
        Call(hud, "HideInventoryPanel");
        object state = Static("MapInventoryVisualTools", "CreateState");
        ScriptableObject character = ScriptableObject.CreateInstance(TypeOf("CharacterData_SO"));
        try
        {
            object weapon = Get(((IList)Get(state, "weaponInventory"))[0], "weaponData");
            Set(character, "compatibleWeaponType", Get(weapon, "weaponType"));
            object member = Activator.CreateInstance(TypeOf("RunPartyMemberState"));
            Set(member, "memberId", "InventoryRestMember"); Set(member, "characterData", character);
            Set(member, "currentHP", 3); Set(member, "maxHP", 20);
            ((IList)Get(state, "partyMembers")).Add(member);
            Set(state, "money", 53);
            object snapshot = Activator.CreateInstance(TypeOf("MapRunSnapshot"));
            Set(snapshot, "sceneName", "InventoryRestReturn"); Set(snapshot, "runState", state); Set(snapshot, "actionPointCurrent", 2);
            Static("MapRunRuntime", "RestoreSnapshot", snapshot);
            Static("RestSceneRuntime", "BeginVisit", "InventoryRestReturn", Vector2Int.one, null, null);
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/RestScene.scene", new LoadSceneParameters(LoadSceneMode.Additive));
            restScene = SceneManager.GetSceneByPath("Assets/Scenes/RestScene.scene");
            yield return null; yield return null;
            Component entryOwner = restScene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<MonoBehaviour>(true)).Single(c => c != null && c.GetType().Name == "RestSceneInventory");
            Component restHud = (Component)Get(entryOwner, "hud");
            Button entry = (Button)Get(restHud, "inventoryButton"); Assert.IsTrue(entry.interactable);
            entry.onClick.Invoke(); yield return new WaitForSecondsRealtime(0.28f);
            Assert.AreEqual(true, Static("RestSceneModalState", "IsLockedBy", entryOwner));
            ((Button)Get(restHud, "weaponTabButton")).onClick.Invoke();
            ((IList)Get(restHud, "inventorySlotObjects")).Cast<GameObject>().First().GetComponent<Button>().onClick.Invoke();
            Call(restHud, "OnEquipSelectedWeapon");
            Call(restHud, "CloseInventoryPanel"); yield return new WaitForSecondsRealtime(0.22f);
            Assert.AreEqual(false, TypeOf("RestSceneModalState").GetProperty("IsLocked").GetValue(null));
            Assert.IsTrue(entry.interactable);
            object live = Get(Snapshot.GetValue(null), "runState");
            Assert.AreSame(weapon, Get(((IList)Get(live, "partyMembers"))[0], "weaponData"));
            Assert.AreEqual(3, Get(((IList)Get(live, "partyMembers"))[0], "currentHP"));
            Assert.AreEqual(53, Get(live, "money")); Assert.AreEqual(2, Get(Snapshot.GetValue(null), "actionPointCurrent"));
            Assert.AreEqual(true, TypeOf("RestSceneRuntime").GetProperty("HasPendingVisit").GetValue(null));
        }
        finally { Object.Destroy(character); }
    }

    private static Type TypeOf(string name) => AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name)).First(t => t != null);
    private static object Get(object target, string name) => target.GetType().GetProperty(name, Members)?.GetValue(target) ?? target.GetType().GetField(name, Members).GetValue(target);
    private static void Set(object target, string name, object value) => target.GetType().GetField(name, Members).SetValue(target, value);
    private static object Call(object target, string method, params object[] args) => target.GetType().GetMethods(Members).Single(m => m.Name == method && m.GetParameters().Length == args.Length).Invoke(target, args);
    private static object Static(string type, string method, params object[] args) => TypeOf(type).GetMethods(BindingFlags.Public | BindingFlags.Static).Single(m => m.Name == method && m.GetParameters().Length == args.Length).Invoke(null, args);
}
#endif
