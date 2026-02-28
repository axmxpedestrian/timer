using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Localization;
using PomodoroTimer.Core;
using PomodoroTimer.Map.Data;
using PomodoroTimer.Map.Sprite2D;
using PomodoroTimer.Resource;
using static PomodoroTimer.Utils.LocalizedText;

namespace PomodoroTimer.UI.Building
{
    /// <summary>
    /// 建造面板UI控制器
    /// 管理建造物选择、分类筛选、放置流程
    /// </summary>
    public class BuildingPanelUI : MonoBehaviour
    {
        public static BuildingPanelUI Instance { get; private set; }

        [Header("面板控制")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private RectTransform panelTransform;
        [SerializeField] private Button toggleButton;           // 底部"放置"按钮
        [SerializeField] private Button closeButton;            // 右上角关闭按钮
        [SerializeField] private float slideSpeed = 800f;       // 滑动速度
        [SerializeField] private float hiddenYOffset = -300f;   // 隐藏时的Y偏移

        // 用于控制隐藏时不拦截鼠标事件
        private CanvasGroup panelCanvasGroup;

        [Header("分类按钮")]
        [SerializeField] private Transform categoryButtonContainer;
        [SerializeField] private GameObject categoryButtonPrefab;
        [SerializeField] private Color categoryNormalColor = new Color(0.3f, 0.3f, 0.3f);
        [SerializeField] private Color categorySelectedColor = new Color(0.2f, 0.6f, 0.2f);
        [Tooltip("分类图标，顺序：全部、建筑、道路、自然、设施、结构")]
        [SerializeField] private Sprite[] categoryIcons;

        [Header("建造物列表")]
        [SerializeField] private VirtualScrollView buildingScrollView;

        [Header("标签筛选")]
        [SerializeField] private BuildingTagFilterUI tagFilterUI;

        [Header("提示信息")]
        [SerializeField] private TextMeshProUGUI hintText;
        [SerializeField] private GameObject rotationHint;

        [Header("懒加载设置")]
        [SerializeField] private int currentTechLevel = 0;      // 当前科技等级
        [SerializeField] private bool loadAllOnStart = false;   // 是否启动时加载全部

        // 分类按钮
        private List<Button> categoryButtons = new List<Button>();
        private List<Image> categoryButtonImages = new List<Image>();
        private BuildingCategory currentCategory = BuildingCategory.All;

        // 标签筛选
        private BuildingTagFilter tagFilter;

        // 建造物数据
        private List<BuildingItemData> allBuildingData = new List<BuildingItemData>();
        private List<BuildingItemData> filteredBuildingData = new List<BuildingItemData>();
        private Dictionary<int, BuildingItemData> loadedBlueprintData = new Dictionary<int, BuildingItemData>();

        // 面板状态
        private bool isPanelOpen = false;
        private bool isAnimating = false;
        private float targetY;
        private float defaultY;

        // 选中状态
        private BuildingItemData selectedBuilding;
        private bool isPlacementMode = false;

        // 资源检查缓存
        private float lastAffordableCheckTime;
        private const float AFFORDABLE_CHECK_INTERVAL = 0.5f;

        // 事件
        public event System.Action<bool> OnPanelToggled;
        public event System.Action<BuildingBlueprint> OnBuildingSelected;

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
            // 防止重复实例（Destroy是延迟执行的，Start仍会被调用）
            if (Instance != this) return;

            Initialize();
        }

        private void OnDestroy()
        {
            if (tagFilter != null)
            {
                tagFilter.OnFilterChanged -= OnTagFilterChanged;
            }

            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.OnLocaleChanged -= OnLocaleChanged;
            }

            if (Instance == this)
                Instance = null;
        }

        /// <summary>
        /// 语言切换回调 - 刷新所有动态文本
        /// </summary>
        private void OnLocaleChanged(Locale newLocale)
        {
            // 刷新提示文本
            if (!isPlacementMode)
            {
                UpdateHintText(Get("UI_Building", "hint_click_to_place"));
            }

            // 刷新分类按钮文本
            RefreshCategoryButtonTexts();
        }

