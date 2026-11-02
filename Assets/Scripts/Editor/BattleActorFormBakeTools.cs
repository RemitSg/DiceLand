using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>Deterministic, color-independent form data. Does not alter source images or their importers.</summary>
public static class BattleActorFormBakeTools
{
    public const string Folder = BattleLookPreviewTools.ForestAssets + "/Actor Forms";
    public const string LibraryPath = Folder + "/Forest Actor Forms.asset";
    private const int LongEdge = 256;

    private static readonly string[] Sources =
    {
        "Assets/Data/Characters/Man/Man_InBattle.png",
        "Assets/Assets/Textures/Characters/Molly/Molly_InBattle.png",
        "Assets/Assets/Textures/Characters/Chara/Chara_InBattle.png",
        "Assets/Assets/Textures/Characters/Jenny/Jenny_InBattle.png",
        "Assets/Data/Enemies/Enemies/037 BlackCoffin/黑棺_x4.png",
        "Assets/Data/Weapons/204 猎光者/猎光者_x4.png",
        "Assets/Data/Weapons/303 荼蘼/荼蘼_x4.png"
    };

    [MenuItem("Tools/Battle Visuals/Bake Forest Actor Forms")]
    public static void BakeLibrary()
    {
        if (!AssetDatabase.IsValidFolder(Folder))
            AssetDatabase.CreateFolder(BattleLookPreviewTools.ForestAssets, "Actor Forms");
        var maps = new List<BattleActorFormLibrary.Entry>();
        foreach (string sourcePath in Sources)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(sourcePath);
            if (sprite == null)
                throw new InvalidOperationException("Missing form source: " + sourcePath);
            Texture2D source = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            Texture2D form = null;
            try
            {
                if (!ImageConversion.LoadImage(source, File.ReadAllBytes(sourcePath), false))
                    throw new InvalidOperationException("Cannot read source alpha: " + sourcePath);
                float scale = (float)LongEdge / Mathf.Max(source.width, source.height);
                int width = Mathf.Max(4, Mathf.RoundToInt(source.width * scale));
                int height = Mathf.Max(4, Mathf.RoundToInt(source.height * scale));
                var inside = new bool[width * height];
                for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    inside[y * width + x] = source.GetPixelBilinear((x + 0.5f) / width, (y + 0.5f) / height).a >= 0.5f;
                form = new Texture2D(width, height, TextureFormat.RGBA32, true, true)
                {
                    name = sprite.name + " silhouette form",
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Trilinear,
                    anisoLevel = 1
                };
                form.SetPixels32(BakeNormalPixels(inside, width, height));
                form.Apply(true, true);
                string mapPath = Folder + "/" + sprite.name + " Form.asset";
                Texture2D existing = AssetDatabase.LoadAssetAtPath<Texture2D>(mapPath);
                if (existing == null)
                {
                    AssetDatabase.CreateAsset(form, mapPath);
                    existing = form;
                    form = null;
                }
                else
                {
                    EditorUtility.CopySerialized(form, existing);
                    EditorUtility.SetDirty(existing);
                    AssetDatabase.SaveAssetIfDirty(existing);
                }
                maps.Add(new BattleActorFormLibrary.Entry { sprite = sprite, formMap = existing });
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(source);
                if (form != null)
                    UnityEngine.Object.DestroyImmediate(form);
            }
        }
        BattleActorFormLibrary library = AssetDatabase.LoadAssetAtPath<BattleActorFormLibrary>(LibraryPath);
        if (library == null)
        {
            library = ScriptableObject.CreateInstance<BattleActorFormLibrary>();
            AssetDatabase.CreateAsset(library, LibraryPath);
        }
        SerializedObject serialized = new SerializedObject(library);
        SerializedProperty entries = serialized.FindProperty("entries");
        entries.arraySize = maps.Count;
        for (int i = 0; i < maps.Count; i++)
        {
            entries.GetArrayElementAtIndex(i).FindPropertyRelative("sprite").objectReferenceValue = maps[i].sprite;
            entries.GetArrayElementAtIndex(i).FindPropertyRelative("formMap").objectReferenceValue = maps[i].formMap;
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(library);
        AssetDatabase.SaveAssetIfDirty(library);
    }

    public static Color32[] BakeNormalPixels(bool[] inside, int width, int height)
    {
        if (inside == null || width < 2 || height < 2 || inside.Length != width * height)
            throw new ArgumentException("A complete alpha mask of at least 2x2 is required.");
        var distance = new float[inside.Length];
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
            distance[y * width + x] = inside[y * width + x]
                ? Mathf.Min(x + 1, y + 1, width - x, height - y) : 0f;
        const float diagonal = 1.41421356f;
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            int i = y * width + x;
            if (x > 0) distance[i] = Mathf.Min(distance[i], distance[i - 1] + 1f);
            if (y > 0) distance[i] = Mathf.Min(distance[i], distance[i - width] + 1f);
            if (x > 0 && y > 0) distance[i] = Mathf.Min(distance[i], distance[i - width - 1] + diagonal);
            if (x + 1 < width && y > 0) distance[i] = Mathf.Min(distance[i], distance[i - width + 1] + diagonal);
        }
        for (int y = height - 1; y >= 0; y--)
        for (int x = width - 1; x >= 0; x--)
        {
            int i = y * width + x;
            if (x + 1 < width) distance[i] = Mathf.Min(distance[i], distance[i + 1] + 1f);
            if (y + 1 < height) distance[i] = Mathf.Min(distance[i], distance[i + width] + 1f);
            if (x + 1 < width && y + 1 < height) distance[i] = Mathf.Min(distance[i], distance[i + width + 1] + diagonal);
            if (x > 0 && y + 1 < height) distance[i] = Mathf.Min(distance[i], distance[i + width - 1] + diagonal);
        }
        float radius = Mathf.Max(2f, Mathf.Max(width, height) / 24f);
        for (int i = 0; i < distance.Length; i++)
            distance[i] = radius * 1.6f * (1f - Mathf.Exp(-distance[i] / radius));
        // Broad forms follow limbs and silhouette holes without embossing painted lines or pigment changes.
        float[] smooth = Blur(Blur(distance, width, height, true), width, height, false);
        smooth = Blur(Blur(smooth, width, height, true), width, height, false);
        var pixels = new Color32[inside.Length];
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            float dx = (smooth[y * width + Mathf.Min(width - 1, x + 1)] - smooth[y * width + Mathf.Max(0, x - 1)]) * 0.5f;
            float dy = (smooth[Mathf.Min(height - 1, y + 1) * width + x] - smooth[Mathf.Max(0, y - 1) * width + x]) * 0.5f;
            Vector3 normal = new Vector3(-dx, -dy, 1f).normalized;
            pixels[y * width + x] = new Color(normal.x * 0.5f + 0.5f, normal.y * 0.5f + 0.5f,
                normal.z * 0.5f + 0.5f, 1f);
        }
        return pixels;
    }

    private static float[] Blur(float[] source, int width, int height, bool horizontal)
    {
        var result = new float[source.Length];
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            float sum = 0f;
            for (int offset = -2; offset <= 2; offset++)
            {
                int sx = horizontal ? Mathf.Clamp(x + offset, 0, width - 1) : x;
                int sy = horizontal ? y : Mathf.Clamp(y + offset, 0, height - 1);
                sum += source[sy * width + sx];
            }
            result[y * width + x] = sum / 5f;
        }
        return result;
    }
}
