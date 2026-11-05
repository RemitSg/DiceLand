using System.Collections.Generic;
using UnityEngine;

// A single validation/consumption path for both the forge UI and agent decisions.
public static class RunCraftingService
{
    public enum Availability { Ready, Unavailable, InsufficientGold, InsufficientMaterials }

    public static List<CraftingMaterialRequirement> GetRequirements(CraftingBlueprint_SO blueprint)
    {
        var result = new List<CraftingMaterialRequirement>();
        if (blueprint?.materialRequirements == null) return result;
        foreach (CraftingMaterialRequirement entry in blueprint.materialRequirements)
        {
            if (entry?.lootItem == null) continue;
            if (entry.amount <= 0) return new List<CraftingMaterialRequirement>();
            CraftingMaterialRequirement total = result.Find(r => r.lootItem == entry.lootItem);
            if (total == null) result.Add(new CraftingMaterialRequirement { lootItem = entry.lootItem, amount = entry.amount });
            else
            {
                long sum = (long)total.amount + entry.amount;
                if (sum > int.MaxValue) return new List<CraftingMaterialRequirement>();
                total.amount = (int)sum;
            }
        }
        return result;
    }

    public static int Cost(RunState state) => CalamitySystem.ApplyForgeCostModifiers(0, state != null ? state.activeCalamities : null);

    public static bool CanCraft(RunState state, CraftingBlueprint_SO blueprint, out string message)
        => CheckAvailability(state, blueprint, out message) == Availability.Ready;

    public static Availability CheckAvailability(RunState state, CraftingBlueprint_SO blueprint, out string message)
    {
        message = null;
        if (state == null || blueprint == null) { message = GameText.Get("ui.run_crafting.forging_is_unavailable", "无法进行锻造。"); return Availability.Unavailable; }
        if (state.GetBlueprintAmount(blueprint) <= 0) { message = GameText.Get("ui.run_crafting.you_do_not_own_this_blueprint", "尚未拥有该图纸。"); return Availability.Unavailable; }
        if (blueprint.targetWeapon == null) { message = GameText.Get("ui.run_crafting.this_blueprint_has_no_weapon_configured", "该图纸未配置目标武器。"); return Availability.Unavailable; }
        List<CraftingMaterialRequirement> requirements = GetRequirements(blueprint);
        if (requirements.Count == 0) { message = GameText.Get("ui.run_crafting.this_blueprint_has_no_valid_material_requirements", "该图纸未配置有效材料需求。"); return Availability.Unavailable; }
        int cost = Cost(state);
        if (state.money < cost) { message = GameText.Format("ui.run_crafting.you_need_gold", "金币不足：需要 {cost} 金币", ("cost", cost)); return Availability.InsufficientGold; }
        foreach (CraftingMaterialRequirement entry in requirements)
            if (state.GetLootAmount(entry.lootItem) < entry.amount)
            { message = GameText.Format("ui.run_crafting.missing_material", "材料不足：{localizedName} x{amount}", ("localizedName", entry.lootItem.LocalizedName), ("amount", entry.amount)); return Availability.InsufficientMaterials; }
        return Availability.Ready;
    }

    public static bool TryCraft(RunState state, CraftingBlueprint_SO blueprint, out string message)
    {
        if (!CanCraft(state, blueprint, out message)) return false;
        foreach (CraftingMaterialRequirement entry in GetRequirements(blueprint)) state.TryConsumeLoot(entry.lootItem, entry.amount);
        state.TrySpendMoney(Cost(state));
        state.AddWeapon(blueprint.targetWeapon);
        if (state.settlementStats == null) state.settlementStats = new RunSettlementStats();
        state.settlementStats.craftedWeaponCount++;
        message = GameText.Format("ui.run_crafting.forged", "成功锻造：{localizedName}", ("localizedName", blueprint.targetWeapon.LocalizedName));
        return true;
    }
}
