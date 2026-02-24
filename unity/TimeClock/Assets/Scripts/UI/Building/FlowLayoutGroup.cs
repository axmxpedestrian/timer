using UnityEngine;
using UnityEngine.UI;

namespace PomodoroTimer.UI.Building
{
    /// <summary>
    /// 流式布局组 - 子元素从左到右排列，超出容器宽度时自动换行
    /// 配合 ScrollRect + ContentSizeFitter(Vertical=PreferredSize) 实现可滚动的标签云
    /// </summary>
    [AddComponentMenu("Layout/Flow Layout Group", 153)]
    public class FlowLayoutGroup : LayoutGroup
    {
        [SerializeField] private float spacingX = 8f;
        [SerializeField] private float spacingY = 8f;

        public float SpacingX { get => spacingX; set { SetProperty(ref spacingX, value); } }
        public float SpacingY { get => spacingY; set { SetProperty(ref spacingY, value); } }

        public override void CalculateLayoutInputHorizontal()
        {
            base.CalculateLayoutInputHorizontal();
            float minWidth = padding.left + padding.right;
            float preferredWidth = minWidth;
            for (int i = 0; i < rectChildren.Count; i++)
            {
                float childMin = LayoutUtility.GetMinWidth(rectChildren[i]);
                float childPreferred = LayoutUtility.GetPreferredWidth(rectChildren[i]);
                minWidth = Mathf.Max(minWidth, childMin + padding.left + padding.right);
                preferredWidth = Mathf.Max(preferredWidth, childPreferred + padding.left + padding.right);
            }
            SetLayoutInputForAxis(minWidth, preferredWidth, -1, 0);
        }

        public override void CalculateLayoutInputVertical()
        {
            float height = CalculateTotalHeight();
            SetLayoutInputForAxis(height, height, -1, 1);
        }

        public override void SetLayoutHorizontal()
        {
            // 水平定位在 SetLayoutVertical 中一并处理
        }

        public override void SetLayoutVertical()
        {
            LayoutChildren();
        }

        private float CalculateTotalHeight()
        {
            float containerWidth = rectTransform.rect.width;
            if (containerWidth <= 0) return padding.top + padding.bottom;

            float currentX = padding.left;
            float currentY = (float)padding.top;
            float rowHeight = 0f;

            for (int i = 0; i < rectChildren.Count; i++)
            {
                float childWidth = LayoutUtility.GetPreferredWidth(rectChildren[i]);
                float childHeight = LayoutUtility.GetPreferredHeight(rectChildren[i]);

                if (currentX + childWidth + padding.right > containerWidth && currentX > padding.left)
                {
                    currentX = padding.left;
                    currentY += rowHeight + spacingY;
                    rowHeight = 0f;
                }

                rowHeight = Mathf.Max(rowHeight, childHeight);
                currentX += childWidth + spacingX;
            }

            return currentY + rowHeight + padding.bottom;
        }

        private void LayoutChildren()
        {
            float containerWidth = rectTransform.rect.width;
            if (containerWidth <= 0) return;

            float currentX = padding.left;
            float currentY = (float)padding.top;
            float rowHeight = 0f;

            for (int i = 0; i < rectChildren.Count; i++)
            {
                var child = rectChildren[i];
                float childWidth = LayoutUtility.GetPreferredWidth(child);
                float childHeight = LayoutUtility.GetPreferredHeight(child);

                // 需要换行
                if (currentX + childWidth + padding.right > containerWidth && currentX > padding.left)
                {
                    currentX = padding.left;
                    currentY += rowHeight + spacingY;
                    rowHeight = 0f;
                }

                rowHeight = Mathf.Max(rowHeight, childHeight);

                SetChildAlongAxis(child, 0, currentX, childWidth);
                SetChildAlongAxis(child, 1, currentY, childHeight);

                currentX += childWidth + spacingX;
            }
        }
    }
}
