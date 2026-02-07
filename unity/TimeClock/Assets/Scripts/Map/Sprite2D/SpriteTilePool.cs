using UnityEngine;
using System.Collections.Generic;

namespace PomodoroTimer.Map.Sprite2D
{
    /// <summary>
    /// 地砖Sprite对象池
    /// 管理地砖SpriteRenderer的复用
    /// </summary>
    public class SpriteTilePool
    {
        private readonly Stack<SpriteRenderer> pool;
        private readonly Transform parent;
        private readonly Material sharedMaterial;
        private readonly int initialCapacity;
        private readonly int maxCapacity;

        private int activeCount;

        public int ActiveCount => activeCount;
        public int PooledCount => pool.Count;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="parent">父对象</param>
        /// <param name="sharedMaterial">共享材质（用于批处理）</param>
        /// <param name="initialCapacity">初始容量</param>
        /// <param name="maxCapacity">最大容量</param>
        public SpriteTilePool(Transform parent, Material sharedMaterial,
            int initialCapacity = 256, int maxCapacity = 2048)
        {
            this.parent = parent;
            this.sharedMaterial = sharedMaterial;
            this.initialCapacity = initialCapacity;
            this.maxCapacity = maxCapacity;
            this.pool = new Stack<SpriteRenderer>(initialCapacity);
            this.activeCount = 0;

            PrewarmPool();
        }

        /// <summary>
        /// 预热对象池
        /// </summary>
        private void PrewarmPool()
        {
            for (int i = 0; i < initialCapacity; i++)
            {
                var tile = CreateTileRenderer();
                tile.gameObject.SetActive(false);
                pool.Push(tile);
            }
        }

        /// <summary>
        /// 创建地砖渲染器
        /// </summary>
        private SpriteRenderer CreateTileRenderer()
        {
            var go = new GameObject("Tile");
            go.transform.SetParent(parent);
            go.transform.localPosition = Vector3.zero;

            var renderer = go.AddComponent<SpriteRenderer>();
            if (sharedMaterial != null)
            {
                renderer.sharedMaterial = sharedMaterial;
            }

            return renderer;
        }

        /// <summary>
        /// 获取地砖
        /// </summary>
        public SpriteRenderer Get()
        {
            SpriteRenderer tile;

            if (pool.Count > 0)
            {
                tile = pool.Pop();
            }
            else if (activeCount < maxCapacity)
            {
                tile = CreateTileRenderer();
            }
            else
            {
                Debug.LogWarning("[SpriteTilePool] 已达到最大容量");
                return null;
            }

            tile.gameObject.SetActive(true);
            activeCount++;
            return tile;
        }

        /// <summary>
        /// 归还地砖
        /// </summary>
        public void Return(SpriteRenderer tile)
        {
            if (tile == null) return;

            tile.gameObject.SetActive(false);
            tile.sprite = null;
            tile.transform.SetParent(parent);
            pool.Push(tile);
            activeCount--;
        }

        /// <summary>
        /// 批量归还
        /// </summary>
        public void ReturnAll(List<SpriteRenderer> tiles)
        {
            foreach (var tile in tiles)
            {
                Return(tile);
            }
            tiles.Clear();
        }

        /// <summary>
        /// 清空对象池
        /// </summary>
        public void Clear()
        {
            while (pool.Count > 0)
            {
                var tile = pool.Pop();
                if (tile != null && tile.gameObject != null)
                {
                    Object.Destroy(tile.gameObject);
                }
            }
            activeCount = 0;
        }
    }
}
