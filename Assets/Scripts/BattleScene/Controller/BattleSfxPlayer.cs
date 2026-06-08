using UnityEngine;

[DisallowMultipleComponent]
public class BattleSfxPlayer : MonoBehaviour
{
    [Header("SFX Interfaces")]
    [SerializeField] private AudioClip clashAnimationSfx;
    [SerializeField] private AudioClip damageTakenSfx;

    [Header("Playback")]
    [SerializeField] private AudioSource sfxSource;

    private int lastDamageTakenSfxFrame = -1;

    public void PlayClashAnimation()
    {
        PlayOneShot(clashAnimationSfx);
    }

    public void PlayDamageTakenOncePerFrame()
    {
        int currentFrame = Time.frameCount;
        if (lastDamageTakenSfxFrame == currentFrame)
            return;

        lastDamageTakenSfxFrame = currentFrame;
        PlayOneShot(damageTakenSfx);
    }

    private void Awake()
    {
        EnsureAudioSource();
    }

    private void PlayOneShot(AudioClip clip)
    {
        if (clip == null)
            return;

        EnsureAudioSource();
        if (sfxSource == null)
            return;

        sfxSource.PlayOneShot(clip, ResolveSfxVolume());
    }

    private void EnsureAudioSource()
    {
        if (sfxSource == null)
            sfxSource = GetComponent<AudioSource>();
        if (sfxSource == null)
            sfxSource = gameObject.AddComponent<AudioSource>();

        sfxSource.playOnAwake = false;
        sfxSource.loop = false;
        sfxSource.spatialBlend = 0f;
    }

    private static float ResolveSfxVolume()
    {
        MainMenuSettingsData settings = MainMenuSettingsPersistence.Current;
        return settings != null ? Mathf.Clamp01(settings.sfxVolume) : 1f;
    }
}
