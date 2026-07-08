// -----------------------------------------------------------------------
// LiftingTwin - UI 管理器
//
// 职责：
//   创建并更新游戏内 HUD：FPS、状态信息、操作提示等。
//   挂载到 Bootstrap 上，自动在 Awake 构建 Canvas。
// -----------------------------------------------------------------------

using LiftingTwin.Mesh;
using LiftingTwin.PointCloud;
using LiftingTwin.Utils;
using UnityEngine;
using UnityEngine.UI;
using System.Text;

namespace LiftingTwin.UI
{
    /// <summary>
    /// UI 管理器。自动创建 Canvas 并显示系统状态信息。
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        [Header("References (自动查找)")]
        public MeshManagerView meshManager;
        public PointCloudView pointCloudView;

        [Header("Display")]
        public bool showFPS = true;
        public bool showInfoPanel = true;
        public bool showControlHints = true;

        private Text _fpsText;
        private Text _statusText;
        private Text _infoText;
        private Text _selectionText;
        private Text _hintText;
        private Font _font;

        private SelectionManager _selection;
        private GameObject _selectionPanel;

        private float _fpsDelta;
        private float _fpsLastUpdate;
        private int _fpsFrameCount;

        private void Awake()
        {
            try
            {
                CreateCanvas();
                Log.Info("UI", "UIManager 初始化完成");
            }
            catch (System.Exception ex)
            {
                Log.Error("UI", "UIManager 初始化失败: {0}", ex.Message);
                enabled = false;
            }
        }

        private void Update()
        {
            if (showFPS && _fpsText != null) UpdateFPS();
            if (showInfoPanel && _infoText != null) UpdateInfoPanel();
        }

        // ── Canvas 构建 ──

        private void CreateCanvas()
        {
            // 查找可用的字体
            _font = ResolveFont();
            if (_font == null)
                Log.Warn("UI", "未找到 Arial/LegacyRuntime 字体，UI 文字可能不显示");

            var canvasGO = new GameObject("HUD Canvas");
            canvasGO.transform.SetParent(transform);

            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000; // 确保在最上层

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasGO.AddComponent<GraphicRaycaster>();

            // 顶部状态栏
            if (showFPS)
                CreateTopBar(canvasGO.transform);

            // 场景信息面板
            if (showInfoPanel)
                CreateInfoPanel(canvasGO.transform);

            // 选中物体面板（独立于 infoPanel，有选中时显示）
            CreateSelectionPanel(canvasGO.transform);

            // 底部操作提示
            if (showControlHints)
                CreateControlHints(canvasGO.transform);
        }

        /// <summary>
        /// 查找可用的系统字体。
        /// </summary>
        private static Font ResolveFont()
        {
            // 按优先级尝试不同的字体源
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font != null) return font;

            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (font != null) return font;

            // 尝试系统字体
            var systemFonts = Font.GetOSInstalledFontNames();
            foreach (var name in systemFonts)
            {
                if (name.ToLower().Contains("arial") || name.ToLower().Contains("sans") || name.ToLower().Contains("segoe"))
                {
                    font = Font.CreateDynamicFontFromOSFont(name, 16);
                    if (font != null) return font;
                }
            }

            // 第一个可用的系统字体
            if (systemFonts.Length > 0)
                return Font.CreateDynamicFontFromOSFont(systemFonts[0], 16);

