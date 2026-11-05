using System.Collections.Generic;
using System.Text;

/// <summary>Uses the forge's validated, aggregated requirements and current run balances.</summary>
public static class BlueprintRequirementPreviewService
{
    private const string Ready = "#9BE2AF";
    private const string Missing = "#F49B84";

    public static string BuildDescription(CraftingBlueprint_SO blueprint, RunState state)
    {
        if (blueprint == null) return GameText.Get("ui.blueprint_requirement_preview.no_blueprint_information_available", "暂无图纸信息。");
        var text = new StringBuilder();
        List<CraftingMaterialRequirement> requirements = RunCraftingService.GetRequirements(blueprint);
        text.AppendLine(state != null ? GameText.Get("ui.blueprint_requirement_preview.forge_requirements_owned_required", "锻造需求（持有 / 需求）") : GameText.Get("ui.blueprint_requirement_preview.materials", "所需材料："));
        bool complete = requirements.Count > 0;
        if (state != null)
        {
            int cost = RunCraftingService.Cost(state);
            if (cost > 0 || state.money < cost) AppendRequirement(text, GameText.Get("term.gold", "金币"), state.money, cost, ref complete);
        }
        foreach (CraftingMaterialRequirement requirement in requirements)
        {
            if (state != null)
                AppendRequirement(text, requirement.lootItem.LocalizedName, state.GetLootAmount(requirement.lootItem), requirement.amount, ref complete);
            else text.AppendLine(" - " + requirement.lootItem.LocalizedName + " ×" + requirement.amount);
        }
        if (requirements.Count == 0) text.AppendLine(GameText.Get("ui.blueprint_requirement_preview.no_valid_material_requirements", "未配置有效材料需求。"));
        if (state != null && requirements.Count > 0)
        {
            text.AppendLine();
            text.AppendLine(complete ? "<color=" + Ready + GameText.Get("ui.blueprint_requirement_preview.materials_ready", ">材料齐备</color>") : "<color=" + Missing + GameText.Get("ui.blueprint_requirement_preview.materials_missing", ">材料尚未备齐</color>"));
        }
        string target = blueprint.targetWeapon != null && !string.IsNullOrWhiteSpace(blueprint.targetWeapon.weaponName)
            ? blueprint.targetWeapon.LocalizedName : GameText.Get("ui.blueprint_requirement_preview.no_weapon_configured", "未配置武器");
        text.AppendLine();
        text.AppendLine(GameText.Get("ui.blueprint_requirement_preview.produces", "锻造成品：") + target);
        text.AppendLine();
        text.AppendLine(string.IsNullOrWhiteSpace(blueprint.description) ? GameText.Get("ui.map_crafting.no_blueprint_description_available", "暂无图纸描述。") : blueprint.LocalizedDescription);
        return text.ToString().TrimEnd();
    }

    private static void AppendRequirement(StringBuilder text, string name, int owned, int required, ref bool complete)
    {
        bool ready = owned >= required;
        complete &= ready;
        text.Append(" - ").Append(name).Append("  <color=").Append(ready ? Ready : Missing).Append('>')
            .Append(owned).Append(" / ").Append(required).Append("</color>")
            .AppendLine(ready ? GameText.Get("ui.blueprint_requirement_preview.ready", "  已满足") : GameText.Get("ui.blueprint_requirement_preview.missing", "  还缺 ") + ((long)required - owned));
    }
}
