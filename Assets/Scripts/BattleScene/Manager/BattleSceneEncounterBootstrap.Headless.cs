using System.Collections.Generic;
using UnityEngine;

public partial class BattleSceneEncounterBootstrap
{
    private bool headlessExecution;
    private readonly List<ScriptableObject> headlessDefinitions = new List<ScriptableObject>();

    public void ConfigureHeadlessExecution(BattleManager manager)
    {
        headlessExecution = true;
        battleManager = manager;
        alliesParent = spawnedAlliesRoot = manager.alliesParent.transform;
        enemiesParent = spawnedEnemiesRoot = manager.enemiesParent.transform;
        // No rendered formation anchors: use the existing fallback formation policy.
        anchorRoot = manager.transform;
    }

    public void ReleaseHeadlessDefinitions()
    {
        foreach (ScriptableObject definition in headlessDefinitions)
            if (definition != null) DestroyImmediate(definition);
        headlessDefinitions.Clear();
    }
}
