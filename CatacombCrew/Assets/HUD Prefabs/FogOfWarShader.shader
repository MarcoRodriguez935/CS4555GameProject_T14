Shader "UI/FogOfWarShader"
{
    Properties
    {
        _RevealMask("Reveal Mask", 2D) = "white" {}
        _FogColor("Fog Color", Color) = (0,0,0,1)
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off ZWrite Off ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "UIFog"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _RevealMask;
            fixed4 _FogColor;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Sample reveal texture (black = fog, white = revealed)
                float reveal = tex2D(_RevealMask, i.uv).r;

                // Base fog color
                fixed4 col = _FogColor;

                // When reveal = 0 (black), fog = opaque.
                // When reveal = 1 (white), fog = transparent.
                col.a = 1 - reveal;

                return col;
            }
            ENDCG
        }
    }
}