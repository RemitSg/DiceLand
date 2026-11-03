using System;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>Approximate paving bevels from the painted joints; source art remains unchanged.</summary>
public static class BattleForestPavingBakeTools
{
    public const string MapPath = BattleLookPreviewTools.ForestAssets + "/Forest Paving Relief.asset";
    private const int Size = 512;
    private const float RepeatsPerMetre = 0.16f;

    [MenuItem("Tools/Battle Visuals/Apply Paving Surface Lighting")]
    public static void ApplyToSavedMaterials()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Exit play mode before saving paving materials.");
        Texture2D map = BakeMap();
        foreach (string path in new[] { BattleLookPreviewTools.GroundPath,
                     BattleLookPreviewTools.ForestAssets + "/Forest Terrace Stone.mat" })
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) throw new InvalidOperationException("Missing paving material: " + path);
            Configure(material, map);
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssetIfDirty(material);
        }
    }

    public static void Configure(Material material, Texture2D map)
    {
        material.SetTexture("_PavingReliefMap", map);
        material.SetFloat("_PavingReliefStrength", 1f);
        material.SetFloat("_PavingGlintStrength", 0.12f);
        material.SetFloat("_PavingLocalGlintStrength", 0.22f);
    }

    public static Texture2D BakeMap(string assetPath = MapPath)
    {
        Texture2D source = new Texture2D(2,2,TextureFormat.RGBA32,false,true);
        Texture2D map = null;
        try
        {
            if (!ImageConversion.LoadImage(source, File.ReadAllBytes(BattleLookPreviewTools.StoneTexturePath)))
                throw new InvalidOperationException("Cannot read paving source.");
            var colors = new Color32[Size * Size];
            for (int y = 0; y < Size; y++)
            for (int x = 0; x < Size; x++)
                colors[y * Size + x] = source.GetPixelBilinear((x + 0.5f) / Size, (y + 0.5f) / Size);
            map = new Texture2D(Size,Size,TextureFormat.RGBA32,true,true)
            {
                name = "Forest Paving Relief", filterMode = FilterMode.Trilinear,
                wrapMode = TextureWrapMode.Repeat, anisoLevel = 4
            };
            map.SetPixels32(BakePixels(colors,Size));
            map.Apply(true,true);
            Texture2D existing = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(map,assetPath);
                map = null;
                AssetDatabase.ImportAsset(assetPath,ImportAssetOptions.ForceSynchronousImport);
                existing = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
                if (existing == null) throw new InvalidOperationException("Cannot register the paving data asset.");
            }
            else
            {
                EditorUtility.CopySerialized(map,existing);
                EditorUtility.SetDirty(existing);
                AssetDatabase.SaveAssetIfDirty(existing);
            }
            return existing;
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(source);
            if (map != null) UnityEngine.Object.DestroyImmediate(map);
        }
    }

    public static Color32[] BakePixels(Color32[] colors, int size)
    {
        if (size < 16 || size > 1024 || colors == null || colors.Length != size * size)
            throw new ArgumentException("A complete square paving sample of 16–1024 pixels is required.");
        bool[] stone = new bool[colors.Length];
        for (int i = 0; i < colors.Length; i++)
        {
            Color32 c = colors[i];
            float luminance = (c.r * .2126f + c.g * .7152f + c.b * .0722f) / 255f;
            stone[i] = luminance > .42f && c.b > .70f * c.g;
        }
        // Periodic closing removes thin painted cracks while retaining the wider soil joints.
        stone = Morph(Morph(stone,size,true),size,false);
        float[] distance = new float[colors.Length];
        for (int i = 0; i < distance.Length; i++) distance[i] = stone[i] ? size / 16f : 0;
        for (int iteration = 0; iteration < 4; iteration++)
        for (int direction = -1; direction <= 1; direction += 2)
        for (int row = 0; row < size; row++)
        for (int column = 0; column < size; column++)
        {
            int x = direction > 0 ? column : size - 1 - column;
            int y = direction > 0 ? row : size - 1 - row;
            int i = y * size + x;
            if (!stone[i]) continue;
            for (int dy = -1; dy <= 1; dy++)
            for (int dx = -1; dx <= 1; dx++)
                if (dx != 0 || dy != 0)
                    distance[i] = Mathf.Min(distance[i], distance[Index(x+dx,y+dy,size)]
                        + (dx != 0 && dy != 0 ? 1.41421356f : 1f));
        }
        float[] height = new float[colors.Length];
        for (int i = 0; i < height.Length; i++)
            height[i] = 1f - Mathf.Exp(-distance[i] / (size * RepeatsPerMetre * .06103515625f));
        float[] smooth = new float[height.Length];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        for (int dy = -1; dy <= 1; dy++)
        for (int dx = -1; dx <= 1; dx++)
            smooth[y*size+x] += height[Index(x+dx,y+dy,size)] / 9f;
        Color32[] pixels = new Color32[height.Length];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float slope = .075f * size * RepeatsPerMetre * .5f;
            Vector3 normal = new Vector3(-(smooth[Index(x+1,y,size)]-smooth[Index(x-1,y,size)])*slope,
                1f, -(smooth[Index(x,y+1,size)]-smooth[Index(x,y-1,size)])*slope).normalized;
            float h = smooth[y*size+x];
            pixels[y*size+x] = new Color(normal.x*.5f+.5f, normal.z*.5f+.5f, .78f+.22f*h, h);
        }
        return pixels;
    }

    private static int Index(int x, int y, int size) => ((y+size)%size)*size+(x+size)%size;

    private static bool[] Morph(bool[] source, int size, bool dilate)
    {
        bool[] result = new bool[source.Length];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            bool value = !dilate;
            for (int dy = -1; dy <= 1; dy++)
            for (int dx = -1; dx <= 1; dx++)
                if (dilate) value |= source[Index(x+dx,y+dy,size)];
                else value &= source[Index(x+dx,y+dy,size)];
            result[y*size+x] = value;
        }
        return result;
    }
}
