using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

namespace PomodoroTimer.Resource
{
    /// <summary>
    /// 资源产出动画控制器
    /// 在建筑上方显示资源产出的浮动文字动画
    /// </summary>
    public class ResourceProductionAnimator : MonoBehaviour
    {
        public static ResourceProductionAnimator Instance { get; private set; }

        [Header("设置")]
        [SerializeField] private Canvas worldCanvas;
        [SerializeField] private GameObject floatingTextPrefab;
        [SerializeField] private float floatHeight = 1f;
        [SerializeField] private float floatDuration = 1.5f;
        [SerializeField] private float fadeStartTime = 1f;

        [Header("颜色")]
        [SerializeField] private Color productionColor = new Color(0.3f, 0.9f, 0.3f);
        [SerializeField] private Color consumptionColor = new Color(0.9f, 0.6f, 0.3f);

        // 对象池
        private Queue<GameObject> textPool = new Queue<GameObject>();
        private int poolSize = 20;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            InitializePool();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void InitializePool()
        {
            if (floatingTextPrefab == null)
            {
                CreateDefaultPrefab();
            }

            for (int i = 0; i < poolSize; i++)
            {
                var obj = CreateFloatingText();
                obj.SetActive(false);
                textPool.Enqueue(obj);
            }
        }

        private void CreateDefaultPrefab()
        {
            // 创建默认的浮动文字预制体
            floatingTextPrefab = new GameObject("FloatingTextPrefab");
            floatingTextPrefab.transform.SetParent(transform);

            var canvas = floatingTextPrefab.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingLayerName = "Effects";
            canvas.sortingOrder = 1000;

            var scaler = floatingTextPrefab.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 100;

            var textObj = new GameObject("Text");
            textObj.transform.SetParent(floatingTextPrefab.transform);
            textObj.transform.localPosition = Vector3.zero;

            var text = textObj.AddComponent<TextMeshProUGUI>();
            text.fontSize = 24;
            text.alignment = TextAlignmentOptions.Center;
            text.fontStyle = FontStyles.Bold;

            var rectTransform = textObj.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(200, 50);

            floatingTextPrefab.SetActive(false);
        }

        private GameObject CreateFloatingText()
        {
            var obj = Instantiate(floatingTextPrefab, transform);
            return obj;
        }

        private GameObject GetFromPool()
        {
            if (textPool.Count > 0)
            {
                return textPool.Dequeue();
            }
            return CreateFloatingText();
        }

        private void ReturnToPool(GameObject obj)
        {
            obj.SetActive(false);
            textPool.Enqueue(obj);
        }

        /// <summary>
        /// 显示资源产出动画
        /// </summary>
        public void ShowProduction(Vector3 worldPosition, ResourceType resourceType, long amount)
        {
            var definition = ResourceManager.Instance?.GetDefinition(resourceType);
            string text = $"+{ResourceDefinition.FormatAmount(amount)}";

            if (definition?.icon != null)
            {
                // 如果有图标，可以在文字前添加图标名称
                text = $"+{ResourceDefinition.FormatAmount(amount)} {definition.resourceName}";
            }

            ShowFloatingText(worldPosition, text, productionColor);
        }

        /// <summary>
        /// 显示资源消耗动画
        /// </summary>
        public void ShowConsumption(Vector3 worldPosition, ResourceType resourceType, long amount)
        {
            var definition = ResourceManager.Instance?.GetDefinition(resourceType);
            string text = $"-{ResourceDefinition.FormatAmount(amount)}";

            if (definition?.icon != null)
            {
                text = $"-{ResourceDefinition.FormatAmount(amount)} {definition.resourceName}";
            }

            ShowFloatingText(worldPosition, text, consumptionColor);
        }

        /// <summary>
        /// 显示浮动文字
        /// </summary>
        public void ShowFloatingText(Vector3 worldPosition, string text, Color color)
        {
            var obj = GetFromPool();
            obj.transform.position = worldPosition;
            obj.SetActive(true);

            var textComponent = obj.GetComponentInChildren<TextMeshProUGUI>();
            if (textComponent != null)
            {
                textComponent.text = text;
                textComponent.color = color;
            }

            StartCoroutine(AnimateFloatingText(obj, textComponent, color));
        }

        private IEnumerator AnimateFloatingText(GameObject obj, TextMeshProUGUI textComponent, Color baseColor)
        {
            Vector3 startPos = obj.transform.position;
            Vector3 endPos = startPos + Vector3.up * floatHeight;
            float elapsed = 0f;

            while (elapsed < floatDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / floatDuration;

                // 位置动画（缓出）
                float posT = 1f - Mathf.Pow(1f - t, 2f);
                obj.transform.position = Vector3.Lerp(startPos, endPos, posT);

                // 透明度动画
                if (textComponent != null && elapsed > fadeStartTime)
                {
                    float fadeT = (elapsed - fadeStartTime) / (floatDuration - fadeStartTime);
                    Color c = baseColor;
                    c.a = 1f - fadeT;
                    textComponent.color = c;
                }

                yield return null;
            }

            ReturnToPool(obj);
        }

        /// <summary>
        /// 显示代币获得动画（特殊样式）
        /// </summary>
        public void ShowCoinGain(Vector3 worldPosition, int amount)
        {
            ShowFloatingText(worldPosition, $"+{amount} <sprite name=\"coin\">", new Color(1f, 0.85f, 0.2f));
        }
    }
}
