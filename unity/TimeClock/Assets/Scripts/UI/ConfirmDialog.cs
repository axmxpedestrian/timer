using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Localization;
using PomodoroTimer.Core;
using PomodoroTimer.Utils;
using static PomodoroTimer.Utils.LocalizedText;

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

        [Header("模态设置")]
        [SerializeField] private int dialogSortingOrder = 100;

        private Action onConfirm;
        private Action onCancel;
        private bool showPending = false; // 标记是否有待显示的请求

        // 用于模态显示的 Canvas 和 GraphicRaycaster
        private Canvas dialogCanvas;
        private GraphicRaycaster dialogRaycaster;

        // 记录当前显示参数，用于语言切换时刷新
        private string currentConfirmTextKey;   // 传入的 confirmText 参数（null 表示使用默认）
        private string currentCancelTextKey;    // 传入的 cancelText 参数（null 表示使用默认）
        private bool currentIsDanger;
        private bool isShowing = false;

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

            // 确保 dialogPanel 上有独立的 Canvas + GraphicRaycaster，用于模态排序
            EnsureModalCanvas();

            // 订阅语言切换事件
            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.OnLocaleChanged += OnLocaleChanged;
            }

            // 【修复】如果没有待显示的请求，才隐藏
            if (!showPending)
            {
                dialogPanel?.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.OnLocaleChanged -= OnLocaleChanged;
            }

            if (_instance == this)
            {
                _instance = null;
            }
        }

        /// <summary>
        /// 确保 dialogPanel 上有独立的 Canvas 和 GraphicRaycaster，
        /// 通过 overrideSorting 保证弹窗始终显示在最上层，阻挡其他一切交互
        /// </summary>
        private void EnsureModalCanvas()
        {
            if (dialogPanel == null) return;

            dialogCanvas = dialogPanel.GetComponent<Canvas>();
            if (dialogCanvas == null)
            {
                dialogCanvas = dialogPanel.AddComponent<Canvas>();
            }
            dialogCanvas.overrideSorting = true;
            dialogCanvas.sortingOrder = dialogSortingOrder;

            dialogRaycaster = dialogPanel.GetComponent<GraphicRaycaster>();
            if (dialogRaycaster == null)
            {
                dialogRaycaster = dialogPanel.AddComponent<GraphicRaycaster>();
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
            string confirmText = null,
            string cancelText = null,
            bool isDanger = false)
        {
            Debug.Log($"[ConfirmDialog] 显示弹窗: {title}");

            // 【修复】标记有待显示的请求，防止 Awake 中的 Hide 覆盖
            showPending = true;

            // 记录原始参数，用于语言切换时刷新
            currentConfirmTextKey = confirmText;
            currentCancelTextKey = cancelText;
            currentIsDanger = isDanger;

            // 解析本地化文本
            confirmText = confirmText ?? Get("UI_General", "btn_confirm");
            cancelText = cancelText ?? Get("UI_General", "btn_cancel");

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

                // 确保弹窗在 UI 层级中最前面
                dialogPanel.transform.SetAsLastSibling();

                Debug.Log("[ConfirmDialog] dialogPanel 已激活");
            }
            else
            {
                Debug.LogError("[ConfirmDialog] dialogPanel 为 null，无法显示弹窗！");
            }

            isShowing = true;
            showPending = false;
        }

        /// <summary>
        /// 显示简单确认弹窗（快捷方法）
        /// </summary>
        public void ShowConfirm(string message, Action onConfirmCallback, bool isDanger = true)
        {
            Show(Get("UI_General", "dialog_confirm_action"), message, onConfirmCallback, null, null, null, isDanger);
        }

        /// <summary>
        /// 显示提示弹窗（只有一个按钮）
        /// </summary>
        public void ShowAlert(string title, string message, string buttonText = null)
        {
            Show(title, message, null, null, buttonText ?? Get("UI_General", "btn_ok"), "", false);
        }

        /// <summary>
        /// 显示删除确认弹窗（快捷方法）
        /// </summary>
        public void ShowDelete(string itemName, Action onConfirmCallback)
        {
            Show(
                Get("UI_General", "dialog_confirm_delete"),
                GetSmart("UI_General", "dialog_delete_message", ("itemName", itemName)),
                onConfirmCallback,
                null,
                Get("UI_General", "btn_delete"),
                null,
                true
            );
        }

        /// <summary>
        /// 隐藏弹窗
        /// </summary>
        public void Hide()
        {
            dialogPanel?.SetActive(false);
            isShowing = false;

            // 恢复取消按钮显示（为下次使用做准备）
            if (cancelButton != null)
            {
                cancelButton.gameObject.SetActive(true);
            }

            onConfirm = null;
            onCancel = null;
        }

        /// <summary>
        /// 语言切换回调 - 刷新按钮文本
        /// </summary>
        private void OnLocaleChanged(Locale newLocale)
        {
            if (!isShowing) return;

            // 重新解析默认按钮文本（null 参数使用默认本地化键）
            string confirmText = currentConfirmTextKey ?? Get("UI_General", "btn_confirm");
            string cancelText = currentCancelTextKey ?? Get("UI_General", "btn_cancel");

            if (confirmButtonText != null) confirmButtonText.text = confirmText;
            if (cancelButtonText != null) cancelButtonText.text = cancelText;
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
