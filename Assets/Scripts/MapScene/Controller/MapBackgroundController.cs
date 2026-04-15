using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class MapBackgroundController : MonoBehaviour
{
    [Header("Roots")]
    [SerializeField] private Transform backgroundRoot;

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
    private Sprite bandGradientSprite;
    private Sprite glowParticleSprite;
    private Material runtimeAmbientParticleMaterial;

    public void Initialize(Transform root, Camera mapCamera, BoardManager board)
    {
        targetCamera = mapCamera;
        boardManager = board;

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

        EnsureRuntimeSprites();
        ClearExisting();
        BuildBackground();
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

    private void BuildBackground()
    {
        float extent = boardManager.BoardHalfExtentWorld + extentPadding;

        CreateLayer(
            "BackdropGradient",
            verticalGradientSprite,
            Vector3.zero,
            Vector2.one * extent * 3f,
            Color.white,
            sortingOrderBase,
            0f,
            Vector2.zero);

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
            radialGradientSprite,
            Vector3.zero,
            new Vector2(extent * 3.4f, extent * 3.4f),
            vignetteColor,
            sortingOrderBase + 5,
            0f,
            Vector2.zero);

        if (enableAmbientParticles)
            CreateAmbientParticles(extent);
    }

    private void CreateLayer(
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

    private void ClearExisting()
    {
        movingLayers.Clear();
        if (backgroundRoot == null)
            return;

        for (int i = backgroundRoot.childCount - 1; i >= 0; i--)
            Destroy(backgroundRoot.GetChild(i).gameObject);
    }

    private void EnsureRuntimeSprites()
    {
        if (verticalGradientSprite == null)
            verticalGradientSprite = CreateVerticalGradientSprite("MapVerticalGradient", 8, 256, topGradientColor, bottomGradientColor);
        if (radialGradientSprite == null)
            radialGradientSprite = CreateRadialGradientSprite("MapRadialGradient", 256);
        if (bandGradientSprite == null)
            bandGradientSprite = CreateBandGradientSprite("MapBandGradient", 256, 64);
        if (glowParticleSprite == null)
            glowParticleSprite = CreateGlowParticleSprite("MapGlowParticle", 128);
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
