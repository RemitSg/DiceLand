using System;
using System.Collections.Generic;
using UnityEngine;

public class BattleAllyController : MonoBehaviour
{
    [Header("References")]
    public SpeedButton speedButton;
    [SerializeField] private List<SpeedButton> speedButtons = new List<SpeedButton>();

    public int Speed { get; private set; }
    public int CurrentHP { get; private set; }
    public int MaxHP { get; private set; }
    public int LastDamageModifierRatioPercent { get; private set; } = 100;
    public int LastPowerModifierAmount { get; private set; }
    public int Level { get; private set; } = 1;
    public int Experience { get; private set; }
    public CharacterData_SO CharacterData => allyController != null ? allyController.characterData : null;
    public EnemySkill_SO SelectedSkill { get; private set; }
    public EnemySkill_SO CurrentRoundUsedSkill { get; private set; }
    public EnemySkill_SO LastRoundUsedSkill { get; private set; }
    public int CurrentSkillIndex { get; private set; }
    public int DiceCount => SelectedSkill != null ? SelectedSkill.diceCount : 1;
    public bool IsDead { get; private set; }
    public int BurnStacks => GetLegacyStatusStacks(BurnStatusId);
    public int PendingRoundStartSpeedBonus { get; private set; }
    public IReadOnlyList<BattleStatusRuntimeEntry> StatusEntries => statusEntries;
    public IReadOnlyList<SpeedButton> SpeedButtons => activeSpeedButtons;
    public bool HasTemporarySkills => temporarySkills.Count > 0;
    public bool TemporarySkillsOverrideWeaponPool => temporarySkillsOverrideWeaponPool;
    public Vector3 LastHitDirection { get; private set; } = Vector3.up;

    public event Action<BattleAllyController, int> Damaged;
    public event Action<BattleAllyController> HealthChanged;
    public event Action<BattleAllyController> Died;
    public event Action<BattleAllyController, int> BurnChanged;
    public event Action<BattleAllyController> StatusChanged;
    public event Action<BattleAllyController> SelectedSkillChanged;

    private AllyController allyController;
    private bool hasRuntimeStateOverride;
    private bool temporarySkillsOverrideWeaponPool;
    private readonly List<BattleStatusRuntimeEntry> statusEntries = new List<BattleStatusRuntimeEntry>();
    private readonly List<PendingBattleStatusApplication> pendingStatusApplications = new List<PendingBattleStatusApplication>();
    private readonly List<EnemySkill_SO> temporarySkills = new List<EnemySkill_SO>();
    private readonly List<SpeedButton> activeSpeedButtons = new List<SpeedButton>();
    private readonly HashSet<string> consumedLethalProtectionRelicIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    private const string BurnStatusId = "burn";
    private const string BleedStatusId = "bleed";
    private EnemySkill_SO consecutiveSkillTracker;
    private int consecutiveSkillUseCount;
    private EnemySkill_SO pendingConsecutiveSkillResetSkill;
    private GameObject speedButtonTemplateObject;
    private float speedButtonTopLocalY;
    private Vector2 speedButtonBaseOffset = new Vector2(0f, 1f);
    private Vector2 speedButtonMultiGroupOffset = Vector2.zero;
    private float speedButtonHorizontalSpacing = 0.82f;
    private int temporaryExtraSpeedButtons;
    private int inheritedExtraSpeedButtons;
    private int lastLivingActiveSpeedButtonCount = 1;

    private void Awake()
    {
        RefreshSpeedButtonsFromHierarchy();
        if (speedButton == null)
            Debug.LogWarning($"[BattleAllyController] {gameObject.name} is missing a SpeedButton reference.");

        allyController = GetComponent<AllyController>();
        if (allyController == null)
            allyController = GetComponentInParent<AllyController>();

        if (hasRuntimeStateOverride)
            return;

        int baseHP = allyController != null && allyController.characterData != null
            ? allyController.characterData.baseHP
            : 100;

        MaxHP = CurrentHP = Mathf.Max(1, baseHP);
    }

