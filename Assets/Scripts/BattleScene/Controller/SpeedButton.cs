using TMPro;
using System;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(LineRenderer))]
public class SpeedButton : MonoBehaviour
{
    private const int OverlayCurveRenderQueue = 5000;

    public GameObject unpressedButton;
    public GameObject pressedButton;
    public GameObject speedText;
    [Header("Speed Label")]
    [Tooltip("速度文本相对速度按钮的本地偏移")]
    [SerializeField] private Vector3 speedTextLocalOffset = new Vector3(0f, 0.85f, 0f);
    [Tooltip("自定义速度字体，留空则使用文本当前字体或 TMP 默认字体")]
    [SerializeField] private TMP_FontAsset customSpeedFont;
    [Tooltip("速度文本字号")]
    [SerializeField] private float speedTextFontSize = 5.5f;
    [Tooltip("速度文本颜色")]
    [SerializeField] private Color speedTextColor = Color.white;
    [SerializeField] private int speedTextSortingOrder = 30;
    private PolygonCollider2D polygonCollider2D;

    [Header("Button Settings")]
    [Tooltip("按钮类型：0=敌方，1=我方")]
    [SerializeField] private ButtonType buttonType = ButtonType.Enemy;
    public enum ButtonType { Enemy, Ally }

    [Header("Curve Settings")]
    [Tooltip("曲线的采样点数量，越大越平滑")]
    [SerializeField] private int curveResolution = 30;
    [Tooltip("控制点相对于曲线中点的垂直偏移，影响弧度大小")]
    [SerializeField] private float controlPointOffset = 2f;
    [SerializeField] private int curveSortingOrderOffset = 2;
    [SerializeField] private int arrowSortingOrderOffset = 1;

    [Header("Curve Colors")]
    [Tooltip("未连接时曲线颜色")]
    [SerializeField] private Color unconnectedColor = Color.white;
    [Tooltip("已连接时曲线颜色")]
    [SerializeField] private Color connectedColor = Color.yellow;

    [Header("Battle Settings")]
    [Tooltip("速度值")]
    [SerializeField] private int speed = 0;
    [Tooltip("是否可被交互（友方按钮在战斗开始前不可交互）")]
    [SerializeField] private bool isInteractable = false;
    [Tooltip("友方劣势时连线颜色（浅蓝色）")]
    [SerializeField] private Color allySubordinateColor = new Color(0.4f, 0.8f, 1f);
    [Tooltip("敌方自动连线颜色")]
    [SerializeField] private Color enemyAutoConnectColor = new Color(1f, 0.3f, 0.3f);

    [Header("Curve Dash")]
    [Tooltip("是否将拖拽曲线显示为虚线")]
    [SerializeField] private bool useDashedCurve = true; // 默认使用虚线
    [Tooltip("虚线中实线段长度（世界单位）")]
    [SerializeField] private float dashLength = 0.4f;
    [Tooltip("虚线中间隔长度（世界单位）")]
    [SerializeField] private float dashGap = 0.2f;
    [Tooltip("虚线纹理分辨率，越高越平滑")]
    [SerializeField] private int dashTextureResolution = 64;
    [Tooltip("虚线流动速度（单位/秒），正值向右流动，负值向左流动，0 不流动")]
    [SerializeField] private float dashOffsetSpeed = 1.0f;

    private float currentDashOffset = 0f;
    private float lastCurveLength = 0f;
    private bool isConnected = false;
    private SpeedButton connectedButton = null;

    [Header("Hover Outline")]
    [Tooltip("鼠标悬停时描边颜色")]
    [SerializeField] private Color hoverOutlineColor = Color.yellow;
    [Tooltip("鼠标悬停时描边粗细（世界单位）")]
    [SerializeField] private float hoverOutlineThickness = 0.08f;
    [Tooltip("描边相对按钮外扩边距（世界单位）")]
    [SerializeField] private float hoverOutlinePadding = 0.05f;

    private LineRenderer lineRenderer;
    private bool isDragging = false;
    private bool isHovering = false;
    private bool isPointerPressed = false;
    private bool longPressTriggered = false;
    private Vector3 pressStartScreenPosition;
    private float pressStartTime = 0f;
    private LineRenderer hoverOutlineRenderer;
    private Material curveRuntimeMaterial;
    private Material hoverOutlineMaterial;
    private Texture2D dashTexture;
    private Texture2D solidTexture;
    private SpeedButton hoveredEnemyButton = null;
    private BattleSkillInfoOverlay cachedSkillOverlay;

    // 静态字段：记录当前鼠标悬停的敌方按钮（由敌方按钮自身的 OnMouseEnter/Exit 写入）
    // 与 Unity 内部鼠标检测系统完全一致，避免 Physics2D 查询不一致的问题
    private static SpeedButton currentlyHoveredEnemy = null;

    // 敌方：本轮开始时的原始自动连线目标（EnemyDrawLineTo 设置，玩家交互期间不变）
    private SpeedButton autoTargetAlly = null;
    // 友方：被吸收连线的敌方按钮（黄色连线时设置，友方断线时自动恢复）
    private SpeedButton absorbedEnemyButton = null;
    // 敌方：当前被哪个友方占用为拼点目标
    private SpeedButton contestingAllyButton = null;

    /// <summary>全局战斗锁 —— 战斗结算阶段禁止所有按钮交互与画线</summary>
    public static bool CombatLocked = false;
    public static bool InputSuppressed = false;

    public event Action<SpeedButton> AllyLongPressed;
    public event Action<SpeedButton> EnemyClicked;
    public event Action<SpeedButton> ConnectionChanged;

    [Header("Press Interaction")]
    [Tooltip("长按友方速度按钮多久后打开技能面板")]
    [SerializeField] private float longPressDuration = 0.45f;
    [Tooltip("按下后拖动超过多少像素才算开始画线")]
    [SerializeField] private float dragStartThresholdPixels = 16f;

    // 战斗属性
    public int Speed
    {
        get => speed;
        set { speed = value; UpdateSpeedText(); }
    }

    public bool IsInteractable
    {
        get => isInteractable;
        set => isInteractable = value;
    }

    public void SetButtonType(ButtonType type)
    {
        buttonType = type;
        UpdateHoverOutlineVisibility();
    }

    /// <summary>敌方当前连线目标（友方可读当前被瞄准的友方按钮）</summary>
    public SpeedButton ConnectedTarget => connectedButton;

