# 数据接入指南

## 整体架构

```
┌─────────────────────────────────────────────────────────────────┐
│                         Ubuntu 10.36.37.93                       │
│                                                                  │
│  ROS2 相机节点                          ROS2 点云节点             │
│  /left_camera/image/image     /frontend/current_cloud_rgb_map    │
│         │                              │                        │
│         ▼                              │                        │
│  ros2_tcp_relay.py (9001)              │                        │
│  (Python 中继脚本)                      │                        │
│         │                              │                        │
├─────────┼──────────────────────────────┼────────────────────────┤
│         │                              │                        │
│         ▼                              ▼                        │
│  TCP :9001                       ros_tcp_endpoint :10000         │
│  (JPEG 流)                       (Unity ROS-TCP-Connector)      │
│         │                              │                        │
├─────────┼──────────────────────────────┼────────────────────────┤
│         ▼                              ▼                        │
│  CameraStreamClient              PointCloudSubscriber            │
│  → QML Image                     → GPU 点云渲染                  │
│                                                                  │
│                      Windows Qt + Unity                          │
└─────────────────────────────────────────────────────────────────┘
```

**两条独立链路：**

| 数据 | 协议 | Ubuntu 端 | Windows 端 |
|------|------|-----------|------------|
| 相机画面 | TCP JPEG :9001 | `ros2_tcp_relay.py` | `CameraStreamClient` |
| 点云 | ROS-TCP :10000 | `ros_tcp_endpoint` | `PointCloudSubscriber` |

---

## 一、相机画面 (Camera Stream)

### Ubuntu 端

**1. Python 中继脚本**

项目中的 `ros2_tcp_relay.py` 是一个独立 Python 脚本，负责：
- 订阅 ROS2 相机话题（如 `/left_camera/image/image`）
- 将原始图像压缩为 JPEG
- 通过 TCP 推流到指定端口（默认 9001）

**安装依赖**（仅需一次）：
```bash
pip install opencv-python cv-bridge
```

**启动**：
```bash
python3 ros2_tcp_relay.py \
  --topic /left_camera/image/image \
  --port 9001 \
  --quality 80
```

参数说明：

| 参数 | 默认值 | 说明 |
|------|--------|------|
| `--topic` | `/left_camera/image/image` | 相机话题名 |
| `--port` | `9001` | TCP 推流端口 |
| `--quality` | `80` | JPEG 压缩质量 (1-100) |

脚本为**事件驱动**设计：有新帧时才会发送，不空转不重复发旧帧。发送格式为 `4字节长度头 + JPEG 数据`。

### Windows 端

Qt 中的 `CameraStreamClient`（C++）自动连接 `10.36.37.93:9001`，接收 JPEG 帧后解码并通过 `CameraImageProvider` 提供给 QML 的 `Image` 元素显示。

**连接配置**位于 [main.qml](../QtUI/qml/main.qml)：
```qml
camStream.connectToCamera("10.36.37.93", 9001)
```

**跳帧机制**：当 TCP 缓冲区积压超过 200KB 时，自动丢弃旧帧只保留最新帧，降低延迟。

---

## 二、点云 (Point Cloud)

### Ubuntu 端

**1. ros_tcp_endpoint**

