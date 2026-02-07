using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PomodoroTimer.Resource
{
    /// <summary>
    /// 资源变化预览UI
    /// 用于建筑放置/升级时显示资源变化
    /// </summary>
    public class ResourcePreviewUI : MonoBehaviour
    {
        [Header("UI组件")]
        [SerializeField] private GameObject previewPanel;
        [SerializeField] private Transform costContainer;
        [SerializeField] private Transform productionContainer;
        [SerializeField] private Transform consumptionContainer;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;

        [Header("预制体")]
        [SerializeField] private GameObject costItemPrefab;
        [SerializeField] private GameObject productionItemPrefab;

        [Header("颜色设置")]
        [SerializeField] private Color affordableColor = Color.white;
        [SerializeField] private Color unaffordableColor = new Color(1f, 0.3f, 0.3f);
        [SerializeField] private Color productionColor = new Color(0.3f, 0.8f, 0.3f);
        [SerializeField] private Color consumptionColor = new Color(0.8f, 0.5f, 0.3f);

        private ResourceChangePreview currentPreview;
        private System.Action onConfirm;
        private System.Action onCancel;

        private List<GameObject> spawnedItems = new List<GameObject>();

        private void Awake()
        {
            if (confirmButton != null)
                confirmButton.onClick.AddListener(OnConfirmClicked);
            if (cancelButton != null)
                cancelButton.onClick.AddListener(OnCancelClicked);

            Hide();
        }

        /// <summary>
        /// 显示建造预览
        /// </summary>
        public void ShowBuildPreview(int blueprintId, string buildingName, System.Action onConfirm, System.Action onCancel)
        {
            var preview = BuildingResourceSystemManager.Instance?.GetBuildPreview(blueprintId);
            if (preview == null)
            {
                preview = new ResourceChangePreview();
            }

            this.onConfirm = onConfirm;
            this.onCancel = onCancel;

            if (titleText != null)
                titleText.text = $"建造 {buildingName}";

            ShowPreview(preview);
        }

        /// <summary>
        /// 显示升级预览
        /// </summary>
        public void ShowUpgradePreview(int instanceId, string buildingName, int currentLevel, System.Action onConfirm, System.Action onCancel)
        {
            var preview = BuildingResourceSystemManager.Instance?.GetUpgradePreview(instanceId);
            if (preview == null)
            {
                preview = new ResourceChangePreview();
            }

            this.onConfirm = onConfirm;
            this.onCancel = onCancel;

            if (titleText != null)
                titleText.text = $"升级 {buildingName} (Lv.{currentLevel} → Lv.{currentLevel + 1})";

            ShowPreview(preview);
        }

        /// <summary>
        /// 显示预览
        /// </summary>
        private void ShowPreview(ResourceChangePreview preview)
        {
            currentPreview = preview;
            ClearSpawnedItems();

            // 显示花费
            if (costContainer != null)
            {
                foreach (var kvp in preview.costs)
                {
                    CreateCostItem(costContainer, kvp.Key, kvp.Value);
                }
            }

            // 显示产出变化
            if (productionContainer != null)
            {
                foreach (var change in preview.productionChanges)
                {
                    CreateProductionItem(productionContainer, change, true);
                }
            }

            // 显示消耗变化
            if (consumptionContainer != null)
            {
                foreach (var change in preview.consumptionChanges)
                {
                    CreateProductionItem(consumptionContainer, change, false);
                }
            }

            // 更新确认按钮状态
            UpdateConfirmButton();

            previewPanel?.SetActive(true);
        }

        /// <summary>
        /// 创建花费项
        /// </summary>
        private void CreateCostItem(Transform parent, ResourceType type, long amount)
        {
            if (costItemPrefab == null) return;

            var itemObj = Instantiate(costItemPrefab, parent);
            spawnedItems.Add(itemObj);

            var definition = ResourceManager.Instance?.GetDefinition(type);
            long currentAmount = ResourceManager.Instance?.GetAmount(type) ?? 0;
            bool canAfford = currentAmount >= amount;

            // 设置图标
            var iconImage = itemObj.GetComponentInChildren<Image>();
            if (iconImage != null && definition?.icon != null)
            {
                iconImage.sprite = definition.icon;
                iconImage.color = definition.iconColor;
            }

            // 设置文本
            var texts = itemObj.GetComponentsInChildren<TextMeshProUGUI>();
            foreach (var text in texts)
            {
                if (text.name.Contains("Amount") || text.name.Contains("Cost"))
                {
                    text.text = $"-{ResourceDefinition.FormatAmount(amount)}";
                    text.color = canAfford ? affordableColor : unaffordableColor;
                }
                else if (text.name.Contains("Current"))
                {
                    text.text = $"({ResourceDefinition.FormatAmount(currentAmount)})";
                    text.color = canAfford ? affordableColor : unaffordableColor;
                }
                else if (text.name.Contains("Name"))
                {
                    text.text = definition?.resourceName ?? type.ToString();
                }
            }

            // 如果只有一个Text组件
            if (texts.Length == 1)
            {
                texts[0].text = $"{definition?.resourceName ?? type.ToString()}: -{ResourceDefinition.FormatAmount(amount)} ({ResourceDefinition.FormatAmount(currentAmount)})";
                texts[0].color = canAfford ? affordableColor : unaffordableColor;
            }
        }

        /// <summary>
        /// 创建产出/消耗项
        /// </summary>
        private void CreateProductionItem(Transform parent, ResourceChangeItem change, bool isProduction)
        {
            if (productionItemPrefab == null) return;

            var itemObj = Instantiate(productionItemPrefab, parent);
            spawnedItems.Add(itemObj);

            var definition = ResourceManager.Instance?.GetDefinition(change.resourceType);
            Color itemColor = isProduction ? productionColor : consumptionColor;

            // 设置图标
            var iconImage = itemObj.GetComponentInChildren<Image>();
            if (iconImage != null && definition?.icon != null)
            {
                iconImage.sprite = definition.icon;
                iconImage.color = definition.iconColor;
            }

            // 计算每秒变化
            float perSecond = change.GetPerSecond();
            string rateText = isProduction
                ? $"+{ResourceDefinition.FormatAmount(change.amountPerCycle)}/{change.cycleSeconds}s"
                : $"-{ResourceDefinition.FormatAmount(change.amountPerCycle)}/{change.cycleSeconds}s";

            // 设置文本
            var texts = itemObj.GetComponentsInChildren<TextMeshProUGUI>();
            foreach (var text in texts)
            {
                if (text.name.Contains("Rate") || text.name.Contains("Amount"))
                {
                    text.text = rateText;
                    text.color = itemColor;
                }
                else if (text.name.Contains("Name"))
                {
                    text.text = definition?.resourceName ?? change.resourceType.ToString();
                }
            }

            // 如果只有一个Text组件
            if (texts.Length == 1)
            {
                texts[0].text = $"{definition?.resourceName ?? change.resourceType.ToString()}: {rateText}";
                texts[0].color = itemColor;
            }
        }

        /// <summary>
        /// 更新确认按钮状态
        /// </summary>
        private void UpdateConfirmButton()
        {
            if (confirmButton == null) return;

            bool canAfford = currentPreview?.CanAfford() ?? false;
            confirmButton.interactable = canAfford;

            // 更新按钮文本颜色
            var buttonText = confirmButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.color = canAfford ? Color.white : new Color(0.5f, 0.5f, 0.5f);
            }
        }

        /// <summary>
        /// 隐藏预览
        /// </summary>
        public void Hide()
        {
            previewPanel?.SetActive(false);
            ClearSpawnedItems();
            currentPreview = null;
            onConfirm = null;
            onCancel = null;
        }

        private void ClearSpawnedItems()
        {
            foreach (var item in spawnedItems)
            {
                if (item != null)
                    Destroy(item);
            }
            spawnedItems.Clear();
        }

        private void OnConfirmClicked()
        {
            var callback = onConfirm;
            Hide();
            callback?.Invoke();
        }

        private void OnCancelClicked()
        {
            var callback = onCancel;
            Hide();
            callback?.Invoke();
        }

        /// <summary>
        /// 检查当前预览是否可负担
        /// </summary>
        public bool CanAfford()
        {
            return currentPreview?.CanAfford() ?? false;
        }
    }
}
