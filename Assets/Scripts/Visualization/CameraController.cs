// -----------------------------------------------------------------------
// LiftingTwin - 相机控制器（纯 C# 类）
//
// 职责：
//   封装自由相机的轨道/平移/缩放数学运算，不依赖 MonoBehaviour。
//   FreeCameraView 负责输入采集和生命周期桥接。
//
// 用法：
//   var controller = new CameraController(config);
//   controller.Orbit(deltaX, deltaY);
//   controller.Pan(deltaX, deltaY);
//   controller.Zoom(delta);
//   controller.Tick(deltaTime, out pos, out rot);
// -----------------------------------------------------------------------

using UnityEngine;

namespace LiftingTwin.Visualization
{
    /// <summary>
    /// 自由相机轨道/平移/缩放控制器。
    /// 所有角度、距离运算在这里完成，输出目标 position + rotation。
    /// </summary>
    public class CameraController
    {
        private readonly CameraConfig _config;

        // 球坐标系参数
        private float _azimuth;
        private float _polar;
        private float _distance;

        // 平滑缓冲
        private float _smoothAzimuth;
        private float _smoothPolar;
        private float _smoothDistance;
        private Vector3 _smoothTarget;

        /// <summary>
        /// 当前观察目标点（世界坐标）
        /// </summary>
        public Vector3 Target { get; set; }

        /// <summary>
        /// 当前原始（未平滑）方位角（度）
        /// </summary>
        public float Azimuth => _azimuth;

        /// <summary>
        /// 当前原始（未平滑）极角（度）
        /// </summary>
        public float Polar => _polar;

        /// <summary>
        /// 当前原始（未平滑）距离
        /// </summary>
        public float Distance => _distance;

        /// <summary>
        /// 使用指定配置创建相机控制器。
        /// </summary>
        /// <param name="config">相机配置资产</param>
        public CameraController(CameraConfig config)
        {
            _config = config;
            Reset();
        }

        /// <summary>
        /// 重置到配置中的默认位置和目标。
        /// </summary>
        public void Reset()
        {
            var offset = _config.defaultPosition - _config.defaultTarget;
            var spherical = ToSpherical(offset);

            _azimuth = spherical.x;
            _polar = spherical.y;
            _distance = spherical.z;
            Target = _config.defaultTarget;

            _smoothAzimuth = _azimuth;
            _smoothPolar = _polar;
            _smoothDistance = _distance;
            _smoothTarget = Target;
        }

        /// <summary>
        /// 设置目标点并调整距离以保持相机位置不变。
        /// </summary>
        /// <param name="newTarget">新的观察目标点</param>
        public void SetTarget(Vector3 newTarget)
        {
            var currentPos = GetPosition();
            Target = newTarget;
            var offset = currentPos - Target;
            var spherical = ToSpherical(offset);
            _distance = Mathf.Clamp(spherical.z, _config.minDistance, _config.maxDistance);
            _smoothTarget = Target;
            _smoothDistance = _distance;
        }

        /// <summary>
        /// 轨道旋转。由鼠标拖拽驱动。
        /// </summary>
        /// <param name="deltaX">水平像素偏移</param>
        /// <param name="deltaY">垂直像素偏移</param>
        public void Orbit(float deltaX, float deltaY)
        {
            _azimuth += deltaX * _config.orbitSpeed;
            _polar += deltaY * _config.orbitSpeed;
            ClampAngles();
        }

        /// <summary>
        /// 平移目标点。由鼠标中键拖拽驱动。
        /// </summary>
        /// <param name="deltaX">水平像素偏移</param>
        /// <param name="deltaY">垂直像素偏移</param>
        public void Pan(float deltaX, float deltaY)
        {
            var right = GetRight();
            var up = Vector3.Cross(right, GetForward()).normalized;

            var panAmount = _distance * _config.panSpeed;
            Target -= right * (deltaX * panAmount);
            Target += up * (deltaY * panAmount);
        }

        /// <summary>
        /// 缩放。由滚轮驱动。
        /// </summary>
        /// <param name="delta">滚轮增量</param>
        public void Zoom(float delta)
        {
            _distance -= delta * _config.zoomSpeed;
            _distance = Mathf.Clamp(_distance, _config.minDistance, _config.maxDistance);
        }

