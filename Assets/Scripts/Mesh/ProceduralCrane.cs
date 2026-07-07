// -----------------------------------------------------------------------
// LiftingTwin - 程序化移动式起重机网格生成器
//
// 用途：
//   生成简化的移动式起重机模型用于开发测试。
//   分为底盘、吊臂、吊钩三部分，可各自独立控制。
// -----------------------------------------------------------------------

using UnityEngine;

namespace LiftingTwin.Mesh
{
    /// <summary>
    /// 程序化移动式起重机生成器。
    /// 生成底盘、吊臂、吊钩三个独立的 MeshFrame。
    /// </summary>
    public static class ProceduralCrane
    {
        /// <summary>
        /// 生成起重机底盘（车体、驾驶室、履带、配重）。
        /// </summary>
        public static MeshFrame GenerateChassis()
        {
            var verts = new System.Collections.Generic.List<Vector3>();
            var tris = new System.Collections.Generic.List<int>();
            var colors = new System.Collections.Generic.List<Color>();
            var normals = new System.Collections.Generic.List<Vector3>();

            var yellow = new Color(0.95f, 0.75f, 0.05f);
            var darkGray = new Color(0.25f, 0.25f, 0.25f);
            var black = new Color(0.1f, 0.1f, 0.1f);

            // 主车体
            AddBox(verts, tris, colors, normals,
                new Vector3(0, 0.35f, 0), new Vector3(2.8f, 0.5f, 1.4f), yellow);

            // 驾驶室
            AddBox(verts, tris, colors, normals,
                new Vector3(0.6f, 0.75f, 0), new Vector3(0.8f, 0.5f, 1.0f), yellow);

            // 车窗（深色）
            AddBox(verts, tris, colors, normals,
                new Vector3(0.8f, 0.85f, 0), new Vector3(0.4f, 0.3f, 0.8f), new Color(0.3f, 0.5f, 0.8f, 0.7f));

            // 配重
            AddBox(verts, tris, colors, normals,
                new Vector3(-0.9f, 0.45f, 0), new Vector3(0.6f, 0.3f, 1.0f), darkGray);

            // 左右履带
            AddBox(verts, tris, colors, normals,
                new Vector3(0, 0.1f, -0.8f), new Vector3(2.6f, 0.2f, 0.3f), black);
            AddBox(verts, tris, colors, normals,
                new Vector3(0, 0.1f, 0.8f), new Vector3(2.6f, 0.2f, 0.3f), black);

            // 回转平台（吊臂基座）
            AddBox(verts, tris, colors, normals,
                new Vector3(0.3f, 0.7f, 0), new Vector3(0.5f, 0.15f, 0.5f), darkGray);

            return new MeshFrame
            {
                Vertices = verts.ToArray(),
                Triangles = tris.ToArray(),
                Colors = colors.ToArray(),
                Normals = normals.ToArray()
            };
        }

        /// <summary>
        /// 生成吊臂。从基座位置以指定角度伸出。
        /// </summary>
        /// <param name="angleDeg">与水平面夹角（度），0=水平，90=竖直</param>
        /// <param name="length">吊臂长度</param>
        public static MeshFrame GenerateBoom(float angleDeg = 45f, float length = 4.5f)
        {
            var verts = new System.Collections.Generic.List<Vector3>();
            var tris = new System.Collections.Generic.List<int>();
            var colors = new System.Collections.Generic.List<Color>();
            var normals = new System.Collections.Generic.List<Vector3>();

            var boomColor = new Color(0.9f, 0.7f, 0.05f);
            var accentColor = new Color(0.8f, 0.3f, 0.0f); // 警示色条纹

            // 吊臂基座铰点（车体回转平台上表面中心偏前）
            Vector3 pivot = new Vector3(0.3f, 0.85f, 0);

            float rad = angleDeg * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0);
            Vector3 tip = pivot + dir * length;

