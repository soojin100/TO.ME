Shader "Custom/SpriteOutline"
{
    // Alpha-edge based sprite outline. Works with Tight or FullRect sprite mesh.
    // Thickness in TEXEL units (소스 텍스처 픽셀) → 스프라이트 스케일과 무관하게 일관된 굵기.
    // 한 패스로 외곽선 + 본체 동시 출력. _OutlineEnabled=0이면 본체만.
    // 외곽선은 알파 컨텐츠 주변의 투명 영역에 그려지므로, 스프라이트 PNG에 약간의
    // 투명 패딩이 있어야 보임 (대부분 스프라이트는 패딩 있음).
    Properties
    {
        _MainTex          ("Texture", 2D)                = "white" {}
        _Color            ("Tint", Color)                = (1,1,1,1)
        _OutlineColor     ("Outline Color", Color)       = (1, 0.85, 0.35, 1)
        _OutlineThickness ("Outline Thickness (texels)", Range(0, 8)) = 2
        _OutlineEnabled   ("Outline Enabled", Float)     = 0
    }
    SubShader
    {
        Tags
        {
            "Queue"            = "Transparent"
            "RenderType"       = "Transparent"
            "IgnoreProjector"  = "True"
            "PreviewType"      = "Plane"
            "CanUseSpriteAtlas"= "True"
        }
        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
                float4 color  : COLOR;
            };
            struct v2f
            {
                float4 pos    : SV_POSITION;
                float2 uv     : TEXCOORD0;
                float4 color  : COLOR;
            };

            sampler2D _MainTex;
            float4    _MainTex_ST;
            float4    _MainTex_TexelSize; // (1/w, 1/h, w, h)
            float4    _Color;
            float4    _OutlineColor;
            float     _OutlineThickness;
            float     _OutlineEnabled;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos   = UnityObjectToClipPos(v.vertex);
                o.uv    = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                return o;
            }

            float sampleA(float2 uv) { return tex2D(_MainTex, uv).a; }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 sprite = tex2D(_MainTex, i.uv) * i.color * _Color;

                if (_OutlineEnabled < 0.5)
                    return sprite;

                // 본체 (opaque) 영역은 원래 색
                if (sprite.a > 0.5)
                    return sprite;

                // 투명 영역: 두께 범위 이내에 opaque 픽셀이 있으면 outline 색
                float2 px = _MainTex_TexelSize.xy * _OutlineThickness;
                float n0 = sampleA(i.uv + float2( px.x,  0));
                float n1 = sampleA(i.uv + float2(-px.x,  0));
                float n2 = sampleA(i.uv + float2( 0,     px.y));
                float n3 = sampleA(i.uv + float2( 0,    -px.y));
                float n4 = sampleA(i.uv + float2( px.x,  px.y));
                float n5 = sampleA(i.uv + float2(-px.x, -px.y));
                float n6 = sampleA(i.uv + float2( px.x, -px.y));
                float n7 = sampleA(i.uv + float2(-px.x,  px.y));
                float maxA = max(max(max(n0, n1), max(n2, n3)),
                                 max(max(n4, n5), max(n6, n7)));

                if (maxA > 0.5)
                    return fixed4(_OutlineColor.rgb, _OutlineColor.a);

                // 본체도 외곽선도 아니면 투명
                return sprite;
            }
            ENDCG
        }
    }
}
