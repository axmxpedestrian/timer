using UnityEngine;
using System.Collections.Generic;

namespace PomodoroTimer.Map.Sprite2D
{
    /// <summary>
    /// 模块化建筑对象池
    /// 管理ModularBuildingInstance的创建和回收
    /// </summary>
    public class ModularBuildingPool
    {
        private readonly Stack<ModularBuildingInstance> pool;
        private readonly Transform parent;
        private readonly Material sharedMaterial;
        private readonly int initialCapacity;
        private readonly int maxCapacity;
        private int activeCount;

        public int ActiveCount => activeCount;
        public int PooledCount => pool.Count;
        public int TotalCount => activeCount + pool.Count;

        public ModularBuildingPool(Transform parent, Material sharedMaterial,
            int initialCapacity = 64, int maxCapacity = 512)
        {
            this.parent = parent;
            this.sharedMaterial = sharedMaterial;
            this.initialCapacity = initialCapacity;
            this.maxCapacity = maxCapacity;
            this.pool = new Stack<ModularBuildingInstance>(initialCapacity);
            this.activeCount = 0;
            PrewarmPool();
        }

        private void PrewarmPool()
        {
            for (int i = 0; i < initialCapacity; i++)
            {
                var building = CreateBuildingInstance();
                building.gameObject.SetActive(false);
                pool.Push(building);
            }
        }

        private ModularBuildingInstance CreateBuildingInstance()
        {
            var go = new GameObject("ModularBuilding_Pooled");
            go.transform.SetParent(parent);
            go.transform.localPosition = Vector3.zero;

            var instance = go.AddComponent<ModularBuildingInstance>();
            instance.SetSharedMaterial(sharedMaterial);
            return instance;
        }

        /// <summary>
        /// 从池中获取一个建筑实例
        /// </summary>
        public ModularBuildingInstance Get()
        {
            ModularBuildingInstance building;
            if (pool.Count > 0)
            {
                building = pool.Pop();
            }
            else if (activeCount < maxCapacity)
            {
                building = CreateBuildingInstance();
            }
            else
            {
                Debug.LogWarning("[ModularBuildingPool] 已达到最大容量");
                return null;
            }
            building.gameObject.SetActive(true);
            activeCount++;
            return building;
        }

        /// <summary>
        /// 将建筑实例返回池中
        /// </summary>
        public void Return(ModularBuildingInstance building)
        {
            if (building == null) return;
            building.Reset();
            building.gameObject.SetActive(false);
            building.transform.SetParent(parent);
            pool.Push(building);
            activeCount--;
        }

        /// <summary>
        /// 清空池
        /// </summary>
        public void Clear()
        {
            while (pool.Count > 0)
            {
                var building = pool.Pop();
                if (building != null && building.gameObject != null)
                    Object.Destroy(building.gameObject);
            }
            activeCount = 0;
        }

        /// <summary>
        /// 预热池到指定数量
        /// </summary>
        public void Prewarm(int count)
        {
            int toCreate = Mathf.Min(count - pool.Count, maxCapacity - TotalCount);
            for (int i = 0; i < toCreate; i++)
            {
                var building = CreateBuildingInstance();
                building.gameObject.SetActive(false);
                pool.Push(building);
            }
        }

        /// <summary>
        /// 收缩池到指定数量
        /// </summary>
        public void Shrink(int targetPoolSize)
        {
            while (pool.Count > targetPoolSize)
            {
                var building = pool.Pop();
                if (building != null && building.gameObject != null)
                    Object.Destroy(building.gameObject);
            }
        }
    }
}
