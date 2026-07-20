# MRReP 菜单与画线交互逻辑规范（设计稿 v2）

> **版本**：v2，2026-07-17。规范 HoloLens2 端**菜单触发、菜单层级、模式状态机、手势映射、画线规则、接近段（车→起点自动靠拢）**——即"行为逻辑"。视觉样式见 `MRReP_UI_Design_Guidelines.md`。
> **性质**：设计提案，✅=现状（代码已有）/ 💡=建议（提议改动）/ 🆕=v2 新增。你过目后我再按反馈调整。
> **相关代码**：`MainMenuController.cs`、`PreferredPathMenuController.cs`、`HandTracker.cs`、`PathData.cs`、`PathRenderer.cs`、`ConfirmDialog.cs`、`PathSender.cs`、`SpatialAnchorManager.cs`。

---

## 变更记录

- **v2（本次）**：
  - 🆕 **菜单结构确认为两级**（MainMenu + PreferredPathMenu），按用户确认；撤回 v1"合并单层"建议。
  - 🆕 **MainMenu = Preferred Path + Settings + 中间留白**（为未来功能预留）；Settings 为占位。
  - 🆕 **PreferredPathMenu = Back / Clear / Send / Add** 四按钮。
  - 🆕 **接近段（Approach）**：起点不在车上 → 虚线连接车与起点 → 无障碍则小车自动靠拢到起点，再沿手绘路径跟随。
  - 状态机加 `APPROACH` 子态；SEND 流程并入接近段。
- **v1**：初版三态状态机、手势映射、画线规则、确认弹窗、改动清单（内容保留于下）。

---

## 0. 设计目标

1. **单一入口**：伸出手掌（掌心朝上）触发菜单，放下即收——零记忆负担。
2. **小巧贴手**：菜单做成**条状**吸附手腕，不挡视野（用户确认："条状的在手边也简洁"）。
3. **状态自明**：顶部状态横幅随时告诉你"现在在哪个模式"。
4. **可预测 + 安全**：画线只追加、不清空；清空/发送都二次确认；**车只在用户确认后才动**。
5. **HL2 与 Editor 行为一致**：手势 vs 鼠标同构，方便 Play Mode 调试。

---

## 1. 现状速览（当前代码）

| 项 | 现状 | 文件 |
|---|---|---|
| 菜单触发 | README 称 `HandConstraintPalmUp` 掌心触发主菜单 | 场景配置 |
| 菜单层级 | 两层：主菜单 → Preferred Path → 操作菜单 | `MainMenuController`/`PreferredPathMenuController` |
| 状态机 | `MenuState{Off, Add, Send}`，A/C/S/B 快捷键 | `PreferredPathMenuController` |
| 画线 | 捏合采样 + `waitingForRelease` + 0.05m 降采样；点存相对锚点 | `HandTracker`/`PathData` |
| 发送 | `localPathFollower≠null`→虚拟车跟随；否则 `pathSender.SendPath` | `PreferredPathMenuController` |
| 确认弹窗 | `ConfirmDialog.Show(msg, cb)` | `ConfirmDialog` |

> v1 发现的小问题（保留）：CLEAR 确认文案两处不一致；`Send` 是终态、无显式退出。v2 一并规范。

---

## 2. 菜单触发与层级 🆕确认

### 2.1 掌心触发（核心入口）
- **触发**：手掌张开、**掌心朝上**达阈值 → 主菜单淡入，吸附手腕外侧（`HandConstraintPalmUp`，安全区 `UlnarSide`，不挡手）。
- **收起**：手放下 / 翻掌 / 手离开 → 菜单淡出。
- 接线：`OnFirstHandPalmUp → MainMenuController.ShowMainMenu()`；`OnLastHandLost → HideMainMenu()`。

### 2.2 菜单层级（两级，按用户确认）

