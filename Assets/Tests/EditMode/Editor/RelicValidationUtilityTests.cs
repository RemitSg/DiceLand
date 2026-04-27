using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class RelicValidationUtilityTests
{
    private readonly List<Object> createdObjects = new List<Object>();

    [TearDown]
    public void TearDown()
    {
        for (int i = createdObjects.Count - 1; i >= 0; i--)
        {
            if (createdObjects[i] != null)
                Object.DestroyImmediate(createdObjects[i]);
        }

        createdObjects.Clear();
    }

    [Test]
    public void ValidateRelic_ReportsMissingConditionStatusData()
    {
        Relic_SO relic = CreateRelic(
            Relic_SO.RelicBuiltInEffectType.ModifyPlayerDiceWhenSelfHasStatus,
            effect => effect.powerAmount = 1);

        List<RelicValidationIssue> issues = RelicValidationUtility.ValidateRelic(relic);

        AssertHasIssue(issues, RelicValidationSeverity.Error, "conditionStatusData");
    }

    [Test]
    public void ValidateRelic_ReportsMissingStatusData()
    {
        Relic_SO relic = CreateRelic(
            Relic_SO.RelicBuiltInEffectType.ApplyStatusToAllEnemiesOnSpecificRound,
            effect => effect.amount = 2);

        List<RelicValidationIssue> issues = RelicValidationUtility.ValidateRelic(relic);

        AssertHasIssue(issues, RelicValidationSeverity.Error, "statusData");
    }

    [Test]
    public void ValidateRelic_ReportsNoneEffectAndZeroChance()
    {
        Relic_SO relic = CreateRelic(
            Relic_SO.RelicBuiltInEffectType.None,
            effect => effect.chance = 0f);

        List<RelicValidationIssue> issues = RelicValidationUtility.ValidateRelic(relic);

        AssertHasIssue(issues, RelicValidationSeverity.Error, "effectType");
        AssertHasIssue(issues, RelicValidationSeverity.Error, "chance");
    }

    [Test]
    public void ValidateRelic_ReportsUnregisteredBuiltInEffect()
    {
        Relic_SO relic = CreateRelic((Relic_SO.RelicBuiltInEffectType)9999);

        List<RelicValidationIssue> issues = RelicValidationUtility.ValidateRelic(relic);

        AssertHasIssue(issues, RelicValidationSeverity.Error, "effectType");
    }

    [Test]
    public void ValidateRelic_ReportsIneffectiveNumericConfiguration()
    {
        Relic_SO relic = CreateRelic(
            Relic_SO.RelicBuiltInEffectType.ModifyPlayerClashDiceWhenFasterThanTarget,
            effect => effect.powerAmount = 0);

        List<RelicValidationIssue> issues = RelicValidationUtility.ValidateRelic(relic);

        AssertHasIssue(issues, RelicValidationSeverity.Warning, "powerAmount");
    }

    [Test]
    public void ValidateRelic_UsesMetadataNumericRequirementForNoAmountEffects()
    {
        Relic_SO relic = CreateRelic(
            Relic_SO.RelicBuiltInEffectType.TransferSpeedButtonsToLowestHpAllyOnPlayerDeath,
            effect => effect.amount = 0);

        List<RelicValidationIssue> issues = RelicValidationUtility.ValidateRelic(relic);

        AssertNoIssue(issues, RelicValidationSeverity.Warning, "amount");
    }

    [Test]
    public void ValidateRelic_UsesMetadataNumericFieldPathForPowerDeltaEffects()
    {
        Relic_SO relic = CreateRelic(
            Relic_SO.RelicBuiltInEffectType.ModifyPlayerDiceWhenSelfHasStatus,
            effect =>
            {
                effect.conditionStatusData = CreateStatus("metadata-condition");
                effect.powerAmount = 0;
                effect.minPowerAmount = 0;
                effect.maxPowerAmount = 0;
            });

        List<RelicValidationIssue> issues = RelicValidationUtility.ValidateRelic(relic);

        AssertHasIssue(issues, RelicValidationSeverity.Warning, "powerAmount/minPowerAmount/maxPowerAmount");
    }

    [Test]
    public void ValidateRelicTable_ReportsEmptyTableDuplicateRelicsAndZeroWeights()
    {
        RelicTable_SO emptyTable = CreateRelicTable();
        SetPrivateList(emptyTable, "relics", new List<Relic_SO>());
        SetPrivateList(emptyTable, "rarityWeights", CreateWeights(0f));

        Relic_SO duplicateRelic = CreateRelic(
            Relic_SO.RelicBuiltInEffectType.ChanceToIgnoreMoveCost);
        RelicTable_SO duplicateTable = CreateRelicTable();
        SetPrivateList(duplicateTable, "relics", new List<Relic_SO> { duplicateRelic, duplicateRelic });
        SetPrivateList(duplicateTable, "rarityWeights", CreateWeights(0f));

        List<RelicValidationIssue> emptyIssues = RelicValidationUtility.ValidateRelicTable(emptyTable);
        List<RelicValidationIssue> duplicateIssues = RelicValidationUtility.ValidateRelicTable(duplicateTable);

        AssertHasIssue(emptyIssues, RelicValidationSeverity.Error, "relics");
        AssertHasIssue(emptyIssues, RelicValidationSeverity.Error, "rarityWeights");
        AssertHasIssue(duplicateIssues, RelicValidationSeverity.Warning, "relics");
        AssertHasIssue(duplicateIssues, RelicValidationSeverity.Error, "rarityWeights");
    }

    private Relic_SO CreateRelic(
        Relic_SO.RelicBuiltInEffectType effectType,
        System.Action<Relic_SO.RelicBuiltInEffectEntry> configure = null)
    {
        Relic_SO relic = ScriptableObject.CreateInstance<Relic_SO>();
        relic.relicId = "validation-test";
        relic.relicName = "Validation Test";
        relic.builtInEffects = new List<Relic_SO.RelicBuiltInEffectEntry>
        {
            new Relic_SO.RelicBuiltInEffectEntry
            {
                trigger = Relic_SO.RelicTrigger.OnMoveCostAboutToSpend,
                effectType = effectType,
                chance = 1f,
                amount = 1
            }
        };
        configure?.Invoke(relic.builtInEffects[0]);
        createdObjects.Add(relic);
        return relic;
    }

    private RelicTable_SO CreateRelicTable()
    {
        RelicTable_SO table = ScriptableObject.CreateInstance<RelicTable_SO>();
        table.tableName = "Validation Table";
        createdObjects.Add(table);
        return table;
    }

    private BattleStatus_SO CreateStatus(string statusId)
    {
        BattleStatus_SO status = ScriptableObject.CreateInstance<BattleStatus_SO>();
        SetPrivateField(status, "statusId", statusId);
        SetPrivateField(status, "displayName", statusId);
        createdObjects.Add(status);
        return status;
    }

    private static void SetPrivateField<T>(Object target, string fieldName, T value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"Missing field {fieldName}.");
        field.SetValue(target, value);
    }

    private static List<RelicTable_SO.RarityWeightEntry> CreateWeights(float weight)
    {
        return new List<RelicTable_SO.RarityWeightEntry>
        {
            new RelicTable_SO.RarityWeightEntry { rarity = LootRarity.Common, weight = weight },
            new RelicTable_SO.RarityWeightEntry { rarity = LootRarity.Uncommon, weight = weight },
            new RelicTable_SO.RarityWeightEntry { rarity = LootRarity.Rare, weight = weight },
            new RelicTable_SO.RarityWeightEntry { rarity = LootRarity.Epic, weight = weight },
            new RelicTable_SO.RarityWeightEntry { rarity = LootRarity.Legendary, weight = weight }
        };
    }

    private static void SetPrivateList<T>(RelicTable_SO table, string fieldName, List<T> value)
    {
        FieldInfo field = typeof(RelicTable_SO).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"Missing field {fieldName}.");
        field.SetValue(table, value);
    }

    private static void AssertHasIssue(
        List<RelicValidationIssue> issues,
        RelicValidationSeverity severity,
        string fieldPath)
    {
        for (int i = 0; i < issues.Count; i++)
        {
            RelicValidationIssue issue = issues[i];
            if (issue.Severity == severity && issue.FieldPath.Contains(fieldPath))
                return;
        }

        Assert.Fail($"Expected {severity} issue containing field path '{fieldPath}'.");
    }

    private static void AssertNoIssue(
        List<RelicValidationIssue> issues,
        RelicValidationSeverity severity,
        string fieldPath)
    {
        for (int i = 0; i < issues.Count; i++)
        {
            RelicValidationIssue issue = issues[i];
            if (issue.Severity == severity && issue.FieldPath.Contains(fieldPath))
                Assert.Fail($"Did not expect {severity} issue containing field path '{fieldPath}'.");
        }
    }
}
