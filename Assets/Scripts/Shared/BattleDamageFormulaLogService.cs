using System.Collections.Generic;
using System.Text;
using UnityEngine;

public readonly struct BattleDamageFormulaModifierSource
{
    public readonly int Amount;
    public readonly string SourceName;

    public BattleDamageFormulaModifierSource(int amount, string sourceName)
    {
        Amount = amount;
        SourceName = sourceName;
    }
}

public static class BattleDamageFormulaLogService
{
    public static string BuildAttackFormula(
        string label,
        string attackerName,
        string targetName,
        int baseDamage,
        IReadOnlyList<BattleDamageFormulaModifierSource> percentSources,
        IReadOnlyList<BattleDamageFormulaModifierSource> flatSources,
        int finalDamage)
    {
        int safeBaseDamage = Mathf.Max(0, baseDamage);
        int totalPercent = SumAmounts(percentSources);
        int totalFlat = SumAmounts(flatSources);
        float multipliedValue = safeBaseDamage * (1f + totalPercent / 100f);
        float finalRawValue = multipliedValue + totalFlat;

        StringBuilder builder = new StringBuilder(160);
        builder.Append("[DamageCalc] ");
        if (!string.IsNullOrWhiteSpace(label))
            builder.Append(label).Append("：");
        if (!string.IsNullOrWhiteSpace(attackerName) || !string.IsNullOrWhiteSpace(targetName))
            builder.Append(attackerName).Append(" -> ").Append(targetName).Append("，");

        builder.Append(safeBaseDamage).Append("（基础伤害）");
        if (totalPercent != 0)
        {
            builder.Append(" x (1");
            AppendSignedPercentTerms(builder, percentSources);
            builder.Append(")");
            AppendSourceGroup(builder, percentSources);
        }

        if (totalFlat != 0)
        {
            AppendSignedFlatTerms(builder, flatSources);
        }

        builder.Append(" = ").Append(FormatNumber(finalRawValue));
        builder.Append(" = ").Append(Mathf.Max(0, finalDamage));
        return builder.ToString();
    }

    public static string BuildStatusDamageFormula(
        string targetName,
        string statusName,
        int stacks,
        int damagePerStack,
        int finalDamage)
    {
        int safeStacks = Mathf.Max(0, stacks);
        int safeDamagePerStack = Mathf.Max(0, damagePerStack);
        int baseDamage = safeStacks * safeDamagePerStack;
        return $"[DamageCalc] 状态伤害：{targetName} 受到 {statusName}，{safeStacks} 层 x {safeDamagePerStack}（每层伤害） = {baseDamage} = {Mathf.Max(0, finalDamage)}";
    }

    private static int SumAmounts(IReadOnlyList<BattleDamageFormulaModifierSource> sources)
    {
        int total = 0;
        if (sources == null)
            return 0;

        for (int i = 0; i < sources.Count; i++)
            total += sources[i].Amount;

        return total;
    }

    private static void AppendSignedPercentTerms(StringBuilder builder, IReadOnlyList<BattleDamageFormulaModifierSource> sources)
    {
        if (sources == null)
            return;

        for (int i = 0; i < sources.Count; i++)
        {
            int amount = sources[i].Amount;
            if (amount == 0)
                continue;

            builder.Append(amount > 0 ? "+" : "-");
            builder.Append(Mathf.Abs(amount)).Append("%");
        }
    }

    private static void AppendSignedFlatTerms(StringBuilder builder, IReadOnlyList<BattleDamageFormulaModifierSource> sources)
    {
        if (sources == null)
            return;

        for (int i = 0; i < sources.Count; i++)
        {
            int amount = sources[i].Amount;
            if (amount == 0)
                continue;

            builder.Append(amount > 0 ? " + " : " - ");
            builder.Append(Mathf.Abs(amount));
            if (!string.IsNullOrWhiteSpace(sources[i].SourceName))
                builder.Append("（").Append(sources[i].SourceName).Append("）");
        }
    }

    private static void AppendSourceGroup(StringBuilder builder, IReadOnlyList<BattleDamageFormulaModifierSource> sources)
    {
        if (sources == null || sources.Count == 0)
            return;

        bool hasSource = false;
        for (int i = 0; i < sources.Count; i++)
        {
            if (!string.IsNullOrWhiteSpace(sources[i].SourceName))
            {
                hasSource = true;
                break;
            }
        }

        if (!hasSource)
            return;

        builder.Append("（");
        bool appended = false;
        for (int i = 0; i < sources.Count; i++)
        {
            string sourceName = sources[i].SourceName;
            if (string.IsNullOrWhiteSpace(sourceName))
                continue;

            if (appended)
                builder.Append("、");
            builder.Append(sourceName);
            appended = true;
        }

        builder.Append("）");
    }

    private static string FormatNumber(float value)
    {
        if (Mathf.Approximately(value, Mathf.Round(value)))
            return Mathf.RoundToInt(value).ToString();

        return value.ToString("0.##");
    }
}
