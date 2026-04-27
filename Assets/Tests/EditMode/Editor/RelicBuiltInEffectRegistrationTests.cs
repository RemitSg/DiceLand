using System;
using System.Collections.Generic;
using NUnit.Framework;

public class RelicBuiltInEffectRegistrationTests
{
    [Test]
    public void AllConfiguredBuiltInEffectTypesHaveRegisteredHandlers()
    {
        List<Relic_SO.RelicBuiltInEffectType> missingEffects =
            RelicSystem.GetUnregisteredBuiltInEffectTypesForDiagnostics();

        Assert.IsEmpty(missingEffects, "Every non-None relic built-in effect enum value should have a registered handler.");
    }

    [Test]
    public void NoneAndUnknownBuiltInEffectTypesAreNotRegistered()
    {
        Assert.IsFalse(RelicSystem.IsBuiltInEffectRegistered(Relic_SO.RelicBuiltInEffectType.None));
        Assert.IsFalse(RelicSystem.IsBuiltInEffectRegistered((Relic_SO.RelicBuiltInEffectType)9999));
    }

    [Test]
    public void AllConfiguredBuiltInEffectTypesHaveValidationMetadata()
    {
        List<Relic_SO.RelicBuiltInEffectType> missingMetadata =
            RelicBuiltInEffectMetadataCatalog.GetMissingMetadataForDiagnostics();

        Assert.IsEmpty(missingMetadata, "Every non-None relic built-in effect enum value should have validation metadata.");
    }

    [Test]
    public void ValidationMetadata_DescribesRequiredAssetsAndNumericRules()
    {
        RelicBuiltInEffectMetadata statusMetadata = RequireMetadata(Relic_SO.RelicBuiltInEffectType.ApplyStatusToAllEnemiesOnSpecificRound);
        Assert.IsTrue(statusMetadata.RequiresStatusData);
        Assert.IsFalse(statusMetadata.RequiresConditionStatusData);
        Assert.AreEqual(RelicBuiltInNumericRequirement.AmountPositive, statusMetadata.NumericRequirement);
        Assert.AreEqual("amount", statusMetadata.NumericFieldPath);

        RelicBuiltInEffectMetadata conditionMetadata = RequireMetadata(Relic_SO.RelicBuiltInEffectType.ModifyPlayerDiceWhenSelfHasStatus);
        Assert.IsFalse(conditionMetadata.RequiresStatusData);
        Assert.IsTrue(conditionMetadata.RequiresConditionStatusData);
        Assert.AreEqual(RelicBuiltInNumericRequirement.AnyPowerDeltaNonZero, conditionMetadata.NumericRequirement);
        Assert.AreEqual("powerAmount/minPowerAmount/maxPowerAmount", conditionMetadata.NumericFieldPath);

        RelicBuiltInEffectMetadata passiveMetadata = RequireMetadata(Relic_SO.RelicBuiltInEffectType.TransferSpeedButtonsToLowestHpAllyOnPlayerDeath);
        Assert.IsFalse(passiveMetadata.RequiresStatusData);
        Assert.IsFalse(passiveMetadata.RequiresConditionStatusData);
        Assert.AreEqual(RelicBuiltInNumericRequirement.None, passiveMetadata.NumericRequirement);
    }

    private static RelicBuiltInEffectMetadata RequireMetadata(Relic_SO.RelicBuiltInEffectType effectType)
    {
        RelicBuiltInEffectMetadata metadata;
        Assert.IsTrue(RelicBuiltInEffectMetadataCatalog.TryGet(effectType, out metadata), $"Missing metadata for {effectType}.");
        return metadata;
    }
}
