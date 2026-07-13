using System;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MapBattleTilePreviewOverlayTests
{
    private GameObject root;
    private MapBattleTilePreviewOverlay overlay;
    private int[] preExistingEventSystemIds;

    [SetUp]
    public void SetUp()
    {
        preExistingEventSystemIds = UnityEngine.Object
            .FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Select(system => system.GetInstanceID())
            .ToArray();
        root = new GameObject("MapBattleTilePreviewOverlay");
        overlay = root.AddComponent<MapBattleTilePreviewOverlay>();
    }

    [TearDown]
    public void TearDown()
    {
        if (root != null)
            UnityEngine.Object.DestroyImmediate(root);

        EventSystem[] systems = UnityEngine.Object.FindObjectsByType<EventSystem>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        for (int i = 0; i < systems.Length; i++)
        {
            EventSystem system = systems[i];
            if (system != null && !preExistingEventSystemIds.Contains(system.GetInstanceID()))
                UnityEngine.Object.DestroyImmediate(system.gameObject);
        }
    }

    [Test]
    public void Show_BattleContextDisplaysAllowedPreviewOnly()
    {
        MapBattleTilePreviewContext context = CreateContext(MapTileType.Battle, true);
        context.tile.previewEnemyCount = 17;
        context.tile.previewTag = "EnemyGroupOmega LegendaryReward seed-98765";
        context.tile.encounterSeed = 98765;
        context.dangerRating = 6;
        context.attributeNames = new[] { "Fire", "Dark" };
        context.confirmLabel = "Enter Battle - 2 AP";

        overlay.Show(context);

        string rendered = AllRenderedText();
        Assert.That(rendered, Does.Contain("\u666e\u901a"));
        Assert.That(rendered, Does.Contain(MapDangerRatingService.GetRatingPhrase(6)));
        Assert.That(rendered, Does.Contain("6/8"));
        Assert.That(rendered, Does.Contain("Fire"));
        Assert.That(rendered, Does.Contain("Dark"));
        Assert.That(rendered, Does.Contain("Enter Battle - 2 AP"));
        Assert.That(rendered, Does.Not.Contain("17"));
        Assert.That(rendered, Does.Not.Contain("EnemyGroupOmega"));
        Assert.That(rendered, Does.Not.Contain("LegendaryReward"));
        Assert.That(rendered, Does.Not.Contain("98765"));

        Canvas canvas = root.GetComponentInChildren<Canvas>(true);
        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        Assert.AreEqual(RenderMode.ScreenSpaceOverlay, canvas.renderMode);
        Assert.AreEqual(8010, canvas.sortingOrder);
        Assert.AreEqual(CanvasScaler.ScaleMode.ScaleWithScreenSize, scaler.uiScaleMode);
        Assert.AreEqual(new Vector2(1920f, 1080f), scaler.referenceResolution);
    }

    [Test]
    public void Show_UsesAtMostThreeExactAttributeLabelsWithoutCountInference()
    {
        MapBattleTilePreviewContext context = CreateContext(MapTileType.Elite, true);
        context.tile.previewEnemyCount = 999;
        context.attributeNames = new[] { "Fire", "Dark", "Water", "HiddenFourth" };

        overlay.Show(context);

        TextMeshProUGUI[] attributes = root.GetComponentsInChildren<TextMeshProUGUI>(true)
            .Where(text => text.name.StartsWith("Attribute", StringComparison.Ordinal))
            .Where(text => text.name.Length == "Attribute0".Length && char.IsDigit(text.name[text.name.Length - 1]))
            .Where(text => text.gameObject.activeSelf)
            .ToArray();
        CollectionAssert.AreEqual(new[] { "Fire", "Dark", "Water" }, attributes.Select(text => text.text).ToArray());
        Assert.That(AllRenderedText(), Does.Not.Contain("HiddenFourth"));
        Assert.That(AllRenderedText(), Does.Not.Contain("999"));
        Assert.That(AllRenderedText(), Does.Not.Contain("+1"));
    }

    [Test]
    public void Show_LockedBossDisplaysReasonAndDisablesOnlyConfirm()
    {
        MapBattleTilePreviewContext context = CreateContext(MapTileType.Boss, false);
        context.blockReason = "Boss locked: 1/3 keys";

        overlay.Show(context);

        Assert.That(FindText("BattleClassText").text, Does.Contain("Boss"));
        Assert.That(FindText("StatusText").text, Does.Contain("1/3"));
        Assert.IsFalse(FindButton("ConfirmButton").interactable);
        Assert.IsTrue(FindButton("ReturnButton").interactable);
    }

    [Test]
    public void Show_NullContextIsVisibleWithUnavailableReasonAndDisabledConfirm()
    {
        Assert.DoesNotThrow(() => overlay.Show(null));

        Assert.IsTrue(overlay.IsVisible);
        Assert.IsFalse(FindButton("ConfirmButton").interactable);
        Assert.IsTrue(FindButton("ReturnButton").interactable);
        Assert.That(FindText("StatusText").text, Does.Contain("Unavailable"));
    }

    [Test]
    public void InteractionLockAndFailureUpdateBothActionsAndPreciseStatus()
    {
        overlay.Show(CreateContext(MapTileType.Elite, true));

        overlay.SetInteractionEnabled(false);

        Assert.IsFalse(FindButton("ConfirmButton").interactable);
        Assert.IsFalse(FindButton("ReturnButton").interactable);

        overlay.ShowFailure("Transition failed: AP changed");

        Assert.IsTrue(FindButton("ConfirmButton").interactable);
        Assert.IsTrue(FindButton("ReturnButton").interactable);
        Assert.AreEqual("Transition failed: AP changed", FindText("StatusText").text);
    }

    [Test]
    public void ShowFailure_RestoresReturnButKeepsContextBlockedConfirmDisabled()
    {
        overlay.Show(CreateContext(MapTileType.Boss, false));
        overlay.SetInteractionEnabled(false);

        overlay.ShowFailure("Battle could not start");

        Assert.IsFalse(FindButton("ConfirmButton").interactable);
        Assert.IsTrue(FindButton("ReturnButton").interactable);
    }

    [Test]
    public void ShowFailure_WhileHiddenDoesNotCreateUiOrRetainFailureState()
    {
        overlay.ShowFailure("Invisible stale failure");

        Assert.IsFalse(overlay.IsVisible);
        Assert.AreEqual(0, root.GetComponentsInChildren<Canvas>(true).Length);

        overlay.Show(CreateContext(MapTileType.Battle, true));

        Assert.That(FindText("StatusText").text, Does.Not.Contain("Invisible stale failure"));
    }

    [Test]
    public void Show_CreatesOrReusesExactlyOneEventSystem()
    {
        EventSystem preferredBefore = EventSystem.current;
        if (preferredBefore == null)
        {
            preferredBefore = UnityEngine.Object
                .FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault();
        }

        overlay.Show(CreateContext(MapTileType.Battle, true));

        EventSystem[] systems = UnityEngine.Object.FindObjectsByType<EventSystem>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        Assert.AreEqual(1, systems.Length);
        if (preferredBefore != null)
            Assert.AreSame(preferredBefore, systems[0]);
    }

    [Test]
    public void ButtonsFireOncePerClickAcrossRepeatedShowAndCancelDoesNotHide()
    {
        int confirms = 0;
        int cancels = 0;
        overlay.ConfirmRequested += () => confirms++;
        overlay.CancelRequested += () => cancels++;

        overlay.Show(CreateContext(MapTileType.Battle, true));
        overlay.Show(CreateContext(MapTileType.Elite, true));
        overlay.Show(CreateContext(MapTileType.Boss, true));
        FindButton("ConfirmButton").onClick.Invoke();
        FindButton("ReturnButton").onClick.Invoke();

        Assert.AreEqual(1, confirms);
        Assert.AreEqual(1, cancels);
        Assert.IsTrue(overlay.IsVisible);
    }

    [Test]
    public void HideClearsVisibilityAndRepeatShowDoesNotDuplicateObjectsOrListeners()
    {
        int confirms = 0;
        overlay.ConfirmRequested += () => confirms++;
        overlay.Show(CreateContext(MapTileType.Battle, true));
        int firstObjectCount = root.GetComponentsInChildren<Transform>(true).Length;

        overlay.Hide();
        overlay.Show(CreateContext(MapTileType.Elite, true));
        int secondObjectCount = root.GetComponentsInChildren<Transform>(true).Length;
        FindButton("ConfirmButton").onClick.Invoke();

        Assert.IsTrue(overlay.IsVisible);
        Assert.AreEqual(firstObjectCount, secondObjectCount);
        Assert.AreEqual(1, confirms);
        overlay.Hide();
        Assert.IsFalse(overlay.IsVisible);
    }

    [Test]
    public void ApplyResponsiveLayout_AnchorsLandscapeRightAndPortraitBottom()
    {
        overlay.Show(CreateContext(MapTileType.Battle, true));
        RectTransform panel = FindRect("PreviewPanel");

        overlay.ApplyResponsiveLayout(new Vector2(1920f, 1080f));

        Assert.AreEqual(new Vector2(1f, 0.5f), panel.anchorMin);
        Assert.AreEqual(new Vector2(1f, 0.5f), panel.anchorMax);
        Assert.AreEqual(new Vector2(1f, 0.5f), panel.pivot);
        Assert.That(panel.sizeDelta.x, Is.EqualTo(500f).Within(0.01f));
        Assert.That(panel.sizeDelta.y, Is.EqualTo(620f).Within(0.01f));

        overlay.ApplyResponsiveLayout(new Vector2(1080f, 1920f));

        Assert.AreEqual(new Vector2(0.5f, 0f), panel.anchorMin);
        Assert.AreEqual(new Vector2(0.5f, 0f), panel.anchorMax);
        Assert.AreEqual(new Vector2(0.5f, 0f), panel.pivot);
        Assert.That(panel.sizeDelta.x, Is.EqualTo(920f).Within(0.01f));
        Assert.That(panel.sizeDelta.y, Is.EqualTo(330f).Within(0.01f));
    }

    [Test]
    public void ApplyResponsiveLayout_EquivalentPortraitResolutionsUseSameReferencePanelSizeAndAnchors()
    {
        overlay.Show(CreateContext(MapTileType.Battle, true));
        RectTransform panel = FindRect("PreviewPanel");

        overlay.ApplyResponsiveLayout(new Vector2(720f, 1280f), new Rect(0f, 0f, 720f, 1280f));
        Vector2 smallSize = panel.sizeDelta;
        Vector2 smallAnchorMin = panel.anchorMin;
        Vector2 smallAnchorMax = panel.anchorMax;
        Vector2 smallPivot = panel.pivot;

        overlay.ApplyResponsiveLayout(new Vector2(1080f, 1920f), new Rect(0f, 0f, 1080f, 1920f));

        Assert.AreEqual(new Vector2(920f, 330f), smallSize);
        Assert.AreEqual(smallSize, panel.sizeDelta);
        Assert.AreEqual(new Vector2(0.5f, 0f), smallAnchorMin);
        Assert.AreEqual(smallAnchorMin, panel.anchorMin);
        Assert.AreEqual(new Vector2(0.5f, 0f), smallAnchorMax);
        Assert.AreEqual(smallAnchorMax, panel.anchorMax);
        Assert.AreEqual(new Vector2(0.5f, 0f), smallPivot);
        Assert.AreEqual(smallPivot, panel.pivot);
    }

    [Test]
    public void ApplyResponsiveLayout_PortraitSafeAreaOffsetsPanelAboveBottomInset()
    {
        overlay.Show(CreateContext(MapTileType.Battle, true));
        RectTransform panel = FindRect("PreviewPanel");
        Vector2 screenSize = new Vector2(1080f, 1920f);
        Rect safeArea = new Rect(0f, 120f, 1080f, 1800f);

        overlay.ApplyResponsiveLayout(screenSize, safeArea);

        Assert.AreEqual(new Vector2(920f, 330f), panel.sizeDelta);
        Assert.AreEqual(new Vector2(0.5f, 0f), panel.anchorMin);
        Assert.AreEqual(new Vector2(0.5f, 0f), panel.anchorMax);
        Assert.That(panel.anchoredPosition.y, Is.EqualTo(144f).Within(0.01f));
        Assert.GreaterOrEqual(panel.anchoredPosition.y, safeArea.yMin + 24f);
    }

    [Test]
    public void ApplyResponsiveLayout_LandscapeSafeAreaAvoidsRightAndTopInsets()
    {
        overlay.Show(CreateContext(MapTileType.Battle, true));
        RectTransform panel = FindRect("PreviewPanel");
        Vector2 screenSize = new Vector2(1920f, 1080f);
        Rect safeArea = new Rect(0f, 0f, 1800f, 980f);

        overlay.ApplyResponsiveLayout(screenSize, safeArea);

        float panelRight = screenSize.x + panel.anchoredPosition.x;
        float panelTop = screenSize.y * 0.5f + panel.anchoredPosition.y + panel.sizeDelta.y * 0.5f;
        Assert.AreEqual(new Vector2(500f, 620f), panel.sizeDelta);
        Assert.AreEqual(new Vector2(1f, 0.5f), panel.anchorMin);
        Assert.AreEqual(new Vector2(1f, 0.5f), panel.anchorMax);
        Assert.That(panel.anchoredPosition.x, Is.EqualTo(-160f).Within(0.01f));
        Assert.That(panel.anchoredPosition.y, Is.EqualTo(-50f).Within(0.01f));
        Assert.LessOrEqual(panelRight, safeArea.xMax - 40f);
        Assert.LessOrEqual(panelTop, safeArea.yMax - 24f);
    }

    [Test]
    public void RefreshResponsiveLayoutIfChanged_RotationSwitchesLayoutWhileVisible()
    {
        overlay.Show(CreateContext(MapTileType.Battle, true));
        RectTransform panel = FindRect("PreviewPanel");

        overlay.ApplyResponsiveLayout(
            new Vector2(1920f, 1080f),
            new Rect(0f, 0f, 1920f, 1080f));

        Assert.IsTrue(overlay.IsVisible);
        Assert.AreEqual(new Vector2(1f, 0.5f), panel.anchorMin);
        Assert.AreEqual(new Vector2(500f, 620f), panel.sizeDelta);

        bool appliedPortrait = overlay.RefreshResponsiveLayoutIfChanged(
            new Vector2(720f, 1280f),
            new Rect(0f, 0f, 720f, 1280f));

        Assert.IsTrue(appliedPortrait);
        Assert.IsTrue(overlay.IsVisible);
        Assert.AreEqual(new Vector2(0.5f, 0f), panel.anchorMin);
        Assert.AreEqual(new Vector2(0.5f, 0f), panel.anchorMax);
        Assert.AreEqual(new Vector2(920f, 330f), panel.sizeDelta);
    }

    [Test]
    public void RefreshResponsiveLayoutIfChanged_NonReferencePortraitConvertsBottomInsetToReferenceUnits()
    {
        overlay.Show(CreateContext(MapTileType.Battle, true));
        RectTransform panel = FindRect("PreviewPanel");
        Vector2 screenSize = new Vector2(720f, 1280f);
        Rect safeArea = new Rect(0f, 80f, 720f, 1200f);

        overlay.RefreshResponsiveLayoutIfChanged(screenSize, safeArea);

        const float expectedScaleFactor = 2f / 3f;
        float panelBottomPixels = panel.anchoredPosition.y * expectedScaleFactor;
        float panelTopPixels = (panel.anchoredPosition.y + panel.sizeDelta.y) * expectedScaleFactor;
        Assert.AreEqual(new Vector2(920f, 330f), panel.sizeDelta);
        Assert.That(panel.anchoredPosition.y, Is.EqualTo(144f).Within(0.01f));
        Assert.GreaterOrEqual(panelBottomPixels, safeArea.yMin);
        Assert.LessOrEqual(panelTopPixels, safeArea.yMax);
    }

    [Test]
    public void ConfigureFonts_AfterCreationUpdatesEveryTextRole()
    {
        TMP_FontAsset keyword = ScriptableObject.CreateInstance<TMP_FontAsset>();
        TMP_FontAsset body = ScriptableObject.CreateInstance<TMP_FontAsset>();
        TMP_FontAsset attribute = ScriptableObject.CreateInstance<TMP_FontAsset>();
        try
        {
            overlay.Show(CreateContext(MapTileType.Battle, true));

            overlay.ConfigureFonts(keyword, body, attribute);

            Assert.AreSame(keyword, FindText("BattleClassText").font);
            Assert.AreSame(body, FindText("DangerText").font);
            Assert.AreSame(body, FindText("StatusText").font);
            Assert.AreSame(attribute, FindText("Attribute0").font);
            Assert.AreSame(keyword, FindText("ConfirmLabel").font);
            Assert.AreSame(keyword, FindText("ReturnLabel").font);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(keyword);
            UnityEngine.Object.DestroyImmediate(body);
            UnityEngine.Object.DestroyImmediate(attribute);
        }
    }

    [Test]
    public void HandleCancelInput_IsOnlyCancellationPathAndLayoutRefreshNeverRaisesCancel()
    {
        int cancels = 0;
        overlay.CancelRequested += () => cancels++;

        overlay.HandleCancelInput();
        overlay.Show(CreateContext(MapTileType.Battle, true));
        overlay.RefreshResponsiveLayoutIfChanged(
            new Vector2(1920f, 1080f),
            new Rect(0f, 0f, 1920f, 1080f));
        bool repeatedLayoutApplied = overlay.RefreshResponsiveLayoutIfChanged(
            new Vector2(1920f, 1080f),
            new Rect(0f, 0f, 1920f, 1080f));
        overlay.RefreshResponsiveLayoutIfChanged(
            new Vector2(720f, 1280f),
            new Rect(0f, 80f, 720f, 1200f));

        Assert.IsFalse(repeatedLayoutApplied);
        Assert.AreEqual(0, cancels);

        overlay.SetInteractionEnabled(false);
        overlay.HandleCancelInput();
        overlay.SetInteractionEnabled(true);
        overlay.HandleCancelInput();
        overlay.Hide();
        overlay.HandleCancelInput();

        Assert.AreEqual(1, cancels);
        Assert.IsFalse(overlay.IsVisible);
    }

    private static MapBattleTilePreviewContext CreateContext(MapTileType tileType, bool canConfirm)
    {
        return new MapBattleTilePreviewContext
        {
            tile = new BoardTileData { tileType = tileType },
            dangerRating = 4,
            attributeNames = new[] { "Fire", "Dark" },
            confirmLabel = "Enter Battle - 2 AP",
            canPreview = true,
            canConfirm = canConfirm,
            blockReason = canConfirm ? string.Empty : "Unavailable"
        };
    }

    private string AllRenderedText()
    {
        return string.Join("\n", root.GetComponentsInChildren<TextMeshProUGUI>(true)
            .Where(text => text.gameObject.activeSelf)
            .Select(text => text.text));
    }

    private TextMeshProUGUI FindText(string name)
    {
        TextMeshProUGUI result = root.GetComponentsInChildren<TextMeshProUGUI>(true)
            .FirstOrDefault(text => text.name == name);
        Assert.IsNotNull(result, $"Missing TMP text named {name}.");
        return result;
    }

    private Button FindButton(string name)
    {
        Button result = root.GetComponentsInChildren<Button>(true)
            .FirstOrDefault(button => button.name == name);
        Assert.IsNotNull(result, $"Missing button named {name}.");
        return result;
    }

    private RectTransform FindRect(string name)
    {
        RectTransform result = root.GetComponentsInChildren<RectTransform>(true)
            .FirstOrDefault(rect => rect.name == name);
        Assert.IsNotNull(result, $"Missing RectTransform named {name}.");
        return result;
    }
}
