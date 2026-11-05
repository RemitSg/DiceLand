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
    public const string CharaId = "character.chara";
    public const string MollyId = "character.molly";

    public string characterId;
    public string legacySaveId;
    public string characterName;

    public string CharacterId => string.IsNullOrWhiteSpace(characterId) ? LegacySaveId : characterId;
    public string LegacySaveId => !string.IsNullOrWhiteSpace(legacySaveId) ? legacySaveId
        : !string.IsNullOrWhiteSpace(characterName) ? characterName : name;

    public bool MatchesId(string id)
    {
        return !string.IsNullOrWhiteSpace(id) && (string.Equals(CharacterId, id, System.StringComparison.OrdinalIgnoreCase)
            || string.Equals(LegacySaveId, id, System.StringComparison.OrdinalIgnoreCase));
    }
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
