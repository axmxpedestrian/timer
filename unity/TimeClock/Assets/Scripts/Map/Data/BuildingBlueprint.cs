using UnityEngine;
using System.Collections.Generic;
using PomodoroTimer.Resource;

namespace PomodoroTimer.Map.Data
{
    /// <summary>
    /// 建筑类别
    /// </summary>
    public enum BuildingCategory
    {
        All,            // 全部（用于筛选）
        Residential,    // 住宅/建筑
        Road,           // 道路
        Nature,         // 自然
        Facility,       // 设施
        Structure,      // 结构
        Decoration,     // 装饰
        Commercial,     // 商业
        Industrial,     // 工业
        Infrastructure, // 基础设施
        Special         // 特殊建筑
    }

    /// <summary>
    /// 建筑特效配置
    /// </summary>
    [System.Serializable]
    public class BuildingEffectConfig
    {
        [Tooltip("特效类型")]
        public BuildingEffectType effectType;

        [Tooltip("特效预制体")]
        public GameObject effectPrefab;

        [Tooltip("相对于建筑原点的偏移")]
        public Vector3 localOffset;

        [Tooltip("是否响应时间系统")]
        public bool respondsToTime = true;

        [Tooltip("激活时间范围（归一化，0-1）")]
        public Vector2 activeTimeRange = new Vector2(0.75f, 0.25f); // 夜间激活

        [Tooltip("是否响应天气")]
        public bool respondsToWeather = false;
    }

    /// <summary>
    /// 建筑特效类型
    /// </summary>
    public enum BuildingEffectType
    {
        Smoke,          // 烟雾
        Light,          // 灯光
        Particle,       // 粒子
        Animation       // 动画特效
    }

    /// <summary>
    /// 建筑模板 - ScriptableObject
    /// 定义一种建筑类型的所有配置
    /// </summary>
    [CreateAssetMenu(fileName = "BuildingBlueprint", menuName = "Map/Building Blueprint")]
    public class BuildingBlueprint : ScriptableObject
    {
        [Header("基础信息")]
        [Tooltip("建筑模板ID")]
        public int blueprintId;

        [Tooltip("建筑名称")]
        public string buildingName;

        [Tooltip("建筑类别")]
        public BuildingCategory category;

        [Tooltip("预览图标")]
        public Sprite previewIcon;

        [Tooltip("建筑描述")]
        [TextArea(2, 4)]
        public string description;

        [Header("网格设置")]
        [Tooltip("基础尺寸（矩形边界）")]
        public Vector2Int baseSize = Vector2Int.one;

        [Tooltip("占位掩码（不规则形状）")]
        public OccupancyMask occupancyMask;

        [Tooltip("高度层级（影响排序）")]
        public int heightLevel = 0;

        [Tooltip("Y轴偏移（像素）")]
        public float yOffset = 0f;

        [Header("多层结构")]
        [Tooltip("建筑楼层定义")]
        public BuildingFloor[] floors;

        [Tooltip("默认楼层数")]
        public int defaultFloorCount = 1;

        [Header("模块化部件（单层建筑简化配置）")]
        [Tooltip("部件槽位")]
        public BuildingPartSlot[] partSlots;

        [Tooltip("默认部件配置")]
        public BuildingPartConfig[] defaultParts;

        [Header("动画设置")]
        [Tooltip("动画帧率")]
        public float frameRate = 8f;

        [Tooltip("是否有待机动画")]
        public bool hasIdleAnimation = false;

        [Header("特效配置")]
        [Tooltip("建筑特效")]
        public BuildingEffectConfig[] effects;

        [Header("音效")]
        [Tooltip("点击音效")]
        public AudioClip clickSound;

        [Tooltip("放置音效")]
        public AudioClip placeSound;

        [Tooltip("环境音效")]
        public AudioClip ambientSound;

        [Header("游戏属性")]
        [Tooltip("建造消耗资源列表")]
        public ResourceCost[] buildCosts;

        [Tooltip("建造时间（秒）")]
        public int buildTimeSeconds;

        [Tooltip("最大生命值")]
        public int maxHealth = 100;

        [Tooltip("是否可行走")]
        public bool isWalkable = false;

        [Header("科技与解锁")]
        [Tooltip("科技等级要求（0=初始可用）")]
        public int techLevel = 0;

        [Tooltip("是否已解锁（运行时状态）")]
        [System.NonSerialized]
        public bool isUnlocked = true;

        [Tooltip("解锁条件描述")]
        [TextArea(1, 2)]
        public string unlockConditionDesc;

        [Header("标签分类")]
        [Tooltip("建筑类型标签（如：住宅、商铺、仓库）")]
        public List<string> buildingTypeTags = new List<string>();

        [Tooltip("风格标签（如：中式、欧式、现代）")]
        public List<string> styleTags = new List<string>();

        [Tooltip("资源消耗类型标签（如：木材、石材、金币）")]
        public List<string> consumptionTypeTags = new List<string>();

        [Tooltip("资源生成类型标签（如：食物、金币、科技点）")]
        public List<string> productionTypeTags = new List<string>();

        /// <summary>
        /// 按分类索引获取对应的标签列表
        /// 0=建筑类型, 1=风格, 2=资源消耗类型, 3=资源生成类型
        /// </summary>
        public List<string> GetTagsByCategory(int categoryIndex)
        {
            switch (categoryIndex)
            {
                case 0: return buildingTypeTags;
                case 1: return styleTags;
                case 2: return consumptionTypeTags;
                case 3: return productionTypeTags;
                default: return null;
            }
        }

