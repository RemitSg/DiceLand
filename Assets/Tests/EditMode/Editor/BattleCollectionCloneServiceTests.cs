using NUnit.Framework;

public class BattleCollectionCloneServiceTests
{
    [Test]
    public void CloneNonNullReferences_SkipsNullEntries()
    {
        LootTable_SO table = null;
        System.Collections.Generic.List<LootTable_SO> clone = BattleCollectionCloneService.CloneNonNullReferences(
            new System.Collections.Generic.List<LootTable_SO> { null, table });

        Assert.IsEmpty(clone);
    }
}
