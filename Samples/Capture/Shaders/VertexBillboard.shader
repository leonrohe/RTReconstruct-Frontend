Shader "Custom/VertexBillboard"
{
    Properties
    {
        _PointSize ("Point Size", Float) = 10.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            // Enable geometry shader
            CGPROGRAM
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag
            #pragma target 5.0

            #include "UnityCG.cginc"

            float _PointSize;

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
            };

            struct v2g
            {
                float4 pos : POSITION;
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
                o.pos = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                return o;
            }

            [maxvertexcount(4)]
            void geom(point v2g input[1], inout TriangleStream<g2f> triStream)
            {
                float4 center = input[0].pos;
                float4 color = input[0].color;

                // Get size in clip space
                float size = _PointSize;

                // Compute right and up vectors in clip space (screen aligned)
                float4 right = float4(size, 0, 0, 0);
                float4 up = float4(0, size, 0, 0);

                // Build four vertices of the quad (triangle strip or two triangles)
                g2f o;

                // Bottom-left
                o.pos = center + (-right - up);
                o.color = color;
                o.uv = float2(0, 0);
                triStream.Append(o);

                // Bottom-right
                o.pos = center + (right - up);
                o.color = color;
                o.uv = float2(1, 0);
                triStream.Append(o);

                // Top-right
                o.pos = center + (right + up);
                o.color = color;
                o.uv = float2(1, 1);
                triStream.Append(o);

                // Top-left
                o.pos = center + (-right + up);
                o.color = color;
                o.uv = float2(0, 1);
                triStream.Append(o);

                triStream.RestartStrip();
            }

            fixed4 frag(g2f i) : SV_Target
            {
                return i.color;
            }
            ENDCG
        }
    }
}
