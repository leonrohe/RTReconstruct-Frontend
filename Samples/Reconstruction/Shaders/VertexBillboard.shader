Shader "Custom/VertexBillboard"
{
    Properties
    {
        _MinPointSize ("Min Point Size", Float) = 5.0
        _MaxPointSize ("Max Point Size", Float) = 20.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag
            #pragma target 5.0

            #include "UnityCG.cginc"

            float _MinPointSize;
            float _MaxPointSize;

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
            };

            struct v2g
            {
                float4 clipPos : POSITION;
                float4 worldPos : TEXCOORD1;
                float4 color : COLOR;
            };

            struct g2f
            {
                float4 pos : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            v2g vert(appdata v)
            {
                v2g o;
                float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
                o.worldPos = worldPos;
                o.clipPos = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                return o;
            }

            [maxvertexcount(6)]
            void geom(point v2g input[1], inout TriangleStream<g2f> triStream)
            {
                float4 center = input[0].clipPos;
                float4 worldPos = input[0].worldPos;
                float4 color = input[0].color;

                // Get camera position in world space
                float3 cameraPos = _WorldSpaceCameraPos;
                float distToCam = distance(_WorldSpaceCameraPos, worldPos.xyz);

                // AR-tuned range
                float minDistance = 0.1;
                float maxDistance = 1.0;

                // Logarithmic interpolation
                float t = saturate(log2(distToCam / minDistance) / log2(maxDistance / minDistance));
                float pointSize = lerp(_MinPointSize, _MaxPointSize, 1.0 - t);

                // Convert size to NDC
                float2 screenSize = pointSize / _ScreenParams.xy;
                float2 size = screenSize * center.w;

                float4 right = float4(size.x, 0, 0, 0);
                float4 up = float4(0, size.y, 0, 0);

                float4 bl = center + (-right - up);
                float4 br = center + ( right - up);
                float4 tr = center + ( right + up);
                float4 tl = center + (-right + up);

                g2f o;
                o.color = color;

                // Triangle 1
                o.pos = bl; o.uv = float2(0, 0); triStream.Append(o);
                o.pos = br; o.uv = float2(1, 0); triStream.Append(o);
                o.pos = tr; o.uv = float2(1, 1); triStream.Append(o);

                // Triangle 2
                o.pos = bl; o.uv = float2(0, 0); triStream.Append(o);
                o.pos = tl; o.uv = float2(0, 1); triStream.Append(o);
                o.pos = tr; o.uv = float2(1, 1); triStream.Append(o);
            }

            fixed4 frag(g2f i) : SV_Target
            {
                return i.color;
            }
            ENDCG
        }
    }
}
