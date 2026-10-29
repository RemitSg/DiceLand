var paths = UnityEditor.AssetDatabase.FindAssets("t:Relic_SO", new[] { "Assets/Data" });
var rows = new System.Collections.Generic.List<object>();
foreach (var guid in paths)
{
    var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
    var relic = UnityEditor.AssetDatabase.LoadAssetAtPath<Relic_SO>(path);
    if (relic == null) continue;
    var effects = new System.Collections.Generic.List<object>();
    foreach (var effect in relic.builtInEffects)
    {
        if (effect == null) continue;
        var references = new System.Collections.Generic.Dictionary<string, object>();
        foreach (var field in effect.GetType().GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
        {
            if (!typeof(UnityEngine.Object).IsAssignableFrom(field.FieldType)) continue;
            var obj = field.GetValue(effect) as UnityEngine.Object;
            var refPath = obj != null ? UnityEditor.AssetDatabase.GetAssetPath(obj) : "";
            references[field.Name] = new { name = obj != null ? obj.name : "", path = refPath, guid = string.IsNullOrEmpty(refPath) ? "" : UnityEditor.AssetDatabase.AssetPathToGUID(refPath), valid = obj != null };
        }
        effects.Add(new { trigger = effect.trigger.ToString(), effect = effect.effectType.ToString(), data = Newtonsoft.Json.Linq.JObject.Parse(UnityEngine.JsonUtility.ToJson(effect)), references = references });
    }
    var iconPath = relic.icon != null ? UnityEditor.AssetDatabase.GetAssetPath(relic.icon) : "";
    var tables = new System.Collections.Generic.List<string>();
    foreach (var tableGuid in UnityEditor.AssetDatabase.FindAssets("t:RelicTable_SO", new[] { "Assets/Data", "Assets/Resources" }))
    {
        var tablePath = UnityEditor.AssetDatabase.GUIDToAssetPath(tableGuid);
        foreach (var dependency in UnityEditor.AssetDatabase.GetDependencies(tablePath, false))
            if (dependency == path) { tables.Add(tablePath); break; }
    }
    rows.Add(new {
        guid = guid, path = path, relicId = relic.relicId, relicName = relic.DisplayName,
        description = relic.description, additionalDescription = relic.additionalDescription,
        rarity = (int)relic.rarity, rarityName = relic.rarity.ToString(),
        iconPath = iconPath, iconGuid = string.IsNullOrEmpty(iconPath) ? "" : UnityEditor.AssetDatabase.AssetPathToGUID(iconPath),
        iconRect = relic.icon != null ? new[] { relic.icon.rect.x, relic.icon.rect.y, relic.icon.rect.width, relic.icon.rect.height } : new float[0],
        effects = effects, referencedByRelicTables = tables,
        customEffectHandler = relic.customEffectHandler != null ? UnityEditor.AssetDatabase.GetAssetPath(relic.customEffectHandler) : ""
    });
}
var output = System.IO.Path.GetFullPath(System.IO.Path.Combine(UnityEngine.Application.dataPath, "../outputs/relic-catalog/relics-raw.json"));
System.IO.File.WriteAllText(output, Newtonsoft.Json.JsonConvert.SerializeObject(new { exportedAt = System.DateTimeOffset.Now.ToString("o"), rows = rows }, Newtonsoft.Json.Formatting.Indented), new System.Text.UTF8Encoding(false));
return "Exported " + rows.Count + " relic assets to " + output;