    /// <summary>是否为拼点连线（黄色：友方吸收了敌方连线，双方直接对抗）</summary>
    public bool IsContestedConnection => isConnected && connectedButton != null && absorbedEnemyButton != null;

    [Header("Arrow Settings")]
    [Tooltip("箭头大小（世界单位）")]
    [SerializeField] private float arrowSize = 0.3f;

    private GameObject arrowObj;
    private MeshFilter arrowMeshFilter;
    private MeshRenderer arrowMeshRenderer;
    private Material arrowMaterial;
    private Mesh arrowMesh;

    private void Awake()
    {
        polygonCollider2D = GetComponent<PolygonCollider2D>();
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = curveResolution;
        lineRenderer.useWorldSpace = true;
        lineRenderer.enabled = false;

        SetupCurveRenderingStyle();
        RefreshCurveAndArrowSorting();

        CreateArrowObject();
        CreateHoverOutlineRenderer();
        UpdateHoverOutlineGeometry();
        UpdateHoverOutlineStyle();
        UpdateHoverOutlineVisibility();
        EnsureSpeedTextObject();
        ApplySpeedTextPresentation();
        UpdateSpeedText();
        if (speedText != null)
            speedText.SetActive(false);
    }

    private void OnValidate()
    {
        curveResolution = Mathf.Max(2, curveResolution);
        controlPointOffset = Mathf.Abs(controlPointOffset);
        dashLength = Mathf.Max(0.01f, dashLength);
        dashGap = Mathf.Max(0.01f, dashGap);
        dashTextureResolution = Mathf.Clamp(dashTextureResolution, 8, 512);
        hoverOutlineThickness = Mathf.Max(0.001f, hoverOutlineThickness);
        hoverOutlinePadding = Mathf.Max(0f, hoverOutlinePadding);
        arrowSize = Mathf.Max(0.01f, arrowSize);

        if (!Application.isPlaying) return;
        if (hoverOutlineRenderer == null) return;

        SetupCurveRenderingStyle();
        UpdateHoverOutlineStyle();
        UpdateHoverOutlineGeometry();
        UpdateHoverOutlineVisibility();
        EnsureSpeedTextObject();
        ApplySpeedTextPresentation();
    }

    private void OnMouseEnter()
    {
        isHovering = true;
        if (buttonType == ButtonType.Enemy)
        {
            currentlyHoveredEnemy = this;
            TryShowEnemyHoverSkillInfo();
        }
        UpdateHoverOutlineVisibility();
    }

    private void OnMouseExit()
    {
        isHovering = false;
        if (buttonType == ButtonType.Enemy && currentlyHoveredEnemy == this)
            currentlyHoveredEnemy = null;
        if (buttonType == ButtonType.Enemy)
            HideEnemyHoverSkillInfo();
        UpdateHoverOutlineVisibility();
    }

    // 当鼠标/手指按下此物体（需要挂载 Collider）时触发
    private void OnMouseDown()
    {
        if (CombatLocked || InputSuppressed) return;
        if (buttonType == ButtonType.Ally && !isInteractable) return;

        isPointerPressed = true;
        longPressTriggered = false;
        pressStartTime = Time.unscaledTime;
        pressStartScreenPosition = Input.mousePosition;

        if (unpressedButton != null) unpressedButton.SetActive(false);
        if (pressedButton != null) pressedButton.SetActive(true);

        UpdateHoverOutlineVisibility();
    }

    // 当鼠标/手指抬起时停止
    private void OnMouseUp()
    {
        if (buttonType == ButtonType.Ally && isInteractable && isPointerPressed && !longPressTriggered && !isDragging)
        {
            if (Vector3.Distance(Input.mousePosition, pressStartScreenPosition) < dragStartThresholdPixels)
            {
                AllyLongPressed?.Invoke(this);
                TryShowAllySkillSelection();
            }
        }

        if (buttonType == ButtonType.Enemy && isPointerPressed && !longPressTriggered)
        {
            if (Vector3.Distance(Input.mousePosition, pressStartScreenPosition) < dragStartThresholdPixels)
            {
                EnemyClicked?.Invoke(this);
                TryShowEnemySkillInfo();
            }
        }

        if (isDragging && buttonType == ButtonType.Ally && isInteractable)
        {
            // 优先使用敌方按钮自身 OnMouseEnter 写入的静态字段（与 Unity 鼠标系统同源，最可靠）
            // 再用 Update 中缓存的 hoveredEnemyButton 兜底
            SpeedButton target = currentlyHoveredEnemy ?? hoveredEnemyButton ?? GetEnemyButtonAtMouse();
            if (target != null)
            {
                ResolveAllyConnection(target);
                hoveredEnemyButton = null;
                UpdateHoverOutlineVisibility();
                return;
            }

            // 松开时未落在任何敌方按钮上：取消连线
            DisconnectFromTarget();
            isDragging = false;
        }

        isPointerPressed = false;
        longPressTriggered = false;
        hoveredEnemyButton = null;
        if (unpressedButton != null) unpressedButton.SetActive(true);
        if (pressedButton != null) pressedButton.SetActive(false);
        UpdateHoverOutlineVisibility();
    }

    /// <summary>
    /// 激活曲线，可供外部或 UI Button 事件调用
    /// </summary>
    public void StartCurve()
    {
        isPointerPressed = false;
        isDragging = true;
        lineRenderer.enabled = true;
        UpdateUnconnectedCurveStyle();
        UpdateHoverOutlineVisibility();
    }

    /// <summary>
    /// 停止并隐藏曲线，可供外部或 UI Button 事件调用
    /// </summary>
    public void StopCurve()
    {
        isPointerPressed = false;
        longPressTriggered = false;
        isDragging = false;
        lineRenderer.enabled = false;
        HideArrow();
        UpdateHoverOutlineVisibility();
    }

    /// <summary>
    /// 公开断开连接，供外部调用（如战斗逻辑中取消敌方连线）
    /// </summary>
    public void DisconnectLine()
    {
        DisconnectFromTarget();
    }

    /// <summary>
    /// 重置连接状态（新一轮战斗开始时调用）
    /// </summary>
    public void ResetConnection()
    {
        absorbedEnemyButton = null;  // 新回合不触发敌方连线恢复
        contestingAllyButton = null;
        autoTargetAlly = null;        // 清除本轮原始目标
        DisconnectFromTarget();
        isPointerPressed = false;
        longPressTriggered = false;
        isDragging = false;
        UpdateHoverOutlineVisibility();
    }

