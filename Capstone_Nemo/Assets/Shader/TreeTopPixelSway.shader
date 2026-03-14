Shader "Unlit/TreeTopPixelSway"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _Amplitude ("Horizontal Amplitude", Range(0, 0.2)) = 0.03
        _VerticalAmplitude ("Vertical Wave Amplitude", Range(0, 0.15)) = 0.015
        _Speed ("Speed", Range(0, 4)) = 1.0
        _TopStart ("Top Start (UV Y)", Range(0, 1)) = 0.42
        _Feather ("Feather", Range(0.01, 0.5)) = 0.25
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

                // 3-way smooth regional masks
                float leftMask  = 1.0 - smoothstep(0.28, 0.48, uv.x);
                float rightMask = smoothstep(0.52, 0.72, uv.x);
                float centerMask = saturate(1.0 - leftMask - rightMask);

                // Left / center / right move at slightly different timing
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

            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, IN.uv) * IN.color;
                return c;
            }
            ENDCG
        }
    }
}
