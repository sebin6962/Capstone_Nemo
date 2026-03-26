Shader "Unlit/LanternRadialLight"
{
    Properties
    {
        _Color ("Light Color", Color) = (1, 0.9, 0.65, 0.8)

        _CenterX ("Center X", Range(0.0, 1.0)) = 0.5
        _CenterY ("Center Y", Range(-0.5, 1.0)) = 0.30

        _Radius ("Core Radius", Range(0.01, 2.0)) = 0.40
        _Aspect ("Core Aspect", Range(0.2, 3.0)) = 1.0
        _EdgeSoftness ("Core Edge Softness", Range(0.001, 1.0)) = 0.16

        _OuterRadius ("Outer Radius", Range(0.01, 3.0)) = 0.90
        _OuterSoftness ("Outer Softness", Range(0.01, 2.0)) = 0.65
        _OuterIntensity ("Outer Intensity", Range(0.0, 2.0)) = 0.48
        _OuterVerticalStretch ("Outer Vertical Stretch", Range(0.5, 2.5)) = 1.18

        _InnerGlow ("Inner Glow", Range(0.1, 4.0)) = 1.45
        _Intensity ("Intensity", Range(0.0, 3.0)) = 1.18
        _OuterFade ("Outer Fade", Range(0.2, 4.0)) = 1.1

        _NoiseStrength ("Noise Strength", Range(0.0, 0.2)) = 0.018

        _FlickerStrength ("Flicker Strength", Range(0.0, 0.5)) = 0.06
        _FlickerSpeed ("Flicker Speed", Range(0.0, 10.0)) = 0.65

        _DustAmount ("Dust Amount", Range(0.0, 1.0)) = 0.20
        _DustSize ("Dust Size", Range(10.0, 120.0)) = 36.0
        _DustSoftness ("Dust Softness", Range(0.01, 0.5)) = 0.09
        _DustSpeed ("Dust Speed", Range(0.0, 3.0)) = 0.35
        _DustBrightness ("Dust Brightness", Range(0.0, 2.0)) = 0.25
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

            float _CenterX;
            float _CenterY;

            float _Radius;
            float _Aspect;
            float _EdgeSoftness;

            float _OuterRadius;
            float _OuterSoftness;
            float _OuterIntensity;
            float _OuterVerticalStretch;

            float _InnerGlow;
            float _Intensity;
            float _OuterFade;

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

            float dustLayer(float2 uv, float t, float cellScale, float driftMul, float softness)
            {
                float2 g = uv * cellScale;

                float2 id = floor(g);
                float2 gv = frac(g) - 0.5;

                float rnd1 = hash21(id);
                float rnd2 = hash22(id + 17.3);

                float2 offset = float2(
                    sin(t * (0.8 + rnd1 * 1.7) + rnd2 * 6.2831),
                    cos(t * (0.6 + rnd2 * 1.9) + rnd1 * 6.2831)
                ) * (0.18 * driftMul);

                float2 p = gv - offset;
                float d = length(p);

                float particle = 1.0 - smoothstep(softness * 0.5, softness, d);
                float appear = step(0.74, rnd1);

                return particle * appear;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float t = _Time.y;

                float2 center = float2(_CenterX, _CenterY);

                float2 pCore = uv - center;
                pCore.x /= max(_Aspect, 0.0001);

                float distCore = length(pCore);

                float coreRadial = 1.0 - smoothstep(_Radius - _EdgeSoftness, _Radius, distCore);
                coreRadial = saturate(coreRadial);

                float inner = 1.0 - saturate(distCore / max(_Radius, 0.0001));
                inner = pow(inner, _OuterFade);

                float coreGlow = lerp(coreRadial, 1.0, inner);
                coreGlow = saturate(pow(coreGlow, 1.0 / max(_InnerGlow, 0.001)));

                float bottomLift = smoothstep(-0.15, 0.15, uv.y - (_CenterY - _Radius * 0.9));
                float coreMask = coreRadial * lerp(0.92, 1.0, bottomLift);

                float2 pOuter = uv - center;
                pOuter.x /= max(_Aspect, 0.0001);
                pOuter.y /= max(_OuterVerticalStretch, 0.0001);

                float distOuter = length(pOuter);

                float outerHalo = 1.0 - smoothstep(_OuterRadius - _OuterSoftness, _OuterRadius, distOuter);
                outerHalo = saturate(outerHalo);

                float outerRingBias = smoothstep(0.08, 0.45, distCore / max(_Radius, 0.0001));
                outerHalo *= lerp(0.55, 1.0, outerRingBias);

                float spaceLift = smoothstep(_CenterY - 0.15, _CenterY + 0.45, uv.y);
                outerHalo *= lerp(1.0, 0.82, spaceLift);

                float shapeMask = saturate(coreMask + outerHalo * _OuterIntensity);
                float centerGlow = saturate(coreGlow + outerHalo * (_OuterIntensity * 0.35));

                float noise = (hash21(floor(uv * 80.0) + floor(t * 8.0)) - 0.5) * 2.0;
                noise *= _NoiseStrength;

                // ´À¸´ÇÑ ·£ÅÏ½Ä ±ôºýÀÓ
                float flickerWaveA = sin(t * _FlickerSpeed * 0.55 + 0.4) * 0.5 + 0.5;
                float flickerWaveB = sin(t * _FlickerSpeed * 1.05 + 2.1) * 0.5 + 0.5;
                float flickerWaveC = sin(t * _FlickerSpeed * 0.28 + 4.0) * 0.5 + 0.5;

                float flickerMix = flickerWaveA * 0.5 + flickerWaveB * 0.3 + flickerWaveC * 0.2;
                float flickerNoise = (hash21(float2(floor(t * 1.2), 17.0)) - 0.5) * 0.08;

                float flicker = 1.0 + ((flickerMix - 0.5) * 0.85 + flickerNoise) * (_FlickerStrength * 2.0);
                flicker = max(0.75, flicker);

                float ringMask =
                    smoothstep(0.05, 0.55, distCore / max(_Radius, 0.0001)) *
                    (1.0 - smoothstep(0.65, 1.0, distCore / max(_Radius, 0.0001)));

                float outerDustMask = 1.0 - smoothstep(0.55, 1.0, distOuter / max(_OuterRadius, 0.0001));
                float dustVisibility = saturate(shapeMask * lerp(0.45, 1.0, ringMask + outerDustMask * 0.35));

                float dust1 = dustLayer(
                    uv + float2(0.0, -t * _DustSpeed * 0.08),
                    t,
                    _DustSize,
                    1.0,
                    _DustSoftness
                );

                float dust2 = dustLayer(
                    uv * 1.31 + float2(2.7, -t * _DustSpeed * 0.11),
                    t * 1.2,
                    _DustSize * 0.72,
                    1.25,
                    _DustSoftness
                );

                float dust = (dust1 * 0.65 + dust2 * 0.35) * _DustAmount * dustVisibility;

                float alpha = shapeMask;
                alpha *= lerp(0.9, 1.18, inner);
                alpha += noise;
                alpha += dust * 0.28;
                alpha = saturate(alpha);
                alpha *= _Color.a;
                alpha *= flicker;

                fixed3 rgb = _Color.rgb * alpha * _Intensity;
                rgb += _Color.rgb * outerHalo * (_OuterIntensity * 0.22) * flicker;
                rgb += _Color.rgb * centerGlow * 0.05 * flicker;
                rgb += _Color.rgb * dust * _DustBrightness * flicker;

                return fixed4(rgb, alpha);
            }
            ENDCG
        }
    }
}
