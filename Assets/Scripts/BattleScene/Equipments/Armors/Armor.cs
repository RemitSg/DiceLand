using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ArmorType
{
    Helmet,
    Chestplate,
    Leggings,
    Boots,
    Shield
}

public class Armor : MonoBehaviour
{
    public string armorName;
    public string description;
    public int defense;
    public Rarity rarity;
    public ArmorType armorType;
    public Sprite icon;
}
