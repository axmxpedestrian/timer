using UnityEngine;

namespace PomodoroTimer.Map.Sprite2D
{
    /// <summary>
    /// 等距视角排序工具类
    /// 用于计算2D Sprite的排序顺序
    /// </summary>
    public static class IsometricSortingHelper
    {
        // 排序层级名称
        public const string LAYER_GROUND = "Ground";
        public const string LAYER_GROUND_DECORATION = "GroundDecoration";
        public const string LAYER_BUILDINGS = "Buildings";
        public const string LAYER_BUILDING_TOP = "BuildingTop";
        public const string LAYER_EFFECTS = "Effects";

        // 排序间隔常量
        private const int DEPTH_MULTIPLIER = 10;      // 网格深度乘数
        private const int HEIGHT_MULTIPLIER = 1000;   // 高度层级乘数

        /// <summary>
        /// 计算排序顺序
        /// </summary>
        /// <param name="gridPos">网格坐标</param>
        /// <param name="heightLevel">高度层级（0=地面，1=一层建筑等）</param>
        /// <returns>排序顺序值</returns>
        public static int CalculateSortingOrder(Vector2Int gridPos, int heightLevel = 0)
        {
            int depthOrder = (gridPos.x + gridPos.y) * DEPTH_MULTIPLIER;
            int heightOrder = heightLevel * HEIGHT_MULTIPLIER;
            return depthOrder + heightOrder;
        }

        /// <summary>
        /// 计算地砖排序顺序
        /// </summary>
        public static int CalculateTileSortingOrder(Vector2Int gridPos)
        {
            return CalculateSortingOrder(gridPos, 0);
        }

        /// <summary>
        /// 计算建筑排序顺序
        /// </summary>
        /// <param name="gridPos">建筑基础网格坐标</param>
        /// <param name="buildingHeight">建筑高度层级</param>
        public static int CalculateBuildingSortingOrder(Vector2Int gridPos, int buildingHeight = 0)
        {
            return CalculateSortingOrder(gridPos, 1 + buildingHeight);
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
            renderer.sortingOrder = CalculateSortingOrder(gridPos, heightLevel);
        }

        /// <summary>
        /// 应用地砖排序
        /// </summary>
        public static void ApplyTileSorting(SpriteRenderer renderer, Vector2Int gridPos)
        {
            ApplySorting(renderer, gridPos, LAYER_GROUND, 0);
        }

        /// <summary>
        /// 应用建筑排序
        /// </summary>
        public static void ApplyBuildingSorting(SpriteRenderer renderer, Vector2Int gridPos, int heightLevel = 0)
        {
            ApplySorting(renderer, gridPos, LAYER_BUILDINGS, 1 + heightLevel);
        }
    }
}
