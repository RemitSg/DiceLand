using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>Keeps both battle cameras focused on combatants instead of scenery.</summary>
[DisallowMultipleComponent]
public sealed class BattleLookController : MonoBehaviour
{
    [SerializeField] private Camera overviewCamera;
    [SerializeField] private Camera closeupCamera;
    [SerializeField] private Volume lookVolume;
    [SerializeField] private Transform allies;
    [SerializeField] private Transform enemies;
    [SerializeField] private Renderer midground;
    [SerializeField, Min(0f)] private float sharpDepthPadding = 2f;
    [SerializeField, Min(1f)] private float backgroundBlurDistance = 12f;
    [SerializeField, Range(0f, 1f)] private float motionBlurIntensity = 0.22f;
    [Header("Overview framing")]
    [SerializeField, Range(15f, 50f)] private float overviewReferenceFieldOfView = 28f;
    [SerializeField, Min(1f)] private float overviewReferenceAspect = 1.777778f;
    [SerializeField, Range(0.1f, 0.4f)] private float groundViewportHeight = 0.22f;
    private float previousOverviewAspect = -1f;

    private readonly List<SpriteRenderer> actorRenderers = new List<SpriteRenderer>();
    private readonly List<SpriteRenderer> scratchRenderers = new List<SpriteRenderer>();
    private readonly List<BattleEnemyController> scratchEnemies = new List<BattleEnemyController>();
    private VolumeProfile runtimeProfile;
    private VolumeProfile originalProfile;
    private DepthOfField depthOfField;
    private MotionBlur motionBlur;
    private float nextActorRefresh;
    private Camera worldUiCamera;
    private readonly Dictionary<Camera, int> originalCameraMasks = new Dictionary<Camera, int>();
    private readonly Dictionary<GameObject, int> originalUiLayers = new Dictionary<GameObject, int>();
    private readonly List<SpeedButton> scratchButtons = new List<SpeedButton>();
    private readonly List<Transform> scratchTransforms = new List<Transform>();
    private const int WorldUiLayer = 5; // Built-in UI layer; no project layer changes.

    private void OnEnable()
    {
        InitializeLook();
    }

    public void InitializeLook()
    {
        if (runtimeProfile != null || lookVolume == null || lookVolume.sharedProfile == null)
            return;

        originalProfile = lookVolume.sharedProfile;
        // Instantiate the components too: a shallow profile copy would edit the asset.
        runtimeProfile = ScriptableObject.CreateInstance<VolumeProfile>();
        foreach (VolumeComponent component in originalProfile.components)
            runtimeProfile.components.Add(Instantiate(component));
        runtimeProfile.TryGet(out depthOfField);
        runtimeProfile.TryGet(out motionBlur);
        lookVolume.sharedProfile = runtimeProfile;
        InitializeWorldUiCamera();
        RefreshActors();
        RenderPipelineManager.beginCameraRendering += BeforeCameraRendering;
    }

    private void BeforeCameraRendering(ScriptableRenderContext context, Camera camera)
    {
        if (camera != overviewCamera && camera != closeupCamera)
            return;
        PrepareCamera(camera);
    }

