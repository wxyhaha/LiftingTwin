// -----------------------------------------------------------------------
// LiftingTwin - 物体选中管理器
//
// 职责：
//   鼠标点击选取场景中的可选中物体（MeshView），高亮并通知 UI。
// -----------------------------------------------------------------------

using LiftingTwin.Mesh;
using LiftingTwin.Utils;
using UnityEngine;

namespace LiftingTwin.UI
{
    /// <summary>
    /// 场景物体选中管理器。鼠标点击射线检测，高亮选中物体。
    /// </summary>
    public class SelectionManager : MonoBehaviour
    {
        [Header("Highlight")]
        [Tooltip("选中高亮颜色")]
        public Color highlightColor = new Color(0.3f, 0.8f, 1.0f);

        [Tooltip("高亮强度")]
        public float highlightIntensity = 1.5f;

        private Camera _camera;
        private GameObject _selectedObject;
        private MeshRenderer _selectedRenderer;
        private MaterialPropertyBlock _highlightBlock;

        /// <summary>
        /// 当前选中的 GameObject。
        /// </summary>
        public GameObject SelectedObject => _selectedObject;

        /// <summary>
        /// 当前选中物体的名称。
        /// </summary>
        public string SelectedName =>
            _selectedObject != null ? _selectedObject.name : null;

        /// <summary>
        /// 当前选中物体的 ID（通过 MeshManager 获取）。
        /// </summary>
        public int? SelectedId { get; private set; }

        /// <summary>
        /// 是否有选中物体。
        /// </summary>
        public bool HasSelection => _selectedObject != null;

        /// <summary>
        /// 选中发生变化时触发。
        /// </summary>
        public event System.Action<GameObject> OnSelectionChanged;

        private void Awake()
        {
            _highlightBlock = new MaterialPropertyBlock();
        }

        private void Start()
        {
            _camera = Camera.main;
            if (_camera == null)
                Log.Error("UI", "SelectionManager: 未找到 Main Camera，请确保 Camera 标签为 MainCamera");
            else
                Log.Info("UI", "SelectionManager 就绪，Camera={0}", _camera.name);
        }

        private void Update()
        {
            // 鼠标左键点击
            if (Input.GetMouseButtonDown(0) && !Input.GetKey(KeyCode.LeftShift))
            {
                HandleClick();
            }

            // ESC 取消选中
            if (Input.GetKeyDown(KeyCode.Escape) && HasSelection)
            {
                Deselect();
            }
        }

        private void HandleClick()
        {
            if (_camera == null)
            {
                _camera = Camera.main; // 延迟重试
                if (_camera == null) return;
            }

            var ray = _camera.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out var hit, 500f))
            {
                var hitGo = hit.collider.gameObject;
                Log.Debug("UI", "点击命中: {0} (layer={1}, hasMeshView={2})",
                    hitGo.name, hitGo.layer, hitGo.GetComponent<MeshView>() != null);

                // 检查是否有 MeshView（管理对象的标识）
                if (hitGo.GetComponent<MeshView>() != null)
                {
                    Select(hitGo);
                    return;
                }
            }
            else
            {
                Log.Debug("UI", "点击未命中任何物体");
            }

            // 点击空白处取消选中
            Deselect();
        }

        /// <summary>
        /// 选中指定物体。
        /// </summary>
        public void Select(GameObject obj)
        {
            if (_selectedObject == obj) return; // 已是选中状态

            // 恢复上一个物体的颜色
            RestoreHighlight();

            _selectedObject = obj;
            _selectedRenderer = obj.GetComponent<MeshRenderer>();

            // 获取 MeshManager 中的 ID
            SelectedId = FindMeshManagerId(obj);

            // 高亮新物体
            ApplyHighlight();

            Log.Debug("UI", "选中物体: {0}", obj.name);
            OnSelectionChanged?.Invoke(obj);
        }

        /// <summary>
        /// 取消选中。
        /// </summary>
        public void Deselect()
        {
            if (_selectedObject == null) return;

            RestoreHighlight();

            var prevName = _selectedObject.name;
            _selectedObject = null;
            _selectedRenderer = null;
            SelectedId = null;

            Log.Debug("UI", "取消选中: {0}", prevName);
            OnSelectionChanged?.Invoke(null);
        }

        /// <summary>
        /// 获取选中物体的 MeshView 组件。
        /// </summary>
        public MeshView GetSelectedMeshView()
        {
            return _selectedObject?.GetComponent<MeshView>();
        }

        // ── 高亮 ──

        private void ApplyHighlight()
        {
            if (_selectedRenderer == null) return;

            // 保存原始 color（防止多次选中时覆盖）
            _selectedRenderer.GetPropertyBlock(_highlightBlock);
            var baseColor = _highlightBlock.HasProperty(Shader.PropertyToID("_Color"))
                ? _highlightBlock.GetColor(Shader.PropertyToID("_Color"))
                : Color.white;

            // 设置高亮颜色
            _highlightBlock.SetColor(Shader.PropertyToID("_Color"), highlightColor * highlightIntensity);
            _selectedRenderer.SetPropertyBlock(_highlightBlock);
        }

        private void RestoreHighlight()
        {
            if (_selectedRenderer == null) return;

            _selectedRenderer.GetPropertyBlock(_highlightBlock);
            _highlightBlock.Clear();
            _selectedRenderer.SetPropertyBlock(_highlightBlock);
        }

        private int? FindMeshManagerId(GameObject obj)
        {
            // 从 Bootstrap 找 MeshManagerView，遍历其管理的对象
            var meshView = GetComponent<MeshManagerView>()
                ?? FindObjectOfType<MeshManagerView>();
            if (meshView == null || meshView.Manager == null) return null;

            foreach (var id in meshView.Manager.GetAllIds())
            {
                var t = meshView.Manager.GetTransform(id);
                if (t != null && t.gameObject == obj)
                    return id;
            }
            return null;
        }
    }
}
