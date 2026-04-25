# Audio Carousel

[English](README.md) | [日本語](README.ja.md) | [简体中文](README.zh-Hans.md)

一款轻量级 Windows 任务栏工具，通过单个全局热键在配置好的设备列表中循环切换系统默认音频输出设备。

灵感来自 [PeekDesktop](https://github.com/shanselman/PeekDesktop)：无需安装程序、单一可执行文件、便携式配置。

## 使用方法

1. 从 [Releases](https://github.com/kai-rin/audio-carousel/releases) 页面下载 `AudioCarousel.exe`，并放到任意文件夹。
2. 双击运行。

   > 首次运行提示： 由于二进制文件未经签名，Windows SmartScreen 可能会显示「Windows 已保护你的电脑」。点击 更多信息 → 仍要运行。或者在启动前右键单击 .exe → 属性 → 勾选 解除锁定 → 确定。

3. 托盘图标随即出现，首次运行时设置窗口会自动打开。
4. 添加需要循环切换的音频输出设备，设置热键（如 `F16`、`Ctrl+Alt+A`），然后点击「确定」。
5. 按下热键即可在设备间循环切换。活动显示器右下角会弹出提示，显示新设备的名称。
6. 可选：在托盘菜单或设置中启用「随 Windows 启动」。

配置保存在与可执行文件同目录下的 `audio-carousel.json` 中。仅当启用「随 Windows 启动」时才会写入注册表项 `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`，且只影响当前用户。无需管理员权限。

## 构建

需要 Windows 系统并安装 .NET 9 SDK。

```bash
dotnet build
dotnet test
```

## 发布单文件可执行程序

```powershell
pwsh ./scripts/publish.ps1
```

输出：`publish/AudioCarousel.exe`。

> 关于二进制大小： Windows Forms 与 NativeAOT 不兼容（`NETSDK1175`），而修剪（trimming）会移除 `NAudio.CoreAudioApi` 依赖的运行时 COM 互操作机制。因此该脚本采用 未修剪、自包含、JIT、单文件发布，生成约 108 MB 的可执行文件。这个体积是「将 .exe 放在任何位置即可运行，无需安装 .NET 运行时」的代价。

## 系统要求

- Windows 10 1809 或更新版本，或 Windows 11
- x64
- 无需管理员权限

## 免责声明

Audio Carousel 调用 `IPolicyConfig` 这一未公开的 Windows COM 接口来切换默认音频终结点。许多同类工具（SoundSwitch、EarTrumpet、NirCmd 等）都采用相同手法，从 Windows Vista 到 Windows 11 一直稳定可用。但由于该 API 是非官方的，微软可能在未来的 Windows 更新中无预警地更改或移除它。如果未来的 Windows 更新导致 Audio Carousel 无法工作，请提交 Issue。

发布的二进制文件未经代码签名（代码签名证书需要持续的费用支出，本项目作为免费工具目前暂未引入）。在二进制声誉积累起来之前，Windows SmartScreen 会在首次启动时发出警告——参见 使用方法 中的 *首次运行提示*。

## 安全性

威胁模型与安全问题报告方式见 [SECURITY.md](SECURITY.md)。

## 许可证

[MIT](LICENSE) — © 2026 kai-rin。
随附的第三方组件及其许可证列于
[THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md) 中。
