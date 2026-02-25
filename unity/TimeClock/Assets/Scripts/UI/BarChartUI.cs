using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PomodoroTimer.Utils;
using PomodoroTimer.Core;
using static PomodoroTimer.Utils.LocalizedText;

namespace PomodoroTimer.UI
{
    /// <summary>
    /// 柱状图UI组件 - 支持堆叠柱状图和悬停提示
    /// </summary>
    public class BarChartUI : MonoBehaviour
    {
        [Header("容器")]
        [SerializeField] private RectTransform chartContainer;
        [SerializeField] private RectTransform barsContainer;
        [SerializeField] private RectTransform labelsContainer;
        [SerializeField] private RectTransform gridContainer;
        
        [Header("预制体")]
        [SerializeField] private GameObject barPrefab;
        [SerializeField] private GameObject labelPrefab;
        [SerializeField] private GameObject gridLinePrefab;
        
        [Header("设置")]
        [SerializeField] private float barSpacing = 10f;
        [SerializeField] private float maxBarWidth = 60f;
        [SerializeField] private float chartPadding = 40f;
        [SerializeField] private int gridLineCount = 5;
        [SerializeField] private bool showValueOnBar = true;
        [SerializeField] private bool animateBars = true;
        [SerializeField] private float animationDuration = 0.5f;
        
        [Header("Y轴")]
        [SerializeField] private TextMeshProUGUI yAxisMaxText;
        [SerializeField] private TextMeshProUGUI yAxisMidText;
        [SerializeField] private TextMeshProUGUI yAxisUnitText;
        
        [Header("悬停提示")]
        [SerializeField] private GameObject tooltipPanel;      // 提示面板（可选，不设置则自动创建）
        [SerializeField] private TextMeshProUGUI tooltipText;  // 提示文本（可选）
        [SerializeField] private Vector2 tooltipOffset = new Vector2(10, 10);
        
        [Header("默认尺寸")]
        [SerializeField] private float defaultContainerWidth = 400f;
        [SerializeField] private float defaultContainerHeight = 200f;
        
        [Header("调试")]
        [SerializeField] private bool enableDebugLog = false;
        
        private List<GameObject> bars = new List<GameObject>();
        private List<GameObject> labels = new List<GameObject>();
        private List<GameObject> gridLines = new List<GameObject>();
        private List<float> targetHeights = new List<float>();
        private bool isAnimating = false;
        private float animationProgress = 0f;
        
        // Tooltip相关
        private GameObject tooltipInstance;
        private TextMeshProUGUI tooltipTextInstance;
        private RectTransform tooltipRect;
        private Canvas parentCanvas;
        private bool isTooltipVisible = false;
        
        // 缓存的数据
        private List<string> pendingLabels;
        private List<float> pendingValues;
        private List<List<TaskBreakdownItem>> pendingStackedData; // 堆叠数据
        private string pendingUnit;
        private bool hasPendingData = false;
        private bool isStackedMode = false;
        private int renderAttempts = 0;
        private const int MAX_RENDER_ATTEMPTS = 5;
        
        private void Awake()
        {
            InitializeTooltip();
        }
        
        private void Log(string message)
        {
            if (enableDebugLog)
            {
                Debug.Log($"[BarChartUI] {message}");
            }
        }
        
        private void OnEnable()
        {
            // 每次启用时尝试渲染
            if (hasPendingData)
            {
                renderAttempts = 0;
                StartCoroutine(TryRenderWithRetry());
            }
        }
        
        /// <summary>
        /// 设置普通柱状图数据
        /// </summary>
        public void SetData(List<string> labelTexts, List<float> values, string unit = "")
        {
            pendingLabels = labelTexts;
            pendingValues = values;
            pendingStackedData = null;
            pendingUnit = unit;
            hasPendingData = true;
            isStackedMode = false;
            renderAttempts = 0;
            
            if (!gameObject.activeInHierarchy)
            {
                Log("对象未激活，数据已缓存");
                return;
            }
            
            StartCoroutine(TryRenderWithRetry());
        }
        
