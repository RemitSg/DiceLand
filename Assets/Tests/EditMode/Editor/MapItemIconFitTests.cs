using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public sealed class MapItemIconFitTests
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    [TestCase(64, 64)]
    [TestCase(112, 112)]
    [TestCase(460, 360)]
    public void RealWeaponAndScrollSilhouettesAreCenteredAndFitWithoutDistortion(int width, int height)
    {
        GameObject host = new GameObject("IconFitTest", typeof(RectTransform), typeof(Image));
        Mesh mesh = new Mesh();
        try
        {
            Image image = host.GetComponent<Image>();
            image.rectTransform.sizeDelta = new Vector2(width, height);
            image.rectTransform.pivot = new Vector2(0.2f, 0.8f);
            image.color = new Color32(193, 210, 164, 227);
            MapItemIconFit.Attach(image);
            Sprite[] icons = Weapons().Where(w => w.icon != null).Select(w => w.icon)
                .Concat(Blueprints().Where(b => b.icon != null).Select(b => b.icon)).Distinct().ToArray();
            Assert.GreaterOrEqual(icons.Length, 40, "Audit the complete current weapon and blueprint set.");
            foreach (Sprite sprite in icons)
            {
                image.sprite = sprite;
                Vector2[] source = sprite.vertices;
                float sourceWidth = source.Max(v => v.x) - source.Min(v => v.x);
                float sourceHeight = source.Max(v => v.y) - source.Min(v => v.y);
                using (var vertices = new VertexHelper())
                {
                    image.GetComponent<MapItemIconFit>().ModifyMesh(vertices);
                    mesh.Clear(); vertices.FillMesh(mesh);
                }
                Bounds visible = mesh.bounds;
                Rect box = image.GetPixelAdjustedRect();
                Assert.That(visible.center.x, Is.EqualTo(box.center.x).Within(0.01f), sprite.name);
                Assert.That(visible.center.y, Is.EqualTo(box.center.y).Within(0.01f), sprite.name);
                Assert.LessOrEqual(visible.size.x, box.width + 0.01f, sprite.name);
                Assert.LessOrEqual(visible.size.y, box.height + 0.01f, sprite.name);
                Assert.That(Mathf.Max(visible.size.x / box.width, visible.size.y / box.height), Is.EqualTo(1).Within(0.001f), sprite.name);
                Assert.That(visible.size.x / visible.size.y, Is.EqualTo(sourceWidth / sourceHeight).Within(0.001f), sprite.name);
                CollectionAssert.AreEqual(sprite.uv, mesh.uv, sprite.name + " atlas UVs");
                Assert.AreEqual((Color32)image.color, mesh.colors32[0]);
                Assert.AreSame(sprite, image.sprite);
                CollectionAssert.AreEqual(source, sprite.vertices, sprite.name + " original world geometry");
            }
        }
        finally
        {
            Object.DestroyImmediate(mesh);
            Object.DestroyImmediate(host);
        }
    }

    [Test]
    public void OverrideSpriteUsesItsOwnShapeAndSliceGraphicsRemainUntouched()
    {
        GameObject host = new GameObject("OverrideIconTest", typeof(RectTransform), typeof(Image));
        Texture2D texture = new Texture2D(12, 12);
        Sprite original = Sprite.Create(texture, new Rect(0, 0, 4, 12), Vector2.zero);
        Sprite replacement = Sprite.Create(texture, new Rect(4, 0, 8, 4), Vector2.one);
        Mesh mesh = new Mesh();
        try
        {
            Image image = host.GetComponent<Image>(); image.rectTransform.sizeDelta = new Vector2(80, 80);
            image.sprite = original; image.overrideSprite = replacement;
            MapItemIconFit.Attach(image);
            using (var vertices = new VertexHelper())
            {
                image.GetComponent<MapItemIconFit>().ModifyMesh(vertices); vertices.FillMesh(mesh);
                Assert.That(mesh.bounds.size.x / mesh.bounds.size.y, Is.EqualTo(2).Within(0.001f));
                Assert.That(mesh.bounds.center.sqrMagnitude, Is.LessThan(0.001f));
                int before = vertices.currentVertCount;
                image.type = Image.Type.Sliced;
                image.GetComponent<MapItemIconFit>().ModifyMesh(vertices);
                Assert.AreEqual(before, vertices.currentVertCount);
            }
            Assert.AreSame(original, image.sprite);
        }
        finally
        {
            Object.DestroyImmediate(host); Object.DestroyImmediate(mesh);
            Object.DestroyImmediate(original); Object.DestroyImmediate(replacement); Object.DestroyImmediate(texture);
        }
    }

    [Test]
    public void EveryCurrentBlueprintHasDedicatedScrollArt()
    {
        CraftingBlueprint_SO[] recipes = Blueprints();
        Assert.AreEqual(20, recipes.Length);
        foreach (CraftingBlueprint_SO recipe in recipes)
        {
            Assert.IsNotNull(recipe.icon, recipe.DisplayName);
            Assert.AreNotSame(recipe.targetWeapon.icon, recipe.DisplayIcon, recipe.DisplayName);
            StringAssert.StartsWith("Assets/Data/Scroll/", AssetDatabase.GetAssetPath(recipe.icon), recipe.DisplayName);
        }
    }

    [TestCase("weaponTabButton")]
    [TestCase("blueprintTabButton")]
    public void InventoryGridAndDetailApplyTheSameVisibleShapeFit(string tabField)
    {
        GameObject host = new GameObject("InventoryFitIntegration");
        try
        {
            MapHUD hud = host.AddComponent<MapHUD>();
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath("c6e7143cd2df45b45a784ef8c61a210f"));
            hud.ConfigureFonts(font, font, font);
            var state = new RunState();
            CraftingBlueprint_SO recipe = Blueprints().First(b => b.blueprintID == 103);
            state.AddWeapon(recipe.targetWeapon, 1); state.AddBlueprint(recipe, 1);
            hud.Refresh(state, null); hud.ShowInventoryPanel();
            ((Button)typeof(MapHUD).GetField(tabField, Private).GetValue(hud)).onClick.Invoke();
            Image gridIcon = host.GetComponentsInChildren<Image>().First(i => i.name == "Icon" && i.transform.parent.name == "Slot_0");
            Assert.IsNotNull(gridIcon.GetComponent<MapItemIconFit>());
            Sprite expected = tabField == "weaponTabButton" ? recipe.targetWeapon.icon : recipe.icon;
            Assert.AreSame(expected, gridIcon.sprite);
            gridIcon.transform.parent.GetComponent<Button>().onClick.Invoke();
            MapInventoryDetailView detail = host.GetComponentInChildren<MapInventoryDetailView>();
            Assert.IsNotNull(detail);
            Image detailIcon = detail.transform.Find("Icon").GetComponent<Image>();
            Assert.IsNotNull(detailIcon.GetComponent<MapItemIconFit>());
            Assert.AreSame(expected, detailIcon.sprite);
        }
        finally { Object.DestroyImmediate(host); }
    }

    private static CraftingBlueprint_SO[] Blueprints() => AssetDatabase.FindAssets("t:CraftingBlueprint_SO", new[] { "Assets/Data" })
        .Select(g => AssetDatabase.LoadAssetAtPath<CraftingBlueprint_SO>(AssetDatabase.GUIDToAssetPath(g))).Where(b => b != null).ToArray();
    private static Weapon_SO[] Weapons() => AssetDatabase.FindAssets("t:Weapon_SO", new[] { "Assets/Data" })
        .Select(g => AssetDatabase.LoadAssetAtPath<Weapon_SO>(AssetDatabase.GUIDToAssetPath(g))).Where(w => w != null).ToArray();
}
