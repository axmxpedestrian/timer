using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text;
using PomodoroTimer.Map.Data;
using PomodoroTimer.Resource;
using static PomodoroTimer.Utils.LocalizedText;

namespace PomodoroTimer.UI.Building
{
    /// <summary>
    /// 建筑物浮动信息面板
    /// 显示：建筑名称、建造消耗、资源生产效率
    /// 支持富文本（TMP Sprite Tag）
    /// </summary>
    public class BuildingTooltipUI : MonoBehaviour
    {
        public static BuildingTooltipUI Instance { get; private set; }

        [Header("面板引用")]
        [SerializeField] private RectTransform tooltipPanel;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("内容引用")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI costText;
        [SerializeField] private TextMeshProUGUI productionText;

        [Header("设置")]
        [Tooltip("鼠标悬停多久后显示（秒）")]
        [SerializeField] private float showDelay = 0.35f;
        [Tooltip("面板相对于鼠标的偏移")]
        [SerializeField] private Vector2 offset = new Vector2(16f, -16f);
        [Tooltip("距离屏幕边缘的最小距离")]
        [SerializeField] private float screenEdgePadding = 8f;

        private Canvas rootCanvas;
        private RectTransform canvasRect;
        private BuildingItemData pendingData;
        private BuildingItemData currentData;
        private float hoverTimer;
        private bool isShowing;
        private bool isWaitingToShow;

        // ResourceType -> sprite name 映射
        private static readonly System.Collections.Generic.Dictionary<ResourceType, string> ResourceSpriteNames =
            new System.Collections.Generic.Dictionary<ResourceType, string>
        {
            { ResourceType.Coin, "coin" },
            { ResourceType.Food, "food" },
            { ResourceType.Labor, "labor" },
            { ResourceType.Wood, "wood" },
            { ResourceType.Mineral, "mineral" },
            { ResourceType.Storage, "storage" },
            { ResourceType.Energy, "energy" },
            { ResourceType.Transport, "transport" },
            { ResourceType.Education, "education" },
            { ResourceType.Scenery, "scenery" },
            { ResourceType.Welfare, "welfare" },
            { ResourceType.Productivity, "productivity" },
            { ResourceType.Research, "research" },
        };

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
            {
                Destroy(gameObject);
                return;
            }

            rootCanvas = GetComponentInParent<Canvas>();
            if (rootCanvas != null)
            {
                Canvas[] canvases = rootCanvas.GetComponentsInParent<Canvas>();
                if (canvases.Length > 0)
                    rootCanvas = canvases[canvases.Length - 1];
                canvasRect = rootCanvas.GetComponent<RectTransform>();
            }

            if (tooltipPanel != null && canvasRect != null)
            {
                tooltipPanel.SetParent(canvasRect, false);
                tooltipPanel.SetAsLastSibling();
                tooltipPanel.pivot = new Vector2(0f, 1f);
                tooltipPanel.anchorMin = new Vector2(0.5f, 0.5f);
                tooltipPanel.anchorMax = new Vector2(0.5f, 0.5f);
            }

            Hide();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void Update()
        {
            if (isWaitingToShow)
            {
                hoverTimer += Time.unscaledDeltaTime;
                if (hoverTimer >= showDelay)
                {
                    isWaitingToShow = false;
                    ShowTooltip(pendingData);
                }
            }

            if (isShowing)
            {
                UpdatePosition();
            }
        }

        public void RequestShow(BuildingItemData data)
        {
            if (data == null) return;
            if (isShowing && currentData == data) return;
            if (isWaitingToShow && pendingData == data) return;

            pendingData = data;
            hoverTimer = 0f;
            isWaitingToShow = true;

            if (isShowing)
            {
                isWaitingToShow = false;
                ShowTooltip(data);
            }
        }

        public void RequestHide(BuildingItemData data)
        {
            if (pendingData == data || currentData == data)
            {
                Hide();
            }
        }

        public void ForceHide()
        {
            Hide();
        }

        private void ShowTooltip(BuildingItemData data)
        {
            if (data == null || tooltipPanel == null) return;

            currentData = data;
            isShowing = true;

            UpdateContent(data);
            UpdatePosition();

            tooltipPanel.gameObject.SetActive(true);
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = false;
            }
        }

        private void Hide()
        {
            isShowing = false;
            isWaitingToShow = false;
            pendingData = null;
            currentData = null;
            hoverTimer = 0f;

            if (tooltipPanel != null)
                tooltipPanel.gameObject.SetActive(false);
        }

        /// <summary>
        /// 获取资源对应的 sprite tag
        /// </summary>
        private string GetResourceSprite(ResourceType type)
        {
            if (ResourceSpriteNames.TryGetValue(type, out string spriteName))
                return $"<sprite name=\"{spriteName}\">";
            return "";
        }

