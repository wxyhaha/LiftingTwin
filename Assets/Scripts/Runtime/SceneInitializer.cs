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
            // 地面和参考方块已在场景中静态放置，不再运行时重复创建
            // 保留 CreateGround/CreateReferenceCube 方法备用

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
        /// 开发阶段：在场景中创建一个动态网格测试对象。
        /// 验证 DynamicMesh + MeshView 系统正常工作。
        /// </summary>
        private void CreateDynamicMeshTest()
        {
            var go = new GameObject("Dynamic Mesh (Test)");
            go.transform.position = new Vector3(3, 0.5f, 3);
            go.AddComponent<MeshFilter>();
            go.AddComponent<MeshRenderer>();
            go.AddComponent<MeshView>();
            go.AddComponent<MeshTestController>();

            Log.Debug("Runtime", "SceneInitializer: 创建动态网格测试对象");
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
