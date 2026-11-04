using UnityEngine;
using UnityEngine.EventSystems;

public static class EventSystemUtility
{
    public static EventSystem EnsureSingleEventSystem()
    {
        EventSystem[] systems = Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (systems == null || systems.Length == 0)
        {
            GameObject eventSystemGo = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            return eventSystemGo.GetComponent<EventSystem>();
        }

        EventSystem preferred = ChoosePreferredSystem(systems);
        // Editor previews and EditMode tests must not delete or mutate the user's loaded scene.
        // Only the running game owns the lifetime and configuration of duplicate event systems.
        if (!Application.isPlaying) return preferred;
        for (int i = 0; i < systems.Length; i++)
        {
            EventSystem system = systems[i];
            if (system == null || system == preferred)
                continue;

            Object.Destroy(system.gameObject);
        }

        if (!preferred.gameObject.activeSelf)
            preferred.gameObject.SetActive(true);

        if (!preferred.enabled)
            preferred.enabled = true;

        BaseInputModule inputModule = preferred.GetComponent<BaseInputModule>();
        if (inputModule == null)
            inputModule = preferred.gameObject.AddComponent<StandaloneInputModule>();
        if (!inputModule.enabled)
            inputModule.enabled = true;

        if (Application.isPlaying && EventSystem.current != preferred)
            EventSystem.current = preferred;
        return preferred;
    }

    private static EventSystem ChoosePreferredSystem(EventSystem[] systems)
    {
        string activeSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        EventSystem current = EventSystem.current;
        if (current != null && current.gameObject.scene.name == activeSceneName)
            return current;

        for (int i = 0; i < systems.Length; i++)
        {
            EventSystem system = systems[i];
            if (system != null && system.gameObject.scene.name == activeSceneName)
                return system;
        }

        return current != null ? current : systems[0];
    }
}
