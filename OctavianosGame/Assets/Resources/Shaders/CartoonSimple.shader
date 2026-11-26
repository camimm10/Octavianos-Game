// Shader sencillo de dibujos animados para Octavio en la RA del móvil.
// No depende de las variantes de URP/Lit (que en algunos móviles no se dibujaban):
// luz suave en dos tonos, un brillo y un poco de borde iluminado. Usa _BaseColor y _EmissionColor,
// así OctavioSkinColor lo sigue tiñendo igual.
Shader "Octavianos/CartoonSimple"
{
    Properties
    {
        _BaseColor ("Color", Color) = (1, 1, 1, 1)
        _BaseMap ("Textura", 2D) = "white" {}
        _EmissionColor ("Luz propia", Color) = (0, 0, 0, 1)
        _Cull ("Cull", Float) = 2
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }
        Pass
        {
            Cull [_Cull]
            ZWrite On
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float4 _BaseColor;
            float4 _EmissionColor;
            sampler2D _BaseMap;
            float4 _BaseMap_ST;

            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float3 n : TEXCOORD0; float2 uv : TEXCOORD1; float3 vista : TEXCOORD2; };

            v2f vert (appdata i)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(i.vertex);
                o.n = UnityObjectToWorldNormal(i.normal);
                o.uv = TRANSFORM_TEX(i.uv, _BaseMap);
                o.vista = WorldSpaceViewDir(i.vertex);
                return o;
            }

            fixed4 frag (v2f i, fixed cara : VFACE) : SV_Target
            {
                float3 n = normalize(i.n) * (cara > 0 ? 1 : -1);
                float3 v = normalize(i.vista);
                float3 l = normalize(float3(0.35, 0.85, -0.4));
                float d = saturate(dot(n, l));
                float tono = 0.58 + 0.42 * smoothstep(0.05, 0.55, d);          // dos tonos suaves
                float3 col = _BaseColor.rgb * tex2D(_BaseMap, i.uv).rgb * tono;
                float3 h = normalize(l + v);
                float brillo = pow(saturate(dot(n, h)), 48) * 0.45;
                float borde = pow(1 - saturate(dot(n, v)), 3) * 0.18;
                return fixed4(col + _EmissionColor.rgb + brillo + borde, 1);
            }
            ENDCG
        }
    }
}
