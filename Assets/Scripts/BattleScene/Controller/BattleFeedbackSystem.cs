using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class BattleFeedbackSystem : MonoBehaviour
{
    private sealed class TrackedUnitUI
    {
        public RectTransform root;
        public Image fillImage;
        public RectTransform fillRect;
        public Image bufferImage;
        public RectTransform bufferRect;
        public Image flashImage;
        public float fillWidth;
        public TextMeshProUGUI healthValueText;
        public MonoBehaviour owner;
        public Transform worldAnchor;
        public bool isEnemy;
        public RectTransform statusRoot;
        public List<StatusIconView> statusIcons;
        public int lastKnownHP;
        public float currentFillRatio;
        public float currentBufferRatio;
        public Coroutine animationRoutine;
    }

    private sealed class StatusIconView
    {
        public RectTransform root;
        public Image iconImage;
        public TextMeshProUGUI stackText;
        public EventTrigger trigger;
        public BattleStatusRuntimeEntry entry;
    }

    private sealed class StatusTooltipView
    {
        public RectTransform root;
        public Image backgroundImage;
        public Image iconImage;
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI bodyText;
    }

    private sealed class DamagePopupView
    {
        public RectTransform root;
        public TextMeshProUGUI ratioText;
        public TextMeshProUGUI modifierText;
        public TextMeshProUGUI mainText;
        public List<TextMeshProUGUI> ratioOutlineTexts;
        public List<TextMeshProUGUI> modifierOutlineTexts;
        public List<TextMeshProUGUI> outlineTexts;
        public Vector2 initialScreenOffset;
    }

    public sealed class ClashDiceDisplayHandle
    {
        internal RectTransform root;
        internal TextMeshProUGUI rangeText;
        internal List<TextMeshProUGUI> rangeOutlines;
        internal TextMeshProUGUI valueText;
        internal List<TextMeshProUGUI> valueOutlines;
        internal TextMeshProUGUI modifierText;
        internal List<TextMeshProUGUI> modifierOutlines;
        internal Transform worldAnchor;
        internal Vector3 worldOffset;
        internal Color rangeColor;
        internal Color valueColor;
        internal Color modifierColor;
        internal bool revealRequested;
        internal bool hideRequested;
        internal int revealedValue;
        internal int revealedModifier;
        internal Coroutine animationRoutine;
    }

    [Header("Canvas")]
    [SerializeField] private int canvasSortOrder = 5000;
    [SerializeField] private Camera worldCamera;

    [Header("Damage Popup")]
    [SerializeField] private TMP_FontAsset damageFont;
    [SerializeField] private float damageFontSize = 42f;
    [SerializeField] private float damageRatioFontSize = 20f;
    [SerializeField] private FontStyles damageFontStyle = FontStyles.Bold;
    [SerializeField] private FontStyles damageRatioFontStyle = FontStyles.Bold;
    [SerializeField] private Color allyDamageColor = new Color(1f, 0.45f, 0.4f, 1f);
    [SerializeField] private Color enemyDamageColor = new Color(1f, 0.9f, 0.35f, 1f);
    [SerializeField] private Color healingPopupColor = new Color(0.35f, 1f, 0.45f, 1f);
    [SerializeField] private Color damageRatioColor = new Color(1f, 1f, 1f, 0.96f);
    [SerializeField] private Vector2 damageRatioOffset = new Vector2(0f, 24f);
    [SerializeField] private Color damageModifierColor = new Color(1f, 1f, 1f, 0.96f);
    [SerializeField] private Vector2 damageModifierOffset = new Vector2(0f, -24f);
    [SerializeField] private Vector3 damageWorldOffset = new Vector3(0f, 1.4f, 0f);
    [SerializeField] private Vector2 damageHorizontalSpeedRange = new Vector2(90f, 180f);
    [SerializeField] private Vector2 damageUpwardSpeedRange = new Vector2(170f, 260f);
    [SerializeField] private Vector2 damagePopupScreenJitterRange = new Vector2(18f, 56f);
    [SerializeField, Range(0f, 85f)] private float damagePopupLaunchAngleJitter = 42f;
    [SerializeField] private float damageGravity = 520f;
    [SerializeField] private float damagePopupLifetime = 0.8f;
    [SerializeField] private float damageFadeStartNormalized = 0.45f;
    [SerializeField] private float damageScale = 1f;
    [SerializeField] private bool damagePopupScaleWithCamera = true;
    [SerializeField, Min(0f)] private float damageCameraScaleInfluence = 1f;
    [SerializeField, Min(0.1f)] private float minDamageCameraScale = 0.75f;
    [SerializeField, Min(0.1f)] private float maxDamageCameraScale = 2.5f;
    [SerializeField] private Color damageOutlineColor = Color.white;
    [SerializeField, Range(0f, 1f)] private float damageOutlineWidth = 0.22f;

    [Header("Clash Dice UI")]
    [SerializeField] private TMP_FontAsset clashDiceValueFont;
    [SerializeField] private TMP_FontAsset clashDiceRangeFont;
    [SerializeField] private float clashDiceValueFontSize = 38f;
    [SerializeField] private float clashDiceRangeFontSize = 24f;
    [SerializeField] private FontStyles clashDiceValueFontStyle = FontStyles.Bold;
    [SerializeField] private FontStyles clashDiceRangeFontStyle = FontStyles.Bold;
    [SerializeField] private Color allyClashDiceValueColor = new Color(0.72f, 1f, 0.95f, 1f);
    [SerializeField] private Color enemyClashDiceValueColor = new Color(1f, 0.88f, 0.32f, 1f);
    [SerializeField] private Color allyClashDiceRangeColor = new Color(0.82f, 0.94f, 1f, 0.95f);
    [SerializeField] private Color enemyClashDiceRangeColor = new Color(1f, 0.92f, 0.7f, 0.95f);
    [SerializeField] private Color obsoleteClashDiceRangeColor = new Color(0.55f, 0.55f, 0.55f, 0.95f);
    [SerializeField] private TMP_FontAsset clashDiceModifierFont;
    [SerializeField] private float clashDiceModifierFontSize = 20f;
    [SerializeField] private FontStyles clashDiceModifierFontStyle = FontStyles.Bold;
    [SerializeField] private Color clashDiceModifierColor = new Color(1f, 1f, 1f, 0.96f);
    [SerializeField] private Vector2 clashDiceModifierOffset = new Vector2(0f, -24f);
    [SerializeField] private Color clashDiceOutlineColor = new Color(0f, 0f, 0f, 0.85f);
    [SerializeField, Range(0f, 1f)] private float clashDiceOutlineWidth = 0.22f;
    [SerializeField] private Vector3 allyClashDiceWorldOffset = new Vector3(-0.6f, 1.55f, 0f);
    [SerializeField] private Vector3 enemyClashDiceWorldOffset = new Vector3(0.6f, 1.55f, 0f);
    [SerializeField, Min(0.01f)] private float clashDiceRangeFadeInDuration = 0.12f;
    [SerializeField, Min(0.01f)] private float clashDiceValueRevealDuration = 0.16f;
    [SerializeField, Min(0.01f)] private float clashDiceFadeOutDuration = 0.12f;
    [SerializeField, Min(0.1f)] private float clashDiceRangeStartScale = 1.12f;
    [SerializeField, Min(0.1f)] private float clashDiceRangeEndScale = 1f;
    [SerializeField, Min(0.1f)] private float clashDiceValueStartScale = 1.7f;
    [SerializeField, Min(0.1f)] private float clashDiceValueEndScale = 1f;
    [SerializeField] private bool clashDiceScaleWithCamera = true;

    [Header("Health Bar")]
    [SerializeField] private Vector2 healthBarSize = new Vector2(110f, 14f);
    [SerializeField] private Vector3 healthBarWorldOffset = new Vector3(0f, -1.2f, 0f);
    [SerializeField] private Color allyHealthFillColor = new Color(0.3f, 1f, 0.55f, 1f);
    [SerializeField] private Color enemyHealthFillColor = new Color(1f, 0.35f, 0.35f, 1f);
    [SerializeField] private Color healthBarBackgroundColor = new Color(0f, 0f, 0f, 0.72f);
    [SerializeField] private Color healthBarBorderColor = new Color(1f, 1f, 1f, 0.95f);
    [SerializeField] private TMP_FontAsset healthValueFont;
    [SerializeField] private float healthValueFontSize = 14f;
    [SerializeField] private Color healthValueColor = Color.white;
    [SerializeField, Min(0f)] private float healthBarInnerPadding = 1.5f;
    [SerializeField] private Color healthBarDamageBufferColor = new Color(1f, 0.95f, 0.95f, 0.7f);
    [SerializeField] private Color healthBarFlashColor = new Color(1f, 1f, 1f, 0.85f);
    [SerializeField, Min(0.01f)] private float healthBarFrontEaseDuration = 0.12f;
    [SerializeField, Min(0f)] private float healthBarBufferDelay = 0.05f;
    [SerializeField, Min(0.01f)] private float healthBarBufferEaseDuration = 0.28f;
    [SerializeField, Min(0.01f)] private float healthBarFlashDuration = 0.16f;
    [SerializeField, Min(1f)] private float healthBarPunchScale = 1.08f;

    [Header("Status UI")]
    [SerializeField] private Vector2 statusIconSize = new Vector2(28f, 28f);
    [SerializeField] private Vector2 statusRowOffset = new Vector2(0f, -26f);
    [SerializeField] private float statusIconSpacing = 6f;
    [SerializeField] private TMP_FontAsset statusStackFont;
    [SerializeField] private float statusStackFontSize = 16f;
    [SerializeField] private Color statusStackTextColor = Color.white;
    [SerializeField] private Sprite statusFallbackSprite;

    [Header("Status Tooltip")]
    [SerializeField] private Vector2 statusTooltipSize = new Vector2(320f, 120f);
    [SerializeField] private Vector2 statusTooltipOffset = new Vector2(0f, 72f);
    [SerializeField] private Sprite statusTooltipBackgroundSprite;
    [SerializeField] private Color statusTooltipBackgroundColor = new Color(0.08f, 0.1f, 0.14f, 0.94f);
    [SerializeField] private TMP_FontAsset statusTooltipTitleFont;
    [SerializeField] private TMP_FontAsset statusTooltipBodyFont;
    [SerializeField] private float statusTooltipTitleFontSize = 24f;
    [SerializeField] private float statusTooltipBodyFontSize = 18f;
    [SerializeField] private Color statusTooltipTitleColor = Color.white;
    [SerializeField] private Color statusTooltipBodyColor = new Color(0.92f, 0.96f, 1f, 1f);

    [Header("Health Bar Hit FX")]
    [SerializeField, Min(0)] private short healthBarHitBurstCount = 10;
    [SerializeField, Min(0.01f)] private float healthBarHitParticleSpeed = 1.25f;
    [SerializeField, Min(0.01f)] private float healthBarHitParticleLifetime = 0.3f;
    [SerializeField, Min(0.01f)] private float healthBarHitParticleSize = 0.055f;
    [SerializeField, Min(0f)] private float healthBarHitParticleFadeDuration = 0.18f;
    [SerializeField, Min(0f)] private float healthBarHitSpawnRadius = 0.04f;
    [SerializeField] private int healthBarHitSortingOrder = 430;

    [Header("Death Effect")]
    [SerializeField] private Vector3 deathDirection = new Vector3(0f, 1f, 0f);
    [SerializeField, Min(1)] private int deathBurstCount = 36;
    [SerializeField, Min(0.01f)] private float deathParticleMinSpeed = 1.6f;
    [SerializeField, Min(0.01f)] private float deathParticleMaxSpeed = 2.8f;
    [SerializeField, Min(0.01f)] private float deathParticleLifetime = 1.15f;
    [SerializeField, Min(0.01f)] private float deathParticleSize = 0.09f;
    [SerializeField, Min(0f)] private float deathParticleFadeDuration = 0.35f;
    [SerializeField, Range(0f, 90f)] private float deathSpreadAngle = 38f;
    [SerializeField, Min(0f)] private float deathGravity = 0.08f;
    [SerializeField, Min(0f)] private float deathSpawnRadius = 0.12f;
    [SerializeField, Min(0f)] private float deathDirectionalBoost = 3.2f;
    [SerializeField, Min(0f)] private float deathDirectionalRandomness = 0.45f;
    [SerializeField, Min(0f)] private float deathDirectionalDrag = 0.12f;
    [SerializeField] private int deathSortingOrder = 420;
    [SerializeField] private Color deathParticleColor = Color.white;

    [Header("Death Slow Motion")]
    [SerializeField] private bool enableDeathSlowMotion = true;
    [SerializeField, Range(0.01f, 1f)] private float deathSlowMotionScale = 0.18f;
    [SerializeField, Min(0f)] private float deathSlowMotionDuration = 0.22f;
    [SerializeField, Min(0f)] private float deathSlowMotionRecoveryDuration = 0.16f;
    [SerializeField] private AnimationCurve deathSlowMotionRecoveryCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Particle Collision")]
    [SerializeField] private bool enableParticleGroundCollision = true;
    [SerializeField] private LayerMask particleCollisionLayers = ~0;
    [SerializeField, Range(0f, 1f)] private float particleCollisionBounce = 0.45f;
    [SerializeField, Range(0f, 1f)] private float particleCollisionDampen = 0.35f;
    [SerializeField, Range(0f, 1f)] private float particleCollisionLifetimeLoss = 0.05f;
    [SerializeField, Range(0f, 1f)] private float particleCollisionRadiusScale = 0.9f;
    [SerializeField, Min(0f)] private float particleCollisionMinKillSpeed = 0.05f;
    [SerializeField, Min(1)] private int particleCollisionMaxShapes = 256;

    private readonly Dictionary<BattleAllyController, TrackedUnitUI> allyUIs = new Dictionary<BattleAllyController, TrackedUnitUI>();
    private readonly Dictionary<BattleEnemyController, TrackedUnitUI> enemyUIs = new Dictionary<BattleEnemyController, TrackedUnitUI>();

    private Canvas overlayCanvas;
    private RectTransform overlayRoot;
    private Material particleMaterial;
    private Texture2D whiteTexture;
    private Camera presentationCameraOverride;
    private float presentationDamageScaleOverride = 1f;
    private bool damageScaleBaselineCaptured;
    private float baseDamageOrthographicSize;
    private float baseDamageFieldOfView;
    private Coroutine deathSlowMotionRoutine;
    private float deathSlowMotionRestoreScale = 1f;
    private float deathSlowMotionBaseFixedStep = 0.02f;
    private StatusTooltipView statusTooltipView;
    private StatusIconView activeStatusTooltipIcon;

    private void Awake()
    {
        EnsureCanvas();
        EnsureParticleResources();
        EnsureStatusTooltip();
    }

    private void OnDisable()
    {
        if (deathSlowMotionRoutine == null)
            return;

        StopCoroutine(deathSlowMotionRoutine);
        deathSlowMotionRoutine = null;
        Time.timeScale = Mathf.Max(0.01f, deathSlowMotionRestoreScale);
        Time.fixedDeltaTime = Mathf.Max(0.0001f, deathSlowMotionBaseFixedStep) * Mathf.Max(0.01f, deathSlowMotionRestoreScale);
    }

    private void LateUpdate()
    {
        UpdateTrackedPositions(allyUIs, healthBarWorldOffset);
        UpdateTrackedPositions(enemyUIs, healthBarWorldOffset);
    }

    public void BindCombatants(BattleAllyController[] allies, BattleEnemyController[] enemies)
    {
        EnsureCanvas();
        EnsureParticleResources();
        EnsureStatusTooltip();

        HashSet<BattleAllyController> allySet = new HashSet<BattleAllyController>();
        foreach (BattleAllyController ally in allies)
        {
            if (ally == null || ally.IsDead)
                continue;

            allySet.Add(ally);
            RegisterAlly(ally);
        }

        HashSet<BattleEnemyController> enemySet = new HashSet<BattleEnemyController>();
        foreach (BattleEnemyController enemy in enemies)
        {
            if (enemy == null || enemy.IsDead)
                continue;

            enemySet.Add(enemy);
            RegisterEnemy(enemy);
        }

        CleanupMissingAllies(allySet);
        CleanupMissingEnemies(enemySet);
    }

    public void ShowCustomPopup(Vector3 worldPosition, string popupText, Color popupColor)
    {
        ShowValuePopup(worldPosition, popupText, popupColor);
    }

    public void ShowDamageResultPopup(Vector3 worldPosition, int amount, bool targetIsEnemy, int ratioPercent, int powerModifierAmount = 0)
    {
        ShowDamagePopup(worldPosition, amount, targetIsEnemy, ratioPercent, powerModifierAmount);
    }

    public void SetPresentationCamera(Camera cameraOverride, float damageScaleOverride = 1f)
    {
        presentationCameraOverride = cameraOverride;
        presentationDamageScaleOverride = Mathf.Max(0.1f, damageScaleOverride);
    }

    public float GetClashDiceValueRevealDuration()
    {
        return Mathf.Max(0.01f, clashDiceValueRevealDuration);
    }

    public ClashDiceDisplayHandle ShowClashDiceRange(
        Transform actor,
        bool isEnemy,
        EnemySkill_SO.DicePowerRange range,
        EnemySkill_SO.DicePowerRange? baseRange = null)
    {
        if (actor == null)
            return null;

        EnsureCanvas();

        GameObject rootGo = new GameObject("ClashDiceDisplay", typeof(RectTransform));
        rootGo.transform.SetParent(overlayRoot, false);

        RectTransform root = rootGo.GetComponent<RectTransform>();
        root.pivot = new Vector2(0.5f, 0.5f);
        root.sizeDelta = new Vector2(
            Mathf.Max(180f, clashDiceValueFontSize * 3.4f),
            Mathf.Max(84f, clashDiceValueFontSize * 1.9f));

        ClashDiceDisplayHandle handle = new ClashDiceDisplayHandle
        {
            root = root,
            worldAnchor = actor,
            worldOffset = isEnemy ? enemyClashDiceWorldOffset : allyClashDiceWorldOffset,
            rangeColor = isEnemy ? enemyClashDiceRangeColor : allyClashDiceRangeColor,
            valueColor = isEnemy ? enemyClashDiceValueColor : allyClashDiceValueColor,
            modifierColor = clashDiceModifierColor
        };

        string rangeText = BuildClashDiceRangeText(baseRange, range, handle.rangeColor);
        handle.rangeOutlines = CreateOutlinedTextGroup(
            root,
            rangeText,
            clashDiceRangeFont,
            clashDiceRangeFontSize,
            clashDiceRangeFontStyle,
            clashDiceOutlineColor,
            clashDiceOutlineWidth,
            out handle.rangeText);
        handle.valueOutlines = CreateOutlinedTextGroup(
            root,
            string.Empty,
            clashDiceValueFont,
            clashDiceValueFontSize,
            clashDiceValueFontStyle,
            clashDiceOutlineColor,
            clashDiceOutlineWidth,
            out handle.valueText);
        handle.modifierOutlines = CreateOutlinedTextGroup(
            root,
            string.Empty,
            clashDiceModifierFont != null ? clashDiceModifierFont : clashDiceRangeFont,
            clashDiceModifierFontSize,
            clashDiceModifierFontStyle,
            clashDiceOutlineColor,
            clashDiceOutlineWidth,
            out handle.modifierText);
        if (handle.modifierText != null)
            handle.modifierText.rectTransform.anchoredPosition = clashDiceModifierOffset;
        if (handle.modifierOutlines != null)
        {
            for (int i = 0; i < handle.modifierOutlines.Count; i++)
            {
                if (handle.modifierOutlines[i] != null)
                    handle.modifierOutlines[i].rectTransform.anchoredPosition += clashDiceModifierOffset;
            }
        }

        SetTextGroupVisual(handle.rangeText, handle.rangeOutlines, handle.rangeColor, 0f, clashDiceRangeStartScale);
        SetTextGroupVisual(handle.valueText, handle.valueOutlines, handle.valueColor, 0f, clashDiceValueStartScale);
        SetTextGroupVisual(handle.modifierText, handle.modifierOutlines, handle.modifierColor, 0f, 1f);
        handle.animationRoutine = StartCoroutine(AnimateClashDiceDisplay(handle));
        return handle;
    }

    private string BuildClashDiceRangeText(
        EnemySkill_SO.DicePowerRange? baseRange,
        EnemySkill_SO.DicePowerRange displayedRange,
        Color displayedColor)
    {
        if (!baseRange.HasValue
            || (baseRange.Value.minPower == displayedRange.minPower && baseRange.Value.maxPower == displayedRange.maxPower))
            return $"{displayedRange.minPower}~{displayedRange.maxPower}";

        string obsoleteRange = BuildColoredToken($"<s>{baseRange.Value.minPower}~{baseRange.Value.maxPower}</s>", obsoleteClashDiceRangeColor);
        string currentRange = BuildColoredToken($"{displayedRange.minPower}~{displayedRange.maxPower}", displayedColor);
        return $"{obsoleteRange} {currentRange}";
    }

    private static string BuildColoredToken(string content, Color color)
    {
        return $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{content}</color>";
    }

    public void RevealClashDiceValue(ClashDiceDisplayHandle handle, int value, int modifierAmount = 0)
    {
        if (handle == null)
            return;

        handle.revealedValue = value;
        handle.revealedModifier = modifierAmount;
        handle.revealRequested = true;
    }

    public void HideClashDiceDisplay(ClashDiceDisplayHandle handle)
    {
        if (handle == null)
            return;

        handle.hideRequested = true;
    }

    private void RegisterAlly(BattleAllyController ally)
    {
        if (ally == null)
            return;

        bool created = false;
        if (!allyUIs.ContainsKey(ally))
        {
            ally.Damaged += OnAllyDamaged;
            ally.HealthChanged += OnAllyHealthChanged;
            ally.Died += OnAllyDied;
            ally.StatusChanged += OnAllyStatusChanged;
            allyUIs.Add(ally, CreateTrackedUnitUI(ally, false));
            created = true;
        }

        if (created)
            SetHealthBarImmediate(allyUIs[ally], ally.CurrentHP, ally.MaxHP);
        else
            RefreshHealthBar(allyUIs[ally], ally.CurrentHP, ally.MaxHP);

        RefreshStatusIcons(allyUIs[ally], ally.StatusEntries);
    }

    private void RegisterEnemy(BattleEnemyController enemy)
    {
        if (enemy == null)
            return;

        bool created = false;
        if (!enemyUIs.ContainsKey(enemy))
        {
            enemy.Damaged += OnEnemyDamaged;
            enemy.HealthChanged += OnEnemyHealthChanged;
            enemy.Died += OnEnemyDied;
            enemy.StatusChanged += OnEnemyStatusChanged;
            enemyUIs.Add(enemy, CreateTrackedUnitUI(enemy, true));
            created = true;
        }

        if (created)
            SetHealthBarImmediate(enemyUIs[enemy], enemy.CurrentHP, enemy.MaxHP);
        else
            RefreshHealthBar(enemyUIs[enemy], enemy.CurrentHP, enemy.MaxHP);

        RefreshStatusIcons(enemyUIs[enemy], enemy.StatusEntries);
    }

    private TrackedUnitUI CreateTrackedUnitUI(MonoBehaviour owner, bool isEnemy)
    {
        GameObject rootGo = new GameObject($"{owner.gameObject.name}_HealthBar", typeof(RectTransform));
        RectTransform root = rootGo.GetComponent<RectTransform>();
        root.SetParent(overlayRoot, false);
        root.sizeDelta = healthBarSize;
        root.pivot = new Vector2(0.5f, 0.5f);

        Image border = CreateImage("Border", root, healthBarBorderColor);
        RectTransform borderRect = border.rectTransform;
        borderRect.anchorMin = Vector2.zero;
        borderRect.anchorMax = Vector2.one;
        borderRect.offsetMin = Vector2.zero;
        borderRect.offsetMax = Vector2.zero;

        Image background = CreateImage("Background", root, healthBarBackgroundColor);
        RectTransform backgroundRect = background.rectTransform;
        backgroundRect.anchorMin = new Vector2(0.5f, 0.5f);
        backgroundRect.anchorMax = new Vector2(0.5f, 0.5f);
        backgroundRect.pivot = new Vector2(0.5f, 0.5f);
        backgroundRect.anchoredPosition = Vector2.zero;
        backgroundRect.sizeDelta = healthBarSize - Vector2.one * (healthBarInnerPadding * 2f);

        Image buffer = CreateImage("DamageBuffer", background.rectTransform, healthBarDamageBufferColor);
        RectTransform bufferRect = buffer.rectTransform;
        bufferRect.anchorMin = new Vector2(0f, 0f);
        bufferRect.anchorMax = new Vector2(0f, 1f);
        bufferRect.pivot = new Vector2(0f, 0.5f);
        bufferRect.anchoredPosition = Vector2.zero;
        bufferRect.sizeDelta = new Vector2(backgroundRect.sizeDelta.x, 0f);

        Image fill = CreateImage("Fill", background.rectTransform, isEnemy ? enemyHealthFillColor : allyHealthFillColor);
        RectTransform fillRect = fill.rectTransform;
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(0f, 1f);
        fillRect.pivot = new Vector2(0f, 0.5f);
        fillRect.anchoredPosition = Vector2.zero;
        fillRect.sizeDelta = new Vector2(backgroundRect.sizeDelta.x, 0f);
        fill.type = Image.Type.Simple;

        Image flash = CreateImage("Flash", background.rectTransform, Color.clear);
        RectTransform flashRect = flash.rectTransform;
        flashRect.anchorMin = Vector2.zero;
        flashRect.anchorMax = Vector2.one;
        flashRect.offsetMin = Vector2.zero;
        flashRect.offsetMax = Vector2.zero;

        TextMeshProUGUI healthValueText = CreateHealthValueText(root);
        RectTransform statusRoot = CreateStatusRoot(root);

        return new TrackedUnitUI
        {
            root = root,
            fillImage = fill,
            fillRect = fillRect,
            bufferImage = buffer,
            bufferRect = bufferRect,
            flashImage = flash,
            fillWidth = backgroundRect.sizeDelta.x,
            healthValueText = healthValueText,
            owner = owner,
            worldAnchor = owner.transform,
            isEnemy = isEnemy,
            statusRoot = statusRoot,
            statusIcons = new List<StatusIconView>(),
            currentFillRatio = 1f,
            currentBufferRatio = 1f
        };
    }

    private static Image CreateImage(string name, Transform parent, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.color = color;
        return image;
    }

    private TextMeshProUGUI CreateHealthValueText(RectTransform parent)
    {
        GameObject go = new GameObject("HealthValue", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
        if (healthValueFont != null)
            text.font = healthValueFont;
        text.fontSize = healthValueFontSize;
        text.color = healthValueColor;
        text.alignment = TextAlignmentOptions.Center;
        return text;
    }

    private RectTransform CreateStatusRoot(RectTransform parent)
    {
        GameObject go = new GameObject("Statuses", typeof(RectTransform));
        go.transform.SetParent(parent, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = statusRowOffset;
        rect.sizeDelta = new Vector2(healthBarSize.x + 80f, statusIconSize.y + 10f);
        return rect;
    }

    private void RefreshHealthBar(TrackedUnitUI ui, int currentHP, int maxHP)
    {
        if (ui == null || ui.fillRect == null)
            return;

        if (ui.lastKnownHP == 0 && maxHP > 0)
        {
            SetHealthBarImmediate(ui, currentHP, maxHP);
            return;
        }

        if (currentHP == ui.lastKnownHP)
        {
            UpdateHealthValueText(ui, currentHP, maxHP);
            return;
        }

        float ratio = maxHP > 0 ? Mathf.Clamp01((float)currentHP / maxHP) : 0f;
        bool tookDamage = currentHP < ui.lastKnownHP;

        if (ui.animationRoutine != null)
            StopCoroutine(ui.animationRoutine);
        ui.animationRoutine = StartCoroutine(AnimateHealthBar(ui, currentHP, maxHP, ratio, tookDamage));
    }

    private void SetHealthBarImmediate(TrackedUnitUI ui, int currentHP, int maxHP)
    {
        if (ui == null || ui.fillRect == null)
            return;

        float ratio = maxHP > 0 ? Mathf.Clamp01((float)currentHP / maxHP) : 0f;
        ui.fillRect.sizeDelta = new Vector2(ui.fillWidth * ratio, ui.fillRect.sizeDelta.y);
        if (ui.bufferRect != null)
            ui.bufferRect.sizeDelta = new Vector2(ui.fillWidth * ratio, ui.bufferRect.sizeDelta.y);
        if (ui.flashImage != null)
            ui.flashImage.color = Color.clear;
        if (ui.root != null)
            ui.root.localScale = Vector3.one;
        ui.currentFillRatio = ratio;
        ui.currentBufferRatio = ratio;
        ui.lastKnownHP = currentHP;
        ui.animationRoutine = null;
        UpdateHealthValueText(ui, currentHP, maxHP);
    }

    private void UpdateHealthValueText(TrackedUnitUI ui, int currentHP, int maxHP)
    {
        if (ui.healthValueText != null)
            ui.healthValueText.text = $"{currentHP} / {maxHP}";
    }

    private void RefreshStatusIcons(TrackedUnitUI ui, IReadOnlyList<BattleStatusRuntimeEntry> statusEntries)
    {
        if (ui == null || ui.statusRoot == null)
            return;

        int visibleCount = 0;
        if (statusEntries != null)
        {
            for (int i = 0; i < statusEntries.Count; i++)
            {
                BattleStatusRuntimeEntry entry = statusEntries[i];
                if (entry == null || entry.stacks <= 0)
                    continue;

                StatusIconView iconView = GetOrCreateStatusIcon(ui, visibleCount);
                if (iconView.iconImage != null)
                {
                    iconView.iconImage.sprite = entry.IconSprite != null ? entry.IconSprite : statusFallbackSprite;
                    iconView.iconImage.color = entry.IconSprite != null ? Color.white : entry.AccentColor;
                    iconView.iconImage.type = iconView.iconImage.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
                }

                iconView.entry = entry;

                if (iconView.stackText != null)
                {
                    iconView.stackText.text = entry.stacks.ToString();
                    iconView.stackText.color = statusStackTextColor;
                }

                if (iconView.root != null)
                    iconView.root.gameObject.SetActive(true);

                visibleCount++;
            }
        }

        for (int i = visibleCount; i < ui.statusIcons.Count; i++)
        {
            if (ui.statusIcons[i] == null)
                continue;

            ui.statusIcons[i].entry = null;
            if (activeStatusTooltipIcon == ui.statusIcons[i])
                HideStatusTooltip();

            if (ui.statusIcons[i].root != null)
                ui.statusIcons[i].root.gameObject.SetActive(false);
        }

        LayoutStatusIcons(ui, visibleCount);
    }

    private StatusIconView GetOrCreateStatusIcon(TrackedUnitUI ui, int index)
    {
        while (ui.statusIcons.Count <= index)
        {
            GameObject rootGo = new GameObject($"Status_{ui.statusIcons.Count}", typeof(RectTransform));
            rootGo.transform.SetParent(ui.statusRoot, false);
            RectTransform root = rootGo.GetComponent<RectTransform>();
            root.sizeDelta = statusIconSize;
            root.pivot = new Vector2(0.5f, 1f);

            Image icon = CreateImage("Icon", root, Color.white);
            RectTransform iconRect = icon.rectTransform;
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.offsetMin = Vector2.zero;
            iconRect.offsetMax = Vector2.zero;
            EventTrigger trigger = icon.gameObject.AddComponent<EventTrigger>();

            GameObject stackGo = new GameObject("Stack", typeof(RectTransform), typeof(TextMeshProUGUI));
            stackGo.transform.SetParent(root, false);
            RectTransform stackRect = stackGo.GetComponent<RectTransform>();
            stackRect.anchorMin = Vector2.zero;
            stackRect.anchorMax = Vector2.one;
            stackRect.offsetMin = Vector2.zero;
            stackRect.offsetMax = Vector2.zero;

            TextMeshProUGUI stackText = stackGo.GetComponent<TextMeshProUGUI>();
            if (statusStackFont != null)
                stackText.font = statusStackFont;
            stackText.fontSize = statusStackFontSize;
            stackText.color = statusStackTextColor;
            stackText.alignment = TextAlignmentOptions.BottomRight;
            stackText.fontStyle = FontStyles.Bold;
            stackText.raycastTarget = false;

            ui.statusIcons.Add(new StatusIconView
            {
                root = root,
                iconImage = icon,
                stackText = stackText,
                trigger = trigger
            });

            ConfigureStatusIconEvents(ui.statusIcons[ui.statusIcons.Count - 1]);
        }

        return ui.statusIcons[index];
    }

    private void ConfigureStatusIconEvents(StatusIconView view)
    {
        if (view == null || view.trigger == null)
            return;

        view.trigger.triggers.Clear();
        AddStatusIconEvent(view.trigger, EventTriggerType.PointerEnter, _ => OnStatusIconPointerEnter(view));
        AddStatusIconEvent(view.trigger, EventTriggerType.PointerExit, _ => OnStatusIconPointerExit(view));
        AddStatusIconEvent(view.trigger, EventTriggerType.PointerClick, _ => OnStatusIconPointerClick(view));
    }

    private static void AddStatusIconEvent(EventTrigger trigger, EventTriggerType eventType, System.Action<BaseEventData> callback)
    {
        EventTrigger.Entry entry = new EventTrigger.Entry { eventID = eventType };
        entry.callback.AddListener(data => callback?.Invoke(data));
        trigger.triggers.Add(entry);
    }

    private void OnStatusIconPointerEnter(StatusIconView view)
    {
        if (!ShouldUseHoverStatusTooltip())
            return;

        ShowStatusTooltip(view);
    }

    private void OnStatusIconPointerExit(StatusIconView view)
    {
        if (!ShouldUseHoverStatusTooltip())
            return;

        if (activeStatusTooltipIcon == view)
            HideStatusTooltip();
    }

    private void OnStatusIconPointerClick(StatusIconView view)
    {
        if (ShouldUseHoverStatusTooltip())
            return;

        if (activeStatusTooltipIcon == view && statusTooltipView != null && statusTooltipView.root != null && statusTooltipView.root.gameObject.activeSelf)
        {
            HideStatusTooltip();
            return;
        }

        ShowStatusTooltip(view);
    }

    private bool ShouldUseHoverStatusTooltip()
    {
        return !Application.isMobilePlatform;
    }

    private void LayoutStatusIcons(TrackedUnitUI ui, int visibleCount)
    {
        if (ui == null || ui.statusRoot == null)
            return;

        float slotWidth = statusIconSize.x + statusIconSpacing;
        float totalWidth = visibleCount > 0 ? (visibleCount * statusIconSize.x) + ((visibleCount - 1) * statusIconSpacing) : 0f;
        float startX = -totalWidth * 0.5f + statusIconSize.x * 0.5f;

        for (int i = 0; i < visibleCount; i++)
        {
            if (ui.statusIcons[i]?.root == null)
                continue;

            ui.statusIcons[i].root.anchoredPosition = new Vector2(startX + i * slotWidth, 0f);
        }
    }

    private IEnumerator AnimateHealthBar(TrackedUnitUI ui, int currentHP, int maxHP, float targetRatio, bool tookDamage)
    {
        if (ui == null)
            yield break;

        UpdateHealthValueText(ui, currentHP, maxHP);

        float startFillRatio = ui.currentFillRatio;
        float startBufferRatio = ui.currentBufferRatio;
        float frontDuration = Mathf.Max(0.01f, healthBarFrontEaseDuration);

        if (tookDamage)
        {
            PlayHealthBarHitFx(ui);
            StartCoroutine(PlayHealthBarFlash(ui));
        }

        for (float time = 0f; time < frontDuration; time += Time.deltaTime)
        {
            if (ui.fillRect == null)
                yield break;

            float normalized = Mathf.Clamp01(time / frontDuration);
            float eased = 1f - Mathf.Pow(1f - normalized, 3f);
            float ratio = Mathf.Lerp(startFillRatio, targetRatio, eased);
            SetFillWidth(ui.fillRect, ui.fillWidth, ratio);
            ui.currentFillRatio = ratio;

            if (!tookDamage && ui.bufferRect != null)
            {
                SetFillWidth(ui.bufferRect, ui.fillWidth, ratio);
                ui.currentBufferRatio = ratio;
            }

            if (ui.root != null)
            {
                float punch = tookDamage ? Mathf.Lerp(healthBarPunchScale, 1f, normalized) : 1f;
                ui.root.localScale = Vector3.one * punch;
            }

            yield return null;
        }

        SetFillWidth(ui.fillRect, ui.fillWidth, targetRatio);
        ui.currentFillRatio = targetRatio;

        if (!tookDamage)
        {
            if (ui.bufferRect != null)
                SetFillWidth(ui.bufferRect, ui.fillWidth, targetRatio);
            ui.currentBufferRatio = targetRatio;
            if (ui.root != null)
                ui.root.localScale = Vector3.one;
            ui.lastKnownHP = currentHP;
            ui.animationRoutine = null;
            yield break;
        }

        if (healthBarBufferDelay > 0f)
            yield return new WaitForSeconds(healthBarBufferDelay);

        float bufferDuration = Mathf.Max(0.01f, healthBarBufferEaseDuration);
        for (float time = 0f; time < bufferDuration; time += Time.deltaTime)
        {
            if (ui.bufferRect == null)
                yield break;

            float normalized = Mathf.Clamp01(time / bufferDuration);
            float eased = Mathf.SmoothStep(0f, 1f, normalized);
            float ratio = Mathf.Lerp(startBufferRatio, targetRatio, eased);
            SetFillWidth(ui.bufferRect, ui.fillWidth, ratio);
            ui.currentBufferRatio = ratio;
            yield return null;
        }

        if (ui.bufferRect != null)
            SetFillWidth(ui.bufferRect, ui.fillWidth, targetRatio);
        ui.currentBufferRatio = targetRatio;
        if (ui.root != null)
            ui.root.localScale = Vector3.one;
        ui.lastKnownHP = currentHP;
        ui.animationRoutine = null;
    }

    private IEnumerator PlayHealthBarFlash(TrackedUnitUI ui)
    {
        if (ui == null || ui.flashImage == null)
            yield break;

        float duration = Mathf.Max(0.01f, healthBarFlashDuration);
        for (float time = 0f; time < duration; time += Time.deltaTime)
        {
            if (ui.flashImage == null)
                yield break;

            float normalized = Mathf.Clamp01(time / duration);
            float alpha = (1f - normalized) * healthBarFlashColor.a;
            Color color = healthBarFlashColor;
            color.a = alpha;
            ui.flashImage.color = color;
            yield return null;
        }

        if (ui.flashImage != null)
            ui.flashImage.color = Color.clear;
    }

    private void SetFillWidth(RectTransform rect, float width, float ratio)
    {
        if (rect == null)
            return;

        rect.sizeDelta = new Vector2(width * Mathf.Clamp01(ratio), rect.sizeDelta.y);
    }

    private void UpdateTrackedPositions<T>(Dictionary<T, TrackedUnitUI> units, Vector3 worldOffset) where T : MonoBehaviour
    {
        Camera cam = ResolveCamera();
        if (cam == null)
            return;

        foreach (TrackedUnitUI ui in units.Values)
        {
            if (ui == null || ui.root == null || ui.owner == null)
                continue;

            if (!ui.owner.gameObject.activeInHierarchy)
            {
                ui.root.gameObject.SetActive(false);
                continue;
            }

            Vector3 worldPosition = ui.worldAnchor.position + worldOffset;
            Vector3 screenPosition = cam.WorldToScreenPoint(worldPosition);
            bool visible = screenPosition.z > 0f;
            ui.root.gameObject.SetActive(visible);
            if (!visible)
                continue;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(overlayRoot, screenPosition, null, out Vector2 localPoint);
            ui.root.anchoredPosition = localPoint;
        }
    }

    private void OnAllyDamaged(BattleAllyController ally, int damage)
    {
        if (ally == null || damage <= 0)
            return;

        ShowDamagePopup(
            BattleActorVisualUtility.GetActorCenterWorld(ally.transform) + damageWorldOffset,
            damage,
            false,
            ally.LastDamageModifierRatioPercent,
            0);
    }

    private void OnEnemyDamaged(BattleEnemyController enemy, int damage)
    {
        if (enemy == null || damage <= 0)
            return;

        ShowDamagePopup(
            BattleActorVisualUtility.GetActorCenterWorld(enemy.transform) + damageWorldOffset,
            damage,
            true,
            enemy.LastDamageModifierRatioPercent,
            0);
    }

    private void OnAllyHealthChanged(BattleAllyController ally)
    {
        if (ally != null && allyUIs.TryGetValue(ally, out TrackedUnitUI ui))
        {
            int healedAmount = ally.CurrentHP - ui.lastKnownHP;
            if (ui.lastKnownHP > 0 && healedAmount > 0)
                ShowHealingPopup(BattleActorVisualUtility.GetActorCenterWorld(ally.transform) + damageWorldOffset, healedAmount);
            RefreshHealthBar(ui, ally.CurrentHP, ally.MaxHP);
        }
    }

    private void OnEnemyHealthChanged(BattleEnemyController enemy)
    {
        if (enemy != null && enemyUIs.TryGetValue(enemy, out TrackedUnitUI ui))
        {
            int healedAmount = enemy.CurrentHP - ui.lastKnownHP;
            if (ui.lastKnownHP > 0 && healedAmount > 0)
                ShowHealingPopup(BattleActorVisualUtility.GetActorCenterWorld(enemy.transform) + damageWorldOffset, healedAmount);
            RefreshHealthBar(ui, enemy.CurrentHP, enemy.MaxHP);
        }
    }

    private void OnAllyStatusChanged(BattleAllyController ally)
    {
        if (ally != null && allyUIs.TryGetValue(ally, out TrackedUnitUI ui))
            RefreshStatusIcons(ui, ally.StatusEntries);
    }

    private void OnEnemyStatusChanged(BattleEnemyController enemy)
    {
        if (enemy != null && enemyUIs.TryGetValue(enemy, out TrackedUnitUI ui))
            RefreshStatusIcons(ui, enemy.StatusEntries);
    }

    private void OnAllyDied(BattleAllyController ally)
    {
        if (ally == null)
            return;

        TriggerDeathSlowMotion();
        PlayDeathEffect(BattleActorVisualUtility.GetActorCenterWorld(ally.transform), ally.LastHitDirection);
        HideCombatantVisuals(ally.gameObject);
        RemoveTrackedAlly(ally);
    }

    private void OnEnemyDied(BattleEnemyController enemy)
    {
        if (enemy == null)
            return;

        TriggerDeathSlowMotion();
        PlayDeathEffect(BattleActorVisualUtility.GetActorCenterWorld(enemy.transform), enemy.LastHitDirection);
        HideCombatantVisuals(enemy.gameObject);
        RemoveTrackedEnemy(enemy);
    }

    private void TriggerDeathSlowMotion()
    {
        if (!enableDeathSlowMotion)
            return;

        if (deathSlowMotionRoutine == null)
        {
            float currentScale = Mathf.Max(0.01f, Time.timeScale);
            deathSlowMotionRestoreScale = currentScale;
            deathSlowMotionBaseFixedStep = currentScale > 0.0001f
                ? Time.fixedDeltaTime / currentScale
                : 0.02f;
        }

        if (deathSlowMotionRoutine != null)
            StopCoroutine(deathSlowMotionRoutine);

        deathSlowMotionRoutine = StartCoroutine(PlayDeathSlowMotion());
    }

    private IEnumerator PlayDeathSlowMotion()
    {
        float restoreScale = Mathf.Max(0.01f, deathSlowMotionRestoreScale);
        float baseFixedStep = Mathf.Max(0.0001f, deathSlowMotionBaseFixedStep);
        float slowScale = Mathf.Min(restoreScale, Mathf.Clamp(deathSlowMotionScale, 0.01f, 1f));

        Time.timeScale = slowScale;
        Time.fixedDeltaTime = baseFixedStep * slowScale;

        float totalDuration = Mathf.Max(0f, deathSlowMotionDuration);
        float recoveryDuration = Mathf.Clamp(deathSlowMotionRecoveryDuration, 0f, totalDuration);
        float holdDuration = Mathf.Max(0f, totalDuration - recoveryDuration);
        if (holdDuration > 0f)
            yield return new WaitForSecondsRealtime(holdDuration);

        if (recoveryDuration > 0f)
        {
            float elapsed = 0f;
            while (elapsed < recoveryDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float normalized = Mathf.Clamp01(elapsed / recoveryDuration);
                float eased = deathSlowMotionRecoveryCurve != null
                    ? deathSlowMotionRecoveryCurve.Evaluate(normalized)
                    : normalized;
                float currentScale = Mathf.Lerp(slowScale, restoreScale, eased);
                Time.timeScale = currentScale;
                Time.fixedDeltaTime = baseFixedStep * currentScale;
                yield return null;
            }
        }

        Time.timeScale = restoreScale;
        Time.fixedDeltaTime = baseFixedStep * restoreScale;
        deathSlowMotionRoutine = null;
    }

    private void ShowDamagePopup(Vector3 worldPosition, int amount, bool targetIsEnemy, int ratioPercent, int powerModifierAmount)
    {
        ShowValuePopup(
            worldPosition,
            amount.ToString(),
            targetIsEnemy ? enemyDamageColor : allyDamageColor,
            ratioPercent > 0 ? $"{ratioPercent}%" : string.Empty,
            powerModifierAmount != 0 ? $"{(powerModifierAmount > 0 ? "+" : string.Empty)}{powerModifierAmount}" : string.Empty);
    }

    private void ShowHealingPopup(Vector3 worldPosition, int amount)
    {
        ShowValuePopup(worldPosition, $"+{amount}", healingPopupColor);
    }

    private void ShowValuePopup(Vector3 worldPosition, string popupText, Color popupColor, string ratioText = "", string modifierText = "")
    {
        if (overlayRoot == null)
            return;

        GameObject popupGo = new GameObject("DamagePopup", typeof(RectTransform));
        popupGo.transform.SetParent(overlayRoot, false);

        RectTransform rect = popupGo.GetComponent<RectTransform>();
        float popupWidth = Mathf.Max(180f, damageFontSize * 3.8f);
        float popupHeight = Mathf.Max(72f, damageFontSize * 1.8f);
        rect.sizeDelta = new Vector2(popupWidth, popupHeight);
        rect.localScale = Vector3.one * damageScale;
        DamagePopupView popupView = CreateDamagePopupView(rect, popupText, popupColor, ratioText, modifierText);

        float horizontalMagnitude = Random.Range(damageHorizontalSpeedRange.x, damageHorizontalSpeedRange.y);
        float upwardMagnitude = Random.Range(damageUpwardSpeedRange.x, damageUpwardSpeedRange.y);
        float horizontalDirection = Random.value < 0.5f ? -1f : 1f;
        float launchAngleOffset = Random.Range(-damagePopupLaunchAngleJitter, damagePopupLaunchAngleJitter);
        Vector2 initialVelocity = Quaternion.Euler(0f, 0f, launchAngleOffset)
            * new Vector2(horizontalMagnitude * horizontalDirection, upwardMagnitude);

        float jitterDistance = Random.Range(damagePopupScreenJitterRange.x, damagePopupScreenJitterRange.y);
        Vector2 jitterDirection = Random.insideUnitCircle;
        if (jitterDirection.sqrMagnitude < 0.0001f)
            jitterDirection = Vector2.up;
        popupView.initialScreenOffset = jitterDirection.normalized * jitterDistance;

        StartCoroutine(AnimateDamagePopup(popupView, worldPosition, initialVelocity));
    }

    private IEnumerator AnimateDamagePopup(DamagePopupView popupView, Vector3 worldPosition, Vector2 initialVelocity)
    {
        if (popupView == null || popupView.root == null || popupView.mainText == null)
            yield break;

        RectTransform rect = popupView.root;
        TextMeshProUGUI text = popupView.mainText;
        float duration = Mathf.Max(0.05f, damagePopupLifetime);
        Color baseColor = text.color;
        Color baseOutlineColor = damageOutlineColor;
        Color baseRatioColor = popupView.ratioText != null ? popupView.ratioText.color : damageRatioColor;
        Color baseModifierColor = popupView.modifierText != null ? popupView.modifierText.color : damageModifierColor;
        for (float time = 0f; time < duration; time += Time.deltaTime)
        {
            Camera cam = ResolveCamera();
            if (rect == null || text == null || cam == null)
                yield break;

            Vector3 screenPosition = cam.WorldToScreenPoint(worldPosition);
            if (screenPosition.z > 0f)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(overlayRoot, screenPosition, null, out Vector2 localPoint);
                Vector2 gravityOffset = new Vector2(0f, -0.5f * damageGravity * time * time);
                rect.anchoredPosition = localPoint + popupView.initialScreenOffset + initialVelocity * time + gravityOffset;
            }

            rect.localScale = Vector3.one * damageScale * ResolveDamagePopupCameraScale(cam);

            float normalized = Mathf.Clamp01(time / duration);
            float alpha = normalized < damageFadeStartNormalized
                ? 1f
                : 1f - Mathf.InverseLerp(damageFadeStartNormalized, 1f, normalized);

            Color color = baseColor;
            color.a = alpha;
            text.color = color;

            if (popupView.outlineTexts != null)
            {
                Color outlineColor = baseOutlineColor;
                outlineColor.a = alpha;
                foreach (TextMeshProUGUI outline in popupView.outlineTexts)
                {
                    if (outline != null)
                        outline.color = outlineColor;
                }
            }

            if (popupView.ratioText != null)
            {
                Color ratioColor = baseRatioColor;
                ratioColor.a = alpha;
                popupView.ratioText.color = ratioColor;
            }

            if (popupView.ratioOutlineTexts != null)
            {
                Color outlineColor = baseOutlineColor;
                outlineColor.a = alpha;
                foreach (TextMeshProUGUI outline in popupView.ratioOutlineTexts)
                {
                    if (outline != null)
                        outline.color = outlineColor;
                }
            }

            if (popupView.modifierText != null)
            {
                Color modifierColor = baseModifierColor;
                modifierColor.a = alpha;
                popupView.modifierText.color = modifierColor;
            }

            if (popupView.modifierOutlineTexts != null)
            {
                Color outlineColor = baseOutlineColor;
                outlineColor.a = alpha;
                foreach (TextMeshProUGUI outline in popupView.modifierOutlineTexts)
                {
                    if (outline != null)
                        outline.color = outlineColor;
                }
            }
            yield return null;
        }

        if (rect != null)
            Destroy(rect.gameObject);
    }

    private DamagePopupView CreateDamagePopupView(RectTransform parent, string value, Color mainColor, string ratioText, string modifierText)
    {
        const int outlineCopies = 12;
        List<TextMeshProUGUI> outlines = new List<TextMeshProUGUI>(outlineCopies);
        float outlineStrength = Mathf.Clamp01(damageOutlineWidth);
        float outlineOffset = Mathf.Clamp(damageFontSize * Mathf.Max(0.06f, outlineStrength) * 0.55f, 2f, 10f);
        List<TextMeshProUGUI> ratioOutlines = null;
        TextMeshProUGUI ratioLabel = null;
        List<TextMeshProUGUI> modifierOutlines = null;
        TextMeshProUGUI modifierLabel = null;

        if (!string.IsNullOrWhiteSpace(ratioText))
        {
            ratioOutlines = new List<TextMeshProUGUI>(outlineCopies);
            float ratioOutlineOffset = Mathf.Clamp(damageRatioFontSize * Mathf.Max(0.06f, outlineStrength) * 0.55f, 1.5f, 6f);
            for (int i = 0; i < outlineCopies; i++)
            {
                float angle = Mathf.PI * 2f * i / outlineCopies;
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                TextMeshProUGUI outline = CreateDamagePopupText(
                    parent,
                    ratioText,
                    damageOutlineColor,
                    damageRatioOffset + direction * ratioOutlineOffset,
                    damageRatioFontSize,
                    damageRatioFontStyle);
                ratioOutlines.Add(outline);
            }

            ratioLabel = CreateDamagePopupText(
                parent,
                ratioText,
                damageRatioColor,
                damageRatioOffset,
                damageRatioFontSize,
                damageRatioFontStyle);
            ratioLabel.transform.SetAsLastSibling();
        }

        if (!string.IsNullOrWhiteSpace(modifierText))
        {
            modifierOutlines = new List<TextMeshProUGUI>(outlineCopies);
            float modifierOutlineOffset = Mathf.Clamp(damageRatioFontSize * Mathf.Max(0.06f, outlineStrength) * 0.55f, 1.5f, 6f);
            for (int i = 0; i < outlineCopies; i++)
            {
                float angle = Mathf.PI * 2f * i / outlineCopies;
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                TextMeshProUGUI outline = CreateDamagePopupText(
                    parent,
                    modifierText,
                    damageOutlineColor,
                    damageModifierOffset + direction * modifierOutlineOffset,
                    damageRatioFontSize,
                    damageRatioFontStyle);
                modifierOutlines.Add(outline);
            }

            modifierLabel = CreateDamagePopupText(
                parent,
                modifierText,
                damageModifierColor,
                damageModifierOffset,
                damageRatioFontSize,
                damageRatioFontStyle);
            modifierLabel.transform.SetAsLastSibling();
        }

        for (int i = 0; i < outlineCopies; i++)
        {
            float angle = Mathf.PI * 2f * i / outlineCopies;
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            TextMeshProUGUI outline = CreateDamagePopupText(parent, value, damageOutlineColor, direction * outlineOffset, damageFontSize, damageFontStyle);
            outlines.Add(outline);
        }

        TextMeshProUGUI mainText = CreateDamagePopupText(parent, value, mainColor, Vector2.zero, damageFontSize, damageFontStyle);
        mainText.transform.SetAsLastSibling();

        return new DamagePopupView
        {
            root = parent,
            ratioText = ratioLabel,
            modifierText = modifierLabel,
            mainText = mainText,
            ratioOutlineTexts = ratioOutlines,
            modifierOutlineTexts = modifierOutlines,
            outlineTexts = outlines
        };
    }

    private TextMeshProUGUI CreateDamagePopupText(RectTransform parent, string value, Color color, Vector2 anchoredOffset, float fontSize, FontStyles fontStyle)
    {
        GameObject textGo = new GameObject("DamageText", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(parent, false);

        RectTransform textRect = textGo.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.sizeDelta = parent.sizeDelta;
        textRect.anchoredPosition = anchoredOffset;

        TextMeshProUGUI text = textGo.GetComponent<TextMeshProUGUI>();
        if (damageFont != null)
            text.font = damageFont;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.alignment = TextAlignmentOptions.Center;
        text.color = color;
        text.text = value;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Overflow;
        text.extraPadding = true;
        text.raycastTarget = false;
        return text;
    }

    private IEnumerator AnimateClashDiceDisplay(ClashDiceDisplayHandle handle)
    {
        if (handle == null || handle.root == null || handle.worldAnchor == null)
            yield break;

        float rangeIntroDuration = Mathf.Max(0.01f, clashDiceRangeFadeInDuration);
        for (float time = 0f; time < rangeIntroDuration && !handle.revealRequested && !handle.hideRequested; time += Time.deltaTime)
        {
            if (!UpdateFloatingElementPosition(handle.root, handle.worldAnchor, handle.worldOffset, clashDiceScaleWithCamera))
                yield break;

            float normalized = Mathf.Clamp01(time / rangeIntroDuration);
            float eased = 1f - Mathf.Pow(1f - normalized, 3f);
            float scale = Mathf.Lerp(clashDiceRangeStartScale, clashDiceRangeEndScale, eased);
            SetTextGroupVisual(handle.rangeText, handle.rangeOutlines, handle.rangeColor, eased, scale);
            SetTextGroupVisual(handle.valueText, handle.valueOutlines, handle.valueColor, 0f, clashDiceValueStartScale);
            SetTextGroupVisual(handle.modifierText, handle.modifierOutlines, handle.modifierColor, 0f, 1f);
            yield return null;
        }

        if (handle.hideRequested)
        {
            yield return FadeOutClashDiceDisplay(handle, false);
            yield break;
        }

        SetTextGroupVisual(handle.rangeText, handle.rangeOutlines, handle.rangeColor, 1f, clashDiceRangeEndScale);
        while (!handle.revealRequested && !handle.hideRequested)
        {
            if (!UpdateFloatingElementPosition(handle.root, handle.worldAnchor, handle.worldOffset, clashDiceScaleWithCamera))
                yield break;
            yield return null;
        }

        if (handle.hideRequested)
        {
            yield return FadeOutClashDiceDisplay(handle, false);
            yield break;
        }

        string valueText = handle.revealedValue.ToString();
        ApplyTextGroupContent(handle.valueText, handle.valueOutlines, valueText);
        string modifierText = handle.revealedModifier != 0
            ? $"{(handle.revealedModifier > 0 ? "+" : string.Empty)}{handle.revealedModifier}"
            : string.Empty;
        ApplyTextGroupContent(handle.modifierText, handle.modifierOutlines, modifierText);

        float revealDuration = Mathf.Max(0.01f, clashDiceValueRevealDuration);
        for (float time = 0f; time < revealDuration && !handle.hideRequested; time += Time.deltaTime)
        {
            if (!UpdateFloatingElementPosition(handle.root, handle.worldAnchor, handle.worldOffset, clashDiceScaleWithCamera))
                yield break;

            float normalized = Mathf.Clamp01(time / revealDuration);
            float eased = 1f - Mathf.Pow(1f - normalized, 4f);
            float rangeAlpha = 1f - eased;
            float valueScale = Mathf.Lerp(clashDiceValueStartScale, clashDiceValueEndScale, eased);
            SetTextGroupVisual(handle.rangeText, handle.rangeOutlines, handle.rangeColor, rangeAlpha, clashDiceRangeEndScale);
            SetTextGroupVisual(handle.valueText, handle.valueOutlines, handle.valueColor, eased, valueScale);
            SetTextGroupVisual(handle.modifierText, handle.modifierOutlines, handle.modifierColor, handle.revealedModifier != 0 ? eased : 0f, 1f);
            yield return null;
        }

        SetTextGroupVisual(handle.rangeText, handle.rangeOutlines, handle.rangeColor, 0f, clashDiceRangeEndScale);
        SetTextGroupVisual(handle.valueText, handle.valueOutlines, handle.valueColor, 1f, clashDiceValueEndScale);
        SetTextGroupVisual(handle.modifierText, handle.modifierOutlines, handle.modifierColor, handle.revealedModifier != 0 ? 1f : 0f, 1f);

        while (!handle.hideRequested)
        {
            if (!UpdateFloatingElementPosition(handle.root, handle.worldAnchor, handle.worldOffset, clashDiceScaleWithCamera))
                yield break;
            yield return null;
        }

        yield return FadeOutClashDiceDisplay(handle, true);
    }

    private IEnumerator FadeOutClashDiceDisplay(ClashDiceDisplayHandle handle, bool fadeValue)
    {
        if (handle == null || handle.root == null)
            yield break;

        float duration = Mathf.Max(0.01f, clashDiceFadeOutDuration);
        for (float time = 0f; time < duration; time += Time.deltaTime)
        {
            if (!UpdateFloatingElementPosition(handle.root, handle.worldAnchor, handle.worldOffset, clashDiceScaleWithCamera))
                break;

            float normalized = Mathf.Clamp01(time / duration);
            float alpha = 1f - normalized;
            if (fadeValue)
            {
                SetTextGroupVisual(handle.valueText, handle.valueOutlines, handle.valueColor, alpha, Mathf.Lerp(clashDiceValueEndScale, clashDiceValueEndScale * 0.9f, normalized));
                SetTextGroupVisual(handle.modifierText, handle.modifierOutlines, handle.modifierColor, handle.revealedModifier != 0 ? alpha : 0f, 1f);
            }
            else
            {
                SetTextGroupVisual(handle.rangeText, handle.rangeOutlines, handle.rangeColor, alpha, Mathf.Lerp(clashDiceRangeEndScale, clashDiceRangeEndScale * 0.92f, normalized));
                SetTextGroupVisual(handle.modifierText, handle.modifierOutlines, handle.modifierColor, 0f, 1f);
            }

            yield return null;
        }

        if (handle.root != null)
            Destroy(handle.root.gameObject);
    }

    private List<TextMeshProUGUI> CreateOutlinedTextGroup(
        RectTransform parent,
        string value,
        TMP_FontAsset font,
        float fontSize,
        FontStyles fontStyle,
        Color outlineColor,
        float outlineWidth,
        out TextMeshProUGUI mainText)
    {
        const int outlineCopies = 10;
        List<TextMeshProUGUI> outlines = new List<TextMeshProUGUI>(outlineCopies);
        float outlineOffset = Mathf.Clamp(fontSize * Mathf.Max(0.06f, outlineWidth) * 0.42f, 1f, 8f);

        for (int i = 0; i < outlineCopies; i++)
        {
            float angle = Mathf.PI * 2f * i / outlineCopies;
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            TextMeshProUGUI outline = CreateOverlayText(parent, value, font, fontSize, fontStyle, outlineColor, direction * outlineOffset);
            outlines.Add(outline);
        }

        mainText = CreateOverlayText(parent, value, font, fontSize, fontStyle, Color.white, Vector2.zero);
        mainText.transform.SetAsLastSibling();
        return outlines;
    }

    private TextMeshProUGUI CreateOverlayText(
        RectTransform parent,
        string value,
        TMP_FontAsset font,
        float fontSize,
        FontStyles fontStyle,
        Color color,
        Vector2 anchoredOffset)
    {
        GameObject textGo = new GameObject("OverlayText", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(parent, false);

        RectTransform textRect = textGo.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.sizeDelta = parent.sizeDelta;
        textRect.anchoredPosition = anchoredOffset;

        TextMeshProUGUI text = textGo.GetComponent<TextMeshProUGUI>();
        if (font != null)
            text.font = font;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.alignment = TextAlignmentOptions.Center;
        text.color = color;
        text.text = value;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Overflow;
        text.extraPadding = true;
        text.raycastTarget = false;
        return text;
    }

    private void ApplyTextGroupContent(TextMeshProUGUI mainText, List<TextMeshProUGUI> outlines, string value)
    {
        if (mainText != null)
            mainText.text = value;

        if (outlines == null)
            return;

        foreach (TextMeshProUGUI outline in outlines)
        {
            if (outline != null)
                outline.text = value;
        }
    }

    private void SetTextGroupVisual(TextMeshProUGUI mainText, List<TextMeshProUGUI> outlines, Color mainColor, float alpha, float scale)
    {
        if (mainText != null)
        {
            Color color = mainColor;
            color.a *= Mathf.Clamp01(alpha);
            mainText.color = color;
            mainText.canvasRenderer.SetAlpha(Mathf.Clamp01(alpha));
            mainText.rectTransform.localScale = Vector3.one * scale;
        }

        if (outlines == null)
            return;

        foreach (TextMeshProUGUI outline in outlines)
        {
            if (outline == null)
                continue;

            Color outlineColor = clashDiceOutlineColor;
            outlineColor.a *= Mathf.Clamp01(alpha);
            outline.color = outlineColor;
            outline.canvasRenderer.SetAlpha(Mathf.Clamp01(alpha));
            outline.rectTransform.localScale = Vector3.one * scale;
        }
    }

    private bool UpdateFloatingElementPosition(RectTransform rect, Transform worldAnchor, Vector3 worldOffset, bool scaleWithCamera)
    {
        if (rect == null || worldAnchor == null)
            return false;

        Camera cam = ResolveCamera();
        if (cam == null)
            return false;

        Vector3 screenPosition = cam.WorldToScreenPoint(worldAnchor.position + worldOffset);
        bool visible = screenPosition.z > 0f;
        rect.gameObject.SetActive(visible);
        if (!visible)
            return true;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(overlayRoot, screenPosition, null, out Vector2 localPoint);
        rect.anchoredPosition = localPoint;
        rect.localScale = Vector3.one * (scaleWithCamera ? ResolveDamagePopupCameraScale(cam) : 1f);
        return true;
    }

    private void PlayHealthBarHitFx(TrackedUnitUI ui)
    {
        if (ui == null || ui.worldAnchor == null || healthBarHitBurstCount <= 0)
            return;

        EnsureParticleResources();

        GameObject root = new GameObject("HealthBarHitFx");
        root.transform.position = ui.worldAnchor.position + healthBarWorldOffset;
        root.transform.rotation = Quaternion.identity;

        ParticleSystem particleSystem = root.AddComponent<ParticleSystem>();
        ParticleSystemRenderer renderer = root.GetComponent<ParticleSystemRenderer>();
        renderer.material = particleMaterial;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortingOrder = healthBarHitSortingOrder;

        Color baseColor = ui.isEnemy ? enemyHealthFillColor : allyHealthFillColor;

        ParticleSystem.MainModule main = particleSystem.main;
        main.playOnAwake = false;
        main.loop = false;
        main.startColor = baseColor;
        main.startLifetime = healthBarHitParticleLifetime;
        main.startSpeed = healthBarHitParticleSpeed;
        main.startSize = healthBarHitParticleSize;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = Mathf.Max(healthBarHitBurstCount, 8);

        ParticleSystem.EmissionModule emission = particleSystem.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, healthBarHitBurstCount) });

        ParticleSystem.ShapeModule shape = particleSystem.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = healthBarHitSpawnRadius;
        shape.arc = 360f;

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particleSystem.colorOverLifetime;
        colorOverLifetime.enabled = true;
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(
            BuildParticleGradient(Color.white, baseColor, baseColor, 1f, healthBarHitParticleFadeDuration, healthBarHitParticleLifetime));

        ParticleSystem.VelocityOverLifetimeModule velocityOverLifetime = particleSystem.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.space = ParticleSystemSimulationSpace.World;
        velocityOverLifetime.x = new ParticleSystem.MinMaxCurve(-0.18f, 0.18f);
        velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(0.2f, 0.55f);
        velocityOverLifetime.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        ParticleSystem.RotationOverLifetimeModule rotation = particleSystem.rotationOverLifetime;
        rotation.enabled = true;
        rotation.z = new ParticleSystem.MinMaxCurve(-2f, 2f);

        ConfigureParticleCollision(particleSystem);

        particleSystem.Play();
        Destroy(root, healthBarHitParticleLifetime + 0.08f);
    }

    private void PlayDeathEffect(Vector3 position, Vector3 preferredDirection)
    {
        EnsureParticleResources();
        float minSpeed = Mathf.Max(0.01f, deathParticleMinSpeed);
        float maxSpeed = Mathf.Max(minSpeed, deathParticleMaxSpeed);

        GameObject root = new GameObject("DeathSnowEffect");
        root.transform.position = position;
        Vector3 finalDirection = preferredDirection.sqrMagnitude > 0.001f
            ? preferredDirection.normalized
            : (deathDirection.sqrMagnitude > 0.001f ? deathDirection.normalized : Vector3.up);
        root.transform.rotation = Quaternion.FromToRotation(Vector3.forward, finalDirection);

        ParticleSystem particleSystem = root.AddComponent<ParticleSystem>();
        ParticleSystemRenderer renderer = root.GetComponent<ParticleSystemRenderer>();
        renderer.material = particleMaterial;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortingOrder = deathSortingOrder;

        ParticleSystem.MainModule main = particleSystem.main;
        main.playOnAwake = false;
        main.loop = false;
        main.startColor = deathParticleColor;
        main.startLifetime = deathParticleLifetime;
        main.startSpeed = new ParticleSystem.MinMaxCurve(minSpeed, maxSpeed);
        main.startSize = deathParticleSize;
        main.gravityModifier = deathGravity;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = Mathf.Max(24, deathBurstCount * 2);

        ParticleSystem.EmissionModule emission = particleSystem.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, deathBurstCount) });

        ParticleSystem.ShapeModule shape = particleSystem.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.radius = deathSpawnRadius;
        shape.angle = deathSpreadAngle;

        Vector3 directionalVelocity = finalDirection * deathDirectionalBoost;
        ParticleSystem.VelocityOverLifetimeModule velocityOverLifetime = particleSystem.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.space = ParticleSystemSimulationSpace.World;
        velocityOverLifetime.x = new ParticleSystem.MinMaxCurve(
            directionalVelocity.x - deathDirectionalRandomness,
            directionalVelocity.x + deathDirectionalRandomness);
        velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(
            directionalVelocity.y - deathDirectionalRandomness,
            directionalVelocity.y + deathDirectionalRandomness);
        velocityOverLifetime.z = new ParticleSystem.MinMaxCurve(
            directionalVelocity.z - deathDirectionalRandomness,
            directionalVelocity.z + deathDirectionalRandomness);

        ParticleSystem.LimitVelocityOverLifetimeModule limitVelocity = particleSystem.limitVelocityOverLifetime;
        limitVelocity.enabled = true;
        limitVelocity.space = ParticleSystemSimulationSpace.World;
        limitVelocity.limit = maxSpeed + deathDirectionalBoost + deathDirectionalRandomness;
        limitVelocity.dampen = deathDirectionalDrag;

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particleSystem.colorOverLifetime;
        colorOverLifetime.enabled = true;
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(
            BuildParticleGradient(deathParticleColor, new Color(0.9f, 0.95f, 1f, 1f), deathParticleColor, 1f, deathParticleFadeDuration, deathParticleLifetime));

        ParticleSystem.RotationOverLifetimeModule rotation = particleSystem.rotationOverLifetime;
        rotation.enabled = true;
        rotation.z = new ParticleSystem.MinMaxCurve(-2f, 2f);

        ConfigureParticleCollision(particleSystem);

        particleSystem.Play();
        Destroy(root, deathParticleLifetime + 0.1f);
    }

    private void HideCombatantVisuals(GameObject combatant)
    {
        if (combatant == null)
            return;

        foreach (Renderer renderer in combatant.GetComponentsInChildren<Renderer>(true))
            renderer.enabled = false;

        foreach (Collider collider in combatant.GetComponentsInChildren<Collider>(true))
            collider.enabled = false;

        foreach (Collider2D collider2D in combatant.GetComponentsInChildren<Collider2D>(true))
            collider2D.enabled = false;

        foreach (Canvas canvas in combatant.GetComponentsInChildren<Canvas>(true))
            canvas.enabled = false;
    }

    private void CleanupMissingAllies(HashSet<BattleAllyController> livingSet)
    {
        List<BattleAllyController> stale = new List<BattleAllyController>();
        foreach (BattleAllyController ally in allyUIs.Keys)
        {
            if (!livingSet.Contains(ally))
                stale.Add(ally);
        }

        foreach (BattleAllyController ally in stale)
            RemoveTrackedAlly(ally);
    }

    private void CleanupMissingEnemies(HashSet<BattleEnemyController> livingSet)
    {
        List<BattleEnemyController> stale = new List<BattleEnemyController>();
        foreach (BattleEnemyController enemy in enemyUIs.Keys)
        {
            if (!livingSet.Contains(enemy))
                stale.Add(enemy);
        }

        foreach (BattleEnemyController enemy in stale)
            RemoveTrackedEnemy(enemy);
    }

    private void RemoveTrackedAlly(BattleAllyController ally)
    {
        if (ally == null)
            return;

        ally.Damaged -= OnAllyDamaged;
        ally.HealthChanged -= OnAllyHealthChanged;
        ally.Died -= OnAllyDied;
        ally.StatusChanged -= OnAllyStatusChanged;

        if (allyUIs.TryGetValue(ally, out TrackedUnitUI ui))
        {
            if (activeStatusTooltipIcon != null && ui.statusIcons.Contains(activeStatusTooltipIcon))
                HideStatusTooltip();

            if (ui.root != null)
                Destroy(ui.root.gameObject);
            allyUIs.Remove(ally);
        }
    }

    private void RemoveTrackedEnemy(BattleEnemyController enemy)
    {
        if (enemy == null)
            return;

        enemy.Damaged -= OnEnemyDamaged;
        enemy.HealthChanged -= OnEnemyHealthChanged;
        enemy.Died -= OnEnemyDied;
        enemy.StatusChanged -= OnEnemyStatusChanged;

        if (enemyUIs.TryGetValue(enemy, out TrackedUnitUI ui))
        {
            if (activeStatusTooltipIcon != null && ui.statusIcons.Contains(activeStatusTooltipIcon))
                HideStatusTooltip();

            if (ui.root != null)
                Destroy(ui.root.gameObject);
            enemyUIs.Remove(enemy);
        }
    }

    private void EnsureStatusTooltip()
    {
        EnsureCanvas();
        if (statusTooltipView != null && statusTooltipView.root != null)
            return;

        GameObject rootGo = new GameObject("StatusTooltip", typeof(RectTransform));
        rootGo.transform.SetParent(overlayRoot, false);
        RectTransform root = rootGo.GetComponent<RectTransform>();
        root.anchorMin = new Vector2(0.5f, 0.5f);
        root.anchorMax = new Vector2(0.5f, 0.5f);
        root.pivot = new Vector2(0.5f, 0f);
        root.sizeDelta = statusTooltipSize;

        Image background = CreateImage("Background", root, statusTooltipBackgroundColor);
        RectTransform backgroundRect = background.rectTransform;
        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;
        if (statusTooltipBackgroundSprite != null)
            background.sprite = statusTooltipBackgroundSprite;
        background.type = statusTooltipBackgroundSprite != null ? Image.Type.Sliced : Image.Type.Simple;
        background.raycastTarget = false;

        Image icon = CreateImage("Icon", root, Color.white);
        RectTransform iconRect = icon.rectTransform;
        iconRect.anchorMin = new Vector2(0f, 1f);
        iconRect.anchorMax = new Vector2(0f, 1f);
        iconRect.pivot = new Vector2(0f, 1f);
        iconRect.anchoredPosition = new Vector2(16f, -16f);
        iconRect.sizeDelta = new Vector2(44f, 44f);
        icon.preserveAspect = true;
        icon.raycastTarget = false;

        TextMeshProUGUI title = CreateTooltipText(root, "Title", new Vector2(68f, -14f), new Vector2(228f, 34f), statusTooltipTitleFontSize, statusTooltipTitleColor, TextAlignmentOptions.TopLeft, statusTooltipTitleFont);
        TextMeshProUGUI body = CreateTooltipText(root, "Body", new Vector2(16f, -64f), new Vector2(288f, 42f), statusTooltipBodyFontSize, statusTooltipBodyColor, TextAlignmentOptions.TopLeft, statusTooltipBodyFont);
        body.enableWordWrapping = true;
        body.overflowMode = TextOverflowModes.Overflow;

        statusTooltipView = new StatusTooltipView
        {
            root = root,
            backgroundImage = background,
            iconImage = icon,
            titleText = title,
            bodyText = body
        };

        root.gameObject.SetActive(false);
    }

    private TextMeshProUGUI CreateTooltipText(RectTransform parent, string name, Vector2 anchoredPosition, Vector2 size, float fontSize, Color color, TextAlignmentOptions alignment, TMP_FontAsset font)
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
        if (font != null)
            text.font = font;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.raycastTarget = false;
        return text;
    }

    private void ShowStatusTooltip(StatusIconView view)
    {
        if (view == null || view.entry == null || view.root == null)
            return;

        EnsureStatusTooltip();
        if (statusTooltipView == null || statusTooltipView.root == null)
            return;

        BattleStatusRuntimeEntry entry = view.entry;
        if (statusTooltipView.backgroundImage != null)
        {
            statusTooltipView.backgroundImage.sprite = statusTooltipBackgroundSprite;
            statusTooltipView.backgroundImage.type = statusTooltipBackgroundSprite != null ? Image.Type.Sliced : Image.Type.Simple;
            statusTooltipView.backgroundImage.color = statusTooltipBackgroundColor;
        }

        if (statusTooltipView.iconImage != null)
        {
            statusTooltipView.iconImage.sprite = entry.IconSprite != null ? entry.IconSprite : statusFallbackSprite;
            statusTooltipView.iconImage.color = entry.IconSprite != null ? Color.white : entry.AccentColor;
            statusTooltipView.iconImage.preserveAspect = true;
        }

        if (statusTooltipView.titleText != null)
            statusTooltipView.titleText.text = entry.DisplayName;

        if (statusTooltipView.bodyText != null)
            statusTooltipView.bodyText.text = BuildStatusTooltipDescription(entry);

        statusTooltipView.root.sizeDelta = statusTooltipSize;
        statusTooltipView.root.gameObject.SetActive(true);
        statusTooltipView.root.SetAsLastSibling();
        statusTooltipView.root.position = view.root.position + (Vector3)statusTooltipOffset;
        activeStatusTooltipIcon = view;
    }

    private void HideStatusTooltip()
    {
        activeStatusTooltipIcon = null;
        if (statusTooltipView?.root != null)
            statusTooltipView.root.gameObject.SetActive(false);
    }

    private static string BuildStatusTooltipDescription(BattleStatusRuntimeEntry entry)
    {
        if (entry == null)
            return string.Empty;

        if (entry.statusData != null && !string.IsNullOrWhiteSpace(entry.statusData.Description))
            return entry.statusData.Description;

        if (entry.statusData != null && entry.statusData.BehaviorType == BattleStatus_SO.StatusBehaviorType.DamageAtRoundStart)
            return $"\u6bcf\u56de\u5408\u5f00\u59cb\u65f6\u53d7\u5230 {entry.stacks} \u70b9\u4f24\u5bb3\u3002";

        return $"\u5f53\u524d\u62e5\u6709 {entry.stacks} \u5c42 {entry.DisplayName}\u3002";
    }

    private void EnsureCanvas()
    {
        if (overlayCanvas != null && overlayRoot != null)
            return;

        GameObject canvasGo = new GameObject("BattleFeedbackCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(null, false);

        overlayCanvas = canvasGo.GetComponent<Canvas>();
        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        overlayCanvas.sortingOrder = canvasSortOrder;

        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        overlayRoot = canvasGo.GetComponent<RectTransform>();
        overlayRoot.anchorMin = Vector2.zero;
        overlayRoot.anchorMax = Vector2.one;
        overlayRoot.offsetMin = Vector2.zero;
        overlayRoot.offsetMax = Vector2.zero;

        EnsureEventSystem();
    }

    private static void EnsureEventSystem()
    {
        EventSystemUtility.EnsureSingleEventSystem();
    }

    private Camera ResolveCamera()
    {
        if (presentationCameraOverride != null && presentationCameraOverride.enabled)
            return presentationCameraOverride;

        if (worldCamera == null)
            worldCamera = Camera.main;

        CacheDamageScaleBaseline(worldCamera);

        return worldCamera;
    }

    private void CacheDamageScaleBaseline(Camera camera)
    {
        if (damageScaleBaselineCaptured || camera == null)
            return;

        baseDamageOrthographicSize = camera.orthographic ? Mathf.Max(0.01f, camera.orthographicSize) : 0f;
        baseDamageFieldOfView = !camera.orthographic ? Mathf.Max(1f, camera.fieldOfView) : 0f;
        damageScaleBaselineCaptured = true;
    }

    private float ResolveDamagePopupCameraScale(Camera camera)
    {
        if (!damagePopupScaleWithCamera || camera == null)
            return 1f;

        CacheDamageScaleBaseline(worldCamera != null ? worldCamera : camera);

        float rawScale = 1f;
        if (camera.orthographic)
        {
            float baseline = baseDamageOrthographicSize > 0f ? baseDamageOrthographicSize : Mathf.Max(0.01f, camera.orthographicSize);
            rawScale = baseline / Mathf.Max(0.01f, camera.orthographicSize);
        }
        else
        {
            float baselineFov = baseDamageFieldOfView > 0f ? baseDamageFieldOfView : Mathf.Max(1f, camera.fieldOfView);
            float baselineTan = Mathf.Tan(baselineFov * 0.5f * Mathf.Deg2Rad);
            float currentTan = Mathf.Tan(Mathf.Max(1f, camera.fieldOfView) * 0.5f * Mathf.Deg2Rad);
            rawScale = baselineTan / Mathf.Max(0.0001f, currentTan);
        }

        float blendedScale = Mathf.Lerp(1f, rawScale, damageCameraScaleInfluence) * presentationDamageScaleOverride;
        return Mathf.Clamp(blendedScale, minDamageCameraScale, maxDamageCameraScale);
    }

    private void EnsureParticleResources()
    {
        if (whiteTexture == null)
        {
            whiteTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            whiteTexture.SetPixel(0, 0, Color.white);
            whiteTexture.Apply(false, false);
        }

        if (particleMaterial == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                ?? Shader.Find("Particles/Standard Unlit")
                ?? Shader.Find("Sprites/Default");

            if (shader != null)
            {
                particleMaterial = new Material(shader);
                if (particleMaterial.HasProperty("_MainTex"))
                    particleMaterial.SetTexture("_MainTex", whiteTexture);
                if (particleMaterial.HasProperty("_BaseMap"))
                    particleMaterial.SetTexture("_BaseMap", whiteTexture);
                ConfigureTransparentParticleMaterial(particleMaterial);
            }
        }
    }

    private void ConfigureParticleCollision(ParticleSystem particleSystem)
    {
        if (particleSystem == null)
            return;

        ParticleSystem.CollisionModule collision = particleSystem.collision;
        collision.enabled = enableParticleGroundCollision;
        if (!enableParticleGroundCollision)
            return;

        collision.type = ParticleSystemCollisionType.World;
        collision.mode = ParticleSystemCollisionMode.Collision3D;
        collision.collidesWith = particleCollisionLayers;
        collision.enableDynamicColliders = true;
        collision.sendCollisionMessages = false;
        collision.bounce = particleCollisionBounce;
        collision.dampen = particleCollisionDampen;
        collision.lifetimeLoss = particleCollisionLifetimeLoss;
        collision.radiusScale = particleCollisionRadiusScale;
        collision.minKillSpeed = particleCollisionMinKillSpeed;
        collision.maxKillSpeed = float.MaxValue;
        collision.maxCollisionShapes = particleCollisionMaxShapes;
        collision.quality = ParticleSystemCollisionQuality.High;
    }

    private static void ConfigureTransparentParticleMaterial(Material material)
    {
        if (material == null)
            return;

        if (material.HasProperty("_Surface"))
            material.SetFloat("_Surface", 1f);
        if (material.HasProperty("_SurfaceType"))
            material.SetFloat("_SurfaceType", 1f);
        if (material.HasProperty("_Blend"))
            material.SetFloat("_Blend", 0f);
        if (material.HasProperty("_Mode"))
            material.SetFloat("_Mode", 2f);
        if (material.HasProperty("_SrcBlend"))
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (material.HasProperty("_DstBlend"))
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        if (material.HasProperty("_ZWrite"))
            material.SetFloat("_ZWrite", 0f);
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", Color.white);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", Color.white);

        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.DisableKeyword("_ALPHATEST_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
    }

    private static Gradient BuildParticleGradient(Color startColor, Color midColor, Color endColor, float maxAlpha, float fadeDuration, float lifetime)
    {
        Gradient gradient = new Gradient();
        float fadeStart = ResolveFadeStart(fadeDuration, lifetime);
        float midTime = Mathf.Clamp01(Mathf.Min(fadeStart, 0.35f));
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(startColor, 0f),
                new GradientColorKey(midColor, midTime),
                new GradientColorKey(endColor, 1f)
            },
            BuildFadeAlphaKeys(maxAlpha, fadeStart));
        return gradient;
    }

    private static GradientAlphaKey[] BuildFadeAlphaKeys(float maxAlpha, float fadeStart)
    {
        if (fadeStart <= 0.001f)
        {
            return new[]
            {
                new GradientAlphaKey(maxAlpha, 0f),
                new GradientAlphaKey(0f, 1f)
            };
        }

        return new[]
        {
            new GradientAlphaKey(maxAlpha, 0f),
            new GradientAlphaKey(maxAlpha, fadeStart),
            new GradientAlphaKey(0f, 1f)
        };
    }

    private static float ResolveFadeStart(float fadeDuration, float lifetime)
    {
        if (lifetime <= 0.0001f)
            return 0f;

        return Mathf.Clamp01(1f - Mathf.Clamp(fadeDuration, 0f, lifetime) / lifetime);
    }

    private void OnDestroy()
    {
        foreach (BattleAllyController ally in new List<BattleAllyController>(allyUIs.Keys))
            RemoveTrackedAlly(ally);
        foreach (BattleEnemyController enemy in new List<BattleEnemyController>(enemyUIs.Keys))
            RemoveTrackedEnemy(enemy);

        if (overlayCanvas != null)
            Destroy(overlayCanvas.gameObject);

        if (particleMaterial != null)
            Destroy(particleMaterial);
        if (whiteTexture != null)
            Destroy(whiteTexture);
    }
}