        /// <summary>
        /// 设置堆叠柱状图数据
        /// </summary>
        public void SetStackedData(List<string> labelTexts, List<List<TaskBreakdownItem>> stackedData, string unit = "")
        {
            pendingLabels = labelTexts;
            pendingStackedData = stackedData;
            pendingUnit = unit;
            hasPendingData = true;
            isStackedMode = true;
            renderAttempts = 0;
            
            // 计算每个柱子的总值
            pendingValues = new List<float>();
            foreach (var stack in stackedData)
            {
                float total = 0;
                if (stack != null)
                {
                    foreach (var item in stack)
                    {
                        total += item.totalSeconds / 60f; // 转换为分钟
                    }
                }
                pendingValues.Add(total);
            }
            
            if (!gameObject.activeInHierarchy)
            {
                Log("对象未激活，数据已缓存");
                return;
            }
            
            StartCoroutine(TryRenderWithRetry());
        }
        
        /// <summary>
        /// 多次尝试渲染，确保获取到正确的容器尺寸
        /// </summary>
        private System.Collections.IEnumerator TryRenderWithRetry()
        {
            while (renderAttempts < MAX_RENDER_ATTEMPTS && hasPendingData)
            {
                renderAttempts++;
                
                // 强制更新所有Canvas布局
                Canvas.ForceUpdateCanvases();
                
                // 强制刷新布局
                if (chartContainer != null)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(chartContainer);
                }
                if (barsContainer != null)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(barsContainer);
                }
                
                // 等待帧结束
                yield return new WaitForEndOfFrame();
                
                // 获取容器尺寸
                float containerHeight = GetContainerHeight();
                float containerWidth = GetContainerWidth();
                
                Log($"尝试 {renderAttempts}: 容器尺寸 = {containerWidth} x {containerHeight}");
                
                // 检查尺寸是否有效
                if (containerHeight > 10f && containerWidth > 10f)
                {
                    RenderChart(pendingLabels, pendingValues, pendingUnit);
                    hasPendingData = false;
                    yield break;
                }
                
                // 等待更多帧
                yield return null;
                yield return null;
            }
            
