using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>Records the production contact effects with battle sprites and lighting in a temporary scene.</summary>
public static class BattleContactVfxVisualTools
{
    public const string OutputPath = "outputs/agent-battle/contact-fallout-20261105";
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private const int Width = 960, Height = 540, Fps = 60, Frames = 144, ContactFrame = 18;

    [MenuItem("Tools/Battle Visuals/Capture Contact Effects")]
    public static void CaptureBoth()
    {
        Capture(true);
        Capture(false);
    }

    public static string Capture(bool clash)
    {
        if (Application.isPlaying)
            throw new InvalidOperationException("Contact capture needs Edit Mode.");
        string clip = clash ? "clash" : "hit";
        string folder = Path.GetFullPath(Path.Combine(OutputPath, clip));
        Directory.CreateDirectory(folder);
        Action<Camera> record = camera => Record(camera, folder, clash);
        typeof(BattleLookPreviewTools).GetMethod("CapturePreview", BindingFlags.Static | BindingFlags.NonPublic)
            .Invoke(null, new object[] { clip + "-contact-stage.png", false, -1, Width, Height, record });
        return folder;
    }

    private static void Record(Camera camera, string folder, bool clash)
    {
        GameObject[] roots = camera.gameObject.scene.GetRootGameObjects();
        BattleManager manager = roots.SelectMany(root => root.GetComponentsInChildren<BattleManager>(true)).First();
        BattlePresentationSystem presentation = roots.SelectMany(root => root.GetComponentsInChildren<BattlePresentationSystem>(true)).First();
        Transform ally = manager.alliesParent.transform.GetChild(0);
        Transform enemy = manager.enemiesParent.transform.GetChild(0);
        foreach (Transform actor in manager.alliesParent.transform)
            actor.gameObject.SetActive(actor == ally);
        foreach (Transform actor in manager.enemiesParent.transform)
            actor.gameObject.SetActive(actor == enemy);
        foreach (Canvas canvas in roots.SelectMany(root => root.GetComponentsInChildren<Canvas>(true)))
            canvas.gameObject.SetActive(false);
        foreach (SpeedButton button in roots.SelectMany(root => root.GetComponentsInChildren<SpeedButton>(true)))
            button.gameObject.SetActive(false);

        float gap = BattlePresentationSystem.CalculateClashVisualSeparation(ally, enemy, Vector3.right,
            Read<float>(presentation, "clashFaceGap"), Read<float>(presentation, "clashMinimumGap"));
        Vector3 allyContact = new Vector3(-gap * 0.5f, ally.position.y, 0);
        Vector3 enemyContact = new Vector3(gap * 0.5f, enemy.position.y, 0);
        ally.position = allyContact;
        enemy.position = enemyContact;
        BattleActorVisualUtility.TryGetActorSpriteBounds(ally, out Bounds allyBounds);
        BattleActorVisualUtility.TryGetActorSpriteBounds(enemy, out Bounds enemyBounds);
        Vector3 target = (allyBounds.center + enemyBounds.center) * 0.5f;
        target.x += clash ? 0 : 0.55f;
        float distance = Mathf.Max(2.5f, allyBounds.size.y, enemyBounds.size.y) * 3.0f;
        Vector3 cameraOffset = new Vector3(0, distance * 0.12f, -distance);
        camera.transform.position = target + cameraOffset;
        camera.transform.LookAt(target);
        const float baseLens = 40;
        camera.fieldOfView = baseLens;
        camera.aspect = (float)Width / Height;
        camera.GetUniversalAdditionalCameraData().cameraStack.Clear();
        foreach (BattleLookController look in roots.SelectMany(root => root.GetComponentsInChildren<BattleLookController>(true)))
            look.PrepareCamera(camera);

        var contactShadows = roots.SelectMany(root => root.GetComponentsInChildren<BattleContactShadowController>(true)).ToArray();
        var sunShadows = roots.SelectMany(root => root.GetComponentsInChildren<BattleActorSunShadowController>(true)).ToArray();
        AnimationCurve move = Read<AnimationCurve>(presentation, "moveCurve");
        float approach = Read<float>(presentation, "advanceDuration");
        float recoilDistance = Read<float>(presentation, "defenderKnockbackDistance");
        float recoilOut = Read<float>(presentation, "defenderKnockbackOutDuration");
        float recoilBack = Read<float>(presentation, "defenderKnockbackReturnDuration");
        BattleContactVfx effect = null;
        int clipped = 0;
        int groundedTailFrames = 0;
        float followedLens = baseLens;
        float maxLens = baseLens;
        Vector3 followedTarget = target;
        Bounds actorBounds = allyBounds;
        actorBounds.Encapsulate(enemyBounds);
        var buffer = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
        var pixels = new Texture2D(Width, Height, TextureFormat.RGB24, false);
        RenderTexture previous = RenderTexture.active;
        RenderTexture oldTarget = camera.targetTexture;
        try
        {
            buffer.Create();
            camera.targetTexture = buffer;
            for (int frame = 0; frame < Frames; frame++)
            {
                float time = (frame - ContactFrame) / (float)Fps;
                float ready = move.Evaluate(Mathf.InverseLerp(-approach, 0, time));
                ally.position = allyContact + Vector3.left * (1 - ready) * (clash ? 1.05f : 1.40f);
                enemy.position = enemyContact + Vector3.right * (clash ? (1 - ready) * 0.80f : 0);
                if (frame == ContactFrame)
                    effect = SpawnContact(presentation, ally, enemy, clash);
                else if (effect != null)
                    effect.Advance(1f / Fps);
                Bounds shotBounds = actorBounds;
                if (!clash && time >= 0)
                {
                    float outAmount = move.Evaluate(Mathf.InverseLerp(0, recoilOut, time));
                    float backAmount = 1 - move.Evaluate(Mathf.InverseLerp(recoilOut, recoilOut + recoilBack, time));
                    Vector3 recoil = Vector3.right * recoilDistance * outAmount * backAmount;
                    enemy.position += recoil;
                    Bounds recoiledEnemy = enemyBounds;
                    recoiledEnemy.center += recoil;
                    shotBounds.Encapsulate(recoiledEnemy);
                }
                bool visibleBurst = effect != null && effect.HasVisibleGeometry;
                if (visibleBurst)
                    shotBounds.Encapsulate(effect.VisualBounds);
                Vector3 centerShift = shotBounds.center - actorBounds.center;
                centerShift.z = 0;
                followedTarget = Vector3.Lerp(followedTarget, target + centerShift, 0.19f);
                camera.transform.position = followedTarget + cameraOffset;
                camera.transform.LookAt(followedTarget);
                camera.fieldOfView = baseLens;
                float fitLens = visibleBurst ? BattleContactVfx.LensThatFits(camera, shotBounds) : baseLens;
                followedLens = Mathf.Max(fitLens, Mathf.Lerp(followedLens, baseLens, 0.12f));
                camera.fieldOfView = followedLens;
                maxLens = Mathf.Max(maxLens, followedLens);
                if (visibleBurst)
                {
                    if (IsClipped(camera, effect.VisualBounds))
                        clipped++;
                    if (!effect.HasFallingBurst)
                        groundedTailFrames++;
                }
                foreach (BattleContactShadowController shadows in contactShadows)
                    shadows.UpdateShadows();
                foreach (BattleActorSunShadowController shadows in sunShadows)
                    shadows.UpdateShadows();
                camera.Render();
                RenderTexture.active = buffer;
                pixels.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
                pixels.Apply();
                File.WriteAllBytes(Path.Combine(folder, frame.ToString("D3") + ".png"), pixels.EncodeToPNG());
            }
            File.WriteAllText(Path.Combine(folder, "capture.json"), JsonUtility.ToJson(new Report
            {
                frames = Frames, fps = Fps, width = Width, height = Height, clippedFrames = clipped,
                particles = effect.ParticleCount, groundedParticles = effect.GroundedParticleCount,
                groundedTailFrames = groundedTailFrames, maximumFov = maxLens, effectComplete = effect.IsComplete
            }, true));
        }
        finally
        {
            camera.targetTexture = oldTarget;
            RenderTexture.active = previous;
            UnityEngine.Object.DestroyImmediate(pixels);
            buffer.Release();
            UnityEngine.Object.DestroyImmediate(buffer);
        }
    }