            // 主臂（变截面的长箱体）
            // 根部粗，顶端细
            float baseW = 0.2f, baseH = 0.25f;
            float tipW = 0.08f, tipH = 0.1f;

            // 把吊臂分成几段，每段一个箱体
            int segments = 6;
            for (int i = 0; i < segments; i++)
            {
                float t0 = (float)i / segments;
                float t1 = (float)(i + 1) / segments;
                Vector3 p0 = pivot + dir * (length * t0);
                Vector3 p1 = pivot + dir * (length * t1);
                float w0 = Mathf.Lerp(baseW, tipW, t0);
                float w1 = Mathf.Lerp(baseW, tipW, t1);
                float h0 = Mathf.Lerp(baseH, tipH, t0);
                float h1 = Mathf.Lerp(baseH, tipH, t1);

                AddOrientedBox(verts, tris, colors, normals,
                    p0, p1, w0, w1, h0, h1,
                    i % 2 == 0 ? boomColor : accentColor);
            }

            // 吊臂顶端滑轮
            AddBox(verts, tris, colors, normals,
                tip + new Vector3(0, 0, 0), new Vector3(0.15f, 0.15f, 0.3f), new Color(0.3f, 0.3f, 0.3f));

            return new MeshFrame
            {
                Vertices = verts.ToArray(),
                Triangles = tris.ToArray(),
                Colors = colors.ToArray(),
                Normals = normals.ToArray()
            };
        }

        /// <summary>
        /// 生成吊钩和钢丝绳。
        /// </summary>
        /// <param name="boomTip">吊臂顶端世界坐标</param>
        /// <param name="cableLength">钢丝绳长度</param>
        public static MeshFrame GenerateHook(Vector3 boomTip, float cableLength = 1.5f)
        {
            var verts = new System.Collections.Generic.List<Vector3>();
            var tris = new System.Collections.Generic.List<int>();
            var colors = new System.Collections.Generic.List<Color>();
            var normals = new System.Collections.Generic.List<Vector3>();

            var cableColor = new Color(0.2f, 0.2f, 0.2f);
            var hookColor = new Color(0.6f, 0.15f, 0.05f);

            Vector3 hookPos = boomTip + Vector3.down * cableLength;

            // 钢丝绳（细长柱）
            AddBox(verts, tris, colors, normals,
                (boomTip + hookPos) * 0.5f,
                new Vector3(0.03f, cableLength, 0.03f), cableColor);

            // 吊钩主体（红色，位于钢丝绳底端）
            AddBox(verts, tris, colors, normals,
                hookPos + new Vector3(0, -0.05f, 0),
                new Vector3(0.2f, 0.15f, 0.12f), hookColor);

            // 钩子底部弧形（简化为两个小方块）
            AddBox(verts, tris, colors, normals,
                hookPos + new Vector3(0, -0.15f, 0.1f),
                new Vector3(0.25f, 0.08f, 0.08f), hookColor);

            return new MeshFrame
            {
                Vertices = verts.ToArray(),
                Triangles = tris.ToArray(),
                Colors = colors.ToArray(),
                Normals = normals.ToArray()
            };
        }