        /// <summary>
        /// 刷新分类按钮的本地化文本
        /// </summary>
        private void RefreshCategoryButtonTexts()
        {
            string[] categoryNames = new string[]
            {
                Get("UI_Building", "category_all"),
                Get("UI_Building", "panel_category_building"),
                Get("UI_Building", "category_road"),
                Get("UI_Building", "category_nature"),
                Get("UI_Building", "category_facility"),
                Get("UI_Building", "category_structure")
            };

            for (int i = 0; i < categoryButtons.Count && i < categoryNames.Length; i++)
            {
                var text = categoryButtons[i]?.GetComponentInChildren<TextMeshProUGUI>();
                if (text != null)
                {
                    text.text = categoryNames[i];
                }
            }
        }

        private void Update()
        {
            // 面板滑动动画
            if (isAnimating)
            {
                UpdatePanelAnimation();
            }

            // 放置模式输入处理
            if (isPlacementMode)
            {
                HandlePlacementInput();
            }

            // 定期刷新可负担状态
            if (isPanelOpen && Time.time - lastAffordableCheckTime > AFFORDABLE_CHECK_INTERVAL)
            {
                RefreshAffordableState();
                lastAffordableCheckTime = Time.time;
            }
        }

        /// <summary>
        /// 初始化
        /// </summary>
        private void Initialize()
        {
            // 确保 panelRoot 上有 CanvasGroup
            if (panelRoot != null)
            {
                panelCanvasGroup = panelRoot.GetComponent<CanvasGroup>();
                if (panelCanvasGroup == null)
                    panelCanvasGroup = panelRoot.AddComponent<CanvasGroup>();
            }

            // 记录默认位置
            if (panelTransform != null)
            {
                defaultY = panelTransform.anchoredPosition.y;
                targetY = defaultY + hiddenYOffset;
                panelTransform.anchoredPosition = new Vector2(
                    panelTransform.anchoredPosition.x, targetY);
            }

            // 初始状态面板关闭，禁止拦截射线
            SetPanelInteractable(false);

            // 绑定按钮事件
            if (toggleButton != null)
            {
                toggleButton.onClick.AddListener(TogglePanel);
            }

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(ClosePanel);
            }

            // 创建分类按钮
            CreateCategoryButtons();

            // 初始化标签筛选
            tagFilter = new BuildingTagFilter();
            tagFilter.OnFilterChanged += OnTagFilterChanged;
            if (tagFilterUI != null)
            {
                tagFilterUI.Initialize(tagFilter);
            }

            // 延迟加载建造物数据，确保 ModularBuildingManager 已完成初始化
            StartCoroutine(DelayedLoadBuildingData());

