# 海康工业相机 (MV-CU013-A0UC) ROS2 集成与 Qt 显示指南

## 架构总览 (VMware 方案)

```
海康 MV-CU013-A0UC (USB3, Windows 主机)
  │ VMware USB passthrough (xHCI 控制器)
  ▼
VMware Workstation Pro — Ubuntu 22.04
  ├── MVS SDK (libMvCameraControl.so) → hik_camera_driver.py
  │   └── 发布 /camera/image_raw (sensor_msgs/Image, RGB8)
  │
  └── stream_server_v3.py (TCP 流服务器, 端口 9001)
      └── 订阅相机话题 → JPEG 编码 → TCP 推流 (子进程隔离)
  │
  ▼ (NAT 网络, 192.168.164.128:9001)
Windows Qt 应用 (LiftingTwin)
  └── CameraStreamClient (C++ TCP 客户端 + QQuickImageProvider)
      └── QML Image 元素显示实时视频 (image://camerafeed/live)
```

## 环境要求

| 组件 | 版本 | 说明 |
|------|------|------|
| VMware Workstation Pro | 最新版 | USB 控制器设为 USB 3.0 (xHCI) |
| Ubuntu | 22.04 LTS | ROS2 Humble 官方支持 |
| ROS2 | Humble | 机器人操作系统 |
| MVS SDK | < 4.6.1 | 海康机器人 Linux SDK |
| Qt | 6.x | 桌面 UI 框架 |
| 相机 | MV-CU013-A0UC | USB3 Vision, 1280x1024, VID 2bdf |

---

## Phase 0: 虚拟机网络配置

### 网络模式: NAT

VMware 默认 NAT 模式即可。VM IP 固定为 `192.168.164.128`（DHCP 分配）。

### 确认网络互通

```bash
# VM 中查看 IP
ip addr show | grep "inet " | grep -v 127.0.0.1

# Windows PowerShell 测试连通性
ping 192.168.164.128
```

> 如果 ping 不通，检查 VMware 虚拟网络编辑器 → VMnet8 (NAT) 子网是否正确。

---

## Phase 1: VMware USB 相机直通

### 连接相机

1. 插好相机 USB 线（建议蓝色 USB 3.0 口）
2. VMware 菜单 → 虚拟机 → 可移动设备 → MV-CU013-A0UC → **连接（断开与主机的连接）**
    - 如果选择"连接到主机"则相机被 Windows 独占，VM 拿不到
3. VM 内验证:

```bash
lsusb | grep 2bdf
# 应看到: 2bdf:0203 (或 2bdf:0001) Hikrobot 等
```

### USB 没有设备？

- 确认 Windows 上没有 MVS Client 后台占用相机
- 换个 USB 口（电脑可能有多个 USB 控制器）
- 在 VMX 配置文件中尝试添加 USB 兼容性设置:

  编辑 `C:\Users\wxy\Documents\Virtual Machines\Ubuntu 64-bit\Ubuntu 64-bit.vmx`，添加:

  ```
  usb.quirks = "allow:2bdf:0203"
  ehci.pciorientation = "true"
  usb.assumeTopo = "TRUE"
  ```

---

## Phase 2: MVS SDK 安装配置

### 安装 MVS SDK

从海康机器人官网下载 Linux 版 MVS SDK:

```bash
# 解压安装包
tar xzf MVS-4.x.x_ubuntu_22.04.tar.gz
cd MVS-4.x.x_ubuntu_22.04

# 安装
sudo ./install.sh
# 默认安装到 /opt/MVS
```

### 验证安装

```bash
ls -la /opt/MVS/lib/64/libMvCameraControl.so

# 加载动态库
python3 -c "
import ctypes
lib = ctypes.CDLL('/opt/MVS/lib/64/libMvCameraControl.so')
print('MVS SDK loaded OK')
"
```

### 相机权限

```bash
sudo sh -c 'echo "SUBSYSTEM==\"usb\", ATTRS{idVendor}==\"2bdf\", MODE=\"0666\"" > /etc/udev/rules.d/99-hikvision.rules'
sudo udevadm control --reload-rules
sudo udevadm trigger
sudo usermod -aG video $USER
```

