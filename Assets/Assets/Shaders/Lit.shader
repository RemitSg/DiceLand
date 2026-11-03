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
        [HideInInspector] _ForestSpriteBounds ("Forest illustration bounds", Vector) = (0,0,0,0)
        [HideInInspector] _ForestSurfaceWeight ("Forest surface shaping", Range(0,1)) = 0
        [HideInInspector] _ForestIndirectStrength ("Forest indirect fill", Range(0,2)) = 1
        [HideInInspector] _ForestPigmentStrength ("Forest pigment response", Range(0,1)) = 1
        [HideInInspector] _ForestHighlightStrength ("Forest highlight shoulder", Range(0,1)) = 1
        [HideInInspector] _ForestFormMap ("Forest silhouette form", 2D) = "bump" {}
        [HideInInspector] _ForestFormParams ("Form weight and sprite mirroring", Vector) = (0,1,1,0)
        [HideInInspector] _ForestMaterialMap ("Forest skin, metal and roughness", 2D) = "black" {}
        [HideInInspector] _ForestMaterialWeight ("Forest authored material response", Range(0,1)) = 0
        [HideInInspector] _ForestEmissionWeight ("Forest emissive material regions", Range(0,1)) = 0
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
            Cull Off // Match two-sided, flipped sprite geometry in the forward pass.
            
            HLSLPROGRAM
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            #pragma multi_compile_instancing
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA

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
            float4 _ForestSpriteBounds;
            float _ForestSurfaceWeight;
            float _ForestIndirectStrength;
            float _ForestPigmentStrength;
            float _ForestHighlightStrength;
            float4 _ForestFormParams;
            float _ForestMaterialWeight, _ForestEmissionWeight;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_AlphaTex); SAMPLER(sampler_AlphaTex);

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
                #if ETC1_EXTERNAL_ALPHA
                baseColor.a = lerp(baseColor.a, SAMPLE_TEXTURE2D(_AlphaTex, sampler_AlphaTex, input.uv).r * input.color.a, _EnableExternalAlpha);
                #endif
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
            Cull Off // Mirrored sprites must still cast alpha-clipped shadows.

            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #pragma multi_compile_instancing
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
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
            float4 _ForestSpriteBounds;
            float _ForestSurfaceWeight;
            float _ForestIndirectStrength;
            float _ForestPigmentStrength;
            float _ForestHighlightStrength;
            float4 _ForestFormParams;
            float _ForestMaterialWeight, _ForestEmissionWeight;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_AlphaTex); SAMPLER(sampler_AlphaTex);

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

                // Cull Off renders either side of a flat sprite into the shadow map.
                // Face its bias normal toward the light; a back-facing normal would
                // move the caster toward the receiver and produce striped self-shadow.
                normalWS = dot(normalWS, lightDirectionWS) < 0.0 ? -normalWS : normalWS;
                float3 biasedPositionWS = ApplyShadowBias(positionWS, normalWS, lightDirectionWS);

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
                #if ETC1_EXTERNAL_ALPHA
                baseColor.a = lerp(baseColor.a, SAMPLE_TEXTURE2D(_AlphaTex, sampler_AlphaTex, input.uv).r * input.color.a, _EnableExternalAlpha);
                #endif
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
            #pragma multi_compile_fog
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
            #include "Assets/Assets/Shaders/BattleForestLighting.hlsl"
            #include "Assets/Assets/Shaders/BattleForestSpriteLighting.hlsl"

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
            float4 _ForestSpriteBounds;
            float _ForestSurfaceWeight;
            float _ForestIndirectStrength;
            float _ForestPigmentStrength;
            float _ForestHighlightStrength;
            float4 _ForestFormParams;
            float _ForestMaterialWeight, _ForestEmissionWeight;
            CBUFFER_END

            TEXTURE2D(_MainTex);       SAMPLER(sampler_MainTex);
            TEXTURE2D(_AlphaTex);      SAMPLER(sampler_AlphaTex);
            TEXTURE2D(_NormalMap);     SAMPLER(sampler_NormalMap);
            TEXTURE2D(_ForestFormMap); SAMPLER(sampler_ForestFormMap);
            TEXTURE2D(_ForestMaterialMap); SAMPLER(sampler_ForestMaterialMap);

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
                half fogFactor         : TEXCOORD5;
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
                o.fogFactor = ComputeFogFactor(o.positionCS.z);

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
                                     float3 viewDirWS, float4 shadowCoord, float surfaceWeight, half3 material, float materialWeight)
            {
                float pigmentWeight = saturate(surfaceWeight * _ForestPigmentStrength);
                if (pigmentWeight > 0.001)
                    albedo = lerp(albedo, DiceLandForestPigmentAlbedo(albedo), pigmentWeight);
                float3 lighting = albedo * _AmbientIntensity;
                float3 totalDiffuse = float3(0, 0, 0);
                float3 totalSpecular = float3(0, 0, 0);
                
                // ✅ 使用传入的shadowCoord而不是在此处重新计算
                Light mainLight = GetMainLight(shadowCoord);
                float fallback = step(0.001, materialWeight)
                    * (1.0 - step(0.0001, dot(mainLight.color, half3(1,1,1))));
                mainLight.direction = lerp(mainLight.direction, DiceLandForestSunDirection(), fallback);
                mainLight.color = lerp(mainLight.color, DiceLandForestSunColor(), fallback);
                mainLight.distanceAttenuation = lerp(mainLight.distanceAttenuation, 1.0, fallback);
                mainLight.shadowAttenuation = lerp(mainLight.shadowAttenuation, 1.0, fallback);
                
                float3 keyDirection = lerp(mainLight.direction, DiceLandForestPaintedKey(mainLight.direction), surfaceWeight);
                float NdotL_main = dot(normalWS, keyDirection);
                float diffuseFactor = lerp(CalculateDiffuseFactor(dot(normalWS, mainLight.direction), true),
                    0.32 + 0.90 * saturate(NdotL_main), surfaceWeight);
                diffuseFactor = lerp(diffuseFactor, DiceLandForestMaterialDiffuse(material, NdotL_main), materialWeight);
                // Glossiness zero previously made pow(NdotH, 0) a constant white wash.
                // Preserve the historical material response outside this forest-only surface treatment.
                float specularPower = lerp(_Glossiness * 128.0, max(12.0, _Glossiness * 128.0), surfaceWeight);
                float specularWeight = lerp(1.0, 0.65, surfaceWeight);
                // ✅ shadowAttenuation现在会正确响应其它sprite的投影
                float3 diffuse = mainLight.color * diffuseFactor 
                                 * mainLight.shadowAttenuation 
                                 * mainLight.distanceAttenuation;
                totalDiffuse += diffuse;
                
                if (NdotL_main > 0)
                {
                    float3 halfDir = normalize(keyDirection + viewDirWS);
                    float NdotH = saturate(dot(normalWS, halfDir));
                    float specPow = pow(NdotH, specularPower) * specularWeight;
                    float3 reflected = lerp(specPow * _Specular.rgb,
                        DiceLandForestMaterialSpecular(albedo, material, NdotH), materialWeight);
                    totalSpecular += mainLight.color * reflected
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
                        float specPow = pow(NdotH, specularPower) * specularWeight;
                        float3 reflected = lerp(specPow * _Specular.rgb,
                            DiceLandForestMaterialSpecular(albedo, material, NdotH), materialWeight);
                        totalSpecular += light.color * reflected * lightAttenuation;
                    }
                }
                #endif
                
                float rim = 1.0 - saturate(dot(viewDirWS, normalWS));
                rim = pow(rim, _RimPower);
                float3 rimLight = _RimColor.rgb * rim;
                rimLight *= lerp(1.0, DiceLandForestMaterialRim(material), materialWeight);
                
                lighting += albedo * totalDiffuse + totalSpecular + rimLight;
                
                float minBrightness = lerp(0.1, 0.0, pigmentWeight);
                float currentBrightness = length(lighting);
                if (currentBrightness > 0.0 && currentBrightness < minBrightness)
                    lighting = normalize(lighting) * minBrightness;
                
                float paintedIncidence = saturate(dot(normalWS, DiceLandForestPaintedKey(mainLight.direction)));
                float formGain = lerp(0.58, 1.22, smoothstep(0.08, 0.80, paintedIncidence));
                lighting *= lerp(half3(1,1,1), DiceLandForestSurfaceTint(paintedIncidence) * formGain, surfaceWeight);
                float3 indirectFill = 0;
                if (surfaceWeight > 0.001)
                    indirectFill = DiceLandForestIndirectFill(albedo, normalWS) * (surfaceWeight * _ForestIndirectStrength);
                return lighting * DiceLandForestLightTint(positionWS)
                    + indirectFill;
            }

            float4 frag(Varyings i) : SV_Target
            {
                float4 baseColor = SampleSpriteTexture(i.uv) * i.color;
                clip(baseColor.a - _Cutoff);
                
                float4 normalMap = SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, i.uv);
                float3 normalTS = UnpackNormal(normalMap);
                
                float3x3 TBN = float3x3(i.tangentWS, i.bitangentWS, i.normalWS);
                float3 normalWS = normalize(mul(normalTS, TBN));
                float surfaceWeight = saturate(_DiceLandForestLighting * _ForestSurfaceWeight)
                    * step(0.001, min(_ForestSpriteBounds.z, _ForestSpriteBounds.w));
                if (surfaceWeight > 0.001)
                {
                    normalWS = DiceLandForestSculptedNormal(i.positionWS, _ForestSpriteBounds, surfaceWeight);
                    if (_ForestFormParams.x > 0.001)
                    {
                        // RGB stores a linear positive-Z silhouette/local-form normal.
                        // Sprite mirroring is separate from Transform's rotation/negative scale.
                        float3 form = SAMPLE_TEXTURE2D(_ForestFormMap, sampler_ForestFormMap, i.uv).rgb * 2.0 - 1.0;
                        form = normalize(float3(form.xy * _ForestFormParams.yz, -max(0.1, form.z)));
                        float3 formWS = TransformObjectToWorldNormal(form);
                        // Two-sided illustration quads may also face left through a 180-degree Y turn.
                        formWS.z = -max(0.1, abs(formWS.z));
                        formWS = normalize(formWS);
                        normalWS = normalize(lerp(normalWS, formWS, saturate(_ForestFormParams.x)));
                    }
                }
                
                float3 viewDirWS = normalize(_WorldSpaceCameraPos.xyz - i.positionWS);
                
                // ✅ 在fragment中重新计算shadowCoord，精度更高
                #if defined(_MAIN_LIGHT_SHADOWS_SCREEN)
                float4 shadowCoord = ComputeScreenPos(i.positionCS);
                #else
                float4 shadowCoord = TransformWorldToShadowCoord(i.positionWS);
                #endif
                
                float materialWeight = saturate(_ForestMaterialWeight) * surfaceWeight;
                half4 materialData = half4(0,0,1,0);
                if (materialWeight > 0.001)
                    materialData = SAMPLE_TEXTURE2D(_ForestMaterialMap, sampler_ForestMaterialMap, i.uv);
                half3 material = materialData.rgb;
                float3 litColor = CalculateLighting(baseColor.rgb, normalWS, i.positionWS,
                                                    viewDirWS, shadowCoord, surfaceWeight, material, materialWeight);
                float highlightWeight = saturate(surfaceWeight * _ForestHighlightStrength);
                if (highlightWeight > 0.001)
                    litColor = lerp(litColor, DiceLandForestHighlightShoulder(litColor), highlightWeight);
                // Only explicitly tagged material maps use A as emission. Older maps keep
                // their silhouette alpha and never inherit this response.
                float emission = materialWeight * saturate(_ForestEmissionWeight) * materialData.a;
                if (emission > 0.001)
                {
                    float breath = 0.90 + 0.10 * sin(_Time.y * 2.1 + dot(_ForestSpriteBounds.xy, float2(0.07,0.11)));
                    litColor += baseColor.rgb * (5.0 * emission * breath);
                }
                if (_DiceLandForestLighting > 0.001)
                {
                    // A narrow sun-facing alpha edge gives flat illustrated sprites a restrained rim.
                    float3 rimSun = lerp(GetMainLight().direction, DiceLandForestSunDirection(), materialWeight);
                    float3 sunOS = TransformWorldToObjectDir(rimSun);
                    float2 rimDirection = normalize(sunOS.xy * _Flip.xy + float2(0.0001, 0.0001));
                    float2 rimStep = max(fwidth(i.uv), float2(0.0001, 0.0001)) * 1.5;
                    float edgeAlpha = SampleSpriteTexture(i.uv + rimDirection * rimStep).a * i.color.a;
                    float rim = saturate(baseColor.a - edgeAlpha);
                    half3 edgeLight = lerp(GetMainLight().color, half3(1.20, 0.65, 0.16), surfaceWeight);
                    float edgeResponse = lerp(1.0, DiceLandForestMaterialRim(material), materialWeight);
                    litColor += edgeLight * rim * (0.55 * _DiceLandForestLighting) * edgeResponse;
                }
                litColor *= baseColor.a;
                
                return float4(MixFog(litColor, i.fogFactor), baseColor.a);
            }
            ENDHLSL
        }

        // Alpha-clipped object velocities for Tuanjie URP's camera-and-object blur.
        Pass
        {
            Name "MotionVectors"
            Tags { "LightMode"="MotionVectors" }
            ColorMask RG
            Cull Off
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex MotionVert
            #pragma fragment MotionFrag
            #pragma multi_compile_instancing
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MotionVectorsCommon.hlsl"
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
            float4 _ForestSpriteBounds;
            float _ForestSurfaceWeight;
            float _ForestIndirectStrength;
            float _ForestPigmentStrength;
            float _ForestHighlightStrength;
            float4 _ForestFormParams;
            float _ForestMaterialWeight, _ForestEmissionWeight;
            CBUFFER_END
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            TEXTURE2D(_AlphaTex); SAMPLER(sampler_AlphaTex);
            struct MotionAttributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct MotionVaryings
            {
                float4 positionCS : SV_POSITION;
                float4 currentCS : TEXCOORD0;
                float4 previousCS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                half alpha : TEXCOORD3;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            MotionVaryings MotionVert(MotionAttributes input)
            {
                MotionVaryings output=(MotionVaryings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                float4 position=input.positionOS;
                position.xy*=_Flip.xy;
                output.positionCS=TransformObjectToHClip(position.xyz);
                output.currentCS=mul(_NonJitteredViewProjMatrix,mul(UNITY_MATRIX_M,position));
                output.previousCS=mul(_PrevViewProjMatrix,mul(UNITY_PREV_MATRIX_M,position));
                output.uv=TRANSFORM_TEX(input.uv,_MainTex);
                output.alpha=input.color.a*_Color.a*_RendererColor.a;
                return output;
            }
            half4 MotionFrag(MotionVaryings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                half alpha=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,input.uv).a;
                #if ETC1_EXTERNAL_ALPHA
                alpha=lerp(alpha,SAMPLE_TEXTURE2D(_AlphaTex,sampler_AlphaTex,input.uv).r,_EnableExternalAlpha);
                #endif
                clip(alpha*input.alpha-_Cutoff);
                return half4(CalcNdcMotionVectorFromCsPositions(input.currentCS,input.previousCS),0,0);
            }
            ENDHLSL
        }
    }

    Fallback "Universal Render Pipeline/2D/Sprite-Unlit-Default"
}
