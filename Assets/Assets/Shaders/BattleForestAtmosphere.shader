Shader "DiceLand/Battle Forest Atmosphere"
{
    Properties
    {
        _Color ("Tint and density", Color) = (1,0.9,0.65,0.12)
        [Enum(Light shaft,0,Ground mist,1,Dust,2,Leaf,3)] _Mode ("Shape", Float) = 0
        _SoftDistance ("Soft intersection distance", Float) = 1.5
        _ShaftStriation ("Leaf-filtered shaft detail", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            CBUFFER_START(UnityPerMaterial)
            half4 _Color;
            float _Mode, _SoftDistance, _ShaftStriation;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float eyeDepth : TEXCOORD1; half4 color : COLOR; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 positionWS=TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS=TransformWorldToHClip(positionWS);
                output.eyeDepth=-TransformWorldToView(positionWS).z;
                output.uv=input.uv;
                output.color=input.color;
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv=input.uv;
                float mask;
                if (_Mode < 0.5)
                {
                    float edge=1.0-smoothstep(0.06,0.5,abs(uv.x-0.5));
                    float endFade=smoothstep(0.0,0.19,uv.y)*(1.0-smoothstep(0.85,1.0,uv.y));
                    mask=edge*edge*endFade*(0.88+0.12*sin(uv.y*11.0+uv.x*7.0+_Time.y*0.13));
                    // Uneven canopy gaps leave coherent ribbons along the sun direction.
                    // Zero retains the original broad shaft; other atmosphere modes stay unchanged.
                    float2 origin=TransformObjectToWorld(float3(0,0,0)).xz;
                    float seed=frac(dot(origin,float2(0.1031,0.11369)));
                    float width=lerp(0.85,1.2,frac(seed*7.13+0.37));
                    float across=(uv.x-0.5)/width+0.5+(seed-0.5)*0.12;
                    float ribbons=0.90*(1.0-smoothstep(0.025,0.09,abs(across-0.32)));
                    ribbons+=1.0-smoothstep(0.02,0.075,abs(across-0.50));
                    ribbons+=0.55*(1.0-smoothstep(0.02,0.06,abs(across-0.67)));
                    mask*=lerp(1.0,0.28+ribbons,saturate(_ShaftStriation));
                }
                else if (_Mode < 1.5)
                {
                    float drift=sin(uv.x*17.0+_Time.y*0.07)*0.035+sin(uv.x*39.0-_Time.y*0.11)*0.018;
                    mask=pow(saturate(1.0-abs(uv.y-0.45+drift)*2.3),2.0);
                    mask*=smoothstep(0.0,0.12,uv.x)*(1.0-smoothstep(0.88,1.0,uv.x));
                }
                else if (_Mode < 2.5)
                {
                    mask=pow(saturate(1.0-length(uv-0.5)*2.0),2.0);
                }
                else
                {
                    // Vertex blue is constant over a leaf's lifetime: flutter stays independent
                    // of camera cuts while the particle system supplies spin and world drift.
                    float arc=sin(saturate(uv.y)*PI);
                    float flutter=0.35+0.65*abs(sin(_Time.y*1.25+input.color.b*19.0));
                    float spine=0.5+0.08*arc;
                    float across=(uv.x-spine)/max(0.003,0.27*arc*flutter);
                    float feather=clamp(fwidth(across),0.04,0.5);
                    mask=1.0-smoothstep(1.0-feather,1.0+feather,abs(across));
                    mask*=smoothstep(0.0,0.04,uv.y)*(1.0-smoothstep(0.96,1.0,uv.y));
                    float vein=(1.0-smoothstep(0.02,0.09,abs(across)))*arc;
                    input.color.rgb*=lerp(0.65,1.05,smoothstep(-0.7,0.6,across))+vein*0.3;
                }
                float rawDepth=SampleSceneDepth(GetNormalizedScreenSpaceUV(input.positionCS));
                float sceneDepth=unity_OrthoParams.w==0 ? LinearEyeDepth(rawDepth,_ZBufferParams) : LinearDepthToEyeDepth(rawDepth);
                float intersection=saturate((sceneDepth-input.eyeDepth)/max(0.01,_SoftDistance));
                return half4(_Color.rgb*input.color.rgb,_Color.a*input.color.a*mask*intersection);
            }
            ENDHLSL
        }
    }
}
