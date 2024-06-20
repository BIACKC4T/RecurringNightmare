Shader "Hidden/Muse/ShapePreview"
{
    Properties
    {
        _MainTex ("Color (RGB) Alpha (A)", 2D) = "white"{}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Cull Off
        ZWrite Off
        ZTest Always

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
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            fixed4 _ReferenceColor;
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o, o.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, i.uv);
                fixed4 color = (c.r < 0.95 || c.g < 0.95 || c.b < 0.95) ? _ReferenceColor : c;

                #ifdef UNITY_COLORSPACE_GAMMA

                return color;

                #endif
                return fixed4(GammaToLinearSpace(color.rgb), color.a);
            }
            ENDCG
        }
    }
}