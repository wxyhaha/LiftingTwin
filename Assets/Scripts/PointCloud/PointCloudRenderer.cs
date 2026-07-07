// -----------------------------------------------------------------------
// LiftingTwin - 点云渲染器（纯 C# 类）
//
// 职责：
//   管理 GPU ComputeBuffer 并驱动点云渲染。
//   使用 Graphics.DrawMeshInstancedIndirect 实现高性能绘制。
// -----------------------------------------------------------------------

using LiftingTwin.Utils;
using UnityEngine;
using UnityEngine.Rendering;

namespace LiftingTwin.PointCloud
{
    /// <summary>
    /// 点云渲染器。管理 GPU 点缓冲区，每帧执行实例化绘制。
    /// </summary>
    public class PointCloudRenderer
    {
        private readonly Material _material;
        private readonly MaterialPropertyBlock _props;
        private readonly UnityEngine.Mesh _quadMesh;
        private readonly int _maxPoints;

        private ComputeBuffer _pointBuffer;
        private ComputeBuffer _argsBuffer;
        private int _pointCount;
        private int _lastAllocatedCount;
        private Bounds _bounds;

        private static readonly int PointBufferId = Shader.PropertyToID("_PointBuffer");
        private static readonly int PointSizeId = Shader.PropertyToID("_PointSize");

        public int PointCount => _pointCount;
        public int MaxPoints => _maxPoints;
        public float PointSize { get; set; } = 0.5f;

        /// <summary>
        /// 创建点云渲染器。
        /// </summary>
        public PointCloudRenderer(Material material, int maxPoints = 1_000_000)
        {
            _material = material;
            _maxPoints = Mathf.Max(1, maxPoints);
            _quadMesh = CreateQuadMesh();
            _bounds = new Bounds(Vector3.zero, new Vector3(1000, 1000, 1000));
            _props = new MaterialPropertyBlock();

            int stride = System.Runtime.InteropServices.Marshal.SizeOf<PointData>();
            _pointBuffer = new ComputeBuffer(_maxPoints, stride);
            _pointBuffer.SetData(new PointData[_maxPoints]);

            _argsBuffer = new ComputeBuffer(1, 4 * sizeof(int), ComputeBufferType.IndirectArguments);

            Log.Info("PointCloud", "PointCloudRenderer 创建，最大点数={0}，stride={1}", _maxPoints, stride);
        }

        /// <summary>
        /// 更新点云数据。
        /// </summary>
        public void ApplyFrame(PointCloudFrame frame)
        {
            if (!frame.IsValid)
            {
                _pointCount = 0;
                return;
            }

            int count = Mathf.Min(frame.Positions.Length, _maxPoints);
            var data = new PointData[count];

            bool hasColors = frame.Colors != null && frame.Colors.Length >= count;
            bool hasSizes = frame.Sizes != null && frame.Sizes.Length >= count;

            for (int i = 0; i < count; i++)
            {
                data[i] = new PointData
                {
                    position = frame.Positions[i],
                    size = hasSizes ? frame.Sizes[i] : 1f,
                    color = hasColors ? frame.Colors[i] : Color.white
                };
            }

            _pointBuffer.SetData(data, 0, 0, count);
            _pointCount = count;

            Log.Debug("PointCloud", "更新点云：{0} 点", count);
        }

        /// <summary>
        /// 每帧调用，执行 GPU 实例化绘制。
        /// </summary>
        public void Render()
        {
            if (_pointCount == 0 || _material == null) return;

            // 使用 MaterialPropertyBlock 设置 shader 属性（兼容 URP SRP Batcher）
            _props.SetBuffer(PointBufferId, _pointBuffer);
            _props.SetFloat(PointSizeId, PointSize);

            if (_pointCount != _lastAllocatedCount)
            {
                var args = new int[4];
                args[0] = (int)_quadMesh.GetIndexCount(0);
                args[1] = _pointCount;
                args[2] = 0;
                args[3] = 0;
                _argsBuffer.SetData(args);
                _lastAllocatedCount = _pointCount;
            }

            Graphics.DrawMeshInstancedIndirect(
                _quadMesh, 0, _material, _bounds,
                _argsBuffer, 0, _props,
                ShadowCastingMode.Off, false
            );
        }

        /// <summary>
        /// 释放 GPU 资源。
        /// </summary>
        public void Release()
        {
            _pointBuffer?.Release();
            _pointBuffer = null;

            _argsBuffer?.Release();
            _argsBuffer = null;

            _pointCount = 0;
            Log.Info("PointCloud", "PointCloudRenderer 已释放");
        }

        /// <summary>
        /// 清空所有点。
        /// </summary>
        public void Clear()
        {
            _pointCount = 0;
        }

        private static UnityEngine.Mesh CreateQuadMesh()
        {
            var mesh = new UnityEngine.Mesh();
            mesh.name = "PointCloud Quad";
            mesh.vertices = new[]
            {
                new Vector3(-0.5f, -0.5f, 0),
                new Vector3(0.5f, -0.5f, 0),
                new Vector3(0.5f, 0.5f, 0),
                new Vector3(-0.5f, 0.5f, 0)
            };
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            mesh.uv = new[]
            {
                new Vector2(0, 0), new Vector2(1, 0),
                new Vector2(1, 1), new Vector2(0, 1)
            };
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// GPU 点数据结构，与 PointCloud.shader 中的 PointData 严格对应。
        /// </summary>
        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        private struct PointData
        {
            public Vector3 position;  // 12 bytes
            public float size;        // 4 bytes
            public Color color;       // 16 bytes
            // Total: 32 bytes
        }
    }
}
