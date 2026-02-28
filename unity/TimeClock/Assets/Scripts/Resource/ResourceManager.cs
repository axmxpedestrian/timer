using System;
using System.Collections.Generic;
using UnityEngine;

namespace PomodoroTimer.Resource
{
    /// <summary>
    /// 资源变更事件参数
    /// </summary>
    public class ResourceChangedEventArgs : EventArgs
    {
        public ResourceType ResourceType { get; }
        public long OldAmount { get; }
        public long NewAmount { get; }
        public long Delta => NewAmount - OldAmount;
        public string Source { get; }

        public ResourceChangedEventArgs(ResourceType type, long oldAmount, long newAmount, string source = null)
        {
            ResourceType = type;
            OldAmount = oldAmount;
            NewAmount = newAmount;
            Source = source;
        }
    }

    /// <summary>
    /// 资源解锁事件参数
    /// </summary>
    public class ResourceUnlockedEventArgs : EventArgs
    {
        public ResourceType ResourceType { get; }

        public ResourceUnlockedEventArgs(ResourceType type)
        {
            ResourceType = type;
        }
    }

    /// <summary>
    /// 资源管理器
    /// 统一管理所有资源的增减、查询、事件通知
    /// </summary>
    public class ResourceManager : MonoBehaviour
    {
        public static ResourceManager Instance { get; private set; }

        [Header("资源定义")]
        [SerializeField] private ResourceDefinition[] resourceDefinitions;

        [Header("容量设置")]
        [Tooltip("基础保底容量（即使没有建筑也有的最低存储上限）")]
        [SerializeField] private ResourceCapacity[] defaultBaseCapacities;

        // 资源数据
        private Dictionary<ResourceType, long> resources = new Dictionary<ResourceType, long>();
        private Dictionary<ResourceType, bool> unlockedResources = new Dictionary<ResourceType, bool>();
        private Dictionary<ResourceType, ResourceDefinition> definitionMap = new Dictionary<ResourceType, ResourceDefinition>();

        // 容量数据
        private Dictionary<ResourceType, long> baseCapacities = new Dictionary<ResourceType, long>();
        private Dictionary<ResourceType, long> buildingCapacities = new Dictionary<ResourceType, long>();

        // 事件
        public event EventHandler<ResourceChangedEventArgs> OnResourceChanged;
        public event EventHandler<ResourceUnlockedEventArgs> OnResourceUnlocked;
        public event Action OnResourcesLoaded;
        public event Action<ResourceType, long> OnCapacityChanged;

