using UnityEngine;
using UnityEngine.SceneManagement;

public static class MapSceneBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureMapSceneManager()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (!activeScene.IsValid() || !activeScene.name.Equals("MapScene", System.StringComparison.OrdinalIgnoreCase))
            return;

        if (Object.FindObjectOfType<RunGameManager>() != null)
            return;

        GameObject root = new GameObject("MapSceneRoot");
        root.AddComponent<RunGameManager>();
    }
}
