Shader "MobileGame/RestScene/SolidSpriteOutline"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _OutlineWidth ("Outline Width", Float) = 1
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment SpriteFrag
            #pragma target 2.0
            #pragma multi_compile_local _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA

            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            sampler2D _MainTex;
            sampler2D _AlphaTex;
            float4 _MainTex_TexelSize;
            fixed4 _Color;
            fixed4 _RendererColor;
            float _OutlineWidth;
            float _EnableExternalAlpha;

            v2f SpriteVert(appdata_t input)
            {
                v2f output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.texcoord = input.texcoord;
                output.color = input.color * _Color * _RendererColor;

                #ifdef PIXELSNAP_ON
                output.vertex = UnityPixelSnap(output.vertex);
                #endif

                return output;
            }

            fixed SampleSpriteAlpha(float2 uv)
            {
                fixed4 color = tex2D(_MainTex, uv);

                #if ETC1_EXTERNAL_ALPHA
                color.a = lerp(color.a, tex2D(_AlphaTex, uv).r, _EnableExternalAlpha);
                #endif

                return color.a;
            }

            fixed4 SpriteFrag(v2f input) : SV_Target
            {
                fixed baseAlpha = SampleSpriteAlpha(input.texcoord);
                float2 texelOffset = _MainTex_TexelSize.xy * max(_OutlineWidth, 0.0);

                fixed expandedAlpha = baseAlpha;
                expandedAlpha = max(expandedAlpha, SampleSpriteAlpha(input.texcoord + float2(texelOffset.x, 0.0)));
                expandedAlpha = max(expandedAlpha, SampleSpriteAlpha(input.texcoord + float2(-texelOffset.x, 0.0)));
                expandedAlpha = max(expandedAlpha, SampleSpriteAlpha(input.texcoord + float2(0.0, texelOffset.y)));
                expandedAlpha = max(expandedAlpha, SampleSpriteAlpha(input.texcoord + float2(0.0, -texelOffset.y)));
                expandedAlpha = max(expandedAlpha, SampleSpriteAlpha(input.texcoord + texelOffset));
                expandedAlpha = max(expandedAlpha, SampleSpriteAlpha(input.texcoord + float2(texelOffset.x, -texelOffset.y)));
                expandedAlpha = max(expandedAlpha, SampleSpriteAlpha(input.texcoord + float2(-texelOffset.x, texelOffset.y)));
                expandedAlpha = max(expandedAlpha, SampleSpriteAlpha(input.texcoord - texelOffset));

                fixed outlineAlpha = saturate(expandedAlpha - baseAlpha);
                fixed4 color = input.color;
                color.a *= outlineAlpha;
                return color;
            }
            ENDCG
        }
    }
}
