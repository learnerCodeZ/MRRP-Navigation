# MRReP - Mixed Reality Hand-drawn Reference Path

基于混合现实的移动机器人手画参考路径虚拟仿真系统。

## 系统架构

```
HoloLens 2 (Unity + MRTK)  ── /hrp_path ──▶  ROS Noetic (Ubuntu)
        ▲                                           │
        └──────────── /odom ◀─────────────────────┘
```

- **MR 端**：Unity 2022.3 LTS + MRTK 2.x，运行于 HoloLens 2
- **算法端**：Ubuntu 20.04 + ROS Noetic
- **通信**：ROS-TCP-Connector (Unity) ↔ ROS-TCP-Endpoint (ROS)

## 功能

| 功能 | 说明 |
|------|------|
| 掌心菜单 | HandConstraintPalmUp 触发主菜单 |
| ADD 模式 | 食指与大拇指捏合（Pinch）采样，蓝色球体 + LineRenderer 画线 |
| CLEAR 模式 | 二次确认后清空路径 |
| SEND 模式 | 坐标系转换后发送路径至 ROS |
| Pure Pursuit | ROS 端纯追踪算法，20Hz 回传位姿 |
| 虚拟小车 | Unity 实时渲染小车沿路径运动 |

### 手势交互

| 手势 | 功能 |
|------|------|
| 手掌朝上 (Open Palm) | 弹出主菜单 |
| 食指点击 (Air Tap) | 点击 UI 按钮 |
| 拇指+食指捏住不放 | 画线模式（移动手部采样路径点） |

## 目录结构

```
Unity_Ros_v1.1/
├── Assets/
│   ├── Materials/          # 材质资源
│   ├── Prefabs/            # 预制体（VirtualCar）
│   ├── Resources/          # 运行时加载资源
│   ├── Scenes/             # Unity 场景
│   ├── Editor/             # Editor 工具脚本
│   │   └── UIResizer.cs    # HoloLens 2 按钮批量缩放工具
│   └── Scripts/
│       ├── Anchor/         # SpatialAnchor 空间锚点
│       ├── Path/           # 路径数据、追踪、渲染
│       ├── Robot/          # 虚拟小车控制
│       ├── ROS/            # ROS 通信、坐标转换
│       └── UI/             # 菜单、对话框、Play Mode 测试
├── docs/
│   ├── PRD.md              # 产品需求文档
│   └── Unity_Scene_Setup.md # Unity 场景配置文档
├── Packages/               # Unity 包管理
├── ProjectSettings/        # Unity 项目设置
└── ros_ws/
    └── src/mrrep/
        ├── msg/            # 自定义 ROS 消息
        ├── scripts/        # Python 节点
        ├── CMakeLists.txt
        └── package.xml
```

## 环境要求

| 组件 | 版本 |
|------|------|
| Unity | 2022.3 LTS |
| MRTK | 2.8+ |
| ROS | Noetic (ROS 1) |
| Ubuntu | 20.04 LTS |
| ROS-TCP-Connector | latest |
| ROS-TCP-Endpoint | latest |

## 快速开始

### Unity 端
1. Unity Hub 打开本项目
2. 导入 MRTK 2.x（Mixed Reality Feature Tool）
3. 导入 ROS-TCP-Connector（Package Manager → GitHub）
4. 打开 `Assets/Scenes/MainScene`
5. 配置 `ROSConnectionManager` 的 IP 地址

### ROS 端
```bash
cd ros_ws
catkin_make
source devel/setup.bash
# 启动 TCP Endpoint
rosrun mrrep tcp_endpoint.py
# 启动 Pure Pursuit 控制器
rosrun mrrep pure_pursuit.py
```

## 坐标系转换

| | Unity (左手系) | ROS (右手系) |
|---|---|---|
| 前 | +Z | +X |
| 右 | +X | -Y |
| 上 | +Y | +Z |

转换公式：`ROS_x = Unity_z, ROS_y = -Unity_x, ROS_z = Unity_y`

## 开发路线

- [x] Phase 1：环境搭建与脚本框架
- [x] Phase 2：MR UI 开发（菜单、画线、清空）
- [ ] Phase 3：数据对接（SEND 功能）
- [ ] Phase 4：Pure Pursuit 闭环仿真

## Play Mode 测试

在 Unity Editor 中测试时：

1. 将 `PlayModeUIFixer` 脚本添加到场景中的空 GameObject 上
2. Play 模式下自动修复 UI 组件（GraphicRaycaster、Image、字体）
3. **M 键**：切换主菜单显示
4. **鼠标左键**：模拟捏合画线
5. **鼠标点击按钮**：点击 UI 按钮

> **注意**：Play Mode 下 MRTK 的 MixedRealityInputModule 可能报错，这是正常现象（无 HoloLens 硬件）。`PlayModeUIFixer` 的 `Update()` 方法直接通过 `EventSystem.RaycastAll` 处理鼠标点击。

## Citation

If you use this work, please cite:

> **MRReP: Mixed Reality-based Hand-drawn Reference Path Editing Interface 
> for Mobile Robot Navigation**
> 
> Takumi Taki\*, Masato Kobayashi\*+, Yuki Uranishi
> 
> \* Equal Contribution, + Corresponding author
> 
> The University of Osaka, Kobe University
