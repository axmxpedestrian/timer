using System;
using System.Collections.Generic;
using UnityEngine;

namespace PomodoroTimer.Resource
{
    /// <summary>
    /// 资源消耗/产出配置
    /// </summary>
    [Serializable]
    public class ResourceCost
    {
        public ResourceType resourceType;
        public long amount;

        public ResourceCost() { }

        public ResourceCost(ResourceType type, long amount)
        {
            this.resourceType = type;
            this.amount = amount;
        }
    }

    /// <summary>
    /// 资源生产配置
    /// </summary>
    [Serializable]
    public class ResourceProduction
    {
        [Tooltip("产出的资源类型")]
        public ResourceType resourceType;

        [Tooltip("每次产出数量")]
        public long amountPerCycle;

        [Tooltip("生产周期（秒）")]
        public float cycleSeconds = 5f;

        [Tooltip("产出动画显示频率（秒）")]
        public float animationInterval = 5f;
    }

    /// <summary>
    /// 资源消耗配置（持续消耗）
    /// </summary>
    [Serializable]
    public class ResourceConsumption
    {
        [Tooltip("消耗的资源类型")]
        public ResourceType resourceType;

        [Tooltip("每次消耗数量")]
        public long amountPerCycle;

        [Tooltip("消耗周期（秒）")]
        public float cycleSeconds = 5f;
    }

    /// <summary>
    /// 建筑资源配置 - ScriptableObject
    /// 定义建筑的资源相关属性
    /// </summary>
    [CreateAssetMenu(fileName = "BuildingResourceConfig", menuName = "Resource/Building Resource Config")]
    public class BuildingResourceConfig : ScriptableObject
    {
        [Header("基础信息")]
        [Tooltip("对应的建筑模板ID")]
        public int blueprintId;

        [Tooltip("建筑名称")]
        public string buildingName;

        [Header("建造花费")]
        [Tooltip("初次建造花费")]
        public ResourceCost[] buildCosts;

        [Header("升级花费")]
        [Tooltip("每级升级花费倍率")]
        public float upgradeCostMultiplier = 1.5f;

        [Tooltip("最大等级")]
        public int maxLevel = 10;

        [Header("资源生产")]
        [Tooltip("资源产出配置")]
        public ResourceProduction[] productions;

        [Tooltip("每级产出增加倍率")]
        public float productionLevelMultiplier = 1.5f;

        [Header("资源消耗")]
        [Tooltip("持续资源消耗配置")]
        public ResourceConsumption[] consumptions;

        [Tooltip("每级消耗增加倍率")]
        public float consumptionLevelMultiplier = 1.5f;

        /// <summary>
        /// 获取指定等级的建造/升级花费
        /// </summary>
        public Dictionary<ResourceType, long> GetCostForLevel(int level)
        {
            var costs = new Dictionary<ResourceType, long>();
            if (buildCosts == null) return costs;

            float multiplier = level <= 1 ? 1f : Mathf.Pow(upgradeCostMultiplier, level - 1);

            foreach (var cost in buildCosts)
            {
                long amount = Mathf.RoundToInt(cost.amount * multiplier);
                if (costs.ContainsKey(cost.resourceType))
                    costs[cost.resourceType] += amount;
                else
                    costs[cost.resourceType] = amount;
            }

            return costs;
        }

        /// <summary>
        /// 获取指定等级的产出量
        /// </summary>
        public long GetProductionAmount(int productionIndex, int level)
        {
            if (productions == null || productionIndex >= productions.Length)
                return 0;

            var production = productions[productionIndex];
            float multiplier = level <= 1 ? 1f : Mathf.Pow(productionLevelMultiplier, level - 1);
            return Mathf.RoundToInt(production.amountPerCycle * multiplier);
        }

