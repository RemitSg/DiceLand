Shader "DiceLand/Battle Forest Ground"
{
    Properties
    {
        _SoilColor ("Earth", Color) = (0.39,0.34,0.23,1)
        _MossColor ("Moss", Color) = (0.19,0.27,0.15,1)
        _StoneColor ("Worn paving", Color) = (0.44,0.44,0.33,1)
        _GroundMap ("Painted earth and moss", 2D) = "white" {}
        _TextureScale ("Texture repeats per metre", Float) = 0.075
        _TextureWeight ("Painted texture weight", Range(0,1)) = 0.88
        _StoneMap ("Terrace paving at woodland margin", 2D) = "white" {}
        _StoneTextureWeight ("Painted paving weight", Range(0,1)) = 1
        _PavingScale ("Paving repeats per metre", Float) = 0.07
        _PavingGrowth ("Earth and moss islands", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent-100" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        // The distant floor dissolves into the painted landscape instead of ending in a straight horizon.
        // This lower decorative floor draws before the mist; the opaque battle terrace supplies combat depth.
        float ForestFloorCoverage(float3 world)
        {
            float distance = world.z + sin(world.x * 0.37) * 1.7;
            return 1.0 - smoothstep(18.0, 42.0, distance);
        }
        ENDHLSL
        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Assets/Assets/Shaders/BattleForestSurface.hlsl"
            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; half fog : TEXCOORD1; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionWS=TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS=TransformWorldToHClip(output.positionWS);
                output.fog=ComputeFogFactor(output.positionCS.z);
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                half3 color=ForestSurfaceAlbedo(input.positionWS.xz)*ForestSurfaceLighting(input.positionWS,half3(0,1,0));
                return half4(MixFog(color,input.fog),ForestFloorCoverage(input.positionWS));
            }
            ENDHLSL
        }
    }
}
