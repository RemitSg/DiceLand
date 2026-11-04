Shader "DiceLand/Battle Death Dissolve"
{
    Properties
    {
        [PerRendererData] _MainTex("Sprite",2D)="white"{}
        [HideInInspector] _Flip("Sprite flip",Vector)=(1,1,1,1)
        [HideInInspector] _RendererColor("Sprite tint",Color)=(1,1,1,1)
        _AlphaTex("Split alpha",2D)="white"{}
        _UseExternalAlpha("Use split alpha",Float)=0
        _SoulColor("Soul color",Color)=(0.2,0.88,1,1)
        _EdgeColor("Fragment edge",Color)=(1,0.87,0.48,1)
        _Age("Age",Float)=0
        _BodyBounds("Body bottom and height",Vector)=(0,3,0,2)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+20" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.0
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            TEXTURE2D(_AlphaTex); SAMPLER(sampler_AlphaTex);
            CBUFFER_START(UnityPerMaterial)
            half4 _SoulColor,_EdgeColor,_RendererColor; float4 _BodyBounds,_Flip; float _Age,_UseExternalAlpha;
            CBUFFER_END
            struct Attributes { float3 positionOS:POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; };
            struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; float2 body:TEXCOORD1; half4 color:COLOR; };
            float Hash(float2 p) { return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }
            float Noise(float2 p)
            {
                float2 cell=floor(p), f=frac(p); f=f*f*(3-2*f);
                return lerp(lerp(Hash(cell),Hash(cell+float2(1,0)),f.x),lerp(Hash(cell+float2(0,1)),Hash(cell+1),f.x),f.y);
            }
            Varyings Vert(Attributes input)
            {
                Varyings output; input.positionOS.xy*=_Flip.xy; float3 world=TransformObjectToWorld(input.positionOS);
                output.positionCS=TransformWorldToHClip(world); output.uv=input.uv; output.color=input.color*_RendererColor;
                output.body=float2((world.x-_BodyBounds.z)/_BodyBounds.w,(world.y-_BodyBounds.x)/_BodyBounds.y); return output;
            }
            half4 Frag(Varyings input):SV_Target
            {
                half4 sprite=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,input.uv)*input.color;
                if(_UseExternalAlpha>0.5) sprite.a=SAMPLE_TEXTURE2D(_AlphaTex,sampler_AlphaTex,input.uv).r*input.color.a;
                float noise=Noise(input.body*float2(19,27))*0.58+Noise(input.body*float2(43,57))*0.16+input.body.y*0.26;
                float progress=saturate((_Age-0.1)/0.72)*1.15-0.1;
                float edge=noise-progress;
                float coverage=smoothstep(-0.02,0.035,edge)*saturate((0.95-_Age)*12);
                float rim=(1-smoothstep(0.01,0.16,edge))*step(0,edge);
                float flash=pow(saturate(1-abs(_Age-0.075)/0.075),2);
                half3 color=lerp(sprite.rgb,_SoulColor.rgb*0.6+sprite.rgb*0.4,saturate(_Age*2));
                color=lerp(color,half3(1,0.98,0.91)*1.8,flash*0.8);
                color+=lerp(_SoulColor.rgb,_EdgeColor.rgb,rim)*rim*2.2;
                return half4(color,sprite.a*coverage);
            }
            ENDHLSL
        }
    }
}
