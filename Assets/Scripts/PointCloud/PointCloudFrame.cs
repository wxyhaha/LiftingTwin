// -----------------------------------------------------------------------
// LiftingTwin - 点云帧数据结构
//
// 用途：
//   承载一帧点云数据，包含每个点的位置、颜色和大小。
//   字段为 null 时表示该通道无数据。
// -----------------------------------------------------------------------

using UnityEngine;

namespace LiftingTwin.PointCloud
{
    /// <summary>
    /// 点云帧数据。包含所有点的位置和可选颜色/大小。
    /// 网络模块或模拟器产生此结构，PointCloudRenderer 消费。
    /// </summary>
    public struct PointCloudFrame
    {
        /// <summary>
        /// 点云位置数组。必须提供。
        /// </summary>
        public Vector3[] Positions { get; set; }

        /// <summary>
        /// 点云颜色数组（可选）。长度应与 Positions 一致。
        /// 不提供时默认白色。
        /// </summary>
        public Color[] Colors { get; set; }

        /// <summary>
        /// 点大小倍数数组（可选）。长度应与 Positions 一致。
        /// 不提供时所有点使用统一大小（通过 renderer 的 PointSize 设置）。
        /// </summary>
        public float[] Sizes { get; set; }

        /// <summary>
        /// 是否有有效的点云数据。
        /// </summary>
        public bool IsValid => Positions is { Length: > 0 };
    }
}
