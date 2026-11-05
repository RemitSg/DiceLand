using System.Collections.Generic;
using UnityEngine;

public partial class GameMusicPlayer
{
    private const string DeathImpactResourcePath = "SFX/BattleDeathImpact";
    private const float DeathMusicDuckMultiplier = 0.2f;

    [Header("Death Audio")]
    [SerializeField] private AudioClip deathImpactSound;
    [SerializeField, Min(0.01f)] private float deathMusicFadeDuration = 0.4f;
    [SerializeField, Range(0f, 1f)] private float deathImpactGain = 0.95f;

    private readonly HashSet<Object> deathSlowMotionOwners = new HashSet<Object>();
    private AudioSource deathAudioSource;
    private float deathMusicMultiplier = 1f;
    private float deathSoundRemaining;
    private int lastDeathSoundFrame = -1;
    private bool deathAudioActive;

    public static void PlayDeathAudio(Object slowMotionOwner = null)
    {
        GetOrCreate().PlayDeathAudioInstance(slowMotionOwner);
    }

    public static void EndDeathSlowMotion(Object owner)
    {
        GameMusicPlayer player = Instance;
        if (player != null) player.deathSlowMotionOwners.Remove(owner);
    }

    private void PlayDeathAudioInstance(Object slowMotionOwner)
    {
        EnsureComponents();
        if (slowMotionOwner != null) deathSlowMotionOwners.Add(slowMotionOwner);
        deathAudioActive = true;

        // A shared voice lets simultaneous deaths keep their tail without stacking bass.
        if (lastDeathSoundFrame == Time.frameCount || deathSoundRemaining > 0f ||
            (deathAudioSource != null && deathAudioSource.isPlaying)) return;

        if (deathImpactSound == null) deathImpactSound = Resources.Load<AudioClip>(DeathImpactResourcePath);
        if (deathImpactSound == null) return;
        if (deathAudioSource == null)
        {
            GameObject voice = new GameObject("DeathImpactAudio");
            voice.transform.SetParent(transform, false);
            deathAudioSource = voice.AddComponent<AudioSource>();
            deathAudioSource.playOnAwake = false;
            deathAudioSource.loop = false;
            deathAudioSource.spatialBlend = 0f;
            deathAudioSource.ignoreListenerPause = true;
            deathAudioSource.priority = 32;
        }

        deathAudioSource.pitch = 1f;
        deathAudioSource.clip = deathImpactSound;
        ApplyDeathSoundVolume();
        deathAudioSource.Play();
        deathSoundRemaining = deathImpactSound.length;
        lastDeathSoundFrame = Time.frameCount;
    }

    private void UpdateDeathAudio(float unscaledDeltaTime)
    {
        if (!deathAudioActive) return;
        deathSlowMotionOwners.RemoveWhere(owner => owner == null);
        deathSoundRemaining = Mathf.Max(0f, deathSoundRemaining - unscaledDeltaTime);
        ApplyDeathSoundVolume();
        bool holding = deathSlowMotionOwners.Count > 0 || deathSoundRemaining > 0f ||
            (deathAudioSource != null && deathAudioSource.isPlaying);
        float fadeStep = (1f - DeathMusicDuckMultiplier) * Mathf.Max(0f, unscaledDeltaTime)
            / Mathf.Max(0.01f, deathMusicFadeDuration);
        deathMusicMultiplier = Mathf.MoveTowards(deathMusicMultiplier, holding ? DeathMusicDuckMultiplier : 1f, fadeStep);
        if (!holding && deathMusicMultiplier >= 1f) deathAudioActive = false;
    }

    private void ApplyDeathSoundVolume()
    {
        if (deathAudioSource == null) return;
        MainMenuSettingsData settings = MainMenuSettingsPersistence.Current;
        float volume = settings != null ? Mathf.Clamp01(settings.sfxVolume) : 1f;
        deathAudioSource.volume = volume * Mathf.Clamp01(deathImpactGain);
    }
}
