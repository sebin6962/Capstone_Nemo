Shader "Custom/SpriteCraftWiggle"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _Crafting ("Crafting", Float) = 0
        _MoveAmount ("Move Amount", Float) = 0.015
        _MoveSpeed ("Move Speed", Float) = 12
        _TopStart ("Top Start", Range(0, 1)) = 0.55
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
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            fixed4 _Color;

            float _Crafting;
            float _MoveAmount;
            float _MoveSpeed;
            float _TopStart;

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
            };

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                float2 uv = IN.texcoord;

                // 위쪽 영역만 움직이도록 마스크 생성
                float topMask = smoothstep(_TopStart, 1.0, uv.y);

                // 위아래 흔들림
                float wave = sin(_Time.y * _MoveSpeed) * _MoveAmount * _Crafting * topMask;

                // 화면상으로 위아래 움직이는 느낌
                uv.y -= wave;

                fixed4 c = tex2D(_MainTex, uv) * IN.color;
                return c;
            }
            ENDCG
        }
    }
}
