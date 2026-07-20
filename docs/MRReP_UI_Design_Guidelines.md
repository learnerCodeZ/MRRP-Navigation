# MRReP HoloLens2 UI 设计规范（v2 · 对齐参考图）

> **版本**：v2，2026-07-17。视觉与交互**对齐用户参考图**（半透明藏青面板 + 圆角白胶囊按钮 + 条状贴手菜单 + 掌心触发）。
> **依据**：参考图已确认——MainMenu（竖条状：Preferred Path / Settings，中间留白）、SEND PATH 确认弹窗、CLEAR 确认弹窗。
> **菜单结构（用户确认·两级）**：MainMenu = Preferred Path + Settings（中间留白预留） → PreferredPathMenu = Back / Clear / Send / Add。行为细节见 `MRReP_菜单与画线交互规范.md`。
> **核心诉求**：菜单由**伸出手掌（掌心朝上）触发**，条状吸附手腕弹出；放下手即收起。

---

## 0. ❗ 相对 v1 的变更说明（必读）

| 项 | v1（旧） | v2（新，以参考图为准） |
|---|---|---|
| **面板/按钮边角** | 硬直角、无圆角（工业感） | **圆角**：面板圆角≈短边 8%，按钮胶囊/大圆角 |
| **激活态按钮** | 仅 alpha 提亮 | **青色 `#00E5FF` 填充**高亮（参考图 Yes 按钮） |
| **触发方式** | 文档未强调 | **掌心朝上触发**为唯一入口，明确定义 MRTK 接线 |
| 配色（藏青/青/白/黄） | — | **保持不变**（新旧一致） |

> 其余配色、字体、图标线框风格延续 v1，仅几何与触发按参考图更新。

---

## 1. 设计总纲

- **风格**：扁平 + 玻璃拟态（Diegetic Glassmorphism）半透明悬浮面板，低信息密度、高对比，能透过 UI 看到真实地面。
- **美学参考**：HoloLens2 官方 MR UI 指南（克制、通透、功能优先），**不走** Apple Vision Pro 那种重度科幻装饰。
- **交互入口**：掌心朝上 → 菜单吸附手掌上方 ~30cm 淡入；手掌放下/翻转 → 淡出。

---

## 2. 颜色系统（Unity 可直接用）

| 用途 | Hex | RGB(0–255) | Unity Color (0–1) | Alpha |
|---|---|---|---|---|
| 面板背景（藏青） | `#1A237E` | (26,35,126) | (0.102, 0.137, 0.494) | 0.70–0.80 |
| 面板渐变暗端（可选） | `#1A255C` | (26,37,92) | (0.102, 0.145, 0.361) | — |
| 面板渐变亮端（可选） | `#2C3A8A` | (44,58,138) | (0.173, 0.227, 0.541) | — |
| 面板描边 | `#FFFFFF` | (255,255,255) | (1,1,1) | 0.30–0.40 |
| 强调/激活（青） | `#00E5FF` | (0,229,255) | (0, 0.898, 1.0) | 1.0 |
| 主文字 | `#FFFFFF` | (255,255,255) | (1,1,1) | 1.0 |
| 次文字 | `#FFFFFF` | — | (1,1,1) | 0.70 |
| 路径采样球（青蓝发光） | `#4DEEEA` | (77,238,234) | (0.302, 0.933, 0.918) | 0.6–0.8（发光） |
| 终点/目标标记（黄） | `#FFD700` | (255,215,0) | (1, 0.839, 0) | 1.0 |
| 柔和投影 | `#000000` | (0,0,0) | (0,0,0) | 0.20 |

> 面板做**细腻渐变**（藏青亮端→暗端），不要死板纯色；Alpha 70–80% 保证通透感。

---

## 3. 形状与几何（对齐参考图：圆角）

