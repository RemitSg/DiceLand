using System.Collections.Generic;
using NUnit.Framework;

public class BattleRelicProcessDispatcherTests
{
    [Test]
    public void Process_ForwardsEncounterAndContext()
    {
        List<string> calls = new List<string>();
        BattleEncounterData encounter = new BattleEncounterData();
        RelicExecutionContext context = new RelicExecutionContext { diceIndex = 3 };

        BattleRelicProcessDispatcher.Process(
            encounter,
            context,
            (receivedEncounter, receivedContext) =>
                calls.Add($"{ReferenceEquals(encounter, receivedEncounter)}:{receivedContext.diceIndex}"));

        CollectionAssert.AreEqual(new[] { "True:3" }, calls);
    }
}
