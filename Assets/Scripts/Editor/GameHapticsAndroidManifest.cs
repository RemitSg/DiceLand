using System.IO;
using System.Xml;

public static class GameHapticsAndroidManifest
{
    private const string AndroidNamespace = "http://schemas.android.com/apk/res/android";
    private const string VibrationPermission = "android.permission.VIBRATE";

    public static void EnsurePermission(string manifestPath)
    {
        var document = new XmlDocument();
        document.Load(manifestPath);
        XmlElement manifest = document.DocumentElement;
        foreach (XmlNode node in manifest.ChildNodes)
            if (node.LocalName == "uses-permission" && node is XmlElement permission &&
                permission.GetAttribute("name", AndroidNamespace) == VibrationPermission)
                return;

        XmlElement entry = document.CreateElement("uses-permission");
        XmlAttribute name = document.CreateAttribute("android", "name", AndroidNamespace);
        name.Value = VibrationPermission;
        entry.Attributes.Append(name);
        manifest.AppendChild(entry);
        document.Save(manifestPath);
    }
}

#if UNITY_ANDROID
public sealed class GameHapticsAndroidBuildPostprocessor : UnityEditor.Android.IPostGenerateGradleAndroidProject
{
    public int callbackOrder => 100;

    public void OnPostGenerateGradleAndroidProject(string path)
    {
        GameHapticsAndroidManifest.EnsurePermission(Path.Combine(path, "src", "main", "AndroidManifest.xml"));
    }
}
#endif
