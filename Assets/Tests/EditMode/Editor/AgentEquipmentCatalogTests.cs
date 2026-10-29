using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class AgentEquipmentCatalogTests
{
    private AgentBattleSession battle;
    private AgentAdventureSession adventure;
    private bool input, locked;
    private UnityEngine.Random.State random;
    [SetUp] public void SetUp() { input = SpeedButton.InputSuppressed; locked = SpeedButton.CombatLocked; random = UnityEngine.Random.state; }
    [TearDown] public void TearDown()
    {
        battle?.Dispose(); battle = null; adventure?.Dispose(); adventure = null;
        SpeedButton.InputSuppressed = input; SpeedButton.CombatLocked = locked; UnityEngine.Random.state = random;
    }

    [Test] public void CatalogLinksUnownedBlueprintToWeaponSkillsSlotsAndCompatibleCharacter()
    {
        AgentBattleCatalogue catalog = AgentBattleSession.Catalogue();
        var blueprint = catalog.blueprintDetails.Single(b => b.name == "刹影之卷");
        var weapon = catalog.weaponDetails.Single(w => w.id == blueprint.weapon);
        Assert.AreEqual("刹影短匕", weapon.name); Assert.AreEqual(2, weapon.speedButtonCount);
        Assert.AreEqual("Dagger", weapon.weaponType); Assert.AreEqual("Rare", weapon.rarity);
        Assert.AreEqual(4, weapon.skills.Length); Assert.IsTrue(weapon.skills.All(s => s.dice.Length > 0 && !string.IsNullOrEmpty(s.description)));
        Assert.AreEqual(3, blueprint.materials.Length);
        Assert.AreEqual(3, blueprint.materials.Single(m => m.name == "旋速液").amount);
        var molly = catalog.characterDetails.Single(c => c.name == "Molly");
        Assert.AreEqual(weapon.weaponType, molly.weaponType);
        Assert.AreEqual("Dagger", catalog.weaponDetails.Single(w => w.id == molly.initialWeapon).weaponType);
    }

    [Test] public void CatalogPreservesConditionalSkillTextAndAuthoredDiceWithoutRichText()
    {
        var catalog = AgentBattleSession.Catalogue();
        var wand = catalog.weaponDetails.Single(w => w.name == "老旧法杖");
        var star = wand.skills.Single(s => s.name == "星辰冲击");
        Assert.That(star.description, Does.Contain("至少拥有 2 层 蓄能"));
        Assert.That(star.description, Does.Not.Contain("<color"));
        Assert.AreEqual(1, star.energyCost); Assert.AreEqual(2, star.dice.Length);
        Assert.AreEqual(2, star.dice[0].min); Assert.AreEqual(5, star.dice[0].max);
        Assert.AreEqual("Attack", star.dice[0].category);
    }

    [Test] public void BrowsingCatalogDoesNotCreateUiOrDirtyTheCallerSceneOrAdvanceRandom()
    {
        Scene scene = SceneManager.GetActiveScene(); bool dirty = scene.isDirty;
        int roots = scene.rootCount, sceneCount = SceneManager.sceneCount;
        int canvases = Resources.FindObjectsOfTypeAll<Canvas>().Length;
        int overlays = Resources.FindObjectsOfTypeAll<BattleSkillInfoOverlay>().Length;
        SpeedButton.InputSuppressed = true; SpeedButton.CombatLocked = true;
        string rng = JsonUtility.ToJson(UnityEngine.Random.state);
        string first = JsonUtility.ToJson(AgentBattleSession.Catalogue());
        Assert.AreEqual(first, JsonUtility.ToJson(AgentBattleSession.Catalogue()));
        Assert.AreEqual(scene, SceneManager.GetActiveScene()); Assert.AreEqual(dirty, scene.isDirty);
        Assert.AreEqual(roots, scene.rootCount); Assert.AreEqual(sceneCount, SceneManager.sceneCount);
        Assert.AreEqual(canvases, Resources.FindObjectsOfTypeAll<Canvas>().Length);
        Assert.AreEqual(overlays, Resources.FindObjectsOfTypeAll<BattleSkillInfoOverlay>().Length);
        Assert.IsTrue(SpeedButton.InputSuppressed); Assert.IsTrue(SpeedButton.CombatLocked);
        Assert.AreEqual(rng, JsonUtility.ToJson(UnityEngine.Random.state));
    }

    [Test] public void CatalogDuringBattleCannotChangeDecisionsRuntimeOrInputLocks()
    {
        var catalog = AgentBattleSession.Catalogue();
        battle = new AgentBattleSession(new AgentBattleRequest
        {
            party = new[] { new AgentBattlePartyMember { character = catalog.characters.Single(c => c.name == "Chara").id,
                weapon = catalog.weapons.Single(w => w.name == "新手木剑").id } },
            enemies = new[] { catalog.enemies.First(e => e.name.Contains("史莱姆")).id }
        });
        string before = JsonUtility.ToJson(battle.Observe()); var encounter = BattleEncounterRuntime.ActiveEncounter;
        SpeedButton.InputSuppressed = true;
        catalog = AgentBattleSession.Catalogue();
        Assert.AreEqual(before, JsonUtility.ToJson(battle.Observe())); Assert.AreSame(encounter, BattleEncounterRuntime.ActiveEncounter);
        Assert.IsTrue(SpeedButton.InputSuppressed);
    }

    [Test] public void CurrentForgeCostIsObservableBeforeOwningAnyBlueprintOrEnteringForge()
    {
        adventure = new AgentAdventureSession(new AgentAdventureRequest { seed = 1234 });
        Assert.IsEmpty(adventure.Observe().recipes); Assert.AreEqual(0, adventure.Observe().craftingCost);
        var map = (AgentAdventureMapRuntime)typeof(AgentAdventureSession).GetField("map", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(adventure);
        var calamity = ScriptableObject.CreateInstance<Calamity_SO>();
        try
        {
            calamity.builtInEffects.Add(new CalamityBuiltInEffectEntry { effectType = CalamityBuiltInEffectType.ForgeCostFlatModifier, effectAmount = 40 });
            map.Snapshot.runState.AddCalamity(calamity, 1);
            var observation = adventure.Observe();
            Assert.AreEqual("map", observation.phase); Assert.AreEqual(40, observation.craftingCost); Assert.IsEmpty(observation.recipes);
            Assert.AreEqual(JsonUtility.ToJson(observation), JsonUtility.ToJson(adventure.Observe()));
        }
        finally { UnityEngine.Object.DestroyImmediate(calamity); }
    }

    [Test] public void BattleShowsFormalStatusAdjustedRangesAndKeepsAuthoredDiceSeparate()
    {
        var catalog = AgentBattleSession.Catalogue();
        battle = new AgentBattleSession(new AgentBattleRequest
        {
            party = new[] { new AgentBattlePartyMember { character = catalog.characters.Single(c => c.name == "Chara").id,
                weapon = catalog.weapons.Single(w => w.name == "新手木剑").id } },
            enemies = new[] { catalog.enemies.First(e => e.name.Contains("史莱姆")).id }
        });
        var ally = Object.FindObjectsOfType<BattleAllyController>().Single();
        var enemy = Object.FindObjectsOfType<BattleEnemyController>().Single();
        var status = ScriptableObject.CreateInstance<BattleStatus_SO>();
        try
        {
            typeof(BattleStatus_SO).GetField("behaviorType", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(status, BattleStatus_SO.StatusBehaviorType.PowerBonusFlat);
            typeof(BattleStatus_SO).GetField("powerPerStack", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(status, 1);
            ally.AddStatus(status, "catalog-power", "Catalog Power", 2); enemy.AddStatus(status, "catalog-power", "Catalog Power", 2);
            var observed = battle.Observe();
            foreach (var actor in observed.actors)
            foreach (var slot in actor.slots)
            {
                var skill = actor.side == "ally" ? ally.GetSelectedSkillForButton(ally.SpeedButtons[slot.index]) : enemy.GetSelectedSkillForButton(enemy.SpeedButtons[slot.index]);
                if (skill == null) continue;
                var expected = actor.side == "ally" ? ally.GetDisplayedDicePowerRange(skill, 0) : enemy.GetEffectiveDisplayedDicePowerRange(skill, 0);
                Assert.AreEqual(expected.minPower, slot.selectedSkill.displayedDice[0].min);
                Assert.AreEqual(expected.maxPower, slot.selectedSkill.displayedDice[0].max);
                Assert.AreEqual(skill.GetDicePowerRange(0).minPower, slot.selectedSkill.dice[0].min);
                Assert.Greater(slot.selectedSkill.displayedDice[0].max, slot.selectedSkill.dice[0].max);
            }
            string rng = JsonUtility.ToJson(UnityEngine.Random.state), before = JsonUtility.ToJson(observed);
            Assert.AreEqual(before, JsonUtility.ToJson(battle.Observe())); Assert.AreEqual(rng, JsonUtility.ToJson(UnityEngine.Random.state));
        }
        finally { Object.DestroyImmediate(status); }
    }
}