        /// <summary>
        /// 计算吊臂顶端位置（方便外部定位吊钩）。
        /// </summary>
        public static Vector3 GetBoomTip(Vector3 pivot, float angleDeg, float length)
        {
            float rad = angleDeg * Mathf.Deg2Rad;
            return pivot + new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0) * length;
        }

        // ---- 内部几何辅助 ----

        /// <summary>轴对齐箱子</summary>
        private static void AddBox(
            System.Collections.Generic.List<Vector3> verts,
            System.Collections.Generic.List<int> tris,
            System.Collections.Generic.List<Color> colors,
            System.Collections.Generic.List<Vector3> normals,
            Vector3 center, Vector3 size, Color color)
        {
            int vi = verts.Count;
            float hx = size.x * 0.5f, hy = size.y * 0.5f, hz = size.z * 0.5f;

            // 8 个顶点
            verts.Add(center + new Vector3(-hx, -hy, -hz));
            verts.Add(center + new Vector3(hx, -hy, -hz));
            verts.Add(center + new Vector3(hx, hy, -hz));
            verts.Add(center + new Vector3(-hx, hy, -hz));
            verts.Add(center + new Vector3(-hx, -hy, hz));
            verts.Add(center + new Vector3(hx, -hy, hz));
            verts.Add(center + new Vector3(hx, hy, hz));
            verts.Add(center + new Vector3(-hx, hy, hz));

            for (int i = 0; i < 8; i++) colors.Add(color);
            for (int i = 0; i < 8; i++) normals.Add(Vector3.zero);

            // 6 个面（12 个三角形）
            int[] faces = {
                0,1,2,0,2,3,  // -Z
                4,7,6,4,6,5,  // +Z
                0,4,5,0,5,1,  // -Y
                3,2,6,3,6,7,  // +Y
                0,3,7,0,7,4,  // -X
                1,5,6,1,6,2   // +X
            };
            foreach (int idx in faces) tris.Add(vi + idx);
        }

        /// <summary>
        /// 在两个点之间生成沿方向延伸的箱体（两个端面尺寸可以不同）。
        /// </summary>
        private static void AddOrientedBox(
            System.Collections.Generic.List<Vector3> verts,
            System.Collections.Generic.List<int> tris,
            System.Collections.Generic.List<Color> colors,
            System.Collections.Generic.List<Vector3> normals,
            Vector3 p1, Vector3 p2,
            float w1, float w2, float h1, float h2, Color color)
        {
            var dir = (p2 - p1).normalized;
            float len = (p2 - p1).magnitude;
            if (len < 0.001f) return;

            // 计算局部基向量
            var up = Vector3.Cross(dir, Vector3.right).normalized;
            if (up.sqrMagnitude < 0.001f)
                up = Vector3.Cross(dir, Vector3.forward).normalized;
            var right = Vector3.Cross(dir, up).normalized;
            up = Vector3.Cross(right, dir).normalized;

            int vi = verts.Count;

            // 根部 4 个顶点
            verts.Add(p1 - right * w1 - up * h1);
            verts.Add(p1 + right * w1 - up * h1);
            verts.Add(p1 + right * w1 + up * h1);
            verts.Add(p1 - right * w1 + up * h1);
            // 顶端 4 个顶点
            verts.Add(p2 - right * w2 - up * h2);
            verts.Add(p2 + right * w2 - up * h2);
            verts.Add(p2 + right * w2 + up * h2);
            verts.Add(p2 - right * w2 + up * h2);

            for (int i = 0; i < 8; i++)
            {
                colors.Add(color);
                normals.Add(Vector3.zero);
            }

            // 6 个面
            tris.Add(vi + 1); tris.Add(vi + 5); tris.Add(vi + 6);
            tris.Add(vi + 1); tris.Add(vi + 6); tris.Add(vi + 2);
            tris.Add(vi + 0); tris.Add(vi + 3); tris.Add(vi + 7);
            tris.Add(vi + 0); tris.Add(vi + 7); tris.Add(vi + 4);
            tris.Add(vi + 3); tris.Add(vi + 2); tris.Add(vi + 6);
            tris.Add(vi + 3); tris.Add(vi + 6); tris.Add(vi + 7);
            tris.Add(vi + 0); tris.Add(vi + 4); tris.Add(vi + 5);
            tris.Add(vi + 0); tris.Add(vi + 5); tris.Add(vi + 1);
            tris.Add(vi + 0); tris.Add(vi + 1); tris.Add(vi + 2);
            tris.Add(vi + 0); tris.Add(vi + 2); tris.Add(vi + 3);
            tris.Add(vi + 4); tris.Add(vi + 7); tris.Add(vi + 6);
            tris.Add(vi + 4); tris.Add(vi + 6); tris.Add(vi + 5);
        }
    }
}
