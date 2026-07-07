// -----------------------------------------------------------------------
// LiftingTwin - 自由相机视图（MonoBehaviour 桥接）
//
// 职责：
//   - 挂载到 Main Camera 上
//   - 采集鼠标输入（右键轨道、中键平移、滚轮缩放）
//   - 驱动 CameraController 计算目标变换
//   - 将结果应用到 Transform
//
// 设计原则（遵循 AGENTS.md）：
//   MonoBehaviour 只做桥接，不包含复杂逻辑。
// -----------------------------------------------------------------------

using LiftingTwin.Utils;
using UnityEngine;

namespace LiftingTwin.Visualization
{
    /// <summary>
    /// 自由相机输入桥接。挂载到 Main Camera GameObject 上。
    /// 右键拖拽旋转，中键拖拽平移，滚轮缩放。
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class FreeCameraView : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("相机配置资产，从 Assets/_Config/ 引用")]
        private CameraConfig config;

        private CameraController _controller;
        private Vector3 _lastMousePosition;

        private void Awake()
        {
            if (config == null)
            {
                Log.Warn("Visualization", "FreeCameraView: 未指定 CameraConfig，使用默认值创建");
                config = ScriptableObject.CreateInstance<CameraConfig>();
            }

            _controller = new CameraController(config);
            Log.Info("Visualization", "FreeCameraView 初始化完成，默认目标={0}", config.defaultTarget);
        }

        private void Start()
        {
            var pos = _controller.GetPosition();
            var rot = Quaternion.LookRotation(_controller.Target - pos, Vector3.up);
            transform.SetPositionAndRotation(pos, rot);
            _lastMousePosition = Input.mousePosition;
        }

        private void Update()
        {
            if (!Input.mousePresent) return;

            HandleMouseInput();

            _controller.Tick(Time.deltaTime, out var position, out var rotation);
            transform.SetPositionAndRotation(position, rotation);
        }

        private void HandleMouseInput()
        {
            var mouseDelta = Input.mousePosition - _lastMousePosition;
            _lastMousePosition = Input.mousePosition;

            // 滚轮缩放
            var scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.001f)
            {
                _controller.Zoom(scroll);
            }

            // 右键拖拽 -> 轨道旋转
            if (Input.GetMouseButton(1))
            {
                _controller.Orbit(mouseDelta.x, -mouseDelta.y);
            }

            // 中键拖拽 -> 平移
            if (Input.GetMouseButton(2))
            {
                _controller.Pan(-mouseDelta.x, -mouseDelta.y);
            }

            // Shift+左键 -> 平移（备用操作）
            if (Input.GetMouseButton(0) && Input.GetKey(KeyCode.LeftShift))
            {
                _controller.Pan(-mouseDelta.x, -mouseDelta.y);
            }
        }

        /// <summary>
        /// 将焦点切换到指定世界坐标。
        /// </summary>
        /// <param name="worldPosition">目标世界坐标</param>
        public void FocusOn(Vector3 worldPosition)
        {
            _controller.SetTarget(worldPosition);
            Log.Info("Visualization", "相机焦点切换到 {0}", worldPosition);
        }

        /// <summary>
        /// 重置相机到默认位置和角度。
        /// </summary>
        public void ResetView()
        {
            _controller.Reset();
            Log.Info("Visualization", "相机视图已重置");
        }

        /// <summary>
        /// 获取当前观察目标点。
        /// </summary>
        public Vector3 GetTarget() => _controller.Target;
    }
}
