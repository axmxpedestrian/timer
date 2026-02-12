using UnityEngine;
using System.Collections.Generic;
using PomodoroTimer.Map.Data;
using PomodoroTimer.UI;
using PomodoroTimer.UI.Building;

namespace PomodoroTimer.Map.Sprite2D
{
    /// <summary>
    /// 建筑放置控制器
    /// 处理建筑放置预览、旋转、确认/取消
    /// </summary>
    public class BuildingPlacementController : MonoBehaviour
    {
        public static BuildingPlacementController Instance { get; private set; }

        [Header("预览设置")]
        [SerializeField] private Color validColor = new Color(0f, 1f, 0f, 0.5f);
        [SerializeField] private Color invalidColor = new Color(1f, 0f, 0f, 0.5f);
        [SerializeField] private Material previewMaterial;

        [Header("网格指示器")]
        [SerializeField] private Sprite gridIndicatorSprite;
        [SerializeField] private Color gridValidColor = new Color(0f, 1f, 0f, 0.3f);
        [SerializeField] private Color gridInvalidColor = new Color(1f, 0f, 0f, 0.3f);

        // 放置状态
        private bool isPlacing;
        private BuildingBlueprint currentBlueprint;
        private Vector2Int currentGridPos;
        private int currentRotation;
        private int currentFloorCount;

        private ModularBuildingInstance lastPlacedBuilding;
        public ModularBuildingInstance LastPlacedBuilding => lastPlacedBuilding;

        // 预览对象
        private GameObject previewObject;
        private List<SpriteRenderer> previewRenderers = new List<SpriteRenderer>();
        private List<SpriteRenderer> gridIndicators = new List<SpriteRenderer>();
        private Transform previewContainer;
        private Transform gridContainer;

        // 缓存
        private List<Vector2Int> currentConflicts = new List<Vector2Int>();
        private List<Vector2Int> currentOccupiedCells = new List<Vector2Int>();
        private Material previewMaterialInstance;

        // Shader属性ID
        private static readonly int PreviewColorId = Shader.PropertyToID("_PreviewColor");
        private static readonly int InvalidColorId = Shader.PropertyToID("_InvalidColor");
        private static readonly int IsValidId = Shader.PropertyToID("_IsValid");

        // 事件
        public event System.Action<BuildingBlueprint, Vector2Int, int> OnPlacementConfirmed;
        public event System.Action OnPlacementCancelled;
        public event System.Action<Vector2Int, bool> OnPreviewUpdated;

        public bool IsPlacing => isPlacing;
        public BuildingBlueprint CurrentBlueprint => currentBlueprint;
        public Vector2Int CurrentGridPosition => currentGridPos;
        public int CurrentRotation => currentRotation;
        public bool CanPlace => currentConflicts.Count == 0;

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

            CreateContainers();
            CreatePreviewMaterial();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;

            ClearPreview();
            if (previewMaterialInstance != null)
                Destroy(previewMaterialInstance);
        }

        private void Update()
        {
            if (!isPlacing) return;

            HandleInput();
            UpdatePreviewPosition();
        }

        private void CreateContainers()
        {
            previewObject = new GameObject("BuildingPreview");
            previewObject.transform.SetParent(transform);
            previewObject.SetActive(false);

            previewContainer = new GameObject("PreviewSprites").transform;
            previewContainer.SetParent(previewObject.transform);
            previewContainer.localPosition = Vector3.zero;

            gridContainer = new GameObject("GridIndicators").transform;
            gridContainer.SetParent(previewObject.transform);
            gridContainer.localPosition = Vector3.zero;
        }

        private void CreatePreviewMaterial()
        {
            if (previewMaterial != null)
            {
                previewMaterialInstance = new Material(previewMaterial);
            }
            else
            {
                var shader = Shader.Find("Custom/BuildingPreview");
                if (shader != null)
                {
                    previewMaterialInstance = new Material(shader);
                }
                else
                {
                    previewMaterialInstance = new Material(Shader.Find("Sprites/Default"));
                }
            }

            previewMaterialInstance.SetColor(PreviewColorId, validColor);
            previewMaterialInstance.SetColor(InvalidColorId, invalidColor);
        }

