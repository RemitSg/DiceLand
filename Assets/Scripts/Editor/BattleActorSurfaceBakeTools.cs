using System;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>Linear material data and local curves from authored source-space regions.</summary>
public static class BattleActorSurfaceBakeTools
{
    private const int LongEdge = 256;
    private static readonly Color Cloth = new Color(0f, 0f, 1f, 1f);
    private static readonly Color Skin = new Color(1f, 0f, 0.65f, 1f);
    private static readonly Color Metal = new Color(0f, 1f, 0.32f, 1f);
    private static readonly Color Leather = new Color(0f, 0f, 0.78f, 1f);
    public const string ManPath = "Assets/Data/Characters/Man/Man_InBattle.png";
    public const string MollyPath = "Assets/Assets/Textures/Characters/Molly/Molly_InBattle.png";

    private sealed class Region
    {
        public Color value;
        public Vector2[] points;
    }

    private struct Volume
    {
        public Vector2 start, end, radius;
        public float weight;
        public Volume(float x, float y, float endX, float endY, float radiusX, float radiusY, float strength)
        {
            start = new Vector2(x,y); end = new Vector2(endX,endY);
            radius = new Vector2(radiusX,radiusY); weight = strength;
        }
    }

    [MenuItem("Tools/Battle Visuals/Bake Forest Actor Materials")]
    public static void BakeLibrary()
    {
        BattleActorFormBakeTools.BakeLibrary();
        BattleActorFormLibrary library = AssetDatabase.LoadAssetAtPath<BattleActorFormLibrary>(BattleActorFormBakeTools.LibraryPath);
        if (library == null) throw new InvalidOperationException("Cannot create the forest actor form library.");
        SerializedObject serialized = new SerializedObject(library);
        SerializedProperty entries = serialized.FindProperty("entries");
        for (int i = 0; i < entries.arraySize; i++)
        {
            SerializedProperty entry = entries.GetArrayElementAtIndex(i);
            Sprite sprite = entry.FindPropertyRelative("sprite").objectReferenceValue as Sprite;
            string path = AssetDatabase.GetAssetPath(sprite);
            if (path != ManPath && path != MollyPath) continue;
            Texture2D source = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            Texture2D map = null;
            try
            {
                if (!ImageConversion.LoadImage(source, File.ReadAllBytes(path), false))
                    throw new InvalidOperationException("Cannot read material source: " + path);
                float scale = (float)LongEdge / Mathf.Max(source.width, source.height);
                int width = Mathf.Max(4, Mathf.RoundToInt(source.width * scale));
                int height = Mathf.Max(4, Mathf.RoundToInt(source.height * scale));
                map = new Texture2D(width, height, TextureFormat.RGBA32, true, true)
                {
                    name = sprite.name + " material regions", filterMode = FilterMode.Trilinear,
                    wrapMode = TextureWrapMode.Clamp, anisoLevel = 1
                };
                Color32[] pixels = BakePixels(path, width, height);
                for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    pixels[y * width + x].a = source.GetPixelBilinear((x + 0.5f) / width, (y + 0.5f) / height).a >= 0.5f
                        ? (byte)255 : (byte)0;
                map.SetPixels32(pixels); map.Apply(true, true);
                string mapPath = BattleActorFormBakeTools.Folder + "/" + sprite.name + " Surface.asset";
                Texture2D existing = AssetDatabase.LoadAssetAtPath<Texture2D>(mapPath);
                if (existing == null)
                {
                    AssetDatabase.CreateAsset(map, mapPath); existing = map; map = null;
                }
                else
                {
                    EditorUtility.CopySerialized(map, existing); EditorUtility.SetDirty(existing);
                    AssetDatabase.SaveAssetIfDirty(existing);
                }
                entry.FindPropertyRelative("materialMap").objectReferenceValue = existing;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(source);
                if (map != null) UnityEngine.Object.DestroyImmediate(map);
            }
        }
        serialized.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(library);
        AssetDatabase.SaveAssetIfDirty(library);
    }

