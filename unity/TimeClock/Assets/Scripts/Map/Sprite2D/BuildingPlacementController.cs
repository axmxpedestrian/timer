using UnityEngine;
using System.Collections.Generic;
using PomodoroTimer.Map.Data;
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
        }

        #endregion

        #region 输入处理

        private void HandleInput()
        {
            // 旋转 - Q键逆时针，E键顺时针，R键或鼠标中键90度
            if (Input.GetKeyDown(KeyCode.Q))
            {
                SetRotation(currentRotation - 90);
            }
            else if (Input.GetKeyDown(KeyCode.E))
            {
                SetRotation(currentRotation + 90);
            }
            else if (Input.GetKeyDown(KeyCode.R) || Input.GetMouseButtonDown(2))
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
            RebuildGridIndicators();
            UpdatePreview();
        }

        /// <summary>
        /// 设置旋转角度
        /// </summary>
        public void SetRotation(int rotation)
        {
            currentRotation = ((rotation % 360) + 360) % 360;
            currentRotation = (currentRotation / 90) * 90;
            RebuildGridIndicators();
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

            // 构建部件预览
            if (currentBlueprint.partSlots != null)
            {
                foreach (var slot in currentBlueprint.partSlots)
                {
                    var variant = slot.GetDefaultVariant();
                    if (variant != null && variant.frames != null && variant.frames.Length > 0)
                    {
                        CreatePreviewSprite(variant.frames[0], variant.localOffset, variant.sortingOrderOffset);
                    }
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
                        if (variant != null && variant.frames != null && variant.frames.Length > 0)
                        {
                            Vector2 offset = variant.localOffset;
                            offset.y += floor.heightOffset / ppu;
                            CreatePreviewSprite(variant.frames[0], offset,
                                floor.sortingOrderBase + variant.sortingOrderOffset);
                        }
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
            // 创建一个简单的菱形指示器
            int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;

            Color transparent = new Color(0, 0, 0, 0);
            Color white = Color.white;

            // 填充透明
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    texture.SetPixel(x, y, transparent);
                }
            }

            // 绘制菱形边框
            int halfW = size / 2;
            int halfH = size / 4;
            int centerX = size / 2;
            int centerY = size / 2;

            for (int i = 0; i <= halfW; i++)
            {
                int y1 = centerY + (i * halfH / halfW);
                int y2 = centerY - (i * halfH / halfW);

                texture.SetPixel(centerX + i, y1, white);
                texture.SetPixel(centerX - i, y1, white);
                texture.SetPixel(centerX + i, y2, white);
                texture.SetPixel(centerX - i, y2, white);
            }

            texture.Apply();

            return Sprite.Create(texture, new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f), 32f);
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
