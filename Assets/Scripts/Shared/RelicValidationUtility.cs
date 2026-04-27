using System;
using System.Collections.Generic;
using UnityEngine;

public enum RelicValidationSeverity
{
    Warning = 0,
    Error = 1
}

public class RelicValidationIssue
{
    public RelicValidationIssue(
        RelicValidationSeverity severity,
        UnityEngine.Object asset,
        string assetName,
        string fieldPath,
        string message)
    {
        Severity = severity;
        Asset = asset;
        AssetName = assetName;
        FieldPath = fieldPath;
        Message = message;
    }

    public RelicValidationSeverity Severity { get; private set; }
    public UnityEngine.Object Asset { get; private set; }
    public string AssetName { get; private set; }
    public string FieldPath { get; private set; }
    public string Message { get; private set; }

    public override string ToString()
    {
        string severityText = Severity == RelicValidationSeverity.Error ? "Error" : "Warning";
        return string.Format("[RelicValidation/{0}] {1}.{2}: {3}", severityText, AssetName, FieldPath, Message);
    }
}

public static class RelicValidationUtility
{
    public static List<RelicValidationIssue> ValidateRelic(Relic_SO relic)
    {
        List<RelicValidationIssue> issues = new List<RelicValidationIssue>();
        if (relic == null)
        {
            issues.Add(new RelicValidationIssue(
                RelicValidationSeverity.Error,
                null,
                "Missing Relic",
                "relic",
                "Relic asset reference is missing."));
            return issues;
        }

        List<Relic_SO.RelicBuiltInEffectEntry> effects = relic.builtInEffects;
        if ((effects == null || effects.Count == 0) && relic.customEffectHandler == null)
        {
            AddIssue(
                issues,
                RelicValidationSeverity.Warning,
                relic,
                "builtInEffects",
                "Relic has no built-in effects and no custom effect handler.");
            return issues;
        }

        if (effects == null)
            return issues;

        for (int i = 0; i < effects.Count; i++)
        {
            Relic_SO.RelicBuiltInEffectEntry effect = effects[i];
            string path = string.Format("builtInEffects[{0}]", i);
            if (effect == null)
            {
                AddIssue(
                    issues,
                    RelicValidationSeverity.Error,
                    relic,
                    path,
                    "Built-in effect entry is missing.");
                continue;
            }

            ValidateEffectEntry(relic, effect, path, issues);
        }

        return issues;
    }

    public static List<RelicValidationIssue> ValidateRelicTable(RelicTable_SO table)
    {
        List<RelicValidationIssue> issues = new List<RelicValidationIssue>();
        if (table == null)
        {
            issues.Add(new RelicValidationIssue(
                RelicValidationSeverity.Error,
                null,
                "Missing Relic Table",
                "table",
                "Relic table asset reference is missing."));
            return issues;
        }

        IReadOnlyList<Relic_SO> relics = table.Relics;
        if (!HasAnyRelic(relics))
        {
            AddIssue(
                issues,
                RelicValidationSeverity.Error,
                table,
                "relics",
                "Relic table has no usable relic entries.");
        }

        AddDuplicateRelicIssues(table, relics, issues);

        IReadOnlyList<RelicTable_SO.RarityWeightEntry> weights = table.RarityWeights;
        if (!HasPositiveRarityWeight(weights))
        {
            AddIssue(
                issues,
                RelicValidationSeverity.Error,
                table,
                "rarityWeights",
                "All rarity weights are zero, so rarity selection falls back to unweighted relic draws.");
        }

        return issues;
    }

    private static void ValidateEffectEntry(
        Relic_SO relic,
        Relic_SO.RelicBuiltInEffectEntry effect,
        string path,
        List<RelicValidationIssue> issues)
    {
        if (effect.effectType == Relic_SO.RelicBuiltInEffectType.None)
        {
            AddIssue(
                issues,
                RelicValidationSeverity.Error,
                relic,
                path + ".effectType",
                "Built-in effect type is None.");
        }
        else if (!RelicSystem.IsBuiltInEffectRegistered(effect.effectType))
        {
            AddIssue(
                issues,
                RelicValidationSeverity.Error,
                relic,
                path + ".effectType",
                "Built-in effect type has no registered handler and will never execute.");
        }

        if (effect.chance <= 0f)
        {
            AddIssue(
                issues,
                RelicValidationSeverity.Error,
                relic,
                path + ".chance",
                "Effect chance is zero, so this effect can never trigger.");
        }

        RelicBuiltInEffectMetadata metadata = null;
        if (effect.effectType != Relic_SO.RelicBuiltInEffectType.None &&
            RelicSystem.IsBuiltInEffectRegistered(effect.effectType) &&
            !RelicBuiltInEffectMetadataCatalog.TryGet(effect.effectType, out metadata))
        {
            AddIssue(
                issues,
                RelicValidationSeverity.Error,
                relic,
                path + ".effectType",
                "Built-in effect type has no validation metadata; update RelicBuiltInEffectMetadataCatalog.");
        }

        if (metadata != null && metadata.RequiresStatusData && effect.statusData == null)
        {
            AddIssue(
                issues,
                RelicValidationSeverity.Error,
                relic,
                path + ".statusData",
                "This effect needs a statusData asset to apply or transfer status.");
        }

        if (metadata != null && metadata.RequiresConditionStatusData && effect.conditionStatusData == null)
        {
            AddIssue(
                issues,
                RelicValidationSeverity.Error,
                relic,
                path + ".conditionStatusData",
                "This effect needs a conditionStatusData asset to check status stacks.");
        }

        if (metadata != null && !metadata.HasMeaningfulNumericValue(effect))
        {
            AddIssue(
                issues,
                RelicValidationSeverity.Warning,
                relic,
                path + "." + metadata.NumericFieldPath,
                "This effect has no meaningful numeric value and is likely to do nothing.");
        }
    }

    private static bool HasAnyRelic(IReadOnlyList<Relic_SO> relics)
    {
        if (relics == null)
            return false;

        for (int i = 0; i < relics.Count; i++)
        {
            if (relics[i] != null)
                return true;
        }

        return false;
    }

    private static bool HasPositiveRarityWeight(IReadOnlyList<RelicTable_SO.RarityWeightEntry> weights)
    {
        if (weights == null)
            return false;

        for (int i = 0; i < weights.Count; i++)
        {
            if (weights[i].weight > 0f)
                return true;
        }

        return false;
    }

    private static void AddDuplicateRelicIssues(
        RelicTable_SO table,
        IReadOnlyList<Relic_SO> relics,
        List<RelicValidationIssue> issues)
    {
        if (relics == null || relics.Count <= 1)
            return;

        HashSet<Relic_SO> seen = new HashSet<Relic_SO>();
        HashSet<Relic_SO> reported = new HashSet<Relic_SO>();
        for (int i = 0; i < relics.Count; i++)
        {
            Relic_SO relic = relics[i];
            if (relic == null)
                continue;

            if (seen.Add(relic))
                continue;

            if (!reported.Add(relic))
                continue;

            AddIssue(
                issues,
                RelicValidationSeverity.Warning,
                table,
                "relics",
                string.Format("Relic table contains duplicate relic '{0}'.", relic.DisplayName));
        }
    }

    private static void AddIssue(
        List<RelicValidationIssue> issues,
        RelicValidationSeverity severity,
        UnityEngine.Object asset,
        string fieldPath,
        string message)
    {
        string assetName = asset != null ? asset.name : "Missing Asset";
        issues.Add(new RelicValidationIssue(severity, asset, assetName, fieldPath, message));
    }
}
