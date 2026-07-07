// -----------------------------------------------------------------------
// LiftingTwin - 相机配置 (ScriptableObject)
//
// 用途：
//   存储相机控制的所有可调参数，在 Inspector 中直接编辑。
//   通过 CreateAssetMenu 创建实例，存放于 Assets/_Config/ 目录。
// -----------------------------------------------------------------------

using UnityEngine;

namespace LiftingTwin.Visualization
{
    /// <summary>
    /// 自由相机控制的全部可配置参数。
    /// 创建方式：Assets > Create > LiftingTwin > Camera Config
    /// </summary>
    [CreateAssetMenu(fileName = "CameraConfig", menuName = "LiftingTwin/Camera Config", order = 2)]
    public class CameraConfig : ScriptableObject
    {
        [Header("Orbit")]
        [Tooltip("轨道旋转速度（度/像素）")]
        public float orbitSpeed = 0.3f;

        [Tooltip("轨道旋转角度上限（度），0 = 不限制")]
        public float maxPolarAngle = 89f;

        [Tooltip("轨道旋转角度下限（度），0 = 不限制")]
        public float minPolarAngle = 5f;

        [Header("Pan")]
        [Tooltip("平移速度系数")]
        public float panSpeed = 0.01f;

        [Header("Zoom")]
        [Tooltip("缩放速度系数")]
        public float zoomSpeed = 1.0f;

        [Tooltip("最近缩放距离")]
        public float minDistance = 0.5f;

        [Tooltip("最远缩放距离")]
        public float maxDistance = 500f;

        [Header("Smoothing")]
        [Tooltip("启用平滑过渡")]
        public bool enableSmoothing = true;

        [Tooltip("平滑插值速度（越大越快）")]
        public float smoothSpeed = 8f;

        [Header("Defaults")]
        [Tooltip("初始相机位置")]
        public Vector3 defaultPosition = new Vector3(0, 5, -15);

        [Tooltip("初始观察目标点")]
        public Vector3 defaultTarget = Vector3.zero;
    }
}
