# Unity 场景搭建指南

本文档记录 Unity 场景中所有物体创建、脚本挂载和引用绑定的完整步骤。

---

## 1. 场景层级结构

```
MainScene
├── MixedRealityToolkit          ← MRTK 自动创建
├── Managers
│   ├── ROSConnectionManager
│   ├── SpatialAnchorManager
│   ├── PathData
│   ├── PathRenderer
│   ├── HandTracker
│   └── PathSender
├── MainMenu (Canvas)
│   └── PreferredPathButton
│       └── Text (TMP)
├── PreferredPathMenu (Canvas)
│   ├── StatusText
│   ├── AddButton
│   │   └── Text (TMP)
│   ├── SendButton
│   │   └── Text (TMP)
│   ├── ClearButton
│   │   └── Text (TMP)
│   └── BackButton
│       └── Text (TMP)
├── ConfirmDialog (Canvas)
│   ├── DialogMessage
│   ├── YesButton
│   │   └── Text (TMP)
│   └── NoButton
│       └── Text (TMP)
├── MixedRealitySceneContent
│   └── MixedRealityPlayspace
│       └── Main Camera
└── Directional Light
```

---

## 2. 物体创建步骤

### 2.1 Managers 物体
1. Hierarchy 右键 → **Create Empty** → 命名 `Managers`
2. 右键 Managers → **Create Empty** → 命名 `ROSConnectionManager`
3. 右键 Managers → **Create Empty** → 命名 `SpatialAnchorManager`
4. 右键 Managers → **Create Empty** → 命名 `PathData`
5. 右键 Managers → **Create Empty** → 命名 `PathRenderer`
6. 右键 Managers → **Create Empty** → 命名 `HandTracker`
7. 右键 Managers → **Create Empty** → 命名 `PathSender`

### 2.2 MainMenu
1. Hierarchy 右键 → **UI** → **Canvas** → 命名 `MainMenu`
2. 右键 MainMenu → **UI** → **Button - TextMeshPro** → 命名 `PreferredPathButton`
3. 展开 PreferredPathButton，选中 `Text (TMP)` 子物体，Inspector 里 Text Input 改为 `Preferred Path`

### 2.3 PreferredPathMenu
1. Hierarchy 右键 → **UI** → **Canvas** → 命名 `PreferredPathMenu`
2. 右键 PreferredPathMenu → **UI** → **Text - TextMeshPro** → 命名 `StatusText`
   - Text Input 改为 `Stage 0: OFF MODE`
   - Font Size 建议 24-30
3. 右键 PreferredPathMenu → **UI** → **Button - TextMeshPro** → 命名 `AddButton`，文字改 `ADD`
4. 右键 PreferredPathMenu → **UI** → **Button - TextMeshPro** → 命名 `SendButton`，文字改 `SEND`
5. 右键 PreferredPathMenu → **UI** → **Button - TextMeshPro** → 命名 `ClearButton`，文字改 `CLEAR`
6. 右键 PreferredPathMenu → **UI** → **Button - TextMeshPro** → 命名 `BackButton`，文字改 `Back`

### 2.4 ConfirmDialog
1. Hierarchy 右键 → **UI** → **Canvas** → 命名 `ConfirmDialog`
2. 右键 ConfirmDialog → **UI** → **Text - TextMeshPro** → 命名 `DialogMessage`
3. 右键 ConfirmDialog → **UI** → **Button - TextMeshPro** → 命名 `YesButton`，文字改 `Yes`
4. 右键 ConfirmDialog → **UI** → **Button - TextMeshPro** → 命名 `NoButton`，文字改 `No`

---

## 3. 脚本挂载

选中物体 → Inspector → **Add Component** → 搜索脚本名 → 添加

| 物体 | 挂载脚本 |
|------|---------|
| ROSConnectionManager | `ROSConnectionManager.cs` |
| SpatialAnchorManager | `SpatialAnchorManager.cs` |
| PathData | `PathData.cs` |
| PathRenderer | `PathRenderer.cs` + `LineRenderer`（内置组件） |
| HandTracker | `HandTracker.cs` |
| PathSender | `PathSender.cs` |
| MainMenu | `MainMenuController.cs` |
| PreferredPathMenu | `PreferredPathMenuController.cs` |
| ConfirmDialog | `ConfirmDialog.cs` |

