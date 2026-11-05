using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class DisplayResolutionSelectionServiceTests
{
    private static readonly DisplayResolutionOption[] Modes =
    {
        new DisplayResolutionOption(1920, 1080, 144),
        new DisplayResolutionOption(2560, 1440, 120),
        new DisplayResolutionOption(1920, 1080, 60),
        new DisplayResolutionOption(1920, 1080, 120),
        new DisplayResolutionOption(1280, 720, 60)
    };

    [TestCase(60, 2)]
    [TestCase(120, 3)]
    [TestCase(144, 0)]
    [TestCase(100, 3)]
    [TestCase(50, 2)]
    [TestCase(240, 0)]
    public void SavedRefreshRateWinsRegardlessOfListOrderAndFallsBackToTheNearest(int hz, int expected)
    {
        Assert.AreEqual(expected, DisplayResolutionSelectionService.FindBestIndex(Modes,
            new DisplayResolutionOption(1920, 1080, hz)));
    }

    [Test]
    public void EquallyCloseModesPreferHigherHzInEitherOrder()
    {
        var modes = new[] { new DisplayResolutionOption(1920, 1080, 60), new DisplayResolutionOption(1920, 1080, 120) };
        var requested = new DisplayResolutionOption(1920, 1080, 90);
        Assert.AreEqual(1, DisplayResolutionSelectionService.FindBestIndex(modes, requested));
        Array.Reverse(modes);
        Assert.AreEqual(0, DisplayResolutionSelectionService.FindBestIndex(modes, requested));
    }

    [Test]
    public void MissingDimensionsAndEmptyModesRequestTheCallersDeviceFallback()
    {
        var requested = new DisplayResolutionOption(800, 600, 60);
        Assert.AreEqual(-1, DisplayResolutionSelectionService.FindBestIndex(Modes, requested));
        Assert.AreEqual(-1, DisplayResolutionSelectionService.FindBestIndex(Array.Empty<DisplayResolutionOption>(), requested));
        Assert.AreEqual(-1, DisplayResolutionSelectionService.FindBestIndex(null, requested));
    }

    [TestCase(60, 2)]
    [TestCase(120, 3)]
    [TestCase(144, 0)]
    [TestCase(100, 3)]
    public void ReopenedOverlayRestoresHzRatherThanTheFirstMatchingWidthAndHeight(int hz, int expected)
    {
        GameObject host = new GameObject("Resolution restoration test");
        try
        {
            var overlay = host.AddComponent<MainMenuSettingsOverlay>();
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            Type optionType = typeof(MainMenuSettingsOverlay).GetNestedType("RuntimeResolutionOption", BindingFlags.NonPublic);
            IList options = (IList)typeof(MainMenuSettingsOverlay).GetField("resolutionOptions", flags).GetValue(overlay);
            foreach (DisplayResolutionOption mode in Modes)
            {
                object option = Activator.CreateInstance(optionType, true);
                optionType.GetField("width").SetValue(option, mode.width);
                optionType.GetField("height").SetValue(option, mode.height);
                optionType.GetField("refreshRate").SetValue(option, mode.refreshRate);
                options.Add(option);
            }
            var saved = new MainMenuSettingsData { resolutionWidth = 1920, resolutionHeight = 1080, refreshRate = hz };
            Assert.AreEqual(expected, typeof(MainMenuSettingsOverlay).GetMethod("FindResolutionIndex", flags)
                .Invoke(overlay, new object[] { saved }));
        }
        finally { UnityEngine.Object.DestroyImmediate(host); }
    }
}
