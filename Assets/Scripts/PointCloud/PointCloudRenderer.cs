// -----------------------------------------------------------------------
// LiftingTwin - 点云渲染器（纯 C# 类）
//
// 职责：
//   管理 GPU ComputeBuffer 并使用 Graphics.DrawProcedural 渲染。
//   每帧提交一个公告板四边形实例，SV_InstanceID 索引到点数据。
// -----------------------------------------------------------------------

using LiftingTwin.Utils;
using UnityEngine;
using UnityEngine.Rendering;

namespace LiftingTwin.PointCloud
{
    public class PointCloudRenderer
    {
        private readonly Material _material;
        private readonly int _maxPoints;
        private ComputeBuffer _pointBuffer;
        private int _pointCount;
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
            // 为每个渲染器创建材质实例，避免 SRP Batcher 冲突
            _material = Object.Instantiate(material);
            _material.enableInstancing = false; // DrawProcedural 不需要 instancing keyword
            _maxPoints = Mathf.Max(1, maxPoints);
            _bounds = new Bounds(Vector3.zero, new Vector3(1000, 1000, 1000));

            int stride = System.Runtime.InteropServices.Marshal.SizeOf<PointData>();
            _pointBuffer = new ComputeBuffer(_maxPoints, stride);
            _pointBuffer.SetData(new PointData[_maxPoints]);

            Log.Info("PointCloud", "PointCloudRenderer 创建，最大={0}点，stride={1}B", _maxPoints, stride);
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
        /// 每帧调用，提交绘制。
        /// 使用 DrawProcedural: 每个实例 6 顶点（2 三角形 = 公告板四边形）。
        /// </summary>
        public void Render()
        {
            if (_pointCount == 0 || _material == null) return;

            _material.SetBuffer(PointBufferId, _pointBuffer);
            _material.SetFloat(PointSizeId, PointSize);

            Graphics.DrawProcedural(
                _material, _bounds,
                MeshTopology.Triangles,
                6,              // 每个实例 6 顶点（2 三角形）
                _pointCount     // 实例数 = 点数
            );
        }

        public void Release()
        {
            _pointBuffer?.Release();
            _pointBuffer = null;
            _pointCount = 0;
            if (_material != null) Object.Destroy(_material);
            Log.Info("PointCloud", "PointCloudRenderer 已释放");
        }

        public void Clear() => _pointCount = 0;

        /// <summary>
        /// GPU 点数据结构，与 PointCloud.shader 严格对应。
        /// </summary>
        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        private struct PointData
        {
            public Vector3 position;  // 12B
            public float size;        // 4B
            public Color color;       // 16B
            // Total: 32B
        }
    }
}
