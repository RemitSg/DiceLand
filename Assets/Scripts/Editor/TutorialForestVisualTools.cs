using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Renders the real tutorial overlays and MapHUD in a disposable scene without saving or starting a run.</summary>
public static class TutorialForestVisualTools
{
    public const string OutputPath = "outputs/agent-battle/tutorial-ui-20261105";
    public const string BodyFontPath = "Assets/Assets/Fonts/AlibabaPuHuiTi-3-95-ExtraBold.ttf";
    public const string HeadingFontPath = "Assets/Assets/Fonts/Hi字楷体简.ttf";
    public const string WelcomeTitle = "即将开始教程，是否开始？";
    public const string WelcomeBody = "进入教程会让你了解战斗规则、基本玩法、游戏系统等。教程会从地图推进开始，逐步带你熟悉移动、战斗、资源管理与成长流程。";
    public const string ActionBody = "左上角的状态栏显示了你当前的行动点，移动到其它格子都会消耗你的行动点。";
    public const string HeaderBody = "第一行包括格子的坐标与类型。“战斗”代表你会在进入该格子时开启一场战斗。";

    public static string Capture(string name, int width = 2640, int height = 1216, int mode = 0, Rect? safe = null, bool scrollEnd = false)
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Capture uses a disposable Edit Mode scene.");
        Scene original = SceneManager.GetActiveScene();
        Scene preview = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(preview);
        float scale = Time.timeScale;
        bool input = SpeedButton.InputSuppressed;
        RenderTexture previous = RenderTexture.active, target = null;
        Texture2D image = null;
        Sprite menuBackdrop = null;
        TutorialMenuCatalog_SO catalog = null;
        GameObject host = null;
        Camera camera = null;
        try
        {
            host = new GameObject("TutorialVisualPreview", typeof(EventSystem));
            camera = new GameObject("TutorialPreviewCamera", typeof(Camera)).GetComponent<Camera>(); camera.scene = preview;
            camera.orthographic = true; camera.orthographicSize = 5; camera.aspect = width / (float)height;
            camera.transform.position = new Vector3(0, 0, -10); camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.02f, .06f, .03f);
            new GameObject("TutorialPreviewLight", typeof(Light)).GetComponent<Light>().type = LightType.Directional;
            target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32); camera.targetTexture = target;
            var forest = new GameObject("Forest", typeof(SpriteRenderer)).GetComponent<SpriteRenderer>(); forest.transform.SetParent(host.transform, false);
            if (mode == 0 || mode == 4)
            {
                Texture2D menuArt = Resources.Load<Texture2D>("MainMenu/ForestJourney");
                if (menuArt != null) menuBackdrop = Sprite.Create(menuArt, new Rect(0, 0, menuArt.width, menuArt.height), Vector2.one * .5f);
            }
            forest.sprite = menuBackdrop != null ? menuBackdrop : MapForestArt.Background(MapBackgroundPhase.Morning);
            if (forest.sprite != null) forest.transform.localScale = Vector3.one * Mathf.Max(10 / forest.sprite.bounds.size.y, 10 * camera.aspect / forest.sprite.bounds.size.x);
            var body = AssetDatabase.LoadAssetAtPath<Font>(BodyFontPath); var heading = AssetDatabase.LoadAssetAtPath<Font>(HeadingFontPath);
            var tmp = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath("c6e7143cd2df45b45a784ef8c61a210f"));
            var pixels = new Vector2(width, height); Rect safePixels = safe ?? new Rect(0, 0, width, height);
            TutorialForestView view;
            MapHUD hud = null;
            if (mode == 0 || mode == 4)
            {
                var menu = host.AddComponent<MainMenuTutorialOverlay>(); menu.ConfigureFonts(body, heading, heading);
                if (mode == 4)
                {
                    catalog = ScriptableObject.CreateInstance<TutorialMenuCatalog_SO>();
                    catalog.Sections.Add(new TutorialMenuSection { title = "地图探索", summary = "查看格子详情，选择路线并管理行动点。", sortOrder = 10 });
                    catalog.Sections.Add(new TutorialMenuSection { title = "骰子战斗", summary = "了解速度、技能选择、行动顺序和骰子判定。", sortOrder = 20 });
                    catalog.Sections.Add(new TutorialMenuSection { title = "资源与成长", summary = "了解金币、商店、图纸与制作。", sortOrder = 30 });
                }
                menu.Show(catalog, null, null, "开始教程", "返回主菜单", WelcomeTitle, WelcomeBody, mode == 0);
                view = menu.Presentation;
            }
            else if (mode == 3 || mode == 6)
            {
                var battle = host.AddComponent<TutorialBattleGuideOverlay>(); battle.ConfigureFonts(tmp, tmp);
                string text = mode == 6 ? string.Join("\n\n", Enumerable.Repeat("阅读完整的战斗条件、行动顺序和技能骰说明。", 20)) + "\n最后一条说明。" : "先查看双方的速度按钮，再选择技能与目标。多个骰子会依次结算；技能详情中可以查看每颗骰子的威力和词条。";
                battle.Show("速度与技能", text, "继续", null); view = battle.Presentation;
            }
            else
            {
                hud = host.AddComponent<MapHUD>(); hud.ConfigureFont(tmp);
                var action = host.AddComponent<ActionPointSystem>(); action.Initialize(5);
                hud.Refresh(new RunState { difficultyName = "Normal", requiredKeys = 1 }, action);
                hud.ShowTile(new BoardTileData { coord = new Vector2Int(mode == 1 ? 0 : 1, 3), tileType = mode == 1 ? MapTileType.Empty : MapTileType.Battle,
                    encounterSeed = 26001, isVisited = mode == 1, isResolved = mode == 1, previewTag = "教程战斗" }, mode == 1 ? 0 : 2, true);
                hud.ShowStatus(mode == 1 ? "很好，松开操作并稍等片刻，教程会继续……" : "正在查看格子详情。再次点击该格子可尝试进入。");
                Canvas hudCanvas = host.GetComponentsInChildren<Canvas>(true).Single(c => c.name == "MapHudCanvas");
                ConfigureCanvas(hudCanvas, camera, pixels, -2.5f); hud.ApplyViewport(pixels, safePixels);
                Canvas.ForceUpdateCanvases();
                var guide = host.AddComponent<MapTutorialGuideOverlay>(); guide.ConfigureFonts(heading, body, heading);
                MapTutorialFocusPart focus = mode == 1 ? MapTutorialFocusPart.ActionPoints : MapTutorialFocusPart.TileHeader;
                MapTutorialFocusPart bounds = mode == 1 ? MapTutorialFocusPart.Resources : MapTutorialFocusPart.TilePanel;
                Func<Rect?> targetBounds = () => hud.TryGetTutorialTargetScreenRect(focus, out Rect rect) ? rect : (Rect?)null;
                if (mode == 5)
                {
                    guide.ShowTargetHighlight(targetBounds() ?? Rect.zero); guide.TrackTarget(targetBounds);
                }
                else guide.ShowFocused(mode == 1 ? "行动点" : "坐标与类型", mode == 1 ? ActionBody : HeaderBody, null, "继续", targetBounds,
                    () => hud.TryGetTutorialTargetScreenRect(bounds, out Rect rect) ? rect : (Rect?)null, mode == 1 ? "地图探索" : "格子详情");
                view = guide.Presentation;
            }
            ConfigureCanvas(view.Canvas, camera, pixels, -3);
            Canvas.ForceUpdateCanvases(); view.ApplyViewport(pixels, safePixels); view.Advance(2);
            if (scrollEnd) view.Reading.verticalNormalizedPosition = 0;
            foreach (TMP_Text text in host.GetComponentsInChildren<TMP_Text>()) text.ForceMeshUpdate(true, true);
            foreach (Graphic graphic in host.GetComponentsInChildren<Graphic>())
                if (!(graphic is TMP_Text) && !(graphic is TMP_SubMeshUI)) { graphic.SetAllDirty(); graphic.Rebuild(CanvasUpdate.PreRender); }
            Canvas.ForceUpdateCanvases(); camera.Render(); RenderTexture.active = target;
            image = new Texture2D(width, height, TextureFormat.RGB24, false); image.ReadPixels(new Rect(0, 0, width, height), 0, 0); image.Apply();
            Directory.CreateDirectory(OutputPath); string path = Path.Combine(OutputPath, name); File.WriteAllBytes(path, image.EncodeToPNG());
            File.WriteAllLines(path + ".layout.txt", new[] { "title=" + TutorialForestView.GetText(view.Title), "body=" + TutorialForestView.GetText(view.Body),
                "focus=" + view.HasFocus + " screen=" + view.FocusScreenRect + " hole=" + view.Shade.hole,
                "panel=" + view.Panel.sizeDelta + " safe=" + view.SafeRect, "content=" + view.BodyContent.rect.height + " viewport=" + view.Reading.viewport.rect.height + " scrollbar=" + view.Reading.verticalScrollbar.gameObject.activeInHierarchy });
            return Path.GetFullPath(path);
        }
        finally
        {
            RenderTexture.active = previous; if (camera != null) camera.targetTexture = null;
            if (image != null) UnityEngine.Object.DestroyImmediate(image);
            if (target != null) UnityEngine.Object.DestroyImmediate(target);
            if (host != null) UnityEngine.Object.DestroyImmediate(host);
            if (catalog != null) UnityEngine.Object.DestroyImmediate(catalog);
            if (menuBackdrop != null) UnityEngine.Object.DestroyImmediate(menuBackdrop);
            if (original.IsValid() && original.isLoaded) SceneManager.SetActiveScene(original);
            EditorSceneManager.CloseScene(preview, true); Time.timeScale = scale; SpeedButton.InputSuppressed = input;
        }
    }

    public static void ConfigureCanvas(Canvas canvas, Camera camera, Vector2 pixels, float z)
    {
        canvas.GetComponent<CanvasScaler>().enabled = false; canvas.renderMode = RenderMode.WorldSpace; canvas.worldCamera = camera;
        RectTransform rect = (RectTransform)canvas.transform; rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * .5f;
        rect.sizeDelta = new Vector2(1080 * pixels.x / pixels.y, 1080); rect.position = new Vector3(0, 0, z); rect.localScale = Vector3.one * (5f / 540f);
    }
}
