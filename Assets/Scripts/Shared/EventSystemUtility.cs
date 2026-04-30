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
