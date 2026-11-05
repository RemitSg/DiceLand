using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>Captures production death callbacks in a disposable copy of the saved battle scene.</summary>
public static class BattleDeathVfxVisualTools
{
    public const string OutputPath = "outputs/agent-battle/death-camera-20261105";
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    [MenuItem("Tools/Battle Visuals/Capture Death Dissolution")]
    public static void CaptureDefault() { Capture("ally", false); Capture("enemy", true); }

    public static string Capture(string clip, bool enemy, int frames = 150, int width = 960, int height = 540)
    {
        string folder = Path.GetFullPath(Path.Combine(OutputPath, clip)); Directory.CreateDirectory(folder);
        Action<Camera> render = camera =>
        {
            GameObject[] roots = camera.gameObject.scene.GetRootGameObjects();
            BattleManager manager = roots.SelectMany(g => g.GetComponentsInChildren<BattleManager>(true)).First();
            Transform actor = enemy ? manager.enemiesParent.transform.GetChild(0) : manager.alliesParent.transform.GetChild(0);
            foreach (Transform other in manager.alliesParent.transform) if (other != actor) other.gameObject.SetActive(false);
            foreach (Transform other in manager.enemiesParent.transform) if (other != actor) other.gameObject.SetActive(false);
            foreach (Canvas canvas in roots.SelectMany(g => g.GetComponentsInChildren<Canvas>(true))) canvas.gameObject.SetActive(false);
            foreach (SpeedButton button in actor.GetComponentsInChildren<SpeedButton>(true)) button.gameObject.SetActive(false);
            BattleActorVisualUtility.TryGetActorSpriteBounds(actor, out Bounds bounds);
            Vector3 direction = enemy ? Vector3.right : Vector3.left;
            Vector3 target = bounds.center + Vector3.up * bounds.size.y * 0.14f;
            float distance = Mathf.Max(2, bounds.size.y) * 3.2f;
            camera.transform.position = target + new Vector3(0, distance * 0.12f, -distance);
            camera.transform.LookAt(target); camera.fieldOfView = 42; camera.aspect = (float)width / height;
            camera.GetUniversalAdditionalCameraData().cameraStack.Clear();
            foreach (BattleLookController look in roots.SelectMany(g => g.GetComponentsInChildren<BattleLookController>(true))) look.PrepareCamera(camera);
            BattleFeedbackSystem feedback = roots.SelectMany(g => g.GetComponentsInChildren<BattleFeedbackSystem>(true)).First();
            BattlePresentationSystem presentation = roots.SelectMany(g => g.GetComponentsInChildren<BattlePresentationSystem>(true)).First();
            typeof(BattlePresentationSystem).GetField("battleCamera", Private).SetValue(presentation, camera);
            typeof(BattlePresentationSystem).GetField("closeupCamera", Private).SetValue(presentation, null);
            typeof(BattleFeedbackSystem).GetField("presentationSystem", Private).SetValue(feedback, presentation);
            bool slowMotion = (bool)typeof(BattleFeedbackSystem).GetField("enableDeathSlowMotion", Private).GetValue(feedback);
            float slowScale = (float)typeof(BattleFeedbackSystem).GetField("deathSlowMotionScale", Private).GetValue(feedback);
            float slowDuration = (float)typeof(BattleFeedbackSystem).GetField("deathSlowMotionDuration", Private).GetValue(feedback);
            float recovery = (float)typeof(BattleFeedbackSystem).GetField("deathSlowMotionRecoveryDuration", Private).GetValue(feedback);
            AnimationCurve curve = (AnimationCurve)typeof(BattleFeedbackSystem).GetField("deathSlowMotionRecoveryCurve", Private).GetValue(feedback);
            // Sample the production slow-motion curve without changing the user's global Time settings in edit mode.
            typeof(BattleFeedbackSystem).GetField("enableDeathSlowMotion", Private).SetValue(feedback, false);
            BattleDeathVfx effect = null;
            BattleDeathCameraMotion motion = null;
            Vector3 startingPosition = camera.transform.position;
            float maximumTravel = 0, minimumFov = camera.fieldOfView, maximumFov = camera.fieldOfView;
            int clippedFrames = 0;
            RenderTexture buffer = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            Texture2D pixels = new Texture2D(width, height, TextureFormat.RGB24, false);
            RenderTexture previous = RenderTexture.active, oldTarget = camera.targetTexture;
            try
            {
                buffer.Create(); camera.targetTexture = buffer;
                for (int frame = 0; frame < frames; frame++)
                {
                    if (motion != null) motion.RestoreBasePose();
                    float visualDelta = 0;
                    if (frame == 12)
                    {
                        if (enemy)
                        {
                            BattleEnemyController controller = actor.GetComponent<BattleEnemyController>() ?? actor.gameObject.AddComponent<BattleEnemyController>();
                            controller.RegisterLastHitDirection(direction);
                            controller.Died += dead => typeof(BattleFeedbackSystem).GetMethod("OnEnemyDied", Private).Invoke(feedback, new object[] { dead });
                            controller.KillInstantly();
                        }
                        else
                        {
                            BattleAllyController controller = actor.GetComponent<BattleAllyController>() ?? actor.gameObject.AddComponent<BattleAllyController>();
                            controller.RegisterLastHitDirection(direction);
                            controller.Died += dead => typeof(BattleFeedbackSystem).GetMethod("OnAllyDied", Private).Invoke(feedback, new object[] { dead });
                            controller.KillInstantly();
                        }
                        effect = actor.gameObject.scene.GetRootGameObjects().Select(g => g.GetComponent<BattleDeathVfx>()).First(e => e != null);
                        motion = presentation.GetComponent<BattleDeathCameraMotion>();
                    }
                    else if (effect != null)
                    {
                        float age = (frame - 12 - 0.5f) / 30;
                        float scale = 1;
                        if (slowMotion && age < slowDuration)
                        {
                            float hold = Mathf.Max(0, slowDuration - recovery);
                            float t = recovery > 0 ? Mathf.Clamp01((age - hold) / recovery) : 0;
                            scale = Mathf.Lerp(slowScale, 1, curve != null ? curve.Evaluate(t) : t);
                        }
                        visualDelta = BattleDeathVfx.VisualTimeScale(scale) / 30;
                        effect.Advance(visualDelta);
                    }
                    if (motion != null) { motion.Advance(visualDelta); motion.ApplyPose(); }
                    maximumTravel = Mathf.Max(maximumTravel, Vector3.Distance(startingPosition, camera.transform.position));
                    minimumFov = Mathf.Min(minimumFov, camera.fieldOfView); maximumFov = Mathf.Max(maximumFov, camera.fieldOfView);
                    if (effect != null && effect.HasVisibleGeometry)
                    {
                        Bounds visible = effect.VisualBounds; bool clipped = false;
                        for (int corner = 0; corner < 8; corner++)
                        {
                            Vector3 point = visible.center + Vector3.Scale(visible.extents, new Vector3((corner & 1) == 0 ? -1 : 1, (corner & 2) == 0 ? -1 : 1, (corner & 4) == 0 ? -1 : 1));
                            Vector3 view = camera.WorldToViewportPoint(point);
                            if (view.z <= camera.nearClipPlane || view.x < 0 || view.x > 1 || view.y < 0 || view.y > 1) clipped = true;
                        }
                        if (clipped) clippedFrames++;
                    }
                    foreach (BattleContactShadowController shadows in roots.SelectMany(g => g.GetComponentsInChildren<BattleContactShadowController>(true))) shadows.UpdateShadows();
                    foreach (BattleActorSunShadowController shadows in roots.SelectMany(g => g.GetComponentsInChildren<BattleActorSunShadowController>(true))) shadows.UpdateShadows();
                    camera.Render(); RenderTexture.active = buffer;
                    pixels.ReadPixels(new Rect(0, 0, width, height), 0, 0); pixels.Apply();
                    File.WriteAllBytes(Path.Combine(folder, frame.ToString("D3") + ".png"), pixels.EncodeToPNG());
                }
                File.WriteAllText(Path.Combine(folder, "capture.json"), JsonUtility.ToJson(new CaptureReport { frames = frames, fps = 30, width = width, height = height, enemy = enemy, direction = direction, slowMotion = slowMotion, effectComplete = effect != null && effect.IsComplete, fragments = effect != null ? effect.FragmentCount : 0, cameraMotionComplete = motion != null && !motion.IsActive, clippedFrames = clippedFrames, maximumCameraTravel = maximumTravel, minimumFov = minimumFov, maximumFov = maximumFov }, true));
            }
            finally
            {
                if (motion != null) motion.Cancel();
                camera.targetTexture = oldTarget; RenderTexture.active = previous;
                UnityEngine.Object.DestroyImmediate(pixels); buffer.Release(); UnityEngine.Object.DestroyImmediate(buffer);
            }
        };
        // Reuses the existing capture's scene, lighting setup and finally-based resource restoration.
        typeof(BattleLookPreviewTools).GetMethod("CapturePreview", BindingFlags.Static | BindingFlags.NonPublic)
            .Invoke(null, new object[] { "death-vfx-stage.png", false, -1, width, height, render });
        return folder;
    }

    [Serializable] private sealed class CaptureReport
    {
        public int frames, fps, width, height, fragments, clippedFrames;
        public bool enemy, slowMotion, effectComplete, cameraMotionComplete;
        public float maximumCameraTravel, minimumFov, maximumFov;
        public Vector3 direction;
    }
}
