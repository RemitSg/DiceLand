using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization.Tables;

public sealed class LocalizationIdentityTests
{
    [Test]
    public void PreviousSaveResolvesLegacyIdsAndWritesCanonicalIds()
    {
        var character = AssetDatabase.LoadAssetAtPath<CharacterData_SO>("Assets/Data/Characters/Chara/Chara.asset");
        var weapon = AssetDatabase.LoadAssetAtPath<Weapon_SO>("Assets/Data/Weapons/000 WoodenSword/WoodenSword_Data.asset");
        var loot = AssetDatabase.LoadAssetAtPath<LootItem_SO>("Assets/Data/Loots/LootItems/000 史莱姆粘液/SlimeMucus.asset");
        var group = AssetDatabase.LoadAssetAtPath<EnemyGroup_SO>("Assets/Data/Enemies/EnemyGroup/Outer/Danger1/GreenSlime x2.asset");
        var catalog = new MainMenuSaveAssetCatalog();
        catalog.Add(character);
        catalog.Add(weapon);
        catalog.Add(loot);
        catalog.Add(group);

        string path = Path.Combine(Application.persistentDataPath, "menu_continue_save.json");
        byte[] previous = File.Exists(path) ? File.ReadAllBytes(path) : null;
        try
        {
            Directory.CreateDirectory(Application.persistentDataPath);
            File.WriteAllText(path, @"{
              ""metadata"": { ""sceneName"": ""MapScene"", ""difficultyName"": ""Medium"" },
              ""snapshot"": {
                ""sceneName"": ""MapScene"", ""boardSize"": 6,
                ""spawnCoord"": { ""x"": 0, ""y"": 1 }, ""bossCoord"": { ""x"": 5, ""y"": 1 },
                ""selectedCoord"": { ""x"": 1, ""y"": 1 }, ""actionPointBase"": 6, ""actionPointCurrent"": 4,
                ""runState"": {
                  ""difficultyName"": ""Medium"", ""money"": 23,
                  ""partyMembers"": [{ ""memberId"": ""Chara"", ""characterId"": ""Chara"", ""weaponId"": ""新手木剑"",
                     ""level"": 2, ""experience"": 5, ""maxHP"": 16, ""currentHP"": 12 }],
                  ""lootInventory"": [{ ""lootId"": ""史莱姆粘液"", ""amount"": 2 }]
                },
                ""tiles"": [{ ""coord"": { ""x"": 1, ""y"": 1 }, ""assignedEnemyGroupId"": ""GreenSlime x2"" }]
              }
            }");
            Assert.IsTrue(MainMenuRunSaveSystem.TryLoadSnapshot("MapScene", catalog, out MapRunSnapshot snapshot));
            RunPartyMemberState member = snapshot.runState.partyMembers[0];
            Assert.AreEqual("Chara", member.memberId);
            Assert.AreSame(character, member.characterData);
            Assert.AreSame(weapon, member.weaponData);
            Assert.AreSame(loot, snapshot.runState.lootInventory[0].lootItem);
            Assert.AreSame(group, snapshot.tiles[0].assignedEnemyGroup);
            Assert.AreEqual(12, member.currentHP);
            Assert.AreEqual(23, snapshot.runState.money);
            Assert.AreEqual(4, snapshot.actionPointCurrent);

            var serializer = typeof(MainMenuRunSaveSystem).GetMethod("SerializeRunState", BindingFlags.Static | BindingFlags.NonPublic);
            string rewritten = JsonUtility.ToJson(serializer.Invoke(null, new object[] { snapshot.runState }));
            StringAssert.Contains("\"characterId\":\"character.chara\"", rewritten);
            StringAssert.Contains("\"weaponId\":\"weapon.wooden_sword\"", rewritten);
            StringAssert.Contains("\"lootId\":\"loot.slime_mucus\"", rewritten);
            Assert.AreSame(character, catalog.ResolveCharacter("character.chara"));
            Assert.AreSame(group, catalog.ResolveEnemyGroup("enemy_group.green_slime_pair"));
        }
        finally
        {
            if (previous == null) File.Delete(path);
            else File.WriteAllBytes(path, previous);
        }
    }

    [TestCase("030 BloodfeastSpider/SpiderPregnancy.asset", BattleSkillMechanic.SpiderPregnancy)]
    [TestCase("030 BloodfeastSpider/KinFeast.asset", BattleSkillMechanic.SpiderKinFeast)]
    [TestCase("033 WanderingPhantom/BlendIntoShadow.asset", BattleSkillMechanic.BlendIntoShadow)]
    [TestCase("036 Gambler/Gamble.asset", BattleSkillMechanic.Gamble)]
    [TestCase("036 Gambler/GrantedGamble.asset", BattleSkillMechanic.Gamble)]
    public void SpecialSkillsHaveAuthoredMechanicTags(string path, BattleSkillMechanic expected)
    {
        var source = AssetDatabase.LoadAssetAtPath<EnemySkill_SO>("Assets/Data/Enemies/Enemies/" + path);
        var skill = Object.Instantiate(source);
        try
        {
            skill.skillName = "Renamed skill";
            Assert.AreEqual(expected, skill.mechanic);
            Assert.AreEqual(expected == BattleSkillMechanic.Gamble, BattleGamblerPassiveService.IsGambleSkill(skill));
        }
        finally { Object.DestroyImmediate(skill); }
    }