    public void PrepareCamera(Camera camera)
    {
        if (camera == null || depthOfField == null)
            return;
        if (camera == overviewCamera && !camera.orthographic && Mathf.Abs(camera.aspect - previousOverviewAspect) > 0.001f)
        {
            // Preserve horizontal formation coverage on wide phones, rather than shrinking the actors.
            float fieldOfView = CalculateOverviewFieldOfView(overviewReferenceFieldOfView, overviewReferenceAspect, camera.aspect);
            float pitch = camera.transform.eulerAngles.x * Mathf.Deg2Rad;
            float slope = (2f * groundViewportHeight - 1f) * Mathf.Tan(fieldOfView * 0.5f * Mathf.Deg2Rad);
            Vector3 position = camera.transform.position;
            float denominator = Mathf.Cos(pitch) + slope * Mathf.Sin(pitch);
            if (Mathf.Abs(denominator) > 0.1f)
                position.y = 0.045f - position.z * (Mathf.Sin(pitch) - slope * Mathf.Cos(pitch)) / denominator;
            camera.transform.position = position;
            camera.fieldOfView = fieldOfView;
            previousOverviewAspect = camera.aspect;
        }
        if (worldUiCamera != null && (camera == overviewCamera || camera == closeupCamera))
        {
            if (!SpeedButton.CombatLocked && enemies != null)
            {
                scratchEnemies.Clear();
                enemies.GetComponentsInChildren(false, scratchEnemies);
                foreach (BattleEnemyController enemy in scratchEnemies)
                    enemy.FitSpeedButtonsToCamera(camera, Screen.safeArea);
            }
            SyncWorldUiCamera(camera);
            MoveWorldUi(allies, camera);
            MoveWorldUi(enemies, camera);
        }
        if (Time.unscaledTime >= nextActorRefresh)
            RefreshActors();

        float nearest = float.PositiveInfinity;
        float farthest = 0f;
        Vector3 forward = camera.transform.forward;
        foreach (SpriteRenderer renderer in actorRenderers)
        {
            if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
                continue;
            Bounds bounds = renderer.bounds;
            Vector3 viewport = camera.WorldToViewportPoint(bounds.center);
            if (viewport.z <= camera.nearClipPlane || viewport.x < -0.15f || viewport.x > 1.15f ||
                viewport.y < -0.15f || viewport.y > 1.15f)
                continue;

            float depth = Vector3.Dot(bounds.center - camera.transform.position, forward);
            float extent = Vector3.Dot(bounds.extents, new Vector3(Mathf.Abs(forward.x), Mathf.Abs(forward.y), Mathf.Abs(forward.z)));
            nearest = Mathf.Min(nearest, depth - extent);
            farthest = Mathf.Max(farthest, depth + extent);
        }
        if (farthest <= 0f)
        {
            nearest = farthest = Mathf.Max(camera.nearClipPlane + 1f,
                Vector3.Dot(new Vector3(0f, 1.5f, 0f) - camera.transform.position, forward));
        }

        // The overview is an environmental composition: keep the authored tree/ruin layer
        // readable and let only the distant landscape fall out of focus. Closeups follow actors.
        float sharpEnd = farthest;
        if (camera == overviewCamera && midground != null && midground.enabled && midground.gameObject.activeInHierarchy)
        {
            Bounds bounds = midground.bounds;
            float depth = Vector3.Dot(bounds.center - camera.transform.position, forward);
            float extent = Vector3.Dot(bounds.extents,
                new Vector3(Mathf.Abs(forward.x), Mathf.Abs(forward.y), Mathf.Abs(forward.z)));
            if (depth - extent > camera.nearClipPlane)
                sharpEnd = Mathf.Max(sharpEnd, depth + extent);
        }
        Vector2 range = CalculateFocusRange(nearest, sharpEnd, sharpDepthPadding,
            backgroundBlurDistance * (camera == overviewCamera ? 2f : 1f));
        bool cinematic = !Application.isMobilePlatform && QualitySettings.GetQualityLevel() >= 3;
        depthOfField.mode.Override(cinematic && camera != overviewCamera ? DepthOfFieldMode.Bokeh : DepthOfFieldMode.Gaussian);
        depthOfField.focusDistance.Override(Mathf.Max(camera.nearClipPlane + 0.1f, (nearest + farthest) * 0.5f));
        depthOfField.focalLength.Override(camera == overviewCamera ? 110f : 65f);
        depthOfField.aperture.Override(camera == overviewCamera ? 4.5f : 5.6f);
        depthOfField.gaussianStart.Override(range.x);
        depthOfField.gaussianEnd.Override(range.y);
        depthOfField.gaussianMaxRadius.Override(camera == overviewCamera ? 1f : 0.85f);
        depthOfField.highQualitySampling.Override(!Application.isMobilePlatform && QualitySettings.GetQualityLevel() >= 3);
        // Tuanjie 14.1 adds object motion blur, but the low tier keeps the cheaper camera path.
        if (motionBlur != null)
        {
            bool lowQuality = QualitySettings.GetQualityLevel() < 2;
            motionBlur.mode.Override(lowQuality ? MotionBlurMode.CameraOnly : MotionBlurMode.CameraAndObjects);
            motionBlur.quality.Override(MotionBlurQuality.Low);
            motionBlur.intensity.Override(lowQuality ? 0f : motionBlurIntensity);
            motionBlur.clamp.Override(0.018f);
        }
    }

    public static Vector2 CalculateFocusRange(float nearest, float farthest, float padding, float falloff)
    {
        float start = Mathf.Max(0.1f, Mathf.Max(nearest, farthest) + Mathf.Max(0f, padding));
        return new Vector2(start, start + Mathf.Max(1f, falloff));
    }

    public static float CalculateOverviewFieldOfView(float referenceFieldOfView, float referenceAspect, float aspect)
    {
        float halfHeight = Mathf.Tan(referenceFieldOfView * 0.5f * Mathf.Deg2Rad);
        return Mathf.Clamp(2f * Mathf.Atan(halfHeight * referenceAspect / Mathf.Max(0.5f, aspect)) * Mathf.Rad2Deg, 15f, 60f);
    }

