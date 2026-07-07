// -----------------------------------------------------------------------
// LiftingTwin - 动态网格管理器（纯 C# 类）
//
// 职责：
//   管理一个 Unity Mesh 对象的创建与动态更新。
//   不依赖 MonoBehaviour，便于测试和复用。
//
// 用法：
//   var dynamicMesh = new DynamicMesh();
//   dynamicMesh.UpdateFrame(frame);
//   meshFilter.sharedMesh = dynamicMesh.Mesh;
// -----------------------------------------------------------------------

using LiftingTwin.Utils;
using UnityEngine;

namespace LiftingTwin.Mesh
{
    /// <summary>
    /// 动态网格管理器。封装 Unity Mesh 的创建和更新逻辑。
    /// 支持每帧替换顶点/索引数据，标记为 MarkDynamic 以优化性能。
    /// </summary>
    public class DynamicMesh
    {
        private readonly UnityEngine.Mesh _mesh;

        /// <summary>
        /// 当前管理的 Unity Mesh 实例。
        /// </summary>
        public UnityEngine.Mesh Mesh => _mesh;

        /// <summary>
        /// 创建动态网格管理器，内部创建并标记为动态 Mesh。
        /// </summary>
        public DynamicMesh()
        {
            _mesh = new UnityEngine.Mesh();
            _mesh.MarkDynamic();
        }

        /// <summary>
        /// 用新帧数据更新网格。自动清空旧数据，计算法线和包围盒。
        /// 传入无效帧（无顶点或索引）时仅清空，不报错。
        /// </summary>
        /// <param name="frame">网格帧数据</param>
        public void UpdateFrame(MeshFrame frame)
        {
            _mesh.Clear();

            if (!frame.IsValid)
            {
                Log.Warn("Mesh", "DynamicMesh.UpdateFrame 收到无效帧，已清空网格");
                return;
            }

            _mesh.vertices = frame.Vertices;
            _mesh.triangles = frame.Triangles;

            if (frame.Normals != null)
                _mesh.normals = frame.Normals;

            if (frame.Uvs != null)
                _mesh.uv = frame.Uvs;

            if (frame.Colors != null)
                _mesh.colors = frame.Colors;

            // 没有提供法线时自动计算
            if (frame.Normals == null)
                _mesh.RecalculateNormals();

            _mesh.RecalculateBounds();
        }

        /// <summary>
        /// 清空网格数据，释放顶点缓冲区。
        /// </summary>
        public void Clear()
        {
            _mesh.Clear();
        }
    }
}