---

## 4. 脚本字段绑定

### 4.1 MainMenuController（挂在 MainMenu 上）

| 字段 | 拖入物体 |
|------|---------|
| main menu panel | MainMenu |
| preferred path menu | PreferredPathMenu |

### 4.2 PreferredPathMenuController（挂在 PreferredPathMenu 上）

| 字段 | 拖入物体 |
|------|---------|
| status text | StatusText |
| preferred path menu | PreferredPathMenu |
| main menu controller | MainMenu |
| hand tracker | HandTracker |
| path renderer | PathRenderer |
| path data | PathData |
| path sender | PathSender |
| confirm dialog | ConfirmDialog |

### 4.3 PathRenderer（挂在 PathRenderer 物体上）

| 字段 | 拖入物体 |
|------|---------|
| path data | PathData |

### 4.4 HandTracker（挂在 HandTracker 物体上）

| 字段 | 拖入物体 |
|------|---------|
| path data | PathData |

### 4.5 SpatialAnchorManager（挂在 SpatialAnchorManager 上）

| 字段 | 拖入物体 |
|------|---------|
| path data | PathData |

> 注意：SpatialAnchorManager 在 `Start()` 中会自动在当前位置创建锚点，无需手动调用。

### 4.6 ConfirmDialog（挂在 ConfirmDialog Canvas 上）

| 字段 | 拖入物体 |
|------|---------|
| dialog panel | ConfirmDialog（自己） |
| message text | DialogMessage |

---

## 5. 按钮事件绑定

选中按钮 → Inspector → **Button** 组件 → **On Click ()** → 点 **+** → 拖入物体 → 选择函数

| 按钮 | 拖入物体 | 选择函数 |
|------|---------|---------|
| PreferredPathButton | MainMenu | MainMenuController → OnPreferredPathClicked |
| AddButton | PreferredPathMenu | PreferredPathMenuController → OnAddClicked |
| SendButton | PreferredPathMenu | PreferredPathMenuController → OnSendClicked |
| ClearButton | PreferredPathMenu | PreferredPathMenuController → OnClearClicked |
| BackButton | PreferredPathMenu | PreferredPathMenuController → OnBackClicked |
| YesButton | ConfirmDialog | ConfirmDialog → OnYesClicked |
| NoButton | ConfirmDialog | ConfirmDialog → OnNoClicked |

---

## 6. 常见问题

### 拖入字段时出现禁止符号
- 原因：类型不匹配
- UI 文字使用 `TextMeshProUGUI`（不是 `TextMeshPro`）
- 确认脚本中字段类型正确

### Add Component 找不到脚本
- 检查 Unity Console 是否有编译错误
- 等待 Unity 重新编译
- 搜索时输入部分名称即可

### 按钮点击没反应
- 确认 OnClick 事件已绑定
- 确认拖入的是正确的 GameObject（不是子物体）
- 确认选择的函数名正确

### Play 模式下 UI 显示异常
- Canvas 默认是 `Screen Space - Overlay` 模式
- 调整文字 Font Size 改善可读性
- 调整文字颜色（白色或亮色）

### HandTracker 在 Play Mode 下用鼠标替代 MRTK
- `HandTracker.cs` 使用 `#if UNITY_EDITOR` 条件编译
- Editor Play Mode 下：左键点击鼠标模拟捏合采样
- HoloLens 2 真机：自动使用 MRTK 手部追踪（ThumbTip + IndexTip 捏合检测）

### SpatialAnchorManager 自动创建锚点
- `SpatialAnchorManager.cs` 在 `Start()` 中自动创建锚点
- 锚点位置为 SpatialAnchorManager 物体的初始位置
- 可在 Inspector 中调整物体位置来设置锚点初始位置
