# Tutorial Menu Center Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a `Tutorial` entry to the main menu that opens a data-driven tutorial center overlay with Chinese content and a future-safe start hook.

**Architecture:** Extend the existing main menu overlay pattern instead of rewriting the scene UI. Keep the menu entry, content model, and future tutorial-start hook isolated so the later gameplay tutorial flow can plug in without changing the overlay structure.

**Tech Stack:** Unity 2022.3 / Tuanjie 1.8.4, C#, uGUI runtime-generated UI, ScriptableObject content assets, Unity EditMode tests with NUnit.

---

### Task 1: Add Tutorial Runtime Request State

**Files:**
- Modify: `Assets/Scripts/Shared/MainMenuRuntime.cs`
- Create: `Assets/Tests/EditMode/Editor/MainMenuRuntimeTutorialTests.cs`

- [ ] **Step 1: Write the failing tests for tutorial request state**

```csharp
using NUnit.Framework;

public class MainMenuRuntimeTutorialTests
{
    [SetUp]
    public void SetUp()
    {
        MainMenuRuntime.ClearSelectedDifficulty();
        MainMenuRuntime.ClearContinueRequest();
        MainMenuRuntime.ClearTutorialRequest();
    }

    [Test]
    public void RequestTutorialStart_SetsTutorialRequestedFlag()
    {
        MainMenuRuntime.RequestTutorialStart();

        Assert.IsTrue(MainMenuRuntime.TutorialRequested);
    }

    [Test]
    public void ConsumeTutorialRequest_ReturnsTrueOnceAndClearsFlag()
    {
        MainMenuRuntime.RequestTutorialStart();

        Assert.IsTrue(MainMenuRuntime.ConsumeTutorialRequest());
        Assert.IsFalse(MainMenuRuntime.ConsumeTutorialRequest());
        Assert.IsFalse(MainMenuRuntime.TutorialRequested);
    }

    [Test]
    public void ClearTutorialRequest_ResetsTutorialFlagWithoutTouchingContinue()
    {
        MainMenuRuntime.RequestContinueFromSave();
        MainMenuRuntime.RequestTutorialStart();

        MainMenuRuntime.ClearTutorialRequest();

        Assert.IsFalse(MainMenuRuntime.TutorialRequested);
        Assert.IsTrue(MainMenuRuntime.ConsumeContinueRequest());
    }
}
```

- [ ] **Step 2: Run the test to verify it fails**

Run:

```powershell
& 'E:\Unity\2022.3.62t6\Editor\Tuanjie.exe' `
  -batchmode `
  -projectPath 'L:\gameProject\MobileGame' `
  -runTests `
  -testPlatform EditMode `
  -testFilter MainMenuRuntimeTutorialTests `
  -testResults 'L:\gameProject\MobileGame\Logs\MainMenuRuntimeTutorialTests.xml' `
  -quit
```

Expected:

- test run fails because `TutorialRequested`, `RequestTutorialStart`, `ConsumeTutorialRequest`, and `ClearTutorialRequest` do not exist yet

- [ ] **Step 3: Implement the minimal tutorial request API in `MainMenuRuntime`**

```csharp
using UnityEngine;

public static class MainMenuRuntime
{
    public static DifficultyConfig_SO SelectedDifficultyConfig { get; private set; }
    public static bool ContinueRequested { get; private set; }
    public static bool TutorialRequested { get; private set; }

    public static void SetSelectedDifficulty(DifficultyConfig_SO difficultyConfig)
    {
        SelectedDifficultyConfig = difficultyConfig;
    }

    public static void ClearSelectedDifficulty()
    {
        SelectedDifficultyConfig = null;
    }

    public static void RequestContinueFromSave()
    {
        ContinueRequested = true;
    }

    public static bool ConsumeContinueRequest()
    {
        bool requested = ContinueRequested;
        ContinueRequested = false;
        return requested;
    }

    public static void ClearContinueRequest()
    {
        ContinueRequested = false;
    }

    public static void RequestTutorialStart()
    {
        TutorialRequested = true;
    }

