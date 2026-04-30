public readonly struct BattleIndexedEffect<TEffect>
{
    public BattleIndexedEffect(int index, TEffect effect)
    {
        Index = index;
        Effect = effect;
    }

    public int Index { get; }
    public TEffect Effect { get; }
}
