using UnityEngine;

namespace PomodoroTimer.Map.Data
{
    /// <summary>
    /// 部件槽位类型
    /// 枚举值同时作为默认渲染排序基准（值越大越靠前绘制）
    /// 实际排序：
    ///   Sorting Layer = GetBuildingSortingLayer(heightLevel, floorIndex)  ← 垂直分层
    ///   sortingOrder  = baseOrder + slotType * SLOT_ORDER_STEP + sortingOrderOffset  ← 水平排序
    /// 常量定义见 IsometricSortingHelper
    /// </summary>
    public enum PartSlotType
    {
        Foundation  = 0,    // 地基（最底层）
        Frame       = 1,    // 框架
        Walls       = 2,    // 墙壁
        Doors       = 3,    // 门
        Windows     = 4,    // 窗户
        Roof        = 5,    // 屋顶
        Decorations = 6,    // 装饰
        Chimney     = 7,    // 烟囱
        Lights      = 8     // 灯光（最顶层）
    }

    /// <summary>
    /// 方向视图数据 - 存储某个旋转角度下的 sprite 和偏移
    /// </summary>
    [System.Serializable]
    public class DirectionalView
    {
        [Tooltip("旋转角度：0/90/180/270")]
        public int rotation;

        [Tooltip("该方向的动画帧")]
        public Sprite[] frames;

        [Tooltip("该方向的本地偏移（覆盖默认 localOffset）")]
        public Vector2 localOffset;

        [Tooltip("该方向的排序偏移微调（叠加在默认 sortingOrderOffset 上）")]
        public int sortingOrderOffsetDelta;
    }

    /// <summary>
    /// 部件变体 - 定义一个部件的具体外观
    /// </summary>
    [System.Serializable]
    public class PartVariant
    {
        [Tooltip("变体唯一标识")]
        public string variantId;

        [Tooltip("默认动画帧（0°方向，或无方向视图时使用）")]
        public Sprite[] frames;

        [Tooltip("方向视图列表（可选，为90°/180°/270°提供不同sprite）")]
        public DirectionalView[] directionalViews;

        [Tooltip("是否支持染色")]
        public bool isTintable = true;

        [Tooltip("默认染色颜色")]
        public Color defaultTint = Color.white;

        [Tooltip("相对于建筑原点的本地偏移（默认/0°方向）")]
        public Vector2 localOffset;

        [Tooltip("排序顺序偏移")]
        public int sortingOrderOffset;

        /// <summary>
        /// 获取指定旋转角度的方向视图，没有则返回 null
        /// </summary>
        public DirectionalView GetDirectionalView(int rotation)
        {
            if (directionalViews == null || directionalViews.Length == 0)
                return null;
            rotation = ((rotation % 360) + 360) % 360;
            foreach (var view in directionalViews)
            {
                if (view.rotation == rotation)
                    return view;
            }
            return null;
        }

        /// <summary>
        /// 获取指定旋转角度下的帧（优先方向视图，回退到默认 frames）
        /// </summary>
        public Sprite GetFrameForRotation(int frameIndex, int rotation)
        {
            var view = GetDirectionalView(rotation);
            if (view != null && view.frames != null && view.frames.Length > 0)
                return view.frames[Mathf.Clamp(frameIndex, 0, view.frames.Length - 1)];
            return GetFrame(frameIndex);
        }

        /// <summary>
        /// 获取指定旋转角度下的本地偏移
        /// </summary>
        public Vector2 GetLocalOffsetForRotation(int rotation)
        {
            var view = GetDirectionalView(rotation);
            if (view != null)
                return view.localOffset;
            return localOffset;
        }

        /// <summary>
        /// 获取指定旋转角度下的排序偏移
        /// </summary>
        public int GetSortingOrderOffsetForRotation(int rotation)
        {
            var view = GetDirectionalView(rotation);
            if (view != null)
                return sortingOrderOffset + view.sortingOrderOffsetDelta;
            return sortingOrderOffset;
        }

        /// <summary>
        /// 获取指定旋转角度下的帧数
        /// </summary>
        public int GetFrameCountForRotation(int rotation)
        {
            var view = GetDirectionalView(rotation);
            if (view != null && view.frames != null && view.frames.Length > 0)
                return view.frames.Length;
            return FrameCount;
        }

        /// <summary>
        /// 是否有方向视图配置
        /// </summary>
        public bool HasDirectionalViews => directionalViews != null && directionalViews.Length > 0;

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
