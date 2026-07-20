# HoloLens 2 部署指南（v2 · 实测版）

> **版本**：v2，2026-07-18 重写。基于本机**实测部署走通**的过程，修正了 v1 里过时/会踩坑的内容（XR 改 OpenXR、ROS 端改 `start.launch`、Target SDK 锁 10.0.19041、补 OpenXR 插件 + 代码适配 + 沉浸式排查）。
> **配套**：开发期迭代见 `HL2开发迭代方式-原生部署与全息远程-20260718.md`；交互设计见 `MRReP_菜单与画线交互规范.md` / `MRReP_UI_Design_Guidelines.md`。

---

## 0. 关键认知（先读）

- **HL2 只认 UWP 应用**，Standalone 的 `.exe` 装不上 → 必须切 UWP 平台构建（详见 `为什么部署HL2要切UWP` 笔记）。
- **HL2 上 App 有两种形态**：① **2D 平板窗口(slate)**——只有 AirTap，**没有手关节追踪**；② **沉浸式全景(Holographic)**——有完整手势。**手势只在沉浸式下工作**，所以必须确保 App 是沉浸式（见 §8）。
- **Unity 2022.3 已移除旧 XR**，HL2 手势走 **OpenXR**（不是 v1 说的 Windows Mixed Reality）。
- **代码坑只在 UWP build 暴露**：Editor 能跑不等于设备能编（`#if !UNITY_EDITOR` 块），见 §4。

---

## 1. 前置条件

| 组件 | 版本（实测） |
|---|---|
| Unity | **2022.3 LTS**（实测 2022.3.62f3c1 中国版）|
| MRTK | **2.8.3**（foundation + standardassets）|
| ROS-TCP-Connector | Unity 包（git）|
| **OpenXR 插件** | `com.unity.xr.openxr` 1.14.x + **`com.microsoft.mixedreality.openxr`**（微软 MR OpenXR 插件，手势必需，见 §1.2）|
| Visual Studio | **2022**（带 UWP 组件，见 §1.3）|
| Windows SDK | **10.0.19041.0**（HL2 基线，**别用 10.0.26100**，见 §2.2 坑）|
| HoloLens 2 | 开发者模式 ON、Device Portal ON |

### 1.1 机器人侧（被 HL2 连接）
- Ubuntu + ROS Noetic，`~/mrrep_ws` 工作区，装 `ros_tcp_endpoint`。
- 一条 `start.launch mode:=nav` 起齐：nav(move_base/AMCL/地图) + `ros_tcp_endpoint:10000` + `hrp_follower_node`。
- **不用** v1 说的 `pure_pursuit.py`（那是仿真虚拟车）。

### 1.2 OpenXR 插件安装（手势关键，v1 缺）
MRTK 在设备上读手部数据，需要两个包：
- `com.unity.xr.openxr`（Unity OpenXR 加载器）—— Package Manager 装。
- **`com.microsoft.mixedreality.openxr`**（微软 MR OpenXR 插件）—— 用 **Mixed Reality Feature Tool (MRFT)** 装：
  1. 下载 MRFT（微软/GitHub `microsoft/MixedReality-FeatureTool`）。
  2. Browse 选工程 → Discover Features → 勾 **"Mixed Reality OpenXR Plugin"**（Platform Extensions 分组）。
  3. Import → 回 Unity 让它导入。
- 验证：`Packages/manifest.json` 里有 `com.microsoft.mixedreality.openxr`。

### 1.3 Visual Studio 组件（v1 的"UWP 工作负载"新版 VS 可能没有）
新版 VS2022 把 **"通用 Windows 平台开发"工作负载**弃用/下架了。改走**单个组件**（实测可行）：
- **MSVC v143 - VS 2022 C++ ARM64 生成工具**（HL2 是 ARM）
- **MSVC v143 - x64/x86 生成工具**
- **用于 v143 生成工具的 C++ 通用 Windows 平台工具**
- **Windows SDK (10.0.19041.0)** +（可选 Windows 11 SDK）
- **USB 设备连接性**（USB 部署）
> 若你的 VS **还有**"通用 Windows 平台开发"工作负载，直接勾它（含上述子件）更省事。VS 报"missing components/designtime 错"通常就是这些没装齐。

---

## 2. Unity 项目配置

### 2.1 切到 UWP
1. **File → Build Settings** → 选 **Universal Windows Platform**。
2. **Add Open Scenes** 把 `Assets/Scenes/MainScene` 加进 Scenes In Build（**不加 build 出来是空壳**）。
3. **Switch Platform**（首次几分钟重导入）。

