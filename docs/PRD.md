
# 产品项目计划书：MRReP (Mixed Reality-based Hand-drawn Reference Path) 虚拟仿真版

> **📌 进展说明（2026-07-17 更新）**：本项目已从"全虚拟仿真"推进到**接入真车（RoboMaster EP，ROS1）**阶段。HL2 端菜单/画线/接近段等交互已重新规范——详见 `MRReP_菜单与画线交互规范.md`（行为）与 `MRReP_UI_Design_Guidelines.md`（视觉）；下文 §3 已同步为最新的**两级菜单**结构。原仿真架构（Pure Pursuit / `/odom` 回传）作为离线调试手段保留，真车部署改用 `mrrep_bridge/hrp_follower_node` + move_base。

## 1. 项目概述 & 目标

本项目旨在复刻 MRReP 系统，实现一个基于混合现实（MR）的移动机器人手画参考路径（HRP, Hand-drawn Reference Path）编辑界面。
由于现阶段不上真车（Robotmaster EP），本项目采用**全虚拟仿真模式**：在 Unity (HoloLens 2) 中进行手势交互、路径绘制与虚拟小车渲染，在 Ubuntu (ROS Noetic) 中运行核心路径规划与控制算法。

### 核心硬件与开发环境

* **MR 端设备**：HoloLens 2
* **MR 开发工具**：Unity Hub (推荐 2020.3/2021.3 LTS) + MRTK 2.x (Mixed Reality Toolkit)
* **上位机算法端**：Ubuntu 20.04 LTS + ROS Noetic
* **下位机仿真**：Unity 场景内集成的虚拟小车模型（基于 Robotmaster EP 结构）
* **通信桥梁**：ROS-TCP-Connector (Unity) & ROS-TCP-Endpoint (ROS)

---

## 2. 系统架构设计 (Architecture)

```
[ HoloLens 2 (Unity + MRTK) ] 
       │
       │ (1) 发布 /hrp_path (自定义消息: Vector3数组)
       ▼
[ ROS-TCP-Connector / Endpoint 跨平台通信 ]
       │
       │ (2) 转发给算法节点
       ▼
[ Ubuntu 20.04 (ROS Noetic) ]
   ├── Path Converter (坐标系转换: Unity左手系 ⇄ ROS右手系)
   ├── HRP Planner (全局路径接收与管理)
   └── Pure Pursuit Controller (纯追踪算法: 实时积分计算虚拟小车位姿)
       │
       │ (3) 发布 /odom 或 /robot_pose (nav_msgs/Odometry)
       ▼
[ HoloLens 2 (Unity + MRTK) ] 
       │
       └─► 实时更新虚拟小车模型的 Transform (Position & Rotation)

```

---

## 3. 功能需求与交互逻辑 (Functions & UI)

### 3.1 掌心菜单与导航交互 (Main Menu)

* **触发机制**：使用 MRTK 的 `Hand Constraint Palm Up` 组件。当用户伸出手掌、**掌心朝上**时，触发显示 `Main Menu` 悬浮板（**条状**吸附手腕外侧）；手掌放下或翻转时隐藏。
* **主菜单结构（两级·用户确认）**：`Main Menu` 为竖条状，自上而下：顶部状态栏（`Stage 0` + 当前模式）→ 按钮 **`Preferred Path`** → **中间留白**（为未来功能预留：Direct Nav / Waypoints / Relocate 等）→ 按钮 **`Settings`**（暂为占位）。
* **二级跳转**：点击 `Preferred Path` 进入 `Preferred Path Menu`（路径编辑核心界面）。状态栏初始为 `Stage 0: OFF MODE`。
* > 📌 视觉与交互细节见 `MRReP_UI_Design_Guidelines.md` 与 `MRReP_菜单与画线交互规范.md`（本节为摘要）。

### 3.2 路径编辑核心功能 (Preferred Path Menu)

界面为条状，四个核心按钮（顺序：**Back / Clear / Send / Add**），同款白胶囊（图标左+文字右），当前激活模式青色强调：

```
+--------------------+
|     Stage 0        |
|    [当前模式]       |
+--------------------+
|  ←  Back           |
|  ✕  Clear          |
|  ▤  Send           |
|  +  Add   (激活)    |
+--------------------+
```

#### ① ADD 模式 (添加 HRP)