    /// <summary>
    /// 战斗结算阶段隐藏连线视觉（不影响连接状态、不触发敌方连线恢复）。
    /// </summary>
    public void HideLineForCombat()
    {
        EnsureRuntimeReferences();
        if (isDragging)
        {
            isDragging = false;
        }
        HideArrow();
        if (lineRenderer != null)
            lineRenderer.enabled = false;
    }

    /// <summary>
    /// 战斗结算阶段隐藏按钮、文本、连线和箭头，直到下一次目标指定阶段再恢复。
    /// </summary>
    public void HideForCombat()
    {
        EnsureRuntimeReferences();
        isHovering = false;
        isPointerPressed = false;
        longPressTriggered = false;
        hoveredEnemyButton = null;
        if (buttonType == ButtonType.Enemy && currentlyHoveredEnemy == this)
            currentlyHoveredEnemy = null;

        HideLineForCombat();

        if (unpressedButton != null)
            unpressedButton.SetActive(false);
        if (pressedButton != null)
            pressedButton.SetActive(false);
        if (speedText != null)
            speedText.SetActive(false);
        if (hoverOutlineRenderer != null)
            hoverOutlineRenderer.enabled = false;
        if (polygonCollider2D != null)
            polygonCollider2D.enabled = false;
    }

    /// <summary>
    /// 目标指定阶段恢复按钮与速度文本显示。
    /// </summary>
    public void ShowForTargeting()
    {
        EnsureRuntimeReferences();
        EnsureSpeedTextObject();
        isPointerPressed = false;
        longPressTriggered = false;
        if (polygonCollider2D != null)
            polygonCollider2D.enabled = true;
        if (unpressedButton != null)
            unpressedButton.SetActive(true);
        if (pressedButton != null)
            pressedButton.SetActive(false);
        if (speedText != null)
            speedText.SetActive(true);

        ApplySpeedTextPresentation();
        UpdateSpeedText();
        UpdateHoverOutlineVisibility();
    }

    private void EnsureRuntimeReferences()
    {
        if (polygonCollider2D == null)
            polygonCollider2D = GetComponent<PolygonCollider2D>();

        if (lineRenderer == null)
        {
            lineRenderer = GetComponent<LineRenderer>();
            if (lineRenderer != null)
            {
                lineRenderer.positionCount = Mathf.Max(2, curveResolution);
                lineRenderer.useWorldSpace = true;
                lineRenderer.enabled = false;
                SetupCurveRenderingStyle();
                RefreshCurveAndArrowSorting();
            }
        }

        if (hoverOutlineRenderer == null && gameObject.scene.IsValid())
        {
            CreateHoverOutlineRenderer();
            UpdateHoverOutlineGeometry();
            UpdateHoverOutlineStyle();
        }
    }

    /// <summary>
    /// 敌方按钮自动连线到指定友方按钮（由 BattleEnemyController 调用）
    /// </summary>
    public void EnemyDrawLineTo(SpeedButton allyTarget)
    {
        if (buttonType != ButtonType.Enemy || allyTarget == null) return;
        contestingAllyButton = null;
        autoTargetAlly = allyTarget;  // 记录本轮初始目标，供断线时恢复
        ConnectToTarget(allyTarget, enemyAutoConnectColor);
    }

    /// <summary>
    /// 恢复到本轮自动连线的初始状态（由友方 DisconnectFromTarget 在黄色连线取消时调用）
    /// </summary>
    public void RestoreAutoConnection()
    {
        if (buttonType != ButtonType.Enemy)
            return;

        SpeedButton preferredContestingAlly = FindPreferredContestingAlly();
        if (preferredContestingAlly != null)
        {
            contestingAllyButton = preferredContestingAlly;
            ConnectToTarget(preferredContestingAlly, Color.yellow);
            preferredContestingAlly.PromoteToContestedConnection(this);
            return;
        }

        contestingAllyButton = null;
        BattleAllyController autoTargetController = autoTargetAlly != null ? autoTargetAlly.GetComponentInParent<BattleAllyController>() : null;
        if (autoTargetAlly != null && (autoTargetController == null || !autoTargetController.IsDead))
            ConnectToTarget(autoTargetAlly, enemyAutoConnectColor);
        else
            DisconnectFromTarget();
    }

    /// <summary>
    /// 友方连线落点判定：根据速度关系决定连线颜色并处理敌方连线
    /// </summary>
    private void ResolveAllyConnection(SpeedButton enemy)
    {
        // 先断开当前连线（若先前有黄色连线，此处会恢复被吸收的敌方连线）
        DisconnectFromTarget();

        if (speed >= enemy.speed || enemy.ConnectedTarget == this)
        {
            // 友方速度占优或双方互相瞄准：该友方成为唯一的拼点连线
            enemy.RegisterContestingAlly(this);
            absorbedEnemyButton = enemy;
            ConnectToTarget(enemy, Color.yellow);
        }
        else
        {
            // 敌方速度更高且敌方目标不是自己：浅蓝色实线，敌方连线保留
            absorbedEnemyButton = null;
            ConnectToTarget(enemy, allySubordinateColor);
        }

        isDragging = false;
    }

    private void RegisterContestingAlly(SpeedButton newContestingAlly)
    {
        if (buttonType != ButtonType.Enemy || newContestingAlly == null)
            return;

        if (contestingAllyButton != null && contestingAllyButton != newContestingAlly)
            contestingAllyButton.DowngradeFromContestedConnection(this);

        contestingAllyButton = newContestingAlly;
        ConnectToTarget(newContestingAlly, Color.yellow);
    }

    private SpeedButton FindPreferredContestingAlly()
    {
        if (buttonType != ButtonType.Enemy)
            return null;

        SpeedButton bestCandidate = null;
        SpeedButton[] allButtons = FindObjectsOfType<SpeedButton>(true);
        for (int i = 0; i < allButtons.Length; i++)
        {
            SpeedButton allyButton = allButtons[i];
            if (allyButton == null || allyButton == this)
                continue;
            if (allyButton.buttonType != ButtonType.Ally)
                continue;
            BattleAllyController allyController = allyButton.GetComponentInParent<BattleAllyController>();
            if (allyController != null && allyController.IsDead)
                continue;
            if (!allyButton.isConnected || allyButton.connectedButton != this)
                continue;

            bool isStrongEnoughToContest = allyButton.speed >= speed;
            bool isAutoTargetContest = autoTargetAlly != null && allyButton == autoTargetAlly;
            if (!isStrongEnoughToContest && !isAutoTargetContest)
                continue;

            if (bestCandidate == null)
            {
                bestCandidate = allyButton;
                continue;
            }

            bool candidateIsAutoTarget = autoTargetAlly != null && allyButton == autoTargetAlly;
            bool bestIsAutoTarget = autoTargetAlly != null && bestCandidate == autoTargetAlly;
            if (candidateIsAutoTarget && !bestIsAutoTarget)
            {
                bestCandidate = allyButton;
                continue;
            }

            if (allyButton.speed > bestCandidate.speed)
                bestCandidate = allyButton;
        }

        return bestCandidate;
    }

