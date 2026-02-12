using UnityEngine;
using System.Collections.Generic;
using PomodoroTimer.Core;
using PomodoroTimer.Map.Data;
using PomodoroTimer.Resource;

namespace PomodoroTimer.Map.Sprite2D
{
    /// <summary>
    /// 模块化建筑管理器
    /// 负责建筑的放置、移除、查询和冲突检测
    /// </summary>
    public class ModularBuildingManager : MonoBehaviour
    {
        public static ModularBuildingManager Instance { get; private set; }

        [Header("设置")]
        [SerializeField] private Material buildingMaterial;
        [SerializeField] private int poolInitialSize = 64;
        [SerializeField] private int poolMaxSize = 512;

        [Header("建筑模板")]
        [SerializeField] private BuildingBlueprint[] blueprintList;

        private ModularBuildingPool buildingPool;
        private Dictionary<int, ModularBuildingInstance> activeBuildings;
        private Dictionary<Vector2Int, int> gridToBuilding;
        private Dictionary<int, BuildingBlueprint> blueprintDict;
        private int nextBuildingId = 1;

        // 事件
        public event System.Action<ModularBuildingInstance> OnBuildingPlaced;
        public event System.Action<ModularBuildingInstance> OnBuildingRemoved;
        public event System.Action<ModularBuildingInstance, ModularBuildingState> OnBuildingStateChanged;

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

            activeBuildings = new Dictionary<int, ModularBuildingInstance>();
            gridToBuilding = new Dictionary<Vector2Int, int>();
            blueprintDict = new Dictionary<int, BuildingBlueprint>();
        }

        private void Start()
        {
            InitializePool();
            InitializeBlueprintDict();

            // 初始化完成后，主动从存档加载建筑数据
            // 解决：代码热重载/场景重载后 Manager 被重新创建导致建筑丢失
            TryLoadFromSaveData();
        }

