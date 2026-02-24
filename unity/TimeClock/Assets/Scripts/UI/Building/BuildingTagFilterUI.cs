using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace PomodoroTimer.UI.Building
{
    /// <summary>
    /// 建筑标签筛选UI控制器
    /// 管理下拉菜单和标签切换按钮的显示与交互
    /// </summary>
    public class BuildingTagFilterUI : MonoBehaviour
    {
        [Header("下拉菜单")]
        [SerializeField] private TMP_Dropdown tagCategoryDropdown;

        [Header("标签按钮容器")]
        [SerializeField] private Transform tagToggleContainer;

        [Header("标签按钮预制体")]
        [SerializeField] private GameObject tagTogglePrefab;

        [Header("颜色")]
        [SerializeField] private Color toggleNormalColor = new Color(0.3f, 0.3f, 0.3f);
        [SerializeField] private Color toggleSelectedColor = new Color(0.2f, 0.6f, 0.2f);

        [Header("字体")]
        [SerializeField] private TMP_FontAsset customFont;

        private BuildingTagFilter tagFilter;
        private List<GameObject> activeToggleObjects = new List<GameObject>();

        /// <summary>
        /// 由BuildingPanelUI调用以注入逻辑层
        /// </summary>
        public void Initialize(BuildingTagFilter filter)
        {
            tagFilter = filter;

            SetupDropdown();

            tagFilter.OnActiveCategoryChanged += OnActiveCategoryChanged;
            tagFilter.OnFilterChanged += OnFilterChanged;
        }

        private void OnDestroy()
        {
            if (tagFilter != null)
            {
                tagFilter.OnActiveCategoryChanged -= OnActiveCategoryChanged;
                tagFilter.OnFilterChanged -= OnFilterChanged;
            }
        }

        /// <summary>
        /// 初始化下拉菜单选项
        /// </summary>
        private void SetupDropdown()
        {
            if (tagCategoryDropdown == null) return;

            tagCategoryDropdown.ClearOptions();

            var options = new List<string> { "标签筛选..." };
            for (int i = 0; i < BuildingTagFilter.CategoryCount; i++)
                options.Add(BuildingTagFilter.TagCategoryNames[i]);
            options.Add("清除所有筛选");

            tagCategoryDropdown.AddOptions(options);
            tagCategoryDropdown.value = 0;

            // 应用自定义字体
            if (customFont != null)
            {
                if (tagCategoryDropdown.captionText != null)
                    tagCategoryDropdown.captionText.font = customFont;
                if (tagCategoryDropdown.itemText != null)
                    tagCategoryDropdown.itemText.font = customFont;
            }

            tagCategoryDropdown.onValueChanged.AddListener(OnDropdownValueChanged);
        }

        /// <summary>
        /// 下拉菜单选项变化
        /// </summary>
        private void OnDropdownValueChanged(int index)
        {
            // index 0 = "标签筛选..."（默认占位）
            // index 1~4 = 4个标签分类
            // index 5 = "清除所有筛选"

            if (index == 0)
            {
                // 选择了占位项，不做任何操作
                return;
            }

            int lastIndex = BuildingTagFilter.CategoryCount + 1; // "清除所有筛选"的index
            if (index == lastIndex)
            {
                tagFilter.ClearAllFilters();
                tagCategoryDropdown.SetValueWithoutNotify(0);
                return;
            }

            // 标签分类索引 = dropdown index - 1
            int categoryIndex = index - 1;
            tagFilter.SetActiveCategory(categoryIndex);
        }

        /// <summary>
        /// 激活分类变化回调
        /// </summary>
        private void OnActiveCategoryChanged(int categoryIndex)
        {
            ClearToggleButtons();

            if (categoryIndex < 0 || categoryIndex >= BuildingTagFilter.CategoryCount)
                return;

            var tags = tagFilter.GetAvailableTags(categoryIndex);
            if (tags == null || tags.Count == 0) return;

            CreateToggleButtons(categoryIndex, tags);
        }

        /// <summary>
        /// 筛选条件变化回调 - 刷新按钮颜色
        /// </summary>
        private void OnFilterChanged()
        {
            RefreshToggleColors();
        }

        /// <summary>
        /// 创建标签切换按钮
        /// </summary>
        private void CreateToggleButtons(int categoryIndex, List<string> tags)
        {
            if (tagToggleContainer == null || tagTogglePrefab == null) return;

            foreach (var tag in tags)
            {
                var obj = Instantiate(tagTogglePrefab, tagToggleContainer);
                obj.SetActive(true);

                var btn = obj.GetComponent<Button>();
                var img = obj.GetComponent<Image>();
                var text = obj.GetComponentInChildren<TextMeshProUGUI>();

                if (text != null)
                {
                    text.text = tag;
                    if (customFont != null)
                        text.font = customFont;
                }

                // 设置初始颜色
                if (img != null)
                {
                    img.color = tagFilter.IsTagSelected(categoryIndex, tag)
                        ? toggleSelectedColor
                        : toggleNormalColor;
                }

                // 绑定点击事件
                if (btn != null)
                {
                    string capturedTag = tag;
                    int capturedCategory = categoryIndex;
                    btn.onClick.AddListener(() =>
                    {
                        tagFilter.ToggleTag(capturedCategory, capturedTag);
                    });
                }

                activeToggleObjects.Add(obj);
            }

            // 强制刷新布局，确保 FlowLayoutGroup + ContentSizeFitter 立即生效
            RectTransform containerRect = tagToggleContainer as RectTransform;
            if (containerRect != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(containerRect);
            }
        }

        /// <summary>
        /// 清除所有标签按钮
        /// </summary>
        private void ClearToggleButtons()
        {
            foreach (var obj in activeToggleObjects)
            {
                if (obj != null)
                    Destroy(obj);
            }
            activeToggleObjects.Clear();
        }

        /// <summary>
        /// 刷新所有切换按钮的颜色
        /// </summary>
        private void RefreshToggleColors()
        {
            int categoryIndex = tagFilter.ActiveCategory;
            if (categoryIndex < 0) return;

            var tags = tagFilter.GetAvailableTags(categoryIndex);
            if (tags == null) return;

            for (int i = 0; i < activeToggleObjects.Count && i < tags.Count; i++)
            {
                var obj = activeToggleObjects[i];
                if (obj == null) continue;

                var img = obj.GetComponent<Image>();
                if (img != null)
                {
                    img.color = tagFilter.IsTagSelected(categoryIndex, tags[i])
                        ? toggleSelectedColor
                        : toggleNormalColor;
                }
            }
        }
    }
}