    private void PromoteToContestedConnection(SpeedButton enemyButton)
    {
        if (buttonType != ButtonType.Ally || enemyButton == null)
            return;
        if (!isConnected || connectedButton != enemyButton)
            return;

        absorbedEnemyButton = enemyButton;
        DrawConnectedCurve(Color.yellow);
        lineRenderer.enabled = true;
        UpdateConnectedCurveStyleWithColor(Color.yellow);
    }

    private void DowngradeFromContestedConnection(SpeedButton enemyButton)
    {
        if (buttonType != ButtonType.Ally)
            return;
        if (connectedButton != enemyButton)
            return;

        absorbedEnemyButton = null;
        DrawConnectedCurve(allySubordinateColor);
        lineRenderer.enabled = true;
        UpdateConnectedCurveStyleWithColor(allySubordinateColor);
    }

    /// <summary>
    /// 连接到目标按钮，lineColor 决定最终实线颜色
    /// </summary>
    private void ConnectToTarget(SpeedButton targetButton, Color lineColor)
    {
        // 如果已连接，先断开
        if (isConnected && connectedButton != null)
            DisconnectFromTarget();

        // 连接到新的目标按钮
        connectedButton = targetButton;
        isConnected = true;

        // 绘制最终的连接曲线
        DrawConnectedCurve(lineColor);

        // 关闭拖拽状态
        isDragging = false;
        lineRenderer.enabled = true; // 保持显示连接后的曲线

        // 重置按钮状态
        if (unpressedButton != null) unpressedButton.SetActive(true);
        if (pressedButton != null) pressedButton.SetActive(false);

        // 更新曲线样式为实线
        UpdateConnectedCurveStyleWithColor(lineColor);
        ConnectionChanged?.Invoke(this);
    }

    /// <summary>
    /// 断开与目标按钮的连接（无论是否已连接均可安全调用）
    /// </summary>
    private void DisconnectFromTarget()
    {
        EnsureRuntimeReferences();
        bool hadConnection = connectedButton != null || isConnected;

        // 先暂存需要恢复的敌方按钮，再清除自身状态，
        // 确保 FindPreferredContestingAlly 不会把正在断线的自己算进候选。
        SpeedButton prevAbsorbedEnemy = absorbedEnemyButton;

        absorbedEnemyButton = null;
        connectedButton = null;
        isConnected = false;
        if (lineRenderer != null)
            lineRenderer.enabled = false;
        HideArrow();

        // 若此友方按钮曾吸收敌方连线（黄色），取消后恢复敌方原始连线（红线）
        if (prevAbsorbedEnemy != null)
            prevAbsorbedEnemy.RestoreAutoConnection();

        if (hadConnection)
            ConnectionChanged?.Invoke(this);
    }

    private void HideConnectionVisual()
    {
        lineRenderer.enabled = false;
        HideArrow();
    }

    /// <summary>
    /// 绘制连接曲线（从当前位置到目标按钮）
    /// </summary>
    private void DrawConnectedCurve(Color arrowColor)
    {
        if (connectedButton == null) return;

        Vector3 startPos = transform.position;
        Vector3 endPos = connectedButton.transform.position;

        // 控制点固定向世界上方偏移，确保曲线始终上凸
        Vector3 mid = (startPos + endPos) * 0.5f;
        Vector3 controlPoint = mid + Vector3.up * Mathf.Abs(controlPointOffset);

        for (int i = 0; i < curveResolution; i++)
        {
            float t = i / (float)(curveResolution - 1);
            Vector3 current = QuadraticBezier(startPos, controlPoint, endPos, t);
            lineRenderer.SetPosition(i, current);
        }

        // 箭头尖端指向目标按钮中心，动态抬高排序确保不被按钮精灵遮挡
        Vector3 tangentDir = (endPos - controlPoint).normalized;
        EnsureArrowSortingAboveButton(connectedButton);
        UpdateArrow(endPos, tangentDir, arrowColor);
    }

    /// <summary>
    /// 更新曲线样式为连接状态（使用 connectedColor，保留向后兼容）
    /// </summary>
    private void UpdateConnectedCurveStyle()
    {
        UpdateConnectedCurveStyleWithColor(connectedColor);
    }

    /// <summary>
    /// 更新曲线样式为指定颜色的实线
    /// </summary>
    private void UpdateConnectedCurveStyleWithColor(Color color)
    {
        // 切换到1×1白色实线纹理，去除虚线效果
        if (curveRuntimeMaterial != null && solidTexture != null)
        {
            curveRuntimeMaterial.mainTexture = solidTexture;
            ApplyDashMaterialST(1f, 0f);
        }
        lineRenderer.textureMode = LineTextureMode.Stretch;
        lineRenderer.startColor = color;
        lineRenderer.endColor = color;
        lineRenderer.widthMultiplier = 0.1f;
    }

    /// <summary>
    /// 更新曲线样式为未连接状态（白色虚线）
    /// </summary>
    private void UpdateUnconnectedCurveStyle()
    {
        if (useDashedCurve && curveRuntimeMaterial != null)
        {
            // 恢复虚线纹理
            if (dashTexture == null || dashTexture.width != dashTextureResolution)
                CreateDashTexture();
            curveRuntimeMaterial.mainTexture = dashTexture;
            lineRenderer.textureMode = LineTextureMode.Tile;
        }
        else
        {
            lineRenderer.textureMode = LineTextureMode.Stretch;
        }
        lineRenderer.startColor = unconnectedColor;
        lineRenderer.endColor = unconnectedColor;
        lineRenderer.widthMultiplier = 0.05f;
    }

