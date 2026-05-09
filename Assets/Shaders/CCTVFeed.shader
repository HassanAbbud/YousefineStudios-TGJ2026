Shader "UI/CCTVFeed"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _PixelSize ("Pixel Size (px)", Float) = 4
        _StaticAmount ("Static Amount", Range(0,1)) = 0.15
        _StaticSpeed ("Static Speed", Float) = 30
        _ScanlineStrength ("Scanline Strength", Range(0,1)) = 0.2
        _ScanlineCount ("Scanline Count", Float) = 180
        _Vignette ("Vignette", Range(0,2)) = 0.6
        _GreenTint ("Green/Phosphor Tint", Range(0,1)) = 0.0

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "PreviewType"="Plane" }
        Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
        Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode] Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float4 _Color;
            float _PixelSize;
            float _StaticAmount;
            float _StaticSpeed;
            float _ScanlineStrength;
            float _ScanlineCount;
            float _Vignette;
            float _GreenTint;

            v2f vert(appdata v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.uv; o.color = v.color; return o; }

            float rand(float2 co) { return frac(sin(dot(co, float2(12.9898,78.233))) * 43758.5453); }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 px = _MainTex_TexelSize.zw / max(_PixelSize, 1.0);
                float2 uv = floor(i.uv * px) / px;

                fixed4 col = tex2D(_MainTex, uv) * _Color * i.color;

                float n = rand(uv + _Time.y * _StaticSpeed);
                col.rgb = lerp(col.rgb, float3(n,n,n), _StaticAmount * n);

                float sc = sin(i.uv.y * _ScanlineCount * 3.14159) * 0.5 + 0.5;
                col.rgb *= lerp(1.0, sc, _ScanlineStrength);

                float2 vc = i.uv - 0.5;
                float v = 1.0 - dot(vc, vc) * _Vignette;
                col.rgb *= saturate(v);

                col.rgb = lerp(col.rgb, col.rgb * float3(0.85, 1.05, 0.85), _GreenTint);

                return col;
            }
            ENDCG
        }
    }
}