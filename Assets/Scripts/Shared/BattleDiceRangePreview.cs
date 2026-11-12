using System;
using System.Collections.Generic;

internal sealed class BattleDiceRangePreview
{
    public readonly BattleDiceRangeBreakdown[] Dice;
    public readonly string TargetName;
    public readonly bool IsClash;
    public bool ResetsConsecutiveSkill { get; private set; }

    private BattleDiceRangePreview(int count, string targetName, bool isClash)
    {
        Dice = new BattleDiceRangeBreakdown[count];
        TargetName = targetName;
        IsClash = isClash;
    }

    public static BattleDiceRangePreview Build(EnemySkill_SO skill, BattleAllyController ally, BattleEnemyController enemy,
        SpeedButton sourceButton, HashSet<string> previewProcs = null, bool includeQueuedBonus = true, int? consecutiveSkillUses = null)
    {
        if (ally == null && enemy == null)
            return new BattleDiceRangePreview(0, null, false);

        bool isAlly = ally != null;
        sourceButton = sourceButton != null ? sourceButton : isAlly ? ally.speedButton : enemy?.speedButton;
        SpeedButton targetButton = sourceButton != null && !sourceButton.IsActionDisabled ? sourceButton.ConnectedTarget : null;
        BattleAllyController targetAlly = null;
        BattleEnemyController targetEnemy = null;
        if (targetButton != null)
        {
            if (isAlly)
                targetEnemy = targetButton.GetComponentInParent<BattleEnemyController>();
            else
                targetAlly = targetButton.GetComponentInParent<BattleAllyController>();
        }

        bool hasTarget = (targetEnemy != null && !targetEnemy.IsDead) || (targetAlly != null && !targetAlly.IsDead);
        bool isClash = hasTarget && !targetButton.IsActionDisabled && (isAlly
            ? sourceButton.IsContestedConnection
            : targetButton.IsContestedConnection && targetButton.ConnectedTarget == sourceButton);
        string targetName = !hasTarget ? null : isAlly
            ? targetEnemy.enemyData?.LocalizedName ?? targetEnemy.name
            : targetAlly.GetSettlementDisplayName();
        EnemySkill_SO targetSkill = !hasTarget ? null : isClash
            ? (isAlly ? targetEnemy.GetSelectedSkillForButton(targetButton) : targetAlly.GetSelectedSkillForButton(targetButton))
            : (isAlly ? targetEnemy.SelectedSkill : targetAlly.SelectedSkill);
        int clashDice = isClash ? targetSkill?.diceCount ?? 1 : 0;

        int selfSpeed = isAlly ? ally.Speed : enemy.Speed;
        int targetSpeed = !hasTarget ? 0 : isAlly ? targetEnemy.Speed : targetAlly.Speed;
        Func<BattleStatus_SO, int> selfByAsset = isAlly ? (Func<BattleStatus_SO, int>)ally.GetStatusStacks : enemy.GetStatusStacks;
        Func<string, int> selfById = isAlly ? (Func<string, int>)ally.GetStatusStacks : enemy.GetStatusStacks;
        Func<BattleStatus_SO, int> targetByAsset = !hasTarget ? null : isAlly
            ? (Func<BattleStatus_SO, int>)targetEnemy.GetStatusStacks : targetAlly.GetStatusStacks;
        Func<string, int> targetById = !hasTarget ? null : isAlly
            ? (Func<string, int>)targetEnemy.GetStatusStacks : targetAlly.GetStatusStacks;
        BattleDiceRollService.ForcedRollMode forced = isAlly
            ? ally.ResolveForcedRollMode(out BattleStatusRuntimeEntry forcedBy)
            : enemy.ResolveForcedRollMode(out forcedBy);

        var preview = new BattleDiceRangePreview(Math.Max(0, skill?.diceCount ?? 1), targetName, isClash);
        previewProcs ??= new HashSet<string>(StringComparer.Ordinal);
        BattleEncounterData encounter = BattleEncounterRuntime.ActiveEncounter;
        if (isAlly && !consecutiveSkillUses.HasValue)
        {
            BattleAllyController.ConsecutiveSkillUsage usage = ally.SkillUsage;
            usage.Register(skill);
            consecutiveSkillUses = usage.GetUses(skill);
        }
        for (int i = 0; i < preview.Dice.Length; i++)
        {
            bool clashDie = i < clashDice;
            bool willRoll = hasTarget && (clashDie || skill?.GetDiceSkillCategory(i) == EnemySkill_SO.SkillCategory.Attack);
            EnemySkill_SO.DicePowerRange baseRange = isAlly
                ? ally.ResolveSkillBaseDiceRange(skill, i)
                : BattleEnemyController.ResolveSkillBaseDiceRange(skill, i);
            var breakdown = new BattleDiceRangeBreakdown(baseRange, willRoll);
            preview.Dice[i] = breakdown;
            EnemySkill_SO.DicePowerRange range = isAlly
                ? ally.GetDisplayedDicePowerRange(skill, i, breakdown)
                : enemy.GetEffectiveDisplayedDicePowerRange(skill, i, breakdown, includeQueuedBonus);

            void RecordSkill(string name, int min, int max, bool clashOnly = false)
            {
                breakdown.Add(string.IsNullOrWhiteSpace(name) || GameText.LanguageCode == GameText.English ? skill.LocalizedName : name,
                    null, min, max, isSkill: true, clashOnly: clashOnly);
            }

            range = BattleDiceRangeModifierService.ApplyRangeModifiers(skill, i, range, selfSpeed, targetSpeed,
                selfByAsset, selfById, targetByAsset, targetById, targetSkill, hasTarget, (name, min, max) => RecordSkill(name, min, max));
            if (isAlly && i == 0 && includeQueuedBonus && ally.CurrentRoundRelicMinPowerBonus != 0)
            {
                Relic_SO relic = ally.CurrentRoundMinPowerRelic;
                int bonus = ally.CurrentRoundRelicMinPowerBonus;
                breakdown.Add(relic != null ? relic.LocalizedName : Local("回合骰值奖励", "Round dice bonus"), relic != null ? relic.icon : null, bonus, 0);
                range = BattleDiceRollService.ApplyRangeBonus(range, bonus, 0);
            }

            int relicPower = 0;
            if (encounter?.relicInventory?.Count > 0 && (isAlly || clashDie))
            {
                RelicExecutionContext context = RelicExecutionContext.CreatePlayerDicePrepared(null,
                    isAlly ? skill : targetSkill, i, clashDie, isAlly ? ally : targetAlly, isAlly ? targetEnemy : enemy);
                context.previewRoundRelicProcs = previewProcs;
                context.previewDiceBreakdown = breakdown;
                context.previewDiceForTarget = !isAlly;
                context.previewConsecutiveSkillUses = consecutiveSkillUses.GetValueOrDefault();
                RelicSystem.Process(encounter.relicInventory, context);
                preview.ResetsConsecutiveSkill |= isAlly && context.previewResetsConsecutiveSkill;
                range = isAlly
                    ? BattleDiceRollService.ApplyRangeBonus(range, context.additionalMinPowerBonus, context.additionalMaxPowerBonus)
                    : BattleDiceRollService.ApplyRangeBonus(range, context.additionalTargetMinPowerBonus, 0);
                relicPower = isAlly ? context.additionalPowerBonus : 0;
            }

            range = BattleDiceRollService.NormalizeRange(range);
            if (forced != BattleDiceRollService.ForcedRollMode.None)
            {
                int forcedPower = BattleDiceRollService.RollPower(range, forced);
                breakdown.AddStatus(forcedBy, forcedPower - range.minPower, forcedPower - range.maxPower);
                range.minPower = range.maxPower = forcedPower;
            }

            // Single-attack power evaluation does not pass an opposing skill to the combat resolver.
            EnemySkill_SO opposingSkill = clashDie ? targetSkill : null;
            int skillUsePower = BattlePowerModifierService.EvaluatePowerModifier(skill, i, EnemySkill_SO.DiceEffectTrigger.OnSkillUse,
                selfSpeed, targetSpeed, selfByAsset, selfById, targetByAsset, targetById, opposingSkill, hasTarget,
                (name, bonus) => RecordSkill(name, bonus, bonus));
            int actionPower = 0;
            if (willRoll)
            {
                EnemySkill_SO.DiceEffectTrigger trigger = clashDie
                    ? EnemySkill_SO.DiceEffectTrigger.OnClash : EnemySkill_SO.DiceEffectTrigger.OnSingleAttack;
                actionPower = BattlePowerModifierService.EvaluatePowerModifier(skill, i, trigger, selfSpeed, targetSpeed,
                    selfByAsset, selfById, targetByAsset, targetById, opposingSkill, hasTarget,
                    (name, bonus) => RecordSkill(name, bonus, bonus, clashDie));
            }
            if (!isAlly && clashDie)
            {
                int passivePower = enemy.GetSpeedClashPowerBonus(targetSpeed);
                if (passivePower != 0)
                {
                    string passive = enemy.enemyData?.Localize("passive.0.name", Local("抢风", "Wind Snatch"));
                    breakdown.Add(passive, null, passivePower, passivePower, isSkill: true, clashOnly: true);
                    actionPower += passivePower;
                }
            }

            breakdown.SetRollOutcome(range, relicPower + skillUsePower + actionPower);
        }
        return preview;
    }

    private static string Local(string chinese, string english) => GameText.LanguageCode == GameText.English ? english : chinese;
}
