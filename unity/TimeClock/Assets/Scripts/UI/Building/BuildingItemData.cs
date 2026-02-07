using UnityEngine;
using PomodoroTimer.Map.Data;

namespace PomodoroTimer.UI.Building
{
    /// <summary>
    /// 建造物项数据（用于UI绑定）
    /// </summary>
    public class BuildingItemData
    {
        public BuildingBlueprint blueprint;
        public Sprite iconSprite;
        public string buildingName;
        public BuildingCostEntry[] costs;
        public bool isAffordable;
        public bool isUnlocked;
        public int techLevel;

        public BuildingItemData(BuildingBlueprint bp)
        {
            blueprint = bp;
            iconSprite = bp.GetPreviewSprite();
            buildingName = bp.buildingName;
            costs = bp.buildCosts;
            techLevel = bp.techLevel;
            isUnlocked = bp.isUnlocked;
            isAffordable = bp.CanAfford();
        }

        /// <summary>
        /// 刷新可负担状态（缓存结果）
        /// </summary>
        public void RefreshAffordable()
        {
            if (blueprint != null)
            {
                isAffordable = blueprint.CanAfford();
            }
        }
    }
}
