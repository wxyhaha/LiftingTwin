// -----------------------------------------------------------------------
// LiftingTwin - CameraController 单元测试
//
// 测试策略：
//   CameraController 是纯 C# 类，不依赖 MonoBehaviour 生命周期，
//   因此使用 Edit Mode 测试即可覆盖所有数学运算逻辑。
//
// 测试范围：
//   - 初始化与复位
//   - Orbit / Pan / Zoom 运算
//   - Tick 平滑插值
//   - 角度/距离钳位
//   - SetTarget 位置保持
// -----------------------------------------------------------------------

using NUnit.Framework;
using UnityEngine;
using LiftingTwin.Visualization;

namespace LiftingTwin.Tests.Editor
{
    /// <summary>
    /// CameraController 的编辑模式单元测试。
    /// 使用默认 CameraConfig 参数，验证数学运算的正确性。
    /// </summary>
    public class CameraControllerTests
    {
        private CameraConfig CreateDefaultConfig()
        {
            var config = ScriptableObject.CreateInstance<CameraConfig>();
            config.orbitSpeed = 0.3f;
            config.panSpeed = 0.01f;
            config.zoomSpeed = 1.0f;
            config.minPolarAngle = 5f;
            config.maxPolarAngle = 89f;
            config.minDistance = 0.5f;
            config.maxDistance = 500f;
            config.enableSmoothing = false;
            config.smoothSpeed = 8f;
            config.defaultPosition = new Vector3(0, 5, -15);
            config.defaultTarget = Vector3.zero;
            return config;
        }

        // 容差：浮点比较
        private const float Epsilon = 1e-4f;

        // ---------------------------------------------------------------
        // 初始化与复位
        // ---------------------------------------------------------------

        [Test]
        public void 构造后_位置应与默认配置一致()
        {
            var config = CreateDefaultConfig();
            var controller = new CameraController(config);

            var pos = controller.GetPosition();
            Assert.That(pos.x, Is.EqualTo(config.defaultPosition.x).Within(Epsilon));
            Assert.That(pos.y, Is.EqualTo(config.defaultPosition.y).Within(Epsilon));
            Assert.That(pos.z, Is.EqualTo(config.defaultPosition.z).Within(Epsilon));
            Assert.That(controller.Target, Is.EqualTo(config.defaultTarget));
        }

        [Test]
        public void Reset后_位置应恢复默认值()
        {
            var config = CreateDefaultConfig();
            var controller = new CameraController(config);

            controller.Orbit(100, 50);
            controller.Zoom(10);
            controller.Reset();

            var pos = controller.GetPosition();
            Assert.That(pos.x, Is.EqualTo(config.defaultPosition.x).Within(Epsilon));
            Assert.That(pos.y, Is.EqualTo(config.defaultPosition.y).Within(Epsilon));
            Assert.That(pos.z, Is.EqualTo(config.defaultPosition.z).Within(Epsilon));
            Assert.That(controller.Target, Is.EqualTo(config.defaultTarget));
        }

        // ---------------------------------------------------------------
        // Orbit
        // ---------------------------------------------------------------

        [Test]
        public void Orbit_正向水平拖拽_方位角应增加()
        {
            var config = CreateDefaultConfig();
            var controller = new CameraController(config);

            float azimuthBefore = controller.Azimuth;
            controller.Orbit(100, 0);
            float azimuthAfter = controller.Azimuth;

            Assert.That(azimuthAfter, Is.GreaterThan(azimuthBefore));
            // 每像素 0.3 度
            Assert.That(azimuthAfter - azimuthBefore, Is.EqualTo(30f).Within(Epsilon));
        }

        [Test]
        public void Orbit_正向垂直拖拽_极角应增加()
        {
            var config = CreateDefaultConfig();
            var controller = new CameraController(config);

            float polarBefore = controller.Polar;
            controller.Orbit(0, 100);
            float polarAfter = controller.Polar;

            Assert.That(polarAfter, Is.GreaterThan(polarBefore));
        }

        [Test]
        public void Orbit_极角不应低于下限()
        {
            var config = CreateDefaultConfig();
            var controller = new CameraController(config);

            // 朝下猛拖试图突破下限
            controller.Orbit(0, -1000);
            Assert.That(controller.Polar, Is.GreaterThanOrEqualTo(config.minPolarAngle - Epsilon));
        }

        [Test]
        public void Orbit_极角不应超过上限()
        {
            var config = CreateDefaultConfig();
            var controller = new CameraController(config);

            // 朝上猛拖试图突破上限
            controller.Orbit(0, 1000);
            Assert.That(controller.Polar, Is.LessThanOrEqualTo(config.maxPolarAngle + Epsilon));
        }

        // ---------------------------------------------------------------
        // Pan
        // ---------------------------------------------------------------

        [Test]
        public void Pan_水平拖动_目标点应移动()
        {
            var config = CreateDefaultConfig();
            var controller = new CameraController(config);

            Vector3 targetBefore = controller.Target;
            controller.Pan(100, 0);
            Vector3 targetAfter = controller.Target;

            // 目标点应发生变化
            Assert.That((targetAfter - targetBefore).magnitude, Is.GreaterThan(0));
        }

