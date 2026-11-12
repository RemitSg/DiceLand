using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;
using Availability = RunWeaponFusionService.Availability;

public sealed class RunWeaponFusionServiceTests
{
    private readonly List<ScriptableObject> temporary = new List<ScriptableObject>();
    private RunState state;
    private Weapon_SO subject, donor;
    private EnemySkill_SO original, first, second;

    [SetUp]
    public void SetUp()
    {
        original = MakeSkill("Original");
        first = MakeSkill("First");
        second = MakeSkill("Second");
        subject = MakeWeapon("Base Sword", original);
        donor = MakeWeapon("Donor Sword", first, second);
        state = new RunState { money = 126 };
        state.AddWeapon(subject);
        state.AddWeapon(donor);
    }

    [TearDown]
    public void TearDown()
    {
        foreach (ScriptableObject item in temporary)
            if (item != null) Object.DestroyImmediate(item);
        temporary.Clear();
    }

    [Test]
    public void FusionSpendsOneCopyFromEachStackAndUpgradesTheEquippedSubject()
    {
        state.AddWeapon(subject);
        state.AddWeapon(donor);
        var subjectOwner = Equip(subject, "Subject owner");
        var donorOwner = Equip(donor, "Donor owner");

        RunWeaponState fused = Fuse(Stack(subject), Stack(donor), second);

        Assert.AreEqual(76, state.money);
        Assert.AreEqual(1, state.GetWeaponAmount(subject));
        Assert.AreEqual(1, state.GetWeaponAmount(donor));
        Assert.AreEqual(1, fused.amount);
        Assert.AreSame(fused.weaponData, subjectOwner.weaponData);
        Assert.AreSame(donor, donorOwner.weaponData, "An unequipped spare should be consumed first.");
        Assert.AreSame(subject, fused.weaponData.BaseWeapon);
        Assert.AreEqual("Base Sword · 融合", fused.weaponData.weaponName);
        CollectionAssert.AreEqual(new[] { original, second }, fused.weaponData.skillPool);
        CollectionAssert.AreEqual(new[] { original }, subject.skillPool);
        CollectionAssert.Contains(MerchantOfferExclusionService.BuildOwnedWeaponSet(state), subject);
    }

    [Test]
    public void ConsumingTheLastEquippedDonorClearsThatEquipment()
    {
        RunPartyMemberState owner = Equip(donor, "Donor owner");
        RunWeaponState originalDonor = Stack(donor);
        Assert.AreSame(owner, RunWeaponFusionService.GetDonorOwnerToUnequip(state, Stack(subject), originalDonor));

        Fuse(Stack(subject), originalDonor, first);

        Assert.IsNull(owner.weaponData);
        Assert.AreEqual(0, state.GetWeaponAmount(donor));
        Assert.IsFalse(RunWeaponFusionService.Owns(state, originalDonor));
    }

    [Test]
    public void InvalidInputsAndInsufficientGoldLeaveTheRunUntouched()
    {
        var wrongType = MakeWeapon("Staff", first);
        wrongType.weaponType = WeaponType.Wand;
        state.AddWeapon(wrongType);
        var owner = Equip(subject, "Subject owner");
        AssertRejected(Stack(subject), Stack(subject), first, Availability.InvalidWeapons);
        AssertRejected(Stack(subject), Stack(wrongType), first, Availability.InvalidWeapons);
        AssertRejected(Stack(subject), Stack(donor), MakeSkill("Not on donor"), Availability.InvalidSkill);
        AssertRejected(Stack(subject), Stack(donor), null, Availability.SelectSkill);
        donor.skillPool = new[] { original, first };
        AssertRejected(Stack(subject), Stack(donor), original, Availability.AlreadyKnown);
        state.money = 49;
        AssertRejected(Stack(subject), Stack(donor), first, Availability.InsufficientGold);
        Assert.AreSame(subject, owner.weaponData);
    }

    [Test]
    public void DefaultLimitIsPerCopyAndRejectsASecondFusion()
    {
        state.AddWeapon(subject);
        state.AddWeapon(donor);
        RunWeaponState fused = Fuse(Stack(subject), Stack(donor), first);

        Assert.AreEqual(1, fused.weaponData.FusionCount);
        Assert.AreEqual(1, fused.weaponData.FusionLimit);
        AssertRejected(fused, Stack(donor), second, Availability.FusionLimitReached);
        Assert.AreEqual(Availability.Ready, RunWeaponFusionService.Check(state, Stack(subject), Stack(donor), second));
    }

    [Test]
    public void SpecialLimitAllowsAnotherSkillFromAnAlreadyFusedDonor()
    {
        subject.maxFusionCount = 2;
        state.money = 150;
        var otherSkill = MakeSkill("Inherited from another weapon");
        var otherDonor = MakeWeapon("Other Sword", otherSkill);
        state.AddWeapon(otherDonor);
        state.AddWeapon(donor);
        RunWeaponState fusedDonor = Fuse(Stack(donor), Stack(otherDonor), otherSkill);
        RunWeaponState once = Fuse(Stack(subject), Stack(donor), second);
        RunWeaponState twice = Fuse(once, fusedDonor, otherSkill);

        Assert.AreEqual(0, state.money);
        Assert.AreEqual(2, twice.weaponData.FusionCount);
        Assert.AreEqual(once.weaponData.FusionInstanceId, twice.weaponData.FusionInstanceId);
        Assert.AreEqual("Base Sword · 融合", twice.weaponData.weaponName);
        CollectionAssert.AreEqual(new[] { original, second, otherSkill }, twice.weaponData.skillPool);
        Assert.AreSame(otherDonor, twice.weaponData.InheritedSkills[1].SourceWeapon);
        Assert.AreEqual(Availability.FusionLimitReached, RunWeaponFusionService.Check(state, twice, null, null));
    }