**① MainMenu（掌心条状主菜单）** —— 小巧竖条，贴手腕。
- 顶部：状态横幅 `Stage 0` + 当前模式大字。
- 按钮（自上而下）：
  1. **`Preferred Path`** —— 进入路径操作菜单。
  2. 🆕 **（中间留白）** —— 为未来功能预留（如 Direct Nav / Waypoints / Relocate 等），现在空着。
  3. 🆕 **`Settings`** —— 设置入口（**暂为占位**，内容待定）。
- 💡 设计点：条状 + 留白，视觉简洁、好扩展。

**② PreferredPathMenu（路径操作菜单）** —— 进入后显示。
- 4 按钮（顺序按用户所述）：**`Back` / `Clear` / `Send` / `Add`**。
  - `Back` → 回 MainMenu（**保留路径**）。
  - `Clear` → 删除路径（二次确认）。
  - `Send` → 发送给 robot（二次确认）。
  - `Add` → 进入画线。
  - 💡 注：`Add` 是主操作，可考虑置顶或高亮；现按你给的顺序，可调。

> v1 曾建议"合并为单层"——**作废**，按用户确认保留两级。

### 2.3 菜单与画线互不阻挡
- 进入 ADD 后菜单**吸附手腕**（不消失、不挡画线手）；任意手捏合拖动画线。
- Editor：`M` 键切菜单，鼠标点按钮。

---

## 3. 状态机（核心）🆕加 APPROACH

**主态**：`OFF`（空闲）、`ADD`（画线中）、`FOLLOWING`（沿手绘路径跟随中）。
**子态**：`APPROACH`（靠拢中——Send 后、跟随前的过渡）。
**动作**：`CLEAR`、`SEND`、`BACK`。

```
        ┌──────────────────────────────────────────────────────────┐
        │                  [掌心朝上] → 弹出 MainMenu                │
        └──────────────────────────────────────────────────────────┘
                                     │
                  ┌──── ADD ─────────┼──────── ADD(追加) ──────────┐
                  ▼                  │                              │
               ┌─────┐  BACK         │   SEND(confirm,路径非空)      │
         ┌────▶│ OFF │◀────(保留路径) │                              ▼
         │     └─────┘               │           ┌──────────┐  到达起点 ┌──────────┐
         │       │                   │      ┌───▶│ APPROACH │──────────▶│FOLLOWING │
         │       │ CLEAR(confirm)    │      │    │ 车开往起点│            │沿手绘路径 │
         │       │ 停跟随+清空         │      │    └──────────┘            └────┬─────┘
         │       ▼                   │      │ （起点≠车 且直达）              │
         │     ┌─────┐               │      │                                │ CLEAR(confirm)
         │     │ ADD │────SEND────────┼──────┘                                │ 停+清空
         │     │画线 │(起点=车 或 不直达,跳过 APPROACH)                        │
         │     └──┬──┘                                                       │
         │        │ CLEAR(confirm) 停画+清空                                  │
         └────────┴───────────────────────────────────────────────────────────┘
```

**状态定义**：

| 状态 | 含义 | 可执行动作 |
|---|---|---|
| `OFF` | 空闲；路径可能已存在 | ADD、CLEAR(若有)、SEND(若非空) |
| `ADD` | 画线中 | 继续 ADD、CLEAR、SEND、BACK |
| 🆕 `APPROACH` | Send 确认后，车正自动开往**手绘路径起点** | （过渡态，到达即转 FOLLOWING；CLEAR 可中止） |
| `FOLLOWING` | 车沿手绘路径跟随中 | CLEAR(停+清)、ADD(💡清旧画新)、BACK |

**关键规则**：
- **路径跨模式持久**：ADD 追加、不清空；只有 `CLEAR` 重置。
- **CLEAR / SEND 都二次确认**。
- **SEND 路径为空时禁用**（`Count==0` no-op）。
- 🆕 **SEND = 靠拢 + 跟随**：确认后，若起点≠车且直达 → 先 `APPROACH` 到起点，再 `FOLLOWING`；否则直接 `FOLLOWING`（见 §6 接近段）。
- 💡 `FOLLOWING --CLEAR--> OFF` 明确退出（v1 补的建议，保留）。

---

## 4. 手势 → 动作映射

