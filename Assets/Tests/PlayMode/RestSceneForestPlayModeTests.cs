#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class RestSceneForestPlayModeTests
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private Scene scene;
    private Component view;
    private object[] visuals;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/RestScene.scene", new LoadSceneParameters(LoadSceneMode.Additive));
        scene = SceneManager.GetSceneByPath("Assets/Scenes/RestScene.scene");
        view = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<MonoBehaviour>(true)).Single(c => c != null && c.GetType().Name == "RestSceneForestView");
        visuals = ((IEnumerable)view.GetType().GetProperty("Stations").GetValue(view)).Cast<object>().ToArray();
        Static("RestSceneRuntime", "BeginVisit", "WaystationTestReturn", Vector2Int.one, null, null);
        yield return null;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        foreach (var visual in visuals) Close(Controller(visual));
        Static("RestSceneRuntime", "Clear");
        if (scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene);
    }

    [UnityTest]
    public IEnumerator SignsOpenOriginalModalsAndCancelPreservesPendingVisit()
    {
        foreach (var visual in visuals)
        {
            var sign = (Button)Get(visual, "plaque");
            sign.onClick.Invoke();
            Component controller = Controller(visual);
            Assert.AreEqual(true, Static("RestSceneModalState", "IsLockedBy", controller));
            Assert.IsTrue(controller.GetComponentsInChildren<Canvas>(true).Any(c => c.gameObject.activeInHierarchy));
            Close(controller);
            yield return null;
            Assert.AreEqual(false, TypeOf("RestSceneModalState").GetProperty("IsLocked").GetValue(null));
            Assert.AreEqual(true, TypeOf("RestSceneRuntime").GetProperty("HasPendingVisit").GetValue(null));
        }
    }

    [UnityTest]
    public IEnumerator HoverUsesEachOriginalOutlineAndModalsRejectQueuedOtherActions()
    {
        Component first = Controller(visuals[0]);
        foreach (var visual in visuals)
        {
            Component controller = Controller(visual);
            Invoke(controller, "SetPresentationFocus", true);
            var outline = controller.transform.Find("HoverOutline").GetComponent<SpriteRenderer>();
            Assert.IsTrue(outline.enabled);
            Assert.AreSame(controller.GetComponent<SpriteRenderer>().sprite, outline.sprite);
        }
        ((Button)Get(visuals[0], "plaque")).onClick.Invoke();
        foreach (var visual in visuals.Skip(1)) ((Button)Get(visual, "plaque")).onClick.Invoke();
        yield return null;
        Assert.AreEqual(true, Static("RestSceneModalState", "IsLockedBy", first));
        foreach (var visual in visuals)
            Assert.IsFalse(Controller(visual).transform.Find("HoverOutline").GetComponent<SpriteRenderer>().enabled);
        Close(first);
        yield return null;
        Invoke(first, "SetPresentationFocus", true);
        Assert.IsTrue(first.transform.Find("HoverOutline").GetComponent<SpriteRenderer>().enabled);
    }

    [UnityTest]
    public IEnumerator SignAndFurnitureHoverShareTheOriginalSpriteOutline()
    {
        foreach (var visual in visuals)
        {
            Component controller = Controller(visual);
            var sign = (Button)Get(visual, "plaque");
            var data = new PointerEventData(EventSystem.current);
            ExecuteEvents.Execute(sign.gameObject, data, ExecuteEvents.pointerEnterHandler);
            yield return null;
            var outline = controller.transform.Find("HoverOutline").GetComponent<SpriteRenderer>();
            Assert.IsTrue(outline.enabled);
            ExecuteEvents.Execute(sign.gameObject, data, ExecuteEvents.pointerExitHandler);
            controller.SendMessage("OnMouseExit");
            yield return null;
            Assert.IsFalse(outline.enabled);
            controller.SendMessage("OnMouseEnter");
            yield return null;
            Assert.IsTrue(outline.enabled);
            controller.SendMessage("OnMouseExit");
        }
    }

    [UnityTest]
    public IEnumerator ModalShadeCoversTheScreenAndSlicedButtonsKeepAVisibleCenter()
    {
        foreach (var visual in visuals)
        {
            ((Button)Get(visual, "plaque")).onClick.Invoke();
            yield return null;
            Component controller = Controller(visual);
            Canvas canvas = (Canvas)controller.GetType().GetField("canvas", Members).GetValue(controller);
            var shade = (RectTransform)canvas.transform.Find("ForestModalShade");
            Assert.NotNull(shade);
            var corners = new Vector3[4]; shade.GetWorldCorners(corners);
            Assert.That(corners[0].x, Is.EqualTo(0).Within(1f));
            Assert.That(corners[0].y, Is.EqualTo(0).Within(1f));
            Assert.That(corners[2].x, Is.EqualTo(Screen.width).Within(1f));
            Assert.That(corners[2].y, Is.EqualTo(Screen.height).Within(1f));
            foreach (Button button in canvas.GetComponentsInChildren<Button>(true))
            {
                Image image = button.GetComponent<Image>();
                if (image.sprite == null || image.sprite.name != "GildedButton") continue;
                float pixelsPerUnit = image.sprite.pixelsPerUnit / canvas.referencePixelsPerUnit * image.pixelsPerUnitMultiplier;
                Assert.That(image.rectTransform.rect.width - (image.sprite.border.x + image.sprite.border.z) / pixelsPerUnit,
                    Is.GreaterThan(20f), button.name + " must retain room for its label.");
            }
            Close(controller);
            yield return null;
        }
    }

    [UnityTest]
    public IEnumerator MissingVisitAndDisabledControllerRejectSignActivation()
    {
        var visual = visuals[0]; Component controller = Controller(visual);
        Static("RestSceneRuntime", "Clear");
        Assert.AreEqual(false, Invoke(view, "TryActivateStation", Get(visual, "station")));
        Static("RestSceneRuntime", "BeginVisit", "WaystationTestReturn", Vector2Int.one, null, null);
        ((Behaviour)controller).enabled = false;
        Assert.AreEqual(false, Invoke(view, "TryActivateStation", Get(visual, "station")));
        ((Behaviour)controller).enabled = true;
        yield return null;
        Assert.AreEqual(true, Invoke(view, "TryActivateStation", Get(visual, "station")));
    }

    private Component Controller(object visual) => (Component)Invoke(view, "Controller", Get(visual, "station"));
    private static void Close(Component controller)
    {
        string method = controller.GetType().Name.Contains("Reforge") ? "HideAllPanels" : controller.GetType().Name.Contains("Recruit") ? "HidePanel" : "HidePrompt";
        Invoke(controller, method);
    }
    private static object Get(object target, string field) => target.GetType().GetField(field, Members).GetValue(target);
    private static Type TypeOf(string name) => AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name)).First(t => t != null);
    private static object Static(string type, string method, params object[] args) => TypeOf(type).GetMethods(BindingFlags.Static | BindingFlags.Public).Single(m => m.Name == method && m.GetParameters().Length == args.Length).Invoke(null, args);
    private static object Invoke(object target, string method, params object[] args) => target.GetType().GetMethods(Members).Single(m => m.Name == method && m.GetParameters().Length == args.Length).Invoke(target, args);
}
#endif
