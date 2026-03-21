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

        _FlickerStrength ("Flicker Strength", Range(0.0, 0.3)) = 0.03
        _FlickerSpeed ("Flicker Speed", Range(0.0, 10.0)) = 2.0

        _DustAmount ("Dust Amount", Range(0.0, 1.0)) = 0.35
        _DustSize ("Dust Size", Range(10.0, 120.0)) = 42.0
        _DustSoftness ("Dust Softness", Range(0.01, 0.5)) = 0.12
        _DustSpeed ("Dust Speed", Range(0.0, 3.0)) = 0.55
        _DustBrightness ("Dust Brightness", Range(0.0, 2.0)) = 0.45
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

            float _DustAmount;
            float _DustSize;
            float _DustSoftness;
            float _DustSpeed;
            float _DustBrightness;

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

            float hash22(float2 p)
            {
                p = frac(p * float2(234.56, 789.12));
                p += dot(p, p + 34.56);
                return frac(p.x + p.y);
            }

            float dustLayer(float2 uv, float t, float cellScale, float driftMul)
            {
                float2 g = uv * cellScale;

                float2 id = floor(g);
                float2 gv = frac(g) - 0.5;

                float rnd1 = hash21(id);
                float rnd2 = hash22(id + 17.3);

                // 입자 중심이 셀 안에서 천천히 떠다니게
                float2 offset = float2(
                    sin(t * (0.8 + rnd1 * 1.7) + rnd2 * 6.2831),
                    cos(t * (0.6 + rnd2 * 1.9) + rnd1 * 6.2831)
                ) * (0.18 * driftMul);

                float2 p = gv - offset;

                float d = length(p);
                float particle = 1.0 - smoothstep(_DustSoftness * 0.5, _DustSoftness, d);

                // 랜덤하게 일부 셀만 입자 보이도록
                float appear = step(0.72, rnd1);

                return particle * appear;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;

                float x = uv.x - 0.5;
                float y = uv.y;

                float currentWidth = lerp(_TopWidth, _BottomWidth, y);
                float edgeDist = (currentWidth * 0.5) - abs(x);
                float sideMask = smoothstep(0.0, _EdgeSoftness, edgeDist);

                float verticalFade = pow(saturate(1.0 - y), _HeightFade);
                float bottomFade = 1.0 - smoothstep(1.0 - _BottomRoundFade, 1.0, y);

                float centerGlow = 1.0 - saturate(abs(x) / max(currentWidth * 0.5, 0.0001));
                centerGlow = pow(centerGlow, 1.2);

                // 기존 미세 노이즈
                float noise = (hash21(floor(uv * 80.0) + floor(_Time.y * 8.0)) - 0.5) * 2.0;
                noise *= _NoiseStrength;

                // 더 자연스러운 미세 깜빡임
                float t = _Time.y;
                float flickerWaveA = sin(t * _FlickerSpeed * 1.00) * 0.5 + 0.5;
                float flickerWaveB = sin(t * _FlickerSpeed * 2.37 + 1.7) * 0.5 + 0.5;
                float flickerWaveC = sin(t * _FlickerSpeed * 0.53 + 2.9) * 0.5 + 0.5;

                float flickerMix = (flickerWaveA * 0.5 + flickerWaveB * 0.35 + flickerWaveC * 0.15);
                float flicker = 1.0 + (flickerMix - 0.5) * 2.0 * _FlickerStrength;

                // 먼지: 빛 중심/중하단 쪽에서 더 잘 보이게
                float coneMask = sideMask * verticalFade * bottomFade;
                float dustVisibility = coneMask * lerp(0.35, 1.0, centerGlow) * lerp(0.65, 1.0, 1.0 - y);

                float dust1 = dustLayer(uv + float2(0.0, -t * _DustSpeed * 0.08), t, _DustSize, 1.0);
                float dust2 = dustLayer(uv * 1.37 + float2(3.1, -t * _DustSpeed * 0.11), t * 1.2, _DustSize * 0.75, 1.3);

                float dust = (dust1 * 0.65 + dust2 * 0.35);
                dust *= _DustAmount * dustVisibility;

                float alpha = sideMask * verticalFade * bottomFade;
                alpha *= lerp(0.85, 1.15, centerGlow);
                alpha += noise;
                alpha += dust * 0.35;
                alpha = saturate(alpha);
                alpha *= _Color.a;
                alpha *= flicker;

                fixed3 rgb = _Color.rgb * alpha * _Intensity;

                // 먼지는 살짝 더 밝게 얹기
                rgb += _Color.rgb * dust * _DustBrightness * flicker;

                return fixed4(rgb, alpha);
            }
            ENDCG
        }
    }
}
