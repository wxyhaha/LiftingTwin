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

            // 关闭背面剔除 — 点云四边形从任何角度都要可见
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5
            #pragma instancing_options procedural:ConfigureProcedural

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

            // ── 实例化设置：设置点位置和大小 ──
            void ConfigureProcedural()
            {
                #if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
                    PointData data = _PointBuffer[unity_InstanceID];
                    float s = data.size * _PointSize;

                    // 仅平移 + 统一缩放（不旋转，四边形朝向由顶点着色器处理）
                    unity_ObjectToWorld._m00_m01_m02 = float3(s, 0, 0);
                    unity_ObjectToWorld._m10_m11_m12 = float3(0, s, 0);
                    unity_ObjectToWorld._m20_m21_m22 = float3(0, 0, s);
                    unity_ObjectToWorld._m03_m13_m23 = data.position;
                    unity_ObjectToWorld._m33 = 1.0;
                #endif
            }

            struct Attributes
            {
                float4 vertex : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
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

                #if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
                    PointData data = _PointBuffer[unity_InstanceID];
                    float s = data.size * _PointSize;

                    // 顶点在世界空间中的位置 = 点位置 + 相机朝向的公告板偏移
                    float3 worldPos = data.position;
                    float3 right = UNITY_MATRIX_I_V._m00_m10_m20; // 相机右向量
                    float3 up = UNITY_MATRIX_I_V._m01_m11_m21;    // 相机上向量

                    worldPos += right * v.vertex.x * s;
                    worldPos += up * v.vertex.y * s;

                    o.positionCS = TransformWorldToHClip(worldPos);
                    o.color = data.color;
                #else
                    o.positionCS = TransformWorldToHClip(
                        mul(unity_ObjectToWorld, v.vertex).xyz);
                    o.color = float4(1, 1, 1, 1);
                #endif

                o.uv = v.vertex.xy + 0.5;
                return o;
            }

            // ── 片元着色器 ──
            float4 frag(Varyings i) : SV_Target
            {
                // 圆形裁剪：只显示圆内的像素
                float2 uvOffset = i.uv - 0.5;
                if (dot(uvOffset, uvOffset) > 0.25)
                    discard;

                return i.color;
            }
            ENDHLSL
        }
    }
}
