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
    /// 在 Start 时创建程序化生成的输电塔，并模拟微风摇摆。
    /// </summary>
    public class MeshTestController : MonoBehaviour
    {
        [Header("Mesh Object")]
        [Tooltip("指定现有的 MeshView（可选）。不指定则自动创建。")]
        public MeshView targetMeshView;

        [Header("Animation")]
        [Tooltip("模拟风速（0 = 静止）")]
        [Range(0f, 5f)]
        public float windSpeed = 1.5f;

        [Tooltip("风致摇摆幅度")]
        [Range(0f, 0.5f)]
        public float swayAmplitude = 0.08f;

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

            EnsureMaterial();

            _baseFrame = ProceduralTower.Generate();
            _meshView.ApplyFrame(_baseFrame);
            Log.Info("Mesh", "MeshTestController 已加载输电塔网格，顶点数={0}", _baseFrame.Vertices.Length);
        }

        private void Update()
        {
            _time += Time.deltaTime;
            var animFrame = ApplyWindSway(_baseFrame, _time);
            _meshView.ApplyFrame(animFrame);
        }

        /// <summary>
        /// 在场景中创建一个带 MeshView 的 GameObject。
        /// 塔底在地面（y=0），位置在参考方块旁。
        /// </summary>
        private GameObject CreateMeshObject()
        {
            var go = new GameObject("Dynamic Mesh (Test)");
            go.transform.position = new Vector3(3, 0, 3);
            go.AddComponent<MeshFilter>();
            go.AddComponent<MeshRenderer>();
            go.AddComponent<MeshView>();
            return go;
        }

        /// <summary>
        /// 确保 MeshRenderer 有 URP 兼容材质。
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
                mat.color = new Color(0.55f, 0.55f, 0.58f); // 钢灰色
                renderer.material = mat;
                Log.Debug("Mesh", "MeshTestController 已创建默认 URP Lit 材质");
            }
            else
            {
                Log.Warn("Mesh", "未找到 URP/Lit Shader，使用 MeshRenderer 默认材质");
            }
        }

        /// <summary>
        /// 模拟风致摇摆：塔越高处摆动幅度越大。
        /// </summary>
        private static MeshFrame ApplyWindSway(MeshFrame baseFrame, float time)
        {
            if (baseFrame.Vertices == null || baseFrame.Vertices.Length == 0)
                return baseFrame;

            var verts = new Vector3[baseFrame.Vertices.Length];
            float swayX = Mathf.Sin(time * 1.2f) * 0.08f;
            float swayZ = Mathf.Sin(time * 0.9f + 0.5f) * 0.05f;

            // 找最高点 Y 值用于归一化摇摆幅度
            float maxY = float.MinValue;
            for (int i = 0; i < baseFrame.Vertices.Length; i++)
            {
                if (baseFrame.Vertices[i].y > maxY)
                    maxY = baseFrame.Vertices[i].y;
            }

            for (int i = 0; i < verts.Length; i++)
            {
                var v = baseFrame.Vertices[i];
                float t = maxY > 0.01f ? v.y / maxY : 0f; // 0 在底部，1 在顶部
                verts[i] = new Vector3(
                    v.x + swayX * t,
                    v.y,
                    v.z + swayZ * t
                );
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
