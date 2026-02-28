using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text;
using System.Collections.Generic;
using PomodoroTimer.Core;
using PomodoroTimer.Map.Data;
using PomodoroTimer.Resource;
using static PomodoroTimer.Utils.LocalizedText;
using System.Collections;

namespace PomodoroTimer.UI.Building
{
    /// <summary>
    /// 建筑详情面板 - 右侧固定面板
    /// 显示：建筑名称、预览图、描述、消耗资源、生产效率、类别标签、特殊机制
    /// 支持富文本和语言本地化
    /// </summary>
    public class BuildingDetailPanelUI : MonoBehaviour
    {
        public static BuildingDetailPanelUI Instance { get; private set; }

        [Header("面板根节点")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("关闭按钮")]
        [SerializeField] private Button closeButton;

        [Header("基础信息")]
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private Image previewImage;
        [SerializeField] private TextMeshProUGUI descriptionText;

        [Header("建造消耗")]
        [SerializeField] private TextMeshProUGUI costTitleText;
        [SerializeField] private TextMeshProUGUI costText;

        [Header("生产效率")]
        [SerializeField] private TextMeshProUGUI productionTitleText;
        [SerializeField] private TextMeshProUGUI productionText;
        [SerializeField] private GameObject productionSection;

        [Header("类别与标签")]
        [SerializeField] private TextMeshProUGUI categoryTitleText;
        [SerializeField] private TextMeshProUGUI categoryText;

        [Header("存储容量")]
        [SerializeField] private TextMeshProUGUI capacityTitleText;
        [SerializeField] private TextMeshProUGUI capacityText;
        [SerializeField] private GameObject capacitySection;

        [Header("特殊机制")]
        [SerializeField] private TextMeshProUGUI specialTitleText;
        [SerializeField] private TextMeshProUGUI specialText;
        [SerializeField] private GameObject specialSection;

        // 运行时创建的滚动视图
        private ScrollRect scrollRect;
        private RectTransform contentRectTransform;

        // 当前显示的蓝图
        private BuildingBlueprint currentBlueprint;

        // ResourceType -> sprite name 映射（复用 BuildingTooltipUI 的逻辑）
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
        }

        private void Start()
        {
            SetupScrollView();

            if (closeButton != null)
                closeButton.onClick.AddListener(Hide);

            // 订阅语言切换事件
            if (LocalizationManager.Instance != null)
                LocalizationManager.Instance.OnLocaleChanged += OnLocaleChanged;

            Hide();
        }

        /// <summary>
        /// 运行时将面板内容包裹进 ScrollRect，实现内容超高时可滚动
        /// </summary>
        private void SetupScrollView()
        {
            if (panelRoot == null) return;

            var panelTransform = panelRoot.GetComponent<RectTransform>();
            int panelLayer = panelRoot.layer;

            // 读取现有 VerticalLayoutGroup 的设置
            var existingLayout = panelRoot.GetComponent<VerticalLayoutGroup>();
            var existingFitter = panelRoot.GetComponent<ContentSizeFitter>();

            RectOffset savedPadding = new RectOffset(10, 10, 10, 10);
            float savedSpacing = 8f;
            bool savedChildForceExpandWidth = true;
            bool savedChildForceExpandHeight = false;
            bool savedChildControlWidth = false;
            bool savedChildControlHeight = false;

            if (existingLayout != null)
            {
                savedPadding = new RectOffset(
                    existingLayout.padding.left, existingLayout.padding.right,
                    existingLayout.padding.top, existingLayout.padding.bottom);
                savedSpacing = existingLayout.spacing;
                savedChildForceExpandWidth = existingLayout.childForceExpandWidth;
                savedChildForceExpandHeight = existingLayout.childForceExpandHeight;
                savedChildControlWidth = existingLayout.childControlWidth;
                savedChildControlHeight = existingLayout.childControlHeight;
            }

            // 收集现有子物体
            var children = new List<Transform>();
            for (int i = 0; i < panelTransform.childCount; i++)
                children.Add(panelTransform.GetChild(i));

            // 移除面板根节点上的布局组件（将迁移到 Content 上）
            if (existingFitter != null) Destroy(existingFitter);
            if (existingLayout != null) Destroy(existingLayout);

            // --- 创建 Viewport ---
            var viewportGO = new GameObject("Viewport");
            viewportGO.layer = panelLayer;
            var viewportRect = viewportGO.AddComponent<RectTransform>();
            viewportGO.transform.SetParent(panelTransform, false);
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;

            // 使用 RectMask2D 按矩形边界裁剪，不依赖 Image alpha
            viewportGO.AddComponent<RectMask2D>();

            // --- 创建 Content ---
            var contentGO = new GameObject("ScrollContent");
            contentGO.layer = panelLayer;
            var contentRect = contentGO.AddComponent<RectTransform>();
            contentGO.transform.SetParent(viewportRect, false);
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.sizeDelta = new Vector2(0f, 0f);

            var contentLayout = contentGO.AddComponent<VerticalLayoutGroup>();
            contentLayout.padding = savedPadding;
            contentLayout.spacing = savedSpacing;
            contentLayout.childForceExpandWidth = savedChildForceExpandWidth;
            contentLayout.childForceExpandHeight = savedChildForceExpandHeight;
            contentLayout.childControlWidth = savedChildControlWidth;
            contentLayout.childControlHeight = savedChildControlHeight;

            var contentFitter = contentGO.AddComponent<ContentSizeFitter>();
            contentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            contentRectTransform = contentRect;

            // 将原有子物体移入 Content
            foreach (var child in children)
                child.SetParent(contentRect, false);

            // --- 在面板根节点上添加 ScrollRect ---
            scrollRect = panelRoot.AddComponent<ScrollRect>();
            scrollRect.viewport = viewportRect;
            scrollRect.content = contentRect;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.scrollSensitivity = 25f;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;

            if (LocalizationManager.Instance != null)
                LocalizationManager.Instance.OnLocaleChanged -= OnLocaleChanged;
        }

