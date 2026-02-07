using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace PomodoroTimer.UI.Building
{
    /// <summary>
    /// 虚拟滚动容器
    /// 只渲染可视区域内的元素，滚动时复用UI元素
    /// </summary>
    public class VirtualScrollView : MonoBehaviour
    {
        [Header("设置")]
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private RectTransform content;
        [SerializeField] private GameObject itemPrefab;
        [SerializeField] private float itemWidth = 120f;
        [SerializeField] private float itemSpacing = 10f;
        [SerializeField] private int bufferCount = 2;  // 缓冲区额外渲染数量

        [Header("调试")]
        [SerializeField] private bool enableDebugLog = false;

        private List<BuildingItemData> dataList = new List<BuildingItemData>();
        private List<BuildingItemUI> itemPool = new List<BuildingItemUI>();
        private Dictionary<int, BuildingItemUI> activeItems = new Dictionary<int, BuildingItemUI>();

        private float viewportWidth;
        private int visibleCount;
        private int firstVisibleIndex;
        private int lastVisibleIndex;

        private System.Action<BuildingItemData, int> onItemClick;
        private int selectedIndex = -1;

        public int SelectedIndex => selectedIndex;
        public int DataCount => dataList.Count;

        private void Awake()
        {
            if (scrollRect != null)
            {
                scrollRect.onValueChanged.AddListener(OnScrollChanged);
            }
        }

        private void Start()
        {
            CalculateViewport();
        }

        private void OnDestroy()
        {
            if (scrollRect != null)
            {
                scrollRect.onValueChanged.RemoveListener(OnScrollChanged);
            }
        }

        /// <summary>
        /// 计算视口参数
        /// </summary>
        private void CalculateViewport()
        {
            if (scrollRect == null) return;

            var viewport = scrollRect.viewport ?? scrollRect.GetComponent<RectTransform>();
            viewportWidth = viewport.rect.width;
            visibleCount = Mathf.CeilToInt(viewportWidth / (itemWidth + itemSpacing)) + bufferCount * 2;

            Log($"视口宽度: {viewportWidth}, 可见数量: {visibleCount}");
        }

        /// <summary>
        /// 设置数据源
        /// </summary>
        public void SetData(List<BuildingItemData> data, System.Action<BuildingItemData, int> onClick)
        {
            dataList = data ?? new List<BuildingItemData>();
            onItemClick = onClick;
            selectedIndex = -1;

            // 更新Content尺寸
            UpdateContentSize();

            // 确保有足够的池对象
            EnsurePoolSize();

            // 重置滚动位置
            if (scrollRect != null)
            {
                scrollRect.horizontalNormalizedPosition = 0f;
            }

            // 刷新显示
            RefreshVisibleItems();
        }

        /// <summary>
        /// 更新Content尺寸
        /// </summary>
        private void UpdateContentSize()
        {
            if (content == null) return;

            float totalWidth = dataList.Count * (itemWidth + itemSpacing) - itemSpacing;
            totalWidth = Mathf.Max(totalWidth, 0);

            content.sizeDelta = new Vector2(totalWidth, content.sizeDelta.y);
        }

        /// <summary>
        /// 确保对象池有足够的对象
        /// </summary>
        private void EnsurePoolSize()
        {
            int needed = visibleCount + bufferCount * 2;

            while (itemPool.Count < needed)
            {
                CreatePoolItem();
            }
        }

        /// <summary>
        /// 创建池对象
        /// </summary>
        private BuildingItemUI CreatePoolItem()
        {
            if (itemPrefab == null || content == null) return null;

            var go = Instantiate(itemPrefab, content);
            var item = go.GetComponent<BuildingItemUI>();

            if (item == null)
            {
                item = go.AddComponent<BuildingItemUI>();
            }

            go.SetActive(false);
            itemPool.Add(item);

            return item;
        }

        /// <summary>
        /// 滚动事件处理
        /// </summary>
        private void OnScrollChanged(Vector2 position)
        {
            RefreshVisibleItems();
        }

        /// <summary>
        /// 刷新可见项
        /// </summary>
        public void RefreshVisibleItems()
        {
            if (content == null || dataList.Count == 0)
            {
                HideAllItems();
                return;
            }

            CalculateViewport();

            // 计算当前可见范围
            float scrollX = -content.anchoredPosition.x;
            int newFirstVisible = Mathf.Max(0, Mathf.FloorToInt(scrollX / (itemWidth + itemSpacing)) - bufferCount);
            int newLastVisible = Mathf.Min(dataList.Count - 1,
                newFirstVisible + visibleCount + bufferCount * 2);

            // 回收不再可见的项
            var toRemove = new List<int>();
            foreach (var kvp in activeItems)
            {
                if (kvp.Key < newFirstVisible || kvp.Key > newLastVisible)
                {
                    kvp.Value.gameObject.SetActive(false);
                    toRemove.Add(kvp.Key);
                }
            }
            foreach (var idx in toRemove)
            {
                activeItems.Remove(idx);
            }

            // 显示新的可见项
            for (int i = newFirstVisible; i <= newLastVisible; i++)
            {
                if (!activeItems.ContainsKey(i))
                {
                    var item = GetAvailableItem();
                    if (item != null)
                    {
                        BindItem(item, i);
                        activeItems[i] = item;
                    }
                }
                else
                {
                    // 更新选中状态
                    activeItems[i].SetSelected(i == selectedIndex);
                }
            }

            firstVisibleIndex = newFirstVisible;
            lastVisibleIndex = newLastVisible;

            Log($"可见范围: {firstVisibleIndex} - {lastVisibleIndex}");
        }

        /// <summary>
        /// 获取可用的池对象
        /// </summary>
        private BuildingItemUI GetAvailableItem()
        {
            foreach (var item in itemPool)
            {
                if (!item.gameObject.activeSelf)
                {
                    return item;
                }
            }

            // 池不够，创建新的
            return CreatePoolItem();
        }

        /// <summary>
        /// 绑定数据到项
        /// </summary>
        private void BindItem(BuildingItemUI item, int index)
        {
            if (index < 0 || index >= dataList.Count) return;

            var data = dataList[index];

            // 设置位置
            var rt = item.GetComponent<RectTransform>();
            if (rt != null)
            {
                float x = index * (itemWidth + itemSpacing);
                rt.anchoredPosition = new Vector2(x, 0);
                rt.sizeDelta = new Vector2(itemWidth, rt.sizeDelta.y);
            }

            // 绑定数据
            item.Bind(data, index, OnItemClicked);
            item.SetSelected(index == selectedIndex);
            item.gameObject.SetActive(true);
        }

        /// <summary>
        /// 项点击回调
        /// </summary>
        private void OnItemClicked(BuildingItemData data, int index)
        {
            // 更新选中状态
            int oldSelected = selectedIndex;
            selectedIndex = index;

            // 更新旧选中项
            if (oldSelected >= 0 && activeItems.TryGetValue(oldSelected, out var oldItem))
            {
                oldItem.SetSelected(false);
            }

            // 更新新选中项
            if (activeItems.TryGetValue(index, out var newItem))
            {
                newItem.SetSelected(true);
            }

            // 触发回调
            onItemClick?.Invoke(data, index);
        }

        /// <summary>
        /// 隐藏所有项
        /// </summary>
        private void HideAllItems()
        {
            foreach (var item in itemPool)
            {
                item.gameObject.SetActive(false);
            }
            activeItems.Clear();
        }

        /// <summary>
        /// 刷新所有可见项的可负担状态
        /// </summary>
        public void RefreshAffordableState()
        {
            // 刷新数据
            foreach (var data in dataList)
            {
                data.RefreshAffordable();
            }

            // 刷新可见项显示
            foreach (var kvp in activeItems)
            {
                if (kvp.Key < dataList.Count)
                {
                    kvp.Value.Bind(dataList[kvp.Key], kvp.Key, onItemClick);
                    kvp.Value.SetSelected(kvp.Key == selectedIndex);
                }
            }
        }

        /// <summary>
        /// 清除选中
        /// </summary>
        public void ClearSelection()
        {
            if (selectedIndex >= 0 && activeItems.TryGetValue(selectedIndex, out var item))
            {
                item.SetSelected(false);
            }
            selectedIndex = -1;
        }

        /// <summary>
        /// 滚动到指定索引
        /// </summary>
        public void ScrollToIndex(int index)
        {
            if (scrollRect == null || content == null || dataList.Count == 0) return;

            float targetX = index * (itemWidth + itemSpacing);
            float maxScroll = content.sizeDelta.x - viewportWidth;

            if (maxScroll > 0)
            {
                float normalizedPos = Mathf.Clamp01(targetX / maxScroll);
                scrollRect.horizontalNormalizedPosition = normalizedPos;
            }
        }

        private void Log(string message)
        {
            if (enableDebugLog)
            {
                Debug.Log($"[VirtualScrollView] {message}");
            }
        }
    }
}
