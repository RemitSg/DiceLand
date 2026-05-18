using System.Collections.Generic;
using UnityEngine;

public static class BattleSharedEnergyReservationCollectorService
{
    public static int CalculateReservedEnergyForAlly(BattleAllyController ally)
    {
        if (ally == null || ally.IsDead)
            return 0;

        int reservedEnergy = 0;
        IReadOnlyList<SpeedButton> allyButtons = BattleSpeedButtonListService.GetAllyButtons(ally);
        for (int i = 0; i < allyButtons.Count; i++)
        {
            SpeedButton allyButton = allyButtons[i];
            if (allyButton == null || allyButton.ConnectedTarget == null)
                continue;

            EnemySkill_SO selectedSkill = ally.GetSelectedSkillForButton(allyButton);
            reservedEnergy += Mathf.Max(0, selectedSkill != null ? selectedSkill.energyCost : 0);
        }

        return reservedEnergy;
    }

    public static int CalculateReservedEnergyForAllyButton(BattleAllyController ally, SpeedButton button)
    {
        if (ally == null || ally.IsDead || button == null || button.ConnectedTarget == null)
            return 0;

        EnemySkill_SO selectedSkill = ally.GetSelectedSkillForButton(button);
        return Mathf.Max(0, selectedSkill != null ? selectedSkill.energyCost : 0);
    }

    public static int CalculateConnectedAllyButtonCount(BattleAllyController ally)
    {
        if (ally == null || ally.IsDead)
            return 0;

        int connectedButtonCount = 0;
        IReadOnlyList<SpeedButton> allyButtons = BattleSpeedButtonListService.GetAllyButtons(ally);
        for (int i = 0; i < allyButtons.Count; i++)
        {
            SpeedButton allyButton = allyButtons[i];
            if (allyButton != null && allyButton.ConnectedTarget != null)
                connectedButtonCount++;
        }

        return connectedButtonCount;
    }

    public static int CalculateTotalReservedEnergy(IReadOnlyList<BattleAllyController> allies, int currentSharedEnergy)
    {
        int totalReserved = 0;
        if (allies != null)
        {
            for (int i = 0; i < allies.Count; i++)
                totalReserved += CalculateReservedEnergyForAlly(allies[i]);
        }

        return Mathf.Clamp(totalReserved, 0, Mathf.Max(0, currentSharedEnergy));
    }
}
