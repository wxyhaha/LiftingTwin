# AGENTS.md — LiftingTwin AI 开发规范

> **这是项目唯一的 AI 开发规范文件。**
>
> 适用于：Codex、DeepSeek V4、Claude、Claude Code、Cursor、GitHub Copilot 等所有 AI 工具。
>
> AI 在生成、修改、建议本项目代码时，必须遵守此文件中的所有规则。

---

## 1. 项目定位

LiftingTwin 是一个**数字孪生桌面应用**，**不是游戏**。

- 负责：桌面端三维可视化渲染
- 不负责：任何算法（算法由外部团队提供）
- 输入：Mesh、PointCloud（PLY / PCD）、Pose（Position + Rotation）
- 网络：WebSocket / TCP 接收实时数据

**核心原则：只做渲染和展示，不做业务算法。**

---

## 2. 开发原则

### 2.1 首要原则

| 优先级 | 原则 | 说明 |
|-------|------|------|
| 1 | 简单 | 能用简单方案就不用复杂方案 |
| 2 | 模块化 | 每个模块职责单一、边界清晰 |
| 3 | 可维护 | 代码清晰、注释合理、易于理解 |
| 4 | 可扩展 | 方便后续接入算法实时数据 |

### 2.2 禁止过度设计

- **不要为了架构而架构**：不需要的模块不要提前创建
- **不要过早抽象**：确认有 3 个以上使用场景时再抽象
- **不要为了设计模式而使用设计模式**：模式是手段不是目的
- **不要引入游戏开发专用架构**：不需要 ECS、不需要 FSM、不需要技能系统
- **不要创建 God Object**：一个类只做一件事
- **不要滥用 Singleton**：优先使用依赖注入 / ScriptableObject 引用

### 2.3 代码哲学

- 先让它工作，再让它正确，最后让它优雅
- 能用 C# 原生方案就用原生方案，不引入不必要的第三方库
- 每个新增功能都尽量模块化，即插即用，即删即净
- 公共 API 必须添加 XML 注释（`<summary>` 标签）

---

## 3. 命名规范

### 3.1 标识符（英文）

| 类型 | 规范 | 示例 |
|------|------|------|
| 命名空间 | PascalCase | `LiftingTwin.Network` |
| 类 / 结构体 | PascalCase | `PointCloudRenderer` |
| 接口 | `I` + PascalCase | `IDataReceiver` |
| 枚举 | PascalCase | `LogLevel` |
| 方法 | PascalCase | `LoadMesh()` |
| 属性 | PascalCase | `MaxPointCount` |
| 字段（private） | `_camelCase` | `_pointCloudBuffer` |
| 字段（public） | PascalCase | `ServerPort` |
| 参数 / 局部变量 | camelCase | `frameId` |
| 常量 | PascalCase | `DefaultPort` |
| 静态只读 | PascalCase | `MinLogLevel` |

### 3.2 注释（中文）

- 所有 XML 注释使用中文
- 行内解释性注释使用中文
- 模块头部概述使用中文
- 日志消息使用英文（便于搜索和 log 分析）

```csharp
/// <summary>
/// 将原始字节数组解析为点云帧。
/// 返回 null 表示解析失败。
/// </summary>
/// <param name="data">原始字节数据</param>
/// <returns>解析后的点云帧，失败返回 null</returns>
public PointCloudFrame Parse(byte[] data)
```

### 3.3 文件命名

- 一个文件一个类（特殊情况允许少量关联的枚举/结构体）
- 文件名 = 类名，例如 `PointCloudRenderer.cs`
- Editor 脚本放在与目标类相同的目录下的 `Editor/` 子目录

---

## 4. 代码规范

### 4.1 MonoBehaviour 使用原则

**尽量减少 MonoBehaviour 职责。**

MonoBehaviour 应该只做：
- 作为场景中的入口点（挂载在 GameObject 上）
- 接收 Unity 生命周期事件（Awake, Start, Update 等）
- 连接 Unity Inspector 与纯 C# 逻辑

MonoBehaviour **不应该**做：
- 包含复杂业务逻辑（提取到纯 C# 类）
- 直接操作网络（交给 Network 模块）
- 直接操作渲染管线（交给 Visualization 模块）

```csharp
// ✅ 好的做法：MonoBehaviour 只做桥接
public class PointCloudView : MonoBehaviour
{
    [SerializeField] private AppConfig config;
    private PointCloudRenderer _renderer;

    private void Awake()
    {
        _renderer = new PointCloudRenderer(config);
    }

    private void Update()
    {
        _renderer.Render(Time.deltaTime);
    }
}

// ❌ 坏的做法：MonoBehaviour 包含全部逻辑
public class PointCloudManager : MonoBehaviour
{
    // 500 lines of parsing, networking, rendering all in one class
}
```

### 4.2 类设计

- 优先使用纯 C# 类（不继承 MonoBehaviour）
- 单个类不超过 300 行（如有必要可放宽到 500 行）
- 公共方法参数不超过 5 个（超过则考虑封装为数据类）
- 避免 `public` 字段，使用属性封装

### 4.3 错误处理

- 不要吞掉异常（至少记录日志）
- 网络数据解析失败时记录 Warn，不影响主循环
- 渲染异常记录 Error，降级处理（如显示空场景而非崩溃）
- 使用 `Log.Exception()` 记录带 Exception 的错误

### 4.4 性能

- 避免在 Update 中分配内存（使用对象池 / 预分配 buffer）
- 点云和 Mesh 数据使用 NativeArray 或预分配的托管数组
- 大对象使用 `struct` 避免 GC 压力
- 不需要每帧执行的逻辑放到 Coroutine 或事件驱动

