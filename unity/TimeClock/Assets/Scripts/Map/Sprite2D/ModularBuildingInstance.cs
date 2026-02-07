using UnityEngine;
using System.Collections.Generic;
using PomodoroTimer.Map.Data;

namespace PomodoroTimer.Map.Sprite2D
{
    /// <summary>
    /// 模块化建筑实例
    /// 轻量化设计，仅存储ID和参数，渲染由ModularBuildingRenderer处理
    /// </summary>
    public class ModularBuildingInstance : MonoBehaviour
    {
        // 核心数据
        private int instanceId;
        private BuildingBlueprint blueprint;
        private Vector2Int gridPosition;
        private int rotation;  // 0/90/180/270
        private ModularBuildingState state = ModularBuildingState.Normal;

        // 多层配置
        private int floorCount = 1;
        private Dictionary<int, List<BuildingPartConfig>> floorConfigs;

        // 单层配置
        private List<BuildingPartConfig> partConfigs;

        // 运行时组件
        private ModularBuildingRenderer buildingRenderer;
        private BuildingEffectController effectController;
        private BuildingStateController stateController;

        // 游戏属性
        private int currentHealth;
        private float constructionProgress = 1f;

        // 共享材质
        private Material sharedMaterial;

        // 属性访问器
        public int InstanceId => instanceId;
        public BuildingBlueprint Blueprint => blueprint;
        public Vector2Int GridPosition => gridPosition;
        public int Rotation => rotation;
        public ModularBuildingState State => state;
        public int FloorCount => floorCount;
        public int CurrentHealth => currentHealth;
        public float ConstructionProgress => constructionProgress;
        public bool IsConstructing => state == ModularBuildingState.Constructing;
        public bool IsDestroyed => state == ModularBuildingState.Destroyed;

        // 事件
        public event System.Action<ModularBuildingInstance, ModularBuildingState> OnStateChanged;
        public event System.Action<ModularBuildingInstance> OnPartConfigChanged;
        public event System.Action<ModularBuildingInstance, int> OnHealthChanged;

        private void Awake()
        {
            floorConfigs = new Dictionary<int, List<BuildingPartConfig>>();
            partConfigs = new List<BuildingPartConfig>();
        }

        /// <summary>
        /// 设置共享材质
        /// </summary>
        public void SetSharedMaterial(Material material)
        {
            sharedMaterial = material;
        }

        /// <summary>
        /// 初始化建筑实例
        /// </summary>
        public void Initialize(int id, BuildingBlueprint blueprint, Vector2Int gridPos,
            int rotation = 0, int floorCount = -1)
        {
            this.instanceId = id;
            this.blueprint = blueprint;
            this.gridPosition = gridPos;
            this.rotation = NormalizeRotation(rotation);
            this.floorCount = floorCount > 0 ? floorCount : (blueprint?.defaultFloorCount ?? 1);
            this.currentHealth = blueprint?.maxHealth ?? 100;
            this.state = ModularBuildingState.Normal;
            this.constructionProgress = 1f;

            if (blueprint != null)
            {
                gameObject.name = $"ModularBuilding_{blueprint.buildingName}_{id}";
                UpdateWorldPosition();
                InitializeDefaultParts();
                EnsureRenderer();
                buildingRenderer?.Rebuild();
            }
        }

        /// <summary>
        /// 初始化为建造中状态
        /// </summary>
        public void InitializeAsConstructing(int id, BuildingBlueprint blueprint,
            Vector2Int gridPos, int rotation = 0, int floorCount = -1)
        {
            Initialize(id, blueprint, gridPos, rotation, floorCount);
            this.state = ModularBuildingState.Constructing;
            this.constructionProgress = 0f;
            buildingRenderer?.SetConstructionProgress(0f);
        }

