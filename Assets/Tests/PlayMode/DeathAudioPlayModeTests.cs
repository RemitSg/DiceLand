#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public sealed class DeathAudioPlayModeTests
{
    private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic;
    private GameObject host;
    private Component player;
    private Type playerType;
    private Object previousPlayer;
    private AudioSource music;
    private AudioClip musicClip, impact;
    private object settings;
    private float originalTimeScale, originalListenerVolume, originalMusicVolume, originalSfxVolume;
    private bool originalListenerPause;

    [SetUp]
    public void SetUp()
    {
        originalTimeScale = Time.timeScale;
        originalListenerVolume = AudioListener.volume;
        originalListenerPause = AudioListener.pause;
        Time.timeScale = 1f;
        AudioListener.volume = 0f;
        AudioListener.pause = false;
        settings = RuntimeType("MainMenuSettingsPersistence").GetProperty("Current").GetValue(null);
        originalMusicVolume = (float)settings.GetType().GetField("musicVolume").GetValue(settings);
        originalSfxVolume = (float)settings.GetType().GetField("sfxVolume").GetValue(settings);
        SetSetting("musicVolume", 0.8f);
        SetSetting("sfxVolume", 0.6f);
        playerType = RuntimeType("GameMusicPlayer");
        FieldInfo singleton = playerType.GetField("instance", BindingFlags.Static | BindingFlags.NonPublic);
        previousPlayer = (Object)singleton.GetValue(null);
        singleton.SetValue(null, null);
        host = new GameObject("Death audio fixture");
        player = host.AddComponent(playerType);
        if (Object.FindObjectOfType<AudioListener>() == null) host.AddComponent<AudioListener>();
        musicClip = AudioClip.Create("Continuing test music", 48000 * 8, 1, 48000, false);
        impact = AudioClip.Create("Short test death impact", 31200, 1, 48000, false);
        playerType.GetField("deathImpactSound", Fields).SetValue(player, impact);
        playerType.GetMethod("Play").Invoke(player, new object[] { musicClip });
        music = (AudioSource)playerType.GetField("musicSource", Fields).GetValue(player);
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        Time.timeScale = originalTimeScale;
        AudioListener.volume = originalListenerVolume;
        AudioListener.pause = originalListenerPause;
        SetSetting("musicVolume", originalMusicVolume);
        SetSetting("sfxVolume", originalSfxVolume);
        Object.Destroy(host);
        Object.Destroy(musicClip);
        Object.Destroy(impact);
        yield return null;
        playerType.GetField("instance", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, previousPlayer);
    }

    [UnityTest]
    public IEnumerator SlowMotionDucksToTwentyPercentWithoutStoppingOrPitchingTheMusic()
    {
        Time.timeScale = 0.09f;
        int before = music.timeSamples;
        Begin(host);
        yield return new WaitForSecondsRealtime(0.48f);
        Assert.IsTrue(music.isPlaying);
        Assert.AreSame(musicClip, music.clip);
        Assert.AreEqual(1f, music.pitch);
        Assert.AreEqual(0.16f, music.volume, 0.01f);
        Assert.Greater(music.timeSamples - before, 48000 * 0.25f);
        Assert.AreEqual(1f, Voice().pitch);
    }

    [UnityTest]
    public IEnumerator ShortSlowMotionStillWaitsForTheWholeDeathSound()
    {
        Begin(host);
        yield return new WaitForSecondsRealtime(0.1f);
        End(host);
        yield return new WaitForSecondsRealtime(0.4f);
        Assert.AreEqual(0.16f, music.volume, 0.01f);
        Assert.IsTrue(Voice().isPlaying);
        yield return new WaitForSecondsRealtime(0.68f);
        Assert.AreEqual(0.8f, music.volume, 0.01f);
        Assert.IsTrue(music.isPlaying);
        Assert.AreSame(musicClip, music.clip);
    }

    [UnityTest]
    public IEnumerator FinishedSoundWaitsForEveryActiveSlowMotionOwner()
    {
        GameObject second = new GameObject("Second slow-motion owner");
        second.transform.SetParent(host.transform, false);
        Begin(host);
        Begin(second);
        yield return new WaitForSecondsRealtime(0.8f);
        Assert.IsFalse(Voice().isPlaying);
        Assert.AreEqual(0.16f, music.volume, 0.01f);
        End(host);
        yield return new WaitForSecondsRealtime(0.12f);
        Assert.AreEqual(0.16f, music.volume, 0.01f);
        End(second);
        yield return new WaitForSecondsRealtime(0.48f);
        Assert.AreEqual(0.8f, music.volume, 0.01f);
    }

    [UnityTest]
    public IEnumerator RepeatedDeathsKeepOneVoiceAndPreserveThePlayingTail()
    {
        Begin(host);
        yield return new WaitForSecondsRealtime(0.17f);
        AudioSource voice = Voice();
        int before = voice.timeSamples;
        Begin(host);
        Begin(host);
        Assert.GreaterOrEqual(voice.timeSamples, before);
        Assert.AreEqual(1, host.GetComponentsInChildren<AudioSource>().Count(source => source.clip == impact));
        End(host);
        yield return new WaitForSecondsRealtime(1f);
        Assert.AreEqual(0.8f, music.volume, 0.01f);
    }

    [UnityTest]
    public IEnumerator PausedTimeDoesNotStallFadesAndUserVolumeChangesAreRespected()
    {
        Time.timeScale = 0f;
        Begin(host);
        yield return new WaitForSecondsRealtime(0.48f);
        Assert.AreEqual(0.16f, music.volume, 0.01f);
        SetSetting("musicVolume", 0.25f);
        SetSetting("sfxVolume", 0f);
        yield return null;
        Assert.AreEqual(0.05f, music.volume, 0.01f);
        Assert.AreEqual(0f, Voice().volume);
        End(host);
        yield return new WaitForSecondsRealtime(0.7f);
        Assert.AreEqual(0.25f, music.volume, 0.01f);
        Assert.AreEqual(1f, music.pitch);
    }

    [UnityTest]
    public IEnumerator ExplicitStopsAreNotUndoneByDeathRecovery()
    {
        Begin(host);
        yield return new WaitForSecondsRealtime(0.12f);
        playerType.GetMethod("FadeOutAndStop", new[] { typeof(float) }).Invoke(null, new object[] { 0.1f });
        yield return new WaitForSecondsRealtime(0.2f);
        Assert.IsNull(music.clip);
        Assert.IsFalse(music.isPlaying);
        End(host);
        yield return new WaitForSecondsRealtime(1f);
        Assert.IsNull(music.clip);
        Assert.IsFalse(music.isPlaying);
    }

    [UnityTest]
    public IEnumerator PackagedImpactLoadsAsStereoAndPlaysAtItsOriginalPitch()
    {
        AudioClip packaged = Resources.Load<AudioClip>("SFX/BattleDeathImpact");
        Assert.IsNotNull(packaged);
        Assert.AreEqual(48000, packaged.frequency);
        Assert.AreEqual(2, packaged.channels);
        Assert.AreEqual(2.4f, packaged.length, 0.002f);
        playerType.GetField("deathImpactSound", Fields).SetValue(player, packaged);
        Begin(null);
        yield return null;
        Assert.AreSame(packaged, Voice().clip);
        Assert.AreEqual(1f, Voice().pitch);
        Assert.AreEqual(0.6f * 0.95f, Voice().volume, 0.001f);
    }

    private AudioSource Voice() => (AudioSource)playerType.GetField("deathAudioSource", Fields).GetValue(player);
    private void Begin(Object owner) => playerType.GetMethod("PlayDeathAudio").Invoke(null, new object[] { owner });
    private void End(Object owner) => playerType.GetMethod("EndDeathSlowMotion").Invoke(null, new object[] { owner });
    private void SetSetting(string field, float value) => settings.GetType().GetField(field).SetValue(settings, value);
    private static Type RuntimeType(string name) => AppDomain.CurrentDomain.GetAssemblies().Select(assembly => assembly.GetType(name)).First(type => type != null);
}
#endif
