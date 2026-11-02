Shader "DiceLand/Battle Forest Foliage"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Cutoff ("Alpha cutoff", Range(0,1)) = 0.4
        _SwayAmplitude ("Canopy breeze (metres)", Range(0,0.2)) = 0.045
        _DefocusTexels ("Near foliage softness", Range(0,8)) = 0
        _HighlightGain ("Sunlit canopy highlight gain", Range(1,2)) = 1
        _PaintedFill ("Painted shadow color and strength", Color) = (0.36,0.40,0.32,0)
        _SceneLightingWeight ("World canopy lighting", Range(0,1)) = 0
        [HideInInspector] _RendererColor ("Renderer color", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable external alpha", Float) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="TransparentCutout" "Queue"="AlphaTest" "CanUseSpriteAtlas"="True" }
        Cull Off
        ZWrite On
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Assets/Assets/Shaders/BattleForestLighting.hlsl"
        CBUFFER_START(UnityPerMaterial)
        float4 _MainTex_ST, _MainTex_TexelSize;
        half4 _Color, _PaintedFill;
        float _Cutoff, _EnableExternalAlpha, _SwayAmplitude, _DefocusTexels, _HighlightGain, _SceneLightingWeight;
        CBUFFER_END
        TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
        TEXTURE2D(_AlphaTex); SAMPLER(sampler_AlphaTex);
        struct Attributes { float3 positionOS : POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
        struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; half fog : TEXCOORD1; float3 world : TEXCOORD2; UNITY_VERTEX_OUTPUT_STEREO };
        Varyings Vert(Attributes input)
        {
            Varyings output=(Varyings)0;
            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
            #ifdef UNITY_INSTANCING_ENABLED
                input.positionOS=UnityFlipSprite(input.positionOS,unity_SpriteFlip);
            #else
                input.positionOS=UnityFlipSprite(input.positionOS,unity_SpriteProps.xy);
            #endif
            float3 positionWS=TransformObjectToWorld(input.positionOS);
            // Roots stay planted; only the upper canopy moves, identically in color and depth passes.
            float breeze=sin(_Time.y*0.63+positionWS.x*0.18)+0.35*sin(_Time.y*1.07+positionWS.z*0.23);
            positionWS.x+=breeze*_SwayAmplitude*pow(saturate(input.uv.y),3.0);
            output.positionCS=TransformWorldToHClip(positionWS);
            output.world=positionWS;
            output.uv=TRANSFORM_TEX(input.uv,_MainTex);
            output.color=input.color*_Color*unity_SpriteColor;
            output.fog=ComputeFogFactor(output.positionCS.z);
            return output;
        }
        half4 SampleSprite(Varyings input)
        {
            half4 color=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,input.uv);
            if (_DefocusTexels > 0.01)
            {
                // SpriteRenderer overrides _MainTex per draw, but the material texel size may
                // still describe its white fallback. Screen derivatives stay tied to this sprite.
                float2 offset=fwidth(input.uv)*_DefocusTexels;
                color=color*0.36;
                color+=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,input.uv+float2(offset.x,0))*0.16;
                color+=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,input.uv-float2(offset.x,0))*0.16;
                color+=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,input.uv+float2(0,offset.y))*0.16;
                color+=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,input.uv-float2(0,offset.y))*0.16;
            }
            #if ETC1_EXTERNAL_ALPHA
                color.a=lerp(color.a,SAMPLE_TEXTURE2D(_AlphaTex,sampler_AlphaTex,input.uv).r,_EnableExternalAlpha);
            #endif
            color*=input.color;
            clip(color.a-_Cutoff);
            return color;
        }
        ENDHLSL
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                half4 color=SampleSprite(input);
                half highlight=smoothstep(0.36,0.82,dot(color.rgb,half3(0.2126,0.7152,0.0722)));
                color.rgb*=lerp(1.0,_HighlightGain,highlight);
                half shade=1.0-smoothstep(0.04,0.40,dot(color.rgb,half3(0.2126,0.7152,0.0722)));
                color.rgb+=_PaintedFill.rgb*_PaintedFill.a*shade*input.color.rgb;
                color.rgb*=lerp(half3(1,1,1),DiceLandForestLightTint(input.world),_SceneLightingWeight);
                return half4(MixFog(color.rgb,input.fog),1);
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
            #pragma fragment DepthFrag
            #pragma multi_compile_instancing
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            half4 DepthFrag(Varyings input) : SV_Target { SampleSprite(input); return 0; }
            ENDHLSL
        }
    }
}