### 2.2 UWP 构建设置（实测值，⚠️ 注意 SDK）
| 设置项 | 值 | 说明 |
|---|---|---|
| Target Device | HoloLens | |
| **Architecture** | **ARM64** | HL2 是 ARM；默认 Intel x64 部署后跑不了 |
| Build Type | **D3D Project** | 沉浸式必需（XAML 会变 2D） |
| **Target SDK Version** | **`10.0.19041.0`** | ⚠️**别选 "Latest installed"**（会取 10.0.26100 → VS 报 `MSB3774 找不到 WindowsMobile`，见 §6）|
| Minimum Platform Version | `10.0.10240.0` | |
| Visual Studio Version | Latest installed / VS2022 | |
| Build configuration | **Release** | Debug 在 HL2 上慢 |

### 2.3 Player Settings
Build Settings → **Player Settings...**：
- **Other Settings**：
  - **Scripting Backend = IL2CPP**（HL2/UWP 不支持 Mono；若灰选不了 → 缺 Windows IL2CPP 模块，§1.3）
  - **API Compatibility Level = .NET Standard 2.1**
  - Color Space = Linear（渲染）
  - 取消 **Graphics Jobs**（HL2 上可能崩）
- **Publishing Settings → Capabilities** 勾：
  - `InternetClient`、`InternetClientServer`、`PrivateNetworkClientServer`（连机器人 ROS-TCP）
  - `SpatialPerception`（手势/空间网格，**少勾手势静默失效**）
  - `Webcam`（以后做 QR 对齐再加）

### 2.4 XR：OpenXR（UWP 标签！）
> v1 写的 "Windows Mixed Reality" 已过时，Unity 2022.3 用 OpenXR。

**Edit → Project Settings → XR Plug-in Management**：
1. 切到 **Universal Windows Platform** 标签（⚠️**不是 PC/Standalone**——XR 按平台分，UWP 这栏才管 HL2 build）。
2. 勾 **OpenXR**。
3. 出现**黄三角** → 点 **Fix All**（自动修深度格式等默认设置）。
4. 进 **XR Plug-in Management → OpenXR** 子页：
   - **Interaction Profiles** → **Add** → 加 **Microsoft Hand Interaction Profile**（手势关键）。
   - **OpenXR Feature Groups** → 勾 **Hand Interaction Poses**（+ 可选 Palm Pose）。
5. **Render Mode = Single Pass Instanced**（HL2 推荐）。

> ⚠️ **OpenXR 在 UWP 标签没勾 = build 出 2D slate（无手势）**。这是沉浸式/手势的硬前提。

### 2.5 MRTK 在场景里
- Hierarchy 搜 `MixedReality`，确认有 **`MixedRealityToolkit`** + **`MixedRealityPlayspace`**。
- 没有 → 菜单 **Mixed Reality → Toolkit → Add to Scene and Configure**（加 MRTK + playspace + 配相机为 Holographic 相机）。
- 选中 `MixedRealityToolkit` → Inspector → Configuration Profile（`DefaultMixedRealityToolkitConfigurationProfile` 或 HoloLens 2 profile）→ Input 里确认 Hand Tracking 启用、Input Data Providers 里有 OpenXR/WindowsMixedReality 设备管理器并启用。

---

## 3. ROS 连接配置

### 3.1 Unity 端 ROS IP（⚠️ 改完必须存场景）
1. 选中 `ROSConnectionManager` 物体 → Inspector：
   - **ROS IP Address** = 机器人真实 IP（实测 `192.168.123.30`，**不是默认 `192.168.1.100`**）
   - **ROS Port** = `10000`
2. **Ctrl+S 存场景**！否则 build 里还是默认 IP → App 连不上机器人。

### 3.2 机器人端启动（一条命令）
```bash
source ~/mrrep_ws/devel/setup.bash
roslaunch mrrep_bridge start.launch mode:=nav map_name:=你的地图
```
这条起齐：nav(move_base/AMCL/地图) + `ros_tcp_endpoint:10000` + `hrp_follower_node`。
验证：
```bash
ss -tlnp | grep 10000          # 端点在监听
rostopic echo /hrp_path         # SEND 时这里应收到 nav_msgs/Path
```
> ⚠️ **端点必须和 nav 同一个 master**（在 start.launch 里，不要单独 `roslaunch ros_tcp_endpoint`，否则会 auto-start 另一个 master，`/hrp_path` 进不到 hrp_follower）。

---

## 4. 代码适配（UWP build 才暴露的坑，v1 缺）

> Editor 能跑不等于能 build 到设备——`#if !UNITY_EDITOR` 块在 Editor 不编译，UWP build 才编，MRTK/ROS API 不对就在这暴露。

