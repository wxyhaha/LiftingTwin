// -----------------------------------------------------------------------
// LiftingTwin - 点云测试控制器（开发调试用）
//
// 用途：
//   生成测试点云数据（彩色球体），驱动 PointCloudView 显示。
//   演示每帧动态更新点云数据的能力。
// -----------------------------------------------------------------------

using LiftingTwin.Utils;
using UnityEngine;

namespace LiftingTwin.PointCloud
{
    /// <summary>
    /// 点云系统测试控制器。生成旋转的彩色球体点云。
    /// </summary>
    public class PointCloudTestController : MonoBehaviour
    {
        [Header("Point Cloud")]
        [Tooltip("指定现有的 PointCloudView（可选）")]
        public PointCloudView targetView;

        [Header("Test Settings")]
        [Tooltip("球体半径")]
        public float radius = 1.5f;

        [Tooltip("点数")]
        public int pointCount = 20000;

        [Tooltip("旋转速度（度/秒）")]
        public float rotationSpeed = 15f;

        private PointCloudView _view;
        private PointCloudFrame _baseFrame;
        private float _time;

        private void Start()
        {
            if (targetView != null)
            {
                _view = targetView;
            }
            else
            {
                var go = new GameObject("PointCloud (Test)");
                go.transform.SetParent(transform);
                go.transform.localPosition = new Vector3(8, 1.5f, 3);
                _view = go.AddComponent<PointCloudView>();
            }

            // 生成基础球体点云
            _baseFrame = GenerateSphere(radius, pointCount);
            _view.ApplyFrame(_baseFrame);

            Log.Info("PointCloud", "PointCloudTestController 已加载测试点云，点数={0}", pointCount);
        }

        private void Update()
        {
            _time += Time.deltaTime;
            var rotatedFrame = RotateFrame(_baseFrame, _time * rotationSpeed);
            _view.ApplyFrame(rotatedFrame);
        }

        /// <summary>
        /// 生成球体点云，颜色按位置渐变（红→绿→蓝）。
        /// </summary>
        private static PointCloudFrame GenerateSphere(float radius, int count)
        {
            var positions = new Vector3[count];
            var colors = new Color[count];

            for (int i = 0; i < count; i++)
            {
                // 在球体内随机分布，略微偏向表面
                float theta = Random.value * Mathf.PI * 2f;
                float phi = Mathf.Acos(2f * Random.value - 1f);
                float r = radius * (0.6f + 0.4f * Random.value); // 内部到表面的混合

                positions[i] = new Vector3(
                    r * Mathf.Sin(phi) * Mathf.Cos(theta),
                    r * Mathf.Sin(phi) * Mathf.Sin(theta),
                    r * Mathf.Cos(phi)
                );

                // 基于位置和角度的渐变颜色
                float hue = theta / (Mathf.PI * 2f);
                float sat = 0.7f + 0.3f * (r / radius);
                float val = 0.8f + 0.2f * Mathf.Sin(phi);
                colors[i] = Color.HSVToRGB(hue, sat, val);
            }

            return new PointCloudFrame
            {
                Positions = positions,
                Colors = colors
            };
        }

        /// <summary>
        /// 对点云整体绕 Y 轴旋转。
        /// </summary>
        private static PointCloudFrame RotateFrame(PointCloudFrame frame, float angleDeg)
        {
            var rot = Quaternion.Euler(0, angleDeg, 0);
            var count = frame.Positions.Length;
            var positions = new Vector3[count];

            for (int i = 0; i < count; i++)
                positions[i] = rot * frame.Positions[i];

            return new PointCloudFrame
            {
                Positions = positions,
                Colors = frame.Colors,
                Sizes = frame.Sizes
            };
        }
    }
}
