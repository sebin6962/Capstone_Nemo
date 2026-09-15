Shader "Custom/WhiteColorReplaceLit"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}

        _ReplaceColor ("Replace Color", Color) = (1,1,1,1)

        _WhiteThreshold ("White Threshold", Range(0, 1)) = 0.85
        _BlendRange ("Blend Range", Range(0.001, 0.3)) = 0.05

        _ShadowSourceColor ("Original Shadow Color", Color) = (0.6,0.6,0.6,1)
        _ShadowColorTolerance ("Shadow Color Tolerance", Range(0.001, 0.5)) = 0.05
        _ShadowDarkness ("Shadow Darkness", Range(0, 1)) = 0.7

        [HideInInspector] _Color ("Tint", Color) = (1,1,1,1)
        [HideInInspector] _RendererColor ("Renderer Color", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)

        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "CanUseSpriteAtlas" = "True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            Tags
            {
                "LightMode" = "Universal2D"
            }

            HLSLPROGRAM

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #pragma vertex CombinedShapeLightVertex
            #pragma fragment CombinedShapeLightFragment

            #pragma multi_compile USE_SHAPE_LIGHT_TYPE_0 __
            #pragma multi_compile USE_SHAPE_LIGHT_TYPE_1 __
            #pragma multi_compile USE_SHAPE_LIGHT_TYPE_2 __
            #pragma multi_compile USE_SHAPE_LIGHT_TYPE_3 __
            #pragma multi_compile _ DEBUG_DISPLAY

            struct Attributes
            {
                float3 positionOS : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
                float2 lightingUV : TEXCOORD1;

                #if defined(DEBUG_DISPLAY)
                float3 positionWS : TEXCOORD2;
                #endif

                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            TEXTURE2D(_MaskTex);
            SAMPLER(sampler_MaskTex);

            float4 _MainTex_ST;

            half4 _Color;
            half4 _RendererColor;

            half4 _ReplaceColor;

            float _WhiteThreshold;
            float _BlendRange;

            half4 _ShadowSourceColor;
            float _ShadowColorTolerance;
            float _ShadowDarkness;

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/LightingUtility.hlsl"

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

            Varyings CombinedShapeLightVertex(Attributes v)
            {
                Varyings o = (Varyings)0;

                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.positionCS = TransformObjectToHClip(v.positionOS);

                #if defined(DEBUG_DISPLAY)
                o.positionWS = TransformObjectToWorld(v.positionOS);
                #endif

                o.uv = TRANSFORM_TEX(v.uv, _MainTex);

                float4 screenPos = ComputeScreenPos(o.positionCS);
                o.lightingUV = screenPos.xy / screenPos.w;

                o.color = v.color * _Color;

                return o;
            }

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/CombinedShapeLightShared.hlsl"

            half4 CombinedShapeLightFragment(Varyings i) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(
                    _MainTex,
                    sampler_MainTex,
                    i.uv
                );

                tex *= i.color;

                half3 baseRGB = tex.rgb;

                // -----------------------------
                // 흰색 영역 판정
                // -----------------------------
                float minRGB = min(
                    baseRGB.r,
                    min(baseRGB.g, baseRGB.b)
                );

                float whiteMask = smoothstep(
                    _WhiteThreshold - _BlendRange,
                    _WhiteThreshold,
                    minRGB
                );

                // -----------------------------
                // 지정한 그림자색 판정
                // -----------------------------
                half3 shadowDiff = abs(
                    baseRGB - _ShadowSourceColor.rgb
                );

                float maxShadowDiff = max(
                    shadowDiff.r,
                    max(shadowDiff.g, shadowDiff.b)
                );

                float shadowMask =
                    1.0 - smoothstep(
                        _ShadowColorTolerance,
                        _ShadowColorTolerance + 0.02,
                        maxShadowDiff
                    );

                // 흰색 영역은 그림자로 중복 처리하지 않음
                shadowMask *= (1.0 - whiteMask);

                // -----------------------------
                // 목표 색상
                // -----------------------------
                half3 whiteTargetRGB =
                    _ReplaceColor.rgb;

                half3 shadowTargetRGB =
                    _ReplaceColor.rgb * _ShadowDarkness;

                // -----------------------------
                // 그림자 적용
                // -----------------------------
                half3 resultRGB = lerp(
                    baseRGB,
                    shadowTargetRGB,
                    shadowMask
                );

                // -----------------------------
                // 흰색 적용
                // -----------------------------
                resultRGB = lerp(
                    resultRGB,
                    whiteTargetRGB,
                    whiteMask
                );

                half4 main = half4(
                    resultRGB,
                    tex.a
                );

                half4 mask = SAMPLE_TEXTURE2D(
                    _MaskTex,
                    sampler_MaskTex,
                    i.uv
                );

                SurfaceData2D surfaceData;
                InputData2D inputData;

                InitializeSurfaceData(
                    main.rgb,
                    main.a,
                    mask,
                    surfaceData
                );

                InitializeInputData(
                    i.uv,
                    i.lightingUV,
                    inputData
                );

                return CombinedShapeLightShared(
                    surfaceData,
                    inputData
                );
            }

            ENDHLSL
        }
    }

    Fallback "Universal Render Pipeline/2D/Sprite-Lit-Default"
}