        #region 放置流程

        /// <summary>
        /// 开始放置建筑
        /// </summary>
        public void StartPlacement(BuildingBlueprint blueprint, int floorCount = -1)
        {
            if (blueprint == null) return;

            // 如果已经在放置，先取消
            if (isPlacing)
            {
                CancelPlacement();
            }

            currentBlueprint = blueprint;
            currentRotation = 0;
            currentFloorCount = floorCount > 0 ? floorCount : blueprint.defaultFloorCount;
            isPlacing = true;

            // 隐藏干扰面板
            MainUIController.Instance?.EnterBuildMode();

            BuildPreviewVisuals();
            previewObject.SetActive(true);

            // 初始位置设为屏幕中心对应的网格位置
            var mapManager = IsometricSpriteMapManager.Instance;
            if (mapManager != null)
            {
                currentGridPos = mapManager.ScreenToGrid(new Vector3(Screen.width / 2f, Screen.height / 2f, 0));
            }

            UpdatePreview();
        }

        /// <summary>
        /// 开始放置建筑（通过ID）
        /// </summary>
        public void StartPlacement(int blueprintId, int floorCount = -1)
        {
            var blueprint = ModularBuildingManager.Instance?.GetBlueprint(blueprintId);
            if (blueprint != null)
            {
                StartPlacement(blueprint, floorCount);
            }
        }

        /// <summary>
        /// 确认放置（仅检查冲突，不消耗资源）
        /// </summary>
        public bool ConfirmPlacement()
        {
            if (!isPlacing || !CanPlace) return false;

            var manager = ModularBuildingManager.Instance;
            if (manager == null) return false;

            // 放置建筑（资源消耗由BuildingPanelUI处理）
            var building = manager.PlaceBuilding(currentBlueprint, currentGridPos,
                currentRotation, currentFloorCount);

            if (building != null)
            {
                lastPlacedBuilding = building;
                OnPlacementConfirmed?.Invoke(currentBlueprint, currentGridPos, currentRotation);

                // 播放放置音效
                if (currentBlueprint.placeSound != null)
                {
                    BuildingAudioManager.Instance?.PlaySound(currentBlueprint.placeSound,
                        building.transform.position);
                }

                // 不结束放置模式，由BuildingPanelUI决定是否继续
                return true;
            }

            return false;
        }

        /// <summary>
        /// 确认放置并结束放置模式
        /// </summary>
        public bool ConfirmPlacementAndEnd()
        {
            if (ConfirmPlacement())
            {
                EndPlacement();
                return true;
            }
            return false;
        }

        /// <summary>
        /// 取消放置
        /// </summary>
        public void CancelPlacement()
        {
            if (!isPlacing) return;

            OnPlacementCancelled?.Invoke();
            EndPlacement();
        }

        private void EndPlacement()
        {
            isPlacing = false;
            currentBlueprint = null;
            previewObject.SetActive(false);
            ClearPreview();

            // 恢复面板（仅在销毁模式也未激活时）
            if (DemolishController.Instance == null || !DemolishController.Instance.IsDemolishMode)
            {
                MainUIController.Instance?.ExitBuildMode();
            }
        }

        #endregion

        #region 输入处理

