using System;
using System.Collections.Generic;
using UnityEngine;

public class BattleEnemyController : MonoBehaviour
{
    [Tooltip("Primary speed button reference. When empty, buttons will be discovered from children.")]
    public SpeedButton speedButton;
    [SerializeField] private List<SpeedButton> speedButtons = new List<SpeedButton>();

    public EnemyData_SO enemyData;

    public int EnemyID => enemyData != null ? enemyData.enemyID : -1;
    public string EnemyName => enemyData != null ? enemyData.enemyName : "Unknown";
    public int BaseHP => enemyData != null ? enemyData.baseHP : 0;
    public float DamageReductionPercent => enemyData != null ? Mathf.Clamp(enemyData.damageReductionPercent, 0f, 100f) : 0f;
    public int MinSpeed => enemyData != null ? enemyData.GetStageMinSpeed(CurrentStageIndex) : 0;
    public int MaxSpeed => enemyData != null ? enemyData.GetStageMaxSpeed(CurrentStageIndex) : 0;
    public int BaseSpeedButtonCount => enemyData != null ? enemyData.GetStageSpeedButtonCount(CurrentStageIndex) : 0;
    public int EffectiveSpeedButtonCount => ResolveDesiredActiveSpeedButtonCount();

    public int Speed { get; private set; }
    public int CurrentHP { get; private set; }
    public int MaxHP { get; private set; }
    public int LastDamageModifierRatioPercent { get; private set; } = 100;
    public int LastPowerModifierAmount { get; private set; }
    public int CurrentStageIndex { get; private set; }
    public EnemySkill_SO SelectedSkill { get; private set; }
    public int DiceCount => SelectedSkill != null ? SelectedSkill.diceCount : 1;
    public bool IsDead { get; private set; }
    public static bool HasAnyAliveBleedDecayPreventer => aliveBleedDecayPreventerCount > 0;
    public int BurnStacks => GetLegacyStatusStacks(BurnStatusId);
    public int PendingRoundStartSpeedBonus { get; private set; }
    public IReadOnlyList<BattleStatusRuntimeEntry> StatusEntries => statusEntries;
    public IReadOnlyList<SpeedButton> SpeedButtons => activeSpeedButtons;
    public Vector3 LastHitDirection { get; private set; } = Vector3.up;
    public bool AppliedPendingStageTransitionThisRoundStart { get; private set; }

    public event Action<BattleEnemyController, int> Damaged;
    public event Action<BattleEnemyController> HealthChanged;
    public event Action<BattleEnemyController> Died;
    public event Action<BattleEnemyController, int> BurnChanged;
    public event Action<BattleEnemyController> StatusChanged;

    private readonly List<BattleStatusRuntimeEntry> statusEntries = new List<BattleStatusRuntimeEntry>();
    private readonly List<PendingBattleStatusApplication> pendingStatusApplications = new List<PendingBattleStatusApplication>();
    private readonly HashSet<int> consumedConditionalSkillRules = new HashSet<int>();
    private readonly List<EnemySkill_SO> selectedSkillsPerButton = new List<EnemySkill_SO>();
    private readonly List<SpeedButton> activeSpeedButtons = new List<SpeedButton>();
    private GameObject speedButtonTemplateObject;
    private SpeedButton.ButtonType speedButtonType = SpeedButton.ButtonType.Enemy;
    private Vector2 speedButtonBaseOffset = new Vector2(0f, 1f);
    private Vector2 speedButtonMultiGroupOffset = Vector2.zero;
    private float speedButtonHorizontalSpacing = 0.82f;
    private const string BurnStatusId = "burn";
    private static int aliveBleedDecayPreventerCount;
    private int pendingStageTransitionIndex = -1;
    private bool consumedHealthBasedStageTransition;
    private bool consumedRoundStartRetreat;
    private bool registeredAsBleedDecayPreventer;
    private int pendingNextAttackSkillPowerBonus;
    private int activeAttackSkillPowerBonus;

    private void Awake()
    {
        RefreshSpeedButtonsFromHierarchy();
        if (speedButton == null)
            Debug.LogWarning($"[BattleEnemyController] {gameObject.name} is missing a SpeedButton reference.");
        if (enemyData == null)
            Debug.LogWarning($"[BattleEnemyController] {gameObject.name} is missing EnemyData_SO.");

        CurrentStageIndex = 0;
        MaxHP = CurrentHP = ResolveAdjustedBaseMaxHP();
        ApplyCurrentStageVisual();
        RefreshBleedDecayPreventerRegistration();
    }

    public void ApplyEnemyData(EnemyData_SO data, bool resetHealth = true)
    {
        UnregisterBleedDecayPreventer();
        enemyData = data;
        consumedConditionalSkillRules.Clear();
        pendingStageTransitionIndex = -1;
        consumedHealthBasedStageTransition = false;
        consumedRoundStartRetreat = false;
        pendingNextAttackSkillPowerBonus = 0;
        activeAttackSkillPowerBonus = 0;
        IsDead = false;
        CurrentStageIndex = 0;
        if (enemyData == null)
            return;

        if (resetHealth)
        {
            MaxHP = CurrentHP = ResolveAdjustedBaseMaxHP();
        }

        ApplyCurrentStageVisual();
        RefreshActiveSpeedButtons();
        RefreshBleedDecayPreventerRegistration();
    }

    private void OnDisable()
    {
        UnregisterBleedDecayPreventer();
    }

    private void OnDestroy()
    {
        UnregisterBleedDecayPreventer();
    }

    private int ResolveAdjustedBaseMaxHP()
    {
        int baseHp = enemyData != null ? Mathf.Max(1, enemyData.baseHP) : 100;
        IReadOnlyList<RunCalamityState> activeCalamities = BattleEncounterRuntime.ActiveEncounter != null
            ? BattleEncounterRuntime.ActiveEncounter.activeCalamities
            : null;
        return Mathf.Max(1, CalamitySystem.ApplyEnemyMaxHpModifiers(baseHp, activeCalamities));
    }

    public void GenerateRandomSpeed(int min = 2, int max = 6)
    {
        if (IsDead)
        {
            Speed = 0;
            for (int i = 0; i < activeSpeedButtons.Count; i++)
            {
                if (activeSpeedButtons[i] != null)
                    activeSpeedButtons[i].Speed = 0;
            }
            return;
        }

        int effectiveMin = enemyData != null ? Mathf.Max(0, enemyData.GetStageMinSpeed(CurrentStageIndex)) : min;
        int effectiveMax = enemyData != null ? Mathf.Max(effectiveMin, enemyData.GetStageMaxSpeed(CurrentStageIndex)) : Mathf.Max(min, max);

        Speed = Mathf.Max(1, UnityEngine.Random.Range(effectiveMin, effectiveMax + 1) + PendingRoundStartSpeedBonus);
        for (int i = 0; i < activeSpeedButtons.Count; i++)
        {
            if (activeSpeedButtons[i] != null)
                activeSpeedButtons[i].Speed = Speed;
        }
    }

    public void AutoConnectToRandomAlly(List<SpeedButton> allyButtons)
    {
        if (IsDead || allyButtons == null || allyButtons.Count == 0)
            return;

        for (int i = 0; i < activeSpeedButtons.Count; i++)
        {
            SpeedButton button = activeSpeedButtons[i];
            if (button == null)
                continue;

            SpeedButton target = ResolvePreferredTargetButton(button, allyButtons);
            if (target == null)
                target = allyButtons[UnityEngine.Random.Range(0, allyButtons.Count)];
            button.EnemyDrawLineTo(target);
        }
    }

    public void RegisterSpeedButtons(IEnumerable<SpeedButton> buttons)
    {
        speedButtons.Clear();
        if (buttons != null)
        {
            foreach (SpeedButton button in buttons)
            {
                if (button != null && !speedButtons.Contains(button))
                    speedButtons.Add(button);
            }
        }

        speedButton = speedButtons.Count > 0 ? speedButtons[0] : null;
        if (speedButton != null)
            speedButtonTemplateObject = speedButton.gameObject;
        RefreshActiveSpeedButtons();
        EnsureSelectedSkillSlots();
    }

    public void ConfigureSpeedButtonLayout(
        SpeedButton templateButton,
        SpeedButton.ButtonType buttonType,
        Vector2 baseOffset,
        Vector2 multiGroupOffset,
        float horizontalSpacing)
    {
        if (templateButton != null)
            speedButtonTemplateObject = templateButton.gameObject;

        speedButtonType = buttonType;
        speedButtonBaseOffset = baseOffset;
        speedButtonMultiGroupOffset = multiGroupOffset;
        speedButtonHorizontalSpacing = horizontalSpacing;
        RefreshActiveSpeedButtons();
    }

    public void SelectRandomSkill(int roundNumber = -1)
    {
        SelectRandomSkills(roundNumber);
    }

    public void SelectRandomSkills(int roundNumber = -1)
    {
        if (IsDead)
        {
            SelectedSkill = null;
            selectedSkillsPerButton.Clear();
            return;
        }

        string roundPrefix = roundNumber > 0 ? $"[Round {roundNumber}] " : string.Empty;
        EnsureSelectedSkillSlots();
        activeAttackSkillPowerBonus = 0;
        Dictionary<int, int> selectedRuleCounts = new Dictionary<int, int>();
        List<int> consumedRuleIndicesThisRound = new List<int>();

        for (int slotIndex = 0; slotIndex < selectedSkillsPerButton.Count; slotIndex++)
        {
            EnemySkill_SO slotSkill = ResolveRandomSkillForSlot(roundPrefix, roundNumber, slotIndex, selectedRuleCounts, consumedRuleIndicesThisRound);
            selectedSkillsPerButton[slotIndex] = slotSkill;
        }

        for (int i = 0; i < consumedRuleIndicesThisRound.Count; i++)
            consumedConditionalSkillRules.Add(consumedRuleIndicesThisRound[i]);

        SelectedSkill = selectedSkillsPerButton.Count > 0 ? selectedSkillsPerButton[0] : null;
    }

