using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class MapEventInventoryReforgeService
{
    public static bool RequiresItem(MapEventOptionData option)
    {
        switch (option?.builtInEffectType)
        {
            case MapEventOptionBuiltInEffectType.ReforgeSelectedRelicSameRarity:
            case MapEventOptionBuiltInEffectType.ReforgeSelectedBlueprintSameRarity:
            case MapEventOptionBuiltInEffectType.ReforgeSelectedLootSameRarity:
                return true;
            default:
                return false;
        }
    }

    public static IEnumerable<ScriptableObject> GetEligibleMaterials(RunState state, MapEventOptionData option)
    {
        if (state == null || !RequiresItem(option))
            return Enumerable.Empty<ScriptableObject>();

        var effect = MapEventEffectSequenceBuilder.BuildPrimaryEffect(option);
        return OwnedItems(state, option.builtInEffectType).Where(item => Replacements(effect, item).Any());
    }

    public static bool HasReplacement(MapEventOptionData option, ScriptableObject item)
        => RequiresItem(option) && Replacements(MapEventEffectSequenceBuilder.BuildPrimaryEffect(option), item).Any();

    public static string ItemName(ScriptableObject item)
    {
        switch (item)
        {
            case Relic_SO relic: return relic.LocalizedName;
            case CraftingBlueprint_SO blueprint: return blueprint.LocalizedName;
            case LootItem_SO loot: return loot.LocalizedName;
            default: return item != null ? item.name : string.Empty;
        }
    }

    internal static bool TryReforge(RunState state, MapEventBuiltInEffectData effect, ScriptableObject material, out string message)
    {
        if (material == null || state == null)
        {
            message = GameText.Get("ui.event.reforge.pick_material", "请先从背包选中要重铸的物品。");
            return false;
        }

        ScriptableObject[] candidates = Replacements(effect, material).Distinct().ToArray();
        if (candidates.Length == 0)
        {
            message = GameText.Get("ui.event.reforge.no_replacement", "没有其他同品质的物品可供重铸。");
            return false;
        }

        bool consumed;
        switch (material)
        {
            case Relic_SO relic: consumed = state.TryConsumeRelic(relic, 1); break;
            case CraftingBlueprint_SO blueprint: consumed = state.TryConsumeBlueprint(blueprint, 1); break;
            case LootItem_SO loot: consumed = state.TryConsumeLoot(loot, 1); break;
            default: consumed = false; break;
        }
        if (!consumed)
        {
            message = GameText.Get("ui.event.reforge.material_unavailable", "背包里已没有这件重铸材料。");
            return false;
        }

        ScriptableObject result = candidates[UnityEngine.Random.Range(0, candidates.Length)];
        switch (result)
        {
            case Relic_SO relic: state.AddRelic(relic); break;
            case CraftingBlueprint_SO blueprint: state.AddBlueprint(blueprint); break;
            case LootItem_SO loot: state.AddLoot(loot, 1); break;
        }
        message = GameText.Format("ui.event.reforge.result", "{material} → {result}", ("material", ItemName(material)), ("result", ItemName(result)));
        return true;
    }

    private static IEnumerable<ScriptableObject> OwnedItems(RunState state, MapEventOptionBuiltInEffectType effect)
    {
        switch (effect)
        {
            case MapEventOptionBuiltInEffectType.ReforgeSelectedRelicSameRarity:
                return state.relicInventory?.Where(stack => stack != null && stack.amount > 0 && stack.relicData != null)
                    .Select(stack => stack.relicData) ?? Enumerable.Empty<Relic_SO>();
            case MapEventOptionBuiltInEffectType.ReforgeSelectedBlueprintSameRarity:
                return state.blueprintInventory?.Where(stack => stack != null && stack.amount > 0 && stack.blueprintData != null)
                    .Select(stack => stack.blueprintData) ?? Enumerable.Empty<CraftingBlueprint_SO>();
            case MapEventOptionBuiltInEffectType.ReforgeSelectedLootSameRarity:
                return state.lootInventory?.Where(stack => stack != null && stack.amount > 0 && stack.lootItem != null)
                    .Select(stack => stack.lootItem) ?? Enumerable.Empty<LootItem_SO>();
            default:
                return Enumerable.Empty<ScriptableObject>();
        }
    }

    private static IEnumerable<ScriptableObject> Replacements(MapEventBuiltInEffectData effect, ScriptableObject item)
    {
        switch (effect.effectType)
        {
            case MapEventOptionBuiltInEffectType.ReforgeSelectedRelicSameRarity when item is Relic_SO relic:
                return SameRarity(effect.relicTable?.Relics, relic, value => value.rarity);
            case MapEventOptionBuiltInEffectType.ReforgeSelectedBlueprintSameRarity when item is CraftingBlueprint_SO blueprint:
                return SameRarity(effect.blueprintTable?.Blueprints, blueprint, value => value.rarity);
            case MapEventOptionBuiltInEffectType.ReforgeSelectedLootSameRarity when item is LootItem_SO loot:
                return SameRarity(effect.lootReforgeTable?.LootItems, loot, value => value.rarity);
            default:
                return Enumerable.Empty<ScriptableObject>();
        }
    }

    private static IEnumerable<T> SameRarity<T>(IReadOnlyList<T> pool, T original, Func<T, LootRarity> rarity) where T : ScriptableObject
    {
        if (pool == null || original == null)
            return Enumerable.Empty<T>();
        LootRarity originalRarity = rarity(original);
        return pool.Where(candidate => candidate != null && candidate != original && rarity(candidate) == originalRarity);
    }
}