    private void Update()
    {
        if (hoverOutlineRenderer != null)
            UpdateHoverOutlineGeometry();

        // 全局战斗锁：结算阶段终止拖拽，不处理任何输入
        if (CombatLocked)
        {
            if (isDragging) StopCurve();
            return;
        }

        if (InputSuppressed)
        {
            if (isDragging) StopCurve();
            return;
        }

        if (isPointerPressed && !isDragging)
        {
            float movedPixels = Vector3.Distance(Input.mousePosition, pressStartScreenPosition);
            if (buttonType == ButtonType.Ally && isInteractable)
            {
                if (!longPressTriggered && movedPixels >= dragStartThresholdPixels)
                {
                    StartCurve();
                }
                else if (!longPressTriggered && Time.unscaledTime - pressStartTime >= longPressDuration)
                {
                    longPressTriggered = true;
                    isPointerPressed = false;
                    if (unpressedButton != null) unpressedButton.SetActive(true);
                    if (pressedButton != null) pressedButton.SetActive(false);
                    AllyLongPressed?.Invoke(this);
                    TryShowAllySkillSelection();
                    UpdateHoverOutlineVisibility();
                    return;
                }
            }
            else if (buttonType == ButtonType.Enemy && movedPixels >= dragStartThresholdPixels)
            {
                isPointerPressed = false;
                if (unpressedButton != null) unpressedButton.SetActive(true);
                if (pressedButton != null) pressedButton.SetActive(false);
            }
        }

        // 右键点击我方按钮：断开连线
        if (isHovering && buttonType == ButtonType.Ally && isInteractable && Input.GetMouseButtonDown(1))
        {
            DisconnectFromTarget();
            isDragging = false;
            UpdateHoverOutlineVisibility();
            return;
        }

        if (!isDragging) return;

        // 拖拽时：若鼠标进入敌方按钮碰撞盒则吸附到按钮中心，否则跟随鼠标
        hoveredEnemyButton = GetEnemyButtonAtMouse();
        Vector3 endPos = hoveredEnemyButton != null
            ? hoveredEnemyButton.transform.position
            : GetMouseWorldPosition();

        DrawBezierCurve(transform.position, endPos, hoveredEnemyButton);

        if (useDashedCurve)
            UpdateDashAnimation();
    }

    /// <summary>
    /// 获取当前鼠标位置下的敌方按钮（不存在则返回 null）
    /// 使用 OverlapPointAll 而非 Raycast，避免 2D 相机下方向向量为零导致的误判
    /// </summary>
    private SpeedButton GetEnemyButtonAtMouse()
    {
        Vector2 worldPoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Collider2D[] hits = Physics2D.OverlapPointAll(worldPoint);
        foreach (Collider2D hit in hits)
        {
            SpeedButton btn = hit.GetComponent<SpeedButton>();
            if (btn != null && btn.buttonType == ButtonType.Enemy && btn != this)
                return btn;
        }
        return null;
    }

    /// <summary>
    /// 绘制从 start 到 end 的二次贝塞尔曲线
    /// </summary>
    /// <param name="snapTarget">吸附的目标按钮（如有），用于将箭头放在按钮边缘</param>
    private void DrawBezierCurve(Vector3 start, Vector3 end, SpeedButton snapTarget = null)
    {
        // 控制点固定向世界上方偏移，确保曲线始终上凸
        Vector3 mid = (start + end) * 0.5f;
        Vector3 controlPoint = mid + Vector3.up * Mathf.Abs(controlPointOffset);
        float curveLength = 0f;
        Vector3 previous = start;

        for (int i = 0; i < curveResolution; i++)
        {
            float t = i / (float)(curveResolution - 1);
            Vector3 current = QuadraticBezier(start, controlPoint, end, t);
            lineRenderer.SetPosition(i, current);

            if (i > 0)
            {
                curveLength += Vector3.Distance(previous, current);
            }

            previous = current;
        }

        if (useDashedCurve)
        {
            lastCurveLength = curveLength;
        }

        // 箭头方向 = 贝塞尔末端切线（跟随曲线弧度）
        Vector3 tangentDir = (end - controlPoint).normalized;
        if (snapTarget != null)
            EnsureArrowSortingAboveButton(snapTarget);
        Vector3 arrowTip = snapTarget != null ? snapTarget.transform.position : end;
        UpdateArrow(arrowTip, tangentDir, lineRenderer.startColor);
    }

    /// <summary>
    /// 动态将箭头的 sortingOrder 抬高到目标按钮上方。
    /// arrowObj 在场景根节点（不在任何 SortingGroup 内），
    /// 而目标按钮在 SortingGroup 内，因此必须以 SortingGroup 的全局排序值为基准。
    /// </summary>
    private void EnsureArrowSortingAboveButton(SpeedButton targetButton)
    {
        if (arrowMeshRenderer == null || targetButton == null)
            return;

        // 查找目标和自身所在的 SortingGroup，取最高的全局排序值
        SortingGroup targetGroup = targetButton.GetComponentInParent<SortingGroup>();
        SortingGroup sourceGroup = GetComponentInParent<SortingGroup>();

        SortingGroup referenceGroup = null;
        if (targetGroup != null && sourceGroup != null)
            referenceGroup = targetGroup.sortingOrder >= sourceGroup.sortingOrder ? targetGroup : sourceGroup;
        else if (targetGroup != null)
            referenceGroup = targetGroup;
        else if (sourceGroup != null)
            referenceGroup = sourceGroup;

        if (referenceGroup != null)
        {
            arrowMeshRenderer.sortingLayerID = referenceGroup.sortingLayerID;
            arrowMeshRenderer.sortingLayerName = referenceGroup.sortingLayerName;
            arrowMeshRenderer.sortingOrder = referenceGroup.sortingOrder + 5;
            return;
        }

        // 无 SortingGroup 则回退到按钮渲染器的 sortingOrder
        Renderer topRenderer = targetButton.GetButtonVisualTopRenderer();
        if (topRenderer == null)
            return;

        arrowMeshRenderer.sortingLayerID = topRenderer.sortingLayerID;
        arrowMeshRenderer.sortingLayerName = topRenderer.sortingLayerName;
        arrowMeshRenderer.sortingOrder = topRenderer.sortingOrder + 5;
    }

    // B(t) = (1-t)²P0 + 2(1-t)tP1 + t
    private static Vector3 QuadraticBezier(Vector3 p0, Vector3 p1, Vector3 p2, float t)
    {
        float u = 1f - t;
        return u * u * p0 + 2f * u * t * p1 + t * t * p2;
    }