        // 属性
        public bool IsInitialized { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            InitializeDefinitionMap();
            InitializeBaseCapacities();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void InitializeDefinitionMap()
        {
            definitionMap.Clear();
            if (resourceDefinitions == null) return;

            foreach (var def in resourceDefinitions)
            {
                if (def != null)
                {
                    definitionMap[def.resourceType] = def;
                }
            }
        }

        private void InitializeBaseCapacities()
        {
            baseCapacities.Clear();
            if (defaultBaseCapacities == null) return;

            foreach (var cap in defaultBaseCapacities)
            {
                if (cap != null && cap.capacity > 0)
                {
                    baseCapacities[cap.resourceType] = cap.capacity;
                }
            }
        }

        /// <summary>
        /// 初始化资源系统（从存档加载或新建）
        /// </summary>
        public void Initialize(ResourceSaveData saveData)
        {
            resources.Clear();
            unlockedResources.Clear();
            buildingCapacities.Clear();

            // 初始化所有资源类型
            foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
            {
                resources[type] = 0;
                unlockedResources[type] = false;
            }

            // 从存档恢复
            var loadedFromSave = new HashSet<ResourceType>();
            if (saveData != null && saveData.resourceEntries != null)
            {
                foreach (var entry in saveData.resourceEntries)
                {
                    if (Enum.TryParse<ResourceType>(entry.resourceType, out var type))
                    {
                        resources[type] = entry.amount;
                        unlockedResources[type] = entry.unlocked;
                        loadedFromSave.Add(type);
                    }
                }
            }

            // 应用默认解锁（仅对存档中不存在的资源设置初始值）
            foreach (var def in definitionMap.Values)
            {
                if (def.unlockedByDefault && !unlockedResources[def.resourceType])
                {
                    unlockedResources[def.resourceType] = true;
                    if (!loadedFromSave.Contains(def.resourceType))
                    {
                        resources[def.resourceType] = def.initialAmount;
                    }
                }
            }

            IsInitialized = true;
            OnResourcesLoaded?.Invoke();
        }

        #region 容量系统

        /// <summary>
        /// 获取某资源的总容量上限（基础保底 + 建筑提供）
        /// 返回 0 表示该资源不受容量限制
        /// </summary>
        public long GetCapacity(ResourceType type)
        {
            long baseCap = baseCapacities.TryGetValue(type, out var bc) ? bc : 0;
            long buildingCap = buildingCapacities.TryGetValue(type, out var bbc) ? bbc : 0;

            long total = baseCap + buildingCap;
            return total;
        }

        /// <summary>
        /// 检查某资源是否受容量限制
        /// 只有在 baseCapacities 或 buildingCapacities 中出现过的资源才受限
        /// </summary>
        public bool HasCapacity(ResourceType type)
        {
            return baseCapacities.ContainsKey(type) || buildingCapacities.ContainsKey(type);
        }

        /// <summary>
        /// 由建筑系统调用：用新的建筑容量字典替换当前建筑容量，并截断超出上限的资源
        /// </summary>
        public void RecalculateAllCapacities(Dictionary<ResourceType, long> newBuildingCapacities)
        {
            // 收集所有受容量影响的资源类型（旧 + 新）
            var affectedTypes = new HashSet<ResourceType>();
            foreach (var type in buildingCapacities.Keys)
                affectedTypes.Add(type);
            if (newBuildingCapacities != null)
            {
                foreach (var type in newBuildingCapacities.Keys)
                    affectedTypes.Add(type);
            }
            // 基础容量涉及的类型也需要通知
            foreach (var type in baseCapacities.Keys)
                affectedTypes.Add(type);

            // 替换建筑容量
            buildingCapacities.Clear();
            if (newBuildingCapacities != null)
            {
                foreach (var kvp in newBuildingCapacities)
                {
                    buildingCapacities[kvp.Key] = kvp.Value;
                }
            }

            // 对每个受影响的资源类型：截断超出上限的部分并通知
            foreach (var type in affectedTypes)
            {
                long cap = GetCapacity(type);
                OnCapacityChanged?.Invoke(type, cap);

                // 截断超出容量的资源
                if (HasCapacity(type))
                {
                    long current = GetAmount(type);
                    if (current > cap)
                    {
                        SetResource(type, cap, "CapacityClamp");
                    }
                }
            }
        }

        /// <summary>
        /// 将所有受容量限制的资源截断到上限（用于存档加载后校验）
        /// </summary>
        public void ClampResourcesToCapacity()
        {
            foreach (var type in baseCapacities.Keys)
            {
                long cap = GetCapacity(type);
                long current = GetAmount(type);
                if (current > cap)
                {
                    SetResource(type, cap, "CapacityClamp");
                }
            }
            // 也检查建筑容量涉及的类型
            foreach (var type in buildingCapacities.Keys)
            {
                long cap = GetCapacity(type);
                long current = GetAmount(type);
                if (current > cap)
                {
                    SetResource(type, cap, "CapacityClamp");
                }
            }
        }

        #endregion

        #region 资源查询

        /// <summary>
        /// 获取资源数量
        /// </summary>
        public long GetAmount(ResourceType type)
        {
            return resources.TryGetValue(type, out var amount) ? amount : 0;
        }

        /// <summary>
        /// 检查资源是否已解锁
        /// </summary>
        public bool IsUnlocked(ResourceType type)
        {
            return unlockedResources.TryGetValue(type, out var unlocked) && unlocked;
        }

        /// <summary>
        /// 获取资源定义
        /// </summary>
        public ResourceDefinition GetDefinition(ResourceType type)
        {
            return definitionMap.TryGetValue(type, out var def) ? def : null;
        }

        /// <summary>
        /// 获取所有已解锁的资源类型
        /// </summary>
        public List<ResourceType> GetUnlockedResources()
        {
            var result = new List<ResourceType>();
            foreach (var kvp in unlockedResources)
            {
                if (kvp.Value)
                    result.Add(kvp.Key);
            }

            // 按显示顺序排序
            result.Sort((a, b) =>
            {
                var defA = GetDefinition(a);
                var defB = GetDefinition(b);
                int orderA = defA?.displayOrder ?? (int)a;
                int orderB = defB?.displayOrder ?? (int)b;
                return orderA.CompareTo(orderB);
            });

            return result;
        }

        /// <summary>
        /// 检查是否有足够的资源
        /// </summary>
        public bool HasEnough(ResourceType type, long amount)
        {
            return GetAmount(type) >= amount;
        }

        /// <summary>
        /// 检查是否有足够的多种资源
        /// </summary>
        public bool HasEnough(Dictionary<ResourceType, long> costs)
        {
            if (costs == null) return true;
            foreach (var kvp in costs)
            {
                if (!HasEnough(kvp.Key, kvp.Value))
                    return false;
            }
            return true;
        }

        #endregion

        #region 资源修改

        /// <summary>
        /// 添加资源
        /// </summary>
        public void AddResource(ResourceType type, long amount, string source = null)
        {
            if (amount == 0) return;

            // 首次获得资源时自动解锁
            if (!IsUnlocked(type) && amount > 0)
            {
                UnlockResource(type);
            }

            long oldAmount = GetAmount(type);
            long newAmount = Math.Max(0, oldAmount + amount); // 不允许负数

            // 容量上限限制：正向添加时，不超过容量上限
            if (amount > 0 && HasCapacity(type))
            {
                long cap = GetCapacity(type);
                newAmount = Math.Min(newAmount, cap);
            }

            resources[type] = newAmount;

            // 触发事件
            OnResourceChanged?.Invoke(this, new ResourceChangedEventArgs(type, oldAmount, newAmount, source));
        }

        /// <summary>
        /// 消耗资源
        /// </summary>
        public bool ConsumeResource(ResourceType type, long amount, string source = null)
        {
            if (amount <= 0) return true;
            if (!HasEnough(type, amount)) return false;

            AddResource(type, -amount, source);
            return true;
        }

        /// <summary>
        /// 消耗多种资源
        /// </summary>
        public bool ConsumeResources(Dictionary<ResourceType, long> costs, string source = null)
        {
            if (costs == null) return true;
            if (!HasEnough(costs)) return false;

            foreach (var kvp in costs)
            {
                AddResource(kvp.Key, -kvp.Value, source);
            }
            return true;
        }

        /// <summary>
        /// 设置资源数量（直接设置，用于特殊情况）
        /// </summary>
        public void SetResource(ResourceType type, long amount, string source = null)
        {
            long oldAmount = GetAmount(type);
            long newAmount = Math.Max(0, amount);
            resources[type] = newAmount;

            if (!IsUnlocked(type) && newAmount > 0)
            {
                UnlockResource(type);
            }

            OnResourceChanged?.Invoke(this, new ResourceChangedEventArgs(type, oldAmount, newAmount, source));
        }

        /// <summary>
        /// 解锁资源
        /// </summary>
        public void UnlockResource(ResourceType type)
        {
            if (IsUnlocked(type)) return;

            unlockedResources[type] = true;
            OnResourceUnlocked?.Invoke(this, new ResourceUnlockedEventArgs(type));
        }

        #endregion

        #region 代币特殊处理

        /// <summary>
        /// 添加代币（从番茄钟获得）
        /// </summary>
        public void AddCoinsFromPomodoro(int coins)
        {
            if (coins <= 0) return;
            AddResource(ResourceType.Coin, coins, "Pomodoro");
        }

        #endregion

        #region 存档

        /// <summary>
        /// 创建存档数据
        /// </summary>
        public ResourceSaveData CreateSaveData()
        {
            var saveData = new ResourceSaveData();

            foreach (var kvp in resources)
            {
                // 只保存已解锁或有数量的资源
                if (IsUnlocked(kvp.Key) || kvp.Value > 0)
                {
                    saveData.resourceEntries.Add(new ResourceSaveEntry
                    {
                        resourceType = kvp.Key.ToString(),
                        amount = kvp.Value,
                        unlocked = IsUnlocked(kvp.Key)
                    });
                }
            }

            return saveData;
        }

        #endregion
    }

    /// <summary>
    /// 资源存档数据
    /// </summary>
    [Serializable]
    public class ResourceSaveData
    {
        public List<ResourceSaveEntry> resourceEntries = new List<ResourceSaveEntry>();
    }

    /// <summary>
    /// 单个资源存档条目
    /// </summary>
    [Serializable]
    public class ResourceSaveEntry
    {
        public string resourceType;
        public long amount;
        public bool unlocked;
    }
}