    public void ApplyRuntimeState(RunPartyMemberState partyMemberState)
    {
        if (partyMemberState == null)
            return;

        if (allyController == null)
            allyController = GetComponent<AllyController>();
        if (allyController == null)
            allyController = gameObject.AddComponent<AllyController>();

        allyController.characterData = partyMemberState.characterData;
        if (allyController.weapon == null)
            allyController.weapon = GetComponent<Weapon>() ?? gameObject.AddComponent<Weapon>();
        allyController.weapon.weaponData = partyMemberState.weaponData;

        hasRuntimeStateOverride = true;
        Level = Mathf.Max(1, partyMemberState.level);
        Experience = Mathf.Max(0, partyMemberState.experience);
        MaxHP = Mathf.Max(1, partyMemberState.maxHP);
        CurrentHP = Mathf.Clamp(partyMemberState.currentHP, 0, MaxHP);
        IsDead = CurrentHP <= 0;
        RefreshActiveSpeedButtons();
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

        CharacterData_SO characterData = allyController != null ? allyController.characterData : null;
        int effectiveMin = characterData != null ? Mathf.Max(0, characterData.minSpeed) : Mathf.Max(0, min);
        int effectiveMax = characterData != null ? Mathf.Max(effectiveMin, characterData.maxSpeed) : Mathf.Max(effectiveMin, max);

        Speed = Mathf.Max(1, UnityEngine.Random.Range(effectiveMin, effectiveMax + 1) + PendingRoundStartSpeedBonus);
        for (int i = 0; i < activeSpeedButtons.Count; i++)
        {
            if (activeSpeedButtons[i] != null)
                activeSpeedButtons[i].Speed = Speed;
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
    }

    public void ConfigureSpeedButtonLayout(
        SpeedButton templateButton,
        float topLocalY,
        Vector2 baseOffset,
        Vector2 multiGroupOffset,
        float horizontalSpacing)
    {
        if (templateButton != null)
            speedButtonTemplateObject = templateButton.gameObject;

        speedButtonTopLocalY = topLocalY;
        speedButtonBaseOffset = baseOffset;
        speedButtonMultiGroupOffset = multiGroupOffset;
        speedButtonHorizontalSpacing = horizontalSpacing;
        RefreshActiveSpeedButtons();
    }

    public void SetTemporaryExtraSpeedButtons(int extraCount)
    {
        int resolved = Mathf.Max(0, extraCount);
        if (temporaryExtraSpeedButtons == resolved)
            return;

        temporaryExtraSpeedButtons = resolved;
        RefreshActiveSpeedButtons();
    }

    public void SetInheritedExtraSpeedButtons(int extraCount)
    {
        inheritedExtraSpeedButtons = Mathf.Max(0, extraCount);
        RefreshActiveSpeedButtons();
    }

    public bool TransferInheritableSpeedButtonsTo(BattleAllyController recipient)
    {
        if (recipient == null || recipient == this)
            return false;

        int transferCount = Mathf.Max(0, Mathf.Max(lastLivingActiveSpeedButtonCount, 1 + inheritedExtraSpeedButtons));
        if (transferCount <= 0)
            return false;

        recipient.SetInheritedExtraSpeedButtons(recipient.inheritedExtraSpeedButtons + transferCount);
        inheritedExtraSpeedButtons = 0;
        temporaryExtraSpeedButtons = 0;
        RefreshActiveSpeedButtons();
        return true;
    }

    public void SelectDefaultSkill(int roundNumber = -1)
    {
        if (IsDead)
        {
            SelectedSkill = null;
            return;
        }

        string roundPrefix = roundNumber > 0 ? $"[Round {roundNumber}] " : string.Empty;
        Weapon_SO weaponData = GetWeaponData();
        EnemySkill_SO[] activeSkillPool = BuildActiveSkillPool();
        if ((activeSkillPool == null || activeSkillPool.Length == 0) && weaponData == null)
        {
            SelectedSkill = null;
            CurrentSkillIndex = 0;
            Debug.LogWarning($"[BattleAllyController] {roundPrefix}{gameObject.name} has no weapon data. Falling back to power 1.");
            return;
        }

        if (activeSkillPool == null || activeSkillPool.Length == 0)
        {
            SelectedSkill = null;
            CurrentSkillIndex = 0;
            if (weaponData != null)
                Debug.LogWarning($"[BattleAllyController] {roundPrefix}{gameObject.name} weapon {weaponData.weaponName} has no configured skills. Falling back to legacy single-die power range {weaponData.minPower}~{weaponData.maxPower}.");
            return;
        }

        CurrentSkillIndex = Mathf.Clamp(CurrentSkillIndex, 0, activeSkillPool.Length - 1);
        SelectedSkill = activeSkillPool[CurrentSkillIndex];
        if (SelectedSkill == null)
        {
            CurrentSkillIndex = 0;
            if (weaponData != null)
                Debug.LogWarning($"[BattleAllyController] {roundPrefix}{gameObject.name} weapon {weaponData.weaponName} has no configured skills. Falling back to legacy single-die power range {weaponData.minPower}~{weaponData.maxPower}.");
            return;
        }

        Debug.Log($"[BattleAllyController] {roundPrefix}{GetDebugDisplayName()} selected weapon skill: {GetSelectedSkillDebugSummary()}");
        SelectedSkillChanged?.Invoke(this);
    }

    public EnemySkill_SO[] GetAvailableSkills()
    {
        return BuildActiveSkillPool();
    }

    public bool SetSelectedSkillIndex(int skillIndex, int roundNumber = -1)
    {
        EnemySkill_SO[] activeSkillPool = BuildActiveSkillPool();
        if (activeSkillPool == null || activeSkillPool.Length == 0)
            return false;

        CurrentSkillIndex = Mathf.Clamp(skillIndex, 0, activeSkillPool.Length - 1);
        SelectedSkill = activeSkillPool[CurrentSkillIndex];

        if (SelectedSkill == null)
            return false;

        string roundPrefix = roundNumber > 0 ? $"[Round {roundNumber}] " : string.Empty;
        Debug.Log($"[BattleAllyController] {roundPrefix}{GetDebugDisplayName()} switched weapon skill to: {GetSelectedSkillDebugSummary()}");
        SelectedSkillChanged?.Invoke(this);
        return true;
    }

    public void MarkCurrentRoundSkillUsed()
    {
        RegisterCurrentSkillUsage(SelectedSkill);
        CurrentRoundUsedSkill = SelectedSkill;
    }

    public void FinalizeRoundSkillHistory()
    {
        LastRoundUsedSkill = CurrentRoundUsedSkill;
        CurrentRoundUsedSkill = null;
    }

    public int GetCompletedConsecutiveSkillUses(EnemySkill_SO skill)
    {
        if (skill == null || consecutiveSkillTracker != skill)
            return 0;

        return Mathf.Max(0, consecutiveSkillUseCount);
    }

    public void MarkConsecutiveSkillResetAfterCurrentUse(EnemySkill_SO skill)
    {
        if (skill == null)
            return;

        pendingConsecutiveSkillResetSkill = skill;
    }

    public bool HasConsumedLethalProtection(string relicId)
    {
        if (string.IsNullOrWhiteSpace(relicId))
            return false;

        return consumedLethalProtectionRelicIds.Contains(relicId);
    }

    public bool TryConsumeLethalProtection(string relicId)
    {
        if (string.IsNullOrWhiteSpace(relicId))
            return false;

        return consumedLethalProtectionRelicIds.Add(relicId);
    }

    public void ClearTemporarySkills()
    {
        temporarySkills.Clear();
        temporarySkillsOverrideWeaponPool = false;
        CurrentSkillIndex = 0;

        if (SelectedSkill != null)
        {
            EnemySkill_SO[] activeSkillPool = BuildActiveSkillPool();
            if (activeSkillPool == null || activeSkillPool.Length == 0)
                SelectedSkill = null;
        }

        SelectedSkillChanged?.Invoke(this);
    }

    public void GrantTemporarySkill(EnemySkill_SO skill, bool replaceWeaponSkills = false)
    {
        if (skill == null)
            return;

        if (!temporarySkills.Contains(skill))
            temporarySkills.Add(skill);

        temporarySkillsOverrideWeaponPool |= replaceWeaponSkills;
    }

    public int RollDice(int diceIndex = 0)
    {
        if (IsDead)
            return 0;

        EnemySkill_SO.DicePowerRange range = GetDicePowerRange(diceIndex);
        int min = Mathf.Max(1, range.minPower);
        int max = Mathf.Max(min, range.maxPower);

        switch (ResolveForcedRollMode())
        {
            case ForcedRollMode.Min:
                return min;
            case ForcedRollMode.Max:
                return max;
            default:
                return UnityEngine.Random.Range(min, max + 1);
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
        EnemySkill_SO.DicePowerRange range;
        if (SelectedSkill != null)
        {
            range = SelectedSkill.GetDicePowerRange(diceIndex);
        }
        else
        {
            Weapon_SO weaponData = GetWeaponData();
            if (weaponData == null)
            {
                range = new EnemySkill_SO.DicePowerRange
                {
                    minPower = 1,
                    maxPower = 1
                };
            }
            else
            {
                int min = Mathf.Max(0, weaponData.minPower);
                int max = Mathf.Max(min, weaponData.maxPower);
                range = new EnemySkill_SO.DicePowerRange
                {
                    minPower = min,
                    maxPower = max
                };
            }
        }

        bool isDefenseSkill = SelectedSkill != null && SelectedSkill.IsDefenseSkill;
        return ApplyStatusDiceRangeModifiers(range, isDefenseSkill);
    }

    public string GetDebugDisplayName()
    {
        Weapon_SO weaponData = GetWeaponData();
        return weaponData != null ? $"{gameObject.name}[{weaponData.weaponName}]" : gameObject.name;
    }

    public string GetSelectedSkillDebugSummary()
    {
        if (SelectedSkill != null)
            return $"{SelectedSkill.skillName}({SelectedSkill.diceCount} dice, ranges: {BuildDiceRangeSummary(SelectedSkill)})";

        Weapon_SO weaponData = GetWeaponData();
        if (weaponData != null)
            return $"Legacy weapon die(1 die, range: {Mathf.Max(0, weaponData.minPower)}~{Mathf.Max(Mathf.Max(0, weaponData.minPower), weaponData.maxPower)})";

        return "Default skill(1 die, range: 1~1)";
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
        amount = ApplyIncomingDamageRelicModifiers(amount);
        if (amount <= 0)
        {
            LastDamageModifierRatioPercent = 0;
            return 0;
        }

        if (CurrentHP - amount <= 0 && TryPreventLethalDamage(amount, out int minimumSurvivingHp))
        {
            amount = Mathf.Max(0, CurrentHP - Mathf.Max(1, minimumSurvivingHp));
        }

        if (amount <= 0)
        {
            LastDamageModifierRatioPercent = 0;
            return 0;
        }

        int previousHP = CurrentHP;
        CurrentHP = Mathf.Max(0, CurrentHP - amount);
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

        if (CurrentHP <= 0)
            Die();

        return actualDamage;
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

            if (entry.statusData.BehaviorType != BattleStatus_SO.StatusBehaviorType.DamageTakenBonusPercent)
                continue;

            float percentBonusPerStack = Mathf.Max(0, entry.statusData.PowerPerStack) * 0.01f;
            multiplier += entry.stacks * percentBonusPerStack;
        }

        float bonusMultiplier = Mathf.Max(0f, multiplier - 1f);
        int bonusDamage = Mathf.FloorToInt(baseDamage * bonusMultiplier);
        return Mathf.Max(0, baseDamage + bonusDamage);
    }

    private int ApplyIncomingDamageRelicModifiers(int baseDamage)
    {
        if (baseDamage <= 0)
            return 0;

        BattleEncounterData activeEncounter = BattleEncounterRuntime.ActiveEncounter;
        if (activeEncounter == null || activeEncounter.relicInventory == null || activeEncounter.relicInventory.Count == 0)
            return Mathf.Max(0, baseDamage);

        RelicExecutionContext context = RelicExecutionContext.CreatePlayerAboutToTakeDamage(
            null,
            null,
            -1,
            baseDamage,
            Speed,
            null,
            this);
        RelicTriggerDispatcher.ProcessBattleRelics(activeEncounter, context);

        int adjustedDamage = Mathf.Max(0, baseDamage + context.incomingDamageFlatModifier);
        int reductionPercent = Mathf.Max(0, context.incomingDamagePercentModifier);
        if (reductionPercent > 0)
        {
            float multiplier = Mathf.Max(0f, 1f - (reductionPercent / 100f));
            adjustedDamage = Mathf.Max(0, Mathf.FloorToInt((adjustedDamage * multiplier) + 0.5f));
        }

        return adjustedDamage;
    }

    private bool TryPreventLethalDamage(int incomingDamage, out int minimumSurvivingHp)
    {
        minimumSurvivingHp = 1;
        if (incomingDamage <= 0)
            return false;

        BattleEncounterData activeEncounter = BattleEncounterRuntime.ActiveEncounter;
        if (activeEncounter == null || activeEncounter.relicInventory == null || activeEncounter.relicInventory.Count == 0)
            return false;

        RelicExecutionContext context = RelicExecutionContext.CreatePlayerAboutToTakeLethalDamage(
            null,
            incomingDamage,
            null,
            this);
        RelicTriggerDispatcher.ProcessBattleRelics(activeEncounter, context);
        minimumSurvivingHp = Mathf.Max(1, context.minimumSurvivingHp);
        return context.preventLethalDamage;
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

        BattleStatusRuntimeEntry entry = GetOrCreateStatusEntry(statusData, fallbackStatusId, fallbackDisplayName);
        entry.stacks = Mathf.Max(0, entry.stacks + stackAmount);
        NotifyStatusChanged(entry);
        if (IsSpeedButtonPenaltyStatus(entry))
            RefreshActiveSpeedButtons();
        return true;
    }

    public bool QueueStatusForRoundStart(BattleStatus_SO statusData, string fallbackStatusId, string fallbackDisplayName, int stackAmount, int roundStartDelay, int repeatCount)
    {
        if (IsDead || stackAmount <= 0)
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
            return results;
        }

        PendingRoundStartSpeedBonus = 0;
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
        Speed = 0;
        SelectedSkill = null;
        CurrentRoundUsedSkill = null;
        RefreshActiveSpeedButtons();

        Died?.Invoke(this);
    }