        private void HandleInput()
        {
            // 旋转 - Q键逆时针，E键顺时针，R键90度
            // 注意：鼠标中键已用于拖动视角（MapInputController），不再绑定旋转
            if (Input.GetKeyDown(KeyCode.Q))
            {
                SetRotation(currentRotation - 90);
            }
            else if (Input.GetKeyDown(KeyCode.E))
            {
                SetRotation(currentRotation + 90);
            }
            else if (Input.GetKeyDown(KeyCode.R))
            {
                Rotate90();
            }

            // 确认 - 左键点击
            if (Input.GetMouseButtonDown(0))
            {
                // 检查是否点击在有效网格内
                var mapManager = IsometricSpriteMapManager.Instance;
                if (mapManager != null)
                {
                    Vector2Int clickGridPos = mapManager.ScreenToGrid(Input.mousePosition);

                    // 检查是否点击在UI上（如果是则不处理）
                    if (UnityEngine.EventSystems.EventSystem.current != null &&
                        UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                    {
                        return;
                    }

                    // 检查是否在有效网格范围内
                    if (mapManager.IsValidGridPosition(clickGridPos))
                    {
                        if (ConfirmPlacement())
                        {
                            // 通知BuildingPanelUI放置成功
                            var panelUI = UI.Building.BuildingPanelUI.Instance;
                            if (panelUI != null)
                            {
                                panelUI.OnPlacementConfirmed();
                            }
                        }
                    }
                    else
                    {
                        // 点击网格外部，取消放置
                        CancelPlacement();
                    }
                }
                else
                {
                    ConfirmPlacement();
                }
            }

            // 取消 - 右键或ESC
            if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
            {
                CancelPlacement();
            }

            // 楼层调整 - 滚轮（如果是多层建筑）
            if (currentBlueprint != null && currentBlueprint.IsMultiFloor)
            {
                float scroll = Input.GetAxis("Mouse ScrollWheel");
                if (scroll > 0.1f)
                {
                    AdjustFloorCount(1);
                }
                else if (scroll < -0.1f)
                {
                    AdjustFloorCount(-1);
                }
            }
        }

        /// <summary>
        /// 旋转90度
        /// </summary>
        public void Rotate90()
        {
            currentRotation = (currentRotation + 90) % 360;
            BuildPreviewVisuals();
            UpdatePreview();
        }

        /// <summary>
        /// 设置旋转角度
        /// </summary>
        public void SetRotation(int rotation)
        {
            currentRotation = ((rotation % 360) + 360) % 360;
            currentRotation = (currentRotation / 90) * 90;
            BuildPreviewVisuals();
            UpdatePreview();
        }

        /// <summary>
        /// 调整楼层数
        /// </summary>
        public void AdjustFloorCount(int delta)
        {
            if (currentBlueprint == null || !currentBlueprint.IsMultiFloor) return;

            int maxFloors = currentBlueprint.floors?.Length ?? 1;
            currentFloorCount = Mathf.Clamp(currentFloorCount + delta, 1, maxFloors);
            BuildPreviewVisuals();
            UpdatePreview();
        }

        /// <summary>
        /// 移动预览到指定网格位置
        /// </summary>
        public void MovePreviewTo(Vector2Int gridPos)
        {
            currentGridPos = gridPos;
            UpdatePreview();
        }

        #endregion

        #region 预览更新

        private void UpdatePreviewPosition()
        {
            var mapManager = IsometricSpriteMapManager.Instance;
            if (mapManager == null) return;

            // 获取鼠标位置对应的网格坐标
            Vector2Int newGridPos = mapManager.ScreenToGrid(Input.mousePosition);

            if (newGridPos != currentGridPos)
            {
                currentGridPos = newGridPos;
                UpdatePreview();
            }
        }

        private void UpdatePreview()
        {
            if (!isPlacing || currentBlueprint == null) return;

            var mapManager = IsometricSpriteMapManager.Instance;
            var buildingManager = ModularBuildingManager.Instance;
            if (mapManager == null) return;

            // 更新世界位置
            Vector3 worldPos = mapManager.GridToWorld(currentGridPos);

            // 修正Y偏移：GridToWorld返回菱形底部顶点，建筑Sprite底部对应菱形中心线
            float tileHalfHeight = (mapManager.GetTileHeight() / 2f) / mapManager.GetPixelsPerUnit();
            worldPos.y -= tileHalfHeight;

            worldPos.y += currentBlueprint.yOffset / mapManager.GetPixelsPerUnit();
            previewObject.transform.position = worldPos;

            // 检查冲突
            if (buildingManager != null)
            {
                currentConflicts = buildingManager.GetConflictingCells(currentBlueprint,
                    currentGridPos, currentRotation);
                currentOccupiedCells = buildingManager.GetOccupiedCells(currentBlueprint,
                    currentGridPos, currentRotation);
            }

            // 更新预览材质
            bool isValid = currentConflicts.Count == 0;
            previewMaterialInstance.SetFloat(IsValidId, isValid ? 1f : 0f);

            // 更新网格指示器
            UpdateGridIndicators();

            OnPreviewUpdated?.Invoke(currentGridPos, isValid);
        }

        #endregion

        #region 预览视觉构建

        private void BuildPreviewVisuals()
        {
            ClearPreviewRenderers();

            if (currentBlueprint == null) return;

            // 构建部件预览（使用当前旋转角度选择对应方向的 sprite 和偏移）
            if (currentBlueprint.partSlots != null)
            {
                foreach (var slot in currentBlueprint.partSlots)
                {
                    var variant = slot.GetDefaultVariant();
                    if (variant == null) continue;

                    Sprite frame = variant.GetFrameForRotation(0, currentRotation);
                    if (frame == null) continue;

                    Vector2 offset = variant.GetLocalOffsetForRotation(currentRotation);
                    int sortOffset = variant.GetSortingOrderOffsetForRotation(currentRotation);
                    CreatePreviewSprite(frame, offset, sortOffset);
                }
            }

            // 构建多层预览
            if (currentBlueprint.floors != null)
            {
                var mapManager = IsometricSpriteMapManager.Instance;
                float ppu = mapManager?.GetPixelsPerUnit() ?? 32f;

                for (int i = 0; i < currentFloorCount && i < currentBlueprint.floors.Length; i++)
                {
                    var floor = currentBlueprint.floors[i];
                    if (floor.partSlots == null) continue;

                    foreach (var slot in floor.partSlots)
                    {
                        var variant = slot.GetDefaultVariant();
                        if (variant == null) continue;

                        Sprite frame = variant.GetFrameForRotation(0, currentRotation);
                        if (frame == null) continue;

                        Vector2 offset = variant.GetLocalOffsetForRotation(currentRotation);
                        offset.y += floor.heightOffset / ppu;
                        int sortOffset = floor.sortingOrderBase
                            + variant.GetSortingOrderOffsetForRotation(currentRotation);
                        CreatePreviewSprite(frame, offset, sortOffset);
                    }
                }
            }

            // 如果没有部件，使用预览图标
            if (previewRenderers.Count == 0 && currentBlueprint.previewIcon != null)
            {
                CreatePreviewSprite(currentBlueprint.previewIcon, Vector2.zero, 0);
            }

            // 构建网格指示器
            RebuildGridIndicators();
        }

        private void CreatePreviewSprite(Sprite sprite, Vector2 offset, int sortingOrder)
        {
            var go = new GameObject("PreviewPart");
            go.transform.SetParent(previewContainer);
            go.transform.localPosition = new Vector3(offset.x, offset.y, 0);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.material = previewMaterialInstance;
            sr.sortingLayerName = IsometricSortingHelper.LAYER_EFFECTS;
            sr.sortingOrder = sortingOrder;

            previewRenderers.Add(sr);
        }

        private void RebuildGridIndicators()
        {
            ClearGridIndicators();

            if (currentBlueprint == null) return;

            var mask = currentBlueprint.GetRotatedMask(currentRotation);
            var mapManager = IsometricSpriteMapManager.Instance;
            if (mapManager == null) return;

            foreach (var offset in mask.GetOccupiedPositions())
            {
                CreateGridIndicator(offset);
            }
        }

        private void CreateGridIndicator(Vector2Int offset)
        {
            var mapManager = IsometricSpriteMapManager.Instance;
            if (mapManager == null) return;

            var go = new GameObject($"GridIndicator_{offset.x}_{offset.y}");
            go.transform.SetParent(gridContainer);

            // 计算相对于建筑原点的世界偏移
            Vector3 basePos = mapManager.GridToWorld(Vector2Int.zero);
            Vector3 offsetPos = mapManager.GridToWorld(offset);
            go.transform.localPosition = offsetPos - basePos;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = gridIndicatorSprite ?? CreateDefaultGridSprite();
            sr.sortingLayerName = IsometricSortingHelper.LAYER_GROUND_DECORATION;
            sr.sortingOrder = 9999;

            gridIndicators.Add(sr);
        }

        private void UpdateGridIndicators()
        {
            var mask = currentBlueprint?.GetRotatedMask(currentRotation);
            if (mask == null) return;

            var occupiedPositions = mask.GetOccupiedPositions();

            for (int i = 0; i < gridIndicators.Count && i < occupiedPositions.Count; i++)
            {
                var worldCell = currentGridPos + occupiedPositions[i];
                bool isConflict = currentConflicts.Contains(worldCell);
                gridIndicators[i].color = isConflict ? gridInvalidColor : gridValidColor;
            }
        }

        private Sprite CreateDefaultGridSprite()
        {
            // 优先从实际地砖Sprite读取像素尺寸和PPU，确保网格指示器与地砖完全匹配
            var mapManager = IsometricSpriteMapManager.Instance;
            Sprite tileSprite = mapManager != null ? mapManager.GetDefaultTileSprite() : null;

            int width, height;
            float ppu;

            if (tileSprite != null)
            {
                // 使用实际地砖sprite的像素尺寸和PPU
                width  = Mathf.RoundToInt(tileSprite.rect.width);
                height = Mathf.RoundToInt(tileSprite.rect.height);
                ppu    = tileSprite.pixelsPerUnit;
            }
            else
            {
                // 回退：用地图管理器的配置值
                width  = mapManager != null ? Mathf.RoundToInt(mapManager.GetTileWidth())  : 128;
                height = mapManager != null ? Mathf.RoundToInt(mapManager.GetTileHeight()) : 64;
                ppu    = mapManager != null ? mapManager.GetPixelsPerUnit() : 32f;
            }

            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;

            Color transparent = new Color(0, 0, 0, 0);
            Color white = Color.white;

            // 填充透明
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    texture.SetPixel(x, y, transparent);
                }
            }

