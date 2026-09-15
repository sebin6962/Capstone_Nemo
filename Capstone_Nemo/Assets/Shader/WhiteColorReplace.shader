Shader "Custom/WhiteColorReplace"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}

        _ReplaceColor ("Replace Color", Color) = (1,1,1,1)

        // 어느 정도 밝아야 흰색으로 판단할지
        _WhiteThreshold ("White Threshold", Range(0, 1)) = 0.85

        // 경계를 얼마나 부드럽게 할지
        _BlendRange ("Blend Range", Range(0.001, 0.3)) = 0.05
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

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            sampler2D _MainTex;

            float4 _ReplaceColor;
            float _WhiteThreshold;
            float _BlendRange;

            v2f vert(appdata v)
            {
                v2f o;

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 tex = tex2D(_MainTex, i.uv);

                // 완전히 투명한 부분 처리
                if (tex.a <= 0)
                    return fixed4(0, 0, 0, 0);

                // RGB 중 가장 어두운 값 사용.
                // R/G/B가 모두 밝아야 흰색으로 판단됨.
                float whiteness = min(tex.r, min(tex.g, tex.b));

                float mask = smoothstep(
                    _WhiteThreshold - _BlendRange,
                    _WhiteThreshold,
                    whiteness
                );

                fixed3 resultColor =
                    lerp(tex.rgb, _ReplaceColor.rgb, mask);

                fixed4 result =
                    fixed4(resultColor, tex.a);

                // SpriteRenderer alpha 대응
                result *= i.color;

                result.rgb *= result.a;

                return result;
            }

            ENDCG
        }
    }
}