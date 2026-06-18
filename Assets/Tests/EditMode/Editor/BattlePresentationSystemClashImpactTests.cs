using System.Collections;
using NUnit.Framework;
using UnityEngine;

public class BattlePresentationSystemClashImpactTests
{
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
