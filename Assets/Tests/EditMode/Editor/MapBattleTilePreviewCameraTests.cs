using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class MapBattleTilePreviewCameraTests
{
    private GameObject root;
    private Camera camera;
    private MapViewController view;

    [SetUp]
    public void SetUp()
    {
        root = new GameObject("BattlePreviewCameraTest");
        camera = root.AddComponent<Camera>();
        BoardManager board = root.AddComponent<BoardManager>();
        view = root.AddComponent<MapViewController>();
        board.LoadBoard(1, Vector2Int.zero, Vector2Int.zero, Vector2Int.zero, new BoardTileData[0]);
        view.Initialize(camera, board, 2f);

        camera.transform.SetPositionAndRotation(new Vector3(1.25f, -0.75f, -10f), Quaternion.Euler(0f, 0f, 7f));
        camera.orthographic = true;
        camera.orthographicSize = 5.5f;
        camera.fieldOfView = 51f;
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(root);
    }

    [Test]
    public void ApplyBattlePreviewImmediate_SwitchesToPerspectiveAndFacesTarget()
    {
        Vector3 target = new Vector3(2f, 3f, 0f);

        view.ApplyBattlePreviewImmediate(Vector3.zero, target);

        Assert.IsTrue(view.IsBattlePreviewActive);
        Assert.IsFalse(camera.orthographic);
        Assert.AreEqual(34f, camera.fieldOfView, 0.001f);
        Assert.That(Vector3.Angle(camera.transform.forward, target - camera.transform.position), Is.LessThan(0.01f));
    }

    [Test]
    public void ApplyThenRestorePreview_RestoresEveryCapturedPropertyExactly()
    {
        MapCameraPreviewState before = view.CaptureCameraState();

        view.ApplyBattlePreviewImmediate(Vector3.zero, Vector3.up * 1.35f);
        view.RestoreBattlePreviewImmediate();

        Assert.IsFalse(view.IsBattlePreviewActive);
        Assert.AreEqual(before.orthographic, camera.orthographic);
        Assert.AreEqual(before.orthographicSize, camera.orthographicSize);
        Assert.AreEqual(before.fieldOfView, camera.fieldOfView);
        Assert.AreEqual(before.position, camera.transform.position);
        Assert.AreEqual(before.rotation, camera.transform.rotation);
    }

    [Test]
    public void LargerZoomMultiplier_PositionsCameraCloserToTarget()
    {
        Vector3 origin = new Vector3(-1f, 0f, 0f);
        Vector3 target = new Vector3(2f, 1f, 0f);

        view.ApplyBattlePreviewImmediate(origin, target, 1f);
        float normalDistance = Vector3.Distance(camera.transform.position, target);
        view.ApplyBattlePreviewImmediate(origin, target, 1.75f);
        float zoomedDistance = Vector3.Distance(camera.transform.position, target);

        Assert.Less(zoomedDistance, normalDistance);
    }

    [Test]
    public void RepeatedApplyThenRestore_ReturnsToFirstCapturedState()
    {
        MapCameraPreviewState before = view.CaptureCameraState();

        view.ApplyBattlePreviewImmediate(Vector3.zero, Vector3.up, 1f);
        view.ApplyBattlePreviewImmediate(Vector3.left, Vector3.right, 2f);
        view.RestoreBattlePreviewImmediate();

        Assert.AreEqual(before.position, camera.transform.position);
        Assert.AreEqual(before.rotation, camera.transform.rotation);
        Assert.AreEqual(before.orthographic, camera.orthographic);
        Assert.AreEqual(before.orthographicSize, camera.orthographicSize);
        Assert.AreEqual(before.fieldOfView, camera.fieldOfView);
    }

    [Test]
    public void RestoreBattlePreviewImmediate_IsIdempotent()
    {
        view.ApplyBattlePreviewImmediate(Vector3.zero, Vector3.up);
        view.RestoreBattlePreviewImmediate();
        MapCameraPreviewState restored = view.CaptureCameraState();

        view.RestoreBattlePreviewImmediate();

        Assert.AreEqual(restored.position, camera.transform.position);
        Assert.AreEqual(restored.rotation, camera.transform.rotation);
        Assert.AreEqual(restored.orthographic, camera.orthographic);
        Assert.AreEqual(restored.orthographicSize, camera.orthographicSize);
        Assert.AreEqual(restored.fieldOfView, camera.fieldOfView);
    }

    [Test]
    public void PreviewApis_AreSafeBeforeInitialization()
    {
        GameObject uninitializedRoot = new GameObject("UninitializedBattlePreviewCameraTest");
        try
        {
            MapViewController uninitialized = uninitializedRoot.AddComponent<MapViewController>();

            Assert.DoesNotThrow(() => uninitialized.CaptureCameraState());
            Assert.DoesNotThrow(() => uninitialized.ApplyBattlePreviewImmediate(Vector3.zero, Vector3.one));
            Assert.DoesNotThrow(() => uninitialized.RestoreBattlePreviewImmediate());
            Assert.IsFalse(uninitialized.PlayBattlePreviewRoutine(Vector3.zero, Vector3.one, 1.75f, 0f).MoveNext());
            Assert.IsFalse(uninitialized.RestoreBattlePreviewRoutine(0f).MoveNext());
        }
        finally
        {
            Object.DestroyImmediate(uninitializedRoot);
        }
    }

    [Test]
    public void ZeroDurationAnimationRoutines_ReachExactFinalStates()
    {
        MapCameraPreviewState before = view.CaptureCameraState();
        IEnumerator enter = view.PlayBattlePreviewRoutine(Vector3.zero, Vector3.up, 1.75f, 0f);

        Assert.IsFalse(enter.MoveNext());
        Assert.IsTrue(view.IsBattlePreviewActive);
        Assert.IsFalse(camera.orthographic);

        IEnumerator exit = view.RestoreBattlePreviewRoutine(0f);
        Assert.IsFalse(exit.MoveNext());
        Assert.IsFalse(view.IsBattlePreviewActive);
        Assert.AreEqual(before.position, camera.transform.position);
        Assert.AreEqual(before.rotation, camera.transform.rotation);
        Assert.AreEqual(before.orthographic, camera.orthographic);
        Assert.AreEqual(before.orthographicSize, camera.orthographicSize);
        Assert.AreEqual(before.fieldOfView, camera.fieldOfView);
    }

    [Test]
    public void Update_DoesNotApplyOrthographicSmoothingWhilePreviewIsActive()
    {
        view.ApplyBattlePreviewImmediate(Vector3.zero, Vector3.up);
        Vector3 previewPosition = camera.transform.position;

        typeof(MapViewController).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(view, null);

        Assert.AreEqual(previewPosition, camera.transform.position);
        Assert.IsFalse(camera.orthographic);
    }
}
