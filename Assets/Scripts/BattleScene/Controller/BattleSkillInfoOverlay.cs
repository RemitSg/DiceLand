using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class BattleSkillInfoOverlay : MonoBehaviour
{
    [Header("Fonts")]
    [SerializeField] private TMP_FontAsset keywordFont;
    [SerializeField] private TMP_FontAsset bodyFont;
    [SerializeField] private int titleFontSize = 44;
    [SerializeField] private int bodyFontSize = 26;
    [SerializeField] private int buttonFontSize = 26;

    [Header("Colors")]
    [SerializeField] private Color backdropColor = new Color(0f, 0f, 0f, 0.55f);
    [SerializeField] private Color panelColor = new Color(0.1f, 0.08f, 0.06f, 0.96f);
    [SerializeField] private Color titleColor = Color.white;
    [SerializeField] private Color bodyColor = new Color(0.94f, 0.92f, 0.86f, 1f);
    [SerializeField] private Color accentColor = new Color(1f, 0.82f, 0.35f, 1f);
    [SerializeField] private Color triggerHighlightColor = new Color(1f, 0.78f, 0.3f, 1f);
    [SerializeField] private Color targetHighlightColor = new Color(0.48f, 0.9f, 1f, 1f);
    [SerializeField] private Color valueHighlightColor = new Color(1f, 0.45f, 0.45f, 1f);
    [SerializeField] private Color attackDicePowerColor = new Color(1f, 0.32f, 0.32f, 1f);
    [SerializeField] private Color defenseDicePowerColor = new Color(0.38f, 0.72f, 1f, 1f);
    [SerializeField] private Color skillButtonColor = new Color(0.25f, 0.2f, 0.12f, 0.98f);
    [SerializeField] private Color selectedSkillButtonColor = new Color(0.84f, 0.66f, 0.24f, 1f);
    [SerializeField] private Color skillButtonTextColor = Color.white;
    [SerializeField] private Color selectedSkillButtonTextColor = Color.black;

    [Header("Sprites")]
    [SerializeField] private Sprite panelBackgroundSprite;
    [SerializeField] private Sprite skillButtonSprite;
    [SerializeField] private Sprite selectedSkillButtonSprite;
    [SerializeField] private Sprite closeButtonSprite;

    [Header("Layout")]
    [SerializeField] private Vector2 panelSize = new Vector2(980f, 620f);
    [SerializeField] private Vector2 panelOffset = Vector2.zero;
    [SerializeField] private Vector2 skillButtonSize = new Vector2(280f, 82f);
    [SerializeField] private float skillButtonSpacing = 16f;
    [SerializeField] private float listLeftPadding = 36f;
    [SerializeField] private float listTopPadding = 122f;
    [SerializeField] private float detailLeftInset = 352f;
    [SerializeField] private float detailTopPadding = 120f;
    [SerializeField] private float detailRightPadding = 40f;
    [SerializeField] private float detailBottomPadding = 44f;
    [SerializeField] private float detailInnerPadding = 12f;
    [SerializeField] private float detailScrollbarWidth = 18f;

    [Header("Enemy Hover Layout")]
    [SerializeField] private Vector2 enemyHoverPanelSize = new Vector2(560f, 360f);
    [SerializeField] private Vector2 enemyHoverPanelOffset = new Vector2(-36f, -36f);
    [SerializeField] private int enemyHoverTitleFontSize = 32;
    [SerializeField] private int enemyHoverBodyFontSize = 24;

    public bool IsVisible => panelRoot != null && panelRoot.gameObject.activeSelf;

    private Canvas canvas;
    private RectTransform canvasRoot;
    private RectTransform panelRoot;
    private Button backdropButton;
    private Button closeButton;
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI subtitleText;
    private TextMeshProUGUI detailText;
    private ScrollRect detailScrollRect;
    private RectTransform detailViewport;
    private RectTransform detailContent;
    private RectTransform skillListRoot;
    private RectTransform hoverPanelRoot;
    private TextMeshProUGUI hoverTitleText;
    private TextMeshProUGUI hoverDetailText;
    private readonly List<Button> skillButtons = new List<Button>();
    private readonly List<TextMeshProUGUI> skillButtonTexts = new List<TextMeshProUGUI>();
    private Action hideCallback;
    private BattleAllyController currentAlly;
    private BattleEnemyController currentEnemy;
    private SpeedButton currentHoverSourceButton;
    private BattleButton cachedBattleButton;

    private void Awake()
    {
        EnsureUi();
        Hide(false);
    }

    private void OnDestroy()
    {
        SpeedButton.InputSuppressed = false;
    }

    public void ConfigureFonts(TMP_FontAsset keywordFontAsset, TMP_FontAsset bodyFontAsset)
    {
        if (keywordFontAsset != null)
            keywordFont = keywordFontAsset;
        if (bodyFontAsset != null)
            bodyFont = bodyFontAsset;

        ApplyFonts();
    }

    public void ShowAllySkillSelection(BattleAllyController ally)
    {
        if (ally == null)
            return;

        EnsureUi();
        currentAlly = ally;
        currentEnemy = null;
        ShowForActor(
            $"\u6b66\u5668\u6280\u80fd\uff1a{ally.GetDebugDisplayName()}",
            BuildWeaponSubtitle(ally),
            BuildAllyDetail(ally),
            null);

        RebuildSkillButtons(ally);
    }

    public void ShowEnemySkillInfo(BattleEnemyController enemy)
    {
        ShowEnemySkillInfo(enemy, null);
    }

    public void ShowEnemySkillInfo(BattleEnemyController enemy, SpeedButton sourceButton)
    {
        if (enemy == null)
            return;

        EnsureUi();
        currentAlly = null;
        currentEnemy = enemy;
        EnemySkill_SO skill = sourceButton != null ? enemy.GetSelectedSkillForButton(sourceButton) : enemy.SelectedSkill;
        ShowForActor(
            $"\u654c\u65b9\u6280\u80fd\uff1a{enemy.GetDebugDisplayName()}",
            skill != null ? skill.skillName : "\u5c1a\u672a\u51b3\u5b9a\u6280\u80fd",
            BuildSkillDetail(skill, "\u5f53\u524d\u6280\u80fd", false),
            null);

        ClearSkillButtons();
    }

    public void ShowEnemyHoverSkillInfo(BattleEnemyController enemy, SpeedButton sourceButton)
    {
        if (enemy == null)
            return;

        EnsureUi();
        if (IsVisible)
            return;

        EnemySkill_SO skill = sourceButton != null ? enemy.GetSelectedSkillForButton(sourceButton) : enemy.SelectedSkill;
        if (skill == null || hoverPanelRoot == null || hoverTitleText == null || hoverDetailText == null)
            return;

        currentHoverSourceButton = sourceButton;
        hoverTitleText.text = skill.skillName;
        hoverDetailText.text = BuildCompactEnemyHoverDetail(enemy, skill);
        hoverPanelRoot.gameObject.SetActive(true);
    }

    public void HideEnemyHoverSkillInfo(SpeedButton sourceButton = null)
    {
        if (hoverPanelRoot == null)
            return;

        if (sourceButton != null && currentHoverSourceButton != null && sourceButton != currentHoverSourceButton)
            return;

        hoverPanelRoot.gameObject.SetActive(false);
        if (sourceButton == null || sourceButton == currentHoverSourceButton)
            currentHoverSourceButton = null;
    }

    public void ShowEnemyDetail(BattleEnemyController enemy)
    {
        if (enemy == null)
            return;

        EnsureUi();
        currentAlly = null;
        currentEnemy = enemy;
        ShowForActor(
            $"敌人详情：{enemy.GetDebugDisplayName()}",
            $"最大生命：{enemy.MaxHP}",
            BuildEnemyDataDetail(enemy),
            null);

        ClearSkillButtons();
    }

    public void RefreshCurrentView()
    {
        if (!IsVisible)
            return;

        if (currentAlly != null)
        {
            subtitleText.text = BuildWeaponSubtitle(currentAlly);
            detailText.text = BuildAllyDetail(currentAlly);
            RebuildSkillButtons(currentAlly);
            ResetDetailScrollToTop();
            return;
        }

        if (currentEnemy != null)
        {
            detailText.text = BuildEnemyDetail(currentEnemy);
            ResetDetailScrollToTop();
        }
    }

    public void Hide()
    {
        Hide(true);
    }

    public void Hide(bool invokeCallback)
    {
        if (backdropButton != null)
            backdropButton.gameObject.SetActive(false);
        if (panelRoot != null)
            panelRoot.gameObject.SetActive(false);

        HideEnemyHoverSkillInfo();

        SpeedButton.InputSuppressed = false;

        if (invokeCallback && hideCallback != null)
        {
            Action callback = hideCallback;
            hideCallback = null;
            callback.Invoke();
        }
        else
        {
            hideCallback = null;
        }
    }

    private void ShowForActor(string title, string subtitle, string detail, Action onHide)
    {
        HideEnemyHoverSkillInfo();
        if (canvasRoot != null)
            canvasRoot.gameObject.SetActive(true);
        if (backdropButton != null)
            backdropButton.gameObject.SetActive(true);
        if (panelRoot != null)
            panelRoot.gameObject.SetActive(true);
        titleText.text = title;
        subtitleText.text = subtitle;
        detailText.text = detail;
        hideCallback = onHide;
        ResetDetailScrollToTop();
        SpeedButton.InputSuppressed = true;
    }

    private void EnsureUi()
    {
        if (canvasRoot != null)
            return;

        GameObject canvasGo = new GameObject("BattleSkillInfoCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);

        canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9600;

        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasRoot = canvasGo.GetComponent<RectTransform>();
        canvasRoot.anchorMin = Vector2.zero;
        canvasRoot.anchorMax = Vector2.one;
        canvasRoot.offsetMin = Vector2.zero;
        canvasRoot.offsetMax = Vector2.zero;

        GameObject backdropGo = new GameObject("Backdrop", typeof(RectTransform), typeof(Image), typeof(Button));
        backdropGo.transform.SetParent(canvasRoot, false);
        RectTransform backdropRect = backdropGo.GetComponent<RectTransform>();
        backdropRect.anchorMin = Vector2.zero;
        backdropRect.anchorMax = Vector2.one;
        backdropRect.offsetMin = Vector2.zero;
        backdropRect.offsetMax = Vector2.zero;

        Image backdropImage = backdropGo.GetComponent<Image>();
        backdropImage.color = backdropColor;
        backdropButton = backdropGo.GetComponent<Button>();
        backdropButton.onClick.AddListener(Hide);

        GameObject panelGo = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        panelGo.transform.SetParent(canvasRoot, false);
        panelRoot = panelGo.GetComponent<RectTransform>();
        panelRoot.anchorMin = new Vector2(0.5f, 0.5f);
        panelRoot.anchorMax = new Vector2(0.5f, 0.5f);
        panelRoot.pivot = new Vector2(0.5f, 0.5f);
        panelRoot.anchoredPosition = panelOffset;
        panelRoot.sizeDelta = panelSize;

        Image panelImage = panelGo.GetComponent<Image>();
        if (panelBackgroundSprite != null)
        {
            panelImage.sprite = panelBackgroundSprite;
            panelImage.type = Image.Type.Sliced;
            panelImage.color = Color.white;
        }
        else
        {
            panelImage.color = panelColor;
        }

        titleText = CreateText(panelRoot, "Title", new Vector2(32f, -26f), new Vector2(panelSize.x - 180f, 52f), titleFontSize, titleColor, true, TextAlignmentOptions.TopLeft);
        subtitleText = CreateText(panelRoot, "Subtitle", new Vector2(36f, -82f), new Vector2(panelSize.x - 180f, 44f), bodyFontSize, accentColor, true, TextAlignmentOptions.TopLeft);

        GameObject detailAreaGo = new GameObject("DetailArea", typeof(RectTransform));
        detailAreaGo.transform.SetParent(panelRoot, false);
        RectTransform detailAreaRect = detailAreaGo.GetComponent<RectTransform>();
        detailAreaRect.anchorMin = new Vector2(0f, 0f);
        detailAreaRect.anchorMax = new Vector2(1f, 1f);
        detailAreaRect.pivot = new Vector2(0f, 1f);
        detailAreaRect.anchoredPosition = new Vector2(detailLeftInset, -detailTopPadding);
        detailAreaRect.sizeDelta = new Vector2(-(detailLeftInset + detailRightPadding), -(detailTopPadding + detailBottomPadding));

        GameObject viewportGo = new GameObject("DetailViewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        viewportGo.transform.SetParent(detailAreaRect, false);
        detailViewport = viewportGo.GetComponent<RectTransform>();
        detailViewport.anchorMin = Vector2.zero;
        detailViewport.anchorMax = Vector2.one;
        detailViewport.offsetMin = Vector2.zero;
        detailViewport.offsetMax = new Vector2(-detailScrollbarWidth - detailInnerPadding, 0f);

        Image viewportImage = viewportGo.GetComponent<Image>();
        viewportImage.color = new Color(1f, 1f, 1f, 0.01f);
        viewportGo.GetComponent<Mask>().showMaskGraphic = false;

        GameObject contentGo = new GameObject("DetailContent", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(ContentSizeFitter));
        contentGo.transform.SetParent(detailViewport, false);
        detailContent = contentGo.GetComponent<RectTransform>();
        detailContent.anchorMin = new Vector2(0f, 1f);
        detailContent.anchorMax = new Vector2(1f, 1f);
        detailContent.pivot = new Vector2(0.5f, 1f);
        detailContent.anchoredPosition = new Vector2(0f, -detailInnerPadding);
        detailContent.offsetMin = new Vector2(detailInnerPadding, 0f);
        detailContent.offsetMax = new Vector2(-detailInnerPadding, 0f);

        detailText = contentGo.GetComponent<TextMeshProUGUI>();
        ConfigureText(detailText, bodyFontSize, bodyColor, false, TextAlignmentOptions.TopLeft);
        detailText.enableWordWrapping = true;
        detailText.overflowMode = TextOverflowModes.Overflow;

        ContentSizeFitter contentFitter = contentGo.GetComponent<ContentSizeFitter>();
        contentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        GameObject scrollbarGo = new GameObject("DetailScrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
        scrollbarGo.transform.SetParent(detailAreaRect, false);
        RectTransform scrollbarRect = scrollbarGo.GetComponent<RectTransform>();
        scrollbarRect.anchorMin = new Vector2(1f, 0f);
        scrollbarRect.anchorMax = new Vector2(1f, 1f);
        scrollbarRect.pivot = new Vector2(1f, 0.5f);
        scrollbarRect.sizeDelta = new Vector2(detailScrollbarWidth, 0f);
        scrollbarRect.anchoredPosition = Vector2.zero;

        Image scrollbarBackground = scrollbarGo.GetComponent<Image>();
        scrollbarBackground.color = new Color(0f, 0f, 0f, 0.18f);

        GameObject handleSlideAreaGo = new GameObject("SlidingArea", typeof(RectTransform));
        handleSlideAreaGo.transform.SetParent(scrollbarRect, false);
        RectTransform handleSlideAreaRect = handleSlideAreaGo.GetComponent<RectTransform>();
        handleSlideAreaRect.anchorMin = Vector2.zero;
        handleSlideAreaRect.anchorMax = Vector2.one;
        handleSlideAreaRect.offsetMin = new Vector2(2f, 2f);
        handleSlideAreaRect.offsetMax = new Vector2(-2f, -2f);

        GameObject handleGo = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handleGo.transform.SetParent(handleSlideAreaRect, false);
        RectTransform handleRect = handleGo.GetComponent<RectTransform>();
        handleRect.anchorMin = new Vector2(0f, 1f);
        handleRect.anchorMax = new Vector2(1f, 1f);
        handleRect.pivot = new Vector2(0.5f, 1f);
        handleRect.sizeDelta = new Vector2(0f, 80f);

        Image handleImage = handleGo.GetComponent<Image>();
        handleImage.color = accentColor;

        Scrollbar scrollbar = scrollbarGo.GetComponent<Scrollbar>();
        scrollbar.direction = Scrollbar.Direction.BottomToTop;
        scrollbar.handleRect = handleRect;
        scrollbar.targetGraphic = handleImage;

        detailScrollRect = detailAreaGo.AddComponent<ScrollRect>();
        detailScrollRect.content = detailContent;
        detailScrollRect.viewport = detailViewport;
        detailScrollRect.horizontal = false;
        detailScrollRect.vertical = true;
        detailScrollRect.scrollSensitivity = 30f;
        detailScrollRect.verticalScrollbar = scrollbar;
        detailScrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
        detailScrollRect.movementType = ScrollRect.MovementType.Clamped;

        GameObject listGo = new GameObject("SkillList", typeof(RectTransform));
        listGo.transform.SetParent(panelRoot, false);
        skillListRoot = listGo.GetComponent<RectTransform>();
        skillListRoot.anchorMin = new Vector2(0f, 1f);
        skillListRoot.anchorMax = new Vector2(0f, 1f);
        skillListRoot.pivot = new Vector2(0f, 1f);
        skillListRoot.anchoredPosition = new Vector2(listLeftPadding, -listTopPadding);

        GameObject closeGo = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
        closeGo.transform.SetParent(panelRoot, false);
        RectTransform closeRect = closeGo.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(1f, 1f);
        closeRect.anchorMax = new Vector2(1f, 1f);
        closeRect.pivot = new Vector2(1f, 1f);
        closeRect.anchoredPosition = new Vector2(-26f, -20f);
        closeRect.sizeDelta = new Vector2(64f, 64f);

        Image closeImage = closeGo.GetComponent<Image>();
        if (closeButtonSprite != null)
        {
            closeImage.sprite = closeButtonSprite;
            closeImage.type = Image.Type.Sliced;
            closeImage.color = Color.white;
        }
        else
        {
            closeImage.color = accentColor;
        }

        closeButton = closeGo.GetComponent<Button>();
        closeButton.onClick.AddListener(Hide);

        TextMeshProUGUI closeLabel = CreateText(closeRect, "Label", Vector2.zero, closeRect.sizeDelta, buttonFontSize, Color.black, true, TextAlignmentOptions.Center);
        closeLabel.rectTransform.anchorMin = Vector2.zero;
        closeLabel.rectTransform.anchorMax = Vector2.one;
        closeLabel.rectTransform.offsetMin = Vector2.zero;
        closeLabel.rectTransform.offsetMax = Vector2.zero;
        closeLabel.text = "X";

        GameObject hoverPanelGo = new GameObject("EnemyHoverPanel", typeof(RectTransform), typeof(Image));
        hoverPanelGo.transform.SetParent(canvasRoot, false);
        hoverPanelRoot = hoverPanelGo.GetComponent<RectTransform>();
        hoverPanelRoot.anchorMin = new Vector2(1f, 1f);
        hoverPanelRoot.anchorMax = new Vector2(1f, 1f);
        hoverPanelRoot.pivot = new Vector2(1f, 1f);
        hoverPanelRoot.anchoredPosition = enemyHoverPanelOffset;
        hoverPanelRoot.sizeDelta = enemyHoverPanelSize;

        Image hoverPanelImage = hoverPanelGo.GetComponent<Image>();
        if (panelBackgroundSprite != null)
        {
            hoverPanelImage.sprite = panelBackgroundSprite;
            hoverPanelImage.type = Image.Type.Sliced;
            hoverPanelImage.color = Color.white;
        }
        else
        {
            hoverPanelImage.color = panelColor;
        }

        hoverTitleText = CreateText(hoverPanelRoot, "HoverTitle", new Vector2(24f, -20f), new Vector2(enemyHoverPanelSize.x - 48f, 42f), enemyHoverTitleFontSize, titleColor, true, TextAlignmentOptions.TopLeft);
        hoverDetailText = CreateText(hoverPanelRoot, "HoverDetail", new Vector2(24f, -68f), new Vector2(enemyHoverPanelSize.x - 48f, enemyHoverPanelSize.y - 92f), enemyHoverBodyFontSize, bodyColor, false, TextAlignmentOptions.TopLeft);
        hoverDetailText.enableWordWrapping = true;
        hoverDetailText.overflowMode = TextOverflowModes.Overflow;
        hoverPanelRoot.gameObject.SetActive(false);

        ApplyFonts();
    }

    private void RebuildSkillButtons(BattleAllyController ally)
    {
        ClearSkillButtons();
        if (ally == null)
            return;

        ResolveBattleButton();
        EnemySkill_SO[] skills = ally.GetAvailableSkills();
        if (skills == null || skills.Length == 0)
            return;

        for (int i = 0; i < skills.Length; i++)
        {
            EnemySkill_SO skill = skills[i];
            if (skill == null)
                continue;

            int capturedIndex = i;
            GameObject buttonGo = new GameObject($"Skill_{i}", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonGo.transform.SetParent(skillListRoot, false);

            RectTransform rect = buttonGo.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(0f, -capturedIndex * (skillButtonSize.y + skillButtonSpacing));
            rect.sizeDelta = skillButtonSize;

            Image image = buttonGo.GetComponent<Image>();
            bool isSelected = capturedIndex == ally.CurrentSkillIndex;
            string unavailableReason = cachedBattleButton != null ? cachedBattleButton.GetAllySkillUnavailableReason(ally, skill) : string.Empty;
            bool canSelect = string.IsNullOrEmpty(unavailableReason);
            Color baseColor = isSelected ? selectedSkillButtonColor : skillButtonColor;
            image.color = canSelect ? baseColor : new Color(baseColor.r * 0.45f, baseColor.g * 0.45f, baseColor.b * 0.45f, baseColor.a * 0.75f);
            image.sprite = isSelected && selectedSkillButtonSprite != null ? selectedSkillButtonSprite : skillButtonSprite;
            image.type = image.sprite != null ? Image.Type.Sliced : Image.Type.Simple;

            Button button = buttonGo.GetComponent<Button>();
            button.interactable = canSelect;
            button.onClick.AddListener(() =>
            {
                if (cachedBattleButton != null && !cachedBattleButton.CanSelectSkillForAlly(ally, skill))
                    return;

                if (ally.SetSelectedSkillIndex(capturedIndex))
                {
                    subtitleText.text = BuildWeaponSubtitle(ally);
                    detailText.text = BuildAllyDetail(ally);
                    RebuildSkillButtons(ally);
                }
            });

            TextMeshProUGUI label = CreateText(rect, "Label", new Vector2(16f, -14f), new Vector2(skillButtonSize.x - 24f, skillButtonSize.y - 24f), buttonFontSize, isSelected ? selectedSkillButtonTextColor : skillButtonTextColor, true, TextAlignmentOptions.MidlineLeft);
            label.enableWordWrapping = true;
            label.text = BuildSkillButtonLabel(skill, isSelected, unavailableReason);

            skillButtons.Add(button);
            skillButtonTexts.Add(label);
        }

        skillListRoot.sizeDelta = new Vector2(skillButtonSize.x, Mathf.Max(skillButtonSize.y, skillButtons.Count * (skillButtonSize.y + skillButtonSpacing) - skillButtonSpacing));
    }

    private void ClearSkillButtons()
    {
        for (int i = skillButtons.Count - 1; i >= 0; i--)
        {
            if (skillButtons[i] != null)
                Destroy(skillButtons[i].gameObject);
        }

        skillButtons.Clear();
        skillButtonTexts.Clear();
        if (skillListRoot != null)
            skillListRoot.sizeDelta = Vector2.zero;
    }

    private void ApplyFonts()
    {
        ApplyFont(titleText, GetKeywordFont());
        ApplyFont(subtitleText, GetKeywordFont());
        ApplyFont(detailText, GetBodyFont());
        ApplyFont(hoverTitleText, GetKeywordFont());
        ApplyFont(hoverDetailText, GetBodyFont());

        for (int i = 0; i < skillButtonTexts.Count; i++)
            ApplyFont(skillButtonTexts[i], GetKeywordFont());

        if (closeButton != null)
        {
            TMP_Text[] closeTexts = closeButton.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < closeTexts.Length; i++)
                ApplyFont(closeTexts[i], GetKeywordFont());
        }
    }

    private TMP_FontAsset GetKeywordFont()
    {
        return keywordFont != null ? keywordFont : bodyFont;
    }

    private TMP_FontAsset GetBodyFont()
    {
        return bodyFont != null ? bodyFont : keywordFont;
    }

    private static void ApplyFont(TMP_Text text, TMP_FontAsset font)
    {
        if (text == null || font == null)
            return;

        text.font = font;
        text.UpdateMeshPadding();
        text.SetAllDirty();
    }

    private TextMeshProUGUI CreateText(RectTransform parent, string name, Vector2 anchoredPosition, Vector2 size, int fontSize, Color color, bool useKeywordFont, TextAlignmentOptions alignment)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
        ConfigureText(text, fontSize, color, useKeywordFont, alignment);
        return text;
    }

    private void ConfigureText(TextMeshProUGUI text, int fontSize, Color color, bool useKeywordFont, TextAlignmentOptions alignment)
    {
        if (text == null)
            return;

        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
        text.enableWordWrapping = false;
        ApplyFont(text, useKeywordFont ? GetKeywordFont() : GetBodyFont());
    }

    private static string BuildWeaponSubtitle(BattleAllyController ally)
    {
        Weapon_SO weaponData = ally != null ? ally.GetComponent<AllyController>()?.weapon?.weaponData : null;
        if (weaponData == null)
            return ally != null && ally.HasTemporarySkills ? "\u4ec5\u4f7f\u7528\u4e34\u65f6\u6280\u80fd" : "\u672a\u88c5\u5907\u6b66\u5668";

        string sourceLabel = ally != null && ally.TemporarySkillsOverrideWeaponPool
            ? "\u4e34\u65f6\u6280\u80fd"
            : weaponData.weaponName;
        return $"{sourceLabel}  \u5f53\u524d\u6280\u80fd\uff1a{ally.SelectedSkill?.skillName ?? "\u672a\u914d\u7f6e"}";
    }

    private string BuildAllyDetail(BattleAllyController ally)
    {
        if (ally == null)
            return "\u672a\u627e\u5230\u89d2\u8272\u6570\u636e\u3002";

        return BuildSkillDetail(ally.SelectedSkill, "\u5f53\u524d\u6280\u80fd", true, null, ally);
    }

    private string BuildEnemyDetail(BattleEnemyController enemy)
    {
        if (enemy == null)
            return "\u672a\u627e\u5230\u654c\u65b9\u6570\u636e\u3002";

        return BuildSkillDetail(enemy.SelectedSkill, "\u5f53\u524d\u6280\u80fd", false);
    }

    private void ResolveBattleButton()
    {
        if (cachedBattleButton == null)
            cachedBattleButton = FindObjectOfType<BattleButton>();
    }

    private string BuildEnemyDataDetail(BattleEnemyController enemy)
    {
        if (enemy == null || enemy.enemyData == null)
            return "未找到敌人数据。";

        EnemyData_SO enemyData = enemy.enemyData;
        string description = string.IsNullOrWhiteSpace(enemyData.enemyDescription)
            ? "暂无描述。"
            : enemyData.enemyDescription;

        List<string> lines = new List<string>
        {
            $"名称：{enemyData.enemyName}",
            $"最大生命：{enemyData.baseHP}",
            string.Empty,
            "描述：",
            description
        };

        if (enemyData.passiveDescriptions != null && enemyData.passiveDescriptions.Count > 0)
        {
            lines.Add(string.Empty);
            lines.Add("特殊被动：");
            for (int i = 0; i < enemyData.passiveDescriptions.Count; i++)
            {
                EnemyData_SO.PassiveDescriptionEntry entry = enemyData.passiveDescriptions[i];
                if (string.IsNullOrWhiteSpace(entry.passiveName) && string.IsNullOrWhiteSpace(entry.description))
                    continue;

                if (!string.IsNullOrWhiteSpace(entry.passiveName))
                    lines.Add($"• {BuildColoredToken(entry.passiveName, GetPassiveNameColor(entry.displayColorType))}");
                if (!string.IsNullOrWhiteSpace(entry.description))
                    lines.Add($"  {entry.description}");
            }
        }

        return string.Join("\n", lines);
    }

    private string BuildSkillDetail(EnemySkill_SO skill, string header, bool attackerIsAlly, Func<int, EnemySkill_SO.DicePowerRange> rangeResolver = null, BattleAllyController allyContext = null)
    {
        if (skill == null)
            return $"{header}\n\u672a\u914d\u7f6e\u6280\u80fd\u3002";

        List<string> rangeLines = new List<string>();
        List<string> effectLines = new List<string>();
        Color dicePowerColor = GetDicePowerDisplayColor(skill);
        for (int i = 0; i < skill.diceCount; i++)
        {
            EnemySkill_SO.DicePowerRange range = rangeResolver != null ? rangeResolver(i) : skill.GetDicePowerRange(i);
            string diceLabel = BuildDiceDisplayLabel(skill, i);
            string diceEffectLabel = BuildDiceEffectLabel(skill, i);
            string minLabel = BuildColoredToken(range.minPower.ToString(), dicePowerColor);
            string maxLabel = BuildColoredToken(range.maxPower.ToString(), dicePowerColor);
            rangeLines.Add($"{diceLabel}：{minLabel} ~ {maxLabel}");

            EnemySkill_SO.DiceEffectSettings effectSettings = skill.GetDiceEffectSettings(i);
            if (effectSettings.damageEffects != null)
            {
                for (int effectIndex = 0; effectIndex < effectSettings.damageEffects.Length; effectIndex++)
                {
                    EnemySkill_SO.DamageEffectEntry effect = effectSettings.damageEffects[effectIndex];
                    if (effect.damageAmount <= 0)
                        continue;

                    string triggerLabel = BuildColoredToken(GetGeneralEffectTriggerLabel(effect.trigger), triggerHighlightColor);
                    string targetLabel = BuildColoredToken(GetDamageTargetLabel(effect.targetMode, effect.randomTargetCount, attackerIsAlly), targetHighlightColor);
                    string valueLabel = BuildColoredToken(effect.damageAmount.ToString(), valueHighlightColor);
                    effectLines.Add($"{diceEffectLabel}：{triggerLabel} 对 {targetLabel} 造成 {valueLabel} 点伤害");
                }
            }

            if (effectSettings.statusEffects != null)
            {
                for (int effectIndex = 0; effectIndex < effectSettings.statusEffects.Length; effectIndex++)
                {
                    EnemySkill_SO.StatusEffectEntry effect = effectSettings.statusEffects[effectIndex];
                    if (effect.stackAmount <= 0)
                        continue;

                    string triggerLabel = BuildColoredToken(GetGeneralEffectTriggerLabel(effect.trigger), triggerHighlightColor);
                    string targetLabel = BuildColoredToken(GetDamageTargetLabel(effect.targetMode, effect.randomTargetCount, attackerIsAlly), targetHighlightColor);
                    string stackLabel = BuildColoredToken(effect.stackAmount.ToString(), valueHighlightColor);
                    string statusLabel = BuildColoredToken(GetStatusLabel(effect), valueHighlightColor);
                    string timingLabel = effect.applyTiming == EnemySkill_SO.StatusApplyTiming.NextRoundStart
                        ? BuildColoredToken(
                            Mathf.Max(1, effect.applyRepeatCount) > 1
                                ? $"\u63a5\u4e0b\u6765 {Mathf.Max(1, effect.applyRepeatCount)} \u4e2a\u56de\u5408\u5f00\u59cb\u65f6"
                                : $"\u4e0b {Mathf.Max(1, effect.applyAfterRoundStarts)} \u4e2a\u56de\u5408\u5f00\u59cb\u65f6",
                            triggerHighlightColor)
                        : string.Empty;
                    effectLines.Add(effect.applyTiming == EnemySkill_SO.StatusApplyTiming.NextRoundStart
                        ? $"{diceEffectLabel}：{triggerLabel} 使 {targetLabel} 于 {timingLabel} 获得 {stackLabel} 层 {statusLabel}"
                        : $"{diceEffectLabel}：{triggerLabel} 使 {targetLabel} 获得 {stackLabel} 层 {statusLabel}");
                }
            }

            if (effectSettings.statusBurstEffects != null)
            {
                for (int effectIndex = 0; effectIndex < effectSettings.statusBurstEffects.Length; effectIndex++)
                {
                    EnemySkill_SO.StatusBurstDamageEffectEntry effect = effectSettings.statusBurstEffects[effectIndex];
                    if (effect.damagePerStack <= 0)
                        continue;

                    string triggerLabel = BuildColoredToken(GetGeneralEffectTriggerLabel(effect.trigger), triggerHighlightColor);
                    string targetLabel = BuildColoredToken(GetDamageTargetLabel(effect.targetMode, effect.randomTargetCount, attackerIsAlly), targetHighlightColor);
                    string multiplierLabel = BuildColoredToken(effect.damagePerStack.ToString(), valueHighlightColor);
                    string statusLabel = BuildColoredToken(GetStatusLabel(effect), targetHighlightColor);
                    effectLines.Add(effect.clearStacksAfterBurst
                        ? $"{diceEffectLabel}：{triggerLabel} 对 {targetLabel} 引发 {multiplierLabel} 重 {statusLabel} 爆发，造成 {multiplierLabel} x 层数 的额外伤害，并清空 {statusLabel}"
                        : $"{diceEffectLabel}：{triggerLabel} 对 {targetLabel} 引发 {multiplierLabel} 重 {statusLabel} 爆发，造成 {multiplierLabel} x 层数 的额外伤害");
                }
            }

            if (effectSettings.statusStackOperationEffects != null)
            {
                for (int effectIndex = 0; effectIndex < effectSettings.statusStackOperationEffects.Length; effectIndex++)
                {
                    EnemySkill_SO.StatusStackOperationEffectEntry effect = effectSettings.statusStackOperationEffects[effectIndex];
                    string triggerLabel = BuildColoredToken(GetGeneralEffectTriggerLabel(effect.trigger), triggerHighlightColor);
                    string targetLabel = BuildColoredToken(GetDamageTargetLabel(effect.targetMode, effect.randomTargetCount, attackerIsAlly), targetHighlightColor);
                    string statusLabel = BuildColoredToken(GetStatusLabel(effect), targetHighlightColor);

                    if (effect.operationType == EnemySkill_SO.StatusStackOperationType.ClearStacks)
                    {
                        effectLines.Add($"{diceEffectLabel}：{triggerLabel} 清空 {targetLabel} 的 {statusLabel}");
                    }
                    else
                    {
                        string valueLabel = BuildColoredToken(Mathf.Max(1, effect.stackAmount).ToString(), valueHighlightColor);
                        effectLines.Add($"{diceEffectLabel}：{triggerLabel} 使 {targetLabel} 的 {statusLabel} 减少 {valueLabel} 层");
                    }
                }
            }

            if (effectSettings.enemyStageTransitionEffects != null)
            {
                for (int effectIndex = 0; effectIndex < effectSettings.enemyStageTransitionEffects.Length; effectIndex++)
                {
                    EnemySkill_SO.EnemyStageTransitionEffectEntry effect = effectSettings.enemyStageTransitionEffects[effectIndex];
                    string triggerLabel = BuildColoredToken(GetGeneralEffectTriggerLabel(effect.trigger), triggerHighlightColor);
                    string valueLabel = BuildColoredToken((effect.targetStageIndex + 1).ToString(), valueHighlightColor);
                    if (effect.applyTiming == EnemySkill_SO.EnemyStageTransitionApplyTiming.NextRoundStart)
                        effectLines.Add($"{diceEffectLabel}：{triggerLabel} 于 下个回合开始时 切换至 第 {valueLabel} 阶段");
                    else
                        effectLines.Add($"{diceEffectLabel}：{triggerLabel} 切换至 第 {valueLabel} 阶段");
                }
            }

            if (effectSettings.healingEffects != null)
            {
                for (int effectIndex = 0; effectIndex < effectSettings.healingEffects.Length; effectIndex++)
                {
                    EnemySkill_SO.HealingEffectEntry effect = effectSettings.healingEffects[effectIndex];
                    string triggerLabel = BuildColoredToken(GetGeneralEffectTriggerLabel(effect.trigger), triggerHighlightColor);
                    string targetLabel = BuildColoredToken(GetDamageTargetLabel(effect.targetMode, effect.randomTargetCount, attackerIsAlly), targetHighlightColor);
                    string conditionPrefix = string.Empty;
                    if (effect.useCondition)
                    {
                        string conditionLabel = BuildColoredToken(GetHealingConditionLabel(effect), targetHighlightColor);
                        conditionPrefix = $"若 {conditionLabel} ，则 ";
                    }

                    switch (effect.amountMode)
                    {
                        case EnemySkill_SO.HealAmountMode.DamageDealt:
                            effectLines.Add($"{diceEffectLabel}：{triggerLabel} {conditionPrefix}使 {targetLabel} 恢复 相当于 造成伤害 的生命");
                            break;
                        case EnemySkill_SO.HealAmountMode.StatusStacks:
                            string statusLabel = BuildColoredToken(GetStatusLabel(effect), targetHighlightColor);
                            string sourceLabel = BuildColoredToken(GetDamageTargetLabel(effect.statusStackSourceMode, effect.randomTargetCount, attackerIsAlly), targetHighlightColor);
                            string perStackLabel = BuildColoredToken(Mathf.Max(1, effect.healAmount).ToString(), valueHighlightColor);
                            effectLines.Add($"{diceEffectLabel}：{triggerLabel} {conditionPrefix}使 {targetLabel} 根据 {sourceLabel} 的 {statusLabel} 层数，每层 恢复 {perStackLabel} 点生命");
                            break;
                        default:
                            string healValueLabel = BuildColoredToken(effect.healAmount.ToString(), valueHighlightColor);
                            effectLines.Add($"{diceEffectLabel}：{triggerLabel} {conditionPrefix}使 {targetLabel} 恢复 {healValueLabel} 点生命");
                            break;
                    }
                }
            }

            if (effectSettings.powerModifiers != null)
            {
                for (int effectIndex = 0; effectIndex < effectSettings.powerModifiers.Length; effectIndex++)
                {
                    EnemySkill_SO.ConditionalPowerModifierEntry effect = effectSettings.powerModifiers[effectIndex];
                    bool hasFlatBonus = effect.powerBonus != 0;
                    bool hasScalingBonus = effect.condition == EnemySkill_SO.PowerModifierCondition.TargetStatusStacksPerThreshold && effect.powerBonusPerStep != 0;
                    if (!hasFlatBonus && !hasScalingBonus)
                        continue;

                    string triggerLabel = BuildColoredToken(GetPowerModifierTriggerLabel(effect.trigger), triggerHighlightColor);

                    if (effect.useCondition)
                    {
                        string conditionLabel = BuildColoredToken(GetPowerModifierConditionLabel(effect), targetHighlightColor);
                        if (effect.condition == EnemySkill_SO.PowerModifierCondition.TargetStatusStacksPerThreshold)
                        {
                            string perStepValueLabel = BuildColoredToken(FormatSignedInt(effect.powerBonusPerStep), valueHighlightColor);
                            effectLines.Add($"{diceEffectLabel}：{triggerLabel} 若 {conditionLabel} ，则 每达成一次条件，威力 {perStepValueLabel}");
                        }
                        else
                        {
                            string valueLabel = BuildColoredToken(FormatSignedInt(effect.powerBonus), valueHighlightColor);
                            effectLines.Add($"{diceEffectLabel}：{triggerLabel} 若 {conditionLabel} ，则 威力 {valueLabel}");
                        }
                    }
                    else
                    {
                        string valueLabel = BuildColoredToken(FormatSignedInt(effect.powerBonus), valueHighlightColor);
                        effectLines.Add($"{diceEffectLabel}：{triggerLabel} 威力 {valueLabel}");
                    }
                }
            }
        }

        string description = string.IsNullOrWhiteSpace(skill.description) ? "\u6682\u65e0\u63cf\u8ff0\u3002" : skill.description;
        string targetPrioritySection = BuildTargetPrioritySummary(skill);
        string requirementSection = string.Empty;
        if (attackerIsAlly && skill.RequiresSelfStatusToUse && skill.requiredSelfStatusData != null)
        {
            int requiredStacks = Mathf.Max(1, skill.requiredSelfStatusStacks);
            int currentStacks = allyContext != null ? allyContext.GetStatusStacks(skill.requiredSelfStatusData) : 0;
            string currentSuffix = allyContext != null ? $"（当前 {currentStacks} 层）" : string.Empty;
            string consumeSuffix = skill.keepRequiredSelfStatusAfterUse ? "，施放后不消耗" : "，施放后消耗";
            requirementSection = $"\n施放条件：自身至少拥有 {requiredStacks} 层 {skill.requiredSelfStatusData.DisplayName}{currentSuffix}{consumeSuffix}";
        }
        string effectSection = effectLines.Count > 0
            ? $"\n\n\u8bcd\u6761\uff1a\n{string.Join("\n", effectLines)}"
            : string.Empty;
        return $"{header}\n\u6280\u80fd\uff1a{skill.skillName}\n\u80fd\u8017\uff1a{Mathf.Max(0, skill.energyCost)}{requirementSection}\n\u9ab0\u5b50\u6570\uff1a{skill.diceCount}\n{string.Join("\n", rangeLines)}{targetPrioritySection}{effectSection}\n\n\u63cf\u8ff0\uff1a\n{description}";
    }

    private string BuildCompactSkillHoverDetail(EnemySkill_SO skill, bool attackerIsAlly)
    {
        if (skill == null)
            return "未配置技能。";

        List<string> rangeLines = new List<string>();
        List<string> effectLines = new List<string>();
        Color dicePowerColor = GetDicePowerDisplayColor(skill);

        for (int i = 0; i < skill.diceCount; i++)
        {
            EnemySkill_SO.DicePowerRange range = skill.GetDicePowerRange(i);
            string diceLabel = BuildDiceDisplayLabel(skill, i);
            string diceEffectLabel = BuildDiceEffectLabel(skill, i);
            string minLabel = BuildColoredToken(range.minPower.ToString(), dicePowerColor);
            string maxLabel = BuildColoredToken(range.maxPower.ToString(), dicePowerColor);
            rangeLines.Add($"{diceLabel}：{minLabel} ~ {maxLabel}");

            EnemySkill_SO.DiceEffectSettings effectSettings = skill.GetDiceEffectSettings(i);
            AppendEffectLines(effectLines, diceEffectLabel, effectSettings, attackerIsAlly);
        }

        return effectLines.Count > 0
            ? $"{string.Join("\n", rangeLines)}\n\n词条：\n{string.Join("\n", effectLines)}"
            : $"{string.Join("\n", rangeLines)}\n\n词条：\n无";
    }

    private string BuildCompactEnemyHoverDetail(BattleEnemyController enemy, EnemySkill_SO skill)
    {
        if (enemy == null)
            return BuildCompactSkillHoverDetail(skill, false);

        if (skill == null)
            return "未配置技能。";

        List<string> rangeLines = new List<string>();
        List<string> effectLines = new List<string>();
        Color dicePowerColor = GetDicePowerDisplayColor(skill);

        for (int i = 0; i < skill.diceCount; i++)
        {
            EnemySkill_SO.DicePowerRange range = skill.GetDicePowerRange(i);
            string diceLabel = BuildDiceDisplayLabel(skill, i);
            string diceEffectLabel = BuildDiceEffectLabel(skill, i);
            string minLabel = BuildColoredToken(range.minPower.ToString(), dicePowerColor);
            string maxLabel = BuildColoredToken(range.maxPower.ToString(), dicePowerColor);
            rangeLines.Add($"{diceLabel}：{minLabel} ~ {maxLabel}");

            EnemySkill_SO.DiceEffectSettings effectSettings = skill.GetDiceEffectSettings(i);
            AppendEffectLines(effectLines, diceEffectLabel, effectSettings, false);
        }

        return effectLines.Count > 0
            ? $"{string.Join("\n", rangeLines)}\n\n词条：\n{string.Join("\n", effectLines)}"
            : $"{string.Join("\n", rangeLines)}\n\n词条：\n无";
    }

    private void AppendEffectLines(List<string> effectLines, string diceEffectLabel, EnemySkill_SO.DiceEffectSettings effectSettings, bool attackerIsAlly)
    {
        if (effectSettings.damageEffects != null)
        {
            for (int effectIndex = 0; effectIndex < effectSettings.damageEffects.Length; effectIndex++)
            {
                EnemySkill_SO.DamageEffectEntry effect = effectSettings.damageEffects[effectIndex];
                if (effect.damageAmount <= 0)
                    continue;

                string triggerLabel = BuildColoredToken(GetGeneralEffectTriggerLabel(effect.trigger), triggerHighlightColor);
                string targetLabel = BuildColoredToken(GetDamageTargetLabel(effect.targetMode, effect.randomTargetCount, attackerIsAlly), targetHighlightColor);
                string valueLabel = BuildColoredToken(effect.damageAmount.ToString(), valueHighlightColor);
                effectLines.Add($"{diceEffectLabel}：{triggerLabel} 对 {targetLabel} 造成 {valueLabel} 点伤害");
            }
        }

        if (effectSettings.statusEffects != null)
        {
            for (int effectIndex = 0; effectIndex < effectSettings.statusEffects.Length; effectIndex++)
            {
                EnemySkill_SO.StatusEffectEntry effect = effectSettings.statusEffects[effectIndex];
                if (effect.stackAmount <= 0)
                    continue;

                string triggerLabel = BuildColoredToken(GetGeneralEffectTriggerLabel(effect.trigger), triggerHighlightColor);
                string targetLabel = BuildColoredToken(GetDamageTargetLabel(effect.targetMode, effect.randomTargetCount, attackerIsAlly), targetHighlightColor);
                string stackLabel = BuildColoredToken(effect.stackAmount.ToString(), valueHighlightColor);
                string statusLabel = BuildColoredToken(GetStatusLabel(effect), valueHighlightColor);
                string timingLabel = effect.applyTiming == EnemySkill_SO.StatusApplyTiming.NextRoundStart
                    ? BuildColoredToken(
                        Mathf.Max(1, effect.applyRepeatCount) > 1
                            ? $"接下来 {Mathf.Max(1, effect.applyRepeatCount)} 个回合开始时"
                            : $"下 {Mathf.Max(1, effect.applyAfterRoundStarts)} 个回合开始时",
                        triggerHighlightColor)
                    : string.Empty;
                effectLines.Add(effect.applyTiming == EnemySkill_SO.StatusApplyTiming.NextRoundStart
                    ? $"{diceEffectLabel}：{triggerLabel} 使 {targetLabel} 于 {timingLabel} 获得 {stackLabel} 层 {statusLabel}"
                    : $"{diceEffectLabel}：{triggerLabel} 使 {targetLabel} 获得 {stackLabel} 层 {statusLabel}");
            }
        }

        if (effectSettings.statusBurstEffects != null)
        {
            for (int effectIndex = 0; effectIndex < effectSettings.statusBurstEffects.Length; effectIndex++)
            {
                EnemySkill_SO.StatusBurstDamageEffectEntry effect = effectSettings.statusBurstEffects[effectIndex];
                if (effect.damagePerStack <= 0)
                    continue;

                string triggerLabel = BuildColoredToken(GetGeneralEffectTriggerLabel(effect.trigger), triggerHighlightColor);
                string targetLabel = BuildColoredToken(GetDamageTargetLabel(effect.targetMode, effect.randomTargetCount, attackerIsAlly), targetHighlightColor);
                string multiplierLabel = BuildColoredToken(effect.damagePerStack.ToString(), valueHighlightColor);
                string statusLabel = BuildColoredToken(GetStatusLabel(effect), targetHighlightColor);
                effectLines.Add(effect.clearStacksAfterBurst
                    ? $"{diceEffectLabel}：{triggerLabel} 对 {targetLabel} 引发 {multiplierLabel} 重 {statusLabel} 爆发，造成 {multiplierLabel} x 层数 的额外伤害，并清空 {statusLabel}"
                    : $"{diceEffectLabel}：{triggerLabel} 对 {targetLabel} 引发 {multiplierLabel} 重 {statusLabel} 爆发，造成 {multiplierLabel} x 层数 的额外伤害");
            }
        }

        if (effectSettings.statusStackOperationEffects != null)
        {
            for (int effectIndex = 0; effectIndex < effectSettings.statusStackOperationEffects.Length; effectIndex++)
            {
                EnemySkill_SO.StatusStackOperationEffectEntry effect = effectSettings.statusStackOperationEffects[effectIndex];
                string triggerLabel = BuildColoredToken(GetGeneralEffectTriggerLabel(effect.trigger), triggerHighlightColor);
                string targetLabel = BuildColoredToken(GetDamageTargetLabel(effect.targetMode, effect.randomTargetCount, attackerIsAlly), targetHighlightColor);
                string statusLabel = BuildColoredToken(GetStatusLabel(effect), targetHighlightColor);

                if (effect.operationType == EnemySkill_SO.StatusStackOperationType.ClearStacks)
                    effectLines.Add($"{diceEffectLabel}：{triggerLabel} 清空 {targetLabel} 的 {statusLabel}");
                else
                    effectLines.Add($"{diceEffectLabel}：{triggerLabel} 使 {targetLabel} 的 {statusLabel} 减少 {BuildColoredToken(Mathf.Max(1, effect.stackAmount).ToString(), valueHighlightColor)} 层");
            }
        }

        if (effectSettings.enemyStageTransitionEffects != null)
        {
            for (int effectIndex = 0; effectIndex < effectSettings.enemyStageTransitionEffects.Length; effectIndex++)
            {
                EnemySkill_SO.EnemyStageTransitionEffectEntry effect = effectSettings.enemyStageTransitionEffects[effectIndex];
                string triggerLabel = BuildColoredToken(GetGeneralEffectTriggerLabel(effect.trigger), triggerHighlightColor);
                string valueLabel = BuildColoredToken((effect.targetStageIndex + 1).ToString(), valueHighlightColor);
                effectLines.Add(effect.applyTiming == EnemySkill_SO.EnemyStageTransitionApplyTiming.NextRoundStart
                    ? $"{diceEffectLabel}：{triggerLabel} 于 下个回合开始时 切换至 第 {valueLabel} 阶段"
                    : $"{diceEffectLabel}：{triggerLabel} 切换至 第 {valueLabel} 阶段");
            }
        }

        if (effectSettings.healingEffects != null)
        {
            for (int effectIndex = 0; effectIndex < effectSettings.healingEffects.Length; effectIndex++)
            {
                EnemySkill_SO.HealingEffectEntry effect = effectSettings.healingEffects[effectIndex];
                string triggerLabel = BuildColoredToken(GetGeneralEffectTriggerLabel(effect.trigger), triggerHighlightColor);
                string targetLabel = BuildColoredToken(GetDamageTargetLabel(effect.targetMode, effect.randomTargetCount, attackerIsAlly), targetHighlightColor);
                string conditionPrefix = string.Empty;
                if (effect.useCondition)
                    conditionPrefix = $"若 {BuildColoredToken(GetHealingConditionLabel(effect), targetHighlightColor)} ，则 ";

                switch (effect.amountMode)
                {
                    case EnemySkill_SO.HealAmountMode.DamageDealt:
                        effectLines.Add($"{diceEffectLabel}：{triggerLabel} {conditionPrefix}使 {targetLabel} 恢复 相当于 造成伤害 的生命");
                        break;
                    case EnemySkill_SO.HealAmountMode.StatusStacks:
                        effectLines.Add($"{diceEffectLabel}：{triggerLabel} {conditionPrefix}使 {targetLabel} 根据 {BuildColoredToken(GetDamageTargetLabel(effect.statusStackSourceMode, effect.randomTargetCount, attackerIsAlly), targetHighlightColor)} 的 {BuildColoredToken(GetStatusLabel(effect), targetHighlightColor)} 层数，每层 恢复 {BuildColoredToken(Mathf.Max(1, effect.healAmount).ToString(), valueHighlightColor)} 点生命");
                        break;
                    default:
                        effectLines.Add($"{diceEffectLabel}：{triggerLabel} {conditionPrefix}使 {targetLabel} 恢复 {BuildColoredToken(effect.healAmount.ToString(), valueHighlightColor)} 点生命");
                        break;
                }
            }
        }

        if (effectSettings.powerModifiers != null)
        {
            for (int effectIndex = 0; effectIndex < effectSettings.powerModifiers.Length; effectIndex++)
            {
                EnemySkill_SO.ConditionalPowerModifierEntry effect = effectSettings.powerModifiers[effectIndex];
                bool hasFlatBonus = effect.powerBonus != 0;
                bool hasScalingBonus = effect.condition == EnemySkill_SO.PowerModifierCondition.TargetStatusStacksPerThreshold && effect.powerBonusPerStep != 0;
                if (!hasFlatBonus && !hasScalingBonus)
                    continue;

                string triggerLabel = BuildColoredToken(GetPowerModifierTriggerLabel(effect.trigger), triggerHighlightColor);
                if (effect.useCondition)
                {
                    string conditionLabel = BuildColoredToken(GetPowerModifierConditionLabel(effect), targetHighlightColor);
                    effectLines.Add(effect.condition == EnemySkill_SO.PowerModifierCondition.TargetStatusStacksPerThreshold
                        ? $"{diceEffectLabel}：{triggerLabel} 若 {conditionLabel} ，则 每达成一次条件，威力 {BuildColoredToken(FormatSignedInt(effect.powerBonusPerStep), valueHighlightColor)}"
                        : $"{diceEffectLabel}：{triggerLabel} 若 {conditionLabel} ，则 威力 {BuildColoredToken(FormatSignedInt(effect.powerBonus), valueHighlightColor)}");
                }
                else
                {
                    effectLines.Add($"{diceEffectLabel}：{triggerLabel} 威力 {BuildColoredToken(FormatSignedInt(effect.powerBonus), valueHighlightColor)}");
                }
            }
        }
    }

    private static string BuildSkillButtonLabel(EnemySkill_SO skill, bool isSelected, string unavailableReason)
    {
        if (skill == null)
            return isSelected ? "\u5df2\u88c5\u5907\u6280\u80fd" : "\u672a\u547d\u540d\u6280\u80fd";

        string requirementSuffix = string.Empty;
        if (skill.RequiresSelfStatusToUse && skill.requiredSelfStatusData != null)
            requirementSuffix = $" [{skill.requiredSelfStatusData.DisplayName} {Mathf.Max(1, skill.requiredSelfStatusStacks)}]";

        string equippedSuffix = isSelected ? "  [已装备]" : string.Empty;
        string disabledSuffix = string.IsNullOrWhiteSpace(unavailableReason) ? string.Empty : $"  [{unavailableReason}]";
        return $"{skill.skillName}{requirementSuffix}{equippedSuffix}\n能耗 {Mathf.Max(0, skill.energyCost)}{disabledSuffix}";
    }

    private void ResetDetailScrollToTop()
    {
        if (detailScrollRect == null)
            return;

        Canvas.ForceUpdateCanvases();
        detailScrollRect.verticalNormalizedPosition = 1f;
        LayoutRebuilder.ForceRebuildLayoutImmediate(detailContent);
        Canvas.ForceUpdateCanvases();
        detailScrollRect.verticalNormalizedPosition = 1f;
    }

    private static string BuildColoredToken(string content, Color color)
    {
        return $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{content}</color>";
    }

    private string BuildDiceDisplayLabel(EnemySkill_SO skill, int diceIndex)
    {
        return $"{GetDicePrefixLabel(skill, diceIndex)}";
    }

    private string BuildDiceEffectLabel(EnemySkill_SO skill, int diceIndex)
    {
        return $"{GetDicePrefixLabel(skill, diceIndex)}词条";
    }

    private static string GetDicePrefixLabel(EnemySkill_SO skill, int diceIndex)
    {
        bool isDefense = skill != null && skill.IsDefenseSkill;
        return isDefense
            ? $"第 {diceIndex + 1} 防御骰"
            : $"第 {diceIndex + 1} 攻击骰";
    }

    private Color GetDicePowerDisplayColor(EnemySkill_SO skill)
    {
        return skill != null && skill.IsDefenseSkill
            ? defenseDicePowerColor
            : attackDicePowerColor;
    }

    private static string FormatSignedInt(int value)
    {
        return value > 0 ? $"+{value}" : value.ToString();
    }

    private static Color GetPassiveNameColor(EnemyData_SO.PassiveDisplayColorType colorType)
    {
        switch (colorType)
        {
            case EnemyData_SO.PassiveDisplayColorType.Red:
                return new Color(0.95f, 0.32f, 0.28f, 1f);
            case EnemyData_SO.PassiveDisplayColorType.Orange:
                return new Color(1f, 0.62f, 0.2f, 1f);
            default:
                return new Color(1f, 0.86f, 0.3f, 1f);
        }
    }

    private static string GetDamageTargetLabel(EnemySkill_SO.DamageTargetMode targetMode, int randomTargetCount, bool attackerIsAlly)
    {
        switch (targetMode)
        {
            case EnemySkill_SO.DamageTargetMode.Self:
                return "\u81ea\u5df1";
            case EnemySkill_SO.DamageTargetMode.CurrentTarget:
                return "\u5f53\u524d\u76ee\u6807";
            case EnemySkill_SO.DamageTargetMode.RandomOpponents:
                return $"\u968f\u673a {Mathf.Max(1, randomTargetCount)} \u540d\u76ee\u6807";
            case EnemySkill_SO.DamageTargetMode.AllOpponents:
                return "\u6240\u6709\u76ee\u6807";
            default:
                return "\u76ee\u6807";
        }
    }

    private static string GetStatusLabel(EnemySkill_SO.StatusEffectEntry effect)
    {
        if (effect.statusData != null)
            return effect.statusData.DisplayName;

        switch (effect.statusType)
        {
            case EnemySkill_SO.DiceStatusType.Burn:
                return "\u71c3\u70e7";
            case EnemySkill_SO.DiceStatusType.Electro:
                return "\u7535\u6d41";
            default:
                return "\u672a\u914d\u7f6e\u72b6\u6001";
        }
    }

    private static string GetStatusLabel(EnemySkill_SO.StatusBurstDamageEffectEntry effect)
    {
        if (effect.statusData != null)
            return effect.statusData.DisplayName;

        switch (effect.statusType)
        {
            case EnemySkill_SO.DiceStatusType.Burn:
                return "\u71c3\u70e7";
            case EnemySkill_SO.DiceStatusType.Electro:
                return "\u7535\u6d41";
            default:
                return "\u672a\u914d\u7f6e\u72b6\u6001";
        }
    }

    private static string GetStatusLabel(EnemySkill_SO.HealingEffectEntry effect)
    {
        if (effect.statusData != null)
            return effect.statusData.DisplayName;

        switch (effect.statusType)
        {
            case EnemySkill_SO.DiceStatusType.Burn:
                return "\u71c3\u70e7";
            case EnemySkill_SO.DiceStatusType.Electro:
                return "\u7535\u6d41";
            default:
                return "\u672a\u914d\u7f6e\u72b6\u6001";
        }
    }

    private static string GetStatusLabel(EnemySkill_SO.StatusStackOperationEffectEntry effect)
    {
        if (effect.statusData != null)
            return effect.statusData.DisplayName;

        switch (effect.statusType)
        {
            case EnemySkill_SO.DiceStatusType.Burn:
                return "\u71c3\u70e7";
            case EnemySkill_SO.DiceStatusType.Electro:
                return "\u7535\u6d41";
            default:
                return "\u672a\u914d\u7f6e\u72b6\u6001";
        }
    }

    private static string BuildTargetPrioritySummary(EnemySkill_SO skill)
    {
        if (skill == null || skill.targetPriorityMode == EnemySkill_SO.TargetPriorityMode.None)
            return string.Empty;

        if (skill.targetPriorityMode == EnemySkill_SO.TargetPriorityMode.PreferTargetsWithStatusAtLeast)
        {
            string statusLabel = GetTargetPriorityStatusLabel(skill);
            int stacks = Mathf.Max(1, skill.targetPriorityRequiredStacks);
            return $"\n目标偏好：优先攻击具有 {stacks} 层 {statusLabel} 的目标";
        }

        return string.Empty;
    }

    private static string GetTargetPriorityStatusLabel(EnemySkill_SO skill)
    {
        if (skill == null)
            return "未配置状态";

        if (skill.targetPriorityStatusData != null)
            return skill.targetPriorityStatusData.DisplayName;

        switch (skill.targetPriorityStatusType)
        {
            case EnemySkill_SO.DiceStatusType.Burn:
                return "\u71c3\u70e7";
            case EnemySkill_SO.DiceStatusType.Electro:
                return "\u7535\u6d41";
            default:
                return "\u672a\u914d\u7f6e\u72b6\u6001";
        }
    }

    private static string GetHealingConditionLabel(EnemySkill_SO.HealingEffectEntry effect)
    {
        switch (effect.condition)
        {
            case EnemySkill_SO.PowerModifierCondition.SelfSpeedHigherThanTarget:
                return $"自身速度 高出 目标单位速度 {Mathf.Max(1, effect.requiredConditionSpeedDifference)} 点（含）";
            case EnemySkill_SO.PowerModifierCondition.SelfSpeedLowerThanTarget:
                return $"自身速度 低于 目标单位速度 {Mathf.Max(1, effect.requiredConditionSpeedDifference)} 点（含）";
            case EnemySkill_SO.PowerModifierCondition.SelfSpeedEqualToTarget:
                return "\u81ea\u8eab\u901f\u5ea6 \u7b49\u4e8e \u76ee\u6807\u5355\u4f4d\u901f\u5ea6";
            case EnemySkill_SO.PowerModifierCondition.SelfSpeedHigherThanValue:
                return $"自身速度 高于 {effect.requiredConditionSpeedValue}（含）";
            case EnemySkill_SO.PowerModifierCondition.SelfSpeedLowerThanValue:
                return $"自身速度 低于 {effect.requiredConditionSpeedValue}（含）";
            case EnemySkill_SO.PowerModifierCondition.SelfHasStatus:
                return $"自身拥有 {GetHealingConditionStatusLabel(effect)}";
            case EnemySkill_SO.PowerModifierCondition.SelfStatusStacksGreaterThan:
                return $"自身 {GetHealingConditionStatusLabel(effect)} 层数 高于 {Mathf.Max(0, effect.requiredConditionStatusStacks)}";
            case EnemySkill_SO.PowerModifierCondition.SelfStatusStacksGreaterOrEqual:
                return $"自身 {GetHealingConditionStatusLabel(effect)} 层数 至少为 {Mathf.Max(1, effect.requiredConditionStatusStacks)}";
            case EnemySkill_SO.PowerModifierCondition.SelfStatusStacksLessThan:
                return $"自身 {GetHealingConditionStatusLabel(effect)} 层数 低于 {Mathf.Max(1, effect.requiredConditionStatusStacks)}";
            case EnemySkill_SO.PowerModifierCondition.SelfStatusStacksLessOrEqual:
                return $"自身 {GetHealingConditionStatusLabel(effect)} 层数 至多为 {Mathf.Max(0, effect.requiredConditionStatusStacks)}";
            case EnemySkill_SO.PowerModifierCondition.TargetHasStatus:
                return $"目标拥有 {GetHealingConditionStatusLabel(effect)}";
            case EnemySkill_SO.PowerModifierCondition.TargetStatusStacksAtLeast:
                return $"目标拥有至少 {Mathf.Max(1, effect.requiredConditionStatusStacks)} 层 {GetHealingConditionStatusLabel(effect)}";
            case EnemySkill_SO.PowerModifierCondition.TargetStatusStacksPerThreshold:
                return $"目标每拥有 {Mathf.Max(1, effect.requiredConditionStatusStacks)} 层 {GetHealingConditionStatusLabel(effect)}";
            default:
                return "\u672a\u914d\u7f6e\u6761\u4ef6";
        }
    }

    private static string GetHealingConditionStatusLabel(EnemySkill_SO.HealingEffectEntry effect)
    {
        if (effect.conditionStatusData != null)
            return effect.conditionStatusData.DisplayName;

        if (effect.statusData != null)
            return effect.statusData.DisplayName;

        switch (effect.conditionStatusType)
        {
            case EnemySkill_SO.DiceStatusType.Burn:
                return "\u71c3\u70e7";
            case EnemySkill_SO.DiceStatusType.Electro:
                return "\u7535\u6d41";
            default:
                return "\u672a\u914d\u7f6e\u72b6\u6001";
        }
    }

    private static string GetPowerModifierConditionLabel(EnemySkill_SO.ConditionalPowerModifierEntry effect)
    {
        switch (effect.condition)
        {
            case EnemySkill_SO.PowerModifierCondition.SelfSpeedHigherThanTarget:
                return $"自身速度 高出 目标单位速度 {Mathf.Max(1, effect.requiredSpeedDifference)} 点（含）";
            case EnemySkill_SO.PowerModifierCondition.SelfSpeedLowerThanTarget:
                return $"自身速度 低于 目标单位速度 {Mathf.Max(1, effect.requiredSpeedDifference)} 点（含）";
            case EnemySkill_SO.PowerModifierCondition.SelfSpeedEqualToTarget:
                return "\u81ea\u8eab\u901f\u5ea6 \u7b49\u4e8e \u76ee\u6807\u5355\u4f4d\u901f\u5ea6";
            case EnemySkill_SO.PowerModifierCondition.SelfSpeedHigherThanValue:
                return $"自身速度 高于 {effect.requiredSpeedValue}（含）";
            case EnemySkill_SO.PowerModifierCondition.SelfSpeedLowerThanValue:
                return $"自身速度 低于 {effect.requiredSpeedValue}（含）";
            case EnemySkill_SO.PowerModifierCondition.SelfHasStatus:
                return $"自身拥有 {GetPowerModifierStatusLabel(effect)}";
            case EnemySkill_SO.PowerModifierCondition.SelfStatusStacksGreaterThan:
                return $"自身 {GetPowerModifierStatusLabel(effect)} 层数 高于 {Mathf.Max(0, effect.requiredStatusStacks)}";
            case EnemySkill_SO.PowerModifierCondition.SelfStatusStacksGreaterOrEqual:
                return $"自身 {GetPowerModifierStatusLabel(effect)} 层数 至少为 {Mathf.Max(1, effect.requiredStatusStacks)}";
            case EnemySkill_SO.PowerModifierCondition.SelfStatusStacksLessThan:
                return $"自身 {GetPowerModifierStatusLabel(effect)} 层数 低于 {Mathf.Max(1, effect.requiredStatusStacks)}";
            case EnemySkill_SO.PowerModifierCondition.SelfStatusStacksLessOrEqual:
                return $"自身 {GetPowerModifierStatusLabel(effect)} 层数 至多为 {Mathf.Max(0, effect.requiredStatusStacks)}";
            case EnemySkill_SO.PowerModifierCondition.TargetHasStatus:
                return $"目标拥有 {GetPowerModifierStatusLabel(effect)}";
            case EnemySkill_SO.PowerModifierCondition.TargetStatusStacksAtLeast:
                return $"目标拥有至少 {Mathf.Max(1, effect.requiredStatusStacks)} 层 {GetPowerModifierStatusLabel(effect)}";
            case EnemySkill_SO.PowerModifierCondition.TargetStatusStacksPerThreshold:
                return $"目标每拥有 {Mathf.Max(1, effect.requiredStatusStacks)} 层 {GetPowerModifierStatusLabel(effect)}";
            case EnemySkill_SO.PowerModifierCondition.TargetSkillIsSpecific:
                return $"与技能 {GetPowerModifierTargetSkillLabel(effect)} 拼点";
            default:
                return "\u672a\u914d\u7f6e\u6761\u4ef6";
        }
    }

    private static string GetPowerModifierStatusLabel(EnemySkill_SO.ConditionalPowerModifierEntry effect)
    {
        if (effect.targetStatusData != null)
            return effect.targetStatusData.DisplayName;

        switch (effect.targetStatusType)
        {
            case EnemySkill_SO.DiceStatusType.Burn:
                return "燃烧";
            case EnemySkill_SO.DiceStatusType.Electro:
                return "电流";
            default:
                return "未配置状态";
        }
    }

    private static string GetPowerModifierTargetSkillLabel(EnemySkill_SO.ConditionalPowerModifierEntry effect)
    {
        return effect.targetSkillData != null && !string.IsNullOrWhiteSpace(effect.targetSkillData.skillName)
            ? effect.targetSkillData.skillName
            : "未配置技能";
    }

    private static string GetPowerModifierTriggerLabel(EnemySkill_SO.DiceEffectTrigger trigger)
    {
        switch (trigger)
        {
            case EnemySkill_SO.DiceEffectTrigger.OnHit:
                return "[ 命中时 ]";
            case EnemySkill_SO.DiceEffectTrigger.OnMiss:
                return "[ 未击中时 ]";
            case EnemySkill_SO.DiceEffectTrigger.OnClash:
                return "[ \u62fc\u70b9\u65f6 ]";
            case EnemySkill_SO.DiceEffectTrigger.OnSkillUse:
                return "[ \u4f7f\u7528\u8be5\u6280\u80fd\u65f6 ]";
            case EnemySkill_SO.DiceEffectTrigger.OnClashWinAttack:
                return "[ \u62fc\u70b9\u80dc\u5229\u8ffd\u51fb\u65f6 ]";
            case EnemySkill_SO.DiceEffectTrigger.OnSingleAttack:
                return "[ 单方面攻击时 ]";
            default:
                return $"[ {trigger} ]";
        }
    }

    private static string GetGeneralEffectTriggerLabel(EnemySkill_SO.DiceEffectTrigger trigger)
    {
        switch (trigger)
        {
            case EnemySkill_SO.DiceEffectTrigger.OnHit:
                return "[ \u547d\u4e2d\u65f6 ]";
            case EnemySkill_SO.DiceEffectTrigger.OnMiss:
                return "[ \u672a\u51fb\u4e2d\u65f6 ]";
            case EnemySkill_SO.DiceEffectTrigger.OnSkillUse:
                return "[ \u4f7f\u7528\u65f6 ]";
            case EnemySkill_SO.DiceEffectTrigger.OnClash:
                return "[ \u62fc\u70b9\u65f6 ]";
            case EnemySkill_SO.DiceEffectTrigger.OnClashWinAttack:
                return "[ \u62fc\u70b9\u80dc\u5229\u8ffd\u51fb\u65f6 ]";
            case EnemySkill_SO.DiceEffectTrigger.OnSingleAttack:
                return "[ 单方面攻击时 ]";
            default:
                return $"[ {trigger} ]";
        }
    }
}