        /// <summary>
        /// 语言切换时刷新当前显示内容
        /// </summary>
        private void OnLocaleChanged(UnityEngine.Localization.Locale newLocale)
        {
            if (currentBlueprint != null && panelRoot != null && panelRoot.activeSelf)
                UpdateContent(currentBlueprint);
        }

        /// <summary>
        /// 显示指定建筑的详情面板
        /// </summary>
        public void Show(BuildingBlueprint blueprint)
        {
            if (blueprint == null) return;

            currentBlueprint = blueprint;
            UpdateContent(blueprint);

            if (panelRoot != null)
                panelRoot.SetActive(true);
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = true;
                canvasGroup.interactable = true;
            }

            // 每次显示新建筑时滚动到顶部
            if (scrollRect != null)
                scrollRect.verticalNormalizedPosition = 1f;
        }

        /// <summary>
        /// 隐藏详情面板
        /// </summary>
        public void Hide()
        {
            currentBlueprint = null;

            if (panelRoot != null)
                panelRoot.SetActive(false);
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }
        }

        /// <summary>
        /// 当前是否正在显示
        /// </summary>
        public bool IsShowing => panelRoot != null && panelRoot.activeSelf;

        /// <summary>
        /// 更新面板内容
        /// </summary>
        private void UpdateContent(BuildingBlueprint bp)
        {
            // 建筑名称
            if (nameText != null)
                nameText.text = bp.GetLocalizedName();

            // 预览图
            if (previewImage != null)
            {
                var sprite = bp.GetPreviewSprite();
                previewImage.sprite = sprite;
                previewImage.enabled = sprite != null;
            }

            // 描述（支持富文本）
            if (descriptionText != null)
                descriptionText.text = bp.GetLocalizedDescription();

            // 建造消耗
            UpdateCostSection(bp);

            // 生产效率
            UpdateProductionSection(bp);

            // 存储容量
            UpdateCapacitySection(bp);

            // 类别与标签
            UpdateCategorySection(bp);

            // 特殊机制
            UpdateSpecialSection(bp);

            // 刷新静态标题文本（本地化）
            RefreshTitleTexts();

            // 强制重建布局，避免切换建筑时各 Section 位置未更新导致重叠
            RebuildLayout();
        }

        /// <summary>
        /// 强制重建布局：先等一帧让 TMP 文本几何体更新，再重算 VerticalLayoutGroup
        /// </summary>
        private void RebuildLayout()
        {
            if (contentRectTransform == null) return;

            // 立即重建一次
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRectTransform);

            // TMP 文本的 preferred height 可能在当前帧还未就绪，延迟一帧再重建一次
            StartCoroutine(DelayedLayoutRebuild());
        }

        private IEnumerator DelayedLayoutRebuild()
        {
            yield return null;
            if (contentRectTransform != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(contentRectTransform);
        }

        /// <summary>
        /// 刷新各区域标题的本地化文本
        /// </summary>
        private void RefreshTitleTexts()
        {
            if (costTitleText != null)
                costTitleText.text = Get("UI_Building", "detail_cost_title");
            if (productionTitleText != null)
                productionTitleText.text = Get("UI_Building", "detail_production_title");
            if (capacityTitleText != null)
                capacityTitleText.text = Get("UI_Building", "detail_capacity_title");
            if (categoryTitleText != null)
                categoryTitleText.text = Get("UI_Building", "detail_category_title");
            if (specialTitleText != null)
                specialTitleText.text = Get("UI_Building", "detail_special_title");
        }

        /// <summary>
        /// 更新建造消耗区域
        /// </summary>
        private void UpdateCostSection(BuildingBlueprint bp)
        {
            if (costText == null) return;

            if (bp.buildCosts == null || bp.buildCosts.Length == 0)
            {
                costText.text = Get("UI_Building", "detail_cost_free");
                return;
            }

            var sb = new StringBuilder();
            foreach (var cost in bp.buildCosts)
            {
                if (sb.Length > 0) sb.Append("\n");

                var def = ResourceManager.Instance?.GetDefinition(cost.resourceType);
                string resName = def != null ? def.resourceName : cost.resourceType.ToString();
                string sprite = GetResourceSprite(cost.resourceType);
                string amount = ResourceDefinition.FormatAmount(cost.amount);

                bool canAfford = ResourceManager.Instance != null &&
                    ResourceManager.Instance.GetAmount(cost.resourceType) >= cost.amount;

                if (!canAfford)
                    sb.Append($"<color=#FF6666>{sprite} {resName} x{amount}</color>");
                else
                    sb.Append($"{sprite} {resName} x{amount}");
            }
            costText.text = sb.ToString();
        }

        /// <summary>
        /// 更新生产效率区域
        /// </summary>
        private void UpdateProductionSection(BuildingBlueprint bp)
        {
            var config = BuildingResourceSystemManager.Instance?.GetConfig(bp.blueprintId);

            bool hasProduction = config != null &&
                ((config.productions != null && config.productions.Length > 0) ||
                 (config.consumptions != null && config.consumptions.Length > 0));

            if (productionSection != null)
                productionSection.SetActive(hasProduction);

            if (!hasProduction || productionText == null) return;

            var sb = new StringBuilder();

            // 消耗
            if (config.consumptions != null)
            {
                foreach (var c in config.consumptions)
                {
                    if (sb.Length > 0) sb.Append("\n");
                    var def = ResourceManager.Instance?.GetDefinition(c.resourceType);
                    string resName = def != null ? def.resourceName : c.resourceType.ToString();
                    string sprite = GetResourceSprite(c.resourceType);
                    string amount = ResourceDefinition.FormatAmount(c.amountPerCycle);
                    sb.Append($"<color=#FF9966>{sprite} -{amount} {resName}/{c.cycleSeconds}s</color>");
                }
            }

            // 产出
            if (config.productions != null)
            {
                foreach (var p in config.productions)
                {
                    if (sb.Length > 0) sb.Append("\n");
                    var def = ResourceManager.Instance?.GetDefinition(p.resourceType);
                    string resName = def != null ? def.resourceName : p.resourceType.ToString();
                    string sprite = GetResourceSprite(p.resourceType);
                    string amount = ResourceDefinition.FormatAmount(p.amountPerCycle);
                    sb.Append($"<color=#66FF66>{sprite} +{amount} {resName}/{p.cycleSeconds}s</color>");
                }
            }

            productionText.text = sb.ToString();
        }

        /// <summary>
        /// 更新存储容量区域
        /// </summary>
        private void UpdateCapacitySection(BuildingBlueprint bp)
        {
            bool hasCapacity = bp.storageCapacities != null && bp.storageCapacities.Length > 0;

            if (capacitySection != null)
                capacitySection.SetActive(hasCapacity);

            if (!hasCapacity || capacityText == null) return;

            var sb = new StringBuilder();

            foreach (var sc in bp.storageCapacities)
            {
                if (sc == null || sc.capacity <= 0) continue;

                if (sb.Length > 0) sb.Append("\n");

                var def = ResourceManager.Instance?.GetDefinition(sc.resourceType);
                string resName = def != null ? def.resourceName : sc.resourceType.ToString();
                string sprite = GetResourceSprite(sc.resourceType);
                string amount = ResourceDefinition.FormatAmount(sc.capacity);

                sb.Append($"<color=#66CCFF>{sprite} {resName} +{amount}</color>");
            }

            capacityText.text = sb.ToString();
        }

        /// <summary>
        /// 更新类别与标签区域
        /// </summary>
        private void UpdateCategorySection(BuildingBlueprint bp)
        {
            if (categoryText == null) return;

            var sb = new StringBuilder();

            // 类别
            string categoryKey = "category_" + bp.category.ToString().ToLower();
            string categoryName = Get("UI_Building", categoryKey);
            if (categoryName == categoryKey)
                categoryName = bp.category.ToString();
            sb.Append($"<b>{Get("UI_Building", "detail_category_label")}</b> {categoryName}");

            // 建筑类型标签
            if (bp.buildingTypeTags != null && bp.buildingTypeTags.Count > 0)
            {
                sb.Append($"\n<b>{Get("UI_Building", "detail_type_tags")}</b> ");
                sb.Append(string.Join("、", bp.buildingTypeTags));
            }

            // 风格标签
            if (bp.styleTags != null && bp.styleTags.Count > 0)
            {
                sb.Append($"\n<b>{Get("UI_Building", "detail_style_tags")}</b> ");
                sb.Append(string.Join("、", bp.styleTags));
            }

            categoryText.text = sb.ToString();
        }

        /// <summary>
        /// 更新特殊机制区域（暂未实装）
        /// </summary>
        private void UpdateSpecialSection(BuildingBlueprint bp)
        {
            if (specialSection != null)
                specialSection.SetActive(false);

            if (specialText != null)
                specialText.text = Get("UI_Building", "detail_special_not_implemented");
        }

        private string GetResourceSprite(ResourceType type)
        {
            if (ResourceSpriteNames.TryGetValue(type, out string spriteName))
                return $"<sprite name=\"{spriteName}\">";
            return "";
        }
    }
}
