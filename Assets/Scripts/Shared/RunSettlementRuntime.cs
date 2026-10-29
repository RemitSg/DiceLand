public static class RunSettlementRuntime
{
    internal static RunSettlementContext Context { get; } = new RunSettlementContext();
    public static RunSettlementData Current => Context.Current;
    public static bool HasData => Current != null;

    public static void Set(RunSettlementData data)
    {
        Context.Set(data);
    }

    public static RunSettlementData Consume()
    {
        return Context.Consume();
    }

    public static void Clear()
    {
        Context.Clear();
    }
}