    public static bool ConsumeTutorialRequest()
    {
        bool requested = TutorialRequested;
        TutorialRequested = false;
        return requested;
    }

    public static void ClearTutorialRequest()
    {
        TutorialRequested = false;
    }
}
```

- [ ] **Step 4: Re-run the tests to verify they pass**

Run the same command from Step 2.

Expected:

- exit code `0`
- `Logs/MainMenuRuntimeTutorialTests.xml` shows `3` passing tests and `0` failures

- [ ] **Step 5: Commit**

```powershell
git add -- `
  'Assets/Scripts/Shared/MainMenuRuntime.cs' `
  'Assets/Tests/EditMode/Editor/MainMenuRuntimeTutorialTests.cs'
git commit -m "feat: add tutorial request state to main menu runtime"
```

### Task 2: Add Data-Driven Tutorial Menu Content Model

**Files:**
- Create: `Assets/Scripts/ScriptableObject/TutorialMenuCatalog_SO.cs`
- Create: `Assets/Tests/EditMode/Editor/TutorialMenuCatalogTests.cs`

- [ ] **Step 1: Write the failing tests for tutorial section ordering and defaults**

```csharp
using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class TutorialMenuCatalogTests
{
    [Test]
    public void GetOrderedSections_FiltersNullsAndSortsBySortOrder()
    {
        TutorialMenuCatalog_SO catalog = ScriptableObject.CreateInstance<TutorialMenuCatalog_SO>();
        catalog.Sections = new List<TutorialMenuSection>
        {
            new TutorialMenuSection { sectionId = "world", title = "世界系统", sortOrder = 20 },
            null,
            new TutorialMenuSection { sectionId = "battle", title = "战斗基础", sortOrder = 10 },
            new TutorialMenuSection { sectionId = "map", title = "初识地图", sortOrder = 0 }
        };

        IReadOnlyList<TutorialMenuSection> ordered = catalog.GetOrderedSections();

        Assert.AreEqual(3, ordered.Count);
        Assert.AreEqual("map", ordered[0].sectionId);
        Assert.AreEqual("battle", ordered[1].sectionId);
        Assert.AreEqual("world", ordered[2].sectionId);
    }

    [Test]
    public void OnValidate_FillsMissingDisplayFieldsFromFallbacks()
    {
        TutorialMenuCatalog_SO catalog = ScriptableObject.CreateInstance<TutorialMenuCatalog_SO>();
        catalog.displayName = "";
        catalog.summaryTitle = "";
        catalog.startButtonLabel = "";
        catalog.backButtonLabel = "";

        catalog.ForceValidateForTests();

        Assert.AreEqual("Tutorial", catalog.displayName);
        Assert.AreEqual("教程", catalog.summaryTitle);
        Assert.AreEqual("开始教程", catalog.startButtonLabel);
        Assert.AreEqual("返回", catalog.backButtonLabel);
    }
}
```

- [ ] **Step 2: Run the test to verify it fails**

Run:

```powershell
& 'E:\Unity\2022.3.62t6\Editor\Tuanjie.exe' `
  -batchmode `
  -projectPath 'L:\gameProject\MobileGame' `
  -runTests `
  -testPlatform EditMode `
  -testFilter TutorialMenuCatalogTests `
  -testResults 'L:\gameProject\MobileGame\Logs\TutorialMenuCatalogTests.xml' `
  -quit
```

Expected:

- test run fails because `TutorialMenuCatalog_SO`, `TutorialMenuSection`, `GetOrderedSections`, and `ForceValidateForTests` do not exist yet

- [ ] **Step 3: Implement `TutorialMenuCatalog_SO` with sortable sections**

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class TutorialMenuSection
{
    public string sectionId = "section";
    public string title = "未命名章节";
    [TextArea(2, 4)] public string summary;
    [TextArea(3, 8)] public string details;
    public string statusLabel = "开发中";
    public int sortOrder;
    public bool isAvailable = true;
    public bool isFeatured;
}

