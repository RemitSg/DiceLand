using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Read-only catalog formatting. Actual descriptions are produced by the formal skill overlay.
public sealed class AgentBattleAssetReader : IDisposable
{
    private Scene preview;
    private GameObject root;
    private BattleSkillInfoOverlay formatter;
    private readonly UnityEngine.Random.State outsideRandom = UnityEngine.Random.state;
    private readonly bool inputSuppressed = SpeedButton.InputSuppressed;
    private readonly bool combatLocked = SpeedButton.CombatLocked;
    private bool disposed;

    public AgentBattleAssetReader()
    {
        try
        {
            MethodInfo create = typeof(EditorSceneManager).GetMethod("NewPreviewScene", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                null, Type.EmptyTypes, null);
            if (create == null) throw new NotSupportedException("This engine does not expose the preview-scene catalog reader.");
            preview = (Scene)create.Invoke(null, null);
            root = new GameObject("AgentCatalogFormatter") { hideFlags = HideFlags.HideAndDontSave };
            root.SetActive(false);
            SceneManager.MoveGameObjectToScene(root, preview);
            // Keeping the component inactive avoids Awake/EnsureUi and any UI construction.
            formatter = root.AddComponent<BattleSkillInfoOverlay>();
        }
        catch { Dispose(); throw; }
    }

    public AgentBattleWeaponDetails Weapon(Weapon_SO weapon)
    {
        return new AgentBattleWeaponDetails
        {
            id = AssetId(weapon), name = weapon.weaponName, description = weapon.description,
            weaponType = weapon.weaponType.ToString(), rarity = weapon.rarity.ToString(), speedButtonCount = weapon.speedButtonCount,
            fallbackMinPower = weapon.minPower, fallbackMaxPower = weapon.maxPower,
            skills = (weapon.skillPool ?? Array.Empty<EnemySkill_SO>()).Select((skill, index) => Skill(skill, index, true, formatter)).ToArray()
        };
    }

    public static AgentBattleSkill Skill(EnemySkill_SO skill, int index, bool isAlly, BattleSkillInfoOverlay formatter)
    {
        if (skill == null) return null;
        return new AgentBattleSkill
        {
            index = index, id = AssetId(skill), name = skill.skillName,
            description = System.Text.RegularExpressions.Regex.Replace(formatter.DescribeSkillRules(skill, isAlly), "<[^>]+>", ""),
            energyCost = skill.energyCost,
            dice = Enumerable.Range(0, Mathf.Max(0, skill.diceCount)).Select(i =>
            {
                var range = skill.GetDicePowerRange(i);
                return new AgentBattleDie { min = range.minPower, max = range.maxPower, category = skill.GetDiceSkillCategory(i).ToString() };
            }).ToArray()
        };
    }

    public static AgentBattleBlueprintDetails Blueprint(CraftingBlueprint_SO blueprint)
    {
        return new AgentBattleBlueprintDetails
        {
            id = AssetId(blueprint), name = blueprint.DisplayName, description = blueprint.description,
            rarity = blueprint.rarity.ToString(), weapon = AssetId(blueprint.targetWeapon),
            materials = RunCraftingService.GetRequirements(blueprint).Select(r => new AgentBattleIngredient
            { id = AssetId(r.lootItem), name = r.lootItem.lootName, amount = r.amount }).ToArray()
        };
    }

    private static string AssetId(UnityEngine.Object value) => value != null ? AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(value)) : "";

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        try
        {
            if (root != null) UnityEngine.Object.DestroyImmediate(root);
            root = null; formatter = null;
            if (preview.IsValid() && preview.isLoaded) EditorSceneManager.ClosePreviewScene(preview);
        }
        finally
        {
            UnityEngine.Random.state = outsideRandom;
            // The formal overlay's destruction callback affects these globals; preserve their caller-owned state.
            SpeedButton.InputSuppressed = inputSuppressed; SpeedButton.CombatLocked = combatLocked;
        }
    }
}
