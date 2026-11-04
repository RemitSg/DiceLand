Shader "DiceLand/Battle Death Light"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+30" "RenderType"="Transparent" }
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
            struct Attributes { float3 positionOS:POSITION; float2 uv:TEXCOORD0; float2 kind:TEXCOORD1; half4 color:COLOR; };
            struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; float kind:TEXCOORD1; half4 color:COLOR; };
            Varyings Vert(Attributes input)
            {
                Varyings output; output.positionCS=TransformObjectToHClip(input.positionOS); output.uv=input.uv*2-1;
                output.kind=input.kind.x; output.color=input.color; return output;
            }
            half4 Frag(Varyings input):SV_Target
            {
                float2 p=input.uv; float radius=length(p); float mask; float core;
                if(input.kind<0.5) { mask=exp(-dot(p,p)*4.8)*saturate(1-radius); core=exp(-dot(p,p)*24); }
                else if(input.kind<1.5)
                {
                    float rays=exp(-abs(p.x)*35)*pow(saturate(1-abs(p.y)),2)+exp(-abs(p.y)*35)*pow(saturate(1-abs(p.x)),2);
                    mask=saturate(rays+exp(-dot(p,p)*12)*0.35); core=exp(-dot(p,p)*100);
                }
                else if(input.kind<2.5)
                {
                    float diamond=(abs(p.x)+abs(p.y))*1.3; mask=smoothstep(1,0.74,diamond); core=saturate(1-diamond*1.4);
                    mask*=0.55+0.45*step(p.x,0);
                }
                else if(input.kind<3.5) { float ring=abs(radius-0.8); mask=exp(-ring*ring*1100)*saturate(1-radius); core=exp(-ring*ring*5000); mask*=3; }
                else { mask=exp(-p.y*p.y*7)*smoothstep(1,0.85,abs(p.x)); core=exp(-p.y*p.y*40); }
                half3 color=lerp(input.color.rgb,half3(1,0.98,0.92),core*0.22);
                float opacity=saturate(mask)*input.color.a;
                float coverage=input.kind>1.5 && input.kind<2.5 ? opacity*0.85 : opacity*0.06;
                return half4(color*opacity*1.2, coverage);
            }
            ENDHLSL
        }
    }
}
