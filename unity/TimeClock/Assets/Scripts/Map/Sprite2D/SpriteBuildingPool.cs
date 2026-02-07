using UnityEngine;
using System.Collections.Generic;

namespace PomodoroTimer.Map.Sprite2D
{
    public class SpriteBuildingPool
    {
        private readonly Stack<BuildingSpriteInstance> pool;
        private readonly Transform parent;
        private readonly Material sharedMaterial;
        private readonly int initialCapacity;
        private readonly int maxCapacity;
        private int activeCount;

        public int ActiveCount => activeCount;
        public int PooledCount => pool.Count;

        public SpriteBuildingPool(Transform parent, Material sharedMaterial, int initialCapacity = 64, int maxCapacity = 256)
        {
            this.parent = parent;
            this.sharedMaterial = sharedMaterial;
            this.initialCapacity = initialCapacity;
            this.maxCapacity = maxCapacity;
            this.pool = new Stack<BuildingSpriteInstance>(initialCapacity);
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

        private BuildingSpriteInstance CreateBuildingInstance()
        {
            var go = new GameObject("Building_Pooled");
            go.transform.SetParent(parent);
            go.transform.localPosition = Vector3.zero;

            var renderer = go.AddComponent<SpriteRenderer>();
            if (sharedMaterial != null)
                renderer.sharedMaterial = sharedMaterial;

            var instance = go.AddComponent<BuildingSpriteInstance>();
            return instance;
        }

        public BuildingSpriteInstance Get()
        {
            BuildingSpriteInstance building;
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
                Debug.LogWarning("[SpriteBuildingPool] 已达到最大容量");
                return null;
            }
            building.gameObject.SetActive(true);
            activeCount++;
            return building;
        }

        public void Return(BuildingSpriteInstance building)
        {
            if (building == null) return;
            building.Reset();
            building.gameObject.SetActive(false);
            building.transform.SetParent(parent);
            pool.Push(building);
            activeCount--;
        }

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
    }
}