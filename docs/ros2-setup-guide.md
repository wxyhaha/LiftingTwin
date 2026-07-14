# ROS2 环境搭建与调试指南

## 1. WSL Ubuntu-22.04 环境

### 1.1 安装 WSL 发行版

```powershell
# Windows PowerShell（管理员）
wsl --install -d Ubuntu-22.04
```

首次启动会提示设置 Linux 用户名和密码。

### 1.2 安装 ROS2 Humble

```bash
# 在 WSL 终端中执行

# 设置 locale
sudo apt update && sudo apt install -y locales
sudo locale-gen en_US en_US.UTF-8
sudo update-locale LC_ALL=en_US.UTF-8 LANG=en_US.UTF-8
export LANG=en_US.UTF-8

# 添加 ROS2 源
sudo apt install -y curl gnupg lsb-release
sudo curl -sSL https://raw.githubusercontent.com/ros/rosdistro/master/ros.key \
  -o /usr/share/keyrings/ros-archive-keyring.gpg

# 注意：如果 raw.githubusercontent.com 连接失败（国内网络），
# 需要在 Windows 端使用代理下载后拷入 WSL
# Windows Git Bash:
#   curl -x http://127.0.0.1:7897 -sSL https://raw.githubusercontent.com/ros/rosdistro/master/ros.key \
#     -o ros.key
# WSL:
#   cp /mnt/c/Users/<用户>/ros.key /usr/share/keyrings/ros-archive-keyring.gpg

echo "deb [arch=$(dpkg --print-architecture) signed-by=/usr/share/keyrings/ros-archive-keyring.gpg] http://packages.ros.org/ros2/ubuntu $(lsb_release -cs) main" | sudo tee /etc/apt/sources.list.d/ros2.list > /dev/null

# 安装 ROS2 Humble（基础包）
sudo apt update
sudo apt install -y ros-humble-ros-base python3-colcon-common-extensions python3-rosdep

# 自动 source ROS2 环境
echo 'source /opt/ros/humble/setup.bash' >> ~/.bashrc
source ~/.bashrc
```

### 1.3 验证安装

```bash
# 终端 1
ros2 run demo_nodes_cpp talker

# 终端 2
ros2 run demo_nodes_py listener
```

## 2. ROS-TCP-Endpoint 安装

### 2.1 克隆并构建

```bash
# 创建工作空间
mkdir -p ~/ros_ws/src
cd ~/ros_ws/src

# 克隆 ROS-TCP-Endpoint（main-ros2 分支）
git clone -b main-ros2 https://github.com/Unity-Technologies/ROS-TCP-Endpoint.git

# 注意：如果 git clone 超时，从 Windows Git Bash 下载后传到 WSL：
# Windows:
#   cd /tmp && git clone -b main-ros2 https://github.com/Unity-Technologies/ROS-TCP-Endpoint.git
#   tar cf ros_endpoint.tar ROS-TCP-Endpoint/
# WSL:
#   tar xf /mnt/c/Users/<用户>/ros_endpoint.tar -C ~/ros_ws/src/

# 构建
cd ~/ros_ws
colcon build

# 注意：如果遇到 setuptools 版本冲突报错：
#   canonicalize_version() got an unexpected keyword argument 'strip_trailing_zero'
# 执行：
#   pip3 uninstall -y setuptools   # 回退到系统自带的 setuptools 59.6.0
```

### 2.2 添加 setup 到 bashrc

```bash
echo 'source ~/ros_ws/install/setup.bash' >> ~/.bashrc
source ~/.bashrc
```

## 3. 启动 ROS-TCP-Endpoint

### 3.1 启动命令

```bash
source /opt/ros/humble/setup.bash
source ~/ros_ws/install/setup.bash

ros2 run ros_tcp_endpoint default_server_endpoint \
  --ros-args -p ROS_IP:=0.0.0.0 -p ROS_TCP_PORT:=10000
```

参数说明：
- `ROS_IP:=0.0.0.0` — 监听所有网络接口（WSL2 localhost 转发需要）
- `ROS_TCP_PORT:=10000` — 端口号，与 Unity ROS-TCP-Connector 配置一致

后台运行（不占用终端）：
```bash
nohup ros2 run ros_tcp_endpoint default_server_endpoint \
  --ros-args -p ROS_IP:=0.0.0.0 -p ROS_TCP_PORT:=10000 \
  > /tmp/ros_endpoint.log 2>&1 &
```

### 3.2 验证服务是否运行

```bash
# 检查进程
ps aux | grep default_server_endpoint

# 检查端口（WSL 内）
ss -tlnp | grep 10000

# 从 Windows 测试连通性（PowerShell）
Test-NetConnection -ComputerName 127.0.0.1 -Port 10000

# 输出应该显示 TcpTestSucceeded : True
```

> **注意**：WSL2 支持 localhost 自动转发，Windows 端用 `127.0.0.1:10000` 就能连接到 WSL。无需额外配置。

## 4. Unity ROS-TCP-Connector 配置

### 4.1 添加 Package

在 `Packages/manifest.json` 中添加：
```json
"com.unity.robotics.ros-tcp-connector": "https://github.com/Unity-Technologies/ROS-TCP-Connector.git?path=/com.unity.robotics.ros-tcp-connector"
```

### 4.2 ROS2 协议设置

