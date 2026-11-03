Shader "DiceLand/Battle Forest Sun Volume"
{
    Properties
    {
        [HDR] _Color ("Sunlit scattering", Color) = (1.8,1.45,0.85,1)
        _Density ("Scattering density", Range(0,0.2)) = 0.035
        _BeamWidth ("Canopy ribbon width", Range(0.4,3)) = 1.35
        _Anisotropy ("Forward scattering", Range(0,0.8)) = 0.45
        [Toggle(_VOLUME_DEPTH_AVERAGE)] _DepthAverage ("Average canopy through lit depth", Float) = 0
        _ShaftSoftness ("Light ribbon softness", Range(0,1)) = 0.4
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent-20" "RenderType"="Transparent" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            // Back faces cover the volume once, including when the camera is inside it.
            Cull Front
            ZTest Always
            ZWrite Off
            Blend One SrcAlpha, Zero One
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma shader_feature_local _VOLUME_DEPTH_AVERAGE
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Assets/Assets/Shaders/BattleForestLighting.hlsl"
            #include "Assets/Assets/Shaders/BattleForestSunVolume.hlsl"
            CBUFFER_START(UnityPerMaterial)
            half4 _Color;
            float _Density, _BeamWidth, _Anisotropy, _DepthAverage, _ShaftSoftness;
            CBUFFER_END
            struct Attributes { float3 positionOS : POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; float3 world : TEXCOORD0; UNITY_VERTEX_OUTPUT_STEREO };
            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.world = TransformObjectToWorld(input.positionOS);
                output.positionCS = TransformWorldToHClip(output.world);
                return output;
            }
            float IntegrateOpening(float origin, float rate, float begin, float end, float centre, float width)
            {
                float result = ForestSunGaussianIntegral(origin, rate, begin, end, centre, width * 2.1) * 0.11;
                result += ForestSunGaussianIntegral(origin, rate, begin, end, centre - width * 0.52, width * 0.20) * 0.7;
                result += ForestSunGaussianIntegral(origin, rate, begin, end, centre + width * 0.03, width * 0.27);
                result += ForestSunGaussianIntegral(origin, rate, begin, end, centre + width * 0.62, width * 0.14) * 0.5;
                return result;
            }
            float CanopyGate(float3 origin, float3 direction, float start, float finish,
                float shaftOrigin, float shaftRate, float centre)
            {
                float closest = abs(shaftRate) > 0.0001
                    ? clamp((centre - shaftOrigin) / shaftRate, start, finish) : (start + finish) * 0.5;
                float3 world = origin + direction * closest;
                float3 local = TransformWorldToObject(world) + 0.5;
                float fade = smoothstep(0.0, 0.04, local.y) * (1.0 - smoothstep(0.7, 1.0, local.y));
                fade *= smoothstep(0.0, 0.07, local.z) * (1.0 - smoothstep(0.84, 1.0, local.z));
                float leaves = DiceLandForestCanopyTransmission(DiceLandForestProjectedGround(world), DiceLandForestCanopyBreeze(), world.y);
                return fade * lerp(1.0, leaves, saturate(_DiceLandForestOpenings.z));
            }
            float3 OpeningMoments(float origin, float rate, float begin, float end, float centre, float width)
            {
                float softness = width * _ShaftSoftness;
                float3 moments = ForestSunSoftGaussianMoments(origin, rate, begin, end, centre, width * 2.1, softness) * 0.11;
                moments += ForestSunSoftGaussianMoments(origin, rate, begin, end, centre - width * 0.52, width * 0.20, softness) * 0.7;
                moments += ForestSunSoftGaussianMoments(origin, rate, begin, end, centre + width * 0.03, width * 0.27, softness);
                moments += ForestSunSoftGaussianMoments(origin, rate, begin, end, centre + width * 0.62, width * 0.14, softness) * 0.5;
                return moments;
            }
            float CanopyAt(float3 world)
            {
                float3 local = TransformWorldToObject(world) + 0.5;
                float fade = smoothstep(0.0, 0.04, local.y) * (1.0 - smoothstep(0.7, 1.0, local.y));
                fade *= smoothstep(0.0, 0.07, local.z) * (1.0 - smoothstep(0.84, 1.0, local.z));
                float leaves = DiceLandForestCanopyTransmission(DiceLandForestProjectedGround(world), DiceLandForestCanopyBreeze(), world.y);
                return fade * lerp(1.0, leaves, saturate(_DiceLandForestOpenings.z));
            }
            float DepthAveragedCanopy(float3 origin, float3 direction, float start, float finish, float3 moments)
            {
                if (moments.x <= 0.00001)
                    return 0;
                float mean = clamp(moments.y / moments.x, -1.0, 1.0);
                float deviation = sqrt(max(0.0, moments.z / moments.x - mean * mean));
                float nearSample = max(-1.0, mean - deviation), farSample = min(1.0, mean + deviation);
                float farWeight = saturate((mean - nearSample) / max(0.00001, farSample - nearSample));
                float middle = (start + finish) * 0.5, halfLength = (finish - start) * 0.5;
                float nearGate = CanopyAt(origin + direction * (middle + halfLength * nearSample));
                float farGate = CanopyAt(origin + direction * (middle + halfLength * farSample));
                return lerp(nearGate, farGate, farWeight);
            }
            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                if (_DiceLandForestLighting <= 0.001 || _Density <= 0.0)
                    return half4(0, 0, 0, 1);
                float2 uv = input.positionCS.xy / _ScaledScreenParams.xy;
                float rawDepth = SampleSceneDepth(uv);
                float eyeDepth = unity_OrthoParams.w > 0.5
                    ? LinearDepthToEyeDepth(rawDepth) : LinearEyeDepth(rawDepth, _ZBufferParams);
                float3 origin = GetCameraPositionWS();
                float3 direction = normalize(input.world - origin);
                if (unity_OrthoParams.w > 0.5)
                {
                    direction = TransformViewToWorldDir(float3(0, 0, -1), true);
                    origin = input.world - direction * dot(input.world - origin, direction);
                }
                float2 interval = ForestSunBoxInterval(TransformWorldToObject(origin),
                    mul((float3x3)unity_WorldToObject, direction));
                float start = max(0.0, interval.x);
                float cosine = max(0.001, dot(direction, TransformViewToWorldDir(float3(0, 0, -1), true)));
                float finish = min(interval.y, eyeDepth / cosine);
                if (finish <= start)
                    return half4(0, 0, 0, 1);
                Light sun = GetMainLight();
                sun.direction = DiceLandForestSunDirection();
                sun.color = DiceLandForestSunColor();
                float slope = (sun.direction.x + sun.direction.z * 0.42) / max(0.2, sun.direction.y);
                float shaftOrigin = origin.x + origin.z * 0.42 - slope * origin.y;
                float shaftRate = direction.x + direction.z * 0.42 - slope * direction.y;
                float first = _DiceLandForestOpenings.x + DiceLandForestSunSweep(_DiceLandForestOpenings.x);
                float second = _DiceLandForestOpenings.y + DiceLandForestSunSweep(_DiceLandForestOpenings.y);
                float amount = 0, other = 0;
                #if defined(_VOLUME_DEPTH_AVERAGE)
                    float3 moments = OpeningMoments(shaftOrigin, shaftRate, start, finish, first, _BeamWidth);
                    amount = moments.x * DepthAveragedCanopy(origin, direction, start, finish, moments);
                    float3 otherMoments = OpeningMoments(shaftOrigin, shaftRate, start, finish, second, _BeamWidth * 0.72);
                    other = otherMoments.x * DepthAveragedCanopy(origin, direction, start, finish, otherMoments);
                #else
                    amount = IntegrateOpening(shaftOrigin, shaftRate, start, finish, first, _BeamWidth);
                    amount *= CanopyGate(origin, direction, start, finish, shaftOrigin, shaftRate, first);
                    other = IntegrateOpening(shaftOrigin, shaftRate, start, finish, second, _BeamWidth * 0.72);
                    other *= CanopyGate(origin, direction, start, finish, shaftOrigin, shaftRate, second);
                #endif
                float transmission = exp(-max(0.0, amount + other) * _Density);
                float g = _Anisotropy;
                float phase = (1.0 - g * g) / pow(max(0.1, 1.0 + g * g - 2.0 * g * dot(direction, sun.direction)), 1.5);
                half3 scattering = (1.0 - transmission) * _Color.rgb * sun.color
                    * min(4.0, phase) * saturate(_DiceLandForestLighting);
                return half4(scattering, transmission);
            }
            ENDHLSL
        }
    }
}
