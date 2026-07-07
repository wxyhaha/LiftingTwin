// -----------------------------------------------------------------------
// LiftingTwin - 网格帧数据结构
//
// 用途：
//   承载一个完整网格的顶点、索引及可选属性数据。
//   网络模块或模拟器产生 MeshFrame，MeshRenderer 消费。
//
// 设计原则：
//   纯数据容器，不包含逻辑。
//   所有字段可为 null，消费方按需取用。
// -----------------------------------------------------------------------

using UnityEngine;

namespace LiftingTwin.Mesh
{
    /// <summary>
    /// 网格帧数据，包含渲染一个网格所需的所有顶点数据。
    /// 字段为 null 时表示该通道无数据。
    /// </summary>
    public struct MeshFrame
    {
        /// <summary>
        /// 顶点位置数组。
        /// </summary>
        public Vector3[] Vertices { get; set; }

        /// <summary>
        /// 三角形索引数组。
        /// </summary>
        public int[] Triangles { get; set; }

        /// <summary>
        /// 法线数组（可选）。为 null 时由渲染器自动计算。
        /// </summary>
        public Vector3[] Normals { get; set; }

        /// <summary>
        /// UV 坐标数组（可选）。
        /// </summary>
        public Vector2[] Uvs { get; set; }

        /// <summary>
        /// 顶点颜色数组（可选）。
        /// </summary>
        public Color[] Colors { get; set; }

        /// <summary>
        /// 是否有有效的网格数据（至少包含顶点和索引）。
        /// </summary>
        public bool IsValid => Vertices is { Length: > 0 }
                            && Triangles is { Length: > 0 };
    }
}