[CreateAssetMenu(fileName = "TutorialMenuCatalog", menuName = "Tutorial/Tutorial Menu Catalog")]
public class TutorialMenuCatalog_SO : ScriptableObject
{
    public string tutorialId = "main_tutorial";
    public string displayName = "Tutorial";
    public string summaryTitle = "教程";
    [TextArea(3, 6)] public string summaryBody;
    public string startButtonLabel = "开始教程";
    public string backButtonLabel = "返回";
    [SerializeField] private List<TutorialMenuSection> sections = new List<TutorialMenuSection>();

    public List<TutorialMenuSection> Sections
    {
        get => sections;
        set => sections = value ?? new List<TutorialMenuSection>();
    }

    public IReadOnlyList<TutorialMenuSection> GetOrderedSections()
    {
        List<TutorialMenuSection> ordered = new List<TutorialMenuSection>();
        if (sections != null)
        {
            for (int i = 0; i < sections.Count; i++)
            {
                if (sections[i] != null)
                    ordered.Add(sections[i]);
            }
        }

        ordered.Sort((left, right) => left.sortOrder.CompareTo(right.sortOrder));
        return ordered;
    }

    public void ForceValidateForTests()
    {
        OnValidate();
    }

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(tutorialId))
            tutorialId = "main_tutorial";
        if (string.IsNullOrWhiteSpace(displayName))
            displayName = "Tutorial";
        if (string.IsNullOrWhiteSpace(summaryTitle))
            summaryTitle = "教程";
        if (string.IsNullOrWhiteSpace(startButtonLabel))
            startButtonLabel = "开始教程";
        if (string.IsNullOrWhiteSpace(backButtonLabel))
            backButtonLabel = "返回";
        if (sections == null)
            sections = new List<TutorialMenuSection>();

        for (int i = 0; i < sections.Count; i++)
        {
            TutorialMenuSection section = sections[i];
            if (section == null)
                continue;

            if (string.IsNullOrWhiteSpace(section.sectionId))
                section.sectionId = $"section_{i + 1}";
            if (string.IsNullOrWhiteSpace(section.title))
                section.title = "未命名章节";
            if (string.IsNullOrWhiteSpace(section.statusLabel))
                section.statusLabel = section.isAvailable ? "可用" : "敬请期待";
        }
    }
}
```

- [ ] **Step 4: Re-run the tests to verify they pass**

Run the same command from Step 2.

Expected:

- exit code `0`
- `Logs/TutorialMenuCatalogTests.xml` shows `2` passing tests and `0` failures

- [ ] **Step 5: Commit**

```powershell
git add -- `
  'Assets/Scripts/ScriptableObject/TutorialMenuCatalog_SO.cs' `
  'Assets/Tests/EditMode/Editor/TutorialMenuCatalogTests.cs'
git commit -m "feat: add tutorial menu catalog data model"
```

### Task 3: Add Runtime Tutorial Center Overlay

**Files:**
- Create: `Assets/Scripts/MainMenu/MainMenuTutorialOverlay.cs`
- Create: `Assets/Tests/EditMode/Editor/MainMenuTutorialOverlayTests.cs`

- [ ] **Step 1: Write the failing tests for overlay content binding**

```csharp
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuTutorialOverlayTests
{
    [Test]
    public void Show_BindsCatalogTextsAndCreatesSectionViews()
    {
        GameObject host = new GameObject("Host");
        MainMenuTutorialOverlay overlay = host.AddComponent<MainMenuTutorialOverlay>();
        TutorialMenuCatalog_SO catalog = ScriptableObject.CreateInstance<TutorialMenuCatalog_SO>();
        catalog.summaryTitle = "教程";
        catalog.summaryBody = "学习地图、战斗与世界系统。";
        catalog.startButtonLabel = "开始教程";
        catalog.backButtonLabel = "返回";
        catalog.Sections.Add(new TutorialMenuSection { sectionId = "map", title = "初识地图", summary = "学习移动", sortOrder = 0 });
        catalog.Sections.Add(new TutorialMenuSection { sectionId = "battle", title = "战斗基础", summary = "学习拼点", sortOrder = 10 });

        overlay.Show(catalog, null, null);

        Assert.IsTrue(overlay.IsVisible);
        Assert.AreEqual("教程", overlay.DebugSummaryTitleText.text);
        Assert.AreEqual("学习地图、战斗与世界系统。", overlay.DebugSummaryBodyText.text);
        Assert.AreEqual("开始教程", overlay.DebugStartButtonText.text);
        Assert.AreEqual("返回", overlay.DebugBackButtonText.text);
        Assert.AreEqual(2, overlay.DebugSectionCount);
    }
}
```

- [ ] **Step 2: Run the test to verify it fails**

Run:

```powershell
& 'E:\Unity\2022.3.62t6\Editor\Tuanjie.exe' `
  -batchmode `
  -projectPath 'L:\gameProject\MobileGame' `
  -runTests `
  -testPlatform EditMode `
  -testFilter MainMenuTutorialOverlayTests `
  -testResults 'L:\gameProject\MobileGame\Logs\MainMenuTutorialOverlayTests.xml' `
  -quit
```

