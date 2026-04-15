using System;
using UnityEngine;

[DisallowMultipleComponent]
public class ActionPointSystem : MonoBehaviour
{
    public int BasePoints { get; private set; }
    public int BonusPoints { get; private set; }
    public int CurrentPoints { get; private set; }
    public int CurrentMaxPoints => BasePoints + BonusPoints;

    public event Action<int, int> ActionPointsChanged;
    public event Action ActionPointsExhausted;

    public void Initialize(int startingPoints)
    {
        BasePoints = Mathf.Max(1, startingPoints);
        BonusPoints = 0;
        CurrentPoints = BasePoints;
        NotifyChanged();
    }

    public void SetBasePoints(int basePoints, bool clampCurrentPoints = true)
    {
        BasePoints = Mathf.Max(1, basePoints);
        if (clampCurrentPoints)
            CurrentPoints = Mathf.Clamp(CurrentPoints, 0, CurrentMaxPoints);
        NotifyChanged();
    }

    public bool CanSpend(int amount)
    {
        return CurrentPoints >= Mathf.Max(0, amount);
    }

    public bool TrySpend(int amount)
    {
        int safeAmount = Mathf.Max(0, amount);
        if (safeAmount == 0)
            return true;

        if (CurrentPoints < safeAmount)
            return false;

        CurrentPoints -= safeAmount;
        NotifyChanged();

        if (CurrentPoints <= 0)
            ActionPointsExhausted?.Invoke();

        return true;
    }

    public void AdvanceCycle(int extraPoints)
    {
        BonusPoints += Mathf.Max(0, extraPoints);
        CurrentPoints = CurrentMaxPoints;
        NotifyChanged();
    }

    public void RestoreState(int basePoints, int bonusPoints, int currentPoints)
    {
        BasePoints = Mathf.Max(1, basePoints);
        BonusPoints = Mathf.Max(0, bonusPoints);
        CurrentPoints = Mathf.Clamp(currentPoints, 0, CurrentMaxPoints);
        NotifyChanged();
    }

    private void NotifyChanged()
    {
        ActionPointsChanged?.Invoke(CurrentPoints, CurrentMaxPoints);
    }
}
