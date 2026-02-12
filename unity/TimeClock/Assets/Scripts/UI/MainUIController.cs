using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using PomodoroTimer.Core;
using PomodoroTimer.Data;
using PomodoroTimer.Utils;
using PomodoroTimer.Resource;

// 解决命名空间冲突：为计时器类创建别名
using PomodoroTimerCore = PomodoroTimer.Core.PomodoroTimer;

namespace PomodoroTimer.UI
{
    /// <summary>
    /// 主界面UI控制器
    /// </summary>
    public class MainUIController : MonoBehaviour
    {
        public static MainUIController Instance { get; private set; }

        [Header("计时器显示")]
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private TextMeshProUGUI stateText;
        [SerializeField] private TextMeshProUGUI roundText;
        [SerializeField] private TextMeshProUGUI modeText;
        [SerializeField] private TextMeshProUGUI currentTaskText;
        [SerializeField] private Image timerBackground;

        [Header("代币显示")]
        [SerializeField] private TextMeshProUGUI coinText;
        [SerializeField] private GameObject coinEarnedPopup;      // 获得代币的弹窗（可选）
        [SerializeField] private TextMeshProUGUI coinEarnedText;  // 弹窗文本（可选）

        [Header("控制按钮")]
        [SerializeField] private Button startCountdownButton;
        [SerializeField] private Button startCountupButton;
        [SerializeField] private Button pauseButton;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button stopButton;
        [SerializeField] private Button skipButton;

        [Header("导航按钮")]
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button statisticsButton;

        [Header("一键隐藏UI")]
        [SerializeField] private Button hideUIButton;
        [SerializeField] private Image hideUIButtonImage;
        [SerializeField] private Sprite showUISprite;      // 显示UI时的图标
        [SerializeField] private Sprite hideUISprite;      // 隐藏UI时的图标
        [SerializeField] private GameObject header;
        [SerializeField] private GameObject timerSection;
        [SerializeField] private GameObject controlButtons;
        [SerializeField] private GameObject taskSection;
        [SerializeField] private GameObject coinDisplay;

        [Header("面板引用")]
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject statisticsPanel;
        [SerializeField] private TaskListUI taskListUI;

        [Header("可拖动面板")]
        [SerializeField] private DraggablePanel timerDraggablePanel;  // TimerBackground上的拖动组件

        [Header("全局提示")]
        [SerializeField] private GameObject globalHintContainer;
        [SerializeField] private TextMeshProUGUI globalHintText;
        [SerializeField] private CanvasGroup globalHintCanvasGroup;

        private PomodoroTimerCore timer;
        private bool isInitialized = false;
        private int lastDisplayedCoins = 0;
        private Coroutine hintCoroutine;

        // 隐藏UI状态：0=全部显示, 1=部分隐藏(只显示时间), 2=全部隐藏
        private int hideUIState = 0;
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            // 当UI完全隐藏时，检测鼠标点击以恢复显示
            if (hideUIState >= 2 && Input.GetMouseButtonDown(0))
            {
                ShowAllUI();
            }
        }

        private void Start()
        {
            // 初始化全局提示
            InitializeGlobalHint();

            // 重置可拖动面板到默认位置
            ResetDraggablePanels();

            // 延迟初始化以确保计时器已创建
            StartCoroutine(DelayedInitialize());
        }

        /// <summary>
        /// 重置所有可拖动面板到默认位置
        /// </summary>
        private void ResetDraggablePanels()
        {
            if (timerDraggablePanel != null)
            {
                timerDraggablePanel.ResetToDefaultPosition();
            }

            if (taskListUI != null)
            {
                taskListUI.ResetToDefaultPosition();
            }
        }
        
        private System.Collections.IEnumerator DelayedInitialize()
        {
            // 等待计时器实例创建
            while (PomodoroTimerCore.Instance == null)
            {
                yield return null;
            }

            // 等待StatisticsManager实例创建
            while (StatisticsManager.Instance == null)
            {
                yield return null;
            }

            timer = PomodoroTimerCore.Instance;

            // 绑定按钮事件
            BindButtonEvents();

            // 订阅计时器事件
            SubscribeTimerEvents();

            // 订阅数据加载完成事件，确保代币显示正确
            if (DataManager.Instance != null)
            {
                DataManager.Instance.OnDataLoaded += OnDataLoaded;
            }

            // 初始化UI状态
            UpdateUIState();
            UpdateCoinDisplay();

            // 检查是否有未完成的会话
            if (DataManager.Instance != null && DataManager.Instance.HasActiveSession())
            {
                timer.RestoreSession();
            }

            isInitialized = true;
        }

