// -----------------------------------------------------------------------
// LiftingTwin - DynamicMesh 单元测试
//
// 测试策略：
//   DynamicMesh 是纯 C# 类，不依赖 MonoBehaviour 生命周期，
//   使用 Edit Mode 测试即可覆盖所有网格更新逻辑。
//
// 测试范围：
//   - 构造函数创建有效 Mesh
//   - UpdateFrame 更新顶点和索引
//   - UpdateFrame 处理空/无效帧
//   - Clear 清空网格
//   - 多次更新覆盖旧数据
//   - 法线自动计算
// -----------------------------------------------------------------------

using NUnit.Framework;
using UnityEngine;
using LiftingTwin.Mesh;

namespace LiftingTwin.Tests.Editor
{
    /// <summary>
    /// DynamicMesh 的编辑模式单元测试。
    /// </summary>
    public class DynamicMeshTests
    {
        private const float Epsilon = 1e-5f;

        private static MeshFrame CreateTriangleFrame()
        {
            return new MeshFrame
            {
                Vertices = new[]
                {
                    new Vector3(0, 0, 0),
                    new Vector3(1, 0, 0),
                    new Vector3(0, 1, 0)
                },
                Triangles = new[] { 0, 1, 2 }
            };
        }

        private static MeshFrame CreateQuadFrame()
        {
            return new MeshFrame
            {
                Vertices = new[]
                {
                    new Vector3(-0.5f, 0, -0.5f),
                    new Vector3(0.5f, 0, -0.5f),
                    new Vector3(0.5f, 0, 0.5f),
                    new Vector3(-0.5f, 0, 0.5f)
                },
                Triangles = new[] { 0, 1, 2, 0, 2, 3 },
                Colors = new[]
                {
                    Color.red,
                    Color.green,
                    Color.blue,
                    Color.white
                }
            };
        }

        [Test]
        public void 构造函数_CreateMesh_标记为动态()
        {
            var dm = new DynamicMesh();
            Assert.NotNull(dm.Mesh);
            Assert.That(dm.Mesh.isReadable, Is.True);
        }

        [Test]
        public void UpdateFrame_三角形_顶点和索引匹配()
        {
            var dm = new DynamicMesh();
            var frame = CreateTriangleFrame();

            dm.UpdateFrame(frame);

            Assert.That(dm.Mesh.vertexCount, Is.EqualTo(3));
            Assert.That(dm.Mesh.triangles.Length, Is.EqualTo(3));
            Assert.That(dm.Mesh.vertices[0], Is.EqualTo(new Vector3(0, 0, 0)).Within(Epsilon));
            Assert.That(dm.Mesh.vertices[1], Is.EqualTo(new Vector3(1, 0, 0)).Within(Epsilon));
            Assert.That(dm.Mesh.vertices[2], Is.EqualTo(new Vector3(0, 1, 0)).Within(Epsilon));
        }

        [Test]
        public void UpdateFrame_正方形带颜色_颜色正确()
        {
            var dm = new DynamicMesh();
            var frame = CreateQuadFrame();

            dm.UpdateFrame(frame);

            Assert.That(dm.Mesh.vertexCount, Is.EqualTo(4));
            Assert.That(dm.Mesh.triangles.Length, Is.EqualTo(6));
            Assert.That(dm.Mesh.colors.Length, Is.EqualTo(4));
            Assert.That(dm.Mesh.colors[0], Is.EqualTo(Color.red));
            Assert.That(dm.Mesh.colors[1], Is.EqualTo(Color.green));
        }

        [Test]
        public void UpdateFrame_无法线_自动计算()
        {
            var dm = new DynamicMesh();
            var frame = CreateTriangleFrame();

            dm.UpdateFrame(frame);

            Assert.That(dm.Mesh.normals.Length, Is.EqualTo(3));
            // 三角形法线应为 Z 轴方向
            Assert.That(dm.Mesh.normals[0].z, Is.LessThan(0));
        }

        [Test]
        public void UpdateFrame_空顶点数组_不崩溃()
        {
            var dm = new DynamicMesh();
            var emptyFrame = new MeshFrame
            {
                Vertices = System.Array.Empty<Vector3>(),
                Triangles = System.Array.Empty<int>()
            };

            Assert.DoesNotThrow(() => dm.UpdateFrame(emptyFrame));
            Assert.That(dm.Mesh.vertexCount, Is.EqualTo(0));
        }

        [Test]
        public void UpdateFrame_默认结构体_不崩溃()
        {
            var dm = new DynamicMesh();

            Assert.DoesNotThrow(() => dm.UpdateFrame(default));
            Assert.That(dm.Mesh.vertexCount, Is.EqualTo(0));
        }

        [Test]
        public void Clear_清空后_顶点为零()
        {
            var dm = new DynamicMesh();
            dm.UpdateFrame(CreateTriangleFrame());
            Assert.That(dm.Mesh.vertexCount, Is.EqualTo(3));

            dm.Clear();

            Assert.That(dm.Mesh.vertexCount, Is.EqualTo(0));
        }

        [Test]
        public void 多次更新_数据覆盖正确()
        {
            var dm = new DynamicMesh();
            dm.UpdateFrame(CreateTriangleFrame());
            Assert.That(dm.Mesh.vertexCount, Is.EqualTo(3));

            dm.UpdateFrame(CreateQuadFrame());
            Assert.That(dm.Mesh.vertexCount, Is.EqualTo(4));
            Assert.That(dm.Mesh.triangles.Length, Is.EqualTo(6));
        }

        [Test]
        public void UpdateFrame_带法线_使用提供法线()
        {
            var normals = new[]
            {
                Vector3.up,
                Vector3.up,
                Vector3.up
            };
            var frame = new MeshFrame
            {
                Vertices = new[]
                {
                    new Vector3(0, 0, 0),
                    new Vector3(1, 0, 0),
                    new Vector3(0, 1, 0)
                },
                Triangles = new[] { 0, 1, 2 },
                Normals = normals
            };

            var dm = new DynamicMesh();
            dm.UpdateFrame(frame);

            Assert.That(dm.Mesh.normals.Length, Is.EqualTo(3));
            Assert.That(dm.Mesh.normals[0], Is.EqualTo(Vector3.up).Within(Epsilon));
        }
    }
}
