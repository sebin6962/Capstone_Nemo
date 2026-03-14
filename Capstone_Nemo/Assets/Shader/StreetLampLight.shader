Shader "Unlit/StreetLampLight"
{
     Properties
    {
        _Color ("Light Color", Color) = (1, 0.95, 0.7, 0.8)
        _TopWidth ("Top Width", Range(0.0, 1.0)) = 0.55
        _BottomWidth ("Bottom Width", Range(0.0, 2.0)) = 0.85
        _HeightFade ("Vertical Fade", Range(0.1, 5.0)) = 1.5
        _EdgeSoftness ("Edge Softness", Range(0.001, 0.5)) = 0.12
        _Intensity ("Intensity", Range(0.0, 3.0)) = 1.2
        _BottomRoundFade ("Bottom Fade", Range(0.0, 1.0)) = 0.2
        _NoiseStrength ("Noise Strength", Range(0.0, 0.2)) = 0.03
        _FlickerStrength ("Flicker Strength", Range(0.0, 0.3)) = 0.04
        _FlickerSpeed ("Flicker Speed", Range(0.0, 10.0)) = 2.0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Blend SrcAlpha One
        Cull Off
        Lighting Off
        ZWrite Off

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
                fixed4 color  : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv     : TEXCOORD0;
                fixed4 color  : COLOR;
            };

            fixed4 _Color;
            float _TopWidth;
            float _BottomWidth;
            float _HeightFade;
            float _EdgeSoftness;
            float _Intensity;
            float _BottomRoundFade;
            float _NoiseStrength;
            float _FlickerStrength;
            float _FlickerSpeed;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // UV 기준
                // x: 0~1 (좌우)
                // y: 0~1 (위->아래)
                float2 uv = i.uv;

                // 중심 기준 x 좌표 (-0.5 ~ 0.5)
                float x = uv.x - 0.5;
                float y = uv.y;

                // 위에서 아래로 갈수록 폭 증가
                float currentWidth = lerp(_TopWidth, _BottomWidth, y);

                // 사다리꼴 내부 마스크
                // |x| <= currentWidth * 0.5 이면 안쪽
                float edgeDist = (currentWidth * 0.5) - abs(x);

                // 가장자리 부드럽게
                float sideMask = smoothstep(0.0, _EdgeSoftness, edgeDist);

                // 세로 방향 밝기
                // 위쪽이 가장 밝고 아래로 갈수록 감쇠
                float verticalFade = pow(saturate(1.0 - y), _HeightFade);

                // 아래쪽 끝부분도 자연스럽게 사라지게
                float bottomFade = 1.0 - smoothstep(1.0 - _BottomRoundFade, 1.0, y);

                // 중앙부가 조금 더 밝게
                float centerGlow = 1.0 - saturate(abs(x) / max(currentWidth * 0.5, 0.0001));
                centerGlow = pow(centerGlow, 1.2);

                // 미세 노이즈
                float noise = (hash21(floor(uv * 80.0) + _Time.y) - 0.5) * 2.0;
                noise *= _NoiseStrength;

                // 깜빡임
                float flicker = 1.0 + sin(_Time.y * _FlickerSpeed) * _FlickerStrength;

                float alpha = sideMask * verticalFade * bottomFade;
                alpha *= lerp(0.85, 1.15, centerGlow);
                alpha += noise;
                alpha = saturate(alpha);
                alpha *= _Color.a;
                alpha *= flicker;

                fixed3 rgb = _Color.rgb * alpha * _Intensity;

                return fixed4(rgb, alpha);
            }
            ENDCG
        }
    }
}
