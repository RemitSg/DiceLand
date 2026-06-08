using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum MapBackgroundPhase
{
    Morning = 0,
    Noon = 1,
    Dusk = 2,
    Midnight = 3
}

[DisallowMultipleComponent]
public class MapBackgroundController : MonoBehaviour
{
    private const float CameraSpaceBackgroundDepth = 10f;

    [Header("Roots")]
    [SerializeField] private Transform backgroundRoot;

    [Header("Background Image Interfaces")]
    [SerializeField] private Sprite morningBackgroundSprite;
    [SerializeField] private Sprite noonBackgroundSprite;
    [SerializeField] private Sprite duskBackgroundSprite;
    [SerializeField] private Sprite midnightBackgroundSprite;
    [SerializeField, Range(0f, 1f)] private float backgroundImageAlpha = 1f;
    [SerializeField, Range(0f, 1f)] private float backgroundImageGradientWashAlpha = 0.38f;
    [SerializeField, Range(0, 16)] private int backgroundImageBlurRadius = 6;
    [SerializeField, Range(0, 4)] private int backgroundImageBlurIterations = 2;
    [SerializeField, Range(64, 1024)] private int blurredTextureMaxSize = 512;

    [Header("Rendering")]
    [SerializeField] private Material backgroundMaterial;
    [SerializeField] private Material ambientParticleMaterial;
    [SerializeField] private int sortingOrderBase = -200;
    [SerializeField] private float extentPadding = 10f;

    [Header("Palette")]
    [SerializeField] private Color topGradientColor = new Color(0.06f, 0.12f, 0.2f, 1f);
    [SerializeField] private Color bottomGradientColor = new Color(0.02f, 0.05f, 0.09f, 1f);
    [SerializeField] private Color centerGlowColor = new Color(0.25f, 0.62f, 0.9f, 0.2f);
    [SerializeField] private Color sideGlowColor = new Color(0.95f, 0.45f, 0.22f, 0.12f);
    [SerializeField] private Color mistColor = new Color(0.88f, 0.94f, 1f, 0.08f);
    [SerializeField] private Color vignetteColor = new Color(0.02f, 0.03f, 0.05f, 0.38f);

    [Header("Motion")]
    [SerializeField] private float driftAmplitude = 0.28f;
    [SerializeField] private float driftSpeed = 0.24f;
    [SerializeField] private float rotationDriftAngle = 4f;

    [Header("Background Transition")]
    [SerializeField, Min(0f)] private float fadeToBlackDuration = 0.22f;
    [SerializeField, Min(0f)] private float fadeFromBlackDuration = 0.32f;

    [Header("Ambient Particles")]
    [SerializeField] private bool enableAmbientParticles = true;
    [SerializeField] private int ambientParticleBurst = 36;
    [SerializeField] private float ambientParticleMinSize = 0.1f;
    [SerializeField] private float ambientParticleMaxSize = 0.22f;
    [SerializeField] private float ambientParticleMinSpeed = 0.09f;
    [SerializeField] private float ambientParticleMaxSpeed = 0.28f;
    [SerializeField] private float ambientParticleLifetime = 8f;
    [SerializeField] private Color ambientParticleColor = new Color(0.92f, 0.98f, 1f, 0.3f);
    [SerializeField] private float ambientParticleGlowScale = 1.35f;
    [SerializeField] private float ambientParticleHorizontalDrift = 0.04f;
    [SerializeField] private float ambientParticleVerticalDrift = 0.13f;
    [SerializeField] private float ambientParticleFadeInPortion = 0.14f;
    [SerializeField] private float ambientParticleFadeOutPortion = 0.32f;
    [SerializeField] private float ambientParticleCoreRadius = 0.14f;
    [SerializeField] private float ambientParticleHaloSoftness = 0.9f;

