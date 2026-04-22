using NUnit.Framework;
using System.Reflection;
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

        EnemyGroup_SO group = TutorialBattleContentFactory.CreateRuntimeEnemyGroup(MapTileType.Battle, templates);

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
    public void CreateRuntimeEnemyGroup_EliteAndBossUseTwoSpeedButtons()
    {
        EnemyGroup_SO elite = TutorialBattleContentFactory.CreateRuntimeEnemyGroup(MapTileType.Elite);
        EnemyGroup_SO boss = TutorialBattleContentFactory.CreateRuntimeEnemyGroup(MapTileType.Boss);

        Assert.IsNotNull(elite);
        Assert.IsNotNull(boss);
        Assert.IsNotNull(elite.enemies);
        Assert.IsNotNull(boss.enemies);
        Assert.Greater(elite.enemies.Count, 0);
        Assert.Greater(boss.enemies.Count, 0);
        Assert.AreEqual(2, elite.enemies[0].baseSpeedButtonCount);
        Assert.AreEqual(2, boss.enemies[0].baseSpeedButtonCount);
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
            eliteTemplate = template,
            bossTemplate = template
        };

        EnemyGroup_SO elite = TutorialBattleContentFactory.CreateRuntimeEnemyGroup(MapTileType.Elite, templates);
        EnemyGroup_SO boss = TutorialBattleContentFactory.CreateRuntimeEnemyGroup(MapTileType.Boss, templates);

        Assert.AreEqual(2, elite.enemies[0].GetStageSpeedButtonCount(0));
        Assert.AreEqual(2, boss.enemies[0].GetStageSpeedButtonCount(0));

        Object.DestroyImmediate(template);
        Object.DestroyImmediate(elite);
        Object.DestroyImmediate(boss);
    }

    [Test]
    public void CreateRuntimeEnemyGroup_OpeningBattleGrantsSkillsForUseClashAndSingleAttackTiming()
    {
        EnemyGroup_SO group = TutorialBattleContentFactory.CreateRuntimeEnemyGroup(MapTileType.Battle);

        Assert.IsNotNull(group);
        Assert.IsNotNull(group.enemies);
        Assert.Greater(group.enemies.Count, 0);

        EnemyData_SO tutorialTeacher = group.enemies[0];
        Assert.IsNotNull(tutorialTeacher);
        Assert.IsNotNull(tutorialTeacher.battleStartGrantedAllySkills);
        Assert.GreaterOrEqual(tutorialTeacher.battleStartGrantedAllySkills.Count, 3);

        bool hasOnUseSkill = false;
        bool hasOnClashSkill = false;
        bool hasOnSingleAttackSkill = false;
        for (int i = 0; i < tutorialTeacher.battleStartGrantedAllySkills.Count; i++)
        {
            EnemySkill_SO skill = tutorialTeacher.battleStartGrantedAllySkills[i].skill;
            if (skill == null || skill.diceEffectSettings == null)
                continue;

            for (int diceIndex = 0; diceIndex < skill.diceEffectSettings.Length; diceIndex++)
            {
                EnemySkill_SO.DiceEffectSettings settings = skill.diceEffectSettings[diceIndex];
                if (settings.powerModifiers == null)
                    continue;

                for (int modifierIndex = 0; modifierIndex < settings.powerModifiers.Length; modifierIndex++)
                {
                    EnemySkill_SO.ConditionalPowerModifierEntry modifier = settings.powerModifiers[modifierIndex];
                    hasOnUseSkill |= modifier.trigger == EnemySkill_SO.DiceEffectTrigger.OnSkillUse;
                    hasOnClashSkill |= modifier.trigger == EnemySkill_SO.DiceEffectTrigger.OnClash;
                    hasOnSingleAttackSkill |= modifier.trigger == EnemySkill_SO.DiceEffectTrigger.OnSingleAttack;
                }
            }
        }

        Assert.IsTrue(hasOnUseSkill);
        Assert.IsTrue(hasOnClashSkill);
        Assert.IsTrue(hasOnSingleAttackSkill);
    }

    [Test]
    public void CreateRuntimeEnemyGroup_TutorialEnemiesUseDeterministicRoundSequenceRules()
    {
        AssertTutorialEnemiesUseRoundSequenceRules(TutorialBattleContentFactory.CreateRuntimeEnemyGroup(MapTileType.Battle));
        AssertTutorialEnemiesUseRoundSequenceRules(TutorialBattleContentFactory.CreateRuntimeEnemyGroup(MapTileType.Elite));
        AssertTutorialEnemiesUseRoundSequenceRules(TutorialBattleContentFactory.CreateRuntimeEnemyGroup(MapTileType.Boss));
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
