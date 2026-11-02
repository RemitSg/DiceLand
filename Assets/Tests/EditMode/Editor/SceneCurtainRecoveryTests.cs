using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public sealed class SceneCurtainRecoveryTests
{
    [Test]
    public void ReloadReusesTheExistingCanvasAndDismissesAnOrphanedBlackout()
    {
        GameObject root = new GameObject("Curtain reload test");
        try
        {
            SceneCurtainTransitionController curtain = root.AddComponent<SceneCurtainTransitionController>();
            Invoke(curtain, "EnsureOverlay");
            Invoke(curtain, "SetOverlayVisible", true);
            root.transform.Find("CurtainCanvas/Blackout").gameObject.SetActive(true);
            foreach (string field in new[] { "canvas", "rootRect", "leftCurtain", "rightCurtain", "blackout", "loadingLabel" })
                typeof(SceneCurtainTransitionController).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(curtain, null);

            Invoke(curtain, "OnEnable");

            Assert.AreEqual(1, root.GetComponentsInChildren<Canvas>(true).Length);
            Assert.IsFalse(root.transform.Find("CurtainCanvas").gameObject.activeSelf);
            Assert.IsFalse(root.transform.Find("CurtainCanvas/Blackout").gameObject.activeSelf);
            Assert.IsFalse(curtain.IsTransitioning);
        }
        finally { Object.DestroyImmediate(root); }
    }

    [Test]
    public void EditorRevealDoesNotDependOnTheGameViewRenderingAnEndOfFrame()
    {
        GameObject root = new GameObject("Curtain frame test");
        try
        {
            SceneCurtainTransitionController curtain = root.AddComponent<SceneCurtainTransitionController>();
            IEnumerator routine = (IEnumerator)Invoke(curtain, "WaitForSceneRenderReady");
            int frames = 0;
            while (routine.MoveNext() && frames++ < 20)
                Assert.IsNotInstanceOf<WaitForEndOfFrame>(routine.Current);
            Assert.Less(frames, 20);
        }
        finally { Object.DestroyImmediate(root); }
    }

    [Test]
    public void RevealRepairsTheBaseCameraWithoutChangingTheOverlayCamera()
    {
        WithTestObjects(root =>
        {
            SceneCurtainTransitionController curtain = root.AddComponent<SceneCurtainTransitionController>();
            Camera sceneCamera = CreateCamera(root, "Scene camera");
            sceneCamera.clearFlags = CameraClearFlags.Depth;
            sceneCamera.rect = new Rect(0.1f, 0.2f, 0.7f, 0.6f);
            Camera overlay = CreateCamera(root, "UI overlay");
            overlay.GetUniversalAdditionalCameraData().renderType = CameraRenderType.Overlay;
            overlay.clearFlags = CameraClearFlags.Depth;
            Rect overlayViewport = new Rect(0.2f, 0.1f, 0.6f, 0.8f);
            overlay.rect = overlayViewport;

            Invoke(curtain, "PrepareLoadedSceneForReveal");

            Assert.AreEqual(CameraClearFlags.SolidColor, sceneCamera.clearFlags);
            Assert.AreEqual(new Rect(0f, 0f, 1f, 1f), sceneCamera.rect);
            Assert.AreEqual(CameraClearFlags.Depth, overlay.clearFlags, "UI overlay must not clear the scene color.");
            Assert.AreEqual(overlayViewport, overlay.rect, "The stack owns the overlay viewport.");
        });
    }

    [Test]
    public void AnOverlayCameraAloneDoesNotMakeTheSceneReadyToReveal()
    {
        WithTestObjects(root =>
        {
            Camera overlay = CreateCamera(root, "UI overlay");
            overlay.GetUniversalAdditionalCameraData().renderType = CameraRenderType.Overlay;
            overlay.clearFlags = CameraClearFlags.Depth;
            MethodInfo ready = typeof(SceneCurtainTransitionController).GetMethod("HasRenderableSceneCamera", BindingFlags.Static | BindingFlags.NonPublic);

            Assert.IsFalse((bool)ready.Invoke(null, null));

            CreateCamera(root, "Scene camera");
            Assert.IsTrue((bool)ready.Invoke(null, null));
        });
    }

    private static Camera CreateCamera(GameObject root, string name)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(root.transform);
        return child.AddComponent<Camera>();
    }

    private static void WithTestObjects(System.Action<GameObject> action)
    {
        // Keep the runner's temporary scene and any previously open camera settings intact.
        Camera[] previous = Object.FindObjectsOfType<Camera>(true);
        bool[] enabled = new bool[previous.Length];
        Rect[] viewports = new Rect[previous.Length];
        CameraClearFlags[] flags = new CameraClearFlags[previous.Length];
        for (int i = 0; i < previous.Length; i++)
        {
            enabled[i] = previous[i].enabled;
            viewports[i] = previous[i].rect;
            flags[i] = previous[i].clearFlags;
            previous[i].enabled = false;
        }
        GameObject root = new GameObject("Curtain camera test");
        try { action(root); }
        finally
        {
            Object.DestroyImmediate(root);
            for (int i = 0; i < previous.Length; i++)
                if (previous[i] != null)
                {
                    previous[i].enabled = enabled[i];
                    previous[i].rect = viewports[i];
                    previous[i].clearFlags = flags[i];
                }
        }
    }

    private static object Invoke(SceneCurtainTransitionController target, string name, params object[] args)
    {
        return typeof(SceneCurtainTransitionController).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, args);
    }
}
