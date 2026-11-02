Shader "DiceLand/Battle Forest Backdrop"
{
    Properties
    {
        _BaseMap ("Painted distant landscape", 2D) = "white" {}
        _Tint ("Tint", Color) = (0.82,0.9,0.87,1)
        _AirColor ("Distant air and strength", Color) = (0.66,0.77,0.80,0)
        _Opening ("Canopy opening (UV centre and size)", Vector) = (0.52,0.57,0.82,0.88)
        [HDR] _SunGlow ("Backlit opening and strength", Color) = (1.25,1.05,0.71,0)
        _WaterfallA ("Waterfall A (UV minimum and size)", Vector) = (0.393,0.369,0.053,0.128)
        _WaterfallB ("Waterfall B (UV minimum and size)", Vector) = (0.598,0.298,0.037,0.115)
        _WaterfallC ("Waterfall C (UV minimum and size)", Vector) = (0.683,0.286,0.041,0.128)
        _WaterFlow ("Painted water motion", Range(0,0.3)) = 0
        _WaterFlowSpeed ("Water flow speed", Range(0.1,2)) = 0.8
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Cull Off
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        CBUFFER_START(UnityPerMaterial)
        float4 _BaseMap_ST;
        half4 _Tint, _AirColor, _SunGlow;
        float4 _Opening, _WaterfallA, _WaterfallB, _WaterfallC;
        float _WaterFlow, _WaterFlowSpeed;
        CBUFFER_END
        TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
        struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
        struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };
        Varyings Vert(Attributes input)
        {
            Varyings output;
            output.positionCS=TransformObjectToHClip(input.positionOS.xyz);
            output.uv=TRANSFORM_TEX(input.uv,_BaseMap);
            return output;
        }
        ENDHLSL
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            float WaterfallMask(float2 uv, float4 region)
            {
                float2 p = (uv - region.xy) / max(region.zw, float2(0.001, 0.001));
                float2 edge = smoothstep(0.0, 0.12, p) * (1.0 - smoothstep(0.82, 1.0, p));
                return edge.x * edge.y;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                // Keep the authored 0..1 region intact. Beyond it, reflect the distant painting
                // continuously rather than clamping a border pixel or exposing the camera clear.
                float2 uv = 1.0 - abs(frac(input.uv * 0.5) * 2.0 - 1.0);
                half3 painted = SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,uv).rgb;

                // Crossfade two sliding copies of the existing painted water. Feathered regions
                // and the cool-water mask keep the cliff silhouettes and the rest of the image still.
                float water = max(WaterfallMask(uv, _WaterfallA),
                    max(WaterfallMask(uv, _WaterfallB), WaterfallMask(uv, _WaterfallC)));
                water *= smoothstep(0.07,0.34,painted.b) * (1.0 - smoothstep(0.02,0.12,painted.r-painted.b));
                float phase = frac(_Time.y * (0.22 * _WaterFlowSpeed));
                float secondPhase = frac(phase + 0.5);
                half3 firstFlow = SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,uv + float2(0,phase * 0.018)).rgb;
                half3 secondFlow = SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,uv + float2(0,secondPhase * 0.018)).rgb;
                half3 flowing = lerp(firstFlow,secondFlow,abs(phase * 2.0 - 1.0));
                painted = lerp(painted,flowing,water * saturate(_WaterFlow * 3.0));
                half3 color = painted * _Tint.rgb;

                // Lift only the distant opening; the painted outer silhouettes keep their contrast.
                float2 openingUV = (uv - _Opening.xy) / max(_Opening.zw * 0.5, float2(0.01, 0.01));
                float opening = saturate(1.0 - dot(openingUV, openingUV));
                opening *= opening;
                half luminance = dot(painted, half3(0.2126,0.7152,0.0722));
                float air = _AirColor.a * opening * (0.35 + 0.65 * smoothstep(0.10,0.55,luminance));
                color = lerp(color, _AirColor.rgb, saturate(air));
                color += _SunGlow.rgb * _SunGlow.a * opening * smoothstep(0.08,0.55,luminance);

                return half4(color,1);
            }
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On
            ColorMask 0
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment DepthFrag
            half4 DepthFrag(Varyings input) : SV_Target { return 0; }
            ENDHLSL
        }
    }
}
