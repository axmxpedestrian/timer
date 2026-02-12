using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using PomodoroTimer.Core;
using PomodoroTimer.Map;
using PomodoroTimer.Map.Sprite2D;
using PomodoroTimer.Resource;

namespace PomodoroTimer.UI.Building
{
    /// <summary>
    /// 建筑销毁控制器
    /// 处理销毁模式下的建筑选中（点击/框选）、红色蒙版、资源返还
    /// </summary>
    public class DemolishController : MonoBehaviour
    {
        public static DemolishController Instance { get; private set; }

        [Header("返还设置")]
        [Tooltip("销毁建筑返还建造资源的比例（0~1）")]
        [SerializeField] private float refundRatio = 0.5f;

        [Header("选中蒙版")]
        [SerializeField] private Color selectedTintColor = new Color(1f, 0.2f, 0.2f, 0.7f);
        [SerializeField] private Color normalTintColor = Color.white;

        [Header("框选可视化")]
        [SerializeField] private Color boxSelectColor = new Color(1f, 1f, 0f, 0.25f);
        [SerializeField] private Color boxSelectBorderColor = new Color(1f, 1f, 0f, 0.8f);

        // 状态
        private bool isDemolishMode = false;
        private HashSet<int> selectedBuildingIds = new HashSet<int>();
        private Dictionary<int, List<SpriteRenderer>> buildingRendererCache = new Dictionary<int, List<SpriteRenderer>>();

        // 框选
        private bool isDragging = false;
        private Vector2 dragStartScreen;
        private const float DRAG_THRESHOLD = 10f; // 像素，超过此距离视为框选

        // 框选可视化
        private GameObject boxSelectOverlay;
        private RectTransform boxSelectRect;
        private Image boxSelectFill;
        private Outline boxSelectOutline;

        // 事件
        public event System.Action<bool> OnDemolishModeChanged;
        public event System.Action<int> OnSelectionChanged; // 参数：选中数量

        public bool IsDemolishMode => isDemolishMode;
        public int SelectedCount => selectedBuildingIds.Count;
        public float RefundRatio => refundRatio;

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

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void Update()
        {
            if (!isDemolishMode) return;

            HandleInput();
        }

        #region 模式控制

        /// <summary>
        /// 进入销毁模式
        /// </summary>
        public void EnterDemolishMode()
        {
            if (isDemolishMode) return;

            // 如果正在放置建筑，先取消
            if (BuildingPlacementController.Instance != null &&
                BuildingPlacementController.Instance.IsPlacing)
            {
                BuildingPlacementController.Instance.CancelPlacement();
            }

            // 关闭建造面板（取消放置 + 收起面板）
            if (BuildingPanelUI.Instance != null)
            {
                BuildingPanelUI.Instance.CancelPlacement();
                BuildingPanelUI.Instance.ClosePanel();
            }

            isDemolishMode = true;
            ClearSelection();

            // 隐藏干扰面板
            MainUIController.Instance?.EnterBuildMode();

            // 禁用左键拖动视角，避免与框选冲突
            MapInputController.Instance?.SetLeftClickDragEnabled(false);

            OnDemolishModeChanged?.Invoke(true);

            Debug.Log("[DemolishController] 进入销毁模式");
        }

        /// <summary>
        /// 退出销毁模式
        /// </summary>
        public void ExitDemolishMode()
        {
            if (!isDemolishMode) return;

            ClearSelection();
            isDemolishMode = false;
            isDragging = false;

            // 恢复左键拖动视角
            MapInputController.Instance?.SetLeftClickDragEnabled(true);

            // 恢复面板（仅在放置模式也未激活时）
            if (BuildingPlacementController.Instance == null || !BuildingPlacementController.Instance.IsPlacing)
            {
                MainUIController.Instance?.ExitBuildMode();
            }

            OnDemolishModeChanged?.Invoke(false);

            Debug.Log("[DemolishController] 退出销毁模式");
        }

        /// <summary>
        /// 切换销毁模式
        /// </summary>
        public void ToggleDemolishMode()
        {
            if (isDemolishMode)
                ExitDemolishMode();
            else
                EnterDemolishMode();
        }

        #endregion

        #region 输入处理

        private void HandleInput()
        {
            // ESC 退出
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                ExitDemolishMode();
                return;
            }

            // 忽略 UI 上的点击
            if (UnityEngine.EventSystems.EventSystem.current != null &&
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            // 鼠标按下 - 开始记录
            if (Input.GetMouseButtonDown(0))
            {
                dragStartScreen = Input.mousePosition;
                isDragging = false;
            }

            // 鼠标拖拽中 - 检测是否超过阈值
            if (Input.GetMouseButton(0))
            {
                float dist = Vector2.Distance(dragStartScreen, Input.mousePosition);
                if (dist > DRAG_THRESHOLD)
                {
                    isDragging = true;
                    UpdateBoxSelectVisual(dragStartScreen, Input.mousePosition);
                }
            }

            // 鼠标抬起 - 执行选中
            if (Input.GetMouseButtonUp(0))
            {
                HideBoxSelectVisual();
                if (isDragging)
                {
                    HandleBoxSelect(dragStartScreen, Input.mousePosition);
                }
                else
                {
                    HandleClickSelect(Input.mousePosition);
                }
                isDragging = false;
            }

            // 右键取消全部选中
            if (Input.GetMouseButtonDown(1))
            {
                ClearSelection();
            }
        }

