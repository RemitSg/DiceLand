using System;
using System.Collections.Generic;
using UnityEngine;

public enum Rarity
{
    Common,
    Uncommon,
    Rare,
    Epic,
    Legendary
}

public enum WeaponType
{
    Sword,
    Axe,
    Bow,
    Wand,
    Dagger
}

[CreateAssetMenu(fileName = "New WeaponData", menuName = "Equipments/WeaponData")]
public class Weapon_SO : ScriptableObject
{
    [Header("Base Info")]

    public int id;
    public string saveId;
    public string legacySaveId;
    public string weaponName;
    private const string FusionSuffix = " · 融合";
    public string LocalizedName => IsFused ? FusedDisplayName : Localize("name", weaponName);
    public string FusedDisplayName => BaseWeapon.LocalizedName + (GameText.LanguageCode == GameText.Chinese ? FusionSuffix : " · Fusion");
    public string LocalizedDescription => Localize("description", description);

    private string Localize(string field, string source) => id > 0
        ? GameText.Content("weapon", id, field, source)
        : GameText.Content(saveId, field, source);

    [TextArea(2, 4)]
    public string description;

    public Rarity rarity;
    public WeaponType weaponType;
    public Sprite icon;
    public GameObject weaponPrefab;

    [Header("Battle Speed")]
    [Min(1)]
    [Tooltip("Base speed button count granted to an ally while this weapon is equipped.")]
    public int speedButtonCount = 1;

    [Header("Legacy Fallback Power")]
    [Tooltip("当武器未配置技能时，友方会回退到这个单骰威力范围")]
    public int minPower = 1;

    [Tooltip("当武器未配置技能时，友方会回退到这个单骰威力范围")]
    public int maxPower = 3;

    [Header("Skills")]
    [Tooltip("武器技能列表。当前战斗默认使用第一个技能")]
    public EnemySkill_SO[] skillPool;

    [Min(1)]
    [Tooltip("这把武器作为主体可融合的次数。普通武器为 1。")]
    public int maxFusionCount = 1;

    private Weapon_SO fusionBase;
    public Weapon_SO BaseWeapon => fusionBase != null ? fusionBase : this;
    public string FusionInstanceId { get; private set; }
    public int FusionCount { get; private set; }
    public int FusionLimit => Mathf.Max(1, BaseWeapon.maxFusionCount);
    public IReadOnlyList<InheritedWeaponSkill> InheritedSkills { get; private set; } = Array.Empty<InheritedWeaponSkill>();
    public bool IsFused => !string.IsNullOrEmpty(FusionInstanceId);

    // All existing equipment and battle consumers can use the fused weapon as usual.
    // A new variant is created for each fusion, so snapshots can share its reference.
    internal static Weapon_SO CreateFusedCopy(Weapon_SO template, List<InheritedWeaponSkill> inherited, string instanceId, int fusionCount)
    {
        Weapon_SO baseWeapon = template.BaseWeapon;
        Weapon_SO copy = Instantiate(baseWeapon);
        copy.name = baseWeapon.name;
        copy.weaponName = baseWeapon.weaponName + FusionSuffix;
        copy.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
        copy.fusionBase = baseWeapon;
        copy.FusionInstanceId = instanceId;
        copy.FusionCount = fusionCount;
        copy.InheritedSkills = new List<InheritedWeaponSkill>(inherited).AsReadOnly();
        var skills = new List<EnemySkill_SO>(baseWeapon.skillPool ?? Array.Empty<EnemySkill_SO>());
        foreach (InheritedWeaponSkill entry in inherited)
            skills.Add(entry.Skill);
        copy.skillPool = skills.ToArray();
        return copy;
    }

    public EnemySkill_SO GetDefaultSkill()
    {
        if (skillPool == null || skillPool.Length == 0)
            return null;

        return skillPool[0];
    }

    public EnemySkill_SO GetSkill(int index)
    {
        if (skillPool == null || skillPool.Length == 0)
            return null;

        int safeIndex = Mathf.Clamp(index, 0, skillPool.Length - 1);
        return skillPool[safeIndex];
    }

    private void OnValidate()
    {
        if (minPower < 0)
            minPower = 0;
        if (maxPower < minPower)
            maxPower = minPower;
        if (speedButtonCount < 1)
            speedButtonCount = 1;
    }
}