        private void OnDataLoaded()
        {
            // 数据加载完成后刷新代币显示
            UpdateCoinDisplay();
        }
        
        private void OnDestroy()
        {
            UnsubscribeTimerEvents();

            // 取消订阅数据加载事件
            if (DataManager.Instance != null)
            {
                DataManager.Instance.OnDataLoaded -= OnDataLoaded;
            }
        }
        
        /// <summary>
        /// 绑定按钮事件
        /// </summary>
        private void BindButtonEvents()
        {
            startCountdownButton?.onClick.AddListener(OnStartCountdownClicked);
            startCountupButton?.onClick.AddListener(OnStartCountupClicked);
            pauseButton?.onClick.AddListener(OnPauseClicked);
            resumeButton?.onClick.AddListener(OnResumeClicked);
            stopButton?.onClick.AddListener(OnStopClicked);
            skipButton?.onClick.AddListener(OnSkipClicked);

            settingsButton?.onClick.AddListener(OnSettingsClicked);
            statisticsButton?.onClick.AddListener(OnStatisticsClicked);

            hideUIButton?.onClick.AddListener(OnHideUIClicked);
        }
        
        /// <summary>
        /// 订阅计时器事件
        /// </summary>
        private void SubscribeTimerEvents()
        {
            if (timer == null) return;
            
            timer.OnTimerTick += OnTimerTick;
            timer.OnStateChanged += OnStateChanged;
            timer.OnTypeChanged += OnTypeChanged;
            timer.OnRoundChanged += OnRoundChanged;
            timer.OnPomodoroCompleted += OnPomodoroCompleted;
            timer.OnCountupTimeout += OnCountupTimeout;
        }
        
        /// <summary>
        /// 取消订阅事件
        /// </summary>
        private void UnsubscribeTimerEvents()
        {
            if (timer == null) return;
            
            timer.OnTimerTick -= OnTimerTick;
            timer.OnStateChanged -= OnStateChanged;
            timer.OnTypeChanged -= OnTypeChanged;
            timer.OnRoundChanged -= OnRoundChanged;
            timer.OnPomodoroCompleted -= OnPomodoroCompleted;
            timer.OnCountupTimeout -= OnCountupTimeout;
        }
        
        #region 按钮事件处理
        
        private void OnStartCountdownClicked()
        {
            AudioManager.Instance?.PlayClick();
            timer?.StartCountdown();
        }
        
        private void OnStartCountupClicked()
        {
            AudioManager.Instance?.PlayClick();
            timer?.StartCountup();
        }
        
        private void OnPauseClicked()
        {
            AudioManager.Instance?.PlayClick();
            timer?.Pause();
        }
        
        private void OnResumeClicked()
        {
            AudioManager.Instance?.PlayClick();
            timer?.Resume();
        }
        
        private void OnStopClicked()
        {
            AudioManager.Instance?.PlayClick();
            timer?.Stop();
        }
        
        private void OnSkipClicked()
        {
            AudioManager.Instance?.PlayClick();
            timer?.Skip();
        }
        
        private void OnSettingsClicked()
        {
            AudioManager.Instance?.PlayClick();
            settingsPanel?.SetActive(true);
        }
        
        private void OnStatisticsClicked()
        {
            AudioManager.Instance?.PlayClick();
            statisticsPanel?.SetActive(true);
        }

