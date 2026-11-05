using System;
using System.Collections.Generic;

public readonly struct DisplayResolutionOption
{
    public readonly int width;
    public readonly int height;
    public readonly int refreshRate;

    public DisplayResolutionOption(int width, int height, int refreshRate)
    {
        this.width = width;
        this.height = height;
        this.refreshRate = refreshRate;
    }
}

public static class DisplayResolutionSelectionService
{
    /// <summary>Keep the saved Hz; if unavailable, choose the nearest Hz at the same dimensions.</summary>
    public static int FindBestIndex(IReadOnlyList<DisplayResolutionOption> options, DisplayResolutionOption requested)
    {
        int best = -1;
        long bestDistance = long.MaxValue;
        if (options == null) return best;
        for (int i = 0; i < options.Count; i++)
        {
            DisplayResolutionOption option = options[i];
            if (option.width != requested.width || option.height != requested.height) continue;
            long distance = Math.Abs((long)option.refreshRate - requested.refreshRate);
            if (distance < bestDistance ||
                (distance == bestDistance && best >= 0 && option.refreshRate > options[best].refreshRate))
            {
                best = i;
                bestDistance = distance;
            }
        }
        return best;
    }
}
