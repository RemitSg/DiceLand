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

public sealed class MapEventOverlayPlayModeTests
{
    private GameObject host;
    private Component overlay;
    private ScriptableObject data;
    private EventSystem events;
    private float timeScale;
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    [SetUp]
    public void SetUp()
    {
        timeScale = Time.timeScale;
        host = new GameObject("EventPanelPlayTest");
        overlay = host.AddComponent(TypeOf("MapEventOverlay"));
        data = ScriptableObject.CreateInstance(TypeOf("MapEvent_SO"));
        Set(data, "eventName", "Event panel");
        Set(data, "eventContent", string.Join("\n\n", Enumerable.Repeat("A long narrative passage, kept in the reading pane at its original font size.", 40)));
        IList options = (IList)Get(data, "options");
        for (int i = 0; i < 8; i++)
        {
            object option = Activator.CreateInstance(TypeOf("MapEventOptionData"));
            Set(option, "optionLabel", "Choice " + (i + 1));
            Set(option, "consequenceDescription", "Reward for choice " + (i + 1));
            options.Add(option);
        }
        events = EventSystem.current != null ? EventSystem.current : host.AddComponent<EventSystem>();
        Invoke("Show", null, data, null, null);
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        Time.timeScale = timeScale;
        UnityEngine.Object.Destroy(host);
        UnityEngine.Object.Destroy(data);
        yield return null;
    }

    [UnityTest]
    public IEnumerator NativeRaycastSelectsChoiceAndBackdropBlocksUnderlyingMap()
    {
        yield return null;
        yield return null;
        Canvas canvas = host.GetComponentInChildren<Canvas>();
        Assert.AreEqual(RenderMode.ScreenSpaceOverlay, canvas.renderMode);
        Click(Find("Option_02"));
        yield return null;
        Assert.AreEqual("Reward for choice 2", Get(Find("ConsequenceText").GetComponent(TypeOf("TMPro.TextMeshProUGUI")), "text"));
        List<RaycastResult> hits = Hits(new Vector2(5f, 5f));
        Assert.IsNotEmpty(hits);
        Assert.AreEqual("EventOverlayCanvas", hits[0].gameObject.name, "The full-screen backdrop owns outside-panel input.");
    }

    [UnityTest]
    public IEnumerator StoryChoicesAndPreviewScrollIndependentlyWhilePaused()
    {
        Time.timeScale = 0f;
        yield return null;
        yield return null;
        ScrollRect story = Find("ContentScrollRect").GetComponent<ScrollRect>();
        ScrollRect choices = Find("OptionScrollRect").GetComponent<ScrollRect>();
        ScrollRect preview = Find("ConsequenceScrollRect").GetComponent<ScrollRect>();
        Assert.IsTrue(story.verticalScrollbar.gameObject.activeSelf);
        Assert.IsTrue(choices.verticalScrollbar.gameObject.activeSelf);
        Assert.IsFalse(preview.verticalScrollbar.gameObject.activeSelf);
        float choiceStart = choices.content.anchoredPosition.y;
        story.OnScroll(new PointerEventData(events) { scrollDelta = new Vector2(0f, -6f) });
        yield return null;
        Assert.Greater(story.content.anchoredPosition.y, 0f);
        Assert.AreEqual(choiceStart, choices.content.anchoredPosition.y, 0.1f);
        float storyPosition = story.content.anchoredPosition.y;
        choices.OnScroll(new PointerEventData(events) { scrollDelta = new Vector2(0f, -10f) });
        yield return null;
        Assert.Greater(choices.content.anchoredPosition.y, choiceStart);
        Assert.AreEqual(storyPosition, story.content.anchoredPosition.y, 0.1f);
        Assert.AreEqual(0f, preview.content.anchoredPosition.y, 0.1f);
        Set(((IList)Get(data, "options"))[0], "consequenceDescription", string.Join("\n", Enumerable.Repeat("A complete preview line.", 20)));
        Invoke("Show", null, data, null, null);
        yield return null;
        yield return null;
        preview.OnScroll(new PointerEventData(events) { scrollDelta = new Vector2(0f, -8f) });
        yield return null;
        Assert.Greater(preview.content.anchoredPosition.y, 0f);
        Assert.AreEqual(0f, story.content.anchoredPosition.y, 0.1f);
        Assert.AreEqual(0f, choices.content.anchoredPosition.y, 0.1f);
    }

    [UnityTest]
    public IEnumerator ResultRestoresReadingPositionAndNativeContinueClosesOnce()
    {
        yield return null;
        yield return null;
        int hidden = 0;
        overlay.GetType().GetEvent("Hidden").AddEventHandler(overlay, (Action)(() => hidden++));
        ScrollRect story = Find("ContentScrollRect").GetComponent<ScrollRect>();
        float initialHeight = story.viewport.rect.height;
        story.verticalNormalizedPosition = 0f;
        object option = ((IList)Get(data, "options"))[0];
        Invoke("ShowResultStep", option, "Completed.", "The selected event result.", null);
        yield return null;
        Assert.Greater(story.viewport.rect.height, initialHeight + 150f);
        Assert.AreEqual(0f, story.content.anchoredPosition.y, 0.1f);
        Assert.IsFalse(Find("OptionScrollRect").gameObject.activeSelf);
        Click(Find("ConfirmButton"));
        yield return null;
        Assert.IsFalse((bool)Get(overlay, "IsVisible"));
        Assert.AreEqual(1, hidden);
        Invoke("Hide");
        Assert.AreEqual(1, hidden);
    }

    private void Click(Transform target)
    {
        Canvas.ForceUpdateCanvases();
        RectTransform rect = (RectTransform)target;
        Vector2 point = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
        List<RaycastResult> hits = Hits(point);
        Assert.IsNotEmpty(hits);
        Assert.IsTrue(hits[0].gameObject.transform.IsChildOf(target) || hits[0].gameObject.transform == target, "Top raycast must hit the intended native control: " + hits[0].gameObject.name);
        ExecuteEvents.ExecuteHierarchy(hits[0].gameObject, new PointerEventData(events) { position = point, button = PointerEventData.InputButton.Left }, ExecuteEvents.pointerClickHandler);
    }
    private List<RaycastResult> Hits(Vector2 position)
    {
        List<RaycastResult> hits = new List<RaycastResult>();
        events.RaycastAll(new PointerEventData(events) { position = position }, hits);
        return hits;
    }
    private Transform Find(string name) { return host.GetComponentsInChildren<Transform>(true).First(t => t.name == name); }
    private object Invoke(string name, params object[] args) { return overlay.GetType().GetMethod(name, Members).Invoke(overlay, args); }
    private static Type TypeOf(string name) { return AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name)).First(t => t != null); }
    private static void Set(object target, string name, object value) { target.GetType().GetField(name, Members).SetValue(target, value); }
    private static object Get(object target, string name)
    {
        FieldInfo field = target.GetType().GetField(name, Members);
        return field != null ? field.GetValue(target) : target.GetType().GetProperty(name, Members).GetValue(target);
    }
}
