# LiftingTwin

输变电工程吊装作业三维动态安全管控验证平台

## 项目简介

LiftingTwin 是一个面向输变电工程吊装作业的数字孪生三维可视化验证平台。

- **Unity 端**：桌面端三维可视化渲染，接收实时数据动态更新 Mesh、PointCloud 等
- **Qt 端**：桌面外壳应用，工具栏 + 三维场景 + 状态栏一体化界面，通过 Win32 HWND 嵌入 Unity

算法团队通过实时数据通道向 Unity 推送 Mesh、PointCloud、Pose 等数据，Unity 负责动态更新显示。

## 技术栈

| 项 | 版本/选型 |
|---|----------|
| Unity | 2022.3.62f3 LTS |
| 渲染管线 | Universal Render Pipeline (URP) |
| 目标平台 | Windows Desktop (Standalone) |
| 语言 | C# (Unity) / C++17, QML (Qt) |
| Qt 版本 | Qt 6.5.3 (msvc2019_64) |
| Qt 构建 | CMake + MSBuild (Visual Studio 2022) |
| 版本控制 | Git |
| AI 辅助 | Claude, Cursor, DeepSeek V4 等 |

**关键 Unity Package：**

- `com.unity.render-pipelines.universal` — URP 渲染管线
- `com.unity.textmeshpro` — 文字渲染
- `com.unity.ugui` — UI 系统

## 目录说明

```
LiftingTwin/
├── Assets/
│   ├── _Config/             配置资产（ScriptableObject 实例）
│   ├── _Scenes/             场景文件
│   ├── Art/                 美术资源
│   │   ├── Materials/       材质
│   │   ├── Shaders/         自定义 Shader（HLSL / Shader Graph）
│   │   └── Textures/        贴图
│   ├── Editor/              编辑器扩展脚本
│   │   └── CreateMaterials.cs   创建 Runtime 材质球菜单
│   ├── Plugins/             平台原生插件（.dll / .a / .so）
│   ├── Prefabs/             预制体
│   ├── Resources/           Runtime 动态加载资源
│   │   ├── Mat_Ground.mat   地面材质 (URP/Lit)
│   │   ├── Mat_Gray.mat     灰色材质
│   │   ├── Mat_Orange.mat   橙色材质
│   │   └── Mat_Steel.mat    钢色材质（Mesh 默认）
│   ├── Scripts/             所有 C# 代码
│   │   ├── Core/            应用入口、生命周期、配置
│   │   ├── Runtime/         场景初始化、测试动画
│   │   ├── Mesh/            Mesh 动态更新
│   │   ├── PointCloud/      点云显示
│   │   ├── Network/         QtBridge (IPC 服务器), WebSocket（预留）
│   │   ├── Visualization/   相机控制、渲染辅助
│   │   ├── UI/              UGUI 信息面板、选中系统
│   │   └── Utils/           日志、扩展方法
│   ├── StreamingAssets/     原始文件（按原样复制到构建）
│   ├── ThirdParty/          第三方源码
│   └── UI/                  UI 相关资产
├── QtUI/                    Qt 桌面外壳应用
│   ├── main.cpp             Qt 入口 + Unity 窗口嵌入 + ROS 桥
│   ├── QtRosBridge.h        ROS 桥接 C++ 客户端（暴露给 QML）
│   ├── qml/                 QML 界面
│   │   └── main.qml         主布局（顶部工具栏 + Unity 3D 场景 + 底部状态栏）
│   ├── qml.qrc              QML 资源配置
│   ├── CMakeLists.txt       构建配置
│   └── build4/              构建输出目录
└── ProjectSettings/         Unity 项目设置
```

## 系统架构