        private void InitializeDefaultParts()
        {
            partConfigs.Clear();
            floorConfigs.Clear();

            if (blueprint == null) return;

            // 单层建筑配置
            if (blueprint.defaultParts != null)
            {
                foreach (var config in blueprint.defaultParts)
                {
                    partConfigs.Add(new BuildingPartConfig
                    {
                        slotId = config.slotId,
                        variantId = config.variantId,
                        tintColor = config.tintColor,
                        useCustomTint = config.useCustomTint
                    });
                }
            }

            // 多层建筑配置
            if (blueprint.floors != null)
            {
                for (int i = 0; i < floorCount && i < blueprint.floors.Length; i++)
                {
                    var floor = blueprint.floors[i];
                    var configs = new List<BuildingPartConfig>();

                    if (floor.partSlots != null)
                    {
                        foreach (var slot in floor.partSlots)
                        {
                            var variant = slot.GetDefaultVariant();
                            if (variant != null)
                            {
                                configs.Add(new BuildingPartConfig
                                {
                                    slotId = slot.slotId,
                                    variantId = variant.variantId,
                                    tintColor = variant.defaultTint,
                                    useCustomTint = false
                                });
                            }
                        }
                    }

                    floorConfigs[floor.floorIndex] = configs;
                }
            }
        }

        private void EnsureRenderer()
        {
            if (buildingRenderer == null)
            {
                buildingRenderer = GetComponent<ModularBuildingRenderer>();
                if (buildingRenderer == null)
                    buildingRenderer = gameObject.AddComponent<ModularBuildingRenderer>();
                buildingRenderer.Initialize(this, sharedMaterial);
            }
        }

        private void UpdateWorldPosition()
        {
            var mapManager = IsometricSpriteMapManager.Instance;
            if (mapManager == null) return;

            Vector3 worldPos = mapManager.GridToWorld(gridPosition);
            if (blueprint != null)
                worldPos.y += blueprint.yOffset / mapManager.GetPixelsPerUnit();
            transform.position = worldPos;
        }

        /// <summary>
        /// 设置网格位置
        /// </summary>
        public void SetGridPosition(Vector2Int newPos)
        {
            gridPosition = newPos;
            UpdateWorldPosition();
            buildingRenderer?.UpdateSorting();
        }

        /// <summary>
        /// 设置旋转
        /// </summary>
        public void SetRotation(int newRotation)
        {
            rotation = NormalizeRotation(newRotation);
            buildingRenderer?.Rebuild();
        }

        /// <summary>
        /// 旋转90度
        /// </summary>
        public void Rotate90()
        {
            SetRotation(rotation + 90);
        }

        private int NormalizeRotation(int rot)
        {
            rot = ((rot % 360) + 360) % 360;
            return (rot / 90) * 90; // 只允许0/90/180/270
        }

        /// <summary>
        /// 设置状态
        /// </summary>
        public void SetState(ModularBuildingState newState)
        {
            if (state == newState) return;
            var oldState = state;
            state = newState;

            stateController?.OnStateChanged(oldState, newState);
            buildingRenderer?.OnStateChanged(newState);
            OnStateChanged?.Invoke(this, newState);
        }

        /// <summary>
        /// 设置建造进度
        /// </summary>
        public void SetConstructionProgress(float progress)
        {
            constructionProgress = Mathf.Clamp01(progress);
            buildingRenderer?.SetConstructionProgress(constructionProgress);

            if (constructionProgress >= 1f && state == ModularBuildingState.Constructing)
            {
                SetState(ModularBuildingState.Normal);
            }
        }

        /// <summary>
        /// 设置部件配置（单层）
        /// </summary>
        public void SetPartConfig(string slotId, string variantId, Color? tintColor = null)
        {
            var existing = partConfigs.Find(p => p.slotId == slotId);
            if (existing != null)
            {
                existing.variantId = variantId;
                if (tintColor.HasValue)
                {
                    existing.tintColor = tintColor.Value;
                    existing.useCustomTint = true;
                }
            }
            else
            {
                var config = new BuildingPartConfig(slotId, variantId);
                if (tintColor.HasValue)
                {
                    config.tintColor = tintColor.Value;
                    config.useCustomTint = true;
                }
                partConfigs.Add(config);
            }

            buildingRenderer?.Rebuild();
            OnPartConfigChanged?.Invoke(this);
        }

