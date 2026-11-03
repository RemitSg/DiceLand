using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>Render the saved battle scene without saving or replacing the user's open scene.</summary>
public static class BattleLookPreviewTools
{
    public const string ScenePath = "Assets/Scenes/SampleScene.scene";
    public const string ProfilePath = "Assets/Scenes/SampleScene/Battle Cinematic Profile.asset";
    public const string GroundPath = "Assets/Scenes/SampleScene/Battle Forest Ground.mat";
    public const string ForestAssets = "Assets/Scenes/SampleScene/Forest";
    public const string BackdropPath = "Assets/Assets/Textures/Environments/Forest/ForestRuinsBackdrop.png";
    public const string GroundTexturePath = "Assets/Assets/Textures/Environments/Forest/ForestFloorPainterly.png";
    public const string MidgroundPath = "Assets/Assets/Textures/Environments/Forest/SunlitCanopyTerracesPainted.png";
    public const string ForegroundPath = "Assets/Assets/Textures/Environments/Forest/MossFernForegroundLow.png";
    public const string StoneTexturePath = "Assets/Assets/Textures/Environments/Forest/ForestTerracePavingSoft.png";
    public const string MarginTexturePath = "Assets/Assets/Textures/Environments/Forest/ForestMarginPlants.png";
    public const string RuinTexturePath = "Assets/Assets/Textures/Environments/Forest/ForestRuinTerraces.png";
    public const string SunVolumeMaterialPath = ForestAssets + "/Canopy Sun Volume.mat";
    public const string OutputPath = "outputs/agent-battle/battle-look-20261029";

