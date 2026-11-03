Shader "DiceLand/Battle Actor Sun Shadow"
{
    Properties
    {
        _MainTex ("Sprite silhouette", 2D) = "white" {}
        _ShadowMask ("Linear silhouette mask", 2D) = "white" {}
        _AlphaTex ("External sprite alpha", 2D) = "white" {}
        _Shade ("Indirect light retained in shadow", Color) = (0.42,0.54,0.72,1)
        _Opacity ("Direct light occlusion", Range(0,1)) = 0.75
        _Softness ("Penumbra growth per metre", Range(0,0.1)) = 0.035
        _Blur ("Soft silhouette radius in texels", Range(0,12)) = 5
        _GroundHeight ("Receiving plane height", Float) = 0.06
        _Flip ("Sprite mirroring and opacity", Vector) = (1,1,1,0)
        _UVRect ("Sprite UV limits", Vector) = (0,0,1,1)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent-110" "RenderType"="Transparent" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Blend DstColor Zero, Zero One
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.0
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Assets/Assets/Shaders/BattleForestLighting.hlsl"
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            TEXTURE2D(_ShadowMask); SAMPLER(sampler_ShadowMask);
            TEXTURE2D(_AlphaTex); SAMPLER(sampler_AlphaTex);
            CBUFFER_START(UnityPerMaterial)
            float4x4 _CasterToWorld;
            float4 _MainTex_TexelSize, _AlphaTex_TexelSize, _Flip, _UVRect;
            float4 _MaskMetrics, _WorldToUV;
            half4 _Shade;
            float _Opacity, _Blur, _GroundHeight, _Softness, _UseExternalAlpha;
            CBUFFER_END
            struct Attributes { float3 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float3 world : TEXCOORD1; float height : TEXCOORD2; half fog : TEXCOORD3; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                input.positionOS.xy *= _Flip.xy;
                float3 world = mul(_CasterToWorld, float4(input.positionOS, 1)).xyz;
                output.height = max(0.0, world.y - _GroundHeight);
                float3 sun = DiceLandForestSunDirection();
                world -= sun * (world.y - _GroundHeight) / max(0.2, sun.y);
                world.y = _GroundHeight;
                output.world = world;
                output.positionCS = TransformWorldToHClip(world);
                output.fog = ComputeFogFactor(output.positionCS.z);
                output.uv = input.uv;
                return output;
            }
            half AlphaAt(float2 uv)
            {
                half inside = step(_UVRect.x, uv.x) * step(uv.x, _UVRect.z)
                    * step(_UVRect.y, uv.y) * step(uv.y, _UVRect.w);
                half alpha = _UseExternalAlpha > 0.5 ? SAMPLE_TEXTURE2D(_AlphaTex, sampler_AlphaTex, uv).r
                    : SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).a;
                return alpha * inside;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                half alpha;
                if (_MaskMetrics.w > 0.5)
                {
                    float radius = max(_WorldToUV.x * _MaskMetrics.x, _WorldToUV.y * _MaskMetrics.y)
                        * (0.02 + input.height * _Softness);
                    float lod = clamp(log2(max(1.0, radius)), 0.0, _MaskMetrics.z);
                    alpha = SAMPLE_TEXTURE2D_LOD(_ShadowMask, sampler_ShadowMask, input.uv, lod).a;
                    alpha *= step(_UVRect.x, input.uv.x) * step(input.uv.x, _UVRect.z)
                        * step(_UVRect.y, input.uv.y) * step(input.uv.y, _UVRect.w);
                }
                else
                {
                    float2 texel = _UseExternalAlpha > 0.5 ? _AlphaTex_TexelSize.xy : _MainTex_TexelSize.xy;
                    float2 radius = texel * _Blur * saturate(0.18 + input.height * 0.16);
                    alpha = AlphaAt(input.uv) * 0.2;
                    alpha += (AlphaAt(input.uv + float2(radius.x,0)) + AlphaAt(input.uv - float2(radius.x,0))
                        + AlphaAt(input.uv + float2(0,radius.y)) + AlphaAt(input.uv - float2(0,radius.y))) * 0.12;
                    alpha += (AlphaAt(input.uv + radius) + AlphaAt(input.uv - radius)
                        + AlphaAt(input.uv + radius * float2(-1,1)) + AlphaAt(input.uv + radius * float2(1,-1))) * 0.08;
                }
                // The contact ellipse owns the foot's immediate ambient occlusion.
                alpha *= smoothstep(0.02, 0.28, input.height) * saturate(_Flip.z);
                float lane = smoothstep(-7.0, -6.6, input.world.z) * (1.0 - smoothstep(8.1, 8.5, input.world.z));
                lane *= 1.0 - smoothstep(35.5, 36.0, abs(input.world.x));
                float light = DiceLandForestSunVisibility(input.world) * saturate(_DiceLandForestLighting);
                half strength = saturate(alpha * _Opacity * light * lane);
                half3 shade = lerp(half3(1,1,1), _Shade.rgb, strength);
                return half4(MixFogColor(shade, half3(1,1,1), input.fog), 1);
            }
            ENDHLSL
        }
    }
}
