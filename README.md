# PowerTwin

输变电工程吊装作业三维动态安全管控验证平台

## 项目简介

PowerTwin 是一个面向输变电工程吊装作业的数字孪生三维可视化验证平台。

Unity 端仅负责**桌面端三维可视化渲染**，不负责任何算法。

算法团队通过实时数据通道向 Unity 推送 Mesh、PointCloud、Pose 等数据，Unity 负责动态更新显示。

## 技术栈

| 项 | 版本/选型 |
|---|----------|
| Unity | 2022.3.62f3 LTS |
| 渲染管线 | Universal Render Pipeline (URP) |
| 目标平台 | Windows Desktop (Standalone) |
| 语言 | C# |
| 版本控制 | Git |
| AI 辅助 | Codex, DeepSeek V4, Claude, Cursor 等 |

**关键 Package：**

- `com.unity.render-pipelines.universal` — URP 渲染管线
- `com.unity.textmeshpro` — 文字渲染
- `com.unity.ugui` — UI 系统

## 目录说明

```
Assets/
├── _Config/           配置资产（ScriptableObject 实例）
├── _Scenes/           场景文件
├── Art/               美术资源
│   ├── Materials/     材质
│   ├── Shaders/       自定义 Shader（HLSL / Shader Graph）
│   └── Textures/      贴图
├── Plugins/           平台原生插件（.dll / .a / .so）
├── Prefabs/           预制体
├── Resources/         Runtime 动态加载资源（谨慎使用）
├── Scripts/           所有 C# 代码
│   ├── Core/          应用入口、生命周期、配置
│   ├── Runtime/       主循环、帧同步、数据调度
│   ├── Mesh/          Mesh 动态更新
│   ├── PointCloud/    点云显示
│   ├── Network/       WebSocket / TCP 数据接收
│   ├── Visualization/ 相机控制、渲染辅助、后处理
│   └── Utils/         日志、扩展方法、数学工具
├── StreamingAssets/   原始文件（按原样复制到构建）
├── ThirdParty/        第三方源码
└── UI/                UI 相关资产（UGUI / UI Toolkit）
```

## 开发流程

### 环境准备

1. 安装 Unity Hub
2. 通过 Unity Hub 安装 **Unity 2022.3.62f3 LTS**（务必精确版本）
3. Clone 本项目
4. 在 Unity Hub 中 Add Project，选择本仓库根目录
5. Unity 会自动下载 Package 依赖

### 日常开发

1. 拉取最新 `main`
2. 基于 `main` 创建 feature 分支：`feature/<模块>/<简述>`
3. 开发 + 自测
4. 提交 PR 到 `main`

### 构建

- File > Build Settings > Target Platform: Windows, Mac, Linux (Standalone)
- Architecture: Intel 64-bit
- Build

## Git 规范

### 分支策略

- `main` — 稳定主线，只通过 PR 合入
- `feature/<模块>/<简述>` — 功能分支，例如 `feature/mesh/dynamic-update`
- `fix/<简述>` — 修复分支

### Commit 规范

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

## AI 使用说明

本项目推荐使用 AI 辅助开发。

### 可用的 AI 工具

- **Codex** (OpenAI) — 代码补全
- **DeepSeek V4** — 代码生成、重构
- **Claude** (Claude Code / Cursor) — 复杂任务、架构设计
- **Cursor** — Agent 模式

### AI 使用约定

1. **所有 AI 工具必须遵守 [AGENTS.md](./AGENTS.md)**，这是项目唯一的 AI 开发规范
2. AI 生成的代码必须通过人工 Review
3. AI 生成代码的注释语言：统一使用**中文**
4. 标识符（类名、方法名、变量名）使用**英文**
5. 涉及架构决策时，AI 应给出推荐方案和理由，由开发者最终决定

### AI 使用示例

```
// 在 AGENTS.md 约束下，AI 可以：
- 生成符合规范的 C# 脚本
- 生成 Git Commit Message
- 解释现有代码逻辑
- 提出重构建议
```

## 后续开发计划

| 阶段 | 内容 | 状态 |
|------|------|------|
| 1 | 工程初始化（目录、配置、规范） | ✅ 当前 |
| 2 | 相机控制系统 | 🔲 待开始 |
| 3 | WebSocket / TCP 网络层 | 🔲 待开始 |
| 4 | Mesh 动态更新渲染 | 🔲 待开始 |
| 5 | PointCloud 点云显示 | 🔲 待开始 |
| 6 | 完整数据协议对接 | 🔲 待开始 |
| 7 | UI 面板（数据监控、调试面板） | 🔲 待开始 |

---

> **PowerTwin** — 简单、模块化、可维护的数字孪生平台。
