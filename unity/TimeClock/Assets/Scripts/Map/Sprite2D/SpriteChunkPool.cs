using UnityEngine;
using System.Collections.Generic;

namespace PomodoroTimer.Map.Sprite2D
{
    /// <summary>
    /// Sprite分块对象池
    /// 管理SpriteChunk的复用
    /// </summary>
    public class SpriteChunkPool
    {
        private readonly Stack<SpriteChunk> pool;
        private readonly Transform parent;
        private readonly SpriteTilePool tilePool;
        private readonly int initialCapacity;

        private int activeCount;

        public int ActiveCount => activeCount;
        public int PooledCount => pool.Count;

        /// <summary>
        /// 构造函数
        /// </summary>
        public SpriteChunkPool(Transform parent, SpriteTilePool tilePool, int initialCapacity = 16)
        {
            this.parent = parent;
            this.tilePool = tilePool;
            this.initialCapacity = initialCapacity;
            this.pool = new Stack<SpriteChunk>(initialCapacity);
            this.activeCount = 0;
        }

        /// <summary>
        /// 获取分块
        /// </summary>
        public SpriteChunk Get(Vector2Int coord)
        {
            SpriteChunk chunk;

            if (pool.Count > 0)
            {
                chunk = pool.Pop();
                chunk.Reset();
            }
            else
            {
                chunk = new SpriteChunk(coord, parent);
            }

            activeCount++;
            return chunk;
        }

        /// <summary>
        /// 归还分块
        /// </summary>
        public void Return(SpriteChunk chunk)
        {
            if (chunk == null) return;

            // 归还所有地砖到地砖池
            var tiles = chunk.RemoveAllTiles();
            tilePool.ReturnAll(tiles);

            chunk.SetVisible(false);
            pool.Push(chunk);
            activeCount--;
        }

        /// <summary>
        /// 清空对象池
        /// </summary>
        public void Clear()
        {
            while (pool.Count > 0)
            {
                var chunk = pool.Pop();
                chunk.Destroy();
            }
            activeCount = 0;
        }
    }
}
