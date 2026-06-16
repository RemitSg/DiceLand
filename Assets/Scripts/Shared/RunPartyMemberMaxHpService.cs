using System.Collections.Generic;
using UnityEngine;

public static class RunPartyMemberMaxHpService
{
    public static int ResolvePreCalamityMaxHP(RunPartyMemberState member)
    {
        if (member == null)
            return 1;

        return Mathf.Max(1, member.preCalamityMaxHP > 0 ? member.preCalamityMaxHP : member.maxHP);
    }

    public static void InitializePreCalamityMaxHP(RunPartyMemberState member)
    {
        if (member == null)
            return;

        if (member.preCalamityMaxHP <= 0)
            member.preCalamityMaxHP = Mathf.Max(1, member.maxHP);
    }

    public static void InitializeBaseTracking(RunPartyMemberState member)
    {
        InitializePreCalamityMaxHP(member);
        if (member == null)
            return;

        int characterBaseMaxHP = member.characterData != null ? Mathf.Max(1, member.characterData.baseHP) : member.preCalamityMaxHP;
        if (member.maxHPFlatBonus == 0 && member.preCalamityMaxHP > characterBaseMaxHP)
            member.maxHPFlatBonus = member.preCalamityMaxHP - characterBaseMaxHP;
    }

    public static void RecalculateParty(RunState runState)
    {
        if (runState == null || runState.partyMembers == null)
            return;

        for (int i = 0; i < runState.partyMembers.Count; i++)
            Recalculate(runState.partyMembers[i], runState.activeCalamities);
    }

    public static void Recalculate(RunPartyMemberState member, IReadOnlyList<RunCalamityState> activeCalamities)
    {
        if (member == null)
            return;

        InitializeBaseTracking(member);
        ApplyResolvedMaxHP(member, CalamitySystem.ApplyAllyMaxHpModifiers(member.preCalamityMaxHP, activeCalamities));
    }

    public static void AddPreCalamityMaxHp(
        RunPartyMemberState member,
        int amount,
        IReadOnlyList<RunCalamityState> activeCalamities)
    {
        if (member == null || amount == 0)
            return;

        InitializeBaseTracking(member);
        member.maxHPFlatBonus += amount;
        member.preCalamityMaxHP = Mathf.Max(1, member.preCalamityMaxHP + amount);
        Recalculate(member, activeCalamities);
    }

    public static void SetBaseMaxHp(
        RunPartyMemberState member,
        int value,
        IReadOnlyList<RunCalamityState> activeCalamities)
    {
        if (member == null)
            return;

        InitializeBaseTracking(member);
        member.preCalamityMaxHP = Mathf.Max(1, value + member.maxHPFlatBonus);
        Recalculate(member, activeCalamities);
    }

    private static void ApplyResolvedMaxHP(RunPartyMemberState member, int resolvedMaxHP)
    {
        int previousMaxHP = Mathf.Max(1, member.maxHP);
        int nextMaxHP = Mathf.Max(1, resolvedMaxHP);
        int delta = nextMaxHP - previousMaxHP;

        member.maxHP = nextMaxHP;
        if (member.currentHP <= 0)
        {
            member.currentHP = 0;
            return;
        }

        if (delta > 0)
            member.currentHP = Mathf.Clamp(member.currentHP + delta, 1, nextMaxHP);
        else
            member.currentHP = Mathf.Min(member.currentHP, nextMaxHP);
    }
}
