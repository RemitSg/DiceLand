using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Temporarily culls spectators without disabling combatants or their behaviours.</summary>
internal sealed class BattleCloseupVisibility
{
    private readonly Dictionary<Renderer, bool> originalVisibility = new Dictionary<Renderer, bool>();
    private readonly HashSet<Renderer> spectators = new HashSet<Renderer>();
    private readonly HashSet<Transform> spectatorActors = new HashSet<Transform>();
    private readonly List<Renderer> renderers = new List<Renderer>();
    private readonly List<Renderer> released = new List<Renderer>();
    private readonly List<GameObject> roots = new List<GameObject>();
    private readonly List<BattleAllyController> allies = new List<BattleAllyController>();
    private readonly List<BattleEnemyController> enemies = new List<BattleEnemyController>();

    public void Update(Scene scene, Transform first, Transform second)
    {
        if (!scene.IsValid() || !scene.isLoaded || (first == null && second == null))
        {
            Restore();
            return;
        }

        spectators.Clear();
        spectatorActors.Clear();
        // Reused lists also catch summons, replacement weapons and reparented visuals during a shot.
        scene.GetRootGameObjects(roots);
        foreach (GameObject root in roots)
        {
            root.GetComponentsInChildren(true, allies);
            foreach (BattleAllyController ally in allies)
                Collect(ally.transform, first, second);
            root.GetComponentsInChildren(true, enemies);
            foreach (BattleEnemyController enemy in enemies)
                Collect(enemy.transform, first, second);
        }

        released.Clear();
        foreach (KeyValuePair<Renderer, bool> entry in originalVisibility)
        {
            if (entry.Key != null && spectators.Contains(entry.Key))
                continue;
            if (entry.Key != null)
                entry.Key.forceRenderingOff = entry.Value;
            released.Add(entry.Key);
        }
        foreach (Renderer renderer in released)
            originalVisibility.Remove(renderer);
    }

    private void Collect(Transform actor, Transform first, Transform second)
    {
        if (ContainsParticipant(actor, first) || ContainsParticipant(actor, second))
            return;

        spectatorActors.Add(actor);
        actor.GetComponentsInChildren(true, renderers);
        foreach (Renderer renderer in renderers)
        {
            spectators.Add(renderer);
            if (!originalVisibility.ContainsKey(renderer))
                originalVisibility.Add(renderer, renderer.forceRenderingOff);
            renderer.forceRenderingOff = true;
        }
    }

    private static bool ContainsParticipant(Transform actor, Transform participant)
    {
        return participant != null && (actor == participant || participant.IsChildOf(actor) || actor.IsChildOf(participant));
    }

    public void Restore()
    {
        foreach (KeyValuePair<Renderer, bool> entry in originalVisibility)
            if (entry.Key != null)
                entry.Key.forceRenderingOff = entry.Value;
        originalVisibility.Clear();
        spectators.Clear();
        spectatorActors.Clear();
        renderers.Clear();
        released.Clear();
        roots.Clear();
        allies.Clear();
        enemies.Clear();
    }

    public bool IsSpectator(Transform actor) => actor != null && spectatorActors.Contains(actor);
}
