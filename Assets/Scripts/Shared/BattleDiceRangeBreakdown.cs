using System.Collections.Generic;
using UnityEngine;

internal sealed class BattleDiceRangeBreakdown
{
    internal struct Modifier
    {
        public string Name;
        public Sprite Icon;
        public int Minimum;
        public int Maximum;
        public bool IsSkill;
        public bool ClashOnly;
    }

    private readonly List<Modifier> modifiers = new List<Modifier>();
    private int minimumDelta, maximumDelta;

    public EnemySkill_SO.DicePowerRange BaseRange { get; }
    public EnemySkill_SO.DicePowerRange Range { get; private set; }
    public IReadOnlyList<Modifier> Modifiers => modifiers;
    public bool WillRoll { get; }

    public BattleDiceRangeBreakdown(EnemySkill_SO.DicePowerRange baseRange, bool willRoll)
    {
        BaseRange = baseRange;
        Range = baseRange;
        WillRoll = willRoll;
    }

    public void Add(string name, Sprite icon, int minimum, int maximum, bool isSkill = false, bool clashOnly = false)
    {
        if (minimum == 0 && maximum == 0)
            return;

        modifiers.Add(new Modifier { Name = name, Icon = icon, Minimum = minimum, Maximum = maximum, IsSkill = isSkill, ClashOnly = clashOnly });
        minimumDelta += minimum;
        maximumDelta += maximum;
    }

    public void AddStatus(BattleStatusRuntimeEntry entry, int minimum, int maximum)
    {
        if (entry != null && (minimum != 0 || maximum != 0))
            Add($"{entry.statusData.LocalizedName} ×{entry.stacks}", entry.statusData.IconSprite, minimum, maximum);
    }

    public void SetFinalRange(EnemySkill_SO.DicePowerRange range)
    {
        // Keep the displayed deltas additive even when the combat calculation clamps an endpoint.
        Add(GameText.LanguageCode == GameText.English ? "Range limits" : "范围校正", null,
            range.minPower - BaseRange.minPower - minimumDelta, range.maxPower - BaseRange.maxPower - maximumDelta);
        Range = range;
    }
}
