using NUnit.Framework;
using UnityEngine;

public class MapEventOverlayTests
{
    [Test]
    public void Show_CreatesCanvasAboveMapHud()
    {
        GameObject go = new GameObject("MapEventOverlay");
        try
        {
            MapEventOverlay overlay = go.AddComponent<MapEventOverlay>();

            overlay.Show(null, null, null);

            Canvas canvas = go.GetComponentInChildren<Canvas>(true);
            Assert.IsNotNull(canvas);
            Assert.Greater(canvas.sortingOrder, 8000);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }
}
