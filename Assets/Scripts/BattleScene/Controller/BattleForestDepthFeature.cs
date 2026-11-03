using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>Make opaque depth available before the forest's transparent air and mist.</summary>
public sealed class BattleForestDepthFeature : ScriptableRendererFeature
{
    private static readonly int LightingId = Shader.PropertyToID("_DiceLandForestLighting");
    private ForestDepthInputPass depthInput;

    public override void Create()
    {
        depthInput = new ForestDepthInputPass();
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (renderingData.cameraData.renderType != CameraRenderType.Base
            || renderingData.cameraData.cameraType != CameraType.Game
            || Shader.GetGlobalFloat(LightingId) <= 0.001f)
            return;
        renderer.EnqueuePass(depthInput);
    }

    private sealed class ForestDepthInputPass : ScriptableRenderPass
    {
        public ForestDepthInputPass()
        {
            renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;
            ConfigureInput(ScriptableRenderPassInput.Depth);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            // URP schedules its existing depth copy before this input dependency.
            // No additional drawing or intermediate colour target is needed here.
        }
    }
}
