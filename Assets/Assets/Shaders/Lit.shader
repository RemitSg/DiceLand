Shader "Assets/Shaders/Lit"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
        _Cutoff ("Alpha cutoff", Range(0,1)) = 0.5
        _NormalMap ("Normal Map", 2D) = "bump" {}
        _Specular ("Specular", Color) = (0.5, 0.5, 0.5, 1)
        _Glossiness ("Glossiness", Range(0, 1)) = 0.5
        _RimColor ("Rim Color", Color) = (1, 1, 1, 1)
        _RimPower ("Rim Power", Range(0.5, 8.0)) = 3.0
        _AmbientIntensity ("Ambient Intensity", Range(0, 1)) = 0.3
        _BackLightIntensity ("Back Light Intensity", Range(0, 1)) = 0.3
        _WrapAroundFactor ("Wrap Around Factor", Range(0, 1)) = 0.5
    }

    SubShader
    {
        Tags
        {
            "Queue"="AlphaTest"
            "IgnoreProjector"="True"
            "RenderType"="TransparentCutout"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
            "RenderPipeline"="UniversalPipeline"
        }

        // ✅ 移除SubShader级别的 Lighting Off，避免影响所有Pass
        Cull Off
        ZWrite On

        // ─────────────────────────────────────────
        // Pass 1: DepthOnly（修复CBUFFER重复声明）
        // ─────────────────────────────────────────
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back  // ✅ 改为Back！避免背面污染深度缓冲
            
            HLSLPROGRAM
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // ✅ 只在CBUFFER内声明，不重复
            CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            float4 _Color;
            float4 _RendererColor;
            float4 _Flip;
            float _EnableExternalAlpha;
            float _Cutoff;
            float4 _Specular;
            float _Glossiness;
            float4 _RimColor;
            float _RimPower;
            float _AmbientIntensity;
            float _BackLightIntensity;
            float _WrapAroundFactor;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
                float4 color        : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float2 uv           : TEXCOORD0;
                float4 color        : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings DepthOnlyVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                
                float4 vertex = input.positionOS;
                vertex.xy *= _Flip.xy;
                
                output.positionCS = TransformObjectToHClip(vertex.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.color = input.color * _Color * _RendererColor;
                return output;
            }

            half4 DepthOnlyFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                
                float4 baseColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * input.color;
                clip(baseColor.a - _Cutoff);
                return 0;
            }
            ENDHLSL
        }

        // ─────────────────────────────────────────
        // Pass 2: ShadowCaster（修复法线偏移 + alpha裁剪）
        // ─────────────────────────────────────────
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back  // ✅ 改为Back！避免背面写入shadowmap产生错误阴影

            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #pragma multi_compile_instancing
            // ✅ URP 12+ 需要这个来支持点光源阴影
            #pragma multi_compile _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            // ✅ URP 12+ 使用 _LightDirection 和 _LightPosition
            float3 _LightDirection;
            float3 _LightPosition;

            CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            float4 _Color;
            float4 _RendererColor;
            float4 _Flip;
            float _EnableExternalAlpha;
            float _Cutoff;
            float4 _Specular;
            float _Glossiness;
            float4 _RimColor;
            float _RimPower;
            float _AmbientIntensity;
            float _BackLightIntensity;
            float _WrapAroundFactor;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float2 uv           : TEXCOORD0;
                float4 color        : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float2 uv           : TEXCOORD0;
                float4 color        : COLOR;
                float4 positionCS   : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            float4 GetShadowPositionHClip(Attributes input)
            {
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

                // ✅ 兼容平行光和点光源的阴影偏移
                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                float3 lightDirectionWS = normalize(_LightPosition - positionWS);
                #else
                float3 lightDirectionWS = _LightDirection;
                #endif

                // ✅ 先应用标准bias
                float3 biasedPositionWS = ApplyShadowBias(positionWS, normalWS, lightDirectionWS);
                
                // ✅ 关键修复：对2D sprite额外沿光线方向推移，避免自阴影
                // 将投影位置沿光线方向额外偏移，让shadowmap记录的深度比实际偏深
                biasedPositionWS += lightDirectionWS * 0.05; // 可调节，0.01~0.1
                
                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                biasedPositionWS -= lightDirectionWS * 0.05;
                #else
                biasedPositionWS -= lightDirectionWS * 0.04;
                #endif

                float4 positionCS = TransformWorldToHClip(biasedPositionWS);

                #if UNITY_REVERSED_Z
                positionCS.z = min(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #else
                positionCS.z = max(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #endif

                return positionCS;
            }

            Varyings ShadowPassVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                // ✅ 应用flip
                input.positionOS.xy *= _Flip.xy;

                output.positionCS = GetShadowPositionHClip(input);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.color = input.color * _Color * _RendererColor;
                return output;
            }

            half4 ShadowPassFragment(Varyings input) : SV_TARGET
            {
                UNITY_SETUP_INSTANCE_ID(input);
                
                float4 baseColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * input.color;
                // ✅ alpha裁剪确保阴影形状与sprite轮廓一致
                clip(baseColor.a - _Cutoff);
                return 0;
            }
            ENDHLSL
        }

        // ─────────────────────────────────────────
        // Pass 3: UniversalForward（修复阴影接收）
        // ─────────────────────────────────────────
        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite On
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #pragma multi_compile_instancing
            
            // ✅ 阴影接收所需的所有keywords
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            // ✅ 用于在sprite之间正确计算深度
            #pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            float4 _Color;
            float4 _RendererColor;
            float4 _Flip;
            float _EnableExternalAlpha;
            float _Cutoff;
            float4 _Specular;
            float _Glossiness;
            float4 _RimColor;
            float _RimPower;
            float _AmbientIntensity;
            float _BackLightIntensity;
            float _WrapAroundFactor;
            CBUFFER_END

            TEXTURE2D(_MainTex);       SAMPLER(sampler_MainTex);
            TEXTURE2D(_AlphaTex);      SAMPLER(sampler_AlphaTex);
            TEXTURE2D(_NormalMap);     SAMPLER(sampler_NormalMap);

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float4 color        : COLOR;
                float2 uv           : TEXCOORD0;
                float3 normalOS     : NORMAL;
                float4 tangentOS    : TANGENT;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS      : SV_POSITION;
                float4 color           : COLOR;
                float2 uv              : TEXCOORD0;
                float3 positionWS      : TEXCOORD1;
                float3 normalWS        : TEXCOORD2;
                float3 tangentWS       : TEXCOORD3;
                float3 bitangentWS     : TEXCOORD4;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes v)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                float4 vertex = v.positionOS;
                vertex.xy *= _Flip.xy;
                
                o.positionCS = TransformObjectToHClip(vertex.xyz);
                o.positionWS = TransformObjectToWorld(vertex.xyz);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color * _Color * _RendererColor;
                
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.tangentWS = TransformObjectToWorldDir(v.tangentOS.xyz);
                o.bitangentWS = cross(o.normalWS, o.tangentWS) * v.tangentOS.w;

                return o;
            }

            float4 SampleSpriteTexture(float2 uv)
            {
                float4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                #if ETC1_EXTERNAL_ALPHA
                if (_EnableExternalAlpha > 0.5)
                    color.a = SAMPLE_TEXTURE2D(_AlphaTex, sampler_AlphaTex, uv).r;
                #endif
                return color;
            }

            float CalculateDiffuseFactor(float NdotL, bool isDirectionalLight)
            {
                if (isDirectionalLight)
                {
                    return abs(NdotL) * 0.5 + 0.5;
                }
                else
                {
                    if (NdotL >= 0)
                        return saturate(NdotL);
                    else
                    {
                        float diffuseFactor = max(0, (NdotL + _WrapAroundFactor) / (1.0 + _WrapAroundFactor));
                        return diffuseFactor * _BackLightIntensity;
                    }
                }
            }
            
            float3 CalculateLighting(float3 albedo, float3 normalWS, float3 positionWS, 
                                     float3 viewDirWS, float4 shadowCoord)
            {
                float3 lighting = albedo * _AmbientIntensity;
                float3 totalDiffuse = float3(0, 0, 0);
                float3 totalSpecular = float3(0, 0, 0);
                
                // ✅ 使用传入的shadowCoord而不是在此处重新计算
                Light mainLight = GetMainLight(shadowCoord);
                
                float NdotL_main = dot(normalWS, mainLight.direction);
                float diffuseFactor = CalculateDiffuseFactor(NdotL_main, true);
                // ✅ shadowAttenuation现在会正确响应其它sprite的投影
                float3 diffuse = mainLight.color * diffuseFactor 
                                 * mainLight.shadowAttenuation 
                                 * mainLight.distanceAttenuation;
                totalDiffuse += diffuse;
                
                if (NdotL_main > 0)
                {
                    float3 halfDir = normalize(mainLight.direction + viewDirWS);
                    float NdotH = saturate(dot(normalWS, halfDir));
                    float specPow = pow(NdotH, _Glossiness * 128.0);
                    totalSpecular += mainLight.color * specPow * _Specular.rgb 
                                     * mainLight.shadowAttenuation * mainLight.distanceAttenuation;
                }
                
                #ifdef _ADDITIONAL_LIGHTS
                uint pixelLightCount = GetAdditionalLightsCount();
                for (uint i = 0u; i < pixelLightCount; ++i)
                {
                    Light light = GetAdditionalLight(i, positionWS);
                    float lightAttenuation = light.distanceAttenuation * light.shadowAttenuation;
                    if (lightAttenuation < 0.001) continue;
                    
                    float NdotL = dot(normalWS, light.direction);
                    float df = CalculateDiffuseFactor(NdotL, false);
                    totalDiffuse += light.color * df * lightAttenuation;
                    
                    if (NdotL > 0)
                    {
                        float3 halfDir = normalize(light.direction + viewDirWS);
                        float NdotH = saturate(dot(normalWS, halfDir));
                        float specPow = pow(NdotH, _Glossiness * 128.0);
                        totalSpecular += light.color * specPow * _Specular.rgb * lightAttenuation;
                    }
                }
                #endif
                
                float rim = 1.0 - saturate(dot(viewDirWS, normalWS));
                rim = pow(rim, _RimPower);
                float3 rimLight = _RimColor.rgb * rim;
                
                lighting += albedo * totalDiffuse + totalSpecular + rimLight;
                
                float minBrightness = 0.1;
                float currentBrightness = length(lighting);
                if (currentBrightness < minBrightness)
                    lighting = normalize(lighting) * minBrightness;
                
                return lighting;
            }

            float4 frag(Varyings i) : SV_Target
            {
                float4 baseColor = SampleSpriteTexture(i.uv) * i.color;
                clip(baseColor.a - _Cutoff);
                
                float4 normalMap = SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, i.uv);
                float3 normalTS = UnpackNormal(normalMap);
                
                float3x3 TBN = float3x3(i.tangentWS, i.bitangentWS, i.normalWS);
                float3 normalWS = normalize(mul(normalTS, TBN));
                
                float3 viewDirWS = normalize(_WorldSpaceCameraPos.xyz - i.positionWS);
                
                // ✅ 在fragment中重新计算shadowCoord，精度更高
                #if defined(_MAIN_LIGHT_SHADOWS_SCREEN)
                float4 shadowCoord = ComputeScreenPos(i.positionCS);
                #else
                float4 shadowCoord = TransformWorldToShadowCoord(i.positionWS);
                #endif
                
                // ✅ 传递shadowCoord
                float3 litColor = CalculateLighting(baseColor.rgb, normalWS, i.positionWS, 
                                                    viewDirWS, shadowCoord);
                litColor *= baseColor.a;
                
                return float4(litColor, baseColor.a);
            }
            ENDHLSL
        }
    }

    Fallback "Universal Render Pipeline/2D/Sprite-Unlit-Default"
}
