using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Composes after camera-follow coroutines and before world HUD placement. The base pose is restored before the next update.</summary>
[ExecuteAlways, DefaultExecutionOrder(-250)]
public sealed class BattleDeathCameraMotion : MonoBehaviour
{
    public bool IsActive { get; private set; }
    public Bounds FramingBounds => envelope;
    public const float RecoveryDuration = 0.32f;
    private readonly List<BattleDeathVfx> effects = new List<BattleDeathVfx>();
    private Func<Camera> cameraProvider;
    private Camera appliedCamera;
    private Vector3 basePosition, center, direction;
    private Quaternion baseRotation;
    private float baseLens, age, kickAge, recovery, lastDelta, strength = 1;
    private Bounds envelope;
    private bool hasEnvelope, applied, wasOrthographic, releasing;

    public void Configure(Func<Camera> provider, float intensity = 1)
    {
        cameraProvider = provider; strength = Mathf.Clamp(intensity, 0.25f, 1.5f);
    }

    public void Play(BattleDeathVfx effect)
    {
        if (effect == null || effect.IsComplete || cameraProvider == null || cameraProvider() == null) return;
        RestoreBasePose();
        if (effects.Contains(effect)) return;
        if (!IsActive)
        {
            hasEnvelope = false; age = kickAge = 0; center = effect.VisualBounds.center;
        }
        else if (kickAge > 0.18f) kickAge = 0;
        direction = effect.TravelDirection;
        effects.Add(effect); IsActive = true; releasing = false; recovery = 0;
        GatherBounds();
    }

    private void Update()
    {
        if (!Application.isPlaying) return;
        RestoreBasePose();
        Advance(Mathf.Min(Time.unscaledDeltaTime, 0.05f) * BattleDeathVfx.VisualTimeScale(Time.timeScale));
    }

    private void LateUpdate() { if (Application.isPlaying) ApplyPose(); }

    public void Advance(float delta)
    {
        if (!IsActive) return;
        lastDelta = Mathf.Max(0, delta); age += lastDelta; kickAge += lastDelta;
        GatherBounds();
        if (effects.Count == 0)
        {
            releasing = true; recovery += lastDelta;
            if (recovery >= RecoveryDuration) { Cancel(); return; }
        }
        center = Vector3.Lerp(center, envelope.center, 1 - Mathf.Exp(-14 * lastDelta));
    }

    private void GatherBounds()
    {
        for (int i = effects.Count - 1; i >= 0; i--)
        {
            BattleDeathVfx effect = effects[i];
            if (effect == null || effect.IsComplete) { effects.RemoveAt(i); continue; }
            if (!effect.HasVisibleGeometry) continue;
            Bounds visible = effect.VisualBounds;
            visible.Expand(effect.WorldHeight * 0.14f);
            if (!hasEnvelope) { envelope = visible; hasEnvelope = true; }
            else envelope.Encapsulate(visible);
        }
    }

    public void ApplyPose()
    {
        RestoreBasePose();
        if (!IsActive || !hasEnvelope) return;
        Camera camera = cameraProvider != null ? cameraProvider() : null;
        if (camera == null || !camera.isActiveAndEnabled) { Cancel(); return; }
        GatherBounds();
        basePosition = camera.transform.position; baseRotation = camera.transform.rotation;
        wasOrthographic = camera.orthographic; baseLens = wasOrthographic ? camera.orthographicSize : camera.fieldOfView;
        appliedCamera = camera;
        float release = releasing ? 1 - Mathf.SmoothStep(0, 1, recovery / RecoveryDuration) : 1;
        float tracking = Mathf.SmoothStep(0, 1, Mathf.Clamp01(age / 0.22f));
        float kick = Mathf.Sin(Mathf.Clamp01(kickAge / 0.28f) * Mathf.PI) * strength;
        float size = Mathf.Max(0.5f, envelope.size.y);
        Vector3 forward = baseRotation * Vector3.forward;
        Vector3 right = baseRotation * Vector3.right, up = baseRotation * Vector3.up;
        Vector3 focusDelta = center - basePosition;
        Vector3 pan = right * Vector3.Dot(focusDelta, right) + up * Vector3.Dot(focusDelta, up);
        Vector3 position = basePosition + pan * tracking + forward * size * 0.035f * kick + direction * size * 0.035f * kick;
        Quaternion rotation = baseRotation * Quaternion.Euler(0, 0, -Mathf.Sign(direction.x) * 1.15f * kick);
        float lens = baseLens * (1 - 0.13f * kick);
        // Keep the fitted pose during release, then blend it back. Dropping the fit here would snap the lens/dolly on the first recovery frame.
        if (wasOrthographic) lens = Mathf.Max(lens, RequiredOrthoSize(position, rotation, camera.aspect));
        else
        {
            float maximumFov = Mathf.Max(65, baseLens);
            float backwards = RequiredDolly(position, rotation, camera.aspect, maximumFov, camera.nearClipPlane);
            position -= forward * backwards;
            lens = Mathf.Max(lens, RequiredFov(position, rotation, camera.aspect));
        }
        camera.transform.SetPositionAndRotation(Vector3.Lerp(basePosition, position, release), Quaternion.Slerp(baseRotation, rotation, release));
        lens = Mathf.Lerp(baseLens, lens, release);
        if (wasOrthographic) camera.orthographicSize = lens; else camera.fieldOfView = Mathf.Clamp(lens, 1, 120);
        applied = true;
    }