    private static BattleContactVfx SpawnContact(BattlePresentationSystem presentation, Transform ally, Transform enemy, bool clash)
    {
        Vector3 from = BattleActorVisualUtility.GetActorCenterWorld(ally);
        Vector3 to = BattleActorVisualUtility.GetActorCenterWorld(enemy);
        object effect;
        if (clash)
        {
            Vector3 point = BattlePresentationSystem.ResolveClashContactPoint(ally, enemy, (from + to) * 0.5f,
                Vector3.right, Read<float>(presentation, "impactHeightOffset"));
            effect = typeof(BattlePresentationSystem).GetMethod("SpawnClashContactFx", Private)
                .Invoke(presentation, new object[] { ally, enemy, point, Vector3.right, 9f });
        }
        else
        {
            Vector3 point = (Vector3)typeof(BattlePresentationSystem).GetMethod("ImpactPoint", Private)
                .Invoke(presentation, new object[] { from, to, 0f });
            effect = typeof(BattlePresentationSystem).GetMethod("SpawnHitContactFx", Private)
                .Invoke(presentation, new object[] { enemy, point, (to - from).normalized, Read<Color>(presentation, "allyHitColor"), 9f, false });
        }
        return effect as BattleContactVfx ?? throw new InvalidOperationException("The contact shader is unavailable.");
    }

    private static bool IsClipped(Camera camera, Bounds bounds)
    {
        for (int corner = 0; corner < 8; corner++)
        {
            Vector3 point = bounds.center + Vector3.Scale(bounds.extents,
                new Vector3((corner & 1) == 0 ? -1 : 1, (corner & 2) == 0 ? -1 : 1, (corner & 4) == 0 ? -1 : 1));
            Vector3 view = camera.WorldToViewportPoint(point);
            if (view.z <= camera.nearClipPlane || view.x < 0 || view.x > 1 || view.y < 0 || view.y > 1)
                return true;
        }
        return false;
    }

    private static T Read<T>(BattlePresentationSystem presentation, string field)
    {
        return (T)typeof(BattlePresentationSystem).GetField(field, Private).GetValue(presentation);
    }

    [Serializable]
    private sealed class Report
    {
        public int frames, fps, width, height, particles, groundedParticles, groundedTailFrames, clippedFrames;
        public float maximumFov;
        public bool effectComplete;
    }
}