        /// <summary>
        /// 沿相机观察方向前后移动。由 W/S 键驱动。
        /// </summary>
        /// <param name="delta">按键增量（正=前进，负=后退）</param>
        public void MoveForward(float delta)
        {
            var forward = GetForward();
            forward.y = 0f; // 保持水平
            if (forward.sqrMagnitude < 0.001f) return;
            forward.Normalize();

            var moveAmount = delta * _distance * _config.moveSpeed;
            Target += forward * moveAmount;
        }

        /// <summary>
        /// 沿相机右方向水平移动。由 A/D 键驱动。
        /// </summary>
        /// <param name="delta">按键增量（正=右移，负=左移）</param>
        public void Strafe(float delta)
        {
            var right = GetRight();
            right.y = 0f; // 保持水平
            if (right.sqrMagnitude < 0.001f) return;
            right.Normalize();

            var moveAmount = delta * _distance * _config.moveSpeed;
            Target += right * moveAmount;
        }

        /// <summary>
        /// 每帧调用，返回平滑后的相机位置和旋转。
        /// </summary>
        /// <param name="deltaTime">帧时间</param>
        /// <param name="position">输出：平滑后相机位置</param>
        /// <param name="rotation">输出：平滑后相机旋转</param>
        public void Tick(float deltaTime, out Vector3 position, out Quaternion rotation)
        {
            if (_config.enableSmoothing && deltaTime > 0f)
            {
                var t = Mathf.Clamp01(_config.smoothSpeed * deltaTime);
                _smoothAzimuth = Mathf.Lerp(_smoothAzimuth, _azimuth, t);
                _smoothPolar = Mathf.Lerp(_smoothPolar, _polar, t);
                _smoothDistance = Mathf.Lerp(_smoothDistance, _distance, t);
                _smoothTarget = Vector3.Lerp(_smoothTarget, Target, t);
            }
            else
            {
                _smoothAzimuth = _azimuth;
                _smoothPolar = _polar;
                _smoothDistance = _distance;
                _smoothTarget = Target;
            }

            var pos = FromSpherical(_smoothAzimuth, _smoothPolar, _smoothDistance) + _smoothTarget;
            position = pos;
            rotation = Quaternion.LookRotation(_smoothTarget - pos, Vector3.up);
        }

        /// <summary>
        /// 获取当前平滑后的相机位置（不经 Tick 也能调用）。
        /// </summary>
        public Vector3 GetPosition()
        {
            return FromSpherical(_azimuth, _polar, _distance) + Target;
        }

        // ---- 内部辅助 ----

        private void ClampAngles()
        {
            _azimuth %= 360f;
            if (_config.minPolarAngle > 0)
                _polar = Mathf.Max(_polar, _config.minPolarAngle);
            if (_config.maxPolarAngle > 0)
                _polar = Mathf.Min(_polar, _config.maxPolarAngle);
        }

        private Vector3 GetForward()
        {
            return (Target - GetPosition()).normalized;
        }

        private Vector3 GetRight()
        {
            return Vector3.Cross(GetForward(), Vector3.up).normalized;
        }

        // 向量转球坐标 (azimuth, polar, distance)
        private static Vector3 ToSpherical(Vector3 v)
        {
            var d = v.magnitude;
            if (d < 0.0001f) return Vector3.zero;

            var azimuth = Mathf.Atan2(v.x, v.z) * Mathf.Rad2Deg;
            var polar = Mathf.Asin(v.y / d) * Mathf.Rad2Deg;
            return new Vector3(azimuth, polar, d);
        }

        // 球坐标转向量
        private static Vector3 FromSpherical(float azimuth, float polar, float distance)
        {
            var a = azimuth * Mathf.Deg2Rad;
            var p = polar * Mathf.Deg2Rad;

            return new Vector3(
                distance * Mathf.Cos(p) * Mathf.Sin(a),
                distance * Mathf.Sin(p),
                distance * Mathf.Cos(p) * Mathf.Cos(a)
            );
        }
    }
}