        /// <summary>
        /// 更新 Tooltip 内容：名称 + 消耗 + 生产效率
        /// </summary>
        private void UpdateContent(BuildingItemData data)
        {
            var bp = data.blueprint;

            // 建筑名称（本地化）
            if (titleText != null)
                titleText.text = bp.GetLocalizedName();

            // 建造消耗
            if (costText != null)
            {
                string costStr = FormatCosts(bp.buildCosts);
                costText.text = costStr;
                costText.gameObject.SetActive(!string.IsNullOrEmpty(costStr));
            }

            // 资源生产效率
            if (productionText != null)
            {
                string prodStr = FormatProduction(bp.blueprintId);
                productionText.text = prodStr;
                productionText.gameObject.SetActive(!string.IsNullOrEmpty(prodStr));
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipPanel);
        }

        /// <summary>
        /// 格式化建造消耗，富文本格式：<sprite> -数量 资源名
        /// </summary>
        private string FormatCosts(ResourceCost[] costs)
        {
            if (costs == null || costs.Length == 0) return "";

            var sb = new StringBuilder();
            for (int i = 0; i < costs.Length; i++)
            {
                if (i > 0) sb.Append("  ");

                var def = ResourceManager.Instance?.GetDefinition(costs[i].resourceType);
                string name = def != null ? def.resourceName : costs[i].resourceType.ToString();
                string amount = ResourceDefinition.FormatAmount(costs[i].amount);
                string sprite = GetResourceSprite(costs[i].resourceType);

                bool canAfford = true;
                if (ResourceManager.Instance != null)
                    canAfford = ResourceManager.Instance.GetAmount(costs[i].resourceType) >= costs[i].amount;

                if (!canAfford)
                    sb.Append($"<color=#FF6666>{sprite}-{amount} {name}</color>");
                else
                    sb.Append($"{sprite}-{amount} {name}");
            }
            return sb.ToString();
        }

        /// <summary>
        /// 格式化资源生产效率，从 BuildingResourceConfig 读取
        /// 显示消耗和产出，例如：<sprite> -3 粮食/5s  <sprite> +10 金币/5s
        /// </summary>
        private string FormatProduction(int blueprintId)
        {
            var config = BuildingResourceSystemManager.Instance?.GetConfig(blueprintId);
            if (config == null) return "";

            var sb = new StringBuilder();

            // 消耗
            if (config.consumptions != null)
            {
                foreach (var c in config.consumptions)
                {
                    if (sb.Length > 0) sb.Append("\n");
                    var def = ResourceManager.Instance?.GetDefinition(c.resourceType);
                    string name = def != null ? def.resourceName : c.resourceType.ToString();
                    string sprite = GetResourceSprite(c.resourceType);
                    string amount = ResourceDefinition.FormatAmount(c.amountPerCycle);
                    sb.Append($"<color=#FF9966>{sprite}-{amount} {name}/{c.cycleSeconds}s</color>");
                }
            }

            // 产出
            if (config.productions != null)
            {
                foreach (var p in config.productions)
                {
                    if (sb.Length > 0) sb.Append("\n");
                    var def = ResourceManager.Instance?.GetDefinition(p.resourceType);
                    string name = def != null ? def.resourceName : p.resourceType.ToString();
                    string sprite = GetResourceSprite(p.resourceType);
                    string amount = ResourceDefinition.FormatAmount(p.amountPerCycle);
                    sb.Append($"<color=#66FF66>{sprite}+{amount} {name}/{p.cycleSeconds}s</color>");
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// 更新面板位置，跟随鼠标并防止出屏
        /// </summary>
        private void UpdatePosition()
        {
            if (tooltipPanel == null || canvasRect == null) return;

            Vector2 mousePos = Input.mousePosition;

            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, mousePos, rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera,
                out localPoint);

            localPoint += offset;

            Vector2 panelSize = tooltipPanel.sizeDelta;
            Vector2 canvasSize = canvasRect.sizeDelta;
            Vector2 halfCanvas = canvasSize * 0.5f;

            if (localPoint.x + panelSize.x > halfCanvas.x - screenEdgePadding)
                localPoint.x = localPoint.x - panelSize.x - offset.x * 2f;

            if (localPoint.y - panelSize.y < -halfCanvas.y + screenEdgePadding)
                localPoint.y = localPoint.y + panelSize.y - offset.y * 2f;

            if (localPoint.x < -halfCanvas.x + screenEdgePadding)
                localPoint.x = -halfCanvas.x + screenEdgePadding;

            if (localPoint.y > halfCanvas.y - screenEdgePadding)
                localPoint.y = halfCanvas.y - screenEdgePadding;

            tooltipPanel.anchoredPosition = localPoint;
        }
    }
}
