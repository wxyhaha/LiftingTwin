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

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5
            #pragma instancing_options procedural:ConfigureProcedural

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // ── 点数据结构（与 C# 端 PointData 一一对应） ──
            struct PointData
            {
                float3 position;
                float size;
                float4 color;
            };

            StructuredBuffer<PointData> _PointBuffer;
            float _PointSize;

            // ── 实例化设置：每实例调用，设置变换矩阵 ──
            void ConfigureProcedural()
            {
                #if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
                    PointData data = _PointBuffer[unity_InstanceID];
                    float s = data.size * _PointSize;

                    // 构建变换矩阵：平移 + 统一缩放
                    unity_ObjectToWorld._m00_m01_m02 = float3(s, 0, 0);
                    unity_ObjectToWorld._m10_m11_m12 = float3(0, s, 0);
                    unity_ObjectToWorld._m20_m21_m22 = float3(0, 0, s);
                    unity_ObjectToWorld._m03_m13_m23 = data.position;
                    unity_ObjectToWorld._m33 = 1.0;

                    // 世界到物体的逆矩阵（简化处理，仅用于法线等）
                    float invS = 1.0 / s;
                    unity_WorldToObject._m00_m01_m02 = float3(invS, 0, 0);
                    unity_WorldToObject._m10_m11_m12 = float3(0, invS, 0);
                    unity_WorldToObject._m20_m21_m22 = float3(0, 0, invS);
                    unity_WorldToObject._m03_m13_m23 = -data.position * invS;
                    unity_WorldToObject._m33 = 1.0;
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
                    // 读取实例 ID 对应的点数据
                    PointData data = _PointBuffer[unity_InstanceID];
                    o.color = data.color;

                    // 使用标准变换管线（依赖 ConfigureProcedural 设置的矩阵）
                    o.positionCS = TransformObjectToHClip(v.vertex.xyz);
                #else
                    o.color = float4(1, 1, 1, 1);
                    o.positionCS = TransformObjectToHClip(v.vertex.xyz);
                #endif

                o.uv = v.vertex.xy + 0.5;
                return o;
            }

            // ── 片元着色器 ──
            float4 frag(Varyings i) : SV_Target
            {
                // 圆形裁剪（圆点而非方块）
                float2 uvOffset = i.uv - 0.5;
                if (dot(uvOffset, uvOffset) > 0.25)
                    discard;

                return i.color;
            }
            ENDHLSL
        }
    }
}
