using System.Collections.Generic;
using NUnit.Framework;

public class BattleHitResolutionFacadeTests
{
    [Test]
    public void Resolve_WhenConfirmedHitDamageIsZero_RunsZeroDamageAndSuccessfulHit()
    {
        List<string> events = new List<string>();

        BattleHitResolutionFacade.Resolve(
            dealtDamage: 0,
            hitConfirmed: true,
            onZeroDamage: () => events.Add("zero"),
            onSuccessfulHit: () => events.Add("success"),
            onMiss: () => events.Add("miss"));

        CollectionAssert.AreEqual(new[] { "zero", "success" }, events);
    }

    [Test]
    public void Resolve_WhenDamageIsPositive_RunsSuccessfulHitOnly()
    {
        List<string> events = new List<string>();

        BattleHitResolutionFacade.Resolve(
            dealtDamage: 3,
            hitConfirmed: true,
            onZeroDamage: () => events.Add("zero"),
            onSuccessfulHit: () => events.Add("success"),
            onMiss: () => events.Add("miss"));

        CollectionAssert.AreEqual(new[] { "success" }, events);
    }

    [Test]
    public void Resolve_WhenHitIsNotConfirmed_RunsZeroDamageAndMissOnly()
    {
        List<string> events = new List<string>();

        BattleHitResolutionFacade.Resolve(
            dealtDamage: 0,
            hitConfirmed: false,
            onZeroDamage: () => events.Add("zero"),
            onSuccessfulHit: () => events.Add("success"),
            onMiss: () => events.Add("miss"));

        CollectionAssert.AreEqual(new[] { "zero", "miss" }, events);
    }
}
