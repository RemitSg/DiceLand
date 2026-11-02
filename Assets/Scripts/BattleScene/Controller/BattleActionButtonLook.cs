using UnityEngine;
using UnityEngine.UI;

/// <summary>Owns the ready button's artwork and safe-area layout, leaving its input handlers intact.</summary>
[DisallowMultipleComponent]
public sealed class BattleActionButtonLook : MonoBehaviour
{
    private Button button;
    private BattleButton battleOwner;
    private BattleButtonSpriteStateSwitcher switcher;
    private Image image;
    private RectTransform rect, parentRect, canvasRect;
    private Sprite idle, pressed, originalIdle, originalPressed, originalImage;
    private Vector2 originalAnchorMin, originalAnchorMax, originalPivot, originalSize, originalPosition;
    private Rect previousSafeArea;
    private Vector2 previousScreen, previousCanvasSize;
    private bool configured, applied;
    private CanvasGroup visibilityGroup;
    private bool ownsVisibilityGroup, suppressed;
    private float previousAlpha;
    private bool previousBlocksRaycasts, previousInteractable;

    public static void Apply(BattleButton owner)
    {
        if (owner == null || owner.GetComponent<Button>() == null ||
            owner.GetComponent<BattleButtonSpriteStateSwitcher>() == null)
            return;
        Sprite normal = Resources.Load<Sprite>("BattleUI/ReadyButtonGold_Unpressed");
        Sprite down = Resources.Load<Sprite>("BattleUI/ReadyButtonGold_Pressed");
        if (normal == null || down == null) return;
        BattleActionButtonLook look = owner.GetComponent<BattleActionButtonLook>();
        if (look == null) look = owner.gameObject.AddComponent<BattleActionButtonLook>();
        look.Configure(owner.GetComponent<Button>(), normal, down, owner);
    }

    public void Configure(Button target, Sprite normal, Sprite down, BattleButton battle = null)
    {
        if (configured && button == target && idle == normal && pressed == down && battleOwner == battle)
        {
            RefreshLayout();
            return;
        }
        ReleaseLook();
        if (target == null || normal == null || down == null) return;
        button = target;
        battleOwner = battle;
        switcher = target.GetComponent<BattleButtonSpriteStateSwitcher>();
        rect = target.transform as RectTransform;
        parentRect = rect != null ? rect.parent as RectTransform : null;
        Canvas canvas = target.GetComponentInParent<Canvas>();
        canvasRect = canvas != null ? canvas.rootCanvas.transform as RectTransform : null;
        image = switcher != null && switcher.targetImage != null ? switcher.targetImage : target.GetComponent<Image>();
        if (switcher == null || rect == null || parentRect == null || canvasRect == null || image == null)
            return;

        idle = normal;
        pressed = down;
        originalIdle = switcher.unpressedSprite;
        originalPressed = switcher.pressedSprite;
        originalImage = image.sprite;
        originalAnchorMin = rect.anchorMin;
        originalAnchorMax = rect.anchorMax;
        originalPivot = rect.pivot;
        originalSize = rect.sizeDelta;
        originalPosition = rect.anchoredPosition;
        configured = true;
        if (isActiveAndEnabled) ApplySkin();
    }

    private void OnEnable()
    {
        if (configured) ApplySkin();
    }

    private void ApplySkin()
    {
        if (switcher == null || image == null) return;
        switcher.unpressedSprite = idle;
        switcher.pressedSprite = pressed;
        image.sprite = idle;
        if (visibilityGroup == null)
        {
            visibilityGroup = button.GetComponent<CanvasGroup>();
            if (visibilityGroup == null)
            {
                visibilityGroup = button.gameObject.AddComponent<CanvasGroup>();
                ownsVisibilityGroup = true;
            }
        }
        applied = true;
        SetResolvingPresentation(battleOwner != null && battleOwner.IsResolvingCombat);
        RefreshLayout();
    }

    private void LateUpdate()
    {
        if (!applied || canvasRect == null) return;
        SetResolvingPresentation(battleOwner != null && battleOwner.IsResolvingCombat);
        Vector2 screen = new Vector2(Screen.width, Screen.height);
        if (previousSafeArea != Screen.safeArea || previousScreen != screen || previousCanvasSize != canvasRect.rect.size)
            RefreshLayout();
    }

