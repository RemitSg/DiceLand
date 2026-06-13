using System.Collections.Generic;
using UnityEngine;

public readonly struct RunPartyMemberEffectiveStats
{
    public readonly int maxHP;
    public readonly int currentHP;

    public RunPartyMemberEffectiveStats(int maxHP, int currentHP)
    {
        this.maxHP = maxHP;
        this.currentHP = currentHP;
    }
}

public static class RunPartyMemberEffectiveStatsService
{
    public static RunPartyMemberEffectiveStats Resolve(
        RunPartyMemberState member,
        IReadOnlyList<RunCalamityState> activeCalamities)
    {
        if (member == null)
            return new RunPartyMemberEffectiveStats(1, 0);

        int persistentMaxHP = Mathf.Max(1, member.maxHP);
        int effectiveMaxHP = Mathf.Max(1, CalamitySystem.ApplyAllyMaxHpModifiers(persistentMaxHP, activeCalamities));
        int effectiveCurrentHP = Mathf.Clamp(member.currentHP, 0, effectiveMaxHP);
        return new RunPartyMemberEffectiveStats(effectiveMaxHP, effectiveCurrentHP);
    }
}