        /// <summary>
        /// 检查指定分类下是否包含给定标签集合中的任意一个（OR逻辑）
        /// </summary>
        public bool HasAnyTag(int categoryIndex, HashSet<string> tags)
        {
            if (tags == null || tags.Count == 0) return true;

            var list = GetTagsByCategory(categoryIndex);
            if (list == null) return false;

            foreach (var tag in list)
            {
                if (tags.Contains(tag)) return true;
            }
            return false;
        }

        /// <summary>
        /// 获取有效的占位掩码
        /// </summary>
        public OccupancyMask GetOccupancyMask()
        {
            if (occupancyMask != null && occupancyMask.Width > 0 && occupancyMask.Height > 0)
                return occupancyMask;
            // 如果没有自定义掩码，创建矩形掩码
            return OccupancyMask.CreateRectangle(baseSize.x, baseSize.y);
        }

        /// <summary>
        /// 获取旋转后的占位掩码
        /// </summary>
        public OccupancyMask GetRotatedMask(int rotation)
        {
            return GetOccupancyMask().GetRotated(rotation);
        }

        /// <summary>
        /// 获取预览Sprite
        /// </summary>
        public Sprite GetPreviewSprite()
        {
            if (previewIcon != null)
                return previewIcon;

            // 尝试从第一个部件获取
            if (partSlots != null && partSlots.Length > 0)
            {
                var firstSlot = partSlots[0];
                var variant = firstSlot.GetDefaultVariant();
                if (variant != null && variant.frames != null && variant.frames.Length > 0)
                    return variant.frames[0];
            }

            // 尝试从楼层获取
            if (floors != null && floors.Length > 0)
            {
                var firstFloor = floors[0];
                if (firstFloor.partSlots != null && firstFloor.partSlots.Length > 0)
                {
                    var variant = firstFloor.partSlots[0].GetDefaultVariant();
                    if (variant != null && variant.frames != null && variant.frames.Length > 0)
                        return variant.frames[0];
                }
            }

            return null;
        }

        /// <summary>
        /// 是否为多层建筑
        /// </summary>
        public bool IsMultiFloor => floors != null && floors.Length > 1;

        /// <summary>
        /// 检查是否有足够资源建造
        /// </summary>
        public bool CanAfford()
        {
            if (buildCosts == null || buildCosts.Length == 0)
                return true;  // 免费建筑

            var resourceManager = ResourceManager.Instance;
            if (resourceManager == null) return false;

            foreach (var cost in buildCosts)
            {
                if (resourceManager.GetAmount(cost.resourceType) < cost.amount)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// 消耗建造资源
        /// </summary>
        public bool ConsumeBuildCost()
        {
            if (!CanAfford()) return false;

            var resourceManager = ResourceManager.Instance;
            if (resourceManager == null) return false;

            if (buildCosts == null || buildCosts.Length == 0)
                return true;  // 免费建筑

            foreach (var cost in buildCosts)
            {
                resourceManager.ConsumeResource(cost.resourceType, cost.amount, "Building");
            }
            return true;
        }

        /// <summary>
        /// 检查科技等级是否满足
        /// </summary>
        public bool IsTechUnlocked(int currentTechLevel)
        {
            return techLevel <= currentTechLevel;
        }

        /// <summary>
        /// 获取楼层
        /// </summary>
        public BuildingFloor GetFloor(int floorIndex)
        {
            if (floors == null)
                return null;
            foreach (var floor in floors)
            {
                if (floor.floorIndex == floorIndex)
                    return floor;
            }
            return null;
        }

        /// <summary>
        /// 获取部件槽位（单层建筑）
        /// </summary>
        public BuildingPartSlot GetPartSlot(string slotId)
        {
            if (partSlots == null)
                return null;
            foreach (var slot in partSlots)
            {
                if (slot.slotId == slotId)
                    return slot;
            }
            return null;
        }

        /// <summary>
        /// 获取默认部件配置
        /// </summary>
        public BuildingPartConfig GetDefaultPartConfig(string slotId)
        {
            if (defaultParts == null)
                return null;
            foreach (var config in defaultParts)
            {
                if (config.slotId == slotId)
                    return config;
            }
            return null;
        }

        /// <summary>
        /// 验证配置是否有效
        /// </summary>
        public bool Validate(out string error)
        {
            error = null;

            if (blueprintId <= 0)
            {
                error = "Blueprint ID must be positive";
                return false;
            }

            if (string.IsNullOrEmpty(buildingName))
            {
                error = "Building name is required";
                return false;
            }

            if (baseSize.x <= 0 || baseSize.y <= 0)
            {
                error = "Base size must be positive";
                return false;
            }

            return true;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // 确保baseSize至少为1x1
            baseSize.x = Mathf.Max(1, baseSize.x);
            baseSize.y = Mathf.Max(1, baseSize.y);

            // 确保默认楼层数至少为1
            defaultFloorCount = Mathf.Max(1, defaultFloorCount);

            // 标签验证警告
            if (buildingTypeTags == null || buildingTypeTags.Count == 0)
                Debug.LogWarning($"[BuildingBlueprint] '{buildingName}' 缺少建筑类型标签(buildingTypeTags)", this);
            if (consumptionTypeTags == null || consumptionTypeTags.Count == 0)
                Debug.LogWarning($"[BuildingBlueprint] '{buildingName}' 缺少资源消耗类型标签(consumptionTypeTags)", this);
            if (productionTypeTags == null || productionTypeTags.Count == 0)
                Debug.LogWarning($"[BuildingBlueprint] '{buildingName}' 缺少资源生成类型标签(productionTypeTags)", this);
        }
#endif
    }
}
