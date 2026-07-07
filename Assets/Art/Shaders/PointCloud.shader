Shader "LiftingTwin/PointCloud"
{
    Properties
    {
        _PointSize("Point Size", Float) = 0.5
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // ── 点数据结构 ──
            struct PointData
            {
                float3 position;
                float size;
                float4 color;
            };

            StructuredBuffer<PointData> _PointBuffer;
            float _PointSize;

            // ── 顶点着色器输入 ──
            // DrawProcedural 不传 vertex 数据，只用 SV_VertexID / SV_InstanceID
            struct Attributes
            {
                uint vertexID : SV_VertexID;
                uint instanceID : SV_InstanceID;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            // ── 顶点着色器 ──
            Varyings vert(Attributes v)
            {
                Varyings o;

                // 从 ComputeBuffer 读取当前点数据
                PointData data = _PointBuffer[v.instanceID];

                // 公告板四边形：6 个顶点构成 2 个三角形
                // Tri 1: 0(-1,-1) 1(1,-1) 2(1,1)
                // Tri 2: 3(-1,-1) 4(1,1)  5(-1,1)
                float cornerX = (v.vertexID == 1 || v.vertexID == 2 || v.vertexID == 4) ? 1.0 : -1.0;
                float cornerY = (v.vertexID == 2 || v.vertexID == 4 || v.vertexID == 5) ? 1.0 : -1.0;

                float s = data.size * _PointSize;

                // 公告板：始终面向摄像机
                float3 right = UNITY_MATRIX_I_V._m00_m10_m20;
                float3 up = UNITY_MATRIX_I_V._m01_m11_m21;

                float3 worldPos = data.position + right * cornerX * s + up * cornerY * s;
                o.positionCS = TransformWorldToHClip(worldPos);
                o.color = data.color;

                // UV 从 [-1,1] 映射到 [0,1]
                o.uv = float2((cornerX + 1.0) * 0.5, (cornerY + 1.0) * 0.5);

                return o;
            }

            // ── 片元着色器 ──
            float4 frag(Varyings i) : SV_Target
            {
                // 圆形裁剪
                float2 uvOffset = i.uv - 0.5;
                if (dot(uvOffset, uvOffset) > 0.25)
                    discard;

                return i.color;
            }
            ENDHLSL
        }
    }
}
