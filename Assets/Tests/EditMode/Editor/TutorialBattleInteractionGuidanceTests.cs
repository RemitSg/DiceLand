using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public sealed class TutorialBattleInteractionGuidanceTests
{
    [SetUp]
    public void SetUp()
    {
        MainMenuRuntime.ClearTutorialRequest();
        TutorialBattleRuntime.Clear();
    }

    [TearDown]
    public void TearDown()
    {
        MainMenuRuntime.ClearTutorialRequest();
        TutorialBattleRuntime.Clear();
        SpeedButton.ClearTutorialAllowedScreenRect();
    }

    [Test]
    public void CompletedActionStaysFreeAcrossBattlesAndResetsForNewTutorial()
    {
        var definition = ScriptableObject.CreateInstance<TutorialBattleDefinition_SO>();
        try
        {
            MainMenuRuntime.RequestTutorialStart();
            MainMenuRuntime.ConsumeTutorialRequest();
            TutorialBattleRuntime.BeginBattle(definition);
            Assert.IsTrue(TutorialBattleInteractionGuidance.NeedsGuidance(TutorialBattleInteraction.EnemySkill));
            TutorialBattleInteractionGuidance.Complete(TutorialBattleInteraction.EnemySkill);
            TutorialBattleRuntime.Clear();
            TutorialBattleRuntime.BeginBattle(definition);
            Assert.IsFalse(TutorialBattleInteractionGuidance.NeedsGuidance(TutorialBattleInteraction.EnemySkill));
            Assert.IsTrue(TutorialBattleInteractionGuidance.NeedsGuidance(TutorialBattleInteraction.SwitchSkill));
            MainMenuRuntime.RequestTutorialStart();
            Assert.IsTrue(TutorialBattleInteractionGuidance.NeedsGuidance(TutorialBattleInteraction.EnemySkill));
        }
        finally
        {
            Object.DestroyImmediate(definition);
        }
    }

    [TestCase(TutorialBattleEventType.EnemyDetailViewed, TutorialBattleInteraction.EnemyDetail)]
    [TestCase(TutorialBattleEventType.EnemySkillViewed, TutorialBattleInteraction.EnemySkill)]
    [TestCase(TutorialBattleEventType.AllySkillViewed, TutorialBattleInteraction.AllySkill)]
    [TestCase(TutorialBattleEventType.SkillSwitched, TutorialBattleInteraction.SwitchSkill)]
    [TestCase(TutorialBattleEventType.LineCreated, TutorialBattleInteraction.Connect)]
    [TestCase(TutorialBattleEventType.EnergyLineCreated, TutorialBattleInteraction.EnergyConnect)]
    public void UnconfiguredStepsStillResolveTheirFirstAction(TutorialBattleEventType action, TutorialBattleInteraction expected)
    {
        var step = new TutorialBattleStepDefinition { completionEvent = action };
        Assert.AreEqual(TutorialBattleHighlightTarget.None, step.highlightAfterContinue);
        Assert.AreEqual(expected, TutorialBattleInteractionGuidance.ForStep(step, false));
    }

    [Test]
    public void PrepareExecuteAndRedirectAreTaughtAsDistinctActions()
    {
        var step = new TutorialBattleStepDefinition { completionEvent = TutorialBattleEventType.CombatExecutePressed };
        Assert.AreEqual(TutorialBattleInteraction.Prepare, TutorialBattleInteractionGuidance.ForStep(step, true));
        Assert.AreEqual(TutorialBattleInteraction.Execute, TutorialBattleInteractionGuidance.ForStep(step, false));
        TutorialBattleInteractionGuidance.Complete(TutorialBattleInteraction.Prepare);
        Assert.IsTrue(TutorialBattleInteractionGuidance.NeedsGuidance(TutorialBattleInteraction.Execute));
        step.completionEvent = TutorialBattleEventType.LineCreated;
        step.stepId = "battle02_redirect_with_guard";
        Assert.AreEqual(TutorialBattleInteraction.Redirect, TutorialBattleInteractionGuidance.ForStep(step, false));
    }

    [Test]
    public void EmptyTrackedTargetBlocksWorldInputUntilExplicitlyCleared()
    {
        SpeedButton.SetTutorialAllowedScreenRect(Rect.zero);
        Assert.IsFalse(SpeedButton.IsScreenPointAllowedByTutorialHighlight(new Vector2(100, 100)));
        SpeedButton.ClearTutorialAllowedScreenRect();
        Assert.IsTrue(SpeedButton.IsScreenPointAllowedByTutorialHighlight(new Vector2(100, 100)));
    }

    [Test]
    public void TwoHolesRenderAndAcceptOnlyTheirOwnAreas()
    {
        var go = new GameObject("TutorialMaskTest", typeof(RectTransform));
        try
        {
            var rect = (RectTransform)go.transform;
            rect.sizeDelta = new Vector2(1000, 800);
            var shade = go.AddComponent<TutorialSpotlightGraphic>();
            var first = new Rect(-400, -100, 120, 120);
            var second = new Rect(220, 80, 150, 100);
            shade.SetHole(first, true, false);
            shade.SetAdditionalHole(second, true);
            Assert.IsFalse(shade.IsRaycastLocationValid(first.center, null));
            Assert.IsFalse(shade.IsRaycastLocationValid(second.center, null));
            Assert.IsTrue(shade.IsRaycastLocationValid(Vector2.zero, null));
            var populate = typeof(TutorialSpotlightGraphic).GetMethod("OnPopulateMesh",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
                null, new[] { typeof(VertexHelper) }, null);
            using (var mesh = new VertexHelper())
            {
                populate.Invoke(shade, new object[] { mesh });
                Assert.Greater(mesh.currentVertCount, 0);
                for (int i = 0; i < mesh.currentVertCount; i += 4)
                {
                    var a = new UIVertex();
                    var b = new UIVertex();
                    mesh.PopulateUIVertex(ref a, i);
                    mesh.PopulateUIVertex(ref b, i + 2);
                    Vector2 center = (a.position + b.position) * .5f;
                    Assert.IsFalse(first.Contains(center));
                    Assert.IsFalse(second.Contains(center));
                }
            }
            shade.SetHole(first, true, true);
            Assert.IsTrue(shade.IsRaycastLocationValid(first.center, null));
            Assert.IsTrue(shade.IsRaycastLocationValid(second.center, null));
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }
}
