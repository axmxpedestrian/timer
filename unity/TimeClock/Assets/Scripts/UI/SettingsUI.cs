using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PomodoroTimer.Core;
using PomodoroTimer.Data;
using PomodoroTimer.Utils;

// 解决命名空间冲突：为计时器类创建别名
using PomodoroTimerCore = PomodoroTimer.Core.PomodoroTimer;

namespace PomodoroTimer.UI
{
    /// <summary>
    /// 设置界面UI控制器
    /// </summary>
    public class SettingsUI : MonoBehaviour
    {
        [Header("时长设置")]
        [SerializeField] private TMP_InputField focusDurationInput;
        [SerializeField] private TMP_InputField shortBreakInput;
        [SerializeField] private TMP_InputField longBreakInput;
        [SerializeField] private TMP_InputField roundsInput;
        
        [Header("正计时设置")]
        [SerializeField] private TMP_InputField countupMinInput;
        [SerializeField] private TMP_InputField countupMaxInput;
        
        [Header("其他设置")]
        [SerializeField] private Toggle soundToggle;
        [SerializeField] private Slider volumeSlider;
        [SerializeField] private Toggle autoStartBreakToggle;
        [SerializeField] private Toggle autoStartFocusToggle;
        
        [Header("按钮")]
        [SerializeField] private Button closeButton;
        [SerializeField] private Button saveButton;
        [SerializeField] private Button resetDefaultButton;
        [SerializeField] private Button clearHistoryButton;
        [SerializeField] private Button previewSoundButton;
        
        [Header("信息显示")]
        [SerializeField] private TextMeshProUGUI versionText;
        [SerializeField] private TextMeshProUGUI saveInfoText;
        
        private SettingsData tempSettings;
        
        private void OnEnable()
        {
            LoadCurrentSettings();
        }
        
        private void Start()
        {
            BindEvents();
            
            // 显示版本信息
            if (versionText != null)
            {
                versionText.text = $"版本 {Application.version}";
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
            
            // 读取输入值
            tempSettings.focusDurationMinutes = GetInputValue(focusDurationInput, 25, 10, 180);
            tempSettings.shortBreakMinutes = GetInputValue(shortBreakInput, 5, 1, 60);
            tempSettings.longBreakMinutes = GetInputValue(longBreakInput, 30, 1, 120);
            tempSettings.roundsBeforeLongBreak = GetInputValue(roundsInput, 4, 1, 10);
            tempSettings.countupMinThreshold = GetInputValue(countupMinInput, 10, 10, 60);
            tempSettings.countupMaxMinutes = GetInputValue(countupMaxInput, 120, 30, 180);
            
            tempSettings.soundEnabled = soundToggle?.isOn ?? true;
            tempSettings.soundVolume = volumeSlider?.value ?? 0.8f;
            tempSettings.autoStartBreak = autoStartBreakToggle?.isOn ?? false;
            tempSettings.autoStartFocus = autoStartFocusToggle?.isOn ?? false;
            
            // 保存设置
            DataManager.Instance.UpdateSettings(tempSettings);
            
            // 【修复】通知计时器刷新显示
            PomodoroTimerCore.Instance?.RefreshDisplay();
            
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