---

## 5. 目录规范

### 5.1 放置规则

| 放什么 | 放哪里 |
|--------|--------|
| ScriptableObject 配置文件 | `Assets/_Config/` |
| 场景文件 | `Assets/_Scenes/` |
| C# 源码 | `Assets/Scripts/<模块>/` |
| 材质、Shader、贴图 | `Assets/Art/` 对应子目录 |
| 预制体 | `Assets/Prefabs/` |
| DLL 插件（A/V/硬件 SDK 等） | `Assets/Plugins/` |
| 第三方源码 | `Assets/ThirdParty/` |
| 运行时通过 Resources.Load 加载的 | `Assets/Resources/` |
| 构建后保持原样的文件（PLY, JSON 等） | `Assets/StreamingAssets/` |
| UI 资源（UGUI / UI Toolkit） | `Assets/UI/` |

### 5.2 禁止放置

- **禁止**在 `Assets/` 根目录创建源码文件
- **禁止**在 `Resources/` 中大量存放资源（增加启动时间和包体）
- **禁止**在 `StreamingAssets/` 中存放源码

---

## 6. 模块职责

| 模块 | 命名空间 | 职责 |
|------|---------|------|
| Core | `LiftingTwin.Core` | 应用入口、生命周期、全局配置 |
| Runtime | `LiftingTwin.Runtime` | 主循环、帧同步、数据调度 |
| Mesh | `LiftingTwin.Mesh` | Mesh 数据结构与动态更新渲染 |
| PointCloud | `LiftingTwin.PointCloud` | 点云数据结构与渲染 |
| Network | `LiftingTwin.Network` | WebSocket / TCP 客户端与数据协议 |
| Visualization | `LiftingTwin.Visualization` | 相机控制、渲染辅助、后处理 |
| Utils | `LiftingTwin.Utils` | 通用工具（日志、数学、扩展方法） |

### 6.1 模块间依赖

```
Core ← 所有模块（提供配置和生命周期）
Utils ← 所有模块（提供通用工具）
Network → Core + Utils（接收数据，不渲染）
Mesh → Core + Utils（渲染 Mesh，不关心数据来源）
PointCloud → Core + Utils（渲染点云，不关心数据来源）
Visualization → Core + Utils（相机控制等）
Runtime → Core + Network + Mesh + PointCloud + Visualization（调度层）
```

---

## 7. 日志规范

### 7.1 统一使用 `LiftingTwin.Utils.Log`

```csharp
// ✅ 正确
Log.Info ("Network", "Connected to {0}:{1}", host, port);
Log.Warn ("PointCloud", "Frame {0} dropped — zero points", id);
Log.Error("Mesh", "Vertex buffer overflow: {0}", count);

// ❌ 禁止
Debug.Log("connected");       // 没有 tag，没有级别
Debug.LogWarning("error!");   // 裸调用
Console.WriteLine("...");     // 绕过日志系统
```

### 7.2 Tag 命名

- Tag 使用模块名：`"Network"`, `"Mesh"`, `"PointCloud"`
- 子系统用点号分隔：`"Network.WebSocket"`, `"Mesh.Loader"`

---

## 8. 禁止事项

### 8.1 绝对禁止

| 禁止 | 原因 |
|------|------|
| 创建 God Object（一个类做所有事） | 难以维护和测试 |
| 滥用 Singleton（`Instance` 满天飞） | 隐藏依赖关系，难以测试 |
| 在 `Update()` 中 new 大量对象 | GC 导致帧率波动 |
| 吞掉异常不记录日志 | 问题无法追踪 |
| 将核心逻辑写在 MonoBehaviour 中 | 难以测试和复用 |
| 在 `Resources/` 中放大量文件 | 增加启动时间 |
| 直接 `Debug.Log` | 绕过日志系统，无法统一管理 |
| 裸写魔法数字 | 没有语义，难以理解 |

### 8.2 谨慎使用

| 事项 | 建议 |
|------|------|
| Coroutine | 可以，但不要在热路径中使用 |
| `Update()` | 尽量少用，优先事件驱动 |
| `FindObjectOfType<>()` | 很慢，用 ScriptableObject 引用替代 |
| `SendMessage()` | 反射开销大，用直接调用替代 |
| `Resources.Load()` | 可以，但仅用于少数默认配置 |
| `PlayerPrefs` | 仅用于用户偏好，不用于大量数据 |

---

## 9. 配置使用规范

| 技术 | 用途 |
|------|------|
| `ScriptableObject` | 首选。编辑器可编辑，运行时只读 |
| `Resources/` | 存放默认 ScriptableObject 实例 |
| `StreamingAssets/` | 存放运行时可能被外部替换的配置文件（JSON 等） |
| `PlayerPrefs` | 用户个人偏好（音量、语言等） |

---

## 10. AI 生成代码的额外检查项

AI 在生成代码后，应自行检查：

1. ☐ 命名是否符合本规范的 PascalCase / camelCase 约定？
2. ☐ 公共 API 是否有 XML 注释（中文）？
3. ☐ 类是否单一职责（不超 500 行）？
4. ☐ 是否使用了 `Debug.Log` 而非 `Log`？
5. ☐ MonoBehaviour 是否包含了不该有的业务逻辑？
6. ☐ 是否引入了不必要的第三方依赖？
7. ☐ 是否为了设计模式而设计模式？
8. ☐ 错误路径是否都有日志记录？

---

> **记住：简单 > 复杂，清晰 > 聪明，够用 > 完美。**
