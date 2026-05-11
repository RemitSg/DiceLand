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
    public string weaponName;

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
