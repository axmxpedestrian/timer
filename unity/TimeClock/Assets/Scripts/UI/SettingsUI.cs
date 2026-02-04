using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using PomodoroTimer.Core;
using PomodoroTimer.Data;
using PomodoroTimer.Utils;

// 解决命名空间冲突：为计时器类创建别名
using PomodoroTimerCore = PomodoroTimer.Core.PomodoroTimer;

namespace PomodoroTimer.UI
{
    /// <summary>
    /// 设置界面UI控制器 - 分页版本
    /// 需要在Unity编辑器中手动设置所有引用
    /// </summary>
    public class SettingsUI : MonoBehaviour
    {
        [Header("分页按钮")]
        [SerializeField] private Button gameTabButton;
        [SerializeField] private Button pomodoroTabButton;
        [SerializeField] private Button displayTabButton;
        [SerializeField] private Button soundTabButton;
        [SerializeField] private Button keysTabButton;

        [Header("分页内容")]
        [SerializeField] private GameObject gameTabContent;
        [SerializeField] private GameObject pomodoroTabContent;
        [SerializeField] private GameObject displayTabContent;
        [SerializeField] private GameObject soundTabContent;
        [SerializeField] private GameObject keysTabContent;

        [Header("番茄钟设置 - 时长设置")]
        [SerializeField] private TMP_InputField focusDurationInput;
        [SerializeField] private TMP_InputField shortBreakInput;
        [SerializeField] private TMP_InputField longBreakInput;
        [SerializeField] private TMP_InputField roundsInput;

        [Header("番茄钟设置 - 正计时设置")]
        [SerializeField] private TMP_InputField countupMinInput;
        [SerializeField] private TMP_InputField countupMaxInput;

        [Header("番茄钟设置 - 其他")]
        [SerializeField] private Toggle autoStartBreakToggle;
        [SerializeField] private Toggle autoStartFocusToggle;

        [Header("番茄钟设置 - 按钮")]
        [SerializeField] private Button saveButton;
        [SerializeField] private Button resetDefaultButton;
        [SerializeField] private Button clearHistoryButton;

        [Header("声音设置")]
        [SerializeField] private Toggle soundToggle;
        [SerializeField] private Slider volumeSlider;
        [SerializeField] private Button previewSoundButton;

        [Header("通用按钮")]
        [SerializeField] private Button closeButton;

        [Header("信息显示")]
        [SerializeField] private TextMeshProUGUI versionText;
        [SerializeField] private TextMeshProUGUI saveInfoText;

        private SettingsData tempSettings;
        private static SettingsData pendingTimeSettings; // 静态，确保面板关闭后仍然保留
        private static bool hasUnsavedTimeChanges = false;

        // 当前选中的分页索引
        private int currentTabIndex = 1; // 默认番茄钟分页
        private Button[] tabButtons;
        private GameObject[] tabContents;

        // 分页按钮颜色
        private Color activeTabColor = new Color(0.2f, 0.6f, 0.2f, 1f);
        private Color inactiveTabColor = new Color(0.9f, 0.9f, 0.9f, 1f);

        private void Awake()
        {
            // 订阅计时器停止事件（在 Awake 中订阅，确保即使面板未激活也能接收事件）
            if (PomodoroTimerCore.Instance != null)
            {
                PomodoroTimerCore.Instance.OnStateChanged -= OnTimerStateChanged;
                PomodoroTimerCore.Instance.OnStateChanged += OnTimerStateChanged;
            }
        }

        private void OnEnable()
        {
            LoadCurrentSettings();
            // 默认显示番茄钟分页
            SwitchToTab(1);
        }

        private void Start()
        {
            InitializeTabs();
            BindEvents();

            // 显示版本信息
            if (versionText != null)
            {
                versionText.text = $"版本 {Application.version}";
            }
        }

        private void OnDestroy()
        {
            if (PomodoroTimerCore.Instance != null)
            {
                PomodoroTimerCore.Instance.OnStateChanged -= OnTimerStateChanged;
            }
        }