- **面板**：圆角矩形。圆角半径 ≈ 面板短边的 **6–10%**（视觉上≈图中的 8px 量级）。1px 白色描边（alpha 0.3–0.4），柔和投影（黑 20%），**半透明能透出地面**。
- **按钮**：**胶囊形**，圆角半径 ≈ 按钮高度的 **40–50%**，**图标在左、文字在右**（横向）。两态：
  - **默认态**：**白/浅色填充** + 深色（藏青/黑）线框图标与文字（参考图：白底 + 深色图标，高对比）。
  - **激活/选中态**：**青色 `#00E5FF` 强调**（描边或填充），提示当前模式。
- **路径采样球**：青蓝 `#4DEEEA` 半透明发光球体（已有，保持）。
- **终点标记**：黄色 `#FFD700` 八面体/菱形（已有，保持）。

> ⚠️ v1 写的"硬直角、无大圆角"**作废**——参考图明确是圆角，以此为准。

---

## 4. 排版（Typography）

- **字体**：无衬线 **Segoe UI**（HL2 系统字体）或 Arial / Inter。禁用衬线与装饰体。TMP 用 SDF 版本。
- **层级**：

| 元素 | 字重 | 字号（相对） | 大小写 |
|---|---|---|---|
| 品牌 logo "MRReP"（左上） | Medium/SemiBold | 中 | Title Case |
| 状态小字 "Stage 0" | Medium | 中（≈24pt 量级） | Title Case |
| 主标题 "SEND PATH" / "ADD MODE" | **Bold** | **最大（≈32pt 量级）** | **ALL CAPS** |
| 正文问句 | Regular | 小（≈18pt 量级） | 句首大写 |
| 按钮文字 | Medium | 适中，居中 | Title Case |

- 全大写**仅**用于主标题与状态栏大字；按钮文字用 Title Case。字距/行距拉开，营造开阔严谨的仪表感。

---

## 5. 组件清单与布局（逐个对齐参考图）

### 5.1 顶部状态横幅（Status Banner）
- 两行结构：上 = 辅助小字（`Stage 0`），下 = 当前模式大字（`OFF MODE` / `ADD MODE` / `CLEAR MODE` / `SEND MODE`）。
- 圆角长条，藏青半透明，浮在主面板顶部或场景上方。模式切换时大字实时更新。

### 5.2 MainMenu（掌心条状主菜单 · 核心入口）
- **触发**：掌心朝上 → `HandConstraintPalmUp` 吸附手腕外侧淡入；放下 → 淡出（见 §8 接线）。
- **形态**：**竖条状**（portrait strip），小巧贴手、不挡视野（用户确认"条状的在手边也简洁"）。圆角藏青面板 + 白描边。
- **结构**（自上而下）：
  1. 顶部状态横幅（`Stage 0` + 当前模式大字）。
  2. 按钮 **`Preferred Path`**（白胶囊，图标左+文字右）→ 进入 PreferredPathMenu。
  3. **中间留白**——为未来功能预留（Direct Nav / Waypoints / Relocate 等），现在空着。
  4. 按钮 **`Settings`**（白胶囊，齿轮图标，图标左+文字右）→ 设置（**暂为占位**）。
- 按钮：**白填充胶囊**，**图标在左、文字在右**（横向），深色（藏青/黑）图标与文字高对比；选中态青色强调。

### 5.3 PreferredPathMenu（路径操作菜单）
- 由 MainMenu 的 `Preferred Path` 进入；同款圆角藏青面板、白胶囊按钮、图标左+文字右、大留白。
- 4 按钮（顺序按用户确认）：
  | 按钮 | 图标 | 说明 |
  |---|---|---|
  | Back | `←` | 回 MainMenu（保留路径） |
  | Clear | `✕` | 删除路径（二次确认） |
  | Send | 线框卡车/纸飞机 | 发送给 robot（二次确认） |
  | Add | 细 `+` | 进入画线 |
- 当前激活模式按钮高亮（青色强调），其余默认态。