    private int GetDesiredActiveSpeedButtonCount()
    {
        if (IsDead)
            return 0;

        int baseCount = Mathf.Max(0, 1 + inheritedExtraSpeedButtons + temporaryExtraSpeedButtons);
        return Mathf.Max(0, baseCount - ResolveStatusSpeedButtonPenalty());
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
        if (speedButton != null)
        {
            speedButtonTemplateObject = speedButton.gameObject;
            speedButtonTopLocalY = speedButton.transform.localPosition.y - speedButtonBaseOffset.y;
        }

        RefreshActiveSpeedButtons();
    }

    private void RefreshActiveSpeedButtons()
    {
        EnsureSpeedButtonCount();
        activeSpeedButtons.Clear();

        int desiredCount = GetDesiredActiveSpeedButtonCount();
        float effectiveSpacing = Mathf.Abs(speedButtonHorizontalSpacing);
        float xStart = -((Mathf.Max(1, desiredCount) - 1) * effectiveSpacing) * 0.5f;

        for (int i = 0; i < speedButtons.Count; i++)
        {
            SpeedButton button = speedButtons[i];
            if (button == null)
                continue;

            bool shouldBeActive = i < desiredCount;
            button.gameObject.SetActive(shouldBeActive);
            if (!shouldBeActive)
            {
                button.ResetConnection();
                button.HideForCombat();
                button.IsInteractable = false;
                continue;
            }

            float x = speedButtonBaseOffset.x + (desiredCount > 1 ? speedButtonMultiGroupOffset.x + xStart + i * effectiveSpacing : 0f);
            float y = speedButtonTopLocalY + speedButtonBaseOffset.y + (desiredCount > 1 ? speedButtonMultiGroupOffset.y : 0f);
            Transform buttonTransform = button.transform;
            if (buttonTransform.parent != transform)
                buttonTransform.SetParent(transform, true);
            buttonTransform.localPosition = new Vector3(x, y, buttonTransform.localPosition.z);
            buttonTransform.localRotation = Quaternion.identity;
            buttonTransform.localScale = Vector3.one;
            button.SetButtonType(SpeedButton.ButtonType.Ally);
            button.IsInteractable = false;
            button.Speed = Speed;
            activeSpeedButtons.Add(button);
        }

        speedButton = activeSpeedButtons.Count > 0 ? activeSpeedButtons[0] : null;
        if (!IsDead)
            lastLivingActiveSpeedButtonCount = Mathf.Max(0, activeSpeedButtons.Count);
    }

