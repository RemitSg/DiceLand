using System.Collections.Generic;
using UnityEngine;

// A single validation/consumption path for both the forge UI and agent decisions.
public static class RunCraftingService
{
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
    {
        message = null;
        if (state == null || blueprint == null) { message = "无法进行锻造。"; return false; }
        if (state.GetBlueprintAmount(blueprint) <= 0) { message = "尚未拥有该图纸。"; return false; }
        if (blueprint.targetWeapon == null) { message = "该图纸未配置目标武器。"; return false; }
        List<CraftingMaterialRequirement> requirements = GetRequirements(blueprint);
        if (requirements.Count == 0) { message = "该图纸未配置有效材料需求。"; return false; }
        int cost = Cost(state);
        if (state.money < cost) { message = $"金币不足：需要 {cost} 金币"; return false; }
        foreach (CraftingMaterialRequirement entry in requirements)
            if (state.GetLootAmount(entry.lootItem) < entry.amount)
            { message = $"材料不足：{entry.lootItem.lootName} x{entry.amount}"; return false; }
        return true;
    }

    public static bool TryCraft(RunState state, CraftingBlueprint_SO blueprint, out string message)
    {
        if (!CanCraft(state, blueprint, out message)) return false;
        foreach (CraftingMaterialRequirement entry in GetRequirements(blueprint)) state.TryConsumeLoot(entry.lootItem, entry.amount);
        state.TrySpendMoney(Cost(state));
        state.AddWeapon(blueprint.targetWeapon);
        if (state.settlementStats == null) state.settlementStats = new RunSettlementStats();
        state.settlementStats.craftedWeaponCount++;
        message = $"成功锻造：{blueprint.targetWeapon.weaponName}";
        return true;
    }
}
