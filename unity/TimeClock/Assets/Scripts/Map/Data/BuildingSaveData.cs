using UnityEngine;
using System.Collections.Generic;

namespace PomodoroTimer.Map.Data
{
    /// <summary>
    /// 模块化建筑状态
    /// </summary>
    public enum ModularBuildingState
    {
        Constructing,   // 建造中
        Normal,         // 正常
        Upgrading,      // 升级中
        Selected,       // 被选中
        Damaged,        // 受损
        Destroyed       // 已摧毁
    }

    /// <summary>
    /// 单个建筑实例的存档数据
    /// </summary>
    [System.Serializable]
    public class BuildingInstanceSaveData
    {
        [Tooltip("实例ID")]
        public int instanceId;

        [Tooltip("建筑模板ID")]
        public int blueprintId;

        [Tooltip("网格位置")]
        public Vector2Int gridPosition;

        [Tooltip("旋转角度（0/90/180/270）")]
        public int rotation;

        [Tooltip("当前状态")]
        public ModularBuildingState state;

        [Tooltip("楼层数")]
        public int floorCount;

        [Tooltip("每层的部件配置")]
        public FloorPartSaveData[] floorConfigs;

        [Tooltip("单层建筑的部件配置")]
        public PartConfigSaveData[] partConfigs;

        [Tooltip("当前生命值")]
        public int currentHealth;

        [Tooltip("建造进度（0-1）")]
        public float constructionProgress;

        [Tooltip("创建时间戳")]
        public long createdTimestamp;

        [Tooltip("自定义数据")]
        public string customData;

        public BuildingInstanceSaveData() { }

        public BuildingInstanceSaveData(int instanceId, int blueprintId, Vector2Int gridPosition)
        {
            this.instanceId = instanceId;
            this.blueprintId = blueprintId;
            this.gridPosition = gridPosition;
            this.rotation = 0;
            this.state = ModularBuildingState.Normal;
            this.floorCount = 1;
            this.currentHealth = 100;
            this.constructionProgress = 1f;
            this.createdTimestamp = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }
    }

    /// <summary>
    /// 楼层部件存档数据
    /// </summary>
    [System.Serializable]
    public class FloorPartSaveData
    {
        public int floorIndex;
        public PartConfigSaveData[] parts;

        public FloorPartSaveData() { }

        public FloorPartSaveData(int floorIndex)
        {
            this.floorIndex = floorIndex;
        }
    }

    /// <summary>
    /// 部件配置存档数据
    /// </summary>
    [System.Serializable]
    public class PartConfigSaveData
    {
        public string slotId;
        public string variantId;
        public Color tintColor;
        public bool useCustomTint;

        public PartConfigSaveData() { }

        public PartConfigSaveData(BuildingPartConfig config)
        {
            if (config != null)
            {
                slotId = config.slotId;
                variantId = config.variantId;
                tintColor = config.tintColor;
                useCustomTint = config.useCustomTint;
            }
        }

        public BuildingPartConfig ToPartConfig()
        {
            return new BuildingPartConfig
            {
                slotId = slotId,
                variantId = variantId,
                tintColor = tintColor,
                useCustomTint = useCustomTint
            };
        }
    }

    /// <summary>
    /// 建筑系统完整存档数据
    /// </summary>
    [System.Serializable]
    public class BuildingSystemSaveData
    {
        [Tooltip("存档版本")]
        public int version = 1;

        [Tooltip("下一个可用的实例ID")]
        public int nextInstanceId;

        [Tooltip("所有建筑实例")]
        public List<BuildingInstanceSaveData> buildings;

        [Tooltip("存档时间戳")]
        public long savedTimestamp;

        public BuildingSystemSaveData()
        {
            version = 1;
            nextInstanceId = 1;
            buildings = new List<BuildingInstanceSaveData>();
            savedTimestamp = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }

        /// <summary>
        /// 序列化为JSON
        /// </summary>
        public string ToJson(bool prettyPrint = false)
        {
            savedTimestamp = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            return JsonUtility.ToJson(this, prettyPrint);
        }

        /// <summary>
        /// 从JSON反序列化
        /// </summary>
        public static BuildingSystemSaveData FromJson(string json)
        {
            if (string.IsNullOrEmpty(json))
                return new BuildingSystemSaveData();

            try
            {
                return JsonUtility.FromJson<BuildingSystemSaveData>(json);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[BuildingSystemSaveData] Failed to parse JSON: {e.Message}");
                return new BuildingSystemSaveData();
            }
        }

        /// <summary>
        /// 添加建筑存档
        /// </summary>
        public void AddBuilding(BuildingInstanceSaveData building)
        {
            if (buildings == null)
                buildings = new List<BuildingInstanceSaveData>();
            buildings.Add(building);
        }

        /// <summary>
        /// 移除建筑存档
        /// </summary>
        public bool RemoveBuilding(int instanceId)
        {
            if (buildings == null)
                return false;
            return buildings.RemoveAll(b => b.instanceId == instanceId) > 0;
        }

        /// <summary>
        /// 获取建筑存档
        /// </summary>
        public BuildingInstanceSaveData GetBuilding(int instanceId)
        {
            if (buildings == null)
                return null;
            return buildings.Find(b => b.instanceId == instanceId);
        }

        /// <summary>
        /// 清空所有建筑
        /// </summary>
        public void Clear()
        {
            buildings?.Clear();
            nextInstanceId = 1;
        }
    }
}