### 4.1 HandTracker.cs —— MRTK 2.8.3 手关节 API
MRTK 2.8.3 正确写法（**实测**，源码验证过）：
- 补 `using Microsoft.MixedReality.Toolkit.Utilities;`（`TrackedHandJoint`/`Handedness`/`MixedRealityPose` 在这）。
- 用 **`HandJointUtils.TryGetJointPose(joint, handedness, out pose)`**：
  - ❌ 不是 `HandJointUtils.TryGetJoint`（这版没这方法 → CS0117）。
  - ❌ 不是 `IMixedRealityHandJointService.TryGetJoint`（这版接口只有 `RequestJointTransform`/`IsHandTracked`，没 TryGetJoint → CS1061）。
- 老代码 `GetDataProviders<...>().FirstOrDefault()` 也是错的，删掉。

### 4.2 PathSender.cs —— 消息类型 + 类名
- 发 **`nav_msgs/Path`**（不是 `PoseArray`），`frame_id="map"`，`poses` 是 `PoseStamped[]`。
- 这版 ROS-TCP-Connector 的类名是 **`PathMsg`**（`RosMessageTypes.Nav`）：
  - ❌ 不是 `MPath`（会 `NotImplementedException: MPath is now called PathMsg`）。
- 其他 `XxxMsg` 命名（`PoseStampedMsg`/`HeaderMsg`/`PoseMsg`/`PointMsg`/`QuaternionMsg`）都对的。
- 车相对（测试用）：`PathSender` 订阅 `/amcl_pose`，把画的形状旋转到车头方向 + 平移到车位姿（`carRelative` 开关），路径出现在车前。

---

## 5. 构建

