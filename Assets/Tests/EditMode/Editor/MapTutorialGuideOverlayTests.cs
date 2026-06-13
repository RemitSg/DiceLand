using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public class MapTutorialGuideOverlayTests
{
    [Test]
    public void Show_UsesConfiguredFontsForTutorialContent()
    {
        GameObject go = new GameObject("MapTutorialGuideOverlay");
        Font titleFont = new Font("CustomMapTutorialTitleFont");
        Font bodyFont = new Font("CustomMapTutorialBodyFont");
        Font buttonFont = new Font("CustomMapTutorialButtonFont");

        try
        {
            MapTutorialGuideOverlay overlay = go.AddComponent<MapTutorialGuideOverlay>();
            overlay.ConfigureFonts(titleFont, bodyFont, buttonFont);

            overlay.Show("Title", "Body", null, "Continue");

            Assert.AreSame(titleFont, FindText(go, "Title").font);
            Assert.AreSame(bodyFont, FindText(go, "Body").font);
            Assert.AreSame(buttonFont, FindText(go, "Label").font);
        }
        finally
        {
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(titleFont);
            Object.DestroyImmediate(bodyFont);
            Object.DestroyImmediate(buttonFont);
        }
    }

    [Test]
    public void Show_UsesConfiguredBodyFontForButtonWhenButtonFontMissing()
    {
        GameObject go = new GameObject("MapTutorialGuideOverlay");
        Font bodyFont = new Font("CustomMapTutorialBodyFont");

        try
        {
            MapTutorialGuideOverlay overlay = go.AddComponent<MapTutorialGuideOverlay>();
            overlay.ConfigureFonts(null, bodyFont, null);

            overlay.Show("Title", "Body", null, "Continue");

            Assert.AreSame(bodyFont, FindText(go, "Body").font);
            Assert.AreSame(bodyFont, FindText(go, "Label").font);
        }
        finally
        {
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(bodyFont);
        }
    }

    private static Text FindText(GameObject root, string objectName)
    {
        Text[] texts = root.GetComponentsInChildren<Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i].gameObject.name == objectName)
                return texts[i];
        }

        Assert.Fail("Missing text object: " + objectName);
        return null;
    }
}
