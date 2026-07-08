// -----------------------------------------------------------------------
// LiftingTwin - 多 Mesh 对象管理器（纯 C# 类）
//
// 职责：
//   管理场景中多个动态 Mesh 对象的生命周期。每个对象有独立的
//   GameObject、Transform 和 MeshView，可通过 ID 增删改查。
//
// 用法：
//   var manager = new MeshManager(parentTransform, defaultMaterial);
//   int id = manager.AddObject("塔身", towerFrame);
//   manager.SetPosition(id, new Vector3(10, 0, 5));
//   manager.UpdateMesh(id, newFrame);
//   manager.RemoveObject(id);
// -----------------------------------------------------------------------

using System.Collections.Generic;
using LiftingTwin.Utils;
using UnityEngine;

namespace LiftingTwin.Mesh
{
    /// <summary>
    /// 多 Mesh 对象管理器。每个对象对应一个独立的 GameObject。
    /// </summary>
    public class MeshManager
    {
        private readonly Transform _parent;
        private readonly Material _defaultMaterial;
        private readonly Dictionary<int, MeshObjectData> _objects = new();
        private int _nextId;

        /// <summary>
        /// 当前管理的对象数量。
        /// </summary>
        public int ObjectCount => _objects.Count;

        /// <summary>
        /// 创建 MeshManager。
        /// </summary>
        /// <param name="parent">所有 Mesh 物体的父级 Transform</param>
        /// <param name="defaultMaterial">新建对象默认材质，null 则尝试自动创建 URP Lit</param>
        public MeshManager(Transform parent, Material defaultMaterial = null)
        {
            _parent = parent;
            _defaultMaterial = defaultMaterial ?? CreateDefaultMaterial();
        }

        /// <summary>
        /// 添加一个新的 Mesh 对象到场景中。
        /// </summary>
        /// <param name="name">对象名称</param>
        /// <param name="frame">初始网格数据（可为空，稍后更新）</param>
        /// <param name="material">材质，不指定则使用管理器默认材质</param>
        /// <param name="addCollider">是否添加 MeshCollider（用于射线选中）</param>
        /// <returns>新对象的唯一 ID</returns>
        public int AddObject(string name, MeshFrame frame, Material material = null,
            bool addCollider = false)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_parent);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;

            go.AddComponent<MeshFilter>();
            var renderer = go.AddComponent<MeshRenderer>();
            var meshView = go.AddComponent<MeshView>();

            renderer.material = material ?? _defaultMaterial;

            if (frame.IsValid)
                meshView.ApplyFrame(frame);

            // 添加碰撞体（用于鼠标选中）
            if (addCollider)
            {
                var collider = go.AddComponent<MeshCollider>();
                // 使用 DynamicMesh 的 Unity Mesh 作为碰撞网格
                var dynamicMesh = meshView.GetDynamicMesh();
                if (dynamicMesh != null)
                    collider.sharedMesh = dynamicMesh.Mesh;
            }

            var id = _nextId++;
            _objects[id] = new MeshObjectData
            {
                Id = id,
                Name = name,
                GameObject = go,
                View = meshView
            };

            Log.Info("Mesh", "MeshManager 添加对象 [{0}] id={1}", name, id);
            return id;
        }

        /// <summary>
        /// 移除指定 ID 的 Mesh 对象。
        /// </summary>
        public bool RemoveObject(int id)
        {
            if (!_objects.TryGetValue(id, out var data))
            {
                Log.Warn("Mesh", "MeshManager.RemoveObject: id={0} 不存在", id);
                return false;
            }

            Log.Info("Mesh", "MeshManager 移除对象 [{0}] id={1}", data.Name, id);
            Object.Destroy(data.GameObject);
            _objects.Remove(id);
            return true;
        }

        /// <summary>
        /// 更新指定对象的网格数据。
        /// </summary>
        public bool UpdateMesh(int id, MeshFrame frame)
        {
            if (!TryGet(id, out var data)) return false;
            data.View.ApplyFrame(frame);
            return true;
        }

        /// <summary>
        /// 设置对象的世界坐标位置。
        /// </summary>
        public bool SetPosition(int id, Vector3 position)
        {
            if (!TryGet(id, out var data)) return false;
            data.GameObject.transform.position = position;
            return true;
        }

        /// <summary>
        /// 设置对象的旋转。
        /// </summary>
        public bool SetRotation(int id, Quaternion rotation)
        {
            if (!TryGet(id, out var data)) return false;
            data.GameObject.transform.rotation = rotation;
            return true;
        }

        /// <summary>
        /// 设置对象的缩放。
        /// </summary>
        public bool SetScale(int id, Vector3 scale)
        {
            if (!TryGet(id, out var data)) return false;
            data.GameObject.transform.localScale = scale;
            return true;
        }

        /// <summary>
        /// 同时设置位置和旋转。
        /// </summary>
        public bool SetTransform(int id, Vector3 position, Quaternion rotation)
        {
            if (!TryGet(id, out var data)) return false;
            var t = data.GameObject.transform;
            t.position = position;
            t.rotation = rotation;
            return true;
        }

        /// <summary>
        /// 获取对象的 Transform（可用于自定义操作）。
        /// </summary>
        public Transform GetTransform(int id)
        {
            return _objects.TryGetValue(id, out var data) ? data.GameObject.transform : null;
        }

        /// <summary>
        /// 获取对象的 MeshView（可用于高级操作）。
        /// </summary>
        public MeshView GetMeshView(int id)
        {
            return _objects.TryGetValue(id, out var data) ? data.View : null;
        }

        /// <summary>
        /// 获取对象名称。
        /// </summary>
        public string GetName(int id)
        {
            return _objects.TryGetValue(id, out var data) ? data.Name : null;
        }

        /// <summary>
        /// 获取所有当前管理的对象 ID。
        /// </summary>
        public IEnumerable<int> GetAllIds()
        {
            return _objects.Keys;
        }

        /// <summary>
        /// 清除所有对象。
        /// </summary>
        public void Clear()
        {
            Log.Info("Mesh", "MeshManager 清除所有对象（共 {0} 个）", _objects.Count);
            foreach (var data in _objects.Values)
                Object.Destroy(data.GameObject);
            _objects.Clear();
            _nextId = 0;
        }

        /// <summary>
        /// 是否存在指定 ID 的对象。
        /// </summary>
        public bool HasObject(int id)
        {
            return _objects.ContainsKey(id);
        }

        private bool TryGet(int id, out MeshObjectData data)
        {
            if (_objects.TryGetValue(id, out data))
                return true;

            Log.Warn("Mesh", "MeshManager: id={0} 不存在", id);
            return false;
        }

        private static Material CreateDefaultMaterial()
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader != null)
            {
                var mat = new Material(shader);
                mat.color = new Color(0.55f, 0.55f, 0.58f); // 钢灰色
                return mat;
            }

            Log.Warn("Mesh", "MeshManager: 未找到 URP/Lit Shader，使用内置默认材质");
            return null;
        }

        /// <summary>
        /// 内部数据：一个管理对象的完整信息。
        /// </summary>
        private class MeshObjectData
        {
            public int Id;
            public string Name;
            public GameObject GameObject;
            public MeshView View;
        }
    }
}
