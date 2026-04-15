using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class RestSceneRecruitInteractable : MonoBehaviour
{
    private const string OutlineShaderResourcePath = "Shaders/RestSceneSolidOutline";
    private const string OutlineShaderName = "MobileGame/RestScene/SolidSpriteOutline";
    private const string OutlineWidthPropertyName = "_OutlineWidth";

    [System.Serializable]
    private sealed class RecruitCardView
    {
        public RectTransform root;
        public Image backgroundImage;
        public Image iconImage;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI weaponText;
        public TextMeshProUGUI hpText;
        public TextMeshProUGUI descriptionText;
        public TextMeshProUGUI priceText;
        public Button recruitButton;
        public TextMeshProUGUI recruitButtonText;
        public CharacterData_SO boundCharacter;
    }

    [Header("Outline")]
    [SerializeField] private Color outlineColor = new Color(0.45f, 0.9f, 1f, 1f);
    [FormerlySerializedAs("outlineScale")]
    [SerializeField] private float outlineWidth = 1.25f;
    [SerializeField] private Vector3 outlineOffset = Vector3.zero;
    [SerializeField] private int outlineSortingOrderOffset = -1;

    [Header("Panel")]
    [SerializeField] private Vector2 panelSize = new Vector2(1160f, 660f);
    [SerializeField] private Vector2 panelOffset = Vector2.zero;
    [SerializeField] private Sprite panelBackgroundSprite;
    [SerializeField] private Color panelBackgroundColor = new Color(0f, 0f, 0f, 0.9f);
    [SerializeField] private Sprite panelButtonSprite;
    [SerializeField] private Sprite cardBackgroundSprite;
    [SerializeField] private Vector2 cardSize = new Vector2(460f, 420f);
    [SerializeField] private float cardSpacing = 36f;
    [SerializeField] private Color cardBackgroundColor = new Color(0.16f, 0.12f, 0.09f, 0.96f);
    [SerializeField] private Sprite fallbackPortraitSprite;

    [Header("Buttons")]
    [SerializeField] private Sprite recruitButtonSprite;
    [SerializeField] private Vector2 recruitButtonSize = new Vector2(176f, 62f);
    [SerializeField] private Sprite closeButtonSprite;
    [SerializeField] private Vector2 closeButtonSize = new Vector2(150f, 56f);
    [SerializeField] private Color primaryButtonColor = new Color(1f, 0.84f, 0.36f, 1f);
    [SerializeField] private Color disabledButtonColor = new Color(0.42f, 0.42f, 0.42f, 0.9f);
    [SerializeField] private Color buttonTextColor = Color.black;

    [Header("Font")]
    [SerializeField] private TMP_FontAsset titleFont;
    [SerializeField] private TMP_FontAsset bodyFont;
    [SerializeField] private TMP_FontAsset buttonFont;
    [SerializeField] private int titleFontSize = 42;
    [SerializeField] private int bodyFontSize = 28;
    [SerializeField] private int noteFontSize = 24;
    [SerializeField] private int buttonFontSize = 26;

    [Header("Text")]
    [SerializeField] private string titleText = "\u62db\u52df";
    [SerializeField] private string closeButtonLabel = "\u5173\u95ed";
    [SerializeField] private string recruitButtonLabel = "\u62db\u52df";
    [SerializeField] private string noRecruitLabel = "\u6682\u65e0\u53ef\u62db\u52df\u89d2\u8272";
    [SerializeField] private string partyFullLabel = "\u961f\u4f0d\u5df2\u6ee1\uff0c\u65e0\u6cd5\u7ee7\u7eed\u62db\u52df";
    [SerializeField] private string goldFormat = "\u5f53\u524d\u91d1\u5e01\uff1a{0}";
    [SerializeField] private string costFormat = "\u62db\u52df\u8d39\u7528\uff1a{0}";
    [SerializeField] private string hpFormat = "\u57fa\u7840\u751f\u547d\uff1a{0}";
    [SerializeField] private string weaponFormat = "\u9002\u914d\u6b66\u5668\uff1a{0}";
    [SerializeField] private string insufficientGoldLabel = "\u91d1\u5e01\u4e0d\u8db3";
    [SerializeField] private string missingDescriptionLabel = "\u6682\u65e0\u89d2\u8272\u63cf\u8ff0\u3002";
    [SerializeField] private Color titleColor = Color.white;
    [SerializeField] private Color bodyColor = new Color(0.95f, 0.95f, 0.95f, 1f);
    [SerializeField] private Color noteColor = new Color(1f, 0.84f, 0.36f, 1f);
    [SerializeField] private Color errorColor = new Color(1f, 0.45f, 0.45f, 1f);

    private Canvas canvas;
    private RectTransform panelRect;
    private Image panelImage;
    private TextMeshProUGUI goldText;
    private TextMeshProUGUI costText;
    private TextMeshProUGUI statusText;
    private TextMeshProUGUI emptyText;
    private Button closeButton;
    private readonly List<RecruitCardView> cardViews = new List<RecruitCardView>();

    private SpriteRenderer targetSpriteRenderer;
    private SpriteRenderer outlineRenderer;
    private Material runtimeOutlineMaterial;
    private Collider2D interactionCollider;
    private Camera inputCamera;
    private bool mouseHovering;
    private bool touchHighlighting;
    private bool ownsModalLock;

    private void Awake()
    {
        interactionCollider = GetComponent<Collider2D>();
        inputCamera = Camera.main;
        EnsureOutlineRenderer();
        EnsurePanel();
        SetPanelVisible(false);
        RefreshHighlightState();
    }

    private void OnEnable()
    {
        RefreshHighlightState();
    }

    private void OnDisable()
    {
        mouseHovering = false;
        touchHighlighting = false;
        ReleaseModalLock();
        RefreshHighlightState();
    }

    private void OnDestroy()
    {
        ReleaseModalLock();
        if (runtimeOutlineMaterial == null)
            return;

        if (Application.isPlaying)
            Destroy(runtimeOutlineMaterial);
        else
            DestroyImmediate(runtimeOutlineMaterial);

        runtimeOutlineMaterial = null;
    }

    private void Update()
    {
        UpdateTouchHighlight();
        RefreshOutlineRendererVisuals();
    }

    private void OnMouseEnter()
    {
        if (RestSceneModalState.IsLocked && !RestSceneModalState.IsLockedBy(this))
            return;

        mouseHovering = true;
        RefreshHighlightState();
    }

    private void OnMouseExit()
    {
        mouseHovering = false;
        RefreshHighlightState();
    }

    private void OnMouseDown()
    {
        if (RestSceneModalState.IsLocked && !RestSceneModalState.IsLockedBy(this))
            return;

        mouseHovering = true;
        RefreshHighlightState();
    }

    private void OnMouseUpAsButton()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;
        if (RestSceneModalState.IsLocked && !RestSceneModalState.IsLockedBy(this))
            return;

        if (!RestSceneRuntime.HasPendingVisit)
            return;

        ShowPanel();
    }

    public void ShowPanel()
    {
        if (!RestSceneModalState.TryAcquire(this))
            return;

        ownsModalLock = true;
        EnsurePanel();
        RefreshPanelContent();
        SetPanelVisible(true);
    }

    public void HidePanel()
    {
        SetPanelVisible(false);
    }

    private void RefreshPanelContent()
    {
        IReadOnlyList<CharacterData_SO> candidates = RestSceneRuntime.GetRecruitCandidates();
        int recruitCost = RestSceneRuntime.GetRecruitCost();
        int currentGold = 0;
        int currentRecruitCount = 0;
        int maxRecruitCount = RestSceneRuntime.DifficultyConfig != null ? Mathf.Max(1, RestSceneRuntime.DifficultyConfig.maxRecruitCount) : 4;

        if (RestSceneRuntime.TryGetCurrentSnapshot(out MapRunSnapshot snapshot) && snapshot?.runState != null)
        {
            currentGold = Mathf.Max(0, snapshot.runState.money);
            currentRecruitCount = Mathf.Max(0, snapshot.runState.recruitedMemberCount);
        }

        if (goldText != null)
            goldText.text = string.Format(goldFormat, currentGold);
        if (costText != null)
            costText.text = string.Format(costFormat, recruitCost);
        if (statusText != null)
        {
            statusText.color = noteColor;
            statusText.text = string.Empty;
        }

        bool recruitLimitReached = currentRecruitCount >= maxRecruitCount;
        bool hasCandidates = candidates != null && candidates.Count > 0 && !recruitLimitReached;
        if (emptyText != null)
        {
            emptyText.gameObject.SetActive(!hasCandidates);
            emptyText.text = recruitLimitReached ? partyFullLabel : noRecruitLabel;
        }

        for (int i = 0; i < cardViews.Count; i++)
        {
            RecruitCardView card = cardViews[i];
            if (card == null)
                continue;

            CharacterData_SO character = hasCandidates && i < candidates.Count ? candidates[i] : null;
            BindCard(card, character, recruitCost, currentGold);
        }
    }

    private void OnRecruitClicked(RecruitCardView card)
    {
        if (card == null || card.boundCharacter == null)
            return;

        if (!RestSceneRuntime.TryRecruitAndReturn(card.boundCharacter, out string errorMessage))
        {
            RefreshPanelContent();
            if (statusText != null)
            {
                statusText.color = errorColor;
                statusText.text = errorMessage;
            }
        }
    }

    private void BindCard(RecruitCardView card, CharacterData_SO character, int recruitCost, int currentGold)
    {
        if (card == null || card.root == null)
            return;

        card.boundCharacter = character;
        bool active = character != null;
        card.root.gameObject.SetActive(active);
        if (!active)
            return;

        if (card.iconImage != null)
        {
            card.iconImage.sprite = character.icon != null ? character.icon : fallbackPortraitSprite;
            card.iconImage.preserveAspect = true;
        }

        if (card.nameText != null)
            card.nameText.text = string.IsNullOrWhiteSpace(character.characterName) ? character.name : character.characterName;
        if (card.weaponText != null)
            card.weaponText.text = string.Format(weaponFormat, GetWeaponTypeLabel(character.compatibleWeaponType));
        if (card.hpText != null)
            card.hpText.text = string.Format(hpFormat, Mathf.Max(1, character.baseHP));
        if (card.descriptionText != null)
            card.descriptionText.text = string.IsNullOrWhiteSpace(character.basicDescription) ? missingDescriptionLabel : character.basicDescription;
        if (card.priceText != null)
            card.priceText.text = recruitCost > currentGold ? $"{string.Format(costFormat, recruitCost)}  {insufficientGoldLabel}" : string.Format(costFormat, recruitCost);

        bool canRecruit = currentGold >= recruitCost;
        if (card.recruitButton != null)
        {
            card.recruitButton.interactable = canRecruit;
            Image image = card.recruitButton.GetComponent<Image>();
            if (image != null)
            {
                image.color = canRecruit ? primaryButtonColor : disabledButtonColor;
                image.sprite = recruitButtonSprite;
                image.type = recruitButtonSprite != null ? Image.Type.Sliced : Image.Type.Simple;
            }
        }

        if (card.recruitButtonText != null)
        {
            card.recruitButtonText.text = recruitButtonLabel;
            card.recruitButtonText.color = buttonTextColor;
        }
    }

    private void EnsurePanel()
    {
        EventSystemUtility.EnsureSingleEventSystem();
        if (canvas != null)
            return;

        GameObject canvasObject = new GameObject("RestRecruitCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);

        canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 3020;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        canvasRect.anchorMin = Vector2.zero;
        canvasRect.anchorMax = Vector2.one;
        canvasRect.offsetMin = Vector2.zero;
        canvasRect.offsetMax = Vector2.zero;

        panelImage = CreateImage("Panel", canvasRect, out panelRect);
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = panelOffset;
        panelRect.sizeDelta = panelSize;
        panelImage.sprite = panelBackgroundSprite;
        panelImage.type = panelBackgroundSprite != null ? Image.Type.Sliced : Image.Type.Simple;
        panelImage.color = panelBackgroundSprite != null ? Color.white : panelBackgroundColor;

        TextMeshProUGUI title = CreateText("Title", panelRect, titleFont, titleFontSize, titleColor, TextAlignmentOptions.Center);
        RectTransform titleRect = title.rectTransform;
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -28f);
        titleRect.sizeDelta = new Vector2(320f, 60f);
        title.text = titleText;

        goldText = CreateText("GoldText", panelRect, bodyFont != null ? bodyFont : titleFont, noteFontSize, noteColor, TextAlignmentOptions.Left);
        RectTransform goldRect = goldText.rectTransform;
        goldRect.anchorMin = new Vector2(0f, 1f);
        goldRect.anchorMax = new Vector2(0f, 1f);
        goldRect.pivot = new Vector2(0f, 1f);
        goldRect.anchoredPosition = new Vector2(36f, -34f);
        goldRect.sizeDelta = new Vector2(280f, 40f);

        costText = CreateText("CostText", panelRect, bodyFont != null ? bodyFont : titleFont, noteFontSize, noteColor, TextAlignmentOptions.Right);
        RectTransform costRect = costText.rectTransform;
        costRect.anchorMin = new Vector2(1f, 1f);
        costRect.anchorMax = new Vector2(1f, 1f);
        costRect.pivot = new Vector2(1f, 1f);
        costRect.anchoredPosition = new Vector2(-36f, -34f);
        costRect.sizeDelta = new Vector2(320f, 40f);

        RectTransform cardsRoot = new GameObject("CardsRoot", typeof(RectTransform)).GetComponent<RectTransform>();
        cardsRoot.SetParent(panelRect, false);
        cardsRoot.anchorMin = new Vector2(0.5f, 0.5f);
        cardsRoot.anchorMax = new Vector2(0.5f, 0.5f);
        cardsRoot.pivot = new Vector2(0.5f, 0.5f);
        cardsRoot.anchoredPosition = new Vector2(0f, -14f);
        cardsRoot.sizeDelta = new Vector2(cardSize.x * 2f + cardSpacing, cardSize.y);

        CreateRecruitCard(cardsRoot, new Vector2(-(cardSize.x * 0.5f + cardSpacing * 0.5f), 0f));
        CreateRecruitCard(cardsRoot, new Vector2(cardSize.x * 0.5f + cardSpacing * 0.5f, 0f));

        emptyText = CreateText("EmptyText", panelRect, bodyFont != null ? bodyFont : titleFont, bodyFontSize, bodyColor, TextAlignmentOptions.Center);
        RectTransform emptyRect = emptyText.rectTransform;
        emptyRect.anchorMin = new Vector2(0.5f, 0.5f);
        emptyRect.anchorMax = new Vector2(0.5f, 0.5f);
        emptyRect.pivot = new Vector2(0.5f, 0.5f);
        emptyRect.anchoredPosition = new Vector2(0f, -10f);
        emptyRect.sizeDelta = new Vector2(panelSize.x - 80f, 80f);

        statusText = CreateText("StatusText", panelRect, bodyFont != null ? bodyFont : titleFont, noteFontSize, noteColor, TextAlignmentOptions.Center);
        RectTransform statusRect = statusText.rectTransform;
        statusRect.anchorMin = new Vector2(0.5f, 0f);
        statusRect.anchorMax = new Vector2(0.5f, 0f);
        statusRect.pivot = new Vector2(0.5f, 0f);
        statusRect.anchoredPosition = new Vector2(0f, 92f);
        statusRect.sizeDelta = new Vector2(panelSize.x - 80f, 34f);

        closeButton = CreateButton("CloseButton", panelRect, closeButtonSprite, closeButtonLabel, out RectTransform closeRect);
        closeRect.anchorMin = new Vector2(0.5f, 0f);
        closeRect.anchorMax = new Vector2(0.5f, 0f);
        closeRect.pivot = new Vector2(0.5f, 0f);
        closeRect.anchoredPosition = new Vector2(0f, 24f);
        closeRect.sizeDelta = closeButtonSize;
        closeButton.onClick.AddListener(HidePanel);

        ApplyFonts();
    }

    private void CreateRecruitCard(RectTransform parent, Vector2 anchoredPosition)
    {
        RecruitCardView card = new RecruitCardView();
        card.backgroundImage = CreateImage("RecruitCard", parent, out card.root);
        card.root.anchorMin = new Vector2(0.5f, 0.5f);
        card.root.anchorMax = new Vector2(0.5f, 0.5f);
        card.root.pivot = new Vector2(0.5f, 0.5f);
        card.root.anchoredPosition = anchoredPosition;
        card.root.sizeDelta = cardSize;
        card.backgroundImage.sprite = cardBackgroundSprite;
        card.backgroundImage.type = cardBackgroundSprite != null ? Image.Type.Sliced : Image.Type.Simple;
        card.backgroundImage.color = cardBackgroundSprite != null ? Color.white : cardBackgroundColor;

        card.iconImage = CreateImage("Portrait", card.root, out RectTransform portraitRect);
        portraitRect.anchorMin = new Vector2(0f, 1f);
        portraitRect.anchorMax = new Vector2(0f, 1f);
        portraitRect.pivot = new Vector2(0f, 1f);
        portraitRect.anchoredPosition = new Vector2(24f, -22f);
        portraitRect.sizeDelta = new Vector2(132f, 132f);

        card.nameText = CreateText("Name", card.root, titleFont, titleFontSize - 6, titleColor, TextAlignmentOptions.TopLeft);
        RectTransform nameRect = card.nameText.rectTransform;
        nameRect.anchorMin = new Vector2(0f, 1f);
        nameRect.anchorMax = new Vector2(0f, 1f);
        nameRect.pivot = new Vector2(0f, 1f);
        nameRect.anchoredPosition = new Vector2(176f, -20f);
        nameRect.sizeDelta = new Vector2(cardSize.x - 200f, 46f);

        card.weaponText = CreateText("WeaponText", card.root, bodyFont != null ? bodyFont : titleFont, noteFontSize, noteColor, TextAlignmentOptions.TopLeft);
        RectTransform weaponRect = card.weaponText.rectTransform;
        weaponRect.anchorMin = new Vector2(0f, 1f);
        weaponRect.anchorMax = new Vector2(0f, 1f);
        weaponRect.pivot = new Vector2(0f, 1f);
        weaponRect.anchoredPosition = new Vector2(176f, -78f);
        weaponRect.sizeDelta = new Vector2(cardSize.x - 200f, 34f);

        card.hpText = CreateText("HpText", card.root, bodyFont != null ? bodyFont : titleFont, noteFontSize, noteColor, TextAlignmentOptions.TopLeft);
        RectTransform hpRect = card.hpText.rectTransform;
        hpRect.anchorMin = new Vector2(0f, 1f);
        hpRect.anchorMax = new Vector2(0f, 1f);
        hpRect.pivot = new Vector2(0f, 1f);
        hpRect.anchoredPosition = new Vector2(176f, -112f);
        hpRect.sizeDelta = new Vector2(cardSize.x - 200f, 34f);

        card.descriptionText = CreateText("Description", card.root, bodyFont != null ? bodyFont : titleFont, bodyFontSize, bodyColor, TextAlignmentOptions.TopLeft);
        RectTransform descRect = card.descriptionText.rectTransform;
        descRect.anchorMin = new Vector2(0f, 1f);
        descRect.anchorMax = new Vector2(0f, 1f);
        descRect.pivot = new Vector2(0f, 1f);
        descRect.anchoredPosition = new Vector2(24f, -176f);
        descRect.sizeDelta = new Vector2(cardSize.x - 48f, 148f);
        card.descriptionText.enableWordWrapping = true;
        card.descriptionText.overflowMode = TextOverflowModes.Overflow;

        card.priceText = CreateText("PriceText", card.root, bodyFont != null ? bodyFont : titleFont, noteFontSize, noteColor, TextAlignmentOptions.TopLeft);
        RectTransform priceRect = card.priceText.rectTransform;
        priceRect.anchorMin = new Vector2(0f, 0f);
        priceRect.anchorMax = new Vector2(0f, 0f);
        priceRect.pivot = new Vector2(0f, 0f);
        priceRect.anchoredPosition = new Vector2(24f, 92f);
        priceRect.sizeDelta = new Vector2(cardSize.x - 48f, 34f);

        card.recruitButton = CreateButton("RecruitButton", card.root, recruitButtonSprite, recruitButtonLabel, out RectTransform recruitButtonRect);
        recruitButtonRect.anchorMin = new Vector2(0.5f, 0f);
        recruitButtonRect.anchorMax = new Vector2(0.5f, 0f);
        recruitButtonRect.pivot = new Vector2(0.5f, 0f);
        recruitButtonRect.anchoredPosition = new Vector2(0f, 20f);
        recruitButtonRect.sizeDelta = recruitButtonSize;
        card.recruitButtonText = card.recruitButton.GetComponentInChildren<TextMeshProUGUI>(true);
        RecruitCardView capturedCard = card;
        card.recruitButton.onClick.AddListener(() => OnRecruitClicked(capturedCard));

        cardViews.Add(card);
    }

    private void EnsureOutlineRenderer()
    {
        targetSpriteRenderer = GetComponent<SpriteRenderer>();
        if (targetSpriteRenderer == null)
            targetSpriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
        if (targetSpriteRenderer == null)
            return;

        Transform outlineTransform = targetSpriteRenderer.transform.Find("HoverOutline");
        if (outlineTransform != null)
            outlineRenderer = outlineTransform.GetComponent<SpriteRenderer>();

        if (outlineRenderer == null)
        {
            GameObject outlineObject = new GameObject("HoverOutline", typeof(SpriteRenderer));
            outlineObject.transform.SetParent(targetSpriteRenderer.transform, false);
            outlineRenderer = outlineObject.GetComponent<SpriteRenderer>();
        }

        ClearLegacyOutlineSegments(outlineRenderer.transform);
        outlineRenderer.enabled = false;
        outlineRenderer.sharedMaterial = EnsureOutlineMaterial();
        RefreshOutlineRendererVisuals();
    }

    private void ClearLegacyOutlineSegments(Transform root)
    {
        for (int childIndex = root.childCount - 1; childIndex >= 0; childIndex--)
        {
            Transform child = root.GetChild(childIndex);
            if (!child.name.StartsWith("Segment"))
                continue;

            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }
    }

    private void RefreshOutlineRendererVisuals()
    {
        if (targetSpriteRenderer == null || outlineRenderer == null)
            return;

        Material outlineMaterial = EnsureOutlineMaterial();
        outlineRenderer.sprite = targetSpriteRenderer.sprite;
        outlineRenderer.color = outlineColor;
        outlineRenderer.flipX = targetSpriteRenderer.flipX;
        outlineRenderer.flipY = targetSpriteRenderer.flipY;
        outlineRenderer.drawMode = targetSpriteRenderer.drawMode;
        outlineRenderer.size = targetSpriteRenderer.size;
        outlineRenderer.maskInteraction = targetSpriteRenderer.maskInteraction;
        outlineRenderer.sortingLayerID = targetSpriteRenderer.sortingLayerID;
        outlineRenderer.sortingOrder = targetSpriteRenderer.sortingOrder + outlineSortingOrderOffset;
        outlineRenderer.transform.localPosition = outlineOffset;
        outlineRenderer.transform.localRotation = Quaternion.identity;
        outlineRenderer.transform.localScale = Vector3.one;
        outlineRenderer.sharedMaterial = outlineMaterial;

        if (outlineMaterial != null)
            outlineMaterial.SetFloat(OutlineWidthPropertyName, Mathf.Max(0f, outlineWidth));
    }

    private Material EnsureOutlineMaterial()
    {
        if (runtimeOutlineMaterial != null)
            return runtimeOutlineMaterial;

        Shader shader = Resources.Load<Shader>(OutlineShaderResourcePath);
        if (shader == null)
            shader = Shader.Find(OutlineShaderName);

        if (shader == null)
        {
            Debug.LogWarning($"{nameof(RestSceneRecruitInteractable)} could not find outline shader '{OutlineShaderName}'.", this);
            return null;
        }

        runtimeOutlineMaterial = new Material(shader)
        {
            name = "Runtime_RestSceneRecruitOutlineMaterial"
        };
        runtimeOutlineMaterial.SetFloat(OutlineWidthPropertyName, Mathf.Max(0f, outlineWidth));
        return runtimeOutlineMaterial;
    }

    private void UpdateTouchHighlight()
    {
        bool shouldHighlight = false;
        if (interactionCollider != null && !IsPanelVisible() && (!RestSceneModalState.IsLocked || RestSceneModalState.IsLockedBy(this)))
        {
            inputCamera = inputCamera != null ? inputCamera : Camera.main;
            if (inputCamera != null && Input.touchCount > 0)
            {
                for (int i = 0; i < Input.touchCount; i++)
                {
                    Touch touch = Input.GetTouch(i);
                    if (touch.phase == TouchPhase.Canceled || touch.phase == TouchPhase.Ended)
                        continue;

                    Vector3 worldPoint = inputCamera.ScreenToWorldPoint(touch.position);
                    worldPoint.z = transform.position.z;
                    if (interactionCollider.OverlapPoint(worldPoint))
                    {
                        shouldHighlight = true;
                        break;
                    }
                }
            }
        }

        if (touchHighlighting == shouldHighlight)
            return;

        touchHighlighting = shouldHighlight;
        RefreshHighlightState();
    }

    private void RefreshHighlightState()
    {
        if (outlineRenderer == null)
            return;

        outlineRenderer.enabled = (mouseHovering || touchHighlighting) && !IsPanelVisible();
    }

    private bool IsPanelVisible()
    {
        return canvas != null && canvas.gameObject.activeSelf;
    }

    private void SetPanelVisible(bool visible)
    {
        if (canvas != null)
            canvas.gameObject.SetActive(visible);

        if (visible)
        {
            mouseHovering = false;
            touchHighlighting = false;
        }
        else
        {
            ReleaseModalLock();
        }

        RefreshHighlightState();
    }

    private void ReleaseModalLock()
    {
        if (!ownsModalLock)
            return;

        RestSceneModalState.Release(this);
        ownsModalLock = false;
    }

    private void ApplyFonts()
    {
        if (canvas == null)
            return;

        foreach (TMP_Text text in canvas.GetComponentsInChildren<TMP_Text>(true))
        {
            if (text == null)
                continue;

            if (text.name.Contains("Title") || text.name.Contains("Name"))
                text.font = titleFont != null ? titleFont : bodyFont;
            else if (text.name.Contains("Button") || text.name.Contains("Label"))
                text.font = buttonFont != null ? buttonFont : (bodyFont != null ? bodyFont : titleFont);
            else
                text.font = bodyFont != null ? bodyFont : titleFont;

            text.UpdateMeshPadding();
            text.SetAllDirty();
        }
    }

    private static Image CreateImage(string name, RectTransform parent, out RectTransform rect)
    {
        GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        Image image = imageObject.GetComponent<Image>();
        rect = imageObject.GetComponent<RectTransform>();
        return image;
    }

    private static TextMeshProUGUI CreateText(string name, RectTransform parent, TMP_FontAsset font, int fontSize, Color color, TextAlignmentOptions alignment)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        if (font != null)
            text.font = font;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
        return text;
    }

    private Button CreateButton(string name, RectTransform parent, Sprite sprite, string label, out RectTransform rect)
    {
        GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        Image image = buttonObject.GetComponent<Image>();
        image.sprite = sprite;
        image.type = sprite != null ? Image.Type.Sliced : Image.Type.Simple;
        image.color = primaryButtonColor;

        Button button = buttonObject.GetComponent<Button>();
        rect = buttonObject.GetComponent<RectTransform>();

        TextMeshProUGUI buttonText = CreateText("Label", rect, buttonFont != null ? buttonFont : bodyFont, buttonFontSize, buttonTextColor, TextAlignmentOptions.Center);
        RectTransform textRect = buttonText.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        buttonText.text = label;

        return button;
    }

    private static string GetWeaponTypeLabel(WeaponType weaponType)
    {
        switch (weaponType)
        {
            case WeaponType.Sword:
                return "\u5251";
            case WeaponType.Axe:
                return "\u65a7";
            case WeaponType.Bow:
                return "\u5f13";
            case WeaponType.Wand:
                return "\u6cd5\u6756";
            case WeaponType.Dagger:
                return "\u77ed\u5251";
            default:
                return weaponType.ToString();
        }
    }
}