            return null;
        }

        private void CreateTopBar(Transform parent)
        {
            var bar = CreatePanel("TopBar", parent, new Rect(0, 0.9f, 1, 0.1f),
                new Color(0, 0, 0, 0.5f));

            // 标题
            CreateText("TitleText", bar.transform, "LiftingTwin",
                new Rect(0.02f, 0, 0.3f, 1), TextAnchor.MiddleLeft, 20, Color.white);

            // 状态指示
            _statusText = CreateText("StatusText", bar.transform, "● 就绪",
                new Rect(0.35f, 0, 0.3f, 1), TextAnchor.MiddleCenter, 16, new Color(0.3f, 0.8f, 0.3f));

            // FPS
            _fpsText = CreateText("FPSText", bar.transform, "60 FPS",
                new Rect(0.85f, 0, 0.13f, 1), TextAnchor.MiddleRight, 18, Color.white);
        }

        private void CreateInfoPanel(Transform parent)
        {
            var panel = CreatePanel("InfoPanel", parent, new Rect(0.01f, 0.12f, 0.18f, 0.25f),
                new Color(0, 0, 0, 0.4f));

            _infoText = CreateText("InfoText", panel.transform, "",
                new Rect(0.06f, 0, 0.88f, 1), TextAnchor.UpperLeft, 14, new Color(0.8f, 0.8f, 0.8f));
        }

        private void CreateSelectionPanel(Transform parent)
        {
            _selectionPanel = CreatePanel("SelectionPanel", parent,
                new Rect(0.01f, 0.38f, 0.18f, 0.15f), new Color(0.1f, 0.3f, 0.5f, 0.5f));
            _selectionPanel.SetActive(false); // 默认隐藏

            _selectionText = CreateText("SelectionText", _selectionPanel.transform, "",
                new Rect(0.06f, 0, 0.88f, 1), TextAnchor.UpperLeft, 14, new Color(0.9f, 0.9f, 1f));
        }

        /// <summary>
        /// 绑定选中管理器事件。
        /// </summary>
        private void Start()
        {
            _selection = GetComponent<SelectionManager>();
            if (_selection != null)
            {
                _selection.OnSelectionChanged += OnSelectionChanged;
                Log.Info("UI", "UIManager 已绑定 SelectionManager");
            }
        }

        private void OnSelectionChanged(GameObject obj)
        {
            if (_selectionPanel == null) return;

            if (obj == null)
            {
                _selectionPanel.SetActive(false);
                return;
            }

            _selectionPanel.SetActive(true);
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"--- 选中: {obj.name} ---");

            // 位置
            sb.AppendLine($"位置: ({obj.transform.position.x:F2}, {obj.transform.position.y:F2}, {obj.transform.position.z:F2})");

            // 顶点数
            var meshView = obj.GetComponent<MeshView>();
            if (meshView != null)
            {
                var dm = meshView.GetDynamicMesh();
                if (dm != null && dm.Mesh != null)
                {
                    sb.AppendLine($"顶点: {dm.Mesh.vertexCount}");
                    sb.AppendLine($"三角形: {dm.Mesh.triangles.Length / 3}");
                }
            }

            _selectionText.text = sb.ToString();
        }

        private void OnDestroy()
        {
            if (_selection != null)
                _selection.OnSelectionChanged -= OnSelectionChanged;
        }

        private void CreateControlHints(Transform parent)
        {
            _hintText = CreateText("ControlHints", parent,
                "WASD 移动  |  Q/E 升降  |  鼠标右键旋转  |  滚轮缩放",
                new Rect(0.15f, 0.02f, 0.7f, 0.04f),
                TextAnchor.MiddleCenter, 14, new Color(1, 1, 1, 0.5f));
        }

        // ── 更新 ──

        private void UpdateFPS()
        {
            _fpsFrameCount++;
            _fpsDelta += Time.unscaledDeltaTime;

            if (_fpsDelta >= 0.5f)
            {
                float fps = _fpsFrameCount / _fpsDelta;
                _fpsText.text = $"{Mathf.RoundToInt(fps)} FPS";
                _fpsFrameCount = 0;
                _fpsDelta = 0f;
            }
        }

        private void UpdateInfoPanel()
        {
            // 自动查找组件（首次）
            if (meshManager == null)
                meshManager = FindObjectOfType<MeshManagerView>();
            if (pointCloudView == null)
                pointCloudView = FindObjectOfType<PointCloudView>();

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("--- 场景信息 ---");

            // Mesh 对象数
            if (meshManager != null && meshManager.Manager != null)
                sb.AppendLine($"Mesh 对象: {meshManager.Manager.ObjectCount}");
            else
                sb.AppendLine("Mesh 管理器: 未就绪");

            // 点云
            if (pointCloudView != null && pointCloudView.GetRenderer() != null)
                sb.AppendLine($"点云点数: {pointCloudView.GetRenderer().PointCount}");
            else
                sb.AppendLine("点云: 未就绪");

            // 相机位置
            var cam = Camera.main;
            if (cam != null)
            {
                var p = cam.transform.position;
                sb.AppendLine($"相机: ({p.x:F1}, {p.y:F1}, {p.z:F1})");
            }

            _infoText.text = sb.ToString();
        }

        /// <summary>
        /// 设置连接状态文字。
        /// </summary>
        public void SetStatus(string text, Color color)
        {
            if (_statusText != null)
            {
                _statusText.text = $"● {text}";
                _statusText.color = color;
            }
        }

        // ── UI 构建辅助 ──

        private static GameObject CreatePanel(string name, Transform parent,
            Rect anchorRect, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(anchorRect.xMin, anchorRect.yMin);
            rt.anchorMax = new Vector2(anchorRect.xMax, anchorRect.yMax);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var img = go.AddComponent<Image>();
            img.color = color;

            return go;
        }

        private Text CreateText(string name, Transform parent, string content,
            Rect anchorRect, TextAnchor anchor, int fontSize, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(anchorRect.xMin, anchorRect.yMin);
            rt.anchorMax = new Vector2(anchorRect.xMax, anchorRect.yMax);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var text = go.AddComponent<Text>();
            text.text = content;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = anchor;
            text.font = _font;

            return text;
        }
    }
}