### 5.4 SEND PATH 确认弹窗（对齐参考图）
- **结构**：
  - 左上：`MRReP` 品牌（白，中粗）。
  - 标题区：`Stage 0`（小字）+ `SEND PATH`（大字全大写）。
  - 正文：`Are you sure you want to SEND PATH to the robot?`（居中）。
  - 底部双按钮（白填充胶囊，图标左+文字右）：
    - **Yes**：`✓` + "Yes"（高亮/青色强调）。
    - **No**：`✗` + "No"（默认态）。
  - 按钮间距 ≈ 20–24px，图标↔文字间距 ≈ 16px。
- **容器**：圆角藏青半透明面板（~85%），白描边，柔和投影——与主菜单同语言。

### 5.5 CLEAR 确认弹窗（对齐参考图）
- 标题区：`Stage 0` + `CLEAR MODE`（大字）。
- 正文：`Are you sure you want to clear all path points?`（居中）。（注：参考图原文为 "delete all obstacles"，本系统按"清空路径"语义。）
- 底部 Yes(`✓`) / No(`✗`) 双按钮，同 §5.4 样式。

### 5.6 接近段虚线（车 → 路径起点）🆕
- 当手绘路径**起点不在小车当前位置**时，画一条**虚线**连接 小车 → 起点（行为见交互规范 §6）。
- **样式**：虚线（dash），区别于实线手绘路径；线宽略细（≈0.008m）。
- **颜色（建议）**：直达 = 白 `#FFFFFF` α0.6 或青 `#00E5FF` α0.7；**不直达 = 红/橙** `#FF5252` α0.8 + 警告。
- ADD 时作**预览**；Send 确认后随靠拢推进而收缩，到达起点消失。

---

## 6. 图标规范

- **风格**：极简单色**线框**（Line/Wireframe），纯白，线条纤细，无拟真、无阴影、无填充块。
- 列表：`+`(ADD)、`✕`(CLEAR)、线框卡车/纸飞机(SEND)、`←`(Back)、`✓`(Yes)、`✗`(No)。
- 尺寸统一；按钮内**图标在左、文字在右**（横向居中对齐）。

---

## 7. 动效与反馈

- **掌心触发**：掌心朝上达阈值 → 菜单淡入 + 轻微缩放（0.9→1.0）；手放下 → 淡出。
- **按钮**：
  - Hover / Near（射线或手指接近）：alpha 或亮度轻微提亮。
  - Press：缩放到 ~0.9 + 青色短闪。
- **模式切换**：状态横幅大字更新；激活按钮变青色填充。

---

## 8. Unity / MRTK 2.8 实现要点（重构视觉与触发，业务逻辑基本不动）

> 说明：掌心触发、面板材质、按钮外观多为**场景/Prefab/材质**工作（在 Unity 编辑器里做），不是纯代码。下方给出可落地的接线与参数。

### 8.1 掌心触发（替换当前 `Start()` 里静态 `SetActive(false)`）
- 主菜单根节点挂 **`HandConstraintPalmUp`** solver（MRTK2 自带）。
  - `HandConstraint`：`ConstraintRotation` 锁定面向用户；安全区选 `UlnarSide`（手腕外侧，不挡手）。
  - `HandConstraintPalmUp`：设 `FacingThreshold`（掌心朝上阈值，如 60°）；`UseHandedness` 锁单手或双手。
- 事件接线（Inspector → UnityEvent）：
  - `OnFirstHandPalmUp` / `OnHandPalmUp` → `MainMenuController.ShowMainMenu()`
  - `OnLastHandLost`（手离开） → `MainMenuController.HideMainMenu()`
- 这样**伸出手掌即弹、放下即收**，符合核心诉求。`MainMenuController.cs` 已有 `ShowMainMenu/HideMainMenu`，直接复用。

### 8.2 面板材质（半透明藏青 + 圆角 + 描边）
- **MRTK Standard Material**：`Color = #1A237E`，Alpha 0.75；渲染模式 `Transparent`；`Smoothness` 0.5–0.7（玻璃质感）；可叠 `_Gradient`/渐变贴图做亮→暗。
- **圆角**：MRTK2 用圆角 shader 或 `RoundedRect`（`_Radius` ≈ 短边 8%）；或用圆角遮罩 Sprite。
- **描边**：单独一层细 RectTransform Quad，白色 Alpha 0.3，置于面板边缘。
- **投影**：软阴影 plane 在面板下方，黑 20%。

