using System;
using System.Runtime.InteropServices;
using UnityEngine;

public enum GameHapticCue
{
    CoinArrived,
    Clash,
    EnemyDefeated,
    AllyDefeated
}

public readonly struct GameHapticPulse
{
    public readonly int attackMilliseconds;
    public readonly int tailMilliseconds;
    public readonly float intensity;
    public readonly float minimumInterval;
    public readonly int priority;

    public int DurationMilliseconds => attackMilliseconds + tailMilliseconds;

    public GameHapticPulse(int attack, int tail, float strength, float interval, int importance)
    {
        attackMilliseconds = attack;
        tailMilliseconds = tail;
        intensity = strength;
        minimumInterval = interval;
        priority = importance;
    }
}

/// <summary>Uses real time so death slow motion does not stretch feedback or its cooldown.</summary>
public sealed class GameHapticGate
{
    private double nextAllowedTime = double.NegativeInfinity;
    private int lastFrame = -1;
    private int lastPriority = -1;

    public bool TryAccept(GameHapticPulse pulse, double now, int frame)
    {
        if (double.IsNaN(now) || double.IsInfinity(now)) return false;
        // A death may interrupt a clash; coins must never interrupt a death.
        if ((frame == lastFrame || now < nextAllowedTime) && pulse.priority <= lastPriority)
            return false;

        lastFrame = frame;
        lastPriority = pulse.priority;
        nextAllowedTime = now + Math.Max(pulse.minimumInterval, pulse.DurationMilliseconds / 1000d + 0.025d);
        return true;
    }

    public void Reset()
    {
        nextAllowedTime = double.NegativeInfinity;
        lastFrame = -1;
        lastPriority = -1;
    }
}

/// <summary>Presentation-only mobile feedback. Unsupported platforms are silent.</summary>
public static class GameHaptics
{
    private static readonly GameHapticGate gate = new GameHapticGate();
    private static bool nativeFailed;

#if UNITY_EDITOR
    // Tests observe the actual presentation callbacks without driving physical hardware.
    internal static Action<GameHapticCue, GameHapticPulse> outputOverride;
    internal static Action cancelOverride;
#endif

#if UNITY_ANDROID && !UNITY_EDITOR
    private static AndroidJavaObject vibrator;
    private static AndroidJavaObject audioAttributes;
    private static bool androidInitialized;
    private static bool amplitudeControl;
    private static int androidApi;
#elif UNITY_IOS && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void DiceLand_PlayHaptic(int cue, float intensity);
#endif

    public static GameHapticPulse GetPulse(GameHapticCue cue)
    {
        switch (cue)
        {
            case GameHapticCue.CoinArrived: return new GameHapticPulse(9, 0, 0.22f, 0.07f, 0);
            case GameHapticCue.Clash: return new GameHapticPulse(12, 14, 0.52f, 0.09f, 1);
            case GameHapticCue.EnemyDefeated: return new GameHapticPulse(20, 45, 0.78f, 0.14f, 2);
            case GameHapticCue.AllyDefeated: return new GameHapticPulse(30, 85, 0.95f, 0.22f, 3);
            default: throw new ArgumentOutOfRangeException(nameof(cue), cue, null);
        }
    }

    public static void Play(GameHapticCue cue)
    {
        if (!Application.isPlaying || !MainMenuSettingsPersistence.Current.vibrationEnabled) return;
#if UNITY_EDITOR
        if (outputOverride == null) return;
#else
        if (!Application.isMobilePlatform || !Application.isFocused || nativeFailed) return;
#endif
        GameHapticPulse pulse = GetPulse(cue);
        if (!gate.TryAccept(pulse, Time.realtimeSinceStartupAsDouble, Time.frameCount)) return;
#if UNITY_EDITOR
        outputOverride(cue, pulse);
#else
        try
        {
#if UNITY_ANDROID
            PlayAndroid(pulse);
#elif UNITY_IOS
            DiceLand_PlayHaptic((int)cue, pulse.intensity);
#endif
        }
        catch (Exception exception)
        {
            nativeFailed = true;
            Debug.LogWarning("[GameHaptics] Device feedback unavailable: " + exception.Message);
        }
#endif
    }

    public static void ApplySettings(bool enabled)
    {
        if (!enabled) Stop();
    }

    public static void Stop()
    {
        gate.Reset();
#if UNITY_EDITOR
        cancelOverride?.Invoke();
#elif UNITY_ANDROID
        if (vibrator == null) return;
        try { vibrator.Call("cancel"); }
        catch (Exception) { nativeFailed = true; }
#endif
        // UIKit impact events have no delayed tail or queued continuation to cancel.
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRuntime()
    {
        Stop();
        nativeFailed = false;
#if UNITY_EDITOR
        outputOverride = null;
        cancelOverride = null;
#elif UNITY_ANDROID
        vibrator?.Dispose();
        audioAttributes?.Dispose();
        vibrator = null;
        audioAttributes = null;
        androidInitialized = false;
#endif
        Application.focusChanged -= HandleFocusChanged;
        Application.focusChanged += HandleFocusChanged;
        Application.quitting -= Stop;
        Application.quitting += Stop;
    }

    private static void HandleFocusChanged(bool focused)
    {
        if (!focused) Stop();
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private static void PlayAndroid(GameHapticPulse pulse)
    {
        if (!androidInitialized)
        {
            androidInitialized = true;
            using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
                androidApi = version.GetStatic<int>("SDK_INT");
            using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");

            if (vibrator == null || !vibrator.Call<bool>("hasVibrator"))
            {
                vibrator?.Dispose();
                vibrator = null;
                return;
            }
            amplitudeControl = androidApi >= 26 && vibrator.Call<bool>("hasAmplitudeControl");
            using (var builder = new AndroidJavaObject("android.media.AudioAttributes$Builder"))
            {
                // Sonification uses the system's touch-feedback policy rather than alarm usage.
                using (builder.Call<AndroidJavaObject>("setUsage", 13)) { }
                using (builder.Call<AndroidJavaObject>("setContentType", 4)) { }
                audioAttributes = builder.Call<AndroidJavaObject>("build");
            }
        }
        if (vibrator == null) return;
        if (androidApi < 26)
        {
            vibrator.Call("vibrate", (long)pulse.DurationMilliseconds);
            return;
        }

        int peak = amplitudeControl ? Mathf.Clamp(Mathf.RoundToInt(pulse.intensity * 255f), 1, 255) : -1;
        using (var effects = new AndroidJavaClass("android.os.VibrationEffect"))
        {
            // A defined attack and softer tail give combat weight without a long phone buzz.
            long[] timings = pulse.tailMilliseconds > 0
                ? new long[] { pulse.attackMilliseconds, pulse.tailMilliseconds }
                : new long[] { pulse.attackMilliseconds };
            int[] amplitudes = pulse.tailMilliseconds > 0
                ? new[] { peak, amplitudeControl ? Mathf.Max(1, Mathf.RoundToInt(peak * 0.4f)) : -1 }
                : new[] { peak };
            using (var effect = effects.CallStatic<AndroidJavaObject>("createWaveform", timings, amplitudes, -1))
                vibrator.Call("vibrate", effect, audioAttributes);
        }
    }
#endif
}