    [Test]
    public void SaveAndBattleKeepTheTwoFusedCopiesDistinctAndRestoreTheEquippedOne()
    {
        state.AddWeapon(subject);
        state.AddWeapon(donor);
        Equip(subject, "Main");
        RunWeaponState equipped = Fuse(Stack(subject), Stack(donor), first);
        RunWeaponState spare = Fuse(Stack(subject), Stack(donor), second);
        var catalog = new MainMenuSaveAssetCatalog();
        catalog.Add(equipped.weaponData);
        catalog.Add(spare.weaponData);
        Assert.AreSame(donor, catalog.ResolveWeapon(donor.saveId), "The consumed donor must resolve as a skill source.");
        Assert.AreSame(subject, catalog.ResolveWeapon(subject.saveId));

        MethodInfo serialize = typeof(MainMenuRunSaveSystem).GetMethod("SerializeRunState", BindingFlags.Static | BindingFlags.NonPublic);
        string json = JsonUtility.ToJson(serialize.Invoke(null, new object[] { state }));
        // A patch can reorder the source skill pool without changing the inherited skill.
        donor.skillPool = new[] { second, first };
        Type savedType = typeof(MainMenuRunSaveSystem).Assembly.GetType("SavedRunState");
        MethodInfo deserialize = typeof(MainMenuRunSaveSystem).GetMethod("DeserializeRunState", BindingFlags.Static | BindingFlags.NonPublic);
        var loaded = (RunState)deserialize.Invoke(null, new[] { JsonUtility.FromJson(json, savedType), catalog });
        temporary.AddRange(loaded.weaponInventory.Select(stack => stack.weaponData).Where(weapon => weapon.IsFused));
        Weapon_SO loadedEquipped = loaded.partyMembers.Single().weaponData;
        Assert.AreEqual(26, loaded.money);
        Assert.AreEqual(2, loaded.weaponInventory.Count);
        Assert.AreSame(loadedEquipped, loaded.weaponInventory.Single(stack => stack.weaponData.FusionInstanceId == equipped.weaponData.FusionInstanceId).weaponData);
        CollectionAssert.AreEqual(new[] { original, first }, loadedEquipped.skillPool);
        CollectionAssert.AreEqual(new[] { original, second }, loaded.weaponInventory.Single(stack => stack.weaponData.FusionInstanceId == spare.weaponData.FusionInstanceId).weaponData.skillPool);
        Assert.AreEqual(1, loadedEquipped.FusionCount);

        var actor = new GameObject("Fused weapon battle actor");
        actor.SetActive(false);
        try
        {
            BattleAllyController battle = actor.AddComponent<BattleAllyController>();
            battle.ApplyRuntimeState(loaded.partyMembers.Single());
            CollectionAssert.AreEqual(new[] { original, first }, battle.GetAvailableSkills());
        }
        finally { Object.DestroyImmediate(actor); }
    }

    private RunWeaponState Stack(Weapon_SO weapon) => state.weaponInventory.Single(stack => stack.weaponData == weapon);

    private RunPartyMemberState Equip(Weapon_SO weapon, string id)
    {
        var member = new RunPartyMemberState { weaponData = weapon, memberId = id, currentHP = 20, maxHP = 20 };
        state.partyMembers.Add(member);
        return member;
    }

    private RunWeaponState Fuse(RunWeaponState primary, RunWeaponState material, EnemySkill_SO skill)
    {
        Assert.AreEqual(Availability.Ready, RunWeaponFusionService.TryFuse(state, primary, material, skill, out RunWeaponState result));
        temporary.Add(result.weaponData);
        return result;
    }

    private void AssertRejected(RunWeaponState primary, RunWeaponState material, EnemySkill_SO skill, Availability expected)
    {
        string before = JsonUtility.ToJson(state);
        Assert.AreEqual(expected, RunWeaponFusionService.TryFuse(state, primary, material, skill, out RunWeaponState result));
        Assert.IsNull(result);
        Assert.AreEqual(before, JsonUtility.ToJson(state));
    }

    private EnemySkill_SO MakeSkill(string name)
    {
        var skill = ScriptableObject.CreateInstance<EnemySkill_SO>();
        temporary.Add(skill);
        skill.name = skill.skillName = name;
        return skill;
    }

    private Weapon_SO MakeWeapon(string name, params EnemySkill_SO[] skills)
    {
        var weapon = ScriptableObject.CreateInstance<Weapon_SO>();
        temporary.Add(weapon);
        weapon.name = weapon.saveId = weapon.weaponName = name;
        weapon.weaponType = WeaponType.Sword;
        weapon.skillPool = skills;
        return weapon;
    }
}
