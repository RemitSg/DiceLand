using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class BattleSettlementOverlay
{
    public enum RewardPresentationPhase { Hidden, Counting, Transferring, Complete }
    private sealed class ExperienceFlight
    {
        public Image image;
        public SummaryRowView row;
        public Vector2 source;
        public float delay, age, bend;
        public int credit;
        public bool active;
    }
    private sealed class ExperienceImpact
    {
        public RectTransform root;
        public MapFeedbackGraphic ring, spark;
        public float age = 1f;
    }
    private readonly List<ExperienceFlight> experienceFlights = new List<ExperienceFlight>();
    private readonly List<ExperienceImpact> experienceImpacts = new List<ExperienceImpact>();
    private BattleSettlementData presentationData;
    private float presentationAge, transferAge, walletAge;
    private int walletStage;
    private const float CounterSeconds = 0.7f, ExperienceFlightSeconds = 0.66f;
    public RewardPresentationPhase PresentationPhase { get; private set; }
    public bool IsPresenting => PresentationPhase == RewardPresentationPhase.Counting || PresentationPhase == RewardPresentationPhase.Transferring;
    public int ActiveExperienceFlights
    {
        get { int count = 0; foreach (ExperienceFlight flight in experienceFlights) if (flight.active) count++; return count; }
    }

    private void PreparePresentation(BattleSettlementData data)
    {
        presentationData = data;
        presentationAge = transferAge = 0;
        walletStage = 0;
        WriteRewardCounters(0);
        foreach (SummaryRowView row in rowViews) { row.displayedCredit = 0; row.creditedExperience = 0; }
        foreach (LootItemView view in lootViews) SetLootReveal(view, 0);
    }

    private void BeginPresentation()
    {
        if (presentationData == null) return;
        PresentationPhase = RewardPresentationPhase.Counting;
        presentationAge = 0;
    }

    /// <summary>Presentation only. Uses unscaled time in game and deterministic steps for visual verification.</summary>
    public void AdvancePresentation(float seconds)
    {
        if (!IsPresenting || seconds <= 0 || float.IsNaN(seconds) || float.IsInfinity(seconds)) return;
        float remaining = Mathf.Min(seconds, 10f);
        while (remaining > 0 && IsPresenting)
        {
            float step = Mathf.Min(remaining, 1f / 30f);
            remaining -= step;
            if (PresentationPhase == RewardPresentationPhase.Counting)
            {
                presentationAge += step;
                WriteRewardCounters(Mathf.SmoothStep(0, 1, Mathf.Clamp01(presentationAge / CounterSeconds)));
                if (presentationAge >= CounterSeconds) BeginTransfers();
            }
            else AdvanceTransfers(step);
        }
    }

    private void WriteRewardCounters(float progress)
    {
        if (presentationData == null) return;
        moneyRewardText.text = GameText.Format("battle.settlement.gold", "金币 +{amount}", ("amount", CountReward(presentationData.moneyGained, progress)));
        expText.text = GameText.Format("battle.settlement.xp", "经验值 +{amount}", ("amount", CountReward(presentationData.experienceGained, progress)));
        lootCountText.text = GameText.Format("battle.settlement.loot", "战利品 ×{count}", ("count", CountReward(lootViews.Count, progress)));
    }

    private static int CountReward(int value, float progress)
    {
        int safe = Mathf.Max(0, value);
        return progress >= 1 ? safe : (int)((double)safe * Mathf.Clamp01(progress));
    }

    private void BeginTransfers()
    {
        WriteRewardCounters(1);
        PresentationPhase = RewardPresentationPhase.Transferring;
        transferAge = 0;
        Canvas.ForceUpdateCanvases();
        int poolIndex = 0;
        int perCharacter = Mathf.Clamp(96 / Mathf.Max(1, rowViews.Count), 1, 8);
        Vector2 origin = FlightPosition(expText.rectTransform, expText.rectTransform.rect.center);
        for (int r = 0; r < rowViews.Count; r++)
        {
            SummaryRowView row = rowViews[r];
            int gain = Mathf.Max(0, row.entry.experienceGained);
            int count = Mathf.Min(gain, perCharacter);
            EnsureExperienceImpact(r);
            for (int i = 0; i < count; i++)
            {
                ExperienceFlight flight = GetExperienceFlight(poolIndex++);
                flight.row = row;
                flight.source = origin + new Vector2(Mathf.Sin(i * 2.4f + r) * 60f, Mathf.Cos(i * 1.8f + r) * 28f);
                flight.delay = i * 0.045f;
                flight.age = 0;
                flight.bend = (r % 2 == 0 ? -1 : 1) * (75 + i * 11);
                flight.credit = gain / count + (i < gain % count ? 1 : 0);
                flight.active = true;
                flight.image.gameObject.SetActive(false);
            }
        }
        if (presentationData.moneyGained > 0)
        {
            walletRoot.gameObject.SetActive(true);
            walletCaptionText.text = presentationData.hasMoneyBalance ? GameText.Get("battle.settlement.wallet", "持有")
                : GameText.Get("battle.settlement.earned", "本次获得");
            walletFeedback.gameObject.SetActive(true);
            walletFeedback.Bind(new RunState { money = presentationData.hasMoneyBalance ? presentationData.startingMoney : 0 }, walletAmountText);
            walletStage = 1; walletAge = 0;
            PositionWallet(0);
        }
        else walletStage = 4;
    }

    private void AdvanceTransfers(float seconds)
    {
        transferAge += seconds;
        bool experienceDone = true;
        foreach (ExperienceFlight flight in experienceFlights)
        {
            if (!flight.active) continue;
            experienceDone = false;
            flight.age += seconds;
            float elapsed = flight.age - flight.delay;
            if (elapsed < 0) continue;
            flight.image.gameObject.SetActive(true);
            float t = Mathf.Clamp01(elapsed / ExperienceFlightSeconds);
            Vector2 target = ExperienceDestination(flight.row);
            float eased = t * t;
            Vector2 position = Vector2.Lerp(flight.source, target, eased);
            position += new Vector2(Mathf.Sin(t * Mathf.PI) * flight.bend, Mathf.Sin(t * Mathf.PI) * 45f);
            RectTransform rect = flight.image.rectTransform;
            rect.anchoredPosition = position;
            rect.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(-32, 58, t) + flight.delay * 100);
            rect.localScale = Vector3.one * (Mathf.Lerp(1.1f, 0.48f, eased) + Mathf.Sin(t * 18) * 0.06f);
            flight.image.color = new Color(1, 1, 1, Mathf.Clamp01(elapsed * 12));
            if (t < 1) continue;
            flight.row.creditedExperience += flight.credit;
            flight.active = false;
            flight.image.gameObject.SetActive(false);
            int index = rowViews.IndexOf(flight.row);
            experienceImpacts[index].age = 0;
        }
        for (int i = 0; i < rowViews.Count; i++)
        {
            SummaryRowView row = rowViews[i];
            float gap = row.creditedExperience - row.displayedCredit;
            row.displayedCredit += gap * Mathf.Min(1, seconds * 18f);
            if (Mathf.Abs(gap) < 0.03f) row.displayedCredit = row.creditedExperience;
            if (row.displayedCredit < Mathf.Max(0, row.entry.experienceGained)) experienceDone = false;
            RenderExperienceCredit(row);
            AdvanceExperienceImpact(i, seconds);
        }
        bool lootDone = true;
        float stagger = Mathf.Clamp(1.1f / Mathf.Max(1, lootViews.Count), 0.055f, 0.18f);
        for (int i = 0; i < lootViews.Count; i++)
        {
            float reveal = Mathf.Clamp01((transferAge - 0.10f - i * stagger) / 0.18f);
            SetLootReveal(lootViews[i], reveal);
            if (reveal < 1) lootDone = false;
        }
        AdvanceWallet(seconds);
        if (experienceDone && lootDone && walletStage == 4 && transferAge > 0.4f) CompletePresentation();
    }

    private void RenderExperienceCredit(SummaryRowView row)
    {
        BattleSettlementEntry entry = row.entry;
        if (row.displayedCredit >= Mathf.Max(0, entry.experienceGained)) { ApplyResolvedSummaryState(row); return; }
        int level = Mathf.Max(1, entry.level), max = RequirementFor(row, level);
        double experience = Mathf.Max(0, entry.currentExperience) + (double)row.displayedCredit;
        // Each threshold was captured from the same level table that resolved the battle.
        int last = Mathf.Max(level, entry.resultingLevel);
        while (max > 0 && experience >= max && level < last)
        { experience -= max; level++; max = RequirementFor(row, level); }
        float fraction = entry.experienceGained > 0 ? row.displayedCredit / entry.experienceGained : 1;
        int hp = Mathf.RoundToInt(Mathf.Lerp(entry.currentHP, entry.resultingMaxHP > 0 ? entry.resultingHP : entry.currentHP, fraction));
        int maxHP = Mathf.RoundToInt(Mathf.Lerp(entry.maxHP, entry.resultingMaxHP > 0 ? entry.resultingMaxHP : entry.maxHP, fraction));
        UpdateSummaryRow(row, level, (int)System.Math.Min(int.MaxValue, experience), max, hp, maxHP);
        SetBarFill(row.expFillRect, row.expFillWidth, max > 0 ? (float)(experience / max) : 1);
    }

    private int RequirementFor(SummaryRowView row, int level)
    {
        BattleSettlementEntry entry = row.entry;
        int index = level - Mathf.Max(1, entry.level);
        if (entry.experienceRequirements != null && index >= 0 && index < entry.experienceRequirements.Count)
            return entry.experienceRequirements[index];
        if (entry.resultingLevel > 0 && level == entry.resultingLevel && entry.resultingMaxExperience > 0) return entry.resultingMaxExperience;
        if (level == entry.level) return entry.maxExperience;
        return ResolveExpMax(level, 100);
    }

    private void AdvanceWallet(float seconds)
    {
        if (walletStage == 4 || walletStage == 0) return;
        walletAge += seconds;
        if (walletStage == 1)
        {
            PositionWallet(Mathf.SmoothStep(0, 1, walletAge / 0.26f));
            if (walletAge < 0.26f) return;
            int initial = presentationData.hasMoneyBalance ? presentationData.startingMoney : 0;
            using (MapFeedbackOrigin.FromText(moneyRewardText, moneyRewardText.rectTransform))
                walletFeedback.ObserveMoney(unchecked(initial + Mathf.Max(0, presentationData.moneyGained)));
            walletStage = 2; walletAge = 0;
        }
        walletFeedback.Advance(seconds);
        if (walletStage == 2)
        {
            if (walletFeedback.IsMoneyAnimating) walletAge = 0;
            else if (walletAge > 0.2f) { walletStage = 3; walletAge = 0; }
        }
        if (walletStage == 3)
        {
            PositionWallet(1 - Mathf.SmoothStep(0, 1, walletAge / 0.26f));
            if (walletAge >= 0.26f) { walletStage = 4; walletRoot.gameObject.SetActive(false); walletFeedback.gameObject.SetActive(false); }
        }
    }

    private void PositionWallet(float visible)
    {
        walletRoot.anchoredPosition = new Vector2(-24, Mathf.Lerp(135, -14, Mathf.Clamp01(visible)));
    }

    private ExperienceFlight GetExperienceFlight(int index)
    {
        while (experienceFlights.Count <= index)
        {
            Image image = FSIcon(experienceFlightRoot, "ExperienceStar_" + experienceFlights.Count, ResourceKeywordText.ExperienceIcon, 0, 0, 38, 38);
            image.rectTransform.anchorMin = image.rectTransform.anchorMax = image.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            image.gameObject.SetActive(false);
            experienceFlights.Add(new ExperienceFlight { image = image });
        }
        return experienceFlights[index];
    }

    private Vector2 FlightPosition(RectTransform rect, Vector3 point)
    {
        Vector3 world = rect.TransformPoint(point);
        return experienceFlightRoot.InverseTransformPoint(world);
    }

    private Vector2 ExperienceDestination(SummaryRowView row)
    {
        RectTransform bar = row.expFillRect;
        Vector2 position = FlightPosition(bar, new Vector3(bar.rect.xMax, bar.rect.center.y));
        Rect viewport = partyScroll.viewport.rect;
        Vector2 min = FlightPosition(partyScroll.viewport, viewport.min + new Vector2(12, 12));
        Vector2 max = FlightPosition(partyScroll.viewport, viewport.max - new Vector2(12, 12));
        return new Vector2(Mathf.Clamp(position.x, min.x, max.x), Mathf.Clamp(position.y, min.y, max.y));
    }

    private void EnsureExperienceImpact(int index)
    {
        while (experienceImpacts.Count <= index)
        {
            RectTransform root = FSRect(experienceFlightRoot, "ExperienceImpact_" + experienceImpacts.Count);
            root.anchorMin = root.anchorMax = root.pivot = new Vector2(0.5f, 0.5f); root.sizeDelta = new Vector2(82, 62);
            RectTransform ringRect = FSRect(root, "Ring"); FSStretch(ringRect);
            MapFeedbackGraphic ring = ringRect.gameObject.AddComponent<MapFeedbackGraphic>(); ring.shape = MapFeedbackGraphic.Shape.Ring; ring.raycastTarget = false;
            RectTransform sparkRect = FSRect(root, "Spark"); FSStretch(sparkRect, 12, 8);
            MapFeedbackGraphic spark = sparkRect.gameObject.AddComponent<MapFeedbackGraphic>(); spark.shape = MapFeedbackGraphic.Shape.Spark; spark.raycastTarget = false;
            root.gameObject.SetActive(false);
            experienceImpacts.Add(new ExperienceImpact { root = root, ring = ring, spark = spark });
        }
    }

    private void AdvanceExperienceImpact(int index, float seconds)
    {
        ExperienceImpact impact = experienceImpacts[index];
        impact.age += seconds;
        float t = Mathf.Clamp01(impact.age / 0.30f);
        impact.root.gameObject.SetActive(t < 1);
        if (t >= 1) return;
        impact.root.anchoredPosition = ExperienceDestination(rowViews[index]);
        impact.root.localScale = Vector3.one * Mathf.Lerp(0.45f, 1.2f, t);
        impact.ring.color = new Color(SettlementExperience.r, SettlementExperience.g, SettlementExperience.b, (1 - t) * 0.7f);
        impact.spark.color = new Color(0.92f, 0.95f, 1, 1 - t);
    }

    private static void SetLootReveal(LootItemView view, float progress)
    {
        float eased = Mathf.SmoothStep(0, 1, progress);
        view.revealGroup.alpha = eased;
        view.revealGroup.transform.localScale = Vector3.one * Mathf.Lerp(0.9f, 1, eased);
        view.sealedLabel.SetActive(progress <= 0);
        view.button.interactable = progress >= 1;
    }

    public void CompletePresentation()
    {
        if (presentationData == null) return;
        WriteRewardCounters(1);
        foreach (SummaryRowView row in rowViews) ApplyResolvedSummaryState(row);
        foreach (LootItemView view in lootViews) SetLootReveal(view, 1);
        ClearPresentationEffects();
        PresentationPhase = RewardPresentationPhase.Complete;
    }

    private void CancelPresentation()
    {
        ClearPresentationEffects();
        presentationData = null;
        PresentationPhase = RewardPresentationPhase.Hidden;
    }

    private void ClearPresentationEffects()
    {
        foreach (ExperienceFlight flight in experienceFlights) { flight.active = false; if (flight.image != null) flight.image.gameObject.SetActive(false); }
        foreach (ExperienceImpact impact in experienceImpacts) { impact.age = 1; if (impact.root != null) impact.root.gameObject.SetActive(false); }
        if (walletRoot != null) walletRoot.gameObject.SetActive(false);
        if (walletFeedback != null) walletFeedback.gameObject.SetActive(false);
        walletStage = 4;
    }

    private static void DestroySettlementObject(Object value)
    {
        if (value == null) return;
        // Retire immediately from layout and raycasts while Unity defers destruction in play mode.
        if (value is GameObject go) go.SetActive(false);
        if (Application.isPlaying) Destroy(value); else DestroyImmediate(value);
    }
}
