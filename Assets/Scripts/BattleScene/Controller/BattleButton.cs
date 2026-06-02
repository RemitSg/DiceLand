using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattleButton : MonoBehaviour
{
    private const string BattleStatusLibraryResourcePath = "BattleStatusLibrary";
    private const int PrismDawnServantEnemyId = 4001;
    private const int PrismDuskServantEnemyId = 4002;
    private const string PrismFrontStatusId = "prism_front";
    private const string PrismBackStatusId = "prism_back";

    private sealed class EncounterDropResults
    {
        public List<RunLootState> lootRewards = new List<RunLootState>();
        public List<RunRelicState> relicRewards = new List<RunRelicState>();
        public BattleSettlementRelicChoiceData relicChoice;
    }

    private struct PlayerRelicDiceModifier
    {
        public int powerBonus;
        public int minPowerBonus;
        public int maxPowerBonus;
        public int targetMinPowerBonus;
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
    public TutorialBattleGuideOverlay tutorialBattleGuideOverlay;
    public BattlePauseMenuOverlay battlePauseMenuOverlay;

    [Header("Elite Reward Choice")]
    [SerializeField] private RelicTable_SO eliteRelicChoiceFallbackTable;
    [SerializeField, Min(1)] private int eliteRelicChoiceCount = 3;
    [SerializeField] private string editorEliteRelicChoiceFallbackTableName = "OuterEliteRelicLoot";

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
    [SerializeField] private Color temporarySharedEnergyBarFillColor = new Color(0.64f, 0.29f, 1f, 1f);

    [Header("Tutorial Guide")]
    [SerializeField] private TMP_FontAsset tutorialGuideKeywordFont;
    [SerializeField] private TMP_FontAsset tutorialGuideBodyFont;

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
    private bool battleStartEnergyCalamityApplied;
    private bool allyBattleStartCalamityApplied;
    private int currentSharedEnergy;
    private int temporarySharedEnergy;
    private int currentSharedEnergyMax;
    private int reservedSharedEnergy;
    private int lastSpentSharedEnergy;
    private int lastSpentTemporarySharedEnergy;
    private bool isRefreshingSharedEnergyReservations;
    private BattleAllyController[] activeRoundAllies;
    private BattleEnemyController[] activeRoundEnemies;
    private static BattleStatusLibrary_SO cachedBattleStatusLibrary;
    private Canvas sharedEnergyCanvas;
    private RectTransform sharedEnergyRoot;
    private RectTransform sharedEnergyPanelRect;
    private TextMeshProUGUI sharedEnergyText;
    private Image sharedEnergyBarFill;
    private Image temporarySharedEnergyBarFill;
    private RectTransform roundCounterPanelRect;
    private TextMeshProUGUI roundCounterText;
    private int pendingAllyNextDicePowerBonus;
    private int pendingEnemyNextDicePowerBonus;
    private bool forceReturnToMapAfterDefeat;

    private void Awake()
    {
        // Retry/return transitions can leave the global combat lock raised because
        // SpeedButton.CombatLocked is static. Reset it whenever a battle scene boots.
        SpeedButton.CombatLocked = false;
        SpeedButton.InputSuppressed = false;
        enemyBattleStartEffectsApplied = false;
        battleStartEnergyCalamityApplied = false;
        allyBattleStartCalamityApplied = false;
        currentSharedEnergyMax = Mathf.Max(0, initialSharedEnergyMax);
        currentSharedEnergy = currentSharedEnergyMax;
        temporarySharedEnergy = 0;
        reservedSharedEnergy = 0;
        lastSpentSharedEnergy = 0;
        lastSpentTemporarySharedEnergy = 0;

        if (BattleManagerResolverService.TryResolve(
                battleManager,
                () => FindObjectOfType<BattleManager>(),
                out BattleManager resolvedBattleManager,
                out _))
            battleManager = resolvedBattleManager;

        ResolvePresentationSystem();
        ResolveFeedbackSystem();
        ResolveSettlementOverlay();
        ResolveSkillInfoOverlay();
        ResolveTutorialGuideOverlay();
        ResolvePauseMenuOverlay();
        EnsureSharedEnergyUi();
        RefreshSharedEnergyUi();
        InitializeTutorialBattleForEncounter();
    }

    private void OnDisable()
    {
        ClearActiveRoundCombatants();
    }

    public void OnBattleButtonClicked()
    {
        if (isResolvingCombat)
        {
            Debug.LogWarning($"[BattleButton][\u7b2c {currentRound} \u56de\u5408] \u6218\u6597\u6f14\u51fa\u8fdb\u884c\u4e2d\uff0c\u8bf7\u7b49\u5f85\u5f53\u524d\u7ed3\u7b97\u5b8c\u6210\u3002");
            return;
        }

        if (!BattleManagerResolverService.TryResolve(
                battleManager,
                () => FindObjectOfType<BattleManager>(),
                out BattleManager resolvedBattleManager,
                out string battleManagerWarningMessage))
        {
            if (!string.IsNullOrWhiteSpace(battleManagerWarningMessage))
                Debug.LogWarning(battleManagerWarningMessage);
            return;
        }
        battleManager = resolvedBattleManager;

        ResolvePresentationSystem();
        ResolveFeedbackSystem();
        ResolveSettlementOverlay();
        ResolveSkillInfoOverlay();
        ResolveTutorialGuideOverlay();
        ResolvePauseMenuOverlay();

        BattleButtonClickBlockResult clickBlock = BattleButtonUiOverlayStateService.ResolveClickBlock(
            battleSettlementOverlay != null && battleSettlementOverlay.IsVisible,
            tutorialBattleGuideOverlay != null && tutorialBattleGuideOverlay.IsVisible,
            battleSkillInfoOverlay != null && battleSkillInfoOverlay.IsVisible,
            battlePauseMenuOverlay != null && battlePauseMenuOverlay.IsVisible);
        if (clickBlock.ShouldBlock)
            return;

        if (!BattleCombatantContextService.TryCollectCombatants(
                battleManager.alliesParent,
                battleManager.enemiesParent,
                out BattleAllyController[] allies,
                out BattleEnemyController[] enemies,
                out string combatantWarningMessage))
        {
            if (!string.IsNullOrWhiteSpace(combatantWarningMessage))
                Debug.LogWarning(combatantWarningMessage);
            return;
        }

        BattleFeedbackBindingService.BindIfAvailable(battleFeedbackSystem, allies, enemies);

        if (currentPhase == BattlePhase.Ready)
        {
            SetupTargetingPhase(allies, enemies);
            currentPhase = BattlePhase.Targeting;
            return;
        }

        if (!TutorialBattleRuntime.TryRequestCombatExecution(out string blockedMessage))
        {
            if (!string.IsNullOrWhiteSpace(blockedMessage))
                BattleLog(blockedMessage);
            TryShowPendingTutorialStep();
            return;
        }

        StartCoroutine(ExecuteCombatPhaseRoutine(allies, enemies));
    }

    private void SetActiveRoundCombatants(BattleAllyController[] allies, BattleEnemyController[] enemies)
    {
        ClearActiveRoundCombatants();
        activeRoundAllies = allies;
        activeRoundEnemies = enemies;
        BindActiveRoundCombatantEvents();
    }

    private void ClearActiveRoundCombatants()
    {
        UnbindActiveRoundCombatantEvents();
        activeRoundAllies = null;
        activeRoundEnemies = null;
    }

    private void BindActiveRoundCombatantEvents()
    {
        BattleActiveRoundCombatantEventBindingService.BindDeathHandlers(
            activeRoundAllies,
            activeRoundEnemies,
            HandleActiveRoundAllyDied,
            HandleActiveRoundEnemyDied);
    }

    private void UnbindActiveRoundCombatantEvents()
    {
        BattleActiveRoundCombatantEventBindingService.UnbindDeathHandlers(
            activeRoundAllies,
            activeRoundEnemies,
            HandleActiveRoundAllyDied,
            HandleActiveRoundEnemyDied);
        BattleSharedEnergyListenerBindingService.Unbind(
            activeRoundAllies,
            activeRoundEnemies,
            HandleAllySelectedSkillChanged,
            HandleAllyStatusChanged,
            HandleAllyConnectionChanged,
            HandleEnemyStatusChanged);
    }

    private void HandleActiveRoundAllyDied(BattleAllyController defeatedAlly)
    {
        ApplyPlayerDeathRelics(defeatedAlly, activeRoundAllies, activeRoundEnemies);
    }

    private void HandleActiveRoundEnemyDied(BattleEnemyController defeatedEnemy)
    {
        ApplyEnemyDeathTriggeredStatusGains(defeatedEnemy);
        ApplyEnemyDeathCalamityStatusGains(defeatedEnemy);
        ApplyEnemyDeathRelics(defeatedEnemy, activeRoundAllies, activeRoundEnemies);
    }

    private void SetupTargetingPhase(BattleAllyController[] allies, BattleEnemyController[] enemies)
    {
        SetActiveRoundCombatants(allies, enemies);
        BattleEncounterRuntime.ActiveEncounter?.ResetRoundRelicProcKeys();
        BattleSharedEnergyRoundStartState energyRoundStartState = BattleSharedEnergyRoundStartService.Resolve(
            currentSharedEnergy,
            temporarySharedEnergy,
            currentSharedEnergyMax);
        currentSharedEnergy = energyRoundStartState.CurrentSharedEnergy;
        temporarySharedEnergy = energyRoundStartState.TemporarySharedEnergy;
        reservedSharedEnergy = energyRoundStartState.ReservedSharedEnergy;
        ApplyBattleStartSharedEnergyCalamityIfNeeded();
        ApplyAllyBattleStartCalamityEffectsIfNeeded(allies);
        RefreshSharedEnergyUi();

        BattleLog(BattleTargetingPhaseLogService.BuildPhaseHeader(CountLiving(allies), CountLiving(enemies)));

        ApplyRoundStartStatusEffectsGeneric(allies, enemies);
        BattleRoundStartCompletionGuardResult completionGuard = BattleRoundStartCompletionGuardService.Resolve(
            () => TryHandleRoundStartRetreatSettlement(allies, enemies),
            () => TryHandleBattleCompletion(allies, enemies));
        if (completionGuard.ShouldStopTargetingSetup)
            return;

        ApplyPendingBattleStartAllyStatusGrants(allies);
        ApplyEnemyBattleStartEffectsIfNeeded(allies, enemies);
        ApplyEnemyRepeatRoundStartStatusGains(allies, enemies);
        allies = ApplyBattleSetupRelics(allies, enemies);
        ApplyBattleRoundStartRelics(allies, enemies);

        List<string> allyRoundStartLogs = BattleTargetingRoundStartSetupService.PrepareAllies(
            allies,
            speedMin,
            speedMax,
            currentRound,
            ApplyPlayerRoundStartRelics);
        for (int i = 0; i < allyRoundStartLogs.Count; i++)
            BattleLog(allyRoundStartLogs[i]);

        RegisterSharedEnergyListeners(allies);
        RefreshSharedEnergyReservations(allies);

        List<string> enemyRoundStartLogs = BattleTargetingRoundStartSetupService.PrepareEnemies(enemies, currentRound, allies);
        for (int i = 0; i < enemyRoundStartLogs.Count; i++)
            BattleLog(enemyRoundStartLogs[i]);

        BattleTargetingSpeedButtonStateService.PrepareForTargeting(allies, enemies);

        List<SpeedButton> interactableAllyButtons = BattleTargetingSpeedButtonStateService.EnableLivingAllyButtons(allies);

        List<string> autoTargetLogs = BattleTargetingEnemyAutoTargetService.AutoConnectAndBuildLogEntries(
            enemies,
            interactableAllyButtons);
        for (int i = 0; i < autoTargetLogs.Count; i++)
            BattleLog(autoTargetLogs[i]);

        BattleLog(BattleTargetingPhaseLogService.BuildEnemyAutoTargetCompleteLog());
        TryShowPendingTutorialStep();
    }

    private void ApplyEnemyBattleStartEffectsIfNeeded(BattleAllyController[] allies, BattleEnemyController[] enemies)
    {
        if (enemyBattleStartEffectsApplied)
            return;

        enemyBattleStartEffectsApplied = true;
        if (allies == null || enemies == null)
            return;

        BattleEnemyBattleStartSkillGrantService.Dispatch(
            enemies,
            allies,
            (enemy, skill) => BattleLog($"\u654c\u65b9 {enemy.GetDebugDisplayName()} \u5728\u6218\u6597\u5f00\u59cb\u65f6\u8d4b\u4e88\u53cb\u65b9\u4e34\u65f6\u6280\u80fd\uff1a{skill.skillName}"));

        BattleEnemyTargetingStatusGainPhaseService.DispatchBattleStartStatusGains(
            enemies,
            enemy => ApplyEnemyStatusGains(
                enemy,
                enemy.enemyData.battleStartStatusGains,
                (statusName, appliedStacks, currentStacks) =>
                    $"敌方战斗开始效果：{enemy.GetDebugDisplayName()} 获得 {appliedStacks} 层 {statusName}，当前 {statusName} {currentStacks} 层"),
            enemy => TryLogEnemyStatusDrivenStageTransition(enemy, allies, enemies));

        ApplyEnemyBattleStartCalamityStatuses(enemies);
        ApplyInterlockingPrismBattleStartFaces(enemies);
    }

    private void ApplyBattleStartSharedEnergyCalamityIfNeeded()
    {
        if (battleStartEnergyCalamityApplied)
            return;

        battleStartEnergyCalamityApplied = true;
        BattleEncounterData activeEncounter = BattleEncounterRuntime.ActiveEncounter;
        int energyDelta = CalamitySystem.ResolveBattleStartSharedEnergyDelta(activeEncounter != null ? activeEncounter.activeCalamities : null);
        if (energyDelta == 0)
            return;

        currentSharedEnergy = Mathf.Clamp(currentSharedEnergy + energyDelta, 0, currentSharedEnergyMax);
        reservedSharedEnergy = Mathf.Clamp(reservedSharedEnergy, 0, currentSharedEnergy);
        BattleLog(energyDelta > 0
            ? $"灾厄效果：战斗开始时获得 {energyDelta} 点能量"
            : $"灾厄效果：战斗开始时失去 {Mathf.Abs(energyDelta)} 点能量");
    }

    private void ApplyEnemyBattleStartCalamityStatuses(BattleEnemyController[] enemies)
    {
        BattleEncounterData activeEncounter = BattleEncounterRuntime.ActiveEncounter;
        List<CalamityBuiltInEffectEntry> effects = CalamitySystem.GetEnemyBattleStartStatusEffects(activeEncounter != null ? activeEncounter.activeCalamities : null);
        if (effects.Count == 0 || enemies == null)
            return;

        for (int enemyIndex = 0; enemyIndex < enemies.Length; enemyIndex++)
        {
            BattleEnemyController enemy = enemies[enemyIndex];
            if (enemy == null || enemy.IsDead)
                continue;

            for (int effectIndex = 0; effectIndex < effects.Count; effectIndex++)
            {
                CalamityBuiltInEffectEntry effect = effects[effectIndex];
                if (effect == null || effect.statusData == null || effect.effectAmount <= 0)
                    continue;

                bool applied = enemy.AddStatus(effect.statusData, effect.statusData.StatusId, effect.statusData.DisplayName, effect.effectAmount);
                if (applied)
                    enemy.AddPendingRoundStartSpeedBonusFromStatus(effect.statusData, effect.effectAmount);
                if (applied)
                    BattleLog($"灾厄效果：{enemy.GetDebugDisplayName()} 获得 {effect.effectAmount} 层 {effect.statusData.DisplayName}");
            }
        }
    }

    private void ApplyInterlockingPrismBattleStartFaces(BattleEnemyController[] enemies)
    {
        if (!TryResolveInterlockingPrismStatuses(
                out BattleStatus_SO frontStatus,
                out string frontStatusId,
                out string frontStatusName,
                out BattleStatus_SO backStatus,
                out string backStatusId,
                out string backStatusName))
        {
            return;
        }

        BattleEnemyController dawn = FindLivingEnemyById(enemies, PrismDawnServantEnemyId);
        BattleEnemyController dusk = FindLivingEnemyById(enemies, PrismDuskServantEnemyId);
        if (dawn == null && dusk == null)
            return;

        if (dawn != null)
            SetInterlockingPrismFace(dawn, frontStatus, frontStatusId, frontStatusName, backStatus, backStatusId, backStatusName, true);
        if (dusk != null)
            SetInterlockingPrismFace(dusk, frontStatus, frontStatusId, frontStatusName, backStatus, backStatusId, backStatusName, false);

        BattleLog("敌方被动触发：交辉双棱在战斗开始时定下照面与背面。");
    }

    private void ApplyInterlockingPrismRoundEndFaces(BattleEnemyController[] enemies)
    {
        if (!TryResolveInterlockingPrismStatuses(
                out BattleStatus_SO frontStatus,
                out string frontStatusId,
                out string frontStatusName,
                out BattleStatus_SO backStatus,
                out string backStatusId,
                out string backStatusName))
        {
            return;
        }

        BattleEnemyController dawn = FindLivingEnemyById(enemies, PrismDawnServantEnemyId);
        BattleEnemyController dusk = FindLivingEnemyById(enemies, PrismDuskServantEnemyId);
        if (dawn == null && dusk == null)
            return;

        if (dawn != null && dusk != null)
        {
            bool dawnHasFront = dawn.GetStatusStacks(frontStatus) > 0 || dawn.GetStatusStacks(frontStatusId) > 0;
            bool duskHasFront = dusk.GetStatusStacks(frontStatus) > 0 || dusk.GetStatusStacks(frontStatusId) > 0;
            if (dawnHasFront == duskHasFront)
                dawnHasFront = true;

            SetInterlockingPrismFace(dawn, frontStatus, frontStatusId, frontStatusName, backStatus, backStatusId, backStatusName, !dawnHasFront);
            SetInterlockingPrismFace(dusk, frontStatus, frontStatusId, frontStatusName, backStatus, backStatusId, backStatusName, dawnHasFront);
            BattleLog("敌方被动触发：交辉轮转，曜面棱侍与晦面棱侍交换照面/背面。");
            return;
        }

        if (dawn != null)
        {
            SetInterlockingPrismFace(dawn, frontStatus, frontStatusId, frontStatusName, backStatus, backStatusId, backStatusName, true);
            BattleLog("敌方被动触发：独照，曜面棱侍始终保持照面。");
        }
        else if (dusk != null)
        {
            SetInterlockingPrismFace(dusk, frontStatus, frontStatusId, frontStatusName, backStatus, backStatusId, backStatusName, false);
            BattleLog("敌方被动触发：独照，晦面棱侍始终保持背面。");
        }
    }

    private static bool TryResolveInterlockingPrismStatuses(
        out BattleStatus_SO frontStatus,
        out string frontStatusId,
        out string frontStatusName,
        out BattleStatus_SO backStatus,
        out string backStatusId,
        out string backStatusName)
    {
        ResolveBuiltinStatus(PrismFrontStatusId, "照面", out frontStatus, out frontStatusId, out frontStatusName);
        ResolveBuiltinStatus(PrismBackStatusId, "背面", out backStatus, out backStatusId, out backStatusName);
        return !string.IsNullOrWhiteSpace(frontStatusId) && !string.IsNullOrWhiteSpace(backStatusId);
    }

    private static BattleEnemyController FindLivingEnemyById(BattleEnemyController[] enemies, int enemyId)
    {
        if (enemies == null)
            return null;

        for (int i = 0; i < enemies.Length; i++)
        {
            BattleEnemyController enemy = enemies[i];
            if (enemy != null && !enemy.IsDead && enemy.EnemyID == enemyId)
                return enemy;
        }

        return null;
    }

    private static void SetInterlockingPrismFace(
        BattleEnemyController enemy,
        BattleStatus_SO frontStatus,
        string frontStatusId,
        string frontStatusName,
        BattleStatus_SO backStatus,
        string backStatusId,
        string backStatusName,
        bool useFrontFace)
    {
        if (enemy == null)
            return;

        enemy.ClearStatus(frontStatus, frontStatusId, frontStatusName);
        enemy.ClearStatus(backStatus, backStatusId, backStatusName);
        if (useFrontFace)
            enemy.AddStatus(frontStatus, frontStatusId, frontStatusName, 1);
        else
            enemy.AddStatus(backStatus, backStatusId, backStatusName, 1);
    }

    private void ApplyAllyBattleStartCalamityEffectsIfNeeded(BattleAllyController[] allies)
    {
        if (allyBattleStartCalamityApplied)
            return;

        allyBattleStartCalamityApplied = true;
        if (allies == null)
            return;

        BattleEncounterData activeEncounter = BattleEncounterRuntime.ActiveEncounter;
        IReadOnlyList<RunCalamityState> activeCalamities = activeEncounter != null ? activeEncounter.activeCalamities : null;
        int hpLoss = CalamitySystem.ResolveAllyBattleStartHpLoss(activeCalamities);
        List<CalamityBuiltInEffectEntry> statusEffects = CalamitySystem.GetAllyBattleStartStatusEffects(activeCalamities);
        if (hpLoss <= 0 && statusEffects.Count == 0)
            return;

        for (int i = 0; i < allies.Length; i++)
        {
            BattleAllyController ally = allies[i];
            if (ally == null || ally.IsDead)
                continue;

            if (hpLoss > 0)
            {
                int dealt = ally.TakeDamage(hpLoss, 0, hpLoss);
                if (dealt > 0)
                    BattleLog($"灾厄效果：战斗开始时 {ally.GetDebugDisplayName()} 损失 {dealt} 点生命");
            }

            for (int effectIndex = 0; effectIndex < statusEffects.Count; effectIndex++)
            {
                CalamityBuiltInEffectEntry effect = statusEffects[effectIndex];
                if (effect == null || effect.statusData == null || effect.effectAmount <= 0)
                    continue;

                bool applied = ally.AddStatus(effect.statusData, effect.statusData.StatusId, effect.statusData.DisplayName, effect.effectAmount);
                if (applied)
                    ally.AddPendingRoundStartSpeedBonusFromStatus(effect.statusData, effect.effectAmount);
                if (applied)
                    BattleLog($"灾厄效果：{ally.GetDebugDisplayName()} 获得 {effect.effectAmount} 层 {effect.statusData.DisplayName}");
            }
        }
    }

    private void ApplyEnemyRepeatRoundStartStatusGains(BattleAllyController[] allies, BattleEnemyController[] enemies)
    {
        BattleEnemyTargetingStatusGainPhaseService.DispatchRepeatRoundStartEffects(
            currentRound,
            enemies,
            enemy => ApplyEnemyStatusGains(
                enemy,
                enemy.enemyData.repeatRoundStartStatusGains,
                (statusName, appliedStacks, currentStacks) =>
                    $"敌方回合效果：第 {currentRound} 回合开始时，{enemy.GetDebugDisplayName()} 获得 {appliedStacks} 层 {statusName}，当前 {statusName} {currentStacks} 层"),
            enemy => ApplyEnemyCustomRepeatRoundStartEffects(enemy, allies),
            enemy => TryLogEnemyStatusDrivenStageTransition(enemy, allies, enemies));

    }

    public bool CanSelectSkillForAlly(BattleAllyController ally, EnemySkill_SO skill)
    {
        return string.IsNullOrEmpty(GetAllySkillUnavailableReason(ally, skill));
    }

    public bool CanSelectSkillForAllyButton(BattleAllyController ally, SpeedButton button, EnemySkill_SO skill)
    {
        return string.IsNullOrEmpty(GetAllySkillUnavailableReason(ally, button, skill));
    }

    public string GetAllySkillUnavailableReason(BattleAllyController ally, EnemySkill_SO skill)
    {
        return GetAllySkillUnavailableReason(ally, null, skill);
    }

    public string GetAllySkillUnavailableReason(BattleAllyController ally, SpeedButton button, EnemySkill_SO skill)
    {
        if (ally == null || skill == null)
            return "技能未配置";

        bool hasEnoughEnergy = button != null
            ? BattleAllySkillEnergyAvailabilityService.HasEnoughEnergyForSkillOnButton(
                ally,
                button,
                skill,
                currentSharedEnergy,
                temporarySharedEnergy,
                reservedSharedEnergy)
            : BattleAllySkillEnergyAvailabilityService.HasEnoughEnergyForSkill(
                ally,
                skill,
                currentSharedEnergy,
                temporarySharedEnergy,
                reservedSharedEnergy);
        if (!hasEnoughEnergy)
            return "能量不足";

        if (skill.RequiresSelfStatusToUse)
        {
            int requiredStacks = Mathf.Max(1, skill.requiredSelfStatusStacks);
            int currentStacks = ally.GetStatusStacks(skill.requiredSelfStatusData);
            if (currentStacks < requiredStacks)
                return $"{skill.requiredSelfStatusData.DisplayName}不足";
        }

        return string.Empty;
    }

    private void RegisterSharedEnergyListeners(BattleAllyController[] allies)
    {
        BattleSharedEnergyListenerBindingService.Bind(
            allies,
            activeRoundEnemies,
            HandleAllySelectedSkillChanged,
            HandleAllyStatusChanged,
            HandleAllyConnectionChanged,
            HandleEnemyStatusChanged);
    }

    private void HandleAllySelectedSkillChanged(BattleAllyController ally)
    {
        if (ally == null)
            return;

        int previousReserved = reservedSharedEnergy;

        IReadOnlyList<SpeedButton> allyButtons = BattleSpeedButtonListService.GetAllyButtons(ally);
        List<SpeedButton> unavailableButtons = new List<SpeedButton>();
        for (int i = 0; i < allyButtons.Count; i++)
        {
            SpeedButton allyButton = allyButtons[i];
            if (allyButton == null || allyButton.ConnectedTarget == null)
                continue;

            EnemySkill_SO selectedSkill = ally.GetSelectedSkillForButton(allyButton);
            string unavailableReason = GetAllySkillUnavailableReason(ally, allyButton, selectedSkill);
            if (!string.IsNullOrEmpty(unavailableReason))
                unavailableButtons.Add(allyButton);
        }

        if (unavailableButtons.Count > 0)
        {
            if (!isRefreshingSharedEnergyReservations)
            {
                isRefreshingSharedEnergyReservations = true;
                for (int i = 0; i < unavailableButtons.Count; i++)
                {
                    if (unavailableButtons[i] != null)
                        unavailableButtons[i].DisconnectLine();
                }
                isRefreshingSharedEnergyReservations = false;
            }

            BattleLog($"能量不足：{ally.GetDebugDisplayName()} 有 {unavailableButtons.Count} 个速度按钮无法维持当前技能连线。");
        }

        RefreshSharedEnergyReservations(activeRoundAllies);
        ShowSharedEnergySpendPopupIfNeeded(ally, previousReserved, reservedSharedEnergy);
        battleSkillInfoOverlay?.RefreshCurrentView();
        HandleTutorialProgress(TutorialBattleRuntime.NotifyEvent(TutorialBattleEventType.SkillSwitched), false);
    }

    private void HandleAllyConnectionChanged(SpeedButton button)
    {
        if (button == null || isRefreshingSharedEnergyReservations)
            return;

        BattleAllyController ally = button.GetComponentInParent<BattleAllyController>();
        if (ally == null)
            return;

        int previousReserved = reservedSharedEnergy;

        EnemySkill_SO selectedSkill = ally.GetSelectedSkillForButton(button);
        string unavailableReason = GetAllySkillUnavailableReason(ally, button, selectedSkill);
        if (button.ConnectedTarget != null && !string.IsNullOrEmpty(unavailableReason))
        {
            isRefreshingSharedEnergyReservations = true;
            button.DisconnectLine();
            isRefreshingSharedEnergyReservations = false;
            BattleLog($"{unavailableReason}：{ally.GetDebugDisplayName()} 无法使用技能 {selectedSkill?.skillName ?? "未配置"} 进行目标指定。");
        }

        RefreshSharedEnergyReservations(activeRoundAllies);
        ShowSharedEnergySpendPopupIfNeeded(ally, previousReserved, reservedSharedEnergy);
        battleSkillInfoOverlay?.RefreshCurrentView();

        if (button.ConnectedTarget != null)
        {
            TutorialBattleProgressResult progress = TutorialBattleRuntime.NotifyEvent(GetReservedEnergyCostForAlly(ally) > 0
                ? TutorialBattleEventType.EnergyLineCreated
                : TutorialBattleEventType.LineCreated);
            HandleTutorialProgress(progress, true);
        }
    }

    private void HandleAllyStatusChanged(BattleAllyController ally)
    {
        if (ally == null || ally.StatusEntries == null || ally.StatusEntries.Count == 0)
            return;

        HandleTutorialProgress(TutorialBattleRuntime.NotifyEvent(TutorialBattleEventType.StatusApplied), false);
    }

    private void HandleEnemyStatusChanged(BattleEnemyController enemy)
    {
        if (enemy == null || enemy.StatusEntries == null || enemy.StatusEntries.Count == 0)
            return;

        HandleTutorialProgress(TutorialBattleRuntime.NotifyEvent(TutorialBattleEventType.StatusApplied), false);
    }

    private int GetReservedEnergyCostForAlly(BattleAllyController ally)
    {
        return BattleSharedEnergyReservationCollectorService.CalculateReservedEnergyForAlly(ally);
    }

    private void RefreshSharedEnergyReservations(BattleAllyController[] allies)
    {
        reservedSharedEnergy = BattleSharedEnergyReservationCollectorService.CalculateTotalReservedEnergy(
            allies,
            BattleSharedEnergyReservationService.CalculateTotalAvailableEnergy(currentSharedEnergy, temporarySharedEnergy));
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
        BattleSharedEnergySpendResult spendResult = BattleSharedEnergyReservationService.SpendReservedEnergy(
            currentSharedEnergy,
            temporarySharedEnergy,
            reservedSharedEnergy,
            currentSharedEnergyMax);
        currentSharedEnergy = spendResult.CurrentSharedEnergy;
        temporarySharedEnergy = spendResult.TemporarySharedEnergy;
        reservedSharedEnergy = spendResult.ReservedSharedEnergy;
        lastSpentSharedEnergy = spendResult.SpentSharedEnergy;
        lastSpentTemporarySharedEnergy = spendResult.SpentTemporarySharedEnergy;
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

    private int RefundLastSpentSharedEnergy(int maxRefundAmount)
    {
        int remainingRefund = Mathf.Max(0, maxRefundAmount);
        if (remainingRefund <= 0)
            return 0;

        int temporaryRefund = Mathf.Min(lastSpentTemporarySharedEnergy, remainingRefund);
        if (temporaryRefund > 0)
        {
            temporarySharedEnergy += temporaryRefund;
            lastSpentTemporarySharedEnergy -= temporaryRefund;
            remainingRefund -= temporaryRefund;
        }

        int normalRefund = 0;
        if (remainingRefund > 0)
        {
            int refundableNormal = Mathf.Min(lastSpentSharedEnergy, remainingRefund);
            int previous = currentSharedEnergy;
            currentSharedEnergy = Mathf.Clamp(currentSharedEnergy + refundableNormal, 0, currentSharedEnergyMax);
            normalRefund = currentSharedEnergy - previous;
            lastSpentSharedEnergy = Mathf.Max(0, lastSpentSharedEnergy - normalRefund);
        }

        int refunded = temporaryRefund + normalRefund;
        if (refunded > 0)
            RefreshSharedEnergyUi();
        return refunded;
    }

    public int AddTemporarySharedEnergy(int amount)
    {
        int added = Mathf.Max(0, amount);
        if (added <= 0)
            return 0;

        temporarySharedEnergy += added;
        RefreshSharedEnergyReservations(activeRoundAllies);
        RefreshSharedEnergyUi();
        return added;
    }

    private int IncreaseSharedEnergyMaxAtBattleStart(int amount)
    {
        int added = Mathf.Max(0, amount);
        if (added <= 0)
            return 0;

        currentSharedEnergyMax += added;
        currentSharedEnergy += added;
        RefreshSharedEnergyReservations(activeRoundAllies);
        RefreshSharedEnergyUi();
        return added;
    }

    private void ClearTemporarySharedEnergyAtRoundEnd()
    {
        if (temporarySharedEnergy <= 0)
            return;

        temporarySharedEnergy = 0;
        reservedSharedEnergy = Mathf.Clamp(
            reservedSharedEnergy,
            0,
            BattleSharedEnergyReservationService.CalculateTotalAvailableEnergy(currentSharedEnergy, temporarySharedEnergy));
        RefreshSharedEnergyUi();
    }

    private string GetTemporarySharedEnergyColorHtml()
    {
        return "#" + ColorUtility.ToHtmlStringRGBA(temporarySharedEnergyBarFillColor);
    }

    private void ShowSharedEnergyRefundPopupIfNeeded(BattleAllyController ally, int refundedEnergy)
    {
        if (ally == null || battleFeedbackSystem == null || refundedEnergy <= 0)
            return;

        battleFeedbackSystem.ShowCustomPopup(
            BattleActorVisualUtility.GetActorCenterWorld(ally.transform) + new Vector3(0f, 1.1f, 0f),
            $"+{refundedEnergy}",
            sharedEnergyBarFillColor);
    }

    private bool TryConsumeAllySkillRequirement(BattleAllyController ally, EnemySkill_SO skill, string actionLabel, bool refundEnergyOnFailure)
    {
        if (ally == null || skill == null || !skill.RequiresSelfStatusToUse)
            return true;

        BattleStatus_SO requiredStatus = skill.requiredSelfStatusData;
        int requiredStacks = Mathf.Max(1, skill.requiredSelfStatusStacks);
        int currentStacks = ally.GetStatusStacks(requiredStatus);
        if (currentStacks < requiredStacks)
        {
            if (refundEnergyOnFailure)
            {
                int refundedEnergy = RefundLastSpentSharedEnergy(Mathf.Max(0, skill.energyCost));
                ShowSharedEnergyRefundPopupIfNeeded(ally, refundedEnergy);
            }

            string statusName = requiredStatus != null ? requiredStatus.DisplayName : "状态";
            BattleLog($"{actionLabel}：{ally.GetDebugDisplayName()} 施放 {skill.skillName} 需要 {requiredStacks} 层 {statusName}，当前仅 {currentStacks} 层，技能未能施放。");
            return false;
        }

        if (skill.keepRequiredSelfStatusAfterUse)
            return true;

        int removedStacks = ally.RemoveStatusStacks(requiredStatus, requiredStatus != null ? requiredStatus.StatusId : null, requiredStacks, requiredStatus != null ? requiredStatus.DisplayName : null);
        string consumedStatusName = requiredStatus != null ? requiredStatus.DisplayName : "状态";
        int remainingStacks = requiredStatus != null ? ally.GetStatusStacks(requiredStatus) : 0;
        BattleLog($"{actionLabel}：{ally.GetDebugDisplayName()} 施放 {skill.skillName} 消耗 {removedStacks} 层 {consumedStatusName}，当前 {consumedStatusName} {remainingStacks} 层。");
        return true;
    }

    private void EnsureSharedEnergyUi()
    {
        if (sharedEnergyRoot != null)
            return;

        // 创建屏幕空间覆盖画布，挂到场景根节点（父物体为 null）。
        // 不能挂在 BattleButton 或任何 Canvas 子节点下，否则 ScreenSpaceOverlay 会受父 RectTransform 影响。
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
        sharedEnergyText.richText = true;
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
        GameObject temporaryBarFillGo = new GameObject("TemporaryEnergyBarFill", typeof(RectTransform), typeof(Image));
        temporaryBarFillGo.transform.SetParent(barBgGo.transform, false);
        temporarySharedEnergyBarFill = temporaryBarFillGo.GetComponent<Image>();
        temporarySharedEnergyBarFill.color = temporarySharedEnergyBarFillColor;

        RectTransform temporaryBarFillRect = temporaryBarFillGo.GetComponent<RectTransform>();
        temporaryBarFillRect.anchorMin = new Vector2(0f, 0f);
        temporaryBarFillRect.anchorMax = new Vector2(0f, 1f);
        temporaryBarFillRect.pivot = new Vector2(0f, 0.5f);
        temporaryBarFillRect.offsetMin = Vector2.zero;
        temporaryBarFillRect.offsetMax = Vector2.zero;

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

        sharedEnergyText.text = BattleSharedEnergyHudService.BuildEnergyText(
            currentSharedEnergy,
            temporarySharedEnergy,
            reservedSharedEnergy,
            currentSharedEnergyMax,
            GetTemporarySharedEnergyColorHtml());
        if (roundCounterText != null)
            roundCounterText.text = BattleSharedEnergyHudService.BuildRoundText(currentRound);

        if (sharedEnergyBarFill != null)
        {
            BattleSharedEnergyHudSegments segments = BattleSharedEnergyHudService.CalculateSegmentRatios(
                currentSharedEnergy,
                temporarySharedEnergy,
                reservedSharedEnergy,
                currentSharedEnergyMax);
            RectTransform fillRect = sharedEnergyBarFill.rectTransform;
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(segments.NormalFillRatio, 1f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            if (temporarySharedEnergyBarFill != null)
            {
                RectTransform temporaryFillRect = temporarySharedEnergyBarFill.rectTransform;
                temporaryFillRect.anchorMin = new Vector2(segments.NormalFillRatio, 0f);
                temporaryFillRect.anchorMax = new Vector2(segments.NormalFillRatio + segments.TemporaryFillRatio, 1f);
                temporaryFillRect.offsetMin = Vector2.zero;
                temporaryFillRect.offsetMax = Vector2.zero;
                temporarySharedEnergyBarFill.enabled = segments.TemporaryFillRatio > 0f;
            }
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
        SetActiveRoundCombatants(allies, enemies);
        ApplyCombatPhaseBeginPlan(BattleCombatPhaseBeginPlanService.Resolve(), allies, enemies);
        BattleLog("\u25c6\u25c6\u25c6 \u6218\u6597\u7ed3\u7b97\u9636\u6bb5 \u25c6\u25c6\u25c6");

        List<BattleCombatAction> actions = BuildCombatActions(allies, enemies);
        BattleCombatActionExecutionContext actionExecutionContext = CreateCombatActionExecutionContext(allies, enemies);
        yield return ExecuteCombatActions(actions, actionExecutionContext);

        if (actionExecutionContext.Result.BattleCompletedDuringAction)
            yield break;

        BattleCombatRoundEndDispatcher.Dispatch(
            allies,
            enemies,
            ally => ally.FinalizeRoundSkillHistory(),
            () => LogRoundSummary(allies, enemies),
            ApplyRoundEndStatusEffectsGeneric,
            ApplyBattleRoundEndRelics);
        ApplyInterlockingPrismRoundEndFaces(enemies);
        ClearTemporarySharedEnergyAtRoundEnd();

        BattleCombatPhaseCompletionPlan roundEndPlan = BattleCombatPhaseCompletionPlanService.Resolve(false);
        BattleCombatPhaseCompletionDispatcher.DispatchNormalRoundEnd(
            roundEndPlan,
            () => SpeedButton.CombatLocked = false,
            () => BattleLog("\u518d\u6b21\u6309\u4e0b\u6218\u6597\u6309\u94ae\u5f00\u59cb\u4e0b\u4e00\u56de\u5408\u76ee\u6807\u6307\u5b9a\u3002"),
            plan => ApplyCombatPhaseCompletionPlan(plan, false));
    }

    private IEnumerator ExecuteCombatActions(
        IReadOnlyList<BattleCombatAction> actions,
        BattleCombatActionExecutionContext actionExecutionContext)
    {
        yield return BattleCombatActionExecutionDispatcher.Dispatch(actions, actionExecutionContext);
    }

    private BattleCombatActionExecutionContext CreateCombatActionExecutionContext(
        BattleAllyController[] allies,
        BattleEnemyController[] enemies)
    {
        return new BattleCombatActionExecutionContext(
            action => ResolveClashAction(action.Ally, action.Enemy, action.AllyButton, action.EnemyButton),
            action => ResolveAllySingleAttackAction(action.Ally, action.Enemy, action.AllyButton),
            action => ResolveEnemySingleAttackAction(action.Enemy, action.TargetAlly, action.EnemyButton),
            () => TryHandleBattleCompletion(allies, enemies),
            () =>
            {
                BattleCombatPhaseCompletionDispatcher.DispatchBattleCompletedDuringAction(
                    BattleCombatPhaseCompletionPlanService.Resolve(true),
                    plan => ApplyCombatPhaseCompletionPlan(plan));
            });
    }

    private void LogRoundSummary(BattleAllyController[] allies, BattleEnemyController[] enemies)
    {
        IReadOnlyList<string> summaryLines = BattleRoundSummaryLogService.BuildSummaryLines(allies, enemies);
        for (int i = 0; i < summaryLines.Count; i++)
            BattleLog(summaryLines[i]);
    }

    private void ApplyCombatPhaseBeginPlan(BattleCombatPhaseBeginPlan plan, BattleAllyController[] allies, BattleEnemyController[] enemies)
    {
        if (plan.MarkResolvingCombat)
            isResolvingCombat = true;
        if (plan.CommitReservedEnergy)
            CommitReservedSharedEnergy();
        if (plan.LockCombatInput)
            SpeedButton.CombatLocked = true;

        BattleSpeedButtonCombatVisibilityService.ApplyCombatVisibility(
            allies,
            enemies,
            plan.DisableAllySpeedButtons,
            plan.HideAllySpeedButtons,
            plan.HideEnemySpeedButtons);

        if (plan.HideSkillInfoOverlay && battleSkillInfoOverlay != null && battleSkillInfoOverlay.IsVisible)
            battleSkillInfoOverlay.Hide(false);
    }

    private void ApplyCombatPhaseCompletionPlan(BattleCombatPhaseCompletionPlan plan, bool applyInputLock = true)
    {
        if (applyInputLock && plan.UnlockCombatInput)
            SpeedButton.CombatLocked = false;
        if (plan.ReturnToReadyPhase)
            currentPhase = BattlePhase.Ready;
        if (plan.AdvanceRound)
            currentRound++;
        if (plan.ClearActiveRoundCombatants)
            ClearActiveRoundCombatants();
        if (plan.FinishResolvingCombat)
            isResolvingCombat = false;
    }

    private IEnumerator PlayClashPresentation(
        BattleAllyController ally,
        BattleEnemyController enemy,
        bool hasWinner,
        bool allyWon,
        EnemySkill_SO.DicePowerRange allyBaseRange,
        EnemySkill_SO.DicePowerRange allyRange,
        EnemySkill_SO.DicePowerRange enemyBaseRange,
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
        System.Action onImpactResolved,
        bool moveIntoPosition = true,
        bool returnToOrigin = true)
    {
        if (battlePresentationSystem == null)
            yield break;

        BattlePresentationStep[] presentationSteps = BattleCombatPresentationCoroutinePlanService.BuildClashPresentation(
            moveIntoPosition,
            returnToOrigin);

        yield return battlePresentationSystem.PlayClashSequence(
            ally.transform,
            enemy.transform,
            hasWinner,
            allyWon,
            allyBaseRange,
            allyRange,
            enemyBaseRange,
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
            BattleCombatPresentationCoroutinePlanService.ContainsStep(presentationSteps, BattlePresentationStep.ApplyImpact) ? onImpactResolved : null,
            BattleCombatPresentationCoroutinePlanService.ContainsStep(presentationSteps, BattlePresentationStep.MoveToTarget),
            BattleCombatPresentationCoroutinePlanService.ContainsStep(presentationSteps, BattlePresentationStep.ReturnToOrigin));
    }

    private IEnumerator PlaySingleAttackPresentation(
        Transform attacker,
        Transform defender,
        bool attackerIsAlly,
        EnemySkill_SO.DicePowerRange baseAttackRange,
        EnemySkill_SO.DicePowerRange attackRange,
        int attackPower,
        int attackPowerModifier,
        float hitCameraPower,
        EnemySkill_SO.DicePresentationSettings dicePresentationSettings,
        System.Action onImpactResolved,
        bool moveIntoPosition = true,
        bool returnToOrigin = true)
    {
        if (battlePresentationSystem == null)
            yield break;

        BattlePresentationStep[] presentationSteps = BattleCombatPresentationCoroutinePlanService.BuildSingleAttackPresentation(
            moveIntoPosition,
            onImpactResolved != null,
            returnToOrigin);

        yield return battlePresentationSystem.PlaySingleAttackSequence(
            attacker,
            defender,
            attackerIsAlly,
            baseAttackRange,
            attackRange,
            attackPower,
            attackPowerModifier,
            hitCameraPower,
            dicePresentationSettings,
            BattleCombatPresentationCoroutinePlanService.ContainsStep(presentationSteps, BattlePresentationStep.ApplyImpact) ? onImpactResolved : null,
            BattleCombatPresentationCoroutinePlanService.ContainsStep(presentationSteps, BattlePresentationStep.MoveToTarget),
            BattleCombatPresentationCoroutinePlanService.ContainsStep(presentationSteps, BattlePresentationStep.ReturnToOrigin));
    }

    private List<BattleCombatAction> BuildCombatActions(BattleAllyController[] allies, BattleEnemyController[] enemies)
    {
        return BattleCombatActionBuilder.Build(allies, enemies, BattleLog);
    }

    private static bool IsDefenseSkill(EnemySkill_SO skill)
    {
        return IsDefenseSkill(skill, 0);
    }

    private static bool IsDefenseSkill(EnemySkill_SO skill, int diceIndex)
    {
        return skill != null && skill.GetDiceSkillCategory(diceIndex) == EnemySkill_SO.SkillCategory.Defense;
    }

    private EnemySkill_SO.DicePowerRange ApplyEnemyContextualDiceRangeModifiers(
        BattleEnemyController enemy,
        BattleAllyController target,
        int diceIndex,
        EnemySkill_SO.DicePowerRange range)
    {
        if (enemy == null || target == null || enemy.SelectedSkill == null)
            return range;

        return BattleDiceRangeModifierService.ApplyRangeModifiers(
            enemy.SelectedSkill,
            diceIndex,
            range,
            enemy.Speed,
            target.Speed,
            enemy.GetStatusStacks,
            enemy.GetStatusStacks,
            target.GetStatusStacks,
            target.GetStatusStacks,
            target.SelectedSkill);
    }

    private EnemySkill_SO.DicePowerRange ApplyAllyContextualDiceRangeModifiers(
        BattleAllyController ally,
        BattleEnemyController target,
        int diceIndex,
        EnemySkill_SO.DicePowerRange range)
    {
        if (ally == null || target == null || ally.SelectedSkill == null)
            return range;

        return BattleDiceRangeModifierService.ApplyRangeModifiers(
            ally.SelectedSkill,
            diceIndex,
            range,
            ally.Speed,
            target.Speed,
            ally.GetStatusStacks,
            ally.GetStatusStacks,
            target.GetStatusStacks,
            target.GetStatusStacks,
            target.SelectedSkill);
    }

    private void ApplyEnemyCustomRepeatRoundStartEffects(BattleEnemyController enemy, BattleAllyController[] allies)
    {
        if (enemy == null || enemy.IsDead)
            return;

        if (TryApplyEnemyRoundStartTransformWhenStatusMissing(enemy))
            return;

        if (enemy.EnemyID == 21 && currentRound % 2 == 0)
        {
            ResolveBuiltinStatus("erode", "姘磋殌", out BattleStatus_SO erodeStatus, out string erodeStatusId, out string erodeStatusName);
            for (int allyIndex = 0; allyIndex < allies.Length; allyIndex++)
            {
                BattleAllyController ally = allies[allyIndex];
                if (!BattleStatusFlowTargetService.ShouldConsumeStatusEffects(ally, target => target.IsDead))
                    continue;

                if (ally.AddStatus(erodeStatus, erodeStatusId, erodeStatusName, 1))
                {
                    int stacks = GetResolvedStatusStacks(ally, erodeStatus, erodeStatusId);
                    BattleLog($"敌方回合效果：第 {currentRound} 回合开始时，{enemy.GetDebugDisplayName()}[涨潮] 使 {ally.GetDebugDisplayName()} 获得 1 层 {erodeStatusName}，当前 {erodeStatusName} {stacks} 层");
                }
            }
        }

        if (enemy.EnemyID == 22)
        {
            int highestPoisonStacks = 0;
            for (int allyIndex = 0; allyIndex < allies.Length; allyIndex++)
            {
                BattleAllyController ally = allies[allyIndex];
                if (ally == null || ally.IsDead)
                    continue;

                highestPoisonStacks = Mathf.Max(highestPoisonStacks, ally.GetStatusStacks("poison"));
            }

            ResolveBuiltinStatus("guard", "涓存椂閲嶉槻", out BattleStatus_SO guardStatus, out string guardStatusId, out string guardStatusName);
            if (highestPoisonStacks > 0 && enemy.AddStatus(guardStatus, guardStatusId, guardStatusName, highestPoisonStacks))
            {
                int stacks = GetResolvedStatusStacks(enemy, guardStatus, guardStatusId);
                BattleLog($"敌方回合效果：第 {currentRound} 回合开始时，{enemy.GetDebugDisplayName()}[柔韧] 获得 {highestPoisonStacks} 层 {guardStatusName}，当前 {guardStatusName} {stacks} 层");
            }
        }
    }

    private void ApplyHornedGatekeeperClashWinEffects(BattleEnemyController enemy, BattleAllyController target, int diceIndex, int clashDifference)
    {
        if (enemy == null || target == null || clashDifference <= 0 || enemy.EnemyID != 26)
            return;

        if (!IsDefenseSkill(enemy.SelectedSkill, diceIndex))
            return;

        if (enemy.CurrentStageIndex <= 0)
        {
            int dealtDamage = target.TakeDamage(clashDifference, 0, clashDifference);
            BattleLog($"词条触发：{enemy.GetDebugDisplayName()}[守门] 在防御拼点胜利后对 {target.GetDebugDisplayName()} 造成 {dealtDamage} 点反震伤害");
            return;
        }

        enemy.QueueNextAttackSkillPowerBonus(1);
        BattleLog($"词条触发：{enemy.GetDebugDisplayName()}[断角] 已为下一个攻击技能累计 +1 威力预览");
    }

    private void ApplyBloodSpiderPregnancyClashLossEffects(BattleEnemyController enemy)
    {
        if (enemy == null || enemy.EnemyID != 30 || enemy.SelectedSkill == null || enemy.SelectedSkill.skillName != "蛛孕")
            return;

        List<BattleEnemyController> eggs = GetLivingEnemies();
        for (int i = 0; i < eggs.Count; i++)
        {
            BattleEnemyController egg = eggs[i];
            if (egg == null || egg.IsDead || egg.EnemyID != 2901)
                continue;

            int damage = Mathf.CeilToInt(egg.MaxHP * 0.5f);
            int dealtDamage = egg.TakeDamage(damage, 0, damage);
            if (dealtDamage > 0)
                BattleLog($"词条触发：{enemy.GetDebugDisplayName()}[蛛孕] 拼点失败，使 {egg.GetDebugDisplayName()} 失去 {dealtDamage} 点生命");
        }
    }

    private void ApplyBloodSpiderKinFeastEffects(BattleEnemyController attacker)
    {
        if (attacker == null || attacker.EnemyID != 30 || attacker.SelectedSkill == null || attacker.SelectedSkill.skillName != "噬亲")
            return;

        List<BattleEnemyController> enemies = GetLivingEnemies();
        List<BattleEnemyController> smallSpiders = new List<BattleEnemyController>();
        for (int i = 0; i < enemies.Count; i++)
        {
            BattleEnemyController enemy = enemies[i];
            if (enemy != null && enemy != attacker && enemy.EnemyID == 3002 && !enemy.IsDead)
                smallSpiders.Add(enemy);
        }

        ResolveBuiltinStatus("bleed", "流血", out BattleStatus_SO bleedStatus, out string bleedStatusId, out string bleedStatusName);
        ResolveBuiltinStatus("strength", "临时强壮", out BattleStatus_SO strengthStatus, out string strengthStatusId, out string strengthStatusName);
        ResolveBuiltinStatus("weakness", "虚弱", out BattleStatus_SO weaknessStatus, out string weaknessStatusId, out string weaknessStatusName);
        ResolveBuiltinStatus("guardbreak", "破防", out BattleStatus_SO guardbreakStatus, out string guardbreakStatusId, out string guardbreakStatusName);

        if (smallSpiders.Count == 0)
        {
            attacker.AddStatus(weaknessStatus, weaknessStatusId, weaknessStatusName, 1);
            attacker.AddStatus(guardbreakStatus, guardbreakStatusId, guardbreakStatusName, 1);
            BattleLog($"词条触发：{attacker.GetDebugDisplayName()}[饥饿] 未找到小血蛛，获得 1 层 {weaknessStatusName} 与 1 层 {guardbreakStatusName}");
            return;
        }

        int totalHeal = 0;
        for (int i = 0; i < smallSpiders.Count; i++)
        {
            BattleEnemyController spider = smallSpiders[i];
            int remainingHp = Mathf.Max(0, spider.CurrentHP);
            if (remainingHp <= 0)
                continue;

            totalHeal += remainingHp;
            spider.TakeDamage(remainingHp, 0, remainingHp);
        }

        int healed = attacker.Heal(totalHeal);
        attacker.AddStatus(strengthStatus, strengthStatusId, strengthStatusName, smallSpiders.Count);

        List<BattleAllyController> allies = GetLivingAllies();
        for (int i = 0; i < allies.Count; i++)
        {
            BattleAllyController ally = allies[i];
            if (ally != null && !ally.IsDead)
                ally.AddStatus(bleedStatus, bleedStatusId, bleedStatusName, 1);
        }

        BattleLog($"词条触发：{attacker.GetDebugDisplayName()}[噬亲] 吞噬 {smallSpiders.Count} 只小血蛛，恢复 {healed} 点生命并获得 {smallSpiders.Count} 层 {strengthStatusName}，所有目标获得 1 层 {bleedStatusName}");
        RefreshActiveRoundEnemiesFromScene();
    }

    private void ApplyWanderingPhantomBlendIntoShadowEffects(BattleEnemyController attacker)
    {
        if (attacker == null || attacker.EnemyID != 33 || attacker.SelectedSkill == null || attacker.SelectedSkill.skillName != "融入暗影")
            return;

        int remnantCount = 0;
        List<BattleEnemyController> enemies = GetLivingEnemies();
        for (int i = 0; i < enemies.Count; i++)
        {
            BattleEnemyController enemy = enemies[i];
            if (enemy != null && enemy != attacker && enemy.EnemyID == 32 && !enemy.IsDead)
                remnantCount++;
        }

        if (remnantCount <= 0)
            return;

        ResolveBuiltinStatus("strength", "强壮", out BattleStatus_SO strengthStatus, out string strengthStatusId, out string strengthStatusName);
        attacker.AddStatus(strengthStatus, strengthStatusId, strengthStatusName, remnantCount);
        BattleLog($"词条触发：{attacker.GetDebugDisplayName()}[融入暗影] 场上有 {remnantCount} 个陌路残影，获得 {remnantCount} 层 {strengthStatusName}");
    }

    private void ApplyEnemyBuiltInEffects(BattleEnemyController attacker, BattleAllyController currentTarget, int diceIndex, EnemySkill_SO.DiceEffectTrigger trigger)
    {
        EnemySkill_SO skill = attacker != null ? attacker.SelectedSkill : null;
        if (skill == null)
            return;

        EnemySkill_SO.DiceEffectSettings settings = skill.GetDiceEffectSettings(diceIndex);
        if (settings.builtInEffects == null || settings.builtInEffects.Length == 0)
            return;

        List<BattleAllyController> opponents = GetLivingAllies();
        List<BattleEnemyController> friendlies = GetLivingEnemies();
        IReadOnlyList<BattleIndexedEffect<EnemySkill_SO.BuiltInEffectEntry>> eligibleEffects = BattleEnemyBuiltInEffectResolutionService.BuildEligibleEffects(
            settings.builtInEffects,
            trigger,
            effect => IsEnemyBuiltInEffectConditionMet(attacker, currentTarget, effect, opponents, friendlies));

        ApplyQueuedNextDicePowerEffects(eligibleEffects, false);
        ApplyEnemyStatusScaledBuiltInDamageEffects(attacker, currentTarget, eligibleEffects);

        BattleEnemyBuiltInEffectDispatcher.Dispatch(
            eligibleEffects,
            currentTarget,
            opponents,
            BuildBuiltInEffectLabel,
            (targetMode, target, candidates, count) => SelectAllyTargets(targetMode, target, candidates != null ? new List<BattleAllyController>(candidates) : null, count),
            target => target != null && !target.IsDead,
            (effect, _, effectLabel) =>
            {
                int healedAmount = attacker.Heal(effect.amount);
                if (healedAmount > 0)
                    BattleLog(BattleEnemyBuiltInEffectLogService.BuildHealLog(attacker.GetDebugDisplayName(), effectLabel, null, healedAmount, attacker.CurrentHP, attacker.MaxHP));
            },
            (target, effect, _, effectLabel) =>
            {
                int healedAmount = target.Heal(effect.amount);
                if (healedAmount > 0)
                    BattleLog(BattleEnemyBuiltInEffectLogService.BuildHealLog(attacker.GetDebugDisplayName(), effectLabel, target.GetDebugDisplayName(), healedAmount, target.CurrentHP, target.MaxHP));
            },
            (effect, _, effectLabel) => ApplyEnemyBuiltInEffectToEnemyFriendlies(attacker, effect, effectLabel, (friendly, builtInEffect, label) =>
            {
                int healedAmount = friendly.Heal(builtInEffect.amount);
                if (healedAmount > 0)
                    BattleLog(BattleEnemyBuiltInEffectLogService.BuildHealLog(attacker.GetDebugDisplayName(), label, friendly.GetDebugDisplayName(), healedAmount, friendly.CurrentHP, friendly.MaxHP));
            }),
            (effect, _, effectLabel) =>
            {
                int resolvedDamage = ApplyEnemyCalamityDamageModifiers(effect.amount);
                int dealtDamage = attacker.TakeDamage(resolvedDamage, 0, effect.amount);
                if (dealtDamage > 0)
                    BattleLog(BattleEnemyBuiltInEffectLogService.BuildDamageLog(attacker.GetDebugDisplayName(), effectLabel, null, dealtDamage));
            },
            (target, effect, _, effectLabel) =>
            {
                int resolvedDamage = ApplyEnemyCalamityDamageModifiers(effect.amount);
                int dealtDamage = target.TakeDamage(resolvedDamage, 0, effect.amount);
                if (dealtDamage > 0)
                    BattleLog(BattleEnemyBuiltInEffectLogService.BuildDamageLog(attacker.GetDebugDisplayName(), effectLabel, target.GetDebugDisplayName(), dealtDamage));
            },
            (effect, _, effectLabel) => ApplyEnemyBuiltInEffectToEnemyFriendlies(attacker, effect, effectLabel, (friendly, builtInEffect, label) =>
            {
                int resolvedDamage = ApplyEnemyCalamityDamageModifiers(builtInEffect.amount);
                int dealtDamage = friendly.TakeDamage(resolvedDamage, 0, builtInEffect.amount);
                if (dealtDamage > 0)
                    BattleLog(BattleEnemyBuiltInEffectLogService.BuildDamageLog(attacker.GetDebugDisplayName(), label, friendly.GetDebugDisplayName(), dealtDamage));
            }),
            (effect, _, effectLabel) =>
            {
                if (TryResolveBuiltInPrimaryStatus(effect, out BattleStatus_SO statusData, out string statusId, out string statusName))
                    ApplyEnemyBuiltInStatusToEnemy(attacker, attacker, effectLabel, statusData, statusId, statusName, effect.statusStackAmount, effect.applyTiming, effect.applyAfterRoundStarts, effect.applyRepeatCount);
            },
            (target, effect, _, effectLabel) =>
            {
                if (TryResolveBuiltInPrimaryStatus(effect, out BattleStatus_SO statusData, out string statusId, out string statusName))
                    ApplyEnemyBuiltInStatusToAlly(attacker, target, effectLabel, statusData, statusId, statusName, effect.statusStackAmount, effect.applyTiming, effect.applyAfterRoundStarts, effect.applyRepeatCount);
            },
            (effect, _, effectLabel) =>
            {
                if (!TryResolveBuiltInPrimaryStatus(effect, out BattleStatus_SO statusData, out string statusId, out string statusName))
                    return;

                ApplyEnemyBuiltInEffectToEnemyFriendlies(attacker, effect, effectLabel, (friendly, builtInEffect, label) =>
                    ApplyEnemyBuiltInStatusToEnemy(attacker, friendly, label, statusData, statusId, statusName, builtInEffect.statusStackAmount, builtInEffect.applyTiming, builtInEffect.applyAfterRoundStarts, builtInEffect.applyRepeatCount));
            },
            (targets, effect, _, effectLabel) => ApplyEnemyBuiltInTransferEffectToTargets(attacker, targets, effect, effectLabel));
    }

    private void ApplyAllyBuiltInEffects(BattleAllyController attacker, BattleEnemyController currentTarget, int diceIndex, EnemySkill_SO.DiceEffectTrigger trigger)
    {
        EnemySkill_SO skill = attacker != null ? attacker.SelectedSkill : null;
        if (skill == null)
            return;

        EnemySkill_SO.DiceEffectSettings settings = skill.GetDiceEffectSettings(diceIndex);
        if (settings.builtInEffects == null || settings.builtInEffects.Length == 0)
            return;

        List<BattleEnemyController> opponents = GetLivingEnemies();
        List<BattleAllyController> friendlies = GetLivingAllies();
        IReadOnlyList<BattleIndexedEffect<EnemySkill_SO.BuiltInEffectEntry>> eligibleEffects = BattleEnemyBuiltInEffectResolutionService.BuildEligibleEffects(
            settings.builtInEffects,
            trigger,
            effect => IsAllyBuiltInEffectConditionMet(attacker, currentTarget, effect, opponents, friendlies));

        ApplyQueuedNextDicePowerEffects(eligibleEffects, true);
        ApplyAllyStatusScaledBuiltInDamageEffects(attacker, currentTarget, diceIndex, eligibleEffects);

        BattleEnemyBuiltInEffectDispatcher.Dispatch(
            eligibleEffects,
            currentTarget,
            opponents,
            BuildBuiltInEffectLabel,
            (targetMode, target, candidates, count) => SelectEnemyTargets(targetMode, target, candidates != null ? new List<BattleEnemyController>(candidates) : null, count),
            target => target != null && !target.IsDead,
            (effect, _, effectLabel) =>
            {
                int healedAmount = attacker.Heal(effect.amount);
                if (healedAmount > 0)
                    BattleLog(BattleEnemyBuiltInEffectLogService.BuildHealLog(attacker.GetDebugDisplayName(), effectLabel, null, healedAmount, attacker.CurrentHP, attacker.MaxHP));
            },
            (target, effect, _, effectLabel) =>
            {
                int healedAmount = target.Heal(effect.amount);
                if (healedAmount > 0)
                    BattleLog(BattleEnemyBuiltInEffectLogService.BuildHealLog(attacker.GetDebugDisplayName(), effectLabel, target.GetDebugDisplayName(), healedAmount, target.CurrentHP, target.MaxHP));
            },
            (effect, _, effectLabel) => ApplyAllyBuiltInEffectToAllyFriendlies(attacker, effect, effectLabel, (friendly, builtInEffect, label) =>
            {
                int healedAmount = friendly.Heal(builtInEffect.amount);
                if (healedAmount > 0)
                    BattleLog(BattleEnemyBuiltInEffectLogService.BuildHealLog(attacker.GetDebugDisplayName(), label, friendly.GetDebugDisplayName(), healedAmount, friendly.CurrentHP, friendly.MaxHP));
            }),
            (effect, _, effectLabel) =>
            {
                int dealtDamage = attacker.TakeDamage(effect.amount, 0, effect.amount);
                if (dealtDamage > 0)
                    BattleLog(BattleEnemyBuiltInEffectLogService.BuildDamageLog(attacker.GetDebugDisplayName(), effectLabel, null, dealtDamage));
            },
            (target, effect, _, effectLabel) =>
            {
                int resolvedDamage = ApplyPlayerRelicOutgoingDamageModifiers(attacker, target, diceIndex, effect.amount);
                int dealtDamage = target.TakeDamage(resolvedDamage, 0, effect.amount);
                if (dealtDamage > 0)
                    BattleLog(BattleEnemyBuiltInEffectLogService.BuildDamageLog(attacker.GetDebugDisplayName(), effectLabel, target.GetDebugDisplayName(), dealtDamage));
            },
            (effect, _, effectLabel) => ApplyAllyBuiltInEffectToAllyFriendlies(attacker, effect, effectLabel, (friendly, builtInEffect, label) =>
            {
                int dealtDamage = friendly.TakeDamage(builtInEffect.amount, 0, builtInEffect.amount);
                if (dealtDamage > 0)
                    BattleLog(BattleEnemyBuiltInEffectLogService.BuildDamageLog(attacker.GetDebugDisplayName(), label, friendly.GetDebugDisplayName(), dealtDamage));
            }),
            (effect, _, effectLabel) =>
            {
                if (TryResolveBuiltInPrimaryStatus(effect, out BattleStatus_SO statusData, out string statusId, out string statusName))
                    ApplyAllyBuiltInStatusToAlly(attacker, attacker, effectLabel, statusData, statusId, statusName, effect.statusStackAmount, effect.applyTiming, effect.applyAfterRoundStarts, effect.applyRepeatCount);
            },
            (target, effect, _, effectLabel) =>
            {
                if (TryResolveBuiltInPrimaryStatus(effect, out BattleStatus_SO statusData, out string statusId, out string statusName))
                    ApplyAllyBuiltInStatusToEnemy(attacker, target, effectLabel, statusData, statusId, statusName, effect.statusStackAmount, effect.applyTiming, effect.applyAfterRoundStarts, effect.applyRepeatCount);
            },
            (effect, _, effectLabel) =>
            {
                if (!TryResolveBuiltInPrimaryStatus(effect, out BattleStatus_SO statusData, out string statusId, out string statusName))
                    return;

                ApplyAllyBuiltInEffectToAllyFriendlies(attacker, effect, effectLabel, (friendly, builtInEffect, label) =>
                    ApplyAllyBuiltInStatusToAlly(attacker, friendly, label, statusData, statusId, statusName, builtInEffect.statusStackAmount, builtInEffect.applyTiming, builtInEffect.applyAfterRoundStarts, builtInEffect.applyRepeatCount));
            },
            (targets, effect, _, effectLabel) => ApplyAllyBuiltInTransferEffectToTargets(attacker, targets, effect, effectLabel));
    }

    private void ApplyQueuedNextDicePowerEffects(
        IReadOnlyList<BattleIndexedEffect<EnemySkill_SO.BuiltInEffectEntry>> eligibleEffects,
        bool isAlly)
    {
        if (eligibleEffects == null)
            return;

        for (int i = 0; i < eligibleEffects.Count; i++)
        {
            EnemySkill_SO.BuiltInEffectEntry effect = eligibleEffects[i].Effect;
            if (effect.effectType != EnemySkill_SO.BuiltInEffectType.QueueNextDicePowerBonus || effect.amount == 0)
                continue;

            if (isAlly)
                pendingAllyNextDicePowerBonus += effect.amount;
            else
                pendingEnemyNextDicePowerBonus += effect.amount;
        }
    }

    private bool TryApplyEnemyRoundStartTransformWhenStatusMissing(BattleEnemyController enemy)
    {
        if (enemy == null || enemy.IsDead || enemy.enemyData == null)
            return false;

        EnemyData_SO targetData = enemy.enemyData.transformToEnemyDataWhenStatusMissing;
        BattleStatus_SO requiredStatus = enemy.enemyData.transformMissingStatusData;
        if (targetData == null || requiredStatus == null)
            return false;

        if (enemy.GetStatusStacks(requiredStatus) > 0)
            return false;

        string previousName = enemy.GetDebugDisplayName();
        enemy.ApplyEnemyData(targetData, true);
        BattleLog($"敌方回合效果：{previousName} 失去 {requiredStatus.DisplayName} 后转化为 {enemy.GetDebugDisplayName()}");
        return true;
    }

    private void ApplyAllyStatusScaledBuiltInDamageEffects(
        BattleAllyController attacker,
        BattleEnemyController currentTarget,
        int diceIndex,
        IReadOnlyList<BattleIndexedEffect<EnemySkill_SO.BuiltInEffectEntry>> eligibleEffects)
    {
        if (attacker == null || eligibleEffects == null)
            return;

        for (int i = 0; i < eligibleEffects.Count; i++)
        {
            BattleIndexedEffect<EnemySkill_SO.BuiltInEffectEntry> indexedEffect = eligibleEffects[i];
            EnemySkill_SO.BuiltInEffectEntry effect = indexedEffect.Effect;
            if (effect.effectType != EnemySkill_SO.BuiltInEffectType.DamageByTargetStatusStacksDivided)
                continue;

            string effectLabel = BuildBuiltInEffectLabel(effect, indexedEffect.Index);
            if (effect.targetMode == EnemySkill_SO.DamageTargetMode.Self)
            {
                int selfDamage = ResolveStatusScaledBuiltInDamage(effect, ResolveBuiltInSelfStatusStacks(effect, attacker));
                int dealtSelfDamage = attacker.TakeDamage(selfDamage, 0, selfDamage);
                if (dealtSelfDamage > 0)
                    BattleLog(BattleEnemyBuiltInEffectLogService.BuildDamageLog(attacker.GetDebugDisplayName(), effectLabel, null, dealtSelfDamage));
                continue;
            }

            List<BattleEnemyController> targets = SelectEnemyTargets(effect.targetMode, currentTarget, GetLivingEnemies(), effect.randomTargetCount);
            for (int targetIndex = 0; targetIndex < targets.Count; targetIndex++)
            {
                BattleEnemyController target = targets[targetIndex];
                if (target == null || target.IsDead)
                    continue;

                int baseDamage = ResolveStatusScaledBuiltInDamage(effect, ResolveBuiltInTargetStatusStacks(effect, target));
                if (baseDamage <= 0)
                    continue;

                int resolvedDamage = ApplyPlayerRelicOutgoingDamageModifiers(attacker, target, diceIndex, baseDamage);
                int dealtDamage = target.TakeDamage(resolvedDamage, 0, baseDamage);
                if (dealtDamage > 0)
                    BattleLog(BattleEnemyBuiltInEffectLogService.BuildDamageLog(attacker.GetDebugDisplayName(), effectLabel, target.GetDebugDisplayName(), dealtDamage));
            }
        }
    }

    private void ApplyEnemyStatusScaledBuiltInDamageEffects(
        BattleEnemyController attacker,
        BattleAllyController currentTarget,
        IReadOnlyList<BattleIndexedEffect<EnemySkill_SO.BuiltInEffectEntry>> eligibleEffects)
    {
        if (attacker == null || eligibleEffects == null)
            return;

        for (int i = 0; i < eligibleEffects.Count; i++)
        {
            BattleIndexedEffect<EnemySkill_SO.BuiltInEffectEntry> indexedEffect = eligibleEffects[i];
            EnemySkill_SO.BuiltInEffectEntry effect = indexedEffect.Effect;
            if (effect.effectType != EnemySkill_SO.BuiltInEffectType.DamageByTargetStatusStacksDivided)
                continue;

            string effectLabel = BuildBuiltInEffectLabel(effect, indexedEffect.Index);
            if (effect.targetMode == EnemySkill_SO.DamageTargetMode.Self)
            {
                int selfDamage = ResolveStatusScaledBuiltInDamage(effect, ResolveBuiltInSelfStatusStacks(effect, attacker));
                int resolvedSelfDamage = ApplyEnemyCalamityDamageModifiers(selfDamage);
                int dealtSelfDamage = attacker.TakeDamage(resolvedSelfDamage, 0, selfDamage);
                if (dealtSelfDamage > 0)
                    BattleLog(BattleEnemyBuiltInEffectLogService.BuildDamageLog(attacker.GetDebugDisplayName(), effectLabel, null, dealtSelfDamage));
                continue;
            }

            List<BattleAllyController> targets = SelectAllyTargets(effect.targetMode, currentTarget, GetLivingAllies(), effect.randomTargetCount);
            for (int targetIndex = 0; targetIndex < targets.Count; targetIndex++)
            {
                BattleAllyController target = targets[targetIndex];
                if (target == null || target.IsDead)
                    continue;

                int baseDamage = ResolveStatusScaledBuiltInDamage(effect, ResolveBuiltInTargetStatusStacks(effect, target));
                if (baseDamage <= 0)
                    continue;

                int resolvedDamage = ApplyEnemyCalamityDamageModifiers(baseDamage);
                int dealtDamage = target.TakeDamage(resolvedDamage, 0, baseDamage);
                if (dealtDamage > 0)
                    BattleLog(BattleEnemyBuiltInEffectLogService.BuildDamageLog(attacker.GetDebugDisplayName(), effectLabel, target.GetDebugDisplayName(), dealtDamage));
            }
        }
    }

    private static int ResolveStatusScaledBuiltInDamage(EnemySkill_SO.BuiltInEffectEntry effect, int statusStacks)
    {
        int threshold = Mathf.Max(1, effect.requiredConditionStatusStacks);
        int damagePerStep = Mathf.Max(0, effect.amount);
        if (statusStacks <= 0 || damagePerStep <= 0)
            return 0;

        return (statusStacks / threshold) * damagePerStep;
    }

    private int ConsumePendingAllyNextDicePowerBonus()
    {
        int bonus = pendingAllyNextDicePowerBonus;
        pendingAllyNextDicePowerBonus = 0;
        return bonus;
    }

    private int ConsumePendingEnemyNextDicePowerBonus()
    {
        int bonus = pendingEnemyNextDicePowerBonus;
        pendingEnemyNextDicePowerBonus = 0;
        return bonus;
    }

    private void ApplyAllyBuiltInTransferEffectToTargets(
        BattleAllyController attacker,
        IReadOnlyList<BattleEnemyController> targets,
        EnemySkill_SO.BuiltInEffectEntry effect,
        string effectLabel)
    {
        List<BattleStatusRuntimeEntry> transferableStatuses = BattleEnemyBuiltInTransferService.BuildTransferableStatuses(attacker.StatusEntries);

        if (transferableStatuses.Count > 0 && targets != null && targets.Count > 0)
        {
            BattleEnemyBuiltInTransferPlan transferPlan = BattleEnemyBuiltInTransferService.ResolveTransferPlan(
                transferableStatuses,
                targets.Count,
                effect.amount,
                maxExclusive => UnityEngine.Random.Range(0, maxExclusive));
            if (transferPlan.ShouldTransfer)
            {
                BattleEnemyController transferTarget = targets[transferPlan.TargetIndex];
                if (BattleEnemyBuiltInTransferDispatcher.TryDispatch(
                    transferPlan,
                    (selectedStatus, transferStacks) => attacker.RemoveStatusStacks(selectedStatus.statusData, selectedStatus.fallbackStatusId, transferStacks, selectedStatus.DisplayName),
                    (selectedStatus, removedStacks) => transferTarget.AddStatus(selectedStatus.statusData, selectedStatus.fallbackStatusId, selectedStatus.DisplayName, removedStacks),
                    (selectedStatus, removedStacks) => BattleLog(BattleEnemyBuiltInEffectLogService.BuildTransferLog(attacker.GetDebugDisplayName(), effectLabel, selectedStatus.DisplayName, removedStacks, transferTarget.GetDebugDisplayName()))))
                {
                    return;
                }
            }
        }

        int fallbackStacks = Mathf.Max(0, effect.fallbackAmount);
        if (fallbackStacks <= 0)
            return;
        if (!TryResolveBuiltInPrimaryStatus(effect, out BattleStatus_SO fallbackStatusData, out string fallbackStatusId, out string fallbackStatusName))
            return;

        ApplyAllyBuiltInStatusToAlly(attacker, attacker, effectLabel, fallbackStatusData, fallbackStatusId, fallbackStatusName, fallbackStacks, effect.applyTiming, effect.applyAfterRoundStarts, effect.applyRepeatCount, "未找到可转移的负面状态");
    }

    private void ApplyEnemyBuiltInTransferEffectToTargets(
        BattleEnemyController attacker,
        IReadOnlyList<BattleAllyController> targets,
        EnemySkill_SO.BuiltInEffectEntry effect,
        string effectLabel)
    {
        List<BattleStatusRuntimeEntry> transferableStatuses = BattleEnemyBuiltInTransferService.BuildTransferableStatuses(attacker.StatusEntries);

        if (transferableStatuses.Count > 0 && targets != null && targets.Count > 0)
        {
            BattleEnemyBuiltInTransferPlan transferPlan = BattleEnemyBuiltInTransferService.ResolveTransferPlan(
                transferableStatuses,
                targets.Count,
                effect.amount,
                maxExclusive => UnityEngine.Random.Range(0, maxExclusive));
            if (transferPlan.ShouldTransfer)
            {
                BattleAllyController transferTarget = targets[transferPlan.TargetIndex];
                if (BattleEnemyBuiltInTransferDispatcher.TryDispatch(
                    transferPlan,
                    (selectedStatus, transferStacks) => attacker.RemoveStatusStacks(selectedStatus.statusData, selectedStatus.fallbackStatusId, transferStacks, selectedStatus.DisplayName),
                    (selectedStatus, removedStacks) => transferTarget.AddStatus(selectedStatus.statusData, selectedStatus.fallbackStatusId, selectedStatus.DisplayName, removedStacks),
                    (selectedStatus, removedStacks) => BattleLog(BattleEnemyBuiltInEffectLogService.BuildTransferLog(attacker.GetDebugDisplayName(), effectLabel, selectedStatus.DisplayName, removedStacks, transferTarget.GetDebugDisplayName()))))
                {
                    return;
                }
            }
        }

        int fallbackStacks = Mathf.Max(0, effect.fallbackAmount);
        if (fallbackStacks <= 0)
            return;
        if (!TryResolveBuiltInPrimaryStatus(effect, out BattleStatus_SO fallbackStatusData, out string fallbackStatusId, out string fallbackStatusName))
            return;

        ApplyEnemyBuiltInStatusToEnemy(attacker, attacker, effectLabel, fallbackStatusData, fallbackStatusId, fallbackStatusName, fallbackStacks, effect.applyTiming, effect.applyAfterRoundStarts, effect.applyRepeatCount, "未找到可转移的负面状态");
    }

    private void ApplyAllyBuiltInEffectToAllyFriendlies(
        BattleAllyController attacker,
        EnemySkill_SO.BuiltInEffectEntry effect,
        string effectLabel,
        Action<BattleAllyController, EnemySkill_SO.BuiltInEffectEntry, string> apply)
    {
        if (attacker == null || apply == null)
            return;

        List<BattleAllyController> friendlies = GetAllyFriendlyBuiltInTargets(attacker, effect);
        for (int i = 0; i < friendlies.Count; i++)
        {
            BattleAllyController friendly = friendlies[i];
            if (friendly != null && !friendly.IsDead)
                apply(friendly, effect, effectLabel);
        }
    }

    private List<BattleAllyController> GetAllyFriendlyBuiltInTargets(
        BattleAllyController attacker,
        EnemySkill_SO.BuiltInEffectEntry effect)
    {
        List<BattleAllyController> candidates = GetLivingAllies();
        if (effect.targetMode == EnemySkill_SO.DamageTargetMode.AllFriendliesExceptSelf)
        {
            for (int i = candidates.Count - 1; i >= 0; i--)
            {
                if (candidates[i] == attacker)
                    candidates.RemoveAt(i);
            }
        }

        return SelectAllyTargets(effect.targetMode, attacker, candidates, effect.randomTargetCount);
    }

    private void ApplyEnemyBuiltInEffectToEnemyFriendlies(
        BattleEnemyController attacker,
        EnemySkill_SO.BuiltInEffectEntry effect,
        string effectLabel,
        Action<BattleEnemyController, EnemySkill_SO.BuiltInEffectEntry, string> apply)
    {
        if (attacker == null || apply == null)
            return;

        List<BattleEnemyController> friendlies = GetEnemyFriendlyBuiltInTargets(attacker, effect);
        for (int i = 0; i < friendlies.Count; i++)
        {
            BattleEnemyController friendly = friendlies[i];
            if (friendly != null && !friendly.IsDead)
                apply(friendly, effect, effectLabel);
        }
    }

    private List<BattleEnemyController> GetEnemyFriendlyBuiltInTargets(
        BattleEnemyController attacker,
        EnemySkill_SO.BuiltInEffectEntry effect)
    {
        List<BattleEnemyController> candidates = GetLivingEnemies();
        if (effect.targetMode == EnemySkill_SO.DamageTargetMode.AllFriendliesExceptSelf)
        {
            for (int i = candidates.Count - 1; i >= 0; i--)
            {
                if (candidates[i] == attacker)
                    candidates.RemoveAt(i);
            }
        }

        return SelectEnemyTargets(effect.targetMode, attacker, candidates, effect.randomTargetCount);
    }

    private static bool IsEnemyBuiltInEffectConditionMet(
        BattleEnemyController attacker,
        BattleAllyController currentTarget,
        EnemySkill_SO.BuiltInEffectEntry effect,
        List<BattleAllyController> opponents,
        List<BattleEnemyController> friendlies)
    {
        if (effect.useStatusCondition)
        {
            if (!TryResolveBuiltInConditionStatus(effect, out BattleStatus_SO statusData, out string statusId, out _))
                return false;

            int requiredStacks = Mathf.Max(1, effect.requiredStatusStacks);
            bool statusConditionMet;
            switch (effect.conditionScope)
            {
                case EnemySkill_SO.BuiltInConditionScope.CurrentTarget:
                    statusConditionMet = GetStatusStacks(currentTarget, statusData, statusId) >= requiredStacks;
                    break;
                case EnemySkill_SO.BuiltInConditionScope.AnyOpponent:
                    statusConditionMet = false;
                    for (int i = 0; i < opponents.Count; i++)
                    {
                        if (GetStatusStacks(opponents[i], statusData, statusId) >= requiredStacks)
                        {
                            statusConditionMet = true;
                            break;
                        }
                    }
                    break;
                case EnemySkill_SO.BuiltInConditionScope.AnyFriendly:
                    statusConditionMet = false;
                    for (int i = 0; i < friendlies.Count; i++)
                    {
                        if (GetStatusStacks(friendlies[i], statusData, statusId) >= requiredStacks)
                        {
                            statusConditionMet = true;
                            break;
                        }
                    }
                    break;
                case EnemySkill_SO.BuiltInConditionScope.Self:
                    statusConditionMet = GetStatusStacks(attacker, statusData, statusId) >= requiredStacks;
                    break;
                default:
                    statusConditionMet = false;
                    break;
            }

            if (!statusConditionMet)
                return false;
        }

        if (!effect.useCondition)
            return true;

        return IsEnemyBuiltInGeneralConditionMet(attacker, currentTarget, effect, opponents);
    }

    private static bool IsAllyBuiltInEffectConditionMet(
        BattleAllyController attacker,
        BattleEnemyController currentTarget,
        EnemySkill_SO.BuiltInEffectEntry effect,
        List<BattleEnemyController> opponents,
        List<BattleAllyController> friendlies)
    {
        if (effect.useStatusCondition)
        {
            if (!TryResolveBuiltInConditionStatus(effect, out BattleStatus_SO statusData, out string statusId, out _))
                return false;

            int requiredStacks = Mathf.Max(1, effect.requiredStatusStacks);
            bool statusConditionMet;
            switch (effect.conditionScope)
            {
                case EnemySkill_SO.BuiltInConditionScope.CurrentTarget:
                    statusConditionMet = GetStatusStacks(currentTarget, statusData, statusId) >= requiredStacks;
                    break;
                case EnemySkill_SO.BuiltInConditionScope.AnyOpponent:
                    statusConditionMet = false;
                    for (int i = 0; i < opponents.Count; i++)
                    {
                        if (GetStatusStacks(opponents[i], statusData, statusId) >= requiredStacks)
                        {
                            statusConditionMet = true;
                            break;
                        }
                    }
                    break;
                case EnemySkill_SO.BuiltInConditionScope.AnyFriendly:
                    statusConditionMet = false;
                    for (int i = 0; i < friendlies.Count; i++)
                    {
                        if (GetStatusStacks(friendlies[i], statusData, statusId) >= requiredStacks)
                        {
                            statusConditionMet = true;
                            break;
                        }
                    }
                    break;
                case EnemySkill_SO.BuiltInConditionScope.Self:
                    statusConditionMet = GetStatusStacks(attacker, statusData, statusId) >= requiredStacks;
                    break;
                default:
                    statusConditionMet = false;
                    break;
            }

            if (!statusConditionMet)
                return false;
        }

        if (!effect.useCondition)
            return true;

        return IsAllyBuiltInGeneralConditionMet(attacker, currentTarget, effect, opponents);
    }

    private static bool IsEnemyBuiltInGeneralConditionMet(
        BattleEnemyController attacker,
        BattleAllyController currentTarget,
        EnemySkill_SO.BuiltInEffectEntry effect,
        List<BattleAllyController> opponents)
    {
        int selfSpeed = attacker != null ? attacker.Speed : 0;
        int targetSpeed = currentTarget != null ? currentTarget.Speed : 0;
        switch (effect.condition)
        {
            case EnemySkill_SO.PowerModifierCondition.SelfHasStatus:
                return ResolveBuiltInSelfStatusStacks(effect, attacker) > 0;
            case EnemySkill_SO.PowerModifierCondition.SelfStatusStacksGreaterThan:
                return ResolveBuiltInSelfStatusStacks(effect, attacker) > Mathf.Max(0, effect.requiredConditionStatusStacks);
            case EnemySkill_SO.PowerModifierCondition.SelfStatusStacksGreaterOrEqual:
                return ResolveBuiltInSelfStatusStacks(effect, attacker) >= Mathf.Max(1, effect.requiredConditionStatusStacks);
            case EnemySkill_SO.PowerModifierCondition.SelfStatusStacksLessThan:
                return ResolveBuiltInSelfStatusStacks(effect, attacker) < Mathf.Max(1, effect.requiredConditionStatusStacks);
            case EnemySkill_SO.PowerModifierCondition.SelfStatusStacksLessOrEqual:
                return ResolveBuiltInSelfStatusStacks(effect, attacker) <= Mathf.Max(0, effect.requiredConditionStatusStacks);
            case EnemySkill_SO.PowerModifierCondition.TargetSkillIsSpecific:
                return effect.targetSkillData != null && currentTarget != null && currentTarget.SelectedSkill == effect.targetSkillData;
            case EnemySkill_SO.PowerModifierCondition.TargetHasStatus:
                return ResolveBuiltInTargetStatusStacks(effect, currentTarget) > 0;
            case EnemySkill_SO.PowerModifierCondition.TargetStatusStacksAtLeast:
                return ResolveBuiltInTargetStatusStacks(effect, currentTarget) >= Mathf.Max(1, effect.requiredConditionStatusStacks);
            case EnemySkill_SO.PowerModifierCondition.TargetStatusStacksPerThreshold:
                return ResolveBuiltInTargetStatusStacks(effect, currentTarget) >= Mathf.Max(1, effect.requiredConditionStatusStacks);
            case EnemySkill_SO.PowerModifierCondition.TargetNegativeStatusTypesAtLeast:
                return BattleNegativeStatusService.HasAtLeastNegativeStatusTypes(
                    statusId => currentTarget != null ? currentTarget.GetStatusStacks(statusId) : 0,
                    effect.requiredConditionStatusStacks);
            case EnemySkill_SO.PowerModifierCondition.AnyOpponentNegativeStatusTypesAtLeast:
                return HasAnyAllyWithNegativeStatusTypes(opponents, effect.requiredConditionStatusStacks);
            case EnemySkill_SO.PowerModifierCondition.SelfHpRatioBelowOrEqual:
                return IsHpRatioBelowOrEqual(attacker != null ? attacker.CurrentHP : 0, attacker != null ? attacker.MaxHP : 0, effect.requiredConditionStatusStacks);
            default:
                return BattleModifierConditionService.IsConditionMet(effect.condition, selfSpeed, targetSpeed, effect.requiredConditionSpeedValue, effect.requiredConditionSpeedDifference);
        }
    }

    private static bool IsAllyBuiltInGeneralConditionMet(
        BattleAllyController attacker,
        BattleEnemyController currentTarget,
        EnemySkill_SO.BuiltInEffectEntry effect,
        List<BattleEnemyController> opponents)
    {
        int selfSpeed = attacker != null ? attacker.Speed : 0;
        int targetSpeed = currentTarget != null ? currentTarget.Speed : 0;
        switch (effect.condition)
        {
            case EnemySkill_SO.PowerModifierCondition.SelfHasStatus:
                return ResolveBuiltInSelfStatusStacks(effect, attacker) > 0;
            case EnemySkill_SO.PowerModifierCondition.SelfStatusStacksGreaterThan:
                return ResolveBuiltInSelfStatusStacks(effect, attacker) > Mathf.Max(0, effect.requiredConditionStatusStacks);
            case EnemySkill_SO.PowerModifierCondition.SelfStatusStacksGreaterOrEqual:
                return ResolveBuiltInSelfStatusStacks(effect, attacker) >= Mathf.Max(1, effect.requiredConditionStatusStacks);
            case EnemySkill_SO.PowerModifierCondition.SelfStatusStacksLessThan:
                return ResolveBuiltInSelfStatusStacks(effect, attacker) < Mathf.Max(1, effect.requiredConditionStatusStacks);
            case EnemySkill_SO.PowerModifierCondition.SelfStatusStacksLessOrEqual:
                return ResolveBuiltInSelfStatusStacks(effect, attacker) <= Mathf.Max(0, effect.requiredConditionStatusStacks);
            case EnemySkill_SO.PowerModifierCondition.TargetSkillIsSpecific:
                return effect.targetSkillData != null && currentTarget != null && currentTarget.SelectedSkill == effect.targetSkillData;
            case EnemySkill_SO.PowerModifierCondition.TargetHasStatus:
                return ResolveBuiltInTargetStatusStacks(effect, currentTarget) > 0;
            case EnemySkill_SO.PowerModifierCondition.TargetStatusStacksAtLeast:
                return ResolveBuiltInTargetStatusStacks(effect, currentTarget) >= Mathf.Max(1, effect.requiredConditionStatusStacks);
            case EnemySkill_SO.PowerModifierCondition.TargetStatusStacksPerThreshold:
                return ResolveBuiltInTargetStatusStacks(effect, currentTarget) >= Mathf.Max(1, effect.requiredConditionStatusStacks);
            case EnemySkill_SO.PowerModifierCondition.TargetNegativeStatusTypesAtLeast:
                return BattleNegativeStatusService.HasAtLeastNegativeStatusTypes(
                    statusId => currentTarget != null ? currentTarget.GetStatusStacks(statusId) : 0,
                    effect.requiredConditionStatusStacks);
            case EnemySkill_SO.PowerModifierCondition.AnyOpponentNegativeStatusTypesAtLeast:
                return HasAnyEnemyWithNegativeStatusTypes(opponents, effect.requiredConditionStatusStacks);
            case EnemySkill_SO.PowerModifierCondition.SelfHpRatioBelowOrEqual:
                return IsHpRatioBelowOrEqual(attacker != null ? attacker.CurrentHP : 0, attacker != null ? attacker.MaxHP : 0, effect.requiredConditionStatusStacks);
            default:
                return BattleModifierConditionService.IsConditionMet(effect.condition, selfSpeed, targetSpeed, effect.requiredConditionSpeedValue, effect.requiredConditionSpeedDifference);
        }
    }

    private static bool IsHpRatioBelowOrEqual(int currentHp, int maxHp, int percentThreshold)
    {
        if (maxHp <= 0)
            return false;

        int clampedThreshold = Mathf.Clamp(percentThreshold, 0, 100);
        return currentHp * 100 <= maxHp * clampedThreshold;
    }

    private static bool HasAnyAllyWithNegativeStatusTypes(List<BattleAllyController> allies, int requiredTypes)
    {
        if (allies == null)
            return false;

        for (int i = 0; i < allies.Count; i++)
        {
            BattleAllyController ally = allies[i];
            if (ally == null || ally.IsDead)
                continue;

            if (BattleNegativeStatusService.HasAtLeastNegativeStatusTypes(ally.GetStatusStacks, requiredTypes))
                return true;
        }

        return false;
    }

    private static bool HasAnyEnemyWithNegativeStatusTypes(List<BattleEnemyController> enemies, int requiredTypes)
    {
        if (enemies == null)
            return false;

        for (int i = 0; i < enemies.Count; i++)
        {
            BattleEnemyController enemy = enemies[i];
            if (enemy == null || enemy.IsDead)
                continue;

            if (BattleNegativeStatusService.HasAtLeastNegativeStatusTypes(enemy.GetStatusStacks, requiredTypes))
                return true;
        }

        return false;
    }

    private static int ResolveBuiltInSelfStatusStacks(EnemySkill_SO.BuiltInEffectEntry effect, BattleEnemyController attacker)
    {
        if (attacker == null)
            return 0;
        if (effect.targetStatusData != null)
            return Mathf.Max(0, attacker.GetStatusStacks(effect.targetStatusData));
        if (effect.targetStatusType == EnemySkill_SO.DiceStatusType.Burn)
            return Mathf.Max(0, attacker.GetStatusStacks("burn"));
        return 0;
    }

    private static int ResolveBuiltInSelfStatusStacks(EnemySkill_SO.BuiltInEffectEntry effect, BattleAllyController attacker)
    {
        if (attacker == null)
            return 0;
        if (effect.targetStatusData != null)
            return Mathf.Max(0, attacker.GetStatusStacks(effect.targetStatusData));
        if (effect.targetStatusType == EnemySkill_SO.DiceStatusType.Burn)
            return Mathf.Max(0, attacker.GetStatusStacks("burn"));
        return 0;
    }

    private static int ResolveBuiltInTargetStatusStacks(EnemySkill_SO.BuiltInEffectEntry effect, BattleAllyController currentTarget)
    {
        if (currentTarget == null)
            return 0;
        if (effect.targetStatusData != null)
            return Mathf.Max(0, currentTarget.GetStatusStacks(effect.targetStatusData));
        if (effect.targetStatusType == EnemySkill_SO.DiceStatusType.Burn)
            return Mathf.Max(0, currentTarget.GetStatusStacks("burn"));
        return 0;
    }

    private static int ResolveBuiltInTargetStatusStacks(EnemySkill_SO.BuiltInEffectEntry effect, BattleEnemyController currentTarget)
    {
        if (currentTarget == null)
            return 0;
        if (effect.targetStatusData != null)
            return Mathf.Max(0, currentTarget.GetStatusStacks(effect.targetStatusData));
        if (effect.targetStatusType == EnemySkill_SO.DiceStatusType.Burn)
            return Mathf.Max(0, currentTarget.GetStatusStacks("burn"));
        return 0;
    }

    private void ApplyEnemyBuiltInStatusToEnemy(
        BattleEnemyController attacker,
        BattleEnemyController target,
        string effectLabel,
        BattleStatus_SO statusData,
        string statusId,
        string statusName,
        int stackAmount,
        EnemySkill_SO.StatusApplyTiming applyTiming,
        int applyAfterRoundStarts,
        int applyRepeatCount,
        string logPrefix = null)
    {
        if (target == null || target.IsDead || stackAmount <= 0)
            return;

        BattleStatusApplicationPlan plan = BattleStatusApplicationPlanService.BuildPlan(applyTiming, stackAmount, applyAfterRoundStarts, applyRepeatCount);
        BattleStatusApplicationDispatcher.Execute(
            plan,
            (stacks, delayRounds, repeatCount) =>
            {
                bool queued = target.QueueStatusForRoundStart(statusData, statusId, statusName, stacks, delayRounds, repeatCount);
                if (queued)
                    BattleLog(BattleEnemyBuiltInStatusLogService.BuildDelayedLog(attacker.GetDebugDisplayName(), effectLabel, null, statusName, stacks));
                return queued;
            },
            stacks =>
            {
                bool added = target.AddStatus(statusData, statusId, statusName, stacks);
                if (added)
                    BattleLog(BattleEnemyBuiltInStatusLogService.BuildImmediateLog(attacker.GetDebugDisplayName(), effectLabel, null, statusName, stacks, GetResolvedStatusStacks(target, statusData, statusId), logPrefix));
                return added;
            },
            null);
    }

    private void ApplyEnemyBuiltInStatusToAlly(
        BattleEnemyController attacker,
        BattleAllyController target,
        string effectLabel,
        BattleStatus_SO statusData,
        string statusId,
        string statusName,
        int stackAmount,
        EnemySkill_SO.StatusApplyTiming applyTiming,
        int applyAfterRoundStarts,
        int applyRepeatCount)
    {
        if (target == null || target.IsDead || stackAmount <= 0)
            return;

        BattleStatusApplicationPlan plan = BattleStatusApplicationPlanService.BuildPlan(applyTiming, stackAmount, applyAfterRoundStarts, applyRepeatCount);
        BattleStatusApplicationDispatcher.Execute(
            plan,
            (stacks, delayRounds, repeatCount) =>
            {
                bool queued = target.QueueStatusForRoundStart(statusData, statusId, statusName, stacks, delayRounds, repeatCount);
                if (queued)
                    BattleLog(BattleEnemyBuiltInStatusLogService.BuildDelayedLog(attacker.GetDebugDisplayName(), effectLabel, target.GetDebugDisplayName(), statusName, stacks));
                return queued;
            },
            stacks =>
            {
                bool added = target.AddStatus(statusData, statusId, statusName, stacks);
                if (added)
                    BattleLog(BattleEnemyBuiltInStatusLogService.BuildImmediateLog(attacker.GetDebugDisplayName(), effectLabel, target.GetDebugDisplayName(), statusName, stacks, GetResolvedStatusStacks(target, statusData, statusId), null));
                return added;
            },
            null);
    }

    private void ApplyAllyBuiltInStatusToAlly(
        BattleAllyController attacker,
        BattleAllyController target,
        string effectLabel,
        BattleStatus_SO statusData,
        string statusId,
        string statusName,
        int stackAmount,
        EnemySkill_SO.StatusApplyTiming applyTiming,
        int applyAfterRoundStarts,
        int applyRepeatCount,
        string logPrefix = null)
    {
        if (target == null || target.IsDead || stackAmount <= 0)
            return;

        BattleStatusApplicationPlan plan = BattleStatusApplicationPlanService.BuildPlan(applyTiming, stackAmount, applyAfterRoundStarts, applyRepeatCount);
        BattleStatusApplicationDispatcher.Execute(
            plan,
            (stacks, delayRounds, repeatCount) =>
            {
                bool queued = target.QueueStatusForRoundStart(statusData, statusId, statusName, stacks, delayRounds, repeatCount);
                if (queued)
                    BattleLog(BattleEnemyBuiltInStatusLogService.BuildDelayedLog(attacker.GetDebugDisplayName(), effectLabel, null, statusName, stacks));
                return queued;
            },
            stacks =>
            {
                bool added = target.AddStatus(statusData, statusId, statusName, stacks);
                if (added)
                    BattleLog(BattleEnemyBuiltInStatusLogService.BuildImmediateLog(attacker.GetDebugDisplayName(), effectLabel, null, statusName, stacks, GetResolvedStatusStacks(target, statusData, statusId), logPrefix));
                return added;
            },
            null);
    }

    private void ApplyAllyBuiltInStatusToEnemy(
        BattleAllyController attacker,
        BattleEnemyController target,
        string effectLabel,
        BattleStatus_SO statusData,
        string statusId,
        string statusName,
        int stackAmount,
        EnemySkill_SO.StatusApplyTiming applyTiming,
        int applyAfterRoundStarts,
        int applyRepeatCount)
    {
        if (target == null || target.IsDead || stackAmount <= 0)
            return;

        BattleStatusApplicationPlan plan = BattleStatusApplicationPlanService.BuildPlan(applyTiming, stackAmount, applyAfterRoundStarts, applyRepeatCount);
        BattleStatusApplicationDispatcher.Execute(
            plan,
            (stacks, delayRounds, repeatCount) =>
            {
                bool queued = target.QueueStatusForRoundStart(statusData, statusId, statusName, stacks, delayRounds, repeatCount);
                if (queued)
                    BattleLog(BattleEnemyBuiltInStatusLogService.BuildDelayedLog(attacker.GetDebugDisplayName(), effectLabel, target.GetDebugDisplayName(), statusName, stacks));
                return queued;
            },
            stacks =>
            {
                int adjustedStacks = ResolvePlayerRelicAdjustedStatusStacks(statusData, statusId, statusName, stacks, attacker, target);
                bool added = target.AddStatus(statusData, statusId, statusName, adjustedStacks);
                if (added)
                    BattleLog(BattleEnemyBuiltInStatusLogService.BuildImmediateLog(attacker.GetDebugDisplayName(), effectLabel, target.GetDebugDisplayName(), statusName, adjustedStacks, GetResolvedStatusStacks(target, statusData, statusId), null));
                return added;
            },
            null);
    }

    private static BattleStatusLibrary_SO GetBattleStatusLibrary()
    {
        if (cachedBattleStatusLibrary == null)
            cachedBattleStatusLibrary = Resources.Load<BattleStatusLibrary_SO>(BattleStatusLibraryResourcePath);
        return cachedBattleStatusLibrary;
    }

    private static bool TryResolveBuiltInConditionStatus(EnemySkill_SO.BuiltInEffectEntry effect, out BattleStatus_SO statusData, out string statusId, out string statusName)
    {
        statusId = string.Empty;
        statusName = "未配置状态";
        statusData = effect.conditionStatusData;
        if (statusData != null)
        {
            statusId = !string.IsNullOrWhiteSpace(statusData.StatusId) ? statusData.StatusId : string.Empty;
            statusName = !string.IsNullOrWhiteSpace(statusData.DisplayName) ? statusData.DisplayName : statusName;
            return true;
        }

        if (effect.conditionStatusType == EnemySkill_SO.DiceStatusType.Burn)
        {
            ResolveBuiltinStatus("burn", "鐕冪儳", out statusData, out statusId, out statusName);
            return true;
        }

        return false;
    }

    private static bool TryResolveBuiltInPrimaryStatus(EnemySkill_SO.BuiltInEffectEntry effect, out BattleStatus_SO statusData, out string statusId, out string statusName)
    {
        statusId = string.Empty;
        statusName = "未配置状态";
        statusData = effect.statusData;
        if (statusData != null)
        {
            statusId = !string.IsNullOrWhiteSpace(statusData.StatusId) ? statusData.StatusId : string.Empty;
            statusName = !string.IsNullOrWhiteSpace(statusData.DisplayName) ? statusData.DisplayName : statusName;
            return true;
        }

        if (effect.statusType == EnemySkill_SO.DiceStatusType.Burn)
        {
            ResolveBuiltinStatus("burn", "鐕冪儳", out statusData, out statusId, out statusName);
            return true;
        }

        return false;
    }

    private static void ResolveBuiltinStatus(string fallbackStatusId, string fallbackDisplayName, out BattleStatus_SO statusData, out string statusId, out string statusName)
    {
        statusId = fallbackStatusId ?? string.Empty;
        statusData = GetBattleStatusLibrary()?.GetStatusData(statusId);
        statusName = statusData != null && !string.IsNullOrWhiteSpace(statusData.DisplayName)
            ? statusData.DisplayName
            : fallbackDisplayName;
    }

    private static int GetResolvedStatusStacks(BattleAllyController target, BattleStatus_SO statusData, string statusId)
    {
        if (target == null)
            return 0;

        return statusData != null ? target.GetStatusStacks(statusData) : target.GetStatusStacks(statusId);
    }

    private static int GetResolvedStatusStacks(BattleEnemyController target, BattleStatus_SO statusData, string statusId)
    {
        if (target == null)
            return 0;

        return statusData != null ? target.GetStatusStacks(statusData) : target.GetStatusStacks(statusId);
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

    private int RestoreDefenseEnergyForClashIfNeeded(BattleClashImpactPlan impactPlan, BattleAllyController ally)
    {
        if (!impactPlan.RestoreAllyDefenseEnergy)
            return 0;

        int restoredDefenseEnergy = RestoreSharedEnergy(impactPlan.DefenseEnergyRestoreAmount);
        if (restoredDefenseEnergy > 0 && ally != null)
        {
            battleFeedbackSystem?.ShowCustomPopup(
                BattleActorVisualUtility.GetActorCenterWorld(ally.transform) + new Vector3(0f, 1.1f, 0f),
                $"+{restoredDefenseEnergy}",
                sharedEnergyBarFillColor);
        }

        return restoredDefenseEnergy;
    }

    private void ResolveClashImpactPlan(
        BattleClashImpactPlan impactPlan,
        BattleAllyController ally,
        BattleEnemyController enemy,
        int diceIndex,
        SkillHitTracker allyHitTracker,
        SkillHitTracker enemyHitTracker)
    {
        if (impactPlan.ApplyAllyClashWinEffects)
        {
            ApplyAllyTriggeredEffects(ally, enemy, diceIndex, EnemySkill_SO.DiceEffectTrigger.OnClashWin, 0);
            BattleGamblerPassiveService.TryApplyPlayerGambleClashWinDamage(ally, enemy, out _, BattleLog);
        }

        if (impactPlan.ApplyEnemyClashWinEffects)
        {
            ApplyEnemyTriggeredEffects(enemy, ally, diceIndex, EnemySkill_SO.DiceEffectTrigger.OnClashWin, 0);
            ProcessPlayerClashLostRelics(ally, enemy, diceIndex);
        }

        if (impactPlan.ApplyHornedGatekeeperClashWin)
            ApplyHornedGatekeeperClashWinEffects(enemy, ally, diceIndex, impactPlan.HornedGatekeeperClashDifference);

        if (impactPlan.ApplyEnemyClashLoseEffects)
            ApplyBloodSpiderPregnancyClashLossEffects(enemy);

        if (impactPlan.ApplyAllyHit)
        {
            int dealtDamage = ApplyAllyHit(ally, enemy, impactPlan.AllyHitPower, diceIndex, false, impactPlan.AllyHitPowerModifier);
            if (allyHitTracker != null)
                allyHitTracker.HitCount++;
        }

        if (impactPlan.ApplyEnemyHit)
        {
            int dealtDamage = ApplyEnemyHit(enemy, ally, impactPlan.EnemyHitPower, diceIndex, false, impactPlan.EnemyHitPowerModifier);
            if (enemyHitTracker != null)
                enemyHitTracker.HitCount++;
        }

        if (impactPlan.ApplyPlayerDefenseClashWinRelics)
            ApplyPlayerDefenseClashWinRelics(ally, enemy, diceIndex);

        if (impactPlan.ShowZeroDamageOnEnemy && enemy != null)
            ShowZeroDamagePopup(enemy.transform, true, impactPlan.EnemyZeroDamagePowerModifier);

        if (impactPlan.ShowZeroDamageOnAlly && ally != null)
            ShowZeroDamagePopup(ally.transform, false, impactPlan.AllyZeroDamagePowerModifier);

        if (impactPlan.ApplyAllyMiss)
            ApplyAllyTriggeredEffects(ally, enemy, diceIndex, EnemySkill_SO.DiceEffectTrigger.OnMiss, 0);

        if (impactPlan.ApplyEnemyMiss)
            ApplyEnemyTriggeredEffects(enemy, ally, diceIndex, EnemySkill_SO.DiceEffectTrigger.OnMiss, 0);
    }

    private string BuildClashPostLog(
        BattleClashPresentationPlan presentationPlan,
        BattleAllyController ally,
        BattleEnemyController enemy,
        int allyFollowUpBasePower,
        int enemyFollowUpBasePower,
        int allyWinAttackBonus,
        int enemyWinAttackBonus,
        int allyHitPower,
        int enemyHitPower,
        int allyPower,
        int enemyPower,
        int restoredDefenseEnergy)
    {
        switch (presentationPlan.PostLogType)
        {
            case BattleClashPostLogType.AllyAttackWinFollowUp:
                return BattleClashLogService.FormatAllyAttackWinFollowUp(
                    allyFollowUpBasePower,
                    allyWinAttackBonus,
                    enemy.GetDebugDisplayName(),
                    allyHitPower,
                    enemy.CurrentHP,
                    enemy.MaxHP);

            case BattleClashPostLogType.EnemyAttackWinFollowUp:
                return BattleClashLogService.FormatEnemyAttackWinFollowUp(
                    enemyFollowUpBasePower,
                    enemyWinAttackBonus,
                    enemy.GetDebugDisplayName(),
                    ally.GetDebugDisplayName(),
                    enemyHitPower,
                    ally.CurrentHP,
                    ally.MaxHP);

            case BattleClashPostLogType.AttackTie:
                return BattleClashLogService.FormatAttackTie(allyPower, enemyPower);

            case BattleClashPostLogType.AllyAttackEnemyDefense:
                return BattleClashLogService.FormatAllyAttackEnemyDefenseResult(
                    presentationPlan.AttackBreaksDefense,
                    ally.GetDebugDisplayName(),
                    enemy.GetDebugDisplayName(),
                    allyHitPower,
                    enemy.CurrentHP,
                    enemy.MaxHP,
                    allyPower,
                    enemyPower);

            case BattleClashPostLogType.AllyDefenseEnemyAttack:
                return BattleClashLogService.FormatAllyDefenseEnemyAttackResult(
                    presentationPlan.AttackBreaksDefense,
                    enemy.GetDebugDisplayName(),
                    ally.GetDebugDisplayName(),
                    enemyHitPower,
                    ally.CurrentHP,
                    ally.MaxHP,
                    allyPower,
                    enemyPower,
                    restoredDefenseEnergy);

            case BattleClashPostLogType.BothDefense:
            default:
                return BattleClashLogService.FormatBothDefense(allyPower, enemyPower);
        }
    }

    private string BuildSingleAttackPostLog(
        BattleSingleAttackPresentationPlan presentationPlan,
        int diceIndex,
        string contextLabel,
        string actorName,
        string skillSummary,
        BattleSingleAttackResult attackResult,
        string targetName,
        int targetCurrentHp,
        int targetMaxHp)
    {
        switch (presentationPlan.PostLogType)
        {
            case BattleSingleAttackPostLogType.DefenseOnly:
                return BattleSingleAttackLogService.FormatDefenseOnly(
                    diceIndex,
                    contextLabel,
                    actorName,
                    skillSummary,
                    attackResult.FormattedPower);

            case BattleSingleAttackPostLogType.Hit:
            default:
                return BattleSingleAttackLogService.FormatHit(
                    diceIndex,
                    contextLabel,
                    actorName,
                    skillSummary,
                    attackResult.FormattedPower,
                    targetName,
                    targetCurrentHp,
                    targetMaxHp);
        }
    }

    private struct SingleAttackDiceInput
    {
        public EnemySkill_SO.DicePowerRange BaseDisplayRange;
        public EnemySkill_SO.DicePowerRange DisplayRange;
        public BattleSingleAttackResult AttackResult;
        public EnemySkill_SO.DicePresentationSettings DiceFx;
    }

    private sealed class SkillHitTracker
    {
        public int AttackDiceCount;
        public int HitCount;

        public bool ShouldTriggerAllDiceHit => AttackDiceCount > 0 && HitCount == AttackDiceCount;
    }

    private SingleAttackDiceInput PrepareAllySingleAttackDice(BattleAllyController ally, BattleEnemyController enemy, int diceIndex)
    {
        PlayerRelicDiceModifier allyRelicModifier = ResolvePlayerRelicDiceModifier(ally, enemy, diceIndex, false);
        EnemySkill_SO.DicePowerRange allyRange = ApplyAllyContextualDiceRangeModifiers(ally, enemy, diceIndex, ally.GetDicePowerRange(diceIndex));
        allyRange = BattleDiceRollService.ApplyRangeBonus(allyRange, ally.ConsumeCurrentRoundMinPowerBonus(), 0);
        allyRange = BattleDiceRollService.ApplyRangeBonus(allyRange, allyRelicModifier.minPowerBonus, allyRelicModifier.maxPowerBonus);
        EnemySkill_SO.DicePowerRange allyBaseDisplayRange = ally.SelectedSkill != null
            ? ally.GetDisplayedDicePowerRange(ally.SelectedSkill, diceIndex)
            : allyRange;
        EnemySkill_SO.DicePowerRange allyDisplayRange = ApplyAllyContextualDiceRangeModifiers(ally, enemy, diceIndex, allyBaseDisplayRange);
        allyDisplayRange = BattleDiceRollService.ApplyRangeBonus(allyDisplayRange, allyRelicModifier.minPowerBonus, allyRelicModifier.maxPowerBonus);
        int allyBasePower = BattleDiceRollService.RollPower(allyRange);
        ProcessPlayerDiceRolledRelics(ally, enemy, diceIndex, allyBasePower, Mathf.Max(1, allyRange.minPower));
        bool isDefenseSkill = IsDefenseSkill(ally.SelectedSkill, diceIndex);
        int allyStatusPowerModifier = ally.GetStatusFlatPowerModifier(isDefenseSkill);
        int queuedNextDicePowerBonus = ConsumePendingAllyNextDicePowerBonus();
        BattleSingleAttackDiceResolution diceResolution = BattleSingleAttackDiceResolutionService.ResolveWithModifiers(
            ally.SelectedSkill,
            diceIndex,
            allyBasePower,
            allyStatusPowerModifier,
            isDefenseSkill,
            ally.Speed,
            enemy.Speed,
            ally.GetStatusStacks,
            ally.GetStatusStacks,
            enemy.GetStatusStacks,
            enemy.GetStatusStacks,
            relicPowerBonus: allyRelicModifier.powerBonus + queuedNextDicePowerBonus);

        return new SingleAttackDiceInput
        {
            BaseDisplayRange = allyBaseDisplayRange,
            DisplayRange = allyDisplayRange,
            AttackResult = diceResolution.AttackResult,
            DiceFx = ally.GetDicePresentationSettings(diceIndex)
        };
    }

    private SingleAttackDiceInput PrepareEnemySingleAttackDice(BattleEnemyController enemy, BattleAllyController ally, int diceIndex)
    {
        EnemySkill_SO.DicePowerRange enemyRange = enemy.GetDicePowerRange(diceIndex);
        enemyRange = ApplyEnemyContextualDiceRangeModifiers(enemy, ally, diceIndex, enemyRange);
        EnemySkill_SO.DicePowerRange enemyBaseDisplayRange = enemy.GetEffectiveDisplayedDicePowerRange(enemy.SelectedSkill, diceIndex);
        EnemySkill_SO.DicePowerRange enemyDisplayRange = enemyBaseDisplayRange;
        enemyDisplayRange = ApplyEnemyContextualDiceRangeModifiers(enemy, ally, diceIndex, enemyDisplayRange);
        int enemyBasePower = BattleDiceRollService.RollPower(enemyRange);
        bool isDefenseSkill = IsDefenseSkill(enemy.SelectedSkill, diceIndex);
        int enemyStatusPowerModifier = enemy.GetStatusFlatPowerModifier(isDefenseSkill);
        int queuedNextDicePowerBonus = ConsumePendingEnemyNextDicePowerBonus();
        BattleSingleAttackDiceResolution diceResolution = BattleSingleAttackDiceResolutionService.ResolveWithModifiers(
            enemy.SelectedSkill,
            diceIndex,
            enemyBasePower,
            enemyStatusPowerModifier,
            isDefenseSkill,
            enemy.Speed,
            ally.Speed,
            enemy.GetStatusStacks,
            enemy.GetStatusStacks,
            ally.GetStatusStacks,
            ally.GetStatusStacks,
            relicPowerBonus: queuedNextDicePowerBonus);

        return new SingleAttackDiceInput
        {
            BaseDisplayRange = enemyBaseDisplayRange,
            DisplayRange = enemyDisplayRange,
            AttackResult = diceResolution.AttackResult,
            DiceFx = enemy.GetDicePresentationSettings(diceIndex)
        };
    }

    private IEnumerator PlayResolvedSingleAttackPresentation(
        Transform attackerTransform,
        Transform targetTransform,
        bool isAllyAttack,
        SingleAttackDiceInput diceInput,
        BattleSingleAttackPresentationPlan presentationPlan,
        Action hitAction)
    {
        BattleSingleAttackResult attackResult = diceInput.AttackResult;
        yield return PlaySingleAttackPresentation(
            attackerTransform,
            targetTransform,
            isAllyAttack,
            diceInput.BaseDisplayRange,
            diceInput.DisplayRange,
            attackResult.FinalPower,
            attackResult.DisplayPowerModifier,
            attackResult.FinalPower,
            diceInput.DiceFx,
            presentationPlan.ShouldApplyHit ? hitAction : null,
            presentationPlan.MoveIntoPosition,
            presentationPlan.ReturnToOrigin);
    }

    private Action BuildAllySingleAttackHitAction(
        BattleAllyController ally,
        BattleEnemyController enemy,
        BattleSingleAttackResult attackResult,
        int diceIndex,
        Action<int> onDamageDealt = null)
    {
        return () =>
        {
            int dealtDamage = ApplyAllyHit(ally, enemy, attackResult.HitPower, diceIndex, true, attackResult.HitPowerModifier);
            onDamageDealt?.Invoke(dealtDamage);
        };
    }

    private Action BuildEnemySingleAttackHitAction(
        BattleEnemyController enemy,
        BattleAllyController ally,
        BattleSingleAttackResult attackResult,
        int diceIndex,
        Action<int> onDamageDealt = null)
    {
        return () =>
        {
            int dealtDamage = ApplyEnemyHit(enemy, ally, attackResult.HitPower, diceIndex, true, attackResult.HitPowerModifier);
            onDamageDealt?.Invoke(dealtDamage);
        };
    }

    private void ApplyAllySingleAttackSkillUseEffects(BattleAllyController ally, BattleEnemyController enemy, int diceIndex)
    {
        ApplyAllyTriggeredEffects(ally, enemy, diceIndex, EnemySkill_SO.DiceEffectTrigger.OnSkillUse, 0);
    }

    private void ApplyEnemySingleAttackSkillUseEffects(BattleEnemyController enemy, BattleAllyController ally, int diceIndex)
    {
        ApplyEnemyTriggeredEffects(enemy, ally, diceIndex, EnemySkill_SO.DiceEffectTrigger.OnSkillUse, 0);
    }

    private void TriggerAllDiceHitEffectsIfNeeded(BattleAllyController attacker, BattleEnemyController currentTarget, int diceIndex, SkillHitTracker hitTracker)
    {
        if (attacker == null || currentTarget == null || hitTracker == null || !hitTracker.ShouldTriggerAllDiceHit)
            return;

        ApplyAllyTriggeredEffects(attacker, currentTarget, Mathf.Max(0, diceIndex), EnemySkill_SO.DiceEffectTrigger.OnAllDiceHit, 0);
    }

    private void TriggerAllDiceHitEffectsIfNeeded(BattleEnemyController attacker, BattleAllyController currentTarget, int diceIndex, SkillHitTracker hitTracker)
    {
        if (attacker == null || currentTarget == null || hitTracker == null || !hitTracker.ShouldTriggerAllDiceHit)
            return;

        ApplyEnemyTriggeredEffects(attacker, currentTarget, Mathf.Max(0, diceIndex), EnemySkill_SO.DiceEffectTrigger.OnAllDiceHit, 0);
    }

    private IEnumerator ReturnSingleAttackActorsToOriginIfNeeded(
        bool shouldReturn,
        Transform attackerTransform,
        Vector3 attackerStartPosition,
        Transform targetTransform,
        Vector3 targetStartPosition)
    {
        if (!shouldReturn || battlePresentationSystem == null)
            yield break;

        yield return battlePresentationSystem.ReturnActorsToPositions(
            attackerTransform,
            attackerStartPosition,
            targetTransform,
            targetStartPosition);
    }

    private IEnumerator ResolveClashAction(BattleAllyController ally, BattleEnemyController enemy, SpeedButton allyButton, SpeedButton enemyButton)
    {
        if (ally == null || enemy == null || ally.IsDead || enemy.IsDead)
            yield break;

        pendingAllyNextDicePowerBonus = 0;
        pendingEnemyNextDicePowerBonus = 0;
        ally.ActivateSelectedSkillForButton(allyButton);
        ProcessPlayerSkillUsedRelics(ally, enemy);

        if (!TryConsumeAllySkillRequirement(ally, ally.SelectedSkill, "[鎷肩偣]", true))
        {
            yield return ResolveEnemySingleAttackAction(enemy, ally, enemyButton);
            yield break;
        }

        Vector3 allyStartPosition = ally.transform.position;
        Vector3 enemyStartPosition = enemy.transform.position;

        ally.MarkCurrentRoundSkillUsed();
        enemy.ActivateSelectedSkillForButton(enemyButton);
        TryApplyEnemySkillStageTransition(enemy, enemy.SelectedSkill);
        bool enemyAttackSkillStarted = enemy.SelectedSkill != null && !IsDefenseSkill(enemy.SelectedSkill);

        int allyDiceCount = ally.DiceCount;
        int enemyDiceCount = enemy.DiceCount;
        int clashCount = Mathf.Min(allyDiceCount, enemyDiceCount);
        SkillHitTracker allyHitTracker = new SkillHitTracker();
        SkillHitTracker enemyHitTracker = new SkillHitTracker();

        BattleLog($"[\u62fc\u70b9] {ally.GetDebugDisplayName()}[{ally.GetSelectedSkillDebugSummary()}] vs {enemy.GetDebugDisplayName()}[{enemy.GetSelectedSkillDebugSummary()}]");

        for (int i = 0; i < clashCount; i++)
        {
            if (ally.IsDead || enemy.IsDead)
            {
                yield return ReturnClashActorsToOrigin(ally, allyStartPosition, enemy, enemyStartPosition);
                if (enemyAttackSkillStarted)
                    enemy.ConsumeQueuedAttackSkillPowerBonusAfterAttackSkillUse();
                yield break;
            }

            bool allyDefenseSkill = IsDefenseSkill(ally.SelectedSkill, i);
            bool enemyDefenseSkill = IsDefenseSkill(enemy.SelectedSkill, i);
            PlayerRelicDiceModifier allyRelicModifier = ResolvePlayerRelicDiceModifier(ally, enemy, i, true);
            EnemySkill_SO.DicePowerRange allyRange = ApplyAllyContextualDiceRangeModifiers(ally, enemy, i, ally.GetDicePowerRange(i));
            allyRange = BattleDiceRollService.ApplyRangeBonus(allyRange, ally.ConsumeCurrentRoundMinPowerBonus(), 0);
            allyRange = BattleDiceRollService.ApplyRangeBonus(allyRange, allyRelicModifier.minPowerBonus, allyRelicModifier.maxPowerBonus);
            EnemySkill_SO.DicePowerRange enemyRange = enemy.GetDicePowerRange(i);
            enemyRange = ApplyEnemyContextualDiceRangeModifiers(enemy, ally, i, enemyRange);
            enemyRange = BattleDiceRollService.ApplyRangeBonus(enemyRange, allyRelicModifier.targetMinPowerBonus, 0);
            EnemySkill_SO.DicePowerRange allyBaseDisplayRange = ally.SelectedSkill != null ? ally.GetDisplayedDicePowerRange(ally.SelectedSkill, i) : allyRange;
            EnemySkill_SO.DicePowerRange allyDisplayRange = ApplyAllyContextualDiceRangeModifiers(ally, enemy, i, allyBaseDisplayRange);
            allyDisplayRange = BattleDiceRollService.ApplyRangeBonus(allyDisplayRange, allyRelicModifier.minPowerBonus, allyRelicModifier.maxPowerBonus);
            EnemySkill_SO.DicePowerRange enemyBaseDisplayRange = enemy.GetEffectiveDisplayedDicePowerRange(enemy.SelectedSkill, i);
            EnemySkill_SO.DicePowerRange enemyDisplayRange = enemyBaseDisplayRange;
            enemyDisplayRange = ApplyEnemyContextualDiceRangeModifiers(enemy, ally, i, enemyDisplayRange);
            int allyBasePower = BattleDiceRollService.RollPower(allyRange);
            int enemyBasePower = BattleDiceRollService.RollPower(enemyRange);
            ProcessPlayerDiceRolledRelics(ally, enemy, i, allyBasePower, Mathf.Max(1, allyRange.minPower));
            int allyStatusPowerModifier = ally.GetStatusFlatPowerModifier(allyDefenseSkill);
            int enemyStatusPowerModifier = enemy.GetStatusFlatPowerModifier(enemyDefenseSkill);
            int allySkillUseBonus = BattleDicePowerModifierCollectionService.GetSkillUseBonus(
                ally.SelectedSkill, i, ally.Speed, enemy.Speed, ally.GetStatusStacks, ally.GetStatusStacks, enemy.GetStatusStacks, enemy.GetStatusStacks, enemy.SelectedSkill, allyRelicModifier.powerBonus);
            int enemySkillUseBonus = BattleDicePowerModifierCollectionService.GetSkillUseBonus(
                enemy.SelectedSkill, i, enemy.Speed, ally.Speed, enemy.GetStatusStacks, enemy.GetStatusStacks, ally.GetStatusStacks, ally.GetStatusStacks, ally.SelectedSkill);
            allySkillUseBonus += ConsumePendingAllyNextDicePowerBonus();
            enemySkillUseBonus += ConsumePendingEnemyNextDicePowerBonus();
            int allyClashOnlyBonus = BattleDicePowerModifierCollectionService.GetActionBonus(
                ally.SelectedSkill, i, EnemySkill_SO.DiceEffectTrigger.OnClash, ally.Speed, enemy.Speed, ally.GetStatusStacks, ally.GetStatusStacks, enemy.GetStatusStacks, enemy.GetStatusStacks, enemy.SelectedSkill);
            int enemyClashOnlyBonus = BattleDicePowerModifierCollectionService.GetActionBonus(
                enemy.SelectedSkill, i, EnemySkill_SO.DiceEffectTrigger.OnClash, enemy.Speed, ally.Speed, enemy.GetStatusStacks, enemy.GetStatusStacks, ally.GetStatusStacks, ally.GetStatusStacks, ally.SelectedSkill);
            if (enemy.EnemyID == 24 && enemy.Speed > ally.Speed)
                enemyClashOnlyBonus += 1;
            BattleDicePowerPreparation allyPowerInput = BattleDicePowerPreparationService.Resolve(allyBasePower, allyStatusPowerModifier, 0, allySkillUseBonus, allyClashOnlyBonus);
            BattleDicePowerPreparation enemyPowerInput = BattleDicePowerPreparationService.Resolve(enemyBasePower, enemyStatusPowerModifier, 0, enemySkillUseBonus, enemyClashOnlyBonus);
            int allyBonusPower = allyPowerInput.BonusPower;
            int enemyBonusPower = enemyPowerInput.BonusPower;
            int allyPower = allyPowerInput.FinalPower;
            int enemyPower = enemyPowerInput.FinalPower;
            int allyFollowUpBasePower = allyPowerInput.FollowUpBasePower;
            int enemyFollowUpBasePower = enemyPowerInput.FollowUpBasePower;
            int allyDisplayPowerModifier = allyPowerInput.DisplayPowerModifier;
            int enemyDisplayPowerModifier = enemyPowerInput.DisplayPowerModifier;
            int allyWinAttackBonus = BattleDicePowerModifierCollectionService.GetClashWinBonus(
                ally.SelectedSkill, i, !allyDefenseSkill && !enemyDefenseSkill && allyPower > enemyPower, ally.Speed, enemy.Speed, ally.GetStatusStacks, ally.GetStatusStacks, enemy.GetStatusStacks, enemy.GetStatusStacks, enemy.SelectedSkill);
            int enemyWinAttackBonus = BattleDicePowerModifierCollectionService.GetClashWinBonus(
                enemy.SelectedSkill, i, !allyDefenseSkill && !enemyDefenseSkill && enemyPower > allyPower, enemy.Speed, ally.Speed, enemy.GetStatusStacks, enemy.GetStatusStacks, ally.GetStatusStacks, ally.GetStatusStacks, ally.SelectedSkill);
            BattleClashResult clashResult = BattleClashResultService.Resolve(
                allyPower,
                enemyPower,
                allyDefenseSkill,
                enemyDefenseSkill,
                allyFollowUpBasePower,
                enemyFollowUpBasePower,
                allyWinAttackBonus,
                enemyWinAttackBonus);
            bool hasWinner = clashResult.HasPresentationWinner;
            bool allyWon = clashResult.AllyWonPresentation;
            int allyHitPower = clashResult.AllyHitPower;
            int enemyHitPower = clashResult.EnemyHitPower;
            BattleClashImpactPlan impactPlan = BattleClashImpactPlanService.Resolve(
                clashResult,
                allyPower,
                enemyPower,
                allyBonusPower,
                enemyBonusPower,
                allySkillUseBonus,
                enemySkillUseBonus,
                allyWinAttackBonus,
                enemyWinAttackBonus);
            EnemySkill_SO.DicePresentationSettings allyFx = ally.GetDicePresentationSettings(i);
            EnemySkill_SO.DicePresentationSettings enemyFx = enemy.GetDicePresentationSettings(i);
            EnemySkill_SO.DicePresentationSettings hitFx = allyFx;

            ApplyAllyTriggeredEffects(ally, enemy, i, EnemySkill_SO.DiceEffectTrigger.OnSkillUse, 0);
            ApplyEnemyTriggeredEffects(enemy, ally, i, EnemySkill_SO.DiceEffectTrigger.OnSkillUse, 0);
            ApplyAllyTriggeredEffects(ally, enemy, i, EnemySkill_SO.DiceEffectTrigger.OnClash, 0);
            ApplyEnemyTriggeredEffects(enemy, ally, i, EnemySkill_SO.DiceEffectTrigger.OnClash, 0);

            int restoredDefenseEnergy = RestoreDefenseEnergyForClashIfNeeded(impactPlan, ally);
            if (impactPlan.ApplyAllyHit)
                allyHitTracker.AttackDiceCount++;
            if (impactPlan.ApplyEnemyHit)
                enemyHitTracker.AttackDiceCount++;

            Action resolveImpactAction = () =>
            {
                ResolveClashImpactPlan(impactPlan, ally, enemy, i, allyHitTracker, enemyHitTracker);
            };
            BattleClashPresentationPlan presentationPlan = BattleClashPresentationPlanService.Resolve(clashResult);
            hitFx = presentationPlan.HitFxSource == BattleClashHitFxSource.Enemy ? enemyFx : allyFx;
            string postClashLog = BuildClashPostLog(
                presentationPlan,
                ally,
                enemy,
                allyFollowUpBasePower,
                enemyFollowUpBasePower,
                allyWinAttackBonus,
                enemyWinAttackBonus,
                allyHitPower,
                enemyHitPower,
                allyPower,
                enemyPower,
                restoredDefenseEnergy);

            bool moveIntoPosition = i == 0;
            bool returnToOrigin = i == clashCount - 1;
            yield return PlayClashPresentation(
                ally,
                enemy,
                hasWinner,
                allyWon,
                allyBaseDisplayRange,
                allyDisplayRange,
                enemyBaseDisplayRange,
                enemyDisplayRange,
                allyPower,
                enemyPower,
                allyDisplayPowerModifier,
                enemyDisplayPowerModifier,
                Mathf.Max(allyPower, enemyPower),
                allyWon ? allyHitPower : enemyHitPower,
                allyFx,
                enemyFx,
                hitFx,
                resolveImpactAction,
                moveIntoPosition,
                returnToOrigin);

            BattleLog(BattleClashLogService.FormatClashDice(i, ally.GetDebugDisplayName(), allyBasePower, allyBonusPower, enemy.GetDebugDisplayName(), enemyBasePower, enemyBonusPower));
            BattleLog(postClashLog);
        }

        bool allyRemainingFirst = ally.Speed >= enemy.Speed;
        if (allyRemainingFirst)
        {
            yield return ResolveRemainingAllyClashDice(ally, enemy, clashCount, allyDiceCount, allyHitTracker);
            yield return ResolveRemainingEnemyClashDice(enemy, ally, clashCount, enemyDiceCount, enemyHitTracker);
        }
        else
        {
            yield return ResolveRemainingEnemyClashDice(enemy, ally, clashCount, enemyDiceCount, enemyHitTracker);
            yield return ResolveRemainingAllyClashDice(ally, enemy, clashCount, allyDiceCount, allyHitTracker);
        }

        TriggerAllDiceHitEffectsIfNeeded(ally, enemy, allyDiceCount - 1, allyHitTracker);
        TriggerAllDiceHitEffectsIfNeeded(enemy, ally, enemyDiceCount - 1, enemyHitTracker);

        yield return ReturnClashActorsToOrigin(ally, allyStartPosition, enemy, enemyStartPosition);
        if (enemyAttackSkillStarted)
            enemy.ConsumeQueuedAttackSkillPowerBonusAfterAttackSkillUse();
        yield return ShowTutorialReactionIfNeeded(TutorialBattleEventType.ClashResolved);
    }

    private IEnumerator ResolveRemainingAllyClashDice(BattleAllyController ally, BattleEnemyController enemy, int startIndex, int totalDiceCount, SkillHitTracker hitTracker)
    {
        if (ally == null || enemy == null)
            yield break;

        for (int i = startIndex; i < totalDiceCount; i++)
        {
            if (ally.IsDead || enemy.IsDead)
                yield break;

            SingleAttackDiceInput diceInput = PrepareAllySingleAttackDice(ally, enemy, i);
            BattleSingleAttackResult attackResult = diceInput.AttackResult;
            BattleSingleAttackPresentationPlan presentationPlan = BattleSingleAttackPresentationPlanService.Resolve(
                attackResult,
                i,
                startIndex,
                totalDiceCount,
                true,
                false);
            ApplyAllySingleAttackSkillUseEffects(ally, enemy, i);
            if (!presentationPlan.ShouldPlayPresentation)
            {
                BattleLog(BattleSingleAttackLogService.FormatDefenseOnly(
                    i,
                    "友方余骰单方面",
                    ally.GetDebugDisplayName(),
                    ally.GetSelectedSkillDebugSummary(),
                    attackResult.FormattedPower));
                continue;
            }

            if (hitTracker != null)
                hitTracker.AttackDiceCount++;
            yield return PlayResolvedSingleAttackPresentation(
                ally.transform,
                enemy.transform,
                true,
                diceInput,
                presentationPlan,
                BuildAllySingleAttackHitAction(ally, enemy, attackResult, i, dealtDamage =>
                {
                    if (hitTracker != null)
                        hitTracker.HitCount++;
                }));

            BattleLog(BattleSingleAttackLogService.FormatHit(
                i,
                "友方余骰单方面",
                ally.GetDebugDisplayName(),
                ally.GetSelectedSkillDebugSummary(),
                attackResult.FormattedPower,
                enemy.GetDebugDisplayName(),
                enemy.CurrentHP,
                enemy.MaxHP));
        }
    }

    private IEnumerator ResolveRemainingEnemyClashDice(BattleEnemyController enemy, BattleAllyController ally, int startIndex, int totalDiceCount, SkillHitTracker hitTracker)
    {
        if (enemy == null || ally == null)
            yield break;

        Vector3 enemyStartPosition = enemy.transform.position;
        Vector3 allyStartPosition = ally.transform.position;
        bool needsReturnToOrigin = false;

        for (int i = startIndex; i < totalDiceCount; i++)
        {
            if (enemy.IsDead || ally.IsDead)
            {
                yield return ReturnSingleAttackActorsToOriginIfNeeded(
                    needsReturnToOrigin,
                    enemy.transform,
                    enemyStartPosition,
                    ally.transform,
                    allyStartPosition);
                yield break;
            }

            SingleAttackDiceInput diceInput = PrepareEnemySingleAttackDice(enemy, ally, i);
            BattleSingleAttackResult attackResult = diceInput.AttackResult;
            BattleSingleAttackPresentationPlan presentationPlan = BattleSingleAttackPresentationPlanService.Resolve(
                attackResult,
                i,
                startIndex,
                totalDiceCount,
                false,
                false);
            ApplyEnemySingleAttackSkillUseEffects(enemy, ally, i);
            if (!presentationPlan.ShouldPlayPresentation)
            {
                BattleLog(BattleSingleAttackLogService.FormatDefenseOnly(
                    i,
                    "敌方余骰单方面",
                    enemy.GetDebugDisplayName(),
                    enemy.GetSelectedSkillDebugSummary(),
                    attackResult.FormattedPower));
                continue;
            }

            if (hitTracker != null)
                hitTracker.AttackDiceCount++;
            if (presentationPlan.MoveIntoPosition)
                needsReturnToOrigin = true;
            yield return PlayResolvedSingleAttackPresentation(
                enemy.transform,
                ally.transform,
                false,
                diceInput,
                presentationPlan,
                BuildEnemySingleAttackHitAction(enemy, ally, attackResult, i, dealtDamage =>
                {
                    if (hitTracker != null)
                        hitTracker.HitCount++;
                }));

            BattleLog(BattleSingleAttackLogService.FormatHit(
                i,
                "敌方余骰单方面",
                enemy.GetDebugDisplayName(),
                enemy.GetSelectedSkillDebugSummary(),
                attackResult.FormattedPower,
                ally.GetDebugDisplayName(),
                ally.CurrentHP,
                ally.MaxHP));
        }

        yield return ReturnSingleAttackActorsToOriginIfNeeded(
            needsReturnToOrigin,
            enemy.transform,
            enemyStartPosition,
            ally.transform,
            allyStartPosition);
    }

    private IEnumerator ResolveAllySingleAttackAction(BattleAllyController ally, BattleEnemyController enemy, SpeedButton allyButton)
    {
        if (ally == null || enemy == null || ally.IsDead || enemy.IsDead)
            yield break;

        pendingAllyNextDicePowerBonus = 0;
        ally.ActivateSelectedSkillForButton(allyButton);
        ProcessPlayerSkillUsedRelics(ally, enemy);

        Vector3 allyStartPosition = ally.transform.position;
        Vector3 enemyStartPosition = enemy.transform.position;
        bool needsReturnToOrigin = false;

        if (!TryConsumeAllySkillRequirement(ally, ally.SelectedSkill, "[鍙嬫柟鍗曟柟闈㈡敾鍑籡", true))
            yield break;

        ally.MarkCurrentRoundSkillUsed();
        int allyDiceCount = ally.DiceCount;
        int firstAttackDiceIndex = BattleSingleAttackPresentationStartService.FindFirstAttackDiceIndex(ally.SelectedSkill, allyDiceCount);
        if (firstAttackDiceIndex == BattleSingleAttackPresentationStartService.NoAttackDice)
        {
            BattleLog($"[友方单方面攻击] {ally.GetDebugDisplayName()}[{ally.GetSelectedSkillDebugSummary()}] 为防御技能，未与攻击骰拼点，不会造成拼点/伤害结算，但【使用时】词条会正常生效。");

            for (int i = 0; i < allyDiceCount; i++)
            {
                if (ally.IsDead || enemy.IsDead)
                    yield break;

                ApplyAllySingleAttackSkillUseEffects(ally, enemy, i);
                BattleLog($"第 {i + 1} 段：{ally.GetDebugDisplayName()}[{ally.GetSelectedSkillDebugSummary()}] 为防御技能，本段仅触发【使用时】词条。");
            }

            yield break;
        }

        BattleLog($"[\u53cb\u65b9\u5355\u65b9\u9762\u653b\u51fb] {ally.GetDebugDisplayName()}[{ally.GetSelectedSkillDebugSummary()}] -> {enemy.GetDebugDisplayName()}");
        SkillHitTracker hitTracker = new SkillHitTracker();

        for (int i = 0; i < allyDiceCount; i++)
        {
            if (ally.IsDead || enemy.IsDead)
            {
                yield return ReturnSingleAttackActorsToOriginIfNeeded(
                    needsReturnToOrigin,
                    ally.transform,
                    allyStartPosition,
                    enemy.transform,
                    enemyStartPosition);
                yield break;
            }

            SingleAttackDiceInput diceInput = PrepareAllySingleAttackDice(ally, enemy, i);
            BattleSingleAttackResult attackResult = diceInput.AttackResult;
            BattleSingleAttackPresentationPlan presentationPlan = BattleSingleAttackPresentationPlanService.Resolve(
                attackResult,
                i,
                firstAttackDiceIndex,
                allyDiceCount,
                false,
                false);
            ApplyAllySingleAttackSkillUseEffects(ally, enemy, i);
            if (!presentationPlan.ShouldPlayPresentation)
            {
                BattleLog(BattleSingleAttackLogService.FormatDefenseOnly(
                    i,
                    "友方单方面攻击",
                    ally.GetDebugDisplayName(),
                    ally.GetSelectedSkillDebugSummary(),
                    attackResult.FormattedPower));
                continue;
            }

            if (presentationPlan.MoveIntoPosition)
                needsReturnToOrigin = true;
            hitTracker.AttackDiceCount++;
            yield return PlayResolvedSingleAttackPresentation(
                ally.transform,
                enemy.transform,
                true,
                diceInput,
                presentationPlan,
                BuildAllySingleAttackHitAction(ally, enemy, attackResult, i, dealtDamage =>
                {
                    hitTracker.HitCount++;
                }));

            BattleLog(BuildSingleAttackPostLog(
                presentationPlan,
                i,
                null,
                ally.GetDebugDisplayName(),
                ally.GetSelectedSkillDebugSummary(),
                attackResult,
                enemy.GetDebugDisplayName(),
                enemy.CurrentHP,
                enemy.MaxHP));
        }

        yield return ReturnSingleAttackActorsToOriginIfNeeded(
            needsReturnToOrigin,
            ally.transform,
            allyStartPosition,
            enemy.transform,
            enemyStartPosition);

        TriggerAllDiceHitEffectsIfNeeded(ally, enemy, allyDiceCount - 1, hitTracker);
        yield return ShowTutorialReactionIfNeeded(TutorialBattleEventType.AllyOneSidedAttackResolved);
    }

    private IEnumerator ResolveEnemySingleAttackAction(BattleEnemyController enemy, BattleAllyController targetAlly, SpeedButton enemyButton)
    {
        if (enemy == null || targetAlly == null || enemy.IsDead || targetAlly.IsDead)
            yield break;

        pendingEnemyNextDicePowerBonus = 0;

        Vector3 enemyStartPosition = enemy.transform.position;
        Vector3 allyStartPosition = targetAlly.transform.position;
        bool needsReturnToOrigin = false;

        enemy.ActivateSelectedSkillForButton(enemyButton);
        TryApplyEnemySkillStageTransition(enemy, enemy.SelectedSkill);

        int enemyDiceCount = enemy.DiceCount;
        int firstAttackDiceIndex = BattleSingleAttackPresentationStartService.FindFirstAttackDiceIndex(enemy.SelectedSkill, enemyDiceCount);
        if (firstAttackDiceIndex == BattleSingleAttackPresentationStartService.NoAttackDice)
        {
            BattleLog($"[敌方单方面攻击] {enemy.GetDebugDisplayName()}[{enemy.GetSelectedSkillDebugSummary()}] 为防御技能，未与攻击骰拼点，不会造成拼点/伤害结算，但【使用时】词条会正常生效。");

            for (int i = 0; i < enemyDiceCount; i++)
            {
                if (enemy.IsDead || targetAlly.IsDead)
                    yield break;

                ApplyEnemySingleAttackSkillUseEffects(enemy, targetAlly, i);
                BattleLog($"第 {i + 1} 段：{enemy.GetDebugDisplayName()}[{enemy.GetSelectedSkillDebugSummary()}] 为防御技能，本段仅触发【使用时】词条。");
            }

            yield break;
        }

        BattleLog($"[\u654c\u65b9\u5355\u65b9\u9762\u653b\u51fb] {enemy.GetDebugDisplayName()}[{enemy.GetSelectedSkillDebugSummary()}] -> {targetAlly.GetDebugDisplayName()}");
        bool enemyAttackSkillStarted = true;
        SkillHitTracker hitTracker = new SkillHitTracker();

        for (int i = 0; i < enemyDiceCount; i++)
        {
            if (enemy.IsDead || targetAlly.IsDead)
            {
                yield return ReturnSingleAttackActorsToOriginIfNeeded(
                    needsReturnToOrigin,
                    enemy.transform,
                    enemyStartPosition,
                    targetAlly.transform,
                    allyStartPosition);
                if (enemyAttackSkillStarted)
                    enemy.ConsumeQueuedAttackSkillPowerBonusAfterAttackSkillUse();
                yield break;
            }

            SingleAttackDiceInput diceInput = PrepareEnemySingleAttackDice(enemy, targetAlly, i);
            BattleSingleAttackResult attackResult = diceInput.AttackResult;
            BattleSingleAttackPresentationPlan presentationPlan = BattleSingleAttackPresentationPlanService.Resolve(
                attackResult,
                i,
                firstAttackDiceIndex,
                enemyDiceCount,
                false,
                false);
            ApplyEnemySingleAttackSkillUseEffects(enemy, targetAlly, i);
            if (!presentationPlan.ShouldPlayPresentation)
            {
                BattleLog(BattleSingleAttackLogService.FormatDefenseOnly(
                    i,
                    "敌方单方面攻击",
                    enemy.GetDebugDisplayName(),
                    enemy.GetSelectedSkillDebugSummary(),
                    attackResult.FormattedPower));
                continue;
            }

            if (presentationPlan.MoveIntoPosition)
                needsReturnToOrigin = true;
            hitTracker.AttackDiceCount++;
            yield return PlayResolvedSingleAttackPresentation(
                enemy.transform,
                targetAlly.transform,
                false,
                diceInput,
                presentationPlan,
                BuildEnemySingleAttackHitAction(enemy, targetAlly, attackResult, i, dealtDamage =>
                {
                    hitTracker.HitCount++;
                }));

            BattleLog(BuildSingleAttackPostLog(
                presentationPlan,
                i,
                null,
                enemy.GetDebugDisplayName(),
                enemy.GetSelectedSkillDebugSummary(),
                attackResult,
                targetAlly.GetDebugDisplayName(),
                targetAlly.CurrentHP,
                targetAlly.MaxHP));
        }

        yield return ReturnSingleAttackActorsToOriginIfNeeded(
            needsReturnToOrigin,
            enemy.transform,
            enemyStartPosition,
            targetAlly.transform,
            allyStartPosition);

        if (enemyAttackSkillStarted)
            enemy.ConsumeQueuedAttackSkillPowerBonusAfterAttackSkillUse();
        TriggerAllDiceHitEffectsIfNeeded(enemy, targetAlly, enemyDiceCount - 1, hitTracker);
        yield return ShowTutorialReactionIfNeeded(TutorialBattleEventType.EnemyOneSidedAttackResolved);
    }

    private IEnumerator ReturnClashActorsToOrigin(
        BattleAllyController ally,
        Vector3 allyStartPosition,
        BattleEnemyController enemy,
        Vector3 enemyStartPosition)
    {
        if (battlePresentationSystem == null || ally == null || enemy == null)
            yield break;

        yield return battlePresentationSystem.ReturnActorsToPositions(
            ally.transform,
            allyStartPosition,
            enemy.transform,
            enemyStartPosition);
    }

    private int ApplyAllyHit(BattleAllyController attacker, BattleEnemyController target, int baseDamage, int diceIndex, bool isSingleAttack = false, int powerModifierAmount = 0)
    {
        if (attacker == null || target == null || attacker.IsDead)
            return 0;

        int modifiedDamage = ResolveAllyHitDamage(attacker, target, diceIndex, baseDamage);
        if (BattlePuppetPassiveService.TryApplyPuppetMasterDamageRedirect(
            target,
            modifiedDamage,
            GetLivingEnemies(),
            count => UnityEngine.Random.Range(0, count),
            out int redirectedDamage,
            BattleLog))
        {
            ApplyAllyHitOutcome(attacker, target, diceIndex, redirectedDamage, isSingleAttack);
            return redirectedDamage;
        }

        int dealtDamage = target.TakeDamage(modifiedDamage, powerModifierAmount, baseDamage);
        ApplyAllyHitOutcome(attacker, target, diceIndex, dealtDamage, isSingleAttack);
        return dealtDamage;
    }

    private int ApplyEnemyHit(BattleEnemyController attacker, BattleAllyController target, int baseDamage, int diceIndex, bool isSingleAttack = false, int powerModifierAmount = 0)
    {
        if (attacker == null || target == null || attacker.IsDead)
            return 0;

        int incomingDamage = ResolveEnemyHitDamage(attacker, target, diceIndex, baseDamage, isSingleAttack);
        int dealtDamage = target.TakeDamage(incomingDamage, powerModifierAmount, baseDamage);
        ApplyEnemyHitOutcome(attacker, target, diceIndex, dealtDamage, isSingleAttack);
        return dealtDamage;
    }

    private void ApplyAllyHitOutcome(
        BattleAllyController attacker,
        BattleEnemyController target,
        int diceIndex,
        int dealtDamage,
        bool isSingleAttack)
    {
        BattleHitResolutionFacade.Resolve(
            dealtDamage,
            true,
            () => ShowZeroDamageHitFeedback(target.transform, true, target.LastDamageModifierRatioPercent),
            () => ApplyAllySuccessfulHitEffects(attacker, target, diceIndex, dealtDamage, isSingleAttack),
            () => ApplyAllyMissEffects(attacker, target, diceIndex, dealtDamage));
    }

    private void ApplyEnemyHitOutcome(
        BattleEnemyController attacker,
        BattleAllyController target,
        int diceIndex,
        int dealtDamage,
        bool isSingleAttack)
    {
        BattleHitResolutionFacade.Resolve(
            dealtDamage,
            true,
            () => ShowZeroDamageHitFeedback(target.transform, false, target.LastDamageModifierRatioPercent),
            () => ApplyEnemySuccessfulHitEffects(attacker, target, diceIndex, dealtDamage, isSingleAttack),
            () => ApplyEnemyMissEffects(attacker, target, diceIndex, dealtDamage));
    }

    private int ResolveAllyHitDamage(BattleAllyController attacker, BattleEnemyController target, int diceIndex, int baseDamage)
    {
        BattleAllyHitDamagePipelinePlan plan = BattleAllyHitDamagePipelinePlanService.Resolve();
        return BattleAllyHitDamagePipelineDispatcher.Dispatch(
            baseDamage,
            plan,
            damage => ApplyPlayerRelicOutgoingDamageModifiers(attacker, target, diceIndex, damage));
    }

    private int ResolveEnemyHitDamage(BattleEnemyController attacker, BattleAllyController target, int diceIndex, int baseDamage, bool isSingleAttack)
    {
        BattleEnemyHitDamagePipelinePlan plan = BattleEnemyHitDamagePipelinePlanService.Resolve(isSingleAttack);
        int resolvedDamage = BattleEnemyHitDamagePipelineDispatcher.Dispatch(
            baseDamage,
            plan,
            ApplyEnemyCalamityDamageModifiers,
            damage => ApplyPlayerRelicIncomingAttackDamageModifiers(attacker, target, diceIndex, damage),
            damage => ApplyPlayerRelicIncomingSingleAttackDamageModifiers(attacker, target, diceIndex, damage));
        ResolveBuiltinStatus("target", "标记", out BattleStatus_SO markStatus, out _, out _);
        return Mathf.Max(0, resolvedDamage + BattlePuppetPassiveService.ResolveSoullessPuppetUnmarkedTargetPowerPenalty(attacker, target, markStatus));
    }

    private void ApplyAllyMissEffects(BattleAllyController attacker, BattleEnemyController target, int diceIndex, int dealtDamage)
    {
        BattleMissEffectPlan plan = BattleMissEffectPlanService.Resolve();
        BattleMissEffectDispatcher.Dispatch(
            plan,
            dealtDamage,
            (trigger, damage) => ApplyAllyTriggeredEffects(attacker, target, diceIndex, trigger, damage));
    }

    private void ApplyEnemyMissEffects(BattleEnemyController attacker, BattleAllyController target, int diceIndex, int dealtDamage)
    {
        BattleMissEffectPlan plan = BattleMissEffectPlanService.Resolve();
        BattleMissEffectDispatcher.Dispatch(
            plan,
            dealtDamage,
            (trigger, damage) => ApplyEnemyTriggeredEffects(attacker, target, diceIndex, trigger, damage));
    }

    private void ShowZeroDamageHitFeedback(Transform targetTransform, bool isPlayerAttack, int modifierRatioPercent)
    {
        if (targetTransform == null)
            return;

        BattleZeroDamageFeedbackPlan plan = BattleZeroDamageFeedbackPlanService.Resolve(
            BattleActorVisualUtility.GetActorCenterWorld(targetTransform),
            isPlayerAttack,
            modifierRatioPercent);

        BattleZeroDamageFeedbackDispatcher.Dispatch(
            plan,
            (position, damage, playerAttack, ratioPercent) =>
                battleFeedbackSystem?.ShowDamageResultPopup(position, damage, playerAttack, ratioPercent));
    }

    private void ApplyAllySuccessfulHitEffects(
        BattleAllyController attacker,
        BattleEnemyController target,
        int diceIndex,
        int dealtDamage,
        bool isSingleAttack)
    {
        BattleSuccessfulHitRelicPlan plan = BattleSuccessfulHitRelicPlanService.ResolveAllyHit(target.IsDead);
        BattleSuccessfulHitSkillEffectPlan skillPlan = BattleSuccessfulHitSkillEffectPlanService.Resolve(isSingleAttack);

        BattleSuccessfulHitEffectDispatcher.DispatchAllyHit(
            plan,
            skillPlan,
            () => ApplyPlayerHitRelics(attacker, target, diceIndex, dealtDamage),
            () => ApplyEnemyKilledByPlayerRelics(attacker, target, diceIndex, dealtDamage),
            trigger => ApplyAllyImmediateTriggeredEffects(attacker, target, diceIndex, trigger, dealtDamage),
            () => ApplyReactiveOnHitStatusEffects(target),
            trigger => ApplyAllyStatusEffectsOnly(attacker, target, diceIndex, trigger, false));
    }

    private void ApplyEnemySuccessfulHitEffects(
        BattleEnemyController attacker,
        BattleAllyController target,
        int diceIndex,
        int dealtDamage,
        bool isSingleAttack)
    {
        BattleSuccessfulHitRelicPlan plan = BattleSuccessfulHitRelicPlanService.ResolveEnemyHit(isSingleAttack);
        BattleSuccessfulHitSkillEffectPlan skillPlan = BattleSuccessfulHitSkillEffectPlanService.Resolve(isSingleAttack);

        BattleSuccessfulHitEffectDispatcher.DispatchEnemyHit(
            plan,
            skillPlan,
            () => ApplyPlayerTookSingleAttackRelics(attacker, target, diceIndex, dealtDamage),
            trigger => ApplyEnemyImmediateTriggeredEffects(attacker, target, diceIndex, trigger, dealtDamage),
            () => ApplyReactiveOnHitStatusEffects(target),
            trigger => ApplyEnemyStatusEffectsOnly(attacker, target, diceIndex, trigger, false));
    }

    private int ApplyPlayerRelicIncomingAttackDamageModifiers(
        BattleEnemyController attacker,
        BattleAllyController target,
        int diceIndex,
        int incomingDamage)
    {
        BattleEncounterData activeEncounter = BattleEncounterRuntime.ActiveEncounter;

        RelicExecutionContext context = BattleDamageModifierContextFactory.CreateIncomingAttack(
            attacker != null ? attacker.SelectedSkill : null,
            diceIndex,
            incomingDamage,
            attacker != null ? attacker.Speed : 0,
            target != null ? target.Speed : 0,
            attacker,
            target);
        if (!BattleRelicBridge.Process(activeEncounter, context, (encounter, relicContext) => RelicTriggerDispatcher.ProcessBattleRelics(encounter, relicContext)))
            return Mathf.Max(0, incomingDamage);

        return BattleDamageModifierResolutionDispatcher.ResolveIncomingAndDispatchFeedback(
            incomingDamage,
            context,
            flushContext => BattleRelicEffectRunner.FlushFeedback(flushContext, BattleLog));
    }

    private int ApplyPlayerRelicOutgoingDamageModifiers(
        BattleAllyController attacker,
        BattleEnemyController target,
        int diceIndex,
        int outgoingDamage)
    {
        if (outgoingDamage <= 0 || attacker == null || target == null)
            return Mathf.Max(0, outgoingDamage);

        BattleEncounterData activeEncounter = BattleEncounterRuntime.ActiveEncounter;
        int calamityModifiedDamage = CalamitySystem.ApplyAllyDamageModifiers(outgoingDamage, activeEncounter != null ? activeEncounter.activeCalamities : null);
        calamityModifiedDamage = CalamitySystem.ApplyEnemyIncomingDamageModifiers(calamityModifiedDamage, activeEncounter != null ? activeEncounter.activeCalamities : null);

        RelicExecutionContext context = BattleDamageModifierContextFactory.CreateOutgoing(
            attacker.SelectedSkill,
            diceIndex,
            calamityModifiedDamage,
            attacker,
            target);
        if (!BattleRelicBridge.Process(activeEncounter, context, (encounter, relicContext) => RelicTriggerDispatcher.ProcessBattleRelics(encounter, relicContext)))
            return Mathf.Max(0, calamityModifiedDamage);

        return BattleDamageModifierResolutionDispatcher.ResolveOutgoingAndDispatchFeedback(
            calamityModifiedDamage,
            context,
            flushContext => BattleRelicEffectRunner.FlushFeedback(flushContext, BattleLog));
    }

    private void ApplyPlayerDeathRelics(BattleAllyController defeatedAlly, BattleAllyController[] allies, BattleEnemyController[] enemies)
    {
        BattleEncounterData activeEncounter = BattleEncounterRuntime.ActiveEncounter;
        if (defeatedAlly == null)
            return;

        RelicExecutionContext context = BattleDeathRelicContextFactory.CreatePlayerDeath(defeatedAlly, allies, enemies);
        BattleRelicBridge.Process(activeEncounter, context, (encounter, relicContext) => BattleRelicEffectRunner.Process(encounter, relicContext, BattleLog));
    }

    private int ApplyPlayerRelicIncomingSingleAttackDamageModifiers(
        BattleEnemyController attacker,
        BattleAllyController target,
        int diceIndex,
        int incomingDamage)
    {
        BattleEncounterData activeEncounter = BattleEncounterRuntime.ActiveEncounter;

        RelicExecutionContext context = BattleDamageModifierContextFactory.CreateIncomingSingleAttack(
            attacker != null ? attacker.SelectedSkill : null,
            diceIndex,
            incomingDamage,
            attacker != null ? attacker.Speed : 0,
            target != null ? target.Speed : 0,
            attacker,
            target);
        if (!BattleRelicBridge.Process(activeEncounter, context, (encounter, relicContext) => RelicTriggerDispatcher.ProcessBattleRelics(encounter, relicContext)))
            return Mathf.Max(0, incomingDamage);

        return BattleDamageModifierResolutionDispatcher.ResolveIncomingAndDispatchFeedback(
            incomingDamage,
            context,
            flushContext => BattleRelicEffectRunner.FlushFeedback(flushContext, BattleLog));
    }

    private void ApplyPlayerTookSingleAttackRelics(
        BattleEnemyController attacker,
        BattleAllyController target,
        int diceIndex,
        int dealtDamage)
    {
        BattleEncounterData activeEncounter = BattleEncounterRuntime.ActiveEncounter;
        if (attacker == null || target == null)
            return;

        RelicExecutionContext context = BattleHitRelicContextFactory.CreatePlayerTookSingleAttack(
            attacker.SelectedSkill,
            diceIndex,
            dealtDamage,
            attacker.Speed,
            target.Speed,
            currentRound,
            BattleRelicEventKeyService.BuildSingleAttackTakenKey(attacker, target, currentRound, diceIndex),
            attacker,
            target);
        BattleRelicBridge.Process(activeEncounter, context, (encounter, relicContext) => BattleRelicEffectRunner.Process(encounter, relicContext, BattleLog));
    }

    private void ApplyPlayerHitRelics(
        BattleAllyController attacker,
        BattleEnemyController target,
        int diceIndex,
        int dealtDamage)
    {
        BattleEncounterData activeEncounter = BattleEncounterRuntime.ActiveEncounter;
        if (attacker == null || target == null)
            return;

        RelicExecutionContext context = BattleHitRelicContextFactory.CreatePlayerHitEnemy(
            attacker.SelectedSkill,
            diceIndex,
            dealtDamage,
            attacker.Speed,
            target.Speed,
            currentRound,
            BattleRelicEventKeyService.BuildAllySkillUseKey(attacker, currentRound),
            attacker,
            target);
        BattleRelicBridge.Process(activeEncounter, context, (encounter, relicContext) => BattleRelicEffectRunner.Process(encounter, relicContext, BattleLog));
    }

    private void ApplyEnemyKilledByPlayerRelics(
        BattleAllyController attacker,
        BattleEnemyController target,
        int diceIndex,
        int dealtDamage)
    {
        BattleEncounterData activeEncounter = BattleEncounterRuntime.ActiveEncounter;
        if (attacker == null || target == null || dealtDamage <= 0)
            return;

        RelicExecutionContext context = BattleHitRelicContextFactory.CreateEnemyKilledByPlayer(
            attacker.SelectedSkill,
            diceIndex,
            dealtDamage,
            attacker,
            target);
        BattleRelicBridge.Process(activeEncounter, context, (encounter, relicContext) => BattleRelicEffectRunner.Process(encounter, relicContext, BattleLog));
    }

    private void ApplyPlayerDefenseClashWinRelics(BattleAllyController ally, BattleEnemyController enemy, int diceIndex)
    {
        BattleEncounterData activeEncounter = BattleEncounterRuntime.ActiveEncounter;
        if (ally == null)
            return;

        RelicExecutionContext context = BattleHitRelicContextFactory.CreatePlayerDefenseClashWin(
            ally.SelectedSkill,
            diceIndex,
            currentRound,
            ally,
            enemy);
        BattleRelicBridge.Process(activeEncounter, context, (encounter, relicContext) => BattleRelicEffectRunner.Process(encounter, relicContext, BattleLog));
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

    private void ApplyEnemyDeathCalamityStatusGains(BattleEnemyController defeatedEnemy)
    {
        if (defeatedEnemy == null || activeRoundEnemies == null)
            return;

        BattleEncounterData activeEncounter = BattleEncounterRuntime.ActiveEncounter;
        List<CalamityBuiltInEffectEntry> effects = CalamitySystem.GetEnemyDeathStatusEffects(activeEncounter != null ? activeEncounter.activeCalamities : null);
        if (effects.Count == 0)
            return;

        for (int i = 0; i < activeRoundEnemies.Length; i++)
        {
            BattleEnemyController enemy = activeRoundEnemies[i];
            if (enemy == null || enemy.IsDead || enemy == defeatedEnemy)
                continue;

            for (int effectIndex = 0; effectIndex < effects.Count; effectIndex++)
            {
                CalamityBuiltInEffectEntry effect = effects[effectIndex];
                if (effect == null || effect.statusData == null || effect.effectAmount <= 0)
                    continue;

                if (!enemy.AddStatus(effect.statusData, effect.statusData.StatusId, effect.statusData.DisplayName, effect.effectAmount))
                    continue;

                int stacks = enemy.GetStatusStacks(effect.statusData);
                BattleLog($"灾厄效果：{defeatedEnemy.GetDebugDisplayName()} 死亡后，{enemy.GetDebugDisplayName()} 获得 {effect.effectAmount} 层 {effect.statusData.DisplayName}，当前 {effect.statusData.DisplayName} {stacks} 层");
            }
        }
    }

    private void ApplyEnemyRoundEndCalamityHealing(BattleEnemyController[] enemies)
    {
        if (enemies == null)
            return;

        BattleEncounterData activeEncounter = BattleEncounterRuntime.ActiveEncounter;
        int healAmount = CalamitySystem.ResolveEnemyRoundEndHealAmount(activeEncounter != null ? activeEncounter.activeCalamities : null);
        if (healAmount <= 0)
            return;

        for (int i = 0; i < enemies.Length; i++)
        {
            BattleEnemyController enemy = enemies[i];
            if (enemy == null || enemy.IsDead)
                continue;

            int healed = enemy.Heal(healAmount);
            if (healed > 0)
                BattleLog($"灾厄效果：回合结束时 {enemy.GetDebugDisplayName()} 恢复 {healed} 点生命");
        }
    }

    private bool ApplyEnemyStatusGains(
        BattleEnemyController enemy,
        List<EnemyData_SO.StatusGainEntry> entries,
        Func<string, int, int, string> buildLogMessage)
    {
        if (enemy == null || enemy.IsDead || entries == null || entries.Count == 0)
            return false;

        return BattleEnemyStatusGainDispatcher.Dispatch(
            entries,
            entry => enemy.AddStatus(entry.statusData, entry.statusData.StatusId, entry.statusData.DisplayName, entry.stackAmount),
            entry => enemy.GetStatusStacks(entry.statusData),
            (entry, currentStacks) =>
            {
                if (buildLogMessage != null)
                    BattleLog(buildLogMessage(entry.statusData.DisplayName, entry.stackAmount, currentStacks));
            },
            entry => enemy.AddPendingRoundStartSpeedBonusFromStatus(entry.statusData, entry.stackAmount));
    }

    private void TryLogEnemyStatusDrivenStageTransition(BattleEnemyController enemy, BattleAllyController[] allies, BattleEnemyController[] enemies)
    {
        if (!BattleStatusFlowTargetService.ShouldAttemptStatusDrivenStageTransition(enemy, target => target.IsDead, enemy != null && enemy.AppliedPendingStageTransitionThisRoundStart))
            return;

        int previousStage = enemy.CurrentStageIndex;
        if (enemy.TryApplyStatusDrivenStageTransition(allies, enemies))
            BattleLog(BattleStatusFlowTargetService.BuildStatusDrivenStageTransitionLog(enemy.GetDebugDisplayName(), previousStage, enemy.CurrentStageIndex));
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

            BattleLog(BattleStatusTickLogService.BuildDamageLog(targetName, result.displayName, result.damage, result.stacks, currentHP, maxHP));
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
                BattleDamageEffectDispatcher.DispatchDamage(
                    effect.damageAmount,
                    null,
                    (modifiedDamage, _) => attacker.TakeDamage(modifiedDamage),
                    modifiedDamage => BattleLog($"\u8bcd\u6761\u89e6\u53d1\uff1a{attacker.GetDebugDisplayName()}[{BuildEffectLabel(effect, i)}] \u5bf9 \u81ea\u5df1 \u9020\u6210 {modifiedDamage} \u989d\u5916\u4f24\u5bb3\uff0c\u5269\u4f59HP {attacker.CurrentHP}/{attacker.MaxHP}"));
                continue;
            }

            List<BattleEnemyController> targets = SelectEnemyTargets(effect.targetMode, currentTarget, opponents, effect.randomTargetCount);
            for (int j = 0; j < targets.Count; j++)
            {
                BattleEnemyController target = targets[j];
                if (target == null || target.IsDead)
                    continue;

                BattleDamageEffectDispatcher.DispatchDamage(
                    effect.damageAmount,
                    damage => ApplyPlayerRelicOutgoingDamageModifiers(attacker, target, diceIndex, damage),
                    (modifiedDamage, originalDamage) => target.TakeDamage(modifiedDamage, 0, originalDamage),
                    modifiedDamage => BattleLog($"\u8bcd\u6761\u89e6\u53d1\uff1a{attacker.GetDebugDisplayName()}[{BuildEffectLabel(effect, i)}] \u5bf9 {target.GetDebugDisplayName()} \u9020\u6210 {modifiedDamage} \u989d\u5916\u4f24\u5bb3\uff0c\u5269\u4f59HP {target.CurrentHP}/{target.MaxHP}"));
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
                BattleDamageEffectDispatcher.DispatchDamage(
                    effect.damageAmount,
                    null,
                    (modifiedDamage, _) => attacker.TakeDamage(modifiedDamage),
                    modifiedDamage => BattleLog($"\u8bcd\u6761\u89e6\u53d1\uff1a{attacker.GetDebugDisplayName()}[{BuildEffectLabel(effect, i)}] \u5bf9 \u81ea\u5df1 \u9020\u6210 {modifiedDamage} \u989d\u5916\u4f24\u5bb3\uff0c\u5269\u4f59HP {attacker.CurrentHP}/{attacker.MaxHP}"));
                continue;
            }

            List<BattleAllyController> targets = SelectAllyTargets(effect.targetMode, currentTarget, opponents, effect.randomTargetCount);
            for (int j = 0; j < targets.Count; j++)
            {
                BattleAllyController target = targets[j];
                if (target == null || target.IsDead)
                    continue;

                BattleDamageEffectDispatcher.DispatchDamage(
                    effect.damageAmount,
                    ApplyEnemyCalamityDamageModifiers,
                    (modifiedDamage, originalDamage) => target.TakeDamage(modifiedDamage, 0, originalDamage),
                    modifiedDamage => BattleLog($"\u8bcd\u6761\u89e6\u53d1\uff1a{attacker.GetDebugDisplayName()}[{BuildEffectLabel(effect, i)}] \u5bf9 {target.GetDebugDisplayName()} \u9020\u6210 {modifiedDamage} \u989d\u5916\u4f24\u5bb3\uff0c\u5269\u4f59HP {target.CurrentHP}/{target.MaxHP}"));
            }
        }
    }

    private void ApplyAllyTriggeredEffects(BattleAllyController attacker, BattleEnemyController currentTarget, int diceIndex, EnemySkill_SO.DiceEffectTrigger trigger, int dealtDamage)
    {
        BattleTriggeredEffectDispatcher.Dispatch(
            BattleTriggeredEffectPlanService.Resolve(),
            step =>
            {
                switch (step)
                {
                    case BattleTriggeredEffectStep.ImmediateEffects:
                        ApplyAllyImmediateTriggeredEffects(attacker, currentTarget, diceIndex, trigger, dealtDamage);
                        break;
                    case BattleTriggeredEffectStep.NonConditionalStatus:
                        ApplyAllyStatusEffectsOnly(attacker, currentTarget, diceIndex, trigger, false);
                        break;
                }
            });
    }

    private void ApplyEnemyTriggeredEffects(BattleEnemyController attacker, BattleAllyController currentTarget, int diceIndex, EnemySkill_SO.DiceEffectTrigger trigger, int dealtDamage)
    {
        BattleTriggeredEffectDispatcher.Dispatch(
            BattleTriggeredEffectPlanService.Resolve(),
            step =>
            {
                switch (step)
                {
                    case BattleTriggeredEffectStep.ImmediateEffects:
                        ApplyEnemyImmediateTriggeredEffects(attacker, currentTarget, diceIndex, trigger, dealtDamage);
                        break;
                    case BattleTriggeredEffectStep.NonConditionalStatus:
                        ApplyEnemyStatusEffectsOnly(attacker, currentTarget, diceIndex, trigger, false);
                        break;
                }
            });
    }

    private void ApplyAllyImmediateTriggeredEffects(BattleAllyController attacker, BattleEnemyController currentTarget, int diceIndex, EnemySkill_SO.DiceEffectTrigger trigger, int dealtDamage)
    {
        BattleImmediateEffectDispatcher.Dispatch(
            BattleImmediateEffectPlanService.ResolveAlly(),
            step =>
            {
                switch (step)
                {
                    case BattleImmediateEffectStep.Damage:
                        ApplyAllyDamageEffects(attacker, currentTarget, diceIndex, trigger);
                        break;
                    case BattleImmediateEffectStep.StatusBurst:
                        ApplyAllyStatusBurstEffects(attacker, currentTarget, diceIndex, trigger);
                        break;
                    case BattleImmediateEffectStep.Healing:
                        ApplyAllyHealingEffects(attacker, currentTarget, diceIndex, trigger, dealtDamage);
                        break;
                    case BattleImmediateEffectStep.BuiltIn:
                        ApplyAllyBuiltInEffects(attacker, currentTarget, diceIndex, trigger);
                        break;
                    case BattleImmediateEffectStep.ConditionalStatus:
                        ApplyAllyStatusEffectsOnly(attacker, currentTarget, diceIndex, trigger, true);
                        break;
                    case BattleImmediateEffectStep.StatusStackOperation:
                        ApplyAllyStatusStackOperationEffects(attacker, currentTarget, diceIndex, trigger);
                        break;
                }
            });
    }

    private void ApplyEnemyImmediateTriggeredEffects(BattleEnemyController attacker, BattleAllyController currentTarget, int diceIndex, EnemySkill_SO.DiceEffectTrigger trigger, int dealtDamage)
    {
        BattleImmediateEffectDispatcher.Dispatch(
            BattleImmediateEffectPlanService.ResolveEnemy(),
            step =>
            {
                switch (step)
                {
                    case BattleImmediateEffectStep.Damage:
                        ApplyEnemyDamageEffects(attacker, currentTarget, diceIndex, trigger);
                        break;
                    case BattleImmediateEffectStep.StatusBurst:
                        ApplyEnemyStatusBurstEffects(attacker, currentTarget, diceIndex, trigger);
                        break;
                    case BattleImmediateEffectStep.Healing:
                        ApplyEnemyHealingEffects(attacker, currentTarget, diceIndex, trigger, dealtDamage);
                        break;
                    case BattleImmediateEffectStep.BuiltIn:
                        ApplyEnemyBuiltInEffects(attacker, currentTarget, diceIndex, trigger);
                        break;
                    case BattleImmediateEffectStep.ConditionalStatus:
                        ApplyEnemyStatusEffectsOnly(attacker, currentTarget, diceIndex, trigger, true);
                        break;
                    case BattleImmediateEffectStep.StatusStackOperation:
                        ApplyEnemyStatusStackOperationEffects(attacker, currentTarget, diceIndex, trigger);
                        break;
                    case BattleImmediateEffectStep.Summon:
                        ApplyEnemySummonEffects(attacker, diceIndex, trigger);
                        break;
                    case BattleImmediateEffectStep.StageTransition:
                        ApplyEnemyStageTransitionEffects(attacker, diceIndex, trigger);
                        break;
                }
            });

        if (trigger == EnemySkill_SO.DiceEffectTrigger.OnSkillUse)
        {
            ApplyBloodSpiderKinFeastEffects(attacker);
            ApplyWanderingPhantomBlendIntoShadowEffects(attacker);
        }
    }

    private void ApplyAllyStatusEffectsOnly(BattleAllyController attacker, BattleEnemyController currentTarget, int diceIndex, EnemySkill_SO.DiceEffectTrigger trigger, bool conditionalOnly)
    {
        EnemySkill_SO skill = attacker != null ? attacker.SelectedSkill : null;
        if (skill == null)
            return;

        EnemySkill_SO.DiceEffectSettings settings = skill.GetDiceEffectSettings(diceIndex);
        if (settings.statusEffects == null || settings.statusEffects.Length == 0)
            return;

        List<BattleEnemyController> opponents = GetLivingEnemies();
        IReadOnlyList<BattleIndexedEffect<EnemySkill_SO.StatusEffectEntry>> eligibleEffects = BattleSkillStatusEffectResolutionService.BuildEligibleStatusEffects(
            settings.statusEffects,
            trigger,
            conditionalOnly);

        for (int i = 0; i < eligibleEffects.Count; i++)
        {
            BattleIndexedEffect<EnemySkill_SO.StatusEffectEntry> indexedEffect = eligibleEffects[i];
            EnemySkill_SO.StatusEffectEntry effect = indexedEffect.Effect;
            int effectIndex = indexedEffect.Index;

            if (effect.targetMode == EnemySkill_SO.DamageTargetMode.Self)
            {
                if (!IsAllyStatusEffectConditionMet(attacker, attacker, effect))
                    continue;
                if (!BattleStatusResolutionService.TryResolve(effect.statusData, effect.statusType, out BattleResolvedStatus resolvedStatus))
                    continue;
                int adjustedStacks = ResolvePlayerRelicAdjustedStatusStacks(resolvedStatus.StatusData, resolvedStatus.StatusId, resolvedStatus.StatusName, effect.stackAmount, attacker, attacker);
                if (!BattleStatusEffectApplicationService.TryBuildPlan(effect, adjustedStacks, out BattleStatusEffectApplicationPlan statusPlan))
                    continue;
                BattleStatusApplicationPlan plan = statusPlan.ApplicationPlan;
                if (plan.IsDelayed)
                {
                    if (attacker.QueueStatusForRoundStart(statusPlan.StatusData, statusPlan.StatusId, statusPlan.StatusName, plan.StackAmount, plan.DelayRounds, plan.RepeatCount))
                        BattleLog(BuildDelayedStatusLog(attacker.GetDebugDisplayName(), "\u81ea\u5df1", effect, effectIndex, statusPlan.StatusName, plan.DelayRounds, plan.RepeatCount, plan.StackAmount));
                }
                else
                {
                    if (attacker.AddStatus(statusPlan.StatusData, statusPlan.StatusId, statusPlan.StatusName, plan.StackAmount))
                    {
                        int stacks = statusPlan.StatusData != null ? attacker.GetStatusStacks(statusPlan.StatusData) : attacker.GetStatusStacks(statusPlan.StatusId);
                        BattleLog(BattleStatusEffectApplicationService.BuildImmediateAppliedLog(attacker.GetDebugDisplayName(), "\u81ea\u5df1", BuildStatusEffectLabel(effect, effectIndex), statusPlan.StatusName, plan.StackAmount, stacks));
                    }
                }
                continue;
            }

            bool useCurrentTargetCondition = BattleSkillStatusEffectResolutionService.ShouldEvaluateConditionAgainstCurrentTarget(effect.targetMode);
            if (useCurrentTargetCondition && !IsAllyStatusEffectConditionMet(attacker, currentTarget, effect))
                continue;

            List<BattleEnemyController> targets = SelectEnemyTargets(effect.targetMode, currentTarget, opponents, effect.randomTargetCount);
            for (int j = 0; j < targets.Count; j++)
            {
                BattleEnemyController target = targets[j];
                if (target == null || target.IsDead)
                    continue;
                if (!useCurrentTargetCondition && !IsAllyStatusEffectConditionMet(attacker, target, effect))
                    continue;

                ApplyResolvedStatusEffect(attacker, target, effect, effectIndex);
            }
        }
    }

    private void ApplyEnemyStatusEffectsOnly(BattleEnemyController attacker, BattleAllyController currentTarget, int diceIndex, EnemySkill_SO.DiceEffectTrigger trigger, bool conditionalOnly)
    {
        EnemySkill_SO skill = attacker != null ? attacker.SelectedSkill : null;
        if (skill == null)
            return;

        EnemySkill_SO.DiceEffectSettings settings = skill.GetDiceEffectSettings(diceIndex);
        if (settings.statusEffects == null || settings.statusEffects.Length == 0)
            return;

        List<BattleAllyController> opponents = GetLivingAllies();
        IReadOnlyList<BattleIndexedEffect<EnemySkill_SO.StatusEffectEntry>> eligibleEffects = BattleSkillStatusEffectResolutionService.BuildEligibleStatusEffects(
            settings.statusEffects,
            trigger,
            conditionalOnly);

        for (int i = 0; i < eligibleEffects.Count; i++)
        {
            BattleIndexedEffect<EnemySkill_SO.StatusEffectEntry> indexedEffect = eligibleEffects[i];
            EnemySkill_SO.StatusEffectEntry effect = indexedEffect.Effect;
            int effectIndex = indexedEffect.Index;

            if (effect.targetMode == EnemySkill_SO.DamageTargetMode.Self)
            {
                if (!IsEnemyStatusEffectConditionMet(attacker, attacker, effect))
                    continue;
                if (!BattleStatusEffectApplicationService.TryBuildPlan(effect, effect.stackAmount, out BattleStatusEffectApplicationPlan statusPlan))
                    continue;
                BattleStatusApplicationPlan plan = statusPlan.ApplicationPlan;
                if (plan.IsDelayed)
                {
                    if (attacker.QueueStatusForRoundStart(statusPlan.StatusData, statusPlan.StatusId, statusPlan.StatusName, plan.StackAmount, plan.DelayRounds, plan.RepeatCount))
                        BattleLog(BuildDelayedStatusLog(attacker.GetDebugDisplayName(), "\u81ea\u5df1", effect, effectIndex, statusPlan.StatusName, plan.DelayRounds, plan.RepeatCount, plan.StackAmount));
                }
                else
                {
                    if (attacker.AddStatus(statusPlan.StatusData, statusPlan.StatusId, statusPlan.StatusName, plan.StackAmount))
                    {
                        int stacks = statusPlan.StatusData != null ? attacker.GetStatusStacks(statusPlan.StatusData) : attacker.GetStatusStacks(statusPlan.StatusId);
                        BattleLog(BattleStatusEffectApplicationService.BuildImmediateAppliedLog(attacker.GetDebugDisplayName(), "\u81ea\u5df1", BuildStatusEffectLabel(effect, effectIndex), statusPlan.StatusName, plan.StackAmount, stacks));
                    }
                }
                continue;
            }

            bool useCurrentTargetCondition = BattleSkillStatusEffectResolutionService.ShouldEvaluateConditionAgainstCurrentTarget(effect.targetMode);
            if (useCurrentTargetCondition && !IsEnemyStatusEffectConditionMet(attacker, currentTarget, effect))
                continue;

            List<BattleAllyController> targets = SelectAllyTargets(effect.targetMode, currentTarget, opponents, effect.randomTargetCount);
            for (int j = 0; j < targets.Count; j++)
            {
                BattleAllyController target = targets[j];
                if (target == null || target.IsDead)
                    continue;
                if (!useCurrentTargetCondition && !IsEnemyStatusEffectConditionMet(attacker, target, effect))
                    continue;

                ApplyResolvedStatusEffect(attacker, target, effect, effectIndex);
            }
        }
    }

    private static bool IsAllyStatusEffectConditionMet(BattleAllyController attacker, BattleEnemyController target, EnemySkill_SO.StatusEffectEntry effect)
    {
        if (attacker == null)
            return false;

        return BattleStatusEffectConditionService.IsConditionMet(
            effect,
            attacker.Speed,
            target != null ? target.Speed : attacker.Speed,
            attacker.GetStatusStacks,
            attacker.GetStatusStacks,
            target != null ? target.GetStatusStacks : attacker.GetStatusStacks,
            target != null ? target.GetStatusStacks : attacker.GetStatusStacks,
            target != null ? target.SelectedSkill : null);
    }

    private static bool IsAllyStatusEffectConditionMet(BattleAllyController attacker, BattleAllyController target, EnemySkill_SO.StatusEffectEntry effect)
    {
        if (attacker == null)
            return false;

        BattleAllyController resolvedTarget = target != null ? target : attacker;
        return BattleStatusEffectConditionService.IsConditionMet(
            effect,
            attacker.Speed,
            resolvedTarget.Speed,
            attacker.GetStatusStacks,
            attacker.GetStatusStacks,
            resolvedTarget.GetStatusStacks,
            resolvedTarget.GetStatusStacks,
            resolvedTarget.SelectedSkill);
    }

    private static bool IsEnemyStatusEffectConditionMet(BattleEnemyController attacker, BattleAllyController target, EnemySkill_SO.StatusEffectEntry effect)
    {
        if (attacker == null)
            return false;

        return BattleStatusEffectConditionService.IsConditionMet(
            effect,
            attacker.Speed,
            target != null ? target.Speed : attacker.Speed,
            attacker.GetStatusStacks,
            attacker.GetStatusStacks,
            target != null ? target.GetStatusStacks : attacker.GetStatusStacks,
            target != null ? target.GetStatusStacks : attacker.GetStatusStacks,
            target != null ? target.SelectedSkill : null);
    }

    private static bool IsEnemyStatusEffectConditionMet(BattleEnemyController attacker, BattleEnemyController target, EnemySkill_SO.StatusEffectEntry effect)
    {
        if (attacker == null)
            return false;

        BattleEnemyController resolvedTarget = target != null ? target : attacker;
        return BattleStatusEffectConditionService.IsConditionMet(
            effect,
            attacker.Speed,
            resolvedTarget.Speed,
            attacker.GetStatusStacks,
            attacker.GetStatusStacks,
            resolvedTarget.GetStatusStacks,
            resolvedTarget.GetStatusStacks,
            resolvedTarget.SelectedSkill);
    }

    private void ApplyResolvedStatusEffect(BattleAllyController attacker, BattleEnemyController target, EnemySkill_SO.StatusEffectEntry effect, int effectIndex)
    {
        if (!BattleStatusResolutionService.TryResolve(effect.statusData, effect.statusType, out BattleResolvedStatus resolvedStatus))
            return;
        int adjustedStacks = ResolvePlayerRelicAdjustedStatusStacks(resolvedStatus.StatusData, resolvedStatus.StatusId, resolvedStatus.StatusName, effect.stackAmount, attacker, target);
        if (!BattleStatusEffectApplicationService.TryBuildPlan(effect, adjustedStacks, out BattleStatusEffectApplicationPlan statusPlan))
            return;
        BattleStatusApplicationPlan plan = statusPlan.ApplicationPlan;
        BattleStatusApplicationDispatcher.Execute(
            plan,
            (stacks, delayRounds, repeatCount) =>
            {
                bool queued = target.QueueStatusForRoundStart(statusPlan.StatusData, statusPlan.StatusId, statusPlan.StatusName, stacks, delayRounds, repeatCount);
                if (queued)
                    BattleLog(BuildDelayedStatusLog(attacker.GetDebugDisplayName(), target.GetDebugDisplayName(), effect, effectIndex, statusPlan.StatusName, delayRounds, repeatCount, stacks));
                return queued;
            },
            stacks =>
            {
                bool added = target.AddStatus(statusPlan.StatusData, statusPlan.StatusId, statusPlan.StatusName, stacks);
                if (added)
                {
                    int currentStacks = statusPlan.StatusData != null ? target.GetStatusStacks(statusPlan.StatusData) : target.GetStatusStacks(statusPlan.StatusId);
                    BattleLog(BattleStatusEffectApplicationService.BuildImmediateAppliedLog(attacker.GetDebugDisplayName(), target.GetDebugDisplayName(), BuildStatusEffectLabel(effect, effectIndex), statusPlan.StatusName, stacks, currentStacks));
                }
                return added;
            },
            () => BattleLog(BuildImmuneStatusBlockedLog(attacker.GetDebugDisplayName(), target.GetDebugDisplayName(), effect, effectIndex, statusPlan.StatusName)));
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
        if (!BattleStatusEffectApplicationService.TryBuildPlan(effect, effect.stackAmount, out BattleStatusEffectApplicationPlan statusPlan))
            return;
        BattleStatusApplicationPlan plan = statusPlan.ApplicationPlan;
        BattleStatusApplicationDispatcher.Execute(
            plan,
            (stacks, delayRounds, repeatCount) =>
            {
                bool queued = target.QueueStatusForRoundStart(statusPlan.StatusData, statusPlan.StatusId, statusPlan.StatusName, stacks, delayRounds, repeatCount);
                if (queued)
                    BattleLog(BuildDelayedStatusLog(attacker.GetDebugDisplayName(), target.GetDebugDisplayName(), effect, effectIndex, statusPlan.StatusName, delayRounds, repeatCount, stacks));
                return queued;
            },
            stacks =>
            {
                bool added = target.AddStatus(statusPlan.StatusData, statusPlan.StatusId, statusPlan.StatusName, stacks);
                if (added)
                {
                    int currentStacks = statusPlan.StatusData != null ? target.GetStatusStacks(statusPlan.StatusData) : target.GetStatusStacks(statusPlan.StatusId);
                    BattleLog(BattleStatusEffectApplicationService.BuildImmediateAppliedLog(attacker.GetDebugDisplayName(), target.GetDebugDisplayName(), BuildStatusEffectLabel(effect, effectIndex), statusPlan.StatusName, stacks, currentStacks));
                }
                return added;
            },
            null);
    }

    private static bool TryResolveStatus(EnemySkill_SO.StatusEffectEntry effect, out BattleStatus_SO statusData, out string statusId, out string statusName)
    {
        bool resolved = BattleStatusResolutionService.TryResolve(effect.statusData, effect.statusType, out BattleResolvedStatus result);
        statusData = result.StatusData;
        statusId = result.StatusId;
        statusName = result.StatusName;
        return resolved;
    }

    private static bool TryResolveStatus(EnemySkill_SO.StatusBurstDamageEffectEntry effect, out BattleStatus_SO statusData, out string statusId, out string statusName)
    {
        bool resolved = BattleStatusResolutionService.TryResolve(effect.statusData, effect.statusType, out BattleResolvedStatus result);
        statusData = result.StatusData;
        statusId = result.StatusId;
        statusName = result.StatusName;
        return resolved;
    }

    private static bool TryResolveStatus(EnemySkill_SO.StatusStackOperationEffectEntry effect, out BattleStatus_SO statusData, out string statusId, out string statusName)
    {
        bool resolved = BattleStatusResolutionService.TryResolve(effect.statusData, effect.statusType, out BattleResolvedStatus result);
        statusData = result.StatusData;
        statusId = result.StatusId;
        statusName = result.StatusName;
        return resolved;
    }

    private static bool TryResolveStatus(EnemySkill_SO.BuiltInEffectEntry effect, out BattleStatus_SO statusData, out string statusId, out string statusName)
    {
        return TryResolveBuiltInConditionStatus(effect, out statusData, out statusId, out statusName);
    }

    private static int GetStatusStacks(BattleAllyController target, BattleStatus_SO statusData, string statusId)
    {
        if (target == null || target.IsDead)
            return 0;

        return statusData != null
            ? target.GetStatusStacks(statusData)
            : target.GetStatusStacks(statusId);
    }

    private static int GetStatusStacks(BattleEnemyController target, BattleStatus_SO statusData, string statusId)
    {
        if (target == null || target.IsDead)
            return 0;

        return statusData != null
            ? target.GetStatusStacks(statusData)
            : target.GetStatusStacks(statusId);
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
        return BattleTargetSelectionService.Select(
            targetMode,
            currentTarget,
            candidates,
            randomTargetCount,
            target => target != null && !target.IsDead,
            targets => BattleTargetSelectionService.SelectRandomTargets(targets, randomTargetCount, UnityEngine.Random.Range),
            CompareLowestHpEnemy);
    }

    private List<BattleAllyController> SelectAllyTargets(
        EnemySkill_SO.DamageTargetMode targetMode,
        BattleAllyController currentTarget,
        List<BattleAllyController> candidates,
        int randomTargetCount)
    {
        return BattleTargetSelectionService.Select(
            targetMode,
            currentTarget,
            candidates,
            randomTargetCount,
            target => target != null && !target.IsDead,
            targets => BattleTargetSelectionService.SelectRandomTargets(targets, randomTargetCount, UnityEngine.Random.Range),
            CompareLowestHpAlly);
    }

    private static int CompareLowestHpAlly(BattleAllyController left, BattleAllyController right)
    {
        int leftHp = left != null ? left.CurrentHP : int.MaxValue;
        int rightHp = right != null ? right.CurrentHP : int.MaxValue;
        int hpComparison = leftHp.CompareTo(rightHp);
        if (hpComparison != 0)
            return hpComparison;

        int leftMaxHp = left != null ? left.MaxHP : int.MaxValue;
        int rightMaxHp = right != null ? right.MaxHP : int.MaxValue;
        return leftMaxHp.CompareTo(rightMaxHp);
    }

    private static int CompareLowestHpEnemy(BattleEnemyController left, BattleEnemyController right)
    {
        int leftHp = left != null ? left.CurrentHP : int.MaxValue;
        int rightHp = right != null ? right.CurrentHP : int.MaxValue;
        int hpComparison = leftHp.CompareTo(rightHp);
        if (hpComparison != 0)
            return hpComparison;

        int leftMaxHp = left != null ? left.MaxHP : int.MaxValue;
        int rightMaxHp = right != null ? right.MaxHP : int.MaxValue;
        return leftMaxHp.CompareTo(rightMaxHp);
    }

    private static string BuildEffectLabel(EnemySkill_SO.DamageEffectEntry effect, int fallbackIndex)
    {
        return string.IsNullOrWhiteSpace(effect.effectName) ? $"\u989d\u5916\u4f24\u5bb3#{fallbackIndex + 1}" : effect.effectName;
    }

    private static string BuildStatusEffectLabel(EnemySkill_SO.StatusEffectEntry effect, int fallbackIndex)
    {
        return BattleEnemySkillEffectLabelService.BuildLabel(effect.effectName, "状态效果", fallbackIndex);
    }

    private static string BuildStatusBurstEffectLabel(EnemySkill_SO.StatusBurstDamageEffectEntry effect, int fallbackIndex)
    {
        return BattleEnemySkillEffectLabelService.BuildLabel(effect.effectName, "状态爆发", fallbackIndex);
    }

    private static string BuildHealingEffectLabel(EnemySkill_SO.HealingEffectEntry effect, int fallbackIndex)
    {
        return BattleEnemySkillEffectLabelService.BuildLabel(effect.effectName, "恢复效果", fallbackIndex);
    }

    private static string BuildBuiltInEffectLabel(EnemySkill_SO.BuiltInEffectEntry effect, int fallbackIndex)
    {
        return BattleEnemySkillEffectLabelService.BuildLabel(effect.effectName, "内建效果", fallbackIndex);
    }

    private static string BuildStatusStackOperationEffectLabel(EnemySkill_SO.StatusStackOperationEffectEntry effect, int fallbackIndex)
    {
        return BattleEnemySkillEffectLabelService.BuildLabel(effect.effectName, "状态层数操作", fallbackIndex);
    }

    private static string BuildEnemyStageTransitionEffectLabel(EnemySkill_SO.EnemyStageTransitionEffectEntry effect, int fallbackIndex)
    {
        return BattleEnemySkillEffectLabelService.BuildLabel(effect.effectName, "阶段转换", fallbackIndex);
    }

    private static string BuildDelayedStatusLog(string attackerName, string targetName, EnemySkill_SO.StatusEffectEntry effect, int fallbackIndex, string statusName, int delayRounds, int repeatCount, int appliedStacks = -1)
    {
        return BattleStatusApplicationLogService.BuildDelayedStatusLog(
            attackerName,
            targetName,
            BuildStatusEffectLabel(effect, fallbackIndex),
            statusName,
            delayRounds,
            repeatCount,
            appliedStacks >= 0 ? appliedStacks : effect.stackAmount);
    }

    private static string BuildImmuneStatusBlockedLog(string attackerName, string targetName, EnemySkill_SO.StatusEffectEntry effect, int fallbackIndex, string statusName)
    {
        return BattleStatusApplicationLogService.BuildImmuneStatusBlockedLog(
            attackerName,
            targetName,
            BuildStatusEffectLabel(effect, fallbackIndex),
            statusName);
    }

    private static int ResolvePlayerRelicAdjustedStatusStacks(BattleStatus_SO statusData, string statusId, string statusName, int baseStacks, UnityEngine.Object source, UnityEngine.Object target)
    {
        if ((statusData == null && string.IsNullOrWhiteSpace(statusId)) || baseStacks <= 0)
            return baseStacks;

        BattleEncounterData activeEncounter = BattleEncounterRuntime.ActiveEncounter;
        if (activeEncounter == null || activeEncounter.relicInventory == null || activeEncounter.relicInventory.Count == 0)
            return baseStacks;

        RelicExecutionContext context = RelicTriggerDispatcher.ProcessStatusApplied(
            activeEncounter,
            null,
            statusData,
            baseStacks,
            true,
            source,
            target,
            statusId,
            statusName);
        return Mathf.Max(0, baseStacks + context.additionalStatusStacks);
    }

    private static PlayerRelicDiceModifier ResolvePlayerRelicDiceModifier(BattleAllyController attacker, BattleEnemyController target, int diceIndex, bool isClashPreparation)
    {
        PlayerRelicDiceModifier modifier = default;
        if (attacker == null)
            return modifier;

        BattleEncounterData activeEncounter = BattleEncounterRuntime.ActiveEncounter;
        if (activeEncounter == null || activeEncounter.relicInventory == null || activeEncounter.relicInventory.Count == 0)
            return modifier;

        RelicExecutionContext context = RelicTriggerDispatcher.ProcessPlayerDicePrepared(
            activeEncounter,
            null,
            attacker.SelectedSkill,
            diceIndex,
            isClashPreparation,
            attacker,
            target);
        modifier.powerBonus = context.additionalPowerBonus;
        modifier.minPowerBonus = context.additionalMinPowerBonus;
        modifier.maxPowerBonus = context.additionalMaxPowerBonus;
        modifier.targetMinPowerBonus = context.additionalTargetMinPowerBonus;
        return modifier;
    }

    private void ProcessPlayerDiceRolledRelics(BattleAllyController attacker, BattleEnemyController target, int diceIndex, int rolledPower, int minPower)
    {
        if (attacker == null)
            return;

        BattleEncounterData activeEncounter = BattleEncounterRuntime.ActiveEncounter;
        if (activeEncounter == null || activeEncounter.relicInventory == null || activeEncounter.relicInventory.Count == 0)
            return;

        RelicTriggerDispatcher.ProcessPlayerDiceRolled(
            activeEncounter,
            null,
            attacker.SelectedSkill,
            diceIndex,
            rolledPower,
            minPower,
            attacker,
            target,
            BattleLog);
    }

    private void ProcessPlayerSkillUsedRelics(BattleAllyController attacker, BattleEnemyController target)
    {
        if (attacker == null)
            return;

        BattleEncounterData activeEncounter = BattleEncounterRuntime.ActiveEncounter;
        if (activeEncounter == null || activeEncounter.relicInventory == null || activeEncounter.relicInventory.Count == 0)
            return;

        RelicTriggerDispatcher.ProcessPlayerSkillUsed(
            activeEncounter,
            null,
            attacker.SelectedSkill,
            attacker,
            target,
            BattleLog);
    }

    private void ProcessPlayerClashLostRelics(BattleAllyController attacker, BattleEnemyController target, int diceIndex)
    {
        if (attacker == null || target == null)
            return;

        BattleEncounterData activeEncounter = BattleEncounterRuntime.ActiveEncounter;
        if (activeEncounter == null || activeEncounter.relicInventory == null || activeEncounter.relicInventory.Count == 0)
            return;

        RelicTriggerDispatcher.ProcessPlayerClashLost(
            activeEncounter,
            null,
            attacker.SelectedSkill,
            diceIndex,
            attacker,
            target,
            BattleLog);
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
            ally.Speed,
            currentRound);
        BattleRelicEffectRunner.Process(activeEncounter, context, BattleLog);
    }

    private void ApplyBattleRoundStartRelics(BattleAllyController[] allies, BattleEnemyController[] enemies)
    {
        BattleTargetingPhaseRelicService.DispatchRoundStart(
            BattleEncounterRuntime.ActiveEncounter,
            currentRound,
            allies,
            enemies,
            (activeEncounter, round, roundAllies, roundEnemies) => RelicTriggerDispatcher.ProcessBattleRoundStart(
                activeEncounter,
                null,
                round,
                roundAllies,
                roundEnemies),
            amount => RestoreSharedEnergy(amount),
            amount => AddTemporarySharedEnergy(amount),
            context => BattleRelicEffectRunner.FlushFeedback(context, BattleLog));
    }

    private BattleAllyController[] ApplyBattleSetupRelics(BattleAllyController[] allies, BattleEnemyController[] enemies)
    {
        int extraButtonsPerAlly = BattleTargetingPhaseRelicService.DispatchBattleSetup(
            BattleEncounterRuntime.ActiveEncounter,
            currentRound,
            allies,
            enemies,
            (activeEncounter, round, setupAllies, setupEnemies) => RelicTriggerDispatcher.ProcessBattleSetup(
                activeEncounter,
                null,
                round,
                setupAllies,
                setupEnemies),
            amount => IncreaseSharedEnergyMaxAtBattleStart(amount),
            context => ApplyBattleSetupSummonedAllyRelics(context),
            context => BattleRelicEffectRunner.FlushFeedback(context, BattleLog));

        BattleAllyController[] latestAllies = activeRoundAllies ?? allies;
        BattleTargetingPhaseRelicService.ApplyTemporarySpeedButtons(latestAllies, extraButtonsPerAlly);
        return latestAllies;
    }

    private void ApplyBattleSetupSummonedAllyRelics(RelicExecutionContext context)
    {
        if (context == null || context.summonAllyEnemyData == null)
            return;

        BattleSceneEncounterBootstrap bootstrap = FindObjectOfType<BattleSceneEncounterBootstrap>();
        if (bootstrap == null)
            return;

        BattleAllyController summonedAlly = bootstrap.SpawnAllyFromEnemyDataDuringBattle(context.summonAllyEnemyData);
        if (summonedAlly == null)
            return;

        RefreshActiveRoundAlliesFromScene();
        BattleLog($"遗物召唤：{summonedAlly.GetSettlementDisplayName()} 加入战斗。");
    }

    private void RefreshActiveRoundAlliesFromScene()
    {
        if (battleManager == null || battleManager.alliesParent == null)
            return;

        BattleAllyController[] allies = battleManager.alliesParent.GetComponentsInChildren<BattleAllyController>(false);
        SetActiveRoundCombatants(allies, activeRoundEnemies);

        if (battleFeedbackSystem != null && activeRoundEnemies != null)
            battleFeedbackSystem.BindCombatants(activeRoundAllies, activeRoundEnemies);
    }

    private void ApplyEnemyDeathRelics(BattleEnemyController defeatedEnemy, BattleAllyController[] allies, BattleEnemyController[] enemies)
    {
        BattleEncounterData activeEncounter = BattleEncounterRuntime.ActiveEncounter;
        if (defeatedEnemy == null)
            return;

        RelicExecutionContext context = BattleDeathRelicContextFactory.CreateEnemyDeath(defeatedEnemy, allies, enemies);
        BattleRelicBridge.Process(activeEncounter, context, (encounter, relicContext) => BattleRelicEffectRunner.Process(encounter, relicContext, BattleLog));
    }

    private void ApplyBattleRoundEndRelics(BattleAllyController[] allies, BattleEnemyController[] enemies)
    {
        BattleEncounterData activeEncounter = BattleEncounterRuntime.ActiveEncounter;
        if (activeEncounter == null)
            return;

        if (activeEncounter.relicInventory != null && activeEncounter.relicInventory.Count > 0)
        {
            RelicExecutionContext context = RelicTriggerDispatcher.ProcessBattleRoundEnd(
                activeEncounter,
                null,
                currentRound,
                allies,
                enemies);

            if (context.additionalSharedEnergyRecovery > 0)
                RestoreSharedEnergy(context.additionalSharedEnergyRecovery);

            BattleRelicEffectRunner.FlushFeedback(context, BattleLog);
        }

        ApplyEnemyRoundEndCalamityHealing(enemies);
    }

    private void ApplyPendingBattleStartAllyStatusGrants(BattleAllyController[] allies)
    {
        BattlePendingBattleStartAllyStatusGrantService.Apply(
            BattleEncounterRuntime.ActiveEncounter,
            allies,
            (ally, grant) => BattleLog($"战斗开始效果：{ally.GetDebugDisplayName()} 获得 {grant.stacks} 层 {grant.statusData.DisplayName}"));

    }

    private void ApplyRoundStartStatusEffectsGeneric(BattleAllyController[] allies, BattleEnemyController[] enemies)
    {
        BattleRoundStartStatusEffectService.Dispatch(
            allies,
            enemies,
            ally => ally != null && !ally.IsDead,
            ally =>
            {
                List<BattleStatusTickResult> ticks = ally.ConsumeRoundStartStatusEffects();
                DispatchRoundStartStatusTicks(ally.transform, ally.GetDebugDisplayName(), ally.CurrentHP, ally.MaxHP, ticks);
                ApplyFlameLizardBurnHealing(ticks);
            },
            enemy => BattleStatusFlowTargetService.ShouldConsumeStatusEffects(enemy, target => target.IsDead),
            enemy =>
            {
                if (enemy.TryApplyPendingCalamityDelayedDeath())
                {
                    BattleLog($"灾厄效果：{enemy.GetDebugDisplayName()} 的不屈到期，倒下。");
                    return;
                }

                List<BattleStatusTickResult> ticks = enemy.ConsumeRoundStartStatusEffects();
                DispatchRoundStartStatusTicks(enemy.transform, enemy.GetDebugDisplayName(), enemy.CurrentHP, enemy.MaxHP, ticks);
                ApplyFlameLizardBurnHealing(ticks);
            },
            enemy => BattleStatusFlowTargetService.ShouldAttemptStatusDrivenStageTransition(enemy, target => target.IsDead, enemy != null && enemy.AppliedPendingStageTransitionThisRoundStart),
            enemy =>
            {
                int previousStage = enemy.CurrentStageIndex;
                if (enemy.TryApplyStatusDrivenStageTransition(allies, enemies))
                    BattleLog(BattleStatusFlowTargetService.BuildStatusDrivenStageTransitionLog(enemy.GetDebugDisplayName(), previousStage, enemy.CurrentStageIndex));
            });
    }

    private void ApplyFlameLizardBurnHealing(List<BattleStatusTickResult> ticks)
    {
        if (ticks == null || ticks.Count == 0)
            return;

        int burnDamage = 0;
        for (int i = 0; i < ticks.Count; i++)
        {
            BattleStatusTickResult tick = ticks[i];
            string statusId = !string.IsNullOrWhiteSpace(tick.statusId)
                ? tick.statusId
                : tick.statusData != null ? tick.statusData.StatusId : string.Empty;
            if (tick.damage > 0 && string.Equals(statusId, "burn", StringComparison.OrdinalIgnoreCase))
                burnDamage += tick.damage;
        }

        if (burnDamage <= 0)
            return;

        List<BattleEnemyController> enemies = GetLivingEnemies();
        for (int i = 0; i < enemies.Count; i++)
        {
            BattleEnemyController enemy = enemies[i];
            if (enemy == null || enemy.IsDead || enemy.EnemyID != 29)
                continue;

            int healed = enemy.Heal(burnDamage);
            if (healed > 0)
                BattleLog($"被动触发：{enemy.GetDebugDisplayName()}[熔愈鳞甲] 因燃烧伤害恢复 {healed} 点生命，当前HP {enemy.CurrentHP}/{enemy.MaxHP}");
        }
    }

    private void DispatchRoundStartStatusTicks(Transform targetTransform, string displayName, int currentHP, int maxHP, List<BattleStatusTickResult> ticks)
    {
        BattleStatusTickDispatcher.DispatchRoundStartTicks(
            ticks,
            _ => ResolvePresentationSystem(),
            tick => battlePresentationSystem?.PlayStatusTickEffect(targetTransform, tick.statusData),
            tick => BattleLog(BattleStatusTickLogService.BuildDamageLog(displayName, tick.displayName, tick.damage, tick.stacks, currentHP, maxHP)),
            tick => BattleLog(BattleStatusTickLogService.BuildSpeedLog(displayName, tick.displayName, tick.speedBonus, tick.stacks)),
            tick => BattleLog(BattleStatusTickLogService.BuildAppliedStatusLog(displayName, tick.appliedStatusName, tick.appliedStacks)));
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
        BattleHealingEffectDispatcher.DispatchEffects(
            settings.healingEffects,
            trigger,
            currentTarget,
            opponents,
            effect => ShouldApplyAllyHealingEffect(effect, trigger, attacker, currentTarget),
            effect => ResolveAllyHealingAmount(attacker, currentTarget, opponents, effect, dealtDamage),
            (targetMode, target, candidates, count) => SelectEnemyTargets(targetMode, target, candidates != null ? new List<BattleEnemyController>(candidates) : null, count),
            target => target != null && !target.IsDead,
            attacker.Heal,
            (target, amount) => target.Heal(amount),
            (effect, index, actualHeal) =>
            {
                BattleLog($"词条触发：{attacker.GetDebugDisplayName()}[{BuildHealingEffectLabel(effect, index)}] 使 自己 恢复 {actualHeal} 点生命，当前HP {attacker.CurrentHP}/{attacker.MaxHP}");
            },
            (target, effect, index, actualHeal) =>
            {
                BattleLog($"词条触发：{attacker.GetDebugDisplayName()}[{BuildHealingEffectLabel(effect, index)}] 使 {target.GetDebugDisplayName()} 恢复 {actualHeal} 点生命，当前HP {target.CurrentHP}/{target.MaxHP}");
            });
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
        BattleHealingEffectDispatcher.DispatchEffects(
            settings.healingEffects,
            trigger,
            currentTarget,
            opponents,
            effect => ShouldApplyEnemyHealingEffect(effect, trigger, attacker, currentTarget),
            effect => ResolveEnemyHealingAmount(attacker, currentTarget, opponents, effect, dealtDamage),
            (targetMode, target, candidates, count) => SelectAllyTargets(targetMode, target, candidates != null ? new List<BattleAllyController>(candidates) : null, count),
            target => target != null && !target.IsDead,
            attacker.Heal,
            (target, amount) => target.Heal(amount),
            (effect, index, actualHeal) =>
            {
                BattleLog($"词条触发：{attacker.GetDebugDisplayName()}[{BuildHealingEffectLabel(effect, index)}] 使 自己 恢复 {actualHeal} 点生命，当前HP {attacker.CurrentHP}/{attacker.MaxHP}");
            },
            (target, effect, index, actualHeal) =>
            {
                BattleLog($"词条触发：{attacker.GetDebugDisplayName()}[{BuildHealingEffectLabel(effect, index)}] 使 {target.GetDebugDisplayName()} 恢复 {actualHeal} 点生命，当前HP {target.CurrentHP}/{target.MaxHP}");
            });
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
        BattleStatusStackOperationDispatcher.DispatchEffects(
            settings.statusStackOperationEffects,
            trigger,
            currentTarget,
            opponents,
            effect => IsAllyStatusStackOperationConditionMet(attacker, attacker, effect),
            (effect, target) => IsAllyStatusStackOperationConditionMet(attacker, target, effect),
            (targetMode, target, candidates, count) => SelectEnemyTargets(targetMode, target, candidates != null ? new List<BattleEnemyController>(candidates) : null, count),
            target => target != null && !target.IsDead,
            (effect, index) => ApplyStatusStackOperation(attacker, attacker, effect, index),
            (target, effect, index) => ApplyStatusStackOperation(attacker, target, effect, index));
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
        BattleStatusStackOperationDispatcher.DispatchEffects(
            settings.statusStackOperationEffects,
            trigger,
            currentTarget,
            opponents,
            effect => IsEnemyStatusStackOperationConditionMet(attacker, attacker, effect),
            (effect, target) => IsEnemyStatusStackOperationConditionMet(attacker, target, effect),
            (targetMode, target, candidates, count) => SelectAllyTargets(targetMode, target, candidates != null ? new List<BattleAllyController>(candidates) : null, count),
            target => target != null && !target.IsDead,
            (effect, index) => ApplyStatusStackOperation(attacker, attacker, effect, index),
            (target, effect, index) => ApplyStatusStackOperation(attacker, target, effect, index));
    }

    private static bool IsAllyStatusStackOperationConditionMet(BattleAllyController attacker, BattleEnemyController target, EnemySkill_SO.StatusStackOperationEffectEntry effect)
    {
        if (attacker == null)
            return false;

        return BattleStatusEffectConditionService.IsConditionMet(
            effect,
            attacker.Speed,
            target != null ? target.Speed : attacker.Speed,
            attacker.GetStatusStacks,
            attacker.GetStatusStacks,
            target != null ? target.GetStatusStacks : attacker.GetStatusStacks,
            target != null ? target.GetStatusStacks : attacker.GetStatusStacks,
            target != null ? target.SelectedSkill : null);
    }

    private static bool IsAllyStatusStackOperationConditionMet(BattleAllyController attacker, BattleAllyController target, EnemySkill_SO.StatusStackOperationEffectEntry effect)
    {
        if (attacker == null)
            return false;

        BattleAllyController resolvedTarget = target != null ? target : attacker;
        return BattleStatusEffectConditionService.IsConditionMet(
            effect,
            attacker.Speed,
            resolvedTarget.Speed,
            attacker.GetStatusStacks,
            attacker.GetStatusStacks,
            resolvedTarget.GetStatusStacks,
            resolvedTarget.GetStatusStacks,
            resolvedTarget.SelectedSkill);
    }

    private static bool IsEnemyStatusStackOperationConditionMet(BattleEnemyController attacker, BattleAllyController target, EnemySkill_SO.StatusStackOperationEffectEntry effect)
    {
        if (attacker == null)
            return false;

        return BattleStatusEffectConditionService.IsConditionMet(
            effect,
            attacker.Speed,
            target != null ? target.Speed : attacker.Speed,
            attacker.GetStatusStacks,
            attacker.GetStatusStacks,
            target != null ? target.GetStatusStacks : attacker.GetStatusStacks,
            target != null ? target.GetStatusStacks : attacker.GetStatusStacks,
            target != null ? target.SelectedSkill : null);
    }

    private static bool IsEnemyStatusStackOperationConditionMet(BattleEnemyController attacker, BattleEnemyController target, EnemySkill_SO.StatusStackOperationEffectEntry effect)
    {
        if (attacker == null)
            return false;

        BattleEnemyController resolvedTarget = target != null ? target : attacker;
        return BattleStatusEffectConditionService.IsConditionMet(
            effect,
            attacker.Speed,
            resolvedTarget.Speed,
            attacker.GetStatusStacks,
            attacker.GetStatusStacks,
            resolvedTarget.GetStatusStacks,
            resolvedTarget.GetStatusStacks,
            resolvedTarget.SelectedSkill);
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
            if (!BattleEnemyStageTransitionEffectService.ShouldProcess(effect, trigger))
                continue;

            if (attacker.TryTransitionToStage(
                effect.targetStageIndex,
                BattleEnemyStageTransitionEffectService.ShouldApplyNextRoundStart(effect)))
            {
                if (BattleEnemyStageTransitionEffectService.ShouldApplyNextRoundStart(effect))
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

    private void ApplyEnemySummonEffects(BattleEnemyController attacker, int diceIndex, EnemySkill_SO.DiceEffectTrigger trigger)
    {
        EnemySkill_SO skill = attacker != null ? attacker.SelectedSkill : null;
        if (skill == null)
            return;

        EnemySkill_SO.DiceEffectSettings settings = skill.GetDiceEffectSettings(diceIndex);
        if (settings.summonEffects == null || settings.summonEffects.Length == 0)
            return;

        BattleSceneEncounterBootstrap bootstrap = FindObjectOfType<BattleSceneEncounterBootstrap>();
        if (bootstrap == null)
            return;

        for (int i = 0; i < settings.summonEffects.Length; i++)
        {
            EnemySkill_SO.SummonEnemyEffectEntry effect = settings.summonEffects[i];
            if (!BattleSummonEffectService.ShouldProcess(effect, trigger))
                continue;

            int summonCount = BattleSummonEffectService.NormalizeSummonCount(effect.summonCount);
            int summoned = 0;
            for (int summonIndex = 0; summonIndex < summonCount; summonIndex++)
            {
                BattleEnemyController summonedEnemy = bootstrap.SpawnEnemyDuringBattle(effect.enemyData);
                if (summonedEnemy == null)
                    continue;

                summoned++;
                ApplyEnemyStatusGains(
                    summonedEnemy,
                    summonedEnemy.enemyData != null ? summonedEnemy.enemyData.battleStartStatusGains : null,
                    (statusName, appliedStacks, currentStacks) =>
                        $"召唤效果：{summonedEnemy.GetDebugDisplayName()} 获得 {appliedStacks} 层 {statusName}，当前 {statusName} {currentStacks} 层");
                BattleLog($"词条触发：{attacker.GetDebugDisplayName()}[{BuildSummonEffectLabel(effect, i)}] 召唤了 {summonedEnemy.GetDebugDisplayName()}");
            }

            if (summoned > 0)
                RefreshActiveRoundEnemiesFromScene();
        }
    }

    private void RefreshActiveRoundEnemiesFromScene()
    {
        if (battleManager == null || battleManager.enemiesParent == null)
            return;

        BattleEnemyController[] enemies = battleManager.enemiesParent.GetComponentsInChildren<BattleEnemyController>(false);
        SetActiveRoundCombatants(activeRoundAllies, enemies);

        if (battleFeedbackSystem != null && activeRoundAllies != null)
            battleFeedbackSystem.BindCombatants(activeRoundAllies, activeRoundEnemies);
    }

    private static string BuildSummonEffectLabel(EnemySkill_SO.SummonEnemyEffectEntry effect, int effectIndex)
    {
        return BattleSummonEffectService.BuildLabel(effect, effectIndex);
    }

    private static int ResolveAllyHealingAmount(BattleAllyController attacker, BattleEnemyController currentTarget, List<BattleEnemyController> opponents, EnemySkill_SO.HealingEffectEntry effect, int dealtDamage)
    {
        return BattleHealingEffectResolutionService.ResolveHealingAmount(
            effect,
            dealtDamage,
            _ => ResolveEnemyStatusStackSource(currentTarget, opponents, attacker, effect));
    }

    private static int ResolveEnemyHealingAmount(BattleEnemyController attacker, BattleAllyController currentTarget, List<BattleAllyController> opponents, EnemySkill_SO.HealingEffectEntry effect, int dealtDamage)
    {
        return BattleHealingEffectResolutionService.ResolveHealingAmount(
            effect,
            dealtDamage,
            _ => ResolveAllyStatusStackSource(currentTarget, opponents, attacker, effect));
    }

    private static int ResolveEnemyStatusStackSource(BattleEnemyController currentTarget, List<BattleEnemyController> opponents, BattleAllyController self, EnemySkill_SO.HealingEffectEntry effect)
    {
        return BattleHealingStatusStackSourceService.ResolveStatusStackSource(
            effect.statusStackSourceMode,
            self,
            currentTarget,
            opponents,
            effect.randomTargetCount,
            target => ResolveStatusStacks(target, effect),
            target => ResolveStatusStacks(target, effect),
            SelectEnemyTargetsStatic);
    }

    private static int ResolveAllyStatusStackSource(BattleAllyController currentTarget, List<BattleAllyController> opponents, BattleEnemyController self, EnemySkill_SO.HealingEffectEntry effect)
    {
        return BattleHealingStatusStackSourceService.ResolveStatusStackSource(
            effect.statusStackSourceMode,
            self,
            currentTarget,
            opponents,
            effect.randomTargetCount,
            target => ResolveStatusStacks(target, effect),
            target => ResolveStatusStacks(target, effect),
            SelectAllyTargetsStatic);
    }

    private static int ResolveStatusStacks(BattleEnemyController target, EnemySkill_SO.HealingEffectEntry effect)
    {
        if (target == null)
            return 0;

        return BattleHealingEffectResolutionService.ResolveStatusStacks(
            effect,
            target.GetStatusStacks,
            target.GetStatusStacks);
    }

    private static int ResolveStatusStacks(BattleAllyController target, EnemySkill_SO.HealingEffectEntry effect)
    {
        if (target == null)
            return 0;

        return BattleHealingEffectResolutionService.ResolveStatusStacks(
            effect,
            target.GetStatusStacks,
            target.GetStatusStacks);
    }

    private static bool ShouldApplyAllyHealingEffect(EnemySkill_SO.HealingEffectEntry effect, EnemySkill_SO.DiceEffectTrigger trigger, BattleAllyController attacker, BattleEnemyController currentTarget)
    {
        if (attacker == null)
            return false;

        return BattleHealingEffectResolutionService.ShouldApplyEffect(
            effect,
            trigger,
            attacker.Speed,
            currentTarget != null ? currentTarget.Speed : 0,
            currentTarget != null ? currentTarget.GetStatusStacks : null,
            currentTarget != null ? currentTarget.GetStatusStacks : null);
    }

    private static bool ShouldApplyEnemyHealingEffect(EnemySkill_SO.HealingEffectEntry effect, EnemySkill_SO.DiceEffectTrigger trigger, BattleEnemyController attacker, BattleAllyController currentTarget)
    {
        if (attacker == null)
            return false;

        return BattleHealingEffectResolutionService.ShouldApplyEffect(
            effect,
            trigger,
            attacker.Speed,
            currentTarget != null ? currentTarget.Speed : 0,
            currentTarget != null ? currentTarget.GetStatusStacks : null,
            currentTarget != null ? currentTarget.GetStatusStacks : null);
    }

    private static List<BattleEnemyController> SelectEnemyTargetsStatic(EnemySkill_SO.DamageTargetMode targetMode, BattleEnemyController currentTarget, List<BattleEnemyController> candidates, int randomTargetCount)
    {
        return BattleTargetSelectionService.Select(
            targetMode,
            currentTarget,
            candidates,
            randomTargetCount,
            target => target != null && !target.IsDead,
            targets => BattleTargetSelectionService.SelectRandomTargets(targets, randomTargetCount, UnityEngine.Random.Range),
            CompareLowestHpEnemy);
    }

    private static List<BattleAllyController> SelectAllyTargetsStatic(EnemySkill_SO.DamageTargetMode targetMode, BattleAllyController currentTarget, List<BattleAllyController> candidates, int randomTargetCount)
    {
        return BattleTargetSelectionService.Select(
            targetMode,
            currentTarget,
            candidates,
            randomTargetCount,
            target => target != null && !target.IsDead,
            targets => BattleTargetSelectionService.SelectRandomTargets(targets, randomTargetCount, UnityEngine.Random.Range),
            CompareLowestHpAlly);
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
        BattleStatusBurstDispatcher.DispatchEffects(
            settings.statusBurstEffects,
            trigger,
            currentTarget,
            opponents,
            (targetMode, target, candidates, count) => SelectEnemyTargets(targetMode, target, candidates != null ? new List<BattleEnemyController>(candidates) : null, count),
            target => target != null && !target.IsDead,
            (effect, index) => ApplyStatusBurst(attacker, attacker, effect, index),
            (target, effect, index) => ApplyStatusBurst(attacker, target, effect, index));
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
        BattleStatusBurstDispatcher.DispatchEffects(
            settings.statusBurstEffects,
            trigger,
            currentTarget,
            opponents,
            (targetMode, target, candidates, count) => SelectAllyTargets(targetMode, target, candidates != null ? new List<BattleAllyController>(candidates) : null, count),
            target => target != null && !target.IsDead,
            (effect, index) => ApplyStatusBurst(attacker, attacker, effect, index),
            (target, effect, index) => ApplyStatusBurst(attacker, target, effect, index));
    }

    private void ApplyStatusBurst(BattleAllyController attacker, BattleEnemyController target, EnemySkill_SO.StatusBurstDamageEffectEntry effect, int effectIndex)
    {
        ApplyStatusBurst(attacker.GetDebugDisplayName(), BuildStatusBurstEffectLabel(effect, effectIndex), CreateStatusTargetAdapter(target, true), effect, damage => ApplyPlayerRelicOutgoingDamageModifiers(attacker, target, -1, damage));
    }

    private void ApplyStatusBurst(BattleEnemyController attacker, BattleAllyController target, EnemySkill_SO.StatusBurstDamageEffectEntry effect, int effectIndex)
    {
        ApplyStatusBurst(attacker.GetDebugDisplayName(), BuildStatusBurstEffectLabel(effect, effectIndex), CreateStatusTargetAdapter(target, true), effect, ApplyEnemyCalamityDamageModifiers);
    }

    private void ApplyStatusBurst(BattleAllyController attacker, BattleAllyController target, EnemySkill_SO.StatusBurstDamageEffectEntry effect, int effectIndex)
    {
        ApplyStatusBurst(attacker.GetDebugDisplayName(), BuildStatusBurstEffectLabel(effect, effectIndex), CreateStatusTargetAdapter(target, false), effect, null);
    }

    private void ApplyStatusBurst(BattleEnemyController attacker, BattleEnemyController target, EnemySkill_SO.StatusBurstDamageEffectEntry effect, int effectIndex)
    {
        ApplyStatusBurst(attacker.GetDebugDisplayName(), BuildStatusBurstEffectLabel(effect, effectIndex), CreateStatusTargetAdapter(target, false), effect, null);
    }

    private void ApplyStatusBurst(string attackerName, string effectLabel, BattleStatusTargetAdapter target, EnemySkill_SO.StatusBurstDamageEffectEntry effect, System.Func<int, int> modifyDamage)
    {
        if (!TryResolveStatus(effect, out _, out string statusId, out string statusName))
            return;

        int stacks = target.GetStacks(effect.statusData, statusId);
        BattleStatusBurstDispatcher.DispatchBurst(
            stacks,
            effect.damagePerStack,
            modifyDamage,
            target.TakeDamage,
            () => target.ClearStatus(effect.statusData, statusId, statusName),
            effect.clearStacksAfterBurst,
            (modifiedDamage, _) => BattleLog($"\u8bcd\u6761\u89e6\u53d1\uff1a{attackerName}[{effectLabel}] \u5f15\u53d1 {target.DisplayName} \u7684 {statusName}\uff0c\u9020\u6210 {modifiedDamage} \u70b9\u989d\u5916\u4f24\u5bb3\uff08{effect.damagePerStack} x {stacks}\uff09{(effect.clearStacksAfterBurst ? $"\uff0c\u5e76\u6e05\u7a7a {statusName}" : string.Empty)}\uff0c\u5269\u4f59HP {target.CurrentHP}/{target.MaxHP}"));
    }

    private void ApplyStatusStackOperation(BattleAllyController attacker, BattleEnemyController target, EnemySkill_SO.StatusStackOperationEffectEntry effect, int effectIndex)
    {
        ApplyStatusStackOperation(attacker.GetDebugDisplayName(), BuildStatusStackOperationEffectLabel(effect, effectIndex), CreateStatusTargetAdapter(target, false), effect);
    }

    private void ApplyStatusStackOperation(BattleEnemyController attacker, BattleAllyController target, EnemySkill_SO.StatusStackOperationEffectEntry effect, int effectIndex)
    {
        ApplyStatusStackOperation(attacker.GetDebugDisplayName(), BuildStatusStackOperationEffectLabel(effect, effectIndex), CreateStatusTargetAdapter(target, false), effect);
    }

    private void ApplyStatusStackOperation(BattleAllyController attacker, BattleAllyController target, EnemySkill_SO.StatusStackOperationEffectEntry effect, int effectIndex)
    {
        ApplyStatusStackOperation(attacker.GetDebugDisplayName(), BuildStatusStackOperationEffectLabel(effect, effectIndex), CreateStatusTargetAdapter(target, false), effect);
    }

    private void ApplyStatusStackOperation(BattleEnemyController attacker, BattleEnemyController target, EnemySkill_SO.StatusStackOperationEffectEntry effect, int effectIndex)
    {
        ApplyStatusStackOperation(attacker.GetDebugDisplayName(), BuildStatusStackOperationEffectLabel(effect, effectIndex), CreateStatusTargetAdapter(target, false), effect);
    }

    private void ApplyStatusStackOperation(string attackerName, string effectLabel, BattleStatusTargetAdapter target, EnemySkill_SO.StatusStackOperationEffectEntry effect)
    {
        if (!TryResolveStatus(effect, out _, out string statusId, out string statusName))
            return;

        BattleStatusStackOperationDispatcher.DispatchOperation(
            effect.operationType,
            effect.stackAmount,
            () => target.GetStacks(effect.statusData, statusId),
            amount => target.RemoveStatusStacks(effect.statusData, statusId, amount, statusName),
            () => target.ClearStatus(effect.statusData, statusId, statusName),
            affectedStacks =>
            {
                string actionLabel = BattleStatusStackOperationService.BuildActionLabel(effect.operationType, statusName, affectedStacks);
                BattleLog($"\u8bcd\u6761\u89e6\u53d1\uff1a{attackerName}[{effectLabel}] {actionLabel}\uff0c\u76ee\u6807 {target.DisplayName}");
            });
    }

    private static BattleStatusTargetAdapter CreateStatusTargetAdapter(BattleAllyController target, bool preserveOriginalDamage)
    {
        return new BattleStatusTargetAdapter(
            target != null ? target.GetDebugDisplayName() : string.Empty,
            () => target != null ? target.CurrentHP : 0,
            () => target != null ? target.MaxHP : 0,
            statusData => target != null ? target.GetStatusStacks(statusData) : 0,
            statusId => target != null ? target.GetStatusStacks(statusId) : 0,
            (modifiedDamage, originalDamage) =>
            {
                if (target == null)
                    return;
                if (preserveOriginalDamage)
                    target.TakeDamage(modifiedDamage, 0, originalDamage);
                else
                    target.TakeDamage(modifiedDamage);
            },
            (statusData, statusId, statusName) => target?.ClearStatus(statusData, statusId, statusName),
            (statusData, statusId, stackAmount, statusName) => target != null ? target.RemoveStatusStacks(statusData, statusId, stackAmount, statusName) : 0);
    }

    private static BattleStatusTargetAdapter CreateStatusTargetAdapter(BattleEnemyController target, bool preserveOriginalDamage)
    {
        return new BattleStatusTargetAdapter(
            target != null ? target.GetDebugDisplayName() : string.Empty,
            () => target != null ? target.CurrentHP : 0,
            () => target != null ? target.MaxHP : 0,
            statusData => target != null ? target.GetStatusStacks(statusData) : 0,
            statusId => target != null ? target.GetStatusStacks(statusId) : 0,
            (modifiedDamage, originalDamage) =>
            {
                if (target == null)
                    return;
                if (preserveOriginalDamage)
                    target.TakeDamage(modifiedDamage, 0, originalDamage);
                else
                    target.TakeDamage(modifiedDamage);
            },
            (statusData, statusId, statusName) => target?.ClearStatus(statusData, statusId, statusName),
            (statusData, statusId, stackAmount, statusName) => target != null ? target.RemoveStatusStacks(statusData, statusId, stackAmount, statusName) : 0);
    }

    private void ApplyRoundEndStatusEffectsGeneric(BattleAllyController[] allies, BattleEnemyController[] enemies)
    {
        BattleCombatRoundEndDispatcher.DispatchRoundEndStatusEffects(
            allies,
            enemies,
            ally => ally.ConsumeRoundEndStatusEffects(),
            enemy => enemy.ConsumeRoundEndStatusEffects());
    }

    private void ResolvePresentationSystem()
    {
        GameObject host = battleManager != null ? battleManager.gameObject : gameObject;
        battlePresentationSystem = BattleComponentResolver.ResolveSceneOrHost(battlePresentationSystem, host, false, true);
    }

    private void ResolveFeedbackSystem()
    {
        GameObject host = battleManager != null ? battleManager.gameObject : gameObject;
        battleFeedbackSystem = BattleComponentResolver.ResolveSceneOrHost(battleFeedbackSystem, host, false, true);
    }

    private void ResolveSettlementOverlay()
    {
        GameObject host = battleManager != null ? battleManager.gameObject : gameObject;
        battleSettlementOverlay = BattleComponentResolver.ResolveSceneOrHost(battleSettlementOverlay, host, false, true);
    }

    private void ResolveSkillInfoOverlay()
    {
        GameObject host = battleManager != null ? battleManager.gameObject : gameObject;
        battleSkillInfoOverlay = BattleComponentResolver.ResolveSceneOrHost(battleSkillInfoOverlay, host, true, true);

        if (battleSkillInfoOverlay == null)
            return;

        battleSkillInfoOverlay.Hidden -= HandleSkillInfoOverlayHidden;
        battleSkillInfoOverlay.Hidden += HandleSkillInfoOverlayHidden;
        battleSkillInfoOverlay.AllySkillSelectionShown -= HandleAllySkillSelectionShown;
        battleSkillInfoOverlay.AllySkillSelectionShown += HandleAllySkillSelectionShown;
        battleSkillInfoOverlay.EnemySkillInfoShown -= HandleEnemySkillInfoShown;
        battleSkillInfoOverlay.EnemySkillInfoShown += HandleEnemySkillInfoShown;
        battleSkillInfoOverlay.EnemyDetailShown -= HandleEnemyDetailShown;
        battleSkillInfoOverlay.EnemyDetailShown += HandleEnemyDetailShown;
    }

    private void ResolveTutorialGuideOverlay()
    {
        GameObject host = battleManager != null ? battleManager.gameObject : gameObject;
        tutorialBattleGuideOverlay = BattleComponentResolver.ResolveSceneOrHost(tutorialBattleGuideOverlay, host, true, true);

        if (tutorialBattleGuideOverlay != null)
        {
            tutorialBattleGuideOverlay.ConfigureFonts(
                tutorialGuideKeywordFont != null ? tutorialGuideKeywordFont : sharedEnergyFont,
                tutorialGuideBodyFont != null ? tutorialGuideBodyFont : sharedEnergyFont);
        }
    }

    private void ResolvePauseMenuOverlay()
    {
        GameObject host = battleManager != null ? battleManager.gameObject : gameObject;
        battlePauseMenuOverlay = BattleComponentResolver.ResolveSceneOrHost(battlePauseMenuOverlay, host, true, true);
    }

    private void InitializeTutorialBattleForEncounter()
    {
        BattleEncounterData encounter = BattleEncounterRuntime.ActiveEncounter;
        if (encounter == null)
            encounter = BattleEncounterRuntime.CurrentEncounter;

        if (encounter != null && encounter.isTutorialEncounter && encounter.tutorialBattleDefinition != null)
        {
            TutorialBattleRuntime.BeginBattle(encounter.tutorialBattleDefinition);
            return;
        }

        TutorialBattleRuntime.Clear();
    }

    private void HandleSkillInfoOverlayHidden()
    {
        TryShowPendingTutorialStep();
    }

    private void HandleAllySkillSelectionShown(BattleAllyController ally)
    {
        HandleTutorialProgress(TutorialBattleRuntime.NotifyEvent(TutorialBattleEventType.AllySkillViewed), false);
    }

    private void HandleEnemySkillInfoShown(BattleEnemyController enemy, SpeedButton sourceButton)
    {
        HandleTutorialProgress(TutorialBattleRuntime.NotifyEvent(TutorialBattleEventType.EnemySkillViewed), false);
    }

    private void HandleEnemyDetailShown(BattleEnemyController enemy)
    {
        HandleTutorialProgress(TutorialBattleRuntime.NotifyEvent(TutorialBattleEventType.EnemyDetailViewed), false);
    }

    private void HandleTutorialProgress(TutorialBattleProgressResult progress, bool showPendingImmediately)
    {
        if (progress == null)
            return;

        if (progress.reactionToShow != null)
        {
            ShowTutorialReaction(progress.reactionToShow);
            return;
        }

        if (showPendingImmediately)
            TryShowPendingTutorialStep();
    }

    private void TryShowPendingTutorialStep()
    {
        if (tutorialBattleGuideOverlay == null || tutorialBattleGuideOverlay.IsVisible)
            return;
        if (battleSkillInfoOverlay != null && battleSkillInfoOverlay.IsVisible)
            return;
        if (!TutorialBattleRuntime.HasPendingStepPopup)
            return;

        TutorialBattleStepDefinition step = TutorialBattleRuntime.ConsumePendingStepPopup();
        if (step == null)
            return;

        tutorialBattleGuideOverlay.Show(step.title, step.body, step.continueButtonLabel, () =>
        {
            TutorialBattleRuntime.NotifyEvent(TutorialBattleEventType.ContinuePressed);
            TryShowPendingTutorialStep();
        });
    }

    private void ShowTutorialReaction(TutorialBattleReactionDefinition reaction)
    {
        if (tutorialBattleGuideOverlay == null || reaction == null)
            return;

        tutorialBattleGuideOverlay.Show(reaction.title, reaction.body, reaction.continueButtonLabel, null);
    }

    private IEnumerator ShowTutorialReactionIfNeeded(TutorialBattleEventType eventType)
    {
        TutorialBattleProgressResult progress = TutorialBattleRuntime.NotifyEvent(eventType);
        if (progress == null || progress.reactionToShow == null)
            yield break;

        ShowTutorialReaction(progress.reactionToShow);
        while (tutorialBattleGuideOverlay != null && tutorialBattleGuideOverlay.IsVisible)
            yield return null;
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
        List<BattleSettlementPartyProgression> partyProgressions = BuildPartyProgressions(allies, expReward);
        string retreatSubtitle = $"{retreatingEnemy.GetDebugDisplayName()} 脱离了战斗，剩余可行动角色：{livingAllies}";

        BattleEncounterResolutionWriter.ApplyForcedRetreatResolution(
            activeEncounter,
            expReward,
            moneyReward,
            BattleSettlementPartyMemberFactory.BuildRunStates(partyProgressions),
            lootRewards,
            dropResults.relicRewards,
            retreatSubtitle,
            settlementLootTables);


        if (battleManager != null)
            battleManager.EndBattle();

        SpeedButton.CombatLocked = true;
        currentPhase = BattlePhase.Ready;

        if (battleSettlementOverlay != null)
        {
            battleSettlementOverlay.ShowResult(BattleSettlementDataFactory.BuildForcedRetreatData(
                activeEncounter,
                retreatSubtitle,
                expReward,
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
                BattleSettlementPartyMemberFactory.BuildSettlementEntries(partyProgressions, Mathf.Max(0, expReward)),
                BattleSettlementLootEntryBuilder.Build(lootRewards, dropResults.relicRewards),
                dropResults.relicChoice));
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
        forceReturnToMapAfterDefeat = false;
        if (!victory)
        {
            RelicExecutionContext defeatContext = RelicTriggerDispatcher.ProcessBattleDefeat(
                activeEncounter,
                null,
                allies,
                enemies,
                BattleLog);
            if (defeatContext != null && defeatContext.preventBattleDefeat)
            {
                livingAllies = Mathf.Max(1, CountLiving(allies));
                forceReturnToMapAfterDefeat = defeatContext.forceReturnToMapOnDefeat;
            }
        }
        TutorialBattleRuntime.NotifyEvent(victory ? TutorialBattleEventType.BattleWon : TutorialBattleEventType.BattleLost);
        int expReward = victory ? CalculateVictoryExperienceReward(enemies) : 0;
        RelicExecutionContext completionContext = RelicTriggerDispatcher.ProcessBattleCompletion(
            activeEncounter,
            null,
            victory,
            expReward,
            allies,
            enemies,
            IsSpecialDungeonEncounter(activeEncounter),
            BattleLog);
        if (completionContext != null)
            expReward = Mathf.Max(0, completionContext.modifiedBattleExperience);
        int moneyReward = victory && activeEncounter != null
            ? CalamitySystem.ApplyBattleMoneyModifiers(Mathf.Max(0, activeEncounter.moneyGained), activeEncounter.activeCalamities)
            : 0;
        EncounterDropResults dropResults = victory
            ? RollEncounterDrops(activeEncounter, activeEncounter != null ? activeEncounter.resolutionOverride?.lootTables : null)
            : new EncounterDropResults();
        List<RunLootState> lootRewards = dropResults.lootRewards;
        int awardedExperience = victory ? expReward : 0;
        List<BattleSettlementPartyProgression> partyProgressions = BuildPartyProgressions(allies, awardedExperience);

        BattleEncounterResolutionWriter.ApplyCompletionResolution(
            activeEncounter,
            victory,
            expReward,
            moneyReward,
            BattleSettlementPartyMemberFactory.BuildRunStates(partyProgressions),
            lootRewards,
            dropResults.relicRewards);
        if (!victory && forceReturnToMapAfterDefeat && activeEncounter != null)
            activeEncounter.restartBattleOnDefeat = false;
        if (battleManager != null)
            battleManager.EndBattle();

        SpeedButton.CombatLocked = true;
        currentPhase = BattlePhase.Ready;

        if (battleSettlementOverlay != null)
        {
            battleSettlementOverlay.ShowResult(BattleSettlementDataFactory.BuildCompletionData(
                victory,
                activeEncounter,
                livingAllies,
                expReward,
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
                BattleSettlementPartyMemberFactory.BuildSettlementEntries(partyProgressions, awardedExperience),
                BattleSettlementLootEntryBuilder.Build(lootRewards, dropResults.relicRewards),
                dropResults.relicChoice));
        }

        BattleLog(victory
            ? $"Battle finished. Victory, EXP +{expReward}, Money +{moneyReward}."
            : "Battle finished. Defeat.");
        return true;
    }

    private List<BattleSettlementPartyProgression> BuildPartyProgressions(BattleAllyController[] allies, int experienceReward)
    {
        return BattleSettlementPartyMemberFactory.BuildProgressions(
            allies,
            experienceReward,
            ResolveExperienceToNextLevel,
            ResolveMaxHPForLevel);
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

    private static bool IsSpecialDungeonEncounter(BattleEncounterData encounter)
    {
        return encounter != null && encounter.isSpecialDungeonEncounter;
    }

    private int CalculateVictoryExperienceReward(BattleEnemyController[] enemies)
    {
        if (enemies == null || enemies.Length == 0)
            return 0;

        List<int> enemyBaseHpValues = new List<int>();
        for (int i = 0; i < enemies.Length; i++)
        {
            BattleEnemyController enemy = enemies[i];
            if (enemy == null)
                continue;

            enemyBaseHpValues.Add(enemy.BaseHP);
        }

        int baseReward = BattleExperienceRewardService.CalculateBaseReward(enemyBaseHpValues);
        BattleEncounterData activeEncounter = BattleEncounterRuntime.ActiveEncounter;
        if (activeEncounter == null)
            return baseReward;

        baseReward = BattleExperienceRewardService.ApplyTileMultiplier(baseReward, activeEncounter.tileType);

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

        IReadOnlyList<LootTable_SO> lootTables = BattleEncounterLootUtility.HasOverrideLootTables(overrideLootTables)
            ? overrideLootTables
            : encounter.enemyGroup.GetLootTables();
        System.Random rng = new System.Random(BattleEncounterLootUtility.ResolveRngSeed(encounter.seed));
        Dictionary<LootItem_SO, int> aggregated = new Dictionary<LootItem_SO, int>();
        Dictionary<Relic_SO, int> aggregatedRelics = new Dictionary<Relic_SO, int>();
        HashSet<Relic_SO> ownedRelics = BattleRelicInventorySetBuilder.BuildOwnedRelicSet(encounter != null ? encounter.relicInventory : null);
        List<LootItem_SO> lootPool = new List<LootItem_SO>();
        bool useEliteRelicChoice = BattleEncounterDropContextService.ShouldUseEliteRelicChoice(encounter);
        bool createdEliteRelicChoice = false;
        int relicChoiceCount = BattleEncounterDropContextService.ResolveRelicChoiceCount(eliteRelicChoiceCount);
        bool guaranteeLootDrop = RelicPassiveQueryService.HasBuiltInEffect(
            encounter,
            Relic_SO.RelicBuiltInEffectType.GuaranteeBattleLootDrop,
            Relic_SO.RelicTrigger.OnBattleLootRewardGranted);

        for (int i = 0; lootTables != null && i < lootTables.Count; i++)
        {
            LootTable_SO lootTable = lootTables[i];
            if (lootTable == null)
                continue;

            int lootCountBeforeTable = aggregated.Count;
            BattleLootDropRollService.RollTableLootDrops(lootTable, rng, aggregated, lootPool);
            if (guaranteeLootDrop && aggregated.Count == lootCountBeforeTable)
                BattleLootDropRollService.TryForceOneTableLootDrop(lootTable, rng, aggregated, lootPool);

            createdEliteRelicChoice = BattleRelicDropRollService.RollTableRelicDrops(
                lootTable,
                rng,
                useEliteRelicChoice,
                createdEliteRelicChoice,
                ownedRelics,
                relicChoiceCount,
                aggregatedRelics,
                choiceData => results.relicChoice = choiceData);
        }

        if (useEliteRelicChoice && !createdEliteRelicChoice)
        {
            createdEliteRelicChoice = BattleEliteRelicChoiceBuilderService.TryBuildAndApplyFromTable(
                ResolveEliteRelicChoiceFallbackTable(encounter),
                rng,
                ownedRelics,
                relicChoiceCount,
                choiceData => results.relicChoice = choiceData);
        }

#if UNITY_EDITOR
        if (useEliteRelicChoice && !createdEliteRelicChoice)
            createdEliteRelicChoice = TryBuildEditorEliteRelicChoiceFromAssets(results, rng, ownedRelics, relicChoiceCount);
#endif

        results.lootRewards = BattleRewardAggregationService.BuildLootRewardsFromAggregation(aggregated);

        ApplyRelicChoiceRefreshRelics(encounter, results.relicChoice);

        BattleBonusLootRewardService.ApplyBonusLootRewards(
            results.lootRewards,
            lootPool,
            encounter,
            (battleEncounter, lootContext) => RelicTriggerDispatcher.ProcessBattleRelics(battleEncounter, lootContext),
            lootContext => BattleRelicEffectRunner.FlushFeedback(lootContext, BattleLog));

        results.relicRewards = BattleRewardAggregationService.BuildRelicRewardsFromAggregation(aggregatedRelics);

        return results;
    }

    private static void ApplyRelicChoiceRefreshRelics(BattleEncounterData encounter, BattleSettlementRelicChoiceData relicChoice)
    {
        if (encounter == null || relicChoice == null || encounter.relicInventory == null)
            return;

        for (int i = 0; i < encounter.relicInventory.Count; i++)
        {
            RunRelicState relicState = encounter.relicInventory[i];
            Relic_SO relic = relicState != null ? relicState.relicData : null;
            if (relic == null || relicState.amount <= 0 || relic.builtInEffects == null)
                continue;

            for (int j = 0; j < relic.builtInEffects.Count; j++)
            {
                Relic_SO.RelicBuiltInEffectEntry effect = relic.builtInEffects[j];
                if (effect == null
                    || effect.trigger != Relic_SO.RelicTrigger.OnRelicChoiceRewardGenerated
                    || effect.effectType != Relic_SO.RelicBuiltInEffectType.IncreaseRelicChoiceRefreshCount)
                    continue;

                relicChoice.refreshesRemaining += Mathf.Max(0, effect.amount * Mathf.Max(1, relicState.amount));
            }
        }
    }

    private RelicTable_SO ResolveEliteRelicChoiceFallbackTable(BattleEncounterData encounter)
    {
        RelicTable_SO configuredTable = BattleEliteRelicChoiceBuilderService.ResolveFallbackTable(encounter, eliteRelicChoiceFallbackTable);
        if (configuredTable != null)
            return configuredTable;

#if UNITY_EDITOR
        if (!string.IsNullOrWhiteSpace(editorEliteRelicChoiceFallbackTableName))
        {
            RelicTable_SO editorFallback = BattleEliteRelicChoiceBuilderService.SelectFirstValidFallbackTable(
                BattleEditorAssetFallbackService.FindRelicTablesByName(editorEliteRelicChoiceFallbackTableName));
            if (editorFallback != null)
                return editorFallback;
        }
#endif

        return null;
    }

#if UNITY_EDITOR
    private bool TryBuildEditorEliteRelicChoiceFromAssets(
        EncounterDropResults results,
        System.Random rng,
        ICollection<Relic_SO> ownedRelics,
        int choiceCount)
    {
        if (results == null)
            return false;

        return BattleEliteRelicChoiceBuilderService.TryBuildAndApplyFromRelics(
            BattleEditorAssetFallbackService.FindAllRelics(),
            rng,
            ownedRelics,
            choiceCount,
            choiceData => results.relicChoice = choiceData);
    }

#endif

    private static int CountLiving(BattleAllyController[] allies)
    {
        return BattleLivingCountService.CountLiving(allies, ally => ally != null && !ally.IsDead);
    }

    private static int CountLiving(BattleEnemyController[] enemies)
    {
        return BattleLivingCountService.CountLiving(enemies, enemy => enemy != null && !enemy.IsDead);
    }
}