    public int RollDice(int diceIndex = 0)
    {
        if (IsDead)
            return 1;

        EnemySkill_SO.DicePowerRange range = GetDicePowerRange(diceIndex);
        int minPower = Mathf.Max(1, range.minPower);
        int maxPower = Mathf.Max(minPower, range.maxPower);

        switch (ResolveForcedRollMode())
        {
            case ForcedRollMode.Min:
                return minPower;
            case ForcedRollMode.Max:
                return maxPower;
            default:
                return UnityEngine.Random.Range(minPower, maxPower + 1);
        }
    }

    public int RollCombatPower()
    {
        return RollDice(0);
    }

    public EnemySkill_SO.DicePresentationSettings GetDicePresentationSettings(int diceIndex)
    {
        if (SelectedSkill == null)
            return default;

        return SelectedSkill.GetDicePresentationSettings(diceIndex);
    }

    public EnemySkill_SO.DicePowerRange GetDicePowerRange(int diceIndex)
    {
        EnemySkill_SO.DicePowerRange range = GetDisplayedDicePowerRange(SelectedSkill, diceIndex);
        bool isDefenseSkill = SelectedSkill != null
            && SelectedSkill.GetDiceSkillCategory(diceIndex) == EnemySkill_SO.SkillCategory.Defense;
        return ApplyStatusDiceRangeModifiers(range, isDefenseSkill);
    }

    public EnemySkill_SO.DicePowerRange GetPreviewDicePowerRange(EnemySkill_SO skill, int diceIndex)
    {
        return ApplyQueuedAttackSkillPowerBonus(ResolveSkillBaseDiceRange(skill, diceIndex), skill, pendingNextAttackSkillPowerBonus);
    }

    public EnemySkill_SO.DicePowerRange GetDisplayedDicePowerRange(EnemySkill_SO skill, int diceIndex)
    {
        int queuedBonus = skill != null && skill == SelectedSkill
            ? activeAttackSkillPowerBonus
            : pendingNextAttackSkillPowerBonus;
        return ApplyQueuedAttackSkillPowerBonus(ResolveSkillBaseDiceRange(skill, diceIndex), skill, queuedBonus);
    }

    public void QueueNextAttackSkillPowerBonus(int bonusAmount)
    {
        if (bonusAmount <= 0)
            return;

        pendingNextAttackSkillPowerBonus += bonusAmount;
    }

    public string GetDebugDisplayName()
    {
        return EnemyID >= 0 ? $"{gameObject.name}(ID:{EnemyID})" : gameObject.name;
    }

    public string GetSelectedSkillDebugSummary()
    {
        if (SelectedSkill == null)
            return "Default Skill(1 die, range: 1~1)";

        return $"{SelectedSkill.skillName}({SelectedSkill.diceCount} dice, ranges: {BuildDiceRangeSummary(SelectedSkill)})";
    }

    public string GetSelectedSkillDebugSummary(SpeedButton button)
    {
        EnemySkill_SO skill = GetSelectedSkillForButton(button);
        if (skill == null)
            return "Default Skill(1 die, range: 1~1)";

        return $"{skill.skillName}({skill.diceCount} dice, ranges: {BuildDiceRangeSummary(skill)})";
    }

    public EnemySkill_SO GetSelectedSkillForButton(SpeedButton button)
    {
        int buttonIndex = GetSpeedButtonIndex(button);
        return GetSelectedSkillForButtonIndex(buttonIndex);
    }

    public EnemySkill_SO GetSelectedSkillForButtonIndex(int buttonIndex)
    {
        EnsureSelectedSkillSlots();
        if (buttonIndex < 0 || buttonIndex >= selectedSkillsPerButton.Count)
            return SelectedSkill;

        return selectedSkillsPerButton[buttonIndex];
    }

    public void ActivateSelectedSkillForButton(SpeedButton button)
    {
        SelectedSkill = GetSelectedSkillForButton(button);
        if (SelectedSkill != null && !SelectedSkill.IsDefenseSkill)
        {
            activeAttackSkillPowerBonus = pendingNextAttackSkillPowerBonus;
            pendingNextAttackSkillPowerBonus = 0;
        }
        else
        {
            activeAttackSkillPowerBonus = 0;
        }
    }

    public int TakeDamage(int amount, int powerModifierAmount = 0, int originalReferenceDamage = -1)
    {
        LastPowerModifierAmount = powerModifierAmount;
        if (IsDead)
            return 0;

        if (amount <= 0)
        {
            LastDamageModifierRatioPercent = 0;
            return 0;
        }

        int originalDamage = originalReferenceDamage > 0 ? originalReferenceDamage : amount;
        amount = ApplyIncomingDamageStatusModifiers(amount);
        amount = ApplyBaseDamageReduction(amount);
        if (amount <= 0)
        {
            LastDamageModifierRatioPercent = 0;
            return 0;
        }

        int previousHP = CurrentHP;
        int resultingHP = Mathf.Max(0, CurrentHP - amount);
        resultingHP = ApplyHealthBasedStageTransitionRule(previousHP, resultingHP);
        resultingHP = ApplyLastStandRule(resultingHP);
        CurrentHP = resultingHP;
        int actualDamage = previousHP - CurrentHP;
        if (actualDamage <= 0)
        {
            LastDamageModifierRatioPercent = 0;
            return 0;
        }

        LastDamageModifierRatioPercent = originalDamage > 0
            ? Mathf.Max(0, Mathf.FloorToInt((actualDamage / (float)originalDamage) * 100f))
            : 100;

        Damaged?.Invoke(this, actualDamage);
        HealthChanged?.Invoke(this);
        TryCollapseLinkedLastStandEnemies();

        if (CurrentHP <= 0)
            Die();

        return actualDamage;
    }

    public bool TryTransitionToStage(int targetStageIndex, bool nextRound = false)
    {
        if (enemyData == null || enemyData.StageCount <= 1)
            return false;

        int clampedIndex = Mathf.Clamp(targetStageIndex, 0, enemyData.StageCount - 1);
        if (nextRound)
        {
            if (clampedIndex == CurrentStageIndex)
                return false;

            pendingStageTransitionIndex = clampedIndex;
            return true;
        }

        return ApplyStageTransitionImmediate(clampedIndex);
    }

    public bool TryConsumeRoundStartRetreat(int roundNumber, out IReadOnlyList<LootTable_SO> settlementLootTables)
    {
        settlementLootTables = Array.Empty<LootTable_SO>();
        if (IsDead || enemyData == null || consumedRoundStartRetreat)
            return false;

        EnemyData_SO.RoundStartRetreatRule rule = enemyData.roundStartRetreatRule;
        if (!rule.enabled)
            return false;

        int triggerRound = Mathf.Max(1, rule.triggerRound);
        if (roundNumber != triggerRound)
            return false;

        consumedRoundStartRetreat = true;
        settlementLootTables = rule.settlementLootTables ?? (IReadOnlyList<LootTable_SO>)Array.Empty<LootTable_SO>();
        return true;
    }

    public Sprite GetCurrentStageSprite()
    {
        return enemyData != null ? enemyData.GetStageSprite(CurrentStageIndex) : null;
    }

    private int ApplyIncomingDamageStatusModifiers(int baseDamage)
    {
        if (baseDamage <= 0 || statusEntries.Count == 0)
            return Mathf.Max(0, baseDamage);

        float multiplier = 1f;
        for (int i = 0; i < statusEntries.Count; i++)
        {
            BattleStatusRuntimeEntry entry = statusEntries[i];
            if (entry == null || entry.stacks <= 0 || entry.statusData == null)
                continue;
            if (IsStatusEffectSuppressed(entry))
                continue;

            if (entry.statusData.BehaviorType != BattleStatus_SO.StatusBehaviorType.DamageTakenBonusPercent)
                continue;

            float percentBonusPerStack = Mathf.Max(0, entry.statusData.PowerPerStack) * 0.01f;
            multiplier += entry.stacks * percentBonusPerStack;
        }

        float bonusMultiplier = Mathf.Max(0f, multiplier - 1f);
        int bonusDamage = Mathf.FloorToInt(baseDamage * bonusMultiplier);
        return Mathf.Max(0, baseDamage + bonusDamage);
    }

    private int ApplyBaseDamageReduction(int damage)
    {
        if (damage <= 0)
            return 0;

        float reductionMultiplier = 1f - (DamageReductionPercent * 0.01f);
        reductionMultiplier = Mathf.Clamp01(reductionMultiplier);
        return Mathf.Max(0, Mathf.FloorToInt(damage * reductionMultiplier));
    }

    private EnemySkill_SO.DicePowerRange ApplyStatusDiceRangeModifiers(EnemySkill_SO.DicePowerRange baseRange, bool isDefenseSkill)
    {
        int flatPowerBonus = isDefenseSkill
            ? ResolveStatusFlatPowerAmount(BattleStatus_SO.StatusBehaviorType.DefensePowerBonusFlat)
            : ResolveStatusFlatPowerAmount(BattleStatus_SO.StatusBehaviorType.PowerBonusFlat);
        int flatPowerPenalty = isDefenseSkill
            ? ResolveStatusFlatPowerAmount(BattleStatus_SO.StatusBehaviorType.DefensePowerPenaltyFlat)
            : ResolveStatusFlatPowerAmount(BattleStatus_SO.StatusBehaviorType.PowerPenaltyFlat);
        int netPowerAdjustment = flatPowerBonus - flatPowerPenalty;
        int minPowerPenalty = ResolveStatusMinPowerPenalty();
        int maxPowerPenalty = ResolveStatusMaxPowerPenalty();
        int minPower = Mathf.Max(1, baseRange.minPower + netPowerAdjustment - minPowerPenalty);
        int maxPower = Mathf.Max(minPower, baseRange.maxPower + netPowerAdjustment - maxPowerPenalty);
        return new EnemySkill_SO.DicePowerRange
        {
            minPower = minPower,
            maxPower = maxPower
        };
    }