| 手势（HL2） | Editor 等价 | 作用 | 适用 |
|---|---|---|---|
| 掌心朝上（Open Palm） | `M` 键 | 弹出/切换 MainMenu | 全局 |
| 食指 Air Tap | 鼠标左键点按钮 | 点击 UI 按钮 | 菜单内 |
| 拇指+食指**捏住不放**并移动 | 鼠标左键**按住拖动** | ADD 模式画线采样 | 仅 ADD |
| 松开捏合 | 松开鼠标左键 | 暂停当前笔画（不退出 ADD） | ADD |
| — | `A`/`C`/`S`/`B` | ADD/CLEAR/SEND/BACK 快捷键 | Editor |

> Air Tap（点按钮）与 Pinch（画线）用手势区分；画线只在 ADD 生效，不冲突。

---

## 5. 画线（ADD）逻辑规范

### 5.1 进入 ADD
- 点 `Add` → `state=ADD`，`HandTracker.StartTracking()`。
- ✅ **`waitingForRelease`**：进入后必须**先松开**任何捏合才开始采样新笔画（防"点 Add 的捏合"被当起点）。保留。

### 5.2 采样规则
- **触发**：持续捏合（HL2 拇指-食指 < `pinchThreshold`=0.02m；Editor 鼠标左键按住）。
- **节拍**：每 `trackingInterval`=0.05s（≈20Hz）。
- **降采样**：新点距上一点 ≥ `sampleDistanceThreshold` 才入列（✅ 0.05m）。💡 设备可放宽到 0.05–0.10m。
- **采样位置**：HL2=拇指/食指**中点**；Editor=鼠标射线打 `Y=0.5` 平面交点。

### 5.3 多笔画 / 连续
- 松开 → 当前笔画暂停；再捏合（先松后捏）**追加到同一条路径**（✅）。`CLEAR` 才整体清空。

### 5.4 存储 / 坐标
- ✅ 点存**相对锚点**（`GetRelativePoints` = `point - anchor.position`），锚点来自 `SpatialAnchorManager`（对应 ROS `map` 原点）。
- 发送经 `CoordinateConverter`(Unity→ROS) 转 map 帧，`frame_id="map"`。

### 5.5 渲染
- ✅ `PathRenderer`：LineRenderer + 青蓝采样球(`#4DEEEA`)，点数变化增量更新。
- 💡 终点加**黄色菱形目标标记**(`#FFD700`)。

### 5.6 🆕 画线时的"接近段预览"
- ADD 画线期间，若**路径首点不在小车当前位置**，实时画一条**虚线**连接 小车 → 首点（视觉见设计规范）。
- 同时做**障碍检测**（见 §6.2），据此决定 Send 时是否自动靠拢。
- 这条虚线是**预览/提示**，本身不驱车（驱车时机见 §6.3）。

---

## 6. 🆕 接近段（Approach）：车 → 起点的虚线连接与自动靠拢

> **场景**：用户往往站在远处画路径，**起点不在车上**。需要让车先"靠拢"到起点，再沿手绘路径走。这是 v2 新增的核心交互。

### 6.1 触发条件
- 路径首点 P0 与小车当前位姿 C 的水平距离 > 阈值（如 0.3m）→ 视为"起点不在车上"，启用接近段。
- P0 ≈ C → 无需接近，Send 直接进入跟随。

### 6.2 障碍检测（直线可达性）
- 取直线段 C→P0（map 帧），按固定步长（如 0.1m）离散采样。
- 逐点查 **global costmap / `/map` OccupancyGrid** 的 cell 值：≥ 占据阈值（如 50）= 被阻挡。
- 全段空闲 → **直达**；任一点占据 → **不直达**。

### 6.3 自动靠拢（执行时机 · ✅ 已定方案 A）
- **采用方案 A（安全）**：虚线在 ADD 时仅**预览**；实际靠拢在 **Send 确认后**执行。
  - `SEND 确认 Yes → (起点≠车 且 直达) → APPROACH（车开往起点）→ 到达 → FOLLOWING（沿手绘路径）`。
  - **车只在用户点 Yes 后才动**，符合"可预测+安全"。
