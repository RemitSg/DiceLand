using System;

public static class BattleLivingCountService
{
    public static int CountLiving<T>(T[] actors, Func<T, bool> isLiving)
    {
        if (actors == null || isLiving == null)
            return 0;

        int count = 0;
        foreach (T actor in actors)
        {
            if (isLiving(actor))
                count++;
        }

        return count;
    }
}
