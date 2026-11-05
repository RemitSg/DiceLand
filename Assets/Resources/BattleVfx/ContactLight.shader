Shader "DiceLand/Battle Contact Light"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+35" "RenderType"="Transparent" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Blend One OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.0
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float2 shape : TEXCOORD1;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float shape : TEXCOORD1;
                half4 color : COLOR;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.uv = input.uv * 2 - 1;
                output.shape = input.shape.x;
                output.color = input.color;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 p = input.uv;
                float radius = length(p);
                float mask, heat, coverage, emission;
                if (input.shape < 0.5)
                {
                    mask = exp(-dot(p,p) * 4.5) * (1 - smoothstep(0.60, 1, radius));
                    heat = exp(-dot(p,p) * 22);
                    coverage = 0.025;
                    emission = 1.65;
                }
                else if (input.shape < 1.5)
                {
                    float reach = saturate(1 - abs(p.x));
                    float edge = abs(p.y) / (0.10 + 0.90 * sqrt(reach));
                    mask = (1 - smoothstep(0.44, 1, edge)) * smoothstep(0, 0.18, reach);
                    float head = smoothstep(-0.55, 0.82, p.x);
                    mask *= lerp(0.24, 1, head);
                    heat = exp(-edge * edge * 9) * head;
                    coverage = 0.14;
                    emission = 1.38;
                }
                else if (input.shape < 2.5)
                {
                    float2 stone = float2(p.x + p.y * 0.19, p.y - p.x * 0.22);
                    float cut = max(abs(stone.x) * 1.09 + max(0, stone.y) * 0.37, abs(stone.y) * 1.19);
                    cut = max(cut, -stone.x * 0.91 - stone.y * 0.62);
                    float top = step(0.09 + stone.x * 0.28, stone.y);
                    float side = step(0.22 - stone.y * 0.69, stone.x);
                    float grain = sin(p.x * 48 + p.y * 35) * sin(p.y * 44 - p.x * 30);
                    float shade = lerp(0.81, 1.29, top) * lerp(1, 0.55, side) + grain * 0.062;
                    shade *= lerp(1, 0.62, smoothstep(0.79, 0.99, cut));
                    float alpha = (1 - smoothstep(0.92, 1, cut)) * input.color.a;
                    return half4(saturate(input.color.rgb * shade) * alpha, alpha);
                }
                else if (input.shape < 3.5)
                {
                    float taper = 1 - smoothstep(0.68, 1, abs(p.x));
                    float bevel = p.y - 0.36;
                    float highlight = exp(-bevel * bevel * 38);
                    heat = highlight * 0.68;
                    mask = (1 - smoothstep(0.60, 1, abs(p.y))) * taper * lerp(0.30, 1, highlight);
                    coverage = 0.21;
                    emission = 1.35;
                }
                else
                {
                    float cloud = sin(p.x * 9 + sin(p.y * 12)) * sin(p.y * 8 - p.x * 3);
                    mask = exp(-dot(p,p) * 2.8) * (1 - smoothstep(0.46, 1, radius)) * (0.72 + cloud * 0.28);
                    heat = 0;
                    coverage = 0.85;
                    emission = 0.9;
                }

                float alpha = mask * input.color.a;
                half3 pigment = input.shape > 3.5 ? input.color.rgb : pow(saturate(input.color.rgb), 2.6);
                float whiteMix = input.shape < 0.5 ? 0.82 : 0.26;
                half3 color = lerp(pigment, half3(1, 0.98, 0.92), heat * whiteMix);
                return half4(color * alpha * emission, alpha * coverage);
            }
            ENDHLSL
        }
    }
}