- ~~方案 B~~（未采纳）：画完即自动靠拢——风险是画线途中车就开始动、可能意外；已弃用。

### 6.4 被阻挡时
- 不直达 → **不自动靠拢**，提示用户："起点不可直达，请重画或手动引导小车到起点附近"。
- 仍可强制 Send：车由 move_base 自行绕路到起点（放弃直线靠拢的可预测性）——💡 可作为高级选项。

### 6.5 起点位姿朝向
- 靠拢到 P0 时，车头朝向**路径第二个点 P1**（让车到起点后即对准要走的方向），避免到起点后再原地转。

### 6.6 实现要点（仅设计，不改代码）
- **虚线渲染**：独立 LineRenderer + 虚线材质（dash shader 或分段绘制），颜色/粗细见设计规范，区别于实线手绘路径。
- **障碍检测**：直线段离散点查 costmap_2d（`getCost`）或 OccupancyGrid cell；可在机器人侧加一个"直线可达性服务"，或前端拿 `/map` 自查。
- **靠拢导航**：把 P0 作为 move_base 的**第 0 个航点**前置进 `hrp_follower` 的航点序列（hrp_follower 本就是逐点喂 move_base，天然支持）；或先单独发一个 goal，等 `GOAL_REACHED` 再发手绘路径。
- **朝向**：P0 的 quaternion 由 P0→P1 方向算。

---

## 7. CLEAR / SEND / BACK 动作规范

### 7.1 CLEAR（清空路径）
- 触发 → `StopTracking` → **确认弹窗** → 确认：`StopFollowing`(若跟随中) + 清 `APPROACH` + `ClearRenderers` + `PathData.Clear` → `state=OFF`。
- 💡 统一确认文案：`"Clear all path points?"`（修现状两处不一致）。
- 空路径时 CLEAR 禁用/no-op。

### 7.2 SEND（发送路径） 🆕含接近段
- 前置：`pathData.Count >= 2`（<2 提示）；ROS 已连接（未连提示）。
- 触发 → `StopTracking` → **确认弹窗** `"Are you sure you want to SEND PATH to the robot?"` → Yes：
  - 部署态（`localPathFollower==null`）：`pathSender.SendPath` 发 `/hrp_path`（💡 改 `nav_msgs/Path`）。
  - 仿真态（`localPathFollower!=null`）：`localPathFollower.StartFollowing()`。
  - 🆕 接近段：若起点≠车且直达 → 先 `APPROACH` 到起点，再 `FOLLOWING`（方案 A）；否则直接 `FOLLOWING`。
  - → 状态横幅 `Stage 0 / SEND PATH`。

### 7.3 BACK
- `StopTracking` → 回 MainMenu，**保留路径**（✅）。

---

## 8. 状态显示规范（顶部横幅）

| 状态 | 横幅（上 / 下） |
|---|---|
| `OFF` | `Stage 0` / `OFF MODE` |
| `ADD` | `Stage 0` / `ADD MODE` |
| 🆕 `APPROACH` | `Stage 0` / `APPROACHING` |
| `FOLLOWING` | `Stage 0` / `SEND PATH` |

- 💡 统一两行结构（现状 Send 态缺 `Stage 0` 前缀）。
- 左上常驻 `MRReP` 品牌。

---

## 9. 边界情况

| 情况 | 处理 |
|---|---|
| 路径为空 / <2 点 SEND | 禁用 + 提示"至少画 2 个点" |
| ADD 中再点 ADD | 维持 ADD（幂等） |
| FOLLOWING 中点 ADD | 💡 先停跟随 + 清旧路径再画新 |
| 🆕 起点在车上 | 跳过 APPROACH，直接跟随 |
| 🆕 起点不直达 | 不自动靠拢 + 提示（见 §6.4） |
| 🆕 APPROACH 中障碍物突变 | move_base 实时避障；若彻底堵死，报失败回 OFF |
| 画线时菜单被掌心触发 | 不影响；菜单吸附手腕 |
| 断连（ROS 未连） | SEND 提示"未连接 ROS"，不发 |
| 锚点未设置 | `GetRelativePoints` 退化为绝对坐标（坐标会偏，需 Phase 10 对齐） |

