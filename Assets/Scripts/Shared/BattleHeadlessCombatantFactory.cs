using UnityEngine;

// Constructs the same runtime controllers without visual prefabs. No combat rules live here.
public static class BattleHeadlessCombatantFactory
{
    public static BattleAllyController CreateAlly(Transform parent, RunPartyMemberState member)
    {
        GameObject go = new GameObject(member.memberId);
        go.transform.SetParent(parent, false);
        BattleAllyController ally = go.AddComponent<BattleAllyController>();
        ally.ApplyRuntimeState(member);
        ally.RegisterSpeedButtons(new[] { CreateButton(go.transform, SpeedButton.ButtonType.Ally) });
        return ally;
    }

    public static BattleEnemyController CreateEnemy(Transform parent, EnemyData_SO data, int formationSlot)
    {
        GameObject go = new GameObject("Enemy_" + formationSlot);
        go.transform.SetParent(parent, false);
        BattleEnemyController enemy = go.AddComponent<BattleEnemyController>();
        enemy.ApplyEnemyData(data);
        enemy.AssignFormationSlotIndex(formationSlot);
        enemy.RegisterSpeedButtons(new[] { CreateButton(go.transform, SpeedButton.ButtonType.Enemy) });
        return enemy;
    }

    private static SpeedButton CreateButton(Transform parent, SpeedButton.ButtonType type)
    {
        GameObject go = new GameObject("SpeedButton");
        go.transform.SetParent(parent, false);
        SpeedButton button = go.AddComponent<SpeedButton>();
        button.ConfigureHeadlessExecution();
        button.SetButtonType(type);
        return button;
    }
}