        /// <summary>
        /// 尝试从 DataManager 的存档中加载建筑数据
        /// </summary>
        private void TryLoadFromSaveData()
        {
            var dataManager = DataManager.Instance;
            if (dataManager == null) return;

            var saveData = dataManager.GetBuildingSystemSaveData();
            if (saveData != null && saveData.buildings != null && saveData.buildings.Count > 0)
            {
                LoadFromSaveData(saveData);
                Debug.Log($"[ModularBuildingManager] 从存档恢复了 {activeBuildings.Count} 个建筑");
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
            buildingPool?.Clear();
        }

        private void InitializePool()
        {
            Transform parent = IsometricSpriteMapManager.Instance?.GetBuildingsParent() ?? transform;
            buildingPool = new ModularBuildingPool(parent, buildingMaterial, poolInitialSize, poolMaxSize);
        }

        private void InitializeBlueprintDict()
        {
            blueprintDict.Clear();
            if (blueprintList == null) return;

            foreach (var blueprint in blueprintList)
            {
                if (blueprint != null && blueprint.blueprintId > 0)
                {
                    if (blueprintDict.ContainsKey(blueprint.blueprintId))
                    {
                        Debug.LogWarning($"[ModularBuildingManager] 重复的建筑模板ID: {blueprint.blueprintId}");
                        continue;
                    }
                    blueprintDict[blueprint.blueprintId] = blueprint;
                }
            }

            Debug.Log($"[ModularBuildingManager] 已加载 {blueprintDict.Count} 个建筑模板");
        }

        #region 建筑放置

        /// <summary>
        /// 放置建筑（通过模板ID）
        /// </summary>
        public ModularBuildingInstance PlaceBuilding(int blueprintId, Vector2Int gridPos,
            int rotation = 0, int floorCount = -1)
        {
            var blueprint = GetBlueprint(blueprintId);
            if (blueprint == null)
            {
                Debug.LogWarning($"[ModularBuildingManager] 未找到建筑模板: {blueprintId}");
                return null;
            }
            return PlaceBuilding(blueprint, gridPos, rotation, floorCount);
        }

        /// <summary>
        /// 放置建筑（通过模板对象）
        /// </summary>
        public ModularBuildingInstance PlaceBuilding(BuildingBlueprint blueprint, Vector2Int gridPos,
            int rotation = 0, int floorCount = -1)
        {
            if (blueprint == null) return null;

            // 检查是否可以放置
            var conflicts = GetConflictingCells(blueprint, gridPos, rotation);
            if (conflicts.Count > 0)
            {
                Debug.LogWarning($"[ModularBuildingManager] 无法在 {gridPos} 放置建筑，存在冲突");
                return null;
            }

            // 从池中获取实例
            var building = buildingPool.Get();
            if (building == null) return null;

            // 初始化建筑
            int id = nextBuildingId++;
            building.Initialize(id, blueprint, gridPos, rotation, floorCount);
            building.OnStateChanged += HandleBuildingStateChanged;

            // 注册到管理器
            activeBuildings[id] = building;
            RegisterOccupiedCells(building);

            OnBuildingPlaced?.Invoke(building);
            return building;
        }

        /// <summary>
        /// 放置建筑（建造中状态）
        /// </summary>
        public ModularBuildingInstance PlaceBuildingAsConstructing(BuildingBlueprint blueprint,
            Vector2Int gridPos, int rotation = 0, int floorCount = -1)
        {
            if (blueprint == null) return null;

            var conflicts = GetConflictingCells(blueprint, gridPos, rotation);
            if (conflicts.Count > 0)
            {
                Debug.LogWarning($"[ModularBuildingManager] 无法在 {gridPos} 放置建筑，存在冲突");
                return null;
            }

            var building = buildingPool.Get();
            if (building == null) return null;

            int id = nextBuildingId++;
            building.InitializeAsConstructing(id, blueprint, gridPos, rotation, floorCount);
            building.OnStateChanged += HandleBuildingStateChanged;

            activeBuildings[id] = building;
            RegisterOccupiedCells(building);

            OnBuildingPlaced?.Invoke(building);
            return building;
        }

        private void RegisterOccupiedCells(ModularBuildingInstance building)
        {
            var positions = building.GetOccupiedGridPositions();
            foreach (var pos in positions)
            {
                gridToBuilding[pos] = building.InstanceId;
            }
        }

        private void UnregisterOccupiedCells(ModularBuildingInstance building)
        {
            var positions = building.GetOccupiedGridPositions();
            foreach (var pos in positions)
            {
                gridToBuilding.Remove(pos);
            }
        }

        #endregion

        #region 建筑移除

        /// <summary>
        /// 移除建筑（通过ID）
        /// </summary>
        public bool RemoveBuilding(int buildingId)
        {
            if (!activeBuildings.TryGetValue(buildingId, out var building))
                return false;

            // 注销资源生产器
            BuildingResourceSystemManager.Instance?.UnregisterBuilding(buildingId);

            building.OnStateChanged -= HandleBuildingStateChanged;
            UnregisterOccupiedCells(building);
            activeBuildings.Remove(buildingId);

            OnBuildingRemoved?.Invoke(building);
            buildingPool.Return(building);
            return true;
        }

        /// <summary>
        /// 移除指定位置的建筑
        /// </summary>
        public bool RemoveBuildingAt(Vector2Int gridPos)
        {
            if (gridToBuilding.TryGetValue(gridPos, out int buildingId))
                return RemoveBuilding(buildingId);
            return false;
        }

        /// <summary>
        /// 移除所有建筑
        /// </summary>
        public void RemoveAllBuildings()
        {
            var ids = new List<int>(activeBuildings.Keys);
            foreach (var id in ids)
            {
                RemoveBuilding(id);
            }
        }

        #endregion

        #region 冲突检测

        /// <summary>
        /// 检查是否可以放置建筑
        /// </summary>
        public bool CanPlaceBuilding(BuildingBlueprint blueprint, Vector2Int gridPos, int rotation = 0)
        {
            return GetConflictingCells(blueprint, gridPos, rotation).Count == 0;
        }

        /// <summary>
        /// 获取冲突的格子列表
        /// </summary>
        public List<Vector2Int> GetConflictingCells(BuildingBlueprint blueprint, Vector2Int gridPos,
            int rotation = 0)
        {
            var conflicts = new List<Vector2Int>();
            if (blueprint == null) return conflicts;

            var mask = blueprint.GetRotatedMask(rotation);
            var mapManager = IsometricSpriteMapManager.Instance;

            foreach (var offset in mask.GetOccupiedPositions())
            {
                var worldCell = gridPos + offset;

                // 检查地图边界
                if (mapManager != null && !mapManager.IsValidGridPosition(worldCell))
                {
                    conflicts.Add(worldCell);
                    continue;
                }

                // 检查已有建筑
                if (gridToBuilding.ContainsKey(worldCell))
                {
                    conflicts.Add(worldCell);
                }
            }

            return conflicts;
        }

        /// <summary>
        /// 获取建筑将占用的所有格子
        /// </summary>
        public List<Vector2Int> GetOccupiedCells(BuildingBlueprint blueprint, Vector2Int gridPos,
            int rotation = 0)
        {
            var cells = new List<Vector2Int>();
            if (blueprint == null) return cells;

            var mask = blueprint.GetRotatedMask(rotation);
            foreach (var offset in mask.GetOccupiedPositions())
            {
                cells.Add(gridPos + offset);
            }
            return cells;
        }

        #endregion

        #region 查询

        /// <summary>
        /// 获取指定位置的建筑
        /// </summary>
        public ModularBuildingInstance GetBuildingAt(Vector2Int gridPos)
        {
            if (gridToBuilding.TryGetValue(gridPos, out int buildingId))
            {
                activeBuildings.TryGetValue(buildingId, out var building);
                return building;
            }
            return null;
        }

        /// <summary>
        /// 获取建筑（通过ID）
        /// </summary>
        public ModularBuildingInstance GetBuilding(int buildingId)
        {
            activeBuildings.TryGetValue(buildingId, out var building);
            return building;
        }

        /// <summary>
        /// 获取建筑模板
        /// </summary>
        public BuildingBlueprint GetBlueprint(int blueprintId)
        {
            blueprintDict.TryGetValue(blueprintId, out var blueprint);
            return blueprint;
        }

        /// <summary>
        /// 获取所有建筑模板
        /// </summary>
        public IEnumerable<BuildingBlueprint> GetAllBlueprints()
        {
            return blueprintDict.Values;
        }

        /// <summary>
        /// 获取指定类别的建筑模板
        /// </summary>
        public List<BuildingBlueprint> GetBlueprintsByCategory(BuildingCategory category)
        {
            var result = new List<BuildingBlueprint>();
            foreach (var blueprint in blueprintDict.Values)
            {
                if (blueprint.category == category)
                    result.Add(blueprint);
            }
            return result;
        }

        /// <summary>
        /// 获取所有活跃建筑
        /// </summary>
        public IEnumerable<ModularBuildingInstance> GetAllBuildings()
        {
            return activeBuildings.Values;
        }

        /// <summary>
        /// 获取活跃建筑数量
        /// </summary>
        public int GetActiveBuildingCount() => activeBuildings.Count;

        /// <summary>
        /// 检查指定位置是否被占用
        /// </summary>
        public bool IsCellOccupied(Vector2Int gridPos)
        {
            return gridToBuilding.ContainsKey(gridPos);
        }

        #endregion

        #region 状态管理

        /// <summary>
        /// 设置建筑状态
        /// </summary>
        public void SetBuildingState(int buildingId, ModularBuildingState state)
        {
            if (activeBuildings.TryGetValue(buildingId, out var building))
            {
                building.SetState(state);
            }
        }

        private void HandleBuildingStateChanged(ModularBuildingInstance building, ModularBuildingState state)
        {
            OnBuildingStateChanged?.Invoke(building, state);
        }

        #endregion

        #region 存档/读档

        /// <summary>
        /// 创建存档数据
        /// </summary>
        public BuildingSystemSaveData CreateSaveData()
        {
            var saveData = new BuildingSystemSaveData
            {
                nextInstanceId = nextBuildingId
            };

            foreach (var building in activeBuildings.Values)
            {
                saveData.AddBuilding(building.CreateSaveData());
            }

            return saveData;
        }

        /// <summary>
        /// 从存档数据恢复
        /// </summary>
        public void LoadFromSaveData(BuildingSystemSaveData saveData)
        {
            if (saveData == null) return;

            // 清除现有建筑
            RemoveAllBuildings();

            nextBuildingId = saveData.nextInstanceId;

            if (saveData.buildings == null) return;

            foreach (var buildingSave in saveData.buildings)
            {
                var blueprint = GetBlueprint(buildingSave.blueprintId);
                if (blueprint == null)
                {
                    Debug.LogWarning($"[ModularBuildingManager] 加载时未找到建筑模板: {buildingSave.blueprintId}");
                    continue;
                }

                var building = buildingPool.Get();
                if (building == null) continue;

                building.LoadFromSaveData(buildingSave, blueprint);
                building.OnStateChanged += HandleBuildingStateChanged;

                activeBuildings[building.InstanceId] = building;
                RegisterOccupiedCells(building);

                // 注册到资源生产系统
                if (BuildingResourceSystemManager.Instance != null)
                {
                    BuildingResourceSystemManager.Instance.RegisterBuilding(
                        building.InstanceId, buildingSave.blueprintId);
                }
            }

            Debug.Log($"[ModularBuildingManager] 已加载 {activeBuildings.Count} 个建筑");
        }

        /// <summary>
        /// 保存到JSON
        /// </summary>
        public string SaveToJson(bool prettyPrint = false)
        {
            return CreateSaveData().ToJson(prettyPrint);
        }

        /// <summary>
        /// 从JSON加载
        /// </summary>
        public void LoadFromJson(string json)
        {
            var saveData = BuildingSystemSaveData.FromJson(json);
            LoadFromSaveData(saveData);
        }

        #endregion

        #region 运行时添加模板

        /// <summary>
        /// 运行时注册建筑模板
        /// </summary>
        public void RegisterBlueprint(BuildingBlueprint blueprint)
        {
            if (blueprint == null || blueprint.blueprintId <= 0) return;

            if (blueprintDict.ContainsKey(blueprint.blueprintId))
            {
                Debug.LogWarning($"[ModularBuildingManager] 覆盖已存在的建筑模板: {blueprint.blueprintId}");
            }
            blueprintDict[blueprint.blueprintId] = blueprint;
        }

        /// <summary>
        /// 运行时注销建筑模板
        /// </summary>
        public void UnregisterBlueprint(int blueprintId)
        {
            blueprintDict.Remove(blueprintId);
        }

        #endregion
    }
}
