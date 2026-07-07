// -----------------------------------------------------------------------
// LiftingTwin - 场景初始化器
//
// 用途：
//   在 Play Mode 启动时自动创建地面和参考物体，
//   方便调试和验证相机控制功能。
//   挂载到 Bootstrap GameObject 上。
//
// 设计原则：
//   仅在开发和调试阶段使用，后续可以移除或禁用。
// -----------------------------------------------------------------------

using LiftingTwin.Mesh;
using LiftingTwin.Utils;
using UnityEngine;

namespace LiftingTwin.Runtime
{
    /// <summary>
    /// 场景初始化器。在 Start 时创建地面平面和参考物体，
    /// 为相机控制提供视觉参照。
    /// </summary>
    public class SceneInitializer : MonoBehaviour
    {
        [Header("Ground")]
        [Tooltip("地面大小")]
        public Vector2 groundSize = new Vector2(20, 20);

        [Tooltip("地面材质，可选。不指定则使用默认 URP 材质")]
        public Material groundMaterial;

        [Header("Reference Objects")]
        [Tooltip("是否在原点创建参考立方体")]
        public bool createReferenceCube = true;

        [Tooltip("参考立方体材质，可选")]
        public Material cubeMaterial;

        [Header("Grid")]
        [Tooltip("是否绘制辅助网格线")]
        public bool showGrid = true;

        [Tooltip("网格大小")]
        public int gridSize = 10;

        [Tooltip("网格间距")]
        public float gridSpacing = 1.0f;

        private void Start()
        {
            CreateGround();
            if (createReferenceCube)
                CreateReferenceCube();

            if (showGrid)
                CreateGridHelper();

            // 开发阶段：创建动态网格测试对象
            CreateDynamicMeshTest();

            Log.Info("Runtime", "SceneInitializer: 场景初始化完成");
        }

        private void CreateGround()
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.SetParent(null);
            ground.transform.localScale = new Vector3(groundSize.x / 10f, 1, groundSize.y / 10f);

            if (groundMaterial != null)
            {
                var renderer = ground.GetComponent<MeshRenderer>();
                renderer.material = groundMaterial;
            }

            Log.Debug("Runtime", "SceneInitializer: 创建地面 {0}x{1}", groundSize.x, groundSize.y);
        }

        private void CreateReferenceCube()
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "Reference Cube";
            cube.transform.SetParent(null);
            cube.transform.position = new Vector3(0, 0.5f, 0);

            if (cubeMaterial != null)
            {
                var renderer = cube.GetComponent<MeshRenderer>();
                renderer.material = cubeMaterial;
            }

            // 在四角加小柱子增强空间感
            CreateCornerPillar(new Vector3(-3, 0.25f, -3));
            CreateCornerPillar(new Vector3(3, 0.25f, -3));
            CreateCornerPillar(new Vector3(-3, 0.25f, 3));
            CreateCornerPillar(new Vector3(3, 0.25f, 3));

