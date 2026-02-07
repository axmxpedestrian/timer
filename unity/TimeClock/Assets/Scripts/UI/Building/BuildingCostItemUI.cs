using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PomodoroTimer.Resource;

namespace PomodoroTimer.UI.Building
{
    /// <summary>
    /// 建造消耗项UI
    /// 显示单个资源消耗
    /// </summary>
    public class BuildingCostItemUI : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI amountText;

        [Header("颜色")]
        [SerializeField] private Color affordableColor = Color.white;
        [SerializeField] private Color unaffordableColor = new Color(1f, 0.4f, 0.4f);

        /// <summary>
        /// 设置消耗数据
        /// </summary>
        public void SetData(ResourceType resourceType, long amount, bool canAfford)
        {
            // 获取资源定义
            var definition = ResourceManager.Instance?.GetDefinition(resourceType);

            if (iconImage != null && definition != null)
            {
                iconImage.sprite = definition.icon;
                iconImage.color = canAfford ? affordableColor : unaffordableColor;
            }

            if (amountText != null)
            {
                amountText.text = ResourceDefinition.FormatAmount(amount);
                amountText.color = canAfford ? affordableColor : unaffordableColor;
            }
        }

        /// <summary>
        /// 设置消耗数据（简化版）
        /// </summary>
        public void SetData(Sprite icon, string amountStr, bool canAfford)
        {
            if (iconImage != null)
            {
                iconImage.sprite = icon;
                iconImage.color = canAfford ? affordableColor : unaffordableColor;
            }

            if (amountText != null)
            {
                amountText.text = amountStr;
                amountText.color = canAfford ? affordableColor : unaffordableColor;
            }
        }
    }
}