        /// <summary>
        /// 点击选中
        /// </summary>
        private void HandleClickSelect(Vector2 screenPos)
        {
            var mapManager = IsometricSpriteMapManager.Instance;
            var buildingManager = ModularBuildingManager.Instance;
            if (mapManager == null || buildingManager == null) return;

            Vector2Int gridPos = mapManager.ScreenToGrid(screenPos);
            var building = buildingManager.GetBuildingAt(gridPos);

            if (building != null)
            {
                ToggleBuildingSelection(building);
            }
        }

        /// <summary>
        /// 框选建筑 - 检查建筑所有占用格子是否有任意一个落在框选范围内
        /// </summary>
        private void HandleBoxSelect(Vector2 startScreen, Vector2 endScreen)
        {
            var mapManager = IsometricSpriteMapManager.Instance;
            var buildingManager = ModularBuildingManager.Instance;
            if (mapManager == null || buildingManager == null) return;

            Camera cam = mapManager.GetCamera();
            if (cam == null) return;

            // 计算屏幕矩形
            float minX = Mathf.Min(startScreen.x, endScreen.x);
            float maxX = Mathf.Max(startScreen.x, endScreen.x);
            float minY = Mathf.Min(startScreen.y, endScreen.y);
            float maxY = Mathf.Max(startScreen.y, endScreen.y);
            Rect screenRect = new Rect(minX, minY, maxX - minX, maxY - minY);

            // 遍历所有建筑
            foreach (var building in buildingManager.GetAllBuildings())
            {
                if (building == null || building.IsDestroyed) continue;

                // 检查建筑占用的每个格子，任意一个在框选范围内即选中
                bool hit = false;
                foreach (var gridPos in building.GetOccupiedGridPositions())
                {
                    Vector3 cellWorld = mapManager.GridToWorld(gridPos);
                    Vector3 cellScreen = cam.WorldToScreenPoint(cellWorld);
                    if (screenRect.Contains(new Vector2(cellScreen.x, cellScreen.y)))
                    {
                        hit = true;
                        break;
                    }
                }

                if (hit)
                {
                    SelectBuilding(building);
                }
            }
        }

        #endregion

        #region 选中管理

        /// <summary>
        /// 切换建筑选中状态
        /// </summary>
        private void ToggleBuildingSelection(ModularBuildingInstance building)
        {
            if (selectedBuildingIds.Contains(building.InstanceId))
            {
                DeselectBuilding(building);
            }
            else
            {
                SelectBuilding(building);
            }
        }

        /// <summary>
        /// 选中建筑
        /// </summary>
        private void SelectBuilding(ModularBuildingInstance building)
        {
            if (building == null || selectedBuildingIds.Contains(building.InstanceId)) return;

            selectedBuildingIds.Add(building.InstanceId);
            ApplyRedTint(building, true);
            OnSelectionChanged?.Invoke(selectedBuildingIds.Count);
        }

        /// <summary>
        /// 取消选中建筑
        /// </summary>
        private void DeselectBuilding(ModularBuildingInstance building)
        {
            if (building == null || !selectedBuildingIds.Contains(building.InstanceId)) return;

            selectedBuildingIds.Remove(building.InstanceId);
            ApplyRedTint(building, false);
            OnSelectionChanged?.Invoke(selectedBuildingIds.Count);
        }

        /// <summary>
        /// 清除所有选中
        /// </summary>
        public void ClearSelection()
        {
            var buildingManager = ModularBuildingManager.Instance;
            if (buildingManager != null)
            {
                foreach (int id in selectedBuildingIds)
                {
                    var building = buildingManager.GetBuilding(id);
                    if (building != null)
                    {
                        ApplyRedTint(building, false);
                    }
                }
            }

            selectedBuildingIds.Clear();
            buildingRendererCache.Clear();
            OnSelectionChanged?.Invoke(0);
        }

        /// <summary>
        /// 应用/移除红色蒙版
        /// </summary>
        private void ApplyRedTint(ModularBuildingInstance building, bool selected)
        {
            var renderers = GetBuildingRenderers(building);
            Color targetColor = selected ? selectedTintColor : normalTintColor;

            foreach (var sr in renderers)
            {
                if (sr != null)
                {
                    sr.color = targetColor;
                }
            }
        }

        /// <summary>
        /// 获取建筑的所有 SpriteRenderer（带缓存）
        /// </summary>
        private List<SpriteRenderer> GetBuildingRenderers(ModularBuildingInstance building)
        {
            if (buildingRendererCache.TryGetValue(building.InstanceId, out var cached))
            {
                return cached;
            }

            var renderers = new List<SpriteRenderer>(building.GetComponentsInChildren<SpriteRenderer>());
            buildingRendererCache[building.InstanceId] = renderers;
            return renderers;
        }