Expected:

- test run fails because `MainMenuTutorialOverlay` and the debug accessors do not exist yet

- [ ] **Step 3: Implement `MainMenuTutorialOverlay` using the existing runtime overlay style**

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class MainMenuTutorialOverlay : MonoBehaviour
{
    private const string FallbackFontName = "LegacyRuntime.ttf";

    private Canvas canvas;
    private RectTransform panelRect;
    private Text summaryTitleText;
    private Text summaryBodyText;
    private Text detailText;
    private Button startButton;
    private Button backButton;
    private Text startButtonText;
    private Text backButtonText;
    private RectTransform sectionListRoot;
    private readonly List<GameObject> spawnedSectionViews = new List<GameObject>();
    private Action startHandler;

    public bool IsVisible => canvas != null && canvas.gameObject.activeSelf;
    public Text DebugSummaryTitleText => summaryTitleText;
    public Text DebugSummaryBodyText => summaryBodyText;
    public Text DebugStartButtonText => startButtonText;
    public Text DebugBackButtonText => backButtonText;
    public int DebugSectionCount => spawnedSectionViews.Count;

    public void Show(TutorialMenuCatalog_SO catalog, Action onStart, Action onBack)
    {
        EnsureUi();
        startHandler = onStart;
        TutorialMenuCatalog_SO safeCatalog = catalog;
        BindCatalog(safeCatalog);

        startButton.onClick.RemoveAllListeners();
        startButton.onClick.AddListener(() => startHandler?.Invoke());

        backButton.onClick.RemoveAllListeners();
        backButton.onClick.AddListener(() =>
        {
            onBack?.Invoke();
            Hide();
        });

        canvas.gameObject.SetActive(true);
    }

    public void Hide()
    {
        if (canvas != null)
            canvas.gameObject.SetActive(false);
    }

    private void BindCatalog(TutorialMenuCatalog_SO catalog)
    {
        summaryTitleText.text = catalog != null ? catalog.summaryTitle : "教程";
        summaryBodyText.text = catalog != null ? catalog.summaryBody : string.Empty;
        startButtonText.text = catalog != null ? catalog.startButtonLabel : "开始教程";
        backButtonText.text = catalog != null ? catalog.backButtonLabel : "返回";
        detailText.text = "后续这里可以扩展章节详情、术语说明、截图和进度信息。";

        for (int i = 0; i < spawnedSectionViews.Count; i++)
            DestroyImmediate(spawnedSectionViews[i]);
        spawnedSectionViews.Clear();

        IReadOnlyList<TutorialMenuSection> sections = catalog != null ? catalog.GetOrderedSections() : Array.Empty<TutorialMenuSection>();
        for (int i = 0; i < sections.Count; i++)
        {
            TutorialMenuSection section = sections[i];
            GameObject row = new GameObject($"Section_{i}", typeof(RectTransform), typeof(Image));
            row.transform.SetParent(sectionListRoot, false);
            Text rowText = CreateText("Label", row.GetComponent<RectTransform>(), Vector2.zero, new Vector2(520f, 88f), $"{section.title}\n{section.summary}", 24, ResolveFallbackFont(), TextAnchor.MiddleLeft, Color.white);
            rowText.horizontalOverflow = HorizontalWrapMode.Wrap;
            rowText.verticalOverflow = VerticalWrapMode.Overflow;
            spawnedSectionViews.Add(row);
        }
    }

    private void EnsureUi()
    {
        if (canvas != null)
            return;

        Font font = ResolveFallbackFont();
        GameObject canvasGo = new GameObject("MainMenuTutorialOverlay", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9550;

        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform rootRect = canvasGo.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        panelRect = CreateRect("Panel", rootRect, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1500f, 860f));
        panelRect.gameObject.AddComponent<Image>().color = new Color(0.08f, 0.1f, 0.14f, 0.96f);

        summaryTitleText = CreateText("SummaryTitle", panelRect, new Vector2(-480f, -80f), new Vector2(520f, 60f), "教程", 42, font, TextAnchor.MiddleLeft, Color.white);
        summaryBodyText = CreateText("SummaryBody", panelRect, new Vector2(-480f, -160f), new Vector2(620f, 120f), string.Empty, 24, font, TextAnchor.UpperLeft, new Color(0.86f, 0.88f, 0.92f, 1f));
        sectionListRoot = CreateRect("SectionList", panelRect, new Vector2(-420f, -20f), new Vector2(620f, 420f));
        detailText = CreateText("DetailText", panelRect, new Vector2(360f, -140f), new Vector2(460f, 360f), string.Empty, 24, font, TextAnchor.UpperLeft, new Color(0.84f, 0.88f, 0.96f, 1f));

        startButton = CreateButton("StartButton", panelRect, new Vector2(-160f, 330f), new Vector2(220f, 64f), out startButtonText, font, "开始教程");
        backButton = CreateButton("BackButton", panelRect, new Vector2(90f, 330f), new Vector2(220f, 64f), out backButtonText, font, "返回");

        canvas.gameObject.SetActive(false);
    }

    private static Font ResolveFallbackFont()
    {
        return Resources.GetBuiltinResource<Font>(FallbackFontName);
    }

    private static RectTransform CreateRect(string name, RectTransform parent, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
        return rect;
    }

    private static Text CreateText(string name, RectTransform parent, Vector2 anchoredPosition, Vector2 sizeDelta, string textValue, int fontSize, Font font, TextAnchor alignment, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;

        Text text = go.GetComponent<Text>();
        text.font = font;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color;
        text.text = textValue;
        text.supportRichText = true;
        return text;
    }

    private static Button CreateButton(string name, RectTransform parent, Vector2 anchoredPosition, Vector2 sizeDelta, out Text label, Font font, string textValue)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;

        go.GetComponent<Image>().color = new Color(0.18f, 0.32f, 0.58f, 1f);
        label = CreateText("Label", rect, Vector2.zero, sizeDelta, textValue, 26, font, TextAnchor.MiddleCenter, Color.white);
        return go.GetComponent<Button>();
    }
}
```

- [ ] **Step 4: Re-run the tests to verify they pass**

Run the same command from Step 2.

Expected:

- exit code `0`
- `Logs/MainMenuTutorialOverlayTests.xml` shows `1` passing test and `0` failures

- [ ] **Step 5: Commit**

```powershell
git add -- `
  'Assets/Scripts/MainMenu/MainMenuTutorialOverlay.cs' `
  'Assets/Tests/EditMode/Editor/MainMenuTutorialOverlayTests.cs'
