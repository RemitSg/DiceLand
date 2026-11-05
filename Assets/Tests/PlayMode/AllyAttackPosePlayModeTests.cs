#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public sealed class AllyAttackPosePlayModeTests
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private GameObject host;
    private ScriptableObject skill;
    private float previousTimeScale;

    [SetUp]
    public void SetUp()
    {
        previousTimeScale = Time.timeScale;
        Time.timeScale = 1f;
        host = new GameObject("Ally attack pose tests");
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        Object.Destroy(host);
        if (skill != null) Object.Destroy(skill);
        Time.timeScale = previousTimeScale;
        yield return null;
    }

    [UnityTest]
    public IEnumerator EveryEquippedAttackPoseFollowsMovementAndRestoresTheIdleGrip()
    {
        Component presentation = host.AddComponent(RuntimeType("BattlePresentationSystem"));
        var bootstrapHost = new GameObject("Inactive weapon setup");
        bootstrapHost.transform.SetParent(host.transform, false);
        bootstrapHost.SetActive(false);
        Component bootstrap = bootstrapHost.AddComponent(RuntimeType("BattleSceneEncounterBootstrap"));

        string[] characters = { "Chara/Chara", "Man/Man_Data", "Molly/Molly_Data", "Jenny/Jenny" };
        string[] weapons = { "000 WoodenSword/WoodenSword_Data", "200 Enemyless/Enemyless", "100 Dagger/Dagger_Data", "300 OldWand/OldWand" };
        for (int i = 0; i < characters.Length; i++)
        {
            Object data = Load("Assets/Data/Characters/" + characters[i] + ".asset", RuntimeType("CharacterData_SO"));
            GameObject actor = InstantiateCharacter(data, out Component ally, out Component equipment);
            SpriteRenderer body = actor.GetComponentInChildren<SpriteRenderer>();
            Sprite idle = body.sprite;
            Transform idleHand = actor.transform.Find("HoldPoint");
            Object weaponData = Load("Assets/Data/Weapons/" + weapons[i] + ".asset", RuntimeType("Weapon_SO"));
            var weapon = (Component)Call(bootstrap, "CreateEquippedWeapon", weaponData, actor.transform, idleHand, body, body.sortingOrder - 5);
            Set(equipment, "weapon", weapon);
            var weaponRoot = (Transform)weapon.GetType().GetProperty("BattleVisualRoot", Members).GetValue(weapon);
            var weaponGrip = (Transform)Get(weapon, "HoldPoint");
            Vector3 idlePosition = weaponRoot.localPosition;
            Quaternion idleRotation = weaponRoot.localRotation;
            float depth = weaponGrip.position.z - idleHand.position.z;

            object fx = Call(ally, "GetDicePresentationSettings", 0);
            object pose = Get(fx, "characterAttackPose");
            Sprite attack = (Sprite)Get(pose, "sprite");
            Assert.IsNotNull(attack, characters[i]);
            Call(presentation, "TriggerSkillSpriteSwap", actor.transform, fx, 0.2f);
            Assert.AreSame(attack, body.sprite, characters[i]);
            AssertGrip(body, pose, weaponGrip, depth);

            Vector3 moved = actor.transform.position + new Vector3(0.6f, 0.2f, 0.1f);
            actor.transform.position = moved;
            AssertGrip(body, pose, weaponGrip, depth);
            Call(presentation, "TriggerSkillSpriteSwap", actor.transform, fx, 0.06f);
            AssertGrip(body, pose, weaponGrip, depth);

            if (i == characters.Length - 1)
                ((Behaviour)presentation).enabled = false;
            else
            {
                float deadline = Time.realtimeSinceStartup + 2f;
                while (body.sprite != idle && Time.realtimeSinceStartup < deadline)
                    yield return null;
            }

            Assert.AreSame(idle, body.sprite, characters[i]);
            Assert.That(Vector3.Distance(actor.transform.position, moved), Is.LessThan(0.001f), characters[i]);
            Assert.That(Vector3.Distance(weaponRoot.localPosition, idlePosition), Is.LessThan(0.001f), characters[i]);
            Assert.That(Quaternion.Angle(weaponRoot.localRotation, idleRotation), Is.LessThan(0.1f), characters[i]);
            Object.Destroy(actor);
        }
    }

    [Test]
    public void MixedDiceKeepDefenseAndAnAuthoredSpriteOverrideInControl()
    {
        Object data = Load("Assets/Data/Characters/Chara/Chara.asset", RuntimeType("CharacterData_SO"));
        GameObject actor = InstantiateCharacter(data, out Component ally, out _);
        Sprite original = actor.GetComponentInChildren<SpriteRenderer>().sprite;
        skill = ScriptableObject.CreateInstance(RuntimeType("EnemySkill_SO"));
        Type category = RuntimeType("EnemySkill_SO+SkillCategory");
        Array categories = Array.CreateInstance(category, 3);
        categories.SetValue(Enum.Parse(category, "Attack"), 0);
        categories.SetValue(Enum.Parse(category, "Defense"), 1);
        categories.SetValue(Enum.Parse(category, "Attack"), 2);
        Set(skill, "diceSkillCategories", categories);
        Type fxType = RuntimeType("EnemySkill_SO+DicePresentationSettings");
        Array effects = Array.CreateInstance(fxType, 3);
        object custom = Activator.CreateInstance(fxType);
        Set(custom, "skillSpriteOverride", original);
        effects.SetValue(custom, 2);
        Set(skill, "dicePresentationSettings", effects);
        ally.GetType().GetProperty("SelectedSkill").GetSetMethod(true).Invoke(ally, new object[] { skill });

        Assert.AreSame(Get(Get(data, "attackPose"), "sprite"), PoseSprite(Call(ally, "GetDicePresentationSettings", 0)));
        Assert.IsNull(PoseSprite(Call(ally, "GetDicePresentationSettings", 1)));
        object authored = Call(ally, "GetDicePresentationSettings", 2);
        Assert.AreSame(original, Get(authored, "skillSpriteOverride"));
        Assert.IsNull(PoseSprite(authored));
        Assert.IsNull(PoseSprite(Call(skill, "GetDicePresentationSettings", 0)), "The shared skill asset must remain unchanged.");
    }

    private GameObject InstantiateCharacter(Object data, out Component ally, out Component equipment)
    {
        var actor = Object.Instantiate((GameObject)Get(data, "characterPrefab"), host.transform, false);
        Type holder = RuntimeType("AllyController");
        equipment = actor.GetComponent(holder) ?? actor.AddComponent(holder);
        Set(equipment, "characterData", data);
        Type controller = RuntimeType("BattleAllyController");
        ally = actor.GetComponent(controller) ?? actor.AddComponent(controller);
        return actor;
    }

    private static void AssertGrip(SpriteRenderer body, object pose, Transform weaponGrip, float depth)
    {
        var point = (Vector2)Get(pose, "gripPosition");
        var angle = (float)Get(pose, "gripAngle");
        Sprite sprite = body.sprite;
        Vector2 local = (Vector2.Scale(sprite.rect.size, point) - sprite.pivot) / sprite.pixelsPerUnit;
        Vector3 expected = body.transform.TransformPoint(local) + Vector3.forward * depth;
        Quaternion rotation = body.transform.rotation * Quaternion.Euler(0f, 0f, angle);
        Assert.That(Vector3.Distance(expected, weaponGrip.position), Is.LessThan(0.002f));
        Assert.That(Quaternion.Angle(rotation, weaponGrip.rotation), Is.LessThan(0.15f));
    }

    private static object PoseSprite(object fx) => Get(Get(fx, "characterAttackPose"), "sprite");
    private static Type RuntimeType(string name) => Type.GetType(name + ", Assembly-CSharp", true);
    private static Object Load(string path, Type type)
    {
        Type database = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("UnityEditor.AssetDatabase")).First(t => t != null);
        return (Object)database.GetMethod("LoadAssetAtPath", new[] { typeof(string), typeof(Type) }).Invoke(null, new object[] { path, type });
    }
    private static object Get(object target, string field) => target.GetType().GetField(field, Members).GetValue(target);
    private static void Set(object target, string field, object value) => target.GetType().GetField(field, Members).SetValue(target, value);
    private static object Call(object target, string method, params object[] args) => target.GetType().GetMethod(method, Members).Invoke(target, args);
}
#endif
