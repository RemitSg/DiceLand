using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Rebuilds only the waystation presentation, retaining the original interaction objects and data references.</summary>
public static class RestSceneForestBakeTools
{
    public const string ScenePath = "Assets/Scenes/RestScene.scene";
    private const string ArtPath = "Assets/Resources/RestScene/";
    private static readonly Color Gold = new Color(1f, 0.82f, 0.39f);

    [MenuItem("Tools/Rest Scene/Rebuild Woodland Waystation")]
    public static void Bake()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop play mode before rebuilding the scene.");
        foreach (string name in new[] { "Environment", "ActionPoints", "Forge", "Recruit", "Rest" }) ImportArt(name);
        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        if (!scene.isLoaded) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        var ap = Find<RestSceneActionPointRestoreInteractable>(scene);
        var forge = Find<RestSceneReforgeInteractable>(scene);
        var recruit = Find<RestSceneRecruitInteractable>(scene);
        var rest = Find<RestSceneInteractable>(scene);
        Camera camera = Find<Camera>(scene);
        if (ap == null || forge == null || recruit == null || rest == null || camera == null)
            throw new InvalidOperationException("The original four controllers and camera must exist.");

        // Retain controller GameObjects (including all loot/attribute/relic pool references) across rebakes.
        Behaviour[] controllers = { ap, forge, recruit, rest };
        foreach (Behaviour controller in controllers) controller.transform.SetParent(null, true);
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == "Woodland Waystation" || root.name == "Waystation Signs") UnityEngine.Object.DestroyImmediate(root);
            else if (root.name == "-------Environment-------" || root.name == "Global Volume" ||
                (root.GetComponent<SpriteRenderer>() != null && !controllers.Any(c => c.gameObject == root))) root.SetActive(false);
        }
        camera.transform.SetPositionAndRotation(new Vector3(0, 0, -10f), Quaternion.identity);
        camera.orthographic = true; camera.orthographicSize = 4.705f;
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(0.018f, 0.034f, 0.026f);
        camera.nearClipPlane = 0.1f; camera.farClipPlane = 100f;
        var extra = camera.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
        if (extra != null) extra.renderPostProcessing = false;
        foreach (Behaviour b in camera.GetComponents<Behaviour>())
            if (b.GetType().Name == "PostProcessVolume") b.enabled = false;

        GameObject rootObject = Root("Woodland Waystation", scene);
        RestSceneForestView view = rootObject.AddComponent<RestSceneForestView>();
        Transform world = Child("Layered Composition", rootObject.transform);
        Material material = EnsureSpriteMaterial();
        Sprite environment = AssetDatabase.LoadAssetAtPath<Sprite>(ArtPath + "Environment.png");
        SpriteRenderer backdrop = SpriteObject("Ambient Extension", rootObject.transform, environment, material, -200);
        backdrop.color = new Color(0.30f, 0.34f, 0.30f, 1f);
        SpriteRenderer background = SpriteObject("Environment", world, environment, material, -100);
        background.transform.localScale = new Vector3(16.72f / environment.bounds.size.x, 9.41f / environment.bounds.size.y, 1f);
        var environmentImporter = (TextureImporter)AssetImporter.GetAtPath(ArtPath + "Environment.png");
        environmentImporter.isReadable = false; environmentImporter.SaveAndReimport();

        PlaceStation(ap, world, "ActionPoints", new Rect(42, 478, 384, 260), material, 40);
        PlaceStation(forge, world, "Forge", new Rect(382, 78, 495, 602), material, 10);
        PlaceStation(recruit, world, "Recruit", new Rect(878, 334, 426, 352), material, 30);
        PlaceStation(rest, world, "Rest", new Rect(1282, 240, 378, 365), material, 20);

        GameObject signsObject = Root("Waystation Signs", scene, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = signsObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 100;
        CanvasScaler scaler = signsObject.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = 1f;
        RectTransform canvasRect = (RectTransform)canvas.transform;
        RectTransform composition = Ui("Composition", canvasRect, RestSceneForestView.ReferenceSize, Vector2.zero);
        CanvasGroup interactionGroup = composition.gameObject.AddComponent<CanvasGroup>();
        Font labelFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Assets/Fonts/Hi字楷体简 SDF.asset").sourceFontFile;
        Sprite plate = Resources.LoadAll<Sprite>("BattleUI/ForestRoundPlate").First();
        Sprite frame = Resources.LoadAll<Sprite>("AbilityMenu/GildedPanelFrame").First();
        Sprite button = Resources.LoadAll<Sprite>("MainMenu/GildedButton").First();
        Image heading = Image("Title", composition, new Vector2(366, 80), Point(836, 56), plate);
        Label("Label", heading.rectTransform, "林 间 驿 站", labelFont, 40, Gold, Vector2.zero, new Vector2(300, 64));

        var visuals = new List<RestSceneForestView.StationVisual>();
        visuals.Add(Sign(view, composition, RestSceneForestView.Station.ActionPoints, "行动点恢复", labelFont, plate, new Vector2(286, 67), new Vector2(244, 332)));
        visuals.Add(Sign(view, composition, RestSceneForestView.Station.Forge, "锻造", labelFont, plate, new Vector2(242, 67), new Vector2(634, 332)));
        visuals.Add(Sign(view, composition, RestSceneForestView.Station.Recruit, "招募", labelFont, plate, new Vector2(242, 67), new Vector2(1069, 332)));
        visuals.Add(Sign(view, composition, RestSceneForestView.Station.Rest, "休息", labelFont, plate, new Vector2(242, 67), new Vector2(1486, 332)));
        RectTransform glowRect = Ui("Ambient Light", composition, RestSceneForestView.ReferenceSize, Vector2.zero);
        var glow = glowRect.gameObject.AddComponent<RestSceneAmbientGlow>(); glow.raycastTarget = false; glowRect.SetAsFirstSibling();
        RectTransform motesRect = Ui("Sunlit Motes", composition, RestSceneForestView.ReferenceSize, Vector2.zero);
        var motes = motesRect.gameObject.AddComponent<MainMenuOrnament>(); motes.kind = MainMenuOrnament.Shape.Motes; motes.raycastTarget = false;
        foreach (Behaviour controller in controllers) StyleController(controller, button);
        Canvas.ForceUpdateCanvases();
        view.Configure(ap, forge, recruit, rest, camera, world, backdrop, canvasRect, composition, interactionGroup, frame, visuals.ToArray());
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    public static T Find<T>(Scene scene) where T : Component
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T component = root.GetComponentInChildren<T>(true);
            if (component != null) return component;
        }
        return null;
    }

    private static void ImportArt(string name)
    {
        string path = ArtPath + name + ".png";
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 100; importer.mipmapEnabled = false; importer.alphaIsTransparency = true;
        importer.npotScale = TextureImporterNPOTScale.None; importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 2048; importer.wrapMode = TextureWrapMode.Clamp; importer.isReadable = true;
        var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect; settings.spriteGenerateFallbackPhysicsShape = true;
        importer.SetTextureSettings(settings); importer.SaveAndReimport();
    }

    private static void PlaceStation(Behaviour controller, Transform world, string artName, Rect target, Material material, int order)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArtPath + artName + ".png");
        Rect alpha = AlphaBounds(sprite.texture);
        float scale = Mathf.Min(target.width / alpha.width, target.height / alpha.height);
        Vector2 desired = new Vector2(target.center.x, target.yMax - alpha.height * scale * 0.5f);
        Vector2 alphaCenter = (alpha.center - sprite.pivot) / 100f;
        Vector2 position = Point(desired.x, desired.y) / 100f - alphaCenter * scale;
        // Match physics picking depth to sprite order where station silhouettes overlap.
        Transform t = controller.transform; t.SetParent(world, false); t.localPosition = new Vector3(position.x, position.y, -order * 0.01f);
        t.localRotation = Quaternion.identity; t.localScale = Vector3.one * scale;
        SpriteRenderer renderer = controller.GetComponent<SpriteRenderer>(); renderer.sprite = sprite; renderer.sharedMaterial = material;
        renderer.color = Color.white; renderer.sortingLayerName = "Default"; renderer.sortingOrder = order;
        var collider = controller.GetComponent<PolygonCollider2D>(); collider.offset = Vector2.zero;
        int count = sprite.GetPhysicsShapeCount();
        if (count == 0) throw new InvalidOperationException("Missing alpha physics shape: " + artName);
        collider.pathCount = count;
        var points = new List<Vector2>();
        for (int i = 0; i < count; i++) { sprite.GetPhysicsShape(i, points); collider.SetPath(i, points); }
        var serialized = new SerializedObject(controller);
        serialized.FindProperty("outlineWidth").floatValue = 3.5f;
        serialized.FindProperty("outlineColor").colorValue = artName == "ActionPoints" ? new Color(0.44f, 0.96f, 1f) : Gold;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        // CPU readability is needed only during authoring; runtime input uses the baked PolygonCollider2D.
        var importer = (TextureImporter)AssetImporter.GetAtPath(ArtPath + artName + ".png");
        importer.isReadable = false; importer.SaveAndReimport();
    }

    public static Rect AlphaBounds(Texture2D texture)
    {
        Color32[] pixels = texture.GetPixels32();
        int minX = texture.width, minY = texture.height, maxX = 0, maxY = 0;
        for (int y = 0; y < texture.height; y++) for (int x = 0; x < texture.width; x++)
        {
            if (pixels[y * texture.width + x].a < 32) continue;
            minX = Mathf.Min(minX, x); minY = Mathf.Min(minY, y); maxX = Mathf.Max(maxX, x); maxY = Mathf.Max(maxY, y);
        }
        if (minX > maxX) throw new InvalidOperationException("Sprite has no visible pixels.");
        return Rect.MinMaxRect(minX, minY, maxX + 1, maxY + 1);
    }

    private static RestSceneForestView.StationVisual Sign(RestSceneForestView view, RectTransform parent, RestSceneForestView.Station id,
        string title, Font font, Sprite plate, Vector2 size, Vector2 position)
    {
        Image image = Image(id + " Sign", parent, size, Point(position.x, position.y), plate); image.raycastTarget = true;
        RestSceneStationTarget target = image.gameObject.AddComponent<RestSceneStationTarget>(); target.targetGraphic = image; target.Configure(view, id);
        var nav = target.navigation; nav.mode = Navigation.Mode.Horizontal; target.navigation = nav;
        var icon = Ui("Icon", image.rectTransform, new Vector2(38, 38), new Vector2(-size.x * 0.5f + 46, 0)).gameObject.AddComponent<RestSceneStationIcon>();
        icon.station = id; icon.color = Gold; icon.raycastTarget = false;
        Text label = Label("Label", image.rectTransform, title, font, id == RestSceneForestView.Station.ActionPoints ? 31 : 34,
            Gold, new Vector2(22, 0), new Vector2(size.x - 94, 55));
        return new RestSceneForestView.StationVisual { station = id, plaque = target, frame = image, icon = icon, label = label };
    }

    private static void StyleController(Behaviour controller, Sprite button)
    {
        var so = new SerializedObject(controller);
        foreach (string field in new[] { "promptBackgroundSprite", "panelBackgroundSprite", "cardBackgroundSprite", "sectionBackgroundSprite" })
        { var p = so.FindProperty(field); if (p != null) p.objectReferenceValue = null; }
        foreach (string field in new[] { "confirmButtonSprite", "cancelButtonSprite", "promptConfirmButtonSprite", "promptCancelButtonSprite",
            "panelButtonSprite", "recruitButtonSprite", "closeButtonSprite", "actionButtonSprite", "selectedButtonSprite" })
        { var p = so.FindProperty(field); if (p != null) p.objectReferenceValue = button; }
        foreach (string field in new[] { "promptBackgroundColor", "panelBackgroundColor" })
        { var p = so.FindProperty(field); if (p != null) p.colorValue = new Color(0.018f, 0.044f, 0.034f, 0.98f); }
        foreach (string field in new[] { "titleColor", "noteColor", "accentColor" })
        { var p = so.FindProperty(field); if (p != null) p.colorValue = Gold; }
        var body = so.FindProperty("bodyColor");
        if (body != null) body.colorValue = new Color(0.95f, 0.93f, 0.84f);
        foreach (string field in new[] { "buttonTextColor", "normalButtonTextColor", "selectedButtonTextColor" })
        { var p = so.FindProperty(field); if (p != null) p.colorValue = new Color(0.18f, 0.12f, 0.05f); }
        foreach (string field in new[] { "primaryButtonColor", "actionButtonColor" })
        { var p = so.FindProperty(field); if (p != null) p.colorValue = Color.white; }
        foreach (string field in new[] { "cardBackgroundColor", "sectionBackgroundColor" })
        { var p = so.FindProperty(field); if (p != null) p.colorValue = new Color(0.075f, 0.11f, 0.08f, 0.97f); }
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Material EnsureSpriteMaterial()
    {
        string path = ArtPath + "WaystationSprite.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null) { material = new Material(Shader.Find("Sprites/Default")); AssetDatabase.CreateAsset(material, path); }
        return material;
    }

    private static GameObject Root(string name, Scene scene, params Type[] components)
    { var go = new GameObject(name, components); SceneManager.MoveGameObjectToScene(go, scene); return go; }
    private static Transform Child(string name, Transform parent)
    { var go = new GameObject(name); go.transform.SetParent(parent, false); return go.transform; }
    private static SpriteRenderer SpriteObject(string name, Transform parent, Sprite sprite, Material material, int order)
    { var sr = Child(name, parent).gameObject.AddComponent<SpriteRenderer>(); sr.sprite = sprite; sr.sharedMaterial = material; sr.sortingOrder = order; return sr; }
    private static Vector2 Point(float x, float y) => new Vector2(x - 836f, 470.5f - y);
    private static RectTransform Ui(string name, RectTransform parent, Vector2 size, Vector2 position)
    { var r = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); r.SetParent(parent, false); r.sizeDelta = size; r.anchoredPosition = position; return r; }
    private static Image Image(string name, RectTransform parent, Vector2 size, Vector2 position, Sprite sprite)
    { var image = Ui(name, parent, size, position).gameObject.AddComponent<Image>(); image.sprite = sprite; image.raycastTarget = false; return image; }
    private static Text Label(string name, RectTransform parent, string title, Font font, int fontSize, Color color, Vector2 position, Vector2 size)
    { var label = Ui(name, parent, size, position).gameObject.AddComponent<Text>(); label.font = font; label.fontSize = fontSize; label.text = title;
        label.color = color; label.alignment = TextAnchor.MiddleCenter; label.raycastTarget = false;
        var shadow = label.gameObject.AddComponent<Shadow>(); shadow.effectColor = new Color(0, 0.02f, 0.01f, 0.9f); shadow.effectDistance = new Vector2(1, -2); return label; }
}
