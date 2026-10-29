using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class RunReforgeDefinition
{
    public LootItemTable_SO lootTable;
    public RelicTable_SO relicTable;
    public IReadOnlyList<LootAttribute_SO> attributes;
    public IReadOnlyList<LootItem_SO> LootPool => lootTable != null && lootTable.IsValid ? lootTable.LootItems : Array.Empty<LootItem_SO>();
    public IReadOnlyList<Relic_SO> RelicPool => relicTable != null && relicTable.IsValid ? relicTable.Relics : Array.Empty<Relic_SO>();
}

public static class RunRestService
{
    public static int ReforgeLimit(RunState state) => Mathf.Max(2, RelicRestReforgeService.CalculateLimit(state, 2));
    public static int AdvanceReforgeCount(int successful, int limit) => Mathf.Clamp(successful + 1, 0, limit);

    public static List<LootAttribute_SO> ReforgeAttributes(RunReforgeDefinition definition)
    {
        var result = new List<LootAttribute_SO>(); var seen = new HashSet<LootAttribute_SO>();
        if (definition?.attributes != null)
            foreach (var a in definition.attributes) if (a != null && seen.Add(a)) result.Add(a);
        if (result.Count > 0 || definition == null) return result;
        foreach (var item in definition.LootPool) if (item?.attribute != null && seen.Add(item.attribute)) result.Add(item.attribute);
        result.Sort((a,b) => string.Compare(AttributeName(a), AttributeName(b), StringComparison.OrdinalIgnoreCase));
        return result;
    }

    public static string AttributeName(LootAttribute_SO attribute) => attribute != null ? (!string.IsNullOrWhiteSpace(attribute.attributeName) ? attribute.attributeName : attribute.name) : string.Empty;

    public static bool ValidateLootReforge(RunState state, LootItem_SO first, LootItem_SO second, LootAttribute_SO attribute, out string error)
    {
        error = null;
        if (first == null || second == null) { error = "请先选择两件战利品材料。"; return false; }
        if (attribute == null) { error = "请先选择目标属性。"; return false; }
        if (first.rarity != second.rarity) { error = "两件战利品必须为同阶级。"; return false; }
        if (state == null) { error = "未找到当前队伍背包数据。"; return false; }
        if (state.GetLootAmount(first) < (first == second ? 2 : 1)) { error = "材料不足：" + first.lootName; return false; }
        if (first != second && state.GetLootAmount(second) < 1) { error = "材料不足：" + second.lootName; return false; }
        return true;
    }

    public static bool ValidateRelicReforge(RunState state, Relic_SO first, Relic_SO second, out string error)
    {
        error = null;
        if (first == null || second == null) { error = "请先选择两件遗物材料。"; return false; }
        if (first.rarity != second.rarity) { error = "两件遗物必须为同阶级。"; return false; }
        if (first.rarity >= LootRarity.Legendary) { error = "传说遗物无法继续升阶重铸。"; return false; }
        if (state == null) { error = "未找到当前队伍遗物数据。"; return false; }
        if (state.GetRelicAmount(first) < (first == second ? 2 : 1)) { error = "材料不足：" + first.DisplayName; return false; }
        if (first != second && state.GetRelicAmount(second) < 1) { error = "材料不足：" + second.DisplayName; return false; }
        return true;
    }

    public static List<LootItem_SO> LootReforgeCandidates(RunReforgeDefinition definition, LootItem_SO first, LootItem_SO second, LootAttribute_SO attribute)
    {
        if (definition == null || first == null || second == null || attribute == null || first.rarity != second.rarity) return new List<LootItem_SO>();
        return RewardCandidateFilterService.BuildLootReforgeCandidates(definition.LootPool, first.rarity, attribute);
    }

    public static LootItem_SO RollLootReforge(RunReforgeDefinition definition, LootItem_SO first, LootItem_SO second, LootAttribute_SO attribute, System.Random random)
    {
        var candidates = LootReforgeCandidates(definition, first, second, attribute);
        return candidates.Count > 0 ? candidates[random.Next(candidates.Count)] : null;
    }

    public static Relic_SO RollRelicReforge(RunReforgeDefinition definition, Relic_SO first, Relic_SO second, System.Random random, RunState state = null)
    {
        if (definition == null || first == null || second == null || first.rarity != second.rarity || first.rarity >= LootRarity.Legendary) return null;
        if (RelicFixedReforgeService.TryResolveFixedResult(state, first, second, out Relic_SO fixedResult))
            return fixedResult;
        for (int rarity = (int)first.rarity + 1; rarity <= (int)LootRarity.Legendary; rarity++)
        {
            var candidates = new List<Relic_SO>();
            foreach (var relic in definition.RelicPool) if (relic != null && (int)relic.rarity == rarity) candidates.Add(relic);
            if (candidates.Count > 0) return candidates[random.Next(candidates.Count)];
        }
        return null;
    }

    public static System.Random ReforgeRandom(int successful, int mode, Vector2Int coord, string logicalSceneName,
        LootItem_SO lootA, LootItem_SO lootB, LootAttribute_SO attribute, Relic_SO relicA, Relic_SO relicB, string salt)
    {
        unchecked
        {
            int seed = 17;
            seed = seed * 31 + successful; seed = seed * 31 + mode;
            seed = seed * 31 + coord.x; seed = seed * 31 + coord.y;
            seed = seed * 31 + (logicalSceneName != null ? logicalSceneName.GetHashCode() : 0);
            seed = seed * 31 + ObjectHash(lootA); seed = seed * 31 + ObjectHash(lootB); seed = seed * 31 + ObjectHash(attribute);
            seed = seed * 31 + ObjectHash(relicA); seed = seed * 31 + ObjectHash(relicB);
            seed = seed * 31 + (salt != null ? salt.GetHashCode() : 0);
            return new System.Random(seed);
        }
    }
    private static int ObjectHash(UnityEngine.Object value) => value != null ? value.name.GetHashCode() : 0;

    public static List<CharacterData_SO> GetRecruitCandidates(RunState state, Vector2Int coord, string logicalSceneName, CharacterRoster_SO roster, DifficultyConfig_SO difficulty)
    {
        if (state == null || roster == null || difficulty == null || state.recruitedMemberCount >= Mathf.Max(1, difficulty.maxRecruitCount))
            return new List<CharacterData_SO>();
        int seed = coord.x * 73856093 ^ coord.y * 19349663 ^ (logicalSceneName != null ? logicalSceneName.GetHashCode() : 0);
        return roster.GetRandomUnownedCharacters(state, 0, 2, new System.Random(seed));
    }

    public static int RecruitCost(RunState state, DifficultyConfig_SO difficulty)
    {
        return state != null && difficulty != null ? difficulty.GetRecruitCostForCurrentRecruitCount(state.recruitedMemberCount) : 0;
    }
}
