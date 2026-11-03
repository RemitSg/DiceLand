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
    public const string CoffinPath = "Assets/Data/Enemies/Enemies/037 BlackCoffin/黑棺_x4.png";
    public const string AxePath = "Assets/Data/Weapons/204 猎光者/猎光者_x4.png";
    public const int MollyMapLongEdge = 512;
    public const int AxeMapLongEdge = 512;

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
            if (path != ManPath && path != MollyPath && path != CoffinPath && path != AxePath) continue;
            Texture2D source = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            Texture2D map = null;
            try
            {
                if (!ImageConversion.LoadImage(source, File.ReadAllBytes(path), false))
                    throw new InvalidOperationException("Cannot read material source: " + path);
                int longEdge = path == MollyPath ? MollyMapLongEdge : path == AxePath ? AxeMapLongEdge : path == CoffinPath ? 512 : LongEdge;
                float scale = (float)longEdge / Mathf.Max(source.width, source.height);
                int width = Mathf.Max(4, Mathf.RoundToInt(source.width * scale));
                int height = Mathf.Max(4, Mathf.RoundToInt(source.height * scale));
                map = new Texture2D(width, height, TextureFormat.RGBA32, true, true)
                {
                    name = sprite.name + (path == CoffinPath || path == MollyPath || path == AxePath ? " Surface" : " material regions"), filterMode = FilterMode.Trilinear,
                    wrapMode = TextureWrapMode.Clamp, anisoLevel = 1
                };
                Color32[] pixels = BakePixels(path, width, height);
                for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    int index=y*width+x;
                    bool inside=source.GetPixelBilinear((x + 0.5f) / width, (y + 0.5f) / height).a >= 0.5f;
                    pixels[index].a=inside?(path==CoffinPath||path==AxePath?pixels[index].a:(byte)255):(byte)0;
                }
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
                entry.FindPropertyRelative("emissiveAlpha").boolValue = path == CoffinPath || path == AxePath;
                entry.FindPropertyRelative("fabricSheen").boolValue = path == ManPath || path == MollyPath;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(source);
                if (map != null) UnityEngine.Object.DestroyImmediate(map);
            }
        }
        serialized.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(library);
        AssetDatabase.SaveAssetIfDirty(library);
        ConfigureEmissionLights(library);
    }

    public static void ConfigureEmissionLights(BattleActorFormLibrary library)
    {
        if(library==null)throw new ArgumentNullException(nameof(library));
        SerializedObject serialized = new SerializedObject(library);
        SerializedProperty entries = serialized.FindProperty("entries");
        for(int i=0;i<entries.arraySize;i++)
        {
            SerializedProperty entry = entries.GetArrayElementAtIndex(i);
            string path=AssetDatabase.GetAssetPath(entry.FindPropertyRelative("sprite").objectReferenceValue);
            if(path==CoffinPath) BattleActorFormBakeTools.WriteEmissionLights(entry,new[]
            {
                new BattleActorFormLibrary.EmissionLight { uv=new Vector2(.404f,.655f),color=new Color(1f,.035f,.38f),range=.65f,intensity=.12f },
                new BattleActorFormLibrary.EmissionLight { uv=new Vector2(.62f,.10f),color=new Color(.40f,.12f,1f),range=.42f,intensity=.035f }
            });
            else if(path==AxePath) BattleActorFormBakeTools.WriteEmissionLights(entry,new[]
            {
                new BattleActorFormLibrary.EmissionLight { uv=new Vector2(.474f,.738f),color=new Color(1f,.57f,.16f),range=.75f,intensity=.05f }
            });
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(library);AssetDatabase.SaveAssetIfDirty(library);
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
            Color value = sourcePath == CoffinPath ? new Color(0,0,0.90f,0)
                : sourcePath == AxePath ? new Color(0,1,0.36f,0) : Cloth;
            foreach (Region region in regions)
                value = Color.Lerp(value, region.value, Coverage(point, region.points));
            pixels[y * width + x] = value;
        }
        return pixels;
    }

    public static void RefineFormNormals(string sourcePath, Color32[] pixels, int width, int height)
    {
        if(sourcePath==CoffinPath||sourcePath==AxePath)
        {
            RefinePlanes(pixels,width,height,sourcePath==AxePath?AxeFaces():CoffinFaces());
            return;
        }
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
        if (sourcePath == MollyPath) RefinePlanes(pixels,width,height,MollyPlanes());
    }

    private static void RefinePlanes(Color32[] pixels,int width,int height,Region[] faces)
    {
        if(pixels==null||pixels.Length!=width*height)throw new ArgumentException("A complete form map is required.");
        for(int y=0;y<height;y++)for(int x=0;x<width;x++)
        {
            int i=y*width+x;Color32 pixel=pixels[i];if(pixel.a==0)continue;
            Vector2 point=new Vector2((x+0.5f)/width,1f-(y+0.5f)/height);
            Vector3 normal=new Vector3(pixel.r/127.5f-1,pixel.g/127.5f-1,pixel.b/127.5f-1);
            foreach(Region face in faces)
            {
                Vector3 target=new Vector3(face.value.r,face.value.g,face.value.b);
                normal=Vector3.Lerp(normal,target,Coverage(point,face.points)*face.value.a).normalized;
            }
            Color32 refined=new Color(normal.x*.5f+.5f,normal.y*.5f+.5f,normal.z*.5f+.5f,1);
            refined.a=pixel.a;pixels[i]=refined;
        }
    }

    private static Region[] MollyPlanes()
    {
        // Broad fold faces follow the painted coat and trousers, not pigment gradients.
        // Coordinates share the 1280x1941 illustration reference used by the material regions.
        return new[]
        {
            Shape(new Color(-.34f,.32f,.884f,.82f),1280,1941, 482,411, 560,359, 703,415, 781,449, 708,473, 565,425),
            Shape(new Color(.20f,.52f,.830f,.80f),1280,1941, 568,893, 645,862, 716,928, 636,971, 493,1008),
            Shape(new Color(-.42f,.08f,.904f,.84f),1280,1941, 706,951, 800,979, 729,1123, 687,1290, 665,1377, 685,1114),
            Shape(new Color(.48f,.23f,.847f,.88f),1280,1941, 800,979, 870,907, 865,1080, 810,1292, 665,1377, 687,1290, 729,1123),
            Shape(new Color(-.12f,.52f,.846f,.82f),1280,1941, 512,1040, 681,987, 681,1168, 456,1301, 300,1320, 377,1173),
            Shape(new Color(-.28f,-.30f,.912f,.74f),1280,1941, 300,1320, 456,1301, 681,1168, 480,1448, 304,1454),
            Shape(new Color(.10f,.57f,.815f,.84f),1280,1941, 895,961, 1017,985, 1162,1081, 1266,1157, 1181,1216, 1078,1153, 878,1109),
            Shape(new Color(-.22f,-.30f,.928f,.76f),1280,1941, 878,1109, 1078,1153, 1181,1216, 1240,1259, 1153,1336, 1061,1354, 1065,1244, 948,1213)
        };
    }

    private static Region[] AxeFaces()
    {
        // The cutting bevel and inset plates tilt independently. Do not emboss the engraving.
        return new[]
        {
            Shape(new Color(.30f,.48f,.825f,.90f),1600,1600, 1100,35, 1120,142, 1180,247, 1233,345, 1272,420, 1207,397, 1174,286, 1128,202, 1082,116),
            Shape(new Color(.58f,-.24f,.779f,.92f),1600,1600, 1272,420, 1260,552, 1216,669, 1138,776, 1015,878, 873,968, 925,863, 1044,756, 1142,628, 1207,478),
            Shape(new Color(-.26f,.36f,.896f,.86f),1600,1600, 859,222, 901,257, 978,268, 1049,243, 1099,359, 1059,386, 965,380, 907,353, 858,304),
            Shape(new Color(.32f,-.28f,.905f,.86f),1600,1600, 911,476, 984,450, 1072,461, 1108,524, 1053,580, 1012,593, 963,614, 923,646, 898,624),
            Shape(new Color(-.46f,.30f,.836f,.90f),1600,1600, 605,244, 608,307, 582,371, 550,394, 520,365, 552,330),
            Shape(new Color(-.36f,-.40f,.843f,.90f),1600,1600, 552,450, 583,468, 603,548, 636,588, 626,636, 586,589, 556,518),
            Shape(new Color(-.46f,.30f,.836f,.90f),1600,1600, 324,420, 389,405, 472,421, 423,449, 495,485, 524,507, 397,462),
            Shape(new Color(-.42f,.34f,.841f,.92f),1600,1600, 758,365, 758,420, 718,420),
            Shape(new Color(.42f,.34f,.841f,.92f),1600,1600, 758,365, 799,420, 758,420),
            Shape(new Color(-.42f,-.34f,.841f,.92f),1600,1600, 718,420, 758,420, 758,482),
            Shape(new Color(.42f,-.34f,.841f,.92f),1600,1600, 758,420, 799,420, 758,482)
        };
    }

    private static Region[] CoffinFaces()
    {
        // Faceted planes follow the illustration, in normalized top-left coordinates.
        return new[]
        {
            Shape(new Color(.82f,.08f,.567f,.82f),1,1, .525f,.064f, .735f,.125f, .819f,.327f, .671f,.805f, .54f,.81f, .58f,.51f, .62f,.27f),
            Shape(new Color(.10f,.03f,.994f,.80f),1,1, .326f,.094f, .527f,.065f, .62f,.287f, .54f,.81f, .286f,.81f, .225f,.348f),
            Shape(new Color(-.60f,.06f,.798f,.88f),1,1, .326f,.094f, .356f,.106f, .27f,.314f, .315f,.79f, .286f,.81f, .225f,.348f, .27f,.216f),
            Shape(new Color(.62f,.06f,.782f,.90f),1,1, .527f,.065f, .597f,.21f, .572f,.32f, .56f,.55f, .54f,.79f, .492f,.805f, .52f,.45f, .55f,.27f, .485f,.079f),
            Shape(new Color(.08f,.70f,.710f,.90f),1,1, .326f,.094f, .527f,.065f, .509f,.091f, .350f,.123f)
        };
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
            new Volume(817,616,930,616,46,44,.90f), new Volume(491,599,458,681,33,36,.88f),
            new Volume(720,147,710,205,136,105,.76f), new Volume(760,695,760,728,83,96,.72f),
            new Volume(470,1160,354,1332,123,130,.84f), new Volume(967,1080,1132,1165,126,105,.82f),
            new Volume(188,1590,108,1792,78,116,.88f), new Volume(1105,1501,1098,1711,99,111,.86f),
            new Volume(262,1462,253,1481,70,65,.84f), new Volume(1129,1396,1129,1413,74,63,.86f)
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
            Shape(Leather,1280,1941, 772,572, 839,568, 895,577, 945,575, 971,598, 976,622, 964,646, 927,668, 855,671, 815,650, 786,629),
            Shape(Metal,1280,1941, 639,554, 675,538, 708,548, 734,560, 700,580, 675,604, 665,636, 647,626, 632,597),
            Shape(Metal,1280,1941, 674,603, 703,579, 733,567, 741,569, 715,590, 695,621, 688,650, 698,664, 680,654, 667,634),
            Shape(Metal,1280,1941, 776,573, 788,568, 769,593, 756,621, 761,647, 779,666, 763,662, 747,647, 741,621, 754,591),
            Shape(Metal,1280,1941, 789,568, 845,579, 887,585, 950,575, 952,583, 888,594, 842,587, 785,577),
            Shape(Metal,1280,1941, 773,633, 870,587, 884,587, 779,646),
            Shape(Metal,1280,1941, 835,595, 847,596, 922,659, 910,665),
            Shape(Metal,1280,1941, 943,579, 952,576, 957,611, 949,637, 939,643, 946,612),
            Shape(Leather,1280,1941, 616,768, 802,752, 882,824, 867,890, 649,862, 584,826),
            Shape(Metal,1280,1941, 784,804, 829,779, 871,822, 860,877, 821,900, 787,865),
            Shape(Metal,1280,1941, 702,975, 710,969, 704,1123, 692,1283, 680,1357, 804,1274, 836,1105, 861,961, 870,921, 880,924, 847,1113, 821,1290, 653,1415, 673,1279, 689,1121),
            Shape(Metal,1280,1941, 227,1417, 266,1429, 302,1476, 302,1514, 264,1537, 224,1499),
            Shape(Metal,1280,1941, 1080,1372, 1153,1396, 1217,1366, 1197,1405, 1134,1474, 1105,1434)
        };
        if(path==CoffinPath)return CoffinRegions();
        if(path==AxePath)return AxeRegions();
        return null;
    }

    private static Region[] AxeRegions()
    {
        Color blade = new Color(0,1,.22f,0), inset = new Color(0,1,.48f,0);
        Color grip = new Color(0,0,.86f,0), gem = new Color(0,.55f,.24f,.45f);
        return new[]
        {
            Shape(blade,1600,1600, 1100,35, 1120,142, 1180,247, 1233,345, 1272,420, 1260,552, 1216,669, 1138,776, 1015,878, 873,968, 925,863, 1044,756, 1142,628, 1207,478, 1207,397, 1174,286, 1128,202, 1082,116),
            Shape(inset,1600,1600, 859,222, 901,257, 978,268, 1049,243, 1099,359, 1059,386, 965,380, 907,353, 858,304),
            Shape(inset,1600,1600, 911,476, 984,450, 1072,461, 1108,524, 1053,580, 1012,593, 963,614, 923,646, 898,624),
            Shape(blade,1600,1600, 605,244, 608,307, 582,371, 550,394, 520,365, 552,330),
            Shape(blade,1600,1600, 552,450, 583,468, 603,548, 636,588, 626,636, 586,589, 556,518),
            Shape(blade,1600,1600, 324,420, 522,332, 476,385, 397,420, 475,455, 524,507),
            Shape(grip,1600,1600, 731,713, 782,713, 786,1165, 729,1165),
            Shape(gem,1600,1600, 758,365, 799,420, 758,482, 718,420),
            Shape(new Color(0,.55f,.24f,.40f),1600,1600, 758,1406, 788,1437, 758,1467, 728,1437)
        };
    }

    private static Region[] CoffinRegions()
    {
        Color iron=new Color(0,1,.38f,0),panel=new Color(0,.22f,.78f,0);
        Color gem=new Color(0,.55f,.32f,1),flame=new Color(0,0,1,.18f);
        return new[]
        {
            Shape(panel,1,1, .525f,.064f, .735f,.125f, .819f,.327f, .671f,.805f, .54f,.81f, .58f,.51f, .62f,.27f),
            Shape(iron,1,1, .326f,.094f, .527f,.065f, .509f,.091f, .350f,.123f),
            Shape(iron,1,1, .326f,.094f, .356f,.106f, .27f,.314f, .315f,.79f, .286f,.81f, .225f,.348f, .27f,.216f),
            Shape(iron,1,1, .527f,.065f, .597f,.21f, .572f,.32f, .56f,.55f, .54f,.79f, .492f,.805f, .52f,.45f, .55f,.27f, .485f,.079f),
            Shape(iron,1,1, .286f,.79f, .54f,.785f, .672f,.793f, .667f,.811f, .54f,.816f, .286f,.813f),
            Shape(iron,1,1, .414f,.006f, .45f,.008f, .46f,.07f, .43f,.09f),
            Shape(iron,1,1, .22f,.05f, .34f,.035f, .32f,.07f, .18f,.20f, .15f,.185f),
            Shape(iron,1,1, .718f,.045f, .77f,.044f, .85f,.21f, .82f,.219f, .744f,.102f, .718f,.088f),
            Shape(iron,1,1, .93f,.32f, .995f,.33f, .90f,.52f, .86f,.53f, .945f,.37f),
            Shape(iron,1,1, .09f,.35f, .16f,.35f, .156f,.49f, .13f,.51f, .116f,.39f),
            Shape(iron,1,1, 0,.58f, .07f,.59f, .23f,.79f, .22f,.82f, .005f,.63f),
            Shape(iron,1,1, .385f,.25f, .405f,.273f, .407f,.299f, .448f,.321f, .504f,.336f, .525f,.349f, .461f,.363f, .433f,.381f, .414f,.416f, .401f,.429f, .389f,.397f, .373f,.375f, .316f,.367f, .306f,.354f, .347f,.333f, .369f,.308f),
            Shape(flame,1,1, .76f,.53f, .9f,.56f, .96f,.67f, .86f,.72f, .74f,.68f),
            Shape(flame,1,1, .68f,.75f, .92f,.78f, .78f,.90f, .62f,.96f, .59f,.87f),
            Shape(new Color(0,0,1,.10f),1,1, .52f,.86f, .72f,.92f, .60f,.99f, .39f,.95f, .40f,.88f),
            Shape(new Color(0,0,1,.12f),1,1, .15f,.66f, .24f,.73f, .31f,.85f, .29f,.9f, .18f,.82f),
            Shape(gem,1,1, .404f,.305f, .443f,.342f, .404f,.377f, .374f,.346f),
            Shape(gem,1,1, .421f,.079f, .437f,.101f, .420f,.119f, .407f,.100f),
            Shape(gem,1,1, .119f,.357f, .134f,.370f, .120f,.384f, .109f,.372f),
            Shape(gem,1,1, .74f,.058f, .761f,.067f, .742f,.082f, .725f,.074f),
            Shape(gem,1,1, .953f,.330f, .977f,.344f, .953f,.361f, .938f,.348f),
            Shape(gem,1,1, .028f,.596f, .056f,.609f, .041f,.628f, .017f,.618f),
            Shape(gem,1,1, .393f,.727f, .411f,.752f, .393f,.763f, .378f,.749f)
        };
    }
}
