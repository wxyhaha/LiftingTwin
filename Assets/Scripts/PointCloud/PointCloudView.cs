// -----------------------------------------------------------------------
// LiftingTwin - 点云视图桥接（MonoBehaviour）
//
// 职责：
//   持有 PointCloudRenderer 实例，在 Update 中驱动渲染。
//   对外公开 ApplyFrame / Clear 等 API。
// -----------------------------------------------------------------------

using LiftingTwin.Utils;
using UnityEngine;

namespace LiftingTwin.PointCloud
{
    /// <summary>
    /// 点云视图桥接。挂载到任意 GameObject 上即可显示点云。
    /// </summary>
    public class PointCloudView : MonoBehaviour
    {
        [Header("Rendering")]
        [Tooltip("使用 PointCloud.shader 的材质")]
        public Material pointMaterial;

        [Tooltip("点大小（世界单位）")]
        public float pointSize = 0.05f;

        [Tooltip("最大点数，按需分配")]
        public int maxPoints = 1_000_000;

        private PointCloudRenderer _renderer;

        private void Awake()
        {
            if (pointMaterial == null)
            {
                pointMaterial = CreateDefaultMaterial();
                if (pointMaterial == null)
                {
                    Log.Error("PointCloud", "PointCloudView: 无法创建默认材质，请手动指定");
                    enabled = false;
                    return;
                }
            }

            _renderer = new PointCloudRenderer(pointMaterial, maxPoints);
            _renderer.PointSize = pointSize;
            Log.Info("PointCloud", "PointCloudView 初始化完成");
        }

        private void Update()
        {
            if (_renderer != null)
                _renderer.Render();
        }

        private void OnDestroy()
        {
            if (_renderer != null)
            {
                _renderer.Release();
                _renderer = null;
            }
        }

        /// <summary>
        /// 更新点云显示数据。
        /// </summary>
        public void ApplyFrame(PointCloudFrame frame)
        {
            if (_renderer != null)
            {
                _renderer.PointSize = pointSize;
                _renderer.ApplyFrame(frame);
            }
        }

        /// <summary>
        /// 清空点云。
        /// </summary>
        public void Clear()
        {
            if (_renderer != null)
                _renderer.Clear();
        }

        /// <summary>
        /// 获取底层 PointCloudRenderer（用于高级操作或调试）。
        /// </summary>
        public PointCloudRenderer GetRenderer() => _renderer;

        /// <summary>
        /// 运行时创建默认 URP 材质（使用 PointCloud.shader）。
        /// </summary>
        private static Material CreateDefaultMaterial()
        {
            var shader = Shader.Find("LiftingTwin/PointCloud");
            if (shader != null)
                return new Material(shader);

            Log.Warn("PointCloud", "未找到 LiftingTwin/PointCloud Shader，尝试按名称查找...");
            shader = Shader.Find("Custom/PointCloud");
            if (shader != null)
                return new Material(shader);

            return null;
        }
    }
}
