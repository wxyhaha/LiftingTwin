// -----------------------------------------------------------------------
// LiftingTwin - Mesh 测试动画驱动器（开发调试用）
//
// 用途：
//   演示通过 MeshManager 管理多个 Mesh 对象并独立驱动动画。
//   模拟吊装场景：输电塔随风摇摆，移动式起重机回转起吊。
// -----------------------------------------------------------------------

using System.Collections.Generic;
using LiftingTwin.Utils;
using UnityEngine;

namespace LiftingTwin.Mesh
{
    /// <summary>
    /// 驱动 MeshManager 中多个对象的测试动画。
    /// SceneInitializer 在 Start 时创建并配置。
    /// </summary>
    public class MeshTestAnimator : MonoBehaviour
    {
        private readonly List<AnimatedEntry> _entries = new();

        private void Update()
        {
            float t = Time.time;
            for (int i = _entries.Count - 1; i >= 0; i--)
                _entries[i].Update(t);
        }

        /// <summary>
        /// 添加一个输电塔（风致摇摆）。
        /// </summary>
        public MeshTestAnimator AddTower(MeshManagerView mgr, int id, MeshFrame baseFrame)
        {
            _entries.Add(new TowerSwayEntry(mgr, id, baseFrame));
            Log.Info("Mesh", "MeshTestAnimator: 添加输电塔（摇摆）id={0}", id);
            return this;
        }

        /// <summary>
        /// 添加一个吊装构件（上下运动）。
        /// </summary>
        public MeshTestAnimator AddLiftingPart(MeshManagerView mgr, int id, MeshFrame baseFrame,
            float height = 4f, float speed = 0.8f)
        {
            _entries.Add(new LiftEntry(mgr, id, baseFrame, height, speed));
            Log.Info("Mesh", "MeshTestAnimator: 添加吊装构件（升降）id={0}", id);
            return this;
        }

        /// <summary>
        /// 添加一个旋转物体。
        /// </summary>
        public MeshTestAnimator AddRotatingPart(MeshManagerView mgr, int id,
            Vector3 axis, float speed = 30f)
        {
            _entries.Add(new RotateEntry(mgr, id, axis, speed));
            Log.Info("Mesh", "MeshTestAnimator: 添加旋转物体 id={0}", id);
            return this;
        }

        /// <summary>
        /// 添加一台移动式起重机（底盘回转 + 吊臂俯仰 + 吊钩升降）。
        /// </summary>
        public MeshTestAnimator AddCrane(MeshManagerView mgr,
            int chassisId, int boomId, int hookId)
        {
            _entries.Add(new CraneEntry(mgr, chassisId, boomId, hookId));
            Log.Info("Mesh", "MeshTestAnimator: 添加起重机（3 部件）");
            return this;
        }

        // ---- 内部动画条目抽象 ----

        private abstract class AnimatedEntry
        {
            protected readonly MeshManagerView Manager;
            protected readonly int ObjectId;

            protected AnimatedEntry(MeshManagerView mgr, int id)
            {
                Manager = mgr;
                ObjectId = id;
            }

            public abstract void Update(float time);
        }

        private class TowerSwayEntry : AnimatedEntry
        {
            private readonly MeshFrame _baseFrame;

            public TowerSwayEntry(MeshManagerView mgr, int id, MeshFrame baseFrame) : base(mgr, id)
            {
                _baseFrame = baseFrame;
            }

            public override void Update(float time)
            {
                if (!Manager.Manager.HasObject(ObjectId)) return;

                var verts = new Vector3[_baseFrame.Vertices.Length];
                float swayX = Mathf.Sin(time * 1.2f) * 0.08f;
                float swayZ = Mathf.Sin(time * 0.9f + 0.5f) * 0.05f;

                float maxY = float.MinValue;
                for (int i = 0; i < _baseFrame.Vertices.Length; i++)
                    if (_baseFrame.Vertices[i].y > maxY)
                        maxY = _baseFrame.Vertices[i].y;

                for (int i = 0; i < verts.Length; i++)
                {
                    var v = _baseFrame.Vertices[i];
                    float t = maxY > 0.01f ? v.y / maxY : 0f;
                    verts[i] = new Vector3(v.x + swayX * t, v.y, v.z + swayZ * t);
                }

                Manager.UpdateMesh(ObjectId, new MeshFrame
                {
                    Vertices = verts,
                    Triangles = _baseFrame.Triangles,
                    Normals = _baseFrame.Normals,
                    Colors = _baseFrame.Colors
                });
            }
        }

        private class LiftEntry : AnimatedEntry
        {
            private readonly MeshFrame _baseFrame;
            private readonly float _height;
            private readonly float _speed;

            public LiftEntry(MeshManagerView mgr, int id, MeshFrame baseFrame,
                float height, float speed) : base(mgr, id)
            {
                _baseFrame = baseFrame;
                _height = height;
                _speed = speed;
            }

            public override void Update(float time)
            {
                float y = Mathf.Abs(Mathf.Sin(time * _speed)) * _height;
                Manager.SetPosition(ObjectId, new Vector3(-3, y, 3));
            }
        }

        private class RotateEntry : AnimatedEntry
        {
            private readonly Vector3 _axis;
            private readonly float _speed;

            public RotateEntry(MeshManagerView mgr, int id, Vector3 axis, float speed)
                : base(mgr, id)
            {
                _axis = axis.normalized;
                _speed = speed;
            }

            public override void Update(float time)
            {
                var rot = Quaternion.AngleAxis(time * _speed, _axis);
                Manager.SetRotation(ObjectId, rot);
            }
        }

        /// <summary>
        /// 移动式起重机动画：底盘回转 + 吊臂俯仰 + 吊钩随动。
        /// 三个独立管理对象在相同世界位置，通过网格自身偏移实现相对位置。
        /// </summary>
        private class CraneEntry : AnimatedEntry
        {
            private readonly int _boomId;
            private readonly int _hookId;
            private readonly float _boomLength = 4.5f;

            public CraneEntry(MeshManagerView mgr, int chassisId, int boomId, int hookId)
                : base(mgr, chassisId)
            {
                _boomId = boomId;
                _hookId = hookId;
            }

            public override void Update(float time)
            {
                // 1. 吊臂俯仰（30° ~ 75° 往复）
                float boomAngle = 52f + Mathf.Sin(time * 0.5f) * 22f;
                Manager.UpdateMesh(_boomId, ProceduralCrane.GenerateBoom(boomAngle, _boomLength));

                // 获取底盘位置（底盘不旋转）
                var chassisPos = Manager.Manager.GetTransform(ObjectId)?.position ?? Vector3.zero;
                Manager.SetPosition(_boomId, chassisPos);

                // 2. 吊钩跟随吊臂顶端，钢丝绳伸缩
                var pivot = new Vector3(0.3f, 0.85f, 0);
                var boomTipLocal = ProceduralCrane.GetBoomTip(pivot, boomAngle, _boomLength);
                var hookWorldPos = chassisPos + boomTipLocal;
                float cableLen = 1.0f + Mathf.Abs(Mathf.Sin(time * 0.7f)) * 2.0f;
                Manager.UpdateMesh(_hookId, ProceduralCrane.GenerateHook(Vector3.zero, cableLen));
                Manager.SetPosition(_hookId, hookWorldPos);
            }
        }
    }
}
