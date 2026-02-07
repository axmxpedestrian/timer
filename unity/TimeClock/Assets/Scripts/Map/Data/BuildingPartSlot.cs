using UnityEngine;

namespace PomodoroTimer.Map.Data
{
    /// <summary>
    /// 部件槽位类型
    /// </summary>
    public enum PartSlotType
    {
        Foundation,     // 地基
        Frame,          // 框架
        Walls,          // 墙壁
        Windows,        // 窗户
        Doors,          // 门
        Roof,           // 屋顶
        Decorations,    // 装饰
        Chimney,        // 烟囱
        Lights          // 灯光
    }

    /// <summary>
    /// 部件变体 - 定义一个部件的具体外观
    /// </summary>
    [System.Serializable]
    public class PartVariant
    {
        [Tooltip("变体唯一标识")]
        public string variantId;

        [Tooltip("动画帧（灰度图用于染色）")]
        public Sprite[] frames;

        [Tooltip("是否支持染色")]
        public bool isTintable = true;

        [Tooltip("默认染色颜色")]
        public Color defaultTint = Color.white;

        [Tooltip("相对于建筑原点的本地偏移")]
        public Vector2 localOffset;

        [Tooltip("排序顺序偏移")]
        public int sortingOrderOffset;

        public Sprite GetFrame(int index)
        {
            if (frames == null || frames.Length == 0)
                return null;
            return frames[Mathf.Clamp(index, 0, frames.Length - 1)];
        }

        public int FrameCount => frames?.Length ?? 0;
    }

    /// <summary>
    /// 建筑部件槽位 - 定义建筑的一个可配置部件位置
    /// </summary>
    [System.Serializable]
    public class BuildingPartSlot
    {
        [Tooltip("槽位类型")]
        public PartSlotType slotType;

        [Tooltip("槽位唯一标识")]
        public string slotId;

        [Tooltip("是否必需")]
        public bool isRequired = true;

        [Tooltip("是否允许多个实例")]
        public bool allowMultiple = false;

        [Tooltip("最大数量（当allowMultiple为true时）")]
        public int maxCount = 1;

        [Tooltip("可用的变体列表")]
        public PartVariant[] availableVariants;

        [Tooltip("默认变体索引")]
        public int defaultVariantIndex = 0;

        public PartVariant GetDefaultVariant()
        {
            if (availableVariants == null || availableVariants.Length == 0)
                return null;
            return availableVariants[Mathf.Clamp(defaultVariantIndex, 0, availableVariants.Length - 1)];
        }

        public PartVariant GetVariant(string variantId)
        {
            if (availableVariants == null)
                return null;
            foreach (var variant in availableVariants)
            {
                if (variant.variantId == variantId)
                    return variant;
            }
            return null;
        }

        public PartVariant GetVariant(int index)
        {
            if (availableVariants == null || availableVariants.Length == 0)
                return null;
            return availableVariants[Mathf.Clamp(index, 0, availableVariants.Length - 1)];
        }

        public int VariantCount => availableVariants?.Length ?? 0;
    }

    /// <summary>
    /// 建筑楼层定义 - 支持多层建筑
    /// </summary>
    [System.Serializable]
    public class BuildingFloor
    {
        [Tooltip("楼层索引：0=地下室, 1=一楼, 2=二楼...")]
        public int floorIndex;

        [Tooltip("楼层名称")]
        public string floorName;

        [Tooltip("相对于建筑原点的Y偏移（像素）")]
        public float heightOffset;

        [Tooltip("该楼层的部件槽位")]
        public BuildingPartSlot[] partSlots;

        [Tooltip("该楼层的基础排序值")]
        public int sortingOrderBase;

        public BuildingPartSlot GetSlot(string slotId)
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

        public BuildingPartSlot GetSlotByType(PartSlotType type)
        {
            if (partSlots == null)
                return null;
            foreach (var slot in partSlots)
            {
                if (slot.slotType == type)
                    return slot;
            }
            return null;
        }
    }

    /// <summary>
    /// 建筑部件配置 - 运行时存储的部件选择
    /// </summary>
    [System.Serializable]
    public class BuildingPartConfig
    {
        [Tooltip("槽位ID")]
        public string slotId;

        [Tooltip("选择的变体ID")]
        public string variantId;

        [Tooltip("自定义染色颜色")]
        public Color tintColor = Color.white;

        [Tooltip("是否使用自定义颜色")]
        public bool useCustomTint = false;

        public BuildingPartConfig() { }

        public BuildingPartConfig(string slotId, string variantId)
        {
            this.slotId = slotId;
            this.variantId = variantId;
        }

        public BuildingPartConfig(string slotId, string variantId, Color tintColor)
        {
            this.slotId = slotId;
            this.variantId = variantId;
            this.tintColor = tintColor;
            this.useCustomTint = true;
        }
    }
}
