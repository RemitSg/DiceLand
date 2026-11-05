using System;
using System.IO;
using System.Xml;
using NUnit.Framework;

public sealed class GameHapticsAndroidManifestTests
{
    private string manifest;

    [SetUp]
    public void SetUp()
    {
        manifest = Path.Combine(Path.GetTempPath(), "diceland-haptics-" + Guid.NewGuid().ToString("N") + ".xml");
    }

    [TearDown]
    public void TearDown()
    {
        if (File.Exists(manifest)) File.Delete(manifest);
    }

    [Test]
    public void AddsNormalVibrationPermissionWithoutReplacingTheGeneratedApplication()
    {
        File.WriteAllText(manifest, "<manifest xmlns:android=\"http://schemas.android.com/apk/res/android\" package=\"test.game\">" +
            "<uses-permission android:name=\"android.permission.INTERNET\"/><application android:label=\"DiceLand\"/></manifest>");
        GameHapticsAndroidManifest.EnsurePermission(manifest);
        GameHapticsAndroidManifest.EnsurePermission(manifest);
        var document = new XmlDocument();
        document.Load(manifest);
        Assert.AreEqual(2, document.SelectNodes("/manifest/uses-permission").Count);
        Assert.AreEqual("DiceLand", ((XmlElement)document.SelectSingleNode("/manifest/application"))
            .GetAttribute("label", "http://schemas.android.com/apk/res/android"));
        Assert.AreEqual("android.permission.VIBRATE", ((XmlElement)document.SelectNodes("/manifest/uses-permission")[1])
            .GetAttribute("name", "http://schemas.android.com/apk/res/android"));
    }

    [Test]
    public void ExistingPermissionWithADifferentNamespacePrefixIsPreservedByteForByte()
    {
        const string original = "<manifest xmlns:a=\"http://schemas.android.com/apk/res/android\">" +
            "<uses-permission a:name=\"android.permission.VIBRATE\"/><application/></manifest>";
        File.WriteAllText(manifest, original);
        GameHapticsAndroidManifest.EnsurePermission(manifest);
        Assert.AreEqual(original, File.ReadAllText(manifest));
    }
}