git commit -m "feat: add tutorial center overlay"
```

### Task 4: Wire Tutorial Into Main Menu And Fill Initial Content

**Files:**
- Modify: `Assets/Scripts/MainMenu/MainMenuSceneController.cs`
- Modify: `Assets/Scripts/Shared/MainMenuRuntime.cs`
- Create asset: `Assets/Data/Tutorial/TutorialMenuCatalog.asset`

- [ ] **Step 1: Add the failing scene-level wiring test**

```csharp
using NUnit.Framework;
using UnityEngine;
using System.Reflection;

public class MainMenuSceneControllerTutorialActionTests
{
    [Test]
    public void ResolveMenuAction_RecognizesTutorialLabel()
    {
        MethodInfo method = typeof(MainMenuSceneController).GetMethod("ResolveMenuAction", BindingFlags.NonPublic | BindingFlags.Static);
        object result = method.Invoke(null, new object[] { "Tutorial" });

        Assert.AreEqual("Tutorial", result.ToString());
    }
}
```

- [ ] **Step 2: Run the test to verify it fails**

Run:

```powershell
& 'E:\Unity\2022.3.62t6\Editor\Tuanjie.exe' `
  -batchmode `
  -projectPath 'L:\gameProject\MobileGame' `
  -runTests `
  -testPlatform EditMode `
  -testFilter MainMenuSceneControllerTutorialActionTests `
  -testResults 'L:\gameProject\MobileGame\Logs\MainMenuSceneControllerTutorialActionTests.xml' `
  -quit
```

