using UnityEngine;

public partial class BattleSkillInfoOverlay
{
    public string DescribeItemDiceHeading(EnemySkill_SO skill, int diceIndex)
    {
        return GetDicePrefixLabel(skill, diceIndex);
    }

    /// <summary>Inventory cards use the battle formatter with configuration values and no battle modifiers.</summary>
    public string DescribeItemDiceRules(EnemySkill_SO skill, int diceIndex)
    {
        if (skill == null || diceIndex < 0 || diceIndex >= Mathf.Max(0, skill.diceCount)) return string.Empty;
        return string.Join("\n", BuildDiceRuleLines(skill, diceIndex, true, false));
    }

    public string DescribeItemSkillRequirements(EnemySkill_SO skill)
    {
        if (skill == null) return string.Empty;
        string target = BuildTargetPrioritySummary(skill).Trim();
        if (!skill.RequiresSelfStatusToUse || skill.requiredSelfStatusData == null) return target;
        string consume = skill.keepRequiredSelfStatusAfterUse
            ? GameText.Get("battle.skill.rules.stacks_kept_after_use", "，施放后不消耗")
            : GameText.Get("battle.skill.rules.stacks_spent_on_use", "，施放后消耗");
        string requirement = GameText.Format("battle.skill.rules.requires_stacks_of",
            "\n施放条件：自身至少拥有 {requiredStacks} 层 {localizedName}{currentSuffix}{consumeSuffix}",
            ("requiredStacks", Mathf.Max(1, skill.requiredSelfStatusStacks)),
            ("localizedName", skill.requiredSelfStatusData.LocalizedName), ("currentSuffix", string.Empty), ("consumeSuffix", consume));
        return string.IsNullOrWhiteSpace(target) ? requirement.Trim() : requirement.Trim() + "\n" + target;
    }
}
