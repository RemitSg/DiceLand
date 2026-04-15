using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New WeaponCatalog", menuName = "Equipments/WeaponCatalog")]
public class WeaponCatalog_SO : ScriptableObject
{
    [Serializable]
    public class WeaponEntry
    {
        public Weapon_SO weapon;
        [Min(0)] public int unlockWeight = 1;
        public bool starterAvailable = true;
        [TextArea(1, 3)] public string note;
    }

    [Header("Weapons")]
    [SerializeField] private List<WeaponEntry> weapons = new List<WeaponEntry>();

    public IReadOnlyList<WeaponEntry> Weapons => weapons;

    public Weapon_SO GetWeaponById(int id)
    {
        for (int i = 0; i < weapons.Count; i++)
        {
            WeaponEntry entry = weapons[i];
            if (entry != null && entry.weapon != null && entry.weapon.id == id)
                return entry.weapon;
        }

        return null;
    }

    public IReadOnlyList<Weapon_SO> GetWeaponsByRarity(Rarity rarity)
    {
        List<Weapon_SO> result = new List<Weapon_SO>();
        for (int i = 0; i < weapons.Count; i++)
        {
            WeaponEntry entry = weapons[i];
            if (entry == null || entry.weapon == null || entry.weapon.rarity != rarity)
                continue;

            result.Add(entry.weapon);
        }

        return result;
    }

    public IReadOnlyList<Weapon_SO> GetWeaponsByType(WeaponType weaponType)
    {
        List<Weapon_SO> result = new List<Weapon_SO>();
        for (int i = 0; i < weapons.Count; i++)
        {
            WeaponEntry entry = weapons[i];
            if (entry == null || entry.weapon == null || entry.weapon.weaponType != weaponType)
                continue;

            result.Add(entry.weapon);
        }

        return result;
    }

    public Weapon_SO GetRandomStarterWeapon(System.Random rng = null)
    {
        List<WeaponEntry> candidates = new List<WeaponEntry>();
        for (int i = 0; i < weapons.Count; i++)
        {
            WeaponEntry entry = weapons[i];
            if (entry == null || entry.weapon == null || !entry.starterAvailable)
                continue;

            int weight = Mathf.Max(1, entry.unlockWeight);
            for (int j = 0; j < weight; j++)
                candidates.Add(entry);
        }

        if (candidates.Count == 0)
            return null;

        System.Random effectiveRng = rng ?? new System.Random();
        return candidates[effectiveRng.Next(0, candidates.Count)].weapon;
    }

    private void OnValidate()
    {
        if (weapons == null)
        {
            weapons = new List<WeaponEntry>();
            return;
        }

        for (int i = 0; i < weapons.Count; i++)
        {
            WeaponEntry entry = weapons[i];
            if (entry == null)
            {
                weapons[i] = new WeaponEntry();
                continue;
            }

            entry.unlockWeight = Mathf.Max(1, entry.unlockWeight);
        }
    }
}
