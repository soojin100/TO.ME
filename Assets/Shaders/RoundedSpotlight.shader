Shader "TOME/RoundedSpotlight"
{
    Properties
    {
        _Color      ("Dim Color", Color) = (0,0,0,1)
        _Center     ("Spotlight Center (world xy)", Vector) = (0,0,0,0)
        _HalfSize   ("Spotlight Half Size (world xy)", Vector) = (0,0,0,0)
        _Radius     ("Corner Radius (world)", Float) = 0.6
        _Softness   ("Edge Softness (world)", Float) = 0.15
        _DimAmount  ("Dim Amount", Range(0,1)) = 0.55
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings   { float4 positionCS : SV_POSITION; float2 worldXY : TEXCOORD0; };

            float4 _Color;
            float4 _Center;
            float4 _HalfSize;
            float  _Radius;
            float  _Softness;
            float  _DimAmount;

            Varyings vert (Attributes v)
            {
                Varyings o;
                float3 wp = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(wp);
                o.worldXY = wp.xy;
                return o;
            }

            // 둥근 사각형 SDF. 0보다 작으면 내부.
            float RoundedBoxSDF(float2 p, float2 halfSize, float r)
            {
                float2 q = abs(p) - (halfSize - r);
                return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - r;
            }

            half4 frag (Varyings i) : SV_Target
            {
                // HalfSize가 0이면 스포트라이트 없음 → 화면 전체 균일 암전.
                float cover = 1.0;
                if (_HalfSize.x > 0.0001 && _HalfSize.y > 0.0001)
                {
                    float r = min(_Radius, min(_HalfSize.x, _HalfSize.y));
                    float d = RoundedBoxSDF(i.worldXY - _Center.xy, _HalfSize.xy, r);
                    // 내부(d<0)는 0, 외부는 1. _Softness 폭으로 부드럽게.
                    cover = smoothstep(0.0, max(_Softness, 0.0001), d);
                }
                half4 c = _Color;
                c.a *= cover * _DimAmount;
                return c;
            }
            ENDHLSL
        }
    }
    FallBack Off
}
