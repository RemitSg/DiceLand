using System;
using System.Collections.Generic;

// The source is always a catalog weapon, even if the donor itself was fused.
public readonly struct InheritedWeaponSkill
{
    public Weapon_SO SourceWeapon { get; }
    public EnemySkill_SO Skill { get; }

    public InheritedWeaponSkill(Weapon_SO sourceWeapon, EnemySkill_SO skill)
    {
        SourceWeapon = sourceWeapon;
        Skill = skill;
    }
}

public static class RunWeaponFusionService
{
    public const int Cost = 50;

    public enum Availability
    {
        Ready,
        InvalidWeapons,
        FusionLimitReached,
        SelectSkill,
        InvalidSkill,
        AlreadyKnown,
        InsufficientGold
    }

    public static bool Owns(RunState state, RunWeaponState weapon)
        => weapon?.weaponData != null && weapon.amount > 0 && state?.weaponInventory != null &&
           state.weaponInventory.Contains(weapon);

    public static bool CanPair(RunState state, RunWeaponState subject, RunWeaponState donor)
        => Owns(state, subject) && Owns(state, donor) &&
           subject.weaponData.weaponType == donor.weaponData.weaponType &&
           (subject != donor || subject.amount >= 2);

    public static Availability Check(RunState state, RunWeaponState subject, RunWeaponState donor, EnemySkill_SO skill)
    {
        if (Owns(state, subject) && subject.weaponData.FusionCount >= subject.weaponData.FusionLimit)
            return Availability.FusionLimitReached;
        if (!CanPair(state, subject, donor)) return Availability.InvalidWeapons;
        if (skill == null) return Availability.SelectSkill;
        if (!HasSkill(donor.weaponData, skill) || !TryFindSkillSource(donor.weaponData, skill, out _))
            return Availability.InvalidSkill;
        if (HasSkill(subject.weaponData, skill)) return Availability.AlreadyKnown;
        return state.money >= Cost ? Availability.Ready : Availability.InsufficientGold;
    }

    public static RunPartyMemberState GetDonorOwnerToUnequip(RunState state, RunWeaponState subject, RunWeaponState donor)
    {
        if (!CanPair(state, subject, donor) || state.partyMembers == null)
            return null;

        // Equipped copies are included in the inventory. Prefer to spend a spare.
        Weapon_SO donorWeapon = donor.weaponData;
        bool sameWeapon = subject.weaponData == donorWeapon;
        RunPartyMemberState subjectOwner = sameWeapon ? state.GetEquippedMember(donorWeapon) : null;
        int remaining = state.GetWeaponAmount(donorWeapon) - (sameWeapon ? 2 : 1);
        foreach (RunPartyMemberState member in state.partyMembers)
            if (member != null && member != subjectOwner && member.weaponData == donorWeapon && remaining-- <= 0)
                return member;
        return null;
    }

    public static Availability TryFuse(RunState state, RunWeaponState subject, RunWeaponState donor, EnemySkill_SO skill,
        out RunWeaponState result)
    {
        result = null;
        Availability availability = Check(state, subject, donor, skill);
        if (availability != Availability.Ready) return availability;

        TryFindSkillSource(donor.weaponData, skill, out InheritedWeaponSkill source);
        var inherited = new List<InheritedWeaponSkill>(subject.weaponData.InheritedSkills) { source };
        RunPartyMemberState subjectOwner = state.GetEquippedMember(subject.weaponData);
        RunPartyMemberState donorOwner = GetDonorOwnerToUnequip(state, subject, donor);
        int resultIndex = state.weaponInventory.IndexOf(subject);
        Weapon_SO subjectWeapon = subject.weaponData;
        string instanceId = subjectWeapon.FusionInstanceId ?? Guid.NewGuid().ToString("N");
        Weapon_SO fused = Weapon_SO.CreateFusedCopy(subjectWeapon, inherited, instanceId, subjectWeapon.FusionCount + 1);

        // Validate and allocate before committing. No user callbacks run during the mutation.
        if (!state.TrySpendMoney(Cost))
        {
            if (UnityEngine.Application.isPlaying) UnityEngine.Object.Destroy(fused);
            else UnityEngine.Object.DestroyImmediate(fused);
            return Availability.InsufficientGold;
        }
        subject.amount--;
        donor.amount--;
        if (subject.amount == 0) state.weaponInventory.Remove(subject);
        if (donor != subject && donor.amount == 0)
        {
            if (state.weaponInventory.IndexOf(donor) < resultIndex) resultIndex--;
            state.weaponInventory.Remove(donor);
        }
        result = new RunWeaponState { weaponData = fused, amount = 1 };
        state.weaponInventory.Insert(Math.Min(resultIndex, state.weaponInventory.Count), result);
        if (subjectOwner != null) subjectOwner.weaponData = fused;
        if (donorOwner != null) donorOwner.weaponData = null;
        return Availability.Ready;
    }

    public static bool HasSkill(Weapon_SO weapon, EnemySkill_SO skill)
        => skill != null && weapon != null && weapon.skillPool != null && Array.IndexOf(weapon.skillPool, skill) >= 0;

    private static bool TryFindSkillSource(Weapon_SO weapon, EnemySkill_SO skill, out InheritedWeaponSkill source)
    {
        Weapon_SO baseWeapon = weapon.BaseWeapon;
        if (HasSkill(baseWeapon, skill))
        {
            source = new InheritedWeaponSkill(baseWeapon, skill);
            return true;
        }
        foreach (InheritedWeaponSkill entry in weapon.InheritedSkills)
            if (entry.Skill == skill && HasSkill(entry.SourceWeapon, skill))
            {
                source = entry;
                return true;
            }
        source = default;
        return false;
    }
}
