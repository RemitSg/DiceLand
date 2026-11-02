using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Renders a deterministic movie of production UI in MapVisualTools' disposable scene.</summary>
public static class MapResourceFeedbackVisualTools
{
    public const string OutputPath = "outputs/agent-battle/resource-feedback-20261102";
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    [MenuItem("Tools/Map Visuals/Capture Resource Feedback")]
    public static void Capture()
    {
        Directory.CreateDirectory(OutputPath);
        File.WriteAllText(Path.Combine(OutputPath, "preview-complete.json"), "{\"success\":false,\"running\":true}");
        MapEvent_SO sample = ScriptableObject.CreateInstance<MapEvent_SO>();
        try
        {
            sample.eventName = "旧路上的钱袋";
            sample.eventContent = "路边的青苔间，露出一只磨损的钱袋。\n\n你拨开落叶，袋中传来清脆的金币碰撞声。";
            sample.options.Add(new MapEventOptionData { optionLabel = "收下钱袋", consequenceDescription = "获得 107 金币。" });
            sample.options.Add(new MapEventOptionData { optionLabel = "放回原处", consequenceDescription = "继续赶路。" });
            MapVisualTools.Capture("feedback-background.png", 2340, 1080, MapBackgroundPhase.Morning, false, 1, null, (host, camera) =>
            {
                const int width = 2340, height = 1080, fps = 24, frameCount = 150;
                MapHUD hud = host.GetComponent<MapHUD>();
                RunState state = (RunState)typeof(MapHUD).GetField("currentRunState", Private).GetValue(hud);
                TMP_Text wallet = (TMP_Text)typeof(MapHUD).GetField("moneyText", Private).GetValue(hud);
                ActionPointSystem points = host.GetComponent<ActionPointSystem>();
                state.money = 100;
                wallet.text = "100";
                MapResourceFeedback feedback = host.AddComponent<MapResourceFeedback>();
                feedback.Bind(state, wallet);
                typeof(MapHUD).GetField("resourceFeedback", Private).SetValue(hud, feedback);
                hud.Refresh(state, points);
                hud.ApplyViewport(new Vector2(width, height), new Rect(0, 0, width, height));
                Image hpFill = host.GetComponentsInChildren<Image>().First(x => x.name == "Fill" && x.transform.parent.name == "HpBar");
                TMP_Text hpLabel = host.GetComponentsInChildren<TMP_Text>().First(x => x.name == "HpText");
                GameObject panelHost = new GameObject("FeedbackEventSample");
                panelHost.transform.SetParent(host.transform, false);
                MapEventOverlay overlay = panelHost.AddComponent<MapEventOverlay>();
                overlay.ConfigureFonts(wallet.font, wallet.font);
                overlay.Show(new BoardTileData { tileType = MapTileType.Event }, sample,
                    (BoardTileData tile, MapEvent_SO data, MapEventOptionData option, out string message, out string result, out MapEvent_SO next) =>
                    {
                        state.money += 107;
                        feedback.ObserveMoney(state.money);
                        message = "获得 107 金币。";
                        result = "金币在掌心轻轻一碰，你把钱袋收进了行囊。\n\n获得 107 金币。";
                        next = null;
                        return true;
                    });
                Canvas eventCanvas = panelHost.GetComponentInChildren<Canvas>();
                WorldCanvas(eventCanvas, camera, width, height, -3f);
                overlay.ApplyViewport(new Vector2(width, height), new Rect(0, 0, width, height));
                typeof(MapEventOverlay).GetMethod("RefreshLayout", Private).Invoke(overlay, null);
                RenderTexture previous = camera.targetTexture, previousActive = RenderTexture.active;
                RenderTexture target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
                Texture2D pixels = new Texture2D(width, height, TextureFormat.RGB24, false);
                List<string> trace = new List<string> { "frame,time,money,hpFill" };
                try
                {
                    camera.targetTexture = target;
                    for (int frame = 0; frame < frameCount; frame++)
                    {
                        if (frame == 6)
                        {
                            ((Button)typeof(MapEventOverlay).GetField("confirmButton", Private).GetValue(overlay)).onClick.Invoke();
                            Canvas canvas = host.GetComponentsInChildren<Canvas>().First(x => x.name == "MapResourceFeedbackCanvas");
                            WorldCanvas(canvas, camera, width, height, -4f);
                        }
                        if (frame == 48)
                        {
                            overlay.Hide();
                            state.money -= 75;
                            feedback.ObserveMoney(state.money);
                        }
                        if (frame == 78 || frame == 108)
                        {
                            state.partyMembers[0].currentHP = frame == 78 ? 6 : 15;
                            // The editor game-view dimensions differ from this camera's target.
                            // Advance the same presentation path without reapplying Screen.safeArea.
                            feedback.PresentHealth(state.partyMembers[0], hpFill, hpLabel, state.partyMembers[0].currentHP, state.partyMembers[0].maxHP);
                        }
                        feedback.Advance(1f / fps);
                        Canvas.ForceUpdateCanvases();
                        foreach (Canvas canvas in host.GetComponentsInChildren<Canvas>())
                        {
                            foreach (TMP_Text text in canvas.GetComponentsInChildren<TMP_Text>()) text.ForceMeshUpdate(true, true);
                            foreach (Graphic graphic in canvas.GetComponentsInChildren<Graphic>())
                                if (!(graphic is TMP_Text) && !(graphic is TMP_SubMeshUI))
                                {
                                    graphic.SetAllDirty();
                                    graphic.Rebuild(CanvasUpdate.PreRender);
                                }
                        }
                        Canvas.ForceUpdateCanvases();
                        camera.Render();
                        RenderTexture.active = target;
                        pixels.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                        pixels.Apply();
                        if (frame == 14 || frame == 83)
                            File.WriteAllLines(Path.Combine(OutputPath, $"feedback-{frame:D3}.layout.txt"), feedback.GetComponentsInChildren<MapFeedbackGraphic>().Select(graphic =>
                            {
                                Mesh mesh = graphic.canvasRenderer.GetMesh();
                                return graphic.name + " shape=" + graphic.shape + " rect=" + graphic.rectTransform.rect + " color=" + graphic.color +
                                    " culled=" + graphic.canvasRenderer.cull + " vertices=" + (mesh != null ? mesh.vertexCount : 0) +
                                    " alpha=" + (mesh != null && mesh.vertexCount > 0 ? mesh.colors32.Max(c => c.a) : 0);
                            }));
                        File.WriteAllBytes(Path.Combine(OutputPath, $"feedback-{frame:D3}.png"), pixels.EncodeToPNG());
                        trace.Add(string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0},{1:F4},{2},{3:F4}", frame, frame / (float)fps, feedback.DisplayedMoney, hpFill.rectTransform.anchorMax.x));
                    }
                    File.WriteAllLines(Path.Combine(OutputPath, "feedback-trace.csv"), trace);
                    File.WriteAllText(Path.Combine(OutputPath, "preview-complete.json"), "{\"success\":true,\"running\":false,\"frames\":150,\"fps\":24,\"width\":2340,\"height\":1080,\"playerSavesUsed\":false,\"sampleResourcesOnly\":true,\"audioRecorded\":false}");
                }
                finally
                {
                    camera.targetTexture = previous;
                    RenderTexture.active = previousActive;
                    UnityEngine.Object.DestroyImmediate(pixels);
                    UnityEngine.Object.DestroyImmediate(target);
                }
            });
        }
        catch (Exception error)
        {
            File.WriteAllText(Path.Combine(OutputPath, "preview-complete.json"), "{\"success\":false,\"running\":false}");
            File.WriteAllText(Path.Combine(OutputPath, "preview-error.txt"), error.ToString());
            throw;
        }
        finally { UnityEngine.Object.DestroyImmediate(sample); }
    }

    private static void WorldCanvas(Canvas canvas, Camera camera, int width, int height, float depth)
    {
        canvas.gameObject.SetActive(false);
        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler != null) scaler.enabled = false;
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = camera;
        RectTransform rect = (RectTransform)canvas.transform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(1080f * width / height, 1080f);
        rect.position = new Vector3(0, 0, depth);
        rect.localScale = Vector3.one * (camera.orthographicSize / 540f);
        canvas.gameObject.SetActive(true);
    }
}
