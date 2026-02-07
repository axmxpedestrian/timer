using UnityEngine;

namespace PomodoroTimer.Map.Data
{
    [CreateAssetMenu(fileName = "BuildingSpriteData", menuName = "Map/Building Sprite Data")]
    public class BuildingSpriteData : ScriptableObject
    {
        [Header("基础信息")]
        public int buildingTypeId;
        public string buildingName;

        [Header("尺寸设置")]
        [Tooltip("建筑占用的网格大小")]
        public Vector2Int gridSize = Vector2Int.one;
        [Tooltip("建筑高度层级（影响排序）")]
        public int heightLevel = 0;
        [Tooltip("Y轴偏移（像素）")]
        public float yOffset = 0f;

        [Header("动画帧 - Idle状态")]
        public Sprite[] idleFrames;

        [Header("动画帧 - Active状态")]
        public Sprite[] activeFrames;

        [Header("动画帧 - Building状态")]
        public Sprite[] buildingFrames;

        [Header("动画帧 - Destroyed状态")]
        public Sprite[] destroyedFrames;

        [Header("动画设置")]
        [Tooltip("动画帧率")]
        public float frameRate = 8f;

        [Header("游戏属性")]
        public int buildCost;
        public int maxHealth = 100;
        public bool isWalkable = false;

        public Sprite GetPreviewSprite()
        {
            if (idleFrames != null && idleFrames.Length > 0)
                return idleFrames[0];
            return null;
        }
    }
}