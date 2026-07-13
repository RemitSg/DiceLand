using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class MapBattleTilePreviewPlayModeTests
{
    private MapBattleTilePreviewPlayModeFixture fixture;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        fixture = new MapBattleTilePreviewPlayModeFixture();
        fixture.Build();
        yield return null;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        fixture?.TearDown();
        fixture = null;
        yield return null;
    }

    [UnityTest]
    public IEnumerator AnimatedCancel_PreservesCoordinateActionPointsAndCamera()
    {
        Vector3 cameraPosition = fixture.Camera.transform.position;
        Quaternion cameraRotation = fixture.Camera.transform.rotation;
        bool orthographic = fixture.Camera.orthographic;

        Assert.IsTrue(fixture.BeginPreview(immediate: false));
        yield return fixture.WaitForState("Ready", 2f);

        fixture.Cancel(immediate: false);
        yield return fixture.WaitForState("Hidden", 1f);

        Assert.AreEqual(fixture.OriginCoordinate, fixture.PlayerCoordinate);
        Assert.AreEqual(5, fixture.CurrentActionPoints);
        Assert.AreEqual(orthographic, fixture.Camera.orthographic);
        Assert.Less(Vector3.Distance(cameraPosition, fixture.Camera.transform.position), 0.001f);
        Assert.Less(Quaternion.Angle(cameraRotation, fixture.Camera.transform.rotation), 0.01f);
    }

    [UnityTest]
    public IEnumerator DoubleConfirm_CommitsMatchingPreviewOnceAndSpendsCostOnce()
    {
        Assert.IsTrue(fixture.BeginPreview(immediate: true));

        fixture.Confirm();
        fixture.Confirm();
        yield return null;

        Assert.AreEqual(1, fixture.CommitCount);
        Assert.AreEqual(fixture.TargetCoordinate, fixture.PlayerCoordinate);
        Assert.AreEqual(3, fixture.CurrentActionPoints);
        Assert.AreEqual("Hidden", fixture.StateName);
        Assert.IsFalse(fixture.IsOverlayVisible);
    }

    [UnityTest]
    public IEnumerator LockedBoss_PreviewsKeyRequirementWithoutAllowingConfirm()
    {
        fixture.ConfigureLockedBoss(currentKeys: 1, requiredKeys: 3);

        Assert.IsTrue(fixture.BeginPreview(immediate: true));
        yield return null;

        Assert.AreEqual("Ready", fixture.StateName);
        Assert.IsFalse(fixture.ConfirmButton.interactable);
        Assert.That(fixture.AllOverlayText, Does.Contain("1/3"));
        Assert.AreEqual(0, fixture.CommitCount);
        Assert.AreEqual(5, fixture.CurrentActionPoints);
        Assert.AreEqual(fixture.OriginCoordinate, fixture.PlayerCoordinate);
    }

    [UnityTest]
    public IEnumerator ImmediatePauseCleanup_RestoresPreviewWithoutCommitting()
    {
        Assert.IsTrue(fixture.BeginPreview(immediate: true));
        Assert.AreEqual("Ready", fixture.StateName);

        fixture.CancelImmediate();
        yield return null;

        Assert.AreEqual("Hidden", fixture.StateName);
        Assert.AreEqual(0, fixture.CommitCount);
        Assert.AreEqual(5, fixture.CurrentActionPoints);
        Assert.AreEqual(fixture.OriginCoordinate, fixture.PlayerCoordinate);
        Assert.IsFalse(fixture.IsOverlayVisible);
    }
}

internal sealed class MapBattleTilePreviewPlayModeFixture
{
    private const string RuntimeAssemblyName = "Assembly-CSharp";

    private readonly List<UnityEngine.Object> createdObjects = new List<UnityEngine.Object>();
    private readonly HashSet<int> preExistingEventSystemIds = new HashSet<int>();
    private object board;
    private object actionPoints;
    private object player;
    private object view;
    private object overlay;
    private object controller;
    private object runState;
    private object origin;
    private object target;
    private object currentContext;

    public Camera Camera { get; private set; }
    public int CommitCount { get; private set; }

    public int CurrentActionPoints => (int)GetProperty(actionPoints, "CurrentPoints");
    public Vector2Int PlayerCoordinate => (Vector2Int)GetField(runState, "playerGridPosition");
    public Vector2Int OriginCoordinate => (Vector2Int)GetField(origin, "coord");
    public Vector2Int TargetCoordinate => (Vector2Int)GetField(target, "coord");
    public string StateName => GetProperty(controller, "State").ToString();
    public bool IsOverlayVisible => (bool)GetProperty(overlay, "IsVisible");
    public Button ConfirmButton => FindButton("ConfirmButton");
    public string AllOverlayText
    {
        get
        {
            Type textType = Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro");
            Assert.IsNotNull(textType);
            Component[] labels = ((Component)overlay).GetComponentsInChildren(textType, true);
            return string.Join("|", labels.Select(label => GetProperty(label, "text") as string));
        }
    }

