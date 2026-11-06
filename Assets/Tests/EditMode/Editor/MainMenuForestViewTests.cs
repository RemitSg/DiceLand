using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using MenuAction = MainMenuSceneController.MenuAction;

public class MainMenuForestViewTests
{
    private GameObject host;
    private MainMenuForestView view;
    private DifficultyConfig_SO easy, hard;
    private readonly List<MenuAction> actions = new List<MenuAction>();
    private readonly List<DifficultyConfig_SO> starts = new List<DifficultyConfig_SO>();

    [SetUp]
    public void SetUp()
    {
        easy = ScriptableObject.CreateInstance<DifficultyConfig_SO>();
        easy.difficultyName = "Easy"; easy.boardSize = 7;
        hard = ScriptableObject.CreateInstance<DifficultyConfig_SO>();
        hard.difficultyName = "Hard"; hard.boardSize = 11;
        host = new GameObject("MainMenuForestTestHost");
        view = host.AddComponent<MainMenuForestView>();
        view.Build(null, null, new[] { easy, hard }, action =>
        {
            actions.Add(action);
            if (action == MenuAction.NewGame) view.ShowDifficulty();
        }, starts.Add);
        actions.Clear(); starts.Clear();
    }

    [TearDown]
    public void TearDown()
    {
        if (host != null) Object.DestroyImmediate(host);
        Object.DestroyImmediate(easy); Object.DestroyImmediate(hard);
    }

    [TestCase(MenuAction.NewGame)]
    [TestCase(MenuAction.Continue)]
    [TestCase(MenuAction.Ability)]
    [TestCase(MenuAction.Encyclopedia)]
    [TestCase(MenuAction.Tutorial)]
    [TestCase(MenuAction.Settings)]
    [TestCase(MenuAction.Contributors)]
    [TestCase(MenuAction.Exit)]
    [TestCase(MenuAction.DemoTest)]
    public void NativeButton_RoutesExistingEntryExactlyOnce(MenuAction action)
    {
        view.SetContinueAvailable(true);
        Click(view.GetButton(action));
        CollectionAssert.AreEqual(new[] { action }, actions);
    }

    [Test]
    public void Continue_RejectsMissingSaveAndRecoversWhenSaveBecomesAvailable()
    {
        view.SetContinueAvailable(false);
        Click(view.GetButton(MenuAction.Continue));
        Assert.IsEmpty(actions);
        Assert.AreEqual("暂无存档", view.GetButton(MenuAction.Continue).transform.Find("SaveStatus").GetComponent<Text>().text);
        view.SetContinueAvailable(true);
        Click(view.GetButton(MenuAction.Continue));
        CollectionAssert.AreEqual(new[] { MenuAction.Continue }, actions);
    }

    [Test]
    public void NewGame_SelectsActualConfigAndRequiresConfirmationThenPreventsDoubleStart()
    {
        Click(view.GetButton(MenuAction.NewGame));
        Assert.IsTrue(view.IsDifficultyVisible);
        Transform page = view.Canvas.transform.Find("SafeArea/Composition/Difficulty");
        Click(page.Find("Difficulty_1").GetComponent<Button>());
        Assert.IsEmpty(starts);
        Button start = page.Find("StartAdventure").GetComponent<Button>();
        Click(start);
        // Even a queued duplicate invocation must not delete/reset a run twice.
        start.onClick.Invoke();
        CollectionAssert.AreEqual(new[] { hard }, starts);
    }

    [Test]
    public void Back_ReturnsToAllMenuEntriesWithoutStartingRun()
    {
        view.ShowDifficulty();
        Transform page = view.Canvas.transform.Find("SafeArea/Composition/Difficulty");
        Click(page.Find("Back").GetComponent<Button>());
        Assert.IsFalse(view.IsDifficultyVisible);
        Assert.IsEmpty(starts);
        Assert.IsTrue(view.GetButton(MenuAction.NewGame).gameObject.activeInHierarchy);
        Assert.IsTrue(view.GetButton(MenuAction.DemoTest).gameObject.activeInHierarchy);
    }

    [Test]
    public void BlockingOverlay_SuppressesMenuInputUntilReleased()
    {
        view.SetInputEnabled(false);
        view.GetButton(MenuAction.Ability).onClick.Invoke();
        view.GetButton(MenuAction.NewGame).onClick.Invoke();
        Assert.IsEmpty(actions);
        Assert.IsFalse(view.IsDifficultyVisible);
        Assert.IsFalse(view.Canvas.transform.Find("SafeArea").GetComponent<CanvasGroup>().blocksRaycasts);
        view.SetInputEnabled(true);
        Click(view.GetButton(MenuAction.Settings));
        CollectionAssert.AreEqual(new[] { MenuAction.Settings }, actions);
    }

