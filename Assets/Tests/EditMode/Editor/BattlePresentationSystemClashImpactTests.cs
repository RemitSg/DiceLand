using System.Collections;
using NUnit.Framework;
using UnityEngine;

public class BattlePresentationSystemClashImpactTests
{
    [TestCase(true)]
    [TestCase(false)]
    public void HeavyContactSpreadsAndLandsBeforeItsVisibleTailFades(bool clash)
    {
        GameObject owner = new GameObject("Contact fallout fixture");
        try
        {
            var origin = new Vector3(0, 1.3f, 0);
            BattleContactVfx effect = BattleContactVfx.Spawn(owner.transform, origin, Vector3.right,
                Color.white, 1, 2.2f, 0, 0, 290, clash);
            Assert.IsNotNull(effect);
            Assert.GreaterOrEqual(effect.ParticleCount, clash ? 250 : 140);
            float highest = origin.y;
            float left = origin.x;
            float right = origin.x;
            bool groundTailVisible = false;
            for (int frame = 0; frame < 150 && !effect.IsComplete; frame++)
            {
                effect.Advance(1f / 60);
                if (!effect.HasVisibleGeometry)
                    continue;
                Bounds bounds = effect.VisualBounds;
                highest = Mathf.Max(highest, bounds.max.y);
                left = Mathf.Min(left, bounds.min.x);
                right = Mathf.Max(right, bounds.max.x);
                if (effect.GroundedParticleCount >= effect.ParticleCount * 0.8f && bounds.min.y < 0.14f)
                    groundTailVisible = true;
            }
            Assert.Greater(highest, origin.y + 0.65f, "The burst should visibly lift into the air.");
            Assert.Greater(right, origin.x + 2f, "The heavy release should travel away from contact.");
            if (clash)
                Assert.Less(left, origin.x - 2f, "Clash sparks must spray on both sides.");
            else
                Assert.Greater(right - origin.x, (origin.x - left) * 1.8f, "The bulk of hit debris should travel with the impact.");
            Assert.IsTrue(groundTailVisible, "The debris should still be visible after it has reached the floor.");
            Assert.IsTrue(effect.IsComplete);
            Assert.IsFalse(effect.HasVisibleGeometry);
        }
        finally
        {
            Object.DestroyImmediate(owner);
        }
    }

    [Test]
    public void PlayClashSequence_NoPresentationWinner_StillResolvesImpact()
    {
        GameObject systemObject = new GameObject("BattlePresentationSystemClashImpactTests");
        GameObject allyObject = new GameObject("Ally");
        GameObject enemyObject = new GameObject("Enemy");
        try
        {
            BattlePresentationSystem system = systemObject.AddComponent<BattlePresentationSystem>();
            allyObject.transform.position = Vector3.zero;
            enemyObject.transform.position = Vector3.right;

            bool impactResolved = false;
            IEnumerator routine = system.PlayClashSequence(
                allyObject.transform,
                enemyObject.transform,
                hasWinner: false,
                allyWon: false,
                allyBaseRange: new EnemySkill_SO.DicePowerRange { minPower = 3, maxPower = 5 },
                allyRange: new EnemySkill_SO.DicePowerRange { minPower = 3, maxPower = 5 },
                enemyBaseRange: new EnemySkill_SO.DicePowerRange { minPower = 2, maxPower = 4 },
                enemyRange: new EnemySkill_SO.DicePowerRange { minPower = 2, maxPower = 4 },
                allyPower: 5,
                enemyPower: 3,
                allyPowerModifier: 0,
                enemyPowerModifier: 0,
                clashCameraPower: 5f,
                hitCameraPower: 0f,
                allyClashFx: default,
                enemyClashFx: default,
                hitFx: default,
                onImpact: () => impactResolved = true,
                moveIntoPosition: false,
                returnToOrigin: false);

            RunCoroutineToCompletion(routine);

            Assert.IsTrue(impactResolved);
            Assert.AreEqual(1, systemObject.GetComponentsInChildren<BattleContactVfx>().Length,
                "Even a tied clash should play one shared contact burst.");
        }
        finally
        {
            Object.DestroyImmediate(systemObject);
            Object.DestroyImmediate(allyObject);
            Object.DestroyImmediate(enemyObject);
        }
    }

    private static void RunCoroutineToCompletion(IEnumerator routine)
    {
        const int maxSteps = 256;
        for (int i = 0; i < maxSteps && routine.MoveNext(); i++)
        {
            if (routine.Current is IEnumerator nested)
                RunCoroutineToCompletion(nested);
        }
    }
}
