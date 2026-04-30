using NUnit.Framework;

public class BattleEnemyBuiltInEffectLogServiceTests
{
    [Test]
    public void BuildHealLog_FormatsSelfAndTargetHealing()
    {
        string self = BattleEnemyBuiltInEffectLogService.BuildHealLog("Enemy", "Heal", null, 3, 5, 9);
        string target = BattleEnemyBuiltInEffectLogService.BuildHealLog("Enemy", "Heal", "Ally", 4, 6, 10);

        StringAssert.Contains("Enemy[Heal]", self);
        StringAssert.Contains("恢复 3 点生命", self);
        StringAssert.Contains("当前HP 5/9", self);
        StringAssert.Contains("使 Ally 恢复 4 点生命", target);
    }

    [Test]
    public void BuildDamageLog_FormatsSelfAndTargetDamage()
    {
        string self = BattleEnemyBuiltInEffectLogService.BuildDamageLog("Enemy", "Blast", null, 3);
        string target = BattleEnemyBuiltInEffectLogService.BuildDamageLog("Enemy", "Blast", "Ally", 4);

        StringAssert.Contains("对 自己 造成 3 点伤害", self);
        StringAssert.Contains("对 Ally 追加造成 4 点伤害", target);
    }

    [Test]
    public void BuildTransferLog_FormatsTransfer()
    {
        string log = BattleEnemyBuiltInEffectLogService.BuildTransferLog("Enemy", "Shift", "Poison", 2, "Ally");

        StringAssert.Contains("Enemy[Shift]", log);
        StringAssert.Contains("将 2 层 Poison 转移给 Ally", log);
    }
}
