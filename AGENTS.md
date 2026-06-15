# AGENTS.md

本文件为 AI 辅助开发代理提供项目上下文与开发规范。

## 项目简介

MRReP 是一个 HoloLens 2 + ROS Noetic 混合现实项目，允许用户在 MR 环境中手画路径，发送给 ROS 端 Pure Pursuit 控制器驱动虚拟小车沿路径运动。

## 技术栈约束

- **Unity 版本**：2022.3 LTS
- **MRTK 版本**：2.x（非 MRTK3），API 命名空间为 `Microsoft.MixedReality.Toolkit.*`
- **ROS 版本**：ROS 1 Noetic（非 ROS 2）
- **Python 版本**：Python 3（ROS Noetic 默认）
- **通信**：仅使用 ROS-TCP-Connector/Endpoint，不使用 rosbridge 或其他方案

## C# 编码规范

- 命名空间与文件夹对应：`MRReP.Path`、`MRReP.UI`、`MRReP.ROS`、`MRReP.Robot`、`MRReP.Anchor`
- 使用 `[SerializeField] private` 暴露 Inspector 字段，避免 public 字段
- 回调使用 `System.Action` 委托，不用 UnityEvent（除非需要 Inspector 绑定）
- 使用 `#if WINDOWS_UWP` 条件编译隔离 UWP 专属 API

## Python 编码规范

- 所有 ROS 节点使用 `if __name__ == "__main__"` 入口
- 使用 `rospy.loginfo` 记录关键状态
- Pure Pursuit 参数通过 `rospy.get_param` 支持参数服务器配置

## Unity 场景层级约定

```
MainScene (Scene)
├── MRTK (MixedRealityToolkit)
├── Managers
│   ├── ROSConnectionManager
│   ├── SpatialAnchorManager
│   └── PathData
├── UI
│   ├── MainMenu (HandConstraintPalmUp)
│   ├── PreferredPathMenu
│   └── ConfirmDialog
├── PathVisuals
│   └── PathRenderer (LineRenderer)
├── VirtualCar (Prefab)
└── Directional Light
```

## 关键技术点

### 坐标系转换
Unity 左手系 → ROS 右手系：`ROS = (Unity.z, -Unity.x, Unity.y)`
所有发往 ROS 的坐标必须减去 SpatialAnchor 原点偏移。

### 空间锚点
HoloLens 每次启动原点随机，必须在地面建立 SpatialAnchor 作为零点。
路径采样点和小车位姿均相对于锚点存储。

### MRTK 手部追踪
使用 `IMixedRealityHandJointService` 同时获取 `TrackedHandJoint.ThumbTip` 和 `TrackedHandJoint.IndexTip`。
当两指尖距离 < 2cm 时判定为捏合（Pinch），取中点作为采样点。
采样间隔：固定 50ms 时间间隔。

## 不要做的事情

- 不要引入 ROS 2 的 `rclpy`/`rclcpp` 依赖
- 不要使用 Unity 的 `XR.Interaction.Toolkit`（使用 MRTK 输入系统）
- 不要创建额外的 ROS 消息类型，优先使用 `geometry_msgs/Point`
- 不要在脚本中硬编码 IP 地址，使用 Inspector 序列化字段
