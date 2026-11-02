using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Presentation-only map resource changes. The authoritative run is never modified here.</summary>
[DisallowMultipleComponent]
public partial class MapResourceFeedback : MonoBehaviour
{
    private const int MaxCoins = 18;
    private const int MaxQueuedChanges = 4;
    private const float CoinFlightSeconds = 0.72f;
    private static readonly Color Gold = new Color32(240, 204, 107, 255);
    private static readonly Color Loss = new Color32(245, 111, 95, 255);
    private static readonly Color Healing = new Color32(123, 232, 172, 255);

    [Header("Optional sound overrides (otherwise use Resources/SFX/Map*)")]
    [SerializeField] private AudioClip coinReleaseSound;
    [SerializeField] private AudioClip coinArrivalSound;
    [SerializeField] private AudioClip coinSpendSound;
    [SerializeField] private AudioClip damageSound;
    [SerializeField] private AudioClip healSound;

    private sealed class MoneyChange
    {
        public int target;
        public Vector2 origin;
    }

    private sealed class HealthChange
    {
        public RunPartyMemberState member;
        public Image fill, trail;
        public TMP_Text label;
        public Color fillColor, textColor;
        public int target, maximum;
        public float fromHp, shownHp, fromRatio, ratio, trailFrom, age;
        public bool active, healing;
    }

    private RunState boundRun;
    private bool hasSnapshot;
    private TMP_Text wallet;
    private RectTransform walletIcon;
    private Color walletColor;
    private Vector3 walletScale;
    private int observedMoney;
    private int writtenMoney;
    private bool hasWrittenMoney;
    private double displayedMoney, moneyFrom;
    private float moneyAge, walletPulse, lossTint, soundCooldown;
    private MoneyChange activeMoney;
    private readonly List<MoneyChange> moneyQueue = new List<MoneyChange>();
    private readonly List<HealthChange> health = new List<HealthChange>();
    private AudioSource audioSource;

    public int DisplayedMoney => (int)Math.Round(displayedMoney);
    public bool IsMoneyAnimating => activeMoney != null || moneyQueue.Count > 0;

    public void Bind(RunState state, TMP_Text moneyLabel)
    {
        if (wallet != moneyLabel)
        {
            RestoreWallet();
            wallet = moneyLabel;
            walletIcon = wallet != null && wallet.transform.parent != null ? wallet.transform.parent.Find("MoneyIcon") as RectTransform : null;
            hasWrittenMoney = false;
            if (wallet != null) { walletColor = wallet.color; walletScale = wallet.rectTransform.localScale; }
        }
        if (hasSnapshot && ReferenceEquals(boundRun, state)) return;
        SnapAndClear();
        boundRun = state;
        observedMoney = state != null ? state.money : 0;
        displayedMoney = observedMoney;
        hasSnapshot = true;
        WriteMoney();
    }

    public void ObserveMoney(int value)
    {
        if (!hasSnapshot || value == observedMoney) return;
        observedMoney = value;
        Vector2 source = MapFeedbackOrigin.Current ?? new Vector2(0.58f, 0.52f);
        MoneyChange change = new MoneyChange { target = value, origin = source };
        // Bound both the particle count and backlog during repeated rapid actions.
        if (moneyQueue.Count >= MaxQueuedChanges) moneyQueue[moneyQueue.Count - 1] = change;
        else moneyQueue.Add(change);
        if (activeMoney == null) BeginNextMoneyChange();
    }

