// -----------------------------------------------------------------------
// LiftingTwin - 程序化输电塔网格生成器
//
// 用途：
//   在没有真实数据的情况下，生成近似输电塔的网格用于开发测试。
//   塔身为格构式结构，带横隔面斜撑和顶部横担。
// -----------------------------------------------------------------------

using UnityEngine;

namespace LiftingTwin.Mesh
{
    /// <summary>
    /// 程序化输电塔网格生成器。生成一个格构式铁塔的 MeshFrame。
    /// </summary>
    public static class ProceduralTower
    {
        /// <summary>
        /// 生成一个输电塔网格。
        /// </summary>
        /// <param name="height">塔身总高度</param>
        /// <param name="baseWidth">底部宽度</param>
        /// <param name="topWidth">顶部宽度（横担以下）</param>
        /// <param name="segmentCount">塔身分段数</param>
        /// <param name="armLength">横担伸出长度</param>
        /// <returns>输电塔 MeshFrame</returns>
        public static MeshFrame Generate(float height = 12f, float baseWidth = 3f,
            float topWidth = 1f, int segmentCount = 6, float armLength = 2f)
        {
            var verts = new System.Collections.Generic.List<Vector3>();
            var tris = new System.Collections.Generic.List<int>();
            var colors = new System.Collections.Generic.List<Color>();
            var normals = new System.Collections.Generic.List<Vector3>();

            var steelColor = new Color(0.55f, 0.55f, 0.58f); // 钢灰色
            var armColor = new Color(0.6f, 0.3f, 0.1f);      // 锈红色（横担）

            // 塔身主材 + 横隔面
            float segHeight = height / segmentCount;
            for (int i = 0; i < segmentCount; i++)
            {
                float t0 = (float)i / segmentCount;
                float t1 = (float)(i + 1) / segmentCount;
                float w0 = Mathf.Lerp(baseWidth, topWidth, t0) * 0.5f;
                float w1 = Mathf.Lerp(baseWidth, topWidth, t1) * 0.5f;
                float y0 = i * segHeight;
                float y1 = (i + 1) * segHeight;

                // 四个角点在底部和顶部的坐标
                Vector3[] btm = {
                    new Vector3(-w0, y0, -w0), new Vector3(w0, y0, -w0),
                    new Vector3(w0, y0, w0),  new Vector3(-w0, y0, w0)
                };
                Vector3[] top = {
                    new Vector3(-w1, y1, -w1), new Vector3(w1, y1, -w1),
                    new Vector3(w1, y1, w1),  new Vector3(-w1, y1, w1)
                };

                // 四条主立柱（每段）
                for (int j = 0; j < 4; j++)
                    AddBeam(verts, tris, colors, normals, btm[j], top[j], 0.08f, steelColor);

                // 横隔面斜材（X 形）
                AddBeam(verts, tris, colors, normals, top[0], top[2], 0.05f, steelColor);
                AddBeam(verts, tris, colors, normals, top[1], top[3], 0.05f, steelColor);

                // 四个面的交叉斜材
                int[] a = { 0, 1, 2, 3 };
                int[] b = { 1, 2, 3, 0 };
                for (int j = 0; j < 4; j++)
                {
                    AddBeam(verts, tris, colors, normals, btm[a[j]], top[b[j]], 0.05f, steelColor);
                    AddBeam(verts, tris, colors, normals, btm[b[j]], top[a[j]], 0.05f, steelColor);
                }
            }

            // 顶部横担（十字形）
            float topY = height;
            float tw = topWidth * 0.5f;
            // 横担沿 X 方向延伸
            Vector3 armCenter = new Vector3(0, topY, tw * 0.3f);
            AddBeam(verts, tris, colors, normals,
                armCenter + new Vector3(-armLength, 0, 0),
                armCenter + new Vector3(armLength, 0, 0), 0.1f, armColor);
            // 横担沿 Z 方向延伸
            armCenter = new Vector3(tw * 0.3f, topY, 0);
            AddBeam(verts, tris, colors, normals,
                armCenter + new Vector3(0, 0, -armLength),
                armCenter + new Vector3(0, 0, armLength), 0.1f, armColor);

            // 塔顶小尖
            AddBeam(verts, tris, colors, normals,
                new Vector3(0, topY, 0), new Vector3(0, topY + 0.8f, 0), 0.06f, steelColor);

            return new MeshFrame
            {
                Vertices = verts.ToArray(),
                Triangles = tris.ToArray(),
                Colors = colors.ToArray(),
                Normals = normals.ToArray()
            };
        }

        /// <summary>
        /// 在两个点之间添加一个矩形截面的梁。
        /// </summary>
        private static void AddBeam(
            System.Collections.Generic.List<Vector3> verts,
            System.Collections.Generic.List<int> tris,
            System.Collections.Generic.List<Color> colors,
            System.Collections.Generic.List<Vector3> normals,
            Vector3 p1, Vector3 p2, float thickness, Color color)
        {
            var dir = (p2 - p1).normalized;
            float len = (p2 - p1).magnitude;
            if (len < 0.001f) return;

            // 计算局部坐标系
            var up = Vector3.Cross(dir, Vector3.right).normalized;
            if (up.sqrMagnitude < 0.001f)
                up = Vector3.Cross(dir, Vector3.forward).normalized;
            var right = Vector3.Cross(dir, up).normalized;
            up = Vector3.Cross(right, dir).normalized;

            float hw = thickness * 0.5f;
            var r = right * hw;
            var u = up * hw;

            int vi = verts.Count;

            // 8 个顶点（p1 端 4 个，p2 端 4 个）
            verts.Add(p1 - r - u); // 0
            verts.Add(p1 + r - u); // 1
            verts.Add(p1 + r + u); // 2
            verts.Add(p1 - r + u); // 3
            verts.Add(p2 - r - u); // 4
            verts.Add(p2 + r - u); // 5
            verts.Add(p2 + r + u); // 6
            verts.Add(p2 - r + u); // 7

            for (int i = 0; i < 8; i++)
            {
                colors.Add(color);
                normals.Add(Vector3.zero); // 填充占位
            }

            // 6 个面，每个面 2 个三角形（12 个索引）
            // 右：1-5-6-2
            tris.Add(vi + 1); tris.Add(vi + 5); tris.Add(vi + 6);
            tris.Add(vi + 1); tris.Add(vi + 6); tris.Add(vi + 2);
            // 左：0-3-7-4
            tris.Add(vi + 0); tris.Add(vi + 3); tris.Add(vi + 7);
            tris.Add(vi + 0); tris.Add(vi + 7); tris.Add(vi + 4);
            // 上：3-2-6-7
            tris.Add(vi + 3); tris.Add(vi + 2); tris.Add(vi + 6);
            tris.Add(vi + 3); tris.Add(vi + 6); tris.Add(vi + 7);
            // 下：0-4-5-1
            tris.Add(vi + 0); tris.Add(vi + 4); tris.Add(vi + 5);
            tris.Add(vi + 0); tris.Add(vi + 5); tris.Add(vi + 1);
            // 前（p1 端）：0-1-2-3
            tris.Add(vi + 0); tris.Add(vi + 1); tris.Add(vi + 2);
            tris.Add(vi + 0); tris.Add(vi + 2); tris.Add(vi + 3);
            // 后（p2 端）：4-7-6-5
            tris.Add(vi + 4); tris.Add(vi + 7); tris.Add(vi + 6);
            tris.Add(vi + 4); tris.Add(vi + 6); tris.Add(vi + 5);
        }
    }
}
