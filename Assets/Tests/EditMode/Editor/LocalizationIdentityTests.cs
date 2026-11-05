using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

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
    public void ResourceIconUsesTheTokenIdWithEnglishText()
    {
        string result = ResourceKeywordText.Format("Earn [[DL_Money|gold]] and [[DL_Status_Burn|Burn]].");
        StringAssert.Contains("<sprite name=\"DL_Money\"", result);
        StringAssert.Contains("<sprite name=\"DL_Status_Burn\"", result);
        StringAssert.Contains("gold</color>", result);
        StringAssert.DoesNotContain("[[", result);
    }
}
