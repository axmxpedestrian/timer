using System;
using System.Collections.Generic;
using PomodoroTimer.Map.Data;

namespace PomodoroTimer.UI.Building
{
    /// <summary>
    /// 建筑标签筛选逻辑（纯C#类，不依赖MonoBehaviour）
    /// 支持4个标签分类，分类内OR逻辑，分类间AND逻辑
    /// </summary>
    public class BuildingTagFilter
    {
        /// <summary>
        /// 标签分类名称
        /// </summary>
        public static readonly string[] TagCategoryNames = new string[]
        {
            "建筑类型",
            "风格",
            "资源消耗类型",
            "资源生成类型"
        };

        public const int CategoryCount = 4;

        /// <summary>
        /// 每个分类中已选中的标签
        /// </summary>
        private readonly HashSet<string>[] selectedTags;

        /// <summary>
        /// 每个分类中可用的标签（从蓝图中收集）
        /// </summary>
        private readonly List<string>[] availableTags;

        /// <summary>
        /// 当前激活的标签分类索引（-1表示无）
        /// </summary>
        private int activeCategory = -1;

        /// <summary>
        /// 筛选条件变化时触发
        /// </summary>
        public event Action OnFilterChanged;

        /// <summary>
        /// 当前激活分类变化时触发
        /// </summary>
        public event Action<int> OnActiveCategoryChanged;

        public BuildingTagFilter()
        {
            selectedTags = new HashSet<string>[CategoryCount];
            availableTags = new List<string>[CategoryCount];
            for (int i = 0; i < CategoryCount; i++)
            {
                selectedTags[i] = new HashSet<string>();
                availableTags[i] = new List<string>();
            }
        }

        /// <summary>
        /// 从蓝图集合中收集所有可用标签
        /// </summary>
        public void CollectTagsFromBlueprints(IEnumerable<BuildingBlueprint> blueprints)
        {
            // 用HashSet去重后转为排序列表
            var tagSets = new HashSet<string>[CategoryCount];
            for (int i = 0; i < CategoryCount; i++)
                tagSets[i] = new HashSet<string>();

            foreach (var bp in blueprints)
            {
                for (int i = 0; i < CategoryCount; i++)
                {
                    var tags = bp.GetTagsByCategory(i);
                    if (tags != null)
                    {
                        foreach (var tag in tags)
                        {
                            if (!string.IsNullOrEmpty(tag))
                                tagSets[i].Add(tag);
                        }
                    }
                }
            }

            for (int i = 0; i < CategoryCount; i++)
            {
                availableTags[i].Clear();
                availableTags[i].AddRange(tagSets[i]);
                availableTags[i].Sort();
            }
        }

        /// <summary>
        /// 获取指定分类的可用标签列表
        /// </summary>
        public List<string> GetAvailableTags(int categoryIndex)
        {
            if (categoryIndex < 0 || categoryIndex >= CategoryCount)
                return null;
            return availableTags[categoryIndex];
        }

        /// <summary>
        /// 获取当前激活的分类索引
        /// </summary>
        public int ActiveCategory => activeCategory;

        /// <summary>
        /// 设置当前激活的标签分类
        /// </summary>
        public void SetActiveCategory(int categoryIndex)
        {
            if (categoryIndex < 0 || categoryIndex >= CategoryCount)
                categoryIndex = -1;

            if (activeCategory == categoryIndex) return;

            activeCategory = categoryIndex;
            OnActiveCategoryChanged?.Invoke(activeCategory);
        }

        /// <summary>
        /// 切换指定分类中某标签的选中状态
        /// </summary>
        public void ToggleTag(int categoryIndex, string tag)
        {
            if (categoryIndex < 0 || categoryIndex >= CategoryCount) return;
            if (string.IsNullOrEmpty(tag)) return;

            if (selectedTags[categoryIndex].Contains(tag))
                selectedTags[categoryIndex].Remove(tag);
            else
                selectedTags[categoryIndex].Add(tag);

            OnFilterChanged?.Invoke();
        }

        /// <summary>
        /// 检查指定分类中某标签是否被选中
        /// </summary>
        public bool IsTagSelected(int categoryIndex, string tag)
        {
            if (categoryIndex < 0 || categoryIndex >= CategoryCount) return false;
            return selectedTags[categoryIndex].Contains(tag);
        }

        /// <summary>
        /// 清除所有筛选条件
        /// </summary>
        public void ClearAllFilters()
        {
            bool hadFilters = HasActiveFilters();

            for (int i = 0; i < CategoryCount; i++)
                selectedTags[i].Clear();

            activeCategory = -1;
            OnActiveCategoryChanged?.Invoke(-1);

            if (hadFilters)
                OnFilterChanged?.Invoke();
        }

        /// <summary>
        /// 是否有任何激活的筛选条件
        /// </summary>
        public bool HasActiveFilters()
        {
            for (int i = 0; i < CategoryCount; i++)
            {
                if (selectedTags[i].Count > 0) return true;
            }
            return false;
        }

        /// <summary>
        /// 对建筑列表应用标签筛选
        /// 分类内OR逻辑（匹配任一标签即通过），分类间AND逻辑（所有有选中标签的分类都须通过）
        /// </summary>
        public List<BuildingItemData> ApplyFilter(List<BuildingItemData> items)
        {
            if (!HasActiveFilters()) return items;

            var result = new List<BuildingItemData>();
            foreach (var item in items)
            {
                if (MatchesFilter(item.blueprint))
                    result.Add(item);
            }
            return result;
        }

        /// <summary>
        /// 检查单个蓝图是否匹配当前筛选条件
        /// </summary>
        private bool MatchesFilter(BuildingBlueprint blueprint)
        {
            for (int i = 0; i < CategoryCount; i++)
            {
                if (selectedTags[i].Count == 0) continue; // 该分类无筛选，跳过

                if (!blueprint.HasAnyTag(i, selectedTags[i]))
                    return false; // AND逻辑：任一分类不通过则排除
            }
            return true;
        }
    }
}
