Shader "DiceLand/Speed Button Glow"
{
    Properties
    {
        [HDR] _GlowColor ("Gold emissive edge", Color) = (1.5,0.65,0.12,1)
        _Intensity ("Glow intensity", Range(0,2)) = 1
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent-1" "RenderType"="Transparent" }
        Cull Off
        ZWrite Off
        ZTest Always
        Blend One One
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            half4 _GlowColor;
            float _Intensity;
            CBUFFER_END
            struct Attributes { float3 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.uv = input.uv;
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float2 p = abs((input.uv * 2.0 - 1.0) * float2(1.3856406, 1.6));
                float distance = abs(max(p.x, dot(p, float2(0.5, 0.8660254))) - 0.8660254);
                float light = exp2(-distance * 27.0) * 1.25 + exp2(-distance * 7.0) * 0.52;
                // A finite falloff avoids a visible rectangular cut at the halo quad's edge.
                light *= 1.0 - smoothstep(0.30, 0.50, distance);
                return half4(_GlowColor.rgb * (light * _Intensity), 0);
            }
            ENDHLSL
        }
    }
}