    /// <summary>
    /// 将屏幕鼠标坐标转换为世界坐标（适用于正交/透视摄像机）
    /// </summary>
    private Vector3 GetMouseWorldPosition()
    {
        Camera cam = Camera.main;
        Vector3 screenPos = Input.mousePosition;

        if (cam.orthographic)
        {
            // 正交摄像机：保持与按钮相同的 Z 深度
            screenPos.z = cam.WorldToScreenPoint(transform.position).z;
        }
        else
        {
            // 透视摄像机：投影到按钮所在平面的深度
            screenPos.z = Vector3.Dot(transform.position - cam.transform.position,
                                      cam.transform.forward);
        }

        return cam.ScreenToWorldPoint(screenPos);
    }

    private void CreateHoverOutlineRenderer()
    {
        GameObject outlineObj = new GameObject("HoverOutline");
        outlineObj.transform.SetParent(transform, false);
        outlineObj.transform.localPosition = Vector3.zero;
        outlineObj.transform.localRotation = Quaternion.identity;
        outlineObj.transform.localScale = Vector3.one;

        hoverOutlineRenderer = outlineObj.AddComponent<LineRenderer>();
        hoverOutlineRenderer.useWorldSpace = true;
        hoverOutlineRenderer.loop = true;
        hoverOutlineRenderer.positionCount = 0;
        hoverOutlineRenderer.enabled = false;

        // 描边使用独立的纯色材质，避免与虚线曲线材质共享导致描边出现透明间隔
        Shader outlineShader = Shader.Find("Sprites/Default");
        if (outlineShader == null) outlineShader = Shader.Find("Unlit/Transparent");
        if (outlineShader != null)
        {
            hoverOutlineMaterial = new Material(outlineShader);
            hoverOutlineMaterial.name = "Runtime_HoverOutlineMaterial";
            hoverOutlineRenderer.material = hoverOutlineMaterial;
        }
    }

    private void UpdateHoverOutlineStyle()
    {
        hoverOutlineRenderer.startWidth = hoverOutlineThickness;
        hoverOutlineRenderer.endWidth = hoverOutlineThickness;
        hoverOutlineRenderer.startColor = hoverOutlineColor;
        hoverOutlineRenderer.endColor = hoverOutlineColor;
    }

    private void UpdateHoverOutlineVisibility()
    {
        if (hoverOutlineRenderer == null) return;
        hoverOutlineRenderer.enabled = isHovering && !isDragging;
    }

    private void UpdateHoverOutlineGeometry()
    {
        if (hoverOutlineRenderer == null) return;
        if (TryUpdateOutlineFromPolygonCollider()) return;

        Bounds bounds = GetButtonBounds();
        Vector3 center = bounds.center;
        Vector3 ext = bounds.extents + Vector3.one * hoverOutlinePadding;

        hoverOutlineRenderer.positionCount = 4;
        float z = center.z;
        hoverOutlineRenderer.SetPosition(0, new Vector3(center.x - ext.x, center.y - ext.y, z));
        hoverOutlineRenderer.SetPosition(1, new Vector3(center.x - ext.x, center.y + ext.y, z));
        hoverOutlineRenderer.SetPosition(2, new Vector3(center.x + ext.x, center.y + ext.y, z));
        hoverOutlineRenderer.SetPosition(3, new Vector3(center.x + ext.x, center.y - ext.y, z));
    }

    private bool TryUpdateOutlineFromPolygonCollider()
    {
        if (polygonCollider2D == null) return false;
        if (polygonCollider2D.pathCount <= 0) return false;

        int selectedPath = 0;
        float maxPerimeter = -1f;

        // PolygonCollider2D 可能有多个 path，优先选择外轮廓（周长最大的 path）。
        for (int pathIndex = 0; pathIndex < polygonCollider2D.pathCount; pathIndex++)
        {
            Vector2[] path = polygonCollider2D.GetPath(pathIndex);
            if (path == null || path.Length < 3) continue;

            float perimeter = 0f;
            for (int i = 0; i < path.Length; i++)
            {
                Vector2 a = path[i];
                Vector2 b = path[(i + 1) % path.Length];
                perimeter += Vector2.Distance(a, b);
            }

            if (perimeter > maxPerimeter)
            {
                maxPerimeter = perimeter;
                selectedPath = pathIndex;
            }
        }

        Vector2[] points = polygonCollider2D.GetPath(selectedPath);
        if (points == null || points.Length < 3) return false;

        hoverOutlineRenderer.positionCount = points.Length;

        Vector2 centroid = Vector2.zero;
        for (int i = 0; i < points.Length; i++)
        {
            centroid += points[i];
        }
        centroid /= points.Length;

        for (int i = 0; i < points.Length; i++)
        {
            Vector2 localPoint = points[i];

            // 为 polygon 轮廓提供一个近似外扩。
            if (hoverOutlinePadding > 0f)
            {
                Vector2 dir = (localPoint - centroid).normalized;
                localPoint += dir * hoverOutlinePadding;
            }

            hoverOutlineRenderer.SetPosition(i, transform.TransformPoint(localPoint));
        }

        return true;
    }

    private Bounds GetButtonBounds()
    {
        Collider2D col2D = GetComponent<Collider2D>();
        if (col2D != null)
        {
            return col2D.bounds;
        }

        Collider col3D = GetComponent<Collider>();
        if (col3D != null)
        {
            return col3D.bounds;
        }

        Renderer renderer = GetComponentInChildren<Renderer>();
        if (renderer != null)
        {
            return renderer.bounds;
        }

        return new Bounds(transform.position, Vector3.one);
    }

    private void SetupCurveRenderingStyle()
    {
        if (lineRenderer == null) return;

        if (!useDashedCurve)
        {
            lineRenderer.textureMode = LineTextureMode.Stretch;
            return;
        }

        if (curveRuntimeMaterial == null)
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Transparent");
            }

