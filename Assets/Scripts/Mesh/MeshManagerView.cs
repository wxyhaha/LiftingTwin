// -----------------------------------------------------------------------
// LiftingTwin - Mesh 管理器视图（MonoBehaviour 桥接）
//
// 职责：
//   作为 MeshManager 的场景入口，挂载到 Bootstrap 上。
//   提供全局访问点，供 Network / Runtime 等其他模块调用。
// -----------------------------------------------------------------------

using LiftingTwin.Utils;
using UnityEngine;

namespace LiftingTwin.Mesh
{
    /// <summary>
    /// Mesh 管理器桥接组件。
    /// 挂载到 Bootstrap GameObject 上，持有 MeshManager 实例。
    /// </summary>
    public class MeshManagerView : MonoBehaviour
    {
        [Header("Defaults")]
        [Tooltip("新建 Mesh 对象的默认材质")]
        public Material defaultMaterial;

        private MeshManager _manager;
        private GameObject _container;

        /// <summary>
        /// 获取底层的 MeshManager 实例。
        /// </summary>
        public MeshManager Manager
        {
            get
            {
                if (_manager == null)
                    Initialize();
                return _manager;
            }
        }

        private void Awake()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (_manager != null) return;

            _container = new GameObject("Mesh Objects");
            _container.transform.SetParent(transform);
            _container.transform.localPosition = Vector3.zero;

            _manager = new MeshManager(_container.transform, defaultMaterial);
            Log.Info("Mesh", "MeshManagerView 初始化完成");
        }

        private void OnDestroy()
        {
            if (_manager != null)
            {
                _manager.Clear();
                _manager = null;
            }
        }

        // ---- 便捷代理方法 ----

        /// <summary>添加 Mesh 对象。</summary>
        public int AddObject(string name, MeshFrame frame, Material material = null)
            => Manager.AddObject(name, frame, material);

        /// <summary>移除对象。</summary>
        public bool RemoveObject(int id) => Manager.RemoveObject(id);

        /// <summary>更新网格数据。</summary>
        public bool UpdateMesh(int id, MeshFrame frame) => Manager.UpdateMesh(id, frame);

        /// <summary>设置位置。</summary>
        public bool SetPosition(int id, Vector3 position) => Manager.SetPosition(id, position);

        /// <summary>设置旋转。</summary>
        public bool SetRotation(int id, Quaternion rotation) => Manager.SetRotation(id, rotation);

        /// <summary>设置缩放。</summary>
        public bool SetScale(int id, Vector3 scale) => Manager.SetScale(id, scale);

        /// <summary>清除所有对象。</summary>
        public void ClearAll() => Manager.Clear();
    }
}
