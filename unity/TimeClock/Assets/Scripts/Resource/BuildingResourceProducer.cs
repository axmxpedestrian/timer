using System;
using System.Collections.Generic;
using UnityEngine;

namespace PomodoroTimer.Resource
{
    /// <summary>
    /// 建筑资源生产器
    /// 附加到建筑实例上，处理资源的生产和消耗
    /// </summary>
    public class BuildingResourceProducer : MonoBehaviour
    {
        private BuildingResourceConfig config;
        private int buildingInstanceId;
        private int currentLevel = 1;
        private bool isProducing = true;
        private bool isPaused = false;

        // 生产计时器
        private float[] productionTimers;
        private float[] consumptionTimers;

        // 事件
        public event Action<ResourceType, long> OnResourceProduced;
        public event Action<ResourceType, long> OnResourceConsumed;
        public event Action<bool> OnProductionStateChanged;

        public int BuildingInstanceId => buildingInstanceId;
        public int CurrentLevel => currentLevel;
        public bool IsProducing => isProducing && !isPaused;
        public BuildingResourceConfig Config => config;

        /// <summary>
        /// 初始化生产器
        /// </summary>
        public void Initialize(int instanceId, BuildingResourceConfig resourceConfig, int level = 1)
        {
            buildingInstanceId = instanceId;
            config = resourceConfig;
            currentLevel = Mathf.Max(1, level);
            isProducing = true;
            isPaused = false;

            // 初始化计时器
            if (config.productions != null)
            {
                productionTimers = new float[config.productions.Length];
            }

            if (config.consumptions != null)
            {
                consumptionTimers = new float[config.consumptions.Length];
            }
        }

        private void Update()
        {
            if (!isProducing || isPaused || config == null) return;

            // 先处理消耗
            bool canProduce = ProcessConsumptions();

            // 如果消耗资源不足，暂停生产
            if (!canProduce)
            {
                if (isProducing)
                {
                    isProducing = false;
                    OnProductionStateChanged?.Invoke(false);
                }
                return;
            }
            else if (!isProducing)
            {
                isProducing = true;
                OnProductionStateChanged?.Invoke(true);
            }

            // 处理生产
            ProcessProductions();
        }

        /// <summary>
        /// 处理资源消耗
        /// </summary>
        private bool ProcessConsumptions()
        {
            if (config.consumptions == null || config.consumptions.Length == 0)
                return true;

            var resourceManager = ResourceManager.Instance;
            if (resourceManager == null) return false;

            bool allSatisfied = true;

            for (int i = 0; i < config.consumptions.Length; i++)
            {
                var consumption = config.consumptions[i];
                consumptionTimers[i] += Time.deltaTime;

                if (consumptionTimers[i] >= consumption.cycleSeconds)
                {
                    long amount = config.GetConsumptionAmount(i, currentLevel);

                    // 检查是否有足够资源
                    if (resourceManager.HasEnough(consumption.resourceType, amount))
                    {
                        resourceManager.ConsumeResource(consumption.resourceType, amount,
                            $"Building_{buildingInstanceId}");
                        consumptionTimers[i] = 0f;
                        OnResourceConsumed?.Invoke(consumption.resourceType, amount);
                    }
                    else
                    {
                        // 资源不足，不重置计时器，等待资源
                        allSatisfied = false;
                    }
                }
            }

            return allSatisfied;
        }

        /// <summary>
        /// 处理资源生产
        /// </summary>
        private void ProcessProductions()
        {
            if (config.productions == null || config.productions.Length == 0)
                return;

            var resourceManager = ResourceManager.Instance;
            if (resourceManager == null) return;

            for (int i = 0; i < config.productions.Length; i++)
            {
                var production = config.productions[i];
                productionTimers[i] += Time.deltaTime;

                if (productionTimers[i] >= production.cycleSeconds)
                {
                    long amount = config.GetProductionAmount(i, currentLevel);
                    resourceManager.AddResource(production.resourceType, amount,
                        $"Building_{buildingInstanceId}");
                    productionTimers[i] = 0f;
                    OnResourceProduced?.Invoke(production.resourceType, amount);
                }
            }
        }

        /// <summary>
        /// 升级建筑
        /// </summary>
        public bool Upgrade()
        {
            if (config == null) return false;
            if (currentLevel >= config.maxLevel) return false;

            var resourceManager = ResourceManager.Instance;
            if (resourceManager == null) return false;

            var costs = config.GetCostForLevel(currentLevel + 1);
            if (!resourceManager.HasEnough(costs)) return false;

            resourceManager.ConsumeResources(costs, $"Upgrade_Building_{buildingInstanceId}");
            currentLevel++;

            return true;
        }

        /// <summary>
        /// 暂停生产
        /// </summary>
        public void Pause()
        {
            isPaused = true;
        }

        /// <summary>
        /// 恢复生产
        /// </summary>
        public void Resume()
        {
            isPaused = false;
        }

        /// <summary>
        /// 获取当前生产进度（0-1）
        /// </summary>
        public float GetProductionProgress(int productionIndex)
        {
            if (config?.productions == null || productionIndex >= config.productions.Length)
                return 0f;
            if (productionTimers == null || productionIndex >= productionTimers.Length)
                return 0f;

            return productionTimers[productionIndex] / config.productions[productionIndex].cycleSeconds;
        }

        /// <summary>
        /// 获取资源变化预览
        /// </summary>
        public ResourceChangePreview GetUpgradePreview()
        {
            if (config == null) return new ResourceChangePreview();
            return config.CalculateChangePreview(currentLevel, currentLevel + 1);
        }

        /// <summary>
        /// 创建存档数据
        /// </summary>
        public BuildingProducerSaveData CreateSaveData()
        {
            return new BuildingProducerSaveData
            {
                buildingInstanceId = buildingInstanceId,
                configId = config?.blueprintId ?? 0,
                currentLevel = currentLevel,
                productionTimers = productionTimers != null ? (float[])productionTimers.Clone() : null,
                consumptionTimers = consumptionTimers != null ? (float[])consumptionTimers.Clone() : null
            };
        }

        /// <summary>
        /// 从存档恢复
        /// </summary>
        public void LoadFromSaveData(BuildingProducerSaveData saveData)
        {
            if (saveData == null) return;

            currentLevel = saveData.currentLevel;

            if (saveData.productionTimers != null && productionTimers != null)
            {
                int len = Mathf.Min(saveData.productionTimers.Length, productionTimers.Length);
                Array.Copy(saveData.productionTimers, productionTimers, len);
            }

            if (saveData.consumptionTimers != null && consumptionTimers != null)
            {
                int len = Mathf.Min(saveData.consumptionTimers.Length, consumptionTimers.Length);
                Array.Copy(saveData.consumptionTimers, consumptionTimers, len);
            }
        }
    }

    /// <summary>
    /// 建筑生产器存档数据
    /// </summary>
    [Serializable]
    public class BuildingProducerSaveData
    {
        public int buildingInstanceId;
        public int configId;
        public int currentLevel;
        public float[] productionTimers;
        public float[] consumptionTimers;
    }
}
