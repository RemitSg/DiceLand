Shader "DiceLand/Battle Forest Stone"
{
    Properties
    {
        _StoneColor ("Warm weathered stone", Color) = (0.62,0.59,0.46,1)
        _MossColor ("Moss in joints", Color) = (0.11,0.19,0.13,1)
        _StoneMap ("Painted irregular paving", 2D) = "white" {}
        _StoneTextureWeight ("Painted paving weight", Range(0,1)) = 0
        _GroundMap ("Woodland margin", 2D) = "white" {}
        _SoilColor ("Earth", Color) = (0.39,0.34,0.23,1)
        _TextureScale ("Earth repeats per metre", Float) = 0.055
        _TextureWeight ("Painted earth weight", Range(0,1)) = 1
        _PavingScale ("Paving repeats per metre", Float) = 0.07
        _PavingGrowth ("Earth and moss islands", Range(0,1)) = 0
        _LowerStepGain ("Lower terrace step shade", Range(0,1)) = 1
        _PavingReliefMap ("Paving normal XZ, cavity and height", 2D) = "gray" {}
        _PavingReliefStrength ("Paving bevel lighting", Range(0,1)) = 0
        _PavingGlintStrength ("Rough paving highlights", Range(0,0.3)) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Assets/Assets/Shaders/BattleForestSurface.hlsl"
            struct Attributes { float3 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 world : TEXCOORD0; half3 normal : TEXCOORD1; half fog : TEXCOORD2; };
            Varyings Vert(Attributes input)
            {
                Varyings o;
                o.world = TransformObjectToWorld(input.positionOS);
                o.positionCS = TransformWorldToHClip(o.world);
                o.normal = TransformObjectToWorldNormal(input.normalOS);
                o.fog = ComputeFogFactor(o.positionCS.z);
                return o;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float top = saturate(input.normal.y);
                half3 fascia = SAMPLE_TEXTURE2D(_StoneMap, sampler_StoneMap, input.world.xy * float2(_PavingScale, _PavingScale * 3.0)).rgb;
                float paving = ForestSurfacePaving(input.world.xz);
                half3 albedo = lerp(fascia, ForestSurfaceAlbedo(input.world.xz,paving), top);
                half3 color = ForestSurfaceShading(input.world, normalize(input.normal), albedo,paving);
                // Recessed steps stay in shade while the combat plane keeps its existing light.
                float lowerStep = 1.0 - smoothstep(-0.24, 0.045, input.world.y);
                color *= lerp(1.0, _LowerStepGain, lowerStep);
                return half4(MixFog(color, input.fog), 1);
            }
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ColorMask 0
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            float4 Vert(float3 p : POSITION) : SV_POSITION { return TransformObjectToHClip(p); }
            half4 Frag() : SV_Target { return 0; }
            ENDHLSL
        }
    }
}
