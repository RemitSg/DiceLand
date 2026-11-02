using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class BattleActionButtonLookTests
{
    private GameObject root;
    private RectTransform canvasRect, rect;
    private Button button;
    private Image image;
    private BattleButtonSpriteStateSwitcher switcher;
    private BattleActionButtonLook look;
    private Texture2D texture;
    private Sprite originalIdle, originalPressed, idle, pressed;

    [SetUp]
    public void SetUp()
    {
        root = new GameObject("Action button fixture", typeof(RectTransform), typeof(Canvas));
        root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        canvasRect = root.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(1920, 1080);
        GameObject item = new GameObject("Ready", typeof(RectTransform), typeof(Image), typeof(Button));
        item.transform.SetParent(root.transform, false);
        rect = item.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0);
        rect.pivot = Vector2.one * 0.5f;
        rect.sizeDelta = new Vector2(300, 100);
        rect.anchoredPosition = new Vector2(0, 50.5f);
        button = item.GetComponent<Button>();
        image = item.GetComponent<Image>();
        button.targetGraphic = image;
        switcher = item.AddComponent<BattleButtonSpriteStateSwitcher>();
        texture = new Texture2D(2, 2);
        originalIdle = Sprite.Create(texture, new Rect(0, 0, 2, 2), Vector2.one * 0.5f);
        originalPressed = Sprite.Create(texture, new Rect(0, 0, 2, 2), Vector2.one * 0.5f);
        idle = Resources.Load<Sprite>("BattleUI/ReadyButtonGold_Unpressed");
        pressed = Resources.Load<Sprite>("BattleUI/ReadyButtonGold_Pressed");
        Assert.IsNotNull(idle, "The production unpressed sprite must be packaged in Resources.");
        Assert.IsNotNull(pressed, "The production pressed sprite must be packaged in Resources.");
        switcher.unpressedSprite = originalIdle;
        switcher.pressedSprite = originalPressed;
        switcher.targetImage = image;
        typeof(BattleButtonSpriteStateSwitcher).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(switcher, null);
        image.sprite = originalIdle;
        look = item.AddComponent<BattleActionButtonLook>();
    }

    [TearDown]
    public void TearDown()
    {
        if (root != null) Object.DestroyImmediate(root);
        if (originalIdle != null) Object.DestroyImmediate(originalIdle);
        if (originalPressed != null) Object.DestroyImmediate(originalPressed);
        if (texture != null) Object.DestroyImmediate(texture);
    }

    [TestCase(1920, 1080, 0, 0)]
    [TestCase(2400, 1080, 110, 36)]
    [TestCase(1600, 1200, 64, 24)]
    [TestCase(600, 1080, 32, 20)]
    public void FullArtworkFitsTheSafeAreaAndKeepsItsAspect(int width, int height, int notch, int bottom)
    {
        canvasRect.sizeDelta = new Vector2(width, height);
        look.Configure(button, idle, pressed);
        Rect safe = new Rect(notch, bottom, width - notch * 2, height - bottom);
        look.ApplyLayout(safe, new Vector2(width, height));
        Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(canvasRect, rect);
        Vector2 safeMin = canvasRect.rect.min + new Vector2(safe.xMin / width * canvasRect.rect.width,
            safe.yMin / height * canvasRect.rect.height);
        Vector2 safeMax = canvasRect.rect.min + new Vector2(safe.xMax / width * canvasRect.rect.width,
            safe.yMax / height * canvasRect.rect.height);
        Assert.Greater(bounds.min.y, safeMin.y);
        Assert.GreaterOrEqual(bounds.min.x, safeMin.x);
        Assert.LessOrEqual(bounds.max.x, safeMax.x);
        Assert.LessOrEqual(bounds.max.y, safeMax.y);
        Assert.That(rect.rect.width / rect.rect.height, Is.EqualTo(idle.rect.width / idle.rect.height).Within(0.001f));
        Assert.That(bounds.center.x, Is.EqualTo((safeMin.x + safeMax.x) * 0.5f).Within(0.01f));
        Assert.That(rect.rect.width, Is.LessThanOrEqualTo(450f));
    }

    [Test]
    public void ExistingPressReleaseExitAndClickHandlersRetainTheirBehavior()
    {
        int clicks = 0;
        button.onClick.AddListener(() => clicks++);
        look.Configure(button, idle, pressed);
        PointerEventData pointer = new PointerEventData(null) { button = PointerEventData.InputButton.Left };
        switcher.OnPointerDown(pointer);
        Assert.AreSame(pressed, image.sprite);
        switcher.OnPointerExit(pointer);
        Assert.AreSame(idle, image.sprite);
        switcher.OnPointerDown(pointer);
        switcher.OnPointerUp(pointer);
        Assert.AreSame(idle, image.sprite);
        ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerClickHandler);
        Assert.AreEqual(1, clicks);
        button.interactable = false;
        switcher.OnPointerDown(pointer);
        ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerClickHandler);
        Assert.AreSame(idle, image.sprite);
        Assert.AreEqual(1, clicks);
        Assert.AreSame(image, button.targetGraphic);
        Assert.IsTrue(image.raycastTarget);
        Assert.AreEqual(1, button.GetComponentsInChildren<Graphic>(true).Length, "Artwork must not add another input surface.");
    }

    [Test]
    public void ResolvingHidesTheButtonAndRestoresTheExistingGroupState()
    {
        CanvasGroup group = button.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0.65f;
        group.interactable = false;
        look.Configure(button, idle, pressed);
        look.SetResolvingPresentation(true);
        Assert.AreEqual(0, group.alpha);
        Assert.IsFalse(group.blocksRaycasts);
        Assert.IsFalse(group.interactable);
        Assert.IsTrue(button.interactable, "The presentation must not overwrite the button's own interactable setting.");
        look.SetResolvingPresentation(false);
        Assert.AreEqual(0.65f, group.alpha);
        Assert.IsTrue(group.blocksRaycasts);
        Assert.IsFalse(group.interactable);
        group.alpha = 0.8f;
        group.blocksRaycasts = false;
        group.interactable = true;
        look.SetResolvingPresentation(true);
        look.ReleaseLook();
        Assert.AreEqual(0.8f, group.alpha);
        Assert.IsFalse(group.blocksRaycasts);
        Assert.IsTrue(group.interactable);
    }

    [Test]
    public void ReconfigurationAndReleaseRestoreTheOriginalArtAndRect()
    {
        look.Configure(button, idle, pressed);
        look.Configure(button, idle, pressed);
        look.ReleaseLook();
        Assert.AreSame(originalIdle, switcher.unpressedSprite);
        Assert.AreSame(originalPressed, switcher.pressedSprite);
        Assert.AreSame(originalIdle, image.sprite);
        Assert.AreEqual(new Vector2(300, 100), rect.sizeDelta);
        Assert.AreEqual(new Vector2(0, 50.5f), rect.anchoredPosition);
        Assert.AreEqual(new Vector2(0.5f, 0), rect.anchorMin);
        Assert.AreEqual(rect.anchorMin, rect.anchorMax);
        Assert.AreEqual(Vector2.one * 0.5f, rect.pivot);
        look.Configure(button, idle, pressed);
        Assert.AreSame(idle, image.sprite);
        look.ReleaseLook();
        Assert.AreSame(originalIdle, image.sprite);
    }
}
