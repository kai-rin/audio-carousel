# Audio Carousel

[English](README.md) | [日本語](README.ja.md) | [简体中文](README.zh-Hans.md)

![Audio Carousel](docs/images/hero-zh-Hans.png)

一款轻量级 Windows 任务栏工具，通过单个全局热键在你指定的设备列表中循环切换系统默认音频输出设备。

灵感来自 [PeekDesktop](https://github.com/shanselman/PeekDesktop)：无需安装程序、单一可执行文件、便携式配置。

---

## 功能特性

- **一个热键，一次切换。** 在游戏、会议、浏览器等任何应用中按下热键，系统默认音频输出会立即跳到列表里的下一个设备，无需打开声音设置面板。
- **由你决定循环哪些设备。** 不论台式机还是笔记本，一般的 Windows PC 都会同时存在多种输出：主板/机身上的模拟接口（扬声器、耳机插孔、Line Out）、通过 DisplayPort 或 HDMI 传出的显示器音频、USB DAC 与耳机适配器、配过对的所有蓝牙耳机、虚拟声卡等。Audio Carousel 只在 **你选中的设备之间** 循环，所以循环序列短且可预测。
- **可进可退。** 一个热键切到下一个设备；可选的第二个热键切回上一个设备。
- **麦克风也能切换。** 可以另外登记一组录制设备（如桌面麦克风和耳机麦克风），用专用热键或托盘的「麦克风」子菜单切换。
- **切换有视觉反馈。** 鼠标所在显示器的右下角会弹出提示，显示新设备的名称（可关闭）。托盘图标的工具提示始终显示当前设备。
- **托盘常驻、鼠标可操作。** 左键点击托盘图标会打开设置（开启相应选项后也可像热键一样循环切换）。右键打开菜单，可在已注册设备列表中直接点击切换，还有切换到下一个/上一个设备、设置、关于、退出。
- **灵活的热键。** 修饰键组合（`Ctrl+Alt+A`）和可编程键盘 / 宏键盘上常见的 `F13`–`F24` 都支持。
- **通话设备可以保持不动。** 默认情况下，通话所用的设备（Teams、Discord、Zoom 等）也会一起切换；在设置中取消勾选一项，就能只切换音乐和游戏的输出，通话仍走耳机。
- **10 种语言界面。** English / 日本語 / 简体中文 / 繁體中文 / Español / Français / Deutsch / Português (Brasil) / Русский / 한국어。根据 Windows 显示语言自动选择，也可以在设置中手动切换。
- **纯文本 JSON 配置。** 设置保存在与可执行文件同目录的 `audio-carousel.json` 中。用任何文本编辑器打开即可看到完整内容，不必启动界面就能改热键或设备名，可纳入 git 进行版本管理，也能让脚本直接生成或写入。设置界面只是在写同一个文件。
- **可选随系统启动。** 启用「随 Windows 启动」只会在当前用户的 `HKCU\...\Run` 键写入一行——不需要管理员权限、不创建服务、不创建计划任务。
- **无需安装、无需管理员、无需 .NET 运行时。** 把 .exe 放在任意文件夹运行即可，自包含构建意味着不必再装其他东西。
- **能识别离线设备。** 拔线、断电或当前未连接的设备在设置界面会以灰色显示，循环时会自动跳过。蓝牙耳机尤其方便：把它常驻在循环列表里，只在实际连接的时候才会自动加入轮换。

## 下载与运行

1. 从 [Releases](https://github.com/kai-rin/audio-carousel/releases) 页面下载 `AudioCarousel-<版本>-win-x64.zip`，解压到任意文件夹（如 `C:\Tools\AudioCarousel\`）。压缩包内含 `AudioCarousel.exe` 和许可证文件。

   > **（可选）校验下载：** 每个版本都附有 `.sha256` 校验文件，构建过程也经过 GitHub Actions 证明（attestation）：
   > `gh attestation verify AudioCarousel-<版本>-win-x64.zip --repo kai-rin/audio-carousel`

2. 双击 `AudioCarousel.exe` 运行。

   > **首次运行提示：** 由于二进制文件未经签名，Windows SmartScreen 可能会显示「Windows 已保护你的电脑」。点击 **更多信息 → 仍要运行**。或者在启动前右键单击 .exe → **属性** → 勾选 **解除锁定** → **确定**。原因详见下文 *免责声明*。

3. 托盘图标（时钟附近）随即出现，首次运行时 **设置** 窗口会自动打开。

## 首次设置

在设置窗口中：

1. **添加要循环的音频输出设备。** 点击 *添加设备* 从下拉菜单中选择。常见组合：扬声器、耳机、通过 HDMI/DisplayPort 连接的显示器、蓝牙耳机。两个以上才能体现热键的价值。
2. **如有需要，调整顺序。** 用 *上移* / *下移*（或 `Ctrl+↑` / `Ctrl+↓`）设定循环顺序；按 `Delete` 删除选中的设备。
3. **设置热键。** 点击 *下一个设备* 输入框（或用 Tab 移过去后按 `Enter`），再按下你想用的键（如 `F16`、`Ctrl+Alt+A`、`Ctrl+Shift+F9`）。`F1`–`F24` 可单独使用；其他按键需要搭配 `Ctrl`、`Alt` 或 `Win`，以免占用日常输入。`Esc` 取消，`Backspace`（或 *清除*）移除热键。如有需要，可用同样方法设置 *上一个设备* 热键。
4. **（可选）界面语言。** 默认 *自动*（跟随 Windows）。也可以手动选择具体语言。
5. **（可选）随 Windows 启动。** 勾选后，登录时 Audio Carousel 会自动启动。
6. **（可选）麦克风。** 将列表切换到 *录制*，添加要循环的麦克风，并设置 *下一个麦克风* 热键。
7. **（可选）通话设备。** 如果希望通话保持在 Windows 中选定的设备上，请取消勾选 *同时切换通信设备（用于通话）*。
8. **（可选）提示与左键。** 可以关闭切换提示，或让左键点击托盘图标直接切换设备（默认关闭，以免会议中误点改变音频输出——左键会打开设置）。
9. 点击 **OK**。随后会弹出通知，告诉你托盘图标的位置和用法。Windows 11 中新的托盘图标起初会放在时钟旁的 `^`（隐藏的图标）里，把它拖到任务栏上即可一直显示。

## 日常使用

- **按下热键** 即可在任何应用中将音频跳到列表的下一个设备（*上一个设备* 热键则返回），鼠标所在显示器的右下角会弹出提示。
- **左键点击托盘图标** 打开设置（若开启「左键切换」选项，则与按热键效果相同；该选项推出前的配置文件仍保持左键切换）。
- **鼠标悬停在托盘图标上** 可查看当前输出设备；即使 Windows 或其他应用更改了设备，也会同步更新。
- **再次运行 `AudioCarousel.exe`** 会打开正在运行的 Audio Carousel 的设置窗口。
- **右键点击托盘图标** 打开菜单：
  - **已注册设备列表** — 当前设备带有勾选标记；点击其他设备即可直接切换（离线设备灰显）
  - **切换到下一个设备** / **切换到上一个设备** — 与热键相同
  - **麦克风** — 已登记的麦克风和 *切换到下一个麦克风*（登记麦克风后才显示）
  - **设置** — 重新打开设置窗口
  - **随 Windows 启动** — 不进设置即可切换
  - **关于** — 版本信息
  - **退出** — 关闭程序

## 配置与数据位置

- **配置文件：** 与 .exe 同目录的 `audio-carousel.json`。可手动编辑，也可从设置界面修改。保存时会经过临时文件 `audio-carousel.json.tmp`。无法解析为 JSON 的文件会备份为 `audio-carousel.json.bak` 并改用默认值；个别无效条目会被忽略。如果文件本身无法读取（例如被其他程序锁定），本次运行会使用默认值，且绝不会覆盖原文件。
- **注册表：** 启用「随 Windows 启动」时，会在 `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` 下写入一个值，关闭时会自动删除。如果在任务管理器的「启动应用」中禁用了本程序，程序内也会显示为关闭；从程序中重新开启时会一并清除该禁用标记（`...\Explorer\StartupApproved\Run`）。只影响当前用户，无需管理员权限。
- **更新：** 程序从不联网，因此不会提示新版本。在 GitHub 仓库设置 *Watch → Custom → Releases* 即可收到通知。
- **没有后台服务、没有计划任务、没有遥测。** 程序本体就是这一个 .exe 文件。

## 卸载

1. 右键托盘图标 → **设置** → 取消勾选 *随 Windows 启动* → **OK**（注册表项随之删除）。
2. 右键托盘图标 → **退出**。
3. 删除存放 `AudioCarousel.exe` 和 `audio-carousel.json` 的文件夹。

如果跳过了第 1 步，可以稍后用 `regedit` 删除 `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` 下的 `AudioCarousel` 值进行清理。

## 系统要求

- Windows 10 1809 或更新版本，或 Windows 11
- x64
- 无需管理员权限
- 无需 .NET 运行时（自包含构建，因此 exe 约 111 MB，下载的 zip 约 45 MB）

## 已知限制

- **同名设备。** 部分驱动（尤其是 NVIDIA 的 HDMI / DisplayPort 音频）在重启后会重新分配设备 ID，Audio Carousel 因此按名称重新找回设备。若有两个名称完全相同的输出（如两台同型号显示器），它们可能互换位置。在 Windows 声音设置中给其中一个改名即可避免。
- **系统保留的热键。** `Win+L` 等 Windows 自用的组合无法注册；设置界面会提示该组合不可用。

## 免责声明

Audio Carousel 调用 `IPolicyConfig` 这一未公开的 Windows COM 接口来切换默认音频终结点。许多同类工具（SoundSwitch、EarTrumpet、NirCmd 等）都采用相同手法，从 Windows Vista 到 Windows 11 一直稳定可用。但由于该 API 是非官方的，微软可能在未来的 Windows 更新中无预警地更改或移除它。如果未来的 Windows 更新导致 Audio Carousel 无法工作，请提交 Issue。

发布的二进制文件未经代码签名（代码签名证书需要持续的费用支出，本项目作为免费工具目前暂未引入）。在二进制声誉积累起来之前，Windows SmartScreen 会在首次启动时发出警告——参见 *下载与运行* 中的 *首次运行提示*。

## 安全性

威胁模型与安全问题报告方式见 [SECURITY.md](SECURITY.md)。

## 许可证

[MIT](LICENSE) — © 2026 Kairin。
随附的第三方组件及其许可证列于
[THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md) 中。

---

# 面向开发者

下面的内容仅供从源码构建的人使用。普通用户只需下载上方的发布版可执行文件即可，无需关心以下任何步骤。

## 技术栈

- C# / .NET 10 (LTS) / WinForms（`net10.0-windows`）
- 测试框架：xUnit
- `NAudio.Wasapi`（`NAudio.CoreAudioApi` 命名空间）+ `src/AudioCarousel/Audio/` 下的内联 `IPolicyConfig` COM 声明

## 构建与测试

需要 Windows 系统并安装 .NET 10 SDK。

```bash
dotnet build
dotnet test
dotnet format          # 按 .editorconfig 规整空白与换行符
```

## 发布单文件可执行程序

```powershell
pwsh ./scripts/publish.ps1
```

输出：`publish/AudioCarousel.exe`（约 111 MB，自包含）。如果 `publish/AudioCarousel.exe` 正在运行，脚本会报错停止；已有的 `publish/audio-carousel.json` 在重新构建后会保留。

`scripts/publish.ps1` 是生成发布版 exe **唯一支持的方式**。其参数经过精心调校，请不要手动拼装 `dotnet publish` 命令行。

> **关于二进制大小。** Windows Forms 与 NativeAOT 不兼容（`NETSDK1175`），而修剪（trimming）会移除 `NAudio.CoreAudioApi` 依赖的运行时 COM 互操作机制。因此该脚本采用 **未修剪、自包含、JIT、单文件发布**，生成约 111 MB 的可执行文件。这个体积是「将 .exe 放在任何位置即可运行，无需安装 .NET 运行时」的代价，是经过权衡后有意做出的取舍。

## 项目结构

架构概览、约定，以及不要触碰的关键细节（禁用 AOT/Trimming、原子化配置写入、测试隔离规则等）都整理在 [CLAUDE.md](CLAUDE.md) 中。设计文档位于 `docs/superpowers/specs/`。

