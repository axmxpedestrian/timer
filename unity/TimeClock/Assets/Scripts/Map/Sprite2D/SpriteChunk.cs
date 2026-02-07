using UnityEngine;
using System.Collections.Generic;

namespace PomodoroTimer.Map.Sprite2D
{
    /// <summary>
    /// Sprite分块
    /// 管理一组地砖Sprite的显示和剔除
    /// </summary>
    public class SpriteChunk
    {
        private readonly GameObject gameObject;
        private readonly Transform transform;
        private readonly List<SpriteRenderer> tileRenderers;
        private readonly Vector2Int chunkCoord;

        private Bounds bounds;
        private bool isVisible = true;

        public Vector2Int ChunkCoord => chunkCoord;
        public Bounds Bounds => bounds;
        public bool IsVisible => isVisible;
        public Transform Transform => transform;
        public int TileCount => tileRenderers.Count;

        /// <summary>
        /// 构造函数
        /// </summary>
        public SpriteChunk(Vector2Int coord, Transform parent)
        {
            this.chunkCoord = coord;
            this.tileRenderers = new List<SpriteRenderer>();

            gameObject = new GameObject($"Chunk_{coord.x}_{coord.y}");
            transform = gameObject.transform;
            transform.SetParent(parent);
            transform.localPosition = Vector3.zero;
        }

        /// <summary>
        /// 添加地砖渲染器
        /// </summary>
        public void AddTile(SpriteRenderer renderer)
        {
            if (renderer == null) return;

            renderer.transform.SetParent(transform);
            tileRenderers.Add(renderer);
        }

        /// <summary>
        /// 移除所有地砖（返回给对象池）
        /// </summary>
        public List<SpriteRenderer> RemoveAllTiles()
        {
            var tiles = new List<SpriteRenderer>(tileRenderers);
            tileRenderers.Clear();
            return tiles;
        }

        /// <summary>
        /// 设置边界
        /// </summary>
        public void SetBounds(Bounds bounds)
        {
            this.bounds = bounds;
        }

        /// <summary>
        /// 设置可见性
        /// </summary>
        public void SetVisible(bool visible)
        {
            if (isVisible == visible) return;

            isVisible = visible;
            gameObject.SetActive(visible);
        }

        /// <summary>
        /// 重置分块
        /// </summary>
        public void Reset()
        {
            tileRenderers.Clear();
            bounds = default;
            isVisible = true;
            gameObject.SetActive(true);
        }

        /// <summary>
        /// 销毁分块
        /// </summary>
        public void Destroy()
        {
            tileRenderers.Clear();
            if (gameObject != null)
            {
                Object.Destroy(gameObject);
            }
        }
    }
}