    public void PresentHealth(RunPartyMemberState member, Image fill, TMP_Text label, int current, int maximum)
    {
        if (fill == null) return;
        HealthChange change = health.Find(x => x.fill == fill);
        if (change == null)
        {
            change = new HealthChange { fill = fill, label = label, fillColor = fill.color, textColor = label != null ? label.color : Color.white };
            health.Add(change);
        }
        float ratio = current / (float)Mathf.Max(1, maximum);
        if (!ReferenceEquals(change.member, member) || change.maximum == 0)
        {
            change.member = member;
            change.target = current;
            change.maximum = maximum;
            change.shownHp = current;
            change.ratio = ratio;
            change.active = false;
            RenderHealth(change, 1f);
            return;
        }
        if (change.target == current && change.maximum == maximum) return;
        int delta = current - change.target;
        change.fromHp = change.shownHp;
        change.fromRatio = change.ratio;
        change.trailFrom = change.trail != null ? Mathf.Max(change.ratio, change.trail.rectTransform.anchorMax.x) : change.ratio;
        change.target = current;
        change.maximum = maximum;
        change.age = 0f;
        change.active = true;
        change.healing = delta > 0;
        EnsureHealthTrail(change);
        RenderHealth(change, 0f);
        if (delta != 0)
        {
            EnsureVisuals();
            Vector2 position = MapFeedbackOrigin.NormalizedPosition(fill.rectTransform, fill.rectTransform.rect.center);
            SpawnPopup(position, (delta > 0 ? "+" : string.Empty) + delta, delta > 0 ? Healing : Loss);
            FlashScreen(delta > 0, Mathf.Abs(delta) / (float)Mathf.Max(1, maximum));
            PlaySound(delta > 0 ? "Heal" : "Damage", 1f, 0.48f);
        }
    }

    private void BeginNextMoneyChange()
    {
        while (moneyQueue.Count > 0)
        {
            activeMoney = moneyQueue[0];
            moneyQueue.RemoveAt(0);
            moneyFrom = displayedMoney;
            moneyAge = 0f;
            long delta = (long)activeMoney.target - DisplayedMoney;
            if (delta == 0) { activeMoney = null; continue; }
            EnsureVisuals();
            if (delta > 0)
            {
                int count = Mathf.Clamp(Mathf.CeilToInt(Mathf.Sqrt(delta) * 1.8f), 1, MaxCoins);
                count = (int)Math.Min(count, delta);
                BeginCoins(count, delta, activeMoney.origin);
                SpawnPopup(activeMoney.origin, "+" + delta, Gold);
                PlaySound("CoinRelease", 1f, 0.38f);
            }
            else
            {
                lossTint = 0.7f;
                SpawnPopup(WalletPosition(), delta.ToString(), Loss);
                PlaySound("CoinSpend", 1f, 0.5f);
            }
            WriteMoney();
            return;
        }
    }

    private void Update() => Advance(Time.unscaledDeltaTime);

    /// <summary>Also used by deterministic visual captures; time is always unscaled.</summary>
    public void Advance(float seconds)
    {
        if (seconds <= 0f || float.IsNaN(seconds) || float.IsInfinity(seconds)) return;
        soundCooldown = Mathf.Max(0f, soundCooldown - seconds);
        walletPulse = Mathf.Max(0f, walletPulse - seconds * 5f);
        lossTint = Mathf.Max(0f, lossTint - seconds);
        if (activeMoney != null)
        {
            moneyAge += seconds;
            bool complete;
            if (activeMoney.target > moneyFrom) complete = AdvanceCoins();
            else
            {
                float t = Mathf.Clamp01(moneyAge / 0.48f);
                displayedMoney = moneyFrom + (activeMoney.target - moneyFrom) * Mathf.SmoothStep(0f, 1f, t);
                lossTint = Mathf.Max(lossTint, 0.22f);
                complete = t >= 1f;
            }
            if (complete)
            {
                displayedMoney = activeMoney.target;
                activeMoney = null;
                BeginNextMoneyChange();
            }
        }
        WriteMoney();
        for (int i = health.Count - 1; i >= 0; i--)
        {
            HealthChange change = health[i];
            if (change.fill == null) { health.RemoveAt(i); continue; }
            if (!change.active) continue;
            change.age += seconds;
            float t = Mathf.Clamp01(change.age / 0.48f), eased = Mathf.SmoothStep(0f, 1f, t);
            change.shownHp = Mathf.Lerp(change.fromHp, change.target, eased);
            change.ratio = Mathf.Lerp(change.fromRatio, change.target / (float)Mathf.Max(1, change.maximum), eased);
            RenderHealth(change, t);
            if (change.age >= 0.84f) { change.active = false; RenderHealth(change, 1f); }
        }
        AdvanceDecorations(seconds);
    }