    private int ResolveStatusMinPowerPenalty()
    {
        if (statusEntries.Count == 0)
            return 0;

        int totalPenalty = 0;
        for (int i = 0; i < statusEntries.Count; i++)
        {
            BattleStatusRuntimeEntry entry = statusEntries[i];
            if (entry == null || entry.stacks <= 0 || entry.statusData == null)
                continue;
            if (IsStatusEffectSuppressed(entry))
                continue;
            if (entry.statusData.BehaviorType != BattleStatus_SO.StatusBehaviorType.MinPowerPenalty)
                continue;

            totalPenalty += entry.stacks * Mathf.Max(0, entry.statusData.PowerPerStack);
        }

        return Mathf.Max(0, totalPenalty);
    }

    private int ResolveStatusMaxPowerPenalty()
    {
        if (statusEntries.Count == 0)
            return 0;

        int totalPenalty = 0;
        for (int i = 0; i < statusEntries.Count; i++)
        {
            BattleStatusRuntimeEntry entry = statusEntries[i];
            if (entry == null || entry.stacks <= 0 || entry.statusData == null)
                continue;
            if (IsStatusEffectSuppressed(entry))
                continue;
            if (entry.statusData.BehaviorType != BattleStatus_SO.StatusBehaviorType.MaxPowerPenalty)
                continue;

            totalPenalty += entry.stacks * Mathf.Max(0, entry.statusData.PowerPerStack);
        }

        return Mathf.Max(0, totalPenalty);
    }

    private ForcedRollMode ResolveForcedRollMode()
    {
        bool forceMax = false;
        for (int i = 0; i < statusEntries.Count; i++)
        {
            BattleStatusRuntimeEntry entry = statusEntries[i];
            if (entry == null || entry.stacks <= 0 || entry.statusData == null)
                continue;
            if (IsStatusEffectSuppressed(entry))
                continue;

            switch (entry.statusData.BehaviorType)
            {
                case BattleStatus_SO.StatusBehaviorType.ForceRollToMin:
                    return ForcedRollMode.Min;
                case BattleStatus_SO.StatusBehaviorType.ForceRollToMax:
                    forceMax = true;
                    break;
            }
        }

        return forceMax ? ForcedRollMode.Max : ForcedRollMode.None;
    }

    private enum ForcedRollMode
    {
        None = 0,
        Min = 1,
        Max = 2
    }

    private int ResolveStatusFlatPowerAmount(BattleStatus_SO.StatusBehaviorType behaviorType)
    {
        if (statusEntries.Count == 0)
            return 0;

        int totalAmount = 0;
        for (int i = 0; i < statusEntries.Count; i++)
        {
            BattleStatusRuntimeEntry entry = statusEntries[i];
            if (entry == null || entry.stacks <= 0 || entry.statusData == null)
                continue;
            if (IsStatusEffectSuppressed(entry))
                continue;
            if (entry.statusData.BehaviorType != behaviorType)
                continue;

            totalAmount += entry.stacks * Mathf.Max(0, entry.statusData.PowerPerStack);
        }

        return Mathf.Max(0, totalAmount);
    }

    public int GetStatusFlatPowerModifier(bool isDefenseSkill)
    {
        int flatPowerBonus = isDefenseSkill
            ? ResolveStatusFlatPowerAmount(BattleStatus_SO.StatusBehaviorType.DefensePowerBonusFlat)
            : ResolveStatusFlatPowerAmount(BattleStatus_SO.StatusBehaviorType.PowerBonusFlat);
        int flatPowerPenalty = isDefenseSkill
            ? ResolveStatusFlatPowerAmount(BattleStatus_SO.StatusBehaviorType.DefensePowerPenaltyFlat)
            : ResolveStatusFlatPowerAmount(BattleStatus_SO.StatusBehaviorType.PowerPenaltyFlat);
        return flatPowerBonus - flatPowerPenalty;
    }

    public int Heal(int amount)
    {
        if (IsDead || amount <= 0)
            return 0;

        int previousHP = CurrentHP;
        CurrentHP = Mathf.Clamp(CurrentHP + amount, 0, MaxHP);
        int actualHeal = CurrentHP - previousHP;
        if (actualHeal <= 0)
            return 0;

        HealthChanged?.Invoke(this);
        return actualHeal;
    }

    public void RegisterLastHitDirection(Vector3 direction)
    {
        if (direction.sqrMagnitude <= 0.0001f)
            return;

        LastHitDirection = direction.normalized;
    }

    public void AddBurn(int stackAmount)
    {
        AddStatus(null, BurnStatusId, "燃烧", stackAmount);
    }

    public int ConsumeBurnDamageForRound()
    {
        BattleStatusTickResult burnTick = default;
        List<BattleStatusTickResult> ticks = ConsumeRoundStartStatusEffects();
        for (int i = 0; i < ticks.Count; i++)
        {
            if (ticks[i].statusId == BurnStatusId)
            {
                burnTick = ticks[i];
                break;
            }
        }

        if (burnTick.damage > 0)
            BurnChanged?.Invoke(this, BurnStacks);
        return burnTick.damage;
    }

    public bool AddStatus(BattleStatus_SO statusData, string fallbackStatusId, string fallbackDisplayName, int stackAmount)
    {
        if (IsDead || stackAmount <= 0)
            return false;
        if (!CanReceiveStatusApplication(statusData, fallbackStatusId))
            return false;

        BattleStatusRuntimeEntry entry = GetOrCreateStatusEntry(statusData, fallbackStatusId, fallbackDisplayName);
        entry.stacks = Mathf.Max(0, entry.stacks + stackAmount);
        int minimumStacks = RelicSystem.ResolveEnemyMinimumStatusStacks(entry.Key);
        if (minimumStacks > 0)
            entry.stacks = Mathf.Max(entry.stacks, minimumStacks);
        NotifyStatusChanged(entry);
        if (IsSpeedButtonPenaltyStatus(entry))
            RefreshActiveSpeedButtons();
        return true;
    }

    public bool QueueStatusForRoundStart(BattleStatus_SO statusData, string fallbackStatusId, string fallbackDisplayName, int stackAmount, int roundStartDelay, int repeatCount)
    {
        if (IsDead || stackAmount <= 0)
            return false;
        if (!CanReceiveStatusApplication(statusData, fallbackStatusId))
            return false;

        int safeDelay = Mathf.Max(1, roundStartDelay);
        int safeRepeatCount = Mathf.Max(1, repeatCount);

        string key = ResolveStatusKey(statusData, fallbackStatusId, fallbackDisplayName);
        for (int i = 0; i < pendingStatusApplications.Count; i++)
        {
            PendingBattleStatusApplication pending = pendingStatusApplications[i];
            string pendingKey = ResolveStatusKey(pending.statusData, pending.fallbackStatusId, pending.displayName);
            if (!string.Equals(pendingKey, key, StringComparison.OrdinalIgnoreCase))
                continue;

            if (pending.remainingRoundStarts != safeDelay || pending.repeatCountRemaining != safeRepeatCount)
                continue;

            pending.stacks = Mathf.Max(0, pending.stacks + stackAmount);
            if (statusData != null)
                pending.statusData = statusData;
            if (!string.IsNullOrWhiteSpace(fallbackStatusId))
                pending.fallbackStatusId = fallbackStatusId;
            if (!string.IsNullOrWhiteSpace(fallbackDisplayName))
                pending.displayName = fallbackDisplayName;
            return true;
        }

        pendingStatusApplications.Add(new PendingBattleStatusApplication
        {
            statusData = statusData,
            fallbackStatusId = fallbackStatusId,
            displayName = fallbackDisplayName,
            stacks = Mathf.Max(0, stackAmount),
            remainingRoundStarts = safeDelay,
            repeatCountRemaining = safeRepeatCount
        });
        return true;
    }

    public int GetStatusStacks(BattleStatus_SO statusData)
    {
        if (statusData == null)
            return 0;

        BattleStatusRuntimeEntry entry = FindStatusEntry(statusData.StatusId);
        return entry != null ? Mathf.Max(0, entry.stacks) : 0;
    }

    public int GetStatusStacks(string fallbackStatusId)
    {
        if (string.IsNullOrWhiteSpace(fallbackStatusId))
            return 0;

        BattleStatusRuntimeEntry entry = FindStatusEntry(fallbackStatusId);
        return entry != null ? Mathf.Max(0, entry.stacks) : 0;
    }

    public void ClearStatus(BattleStatus_SO statusData, string fallbackStatusId, string fallbackDisplayName = null)
    {
        string key = ResolveStatusKey(statusData, fallbackStatusId, fallbackDisplayName);
        BattleStatusRuntimeEntry entry = FindStatusEntry(key);
        if (entry == null || entry.stacks <= 0)
            return;

        entry.stacks = 0;
        NotifyStatusChanged(entry);
        if (IsSpeedButtonPenaltyStatus(entry))
            RefreshActiveSpeedButtons();
    }

