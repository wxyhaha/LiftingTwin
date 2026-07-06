# LiftingTwin 项目初始化 Checklist

## 一、工程结构

- [x] 创建 Assets 完整目录结构
- [x] 创建 Scripts 子模块目录（Core, Runtime, Mesh, PointCloud, Network, Visualization, Utils）
- [x] 删除 SampleScene
- [x] 删除 TutorialInfo 模板资源
- [x] 删除 URP SampleSceneProfile
- [ ] **在 Unity Editor 中创建 Main Scene** → `Assets/_Scenes/MainScene.unity`
- [ ] **创建 Bootstrap GameObject**，挂载 `AppBootstrap.cs`，引用 `AppConfig.asset`
- [ ] **创建 AppConfig 资产** → `Assets/_Config/AppConfig.asset`
  - Unity 菜单：`Assets > Create > LiftingTwin > App Config`
  - 拖放到 `Assets/_Config/` 目录

## 二、Project Settings（详见下方说明）

- [ ] **Color Space → Linear**
  - Edit > Project Settings > Player > Other Settings > Color Space
- [ ] **API Compatibility Level → .NET Standard 2.1**
  - Edit > Project Settings > Player > Other Settings > Configuration
- [ ] **Scripting Backend → IL2CPP**（发布时，开发期可用 Mono）
- [ ] **Asset Serialization Mode → Force Text**
  - Edit > Project Settings > Editor > Asset Serialization
- [ ] **Version Control Mode → Visible Meta Files**
  - Edit > Project Settings > Editor > Version Control
- [ ] **Target Frame Rate → 60**
  - 通过 AppConfig 配置（`Application.targetFrameRate`）
- [ ] **Quality Settings** — 使用 URP-HighFidelity 作为默认
  - Edit > Project Settings > Quality
- [ ] **Company Name → LiftingTwin Team**
  - Edit > Project Settings > Player > Company Name
- [ ] **Product Name → LiftingTwin**
  - Edit > Project Settings > Player > Product Name
- [ ] **Default Window Size → 1920×1080**
  - Edit > Project Settings > Player > Resolution
- [ ] **Allow Fullscreen Switch → true**
  - Edit > Project Settings > Player > Resolution

## 三、Git

- [x] 创建 .gitignore
- [ ] **初始化 Git 仓库**：`git init`
- [ ] **首次 Commit**

## 四、文档

- [x] 创建 README.md
- [x] 创建 AGENTS.md

## 五、验证

- [ ] Unity 打开项目无报错
- [ ] Main Scene 能正常进入 Play Mode
- [ ] AppBootstrap 正确加载 AppConfig
- [ ] Log 系统正常输出
- [ ] .gitignore 生效（Library/, Temp/, UserSettings/ 不被追踪）

---

## Project Settings 推荐说明

### Color Space → Linear（必须）

**原因：**

- 数字孪生需要准确的物理光照计算
- Linear 空间下的渲染更真实，色彩混合正确
- URP 官方推荐的色彩空间
- 代价：对硬件要求略高，但桌面端完全没问题

### API Compatibility Level → .NET Standard 2.1

**原因：**

- .NET Standard 2.1 提供更现代的 C# 特性（Span, ValueTask 等）
- 适合数据处理密集型应用（点云、Mesh 数据解析）
- 与 .NET Framework 4.x 相比，跨平台支持更好

### Scripting Backend（开发期 Mono，发布期 IL2CPP）

**原因：**

- Mono 构建快，适合日常开发迭代
- IL2CPP 性能更好，发布版本推荐使用
- IL2CPP 的 C++ 编译可带来明显的 CPU 密集型数据处理性能提升

### Asset Serialization → Force Text

**原因：**

- 场景和预制体的 YAML 文本格式，Git diff 可读
- 合并冲突时可以手动解决
- 这是 Unity 官方推荐的企业团队设置

### Version Control → Visible Meta Files

**原因：**

- `.meta` 文件是 Unity 资产的身份证，必须纳入版本控制
- Visible 模式确保 `.meta` 在文件系统中可见，便于 Git 管理

### Target Frame Rate → 60

**原因：**

- 数字孪生桌面应用，60fps 保证流畅的三维交互
- 不是移动端或 VR，不需要 90/120fps
- 过高的帧率白白消耗 GPU，数字孪生场景通常包含大量数据

### Quality → URP-HighFidelity

**原因：**

- 桌面端硬件性能足够
- 数字孪生对渲染质量要求高（Mesh 细节、光照精度）
- URP 自带的三个预设中，HighFidelity 最适配

### Default Resolution → 1920×1080

**原因：**

- 最常见的目标显示器分辨率
- 用户可以拖拽调整窗口大小

---

## 目录使用规范详细说明

### Resources — Runtime 加载

**何时放入：**

- 需要通过 `Resources.Load<T>()` 动态加载的默认 ScriptableObject 配置
- UI 预制体（如果用 UGUI 传统方案）
- 数量极少（通常 < 10 个）、体积小的必要资源

**何时不要放入：**

- 大量资源（Resources 下所有资源在启动时都会被索引，拖慢启动速度）
- Mesh、点云数据、大纹理（用 StreamingAssets）
- 第三方库的 Bundle（用 Plugins 或 ThirdParty）

### StreamingAssets — 保持原样

**何时放入：**

- 运行时可能被外部替换的配置文件（JSON, YAML, XML）
- 算法团队输出的测试数据（PLY, PCD 等）
- 需要在构建后仍可见为独立文件的数据

**何时不要放入：**

- C# 源码
- Unity 可识别并处理的资产（贴图、材质、预制体——这些放在 Art/ 或 Prefabs/）
- 需要 Unity 预处理优化的资源（StreamingAssets 中的资源不会被 Unity 优化）

### ThirdParty — 第三方源码

**何时放入：**

- 第三方 C# 源码库（如 JSON 解析器、网络库源码）
- 包含 `.cs` 文件，需要被 Unity 编译的第三方代码

**何时不要放入：**

- 已编译的 DLL（放 Plugins/）
- Unity Package Manager 可管理的依赖（优先用 UPM）
- 自己写的业务代码（放 Scripts/）

### Plugins — 原生插件

**何时放入：**

- 预编译的 DLL（Windows .dll, macOS .bundle, Linux .so）
- 原生 SDK（如硬件厂商提供的 SDK）
- 需要特定平台编译设置的插件

**何时不要放入：**

- C# 源码（放 Scripts/ 或 ThirdParty/）
- 纯托管代码的 DLL（考虑放 ThirdParty/ 或通过 UPM 管理）
- 仅 Editor 使用的脚本（放 Editor/ 目录下）

---

> 完成所有 Checkbox 后，项目初始化阶段结束，可以进入功能开发。
