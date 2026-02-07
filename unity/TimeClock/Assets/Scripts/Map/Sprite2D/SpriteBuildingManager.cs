using UnityEngine;
using System.Collections.Generic;
using PomodoroTimer.Map.Data;

namespace PomodoroTimer.Map.Sprite2D
{
    public class SpriteBuildingManager : MonoBehaviour
    {
        public static SpriteBuildingManager Instance { get; private set; }

        [Header("设置")]
        [SerializeField] private Material buildingMaterial;
        [SerializeField] private int poolInitialSize = 64;
        [SerializeField] private int poolMaxSize = 256;

        [Header("建筑数据")]
        [SerializeField] private BuildingSpriteData[] buildingDataList;

        private SpriteBuildingPool buildingPool;
        private Dictionary<int, BuildingSpriteInstance> activeBuildings = new Dictionary<int, BuildingSpriteInstance>();
        private Dictionary<Vector2Int, int> gridToBuilding = new Dictionary<Vector2Int, int>();
        private int nextBuildingId = 1;

        public event System.Action<BuildingSpriteInstance> OnBuildingPlaced;
        public event System.Action<BuildingSpriteInstance> OnBuildingRemoved;
        public event System.Action<BuildingSpriteInstance, BuildingAnimationState> OnBuildingStateChanged;

        private void Awake()
        {
            if (Instance == null) { Instance = this; }
            else { Destroy(gameObject); return; }
        }

        private void Start()
        {
            InitializePool();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            buildingPool?.Clear();
        }

        private void InitializePool()
        {
            Transform parent = IsometricSpriteMapManager.Instance?.GetBuildingsParent() ?? transform;
            buildingPool = new SpriteBuildingPool(parent, buildingMaterial, poolInitialSize, poolMaxSize);
        }

        public BuildingSpriteInstance PlaceBuilding(int buildingTypeId, Vector2Int gridPos)
        {
            BuildingSpriteData data = GetBuildingData(buildingTypeId);
            if (data == null)
            {
                Debug.LogWarning($"[SpriteBuildingManager] 未找到建筑类型: {buildingTypeId}");
                return null;
            }
            return PlaceBuilding(data, gridPos);
        }

        public BuildingSpriteInstance PlaceBuilding(BuildingSpriteData data, Vector2Int gridPos)
        {
            if (data == null) return null;

            if (!CanPlaceBuilding(data, gridPos))
            {
                Debug.LogWarning($"[SpriteBuildingManager] 无法在 {gridPos} 放置建筑");
                return null;
            }

            BuildingSpriteInstance building = buildingPool.Get();
            if (building == null) return null;

            int id = nextBuildingId++;
            building.Initialize(id, data, gridPos);
            activeBuildings[id] = building;

            for (int x = 0; x < data.gridSize.x; x++)
            {
                for (int y = 0; y < data.gridSize.y; y++)
                {
                    gridToBuilding[gridPos + new Vector2Int(x, y)] = id;
                }
            }

            OnBuildingPlaced?.Invoke(building);
            return building;
        }

        public bool RemoveBuilding(int buildingId)
        {
            if (!activeBuildings.TryGetValue(buildingId, out BuildingSpriteInstance building))
                return false;

            BuildingSpriteData data = building.BuildingData;
            Vector2Int gridPos = building.GridPosition;

            if (data != null)
            {
                for (int x = 0; x < data.gridSize.x; x++)
                {
                    for (int y = 0; y < data.gridSize.y; y++)
                    {
                        gridToBuilding.Remove(gridPos + new Vector2Int(x, y));
                    }
                }
            }

            activeBuildings.Remove(buildingId);
            OnBuildingRemoved?.Invoke(building);
            buildingPool.Return(building);
            return true;
        }

        public bool RemoveBuildingAt(Vector2Int gridPos)
        {
            if (gridToBuilding.TryGetValue(gridPos, out int buildingId))
                return RemoveBuilding(buildingId);
            return false;
        }

        public void SetBuildingState(int buildingId, BuildingAnimationState state)
        {
            if (activeBuildings.TryGetValue(buildingId, out BuildingSpriteInstance building))
            {
                building.SetState(state);
                OnBuildingStateChanged?.Invoke(building, state);
            }
        }

        public bool CanPlaceBuilding(BuildingSpriteData data, Vector2Int gridPos)
        {
            if (data == null) return false;
            var mapManager = IsometricSpriteMapManager.Instance;
            if (mapManager == null) return false;

            for (int x = 0; x < data.gridSize.x; x++)
            {
                for (int y = 0; y < data.gridSize.y; y++)
                {
                    Vector2Int checkPos = gridPos + new Vector2Int(x, y);
                    if (!mapManager.IsValidGridPosition(checkPos)) return false;
                    if (gridToBuilding.ContainsKey(checkPos)) return false;
                }
            }
            return true;
        }

        public BuildingSpriteInstance GetBuildingAt(Vector2Int gridPos)
        {
            if (gridToBuilding.TryGetValue(gridPos, out int buildingId))
            {
                activeBuildings.TryGetValue(buildingId, out BuildingSpriteInstance building);
                return building;
            }
            return null;
        }

        public BuildingSpriteInstance GetBuilding(int buildingId)
        {
            activeBuildings.TryGetValue(buildingId, out BuildingSpriteInstance building);
            return building;
        }

        public BuildingSpriteData GetBuildingData(int typeId)
        {
            if (buildingDataList == null) return null;
            foreach (var data in buildingDataList)
            {
                if (data != null && data.buildingTypeId == typeId)
                    return data;
            }
            return null;
        }

        public int GetActiveBuildingCount() => activeBuildings.Count;
    }
}