    private readonly List<LayerMotion> movingLayers = new List<LayerMotion>();
    private Camera targetCamera;
    private BoardManager boardManager;
    private Sprite verticalGradientSprite;
    private Sprite radialGradientSprite;
    private Sprite edgeVignetteSprite;
    private Sprite bandGradientSprite;
    private Sprite glowParticleSprite;
    private Sprite solidSprite;
    private Material runtimeAmbientParticleMaterial;
    private readonly Dictionary<Sprite, Sprite> blurredSpriteCache = new Dictionary<Sprite, Sprite>();
    private ActionPointSystem actionPointSystem;
    private MapBackgroundPhase currentBackgroundPhase = MapBackgroundPhase.Morning;
    private Coroutine backgroundTransitionRoutine;
    private Vector3 initialBackgroundRootScale = Vector3.one;
    private float initialCameraOrthographicSize = 1f;

    public void Initialize(Transform root, Camera mapCamera, BoardManager board)
    {
        Initialize(root, mapCamera, board, null);
    }

    public void Initialize(Transform root, Camera mapCamera, BoardManager board, ActionPointSystem points)
    {
        if (backgroundTransitionRoutine != null)
        {
            StopCoroutine(backgroundTransitionRoutine);
            backgroundTransitionRoutine = null;
        }

        targetCamera = mapCamera;
        boardManager = board;
        BindActionPointSystem(points);
        currentBackgroundPhase = ResolveCurrentActionPointPhase();

        if (root != null)
            backgroundRoot = root;
        if (backgroundRoot == null)
        {
            GameObject go = new GameObject("BackgroundRoot");
            go.transform.SetParent(transform, false);
            backgroundRoot = go.transform;
        }

        if (targetCamera == null || boardManager == null || boardManager.BoardSize <= 0)
            return;

        AttachBackgroundRootToCamera();
        initialBackgroundRootScale = Vector3.one;
        backgroundRoot.localScale = initialBackgroundRootScale;
        initialCameraOrthographicSize = targetCamera.orthographic
            ? Mathf.Max(0.01f, targetCamera.orthographicSize)
            : 1f;
        UpdateCameraSpaceBackgroundTransform();
        EnsureRuntimeSprites();
        ClearExisting();
        BuildBackground();
    }

    public static MapBackgroundPhase ResolveActionPointBackgroundPhase(int currentPoints, int maxPoints)
    {
        int safeMax = Mathf.Max(1, maxPoints);
        float ratio = Mathf.Clamp01(Mathf.Max(0, currentPoints) / (float)safeMax);
        if (ratio >= 0.6f)
            return MapBackgroundPhase.Morning;
        if (ratio >= 0.2f)
            return MapBackgroundPhase.Noon;

        return MapBackgroundPhase.Dusk;
    }

    public Sprite GetBlurredMidnightBackgroundSprite()
    {
        return GetBlurredBackgroundSprite(ResolveSpriteWithFallback(midnightBackgroundSprite, duskBackgroundSprite, noonBackgroundSprite, morningBackgroundSprite));
    }

    public static float CalculateEdgeVignetteAlpha(float normalizedDistanceFromCenter)
    {
        float edgeWeight = Mathf.InverseLerp(0.52f, 1f, Mathf.Clamp01(normalizedDistanceFromCenter));
        return edgeWeight * edgeWeight * (3f - 2f * edgeWeight);
    }

    public static float CalculateCameraCoverWorldSize(float fallbackWorldSize, float orthographicSize, float aspect, float padding)
    {
        float coverSize = Mathf.Max(0.01f, fallbackWorldSize);
        float height = Mathf.Max(0.01f, orthographicSize * 2f);
        float width = height * Mathf.Max(0.01f, aspect);
        return Mathf.Max(coverSize, Mathf.Max(width, height) + Mathf.Max(0f, padding) * 2f);
    }

    public static float CalculateViewLockedScaleRatio(float initialOrthographicSize, float currentOrthographicSize)
    {
        return Mathf.Max(0.01f, currentOrthographicSize) / Mathf.Max(0.01f, initialOrthographicSize);
    }