    private void EnsureSpeedButtonCount()
    {
        int desiredCount = Mathf.Max(1, 1 + inheritedExtraSpeedButtons + temporaryExtraSpeedButtons);
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

            created.SetButtonType(SpeedButton.ButtonType.Ally);
            created.IsInteractable = false;
            speedButtons.Add(created);
        }
    }

    private void RegisterCurrentSkillUsage(EnemySkill_SO skill)
    {
        if (skill == null)
            return;

        if (pendingConsecutiveSkillResetSkill == skill)
        {
            consecutiveSkillTracker = skill;
            consecutiveSkillUseCount = 0;
            pendingConsecutiveSkillResetSkill = null;
            return;
        }

        if (consecutiveSkillTracker == skill)
            consecutiveSkillUseCount = consecutiveSkillUseCount > 0 ? consecutiveSkillUseCount + 1 : 1;
        else
        {
            consecutiveSkillTracker = skill;
            consecutiveSkillUseCount = 1;
        }

        pendingConsecutiveSkillResetSkill = null;
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

        if (entry.statusData != null)
        {
            if (entry.statusData.BehaviorType == BattleStatus_SO.StatusBehaviorType.DamageAtRoundStart)
                return Mathf.Max(0, entry.statusData.PowerPerStack) * entry.stacks;
            return 0;
        }

        if (string.Equals(entry.Key, BurnStatusId, StringComparison.OrdinalIgnoreCase))
            return entry.stacks;

        return 0;
    }

    private int ResolveRoundStartStatusSpeedBonus(BattleStatusRuntimeEntry entry)
    {
        if (entry == null || entry.stacks <= 0 || entry.statusData == null)
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

        if (entry.statusData.BehaviorType != BattleStatus_SO.StatusBehaviorType.ApplyLinkedStatusAtRoundStart)
            return 0;
        if (entry.statusData.LinkedStatusData == null)
            return 0;

        return Mathf.Max(0, entry.statusData.PowerPerStack) * entry.stacks;
    }

    private int ResolveOnHitStatusDamage(BattleStatusRuntimeEntry entry)
    {
        if (entry == null || entry.stacks <= 0 || entry.statusData == null)
            return 0;

        if (entry.statusData.BehaviorType == BattleStatus_SO.StatusBehaviorType.DamageWhenHit)
            return Mathf.Max(0, entry.statusData.PowerPerStack) * entry.stacks;

        return 0;
    }

    private int ResolveOnHitStatusStacksAfterTrigger(BattleStatusRuntimeEntry entry, int stacksBeforeTrigger)
    {
        if (entry == null || stacksBeforeTrigger <= 0 || entry.statusData == null)
            return stacksBeforeTrigger;
        if (ShouldPreservePersistentStatusStacks(entry))
            return stacksBeforeTrigger;
        if (ShouldPreventBleedDecayOnHit(entry))
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
        if (ShouldPreservePersistentStatusStacks(entry))
            return stacksBeforeTick;

        if (string.Equals(entry.Key, BurnStatusId, StringComparison.OrdinalIgnoreCase))
            return Mathf.Max(0, stacksBeforeTick / 2);

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

    private static bool ShouldPreventBleedDecayOnHit(BattleStatusRuntimeEntry entry)
    {
        if (entry == null)
            return false;

        string statusKey = entry.statusData != null && !string.IsNullOrWhiteSpace(entry.statusData.StatusId)
            ? entry.statusData.StatusId
            : entry.Key;
        if (!string.Equals(statusKey, BleedStatusId, StringComparison.OrdinalIgnoreCase))
            return false;

        return BattleEnemyController.HasAnyAliveBleedDecayPreventer;
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

    private Weapon_SO GetWeaponData()
    {
        if (allyController == null || allyController.weapon == null)
            return null;

        return allyController.weapon.weaponData;
    }

    private EnemySkill_SO[] BuildActiveSkillPool()
    {
        Weapon_SO weaponData = GetWeaponData();
        EnemySkill_SO[] weaponSkills = weaponData != null ? weaponData.skillPool : null;

        if (temporarySkillsOverrideWeaponPool)
            return temporarySkills.Count > 0 ? temporarySkills.ToArray() : Array.Empty<EnemySkill_SO>();

        if ((weaponSkills == null || weaponSkills.Length == 0) && temporarySkills.Count == 0)
            return null;

        List<EnemySkill_SO> combined = new List<EnemySkill_SO>();
        if (weaponSkills != null)
        {
            for (int i = 0; i < weaponSkills.Length; i++)
            {
                EnemySkill_SO skill = weaponSkills[i];
                if (skill != null && !combined.Contains(skill))
                    combined.Add(skill);
            }
        }

        for (int i = 0; i < temporarySkills.Count; i++)
        {
            EnemySkill_SO skill = temporarySkills[i];
            if (skill != null && !combined.Contains(skill))
                combined.Add(skill);
        }

        return combined.Count > 0 ? combined.ToArray() : Array.Empty<EnemySkill_SO>();
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
}
