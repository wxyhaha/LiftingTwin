Shader "LiftingTwin/PointCloud"
{
    Properties
    {
        _PointSize("Point Size", Float) = 0.05
        _MinPixelSize("Min Pixel Size", Float) = 2.0
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
            #pragma multi_compile_instancing
            #pragma instancing_options procedural:ConfigureProcedural
            #pragma multi_compile _ DOTS_INSTANCING_ON

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
            float _MinPixelSize;

            // ── 实例化设置 ──
            void ConfigureProcedural()
            {
                #if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
                    PointData data = _PointBuffer[unity_InstanceID];
                    float s = data.size * _PointSize;
                    unity_ObjectToWorld._m00_m01_m02 = float3(s, 0, 0);
                    unity_ObjectToWorld._m10_m11_m12 = float3(0, s, 0);
                    unity_ObjectToWorld._m20_m21_m22 = float3(0, 0, s);
                    unity_ObjectToWorld._m03_m13_m23 = data.position;
                #endif
            }

            // ── 顶点着色器输入 ──
            struct Attributes
            {
                float4 vertex : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            // ── 片元着色器输入 ──
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            // ── 顶点着色器 ──
            Varyings vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                #if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
                    PointData data = _PointBuffer[unity_InstanceID];

                    // 相机朝向公告板（billboard）
                    float3 right = UNITY_MATRIX_I_V._m00_m10_m20;
                    float3 up = UNITY_MATRIX_I_V._m01_m11_m21;
                    float size = data.size * _PointSize;

                    float3 worldPos = data.position
                        + right * v.vertex.x * size
                        + up * v.vertex.y * size;

                    o.positionCS = TransformWorldToHClip(worldPos);
                    o.color = data.color;

                    // UV 用于片元圆形裁剪
                    o.uv = v.vertex.xy + 0.5;
                #else
                    // 非实例化回退
                    float3 worldPos = TransformObjectToWorld(v.vertex.xyz);
                    o.positionCS = TransformWorldToHClip(worldPos);
                    o.color = float4(1, 1, 1, 1);
                    o.uv = v.vertex.xy + 0.5;
                #endif

                return o;
            }

            // ── 片元着色器 ──
            float4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);

                // 圆形裁剪：UV 中心距离 > 0.5 则丢弃（圆点而非方块）
                float2 uvOffset = i.uv - 0.5;
                float distSq = dot(uvOffset, uvOffset);
                if (distSq > 0.25)
                    discard;

                return i.color;
            }

            ENDHLSL
        }
    }
}