### 环境变量

每次使用前导出（也可以写到 `~/.bashrc`）:

```bash
export MVCAM_COMMON_RUNENV=/opt/MVS/lib
export LD_LIBRARY_PATH=/opt/MVS/lib/64:$LD_LIBRARY_PATH
```

---

## Phase 3: ROS2 包创建与编译

### 创建工作空间

```bash
mkdir -p ~/ros_ws/src
cd ~/ros_ws/src

# 如果已有源码则跳过
git clone ... (或手动复制)

# 安装依赖
pip install opencv-python cv-bridge
```

### 编译

```bash
cd ~/ros_ws
colcon build --packages-select camera_stream_server
source install/setup.bash
```

### 包结构

```
camera_stream_server/
├── setup.py
├── package.xml
├── resource/
└── camera_stream_server/
    ├── __init__.py
    ├── hik_camera_driver.py    # 相机驱动节点
    ├── stream_server_v3.py     # TCP 流服务器 (子进程隔离版)
    ├── stream_server.py        # (旧版, 不启用)
    ├── test_pattern.py         # 测试图案发生器
    └── MvImport/               # MVS Python 包装层
```

---

## Phase 4: 启动管线

### 终端 1 — 相机驱动

```bash
cd ~/ros_ws
source /opt/ros/humble/setup.bash
source install/setup.bash
export MVCAM_COMMON_RUNENV=/opt/MVS/lib
export LD_LIBRARY_PATH=/opt/MVS/lib/64:$LD_LIBRARY_PATH

~/ros_ws/install/camera_stream_server/bin/hik_camera_driver --ros-args \
  -p width:=640 -p height:=480 -p fps:=10 \
  -p exposure_time:=55000 -p gain:=16.0 -p auto_exposure:="Off"
```

**相机参数说明:**

| 参数 | 默认值 | 说明 |
|------|--------|------|
| `width` | 640 | 图像宽度 |
| `height` | 480 | 图像高度 |
| `fps` | 10 | 目标帧率 (受曝光时间限制) |
| `exposure_time` | 30000 | 曝光时间 (微秒)。现场照明下尝试 5000-50000 |
| `gain` | 16.0 | 增益 (dB) |
| `auto_exposure` | "Off" | 自动曝光: Off / Once / Continuous |

> ⚠ 关键: `TriggerMode` 必须在开始抓图前设为 OFF，否则拿不到帧。
> 代码中已自动处理。

### 终端 2 — TCP 流服务器

```bash
cd ~/ros_ws
source /opt/ros/humble/setup.bash
source install/setup.bash
export MVCAM_COMMON_RUNENV=/opt/MVS/lib
export LD_LIBRARY_PATH=/opt/MVS/lib/64:$LD_LIBRARY_PATH

~/ros_ws/install/camera_stream_server/bin/stream_server --ros-args \
  -p stream_port:=9001 -p use_compressed:=false
```

> ⚠ 使用 `stream_server_v3.py`（子进程隔离版），而非 `stream_server.py`。
> v3 将 TCP socket 放在子进程中，避免 rclpy 的 Python socket 冲突 bug。
> `stream_server` entry point 已经被配置为指向 v3。

### 终端 3 — 验证话题 (可选)

```bash
source /opt/ros/humble/setup.bash
ros2 topic list              # 应有 /camera/image_raw
ros2 topic hz /camera/image_raw  # 查看帧率
```

### Windows — Qt 应用

```powershell
# 确保 QML 最新
copy d:\studyPlace\LiftingTwin\QtUI\qml\main.qml d:\studyPlace\LiftingTwin\build\Release\LiftingTwin\qml\main.qml

# 启动
cd d:\studyPlace\LiftingTwin\build\Release
.\appLiftingTwinUI.exe
```

Qt 应用自动连接到 `192.168.164.128:9001`（在 `main.qml:364` 配置）。

---

## Phase 5: 测试图案模式 (无相机验证)

