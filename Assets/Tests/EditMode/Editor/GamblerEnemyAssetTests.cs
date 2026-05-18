using NUnit.Framework;
using UnityEditor;

public class GamblerEnemyAssetTests
{
    private const string EnemyPath = "Assets/Data/Enemies/Enemies/036 Gambler/Gambler_Data.asset";
    private const string GroupPath = "Assets/Data/Enemies/EnemyGroup/Events/Gambler.asset";
    private const string DebtPath = "Assets/Data/Battle/Debt/Debt.asset";

    [Test]
    public void GamblerEncounter_HasExpectedEnemyGroupComposition()
    {
        EnemyData_SO gambler = Load<EnemyData_SO>(EnemyPath);
        EnemyGroup_SO group = Load<EnemyGroup_SO>(GroupPath);

        Assert.AreEqual(36, group.groupID);
        Assert.AreEqual("Gambler", group.groupName);
        Assert.AreEqual(1, group.enemies.Count);
        Assert.AreSame(gambler, group.enemies[0]);
        Assert.AreEqual(1, group.attributes.Count);
        Assert.AreEqual("\u6697", group.attributes[0].attributeName);
    }

    [Test]
    public void GamblerEnemy_HasExpectedStatsPassivesAndBattleStartSkills()
    {
        EnemyData_SO gambler = Load<EnemyData_SO>(EnemyPath);

        Assert.AreEqual(36, gambler.enemyID);
        Assert.AreEqual("\u8D1D\u8005", gambler.enemyName);
        Assert.AreEqual(6, gambler.baseHP);
        Assert.AreEqual(1, gambler.minSpeed);
        Assert.AreEqual(1, gambler.maxSpeed);
        Assert.AreEqual(1, gambler.baseSpeedButtonCount);
        Assert.AreEqual(4, gambler.passiveDescriptions.Count);
        Assert.AreEqual("\u507F\u8FD8", gambler.passiveDescriptions[0].passiveName);
        Assert.AreEqual("\u4E0A\u684C", gambler.passiveDescriptions[3].passiveName);
        Assert.AreEqual(2, gambler.battleStartGrantedAllySkills.Count);
        Assert.AreEqual("\u8D4C\u535A", gambler.battleStartGrantedAllySkills[0].skill.skillName);
        Assert.AreEqual("\u5343\u672F", gambler.battleStartGrantedAllySkills[1].skill.skillName);
    }

    [Test]
    public void GamblerSkillsAndDebtStatus_HaveExpectedCoreConfiguration()
    {
        EnemyData_SO gambler = Load<EnemyData_SO>(EnemyPath);
        BattleStatus_SO debt = Load<BattleStatus_SO>(DebtPath);

        Assert.AreEqual("debt", debt.StatusId);
        Assert.AreEqual("\u8D1F\u503A", debt.DisplayName);
        Assert.AreEqual(1, gambler.skillPool.Count);

        EnemySkill_SO enemyGamble = gambler.skillPool[0];
        Assert.AreEqual("\u8D4C\u535A", enemyGamble.skillName);
        Assert.AreEqual(1, enemyGamble.diceCount);
        Assert.AreEqual(1, enemyGamble.GetDicePowerRange(0).minPower);
        Assert.AreEqual(6, enemyGamble.GetDicePowerRange(0).maxPower);
        Assert.AreSame(debt, enemyGamble.GetDiceEffectSettings(0).statusEffects[0].statusData);
        Assert.AreEqual(EnemySkill_SO.DiceEffectTrigger.OnClashWin, enemyGamble.GetDiceEffectSettings(0).statusEffects[0].trigger);

        EnemySkill_SO allyCheat = gambler.battleStartGrantedAllySkills[1].skill;
        Assert.AreEqual(5, allyCheat.energyCost);
        Assert.AreSame(debt, allyCheat.GetDiceEffectSettings(0).statusStackOperationEffects[0].statusData);
        Assert.AreEqual("weaknessTemp", allyCheat.GetDiceEffectSettings(0).statusEffects[0].statusData.StatusId);
    }

    private static T Load<T>(string path) where T : UnityEngine.Object
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        Assert.IsNotNull(asset, path);
        return asset;
    }
}