        /// <summary>
        /// 设置部件配置（多层）
        /// </summary>
        public void SetFloorPartConfig(int floorIndex, string slotId, string variantId,
            Color? tintColor = null)
        {
            if (!floorConfigs.TryGetValue(floorIndex, out var configs))
            {
                configs = new List<BuildingPartConfig>();
                floorConfigs[floorIndex] = configs;
            }

            var existing = configs.Find(p => p.slotId == slotId);
            if (existing != null)
            {
                existing.variantId = variantId;
                if (tintColor.HasValue)
                {
                    existing.tintColor = tintColor.Value;
                    existing.useCustomTint = true;
                }
            }
            else
            {
                var config = new BuildingPartConfig(slotId, variantId);
                if (tintColor.HasValue)
                {
                    config.tintColor = tintColor.Value;
                    config.useCustomTint = true;
                }
                configs.Add(config);
            }

            buildingRenderer?.Rebuild();
            OnPartConfigChanged?.Invoke(this);
        }

        /// <summary>
        /// 获取部件配置（单层）
        /// </summary>
        public BuildingPartConfig GetPartConfig(string slotId)
        {
            return partConfigs.Find(p => p.slotId == slotId);
        }

        /// <summary>
        /// 获取部件配置（多层）
        /// </summary>
        public BuildingPartConfig GetFloorPartConfig(int floorIndex, string slotId)
        {
            if (floorConfigs.TryGetValue(floorIndex, out var configs))
                return configs.Find(p => p.slotId == slotId);
            return null;
        }

        /// <summary>
        /// 获取所有部件配置（单层）
        /// </summary>
        public List<BuildingPartConfig> GetAllPartConfigs()
        {
            return new List<BuildingPartConfig>(partConfigs);
        }

        /// <summary>
        /// 获取楼层部件配置
        /// </summary>
        public List<BuildingPartConfig> GetFloorPartConfigs(int floorIndex)
        {
            if (floorConfigs.TryGetValue(floorIndex, out var configs))
                return new List<BuildingPartConfig>(configs);
            return new List<BuildingPartConfig>();
        }

        /// <summary>
        /// 设置楼层数
        /// </summary>
        public void SetFloorCount(int count)
        {
            if (blueprint == null) return;
            int maxFloors = blueprint.floors?.Length ?? 1;
            floorCount = Mathf.Clamp(count, 1, maxFloors);
            buildingRenderer?.Rebuild();
        }

        /// <summary>
        /// 造成伤害
        /// </summary>
        public void TakeDamage(int damage)
        {
            if (state == ModularBuildingState.Destroyed) return;

            int oldHealth = currentHealth;
            currentHealth = Mathf.Max(0, currentHealth - damage);
            OnHealthChanged?.Invoke(this, currentHealth);

            if (currentHealth <= 0)
            {
                SetState(ModularBuildingState.Destroyed);
            }
            else if (currentHealth < (blueprint?.maxHealth ?? 100) * 0.3f)
            {
                SetState(ModularBuildingState.Damaged);
            }
        }

        /// <summary>
        /// 修复建筑
        /// </summary>
        public void Repair(int amount)
        {
            if (state == ModularBuildingState.Destroyed) return;

            int maxHealth = blueprint?.maxHealth ?? 100;
            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            OnHealthChanged?.Invoke(this, currentHealth);

            if (currentHealth >= maxHealth * 0.3f && state == ModularBuildingState.Damaged)
            {
                SetState(ModularBuildingState.Normal);
            }
        }

