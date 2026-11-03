using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

/// <summary>Imports the unchanged generated keyword sheet as ten named, tightly bounded sprites.</summary>
public static class KeywordIconImportTools
{
    private const string AtlasPath = "Assets/Resources/MapUI/KeywordIcons.png";
    private static readonly string[] Names =
        { "Flame", "Water", "Poison", "Blood", "Dark", "Electricity", "Light", "Speed", "Neutral", "Health" };

    [MenuItem("Tools/Map Visuals/Configure Attribute And Health Icons")]
    public static void ConfigureImports()
    {
        TextureImporter importer = AssetImporter.GetAtPath(AtlasPath) as TextureImporter;
        if (importer == null) throw new FileNotFoundException(AtlasPath);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = 100f;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.maxTextureSize = 2048;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.filterMode = FilterMode.Bilinear;
        importer.wrapMode = TextureWrapMode.Clamp;
        SpriteDataProviderFactories factories = new SpriteDataProviderFactories();
        factories.Init();
        ISpriteEditorDataProvider provider = factories.GetSpriteEditorDataProviderFromObject(importer);
        provider.InitSpriteEditorDataProvider();
        Dictionary<string, SpriteRect> previous = provider.GetSpriteRects().ToDictionary(rect => rect.name);
        Texture2D source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        try
        {
            source.LoadImage(File.ReadAllBytes(AtlasPath));
            Color32[] pixels = source.GetPixels32();
            SpriteRect[] entries = new SpriteRect[Names.Length];
            for (int i = 0; i < Names.Length; i++)
            {
                int column = i % 5, row = i / 5;
                int left = Mathf.RoundToInt(column * source.width / 5f), right = Mathf.RoundToInt((column + 1) * source.width / 5f);
                int bottom = source.height - Mathf.RoundToInt((row + 1) * source.height / 2f);
                int top = source.height - Mathf.RoundToInt(row * source.height / 2f);
                int minX = right, maxX = left, minY = top, maxY = bottom;
                for (int y = bottom; y < top; y++)
                    for (int x = left; x < right; x++)
                        if (pixels[y * source.width + x].a >= 32)
                        { minX = Mathf.Min(minX, x); maxX = Mathf.Max(maxX, x); minY = Mathf.Min(minY, y); maxY = Mathf.Max(maxY, y); }
                if (maxX <= minX || maxY <= minY) throw new InvalidDataException("Missing keyword icon " + Names[i]);
                minX = Mathf.Max(left, minX - 2); minY = Mathf.Max(bottom, minY - 2);
                maxX = Mathf.Min(right, maxX + 3); maxY = Mathf.Min(top, maxY + 3);
                entries[i] = new SpriteRect
                {
                    name = Names[i], rect = new Rect(minX, minY, maxX - minX, maxY - minY),
                    alignment = SpriteAlignment.Center, pivot = new Vector2(0.5f, 0.5f), border = Vector4.zero,
                    spriteID = previous.TryGetValue(Names[i], out SpriteRect existing) ? existing.spriteID : GUID.Generate()
                };
            }
            provider.SetSpriteRects(entries);
            ISpriteNameFileIdDataProvider nameIds = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
            if (nameIds != null)
            {
                Dictionary<string, SpriteNameFileIdPair> previousIds = nameIds.GetNameFileIdPairs().ToDictionary(pair => pair.name);
                nameIds.SetNameFileIdPairs(entries.Select(entry => previousIds.TryGetValue(entry.name, out SpriteNameFileIdPair pair)
                    ? pair : new SpriteNameFileIdPair(entry.name, entry.spriteID)));
            }
            provider.Apply();
            importer.SaveAndReimport();
            Debug.Log("Imported nine attribute icons and the health heart without changing source pixels.");
        }
        finally { UnityEngine.Object.DestroyImmediate(source); }
    }
}
