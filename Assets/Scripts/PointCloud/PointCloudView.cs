// -----------------------------------------------------------------------
// LiftingTwin - 点云视图桥接（MonoBehaviour）
//
// 职责：
//   持有 PointCloudRenderer，驱动点云渲染。
//   提供 Mesh 回退模式（useMeshFallback），开发阶段先用 MeshView
//   渲染点云，待 GPU 版稳定后再切换到 GPU Instancing。
// -----------------------------------------------------------------------

using LiftingTwin.Mesh;
using LiftingTwin.Utils;
using UnityEngine;

namespace LiftingTwin.PointCloud
{
    /// <summary>
    /// 点云视图桥接。挂载到任意 GameObject 上即可显示点云。
    /// </summary>
    public class PointCloudView : MonoBehaviour
    {
        [Header("Rendering")]
        [Tooltip("使用 Mesh 回退模式（开发阶段先用 MeshView 显示）")]
        public bool useMeshFallback = true;

        [Tooltip("点大小（世界单位）")]
        public float pointSize = 0.5f;

        [Tooltip("使用 PointCloud.shader 的材质（GPU 模式用）")]
        public Material pointMaterial;

        [Tooltip("最大点数（GPU 模式用）")]
        public int maxPoints = 1_000_000;

        // GPU 模式
        private PointCloudRenderer _gpuRenderer;

        // Mesh 回退模式
        private DynamicMesh _fallbackMesh;
        private MeshFilter _fallbackFilter;
        private MeshRenderer _fallbackRenderer;
        private bool _initialized;

        private void Awake()
        {
            if (useMeshFallback)
                InitMeshFallback();
            else
                InitGpuMode();
        }

        private void InitMeshFallback()
        {
            // 使用 MeshView 显示点云——把每个点渲染成小方块
            _fallbackMesh = new DynamicMesh();

            _fallbackFilter = gameObject.GetComponent<MeshFilter>();
            if (_fallbackFilter == null)
                _fallbackFilter = gameObject.AddComponent<MeshFilter>();
            _fallbackFilter.sharedMesh = _fallbackMesh.Mesh;

            _fallbackRenderer = gameObject.GetComponent<MeshRenderer>();
            if (_fallbackRenderer == null)
                _fallbackRenderer = gameObject.AddComponent<MeshRenderer>();

            // 确保有材质
            if (_fallbackRenderer.sharedMaterial == null
                || _fallbackRenderer.sharedMaterial.shader.name == "Hidden/InternalErrorShader")
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader != null)
                {
                    var mat = new Material(shader);
                    mat.color = new Color(0.3f, 0.6f, 1.0f);
                    _fallbackRenderer.material = mat;
                }
            }

            _initialized = true;
            Log.Info("PointCloud", "PointCloudView 初始化完成（Mesh 回退模式）");
        }

        private void InitGpuMode()
        {
            if (pointMaterial == null)
            {
                pointMaterial = CreateDefaultMaterial();
                if (pointMaterial == null)
                {
                    Log.Warn("PointCloud", "自定义 Shader 不可用，回退到 Mesh 模式");
                    useMeshFallback = true;
                    InitMeshFallback();
                    return;
                }
            }

            _gpuRenderer = new PointCloudRenderer(pointMaterial, maxPoints);
            _gpuRenderer.PointSize = pointSize;
            _initialized = true;
            Log.Info("PointCloud", "PointCloudView 初始化完成（GPU 模式）");
        }

        private void OnRenderObject()
        {
            if (!_initialized) return;

            if (_gpuRenderer != null)
                _gpuRenderer.Render();
        }

        private void OnDestroy()
        {
            if (_gpuRenderer != null)
            {
                _gpuRenderer.Release();
                _gpuRenderer = null;
            }

            if (_fallbackMesh != null)
            {
                _fallbackMesh.Clear();
                _fallbackMesh = null;
            }
        }

        /// <summary>
        /// 更新点云显示数据。
        /// </summary>
        public void ApplyFrame(PointCloudFrame frame)
        {
            if (!_initialized || !frame.IsValid) return;

            if (_gpuRenderer != null)
            {
                _gpuRenderer.PointSize = pointSize;
                _gpuRenderer.ApplyFrame(frame);
            }
            else if (_fallbackMesh != null)
            {
                var meshFrame = ConvertToPointQuads(frame, pointSize);
                _fallbackMesh.UpdateFrame(meshFrame);
            }
        }

        /// <summary>
        /// 清空点云。
        /// </summary>
        public void Clear()
        {
            if (_gpuRenderer != null)
                _gpuRenderer.Clear();
            else if (_fallbackMesh != null)
                _fallbackMesh.Clear();
        }

        /// <summary>
        /// 将点云帧转换为网格帧（每个点 = 一个小方块 = 4 顶点 + 2 三角形）。
        /// 用于 Mesh 回退模式。
        /// </summary>
        private static MeshFrame ConvertToPointQuads(PointCloudFrame frame, float quadSize)
        {
            int count = frame.Positions.Length;
            var verts = new Vector3[count * 4];
            var tris = new int[count * 6];
            var colors = new Color[count * 4];

            bool hasColors = frame.Colors != null && frame.Colors.Length >= count;
            float halfSize = quadSize * 0.5f;

            for (int i = 0; i < count; i++)
            {
                var pos = frame.Positions[i];
                var color = hasColors ? frame.Colors[i] : Color.white;
                int vi = i * 4;

                // 4 个顶点构成一个小方块（在 XZ 平面展开）
                verts[vi + 0] = pos + new Vector3(-halfSize, -halfSize, 0);
                verts[vi + 1] = pos + new Vector3(halfSize, -halfSize, 0);
                verts[vi + 2] = pos + new Vector3(halfSize, halfSize, 0);
                verts[vi + 3] = pos + new Vector3(-halfSize, halfSize, 0);

                for (int j = 0; j < 4; j++)
                    colors[vi + j] = color;

                int ti = i * 6;
                tris[ti + 0] = vi + 0; tris[ti + 1] = vi + 1; tris[ti + 2] = vi + 2;
                tris[ti + 3] = vi + 0; tris[ti + 4] = vi + 2; tris[ti + 5] = vi + 3;
            }

            return new MeshFrame
            {
                Vertices = verts,
                Triangles = tris,
                Colors = colors
            };
        }

        /// <summary>
        /// 创建默认 GPU 材质（使用 PointCloud.shader）。
        /// </summary>
        private static Material CreateDefaultMaterial()
        {
            var shader = Shader.Find("LiftingTwin/PointCloud");
            if (shader != null)
                return new Material(shader);
            return null;
        }
    }
}
