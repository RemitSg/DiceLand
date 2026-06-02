using NUnit.Framework;
using UnityEditor;

public class TodoWeaponAssetTests
{
    private const string HuiShiSwordPath = "Assets/Data/Weapons/008 晦誓剑/008 晦誓剑.asset";
    private const string HotAnvilBoardPath = "Assets/Data/Weapons/203 热砧板/203 热砧板.asset";
    private const string WeaponCatalogPath = "Assets/Data/Weapons/WeaponCatalog.asset";

    [Test]
    public void HuiShiSword_LoadsWithConfiguredDarknessSkillPackage()
    {
        Weapon_SO weapon = Load<Weapon_SO>(HuiShiSwordPath);

        Assert.AreEqual(8, weapon.id);
        Assert.AreEqual("晦誓剑", weapon.weaponName);
        Assert.AreEqual(Rarity.Rare, weapon.rarity);
        Assert.AreEqual(WeaponType.Sword, weapon.weaponType);
        Assert.AreEqual(1, weapon.speedButtonCount);
        Assert.AreEqual(4, weapon.skillPool.Length);

        EnemySkill_SO blind = weapon.skillPool[3];
        Assert.AreEqual("失明", blind.skillName);
        Assert.AreEqual(4, blind.energyCost);
        Assert.AreEqual(2, blind.diceCount);

        EnemySkill_SO.DiceEffectSettings firstDie = blind.GetDiceEffectSettings(0);
        Assert.AreEqual("stun", firstDie.statusEffects[0].statusData.StatusId);
        Assert.AreEqual(EnemySkill_SO.StatusApplyTiming.NextRoundStart, firstDie.statusEffects[0].applyTiming);
        Assert.AreEqual("darkness", firstDie.statusStackOperationEffects[0].statusData.StatusId);
        Assert.AreEqual(2, firstDie.statusStackOperationEffects[0].stackAmount);

        EnemySkill_SO.DiceEffectSettings secondDie = blind.GetDiceEffectSettings(1);
        Assert.AreEqual(3, secondDie.rangeModifiers[0].minPowerBonus);
        Assert.AreEqual("darkness", secondDie.rangeModifiers[0].targetStatusData.StatusId);
    }

    [Test]
    public void HotAnvilBoard_LoadsWithBurnSpreadAndSelfBurnSkills()
    {
        Weapon_SO weapon = Load<Weapon_SO>(HotAnvilBoardPath);

        Assert.AreEqual(203, weapon.id);
        Assert.AreEqual("热砧板", weapon.weaponName);
        Assert.AreEqual(Rarity.Rare, weapon.rarity);
        Assert.AreEqual(WeaponType.Axe, weapon.weaponType);
        Assert.AreEqual(4, weapon.skillPool.Length);

        EnemySkill_SO furnaceBreak = weapon.skillPool[1];
        EnemySkill_SO.StatusEffectEntry spread = furnaceBreak.GetDiceEffectSettings(1).statusEffects[0];
        Assert.AreEqual(EnemySkill_SO.DamageTargetMode.AllOpponentsExceptCurrent, spread.targetMode);
        Assert.AreEqual("burn", spread.statusData.StatusId);
        Assert.AreEqual(2, spread.requiredConditionStatusStacks);

        EnemySkill_SO reduce = weapon.skillPool[3];
        EnemySkill_SO.StatusEffectEntry targetBurn = reduce.GetDiceEffectSettings(0).statusEffects[0];
        EnemySkill_SO.StatusEffectEntry selfBurn = reduce.GetDiceEffectSettings(0).statusEffects[1];
        Assert.AreEqual(EnemySkill_SO.DamageTargetMode.CurrentTarget, targetBurn.targetMode);
        Assert.AreEqual(EnemySkill_SO.DamageTargetMode.Self, selfBurn.targetMode);
        Assert.AreEqual("burn", selfBurn.statusData.StatusId);
    }

    [Test]
    public void WeaponCatalog_ContainsTodoWeapons()
    {
        WeaponCatalog_SO catalog = Load<WeaponCatalog_SO>(WeaponCatalogPath);

        Assert.AreSame(Load<Weapon_SO>(HuiShiSwordPath), catalog.GetWeaponById(8));
        Assert.AreSame(Load<Weapon_SO>(HotAnvilBoardPath), catalog.GetWeaponById(203));
    }

    private static T Load<T>(string path) where T : UnityEngine.Object
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        Assert.IsNotNull(asset, path);
        return asset;
    }
}
