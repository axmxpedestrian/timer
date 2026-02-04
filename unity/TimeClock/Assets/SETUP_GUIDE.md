# Unity场景搭建详细指南

## 🎯 概述

本文档详细说明如何在Unity中搭建番茄钟的完整UI场景。

---

## 📋 前置准备

### 1. 安装必要Package
通过 Package Manager 安装：
- **TextMeshPro** (通常已内置)
- **Input System** (可选，如果使用新输入系统)

首次使用TextMeshPro时会提示导入Essential Resources，请点击导入。

### 2. 导入脚本
将 `Scripts` 文件夹拖入项目的 `Assets` 目录。

### 3. 创建文件夹结构
```
Assets/
├── Scripts/        (已提供)
├── Prefabs/        (稍后创建)
├── Scenes/
├── Resources/
│   └── Audio/      (放置音效文件)
└── Sprites/        (可选，放置图标)
```

---

## 🏗️ 场景搭建步骤

### 第一步：创建场景和基础对象

1. 创建新场景 `File > New Scene`，保存为 `MainScene`

2. 创建 **GameManager** 空对象
   - 右键 Hierarchy > Create Empty
   - 命名为 `GameManager`
   - 添加组件 `GameManager.cs`

3. 创建 **EventSystem**（如果没有）
   - 右键 Hierarchy > UI > Event System

---

### 第二步：创建Canvas

1. 右键 Hierarchy > UI > Canvas
2. 设置Canvas组件：
   - Render Mode: `Screen Space - Overlay`
   - UI Scale Mode: `Scale With Screen Size`
   - Reference Resolution: `1920 x 1080`
   - Screen Match Mode: `Match Width Or Height`
   - Match: `0.5`

3. 添加 **Canvas Scaler** 设置（通常自动添加）

---

### 第三步：创建主面板 (MainPanel)

在Canvas下创建：

```
Canvas
└── MainPanel (Panel)
    ├── Header (Horizontal Layout Group)
    │   ├── TitleText (TextMeshPro)
    │   ├── Spacer (Layout Element, Flexible Width)
    │   ├── TopMostToggle (Toggle)
    │   ├── StatisticsButton (Button)
    │   └── SettingsButton (Button)
    │
    ├── TimerSection (Panel)
    │   ├── TimerBackground (Image, 圆角矩形)
    │   ├── TimerText (TextMeshPro - 大字体)
    │   ├── StateText (TextMeshPro)
    │   ├── RoundText (TextMeshPro)
    │   ├── ModeText (TextMeshPro)
    │   └── CurrentTaskText (TextMeshPro)
    │
    ├── ControlButtons (Horizontal Layout Group)
    │   ├── StartCountdownButton (Button)
    │   ├── StartCountupButton (Button)
    │   ├── PauseButton (Button)
    │   ├── ResumeButton (Button)
    │   ├── StopButton (Button)
    │   └── SkipButton (Button)
    │
    └── TaskSection (Panel)
        ├── TaskListHeader (Horizontal Layout Group)
        │   ├── TaskListTitle (TextMeshPro)
        │   └── AddTaskButton (Button)
        └── TaskScrollView (Scroll View)
            └── Viewport
                └── TaskListContent (Vertical Layout Group)
```

#### 详细设置：

**MainPanel:**
- Anchor: Stretch (四角都拉到边缘)
- Left/Right/Top/Bottom: 20
- 添加 `MainUIController.cs` 组件

**Header:**
- Height: 60
- 添加 Horizontal Layout Group
  - Child Alignment: Middle Left
  - Spacing: 15
  - Child Force Expand Width: false

**TitleText:**
- Text: "🍅 番茄钟"
- Font Size: 28
- Font Style: Bold

**TimerSection:**
- Width: 400, Height: 300
- Anchor: Top Center

**TimerText:**
- Text: "25:00"
- Font Size: 96
- Alignment: Center
- Color: #E84C3D (番茄红)

**StateText:**
- Text: "专注中"
- Font Size: 24
- Alignment: Center