菜单栏 **Robotics → ROS Settings**：
- **Protocol** → `ROS2`（首次切换会触发 Unity 重新编译）
- **ROS IP Address** → `127.0.0.1`
- **ROS Port** → `10000`
- **Connect on Startup** → ✅ 勾选

### 4.3 场景配置

- 将 `Assets/Resources/ROSConnectionPrefab.prefab` 拖入场景 Hierarchy
- 预制体自带 `ROSConnection`、`QtBridge`、`RosSubscriberTest` 三个组件
- Play 模式后，左上角 HUD 蓝色箭头 = 连接成功

## 5. 测试通信

### 5.1 从 ROS2 发消息到 Unity

```bash
source /opt/ros/humble/setup.bash

# 发布一次
ros2 topic pub /unity_test std_msgs/String "data: hello" --once

# 持续发布（每秒一次）
ros2 topic pub /unity_test std_msgs/String "data: hello" --rate 1
```

Unity 右上角应显示接收到的消息。

### 5.2 查看 ROS2 话题状态

```bash
# 列出所有话题
ros2 topic list

# 查看话题信息
ros2 topic info /unity_test

# 实时监听话题消息
ros2 topic echo /unity_test
```

## 6. 调试命令汇总

| 场景 | 命令 |
|------|------|
| 启动 Endpoint | `ros2 run ros_tcp_endpoint default_server_endpoint --ros-args -p ROS_IP:=0.0.0.0 -p ROS_TCP_PORT:=10000` |
| 检查进程 | `ps aux \| grep default_server_endpoint` |
| 检查端口 | `ss -tlnp \| grep 10000` |
| Windows 侧测连通 | `Test-NetConnection 127.0.0.1 -Port 10000` |
| 发测试消息 | `ros2 topic pub /unity_test std_msgs/String "data: test" --once` |
| 监听消息 | `ros2 topic echo /unity_test` |
| 查看话题列表 | `ros2 topic list` |
| 查看话题信息 | `ros2 topic info /unity_test` |
| 杀掉 Endpoint | `pkill -f default_server_endpoint` |
| 重启 Endpoint | 先 kill 再 start |

> **提示**：此过程可通过 `.bash_aliases` 简化：
> ```bash
> echo 'alias ros_start="ros2 run ros_tcp_endpoint default_server_endpoint --ros-args -p ROS_IP:=0.0.0.0 -p ROS_TCP_PORT:=10000 &"' >> ~/.bash_aliases
> echo 'alias ros_pub='\''ros2 topic pub /unity_test std_msgs/String '\''"data: test"'\'' --once'\''' >> ~/.bash_aliases
> source ~/.bashrc
> ```

## 7. 常见问题

### 7.1 端口 10000 连接不上

```bash
# WSL 内检查 Endpoint 是否在运行
ps aux | grep default_server_endpoint

# 检查端口监听
ss -tlnp | grep 10000

# 如果没输出，说明 Endpoint 挂了，重新启动
```

### 7.2 Unity 显示 "Connection failed"

- 确认 WSL 中 Endpoint 在运行
- 确认 Unity ROS Settings 中 Protocol 为 `ROS2`
- 确认 IP 为 `127.0.0.1`，端口 `10000`
- Unity Console 查看详细信息

### 7.3 端口 9000 被占用（QtBridge 端口）

```powershell
# Windows 查找占用 9000 端口的进程
netstat -ano | findstr :9000

# 根据最后一列的 PID 杀掉进程
taskkill /F /PID <PID>
```

### 7.4 GitHub 在 WSL 中连接超时

WSL 默认无法通过 Windows 代理访问 GitHub。解决方法：

**方法 A**：从 Windows Git Bash 克隆，传到 WSL
```bash
# Windows Git Bash
cd /tmp
git clone -b main-ros2 https://github.com/Unity-Technologies/ROS-TCP-Endpoint.git
tar cf ros_endpoint.tar ROS-TCP-Endpoint/

# WSL
tar xf /mnt/c/Users/<用户>/ros_endpoint.tar -C ~/ros_ws/src/
```

**方法 B**：设置 Windows 代理（需要代理监听 `0.0.0.0`）
```bash
git config --global http.proxy http://<Windows-IP>:7897
```

## 8. 通信架构图

```
┌─────────────────────────────────────────────────────────┐
│                    Qt 桌面外壳                           │
│  rosBridge.publishString("/cmd", "data")                 │
└──────────────────────┬──────────────────────────────────┘
                       │ TCP localhost:9000（JSON 协议）
┌──────────────────────▼──────────────────────────────────┐
│                    Unity Editor                          │
│  ┌──────────────┐    ┌──────────────────────────────┐   │
│  │ QtBridge     │───▶│ ROSConnection                │   │
│  │ (TCP 服务端) │    │ (ROS-TCP-Connector)          │   │
│  └──────────────┘    └───────────┬──────────────────┘   │
└──────────────────────────────────┼──────────────────────┘
                                   │ TCP localhost:10000
┌──────────────────────────────────▼──────────────────────┐
│                   WSL Ubuntu-22.04                       │
│  ┌────────────────────────────────────────────────────┐  │
│  │ ROS-TCP-Endpoint  (Python, colcon 构建)            │  │
│  │ ros2 run ros_tcp_endpoint default_server_endpoint  │  │
│  └──────────────────────┬─────────────────────────────┘  │
│                         │ DDS                            │
│  ┌──────────────────────▼─────────────────────────────┐  │
│  │ ROS2 Humble  (ros2 topic pub / echo)               │  │
│  └────────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────┘
```
