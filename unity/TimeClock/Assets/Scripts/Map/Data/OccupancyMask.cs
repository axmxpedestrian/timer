using UnityEngine;
using System.Collections.Generic;

namespace PomodoroTimer.Map.Data
{
    /// <summary>
    /// 占位掩码 - 用于定义不规则建筑的占地形状
    /// 使用扁平化数组存储，左上角为原点
    /// </summary>
    [System.Serializable]
    public class OccupancyMask
    {
        [SerializeField] private int width = 1;
        [SerializeField] private int height = 1;
        [SerializeField] private bool[] cells;

        public int Width => width;
        public int Height => height;

        public OccupancyMask()
        {
            width = 1;
            height = 1;
            cells = new bool[] { true };
        }

        public OccupancyMask(int width, int height)
        {
            this.width = Mathf.Max(1, width);
            this.height = Mathf.Max(1, height);
            cells = new bool[this.width * this.height];
            // 默认全部占用
            for (int i = 0; i < cells.Length; i++)
                cells[i] = true;
        }

        public OccupancyMask(int width, int height, bool[] cells)
        {
            this.width = Mathf.Max(1, width);
            this.height = Mathf.Max(1, height);
            int expectedSize = this.width * this.height;
            this.cells = new bool[expectedSize];
            if (cells != null)
            {
                int copyLength = Mathf.Min(cells.Length, expectedSize);
                System.Array.Copy(cells, this.cells, copyLength);
            }
        }

        /// <summary>
        /// 检查指定位置是否被占用
        /// </summary>
        public bool IsOccupied(int x, int y)
        {
            if (x < 0 || x >= width || y < 0 || y >= height)
                return false;
            return cells[y * width + x];
        }

        /// <summary>
        /// 设置指定位置的占用状态
        /// </summary>
        public void SetOccupied(int x, int y, bool occupied)
        {
            if (x < 0 || x >= width || y < 0 || y >= height)
                return;
            cells[y * width + x] = occupied;
        }

        /// <summary>
        /// 获取所有被占用的位置（相对于原点）
        /// </summary>
        public List<Vector2Int> GetOccupiedPositions()
        {
            var positions = new List<Vector2Int>();
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (IsOccupied(x, y))
                        positions.Add(new Vector2Int(x, y));
                }
            }
            return positions;
        }

        /// <summary>
        /// 获取旋转后的掩码
        /// </summary>
        /// <param name="rotation">旋转角度：0, 90, 180, 270</param>
        public OccupancyMask GetRotated(int rotation)
        {
            rotation = ((rotation % 360) + 360) % 360;
            if (rotation == 0)
                return Clone();

            int newWidth, newHeight;
            GetRotatedSize(rotation, out newWidth, out newHeight);

            var rotated = new OccupancyMask(newWidth, newHeight);
            for (int i = 0; i < rotated.cells.Length; i++)
                rotated.cells[i] = false;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (!IsOccupied(x, y))
                        continue;

                    int newX, newY;
                    switch (rotation)
                    {
                        case 90:
                            // 90度顺时针: newX = height-1-y, newY = x
                            newX = height - 1 - y;
                            newY = x;
                            break;
                        case 180:
                            // 180度: newX = width-1-x, newY = height-1-y
                            newX = width - 1 - x;
                            newY = height - 1 - y;
                            break;
                        case 270:
                            // 270度顺时针 (90度逆时针): newX = y, newY = width-1-x
                            newX = y;
                            newY = width - 1 - x;
                            break;
                        default:
                            newX = x;
                            newY = y;
                            break;
                    }

                    rotated.SetOccupied(newX, newY, true);
                }
            }

            return rotated;
        }

        /// <summary>
        /// 获取旋转后的尺寸
        /// </summary>
        public Vector2Int GetRotatedSize(int rotation)
        {
            int newWidth, newHeight;
            GetRotatedSize(rotation, out newWidth, out newHeight);
            return new Vector2Int(newWidth, newHeight);
        }

        private void GetRotatedSize(int rotation, out int newWidth, out int newHeight)
        {
            rotation = ((rotation % 360) + 360) % 360;
            if (rotation == 90 || rotation == 270)
            {
                newWidth = height;
                newHeight = width;
            }
            else
            {
                newWidth = width;
                newHeight = height;
            }
        }

        /// <summary>
        /// 克隆掩码
        /// </summary>
        public OccupancyMask Clone()
        {
            var clone = new OccupancyMask(width, height);
            System.Array.Copy(cells, clone.cells, cells.Length);
            return clone;
        }

        /// <summary>
        /// 获取占用格子数量
        /// </summary>
        public int GetOccupiedCount()
        {
            int count = 0;
            foreach (var cell in cells)
                if (cell) count++;
            return count;
        }

        /// <summary>
        /// 检查是否为矩形（所有格子都被占用）
        /// </summary>
        public bool IsRectangular()
        {
            return GetOccupiedCount() == width * height;
        }

        /// <summary>
        /// 创建矩形掩码
        /// </summary>
        public static OccupancyMask CreateRectangle(int width, int height)
        {
            return new OccupancyMask(width, height);
        }

        /// <summary>
        /// 创建L形掩码
        /// </summary>
        public static OccupancyMask CreateLShape(int width, int height, int cornerWidth, int cornerHeight)
        {
            var mask = new OccupancyMask(width, height);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    bool occupied = (x < cornerWidth) || (y >= height - cornerHeight);
                    mask.SetOccupied(x, y, occupied);
                }
            }
            return mask;
        }

        /// <summary>
        /// 创建T形掩码
        /// </summary>
        public static OccupancyMask CreateTShape(int width, int height, int stemWidth)
        {
            var mask = new OccupancyMask(width, height);
            int stemStart = (width - stemWidth) / 2;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    bool occupied = (y == 0) || (x >= stemStart && x < stemStart + stemWidth);
                    mask.SetOccupied(x, y, occupied);
                }
            }
            return mask;
        }

#if UNITY_EDITOR
        /// <summary>
        /// 用于编辑器显示的字符串表示
        /// </summary>
        public string ToDebugString()
        {
            var sb = new System.Text.StringBuilder();
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    sb.Append(IsOccupied(x, y) ? "■" : "□");
                }
                if (y < height - 1)
                    sb.AppendLine();
            }
            return sb.ToString();
        }
#endif
    }
}
