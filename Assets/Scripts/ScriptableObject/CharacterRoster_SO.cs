using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CharacterRoster", menuName = "Characters/CharacterRoster")]
public class CharacterRoster_SO : ScriptableObject
{
    [SerializeField] private List<CharacterData_SO> characters = new List<CharacterData_SO>();

    public IReadOnlyList<CharacterData_SO> Characters => characters;

    public List<CharacterData_SO> GetUnownedCharacters(RunState runState, int maxPartyMembers)
    {
        List<CharacterData_SO> result = new List<CharacterData_SO>();
        if (characters == null || characters.Count == 0 || runState == null)
            return result;

        if (maxPartyMembers > 0 && runState.GetPartyMemberCount() >= maxPartyMembers)
            return result;

        for (int i = 0; i < characters.Count; i++)
        {
            CharacterData_SO character = characters[i];
            if (character == null || runState.HasCharacter(character))
                continue;

            result.Add(character);
        }

        return result;
    }

    public List<CharacterData_SO> GetRandomUnownedCharacters(RunState runState, int maxPartyMembers, int count, System.Random rng)
    {
        List<CharacterData_SO> pool = GetUnownedCharacters(runState, maxPartyMembers);
        List<CharacterData_SO> result = new List<CharacterData_SO>();
        if (pool.Count == 0 || count <= 0)
            return result;

        while (result.Count < count && pool.Count > 0)
        {
            int index = rng != null ? rng.Next(0, pool.Count) : UnityEngine.Random.Range(0, pool.Count);
            result.Add(pool[index]);
            pool.RemoveAt(index);
        }

        return result;
    }

    private void OnValidate()
    {
        if (characters == null)
            characters = new List<CharacterData_SO>();
    }
}