**ControlButtons:**
- 添加 Horizontal Layout Group
- Spacing: 20
- Child Force Expand: false

**每个按钮:**
- Width: 100, Height: 50
- Font Size: 18

**TaskScrollView:**
- Anchor: 下半部分区域
- Scroll Direction: Vertical

**TaskListContent:**
- 添加 Vertical Layout Group
  - Spacing: 8
  - Child Force Expand Height: false
- 添加 Content Size Fitter
  - Vertical Fit: Preferred Size
- 添加 `TaskListUI.cs` 组件

---

### 第四步：创建任务项预制体

1. 在Canvas下临时创建一个任务项：

```
TaskItemPrefab (Panel)
├── ColorBar (Image, 左侧窄条)
├── ContentArea (Horizontal Layout Group)
│   ├── TaskNameText (TextMeshPro)
│   ├── Spacer
│   ├── PomodoroCountText (TextMeshPro)
│   └── TotalTimeText (TextMeshPro)
├── SelectButton (Button, 覆盖整个项)
├── EditButton (Button, 右侧小按钮)
└── SelectedIndicator (Image, 选中时显示)
```

**设置：**
- 整体 Height: 60
- ColorBar: Width 6, Anchor 左侧拉伸
- 添加 `TaskItemUI.cs` 组件
- 拖入 `Assets/Prefabs` 文件夹创建预制体
- 删除场景中的临时对象

---

### 第五步：创建设置面板

在Canvas下创建（默认隐藏）：

```
SettingsPanel (Panel, 半透明背景)
└── SettingsContent (Panel, 白色卡片居中)
    ├── Header
    │   ├── TitleText ("设置")
    │   └── CloseButton
    ├── ScrollView
    │   └── Content (Vertical Layout)
    │       ├── Section: 时长设置
    │       │   ├── Label "专注时长(分钟)"
    │       │   ├── FocusDurationInput (TMP_InputField)
    │       │   ├── Label "短休息(分钟)"
    │       │   ├── ShortBreakInput
    │       │   ├── Label "长休息(分钟)"
    │       │   ├── LongBreakInput
    │       │   ├── Label "长休息前轮数"
    │       │   └── RoundsInput
    │       │
    │       ├── Section: 正计时设置
    │       │   ├── Label "最小有效时长(分钟)"
    │       │   ├── CountupMinInput
    │       │   ├── Label "最大时长(分钟)"
    │       │   └── CountupMaxInput
    │       │
    │       ├── Section: 音效设置
    │       │   ├── SoundToggle
    │       │   ├── VolumeSlider
    │       │   └── PreviewSoundButton
    │       │
    │       └── Section: 其他
    │           ├── AutoStartBreakToggle
    │           ├── AutoStartFocusToggle
    │           ├── ClearHistoryButton
    │           └── ResetDefaultButton
    │
    └── SaveButton
```

- 添加 `SettingsUI.cs` 组件
- 默认设为 `SetActive(false)`

---

### 第六步：创建统计面板

```
StatisticsPanel (Panel, 半透明背景)
└── StatisticsContent (Panel, 白色卡片)
    ├── Header
    │   ├── TitleText ("统计")
    │   ├── DailyTabButton
    │   ├── WeeklyTabButton
    │   └── CloseButton
    │
    ├── ChartArea (主要区域)
    │   ├── YAxisLabels
    │   │   ├── YAxisMaxText
    │   │   ├── YAxisMidText
    │   │   └── YAxisUnitText
    │   ├── GridContainer
    │   ├── BarsContainer
    │   └── LabelsContainer
    │
    ├── SummarySection (Horizontal Layout)
    │   ├── TotalPomodorosText
    │   ├── TotalTimeText
    │   ├── StreakText
    │   └── AverageText
    │
    └── TaskFilterDropdown
```

- 添加 `StatisticsUI.cs` 组件
- ChartArea 添加 `BarChartUI.cs` 组件
- 默认设为 `SetActive(false)`

---

### 第七步：创建任务编辑面板

