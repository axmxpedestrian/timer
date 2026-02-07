using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace PomodoroTimer.Resource
{
    /// <summary>
    /// 单个资源显示项UI
    /// </summary>
    public class ResourceItemUI : MonoBehaviour
    {
        [Header("UI组件")]
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI amountText;
        [SerializeField] private TextMeshProUGUI changeText;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("动画设置")]
        [SerializeField] private float changeDisplayDuration = 2f;
        [SerializeField] private Color positiveChangeColor = new Color(0.2f, 0.8f, 0.2f);
        [SerializeField] private Color negativeChangeColor = new Color(0.8f, 0.2f, 0.2f);

        private ResourceType resourceType;
        private long displayedAmount;
        private Coroutine changeCoroutine;

        public ResourceType ResourceType => resourceType;

        /// <summary>
        /// 初始化资源项
        /// </summary>
        public void Initialize(ResourceDefinition definition)
        {
            if (definition == null) return;

            resourceType = definition.resourceType;

            if (iconImage != null)
            {
                iconImage.sprite = definition.icon;
                iconImage.color = definition.iconColor;
            }

            // 隐藏变化文本
            if (changeText != null)
            {
                changeText.gameObject.SetActive(false);
            }

            UpdateAmount(ResourceManager.Instance?.GetAmount(resourceType) ?? 0);
        }

        /// <summary>
        /// 更新显示数量
        /// </summary>
        public void UpdateAmount(long newAmount)
        {
            long oldAmount = displayedAmount;
            displayedAmount = newAmount;

            if (amountText != null)
            {
                amountText.text = ResourceDefinition.FormatAmount(newAmount);
            }

            // 显示变化动画
            if (oldAmount != newAmount && oldAmount != 0)
            {
                ShowChange(newAmount - oldAmount);
            }
        }

        /// <summary>
        /// 显示资源变化
        /// </summary>
        private void ShowChange(long delta)
        {
            if (changeText == null) return;

            if (changeCoroutine != null)
            {
                StopCoroutine(changeCoroutine);
            }

            changeCoroutine = StartCoroutine(ShowChangeCoroutine(delta));
        }

        private IEnumerator ShowChangeCoroutine(long delta)
        {
            changeText.gameObject.SetActive(true);
            changeText.text = ResourceDefinition.FormatAmountChange(delta);
            changeText.color = delta > 0 ? positiveChangeColor : negativeChangeColor;

            // 淡入
            float fadeInTime = 0.2f;
            float elapsed = 0f;
            Color startColor = changeText.color;
            startColor.a = 0f;
            Color endColor = changeText.color;
            endColor.a = 1f;

            while (elapsed < fadeInTime)
            {
                elapsed += Time.deltaTime;
                changeText.color = Color.Lerp(startColor, endColor, elapsed / fadeInTime);
                yield return null;
            }

            // 保持显示
            yield return new WaitForSeconds(changeDisplayDuration);

            // 淡出
            float fadeOutTime = 0.3f;
            elapsed = 0f;
            startColor = changeText.color;
            endColor = startColor;
            endColor.a = 0f;

            while (elapsed < fadeOutTime)
            {
                elapsed += Time.deltaTime;
                changeText.color = Color.Lerp(startColor, endColor, elapsed / fadeOutTime);
                yield return null;
            }

            changeText.gameObject.SetActive(false);
            changeCoroutine = null;
        }

        /// <summary>
        /// 播放获得动画
        /// </summary>
        public void PlayGainAnimation()
        {
            StartCoroutine(GainAnimationCoroutine());
        }

        private IEnumerator GainAnimationCoroutine()
        {
            if (canvasGroup == null) yield break;

            // 缩放弹跳
            Vector3 originalScale = transform.localScale;
            float duration = 0.3f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float scale = 1f + Mathf.Sin(t * Mathf.PI) * 0.2f;
                transform.localScale = originalScale * scale;
                yield return null;
            }

            transform.localScale = originalScale;
        }

        /// <summary>
        /// 设置可见性
        /// </summary>
        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }
    }
}
