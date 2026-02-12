using System;
using System.Collections.Generic;
using UnityEngine;

namespace PomodoroTimer.Resource
{
    /// <summary>
    /// 建筑资源系统管理器
    /// 管理所有建筑的资源生产，与ModularBuildingManager集成
    /// </summary>
    public class BuildingResourceSystemManager : MonoBehaviour
    {
        public static BuildingResourceSystemManager Instance { get; private set; }

        [Header("建筑资源配置")]
        [SerializeField] private BuildingResourceConfig[] buildingConfigs;

        // 配置映射
        private Dictionary<int, BuildingResourceConfig> configMap = new Dictionary<int, BuildingResourceConfig>();

        // 活跃的生产器
        private Dictionary<int, BuildingResourceProducer> activeProducers = new Dictionary<int, BuildingResourceProducer>();

        // 事件
        public event Action<int, ResourceType, long> OnBuildingProduced;
        public event Action<int, ResourceType, long> OnBuildingConsumed;
        public event Action<int, bool> OnBuildingProductionStateChanged;

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

            InitializeConfigMap();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void InitializeConfigMap()
        {
            configMap.Clear();
            if (buildingConfigs == null) return;

            foreach (var config in buildingConfigs)
            {
                if (config != null)
                {
                    configMap[config.BlueprintId] = config;
                }
            }

            Debug.Log($"[BuildingResourceSystemManager] 已加载 {configMap.Count} 个建筑资源配置");
        }

        /// <summary>
        /// 获取建筑资源配置
        /// </summary>
        public BuildingResourceConfig GetConfig(int blueprintId)
        {
            return configMap.TryGetValue(blueprintId, out var config) ? config : null;
        }

        /// <summary>
        /// 注册建筑生产器
        /// </summary>
        public BuildingResourceProducer RegisterBuilding(int instanceId, int blueprintId, int level = 1)
        {
            var config = GetConfig(blueprintId);
            if (config == null)
            {
                Debug.LogWarning($"[BuildingResourceSystemManager] 未找到建筑资源配置: {blueprintId}");
                return null;
            }

            // 如果已存在，先移除
            UnregisterBuilding(instanceId);

            // 创建生产器
            var producerObj = new GameObject($"Producer_{instanceId}");
            producerObj.transform.SetParent(transform);

            var producer = producerObj.AddComponent<BuildingResourceProducer>();
            producer.Initialize(instanceId, config, level);

            // 订阅事件
            producer.OnResourceProduced += (type, amount) => OnBuildingProduced?.Invoke(instanceId, type, amount);
            producer.OnResourceConsumed += (type, amount) => OnBuildingConsumed?.Invoke(instanceId, type, amount);
            producer.OnProductionStateChanged += (state) => OnBuildingProductionStateChanged?.Invoke(instanceId, state);

            activeProducers[instanceId] = producer;
            return producer;
        }

        /// <summary>
        /// 注销建筑生产器
        /// </summary>
        public void UnregisterBuilding(int instanceId)
        {
            if (activeProducers.TryGetValue(instanceId, out var producer))
            {
                if (producer != null && producer.gameObject != null)
                {
                    Destroy(producer.gameObject);
                }
                activeProducers.Remove(instanceId);
            }
        }

        /// <summary>
        /// 获取建筑生产器
        /// </summary>
        public BuildingResourceProducer GetProducer(int instanceId)
        {
            return activeProducers.TryGetValue(instanceId, out var producer) ? producer : null;
        }

        /// <summary>
        /// 获取建造预览
        /// </summary>
        public ResourceChangePreview GetBuildPreview(int blueprintId)
        {
            var config = GetConfig(blueprintId);
            if (config == null) return new ResourceChangePreview();

            return config.CalculateChangePreview(0, 1);
        }

        /// <summary>
        /// 获取升级预览
        /// </summary>
        public ResourceChangePreview GetUpgradePreview(int instanceId)
        {
            var producer = GetProducer(instanceId);
            if (producer == null) return new ResourceChangePreview();

            return producer.GetUpgradePreview();
        }

        /// <summary>
        /// 升级建筑
        /// </summary>
        public bool UpgradeBuilding(int instanceId)
        {
            var producer = GetProducer(instanceId);
            return producer?.Upgrade() ?? false;
        }

        /// <summary>
        /// 暂停所有生产
        /// </summary>
        public void PauseAll()
        {
            foreach (var producer in activeProducers.Values)
            {
                producer?.Pause();
            }
        }

        /// <summary>
        /// 恢复所有生产
        /// </summary>
        public void ResumeAll()
        {
            foreach (var producer in activeProducers.Values)
            {
                producer?.Resume();
            }
        }

        /// <summary>
        /// 创建存档数据
        /// </summary>
        public List<BuildingProducerSaveData> CreateSaveData()
        {
            var saveList = new List<BuildingProducerSaveData>();
            foreach (var producer in activeProducers.Values)
            {
                if (producer != null)
                {
                    saveList.Add(producer.CreateSaveData());
                }
            }
            return saveList;
        }

        /// <summary>
        /// 从存档恢复
        /// </summary>
        public void LoadFromSaveData(List<BuildingProducerSaveData> saveDataList)
        {
            if (saveDataList == null) return;

            foreach (var saveData in saveDataList)
            {
                var producer = GetProducer(saveData.buildingInstanceId);
                if (producer != null)
                {
                    producer.LoadFromSaveData(saveData);
                }
            }
        }

        /// <summary>
        /// 获取活跃生产器数量
        /// </summary>
        public int GetActiveProducerCount() => activeProducers.Count;

        /// <summary>
        /// 运行时注册配置
        /// </summary>
        public void RegisterConfig(BuildingResourceConfig config)
        {
            if (config == null) return;
            configMap[config.BlueprintId] = config;
        }
    }
}