            // 初始隐藏面板
            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
            }

            // 隐藏提示
            if (rotationHint != null)
            {
                rotationHint.SetActive(false);
            }

            UpdateHintText(Get("UI_Building", "hint_click_to_place"));

            // 订阅语言切换事件，刷新所有动态文本
            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.OnLocaleChanged += OnLocaleChanged;
            }
        }

        /// <summary>
        /// 延迟加载建造物数据，等待其他管理器初始化完成
        /// </summary>
        private IEnumerator DelayedLoadBuildingData()
        {
            // 等待一帧，确保所有 Start() 都已执行完毕
            yield return null;

            // 等待 ModularBuildingManager 就绪
            while (ModularBuildingManager.Instance == null)
            {
                yield return null;
            }

            // 加载建造物数据
            if (loadAllOnStart)
            {
                LoadAllBuildingData();
            }
            else
            {
                LoadBuildingDataByTechLevel(currentTechLevel);
            }

            Debug.Log($"[BuildingPanelUI] 建造物数据加载完成，共 {allBuildingData.Count} 项");
        }

        #region 分类按钮

        /// <summary>
        /// 创建分类按钮
        /// </summary>
        private void CreateCategoryButtons()
        {
            if (categoryButtonContainer == null || categoryButtonPrefab == null) return;

            // 清除旧按钮，防止重复生成
            foreach (var btn in categoryButtons)
            {
                if (btn != null)
                    Destroy(btn.gameObject);
            }
            categoryButtons.Clear();
            categoryButtonImages.Clear();

            // 定义要显示的分类
            BuildingCategory[] categories = new BuildingCategory[]
            {
                BuildingCategory.All,
                BuildingCategory.Residential,
                BuildingCategory.Road,
                BuildingCategory.Nature,
                BuildingCategory.Facility,
                BuildingCategory.Structure
            };

            string[] categoryNames = new string[]
            {
                Get("UI_Building", "category_all"),
                Get("UI_Building", "panel_category_building"),
                Get("UI_Building", "category_road"),
                Get("UI_Building", "category_nature"),
                Get("UI_Building", "category_facility"),
                Get("UI_Building", "category_structure")
            };

            for (int i = 0; i < categories.Length; i++)
            {
                var btnObj = Instantiate(categoryButtonPrefab, categoryButtonContainer);
                var btn = btnObj.GetComponent<Button>();
                var img = btnObj.GetComponent<Image>();
                var text = btnObj.GetComponentInChildren<TextMeshProUGUI>();

                if (text != null)
                {
                    text.text = categoryNames[i];
                }

                // 设置分类图标
                var iconTransform = btnObj.transform.Find("Image");
                if (iconTransform != null)
                {
                    var iconImage = iconTransform.GetComponent<Image>();
                    if (iconImage != null)
                    {
                        if (categoryIcons != null && i < categoryIcons.Length && categoryIcons[i] != null)
                        {
                            iconImage.sprite = categoryIcons[i];
                            iconImage.color = Color.white;
                        }
                        else
                        {
                            // 没有配置图标时隐藏图标Image，避免显示默认白图
                            iconImage.enabled = false;
                        }
                    }
                }

                int index = i;
                BuildingCategory cat = categories[i];

                if (btn != null)
                {
                    btn.onClick.AddListener(() => OnCategoryClicked(cat, index));
                    categoryButtons.Add(btn);
                }

                if (img != null)
                {
                    categoryButtonImages.Add(img);
                }
            }

            // 默认选中"全部"
            UpdateCategoryButtonVisuals(0);
        }

        /// <summary>
        /// 分类按钮点击
        /// </summary>
        private void OnCategoryClicked(BuildingCategory category, int buttonIndex)
        {
            if (currentCategory == category) return;

            currentCategory = category;
            UpdateCategoryButtonVisuals(buttonIndex);
            FilterBuildingsByCategory();
        }

        /// <summary>
        /// 更新分类按钮视觉
        /// </summary>
        private void UpdateCategoryButtonVisuals(int selectedIndex)
        {
            for (int i = 0; i < categoryButtonImages.Count; i++)
            {
                categoryButtonImages[i].color = (i == selectedIndex)
                    ? categorySelectedColor
                    : categoryNormalColor;
            }
        }

        #endregion

        #region 建造物数据加载

        /// <summary>
        /// 加载所有建造物数据
        /// </summary>
        private void LoadAllBuildingData()
        {
            allBuildingData.Clear();
            loadedBlueprintData.Clear();

            var manager = ModularBuildingManager.Instance;
            if (manager == null) return;

            foreach (var blueprint in manager.GetAllBlueprints())
            {
                var data = new BuildingItemData(blueprint);
                data.isUnlocked = blueprint.IsTechUnlocked(currentTechLevel);
                allBuildingData.Add(data);
                loadedBlueprintData[blueprint.blueprintId] = data;
            }

            // 按科技等级排序
            allBuildingData.Sort((a, b) => a.techLevel.CompareTo(b.techLevel));

            // 收集标签
            if (tagFilter != null)
                tagFilter.CollectTagsFromBlueprints(manager.GetAllBlueprints());

            FilterBuildingsByCategory();
        }

        /// <summary>
        /// 按科技等级懒加载建造物数据
        /// </summary>
        public void LoadBuildingDataByTechLevel(int techLevel)
        {
            currentTechLevel = techLevel;

            var manager = ModularBuildingManager.Instance;
            if (manager == null) return;

            foreach (var blueprint in manager.GetAllBlueprints())
            {
                // 只加载当前科技等级可用的建筑
                if (blueprint.techLevel <= techLevel)
                {
                    if (!loadedBlueprintData.ContainsKey(blueprint.blueprintId))
                    {
                        var data = new BuildingItemData(blueprint);
                        data.isUnlocked = true;
                        allBuildingData.Add(data);
                        loadedBlueprintData[blueprint.blueprintId] = data;
                    }
                    else
                    {
                        // 更新解锁状态
                        loadedBlueprintData[blueprint.blueprintId].isUnlocked = true;
                    }
                }
            }

            // 按科技等级排序
            allBuildingData.Sort((a, b) => a.techLevel.CompareTo(b.techLevel));

            // 收集标签
            if (tagFilter != null)
                tagFilter.CollectTagsFromBlueprints(manager.GetAllBlueprints());

            FilterBuildingsByCategory();
        }

        /// <summary>
        /// 解锁新科技等级
        /// </summary>
        public void UnlockTechLevel(int newTechLevel)
        {
            if (newTechLevel <= currentTechLevel) return;

            LoadBuildingDataByTechLevel(newTechLevel);
        }

        /// <summary>
        /// 按分类筛选建造物
        /// </summary>
        private void FilterBuildingsByCategory()
        {
            filteredBuildingData.Clear();

            foreach (var data in allBuildingData)
            {
                if (currentCategory == BuildingCategory.All ||
                    data.blueprint.category == currentCategory)
                {
                    filteredBuildingData.Add(data);
                }
            }

            // 应用标签筛选
            if (tagFilter != null && tagFilter.HasActiveFilters())
            {
                filteredBuildingData = tagFilter.ApplyFilter(filteredBuildingData);
            }

            // 更新滚动视图
            if (buildingScrollView != null)
            {
                buildingScrollView.SetData(filteredBuildingData, OnBuildingItemClicked);
            }
        }

        /// <summary>
        /// 标签筛选变化回调
        /// </summary>
        private void OnTagFilterChanged()
        {
            FilterBuildingsByCategory();
        }

        /// <summary>
        /// 刷新可负担状态
        /// </summary>
        private void RefreshAffordableState()
        {
            if (buildingScrollView != null)
            {
                buildingScrollView.RefreshAffordableState();
            }
        }

        #endregion

        #region 面板控制

        /// <summary>
        /// 切换面板显示
        /// </summary>
        public void TogglePanel()
        {
            if (isAnimating) return;

            if (isPanelOpen)
            {
                ClosePanel();
            }
            else
            {
                OpenPanel();
            }
        }

        /// <summary>
        /// 打开面板
        /// </summary>
        public void OpenPanel()
        {
            if (isPanelOpen || isAnimating) return;

            // 如果正在销毁模式，先退出
            if (DemolishController.Instance != null && DemolishController.Instance.IsDemolishMode)
            {
                DemolishController.Instance.ExitDemolishMode();
            }

            isPanelOpen = true;
            targetY = defaultY;
            isAnimating = true;

            // 打开时立即允许交互
            SetPanelInteractable(true);

            // 隐藏 TimerSection、ControlButtons、TaskSection，避免遮挡地图
            MainUIController.Instance?.EnterBuildMode();

            // 刷新数据
            RefreshAffordableState();

            OnPanelToggled?.Invoke(true);
        }

        /// <summary>
        /// 关闭面板
        /// </summary>
        public void ClosePanel()
        {
            if (!isPanelOpen || isAnimating) return;

            isPanelOpen = false;
            targetY = defaultY + hiddenYOffset;
            isAnimating = true;

            // 取消放置模式
            CancelPlacement();

            // 恢复 TimerSection、ControlButtons、TaskSection
            MainUIController.Instance?.ExitBuildMode();

            OnPanelToggled?.Invoke(false);
        }

        /// <summary>
        /// 更新面板动画
        /// </summary>
        private void UpdatePanelAnimation()
        {
            if (panelTransform == null) return;

            Vector2 pos = panelTransform.anchoredPosition;
            float newY = Mathf.MoveTowards(pos.y, targetY, slideSpeed * Time.deltaTime);
            panelTransform.anchoredPosition = new Vector2(pos.x, newY);

            if (Mathf.Approximately(newY, targetY))
            {
                isAnimating = false;

                // 关闭动画结束后禁止拦截射线
                if (!isPanelOpen)
                {
                    SetPanelInteractable(false);
                }
            }
        }

        /// <summary>
        /// 设置面板是否可交互（控制 CanvasGroup 的 blocksRaycasts 和 interactable）
        /// </summary>
        private void SetPanelInteractable(bool interactable)
        {
            if (panelCanvasGroup != null)
            {
                panelCanvasGroup.blocksRaycasts = interactable;
                panelCanvasGroup.interactable = interactable;
            }
        }

        #endregion

        #region 建造物选择与放置

        /// <summary>
        /// 建造物项点击
        /// </summary>
        private void OnBuildingItemClicked(BuildingItemData data, int index)
        {
            if (data == null || !data.isUnlocked) return;

            // 资源不足：取消当前放置模式，显示详情面板供查看
            if (!data.isAffordable)
            {
                CancelPlacement();

                // 取消放置后再显示详情（CancelPlacement 会 Hide DetailPanel）
                if (BuildingDetailPanelUI.Instance != null)
                {
                    BuildingDetailPanelUI.Instance.Show(data.blueprint);
                }

                UpdateHintText(Get("UI_Building", "hint_insufficient_resources"));
                return;
            }

            // 始终显示建筑详情面板
            if (BuildingDetailPanelUI.Instance != null)
            {
                BuildingDetailPanelUI.Instance.Show(data.blueprint);
            }

            // 选中建造物，进入放置模式
            selectedBuilding = data;
            isPlacementMode = true;

            // 开始放置预览
            var placementController = BuildingPlacementController.Instance;
            if (placementController != null)
            {
                placementController.StartPlacement(data.blueprint);
            }

            // 显示旋转提示
            if (rotationHint != null)
            {
                rotationHint.SetActive(true);
            }

            UpdateHintText(GetSmart("UI_Building", "hint_placing", ("name", data.buildingName)));

            OnBuildingSelected?.Invoke(data.blueprint);
        }

        /// <summary>
        /// 处理放置模式输入
        /// </summary>
        private void HandlePlacementInput()
        {
            var placementController = BuildingPlacementController.Instance;
            if (placementController == null || !placementController.IsPlacing) return;

            // Q/E 旋转由 BuildingPlacementController.HandleInput() 统一处理，
            // 此处不再重复监听，避免同一帧内旋转两次。

            // ESC取消
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CancelPlacement();
            }

            // 左键点击放置（由BuildingPlacementController处理）
            // 这里监听放置成功事件
        }

        /// <summary>
        /// 确认放置（由BuildingPlacementController调用）
        /// </summary>
        public void OnPlacementConfirmed()
        {
            if (selectedBuilding == null) return;

            // 消耗资源
            if (selectedBuilding.blueprint.ConsumeBuildCost())
            {
                Debug.Log($"成功放置建筑: {selectedBuilding.buildingName}");

                // 注册建筑资源生产器
                var placementController = BuildingPlacementController.Instance;
                if (placementController?.LastPlacedBuilding != null &&
                    BuildingResourceSystemManager.Instance != null)
                {
                    var building = placementController.LastPlacedBuilding;
                    BuildingResourceSystemManager.Instance.RegisterBuilding(
                        building.InstanceId,
                        selectedBuilding.blueprint.blueprintId);
                }

                // 刷新可负担状态
                RefreshAffordableState();

                // 连续放置模式：检查是否还能继续放置
                if (selectedBuilding.blueprint.CanAfford())
                {
                    // 继续放置同一建筑
                    if (placementController != null)
                    {
                        placementController.StartPlacement(selectedBuilding.blueprint);
                    }
                    UpdateHintText(GetSmart("UI_Building", "hint_placement_continue", ("name", selectedBuilding.buildingName)));
                }
                else
                {
                    // 资源不足，退出放置模式
                    UpdateHintText(Get("UI_Building", "hint_placement_end_resources"));
                    ExitPlacementMode();
                }
            }
            else
            {
                UpdateHintText(Get("UI_Building", "hint_insufficient_resources"));
                CancelPlacement();
            }
        }

        /// <summary>
        /// 取消放置
        /// </summary>
        public void CancelPlacement()
        {
            var placementController = BuildingPlacementController.Instance;
            if (placementController != null && placementController.IsPlacing)
            {
                placementController.CancelPlacement();
            }

            ExitPlacementMode();
        }

        /// <summary>
        /// 退出放置模式
        /// </summary>
        private void ExitPlacementMode()
        {
            isPlacementMode = false;
            selectedBuilding = null;

            // 隐藏建筑详情面板
            if (BuildingDetailPanelUI.Instance != null)
            {
                BuildingDetailPanelUI.Instance.Hide();
            }

            if (buildingScrollView != null)
            {
                buildingScrollView.ClearSelection();
            }

            if (rotationHint != null)
            {
                rotationHint.SetActive(false);
            }

            UpdateHintText(Get("UI_Building", "hint_click_to_place"));
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
        /// 获取当前是否在放置模式
        /// </summary>
        public bool IsPlacementMode => isPlacementMode;

        /// <summary>
        /// 获取当前选中的建筑
        /// </summary>
        public BuildingBlueprint SelectedBlueprint => selectedBuilding?.blueprint;

        /// <summary>
        /// 设置科技等级
        /// </summary>
        public void SetTechLevel(int level)
        {
            UnlockTechLevel(level);
        }

        /// <summary>
        /// 强制刷新建造物列表
        /// </summary>
        public void RefreshBuildingList()
        {
            LoadBuildingDataByTechLevel(currentTechLevel);
        }

        #endregion
    }
}