没有相机或 USB 直通不稳定时，可以用测试图案验证整个显示管线:

### 终端 1 — 测试图案

```bash
cd ~/ros_ws
source /opt/ros/humble/setup.bash
source install/setup.bash
~/ros_ws/install/camera_stream_server/bin/test_pattern
```

(发布 640x480@30fps 彩色条纹 + 帧计数到 `/camera/image_raw`)

### 终端 2 — 流服务器 (同上)

```bash
~/ros_ws/install/camera_stream_server/bin/stream_server --ros-args \
  -p stream_port:=9001 -p use_compressed:=false
```

### Windows Qt 显示 (同上)

---

## 启动顺序总结

### 启动顺序
```
1. VMware 中: 连接 USB 相机 (可移动设备 → MV-CU013-A0UC → 连接)
2. VM 终端 1: hik_camera_driver (相机驱动)
3. VM 终端 2: stream_server (TCP 流服务器)
4. Windows: appLiftingTwinUI.exe
```

### 终端 1 — 相机驱动 (完整命令)
```bash
cd ~/ros_ws
source /opt/ros/humble/setup.bash
source install/setup.bash
export MVCAM_COMMON_RUNENV=/opt/MVS/lib
export LD_LIBRARY_PATH=/opt/MVS/lib/64:$LD_LIBRARY_PATH

~/ros_ws/install/camera_stream_server/bin/hik_camera_driver --ros-args \
  -p width:=640 -p height:=480 -p fps:=10 \
  -p exposure_time:=55000 -p gain:=16.0
```

### 终端 2 — TCP 流服务器 (完整命令)
```bash
cd ~/ros_ws
source /opt/ros/humble/setup.bash
source install/setup.bash
export MVCAM_COMMON_RUNENV=/opt/MVS/lib
export LD_LIBRARY_PATH=/opt/MVS/lib/64:$LD_LIBRARY_PATH

~/ros_ws/install/camera_stream_server/bin/stream_server --ros-args \
  -p stream_port:=9001 -p use_compressed:=false
```

### Windows — Qt 应用启动
```powershell
# 如果 QML 有更新，先复制
copy d:\studyPlace\LiftingTwin\QtUI\qml\main.qml d:\studyPlace\LiftingTwin\QtUI\build\Release\LiftingTwin\qml\main.qml

# 启动
cd d:\studyPlace\LiftingTwin\QtUI\build\Release
.\appLiftingTwinUI.exe
```

> ⚠ 如果 Ctrl+C 停掉驱动后重开失败（`OpenDevice failed: 0x80000301`），
> 需要 VMware 菜单 → 可移动设备 → 断开 USB 再重新连接。

### 快速启动脚本 (VM ~/ros_ws/start_camera.sh)

```bash
#!/bin/bash
source /opt/ros/humble/setup.bash
cd ~/ros_ws
source install/setup.bash
export MVCAM_COMMON_RUNENV=/opt/MVS/lib
export LD_LIBRARY_PATH=/opt/MVS/lib/64:$LD_LIBRARY_PATH

echo "=== Camera Driver ==="
~/ros_ws/install/camera_stream_server/bin/hik_camera_driver --ros-args \
  -p width:=640 -p height:=480 -p fps:=10 \
  -p exposure_time:=55000 -p gain:=16.0 -p auto_exposure:="Off"
```

```bash
chmod +x ~/ros_ws/start_camera.sh
```

---

## 已知问题与解决方案

### rclpy socket 冲突 (AttributeError: 'socket' object has no attribute 'handle')

**表现**: `stream_server.py` 在 `rclpy.spin()` 时崩溃。

**原因**: ROS2 Humble 的 rclpy executor 将 Python socket 对象误认为了 rclpy client，这是 Humble 已知 bug。

**解决方案**: 使用 `stream_server_v3.py` (子进程隔离)。TCP 服务器跑在 `multiprocessing.Process` 中，
ROS2 节点通过 `/dev/shm/cam_frame.jpg` 共享 JPEG 帧。主进程只跑 `rclpy.spin()`，不受 socket 干扰。

