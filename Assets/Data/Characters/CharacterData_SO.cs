using UnityEngine;

[CreateAssetMenu(fileName = "New CharacterData", menuName = "Characters/CharacterData")]
public class CharacterData_SO : ScriptableObject
{
    public string characterName;
    [TextArea(2, 5)]
    public string basicDescription;
    public int baseHP;
    public int minSpeed;
    public int maxSpeed;
    public WeaponType compatibleWeaponType;
    public Weapon_SO initialWeaponData;
    public Sprite icon;
    public Sprite battleSprite;
    public GameObject characterPrefab;
}