        /// <summary>
        /// 初始化分页系统
        /// </summary>
        private void InitializeTabs()
        {
            tabButtons = new Button[] { gameTabButton, pomodoroTabButton, displayTabButton, soundTabButton, keysTabButton };
            tabContents = new GameObject[] { gameTabContent, pomodoroTabContent, displayTabContent, soundTabContent, keysTabContent };

            // 绑定分页按钮事件
            if (gameTabButton != null) gameTabButton.onClick.AddListener(() => SwitchToTab(0));
            if (pomodoroTabButton != null) pomodoroTabButton.onClick.AddListener(() => SwitchToTab(1));
            if (displayTabButton != null) displayTabButton.onClick.AddListener(() => SwitchToTab(2));
            if (soundTabButton != null) soundTabButton.onClick.AddListener(() => SwitchToTab(3));
            if (keysTabButton != null) keysTabButton.onClick.AddListener(() => SwitchToTab(4));
        }

        /// <summary>
        /// 切换到指定分页
        /// </summary>
        private void SwitchToTab(int tabIndex)
        {
            AudioManager.Instance?.PlayClick();
            currentTabIndex = tabIndex;

            // 更新分页内容显示
            for (int i = 0; i < tabContents.Length; i++)
            {
                if (tabContents[i] != null)
                {
                    tabContents[i].SetActive(i == tabIndex);
                }
            }

            // 更新分页按钮样式
            UpdateTabButtonStyles();
        }

        /// <summary>
        /// 更新分页按钮样式
        /// </summary>
        private void UpdateTabButtonStyles()
        {
            for (int i = 0; i < tabButtons.Length; i++)
            {
                if (tabButtons[i] != null)
                {
                    // 更新按钮背景颜色
                    var image = tabButtons[i].GetComponent<Image>();
                    if (image != null)
                    {
                        image.color = (i == currentTabIndex) ? activeTabColor : inactiveTabColor;
                    }

                    // 更新按钮文字颜色
                    var buttonText = tabButtons[i].GetComponentInChildren<TextMeshProUGUI>();
                    if (buttonText != null)
                    {
                        buttonText.color = (i == currentTabIndex) ? Color.white : new Color(0.2f, 0.2f, 0.2f, 1f);
                    }
                }
            }
        }

        /// <summary>
        /// 绑定事件
        /// </summary>
        private void BindEvents()
        {
            closeButton?.onClick.AddListener(OnCloseClicked);
            saveButton?.onClick.AddListener(OnSaveClicked);
            resetDefaultButton?.onClick.AddListener(OnResetDefaultClicked);
            clearHistoryButton?.onClick.AddListener(OnClearHistoryClicked);
            previewSoundButton?.onClick.AddListener(OnPreviewSoundClicked);

            // 实时更新音量
            volumeSlider?.onValueChanged.AddListener(OnVolumeChanged);
        }

        /// <summary>
        /// 加载当前设置
        /// </summary>
        private void LoadCurrentSettings()
        {
            var settings = DataManager.Instance?.Settings;
            if (settings == null) return;

            // 创建临时副本
            tempSettings = new SettingsData
            {
                focusDurationMinutes = settings.focusDurationMinutes,
                shortBreakMinutes = settings.shortBreakMinutes,
                longBreakMinutes = settings.longBreakMinutes,
                roundsBeforeLongBreak = settings.roundsBeforeLongBreak,
                countupMinThreshold = settings.countupMinThreshold,
                countupMaxMinutes = settings.countupMaxMinutes,
                soundEnabled = settings.soundEnabled,
                soundVolume = settings.soundVolume,
                autoStartBreak = settings.autoStartBreak,
                autoStartFocus = settings.autoStartFocus
            };

            // 填充UI
            SetInputValue(focusDurationInput, settings.focusDurationMinutes);
            SetInputValue(shortBreakInput, settings.shortBreakMinutes);
            SetInputValue(longBreakInput, settings.longBreakMinutes);
            SetInputValue(roundsInput, settings.roundsBeforeLongBreak);
            SetInputValue(countupMinInput, settings.countupMinThreshold);
            SetInputValue(countupMaxInput, settings.countupMaxMinutes);

            if (soundToggle != null) soundToggle.isOn = settings.soundEnabled;
            if (volumeSlider != null) volumeSlider.value = settings.soundVolume;
            if (autoStartBreakToggle != null) autoStartBreakToggle.isOn = settings.autoStartBreak;
            if (autoStartFocusToggle != null) autoStartFocusToggle.isOn = settings.autoStartFocus;

            // 更新存档信息
            UpdateSaveInfo();
        }

