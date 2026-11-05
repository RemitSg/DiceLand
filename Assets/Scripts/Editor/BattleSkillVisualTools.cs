using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Production skill UI in a disposable active scene. Never saves a scene or touches a run.</summary>
public static class BattleSkillVisualTools
{
    public const string OutputPath = "outputs/agent-battle/skill-ui-20261105";
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    public static string Capture(string name, int width = 1920, int height = 1080, int mode = 0, Rect? safe = null, bool scrollEnd = false)
    {
        Scene original = SceneManager.GetActiveScene();
        bool input = SpeedButton.InputSuppressed, locked = SpeedButton.CombatLocked;
        Scene preview = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(preview);
        RenderTexture previous = RenderTexture.active, target = null;
        Texture2D pixels = null; Camera camera = null; GameObject host = null;
        var transient = new List<UnityEngine.Object>();
        try
        {
            SpeedButton.CombatLocked = false;
            host = new GameObject("SkillVisualPreview", typeof(EventSystem));
            GameObject cameraObject = new GameObject("SkillPreviewCamera", typeof(Camera));
            camera = cameraObject.GetComponent<Camera>(); camera.scene = preview;
            camera.orthographic = true; camera.orthographicSize = 5; camera.aspect = width / (float)height;
            camera.transform.position = new Vector3(0, 0, -10); camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(0.025f, 0.06f, 0.035f);
            new GameObject("SkillPreviewLight", typeof(Light)).GetComponent<Light>().type = LightType.Directional;
            GameObject scenery = new GameObject("Forest", typeof(SpriteRenderer)); scenery.transform.SetParent(host.transform, false);
            SpriteRenderer forest = scenery.GetComponent<SpriteRenderer>(); forest.sprite = MapForestArt.Background(MapBackgroundPhase.Morning);
            if (forest.sprite != null) scenery.transform.localScale = Vector3.one * Mathf.Max(10 / forest.sprite.bounds.size.y, 10 * camera.aspect / forest.sprite.bounds.size.x);
            BattleSkillInfoOverlay overlay = host.AddComponent<BattleSkillInfoOverlay>();
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath("c6e7143cd2df45b45a784ef8c61a210f"));
            overlay.ConfigureFonts(font, font);
            if (mode == 0 || mode == 1 || mode == 6)
            {
                Weapon_SO weapon = AssetDatabase.LoadAssetAtPath<Weapon_SO>(mode == 0 ? "Assets/Data/Weapons/000 WoodenSword/WoodenSword_Data.asset" : "Assets/Data/Weapons/007 烬冠剑/007 烬冠剑.asset");
                BattleAllyController ally = CreateAlly(host.transform, weapon);
                if (mode == 6)
                {
                    EnemySkill_SO longSkill = UnityEngine.Object.Instantiate(weapon.skillPool.Last()); transient.Add(longSkill);
                    longSkill.skillName = "多骰长词条验证"; longSkill.diceCount = 3;
                    longSkill.dicePowerRanges = new[] { new EnemySkill_SO.DicePowerRange { minPower = 3, maxPower = 6 }, new EnemySkill_SO.DicePowerRange { minPower = 1, maxPower = 4 }, new EnemySkill_SO.DicePowerRange { minPower = 2, maxPower = 5 } };
                    longSkill.diceSkillCategories = new[] { EnemySkill_SO.SkillCategory.Attack, EnemySkill_SO.SkillCategory.Defense, EnemySkill_SO.SkillCategory.Attack };
                    longSkill.description = string.Join("\n", Enumerable.Repeat("长说明验证：保留完整条件、目标、数值与延迟，不以缩小文字替代滚动。", 24));
                    ally.GrantTemporarySkill(longSkill, true); ally.SetSelectedSkillIndexForButton(ally.SpeedButtons[0], 0);
                }
                else ally.SetSelectedSkillIndexForButton(ally.SpeedButtons[0], mode == 0 ? 1 : 3);
                overlay.ShowAllySkillSelection(ally, ally.SpeedButtons[0]);
            }
            else
            {
                string folder = mode == 4 || mode == 5 ? "Assets/Data/Enemies/Enemies/027 UtilityPole" : mode == 7 ? "Assets/Data/Enemies/Enemies/029 EmberScaleLizard" : "Assets/Data/Enemies/Enemies/014 BloodRat";
                EnemyData_SO data = AssetDatabase.FindAssets("t:EnemyData_SO", new[] { folder }).Select(g => AssetDatabase.LoadAssetAtPath<EnemyData_SO>(AssetDatabase.GUIDToAssetPath(g))).First();
                if (mode == 9 || mode == 10)
                {
                    data = UnityEngine.Object.Instantiate(data); transient.Add(data);
                    if (mode == 9)
                    {
                        data.enemyDescription = string.Join("\n", Enumerable.Repeat(data.enemyDescription, 5));
                        data.passiveDescriptions = Enumerable.Range(0, 6).Select(i => new EnemyData_SO.PassiveDescriptionEntry
                        {
                            passiveName = "长名称与多条被动验证 · " + (i + 1), displayColorType = (EnemyData_SO.PassiveDisplayColorType)(i % 3),
                            description = string.Join("\n", Enumerable.Repeat("当自身以外的 【嗜血啮鼠】 死亡时，给予自身 1 层虚弱。保留完整条件与持续说明。", 3))
                        }).ToList();
                    }
                    else { data.enemySprite = null; data.passiveDescriptions = null; data.enemyDescription = string.Empty; }
                }
                EnemySkill_SO skill = AssetDatabase.LoadAssetAtPath<EnemySkill_SO>(folder + (mode == 4 || mode == 5 ? "/HighVoltage.asset" : mode == 7 ? "/CurlCounter.asset" : "/RotfangBite.asset"));
                BattleEnemyController enemy = CreateEnemy(host.transform, data, skill);
                if (mode >= 8) overlay.ShowEnemyDetail(enemy);
                else if (mode == 3 || mode == 5) overlay.ShowEnemyHoverSkillInfo(enemy, enemy.SpeedButtons[0]);
                else overlay.ShowEnemySkillInfo(enemy, enemy.SpeedButtons[0]);
            }
            Canvas canvas = (Canvas)typeof(BattleSkillInfoOverlay).GetField("canvas", Private).GetValue(overlay);
            if (canvas.gameObject.scene != preview) throw new InvalidOperationException("Skill canvas escaped preview scene.");
            canvas.GetComponent<CanvasScaler>().enabled = false; canvas.renderMode = RenderMode.WorldSpace; canvas.worldCamera = camera;
            RectTransform rect = (RectTransform)canvas.transform; rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * 0.5f;
            rect.sizeDelta = new Vector2(1080f * width / height, 1080); rect.position = new Vector3(0, 0, -3); rect.localScale = Vector3.one * (5f / 540);
            Canvas.ForceUpdateCanvases();
            overlay.ApplyViewport(new Vector2(width, height), safe ?? new Rect(0, 0, width, height)); overlay.AdvancePresentation(2);
            if (scrollEnd)
            {
                if (overlay.IsVisible)
                {
                    ScrollRect scroll = (ScrollRect)typeof(BattleSkillInfoOverlay).GetField(mode >= 8 ? "enemyProfileScroll" : "detailScrollRect", Private).GetValue(overlay);
                    scroll.verticalNormalizedPosition = 0;
                }
                else
                {
                    RectTransform content = (RectTransform)typeof(BattleSkillInfoOverlay).GetField("hoverClauseContent", Private).GetValue(overlay);
                    RectTransform viewport = (RectTransform)typeof(BattleSkillInfoOverlay).GetField("hoverClauseViewport", Private).GetValue(overlay);
                    content.anchoredPosition = Vector2.up * Mathf.Max(0, content.rect.height - viewport.rect.height);
                }
                Canvas.ForceUpdateCanvases();
                if (mode >= 8) typeof(BattleSkillInfoOverlay).GetMethod("UpdateEnemyScrollHint", Private).Invoke(overlay, null);
            }
            foreach (TMP_Text text in canvas.GetComponentsInChildren<TMP_Text>()) text.ForceMeshUpdate(true, true);
            foreach (Graphic graphic in canvas.GetComponentsInChildren<Graphic>())
                if (!(graphic is TMP_Text) && !(graphic is TMP_SubMeshUI)) { graphic.SetAllDirty(); graphic.Rebuild(CanvasUpdate.PreRender); }
            Canvas.ForceUpdateCanvases();
            target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32); camera.targetTexture = target;
            camera.Render(); RenderTexture.active = target;
            pixels = new Texture2D(width, height, TextureFormat.RGB24, false); pixels.ReadPixels(new Rect(0, 0, width, height), 0, 0); pixels.Apply();
            Directory.CreateDirectory(OutputPath); string path = Path.Combine(OutputPath, name); File.WriteAllBytes(path, pixels.EncodeToPNG());
            File.WriteAllLines(path + ".layout.txt", canvas.GetComponentsInChildren<TMP_Text>(true).Select(t => t.name + " | " + t.text + " | active=" + t.isActiveAndEnabled + " | truncated=" + t.isTextTruncated));
            if (mode >= 8)
            {
                ScrollRect scroll = (ScrollRect)typeof(BattleSkillInfoOverlay).GetField("enemyProfileScroll", Private).GetValue(overlay);
                File.AppendAllText(path + ".layout.txt", "Profile content=" + scroll.content.rect.height + " viewport=" + scroll.viewport.rect.height + " scrollbar=" + scroll.verticalScrollbar.gameObject.activeInHierarchy);
            }
            return Path.GetFullPath(path);
        }
        finally
        {
            RenderTexture.active = previous;
            if (camera != null) camera.targetTexture = null;
            if (pixels != null) UnityEngine.Object.DestroyImmediate(pixels);
            if (target != null) UnityEngine.Object.DestroyImmediate(target);
            if (host != null) UnityEngine.Object.DestroyImmediate(host);
            foreach (UnityEngine.Object item in transient) if (item != null) UnityEngine.Object.DestroyImmediate(item);
            if (original.IsValid() && original.isLoaded) SceneManager.SetActiveScene(original);
            EditorSceneManager.CloseScene(preview, true);
            SpeedButton.InputSuppressed = input; SpeedButton.CombatLocked = locked;
        }
    }

    public static BattleAllyController CreateAlly(Transform parent, Weapon_SO weapon)
    {
        GameObject actor = new GameObject("Chara"); actor.transform.SetParent(parent, false); actor.SetActive(false);
        AllyController data = actor.AddComponent<AllyController>();
        GameObject weaponHost = new GameObject("Weapon"); weaponHost.transform.SetParent(actor.transform, false);
        data.weapon = weaponHost.AddComponent<Weapon>(); data.weapon.weaponData = weapon;
        BattleAllyController ally = actor.AddComponent<BattleAllyController>();
        typeof(BattleAllyController).GetField("allyController", Private).SetValue(ally, data);
        var buttons = (List<SpeedButton>)typeof(BattleAllyController).GetField("activeSpeedButtons", Private).GetValue(ally);
        for (int i = 0; i < 2; i++)
        {
            GameObject go = new GameObject("Speed_" + i); go.transform.SetParent(actor.transform, false);
            SpeedButton button = go.AddComponent<SpeedButton>(); button.SetButtonType(SpeedButton.ButtonType.Ally); buttons.Add(button);
        }
        ally.SetSelectedSkillIndex(0); return ally;
    }

    public static BattleEnemyController CreateEnemy(Transform parent, EnemyData_SO data, EnemySkill_SO skill)
    {
        GameObject actor = new GameObject(data.enemyName); actor.transform.SetParent(parent, false); actor.SetActive(false);
        BattleEnemyController enemy = actor.AddComponent<BattleEnemyController>(); enemy.enemyData = data;
        var buttons = (List<SpeedButton>)typeof(BattleEnemyController).GetField("activeSpeedButtons", Private).GetValue(enemy);
        for (int i = 0; i < 2; i++)
        {
            GameObject go = new GameObject("EnemySpeed_" + i); go.transform.SetParent(actor.transform, false);
            SpeedButton button = go.AddComponent<SpeedButton>(); button.SetButtonType(SpeedButton.ButtonType.Enemy); buttons.Add(button);
        }
        enemy.SetRuntimeCopiedSkills(new[] { skill });
        typeof(BattleEnemyController).GetField("<MaxHP>k__BackingField", Private).SetValue(enemy, data.baseHP);
        return enemy;
    }
}
