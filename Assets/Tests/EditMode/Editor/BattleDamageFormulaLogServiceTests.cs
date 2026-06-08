using System.Collections.Generic;
using NUnit.Framework;

public class BattleDamageFormulaLogServiceTests
{
    [Test]
    public void BuildAttackFormula_ListsPercentAndFlatSources()
    {
        List<BattleDamageFormulaModifierSource> percentSources = new List<BattleDamageFormulaModifierSource>
        {
            new BattleDamageFormulaModifierSource(30, "劲敌·伤害强化 III"),
            new BattleDamageFormulaModifierSource(20, "劲敌·伤害强化 II")
        };
        List<BattleDamageFormulaModifierSource> flatSources = new List<BattleDamageFormulaModifierSource>
        {
            new BattleDamageFormulaModifierSource(-1, "猫咪的原谅")
        };

        string formula = BattleDamageFormulaLogService.BuildAttackFormula(
            "敌方攻击",
            "Enemy",
            "Ally",
            5,
            percentSources,
            flatSources,
            7);

        StringAssert.Contains("5（基础伤害） x (1+30%+20%)（劲敌·伤害强化 III、劲敌·伤害强化 II） - 1（猫咪的原谅） = 6.5 = 7", formula);
    }

    [Test]
    public void BuildStatusDamageFormula_ListsStacksAndDamagePerStack()
    {
        string formula = BattleDamageFormulaLogService.BuildStatusDamageFormula(
            "Ally",
            "燃烧",
            3,
            1,
            3);

        StringAssert.Contains("状态伤害：Ally 受到 燃烧，3 层 x 1（每层伤害） = 3 = 3", formula);
    }
}
