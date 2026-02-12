using UnityEngine;

namespace PomodoroTimer.Map.Sprite2D
{
    /// <summary>
    /// 等距视角排序工具类
    /// 用于计算2D Sprite的排序顺序
    ///
    /// ── 坐标系 ──
    /// GridToWorld:
    ///   worldX = (x - y) * tileWidth/2/ppu
    ///   worldY = (x + y) * tileHeight/2/ppu
    ///
    /// (x+y) 越大 → worldY 越大 → 屏幕越靠上 → 离镜头越远。
    /// 等距视角中，靠近镜头（屏幕下方）的物体应遮挡远处的物体，
    /// 因此 sortingOrder 需要随 (x+y) 递减：(x+y) 小 → sortingOrder 大。
    ///
    /// ── 设计说明 ──
    /// Unity sortingOrder 内部为 16-bit signed（-32768 ~ 32767）。
    /// 为了在大地图（最大 96×96）下不溢出，采用 Sorting Layer 分离垂直维度：
    ///
    ///   Sorting Layer 负责：heightLevel + floorIndex（垂直分层）
    ///   sortingOrder 负责：网格深度 + 部件类型 + 微调（水平排序）
    ///
    /// sortingOrder 公式（同一 Sorting Layer 内）：
    ///   MAX_DEPTH_ORDER - (gridPos.x + gridPos.y) * DEPTH_MULTIPLIER
    ///     + slotType * SLOT_ORDER_STEP
    ///     + variantOffset
    ///
    /// 单栋建筑内部最大跨度 = SLOT_ORDER_STEP * 8 + ~9 ≈ 89
    /// DEPTH_MULTIPLIER 取 100 > 89，保证相邻网格不交叉。
    ///
    /// 96×96 地图：MAX_DEPTH_ORDER = 190*100 = 19000
    /// 最大 sortingOrder = 19000 + 89 = 19,089 < 32,767 ✓
    ///
    /// ── Sorting Layer 列表（从后到前） ──
    /// Ground             地砖
    /// GroundDecoration    地面装饰
    /// Buildings_F0        建筑 楼层0（地面层）
    /// Buildings_F1        建筑 楼层1
    /// Buildings_F2        建筑 楼层2
    /// Buildings_F3        建筑 楼层3
    /// Buildings_F4        建筑 楼层4
    /// BuildingTop_F0      heightLevel≥1 的建筑 楼层0
    /// BuildingTop_F1      heightLevel≥1 的建筑 楼层1
    /// BuildingTop_F2      heightLevel≥1 的建筑 楼层2
    /// BuildingTop_F3      heightLevel≥1 的建筑 楼层3
    /// BuildingTop_F4      heightLevel≥1 的建筑 楼层4
    /// Effects             特效
    ///
    /// 需要在 Unity Editor → Project Settings → Tags and Layers → Sorting Layers
    /// 中按上述顺序添加这些层。
    /// </summary>
    public static class IsometricSortingHelper
    {
        // ── Sorting Layer 名称 ──
        public const string LAYER_GROUND = "Ground";
        public const string LAYER_GROUND_DECORATION = "GroundDecoration";
        public const string LAYER_EFFECTS = "Effects";

        // 建筑楼层 Sorting Layer 前缀
        private const string BUILDINGS_LAYER_PREFIX = "Buildings_F";
        private const string BUILDING_TOP_LAYER_PREFIX = "BuildingTop_F";

        /// <summary>支持的最大楼层数</summary>
        public const int MAX_FLOOR_COUNT = 5;

        // ── 排序间隔常量 ──
        // DEPTH_MULTIPLIER 必须 > 单栋建筑内部最大排序跨度。
        // 楼层已拆到 Sorting Layer，内部跨度 = SLOT_ORDER_STEP*8 + variantOffset ≈ 89
        // 取 100，96×96 地图：190*100+89 = 19,089 < 32,767 ✓
        private const int DEPTH_MULTIPLIER = 100;

        // ── 建筑内部排序常量（供 ModularBuildingRenderer 等外部引用） ──
        /// <summary>每个 PartSlotType 之间的排序间隔（需 > 最大 variantOffset）</summary>
        public const int SLOT_ORDER_STEP = 10;

        /// <summary>sortingOrder 安全上限（16-bit signed）</summary>
        private const int MAX_SORTING_ORDER = 32767;