使用 Unity 官方 [ROS-TCP-Endpoint](https://github.com/Unity-Technologies/ROS-TCP-Endpoint)，负责将 Unity 的 ROS 请求转发到 ROS2。

**安装**（源码编译）：
```bash
cd ~/fast_ws
git clone -b main-ros2 https://github.com/Unity-Technologies/ROS-TCP-Endpoint.git src/ros_tcp_endpoint
cd ~/fast_ws
colcon build --packages-select ros_tcp_endpoint
```

**每次新终端启动前需 source 工作空间**：
```bash
cd ~/fast_ws
source install/setup.bash
ros2 run ros_tcp_endpoint default_server_endpoint \
  --ros-args -p port:=10000 -p ros_ip:=0.0.0.0
```

参数说明：

| 参数 | 值 | 说明 |
|------|-----|------|
| `port` | `10000` | TCP 监听端口，与 Unity ROSConnectionPrefab 一致 |
| `ros_ip` | `0.0.0.0` | 绑定所有网卡，允许远程连接 |

### Windows 端

Unity 中的 `PointCloudSubscriber`（C#）：
- 通过 ROS-TCP-Connector 订阅 `/frontend/current_cloud_rgb_map`
- 解析 `PointCloud2Msg`（位置 xyz + 颜色 rgb）
- 过滤 NaN/Inf 无效点
- 支持降采样步长（Inspector 中 `Step` 参数）
- 自动计算自适应点大小（根据点云空间范围和密度）

**渲染管线**：
```
PointCloudSubscriber → PointCloudFrame → PointCloudView
  → PointCloudRenderer (GPU ComputeBuffer)
  → PointCloud.shader (URP 公告板四边形)
```

**Unity 场景配置**：
1. 创建空 GameObject
2. 挂载 `PointCloudView` 组件（自动加载 `LiftingTwin/PointCloud` 着色器）
3. 挂载 `PointCloudSubscriber` 组件（自动依赖 PointCloudView）
4. 确认 `ROSConnectionPrefab` 的 IP 为 `10.36.37.93:10000`

---

## 三、启动顺序

### 完整启动流程

```bash
# ─── Ubuntu ────────────────────────────────────────

# 1. (仅首次) 安装依赖
pip install opencv-python cv-bridge

# 2. 启动 ros_tcp_endpoint（点云用）
cd ~/fast_ws
source install/setup.bash
ros2 run ros_tcp_endpoint default_server_endpoint \
  --ros-args -p port:=10000 -p ros_ip:=0.0.0.0

# 3. (新终端) 启动相机中继
python3 ros2_tcp_relay.py \
  --topic /left_camera/image/image \
  --port 9001 \
  --quality 80

# ─── Windows ────────────────────────────────────────

# 4. 启动 LiftingTwin（Qt 自动连接 9001 + Unity 自动连接 10000）
build5\Release\appLiftingTwinUI.exe
```

### 验证连通性

```bash
# 从 Windows 测试 Ubuntu 端口
curl -s telnet://10.36.37.93:9001   # 相机流
curl -s telnet://10.36.37.93:10000  # ros_tcp_endpoint
```

### 常见问题

| 问题 | 原因 | 解决 |
|------|------|------|
| 相机画面卡顿/延迟 | TCP 缓冲区积压旧帧 | `CameraStreamClient` 已自动跳帧，检查网络 |
| 点云不显示 | `ros_tcp_endpoint` 未运行 | 按上面步骤重新启动 |
| `ros_tcp_endpoint` 找不到 | 新终端未 source | 执行 `cd ~/fast_ws && source install/setup.bash` |
| Unity 无法连接 | IP/端口配置不对 | 检查 `ROSConnectionPrefab` 中的 IP 和端口 |
| `package 'ros_tcp_endpoint' not found` | 未编译 | 执行 `colcon build --packages-select ros_tcp_endpoint` |

---

## 四、相关文件索引

### Windows 端

| 文件 | 功能 |
|------|------|
| [QtUI/CameraStreamClient.h/.cpp](../QtUI/CameraStreamClient.h) | TCP JPEG 流接收 |
| [QtUI/CameraImageProvider.h/.cpp](../QtUI/CameraImageProvider.h) | QML 图像提供器 |
| [QtUI/qml/main.qml](../QtUI/qml/main.qml) | 相机画面显示 QML |
| [Assets/Scripts/PointCloud/PointCloudSubscriber.cs](../Assets/Scripts/PointCloud/PointCloudSubscriber.cs) | 点云 ROS2 订阅 |
| [Assets/Scripts/PointCloud/PointCloudView.cs](../Assets/Scripts/PointCloud/PointCloudView.cs) | 点云视图桥接 |
| [Assets/Scripts/PointCloud/PointCloudRenderer.cs](../Assets/Scripts/PointCloud/PointCloudRenderer.cs) | GPU 点云渲染 |
| [Assets/Art/Shaders/PointCloud.shader](../Assets/Art/Shaders/PointCloud.shader) | 点云着色器 |
| [Assets/Resources/ROSConnectionPrefab.prefab](../Assets/Resources/ROSConnectionPrefab.prefab) | ROS 连接配置 |

### Ubuntu 端

| 文件 | 功能 |
|------|------|
| [ros2_tcp_relay.py](../ros2_tcp_relay.py) | ROS2 → TCP JPEG 中继脚本 |
| `~/fast_ws/src/ros_tcp_endpoint/` | ROS-TCP-Endpoint 源码 |
