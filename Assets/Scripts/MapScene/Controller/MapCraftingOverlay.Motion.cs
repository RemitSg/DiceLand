using UnityEngine;
using UnityEngine.UI;

public partial class MapCraftingOverlay
{
    private bool closing, forgeBurst;
    private float openProgress, selectionTime, feedbackTime;

    private void LateUpdate()
    {
        if (!IsVisible) return;
        Vector2 viewport = new Vector2(Screen.width, Screen.height);
        if (viewport != lastViewport || Screen.safeArea != lastSafeArea) ApplyViewport(viewport, Screen.safeArea);
        float delta = Time.unscaledDeltaTime;
        openProgress = Mathf.MoveTowards(openProgress, closing ? 0 : 1, delta / (closing ? 0.16f : 0.24f));
        selectionTime = Mathf.Max(0, selectionTime - delta);
        if (feedbackTime > 0)
        {
            feedbackTime = Mathf.Max(0, feedbackTime - delta);
            if (feedbackTime <= 0) RefreshAvailability();
        }
        ApplyMotion();
        if (closing && openProgress <= 0) FinishHide();
    }

    private void ApplyMotion()
    {
        if (canvas == null) return;
        float ease = 1 - Mathf.Pow(1 - openProgress, 3);
        panelGroup.alpha = ease;
        panelRect.localScale = Vector3.one * (layoutScale * Mathf.Lerp(0.955f, 1, ease));
        panelRect.anchoredPosition = restingPosition + Vector2.down * ((1 - ease) * 22 * layoutScale);
        backdrop.color = new Color(0.006f, 0.014f, 0.010f, 0.62f * ease);
        float selection = Mathf.Clamp01(selectionTime / 0.24f);
        previewGroup.alpha = 1 - selection * 0.30f;
        float burst = forgeBurst ? Mathf.Clamp01(feedbackTime / 0.72f) : 0;
        iconImage.rectTransform.anchoredPosition = weaponRestingPosition + Vector2.down * (selection * 14);
        iconImage.rectTransform.localScale = Vector3.one * (1 + Mathf.Sin(burst * Mathf.PI) * 0.035f - selection * 0.025f);
        float light = Mathf.Sin(Time.unscaledTime * 1.2f) * 0.025f + 0.04f + Mathf.Sin(burst * Mathf.PI) * 0.8f;
        if (Mathf.Abs(engraving.glow - light) > 0.005f) { engraving.glow = light; engraving.SetVerticesDirty(); }
        for (int i = 0; i < sparks.Length; i++)
        {
            float age = 1 - burst;
            float angle = (i * 137.508f + 20) * Mathf.Deg2Rad;
            float travel = (28 + age * (90 + i * 7));
            Vector2 center = weaponRestingPosition;
            sparks[i].anchoredPosition = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * travel + Vector2.up * age * 46;
            sparks[i].sizeDelta = new Vector2(3 + i % 3, 6 + i % 4);
            sparks[i].localEulerAngles = new Vector3(0, 0, angle * Mathf.Rad2Deg - 90);
            sparks[i].GetComponent<Image>().color = new Color(1, 0.81f + (i % 3) * 0.04f, 0.4f, burst > 0 ? Mathf.Sin(age * Mathf.PI) * 0.85f : 0);
        }
    }
}