    private void InitializeWorldUiCamera()
    {
        // World-space speed slots and lines must remain readable after scene post-processing.
        GameObject cameraObject = new GameObject("Battle World UI Camera");
        cameraObject.transform.SetParent(transform, false);
        worldUiCamera = cameraObject.AddComponent<Camera>();
        worldUiCamera.clearFlags = CameraClearFlags.Depth;
        worldUiCamera.cullingMask = 1 << WorldUiLayer;
        worldUiCamera.eventMask = 1 << WorldUiLayer;
        UniversalAdditionalCameraData data = worldUiCamera.GetUniversalAdditionalCameraData();
        data.renderType = CameraRenderType.Overlay;
        data.renderPostProcessing = false;
        data.renderShadows = false;
        data.requiresDepthTexture = false;
        data.requiresColorTexture = false;
        AttachWorldUiCamera(overviewCamera);
        AttachWorldUiCamera(closeupCamera);
    }

    private void AttachWorldUiCamera(Camera camera)
    {
        if (camera == null || originalCameraMasks.ContainsKey(camera))
            return;
        List<Camera> stack = camera.GetUniversalAdditionalCameraData().cameraStack;
        if (stack == null)
            return;
        originalCameraMasks.Add(camera, camera.cullingMask);
        camera.cullingMask &= ~(1 << WorldUiLayer);
        stack.Add(worldUiCamera);
    }

    private void SyncWorldUiCamera(Camera camera)
    {
        worldUiCamera.transform.SetPositionAndRotation(camera.transform.position, camera.transform.rotation);
        worldUiCamera.orthographic = camera.orthographic;
        worldUiCamera.orthographicSize = camera.orthographicSize;
        worldUiCamera.fieldOfView = camera.fieldOfView;
        worldUiCamera.nearClipPlane = camera.nearClipPlane;
        worldUiCamera.farClipPlane = camera.farClipPlane;
        worldUiCamera.aspect = camera.aspect;
        worldUiCamera.rect = camera.rect;
        worldUiCamera.projectionMatrix = camera.projectionMatrix;
        worldUiCamera.allowHDR = camera.allowHDR;
        worldUiCamera.allowMSAA = camera.allowMSAA;
        worldUiCamera.targetDisplay = camera.targetDisplay;
    }

    private void MoveWorldUi(Transform root, Camera camera)
    {
        if (root == null)
            return;
        scratchButtons.Clear();
        root.GetComponentsInChildren(true, scratchButtons);
        foreach (SpeedButton button in scratchButtons)
        {
            scratchTransforms.Clear();
            button.GetComponentsInChildren(true, scratchTransforms);
            foreach (Transform item in scratchTransforms)
                MoveWorldUiObject(item.gameObject);
            // Arrows deliberately live at the scene root to sort above character groups.
            MoveWorldUiObject(button.ArrowVisual);
            button.RefreshIntentForCamera(camera);
        }
    }

    private void MoveWorldUiObject(GameObject target)
    {
        if (target == null)
            return;
        if (!originalUiLayers.ContainsKey(target))
            originalUiLayers.Add(target, target.layer);
        target.layer = WorldUiLayer;
    }

    private void RefreshActors()
    {
        actorRenderers.Clear();
        CollectActors(allies);
        CollectActors(enemies);
        nextActorRefresh = Time.unscaledTime + 0.25f;
    }

    private void CollectActors(Transform root)
    {
        if (root == null)
            return;
        scratchRenderers.Clear();
        root.GetComponentsInChildren(false, scratchRenderers);
        foreach (SpriteRenderer renderer in scratchRenderers)
        {
            if (renderer.GetComponentInParent<SpeedButton>() == null && renderer.GetComponentInParent<Weapon>() == null)
                actorRenderers.Add(renderer);
        }
    }

    private void OnDisable()
    {
        ReleaseLook();
    }

    public void ReleaseLook()
    {
        RenderPipelineManager.beginCameraRendering -= BeforeCameraRendering;
        foreach (KeyValuePair<GameObject, int> entry in originalUiLayers)
            if (entry.Key != null)
                entry.Key.layer = entry.Value;
        originalUiLayers.Clear();
        foreach (KeyValuePair<Camera, int> entry in originalCameraMasks)
        {
            if (entry.Key == null)
                continue;
            entry.Key.cullingMask = entry.Value;
            entry.Key.GetUniversalAdditionalCameraData().cameraStack?.Remove(worldUiCamera);
        }
        originalCameraMasks.Clear();
        if (worldUiCamera != null)
        {
            worldUiCamera.enabled = false;
            Release(worldUiCamera.gameObject);
            worldUiCamera = null;
        }
        if (lookVolume != null && lookVolume.sharedProfile == runtimeProfile)
            lookVolume.sharedProfile = originalProfile;
        if (runtimeProfile != null)
        {
            foreach (VolumeComponent component in runtimeProfile.components)
                Release(component);
            Release(runtimeProfile);
        }
        runtimeProfile = null;
        depthOfField = null;
        motionBlur = null;
        previousOverviewAspect = -1f;
    }

    private static void Release(Object value)
    {
        if (Application.isPlaying)
            Destroy(value);
        else
            DestroyImmediate(value);
    }
}
