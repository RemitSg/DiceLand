// One settlement lifetime. The formal scene and each agent session own separate contexts.
public sealed class RunSettlementContext
{
    public RunSettlementData Current { get; private set; }
    public bool HasData => Current != null;

    public void Set(RunSettlementData data) { Current = data; }
    public RunSettlementData Consume()
    {
        RunSettlementData data = Current;
        Current = null;
        return data;
    }
    public void Clear() { Current = null; }
}
