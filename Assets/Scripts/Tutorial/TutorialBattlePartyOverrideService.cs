using System;
using System.Collections.Generic;
using UnityEngine;

public static class TutorialBattlePartyOverrideService
{
    private const string MollyMemberId = "TutorialGuest_Molly";

    public static void ApplyOverrides(BattleEncounterData encounterData, CharacterData_SO battle02MollyVisualSource = null)
    {
        if (encounterData == null || !encounterData.isTutorialEncounter)
            return;

        switch (encounterData.tutorialBattleStageId)
        {
            case TutorialBattleStageId.Battle02:
                EnsureBattle02MollyGuest(encounterData.partyMembers, battle02MollyVisualSource);
                break;
        }
    }

    private static void EnsureBattle02MollyGuest(List<RunPartyMemberState> partyMembers, CharacterData_SO visualSource)
    {
        if (partyMembers == null)
            return;

        for (int i = 0; i < partyMembers.Count; i++)
        {
            RunPartyMemberState member = partyMembers[i];
            if (member == null)
                continue;

            if (member.isTemporaryBattleGuest && string.Equals(member.memberId, MollyMemberId, StringComparison.OrdinalIgnoreCase))
                return;

            if (member.characterData != null
                && string.Equals(member.characterData.characterName, "Molly", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
        }

        partyMembers.Insert(0, CreateBattle02MollyGuest(visualSource));
    }

    private static RunPartyMemberState CreateBattle02MollyGuest(CharacterData_SO visualSource)
    {
        EnemySkill_SO lightAttack = ScriptableObject.CreateInstance<EnemySkill_SO>();
        PrepareRuntimeObject(lightAttack, "Tutorial_Molly_LightAttack");
        lightAttack.skillName = "轻击";
        lightAttack.description = "Molly 的基础攻击。";
        lightAttack.minPower = 1;
        lightAttack.maxPower = 2;
        lightAttack.diceCount = 1;
        lightAttack.skillCategory = EnemySkill_SO.SkillCategory.Attack;

        Weapon_SO weapon = ScriptableObject.CreateInstance<Weapon_SO>();
        PrepareRuntimeObject(weapon, "Tutorial_Molly_Weapon");
        weapon.weaponName = "Molly";
        weapon.speedButtonCount = 1;
        weapon.skillPool = new[] { lightAttack };

        CharacterData_SO character = ScriptableObject.CreateInstance<CharacterData_SO>();
        PrepareRuntimeObject(character, "Tutorial_Molly_Character");
        character.characterName = "Molly";
        character.basicDescription = visualSource != null ? visualSource.basicDescription : string.Empty;
        character.baseHP = 16;
        character.minSpeed = 1;
        character.maxSpeed = 1;
        character.initialWeaponData = weapon;
        if (visualSource != null)
        {
            character.compatibleWeaponType = visualSource.compatibleWeaponType;
            character.icon = visualSource.icon;
            character.battleSprite = visualSource.battleSprite;
            character.characterPrefab = visualSource.characterPrefab;
        }

        return new RunPartyMemberState
        {
            memberId = MollyMemberId,
            characterData = character,
            weaponData = weapon,
            level = 1,
            experience = 0,
            preCalamityMaxHP = 16,
            maxHP = 16,
            currentHP = 1,
            isTemporaryBattleGuest = true
        };
    }

    private static void PrepareRuntimeObject(UnityEngine.Object target, string name)
    {
        if (target == null)
            return;

        target.name = name;
        target.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
    }
}