    public void RefreshLayout() => ApplyLayout(Screen.safeArea, new Vector2(Screen.width, Screen.height));

    public void ApplyLayout(Rect safeArea, Vector2 screenSize)
    {
        if (!applied || rect == null || parentRect == null || canvasRect == null ||
            screenSize.x <= 0 || screenSize.y <= 0 || canvasRect.rect.width <= 0 || canvasRect.rect.height <= 0)
            return;
        Vector2 min = ToParent(new Vector2(Mathf.Clamp(safeArea.xMin, 0, screenSize.x),
            Mathf.Clamp(safeArea.yMin, 0, screenSize.y)), screenSize);
        Vector2 max = ToParent(new Vector2(Mathf.Clamp(safeArea.xMax, 0, screenSize.x),
            Mathf.Clamp(safeArea.yMax, 0, screenSize.y)), screenSize);
        Rect safe = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        if (safe.width <= 0 || safe.height <= 0) return;
        float sidePadding = Mathf.Min(24f, safe.width * 0.1f);
        float bottomPadding = Mathf.Min(36f, safe.height * 0.08f);
        float aspect = idle.rect.height / idle.rect.width;
        float width = Mathf.Min(450f, safe.width - sidePadding * 2f, (safe.height - bottomPadding * 2f) / aspect);
        Vector2 size = new Vector2(width, width * aspect);
        Vector2 center = new Vector2(safe.center.x, safe.yMin + bottomPadding + size.y * 0.5f);
        rect.anchorMin = rect.anchorMax = Vector2.zero;
        rect.pivot = Vector2.one * 0.5f;
        rect.sizeDelta = size;
        rect.anchoredPosition = center - parentRect.rect.min;
        previousSafeArea = safeArea;
        previousScreen = screenSize;
        previousCanvasSize = canvasRect.rect.size;
    }

    private Vector2 ToParent(Vector2 screenPoint, Vector2 screenSize)
    {
        Rect canvas = canvasRect.rect;
        Vector3 local = new Vector3(canvas.xMin + screenPoint.x / screenSize.x * canvas.width,
            canvas.yMin + screenPoint.y / screenSize.y * canvas.height, 0);
        return parentRect.InverseTransformPoint(canvasRect.TransformPoint(local));
    }

    private void RestoreSkin()
    {
        if (!applied) return;
        RestoreVisibility();
        if (switcher != null)
        {
            if (switcher.unpressedSprite == idle) switcher.unpressedSprite = originalIdle;
            if (switcher.pressedSprite == pressed) switcher.pressedSprite = originalPressed;
        }
        if (image != null && (image.sprite == idle || image.sprite == pressed)) image.sprite = originalImage;
        if (rect != null)
        {
            rect.anchorMin = originalAnchorMin;
            rect.anchorMax = originalAnchorMax;
            rect.pivot = originalPivot;
            rect.sizeDelta = originalSize;
            rect.anchoredPosition = originalPosition;
        }
        applied = false;
    }

    public void SetResolvingPresentation(bool resolving)
    {
        if (!applied || visibilityGroup == null) return;
        if (!resolving)
        {
            RestoreVisibility();
            return;
        }
        if (!suppressed)
        {
            previousAlpha = visibilityGroup.alpha;
            previousBlocksRaycasts = visibilityGroup.blocksRaycasts;
            previousInteractable = visibilityGroup.interactable;
            suppressed = true;
        }
        visibilityGroup.alpha = 0;
        visibilityGroup.blocksRaycasts = false;
        visibilityGroup.interactable = false;
    }

    private void RestoreVisibility()
    {
        if (!suppressed) return;
        if (visibilityGroup != null)
        {
            visibilityGroup.alpha = previousAlpha;
            visibilityGroup.blocksRaycasts = previousBlocksRaycasts;
            visibilityGroup.interactable = previousInteractable;
        }
        suppressed = false;
    }

    public void ReleaseLook()
    {
        RestoreSkin();
        configured = false;
    }

    private void OnDisable() => RestoreSkin();
    private void OnDestroy()
    {
        RestoreSkin();
        if (!ownsVisibilityGroup || visibilityGroup == null) return;
        if (Application.isPlaying) Destroy(visibilityGroup);
        else DestroyImmediate(visibilityGroup);
    }
}