        private void OnHideUIClicked()
        {
            AudioManager.Instance?.PlayClick();
            hideUIState++;

            if (hideUIState == 1)
            {
                // 第一次点击：隐藏除TimerText以外的UI
                SetUIVisibility(false, true);
                UpdateHideUIButtonIcon(true);
            }
            else if (hideUIState >= 2)
            {
                // 第二次点击：隐藏所有UI（包括按钮本身）
                SetUIVisibility(false, false);
                hideUIButton?.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 设置UI可见性
        /// </summary>
        /// <param name="visible">是否可见</param>
        /// <param name="keepTimerText">是否保留计时器文本</param>
        private void SetUIVisibility(bool visible, bool keepTimerText)
        {
            header?.SetActive(visible);
            controlButtons?.SetActive(visible);
            taskSection?.SetActive(visible);
            coinDisplay?.SetActive(visible);

            if (timerSection != null)
            {
                if (visible)
                {
                    // 显示所有
                    timerSection.SetActive(true);
                    SetTimerSectionChildrenVisibility(true);
                }
                else if (keepTimerText)
                {
                    // 只显示TimerText
                    timerSection.SetActive(true);
                    SetTimerSectionChildrenVisibility(false);
                }
                else
                {
                    // 全部隐藏
                    timerSection.SetActive(false);
                }
            }
        }

        /// <summary>
        /// 设置TimerSection子对象可见性（除TimerText外）
        /// </summary>
        private void SetTimerSectionChildrenVisibility(bool visible)
        {
            if (timerSection == null) return;

            foreach (Transform child in timerSection.transform)
            {
                // TimerText保持显示
                if (child.name == "TimerText" || child.GetComponent<TextMeshProUGUI>() == timerText)
                {
                    child.gameObject.SetActive(true);
                }
                else
                {
                    child.gameObject.SetActive(visible);
                }
            }
        }

        /// <summary>
        /// 更新隐藏按钮图标
        /// </summary>
        private void UpdateHideUIButtonIcon(bool isHidden)
        {
            if (hideUIButtonImage != null)
            {
                hideUIButtonImage.sprite = isHidden ? showUISprite : hideUISprite;
            }
        }

        /// <summary>
        /// 显示所有UI（鼠标点击时调用）
        /// </summary>
        public void ShowAllUI()
        {
            if (hideUIState == 0) return;

            hideUIState = 0;
            SetUIVisibility(true, true);
            hideUIButton?.gameObject.SetActive(true);
            UpdateHideUIButtonIcon(false);
        }

        /// <summary>
        /// 进入建造/销毁模式时隐藏干扰面板并关闭其 Raycast
        /// </summary>
        public void EnterBuildMode()
        {
            SetBuildModePanels(false);
        }

        /// <summary>
        /// 退出建造/销毁模式时恢复面板
        /// </summary>
        public void ExitBuildMode()
        {
            // 仅在非手动隐藏状态下恢复
            if (hideUIState == 0)
            {
                SetBuildModePanels(true);
            }
        }

        /// <summary>
        /// 设置建造模式下需要隐藏的面板（TaskSection、ControlButtons、TimerSection）
        /// </summary>
        private void SetBuildModePanels(bool visible)
        {
            taskSection?.SetActive(visible);
            controlButtons?.SetActive(visible);

            if (timerSection != null)
            {
                // 隐藏时只保留 TimerText，与 hideUIState=1 行为一致
                if (visible)
                {
                    timerSection.SetActive(true);
                    SetTimerSectionChildrenVisibility(true);
                }
                else
                {
                    timerSection.SetActive(true);
                    SetTimerSectionChildrenVisibility(false);
                }
            }

            // 关闭/恢复这些面板上的 Raycast Target
            SetPanelRaycast(taskSection, visible);
            SetPanelRaycast(controlButtons, visible);
            SetPanelRaycast(timerSection, visible);
        }

        /// <summary>
        /// 递归设置 GameObject 下所有 Graphic 组件的 raycastTarget
        /// </summary>
        private static void SetPanelRaycast(GameObject panel, bool enabled)
        {
            if (panel == null) return;
            var graphics = panel.GetComponentsInChildren<Graphic>(true);
            foreach (var g in graphics)
            {
                g.raycastTarget = enabled;
            }
        }

        #endregion
        
        #region 计时器事件处理
        
        private void OnTimerTick(float current, float target)
        {
            UpdateTimerDisplay();
        }
        
        private void OnStateChanged(TimerState state)
        {
            UpdateUIState();
        }
        
        private void OnTypeChanged(PomodoroType type)
        {
            UpdateStateDisplay();
            UpdateTimerBackground();
            UpdateTimerDisplay(); // 也更新时间显示
        }
        
        private void OnRoundChanged(int round)
        {
            UpdateRoundDisplay();
        }
        
        private void OnPomodoroCompleted(PomodoroRecord record)
        {
            // 计算获得的代币（使用CoinCalculator）
            float minutes = record.durationSeconds / 60f;
            int earnedCoins = CoinCalculator.CalculateCoins(minutes);

            Debug.Log($"番茄钟完成: {record.GetFormattedDuration()}, 获得 {earnedCoins} 代币");

            // 更新代币显示
            UpdateCoinDisplay();

            // 显示获得代币的弹窗
            if (earnedCoins > 0)
            {
                ShowCoinEarnedPopup(earnedCoins);
            }
        }
        
        private void OnCountupTimeout()
        {
            Debug.Log("正计时超时，自动中断");
        }
        
        /// <summary>
        /// 显示获得代币的弹窗
        /// </summary>
        private void ShowCoinEarnedPopup(int coins)
        {
            if (coinEarnedPopup != null && coinEarnedText != null)
            {
                coinEarnedText.text = $"+{coins}";
                coinEarnedPopup.SetActive(true);
                
                // 2秒后自动隐藏
                StartCoroutine(HideCoinPopupAfterDelay(2f));
            }
        }
        
        private System.Collections.IEnumerator HideCoinPopupAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            coinEarnedPopup?.SetActive(false);
        }

        /// <summary>
        /// 更新代币显示
        /// </summary>
        private void UpdateCoinDisplay()
        {
            if (coinText == null || ResourceManager.Instance == null) return;
            long currentCoins = ResourceManager.Instance.GetAmount(ResourceType.Coin);
            coinText.text = $"🪙 {ResourceDefinition.FormatAmount(currentCoins)}";
            lastDisplayedCoins = (int)currentCoins;
        }

        #endregion

        #region 全局提示系统

        /// <summary>
        /// 初始化全局提示
        /// </summary>
        private void InitializeGlobalHint()
        {
            if (globalHintCanvasGroup != null)
            {
                globalHintCanvasGroup.alpha = 0;
            }

            if (globalHintContainer != null)
            {
                globalHintContainer.SetActive(false);
            }
        }

        /// <summary>
        /// 显示全局提示信息（3秒后淡出）
        /// </summary>
        public void ShowGlobalHint(string message)
        {
            if (globalHintText == null || globalHintCanvasGroup == null || globalHintContainer == null)
            {
                Debug.LogWarning("[MainUIController] 全局提示组件未设置，请在Inspector中绑定GlobalHintContainer、GlobalHintText和GlobalHintCanvasGroup");
                return;
            }

            if (hintCoroutine != null)
            {
                StopCoroutine(hintCoroutine);
            }

            hintCoroutine = StartCoroutine(ShowGlobalHintCoroutine(message));
        }

        private IEnumerator ShowGlobalHintCoroutine(string message)
        {
            globalHintContainer.SetActive(true);
            globalHintText.text = message;
            globalHintCanvasGroup.alpha = 1f;

            // 显示3秒
            yield return new WaitForSeconds(3f);

            // 淡出效果（0.5秒）
            float fadeTime = 0.5f;
            float elapsed = 0f;

            while (elapsed < fadeTime)
            {
                elapsed += Time.deltaTime;
                globalHintCanvasGroup.alpha = 1f - (elapsed / fadeTime);
                yield return null;
            }

            globalHintCanvasGroup.alpha = 0f;
            globalHintContainer.SetActive(false);
            hintCoroutine = null;
        }

        #endregion
        
        #region UI更新方法
        
        /// <summary>
        /// 更新整体UI状态
        /// </summary>
        private void UpdateUIState()
        {
            if (timer == null) return;

            var state = timer.CurrentState;
            var mode = timer.CurrentMode;

            // 更新按钮可见性
            bool isIdle = state == TimerState.Idle;
            bool isRunning = state == TimerState.Running;
            bool isPaused = state == TimerState.Paused;
            bool isCountdown = mode == TimerMode.Countdown;

            startCountdownButton?.gameObject.SetActive(isIdle);
            startCountupButton?.gameObject.SetActive(isIdle);
            pauseButton?.gameObject.SetActive(isRunning);
            resumeButton?.gameObject.SetActive(isPaused);
            stopButton?.gameObject.SetActive(!isIdle);
            // 跳过按钮只在倒计时模式下显示（正计时模式不显示）
            skipButton?.gameObject.SetActive(!isIdle && isCountdown);

            // 更新显示
            UpdateTimerDisplay();
            UpdateStateDisplay();
            UpdateRoundDisplay();
            UpdateCurrentTaskDisplay();
            UpdateTimerBackground();
        }
        
        /// <summary>
        /// 更新计时器显示
        /// </summary>
        private void UpdateTimerDisplay()
        {
            if (timer == null) return;
            
            if (timerText != null)
            {
                timerText.text = timer.GetDisplayTime();
            }
            
            if (modeText != null)
            {
                modeText.text = timer.GetModeText();
            }
        }
        
        /// <summary>
        /// 更新状态显示
        /// </summary>
        private void UpdateStateDisplay()
        {
            if (timer == null) return;
            
            if (stateText != null)
            {
                string statusText = timer.GetStateText();
                if (timer.CurrentState == TimerState.Paused)
                {
                    statusText += " (已暂停)";
                }
                else if (timer.CurrentState == TimerState.Idle)
                {
                    // 空闲状态时显示准备状态
                    switch (timer.CurrentType)
                    {
                        case PomodoroType.Focus:
                            statusText = "准备专注";
                            break;
                        case PomodoroType.ShortBreak:
                            statusText = "准备短休息";
                            break;
                        case PomodoroType.LongBreak:
                            statusText = "准备长休息";
                            break;
                    }
                }
                stateText.text = statusText;
            }
        }
        
        /// <summary>
        /// 更新轮次显示
        /// </summary>
        private void UpdateRoundDisplay()
        {
            if (timer == null) return;
            
            if (roundText != null)
            {
                if (timer.CurrentMode == TimerMode.Countup)
                {
                    roundText.text = "";
                }
                else
                {
                    roundText.text = $"第 {timer.CurrentRound} 轮 / 共 {timer.TotalRounds} 轮";
                }
            }
        }
        
        /// <summary>
        /// 更新当前任务显示
        /// </summary>
        private void UpdateCurrentTaskDisplay()
        {
            if (timer == null) return;
            
            if (currentTaskText != null)
            {
                var task = timer.CurrentTask;
                if (task != null)
                {
                    currentTaskText.text = $"当前任务: {task.taskName}";
                    currentTaskText.color = ColorPalette.GetTaskColor(task.colorIndex);
                }
                else
                {
                    currentTaskText.text = "请选择任务";
                    currentTaskText.color = ColorPalette.Theme.TextSecondary;
                }
            }
        }
        
        /// <summary>
        /// 更新计时器背景颜色
        /// </summary>
        private void UpdateTimerBackground()
        {
            if (timer == null || timerBackground == null) return;
            
            Color targetColor;
            
            switch (timer.CurrentState)
            {
                case TimerState.Paused:
                    targetColor = ColorPalette.Theme.PausedColor;
                    break;
                case TimerState.Running:
                case TimerState.Idle:
                default:
                    targetColor = timer.CurrentType == PomodoroType.Focus
                        ? ColorPalette.Theme.FocusColor
                        : ColorPalette.Theme.BreakColor;
                    break;
            }
            
            timerBackground.color = ColorPalette.GetTransparent(targetColor, 0.15f);
        }
        
        /// <summary>
        /// 从任务列表选择任务
        /// </summary>
        public void OnTaskSelected(TaskData task)
        {
            timer?.BindTask(task);
            UpdateCurrentTaskDisplay();
        }
        
        /// <summary>
        /// 取消选择任务
        /// </summary>
        public void OnTaskDeselected()
        {
            timer?.BindTask(null);
            UpdateCurrentTaskDisplay();
        }
        
        #endregion
    }
}