    private Vector3 Corner(int index) => envelope.center + Vector3.Scale(envelope.extents,
        new Vector3((index & 1) == 0 ? -1 : 1, (index & 2) == 0 ? -1 : 1, (index & 4) == 0 ? -1 : 1));

    private float RequiredOrthoSize(Vector3 position, Quaternion rotation, float aspect)
    {
        Quaternion inverse = Quaternion.Inverse(rotation); float size = 0.1f;
        for (int i = 0; i < 8; i++)
        {
            Vector3 local = inverse * (Corner(i) - position);
            size = Mathf.Max(size, Mathf.Max(Mathf.Abs(local.y) / 0.84f, Mathf.Abs(local.x) / (Mathf.Max(0.1f, aspect) * 0.84f)));
        }
        return size;
    }

    private float RequiredDolly(Vector3 position, Quaternion rotation, float aspect, float fov, float near)
    {
        Quaternion inverse = Quaternion.Inverse(rotation); float backwards = 0;
        float tangent = Mathf.Tan(fov * Mathf.Deg2Rad * 0.5f) * 0.84f;
        for (int i = 0; i < 8; i++)
        {
            Vector3 local = inverse * (Corner(i) - position);
            float depth = Mathf.Max(Mathf.Abs(local.y), Mathf.Abs(local.x) / Mathf.Max(0.1f, aspect)) / tangent;
            backwards = Mathf.Max(backwards, Mathf.Max(near + 0.1f, depth) - local.z);
        }
        return backwards;
    }

    private float RequiredFov(Vector3 position, Quaternion rotation, float aspect)
    {
        Quaternion inverse = Quaternion.Inverse(rotation); float tangent = 0;
        for (int i = 0; i < 8; i++)
        {
            Vector3 local = inverse * (Corner(i) - position);
            tangent = Mathf.Max(tangent, Mathf.Max(Mathf.Abs(local.y), Mathf.Abs(local.x) / Mathf.Max(0.1f, aspect)) / (Mathf.Max(0.01f, local.z) * 0.84f));
        }
        return 2 * Mathf.Atan(tangent) * Mathf.Rad2Deg;
    }

    public void RestoreBasePose()
    {
        if (!applied) return;
        if (appliedCamera != null)
        {
            appliedCamera.transform.SetPositionAndRotation(basePosition, baseRotation);
            if (wasOrthographic) appliedCamera.orthographicSize = baseLens; else appliedCamera.fieldOfView = baseLens;
        }
        applied = false; appliedCamera = null;
    }

    public void Cancel()
    {
        RestoreBasePose(); effects.Clear(); IsActive = false; hasEnvelope = false; releasing = false; recovery = 0;
    }

    public static BattleDeathCameraMotion FindActive(Scene scene)
    {
        foreach (BattleDeathCameraMotion motion in FindObjectsOfType<BattleDeathCameraMotion>())
            if (motion.gameObject.scene == scene && motion.IsActive) return motion;
        return null;
    }

    private void OnDisable() { Cancel(); }
    private void OnDestroy() { Cancel(); }
}