```
┌─────────────────────────────────────────────────────┐
│                   Qt 桌面外壳                        │
│  ┌───────────┐ ┌────────────────────┐ ┌──────────┐ │
│  │ 顶部工具栏  │ │  Unity 3D 场景窗口  │ │ 状态栏    │ │
│  │  (QML)    │ │  (Win32 HWND 嵌入) │ │  (QML)   │ │
│  └───────────┘ └────────────────────┘ └──────────┘ │
└─────────────────────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────┐
│                 Unity Standalone                     │
│  ┌────────┐ ┌──────────┐ ┌──────────┐ ┌─────────┐  │
│  │ 地面/  │ │ Mesh渲染  │ │ 点云渲染  │ │ 信息面板 │  │
│  │ 参考物  │ │ (动态网格) │ │ (GL点)   │ │ (UGUI)  │  │
│  └────────┘ └──────────┘ └──────────┘ └─────────┘  │
└─────────────────────────────────────────────────────┘
```

## 已实现功能

| 模块 | 状态 | 说明 |
|------|------|------|
| 工程初始化 | ✅ 完成 | 项目目录、配置、规范 |
| Unity 场景 | ✅ 完成 | 地面 + 参考物体 + 辅助网格 |
| 相机控制 | ✅ 完成 | 鼠标拖拽旋转/平移/滚轮缩放 |
| Mesh 动态更新 | ✅ 完成 | DynamicMesh + MeshManager |
| 点云渲染 | ✅ 完成 | PointCloudView + GL 渲染 |
| Qt 桌面外壳 | ✅ 完成 | QML 布局 + Unity 窗口嵌入 |
| UGUI 信息面板 | ✅ 完成 | 对象/帧率/日志显示 |
| 选中交互 | ✅ 完成 | 射线检测 MeshCollider 选中 |
| 测试场景 | ✅ 完成 | 输电塔(风致摇摆) + 起重机(底盘/吊臂/吊钩) |
| ROS2 通信 | ✅ 完成 | Unity ROS-TCP-Connector ↔ ROS-TCP-Endpoint (WSL) |
| Qt ↔ Unity IPC 桥接 | ✅ 完成 | Qt 通过 TCP localhost:9000 控制 Unity 发布/订阅 ROS2 |
| WebSocket 网络层 | 🔲 待开始 | 实时数据接收 |
| 完整数据协议对接 | 🔲 待开始 | 协议定义 + 解析 + 调度 |
| URP 材质 Build | ✅ 完成 | Resources 加载 + 回退创建 |

## 构建指南

### Unity 构建

1. 在 Unity Editor 中打开项目
2. 运行菜单 **Tools → Create Runtime Materials**（确保 Resources 材质已生成）
3. 检查 **Project Settings → Graphics → Always Included Shaders**：
   - 确认 `Universal Render Pipeline/Lit` **不在**列表中（变体过多会导致构建失败）
   - 保留默认的 URP shader 列表即可
4. **File → Build Settings**：
   - Target Platform: **Windows, Mac, Linux (Standalone)**
   - Architecture: **Intel 64-bit**
   - 勾选 **Development Build**（调试用）
5. 点击 **Build**，输出到 `Build/Windows/`

### Qt 桌面外壳构建

```bash
# 使用 CMake + MSBuild 构建
cd QtUI
cmake -B build -G "Visual Studio 17 2022" -A x64 -DCMAKE_PREFIX_PATH="C:\Qt\6.5.3\msvc2019_64"
cmake --build build --config Release
```

构建产物：`build/Release/appLiftingTwinUI.exe`

启动方式：直接运行 `appLiftingTwinUI.exe`，它会自动查找并启动 `Build/Windows/LiftingTwin.exe`，嵌入到界面中心区域。

### ROS2 通信配置

Unity Editor Play Mode 下测试 ROS2 通信：

1. **WSL 端**：确保 ROS-TCP-Endpoint 在运行
   ```bash
   source /opt/ros/humble/setup.bash
   ros2 run ros_tcp_endpoint default_server_endpoint \
     --ros-args -p ROS_IP:=0.0.0.0 -p ROS_TCP_PORT:=10000
   ```

