using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public sealed class BattleDeathVfxTests
{
    private GameObject actor;
    private Sprite sprite;
    private Texture2D texture;
    private BattleDeathVfx effect;
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    [SetUp]
    public void SetUp()
    {
        actor = new GameObject("Death visual witness"); actor.transform.position = new Vector3(2, 3, 0);
        texture = new Texture2D(16, 32); texture.SetPixels(Enumerable.Repeat(Color.white, 512).ToArray()); texture.Apply();
        sprite = Sprite.Create(texture, new Rect(0, 0, 16, 32), Vector2.one * 0.5f, 16);
        SpriteRenderer renderer = actor.AddComponent<SpriteRenderer>(); renderer.sprite = sprite; renderer.flipX = true;
        actor.transform.localScale = new Vector3(-1.5f, 2, 1);
    }

    [TearDown]
    public void TearDown()
    {
        if (effect != null) Object.DestroyImmediate(effect.gameObject);
        Object.DestroyImmediate(actor); Object.DestroyImmediate(sprite); Object.DestroyImmediate(texture);
    }

    private void Play(bool enemy = false, int detail = 36)
    {
        Assert.IsTrue(BattleDeathVfx.TryPlay(actor.transform, Vector3.right, enemy, 420, detail));
        effect = Object.FindObjectsOfType<BattleDeathVfx>().First(e => e.gameObject.scene == actor.scene);
    }

    [Test]
    public void SnapshotSurvivesSourceRemovalAndPreservesItsSpriteFlipAndWorldSize()
    {
        Bounds original = actor.GetComponent<SpriteRenderer>().bounds; Play();
        SpriteRenderer copy = effect.GetComponentInChildren<SpriteRenderer>();
        Assert.AreSame(sprite, copy.sprite); Assert.IsTrue(copy.flipX);
        Assert.That(Vector3.Distance(copy.bounds.center, original.center), Is.LessThan(0.001f));
        Assert.That(Vector3.Distance(copy.bounds.size, original.size), Is.LessThan(0.001f));
        Assert.IsTrue(actor.GetComponent<SpriteRenderer>().enabled, "The snapshot must not alter gameplay or source visibility.");
        Object.DestroyImmediate(actor); effect.Advance(0.4f);
        Assert.IsNotNull(copy); Assert.Greater(effect.GetComponent<MeshFilter>().sharedMesh.triangles.Length, 0);
    }

    [Test]
    public void SpeedButtonsAndWeaponsNeverBecomeGhostBodies()
    {
        GameObject badge = new GameObject("Speed badge"); badge.transform.SetParent(actor.transform); badge.AddComponent<SpeedButton>(); badge.AddComponent<SpriteRenderer>().sprite = sprite;
        GameObject weapon = new GameObject("Weapon"); weapon.transform.SetParent(actor.transform); weapon.AddComponent<Weapon>(); weapon.AddComponent<SpriteRenderer>().sprite = sprite;
        Play(); Assert.AreEqual(1, effect.GetComponentsInChildren<SpriteRenderer>().Length);
    }

    [Test]
    public void LightBurstHasABoundedBudgetAndDifferentFriendlyAndHostilePalettes()
    {
        Play(false, 10000); Assert.AreEqual(180, effect.FragmentCount);
        Color friendly = effect.GetComponentInChildren<SpriteRenderer>().sharedMaterial.GetColor("_SoulColor");
        Object.DestroyImmediate(effect.gameObject); effect = null; Play(true, -1);
        Assert.AreEqual(72, effect.FragmentCount);
        Color hostile = effect.GetComponentInChildren<SpriteRenderer>().sharedMaterial.GetColor("_SoulColor");
        Assert.Greater(friendly.g, hostile.g); Assert.Greater(hostile.r, friendly.r);
    }

    [Test]
    public void EffectsFinishWithoutChangingTimeOrMutatingTheSourceMaterial()
    {
        Material source = actor.GetComponent<SpriteRenderer>().sharedMaterial;
        float scale = Time.timeScale, step = Time.fixedDeltaTime; Play();
        effect.Advance(-1); Assert.AreEqual(0, effect.Elapsed);
        effect.Advance(BattleDeathVfx.Lifetime); Assert.IsTrue(effect.IsComplete);
        Assert.AreEqual(0, effect.GetComponent<MeshFilter>().sharedMesh.triangles.Length);
        Assert.AreSame(source, actor.GetComponent<SpriteRenderer>().sharedMaterial);
        Assert.AreEqual(scale, Time.timeScale); Assert.AreEqual(step, Time.fixedDeltaTime);
    }

    [Test]
    public void DestroyingAnEffectReleasesItsNativeMeshAndMaterials()
    {
        Play(); Mesh mesh = effect.GetComponent<MeshFilter>().sharedMesh;
        Material[] materials = effect.GetComponentsInChildren<Renderer>().Select(r => r.sharedMaterial).ToArray();
        Object.DestroyImmediate(effect.gameObject); effect = null;
        Assert.IsTrue(mesh == null); Assert.IsTrue(materials.All(m => m == null));
    }

    [TestCase(false)] [TestCase(true)]
    public void ActualDeathCallbackStillHidesTheDeadActorImmediatelyAndCreatesOneIndependentEffect(bool enemy)
    {
        GameObject host = new GameObject("Death feedback owner"); host.SetActive(false);
        BattleFeedbackSystem feedback = host.AddComponent<BattleFeedbackSystem>();
        typeof(BattleFeedbackSystem).GetField("enableDeathSlowMotion", Private).SetValue(feedback, false);
        try
        {
            if (enemy)
            {
                BattleEnemyController controller = actor.AddComponent<BattleEnemyController>();
                controller.Died += dead => typeof(BattleFeedbackSystem).GetMethod("OnEnemyDied", Private).Invoke(feedback, new object[] { dead });
                controller.KillInstantly(); controller.KillInstantly(); Assert.IsTrue(controller.IsDead);
            }
            else
            {
                BattleAllyController controller = actor.AddComponent<BattleAllyController>();
                controller.Died += dead => typeof(BattleFeedbackSystem).GetMethod("OnAllyDied", Private).Invoke(feedback, new object[] { dead });
                controller.KillInstantly(); controller.KillInstantly(); Assert.IsTrue(controller.IsDead);
            }
            effect = Object.FindObjectOfType<BattleDeathVfx>(); Assert.IsNotNull(effect);
            Assert.IsFalse(actor.GetComponent<SpriteRenderer>().enabled);
            Assert.AreEqual(1, Object.FindObjectsOfType<BattleDeathVfx>().Length);
            Assert.IsTrue(effect.GetComponentInChildren<SpriteRenderer>().enabled);
        }
        finally { Object.DestroyImmediate(host); }
    }

    [Test]
    public void ProductionShadersAreIncludedAndCompileWithoutErrors()
    {
        foreach (string name in new[] { "DeathLight", "DeathDissolve" })
        {
            Shader shader = Resources.Load<Shader>("BattleVfx/" + name); Assert.NotNull(shader); Assert.IsTrue(shader.isSupported); Assert.IsFalse(ShaderUtil.ShaderHasError(shader), name);
        }
    }
}
