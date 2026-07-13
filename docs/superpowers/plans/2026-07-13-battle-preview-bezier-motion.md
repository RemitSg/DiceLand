# Battle Preview Bezier Motion Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make battle-tile preview entry and return faster and smoother by applying one cubic Bezier easing function to camera and pawn motion.

**Architecture:** Add a stateless shared motion helper that evaluates `B(t)=3t^2-2t^3`, then route both camera and pawn interpolation through it. Keep timing ownership in `MapBattleTilePreviewController`, using `0.18s` for every entry and `0.20s` for return, and remove repeat-preview timing state.

**Tech Stack:** Unity 2022.3/Tuanjie, C#, NUnit, Unity Test Framework

---

### Task 1: Shared Bezier easing for camera and pawn

**Files:**
- Create: `Assets/Scripts/Shared/MapBattleTilePreviewMotion.cs`
- Create: `Assets/Scripts/Shared/MapBattleTilePreviewMotion.cs.meta`
- Modify: `Assets/Scripts/MapScene/Controller/MapViewController.cs:236-279`
- Modify: `Assets/Scripts/MapScene/Manager/PlayerMapController.cs:273-318`
- Create: `Assets/Tests/EditMode/Editor/MapBattleTilePreviewMotionTests.cs`
- Create: `Assets/Tests/EditMode/Editor/MapBattleTilePreviewMotionTests.cs.meta`

- [ ] **Step 1: Write the failing easing tests**

```csharp
[TestCase(0f, 0f)]
[TestCase(0.25f, 0.15625f)]
[TestCase(0.5f, 0.5f)]
[TestCase(0.75f, 0.84375f)]
[TestCase(1f, 1f)]
public void Evaluate_ReturnsCubicBezierProgress(float input, float expected)
{
    Assert.AreEqual(expected, MapBattleTilePreviewMotion.Evaluate(input), 0.00001f);
}

[Test]
public void Evaluate_ClampsInputAndStartsAndStopsMoreGentlyThanMiddle()
{
    const float step = 0.01f;
    float startDelta = MapBattleTilePreviewMotion.Evaluate(step) - MapBattleTilePreviewMotion.Evaluate(0f);
    float middleDelta = MapBattleTilePreviewMotion.Evaluate(0.5f + step) - MapBattleTilePreviewMotion.Evaluate(0.5f);
    float endDelta = MapBattleTilePreviewMotion.Evaluate(1f) - MapBattleTilePreviewMotion.Evaluate(1f - step);

    Assert.AreEqual(0f, MapBattleTilePreviewMotion.Evaluate(-1f));
    Assert.AreEqual(1f, MapBattleTilePreviewMotion.Evaluate(2f));
    Assert.Less(startDelta, middleDelta);
    Assert.Less(endDelta, middleDelta);
}
```

- [ ] **Step 2: Run the focused tests and verify RED**

Run the Unity EditMode test class `MapBattleTilePreviewMotionTests`.

Expected: compilation fails because `MapBattleTilePreviewMotion` does not exist.

- [ ] **Step 3: Add the shared cubic Bezier evaluator**

```csharp
using UnityEngine;

public static class MapBattleTilePreviewMotion
{
    public static float Evaluate(float normalizedTime)
    {
        float t = Mathf.Clamp01(normalizedTime);
        return t * t * (3f - 2f * t);
    }
}
```

- [ ] **Step 4: Route camera and pawn interpolation through the helper**

In both camera loops, replace linear `t` with:

```csharp
float t = Mathf.Clamp01(elapsed / safeDuration);
float eased = MapBattleTilePreviewMotion.Evaluate(t);
ApplyInterpolatedCameraState(startState, destinationState, eased, false);
```

In both pawn loops, replace the existing cubic ease-out with:

```csharp
float t = Mathf.Clamp01(time / safeDuration);
float eased = MapBattleTilePreviewMotion.Evaluate(t);
```

Keep the pawn hop/visual arc based on raw `t` so it still begins and ends at zero.

- [ ] **Step 5: Run motion, camera, and pawn tests and verify GREEN**

Run Unity EditMode test classes:

```text
MapBattleTilePreviewMotionTests
MapBattleTilePreviewCameraTests
MapBattleTilePreviewPawnTests
```

Expected: all selected tests pass.

