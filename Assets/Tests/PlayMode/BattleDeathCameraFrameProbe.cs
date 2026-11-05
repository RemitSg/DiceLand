using UnityEngine;

/// <summary>Samples the rendered pose after the death layer and world HUD, rather than the restored update-time base.</summary>
[DefaultExecutionOrder(1000)]
public sealed class BattleDeathCameraFrameProbe : MonoBehaviour
{
    public Camera observed;
    public Vector3 position;
    public float lens;
    private void LateUpdate()
    {
        if (observed == null) return;
        position = observed.transform.position; lens = observed.fieldOfView;
    }
}
