Shader "Custom/TreeTopPixelSwayLitURP2D"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
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
        _MotionTime ("Motion Time", Float) = 0

        [PerRendererData] _MaskTex("Mask", 2D) = "white" {}

        // SpriteRenderer 호환용 숨김 프로퍼티
        [HideInInspector] _RendererColor("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip("Flip", Vector) = (1,1,1,1)
        [HideInInspector] _AlphaTex("External Alpha", 2D) = "white" {}
        [HideInInspector] _EnableExternalAlpha("Enable External Alpha", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "RenderPipeline"="UniversalPipeline"
            "IgnoreProjector"="True"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha

        Pass
        {
            Name "Universal2D"
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #pragma vertex CombinedShapeLightVertex
            #pragma fragment CombinedShapeLightFragment
            #pragma multi_compile USE_SHAPE_LIGHT_TYPE_0 __
            #pragma multi_compile USE_SHAPE_LIGHT_TYPE_1 __
            #pragma multi_compile USE_SHAPE_LIGHT_TYPE_2 __
            #pragma multi_compile USE_SHAPE_LIGHT_TYPE_3 __
            #pragma multi_compile _ DEBUG_DISPLAY

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/LightingUtility.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;
                float4 color      : COLOR;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color       : COLOR;
                float2 uv         : TEXCOORD0;
                float4 screenPos  : TEXCOORD1;
                #if defined(DEBUG_DISPLAY)
                float3 positionWS : TEXCOORD2;
                #endif
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_MaskTex);
            SAMPLER(sampler_MaskTex);

            half4 _MainTex_ST;
            half4 _Color;
            half4 _RendererColor;

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

            half4 _PurpleColor;
            half4 _BlueColor;
            half4 _HighlightColor;

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
            float _MotionTime;

            #if USE_SHAPE_LIGHT_TYPE_0
            SHAPE_LIGHT(0)
            #endif
            #if USE_SHAPE_LIGHT_TYPE_1
            SHAPE_LIGHT(1)
            #endif
            #if USE_SHAPE_LIGHT_TYPE_2
            SHAPE_LIGHT(2)
            #endif
            #if USE_SHAPE_LIGHT_TYPE_3
            SHAPE_LIGHT(3)
            #endif

            float3 ApplySway(float3 positionOS, float2 uv)
            {
                float2 localPos = positionOS.xy;

                float mask = smoothstep(_TopStart, min(1.0, _TopStart + _Feather), uv.y);
                float top01 = saturate((uv.y - _TopStart) / max(0.0001, (1.0 - _TopStart)));
                float strength = mask * top01;

                float fromCenter = abs(uv.x - 0.5) * 2.0;
                float centerFactor = 1.0 - fromCenter;
                float edgeFactor = 1.0 + fromCenter * _EdgeBoost;

                float sideProtect = smoothstep(0.0, 0.12, uv.x) * (1.0 - smoothstep(0.88, 1.0, uv.x));

                float leftMask = 1.0 - smoothstep(0.28, 0.48, uv.x);
                float rightMask = smoothstep(0.52, 0.72, uv.x);
                float centerMask = saturate(1.0 - leftMask - rightMask);

                float regionPhaseOffset =
                    leftMask * _RegionPhaseAmount +
                    centerMask * 0.0 +
                    rightMask * (-_RegionPhaseAmount * 0.92);

                float regionAmpMul =
                    leftMask * (1.0 + _RegionAmplitudeBoost * 0.35) +
                    centerMask * 0.96 +
                    rightMask * (1.0 + _RegionAmplitudeBoost * 0.40);

                float regionVerticalMul =
                    leftMask * (1.0 + _RegionVerticalBoost * 0.85) +
                    centerMask * (1.0 + _CenterLift * 0.15) +
                    rightMask * (1.0 + _RegionVerticalBoost);

                float t = _MotionTime * _Speed + _PhaseOffset;
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
                    verticalWave * _VerticalAmplitude * strength * (0.65 + lift) * regionVerticalMul -
                    abs(sway) * _VerticalSquash * strength;

                if (_UsePixelSnap > 0.5)
                {
                    float unitPerPixel = 1.0 / max(_PixelPerUnit, 1.0);
                    xOffset = round(xOffset / unitPerPixel) * unitPerPixel;
                    yOffset = round(yOffset / unitPerPixel) * unitPerPixel;
                }

                localPos.x += xOffset;
                localPos.y += yOffset;

                return float3(localPos, positionOS.z);
            }

            float WrappedDist(float a, float b)
            {
                float d = abs(a - b);
                return min(d, 1.0 - d);
            }

            float Band(float x, float center, float width, float sharpness)
            {
                float d = WrappedDist(x, center);
                float w = saturate(1.0 - d / max(width, 0.0001));
                return pow(w, sharpness);
            }

            Varyings CombinedShapeLightVertex(Attributes v)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                float3 animatedPos = ApplySway(v.positionOS, v.uv);

                o.positionCS = TransformObjectToHClip(animatedPos);
                #if defined(DEBUG_DISPLAY)
                o.positionWS = TransformObjectToWorld(animatedPos);
                #endif
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.screenPos = ComputeScreenPos(o.positionCS);
                o.color = v.color * _Color * _RendererColor;
                return o;
            }

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/CombinedShapeLightShared.hlsl"

            half4 CombinedShapeLightFragment(Varyings i) : SV_Target
            {
                half4 mainSample = i.color * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                if (mainSample.a <= 0.001)
                    discard;

                half4 mask = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, i.uv);

                float2 uv = i.uv;
                float t = _MotionTime * _AuroraSpeed;

                float wave1 = sin((uv.y * 1.7 + uv.x * 0.6) * _AuroraScale + t * 1.0);
                float wave2 = cos((uv.y * 2.3 - uv.x * 0.8) * (_AuroraScale * 0.85) - t * 1.3);
                float wave3 = sin((uv.x * 1.1 + uv.y * 1.9) * (_AuroraScale * 1.2) + t * 0.7);

                float distortion = (wave1 * 0.5 + wave2 * 0.35 + wave3 * 0.15) * _DistortionStrength;
                float flow = sin(uv.y * 2.0 + t * 1.4) * _FlowStrength
                           + cos(uv.x * 1.3 - t * 0.8) * (_FlowStrength * 0.5);

                float phase = frac(uv.x - t + distortion + flow);

                float purpleCenter = 0.00;
                float highlightCenter = frac(0.33 + _HighlightShift);
                float blueCenter = 0.66;

                float wPurple = Band(phase, purpleCenter, _CycleWidth, _CycleSharpness);
                float wHighlight = Band(phase, highlightCenter, _CycleWidth * 0.85, _CycleSharpness + 0.4);
                float wBlue = Band(phase, blueCenter, _CycleWidth, _CycleSharpness);

                float sumW = max(wPurple + wHighlight + wBlue, 0.0001);

                half3 auroraColor =
                    (_PurpleColor.rgb * wPurple +
                     _HighlightColor.rgb * wHighlight +
                     _BlueColor.rgb * wBlue) / sumW;

                float softPulse = sin((uv.y * 1.6 - uv.x * 0.7) * _AuroraScale + t * 0.9) * 0.5 + 0.5;
                float highlightMask = pow(saturate(wHighlight * (0.75 + softPulse * 0.25)), _HighlightSoftness);
                auroraColor = lerp(auroraColor, _HighlightColor.rgb, highlightMask * _HighlightAmount);

                float shimmer = sin((uv.x * 7.0 + uv.y * 5.0) + _MotionTime * _ShimmerSpeed) * 0.5 + 0.5;
                shimmer *= _ShimmerStrength;

                half3 baseRgb = mainSample.rgb;
                half3 tinted = baseRgb * auroraColor * _AuroraBrightness;
                half3 finalRgb = lerp(baseRgb, tinted, _OverlayStrength);
                finalRgb += auroraColor * (highlightMask * 0.08 + shimmer * 0.05) * mainSample.a;

                SurfaceData2D surfaceData;
                InputData2D inputData;
                InitializeSurfaceData(finalRgb, mainSample.a, mask, surfaceData);
                half2 lightingUV = i.screenPos.xy / i.screenPos.w;
                InitializeInputData(i.uv, lightingUV, inputData);

                return CombinedShapeLightShared(surfaceData, inputData);
            }
            ENDHLSL
        }

        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode"="UniversalForward" "Queue"="Transparent" "RenderType"="Transparent" }

            HLSLPROGRAM
            #pragma vertex UnlitVertex
            #pragma fragment UnlitFragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;
                float4 color      : COLOR;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 color      : COLOR;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_ST;
            float4 _Color;
            half4 _RendererColor;

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

            float3 ApplySwayForward(float3 positionOS, float2 uv)
            {
                float2 localPos = positionOS.xy;

                float mask = smoothstep(_TopStart, min(1.0, _TopStart + _Feather), uv.y);
                float top01 = saturate((uv.y - _TopStart) / max(0.0001, (1.0 - _TopStart)));
                float strength = mask * top01;

                float fromCenter = abs(uv.x - 0.5) * 2.0;
                float centerFactor = 1.0 - fromCenter;
                float edgeFactor = 1.0 + fromCenter * _EdgeBoost;

                float sideProtect = smoothstep(0.0, 0.12, uv.x) * (1.0 - smoothstep(0.88, 1.0, uv.x));
                float leftMask = 1.0 - smoothstep(0.28, 0.48, uv.x);
                float rightMask = smoothstep(0.52, 0.72, uv.x);
                float centerMask = saturate(1.0 - leftMask - rightMask);

                float regionPhaseOffset =
                    leftMask * _RegionPhaseAmount +
                    centerMask * 0.0 +
                    rightMask * (-_RegionPhaseAmount * 0.92);

                float regionAmpMul =
                    leftMask * (1.0 + _RegionAmplitudeBoost * 0.35) +
                    centerMask * 0.96 +
                    rightMask * (1.0 + _RegionAmplitudeBoost * 0.40);

                float regionVerticalMul =
                    leftMask * (1.0 + _RegionVerticalBoost * 0.85) +
                    centerMask * (1.0 + _CenterLift * 0.15) +
                    rightMask * (1.0 + _RegionVerticalBoost);

                float t = _MotionTime * _Speed + _PhaseOffset;
                float phase = t + top01 * _HeightPhaseShift + regionPhaseOffset;

                float sway =
                    sin(phase) * (1.0 - _SecondWaveStrength) +
                    sin(phase * 0.67 + 0.8 + uv.x * _UvWaveInfluence) * _SecondWaveStrength;

                float xOffset = sway * _Amplitude * strength * edgeFactor * regionAmpMul * sideProtect;
                xOffset += sway * _BendStrength * strength * strength * sideProtect;

                float verticalPhase =
                    t * 1.15 + top01 * _VerticalPhaseShift + uv.x * _UvWaveInfluence + regionPhaseOffset * 1.25;

                float verticalWave =
                    sin(verticalPhase + 0.6) * 0.7 +
                    sin(verticalPhase * 1.73 + 1.2) * 0.3;

                float lift = centerFactor * _CenterLift;
                float yOffset =
                    verticalWave * _VerticalAmplitude * strength * (0.65 + lift) * regionVerticalMul -
                    abs(sway) * _VerticalSquash * strength;

                if (_UsePixelSnap > 0.5)
                {
                    float unitPerPixel = 1.0 / max(_PixelPerUnit, 1.0);
                    xOffset = round(xOffset / unitPerPixel) * unitPerPixel;
                    yOffset = round(yOffset / unitPerPixel) * unitPerPixel;
                }

                localPos.x += xOffset;
                localPos.y += yOffset;
                return float3(localPos, positionOS.z);
            }

            Varyings UnlitVertex(Attributes v)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                float3 animatedPos = ApplySwayForward(v.positionOS, v.uv);
                o.positionCS = TransformObjectToHClip(animatedPos);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color * _Color * _RendererColor;
                return o;
            }

            float4 UnlitFragment(Varyings i) : SV_Target
            {
                return i.color * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
            }
            ENDHLSL
        }
    }

    Fallback "Sprites/Default"
}