        [Test]
        public void Pan_负值拖动_移动方向应与正值相反()
        {
            var config = CreateDefaultConfig();
            var controller = new CameraController(config);

            controller.Pan(100, 0);
            var offsetPositive = controller.Target;

            controller.Reset();

            controller.Pan(-100, 0);
            var offsetNegative = controller.Target;

            // 两个方向应相反
            Assert.That(offsetPositive.x, Is.GreaterThan(0)); // 向右
            Assert.That(offsetNegative.x, Is.LessThan(0));    // 向左
        }

        // ---------------------------------------------------------------
        // Zoom
        // ---------------------------------------------------------------

        [Test]
        public void Zoom_正值_距离应减小()
        {
            var config = CreateDefaultConfig();
            var controller = new CameraController(config);

            float distBefore = controller.Distance;
            controller.Zoom(5);
            float distAfter = controller.Distance;

            Assert.That(distAfter, Is.LessThan(distBefore));
        }

        [Test]
        public void Zoom_负值_距离应增大()
        {
            var config = CreateDefaultConfig();
            var controller = new CameraController(config);

            float distBefore = controller.Distance;
            controller.Zoom(-5);
            float distAfter = controller.Distance;

            Assert.That(distAfter, Is.GreaterThan(distBefore));
        }

        [Test]
        public void Zoom_距离不应低于最小值()
        {
            var config = CreateDefaultConfig();
            var controller = new CameraController(config);

            controller.Zoom(10000);
            Assert.That(controller.Distance, Is.GreaterThanOrEqualTo(config.minDistance - Epsilon));
        }

        [Test]
        public void Zoom_距离不应超过最大值()
        {
            var config = CreateDefaultConfig();
            var controller = new CameraController(config);

            // 先拉到最近再反向拉远
            controller.Zoom(-100000);
            Assert.That(controller.Distance, Is.LessThanOrEqualTo(config.maxDistance + Epsilon));
        }

        // ---------------------------------------------------------------
        // Tick
        // ---------------------------------------------------------------

        [Test]
        public void Tick_无平滑时_输出应与当前位置一致()
        {
            var config = CreateDefaultConfig();
            config.enableSmoothing = false;
            var controller = new CameraController(config);

            controller.Orbit(30, 20);
            controller.Tick(0.016f, out Vector3 pos, out Quaternion rot);

            var expectedPos = controller.GetPosition();
            Assert.That(pos.x, Is.EqualTo(expectedPos.x).Within(Epsilon));
            Assert.That(pos.y, Is.EqualTo(expectedPos.y).Within(Epsilon));
            Assert.That(pos.z, Is.EqualTo(expectedPos.z).Within(Epsilon));
        }

        [Test]
        public void Tick_有平滑时_位置应朝目标过渡()
        {
            var config = CreateDefaultConfig();
            config.enableSmoothing = true;
            config.smoothSpeed = 1f;
            var controller = new CameraController(config);

            Vector3 posBefore = controller.GetPosition();
            controller.Orbit(100, 50);
            controller.Tick(0.016f, out Vector3 posAfter, out _);

            // 平滑后应该移动了，但还没到最终位置
            Assert.That((posAfter - posBefore).magnitude, Is.GreaterThan(0));
            var finalPos = controller.GetPosition();
            Assert.That((posAfter - finalPos).magnitude, Is.GreaterThan(0));
        }

        [Test]
        public void Tick_经过足够时间_平滑位置应收敛到目标()
        {
            var config = CreateDefaultConfig();
            config.enableSmoothing = true;
            config.smoothSpeed = 10f;
            var controller = new CameraController(config);

            controller.Orbit(100, 50);

            // 模拟多帧更新
            for (int i = 0; i < 200; i++)
            {
                controller.Tick(0.016f, out _, out _);
            }

            var finalPos = controller.GetPosition();
            controller.Tick(0.016f, out Vector3 smoothPos, out _);

            Assert.That((smoothPos - finalPos).magnitude, Is.LessThan(0.01f));
        }

        // ---------------------------------------------------------------
        // SetTarget
        // ---------------------------------------------------------------

        [Test]
        public void SetTarget_目标点改变后_位置应保持不变()
        {
            var config = CreateDefaultConfig();
            var controller = new CameraController(config);

            Vector3 posBefore = controller.GetPosition();
            controller.SetTarget(new Vector3(5, 0, 5));
            Vector3 posAfter = controller.GetPosition();

            // 目标变了，但相机位置不变
            Assert.That(controller.Target, Is.EqualTo(new Vector3(5, 0, 5)));
            Assert.That((posAfter - posBefore).magnitude, Is.LessThan(Epsilon));
        }

        // ---------------------------------------------------------------
        // 组合操作
        // ---------------------------------------------------------------

        [Test]
        public void OrbitPanZoom组合_输出应为有效旋转和位置()
        {
            var config = CreateDefaultConfig();
            var controller = new CameraController(config);

            controller.Orbit(45, 30);
            controller.Pan(10, -5);
            controller.Zoom(3);
            controller.Tick(0.016f, out Vector3 pos, out Quaternion rot);

            // 位置应为有效值（非 NaN/Infinity）
            Assert.That(float.IsFinite(pos.x), Is.True);
            Assert.That(float.IsFinite(pos.y), Is.True);
            Assert.That(float.IsFinite(pos.z), Is.True);

            // 旋转应为单位四元数 (手动计算 sqrMagnitude)
            float sqrMag = rot.x * rot.x + rot.y * rot.y + rot.z * rot.z + rot.w * rot.w;
            Assert.That(Mathf.Abs(sqrMag - 1f), Is.LessThan(Epsilon));
        }
    }
}
