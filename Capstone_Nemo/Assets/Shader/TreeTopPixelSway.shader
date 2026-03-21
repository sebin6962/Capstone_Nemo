Shader "Unlit/TreeTopPixelSway"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _Amplitude ("Horizontal Amplitude", Range(0, 0.2)) = 0.03
        _VerticalAmplitude ("Vertical Wave Amplitude", Range(0, 0.15)) = 0.015
        _Speed ("Speed", Range(0, 4)) = 1.0
        _TopStart ("Top Start (UV Y)", Range(0, 1)) = 0.15
        _Feather ("Feather", Range(0.01, 0.5)) = 0.35
        _PhaseOffset ("Phase Offset", Float) = 0

        _BendStrength ("Bend Strength", Range(0, 0.2)) = 0.035
        _EdgeBoost ("Edge Boost", Range(0, 2)) = 0.35
        _VerticalSquash ("Vertical Squash", Range(0, 0.1)) = 0.012

        _SecondWaveStrength ("Second Wave Strength", Range(0, 1)) = 0.35
        _HeightPhaseShift ("Height Phase Shift", Range(0, 5)) = 1.4
        _VerticalPhaseShift ("Vertical Phase Shift", Range(0, 5)) = 2.1
        _CenterLift ("Center Lift", Range(0, 2)) = 0.35
        _UvWaveInfluence ("UV Wave Influence", Range(0, 5)) = 1.2

        _RegionPhaseAmount ("Region Phase Amount", Range(0, 3)) = 0.55
        _RegionAmplitudeBoost ("Region Amplitude Boost", Range(0, 1)) = 0.18
        _RegionVerticalBoost ("Region Vertical Boost", Range(0, 1)) = 0.15

        _UsePixelSnap ("Use Pixel Snap", Range(0,1)) = 0
        _PixelPerUnit ("Pixels Per Unit", Float) = 16

        // Aurora colors
        _PurpleColor ("Purple Color", Color) = (0.80, 0.30, 1.0, 1.0)
        _BlueColor ("Blue Color", Color) = (0.18, 0.50, 1.0, 1.0)
        _HighlightColor ("Highlight Color", Color) = (0.88, 0.98, 1.0, 1.0)

        _OverlayStrength ("Overlay Strength", Range(0, 1)) = 0.78
        _AuroraBrightness ("Aurora Brightness", Range(0, 3)) = 1.25

        _AuroraSpeed ("Aurora Speed", Range(0, 5)) = 0.18
        _AuroraScale ("Aurora Scale", Range(0.1, 10)) = 1.6
        _DistortionStrength ("Distortion Strength", Range(0, 1)) = 0.08
        _FlowStrength ("Flow Strength", Range(0, 1)) = 0.04

        _CycleWidth ("Cycle Width", Range(0.05, 0.6)) = 0.34
        _CycleSharpness ("Cycle Sharpness", Range(0.5, 6.0)) = 1.6
        _HighlightShift ("Highlight Shift", Range(-0.2, 0.2)) = 0.0

        _HighlightAmount ("Highlight Amount", Range(0, 1)) = 0.20
        _HighlightSoftness ("Highlight Softness", Range(0.1, 3)) = 1.5

        _ShimmerStrength ("Shimmer Strength", Range(0, 0.3)) = 0.02
        _ShimmerSpeed ("Shimmer Speed", Range(0, 10)) = 0.7
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

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 uv       : TEXCOORD0;
            };

            sampler2D _MainTex;
            fixed4 _Color;

            float _Amplitude;
            float _VerticalAmplitude;
            float _Speed;
            float _TopStart;
            float _Feather;
            float _PhaseOffset;

            float _BendStrength;
            float _EdgeBoost;
            float _VerticalSquash;

            float _SecondWaveStrength;
            float _HeightPhaseShift;
            float _VerticalPhaseShift;
            float _CenterLift;
            float _UvWaveInfluence;

            float _RegionPhaseAmount;
            float _RegionAmplitudeBoost;
            float _RegionVerticalBoost;

            float _UsePixelSnap;
            float _PixelPerUnit;

            fixed4 _PurpleColor;
            fixed4 _BlueColor;
            fixed4 _HighlightColor;

            float _OverlayStrength;
            float _AuroraBrightness;

            float _AuroraSpeed;
            float _AuroraScale;
            float _DistortionStrength;
            float _FlowStrength;

            float _CycleWidth;
            float _CycleSharpness;
            float _HighlightShift;

            float _HighlightAmount;
            float _HighlightSoftness;

            float _ShimmerStrength;
            float _ShimmerSpeed;

            v2f vert(appdata_t IN)
            {
                v2f OUT;

                float2 localPos = IN.vertex.xy;
                float2 uv = IN.texcoord;

                float mask = smoothstep(_TopStart, min(1.0, _TopStart + _Feather), uv.y);
                float top01 = saturate((uv.y - _TopStart) / max(0.0001, (1.0 - _TopStart)));
                float strength = mask * top01;

                float fromCenter = abs(uv.x - 0.5) * 2.0;
                float centerFactor = 1.0 - fromCenter;
                float edgeFactor = 1.0 + fromCenter * _EdgeBoost;

                float sideProtect = smoothstep(0.0, 0.12, uv.x) * (1.0 - smoothstep(0.88, 1.0, uv.x));

                float leftMask  = 1.0 - smoothstep(0.28, 0.48, uv.x);
                float rightMask = smoothstep(0.52, 0.72, uv.x);
                float centerMask = saturate(1.0 - leftMask - rightMask);

                float regionPhaseOffset =
                    leftMask  * _RegionPhaseAmount +
                    centerMask * 0.0 +
                    rightMask * (-_RegionPhaseAmount * 0.92);

                float regionAmpMul =
                    leftMask  * (1.0 + _RegionAmplitudeBoost * 0.35) +
                    centerMask * 0.96 +
                    rightMask * (1.0 + _RegionAmplitudeBoost * 0.40);

                float regionVerticalMul =
                    leftMask  * (1.0 + _RegionVerticalBoost * 0.85) +
                    centerMask * (1.0 + _CenterLift * 0.15) +
                    rightMask * (1.0 + _RegionVerticalBoost);

                float t = _Time.y * _Speed + _PhaseOffset;

                float phase = t + top01 * _HeightPhaseShift + regionPhaseOffset;

                float sway =
                    sin(phase) * (1.0 - _SecondWaveStrength) +
                    sin(phase * 0.67 + 0.8 + uv.x * _UvWaveInfluence) * _SecondWaveStrength;

                float xOffset = sway * _Amplitude * strength * edgeFactor * regionAmpMul * sideProtect;

                float bend = sway * _BendStrength * strength * strength * sideProtect;
                xOffset += bend;

                float verticalPhase =
                    t * 1.15 +
                    top01 * _VerticalPhaseShift +
                    uv.x * _UvWaveInfluence +
                    regionPhaseOffset * 1.25;

                float verticalWave =
                    sin(verticalPhase + 0.6) * 0.7 +
                    sin(verticalPhase * 1.73 + 1.2) * 0.3;

                float lift = centerFactor * _CenterLift;

                float yOffset =
                    verticalWave * _VerticalAmplitude * strength * (0.65 + lift) * regionVerticalMul
                    - abs(sway) * _VerticalSquash * strength;

                if (_UsePixelSnap > 0.5)
                {
                    float unitPerPixel = 1.0 / max(_PixelPerUnit, 1.0);
                    xOffset = round(xOffset / unitPerPixel) * unitPerPixel;
                    yOffset = round(yOffset / unitPerPixel) * unitPerPixel;
                }

                localPos.x += xOffset;
                localPos.y += yOffset;

                OUT.vertex = UnityObjectToClipPos(float4(localPos, IN.vertex.z, 1.0));
                OUT.uv = uv;
                OUT.color = IN.color * _Color;

                return OUT;
            }

            float wrappedDist(float a, float b)
            {
                float d = abs(a - b);
                return min(d, 1.0 - d);
            }

            float band(float x, float center, float width, float sharpness)
            {
                float d = wrappedDist(x, center);
                float w = saturate(1.0 - d / max(width, 0.0001));
                return pow(w, sharpness);
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, IN.uv) * IN.color;

                if (c.a <= 0.001)
                    discard;

                float2 uv = IN.uv;
                float t = _Time.y * _AuroraSpeed;

                // 오로라처럼 부드럽게 흐르는 왜곡
                float wave1 = sin((uv.y * 1.7 + uv.x * 0.6) * _AuroraScale + t * 1.0);
                float wave2 = cos((uv.y * 2.3 - uv.x * 0.8) * (_AuroraScale * 0.85) - t * 1.3);
                float wave3 = sin((uv.x * 1.1 + uv.y * 1.9) * (_AuroraScale * 1.2) + t * 0.7);

                float distortion = (wave1 * 0.5 + wave2 * 0.35 + wave3 * 0.15) * _DistortionStrength;

                float flow = sin(uv.y * 2.0 + t * 1.4) * _FlowStrength
                           + cos(uv.x * 1.3 - t * 0.8) * (_FlowStrength * 0.5);

                // 이동 + 래핑되는 순환 phase
                float phase = frac(uv.x - t + distortion + flow);

                // 보라 -> 하이라이트 -> 파랑 -> 다시 보라
                float purpleCenter    = 0.00;
                float highlightCenter = frac(0.33 + _HighlightShift);
                float blueCenter      = 0.66;

                float wPurple    = band(phase, purpleCenter,    _CycleWidth, _CycleSharpness);
                float wHighlight = band(phase, highlightCenter, _CycleWidth * 0.85, _CycleSharpness + 0.4);
                float wBlue      = band(phase, blueCenter,      _CycleWidth, _CycleSharpness);

                float sumW = max(wPurple + wHighlight + wBlue, 0.0001);

                fixed3 auroraColor =
                    (_PurpleColor.rgb    * wPurple +
                     _HighlightColor.rgb * wHighlight +
                     _BlueColor.rgb      * wBlue) / sumW;

                // 하이라이트를 너무 띠처럼 보이지 않게 부드럽게
                float softPulse = sin((uv.y * 1.6 - uv.x * 0.7) * _AuroraScale + t * 0.9) * 0.5 + 0.5;
                float highlightMask = pow(saturate(wHighlight * (0.75 + softPulse * 0.25)), _HighlightSoftness);

                auroraColor = lerp(auroraColor, _HighlightColor.rgb, highlightMask * _HighlightAmount);

                float shimmer = sin((uv.x * 7.0 + uv.y * 5.0) + _Time.y * _ShimmerSpeed) * 0.5 + 0.5;
                shimmer *= _ShimmerStrength;

                fixed3 tinted = c.rgb * auroraColor * _AuroraBrightness;
                fixed3 finalRgb = lerp(c.rgb, tinted, _OverlayStrength);

                finalRgb += auroraColor * (highlightMask * 0.08 + shimmer * 0.05) * c.a;

                finalRgb *= c.a;
                return fixed4(finalRgb, c.a);
            }
            ENDCG
        }
    }
}
