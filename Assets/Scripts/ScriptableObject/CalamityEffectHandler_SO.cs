using UnityEngine;

public abstract class CalamityEffectHandler_SO : ScriptableObject
{
    public virtual void OnCalamityApplied(RunState runState, RunGameManager runGameManager)
    {
    }

    public virtual int ModifyEnemyMaxHP(int currentValue, RunCalamityState calamityState)
    {
        return currentValue;
    }

    public virtual int ModifyEnemyDamage(int currentValue, RunCalamityState calamityState)
    {
        return currentValue;
    }

    public virtual int ModifyBattleExperienceReward(int currentValue, RunCalamityState calamityState)
    {
        return currentValue;
    }

    public virtual int ModifyBattleMoneyReward(int currentValue, RunCalamityState calamityState)
    {
        return currentValue;
    }

    public virtual int ModifyMerchantPrice(int currentValue, RunCalamityState calamityState)
    {
        return currentValue;
    }

    public virtual int ModifyAllyDamage(int currentValue, RunCalamityState calamityState)
    {
        return currentValue;
    }

    public virtual int ModifyAllyMaxHP(int currentValue, RunCalamityState calamityState)
    {
        return currentValue;
    }

    public virtual int ModifyEnemyIncomingDamage(int currentValue, RunCalamityState calamityState)
    {
        return currentValue;
    }
}