    [MenuItem("Tools/Battle Visuals/Apply Forest Cinematic Look")]
    public static void ApplyToSavedBattleScene()
    {
        Scene original = SceneManager.GetActiveScene();
        if (Enumerable.Range(0, SceneManager.sceneCount).Any(i => SceneManager.GetSceneAt(i).path == ScenePath))
            throw new InvalidOperationException("Close the battle scene before applying the saved-scene look; unsaved scene changes are never replaced.");
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        try
        {
            SceneManager.SetActiveScene(scene);
            ConfigureScene(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
        finally
        {
            SceneManager.SetActiveScene(original);
            EditorSceneManager.CloseScene(scene, true);
        }
    }

    [MenuItem("Tools/Battle Visuals/Apply Ruin Depth Layers")]
    public static void ApplyRuinDepthToSavedBattleScene()
    {
        Scene original = SceneManager.GetActiveScene();
        if (Enumerable.Range(0, SceneManager.sceneCount).Any(i => SceneManager.GetSceneAt(i).path == ScenePath))
            throw new InvalidOperationException("Close the battle scene before applying its ruin layers; unsaved changes are never replaced.");
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        try
        {
            SceneManager.SetActiveScene(scene);
            GameObject atmosphere = scene.GetRootGameObjects().First(item => item.name == "Battle Atmosphere");
            AddRuinDepthLayers(atmosphere.transform);
            foreach (string path in new[] { GroundPath, ForestAssets + "/Forest Terrace Stone.mat" })
            {
                Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null)
                    throw new InvalidOperationException("Missing forest paving material: " + path);
                ConfigurePavingScale(material);
                EditorUtility.SetDirty(material);
                AssetDatabase.SaveAssetIfDirty(material);
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
        finally
        {
            SceneManager.SetActiveScene(original);
            EditorSceneManager.CloseScene(scene, true);
        }
    }

    [MenuItem("Tools/Battle Visuals/Apply Forest Sun Volume")]
    public static void ApplySunVolumeToSavedBattleScene()
    {
        Scene original = SceneManager.GetActiveScene();
        if (Enumerable.Range(0, SceneManager.sceneCount).Any(i => SceneManager.GetSceneAt(i).path == ScenePath))
            throw new InvalidOperationException("Close the battle scene before applying its sunlight; unsaved changes are never replaced.");
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        try
        {
            SceneManager.SetActiveScene(scene);
            Transform atmosphere = scene.GetRootGameObjects().First(item => item.name == "Battle Atmosphere").transform;
            foreach (MeshRenderer renderer in atmosphere.GetComponentsInChildren<MeshRenderer>(true))
                if (renderer.name.StartsWith("Canopy sun shaft ", StringComparison.Ordinal))
                    renderer.enabled = false;
            AddSunVolume(atmosphere);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
        finally
        {
            SceneManager.SetActiveScene(original);
            EditorSceneManager.CloseScene(scene, true);
        }
    }

    [MenuItem("Tools/Battle Visuals/Apply Actor Sun Shadows")]
    public static void ApplySunShadowsToSavedBattleScene()
    {
        Scene original = SceneManager.GetActiveScene();
        if (Enumerable.Range(0, SceneManager.sceneCount).Any(i => SceneManager.GetSceneAt(i).path == ScenePath))
            throw new InvalidOperationException("Close the battle scene before applying its actor shadows; unsaved changes are never replaced.");
        BattleActorFormBakeTools.BakeLibrary();
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        try
        {
            SceneManager.SetActiveScene(scene);
            GameObject atmosphere = scene.GetRootGameObjects().First(item => item.name == "Battle Atmosphere");
            BattleManager manager = scene.GetRootGameObjects().SelectMany(item => item.GetComponentsInChildren<BattleManager>(true)).Single();
            AddActorSunShadows(atmosphere, manager);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
        finally
        {
            SceneManager.SetActiveScene(original);
            EditorSceneManager.CloseScene(scene, true);
        }
    }

    private static void ConfigureScene(Scene scene)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        Camera overview = roots.SelectMany(root => root.GetComponentsInChildren<Camera>(true)).First(camera => camera.CompareTag("MainCamera"));
        Camera closeup = roots.SelectMany(root => root.GetComponentsInChildren<Camera>(true)).First(camera => camera != overview);
        // URP's lens-based depth reconstruction needs a perspective projection.
        // Match the old overview framing at the combat plane before changing it.
        if (overview.orthographic)
            overview.fieldOfView = MapViewController.CalculateEquivalentPerspectiveFieldOfView(
                overview.transform.position, overview.transform.rotation, overview.orthographicSize, overview.fieldOfView);
        overview.orthographic = false;
        overview.transform.SetPositionAndRotation(new Vector3(0f, 5.6549635f, -29f), Quaternion.Euler(3f, 0f, 0f));
        overview.fieldOfView = 28f;
        closeup.orthographic = false;
        closeup.fieldOfView = 28f;
        BattlePresentationSystem presentation = roots.SelectMany(root => root.GetComponentsInChildren<BattlePresentationSystem>(true)).FirstOrDefault();
        if (presentation != null)
        {
            SerializedObject settings = new SerializedObject(presentation);
            settings.FindProperty("closeupCameraOffset").vector3Value = new Vector3(0f, 2.2f, -10f);
            settings.FindProperty("closeupFieldOfView").floatValue = 28f;
            settings.FindProperty("cameraFocusOffset").vector3Value = new Vector3(0f, -0.15f, 0f);
            settings.FindProperty("closeupLookOffset").vector3Value = Vector3.zero;
            settings.FindProperty("closeupLargeTargetVerticalBias").floatValue = 0f;
            settings.FindProperty("closeupRangePositionPulseAmplitude").floatValue = 0.06f;
            settings.FindProperty("clashShakeAmplitude").floatValue = 0.28f;
            settings.FindProperty("hitShakeAmplitude").floatValue = 0.40f;
            settings.FindProperty("clashCameraImpactDuration").floatValue = 0.22f;
            settings.FindProperty("hitCameraImpactDuration").floatValue = 0.30f;
            settings.FindProperty("minimumCameraIntensity").floatValue = 0.46f;
            settings.FindProperty("cameraPowerNormalization").floatValue = 9f;
            settings.FindProperty("cameraShakeFrequency").floatValue = 18f;
            settings.FindProperty("cameraImpactLensKick").floatValue = 0.032f;
            settings.FindProperty("cameraImpactRoll").floatValue = 0.95f;
            settings.ApplyModifiedPropertiesWithoutUndo();
        }
        BattleManager manager = roots.SelectMany(root => root.GetComponentsInChildren<BattleManager>(true)).First();
        Volume volume = roots.SelectMany(root => root.GetComponentsInChildren<Volume>(true)).First(item => item.name == "Global Volume");

        foreach (Light light in roots.SelectMany(root => root.GetComponentsInChildren<Light>(true)))
        {
            if (light.type == LightType.Directional)
            {
                light.transform.rotation = Quaternion.Euler(52f, 218f, 0f);
                light.color = new Color(1f, 0.94f, 0.84f);
                light.intensity = 1.6f;
                light.shadowStrength = 0.82f;
                light.shadows = LightShadows.Soft;
            }
            else if (light.type == LightType.Point)
            {
                light.color = new Color(0.48f, 0.7f, 1f);
                light.intensity = 0.8f;
                light.range = 12f;
                light.shadows = LightShadows.None;
                light.transform.position = new Vector3(2f, 6f, -2f);
            }
        }
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.20f, 0.28f, 0.38f);
        RenderSettings.ambientEquatorColor = new Color(0.10f, 0.15f, 0.20f);
        RenderSettings.ambientGroundColor = new Color(0.065f, 0.085f, 0.10f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.30f, 0.40f, 0.46f);
        RenderSettings.fogStartDistance = 42f;
        RenderSettings.fogEndDistance = 130f;

        VolumeProfile profile = CreateLookProfile();
        volume.sharedProfile = profile;
        volume.priority = 10f;
        foreach (Camera camera in new[] { overview, closeup })
        {
            camera.allowHDR = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = RenderSettings.fogColor;
            camera.farClipPlane = 120f;
            UniversalAdditionalCameraData data = camera.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;
            data.requiresDepthTexture = true;
            data.dithering = true;
            data.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
        }

        BattleLookController look = volume.GetComponent<BattleLookController>();
        if (look == null)
            look = volume.gameObject.AddComponent<BattleLookController>();
        SerializedObject serialized = new SerializedObject(look);
        serialized.FindProperty("overviewCamera").objectReferenceValue = overview;
        serialized.FindProperty("closeupCamera").objectReferenceValue = closeup;
        serialized.FindProperty("lookVolume").objectReferenceValue = volume;
        serialized.FindProperty("allies").objectReferenceValue = manager.alliesParent.transform;
        serialized.FindProperty("enemies").objectReferenceValue = manager.enemiesParent.transform;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        Material ground = AssetDatabase.LoadAssetAtPath<Material>(GroundPath);
        if (ground == null)
        {
            ground = new Material(Shader.Find("DiceLand/Battle Forest Ground"));
            AssetDatabase.CreateAsset(ground, GroundPath);
        }
        ground.SetColor("_SoilColor", new Color(0.39f, 0.34f, 0.23f));
        ground.SetColor("_MossColor", new Color(0.19f, 0.27f, 0.15f));
        ground.SetColor("_StoneColor", new Color(0.50f, 0.47f, 0.36f));
        ground.SetTexture("_GroundMap", AssetDatabase.LoadAssetAtPath<Texture2D>(GroundTexturePath));
        ground.SetFloat("_TextureScale", 0.055f);
        ground.SetFloat("_TextureWeight", 1f);
        ground.SetTexture("_StoneMap", AssetDatabase.LoadAssetAtPath<Texture2D>(StoneTexturePath));
        ground.SetFloat("_StoneTextureWeight", 0.8f);
        ConfigurePavingScale(ground);
        EditorUtility.SetDirty(ground);
        AssetDatabase.SaveAssetIfDirty(ground);
        MeshRenderer groundRenderer = roots.SelectMany(root => root.GetComponentsInChildren<MeshRenderer>(true)).First(renderer => renderer.name == "Plane");
        groundRenderer.sharedMaterial = ground;
        // The real ground ends in the mist bank, ahead of the distant painted landscape.
        groundRenderer.transform.position = new Vector3(0f, 0.03f, -40f);
        groundRenderer.transform.localScale = new Vector3(14f, 1f, 12f);
        foreach (SpriteRenderer renderer in roots.SelectMany(root => root.GetComponentsInChildren<SpriteRenderer>(true)))
        {
            if (renderer.name == "Tree" || renderer.name == "Tree (2)" || renderer.name.StartsWith("TX Tree T1"))
                renderer.enabled = false;
        }
        AddForestLayers(scene, overview, manager);
    }

    private static VolumeProfile CreateLookProfile()
    {
        VolumeProfile existing = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
        VolumeProfile profile = existing != null ? existing : ScriptableObject.CreateInstance<VolumeProfile>();
        if (existing == null)
            AssetDatabase.CreateAsset(profile, ProfilePath);
        Bloom bloom = GetOrAdd<Bloom>(profile);
        bloom.threshold.Override(1.05f);
        bloom.intensity.Override(0.55f);
        bloom.scatter.Override(0.55f);
        bloom.highQualityFiltering.Override(false);
        ColorAdjustments color = GetOrAdd<ColorAdjustments>(profile);
        color.postExposure.Override(0.18f);
        color.contrast.Override(9f);
        color.saturation.Override(-4f);
        WhiteBalance balance = GetOrAdd<WhiteBalance>(profile);
        balance.temperature.Override(3f);
        balance.tint.Override(2f);
        Tonemapping tone = GetOrAdd<Tonemapping>(profile);
        tone.mode.Override(TonemappingMode.ACES);
        SplitToning split = GetOrAdd<SplitToning>(profile);
        split.shadows.Override(new Color(0.44f, 0.49f, 0.54f));
        split.highlights.Override(new Color(0.53f, 0.51f, 0.47f));
        split.balance.Override(-15f);
        Vignette vignette = GetOrAdd<Vignette>(profile);
        vignette.color.Override(new Color(0.035f, 0.055f, 0.075f));
        vignette.intensity.Override(0.16f);
        vignette.smoothness.Override(0.45f);
        DepthOfField depth = GetOrAdd<DepthOfField>(profile);
        depth.mode.Override(DepthOfFieldMode.Gaussian);
        depth.gaussianStart.Override(34f);
        depth.gaussianEnd.Override(46f);
        depth.gaussianMaxRadius.Override(1f);
        MotionBlur motion = GetOrAdd<MotionBlur>(profile);
        motion.mode.Override(MotionBlurMode.CameraAndObjects);
        motion.quality.Override(MotionBlurQuality.Low);
        motion.intensity.Override(0.22f);
        motion.clamp.Override(0.018f);
        foreach (VolumeComponent component in profile.components)
        {
            if (!AssetDatabase.Contains(component))
                AssetDatabase.AddObjectToAsset(component, profile);
            EditorUtility.SetDirty(component);
        }
        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssetIfDirty(profile);
        return profile;
    }

    private static T GetOrAdd<T>(VolumeProfile profile) where T : VolumeComponent
    {
        return profile.TryGet(out T component) ? component : profile.Add<T>(true);
    }

    private static Material ForestMaterial(string name, string shaderName)
    {
        string path = ForestAssets + "/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Shader shader = Shader.Find(shaderName);
            if (shader == null)
                throw new InvalidOperationException("Missing forest shader: " + shaderName);
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }
        return material;
    }

    private static GameObject Quad(string name, Transform parent, Material material)
    {
        GameObject item = GameObject.CreatePrimitive(PrimitiveType.Quad);
        item.name = name;
        item.transform.SetParent(parent, false);
        UnityEngine.Object.DestroyImmediate(item.GetComponent<Collider>());
        MeshRenderer renderer = item.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return item;
    }

    private static void AddForestLayers(Scene scene, Camera camera, BattleManager manager)
    {
        if (!AssetDatabase.IsValidFolder(ForestAssets))
            AssetDatabase.CreateFolder("Assets/Scenes/SampleScene", "Forest");
        TextureImporter importer = AssetImporter.GetAtPath(BackdropPath) as TextureImporter;
        if (importer == null)
            throw new InvalidOperationException("Missing forest backdrop: " + BackdropPath);
        if (importer.textureType != TextureImporterType.Default || !importer.mipmapEnabled || importer.wrapMode != TextureWrapMode.Clamp)
        {
            importer.textureType = TextureImporterType.Default;
            importer.mipmapEnabled = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.SaveAndReimport();
        }
        GameObject previous = scene.GetRootGameObjects().FirstOrDefault(item => item.name == "Battle Atmosphere");
        if (previous != null)
            UnityEngine.Object.DestroyImmediate(previous);
        GameObject root = new GameObject("Battle Atmosphere");
        SceneManager.MoveGameObjectToScene(root, scene);

        Material backdrop = ForestMaterial("Forest Ruins Backdrop", "DiceLand/Battle Forest Backdrop");
        backdrop.shader = Shader.Find("DiceLand/Battle Forest Backdrop");
        backdrop.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(BackdropPath));
        backdrop.SetColor("_Tint", new Color(0.94f, 1.0f, 1.03f));
        backdrop.SetColor("_AirColor", new Color(0.66f, 0.77f, 0.80f, 0.52f));
        backdrop.SetVector("_Opening", new Vector4(0.52f, 0.57f, 0.82f, 0.88f));
        backdrop.SetColor("_SunGlow", new Color(1.25f, 1.05f, 0.71f, 0.18f));
        backdrop.SetVector("_WaterfallA", new Vector4(0.393f, 0.369f, 0.053f, 0.128f));
        backdrop.SetVector("_WaterfallB", new Vector4(0.598f, 0.298f, 0.037f, 0.115f));
        backdrop.SetVector("_WaterfallC", new Vector4(0.683f, 0.286f, 0.041f, 0.128f));
        backdrop.SetFloat("_WaterFlow", 0.12f);
        backdrop.SetFloat("_WaterFlowSpeed", 0.8f);
        GameObject landscape = Quad("Distant ruins and canopy", root.transform, backdrop);
        // Keep the painted cascade at its authored world pose when overview framing changes.
        landscape.transform.SetPositionAndRotation(new Vector3(-12f, 11.372186f, 62.120636f), Quaternion.Euler(7f, 0f, 0f));
        landscape.transform.localScale = new Vector3(124f, 69.75f, 1f);
        Camera closeup = scene.GetRootGameObjects().SelectMany(item => item.GetComponentsInChildren<Camera>(true))
            .First(item => item != camera);
        root.AddComponent<BattleBackdropCoverageController>().Configure(landscape.GetComponent<MeshFilter>(), camera, closeup);

        Material material = ForestMaterial("Forest Foliage", "DiceLand/Battle Forest Foliage");
        material.shader = Shader.Find("DiceLand/Battle Forest Foliage");
        material.SetColor("_Color", Color.white);
        material.SetFloat("_HighlightGain", 1.6f);
        material.SetColor("_PaintedFill", new Color(0.36f, 0.40f, 0.32f, 0.12f));
        material.SetFloat("_SceneLightingWeight", 0.22f);
        Material canopyMaterial = BattleForestCanopyBakeTools.ConfigureMaterial(material);
        Sprite ancientTree = LoadForestSprite(MidgroundPath);
        float treeScale = 1.85f;
        AddTree(root.transform, ancientTree, canopyMaterial, "Ancient tree and ruined terrace",
            new Vector3(0.2f, -ancientTree.bounds.min.y * treeScale - 1.7f, 12f), treeScale,
            new Color(0.95f, 0.98f, 1f), false);
        SpriteRenderer midground = root.transform.Find("Ancient tree and ruined terrace").GetComponent<SpriteRenderer>();
        midground.transform.localScale = new Vector3(1.65f, treeScale, 1.65f);
        BattleLookController look = scene.GetRootGameObjects()
            .SelectMany(item => item.GetComponentsInChildren<BattleLookController>(true)).First();
        SerializedObject lookSettings = new SerializedObject(look);
        lookSettings.FindProperty("midground").objectReferenceValue = midground;
        lookSettings.ApplyModifiedPropertiesWithoutUndo();
        Material nearMaterial = ForestMaterial("Near Forest Foliage", "DiceLand/Battle Forest Foliage");
        nearMaterial.SetFloat("_DefocusTexels", 10f);
        nearMaterial.SetFloat("_SoftSilhouette", 1f);
        nearMaterial.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        nearMaterial.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        nearMaterial.SetFloat("_ZWrite", 0f);
        nearMaterial.SetShaderPassEnabled("DepthOnly", false);
        nearMaterial.SetOverrideTag("RenderType", "Transparent");
        nearMaterial.renderQueue = (int)RenderQueue.Transparent;
        nearMaterial.SetColor("_Color", new Color(0.68f, 0.76f, 0.65f, 1f));
        nearMaterial.SetFloat("_SwayAmplitude", 0.025f);
        Sprite foreground = LoadForestSprite(ForegroundPath);
        AddTree(root.transform, foreground, nearMaterial, "Left foreground foliage", new Vector3(-8.3f, 2.7192f, -8.4f), 0.90f, new Color(0.67f, 0.78f, 0.86f), false);
        AddTree(root.transform, foreground, nearMaterial, "Right foreground foliage", new Vector3(9.4f, 1.9144f, -8.4f), 0.72f, new Color(0.65f, 0.76f, 0.84f), true);
        float groveHeight = -foreground.bounds.min.y * 0.85f - 0.5f;
        AddTree(root.transform, foreground, material, "Left outer grove", new Vector3(-21.7f, groveHeight, 19f), 0.85f, new Color(0.46f, 0.62f, 0.70f), false);
        AddTree(root.transform, foreground, material, "Right outer grove", new Vector3(21.7f, groveHeight, 19f), 0.85f, new Color(0.46f, 0.62f, 0.70f), true);

        Material stone = ForestMaterial("Forest Terrace Stone", "DiceLand/Battle Forest Stone");
        stone.SetColor("_StoneColor", new Color(0.50f, 0.47f, 0.36f));
        stone.SetColor("_MossColor", new Color(0.11f, 0.19f, 0.13f));
        Texture2D paving = AssetDatabase.LoadAssetAtPath<Texture2D>(StoneTexturePath);
        stone.SetTexture("_StoneMap", paving);
        stone.SetFloat("_StoneTextureWeight", paving != null ? 0.8f : 0f);
        stone.SetFloat("_LowerStepGain", 0.35f);
        ConfigurePavingScale(stone);
        stone.SetTexture("_GroundMap", AssetDatabase.LoadAssetAtPath<Texture2D>(GroundTexturePath));
        stone.SetFloat("_TextureScale", 0.055f);
        stone.SetFloat("_TextureWeight", 1f);
        stone.SetColor("_SoilColor", new Color(0.39f, 0.34f, 0.23f));
        stone.SetColor("_MossColor", new Color(0.19f, 0.27f, 0.15f));
        root.AddComponent<BattleForestStageController>().Configure(
            scene.GetRootGameObjects().SelectMany(item => item.GetComponentsInChildren<MeshRenderer>(true)).First(item => item.name == "Plane"), stone, 0.65f, 0.8f,
            BattleForestCanopyShadowBakeTools.BakeCookie());
        root.AddComponent<BattleActorLightingController>().Configure(manager.alliesParent.transform, manager.enemiesParent.transform,
            AssetDatabase.LoadAssetAtPath<BattleActorFormLibrary>(BattleActorFormBakeTools.LibraryPath));
        foreach (Material generated in new[] { nearMaterial, stone })
        {
            EditorUtility.SetDirty(generated);
            AssetDatabase.SaveAssetIfDirty(generated);
        }

        AddAtmosphere(root, manager, backdrop, material);
        AddWoodlandMargin(root.transform);
        AddRuinDepthLayers(root.transform);
    }

    private static void ConfigurePavingScale(Material material)
    {
        material.SetFloat("_PavingScale", 0.16f);
        material.SetFloat("_PavingGrowth", 0.72f);
    }

    private static void AddRuinDepthLayers(Transform parent)
    {
        Sprite sprite = LoadForestSprite(RuinTexturePath);
        Material material = ForestMaterial("Forest Ruin Terraces", "DiceLand/Battle Forest Foliage");
        material.SetColor("_PaintedFill", new Color(0.36f, 0.40f, 0.32f, 0.12f));
        material.SetFloat("_HighlightGain", 1.1f);
        material.SetFloat("_SwayAmplitude", 0f);
        material.SetFloat("_SceneLightingWeight", 0.35f);
        material.SetFloat("_Cutoff", 0.45f);
        material.SetColor("_Color", Color.white);
        Transform root = parent.Find("Ruin depth layers");
        if (root == null)
        {
            root = new GameObject("Ruin depth layers").transform;
            root.SetParent(parent, false);
        }
        ConfigureRuinPiece(root, sprite, material, "Near left ruined terraces", -7f, 9.5f, -0.35f,
            0.95f, new Color(0.92f, 0.96f, 1f), false);
        ConfigureRuinPiece(root, sprite, material, "Far right ruined terraces", 16.5f, 16f, -0.4f,
            1.1f, new Color(0.82f, 0.9f, 0.95f), true);
        EditorUtility.SetDirty(material);
        AssetDatabase.SaveAssetIfDirty(material);
    }

    private static void ConfigureRuinPiece(Transform parent, Sprite sprite, Material material, string name,
        float x, float z, float baseHeight, float scale, Color tint, bool flip)
    {
        // Reuse authored objects so running the update twice preserves scene references.
        Transform piece = parent.Find(name);
        if (piece == null)
        {
            piece = new GameObject(name).transform;
            piece.SetParent(parent, false);
        }
        piece.localPosition = new Vector3(x, -sprite.bounds.min.y * scale + baseHeight, z);
        piece.localRotation = Quaternion.identity;
        piece.localScale = Vector3.one * scale;
        SpriteRenderer renderer = piece.GetComponent<SpriteRenderer>();
        if (renderer == null)
            renderer = piece.gameObject.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sharedMaterial = material;
        renderer.color = tint;
        renderer.flipX = flip;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    private static void AddWoodlandMargin(Transform parent)
    {
        Sprite plants = LoadForestSprite(MarginTexturePath);
        Material margin = ForestMaterial("Woodland Margin Plants", "DiceLand/Battle Forest Foliage");
        margin.SetFloat("_SwayAmplitude", 0.015f);
        margin.SetFloat("_HighlightGain", 1f);
        margin.SetFloat("_SceneLightingWeight", 1f);
        // Authored variation is repeatable and leaves the gameplay RNG entirely untouched.
        float[] x = { -32f, -23f, -15.5f, -8.2f, -1.7f, 5.2f, 12.4f, 21f, 30.5f };
        float[] z = { 11f, 8.8f, 12.5f, 9.7f, 13f, 10.4f, 8.6f, 12f, 9.1f };
        for (int i = 0; i < x.Length; i++)
        {
            float scale = 0.12f + (i * 7 % 5) * 0.020f;
            AddTree(parent, plants, margin, "Woodland margin patch " + (i + 1),
                new Vector3(x[i], 0.045f - plants.bounds.min.y * scale, z[i]), scale,
                new Color(0.85f, 0.91f, 0.87f), i % 3 == 0);
        }
        EditorUtility.SetDirty(margin);
        AssetDatabase.SaveAssetIfDirty(margin);
    }

    private static Sprite LoadForestSprite(string path)
    {
        TextureImporter treeImporter = AssetImporter.GetAtPath(path) as TextureImporter;
        if (treeImporter == null)
            throw new InvalidOperationException("Missing forest sprite: " + path);
        if (treeImporter.textureType != TextureImporterType.Sprite || !treeImporter.alphaIsTransparency || !treeImporter.mipmapEnabled)
        {
            treeImporter.textureType = TextureImporterType.Sprite;
            treeImporter.spriteImportMode = SpriteImportMode.Single;
            treeImporter.spritePixelsPerUnit = 100f;
            treeImporter.alphaIsTransparency = true;
            treeImporter.mipmapEnabled = true;
            treeImporter.wrapMode = TextureWrapMode.Clamp;
            treeImporter.filterMode = FilterMode.Trilinear;
            treeImporter.textureCompression = TextureImporterCompression.CompressedHQ;
            treeImporter.maxTextureSize = 2048;
            treeImporter.SaveAndReimport();
        }
        if (path == ForegroundPath)
        {
            // A tight leaf mesh would trim the blurred alpha at its original sharp contour.
            TextureImporterSettings settings = new TextureImporterSettings();
            treeImporter.ReadTextureSettings(settings);
            if (settings.spriteMeshType != SpriteMeshType.FullRect)
            {
                settings.spriteMeshType = SpriteMeshType.FullRect;
                treeImporter.SetTextureSettings(settings);
                treeImporter.SaveAndReimport();
            }
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static void AddAtmosphere(GameObject root, BattleManager manager, Material backdrop, Material material)
    {
        Material rays = AddSunVolume(root.transform);
        Material mist = ForestMaterial("Low Forest Mist", "DiceLand/Battle Forest Atmosphere");
        mist.SetColor("_Color", new Color(0.98f, 0.91f, 0.75f, 0.28f));
        mist.SetFloat("_MistBreakup", 0.65f);
        mist.SetFloat("_Mode", 1f);
        mist.SetFloat("_SoftDistance", 1.5f);
        for (int i = 0; i < 2; i++)
        {
            GameObject bank = Quad("Drifting mist bank " + (i + 1), root.transform, mist);
            bank.transform.localPosition = new Vector3(i == 0 ? -5f : 8f, 1.6f + i * 0.6f, i == 0 ? 10f : 26f);
            bank.transform.localScale = new Vector3(65f, 8f, 1f);
        }
        AddDust(root.transform);
        AddFallingLeaves(root.transform);
        Material contact = ForestMaterial("Contact Shadows", "DiceLand/Battle Contact Shadow");
        BattleContactShadowController shadows = root.AddComponent<BattleContactShadowController>();
        shadows.Configure(manager.alliesParent.transform, manager.enemiesParent.transform, contact);
        AddActorSunShadows(root, manager);
        EditorUtility.SetDirty(contact);
        AssetDatabase.SaveAssetIfDirty(contact);
        foreach (Material generated in new[] { backdrop, material, rays, mist })
        {
            EditorUtility.SetDirty(generated);
            AssetDatabase.SaveAssetIfDirty(generated);
        }
    }

    private static void AddActorSunShadows(GameObject root, BattleManager manager)
    {
        Material material = ForestMaterial("Actor Sun Shadows", "DiceLand/Battle Actor Sun Shadow");
        material.SetColor("_Shade", new Color(0.42f,0.54f,0.72f,1f));
        material.SetFloat("_Opacity", 0.75f);
        material.SetFloat("_Softness", 0.035f);
        material.SetFloat("_Blur", 5f);
        material.SetFloat("_GroundHeight", 0.06f);
        Light sun = root.scene.GetRootGameObjects().SelectMany(item => item.GetComponentsInChildren<Light>(true))
            .FirstOrDefault(item => item.enabled && item.type == LightType.Directional);
        BattleActorSunShadowController controller = root.GetComponent<BattleActorSunShadowController>();
        if (controller == null) controller = root.AddComponent<BattleActorSunShadowController>();
        controller.Configure(manager.alliesParent.transform, manager.enemiesParent.transform, material,
            AssetDatabase.LoadAssetAtPath<BattleActorFormLibrary>(BattleActorFormBakeTools.LibraryPath), sun);
        EditorUtility.SetDirty(controller);
        EditorUtility.SetDirty(material);
        AssetDatabase.SaveAssetIfDirty(material);
    }

    private static Material AddSunVolume(Transform atmosphere)
    {
        Material material = ForestMaterial("Canopy Sun Volume", "DiceLand/Battle Forest Sun Volume");
        material.SetColor("_Color", new Color(1.8f, 1.45f, 0.85f, 1f));
        material.SetFloat("_Density", 0.016f);
        material.SetFloat("_BeamWidth", 1.35f);
        material.SetFloat("_Anisotropy", 0.45f);
        Transform volume = atmosphere.Find("Canopy sunlight volume");
        if (volume == null)
        {
            GameObject item = GameObject.CreatePrimitive(PrimitiveType.Cube);
            item.name = "Canopy sunlight volume";
            volume = item.transform;
            volume.SetParent(atmosphere, false);
            UnityEngine.Object.DestroyImmediate(item.GetComponent<Collider>());
        }
        volume.localPosition = new Vector3(0f, 11f, 20f);
        volume.localRotation = Quaternion.identity;
        volume.localScale = new Vector3(70f, 22f, 32f);
        MeshRenderer renderer = volume.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = LightProbeUsage.Off;
        renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        EditorUtility.SetDirty(material);
        AssetDatabase.SaveAssetIfDirty(material);
        EnsureForestDepthFeatures();
        return material;
    }

    private static void EnsureForestDepthFeatures()
    {
        foreach (string tier in new[] { "Very Low", "Low", "Medium", "High", "Very High", "Ultra" })
        {
            string path = "Assets/Settings/" + tier + "_PipelineAsset_ForwardRenderer.asset";
            UniversalRendererData data = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(path);
            if (data == null)
                throw new InvalidOperationException("Missing battle renderer: " + path);
            BattleForestDepthFeature feature = data.rendererFeatures.OfType<BattleForestDepthFeature>().FirstOrDefault();
            if (feature == null)
            {
                feature = ScriptableObject.CreateInstance<BattleForestDepthFeature>();
                feature.name = "Forest transparent depth";
                AssetDatabase.AddObjectToAsset(feature, data);
                data.rendererFeatures.Add(feature);
            }
            feature.SetActive(true);
            SerializedObject serialized = new SerializedObject(data);
            SerializedProperty map = serialized.FindProperty("m_RendererFeatureMap");
            map.arraySize = data.rendererFeatures.Count;
            for (int index = 0; index < data.rendererFeatures.Count; index++)
            {
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(data.rendererFeatures[index], out string guid, out long localId);
                map.GetArrayElementAtIndex(index).longValue = localId;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            data.SetDirty();
            EditorUtility.SetDirty(feature);
            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssetIfDirty(data);
        }
    }

    private static void AddTree(Transform parent, Sprite sprite, Material material, string name, Vector3 position, float scale, Color color, bool flip)
    {
        GameObject tree = new GameObject(name);
        tree.transform.SetParent(parent, false);
        tree.transform.localPosition = position;
        tree.transform.localScale = Vector3.one * scale;
        SpriteRenderer renderer = tree.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sharedMaterial = material;
        renderer.flipX = flip;
        renderer.color = color;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    private static void AddDust(Transform parent)
    {
        Material material = ForestMaterial("Sunlit Dust", "DiceLand/Battle Forest Atmosphere");
        material.SetColor("_Color", new Color(6f, 4.2f, 1.8f, 0.55f));
        material.SetFloat("_Mode", 2f);
        material.SetFloat("_SoftDistance", 0.5f);
        EditorUtility.SetDirty(material);
        AssetDatabase.SaveAssetIfDirty(material);
        GameObject dust = new GameObject("Sunlit drifting dust");
        dust.transform.SetParent(parent, false);
        dust.transform.localPosition = new Vector3(0f, 4f, 6f);
        ParticleSystem particles = dust.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        particles.useAutoRandomSeed = false;
        particles.randomSeed = 20261029;
        ParticleSystem.MainModule main = particles.main;
        main.duration = 10f;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(6f, 10f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.04f, 0.12f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.035f, 0.085f);
        main.maxParticles = 60;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 6f;
        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(28f, 7f, 16f);
        ParticleSystem.VelocityOverLifetimeModule velocity = particles.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.x = new ParticleSystem.MinMaxCurve(0.05f);
        velocity.y = new ParticleSystem.MinMaxCurve(0.025f);
        Gradient fade = new Gradient();
        fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.6f, 0.25f), new GradientAlphaKey(0.45f, 0.75f), new GradientAlphaKey(0f, 1f) });
        ParticleSystem.ColorOverLifetimeModule color = particles.colorOverLifetime;
        color.enabled = true;
        color.color = new ParticleSystem.MinMaxGradient(fade);
        ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        particles.Play();
    }

    private static void AddFallingLeaves(Transform parent)
    {
        Material material = ForestMaterial("Sunlit Falling Leaves", "DiceLand/Battle Forest Atmosphere");
        material.SetColor("_Color", new Color(1.9f, 1.35f, 0.55f, 0.85f));
        material.SetFloat("_Mode", 3f);
        material.SetFloat("_SoftDistance", 0.5f);
        EditorUtility.SetDirty(material);
        AssetDatabase.SaveAssetIfDirty(material);
        GameObject leaves = new GameObject("Sunlit drifting leaves");
        leaves.transform.SetParent(parent, false);
        leaves.transform.localPosition = new Vector3(0f, 5.5f, 4f);
        ParticleSystem particles = leaves.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        particles.useAutoRandomSeed = false;
        particles.randomSeed = 20261101;
        ParticleSystem.MainModule main = particles.main;
        main.duration = 14f;
        main.loop = true;
        main.prewarm = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(10f, 14f);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.16f, 0.33f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.88f, 0.90f, 0.76f), Color.white);
        main.maxParticles = 12;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 0.75f;
        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(28f, 8f, 14f);
        ParticleSystem.VelocityOverLifetimeModule velocity = particles.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.x = new ParticleSystem.MinMaxCurve(0.12f, 0.28f);
        velocity.y = new ParticleSystem.MinMaxCurve(-0.24f, -0.12f);
        velocity.z = new ParticleSystem.MinMaxCurve(-0.04f, 0.04f);
        ParticleSystem.RotationOverLifetimeModule spin = particles.rotationOverLifetime;
        spin.enabled = true;
        spin.z = new ParticleSystem.MinMaxCurve(-0.9f, 0.9f);
        ParticleSystem.NoiseModule noise = particles.noise;
        noise.enabled = true;
        noise.strength = new ParticleSystem.MinMaxCurve(0.08f, 0.16f);
        noise.frequency = 0.18f;
        noise.scrollSpeed = 0.12f;
        noise.quality = ParticleSystemNoiseQuality.Low;
        Gradient fade = new Gradient();
        fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.18f),
                new GradientAlphaKey(0.85f, 0.8f), new GradientAlphaKey(0f, 1f) });
        ParticleSystem.ColorOverLifetimeModule color = particles.colorOverLifetime;
        color.enabled = true;
        color.color = new ParticleSystem.MinMaxGradient(fade);
        ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = material;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        particles.Play();
    }

    [MenuItem("Tools/Battle Visuals/Capture Saved Battle Scene")]
    public static void CaptureSavedBattleScene()
    {
        Capture("battle-after.png", false);
        Debug.Log("Battle preview saved to " + Path.GetFullPath(OutputPath));
    }

    // Render the real running scene even when the editor Game view is not visible.
    // Overlay canvases are moved to the final unprocessed camera for this capture only.
    public static void CaptureLive(string fileName, int width = 1600, int height = 900)
    {
        if (!Application.isPlaying)
            throw new InvalidOperationException("Live capture requires a running battle.");
        Camera camera = UnityEngine.Object.FindObjectsOfType<Camera>()
            .First(item => item.enabled && item.GetUniversalAdditionalCameraData().renderType == CameraRenderType.Base);
        Camera ui = camera.GetUniversalAdditionalCameraData().cameraStack.LastOrDefault();
        Canvas[] canvases = UnityEngine.Object.FindObjectsOfType<Canvas>()
            .Where(item => item.isRootCanvas && item.renderMode == RenderMode.ScreenSpaceOverlay).ToArray();
        Camera[] previousCameras = canvases.Select(item => item.worldCamera).ToArray();
        float[] previousDistances = canvases.Select(item => item.planeDistance).ToArray();
        Dictionary<GameObject, int> previousLayers = new Dictionary<GameObject, int>();
        float previousAspect = camera.aspect;
        try
        {
            camera.aspect = (float)width / height;
            BattleLookController look = UnityEngine.Object.FindObjectOfType<BattleLookController>();
            if (look != null)
                look.PrepareCamera(camera);
            for (int i = 0; i < canvases.Length; i++)
            {
                if (ui != null)
                    foreach (Transform child in canvases[i].GetComponentsInChildren<Transform>(true))
                    {
                        previousLayers.Add(child.gameObject, child.gameObject.layer);
                        child.gameObject.layer = LayerMask.NameToLayer("UI");
                    }
                canvases[i].renderMode = RenderMode.ScreenSpaceCamera;
                canvases[i].worldCamera = ui != null ? ui : camera;
                canvases[i].planeDistance = Mathf.Max(1f, camera.nearClipPlane + 0.1f);
            }
            Canvas.ForceUpdateCanvases();
            Render(camera, Path.Combine(OutputPath, fileName), width, height, true);
        }
        finally
        {
            foreach (KeyValuePair<GameObject, int> entry in previousLayers)
                if (entry.Key != null)
                    entry.Key.layer = entry.Value;
            camera.aspect = previousAspect;
            for (int i = 0; i < canvases.Length; i++)
            {
                canvases[i].renderMode = RenderMode.ScreenSpaceOverlay;
                canvases[i].worldCamera = previousCameras[i];
                canvases[i].planeDistance = previousDistances[i];
            }
            Canvas.ForceUpdateCanvases();
            BattleLookController look = UnityEngine.Object.FindObjectOfType<BattleLookController>();
            if (look != null)
                look.PrepareCamera(camera);
        }
    }

    public static void Capture(string fileName, bool legacy, int qualityOverride = -1, int width = 1600, int height = 900)
    {
        CapturePreview(fileName, legacy, qualityOverride, width, height, null);
    }

    // Repeatable off-centre / wide-lens probes without moving the saved gameplay cameras.
    public static void CaptureCameraProbe(string fileName, Vector3 position, Quaternion rotation, float fieldOfView,
        int qualityOverride = 2, int width = 1920, int height = 884)
    {
        CapturePreview(fileName, false, qualityOverride, width, height, camera =>
        {
            camera.transform.SetPositionAndRotation(position, rotation);
            camera.fieldOfView = fieldOfView;
        });
    }

    private static void CapturePreview(string fileName, bool legacy, int qualityOverride, int width, int height, Action<Camera> configureCamera)
    {
        Scene original = SceneManager.GetActiveScene();
        if (Enumerable.Range(0, SceneManager.sceneCount).Any(i => SceneManager.GetSceneAt(i).path == ScenePath))
            throw new InvalidOperationException("Close the battle scene before capturing its saved version; open scene changes are never replaced.");
        int quality = QualitySettings.GetQualityLevel();
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        BattleForestStageController stage = null;
        BattleContactShadowController shadows = null;
        BattleActorSunShadowController sunShadows = null;
        BattleLookController look = null;
        BattleActorLightingController actorLighting = null;
        BattleBackdropCoverageController coverage = null;
        try
        {
            if (qualityOverride >= 0)
                QualitySettings.SetQualityLevel(qualityOverride, false);
            SceneManager.SetActiveScene(scene);
            Camera camera = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Camera>(true))
                .First(item => item.CompareTag("MainCamera"));
            camera.scene = scene;
            camera.aspect = (float)width / height;
            AddSampleActors(scene);
            actorLighting = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<BattleActorLightingController>(true)).FirstOrDefault();
            if (actorLighting != null && !legacy)
                actorLighting.UpdateLighting();
            coverage = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<BattleBackdropCoverageController>(true)).FirstOrDefault();
            if (coverage != null && !legacy)
                coverage.InitializeCoverage();
            stage = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<BattleForestStageController>(true)).FirstOrDefault();
            if (stage != null && !legacy)
                stage.InitializeStage();
            shadows = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<BattleContactShadowController>(true)).FirstOrDefault();
            if (shadows != null && !legacy)
                shadows.UpdateShadows();
            sunShadows = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<BattleActorSunShadowController>(true)).FirstOrDefault();
            if (sunShadows != null && !legacy)
                sunShadows.UpdateShadows();
            look = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<BattleLookController>(true)).FirstOrDefault();
            if (legacy)
                ApplyLegacyLook(scene);
            else if (look != null)
            {
                look.InitializeLook();
                look.PrepareCamera(camera);
            }
            foreach (ParticleSystem particles in scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<ParticleSystem>(true)))
                if (particles.name == "Sunlit drifting dust")
                    particles.Simulate(5f, true, true);
            configureCamera?.Invoke(camera);
            Render(camera, Path.Combine(OutputPath, fileName), width, height);
        }
        finally
        {
            if (actorLighting != null)
                actorLighting.ReleaseLighting();
            if (coverage != null)
                coverage.ReleaseCoverage();
            if (look != null)
                look.ReleaseLook();
            if (shadows != null)
                shadows.ReleaseShadows();
            if (sunShadows != null)
                sunShadows.ReleaseShadows();
            if (stage != null)
                stage.ReleaseStage();
            QualitySettings.SetQualityLevel(quality, false);
            SceneManager.SetActiveScene(original);
            EditorSceneManager.CloseScene(scene, true);
        }
    }

    private static void AddSampleActors(Scene scene)
    {
        BattleManager manager = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<BattleManager>(true)).First();
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Characters/Chara_InBattle.prefab");
        GameObject ally = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        ally.transform.SetParent(manager.alliesParent.transform, false);
        ally.transform.position = new Vector3(-6.2f, 0.12f, 0f);
        foreach (SpriteRenderer renderer in ally.GetComponentsInChildren<SpriteRenderer>())
            renderer.sharedMaterial = manager.EnemyMaterial;
        if (BattleActorVisualUtility.TryGetActorSpriteBounds(ally.transform, out Bounds allyBounds))
            ally.transform.position += Vector3.up * (0.12f - allyBounds.min.y);

        AddEnemy(manager, "Assets/Data/Enemies/Enemies/006 BloodPearl/BloodPearl.png", new Vector3(5.4f, 0.12f, 0f), 3.8f);
        AddEnemy(manager, "Assets/Data/Enemies/Enemies/006 BloodPearl/BloodPearl.png", new Vector3(9.1f, 0.12f, 3.3f), 3.2f);
    }

    private static void AddEnemy(BattleManager manager, string path, Vector3 position, float height)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null)
            return;
        GameObject actor = new GameObject("Preview Blood Spore (visual only)");
        actor.transform.SetParent(manager.enemiesParent.transform, false);
        SpriteRenderer renderer = actor.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sharedMaterial = manager.EnemyMaterial;
        renderer.shadowCastingMode = ShadowCastingMode.On;
        renderer.receiveShadows = true;
        float scale = height / Mathf.Max(0.01f, sprite.bounds.size.y);
        actor.transform.localScale = Vector3.one * scale;
        actor.transform.position = position + Vector3.up * (-sprite.bounds.min.y * scale);
    }

    private static void ApplyLegacyLook(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == "Battle Atmosphere")
                root.SetActive(false);
            foreach (Light light in root.GetComponentsInChildren<Light>(true))
            {
                if (light.type == LightType.Directional)
                {
                    light.color = new Color(1f, 0.7295598f, 0f);
                    light.intensity = 1.1f;
                    light.shadowStrength = 1f;
                }
                else if (light.type == LightType.Point)
                {
                    light.color = new Color(1f, 0.9109521f, 0f);
                    light.intensity = 6.68f;
                    light.range = 4.13f;
                    light.transform.position = new Vector3(-0.73f, 7.57f, 6.719f);
                }
            }
            foreach (Volume volume in root.GetComponentsInChildren<Volume>(true))
                if (volume.gameObject.name == "Global Volume")
                    volume.sharedProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/Scenes/SampleScene/Global Volume Profile.asset");
            foreach (MeshRenderer renderer in root.GetComponentsInChildren<MeshRenderer>(true))
                if (renderer.name == "Plane")
                    renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Assets/Materials/Ground.mat");
        }
        RenderSettings.fog = false;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.088050246f, 0.088050246f, 0.088050246f);
        Camera camera = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Camera>(true)).First(item => item.CompareTag("MainCamera"));
        camera.orthographic = true;
        camera.orthographicSize = 7.41f;
        camera.clearFlags = CameraClearFlags.Skybox;
        camera.GetUniversalAdditionalCameraData().antialiasing = AntialiasingMode.None;
        scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<MeshRenderer>(true))
            .First(renderer => renderer.name == "Plane").transform.localScale = new Vector3(6.603027f, 6.603027f, 20.103577f);
    }

    private static void Render(Camera camera, string path, int width, int height, bool fullStack = false)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        RenderTexture previous = RenderTexture.active;
        RenderTexture target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        Texture2D image = new Texture2D(width, height, TextureFormat.RGB24, false);
        Camera[] captureCameras = fullStack
            ? new[] { camera }.Concat(camera.GetUniversalAdditionalCameraData().cameraStack.Where(item => item != null)).ToArray()
            : new[] { camera };
        RenderTexture[] previousTargets = captureCameras.Select(item => item.targetTexture).ToArray();
        target.Create();
        try
        {
            foreach (Camera captureCamera in captureCameras)
                captureCamera.targetTexture = target;
            if (fullStack)
                Canvas.ForceUpdateCanvases();
            for (int i = 0; i < 2; i++)
            {
                if (fullStack)
                    RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
                else
                    RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
            }
            RenderTexture.active = target;
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            image.Apply();
            File.WriteAllBytes(path, image.EncodeToPNG());
        }
        finally
        {
            for (int i = 0; i < captureCameras.Length; i++)
                captureCameras[i].targetTexture = previousTargets[i];
            RenderTexture.active = previous;
            target.Release();
            UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.DestroyImmediate(image);
        }
    }
}
