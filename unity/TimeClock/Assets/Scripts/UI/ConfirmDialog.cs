using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PomodoroTimer.Utils;

namespace PomodoroTimer.UI
{
    /// <summary>
    /// 通用确认弹窗组件
    /// </summary>
    public class ConfirmDialog : MonoBehaviour
    {
        private static ConfirmDialog _instance;
        public static ConfirmDialog Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<ConfirmDialog>(true);
                    
                    if (_instance == null)
                    {
                        Debug.LogError("[ConfirmDialog] 场景中找不到 ConfirmDialog 组件！请确保已创建并正确设置。");
                    }
                }
                return _instance;
            }
        }
        
        [Header("UI元素")]
        [SerializeField] private GameObject dialogPanel;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private TextMeshProUGUI confirmButtonText;
        [SerializeField] private TextMeshProUGUI cancelButtonText;
        
        [Header("按钮颜色")]
        [SerializeField] private Color normalButtonColor = new Color(0.3f, 0.3f, 0.3f);
        [SerializeField] private Color dangerButtonColor = new Color(0.8f, 0.2f, 0.2f);
        
        private Action onConfirm;
        private Action onCancel;
        private bool showPending = false; // 标记是否有待显示的请求
        
        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                Debug.Log("[ConfirmDialog] 初始化成功");
            }
            else if (_instance != this)
            {
                Debug.LogWarning("[ConfirmDialog] 场景中存在多个 ConfirmDialog，销毁重复的");
                Destroy(gameObject);
                return;
            }
            
            // 绑定按钮事件
            if (confirmButton != null)
            {
                confirmButton.onClick.AddListener(OnConfirmClicked);
            }
            else
            {
                Debug.LogError("[ConfirmDialog] confirmButton 未设置！");
            }
            
            if (cancelButton != null)
            {
                cancelButton.onClick.AddListener(OnCancelClicked);
            }
            else
            {
                Debug.LogError("[ConfirmDialog] cancelButton 未设置！");
            }
            
            if (dialogPanel == null)
            {
                Debug.LogError("[ConfirmDialog] dialogPanel 未设置！");
            }
            
            // 【修复】如果没有待显示的请求，才隐藏
            if (!showPending)
            {
                dialogPanel?.SetActive(false);
            }
        }
        
        /// <summary>
        /// 显示确认弹窗
        /// </summary>
        public void Show(
            string title,
            string message,
            Action onConfirmCallback,
            Action onCancelCallback = null,
            string confirmText = "确认",
            string cancelText = "取消",
            bool isDanger = false)
        {
            Debug.Log($"[ConfirmDialog] 显示弹窗: {title}");
            
            // 【修复】标记有待显示的请求，防止 Awake 中的 Hide 覆盖
            showPending = true;
            
            onConfirm = onConfirmCallback;
            onCancel = onCancelCallback;
            
            if (titleText != null) titleText.text = title;
            if (messageText != null) messageText.text = message;
            if (confirmButtonText != null) confirmButtonText.text = confirmText;
            if (cancelButtonText != null) cancelButtonText.text = cancelText;
            
            // 设置确认按钮颜色
            var confirmImage = confirmButton?.GetComponent<Image>();
            if (confirmImage != null)
            {
                confirmImage.color = isDanger ? dangerButtonColor : normalButtonColor;
            }
            
            // 【新增】如果取消按钮文字为空，隐藏取消按钮
            if (cancelButton != null)
            {
                cancelButton.gameObject.SetActive(!string.IsNullOrEmpty(cancelText));
            }
            
            if (dialogPanel != null)
            {
                dialogPanel.SetActive(true);
                Debug.Log("[ConfirmDialog] dialogPanel 已激活");
            }
            else
            {
                Debug.LogError("[ConfirmDialog] dialogPanel 为 null，无法显示弹窗！");
            }
            
            showPending = false;
        }
        
        /// <summary>
        /// 显示简单确认弹窗（快捷方法）
        /// </summary>
        public void ShowConfirm(string message, Action onConfirmCallback, bool isDanger = true)
        {
            Show("确认操作", message, onConfirmCallback, null, "确认", "取消", isDanger);
        }
        
        /// <summary>
        /// 显示提示弹窗（只有一个按钮）
        /// </summary>
        public void ShowAlert(string title, string message, string buttonText = "知道了")
        {
            Show(title, message, null, null, buttonText, "", false);
        }
        
        /// <summary>
        /// 显示删除确认弹窗（快捷方法）
        /// </summary>
        public void ShowDelete(string itemName, Action onConfirmCallback)
        {
            Show(
                "确认删除",
                $"确定要删除「{itemName}」吗？\n此操作无法撤销。",
                onConfirmCallback,
                null,
                "删除",
                "取消",
                true
            );
        }
        
        /// <summary>
        /// 隐藏弹窗
        /// </summary>
        public void Hide()
        {
            dialogPanel?.SetActive(false);
            
            // 恢复取消按钮显示（为下次使用做准备）
            if (cancelButton != null)
            {
                cancelButton.gameObject.SetActive(true);
            }
            
            onConfirm = null;
            onCancel = null;
        }
        
        private void OnConfirmClicked()
        {
            Debug.Log("[ConfirmDialog] 点击确认");
            AudioManager.Instance?.PlayClick();
            var callback = onConfirm;
            Hide();
            callback?.Invoke();
        }
        
        private void OnCancelClicked()
        {
            Debug.Log("[ConfirmDialog] 点击取消");
            AudioManager.Instance?.PlayClick();
            var callback = onCancel;
            Hide();
            callback?.Invoke();
        }
    }
}