Expected:

- test run fails because the menu action enum / resolver does not yet recognize `Tutorial`

- [ ] **Step 3: Implement main menu tutorial wiring**

Update `MainMenuSceneController` with these changes:

```csharp
private enum MenuAction
{
    None = 0,
    NewGame = 1,
    Continue = 2,
    Contributors = 3,
    Settings = 4,
    Exit = 5,
    DemoTest = 6,
    Tutorial = 7
}

[Header("Tutorial")]
[SerializeField] private TutorialMenuCatalog_SO tutorialMenuCatalog;
[SerializeField] private string tutorialLabel = "Tutorial";

private MainMenuTutorialOverlay tutorialOverlay;
```

In `Awake()` after the existing overlay setup:

```csharp
tutorialOverlay = GetComponent<MainMenuTutorialOverlay>();
if (tutorialOverlay == null)
    tutorialOverlay = gameObject.AddComponent<MainMenuTutorialOverlay>();
```

In `HandleMainMenuItemClicked(...)`:

```csharp
case MenuAction.Tutorial:
    ShowTutorialOverlay();
    break;
```

Add new methods:

```csharp
private void ShowTutorialOverlay()
{
    if (tutorialOverlay == null || tutorialOverlay.IsVisible)
        return;

    tutorialOverlay.Show(tutorialMenuCatalog, HandleTutorialStartClicked, null);
    SetGalleryInputEnabled(false);
}

private void HandleTutorialStartClicked()
{
    MainMenuRuntime.ClearContinueRequest();
    MainMenuRuntime.RequestTutorialStart();
    Debug.Log("[MainMenuSceneController] Tutorial start requested from tutorial center.");
}
```

Update blocking overlay checks:

```csharp
private bool IsBlockingOverlayVisible()
{
    return (contributorsOverlay != null && contributorsOverlay.IsVisible)
        || (settingsOverlay != null && settingsOverlay.IsVisible)
        || (tutorialOverlay != null && tutorialOverlay.IsVisible);
}
```

Update action resolution and labels:

```csharp
case "tutorial":
    return MenuAction.Tutorial;
```