```
TaskEditPanel (Panel, 半透明背景)
└── EditContent (Panel, 白色卡片, 小尺寸)
    ├── TitleText ("新建任务" / "编辑任务")
    ├── TaskNameInput (TMP_InputField)
    ├── ColorPicker (Horizontal Layout)
    │   ├── ColorButton_0 (红)
    │   ├── ColorButton_1 (橙)
    │   ├── ColorButton_2 (黄)
    │   ├── ColorButton_3 (绿)
    │   ├── ColorButton_4 (蓝)
    │   └── ColorButton_5 (紫)
    ├── ButtonsRow
    │   ├── DeleteButton (仅编辑时显示)
    │   ├── CancelButton
    │   └── SaveButton
```

- 默认设为 `SetActive(false)`

---

### 第八步：创建柱状图预制体

1. **BarPrefab (柱子)**
```
BarPrefab (Image)
└── ValueText (TextMeshPro, 顶部)
```
- Image Color: #E84C3D (番茄红)
- Pivot: (0.5, 0) 底部中心

2. **LabelPrefab (X轴标签)**
```
LabelPrefab (TextMeshPro)
```
- Font Size: 12
- Alignment: Top Center

3. **GridLinePrefab (网格线)**
```
GridLinePrefab (Image)
```
- Color: 浅灰色半透明
- Height: 1

将这些拖入 `Assets/Prefabs`

---

### 第九步：连接引用

1. 选中 `MainPanel`，在 Inspector 中的 `MainUIController` 组件：
   - 拖入对应的UI元素引用

2. 选中 `TaskListContent` 父对象，设置 `TaskListUI`：
   - Task Item Prefab: 拖入任务项预制体
   - Task List Content: 拖入 TaskListContent

3. 设置 `SettingsUI` 和 `StatisticsUI` 的所有引用

4. 设置 `BarChartUI` 的预制体引用

---

### 第十步：创建音效资源

在 `Assets/Resources/Audio/` 下放置：
- `timer_complete.wav` - 计时完成音效
- `button_click.wav` - 按钮点击音效 (可选)

可以从免费音效网站下载，推荐：
- https://freesound.org
- https://mixkit.co/free-sound-effects/

---

## 🎨 推荐的视觉设置

### 颜色方案
- 主色: #E84C3D (番茄红)
- 背景: #F5F5F5 (浅灰)
- 卡片: #FFFFFF (白色)
- 文字主色: #212121
- 文字次色: #757575
- 成功色: #2ECC71 (绿色)
- 警告色: #F1C40F (黄色)

### 字体
- 推荐使用 TextMeshPro 的默认字体
- 或导入中文字体：思源黑体、阿里普惠体等

### 按钮样式
- 圆角: 8-12px
- 投影: 可选添加轻微阴影
- 悬停: 轻微变亮
- 点击: 轻微缩小

---

## 🔧 Build Settings

1. `File > Build Settings`
2. Platform: `PC, Mac & Linux Standalone`
3. Target Platform: `Windows`
4. Architecture: `x86_64`

5. `Player Settings`:
   - Resolution: Default 1920x1080
   - Resizable Window: ✓
   - Run In Background: ✓
   - Company Name: 自定义
   - Product Name: 番茄钟

---

## ✅ 测试清单

- [ ] 创建任务
- [ ] 编辑/删除任务
- [ ] 选择任务绑定
- [ ] 倒计时开始/暂停/停止
- [ ] 正计时开始/停止
- [ ] 完成音效播放
- [ ] 轮次自动切换
- [ ] 统计数据显示
- [ ] 柱状图正确渲染
- [ ] 设置保存生效
- [ ] 关闭重开数据保留

---

## 🐛 常见问题

**Q: TextMeshPro文字不显示？**
A: 确保导入了TMP Essential Resources

**Q: 按钮点击无反应？**
A: 检查EventSystem是否存在，Canvas是否有GraphicRaycaster

**Q: 数据没有保存？**
A: 检查DataManager是否正确初始化

**Q: 柱状图不显示？**
A: 确认BarChartUI的预制体引用已正确设置
