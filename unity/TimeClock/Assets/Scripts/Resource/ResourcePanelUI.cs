using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PomodoroTimer.Core;

namespace PomodoroTimer.Resource
{
    /// <summary>
    /// 初始资源条目 - 用于在 Inspector 中配置新存档的初始资源
    /// </summary>
    [Serializable]
    public class InitialResourceEntry
    {
        public ResourceType resourceType;
        public long amount;
    }

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

        [Header("面板外观")]
        [Tooltip("面板背景 Image 组件（可为空，自动查找）")]
        [SerializeField] private Image panelBackground;
        [Tooltip("面板背景颜色（含透明度）")]
        [SerializeField] private Color panelColor = new Color(0f, 0f, 0f, 0.5f);

        [Header("资源数量文字 (AmountText)")]
        [Tooltip("默认字体颜色")]
        [SerializeField] private Color amountTextColor = Color.white;
        [Tooltip("容量接近上限时的颜色（>= 80%）")]
        [SerializeField] private Color capacityWarningColor = new Color(1f, 0.85f, 0.2f);
        [Tooltip("容量已满时的颜色")]
        [SerializeField] private Color capacityFullColor = new Color(1f, 0.4f, 0.4f);

        [Header("资源变化文字 (ChangeText)")]
        [Tooltip("资源增加时的颜色")]
        [SerializeField] private Color positiveChangeColor = new Color(0.2f, 0.8f, 0.2f);
        [Tooltip("资源减少时的颜色")]
        [SerializeField] private Color negativeChangeColor = new Color(0.8f, 0.2f, 0.2f);

        [Header("资源定义")]
        [SerializeField] private ResourceDefinition[] resourceDefinitions;

        [Header("新存档初始资源")]
        [Tooltip("当检测到没有存档时，为玩家提供的初始资源")]
        [SerializeField] private InitialResourceEntry[] initialResources;

        // 资源项映射
        private Dictionary<ResourceType, ResourceItemUI> resourceItems = new Dictionary<ResourceType, ResourceItemUI>();
        private Dictionary<ResourceType, ResourceDefinition> definitionMap = new Dictionary<ResourceType, ResourceDefinition>();

        private void Awake()
        {
            InitializeDefinitionMap();
            ApplyPanelColor();
        }

        private void Start()
        {
            // 订阅资源管理器事件
            if (ResourceManager.Instance != null)
            {
                ResourceManager.Instance.OnResourceChanged += OnResourceChanged;
                ResourceManager.Instance.OnResourceUnlocked += OnResourceUnlocked;
                ResourceManager.Instance.OnResourcesLoaded += OnResourcesLoaded;
                ResourceManager.Instance.OnCapacityChanged += OnCapacityChanged;

                // 如果资源管理器已经初始化完成，直接刷新显示
                // （防止错过 OnResourcesLoaded 事件）
                if (ResourceManager.Instance.IsInitialized)
                {
                    RefreshAllItems();
                }
            }
        }

        private void OnDestroy()
        {
            if (ResourceManager.Instance != null)
            {
                ResourceManager.Instance.OnResourceChanged -= OnResourceChanged;
                ResourceManager.Instance.OnResourceUnlocked -= OnResourceUnlocked;
                ResourceManager.Instance.OnResourcesLoaded -= OnResourcesLoaded;
                ResourceManager.Instance.OnCapacityChanged -= OnCapacityChanged;
            }
        }

        /// <summary>
        /// 应用面板背景颜色
        /// </summary>
        private void ApplyPanelColor()
        {
            if (panelBackground == null)
                panelBackground = GetComponent<Image>();

            if (panelBackground != null)
                panelBackground.color = panelColor;
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
            ApplyInitialResourcesIfNewSave();
            RefreshAllItems();
        }

        /// <summary>
        /// 如果是新存档，应用初始资源
        /// </summary>
        private void ApplyInitialResourcesIfNewSave()
        {
            if (DataManager.Instance == null || !DataManager.Instance.IsNewSave) return;
            if (ResourceManager.Instance == null) return;
            if (initialResources == null || initialResources.Length == 0) return;

            foreach (var entry in initialResources)
            {
                if (entry.amount > 0)
                {
                    ResourceManager.Instance.AddResource(entry.resourceType, entry.amount, "InitialResource");
                }
            }

            Debug.Log($"[ResourcePanelUI] 新存档：已应用 {initialResources.Length} 项初始资源");
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
        /// 容量变化回调
        /// </summary>
        private void OnCapacityChanged(ResourceType type, long newCapacity)
        {
            if (resourceItems.TryGetValue(type, out var item))
            {
                bool hasCap = ResourceManager.Instance?.HasCapacity(type) ?? false;
                item.UpdateCapacity(newCapacity, hasCap);
            }
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
                // 将面板级样式传递给资源项
                item.ApplyStyle(amountTextColor, capacityWarningColor, capacityFullColor,
                    positiveChangeColor, negativeChangeColor);

                item.Initialize(definition);
                item.UpdateAmount(ResourceManager.Instance?.GetAmount(type) ?? 0);

                // 初始化容量显示
                var rm = ResourceManager.Instance;
                if (rm != null && rm.HasCapacity(type))
                {
                    item.UpdateCapacity(rm.GetCapacity(type), true);
                }

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

#if UNITY_EDITOR
        /// <summary>
        /// Editor 中修改颜色参数时实时预览
        /// </summary>
        private void OnValidate()
        {
            ApplyPanelColor();

            // 运行中同步更新已有资源项的样式
            if (Application.isPlaying && resourceItems != null)
            {
                foreach (var item in resourceItems.Values)
                {
                    if (item != null)
                    {
                        item.ApplyStyle(amountTextColor, capacityWarningColor, capacityFullColor,
                            positiveChangeColor, negativeChangeColor);
                    }
                }
            }
        }
#endif
    }
}
