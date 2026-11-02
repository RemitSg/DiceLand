Shader "DiceLand/Speed Button Badge"
{
    Properties
    {
        [PerRendererData] _MainTex ("Existing hexagon artwork", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _CoreLight ("Amber inner light", Range(0,2)) = 1
        _EdgeLight ("Bright gold rim", Range(0,2)) = 1
        [HideInInspector] _BadgeBounds ("Tight sprite bounds", Vector) = (-1,-1,2,2)
        [HideInInspector] _Pressed ("Pressed state", Float) = 0
        [PerRendererData] _AlphaTex ("External alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable external alpha", Float) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
            CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST, _BadgeBounds;
            half4 _Color;
            float _CoreLight, _EdgeLight, _Pressed, _EnableExternalAlpha;
            CBUFFER_END
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            TEXTURE2D(_AlphaTex); SAMPLER(sampler_AlphaTex);
            struct Attributes { float3 positionOS : POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; float2 badge : TEXCOORD1; UNITY_VERTEX_OUTPUT_STEREO };
            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.badge = (input.positionOS.xy - _BadgeBounds.xy) / max(_BadgeBounds.zw, float2(0.001,0.001)) * 2.0 - 1.0;
                #ifdef UNITY_INSTANCING_ENABLED
                    input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteFlip);
                #else
                    input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);
                #endif
                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.color = input.color * _Color * unity_SpriteColor;
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * input.color;
                #if ETC1_EXTERNAL_ALPHA
                    color.a = lerp(color.a, SAMPLE_TEXTURE2D(_AlphaTex, sampler_AlphaTex, input.uv).r * input.color.a, _EnableExternalAlpha);
                #endif
                float2 p = abs(input.badge * float2(0.8660254, 1.0));
                float hex = max(p.x, dot(p,float2(0.5,0.8660254))) / 0.8660254;
                float inner = 1.0 - smoothstep(0.65,0.82,hex);
                float rim = exp2(-abs(hex - 0.79) * 58.0);
                float catchLight = saturate(0.5 + input.badge.y * 0.4 - input.badge.x * 0.2);
                color.rgb += half3(0.22,0.12,0.018) * inner * _CoreLight * lerp(1.0,0.55,_Pressed);
                color.rgb += half3(0.9,0.58,0.18) * rim * (0.6 + catchLight * 0.5) * _EdgeLight;
                return color;
            }
            ENDHLSL
        }
    }
}