    [Test]
    public void StringTablesHaveBothLanguagesAndMatchingFormatArguments()
    {
        StringTable chinese = LoadTable("zh-Hans");
        StringTable english = LoadTable("en");
        Assert.AreSame(chinese.SharedData, english.SharedData);
        foreach (var shared in chinese.SharedData.Entries)
        {
            StringTableEntry source = chinese.GetEntry(shared.Id);
            StringTableEntry translation = english.GetEntry(shared.Id);
            Assert.IsNotNull(source, shared.Key);
            Assert.IsNotNull(translation, shared.Key);
            Assert.IsNotEmpty(source.Value, shared.Key);
            Assert.IsNotEmpty(translation.Value, shared.Key);
            // Credits keep the contributors' own names. The picker shows the locale's native name.
            if (shared.Key != "ui.credits.people" && translation.Value != "简体中文")
                Assert.IsFalse(Regex.IsMatch(translation.Value, "[\u3400-\u9fff]"), shared.Key);
            Assert.AreEqual(source.IsSmart, translation.IsSmart, shared.Key);
            if (source.IsSmart)
                CollectionAssert.AreEquivalent(FormatArguments(source.Value), FormatArguments(translation.Value), shared.Key);
        }
    }

    [Test]
    public void AuthoredEventsAndSkillsHaveEnglishDisplayEntries()
    {
        StringTable english = LoadTable("en");
        foreach (string guid in AssetDatabase.FindAssets("t:MapEvent_SO", new[] { "Assets/Data/Events" }))
        {
            var data = AssetDatabase.LoadAssetAtPath<MapEvent_SO>(AssetDatabase.GUIDToAssetPath(guid));
            string key = "event." + data.eventId;
            AssertTranslated(english, key + ".name", data.eventName);
            AssertTranslated(english, key + ".body", data.eventContent);
            foreach (MapEventOptionData option in data.GetValidOptions())
            {
                string optionKey = key + ".option." + option.optionId;
                AssertTranslated(english, optionKey + ".label", option.optionLabel);
                AssertTranslated(english, optionKey + ".preview", option.consequenceDescription);
                AssertTranslated(english, optionKey + ".result", option.resultContent);
                foreach (MapEventPossibleResultData result in option.possibleResults)
                {
                    if (result == null) continue;
                    string resultKey = optionKey + ".result." + result.resultId;
                    AssertTranslated(english, resultKey + ".preview", result.consequenceDescription);
                    AssertTranslated(english, resultKey + ".result", result.resultContent);
                }
            }
        }

        foreach (string guid in AssetDatabase.FindAssets("t:EnemySkill_SO", new[] { "Assets/Data" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var data = AssetDatabase.LoadAssetAtPath<EnemySkill_SO>(path);
            Assert.IsNotEmpty(data.localizationKey, path);
            AssertTranslated(english, data.localizationKey + ".name", data.skillName);
            AssertTranslated(english, data.localizationKey + ".description", data.description);
        }
    }

    private static StringTable LoadTable(string code)
    {
        var table = AssetDatabase.LoadAssetAtPath<StringTable>($"Assets/Localization/Tables/DiceLand_{code}.asset");
        Assert.IsNotNull(table, code);
        return table;
    }

    private static void AssertTranslated(StringTable table, string key, string source)
    {
        if (string.IsNullOrWhiteSpace(source)) return;
        Assert.IsNotEmpty(table.GetEntry(key)?.Value, key);
    }

    private static string[] FormatArguments(string value) =>
        Regex.Matches(value, @"(?<!\{)\{([a-zA-Z][a-zA-Z0-9_]*)\b").Cast<Match>().Select(match => match.Groups[1].Value).Distinct().ToArray();

    [Test]
    public void ResourceIconUsesTheTokenIdWithEnglishText()
    {
        string result = ResourceKeywordText.Format("Earn [[DL_Money|gold]] and [[DL_Status_Burn|Burn]].");
        StringAssert.Contains("<sprite name=\"DL_Money\"", result);
        StringAssert.Contains("<sprite name=\"DL_Status_Burn\"", result);
        StringAssert.Contains("gold</color>", result);
        StringAssert.DoesNotContain("[[", result);
    }
}