        #endregion

        #region 销毁执行

        /// <summary>
        /// 获取选中建筑的返还资源汇总
        /// </summary>
        public Dictionary<ResourceType, long> GetRefundSummary()
        {
            var summary = new Dictionary<ResourceType, long>();
            var buildingManager = ModularBuildingManager.Instance;
            if (buildingManager == null) return summary;

            foreach (int id in selectedBuildingIds)
            {
                var building = buildingManager.GetBuilding(id);
                if (building?.Blueprint?.buildCosts == null) continue;

                foreach (var cost in building.Blueprint.buildCosts)
                {
                    long refund = (long)(cost.amount * refundRatio);
                    if (refund <= 0) continue;

                    if (summary.ContainsKey(cost.resourceType))
                        summary[cost.resourceType] += refund;
                    else
                        summary[cost.resourceType] = refund;
                }
            }

            return summary;
        }

        /// <summary>
        /// 获取选中建筑的名称列表
        /// </summary>
        public List<string> GetSelectedBuildingNames()
        {
            var names = new List<string>();
            var buildingManager = ModularBuildingManager.Instance;
            if (buildingManager == null) return names;

            foreach (int id in selectedBuildingIds)
            {
                var building = buildingManager.GetBuilding(id);
                if (building?.Blueprint != null)
                {
                    names.Add(building.Blueprint.buildingName);
                }
            }

            return names;
        }

        /// <summary>
        /// 确认销毁选中的建筑
        /// </summary>
        public void ConfirmDemolish()
        {
            var buildingManager = ModularBuildingManager.Instance;
            var resourceManager = ResourceManager.Instance;
            if (buildingManager == null) return;

            // 返还资源
            if (resourceManager != null)
            {
                var refunds = GetRefundSummary();
                foreach (var kvp in refunds)
                {
                    resourceManager.AddResource(kvp.Key, kvp.Value, "BuildingDemolish");
                }
            }

            // 移除建筑（复制列表避免迭代中修改）
            var idsToRemove = new List<int>(selectedBuildingIds);
            foreach (int id in idsToRemove)
            {
                buildingManager.RemoveBuilding(id);
            }

            Debug.Log($"[DemolishController] 已销毁 {idsToRemove.Count} 个建筑");

            // 清除选中状态
            selectedBuildingIds.Clear();
            buildingRendererCache.Clear();
            OnSelectionChanged?.Invoke(0);

            // 触发存档
            if (DataManager.Instance != null)
            {
                DataManager.Instance.Save();
            }
        }

        #endregion

        #region 框选可视化

        /// <summary>
        /// 创建框选可视化 UI（挂在场景中第一个 Canvas 下）
        /// </summary>
        private void EnsureBoxSelectOverlay()
        {
            if (boxSelectOverlay != null) return;

            // 找到场景中的 Canvas
            var canvas = FindObjectOfType<Canvas>();
            if (canvas == null) return;

            boxSelectOverlay = new GameObject("BoxSelectOverlay");
            boxSelectOverlay.transform.SetParent(canvas.transform, false);

            // 填充区域
            boxSelectRect = boxSelectOverlay.AddComponent<RectTransform>();
            boxSelectFill = boxSelectOverlay.AddComponent<Image>();
            boxSelectFill.color = boxSelectColor;
            boxSelectFill.raycastTarget = false;

            // 边框（用 Outline 组件模拟）
            boxSelectOutline = boxSelectOverlay.AddComponent<Outline>();
            boxSelectOutline.effectColor = boxSelectBorderColor;
            boxSelectOutline.effectDistance = new Vector2(1.5f, 1.5f);

            boxSelectOverlay.SetActive(false);
        }

        /// <summary>
        /// 更新框选矩形显示
        /// </summary>
        private void UpdateBoxSelectVisual(Vector2 startScreen, Vector2 currentScreen)
        {
            EnsureBoxSelectOverlay();
            if (boxSelectOverlay == null) return;

            boxSelectOverlay.SetActive(true);

            // 屏幕坐标转 Canvas 局部坐标
            var canvas = boxSelectOverlay.GetComponentInParent<Canvas>();
            if (canvas == null) return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform, startScreen, canvas.worldCamera, out Vector2 localStart);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform, currentScreen, canvas.worldCamera, out Vector2 localEnd);

            Vector2 min = Vector2.Min(localStart, localEnd);
            Vector2 max = Vector2.Max(localStart, localEnd);

            boxSelectRect.anchorMin = new Vector2(0.5f, 0.5f);
            boxSelectRect.anchorMax = new Vector2(0.5f, 0.5f);
            boxSelectRect.pivot = new Vector2(0.5f, 0.5f);
            boxSelectRect.anchoredPosition = (min + max) / 2f;
            boxSelectRect.sizeDelta = max - min;
        }

        /// <summary>
        /// 隐藏框选矩形
        /// </summary>
        private void HideBoxSelectVisual()
        {
            if (boxSelectOverlay != null)
            {
                boxSelectOverlay.SetActive(false);
            }
        }

        #endregion
    }
}