    public int RemoveStatusStacks(BattleStatus_SO statusData, string fallbackStatusId, int stackAmount, string fallbackDisplayName = null)
    {
        if (stackAmount <= 0)
            return 0;

        string key = ResolveStatusKey(statusData, fallbackStatusId, fallbackDisplayName);
        BattleStatusRuntimeEntry entry = FindStatusEntry(key);
        if (entry == null || entry.stacks <= 0)
            return 0;

        int removedStacks = Mathf.Min(entry.stacks, stackAmount);
        entry.stacks = Mathf.Max(0, entry.stacks - removedStacks);
        NotifyStatusChanged(entry);
        if (IsSpeedButtonPenaltyStatus(entry))
            RefreshActiveSpeedButtons();
        return removedStacks;
    }

    public List<BattleStatusTickResult> ConsumeRoundStartStatusEffects()
    {
        List<BattleStatusTickResult> results = new List<BattleStatusTickResult>();
        if (IsDead)
        {
            PendingRoundStartSpeedBonus = 0;
            AppliedPendingStageTransitionThisRoundStart = false;
            return results;
        }

        PendingRoundStartSpeedBonus = 0;
        AppliedPendingStageTransitionThisRoundStart = false;
        ApplyPendingStageTransitionIfNeeded();
        ResolvePendingStatusesAtRoundStart();
        if (statusEntries.Count == 0)
            return results;

        for (int i = 0; i < statusEntries.Count; i++)
        {
            BattleStatusRuntimeEntry entry = statusEntries[i];
            if (entry == null || entry.stacks <= 0)
                continue;

            int stacksBeforeTick = entry.stacks;
            int damage = ResolveRoundStartStatusDamage(entry);
            int speedBonus = ResolveRoundStartStatusSpeedBonus(entry);
            int appliedStacks = ResolveRoundStartLinkedStatusAmount(entry);
            BattleStatus_SO appliedStatusData = entry.statusData != null ? entry.statusData.LinkedStatusData : null;
            string appliedStatusName = appliedStatusData != null ? appliedStatusData.DisplayName : string.Empty;

            if (appliedStacks > 0 && appliedStatusData != null)
                AddStatus(appliedStatusData, appliedStatusData.StatusId, appliedStatusData.DisplayName, appliedStacks);

            if (damage <= 0 && speedBonus == 0 && appliedStacks <= 0)
                continue;

            if (damage > 0)
                TakeDamage(damage);
            if (speedBonus != 0)
                PendingRoundStartSpeedBonus += speedBonus;
            entry.stacks = ResolveRoundStartStatusStacksAfterTick(entry, stacksBeforeTick);
            if (string.Equals(entry.Key, BurnStatusId, StringComparison.OrdinalIgnoreCase))
                BurnChanged?.Invoke(this, Mathf.Max(0, entry.stacks));
            results.Add(new BattleStatusTickResult
            {
                statusData = entry.statusData,
                statusId = entry.Key,
                displayName = entry.DisplayName,
                stacks = Mathf.Max(0, entry.stacks),
                damage = damage,
                speedBonus = speedBonus,
                appliedStatusData = appliedStatusData,
                appliedStatusName = appliedStatusName,
                appliedStacks = appliedStacks
            });
        }

        if (results.Count > 0)
            StatusChanged?.Invoke(this);

        return results;
    }

    public List<BattleStatusTickResult> ConsumeOnHitStatusEffects()
    {
        List<BattleStatusTickResult> results = new List<BattleStatusTickResult>();
        if (statusEntries.Count == 0)
            return results;

        for (int i = 0; i < statusEntries.Count; i++)
        {
            BattleStatusRuntimeEntry entry = statusEntries[i];
            if (entry == null || entry.stacks <= 0)
                continue;

            int stacksBeforeTrigger = entry.stacks;
            int damage = ResolveOnHitStatusDamage(entry);
            if (damage <= 0)
                continue;

            TakeDamage(damage);
            entry.stacks = ResolveOnHitStatusStacksAfterTrigger(entry, stacksBeforeTrigger);
            results.Add(new BattleStatusTickResult
            {
                statusData = entry.statusData,
                statusId = entry.Key,
                displayName = entry.DisplayName,
                stacks = Mathf.Max(0, entry.stacks),
                damage = damage,
                speedBonus = 0
            });
        }

        if (results.Count > 0)
            StatusChanged?.Invoke(this);

        return results;
    }

    public bool ConsumeRoundEndStatusEffects()
    {
        if (IsDead || statusEntries.Count == 0)
            return false;

        bool changed = false;
        bool speedButtonCountChanged = false;
        for (int i = 0; i < statusEntries.Count; i++)
        {
            BattleStatusRuntimeEntry entry = statusEntries[i];
            if (entry == null || entry.stacks <= 0)
                continue;

            int stacksAfterDecay = ResolveRoundEndStatusStacks(entry, entry.stacks);
            if (stacksAfterDecay == entry.stacks)
                continue;

            entry.stacks = stacksAfterDecay;
            changed = true;
            if (IsSpeedButtonPenaltyStatus(entry))
                speedButtonCountChanged = true;
            if (string.Equals(entry.Key, BurnStatusId, StringComparison.OrdinalIgnoreCase))
                BurnChanged?.Invoke(this, Mathf.Max(0, entry.stacks));
        }

        if (speedButtonCountChanged)
            RefreshActiveSpeedButtons();
        if (changed)
            StatusChanged?.Invoke(this);

        return changed;
    }

    private void Die()
    {
        if (IsDead)
            return;

        IsDead = true;
        UnregisterBleedDecayPreventer();
        Speed = 0;
        SelectedSkill = null;
        selectedSkillsPerButton.Clear();

        for (int i = 0; i < activeSpeedButtons.Count; i++)
        {
            if (activeSpeedButtons[i] == null)
                continue;

            activeSpeedButtons[i].IsInteractable = false;
            activeSpeedButtons[i].HideForCombat();
        }

        Died?.Invoke(this);
    }

    private void RefreshBleedDecayPreventerRegistration()
    {
        bool shouldRegister = !IsDead && isActiveAndEnabled && enemyData != null && enemyData.preventAllyBleedDecayOnHitWhileAlive;
        if (shouldRegister == registeredAsBleedDecayPreventer)
            return;

        if (shouldRegister)
        {
            aliveBleedDecayPreventerCount += 1;
            registeredAsBleedDecayPreventer = true;
            return;
        }

        UnregisterBleedDecayPreventer();
    }

    private void UnregisterBleedDecayPreventer()
    {
        if (!registeredAsBleedDecayPreventer)
            return;

        aliveBleedDecayPreventerCount = Mathf.Max(0, aliveBleedDecayPreventerCount - 1);
        registeredAsBleedDecayPreventer = false;
    }

    private EnemySkill_SO ResolveRandomSkillForSlot(
        string roundPrefix,
        int roundNumber,
        int slotIndex,
        Dictionary<int, int> selectedRuleCounts,
        List<int> consumedRuleIndicesThisRound)
    {
        EnemySkill_SO conditionalSkill = TrySelectConditionalSkill(
            roundPrefix,
            roundNumber,
            slotIndex,
            selectedRuleCounts,
            consumedRuleIndicesThisRound);
        if (conditionalSkill != null)
            return conditionalSkill;

        if (enemyData == null || enemyData.skillPool == null || enemyData.skillPool.Count == 0)
        {
            Debug.LogWarning($"[BattleEnemyController] {roundPrefix}{gameObject.name} skill pool is empty, using fallback power 1.");
            return null;
        }

        List<EnemySkill_SO> usableSkills = new List<EnemySkill_SO>();
        for (int i = 0; i < enemyData.skillPool.Count; i++)
        {
            EnemySkill_SO candidate = enemyData.skillPool[i];
            if (candidate == null || !candidate.IsUsableInEnemyStage(CurrentStageIndex))
                continue;

            usableSkills.Add(candidate);
        }

        if (usableSkills.Count == 0)
        {
            Debug.LogWarning($"[BattleEnemyController] {roundPrefix}{GetDebugDisplayName()} has no normal skill usable in stage {CurrentStageIndex + 1}. Falling back to power 1.");
            return null;
        }

        EnemySkill_SO randomSkill = usableSkills[UnityEngine.Random.Range(0, usableSkills.Count)];
        string slotLabel = selectedSkillsPerButton.Count > 1 ? $" [Slot {slotIndex + 1}]" : string.Empty;
        Debug.Log($"[BattleEnemyController] {roundPrefix}{GetDebugDisplayName()}{slotLabel} selected random skill: {BuildSkillSummary(randomSkill)}{BuildSkillDescriptionSuffix(randomSkill)}");
        return randomSkill;
    }

    private int ApplyHealthBasedStageTransitionRule(int previousHP, int resultingHP)
    {
        if (enemyData == null
            || consumedHealthBasedStageTransition
            || !enemyData.enableHealthBasedStageTransition
            || enemyData.StageCount <= 1)
            return resultingHP;

        int targetStageIndex = Mathf.Clamp(enemyData.healthBasedTransitionTargetStageIndex, 0, enemyData.StageCount - 1);
        if (targetStageIndex == CurrentStageIndex)
            return resultingHP;

        int thresholdHP = Mathf.CeilToInt(MaxHP * Mathf.Clamp01(enemyData.healthBasedTransitionThresholdNormalized));
        thresholdHP = Mathf.Clamp(thresholdHP, 1, MaxHP);
        bool crossedThreshold = previousHP > thresholdHP && resultingHP <= thresholdHP;
        bool alreadyBelowThreshold = previousHP <= thresholdHP && resultingHP > 0;
        if (!crossedThreshold && !alreadyBelowThreshold)
            return resultingHP;

        if (enemyData.lockHPAtHealthTransitionThreshold && crossedThreshold)
            resultingHP = Mathf.Max(resultingHP, thresholdHP);

        pendingStageTransitionIndex = targetStageIndex;
        consumedHealthBasedStageTransition = true;
        return resultingHP;
    }

