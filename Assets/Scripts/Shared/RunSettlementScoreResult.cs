using System.Collections.Generic;

[System.Serializable]
public sealed class RunSettlementScoreResult
{
    public int baseScore;
    public int finalScore;
    public int gainedAbilityPoints;
    public float finalMultiplier = 1f;
    public RunSettlementDisplayStats displayStats = new RunSettlementDisplayStats();
    public List<string> debugLines = new List<string>();
}
