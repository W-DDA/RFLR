# 赤狐启动器 (RFLR)

一个用 C# + WPF 写的 Minecraft 启动器，支持原版下载、Forge/Fabric 安装、中文 Mod 搜索。

## 功能

- **原版下载**：从 BMCLAPI 镜像源下载游戏，PCL2 风格的分组折叠版本列表
- **Forge / Fabric 安装**：一键安装主流模组加载器
- **中文 Mod 搜索**：内置中文映射表，输入中文自动翻译成英文去 Modrinth 搜索
- **实例隔离**：每个版本有独立的 `mods`、`saves`、`config` 文件夹，互不干扰
- **崩溃分析**：游戏崩溃后自动解析 crash-report，给出人类可读的提示
- **全局进度条**：下载进度在任何页面都能看到
- **启动动画**：带 logo 动画的启动画面

## 技术栈

- **语言**：C# / .NET 11
- **UI**：WPF（无边框圆角窗口，赤狐橙主题）
- **Minecraft 核心**：自研（`Core/` 文件夹），不依赖 CmlLib.Core
- **Mod 数据源**：Modrinth API
- **中文映射**：从 MC 百科爬取，通过 GitHub Release 单独分发

## 项目结构

```
RFLR/
├── Core/                   # 自研启动核心
│   ├── Download/           # 下载引擎（多线程、断点续传、SHA1 校验）
│   ├── Version/            # 版本解析、加载器安装
│   ├── Launch/             # 启动参数组装、进程管理、崩溃分析
│   ├── Mod/                # Mod 扫描
│   └── Util/               # JSON 工具
├── Modules/                # 业务模块
│   ├── ModConfig.cs        # 配置读写
│   ├── ModMinecraft.cs     # 本地版本扫描
│   └── ModrinthApi.cs      # Modrinth 搜索 + 中文翻译
├── MainWindow.xaml         # 主窗口（侧边导航）
├── GamePage.xaml           # 游戏页
├── DownloadPage.xaml       # 下载页
├── ModPage.xaml            # Mod 页
├── SettingsPage.xaml       # 设置页
├── SplashWindow.xaml       # 启动动画
└── CrashReportWindow.xaml  # 崩溃报告弹窗
```

## 编译

### 环境要求

- .NET 11 SDK（或 .NET 8）
- Windows 10 / 11

### 编译步骤

```bash
git clone https://github.com/W-DDA/RFLR.git
cd RFLR
dotnet build
dotnet run
```

### 发布单文件 exe

```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

输出在 `bin/Release/net11.0-windows/win-x64/publish/`。

## 中文映射数据

中文映射表 `modname_map.json` **不包含在源码仓库里**，通过 GitHub Release 单独分发。

启动器首次运行时会自动从 Release 下载。如果你想手动更新，可以从这里下载：

- [modname_map.json](https://github.com/W-DDA/RFLR/releases/download/v1.0-data/modname_map.json)

## 已知问题

- **Modrinth 数据源**：Modrinth 上的 Mod 数量比 CurseForge 少，部分老牌 Mod 搜不到
- **中文映射覆盖率**：目前约 700 条，覆盖主流 Mod，冷门 Mod 可能搜不到
- **仅支持 Windows**：WPF 是 Windows 专属框架，无法跨平台

## 贡献

欢迎提交 Issue 和 PR。如果你想补充中文映射，可以编辑 `modname_map.json` 后提交到 [Release](https://github.com/W-DDA/RFLR/releases)。

## 许可证

MIT License