### 5.1 Build 出 .sln
1. File → Build Settings → 确认 MainScene 在 Scenes In Build、Architecture=ARM64、Target SDK=10.0.19041。
2. 点 **Build**（⚠️**不要** Build And Run，那个会立刻尝试部署、易卡）。
3. 选**新建空文件夹**（如 `D:\Unity_Ros_v1.1\UWPBuild\`），别 build 进项目根。
4. ⏳ 首次 IL2CPP 编 ARM64，**10–30 分钟**。
5. 产物：`UWPBuild\Unity_Ros_v1.1.sln`（+ 子项目 Il2CppOutputProject）。

### 5.2 Build 常见错误（实测过的）
| 错误 | 原因 | 修 |
|---|---|---|
| `error MSB3774: 找不到 SDK "WindowsMobile, Version=10.0.26100.0"` | Target SDK 选了 "Latest installed"(10.0.26100)，其 WindowsMobile 扩展没注册 | Unity Target SDK 改 **10.0.19041.0** → 重新 Build |
| `CS0117: HandJointUtils 没有 TryGetJoint` | MRTK 2.8.3 方法名是 `TryGetJointPose` | 见 §4.1 |
| `CS1061: IMixedRealityHandJointService 没有 TryGetJoint` | 接口没这方法 / 参数少 handedness | 用 HandJointUtils.TryGetJointPose |
| `NotImplementedException: MPath is now called PathMsg` | 类名过时 | `MPath`→`PathMsg`（§4.2）|
| `CS0103: TrackedHandJoint 不存在` | 缺 using Utilities | 加 `using Microsoft.MixedReality.Toolkit.Utilities;` |
| Material `.mat.meta` GUID 警告（base64） | 工程原带坏 meta | 删坏 `.mat.meta`，Unity 重建（无害，不挡 build）|

---

## 6. Visual Studio 部署

### 6.1 打开 + 配置
1. 双击 `UWPBuild\Unity_Ros_v1.1.sln`（VS2022）。
2. 顶部：**Solution Configuration = Release**，**Solution Platform = ARM64**。

### 6.2 连 HL2 + 部署
**USB（首次推荐）**：
1. USB-C 连 HL2。HL2：**设置 → 更新和安全 → 开发者 → 设备 → 配对** → 显示 PIN。
2. VS 部署目标下拉 → 选 HL2（如 `HL2@xxx`）→ 弹 PIN 框 → 填 PIN → 身份验证 **通用(未加密)**。
3. 右键项目 → **Deploy**（或 生成 → 部署解决方案）。

**Wi-Fi**：部署目标选 **Remote Machine** → 填 HL2 IP（端口 2980）→ Universal。

### 6.3 VS 侧常见报错
| 现象 | 处理 |
|---|---|
| `Designtime 生成失败 / IntelliSense 不可用` | **designtime ≠ 真 build，不挡 Deploy**，忽略 |
| `Selected Visual Studio is missing required components` | 装 UWP 组件（§1.3）；不挡 Unity Build，但 VS 编 .sln 可能要用 |
| `MSB3774 WindowsMobile` | Target SDK 改 10.0.19041（§2.2）|
| `DEP0700 注册失败` | HL2 上卸载旧版 App 再部署 |

---

## 7. 运行验证

部署成功后，HL2 上：
1. 开始菜单启动 App（或 Deploy 后自动开）。
2. **App 应是沉浸式全景**（不是小窗口）。
3. 机器人端 `rostopic echo /hrp_path` 开着。
4. **掌心朝上** → 主菜单弹 → AirTap "Preferred Path" → AirTap "Add" → **捏合手画线** → AirTap "Send" → AirTap "Yes" → `/hrp_path` 收到数据 → **小车跟随**。

### 验证清单
- [ ] App 是**沉浸式全景**（不是 2D 小窗口）
- [ ] 右上角/状态显示**已连接 ROS**（IP 对了）
- [ ] 掌心朝上能**呼出主菜单**
- [ ] AirTap 能点按钮
- [ ] 捏合手势能**画线**
- [ ] SEND → Yes 后 `rostopic echo /hrp_path` 有数据、**小车动**
- [ ] 画"朝前"→ 路径从车头延伸（carRelative 生效）

---

## 8. ⚠️ 沉浸式 vs 2D slate（手势不工作的头号原因）

**症状**：App 显示成小窗口（2D 平板），掌心菜单不弹、捏合画不出线。
**根因**：HL2 的 2D slate 只给 **AirTap**，**不给手关节数据** → MRTK 手势完全无效。App 必须是**沉浸式**。

**确保沉浸式（排查顺序）**：
1. **XR Plug-in Management → UWP 标签 → OpenXR 勾上**（没勾 = 2D app）。⚠️ 是 UWP 标签，不是 PC/Standalone。
2. **Build Type = D3D Project**（XAML 会变 2D）。
3. **相机在 `MixedRealityPlayspace` 下**（MRTK 加的 playspace）；相机 Clear Flags=Solid Color、Background=黑(0,0,0,0) 透出真实环境。
4. MRTK 在场景里（`MixedRealityToolkit` + `MixedRealityPlayspace`）。
5. **改了 XR/相机后必须重新 Build + Deploy**（旧包不自动更新）。

---

## 9. 手势不工作（沉浸式下）排查

沉浸式了但手势还不灵 → 手部数据链路没通：
1. **`com.microsoft.mixedreality.openxr` 装了没**（§1.2）？没装 → MRTK 没 OpenXR 手数据源。
2. **OpenXR → Microsoft Hand Interaction Profile** 加了没（§2.4）？
3. **MRTK Input Data Providers** 里 OpenXR/WindowsMixedReality 设备管理器**启用**了没？
4. **HandTracker.cs** 用的是 `HandJointUtils.TryGetJointPose`（§4.1）？
5. HL2 手部追踪权限/环境光线充足。

---

## 10. 连不上 ROS 排查

- App 显示"未连接 ROS" → 多半 **ROS IP 没存对**（场景里要是 `192.168.123.30`，且 Ctrl+S 存了）→ 改 IP + 存场景 + 重新 Build。
- 机器人 `ss -tlnp | grep 10000` 要有监听。
- HL2 和机器人**同一局域网**。
- 机器人防火墙放行 10000。
- 端点和 nav **同一个 master**（用 start.launch，别单独起端点）。

---

## 11. 迭代方式（重要）

**原生部署每次改都要 rebuild+redeploy（慢）**。开发期改 UI/调功能用 **Holographic Remoting**（Editor Play 串流到 HL2、改完即看、真手势回传），原生只在"验链路"和"交付"各做一次。详见 `HL2开发迭代方式-原生部署与全息远程-20260718.md`。

---

## 附：实测配置快照（本机）
- Unity 2022.3.62f3c1 / MRTK 2.8.3 / ROS-TCP-Connector(git) / OpenXR 1.14.3 / MR OpenXR 1.11.2
- Target SDK 10.0.19041 / Min 10.0.10240 / ARM64 / D3D / IL2CPP / .NET Standard 2.1 / Release
- Capabilities: InternetClient, InternetClientServer, PrivateNetworkClientServer, SpatialPerception
- 机器人 IP 192.168.123.30 :10000，`start.launch mode:=nav`
- 代码：HandTracker(`HandJointUtils.TryGetJointPose`) / PathSender(`PathMsg` + nav_msgs/Path + carRelative)