        /// <summary>
        /// 设置输入框值
        /// </summary>
        private void SetInputValue(TMP_InputField input, int value)
        {
            if (input != null)
            {
                input.text = value.ToString();
            }
        }

        /// <summary>
        /// 获取输入框值
        /// </summary>
        private int GetInputValue(TMP_InputField input, int defaultValue, int min = 1, int max = 999)
        {
            if (input != null && int.TryParse(input.text, out int value))
            {
                return Mathf.Clamp(value, min, max);
            }
            return defaultValue;
        }

        /// <summary>
        /// 更新存档信息显示
        /// </summary>
        private void UpdateSaveInfo()
        {
            if (saveInfoText != null && DataManager.Instance != null)
            {
                saveInfoText.text = DataManager.Instance.GetSaveInfo();
            }
        }

        /// <summary>
        /// 计时器状态变化回调
        /// </summary>
        private void OnTimerStateChanged(TimerState newState)
        {
            // 当计时器停止时，应用待生效的时间设置
            if (newState == TimerState.Idle && hasUnsavedTimeChanges && pendingTimeSettings != null)
            {
                ApplyPendingTimeSettings();
            }
        }

        /// <summary>
        /// 应用待生效的时间设置
        /// </summary>
        private void ApplyPendingTimeSettings()
        {
            if (pendingTimeSettings == null) return;

            var currentSettings = DataManager.Instance.Settings;
            currentSettings.focusDurationMinutes = pendingTimeSettings.focusDurationMinutes;
            currentSettings.shortBreakMinutes = pendingTimeSettings.shortBreakMinutes;
            currentSettings.longBreakMinutes = pendingTimeSettings.longBreakMinutes;
            currentSettings.roundsBeforeLongBreak = pendingTimeSettings.roundsBeforeLongBreak;
            currentSettings.countupMinThreshold = pendingTimeSettings.countupMinThreshold;
            currentSettings.countupMaxMinutes = pendingTimeSettings.countupMaxMinutes;

            DataManager.Instance.Save();
            PomodoroTimerCore.Instance?.RefreshDisplay();

            hasUnsavedTimeChanges = false;
            pendingTimeSettings = null;

            Debug.Log("待生效的时间设置已应用");
        }

        /// <summary>
        /// 显示提示信息（使用全局提示系统）
        /// </summary>
        private void ShowHint(string message)
        {
            // 使用全局提示系统（在主界面显示）
            if (MainUIController.Instance != null)
            {
                MainUIController.Instance.ShowGlobalHint(message);
            }
            else
            {
                Debug.LogWarning($"[SettingsUI] 无法显示提示: {message}");
            }
        }

        #region 按钮事件

