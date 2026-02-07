using UnityEngine;

namespace PomodoroTimer.Map.Data
{
    /// <summary>
    /// 地砖Sprite配置数据
    /// </summary>
    [CreateAssetMenu(fileName = "TileSpriteData", menuName = "Map/Tile Sprite Data")]
    public class TileSpriteData : ScriptableObject
    {
        [Header("基础设置")]
        [Tooltip("地砖类型ID")]
        public int tileTypeId;

        [Tooltip("地砖名称")]
        public string tileName;

        [Header("Sprite设置")]
        [Tooltip("地砖Sprite")]
        public Sprite tileSprite;

        [Tooltip("是否可通行")]
        public bool isWalkable = true;

        [Tooltip("建造成本")]
        public int buildCost;

        [Header("视觉设置")]
        [Tooltip("颜色叠加")]
        public Color tintColor = Color.white;

        [Tooltip("排序层级偏移")]
        public int sortingOrderOffset;
    }

    /// <summary>
    /// 地砖类型枚举
    /// </summary>
    public enum TileType
    {
        Grass = 0,
        Dirt = 1,
        Stone = 2,
        Water = 3,
        Sand = 4,
        Snow = 5
    }
}
