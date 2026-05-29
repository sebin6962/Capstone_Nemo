Shader "Custom/UI/HighlightSweep_Exact"
{
    Properties
    {
        [PerRendererData] _MainTex ("_MainTex", 2D) = "white" {}

        _Color ("Tint", Color) = (1,1,1,1)

        // 기존 ShaderGraph의 오타 이름 그대로 유지해야 기존 Material 값이 유지됨
        _HightlightColor ("HightlightColor", Color) = (1,0.9356083,0.6698112,0)
        _SweepSpeed ("SweepSpeed", Range(0, 1)) = 1
        _SweepStrength ("SweepStrength", Range(0, 1)) = 0.2
        _SweepWidth ("SweepWidth", Range(0, 1)) = 0.2
        _SweepCenter ("SweepCenter", Range(0, 1)) = 0.5
        _EdgeSoftness ("EdgeSoftness", Range(0, 1)) = 0.05

        [HideInInspector] _TextureSampleAdd ("Texture Sample Add", Vector) = (0,0,0,0)

        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            sampler2D _MainTex;
            fixed4 _TextureSampleAdd;
            fixed4 _Color;
            fixed4 _HightlightColor;

            float _SweepSpeed;
            float _SweepStrength;
            float _SweepWidth;
            float _SweepCenter;
            float _EdgeSoftness;

            float4 _ClipRect;

            struct appdata_t
            {
                float4 vertex : POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
            };

            v2f vert(appdata_t v)
            {
                v2f OUT;
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(v.vertex);
                OUT.texcoord = v.texcoord;
                OUT.color = v.color * _Color;
                return OUT;
            }

            // ShaderGraph Rotate Node와 같은 방식
            float2 RotateRadians(float2 UV, float2 Center, float Rotation)
            {
                UV -= Center;

                float s = sin(Rotation);
                float c = cos(Rotation);

                float2x2 rMatrix = float2x2(c, -s, s, c);
                rMatrix *= 0.5;
                rMatrix += 0.5;
                rMatrix = rMatrix * 2 - 1;

                UV.xy = mul(UV.xy, rMatrix);
                UV += Center;

                return UV;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 tex = tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd;

                // 기존 ShaderGraph: UV를 -20 회전
                float2 rotatedUV = RotateRadians(IN.texcoord, float2(0.5, 0.5), -20.0);

                // 기존 ShaderGraph: rotated UV.x + Time * SweepSpeed 후 Fraction
                float sweepPos = frac(rotatedUV.x + (_Time.y * _SweepSpeed));

                float halfWidth = _SweepWidth * 0.5;

                float left = _SweepCenter - halfWidth;
                float right = _SweepCenter + halfWidth;

                float leftSmooth = smoothstep(
                    left - _EdgeSoftness,
                    left + _EdgeSoftness,
                    sweepPos
                );

                float rightSmooth = smoothstep(
                    right - _EdgeSoftness,
                    right + _EdgeSoftness,
                    sweepPos
                );

                // 기존 ShaderGraph: Smoothstep1 - Smoothstep2
                float sweepMask = leftSmooth - rightSmooth;

                // 기존 ShaderGraph: HightlightColor * SweepStrength * mask
                float3 highlight = _HightlightColor.rgb * _SweepStrength * sweepMask;

                // 기존 ShaderGraph: Sample Texture + Highlight
                fixed4 col;
                col.rgb = (tex.rgb * IN.color.rgb) + highlight;
                col.a = tex.a * IN.color.a;

                #ifdef UNITY_UI_CLIP_RECT
                col.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(col.a - 0.001);
                #endif

                return col;
            }
            ENDCG
        }
    }
}