            Log.Debug("Runtime", "SceneInitializer: 创建参考立方体 + 四角标记");
        }

        private void CreateCornerPillar(Vector3 position)
        {
            var pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pillar.name = "Corner Marker";
            pillar.transform.SetParent(null);
            pillar.transform.position = position;
            pillar.transform.localScale = new Vector3(0.2f, 0.25f, 0.2f);

            // 降低亮度方便视觉区分
            var renderer = pillar.GetComponent<MeshRenderer>();
            renderer.material.color = Color.gray * 0.7f;
        }

        private void CreateGridHelper()
        {
            var gridGo = new GameObject("Grid Helper");
            gridGo.transform.SetParent(null);
            var grid = gridGo.AddComponent<GridDebug>();
            grid.gridSize = gridSize;
            grid.gridSpacing = gridSpacing;
        }

        /// <summary>
        /// 开发阶段：创建 MeshManager 并添加多个测试对象，演示多物体独立控制。
        /// </summary>
        private void CreateDynamicMeshTest()
        {
            // 创建 MeshManager（也可拖拽 MeshManagerView 到 Bootstrap 上）
            var mgrObj = new GameObject("Mesh Manager");
            mgrObj.transform.SetParent(transform);
            var meshView = mgrObj.AddComponent<MeshManagerView>();

            // 1. 输电塔（风致摇摆）
            var towerFrame = ProceduralTower.Generate();
            int towerId = meshView.AddObject("输电塔 (测试)", towerFrame);
            meshView.SetPosition(towerId, new Vector3(3, 0, 3));

            // 2. 吊装构件（简单的红色箱子，上下升降）
            var partFrame = CreateTestBox(0.8f, 0.5f, 0.6f, new Color(0.8f, 0.2f, 0.1f));
            int partId = meshView.AddObject("吊装构件 (测试)", partFrame);
            meshView.SetPosition(partId, new Vector3(-3, 0, 3));

            // 驱动动画
            gameObject.AddComponent<MeshTestAnimator>()
                .AddTower(meshView, towerId, towerFrame)
                .AddLiftingPart(meshView, partId, partFrame, height: 4f, speed: 0.8f);

            Log.Info("Runtime", "SceneInitializer: 创建 MeshManager 测试场景（输电塔 + 吊装构件）");
        }

        /// <summary>
        /// 创建一个简单的彩色箱子网格。
        /// </summary>
        private static MeshFrame CreateTestBox(float w, float h, float d, Color color)
        {
            var verts = new Vector3[24];
            var tris = new int[36];
            var colors = new Color[24];
            var normals = new Vector3[24];

            Vector3[] dirs = {
                Vector3.forward, Vector3.back, Vector3.up, Vector3.down,
                Vector3.right, Vector3.left
            };
            Color[] faceColors = { color, color, color, color, color, color };

            int vi = 0, ti = 0;
            for (int f = 0; f < 6; f++)
            {
                var dir = dirs[f];
                var upDir = f < 2 ? Vector3.up
                    : (f == 2 ? Vector3.forward : (f == 3 ? Vector3.back : Vector3.up));
                var rightDir = Vector3.Cross(dir, upDir).normalized;
                var topDir = Vector3.Cross(rightDir, dir).normalized;

                for (int c = 0; c < 4; c++)
                {
                    float u = (c == 0 || c == 3) ? -w * 0.5f : w * 0.5f;
                    float v = (c == 0 || c == 1) ? -h * 0.5f : h * 0.5f;
                    verts[vi + c] = dir * d * 0.5f + rightDir * u + topDir * v;
                    colors[vi + c] = faceColors[f];
                    normals[vi + c] = dir;
                }

                tris[ti + 0] = vi + 0; tris[ti + 1] = vi + 1; tris[ti + 2] = vi + 2;
                tris[ti + 3] = vi + 0; tris[ti + 4] = vi + 2; tris[ti + 5] = vi + 3;
                vi += 4; ti += 6;
            }

            return new MeshFrame
            {
                Vertices = verts, Triangles = tris,
                Normals = normals, Colors = colors
            };
        }

        /// <summary>
        /// 内部辅助类：在 Scene 视图中绘制网格线（Gizmos）。
        /// 仅在 Editor 中可见，Build 后不显示。
        /// </summary>
        private class GridDebug : MonoBehaviour
        {
            public int gridSize = 10;
            public float gridSpacing = 1.0f;

            private void OnDrawGizmos()
            {
                Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.3f);
                var half = gridSize * gridSpacing * 0.5f;

                for (int i = -gridSize / 2; i <= gridSize / 2; i++)
                {
                    var pos = i * gridSpacing;
                    Gizmos.DrawLine(
                        new Vector3(pos, 0, -half),
                        new Vector3(pos, 0, half)
                    );
                    Gizmos.DrawLine(
                        new Vector3(-half, 0, pos),
                        new Vector3(half, 0, pos)
                    );
                }

                // 坐标轴（X红色，Z蓝色）
                Gizmos.color = Color.red;
                Gizmos.DrawLine(Vector3.zero, new Vector3(2, 0, 0));
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(Vector3.zero, new Vector3(0, 0, 2));
            }
        }
    }
}
