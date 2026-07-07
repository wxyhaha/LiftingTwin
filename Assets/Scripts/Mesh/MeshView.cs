// -----------------------------------------------------------------------
// LiftingTwin - 网格视图桥接（MonoBehaviour）
//
// 职责：
//   挂载到拥有 MeshFilter + MeshRenderer 的 GameObject 上，
//   将 DynamicMesh 与 Unity 渲染管线连接起来。
//
// 用法：
//   meshView.ApplyFrame(frame);  // 从网络层或模拟器调用
//   meshView.Clear();             // 清除网格
// -----------------------------------------------------------------------

using LiftingTwin.Utils;
using UnityEngine;

namespace LiftingTwin.Mesh
{
    /// <summary>
    /// 网格视图桥接。挂载到需要显示动态网格的 GameObject 上。
    /// 要求同时存在 MeshFilter 和 MeshRenderer 组件。
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class MeshView : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("可选材质覆盖。不指定则使用 MeshRenderer 的默认材质。")]
        private Material material;

        private DynamicMesh _dynamicMesh;
        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;

        private void Awake()
        {
            _meshFilter = GetComponent<MeshFilter>();
            _meshRenderer = GetComponent<MeshRenderer>();

            _dynamicMesh = new DynamicMesh();
            _meshFilter.sharedMesh = _dynamicMesh.Mesh;

            if (material != null)
                _meshRenderer.material = material;

            Log.Info("Mesh", "MeshView 初始化完成，位置={0}", transform.position);
        }

        /// <summary>
        /// 应用新的网格帧数据，更新显示。
        /// 可在任意时刻调用，包括外部网络回调。
        /// </summary>
        /// <param name="frame">网格帧数据</param>
        public void ApplyFrame(MeshFrame frame)
        {
            _dynamicMesh.UpdateFrame(frame);
        }

        /// <summary>
        /// 清空当前显示的网格。
        /// </summary>
        public void Clear()
        {
            _dynamicMesh.Clear();
        }

        /// <summary>
        /// 获取当前 DynamicMesh 实例（用于测试或高级操作）。
        /// </summary>
        public DynamicMesh GetDynamicMesh() => _dynamicMesh;
    }
}