---

## 10. 建议改动清单（相对现状）

| # | 改动 | 文件 | 优先级 |
|---|---|---|---|
| 1 | 🆕 MainMenu 重构为：Preferred Path + 中间留白 + Settings | 场景/Prefab + `MainMenuController` | 中 |
| 2 | 🆕 PreferredPathMenu 四按钮 Back/Clear/Send/Add（按此顺序） | 场景/Prefab + `PreferredPathMenuController` | 中 |
| 3 | 🆕 **接近段**：虚线预览 + 障碍检测 + 靠拢导航 | 新增脚本 + `hrp_follower`/costmap | **高（核心新交互）** |
| 4 | `MenuState.Send` → 明确 `FOLLOWING` + `APPROACH` 子态 | `PreferredPathMenuController` | 中 |
| 5 | 统一 CLEAR 确认文案 `"Clear all path points?"` | `PreferredPathMenuController`+`PlayModeUIFixer` | 低 |
| 6 | 状态横幅统一两行（含 APPROACHING/FOLLOWING） | `PreferredPathMenuController.UpdateStatusText` | 低 |
| 7 | SEND 前校验：点数≥2、ROS 已连 | `PreferredPathMenuController.OnSendClicked` | 中 |
| 8 | `PathSender` 发 `nav_msgs/Path`（PoseArray→Path, `frame_id=map`） | `PathSender.cs` | **高（接真车硬前提）** |
| 9 | 终点黄色菱形目标标记 | `PathRenderer` | 低 |

---

## 11. 验收（逻辑层）

1. 掌心朝上 → MainMenu 弹（条状贴手）；放下 → 收。
2. MainMenu：`Preferred Path` 进操作菜单；`Settings` 占位；中间留白。
3. 操作菜单四按钮；`Add` 画线、`Clear`/`Send` 二次确认、`Back` 回主菜单保留路径。
4. ADD：捏住拖动画线；若起点不在车上，**实时显示车→起点虚线**。
5. SEND：确认 → 若起点≠车且直达，**车先自动靠拢到起点**，再沿手绘路径走；不直达则提示。
6. CLEAR：确认后清空回 OFF；FOLLOWING 中 CLEAR 停车+清空。
7. 状态横幅任意时刻反映 OFF/ADD/APPROACHING/SEND PATH。
8. Editor：`M` 切菜单、鼠标画线、`A/C/S/B` 快捷键，行为与 HL2 同构。

---

## 附：状态机伪代码（对照 `PreferredPathMenuController`）

```csharp
enum Mode { Off, Add, Approach, Following }

void OnAdd()   { state = Add; handTracker.StartTracking(); }
void OnClear() { handTracker.StopTracking();
                 confirm("Clear all path points?", ok => { if(!ok) return;
                     localPathFollower?.StopFollowing(); cancelApproach();
                     pathRenderer.ClearRenderers(); pathData.Clear();
                     state = Off; }); }
void OnSend()  { if (pathData.Count < 2) { toast("至少画 2 个点"); return; }
                 if (!rosConnected)      { toast("未连接 ROS"); return; }
                 handTracker.StopTracking();
                 confirm("Are you sure you want to SEND PATH to the robot?", ok => {
                     if(!ok) return;
                     // 部署态发 /hrp_path (nav_msgs/Path)；仿真态本地跟随
                     if (localPathFollower != null) localPathFollower.StartFollowing();
                     else pathSender.SendPath(pathData);
                     // 接近段（方案A）：起点≠车且直达 → 先靠拢
                     if (NeedsApproach(pathData, carPose)) { state = Approach; StartApproachTo(pathData[0]); }
                     else { state = Following; }
                 }); }
void OnApproachReached() { state = Following; }   // 到达起点 → 沿手绘路径
void OnBack() { handTracker.StopTracking(); state = Off; showMainMenu(); /* 保留路径 */ }
```