### Qt 连接拒绝或代理冲突

**表现**: Qt 应用报 `QAbstractSocket::ConnectionRefusedError` 或"对于这个操作代理类型是无效的"。

**原因**: Clash/V2Ray 等系统代理拦截了到 VM 的 TCP 连接。

**解决方案**: `CameraStreamClient` 初始化时调用 `m_socket->setProxy(QNetworkProxy::NoProxy)`，
代码中已处理。

### Ctrl+C 停止后无法再次启动相机

**原因**: `MV_CC_Initialize()` 是全局初始化，没有配对调用 `MV_CC_Finalize()`。
进程退出后 MVS SDK 的全局状态未清理，下次启动时 `MV_CC_Initialize()` 返回资源占用错误。

**临时恢复**: VMware 菜单 → 可移动设备 → 断开相机再重新连接（USB 硬件复位）。

**根本修复**: 更新 `hik_camera_driver.py`，在 `destroy_node()` 中添加 `MV_CC_Finalize()`:

```python
def destroy_node(self):
    self.running = False
    if self.grab_thread and self.grab_thread.is_alive():
        self.grab_thread.join(timeout=2)
    if self.cam:
        try:
            self.cam.MV_CC_StopGrabbing()
            self.cam.MV_CC_CloseDevice()
            self.cam.MV_CC_DestroyHandle()
        except:
            pass
    MvCamera.MV_CC_Finalize()  # ← 添加这一行
    super().destroy_node()
```

修改后重新编译:
```bash
cd ~/ros_ws
colcon build --packages-select camera_stream_server
```

### VMware USB 直通不稳定

**表现**: 相机枚举成功但开始抓图时报 `MV_E_USB_WRITE (0x80000301)`，或 `dmesg` 中反复 "Cannot enable"。

**原因**: VMware 虚拟 USB 控制器对 isochronous 传输支持不完善。

**尝试解决**:
1. 换一个物理 USB 口
2. VM 设置 → USB 控制器 → 切换 USB 2.0 / 3.0
3. 编辑 VMX 添加 `usb.quirks`
4. 重启 Windows + 重新插拔相机后再开 VM

### QML 刷新不显示

QML 使用 `Timer` 每 50ms 修改 `Image.source` 来触发刷新，通过 `Math.random()` 参数避免缓存:

```qml
Timer {
    interval: 50
    running: true; repeat: true
    onTriggered: camImage1.source = "image://camerafeed/live?" + Math.random()
}
Image {
    cache: false
    sourceSize.width: 192; sourceSize.height: 75
}
```

---

## 关键代码文件索引

| 文件 | 位置 | 功能 |
|------|------|------|
| CameraStreamClient.h/.cpp | `QtUI/` | TCP 接收 + JPEG 解码 |
| CameraImageProvider.h/.cpp | `QtUI/` | QQuickImageProvider |
| main.cpp | `QtUI/` | 注册 camStream + imageProvider |
| main.qml | `QtUI/qml/` | QML UI, camStream.connectToCamera |
| hik_camera_driver.py | `ros_ws/src/.../` | MVS SDK → ROS2 Image |
| stream_server_v3.py | `ros_ws/src/.../` | ROS2 → TCP JPEG (子进程版) |
| test_pattern.py | `ros_ws/src/.../` | 测试图案发生器 |
| CMakeLists.txt | `QtUI/` | Qt 构建配置 (+Network) |

---

## 相机参数调参建议

当前参数 `exposure_time:=410509µs` (~410ms) 是低光环境下调出来的。
正常室内照明应大幅降低:

```bash
# 先试自动曝光一次
... -p auto_exposure:="Once"

# 再改为手动，逐步降低
... -p exposure_time:=20000 -p auto_exposure:="Off"   # 20ms
... -p exposure_time:=10000 -p auto_exposure:="Off"   # 10ms
... -p exposure_time:=5000  -p auto_exposure:="Off"   # 5ms
```

曝光时间越短，帧率可以越高 (fps=1000/exposure_time_ms)。
