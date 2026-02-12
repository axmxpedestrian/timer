using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PomodoroTimer.Map.Data;
using PomodoroTimer.Resource;

namespace PomodoroTimer.UI.Building
{
    /// <summary>
    /// 建造物项UI（用于虚拟滚动复用）
    /// </summary>
    public class BuildingItemUI : MonoBehaviour
    {
        [Header("UI引用")]
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private Transform costContainer;
        [SerializeField] private GameObject costItemPrefab;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Button selectButton;
        [SerializeField] private GameObject selectedIndicator;
        [SerializeField] private GameObject lockedOverlay;

        [Header("颜色设置")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color unaffordableColor = new Color(1f, 0.5f, 0.5f);
        [SerializeField] private Color selectedColor = new Color(0.8f, 1f, 0.8f);

        private BuildingItemData currentData;
        private int dataIndex = -1;
        private System.Action<BuildingItemData, int> onClickCallback;

        public int DataIndex => dataIndex;
        public BuildingItemData CurrentData => currentData;

        private void Awake()
        {
            if (selectButton != null)
            {
                selectButton.onClick.AddListener(OnClick);
            }
        }

        /// <summary>
        /// 绑定数据
        /// </summary>
        public void Bind(BuildingItemData data, int index, System.Action<BuildingItemData, int> onClick)
        {
            currentData = data;
            dataIndex = index;
            onClickCallback = onClick;

            if (data == null)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);

            // 设置图标
            if (iconImage != null)
            {
                iconImage.sprite = data.iconSprite;
                iconImage.color = data.isAffordable ? Color.white : unaffordableColor;
            }

            // 设置名称
            if (nameText != null)
            {
                nameText.text = data.buildingName;
                nameText.color = data.isAffordable ? Color.white : unaffordableColor;
            }

            // 设置锁定状态
            if (lockedOverlay != null)
            {
                lockedOverlay.SetActive(!data.isUnlocked);
            }

            // 更新消耗资源显示
            UpdateCostDisplay(data);

            // 更新背景颜色
            UpdateBackground(false);
        }

        /// <summary>
        /// 更新消耗资源显示
        /// </summary>
        private void UpdateCostDisplay(BuildingItemData data)
        {
            if (costContainer == null) return;

            // 清除现有的消耗项
            foreach (Transform child in costContainer)
            {
                child.gameObject.SetActive(false);
            }

            if (data.costs == null || data.costs.Length == 0)
            {
                return;
            }

            for (int i = 0; i < data.costs.Length; i++)
            {
                var cost = data.costs[i];
                bool canAfford = true;

                if (ResourceManager.Instance != null)
                {
                    canAfford = ResourceManager.Instance.GetAmount(cost.resourceType) >= cost.amount;
                }

                CreateOrUpdateCostItem(i, cost.resourceType, cost.amount, canAfford);
            }
        }

        /// <summary>
        /// 创建或更新消耗项
        /// </summary>
        private void CreateOrUpdateCostItem(int index, ResourceType type, long amount, bool canAfford)
        {
            if (costContainer == null || costItemPrefab == null) return;

            GameObject costItem;
            if (index < costContainer.childCount)
            {
                costItem = costContainer.GetChild(index).gameObject;
            }
            else
            {
                costItem = Instantiate(costItemPrefab, costContainer);
            }

            costItem.SetActive(true);

            // 获取资源定义
            var definition = ResourceManager.Instance?.GetDefinition(type);

            // 设置图标
            var iconImg = costItem.transform.Find("Icon")?.GetComponent<Image>();
            if (iconImg != null && definition != null)
            {
                iconImg.sprite = definition.icon;
            }

            // 设置数量文本
            var amountText = costItem.transform.Find("Amount")?.GetComponent<TextMeshProUGUI>();
            if (amountText != null)
            {
                amountText.text = ResourceDefinition.FormatAmount(amount);
                amountText.color = canAfford ? Color.white : unaffordableColor;
            }
        }

        /// <summary>
        /// 设置选中状态
        /// </summary>
        public void SetSelected(bool selected)
        {
            UpdateBackground(selected);

            if (selectedIndicator != null)
            {
                selectedIndicator.SetActive(selected);
            }
        }

        /// <summary>
        /// 更新背景颜色
        /// </summary>
        private void UpdateBackground(bool selected)
        {
            if (backgroundImage == null) return;

            if (selected)
            {
                backgroundImage.color = selectedColor;
            }
            else if (currentData != null && !currentData.isAffordable)
            {
                backgroundImage.color = unaffordableColor * 0.3f;
            }
            else
            {
                backgroundImage.color = normalColor;
            }
        }

        /// <summary>
        /// 刷新可负担状态
        /// </summary>
        public void RefreshAffordable()
        {
            if (currentData != null)
            {
                currentData.RefreshAffordable();
                Bind(currentData, dataIndex, onClickCallback);
            }
        }

        private void OnClick()
        {
            if (currentData != null && currentData.isUnlocked)
            {
                onClickCallback?.Invoke(currentData, dataIndex);
            }
        }
    }
}
