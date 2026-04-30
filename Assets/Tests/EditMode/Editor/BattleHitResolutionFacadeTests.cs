using System.Collections.Generic;
using NUnit.Framework;

public class BattleHitResolutionFacadeTests
{
    [Test]
    public void Resolve_WhenDamageIsZero_RunsZeroDamageAndMissOnly()
    {
        List<string> events = new List<string>();

        BattleHitResolutionFacade.Resolve(
            dealtDamage: 0,
            onZeroDamage: () => events.Add("zero"),
            onSuccessfulHit: () => events.Add("success"),
            onMiss: () => events.Add("miss"));

        CollectionAssert.AreEqual(new[] { "zero", "miss" }, events);
    }

    [Test]
    public void Resolve_WhenDamageIsPositive_RunsSuccessfulHitOnly()
    {
        List<string> events = new List<string>();

        BattleHitResolutionFacade.Resolve(
            dealtDamage: 3,
            onZeroDamage: () => events.Add("zero"),
            onSuccessfulHit: () => events.Add("success"),
            onMiss: () => events.Add("miss"));

        CollectionAssert.AreEqual(new[] { "success" }, events);
    }
}