            // 最终使用默认尺寸渲染
            if (hasPendingData)
            {
                Log($"使用默认尺寸渲染: {defaultContainerWidth} x {defaultContainerHeight}");
                RenderChartWithSize(pendingLabels, pendingValues, pendingUnit, 
                                   defaultContainerWidth, defaultContainerHeight);
                hasPendingData = false;
            }
        }
        
        private float GetContainerWidth()
        {
            // 尝试多种方式获取宽度
            if (chartContainer != null)
            {
                float width = chartContainer.rect.width;
                if (width > 10f) return width;
            }
            
            if (barsContainer != null)
            {
                float width = barsContainer.rect.width;
                if (width > 10f) return width;
                
                // 尝试从LayoutElement获取
                var layout = barsContainer.GetComponent<LayoutElement>();
                if (layout != null && layout.preferredWidth > 0)
                    return layout.preferredWidth;
            }
            
            // 尝试从父对象获取
            var parent = transform.parent as RectTransform;
            if (parent != null && parent.rect.width > 10f)
            {
                return parent.rect.width - 40f; // 减去边距
            }
            
            return defaultContainerWidth;
        }
        
        private float GetContainerHeight()
        {
            // 尝试多种方式获取高度
            if (barsContainer != null)
            {
                float height = barsContainer.rect.height;
                if (height > 10f) return height;
                
                // 尝试从LayoutElement获取
                var layout = barsContainer.GetComponent<LayoutElement>();
                if (layout != null)
                {
                    if (layout.preferredHeight > 0) return layout.preferredHeight;
                    if (layout.minHeight > 0) return layout.minHeight;
                }
            }
            
            if (chartContainer != null)
            {
                float height = chartContainer.rect.height;
                if (height > 10f) return height * 0.7f; // 假设柱状图区域占70%
            }
            
            // 尝试从父对象获取
            var parent = transform.parent as RectTransform;
            if (parent != null && parent.rect.height > 10f)
            {
                return parent.rect.height * 0.5f; // 假设柱状图区域占一半
            }
            
            return defaultContainerHeight;
        }
        
        private void RenderChart(List<string> labelTexts, List<float> values, string unit)
        {
            float containerWidth = GetContainerWidth();
            float containerHeight = GetContainerHeight();
            RenderChartWithSize(labelTexts, values, unit, containerWidth, containerHeight);
        }
        
        private void RenderChartWithSize(List<string> labelTexts, List<float> values, string unit,
                                         float containerWidth, float containerHeight)
        {
            ClearChart();
            
            if (labelTexts == null || values == null || labelTexts.Count == 0)
            {
                Log("没有数据需要渲染");
                return;
            }
            
            if (labelTexts.Count != values.Count)
            {
                Debug.LogError("BarChartUI: 标签和数值数量不匹配");
                return;
            }
            
            Log($"开始渲染 {labelTexts.Count} 个柱子，容器: {containerWidth} x {containerHeight}, 堆叠模式: {isStackedMode}");
            
            // 计算最大值
            float maxValue = 0;
            foreach (var v in values)
            {
                if (v > maxValue) maxValue = v;
            }
            
            maxValue = Mathf.Max(maxValue, 1f);
            maxValue = RoundUpToNiceNumber(maxValue);
            
            UpdateYAxisLabels(maxValue, unit);
            CreateGridLines(containerWidth, containerHeight);
            
            // 计算柱子宽度
            float availableWidth = containerWidth - chartPadding * 2;
            float totalBarsWidth = availableWidth - barSpacing * (labelTexts.Count - 1);
            float barWidth = Mathf.Min(totalBarsWidth / labelTexts.Count, maxBarWidth);
            barWidth = Mathf.Max(barWidth, 20f); // 最小宽度
            
            // 计算起始X位置(居中)
            float totalWidth = barWidth * labelTexts.Count + barSpacing * (labelTexts.Count - 1);
            float startX = (availableWidth - totalWidth) / 2f + chartPadding;
            
            Log($"柱子宽度: {barWidth}, 起始X: {startX}");
            
            // 创建柱子和标签
            for (int i = 0; i < labelTexts.Count; i++)
            {
                float xPos = startX + i * (barWidth + barSpacing);
                float normalizedValue = values[i] / maxValue;
                float targetHeight = normalizedValue * containerHeight;
                targetHeight = Mathf.Max(targetHeight, values[i] > 0 ? 2f : 0f);
                
                if (isStackedMode && pendingStackedData != null && i < pendingStackedData.Count)
                {
                    // 堆叠柱状图模式 - 不在这里添加 targetHeights，由 CreateStackedBar 处理
                    CreateStackedBar(xPos, barWidth, containerHeight, maxValue, pendingStackedData[i], i);
                }
                else
                {
                    // 普通柱状图模式
                    CreateBar(xPos, barWidth, targetHeight, values[i], i);
                    targetHeights.Add(targetHeight);
                }
                
                CreateLabel(xPos, barWidth, labelTexts[i]);
                
                Log($"柱子 {i}: value={values[i]}, height={targetHeight}");
            }
            
            // 开始动画（仅普通模式）
            if (animateBars && bars.Count > 0 && !isStackedMode)
            {
                isAnimating = true;
                animationProgress = 0f;
            }
            
            Log($"渲染完成，创建了 {bars.Count} 个柱子, targetHeights={targetHeights.Count}");
        }
        
        /// <summary>
        /// 创建堆叠柱子 - 【修复】禁用动画，直接显示最终高度
        /// </summary>
        private void CreateStackedBar(float x, float width, float containerHeight, float maxValue, 
                                      List<TaskBreakdownItem> stackData, int index)
        {
            if (barPrefab == null || barsContainer == null) return;
            if (stackData == null || stackData.Count == 0)
            {
                // 没有数据，不创建任何柱子
                return;
            }
            
            float currentY = 0;
            float totalMinutes = stackData.Sum(s => s.totalSeconds / 60f);
            
            Log($"堆叠柱 {index}: 共 {stackData.Count} 个任务段, 总时长 {totalMinutes:F1} 分钟");
            
            // 为每个任务创建一个柱子段
            foreach (var item in stackData)
            {
                float segmentMinutes = item.totalSeconds / 60f;
                float normalizedHeight = (segmentMinutes / maxValue) * containerHeight;
                normalizedHeight = Mathf.Max(normalizedHeight, segmentMinutes > 0 ? 2f : 0f);
                
                var barObj = Instantiate(barPrefab, barsContainer);
                var rect = barObj.GetComponent<RectTransform>();
                
                if (rect != null)
                {
                    rect.anchorMin = new Vector2(0, 0);
                    rect.anchorMax = new Vector2(0, 0);
                    rect.pivot = new Vector2(0.5f, 0);
                    rect.anchoredPosition = new Vector2(x + width / 2f, currentY);
                    
                    // 【修复】堆叠模式直接使用最终高度，不使用动画
                    rect.sizeDelta = new Vector2(width, normalizedHeight);
                }
                
                var image = barObj.GetComponent<Image>();
                if (image != null)
                {
                    // 使用任务的颜色
                    image.color = ColorPalette.GetTaskColor(item.colorIndex);
                }
                
                // 隐藏堆叠段上的数值文本
                var valueText = barObj.GetComponentInChildren<TextMeshProUGUI>();
                if (valueText != null)
                {
                    valueText.text = "";
                }
                
                barObj.SetActive(true);
                bars.Add(barObj);
                // 【修复】堆叠模式不添加到 targetHeights，因为不使用动画
                
                currentY += normalizedHeight;
                
                Log($"  段 {item.taskName}: {segmentMinutes:F1}分钟, 高度={normalizedHeight:F1}, Y={currentY:F1}");
                
                // 添加悬停交互组件
                var segmentUI = barObj.AddComponent<BarSegmentUI>();
                segmentUI.Initialize(item.taskName, segmentMinutes, OnBarSegmentEnter, OnBarSegmentExit);
            }
        }
        
        /// <summary>
        /// 初始化Tooltip
        /// </summary>
        private void InitializeTooltip()
        {
            // 获取父Canvas
            parentCanvas = GetComponentInParent<Canvas>();
            
            // 如果已经设置了tooltipPanel，使用它
            if (tooltipPanel != null)
            {
                tooltipInstance = tooltipPanel;
                tooltipRect = tooltipInstance.GetComponent<RectTransform>();
                tooltipTextInstance = tooltipText != null ? tooltipText : tooltipInstance.GetComponentInChildren<TextMeshProUGUI>();
                tooltipInstance.SetActive(false);
                return;
            }
            
            // 否则动态创建Tooltip
            CreateTooltip();
        }
        
        /// <summary>
        /// 动态创建Tooltip
        /// </summary>
        private void CreateTooltip()
        {
            // 创建Tooltip容器
            tooltipInstance = new GameObject("Tooltip");
            tooltipRect = tooltipInstance.AddComponent<RectTransform>();
            
            // 设置父对象（放在Canvas下以确保显示在最上层）
            if (parentCanvas != null)
            {
                tooltipInstance.transform.SetParent(parentCanvas.transform, false);
                tooltipInstance.transform.SetAsLastSibling();
            }
            else
            {
                tooltipInstance.transform.SetParent(transform, false);
            }
            
            // 设置RectTransform
            tooltipRect.pivot = new Vector2(0, 1); // 左上角
            tooltipRect.anchorMin = new Vector2(0, 0);
            tooltipRect.anchorMax = new Vector2(0, 0);
            
            // 添加背景Image
            var bgImage = tooltipInstance.AddComponent<Image>();
            bgImage.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
            
            // 添加ContentSizeFitter
            var fitter = tooltipInstance.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            // 添加HorizontalLayoutGroup用于padding
            var layout = tooltipInstance.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 6, 6);
            layout.childAlignment = TextAnchor.MiddleCenter;
            
            // 创建文本
            var textObj = new GameObject("Text");
            textObj.transform.SetParent(tooltipInstance.transform, false);
            
            tooltipTextInstance = textObj.AddComponent<TextMeshProUGUI>();
            tooltipTextInstance.fontSize = 14;
            tooltipTextInstance.color = Color.white;
            tooltipTextInstance.alignment = TextAlignmentOptions.Center;
            tooltipTextInstance.text = "Tooltip";
            
            // 隐藏
            tooltipInstance.SetActive(false);
        }
        
        /// <summary>
        /// 柱子段鼠标进入回调
        /// </summary>
        private void OnBarSegmentEnter(BarSegmentUI segment)
        {
            if (segment == null) return;
            
            ShowTooltip(segment.TaskName, segment.Minutes);
        }
        
        /// <summary>
        /// 柱子段鼠标离开回调
        /// </summary>
        private void OnBarSegmentExit(BarSegmentUI segment)
        {
            HideTooltip();
        }
        
        /// <summary>
        /// 显示Tooltip
        /// </summary>
        private void ShowTooltip(string taskName, float minutes)
        {
            if (tooltipInstance == null || tooltipTextInstance == null) return;
            
            // 格式化时间显示
            string timeText;
            if (minutes >= 60)
            {
                int hours = (int)(minutes / 60);
                int mins = (int)(minutes % 60);
                timeText = mins > 0
                    ? GetSmart("UI_General", "time_hours_minutes", ("hours", hours), ("minutes", mins))
                    : GetSmart("UI_General", "time_hours", ("hours", hours));
            }
            else
            {
                timeText = GetSmart("UI_General", "time_minutes", ("minutes", $"{minutes:F0}"));
            }
            
            tooltipTextInstance.text = $"{taskName}\n{timeText}";
            tooltipInstance.SetActive(true);
            isTooltipVisible = true;
            
            // 强制更新布局
            LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipRect);
        }
        
        /// <summary>
        /// 隐藏Tooltip
        /// </summary>
        private void HideTooltip()
        {
            if (tooltipInstance == null) return;
            
            tooltipInstance.SetActive(false);
            isTooltipVisible = false;
        }
        
        /// <summary>
        /// 更新Tooltip位置（跟随鼠标）
        /// </summary>
        private void UpdateTooltipPosition()
        {
            if (!isTooltipVisible || tooltipRect == null || parentCanvas == null) return;
            
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentCanvas.transform as RectTransform,
                Input.mousePosition,
                parentCanvas.worldCamera,
                out localPoint
            );
            
            // 添加偏移
            localPoint += tooltipOffset;
            
            // 确保不超出屏幕边界
            var canvasRect = parentCanvas.transform as RectTransform;
            if (canvasRect != null)
            {
                float maxX = canvasRect.rect.width / 2 - tooltipRect.rect.width;
                float minY = -canvasRect.rect.height / 2 + tooltipRect.rect.height;
                
                localPoint.x = Mathf.Min(localPoint.x, maxX);
                localPoint.y = Mathf.Max(localPoint.y, minY);
            }
            
            tooltipRect.anchoredPosition = localPoint;
        }
        
        private void CreateBar(float x, float width, float height, float value, int index)
        {
            if (barPrefab == null || barsContainer == null) return;
            
            var barObj = Instantiate(barPrefab, barsContainer);
            var rect = barObj.GetComponent<RectTransform>();
            
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0, 0);
                rect.anchorMax = new Vector2(0, 0);
                rect.pivot = new Vector2(0.5f, 0);
                rect.anchoredPosition = new Vector2(x + width / 2f, 0);
                
                // 【关键】确保初始高度不为0
                float initialHeight = animateBars ? 2f : height;
                rect.sizeDelta = new Vector2(width, initialHeight);
            }
            
            var image = barObj.GetComponent<Image>();
            if (image != null)
            {
                image.color = ColorPalette.Theme.ChartBar;
            }
            
            if (showValueOnBar)
            {
                var valueText = barObj.GetComponentInChildren<TextMeshProUGUI>();
                if (valueText != null)
                {
                    valueText.text = value > 0 ? value.ToString("F0") : "";
                }
            }
            
            barObj.SetActive(true);
            bars.Add(barObj);
        }
        
        private void CreateLabel(float x, float width, string text)
        {
            if (labelPrefab == null || labelsContainer == null) return;
            
            var labelObj = Instantiate(labelPrefab, labelsContainer);
            var rect = labelObj.GetComponent<RectTransform>();
            
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0, 1);
                rect.anchorMax = new Vector2(0, 1);
                rect.pivot = new Vector2(0.5f, 1);
                rect.anchoredPosition = new Vector2(x + width / 2f, 0);
                rect.sizeDelta = new Vector2(width + barSpacing, 40);
            }
            
            var textComp = labelObj.GetComponent<TextMeshProUGUI>();
            if (textComp != null)
            {
                textComp.text = text;
                textComp.fontSize = 12;
                textComp.alignment = TextAlignmentOptions.Top;
                textComp.color = ColorPalette.Theme.TextSecondary;
            }
            
            labelObj.SetActive(true);
            labels.Add(labelObj);
        }
        
        private void CreateGridLines(float containerWidth, float containerHeight)
        {
            if (gridLinePrefab == null || gridContainer == null) return;
            
            for (int i = 0; i <= gridLineCount; i++)
            {
                var lineObj = Instantiate(gridLinePrefab, gridContainer);
                var rect = lineObj.GetComponent<RectTransform>();
                
                if (rect != null)
                {
                    float yPos = (i / (float)gridLineCount) * containerHeight;
                    
                    rect.anchorMin = new Vector2(0, 0);
                    rect.anchorMax = new Vector2(0, 0);
                    rect.pivot = new Vector2(0, 0.5f);
                    rect.anchoredPosition = new Vector2(chartPadding, yPos);
                    rect.sizeDelta = new Vector2(containerWidth - chartPadding * 2, 1);
                }
                
                var image = lineObj.GetComponent<Image>();
                if (image != null)
                {
                    image.color = ColorPalette.Theme.ChartGrid;
                }
                
                lineObj.SetActive(true);
                gridLines.Add(lineObj);
            }
        }
        
        private void UpdateYAxisLabels(float maxValue, string unit)
        {
            if (yAxisMaxText != null)
                yAxisMaxText.text = maxValue.ToString("F0");
            
            if (yAxisMidText != null)
                yAxisMidText.text = (maxValue / 2f).ToString("F0");
            
            if (yAxisUnitText != null)
                yAxisUnitText.text = unit;
        }
        
        private void ClearChart()
        {
            // 隐藏Tooltip
            HideTooltip();
            
            foreach (var bar in bars) if (bar != null) Destroy(bar);
            foreach (var label in labels) if (label != null) Destroy(label);
            foreach (var line in gridLines) if (line != null) Destroy(line);
            
            bars.Clear();
            labels.Clear();
            gridLines.Clear();
            targetHeights.Clear();
            
            isAnimating = false;
        }
        
        private float RoundUpToNiceNumber(float value)
        {
            if (value <= 0) return 10;
            
            float magnitude = Mathf.Pow(10, Mathf.Floor(Mathf.Log10(value)));
            float normalized = value / magnitude;
            
            float[] niceNumbers = { 1, 2, 5, 10 };
            
            foreach (var nice in niceNumbers)
            {
                if (normalized <= nice)
                {
                    return nice * magnitude;
                }
            }
            
            return 10 * magnitude;
        }
        
        private void Update()
        {
            // 更新Tooltip位置
            UpdateTooltipPosition();
            
            // 柱状图动画
            if (!isAnimating) return;
            
            animationProgress += Time.deltaTime / animationDuration;
            
            if (animationProgress >= 1f)
            {
                animationProgress = 1f;
                isAnimating = false;
            }
            
            float easedProgress = EaseOutQuad(animationProgress);
            
            for (int i = 0; i < bars.Count && i < targetHeights.Count; i++)
            {
                if (bars[i] == null) continue;
                
                var rect = bars[i].GetComponent<RectTransform>();
                if (rect != null)
                {
                    float currentHeight = Mathf.Max(targetHeights[i] * easedProgress, 2f);
                    rect.sizeDelta = new Vector2(rect.sizeDelta.x, currentHeight);
                }
            }
        }
        
        private void OnDisable()
        {
            // 隐藏Tooltip
            HideTooltip();
        }
        
        private float EaseOutQuad(float t)
        {
            return 1 - (1 - t) * (1 - t);
        }
        
        /// <summary>
        /// 强制使用指定尺寸重新渲染（可从外部调用）
        /// </summary>
        public void ForceRenderWithSize(float width, float height)
        {
            if (pendingLabels != null && pendingValues != null)
            {
                RenderChartWithSize(pendingLabels, pendingValues, pendingUnit, width, height);
            }
        }
    }
}
