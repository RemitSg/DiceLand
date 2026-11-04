using UnityEngine;
using UnityEngine.EventSystems;

public partial class MapInventoryDetailView
{
    private bool closing;
    private float openProgress, switchProgress = 1, displayAge;
    private GameObject previousSelection;

    private void BeginPresentation(bool changed)
    {
        bool wasVisible = IsVisible;
        closing = false;
        if (modalRoot != null)
        {
            if (!wasVisible && EventSystem.current != null) previousSelection = EventSystem.current.currentSelectedGameObject;
            modalRoot.gameObject.SetActive(true);
            modalRoot.SetAsLastSibling();
            panelGroup.interactable = true; panelGroup.blocksRaycasts = true;
            if (!wasVisible) openProgress = Application.isPlaying ? 0 : 1;
            if (changed) { switchProgress = Application.isPlaying ? 0 : 1; displayAge = 0; }
            RefreshDescriptionLayout(changed);
            ApplyViewport(new Vector2(Screen.width, Screen.height), Screen.safeArea);
        }
        gameObject.SetActive(true);
    }

    private void LateUpdate()
    {
        if (modalRoot == null || !IsVisible) return;
        Vector2 viewport = new Vector2(Screen.width, Screen.height);
        if (viewport != layoutViewport || Screen.safeArea != layoutSafeArea || modalRoot.rect.size != layoutCanvasSize)
            ApplyViewport(viewport, Screen.safeArea);
        float delta = Time.unscaledDeltaTime;
        openProgress = Mathf.MoveTowards(openProgress, closing ? 0 : 1, delta / (closing ? 0.16f : 0.24f));
        switchProgress = Mathf.MoveTowards(switchProgress, 1, delta / 0.2f);
        displayAge += delta;
        ApplyDetailMotion();
        if (closing && openProgress <= 0) HideImmediately();
    }

    private void ApplyDetailMotion()
    {
        if (detailRect == null) return;
        float ease = 1 - Mathf.Pow(1 - openProgress, 3);
        panelGroup.alpha = ease;
        detailRect.localScale = Vector3.one * (detailScale * Mathf.Lerp(0.955f, 1, ease));
        detailRect.anchoredPosition = Vector2.down * ((1 - ease) * 20 * detailScale);
        detailBackdrop.color = new Color(0.004f, 0.012f, 0.008f, ease * 0.60f);
        float selection = 1 - switchProgress;
        // Only the art breathes; its frame, quantity and text remain still.
        if (iconImage != null)
        {
            iconImage.rectTransform.anchoredPosition = new Vector2(220, -226 - selection * 10 + Mathf.Sin(displayAge * 1.3f) * 2);
            iconImage.rectTransform.localScale = Vector3.one * (1 - selection * 0.035f);
            iconImage.color = new Color(1, 1, 1, 1 - selection * 0.45f);
        }
        if (displayEngraving != null)
        {
            float glow = 0.08f + Mathf.Sin(displayAge * 1.3f) * 0.04f;
            if (Mathf.Abs(displayEngraving.glow - glow) > 0.005f) { displayEngraving.glow = glow; displayEngraving.SetVerticesDirty(); }
        }
    }

    public void HideImmediately()
    {
        bool visible = IsVisible;
        closing = false; openProgress = 0; displayedItem = null;
        SetRelicAdditionalDescription(string.Empty);
        ConfigureActionButton(string.Empty, false, null, string.Empty);
        if (descriptionScroll != null) descriptionScroll.StopMovement();
        if (modalRoot != null) modalRoot.gameObject.SetActive(false); else gameObject.SetActive(false);
        if (previousSelection != null && previousSelection.activeInHierarchy && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(previousSelection);
        previousSelection = null;
        if (visible) Hidden?.Invoke();
    }
}
