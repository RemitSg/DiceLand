using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class TutorialBattleContentFactoryTests
{
    [Test]
    public void CreateRuntimeEnemyGroup_UsesConfiguredVisualTemplateSprite()
    {
        Sprite testSprite = CreateTestSprite();
        EnemyData_SO template = ScriptableObject.CreateInstance<EnemyData_SO>();
        template.enemySprite = testSprite;
        template.enemyVisualScale = 1.75f;
        template.enemyVisualOffset = new Vector3(1f, 2f, 0f);
        template.speedButtonOffset = new Vector2(3f, 4f);

        TutorialEnemyVisualTemplateSet templates = new TutorialEnemyVisualTemplateSet
        {
            openingPrimaryTemplate = template
        };

        EnemyGroup_SO group = TutorialBattleContentFactory.CreateRuntimeEnemyGroup(TutorialBattleStageId.Battle01, templates);

        Assert.IsNotNull(group);
        Assert.IsNotNull(group.enemies);
        Assert.Greater(group.enemies.Count, 0);
        Assert.AreSame(testSprite, group.enemies[0].enemySprite);
        Assert.AreEqual(1.75f, group.enemies[0].enemyVisualScale);
        Assert.AreEqual(new Vector3(1f, 2f, 0f), group.enemies[0].enemyVisualOffset);
        Assert.AreEqual(new Vector2(3f, 4f), group.enemies[0].speedButtonOffset);

        Object.DestroyImmediate(template);
        Object.DestroyImmediate(group);
        Object.DestroyImmediate(testSprite.texture);
        Object.DestroyImmediate(testSprite);
    }

    [Test]
    public void CreateRuntimeEnemyGroup_Battle02AndBattle03UseTwoSpeedButtons()
    {
        EnemyGroup_SO battle02 = TutorialBattleContentFactory.CreateRuntimeEnemyGroup(TutorialBattleStageId.Battle02);
        EnemyGroup_SO battle03 = TutorialBattleContentFactory.CreateRuntimeEnemyGroup(TutorialBattleStageId.Battle03);

        Assert.IsNotNull(battle02);
        Assert.IsNotNull(battle03);
        Assert.IsNotNull(battle02.enemies);
        Assert.IsNotNull(battle03.enemies);
        Assert.Greater(battle02.enemies.Count, 0);
        Assert.Greater(battle03.enemies.Count, 0);
        Assert.AreEqual(2, battle02.enemies[0].baseSpeedButtonCount);
        Assert.AreEqual(2, battle03.enemies[0].baseSpeedButtonCount);
    }

    [Test]
    public void CreateRuntimeEnemyGroup_VisualTemplateStagesDoNotOverrideTutorialSpeedButtonCount()
    {
        EnemyData_SO template = ScriptableObject.CreateInstance<EnemyData_SO>();
        template.stageSprites = new System.Collections.Generic.List<EnemyData_SO.EnemyStageEntry>
        {
            new EnemyData_SO.EnemyStageEntry
            {
                stageName = "Template Stage",
                speedButtonCountOverride = 1
            }
        };

        TutorialEnemyVisualTemplateSet templates = new TutorialEnemyVisualTemplateSet
        {
            openingSecondaryTemplate = template,
            bossTemplate = template
        };

        EnemyGroup_SO battle02 = TutorialBattleContentFactory.CreateRuntimeEnemyGroup(TutorialBattleStageId.Battle02, templates);
        EnemyGroup_SO battle07 = TutorialBattleContentFactory.CreateRuntimeEnemyGroup(TutorialBattleStageId.Battle07, templates);

        Assert.AreEqual(2, battle02.enemies[0].GetStageSpeedButtonCount(0));
        Assert.AreEqual(2, battle07.enemies[0].GetStageSpeedButtonCount(0));

        Object.DestroyImmediate(template);
        Object.DestroyImmediate(battle02);
        Object.DestroyImmediate(battle07);
    }

    [Test]
    public void CreateRuntimeEnemyGroup_AllTutorialBattleStagesUseDeterministicRoundSequenceRules()
    {
        for (int i = 1; i <= 7; i++)
            AssertTutorialEnemiesUseRoundSequenceRules(TutorialBattleContentFactory.CreateRuntimeEnemyGroup((TutorialBattleStageId)i));
    }

    [Test]
    public void CreateRuntimeDefinition_Battle01_DoesNotIncludeAdvancedModifierTimingTeaching()
    {
        TutorialBattleDefinition_SO definition = TutorialBattleContentFactory.CreateRuntimeDefinition(TutorialBattleStageId.Battle01);

        Assert.IsNotNull(definition);
        string combined = CombineDefinitionText(definition);
        StringAssert.Contains("速度", combined);
        StringAssert.DoesNotContain("[拼点时]", combined);
        StringAssert.DoesNotContain("[单方面攻击时]", combined);
    }

    [Test]
    public void CreateRuntimeDefinition_Battle04_ExplicitlyTeachesThreeModifierTimings()
    {
        TutorialBattleDefinition_SO definition = TutorialBattleContentFactory.CreateRuntimeDefinition(TutorialBattleStageId.Battle04);

        Assert.IsNotNull(definition);
        string combined = CombineDefinitionText(definition);
        StringAssert.Contains("[拼点时]", combined);
        StringAssert.Contains("[使用时]", combined);
        StringAssert.Contains("[单方面攻击时]", combined);
    }

    [Test]
    public void CreateRuntimeDefinition_Battle03_AdvancesWhenAllySkillIsViewed()
    {
        TutorialBattleDefinition_SO definition = TutorialBattleContentFactory.CreateRuntimeDefinition(TutorialBattleStageId.Battle03);

        Assert.IsNotNull(definition);
        Assert.Greater(definition.GetOrderedSteps().Count, 0);
        Assert.AreEqual(TutorialBattleEventType.AllySkillViewed, definition.GetOrderedSteps()[0].completionEvent);
    }

    [Test]
    public void CreateRuntimeEnemyGroup_UsesRequestedTutorialHpCurve()
    {
        EnemyGroup_SO battle01 = TutorialBattleContentFactory.CreateRuntimeEnemyGroup(TutorialBattleStageId.Battle01);
        EnemyGroup_SO battle02 = TutorialBattleContentFactory.CreateRuntimeEnemyGroup(TutorialBattleStageId.Battle02);
        EnemyGroup_SO battle05 = TutorialBattleContentFactory.CreateRuntimeEnemyGroup(TutorialBattleStageId.Battle05);
        EnemyGroup_SO battle06 = TutorialBattleContentFactory.CreateRuntimeEnemyGroup(TutorialBattleStageId.Battle06);

        Assert.IsNotNull(battle01);
        Assert.IsNotNull(battle02);
        Assert.IsNotNull(battle05);
        Assert.IsNotNull(battle06);
        Assert.Greater(battle01.enemies.Count, 0);
        Assert.Greater(battle02.enemies.Count, 0);
        Assert.Greater(battle05.enemies.Count, 0);
        Assert.Greater(battle06.enemies.Count, 0);
        Assert.AreEqual(12, battle01.enemies[0].baseHP);
        Assert.AreEqual(16, battle02.enemies[0].baseHP);
        Assert.AreEqual(20, battle05.enemies[0].baseHP);
        Assert.AreEqual(20, battle06.enemies[0].baseHP);
    }

    [Test]
    public void CreateRuntimeBattle06_ReusesBattle05MergedTutorialContent()
    {
        TutorialBattleDefinition_SO battle05Definition = TutorialBattleContentFactory.CreateRuntimeDefinition(TutorialBattleStageId.Battle05);
        TutorialBattleDefinition_SO battle06Definition = TutorialBattleContentFactory.CreateRuntimeDefinition(TutorialBattleStageId.Battle06);
        EnemyGroup_SO battle05Group = TutorialBattleContentFactory.CreateRuntimeEnemyGroup(TutorialBattleStageId.Battle05);
        EnemyGroup_SO battle06Group = TutorialBattleContentFactory.CreateRuntimeEnemyGroup(TutorialBattleStageId.Battle06);

        Assert.IsNotNull(battle05Definition);
        Assert.IsNotNull(battle06Definition);
        Assert.IsNotNull(battle05Group);
        Assert.IsNotNull(battle06Group);
        Assert.AreEqual(CombineDefinitionText(battle05Definition), CombineDefinitionText(battle06Definition));
        Assert.AreEqual(battle05Group.enemies[0].baseHP, battle06Group.enemies[0].baseHP);
        Assert.AreEqual(battle05Group.enemies[0].baseSpeedButtonCount, battle06Group.enemies[0].baseSpeedButtonCount);
        Assert.AreEqual(battle05Group.enemies[0].conditionalSkillRules.Count, battle06Group.enemies[0].conditionalSkillRules.Count);
    }

    [Test]
    public void CreateRuntimeEnemyGroup_Battle07_UsesConfiguredBurnStatusResource()
    {
        BattleStatus_SO burnStatus = ScriptableObject.CreateInstance<BattleStatus_SO>();
        TutorialEnemyVisualTemplateSet templates = new TutorialEnemyVisualTemplateSet
        {
            burnStatusTemplate = burnStatus
        };

        EnemyGroup_SO battle07 = TutorialBattleContentFactory.CreateRuntimeEnemyGroup(TutorialBattleStageId.Battle07, templates);

        Assert.IsNotNull(battle07);
        Assert.Greater(battle07.enemies.Count, 0);
        Assert.IsNotNull(battle07.enemies[0].skillPool);
        Assert.Greater(battle07.enemies[0].skillPool.Count, 0);
        Assert.AreSame(
            burnStatus,
            battle07.enemies[0].skillPool[0].diceEffectSettings[0].statusEffects[0].statusData);

        Object.DestroyImmediate(burnStatus);
        Object.DestroyImmediate(battle07);
    }

    [Test]
    public void BattleButton_ExposesTutorialGuideFontFieldsForInspectorConfiguration()
    {
        FieldInfo keywordField = typeof(BattleButton).GetField("tutorialGuideKeywordFont", BindingFlags.Instance | BindingFlags.NonPublic);
        FieldInfo bodyField = typeof(BattleButton).GetField("tutorialGuideBodyFont", BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.IsNotNull(keywordField);
        Assert.IsNotNull(bodyField);
        Assert.AreEqual(typeof(TMPro.TMP_FontAsset), keywordField.FieldType);
        Assert.AreEqual(typeof(TMPro.TMP_FontAsset), bodyField.FieldType);
    }

    private static string CombineDefinitionText(TutorialBattleDefinition_SO definition)
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        var steps = definition.GetOrderedSteps();
        for (int i = 0; i < steps.Count; i++)
        {
            if (steps[i] == null)
                continue;

            builder.Append(steps[i].title);
            builder.Append(steps[i].body);
        }

        var reactions = definition.GetOrderedReactions();
        for (int i = 0; i < reactions.Count; i++)
        {
            if (reactions[i] == null)
                continue;

            builder.Append(reactions[i].title);
            builder.Append(reactions[i].body);
        }

        return builder.ToString();
    }

    private static Sprite CreateTestSprite()
    {
        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        texture.SetPixel(0, 0, Color.white);
        texture.SetPixel(1, 0, Color.white);
        texture.SetPixel(0, 1, Color.white);
        texture.SetPixel(1, 1, Color.white);
        texture.Apply(false, false);
        return Sprite.Create(texture, new Rect(0f, 0f, 2f, 2f), new Vector2(0.5f, 0.5f), 1f);
    }

    private static void AssertTutorialEnemiesUseRoundSequenceRules(EnemyGroup_SO group)
    {
        Assert.IsNotNull(group);
        Assert.IsNotNull(group.enemies);
        Assert.Greater(group.enemies.Count, 0);

        for (int enemyIndex = 0; enemyIndex < group.enemies.Count; enemyIndex++)
        {
            EnemyData_SO enemy = group.enemies[enemyIndex];
            Assert.IsNotNull(enemy);
            Assert.IsNotNull(enemy.conditionalSkillRules);
            Assert.Greater(enemy.conditionalSkillRules.Count, 0, $"Enemy {enemy.enemyName} should have scripted tutorial skill rules.");

            bool hasRoundSequenceRule = false;
            for (int ruleIndex = 0; ruleIndex < enemy.conditionalSkillRules.Count; ruleIndex++)
            {
                EnemyData_SO.ConditionalSkillRule rule = enemy.conditionalSkillRules[ruleIndex];
                if (rule.conditionType != EnemyData_SO.ConditionalSkillConditionType.RoundSequence)
                    continue;

                hasRoundSequenceRule = true;
                Assert.GreaterOrEqual(rule.firstTriggerRound, 1);
                Assert.GreaterOrEqual(rule.repeatEveryNRounds, 1);
                Assert.GreaterOrEqual(rule.maxSlotsPerRound, 1);
            }

            Assert.IsTrue(hasRoundSequenceRule, $"Enemy {enemy.enemyName} should use round-sequence rules for deterministic tutorial behavior.");
        }
    }
}