    [TestCase(2340,1080,90,0)]
    [TestCase(1920,1080,0,0)]
    [TestCase(1600,1200,0,24)]
    public void Layout_KeepsEveryEntryInsideSafeArea(int width, int height, int horizontalInset, int verticalInset)
    {
        Canvas canvas = view.Canvas;
        canvas.GetComponent<CanvasScaler>().enabled = false;
        canvas.renderMode = RenderMode.WorldSpace;
        RectTransform root = (RectTransform)canvas.transform;
        root.sizeDelta = new Vector2(1080f * width / height, 1080f);
        Rect safe = new Rect(horizontalInset, verticalInset, width - horizontalInset * 2, height - verticalInset * 2);
        view.ApplyLayout(new Vector2(width,height), safe);
        Canvas.ForceUpdateCanvases();
        Vector3[] corners = new Vector3[4];
        foreach (MenuAction action in new[] { MenuAction.NewGame,MenuAction.Continue,MenuAction.Ability,MenuAction.Tutorial,
            MenuAction.Settings,MenuAction.Contributors,MenuAction.Exit,MenuAction.DemoTest })
        {
            ((RectTransform)view.GetButton(action).transform).GetWorldCorners(corners);
            foreach (Vector3 corner in corners)
            {
                Vector3 local = root.InverseTransformPoint(corner);
                Vector2 pixel = new Vector2((local.x + root.rect.width * 0.5f) * height / 1080f,
                    (local.y + 540f) * height / 1080f);
                Assert.That(pixel.x, Is.InRange(safe.xMin-1f,safe.xMax+1f), action + " horizontal");
                Assert.That(pixel.y, Is.InRange(safe.yMin-1f,safe.yMax+1f), action + " vertical");
            }
        }
    }

    [Test]
    public void DestroyingOwner_RemovesIndependentCanvas()
    {
        Canvas owned = view.Canvas;
        Object.DestroyImmediate(host);
        Assert.IsTrue(owned == null);
    }

    [Test]
    public void Hover_UsesOneNonBlockingFrameAndRetargetsWithoutTeleporting()
    {
        RectTransform frame = Frame();
        Assert.IsFalse(frame.GetComponent<Graphic>().raycastTarget);
        Vector2 start = frame.anchoredPosition;
        view.Hover(MenuAction.Settings);
        Assert.AreEqual(start, frame.anchoredPosition);
        Step(5);
        Assert.Less(frame.anchoredPosition.y, start.y);
        Assert.Greater(frame.anchoredPosition.y, ((RectTransform)view.GetButton(MenuAction.Settings).transform).anchoredPosition.y);
        Vector2 midFlight = frame.anchoredPosition;
        view.Hover(MenuAction.Ability);
        Assert.AreEqual(midFlight, frame.anchoredPosition, "Retargeting must preserve the current pose.");
        Step(45);
        Assert.AreEqual(((RectTransform)view.GetButton(MenuAction.Ability).transform).anchoredPosition, frame.anchoredPosition);
        Assert.Less(view.GetButton(MenuAction.Ability).transform.Find("Label").GetComponent<Text>().color.r, 0.4f);
        Assert.Greater(view.GetButton(MenuAction.NewGame).transform.Find("Label").GetComponent<Text>().color.r, 0.9f);
    }

    [Test]
    public void Hover_IgnoresDisabledContinueAndKeepsFocusBetweenRows()
    {
        Vector2 initial = Frame().anchoredPosition;
        view.Hover(MenuAction.Continue);
        Step(40);
        Assert.AreEqual(initial, Frame().anchoredPosition);
        view.Hover(MenuAction.Tutorial);
        view.LeaveHover(MenuAction.Tutorial);
        Step(40);
        Assert.AreEqual(((RectTransform)view.GetButton(MenuAction.Tutorial).transform).anchoredPosition, Frame().anchoredPosition);
    }

    [Test]
    public void ContinueBecomesUnavailable_ReturnsFrameToNewGame()
    {
        view.SetContinueAvailable(true);
        view.Hover(MenuAction.Continue);
        Step(40);
        view.SetContinueAvailable(false);
        Step(40);
        Assert.AreEqual(((RectTransform)view.GetButton(MenuAction.NewGame).transform).anchoredPosition, Frame().anchoredPosition);
    }

    [Test]
    public void Press_LeavingButtonReleasesFrameCompression()
    {
        view.SetPressed(MenuAction.NewGame, true);
        Step(12);
        Assert.Less(Frame().localScale.x, 0.99f);
        view.LeaveHover(MenuAction.NewGame);
        Step(30);
        Assert.That(Frame().localScale.x, Is.EqualTo(1f).Within(0.001f));
    }

    [Test]
    public void ExternalPage_DisablesMenuAndPreservesHiddenPoseAcrossResize()
    {
        view.SetExternalPageVisible(true);
        view.GetButton(MenuAction.Ability).onClick.Invoke();
        view.GetButton(MenuAction.NewGame).onClick.Invoke();
        view.Hover(MenuAction.Settings);
        view.ApplyLayout(new Vector2(1600, 1200), new Rect(0, 24, 1600, 1152));
        Assert.IsEmpty(actions);
        Assert.AreEqual(0f, view.Canvas.transform.Find("SafeArea").GetComponent<CanvasGroup>().alpha);
        Assert.IsFalse(view.IsDifficultyVisible);
        view.SetExternalPageVisible(false);
        Click(view.GetButton(MenuAction.Ability));
        CollectionAssert.AreEqual(new[] { MenuAction.Ability }, actions);
        Assert.AreEqual(1f, view.Canvas.transform.Find("SafeArea").GetComponent<CanvasGroup>().alpha);
    }

    private RectTransform Frame() => view.Canvas.transform.Find("SafeArea/Composition/Menu/SelectionFrame").GetComponent<RectTransform>();

    private void Step(int frames)
    {
        MethodInfo tick = typeof(MainMenuForestView).GetMethod("TickMotion", BindingFlags.Instance | BindingFlags.NonPublic);
        for (int i = 0; i < frames; i++) tick.Invoke(view, new object[] { 1f / 60f });
    }

    private static void Click(Button button)
    {
        Assert.IsNotNull(button);
        button.OnPointerClick(new PointerEventData(null) { button = PointerEventData.InputButton.Left });
    }
}
