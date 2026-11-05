using UnityEngine;

[System.Serializable]
public struct CharacterAttackPose
{
    public Sprite sprite;
    [Tooltip("Weapon grip on the sprite, in normalized coordinates from the bottom-left corner.")]
    public Vector2 gripPosition;
    [Tooltip("Weapon grip rotation in degrees around the sprite's local Z axis.")]
    public float gripAngle;
}

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
    public CharacterAttackPose attackPose;
}