    private void ApplyPendingStageTransitionIfNeeded()
    {
        if (pendingStageTransitionIndex < 0)
            return;

        int targetStageIndex = pendingStageTransitionIndex;
        pendingStageTransitionIndex = -1;
        AppliedPendingStageTransitionThisRoundStart = ApplyStageTransitionImmediate(targetStageIndex);
    }

    private bool ApplyStageTransitionImmediate(int targetStageIndex)
    {
        if (enemyData == null || enemyData.StageCount <= 1)
            return false;

        int clampedIndex = Mathf.Clamp(targetStageIndex, 0, enemyData.StageCount - 1);
        if (clampedIndex == CurrentStageIndex)
            return false;

        CurrentStageIndex = clampedIndex;
        ApplyCurrentStageVisual();
        RefreshActiveSpeedButtons();
        return true;
    }

    public bool TryApplyStatusDrivenStageTransition(BattleAllyController[] allies, BattleEnemyController[] enemies)
    {
        if (enemyData == null
            || enemyData.StageCount <= 1
            || enemyData.statusStageTransitionRules == null
            || enemyData.statusStageTransitionRules.Count == 0)
            return false;

        int selectedTargetStage = -1;
        int selectedPriority = int.MinValue;
        string selectedRuleName = string.Empty;

        for (int i = 0; i < enemyData.statusStageTransitionRules.Count; i++)
        {
            EnemyData_SO.StatusStageTransitionRule rule = enemyData.statusStageTransitionRules[i];
            if (rule.statusData == null)
                continue;

            int targetStageIndex = Mathf.Clamp(rule.targetStageIndex, 0, enemyData.StageCount - 1);
            if (targetStageIndex == CurrentStageIndex)
                continue;

            if (!IsStatusStageTransitionRuleSatisfied(rule, allies, enemies))
                continue;

            if (selectedTargetStage < 0 || rule.priority > selectedPriority)
            {
                selectedTargetStage = targetStageIndex;
                selectedPriority = rule.priority;
                selectedRuleName = string.IsNullOrWhiteSpace(rule.ruleName) ? $"Status Rule {i + 1}" : rule.ruleName;
            }
        }

        if (selectedTargetStage < 0)
            return false;

        bool transitioned = ApplyStageTransitionImmediate(selectedTargetStage);
        if (transitioned)
            Debug.Log($"[BattleEnemyController] {GetDebugDisplayName()} transitioned to stage {selectedTargetStage + 1} by {selectedRuleName}.");
        return transitioned;
    }

    private EnemySkill_SO TrySelectConditionalSkill(
        string roundPrefix,
        int roundNumber,
        int slotIndex,
        Dictionary<int, int> selectedRuleCounts,
        List<int> consumedRuleIndicesThisRound)
    {
        if (enemyData == null || enemyData.conditionalSkillRules == null || enemyData.conditionalSkillRules.Count == 0)
            return null;

        int selectedRuleIndex = -1;
        int selectedPriority = int.MinValue;
        EnemySkill_SO selectedRuleSkill = null;

        for (int i = 0; i < enemyData.conditionalSkillRules.Count; i++)
        {
            EnemyData_SO.ConditionalSkillRule rule = enemyData.conditionalSkillRules[i];
            if (rule.skill == null)
                continue;
            if (!rule.skill.IsUsableInEnemyStage(CurrentStageIndex))
                continue;
            if (rule.useOncePerBattle && consumedConditionalSkillRules.Contains(i))
                continue;
            if (selectedRuleCounts != null &&
                selectedRuleCounts.TryGetValue(i, out int currentCount) &&
                currentCount >= Mathf.Max(1, rule.maxSlotsPerRound))
                continue;
            if (!IsConditionalSkillRuleSatisfied(rule, roundNumber))
                continue;

            if (selectedRuleSkill == null || rule.priority > selectedPriority)
            {
                selectedRuleIndex = i;
                selectedPriority = rule.priority;
                selectedRuleSkill = rule.skill;
            }
        }

        if (selectedRuleSkill == null)
            return null;

        if (selectedRuleCounts != null)
        {
            selectedRuleCounts.TryGetValue(selectedRuleIndex, out int currentCount);
            selectedRuleCounts[selectedRuleIndex] = currentCount + 1;
        }

        if (enemyData.conditionalSkillRules[selectedRuleIndex].useOncePerBattle &&
            consumedRuleIndicesThisRound != null &&
            !consumedRuleIndicesThisRound.Contains(selectedRuleIndex))
        {
            consumedRuleIndicesThisRound.Add(selectedRuleIndex);
        }

        string ruleLabel = string.IsNullOrWhiteSpace(enemyData.conditionalSkillRules[selectedRuleIndex].ruleName)
            ? $"Rule {selectedRuleIndex + 1}"
            : enemyData.conditionalSkillRules[selectedRuleIndex].ruleName;
        string slotLabel = selectedSkillsPerButton.Count > 1 ? $" [Slot {slotIndex + 1}]" : string.Empty;
        int slotLimit = Mathf.Max(1, enemyData.conditionalSkillRules[selectedRuleIndex].maxSlotsPerRound);
        Debug.Log($"[BattleEnemyController] {roundPrefix}{GetDebugDisplayName()}{slotLabel} selected conditional skill by {ruleLabel} (max slots this round: {slotLimit}): {BuildSkillSummary(selectedRuleSkill)}{BuildSkillDescriptionSuffix(selectedRuleSkill)}");
        return selectedRuleSkill;
    }

    private void EnsureSelectedSkillSlots()
    {
        int desiredCount = Mathf.Max(1, activeSpeedButtons.Count);
        while (selectedSkillsPerButton.Count < desiredCount)
            selectedSkillsPerButton.Add(null);
        while (selectedSkillsPerButton.Count > desiredCount)
            selectedSkillsPerButton.RemoveAt(selectedSkillsPerButton.Count - 1);
    }

    private int GetSpeedButtonIndex(SpeedButton button)
    {
        if (button == null)
            return 0;

        for (int i = 0; i < activeSpeedButtons.Count; i++)
        {
            if (activeSpeedButtons[i] == button)
                return i;
        }

        return 0;
    }

    private static string BuildSkillSummary(EnemySkill_SO skill)
    {
        if (skill == null)
            return "Default Skill(1 die, range: 1~1)";

        return $"{skill.skillName}({skill.diceCount} dice, ranges: {BuildDiceRangeSummary(skill)})";
    }

    private bool IsConditionalSkillRuleSatisfied(EnemyData_SO.ConditionalSkillRule rule, int roundNumber)
    {
        switch (rule.conditionType)
        {
            case EnemyData_SO.ConditionalSkillConditionType.HealthBelowPercent:
                if (MaxHP <= 0)
                    return false;

                float currentRatio = (float)CurrentHP / MaxHP;
                return currentRatio <= Mathf.Clamp01(rule.healthThresholdNormalized);
            case EnemyData_SO.ConditionalSkillConditionType.RoundSequence:
                if (roundNumber <= 0)
                    return false;

                int firstRound = Mathf.Max(1, rule.firstTriggerRound);
                int repeatInterval = Mathf.Max(1, rule.repeatEveryNRounds);
                if (roundNumber < firstRound)
                    return false;

                return (roundNumber - firstRound) % repeatInterval == 0;
            case EnemyData_SO.ConditionalSkillConditionType.AliveEnemyCountAtMost:
                return CountAliveEnemiesOnBattlefield() <= Mathf.Max(1, rule.requiredAliveEnemyCount);
            default:
                return false;
        }
    }

    private int CountAliveEnemiesOnBattlefield()
    {
        BattleEnemyController[] enemies = FindObjectsOfType<BattleEnemyController>(false);
        int aliveCount = 0;
        for (int i = 0; i < enemies.Length; i++)
        {
            BattleEnemyController enemy = enemies[i];
            if (enemy != null && !enemy.IsDead)
                aliveCount++;
        }

        return aliveCount;
    }

    private bool IsStatusStageTransitionRuleSatisfied(
        EnemyData_SO.StatusStageTransitionRule rule,
        BattleAllyController[] allies,
        BattleEnemyController[] enemies)
    {
        int comparisonThreshold = Mathf.Max(0, rule.stackThreshold);
        switch (rule.targetMode)
        {
            case EnemyData_SO.StageStatusTargetMode.Self:
                return CompareStatusStacks(GetStatusStacks(rule.statusData), rule.comparisonType, comparisonThreshold);
            case EnemyData_SO.StageStatusTargetMode.AnyUnit:
                return AnyUnitMatchesStatusCondition(rule, allies, enemies, comparisonThreshold);
            case EnemyData_SO.StageStatusTargetMode.SpecificEnemy:
                return SpecificEnemyMatchesStatusCondition(rule, enemies, comparisonThreshold);
            case EnemyData_SO.StageStatusTargetMode.SpecificAlly:
                return SpecificAllyMatchesStatusCondition(rule, allies, comparisonThreshold);
            default:
                return false;
        }
    }