    public static Color32[] BakePixels(string sourcePath, int width, int height)
    {
        if (width < 2 || height < 2) throw new ArgumentException("A material map needs at least 2x2 pixels.");
        Region[] regions = Regions(sourcePath);
        if (regions == null) throw new ArgumentException("No authored material regions for " + sourcePath);
        Color32[] pixels = new Color32[width * height];
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            Vector2 point = new Vector2((x + 0.5f) / width, 1f - (y + 0.5f) / height);
            Color value = Cloth;
            foreach (Region region in regions)
                value = Color.Lerp(value, region.value, Coverage(point, region.points));
            pixels[y * width + x] = value;
        }
        return pixels;
    }

    public static void RefineFormNormals(string sourcePath, Color32[] pixels, int width, int height)
    {
        Volume[] volumes = Volumes(sourcePath);
        if (volumes == null) return;
        if (pixels == null || pixels.Length != width * height)
            throw new ArgumentException("A complete form map is required.");
        Vector2 reference = sourcePath == ManPath ? new Vector2(1230,1478) : new Vector2(1280,1941);
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            int index = y * width + x;
            if (pixels[index].a == 0) continue;
            Vector2 point = new Vector2((x + 0.5f) / width * reference.x, (1f - (y + 0.5f) / height) * reference.y);
            Vector3 normalSum = Vector3.zero;
            float total = 0f, strongest = 0f;
            foreach (Volume volume in volumes)
            {
                Vector2 start = Vector2.Scale(volume.start, new Vector2(1f / volume.radius.x,1f / volume.radius.y));
                Vector2 end = Vector2.Scale(volume.end, new Vector2(1f / volume.radius.x,1f / volume.radius.y));
                Vector2 local = Vector2.Scale(point, new Vector2(1f / volume.radius.x,1f / volume.radius.y));
                Vector2 axis = end - start;
                float along = Mathf.Clamp01(Vector2.Dot(local - start,axis) / Mathf.Max(0.00001f,axis.sqrMagnitude));
                Vector2 offset = local - Vector2.Lerp(start,end,along);
                float distance = offset.magnitude;
                float weight = (1f - Mathf.SmoothStep(0f,1f,Mathf.InverseLerp(0.65f,1.35f,distance))) * volume.weight;
                if (weight <= 0f) continue;
                Vector3 normal = new Vector3(offset.x * 0.82f, -offset.y * 0.82f,
                    Mathf.Sqrt(Mathf.Max(0.12f,1f - distance * distance * 0.68f))).normalized;
                normalSum += normal * weight; total += weight; strongest = Mathf.Max(strongest,weight);
            }
            if (total <= 0f) continue;
            Color32 pixel = pixels[index];
            Vector3 previous = new Vector3(pixel.r / 127.5f - 1f,pixel.g / 127.5f - 1f,pixel.b / 127.5f - 1f);
            Vector3 refined = Vector3.Lerp(previous,normalSum / total,strongest).normalized;
            Color32 result = new Color(refined.x * 0.5f + 0.5f,refined.y * 0.5f + 0.5f,refined.z * 0.5f + 0.5f,1f);
            result.a = pixel.a; pixels[index] = result;
        }
    }

    private static Volume[] Volumes(string path)
    {
        // Ellipsoid/capsule proxies follow painted body parts. They are not recovered anatomical geometry.
        if (path == ManPath) return new[]
        {
            new Volume(739,194,739,217,65,89,.90f), new Volume(605,238,648,364,51,65,.80f),
            new Volume(350,374,350,395,124,115,.95f), new Volume(290,446,191,547,74,74,.90f),
            new Volume(546,493,611,505,156,111,.70f), new Volume(821,276,821,300,78,125,.90f),
            new Volume(94,720,100,779,82,112,.86f), new Volume(291,1117,291,1138,89,95,.85f),
            new Volume(1027,1026,1027,1048,76,80,.85f), new Volume(642,729,657,735,71,64,.80f)
        };
        if (path == MollyPath) return new[]
        {
            new Volume(787,280,793,330,55,70,.90f), new Volume(650,333,680,357,45,37,.72f),
            new Volume(618,495,631,521,78,66,.95f), new Volume(645,587,713,628,45,45,.82f),
            new Volume(817,616,930,616,46,44,.90f), new Volume(491,599,458,681,33,36,.88f)
        };
        return null;
    }

    private static float Coverage(Vector2 point, Vector2[] polygon)
    {
        bool inside = false;
        float distance = float.MaxValue;
        for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
        {
            Vector2 a = polygon[j], b = polygon[i], edge = b - a;
            if ((a.y > point.y) != (b.y > point.y)
                && point.x < (b.x - a.x) * (point.y - a.y) / (b.y - a.y) + a.x) inside = !inside;
            float t = Mathf.Clamp01(Vector2.Dot(point - a, edge) / Mathf.Max(0.000001f, edge.sqrMagnitude));
            distance = Mathf.Min(distance, Vector2.Distance(point, a + edge * t));
        }
        return Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(0.5f + (inside ? distance : -distance) / 0.006f));
    }

    private static Region Shape(Color color, float width, float height, params float[] xy)
    {
        Vector2[] points = new Vector2[xy.Length / 2];
        for (int i = 0; i < points.Length; i++) points[i] = new Vector2(xy[i * 2] / width, xy[i * 2 + 1] / height);
        return new Region { value = color, points = points };
    }

    private static Region[] Regions(string path)
    {
        // Coordinates use the displayed source illustration, top-left origin. Later regions own overlaps.
        if (path == ManPath) return new[]
        {
            Shape(Skin,1230,1478, 549,222, 562,207, 586,202, 606,199, 622,190, 634,188, 641,201, 658,209, 675,213, 676,243, 692,263, 706,271, 714,277, 693,299, 695,311, 706,337, 720,356, 715,394, 682,432, 619,431, 581,379, 573,333, 563,285),
            Shape(Skin,1230,1478, 480,280, 515,310, 535,354, 575,412, 642,449, 707,413, 756,371, 785,322, 812,366, 822,414, 800,476, 762,520, 722,575, 665,620, 595,640, 537,615, 461,579, 421,548, 386,507, 350,547, 302,574, 252,590, 210,608, 199,645, 155,661, 123,625, 84,594, 54,583, 70,540, 105,505, 157,467, 196,431, 218,394, 244,350, 287,318, 340,296, 386,286, 427,293, 451,324, 483,323),
            Shape(Leather,1230,1478, 476,294, 510,311, 565,414, 610,449, 659,475, 691,508, 674,545, 618,508, 574,467, 530,425, 500,379),
            Shape(Leather,1230,1478, 744,369, 778,389, 736,463, 690,510, 642,549, 589,582, 528,591, 483,563, 526,540, 581,542, 637,509, 688,456),
            Shape(Skin,1230,1478, 56,822, 151,805, 198,827, 215,884, 213,924, 180,965, 126,987, 63,966, 11,930, 6,890),
            Shape(Skin,1230,1478, 936,537, 978,530, 1006,549, 1029,577, 1025,643, 997,678, 955,707, 916,688, 898,640, 902,589),
            Shape(Metal,1230,1478, 808,158, 825,173, 870,198, 885,237, 903,280, 902,339, 863,345, 835,392, 800,400, 756,414, 722,382, 735,358, 725,315, 750,268, 778,274, 778,247, 786,223, 799,212, 788,195, 790,177, 800,167),
            Shape(Metal,1230,1478, 30,622, 85,605, 168,626, 211,670, 203,755, 151,817, 65,884, 21,923, 23,808, 2,730),
            Shape(Metal,1230,1478, 580,691, 642,663, 706,681, 748,727, 714,778, 649,805, 589,765),
            Shape(Metal,1230,1478, 216,1041, 290,1014, 371,1048, 394,1110, 342,1185, 287,1220, 235,1176, 207,1118),
            Shape(Metal,1230,1478, 973,974, 1042,967, 1100,1023, 1084,1085, 1049,1128, 1005,1113, 958,1076, 952,1028),
            Shape(Skin,1230,1478, 706,113, 729,103, 761,112, 783,127, 791,150, 797,185, 786,191, 785,205, 798,213, 786,221, 777,247, 778,274, 772,280, 738,279, 690,263, 679,248, 675,214, 660,207, 648,197, 644,177, 652,161, 663,160, 672,172, 682,184, 693,157, 695,140)
        };
        if (path == MollyPath) return new[]
        {
            Shape(Skin,1280,1941, 730,259, 759,223, 811,190, 837,221, 854,281, 841,310, 858,334, 840,348, 824,361, 822,383, 805,401, 766,394, 742,372, 714,342, 721,309),
            Shape(Skin,1280,1941, 653,305, 714,342, 736,371, 730,398, 648,373, 600,353, 617,331, 633,310),
            Shape(Skin,1280,1941, 650,239, 678,241, 694,262, 692,282, 676,296, 648,282, 638,261),
            Shape(Skin,1280,1941, 498,539, 547,578, 513,649, 492,681, 481,711, 460,718, 431,694, 426,681, 455,607),
            Shape(Skin,1280,1941, 1056,545, 1070,543, 1085,554, 1107,572, 1107,587, 1093,602, 1086,624, 1072,644, 1045,650, 1044,624, 1050,597),
            Shape(Metal,1280,1941, 548,454, 591,431, 648,452, 699,493, 721,530, 688,565, 624,586, 568,560, 535,532),
            Shape(Metal,1280,1941, 772,572, 839,568, 895,577, 945,575, 971,598, 976,622, 964,646, 927,668, 855,671, 815,650, 786,629),
            Shape(Metal,1280,1941, 784,804, 829,779, 871,822, 860,877, 821,900, 787,865)
        };
        return null;
    }
}