- [ ] **Step 6: Commit Task 1**

```powershell
git add -- Assets/Scripts/Shared/MapBattleTilePreviewMotion.cs Assets/Scripts/Shared/MapBattleTilePreviewMotion.cs.meta Assets/Scripts/MapScene/Controller/MapViewController.cs Assets/Scripts/MapScene/Manager/PlayerMapController.cs Assets/Tests/EditMode/Editor/MapBattleTilePreviewMotionTests.cs Assets/Tests/EditMode/Editor/MapBattleTilePreviewMotionTests.cs.meta
git commit -m "feat: smooth battle preview motion with bezier easing"
```

### Task 2: Unified fast preview timing

**Files:**
- Modify: `Assets/Scripts/MapScene/Manager/MapBattleTilePreviewController.cs:16-18,80-85,291`
- Modify: `Assets/Scripts/MapScene/Controller/MapViewController.cs:24-25,54-55`
- Modify: `Assets/Tests/EditMode/Editor/MapBattleTilePreviewControllerTests.cs:274-310`
- Modify: `Assets/Tests/EditMode/Editor/MapBattleTilePreviewCameraTests.cs:346-375`
- Test: `Assets/Tests/PlayMode/MapBattleTilePreviewPlayModeTests.cs`

- [ ] **Step 1: Replace timing expectations before production code**

Replace repeat-history assertions with a single invariant:

```csharp
[Test]
public void EveryPreview_UsesUnifiedFastEntryDuration()
{
    controller.BeginPreview(Context(), true);
    Assert.AreEqual(0.18f, GetPrivateField<float>(controller, "lastEnterDuration"), 0.001f);
    controller.CancelImmediate();

    controller.BeginPreview(Context(), true);
    Assert.AreEqual(0.18f, GetPrivateField<float>(controller, "lastEnterDuration"), 0.001f);
}
```

Update malformed serialized-duration fallback expectations to `0.18f` for entry and `0.20f` for exit.

- [ ] **Step 2: Run controller and camera tests and verify RED**

Run Unity EditMode test classes:

```text
MapBattleTilePreviewControllerTests
MapBattleTilePreviewCameraTests
```

Expected: old `0.8f`/`0.3f` timing values fail the new assertions.

- [ ] **Step 3: Unify timing constants and remove repeat tracking**

Use:

```csharp
private const float PreviewEnterDuration = 0.18f;
private const float PreviewExitDuration = 0.20f;
```

Assign `lastEnterDuration = PreviewEnterDuration` for every preview. Remove `FirstPreviewDuration`, `RepeatPreviewDuration`, the `previewedTiles` set, the `previewedTiles.Contains` branch, and `previewedTiles.Add`.

Update `MapViewController` serialized/default duration values to `0.18f` and `0.20f` so inspector defaults and public fallback properties match the coordinator.

- [ ] **Step 4: Run all battle-preview tests and builds**

Run Unity EditMode test classes:

```text
MapBattleTilePreviewMotionTests
MapBattleTilePreviewServiceTests
MapBattleTilePreviewCameraTests
MapBattleTilePreviewPawnTests
MapBattleTilePreviewOverlayTests
MapBattleTilePreviewControllerTests
```

Run Unity PlayMode test class `MapBattleTilePreviewPlayModeTests`.

Run:

```powershell
dotnet build Assembly-CSharp-Editor.csproj --no-incremental
dotnet build MobileGame.PlayModeTests.csproj --no-incremental
```

Expected: selected Unity tests pass; both builds complete with zero errors; Unity console contains zero errors.

- [ ] **Step 5: Restore Unity test-runner settings side effects**

If `ProjectSettings/EditorSettings.asset` changes only `m_EnterPlayModeOptionsEnabled`, restore it to its pre-test value `0` and verify that file has no diff.

- [ ] **Step 6: Commit Task 2**

```powershell
git add -- Assets/Scripts/MapScene/Manager/MapBattleTilePreviewController.cs Assets/Scripts/MapScene/Controller/MapViewController.cs Assets/Tests/EditMode/Editor/MapBattleTilePreviewControllerTests.cs Assets/Tests/EditMode/Editor/MapBattleTilePreviewCameraTests.cs
git commit -m "tune: speed up battle preview transitions"
```