    public void Build()
    {
        EventSystem[] existingSystems = UnityEngine.Object.FindObjectsByType<EventSystem>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        for (int i = 0; i < existingSystems.Length; i++)
            preExistingEventSystemIds.Add(existingSystems[i].GetInstanceID());

        GameObject root = CreateGameObject("Battle Tile Preview PlayMode Fixture");
        board = AddRuntimeComponent(root, "BoardManager");
        actionPoints = AddRuntimeComponent(root, "ActionPointSystem");
        player = AddRuntimeComponent(root, "PlayerMapController");
        view = AddRuntimeComponent(root, "MapViewController");
        controller = AddRuntimeComponent(root, "MapBattleTilePreviewController");

        GameObject cameraObject = CreateGameObject("Battle Tile Preview Camera");
        cameraObject.transform.SetParent(root.transform, false);
        Camera = cameraObject.AddComponent<Camera>();

        GameObject overlayObject = CreateGameObject("Battle Tile Preview Overlay");
        overlayObject.transform.SetParent(root.transform, false);
        overlay = AddRuntimeComponent(overlayObject, "MapBattleTilePreviewOverlay");

        Type tileType = RuntimeType("BoardTileData");
        Type mapTileType = RuntimeType("MapTileType");
        origin = Activator.CreateInstance(tileType);
        SetField(origin, "coord", Vector2Int.zero);
        SetField(origin, "tileType", Enum.Parse(mapTileType, "Empty"));
        SetField(origin, "isVisited", true);
        target = Activator.CreateInstance(tileType);
        SetField(target, "coord", Vector2Int.right);
        SetField(target, "tileType", Enum.Parse(mapTileType, "Battle"));
        SetField(target, "previewDangerRating", 5);

        object tiles = CreateRuntimeList(tileType, origin, target);
        Invoke(board, "LoadBoard", 2, Vector2Int.zero, Vector2Int.right, Vector2Int.zero, tiles);
        Invoke(actionPoints, "Initialize", 5);
        runState = Activator.CreateInstance(RuntimeType("RunState"));
        SetField(runState, "playerGridPosition", Vector2Int.zero);
        Invoke(player, "Initialize", root.transform, board, actionPoints, null, runState);
        Invoke(player, "SetPosition", origin, false);
        Invoke(view, "Initialize", Camera, board, 2f);

        ConfigureController();
        currentContext = CreateContext(target, canConfirm: true, blockReason: string.Empty);
    }

    public void ConfigureLockedBoss(int currentKeys, int requiredKeys)
    {
        SetField(runState, "currentKeys", currentKeys);
        SetField(runState, "requiredKeys", requiredKeys);
        SetField(target, "tileType", Enum.Parse(RuntimeType("MapTileType"), "Boss"));
        MethodInfo buildContext = RuntimeType("MapBattleTilePreviewService").GetMethod(
            "BuildContext",
            BindingFlags.Public | BindingFlags.Static);
        Assert.IsNotNull(buildContext);
        currentContext = buildContext.Invoke(null, new[] { runState, target, 2, (object)true });
    }

    public bool BeginPreview(bool immediate)
    {
        return (bool)Invoke(controller, "BeginPreview", currentContext, immediate);
    }

    public void Confirm()
    {
        Invoke(controller, "Confirm");
    }

    public void Cancel(bool immediate)
    {
        Invoke(controller, "Cancel", immediate);
    }

    public void CancelImmediate()
    {
        Invoke(controller, "CancelImmediate");
    }

    public IEnumerator WaitForState(string expected, float timeout)
    {
        float deadline = Time.realtimeSinceStartup + timeout;
        while (StateName != expected && Time.realtimeSinceStartup < deadline)
            yield return null;
        Assert.AreEqual(expected, StateName);
    }

    public void TearDown()
    {
        for (int i = createdObjects.Count - 1; i >= 0; i--)
        {
            UnityEngine.Object created = createdObjects[i];
            if (created != null)
                UnityEngine.Object.Destroy(created);
        }
        createdObjects.Clear();

        EventSystem[] systems = UnityEngine.Object.FindObjectsByType<EventSystem>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        for (int i = 0; i < systems.Length; i++)
        {
            EventSystem system = systems[i];
            if (system != null && !preExistingEventSystemIds.Contains(system.GetInstanceID()))
                UnityEngine.Object.Destroy(system.gameObject);
        }
    }

