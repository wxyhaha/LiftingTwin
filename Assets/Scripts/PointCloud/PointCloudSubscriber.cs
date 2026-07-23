// -----------------------------------------------------------------------
// LiftingTwin - ROS2 点云订阅（对接 ROS-TCP-Connector）
//
// 订阅 /frontend/current_cloud_rgb_map (sensor_msgs/PointCloud2)
// 解析为 PointCloudFrame 后由 PointCloudView 渲染。
// -----------------------------------------------------------------------

#if ROS2
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Sensor;
using LiftingTwin.Utils;

namespace LiftingTwin.PointCloud
{
    /// <summary>
    /// ROS2 点云订阅者。挂载到包含 PointCloudView 的 GameObject 上。
    /// </summary>
    [RequireComponent(typeof(PointCloudView))]
    public class PointCloudSubscriber : MonoBehaviour
    {
        [Header("ROS2")]
        [Tooltip("点云话题")]
        public string topic = "/frontend/current_cloud_rgb_map";

        [Header("Filter")]
        [Tooltip("降采样：取点的步长，1 表示不降采样")]
        public int step = 1;

        [Tooltip("最大点数")]
        public int maxPoints = 1_000_000;

        private PointCloudView _view;
        private ROSConnection _ros;

        void Start()
        {
            _view = GetComponent<PointCloudView>();
            _view.maxPoints = maxPoints;

            _ros = ROSConnection.GetOrCreateInstance();
            _ros.Subscribe<PointCloud2Msg>(topic, OnPointCloud);
            Log.Info("PointCloud", "已订阅 {0}", topic);
        }

        void OnPointCloud(PointCloud2Msg msg)
        {
            int xOffset = -1, yOffset = -1, zOffset = -1;
            int rgbOffset = -1;
            int pointStep = (int)msg.point_step;
            int totalPoints = (int)(msg.width * msg.height);

            foreach (var field in msg.fields)
            {
                int off = (int)field.offset;
                switch (field.name)
                {
                    case "x": xOffset = off; break;
                    case "y": yOffset = off; break;
                    case "z": zOffset = off; break;
                    case "rgb":
                    case "rgba": rgbOffset = off; break;
                }
            }

            if (xOffset < 0 || yOffset < 0 || zOffset < 0)
            {
                Log.Warn("PointCloud", "点云缺少 x/y/z 字段");
                return;
            }

            int actualStep = Mathf.Max(1, step);
            int maxCount = Mathf.Min(totalPoints / actualStep, maxPoints);

            var positions = new Vector3[maxCount];
            var colors = new Color[maxCount];
            byte[] data = msg.data;
            bool hasColor = rgbOffset >= 0;

            int idx = 0;
            for (int i = 0; i < totalPoints && idx < maxCount; i += actualStep)
            {
                int baseIdx = i * pointStep;

                float x = System.BitConverter.ToSingle(data, baseIdx + xOffset);
                float y = System.BitConverter.ToSingle(data, baseIdx + yOffset);
                float z = System.BitConverter.ToSingle(data, baseIdx + zOffset);

                if (!float.IsFinite(x) || !float.IsFinite(y) || !float.IsFinite(z))
                    continue;

                positions[idx] = new Vector3(x, y, z);

                if (hasColor)
                {
                    uint rgbPacked = System.BitConverter.ToUInt32(data, baseIdx + rgbOffset);
                    colors[idx] = new Color32(
                        (byte)((rgbPacked >> 16) & 0xFF),
                        (byte)((rgbPacked >> 8) & 0xFF),
                        (byte)(rgbPacked & 0xFF),
                        255
                    );
                }
                else
                {
                    colors[idx] = Color.white;
                }

                idx++;
            }

            if (idx < maxCount)
            {
                System.Array.Resize(ref positions, idx);
                System.Array.Resize(ref colors, idx);
            }

            // 自适应点大小：根据点云空间范围估算平均点间距
            if (idx > 0)
            {
                Vector3 min = positions[0], max = positions[0];
                for (int i = 1; i < idx; i++)
                {
                    min = Vector3.Min(min, positions[i]);
                    max = Vector3.Max(max, positions[i]);
                }
                float extent = (max - min).magnitude;
                float spacing = extent / Mathf.Sqrt(idx);
                _view.pointSize = Mathf.Clamp(spacing * 0.5f, 0.001f, 0.1f);
            }

            _view.ApplyFrame(new PointCloudFrame
            {
                Positions = positions,
                Colors = colors
            });
        }
    }
}
#endif
