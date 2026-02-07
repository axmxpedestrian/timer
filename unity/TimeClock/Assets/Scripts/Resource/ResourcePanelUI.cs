using System.Collections.Generic;
using UnityEngine;

namespace PomodoroTimer.Resource
{
    /// <summary>
    /// 资源面板UI
    /// 显示在画面左侧，管理所有资源项的显示
    /// </summary>
    public class ResourcePanelUI : MonoBehaviour
    {
        [Header("设置")]
        [SerializeField] private Transform resourceContainer;
        [SerializeField] private ResourceItemUI resourceItemPrefab;
        [SerializeField] private float itemSpacing = 5f;

        [Header("资源定义")]
        [SerializeField] private ResourceDefinition[] resourceDefinitions;

        // 资源项映射
        private Dictionary<ResourceType, ResourceItemUI> resourceItems = new Dictionary<ResourceType, ResourceItemUI>();
        private Dictionary<ResourceType, ResourceDefinition> definitionMap = new Dictionary<ResourceType, ResourceDefinition>();

        private void Awake()
        {
            InitializeDefinitionMap();
        }

        private void Start()
        {
            // 订阅资源管理器事件
            if (ResourceManager.Instance != null)
            {
                ResourceManager.Instance.OnResourceChanged += OnResourceChanged;
                ResourceManager.Instance.OnResourceUnlocked += OnResourceUnlocked;
                ResourceManager.Instance.OnResourcesLoaded += OnResourcesLoaded;
            }
        }

        private void OnDestroy()
        {
            if (ResourceManager.Instance != null)
            {
                ResourceManager.Instance.OnResourceChanged -= OnResourceChanged;
                ResourceManager.Instance.OnResourceUnlocked -= OnResourceUnlocked;
                ResourceManager.Instance.OnResourcesLoaded -= OnResourcesLoaded;
            }
        }

        private void InitializeDefinitionMap()
        {
            definitionMap.Clear();
            if (resourceDefinitions == null) return;

            foreach (var def in resourceDefinitions)
            {
                if (def != null)
                {
                    definitionMap[def.resourceType] = def;
                }
            }
        }

        /// <summary>
        /// 资源加载完成回调
        /// </summary>
        private void OnResourcesLoaded()
        {
            RefreshAllItems();
        }

        /// <summary>
        /// 资源变化回调
        /// </summary>
        private void OnResourceChanged(object sender, ResourceChangedEventArgs e)
        {
            UpdateResourceItem(e.ResourceType, e.NewAmount);

            // 如果是增加，播放动画
            if (e.Delta > 0 && resourceItems.TryGetValue(e.ResourceType, out var item))
            {
                item.PlayGainAnimation();
            }
        }

        /// <summary>
        /// 资源解锁回调
        /// </summary>
        private void OnResourceUnlocked(object sender, ResourceUnlockedEventArgs e)
        {
            CreateResourceItem(e.ResourceType);
            SortResourceItems();
        }

        /// <summary>
        /// 刷新所有资源项
        /// </summary>
        public void RefreshAllItems()
        {
            // 清除现有项
            ClearAllItems();

            if (ResourceManager.Instance == null) return;

            // 获取已解锁的资源
            var unlockedResources = ResourceManager.Instance.GetUnlockedResources();

            foreach (var type in unlockedResources)
            {
                CreateResourceItem(type);
            }

            SortResourceItems();
        }

        /// <summary>
        /// 创建资源项
        /// </summary>
        private void CreateResourceItem(ResourceType type)
        {
            if (resourceItems.ContainsKey(type)) return;
            if (resourceItemPrefab == null || resourceContainer == null) return;

            var definition = GetDefinition(type);
            if (definition == null) return;

            var itemObj = Instantiate(resourceItemPrefab, resourceContainer);
            var item = itemObj.GetComponent<ResourceItemUI>();

            if (item != null)
            {
                item.Initialize(definition);
                item.UpdateAmount(ResourceManager.Instance?.GetAmount(type) ?? 0);
                resourceItems[type] = item;
            }
        }

        /// <summary>
        /// 更新资源项
        /// </summary>
        private void UpdateResourceItem(ResourceType type, long amount)
        {
            if (resourceItems.TryGetValue(type, out var item))
            {
                item.UpdateAmount(amount);
            }
        }

        /// <summary>
        /// 排序资源项
        /// </summary>
        private void SortResourceItems()
        {
            if (resourceContainer == null) return;

            // 按显示顺序排序
            var sortedTypes = new List<ResourceType>(resourceItems.Keys);
            sortedTypes.Sort((a, b) =>
            {
                var defA = GetDefinition(a);
                var defB = GetDefinition(b);
                int orderA = defA?.displayOrder ?? (int)a;
                int orderB = defB?.displayOrder ?? (int)b;
                return orderA.CompareTo(orderB);
            });

            // 重新排列子对象
            for (int i = 0; i < sortedTypes.Count; i++)
            {
                if (resourceItems.TryGetValue(sortedTypes[i], out var item))
                {
                    item.transform.SetSiblingIndex(i);
                }
            }
        }

        /// <summary>
        /// 清除所有资源项
        /// </summary>
        private void ClearAllItems()
        {
            foreach (var item in resourceItems.Values)
            {
                if (item != null && item.gameObject != null)
                {
                    Destroy(item.gameObject);
                }
            }
            resourceItems.Clear();
        }

        /// <summary>
        /// 获取资源定义
        /// </summary>
        private ResourceDefinition GetDefinition(ResourceType type)
        {
            if (definitionMap.TryGetValue(type, out var def))
                return def;

            // 尝试从ResourceManager获取
            return ResourceManager.Instance?.GetDefinition(type);
        }

        /// <summary>
        /// 设置面板可见性
        /// </summary>
        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }
    }
}