    private void ConfigureController()
    {
        MethodInfo configure = RuntimeType("MapBattleTilePreviewController").GetMethod("Configure", BindingFlags.Public | BindingFlags.Instance);
        Assert.IsNotNull(configure);
        Type commitDelegateType = configure.GetParameters()[4].ParameterType;
        MethodInfo invokeMethod = commitDelegateType.GetMethod("Invoke");
        ParameterExpression tileParameter = Expression.Parameter(invokeMethod.GetParameters()[0].ParameterType, "tile");
        ParameterExpression costParameter = Expression.Parameter(typeof(int), "cost");
        MethodInfo bridge = GetType().GetMethod(nameof(CommitBridge), BindingFlags.NonPublic | BindingFlags.Instance);
        MethodCallExpression bridgeCall = Expression.Call(
            Expression.Constant(this),
            bridge,
            Expression.Convert(tileParameter, typeof(object)),
            costParameter);
        Delegate commitDelegate = Expression.Lambda(
            commitDelegateType,
            Expression.Convert(bridgeCall, invokeMethod.ReturnType),
            tileParameter,
            costParameter).Compile();

        configure.Invoke(controller, new[] { view, player, board, overlay, commitDelegate, null });
    }

    private object CommitBridge(object tile, int cost)
    {
        CommitCount++;
        bool moved = (bool)Invoke(player, "TryMoveTo", tile, cost);
        if (!moved)
            return Enum.Parse(RuntimeType("MapBattleTilePreviewCommitResult"), "NotCommitted");

        Invoke(player, "CompletePendingBattlePreviewArrival");
        return Enum.Parse(RuntimeType("MapBattleTilePreviewCommitResult"), "MovementCommittedWithoutHandoff");
    }

    private object CreateContext(object tile, bool canConfirm, string blockReason)
    {
        object context = Activator.CreateInstance(RuntimeType("MapBattleTilePreviewContext"));
        SetField(context, "tile", tile);
        SetField(context, "moveCost", 2);
        SetField(context, "dangerRating", 5);
        SetField(context, "attributeNames", new[] { "Fire", "Dark" });
        SetField(context, "canPreview", true);
        SetField(context, "canConfirm", canConfirm);
        SetField(context, "blockReason", blockReason);
        SetField(context, "confirmLabel", "Enter Battle - 2 AP");
        return context;
    }

    private Button FindButton(string name)
    {
        Button button = ((Component)overlay).GetComponentsInChildren<Button>(true).FirstOrDefault(item => item.name == name);
        Assert.IsNotNull(button, $"Missing button {name}.");
        return button;
    }

    private GameObject CreateGameObject(string name)
    {
        GameObject gameObject = new GameObject(name);
        createdObjects.Add(gameObject);
        return gameObject;
    }

    private static object AddRuntimeComponent(GameObject targetObject, string typeName)
    {
        return targetObject.AddComponent(RuntimeType(typeName));
    }

    private static object CreateRuntimeList(Type elementType, params object[] values)
    {
        IList list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType));
        for (int i = 0; i < values.Length; i++)
            list.Add(values[i]);
        return list;
    }

    private static object Invoke(object targetObject, string methodName, params object[] arguments)
    {
        MethodInfo method = targetObject.GetType().GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(method, $"Missing method {methodName} on {targetObject.GetType().Name}.");
        return method.Invoke(targetObject, CompleteOptionalArguments(method, arguments));
    }

    private static object[] CompleteOptionalArguments(MethodInfo method, object[] arguments)
    {
        ParameterInfo[] parameters = method.GetParameters();
        if (parameters.Length == arguments.Length)
            return arguments;

        object[] completed = new object[parameters.Length];
        for (int i = 0; i < parameters.Length; i++)
        {
            if (i < arguments.Length)
                completed[i] = arguments[i];
            else
            {
                Assert.IsTrue(parameters[i].IsOptional, $"Missing required argument {parameters[i].Name}.");
                completed[i] = parameters[i].DefaultValue;
            }
        }
        return completed;
    }

    private static object GetProperty(object targetObject, string propertyName)
    {
        PropertyInfo property = targetObject.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(property, $"Missing property {propertyName} on {targetObject.GetType().Name}.");
        return property.GetValue(targetObject);
    }

    private static object GetField(object targetObject, string fieldName)
    {
        FieldInfo field = targetObject.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(field, $"Missing field {fieldName} on {targetObject.GetType().Name}.");
        return field.GetValue(targetObject);
    }

    private static void SetField(object targetObject, string fieldName, object value)
    {
        FieldInfo field = targetObject.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(field, $"Missing field {fieldName} on {targetObject.GetType().Name}.");
        field.SetValue(targetObject, value);
    }

    private static Type RuntimeType(string typeName)
    {
        Type type = Type.GetType(typeName + ", " + RuntimeAssemblyName);
        Assert.IsNotNull(type, $"Runtime type {typeName} was not found.");
        return type;
    }
}