        private void OnCloseClicked()
        {
            AudioManager.Instance?.PlayClick();
            // 关闭父级面板（SettingPanel），而不是当前内容面板
            if (transform.parent != null)
            {
                transform.parent.gameObject.SetActive(false);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        private void OnSaveClicked()
        {
            AudioManager.Instance?.PlayClick();

            // 读取时间相关的输入值
            int newFocusDuration = GetInputValue(focusDurationInput, 25, 10, 180);
            int newShortBreak = GetInputValue(shortBreakInput, 5, 1, 60);
            int newLongBreak = GetInputValue(longBreakInput, 30, 1, 120);
            int newRounds = GetInputValue(roundsInput, 4, 1, 10);
            int newCountupMin = GetInputValue(countupMinInput, 10, 10, 60);
            int newCountupMax = GetInputValue(countupMaxInput, 120, 30, 180);

            var currentSettings = DataManager.Instance.Settings;

            // 检查时间设置是否有变化
            bool timeSettingsChanged =
                newFocusDuration != currentSettings.focusDurationMinutes ||
                newShortBreak != currentSettings.shortBreakMinutes ||
                newLongBreak != currentSettings.longBreakMinutes ||
                newRounds != currentSettings.roundsBeforeLongBreak ||
                newCountupMin != currentSettings.countupMinThreshold ||
                newCountupMax != currentSettings.countupMaxMinutes;

            // 检查计时器是否正在运行
            bool timerRunning = PomodoroTimerCore.Instance != null &&
                               PomodoroTimerCore.Instance.CurrentState != TimerState.Idle;

            // 如果时间设置有变化且计时器正在运行，则延迟生效
            if (timeSettingsChanged && timerRunning)
            {
                pendingTimeSettings = new SettingsData
                {
                    focusDurationMinutes = newFocusDuration,
                    shortBreakMinutes = newShortBreak,
                    longBreakMinutes = newLongBreak,
                    roundsBeforeLongBreak = newRounds,
                    countupMinThreshold = newCountupMin,
                    countupMaxMinutes = newCountupMax
                };
                hasUnsavedTimeChanges = true;

                ShowHint("时间设置将在计时停止后生效");
            }
            else if (timeSettingsChanged)
            {
                // 计时器未运行，直接应用时间设置
                currentSettings.focusDurationMinutes = newFocusDuration;
                currentSettings.shortBreakMinutes = newShortBreak;
                currentSettings.longBreakMinutes = newLongBreak;
                currentSettings.roundsBeforeLongBreak = newRounds;
                currentSettings.countupMinThreshold = newCountupMin;
                currentSettings.countupMaxMinutes = newCountupMax;
            }

            // 其他设置立即生效
            currentSettings.soundEnabled = soundToggle?.isOn ?? true;
            currentSettings.soundVolume = volumeSlider?.value ?? 0.8f;
            currentSettings.autoStartBreak = autoStartBreakToggle?.isOn ?? false;
            currentSettings.autoStartFocus = autoStartFocusToggle?.isOn ?? false;

            // 保存设置
            DataManager.Instance.Save();

            // 刷新显示（如果没有待生效的时间设置）
            if (!hasUnsavedTimeChanges)
            {
                PomodoroTimerCore.Instance?.RefreshDisplay();
            }

            Debug.Log("设置已保存");

            // 关闭父级面板
            if (transform.parent != null)
            {
                transform.parent.gameObject.SetActive(false);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        private void OnResetDefaultClicked()
        {
            AudioManager.Instance?.PlayClick();

            // 重置为默认值
            SetInputValue(focusDurationInput, 25);
            SetInputValue(shortBreakInput, 5);
            SetInputValue(longBreakInput, 30);
            SetInputValue(roundsInput, 4);
            SetInputValue(countupMinInput, 10);
            SetInputValue(countupMaxInput, 120);

            if (soundToggle != null) soundToggle.isOn = true;
            if (volumeSlider != null) volumeSlider.value = 0.8f;
            if (autoStartBreakToggle != null) autoStartBreakToggle.isOn = false;
            if (autoStartFocusToggle != null) autoStartFocusToggle.isOn = false;
        }

        private void OnClearHistoryClicked()
        {
            Debug.Log("[SettingsUI] 点击清除历史记录按钮");
            AudioManager.Instance?.PlayClick();

            // 检查 HistoryManageUI 是否存在
            if (HistoryManageUI.Instance == null)
            {
                Debug.LogError("[SettingsUI] HistoryManageUI.Instance 为 null！请检查场景中是否已创建 HistoryManageUI 组件。");

                // 显示提示
                ConfirmDialog.Instance?.ShowAlert(
                    "功能未就绪",
                    "历史记录管理界面尚未创建，请先在场景中添加 HistoryManageUI 组件。"
                );
                return;
            }

            // 打开历史记录管理界面
            HistoryManageUI.Instance.Show();
        }

        private void OnPreviewSoundClicked()
        {
            AudioManager.Instance?.PreviewCompleteSound();
        }

        private void OnVolumeChanged(float value)
        {
            AudioManager.Instance?.SetVolume(value);
        }

        #endregion
    }
}