        /// <summary>
        /// 深度排序基准值，在 ValidateMapSize 中根据实际地图尺寸设置。
        /// 默认值支持 96×96 地图。
        /// </summary>
        private static int maxDepthOrder = 190 * DEPTH_MULTIPLIER;

        /// <summary>
        /// 计算网格深度排序值（同一 Sorting Layer 内使用）。
        /// (x+y) 越大 → 离镜头越远 → sortingOrder 越小（被前方物体遮挡）。
        /// </summary>
        public static int CalculateSortingOrder(Vector2Int gridPos)
        {
            return maxDepthOrder - (gridPos.x + gridPos.y) * DEPTH_MULTIPLIER;
        }

        /// <summary>
        /// 计算地砖排序顺序
        /// </summary>
        public static int CalculateTileSortingOrder(Vector2Int gridPos)
        {
            return CalculateSortingOrder(gridPos);
        }

        /// <summary>
        /// 计算建筑排序顺序（基础值，不含部件偏移）
        /// </summary>
        public static int CalculateBuildingSortingOrder(Vector2Int gridPos, int buildingHeight = 0)
        {
            return CalculateSortingOrder(gridPos);
        }

        /// <summary>
        /// 根据 heightLevel 和 floorIndex 返回对应的 Sorting Layer 名称。
        /// heightLevel 0 → "Buildings_F{floorIndex}"
        /// heightLevel 1+ → "BuildingTop_F{floorIndex}"
        /// floorIndex 会被 clamp 到 [0, MAX_FLOOR_COUNT-1]。
        /// </summary>
        public static string GetBuildingSortingLayer(int heightLevel, int floorIndex = 0)
        {
            floorIndex = Mathf.Clamp(floorIndex, 0, MAX_FLOOR_COUNT - 1);
            string prefix = heightLevel > 0 ? BUILDING_TOP_LAYER_PREFIX : BUILDINGS_LAYER_PREFIX;
            return prefix + floorIndex;
        }

        /// <summary>
        /// 获取排序层级ID
        /// </summary>
        public static int GetSortingLayerID(string layerName)
        {
            return SortingLayer.NameToID(layerName);
        }

        /// <summary>
        /// 应用排序设置到SpriteRenderer
        /// </summary>
        public static void ApplySorting(SpriteRenderer renderer, Vector2Int gridPos,
            string sortingLayer = LAYER_GROUND, int heightLevel = 0)
        {
            renderer.sortingLayerName = sortingLayer;
            renderer.sortingOrder = CalculateSortingOrder(gridPos);
        }

        /// <summary>
        /// 应用地砖排序
        /// </summary>
        public static void ApplyTileSorting(SpriteRenderer renderer, Vector2Int gridPos)
        {
            renderer.sortingLayerName = LAYER_GROUND;
            renderer.sortingOrder = CalculateSortingOrder(gridPos);
        }

        /// <summary>
        /// 应用建筑排序（简单建筑，单 sprite，无楼层）
        /// </summary>
        public static void ApplyBuildingSorting(SpriteRenderer renderer, Vector2Int gridPos, int heightLevel = 0)
        {
            renderer.sortingLayerName = GetBuildingSortingLayer(heightLevel, 0);
            renderer.sortingOrder = CalculateSortingOrder(gridPos);
        }

        /// <summary>
        /// 验证地图尺寸并初始化深度排序基准值。
        /// 必须在地图初始化时调用。
        /// </summary>
        public static bool ValidateMapSize(int mapWidth, int mapHeight)
        {
            int maxSum = (mapWidth - 1) + (mapHeight - 1);
            maxDepthOrder = maxSum * DEPTH_MULTIPLIER;

            int maxInternalOffset = 8 * SLOT_ORDER_STEP + SLOT_ORDER_STEP; // slotType*STEP + 余量
            int worstCase = maxDepthOrder + maxInternalOffset;

            if (worstCase > MAX_SORTING_ORDER)
            {
                Debug.LogError(
                    $"[IsometricSortingHelper] 地图尺寸 {mapWidth}x{mapHeight} 会导致 sortingOrder 溢出！" +
                    $" 最大值={worstCase}, 上限={MAX_SORTING_ORDER}。" +
                    $" 请缩小地图或调整排序常量。");
                return false;
            }

            Debug.Log($"[IsometricSortingHelper] 地图 {mapWidth}x{mapHeight} 排序初始化完成，" +
                $"maxDepthOrder={maxDepthOrder}, 最大sortingOrder={worstCase}");
            return true;
        }
    }
}
