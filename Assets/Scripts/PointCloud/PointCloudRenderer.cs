// -----------------------------------------------------------------------
// LiftingTwin - 点云渲染器（纯 C# 类）
//
// 职责：
//   管理 GPU ComputeBuffer 并驱动点云渲染。
//   使用 Graphics.DrawMeshInstancedIndirect 实现高性能绘制。
//   不依赖 MonoBehaviour，便于测试和复用。
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
        private readonly UnityEngine.Mesh _quadMesh;
        private readonly int _maxPoints;

        private ComputeBuffer _pointBuffer;
        private ComputeBuffer _argsBuffer;
        private int _pointCount;
        private int _lastAllocatedCount;
        private Bounds _bounds;

        // Shader 属性 ID
        private static readonly int PointBufferId = Shader.PropertyToID("_PointBuffer");
        private static readonly int PointSizeId = Shader.PropertyToID("_PointSize");

        /// <summary>
        /// 当前点数量。
        /// </summary>
        public int PointCount => _pointCount;

        /// <summary>
        /// 最大容量。
        /// </summary>
        public int MaxPoints => _maxPoints;

        /// <summary>
        /// 点大小。
        /// </summary>
        public float PointSize { get; set; } = 0.05f;

        /// <summary>
        /// 创建点云渲染器。
        /// </summary>
        /// <param name="material">使用 PointCloud.shader 的材质</param>
        /// <param name="maxPoints">最大点数量（预先分配 GPU 缓冲区）</param>
        public PointCloudRenderer(Material material, int maxPoints = 1_000_000)
        {
            _material = material;
            _maxPoints = Mathf.Max(1, maxPoints);
            _quadMesh = CreateQuadMesh();
            _bounds = new Bounds(Vector3.zero, new Vector3(1000, 1000, 1000));

            // 预先分配 GPU 缓冲区
            int stride = System.Runtime.InteropServices.Marshal.SizeOf<PointData>();
            _pointBuffer = new ComputeBuffer(_maxPoints, stride);
            _pointBuffer.SetData(new PointData[_maxPoints]); // 清零

            // 间接绘制参数缓冲区
            _argsBuffer = new ComputeBuffer(1, 4 * sizeof(int), ComputeBufferType.IndirectArguments);

            Log.Info("PointCloud", "PointCloudRenderer 创建，最大点数={0}", _maxPoints);
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

            bool hasColors = frame.Colors is { Length: >= count };
            bool hasSizes = frame.Sizes is { Length: >= count };

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

            // 更新包围盒
            RecalculateBounds(frame.Positions, count);

            Log.Debug("PointCloud", "更新点云：{0} 点", count);
        }

        /// <summary>
        /// 每帧调用，执行 GPU 实例化绘制。
        /// 需要在摄像机可见范围内调用（通常在 OnRenderObject / Update 中）。
        /// </summary>
        public void Render()
        {
            if (_pointCount == 0 || _material == null) return;

            _material.SetBuffer(PointBufferId, _pointBuffer);
            _material.SetFloat(PointSizeId, PointSize);

            // 仅在点数变化时更新 args buffer
            if (_pointCount != _lastAllocatedCount)
            {
                var args = new int[4];
                args[0] = _quadMesh.GetIndexCount(0);
                args[1] = _pointCount;
                args[2] = 0;
                args[3] = 0;
                _argsBuffer.SetData(args);
                _lastAllocatedCount = _pointCount;
            }

            Graphics.DrawMeshInstancedIndirect(
                _quadMesh, 0, _material, _bounds,
                _argsBuffer, 0,
                null, ShadowCastingMode.Off, false
            );
        }

        /// <summary>
        /// 释放 GPU 资源。
        /// </summary>
        public void Release()
        {
            if (_pointBuffer != null)
            {
                _pointBuffer.Release();
                _pointBuffer = null;
            }
            if (_argsBuffer != null)
            {
                _argsBuffer.Release();
                _argsBuffer = null;
            }
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

        // ── 内部 ──

        /// <summary>
        /// 创建用于点精灵的公告板网格（相机朝向的四边形）。
        /// </summary>
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
                new Vector2(0, 0),
                new Vector2(1, 0),
                new Vector2(1, 1),
                new Vector2(0, 1)
            };

            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// 根据点位置重新计算包围盒。
        /// </summary>
        private static void RecalculateBounds(Vector3[] positions, int count)
        {
            // 由 PointCloudView 管理包围盒更新，此处简化处理
        }

        /// <summary>
        /// GPU 点数据结构，与 shader 中的 PointData 一一对应。
        /// </summary>
        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        private struct PointData
        {
            public Vector3 position;
            public float size;
            public Color color;
        }
    }
}