* **状态切换**：点击后，状态栏切换为 `Stage 0: ADD MODE`。
* **路径绘制**：
* 系统实时追踪用户右手食指尖（`TrackedHandJoint.IndexTip`）的空间坐标。
* 当食指尖在空间中移动时，采用“距离阈值法”（如每移动 5cm）或固定时间间隔采样空间点，存入 `List<Vector3> currentPath`。
* 在每个采样点位置动态生成一个虚拟蓝色球体作为视觉标记。
* 利用 Unity 的 `LineRenderer` 组件实时将这些点连成一条平滑的蓝色半透明轨迹。
* **接近段预览（起点不在车上）🆕**：若手绘路径**起点不在小车当前位置**，实时画一条**虚线**连接 小车 → 起点，并做直线障碍检测（沿段在 costmap/地图查占据）。该虚线在 ADD 时仅作**预览**，实际靠拢在 SEND 确认后执行（见 ③）。直达=白/青虚线，不直达=红/橙+警告。



#### ② CLEAR 模式 (删除/清空)

* **交互逻辑**：点击后弹出二次确认 Modal 框：*"Are you sure you want to clear all path points?"*。
* **执行结果**：若选择 `Yes`，清空 `List<Vector3>`，销毁（`Destroy`）场景中所有生成的蓝色路径球，重置 `LineRenderer`，状态栏恢复 `OFF MODE`。

#### ③ SEND 模式 (发送并启动)

* **交互逻辑**：路径绘制完成后，点击 **SEND** 按钮，弹出二次确认框：*"Are you sure you want to SEND PATH to the robot?"*。
* **接近段执行（方案 A · 已定）🆕**：确认 Yes 后，若起点不在车上且虚线直达，**小车先自动靠拢到起点**（move_base 目标=起点位姿，朝向路径第二点），到达后再沿手绘路径跟随；不直达则提示用户、不自动靠拢。**车只在用户点 Yes 后才动。**
* **数据转换**：确认后，脚本将 `List<Vector3>` 中的 Unity 坐标点，转换为 ROS 相对坐标系下的点（处理 $Y$ 轴与 $Z$ 轴的对调，并对齐空间锚点 = map 原点）。
* **通信发送**：将转换后的坐标打包为 **`nav_msgs/Path`（`frame_id=map`）**，通过 ROS-TCP-Connector 发布到 `/hrp_path`；机器人侧 `hrp_follower_node` 逐点喂 move_base（真车）或本地 Pure Pursuit（仿真）。状态栏切换为 `Stage 0 / SEND PATH`。

---

## 4. 关键技术攻关与规约 (Technical Constraints)

### 4.1 空间对齐与统一原点（关键防坑）

由于 HoloLens 每次开机或识别的空间原点 $(0,0,0)$ 具有随机性，直接发送绝对坐标会导致机器人漂移。

* **规范**：系统启动时，需在地面确立一个虚拟空间锚点（Spatial Anchor）作为零点。
* **数据处理**：

$$\text{发送给 ROS 的点} = \text{HoloLens 食指尖采样点} - \text{空间锚点}$$


$$\text{Unity 渲染小车位置} = \text{ROS 返回的小车位姿} + \text{空间锚点}$$



### 4.2 坐标系转换规约

* **Unity 坐标系**：左手坐标系（$X$ 向右，$Y$ 向上，$Z$ 向前）。
* **ROS 坐标系**：右手坐标系（$X$ 向前，$Y$ 向左，$Z$ 向上）。
* 利用 `ROS-TCP-Connector` 提供的内置转换函数或手动进行轴向映射（通常为：$\text{ROS}_x = \text{Unity}_z$，$\text{ROS}_y = -\text{Unity}_x$，$\text{ROS}_z = \text{Unity}_y$）。

### 4.3 ROS 算法端仿真逻辑

* **接收**：编写 ROS 节点订阅 `/hrp_path`。
* **控制**：采用 **Pure Pursuit（纯追踪）** 算法，将路径点作为 Global Path。设定虚拟小车的初始位姿，根据 Look-ahead Distance（前视距离）计算小车所需的 $v$（线速度）和 $\omega$（角速度）。
* **状态更新**：通过差速运动学模型实时解算小车下一帧的 $x, y, \theta$，并以固定频率（如 20Hz）向 Unity 发布 `/odom` 话题。

---

## 5. 项目分阶段开发计划 (Roadmap)

* **Phase 1 (环境搭建)**：配置 Unity 2020/2021 MRTK 运行环境，在 Ubuntu 上部署 `ros_tcp_endpoint`，测试基础的字符串或单个 Vector3 通信。
* **Phase 2 (MR UI 开发)**：实现手掌心呼出菜单，编写食指追踪打点、`LineRenderer` 画线、以及 **ADD/CLEAR** 的 C# 逻辑。
* **Phase 3 (数据对接)**：实现坐标系转换，完成 **SEND** 按钮将三维坐标数组批量发送至 ROS 的功能。
* **Phase 4 (算法与闭环仿真)**：在 ROS 端编写 Pure Pursuit 仿真节点，将计算出的小车位置回传给 Unity，在 Unity 中看到虚拟小车完美贴合手画路径移动。