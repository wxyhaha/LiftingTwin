// -----------------------------------------------------------------------
// LiftingTwin - 网格测试控制器（开发调试用）
//
// 用途：
//   开发阶段验证 Mesh 动态更新功能。
//   挂载到任意 GameObject 上，自动创建测试网格并驱动顶点变形动画。
//   上线或接入真实数据后可移除或禁用此脚本。
// -----------------------------------------------------------------------

using LiftingTwin.Utils;
using UnityEngine;

namespace LiftingTwin.Mesh
{
    /// <summary>
    /// 网格系统开发测试控制器。
    /// 挂载到场景中任意 GameObject（如 Bootstrap），
    /// 在 Start 时创建动态网格并驱动顶点变形动画。
    /// </summary>
    public class MeshTestController : MonoBehaviour
    {
        [Header("Mesh Object")]
        [Tooltip("指定现有的 MeshView（可选）。不指定则自动创建。")]
        public MeshView targetMeshView;

        [Header("Animation")]
        [Tooltip("顶点变形幅度")]
        public float deformAmplitude = 0.5f;

        [Tooltip("变形频率")]
        public float deformFrequency = 1.5f;

        private MeshView _meshView;
        private MeshFrame _baseFrame;
        private float _time;
        private GameObject _meshObject;

        private void Start()
        {
            if (targetMeshView != null)
            {
                _meshView = targetMeshView;
            }
            else
            {
                _meshObject = CreateMeshObject();
                _meshView = _meshObject.GetComponent<MeshView>();
            }

            // 确保 MeshRenderer 有 URP 兼容材质
            EnsureMaterial();

            _baseFrame = CreateTestCube();
            _meshView.ApplyFrame(_baseFrame);
            Log.Info("Mesh", "MeshTestController 已加载测试网格，顶点数={0}", _baseFrame.Vertices.Length);
        }

        private void Update()
        {
            _time += Time.deltaTime;
            var animFrame = AnimateFrame(_baseFrame, _time);
            _meshView.ApplyFrame(animFrame);
        }

        /// <summary>
        /// 在场景中创建一个带 MeshView 的 GameObject。
        /// </summary>
        private GameObject CreateMeshObject()
        {
            var go = new GameObject("Dynamic Mesh (Test)");
            go.transform.position = new Vector3(3, 0.5f, 3);
            go.AddComponent<MeshFilter>();
            go.AddComponent<MeshRenderer>();
            go.AddComponent<MeshView>();
            return go;
        }

        /// <summary>
        /// 确保 MeshRenderer 有可用的材质。如果没有，创建一个 URP Lit 材质。
        /// </summary>
        private void EnsureMaterial()
        {
            var renderer = _meshView.GetComponent<MeshRenderer>();
            if (renderer.sharedMaterial != null && renderer.sharedMaterial.shader.name != "Hidden/InternalErrorShader")
                return;

            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader != null)
            {
                var mat = new Material(shader);
                mat.color = new Color(0.3f, 0.6f, 1.0f); // 浅蓝色
                renderer.material = mat;
                Log.Debug("Mesh", "MeshTestController 已创建默认 URP Lit 材质");
            }
            else
            {
                Log.Warn("Mesh", "未找到 URP/Lit Shader，使用 MeshRenderer 默认材质");
            }
        }

        /// <summary>
        /// 创建一个彩色立方体网格（24 顶点，6 面独立法线）。
        /// </summary>
        private static MeshFrame CreateTestCube()
        {
            // 标准立方体：6 个面，每面 4 个独立顶点（共 24 顶点）
            var verts = new Vector3[24];
            var tris = new int[36];
            var colors = new Color[24];
            var normals = new Vector3[24];

            // 6 个面的方向
            var faceDirections = new[] {
                Vector3.forward, Vector3.back,
                Vector3.up, Vector3.down,
                Vector3.right, Vector3.left
            };

            // 6 个面的颜色
            var faceColors = new[] {
                Color.red, Color.cyan,
                Color.green, Color.yellow,
                Color.blue, Color.magenta
            };

            var vi = 0;
            var ti = 0;

            for (int f = 0; f < 6; f++)
            {
                var dir = faceDirections[f];
                var color = faceColors[f];

                // 计算该面朝向的 right 和 up 向量
                var upDir = f < 2
                    ? Vector3.up
                    : (f == 2 ? Vector3.forward : (f == 3 ? Vector3.back : Vector3.up));
                var rightDir = Vector3.Cross(dir, upDir).normalized;
                var topDir = Vector3.Cross(rightDir, dir).normalized;

                for (int c = 0; c < 4; c++)
                {
                    float u = (c == 0 || c == 3) ? -0.5f : 0.5f;
                    float v = (c == 0 || c == 1) ? -0.5f : 0.5f;
                    verts[vi + c] = dir * 0.5f + rightDir * u + topDir * v;
                    colors[vi + c] = color;
                    normals[vi + c] = dir;
                }

                tris[ti + 0] = vi + 0; tris[ti + 1] = vi + 1; tris[ti + 2] = vi + 2;
                tris[ti + 3] = vi + 0; tris[ti + 4] = vi + 2; tris[ti + 5] = vi + 3;

                vi += 4;
                ti += 6;
            }

            return new MeshFrame
            {
                Vertices = verts,
                Triangles = tris,
                Normals = normals,
                Colors = colors
            };
        }

        /// <summary>
        /// 对基础网格做顶点变形动画。
        /// 沿法线方向推动顶点，产生呼吸/脉动效果。
        /// </summary>
        private static MeshFrame AnimateFrame(MeshFrame baseFrame, float time)
        {
            var verts = new Vector3[baseFrame.Vertices.Length];
            var deform = Mathf.Sin(time * 1.5f) * 0.3f;

            for (int i = 0; i < verts.Length; i++)
            {
                var dir = baseFrame.Normals != null ? baseFrame.Normals[i] : baseFrame.Vertices[i].normalized;
                verts[i] = baseFrame.Vertices[i] + dir * deform;
            }

            return new MeshFrame
            {
                Vertices = verts,
                Triangles = baseFrame.Triangles,
                Normals = baseFrame.Normals,
                Colors = baseFrame.Colors,
                Uvs = baseFrame.Uvs
            };
        }
    }
}
