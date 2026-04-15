using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class RestSceneInteractable : MonoBehaviour
{
    private const string OutlineShaderResourcePath = "Shaders/RestSceneSolidOutline";
    private const string OutlineShaderName = "MobileGame/RestScene/SolidSpriteOutline";
    private const string OutlineWidthPropertyName = "_OutlineWidth";

    [Header("Outline")]
    [SerializeField] private Color outlineColor = new Color(1f, 0.9f, 0.35f, 1f);
    [FormerlySerializedAs("outlineScale")]
    [SerializeField] private float outlineWidth = 1.25f;
    [SerializeField] private Vector3 outlineOffset = Vector3.zero;
    [SerializeField] private int outlineSortingOrderOffset = -1;

    [Header("Prompt")]
    [SerializeField] private Vector2 promptSize = new Vector2(760f, 360f);
    [SerializeField] private Vector2 promptOffset = Vector2.zero;
    [SerializeField] private Sprite promptBackgroundSprite;
    [SerializeField] private Color promptBackgroundColor = new Color(0f, 0f, 0f, 0.88f);
    [SerializeField] private Sprite confirmButtonSprite;
    [SerializeField] private Sprite cancelButtonSprite;
    [SerializeField] private Vector2 buttonSize = new Vector2(220f, 82f);
    [SerializeField] private float buttonSpacing = 36f;

    [Header("Text")]
    [SerializeField] private TMP_FontAsset titleFont;
    [SerializeField] private TMP_FontAsset bodyFont;
    [SerializeField] private TMP_FontAsset buttonFont;
    [SerializeField] private string titleText = "休息";
    [SerializeField] private string bodyText = "确定要休息吗？\n休息将完全恢复所有角色的血量。";
    [SerializeField] private string confirmLabel = "确定";
    [SerializeField] private string cancelLabel = "取消";
    [SerializeField] private Color titleColor = Color.white;
    [SerializeField] private Color bodyColor = new Color(0.95f, 0.95f, 0.95f, 1f);
    [SerializeField] private Color buttonTextColor = Color.white;
    [SerializeField] private int titleFontSize = 44;
    [SerializeField] private int bodyFontSize = 30;
    [SerializeField] private int buttonFontSize = 32;

    private Canvas canvas;
    private RectTransform panelRect;
    private Image panelImage;
    private Button confirmButton;
    private Button cancelButton;
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
        EnsurePrompt();
        SetPromptVisible(false);
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

        ShowPrompt();
    }

    public void ShowPrompt()
    {
        if (!RestSceneModalState.TryAcquire(this))
            return;

        ownsModalLock = true;
        EnsurePrompt();
        SetPromptVisible(true);
    }

    public void HidePrompt()
    {
        SetPromptVisible(false);
    }

    private void HandleConfirmRest()
    {
        if (!RestSceneRuntime.HasPendingVisit)
        {
            HidePrompt();
            return;
        }

        SetPromptVisible(false);
        RestSceneRuntime.TryConfirmRestAndReturn();
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
            Debug.LogWarning($"{nameof(RestSceneInteractable)} could not find outline shader '{OutlineShaderName}'.", this);
            return null;
        }

        runtimeOutlineMaterial = new Material(shader)
        {
            name = "Runtime_RestSceneOutlineMaterial"
        };
        runtimeOutlineMaterial.SetFloat(OutlineWidthPropertyName, Mathf.Max(0f, outlineWidth));
        return runtimeOutlineMaterial;
    }

    private void UpdateTouchHighlight()
    {
        bool shouldHighlight = false;
        if (interactionCollider != null && !IsPromptVisible() && (!RestSceneModalState.IsLocked || RestSceneModalState.IsLockedBy(this)))
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

        bool visible = (mouseHovering || touchHighlighting) && !IsPromptVisible();
        outlineRenderer.enabled = visible;
    }

    private bool IsPromptVisible()
    {
        return canvas != null && canvas.gameObject.activeSelf;
    }

    private void EnsurePrompt()
    {
        EventSystemUtility.EnsureSingleEventSystem();
        if (canvas != null)
        {
            ApplyVisuals();
            return;
        }

        GameObject canvasObject = new GameObject("RestPromptCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);

        canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 3000;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        canvasRect.anchorMin = Vector2.zero;
        canvasRect.anchorMax = Vector2.one;
        canvasRect.offsetMin = Vector2.zero;
        canvasRect.offsetMax = Vector2.zero;

        panelImage = CreateImage("PromptPanel", canvasRect, out panelRect);
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);

        TextMeshProUGUI title = CreateText("Title", panelRect, titleFont, titleFontSize, titleColor, TextAlignmentOptions.Center);
        RectTransform titleRect = title.rectTransform;
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -34f);
        titleRect.sizeDelta = new Vector2(640f, 70f);
        title.text = titleText;

        TextMeshProUGUI body = CreateText("Body", panelRect, bodyFont != null ? bodyFont : titleFont, bodyFontSize, bodyColor, TextAlignmentOptions.Center);
        body.enableWordWrapping = true;
        RectTransform bodyRect = body.rectTransform;
        bodyRect.anchorMin = new Vector2(0.5f, 0.5f);
        bodyRect.anchorMax = new Vector2(0.5f, 0.5f);
        bodyRect.pivot = new Vector2(0.5f, 0.5f);
        bodyRect.anchoredPosition = new Vector2(0f, 12f);
        bodyRect.sizeDelta = new Vector2(620f, 140f);
        body.text = bodyText;

        confirmButton = CreateButton("ConfirmButton", panelRect, confirmButtonSprite, confirmLabel, out RectTransform confirmRect);
        confirmRect.anchorMin = new Vector2(0.5f, 0f);
        confirmRect.anchorMax = new Vector2(0.5f, 0f);
        confirmRect.pivot = new Vector2(0.5f, 0f);

        cancelButton = CreateButton("CancelButton", panelRect, cancelButtonSprite, cancelLabel, out RectTransform cancelRect);
        cancelRect.anchorMin = new Vector2(0.5f, 0f);
        cancelRect.anchorMax = new Vector2(0.5f, 0f);
        cancelRect.pivot = new Vector2(0.5f, 0f);

        confirmButton.onClick.AddListener(HandleConfirmRest);
        cancelButton.onClick.AddListener(HidePrompt);

        ApplyVisuals();
    }

    private void ApplyVisuals()
    {
        if (panelRect == null || panelImage == null)
            return;

        panelRect.sizeDelta = promptSize;
        panelRect.anchoredPosition = promptOffset;
        panelImage.sprite = promptBackgroundSprite;
        panelImage.type = promptBackgroundSprite != null ? Image.Type.Sliced : Image.Type.Simple;
        panelImage.color = promptBackgroundColor;

        ApplyButtonLayout(confirmButton, new Vector2(-(buttonSize.x * 0.5f + buttonSpacing * 0.5f), 28f), confirmButtonSprite, confirmLabel);
        ApplyButtonLayout(cancelButton, new Vector2(buttonSize.x * 0.5f + buttonSpacing * 0.5f, 28f), cancelButtonSprite, cancelLabel);
    }

    private void ApplyButtonLayout(Button button, Vector2 anchoredPosition, Sprite sprite, string label)
    {
        if (button == null)
            return;

        RectTransform rect = button.GetComponent<RectTransform>();
        rect.sizeDelta = buttonSize;
        rect.anchoredPosition = anchoredPosition;

        Image image = button.GetComponent<Image>();
        image.sprite = sprite;
        image.type = sprite != null ? Image.Type.Sliced : Image.Type.Simple;

        TextMeshProUGUI labelText = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (labelText != null)
        {
            if (buttonFont != null)
                labelText.font = buttonFont;
            labelText.fontSize = buttonFontSize;
            labelText.color = buttonTextColor;
            labelText.text = label;
        }
    }

    private void SetPromptVisible(bool visible)
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
}
