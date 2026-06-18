using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class AbilityMenuOverlay : MonoBehaviour
{
    private const string FallbackFontName = "LegacyRuntime.ttf";
    private const float TreeViewportPadding = 80f;
    private static readonly Vector2 NodeButtonSize = new Vector2(210f, 86f);

    private readonly Dictionary<string, Button> nodeButtons = new Dictionary<string, Button>(StringComparer.Ordinal);
    private readonly Dictionary<string, Text> nodeTexts = new Dictionary<string, Text>(StringComparer.Ordinal);
    private readonly List<Image> connectionLines = new List<Image>();

    private Canvas canvas;
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

    public void ConfigureFonts(Font titleFont, Font bodyFont, Font buttonFont)
    {
        titleFontOverride = titleFont;
        bodyFontOverride = bodyFont;
        buttonFontOverride = buttonFont;
        ApplyFonts();
    }

    public void Show(AbilityTreeConfig_SO abilityConfig, AbilityPointWallet wallet, Action onClosed)
    {
        EnsureUi();
        config = abilityConfig;
        this.wallet = wallet ?? new AbilityPointWallet();
        service = new AbilityProgressionService(config, this.wallet, AbilityProgressionService.DefaultPath);
        closedHandler = onClosed;
        selectedNodeId = FindFirstNodeId();
        treeContentOffset = Vector2.zero;

        RebuildTree();
        Refresh();
        canvas.gameObject.SetActive(true);
    }

    public void Hide()
    {
        if (canvas != null)
            canvas.gameObject.SetActive(false);

        closedHandler?.Invoke();
    }

    private void EnsureUi()
    {
        if (canvas != null)
            return;

        Font titleFont = ResolveTitleFont();
        Font bodyFont = ResolveBodyFont();
        Font buttonFont = ResolveButtonFont();

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
        blocker.color = new Color(0.018f, 0.019f, 0.025f, 1f);

        RectTransform panel = CreateRect("Panel", rootRect, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1660f, 920f));
        Stretch(panel);
        Image panelImage = panel.gameObject.AddComponent<Image>();
        panelImage.color = new Color(0.035f, 0.038f, 0.052f, 1f);

        titleText = CreateText("Title", panel, new Vector2(-690f, 388f), new Vector2(360f, 68f), "能力", 48, titleFont, TextAnchor.MiddleLeft, new Color(1f, 0.84f, 0.36f, 1f));
        pointsText = CreateText("Points", panel, new Vector2(620f, 392f), new Vector2(340f, 52f), "当前能力点 0", 30, bodyFont, TextAnchor.MiddleRight, new Color(0.86f, 0.79f, 0.66f, 1f));

        treeRoot = CreateRect("TreeRoot", panel, new Vector2(-250f, -34f), new Vector2(1060f, 720f));
        Image treeImage = treeRoot.gameObject.AddComponent<Image>();
        treeImage.color = new Color(0.055f, 0.08f, 0.1f, 0.92f);
        treeRoot.gameObject.AddComponent<RectMask2D>();
        AddTreePanInput(treeRoot.gameObject);

        treeContent = CreateRect("TreeContent", treeRoot, Vector2.zero, Vector2.zero);

        detailPanel = CreateRect("DetailPanel", panel, new Vector2(600f, -34f), new Vector2(430f, 720f));
        Image detailImage = detailPanel.gameObject.AddComponent<Image>();
        detailImage.color = new Color(0.08f, 0.075f, 0.065f, 0.94f);

        detailNameText = CreateText("DetailName", detailPanel, new Vector2(0f, 282f), new Vector2(340f, 62f), string.Empty, 34, titleFont, TextAnchor.MiddleCenter, new Color(1f, 0.86f, 0.46f, 1f));
        detailDescriptionText = CreateText("DetailDescription", detailPanel, new Vector2(0f, 162f), new Vector2(340f, 130f), string.Empty, 24, bodyFont, TextAnchor.UpperLeft, new Color(0.88f, 0.86f, 0.78f, 1f));
        detailCostText = CreateText("DetailCost", detailPanel, new Vector2(0f, 46f), new Vector2(340f, 52f), string.Empty, 26, bodyFont, TextAnchor.MiddleCenter, new Color(1f, 0.82f, 0.35f, 1f));
        detailEffectsText = CreateText("DetailEffects", detailPanel, new Vector2(0f, -88f), new Vector2(340f, 120f), string.Empty, 24, bodyFont, TextAnchor.UpperLeft, new Color(0.82f, 0.9f, 0.78f, 1f));
        statusText = CreateText("Status", detailPanel, new Vector2(0f, -206f), new Vector2(340f, 54f), string.Empty, 22, bodyFont, TextAnchor.MiddleCenter, new Color(0.74f, 0.7f, 0.62f, 1f));

        unlockButton = CreateButton("UnlockButton", detailPanel, new Vector2(0f, -292f), new Vector2(280f, 70f), "解锁", buttonFont, out unlockButtonText);
        unlockButton.onClick.AddListener(HandleUnlockClicked);

        closeButton = CreateButton("CloseButton", panel, new Vector2(686f, -402f), new Vector2(320f, 70f), "返回主菜单", buttonFont, out Text closeText);
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
                    connectionLines.Add(CreateConnectionLine(prerequisite.position, node.position));
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

        treeContentOffset = AbilityTreeViewportMath.ClampOffset(treeContentOffset, CalculateTreeContentBounds(), treeRoot.rect.size, TreeViewportPadding);
        ApplyTreeContentOffset();
    }

    private void Refresh()
    {
        if (service == null)
            return;

        pointsText.text = $"当前能力点 {(wallet != null ? wallet.GetCurrentPoints() : 0)}";
        if (config == null || config.nodes == null || config.nodes.Count == 0)
        {
            detailNameText.text = "未配置能力树";
            detailDescriptionText.text = "请创建 AbilityTreeConfig，并在主菜单控制器中引用。";
            detailCostText.text = string.Empty;
            detailEffectsText.text = string.Empty;
            statusText.text = string.Empty;
            unlockButton.interactable = false;
            return;
        }

        for (int i = 0; i < config.nodes.Count; i++)
        {
            AbilityNodeDefinition node = config.nodes[i];
            if (node == null || string.IsNullOrWhiteSpace(node.nodeId))
                continue;

            AbilityNodeState state = service.GetNodeState(node.nodeId);
            Button button;
            if (nodeButtons.TryGetValue(node.nodeId, out button))
                ApplyNodeState(button, state, node.nodeId == selectedNodeId);

            Text text;
            if (nodeTexts.TryGetValue(node.nodeId, out text))
                text.text = BuildNodeLabel(node, state, service);
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
        detailCostText.text = $"消耗能力点 {Mathf.Max(0, node.cost)}";
        detailEffectsText.text = BuildEffectDescription(node);
        statusText.text = ResolveStateText(state);
        unlockButton.interactable = state == AbilityNodeState.Available || (state == AbilityNodeState.Unlocked && node.isToggleable);
        unlockButtonText.text = state == AbilityNodeState.Unlocked ? "已解锁" : "解锁";
        if (state == AbilityNodeState.Unlocked && node.isToggleable)
            unlockButtonText.text = service.IsNodeEnabled(node.nodeId) ? "关闭" : "启用";
    }

    private void HandleUnlockClicked()
    {
        if (service == null || string.IsNullOrWhiteSpace(selectedNodeId))
            return;

        AbilityNodeDefinition node = config != null ? config.FindNode(selectedNodeId) : null;
        AbilityUnlockResult result = node != null && node.isToggleable && service.GetNodeState(selectedNodeId) == AbilityNodeState.Unlocked
            ? service.SetNodeEnabled(selectedNodeId, !service.IsNodeEnabled(selectedNodeId))
            : service.TryUnlock(selectedNodeId);
        statusText.text = result.message;
        Refresh();
    }

    private Button CreateNodeButton(AbilityNodeDefinition node)
    {
        Button button = CreateButton(
            $"AbilityNode_{node.nodeId}",
            treeContent != null ? treeContent : treeRoot,
            node.position,
            NodeButtonSize,
            string.IsNullOrWhiteSpace(node.displayName) ? node.nodeId : node.displayName,
            ResolveBodyFont(),
            out Text text);
        text.fontSize = 22;
        AddTreePanInput(button.gameObject);
        string capturedId = node.nodeId;
        button.onClick.AddListener(() =>
        {
            selectedNodeId = capturedId;
            Refresh();
        });
        return button;
    }

    private Image CreateConnectionLine(Vector2 from, Vector2 to)
    {
        RectTransform line = CreateRect("ConnectionLine", treeContent != null ? treeContent : treeRoot, (from + to) * 0.5f, new Vector2(Vector2.Distance(from, to), 4f));
        Image image = line.gameObject.AddComponent<Image>();
        image.color = new Color(0.68f, 0.46f, 0.16f, 0.82f);
        Vector2 direction = to - from;
        line.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        line.SetAsFirstSibling();
        return image;
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

            float nodeMinX = node.position.x - halfNode.x;
            float nodeMaxX = node.position.x + halfNode.x;
            float nodeMinY = node.position.y - halfNode.y;
            float nodeMaxY = node.position.y + halfNode.y;
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

        float scaleFactor = canvas != null ? Mathf.Max(0.01f, canvas.scaleFactor) : 1f;
        treeContentOffset += screenDelta / scaleFactor;
        treeContentOffset = AbilityTreeViewportMath.ClampOffset(treeContentOffset, CalculateTreeContentBounds(), treeRoot.rect.size, TreeViewportPadding);
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

        if (treeContent == null)
            return;

        for (int i = treeContent.childCount - 1; i >= 0; i--)
            Destroy(treeContent.GetChild(i).gameObject);
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
        return $"{name}\n{suffix}";
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

    private static void ApplyNodeState(Button button, AbilityNodeState state, bool selected)
    {
        Image image = button.GetComponent<Image>();
        if (image == null)
            return;

        if (selected)
            image.color = new Color(0.4f, 0.28f, 0.08f, 0.98f);
        else if (state == AbilityNodeState.Unlocked)
            image.color = new Color(0.12f, 0.32f, 0.18f, 0.96f);
        else if (state == AbilityNodeState.Available)
            image.color = new Color(0.18f, 0.16f, 0.12f, 0.96f);
        else
            image.color = new Color(0.08f, 0.08f, 0.08f, 0.86f);
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
        Text text = rect.gameObject.AddComponent<Text>();
        text.text = value;
        text.font = font;
        text.fontSize = fontSize;
        text.alignment = anchor;
        text.color = color;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    private static Button CreateButton(string name, Transform parent, Vector2 anchoredPosition, Vector2 size, string label, Font font, out Text text)
    {
        RectTransform rect = CreateRect(name, parent, anchoredPosition, size);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = new Color(0.18f, 0.14f, 0.08f, 0.96f);
        Button button = rect.gameObject.AddComponent<Button>();

        text = CreateText("Text", rect, Vector2.zero, size - new Vector2(20f, 12f), label, 26, font, TextAnchor.MiddleCenter, new Color(0.93f, 0.88f, 0.75f, 1f));
        return button;
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