    private void OnDestroy()
    {
        if (backgroundTransitionRoutine != null)
        {
            StopCoroutine(backgroundTransitionRoutine);
            backgroundTransitionRoutine = null;
        }

        BindActionPointSystem(null);
    }

    private void Update()
    {
        if (movingLayers.Count == 0)
            return;

        float time = Time.unscaledTime;
        for (int i = 0; i < movingLayers.Count; i++)
        {
            LayerMotion motion = movingLayers[i];
            if (motion.transform == null)
                continue;

            float wave = Mathf.Sin(time * motion.speed + motion.phase);
            float swirl = Mathf.Cos(time * motion.speed * 0.85f + motion.phase * 1.37f);
            motion.transform.localPosition = motion.basePosition + new Vector3(wave * motion.positionAmplitude.x, swirl * motion.positionAmplitude.y, 0f);
            motion.transform.localRotation = Quaternion.Euler(0f, 0f, motion.baseRotation + wave * motion.rotationAmplitude);
        }
    }

    private void LateUpdate()
    {
        UpdateCameraSpaceBackgroundTransform();
    }

    private void AttachBackgroundRootToCamera()
    {
        if (targetCamera == null || backgroundRoot == null)
            return;

        if (backgroundRoot.parent != targetCamera.transform)
            backgroundRoot.SetParent(targetCamera.transform, false);

        backgroundRoot.localPosition = new Vector3(0f, 0f, CameraSpaceBackgroundDepth);
        backgroundRoot.localRotation = Quaternion.identity;
    }

    private void UpdateCameraSpaceBackgroundTransform()
    {
        if (targetCamera == null || backgroundRoot == null)
            return;

        AttachBackgroundRootToCamera();

        if (!targetCamera.orthographic)
            return;

        float scaleRatio = CalculateViewLockedScaleRatio(initialCameraOrthographicSize, targetCamera.orthographicSize);
        backgroundRoot.localScale = initialBackgroundRootScale * scaleRatio;
    }

    private void BuildBackground()
    {
        float extent = boardManager.BoardHalfExtentWorld + extentPadding;
        float backgroundCoverSize = CalculateCameraCoverWorldSize(extent * 3.4f);
        Sprite selectedBackground = GetBlurredBackgroundSprite(ResolveMapBackgroundSprite(currentBackgroundPhase));

        if (selectedBackground != null)
        {
            CreateLayer(
                "ImageBackdrop",
                selectedBackground,
                Vector3.zero,
                CalculateCoverScale(selectedBackground, backgroundCoverSize),
                new Color(1f, 1f, 1f, backgroundImageAlpha),
                sortingOrderBase,
                0f,
                Vector2.zero);

            CreateLayer(
                "BackdropGradientWash",
                verticalGradientSprite,
                Vector3.zero,
                CalculateCoverScale(verticalGradientSprite, backgroundCoverSize),
                new Color(1f, 1f, 1f, backgroundImageGradientWashAlpha),
                sortingOrderBase + 1,
                0f,
                Vector2.zero);
        }
        else
        {
            CreateLayer(
                "BackdropGradient",
                verticalGradientSprite,
                Vector3.zero,
                CalculateCoverScale(verticalGradientSprite, backgroundCoverSize),
                Color.white,
                sortingOrderBase,
                0f,
                Vector2.zero);
        }

        CreateLayer(
            "CenterGlow",
            radialGradientSprite,
            new Vector3(0f, 0f, 0f),
            new Vector2(extent * 1.95f, extent * 1.95f),
            centerGlowColor,
            sortingOrderBase + 1,
            rotationDriftAngle * 0.35f,
            new Vector2(driftAmplitude * 0.5f, driftAmplitude * 0.35f));

        CreateLayer(
            "SideGlow",
            radialGradientSprite,
            new Vector3(-extent * 0.56f, extent * 0.24f, 0f),
            new Vector2(extent * 1.45f, extent * 1.45f),
            sideGlowColor,
            sortingOrderBase + 2,
            rotationDriftAngle,
            new Vector2(driftAmplitude, driftAmplitude * 0.75f));

        CreateLayer(
            "MistBandA",
            bandGradientSprite,
            new Vector3(extent * 0.28f, extent * 0.18f, 0f),
            new Vector2(extent * 2.6f, extent * 0.9f),
            mistColor,
            sortingOrderBase + 3,
            rotationDriftAngle * 0.7f,
            new Vector2(driftAmplitude * 0.75f, driftAmplitude * 0.3f),
            22f,
            driftSpeed * 1.2f);

        CreateLayer(
            "MistBandB",
            bandGradientSprite,
            new Vector3(-extent * 0.32f, -extent * 0.08f, 0f),
            new Vector2(extent * 2.15f, extent * 0.72f),
            new Color(mistColor.r, mistColor.g, mistColor.b, mistColor.a * 0.7f),
            sortingOrderBase + 4,
            rotationDriftAngle * 0.55f,
            new Vector2(driftAmplitude * 0.52f, driftAmplitude * 0.25f),
            -16f,
            driftSpeed * 0.9f);

        CreateLayer(
            "Vignette",
            edgeVignetteSprite,
            Vector3.zero,
            Vector2.one * backgroundCoverSize,
            vignetteColor,
            sortingOrderBase + 5,
            0f,
            Vector2.zero);

        if (enableAmbientParticles)
            CreateAmbientParticles(extent);
    }

