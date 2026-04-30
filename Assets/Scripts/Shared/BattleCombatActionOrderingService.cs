using System.Collections.Generic;

public static class BattleCombatActionOrderingService
{
    public static void Sort(List<BattleCombatAction> actions)
    {
        actions?.Sort(Compare);
    }

    public static int Compare(BattleCombatAction a, BattleCombatAction b)
    {
        if (ReferenceEquals(a, b))
            return 0;
        if (a == null)
            return 1;
        if (b == null)
            return -1;

        int speedCompare = b.PrioritySpeed.CompareTo(a.PrioritySpeed);
        if (speedCompare != 0)
            return speedCompare;

        int tieCompare = a.TiePriority.CompareTo(b.TiePriority);
        if (tieCompare != 0)
            return tieCompare;

        return a.SequenceIndex.CompareTo(b.SequenceIndex);
    }
}
