using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

public sealed class BlueprintRequirementPreviewTests
{
    private readonly List<Object> owned = new List<Object>();
    private T Make<T>() where T : ScriptableObject { T value = ScriptableObject.CreateInstance<T>(); owned.Add(value); return value; }
    [TearDown] public void TearDown() { foreach (Object value in owned) Object.DestroyImmediate(value); owned.Clear(); }

    [Test]
    public void CountsAreAggregatedAndShortagesBecomeReadyUsingTheSameForgeRules()
    {
        CraftingBlueprint_SO blueprint = Make<CraftingBlueprint_SO>(); blueprint.targetWeapon = Make<Weapon_SO>();
        blueprint.description = "这是一张完整保留的图纸说明。";
        LootItem_SO ore = Make<LootItem_SO>(), feather = Make<LootItem_SO>(); ore.lootName = "铁矿"; feather.lootName = "焰羽";
        blueprint.materialRequirements.Add(new CraftingMaterialRequirement { lootItem = ore, amount = 2 });
        blueprint.materialRequirements.Add(new CraftingMaterialRequirement { lootItem = ore, amount = 3 });
        blueprint.materialRequirements.Add(new CraftingMaterialRequirement { lootItem = feather, amount = 1 });
        var state = new RunState(); state.AddPreviewedBlueprint(blueprint); state.AddLoot(ore, 2); state.AddLoot(feather, 1);
        string missing = BlueprintRequirementPreviewService.BuildDescription(blueprint, state);
        Assert.That(missing, Does.Contain("2 / 5").And.Contain("还缺 3").And.Contain("1 / 1").And.Contain("#F49B84").And.Contain("#9BE2AF"));
        Assert.Less(missing.IndexOf("2 / 5"), missing.IndexOf(blueprint.description), "Material counts should be visible before the longer flavor description.");
        Assert.IsFalse(RunCraftingService.CanCraft(state, blueprint, out _));
        Assert.AreEqual(missing.IndexOf("铁矿"), missing.LastIndexOf("铁矿"));
        state.AddLoot(ore, 3);
        Assert.That(BlueprintRequirementPreviewService.BuildDescription(blueprint, state), Does.Contain("5 / 5").And.Contain("材料齐备"));
        Assert.IsTrue(RunCraftingService.CanCraft(state, blueprint, out _));
        Assert.AreEqual(5, state.GetLootAmount(ore)); Assert.AreEqual(0, state.GetWeaponAmount(blueprint.targetWeapon));
    }

    [Test]
    public void ActualCalamityFeeAndNegativeWalletAreShownWithoutInventingOwnedMaterials()
    {
        CraftingBlueprint_SO blueprint = Make<CraftingBlueprint_SO>(); LootItem_SO ore = Make<LootItem_SO>(); ore.lootName = "铁矿";
        blueprint.materialRequirements.Add(new CraftingMaterialRequirement { lootItem = ore, amount = 1 });
        Calamity_SO calamity = Make<Calamity_SO>();
        calamity.builtInEffects.Add(new CalamityBuiltInEffectEntry { effectType = CalamityBuiltInEffectType.ForgeCostFlatModifier, effectAmount = 3 });
        var state = new RunState { money = -2 }; state.AddCalamity(calamity, 1);
        string text = BlueprintRequirementPreviewService.BuildDescription(blueprint, state);
        Assert.That(text, Does.Contain("金币").And.Contain("-2 / 3").And.Contain("还缺 5").And.Contain("0 / 1"));
        string noContext = BlueprintRequirementPreviewService.BuildDescription(blueprint, null);
        Assert.That(noContext, Does.Contain("铁矿 ×1").And.Not.Contain("还缺").And.Not.Contain("0 / 1"));
    }

    [Test]
    public void InvalidConfiguredRequirementsFollowTheForgeInsteadOfDisplayingFalseReadiness()
    {
        CraftingBlueprint_SO blueprint = Make<CraftingBlueprint_SO>(); LootItem_SO ore = Make<LootItem_SO>();
        blueprint.materialRequirements.Add(new CraftingMaterialRequirement { lootItem = ore, amount = 0 });
        string text = BlueprintRequirementPreviewService.BuildDescription(blueprint, new RunState());
        Assert.That(text, Does.Contain("未配置有效材料需求").And.Not.Contain("材料齐备"));
    }
}