2. **Unity 端**：菜单 **Robotics → ROS Settings**
   - **Protocol** → `ROS2`（首次切换会触发重新编译）
   - **ROS IP Address** → `127.0.0.1`
   - **ROS Port** → `10000`
   - 点击 Play，左上角 HUD **蓝色箭头** = 连接成功

3. **测试发送消息**（WSL 终端）：
   ```bash
   ros2 topic pub /unity_test std_msgs/String "data: hello" --once
   ```
   Unity 右上角显示消息，Console 输出 `[ROS2 收到消息]`

### Qt ↔ Unity IPC 桥接

Qt 外壳通过 TCP `localhost:9000` 与 Unity 通信，Unity 转发到 ROS2。

```
Qt (QML)  → TCP :9000 → Unity QtBridge → ROSConnection → ROS2 (WSL)
```

- Unity 侧：`Assets/Scripts/Network/QtBridge.cs`（TCP 服务器，监听 9000）
- Qt 侧：`QtUI/QtRosBridge.h`（C++ TCP 客户端，暴露给 QML）
- QML 中通过 `rosBridge` 对象调用：`publishString(topic, data)`、`subscribe(topic)`、`unsubscribe(topic)`

**启动顺序：**
1. WSL：启动 ROS-TCP-Endpoint
2. Unity：Play
3. Qt：运行 `appLiftingTwinUI.exe`（自动连接 Unity 和 ROS2）

> **如果 Unity 重新导入或删除过 ROSConnectionPrefab**，需要重新在场景中拖入 `Assets/Resources/ROSConnectionPrefab.prefab`，或检查场景中是否包含该预制体实例。`QtBridge` 和 `RosSubscriberTest` 组件已内置在预制体中，无需手动添加。

### Unity 材质系统说明

构建后材质粉色（品红）问题的解决方案：

- URP Lit Shader 有 130 万+ 变体，无法加入 Always Included Shaders
- 改用 `Resources.Load<Material>()` 在 Start 时加载材质球
- 材质球文件存放在 `Assets/Resources/`，构建时自动包含
- 回退机制：`Resources.Load` 失败时通过 `Shader.Find` + `new Material` 动态创建
- 编辑器脚本 `CreateMaterials.cs` 提供一键生成材质球功能

## 开发规范

### Git 规范

#### 分支策略

- `main` — 稳定主线，只通过 PR 合入
- `feature/<模块>/<简述>` — 功能分支
- `fix/<简述>` — 修复分支

#### Commit 规范

```
<type>: <简短描述>

类型（type）：
  feat     — 新功能
  fix      — 修 Bug
  refactor — 重构（不改变行为）
  chore    — 工程配置、目录结构等
  docs     — 文档
```

示例：

```
feat: Add point cloud PLY parser
fix: Fix mesh vertex buffer overflow on large models
chore: Initialize project directory structure
```

### 注意事项

- `.meta` 文件必须全部纳入版本控制
- Library/、Temp/、Build/ 等已在 .gitignore 排除
- 不要提交 `UserSettings/`
- Qt 构建产物 (`QtUI/build*`, `QtUI/bin/`) 已在 .gitignore 排除

## AI 使用说明

本项目推荐使用 AI 辅助开发。

### 可用的 AI 工具

- **Claude** (Claude Code) — 架构设计、代码生成、重构
- **Cursor** — Agent 模式
- **DeepSeek V4** — 代码生成、问答

### AI 使用约定

1. **所有 AI 工具必须遵守 [AGENTS.md](./AGENTS.md)**，这是项目唯一的 AI 开发规范
2. AI 生成的代码必须通过人工 Review
3. AI 生成代码的注释语言：统一使用**中文**
4. 标识符（类名、方法名、变量名）使用**英文**
5. 涉及架构决策时，AI 应给出推荐方案和理由，由开发者最终决定

---

> **LiftingTwin** — 简单、模块化、可维护的数字孪生平台。
