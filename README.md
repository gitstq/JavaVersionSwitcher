# Java Version Switcher 🚀

一个简洁易用的 Windows WPF 应用程序，帮助开发者快速切换不同的 Java 版本。

## 📋 项目简介

Java Version Switcher 是一个专为 Windows 平台设计的图形化工具，解决了开发过程中需要在多个 Java 版本之间切换的痛点。无需手动修改系统环境变量，一键即可完成 Java 版本切换。

## ✨ 主要特性

- 🎯 **一键切换**: 点击按钮即可切换 Java 版本
- 🖥️ **图形界面**: 基于 WPF 的现代化用户界面
- ⚡ **快速检测**: 自动检测当前系统 Java 版本
- 🔧 **多版本支持**: 支持 Java 7/8/11/17/25 等多个版本
- 📱 **轻量级**: 单文件发布，无需额外依赖
- 🔒 **管理员权限**: 自动请求管理员权限进行系统级配置

## 🚀 支持的 Java 版本

- Java 7 (1.7)
- Java 8 (1.8) - 包含 32位和64位版本
- Java 11
- Java 17  
- Java 25

## 📦 安装说明

### 方法一：下载发布版本
1. 访问 [Releases](https://github.com/gitstq/JavaVersionSwitcher/releases) 页面
2. 下载最新版本的 `JavaVersionSwitcher.exe`
3. 以管理员身份运行程序

### 方法二：从源码编译
```bash
# 克隆仓库
git clone https://github.com/gitstq/JavaVersionSwitcher.git

# 进入项目目录
cd JavaVersionSwitcher

# 还原依赖
dotnet restore

# 编译项目
dotnet build -c Release

# 发布单文件版本
dotnet publish -c Release -r win-x64 --self-contained true
```

## 🎯 使用方法

1. **启动程序**: 双击 `JavaVersionSwitcher.exe`（需要管理员权限）
2. **查看当前版本**: 程序会自动检测并显示当前 Java 版本
3. **切换版本**: 点击对应的 Java 版本按钮
4. **验证结果**: 切换完成后，程序会显示新的 Java 版本信息

### 界面说明
- **当前版本显示**: 实时显示系统当前的 Java 版本
- **版本按钮**: 每个按钮对应一个预配置的 Java 版本
- **状态提示**: 显示切换操作的状态和结果

## ⚙️ 配置说明

程序默认配置了以下 Java 版本路径：

```csharp
Java7    -> D:\java\jre-7u5--i586
Java8_32 -> D:\java\jdk8_32
Java8    -> D:\java\jdk-8u361
Java11   -> D:\java\jdk-11.0.23
Java17   -> D:\java\jdk-17.0.12
Java25   -> D:\java\jdk-25.0.1
```

如需修改路径，请编辑 `MainWindow.xaml.cs` 文件中的 `JavaVersionPaths` 字典。

## 🛠️ 技术栈

- **.NET 10.0**: 最新的 .NET 平台
- **WPF (Windows Presentation Foundation)**: 现代化的桌面应用框架
- **C#**: 现代化的面向对象编程语言
- **Visual Studio 2022**: 开发环境

## 📁 项目结构

```
JavaVersionSwitcher/
├── App.xaml                    # 应用程序入口
├── App.xaml.cs                # 应用程序逻辑
├── MainWindow.xaml            # 主界面设计
├── MainWindow.xaml.cs         # 主界面逻辑
├── JavaVersionSwitcher.csproj # 项目文件
├── bin/                       # 编译输出
│   ├── Debug/
│   └── Release/
└── obj/                       # 中间文件
```

## 🔧 开发环境要求

- Windows 10/11
- .NET 10.0 SDK
- Visual Studio 2022 或更高版本
- 管理员权限（用于修改系统环境变量）

## 📝 构建配置

项目支持两种构建配置：

### Debug 配置
- 用于开发和调试
- 生成标准可执行文件

### Release 配置
- 用于发布
- 生成单文件可执行程序
- 包含所有依赖项
- 启用 ReadyToRun 优化

## 🤝 贡献指南

欢迎提交 Issue 和 Pull Request 来改进这个项目！

### 开发步骤
1. Fork 这个仓库
2. 创建您的功能分支 (`git checkout -b feature/AmazingFeature`)
3. 提交您的更改 (`git commit -m 'Add some AmazingFeature'`)
4. 推送到分支 (`git push origin feature/AmazingFeature`)
5. 打开一个 Pull Request

## 🐛 已知问题

- 需要管理员权限运行（这是修改系统环境变量的必要要求）
- 大文件可能导致 Git 操作变慢（考虑使用 Git LFS）

## 📄 许可证

本项目采用 MIT 许可证 - 详情请查看 [LICENSE](LICENSE) 文件

## 🙏 致谢

- 感谢 .NET 团队提供优秀的开发平台
- 感谢 WPF 框架带来的现代化界面体验

## 📞 联系方式

如果您有任何问题或建议，欢迎通过以下方式联系：

- 提交 [Issue](https://github.com/gitstq/JavaVersionSwitcher/issues)
- 发送邮件（请在 GitHub 个人资料中查看）

---

⭐ 如果这个项目对您有帮助，请给个 Star 支持一下！

**Happy Coding! 🎉**