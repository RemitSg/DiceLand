using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class BattleSpeedButtonLookTests
{
    private GameObject root;

    [SetUp]
    public void SetUp()
    {
        root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Button.prefab"));
    }

    [TearDown]
    public void TearDown()
    {
        if (root != null) Object.DestroyImmediate(root);
    }

    [Test]
    public void BothBadgeStatesHaveBrightArtworkAndSharedNonInteractiveHalos()
    {
        SpeedButton button = root.GetComponent<SpeedButton>();
        BattleSpeedButtonLook look = root.GetComponent<BattleSpeedButtonLook>();
        int colliderCount = root.GetComponentsInChildren<Collider2D>(true).Length;
        look.RefreshLook();
        look.RefreshLook();
        MeshRenderer[] halos = root.GetComponentsInChildren<MeshRenderer>(true)
            .Where(renderer => renderer.name == "Speed badge soft glow").ToArray();
        Assert.AreEqual(2, halos.Length, "Refresh must not duplicate the two state halos.");
        Assert.AreSame(halos[0].sharedMaterial, halos[1].sharedMaterial);
        Assert.AreSame(halos[0].GetComponent<MeshFilter>().sharedMesh, halos[1].GetComponent<MeshFilter>().sharedMesh);
        Assert.AreEqual("DiceLand/Speed Button Glow", halos[0].sharedMaterial.shader.name);
        Assert.AreEqual(Color.white, button.unpressedButton.GetComponent<SpriteRenderer>().color);
        Assert.AreEqual(Color.white, button.pressedButton.GetComponent<SpriteRenderer>().color);
        Assert.AreEqual(colliderCount, root.GetComponentsInChildren<Collider2D>(true).Length);
        foreach (MeshRenderer halo in halos)
        {
            SpriteRenderer body = halo.transform.parent.GetComponent<SpriteRenderer>();
            Assert.AreEqual("DiceLand/Speed Button Badge", body.sharedMaterial.shader.name);
            MaterialPropertyBlock badge = new MaterialPropertyBlock();
            body.GetPropertyBlock(badge);
            Assert.Greater(badge.GetVector("_BadgeBounds").z, 0f);
            Assert.Less(halo.sortingOrder, body.sortingOrder);
            body.gameObject.layer = 5;
        }
        look.RefreshLook();
        Assert.IsTrue(halos.All(halo => halo.gameObject.layer == 5));
    }

    [Test]
    public void PressedAndCombatHiddenStatesCannotLeaveFloatingGlows()
    {
        SpeedButton button = root.GetComponent<SpeedButton>();
        BattleSpeedButtonLook look = root.GetComponent<BattleSpeedButtonLook>();
        look.RefreshLook();
        button.unpressedButton.SetActive(false);
        button.pressedButton.SetActive(true);
        Assert.IsFalse(button.unpressedButton.GetComponentInChildren<MeshRenderer>(true).gameObject.activeInHierarchy);
        Assert.IsTrue(button.pressedButton.GetComponentInChildren<MeshRenderer>(true).gameObject.activeInHierarchy);
        button.HideForCombat();
        Assert.IsFalse(root.GetComponentsInChildren<MeshRenderer>(true)
            .Any(renderer => renderer.name == "Speed badge soft glow" && renderer.gameObject.activeInHierarchy));
        look.ReleaseLook();
        Assert.IsFalse(root.GetComponentsInChildren<MeshRenderer>(true).Any(renderer => renderer.name == "Speed badge soft glow"));
    }

    [Test]
    public void ViewportBoundsIncludeTheActiveHaloAfterMovingWithoutPhysicsSync()
    {
        SpeedButton button = root.GetComponent<SpeedButton>();
        BattleSpeedButtonLook look = root.GetComponent<BattleSpeedButtonLook>();
        bool previousSync = Physics2D.autoSyncTransforms;
        try
        {
            Physics2D.autoSyncTransforms = false;
            look.RefreshLook();
            button.GetViewportBounds();
            root.transform.position = new Vector3(9f, 6f, 0f);
            foreach (bool pressed in new[] { false, true })
            {
                button.unpressedButton.transform.localPosition = pressed ? Vector3.one * 100f : Vector3.zero;
                button.pressedButton.transform.localPosition = pressed ? Vector3.zero : Vector3.one * 100f;
                button.unpressedButton.SetActive(!pressed);
                button.pressedButton.SetActive(pressed);
                Assert.IsTrue(look.TryGetVisualBounds(out Bounds halo));
                Bounds viewport = button.GetViewportBounds();
                viewport.Expand(0.00001f); // Bounds centre/extents reconstruction can round by one float ULP.
                Assert.IsTrue(viewport.Contains(halo.min));
                Assert.IsTrue(viewport.Contains(halo.max));
                Assert.That(viewport.size.x, Is.LessThan(10f), "Inactive state geometry must not enlarge the bounds.");
            }
            button.HideForCombat();
            Assert.IsFalse(look.TryGetVisualBounds(out _));
        }
        finally { Physics2D.autoSyncTransforms = previousSync; }
    }

    [Test]
    public void HeadlessBattleDoesNotAllocateGlowObjectsOrAdvanceRandomState()
    {
        SpeedButton button = root.GetComponent<SpeedButton>();
        BattleSpeedButtonLook look = root.GetComponent<BattleSpeedButtonLook>();
        string randomState = JsonUtility.ToJson(Random.state);
        button.ConfigureHeadlessExecution();
        look.RefreshLook();
        Assert.IsFalse(root.GetComponentsInChildren<MeshRenderer>(true).Any(renderer => renderer.name == "Speed badge soft glow"));
        Assert.AreEqual(randomState, JsonUtility.ToJson(Random.state));
    }
}
