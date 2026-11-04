using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public partial class GameMusicPlayer : MonoBehaviour
{
    private const string PlayerObjectName = "GameMusicPlayer";
    private const string DefaultConfigResourcePath = "GameMusicConfig";

    private static GameMusicPlayer instance;

    [Header("Playback")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField, Min(0f)] private float defaultStopFadeDuration = 0.35f;

    private Coroutine fadeOutRoutine;
    private float fadeVolumeMultiplier = 1f;

    public static GameMusicPlayer Instance
    {
        get
        {
            if (instance == null)
                instance = FindObjectOfType<GameMusicPlayer>();

            return instance;
        }
    }

    public static GameMusicPlayer GetOrCreate()
    {
        GameMusicPlayer existing = Instance;
        if (existing != null)
            return existing;

        GameObject root = new GameObject(PlayerObjectName);
        return root.AddComponent<GameMusicPlayer>();
    }

    public static GameMusicConfig_SO ResolveConfig(GameMusicConfig_SO configuredConfig)
    {
        return configuredConfig != null
            ? configuredConfig
            : Resources.Load<GameMusicConfig_SO>(DefaultConfigResourcePath);
    }

    public static void PlayMapMusic(GameMusicConfig_SO configuredConfig)
    {
        GameMusicConfig_SO config = ResolveConfig(configuredConfig);
        if (config == null)
            return;

        GetOrCreate().Play(GameMusicResolver.ResolveMapMusic(config));
    }

    public static void PlayBattleMusic(GameMusicConfig_SO configuredConfig, BattleEncounterData encounter)
    {
        GameMusicConfig_SO config = ResolveConfig(configuredConfig);
        AudioClip clip = GameMusicResolver.ResolveBattleMusic(config, encounter);
        GetOrCreate().Play(clip);
    }

    public static void FadeOutAndStop()
    {
        GameMusicPlayer player = Instance;
        if (player == null)
            return;

        player.FadeOutAndStopInstance(player.defaultStopFadeDuration);
    }

    public static void FadeOutAndStop(float duration)
    {
        GameMusicPlayer player = Instance;
        if (player == null)
            return;

        player.FadeOutAndStopInstance(duration);
    }

    public void Play(AudioClip clip)
    {
        EnsureComponents();
        CancelFadeOut();
        ApplyMusicVolume();

        if (clip == null)
        {
            musicSource.Stop();
            musicSource.clip = null;
            return;
        }

        musicSource.loop = true;
        if (musicSource.clip == clip)
        {
            if (!musicSource.isPlaying)
                musicSource.Play();
            return;
        }

        musicSource.clip = clip;
        musicSource.Play();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureComponents();
    }

    private void Update()
    {
        if (musicSource == null)
            return;

        musicSource.pitch = 1f;
        UpdateDeathAudio(Time.unscaledDeltaTime);
        ApplyMusicVolume();
    }

    private void EnsureComponents()
    {
        if (musicSource == null)
            musicSource = GetComponent<AudioSource>();
        if (musicSource == null)
            musicSource = gameObject.AddComponent<AudioSource>();

        musicSource.playOnAwake = false;
        musicSource.loop = true;
        musicSource.pitch = 1f;

        // Older live players may still carry the former slow-motion filter.
        AudioLowPassFilter legacyFilter = GetComponent<AudioLowPassFilter>();
        if (legacyFilter != null) legacyFilter.enabled = false;
    }

    private void ApplyMusicVolume()
    {
        if (musicSource == null)
            return;

        MainMenuSettingsData settings = MainMenuSettingsPersistence.Current;
        float settingsVolume = settings != null ? Mathf.Clamp01(settings.musicVolume) : 1f;
        musicSource.volume = settingsVolume * Mathf.Clamp01(fadeVolumeMultiplier) * deathMusicMultiplier;
    }

    private void FadeOutAndStopInstance(float duration)
    {
        EnsureComponents();

        if (fadeOutRoutine != null)
            StopCoroutine(fadeOutRoutine);

        if (duration <= 0f || musicSource.clip == null)
        {
            StopImmediately();
            return;
        }

        fadeOutRoutine = StartCoroutine(FadeOutAndStopRoutine(duration));
    }

    private IEnumerator FadeOutAndStopRoutine(float duration)
    {
        float startMultiplier = Mathf.Clamp01(fadeVolumeMultiplier);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float normalized = Mathf.Clamp01(elapsed / duration);
            fadeVolumeMultiplier = Mathf.Lerp(startMultiplier, 0f, normalized);
            ApplyMusicVolume();
            yield return null;
        }

        StopImmediately();
        fadeOutRoutine = null;
    }

    private void StopImmediately()
    {
        if (musicSource != null)
        {
            musicSource.Stop();
            musicSource.clip = null;
        }

        fadeVolumeMultiplier = 1f;
        ApplyMusicVolume();
    }

    private void CancelFadeOut()
    {
        if (fadeOutRoutine != null)
        {
            StopCoroutine(fadeOutRoutine);
            fadeOutRoutine = null;
        }

        fadeVolumeMultiplier = 1f;
    }
}
