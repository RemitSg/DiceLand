using System.Collections.Generic;
using NUnit.Framework;

public class BattleSuccessfulHitEffectDispatcherTests
{
    [Test]
    public void DispatchAllyHit_RunsRelicsBeforeSkillEffectsInPlanOrder()
    {
        List<string> calls = new List<string>();
        BattleSuccessfulHitRelicPlan relicPlan = new BattleSuccessfulHitRelicPlan
        {
            ApplyPlayerHitRelics = true,
            ApplyEnemyKilledByPlayerRelics = true
        };
        BattleSuccessfulHitSkillEffectPlan skillPlan = BattleSuccessfulHitSkillEffectPlanService.Resolve(isSingleAttack: true);

        BattleSuccessfulHitEffectDispatcher.DispatchAllyHit(
            relicPlan,
            skillPlan,
            () => calls.Add("player-hit-relics"),
            () => calls.Add("kill-relics"),
            trigger => calls.Add($"immediate:{trigger}"),
            () => calls.Add("reactive-status"),
            trigger => calls.Add($"status:{trigger}"));

        CollectionAssert.AreEqual(
            new[]
            {
                "player-hit-relics",
                "kill-relics",
                "immediate:OnSingleAttack",
                "immediate:OnClashWin",
                "immediate:OnHit",
                "reactive-status",
                "status:OnSingleAttack",
                "status:OnClashWin",
                "status:OnHit"
            },
            calls);
    }

    [Test]
    public void DispatchEnemyHit_RunsSingleAttackRelicsBeforeSkillEffects()
    {
        List<string> calls = new List<string>();
        BattleSuccessfulHitRelicPlan relicPlan = new BattleSuccessfulHitRelicPlan
        {
            ApplyPlayerTookSingleAttackRelics = true
        };
        BattleSuccessfulHitSkillEffectPlan skillPlan = BattleSuccessfulHitSkillEffectPlanService.Resolve(isSingleAttack: false);

        BattleSuccessfulHitEffectDispatcher.DispatchEnemyHit(
            relicPlan,
            skillPlan,
            () => calls.Add("took-single-attack-relics"),
            trigger => calls.Add($"immediate:{trigger}"),
            () => calls.Add("reactive-status"),
            trigger => calls.Add($"status:{trigger}"));

        CollectionAssert.AreEqual(
            new[]
            {
                "took-single-attack-relics",
                "immediate:OnClashWin",
                "immediate:OnHit",
                "reactive-status",
                "status:OnClashWin",
                "status:OnHit"
            },
            calls);
    }
}
