using System.Collections.Generic;

public sealed class RunSettlementScoreResult
{
    public int baseScore;
    public int finalScore;
    public int gainedAbilityPoints;
    public float finalMultiplier = 1f;
    public RunSettlementDisplayStats displayStats = new RunSettlementDisplayStats();
    public List<string> debugLines = new List<string>();
}
