using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using PomodoroTimer.Resource;

namespace PomodoroTimer.UI.Building
{
    /// <summary>
    /// 销毁面板UI控制器
    /// 管理销毁按钮、屏幕边框警告、确认按钮、确认对话框
    /// </summary>
    public class DemolishPanelUI : MonoBehaviour
    {
        public static DemolishPanelUI Instance { get; private set; }

        [Header("销毁按钮（左下角）")]
        [SerializeField] private Button demolishToggleButton;
        [SerializeField] private Image demolishButtonImage;
        [SerializeField] private Color buttonNormalColor = new Color(0.3f, 0.3f, 0.3f);
        [SerializeField] private Color buttonActiveColor = new Color(0.8f, 0.6f, 0.1f);

        [Header("屏幕边框警告")]
        [SerializeField] private GameObject borderWarningRoot;
        [SerializeField] private Image borderTop;
        [SerializeField] private Image borderBottom;
        [SerializeField] private Image borderLeft;
        [SerializeField] private Image borderRight;
        [SerializeField] private Color borderWarningColor = new Color(1f, 0.85f, 0f, 0.6f);
        [SerializeField] private float borderThickness = 6f;
        [SerializeField] private float borderPulseSpeed = 2f;

        [Header("确认按钮（右下角）")]
        [SerializeField] private GameObject confirmButtonRoot;
        [SerializeField] private Button confirmButton;
        [SerializeField] private TextMeshProUGUI confirmButtonText;

        [Header("提示文本")]
        [SerializeField] private TextMeshProUGUI hintText;

        // 状态
        private bool isDemolishActive = false;
        private float pulseTimer = 0f;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        private void Start()
        {
            // 绑定销毁按钮
            if (demolishToggleButton != null)
            {
                demolishToggleButton.onClick.AddListener(OnDemolishToggleClicked);
            }

            // 绑定确认按钮
            if (confirmButton != null)
            {
                confirmButton.onClick.AddListener(OnConfirmClicked);
            }

            // 监听 DemolishController 事件
            StartCoroutine(BindControllerEvents());

            // 初始隐藏
            SetBorderWarningActive(false);
            SetConfirmButtonActive(false);
            UpdateHintText("");
        }

        private System.Collections.IEnumerator BindControllerEvents()
        {
            // 等待 DemolishController 就绪
            while (DemolishController.Instance == null)
            {
                yield return null;
            }

            DemolishController.Instance.OnDemolishModeChanged += OnDemolishModeChanged;
            DemolishController.Instance.OnSelectionChanged += OnSelectionChanged;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;

            if (DemolishController.Instance != null)
            {
                DemolishController.Instance.OnDemolishModeChanged -= OnDemolishModeChanged;
                DemolishController.Instance.OnSelectionChanged -= OnSelectionChanged;
            }
        }

        private void Update()
        {
            if (!isDemolishActive) return;

            // 边框呼吸动画
            UpdateBorderPulse();
        }

        #region 按钮事件

        /// <summary>
        /// 销毁按钮点击
        /// </summary>
        private void OnDemolishToggleClicked()
        {
            if (DemolishController.Instance == null) return;

            DemolishController.Instance.ToggleDemolishMode();
        }

        /// <summary>
        /// 确认按钮点击 - 弹出确认对话框
        /// </summary>
        private void OnConfirmClicked()
        {
            var controller = DemolishController.Instance;
            if (controller == null || controller.SelectedCount == 0) return;

            // 构建确认信息
            string message = BuildConfirmMessage(controller);

            // 使用现有的 ConfirmDialog
            var dialog = ConfirmDialog.Instance;
            if (dialog != null)
            {
                dialog.Show(
                    "确认销毁",
                    message,
                    OnDemolishConfirmed,
                    null,
                    "确认销毁",
                    "取消",
                    true
                );
            }
            else
            {
                // 没有对话框组件，直接执行
                OnDemolishConfirmed();
            }
        }

        /// <summary>
        /// 确认销毁回调
        /// </summary>
        private void OnDemolishConfirmed()
        {
            var controller = DemolishController.Instance;
            if (controller == null) return;

            controller.ConfirmDemolish();
            UpdateHintText("建筑已销毁，资源已返还");
        }

        #endregion

        #region 事件响应