        /// <summary>
        /// 获取旋转后的占位掩码
        /// </summary>
        public OccupancyMask GetRotatedOccupancyMask()
        {
            return blueprint?.GetRotatedMask(rotation) ?? new OccupancyMask(1, 1);
        }

        /// <summary>
        /// 获取所有占用的网格位置
        /// </summary>
        public List<Vector2Int> GetOccupiedGridPositions()
        {
            var mask = GetRotatedOccupancyMask();
            var positions = new List<Vector2Int>();
            foreach (var offset in mask.GetOccupiedPositions())
            {
                positions.Add(gridPosition + offset);
            }
            return positions;
        }

        /// <summary>
        /// 重置实例（用于对象池回收）
        /// </summary>
        public void Reset()
        {
            instanceId = -1;
            blueprint = null;
            gridPosition = Vector2Int.zero;
            rotation = 0;
            state = ModularBuildingState.Normal;
            floorCount = 1;
            currentHealth = 100;
            constructionProgress = 1f;

            partConfigs.Clear();
            floorConfigs.Clear();

            buildingRenderer?.Clear();
            effectController?.Clear();

            gameObject.name = "ModularBuilding_Pooled";
        }

        /// <summary>
        /// 创建存档数据
        /// </summary>
        public BuildingInstanceSaveData CreateSaveData()
        {
            var saveData = new BuildingInstanceSaveData
            {
                instanceId = instanceId,
                blueprintId = blueprint?.blueprintId ?? 0,
                gridPosition = gridPosition,
                rotation = rotation,
                state = state,
                floorCount = floorCount,
                currentHealth = currentHealth,
                constructionProgress = constructionProgress
            };

            // 保存单层配置
            saveData.partConfigs = new PartConfigSaveData[partConfigs.Count];
            for (int i = 0; i < partConfigs.Count; i++)
            {
                saveData.partConfigs[i] = new PartConfigSaveData(partConfigs[i]);
            }

            // 保存多层配置
            var floorSaveList = new List<FloorPartSaveData>();
            foreach (var kvp in floorConfigs)
            {
                var floorSave = new FloorPartSaveData(kvp.Key);
                floorSave.parts = new PartConfigSaveData[kvp.Value.Count];
                for (int i = 0; i < kvp.Value.Count; i++)
                {
                    floorSave.parts[i] = new PartConfigSaveData(kvp.Value[i]);
                }
                floorSaveList.Add(floorSave);
            }
            saveData.floorConfigs = floorSaveList.ToArray();

            return saveData;
        }

        /// <summary>
        /// 从存档数据恢复
        /// </summary>
        public void LoadFromSaveData(BuildingInstanceSaveData saveData, BuildingBlueprint blueprint)
        {
            this.instanceId = saveData.instanceId;
            this.blueprint = blueprint;
            this.gridPosition = saveData.gridPosition;
            this.rotation = saveData.rotation;
            this.state = saveData.state;
            this.floorCount = saveData.floorCount;
            this.currentHealth = saveData.currentHealth;
            this.constructionProgress = saveData.constructionProgress;

            // 恢复单层配置
            partConfigs.Clear();
            if (saveData.partConfigs != null)
            {
                foreach (var config in saveData.partConfigs)
                {
                    partConfigs.Add(config.ToPartConfig());
                }
            }

            // 恢复多层配置
            floorConfigs.Clear();
            if (saveData.floorConfigs != null)
            {
                foreach (var floorSave in saveData.floorConfigs)
                {
                    var configs = new List<BuildingPartConfig>();
                    if (floorSave.parts != null)
                    {
                        foreach (var part in floorSave.parts)
                        {
                            configs.Add(part.ToPartConfig());
                        }
                    }
                    floorConfigs[floorSave.floorIndex] = configs;
                }
            }

            if (blueprint != null)
            {
                gameObject.name = $"ModularBuilding_{blueprint.buildingName}_{instanceId}";
                UpdateWorldPosition();
                EnsureRenderer();
                buildingRenderer?.Rebuild();
            }
        }
    }
}
