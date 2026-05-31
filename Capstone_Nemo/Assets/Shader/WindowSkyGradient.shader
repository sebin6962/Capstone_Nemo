Shader "Custom/WindowSkyGradient"
{
    Properties
    {
        _TopColor ("Top Color", Color) = (0.4, 0.7, 1, 1)
        _BottomColor ("Bottom Color", Color) = (1, 0.8, 0.5, 1)
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _TopRatio ("Top Color Ratio", Range(0, 1)) = 0.7
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
            "PreviewType"="Plane"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            fixed4 _TopColor;
            fixed4 _BottomColor;
            float _TopRatio;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

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
                fixed4 texColor = tex2D(_MainTex, i.uv);

                float bottomRatio = 1.0 - _TopRatio;
                float gradientT = saturate(i.uv.y / max(bottomRatio, 0.0001));
                gradientT = smoothstep(0.0, 1.0, gradientT);

                fixed4 gradientColor = lerp(_BottomColor, _TopColor, gradientT);

                return gradientColor * texColor.a * i.color.a;
            }
            ENDCG
        }
    }
}