```csharp
case MenuAction.Tutorial:
    return tutorialLabel;
```
```

- [ ] **Step 4: Create the initial tutorial catalog asset and fill it with Chinese content**

In Unity Editor:

1. Create folder `Assets/Data/Tutorial` if it does not exist
2. Create asset `Assets/Data/Tutorial/TutorialMenuCatalog.asset`
3. Fill these values:

```text
tutorialId: main_tutorial
displayName: Tutorial
summaryTitle: 教程
summaryBody: 通过引导式流程学习地图移动、战斗流程、事件选择、休整操作、商店购买、锻造、钥匙推进与 Boss 挑战。
startButtonLabel: 开始教程
backButtonLabel: 返回
```

Sections:

```text
sectionId: map_intro
title: 初识地图
summary: 学习地图移动、格子选择与行动点消耗。
details: 这里将展示地图教学目标、操作重点与后续扩展说明。
statusLabel: 即将开放
sortOrder: 0
isAvailable: true
isFeatured: true
```

```text
sectionId: battle_basics
title: 战斗基础
summary: 学习速度按钮、连线、拼点、单方面攻击与技能查看。
details: 这里将展示战斗教学目标、关键机制和后续补充说明。
statusLabel: 即将开放
sortOrder: 10
isAvailable: true
isFeatured: false
```

```text
sectionId: world_systems
title: 世界系统
summary: 学习事件、休整、商店、锻造、钥匙推进与 Boss 收束。
details: 这里将展示世界系统教学目标、章节说明和未来扩展位。
statusLabel: 即将开放
sortOrder: 20
isAvailable: true
isFeatured: false
```

4. Assign the asset to `MainMenuSceneController.tutorialMenuCatalog`

- [ ] **Step 5: Re-run the tests and perform a focused manual smoke test**

Run the same command from Step 2.

Expected:

- exit code `0`
- `Logs/MainMenuSceneControllerTutorialActionTests.xml` shows `1` passing test and `0` failures

Then verify manually in the Unity Editor:

1. Open the main menu scene
2. Enter Play Mode
3. Focus/click the `Tutorial` item
4. Confirm the overlay opens in Chinese
5. Confirm `返回` closes the overlay
6. Confirm `开始教程` writes the tutorial start request without entering normal game flow

- [ ] **Step 6: Commit**

```powershell
git add -- `
  'Assets/Scripts/MainMenu/MainMenuSceneController.cs' `
  'Assets/Scripts/Shared/MainMenuRuntime.cs' `
  'Assets/Data/Tutorial/TutorialMenuCatalog.asset'
git commit -m "feat: add tutorial center to main menu"
```

### Task 5: Final Verification Pass

**Files:**
- Verify only: `Assets/Scripts/MainMenu/MainMenuSceneController.cs`
- Verify only: `Assets/Scripts/MainMenu/MainMenuTutorialOverlay.cs`
- Verify only: `Assets/Scripts/Shared/MainMenuRuntime.cs`
- Verify only: `Assets/Scripts/ScriptableObject/TutorialMenuCatalog_SO.cs`
- Verify only: `Assets/Data/Tutorial/TutorialMenuCatalog.asset`

- [ ] **Step 1: Run the full focused EditMode test batch**

```powershell
& 'E:\Unity\2022.3.62t6\Editor\Tuanjie.exe' `
  -batchmode `
  -projectPath 'L:\gameProject\MobileGame' `
  -runTests `
  -testPlatform EditMode `
  -testFilter MainMenuRuntimeTutorialTests,TutorialMenuCatalogTests,MainMenuTutorialOverlayTests,MainMenuSceneControllerTutorialActionTests `
  -testResults 'L:\gameProject\MobileGame\Logs\TutorialMenuCenter.EditMode.xml' `
  -quit
```

Expected:

- exit code `0`
- XML report created at `Logs/TutorialMenuCenter.EditMode.xml`
- `0` failures across the four focused test classes

- [ ] **Step 2: Run a manual UI checklist in Play Mode**

Verify all of the following:

- `Tutorial` appears in the main menu list
- `Continue` enable/disable behavior is unchanged
- opening `Tutorial` disables gallery interaction
- overlay renders Chinese overview text and three section cards
- `开始教程` triggers only the isolated placeholder hook
- `返回` closes the overlay and restores gallery interaction
- adding a fourth section to the catalog asset produces a fourth card without code edits

- [ ] **Step 3: Commit the final verification state**

```powershell
git add -- `
  'Assets/Scripts/MainMenu/MainMenuSceneController.cs' `
  'Assets/Scripts/MainMenu/MainMenuTutorialOverlay.cs' `
  'Assets/Scripts/Shared/MainMenuRuntime.cs' `
  'Assets/Scripts/ScriptableObject/TutorialMenuCatalog_SO.cs' `
  'Assets/Data/Tutorial/TutorialMenuCatalog.asset' `
  'Assets/Tests/EditMode/Editor/MainMenuRuntimeTutorialTests.cs' `
  'Assets/Tests/EditMode/Editor/TutorialMenuCatalogTests.cs' `
  'Assets/Tests/EditMode/Editor/MainMenuTutorialOverlayTests.cs' `
  'Assets/Tests/EditMode/Editor/MainMenuSceneControllerTutorialActionTests.cs'
git commit -m "feat: build tutorial menu center"
```
