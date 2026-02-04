# 🍅 Unity番茄钟 - UI搭建完整教程

## 📋 目录
1. [项目初始设置](#1-项目初始设置)
2. [创建管理器对象](#2-创建管理器对象)
3. [创建Canvas画布](#3-创建canvas画布)
4. [创建主面板MainPanel](#4-创建主面板mainpanel)
5. [创建设置面板SettingsPanel](#5-创建设置面板settingspanel)
6. [创建统计面板StatisticsPanel](#6-创建统计面板statisticspanel)
7. [创建任务编辑面板TaskEditPanel](#7-创建任务编辑面板taskeditpanel)
8. [创建预制体Prefabs](#8-创建预制体prefabs)
9. [连接脚本引用](#9-连接脚本引用)
10. [最终检查](#10-最终检查)

---

## 1. 项目初始设置

### 1.1 创建Unity项目
- 打开Unity Hub → New Project
- 选择 **2D (URP)** 或 **2D Core** 模板
- 项目名称：`PomodoroTimer`
- 点击Create

### 1.2 导入TextMeshPro
- 首次创建UI Text时会提示导入
- 或者：Window → TextMeshPro → Import TMP Essential Resources

### 1.3 导入脚本
1. 解压下载的zip文件
2. 将 `Scripts` 文件夹拖入 Unity 的 `Assets` 目录
3. 等待编译完成（右下角转圈结束）

### 1.4 创建文件夹结构
在 Project 窗口的 Assets 下右键创建：
```
Assets/
├── Scripts/       ← (已导入)
├── Prefabs/       ← 右键 Create → Folder
├── Scenes/        ← 右键 Create → Folder  
└── Resources/
    └── Audio/     ← 放音效文件
```

### 1.5 保存场景
- File → Save As
- 保存到 `Assets/Scenes/MainScene.unity`

---

## 2. 创建管理器对象

### 2.1 创建GameManager
1. 在 Hierarchy 窗口右键 → **Create Empty**
2. 重命名为 `GameManager`
3. 在 Inspector 窗口点击 **Add Component**
4. 搜索并添加 `GameManager` 脚本

**注意**：这是整个游戏的入口，必须创建！

---

## 3. 创建Canvas画布

### 3.1 创建Canvas
1. Hierarchy 右键 → **UI → Canvas**

### 3.2 设置Canvas组件
选中Canvas，在Inspector中设置：

| 组件 | 属性 | 值 |
|------|------|-----|
| Canvas | Render Mode | Screen Space - Overlay |
| Canvas Scaler | UI Scale Mode | Scale With Screen Size |
| Canvas Scaler | Reference Resolution | X: 1920, Y: 1080 |
| Canvas Scaler | Screen Match Mode | Match Width Or Height |
| Canvas Scaler | Match | 0.5 |

### 3.3 确认EventSystem存在
创建Canvas时会自动创建EventSystem，确认Hierarchy中有它。

---

## 4. 创建主面板MainPanel

### 4.1 创建MainPanel容器
1. 选中Canvas
2. 右键 → **UI → Panel**
3. 重命名为 `MainPanel`
4. 设置RectTransform（按住Alt点击锚点图标选择stretch-stretch，填满整个画布）:
   - Left: 20, Right: 20, Top: 20, Bottom: 20

5. **添加脚本**：Add Component → `MainUIController`

6. 设置Panel的Image组件：
   - Color: #F5F5F5 (浅灰色背景)

---

### 4.2 创建Header（顶部栏）

1. 选中MainPanel，右键 → **UI → Panel**
2. 重命名为 `Header`
3. RectTransform设置：
   - Anchor: Top-Stretch (顶部横向拉伸)
   - Left: 0, Right: 0, Top: 0
   - Height: 60
   - Pivot: (0.5, 1)

4. 添加组件 **Horizontal Layout Group**：
   - Child Alignment: Middle Left
   - Spacing: 15
   - Padding: Left 20, Right 20
   - Child Force Expand Width: ❌ (取消勾选)
   - Child Force Expand Height: ❌ (取消勾选)

5. Image组件：
   - Color: #FFFFFF (白色)

#### 4.2.1 创建TitleText
1. 选中Header，右键 → **UI → Text - TextMeshPro**
2. 重命名为 `TitleText`
3. TextMeshPro设置：
   - Text: `🍅 番茄钟`
   - Font Size: 28
   - Font Style: Bold
   - Color: #E84C3D (番茄红)
4. 添加 **Layout Element** 组件：
   - Preferred Width: 150

#### 4.2.2 创建Spacer（弹性空间）
1. 选中Header，右键 → **Create Empty**
2. 重命名为 `Spacer`
3. 添加 **Layout Element** 组件：
   - Flexible Width: 1 (这会占据剩余空间)

#### 4.2.3 创建TopMostToggle（置顶开关）
1. 选中Header，右键 → **UI → Toggle**
2. 重命名为 `TopMostToggle`
3. 修改子物体Label的文字为 `置顶`
4. 添加 **Layout Element**：
   - Preferred Width: 80

#### 4.2.4 创建StatisticsButton
1. 选中Header，右键 → **UI → Button - TextMeshPro**
2. 重命名为 `StatisticsButton`
3. 修改子物体Text的文字为 `📊 统计`
4. 添加 **Layout Element**：
   - Preferred Width: 100
   - Preferred Height: 40

#### 4.2.5 创建SettingsButton
1. 选中Header，右键 → **UI → Button - TextMeshPro**
2. 重命名为 `SettingsButton`
3. 修改子物体Text的文字为 `⚙️ 设置`
4. 添加 **Layout Element**：
   - Preferred Width: 100
   - Preferred Height: 40

---

### 4.3 创建TimerSection（计时器区域）

1. 选中MainPanel，右键 → **UI → Panel**
2. 重命名为 `TimerSection`
3. RectTransform设置：
   - Anchor: Top-Center
   - Pos X: 0, Pos Y: -100
   - Width: 500, Height: 350
   - Pivot: (0.5, 1)

4. Image组件：
   - Color: #FFFFFF (白色)

#### 4.3.1 创建TimerBackground
1. 选中TimerSection，右键 → **UI → Image**
2. 重命名为 `TimerBackground`
3. RectTransform：
   - Anchor: Middle-Center
   - Pos X: 0, Pos Y: 30
   - Width: 300, Height: 300
4. Image组件：
   - Color: #E84C3D，Alpha设为40 (半透明番茄红)

#### 4.3.2 创建TimerText
1. 选中TimerSection，右键 → **UI → Text - TextMeshPro**
2. 重命名为 `TimerText`
3. RectTransform：
   - Anchor: Middle-Center
   - Pos X: 0, Pos Y: 50
   - Width: 400, Height: 120
4. TextMeshPro设置：
   - Text: `25:00`
   - Font Size: 96
   - Alignment: Center
   - Color: #E84C3D

#### 4.3.3 创建StateText
1. 选中TimerSection，右键 → **UI → Text - TextMeshPro**
2. 重命名为 `StateText`
3. RectTransform：
   - Anchor: Middle-Center
   - Pos X: 0, Pos Y: -30
   - Width: 300, Height: 40
4. TextMeshPro设置：
   - Text: `专注中`
   - Font Size: 28
   - Alignment: Center
   - Color: #333333

#### 4.3.4 创建RoundText
1. 选中TimerSection，右键 → **UI → Text - TextMeshPro**
2. 重命名为 `RoundText`
3. RectTransform：
   - Anchor: Middle-Center
   - Pos X: 0, Pos Y: -70
   - Width: 300, Height: 30
4. TextMeshPro设置：
   - Text: `第 1 轮 / 共 4 轮`
   - Font Size: 18
   - Alignment: Center
   - Color: #666666

#### 4.3.5 创建ModeText
1. 选中TimerSection，右键 → **UI → Text - TextMeshPro**
2. 重命名为 `ModeText`
3. RectTransform：
   - Anchor: Middle-Center
   - Pos X: 0, Pos Y: -100
   - Width: 200, Height: 30
4. TextMeshPro设置：
   - Text: `倒计时`
   - Font Size: 16
   - Alignment: Center
   - Color: #999999

#### 4.3.6 创建CurrentTaskText
1. 选中TimerSection，右键 → **UI → Text - TextMeshPro**
2. 重命名为 `CurrentTaskText`
3. RectTransform：
   - Anchor: Middle-Center
   - Pos X: 0, Pos Y: -140
   - Width: 400, Height: 30
4. TextMeshPro设置：
   - Text: `请选择任务`
   - Font Size: 18
   - Alignment: Center
   - Color: #999999

---

### 4.4 创建ControlButtons（控制按钮）

1. 选中MainPanel，右键 → **UI → Panel**
2. 重命名为 `ControlButtons`
3. RectTransform：
   - Anchor: Top-Center
   - Pos X: 0, Pos Y: -480
   - Width: 600, Height: 60
   - Pivot: (0.5, 1)

4. 添加 **Horizontal Layout Group**：
   - Child Alignment: Middle Center
   - Spacing: 15
   - Child Force Expand: 都取消勾选

5. Image组件：
   - Color: Alpha设为0 (透明)

#### 创建6个按钮（都在ControlButtons下）

**StartCountdownButton:**
1. 右键 → UI → Button - TextMeshPro
2. 重命名为 `StartCountdownButton`
3. 子物体Text设为 `▶ 开始`
4. Button颜色: Normal #E84C3D, Highlighted #F05A4D
5. 添加Layout Element: Preferred Width 120, Height 50

**StartCountupButton:**
1. 同上创建，重命名为 `StartCountupButton`
2. 子物体Text设为 `⏱ 正计时`
3. Button颜色: Normal #3498DB
4. Layout Element: Preferred Width 120, Height 50

**PauseButton:**
1. 同上创建，重命名为 `PauseButton`
2. 子物体Text设为 `⏸ 暂停`
3. Button颜色: Normal #F1C40F
4. Layout Element: Preferred Width 100, Height 50

**ResumeButton:**
1. 同上创建，重命名为 `ResumeButton`
2. 子物体Text设为 `▶ 继续`
3. Button颜色: Normal #2ECC71
4. Layout Element: Preferred Width 100, Height 50

**StopButton:**
1. 同上创建，重命名为 `StopButton`
2. 子物体Text设为 `⏹ 停止`
3. Button颜色: Normal #95A5A6
4. Layout Element: Preferred Width 100, Height 50

**SkipButton:**
1. 同上创建，重命名为 `SkipButton`
2. 子物体Text设为 `⏭ 跳过`
3. Button颜色: Normal #9B59B6
4. Layout Element: Preferred Width 100, Height 50

---

### 4.5 创建TaskSection（任务列表区域）

1. 选中MainPanel，右键 → **UI → Panel**
2. 重命名为 `TaskSection`
3. RectTransform：
   - Anchor: Stretch-Stretch
   - Left: 20, Right: 20
   - Top: 560, Bottom: 20
4. Image: Color #FFFFFF

#### 4.5.1 创建TaskListHeader
1. 选中TaskSection，右键 → **UI → Panel**
2. 重命名为 `TaskListHeader`
3. RectTransform：
   - Anchor: Top-Stretch
   - Left: 0, Right: 0, Top: 0
   - Height: 50

4. 添加 **Horizontal Layout Group**：
   - Child Alignment: Middle Left
   - Padding: Left 15, Right 15
   - Spacing: 10

5. Image: Color #F8F8F8

**在TaskListHeader下创建：**

**TaskListTitle:**
1. 右键 → UI → Text - TextMeshPro
2. 重命名为 `TaskListTitle`
3. Text: `📋 任务列表`
4. Font Size: 20
5. 添加 Layout Element: Flexible Width 1

**AddTaskButton:**
1. 右键 → UI → Button - TextMeshPro
2. 重命名为 `AddTaskButton`
3. 子物体Text: `+ 添加任务`
4. 添加 Layout Element: Preferred Width 120, Height 40
5. Button颜色: Normal #2ECC71

#### 4.5.2 创建TaskScrollView
1. 选中TaskSection，右键 → **UI → Scroll View**
2. 重命名为 `TaskScrollView`
3. RectTransform：
   - Anchor: Stretch-Stretch
   - Left: 0, Right: 0, Top: 55, Bottom: 0

4. Scroll Rect组件：
   - Horizontal: ❌ (取消勾选)
   - Vertical: ✅

5. **删除**子物体 `Scrollbar Horizontal`

6. 找到子物体路径 `Viewport → Content`，选中 `Content`：
   - 重命名为 `TaskListContent`
   - RectTransform:
     - Anchor: Top-Stretch
     - Left: 10, Right: 10, Top: 0
     - Pivot: (0.5, 1)
   - 添加 **Vertical Layout Group**：
     - Spacing: 8
     - Child Force Expand Height: ❌
     - Child Alignment: Upper Center
   - 添加 **Content Size Fitter**：
     - Vertical Fit: Preferred Size

7. 选中TaskSection，**添加脚本** `TaskListUI`

---

## 5. 创建设置面板SettingsPanel

### 5.1 创建SettingsPanel
1. 选中Canvas，右键 → **UI → Panel**
2. 重命名为 `SettingsPanel`
3. RectTransform: 填满整个Canvas
4. Image: Color #000000, Alpha 150 (半透明黑色遮罩)
5. **默认隐藏**：Inspector顶部取消勾选 ✅

### 5.2 创建SettingsContent（设置内容卡片）
1. 选中SettingsPanel，右键 → **UI → Panel**
2. 重命名为 `SettingsContent`
3. RectTransform：
   - Anchor: Middle-Center
   - Width: 500, Height: 700
4. Image: Color #FFFFFF
5. **添加脚本** `SettingsUI`

### 5.3 创建设置界面内容

#### 5.3.1 Header区域
在SettingsContent下创建：

1. **SettingsHeader** (Panel):
   - Anchor: Top-Stretch, Height 60
   - 添加 Horizontal Layout Group

2. 在SettingsHeader下：
   - **SettingsTitleText** (TextMeshPro): `⚙️ 设置`, Font Size 24
   - **Spacer** (Empty, Layout Element Flexible Width 1)
   - **CloseButton** (Button): Text `✕`, Width 40

#### 5.3.2 ScrollView
1. 选中SettingsContent，右键 → **UI → Scroll View**
2. RectTransform：
   - Anchor: Stretch-Stretch
   - Top: 65, Bottom: 70, Left: 10, Right: 10

3. 在Content下创建所有设置项...

**每个设置项的结构：**
```
SettingRow (Horizontal Layout Group)
├── Label (TextMeshPro, 说明文字)
└── InputField (TMP_InputField, 输入框)
```

**需要创建的输入框（命名要与脚本对应）：**

| 变量名 | 标签文字 | 类型 |
|--------|---------|------|
| FocusDurationInput | 专注时长(分钟) | TMP_InputField |
| ShortBreakInput | 短休息(分钟) | TMP_InputField |
| LongBreakInput | 长休息(分钟) | TMP_InputField |
| RoundsInput | 长休息前轮数 | TMP_InputField |
| CountupMinInput | 正计时最小有效(分钟) | TMP_InputField |
| CountupMaxInput | 正计时最大(分钟) | TMP_InputField |
| SoundToggle | 启用音效 | Toggle |
| VolumeSlider | 音量 | Slider |
| AutoStartBreakToggle | 自动开始休息 | Toggle |
| AutoStartFocusToggle | 自动开始专注 | Toggle |

#### 5.3.3 底部按钮
在SettingsContent下：

1. **ButtonsRow** (Panel):
   - Anchor: Bottom-Stretch, Height 60
   - 添加 Horizontal Layout Group

2. 在ButtonsRow下创建按钮：
   - **ResetDefaultButton**: `恢复默认`
   - **ClearHistoryButton**: `清除历史`
   - **SaveButton**: `保存`, 颜色 #2ECC71

---

## 6. 创建统计面板StatisticsPanel

### 6.1 创建StatisticsPanel
1. 选中Canvas，右键 → **UI → Panel**
2. 重命名为 `StatisticsPanel`
3. RectTransform: 填满Canvas
4. Image: Color #000000, Alpha 150
5. **默认隐藏**

### 6.2 创建StatisticsContent
1. 选中StatisticsPanel，右键 → **UI → Panel**
2. 重命名为 `StatisticsContent`
3. RectTransform：
   - Anchor: Middle-Center
   - Width: 800, Height: 600
4. Image: Color #FFFFFF
5. **添加脚本** `StatisticsUI`

### 6.3 创建统计界面内容

#### 6.3.1 Header
1. **StatsHeader** (Panel): Anchor Top-Stretch, Height 60
2. 添加 Horizontal Layout Group
3. 子物体：
   - **StatsTitleText**: `📊 统计`
   - **DailyTabButton**: `每日`
   - **WeeklyTabButton**: `每周`
   - **Spacer**
   - **TaskFilterDropdown**: TMP_Dropdown
   - **CloseButton**: `✕`

#### 6.3.2 ChartArea（柱状图区域）
1. **ChartArea** (Panel):
   - Anchor: Stretch-Stretch
   - Top: 70, Bottom: 150, Left: 60, Right: 20
2. **添加脚本** `BarChartUI`

3. 在ChartArea下创建：
   - **GridContainer** (Empty): Anchor Stretch-Stretch
   - **BarsContainer** (Empty): Anchor Stretch-Stretch
   - **LabelsContainer** (Empty): Anchor Bottom-Stretch, Height 50

#### 6.3.3 Y轴标签
在ChartArea外，StatisticsContent下：
1. **YAxisMaxText** (TextMeshPro): Anchor左上
2. **YAxisMidText** (TextMeshPro): Anchor左中
3. **YAxisUnitText** (TextMeshPro): 显示单位如"分钟"

#### 6.3.4 Summary区域
1. **SummarySection** (Panel):
   - Anchor: Bottom-Stretch, Height 80
   - 添加 Horizontal Layout Group

2. 子物体 (都是TextMeshPro):
   - **TotalPomodorosText**: `🍅 0`
   - **TotalTimeText**: `0小时0分钟`
   - **StreakText**: `🔥 连续0天`
   - **AverageText**: `日均0分钟`

---

## 7. 创建任务编辑面板TaskEditPanel

### 7.1 创建TaskEditPanel
1. 选中Canvas，右键 → **UI → Panel**
2. 重命名为 `TaskEditPanel`
3. RectTransform: 填满Canvas
4. Image: Color #000000, Alpha 150
5. **默认隐藏**

### 7.2 创建EditContent
1. 选中TaskEditPanel，右键 → **UI → Panel**
2. 重命名为 `EditContent`
3. RectTransform：
   - Anchor: Middle-Center
   - Width: 400, Height: 300
4. Image: Color #FFFFFF

### 7.3 创建编辑内容

1. **EditTitleText** (TextMeshPro): `新建任务`

2. **TaskNameInput** (TMP_InputField):
   - Placeholder: `输入任务名称...`
   - Width: 360, Height: 50

3. **ColorPicker** (Panel, Horizontal Layout Group):
   - 包含6个颜色按钮

**ColorButtons (6个):**
在ColorPicker下创建6个按钮：
```
ColorButton_0: Image Color #E84C3D (红)
ColorButton_1: Image Color #E67E22 (橙)
ColorButton_2: Image Color #F1C40F (黄)
ColorButton_3: Image Color #2ECC71 (绿)
ColorButton_4: Image Color #3498DB (蓝)
ColorButton_5: Image Color #9B59B6 (紫)
```
每个按钮 Width 50, Height 50

4. **ButtonsRow** (Horizontal Layout Group):
   - **DeleteTaskButton**: `删除`, 颜色红色
   - **CancelEditButton**: `取消`
   - **SaveTaskButton**: `保存`, 颜色绿色

---

## 8. 创建预制体Prefabs

### 8.1 TaskItemPrefab（任务项预制体）

1. 选中Canvas，**临时**创建一个Panel
2. 重命名为 `TaskItemPrefab`
3. RectTransform：
   - Width: 自动 (由父级控制)
   - Height: 70
4. 添加 **Layout Element**: Preferred Height 70
5. **添加脚本** `TaskItemUI`
6. Image: Color #FFFFFF

#### 子物体结构：

**ColorBar:**
1. 右键 → UI → Image
2. 重命名为 `ColorBar`
3. RectTransform：
   - Anchor: Left-Stretch (左侧竖向拉伸)
   - Left: 0, Top: 5, Bottom: 5
   - Width: 6
4. Image: Color #E84C3D

**TaskNameText:**
1. 右键 → UI → Text - TextMeshPro
2. 重命名为 `TaskNameText`
3. RectTransform：
   - Anchor: Left-Stretch
   - Left: 20, Right: 200, Top: 10, Bottom: 30
4. TextMeshPro: Font Size 18, Alignment Left

**PomodoroCountText:**
1. 右键 → UI → Text - TextMeshPro
2. 重命名为 `PomodoroCountText`
3. RectTransform：
   - Anchor: Right-Top
   - Right: 80, Top: 10
   - Width: 80, Height: 30
4. Text: `🍅 0`

**TotalTimeText:**
1. 右键 → UI → Text - TextMeshPro
2. 重命名为 `TotalTimeText`
3. RectTransform：
   - Anchor: Right-Bottom
   - Right: 80, Bottom: 10
   - Width: 100, Height: 25
4. Text: `0分钟`, Font Size 14, Color #999999

**SelectButton (覆盖整个项的透明按钮):**
1. 右键 → UI → Button - TextMeshPro
2. 重命名为 `SelectButton`
3. RectTransform: Stretch-Stretch, 全部为0
4. **删除**子物体Text
5. Image: Color Alpha 0 (完全透明)
6. Button: Transition 设为 None

**EditButton:**
1. 右键 → UI → Button - TextMeshPro
2. 重命名为 `EditButton`
3. RectTransform：
   - Anchor: Right-Center
   - Right: 10
   - Width: 60, Height: 40
4. 子物体Text: `编辑`

**SelectedIndicator:**
1. 右键 → UI → Image
2. 重命名为 `SelectedIndicator`
3. RectTransform: Stretch-Stretch, Left 0, Right 0, Top 0, Bottom 0
4. Image: Color #E84C3D, Alpha 20
5. **默认隐藏** (取消勾选)

#### 创建预制体：
1. 将 `TaskItemPrefab` 从 Hierarchy 拖到 `Assets/Prefabs` 文件夹
2. **删除** Hierarchy 中的 TaskItemPrefab (场景中不需要)

---

### 8.2 BarPrefab（柱子预制体）

1. 临时创建 Panel，重命名为 `BarPrefab`
2. RectTransform：
   - Width: 40, Height: 100
   - **Pivot: (0.5, 0)** ← 重要！底部中心
3. Image: Color #E84C3D

**子物体 ValueText:**
1. 右键 → UI → Text - TextMeshPro
2. 重命名为 `ValueText`
3. RectTransform：
   - Anchor: Top-Center
   - Pos Y: 5
   - Width: 60, Height: 25
4. Font Size: 12, Alignment Center

5. 拖到 Prefabs 文件夹，删除场景中的

---

### 8.3 LabelPrefab（X轴标签预制体）

1. 创建 Text - TextMeshPro，重命名为 `LabelPrefab`
2. Width: 60, Height: 40
3. Font Size: 12
4. Alignment: Top-Center
5. 拖到 Prefabs，删除场景中的

---

### 8.4 GridLinePrefab（网格线预制体）

1. 创建 Image，重命名为 `GridLinePrefab`
2. Width: 400, Height: 1
3. Color: #CCCCCC, Alpha 128
4. 拖到 Prefabs，删除场景中的

---

## 9. 连接脚本引用

### 9.1 MainUIController (在MainPanel上)

选中 `MainPanel`，在 Inspector 中找到 `MainUIController` 组件，拖入引用：

| 字段 | 拖入的对象 |
|------|-----------|
| Timer Text | TimerText |
| State Text | StateText |
| Round Text | RoundText |
| Mode Text | ModeText |
| Current Task Text | CurrentTaskText |
| Timer Background | TimerBackground |
| Start Countdown Button | StartCountdownButton |
| Start Countup Button | StartCountupButton |
| Pause Button | PauseButton |
| Resume Button | ResumeButton |
| Stop Button | StopButton |
| Skip Button | SkipButton |
| Settings Button | SettingsButton |
| Statistics Button | StatisticsButton |
| Top Most Toggle | TopMostToggle |
| Settings Panel | SettingsPanel |
| Statistics Panel | StatisticsPanel |
| Task List UI | TaskSection (挂有TaskListUI的对象) |

---

### 9.2 TaskListUI (在TaskSection上)

| 字段 | 拖入的对象 |
|------|-----------|
| Task List Content | TaskListContent |
| Task Item Prefab | Assets/Prefabs/TaskItemPrefab |
| Add Task Button | AddTaskButton |
| Task Edit Panel | TaskEditPanel |
| Task Name Input | TaskEditPanel下的TaskNameInput |
| Color Buttons | 6个ColorButton (按顺序拖入数组) |
| Save Task Button | SaveTaskButton |
| Delete Task Button | DeleteTaskButton |
| Cancel Edit Button | CancelEditButton |
| Main UI | MainPanel |

---

### 9.3 SettingsUI (在SettingsContent上)

| 字段 | 拖入的对象 |
|------|-----------|
| Focus Duration Input | FocusDurationInput |
| Short Break Input | ShortBreakInput |
| Long Break Input | LongBreakInput |
| Rounds Input | RoundsInput |
| Countup Min Input | CountupMinInput |
| Countup Max Input | CountupMaxInput |
| Sound Toggle | SoundToggle |
| Volume Slider | VolumeSlider |
| Auto Start Break Toggle | AutoStartBreakToggle |
| Auto Start Focus Toggle | AutoStartFocusToggle |
| Close Button | CloseButton |
| Save Button | SaveButton |
| Reset Default Button | ResetDefaultButton |
| Clear History Button | ClearHistoryButton |
| Preview Sound Button | PreviewSoundButton (如果有) |

---

### 9.4 StatisticsUI (在StatisticsContent上)

| 字段 | 拖入的对象 |
|------|-----------|
| Daily Tab Button | DailyTabButton |
| Weekly Tab Button | WeeklyTabButton |
| Bar Chart | ChartArea (挂有BarChartUI的对象) |
| Chart Title Text | ChartTitleText (如果有) |
| Total Pomodoros Text | TotalPomodorosText |
| Total Time Text | TotalTimeText |
| Streak Text | StreakText |
| Average Text | AverageText |
| Task Filter Dropdown | TaskFilterDropdown |
| Close Button | CloseButton |

---

### 9.5 BarChartUI (在ChartArea上)

| 字段 | 拖入的对象 |
|------|-----------|
| Chart Container | ChartArea自身 |
| Bars Container | BarsContainer |
| Labels Container | LabelsContainer |
| Grid Container | GridContainer |
| Bar Prefab | Assets/Prefabs/BarPrefab |
| Label Prefab | Assets/Prefabs/LabelPrefab |
| Grid Line Prefab | Assets/Prefabs/GridLinePrefab |
| Y Axis Max Text | YAxisMaxText |
| Y Axis Mid Text | YAxisMidText |
| Y Axis Unit Text | YAxisUnitText |

---

## 10. 最终检查

### 10.1 检查清单

- [ ] GameManager 存在且有 GameManager 脚本
- [ ] Canvas 设置正确 (1920x1080, Scale With Screen Size)
- [ ] MainUIController 所有引用已连接
- [ ] TaskListUI 所有引用已连接
- [ ] SettingsUI 所有引用已连接
- [ ] StatisticsUI 所有引用已连接
- [ ] BarChartUI 预制体引用已连接
- [ ] TaskItemPrefab 已创建并放入 Prefabs 文件夹
- [ ] SettingsPanel 默认隐藏
- [ ] StatisticsPanel 默认隐藏
- [ ] TaskEditPanel 默认隐藏

### 10.2 运行测试

1. 点击 Play 按钮
2. 测试功能：
   - 点击"添加任务"
   - 创建一个任务
   - 选中任务
   - 点击"开始"
   - 点击"暂停"/"继续"
   - 点击"统计"查看图表
   - 点击"设置"修改参数

### 10.3 常见问题

**问题：点击按钮无反应**
- 检查 EventSystem 是否存在
- 检查按钮的 Interactable 是否勾选
- 检查是否有遮挡物体

**问题：脚本报错 NullReference**
- 检查 Inspector 中是否有未连接的引用
- 检查对象名称是否正确

**问题：任务列表不显示**
- 检查 TaskItemPrefab 是否正确创建
- 检查 TaskListContent 的 Layout Group 设置

---

## 🎉 完成！

恭喜！您已完成番茄钟的全部UI搭建。保存场景，运行测试吧！
