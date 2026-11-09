using UnityEngine;

/// <summary>Each event family has its own painting; explicit branch illustrations take precedence.</summary>
public static class MapEventSceneArt
{
    public static Sprite Resolve(MapEvent_SO data)
    {
        if (data != null && data.eventIllustration != null) return data.eventIllustration;
        if (data != null && !string.IsNullOrEmpty(data.eventId))
        {
            string family = data.eventId.Split(new[] { '_', '-' })[0];
            if (int.TryParse(family, out int number))
            {
                Sprite scene = Resources.Load<Sprite>("EventUI/Scenes/" + number.ToString("D3"));
                if (scene != null) return scene;
            }
        }
        return MapForestArt.Background(MapBackgroundPhase.Morning);
    }
}