        /// <summary>
        /// 获取指定等级的消耗量
        /// </summary>
        public long GetConsumptionAmount(int consumptionIndex, int level)
        {
            if (consumptions == null || consumptionIndex >= consumptions.Length)
                return 0;

            var consumption = consumptions[consumptionIndex];
            float multiplier = level <= 1 ? 1f : Mathf.Pow(consumptionLevelMultiplier, level - 1);
            return Mathf.RoundToInt(consumption.amountPerCycle * multiplier);
        }

        /// <summary>
        /// 计算资源变化预览（用于放置/升级预览）
        /// </summary>
        public ResourceChangePreview CalculateChangePreview(int currentLevel, int targetLevel)
        {
            var preview = new ResourceChangePreview();

            // 计算花费
            if (targetLevel > currentLevel)
            {
                for (int lvl = currentLevel + 1; lvl <= targetLevel; lvl++)
                {
                    var levelCosts = GetCostForLevel(lvl);
                    foreach (var kvp in levelCosts)
                    {
                        if (preview.costs.ContainsKey(kvp.Key))
                            preview.costs[kvp.Key] += kvp.Value;
                        else
                            preview.costs[kvp.Key] = kvp.Value;
                    }
                }
            }

            // 计算产出变化（每周期）
            if (productions != null)
            {
                foreach (var production in productions)
                {
                    long oldAmount = currentLevel > 0 ? GetProductionAmountForLevel(production, currentLevel) : 0;
                    long newAmount = GetProductionAmountForLevel(production, targetLevel);
                    long delta = newAmount - oldAmount;

                    if (delta != 0)
                    {
                        preview.productionChanges.Add(new ResourceChangeItem
                        {
                            resourceType = production.resourceType,
                            amountPerCycle = delta,
                            cycleSeconds = production.cycleSeconds
                        });
                    }
                }
            }

            // 计算消耗变化（每周期）
            if (consumptions != null)
            {
                foreach (var consumption in consumptions)
                {
                    long oldAmount = currentLevel > 0 ? GetConsumptionAmountForLevel(consumption, currentLevel) : 0;
                    long newAmount = GetConsumptionAmountForLevel(consumption, targetLevel);
                    long delta = newAmount - oldAmount;

                    if (delta != 0)
                    {
                        preview.consumptionChanges.Add(new ResourceChangeItem
                        {
                            resourceType = consumption.resourceType,
                            amountPerCycle = delta,
                            cycleSeconds = consumption.cycleSeconds
                        });
                    }
                }
            }

            return preview;
        }

        private long GetProductionAmountForLevel(ResourceProduction production, int level)
        {
            float multiplier = level <= 1 ? 1f : Mathf.Pow(productionLevelMultiplier, level - 1);
            return Mathf.RoundToInt(production.amountPerCycle * multiplier);
        }

        private long GetConsumptionAmountForLevel(ResourceConsumption consumption, int level)
        {
            float multiplier = level <= 1 ? 1f : Mathf.Pow(consumptionLevelMultiplier, level - 1);
            return Mathf.RoundToInt(consumption.amountPerCycle * multiplier);
        }
    }

    /// <summary>
    /// 资源变化预览
    /// </summary>
    public class ResourceChangePreview
    {
        public Dictionary<ResourceType, long> costs = new Dictionary<ResourceType, long>();
        public List<ResourceChangeItem> productionChanges = new List<ResourceChangeItem>();
        public List<ResourceChangeItem> consumptionChanges = new List<ResourceChangeItem>();

        /// <summary>
        /// 检查是否能负担花费
        /// </summary>
        public bool CanAfford()
        {
            var resourceManager = ResourceManager.Instance;
            if (resourceManager == null) return false;
            return resourceManager.HasEnough(costs);
        }
    }

    /// <summary>
    /// 资源变化项
    /// </summary>
    public class ResourceChangeItem
    {
        public ResourceType resourceType;
        public long amountPerCycle;
        public float cycleSeconds;

        /// <summary>
        /// 获取每秒变化量
        /// </summary>
        public float GetPerSecond()
        {
            if (cycleSeconds <= 0) return 0;
            return amountPerCycle / cycleSeconds;
        }
    }
}