    private bool AnyUnitMatchesStatusCondition(
        EnemyData_SO.StatusStageTransitionRule rule,
        BattleAllyController[] allies,
        BattleEnemyController[] enemies,
        int comparisonThreshold)
    {
        if (CompareStatusStacks(GetStatusStacks(rule.statusData), rule.comparisonType, comparisonThreshold))
            return true;

        if (allies != null)
        {
            for (int i = 0; i < allies.Length; i++)
            {
                BattleAllyController ally = allies[i];
                if (ally == null || ally.IsDead)
                    continue;
                if (CompareStatusStacks(ally.GetStatusStacks(rule.statusData), rule.comparisonType, comparisonThreshold))
                    return true;
            }
        }

        if (enemies != null)
        {
            for (int i = 0; i < enemies.Length; i++)
            {
                BattleEnemyController enemy = enemies[i];
                if (enemy == null || enemy == this || enemy.IsDead)
                    continue;
                if (CompareStatusStacks(enemy.GetStatusStacks(rule.statusData), rule.comparisonType, comparisonThreshold))
                    return true;
            }
        }

        return false;
    }

    private bool SpecificEnemyMatchesStatusCondition(
        EnemyData_SO.StatusStageTransitionRule rule,
        BattleEnemyController[] enemies,
        int comparisonThreshold)
    {
        if (rule.specificEnemyData == null)
            return false;

        if (enemyData == rule.specificEnemyData)
            return CompareStatusStacks(GetStatusStacks(rule.statusData), rule.comparisonType, comparisonThreshold);

        if (enemies == null)
            return false;

        for (int i = 0; i < enemies.Length; i++)
        {
            BattleEnemyController enemy = enemies[i];
            if (enemy == null || enemy.IsDead || enemy.enemyData != rule.specificEnemyData)
                continue;

            return CompareStatusStacks(enemy.GetStatusStacks(rule.statusData), rule.comparisonType, comparisonThreshold);
        }

        return false;
    }

    private bool SpecificAllyMatchesStatusCondition(
        EnemyData_SO.StatusStageTransitionRule rule,
        BattleAllyController[] allies,
        int comparisonThreshold)
    {
        if (rule.specificCharacterData == null || allies == null)
            return false;

        for (int i = 0; i < allies.Length; i++)
        {
            BattleAllyController ally = allies[i];
            if (ally == null || ally.IsDead || ally.CharacterData != rule.specificCharacterData)
                continue;

            return CompareStatusStacks(ally.GetStatusStacks(rule.statusData), rule.comparisonType, comparisonThreshold);
        }

        return false;
    }

    private static bool CompareStatusStacks(int stacks, EnemyData_SO.StageStatusComparisonType comparisonType, int threshold)
    {
        switch (comparisonType)
        {
            case EnemyData_SO.StageStatusComparisonType.GreaterThan:
                return stacks > threshold;
            case EnemyData_SO.StageStatusComparisonType.GreaterOrEqual:
                return stacks >= threshold;
            case EnemyData_SO.StageStatusComparisonType.LessThan:
                return stacks < threshold;
            case EnemyData_SO.StageStatusComparisonType.LessOrEqual:
                return stacks <= threshold;
            default:
                return false;
        }
    }

    private SpeedButton ResolvePreferredTargetButton(SpeedButton sourceButton, List<SpeedButton> allyButtons)
    {
        EnemySkill_SO skill = GetSelectedSkillForButton(sourceButton);
        if (skill == null || skill.targetPriorityMode == EnemySkill_SO.TargetPriorityMode.None)
            return null;

        if (skill.targetPriorityMode == EnemySkill_SO.TargetPriorityMode.PreferTargetsWithStatusAtLeast)
        {
            List<SpeedButton> preferredTargets = new List<SpeedButton>();
            int requiredStacks = Mathf.Max(1, skill.targetPriorityRequiredStacks);
            for (int i = 0; i < allyButtons.Count; i++)
            {
                SpeedButton allyButton = allyButtons[i];
                if (allyButton == null)
                    continue;

                BattleAllyController ally = allyButton.GetComponentInParent<BattleAllyController>();
                if (ally == null || ally.IsDead)
                    continue;

                if (ResolveTargetPriorityStatusStacks(ally, skill) >= requiredStacks)
                    preferredTargets.Add(allyButton);
            }

            if (preferredTargets.Count > 0)
                return preferredTargets[UnityEngine.Random.Range(0, preferredTargets.Count)];
        }

        if (skill.targetPriorityMode == EnemySkill_SO.TargetPriorityMode.PreferLowestSpeed)
        {
            int lowestSpeed = int.MaxValue;
            List<SpeedButton> preferredTargets = new List<SpeedButton>();
            for (int i = 0; i < allyButtons.Count; i++)
            {
                SpeedButton allyButton = allyButtons[i];
                if (allyButton == null)
                    continue;

                BattleAllyController ally = allyButton.GetComponentInParent<BattleAllyController>();
                if (ally == null || ally.IsDead)
                    continue;

                int allySpeed = Mathf.Max(0, ally.Speed);
                if (allySpeed < lowestSpeed)
                {
                    lowestSpeed = allySpeed;
                    preferredTargets.Clear();
                    preferredTargets.Add(allyButton);
                }
                else if (allySpeed == lowestSpeed)
                {
                    preferredTargets.Add(allyButton);
                }
            }

            if (preferredTargets.Count > 0)
                return preferredTargets[UnityEngine.Random.Range(0, preferredTargets.Count)];
        }

        return null;
    }

    private EnemySkill_SO.DicePowerRange ResolveSkillBaseDiceRange(EnemySkill_SO skill, int diceIndex)
    {
        if (skill == null)
        {
            return new EnemySkill_SO.DicePowerRange
            {
                minPower = 1,
                maxPower = 1
            };
        }

        return skill.GetDicePowerRange(diceIndex);
    }

    private static EnemySkill_SO.DicePowerRange ApplyQueuedAttackSkillPowerBonus(
        EnemySkill_SO.DicePowerRange range,
        EnemySkill_SO skill,
        int bonusAmount)
    {
        if (skill == null || skill.IsDefenseSkill || bonusAmount == 0)
            return range;

        range.minPower = Mathf.Max(1, range.minPower + bonusAmount);
        range.maxPower = Mathf.Max(range.minPower, range.maxPower + bonusAmount);
        return range;
    }

    private int ApplyLastStandRule(int resultingHP)
    {
        if (resultingHP > 0
            || enemyData == null
            || !enemyData.surviveAtOneHpUntilAllLivingAlliesAtOrBelowOneHp
            || !HasOtherLivingAllyAboveOneHp())
        {
            return resultingHP;
        }

        return 1;
    }

    private bool HasOtherLivingAllyAboveOneHp()
    {
        BattleEnemyController[] allEnemies = FindObjectsOfType<BattleEnemyController>();
        for (int i = 0; i < allEnemies.Length; i++)
        {
            BattleEnemyController enemy = allEnemies[i];
            if (enemy == null || enemy == this || enemy.IsDead)
                continue;

            if (enemy.CurrentHP > 1)
                return true;
        }

        return false;
    }

    private void TryCollapseLinkedLastStandEnemies()
    {
        if (enemyData == null
            || !enemyData.surviveAtOneHpUntilAllLivingAlliesAtOrBelowOneHp
            || IsDead)
        {
            return;
        }

        BattleEnemyController[] allEnemies = FindObjectsOfType<BattleEnemyController>();
        bool hasLivingEnemy = false;
        for (int i = 0; i < allEnemies.Length; i++)
        {
            BattleEnemyController enemy = allEnemies[i];
            if (enemy == null || enemy.IsDead)
                continue;

            hasLivingEnemy = true;
            if (enemy.CurrentHP > 1)
                return;
        }

        if (!hasLivingEnemy)
            return;

        for (int i = 0; i < allEnemies.Length; i++)
        {
            BattleEnemyController enemy = allEnemies[i];
            if (enemy == null
                || enemy.IsDead
                || enemy.enemyData == null
                || !enemy.enemyData.surviveAtOneHpUntilAllLivingAlliesAtOrBelowOneHp
                || enemy.CurrentHP > 1)
            {
                continue;
            }

            enemy.ForceResolveLastStandCollapse();
        }
    }

    private void ForceResolveLastStandCollapse()
    {
        if (IsDead || CurrentHP <= 0)
            return;

        CurrentHP = 0;
        HealthChanged?.Invoke(this);
        Die();
    }

    private static int ResolveTargetPriorityStatusStacks(BattleAllyController ally, EnemySkill_SO skill)
    {
        if (ally == null || skill == null)
            return 0;

        if (skill.targetPriorityStatusData != null)
            return ally.GetStatusStacks(skill.targetPriorityStatusData);

        return skill.targetPriorityStatusType == EnemySkill_SO.DiceStatusType.Burn
            ? ally.GetStatusStacks("burn")
            : 0;
    }

    private void RefreshSpeedButtonsFromHierarchy()
    {
        speedButtons.Clear();

        SpeedButton[] foundButtons = GetComponentsInChildren<SpeedButton>(true);
        for (int i = 0; i < foundButtons.Length; i++)
        {
            if (foundButtons[i] != null && !speedButtons.Contains(foundButtons[i]))
                speedButtons.Add(foundButtons[i]);
        }

        if (speedButtons.Count == 0)
        {
            SpeedButton ownButton = GetComponent<SpeedButton>();
            if (ownButton != null)
                speedButtons.Add(ownButton);
        }

        speedButton = speedButtons.Count > 0 ? speedButtons[0] : null;
        RefreshActiveSpeedButtons();
    }

    private void ApplyCurrentStageVisual()
    {
        SpriteRenderer renderer = FindPrimaryBodyRenderer(transform);
        if (renderer == null)
            return;

        Sprite sprite = GetCurrentStageSprite();
        renderer.sprite = sprite;
        if (sprite != null)
            renderer.color = Color.white;

        PolygonCollider2D collider2D = GetComponent<PolygonCollider2D>();
        ApplySpriteAnchorAlignment(sprite, ResolveBodyScale(), ResolveVisualOffset(), renderer.transform, collider2D);
    }

