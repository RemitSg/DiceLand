using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class AbilityMenuOverlay : MonoBehaviour
{
    private const string FallbackFontName = "LegacyRuntime.ttf";
    private const float TreeViewportPadding = 34f;
    private static readonly Vector2 NodeButtonSize = new Vector2(252f, 108f);
    private static readonly Color Gold = new Color(0.91f, 0.71f, 0.36f);
    private static readonly Color Ivory = new Color(0.96f, 0.9f, 0.74f);

    private readonly Dictionary<string, Button> nodeButtons = new Dictionary<string, Button>(StringComparer.Ordinal);
    private readonly Dictionary<string, Text> nodeTexts = new Dictionary<string, Text>(StringComparer.Ordinal);
    private readonly List<ConnectionView> connectionLines = new List<ConnectionView>();
    private readonly Dictionary<string, Image> nodeFrames = new Dictionary<string, Image>(StringComparer.Ordinal);
    private readonly Dictionary<string, Image> nodeGlows = new Dictionary<string, Image>(StringComparer.Ordinal);

    private Canvas canvas;
    private RectTransform designRoot;
    private RectTransform treeRoot;
    private RectTransform treeContent;
    private RectTransform detailPanel;
    private Text pointsText;
    private Text titleText;
    private Text detailNameText;
    private Text detailDescriptionText;
    private Text detailCostText;
    private Text detailEffectsText;
    private Text statusText;
    private Button unlockButton;
    private Text unlockButtonText;
    private Button closeButton;
    private Text treeProgressText;
    private Text zoomText;
    private Text closeButtonText;
    private Image unlockFrame;
    private Sprite nodeFrameSprite;
    private Sprite panelFrameSprite;
    private float treeZoom = 1f;
    private string feedbackMessage;
    private bool feedbackSuccess;

    private AbilityTreeConfig_SO config;
    private AbilityProgressionService service;
    private AbilityPointWallet wallet;
    private Action closedHandler;
    private string selectedNodeId;
    private Font titleFontOverride;
    private Font bodyFontOverride;
    private Font buttonFontOverride;
    private Vector2 treeContentOffset;

    public bool IsVisible => canvas != null && canvas.gameObject.activeSelf;
    public Canvas PresentationCanvas => canvas;

    public void ConfigureFonts(Font titleFont, Font bodyFont, Font buttonFont)
    {
        titleFontOverride = titleFont;
        bodyFontOverride = bodyFont;
        buttonFontOverride = buttonFont;
        ApplyFonts();
    }

    public void Show(AbilityTreeConfig_SO abilityConfig, AbilityPointWallet wallet, Action onClosed, string progressionPath = null)
    {
        EnsureUi();
        config = abilityConfig;
        this.wallet = wallet ?? new AbilityPointWallet();
        service = new AbilityProgressionService(config, this.wallet, progressionPath ?? AbilityProgressionService.DefaultPath);
        closedHandler = onClosed;
        selectedNodeId = FindEntryNodeId();
        feedbackMessage = null;
        treeZoom = 1f;
        treeContentOffset = Vector2.zero;

        canvas.gameObject.SetActive(true);
        UpdateSafeArea();
        Canvas.ForceUpdateCanvases();
        RebuildTree();
        FocusEntry();
        Refresh();
    }

    public void Hide()
    {
        if (!MainMenuPageMotion.TryHide(canvas, HideImmediately)) HideImmediately();
    }

    private void HideImmediately()
    {
        if (!IsVisible)
            return;
        canvas.gameObject.SetActive(false);
        closedHandler?.Invoke();
    }

    private void LateUpdate()
    {
        if (IsVisible)
            UpdateSafeArea();
    }

    private void OnDestroy()
    {
        if (canvas != null)
            DestroyUiObject(canvas.gameObject);
    }

    private void UpdateSafeArea()
    {
        if (designRoot == null || canvas == null || Screen.width <= 0 || Screen.height <= 0)
            return;
        Rect safe = Screen.safeArea;
        if (safe.width <= 0f || safe.height <= 0f)
            safe = new Rect(0f, 0f, Screen.width, Screen.height);
        float canvasScale = Mathf.Max(0.001f, canvas.scaleFactor);
        designRoot.localScale = Vector3.one * Mathf.Min(safe.width / (1920f * canvasScale), safe.height / (1080f * canvasScale));
        designRoot.anchoredPosition = (safe.center - new Vector2(Screen.width, Screen.height) * 0.5f) / canvasScale;
    }

    private void EnsureUi()
    {
        if (canvas != null)
            return;

        Font titleFont = ResolveTitleFont();
        Font bodyFont = ResolveBodyFont();
        Font buttonFont = ResolveButtonFont();
        nodeFrameSprite = Resources.Load<Sprite>("AbilityMenu/GildedNodeFrame");
        panelFrameSprite = Resources.Load<Sprite>("AbilityMenu/GildedPanelFrame");

        GameObject canvasGo = new GameObject("AbilityMenuOverlay", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9400;

        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform rootRect = canvasGo.GetComponent<RectTransform>();
        Stretch(rootRect);

        Image blocker = canvasGo.AddComponent<Image>();
        blocker.color = new Color(0.025f, 0.045f, 0.055f, 1f);
        RectTransform background = CreateRect("ForestSanctuary", rootRect, Vector2.zero, Vector2.zero);
        Stretch(background);
        RawImage art = background.gameObject.AddComponent<RawImage>();
        art.texture = Resources.Load<Texture2D>("AbilityMenu/ForestSanctuary");
        art.raycastTarget = false;
        AspectRatioFitter cover = background.gameObject.AddComponent<AspectRatioFitter>();
        cover.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        cover.aspectRatio = art.texture != null ? (float)art.texture.width / art.texture.height : 16f / 9f;
        RectTransform shade = CreateRect("AtmosphereShade", rootRect, Vector2.zero, Vector2.zero);
        Stretch(shade);
        shade.gameObject.AddComponent<Image>().color = new Color(0.01f, 0.025f, 0.04f, 0.2f);

        designRoot = CreateRect("SafeAreaComposition", rootRect, Vector2.zero, new Vector2(1920f, 1080f));
        RectTransform panel = designRoot;
        AddOrnament("TitleSigil", panel, new Vector2(-846f, 439f), new Vector2(110f, 110f), AbilityMenuOrnament.Shape.Tree, new Color(1f, 0.73f, 0.28f, 0.95f));
        titleText = CreateText("Title", panel, new Vector2(-620f, 444f), new Vector2(300f, 96f), "能力", 72, titleFont, TextAnchor.MiddleLeft, new Color(1f, 0.81f, 0.4f));
        AddOrnament("TitleRule", panel, new Vector2(-600f, 393f), new Vector2(590f, 18f), AbilityMenuOrnament.Shape.Divider, Gold);
        RectTransform pointsPanel = CreateFramedPanel("PointsPanel", panel, new Vector2(668f, 438f), new Vector2(448f, 80f), new Color(0.035f, 0.042f, 0.036f, 0.95f), false);
        pointsText = CreateText("Points", pointsPanel, Vector2.zero, new Vector2(390f, 55f), "当前能力点  0", 31, bodyFont, TextAnchor.MiddleCenter, Ivory);

        RectTransform treeFrame = CreateFramedPanel("TreeFrame", panel, new Vector2(-266f, -7f), new Vector2(1328f, 748f), new Color(0.022f, 0.044f, 0.059f, 0.985f), true);
        AddOrnament("AncestralTree", treeFrame, new Vector2(25f, 24f), new Vector2(655f, 655f), AbilityMenuOrnament.Shape.Tree, new Color(0.57f, 0.58f, 0.43f, 0.115f));
        treeRoot = CreateRect("TreeRoot", treeFrame, new Vector2(0f, 10f), new Vector2(1262f, 636f));
        Image treeImage = treeRoot.gameObject.AddComponent<Image>();
        treeImage.color = Color.clear;
        treeRoot.gameObject.AddComponent<RectMask2D>();
        AddTreePanInput(treeRoot.gameObject);

        treeContent = CreateRect("TreeContent", treeRoot, Vector2.zero, Vector2.zero);
        treeProgressText = CreateText("TreeProgress", treeFrame, new Vector2(-438f, -329f), new Vector2(310f, 34f), string.Empty, 20, bodyFont, TextAnchor.MiddleLeft, new Color(0.71f, 0.74f, 0.66f));
        CreateSmallButton("FocusEntry", treeFrame, new Vector2(250f, -329f), new Vector2(130f, 46f), "定位起点", FocusEntry);
        CreateSmallButton("ZoomOut", treeFrame, new Vector2(354f, -329f), new Vector2(56f, 46f), "−", () => SetTreeZoom(treeZoom - 0.15f));
        zoomText = CreateText("Zoom", treeFrame, new Vector2(427f, -329f), new Vector2(80f, 38f), "100%", 20, bodyFont, TextAnchor.MiddleCenter, Gold);
        CreateSmallButton("ZoomIn", treeFrame, new Vector2(500f, -329f), new Vector2(56f, 46f), "+", () => SetTreeZoom(treeZoom + 0.15f));
        CreateText("PanHint", panel, new Vector2(-266f, -423f), new Vector2(1100f, 38f), "拖动浏览能力树  ·  点击节点查看详情", 22, bodyFont, TextAnchor.MiddleCenter, new Color(0.89f, 0.87f, 0.72f));

        detailPanel = CreateFramedPanel("DetailPanel", panel, new Vector2(668f, -7f), new Vector2(448f, 748f), new Color(0.065f, 0.057f, 0.031f, 0.985f), true);
        detailNameText = CreateText("DetailName", detailPanel, new Vector2(0f, 271f), new Vector2(354f, 92f), string.Empty, 40, titleFont, TextAnchor.MiddleCenter, new Color(1f, 0.81f, 0.4f));
        detailNameText.resizeTextForBestFit = true;
        detailNameText.resizeTextMinSize = 28;
        detailNameText.resizeTextMaxSize = 40;
        AddOrnament("NameDivider", detailPanel, new Vector2(0f, 204f), new Vector2(344f, 12f), AbilityMenuOrnament.Shape.Divider, Gold);
        detailDescriptionText = CreateScrollText("Description", detailPanel, new Vector2(0f, 122f), new Vector2(338f, 112f), 29, bodyFont, Ivory);
        AddOrnament("CostDivider", detailPanel, new Vector2(0f, 24f), new Vector2(344f, 12f), AbilityMenuOrnament.Shape.Divider, Gold);
        detailCostText = CreateText("DetailCost", detailPanel, new Vector2(0f, -14f), new Vector2(350f, 52f), string.Empty, 29, bodyFont, TextAnchor.MiddleCenter, new Color(1f, 0.8f, 0.37f));
        detailEffectsText = CreateScrollText("Effects", detailPanel, new Vector2(0f, -109f), new Vector2(338f, 112f), 27, bodyFont, new Color(0.77f, 0.89f, 0.66f));
        AddOrnament("StatusDivider", detailPanel, new Vector2(0f, -183f), new Vector2(344f, 12f), AbilityMenuOrnament.Shape.Divider, Gold);
        statusText = CreateText("Status", detailPanel, new Vector2(0f, -221f), new Vector2(344f, 62f), string.Empty, 23, bodyFont, TextAnchor.MiddleCenter, new Color(0.77f, 0.75f, 0.65f));

        unlockButton = CreateButton("UnlockButton", detailPanel, new Vector2(0f, -299f), new Vector2(366f, 84f), "解锁", buttonFont, out unlockButtonText);
        unlockButtonText.fontSize = 36;
        unlockFrame = unlockButton.transform.Find("Frame").GetComponent<Image>();
        unlockButton.onClick.AddListener(HandleUnlockClicked);

        closeButton = CreateButton("CloseButton", panel, new Vector2(668f, -449f), new Vector2(448f, 82f), "返回主菜单", buttonFont, out closeButtonText);
        closeButtonText.fontSize = 34;
        closeButton.onClick.AddListener(Hide);

        ApplyFonts();
        canvas.gameObject.SetActive(false);
    }

    private void RebuildTree()
    {
        ClearTree();
        if (config == null || config.nodes == null)
            return;

        Dictionary<string, AbilityNodeDefinition> lookup = BuildLookup();
        for (int i = 0; i < config.nodes.Count; i++)
        {
            AbilityNodeDefinition node = config.nodes[i];
            if (node == null || string.IsNullOrWhiteSpace(node.nodeId) || node.prerequisiteNodeIds == null)
                continue;

            for (int prerequisiteIndex = 0; prerequisiteIndex < node.prerequisiteNodeIds.Count; prerequisiteIndex++)
            {
                string prerequisiteId = node.prerequisiteNodeIds[prerequisiteIndex];
                AbilityNodeDefinition prerequisite;
                if (lookup.TryGetValue(prerequisiteId, out prerequisite))
                    connectionLines.Add(CreateConnectionLine(prerequisite, node));
            }
        }

        for (int i = 0; i < config.nodes.Count; i++)
        {
            AbilityNodeDefinition node = config.nodes[i];
            if (node == null || string.IsNullOrWhiteSpace(node.nodeId))
                continue;

            Button button = CreateNodeButton(node);
            nodeButtons[node.nodeId] = button;
            Text text = button.GetComponentInChildren<Text>();
            if (text != null)
                nodeTexts[node.nodeId] = text;
        }

        ClampTreeOffset();
        ApplyTreeContentOffset();
    }

    private void Refresh()
    {
        if (service == null)
            return;

        int points = wallet != null ? wallet.GetCurrentPoints() : 0;
        pointsText.text = $"当前能力点  <color=#FFD478><size=39>{points}</size></color>";
        if (config == null || config.nodes == null || config.nodes.Count == 0)
        {
            detailNameText.text = "未配置能力树";
            detailDescriptionText.text = "请创建 AbilityTreeConfig，并在主菜单控制器中引用。";
            detailCostText.text = string.Empty;
            detailEffectsText.text = string.Empty;
            statusText.text = string.Empty;
            unlockButton.interactable = false;
            treeProgressText.text = "暂无能力节点";
            return;
        }

        int unlockedCount = 0;
        var states = new Dictionary<string, AbilityNodeState>(StringComparer.Ordinal);
        for (int i = 0; i < config.nodes.Count; i++)
        {
            AbilityNodeDefinition node = config.nodes[i];
            if (node == null || string.IsNullOrWhiteSpace(node.nodeId))
                continue;

            AbilityNodeState state = service.GetNodeState(node.nodeId);
            states[node.nodeId] = state;
            if (state == AbilityNodeState.Unlocked)
                unlockedCount++;
            Button button;
            if (nodeButtons.TryGetValue(node.nodeId, out button))
                ApplyNodeState(button, state, node.nodeId == selectedNodeId);

            Text text;
            if (nodeTexts.TryGetValue(node.nodeId, out text))
                text.text = BuildNodeLabel(node, state, service);
        }

        treeProgressText.text = $"已解锁  {unlockedCount} / {nodeButtons.Count}";
        foreach (ConnectionView connection in connectionLines)
        {
            bool fromUnlocked = states.TryGetValue(connection.fromId, out AbilityNodeState fromState) && fromState == AbilityNodeState.Unlocked;
            bool toUnlocked = states.TryGetValue(connection.toId, out AbilityNodeState toState) && toState == AbilityNodeState.Unlocked;
            Color lineColor = fromUnlocked ? new Color(1f, 0.72f, 0.3f, toUnlocked ? 1f : 0.7f) : new Color(0.52f, 0.46f, 0.31f, 0.45f);
            connection.line.color = lineColor;
            connection.glow.color = new Color(lineColor.r, lineColor.g, lineColor.b, fromUnlocked ? 0.13f : 0.025f);
            connection.gem.color = lineColor;
        }
        RefreshDetail();
    }

    private void RefreshDetail()
    {
        AbilityNodeDefinition node = config != null ? config.FindNode(selectedNodeId) : null;
        if (node == null)
        {
            selectedNodeId = FindFirstNodeId();
            node = config != null ? config.FindNode(selectedNodeId) : null;
        }

        if (node == null)
            return;

        AbilityNodeState state = service.GetNodeState(node.nodeId);
        detailNameText.text = string.IsNullOrWhiteSpace(node.displayName) ? node.nodeId : node.displayName;
        detailDescriptionText.text = string.IsNullOrWhiteSpace(node.description) ? "暂无描述" : node.description;
        detailDescriptionText.rectTransform.anchoredPosition = Vector2.zero;
        detailEffectsText.rectTransform.anchoredPosition = Vector2.zero;
        int cost = Mathf.Max(0, node.cost);
        int points = wallet != null ? wallet.GetCurrentPoints() : 0;
        bool affordable = points >= cost;
        detailCostText.text = state == AbilityNodeState.Unlocked ? "已解锁能力" : $"消耗能力点  {cost}";
        detailEffectsText.text = BuildEffectDescription(node);
        statusText.text = !string.IsNullOrEmpty(feedbackMessage) ? feedbackMessage : state == AbilityNodeState.Available && !affordable ? $"能力点不足，还需要 {cost - points} 点。" : ResolveStateText(state);
        statusText.color = !string.IsNullOrEmpty(feedbackMessage) && feedbackSuccess ? new Color(0.74f, 0.9f, 0.59f) : state == AbilityNodeState.Available && !affordable ? new Color(0.85f, 0.65f, 0.43f) : new Color(0.77f, 0.75f, 0.65f);
        unlockButton.interactable = (state == AbilityNodeState.Available && affordable) || (state == AbilityNodeState.Unlocked && node.isToggleable);
        unlockButtonText.text = state == AbilityNodeState.Unlocked ? "已解锁" : "解锁";
        if (state == AbilityNodeState.Unlocked && node.isToggleable)
        {
            unlockButtonText.text = service.IsNodeEnabled(node.nodeId) ? "关闭" : "启用";
            if (string.IsNullOrEmpty(feedbackMessage))
                statusText.text = service.IsNodeEnabled(node.nodeId) ? "该能力已启用。" : "该能力已关闭，可重新启用。";
        }
        unlockFrame.color = unlockButton.interactable ? new Color(1f, 0.9f, 0.64f) : new Color(0.52f, 0.49f, 0.37f);
        unlockButtonText.color = unlockButton.interactable ? Ivory : new Color(0.66f, 0.63f, 0.53f);
    }

    private void HandleUnlockClicked()
    {
        if (service == null || string.IsNullOrWhiteSpace(selectedNodeId))
            return;

        AbilityNodeDefinition node = config != null ? config.FindNode(selectedNodeId) : null;
        AbilityUnlockResult result = node != null && node.isToggleable && service.GetNodeState(selectedNodeId) == AbilityNodeState.Unlocked
            ? service.SetNodeEnabled(selectedNodeId, !service.IsNodeEnabled(selectedNodeId))
            : service.TryUnlock(selectedNodeId);
        feedbackMessage = result.message;
        feedbackSuccess = result.success;
        Refresh();
    }

    private Button CreateNodeButton(AbilityNodeDefinition node)
    {
        Button button = CreateButton(
            $"AbilityNode_{node.nodeId}",
            treeContent != null ? treeContent : treeRoot,
            ViewPosition(node.position),
            NodeButtonSize,
            string.IsNullOrWhiteSpace(node.displayName) ? node.nodeId : node.displayName,
            ResolveBodyFont(),
            out Text text);
        text.fontSize = 29;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 21;
        text.resizeTextMaxSize = 29;
        text.lineSpacing = 0.94f;
        nodeFrames[node.nodeId] = button.transform.Find("Frame").GetComponent<Image>();
        RectTransform glow = CreateRect("SelectionGlow", button.transform, Vector2.zero, NodeButtonSize + new Vector2(12f, 12f));
        Image glowImage = glow.gameObject.AddComponent<Image>();
        glowImage.sprite = nodeFrameSprite;
        glowImage.type = Image.Type.Sliced;
        glowImage.pixelsPerUnitMultiplier = 5.5f;
        glowImage.raycastTarget = false;
        glowImage.color = Color.clear;
        Outline glowOutline = glow.gameObject.AddComponent<Outline>();
        glowOutline.effectColor = new Color(1f, 0.65f, 0.2f, 0.8f);
        glowOutline.effectDistance = new Vector2(3f, -3f);
        nodeGlows[node.nodeId] = glowImage;
        glow.SetAsFirstSibling();
        AddTreePanInput(button.gameObject);
        string capturedId = node.nodeId;
        button.onClick.AddListener(() =>
        {
            selectedNodeId = capturedId;
            feedbackMessage = null;
            Refresh();
        });
        return button;
    }

    private ConnectionView CreateConnectionLine(AbilityNodeDefinition prerequisite, AbilityNodeDefinition node)
    {
        Vector2 from = ViewPosition(prerequisite.position);
        Vector2 to = ViewPosition(node.position);
        RectTransform line = CreateRect("ConnectionLine", treeContent != null ? treeContent : treeRoot, (from + to) * 0.5f, new Vector2(Vector2.Distance(from, to), 4f));
        Image image = line.gameObject.AddComponent<Image>();
        image.raycastTarget = false;
        Vector2 direction = to - from;
        line.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        RectTransform glow = CreateRect("LineGlow", line, Vector2.zero, new Vector2(line.sizeDelta.x, 13f));
        Image glowImage = glow.gameObject.AddComponent<Image>();
        glowImage.raycastTarget = false;
        AbilityMenuOrnament gem = AddOrnament("ConnectionGem", line, Vector2.zero, new Vector2(20f, 20f), AbilityMenuOrnament.Shape.Diamond, Gold);
        return new ConnectionView { fromId = prerequisite.nodeId, toId = node.nodeId, line = image, glow = glowImage, gem = gem };
    }

    private static Vector2 ViewPosition(Vector2 position) => new Vector2(position.x * 1.22f, position.y * 1.07f);

    private void ClampTreeOffset()
    {
        Rect bounds = CalculateTreeContentBounds();
        bounds = Rect.MinMaxRect(bounds.xMin * treeZoom, bounds.yMin * treeZoom, bounds.xMax * treeZoom, bounds.yMax * treeZoom);
        treeContentOffset = AbilityTreeViewportMath.ClampOffset(treeContentOffset, bounds, treeRoot.rect.size, TreeViewportPadding);
    }

    private void FocusEntry()
    {
        if (treeRoot == null || config == null)
            return;
        AbilityNodeDefinition entry = config.FindNode(FindEntryNodeId());
        if (entry == null)
            return;
        Vector2 entryPosition = ViewPosition(entry.position) * treeZoom;
        treeContentOffset = new Vector2(-treeRoot.rect.width * 0.5f + NodeButtonSize.x * treeZoom * 0.5f + 46f - entryPosition.x, -entryPosition.y);
        ClampTreeOffset();
        ApplyTreeContentOffset();
    }

    private void SetTreeZoom(float value)
    {
        float next = Mathf.Clamp(value, 0.55f, 1.3f);
        treeContentOffset *= next / treeZoom;
        treeZoom = next;
        treeContent.localScale = Vector3.one * treeZoom;
        zoomText.text = $"{Mathf.RoundToInt(treeZoom * 100f)}%";
        ClampTreeOffset();
        ApplyTreeContentOffset();
    }

    private Dictionary<string, AbilityNodeDefinition> BuildLookup()
    {
        Dictionary<string, AbilityNodeDefinition> lookup = new Dictionary<string, AbilityNodeDefinition>(StringComparer.Ordinal);
        if (config == null || config.nodes == null)
            return lookup;

        for (int i = 0; i < config.nodes.Count; i++)
        {
            AbilityNodeDefinition node = config.nodes[i];
            if (node != null && !string.IsNullOrWhiteSpace(node.nodeId) && !lookup.ContainsKey(node.nodeId))
                lookup.Add(node.nodeId, node);
        }

        return lookup;
    }

    private Rect CalculateTreeContentBounds()
    {
        if (config == null || config.nodes == null || config.nodes.Count == 0)
            return new Rect(0f, 0f, 0f, 0f);

        bool hasNode = false;
        float minX = 0f;
        float minY = 0f;
        float maxX = 0f;
        float maxY = 0f;
        Vector2 halfNode = NodeButtonSize * 0.5f;

        for (int i = 0; i < config.nodes.Count; i++)
        {
            AbilityNodeDefinition node = config.nodes[i];
            if (node == null || string.IsNullOrWhiteSpace(node.nodeId))
                continue;

            Vector2 position = ViewPosition(node.position);
            float nodeMinX = position.x - halfNode.x;
            float nodeMaxX = position.x + halfNode.x;
            float nodeMinY = position.y - halfNode.y;
            float nodeMaxY = position.y + halfNode.y;
            if (!hasNode)
            {
                minX = nodeMinX;
                maxX = nodeMaxX;
                minY = nodeMinY;
                maxY = nodeMaxY;
                hasNode = true;
                continue;
            }

            minX = Mathf.Min(minX, nodeMinX);
            maxX = Mathf.Max(maxX, nodeMaxX);
            minY = Mathf.Min(minY, nodeMinY);
            maxY = Mathf.Max(maxY, nodeMaxY);
        }

        if (!hasNode)
            return new Rect(0f, 0f, 0f, 0f);

        return Rect.MinMaxRect(minX, minY, maxX, maxY);
    }

    private void PanTree(Vector2 screenDelta)
    {
        if (treeContent == null || treeRoot == null)
            return;

        float scaleFactor = canvas != null ? Mathf.Max(0.01f, canvas.scaleFactor * designRoot.localScale.x) : 1f;
        treeContentOffset += screenDelta / scaleFactor;
        ClampTreeOffset();
        ApplyTreeContentOffset();
    }

    private void ApplyTreeContentOffset()
    {
        if (treeContent != null)
            treeContent.anchoredPosition = treeContentOffset;
    }

    private void AddTreePanInput(GameObject target)
    {
        AbilityTreePanInput input = target.GetComponent<AbilityTreePanInput>();
        if (input == null)
            input = target.AddComponent<AbilityTreePanInput>();

        input.Initialize(this);
    }

    private void ClearTree()
    {
        nodeButtons.Clear();
        nodeTexts.Clear();
        connectionLines.Clear();
        nodeFrames.Clear();
        nodeGlows.Clear();

        if (treeContent == null)
            return;

        for (int i = treeContent.childCount - 1; i >= 0; i--)
        {
            GameObject child = treeContent.GetChild(i).gameObject;
            child.SetActive(false);
            DestroyUiObject(child);
        }
        treeContent.localScale = Vector3.one;
        if (zoomText != null)
            zoomText.text = "100%";
    }

    private string FindEntryNodeId()
    {
        if (config != null && config.nodes != null)
            foreach (AbilityNodeDefinition node in config.nodes)
                if (node != null && !string.IsNullOrWhiteSpace(node.nodeId) && (node.prerequisiteNodeIds == null || node.prerequisiteNodeIds.Count == 0))
                    return node.nodeId;
        return FindFirstNodeId();
    }

    private string FindFirstNodeId()
    {
        if (config == null || config.nodes == null)
            return null;

        for (int i = 0; i < config.nodes.Count; i++)
        {
            AbilityNodeDefinition node = config.nodes[i];
            if (node != null && !string.IsNullOrWhiteSpace(node.nodeId))
                return node.nodeId;
        }

        return null;
    }

    private static string BuildNodeLabel(AbilityNodeDefinition node, AbilityNodeState state, AbilityProgressionService service)
    {
        string name = string.IsNullOrWhiteSpace(node.displayName) ? node.nodeId : node.displayName;
        string suffix = state == AbilityNodeState.Unlocked ? "已解锁" : $"{Mathf.Max(0, node.cost)} 点";
        if (state == AbilityNodeState.Unlocked && node.isToggleable && service != null)
            suffix = service.IsNodeEnabled(node.nodeId) ? "已启用" : "已关闭";
        return $"{name}\n<size=23>{suffix}</size>";
    }

    private static string BuildEffectDescription(AbilityNodeDefinition node)
    {
        if (node == null || node.effects == null || node.effects.Count == 0)
            return "无效果";

        List<string> lines = new List<string>();
        for (int i = 0; i < node.effects.Count; i++)
        {
            AbilityNodeEffect effect = node.effects[i];
            if (effect == null)
                continue;

            switch (effect.effectType)
            {
                case AbilityEffectType.StartingMoneyFlat:
                    lines.Add($"初始金币 +{effect.intValue}");
                    break;
                case AbilityEffectType.StartingActionPointLimitFlat:
                    lines.Add($"初始行动点上限 +{effect.intValue}");
                    break;
                case AbilityEffectType.StartingBlueprint:
                    lines.Add($"初始随机图纸 x{Mathf.Max(1, effect.intValue)}");
                    break;
                case AbilityEffectType.StartingLootTableRoll:
                    lines.Add($"初始随机掉落 x{Mathf.Max(1, effect.intValue)}");
                    break;
                case AbilityEffectType.MerchantFreeRefreshPerShop:
                    lines.Add($"每个商店免费刷新 +{Mathf.Max(0, effect.intValue)}");
                    break;
                case AbilityEffectType.FirstMerchantPurchaseHalfDiscount:
                    lines.Add("每局首次购买半价");
                    break;
                case AbilityEffectType.BattleStartTemporaryEnergy:
                    lines.Add($"战斗开始临时能量 +{Mathf.Max(0, effect.intValue)}");
                    break;
                case AbilityEffectType.BattleStartEnergyMaxFlat:
                    lines.Add($"战斗开始能量上限 +{Mathf.Max(0, effect.intValue)}");
                    break;
                case AbilityEffectType.BattleStartMaxHealthFlat:
                    lines.Add($"战斗开始最大生命 +{Mathf.Max(0, effect.intValue)}");
                    break;
                case AbilityEffectType.StartingPartySizeFlat:
                    lines.Add($"初始队伍人数 +{Mathf.Max(0, effect.intValue)}");
                    break;
                case AbilityEffectType.ExtraStartingCompanion:
                    lines.Add("选择额外初始队友");
                    break;
                case AbilityEffectType.TreasureRewardTableOverride:
                    lines.Add("替换宝藏奖励表");
                    break;
                case AbilityEffectType.StartingWeaponReplacement:
                    lines.Add("替换主角初始武器");
                    break;
                case AbilityEffectType.RelicChoiceRefreshCount:
                    lines.Add($"遗物选择刷新 +{Mathf.Max(0, effect.intValue)}");
                    break;
            }
        }

        return lines.Count > 0 ? string.Join("\n", lines) : "无效果";
    }

    private static string ResolveStateText(AbilityNodeState state)
    {
        switch (state)
        {
            case AbilityNodeState.Unlocked:
                return "该能力已经生效。";
            case AbilityNodeState.Available:
                return "前置条件已满足，可以解锁。";
            default:
                return "需要先解锁前置能力。";
        }
    }

    private void ApplyNodeState(Button button, AbilityNodeState state, bool selected)
    {
        Image image = button.GetComponent<Image>();
        if (image == null)
            return;

        if (selected)
            image.color = new Color(0.4f, 0.28f, 0.08f, 0.98f);
        else if (state == AbilityNodeState.Unlocked)
            image.color = new Color(0.12f, 0.26f, 0.14f, 1f);
        else if (state == AbilityNodeState.Available)
            image.color = new Color(0.18f, 0.16f, 0.12f, 1f);
        else
            image.color = new Color(0.045f, 0.05f, 0.046f, 1f);
        string id = button.name.Substring("AbilityNode_".Length);
        bool enabled = state != AbilityNodeState.Unlocked || service.IsNodeEnabled(id);
        nodeFrames[id].color = selected ? new Color(1f, 0.85f, 0.46f) : state == AbilityNodeState.Unlocked && enabled ? new Color(0.78f, 0.93f, 0.59f) : state == AbilityNodeState.Available ? new Color(0.97f, 0.84f, 0.58f) : new Color(0.56f, 0.54f, 0.44f);
        nodeGlows[id].color = selected ? new Color(1f, 0.66f, 0.2f, 0.36f) : Color.clear;
        nodeGlows[id].enabled = selected;
        if (nodeTexts.TryGetValue(id, out Text label))
            label.color = selected ? new Color(1f, 0.94f, 0.73f) : state == AbilityNodeState.Locked ? new Color(0.67f, 0.66f, 0.58f) : Ivory;
    }

    private void ApplyFonts()
    {
        if (titleText != null)
            titleText.font = ResolveTitleFont();
        if (pointsText != null)
            pointsText.font = ResolveBodyFont();
        if (detailNameText != null)
            detailNameText.font = ResolveTitleFont();
        if (detailDescriptionText != null)
            detailDescriptionText.font = ResolveBodyFont();
        if (detailCostText != null)
            detailCostText.font = ResolveBodyFont();
        if (detailEffectsText != null)
            detailEffectsText.font = ResolveBodyFont();
        if (statusText != null)
            statusText.font = ResolveBodyFont();
        if (unlockButtonText != null)
            unlockButtonText.font = ResolveButtonFont();
        if (closeButtonText != null)
            closeButtonText.font = ResolveButtonFont();
        foreach (Text label in nodeTexts.Values)
            label.font = ResolveBodyFont();
    }

    private Font ResolveTitleFont()
    {
        return titleFontOverride != null ? titleFontOverride : ResolveBodyFont();
    }

    private Font ResolveBodyFont()
    {
        return bodyFontOverride != null ? bodyFontOverride : Resources.GetBuiltinResource<Font>(FallbackFontName);
    }

    private Font ResolveButtonFont()
    {
        return buttonFontOverride != null ? buttonFontOverride : ResolveBodyFont();
    }

    private static RectTransform CreateRect(string name, Transform parent, Vector2 anchoredPosition, Vector2 size)
    {
        return CreateRect(name, parent, new Vector2(0.5f, 0.5f), anchoredPosition, size);
    }

    private static RectTransform CreateRect(string name, Transform parent, Vector2 anchor, Vector2 anchoredPosition, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        return rect;
    }

    private static Text CreateText(string name, Transform parent, Vector2 anchoredPosition, Vector2 size, string value, int fontSize, Font font, TextAnchor anchor, Color color)
    {
        RectTransform rect = CreateRect(name, parent, anchoredPosition, size);
        Text text = rect.gameObject.AddComponent<ResourceKeywordLegacyText>();
        text.text = value;
        text.font = font;
        text.fontSize = fontSize;
        text.alignment = anchor;
        text.color = color;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        Shadow shadow = text.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.8f);
        shadow.effectDistance = new Vector2(1f, -2f);
        return text;
    }

    private Button CreateButton(string name, Transform parent, Vector2 anchoredPosition, Vector2 size, string label, Font font, out Text text)
    {
        RectTransform rect = CreateRect(name, parent, anchoredPosition, size);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = new Color(0.17f, 0.12f, 0.055f, 0.98f);
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(1.2f, 1.13f, 0.9f);
        colors.pressedColor = new Color(0.74f, 0.67f, 0.47f);
        colors.disabledColor = new Color(0.55f, 0.52f, 0.43f);
        button.colors = colors;
        AddFrame(rect, size, false);

        text = CreateText("Text", rect, Vector2.zero, size - new Vector2(54f, 20f), label, 26, font, TextAnchor.MiddleCenter, Ivory);
        return button;
    }

    private RectTransform CreateFramedPanel(string name, Transform parent, Vector2 position, Vector2 size, Color fill, bool large)
    {
        RectTransform rect = CreateRect(name, parent, position, size);
        rect.gameObject.AddComponent<Image>().color = fill;
        AddFrame(rect, size, large);
        return rect;
    }

    private void AddFrame(RectTransform parent, Vector2 size, bool large)
    {
        RectTransform rect = CreateRect("Frame", parent, Vector2.zero, size);
        Image frame = rect.gameObject.AddComponent<Image>();
        frame.sprite = large ? panelFrameSprite : nodeFrameSprite;
        frame.type = Image.Type.Sliced;
        frame.pixelsPerUnitMultiplier = large ? 2.5f : 5.5f;
        frame.raycastTarget = false;
        frame.color = Color.white;
        if (frame.sprite == null)
            frame.color = new Color(Gold.r, Gold.g, Gold.b, 0.15f);
    }

    private void CreateSmallButton(string name, Transform parent, Vector2 position, Vector2 size, string label, Action action)
    {
        Button button = CreateButton(name, parent, position, size, label, ResolveBodyFont(), out Text text);
        text.fontSize = 20;
        text.rectTransform.sizeDelta = size - new Vector2(12f, 4f);
        button.onClick.AddListener(() => action());
    }

    private static AbilityMenuOrnament AddOrnament(string name, Transform parent, Vector2 position, Vector2 size, AbilityMenuOrnament.Shape shape, Color tint)
    {
        RectTransform rect = CreateRect(name, parent, position, size);
        AbilityMenuOrnament ornament = rect.gameObject.AddComponent<AbilityMenuOrnament>();
        ornament.kind = shape;
        ornament.color = tint;
        ornament.raycastTarget = false;
        return ornament;
    }

    private static Text CreateScrollText(string name, Transform parent, Vector2 position, Vector2 size, int fontSize, Font font, Color tint)
    {
        RectTransform viewport = CreateRect(name + "Viewport", parent, position, size);
        viewport.gameObject.AddComponent<Image>().color = Color.clear;
        viewport.gameObject.AddComponent<RectMask2D>();
        ScrollRect scroll = viewport.gameObject.AddComponent<ScrollRect>();
        Text text = CreateText(name + "Text", viewport, Vector2.zero, size, string.Empty, fontSize, font, TextAnchor.UpperLeft, tint);
        text.rectTransform.anchorMin = new Vector2(0f, 1f);
        text.rectTransform.anchorMax = new Vector2(1f, 1f);
        text.rectTransform.pivot = new Vector2(0.5f, 1f);
        text.rectTransform.sizeDelta = new Vector2(0f, size.y);
        ContentSizeFitter fitter = text.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.viewport = viewport;
        scroll.content = text.rectTransform;
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 22f;
        return text;
    }

    private static void DestroyUiObject(GameObject target)
    {
        if (Application.isPlaying)
            Destroy(target);
        else
            DestroyImmediate(target);
    }

    private sealed class ConnectionView
    {
        public string fromId;
        public string toId;
        public Image line;
        public Image glow;
        public AbilityMenuOrnament gem;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private sealed class AbilityTreePanInput : MonoBehaviour, IBeginDragHandler, IDragHandler, IScrollHandler
    {
        private AbilityMenuOverlay owner;

        public void Initialize(AbilityMenuOverlay overlay)
        {
            owner = overlay;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData == null)
                return;

            eventData.eligibleForClick = false;
            owner?.PanTree(eventData.delta);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData == null)
                return;

            eventData.eligibleForClick = false;
            owner?.PanTree(eventData.delta);
        }

        public void OnScroll(PointerEventData eventData)
        {
            if (eventData == null)
                return;

            owner?.PanTree(new Vector2(eventData.scrollDelta.x, eventData.scrollDelta.y) * 48f);
        }
    }
}