            // 绘制菱形边框（线宽2像素，更清晰）
            int halfW = width / 2;
            int halfH = height / 2;
            int centerX = width / 2;
            int centerY = height / 2;

            for (int i = 0; i <= halfW; i++)
            {
                int dy = i * halfH / halfW;

                for (int t = -1; t <= 1; t++)
                {
                    // 上半菱形边
                    int uy = centerY + dy + t;
                    if (uy >= 0 && uy < height)
                    {
                        if (centerX + i < width) texture.SetPixel(centerX + i, uy, white);
                        if (centerX - i >= 0)    texture.SetPixel(centerX - i, uy, white);
                    }
                    // 下半菱形边
                    int ly = centerY - dy + t;
                    if (ly >= 0 && ly < height)
                    {
                        if (centerX + i < width) texture.SetPixel(centerX + i, ly, white);
                        if (centerX - i >= 0)    texture.SetPixel(centerX - i, ly, white);
                    }
                }
            }

            texture.Apply();

            return Sprite.Create(texture, new Rect(0, 0, width, height),
                new Vector2(0.5f, 0.5f), ppu);
        }

        #endregion

        #region 清理

        private void ClearPreview()
        {
            ClearPreviewRenderers();
            ClearGridIndicators();
        }

        private void ClearPreviewRenderers()
        {
            foreach (var sr in previewRenderers)
            {
                if (sr != null && sr.gameObject != null)
                    Destroy(sr.gameObject);
            }
            previewRenderers.Clear();
        }

        private void ClearGridIndicators()
        {
            foreach (var sr in gridIndicators)
            {
                if (sr != null && sr.gameObject != null)
                    Destroy(sr.gameObject);
            }
            gridIndicators.Clear();
        }

        #endregion
    }
}
