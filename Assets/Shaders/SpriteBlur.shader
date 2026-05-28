Shader "Custom/SpriteBlur"
{
    // 5x5 separable gaussian approx (single pass, 13 taps with Gauss weights)
    // 가중치 합 = 256. _BlurSize는 UV 단위 (0.002~0.01 권장). 너무 크면 sprite 경계 침범으로 깨짐.
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D)      = "white" {}
        _Color    ("Tint", Color)                              = (1,1,1,1)
        _BlurSize ("Blur Size (UV)", Range(0, 0.05))           = 0.004
    }
    SubShader
    {
        Tags
        {
            "Queue"            = "Transparent"
            "IgnoreProjector"  = "True"
            "RenderType"       = "Transparent"
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

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
                fixed4 color  : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv     : TEXCOORD0;
                fixed4 color  : COLOR;
            };

            sampler2D _MainTex;
            float4    _MainTex_TexelSize;
            fixed4    _Color;
            float     _BlurSize;

            v2f vert(appdata_t IN)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(IN.vertex);
                o.uv     = IN.uv;
                o.color  = IN.color * _Color;
                return o;
            }

            // 5x5 gaussian kernel weights (1 4 6 4 1)x(1 4 6 4 1) / 256
            // Approximate using 13 strategic taps so single-pass cost is acceptable.
            fixed4 frag(v2f IN) : SV_Target
            {
                float2 d = float2(_BlurSize, _BlurSize);

                // center weight 36/256
                fixed4 c = tex2D(_MainTex, IN.uv) * 36.0;

                // 1-step orthogonal: weight 24 each (4 taps)
                c += tex2D(_MainTex, IN.uv + float2(-d.x, 0)) * 24.0;
                c += tex2D(_MainTex, IN.uv + float2( d.x, 0)) * 24.0;
                c += tex2D(_MainTex, IN.uv + float2(0, -d.y)) * 24.0;
                c += tex2D(_MainTex, IN.uv + float2(0,  d.y)) * 24.0;

                // diagonals: weight 16 each (4 taps)
                c += tex2D(_MainTex, IN.uv + float2(-d.x, -d.y)) * 16.0;
                c += tex2D(_MainTex, IN.uv + float2( d.x, -d.y)) * 16.0;
                c += tex2D(_MainTex, IN.uv + float2(-d.x,  d.y)) * 16.0;
                c += tex2D(_MainTex, IN.uv + float2( d.x,  d.y)) * 16.0;

                // 2-step orthogonal: weight 6 each (4 taps)
                c += tex2D(_MainTex, IN.uv + float2(-d.x*2, 0)) * 6.0;
                c += tex2D(_MainTex, IN.uv + float2( d.x*2, 0)) * 6.0;
                c += tex2D(_MainTex, IN.uv + float2(0, -d.y*2)) * 6.0;
                c += tex2D(_MainTex, IN.uv + float2(0,  d.y*2)) * 6.0;

                // sum = 36 + 24*4 + 16*4 + 6*4 = 36 + 96 + 64 + 24 = 220
                c /= 220.0;
                return c * IN.color;
            }
            ENDCG
        }
    }
}
