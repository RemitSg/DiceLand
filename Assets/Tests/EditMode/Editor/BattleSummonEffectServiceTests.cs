using NUnit.Framework;
using UnityEngine;

public class BattleSummonEffectServiceTests
{
    private readonly System.Collections.Generic.List<UnityEngine.Object> createdObjects = new System.Collections.Generic.List<UnityEngine.Object>();

    [TearDown]
    public void TearDown()
    {
        for (int i = 0; i < createdObjects.Count; i++)
        {
            if (createdObjects[i] != null)
                Object.DestroyImmediate(createdObjects[i]);
        }

        createdObjects.Clear();
    }

    [Test]
    public void ShouldProcess_RequiresMatchingTriggerAndEnemyData()
    {
        EnemyData_SO enemyData = ScriptableObject.CreateInstance<EnemyData_SO>();
        createdObjects.Add(enemyData);
        EnemySkill_SO.SummonEnemyEffectEntry effect = new EnemySkill_SO.SummonEnemyEffectEntry
        {
            trigger = EnemySkill_SO.DiceEffectTrigger.OnSkillUse,
            enemyData = enemyData
        };

        Assert.IsTrue(BattleSummonEffectService.ShouldProcess(effect, EnemySkill_SO.DiceEffectTrigger.OnSkillUse));
        Assert.IsFalse(BattleSummonEffectService.ShouldProcess(effect, EnemySkill_SO.DiceEffectTrigger.OnClash));

        effect.enemyData = null;
        Assert.IsFalse(BattleSummonEffectService.ShouldProcess(effect, EnemySkill_SO.DiceEffectTrigger.OnSkillUse));
    }

    [Test]
    public void NormalizeSummonCount_ClampsToAtLeastOne()
    {
        Assert.AreEqual(1, BattleSummonEffectService.NormalizeSummonCount(0));
        Assert.AreEqual(3, BattleSummonEffectService.NormalizeSummonCount(3));
    }

    [Test]
    public void BuildLabel_UsesEffectNameEnemyNameThenFallback()
    {
        EnemyData_SO enemyData = ScriptableObject.CreateInstance<EnemyData_SO>();
        createdObjects.Add(enemyData);
        enemyData.enemyName = "Slime";

        Assert.AreEqual(
            "Call",
            BattleSummonEffectService.BuildLabel(
                new EnemySkill_SO.SummonEnemyEffectEntry { effectName = "Call", enemyData = enemyData },
                0));

        Assert.AreEqual(
            "Slime",
            BattleSummonEffectService.BuildLabel(
                new EnemySkill_SO.SummonEnemyEffectEntry { enemyData = enemyData },
                0));

        Assert.AreEqual(
            "閸欘剙鏁滈弫鍫熺亯#2",
            BattleSummonEffectService.BuildLabel(default, 1));
    }

    [Test]
    public void FindNewlySpawnedEnemy_ReturnsNewEnemyWhenSameEnemyDataAlreadyExists()
    {
        EnemyData_SO enemyData = ScriptableObject.CreateInstance<EnemyData_SO>();
        createdObjects.Add(enemyData);

        BattleEnemyController oldEnemy = CreateEnemy("Old", enemyData);
        BattleEnemyController newEnemy = CreateEnemy("New", enemyData);
        BattleEnemyController[] beforeSpawn = { oldEnemy };
        BattleEnemyController[] afterSpawn = { oldEnemy, newEnemy };

        BattleEnemyController resolved = BattleSummonEffectService.FindNewlySpawnedEnemy(beforeSpawn, afterSpawn, enemyData);

        Assert.AreSame(newEnemy, resolved);
    }

    [Test]
    public void FindFirstOpenEnemyAnchorSlot_ReusesDeadEnemySlotBeforeOccupiedLaterSlots()
    {
        Transform[] anchors =
        {
            CreateAnchor("Enemy1", new Vector3(0f, 0f, 0f)),
            CreateAnchor("Enemy2", new Vector3(1f, 0f, 0f)),
            CreateAnchor("Enemy3", new Vector3(2f, 0f, 0f))
        };
        BattleEnemyController deadFirstSlot = CreateEnemy("Dead", null);
        BattleEnemyController aliveSecondSlot = CreateEnemy("Alive Second", null);
        BattleEnemyController aliveThirdSlot = CreateEnemy("Alive Third", null);
        deadFirstSlot.AssignFormationSlotIndex(0);
        aliveSecondSlot.AssignFormationSlotIndex(1);
        aliveThirdSlot.AssignFormationSlotIndex(2);
        deadFirstSlot.KillInstantly();

        int resolvedSlot = BattleSummonEffectService.FindFirstOpenEnemyAnchorSlot(
            anchors,
            new[] { deadFirstSlot, aliveSecondSlot, aliveThirdSlot });

        Assert.AreEqual(0, resolvedSlot);
    }

    [Test]
    public void FindFirstOpenEnemyAnchorSlot_ReturnsMinusOneWhenEveryAnchorIsOccupied()
    {
        Transform[] anchors =
        {
            CreateAnchor("Enemy1", new Vector3(0f, 0f, 0f)),
            CreateAnchor("Enemy2", new Vector3(1f, 0f, 0f))
        };
        BattleEnemyController first = CreateEnemy("First", null);
        BattleEnemyController second = CreateEnemy("Second", null);
        first.AssignFormationSlotIndex(0);
        second.AssignFormationSlotIndex(1);

        int resolvedSlot = BattleSummonEffectService.FindFirstOpenEnemyAnchorSlot(
            anchors,
            new[] { first, second });

        Assert.AreEqual(-1, resolvedSlot);
    }

    private BattleEnemyController CreateEnemy(string objectName, EnemyData_SO enemyData)
    {
        GameObject enemyObject = new GameObject(objectName);
        createdObjects.Add(enemyObject);
        BattleEnemyController enemy = enemyObject.AddComponent<BattleEnemyController>();
        enemy.enemyData = enemyData;
        return enemy;
    }

    private Transform CreateAnchor(string objectName, Vector3 position)
    {
        GameObject anchorObject = new GameObject(objectName);
        createdObjects.Add(anchorObject);
        anchorObject.transform.position = position;
        return anchorObject.transform;
    }
}
