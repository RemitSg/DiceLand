using System.Collections.Generic;
using System.Text;

/// <summary>Uses the forge's validated, aggregated requirements and current run balances.</summary>
public static class BlueprintRequirementPreviewService
{
    private const string Ready = "#9BE2AF";
    private const string Missing = "#F49B84";

    public static string BuildDescription(CraftingBlueprint_SO blueprint, RunState state)
    {
        if (blueprint == null) return "暂无图纸信息。";
        var text = new StringBuilder();
        List<CraftingMaterialRequirement> requirements = RunCraftingService.GetRequirements(blueprint);
        text.AppendLine(state != null ? "锻造需求（持有 / 需求）" : "所需材料：");
        bool complete = requirements.Count > 0;
        if (state != null)
        {
            int cost = RunCraftingService.Cost(state);
            if (cost > 0 || state.money < cost) AppendRequirement(text, "金币", state.money, cost, ref complete);
        }
        foreach (CraftingMaterialRequirement requirement in requirements)
        {
            if (state != null)
                AppendRequirement(text, requirement.lootItem.lootName, state.GetLootAmount(requirement.lootItem), requirement.amount, ref complete);
            else text.AppendLine(" - " + requirement.lootItem.lootName + " ×" + requirement.amount);
        }
        if (requirements.Count == 0) text.AppendLine("未配置有效材料需求。");
        if (state != null && requirements.Count > 0)
        {
            text.AppendLine();
            text.AppendLine(complete ? "<color=" + Ready + ">材料齐备</color>" : "<color=" + Missing + ">材料尚未备齐</color>");
        }
        string target = blueprint.targetWeapon != null && !string.IsNullOrWhiteSpace(blueprint.targetWeapon.weaponName)
            ? blueprint.targetWeapon.weaponName : "未配置武器";
        text.AppendLine();
        text.AppendLine("锻造成品：" + target);
        text.AppendLine();
        text.AppendLine(string.IsNullOrWhiteSpace(blueprint.description) ? "暂无图纸描述。" : blueprint.description);
        return text.ToString().TrimEnd();
    }

    private static void AppendRequirement(StringBuilder text, string name, int owned, int required, ref bool complete)
    {
        bool ready = owned >= required;
        complete &= ready;
        text.Append(" - ").Append(name).Append("  <color=").Append(ready ? Ready : Missing).Append('>')
            .Append(owned).Append(" / ").Append(required).Append("</color>")
            .AppendLine(ready ? "  已满足" : "  还缺 " + ((long)required - owned));
    }
}