    private void RefreshActiveSpeedButtons()
    {
        EnsureSpeedButtonCountForCurrentStage();
        activeSpeedButtons.Clear();
        int desiredCount = ResolveDesiredActiveSpeedButtonCount();
        float topLocalY = CalculateCurrentStageTopLocalY();
        Vector2 resolvedSpeedButtonOffset = ResolveSpeedButtonOffset();
        float effectiveSpacing = Mathf.Abs(speedButtonHorizontalSpacing);
        float xStart = -((desiredCount - 1) * effectiveSpacing) * 0.5f;

        for (int i = 0; i < speedButtons.Count; i++)
        {
            SpeedButton button = speedButtons[i];
            if (button == null)
                continue;

            bool shouldBeActive = i < desiredCount;
            button.gameObject.SetActive(shouldBeActive);
            if (!shouldBeActive)
            {
                button.HideForCombat();
                button.IsInteractable = false;
                continue;
            }

            float x = resolvedSpeedButtonOffset.x + (desiredCount > 1 ? speedButtonMultiGroupOffset.x + xStart + i * effectiveSpacing : 0f);
            float y = topLocalY + resolvedSpeedButtonOffset.y + (desiredCount > 1 ? speedButtonMultiGroupOffset.y : 0f);
            Transform buttonTransform = button.transform;
            if (buttonTransform.parent != transform)
                buttonTransform.SetParent(transform, true);
            buttonTransform.localPosition = new Vector3(x, y, buttonTransform.localPosition.z);
            buttonTransform.localRotation = Quaternion.identity;
            buttonTransform.localScale = Vector3.one;
            button.SetButtonType(speedButtonType);
            activeSpeedButtons.Add(button);
        }

        speedButton = activeSpeedButtons.Count > 0 ? activeSpeedButtons[0] : null;
        EnsureSelectedSkillSlots();
    }

    private int ResolveDesiredActiveSpeedButtonCount()
    {
        if (IsDead)
            return 0;

        int baseCount = Mathf.Max(1, enemyData != null ? enemyData.GetStageSpeedButtonCount(CurrentStageIndex) : speedButtons.Count);
        return Mathf.Max(0, baseCount - ResolveStatusSpeedButtonPenalty());
    }

    private void EnsureSpeedButtonCountForCurrentStage()
    {
        int desiredCount = Mathf.Max(1, enemyData != null ? enemyData.GetStageSpeedButtonCount(CurrentStageIndex) : speedButtons.Count);
        if (speedButtons.Count >= desiredCount)
            return;

        if (speedButtonTemplateObject == null)
        {
            SpeedButton existingTemplate = speedButton != null ? speedButton : (speedButtons.Count > 0 ? speedButtons[0] : null);
            if (existingTemplate != null)
                speedButtonTemplateObject = existingTemplate.gameObject;
        }

        if (speedButtonTemplateObject == null)
            return;

        while (speedButtons.Count < desiredCount)
        {
            GameObject buttonObject = Instantiate(speedButtonTemplateObject, transform);
            buttonObject.name = desiredCount > 1 ? $"SpeedButton_{speedButtons.Count + 1}" : "SpeedButton";
            buttonObject.transform.localRotation = Quaternion.identity;
            buttonObject.transform.localScale = Vector3.one;
            buttonObject.SetActive(true);

            SpeedButton created = buttonObject.GetComponent<SpeedButton>() ?? buttonObject.GetComponentInChildren<SpeedButton>(true);
            if (created == null)
                break;

            created.SetButtonType(speedButtonType);
            created.IsInteractable = false;
            speedButtons.Add(created);
        }
    }

    private float CalculateCurrentStageTopLocalY()
    {
        Sprite sprite = GetCurrentStageSprite();
        if (sprite == null)
            return speedButtonBaseOffset.y;

        Vector3 scale = ResolveBodyScale();
        Vector3 localOffset = ResolveVisualOffset();
        Bounds bounds = sprite.bounds;
        return (-bounds.min.y * scale.y) + (bounds.size.y * scale.y) + localOffset.y;
    }

    private Vector2 ResolveSpeedButtonOffset()
    {
        return speedButtonBaseOffset + (enemyData != null ? enemyData.speedButtonOffset : Vector2.zero);
    }

    private BattleStatusRuntimeEntry GetOrCreateStatusEntry(BattleStatus_SO statusData, string fallbackStatusId, string fallbackDisplayName)
    {
        string key = ResolveStatusKey(statusData, fallbackStatusId, fallbackDisplayName);
        BattleStatusRuntimeEntry entry = FindStatusEntry(key);
        if (entry != null)
        {
            if (statusData != null)
                entry.statusData = statusData;
            if (!string.IsNullOrWhiteSpace(fallbackDisplayName))
                entry.displayName = fallbackDisplayName;
            if (!string.IsNullOrWhiteSpace(fallbackStatusId))
                entry.fallbackStatusId = fallbackStatusId;
            return entry;
        }

        entry = new BattleStatusRuntimeEntry
        {
            statusData = statusData,
            fallbackStatusId = fallbackStatusId,
            displayName = fallbackDisplayName,
            stacks = 0
        };
        statusEntries.Add(entry);
        return entry;
    }

    private BattleStatusRuntimeEntry FindStatusEntry(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return null;

        for (int i = 0; i < statusEntries.Count; i++)
        {
            BattleStatusRuntimeEntry entry = statusEntries[i];
            if (entry != null && string.Equals(entry.Key, key, StringComparison.OrdinalIgnoreCase))
                return entry;
        }

        return null;
    }

    private void ResolvePendingStatusesAtRoundStart()
    {
        if (pendingStatusApplications.Count == 0)
            return;

        for (int i = pendingStatusApplications.Count - 1; i >= 0; i--)
        {
            PendingBattleStatusApplication pending = pendingStatusApplications[i];
            if (pending == null || pending.stacks <= 0)
            {
                pendingStatusApplications.RemoveAt(i);
                continue;
            }

            pending.remainingRoundStarts = Mathf.Max(0, pending.remainingRoundStarts - 1);
            if (pending.remainingRoundStarts > 0)
                continue;

            AddStatus(pending.statusData, pending.fallbackStatusId, pending.displayName, pending.stacks);
            pending.repeatCountRemaining = Mathf.Max(0, pending.repeatCountRemaining - 1);
            if (pending.repeatCountRemaining <= 0)
            {
                pendingStatusApplications.RemoveAt(i);
                continue;
            }

            pending.remainingRoundStarts = 1;
        }
    }

    private int ResolveRoundStartStatusDamage(BattleStatusRuntimeEntry entry)
    {
        if (entry == null || entry.stacks <= 0)
            return 0;
        if (IsStatusEffectSuppressed(entry))
            return 0;

        int damage = 0;
        if (entry.statusData != null)
        {
            if (entry.statusData.BehaviorType == BattleStatus_SO.StatusBehaviorType.DamageAtRoundStart)
                damage = Mathf.Max(0, entry.statusData.PowerPerStack) * entry.stacks;
            return RelicSystem.ResolveEnemyRoundStartStatusDamage(this, entry.statusData, entry.Key, entry.stacks, damage);
        }

        if (string.Equals(entry.Key, BurnStatusId, StringComparison.OrdinalIgnoreCase))
            damage = entry.stacks;

        return RelicSystem.ResolveEnemyRoundStartStatusDamage(this, entry.statusData, entry.Key, entry.stacks, damage);
    }

    private int ResolveRoundStartStatusSpeedBonus(BattleStatusRuntimeEntry entry)
    {
        if (entry == null || entry.stacks <= 0 || entry.statusData == null)
            return 0;
        if (IsStatusEffectSuppressed(entry))
            return 0;

        if (entry.statusData.BehaviorType == BattleStatus_SO.StatusBehaviorType.SpeedBonusAtRoundStart)
            return Mathf.Max(0, entry.statusData.PowerPerStack) * entry.stacks;
        if (entry.statusData.BehaviorType == BattleStatus_SO.StatusBehaviorType.SpeedPenaltyAtRoundStart)
            return -Mathf.Max(0, entry.statusData.PowerPerStack) * entry.stacks;

        return 0;
    }

    private int ResolveRoundStartLinkedStatusAmount(BattleStatusRuntimeEntry entry)
    {
        if (entry == null || entry.stacks <= 0 || entry.statusData == null)
            return 0;
        if (IsStatusEffectSuppressed(entry))
            return 0;

        if (entry.statusData.BehaviorType != BattleStatus_SO.StatusBehaviorType.ApplyLinkedStatusAtRoundStart)
            return 0;
        if (entry.statusData.LinkedStatusData == null)
            return 0;

        int defaultStacks = Mathf.Max(0, entry.statusData.PowerPerStack) * entry.stacks;
        return RelicSystem.ResolveEnemyRoundStartLinkedStatusAmount(entry.statusData, entry.Key, defaultStacks);
    }

    private int ResolveOnHitStatusDamage(BattleStatusRuntimeEntry entry)
    {
        if (entry == null || entry.stacks <= 0 || entry.statusData == null)
            return 0;
        if (IsStatusEffectSuppressed(entry))
            return 0;

        if (entry.statusData.BehaviorType == BattleStatus_SO.StatusBehaviorType.DamageWhenHit)
            return Mathf.Max(0, entry.statusData.PowerPerStack) * entry.stacks;

        return 0;
    }