    private void WriteMoney()
    {
        if (wallet == null) return;
        int value = DisplayedMoney;
        if (!hasWrittenMoney || value != writtenMoney)
        {
            wallet.text = value.ToString();
            writtenMoney = value;
            hasWrittenMoney = true;
        }
        Color receiving = Color.Lerp(walletColor, new Color(1f, 0.93f, 0.7f), walletPulse * 0.28f);
        wallet.color = Color.Lerp(receiving, Loss, Mathf.Clamp01(lossTint * 5f));
        wallet.rectTransform.localScale = walletScale * (1f + Mathf.Sin(walletPulse * Mathf.PI) * 0.065f);
    }

    private void RenderHealth(HealthChange change, float progress)
    {
        SetFill(change.fill, change.ratio);
        if (change.trail != null)
        {
            float delayed = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((change.age - 0.18f) / 0.66f));
            float trailRatio = change.active && !change.healing ? Mathf.Lerp(change.trailFrom, change.ratio, delayed) : change.ratio;
            SetFill(change.trail, trailRatio);
            change.trail.color = new Color(1f, 0.72f, 0.38f, change.active && !change.healing ? 0.72f : 0f);
        }
        float tint = change.active ? Mathf.Sin(progress * Mathf.PI) * (change.healing ? 0.92f : 0.55f) : 0f;
        change.fill.color = Color.Lerp(change.fillColor, change.healing ? Healing : Loss, tint);
        if (change.label != null)
        {
            change.label.text = $"{Mathf.Clamp(Mathf.RoundToInt(change.shownHp), 0, change.maximum)}/{change.maximum}";
            change.label.color = Color.Lerp(change.textColor, change.healing ? Healing : Loss, tint);
        }
    }

    private void EnsureHealthTrail(HealthChange change)
    {
        if (change.trail != null) return;
        Transform parent = change.fill.transform.parent;
        Transform existing = parent.Find("FeedbackTrail");
        change.trail = existing != null ? existing.GetComponent<Image>() : new GameObject("FeedbackTrail", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        change.trail.transform.SetParent(parent, false);
        change.trail.transform.SetSiblingIndex(change.fill.transform.GetSiblingIndex());
        change.trail.raycastTarget = false;
    }

    private static void SetFill(Image fill, float value)
    {
        RectTransform rect = fill.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = new Vector2(Mathf.Clamp01(value), 1f);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    private void PlaySound(string name, float pitch, float gain)
    {
        if (!Application.isPlaying || soundCooldown > 0f) return;
        float volume = Mathf.Clamp01(MainMenuSettingsPersistence.Current.sfxVolume);
        if (volume <= 0f) return;
        AudioClip clip = name == "CoinRelease" ? coinReleaseSound : name == "CoinArrival" ? coinArrivalSound
            : name == "CoinSpend" ? coinSpendSound : name == "Damage" ? damageSound : healSound;
        if (clip == null)
        {
            clip = Resources.Load<AudioClip>("SFX/Map" + name);
            if (name == "CoinRelease") coinReleaseSound = clip;
            else if (name == "CoinArrival") coinArrivalSound = clip;
            else if (name == "CoinSpend") coinSpendSound = clip;
            else if (name == "Damage") damageSound = clip;
            else healSound = clip;
        }
        if (clip == null) return;
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
            audioSource.ignoreListenerPause = true;
        }
        audioSource.pitch = pitch;
        audioSource.PlayOneShot(clip, volume * gain);
        soundCooldown = 0.055f;
    }

    private void RestoreWallet()
    {
        if (wallet == null) return;
        wallet.color = walletColor;
        wallet.rectTransform.localScale = walletScale;
    }

    private void SnapAndClear()
    {
        displayedMoney = observedMoney;
        activeMoney = null;
        moneyQueue.Clear();
        walletPulse = lossTint = 0f;
        RestoreWallet();
        foreach (HealthChange change in health)
        {
            if (change.fill == null) continue;
            change.shownHp = change.target;
            change.ratio = change.target / (float)Mathf.Max(1, change.maximum);
            change.active = false;
            RenderHealth(change, 1f);
        }
        health.Clear();
        ClearDecorations();
    }

    private void OnDisable()
    {
        SnapAndClear();
        WriteMoney();
        hasSnapshot = false;
        if (audioSource != null) audioSource.Stop();
        if (feedbackCanvas != null) feedbackCanvas.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        if (feedbackCanvas != null) feedbackCanvas.gameObject.SetActive(true);
    }
}