### 8.3 按钮（圆角胶囊 + 图标+文字）
- 用 **`PressableButton` + `IconAndText`** 组合（或 `PressableButtonHoloLens2` 预制体改色）。
  - Icon：白色线框 Sprite；Label：TMP 文字。
  - 默认态：背景透明 + 白描边；激活态：背景改青色 `#00E5FF` 填充（绑定模式状态切换 `SetActive`/材质切换）。
  - **图标在左、文字在右**（横向），按钮间距大留白；白填充胶囊 + 深色图标/文字（选中态青色强调）。

### 8.4 确认弹窗（复用既有逻辑，只重做视觉）
- `ConfirmDialog.cs` 的 `Show / OnYesClicked / OnNoClicked` **逻辑已对，不动**；只把 Prefab 重做成 §5.3 结构（MRReP logo + Stage 0 + SEND PATH + 问句 + Yes✓青 / No✗透明），对齐参考图 1。

### 8.5 字体
- TextMeshPro：Segoe UI SDF（HL2 系统字体）或 Arial SDF；主标题 Bold + 全大写。

---

## 9. 与既有代码的映射

| 组件 | 现有脚本 | 改造点 |
|---|---|---|
| 主菜单显隐 | `MainMenuController.cs`（已有 `ShowMainMenu/HideMainMenu`） | 接 `HandConstraintPalmUp` 事件，掌心触发 |
| 模式状态横幅 | （待建 / 绑模式枚举） | TMP 两行，随 ADD/CLEAR/SEND 更新大字 |
| SEND 确认弹窗 | `ConfirmDialog.cs`（逻辑已对） | **只重做 Prefab 视觉**对齐参考图 1 |
| 掌心触发 solver | （场景配置） | `HandConstraintPalmUp` 挂菜单根 |
| 画线视觉 | `HandTracker.cs` / `PathRenderer.cs` | 青蓝球 `#4DEEEA` + 黄终点 `#FFD700` **已符合**，保持 |
| Play Mode 测试 | `PlayModeUIFixer.cs` | 不变（鼠标模拟捏合画线） |

---

## 10. 验收标准（对齐参考图）

1. **掌心朝上 → MainMenu 条状淡入吸附手腕；放下手 → 淡出。**（核心诉求）
2. **MainMenu**：竖条状，`Preferred Path` + `Settings` 两按钮（中间留白），白胶囊 + 图标左/文字右。
3. **PreferredPathMenu**：Back/Clear/Send/Add 四按钮，同款白胶囊；当前模式青色强调。
4. SEND / CLEAR 弹窗对齐参考图：`Stage 0` + 模式大字 + 问句 + Yes✓ / No✗ 白胶囊。
5. 所有面板：**半透明藏青 + 圆角 + 白描边 + 柔和投影**，能透出真实地面。
6. 路径：青蓝采样球 `#4DEEEA` + 黄色终点菱形 `#FFD700`。
7. 🆕 起点不在车上时显示**车→起点虚线**（直达白/青，不直达红/橙）。

---

## 附：给 Claude 的实现指令模板

> 请基于本规范，在 Unity MRTK 2.8 项目里：
> 1. 给主菜单根节点配置 `HandConstraintPalmUp`，把 `OnFirstHandPalmUp` 接到 `MainMenuController.ShowMainMenu`、手离开接到 `HideMainMenu`，实现掌心触发。
> 2. 给面板做 MRTK Standard Material（`#1A237E` / Alpha 0.75 / Transparent / 圆角 / 白描边）。
> 3. 把 SEND 确认弹窗 Prefab 重做成"MRReP + Stage 0 + SEND PATH + 问句 + Yes✓青 / No✗透明"，复用现有 `ConfirmDialog.cs`。
> 4. 主菜单按钮用圆角胶囊 + 白线框图标 + 白字，激活态青色填充。
> 给出每个步骤在 Inspector 里的具体操作和需要新建/修改的 Prefab 清单。