    private int ResolveOnHitStatusStacksAfterTrigger(BattleStatusRuntimeEntry entry, int stacksBeforeTrigger)
    {
        if (entry == null || stacksBeforeTrigger <= 0 || entry.statusData == null)
            return stacksBeforeTrigger;
        if (IsStatusEffectSuppressed(entry))
            return stacksBeforeTrigger;
        if (ShouldPreservePersistentStatusStacks(entry))
            return stacksBeforeTrigger;

        switch (entry.statusData.OnHitDecayMode)
        {
            case BattleStatus_SO.StackDecayMode.HalveRoundedDown:
                return Mathf.Max(0, stacksBeforeTrigger / 2);
            case BattleStatus_SO.StackDecayMode.LoseAll:
                return 0;
            default:
                return stacksBeforeTrigger;
        }
    }

    private int ResolveRoundStartStatusStacksAfterTick(BattleStatusRuntimeEntry entry, int stacksBeforeTick)
    {
        if (entry == null || stacksBeforeTick <= 0)
            return 0;
        if (IsStatusEffectSuppressed(entry))
            return stacksBeforeTick;
        if (ShouldPreservePersistentStatusStacks(entry))
            return stacksBeforeTick;

        if (string.Equals(entry.Key, BurnStatusId, StringComparison.OrdinalIgnoreCase))
            return RelicSystem.ResolveEnemyBurnRoundStartStacksAfterTick(stacksBeforeTick, Mathf.Max(0, stacksBeforeTick / 2));

        if (entry.statusData != null)
        {
            switch (entry.statusData.RoundStartDecayMode)
            {
                case BattleStatus_SO.StackDecayMode.HalveRoundedDown:
                    return Mathf.Max(0, stacksBeforeTick / 2);
                default:
                    return stacksBeforeTick;
            }
        }

        return stacksBeforeTick;
    }

    private int ResolveRoundEndStatusStacks(BattleStatusRuntimeEntry entry, int stacksBeforeDecay)
    {
        if (entry == null || stacksBeforeDecay <= 0 || entry.statusData == null)
            return stacksBeforeDecay;
        if (IsStatusEffectSuppressed(entry))
            return stacksBeforeDecay;
        if (ShouldPreservePersistentStatusStacks(entry))
            return stacksBeforeDecay;

        switch (entry.statusData.RoundEndDecayMode)
        {
            case BattleStatus_SO.StackDecayMode.HalveRoundedDown:
                return Mathf.Max(0, stacksBeforeDecay / 2);
            case BattleStatus_SO.StackDecayMode.LoseAll:
                return 0;
            case BattleStatus_SO.StackDecayMode.LoseOne:
                return Mathf.Max(0, stacksBeforeDecay - 1);
            default:
                return stacksBeforeDecay;
        }
    }

    private int ResolveStatusSpeedButtonPenalty()
    {
        if (statusEntries.Count == 0)
            return 0;

        int totalPenalty = 0;
        for (int i = 0; i < statusEntries.Count; i++)
        {
            BattleStatusRuntimeEntry entry = statusEntries[i];
            if (entry == null || entry.stacks <= 0)
                continue;
            if (IsStatusEffectSuppressed(entry))
                continue;
            if (!IsSpeedButtonPenaltyStatus(entry))
                continue;

            totalPenalty += entry.stacks * Mathf.Max(0, entry.statusData.PowerPerStack);
        }

        return Mathf.Max(0, totalPenalty);
    }

    private static bool IsSpeedButtonPenaltyStatus(BattleStatusRuntimeEntry entry)
    {
        return entry != null
            && entry.statusData != null
            && entry.statusData.BehaviorType == BattleStatus_SO.StatusBehaviorType.SpeedButtonPenalty;
    }

    private static bool ShouldPreservePersistentStatusStacks(BattleStatusRuntimeEntry entry)
    {
        if (entry == null || entry.statusData == null)
            return false;

        BattleStatus_SO data = entry.statusData;
        return data.BehaviorType == BattleStatus_SO.StatusBehaviorType.None
            && data.LinkedStatusData == null
            && data.OnHitDecayMode == BattleStatus_SO.StackDecayMode.None
            && data.RoundStartDecayMode == BattleStatus_SO.StackDecayMode.None
            && data.RoundEndDecayMode == BattleStatus_SO.StackDecayMode.None;
    }

    private int GetLegacyStatusStacks(string fallbackStatusId)
    {
        BattleStatusRuntimeEntry entry = FindStatusEntry(fallbackStatusId);
        return entry != null ? Mathf.Max(0, entry.stacks) : 0;
    }

    private void NotifyStatusChanged(BattleStatusRuntimeEntry entry)
    {
        if (entry == null)
            return;

        if (string.Equals(entry.Key, BurnStatusId, StringComparison.OrdinalIgnoreCase))
            BurnChanged?.Invoke(this, Mathf.Max(0, entry.stacks));

        StatusChanged?.Invoke(this);
    }

    private static string ResolveStatusKey(BattleStatus_SO statusData, string fallbackStatusId, string fallbackDisplayName)
    {
        if (statusData != null && !string.IsNullOrWhiteSpace(statusData.StatusId))
            return statusData.StatusId;
        if (!string.IsNullOrWhiteSpace(fallbackStatusId))
            return fallbackStatusId;
        return fallbackDisplayName ?? string.Empty;
    }

    private bool CanReceiveStatusApplication(BattleStatus_SO statusData, string fallbackStatusId)
    {
        if (!TryGetStatusImmunityRule(statusData, fallbackStatusId, out EnemyData_SO.StatusImmunityRule immunityRule))
            return true;

        return immunityRule.allowApplicationWhileImmune;
    }

    private bool IsStatusEffectSuppressed(BattleStatusRuntimeEntry entry)
    {
        if (entry == null)
            return false;

        return TryGetStatusImmunityRule(entry.statusData, entry.fallbackStatusId, out _);
    }

    private bool TryGetStatusImmunityRule(BattleStatus_SO statusData, string fallbackStatusId, out EnemyData_SO.StatusImmunityRule matchedRule)
    {
        matchedRule = default;
        if (enemyData == null || enemyData.statusImmunityRules == null || enemyData.statusImmunityRules.Count == 0)
            return false;

        string resolvedStatusId = statusData != null ? statusData.StatusId : fallbackStatusId;
        for (int i = 0; i < enemyData.statusImmunityRules.Count; i++)
        {
            EnemyData_SO.StatusImmunityRule rule = enemyData.statusImmunityRules[i];
            if (rule.statusData == null)
                continue;

            if (statusData != null)
            {
                if (rule.statusData == statusData || string.Equals(rule.statusData.StatusId, resolvedStatusId, StringComparison.OrdinalIgnoreCase))
                {
                    matchedRule = rule;
                    return true;
                }
            }
            else if (!string.IsNullOrWhiteSpace(resolvedStatusId) && string.Equals(rule.statusData.StatusId, resolvedStatusId, StringComparison.OrdinalIgnoreCase))
            {
                matchedRule = rule;
                return true;
            }
        }

        return false;
    }

    private static string BuildDiceRangeSummary(EnemySkill_SO skill)
    {
        if (skill == null)
            return "1~1";

        List<string> segments = new List<string>(skill.diceCount);
        for (int i = 0; i < skill.diceCount; i++)
        {
            EnemySkill_SO.DicePowerRange range = skill.GetDicePowerRange(i);
            segments.Add($"{i + 1}:{range.minPower}~{range.maxPower}");
        }

        return string.Join(", ", segments);
    }

    private static string BuildSkillDescriptionSuffix(EnemySkill_SO skill)
    {
        if (skill == null || string.IsNullOrWhiteSpace(skill.description))
            return string.Empty;

        return $" | {skill.description}";
    }

    private Vector3 ResolveBodyScale()
    {
        SpriteRenderer renderer = FindPrimaryBodyRenderer(transform);
        if (renderer != null)
            return renderer.transform.localScale;

        float multiplier = enemyData != null ? Mathf.Max(0.01f, enemyData.enemyVisualScale) : 1f;
        return new Vector3(1.15f * multiplier, 1.15f * multiplier, 1f);
    }

    private Vector3 ResolveVisualOffset()
    {
        return enemyData != null ? enemyData.enemyVisualOffset : Vector3.zero;
    }

    private static SpriteRenderer FindPrimaryBodyRenderer(Transform root)
    {
        if (root == null)
            return null;

        SpriteRenderer[] renderers = root.GetComponentsInChildren<SpriteRenderer>(true);
        SpriteRenderer best = null;
        int bestSorting = int.MinValue;
        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer renderer = renderers[i];
            if (renderer == null || renderer.GetComponentInParent<SpeedButton>() != null)
                continue;

            if (best == null || renderer.sortingOrder > bestSorting)
            {
                best = renderer;
                bestSorting = renderer.sortingOrder;
            }
        }

        return best;
    }

    private static void ApplySpriteAnchorAlignment(Sprite sprite, Vector3 scale, Vector3 localOffset, Transform body, PolygonCollider2D collider2D)
    {
        if (body == null || collider2D == null)
            return;

        Sprite effectiveSprite = sprite;
        if (effectiveSprite == null)
            return;

        Bounds bounds = effectiveSprite.bounds;
        Vector3 bottomCenterOffset = new Vector3(
            -bounds.center.x * scale.x,
            -bounds.min.y * scale.y,
            0f);

        body.localPosition = bottomCenterOffset + localOffset;
        body.localScale = scale;

        float width = Mathf.Max(0.8f, bounds.size.x * scale.x);
        float height = Mathf.Max(1f, bounds.size.y * scale.y);
        collider2D.points = new[]
        {
            new Vector2(-width * 0.42f, 0f),
            new Vector2(-width * 0.5f, height * 0.45f),
            new Vector2(-width * 0.24f, height),
            new Vector2(width * 0.24f, height),
            new Vector2(width * 0.5f, height * 0.45f),
            new Vector2(width * 0.42f, 0f)
        };
    }
}
