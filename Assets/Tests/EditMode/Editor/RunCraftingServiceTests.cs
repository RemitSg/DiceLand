using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public sealed class RunCraftingServiceTests
{
    private readonly List<ScriptableObject> temporary = new List<ScriptableObject>();
    private LootItem_SO material;
    private CraftingBlueprint_SO blueprint;
    private RunState state;
    private T Make<T>() where T : ScriptableObject { var v = ScriptableObject.CreateInstance<T>(); temporary.Add(v); return v; }
    [SetUp] public void SetUp()
    {
        material = Make<LootItem_SO>(); material.lootName = "Material";
        blueprint = Make<CraftingBlueprint_SO>(); blueprint.targetWeapon = Make<Weapon_SO>();
        blueprint.materialRequirements = new List<CraftingMaterialRequirement>
        { new CraftingMaterialRequirement { lootItem = material, amount = 2 }, new CraftingMaterialRequirement { lootItem = material, amount = 3 } };
        state = new RunState { money = 5 }; state.AddBlueprint(blueprint);
    }
    [TearDown] public void TearDown() { foreach (var o in temporary) Object.DestroyImmediate(o); temporary.Clear(); }

    [Test] public void DuplicateMaterialRows_RequireTheirCombinedAmountBeforeAnyMutation()
    {
        state.AddLoot(material, 4); string before = JsonUtility.ToJson(state);
        Assert.IsFalse(RunCraftingService.CanCraft(state, blueprint, out _));
        Assert.IsFalse(RunCraftingService.TryCraft(state, blueprint, out _));
        Assert.AreEqual(before, JsonUtility.ToJson(state));
        Assert.AreEqual(5, RunCraftingService.GetRequirements(blueprint)[0].amount);
    }
    [Test] public void Craft_SpendsCombinedMaterialsAndCalamityCostAndRetainsTheBlueprint()
    {
        state.AddLoot(material, 6);
        Calamity_SO calamity = Make<Calamity_SO>();
        calamity.builtInEffects.Add(new CalamityBuiltInEffectEntry { effectType = CalamityBuiltInEffectType.ForgeCostFlatModifier, effectAmount = 3 });
        state.AddCalamity(calamity, 1);
        Assert.IsTrue(RunCraftingService.TryCraft(state, blueprint, out _));
        Assert.AreEqual(1, state.GetLootAmount(material)); Assert.AreEqual(2, state.money);
        Assert.AreEqual(1, state.GetWeaponAmount(blueprint.targetWeapon)); Assert.AreEqual(1, state.GetBlueprintAmount(blueprint));
        Assert.AreEqual(1, state.settlementStats.craftedWeaponCount);
        string before = JsonUtility.ToJson(state);
        Assert.IsFalse(RunCraftingService.TryCraft(state, blueprint, out _)); Assert.AreEqual(before, JsonUtility.ToJson(state));
    }
    [Test] public void UnownedBlueprint_IsRejectedEvenWithSufficientMaterials()
    {
        state.blueprintInventory.Clear(); state.AddLoot(material, 10);
        Assert.IsFalse(RunCraftingService.TryCraft(state, blueprint, out _));
        Assert.AreEqual(10, state.GetLootAmount(material)); Assert.AreEqual(0, state.GetWeaponAmount(blueprint.targetWeapon));
    }
    [Test] public void MissingInvalidAndOverflowingRequirements_CannotCreateFreeWeapons()
    {
        state.AddLoot(material, 10);
        foreach (int amount in new[] { 0, -1, int.MaxValue })
        {
            blueprint.materialRequirements[0].amount = amount;
            Assert.IsFalse(RunCraftingService.TryCraft(state, blueprint, out _));
        }
        blueprint.materialRequirements.Clear(); blueprint.materialRequirements.Add(null);
        Assert.IsFalse(RunCraftingService.TryCraft(state, blueprint, out _));
        Assert.AreEqual(10, state.GetLootAmount(material)); Assert.AreEqual(5, state.money);
        Assert.AreEqual(0, state.GetWeaponAmount(blueprint.targetWeapon));
    }
    [Test] public void DebtAndInsufficientForgeFee_DoNotConsumeMaterials()
    {
        state.AddLoot(material, 5); state.money = -1;
        Assert.IsFalse(RunCraftingService.TryCraft(state, blueprint, out _));
        Assert.AreEqual(5, state.GetLootAmount(material));
    }
}
