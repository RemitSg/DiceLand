Shader "DiceLand/Battle Forest Foliage"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Cutoff ("Alpha cutoff", Range(0,1)) = 0.4
        _SwayAmplitude ("Canopy breeze (metres)", Range(0,0.2)) = 0.045
        _DefocusTexels ("Near foliage softness", Range(0,16)) = 0
        [Toggle] _SoftSilhouette ("Defocused transparent silhouette", Float) = 0
        [HideInInspector] _SrcBlend ("Source blend", Float) = 1
        [HideInInspector] _DstBlend ("Destination blend", Float) = 0
        [HideInInspector] _ZWrite ("Depth write", Float) = 1
        _HighlightGain ("Sunlit canopy highlight gain", Range(1,2)) = 1
        _PaintedFill ("Painted shadow color and strength", Color) = (0.36,0.40,0.32,0)
        _SceneLightingWeight ("World canopy lighting", Range(0,1)) = 0
        [Toggle(_CANOPY_LIGHT_RESPONSE)] _CanopyResponse ("Authored canopy light response", Float) = 0
        _CanopyResponseMap ("Canopy normals, leaf regions and coverage", 2D) = "black" {}
        _ResponseWeight ("Canopy response strength", Range(0,1)) = 0.75
        _BarkLightGain ("Broad bark sunlight", Range(0,2)) = 0.8
        _LeafTransmission ("Thin leaf transmission", Range(0,2)) = 0.7
        [HideInInspector] _RendererColor ("Renderer color", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable external alpha", Float) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="TransparentCutout" "Queue"="AlphaTest" "CanUseSpriteAtlas"="True" }
        Cull Off
        Blend [_SrcBlend] [_DstBlend]
        ZWrite [_ZWrite]
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Assets/Assets/Shaders/BattleForestLighting.hlsl"
        #include "Assets/Assets/Shaders/BattleForestCanopyLight.hlsl"
        CBUFFER_START(UnityPerMaterial)
        float4 _MainTex_ST, _MainTex_TexelSize;
        half4 _Color, _PaintedFill;
        float _Cutoff, _EnableExternalAlpha, _SwayAmplitude, _DefocusTexels, _HighlightGain, _SceneLightingWeight;
        float _SoftSilhouette, _SrcBlend, _DstBlend, _ZWrite;
        float _CanopyResponse, _ResponseWeight, _BarkLightGain, _LeafTransmission;
        CBUFFER_END
        TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
        TEXTURE2D(_AlphaTex); SAMPLER(sampler_AlphaTex);
        TEXTURE2D(_CanopyResponseMap); SAMPLER(sampler_CanopyResponseMap);
        struct Attributes { float3 positionOS : POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
        struct Varyings
        {
            float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; half4 color : COLOR;
            half fog : TEXCOORD1; float3 world : TEXCOORD2;
            #if defined(_CANOPY_LIGHT_RESPONSE)
                float3 normalX : TEXCOORD3; float3 normalY : TEXCOORD4; float3 normalZ : TEXCOORD5;
            #endif
            UNITY_VERTEX_OUTPUT_STEREO
        };
        Varyings Vert(Attributes input)
        {
            Varyings output=(Varyings)0;
            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
            #ifdef UNITY_INSTANCING_ENABLED
                float2 spriteFlip=unity_SpriteFlip;
            #else
                float2 spriteFlip=unity_SpriteProps.xy;
            #endif
            input.positionOS=UnityFlipSprite(input.positionOS,spriteFlip);
            float3 positionWS=TransformObjectToWorld(input.positionOS);
            // Roots stay planted; only the upper canopy moves, identically in color and depth passes.
            float breeze=sin(_Time.y*0.63+positionWS.x*0.18)+0.35*sin(_Time.y*1.07+positionWS.z*0.23);
            positionWS.x+=breeze*_SwayAmplitude*pow(saturate(input.uv.y),3.0);
            output.positionCS=TransformWorldToHClip(positionWS);
            output.world=positionWS;
            output.uv=TRANSFORM_TEX(input.uv,_MainTex);
            output.color=input.color*_Color*unity_SpriteColor;
            output.fog=ComputeFogFactor(output.positionCS.z);
            #if defined(_CANOPY_LIGHT_RESPONSE)
                // The fragment derives actual UV handedness, including native sprite batching flips.
                output.normalX=TransformObjectToWorldNormal(float3(1,0,0),false);
                output.normalY=TransformObjectToWorldNormal(float3(0,1,0),false);
                output.normalZ=TransformObjectToWorldNormal(float3(0,0,1),false);
            #endif
            return output;
        }
        half4 SampleSoftSprite(float2 uv, float bias)
        {
            half4 color=SAMPLE_TEXTURE2D_BIAS(_MainTex,sampler_MainTex,uv,bias);
            #if ETC1_EXTERNAL_ALPHA
                color.a=lerp(color.a,SAMPLE_TEXTURE2D_BIAS(_AlphaTex,sampler_AlphaTex,uv,bias).r,_EnableExternalAlpha);
            #endif
            // Filter in associated color space so sparse leaves do not acquire dark fringes.
            color.rgb*=color.a;
            return color;
        }
        half4 SampleSprite(Varyings input)
        {
            half4 color=0;
            if (_SoftSilhouette > 0.5)
            {
                // The authored radius is measured at 1080p, retaining the same apparent softness.
                float radius=max(0.0,_DefocusTexels)*_ScaledScreenParams.y/1080.0;
                float2 offset=fwidth(input.uv)*radius;
                // Mip prefiltering fills the gaps between the five broad lens samples.
                // Derivatives use the SpriteRenderer's actual texture rather than a white fallback.
                float bias=max(0.0,log2(max(1.0,radius))-0.5);
                half4 soft=SampleSoftSprite(input.uv,bias)*0.36;
                soft+=SampleSoftSprite(input.uv+float2(offset.x,0),bias)*0.16;
                soft+=SampleSoftSprite(input.uv-float2(offset.x,0),bias)*0.16;
                soft+=SampleSoftSprite(input.uv+float2(0,offset.y),bias)*0.16;
                soft+=SampleSoftSprite(input.uv-float2(0,offset.y),bias)*0.16;
                soft.rgb/=max(0.0001,soft.a);
                color=soft;
            }
            else
            {
                color=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,input.uv);
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
            }
            color*=input.color;
            clip(color.a-(_SoftSilhouette > 0.5 ? 0.002 : _Cutoff));
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
            #pragma shader_feature_local _CANOPY_LIGHT_RESPONSE
            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                #if defined(_CANOPY_LIGHT_RESPONSE)
                    // Compute derivatives before alpha clipping. SpriteRenderer may mirror its
                    // streamed vertices/UVs without exposing that flip in unity_SpriteProps.
                    float2 uvX=ddx(input.uv),uvY=ddy(input.uv);
                    float3 worldX=ddx(input.world),worldY=ddy(input.world);
                    float orientation=uvX.x*uvY.y-uvX.y*uvY.x < 0 ? -1.0 : 1.0;
                    float3 tangent=(worldX*uvY.y-worldY*uvX.y)*orientation;
                    float3 bitangent=(worldY*uvX.x-worldX*uvY.x)*orientation;
                    float2 frameFlip=float2(dot(tangent,input.normalX)<0 ? -1.0 : 1.0,
                        dot(bitangent,input.normalY)<0 ? -1.0 : 1.0);
                #endif
                half4 color=SampleSprite(input);
                half3 pigment=color.rgb;
                half highlight=smoothstep(0.36,0.82,dot(color.rgb,half3(0.2126,0.7152,0.0722)));
                color.rgb*=lerp(1.0,_HighlightGain,highlight);
                half shade=1.0-smoothstep(0.04,0.40,dot(color.rgb,half3(0.2126,0.7152,0.0722)));
                color.rgb+=_PaintedFill.rgb*_PaintedFill.a*shade*input.color.rgb;
                color.rgb*=lerp(half3(1,1,1),DiceLandForestLightTint(input.world),_SceneLightingWeight);
                #if defined(_CANOPY_LIGHT_RESPONSE)
                    half4 response=SAMPLE_TEXTURE2D(_CanopyResponseMap,sampler_CanopyResponseMap,input.uv);
                    float2 xy=response.rg*2.0-1.0;
                    // An unassigned response map falls back to black, which is not a valid
                    // normal; rejecting it prevents a rectangular sunlight patch.
                    response.a*=1.0-step(1.02,dot(xy,xy));
                    float3 normalOS=float3(xy,-sqrt(max(0.08,1.0-dot(xy,xy))));
                    float3 normalWS=SafeNormalize(input.normalX*(normalOS.x*frameFlip.x)
                        +input.normalY*(normalOS.y*frameFlip.y)+input.normalZ*normalOS.z);
                    color.rgb+=ForestCanopyTransport(pigment,response,normalWS,input.world,
                        _ResponseWeight,_BarkLightGain,_LeafTransmission);
                #endif
                return half4(MixFog(color.rgb,input.fog),_SoftSilhouette > 0.5 ? color.a : 1);
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
            #pragma shader_feature_local _CANOPY_LIGHT_RESPONSE
            half4 DepthFrag(Varyings input) : SV_Target
            {
                clip(0.5-_SoftSilhouette);
                SampleSprite(input);
                return 0;
            }
            ENDHLSL
        }
    }
}
