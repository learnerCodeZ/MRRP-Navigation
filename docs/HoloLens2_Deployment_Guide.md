# HoloLens 2 部署指南

本文档介绍如何将 MRReP 项目从 Unity 构建并部署到 HoloLens 2 设备。

---

## 1. 前置条件

| 组件 | 版本要求 |
|------|---------|
| Unity | 2022.3 LTS |
| MRTK | 2.8+ |
| Visual Studio | 2019/2022（含 UWP 工作负载） |
| Windows SDK | 10.0.18362.0+ |
| HoloLens 2 | Windows Holographic 21H1+ |
| ROS-TCP-Endpoint | 运行在 Ubuntu 20.04 上 |

### 1.1 Visual Studio 工作负载

安装 Visual Studio 时，确保勾选以下工作负载：

- **使用 Unity 的游戏开发**
- **通用 Windows 平台开发**
- 在单个组件中勾选：**Windows 10 SDK (10.0.18362.0)**

---

## 2. Unity 项目配置

### 2.1 切换平台为 UWP

1. 打开项目后，进入 **File → Build Settings**
2. 选择 **Universal Windows Platform**
3. 点击 **Switch Platform**（首次切换需要较长时间）
4. 等待 Unity 重新导入所有资源

### 2.2 UWP 构建设置

在 Build Settings 窗口中，确认以下设置：

| 设置项 | 值 |
|--------|-----|
| Target Device | HoloLens |
| Architecture | ARM64 |
| Build Type | D3D Project |
| Target SDK Version | 10.0.18362.0 或最新 |
| Minimum Platform Version | 10.0.18362.0 |
| Visual Studio Version | VS 2019 或 VS 2022 |
| Build and Run on | Local Machine（先构建，后部署） |

### 2.3 XR 插件配置

1. 进入 **Edit → Project Settings → XR Plug-in Management**
2. 切换到 **Universal Windows Platform** 选项卡
3. 勾选 **Windows Mixed Reality**
4. 进入 **Edit → Project Settings → XR Plug-in Management → Windows Mixed Reality**
5. 确认：
   - Depth Buffer Format: **16-bit depth**
   - Shared Depth Buffer: **勾选**

### 2.4 项目能力声明

1. 进入 **Edit → Project Settings → Player**
2. 切换到 **Universal Windows Platform** 选项卡
3. **Publishing Settings → Capabilities** 中勾选：
   - `InternetClient`
   - `InternetClientServer`
   - `PrivateNetworkClientServer`
   - `SpatialPerception`
   - `Microphone`（如需语音输入）
   - `GazeInput`
4. **Other Settings → Rendering** 中：
   - Color Space: **Linear**
   - 取消勾选 **Graphics Jobs**（HoloLens 2 上可能导致崩溃）

### 2.5 MRTK 配置

1. 确保场景中存在 `MixedRealityToolkit` 物体
2. 选中 MixedRealityToolkit → Inspector → 配置 Profile:
   - Default HoloLens 2 Configuration Profile（或自定义 Profile）
3. 确认 **Diagnostics** → Enable Verbose Logging: **取消勾选**（发布版本）
4. 确认 **Input** → Hand Tracking 已启用

---

## 3. 配置 ROS 连接

### 3.1 设置 ROS IP

1. 在 Unity 场景中选中 `ROSConnectionManager` 物体
2. Inspector 中设置：
   - **ROS IP Address**: Ubuntu 主机的 IP 地址（如 `192.168.1.100`）
   - **ROS Port**: `10000`（与 ROS 端 tcp_endpoint.py 一致）
3. 确保 HoloLens 2 和 Ubuntu 主机在同一局域网内

### 3.2 ROS 端启动

```bash
cd ros_ws
source devel/setup.bash
rosrun mrrep tcp_endpoint.py
rosrun mrrep pure_pursuit.py
```

---

## 4. 构建与部署

### 4.1 构建 Unity 项目

1. **File → Build Settings**
2. 确认场景 `Assets/Scenes/MainScene` 已添加到 **Scenes In Build**
3. 点击 **Build**
4. 选择一个空文件夹作为输出目录（如 `Build/HoloLens2/`）
5. 等待构建完成，完成后会自动打开该文件夹

### 4.2 Visual Studio 编译部署

构建完成后，Unity 会生成一个 `.sln` 解决方案文件：

1. 用 Visual Studio 打开输出目录中的 `.sln` 文件
2. 顶部工具栏设置：
   - **Solution Configuration**: `Master` 或 `Release`
   - **Solution Platform**: `ARM64`
3. 连接 HoloLens 2：

#### 方式一：USB 连接（推荐首次部署）

1. 用 USB-C 线连接 HoloLens 2 和电脑
2. VS 工具栏部署目标选择 **Device**
3. 如提示配对，在 HoloLens 上确认配对 PIN

#### 方式二：Wi-Fi 远程部署

1. 确保 HoloLens 2 和电脑在同一 Wi-Fi
2. HoloLens 上进入 **Settings → Network & Internet** 查看 IP
3. VS 工具栏部署目标选择 **Remote Machine**
4. 输入 HoloLens 2 的 IP 地址
5. Authentication Mode 选择 **Universal (Unencrypted Protocol)**

