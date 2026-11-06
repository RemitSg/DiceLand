using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public partial class BattleSkillInfoOverlay : MonoBehaviour
{
    [Header("Fonts")]
    [SerializeField] private TMP_FontAsset keywordFont;
    [SerializeField] private TMP_FontAsset bodyFont;
    [SerializeField] private int titleFontSize = 44;
    [SerializeField] private int bodyFontSize = 26;
    [SerializeField] private int buttonFontSize = 26;

    [Header("Colors")]
    [SerializeField] private Color backdropColor = new Color(0f, 0f, 0f, 0.55f);
    [SerializeField] private Color panelColor = new Color(0.1f, 0.08f, 0.06f, 0.96f);
    [SerializeField] private Color titleColor = Color.white;
    [SerializeField] private Color bodyColor = new Color(0.94f, 0.92f, 0.86f, 1f);
    [SerializeField] private Color accentColor = new Color(1f, 0.82f, 0.35f, 1f);
    [SerializeField] private Color triggerHighlightColor = new Color(1f, 0.78f, 0.3f, 1f);
    [SerializeField] private Color targetHighlightColor = new Color(0.48f, 0.9f, 1f, 1f);
    [SerializeField] private Color valueHighlightColor = new Color(1f, 0.45f, 0.45f, 1f);
    [SerializeField] private Color attackDicePowerColor = new Color(1f, 0.32f, 0.32f, 1f);
    [SerializeField] private Color defenseDicePowerColor = new Color(0.38f, 0.72f, 1f, 1f);
    [SerializeField] private Color obsoleteDicePowerColor = new Color(0.55f, 0.55f, 0.55f, 1f);
    [SerializeField] private Color skillButtonColor = new Color(0.25f, 0.2f, 0.12f, 0.98f);
    [SerializeField] private Color selectedSkillButtonColor = new Color(0.84f, 0.66f, 0.24f, 1f);
    [SerializeField] private Color skillButtonTextColor = Color.white;
    [SerializeField] private Color selectedSkillButtonTextColor = Color.black;

    [Header("Sprites")]
    [SerializeField] private Sprite panelBackgroundSprite;
    [SerializeField] private Sprite skillButtonSprite;
    [SerializeField] private Sprite selectedSkillButtonSprite;
    [SerializeField] private Sprite closeButtonSprite;

    [Header("Layout")]
    [SerializeField] private Vector2 panelSize = new Vector2(980f, 620f);
    [SerializeField] private Vector2 panelOffset = Vector2.zero;
    [SerializeField] private Vector2 skillButtonSize = new Vector2(280f, 82f);
    [SerializeField] private float skillButtonSpacing = 16f;
    [SerializeField] private float listLeftPadding = 36f;
    [SerializeField] private float listTopPadding = 122f;
    [SerializeField] private float detailLeftInset = 352f;
    [SerializeField] private float detailTopPadding = 120f;
    [SerializeField] private float detailRightPadding = 40f;
    [SerializeField] private float detailBottomPadding = 44f;
    [SerializeField] private float detailInnerPadding = 12f;
    [SerializeField] private float detailScrollbarWidth = 18f;

    [Header("Enemy Hover Layout")]
    [SerializeField] private Vector2 enemyHoverPanelSize = new Vector2(560f, 360f);
    [SerializeField] private Vector2 enemyHoverPanelOffset = new Vector2(-36f, -36f);
    [SerializeField] private int enemyHoverTitleFontSize = 32;
    [SerializeField] private int enemyHoverBodyFontSize = 24;
    [SerializeField] private Vector2 enemyHoverClauseViewportSize = new Vector2(512f, 160f);
    [SerializeField] private float enemyHoverClauseScrollSpeed = 24f;
    [SerializeField] private float enemyHoverClauseStartDelay = 3f;
    [SerializeField] private float enemyHoverClauseBottomPause = 3f;

    public bool IsVisible => panelRoot != null && panelRoot.gameObject.activeSelf;

    public event Action Hidden;
    public event Action<BattleAllyController> AllySkillSelectionShown;
    public event Action<BattleEnemyController, SpeedButton> EnemySkillInfoShown;
    public event Action<BattleEnemyController> EnemyDetailShown;

    private Canvas canvas;
    private RectTransform canvasRoot;
    private RectTransform panelRoot;
    private Button backdropButton;
    private Button closeButton;
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI subtitleText;
    private TextMeshProUGUI detailText;
    private ScrollRect detailScrollRect;
    private RectTransform detailViewport;
    private RectTransform detailContent;
    private RectTransform skillListRoot;
    private RectTransform hoverPanelRoot;
    private TextMeshProUGUI hoverTitleText;
    private TextMeshProUGUI hoverDetailText;
    private RectTransform hoverClauseViewport;
    private RectTransform hoverClauseContent;
    private TextMeshProUGUI hoverClauseText;
    private BattleSkillClauseAutoScroller hoverClauseAutoScroller;
    private readonly List<Button> skillButtons = new List<Button>();
    private readonly List<TextMeshProUGUI> skillButtonTexts = new List<TextMeshProUGUI>();
    private Action hideCallback;
    private BattleAllyController currentAlly;
    private BattleEnemyController currentEnemy;
    private SpeedButton currentAllySourceButton;
    private SpeedButton currentHoverSourceButton;
    private BattleButton cachedBattleButton;

    private void Awake() { Hide(false); }

    private void OnDisable() { HideImmediately(false); }
    private void OnDestroy() { HideImmediately(false); }

    private bool CanInspectEnemyInfo()
    {
        if (SpeedButton.CombatLocked || BattlePresentationContinueInputService.IsEnemyDetailInteractionBlocked) return false;
        ResolveBattleButton();
        return cachedBattleButton == null || cachedBattleButton.CanInspectCombatants;
    }

    private void LateUpdate()
    {
        if ((currentEnemy != null && IsVisible || currentHoverEnemy != null) && !CanInspectEnemyInfo()) Hide(false);
        if (canvasRoot == null) return;
        if (currentHoverEnemy == null && hoverPanelRoot != null && hoverPanelRoot.gameObject.activeSelf) HideEnemyHoverSkillInfo();
        if (IsVisible || currentHoverEnemy != null)
        {
            if (IsVisible && IsDetailFontSizeChanged())
                LayoutSkillContent(false);
            if (viewportPixels != new Vector2(Screen.width, Screen.height) || safePixels != Screen.safeArea)
                ApplyViewport(new Vector2(Screen.width, Screen.height), Screen.safeArea);
            AdvancePresentation(Time.unscaledDeltaTime);
        }
    }

    private bool IsDetailFontSizeChanged()
    {
        return viewMode == SkillViewMode.EnemyData
            ? enemyProfileDescription.fontSize != MainMenuSettingsPersistence.DetailFontSize(EnemyProfileBodySize)
            : descriptionBody.fontSize != MainMenuSettingsPersistence.DetailFontSize(SkillDescriptionSize);
    }

    public void ConfigureFonts(TMP_FontAsset keywordFontAsset, TMP_FontAsset bodyFontAsset)
    {
        if (keywordFontAsset != null) keywordFont = keywordFontAsset;
        if (bodyFontAsset != null) bodyFont = bodyFontAsset;
        ApplyFonts();
        if (IsVisible) RefreshCurrentView();
        if (currentHoverEnemy != null) RefreshHover(true);
    }

    public void ShowAllySkillSelection(BattleAllyController ally) { ShowAllySkillSelection(ally, null); }
    public void ShowAllySkillSelection(BattleAllyController ally, SpeedButton sourceButton)
    {
        if (ally == null) return;
        EnsureUi(); currentAlly = ally; currentEnemy = null; currentAllySourceButton = sourceButton;
        currentEnemySourceButton = null; viewMode = SkillViewMode.Ally;
        OpenForestPanel(); RenderCurrentSkill(true); RebuildSkillButtons(ally, sourceButton);
        AllySkillSelectionShown?.Invoke(ally);
    }

    public void ShowEnemySkillInfo(BattleEnemyController enemy) { ShowEnemySkillInfo(enemy, null); }
    public void ShowEnemySkillInfo(BattleEnemyController enemy, SpeedButton sourceButton)
    {
        if (enemy == null || !CanInspectEnemyInfo()) return;
        EnsureUi(); currentAlly = null; currentEnemy = enemy; currentAllySourceButton = null;
        currentEnemySourceButton = sourceButton; viewMode = SkillViewMode.EnemySkill;
        OpenForestPanel(); ClearSkillButtons(); RenderCurrentSkill(true);
        EnemySkillInfoShown?.Invoke(enemy, sourceButton);
    }

    public void ShowEnemyHoverSkillInfo(BattleEnemyController enemy, SpeedButton sourceButton)
    {
        if (enemy == null || !CanInspectEnemyInfo() || IsVisible) return;
        EnsureUi();
        bool changed = currentHoverEnemy != enemy || currentHoverSourceButton != sourceButton;
        currentHoverEnemy = enemy; currentHoverSourceButton = sourceButton;
        RefreshHover(changed);
    }

    public void HideEnemyHoverSkillInfo(SpeedButton sourceButton = null)
    {
        if (sourceButton != null && currentHoverSourceButton != null && sourceButton != currentHoverSourceButton) return;
        currentHoverEnemy = null; currentHoverSourceButton = null; hoverSkill = null;
        if (hoverPanelRoot != null) hoverPanelRoot.gameObject.SetActive(false);
        hoverClauseAutoScroller?.ResetToTop(); hoverProgress = 0;
    }

    public void ShowEnemyDetail(BattleEnemyController enemy)
    {
        if (enemy == null || !CanInspectEnemyInfo()) return;
        EnsureUi(); currentAlly = null; currentEnemy = enemy; currentAllySourceButton = null;
        currentEnemySourceButton = null; viewMode = SkillViewMode.EnemyData;
        OpenForestPanel(); ClearSkillButtons(); RenderCurrentSkill(true);
        if (UnityEngine.EventSystems.EventSystem.current != null)
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(footerClose.gameObject);
        EnemyDetailShown?.Invoke(enemy);
    }

    public void RefreshCurrentView()
    {
        if (IsVisible && !closing)
        {
            RenderCurrentSkill(false);
            if (viewMode == SkillViewMode.Ally) RebuildSkillButtons(currentAlly, currentAllySourceButton);
        }
        if (currentHoverEnemy != null) RefreshHover(false);
    }

    public void Hide() { Hide(true); }
    public void Hide(bool invokeCallback)
    {
        if (invokeCallback && Application.isPlaying && IsVisible && isActiveAndEnabled)
        {
            if (!closing) { closing = true; panelGroup.interactable = false; }
            return;
        }
        HideImmediately(invokeCallback);
    }

    private void EnsureUi() { if (canvasRoot == null) BuildForestUi(); }
    private void RebuildSkillButtons(BattleAllyController ally, SpeedButton sourceButton) { UpdateSkillChoices(ally, sourceButton); }
    private void ClearSkillButtons() { ClearSkillChoices(); }
    private void ApplyFonts()
    {
        if (canvasRoot == null) return;
        foreach (TMP_Text text in canvasRoot.GetComponentsInChildren<TMP_Text>(true))
            ApplyFont(text, text == titleText ? GetKeywordFont() : GetBodyFont());
    }

    private TMP_FontAsset GetKeywordFont()
    {
        return keywordFont != null ? keywordFont : bodyFont;
    }

    private TMP_FontAsset GetBodyFont()
    {
        return bodyFont != null ? bodyFont : keywordFont;
    }

    private Vector2 ResolveEnemyHoverClauseViewportSize()
    {
        float maxWidth = Mathf.Max(1f, enemyHoverPanelSize.x - 48f);
        float maxHeight = Mathf.Max(1f, enemyHoverPanelSize.y - 178f);
        float width = enemyHoverClauseViewportSize.x > 0f ? enemyHoverClauseViewportSize.x : maxWidth;
        float height = enemyHoverClauseViewportSize.y > 0f ? enemyHoverClauseViewportSize.y : maxHeight;
        return new Vector2(Mathf.Clamp(width, 1f, maxWidth), Mathf.Clamp(height, 1f, maxHeight));
    }

    private static void ApplyFont(TMP_Text text, TMP_FontAsset font)
    {
        if (text == null || font == null)
            return;

        text.font = font;
        text.UpdateMeshPadding();
        text.SetAllDirty();
    }

    private TextMeshProUGUI CreateText(RectTransform parent, string name, Vector2 anchoredPosition, Vector2 size, int fontSize, Color color, bool useKeywordFont, TextAlignmentOptions alignment)
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
        ConfigureText(text, fontSize, color, useKeywordFont, alignment);
        return text;
    }

    private void ConfigureText(TextMeshProUGUI text, int fontSize, Color color, bool useKeywordFont, TextAlignmentOptions alignment)
    {
        if (text == null)
            return;

        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
        text.enableWordWrapping = false;
        text.raycastTarget = false;
        ApplyFont(text, useKeywordFont ? GetKeywordFont() : GetBodyFont());
        ResourceKeywordText.Attach(text);
    }

    private static string BuildWeaponSubtitle(BattleAllyController ally, SpeedButton sourceButton)
    {
        Weapon_SO weaponData = ally != null ? ally.GetComponent<AllyController>()?.weapon?.weaponData : null;
        if (weaponData == null)
            return ally != null && ally.HasTemporarySkills ? GameText.Get("battle.skill.rules.temporary_skills_only", "仅使用临时技能") : GameText.Get("battle.skill.no_weapon", "未装备武器");

        string sourceLabel = ally != null && ally.TemporarySkillsOverrideWeaponPool
            ? GameText.Get("battle.skill.temporary", "临时技能")
            : weaponData.LocalizedName;
        EnemySkill_SO selectedSkill = ally != null ? ally.GetSelectedSkillForButton(sourceButton) : null;
        return GameText.Format("battle.skill.rules.current_skill", "{sourceLabel}  当前技能：{value}", ("sourceLabel", sourceLabel), ("value", selectedSkill?.LocalizedName ?? GameText.Get("battle.skill.unset", "未配置")));
    }

    private string BuildAllyDetail(BattleAllyController ally, SpeedButton sourceButton)
    {
        if (ally == null)
            return GameText.Get("battle.skill.rules.character_data_unavailable", "未找到角色数据。");

        EnemySkill_SO selectedSkill = ally.GetSelectedSkillForButton(sourceButton);
        return BuildSkillDetail(
            selectedSkill,
            GameText.Get("battle.skill.current", "当前技能"),
            true,
            selectedSkill != null ? new Func<int, EnemySkill_SO.DicePowerRange>(diceIndex => ally.GetDisplayedDicePowerRange(selectedSkill, diceIndex)) : null,
            ally);
    }

    private string BuildEnemyDetail(BattleEnemyController enemy)
    {
        if (enemy == null)
            return GameText.Get("battle.skill.rules.enemy_data_unavailable", "未找到敌方数据。");

        return BuildEnemySkillDetail(enemy, enemy.SelectedSkill, GameText.Get("battle.skill.current", "当前技能"));
    }

    private string BuildEnemySkillDetail(BattleEnemyController enemy, EnemySkill_SO skill, string header)
    {
        return BuildSkillDetail(
            skill,
            header,
            false,
            enemy != null ? new Func<int, EnemySkill_SO.DicePowerRange>(diceIndex => enemy.GetEffectiveDisplayedDicePowerRange(skill, diceIndex)) : null);
    }

    private void ResolveBattleButton()
    {
        if (cachedBattleButton == null)
            cachedBattleButton = FindObjectOfType<BattleButton>();
    }

    private string BuildEnemyDataDetail(BattleEnemyController enemy)
    {
        if (enemy == null || enemy.enemyData == null)
            return GameText.Get("battle.enemy.missing", "未找到敌人数据。");

        EnemyData_SO enemyData = enemy.enemyData;
        string description = string.IsNullOrWhiteSpace(enemyData.LocalizedDescription)
            ? GameText.Get("ui.no_description", "暂无描述。")
            : enemyData.LocalizedDescription;

        List<string> lines = new List<string>
        {
            GameText.Format("battle.skill.rules.name", "名称：{localizedName}", ("localizedName", enemyData.LocalizedName)),
            GameText.Format("battle.skill.rules.max_hp", "最大生命：{baseHP}", ("baseHP", enemyData.baseHP)),
            string.Empty,
            GameText.Get("battle.skill.rules.description", "描述："),
            description
        };

        if (enemyData.passiveDescriptions != null && enemyData.passiveDescriptions.Count > 0)
        {
            lines.Add(string.Empty);
            lines.Add(GameText.Get("battle.skill.rules.passives", "特殊被动："));
            for (int i = 0; i < enemyData.passiveDescriptions.Count; i++)
            {
                EnemyData_SO.PassiveDescriptionEntry entry = enemyData.passiveDescriptions[i];
                if (string.IsNullOrWhiteSpace(entry.passiveName) && string.IsNullOrWhiteSpace(entry.description))
                    continue;

                if (!string.IsNullOrWhiteSpace(entry.passiveName))
                    lines.Add($"• {BuildColoredToken(enemyData.Localize($"passive.{i}.name", entry.passiveName), GetPassiveNameColor(entry.displayColorType))}");
                if (!string.IsNullOrWhiteSpace(entry.description))
                    lines.Add("  " + enemyData.Localize($"passive.{i}.description", entry.description));
            }
        }

        return string.Join("\n", lines);
    }

    // Reuses the in-game effect formatter without constructing any UI.
    public string DescribeSkillRules(EnemySkill_SO skill, bool attackerIsAlly)
    {
        return BuildSkillDetail(skill, skill != null ? skill.LocalizedName : "", attackerIsAlly);
    }

    private string BuildSkillDetail(EnemySkill_SO skill, string header, bool attackerIsAlly, Func<int, EnemySkill_SO.DicePowerRange> rangeResolver = null, BattleAllyController allyContext = null)
    {
        if (skill == null)
            return GameText.Format("battle.skill.rules.no_skill_equipped", "{header}\n未配置技能。", ("header", header));

        List<string> rangeLines = new List<string>();
        List<string> effectLines = new List<string>();
        for (int i = 0; i < skill.diceCount; i++)
        {
            EnemySkill_SO.DicePowerRange range = rangeResolver != null ? rangeResolver(i) : skill.GetDicePowerRange(i);
            EnemySkill_SO.DicePowerRange baseRange = skill.GetDicePowerRange(i);
            string diceLabel = BuildDiceDisplayLabel(skill, i);
            string diceEffectLabel = BuildDiceEffectLabel(skill, i);
            rangeLines.Add($"{diceLabel}：{BuildDiceRangeLabel(skill, i, baseRange, range)}");

            effectLines.AddRange(BuildDiceRuleLines(skill, i, attackerIsAlly));
        }

        string description = string.IsNullOrWhiteSpace(skill.LocalizedDescription) ? GameText.Get("ui.no_description", "暂无描述。") : skill.LocalizedDescription;
        string targetPrioritySection = BuildTargetPrioritySummary(skill);
        string requirementSection = string.Empty;
        if (attackerIsAlly && skill.RequiresSelfStatusToUse && skill.requiredSelfStatusData != null)
        {
            int requiredStacks = Mathf.Max(1, skill.requiredSelfStatusStacks);
            int currentStacks = allyContext != null ? allyContext.GetStatusStacks(skill.requiredSelfStatusData) : 0;
            string currentSuffix = allyContext != null ? GameText.Format("battle.skill.rules.currently", "（当前 {currentStacks} 层）", ("currentStacks", currentStacks)) : string.Empty;
            string consumeSuffix = skill.keepRequiredSelfStatusAfterUse ? GameText.Get("battle.skill.rules.stacks_kept_after_use", "，施放后不消耗") : GameText.Get("battle.skill.rules.stacks_spent_on_use", "，施放后消耗");
            requirementSection = GameText.Format("battle.skill.rules.requires_stacks_of", "\n施放条件：自身至少拥有 {requiredStacks} 层 {localizedName}{currentSuffix}{consumeSuffix}", ("requiredStacks", requiredStacks), ("localizedName", skill.requiredSelfStatusData.LocalizedName), ("currentSuffix", currentSuffix), ("consumeSuffix", consumeSuffix));
        }
        string effectSection = effectLines.Count > 0
            ? GameText.Format("battle.skill.rules.effects", "\n\n词条：\n{join}", ("join", string.Join("\n", effectLines)))
            : string.Empty;
        return GameText.Format("battle.skill.rules.skill_energy_cost_dice_description", "{header}\n技能：{localizedName}\n能耗：{max}{requirementSection}\n骰子数：{diceCount}\n{join}{targetPrioritySection}{effectSection}\n\n描述：\n{description}", ("header", header), ("localizedName", skill.LocalizedName), ("max", Mathf.Max(0, skill.energyCost)), ("requirementSection", requirementSection), ("diceCount", skill.diceCount), ("join", string.Join("\n", rangeLines)), ("targetPrioritySection", targetPrioritySection), ("effectSection", effectSection), ("description", description));
    }

    // Both the catalog formatter and the card view use exactly the same per-die rules.
    private List<string> BuildDiceRuleLines(EnemySkill_SO skill, int diceIndex, bool attackerIsAlly, bool includeDiceLabel = true)
    {
        List<string> effectLines = new List<string>();
        string diceEffectLabel = BuildDiceEffectLabel(skill, diceIndex);
        EnemySkill_SO.DiceEffectSettings effectSettings = skill.GetDiceEffectSettings(diceIndex);
        HashSet<int> mergedStatusEffectIndices = new HashSet<int>();
        HashSet<int> mergedPowerModifierIndices = new HashSet<int>();
        HashSet<int> mergedStatusStackOperationIndices = new HashSet<int>();
        HashSet<int> mergedHealingEffectIndices = new HashSet<int>();
        HashSet<int> mergedBuiltInEffectIndices = new HashSet<int>();
        AppendConsumedStatusGainEffectLines(
            effectLines,
            diceEffectLabel,
            effectSettings,
            attackerIsAlly,
            mergedStatusEffectIndices,
            mergedPowerModifierIndices,
            mergedStatusStackOperationIndices,
            mergedHealingEffectIndices,
            mergedBuiltInEffectIndices);

        if (effectSettings.damageEffects != null)
        {
            for (int effectIndex = 0; effectIndex < effectSettings.damageEffects.Length; effectIndex++)
            {
                EnemySkill_SO.DamageEffectEntry effect = effectSettings.damageEffects[effectIndex];
                if (effect.damageAmount <= 0)
                    continue;

                string triggerLabel = BuildColoredToken(GetGeneralEffectTriggerLabel(effect.trigger), triggerHighlightColor);
                string targetLabel = BuildColoredToken(GetDamageTargetLabel(effect.targetMode, effect.randomTargetCount, attackerIsAlly), targetHighlightColor);
                string valueLabel = BuildColoredToken(effect.damageAmount.ToString(), valueHighlightColor);
                effectLines.Add(GameText.Format("battle.rule.damage", "{dice}：{trigger} 对 {target} 造成 {amount} 点伤害",
                    ("dice", diceEffectLabel), ("trigger", triggerLabel), ("target", targetLabel), ("amount", valueLabel)));
            }
        }

        if (effectSettings.statusEffects != null)
        {
            AppendMergedStatusBranchLines(effectLines, diceEffectLabel, effectSettings.statusEffects, mergedStatusEffectIndices, attackerIsAlly);
            for (int effectIndex = 0; effectIndex < effectSettings.statusEffects.Length; effectIndex++)
            {
                if (mergedStatusEffectIndices.Contains(effectIndex))
                    continue;

                EnemySkill_SO.StatusEffectEntry effect = effectSettings.statusEffects[effectIndex];
                if (effect.stackAmount <= 0)
                    continue;

                if (TryFindConsumedStatusForStatusEffect(effectSettings, effect, out int mergedStackOperationIndex))
                {
                    AppendConsumedStatusStatusEffectLine(effectLines, diceEffectLabel, effect, effectSettings.statusStackOperationEffects[mergedStackOperationIndex], attackerIsAlly);
                    mergedStatusStackOperationIndices.Add(mergedStackOperationIndex);
                    continue;
                }

                string triggerLabel = BuildColoredToken(GetGeneralEffectTriggerLabel(effect.trigger), triggerHighlightColor);
                string targetLabel = BuildColoredToken(GetDamageTargetLabel(effect.targetMode, effect.randomTargetCount, attackerIsAlly), targetHighlightColor);
                string stackLabel = BuildColoredToken(effect.stackAmount.ToString(), valueHighlightColor);
                string statusLabel = BuildColoredToken(GetStatusLabel(effect), valueHighlightColor);
                string conditionPrefix = BuildStatusEffectConditionPrefix(effect);
                string timingLabel = effect.applyTiming == EnemySkill_SO.StatusApplyTiming.NextRoundStart
                    ? BuildColoredToken(BuildStatusEffectTimingLabel(effect), triggerHighlightColor)
                    : string.Empty;
                effectLines.Add(effect.applyTiming == EnemySkill_SO.StatusApplyTiming.NextRoundStart
                    ? GameText.Format("battle.skill.rules.grant_stacks_of_to_at", "{diceEffectLabel}：{triggerLabel} {conditionPrefix}使 {targetLabel} 于 {timingLabel} 获得 {stackLabel} 层 {statusLabel}", ("diceEffectLabel", diceEffectLabel), ("triggerLabel", triggerLabel), ("conditionPrefix", conditionPrefix), ("targetLabel", targetLabel), ("timingLabel", timingLabel), ("stackLabel", stackLabel), ("statusLabel", statusLabel))
                    : GameText.Format("battle.skill.rules.grant_stacks_of_to", "{diceEffectLabel}：{triggerLabel} {conditionPrefix}使 {targetLabel} 获得 {stackLabel} 层 {statusLabel}", ("diceEffectLabel", diceEffectLabel), ("triggerLabel", triggerLabel), ("conditionPrefix", conditionPrefix), ("targetLabel", targetLabel), ("stackLabel", stackLabel), ("statusLabel", statusLabel)));
            }
        }

        if (effectSettings.statusBurstEffects != null)
        {
            for (int effectIndex = 0; effectIndex < effectSettings.statusBurstEffects.Length; effectIndex++)
            {
                EnemySkill_SO.StatusBurstDamageEffectEntry effect = effectSettings.statusBurstEffects[effectIndex];
                if (effect.damagePerStack <= 0)
                    continue;

                string triggerLabel = BuildColoredToken(GetGeneralEffectTriggerLabel(effect.trigger), triggerHighlightColor);
                string targetLabel = BuildColoredToken(GetDamageTargetLabel(effect.targetMode, effect.randomTargetCount, attackerIsAlly), targetHighlightColor);
                string multiplierLabel = BuildColoredToken(effect.damagePerStack.ToString(), valueHighlightColor);
                string statusLabel = BuildColoredToken(GetStatusLabel(effect), targetHighlightColor);
                effectLines.Add(effect.clearStacksAfterBurst
                    ? GameText.Format("battle.skill.rules.trigger_a_burst_on_deal_damage_per_stack", "{diceEffectLabel}：{triggerLabel} 对 {targetLabel} 引发 {multiplierLabel} 重 {statusLabel} 爆发，造成 {multiplierLabel2} x 层数 的额外伤害，并清空 {statusLabel2}", ("diceEffectLabel", diceEffectLabel), ("triggerLabel", triggerLabel), ("targetLabel", targetLabel), ("multiplierLabel", multiplierLabel), ("statusLabel", statusLabel), ("multiplierLabel2", multiplierLabel), ("statusLabel2", statusLabel))
                    : GameText.Format("battle.skill.rules.trigger_a_burst_on_deal_extra_damage_per", "{diceEffectLabel}：{triggerLabel} 对 {targetLabel} 引发 {multiplierLabel} 重 {statusLabel} 爆发，造成 {multiplierLabel2} x 层数 的额外伤害", ("diceEffectLabel", diceEffectLabel), ("triggerLabel", triggerLabel), ("targetLabel", targetLabel), ("multiplierLabel", multiplierLabel), ("statusLabel", statusLabel), ("multiplierLabel2", multiplierLabel)));
            }
        }

        if (effectSettings.statusStackOperationEffects != null)
        {
            for (int effectIndex = 0; effectIndex < effectSettings.statusStackOperationEffects.Length; effectIndex++)
            {
                if (mergedStatusStackOperationIndices.Contains(effectIndex))
                    continue;

                EnemySkill_SO.StatusStackOperationEffectEntry effect = effectSettings.statusStackOperationEffects[effectIndex];
                if (TryFindConsumedStatusPowerModifier(effectSettings, effect, out int mergedPowerModifierIndex))
                {
                    AppendConsumedStatusPowerModifierLine(effectLines, diceEffectLabel, effect, effectSettings.powerModifiers[mergedPowerModifierIndex], attackerIsAlly);
                    mergedPowerModifierIndices.Add(mergedPowerModifierIndex);
                    continue;
                }

                string triggerLabel = BuildColoredToken(GetGeneralEffectTriggerLabel(effect.trigger), triggerHighlightColor);
                string targetLabel = BuildColoredToken(GetDamageTargetLabel(effect.targetMode, effect.randomTargetCount, attackerIsAlly), targetHighlightColor);
                string statusLabel = BuildColoredToken(GetStatusLabel(effect), targetHighlightColor);

                if (effect.operationType == EnemySkill_SO.StatusStackOperationType.ClearStacks)
                {
                    effectLines.Add(GameText.Format("battle.skill.rules.clear_from", "{diceEffectLabel}：{triggerLabel} 清空 {targetLabel} 的 {statusLabel}", ("diceEffectLabel", diceEffectLabel), ("triggerLabel", triggerLabel), ("targetLabel", targetLabel), ("statusLabel", statusLabel)));
                }
                else
                {
                    string valueLabel = BuildColoredToken(Mathf.Max(1, effect.stackAmount).ToString(), valueHighlightColor);
                    effectLines.Add(GameText.Format("battle.skill.rules.remove_stacks_of_from", "{diceEffectLabel}：{triggerLabel} 使 {targetLabel} 的 {statusLabel} 减少 {valueLabel} 层", ("diceEffectLabel", diceEffectLabel), ("triggerLabel", triggerLabel), ("targetLabel", targetLabel), ("statusLabel", statusLabel), ("valueLabel", valueLabel)));
                }
            }
        }

        if (effectSettings.enemyStageTransitionEffects != null)
        {
            for (int effectIndex = 0; effectIndex < effectSettings.enemyStageTransitionEffects.Length; effectIndex++)
            {
                EnemySkill_SO.EnemyStageTransitionEffectEntry effect = effectSettings.enemyStageTransitionEffects[effectIndex];
                string triggerLabel = BuildColoredToken(GetGeneralEffectTriggerLabel(effect.trigger), triggerHighlightColor);
                string valueLabel = BuildColoredToken((effect.targetStageIndex + 1).ToString(), valueHighlightColor);
                if (effect.applyTiming == EnemySkill_SO.EnemyStageTransitionApplyTiming.NextRoundStart)
                    effectLines.Add(GameText.Format("battle.skill.rules.switch_to_phase_at_the_start_of_the", "{diceEffectLabel}：{triggerLabel} 于 下个回合开始时 切换至 第 {valueLabel} 阶段", ("diceEffectLabel", diceEffectLabel), ("triggerLabel", triggerLabel), ("valueLabel", valueLabel)));
                else
                    effectLines.Add(GameText.Format("battle.skill.rules.switch_to_phase", "{diceEffectLabel}：{triggerLabel} 切换至 第 {valueLabel} 阶段", ("diceEffectLabel", diceEffectLabel), ("triggerLabel", triggerLabel), ("valueLabel", valueLabel)));
            }
        }

        if (effectSettings.healingEffects != null)
        {
            for (int effectIndex = 0; effectIndex < effectSettings.healingEffects.Length; effectIndex++)
            {
                if (mergedHealingEffectIndices.Contains(effectIndex))
                    continue;

                EnemySkill_SO.HealingEffectEntry effect = effectSettings.healingEffects[effectIndex];
                string triggerLabel = BuildColoredToken(GetGeneralEffectTriggerLabel(effect.trigger), triggerHighlightColor);
                string targetLabel = BuildColoredToken(GetDamageTargetLabel(effect.targetMode, effect.randomTargetCount, attackerIsAlly), targetHighlightColor);
                string conditionPrefix = string.Empty;
                if (effect.useCondition)
                {
                    string conditionLabel = BuildColoredToken(GetHealingConditionLabel(effect), targetHighlightColor);
                    conditionPrefix = GameText.Format("battle.skill.rules.if", "若 {conditionLabel} ，则 ", ("conditionLabel", conditionLabel));
                }

                switch (effect.amountMode)
                {
                    case EnemySkill_SO.HealAmountMode.DamageDealt:
                        effectLines.Add(GameText.Format("battle.skill.rules.restore_hp_to_equal_to_damage_dealt", "{diceEffectLabel}：{triggerLabel} {conditionPrefix}使 {targetLabel} 恢复 相当于 造成伤害 的生命", ("diceEffectLabel", diceEffectLabel), ("triggerLabel", triggerLabel), ("conditionPrefix", conditionPrefix), ("targetLabel", targetLabel)));
                        break;
                    case EnemySkill_SO.HealAmountMode.StatusStacks:
                        string statusLabel = BuildColoredToken(GetStatusLabel(effect), targetHighlightColor);
                        string sourceLabel = BuildColoredToken(GetDamageTargetLabel(effect.statusStackSourceMode, effect.randomTargetCount, attackerIsAlly), targetHighlightColor);
                        string perStackLabel = BuildColoredToken(Mathf.Max(1, effect.healAmount).ToString(), valueHighlightColor);
                        effectLines.Add(GameText.Format("battle.skill.rules.heal_for_hp_per_stack_of_on", "{diceEffectLabel}：{triggerLabel} {conditionPrefix}使 {targetLabel} 根据 {sourceLabel} 的 {statusLabel} 层数，每层 恢复 {perStackLabel} 点生命", ("diceEffectLabel", diceEffectLabel), ("triggerLabel", triggerLabel), ("conditionPrefix", conditionPrefix), ("targetLabel", targetLabel), ("sourceLabel", sourceLabel), ("statusLabel", statusLabel), ("perStackLabel", perStackLabel)));
                        break;
                    default:
                        string healValueLabel = BuildColoredToken(effect.healAmount.ToString(), valueHighlightColor);
                        effectLines.Add(GameText.Format("battle.skill.rules.heal_for_hp", "{diceEffectLabel}：{triggerLabel} {conditionPrefix}使 {targetLabel} 恢复 {healValueLabel} 点生命", ("diceEffectLabel", diceEffectLabel), ("triggerLabel", triggerLabel), ("conditionPrefix", conditionPrefix), ("targetLabel", targetLabel), ("healValueLabel", healValueLabel)));
                        break;
                }
            }
        }

        AppendBuiltInEffectLines(effectLines, diceEffectLabel, effectSettings, attackerIsAlly, mergedBuiltInEffectIndices);
        AppendRangeModifierLines(effectLines, diceEffectLabel, effectSettings);
        AppendSummonEffectLines(effectLines, diceEffectLabel, effectSettings);

        if (effectSettings.powerModifiers != null)
        {
            for (int effectIndex = 0; effectIndex < effectSettings.powerModifiers.Length; effectIndex++)
            {
                if (mergedPowerModifierIndices.Contains(effectIndex))
                    continue;

                EnemySkill_SO.ConditionalPowerModifierEntry effect = effectSettings.powerModifiers[effectIndex];
                bool hasFlatBonus = effect.powerBonus != 0;
                bool hasScalingBonus = IsScalingPowerModifier(effect);
                if (!hasFlatBonus && !hasScalingBonus)
                    continue;

                string triggerLabel = BuildColoredToken(GetPowerModifierTriggerLabel(effect.trigger), triggerHighlightColor);

                if (effect.useCondition)
                {
                    string conditionLabel = BuildColoredToken(GetPowerModifierConditionLabel(effect), targetHighlightColor);
                    if (IsScalingPowerModifier(effect))
                    {
                        string perStepValueLabel = BuildColoredToken(FormatSignedInt(effect.powerBonusPerStep), valueHighlightColor);
                        string scalingPrefix = IsSpeedDifferenceScalingPowerModifier(effect) ? GameText.Get("battle.skill.rules.for_each_point_of_speed_above_the_target", "每高出 1 点速度，") : GameText.Get("battle.skill.rules.each_time_the_condition_is_met", "每达成一次条件，");
                        effectLines.Add(GameText.Format("battle.skill.rules.if_power", "{diceEffectLabel}：{triggerLabel} 若 {conditionLabel} ，则 {scalingPrefix}威力 {perStepValueLabel}{buildPowerModifierCapSuffix}", ("diceEffectLabel", diceEffectLabel), ("triggerLabel", triggerLabel), ("conditionLabel", conditionLabel), ("scalingPrefix", scalingPrefix), ("perStepValueLabel", perStepValueLabel), ("buildPowerModifierCapSuffix", BuildPowerModifierCapSuffix(effect))));
                    }
                    else
                    {
                        string valueLabel = BuildColoredToken(FormatSignedInt(effect.powerBonus), valueHighlightColor);
                        effectLines.Add(GameText.Format("battle.skill.rules.build_dice_rule_lines.if_power", "{diceEffectLabel}：{triggerLabel} 若 {conditionLabel} ，则 威力 {valueLabel}", ("diceEffectLabel", diceEffectLabel), ("triggerLabel", triggerLabel), ("conditionLabel", conditionLabel), ("valueLabel", valueLabel)));
                    }
                }
                else
                {
                    string valueLabel = BuildColoredToken(FormatSignedInt(effect.powerBonus), valueHighlightColor);
                    effectLines.Add(GameText.Format("battle.skill.rules.power", "{diceEffectLabel}：{triggerLabel} 威力 {valueLabel}", ("diceEffectLabel", diceEffectLabel), ("triggerLabel", triggerLabel), ("valueLabel", valueLabel)));
                }
            }
        }
        if (!includeDiceLabel)
        {
            string prefix = diceEffectLabel + "：";
            for (int n = 0; n < effectLines.Count; n++)
                if (effectLines[n].StartsWith(prefix, StringComparison.Ordinal))
                    effectLines[n] = effectLines[n].Substring(prefix.Length).TrimStart();
        }
        return effectLines;
    }

    private string BuildCompactSkillHoverDetail(EnemySkill_SO skill, bool attackerIsAlly)
    {
        return BuildCompactSkillHoverDetail(skill, attackerIsAlly, null);
    }

    private string BuildCompactSkillHoverDetail(EnemySkill_SO skill, bool attackerIsAlly, Func<int, EnemySkill_SO.DicePowerRange> rangeResolver)
    {
        string[] sections = BuildCompactSkillHoverSections(skill, attackerIsAlly, rangeResolver);
        return $"{sections[0]}\n\n{sections[1]}";
    }

    private string[] BuildCompactSkillHoverSections(EnemySkill_SO skill, bool attackerIsAlly, Func<int, EnemySkill_SO.DicePowerRange> rangeResolver)
    {
        if (skill == null)
            return new[] { GameText.Get("battle.skill.rules.build_compact_skill_hover_sections.no_skill_equipped", "未配置技能。"), GameText.Get("battle.skill.rules.effects_none", "词条：\n无") };

        List<string> rangeLines = new List<string>();
        List<string> effectLines = new List<string>();

        for (int i = 0; i < skill.diceCount; i++)
        {
            EnemySkill_SO.DicePowerRange range = rangeResolver != null ? rangeResolver(i) : skill.GetDicePowerRange(i);
            EnemySkill_SO.DicePowerRange baseRange = skill.GetDicePowerRange(i);
            string diceLabel = BuildDiceDisplayLabel(skill, i);
            string diceEffectLabel = BuildDiceEffectLabel(skill, i);
            rangeLines.Add($"{diceLabel}：{BuildDiceRangeLabel(skill, i, baseRange, range)}");

            EnemySkill_SO.DiceEffectSettings effectSettings = skill.GetDiceEffectSettings(i);
            AppendEffectLines(effectLines, diceEffectLabel, effectSettings, attackerIsAlly);
        }

        string rangeSection = string.Join("\n", rangeLines);
        string clauseSection = effectLines.Count > 0
            ? GameText.Format("battle.skill.rules.build_compact_skill_hover_sections.effects", "词条：\n{join}", ("join", string.Join("\n", effectLines)))
            : GameText.Get("battle.skill.rules.effects_none", "词条：\n无");
        return new[] { rangeSection, clauseSection };
    }

    private string BuildCompactEnemyHoverDetail(BattleEnemyController enemy, EnemySkill_SO skill)
    {
        string[] sections = BuildCompactEnemyHoverSections(enemy, skill);
        return $"{sections[0]}\n\n{sections[1]}";
    }

    private string[] BuildCompactEnemyHoverSections(BattleEnemyController enemy, EnemySkill_SO skill)
    {
        if (enemy == null)
            return BuildCompactSkillHoverSections(skill, false, null);

        if (skill == null)
            return new[] { GameText.Get("battle.skill.rules.build_compact_skill_hover_sections.no_skill_equipped", "未配置技能。"), GameText.Get("battle.skill.rules.effects_none", "词条：\n无") };

        List<string> rangeLines = new List<string>();
        List<string> effectLines = new List<string>();

        for (int i = 0; i < skill.diceCount; i++)
        {
            EnemySkill_SO.DicePowerRange range = enemy.GetEffectiveDisplayedDicePowerRange(skill, i);
            EnemySkill_SO.DicePowerRange baseRange = skill.GetDicePowerRange(i);
            string diceLabel = BuildDiceDisplayLabel(skill, i);
            string diceEffectLabel = BuildDiceEffectLabel(skill, i);
            rangeLines.Add($"{diceLabel}：{BuildDiceRangeLabel(skill, i, baseRange, range)}");

            EnemySkill_SO.DiceEffectSettings effectSettings = skill.GetDiceEffectSettings(i);
            AppendEffectLines(effectLines, diceEffectLabel, effectSettings, false);
        }

        string rangeSection = string.Join("\n", rangeLines);
        string clauseSection = effectLines.Count > 0
            ? GameText.Format("battle.skill.rules.build_compact_skill_hover_sections.effects", "词条：\n{join}", ("join", string.Join("\n", effectLines)))
            : GameText.Get("battle.skill.rules.effects_none", "词条：\n无");
        return new[] { rangeSection, clauseSection };
    }

    private void AppendEffectLines(List<string> effectLines, string diceEffectLabel, EnemySkill_SO.DiceEffectSettings effectSettings, bool attackerIsAlly)
    {
        HashSet<int> mergedStatusEffectIndices = new HashSet<int>();
        HashSet<int> mergedPowerModifierIndices = new HashSet<int>();
        HashSet<int> mergedStatusStackOperationIndices = new HashSet<int>();
        HashSet<int> mergedHealingEffectIndices = new HashSet<int>();
        HashSet<int> mergedBuiltInEffectIndices = new HashSet<int>();
        AppendConsumedStatusGainEffectLines(
            effectLines,
            diceEffectLabel,
            effectSettings,
            attackerIsAlly,
            mergedStatusEffectIndices,
            mergedPowerModifierIndices,
            mergedStatusStackOperationIndices,
            mergedHealingEffectIndices,
            mergedBuiltInEffectIndices);

        if (effectSettings.damageEffects != null)
        {
            for (int effectIndex = 0; effectIndex < effectSettings.damageEffects.Length; effectIndex++)
            {
                EnemySkill_SO.DamageEffectEntry effect = effectSettings.damageEffects[effectIndex];
                if (effect.damageAmount <= 0)
                    continue;

                string triggerLabel = BuildColoredToken(GetGeneralEffectTriggerLabel(effect.trigger), triggerHighlightColor);
                string targetLabel = BuildColoredToken(GetDamageTargetLabel(effect.targetMode, effect.randomTargetCount, attackerIsAlly), targetHighlightColor);
                string valueLabel = BuildColoredToken(effect.damageAmount.ToString(), valueHighlightColor);
                effectLines.Add(GameText.Format("battle.skill.rules.deal_damage_to", "{diceEffectLabel}：{triggerLabel} 对 {targetLabel} 造成 {valueLabel} 点伤害", ("diceEffectLabel", diceEffectLabel), ("triggerLabel", triggerLabel), ("targetLabel", targetLabel), ("valueLabel", valueLabel)));
            }
        }

        if (effectSettings.statusEffects != null)
        {
            AppendMergedStatusBranchLines(effectLines, diceEffectLabel, effectSettings.statusEffects, mergedStatusEffectIndices, attackerIsAlly);
            for (int effectIndex = 0; effectIndex < effectSettings.statusEffects.Length; effectIndex++)
            {
                if (mergedStatusEffectIndices.Contains(effectIndex))
                    continue;

                EnemySkill_SO.StatusEffectEntry effect = effectSettings.statusEffects[effectIndex];
                if (effect.stackAmount <= 0)
                    continue;

                if (TryFindConsumedStatusForStatusEffect(effectSettings, effect, out int mergedStackOperationIndex))
                {
                    AppendConsumedStatusStatusEffectLine(effectLines, diceEffectLabel, effect, effectSettings.statusStackOperationEffects[mergedStackOperationIndex], attackerIsAlly);
                    mergedStatusStackOperationIndices.Add(mergedStackOperationIndex);
                    continue;
                }

                string triggerLabel = BuildColoredToken(GetGeneralEffectTriggerLabel(effect.trigger), triggerHighlightColor);
                string targetLabel = BuildColoredToken(GetDamageTargetLabel(effect.targetMode, effect.randomTargetCount, attackerIsAlly), targetHighlightColor);
                string stackLabel = BuildColoredToken(effect.stackAmount.ToString(), valueHighlightColor);
                string statusLabel = BuildColoredToken(GetStatusLabel(effect), valueHighlightColor);
                string conditionPrefix = BuildStatusEffectConditionPrefix(effect);
                string timingLabel = effect.applyTiming == EnemySkill_SO.StatusApplyTiming.NextRoundStart
                    ? BuildColoredToken(BuildStatusEffectTimingLabel(effect), triggerHighlightColor)
                    : string.Empty;
                effectLines.Add(effect.applyTiming == EnemySkill_SO.StatusApplyTiming.NextRoundStart
                    ? GameText.Format("battle.skill.rules.grant_stacks_of_to_at", "{diceEffectLabel}：{triggerLabel} {conditionPrefix}使 {targetLabel} 于 {timingLabel} 获得 {stackLabel} 层 {statusLabel}", ("diceEffectLabel", diceEffectLabel), ("triggerLabel", triggerLabel), ("conditionPrefix", conditionPrefix), ("targetLabel", targetLabel), ("timingLabel", timingLabel), ("stackLabel", stackLabel), ("statusLabel", statusLabel))
                    : GameText.Format("battle.skill.rules.grant_stacks_of_to", "{diceEffectLabel}：{triggerLabel} {conditionPrefix}使 {targetLabel} 获得 {stackLabel} 层 {statusLabel}", ("diceEffectLabel", diceEffectLabel), ("triggerLabel", triggerLabel), ("conditionPrefix", conditionPrefix), ("targetLabel", targetLabel), ("stackLabel", stackLabel), ("statusLabel", statusLabel)));
            }
        }

        if (effectSettings.statusBurstEffects != null)
        {
            for (int effectIndex = 0; effectIndex < effectSettings.statusBurstEffects.Length; effectIndex++)
            {
                EnemySkill_SO.StatusBurstDamageEffectEntry effect = effectSettings.statusBurstEffects[effectIndex];
                if (effect.damagePerStack <= 0)
                    continue;

                string triggerLabel = BuildColoredToken(GetGeneralEffectTriggerLabel(effect.trigger), triggerHighlightColor);
                string targetLabel = BuildColoredToken(GetDamageTargetLabel(effect.targetMode, effect.randomTargetCount, attackerIsAlly), targetHighlightColor);
                string multiplierLabel = BuildColoredToken(effect.damagePerStack.ToString(), valueHighlightColor);
                string statusLabel = BuildColoredToken(GetStatusLabel(effect), targetHighlightColor);
                effectLines.Add(effect.clearStacksAfterBurst
                    ? GameText.Format("battle.skill.rules.trigger_a_burst_on_deal_damage_per_stack", "{diceEffectLabel}：{triggerLabel} 对 {targetLabel} 引发 {multiplierLabel} 重 {statusLabel} 爆发，造成 {multiplierLabel2} x 层数 的额外伤害，并清空 {statusLabel2}", ("diceEffectLabel", diceEffectLabel), ("triggerLabel", triggerLabel), ("targetLabel", targetLabel), ("multiplierLabel", multiplierLabel), ("statusLabel", statusLabel), ("multiplierLabel2", multiplierLabel), ("statusLabel2", statusLabel))
                    : GameText.Format("battle.skill.rules.trigger_a_burst_on_deal_extra_damage_per", "{diceEffectLabel}：{triggerLabel} 对 {targetLabel} 引发 {multiplierLabel} 重 {statusLabel} 爆发，造成 {multiplierLabel2} x 层数 的额外伤害", ("diceEffectLabel", diceEffectLabel), ("triggerLabel", triggerLabel), ("targetLabel", targetLabel), ("multiplierLabel", multiplierLabel), ("statusLabel", statusLabel), ("multiplierLabel2", multiplierLabel)));
            }
        }

        if (effectSettings.statusStackOperationEffects != null)
        {
            for (int effectIndex = 0; effectIndex < effectSettings.statusStackOperationEffects.Length; effectIndex++)
            {
                if (mergedStatusStackOperationIndices.Contains(effectIndex))
                    continue;

                EnemySkill_SO.StatusStackOperationEffectEntry effect = effectSettings.statusStackOperationEffects[effectIndex];
                if (TryFindConsumedStatusPowerModifier(effectSettings, effect, out int mergedPowerModifierIndex))
                {
                    AppendConsumedStatusPowerModifierLine(effectLines, diceEffectLabel, effect, effectSettings.powerModifiers[mergedPowerModifierIndex], attackerIsAlly);
                    mergedPowerModifierIndices.Add(mergedPowerModifierIndex);
                    continue;
                }

                string triggerLabel = BuildColoredToken(GetGeneralEffectTriggerLabel(effect.trigger), triggerHighlightColor);
                string targetLabel = BuildColoredToken(GetDamageTargetLabel(effect.targetMode, effect.randomTargetCount, attackerIsAlly), targetHighlightColor);
                string statusLabel = BuildColoredToken(GetStatusLabel(effect), targetHighlightColor);

                if (effect.operationType == EnemySkill_SO.StatusStackOperationType.ClearStacks)
                    effectLines.Add(GameText.Format("battle.skill.rules.clear_from", "{diceEffectLabel}：{triggerLabel} 清空 {targetLabel} 的 {statusLabel}", ("diceEffectLabel", diceEffectLabel), ("triggerLabel", triggerLabel), ("targetLabel", targetLabel), ("statusLabel", statusLabel)));
                else
                    effectLines.Add(GameText.Format("battle.skill.rules.append_effect_lines.remove_stacks_of_from", "{diceEffectLabel}：{triggerLabel} 使 {targetLabel} 的 {statusLabel} 减少 {buildColoredToken} 层", ("diceEffectLabel", diceEffectLabel), ("triggerLabel", triggerLabel), ("targetLabel", targetLabel), ("statusLabel", statusLabel), ("buildColoredToken", BuildColoredToken(Mathf.Max(1, effect.stackAmount).ToString(), valueHighlightColor))));
            }
        }

        if (effectSettings.enemyStageTransitionEffects != null)
        {
            for (int effectIndex = 0; effectIndex < effectSettings.enemyStageTransitionEffects.Length; effectIndex++)
            {
                EnemySkill_SO.EnemyStageTransitionEffectEntry effect = effectSettings.enemyStageTransitionEffects[effectIndex];
                string triggerLabel = BuildColoredToken(GetGeneralEffectTriggerLabel(effect.trigger), triggerHighlightColor);
                string valueLabel = BuildColoredToken((effect.targetStageIndex + 1).ToString(), valueHighlightColor);
                effectLines.Add(effect.applyTiming == EnemySkill_SO.EnemyStageTransitionApplyTiming.NextRoundStart
                    ? GameText.Format("battle.skill.rules.switch_to_phase_at_the_start_of_the", "{diceEffectLabel}：{triggerLabel} 于 下个回合开始时 切换至 第 {valueLabel} 阶段", ("diceEffectLabel", diceEffectLabel), ("triggerLabel", triggerLabel), ("valueLabel", valueLabel))
                    : GameText.Format("battle.skill.rules.switch_to_phase", "{diceEffectLabel}：{triggerLabel} 切换至 第 {valueLabel} 阶段", ("diceEffectLabel", diceEffectLabel), ("triggerLabel", triggerLabel), ("valueLabel", valueLabel)));
            }
        }

        if (effectSettings.healingEffects != null)
        {
            for (int effectIndex = 0; effectIndex < effectSettings.healingEffects.Length; effectIndex++)
            {
                if (mergedHealingEffectIndices.Contains(effectIndex))
                    continue;

                EnemySkill_SO.HealingEffectEntry effect = effectSettings.healingEffects[effectIndex];
                string triggerLabel = BuildColoredToken(GetGeneralEffectTriggerLabel(effect.trigger), triggerHighlightColor);
                string targetLabel = BuildColoredToken(GetDamageTargetLabel(effect.targetMode, effect.randomTargetCount, attackerIsAlly), targetHighlightColor);
                string conditionPrefix = string.Empty;
                if (effect.useCondition)
                    conditionPrefix = GameText.Format("battle.skill.rules.append_effect_lines.if", "若 {buildColoredToken} ，则 ", ("buildColoredToken", BuildColoredToken(GetHealingConditionLabel(effect), targetHighlightColor)));

                switch (effect.amountMode)
                {
                    case EnemySkill_SO.HealAmountMode.DamageDealt:
                        effectLines.Add(GameText.Format("battle.skill.rules.restore_hp_to_equal_to_damage_dealt", "{diceEffectLabel}：{triggerLabel} {conditionPrefix}使 {targetLabel} 恢复 相当于 造成伤害 的生命", ("diceEffectLabel", diceEffectLabel), ("triggerLabel", triggerLabel), ("conditionPrefix", conditionPrefix), ("targetLabel", targetLabel)));
                        break;
                    case EnemySkill_SO.HealAmountMode.StatusStacks:
                        effectLines.Add(GameText.Format("battle.skill.rules.append_effect_lines.heal_for_hp_per_stack_of_on", "{diceEffectLabel}：{triggerLabel} {conditionPrefix}使 {targetLabel} 根据 {buildColoredToken} 的 {buildColoredToken2} 层数，每层 恢复 {buildColoredToken3} 点生命", ("diceEffectLabel", diceEffectLabel), ("triggerLabel", triggerLabel), ("conditionPrefix", conditionPrefix), ("targetLabel", targetLabel), ("buildColoredToken", BuildColoredToken(GetDamageTargetLabel(effect.statusStackSourceMode, effect.randomTargetCount, attackerIsAlly), targetHighlightColor)), ("buildColoredToken2", BuildColoredToken(GetStatusLabel(effect), targetHighlightColor)), ("buildColoredToken3", BuildColoredToken(Mathf.Max(1, effect.healAmount).ToString(), valueHighlightColor))));
                        break;
                    default:
                        effectLines.Add(GameText.Format("battle.skill.rules.append_effect_lines.heal_for_hp", "{diceEffectLabel}：{triggerLabel} {conditionPrefix}使 {targetLabel} 恢复 {buildColoredToken} 点生命", ("diceEffectLabel", diceEffectLabel), ("triggerLabel", triggerLabel), ("conditionPrefix", conditionPrefix), ("targetLabel", targetLabel), ("buildColoredToken", BuildColoredToken(effect.healAmount.ToString(), valueHighlightColor))));
                        break;
                }
            }
        }

        AppendBuiltInEffectLines(effectLines, diceEffectLabel, effectSettings, attackerIsAlly, mergedBuiltInEffectIndices);
        AppendRangeModifierLines(effectLines, diceEffectLabel, effectSettings);
        AppendSummonEffectLines(effectLines, diceEffectLabel, effectSettings);

        if (effectSettings.powerModifiers != null)
        {
            for (int effectIndex = 0; effectIndex < effectSettings.powerModifiers.Length; effectIndex++)
            {
                if (mergedPowerModifierIndices.Contains(effectIndex))
                    continue;

                EnemySkill_SO.ConditionalPowerModifierEntry effect = effectSettings.powerModifiers[effectIndex];
                bool hasFlatBonus = effect.powerBonus != 0;
                bool hasScalingBonus = IsScalingPowerModifier(effect);
                if (!hasFlatBonus && !hasScalingBonus)
                    continue;

                string triggerLabel = BuildColoredToken(GetPowerModifierTriggerLabel(effect.trigger), triggerHighlightColor);
                if (effect.useCondition)
                {
                    string conditionLabel = BuildColoredToken(GetPowerModifierConditionLabel(effect), targetHighlightColor);
                    string scalingPrefix = IsSpeedDifferenceScalingPowerModifier(effect) ? GameText.Get("battle.skill.rules.for_each_point_of_speed_above_the_target", "每高出 1 点速度，") : GameText.Get("battle.skill.rules.each_time_the_condition_is_met", "每达成一次条件，");
                    effectLines.Add(IsScalingPowerModifier(effect)
                        ? GameText.Format("battle.skill.rules.append_effect_lines.if_power", "{diceEffectLabel}：{triggerLabel} 若 {conditionLabel} ，则 {scalingPrefix}威力 {buildColoredToken}{buildPowerModifierCapSuffix}", ("diceEffectLabel", diceEffectLabel), ("triggerLabel", triggerLabel), ("conditionLabel", conditionLabel), ("scalingPrefix", scalingPrefix), ("buildColoredToken", BuildColoredToken(FormatSignedInt(effect.powerBonusPerStep), valueHighlightColor)), ("buildPowerModifierCapSuffix", BuildPowerModifierCapSuffix(effect)))
                        : GameText.Format("battle.skill.rules.append_effect_lines.if_power_2", "{diceEffectLabel}：{triggerLabel} 若 {conditionLabel} ，则 威力 {buildColoredToken}", ("diceEffectLabel", diceEffectLabel), ("triggerLabel", triggerLabel), ("conditionLabel", conditionLabel), ("buildColoredToken", BuildColoredToken(FormatSignedInt(effect.powerBonus), valueHighlightColor))));
                }
                else
                {
                    effectLines.Add(GameText.Format("battle.skill.rules.append_effect_lines.power", "{diceEffectLabel}：{triggerLabel} 威力 {buildColoredToken}", ("diceEffectLabel", diceEffectLabel), ("triggerLabel", triggerLabel), ("buildColoredToken", BuildColoredToken(FormatSignedInt(effect.powerBonus), valueHighlightColor))));
                }
            }
        }
    }

    private void AppendConsumedStatusGainEffectLines(
        List<string> effectLines,
        string diceEffectLabel,
        EnemySkill_SO.DiceEffectSettings effectSettings,
        bool attackerIsAlly,
        HashSet<int> mergedStatusEffectIndices,
        HashSet<int> mergedPowerModifierIndices,
        HashSet<int> mergedStatusStackOperationIndices,
        HashSet<int> mergedHealingEffectIndices,
        HashSet<int> mergedBuiltInEffectIndices)
    {
        if (effectSettings.statusStackOperationEffects == null)
            return;

        for (int operationIndex = 0; operationIndex < effectSettings.statusStackOperationEffects.Length; operationIndex++)
        {
            if (mergedStatusStackOperationIndices.Contains(operationIndex))
                continue;

            EnemySkill_SO.StatusStackOperationEffectEntry operation = effectSettings.statusStackOperationEffects[operationIndex];
            if (!CanMergeConsumedStatusGainOperation(operation))
                continue;

            List<string> gains = new List<string>();
            CollectConsumedStatusStatusEffectGains(effectSettings, operation, gains, mergedStatusEffectIndices, attackerIsAlly);
            CollectConsumedStatusPowerModifierGains(effectSettings, operation, gains, mergedPowerModifierIndices);
            CollectConsumedStatusHealingGains(effectSettings, operation, gains, mergedHealingEffectIndices, attackerIsAlly);
            CollectConsumedStatusBuiltInGains(effectSettings, operation, gains, mergedBuiltInEffectIndices, attackerIsAlly);

            if (gains.Count == 0)
                continue;

            string triggerLabel = BuildColoredToken(GetGeneralEffectTriggerLabel(operation.trigger), triggerHighlightColor);
            string consumeTargetLabel = BuildColoredToken(GetConsumedStatusTargetLabel(operation, attackerIsAlly), targetHighlightColor);
            string consumedStackLabel = BuildColoredToken(Mathf.Max(1, operation.stackAmount).ToString(), valueHighlightColor);
            string consumedStatusLabel = BuildColoredToken(GetStatusLabel(operation), targetHighlightColor);
            string gainText = JoinGainClauses(gains);
            effectLines.Add(GameText.Format("battle.skill.rules.spend_stacks_of_on_to", "{diceEffectLabel}：{triggerLabel} 消耗 {consumeTargetLabel} {consumedStackLabel} 层 {consumedStatusLabel}，来使 {gainText}", ("diceEffectLabel", diceEffectLabel), ("triggerLabel", triggerLabel), ("consumeTargetLabel", consumeTargetLabel), ("consumedStackLabel", consumedStackLabel), ("consumedStatusLabel", consumedStatusLabel), ("gainText", gainText)));
            mergedStatusStackOperationIndices.Add(operationIndex);
        }
    }

    private static bool CanMergeConsumedStatusGainOperation(EnemySkill_SO.StatusStackOperationEffectEntry operation)
    {
        return operation.operationType == EnemySkill_SO.StatusStackOperationType.ReduceStacks
            && operation.useCondition
            && (operation.condition == EnemySkill_SO.PowerModifierCondition.TargetHasStatus
                || operation.condition == EnemySkill_SO.PowerModifierCondition.TargetStatusStacksAtLeast)
            && IsSameOperationAndConditionStatus(operation);
    }

    private void CollectConsumedStatusStatusEffectGains(
        EnemySkill_SO.DiceEffectSettings effectSettings,
        EnemySkill_SO.StatusStackOperationEffectEntry operation,
        List<string> gains,
        HashSet<int> mergedStatusEffectIndices,
        bool attackerIsAlly)
    {
        if (effectSettings.statusEffects == null)
            return;

        for (int i = 0; i < effectSettings.statusEffects.Length; i++)
        {
            if (mergedStatusEffectIndices.Contains(i))
                continue;

            EnemySkill_SO.StatusEffectEntry effect = effectSettings.statusEffects[i];
            if (effect.stackAmount <= 0 || !CanMergeConsumedStatusGainEffect(operation, effect))
                continue;

            string targetLabel = BuildColoredToken(GetDamageTargetLabel(effect.targetMode, effect.randomTargetCount, attackerIsAlly), targetHighlightColor);
            gains.Add(GameText.Format("battle.skill.rules.collect_consumed_status_status_effect_gains.grant_stacks_of_to", "{targetLabel}{buildConsumedGainStatusTimingText}获得 {buildColoredToken} 层 {buildColoredToken2}", ("targetLabel", targetLabel), ("buildConsumedGainStatusTimingText", BuildConsumedGainStatusTimingText(effect)), ("buildColoredToken", BuildColoredToken(effect.stackAmount.ToString(), valueHighlightColor)), ("buildColoredToken2", BuildColoredToken(GetStatusLabel(effect), valueHighlightColor))));
            mergedStatusEffectIndices.Add(i);
        }
    }

    private void CollectConsumedStatusPowerModifierGains(
        EnemySkill_SO.DiceEffectSettings effectSettings,
        EnemySkill_SO.StatusStackOperationEffectEntry operation,
        List<string> gains,
        HashSet<int> mergedPowerModifierIndices)
    {
        if (effectSettings.powerModifiers == null)
            return;

        for (int i = 0; i < effectSettings.powerModifiers.Length; i++)
        {
            if (mergedPowerModifierIndices.Contains(i))
                continue;

            EnemySkill_SO.ConditionalPowerModifierEntry effect = effectSettings.powerModifiers[i];
            if (!CanMergeConsumedStatusGainEffect(operation, effect))
                continue;

            if (IsScalingPowerModifier(effect))
            {
                gains.Add(GameText.Format("battle.skill.rules.give_this_die_power_each_time_the_condition", "本骰子每达成一次条件，威力 {buildColoredToken}{buildPowerModifierCapSuffix}", ("buildColoredToken", BuildColoredToken(FormatSignedInt(effect.powerBonusPerStep), valueHighlightColor)), ("buildPowerModifierCapSuffix", BuildPowerModifierCapSuffix(effect))));
            }
            else
            {
                gains.Add(GameText.Format("battle.skill.rules.give_this_die_power", "本骰子威力 {buildColoredToken}", ("buildColoredToken", BuildColoredToken(FormatSignedInt(effect.powerBonus), valueHighlightColor))));
            }

            mergedPowerModifierIndices.Add(i);
        }
    }

    private void CollectConsumedStatusHealingGains(
        EnemySkill_SO.DiceEffectSettings effectSettings,
        EnemySkill_SO.StatusStackOperationEffectEntry operation,
        List<string> gains,
        HashSet<int> mergedHealingEffectIndices,
        bool attackerIsAlly)
    {
        if (effectSettings.healingEffects == null)
            return;

        for (int i = 0; i < effectSettings.healingEffects.Length; i++)
        {
            if (mergedHealingEffectIndices.Contains(i))
                continue;

            EnemySkill_SO.HealingEffectEntry effect = effectSettings.healingEffects[i];
            if (!CanMergeConsumedStatusGainEffect(operation, effect))
                continue;

            string targetLabel = BuildColoredToken(GetDamageTargetLabel(effect.targetMode, effect.randomTargetCount, attackerIsAlly), targetHighlightColor);
            switch (effect.amountMode)
            {
                case EnemySkill_SO.HealAmountMode.DamageDealt:
                    gains.Add(GameText.Format("battle.skill.rules.heal_for_hp_equal_to_damage_dealt", "{targetLabel}恢复相当于造成伤害的生命", ("targetLabel", targetLabel)));
                    break;
                case EnemySkill_SO.HealAmountMode.StatusStacks:
                    string sourceLabel = BuildColoredToken(GetDamageTargetLabel(effect.statusStackSourceMode, effect.randomTargetCount, attackerIsAlly), targetHighlightColor);
                    string statusLabel = BuildColoredToken(GetStatusLabel(effect), targetHighlightColor);
                    string perStackLabel = BuildColoredToken(Mathf.Max(1, effect.healAmount).ToString(), valueHighlightColor);
                    gains.Add(GameText.Format("battle.skill.rules.collect_consumed_status_healing_gains.heal_for_hp_per_stack_of_on", "{targetLabel}根据 {sourceLabel} 的 {statusLabel} 层数，每层恢复 {perStackLabel} 点生命", ("targetLabel", targetLabel), ("sourceLabel", sourceLabel), ("statusLabel", statusLabel), ("perStackLabel", perStackLabel)));
                    break;
                default:
                    gains.Add(GameText.Format("battle.skill.rules.collect_consumed_status_healing_gains.heal_for_hp", "{targetLabel}恢复 {buildColoredToken} 点生命", ("targetLabel", targetLabel), ("buildColoredToken", BuildColoredToken(effect.healAmount.ToString(), valueHighlightColor))));
                    break;
            }

            mergedHealingEffectIndices.Add(i);
        }
    }

    private void AppendMergedStatusBranchLines(
        List<string> effectLines,
        string diceEffectLabel,
        EnemySkill_SO.StatusEffectEntry[] statusEffects,
        HashSet<int> mergedStatusEffectIndices,
        bool attackerIsAlly)
    {
        for (int i = 0; i < statusEffects.Length; i++)
        {
            if (mergedStatusEffectIndices.Contains(i))
                continue;

            EnemySkill_SO.StatusEffectEntry baseEffect = statusEffects[i];
            if (!CanBeUnmarkedBranch(baseEffect))
                continue;

            for (int j = 0; j < statusEffects.Length; j++)
            {
                if (i == j || mergedStatusEffectIndices.Contains(j))
                    continue;

                EnemySkill_SO.StatusEffectEntry markedEffect = statusEffects[j];
                if (!CanBeMarkedBranchPair(baseEffect, markedEffect))
                    continue;

                AppendMergedStatusBranchLine(effectLines, diceEffectLabel, baseEffect, markedEffect, attackerIsAlly);
                mergedStatusEffectIndices.Add(i);
                mergedStatusEffectIndices.Add(j);
                break;
            }
        }
    }

    private void AppendMergedStatusBranchLine(
        List<string> effectLines,
        string diceEffectLabel,
        EnemySkill_SO.StatusEffectEntry baseEffect,
        EnemySkill_SO.StatusEffectEntry markedEffect,
        bool attackerIsAlly)
    {
        string triggerLabel = BuildColoredToken(GetGeneralEffectTriggerLabel(baseEffect.trigger), triggerHighlightColor);
        string targetLabel = BuildColoredToken(GetDamageTargetLabel(baseEffect.targetMode, baseEffect.randomTargetCount, attackerIsAlly), targetHighlightColor);
        string baseStackLabel = BuildColoredToken(baseEffect.stackAmount.ToString(), valueHighlightColor);
        string baseStatusLabel = BuildColoredToken(GetStatusLabel(baseEffect), valueHighlightColor);
        string markedStackLabel = BuildColoredToken(markedEffect.stackAmount.ToString(), valueHighlightColor);
        string markedStatusLabel = BuildColoredToken(GetStatusLabel(markedEffect), valueHighlightColor);
        string timingLabel = BuildColoredToken(BuildStatusEffectTimingLabel(baseEffect), triggerHighlightColor);
        string conditionStatusLabel = BuildColoredToken(GetStatusEffectConditionStatusLabel(markedEffect), targetHighlightColor);

        effectLines.Add(
            GameText.Format("battle.skill.rules.apply_stacks_of_to", "{diceEffectLabel}：{triggerLabel} {timingLabel}对{targetLabel}施加 {baseStackLabel} 层 {baseStatusLabel}。\n", ("diceEffectLabel", diceEffectLabel), ("triggerLabel", triggerLabel), ("timingLabel", timingLabel), ("targetLabel", targetLabel), ("baseStackLabel", baseStackLabel), ("baseStatusLabel", baseStatusLabel)) +
            GameText.Format("battle.skill.rules.if_has_apply_stacks_of_to_instead", "若{targetLabel}已有{conditionStatusLabel}，改为{timingLabel}对{targetLabel2}施加 {markedStackLabel} 层 {markedStatusLabel}。", ("targetLabel", targetLabel), ("conditionStatusLabel", conditionStatusLabel), ("timingLabel", timingLabel), ("targetLabel2", targetLabel), ("markedStackLabel", markedStackLabel), ("markedStatusLabel", markedStatusLabel)));
    }

    private static bool CanBeUnmarkedBranch(EnemySkill_SO.StatusEffectEntry effect)
    {
        return effect.stackAmount > 0
            && effect.useCondition
            && effect.condition == EnemySkill_SO.PowerModifierCondition.TargetStatusStacksLessOrEqual
            && Mathf.Max(0, effect.requiredConditionStatusStacks) <= 0
            && effect.applyTiming == EnemySkill_SO.StatusApplyTiming.NextRoundStart;
    }

    private static bool CanBeMarkedBranchPair(
        EnemySkill_SO.StatusEffectEntry baseEffect,
        EnemySkill_SO.StatusEffectEntry markedEffect)
    {
        return markedEffect.stackAmount > 0
            && markedEffect.useCondition
            && markedEffect.condition == EnemySkill_SO.PowerModifierCondition.TargetStatusStacksAtLeast
            && Mathf.Max(1, markedEffect.requiredConditionStatusStacks) == 1
            && markedEffect.applyTiming == EnemySkill_SO.StatusApplyTiming.NextRoundStart
            && markedEffect.trigger == baseEffect.trigger
            && markedEffect.targetMode == baseEffect.targetMode
            && Mathf.Max(1, markedEffect.randomTargetCount) == Mathf.Max(1, baseEffect.randomTargetCount)
            && Mathf.Max(1, markedEffect.applyAfterRoundStarts) == Mathf.Max(1, baseEffect.applyAfterRoundStarts)
            && Mathf.Max(1, markedEffect.applyRepeatCount) == Mathf.Max(1, baseEffect.applyRepeatCount)
            && IsSameStatus(markedEffect.conditionStatusData, markedEffect.conditionStatusType, baseEffect.statusData, baseEffect.statusType);
    }

    private static bool IsSameStatus(
        BattleStatus_SO leftData,
        EnemySkill_SO.DiceStatusType leftType,
        BattleStatus_SO rightData,
        EnemySkill_SO.DiceStatusType rightType)
    {
        if (leftData != null || rightData != null)
            return leftData == rightData;

        return leftType == rightType;
    }

    private void CollectConsumedStatusBuiltInGains(
        EnemySkill_SO.DiceEffectSettings effectSettings,
        EnemySkill_SO.StatusStackOperationEffectEntry operation,
        List<string> gains,
        HashSet<int> mergedBuiltInEffectIndices,
        bool attackerIsAlly)
    {
        if (effectSettings.builtInEffects == null)
            return;

        for (int i = 0; i < effectSettings.builtInEffects.Length; i++)
        {
            if (mergedBuiltInEffectIndices.Contains(i))
                continue;

            EnemySkill_SO.BuiltInEffectEntry effect = effectSettings.builtInEffects[i];
            if (!CanMergeConsumedStatusGainEffect(operation, effect))
                continue;

            string targetLabel = BuildColoredToken(GetBuiltInActionTargetLabel(effect, attackerIsAlly), targetHighlightColor);
            switch (effect.effectType)
            {
                case EnemySkill_SO.BuiltInEffectType.Heal:
                    if (effect.amount <= 0)
                        continue;
                    gains.Add(GameText.Format("battle.skill.rules.collect_consumed_status_healing_gains.heal_for_hp", "{targetLabel}恢复 {buildColoredToken} 点生命", ("targetLabel", targetLabel), ("buildColoredToken", BuildColoredToken(effect.amount.ToString(), valueHighlightColor))));
                    break;
                case EnemySkill_SO.BuiltInEffectType.Damage:
                    if (effect.amount <= 0)
                        continue;
                    gains.Add(GameText.Format("battle.skill.rules.deal_extra_damage_to", "对{targetLabel}额外造成 {buildColoredToken} 点伤害", ("targetLabel", targetLabel), ("buildColoredToken", BuildColoredToken(effect.amount.ToString(), valueHighlightColor))));
                    break;
                case EnemySkill_SO.BuiltInEffectType.ApplyStatus:
                    if (effect.statusStackAmount <= 0)
                        continue;
                    gains.Add(GameText.Format("battle.skill.rules.collect_consumed_status_built_in_gains.grant_stacks_of_to", "{targetLabel}{buildConsumedGainBuiltInTimingText}获得 {buildColoredToken} 层 {buildColoredToken2}", ("targetLabel", targetLabel), ("buildConsumedGainBuiltInTimingText", BuildConsumedGainBuiltInTimingText(effect)), ("buildColoredToken", BuildColoredToken(effect.statusStackAmount.ToString(), valueHighlightColor)), ("buildColoredToken2", BuildColoredToken(GetBuiltInPrimaryStatusLabel(effect), valueHighlightColor))));
                    break;
                case EnemySkill_SO.BuiltInEffectType.RepeatCurrentDie:
                    gains.Add(GameText.Get("battle.skill.rules.roll_this_die_again", "重复使用该骰子"));
                    break;
                default:
                    continue;
            }

            mergedBuiltInEffectIndices.Add(i);
        }
    }

    private static bool CanMergeConsumedStatusGainEffect(
        EnemySkill_SO.StatusStackOperationEffectEntry operation,
        EnemySkill_SO.StatusEffectEntry effect)
    {
        return effect.trigger == operation.trigger
            && effect.useCondition
            && IsConditionCompatibleWithConsumedStatus(operation, effect.condition, effect.requiredConditionStatusStacks)
            && IsSameConditionStatus(effect, operation);
    }

    private static bool CanMergeConsumedStatusGainEffect(
        EnemySkill_SO.StatusStackOperationEffectEntry operation,
        EnemySkill_SO.HealingEffectEntry effect)
    {
        return effect.trigger == operation.trigger
            && effect.useCondition
            && IsConditionCompatibleWithConsumedStatus(operation, effect.condition, effect.requiredConditionStatusStacks)
            && IsSameConditionStatus(effect, operation);
    }

    private static bool CanMergeConsumedStatusGainEffect(
        EnemySkill_SO.StatusStackOperationEffectEntry operation,
        EnemySkill_SO.ConditionalPowerModifierEntry effect)
    {
        return effect.trigger == operation.trigger
            && effect.useCondition
            && (effect.powerBonus != 0 || IsScalingPowerModifier(effect))
            && IsConditionCompatibleWithConsumedStatus(operation, effect.condition, effect.requiredStatusStacks)
            && IsSameConditionStatus(effect, operation);
    }

    private static bool CanMergeConsumedStatusGainEffect(
        EnemySkill_SO.StatusStackOperationEffectEntry operation,
        EnemySkill_SO.BuiltInEffectEntry effect)
    {
        return effect.trigger == operation.trigger
            && (HasBuiltInConsumedStatusCondition(operation, effect)
                || HasBuiltInGeneralConsumedStatusCondition(operation, effect));
    }

    private static bool HasBuiltInConsumedStatusCondition(
        EnemySkill_SO.StatusStackOperationEffectEntry operation,
        EnemySkill_SO.BuiltInEffectEntry effect)
    {
        return effect.useStatusCondition
            && effect.conditionScope == EnemySkill_SO.BuiltInConditionScope.CurrentTarget
            && Mathf.Max(1, effect.requiredStatusStacks) >= Mathf.Max(1, operation.stackAmount)
            && IsSameConditionStatus(effect, operation);
    }

    private static bool HasBuiltInGeneralConsumedStatusCondition(
        EnemySkill_SO.StatusStackOperationEffectEntry operation,
        EnemySkill_SO.BuiltInEffectEntry effect)
    {
        return effect.useCondition
            && IsConditionCompatibleWithConsumedStatus(operation, effect.condition, effect.requiredConditionStatusStacks)
            && IsSameBuiltInGeneralConditionStatus(effect, operation);
    }

    private static bool IsConditionCompatibleWithConsumedStatus(
        EnemySkill_SO.StatusStackOperationEffectEntry operation,
        EnemySkill_SO.PowerModifierCondition condition,
        int requiredStacks)
    {
        if (condition == EnemySkill_SO.PowerModifierCondition.TargetHasStatus)
            return true;

        return condition == EnemySkill_SO.PowerModifierCondition.TargetStatusStacksAtLeast
            && Mathf.Max(1, requiredStacks) >= Mathf.Max(1, operation.stackAmount);
    }

    private string BuildConsumedGainStatusTimingText(EnemySkill_SO.StatusEffectEntry effect)
    {
        if (effect.applyTiming != EnemySkill_SO.StatusApplyTiming.NextRoundStart)
            return string.Empty;

        return GameText.Format("battle.skill.rules.at", "于 {buildConsumedStatusStatusEffectTimingLabel} ", ("buildConsumedStatusStatusEffectTimingLabel", BuildConsumedStatusStatusEffectTimingLabel(effect)));
    }

    private static string BuildStatusEffectTimingLabel(EnemySkill_SO.StatusEffectEntry effect)
    {
        return Mathf.Max(1, effect.applyRepeatCount) > 1
            ? GameText.Format("battle.skill.rules.the_start_of_the_next_rounds", "接下来 {max} 个回合开始时", ("max", Mathf.Max(1, effect.applyRepeatCount)))
            : GameText.Format("battle.skill.rules.the_start_of_the_round_in_rounds", "下 {max} 个回合开始时", ("max", Mathf.Max(1, effect.applyAfterRoundStarts)));
    }

    private string BuildConsumedGainBuiltInTimingText(EnemySkill_SO.BuiltInEffectEntry effect)
    {
        if (effect.applyTiming != EnemySkill_SO.StatusApplyTiming.NextRoundStart)
            return string.Empty;

        int repeatCount = Mathf.Max(1, effect.applyRepeatCount);
        if (repeatCount > 1)
            return GameText.Format("battle.skill.rules.build_consumed_gain_built_in_timing_text.at", "于 {buildColoredToken} ", ("buildColoredToken", BuildColoredToken(GameText.Format("battle.skill.rules.build_consumed_gain_built_in_timing_text.the_start_of_the_next_rounds", "接下来 {repeatCount} 个回合开始时", ("repeatCount", repeatCount)), triggerHighlightColor)));

        int delayRounds = Mathf.Max(1, effect.applyAfterRoundStarts);
        return GameText.Format("battle.skill.rules.build_consumed_gain_built_in_timing_text.at", "于 {buildColoredToken} ", ("buildColoredToken", BuildColoredToken(delayRounds == 1 ? GameText.Get("battle.skill.rules.next_round", "下回合") : GameText.Format("battle.skill.rules.build_consumed_gain_built_in_timing_text.the_start_of_the_round_in_rounds", "下 {delayRounds} 个回合开始时", ("delayRounds", delayRounds)), triggerHighlightColor)));
    }

    private static string JoinGainClauses(List<string> gains)
    {
        if (gains == null || gains.Count == 0)
            return string.Empty;
        if (gains.Count == 1)
            return gains[0];
        if (gains.Count == 2)
            return GameText.Format("battle.skill.rules.and", "{value}，并使{value2}", ("value", gains[0]), ("value2", gains[1]));

        List<string> leading = new List<string>();
        for (int i = 0; i < gains.Count - 1; i++)
            leading.Add(gains[i]);

        return GameText.Format("battle.skill.rules.join_gain_clauses.and", "{join}，并使{value}", ("join", string.Join("，", leading)), ("value", gains[gains.Count - 1]));
    }

    private bool TryFindConsumedStatusForStatusEffect(
        EnemySkill_SO.DiceEffectSettings effectSettings,
        EnemySkill_SO.StatusEffectEntry statusEffect,
        out int stackOperationIndex)
    {
        stackOperationIndex = -1;
        if (effectSettings.statusStackOperationEffects == null)
            return false;

        if (statusEffect.targetMode != EnemySkill_SO.DamageTargetMode.CurrentTarget
            || !statusEffect.useCondition
            || statusEffect.condition != EnemySkill_SO.PowerModifierCondition.TargetStatusStacksAtLeast)
            return false;

        int requiredStacks = Mathf.Max(1, statusEffect.requiredConditionStatusStacks);
        for (int i = 0; i < effectSettings.statusStackOperationEffects.Length; i++)
        {
            EnemySkill_SO.StatusStackOperationEffectEntry stackOperation = effectSettings.statusStackOperationEffects[i];
            if (stackOperation.trigger != statusEffect.trigger
                || stackOperation.targetMode != statusEffect.targetMode
                || stackOperation.operationType != EnemySkill_SO.StatusStackOperationType.ReduceStacks
                || Mathf.Max(1, stackOperation.stackAmount) != requiredStacks
                || !stackOperation.useCondition
                || stackOperation.condition != EnemySkill_SO.PowerModifierCondition.TargetStatusStacksAtLeast
                || Mathf.Max(1, stackOperation.requiredConditionStatusStacks) != requiredStacks
                || !IsSameConditionStatus(statusEffect, stackOperation)
                || !IsSameOperationAndConditionStatus(stackOperation))
                continue;

            stackOperationIndex = i;
            return true;
        }

        return false;
    }

    private void AppendConsumedStatusStatusEffectLine(
        List<string> effectLines,
        string diceEffectLabel,
        EnemySkill_SO.StatusEffectEntry statusEffect,
        EnemySkill_SO.StatusStackOperationEffectEntry stackOperation,
        bool attackerIsAlly)
    {
        string triggerLabel = BuildColoredToken(GetGeneralEffectTriggerLabel(statusEffect.trigger), triggerHighlightColor);
        string targetLabel = BuildColoredToken(GetConsumedStatusTargetLabel(stackOperation, attackerIsAlly), targetHighlightColor);
        string consumedStackLabel = BuildColoredToken(Mathf.Max(1, stackOperation.stackAmount).ToString(), valueHighlightColor);
        string consumedStatusLabel = BuildColoredToken(GetStatusLabel(stackOperation), targetHighlightColor);
        string appliedStackLabel = BuildColoredToken(statusEffect.stackAmount.ToString(), valueHighlightColor);
        string appliedStatusLabel = BuildColoredToken(GetStatusLabel(statusEffect), valueHighlightColor);
        string timingLabel = BuildConsumedGainStatusTimingText(statusEffect);

        effectLines.Add(GameText.Format("battle.skill.rules.spend_stacks_of_on_to_grant_stacks_of", "{diceEffectLabel}：{triggerLabel} 消耗{targetLabel} {consumedStackLabel} 层{consumedStatusLabel}，使{targetLabel2}{timingLabel}获得 {appliedStackLabel} 层{appliedStatusLabel}", ("diceEffectLabel", diceEffectLabel), ("triggerLabel", triggerLabel), ("targetLabel", targetLabel), ("consumedStackLabel", consumedStackLabel), ("consumedStatusLabel", consumedStatusLabel), ("targetLabel2", targetLabel), ("timingLabel", timingLabel), ("appliedStackLabel", appliedStackLabel), ("appliedStatusLabel", appliedStatusLabel)));
    }

    private string BuildConsumedStatusStatusEffectTimingLabel(EnemySkill_SO.StatusEffectEntry statusEffect)
    {
        if (statusEffect.applyTiming != EnemySkill_SO.StatusApplyTiming.NextRoundStart)
            return string.Empty;

        int repeatCount = Mathf.Max(1, statusEffect.applyRepeatCount);
        if (repeatCount > 1)
            return BuildColoredToken(GameText.Format("battle.skill.rules.build_consumed_gain_built_in_timing_text.the_start_of_the_next_rounds", "接下来 {repeatCount} 个回合开始时", ("repeatCount", repeatCount)), triggerHighlightColor);

        int delayRounds = Mathf.Max(1, statusEffect.applyAfterRoundStarts);
        return BuildColoredToken(delayRounds == 1 ? GameText.Get("battle.skill.rules.next_round", "下回合") : GameText.Format("battle.skill.rules.build_consumed_gain_built_in_timing_text.the_start_of_the_round_in_rounds", "下 {delayRounds} 个回合开始时", ("delayRounds", delayRounds)), triggerHighlightColor);
    }

    private static bool IsSameConditionStatus(
        EnemySkill_SO.StatusEffectEntry statusEffect,
        EnemySkill_SO.StatusStackOperationEffectEntry stackOperation)
    {
        if (statusEffect.conditionStatusData != null || stackOperation.conditionStatusData != null)
            return statusEffect.conditionStatusData != null && statusEffect.conditionStatusData == stackOperation.conditionStatusData;

        return statusEffect.conditionStatusType == stackOperation.conditionStatusType;
    }

    private static bool IsSameConditionStatus(
        EnemySkill_SO.HealingEffectEntry healingEffect,
        EnemySkill_SO.StatusStackOperationEffectEntry stackOperation)
    {
        BattleStatus_SO healingStatusData = healingEffect.conditionStatusData != null ? healingEffect.conditionStatusData : healingEffect.statusData;
        if (healingStatusData != null || stackOperation.conditionStatusData != null)
            return healingStatusData != null && healingStatusData == stackOperation.conditionStatusData;

        EnemySkill_SO.DiceStatusType healingStatusType = healingEffect.conditionStatusData != null
            ? healingEffect.conditionStatusType
            : healingEffect.statusType;
        return healingStatusType == stackOperation.conditionStatusType;
    }

    private static bool IsSameConditionStatus(
        EnemySkill_SO.BuiltInEffectEntry builtInEffect,
        EnemySkill_SO.StatusStackOperationEffectEntry stackOperation)
    {
        if (builtInEffect.conditionStatusData != null || stackOperation.conditionStatusData != null)
            return builtInEffect.conditionStatusData != null && builtInEffect.conditionStatusData == stackOperation.conditionStatusData;

        return builtInEffect.conditionStatusType == stackOperation.conditionStatusType;
    }

    private static bool IsSameConditionStatus(
        EnemySkill_SO.ConditionalPowerModifierEntry powerModifier,
        EnemySkill_SO.StatusStackOperationEffectEntry stackOperation)
    {
        if (powerModifier.targetStatusData != null || stackOperation.conditionStatusData != null)
            return powerModifier.targetStatusData != null && powerModifier.targetStatusData == stackOperation.conditionStatusData;

        return powerModifier.targetStatusType == stackOperation.conditionStatusType;
    }

    private static bool IsSameBuiltInGeneralConditionStatus(
        EnemySkill_SO.BuiltInEffectEntry builtInEffect,
        EnemySkill_SO.StatusStackOperationEffectEntry stackOperation)
    {
        if (builtInEffect.targetStatusData != null || stackOperation.conditionStatusData != null)
            return builtInEffect.targetStatusData != null && builtInEffect.targetStatusData == stackOperation.conditionStatusData;

        return builtInEffect.targetStatusType == stackOperation.conditionStatusType;
    }

    private static bool IsSameOperationAndConditionStatus(EnemySkill_SO.StatusStackOperationEffectEntry stackOperation)
    {
        if (stackOperation.statusData != null || stackOperation.conditionStatusData != null)
            return stackOperation.statusData != null && stackOperation.statusData == stackOperation.conditionStatusData;

        return stackOperation.statusType == stackOperation.conditionStatusType;
    }

    private bool TryFindConsumedStatusPowerModifier(
        EnemySkill_SO.DiceEffectSettings effectSettings,
        EnemySkill_SO.StatusStackOperationEffectEntry stackOperation,
        out int powerModifierIndex)
    {
        powerModifierIndex = -1;
        if (effectSettings.powerModifiers == null)
            return false;

        if (stackOperation.operationType != EnemySkill_SO.StatusStackOperationType.ReduceStacks
            || stackOperation.targetMode != EnemySkill_SO.DamageTargetMode.CurrentTarget
            || stackOperation.useCondition)
            return false;

        int stackAmount = Mathf.Max(1, stackOperation.stackAmount);
        for (int i = 0; i < effectSettings.powerModifiers.Length; i++)
        {
            EnemySkill_SO.ConditionalPowerModifierEntry modifier = effectSettings.powerModifiers[i];
            if (modifier.trigger != stackOperation.trigger
                || !modifier.useCondition
                || modifier.condition != EnemySkill_SO.PowerModifierCondition.TargetStatusStacksPerThreshold
                || modifier.powerBonusPerStep == 0
                || Mathf.Max(1, modifier.requiredStatusStacks) != stackAmount
                || !IsSameStatus(stackOperation, modifier))
                continue;

            powerModifierIndex = i;
            return true;
        }

        return false;
    }

    private void AppendConsumedStatusPowerModifierLine(
        List<string> effectLines,
        string diceEffectLabel,
        EnemySkill_SO.StatusStackOperationEffectEntry stackOperation,
        EnemySkill_SO.ConditionalPowerModifierEntry modifier,
        bool attackerIsAlly)
    {
        string triggerLabel = BuildColoredToken(GetGeneralEffectTriggerLabel(stackOperation.trigger), triggerHighlightColor);
        string targetLabel = BuildColoredToken(GetConsumedStatusTargetLabel(stackOperation, attackerIsAlly), targetHighlightColor);
        string stackLabel = BuildColoredToken(Mathf.Max(1, stackOperation.stackAmount).ToString(), valueHighlightColor);
        string statusLabel = BuildColoredToken(GetStatusLabel(stackOperation), targetHighlightColor);
        string powerLabel = BuildColoredToken(FormatSignedInt(modifier.powerBonusPerStep), valueHighlightColor);

        effectLines.Add(GameText.Format("battle.skill.rules.spend_the_enemy_s_for_each_stacks_of", "{diceEffectLabel}：{triggerLabel} 消耗敌人的{statusLabel}层数，每消耗{targetLabel} {stackLabel} 层{statusLabel2}，使骰子威力 {powerLabel}{buildPowerModifierCapSuffix}", ("diceEffectLabel", diceEffectLabel), ("triggerLabel", triggerLabel), ("statusLabel", statusLabel), ("targetLabel", targetLabel), ("stackLabel", stackLabel), ("statusLabel2", statusLabel), ("powerLabel", powerLabel), ("buildPowerModifierCapSuffix", BuildPowerModifierCapSuffix(modifier))));
    }

    private string BuildPowerModifierCapSuffix(EnemySkill_SO.ConditionalPowerModifierEntry modifier)
    {
        return modifier.maxPowerBonus > 0
            ? GameText.Format("battle.skill.rules.max", "，最多 {buildColoredToken}", ("buildColoredToken", BuildColoredToken(FormatSignedInt(modifier.maxPowerBonus), valueHighlightColor)))
            : string.Empty;
    }

    private static bool IsScalingPowerModifier(EnemySkill_SO.ConditionalPowerModifierEntry modifier)
    {
        return modifier.powerBonusPerStep != 0
            && (modifier.condition == EnemySkill_SO.PowerModifierCondition.TargetStatusStacksPerThreshold
                || IsSpeedDifferenceScalingPowerModifier(modifier));
    }

    private static bool IsSpeedDifferenceScalingPowerModifier(EnemySkill_SO.ConditionalPowerModifierEntry modifier)
    {
        return modifier.condition == EnemySkill_SO.PowerModifierCondition.SelfSpeedHigherThanTarget
            || modifier.condition == EnemySkill_SO.PowerModifierCondition.SelfSpeedLowerThanTarget;
    }

    private static bool IsSameStatus(
        EnemySkill_SO.StatusStackOperationEffectEntry stackOperation,
        EnemySkill_SO.ConditionalPowerModifierEntry modifier)
    {
        if (stackOperation.statusData != null || modifier.targetStatusData != null)
            return stackOperation.statusData != null && stackOperation.statusData == modifier.targetStatusData;

        return stackOperation.statusType == modifier.targetStatusType;
    }

    private static string GetConsumedStatusTargetLabel(EnemySkill_SO.StatusStackOperationEffectEntry effect, bool attackerIsAlly)
    {
        if (effect.targetMode == EnemySkill_SO.DamageTargetMode.CurrentTarget)
            return attackerIsAlly ? GameText.Get("battle.skill.rules.current_target_enemy", "当前目标（敌人）") : GameText.Get("battle.skill.rules.current_target", "当前目标");

        return GetDamageTargetLabel(effect.targetMode, effect.randomTargetCount, attackerIsAlly);
    }

    private void AppendBuiltInEffectLines(
        List<string> effectLines,
        string diceEffectLabel,
        EnemySkill_SO.DiceEffectSettings effectSettings,
        bool attackerIsAlly,
        HashSet<int> mergedBuiltInEffectIndices)
    {
        if (effectSettings.builtInEffects == null)
            return;

        for (int effectIndex = 0; effectIndex < effectSettings.builtInEffects.Length; effectIndex++)
        {
            if (mergedBuiltInEffectIndices != null && mergedBuiltInEffectIndices.Contains(effectIndex))
                continue;

            EnemySkill_SO.BuiltInEffectEntry effect = effectSettings.builtInEffects[effectIndex];
            string triggerLabel = BuildColoredToken(GetGeneralEffectTriggerLabel(effect.trigger), triggerHighlightColor);
            string targetLabel = BuildColoredToken(GetBuiltInActionTargetLabel(effect, attackerIsAlly), targetHighlightColor);
            string conditionPrefix = BuildBuiltInConditionPrefix(effect);

            switch (effect.effectType)
            {
                case EnemySkill_SO.BuiltInEffectType.Heal:
                    if (effect.amount <= 0)
                        continue;
                    effectLines.Add(GameText.Format("battle.skill.rules.append_built_in_effect_lines.heal_for_hp", "{diceEffectLabel}：{triggerLabel} {conditionPrefix}使{targetLabel}恢复 {buildColoredToken} 点生命", ("diceEffectLabel", diceEffectLabel), ("triggerLabel", triggerLabel), ("conditionPrefix", conditionPrefix), ("targetLabel", targetLabel), ("buildColoredToken", BuildColoredToken(effect.amount.ToString(), valueHighlightColor))));
                    break;
                case EnemySkill_SO.BuiltInEffectType.Damage:
                    if (effect.amount <= 0)
                        continue;
                    effectLines.Add(GameText.Format("battle.skill.rules.append_built_in_effect_lines.deal_extra_damage_to", "{diceEffectLabel}：{triggerLabel} {conditionPrefix}对{targetLabel}额外造成 {buildColoredToken} 点伤害", ("diceEffectLabel", diceEffectLabel), ("triggerLabel", triggerLabel), ("conditionPrefix", conditionPrefix), ("targetLabel", targetLabel), ("buildColoredToken", BuildColoredToken(effect.amount.ToString(), valueHighlightColor))));
                    break;
                case EnemySkill_SO.BuiltInEffectType.ApplyStatus:
                    if (effect.statusStackAmount <= 0)
                        continue;
                    effectLines.Add(GameText.Format("battle.skill.rules.append_built_in_effect_lines.grant_stacks_of_to", "{diceEffectLabel}：{triggerLabel} {conditionPrefix}使{targetLabel}{buildBuiltInTimingPrefix}获得 {buildColoredToken} 层{buildColoredToken2}", ("diceEffectLabel", diceEffectLabel), ("triggerLabel", triggerLabel), ("conditionPrefix", conditionPrefix), ("targetLabel", targetLabel), ("buildBuiltInTimingPrefix", BuildBuiltInTimingPrefix(effect)), ("buildColoredToken", BuildColoredToken(effect.statusStackAmount.ToString(), valueHighlightColor)), ("buildColoredToken2", BuildColoredToken(GetBuiltInPrimaryStatusLabel(effect), valueHighlightColor))));
                    break;
                case EnemySkill_SO.BuiltInEffectType.TransferRandomNegativeStatus:
                    if (effect.amount <= 0 && effect.fallbackAmount <= 0)
                        continue;
                    string transferLine = GameText.Format("battle.skill.rules.transfer_stacks_of_a_random_debuff_from_the", "{diceEffectLabel}：{triggerLabel} {conditionPrefix}将自身 {buildColoredToken} 层随机负面状态转移给 {targetLabel}", ("diceEffectLabel", diceEffectLabel), ("triggerLabel", triggerLabel), ("conditionPrefix", conditionPrefix), ("buildColoredToken", BuildColoredToken(Mathf.Max(1, effect.amount).ToString(), valueHighlightColor)), ("targetLabel", targetLabel));
                    if (effect.fallbackAmount > 0)
                    {
                        transferLine += GameText.Format("battle.skill.rules.if_no_debuff_can_be_transferred_grant_stacks", "；若未找到可转移的负面状态，则使{buildColoredToken} {buildBuiltInTimingPrefix}获得 {buildColoredToken2} 层 {buildColoredToken3}", ("buildColoredToken", BuildColoredToken(GameText.Get("battle.skill.rules.self", "自己"), targetHighlightColor)), ("buildBuiltInTimingPrefix", BuildBuiltInTimingPrefix(effect)), ("buildColoredToken2", BuildColoredToken(effect.fallbackAmount.ToString(), valueHighlightColor)), ("buildColoredToken3", BuildColoredToken(GetBuiltInPrimaryStatusLabel(effect), valueHighlightColor)));
                    }

                    effectLines.Add(transferLine);
                    break;
                case EnemySkill_SO.BuiltInEffectType.QueueNextDicePowerBonus:
                    if (effect.amount == 0)
                        continue;
                    effectLines.Add(GameText.Format("battle.skill.rules.power_for_the_next_die_in_this_skill", "{diceEffectLabel}：{triggerLabel} {conditionPrefix}使该技能的下一个骰子威力 {buildColoredToken}", ("diceEffectLabel", diceEffectLabel), ("triggerLabel", triggerLabel), ("conditionPrefix", conditionPrefix), ("buildColoredToken", BuildColoredToken(FormatSignedInt(effect.amount), valueHighlightColor))));
                    break;
                case EnemySkill_SO.BuiltInEffectType.DamageByTargetStatusStacksDivided:
                    if (effect.amount <= 0)
                        continue;
                    int threshold = Mathf.Max(1, effect.requiredConditionStatusStacks);
                    string statusLabel = BuildColoredToken(GetBuiltInGeneralConditionStatusLabel(effect), valueHighlightColor);
                    string damageLabel = effect.amount == 1
                        ? GameText.Format("battle.skill.rules.equal_to_s_stacks", "相当于 {targetLabel} {statusLabel} 层数 / {buildColoredToken}", ("targetLabel", targetLabel), ("statusLabel", statusLabel), ("buildColoredToken", BuildColoredToken(threshold.ToString(), valueHighlightColor)))
                        : GameText.Format("battle.skill.rules.equal_to_damage_per_stacks_of_on", "相当于 {targetLabel} 每 {buildColoredToken} 层 {statusLabel} {buildColoredToken2} 点", ("targetLabel", targetLabel), ("buildColoredToken", BuildColoredToken(threshold.ToString(), valueHighlightColor)), ("statusLabel", statusLabel), ("buildColoredToken2", BuildColoredToken(effect.amount.ToString(), valueHighlightColor)));
                    effectLines.Add(GameText.Format("battle.skill.rules.append_built_in_effect_lines.deal_extra_damage_to_2", "{diceEffectLabel}：{triggerLabel} {conditionPrefix}对{targetLabel}额外造成 {damageLabel} 的伤害", ("diceEffectLabel", diceEffectLabel), ("triggerLabel", triggerLabel), ("conditionPrefix", conditionPrefix), ("targetLabel", targetLabel), ("damageLabel", damageLabel)));
                    break;
                case EnemySkill_SO.BuiltInEffectType.RepeatCurrentDie:
                    effectLines.Add(GameText.Format("battle.skill.rules.append_built_in_effect_lines.roll_this_die_again", "{diceEffectLabel}：{triggerLabel} {conditionPrefix}重复使用该骰子", ("diceEffectLabel", diceEffectLabel), ("triggerLabel", triggerLabel), ("conditionPrefix", conditionPrefix)));
                    break;
                case EnemySkill_SO.BuiltInEffectType.TriggerTargetLinkedStatus:
                    effectLines.Add(GameText.Format("battle.skill.rules.trigger_on", "{diceEffectLabel}：{triggerLabel} {conditionPrefix}结算{targetLabel}的 {buildColoredToken} 状态", ("diceEffectLabel", diceEffectLabel), ("triggerLabel", triggerLabel), ("conditionPrefix", conditionPrefix), ("targetLabel", targetLabel), ("buildColoredToken", BuildColoredToken(GetBuiltInGeneralConditionStatusLabel(effect), valueHighlightColor))));
                    break;
            }
        }
    }

    private void AppendSummonEffectLines(List<string> effectLines, string diceEffectLabel, EnemySkill_SO.DiceEffectSettings effectSettings)
    {
        if (effectSettings.summonEffects == null)
            return;

        for (int effectIndex = 0; effectIndex < effectSettings.summonEffects.Length; effectIndex++)
        {
            EnemySkill_SO.SummonEnemyEffectEntry effect = effectSettings.summonEffects[effectIndex];
            if (effect.enemyData == null)
                continue;

            string triggerLabel = BuildColoredToken(GetGeneralEffectTriggerLabel(effect.trigger), triggerHighlightColor);
            string countLabel = BuildColoredToken(BattleSummonEffectService.NormalizeSummonCount(effect.summonCount).ToString(), valueHighlightColor);
            string enemyLabel = BuildColoredToken(GetSummonEnemyLabel(effect, effectIndex), targetHighlightColor);
            effectLines.Add(GameText.Format("battle.skill.rules.summon", "{diceEffectLabel}：{triggerLabel} 召唤 {countLabel} 个 {enemyLabel}", ("diceEffectLabel", diceEffectLabel), ("triggerLabel", triggerLabel), ("countLabel", countLabel), ("enemyLabel", enemyLabel)));
        }
    }

    private static string GetSummonEnemyLabel(EnemySkill_SO.SummonEnemyEffectEntry effect, int effectIndex)
    {
        if (effect.enemyData != null && !string.IsNullOrWhiteSpace(effect.enemyData.LocalizedName))
            return effect.enemyData.LocalizedName;
        if (!string.IsNullOrWhiteSpace(effect.effectName))
            return effect.effectName;
        return GameText.Format("battle.skill.rules.summon_target", "召唤目标 {value}", ("value", effectIndex + 1));
    }

    private void AppendRangeModifierLines(List<string> effectLines, string diceEffectLabel, EnemySkill_SO.DiceEffectSettings effectSettings)
    {
        if (effectSettings.rangeModifiers == null)
            return;

        for (int effectIndex = 0; effectIndex < effectSettings.rangeModifiers.Length; effectIndex++)
        {
            EnemySkill_SO.ConditionalDiceRangeModifierEntry effect = effectSettings.rangeModifiers[effectIndex];
            if (effect.minPowerBonus == 0 && effect.maxPowerBonus == 0)
                continue;

            string conditionPrefix = effect.useCondition
                ? GameText.Format("battle.skill.rules.append_effect_lines.if", "若 {buildColoredToken} ，则 ", ("buildColoredToken", BuildColoredToken(GetRangeModifierConditionLabel(effect), targetHighlightColor)))
                : string.Empty;
            string scalingPrefix = effect.useCondition && effect.condition == EnemySkill_SO.PowerModifierCondition.TargetStatusStacksPerThreshold
                ? GameText.Get("battle.skill.rules.each_time_the_condition_is_met", "每达成一次条件，")
                : string.Empty;
            effectLines.Add($"{diceEffectLabel}：{conditionPrefix}{scalingPrefix}{BuildRangeModifierDeltaLabel(effect)}");
        }
    }

    private static string BuildSkillButtonLabel(EnemySkill_SO skill, bool isSelected, string unavailableReason)
    {
        if (skill == null)
            return isSelected ? GameText.Get("battle.skill.rules.equipped_skill", "已装备技能") : GameText.Get("battle.skill.rules.unnamed_skill", "未命名技能");

        string requirementSuffix = string.Empty;
        if (skill.RequiresSelfStatusToUse && skill.requiredSelfStatusData != null)
            requirementSuffix = $" [{skill.requiredSelfStatusData.LocalizedName} {Mathf.Max(1, skill.requiredSelfStatusStacks)}]";

        string equippedSuffix = isSelected ? GameText.Get("battle.skill.rules.build_skill_button_label.equipped", "  [已装备]") : string.Empty;
        string disabledSuffix = string.IsNullOrWhiteSpace(unavailableReason) ? string.Empty : $"  [{unavailableReason}]";
        return GameText.Format("battle.skill.rules.energy_cost", "{localizedName}{requirementSuffix}{equippedSuffix}\n能耗 {max}{disabledSuffix}", ("localizedName", skill.LocalizedName), ("requirementSuffix", requirementSuffix), ("equippedSuffix", equippedSuffix), ("max", Mathf.Max(0, skill.energyCost)), ("disabledSuffix", disabledSuffix));
    }

    private void ResetDetailScrollToTop()
    {
        if (detailScrollRect == null)
            return;

        Canvas.ForceUpdateCanvases();
        detailScrollRect.verticalNormalizedPosition = 1f;
        LayoutRebuilder.ForceRebuildLayoutImmediate(detailContent);
        Canvas.ForceUpdateCanvases();
        detailScrollRect.verticalNormalizedPosition = 1f;
    }

    private static string BuildColoredToken(string content, Color color)
    {
        return $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{content}</color>";
    }

    private string BuildDiceDisplayLabel(EnemySkill_SO skill, int diceIndex)
    {
        return $"{GetDicePrefixLabel(skill, diceIndex)}";
    }

    private string BuildDiceEffectLabel(EnemySkill_SO skill, int diceIndex)
    {
        return GameText.Format("battle.skill.die_effect", "{die}词条", ("die", GetDicePrefixLabel(skill, diceIndex)));
    }

    private static string GetDicePrefixLabel(EnemySkill_SO skill, int diceIndex)
    {
        bool isDefense = skill != null && skill.GetDiceSkillCategory(diceIndex) == EnemySkill_SO.SkillCategory.Defense;
        return isDefense
            ? GameText.Format("battle.skill.defense_die", "第 {index} 防御骰", ("index", diceIndex + 1))
            : GameText.Format("battle.skill.attack_die", "第 {index} 攻击骰", ("index", diceIndex + 1));
    }

    private Color GetDicePowerDisplayColor(EnemySkill_SO skill, int diceIndex)
    {
        return skill != null && skill.GetDiceSkillCategory(diceIndex) == EnemySkill_SO.SkillCategory.Defense
            ? defenseDicePowerColor
            : attackDicePowerColor;
    }

    private string BuildDiceRangeLabel(
        EnemySkill_SO skill,
        int diceIndex,
        EnemySkill_SO.DicePowerRange baseRange,
        EnemySkill_SO.DicePowerRange displayedRange)
    {
        Color dicePowerColor = GetDicePowerDisplayColor(skill, diceIndex);
        string displayedMinLabel = BuildDicePowerValueLabel(baseRange.minPower, displayedRange.minPower, dicePowerColor);
        string displayedMaxLabel = BuildDicePowerValueLabel(baseRange.maxPower, displayedRange.maxPower, dicePowerColor);
        return $"{displayedMinLabel} ~ {displayedMaxLabel}";
    }

    private string BuildDicePowerValueLabel(int basePower, int displayedPower, Color dicePowerColor)
    {
        string displayedPowerLabel = BuildColoredToken(displayedPower.ToString(), dicePowerColor);
        if (basePower == displayedPower)
            return displayedPowerLabel;

        string obsoletePowerLabel = BuildColoredToken($"<s>{basePower}</s>", obsoleteDicePowerColor);
        return $"{obsoletePowerLabel}{displayedPowerLabel}";
    }

    private static string FormatSignedInt(int value)
    {
        return value > 0 ? $"+{value}" : value.ToString();
    }

    private static Color GetPassiveNameColor(EnemyData_SO.PassiveDisplayColorType colorType)
    {
        switch (colorType)
        {
            case EnemyData_SO.PassiveDisplayColorType.Red:
                return new Color(0.95f, 0.32f, 0.28f, 1f);
            case EnemyData_SO.PassiveDisplayColorType.Orange:
                return new Color(1f, 0.62f, 0.2f, 1f);
            default:
                return new Color(1f, 0.86f, 0.3f, 1f);
        }
    }

    private static string GetDamageTargetLabel(EnemySkill_SO.DamageTargetMode targetMode, int randomTargetCount, bool attackerIsAlly)
    {
        switch (targetMode)
        {
            case EnemySkill_SO.DamageTargetMode.Self:
                return GameText.Get("battle.skill.rules.self", "自己");
            case EnemySkill_SO.DamageTargetMode.CurrentTarget:
                return GameText.Get("battle.skill.rules.current_target", "当前目标");
            case EnemySkill_SO.DamageTargetMode.RandomOpponents:
                return GameText.Format("battle.skill.rules.random_targets", "随机 {max} 名目标", ("max", Mathf.Max(1, randomTargetCount)));
            case EnemySkill_SO.DamageTargetMode.AllOpponents:
                return GameText.Get("battle.skill.rules.all_targets", "所有目标");
            case EnemySkill_SO.DamageTargetMode.AllOpponentsExceptCurrent:
                return GameText.Get("battle.skill.rules.other_targets", "其它目标");
            case EnemySkill_SO.DamageTargetMode.AllFriendlies:
                return GameText.Get("battle.skill.rules.all_allies", "全体友方");
            case EnemySkill_SO.DamageTargetMode.AllFriendliesExceptSelf:
                return GameText.Get("battle.skill.rules.other_allies", "除自身以外的友方");
            case EnemySkill_SO.DamageTargetMode.LowestHpFriendly:
                return GameText.Get("battle.skill.rules.ally_with_the_lowest_hp", "血量最低的友方");
            case EnemySkill_SO.DamageTargetMode.LowestHpOpponent:
                return GameText.Get("battle.skill.rules.target_with_the_lowest_hp", "血量最低的目标");
            default:
                return GameText.Get("battle.skill.rules.target", "目标");
        }
    }

    private static string GetStatusLabel(EnemySkill_SO.StatusEffectEntry effect)
    {
        if (effect.statusData != null)
            return effect.statusData.LocalizedName;

        switch (effect.statusType)
        {
            case EnemySkill_SO.DiceStatusType.Burn:
                return GameText.Get("battle.skill.rules.burn", "燃烧");
            case EnemySkill_SO.DiceStatusType.Electro:
                return GameText.Get("battle.skill.rules.shock", "电流");
            case EnemySkill_SO.DiceStatusType.Darkness:
                return GameText.Get("battle.skill.rules.darkness", "黑暗");
            case EnemySkill_SO.DiceStatusType.Mark:
                return GameText.Get("battle.skill.rules.mark", "标记");
            default:
                return GameText.Get("battle.skill.rules.unknown_status", "未配置状态");
        }
    }

    private static string GetStatusLabel(EnemySkill_SO.StatusBurstDamageEffectEntry effect)
    {
        if (effect.statusData != null)
            return effect.statusData.LocalizedName;

        switch (effect.statusType)
        {
            case EnemySkill_SO.DiceStatusType.Burn:
                return GameText.Get("battle.skill.rules.burn", "燃烧");
            case EnemySkill_SO.DiceStatusType.Electro:
                return GameText.Get("battle.skill.rules.shock", "电流");
            case EnemySkill_SO.DiceStatusType.Darkness:
                return GameText.Get("battle.skill.rules.darkness", "黑暗");
            case EnemySkill_SO.DiceStatusType.Mark:
                return GameText.Get("battle.skill.rules.mark", "标记");
            default:
                return GameText.Get("battle.skill.rules.unknown_status", "未配置状态");
        }
    }

    private static string GetStatusLabel(EnemySkill_SO.HealingEffectEntry effect)
    {
        if (effect.statusData != null)
            return effect.statusData.LocalizedName;

        switch (effect.statusType)
        {
            case EnemySkill_SO.DiceStatusType.Burn:
                return GameText.Get("battle.skill.rules.burn", "燃烧");
            case EnemySkill_SO.DiceStatusType.Electro:
                return GameText.Get("battle.skill.rules.shock", "电流");
            case EnemySkill_SO.DiceStatusType.Darkness:
                return GameText.Get("battle.skill.rules.darkness", "黑暗");
            case EnemySkill_SO.DiceStatusType.Mark:
                return GameText.Get("battle.skill.rules.mark", "标记");
            default:
                return GameText.Get("battle.skill.rules.unknown_status", "未配置状态");
        }
    }

    private static string GetStatusLabel(EnemySkill_SO.BuiltInEffectEntry effect)
    {
        if (effect.conditionStatusData != null)
            return effect.conditionStatusData.LocalizedName;

        switch (effect.conditionStatusType)
        {
            case EnemySkill_SO.DiceStatusType.Burn:
                return GameText.Get("battle.skill.rules.burn", "燃烧");
            case EnemySkill_SO.DiceStatusType.Electro:
                return GameText.Get("battle.skill.rules.shock", "电流");
            case EnemySkill_SO.DiceStatusType.Darkness:
                return GameText.Get("battle.skill.rules.darkness", "黑暗");
            case EnemySkill_SO.DiceStatusType.Mark:
                return GameText.Get("battle.skill.rules.mark", "标记");
            default:
                return GameText.Get("battle.skill.rules.unknown_status", "未配置状态");
        }
    }

    private static string GetBuiltInPrimaryStatusLabel(EnemySkill_SO.BuiltInEffectEntry effect)
    {
        if (effect.statusData != null)
            return effect.statusData.LocalizedName;

        switch (effect.statusType)
        {
            case EnemySkill_SO.DiceStatusType.Burn:
                return GameText.Get("battle.skill.rules.burn", "燃烧");
            case EnemySkill_SO.DiceStatusType.Electro:
                return GameText.Get("battle.skill.rules.shock", "电流");
            case EnemySkill_SO.DiceStatusType.Darkness:
                return GameText.Get("battle.skill.rules.darkness", "黑暗");
            case EnemySkill_SO.DiceStatusType.Mark:
                return GameText.Get("battle.skill.rules.mark", "标记");
            default:
                return GameText.Get("battle.skill.rules.unknown_status", "未配置状态");
        }
    }

    private static string GetStatusLabel(EnemySkill_SO.StatusStackOperationEffectEntry effect)
    {
        if (effect.statusData != null)
            return effect.statusData.LocalizedName;

        switch (effect.statusType)
        {
            case EnemySkill_SO.DiceStatusType.Burn:
                return GameText.Get("battle.skill.rules.burn", "燃烧");
            case EnemySkill_SO.DiceStatusType.Electro:
                return GameText.Get("battle.skill.rules.shock", "电流");
            case EnemySkill_SO.DiceStatusType.Darkness:
                return GameText.Get("battle.skill.rules.darkness", "黑暗");
            case EnemySkill_SO.DiceStatusType.Mark:
                return GameText.Get("battle.skill.rules.mark", "标记");
            default:
                return GameText.Get("battle.skill.rules.unknown_status", "未配置状态");
        }
    }

    private static string BuildTargetPrioritySummary(EnemySkill_SO skill)
    {
        if (skill == null || skill.targetPriorityMode == EnemySkill_SO.TargetPriorityMode.None)
            return string.Empty;

        if (skill.targetPriorityMode == EnemySkill_SO.TargetPriorityMode.PreferTargetsWithStatusAtLeast)
        {
            string statusLabel = GetTargetPriorityStatusLabel(skill);
            int stacks = Mathf.Max(1, skill.targetPriorityRequiredStacks);
            return GameText.Format("battle.skill.rules.target_priority_targets_with_stacks_of", "\n目标偏好：优先攻击具有 {stacks} 层 {statusLabel} 的目标", ("stacks", stacks), ("statusLabel", statusLabel));
        }

        if (skill.targetPriorityMode == EnemySkill_SO.TargetPriorityMode.PreferLowestSpeed)
            return GameText.Get("battle.skill.rules.target_priority_lowest_speed", "\n目标偏好：优先攻击速度最低的目标");

        if (skill.targetPriorityMode == EnemySkill_SO.TargetPriorityMode.PreferLowestHP)
            return GameText.Get("battle.skill.rules.target_priority_lowest_hp", "\n目标偏好：优先攻击血量最低的目标");

        if (skill.targetPriorityMode == EnemySkill_SO.TargetPriorityMode.PreferTargetsWithNegativeStatusTypesAtLeast)
            return GameText.Format("battle.skill.rules.target_priority_targets_with_at_least_different_debuffs", "\n目标偏好：优先攻击拥有至少 {max} 种负面状态的目标", ("max", Mathf.Max(1, skill.targetPriorityRequiredStacks)));

        if (skill.targetPriorityMode == EnemySkill_SO.TargetPriorityMode.PreferTargetsWithLowestStatusStacks)
            return GameText.Format("battle.skill.rules.target_priority_fewest_stacks_of", "\n目标偏好：优先攻击{getTargetPriorityStatusLabel}层数最低的目标", ("getTargetPriorityStatusLabel", GetTargetPriorityStatusLabel(skill)));

        if (skill.targetPriorityMode == EnemySkill_SO.TargetPriorityMode.PreferTargetsWithHighestStatusStacks)
            return GameText.Format("battle.skill.rules.target_priority_most_stacks_of", "\n目标偏好：优先攻击{getTargetPriorityStatusLabel}层数最高的目标", ("getTargetPriorityStatusLabel", GetTargetPriorityStatusLabel(skill)));

        return string.Empty;
    }

    private static string GetTargetPriorityStatusLabel(EnemySkill_SO skill)
    {
        if (skill == null)
            return GameText.Get("battle.skill.rules.unknown_status", "未配置状态");

        if (skill.targetPriorityStatusData != null)
            return skill.targetPriorityStatusData.LocalizedName;

        switch (skill.targetPriorityStatusType)
        {
            case EnemySkill_SO.DiceStatusType.Burn:
                return GameText.Get("battle.skill.rules.burn", "燃烧");
            case EnemySkill_SO.DiceStatusType.Electro:
                return GameText.Get("battle.skill.rules.shock", "电流");
            case EnemySkill_SO.DiceStatusType.Darkness:
                return GameText.Get("battle.skill.rules.darkness", "黑暗");
            case EnemySkill_SO.DiceStatusType.Mark:
                return GameText.Get("battle.skill.rules.mark", "标记");
            default:
                return GameText.Get("battle.skill.rules.unknown_status", "未配置状态");
        }
    }

    private static string GetHealingConditionLabel(EnemySkill_SO.HealingEffectEntry effect)
    {
        switch (effect.condition)
        {
            case EnemySkill_SO.PowerModifierCondition.SelfSpeedHigherThanTarget:
                return GameText.Format("battle.skill.rules.the_user_s_speed_is_at_least_higher", "自身速度 高出 目标单位速度 {max} 点（含）", ("max", Mathf.Max(1, effect.requiredConditionSpeedDifference)));
            case EnemySkill_SO.PowerModifierCondition.SelfSpeedLowerThanTarget:
                return GameText.Format("battle.skill.rules.the_user_s_speed_is_at_least_lower", "自身速度 低于 目标单位速度 {max} 点（含）", ("max", Mathf.Max(1, effect.requiredConditionSpeedDifference)));
            case EnemySkill_SO.PowerModifierCondition.SelfSpeedEqualToTarget:
                return GameText.Get("battle.skill.rules.the_user_s_speed_equals_the_target_s", "自身速度 等于 目标单位速度");
            case EnemySkill_SO.PowerModifierCondition.SelfSpeedHigherThanValue:
                return GameText.Format("battle.skill.rules.the_user_s_speed_is_at_least", "自身速度 高于 {requiredConditionSpeedValue}（含）", ("requiredConditionSpeedValue", effect.requiredConditionSpeedValue));
            case EnemySkill_SO.PowerModifierCondition.SelfSpeedLowerThanValue:
                return GameText.Format("battle.skill.rules.the_user_s_speed_is_at_most", "自身速度 低于 {requiredConditionSpeedValue}（含）", ("requiredConditionSpeedValue", effect.requiredConditionSpeedValue));
            case EnemySkill_SO.PowerModifierCondition.SelfHasStatus:
                return GameText.Format("battle.skill.rules.the_user_has", "自身拥有 {getHealingConditionStatusLabel}", ("getHealingConditionStatusLabel", GetHealingConditionStatusLabel(effect)));
            case EnemySkill_SO.PowerModifierCondition.SelfStatusStacksGreaterThan:
                return GameText.Format("battle.skill.rules.the_user_has_more_than_stacks_of", "自身 {getHealingConditionStatusLabel} 层数 高于 {max}", ("getHealingConditionStatusLabel", GetHealingConditionStatusLabel(effect)), ("max", Mathf.Max(0, effect.requiredConditionStatusStacks)));
            case EnemySkill_SO.PowerModifierCondition.SelfStatusStacksGreaterOrEqual:
                return GameText.Format("battle.skill.rules.the_user_has_at_least_stacks_of", "自身 {getHealingConditionStatusLabel} 层数 至少为 {max}", ("getHealingConditionStatusLabel", GetHealingConditionStatusLabel(effect)), ("max", Mathf.Max(1, effect.requiredConditionStatusStacks)));
            case EnemySkill_SO.PowerModifierCondition.SelfStatusStacksLessThan:
                return GameText.Format("battle.skill.rules.the_user_has_fewer_than_stacks_of", "自身 {getHealingConditionStatusLabel} 层数 低于 {max}", ("getHealingConditionStatusLabel", GetHealingConditionStatusLabel(effect)), ("max", Mathf.Max(1, effect.requiredConditionStatusStacks)));
            case EnemySkill_SO.PowerModifierCondition.SelfStatusStacksLessOrEqual:
                return GameText.Format("battle.skill.rules.the_user_has_at_most_stacks_of", "自身 {getHealingConditionStatusLabel} 层数 至多为 {max}", ("getHealingConditionStatusLabel", GetHealingConditionStatusLabel(effect)), ("max", Mathf.Max(0, effect.requiredConditionStatusStacks)));
            case EnemySkill_SO.PowerModifierCondition.TargetHasStatus:
                return GameText.Format("battle.skill.rules.the_target_has", "目标拥有 {getHealingConditionStatusLabel}", ("getHealingConditionStatusLabel", GetHealingConditionStatusLabel(effect)));
            case EnemySkill_SO.PowerModifierCondition.TargetStatusStacksAtLeast:
                return GameText.Format("battle.skill.rules.the_target_has_at_least_stacks_of", "目标拥有至少 {max} 层 {getHealingConditionStatusLabel}", ("max", Mathf.Max(1, effect.requiredConditionStatusStacks)), ("getHealingConditionStatusLabel", GetHealingConditionStatusLabel(effect)));
            case EnemySkill_SO.PowerModifierCondition.TargetStatusStacksLessOrEqual:
                return GameText.Format("battle.skill.rules.the_target_has_at_most_stacks_of", "目标 {getHealingConditionStatusLabel} 层数 至多为 {max}", ("getHealingConditionStatusLabel", GetHealingConditionStatusLabel(effect)), ("max", Mathf.Max(0, effect.requiredConditionStatusStacks)));
            case EnemySkill_SO.PowerModifierCondition.TargetStatusStacksPerThreshold:
                return GameText.Format("battle.skill.rules.for_every_stacks_of_on_the_target", "目标每拥有 {max} 层 {getHealingConditionStatusLabel}", ("max", Mathf.Max(1, effect.requiredConditionStatusStacks)), ("getHealingConditionStatusLabel", GetHealingConditionStatusLabel(effect)));
            case EnemySkill_SO.PowerModifierCondition.TargetNegativeStatusTypesAtLeast:
                return GameText.Format("battle.skill.rules.the_target_has_at_least_different_debuffs", "目标拥有至少 {max} 种负面状态", ("max", Mathf.Max(1, effect.requiredConditionStatusStacks)));
            case EnemySkill_SO.PowerModifierCondition.AnyOpponentNegativeStatusTypesAtLeast:
                return GameText.Format("battle.skill.rules.any_enemy_has_at_least_different_debuffs", "任意敌方目标拥有至少 {max} 种负面状态", ("max", Mathf.Max(1, effect.requiredConditionStatusStacks)));
            case EnemySkill_SO.PowerModifierCondition.SelfHpRatioBelowOrEqual:
                return GameText.Format("battle.skill.rules.the_user_has_hp_or_less", "自身生命值不高于 {clamp}%", ("clamp", Mathf.Clamp(effect.requiredConditionStatusStacks, 0, 100)));
            default:
                return GameText.Get("battle.skill.rules.no_condition_set", "未配置条件");
        }
    }

    private string BuildStatusEffectConditionPrefix(EnemySkill_SO.StatusEffectEntry effect)
    {
        if (!effect.useCondition)
            return string.Empty;

        return GameText.Format("battle.skill.rules.append_effect_lines.if", "若 {buildColoredToken} ，则 ", ("buildColoredToken", BuildColoredToken(GetStatusEffectConditionLabel(effect), targetHighlightColor)));
    }

    private static string GetStatusEffectConditionLabel(EnemySkill_SO.StatusEffectEntry effect)
    {
        switch (effect.condition)
        {
            case EnemySkill_SO.PowerModifierCondition.SelfSpeedHigherThanTarget:
                return GameText.Format("battle.skill.rules.the_user_s_speed_is_at_least_higher", "自身速度 高出 目标单位速度 {max} 点（含）", ("max", Mathf.Max(1, effect.requiredConditionSpeedDifference)));
            case EnemySkill_SO.PowerModifierCondition.SelfSpeedLowerThanTarget:
                return GameText.Format("battle.skill.rules.the_user_s_speed_is_at_least_lower", "自身速度 低于 目标单位速度 {max} 点（含）", ("max", Mathf.Max(1, effect.requiredConditionSpeedDifference)));
            case EnemySkill_SO.PowerModifierCondition.SelfSpeedEqualToTarget:
                return GameText.Get("battle.skill.rules.the_user_s_speed_equals_the_target_s", "自身速度 等于 目标单位速度");
            case EnemySkill_SO.PowerModifierCondition.SelfSpeedHigherThanValue:
                return GameText.Format("battle.skill.rules.the_user_s_speed_is_at_least", "自身速度 高于 {requiredConditionSpeedValue}（含）", ("requiredConditionSpeedValue", effect.requiredConditionSpeedValue));
            case EnemySkill_SO.PowerModifierCondition.SelfSpeedLowerThanValue:
                return GameText.Format("battle.skill.rules.the_user_s_speed_is_at_most", "自身速度 低于 {requiredConditionSpeedValue}（含）", ("requiredConditionSpeedValue", effect.requiredConditionSpeedValue));
            case EnemySkill_SO.PowerModifierCondition.SelfHasStatus:
                return GameText.Format("battle.skill.rules.get_status_effect_condition_label.the_user_has", "自身拥有 {getStatusEffectConditionStatusLabel}", ("getStatusEffectConditionStatusLabel", GetStatusEffectConditionStatusLabel(effect)));
            case EnemySkill_SO.PowerModifierCondition.SelfStatusStacksGreaterThan:
                return GameText.Format("battle.skill.rules.get_status_effect_condition_label.the_user_has_more_than_stacks_of", "自身 {getStatusEffectConditionStatusLabel} 层数 高于 {max}", ("getStatusEffectConditionStatusLabel", GetStatusEffectConditionStatusLabel(effect)), ("max", Mathf.Max(0, effect.requiredConditionStatusStacks)));
            case EnemySkill_SO.PowerModifierCondition.SelfStatusStacksGreaterOrEqual:
                return GameText.Format("battle.skill.rules.get_status_effect_condition_label.the_user_has_at_least_stacks_of", "自身 {getStatusEffectConditionStatusLabel} 层数 至少为 {max}", ("getStatusEffectConditionStatusLabel", GetStatusEffectConditionStatusLabel(effect)), ("max", Mathf.Max(1, effect.requiredConditionStatusStacks)));
            case EnemySkill_SO.PowerModifierCondition.SelfStatusStacksLessThan:
                return GameText.Format("battle.skill.rules.get_status_effect_condition_label.the_user_has_fewer_than_stacks_of", "自身 {getStatusEffectConditionStatusLabel} 层数 低于 {max}", ("getStatusEffectConditionStatusLabel", GetStatusEffectConditionStatusLabel(effect)), ("max", Mathf.Max(1, effect.requiredConditionStatusStacks)));
            case EnemySkill_SO.PowerModifierCondition.SelfStatusStacksLessOrEqual:
                return GameText.Format("battle.skill.rules.get_status_effect_condition_label.the_user_has_at_most_stacks_of", "自身 {getStatusEffectConditionStatusLabel} 层数 至多为 {max}", ("getStatusEffectConditionStatusLabel", GetStatusEffectConditionStatusLabel(effect)), ("max", Mathf.Max(0, effect.requiredConditionStatusStacks)));
            case EnemySkill_SO.PowerModifierCondition.TargetHasStatus:
                return GameText.Format("battle.skill.rules.get_status_effect_condition_label.the_target_has", "目标拥有 {getStatusEffectConditionStatusLabel}", ("getStatusEffectConditionStatusLabel", GetStatusEffectConditionStatusLabel(effect)));
            case EnemySkill_SO.PowerModifierCondition.TargetStatusStacksAtLeast:
                return GameText.Format("battle.skill.rules.get_status_effect_condition_label.the_target_has_at_least_stacks_of", "目标拥有至少 {max} 层 {getStatusEffectConditionStatusLabel}", ("max", Mathf.Max(1, effect.requiredConditionStatusStacks)), ("getStatusEffectConditionStatusLabel", GetStatusEffectConditionStatusLabel(effect)));
            case EnemySkill_SO.PowerModifierCondition.TargetStatusStacksLessOrEqual:
                return GameText.Format("battle.skill.rules.get_status_effect_condition_label.the_target_has_at_most_stacks_of", "目标 {getStatusEffectConditionStatusLabel} 层数 至多为 {max}", ("getStatusEffectConditionStatusLabel", GetStatusEffectConditionStatusLabel(effect)), ("max", Mathf.Max(0, effect.requiredConditionStatusStacks)));
            case EnemySkill_SO.PowerModifierCondition.TargetStatusStacksPerThreshold:
                return GameText.Format("battle.skill.rules.get_status_effect_condition_label.for_every_stacks_of_on_the_target", "目标每拥有 {max} 层 {getStatusEffectConditionStatusLabel}", ("max", Mathf.Max(1, effect.requiredConditionStatusStacks)), ("getStatusEffectConditionStatusLabel", GetStatusEffectConditionStatusLabel(effect)));
            case EnemySkill_SO.PowerModifierCondition.TargetSkillIsSpecific:
                return GameText.Format("battle.skill.rules.clashing_against", "与技能 {getStatusEffectConditionSkillLabel} 拼点", ("getStatusEffectConditionSkillLabel", GetStatusEffectConditionSkillLabel(effect)));
            case EnemySkill_SO.PowerModifierCondition.TargetNegativeStatusTypesAtLeast:
                return GameText.Format("battle.skill.rules.the_target_has_at_least_different_debuffs", "目标拥有至少 {max} 种负面状态", ("max", Mathf.Max(1, effect.requiredConditionStatusStacks)));
            case EnemySkill_SO.PowerModifierCondition.AnyOpponentNegativeStatusTypesAtLeast:
                return GameText.Format("battle.skill.rules.any_enemy_has_at_least_different_debuffs", "任意敌方目标拥有至少 {max} 种负面状态", ("max", Mathf.Max(1, effect.requiredConditionStatusStacks)));
            case EnemySkill_SO.PowerModifierCondition.SelfHpRatioBelowOrEqual:
                return GameText.Format("battle.skill.rules.the_user_has_hp_or_less", "自身生命值不高于 {clamp}%", ("clamp", Mathf.Clamp(effect.requiredConditionStatusStacks, 0, 100)));
            default:
                return GameText.Get("battle.skill.rules.no_condition_set", "未配置条件");
        }
    }

    private static string GetStatusEffectConditionStatusLabel(EnemySkill_SO.StatusEffectEntry effect)
    {
        if (effect.conditionStatusData != null)
            return effect.conditionStatusData.LocalizedName;

        switch (effect.conditionStatusType)
        {
            case EnemySkill_SO.DiceStatusType.Burn:
                return GameText.Get("battle.skill.rules.burn", "燃烧");
            case EnemySkill_SO.DiceStatusType.Electro:
                return GameText.Get("battle.skill.rules.shock", "电流");
            case EnemySkill_SO.DiceStatusType.Darkness:
                return GameText.Get("battle.skill.rules.darkness", "黑暗");
            case EnemySkill_SO.DiceStatusType.Mark:
                return GameText.Get("battle.skill.rules.mark", "标记");
            default:
                return GameText.Get("battle.skill.rules.unknown_status", "未配置状态");
        }
    }

    private static string GetStatusEffectConditionSkillLabel(EnemySkill_SO.StatusEffectEntry effect)
    {
        return effect.conditionTargetSkillData != null && !string.IsNullOrWhiteSpace(effect.conditionTargetSkillData.LocalizedName)
            ? effect.conditionTargetSkillData.LocalizedName
            : GameText.Get("battle.skill.unconfigured", "未配置技能");
    }

    private static string GetBuiltInConditionLabel(EnemySkill_SO.BuiltInEffectEntry effect)
    {
        string statusLabel = GetStatusLabel(effect);
        int requiredStacks = Mathf.Max(1, effect.requiredStatusStacks);
        switch (effect.conditionScope)
        {
            case EnemySkill_SO.BuiltInConditionScope.CurrentTarget:
                return GameText.Format("battle.skill.rules.get_built_in_condition_label.the_target_has_at_least_stacks_of", "目标拥有至少 {requiredStacks} 层 {statusLabel}", ("requiredStacks", requiredStacks), ("statusLabel", statusLabel));
            case EnemySkill_SO.BuiltInConditionScope.AnyOpponent:
                return GameText.Format("battle.skill.rules.any_target_has_at_least_stacks_of", "任意目标拥有至少 {requiredStacks} 层 {statusLabel}", ("requiredStacks", requiredStacks), ("statusLabel", statusLabel));
            case EnemySkill_SO.BuiltInConditionScope.AnyFriendly:
                return GameText.Format("battle.skill.rules.any_ally_has_at_least_stacks_of", "任意友方拥有至少 {requiredStacks} 层 {statusLabel}", ("requiredStacks", requiredStacks), ("statusLabel", statusLabel));
            case EnemySkill_SO.BuiltInConditionScope.Self:
                return GameText.Format("battle.skill.rules.get_built_in_condition_label.the_user_has_at_least_stacks_of", "自身拥有至少 {requiredStacks} 层 {statusLabel}", ("requiredStacks", requiredStacks), ("statusLabel", statusLabel));
            default:
                return GameText.Format("battle.skill.rules.get_built_in_condition_label.the_target_has_at_least_stacks_of", "目标拥有至少 {requiredStacks} 层 {statusLabel}", ("requiredStacks", requiredStacks), ("statusLabel", statusLabel));
        }
    }

    private string BuildBuiltInConditionPrefix(EnemySkill_SO.BuiltInEffectEntry effect)
    {
        List<string> conditions = new List<string>();
        if (effect.useStatusCondition)
            conditions.Add(BuildColoredToken(GetBuiltInConditionLabel(effect), targetHighlightColor));
        if (effect.useCondition)
            conditions.Add(BuildColoredToken(GetBuiltInGeneralConditionLabel(effect), targetHighlightColor));

        return conditions.Count > 0
            ? GameText.Format("battle.skill.rules.build_built_in_condition_prefix.if", "若 {join} ，则 ", ("join", string.Join(GameText.Get("battle.skill.rules.build_built_in_condition_prefix.and", " 且 "), conditions)))
            : string.Empty;
    }

    private static string BuildBuiltInTimingPrefix(EnemySkill_SO.BuiltInEffectEntry effect)
    {
        if (effect.applyTiming != EnemySkill_SO.StatusApplyTiming.NextRoundStart)
            return string.Empty;

        int repeatCount = Mathf.Max(1, effect.applyRepeatCount);
        int delayRounds = Mathf.Max(1, effect.applyAfterRoundStarts);
        return repeatCount > 1
            ? GameText.Format("battle.skill.rules.at_the_start_of_the_next_rounds", "于 接下来 {repeatCount} 个回合开始时 ", ("repeatCount", repeatCount))
            : GameText.Format("battle.skill.rules.at_the_start_of_the_round_in_rounds", "于 下 {delayRounds} 个回合开始时 ", ("delayRounds", delayRounds));
    }

    private static string GetBuiltInGeneralConditionLabel(EnemySkill_SO.BuiltInEffectEntry effect)
    {
        switch (effect.condition)
        {
            case EnemySkill_SO.PowerModifierCondition.SelfSpeedHigherThanTarget:
                return GameText.Format("battle.skill.rules.the_user_s_speed_is_at_least_higher", "自身速度 高出 目标单位速度 {max} 点（含）", ("max", Mathf.Max(1, effect.requiredConditionSpeedDifference)));
            case EnemySkill_SO.PowerModifierCondition.SelfSpeedLowerThanTarget:
                return GameText.Format("battle.skill.rules.the_user_s_speed_is_at_least_lower", "自身速度 低于 目标单位速度 {max} 点（含）", ("max", Mathf.Max(1, effect.requiredConditionSpeedDifference)));
            case EnemySkill_SO.PowerModifierCondition.SelfSpeedEqualToTarget:
                return GameText.Get("battle.skill.rules.the_user_s_speed_equals_the_target_s", "自身速度 等于 目标单位速度");
            case EnemySkill_SO.PowerModifierCondition.SelfSpeedHigherThanValue:
                return GameText.Format("battle.skill.rules.the_user_s_speed_is_at_least", "自身速度 高于 {requiredConditionSpeedValue}（含）", ("requiredConditionSpeedValue", effect.requiredConditionSpeedValue));
            case EnemySkill_SO.PowerModifierCondition.SelfSpeedLowerThanValue:
                return GameText.Format("battle.skill.rules.the_user_s_speed_is_at_most", "自身速度 低于 {requiredConditionSpeedValue}（含）", ("requiredConditionSpeedValue", effect.requiredConditionSpeedValue));
            case EnemySkill_SO.PowerModifierCondition.SelfHasStatus:
                return GameText.Format("battle.skill.rules.get_built_in_general_condition_label.the_user_has", "自身拥有 {getBuiltInGeneralConditionStatusLabel}", ("getBuiltInGeneralConditionStatusLabel", GetBuiltInGeneralConditionStatusLabel(effect)));
            case EnemySkill_SO.PowerModifierCondition.SelfStatusStacksGreaterThan:
                return GameText.Format("battle.skill.rules.get_built_in_general_condition_label.the_user_has_more_than_stacks_of", "自身 {getBuiltInGeneralConditionStatusLabel} 层数 高于 {max}", ("getBuiltInGeneralConditionStatusLabel", GetBuiltInGeneralConditionStatusLabel(effect)), ("max", Mathf.Max(0, effect.requiredConditionStatusStacks)));
            case EnemySkill_SO.PowerModifierCondition.SelfStatusStacksGreaterOrEqual:
                return GameText.Format("battle.skill.rules.get_built_in_general_condition_label.the_user_has_at_least_stacks_of", "自身 {getBuiltInGeneralConditionStatusLabel} 层数 至少为 {max}", ("getBuiltInGeneralConditionStatusLabel", GetBuiltInGeneralConditionStatusLabel(effect)), ("max", Mathf.Max(1, effect.requiredConditionStatusStacks)));
            case EnemySkill_SO.PowerModifierCondition.SelfStatusStacksLessThan:
                return GameText.Format("battle.skill.rules.get_built_in_general_condition_label.the_user_has_fewer_than_stacks_of", "自身 {getBuiltInGeneralConditionStatusLabel} 层数 低于 {max}", ("getBuiltInGeneralConditionStatusLabel", GetBuiltInGeneralConditionStatusLabel(effect)), ("max", Mathf.Max(1, effect.requiredConditionStatusStacks)));
            case EnemySkill_SO.PowerModifierCondition.SelfStatusStacksLessOrEqual:
                return GameText.Format("battle.skill.rules.get_built_in_general_condition_label.the_user_has_at_most_stacks_of", "自身 {getBuiltInGeneralConditionStatusLabel} 层数 至多为 {max}", ("getBuiltInGeneralConditionStatusLabel", GetBuiltInGeneralConditionStatusLabel(effect)), ("max", Mathf.Max(0, effect.requiredConditionStatusStacks)));
            case EnemySkill_SO.PowerModifierCondition.TargetHasStatus:
                return GameText.Format("battle.skill.rules.get_built_in_general_condition_label.the_target_has", "目标拥有 {getBuiltInGeneralConditionStatusLabel}", ("getBuiltInGeneralConditionStatusLabel", GetBuiltInGeneralConditionStatusLabel(effect)));
            case EnemySkill_SO.PowerModifierCondition.TargetStatusStacksAtLeast:
                return GameText.Format("battle.skill.rules.get_built_in_general_condition_label.the_target_has_at_least_stacks_of", "目标拥有至少 {max} 层 {getBuiltInGeneralConditionStatusLabel}", ("max", Mathf.Max(1, effect.requiredConditionStatusStacks)), ("getBuiltInGeneralConditionStatusLabel", GetBuiltInGeneralConditionStatusLabel(effect)));
            case EnemySkill_SO.PowerModifierCondition.TargetStatusStacksPerThreshold:
                return GameText.Format("battle.skill.rules.get_built_in_general_condition_label.for_every_stacks_of_on_the_target", "目标每拥有 {max} 层 {getBuiltInGeneralConditionStatusLabel}", ("max", Mathf.Max(1, effect.requiredConditionStatusStacks)), ("getBuiltInGeneralConditionStatusLabel", GetBuiltInGeneralConditionStatusLabel(effect)));
            case EnemySkill_SO.PowerModifierCondition.TargetSkillIsSpecific:
                return GameText.Format("battle.skill.rules.get_built_in_general_condition_label.clashing_against", "与技能 {getBuiltInGeneralConditionSkillLabel} 拼点", ("getBuiltInGeneralConditionSkillLabel", GetBuiltInGeneralConditionSkillLabel(effect)));
            case EnemySkill_SO.PowerModifierCondition.TargetNegativeStatusTypesAtLeast:
                return GameText.Format("battle.skill.rules.the_target_has_at_least_different_debuffs", "目标拥有至少 {max} 种负面状态", ("max", Mathf.Max(1, effect.requiredConditionStatusStacks)));
            case EnemySkill_SO.PowerModifierCondition.AnyOpponentNegativeStatusTypesAtLeast:
                return GameText.Format("battle.skill.rules.any_enemy_has_at_least_different_debuffs", "任意敌方目标拥有至少 {max} 种负面状态", ("max", Mathf.Max(1, effect.requiredConditionStatusStacks)));
            case EnemySkill_SO.PowerModifierCondition.SelfHpRatioBelowOrEqual:
                return GameText.Format("battle.skill.rules.the_user_has_hp_or_less", "自身生命值不高于 {clamp}%", ("clamp", Mathf.Clamp(effect.requiredConditionStatusStacks, 0, 100)));
            default:
                return GameText.Get("battle.skill.rules.no_condition_set", "未配置条件");
        }
    }

    private static string GetBuiltInGeneralConditionStatusLabel(EnemySkill_SO.BuiltInEffectEntry effect)
    {
        if (effect.targetStatusData != null)
            return effect.targetStatusData.LocalizedName;

        switch (effect.targetStatusType)
        {
            case EnemySkill_SO.DiceStatusType.Burn:
                return GameText.Get("battle.skill.rules.burn", "燃烧");
            case EnemySkill_SO.DiceStatusType.Electro:
                return GameText.Get("battle.skill.rules.shock", "电流");
            case EnemySkill_SO.DiceStatusType.Darkness:
                return GameText.Get("battle.skill.rules.darkness", "黑暗");
            case EnemySkill_SO.DiceStatusType.Mark:
                return GameText.Get("battle.skill.rules.mark", "标记");
            default:
                return GameText.Get("battle.skill.rules.unknown_status", "未配置状态");
        }
    }

    private static string GetBuiltInGeneralConditionSkillLabel(EnemySkill_SO.BuiltInEffectEntry effect)
    {
        return effect.targetSkillData != null && !string.IsNullOrWhiteSpace(effect.targetSkillData.LocalizedName)
            ? effect.targetSkillData.LocalizedName
            : GameText.Get("battle.skill.unconfigured", "未配置技能");
    }

    private static string GetBuiltInActionTargetLabel(EnemySkill_SO.BuiltInEffectEntry effect, bool attackerIsAlly)
    {
        switch (effect.targetMode)
        {
            case EnemySkill_SO.DamageTargetMode.Self:
                return GameText.Get("battle.skill.rules.self", "自己");
            case EnemySkill_SO.DamageTargetMode.CurrentTarget:
                return GameText.Get("battle.skill.rules.target", "目标");
            case EnemySkill_SO.DamageTargetMode.RandomOpponents:
                return GameText.Format("battle.skill.rules.random_targets", "随机 {max} 名目标", ("max", Mathf.Max(1, effect.randomTargetCount)));
            case EnemySkill_SO.DamageTargetMode.AllOpponents:
                return GameText.Get("battle.skill.rules.all_targets", "所有目标");
            case EnemySkill_SO.DamageTargetMode.AllOpponentsExceptCurrent:
                return GameText.Get("battle.skill.rules.other_targets", "其它目标");
            case EnemySkill_SO.DamageTargetMode.AllFriendlies:
                return GameText.Get("battle.skill.rules.all_allies", "全体友方");
            case EnemySkill_SO.DamageTargetMode.AllFriendliesExceptSelf:
                return GameText.Get("battle.skill.rules.other_allies", "除自身以外的友方");
            case EnemySkill_SO.DamageTargetMode.LowestHpFriendly:
                return GameText.Get("battle.skill.rules.ally_with_the_lowest_hp", "血量最低的友方");
            case EnemySkill_SO.DamageTargetMode.LowestHpOpponent:
                return GameText.Get("battle.skill.rules.target_with_the_lowest_hp", "血量最低的目标");
            default:
                return attackerIsAlly ? GameText.Get("battle.skill.rules.enemy_target", "敌方目标") : GameText.Get("battle.skill.rules.target", "目标");
        }
    }

    private static string GetHealingConditionStatusLabel(EnemySkill_SO.HealingEffectEntry effect)
    {
        if (effect.conditionStatusData != null)
            return effect.conditionStatusData.LocalizedName;

        if (effect.statusData != null)
            return effect.statusData.LocalizedName;

        switch (effect.conditionStatusType)
        {
            case EnemySkill_SO.DiceStatusType.Burn:
                return GameText.Get("battle.skill.rules.burn", "燃烧");
            case EnemySkill_SO.DiceStatusType.Electro:
                return GameText.Get("battle.skill.rules.shock", "电流");
            case EnemySkill_SO.DiceStatusType.Darkness:
                return GameText.Get("battle.skill.rules.darkness", "黑暗");
            case EnemySkill_SO.DiceStatusType.Mark:
                return GameText.Get("battle.skill.rules.mark", "标记");
            default:
                return GameText.Get("battle.skill.rules.unknown_status", "未配置状态");
        }
    }

    private static string GetPowerModifierConditionLabel(EnemySkill_SO.ConditionalPowerModifierEntry effect)
    {
        switch (effect.condition)
        {
            case EnemySkill_SO.PowerModifierCondition.SelfSpeedHigherThanTarget:
                return GameText.Format("battle.skill.rules.the_user_s_speed_is_at_least_higher", "自身速度 高出 目标单位速度 {max} 点（含）", ("max", Mathf.Max(1, effect.requiredSpeedDifference)));
            case EnemySkill_SO.PowerModifierCondition.SelfSpeedLowerThanTarget:
                return GameText.Format("battle.skill.rules.the_user_s_speed_is_at_least_lower", "自身速度 低于 目标单位速度 {max} 点（含）", ("max", Mathf.Max(1, effect.requiredSpeedDifference)));
            case EnemySkill_SO.PowerModifierCondition.SelfSpeedEqualToTarget:
                return GameText.Get("battle.skill.rules.the_user_s_speed_equals_the_target_s", "自身速度 等于 目标单位速度");
            case EnemySkill_SO.PowerModifierCondition.SelfSpeedHigherThanValue:
                return GameText.Format("battle.skill.rules.get_power_modifier_condition_label.the_user_s_speed_is_at_least", "自身速度 高于 {requiredSpeedValue}（含）", ("requiredSpeedValue", effect.requiredSpeedValue));
            case EnemySkill_SO.PowerModifierCondition.SelfSpeedLowerThanValue:
                return GameText.Format("battle.skill.rules.get_power_modifier_condition_label.the_user_s_speed_is_at_most", "自身速度 低于 {requiredSpeedValue}（含）", ("requiredSpeedValue", effect.requiredSpeedValue));
            case EnemySkill_SO.PowerModifierCondition.SelfHasStatus:
                return GameText.Format("battle.skill.rules.get_power_modifier_condition_label.the_user_has", "自身拥有 {getPowerModifierStatusLabel}", ("getPowerModifierStatusLabel", GetPowerModifierStatusLabel(effect)));
            case EnemySkill_SO.PowerModifierCondition.SelfStatusStacksGreaterThan:
                return GameText.Format("battle.skill.rules.get_power_modifier_condition_label.the_user_has_more_than_stacks_of", "自身 {getPowerModifierStatusLabel} 层数 高于 {max}", ("getPowerModifierStatusLabel", GetPowerModifierStatusLabel(effect)), ("max", Mathf.Max(0, effect.requiredStatusStacks)));
            case EnemySkill_SO.PowerModifierCondition.SelfStatusStacksGreaterOrEqual:
                return GameText.Format("battle.skill.rules.get_power_modifier_condition_label.the_user_has_at_least_stacks_of", "自身 {getPowerModifierStatusLabel} 层数 至少为 {max}", ("getPowerModifierStatusLabel", GetPowerModifierStatusLabel(effect)), ("max", Mathf.Max(1, effect.requiredStatusStacks)));
            case EnemySkill_SO.PowerModifierCondition.SelfStatusStacksLessThan:
                return GameText.Format("battle.skill.rules.get_power_modifier_condition_label.the_user_has_fewer_than_stacks_of", "自身 {getPowerModifierStatusLabel} 层数 低于 {max}", ("getPowerModifierStatusLabel", GetPowerModifierStatusLabel(effect)), ("max", Mathf.Max(1, effect.requiredStatusStacks)));
            case EnemySkill_SO.PowerModifierCondition.SelfStatusStacksLessOrEqual:
                return GameText.Format("battle.skill.rules.get_power_modifier_condition_label.the_user_has_at_most_stacks_of", "自身 {getPowerModifierStatusLabel} 层数 至多为 {max}", ("getPowerModifierStatusLabel", GetPowerModifierStatusLabel(effect)), ("max", Mathf.Max(0, effect.requiredStatusStacks)));
            case EnemySkill_SO.PowerModifierCondition.TargetHasStatus:
                return GameText.Format("battle.skill.rules.get_power_modifier_condition_label.the_target_has", "目标拥有 {getPowerModifierStatusLabel}", ("getPowerModifierStatusLabel", GetPowerModifierStatusLabel(effect)));
            case EnemySkill_SO.PowerModifierCondition.TargetStatusStacksAtLeast:
                return GameText.Format("battle.skill.rules.get_power_modifier_condition_label.the_target_has_at_least_stacks_of", "目标拥有至少 {max} 层 {getPowerModifierStatusLabel}", ("max", Mathf.Max(1, effect.requiredStatusStacks)), ("getPowerModifierStatusLabel", GetPowerModifierStatusLabel(effect)));
            case EnemySkill_SO.PowerModifierCondition.TargetStatusStacksLessOrEqual:
                return GameText.Format("battle.skill.rules.get_power_modifier_condition_label.the_target_has_at_most_stacks_of", "目标 {getPowerModifierStatusLabel} 层数 至多为 {max}", ("getPowerModifierStatusLabel", GetPowerModifierStatusLabel(effect)), ("max", Mathf.Max(0, effect.requiredStatusStacks)));
            case EnemySkill_SO.PowerModifierCondition.TargetStatusStacksPerThreshold:
                return GameText.Format("battle.skill.rules.get_power_modifier_condition_label.for_every_stacks_of_on_the_target", "目标每拥有 {max} 层 {getPowerModifierStatusLabel}", ("max", Mathf.Max(1, effect.requiredStatusStacks)), ("getPowerModifierStatusLabel", GetPowerModifierStatusLabel(effect)));
            case EnemySkill_SO.PowerModifierCondition.TargetSkillIsSpecific:
                return GameText.Format("battle.skill.rules.get_power_modifier_condition_label.clashing_against", "与技能 {getPowerModifierTargetSkillLabel} 拼点", ("getPowerModifierTargetSkillLabel", GetPowerModifierTargetSkillLabel(effect)));
            case EnemySkill_SO.PowerModifierCondition.TargetNegativeStatusTypesAtLeast:
                return GameText.Format("battle.skill.rules.the_target_has_at_least_different_debuffs", "目标拥有至少 {max} 种负面状态", ("max", Mathf.Max(1, effect.requiredStatusStacks)));
            case EnemySkill_SO.PowerModifierCondition.AnyOpponentNegativeStatusTypesAtLeast:
                return GameText.Format("battle.skill.rules.any_enemy_has_at_least_different_debuffs", "任意敌方目标拥有至少 {max} 种负面状态", ("max", Mathf.Max(1, effect.requiredStatusStacks)));
            case EnemySkill_SO.PowerModifierCondition.SelfHpRatioBelowOrEqual:
                return GameText.Format("battle.skill.rules.the_user_has_hp_or_less", "自身生命值不高于 {clamp}%", ("clamp", Mathf.Clamp(effect.requiredStatusStacks, 0, 100)));
            default:
                return GameText.Get("battle.skill.rules.no_condition_set", "未配置条件");
        }
    }

    private static string GetPowerModifierStatusLabel(EnemySkill_SO.ConditionalPowerModifierEntry effect)
    {
        if (effect.targetStatusData != null)
            return effect.targetStatusData.LocalizedName;

        switch (effect.targetStatusType)
        {
            case EnemySkill_SO.DiceStatusType.Burn:
                return GameText.Get("battle.skill.rules.burn", "燃烧");
            case EnemySkill_SO.DiceStatusType.Electro:
                return GameText.Get("battle.skill.rules.shock", "电流");
            case EnemySkill_SO.DiceStatusType.Darkness:
                return GameText.Get("battle.skill.rules.darkness", "黑暗");
            case EnemySkill_SO.DiceStatusType.Mark:
                return GameText.Get("battle.skill.rules.mark", "标记");
            default:
                return GameText.Get("battle.skill.rules.unknown_status", "未配置状态");
        }
    }

    private static string GetPowerModifierTargetSkillLabel(EnemySkill_SO.ConditionalPowerModifierEntry effect)
    {
        return effect.targetSkillData != null && !string.IsNullOrWhiteSpace(effect.targetSkillData.LocalizedName)
            ? effect.targetSkillData.LocalizedName
            : GameText.Get("battle.skill.unconfigured", "未配置技能");
    }

    private static string GetRangeModifierConditionLabel(EnemySkill_SO.ConditionalDiceRangeModifierEntry effect)
    {
        switch (effect.condition)
        {
            case EnemySkill_SO.PowerModifierCondition.SelfSpeedHigherThanTarget:
                return GameText.Format("battle.skill.rules.the_user_s_speed_is_at_least_higher", "自身速度 高出 目标单位速度 {max} 点（含）", ("max", Mathf.Max(1, effect.requiredSpeedDifference)));
            case EnemySkill_SO.PowerModifierCondition.SelfSpeedLowerThanTarget:
                return GameText.Format("battle.skill.rules.the_user_s_speed_is_at_least_lower", "自身速度 低于 目标单位速度 {max} 点（含）", ("max", Mathf.Max(1, effect.requiredSpeedDifference)));
            case EnemySkill_SO.PowerModifierCondition.SelfSpeedEqualToTarget:
                return GameText.Get("battle.skill.rules.the_user_s_speed_equals_the_target_s", "自身速度 等于 目标单位速度");
            case EnemySkill_SO.PowerModifierCondition.SelfSpeedHigherThanValue:
                return GameText.Format("battle.skill.rules.get_power_modifier_condition_label.the_user_s_speed_is_at_least", "自身速度 高于 {requiredSpeedValue}（含）", ("requiredSpeedValue", effect.requiredSpeedValue));
            case EnemySkill_SO.PowerModifierCondition.SelfSpeedLowerThanValue:
                return GameText.Format("battle.skill.rules.get_power_modifier_condition_label.the_user_s_speed_is_at_most", "自身速度 低于 {requiredSpeedValue}（含）", ("requiredSpeedValue", effect.requiredSpeedValue));
            case EnemySkill_SO.PowerModifierCondition.SelfHasStatus:
                return GameText.Format("battle.skill.rules.get_range_modifier_condition_label.the_user_has", "自身拥有 {getRangeModifierStatusLabel}", ("getRangeModifierStatusLabel", GetRangeModifierStatusLabel(effect)));
            case EnemySkill_SO.PowerModifierCondition.SelfStatusStacksGreaterThan:
                return GameText.Format("battle.skill.rules.get_range_modifier_condition_label.the_user_has_more_than_stacks_of", "自身 {getRangeModifierStatusLabel} 层数 高于 {max}", ("getRangeModifierStatusLabel", GetRangeModifierStatusLabel(effect)), ("max", Mathf.Max(0, effect.requiredStatusStacks)));
            case EnemySkill_SO.PowerModifierCondition.SelfStatusStacksGreaterOrEqual:
                return GameText.Format("battle.skill.rules.get_range_modifier_condition_label.the_user_has_at_least_stacks_of", "自身 {getRangeModifierStatusLabel} 层数 至少为 {max}", ("getRangeModifierStatusLabel", GetRangeModifierStatusLabel(effect)), ("max", Mathf.Max(1, effect.requiredStatusStacks)));
            case EnemySkill_SO.PowerModifierCondition.SelfStatusStacksLessThan:
                return GameText.Format("battle.skill.rules.get_range_modifier_condition_label.the_user_has_fewer_than_stacks_of", "自身 {getRangeModifierStatusLabel} 层数 低于 {max}", ("getRangeModifierStatusLabel", GetRangeModifierStatusLabel(effect)), ("max", Mathf.Max(1, effect.requiredStatusStacks)));
            case EnemySkill_SO.PowerModifierCondition.SelfStatusStacksLessOrEqual:
                return GameText.Format("battle.skill.rules.get_range_modifier_condition_label.the_user_has_at_most_stacks_of", "自身 {getRangeModifierStatusLabel} 层数 至多为 {max}", ("getRangeModifierStatusLabel", GetRangeModifierStatusLabel(effect)), ("max", Mathf.Max(0, effect.requiredStatusStacks)));
            case EnemySkill_SO.PowerModifierCondition.TargetHasStatus:
                return GameText.Format("battle.skill.rules.get_range_modifier_condition_label.the_target_has", "目标拥有 {getRangeModifierStatusLabel}", ("getRangeModifierStatusLabel", GetRangeModifierStatusLabel(effect)));
            case EnemySkill_SO.PowerModifierCondition.TargetStatusStacksAtLeast:
                return GameText.Format("battle.skill.rules.get_range_modifier_condition_label.the_target_has_at_least_stacks_of", "目标拥有至少 {max} 层 {getRangeModifierStatusLabel}", ("max", Mathf.Max(1, effect.requiredStatusStacks)), ("getRangeModifierStatusLabel", GetRangeModifierStatusLabel(effect)));
            case EnemySkill_SO.PowerModifierCondition.TargetStatusStacksLessOrEqual:
                return GameText.Format("battle.skill.rules.get_range_modifier_condition_label.the_target_has_at_most_stacks_of", "目标 {getRangeModifierStatusLabel} 层数 至多为 {max}", ("getRangeModifierStatusLabel", GetRangeModifierStatusLabel(effect)), ("max", Mathf.Max(0, effect.requiredStatusStacks)));
            case EnemySkill_SO.PowerModifierCondition.TargetStatusStacksPerThreshold:
                return GameText.Format("battle.skill.rules.get_range_modifier_condition_label.for_every_stacks_of_on_the_target", "目标每拥有 {max} 层 {getRangeModifierStatusLabel}", ("max", Mathf.Max(1, effect.requiredStatusStacks)), ("getRangeModifierStatusLabel", GetRangeModifierStatusLabel(effect)));
            case EnemySkill_SO.PowerModifierCondition.TargetSkillIsSpecific:
                return GameText.Format("battle.skill.rules.get_range_modifier_condition_label.clashing_against", "与技能 {getRangeModifierTargetSkillLabel} 拼点", ("getRangeModifierTargetSkillLabel", GetRangeModifierTargetSkillLabel(effect)));
            case EnemySkill_SO.PowerModifierCondition.TargetNegativeStatusTypesAtLeast:
                return GameText.Format("battle.skill.rules.the_target_has_at_least_different_debuffs", "目标拥有至少 {max} 种负面状态", ("max", Mathf.Max(1, effect.requiredStatusStacks)));
            case EnemySkill_SO.PowerModifierCondition.AnyOpponentNegativeStatusTypesAtLeast:
                return GameText.Format("battle.skill.rules.any_enemy_has_at_least_different_debuffs", "任意敌方目标拥有至少 {max} 种负面状态", ("max", Mathf.Max(1, effect.requiredStatusStacks)));
            case EnemySkill_SO.PowerModifierCondition.SelfHpRatioBelowOrEqual:
                return GameText.Format("battle.skill.rules.the_user_has_hp_or_less", "自身生命值不高于 {clamp}%", ("clamp", Mathf.Clamp(effect.requiredStatusStacks, 0, 100)));
            default:
                return GameText.Get("battle.skill.rules.no_condition_set", "未配置条件");
        }
    }

    private string BuildRangeModifierDeltaLabel(EnemySkill_SO.ConditionalDiceRangeModifierEntry effect)
    {
        List<string> segments = new List<string>();
        if (effect.minPowerBonus != 0)
            segments.Add(GameText.Format("battle.skill.rules.min_power", "威力范围最小值 {buildColoredToken}{buildRangeModifierCapSuffix}", ("buildColoredToken", BuildColoredToken(FormatSignedInt(effect.minPowerBonus), valueHighlightColor)), ("buildRangeModifierCapSuffix", BuildRangeModifierCapSuffix(effect.minPowerBonusCap))));
        if (effect.maxPowerBonus != 0)
            segments.Add(GameText.Format("battle.skill.rules.max_power", "威力范围最大值 {buildColoredToken}{buildRangeModifierCapSuffix}", ("buildColoredToken", BuildColoredToken(FormatSignedInt(effect.maxPowerBonus), valueHighlightColor)), ("buildRangeModifierCapSuffix", BuildRangeModifierCapSuffix(effect.maxPowerBonusCap))));
        return string.Join("，", segments);
    }

    private string BuildRangeModifierCapSuffix(int cap)
    {
        return cap > 0
            ? GameText.Format("battle.skill.rules.max", "，最多 {buildColoredToken}", ("buildColoredToken", BuildColoredToken(FormatSignedInt(cap), valueHighlightColor)))
            : string.Empty;
    }

    private static string GetRangeModifierStatusLabel(EnemySkill_SO.ConditionalDiceRangeModifierEntry effect)
    {
        if (effect.targetStatusData != null)
            return effect.targetStatusData.LocalizedName;

        switch (effect.targetStatusType)
        {
            case EnemySkill_SO.DiceStatusType.Burn:
                return GameText.Get("battle.skill.rules.burn", "燃烧");
            case EnemySkill_SO.DiceStatusType.Electro:
                return GameText.Get("battle.skill.rules.shock", "电流");
            case EnemySkill_SO.DiceStatusType.Darkness:
                return GameText.Get("battle.skill.rules.darkness", "黑暗");
            case EnemySkill_SO.DiceStatusType.Mark:
                return GameText.Get("battle.skill.rules.mark", "标记");
            default:
                return GameText.Get("battle.skill.rules.unknown_status", "未配置状态");
        }
    }

    private static string GetRangeModifierTargetSkillLabel(EnemySkill_SO.ConditionalDiceRangeModifierEntry effect)
    {
        return effect.targetSkillData != null && !string.IsNullOrWhiteSpace(effect.targetSkillData.LocalizedName)
            ? effect.targetSkillData.LocalizedName
            : GameText.Get("battle.skill.unconfigured", "未配置技能");
    }

    private static string GetPowerModifierTriggerLabel(EnemySkill_SO.DiceEffectTrigger trigger)
    {
        switch (trigger)
        {
            case EnemySkill_SO.DiceEffectTrigger.OnHit:
                return GameText.Get("battle.skill.rules.on_hit", "[ 命中时 ]");
            case EnemySkill_SO.DiceEffectTrigger.OnMiss:
                return GameText.Get("battle.skill.rules.on_miss", "[ 未击中时 ]");
            case EnemySkill_SO.DiceEffectTrigger.OnClash:
                return GameText.Get("battle.skill.rules.on_clash", "[ 拼点时 ]");
            case EnemySkill_SO.DiceEffectTrigger.OnSkillUse:
                return GameText.Get("battle.skill.rules.on_skill_use", "[ 使用该技能时 ]");
            case EnemySkill_SO.DiceEffectTrigger.OnClashWin:
                return GameText.Get("battle.skill.rules.on_clash_win", "[ 拼点胜利时 ]");
            case EnemySkill_SO.DiceEffectTrigger.OnSingleAttack:
                return GameText.Get("battle.skill.rules.on_one_sided_attack", "[ 单方面攻击时 ]");
            case EnemySkill_SO.DiceEffectTrigger.OnAllDiceHit:
                return GameText.Get("battle.skill.rules.if_all_hits_land", "[ 全部攻击段命中时 ]");
            default:
                return $"[ {trigger} ]";
        }
    }

    private static string GetGeneralEffectTriggerLabel(EnemySkill_SO.DiceEffectTrigger trigger)
    {
        switch (trigger)
        {
            case EnemySkill_SO.DiceEffectTrigger.OnHit:
                return GameText.Get("battle.skill.rules.on_hit", "[ 命中时 ]");
            case EnemySkill_SO.DiceEffectTrigger.OnMiss:
                return GameText.Get("battle.skill.rules.on_miss", "[ 未击中时 ]");
            case EnemySkill_SO.DiceEffectTrigger.OnSkillUse:
                return GameText.Get("battle.skill.rules.on_use", "[ 使用时 ]");
            case EnemySkill_SO.DiceEffectTrigger.OnClash:
                return GameText.Get("battle.skill.rules.on_clash", "[ 拼点时 ]");
            case EnemySkill_SO.DiceEffectTrigger.OnClashWin:
                return GameText.Get("battle.skill.rules.on_clash_win", "[ 拼点胜利时 ]");
            case EnemySkill_SO.DiceEffectTrigger.OnSingleAttack:
                return GameText.Get("battle.skill.rules.on_one_sided_attack", "[ 单方面攻击时 ]");
            case EnemySkill_SO.DiceEffectTrigger.OnAllDiceHit:
                return GameText.Get("battle.skill.rules.if_all_hits_land", "[ 全部攻击段命中时 ]");
            default:
                return $"[ {trigger} ]";
        }
    }
}