        /// <summary>
        /// 销毁模式变化
        /// </summary>
        private void OnDemolishModeChanged(bool active)
        {
            isDemolishActive = active;

            // 更新按钮颜色
            if (demolishButtonImage != null)
            {
                demolishButtonImage.color = active ? buttonActiveColor : buttonNormalColor;
            }

            // 边框警告
            SetBorderWarningActive(active);

            // 确认按钮
            if (!active)
            {
                SetConfirmButtonActive(false);
            }

            // 提示文本
            if (active)
            {
                UpdateHintText("点击或框选建筑进行选中，右键取消选中，ESC退出");
            }
            else
            {
                UpdateHintText("");
            }

            pulseTimer = 0f;
        }

        /// <summary>
        /// 选中数量变化
        /// </summary>
        private void OnSelectionChanged(int count)
        {
            // 有选中时显示确认按钮
            SetConfirmButtonActive(count > 0);

            if (confirmButtonText != null)
            {
                confirmButtonText.text = count > 0 ? $"确认销毁 ({count})" : "确认销毁";
            }

            // 更新提示
            if (isDemolishActive)
            {
                if (count > 0)
                {
                    UpdateHintText($"已选中 {count} 个建筑，点击右下角确认销毁");
                }
                else
                {
                    UpdateHintText("点击或框选建筑进行选中，右键取消选中，ESC退出");
                }
            }
        }

        #endregion

        #region 边框警告

        /// <summary>
        /// 设置边框警告显示
        /// </summary>
        private void SetBorderWarningActive(bool active)
        {
            if (borderWarningRoot != null)
            {
                borderWarningRoot.SetActive(active);
            }
        }

        /// <summary>
        /// 边框呼吸动画
        /// </summary>
        private void UpdateBorderPulse()
        {
            pulseTimer += Time.deltaTime * borderPulseSpeed;
            float alpha = Mathf.Lerp(0.3f, borderWarningColor.a, (Mathf.Sin(pulseTimer) + 1f) * 0.5f);
            Color pulseColor = new Color(borderWarningColor.r, borderWarningColor.g, borderWarningColor.b, alpha);

            if (borderTop != null) borderTop.color = pulseColor;
            if (borderBottom != null) borderBottom.color = pulseColor;
            if (borderLeft != null) borderLeft.color = pulseColor;
            if (borderRight != null) borderRight.color = pulseColor;
        }

        #endregion

        #region 确认按钮

        /// <summary>
        /// 设置确认按钮显示
        /// </summary>
        private void SetConfirmButtonActive(bool active)
        {
            if (confirmButtonRoot != null)
            {
                confirmButtonRoot.SetActive(active);
            }
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 构建确认消息
        /// </summary>
        private string BuildConfirmMessage(DemolishController controller)
        {
            var names = controller.GetSelectedBuildingNames();
            var refunds = controller.GetRefundSummary();
            float ratio = controller.RefundRatio;

            // 建筑名称
            string buildingList;
            if (names.Count <= 3)
            {
                buildingList = string.Join("、", names);
            }
            else
            {
                buildingList = $"{names[0]}、{names[1]}...等 {names.Count} 个建筑";
            }

            // 返还资源
            string refundText = "";
            if (refunds.Count > 0)
            {
                var parts = new List<string>();
                foreach (var kvp in refunds)
                {
                    var definition = ResourceManager.Instance?.GetDefinition(kvp.Key);
                    string resName = definition != null ? definition.resourceName : kvp.Key.ToString();
                    parts.Add($"{resName} x{ResourceDefinition.FormatAmount(kvp.Value)}");
                }
                refundText = $"\n\n返还资源（{(int)(ratio * 100)}%）：\n{string.Join("\n", parts)}";
            }

            return $"确定要销毁 {buildingList} 吗？\n此操作无法撤销。{refundText}";
        }

        /// <summary>
        /// 更新提示文本
        /// </summary>
        private void UpdateHintText(string text)
        {
            if (hintText != null)
            {
                hintText.text = text;
            }
        }

        #endregion

        #region 公共接口

        /// <summary>
        /// 设置返还比例
        /// </summary>
        public void SetRefundRatio(float ratio)
        {
            if (DemolishController.Instance != null)
            {
                // 通过反射或直接设置（这里通过序列化字段间接控制）
                Debug.Log($"[DemolishPanelUI] 返还比例需在 DemolishController Inspector 中设置");
            }
        }

        #endregion
    }
}
