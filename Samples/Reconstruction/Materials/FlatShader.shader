Shader "Custom/FlatUnlit"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
            };

            fixed4 _Color;

            v2f vert (appdata v)
            {
                v2f o;
                float4 worldPos4 = mul(unity_ObjectToWorld, v.vertex);
                o.worldPos = worldPos4.xyz;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 dpdx = ddx(i.worldPos);
                float3 dpdy = ddy(i.worldPos);
                float3 faceNormal = normalize(cross(dpdx, dpdy));

                // Fake-Licht aus Kamerarichtung, damit Flächen sich abheben
                float3 viewDir = normalize(_WorldSpaceCameraPos - i.worldPos);
                float NdotV = abs(dot(faceNormal, viewDir));

                // Immer mindestens etwas hell, z. B. 0.2
                float brightness = max(0.2, NdotV);

                return fixed4(_Color.rgb * brightness, 1.0);
            }
            ENDCG
        }
    }
}