4. 点击 **Debug → Start Without Debugging**（或 `Ctrl+F5`）
5. 等待编译、部署、安装完成
6. 应用会自动在 HoloLens 2 上启动

### 4.3 命令行构建 + Device Portal 旁加载（无需 Visual Studio GUI）

此方式无需打开 Visual Studio，通过命令行构建 `.appx`，再通过 Device Portal 安装到 HoloLens 2。

#### 步骤一：命令行构建 .appx
>***将.sln文件转换为.appx可以直接交给AI做***

在 Windows 终端中执行（需安装 Visual Studio 及 UWP 工作负载）：

```powershell
# 打开 VS Developer Command Prompt，或手动设置环境
cd <Unity构建输出目录>

# 还原 NuGet 包
msbuild <项目名>.sln /p:Configuration=Master /p:Platform=ARM64 /t:Restore

# 构建 .appx
msbuild <项目名>.sln /p:Configuration=Master /p:Platform=ARM64 /p:AppxBundle=Never
```

构建完成后，`.appx` 文件位于：
```
<输出目录>\ARM64\Master\<项目名>\<项目名>.appx
```

> 如果构建报错缺少证书，先构建一次 Certificate 项目：
> ```powershell
> msbuild <项目名>.sln /p:Configuration=Master /p:Platform=ARM64 /t:<项目名> /p:AppxBundle=Never
> ```

#### 步骤二：上传 .appx 到 HoloLens 2

通过 Windows Device Portal 旁加载：

1. 确保 HoloLens 2 已开启 **Developer Mode** 和 **Device Portal**
   - **Settings → Update & Security → For developers**
2. 在电脑浏览器中打开 `https://<HoloLens-IP>`，完成配对
3. 进入 **Apps → Apps Manager**
4. 在 **Install App** 区域：
   - **Local Package**：选择上一步生成的 `.appx` 文件
   - 如有依赖文件夹（如 `Dependencies`），一并上传
5. 点击 **Install**，等待安装完成

#### 步骤三：启动应用

- 在 HoloLens 2 的 **Start Menu** 中找到应用并启动
- 或在 Device Portal 的 **Apps → Running Apps** 中启动

> **注意**：首次安装自签名应用时，HoloLens 可能提示信任证书。如提示，确认安装即可。

---

## 5. 运行验证

部署成功后，在 HoloLens 2 上：

1. 应用启动后，MRTK 会初始化手部追踪
2. 伸出手掌朝向自己，应弹出主菜单
3. 点击 **Preferred Path** 进入路径编辑界面
4. 捏合拇指和食指，在空间中画线
5. 确认 ROS 端已运行，点击 **SEND** 发送路径
6. 观察虚拟小车是否沿路径运动

### 验证清单

- [ ] 主菜单可通过手掌朝上呼出
- [ ] 按钮可通过 Air Tap 点击
- [ ] 捏合手势可画线
- [ ] SEND 后 ROS 端收到路径数据
- [ ] 虚拟小车沿路径运动
- [ ] 空间锚点正常工作（关闭应用重新打开后路径不漂移）

---

## 6. 常见问题

### 构建错误：CS0234 命名空间不存在
- 确认 MRTK 和 ROS-TCP-Connector 已正确导入
- 删除 `Library` 文件夹，重新打开项目让 Unity 刷新

### 部署失败：DEP0700 注册失败
- 在 HoloLens 上卸载旧版本应用后重新部署
- **Settings → Apps** 中找到应用并卸载

### 应用启动后黑屏
- 检查 Color Space 是否设为 **Linear**
- 检查 XR Plug-in Management 中 Windows Mixed Reality 是否启用
- 确认 Graphics Jobs 已取消勾选

### 手势追踪不工作
- 确认 HoloLens 手部追踪权限已开启
- 确认 MRTK Profile 中 Hand Tracking 已启用
- 确保环境光线充足

### 无法连接 ROS
- 确认 HoloLens 和 Ubuntu 在同一局域网
- 检查 ROSConnectionManager 的 IP 和端口配置
- 检查 Ubuntu 防火墙是否放行端口 10000
- 在 Ubuntu 上测试：`rosrun mrrep tcp_endpoint.py` 正常启动无报错

### 性能问题（帧率低）
- 降低渲染质量：**Edit → Project Settings → Quality** 选择最低级别
- 减少 LineRenderer 的采样点数量
- 关闭 MRTK Diagnostics 日志
- 确认使用 ARM64 而非 x86/x64

---

## 7. Device Portal 调试

通过 Windows Device Portal 可以远程查看 HoloLens 状态：

1. HoloLens 上进入 **Settings → Update & Security → For developers**
2. 启用 **Developer Mode** 和 **Device Portal**
3. 在电脑浏览器中访问 `https://<HoloLens-IP>`（首次需配对）
4. 可用功能：
   - 查看应用日志（**System → Logs**）
   - 查看实时性能（**Performance → Performance Monitor**）
   - 截图和录屏（**Capture**）
   - 远程安装/卸载应用