            if (shader != null)
            {
                curveRuntimeMaterial = new Material(shader);
                curveRuntimeMaterial.name = "Runtime_DashedCurveMaterial";
                // 确保材质支持偏移
                curveRuntimeMaterial.mainTextureOffset = Vector2.zero;
            }
        }

        if (curveRuntimeMaterial == null) return;

        if (dashTexture == null || dashTexture.width != dashTextureResolution)
        {
            CreateDashTexture();
        }

        if (solidTexture == null)
        {
            solidTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            solidTexture.SetPixel(0, 0, Color.white);
            solidTexture.Apply(false, false);
        }

        lineRenderer.textureMode = LineTextureMode.Tile;
        lineRenderer.material = curveRuntimeMaterial;
        curveRuntimeMaterial.mainTexture = dashTexture;
    }

    private void CreateDashTexture()
    {
        if (dashTexture != null)
        {
            Destroy(dashTexture);
        }

        dashTexture = new Texture2D(dashTextureResolution, 1, TextureFormat.RGBA32, false);
        dashTexture.wrapMode = TextureWrapMode.Repeat;
        dashTexture.filterMode = FilterMode.Point;

        float cycle = dashLength + dashGap;
        int onPixels = Mathf.Clamp(Mathf.RoundToInt(dashTextureResolution * (dashLength / cycle)), 1, dashTextureResolution - 1);

        for (int x = 0; x < dashTextureResolution; x++)
        {
            dashTexture.SetPixel(x, 0, x < onPixels ? Color.white : Color.clear);
        }

        dashTexture.Apply(false, false);
    }

    private void UpdateDashAnimation()
    {
        if (!useDashedCurve) return;
        if (curveRuntimeMaterial == null) return;

        float cycle = dashLength + dashGap;
        float repeat = Mathf.Max(1f, lastCurveLength / cycle);

        currentDashOffset += dashOffsetSpeed * Time.deltaTime;
        float normalizedOffset = Mathf.Repeat(currentDashOffset / cycle, 1f);

        ApplyDashMaterialST(repeat, -normalizedOffset);
    }

    private void ApplyDashMaterialST(float xScale, float xOffset)
    {
        Vector2 scale = new Vector2(xScale, 1f);
        Vector2 offset = new Vector2(xOffset, 0f);

        if (curveRuntimeMaterial.HasProperty("_MainTex"))
        {
            curveRuntimeMaterial.SetTextureScale("_MainTex", scale);
            curveRuntimeMaterial.SetTextureOffset("_MainTex", offset);
        }

        if (curveRuntimeMaterial.HasProperty("_BaseMap"))
        {
            curveRuntimeMaterial.SetTextureScale("_BaseMap", scale);
            curveRuntimeMaterial.SetTextureOffset("_BaseMap", offset);
        }
    }

    private void CreateArrowObject()
    {
        arrowObj = new GameObject("CurveArrow");
        // 继承与 SpeedButton 相同的层，确保相机剔除遮罩一致
        arrowObj.layer = gameObject.layer;
        arrowMeshFilter = arrowObj.AddComponent<MeshFilter>();
        arrowMeshRenderer = arrowObj.AddComponent<MeshRenderer>();

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Unlit/Transparent");
        if (shader != null)
        {
            arrowMaterial = new Material(shader);
            arrowMaterial.name = "Runtime_ArrowMaterial";
            arrowMeshRenderer.material = arrowMaterial;
        }
        RefreshCurveAndArrowSorting();

        arrowMesh = new Mesh();
        arrowMesh.name = "ArrowMesh";
        arrowMesh.vertices = new Vector3[3];
        arrowMesh.triangles = new int[] { 0, 1, 2 };
        arrowMeshFilter.mesh = arrowMesh;

        arrowObj.SetActive(false);
    }

    /// <summary>
    /// 更新箭头三角形的位置、朝向和颜色
    /// </summary>
    /// <param name="tipPos">箭头尖端（曲线末端）</param>
    /// <param name="tangentDir">曲线末端切线方向（已归一化）</param>
    /// <param name="color">箭头颜色</param>
    private void UpdateArrow(Vector3 tipPos, Vector3 tangentDir, Color color)
    {
        if (arrowObj == null || arrowMesh == null) return;

        // 垂直于切线方向的向量（在 XY 平面内旋转 90°）
        Vector3 perp = new Vector3(-tangentDir.y, tangentDir.x, 0f);
        Vector3 baseCenter = tipPos - tangentDir * arrowSize;
        float halfWidth = arrowSize * 0.5f;

        arrowMesh.vertices = new Vector3[]
        {
            new Vector3(tipPos.x,                              tipPos.y,                              tipPos.z),
            new Vector3(baseCenter.x + perp.x * halfWidth,    baseCenter.y + perp.y * halfWidth,    tipPos.z),
            new Vector3(baseCenter.x - perp.x * halfWidth,    baseCenter.y - perp.y * halfWidth,    tipPos.z)
        };
        arrowMesh.RecalculateBounds();

        if (arrowMaterial != null)
            arrowMaterial.color = color;

        arrowObj.SetActive(true);
    }

    private void HideArrow()
    {
        if (arrowObj != null)
            arrowObj.SetActive(false);
    }

    private void RefreshCurveAndArrowSorting()
    {
        Renderer referenceRenderer = GetGlobalTopSpeedButtonRenderer();
        if (referenceRenderer == null)
            referenceRenderer = GetButtonVisualTopRenderer();

        int baseSortingOrder = referenceRenderer != null ? referenceRenderer.sortingOrder : 0;
        int sortingLayerId = referenceRenderer != null ? referenceRenderer.sortingLayerID : 0;
        string sortingLayerName = referenceRenderer != null ? referenceRenderer.sortingLayerName : "Default";

        if (lineRenderer != null)
        {
            lineRenderer.sortingLayerID = sortingLayerId;
            lineRenderer.sortingLayerName = sortingLayerName;
            lineRenderer.sortingOrder = baseSortingOrder + curveSortingOrderOffset;
        }
        if (arrowMeshRenderer != null)
        {
            arrowMeshRenderer.sortingLayerID = sortingLayerId;
            arrowMeshRenderer.sortingLayerName = sortingLayerName;
            arrowMeshRenderer.sortingOrder = baseSortingOrder + curveSortingOrderOffset + arrowSortingOrderOffset;
        }
        if (hoverOutlineRenderer != null)
        {
            hoverOutlineRenderer.sortingLayerID = sortingLayerId;
            hoverOutlineRenderer.sortingLayerName = sortingLayerName;
            hoverOutlineRenderer.sortingOrder = baseSortingOrder + curveSortingOrderOffset + arrowSortingOrderOffset + 1;
        }

        ApplyOverlayRenderQueue();
    }

    private Renderer GetButtonVisualTopRenderer()
    {
        Renderer bestRenderer = null;
        int bestLayerValue = int.MinValue;
        int bestSortingOrder = int.MinValue;
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
                continue;
            if (renderer == lineRenderer || renderer == hoverOutlineRenderer || renderer == arrowMeshRenderer)
                continue;
            if (speedText != null && renderer.transform.IsChildOf(speedText.transform))
                continue;

            int layerValue = SortingLayer.GetLayerValueFromID(renderer.sortingLayerID);
            if (bestRenderer == null || layerValue > bestLayerValue || (layerValue == bestLayerValue && renderer.sortingOrder > bestSortingOrder))
            {
                bestRenderer = renderer;
                bestLayerValue = layerValue;
                bestSortingOrder = renderer.sortingOrder;
            }
        }

        return bestRenderer;
    }

    private Renderer GetGlobalTopSpeedButtonRenderer()
    {
        SpeedButton[] allButtons = FindObjectsOfType<SpeedButton>(true);
        Renderer bestRenderer = null;
        int bestLayerValue = int.MinValue;
        int bestSortingOrder = int.MinValue;

        for (int buttonIndex = 0; buttonIndex < allButtons.Length; buttonIndex++)
        {
            SpeedButton button = allButtons[buttonIndex];
            if (button == null)
                continue;

            Renderer candidate = button.GetButtonVisualTopRenderer();
            if (candidate == null)
                continue;

            int layerValue = SortingLayer.GetLayerValueFromID(candidate.sortingLayerID);
            if (bestRenderer == null || layerValue > bestLayerValue || (layerValue == bestLayerValue && candidate.sortingOrder > bestSortingOrder))
            {
                bestRenderer = candidate;
                bestLayerValue = layerValue;
                bestSortingOrder = candidate.sortingOrder;
            }
        }

        return bestRenderer;
    }

    private void ApplyOverlayRenderQueue()
    {
        // URP 透明渲染 pass 的上限为 queue=5000（含），超过则不会被渲染。
        // 箭头与描边用与曲线相同的队列，由 sortingOrder 来保证视觉层次。
        if (curveRuntimeMaterial != null)
            curveRuntimeMaterial.renderQueue = OverlayCurveRenderQueue;
        if (arrowMaterial != null)
            arrowMaterial.renderQueue = OverlayCurveRenderQueue;
        if (hoverOutlineMaterial != null)
            hoverOutlineMaterial.renderQueue = OverlayCurveRenderQueue;
    }

    /// <summary>
    /// 将速度值同步显示到 speedText 子物体（支持 TextMeshPro 及 TextMeshProUGUI）
    /// </summary>
    private void UpdateSpeedText()
    {
        EnsureSpeedTextObject();
        if (speedText == null) return;
        ApplySpeedTextPresentation();
        var tmp = speedText.GetComponent<TextMeshPro>();
        if (tmp != null) { tmp.text = speed.ToString(); return; }
        var tmpUGUI = speedText.GetComponent<TextMeshProUGUI>();
        if (tmpUGUI != null) tmpUGUI.text = speed.ToString();
    }

    private void ApplySpeedTextPresentation()
    {
        EnsureSpeedTextObject();
        if (speedText == null)
            return;

        speedText.transform.localPosition = speedTextLocalOffset;

        TextMeshPro tmp = speedText.GetComponent<TextMeshPro>();
        if (tmp != null)
        {
            if (customSpeedFont != null)
                tmp.font = customSpeedFont;
            tmp.fontSize = speedTextFontSize;
            tmp.color = speedTextColor;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.sortingOrder = speedTextSortingOrder;
            tmp.enableWordWrapping = false;
            tmp.overflowMode = TextOverflowModes.Overflow;
            return;
        }

        TextMeshProUGUI tmpUGUI = speedText.GetComponent<TextMeshProUGUI>();
        if (tmpUGUI != null)
        {
            if (customSpeedFont != null)
                tmpUGUI.font = customSpeedFont;
            tmpUGUI.fontSize = speedTextFontSize;
            tmpUGUI.color = speedTextColor;
            tmpUGUI.alignment = TextAlignmentOptions.Center;

            RectTransform rect = tmpUGUI.rectTransform;
            rect.anchoredPosition3D = speedTextLocalOffset;
        }
    }

    private void EnsureSpeedTextObject()
    {
        if (speedText != null)
            return;

        GameObject speedTextObject = new GameObject("RuntimeSpeedText", typeof(TextMeshPro));
        speedTextObject.transform.SetParent(transform, false);
        speedTextObject.transform.localPosition = speedTextLocalOffset;
        speedTextObject.transform.localRotation = Quaternion.identity;
        speedTextObject.transform.localScale = Vector3.one;

        TextMeshPro tmp = speedTextObject.GetComponent<TextMeshPro>();
        tmp.text = speed.ToString();
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = false;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.sortingOrder = speedTextSortingOrder;
        tmp.isOrthographic = true;

        speedText = speedTextObject;
        speedText.SetActive(false);
    }

    private void TryShowAllySkillSelection()
    {
        BattleAllyController ally = GetComponentInParent<BattleAllyController>();
        if (ally == null)
            return;

        BattleSkillInfoOverlay overlay = GetSkillOverlay();
        if (overlay == null)
            return;

        overlay.ShowAllySkillSelection(ally, this);
    }

    private void TryShowEnemySkillInfo()
    {
        BattleEnemyController enemy = GetComponentInParent<BattleEnemyController>();
        if (enemy == null)
            return;

        BattleSkillInfoOverlay overlay = GetSkillOverlay();
        if (overlay == null)
            return;

        overlay.ShowEnemySkillInfo(enemy, this);
    }

    private void TryShowEnemyHoverSkillInfo()
    {
        BattleEnemyController enemy = GetComponentInParent<BattleEnemyController>();
        if (enemy == null)
            return;

        BattleSkillInfoOverlay overlay = GetSkillOverlay();
        if (overlay == null)
            return;

        overlay.ShowEnemyHoverSkillInfo(enemy, this);
    }

    private void HideEnemyHoverSkillInfo()
    {
        BattleSkillInfoOverlay overlay = GetSkillOverlay();
        if (overlay == null)
            return;

        overlay.HideEnemyHoverSkillInfo(this);
    }

    private BattleSkillInfoOverlay GetSkillOverlay()
    {
        if (cachedSkillOverlay != null)
            return cachedSkillOverlay;

        cachedSkillOverlay = FindObjectOfType<BattleSkillInfoOverlay>(true);
        return cachedSkillOverlay;
    }

    private void OnDestroy()
    {
        if (dashTexture != null)
            Destroy(dashTexture);

        if (solidTexture != null)
            Destroy(solidTexture);

        if (curveRuntimeMaterial != null)
            Destroy(curveRuntimeMaterial);

        if (hoverOutlineMaterial != null)
            Destroy(hoverOutlineMaterial);

        if (arrowMesh != null)
            Destroy(arrowMesh);

        if (arrowMaterial != null)
            Destroy(arrowMaterial);

        if (arrowObj != null)
            Destroy(arrowObj);
    }
}
