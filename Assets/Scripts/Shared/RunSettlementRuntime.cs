public static class RunSettlementRuntime
{
    public static RunSettlementData Current { get; private set; }
    public static bool HasData => Current != null;

    public static void Set(RunSettlementData data)
    {
        Current = data;
    }

    public static RunSettlementData Consume()
    {
        RunSettlementData data = Current;
        Current = null;
        return data;
    }

    public static void Clear()
    {
        Current = null;
    }
}
