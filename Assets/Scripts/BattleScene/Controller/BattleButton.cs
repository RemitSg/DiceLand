using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattleButton : MonoBehaviour
{
    private enum CombatActionType
    {
        Clash,
        AllySingleAttack,
        EnemySingleAttack
    }

    private sealed class CombatAction
    {
        public CombatActionType Type;
        public BattleAllyController Ally;
        public BattleEnemyController Enemy;
        public SpeedButton EnemyButton;
        public BattleAllyController TargetAlly;
        public int PrioritySpeed;
        public int TiePriority;
        public int SequenceIndex;
    }

    private sealed class AllyProgressionResult
    {
        public CharacterData_SO CharacterData;
        public Weapon_SO WeaponData;
        public string DisplayName;
        public int InitialLevel;
        public int InitialExperience;
        public int InitialMaxHP;
        public int InitialCurrentHP;
        public int FinalLevel;
        public int FinalExperience;
        public int FinalMaxHP;
        public int FinalCurrentHP;
        public bool IsAlive;
    }

    private sealed class EncounterDropResults
    {
        public List<RunLootState> lootRewards = new List<RunLootState>();
        public List<RunRelicState> relicRewards = new List<RunRelicState>();
    }

    private struct PlayerRelicDiceModifier
    {
        public int powerBonus;
        public int minPowerBonus;
        public int maxPowerBonus;
    }

    [Header("References")]
    [Tooltip("\u6218\u6597\u7ba1\u7406\u5668\uff0c\u7559\u7a7a\u5219\u81ea\u52a8\u67e5\u627e\u573a\u666f\u4e2d\u7684 BattleManager")]
    public BattleManager battleManager;

    [Header("Presentation")]
    [Tooltip("\u6218\u6597\u6f14\u51fa\u7cfb\u7edf\uff0c\u7559\u7a7a\u5219\u81ea\u52a8\u67e5\u627e\u6216\u8fd0\u884c\u65f6\u521b\u5efa")]
    public BattlePresentationSystem battlePresentationSystem;

    [Tooltip("\u6218\u6597\u53cd\u9988\u7cfb\u7edf\uff0c\u8d1f\u8d23\u8840\u6761\u3001\u4f24\u5bb3\u6570\u5b57\u548c\u6b7b\u4ea1\u7279\u6548")]
    public BattleFeedbackSystem battleFeedbackSystem;
    public BattleSettlementOverlay battleSettlementOverlay;
    public BattleSkillInfoOverlay battleSkillInfoOverlay;

    [Header("Shared Energy")]
    [SerializeField] private int initialSharedEnergyMax = 10;
    [SerializeField] private TMP_FontAsset sharedEnergyFont;
    [SerializeField] private int sharedEnergyFontSize = 28;
    [SerializeField] private Color sharedEnergyTextColor = Color.white;
    [SerializeField] private Color sharedEnergySpendPopupColor = new Color(1f, 0.88f, 0.35f, 1f);
    [Tooltip("能量面板相对屏幕左上角的边距 (x=左边距, y=上边距)")]
    [SerializeField] private Vector2 sharedEnergyPanelMargin = new Vector2(20f, 20f);
    [Tooltip("能量面板大小")]
    [SerializeField] private Vector2 sharedEnergyPanelSize = new Vector2(260f, 72f);
    [Tooltip("回合数面板大小")]
    [SerializeField] private Vector2 roundCounterPanelSize = new Vector2(220f, 56f);
    [SerializeField] private Color sharedEnergyPanelBgColor = new Color(0f, 0f, 0f, 0.55f);
    [SerializeField] private Color sharedEnergyBarBgColor = new Color(0.18f, 0.18f, 0.18f, 0.9f);
    [SerializeField] private Color sharedEnergyBarFillColor = new Color(0.28f, 0.78f, 1f, 1f);

    [Header("Speed Settings")]
    [Tooltip("\u968f\u673a\u901f\u5ea6\u4e0b\u9650\uff0c\u5305\u542b\u8be5\u503c")]
    public int speedMin = 2;

    [Tooltip("\u968f\u673a\u901f\u5ea6\u4e0a\u9650\uff0c\u5305\u542b\u8be5\u503c")]
    public int speedMax = 6;

    private enum BattlePhase
    {
        Ready,
        Targeting
    }

    private BattlePhase currentPhase = BattlePhase.Ready;
    private int currentRound = 1;
    private bool isResolvingCombat;
    private bool enemyBattleStartEffectsApplied;
    private int currentSharedEnergy;
    private int currentSharedEnergyMax;
    private int reservedSharedEnergy;
    private bool isRefreshingSharedEnergyReservations;
    private BattleAllyController[] activeRoundAllies;
    private BattleEnemyController[] activeRoundEnemies;
    private Canvas sharedEnergyCanvas;
    private RectTransform sharedEnergyRoot;
    private RectTransform sharedEnergyPanelRect;
    private TextMeshProUGUI sharedEnergyText;
    private Image sharedEnergyBarFill;
    private RectTransform roundCounterPanelRect;
    private TextMeshProUGUI roundCounterText;

    private void Awake()
    {
        // Retry/return transitions can leave the global combat lock raised because
        // SpeedButton.CombatLocked is static. Reset it whenever a battle scene boots.
        SpeedButton.CombatLocked = false;
        SpeedButton.InputSuppressed = false;
        enemyBattleStartEffectsApplied = false;
        currentSharedEnergyMax = Mathf.Max(0, initialSharedEnergyMax);
        currentSharedEnergy = currentSharedEnergyMax;
        reservedSharedEnergy = 0;

        if (battleManager == null)
            battleManager = FindObjectOfType<BattleManager>();

        ResolvePresentationSystem();
        ResolveFeedbackSystem();
        ResolveSettlementOverlay();
        ResolveSkillInfoOverlay();
        EnsureSharedEnergyUi();
        RefreshSharedEnergyUi();
    }

    public void OnBattleButtonClicked()
    {
        if (isResolvingCombat)
        {
            Debug.LogWarning($"[BattleButton][\u7b2c {currentRound} \u56de\u5408] \u6218\u6597\u6f14\u51fa\u8fdb\u884c\u4e2d\uff0c\u8bf7\u7b49\u5f85\u5f53\u524d\u7ed3\u7b97\u5b8c\u6210\u3002");
            return;
        }

        if (battleManager == null)
        {
            battleManager = FindObjectOfType<BattleManager>();
            if (battleManager == null)
            {
                Debug.LogWarning("[BattleButton] BattleManager \u672a\u627e\u5230\u3002");
                return;
            }
        }

        ResolvePresentationSystem();
        ResolveFeedbackSystem();
        ResolveSettlementOverlay();
        ResolveSkillInfoOverlay();

        if (battleSettlementOverlay != null && battleSettlementOverlay.IsVisible)
            return;
        if (battleSkillInfoOverlay != null && battleSkillInfoOverlay.IsVisible)
            return;

        if (battleManager.alliesParent == null || battleManager.enemiesParent == null)
        {
            Debug.LogWarning("[BattleButton] alliesParent \u6216 enemiesParent \u672a\u914d\u7f6e\u3002");
            return;
        }

        BattleAllyController[] allies = battleManager.alliesParent.GetComponentsInChildren<BattleAllyController>(false);
        BattleEnemyController[] enemies = battleManager.enemiesParent.GetComponentsInChildren<BattleEnemyController>(false);

        battleFeedbackSystem?.BindCombatants(allies, enemies);

        if (currentPhase == BattlePhase.Ready)
        {
            SetupTargetingPhase(allies, enemies);
            currentPhase = BattlePhase.Targeting;
            return;
        }

        StartCoroutine(ExecuteCombatPhaseRoutine(allies, enemies));
    }

    private void SetupTargetingPhase(BattleAllyController[] allies, BattleEnemyController[] enemies)
    {
        activeRoundAllies = allies;
        activeRoundEnemies = enemies;
        currentSharedEnergy = Mathf.Clamp(currentSharedEnergy + 1, 0, currentSharedEnergyMax);
        reservedSharedEnergy = 0;
        RefreshSharedEnergyUi();

        BattleLog($"\u25c6\u25c6\u25c6 \u76ee\u6807\u6307\u5b9a\u9636\u6bb5 \u25c6\u25c6\u25c6 \u53cb\u65b9 {CountLiving(allies)} \u540d / \u654c\u65b9 {CountLiving(enemies)} \u540d");

        ApplyRoundStartStatusEffectsGeneric(allies, enemies);
        if (TryHandleRoundStartRetreatSettlement(allies, enemies))
            return;
        if (TryHandleBattleCompletion(allies, enemies))
            return;

        ApplyEnemyBattleStartEffectsIfNeeded(allies, enemies);

        foreach (BattleAllyController ally in allies)
        {
            if (ally.speedButton == null)
                continue;

            if (ally.IsDead)
            {
                ally.speedButton.ResetConnection();
                ally.speedButton.HideForCombat();
                ally.speedButton.IsInteractable = false;
                continue;
            }

            ally.speedButton.ShowForTargeting();
            ally.speedButton.ResetConnection();
        }

        foreach (BattleEnemyController enemy in enemies)
        {
            IReadOnlyList<SpeedButton> enemyButtons = GetEnemySpeedButtons(enemy);
            if (enemyButtons.Count == 0)
                continue;

            if (enemy.IsDead)
            {
                for (int i = 0; i < enemyButtons.Count; i++)
                {
                    if (enemyButtons[i] != null)
                        enemyButtons[i].HideForCombat();
                }
                continue;
            }

            for (int i = 0; i < enemyButtons.Count; i++)
            {
                if (enemyButtons[i] == null)
                    continue;

                enemyButtons[i].ShowForTargeting();
                enemyButtons[i].ResetConnection();
            }
        }

        foreach (BattleAllyController ally in allies)
        {
            if (ally.IsDead)
                continue;

            ally.GenerateRandomSpeed(speedMin, speedMax);
            ApplyPlayerRoundStartRelics(ally);
            ally.SelectDefaultSkill(currentRound);
            BattleLog($"\u53cb\u65b9 {ally.GetDebugDisplayName()} \u901f\u5ea6={ally.Speed} HP={ally.CurrentHP}/{ally.MaxHP}");
        }

        RegisterSharedEnergyListeners(allies);
        RefreshSharedEnergyReservations(allies);

        foreach (BattleEnemyController enemy in enemies)
        {
            if (enemy.IsDead)
                continue;

            enemy.GenerateRandomSpeed();
            enemy.SelectRandomSkills(currentRound);
            BattleLog($"\u654c\u65b9 {enemy.GetDebugDisplayName()} \u901f\u5ea6={enemy.Speed} HP={enemy.CurrentHP}/{enemy.MaxHP}");
        }

        List<SpeedButton> allyButtons = new List<SpeedButton>();
        foreach (BattleAllyController ally in allies)
        {
            if (ally.IsDead || ally.speedButton == null)
                continue;

            ally.speedButton.IsInteractable = true;
            allyButtons.Add(ally.speedButton);
        }

        foreach (BattleEnemyController enemy in enemies)
        {
            if (enemy.IsDead)
                continue;

            enemy.AutoConnectToRandomAlly(allyButtons);
            IReadOnlyList<SpeedButton> enemyButtons = GetEnemySpeedButtons(enemy);
            for (int i = 0; i < enemyButtons.Count; i++)
            {
                SpeedButton enemyButton = enemyButtons[i];
                if (enemyButton == null || enemyButton.ConnectedTarget == null)
                    continue;

                BattleAllyController targetAlly = enemyButton.ConnectedTarget.GetComponentInParent<BattleAllyController>();
                string targetName = targetAlly != null ? targetAlly.GetDebugDisplayName() : enemyButton.ConnectedTarget.gameObject.name;
                string buttonLabel = enemyButtons.Count > 1 ? $" [\u6309\u94ae {i + 1}]" : string.Empty;
                BattleLog($"\u654c\u65b9 {enemy.GetDebugDisplayName()}{buttonLabel} \u5df2\u9501\u5b9a\u76ee\u6807 -> {targetName} | \u6280\u80fd {enemy.GetSelectedSkillDebugSummary(enemyButton)}");
            }
        }

        BattleLog("\u654c\u65b9\u5df2\u5b8c\u6210\u81ea\u52a8\u6307\u5b9a\u76ee\u6807\u3002\u8bf7\u73a9\u5bb6\u5b8c\u6210\u753b\u7ebf\u540e\u518d\u6b21\u6309\u4e0b\u6218\u6597\u6309\u94ae\u8fdb\u5165\u7ed3\u7b97\u3002");
    }

    private void ApplyEnemyBattleStartEffectsIfNeeded(BattleAllyController[] allies, BattleEnemyController[] enemies)
    {
        if (enemyBattleStartEffectsApplied)
            return;

        enemyBattleStartEffectsApplied = true;
        if (allies == null || enemies == null)
            return;

        for (int i = 0; i < allies.Length; i++)
        {
            if (allies[i] != null)
                allies[i].ClearTemporarySkills();
        }

        for (int enemyIndex = 0; enemyIndex < enemies.Length; enemyIndex++)
        {
            BattleEnemyController enemy = enemies[enemyIndex];
            if (enemy == null || enemy.IsDead || enemy.enemyData == null)
                continue;

            List<EnemyData_SO.BattleStartAllySkillGrantEntry> grants = enemy.enemyData.battleStartGrantedAllySkills;
            if (grants == null || grants.Count == 0)
                continue;

            for (int grantIndex = 0; grantIndex < grants.Count; grantIndex++)
            {
                EnemyData_SO.BattleStartAllySkillGrantEntry grant = grants[grantIndex];
                if (grant.skill == null)
                    continue;

                for (int allyIndex = 0; allyIndex < allies.Length; allyIndex++)
                {
                    BattleAllyController ally = allies[allyIndex];
                    if (ally == null || ally.IsDead)
                        continue;

                    ally.GrantTemporarySkill(grant.skill, grant.replaceWeaponSkills);
                }

                BattleLog($"\u654c\u65b9 {enemy.GetDebugDisplayName()} \u5728\u6218\u6597\u5f00\u59cb\u65f6\u8d4b\u4e88\u53cb\u65b9\u4e34\u65f6\u6280\u80fd\uff1a{grant.skill.skillName}");
            }
        }
    }

    public bool CanSelectSkillForAlly(BattleAllyController ally, EnemySkill_SO skill)
    {
        if (ally == null || skill == null)
            return false;

        int availableEnergyExcludingCurrentReservation = currentSharedEnergy - Mathf.Max(0, reservedSharedEnergy - GetReservedEnergyCostForAlly(ally));
        return skill.energyCost <= Mathf.Max(0, availableEnergyExcludingCurrentReservation);
    }

    private void RegisterSharedEnergyListeners(BattleAllyController[] allies)
    {
        if (allies == null)
            return;

        for (int i = 0; i < allies.Length; i++)
        {
            BattleAllyController ally = allies[i];
            if (ally == null)
                continue;

            ally.SelectedSkillChanged -= HandleAllySelectedSkillChanged;
            ally.SelectedSkillChanged += HandleAllySelectedSkillChanged;

            if (ally.speedButton != null)
            {
                ally.speedButton.ConnectionChanged -= HandleAllyConnectionChanged;
                ally.speedButton.ConnectionChanged += HandleAllyConnectionChanged;
            }
        }
    }

    private void HandleAllySelectedSkillChanged(BattleAllyController ally)
    {
        if (ally == null)
            return;

        int previousReserved = reservedSharedEnergy;

        if (ally.speedButton != null && ally.speedButton.ConnectedTarget != null && !CanSelectSkillForAlly(ally, ally.SelectedSkill))
        {
            if (!isRefreshingSharedEnergyReservations)
            {
                isRefreshingSharedEnergyReservations = true;
                ally.speedButton.DisconnectLine();
                isRefreshingSharedEnergyReservations = false;
            }

            BattleLog($"能量不足：{ally.GetDebugDisplayName()} 当前技能 {ally.SelectedSkill?.skillName ?? "未配置"} 无法维持连线。");
        }

        RefreshSharedEnergyReservations(activeRoundAllies);
        ShowSharedEnergySpendPopupIfNeeded(ally, previousReserved, reservedSharedEnergy);
        battleSkillInfoOverlay?.RefreshCurrentView();
    }

    private void HandleAllyConnectionChanged(SpeedButton button)
    {
        if (button == null || isRefreshingSharedEnergyReservations)
            return;

        BattleAllyController ally = button.GetComponentInParent<BattleAllyController>();
        if (ally == null)
            return;

        int previousReserved = reservedSharedEnergy;

        if (button.ConnectedTarget != null && !CanSelectSkillForAlly(ally, ally.SelectedSkill))
        {
            isRefreshingSharedEnergyReservations = true;
            button.DisconnectLine();
            isRefreshingSharedEnergyReservations = false;
            BattleLog($"能量不足：{ally.GetDebugDisplayName()} 无法使用技能 {ally.SelectedSkill?.skillName ?? "未配置"} 进行目标指定。");
        }

        RefreshSharedEnergyReservations(activeRoundAllies);
        ShowSharedEnergySpendPopupIfNeeded(ally, previousReserved, reservedSharedEnergy);
        battleSkillInfoOverlay?.RefreshCurrentView();
    }

    private int GetReservedEnergyCostForAlly(BattleAllyController ally)
    {
        if (ally == null || ally.IsDead || ally.speedButton == null || ally.speedButton.ConnectedTarget == null)
            return 0;

        return Mathf.Max(0, ally.SelectedSkill != null ? ally.SelectedSkill.energyCost : 0);
    }

    private void RefreshSharedEnergyReservations(BattleAllyController[] allies)
    {
        int totalReserved = 0;
        if (allies != null)
        {
            for (int i = 0; i < allies.Length; i++)
                totalReserved += GetReservedEnergyCostForAlly(allies[i]);
        }

        reservedSharedEnergy = Mathf.Clamp(totalReserved, 0, Mathf.Max(0, currentSharedEnergy));
        RefreshSharedEnergyUi();
    }

    private void ShowSharedEnergySpendPopupIfNeeded(BattleAllyController ally, int previousReserved, int currentReserved)
    {
        if (ally == null || battleFeedbackSystem == null)
            return;

        int delta = Mathf.Max(0, currentReserved - previousReserved);
        if (delta <= 0)
            return;

        Vector3 popupWorld = BattleActorVisualUtility.GetActorCenterWorld(ally.transform) + new Vector3(0f, 1.1f, 0f);
        battleFeedbackSystem.ShowCustomPopup(popupWorld, $"-{delta}", sharedEnergySpendPopupColor);
    }

    private void CommitReservedSharedEnergy()
    {
        currentSharedEnergy = Mathf.Clamp(currentSharedEnergy - reservedSharedEnergy, 0, currentSharedEnergyMax);
        reservedSharedEnergy = 0;
        RefreshSharedEnergyUi();
    }

    private int RestoreSharedEnergy(int amount)
    {
        if (amount <= 0)
            return 0;

        int previous = currentSharedEnergy;
        currentSharedEnergy = Mathf.Clamp(currentSharedEnergy + amount, 0, currentSharedEnergyMax);
        int restored = currentSharedEnergy - previous;
        if (restored > 0)
            RefreshSharedEnergyUi();
        return restored;
    }

    private void EnsureSharedEnergyUi()
    {
        if (sharedEnergyRoot != null)
            return;

        // 创建屏幕空间覆盖画布，挂到场景根节点（父物体为 null）
        // 不能挂在 BattleButton 或任何 Canvas 子节点下，否则 ScreenSpaceOverlay
        // 会受父 RectTransform 影响，导致位置跟随父对象而非屏幕角落。
        GameObject canvasGo = new GameObject("BattleSharedEnergyCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(null, false);

        sharedEnergyCanvas = canvasGo.GetComponent<Canvas>();
        sharedEnergyCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        sharedEnergyCanvas.sortingOrder = 9050;

        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        sharedEnergyRoot = canvasGo.GetComponent<RectTransform>();
        sharedEnergyRoot.anchorMin = Vector2.zero;
        sharedEnergyRoot.anchorMax = Vector2.one;
        sharedEnergyRoot.offsetMin = Vector2.zero;
        sharedEnergyRoot.offsetMax = Vector2.zero;

        // 能量面板（深色半透明背景，锁定到屏幕左上角）
        GameObject panelGo = new GameObject("EnergyPanel", typeof(RectTransform), typeof(Image));
        panelGo.transform.SetParent(sharedEnergyRoot, false);
        Image panelImage = panelGo.GetComponent<Image>();
        panelImage.color = sharedEnergyPanelBgColor;

        sharedEnergyPanelRect = panelGo.GetComponent<RectTransform>();
        sharedEnergyPanelRect.anchorMin = new Vector2(0f, 1f);
        sharedEnergyPanelRect.anchorMax = new Vector2(0f, 1f);
        sharedEnergyPanelRect.pivot = new Vector2(0f, 1f);
        sharedEnergyPanelRect.anchoredPosition = new Vector2(sharedEnergyPanelMargin.x, -sharedEnergyPanelMargin.y);
        sharedEnergyPanelRect.sizeDelta = sharedEnergyPanelSize;

        float panelW = sharedEnergyPanelSize.x;
        float panelH = sharedEnergyPanelSize.y;
        float pad     = 8f;
        float labelH  = 28f;
        float barH    = 16f;

        // 数字标签："⚡ 8 / 10"
        GameObject labelGo = new GameObject("EnergyLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelGo.transform.SetParent(panelGo.transform, false);
        sharedEnergyText = labelGo.GetComponent<TextMeshProUGUI>();
        sharedEnergyText.fontSize = sharedEnergyFontSize;
        sharedEnergyText.color    = sharedEnergyTextColor;
        sharedEnergyText.alignment = TextAlignmentOptions.MidlineLeft;
        sharedEnergyText.enableWordWrapping = false;
        if (sharedEnergyFont != null)
            sharedEnergyText.font = sharedEnergyFont;

        RectTransform labelRect = labelGo.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 1f);
        labelRect.anchorMax = new Vector2(1f, 1f);
        labelRect.pivot     = new Vector2(0f, 1f);
        labelRect.offsetMin = new Vector2(pad, -(pad + labelH));
        labelRect.offsetMax = new Vector2(-pad, -pad);

        // 能量条背景
        GameObject barBgGo = new GameObject("EnergyBarBg", typeof(RectTransform), typeof(Image));
        barBgGo.transform.SetParent(panelGo.transform, false);
        Image barBgImage = barBgGo.GetComponent<Image>();
        barBgImage.color = sharedEnergyBarBgColor;

        RectTransform barBgRect = barBgGo.GetComponent<RectTransform>();
        barBgRect.anchorMin = new Vector2(0f, 1f);
        barBgRect.anchorMax = new Vector2(1f, 1f);
        barBgRect.pivot     = new Vector2(0f, 1f);
        barBgRect.offsetMin = new Vector2(pad, -(pad + labelH + 6f + barH));
        barBgRect.offsetMax = new Vector2(-pad, -(pad + labelH + 6f));

        // 能量条填充（锁定左端，宽度按拟充量缩放）
        GameObject barFillGo = new GameObject("EnergyBarFill", typeof(RectTransform), typeof(Image));
        barFillGo.transform.SetParent(barBgGo.transform, false);
        sharedEnergyBarFill = barFillGo.GetComponent<Image>();
        sharedEnergyBarFill.color = sharedEnergyBarFillColor;

        RectTransform barFillRect = barFillGo.GetComponent<RectTransform>();
        barFillRect.anchorMin = new Vector2(0f, 0f);
        barFillRect.anchorMax = new Vector2(1f, 1f);
        barFillRect.pivot     = new Vector2(0f, 0.5f);
        barFillRect.offsetMin = Vector2.zero;
        barFillRect.offsetMax = Vector2.zero;
        // anchorMax.x 会在 RefreshSharedEnergyUi 中按能量比例动态设置

        GameObject roundPanelGo = new GameObject("RoundPanel", typeof(RectTransform), typeof(Image));
        roundPanelGo.transform.SetParent(sharedEnergyRoot, false);
        Image roundPanelImage = roundPanelGo.GetComponent<Image>();
        roundPanelImage.color = sharedEnergyPanelBgColor;

        roundCounterPanelRect = roundPanelGo.GetComponent<RectTransform>();
        roundCounterPanelRect.anchorMin = new Vector2(0.5f, 1f);
        roundCounterPanelRect.anchorMax = new Vector2(0.5f, 1f);
        roundCounterPanelRect.pivot = new Vector2(0.5f, 1f);
        roundCounterPanelRect.anchoredPosition = new Vector2(0f, -sharedEnergyPanelMargin.y);
        roundCounterPanelRect.sizeDelta = roundCounterPanelSize;

        GameObject roundLabelGo = new GameObject("RoundLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
        roundLabelGo.transform.SetParent(roundPanelGo.transform, false);
        roundCounterText = roundLabelGo.GetComponent<TextMeshProUGUI>();
        roundCounterText.fontSize = sharedEnergyFontSize;
        roundCounterText.color = sharedEnergyTextColor;
        roundCounterText.alignment = TextAlignmentOptions.Center;
        roundCounterText.enableWordWrapping = false;
        if (sharedEnergyFont != null)
            roundCounterText.font = sharedEnergyFont;

        RectTransform roundLabelRect = roundLabelGo.GetComponent<RectTransform>();
        roundLabelRect.anchorMin = Vector2.zero;
        roundLabelRect.anchorMax = Vector2.one;
        roundLabelRect.offsetMin = new Vector2(8f, 8f);
        roundLabelRect.offsetMax = new Vector2(-8f, -8f);
    }

    private void RefreshSharedEnergyUi()
    {
        EnsureSharedEnergyUi();
        if (sharedEnergyText == null)
            return;

        ApplyBattleHudLayout();

        int availableEnergy = Mathf.Max(0, currentSharedEnergy - reservedSharedEnergy);
        sharedEnergyText.text = $"能量 {availableEnergy} / {currentSharedEnergyMax}";
        if (roundCounterText != null)
            roundCounterText.text = $"第 {currentRound} 回合";

        // 更新能量条填充比例
        if (sharedEnergyBarFill != null)
        {
            float fillRatio = currentSharedEnergyMax > 0
                ? Mathf.Clamp01((float)availableEnergy / currentSharedEnergyMax)
                : 0f;
            RectTransform fillRect = sharedEnergyBarFill.rectTransform;
            fillRect.anchorMax = new Vector2(fillRatio, 1f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
        }
    }

    private void ApplyBattleHudLayout()
    {
        if (sharedEnergyPanelRect != null)
        {
            sharedEnergyPanelRect.anchorMin = new Vector2(0f, 1f);
            sharedEnergyPanelRect.anchorMax = new Vector2(0f, 1f);
            sharedEnergyPanelRect.pivot = new Vector2(0f, 1f);
            sharedEnergyPanelRect.anchoredPosition = new Vector2(sharedEnergyPanelMargin.x, -sharedEnergyPanelMargin.y);
            sharedEnergyPanelRect.sizeDelta = sharedEnergyPanelSize;
        }

        if (roundCounterPanelRect != null)
        {
            roundCounterPanelRect.anchorMin = new Vector2(0.5f, 1f);
            roundCounterPanelRect.anchorMax = new Vector2(0.5f, 1f);
            roundCounterPanelRect.pivot = new Vector2(0.5f, 1f);
            roundCounterPanelRect.anchoredPosition = new Vector2(0f, -sharedEnergyPanelMargin.y);
            roundCounterPanelRect.sizeDelta = roundCounterPanelSize;
        }
    }

    private IEnumerator ExecuteCombatPhaseRoutine(BattleAllyController[] allies, BattleEnemyController[] enemies)
    {
        isResolvingCombat = true;
        activeRoundAllies = allies;
        activeRoundEnemies = enemies;
        CommitReservedSharedEnergy();
        BattleLog("\u25c6\u25c6\u25c6 \u6218\u6597\u7ed3\u7b97\u9636\u6bb5 \u25c6\u25c6\u25c6");

        SpeedButton.CombatLocked = true;

        foreach (BattleAllyController ally in allies)
        {
            if (ally.speedButton == null)
                continue;

            ally.speedButton.IsInteractable = false;
            ally.speedButton.HideForCombat();
        }

        foreach (BattleEnemyController enemy in enemies)
        {
            IReadOnlyList<SpeedButton> enemyButtons = GetEnemySpeedButtons(enemy);
            for (int i = 0; i < enemyButtons.Count; i++)
            {
                if (enemyButtons[i] != null)
                    enemyButtons[i].HideForCombat();
            }
        }

        if (battleSkillInfoOverlay != null && battleSkillInfoOverlay.IsVisible)
            battleSkillInfoOverlay.Hide(false);

        List<CombatAction> actions = BuildCombatActions(allies, enemies);
        foreach (CombatAction action in actions)
        {
            switch (action.Type)
            {
                case CombatActionType.Clash:
                    yield return ResolveClashAction(action.Ally, action.Enemy, action.EnemyButton);
                    break;
                case CombatActionType.AllySingleAttack:
                    yield return ResolveAllySingleAttackAction(action.Ally, action.Enemy);
                    break;
                case CombatActionType.EnemySingleAttack:
                    yield return ResolveEnemySingleAttackAction(action.Enemy, action.TargetAlly, action.EnemyButton);
                    break;
            }

            if (TryHandleBattleCompletion(allies, enemies))
            {
                activeRoundAllies = null;
                activeRoundEnemies = null;
                isResolvingCombat = false;
                yield break;
            }
        }

        foreach (BattleAllyController ally in allies)
        {
            if (ally != null)
                ally.FinalizeRoundSkillHistory();
        }

        BattleLog("\u2500\u2500 \u56de\u5408\u7ed3\u7b97\u6c47\u603b \u2500\u2500");
        foreach (BattleAllyController ally in allies)
        {
            if (!ally.IsDead)
                BattleLog($"\u53cb\u65b9 {ally.GetDebugDisplayName()} HP {ally.CurrentHP}/{ally.MaxHP}");
        }

        foreach (BattleEnemyController enemy in enemies)
        {
            if (!enemy.IsDead)
                BattleLog($"\u654c\u65b9 {enemy.GetDebugDisplayName()} HP {enemy.CurrentHP}/{enemy.MaxHP}");
        }

        ApplyRoundEndStatusEffectsGeneric(allies, enemies);

        SpeedButton.CombatLocked = false;
        BattleLog("\u518d\u6b21\u6309\u4e0b\u6218\u6597\u6309\u94ae\u5f00\u59cb\u4e0b\u4e00\u56de\u5408\u76ee\u6807\u6307\u5b9a\u3002");

        currentPhase = BattlePhase.Ready;
        currentRound++;
        activeRoundAllies = null;
        activeRoundEnemies = null;
        isResolvingCombat = false;
    }

    private IEnumerator PlayClashPresentation(
        BattleAllyController ally,
        BattleEnemyController enemy,
        bool hasWinner,
        bool allyWon,
        EnemySkill_SO.DicePowerRange allyRange,
        EnemySkill_SO.DicePowerRange enemyRange,
        int allyPower,
        int enemyPower,
        int allyPowerModifier,
        int enemyPowerModifier,
        float clashCameraPower,
        float hitCameraPower,
        EnemySkill_SO.DicePresentationSettings allyClashPresentationSettings,
        EnemySkill_SO.DicePresentationSettings enemyClashPresentationSettings,
        EnemySkill_SO.DicePresentationSettings hitPresentationSettings,
        System.Action onImpactResolved)
    {
        if (battlePresentationSystem == null)
            yield break;

        yield return battlePresentationSystem.PlayClashSequence(
            ally.transform,
            enemy.transform,
            hasWinner,
            allyWon,
            allyRange,
            enemyRange,
            allyPower,
            enemyPower,
            allyPowerModifier,
            enemyPowerModifier,
            clashCameraPower,
            hitCameraPower,
            allyClashPresentationSettings,
            enemyClashPresentationSettings,
            hitPresentationSettings,
            onImpactResolved);
    }

    private IEnumerator PlaySingleAttackPresentation(
        Transform attacker,
        Transform defender,
        bool attackerIsAlly,
        EnemySkill_SO.DicePowerRange attackRange,
        int attackPower,
        int attackPowerModifier,
        float hitCameraPower,
        EnemySkill_SO.DicePresentationSettings dicePresentationSettings,
        System.Action onImpactResolved)
    {
        if (battlePresentationSystem == null)
            yield break;

        yield return battlePresentationSystem.PlaySingleAttackSequence(
            attacker,
            defender,
            attackerIsAlly,
            attackRange,
            attackPower,
            attackPowerModifier,
            hitCameraPower,
            dicePresentationSettings,
            onImpactResolved);
    }

    private List<CombatAction> BuildCombatActions(BattleAllyController[] allies, BattleEnemyController[] enemies)
    {
        List<CombatAction> actions = new List<CombatAction>();
        HashSet<SpeedButton> clashEnemyButtons = new HashSet<SpeedButton>();
        int sequenceIndex = 0;

        foreach (BattleAllyController ally in allies)
        {
            if (ally.IsDead || ally.speedButton == null)
                continue;

            SpeedButton enemyButton = ally.speedButton.ConnectedTarget;
            if (enemyButton == null)
            {
                BattleLog($"{ally.GetDebugDisplayName()} \u672a\u6307\u5b9a\u76ee\u6807\uff0c\u672c\u56de\u5408\u8df3\u8fc7\u3002");
                continue;
            }

            BattleEnemyController enemy = enemyButton.GetComponentInParent<BattleEnemyController>();
            if (enemy == null || enemy.IsDead)
                continue;

            CombatAction action = new CombatAction
            {
                Ally = ally,
                Enemy = enemy,
                EnemyButton = enemyButton,
                SequenceIndex = sequenceIndex++
            };

            if (ally.speedButton.IsContestedConnection && !clashEnemyButtons.Contains(enemyButton))
            {
                action.Type = CombatActionType.Clash;
                action.PrioritySpeed = Mathf.Max(ally.Speed, enemy.Speed);
                action.TiePriority = 0;
                clashEnemyButtons.Add(enemyButton);
            }
            else
            {
                action.Type = CombatActionType.AllySingleAttack;
                action.PrioritySpeed = ally.Speed;
                action.TiePriority = 1;
            }

            actions.Add(action);
        }

        foreach (BattleEnemyController enemy in enemies)
        {
            if (enemy.IsDead)
                continue;

            IReadOnlyList<SpeedButton> enemyButtons = GetEnemySpeedButtons(enemy);
            for (int i = 0; i < enemyButtons.Count; i++)
            {
                SpeedButton enemyButton = enemyButtons[i];
                if (enemyButton == null || clashEnemyButtons.Contains(enemyButton))
                    continue;

                SpeedButton targetAllyButton = enemyButton.ConnectedTarget;
                if (targetAllyButton == null)
                    continue;

                BattleAllyController targetAlly = targetAllyButton.GetComponentInParent<BattleAllyController>();
                if (targetAlly == null || targetAlly.IsDead)
                    continue;

                actions.Add(new CombatAction
                {
                    Type = CombatActionType.EnemySingleAttack,
                    Enemy = enemy,
                    EnemyButton = enemyButton,
                    TargetAlly = targetAlly,
                    PrioritySpeed = enemy.Speed,
                    TiePriority = 1,
                    SequenceIndex = sequenceIndex++
                });
            }
        }

        actions.Sort(CompareCombatAction);
        return actions;
    }

    private static IReadOnlyList<SpeedButton> GetEnemySpeedButtons(BattleEnemyController enemy)
    {
        if (enemy == null)
            return Array.Empty<SpeedButton>();

        IReadOnlyList<SpeedButton> buttons = enemy.SpeedButtons;
        if (buttons != null && buttons.Count > 0)
            return buttons;

        return enemy.speedButton != null
            ? new[] { enemy.speedButton }
            : Array.Empty<SpeedButton>();
    }

    private static int EvaluatePowerModifier(
        EnemySkill_SO skill,
        int diceIndex,
        EnemySkill_SO.DiceEffectTrigger trigger,
        int selfSpeed,
        int targetSpeed,
        Func<BattleStatus_SO, int> getSelfStatusStacksByAsset,
        Func<string, int> getSelfStatusStacksById,
        Func<BattleStatus_SO, int> getTargetStatusStacksByAsset,
        Func<string, int> getTargetStatusStacksById,
        EnemySkill_SO targetSkill = null)
    {
        if (skill == null)
            return 0;

        EnemySkill_SO.DiceEffectSettings settings = skill.GetDiceEffectSettings(diceIndex);
        if (settings.powerModifiers == null || settings.powerModifiers.Length == 0)
            return 0;

        int totalBonus = 0;
        for (int i = 0; i < settings.powerModifiers.Length; i++)
        {
            EnemySkill_SO.ConditionalPowerModifierEntry modifier = settings.powerModifiers[i];
            if (modifier.trigger != trigger)
                continue;

            if (!modifier.useCondition)
            {
                if (modifier.powerBonus != 0)
                    totalBonus += modifier.powerBonus;
                continue;
            }

            totalBonus += EvaluateConditionalPowerModifier(
                modifier,
                selfSpeed,
                targetSpeed,
                getSelfStatusStacksByAsset,
                getSelfStatusStacksById,
                getTargetStatusStacksByAsset,
                getTargetStatusStacksById,
                targetSkill);
        }

        return totalBonus;
    }

    private static int EvaluateConditionalPowerModifier(
        EnemySkill_SO.ConditionalPowerModifierEntry modifier,
        int selfSpeed,
        int targetSpeed,
        Func<BattleStatus_SO, int> getSelfStatusStacksByAsset,
        Func<string, int> getSelfStatusStacksById,
        Func<BattleStatus_SO, int> getTargetStatusStacksByAsset,
        Func<string, int> getTargetStatusStacksById,
        EnemySkill_SO targetSkill)
    {
        switch (modifier.condition)
        {
            case EnemySkill_SO.PowerModifierCondition.SelfHasStatus:
                return ResolveSelfStatusStacks(modifier, getSelfStatusStacksByAsset, getSelfStatusStacksById) > 0
                    ? modifier.powerBonus
                    : 0;
            case EnemySkill_SO.PowerModifierCondition.SelfStatusStacksGreaterThan:
                return ResolveSelfStatusStacks(modifier, getSelfStatusStacksByAsset, getSelfStatusStacksById) > Mathf.Max(0, modifier.requiredStatusStacks)
                    ? modifier.powerBonus
                    : 0;
            case EnemySkill_SO.PowerModifierCondition.SelfStatusStacksGreaterOrEqual:
                return ResolveSelfStatusStacks(modifier, getSelfStatusStacksByAsset, getSelfStatusStacksById) >= Mathf.Max(1, modifier.requiredStatusStacks)
                    ? modifier.powerBonus
                    : 0;
            case EnemySkill_SO.PowerModifierCondition.SelfStatusStacksLessThan:
                return ResolveSelfStatusStacks(modifier, getSelfStatusStacksByAsset, getSelfStatusStacksById) < Mathf.Max(1, modifier.requiredStatusStacks)
                    ? modifier.powerBonus
                    : 0;
            case EnemySkill_SO.PowerModifierCondition.SelfStatusStacksLessOrEqual:
                return ResolveSelfStatusStacks(modifier, getSelfStatusStacksByAsset, getSelfStatusStacksById) <= Mathf.Max(0, modifier.requiredStatusStacks)
                    ? modifier.powerBonus
                    : 0;
            case EnemySkill_SO.PowerModifierCondition.TargetSkillIsSpecific:
                return modifier.targetSkillData != null && targetSkill == modifier.targetSkillData
                    ? modifier.powerBonus
                    : 0;
            case EnemySkill_SO.PowerModifierCondition.TargetHasStatus:
                return ResolveTargetStatusStacks(modifier, getTargetStatusStacksByAsset, getTargetStatusStacksById) > 0
                    ? modifier.powerBonus
                    : 0;
            case EnemySkill_SO.PowerModifierCondition.TargetStatusStacksAtLeast:
                return ResolveTargetStatusStacks(modifier, getTargetStatusStacksByAsset, getTargetStatusStacksById) >= Mathf.Max(1, modifier.requiredStatusStacks)
                    ? modifier.powerBonus
                    : 0;
            case EnemySkill_SO.PowerModifierCondition.TargetStatusStacksPerThreshold:
                {
                    int stacks = ResolveTargetStatusStacks(modifier, getTargetStatusStacksByAsset, getTargetStatusStacksById);
                    int threshold = Mathf.Max(1, modifier.requiredStatusStacks);
                    int steps = stacks / threshold;
                    return Mathf.Max(0, steps) * modifier.powerBonusPerStep;
                }
            default:
                return IsPowerModifierConditionMet(modifier.condition, selfSpeed, targetSpeed, modifier.requiredSpeedValue, modifier.requiredSpeedDifference)
                    ? modifier.powerBonus
                    : 0;
        }
    }

    private static int ResolveSelfStatusStacks(
        EnemySkill_SO.ConditionalPowerModifierEntry modifier,
        Func<BattleStatus_SO, int> getSelfStatusStacksByAsset,
        Func<string, int> getSelfStatusStacksById)
    {
        if (modifier.targetStatusData != null && getSelfStatusStacksByAsset != null)
            return Mathf.Max(0, getSelfStatusStacksByAsset(modifier.targetStatusData));

        if (modifier.targetStatusType == EnemySkill_SO.DiceStatusType.Burn && getSelfStatusStacksById != null)
            return Mathf.Max(0, getSelfStatusStacksById("burn"));

        return 0;
    }

    private static bool IsPowerModifierConditionMet(EnemySkill_SO.PowerModifierCondition condition, int selfSpeed, int targetSpeed, int requiredSpeedValue = 0, int requiredSpeedDifference = 0)
    {
        switch (condition)
        {
            case EnemySkill_SO.PowerModifierCondition.SelfSpeedHigherThanTarget:
                return selfSpeed - targetSpeed >= Mathf.Max(1, requiredSpeedDifference);
            case EnemySkill_SO.PowerModifierCondition.SelfSpeedLowerThanTarget:
                return targetSpeed - selfSpeed >= Mathf.Max(1, requiredSpeedDifference);
            case EnemySkill_SO.PowerModifierCondition.SelfSpeedEqualToTarget:
                return selfSpeed == targetSpeed;
            case EnemySkill_SO.PowerModifierCondition.SelfSpeedHigherThanValue:
                return selfSpeed >= requiredSpeedValue;
            case EnemySkill_SO.PowerModifierCondition.SelfSpeedLowerThanValue:
                return selfSpeed <= requiredSpeedValue;
            default:
                return false;
        }
    }

    private static int ResolveTargetStatusStacks(
        EnemySkill_SO.ConditionalPowerModifierEntry modifier,
        Func<BattleStatus_SO, int> getTargetStatusStacksByAsset,
        Func<string, int> getTargetStatusStacksById)
    {
        if (modifier.targetStatusData != null && getTargetStatusStacksByAsset != null)
            return Mathf.Max(0, getTargetStatusStacksByAsset(modifier.targetStatusData));

        if (modifier.targetStatusType == EnemySkill_SO.DiceStatusType.Burn && getTargetStatusStacksById != null)
            return Mathf.Max(0, getTargetStatusStacksById("burn"));

        return 0;
    }

    private static string FormatPowerWithBonus(int basePower, int bonusPower)
    {
        if (bonusPower == 0)
            return basePower.ToString();

        int finalPower = Mathf.Max(1, basePower + bonusPower);
        return $"{basePower}{(bonusPower > 0 ? "+" : string.Empty)}{bonusPower}={finalPower}";
    }

    private static int ClampFinalPower(int basePower, int bonusPower)
    {
        return Mathf.Max(1, basePower + bonusPower);
    }

    private static bool IsDefenseSkill(EnemySkill_SO skill)
    {
        return skill != null && skill.IsDefenseSkill;
    }

    private void ShowZeroDamagePopup(Transform target, bool targetIsEnemy, int powerModifierAmount = 0)
    {
        if (target == null)
            return;

        battleFeedbackSystem?.ShowDamageResultPopup(
            BattleActorVisualUtility.GetActorCenterWorld(target) + new Vector3(0f, 1.4f, 0f),
            0,
            targetIsEnemy,
            0,
            powerModifierAmount);
    }

    private IEnumerator ResolveClashAction(BattleAllyController ally, BattleEnemyController enemy, SpeedButton enemyButton)
    {
        if (ally == null || enemy == null || ally.IsDead || enemy.IsDead)
            yield break;

        ally.MarkCurrentRoundSkillUsed();
        enemy.ActivateSelectedSkillForButton(enemyButton);
        TryApplyEnemySkillStageTransition(enemy, enemy.SelectedSkill);

        int allyDiceCount = ally.DiceCount;
        int enemyDiceCount = enemy.DiceCount;
        int clashCount = Mathf.Min(allyDiceCount, enemyDiceCount);

        BattleLog($"[\u62fc\u70b9] {ally.GetDebugDisplayName()}[{ally.GetSelectedSkillDebugSummary()}] vs {enemy.GetDebugDisplayName()}[{enemy.GetSelectedSkillDebugSummary()}]");

        for (int i = 0; i < clashCount; i++)
        {
            if (ally.IsDead || enemy.IsDead)
                yield break;

            bool allyDefenseSkill = IsDefenseSkill(ally.SelectedSkill);
            bool enemyDefenseSkill = IsDefenseSkill(enemy.SelectedSkill);
            PlayerRelicDiceModifier allyRelicModifier = ResolvePlayerRelicDiceModifier(ally, enemy, i);
            EnemySkill_SO.DicePowerRange allyRange = ApplyPlayerRelicDiceRangeModifier(ally.GetDicePowerRange(i), allyRelicModifier);
            EnemySkill_SO.DicePowerRange enemyRange = enemy.GetDicePowerRange(i);
            int allyBasePower = RollDiceFromRange(allyRange);
            int enemyBasePower = enemy.RollDice(i);
            int allyBonusPower =
                allyRelicModifier.powerBonus +
                EvaluatePowerModifier(ally.SelectedSkill, i, EnemySkill_SO.DiceEffectTrigger.OnClash, ally.Speed, enemy.Speed, ally.GetStatusStacks, ally.GetStatusStacks, enemy.GetStatusStacks, enemy.GetStatusStacks, enemy.SelectedSkill) +
                EvaluatePowerModifier(ally.SelectedSkill, i, EnemySkill_SO.DiceEffectTrigger.OnSkillUse, ally.Speed, enemy.Speed, ally.GetStatusStacks, ally.GetStatusStacks, enemy.GetStatusStacks, enemy.GetStatusStacks, enemy.SelectedSkill);
            int enemyBonusPower =
                EvaluatePowerModifier(enemy.SelectedSkill, i, EnemySkill_SO.DiceEffectTrigger.OnClash, enemy.Speed, ally.Speed, enemy.GetStatusStacks, enemy.GetStatusStacks, ally.GetStatusStacks, ally.GetStatusStacks, ally.SelectedSkill) +
                EvaluatePowerModifier(enemy.SelectedSkill, i, EnemySkill_SO.DiceEffectTrigger.OnSkillUse, enemy.Speed, ally.Speed, enemy.GetStatusStacks, enemy.GetStatusStacks, ally.GetStatusStacks, ally.GetStatusStacks, ally.SelectedSkill);
            int allyPower = ClampFinalPower(allyBasePower, allyBonusPower);
            int enemyPower = ClampFinalPower(enemyBasePower, enemyBonusPower);
            bool hasWinner = false;
            bool allyWon = false;
            int allyWinAttackBonus = 0;
            int enemyWinAttackBonus = 0;
            int allyHitPower = allyPower;
            int enemyHitPower = enemyPower;
            EnemySkill_SO.DicePresentationSettings allyFx = ally.GetDicePresentationSettings(i);
            EnemySkill_SO.DicePresentationSettings enemyFx = enemy.GetDicePresentationSettings(i);
            EnemySkill_SO.DicePresentationSettings hitFx = allyFx;

            ApplyAllyTriggeredEffects(ally, enemy, i, EnemySkill_SO.DiceEffectTrigger.OnSkillUse, 0);
            ApplyEnemyTriggeredEffects(enemy, ally, i, EnemySkill_SO.DiceEffectTrigger.OnSkillUse, 0);

            Action resolveImpactAction;
            string postClashLog;

            if (!allyDefenseSkill && !enemyDefenseSkill)
            {
                hasWinner = allyPower != enemyPower;
                allyWon = allyPower > enemyPower;
                allyWinAttackBonus = allyWon
                    ? EvaluatePowerModifier(ally.SelectedSkill, i, EnemySkill_SO.DiceEffectTrigger.OnClashWinAttack, ally.Speed, enemy.Speed, ally.GetStatusStacks, ally.GetStatusStacks, enemy.GetStatusStacks, enemy.GetStatusStacks, enemy.SelectedSkill)
                    : 0;
                enemyWinAttackBonus = !allyWon && enemyPower > allyPower
                    ? EvaluatePowerModifier(enemy.SelectedSkill, i, EnemySkill_SO.DiceEffectTrigger.OnClashWinAttack, enemy.Speed, ally.Speed, enemy.GetStatusStacks, enemy.GetStatusStacks, ally.GetStatusStacks, ally.GetStatusStacks, ally.SelectedSkill)
                    : 0;
                allyHitPower = Mathf.Max(1, allyPower + allyWinAttackBonus);
                enemyHitPower = Mathf.Max(1, enemyPower + enemyWinAttackBonus);
                hitFx = allyWon ? allyFx : enemyFx;

                resolveImpactAction = () =>
                {
                    if (allyWon)
                    {
                        ApplyAllyHit(ally, enemy, allyHitPower, i, false, allyBonusPower + allyWinAttackBonus);
                        ApplyEnemyTriggeredEffects(enemy, ally, i, EnemySkill_SO.DiceEffectTrigger.OnMiss, 0);
                    }
                    else if (enemyPower > allyPower)
                    {
                        ApplyEnemyHit(enemy, ally, enemyHitPower, i, false, enemyBonusPower + enemyWinAttackBonus);
                        ApplyAllyTriggeredEffects(ally, enemy, i, EnemySkill_SO.DiceEffectTrigger.OnMiss, 0);
                    }
                    else
                    {
                        ApplyAllyTriggeredEffects(ally, enemy, i, EnemySkill_SO.DiceEffectTrigger.OnMiss, 0);
                        ApplyEnemyTriggeredEffects(enemy, ally, i, EnemySkill_SO.DiceEffectTrigger.OnMiss, 0);
                    }
                };

                if (allyWon)
                    postClashLog = $"-> 友方胜！追击威力 {FormatPowerWithBonus(allyPower, allyWinAttackBonus)} ，对 {enemy.GetDebugDisplayName()} 造成 {allyHitPower} 伤害，剩余HP {enemy.CurrentHP}/{enemy.MaxHP}";
                else if (enemyPower > allyPower)
                    postClashLog = $"-> 敌方胜！追击威力 {FormatPowerWithBonus(enemyPower, enemyWinAttackBonus)} ，{enemy.GetDebugDisplayName()} 对 {ally.GetDebugDisplayName()} 造成 {enemyHitPower} 伤害，剩余HP {ally.CurrentHP}/{ally.MaxHP}";
                else
                    postClashLog = $"-> 平局（{allyPower} = {enemyPower}），双方无伤害";
            }
            else if (!allyDefenseSkill && enemyDefenseSkill)
            {
                bool attackBreaksDefense = allyPower > enemyPower;
                hasWinner = attackBreaksDefense;
                allyWon = attackBreaksDefense;
                allyHitPower = Mathf.Max(0, allyPower - enemyPower);
                hitFx = allyFx;

                resolveImpactAction = () =>
                {
                    if (attackBreaksDefense && allyHitPower > 0)
                        ApplyAllyHit(ally, enemy, allyHitPower, i, false, allyBonusPower);
                    else
                        ShowZeroDamagePopup(enemy.transform, true, allyBonusPower);
                };

                postClashLog = attackBreaksDefense
                    ? $"-> 敌方防御骰抵消 {enemyPower} 点伤害，{ally.GetDebugDisplayName()} 仍造成 {allyHitPower} 伤害，{enemy.GetDebugDisplayName()} 剩余HP {enemy.CurrentHP}/{enemy.MaxHP}"
                    : enemyPower > allyPower
                        ? $"-> 敌方防御成功！完全免疫本次伤害与命中时词条（{enemyPower} > {allyPower}）"
                        : $"-> 敌方格挡住了本次攻击，最终伤害为 0（{enemyPower} = {allyPower}）";
            }
            else if (allyDefenseSkill && !enemyDefenseSkill)
            {
                bool attackBreaksDefense = enemyPower > allyPower;
                hasWinner = attackBreaksDefense;
                allyWon = false;
                enemyHitPower = Mathf.Max(0, enemyPower - allyPower);
                hitFx = enemyFx;
                int restoredDefenseEnergy = 0;

                resolveImpactAction = () =>
                {
                    if (attackBreaksDefense && enemyHitPower > 0)
                        ApplyEnemyHit(enemy, ally, enemyHitPower, i, false, enemyBonusPower);
                    else
                        ShowZeroDamagePopup(ally.transform, false, enemyBonusPower);
                };

                if (!attackBreaksDefense && allyPower > enemyPower)
                {
                    restoredDefenseEnergy = RestoreSharedEnergy(allyPower - enemyPower);
                    if (restoredDefenseEnergy > 0)
                    {
                        battleFeedbackSystem?.ShowCustomPopup(
                            BattleActorVisualUtility.GetActorCenterWorld(ally.transform) + new Vector3(0f, 1.1f, 0f),
                            $"+{restoredDefenseEnergy}",
                            sharedEnergyBarFillColor);
                    }
                }

                postClashLog = attackBreaksDefense
                    ? $"-> 友方防御骰抵消 {allyPower} 点伤害，{enemy.GetDebugDisplayName()} 仍造成 {enemyHitPower} 伤害，{ally.GetDebugDisplayName()} 剩余HP {ally.CurrentHP}/{ally.MaxHP}"
                    : allyPower > enemyPower
                        ? $"-> 友方防御成功！完全免疫本次伤害与命中时词条（{allyPower} > {enemyPower}），并恢复 {restoredDefenseEnergy} 能量"
                        : $"-> 友方格挡住了本次攻击，最终伤害为 0（{allyPower} = {enemyPower}）";
            }
            else
            {
                resolveImpactAction = () => { };
                postClashLog = $"-> 双方均为防御技能，本次拼点不会造成伤害（{allyPower} vs {enemyPower}）";
            }

            yield return PlayClashPresentation(
                ally,
                enemy,
                hasWinner,
                allyWon,
                allyRange,
                enemyRange,
                allyPower,
                enemyPower,
                allyBonusPower,
                enemyBonusPower,
                Mathf.Max(allyPower, enemyPower),
                allyWon ? allyHitPower : enemyHitPower,
                allyFx,
                enemyFx,
                hitFx,
                resolveImpactAction);

            BattleLog($"\u7b2c {i + 1} \u6bb5\u62fc\u70b9\uff1a{ally.GetDebugDisplayName()}({FormatPowerWithBonus(allyBasePower, allyBonusPower)}) vs {enemy.GetDebugDisplayName()}({FormatPowerWithBonus(enemyBasePower, enemyBonusPower)})");
            BattleLog(postClashLog);
        }

        bool allyRemainingFirst = ally.Speed >= enemy.Speed;
        if (allyRemainingFirst)
        {
            yield return ResolveRemainingAllyClashDice(ally, enemy, clashCount, allyDiceCount);
            yield return ResolveRemainingEnemyClashDice(enemy, ally, clashCount, enemyDiceCount);
        }
        else
        {
            yield return ResolveRemainingEnemyClashDice(enemy, ally, clashCount, enemyDiceCount);
            yield return ResolveRemainingAllyClashDice(ally, enemy, clashCount, allyDiceCount);
        }
    }

    private IEnumerator ResolveRemainingAllyClashDice(BattleAllyController ally, BattleEnemyController enemy, int startIndex, int totalDiceCount)
    {
        if (ally == null || enemy == null)
            yield break;

        for (int i = startIndex; i < totalDiceCount; i++)
        {
            if (ally.IsDead || enemy.IsDead)
                yield break;

            PlayerRelicDiceModifier allyRelicModifier = ResolvePlayerRelicDiceModifier(ally, enemy, i);
            EnemySkill_SO.DicePowerRange allyRange = ApplyPlayerRelicDiceRangeModifier(ally.GetDicePowerRange(i), allyRelicModifier);
            int allyBasePower = RollDiceFromRange(allyRange);
            int allyBonusPower =
                allyRelicModifier.powerBonus +
                EvaluatePowerModifier(ally.SelectedSkill, i, EnemySkill_SO.DiceEffectTrigger.OnSkillUse, ally.Speed, enemy.Speed, ally.GetStatusStacks, ally.GetStatusStacks, enemy.GetStatusStacks, enemy.GetStatusStacks) +
                EvaluatePowerModifier(ally.SelectedSkill, i, EnemySkill_SO.DiceEffectTrigger.OnSingleAttack, ally.Speed, enemy.Speed, ally.GetStatusStacks, ally.GetStatusStacks, enemy.GetStatusStacks, enemy.GetStatusStacks);
            int allyPower = ClampFinalPower(allyBasePower, allyBonusPower);
            EnemySkill_SO.DicePresentationSettings diceFx = ally.GetDicePresentationSettings(i);
            bool isDefenseSkill = IsDefenseSkill(ally.SelectedSkill);
            if (isDefenseSkill)
            {
                BattleLog($"第 {i + 1} 段（友方余骰单方面）：{ally.GetDebugDisplayName()}[{ally.GetSelectedSkillDebugSummary()}]({FormatPowerWithBonus(allyBasePower, allyBonusPower)}) 为防御技能，未与攻击骰拼点，本次无效");
                continue;
            }

            ApplyAllyTriggeredEffects(ally, enemy, i, EnemySkill_SO.DiceEffectTrigger.OnSkillUse, 0);
            yield return PlaySingleAttackPresentation(
                ally.transform,
                enemy.transform,
                true,
                allyRange,
                allyPower,
                allyBonusPower,
                allyPower,
                diceFx,
                isDefenseSkill ? null : () => ApplyAllyHit(ally, enemy, allyPower, i, true, allyBonusPower));

            BattleLog($"\u7b2c {i + 1} \u6bb5\uff08\u53cb\u65b9\u4f59\u9ab0\u5355\u65b9\u9762\uff09\uff1a{ally.GetDebugDisplayName()}[{ally.GetSelectedSkillDebugSummary()}]({FormatPowerWithBonus(allyBasePower, allyBonusPower)}) -> {enemy.GetDebugDisplayName()}\uff0c\u5269\u4f59HP {enemy.CurrentHP}/{enemy.MaxHP}");
        }
    }

    private IEnumerator ResolveRemainingEnemyClashDice(BattleEnemyController enemy, BattleAllyController ally, int startIndex, int totalDiceCount)
    {
        if (enemy == null || ally == null)
            yield break;

        for (int i = startIndex; i < totalDiceCount; i++)
        {
            if (enemy.IsDead || ally.IsDead)
                yield break;

            EnemySkill_SO.DicePowerRange enemyRange = enemy.GetDicePowerRange(i);
            int enemyBasePower = enemy.RollDice(i);
            int enemyBonusPower =
                EvaluatePowerModifier(enemy.SelectedSkill, i, EnemySkill_SO.DiceEffectTrigger.OnSkillUse, enemy.Speed, ally.Speed, enemy.GetStatusStacks, enemy.GetStatusStacks, ally.GetStatusStacks, ally.GetStatusStacks) +
                EvaluatePowerModifier(enemy.SelectedSkill, i, EnemySkill_SO.DiceEffectTrigger.OnSingleAttack, enemy.Speed, ally.Speed, enemy.GetStatusStacks, enemy.GetStatusStacks, ally.GetStatusStacks, ally.GetStatusStacks);
            int enemyPower = ClampFinalPower(enemyBasePower, enemyBonusPower);
            EnemySkill_SO.DicePresentationSettings diceFx = enemy.GetDicePresentationSettings(i);
            bool isDefenseSkill = IsDefenseSkill(enemy.SelectedSkill);
            if (isDefenseSkill)
            {
                BattleLog($"第 {i + 1} 段（敌方余骰单方面）：{enemy.GetDebugDisplayName()}[{enemy.GetSelectedSkillDebugSummary()}]({FormatPowerWithBonus(enemyBasePower, enemyBonusPower)}) 为防御技能，未与攻击骰拼点，本次无效");
                continue;
            }

            ApplyEnemyTriggeredEffects(enemy, ally, i, EnemySkill_SO.DiceEffectTrigger.OnSkillUse, 0);
            yield return PlaySingleAttackPresentation(
                enemy.transform,
                ally.transform,
                false,
                enemyRange,
                enemyPower,
                enemyBonusPower,
                enemyPower,
                diceFx,
                isDefenseSkill ? null : () => ApplyEnemyHit(enemy, ally, enemyPower, i, true, enemyBonusPower));

            BattleLog($"\u7b2c {i + 1} \u6bb5\uff08\u654c\u65b9\u4f59\u9ab0\u5355\u65b9\u9762\uff09\uff1a{enemy.GetDebugDisplayName()}[{enemy.GetSelectedSkillDebugSummary()}]({FormatPowerWithBonus(enemyBasePower, enemyBonusPower)}) -> {ally.GetDebugDisplayName()}\uff0c\u5269\u4f59HP {ally.CurrentHP}/{ally.MaxHP}");
        }
    }

    private IEnumerator ResolveAllySingleAttackAction(BattleAllyController ally, BattleEnemyController enemy)
    {
        if (ally == null || enemy == null || ally.IsDead || enemy.IsDead)
            yield break;

        bool isDefenseSkill = IsDefenseSkill(ally.SelectedSkill);
        if (isDefenseSkill)
        {
            int refundedEnergy = RestoreSharedEnergy(Mathf.Max(0, ally.SelectedSkill != null ? ally.SelectedSkill.energyCost : 0));
            if (refundedEnergy > 0)
            {
                battleFeedbackSystem?.ShowCustomPopup(
                    BattleActorVisualUtility.GetActorCenterWorld(ally.transform) + new Vector3(0f, 1.1f, 0f),
                    $"+{refundedEnergy}",
                    sharedEnergyBarFillColor);
            }

            BattleLog($"[友方单方面攻击] {ally.GetDebugDisplayName()}[{ally.GetSelectedSkillDebugSummary()}] 为防御技能，未与攻击骰拼点，本次无效。");
            yield break;
        }

        ally.MarkCurrentRoundSkillUsed();
        BattleLog($"[\u53cb\u65b9\u5355\u65b9\u9762\u653b\u51fb] {ally.GetDebugDisplayName()}[{ally.GetSelectedSkillDebugSummary()}] -> {enemy.GetDebugDisplayName()}");

        for (int i = 0; i < ally.DiceCount; i++)
        {
            if (ally.IsDead || enemy.IsDead)
                yield break;

            PlayerRelicDiceModifier allyRelicModifier = ResolvePlayerRelicDiceModifier(ally, enemy, i);
            EnemySkill_SO.DicePowerRange allyRange = ApplyPlayerRelicDiceRangeModifier(ally.GetDicePowerRange(i), allyRelicModifier);
            int allyBasePower = RollDiceFromRange(allyRange);
            int allyBonusPower =
                allyRelicModifier.powerBonus +
                EvaluatePowerModifier(ally.SelectedSkill, i, EnemySkill_SO.DiceEffectTrigger.OnSkillUse, ally.Speed, enemy.Speed, ally.GetStatusStacks, ally.GetStatusStacks, enemy.GetStatusStacks, enemy.GetStatusStacks) +
                EvaluatePowerModifier(ally.SelectedSkill, i, EnemySkill_SO.DiceEffectTrigger.OnSingleAttack, ally.Speed, enemy.Speed, ally.GetStatusStacks, ally.GetStatusStacks, enemy.GetStatusStacks, enemy.GetStatusStacks);
            int allyPower = ClampFinalPower(allyBasePower, allyBonusPower);
            EnemySkill_SO.DicePresentationSettings diceFx = ally.GetDicePresentationSettings(i);
            ApplyAllyTriggeredEffects(ally, enemy, i, EnemySkill_SO.DiceEffectTrigger.OnSkillUse, 0);
            yield return PlaySingleAttackPresentation(
                ally.transform,
                enemy.transform,
                true,
                allyRange,
                allyPower,
                allyBonusPower,
                allyPower,
                diceFx,
                () => ApplyAllyHit(ally, enemy, allyPower, i, true, allyBonusPower));

            BattleLog($"\u7b2c {i + 1} \u6bb5\uff1a{ally.GetDebugDisplayName()}[{ally.GetSelectedSkillDebugSummary()}]({FormatPowerWithBonus(allyBasePower, allyBonusPower)}) -> {enemy.GetDebugDisplayName()}\uff0c\u5269\u4f59HP {enemy.CurrentHP}/{enemy.MaxHP}");
        }
    }

    private IEnumerator ResolveEnemySingleAttackAction(BattleEnemyController enemy, BattleAllyController targetAlly, SpeedButton enemyButton)
    {
        if (enemy == null || targetAlly == null || enemy.IsDead || targetAlly.IsDead)
            yield break;

        enemy.ActivateSelectedSkillForButton(enemyButton);
        TryApplyEnemySkillStageTransition(enemy, enemy.SelectedSkill);

        bool isDefenseSkill = IsDefenseSkill(enemy.SelectedSkill);
        if (isDefenseSkill)
        {
            BattleLog($"[敌方单方面攻击] {enemy.GetDebugDisplayName()}[{enemy.GetSelectedSkillDebugSummary()}] 为防御技能，未与攻击骰拼点，本次无效。");
            yield break;
        }

        BattleLog($"[\u654c\u65b9\u5355\u65b9\u9762\u653b\u51fb] {enemy.GetDebugDisplayName()}[{enemy.GetSelectedSkillDebugSummary()}] -> {targetAlly.GetDebugDisplayName()}");

        for (int i = 0; i < enemy.DiceCount; i++)
        {
            if (enemy.IsDead || targetAlly.IsDead)
                yield break;

            EnemySkill_SO.DicePowerRange enemyRange = enemy.GetDicePowerRange(i);
            int enemyBasePower = enemy.RollDice(i);
            int enemyBonusPower =
                EvaluatePowerModifier(enemy.SelectedSkill, i, EnemySkill_SO.DiceEffectTrigger.OnSkillUse, enemy.Speed, targetAlly.Speed, enemy.GetStatusStacks, enemy.GetStatusStacks, targetAlly.GetStatusStacks, targetAlly.GetStatusStacks) +
                EvaluatePowerModifier(enemy.SelectedSkill, i, EnemySkill_SO.DiceEffectTrigger.OnSingleAttack, enemy.Speed, targetAlly.Speed, enemy.GetStatusStacks, enemy.GetStatusStacks, targetAlly.GetStatusStacks, targetAlly.GetStatusStacks);
            int enemyPower = ClampFinalPower(enemyBasePower, enemyBonusPower);
            EnemySkill_SO.DicePresentationSettings diceFx = enemy.GetDicePresentationSettings(i);
            ApplyEnemyTriggeredEffects(enemy, targetAlly, i, EnemySkill_SO.DiceEffectTrigger.OnSkillUse, 0);
            yield return PlaySingleAttackPresentation(
                enemy.transform,
                targetAlly.transform,
                false,
                enemyRange,
                enemyPower,
                enemyBonusPower,
                enemyPower,
                diceFx,
                () => ApplyEnemyHit(enemy, targetAlly, enemyPower, i, true, enemyBonusPower));

            BattleLog($"\u7b2c {i + 1} \u6bb5\uff1a{enemy.GetDebugDisplayName()}[{enemy.GetSelectedSkillDebugSummary()}]({FormatPowerWithBonus(enemyBasePower, enemyBonusPower)}) -> {targetAlly.GetDebugDisplayName()}\uff0c\u5269\u4f59HP {targetAlly.CurrentHP}/{targetAlly.MaxHP}");
        }
    }

    private void ApplyAllyHit(BattleAllyController attacker, BattleEnemyController target, int baseDamage, int diceIndex, bool isSingleAttack = false, int powerModifierAmount = 0)
    {
        if (attacker == null || target == null || attacker.IsDead)
            return;

        bool targetWasAlive = !target.IsDead;
        int dealtDamage = target.TakeDamage(baseDamage, powerModifierAmount, baseDamage);
        if (dealtDamage <= 0)
        {
            battleFeedbackSystem?.ShowDamageResultPopup(
                BattleActorVisualUtility.GetActorCenterWorld(target.transform) + new Vector3(0f, 1.4f, 0f),
                0,
                true,
                target.LastDamageModifierRatioPercent);
        }
        if (dealtDamage > 0)
        {
            if (isSingleAttack)
                ApplyAllyImmediateTriggeredEffects(attacker, target, diceIndex, EnemySkill_SO.DiceEffectTrigger.OnSingleAttack, dealtDamage);
            ApplyAllyImmediateTriggeredEffects(attacker, target, diceIndex, EnemySkill_SO.DiceEffectTrigger.OnHit, dealtDamage);
            ApplyReactiveOnHitStatusEffects(target);
            if (isSingleAttack)
                ApplyAllyStatusEffectsOnly(attacker, target, diceIndex, EnemySkill_SO.DiceEffectTrigger.OnSingleAttack);
            ApplyAllyStatusEffectsOnly(attacker, target, diceIndex, EnemySkill_SO.DiceEffectTrigger.OnHit);
            if (targetWasAlive && target.IsDead)
                ApplyEnemyDeathTriggeredStatusGains(target);
            return;
        }

        ApplyAllyTriggeredEffects(attacker, target, diceIndex, EnemySkill_SO.DiceEffectTrigger.OnMiss, dealtDamage);
    }

    private void ApplyEnemyHit(BattleEnemyController attacker, BattleAllyController target, int baseDamage, int diceIndex, bool isSingleAttack = false, int powerModifierAmount = 0)
    {
        if (attacker == null || target == null || attacker.IsDead)
            return;

        int incomingDamage = ApplyEnemyCalamityDamageModifiers(baseDamage);
        if (isSingleAttack)
            incomingDamage = ApplyPlayerRelicIncomingSingleAttackDamageModifiers(attacker, target, diceIndex, incomingDamage);

        int dealtDamage = target.TakeDamage(incomingDamage, powerModifierAmount, baseDamage);
        if (dealtDamage <= 0)
        {
            battleFeedbackSystem?.ShowDamageResultPopup(
                BattleActorVisualUtility.GetActorCenterWorld(target.transform) + new Vector3(0f, 1.4f, 0f),
                0,
                false,
                target.LastDamageModifierRatioPercent);
        }
        if (dealtDamage > 0)
        {
            if (isSingleAttack)
                ApplyEnemyImmediateTriggeredEffects(attacker, target, diceIndex, EnemySkill_SO.DiceEffectTrigger.OnSingleAttack, dealtDamage);
            ApplyEnemyImmediateTriggeredEffects(attacker, target, diceIndex, EnemySkill_SO.DiceEffectTrigger.OnHit, dealtDamage);
            ApplyReactiveOnHitStatusEffects(target);
            if (isSingleAttack)
                ApplyEnemyStatusEffectsOnly(attacker, target, diceIndex, EnemySkill_SO.DiceEffectTrigger.OnSingleAttack);
            ApplyEnemyStatusEffectsOnly(attacker, target, diceIndex, EnemySkill_SO.DiceEffectTrigger.OnHit);
            return;
        }

        ApplyEnemyTriggeredEffects(attacker, target, diceIndex, EnemySkill_SO.DiceEffectTrigger.OnMiss, dealtDamage);
    }

    private int ApplyPlayerRelicIncomingSingleAttackDamageModifiers(
        BattleEnemyController attacker,
        BattleAllyController target,
        int diceIndex,
        int incomingDamage)
    {
        BattleEncounterData activeEncounter = BattleEncounterRuntime.ActiveEncounter;
        if (activeEncounter == null || activeEncounter.relicInventory == null || activeEncounter.relicInventory.Count == 0)
            return Mathf.Max(0, incomingDamage);

        RelicExecutionContext context = RelicExecutionContext.CreatePlayerAboutToTakeSingleAttackDamage(
            null,
            attacker != null ? attacker.SelectedSkill : null,
            diceIndex,
            incomingDamage,
            attacker,
            target);
        RelicSystem.Process(activeEncounter.relicInventory, context);

        int adjustedDamage = Mathf.Max(0, incomingDamage + context.incomingDamageFlatModifier);
        if (adjustedDamage < incomingDamage && context.feedbackMessages != null)
        {
            for (int i = 0; i < context.feedbackMessages.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(context.feedbackMessages[i]))
                    BattleLog(context.feedbackMessages[i]);
            }
        }

        return adjustedDamage;
    }

    private void ApplyReactiveOnHitStatusEffects(BattleEnemyController target)
    {
        if (target == null)
            return;

        List<BattleStatusTickResult> results = target.ConsumeOnHitStatusEffects();
        ApplyReactiveOnHitStatusEffects(target.transform, target.GetDebugDisplayName(), target.CurrentHP, target.MaxHP, results);
    }

    private void ApplyEnemyDeathTriggeredStatusGains(BattleEnemyController defeatedEnemy)
    {
        if (defeatedEnemy == null || activeRoundEnemies == null)
            return;

        for (int i = 0; i < activeRoundEnemies.Length; i++)
        {
            BattleEnemyController enemy = activeRoundEnemies[i];
            if (enemy == null || enemy.IsDead || enemy == defeatedEnemy || enemy.enemyData == null)
                continue;

            List<EnemyData_SO.OnSpecificEnemyDeathStatusGainEntry> rules = enemy.enemyData.onSpecificEnemyDeathStatusGains;
            if (rules == null || rules.Count == 0)
                continue;

            for (int ruleIndex = 0; ruleIndex < rules.Count; ruleIndex++)
            {
                EnemyData_SO.OnSpecificEnemyDeathStatusGainEntry rule = rules[ruleIndex];
                if (rule.watchedEnemyData == null || rule.watchedEnemyData != defeatedEnemy.enemyData)
                    continue;
                if (rule.statusData == null || rule.stackAmount <= 0)
                    continue;

                if (enemy.AddStatus(rule.statusData, rule.statusData.StatusId, rule.statusData.DisplayName, rule.stackAmount))
                {
                    int stacks = enemy.GetStatusStacks(rule.statusData);
                    int additionalApplications = Mathf.Max(0, rule.additionalRoundStartApplications);
                    if (additionalApplications > 0)
                    {
                        enemy.QueueStatusForRoundStart(
                            rule.statusData,
                            rule.statusData.StatusId,
                            rule.statusData.DisplayName,
                            rule.stackAmount,
                            1,
                            additionalApplications);
                    }

                    BattleLog(
                        additionalApplications > 0
                            ? $"敌方被动触发：{defeatedEnemy.GetDebugDisplayName()} 死亡后，{enemy.GetDebugDisplayName()} 立即获得 {rule.stackAmount} 层 {rule.statusData.DisplayName}，并将在后续 {additionalApplications} 个回合开始时继续获得相同效果，当前 {rule.statusData.DisplayName} {stacks} 层"
                            : $"敌方被动触发：{defeatedEnemy.GetDebugDisplayName()} 死亡后，{enemy.GetDebugDisplayName()} 获得 {rule.stackAmount} 层 {rule.statusData.DisplayName}，当前 {rule.statusData.DisplayName} {stacks} 层");
                }
            }
        }
    }

    private void ApplyReactiveOnHitStatusEffects(BattleAllyController target)
    {
        if (target == null)
            return;

        List<BattleStatusTickResult> results = target.ConsumeOnHitStatusEffects();
        ApplyReactiveOnHitStatusEffects(target.transform, target.GetDebugDisplayName(), target.CurrentHP, target.MaxHP, results);
    }

    private void ApplyReactiveOnHitStatusEffects(Transform targetTransform, string targetName, int currentHP, int maxHP, List<BattleStatusTickResult> results)
    {
        if (results == null || results.Count == 0)
            return;

        ResolvePresentationSystem();
        for (int i = 0; i < results.Count; i++)
        {
            BattleStatusTickResult result = results[i];
            if (result.damage <= 0)
                continue;

            if (result.statusData != null)
                battlePresentationSystem?.PlayStatusTickEffect(targetTransform, result.statusData);

            BattleLog($"状态触发：{targetName} 受到 {result.damage} 点 {result.displayName} 伤害，当前 {result.displayName} {result.stacks} 层，剩余HP {currentHP}/{maxHP}");
        }
    }

    private void ApplyAllyDamageEffects(BattleAllyController attacker, BattleEnemyController currentTarget, int diceIndex, EnemySkill_SO.DiceEffectTrigger trigger)
    {
        EnemySkill_SO skill = attacker.SelectedSkill;
        if (skill == null)
            return;

        EnemySkill_SO.DiceEffectSettings settings = skill.GetDiceEffectSettings(diceIndex);
        if (settings.damageEffects == null || settings.damageEffects.Length == 0)
            return;

        List<BattleEnemyController> opponents = GetLivingEnemies();
        for (int i = 0; i < settings.damageEffects.Length; i++)
        {
            EnemySkill_SO.DamageEffectEntry effect = settings.damageEffects[i];
            if (effect.trigger != trigger || effect.damageAmount <= 0)
                continue;

            if (effect.targetMode == EnemySkill_SO.DamageTargetMode.Self)
            {
                attacker.TakeDamage(effect.damageAmount);
                BattleLog($"\u8bcd\u6761\u89e6\u53d1\uff1a{attacker.GetDebugDisplayName()}[{BuildEffectLabel(effect, i)}] \u5bf9 \u81ea\u5df1 \u9020\u6210 {effect.damageAmount} \u989d\u5916\u4f24\u5bb3\uff0c\u5269\u4f59HP {attacker.CurrentHP}/{attacker.MaxHP}");
                continue;
            }

            List<BattleEnemyController> targets = SelectEnemyTargets(effect.targetMode, currentTarget, opponents, effect.randomTargetCount);
            for (int j = 0; j < targets.Count; j++)
            {
                BattleEnemyController target = targets[j];
                if (target == null || target.IsDead)
                    continue;

                target.TakeDamage(effect.damageAmount);
                BattleLog($"\u8bcd\u6761\u89e6\u53d1\uff1a{attacker.GetDebugDisplayName()}[{BuildEffectLabel(effect, i)}] \u5bf9 {target.GetDebugDisplayName()} \u9020\u6210 {effect.damageAmount} \u989d\u5916\u4f24\u5bb3\uff0c\u5269\u4f59HP {target.CurrentHP}/{target.MaxHP}");
            }
        }
    }

    private void ApplyEnemyDamageEffects(BattleEnemyController attacker, BattleAllyController currentTarget, int diceIndex, EnemySkill_SO.DiceEffectTrigger trigger)
    {
        EnemySkill_SO skill = attacker.SelectedSkill;
        if (skill == null)
            return;

        EnemySkill_SO.DiceEffectSettings settings = skill.GetDiceEffectSettings(diceIndex);
        if (settings.damageEffects == null || settings.damageEffects.Length == 0)
            return;

        List<BattleAllyController> opponents = GetLivingAllies();
        for (int i = 0; i < settings.damageEffects.Length; i++)
        {
            EnemySkill_SO.DamageEffectEntry effect = settings.damageEffects[i];
            if (effect.trigger != trigger || effect.damageAmount <= 0)
                continue;

            if (effect.targetMode == EnemySkill_SO.DamageTargetMode.Self)
            {
                attacker.TakeDamage(effect.damageAmount);
                BattleLog($"\u8bcd\u6761\u89e6\u53d1\uff1a{attacker.GetDebugDisplayName()}[{BuildEffectLabel(effect, i)}] \u5bf9 \u81ea\u5df1 \u9020\u6210 {effect.damageAmount} \u989d\u5916\u4f24\u5bb3\uff0c\u5269\u4f59HP {attacker.CurrentHP}/{attacker.MaxHP}");
                continue;
            }

            List<BattleAllyController> targets = SelectAllyTargets(effect.targetMode, currentTarget, opponents, effect.randomTargetCount);
            for (int j = 0; j < targets.Count; j++)
            {
                BattleAllyController target = targets[j];
                if (target == null || target.IsDead)
                    continue;

                int modifiedDamage = ApplyEnemyCalamityDamageModifiers(effect.damageAmount);
                target.TakeDamage(modifiedDamage, 0, effect.damageAmount);
                BattleLog($"\u8bcd\u6761\u89e6\u53d1\uff1a{attacker.GetDebugDisplayName()}[{BuildEffectLabel(effect, i)}] \u5bf9 {target.GetDebugDisplayName()} \u9020\u6210 {modifiedDamage} \u989d\u5916\u4f24\u5bb3\uff0c\u5269\u4f59HP {target.CurrentHP}/{target.MaxHP}");
            }
        }
    }

    private void ApplyAllyTriggeredEffects(BattleAllyController attacker, BattleEnemyController currentTarget, int diceIndex, EnemySkill_SO.DiceEffectTrigger trigger, int dealtDamage)
    {
        ApplyAllyImmediateTriggeredEffects(attacker, currentTarget, diceIndex, trigger, dealtDamage);
        ApplyAllyStatusEffectsOnly(attacker, currentTarget, diceIndex, trigger);
    }

    private void ApplyEnemyTriggeredEffects(BattleEnemyController attacker, BattleAllyController currentTarget, int diceIndex, EnemySkill_SO.DiceEffectTrigger trigger, int dealtDamage)
    {
        ApplyEnemyImmediateTriggeredEffects(attacker, currentTarget, diceIndex, trigger, dealtDamage);
        ApplyEnemyStatusEffectsOnly(attacker, currentTarget, diceIndex, trigger);
    }

    private void ApplyAllyImmediateTriggeredEffects(BattleAllyController attacker, BattleEnemyController currentTarget, int diceIndex, EnemySkill_SO.DiceEffectTrigger trigger, int dealtDamage)
    {
        ApplyAllyDamageEffects(attacker, currentTarget, diceIndex, trigger);
        ApplyAllyStatusBurstEffects(attacker, currentTarget, diceIndex, trigger);
        ApplyAllyHealingEffects(attacker, currentTarget, diceIndex, trigger, dealtDamage);
        ApplyAllyStatusStackOperationEffects(attacker, currentTarget, diceIndex, trigger);
    }

    private void ApplyEnemyImmediateTriggeredEffects(BattleEnemyController attacker, BattleAllyController currentTarget, int diceIndex, EnemySkill_SO.DiceEffectTrigger trigger, int dealtDamage)
    {
        ApplyEnemyDamageEffects(attacker, currentTarget, diceIndex, trigger);
        ApplyEnemyStatusBurstEffects(attacker, currentTarget, diceIndex, trigger);
        ApplyEnemyHealingEffects(attacker, currentTarget, diceIndex, trigger, dealtDamage);
        ApplyEnemyStatusStackOperationEffects(attacker, currentTarget, diceIndex, trigger);
        ApplyEnemyStageTransitionEffects(attacker, diceIndex, trigger);
    }

    private void ApplyAllyStatusEffectsOnly(BattleAllyController attacker, BattleEnemyController currentTarget, int diceIndex, EnemySkill_SO.DiceEffectTrigger trigger)
    {
        EnemySkill_SO skill = attacker != null ? attacker.SelectedSkill : null;
        if (skill == null)
            return;

        EnemySkill_SO.DiceEffectSettings settings = skill.GetDiceEffectSettings(diceIndex);
        if (settings.statusEffects == null || settings.statusEffects.Length == 0)
            return;

        List<BattleEnemyController> opponents = GetLivingEnemies();
        for (int i = 0; i < settings.statusEffects.Length; i++)
        {
            EnemySkill_SO.StatusEffectEntry effect = settings.statusEffects[i];
            if (effect.trigger != trigger || effect.stackAmount <= 0)
                continue;

            if (effect.targetMode == EnemySkill_SO.DamageTargetMode.Self)
            {
                if (!TryResolveStatus(effect, out BattleStatus_SO statusData, out string statusId, out string statusName))
                    continue;
                int adjustedStacks = ResolvePlayerRelicAdjustedStatusStacks(statusData, effect.stackAmount, attacker, attacker);
                if (adjustedStacks <= 0)
                    continue;
                if (effect.applyTiming == EnemySkill_SO.StatusApplyTiming.NextRoundStart)
                {
                    int delayRounds = Mathf.Max(1, effect.applyAfterRoundStarts);
                    int repeatCount = Mathf.Max(1, effect.applyRepeatCount);
                    if (attacker.QueueStatusForRoundStart(effect.statusData, statusId, statusName, adjustedStacks, delayRounds, repeatCount))
                        BattleLog(BuildDelayedStatusLog(attacker.GetDebugDisplayName(), "\u81ea\u5df1", effect, i, statusName, delayRounds, repeatCount, adjustedStacks));
                }
                else
                {
                    if (attacker.AddStatus(effect.statusData, statusId, statusName, adjustedStacks))
                    {
                        int stacks = effect.statusData != null ? attacker.GetStatusStacks(effect.statusData) : attacker.BurnStacks;
                        BattleLog($"\u8bcd\u6761\u89e6\u53d1\uff1a{attacker.GetDebugDisplayName()}[{BuildStatusEffectLabel(effect, i)}] \u4f7f \u81ea\u5df1 \u83b7\u5f97 {adjustedStacks} \u5c42 {statusName}\uff0c\u5f53\u524d {statusName} {stacks} \u5c42");
                    }
                }
                continue;
            }

            List<BattleEnemyController> targets = SelectEnemyTargets(effect.targetMode, currentTarget, opponents, effect.randomTargetCount);
            for (int j = 0; j < targets.Count; j++)
            {
                BattleEnemyController target = targets[j];
                if (target == null || target.IsDead)
                    continue;

                ApplyResolvedStatusEffect(attacker, target, effect, i);
            }
        }
    }

    private void ApplyEnemyStatusEffectsOnly(BattleEnemyController attacker, BattleAllyController currentTarget, int diceIndex, EnemySkill_SO.DiceEffectTrigger trigger)
    {
        EnemySkill_SO skill = attacker != null ? attacker.SelectedSkill : null;
        if (skill == null)
            return;

        EnemySkill_SO.DiceEffectSettings settings = skill.GetDiceEffectSettings(diceIndex);
        if (settings.statusEffects == null || settings.statusEffects.Length == 0)
            return;

        List<BattleAllyController> opponents = GetLivingAllies();
        for (int i = 0; i < settings.statusEffects.Length; i++)
        {
            EnemySkill_SO.StatusEffectEntry effect = settings.statusEffects[i];
            if (effect.trigger != trigger || effect.stackAmount <= 0)
                continue;

            if (effect.targetMode == EnemySkill_SO.DamageTargetMode.Self)
            {
                if (!TryResolveStatus(effect, out _, out string statusId, out string statusName))
                    continue;
                if (effect.applyTiming == EnemySkill_SO.StatusApplyTiming.NextRoundStart)
                {
                    int delayRounds = Mathf.Max(1, effect.applyAfterRoundStarts);
                    int repeatCount = Mathf.Max(1, effect.applyRepeatCount);
                    if (attacker.QueueStatusForRoundStart(effect.statusData, statusId, statusName, effect.stackAmount, delayRounds, repeatCount))
                        BattleLog(BuildDelayedStatusLog(attacker.GetDebugDisplayName(), "\u81ea\u5df1", effect, i, statusName, delayRounds, repeatCount));
                }
                else
                {
                    if (attacker.AddStatus(effect.statusData, statusId, statusName, effect.stackAmount))
                    {
                        int stacks = effect.statusData != null ? attacker.GetStatusStacks(effect.statusData) : attacker.BurnStacks;
                        BattleLog($"\u8bcd\u6761\u89e6\u53d1\uff1a{attacker.GetDebugDisplayName()}[{BuildStatusEffectLabel(effect, i)}] \u4f7f \u81ea\u5df1 \u83b7\u5f97 {effect.stackAmount} \u5c42 {statusName}\uff0c\u5f53\u524d {statusName} {stacks} \u5c42");
                    }
                }
                continue;
            }

            List<BattleAllyController> targets = SelectAllyTargets(effect.targetMode, currentTarget, opponents, effect.randomTargetCount);
            for (int j = 0; j < targets.Count; j++)
            {
                BattleAllyController target = targets[j];
                if (target == null || target.IsDead)
                    continue;

                ApplyResolvedStatusEffect(attacker, target, effect, i);
            }
        }
    }

    private void ApplyResolvedStatusEffect(BattleAllyController attacker, BattleEnemyController target, EnemySkill_SO.StatusEffectEntry effect, int effectIndex)
    {
        if (!TryResolveStatus(effect, out BattleStatus_SO statusData, out string statusId, out string statusName))
            return;
        int adjustedStacks = ResolvePlayerRelicAdjustedStatusStacks(statusData, effect.stackAmount, attacker, target);
        if (adjustedStacks <= 0)
            return;
        if (effect.applyTiming == EnemySkill_SO.StatusApplyTiming.NextRoundStart)
        {
            int delayRounds = Mathf.Max(1, effect.applyAfterRoundStarts);
            int repeatCount = Mathf.Max(1, effect.applyRepeatCount);
            if (target.QueueStatusForRoundStart(effect.statusData, statusId, statusName, adjustedStacks, delayRounds, repeatCount))
                BattleLog(BuildDelayedStatusLog(attacker.GetDebugDisplayName(), target.GetDebugDisplayName(), effect, effectIndex, statusName, delayRounds, repeatCount, adjustedStacks));
            else
                BattleLog(BuildImmuneStatusBlockedLog(attacker.GetDebugDisplayName(), target.GetDebugDisplayName(), effect, effectIndex, statusName));
            return;
        }

        if (target.AddStatus(effect.statusData, statusId, statusName, adjustedStacks))
        {
            int stacks = effect.statusData != null ? target.GetStatusStacks(effect.statusData) : target.BurnStacks;
            BattleLog($"\u8bcd\u6761\u89e6\u53d1\uff1a{attacker.GetDebugDisplayName()}[{BuildStatusEffectLabel(effect, effectIndex)}] \u4f7f {target.GetDebugDisplayName()} \u83b7\u5f97 {adjustedStacks} \u5c42 {statusName}\uff0c\u5f53\u524d {statusName} {stacks} \u5c42");
        }
        else
            BattleLog(BuildImmuneStatusBlockedLog(attacker.GetDebugDisplayName(), target.GetDebugDisplayName(), effect, effectIndex, statusName));
    }

    private void TryApplyEnemySkillStageTransition(BattleEnemyController enemy, EnemySkill_SO skill)
    {
        if (enemy == null || skill == null || skill.transformEnemyToStageIndex < 0)
            return;

        if (enemy.TryTransitionToStage(skill.transformEnemyToStageIndex))
            BattleLog($"阶段切换：{enemy.GetDebugDisplayName()} 通过技能 {skill.skillName} 切换到第 {enemy.CurrentStageIndex + 1} 阶段");
    }

    private void ApplyResolvedStatusEffect(BattleEnemyController attacker, BattleAllyController target, EnemySkill_SO.StatusEffectEntry effect, int effectIndex)
    {
        if (!TryResolveStatus(effect, out _, out string statusId, out string statusName))
            return;
        if (effect.applyTiming == EnemySkill_SO.StatusApplyTiming.NextRoundStart)
        {
            int delayRounds = Mathf.Max(1, effect.applyAfterRoundStarts);
            int repeatCount = Mathf.Max(1, effect.applyRepeatCount);
            if (target.QueueStatusForRoundStart(effect.statusData, statusId, statusName, effect.stackAmount, delayRounds, repeatCount))
                BattleLog(BuildDelayedStatusLog(attacker.GetDebugDisplayName(), target.GetDebugDisplayName(), effect, effectIndex, statusName, delayRounds, repeatCount));
            return;
        }

        if (target.AddStatus(effect.statusData, statusId, statusName, effect.stackAmount))
        {
            int stacks = effect.statusData != null ? target.GetStatusStacks(effect.statusData) : target.BurnStacks;
            BattleLog($"\u8bcd\u6761\u89e6\u53d1\uff1a{attacker.GetDebugDisplayName()}[{BuildStatusEffectLabel(effect, effectIndex)}] \u4f7f {target.GetDebugDisplayName()} \u83b7\u5f97 {effect.stackAmount} \u5c42 {statusName}\uff0c\u5f53\u524d {statusName} {stacks} \u5c42");
        }
    }

    private static bool TryResolveStatus(EnemySkill_SO.StatusEffectEntry effect, out BattleStatus_SO statusData, out string statusId, out string statusName)
    {
        if (effect.statusData != null)
        {
            statusData = effect.statusData;
            statusId = effect.statusData.StatusId;
            statusName = effect.statusData.DisplayName;
            return true;
        }

        if (effect.statusType == EnemySkill_SO.DiceStatusType.Burn)
        {
            statusData = null;
            statusId = "burn";
            statusName = "\u71c3\u70e7";
            return true;
        }

        statusData = null;
        statusId = string.Empty;
        statusName = string.Empty;
        return false;
    }

    private static bool TryResolveStatus(EnemySkill_SO.StatusBurstDamageEffectEntry effect, out BattleStatus_SO statusData, out string statusId, out string statusName)
    {
        if (effect.statusData != null)
        {
            statusData = effect.statusData;
            statusId = effect.statusData.StatusId;
            statusName = effect.statusData.DisplayName;
            return true;
        }

        if (effect.statusType == EnemySkill_SO.DiceStatusType.Burn)
        {
            statusData = null;
            statusId = "burn";
            statusName = "\u71c3\u70e7";
            return true;
        }

        statusData = null;
        statusId = string.Empty;
        statusName = string.Empty;
        return false;
    }

    private static bool TryResolveStatus(EnemySkill_SO.StatusStackOperationEffectEntry effect, out BattleStatus_SO statusData, out string statusId, out string statusName)
    {
        if (effect.statusData != null)
        {
            statusData = effect.statusData;
            statusId = effect.statusData.StatusId;
            statusName = effect.statusData.DisplayName;
            return true;
        }

        if (effect.statusType == EnemySkill_SO.DiceStatusType.Burn)
        {
            statusData = null;
            statusId = "burn";
            statusName = "\u71c3\u70e7";
            return true;
        }

        statusData = null;
        statusId = string.Empty;
        statusName = string.Empty;
        return false;
    }

    private List<BattleEnemyController> GetLivingEnemies()
    {
        List<BattleEnemyController> result = new List<BattleEnemyController>();
        if (activeRoundEnemies == null)
            return result;

        for (int i = 0; i < activeRoundEnemies.Length; i++)
        {
            BattleEnemyController enemy = activeRoundEnemies[i];
            if (enemy != null && !enemy.IsDead)
                result.Add(enemy);
        }

        return result;
    }

    private List<BattleAllyController> GetLivingAllies()
    {
        List<BattleAllyController> result = new List<BattleAllyController>();
        if (activeRoundAllies == null)
            return result;

        for (int i = 0; i < activeRoundAllies.Length; i++)
        {
            BattleAllyController ally = activeRoundAllies[i];
            if (ally != null && !ally.IsDead)
                result.Add(ally);
        }

        return result;
    }

    private List<BattleEnemyController> SelectEnemyTargets(
        EnemySkill_SO.DamageTargetMode targetMode,
        BattleEnemyController currentTarget,
        List<BattleEnemyController> candidates,
        int randomTargetCount)
    {
        switch (targetMode)
        {
            case EnemySkill_SO.DamageTargetMode.CurrentTarget:
                return currentTarget != null && !currentTarget.IsDead
                    ? new List<BattleEnemyController> { currentTarget }
                    : new List<BattleEnemyController>();
            case EnemySkill_SO.DamageTargetMode.AllOpponents:
                return new List<BattleEnemyController>(candidates);
            case EnemySkill_SO.DamageTargetMode.RandomOpponents:
                return SelectRandomTargets(candidates, randomTargetCount);
            default:
                return new List<BattleEnemyController>();
        }
    }

    private List<BattleAllyController> SelectAllyTargets(
        EnemySkill_SO.DamageTargetMode targetMode,
        BattleAllyController currentTarget,
        List<BattleAllyController> candidates,
        int randomTargetCount)
    {
        switch (targetMode)
        {
            case EnemySkill_SO.DamageTargetMode.CurrentTarget:
                return currentTarget != null && !currentTarget.IsDead
                    ? new List<BattleAllyController> { currentTarget }
                    : new List<BattleAllyController>();
            case EnemySkill_SO.DamageTargetMode.AllOpponents:
                return new List<BattleAllyController>(candidates);
            case EnemySkill_SO.DamageTargetMode.RandomOpponents:
                return SelectRandomTargets(candidates, randomTargetCount);
            default:
                return new List<BattleAllyController>();
        }
    }

    private List<T> SelectRandomTargets<T>(List<T> candidates, int count)
    {
        List<T> pool = new List<T>(candidates);
        List<T> result = new List<T>();
        if (pool.Count == 0)
            return result;

        int targetCount = Mathf.Clamp(count, 1, pool.Count);

        for (int i = 0; i < targetCount; i++)
        {
            if (pool.Count == 0)
                break;

            int randomIndex = UnityEngine.Random.Range(0, pool.Count);
            result.Add(pool[randomIndex]);
            pool.RemoveAt(randomIndex);
        }

        return result;
    }

    private static string BuildEffectLabel(EnemySkill_SO.DamageEffectEntry effect, int fallbackIndex)
    {
        return string.IsNullOrWhiteSpace(effect.effectName) ? $"\u989d\u5916\u4f24\u5bb3#{fallbackIndex + 1}" : effect.effectName;
    }

    private static string BuildStatusEffectLabel(EnemySkill_SO.StatusEffectEntry effect, int fallbackIndex)
    {
        return string.IsNullOrWhiteSpace(effect.effectName) ? $"\u72b6\u6001\u6548\u679c#{fallbackIndex + 1}" : effect.effectName;
    }

    private static string BuildStatusBurstEffectLabel(EnemySkill_SO.StatusBurstDamageEffectEntry effect, int fallbackIndex)
    {
        return string.IsNullOrWhiteSpace(effect.effectName) ? $"\u72b6\u6001\u7206\u53d1#{fallbackIndex + 1}" : effect.effectName;
    }

    private static string BuildHealingEffectLabel(EnemySkill_SO.HealingEffectEntry effect, int fallbackIndex)
    {
        return string.IsNullOrWhiteSpace(effect.effectName) ? $"\u6062\u590d\u6548\u679c#{fallbackIndex + 1}" : effect.effectName;
    }

    private static string BuildStatusStackOperationEffectLabel(EnemySkill_SO.StatusStackOperationEffectEntry effect, int fallbackIndex)
    {
        return string.IsNullOrWhiteSpace(effect.effectName) ? $"\u72b6\u6001\u5c42\u6570\u64cd\u4f5c#{fallbackIndex + 1}" : effect.effectName;
    }

    private static string BuildEnemyStageTransitionEffectLabel(EnemySkill_SO.EnemyStageTransitionEffectEntry effect, int fallbackIndex)
    {
        return string.IsNullOrWhiteSpace(effect.effectName) ? $"\u9636\u6bb5\u8f6c\u6362#{fallbackIndex + 1}" : effect.effectName;
    }

    private static string BuildDelayedStatusLog(string attackerName, string targetName, EnemySkill_SO.StatusEffectEntry effect, int fallbackIndex, string statusName, int delayRounds, int repeatCount, int appliedStacks = -1)
    {
        string schedule = repeatCount > 1
            ? $"\u63a5\u4e0b\u6765 {repeatCount} \u4e2a\u56de\u5408\u5f00\u59cb\u65f6"
            : $"\u4e0b {delayRounds} \u4e2a\u56de\u5408\u5f00\u59cb\u65f6";
        int resolvedStacks = appliedStacks >= 0 ? appliedStacks : effect.stackAmount;
        return $"\u8bcd\u6761\u89e6\u53d1\uff1a{attackerName}[{BuildStatusEffectLabel(effect, fallbackIndex)}] \u5c06\u5728 {schedule} \u4f7f {targetName} \u83b7\u5f97 {resolvedStacks} \u5c42 {statusName}";
    }

    private static string BuildImmuneStatusBlockedLog(string attackerName, string targetName, EnemySkill_SO.StatusEffectEntry effect, int fallbackIndex, string statusName)
    {
        return $"\u8bcd\u6761\u89e6\u53d1\uff1a{attackerName}[{BuildStatusEffectLabel(effect, fallbackIndex)}] \u8bd5\u56fe\u4f7f {targetName} \u83b7\u5f97 {statusName}\uff0c\u4f46\u76ee\u6807\u514d\u75ab\u8be5\u72b6\u6001\u6548\u679c";
    }

    private static int ResolvePlayerRelicAdjustedStatusStacks(BattleStatus_SO statusData, int baseStacks, UnityEngine.Object source, UnityEngine.Object target)
    {
        if (statusData == null || baseStacks <= 0)
            return baseStacks;

        BattleEncounterData activeEncounter = BattleEncounterRuntime.ActiveEncounter;
        if (activeEncounter == null || activeEncounter.relicInventory == null || activeEncounter.relicInventory.Count == 0)
            return baseStacks;

        RelicExecutionContext context = RelicExecutionContext.CreateStatusApplied(
            null,
            null,
            statusData,
            baseStacks,
            true,
            source,
            target);
        RelicSystem.Process(activeEncounter.relicInventory, context);
        return Mathf.Max(0, baseStacks + context.additionalStatusStacks);
    }

    private static PlayerRelicDiceModifier ResolvePlayerRelicDiceModifier(BattleAllyController attacker, BattleEnemyController target, int diceIndex)
    {
        PlayerRelicDiceModifier modifier = default;
        if (attacker == null)
            return modifier;

        BattleEncounterData activeEncounter = BattleEncounterRuntime.ActiveEncounter;
        if (activeEncounter == null || activeEncounter.relicInventory == null || activeEncounter.relicInventory.Count == 0)
            return modifier;

        RelicExecutionContext context = RelicExecutionContext.CreatePlayerDicePrepared(
            null,
            attacker.SelectedSkill,
            diceIndex,
            attacker,
            target);
        RelicSystem.Process(activeEncounter.relicInventory, context);
        modifier.powerBonus = context.additionalPowerBonus;
        modifier.minPowerBonus = context.additionalMinPowerBonus;
        modifier.maxPowerBonus = context.additionalMaxPowerBonus;
        return modifier;
    }

    private static EnemySkill_SO.DicePowerRange ApplyPlayerRelicDiceRangeModifier(EnemySkill_SO.DicePowerRange baseRange, PlayerRelicDiceModifier modifier)
    {
        int minPower = Mathf.Max(1, baseRange.minPower + modifier.minPowerBonus);
        int maxPower = Mathf.Max(minPower, baseRange.maxPower + modifier.maxPowerBonus);
        return new EnemySkill_SO.DicePowerRange
        {
            minPower = minPower,
            maxPower = maxPower
        };
    }

    private static int RollDiceFromRange(EnemySkill_SO.DicePowerRange range)
    {
        int minPower = Mathf.Max(1, range.minPower);
        int maxPower = Mathf.Max(minPower, range.maxPower);
        return UnityEngine.Random.Range(minPower, maxPower + 1);
    }

    private void ApplyPlayerRoundStartRelics(BattleAllyController ally)
    {
        if (ally == null)
            return;

        BattleEncounterData activeEncounter = BattleEncounterRuntime.ActiveEncounter;
        if (activeEncounter == null || activeEncounter.relicInventory == null || activeEncounter.relicInventory.Count == 0)
            return;

        RelicExecutionContext context = RelicExecutionContext.CreatePlayerRoundStart(
            null,
            ally,
            ally.Speed);
        RelicSystem.Process(activeEncounter.relicInventory, context);

        if (context.feedbackMessages == null || context.feedbackMessages.Count == 0)
            return;

        for (int i = 0; i < context.feedbackMessages.Count; i++)
        {
            if (!string.IsNullOrWhiteSpace(context.feedbackMessages[i]))
                BattleLog(context.feedbackMessages[i]);
        }
    }

    private void ApplyRoundStartStatusEffectsGeneric(BattleAllyController[] allies, BattleEnemyController[] enemies)
    {
        if (allies != null)
        {
            for (int i = 0; i < allies.Length; i++)
            {
                BattleAllyController ally = allies[i];
                if (ally == null || ally.IsDead)
                    continue;

                List<BattleStatusTickResult> ticks = ally.ConsumeRoundStartStatusEffects();
                for (int tickIndex = 0; tickIndex < ticks.Count; tickIndex++)
                {
                    BattleStatusTickResult tick = ticks[tickIndex];
                    if (tick.damage <= 0 && tick.speedBonus == 0 && tick.appliedStacks <= 0)
                        continue;

                    if (tick.statusData != null)
                    {
                        ResolvePresentationSystem();
                        if (tick.damage > 0)
                            battlePresentationSystem?.PlayStatusTickEffect(ally.transform, tick.statusData);
                    }

                    if (tick.damage > 0)
                        BattleLog($"\u72b6\u6001\u89e6\u53d1\uff1a{ally.GetDebugDisplayName()} \u53d7\u5230 {tick.damage} \u70b9 {tick.displayName} \u4f24\u5bb3\uff0c\u5f53\u524d {tick.displayName} {tick.stacks} \u5c42\uff0c\u5269\u4f59HP {ally.CurrentHP}/{ally.MaxHP}");
                    if (tick.speedBonus > 0)
                        BattleLog($"\u72b6\u6001\u89e6\u53d1\uff1a{ally.GetDebugDisplayName()} \u83b7\u5f97 {tick.speedBonus} \u70b9 {tick.displayName} \u901f\u5ea6\u63d0\u5347\uff0c\u5f53\u524d {tick.displayName} {tick.stacks} \u5c42");
                    else if (tick.speedBonus < 0)
                        BattleLog($"\u72b6\u6001\u89e6\u53d1\uff1a{ally.GetDebugDisplayName()} \u53d7\u5230 {Mathf.Abs(tick.speedBonus)} \u70b9 {tick.displayName} \u901f\u5ea6\u964d\u4f4e\uff0c\u5f53\u524d {tick.displayName} {tick.stacks} \u5c42");
                    if (tick.appliedStacks > 0)
                        BattleLog($"状态触发：{ally.GetDebugDisplayName()} 获得 {tick.appliedStacks} 层 {tick.appliedStatusName}");
                }
            }
        }

        if (enemies != null)
        {
            for (int i = 0; i < enemies.Length; i++)
            {
                BattleEnemyController enemy = enemies[i];
                if (enemy == null || enemy.IsDead)
                    continue;

                List<BattleStatusTickResult> ticks = enemy.ConsumeRoundStartStatusEffects();
                for (int tickIndex = 0; tickIndex < ticks.Count; tickIndex++)
                {
                    BattleStatusTickResult tick = ticks[tickIndex];
                    if (tick.damage <= 0 && tick.speedBonus == 0 && tick.appliedStacks <= 0)
                        continue;

                    if (tick.statusData != null)
                    {
                        ResolvePresentationSystem();
                        if (tick.damage > 0)
                            battlePresentationSystem?.PlayStatusTickEffect(enemy.transform, tick.statusData);
                    }

                    if (tick.damage > 0)
                        BattleLog($"\u72b6\u6001\u89e6\u53d1\uff1a{enemy.GetDebugDisplayName()} \u53d7\u5230 {tick.damage} \u70b9 {tick.displayName} \u4f24\u5bb3\uff0c\u5f53\u524d {tick.displayName} {tick.stacks} \u5c42\uff0c\u5269\u4f59HP {enemy.CurrentHP}/{enemy.MaxHP}");
                    if (tick.speedBonus > 0)
                        BattleLog($"\u72b6\u6001\u89e6\u53d1\uff1a{enemy.GetDebugDisplayName()} \u83b7\u5f97 {tick.speedBonus} \u70b9 {tick.displayName} \u901f\u5ea6\u63d0\u5347\uff0c\u5f53\u524d {tick.displayName} {tick.stacks} \u5c42");
                    else if (tick.speedBonus < 0)
                        BattleLog($"\u72b6\u6001\u89e6\u53d1\uff1a{enemy.GetDebugDisplayName()} \u53d7\u5230 {Mathf.Abs(tick.speedBonus)} \u70b9 {tick.displayName} \u901f\u5ea6\u964d\u4f4e\uff0c\u5f53\u524d {tick.displayName} {tick.stacks} \u5c42");
                    if (tick.appliedStacks > 0)
                        BattleLog($"状态触发：{enemy.GetDebugDisplayName()} 获得 {tick.appliedStacks} 层 {tick.appliedStatusName}");
                }
            }

            for (int i = 0; i < enemies.Length; i++)
            {
                BattleEnemyController enemy = enemies[i];
                if (enemy == null || enemy.IsDead)
                    continue;
                if (enemy.AppliedPendingStageTransitionThisRoundStart)
                    continue;

                int previousStage = enemy.CurrentStageIndex;
                if (enemy.TryApplyStatusDrivenStageTransition(allies, enemies))
                    BattleLog($"阶段切换：{enemy.GetDebugDisplayName()} 因状态条件由第 {previousStage + 1} 阶段切换到第 {enemy.CurrentStageIndex + 1} 阶段");
            }
        }
    }

    private void ApplyAllyHealingEffects(BattleAllyController attacker, BattleEnemyController currentTarget, int diceIndex, EnemySkill_SO.DiceEffectTrigger trigger, int dealtDamage)
    {
        EnemySkill_SO skill = attacker != null ? attacker.SelectedSkill : null;
        if (skill == null)
            return;

        EnemySkill_SO.DiceEffectSettings settings = skill.GetDiceEffectSettings(diceIndex);
        if (settings.healingEffects == null || settings.healingEffects.Length == 0)
            return;

        List<BattleEnemyController> opponents = GetLivingEnemies();
        for (int i = 0; i < settings.healingEffects.Length; i++)
        {
            EnemySkill_SO.HealingEffectEntry effect = settings.healingEffects[i];
            if (effect.trigger != trigger)
                continue;
            if (!IsHealingConditionMet(effect, attacker.Speed, currentTarget != null ? currentTarget.Speed : 0, currentTarget))
                continue;

            if (effect.targetMode == EnemySkill_SO.DamageTargetMode.Self)
            {
                int healAmount = ResolveAllyHealingAmount(attacker, currentTarget, opponents, effect, dealtDamage);
                int actualHeal = attacker.Heal(healAmount);
                if (actualHeal > 0)
                    BattleLog($"词条触发：{attacker.GetDebugDisplayName()}[{BuildHealingEffectLabel(effect, i)}] 使 自己 恢复 {actualHeal} 点生命，当前HP {attacker.CurrentHP}/{attacker.MaxHP}");
                continue;
            }

            List<BattleEnemyController> targets = SelectEnemyTargets(effect.targetMode, currentTarget, opponents, effect.randomTargetCount);
            for (int j = 0; j < targets.Count; j++)
            {
                BattleEnemyController target = targets[j];
                if (target == null || target.IsDead)
                    continue;

                int healAmount = ResolveAllyHealingAmount(attacker, currentTarget, opponents, effect, dealtDamage);
                int actualHeal = target.Heal(healAmount);
                if (actualHeal > 0)
                    BattleLog($"词条触发：{attacker.GetDebugDisplayName()}[{BuildHealingEffectLabel(effect, i)}] 使 {target.GetDebugDisplayName()} 恢复 {actualHeal} 点生命，当前HP {target.CurrentHP}/{target.MaxHP}");
            }
        }
    }

    private void ApplyEnemyHealingEffects(BattleEnemyController attacker, BattleAllyController currentTarget, int diceIndex, EnemySkill_SO.DiceEffectTrigger trigger, int dealtDamage)
    {
        EnemySkill_SO skill = attacker != null ? attacker.SelectedSkill : null;
        if (skill == null)
            return;

        EnemySkill_SO.DiceEffectSettings settings = skill.GetDiceEffectSettings(diceIndex);
        if (settings.healingEffects == null || settings.healingEffects.Length == 0)
            return;

        List<BattleAllyController> opponents = GetLivingAllies();
        for (int i = 0; i < settings.healingEffects.Length; i++)
        {
            EnemySkill_SO.HealingEffectEntry effect = settings.healingEffects[i];
            if (effect.trigger != trigger)
                continue;
            if (!IsHealingConditionMet(effect, attacker.Speed, currentTarget != null ? currentTarget.Speed : 0, currentTarget))
                continue;

            if (effect.targetMode == EnemySkill_SO.DamageTargetMode.Self)
            {
                int healAmount = ResolveEnemyHealingAmount(attacker, currentTarget, opponents, effect, dealtDamage);
                int actualHeal = attacker.Heal(healAmount);
                if (actualHeal > 0)
                    BattleLog($"词条触发：{attacker.GetDebugDisplayName()}[{BuildHealingEffectLabel(effect, i)}] 使 自己 恢复 {actualHeal} 点生命，当前HP {attacker.CurrentHP}/{attacker.MaxHP}");
                continue;
            }

            List<BattleAllyController> targets = SelectAllyTargets(effect.targetMode, currentTarget, opponents, effect.randomTargetCount);
            for (int j = 0; j < targets.Count; j++)
            {
                BattleAllyController target = targets[j];
                if (target == null || target.IsDead)
                    continue;

                int healAmount = ResolveEnemyHealingAmount(attacker, currentTarget, opponents, effect, dealtDamage);
                int actualHeal = target.Heal(healAmount);
                if (actualHeal > 0)
                    BattleLog($"词条触发：{attacker.GetDebugDisplayName()}[{BuildHealingEffectLabel(effect, i)}] 使 {target.GetDebugDisplayName()} 恢复 {actualHeal} 点生命，当前HP {target.CurrentHP}/{target.MaxHP}");
            }
        }
    }

    private void ApplyAllyStatusStackOperationEffects(BattleAllyController attacker, BattleEnemyController currentTarget, int diceIndex, EnemySkill_SO.DiceEffectTrigger trigger)
    {
        EnemySkill_SO skill = attacker != null ? attacker.SelectedSkill : null;
        if (skill == null)
            return;

        EnemySkill_SO.DiceEffectSettings settings = skill.GetDiceEffectSettings(diceIndex);
        if (settings.statusStackOperationEffects == null || settings.statusStackOperationEffects.Length == 0)
            return;

        List<BattleEnemyController> opponents = GetLivingEnemies();
        for (int i = 0; i < settings.statusStackOperationEffects.Length; i++)
        {
            EnemySkill_SO.StatusStackOperationEffectEntry effect = settings.statusStackOperationEffects[i];
            if (effect.trigger != trigger)
                continue;

            if (effect.targetMode == EnemySkill_SO.DamageTargetMode.Self)
            {
                ApplyStatusStackOperation(attacker, attacker, effect, i);
                continue;
            }

            List<BattleEnemyController> targets = SelectEnemyTargets(effect.targetMode, currentTarget, opponents, effect.randomTargetCount);
            for (int j = 0; j < targets.Count; j++)
            {
                BattleEnemyController target = targets[j];
                if (target == null || target.IsDead)
                    continue;

                ApplyStatusStackOperation(attacker, target, effect, i);
            }
        }
    }

    private void ApplyEnemyStatusStackOperationEffects(BattleEnemyController attacker, BattleAllyController currentTarget, int diceIndex, EnemySkill_SO.DiceEffectTrigger trigger)
    {
        EnemySkill_SO skill = attacker != null ? attacker.SelectedSkill : null;
        if (skill == null)
            return;

        EnemySkill_SO.DiceEffectSettings settings = skill.GetDiceEffectSettings(diceIndex);
        if (settings.statusStackOperationEffects == null || settings.statusStackOperationEffects.Length == 0)
            return;

        List<BattleAllyController> opponents = GetLivingAllies();
        for (int i = 0; i < settings.statusStackOperationEffects.Length; i++)
        {
            EnemySkill_SO.StatusStackOperationEffectEntry effect = settings.statusStackOperationEffects[i];
            if (effect.trigger != trigger)
                continue;

            if (effect.targetMode == EnemySkill_SO.DamageTargetMode.Self)
            {
                ApplyStatusStackOperation(attacker, attacker, effect, i);
                continue;
            }

            List<BattleAllyController> targets = SelectAllyTargets(effect.targetMode, currentTarget, opponents, effect.randomTargetCount);
            for (int j = 0; j < targets.Count; j++)
            {
                BattleAllyController target = targets[j];
                if (target == null || target.IsDead)
                    continue;

                ApplyStatusStackOperation(attacker, target, effect, i);
            }
        }
    }

    private void ApplyEnemyStageTransitionEffects(BattleEnemyController attacker, int diceIndex, EnemySkill_SO.DiceEffectTrigger trigger)
    {
        EnemySkill_SO skill = attacker != null ? attacker.SelectedSkill : null;
        if (skill == null)
            return;

        EnemySkill_SO.DiceEffectSettings settings = skill.GetDiceEffectSettings(diceIndex);
        if (settings.enemyStageTransitionEffects == null || settings.enemyStageTransitionEffects.Length == 0)
            return;

        for (int i = 0; i < settings.enemyStageTransitionEffects.Length; i++)
        {
            EnemySkill_SO.EnemyStageTransitionEffectEntry effect = settings.enemyStageTransitionEffects[i];
            if (effect.trigger != trigger)
                continue;

            if (effect.targetMode == EnemySkill_SO.EnemyStageTransitionTargetMode.Self
                && attacker.TryTransitionToStage(
                    effect.targetStageIndex,
                    effect.applyTiming == EnemySkill_SO.EnemyStageTransitionApplyTiming.NextRoundStart))
            {
                if (effect.applyTiming == EnemySkill_SO.EnemyStageTransitionApplyTiming.NextRoundStart)
                {
                    BattleLog($"词条触发：{attacker.GetDebugDisplayName()}[{BuildEnemyStageTransitionEffectLabel(effect, i)}] 将于下个回合开始时切换到第 {effect.targetStageIndex + 1} 阶段");
                }
                else
                {
                    BattleLog($"词条触发：{attacker.GetDebugDisplayName()}[{BuildEnemyStageTransitionEffectLabel(effect, i)}] 切换到第 {attacker.CurrentStageIndex + 1} 阶段");
                }
            }
        }
    }

    private static int ResolveAllyHealingAmount(BattleAllyController attacker, BattleEnemyController currentTarget, List<BattleEnemyController> opponents, EnemySkill_SO.HealingEffectEntry effect, int dealtDamage)
    {
        switch (effect.amountMode)
        {
            case EnemySkill_SO.HealAmountMode.DamageDealt:
                return Mathf.Max(0, dealtDamage);
            case EnemySkill_SO.HealAmountMode.StatusStacks:
                return Mathf.Max(0, ResolveEnemyStatusStackSource(currentTarget, opponents, attacker, effect) * Mathf.Max(1, effect.healAmount));
            default:
                return Mathf.Max(0, effect.healAmount);
        }
    }

    private static int ResolveEnemyHealingAmount(BattleEnemyController attacker, BattleAllyController currentTarget, List<BattleAllyController> opponents, EnemySkill_SO.HealingEffectEntry effect, int dealtDamage)
    {
        switch (effect.amountMode)
        {
            case EnemySkill_SO.HealAmountMode.DamageDealt:
                return Mathf.Max(0, dealtDamage);
            case EnemySkill_SO.HealAmountMode.StatusStacks:
                return Mathf.Max(0, ResolveAllyStatusStackSource(currentTarget, opponents, attacker, effect) * Mathf.Max(1, effect.healAmount));
            default:
                return Mathf.Max(0, effect.healAmount);
        }
    }

    private static int ResolveEnemyStatusStackSource(BattleEnemyController currentTarget, List<BattleEnemyController> opponents, BattleAllyController self, EnemySkill_SO.HealingEffectEntry effect)
    {
        if (effect.statusStackSourceMode == EnemySkill_SO.DamageTargetMode.Self)
            return ResolveStatusStacks(self, effect);

        List<BattleEnemyController> targets = SelectEnemyTargetsStatic(effect.statusStackSourceMode, currentTarget, opponents, effect.randomTargetCount);
        if (targets.Count <= 0)
            return 0;

        return ResolveStatusStacks(targets[0], effect);
    }

    private static int ResolveAllyStatusStackSource(BattleAllyController currentTarget, List<BattleAllyController> opponents, BattleEnemyController self, EnemySkill_SO.HealingEffectEntry effect)
    {
        if (effect.statusStackSourceMode == EnemySkill_SO.DamageTargetMode.Self)
            return ResolveStatusStacks(self, effect);

        List<BattleAllyController> targets = SelectAllyTargetsStatic(effect.statusStackSourceMode, currentTarget, opponents, effect.randomTargetCount);
        if (targets.Count <= 0)
            return 0;

        return ResolveStatusStacks(targets[0], effect);
    }

    private static int ResolveStatusStacks(BattleEnemyController target, EnemySkill_SO.HealingEffectEntry effect)
    {
        if (target == null)
            return 0;

        if (effect.statusData != null)
            return target.GetStatusStacks(effect.statusData);

        return effect.statusType == EnemySkill_SO.DiceStatusType.Burn ? target.GetStatusStacks("burn") : 0;
    }

    private static int ResolveStatusStacks(BattleAllyController target, EnemySkill_SO.HealingEffectEntry effect)
    {
        if (target == null)
            return 0;

        if (effect.statusData != null)
            return target.GetStatusStacks(effect.statusData);

        return effect.statusType == EnemySkill_SO.DiceStatusType.Burn ? target.GetStatusStacks("burn") : 0;
    }

    private static bool IsHealingConditionMet(EnemySkill_SO.HealingEffectEntry effect, int selfSpeed, int targetSpeed, BattleEnemyController currentTarget)
    {
        if (!effect.useCondition)
            return true;

        switch (effect.condition)
        {
            case EnemySkill_SO.PowerModifierCondition.TargetHasStatus:
                return ResolveConditionStatusStacks(currentTarget, effect) > 0;
            case EnemySkill_SO.PowerModifierCondition.TargetStatusStacksAtLeast:
                return ResolveConditionStatusStacks(currentTarget, effect) >= Mathf.Max(1, effect.requiredConditionStatusStacks);
            case EnemySkill_SO.PowerModifierCondition.TargetStatusStacksPerThreshold:
                return ResolveConditionStatusStacks(currentTarget, effect) >= Mathf.Max(1, effect.requiredConditionStatusStacks);
            default:
                return IsPowerModifierConditionMet(effect.condition, selfSpeed, targetSpeed, effect.requiredConditionSpeedValue, effect.requiredConditionSpeedDifference);
        }
    }

    private static bool IsHealingConditionMet(EnemySkill_SO.HealingEffectEntry effect, int selfSpeed, int targetSpeed, BattleAllyController currentTarget)
    {
        if (!effect.useCondition)
            return true;

        switch (effect.condition)
        {
            case EnemySkill_SO.PowerModifierCondition.TargetHasStatus:
                return ResolveConditionStatusStacks(currentTarget, effect) > 0;
            case EnemySkill_SO.PowerModifierCondition.TargetStatusStacksAtLeast:
                return ResolveConditionStatusStacks(currentTarget, effect) >= Mathf.Max(1, effect.requiredConditionStatusStacks);
            case EnemySkill_SO.PowerModifierCondition.TargetStatusStacksPerThreshold:
                return ResolveConditionStatusStacks(currentTarget, effect) >= Mathf.Max(1, effect.requiredConditionStatusStacks);
            default:
                return IsPowerModifierConditionMet(effect.condition, selfSpeed, targetSpeed, effect.requiredConditionSpeedValue, effect.requiredConditionSpeedDifference);
        }
    }

    private static int ResolveConditionStatusStacks(BattleEnemyController target, EnemySkill_SO.HealingEffectEntry effect)
    {
        if (target == null)
            return 0;

        if (effect.conditionStatusData != null)
            return target.GetStatusStacks(effect.conditionStatusData);

        if (effect.statusData != null)
            return target.GetStatusStacks(effect.statusData);

        if (effect.conditionStatusType == EnemySkill_SO.DiceStatusType.Burn || effect.statusType == EnemySkill_SO.DiceStatusType.Burn)
            return target.GetStatusStacks("burn");

        return 0;
    }

    private static int ResolveConditionStatusStacks(BattleAllyController target, EnemySkill_SO.HealingEffectEntry effect)
    {
        if (target == null)
            return 0;

        if (effect.conditionStatusData != null)
            return target.GetStatusStacks(effect.conditionStatusData);

        if (effect.statusData != null)
            return target.GetStatusStacks(effect.statusData);

        if (effect.conditionStatusType == EnemySkill_SO.DiceStatusType.Burn || effect.statusType == EnemySkill_SO.DiceStatusType.Burn)
            return target.GetStatusStacks("burn");

        return 0;
    }

    private static List<BattleEnemyController> SelectEnemyTargetsStatic(EnemySkill_SO.DamageTargetMode targetMode, BattleEnemyController currentTarget, List<BattleEnemyController> candidates, int randomTargetCount)
    {
        switch (targetMode)
        {
            case EnemySkill_SO.DamageTargetMode.CurrentTarget:
                return currentTarget != null && !currentTarget.IsDead
                    ? new List<BattleEnemyController> { currentTarget }
                    : new List<BattleEnemyController>();
            case EnemySkill_SO.DamageTargetMode.AllOpponents:
                return new List<BattleEnemyController>(candidates);
            case EnemySkill_SO.DamageTargetMode.RandomOpponents:
                return SelectRandomTargetsStatic(candidates, randomTargetCount);
            default:
                return new List<BattleEnemyController>();
        }
    }

    private static List<BattleAllyController> SelectAllyTargetsStatic(EnemySkill_SO.DamageTargetMode targetMode, BattleAllyController currentTarget, List<BattleAllyController> candidates, int randomTargetCount)
    {
        switch (targetMode)
        {
            case EnemySkill_SO.DamageTargetMode.CurrentTarget:
                return currentTarget != null && !currentTarget.IsDead
                    ? new List<BattleAllyController> { currentTarget }
                    : new List<BattleAllyController>();
            case EnemySkill_SO.DamageTargetMode.AllOpponents:
                return new List<BattleAllyController>(candidates);
            case EnemySkill_SO.DamageTargetMode.RandomOpponents:
                return SelectRandomTargetsStatic(candidates, randomTargetCount);
            default:
                return new List<BattleAllyController>();
        }
    }

    private static List<T> SelectRandomTargetsStatic<T>(List<T> candidates, int count)
    {
        List<T> pool = new List<T>(candidates);
        List<T> result = new List<T>();
        if (pool.Count == 0)
            return result;

        int targetCount = Mathf.Clamp(count, 1, pool.Count);
        for (int i = 0; i < targetCount; i++)
        {
            if (pool.Count == 0)
                break;

            int randomIndex = UnityEngine.Random.Range(0, pool.Count);
            result.Add(pool[randomIndex]);
            pool.RemoveAt(randomIndex);
        }

        return result;
    }

    private void ApplyAllyStatusBurstEffects(BattleAllyController attacker, BattleEnemyController currentTarget, int diceIndex, EnemySkill_SO.DiceEffectTrigger trigger)
    {
        EnemySkill_SO skill = attacker != null ? attacker.SelectedSkill : null;
        if (skill == null)
            return;

        EnemySkill_SO.DiceEffectSettings settings = skill.GetDiceEffectSettings(diceIndex);
        if (settings.statusBurstEffects == null || settings.statusBurstEffects.Length == 0)
            return;

        List<BattleEnemyController> opponents = GetLivingEnemies();
        for (int i = 0; i < settings.statusBurstEffects.Length; i++)
        {
            EnemySkill_SO.StatusBurstDamageEffectEntry effect = settings.statusBurstEffects[i];
            if (effect.trigger != trigger || effect.damagePerStack <= 0)
                continue;

            if (effect.targetMode == EnemySkill_SO.DamageTargetMode.Self)
            {
                ApplyStatusBurst(attacker, attacker, effect, i);
                continue;
            }

            List<BattleEnemyController> targets = SelectEnemyTargets(effect.targetMode, currentTarget, opponents, effect.randomTargetCount);
            for (int j = 0; j < targets.Count; j++)
            {
                BattleEnemyController target = targets[j];
                if (target == null || target.IsDead)
                    continue;

                ApplyStatusBurst(attacker, target, effect, i);
            }
        }
    }

    private void ApplyEnemyStatusBurstEffects(BattleEnemyController attacker, BattleAllyController currentTarget, int diceIndex, EnemySkill_SO.DiceEffectTrigger trigger)
    {
        EnemySkill_SO skill = attacker != null ? attacker.SelectedSkill : null;
        if (skill == null)
            return;

        EnemySkill_SO.DiceEffectSettings settings = skill.GetDiceEffectSettings(diceIndex);
        if (settings.statusBurstEffects == null || settings.statusBurstEffects.Length == 0)
            return;

        List<BattleAllyController> opponents = GetLivingAllies();
        for (int i = 0; i < settings.statusBurstEffects.Length; i++)
        {
            EnemySkill_SO.StatusBurstDamageEffectEntry effect = settings.statusBurstEffects[i];
            if (effect.trigger != trigger || effect.damagePerStack <= 0)
                continue;

            if (effect.targetMode == EnemySkill_SO.DamageTargetMode.Self)
            {
                ApplyStatusBurst(attacker, attacker, effect, i);
                continue;
            }

            List<BattleAllyController> targets = SelectAllyTargets(effect.targetMode, currentTarget, opponents, effect.randomTargetCount);
            for (int j = 0; j < targets.Count; j++)
            {
                BattleAllyController target = targets[j];
                if (target == null || target.IsDead)
                    continue;

                ApplyStatusBurst(attacker, target, effect, i);
            }
        }
    }

    private void ApplyStatusBurst(BattleAllyController attacker, BattleEnemyController target, EnemySkill_SO.StatusBurstDamageEffectEntry effect, int effectIndex)
    {
        if (!TryResolveStatus(effect, out _, out string statusId, out string statusName))
            return;
        int stacks = effect.statusData != null ? target.GetStatusStacks(effect.statusData) : target.GetStatusStacks(statusId);
        if (stacks <= 0)
            return;

        int burstDamage = Mathf.Max(0, effect.damagePerStack) * stacks;
        if (burstDamage <= 0)
            return;

        target.TakeDamage(burstDamage);
        if (effect.clearStacksAfterBurst)
            target.ClearStatus(effect.statusData, statusId, statusName);

        BattleLog($"\u8bcd\u6761\u89e6\u53d1\uff1a{attacker.GetDebugDisplayName()}[{BuildStatusBurstEffectLabel(effect, effectIndex)}] \u5f15\u53d1 {target.GetDebugDisplayName()} \u7684 {statusName}\uff0c\u9020\u6210 {burstDamage} \u70b9\u989d\u5916\u4f24\u5bb3\uff08{effect.damagePerStack} x {stacks}\uff09{(effect.clearStacksAfterBurst ? $"\uff0c\u5e76\u6e05\u7a7a {statusName}" : string.Empty)}\uff0c\u5269\u4f59HP {target.CurrentHP}/{target.MaxHP}");
    }

    private void ApplyStatusBurst(BattleEnemyController attacker, BattleAllyController target, EnemySkill_SO.StatusBurstDamageEffectEntry effect, int effectIndex)
    {
        if (!TryResolveStatus(effect, out _, out string statusId, out string statusName))
            return;
        int stacks = effect.statusData != null ? target.GetStatusStacks(effect.statusData) : target.GetStatusStacks(statusId);
        if (stacks <= 0)
            return;

        int burstDamage = Mathf.Max(0, effect.damagePerStack) * stacks;
        if (burstDamage <= 0)
            return;

        int modifiedDamage = ApplyEnemyCalamityDamageModifiers(burstDamage);
        target.TakeDamage(modifiedDamage, 0, burstDamage);
        if (effect.clearStacksAfterBurst)
            target.ClearStatus(effect.statusData, statusId, statusName);

        BattleLog($"\u8bcd\u6761\u89e6\u53d1\uff1a{attacker.GetDebugDisplayName()}[{BuildStatusBurstEffectLabel(effect, effectIndex)}] \u5f15\u53d1 {target.GetDebugDisplayName()} \u7684 {statusName}\uff0c\u9020\u6210 {modifiedDamage} \u70b9\u989d\u5916\u4f24\u5bb3\uff08{effect.damagePerStack} x {stacks}\uff09{(effect.clearStacksAfterBurst ? $"\uff0c\u5e76\u6e05\u7a7a {statusName}" : string.Empty)}\uff0c\u5269\u4f59HP {target.CurrentHP}/{target.MaxHP}");
    }

    private void ApplyStatusBurst(BattleAllyController attacker, BattleAllyController target, EnemySkill_SO.StatusBurstDamageEffectEntry effect, int effectIndex)
    {
        if (!TryResolveStatus(effect, out _, out string statusId, out string statusName))
            return;
        int stacks = effect.statusData != null ? target.GetStatusStacks(effect.statusData) : target.GetStatusStacks(statusId);
        if (stacks <= 0)
            return;

        int burstDamage = Mathf.Max(0, effect.damagePerStack) * stacks;
        if (burstDamage <= 0)
            return;

        target.TakeDamage(burstDamage);
        if (effect.clearStacksAfterBurst)
            target.ClearStatus(effect.statusData, statusId, statusName);

        BattleLog($"\u8bcd\u6761\u89e6\u53d1\uff1a{attacker.GetDebugDisplayName()}[{BuildStatusBurstEffectLabel(effect, effectIndex)}] \u5f15\u53d1 {target.GetDebugDisplayName()} \u7684 {statusName}\uff0c\u9020\u6210 {burstDamage} \u70b9\u989d\u5916\u4f24\u5bb3\uff08{effect.damagePerStack} x {stacks}\uff09{(effect.clearStacksAfterBurst ? $"\uff0c\u5e76\u6e05\u7a7a {statusName}" : string.Empty)}\uff0c\u5269\u4f59HP {target.CurrentHP}/{target.MaxHP}");
    }

    private void ApplyStatusBurst(BattleEnemyController attacker, BattleEnemyController target, EnemySkill_SO.StatusBurstDamageEffectEntry effect, int effectIndex)
    {
        if (!TryResolveStatus(effect, out _, out string statusId, out string statusName))
            return;
        int stacks = effect.statusData != null ? target.GetStatusStacks(effect.statusData) : target.GetStatusStacks(statusId);
        if (stacks <= 0)
            return;

        int burstDamage = Mathf.Max(0, effect.damagePerStack) * stacks;
        if (burstDamage <= 0)
            return;

        target.TakeDamage(burstDamage);
        if (effect.clearStacksAfterBurst)
            target.ClearStatus(effect.statusData, statusId, statusName);

        BattleLog($"\u8bcd\u6761\u89e6\u53d1\uff1a{attacker.GetDebugDisplayName()}[{BuildStatusBurstEffectLabel(effect, effectIndex)}] \u5f15\u53d1 {target.GetDebugDisplayName()} \u7684 {statusName}\uff0c\u9020\u6210 {burstDamage} \u70b9\u989d\u5916\u4f24\u5bb3\uff08{effect.damagePerStack} x {stacks}\uff09{(effect.clearStacksAfterBurst ? $"\uff0c\u5e76\u6e05\u7a7a {statusName}" : string.Empty)}\uff0c\u5269\u4f59HP {target.CurrentHP}/{target.MaxHP}");
    }

    private void ApplyStatusStackOperation(BattleAllyController attacker, BattleEnemyController target, EnemySkill_SO.StatusStackOperationEffectEntry effect, int effectIndex)
    {
        if (!TryResolveStatus(effect, out _, out string statusId, out string statusName))
            return;

        int affectedStacks = effect.operationType == EnemySkill_SO.StatusStackOperationType.ClearStacks
            ? (effect.statusData != null ? target.GetStatusStacks(effect.statusData) : target.GetStatusStacks(statusId))
            : target.RemoveStatusStacks(effect.statusData, statusId, Mathf.Max(1, effect.stackAmount), statusName);

        if (effect.operationType == EnemySkill_SO.StatusStackOperationType.ClearStacks)
            target.ClearStatus(effect.statusData, statusId, statusName);

        if (affectedStacks <= 0)
            return;

        string actionLabel = effect.operationType == EnemySkill_SO.StatusStackOperationType.ClearStacks
            ? $"\u6e05\u7a7a {statusName}"
            : $"\u4f7f {statusName} \u51cf\u5c11 {affectedStacks} \u5c42";
        BattleLog($"\u8bcd\u6761\u89e6\u53d1\uff1a{attacker.GetDebugDisplayName()}[{BuildStatusStackOperationEffectLabel(effect, effectIndex)}] {actionLabel}\uff0c\u76ee\u6807 {target.GetDebugDisplayName()}");
    }

    private void ApplyStatusStackOperation(BattleEnemyController attacker, BattleAllyController target, EnemySkill_SO.StatusStackOperationEffectEntry effect, int effectIndex)
    {
        if (!TryResolveStatus(effect, out _, out string statusId, out string statusName))
            return;

        int affectedStacks = effect.operationType == EnemySkill_SO.StatusStackOperationType.ClearStacks
            ? (effect.statusData != null ? target.GetStatusStacks(effect.statusData) : target.GetStatusStacks(statusId))
            : target.RemoveStatusStacks(effect.statusData, statusId, Mathf.Max(1, effect.stackAmount), statusName);

        if (effect.operationType == EnemySkill_SO.StatusStackOperationType.ClearStacks)
            target.ClearStatus(effect.statusData, statusId, statusName);

        if (affectedStacks <= 0)
            return;

        string actionLabel = effect.operationType == EnemySkill_SO.StatusStackOperationType.ClearStacks
            ? $"\u6e05\u7a7a {statusName}"
            : $"\u4f7f {statusName} \u51cf\u5c11 {affectedStacks} \u5c42";
        BattleLog($"\u8bcd\u6761\u89e6\u53d1\uff1a{attacker.GetDebugDisplayName()}[{BuildStatusStackOperationEffectLabel(effect, effectIndex)}] {actionLabel}\uff0c\u76ee\u6807 {target.GetDebugDisplayName()}");
    }

    private void ApplyStatusStackOperation(BattleAllyController attacker, BattleAllyController target, EnemySkill_SO.StatusStackOperationEffectEntry effect, int effectIndex)
    {
        if (!TryResolveStatus(effect, out _, out string statusId, out string statusName))
            return;

        int affectedStacks = effect.operationType == EnemySkill_SO.StatusStackOperationType.ClearStacks
            ? (effect.statusData != null ? target.GetStatusStacks(effect.statusData) : target.GetStatusStacks(statusId))
            : target.RemoveStatusStacks(effect.statusData, statusId, Mathf.Max(1, effect.stackAmount), statusName);

        if (effect.operationType == EnemySkill_SO.StatusStackOperationType.ClearStacks)
            target.ClearStatus(effect.statusData, statusId, statusName);

        if (affectedStacks <= 0)
            return;

        string actionLabel = effect.operationType == EnemySkill_SO.StatusStackOperationType.ClearStacks
            ? $"\u6e05\u7a7a {statusName}"
            : $"\u4f7f {statusName} \u51cf\u5c11 {affectedStacks} \u5c42";
        BattleLog($"\u8bcd\u6761\u89e6\u53d1\uff1a{attacker.GetDebugDisplayName()}[{BuildStatusStackOperationEffectLabel(effect, effectIndex)}] {actionLabel}\uff0c\u76ee\u6807 {target.GetDebugDisplayName()}");
    }

    private void ApplyStatusStackOperation(BattleEnemyController attacker, BattleEnemyController target, EnemySkill_SO.StatusStackOperationEffectEntry effect, int effectIndex)
    {
        if (!TryResolveStatus(effect, out _, out string statusId, out string statusName))
            return;

        int affectedStacks = effect.operationType == EnemySkill_SO.StatusStackOperationType.ClearStacks
            ? (effect.statusData != null ? target.GetStatusStacks(effect.statusData) : target.GetStatusStacks(statusId))
            : target.RemoveStatusStacks(effect.statusData, statusId, Mathf.Max(1, effect.stackAmount), statusName);

        if (effect.operationType == EnemySkill_SO.StatusStackOperationType.ClearStacks)
            target.ClearStatus(effect.statusData, statusId, statusName);

        if (affectedStacks <= 0)
            return;

        string actionLabel = effect.operationType == EnemySkill_SO.StatusStackOperationType.ClearStacks
            ? $"\u6e05\u7a7a {statusName}"
            : $"\u4f7f {statusName} \u51cf\u5c11 {affectedStacks} \u5c42";
        BattleLog($"\u8bcd\u6761\u89e6\u53d1\uff1a{attacker.GetDebugDisplayName()}[{BuildStatusStackOperationEffectLabel(effect, effectIndex)}] {actionLabel}\uff0c\u76ee\u6807 {target.GetDebugDisplayName()}");
    }

    private void ApplyRoundEndStatusEffectsGeneric(BattleAllyController[] allies, BattleEnemyController[] enemies)
    {
        if (allies != null)
        {
            for (int i = 0; i < allies.Length; i++)
            {
                BattleAllyController ally = allies[i];
                if (ally == null || ally.IsDead)
                    continue;

                ally.ConsumeRoundEndStatusEffects();
            }
        }

        if (enemies != null)
        {
            for (int i = 0; i < enemies.Length; i++)
            {
                BattleEnemyController enemy = enemies[i];
                if (enemy == null || enemy.IsDead)
                    continue;

                enemy.ConsumeRoundEndStatusEffects();
            }
        }
    }

    private void ApplyRoundStartStatusEffects(BattleAllyController[] allies, BattleEnemyController[] enemies)
    {
        if (allies != null)
        {
            for (int i = 0; i < allies.Length; i++)
            {
                BattleAllyController ally = allies[i];
                if (ally == null || ally.IsDead)
                    continue;

                int burnDamage = ally.ConsumeBurnDamageForRound();
                if (burnDamage > 0)
                    BattleLog($"\u72b6\u6001\u89e6\u53d1\uff1a{ally.GetDebugDisplayName()} \u53d7\u5230 {burnDamage} \u70b9\u71c3\u70e7\u4f24\u5bb3\uff0c\u5f53\u524d\u71c3\u70e7 {ally.BurnStacks} \u5c42\uff0c\u5269\u4f59HP {ally.CurrentHP}/{ally.MaxHP}");
            }
        }

        if (enemies != null)
        {
            for (int i = 0; i < enemies.Length; i++)
            {
                BattleEnemyController enemy = enemies[i];
                if (enemy == null || enemy.IsDead)
                    continue;

                int burnDamage = enemy.ConsumeBurnDamageForRound();
                if (burnDamage > 0)
                    BattleLog($"\u72b6\u6001\u89e6\u53d1\uff1a{enemy.GetDebugDisplayName()} \u53d7\u5230 {burnDamage} \u70b9\u71c3\u70e7\u4f24\u5bb3\uff0c\u5f53\u524d\u71c3\u70e7 {enemy.BurnStacks} \u5c42\uff0c\u5269\u4f59HP {enemy.CurrentHP}/{enemy.MaxHP}");
            }
        }
    }

    private void ResolvePresentationSystem()
    {
        if (battlePresentationSystem != null)
            return;

        battlePresentationSystem = FindObjectOfType<BattlePresentationSystem>();
        if (battlePresentationSystem != null)
            return;

        GameObject host = battleManager != null ? battleManager.gameObject : gameObject;
        battlePresentationSystem = host.GetComponent<BattlePresentationSystem>();
        if (battlePresentationSystem == null)
            battlePresentationSystem = host.AddComponent<BattlePresentationSystem>();
    }

    private void ResolveFeedbackSystem()
    {
        if (battleFeedbackSystem != null)
            return;

        battleFeedbackSystem = FindObjectOfType<BattleFeedbackSystem>();
        if (battleFeedbackSystem != null)
            return;

        GameObject host = battleManager != null ? battleManager.gameObject : gameObject;
        battleFeedbackSystem = host.GetComponent<BattleFeedbackSystem>();
        if (battleFeedbackSystem == null)
            battleFeedbackSystem = host.AddComponent<BattleFeedbackSystem>();
    }

    private void ResolveSettlementOverlay()
    {
        if (battleSettlementOverlay != null)
            return;

        battleSettlementOverlay = FindObjectOfType<BattleSettlementOverlay>();
        if (battleSettlementOverlay != null)
            return;

        GameObject host = battleManager != null ? battleManager.gameObject : gameObject;
        battleSettlementOverlay = host.GetComponent<BattleSettlementOverlay>();
        if (battleSettlementOverlay == null)
            battleSettlementOverlay = host.AddComponent<BattleSettlementOverlay>();
    }

    private void ResolveSkillInfoOverlay()
    {
        if (battleSkillInfoOverlay != null)
            return;

        battleSkillInfoOverlay = FindObjectOfType<BattleSkillInfoOverlay>(true);
        if (battleSkillInfoOverlay != null)
            return;

        GameObject host = battleManager != null ? battleManager.gameObject : gameObject;
        battleSkillInfoOverlay = host.GetComponent<BattleSkillInfoOverlay>();
        if (battleSkillInfoOverlay == null)
            battleSkillInfoOverlay = host.AddComponent<BattleSkillInfoOverlay>();
    }

    private void BattleLog(string message)
    {
        Debug.Log($"\u005bBattleButton\u005d\u005b\u7b2c {currentRound} \u56de\u5408\u005d {message}");
    }

    private bool TryHandleRoundStartRetreatSettlement(BattleAllyController[] allies, BattleEnemyController[] enemies)
    {
        if (allies == null || enemies == null || CountLiving(allies) <= 0)
            return false;

        for (int i = 0; i < enemies.Length; i++)
        {
            BattleEnemyController enemy = enemies[i];
            if (enemy == null || enemy.IsDead)
                continue;

            if (!enemy.TryConsumeRoundStartRetreat(currentRound, out IReadOnlyList<LootTable_SO> settlementLootTables))
                continue;

            HandleForcedRetreatSettlement(allies, enemies, enemy, settlementLootTables);
            return true;
        }

        return false;
    }

    private void HandleForcedRetreatSettlement(
        BattleAllyController[] allies,
        BattleEnemyController[] enemies,
        BattleEnemyController retreatingEnemy,
        IReadOnlyList<LootTable_SO> settlementLootTables)
    {
        int livingAllies = CountLiving(allies);
        int expReward = CalculateVictoryExperienceReward(enemies);
        BattleEncounterData activeEncounter = BattleEncounterRuntime.ActiveEncounter;
        int moneyReward = activeEncounter != null
            ? CalamitySystem.ApplyBattleMoneyModifiers(Mathf.Max(0, activeEncounter.moneyGained), activeEncounter.activeCalamities)
            : 0;
        EncounterDropResults dropResults = RollEncounterDrops(activeEncounter, settlementLootTables);
        List<RunLootState> lootRewards = dropResults.lootRewards;

        if (activeEncounter != null)
        {
            activeEncounter.wasVictory = true;
            activeEncounter.experienceGained = expReward;
            activeEncounter.moneyGained = moneyReward;
            activeEncounter.resolvedPartyMembers = BuildResolvedPartyStates(allies, expReward);
            activeEncounter.resolvedLoot = CloneLootStates(lootRewards);
            activeEncounter.resolvedRelics = CloneRelicStates(dropResults.relicRewards);
            activeEncounter.resolutionOverride = new BattleEncounterResolutionOverride
            {
                forceVictory = true,
                subtitleOverride = $"{retreatingEnemy.GetDebugDisplayName()} 脱离了战斗，剩余可行动角色：{livingAllies}",
                lootTables = CloneLootTables(settlementLootTables)
            };
        }

        if (battleManager != null)
            battleManager.EndBattle();

        SpeedButton.CombatLocked = true;
        currentPhase = BattlePhase.Ready;

        if (battleSettlementOverlay != null)
        {
            battleSettlementOverlay.ShowResult(new BattleSettlementData
            {
                isVictory = true,
                title = "Victory",
                subtitle = $"{retreatingEnemy.GetDebugDisplayName()} 脱离了战斗，剩余可行动角色：{livingAllies}",
                experienceGained = expReward,
                returnSceneName = activeEncounter != null ? activeEncounter.sourceSceneName : "MapScene",
                retrySceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
                partyEntries = BuildSettlementEntries(allies, expReward),
                lootEntries = BuildSettlementLootEntries(lootRewards, dropResults.relicRewards)
            });
        }

        BattleLog($"{retreatingEnemy.GetDebugDisplayName()} 在回合开始时脱离战斗，结算已改用特殊掉落表。");
    }

    private bool TryHandleBattleCompletion(BattleAllyController[] allies, BattleEnemyController[] enemies)
    {
        BattleEncounterData activeEncounter = BattleEncounterRuntime.ActiveEncounter;
        if (activeEncounter != null && activeEncounter.resolutionOverride != null && activeEncounter.resolutionOverride.forceVictory)
            return false;

        int livingAllies = CountLiving(allies);
        int livingEnemies = CountLiving(enemies);
        if (livingAllies > 0 && livingEnemies > 0)
            return false;

        bool victory = livingAllies > 0 && livingEnemies == 0;
        int expReward = victory ? CalculateVictoryExperienceReward(enemies) : 0;
        int moneyReward = victory && activeEncounter != null
            ? CalamitySystem.ApplyBattleMoneyModifiers(Mathf.Max(0, activeEncounter.moneyGained), activeEncounter.activeCalamities)
            : 0;
        EncounterDropResults dropResults = victory
            ? RollEncounterDrops(activeEncounter, activeEncounter != null ? activeEncounter.resolutionOverride?.lootTables : null)
            : new EncounterDropResults();
        List<RunLootState> lootRewards = dropResults.lootRewards;

        if (activeEncounter != null)
        {
            activeEncounter.wasVictory = victory;
            activeEncounter.experienceGained = expReward;
            activeEncounter.moneyGained = moneyReward;
            activeEncounter.resolvedPartyMembers = BuildResolvedPartyStates(allies, victory ? expReward : 0);
            activeEncounter.resolvedLoot = CloneLootStates(lootRewards);
            activeEncounter.resolvedRelics = CloneRelicStates(dropResults.relicRewards);
        }

        if (battleManager != null)
            battleManager.EndBattle();

        SpeedButton.CombatLocked = true;
        currentPhase = BattlePhase.Ready;

        if (battleSettlementOverlay != null)
        {
            battleSettlementOverlay.ShowResult(new BattleSettlementData
            {
                isVictory = victory,
                title = victory ? "Victory" : "Defeat",
                subtitle = victory
                    ? (!string.IsNullOrWhiteSpace(activeEncounter?.resolutionOverride?.subtitleOverride)
                        ? activeEncounter.resolutionOverride.subtitleOverride
                        : $"\u6218\u6597\u80dc\u5229\uff0c\u5269\u4f59\u53ef\u884c\u52a8\u89d2\u8272\uff1a{livingAllies}")
                    : "\u961f\u4f0d\u5df2\u5728\u6218\u6597\u4e2d\u5168\u706d\u3002",
                experienceGained = expReward,
                returnSceneName = activeEncounter != null ? activeEncounter.sourceSceneName : "MapScene",
                retrySceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
                partyEntries = BuildSettlementEntries(allies, victory ? expReward : 0),
                lootEntries = BuildSettlementLootEntries(lootRewards, dropResults.relicRewards)
            });
        }

        BattleLog(victory
            ? $"Battle finished. Victory, EXP +{expReward}, Money +{moneyReward}."
            : "Battle finished. Defeat.");
        return true;
    }

    private List<RunPartyMemberState> BuildResolvedPartyStates(BattleAllyController[] allies, int experienceReward)
    {
        List<RunPartyMemberState> resolved = new List<RunPartyMemberState>();
        if (allies == null)
            return resolved;

        for (int i = 0; i < allies.Length; i++)
        {
            BattleAllyController ally = allies[i];
            if (ally == null)
                continue;

            AllyProgressionResult progression = EvaluateAllyProgression(ally, Mathf.Max(0, experienceReward));

            resolved.Add(new RunPartyMemberState
            {
                memberId = !string.IsNullOrWhiteSpace(progression.DisplayName)
                    ? progression.DisplayName
                    : ally.gameObject.name,
                characterData = progression.CharacterData,
                weaponData = progression.WeaponData,
                level = progression.FinalLevel,
                experience = progression.FinalExperience,
                maxHP = progression.FinalMaxHP,
                currentHP = progression.FinalCurrentHP
            });
        }

        return resolved;
    }

    private List<BattleSettlementEntry> BuildSettlementEntries(BattleAllyController[] allies, int experienceReward)
    {
        List<BattleSettlementEntry> entries = new List<BattleSettlementEntry>();
        if (allies == null)
            return entries;

        for (int i = 0; i < allies.Length; i++)
        {
            BattleAllyController ally = allies[i];
            if (ally == null)
                continue;

            AllyProgressionResult progression = EvaluateAllyProgression(ally, Mathf.Max(0, experienceReward));
            entries.Add(new BattleSettlementEntry
            {
                displayName = progression.DisplayName,
                currentHP = progression.InitialCurrentHP,
                maxHP = progression.InitialMaxHP,
                level = progression.InitialLevel,
                isAlive = progression.IsAlive,
                currentExperience = progression.InitialExperience,
                maxExperience = ResolveExperienceToNextLevel(ally.Level),
                experienceGained = Mathf.Max(0, experienceReward),
                resultingHP = progression.FinalCurrentHP,
                resultingMaxHP = progression.FinalMaxHP,
                resultingLevel = progression.FinalLevel,
                resultingExperience = progression.FinalExperience
            });
        }

        return entries;
    }

    private void ApplyExperienceProgression(ref int level, ref int experience, int gainedExperience)
    {
        int currentLevel = Mathf.Max(1, level);
        int currentExperience = Mathf.Max(0, experience);
        int remaining = Mathf.Max(0, gainedExperience);

        while (remaining > 0)
        {
            int required = ResolveExperienceToNextLevel(currentLevel);
            if (required <= 0)
                break;

            currentExperience = Mathf.Clamp(currentExperience, 0, required);
            int needed = Mathf.Max(0, required - currentExperience);
            if (remaining < needed)
            {
                currentExperience += remaining;
                remaining = 0;
                break;
            }

            remaining -= needed;
            currentLevel += 1;
            currentExperience = 0;
        }

        level = currentLevel;
        experience = currentExperience;
    }

    private AllyProgressionResult EvaluateAllyProgression(BattleAllyController ally, int experienceReward)
    {
        AllyController allyController = ally != null ? ally.GetComponent<AllyController>() : null;
        if (allyController == null && ally != null)
            allyController = ally.GetComponentInParent<AllyController>();

        CharacterData_SO characterData = allyController != null ? allyController.characterData : null;
        Weapon_SO weaponData = allyController != null && allyController.weapon != null
            ? allyController.weapon.weaponData
            : null;

        AllyProgressionResult result = new AllyProgressionResult
        {
            CharacterData = characterData,
            WeaponData = weaponData,
            DisplayName = ally != null ? ally.GetDebugDisplayName() : string.Empty,
            InitialLevel = ally != null ? Mathf.Max(1, ally.Level) : 1,
            InitialExperience = ally != null ? Mathf.Max(0, ally.Experience) : 0,
            InitialMaxHP = ally != null ? Mathf.Max(1, ally.MaxHP) : 1,
            InitialCurrentHP = ally != null ? Mathf.Max(0, ally.CurrentHP) : 0,
            IsAlive = ally != null && !ally.IsDead
        };

        int currentLevel = result.InitialLevel;
        int currentExperience = result.InitialExperience;
        int currentMaxHP = result.InitialMaxHP;
        int currentHP = result.InitialCurrentHP;
        int remaining = Mathf.Max(0, experienceReward);

        while (remaining > 0)
        {
            int required = ResolveExperienceToNextLevel(currentLevel);
            if (required <= 0)
                break;

            currentExperience = Mathf.Clamp(currentExperience, 0, required);
            int needed = Mathf.Max(0, required - currentExperience);
            if (remaining < needed)
            {
                currentExperience += remaining;
                remaining = 0;
                break;
            }

            remaining -= needed;
            currentLevel += 1;
            currentExperience = 0;

            int upgradedMaxHP = ResolveMaxHPForLevel(characterData, currentLevel, currentMaxHP);
            currentMaxHP = upgradedMaxHP;
            if (currentHP > 0)
            {
                int healAmount = Mathf.CeilToInt(upgradedMaxHP * 0.5f);
                currentHP = Mathf.Clamp(currentHP + healAmount, 0, upgradedMaxHP);
            }
        }

        result.FinalLevel = currentLevel;
        result.FinalExperience = currentExperience;
        result.FinalMaxHP = currentMaxHP;
        result.FinalCurrentHP = Mathf.Clamp(currentHP, 0, currentMaxHP);
        return result;
    }

    private int ResolveMaxHPForLevel(CharacterData_SO characterData, int level, int fallbackMaxHP)
    {
        LevelTable_SO levelTable = battleManager != null ? battleManager.LevelTable : null;
        if (levelTable == null)
            return Mathf.Max(1, fallbackMaxHP);

        return Mathf.Max(1, levelTable.GetMaxHP(characterData, level, fallbackMaxHP));
    }

    private int ResolveExperienceToNextLevel(int level)
    {
        LevelTable_SO levelTable = battleManager != null ? battleManager.LevelTable : null;
        if (levelTable == null)
            return 100;

        return Mathf.Max(0, levelTable.GetExperienceToNextLevel(level));
    }

    private int CalculateVictoryExperienceReward(BattleEnemyController[] enemies)
    {
        if (enemies == null || enemies.Length == 0)
            return 0;

        int baseReward = 0;
        for (int i = 0; i < enemies.Length; i++)
        {
            BattleEnemyController enemy = enemies[i];
            if (enemy == null)
                continue;

            baseReward += Mathf.Max(6, enemy.BaseHP / 2);
        }

        BattleEncounterData activeEncounter = BattleEncounterRuntime.ActiveEncounter;
        if (activeEncounter == null)
            return baseReward;

        switch (activeEncounter.tileType)
        {
            case MapTileType.Elite:
                baseReward = Mathf.RoundToInt(baseReward * 1.5f);
                break;
            case MapTileType.Boss:
                baseReward = Mathf.RoundToInt(baseReward * 2.5f);
                break;
        }

        return CalamitySystem.ApplyBattleExperienceModifiers(baseReward, activeEncounter.activeCalamities);
    }

    private int ApplyEnemyCalamityDamageModifiers(int baseDamage)
    {
        BattleEncounterData activeEncounter = BattleEncounterRuntime.ActiveEncounter;
        return CalamitySystem.ApplyEnemyDamageModifiers(baseDamage, activeEncounter != null ? activeEncounter.activeCalamities : null);
    }

    private EncounterDropResults RollEncounterDrops(BattleEncounterData encounter, IReadOnlyList<LootTable_SO> overrideLootTables = null)
    {
        EncounterDropResults results = new EncounterDropResults();
        if (encounter == null || encounter.enemyGroup == null)
            return results;

        IReadOnlyList<LootTable_SO> lootTables = overrideLootTables != null && overrideLootTables.Count > 0
            ? overrideLootTables
            : encounter.enemyGroup.GetLootTables();
        if (lootTables == null || lootTables.Count == 0)
            return results;

        System.Random rng = new System.Random(encounter.seed ^ 0x51A7);
        Dictionary<LootItem_SO, int> aggregated = new Dictionary<LootItem_SO, int>();
        Dictionary<Relic_SO, int> aggregatedRelics = new Dictionary<Relic_SO, int>();

        for (int i = 0; i < lootTables.Count; i++)
        {
            LootTable_SO lootTable = lootTables[i];
            if (lootTable == null)
                continue;

            IReadOnlyList<LootTable_SO.LootDropEntry> drops = lootTable.Drops;
            if (drops == null)
                continue;

            for (int j = 0; j < drops.Count; j++)
            {
                LootTable_SO.LootDropEntry drop = drops[j];
                if (drop == null || drop.lootItem == null || drop.dropChance <= 0f)
                    continue;

                if (rng.NextDouble() > Mathf.Clamp01(drop.dropChance))
                    continue;

                int amount = drop.minAmount;
                if (drop.maxAmount > drop.minAmount)
                    amount = rng.Next(drop.minAmount, drop.maxAmount + 1);

                if (amount <= 0)
                    continue;

                if (aggregated.ContainsKey(drop.lootItem))
                    aggregated[drop.lootItem] += amount;
                else
                    aggregated.Add(drop.lootItem, amount);
            }

            IReadOnlyList<LootTable_SO.RelicDropEntry> relicDrops = lootTable.RelicDrops;
            if (relicDrops == null)
                continue;

            for (int j = 0; j < relicDrops.Count; j++)
            {
                LootTable_SO.RelicDropEntry drop = relicDrops[j];
                if (drop == null || drop.relicData == null || drop.dropChance <= 0f)
                    continue;

                if (rng.NextDouble() > Mathf.Clamp01(drop.dropChance))
                    continue;

                int amount = drop.minAmount;
                if (drop.maxAmount > drop.minAmount)
                    amount = rng.Next(drop.minAmount, drop.maxAmount + 1);

                if (amount <= 0)
                    continue;

                if (aggregatedRelics.ContainsKey(drop.relicData))
                    aggregatedRelics[drop.relicData] += amount;
                else
                    aggregatedRelics.Add(drop.relicData, amount);
            }
        }

        foreach (KeyValuePair<LootItem_SO, int> pair in aggregated)
        {
            results.lootRewards.Add(new RunLootState
            {
                lootItem = pair.Key,
                amount = pair.Value
            });
        }

        results.lootRewards.Sort((a, b) =>
        {
            if (a == null || a.lootItem == null)
                return 1;
            if (b == null || b.lootItem == null)
                return -1;

            int rarityCompare = b.lootItem.rarity.CompareTo(a.lootItem.rarity);
            if (rarityCompare != 0)
                return rarityCompare;

            return string.Compare(a.DisplayName, b.DisplayName, StringComparison.Ordinal);
        });

        foreach (KeyValuePair<Relic_SO, int> pair in aggregatedRelics)
        {
            results.relicRewards.Add(new RunRelicState
            {
                relicData = pair.Key,
                amount = pair.Value
            });
        }

        results.relicRewards.Sort((a, b) =>
        {
            if (a == null || a.relicData == null)
                return 1;
            if (b == null || b.relicData == null)
                return -1;

            int rarityCompare = b.relicData.rarity.CompareTo(a.relicData.rarity);
            if (rarityCompare != 0)
                return rarityCompare;

            return string.Compare(a.DisplayName, b.DisplayName, StringComparison.Ordinal);
        });

        return results;
    }

    private static List<LootTable_SO> CloneLootTables(IReadOnlyList<LootTable_SO> source)
    {
        List<LootTable_SO> clone = new List<LootTable_SO>();
        if (source == null)
            return clone;

        for (int i = 0; i < source.Count; i++)
        {
            if (source[i] != null)
                clone.Add(source[i]);
        }

        return clone;
    }

    private static List<RunLootState> CloneLootStates(List<RunLootState> source)
    {
        List<RunLootState> clone = new List<RunLootState>();
        if (source == null)
            return clone;

        for (int i = 0; i < source.Count; i++)
        {
            RunLootState loot = source[i];
            if (loot != null)
                clone.Add(loot.Clone());
        }

        return clone;
    }

    private static List<RunRelicState> CloneRelicStates(List<RunRelicState> source)
    {
        List<RunRelicState> clone = new List<RunRelicState>();
        if (source == null)
            return clone;

        for (int i = 0; i < source.Count; i++)
        {
            RunRelicState relic = source[i];
            if (relic != null)
                clone.Add(relic.Clone());
        }

        return clone;
    }

    private List<BattleSettlementLootEntry> BuildSettlementLootEntries(List<RunLootState> lootRewards, List<RunRelicState> relicRewards)
    {
        List<BattleSettlementLootEntry> entries = new List<BattleSettlementLootEntry>();
        if (lootRewards != null)
        {
            for (int i = 0; i < lootRewards.Count; i++)
            {
                RunLootState loot = lootRewards[i];
                if (loot == null || loot.lootItem == null || loot.amount <= 0)
                    continue;

                entries.Add(new BattleSettlementLootEntry
                {
                    lootItem = loot.lootItem,
                    amount = loot.amount,
                    displayName = string.IsNullOrWhiteSpace(loot.lootItem.lootName) ? loot.DisplayName : loot.lootItem.lootName,
                    description = loot.lootItem.description ?? string.Empty,
                    rarity = loot.lootItem.rarity,
                    attribute = loot.lootItem.attribute,
                    icon = loot.lootItem.icon
                });
            }
        }

        if (relicRewards != null)
        {
            for (int i = 0; i < relicRewards.Count; i++)
            {
                RunRelicState relic = relicRewards[i];
                if (relic == null || relic.relicData == null || relic.amount <= 0)
                    continue;

                entries.Add(new BattleSettlementLootEntry
                {
                    relicData = relic.relicData,
                    amount = relic.amount,
                    displayName = relic.relicData.DisplayName,
                    description = relic.relicData.description ?? string.Empty,
                    rarity = relic.relicData.rarity,
                    attribute = null,
                    icon = relic.relicData.icon
                });
            }
        }

        return entries;
    }

    private static int CompareCombatAction(CombatAction a, CombatAction b)
    {
        int speedCompare = b.PrioritySpeed.CompareTo(a.PrioritySpeed);
        if (speedCompare != 0)
            return speedCompare;

        int tieCompare = a.TiePriority.CompareTo(b.TiePriority);
        if (tieCompare != 0)
            return tieCompare;

        return a.SequenceIndex.CompareTo(b.SequenceIndex);
    }

    private static int CountLiving(BattleAllyController[] allies)
    {
        int count = 0;
        foreach (BattleAllyController ally in allies)
        {
            if (ally != null && !ally.IsDead)
                count++;
        }

        return count;
    }

    private static int CountLiving(BattleEnemyController[] enemies)
    {
        int count = 0;
        foreach (BattleEnemyController enemy in enemies)
        {
            if (enemy != null && !enemy.IsDead)
                count++;
        }

        return count;
    }
}