    private SpriteRenderer CreateLayer(
        string name,
        Sprite sprite,
        Vector3 localPosition,
        Vector2 size,
        Color color,
        int sortingOrder,
        float rotationAmplitude,
        Vector2 positionAmplitude,
        float baseRotation = 0f,
        float speedMultiplier = 1f)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(backgroundRoot, false);
        go.transform.localPosition = localPosition;
        go.transform.localRotation = Quaternion.Euler(0f, 0f, baseRotation);
        go.transform.localScale = new Vector3(size.x, size.y, 1f);

        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;
        if (backgroundMaterial != null)
            renderer.sharedMaterial = backgroundMaterial;

        if (rotationAmplitude > 0.001f || positionAmplitude.sqrMagnitude > 0.0001f)
        {
            movingLayers.Add(new LayerMotion
            {
                transform = go.transform,
                basePosition = localPosition,
                baseRotation = baseRotation,
                positionAmplitude = positionAmplitude,
                rotationAmplitude = rotationAmplitude,
                speed = driftSpeed * Mathf.Max(0.01f, speedMultiplier),
                phase = Random.Range(0f, Mathf.PI * 2f)
            });
        }

        return renderer;
    }

    private void CreateAmbientParticles(float extent)
    {
        GameObject go = new GameObject("AmbientParticles");
        go.transform.SetParent(backgroundRoot, false);
        go.transform.localPosition = new Vector3(0f, 0f, 0f);

        ParticleSystem particles = go.AddComponent<ParticleSystem>();
        var main = particles.main;
        main.loop = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = ambientParticleLifetime;
        main.startSpeed = new ParticleSystem.MinMaxCurve(ambientParticleMinSpeed, ambientParticleMaxSpeed);
        main.startSize = new ParticleSystem.MinMaxCurve(
            ambientParticleMinSize * ambientParticleGlowScale,
            ambientParticleMaxSize * ambientParticleGlowScale);
        main.startColor = ambientParticleColor;
        main.maxParticles = Mathf.Max(ambientParticleBurst * 2, 24);

        var emission = particles.emission;
        emission.rateOverTime = Mathf.Max(ambientParticleBurst / Mathf.Max(1f, ambientParticleLifetime), 2f);
        emission.SetBursts(new[]
        {
            new ParticleSystem.Burst(0f, (short)Mathf.Max(ambientParticleBurst, 1))
        });

        var shape = particles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Rectangle;
        shape.scale = new Vector3(extent * 2.4f, extent * 2.4f, 0.2f);

        var velocity = particles.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.x = new ParticleSystem.MinMaxCurve(-ambientParticleHorizontalDrift, ambientParticleHorizontalDrift);
        velocity.y = new ParticleSystem.MinMaxCurve(ambientParticleVerticalDrift * 0.4f, ambientParticleVerticalDrift);
        velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        var colorOverLifetime = particles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        float fadeInTime = Mathf.Clamp01(ambientParticleFadeInPortion);
        float fadeOutTime = Mathf.Clamp01(1f - ambientParticleFadeOutPortion);
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(1f, fadeInTime),
                new GradientAlphaKey(1f, fadeOutTime),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

        var sizeOverLifetime = particles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve(
            new Keyframe(0f, 0.45f),
            new Keyframe(0.22f, 1f),
            new Keyframe(0.78f, 0.92f),
            new Keyframe(1f, 0.2f));
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        var renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.sortingOrder = sortingOrderBase + 6;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sharedMaterial = GetAmbientParticleMaterial();

        particles.Play();
    }

    private void ClearExisting(Transform preservedChild = null)
    {
        movingLayers.Clear();
        if (backgroundRoot == null)
            return;

        for (int i = backgroundRoot.childCount - 1; i >= 0; i--)
        {
            Transform child = backgroundRoot.GetChild(i);
            if (child == preservedChild)
                continue;

            Destroy(child.gameObject);
        }
    }

    private void BindActionPointSystem(ActionPointSystem points)
    {
        if (actionPointSystem != null)
            actionPointSystem.ActionPointsChanged -= HandleActionPointsChanged;

        actionPointSystem = points;

        if (actionPointSystem != null)
            actionPointSystem.ActionPointsChanged += HandleActionPointsChanged;
    }

    private void HandleActionPointsChanged(int currentPoints, int maxPoints)
    {
        MapBackgroundPhase nextPhase = ResolveActionPointBackgroundPhase(currentPoints, maxPoints);
        if (nextPhase == currentBackgroundPhase)
            return;

        if (targetCamera == null || boardManager == null || boardManager.BoardSize <= 0 || backgroundRoot == null)
            return;

        if (backgroundTransitionRoutine != null)
            StopCoroutine(backgroundTransitionRoutine);

        RemoveTransitionOverlays();
        backgroundTransitionRoutine = StartCoroutine(TransitionToBackgroundPhase(nextPhase));
    }

    private IEnumerator TransitionToBackgroundPhase(MapBackgroundPhase nextPhase)
    {
        float extent = boardManager.BoardHalfExtentWorld + extentPadding;
        SpriteRenderer overlay = CreateTransitionOverlay(extent);
        yield return FadeTransitionOverlay(overlay, 0f, 1f, fadeToBlackDuration);

        currentBackgroundPhase = nextPhase;
        ClearExisting(overlay != null ? overlay.transform : null);
        BuildBackground();

        yield return FadeTransitionOverlay(overlay, 1f, 0f, fadeFromBlackDuration);
        if (overlay != null)
            Destroy(overlay.gameObject);

        backgroundTransitionRoutine = null;
    }

    private SpriteRenderer CreateTransitionOverlay(float extent)
    {
        float coverWorldSize = CalculateCameraCoverWorldSize(extent * 3.4f);
        return CreateLayer(
            "BackgroundTransitionBlackout",
            solidSprite,
            Vector3.zero,
            CalculateCoverScale(solidSprite, coverWorldSize),
            new Color(0f, 0f, 0f, 0f),
            sortingOrderBase + 30,
            0f,
            Vector2.zero);
    }

    private void RemoveTransitionOverlays()
    {
        if (backgroundRoot == null)
            return;

        for (int i = backgroundRoot.childCount - 1; i >= 0; i--)
        {
            Transform child = backgroundRoot.GetChild(i);
            if (child != null && child.name == "BackgroundTransitionBlackout")
                Destroy(child.gameObject);
        }
    }

    private IEnumerator FadeTransitionOverlay(SpriteRenderer overlay, float fromAlpha, float toAlpha, float duration)
    {
        if (overlay == null)
            yield break;

        if (duration <= 0f)
        {
            overlay.color = new Color(0f, 0f, 0f, toAlpha);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float normalized = Mathf.Clamp01(elapsed / duration);
            float alpha = Mathf.Lerp(fromAlpha, toAlpha, normalized);
            overlay.color = new Color(0f, 0f, 0f, alpha);
            yield return null;
        }

        overlay.color = new Color(0f, 0f, 0f, toAlpha);
    }

    private MapBackgroundPhase ResolveCurrentActionPointPhase()
    {
        if (actionPointSystem == null)
            return MapBackgroundPhase.Morning;

        return ResolveActionPointBackgroundPhase(actionPointSystem.CurrentPoints, actionPointSystem.CurrentMaxPoints);
    }

    private Sprite ResolveMapBackgroundSprite(MapBackgroundPhase phase)
    {
        switch (phase)
        {
            case MapBackgroundPhase.Morning:
                return ResolveSpriteWithFallback(morningBackgroundSprite, noonBackgroundSprite, duskBackgroundSprite);
            case MapBackgroundPhase.Noon:
                return ResolveSpriteWithFallback(noonBackgroundSprite, morningBackgroundSprite, duskBackgroundSprite);
            case MapBackgroundPhase.Dusk:
                return ResolveSpriteWithFallback(duskBackgroundSprite, noonBackgroundSprite, morningBackgroundSprite);
            case MapBackgroundPhase.Midnight:
                return ResolveSpriteWithFallback(midnightBackgroundSprite, duskBackgroundSprite, noonBackgroundSprite, morningBackgroundSprite);
            default:
                return null;
        }
    }

    private static Sprite ResolveSpriteWithFallback(params Sprite[] sprites)
    {
        if (sprites == null)
            return null;

        for (int i = 0; i < sprites.Length; i++)
        {
            if (sprites[i] != null)
                return sprites[i];
        }

        return null;
    }

    private Vector2 CalculateCoverScale(Sprite sprite, float targetWorldSize)
    {
        if (sprite == null || sprite.bounds.size.x <= 0.0001f || sprite.bounds.size.y <= 0.0001f)
            return Vector2.one * targetWorldSize;

        float targetWidth = targetWorldSize;
        float targetHeight = targetWorldSize;
        float spriteAspect = sprite.bounds.size.x / sprite.bounds.size.y;
        if (spriteAspect > 1f)
            targetWidth = targetHeight * spriteAspect;
        else
            targetHeight = targetWidth / Mathf.Max(0.0001f, spriteAspect);

        return new Vector2(
            targetWidth / sprite.bounds.size.x,
            targetHeight / sprite.bounds.size.y);
    }

    private float CalculateCameraCoverWorldSize(float fallbackWorldSize)
    {
        float coverSize = Mathf.Max(0.01f, fallbackWorldSize);
        if (targetCamera == null || !targetCamera.orthographic)
            return coverSize;

        return CalculateCameraCoverWorldSize(coverSize, initialCameraOrthographicSize, targetCamera.aspect, extentPadding);
    }

    private Sprite GetBlurredBackgroundSprite(Sprite source)
    {
        if (source == null)
            return null;

        if (backgroundImageBlurRadius <= 0 || backgroundImageBlurIterations <= 0)
            return source;

        if (blurredSpriteCache.TryGetValue(source, out Sprite cached) && cached != null)
            return cached;

        Sprite blurred = CreateBlurredSprite(source);
        blurredSpriteCache[source] = blurred != null ? blurred : source;
        return blurredSpriteCache[source];
    }

    private Sprite CreateBlurredSprite(Sprite source)
    {
        Texture sourceTexture = source != null ? source.texture : null;
        if (sourceTexture == null)
            return source;

        int maxSize = Mathf.Clamp(blurredTextureMaxSize, 64, 1024);
        float aspect = source.rect.height > 0.0001f ? source.rect.width / source.rect.height : 1f;
        int width;
        int height;
        if (aspect >= 1f)
        {
            width = maxSize;
            height = Mathf.Max(1, Mathf.RoundToInt(maxSize / Mathf.Max(0.0001f, aspect)));
        }
        else
        {
            height = maxSize;
            width = Mathf.Max(1, Mathf.RoundToInt(maxSize * aspect));
        }

        RenderTexture previous = RenderTexture.active;
        RenderTexture renderTexture = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
        Texture2D readableTexture = null;
        try
        {
            Graphics.Blit(sourceTexture, renderTexture);
            RenderTexture.active = renderTexture;
            readableTexture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = $"{source.name}_Blurred",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            readableTexture.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
            readableTexture.Apply(false, false);
        }
        finally
        {
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(renderTexture);
        }

        if (readableTexture == null)
            return source;

        int radius = Mathf.Clamp(backgroundImageBlurRadius, 1, 16);
        int iterations = Mathf.Clamp(backgroundImageBlurIterations, 1, 4);
        for (int i = 0; i < iterations; i++)
            ApplySeparableBoxBlur(readableTexture, radius);

        readableTexture.Apply(false, false);
        return Sprite.Create(
            readableTexture,
            new Rect(0f, 0f, readableTexture.width, readableTexture.height),
            new Vector2(0.5f, 0.5f),
            100f);
    }

    private static void ApplySeparableBoxBlur(Texture2D texture, int radius)
    {
        if (texture == null || radius <= 0)
            return;

        int width = texture.width;
        int height = texture.height;
        Color[] source = texture.GetPixels();
        Color[] horizontal = new Color[source.Length];
        Color[] blurred = new Color[source.Length];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color sum = Color.clear;
                int count = 0;
                for (int offset = -radius; offset <= radius; offset++)
                {
                    int sampleX = Mathf.Clamp(x + offset, 0, width - 1);
                    sum += source[y * width + sampleX];
                    count++;
                }

                horizontal[y * width + x] = sum / Mathf.Max(1, count);
            }
        }

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color sum = Color.clear;
                int count = 0;
                for (int offset = -radius; offset <= radius; offset++)
                {
                    int sampleY = Mathf.Clamp(y + offset, 0, height - 1);
                    sum += horizontal[sampleY * width + x];
                    count++;
                }

                blurred[y * width + x] = sum / Mathf.Max(1, count);
            }
        }

        texture.SetPixels(blurred);
    }

    private void EnsureRuntimeSprites()
    {
        if (verticalGradientSprite == null)
            verticalGradientSprite = CreateVerticalGradientSprite("MapVerticalGradient", 8, 256, topGradientColor, bottomGradientColor);
        if (radialGradientSprite == null)
            radialGradientSprite = CreateRadialGradientSprite("MapRadialGradient", 256);
        if (edgeVignetteSprite == null)
            edgeVignetteSprite = CreateEdgeVignetteSprite("MapEdgeVignette", 256);
        if (bandGradientSprite == null)
            bandGradientSprite = CreateBandGradientSprite("MapBandGradient", 256, 64);
        if (glowParticleSprite == null)
            glowParticleSprite = CreateGlowParticleSprite("MapGlowParticle", 128);
        if (solidSprite == null)
            solidSprite = CreateSolidSprite("MapSolidSprite");
    }

    private Sprite CreateVerticalGradientSprite(string textureName, int width, int height, Color topColor, Color bottomColor)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            name = textureName,
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        for (int y = 0; y < height; y++)
        {
            float t = y / Mathf.Max(1f, height - 1f);
            Color color = Color.Lerp(bottomColor, topColor, t);
            for (int x = 0; x < width; x++)
                texture.SetPixel(x, y, color);
        }

        texture.Apply(false, false);
        return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 32f);
    }

    private Material GetAmbientParticleMaterial()
    {
        if (ambientParticleMaterial != null)
            return ambientParticleMaterial;

        if (runtimeAmbientParticleMaterial != null)
            return runtimeAmbientParticleMaterial;

        Shader shader = Shader.Find("Particles/Additive");
        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");

        runtimeAmbientParticleMaterial = new Material(shader)
        {
            name = "RuntimeMapAmbientParticleMaterial"
        };

        if (glowParticleSprite != null && runtimeAmbientParticleMaterial.HasProperty("_MainTex"))
            runtimeAmbientParticleMaterial.SetTexture("_MainTex", glowParticleSprite.texture);

        if (runtimeAmbientParticleMaterial.HasProperty("_BaseMap") && glowParticleSprite != null)
            runtimeAmbientParticleMaterial.SetTexture("_BaseMap", glowParticleSprite.texture);

        if (runtimeAmbientParticleMaterial.HasProperty("_Color"))
            runtimeAmbientParticleMaterial.SetColor("_Color", Color.white);

        if (runtimeAmbientParticleMaterial.HasProperty("_BaseColor"))
            runtimeAmbientParticleMaterial.SetColor("_BaseColor", Color.white);

        return runtimeAmbientParticleMaterial;
    }

    private Sprite CreateRadialGradientSprite(string textureName, int size)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = textureName,
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = center.x;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center) / Mathf.Max(0.0001f, radius);
                float alpha = Mathf.Clamp01(1f - distance);
                alpha = alpha * alpha * (3f - 2f * alpha);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply(false, false);
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 32f);
    }

    private Sprite CreateEdgeVignetteSprite(string textureName, int size)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = textureName,
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = center.x;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center) / Mathf.Max(0.0001f, radius);
                float alpha = CalculateEdgeVignetteAlpha(distance);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply(false, false);
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 32f);
    }

    private Sprite CreateSolidSprite(string textureName)
    {
        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
        {
            name = textureName,
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Point
        };
        texture.SetPixel(0, 0, Color.white);
        texture.Apply(false, false);
        return Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
    }

    private Sprite CreateBandGradientSprite(string textureName, int width, int height)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            name = textureName,
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        for (int y = 0; y < height; y++)
        {
            float vertical = 1f - Mathf.Abs((y / Mathf.Max(1f, height - 1f)) * 2f - 1f);
            vertical = Mathf.Pow(Mathf.Clamp01(vertical), 2.4f);
            for (int x = 0; x < width; x++)
            {
                float horizontal = Mathf.Sin((x / Mathf.Max(1f, width - 1f)) * Mathf.PI);
                float alpha = vertical * Mathf.Clamp01(horizontal);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply(false, false);
        return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 32f);
    }

    private Sprite CreateGlowParticleSprite(string textureName, int size)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = textureName,
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = center.x;
        float coreRadius = Mathf.Clamp01(ambientParticleCoreRadius);
        float haloSoftness = Mathf.Max(0.05f, ambientParticleHaloSoftness);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float normalizedDistance = Vector2.Distance(new Vector2(x, y), center) / Mathf.Max(0.0001f, radius);
                float alpha;

                if (normalizedDistance <= coreRadius)
                {
                    alpha = 1f;
                }
                else
                {
                    float falloff = Mathf.Clamp01(1f - (normalizedDistance - coreRadius) / haloSoftness);
                    alpha = Mathf.Pow(falloff, 2.4f);
                }

                float coreBoost = normalizedDistance <= coreRadius * 0.65f ? 1f : 0.94f;
                texture.SetPixel(x, y, new Color(coreBoost, coreBoost, coreBoost, alpha));
            }
        }

        texture.Apply(false, false);
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 32f);
    }

    private sealed class LayerMotion
    {
        public Transform transform;
        public Vector3 basePosition;
        public float baseRotation;
        public Vector2 positionAmplitude;
        public float rotationAmplitude;
        public float speed;
        public float phase;
    }
}
