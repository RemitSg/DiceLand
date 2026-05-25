using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class BattleEnemyDataAllyVisualServiceTests
{
    private readonly List<Object> createdObjects = new List<Object>();

    [TearDown]
    public void TearDown()
    {
        for (int i = createdObjects.Count - 1; i >= 0; i--)
        {
            Object created = createdObjects[i];
            if (created != null)
                Object.DestroyImmediate(created);
        }

        createdObjects.Clear();
    }

    [Test]
    public void ApplyEnemyDataVisualSource_MirrorsBodyButNotSpeedButtons()
    {
        GameObject root = CreateVisualTestObject("EnemyDataAlly");
        BattleAllyController ally = root.AddComponent<BattleAllyController>();
        SpriteRenderer bodyRenderer = CreateVisualTestRenderer(root.transform, "Body");
        SpriteRenderer preFlippedBodyRenderer = CreateVisualTestRenderer(root.transform, "PreFlippedBody");
        preFlippedBodyRenderer.flipX = true;
        SpriteRenderer speedButtonRenderer = CreateVisualTestRenderer(root.transform, "SpeedButtonSprite");
        speedButtonRenderer.flipX = true;
        speedButtonRenderer.gameObject.AddComponent<SpeedButton>();
        EnemyData_SO enemyData = ScriptableObject.CreateInstance<EnemyData_SO>();
        createdObjects.Add(enemyData);

        ally.ApplyEnemyDataVisualSource(enemyData, true);
        ally.ApplyEnemyDataVisualSource(enemyData, true);

        Assert.IsTrue(ally.MirrorsEnemyDataSprites);
        Assert.AreSame(enemyData, ally.EnemyDataVisualSource);
        Assert.IsTrue(bodyRenderer.flipX);
        Assert.IsFalse(preFlippedBodyRenderer.flipX);
        Assert.IsTrue(speedButtonRenderer.flipX);
    }

    [Test]
    public void ApplyEnemyDataVisualSource_MirrorsBodyWhenSpeedButtonIsOnRoot()
    {
        GameObject root = CreateVisualTestObject("EnemyDataAllyWithRootButton");
        BattleAllyController ally = root.AddComponent<BattleAllyController>();
        root.AddComponent<SpeedButton>();
        SpriteRenderer bodyRenderer = root.AddComponent<SpriteRenderer>();
        EnemyData_SO enemyData = ScriptableObject.CreateInstance<EnemyData_SO>();
        createdObjects.Add(enemyData);

        ally.ApplyEnemyDataVisualSource(enemyData, true);

        Assert.IsTrue(bodyRenderer.flipX);
    }

    [Test]
    public void ShouldMirrorEnemyDataSkillSprite_OnlyTrueForMirroredEnemyDataAlly()
    {
        GameObject mirroredRoot = CreateVisualTestObject("MirroredAlly");
        BattleAllyController mirroredAlly = mirroredRoot.AddComponent<BattleAllyController>();
        EnemyData_SO enemyData = ScriptableObject.CreateInstance<EnemyData_SO>();
        createdObjects.Add(enemyData);
        mirroredAlly.ApplyEnemyDataVisualSource(enemyData, true);

        GameObject normalRoot = CreateVisualTestObject("NormalAlly");
        normalRoot.AddComponent<BattleAllyController>();

        Assert.IsTrue(BattleEnemyDataAllyVisualService.ShouldMirrorEnemyDataSkillSprite(mirroredRoot.transform));
        Assert.IsFalse(BattleEnemyDataAllyVisualService.ShouldMirrorEnemyDataSkillSprite(normalRoot.transform));
    }

    private SpriteRenderer CreateVisualTestRenderer(Transform parent, string name)
    {
        GameObject go = CreateVisualTestObject(name);
        go.transform.SetParent(parent, false);
        return go.AddComponent<SpriteRenderer>();
    }

    private GameObject CreateVisualTestObject(string name)
    {
        GameObject go = new GameObject(name);
        createdObjects.Add(go);
        return go;
    }
}
