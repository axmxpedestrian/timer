using UnityEngine;
using PomodoroTimer.Map.Data;

namespace PomodoroTimer.Map.Sprite2D
{
    /// <summary>
    /// 建筑状态控制器
    /// 管理建筑的状态机和状态转换效果
    /// </summary>
    public class BuildingStateController : MonoBehaviour
    {
        private ModularBuildingInstance buildingInstance;
        private ModularBuildingRenderer buildingRenderer;

        // 状态效果参数
        [Header("选中效果")]
        [SerializeField] private float selectedPulseSpeed = 2f;
        [SerializeField] private float selectedPulseMin = 1f;
        [SerializeField] private float selectedPulseMax = 1.3f;

        [Header("建造效果")]
        [SerializeField] private float constructionFadeSpeed = 0.5f;

        [Header("受损效果")]
        [SerializeField] private float damagedFlickerSpeed = 5f;
        [SerializeField] private float damagedBrightnessMin = 0.5f;
        [SerializeField] private float damagedBrightnessMax = 0.8f;

        // 状态
        private ModularBuildingState currentState = ModularBuildingState.Normal;
        private float stateTime;
        private bool isProcessingEffect;

        private void Awake()
        {
            buildingInstance = GetComponent<ModularBuildingInstance>();
            buildingRenderer = GetComponent<ModularBuildingRenderer>();
        }

        private void Update()
        {
            if (!isProcessingEffect) return;

            stateTime += Time.deltaTime;
            ProcessStateEffect();
        }

        /// <summary>
        /// 状态变化回调
        /// </summary>
        public void OnStateChanged(ModularBuildingState oldState, ModularBuildingState newState)
        {
            currentState = newState;
            stateTime = 0f;

            // 确定是否需要持续处理效果
            isProcessingEffect = newState == ModularBuildingState.Selected ||
                                 newState == ModularBuildingState.Constructing ||
                                 newState == ModularBuildingState.Damaged;

            // 立即应用状态效果
            ApplyImmediateStateEffect(newState);
        }

        private void ApplyImmediateStateEffect(ModularBuildingState state)
        {
            if (buildingRenderer == null) return;

            switch (state)
            {
                case ModularBuildingState.Normal:
                    buildingRenderer.SetBrightness(1f);
                    buildingRenderer.SetHighlight(false);
                    break;

                case ModularBuildingState.Selected:
                    // 选中效果在Update中处理脉冲
                    break;

                case ModularBuildingState.Constructing:
                    // 建造效果在Update中处理渐显
                    break;

                case ModularBuildingState.Upgrading:
                    buildingRenderer.SetBrightness(1.1f);
                    break;

                case ModularBuildingState.Damaged:
                    // 受损效果在Update中处理闪烁
                    break;

                case ModularBuildingState.Destroyed:
                    buildingRenderer.SetBrightness(0.4f);
                    break;
            }
        }

        private void ProcessStateEffect()
        {
            if (buildingRenderer == null) return;

            switch (currentState)
            {
                case ModularBuildingState.Selected:
                    ProcessSelectedEffect();
                    break;

                case ModularBuildingState.Constructing:
                    ProcessConstructionEffect();
                    break;

                case ModularBuildingState.Damaged:
                    ProcessDamagedEffect();
                    break;
            }
        }

        private void ProcessSelectedEffect()
        {
            // 脉冲发光效果
            float pulse = Mathf.Lerp(selectedPulseMin, selectedPulseMax,
                (Mathf.Sin(stateTime * selectedPulseSpeed * Mathf.PI * 2f) + 1f) * 0.5f);
            buildingRenderer.SetBrightness(pulse);
        }

        private void ProcessConstructionEffect()
        {
            if (buildingInstance == null) return;

            // 建造进度由外部控制，这里只处理视觉效果
            float progress = buildingInstance.ConstructionProgress;

            // 可以添加额外的视觉效果，如闪烁
            float flicker = 1f + Mathf.Sin(stateTime * 10f) * 0.05f * (1f - progress);
            buildingRenderer.SetBrightness(progress * flicker);
        }

        private void ProcessDamagedEffect()
        {
            // 受损闪烁效果
            float flicker = Mathf.Lerp(damagedBrightnessMin, damagedBrightnessMax,
                (Mathf.Sin(stateTime * damagedFlickerSpeed * Mathf.PI * 2f) + 1f) * 0.5f);
            buildingRenderer.SetBrightness(flicker);
        }

        /// <summary>
        /// 播放放置动画
        /// </summary>
        public void PlayPlaceAnimation()
        {
            StartCoroutine(PlaceAnimationCoroutine());
        }

        private System.Collections.IEnumerator PlaceAnimationCoroutine()
        {
            if (buildingRenderer == null) yield break;

            // 缩放弹跳效果
            Vector3 originalScale = transform.localScale;
            float duration = 0.3f;
            float elapsed = 0f;

            // 先缩小
            transform.localScale = originalScale * 0.8f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                // 弹性缓动
                float scale = 1f + Mathf.Sin(t * Mathf.PI) * 0.2f * (1f - t);
                transform.localScale = originalScale * scale;

                yield return null;
            }

            transform.localScale = originalScale;
        }

        /// <summary>
        /// 播放移除动画
        /// </summary>
        public void PlayRemoveAnimation(System.Action onComplete)
        {
            StartCoroutine(RemoveAnimationCoroutine(onComplete));
        }

        private System.Collections.IEnumerator RemoveAnimationCoroutine(System.Action onComplete)
        {
            if (buildingRenderer == null)
            {
                onComplete?.Invoke();
                yield break;
            }

            float duration = 0.2f;
            float elapsed = 0f;
            Vector3 originalScale = transform.localScale;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                // 缩小并淡出
                transform.localScale = originalScale * (1f - t);
                buildingRenderer.SetBrightness(1f - t);

                yield return null;
            }

            onComplete?.Invoke();
        }

        /// <summary>
        /// 播放受击动画
        /// </summary>
        public void PlayHitAnimation()
        {
            StartCoroutine(HitAnimationCoroutine());
        }

        private System.Collections.IEnumerator HitAnimationCoroutine()
        {
            if (buildingRenderer == null) yield break;

            // 快速闪红
            float duration = 0.15f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                // 闪烁效果
                float brightness = 1f + Mathf.Sin(t * Mathf.PI * 4f) * 0.5f;
                buildingRenderer.SetBrightness(brightness);

                yield return null;
            }

            // 恢复到当前状态的亮度
            ApplyImmediateStateEffect(currentState);
        }

        /// <summary>
        /// 播放升级完成动画
        /// </summary>
        public void PlayUpgradeCompleteAnimation()
        {
            StartCoroutine(UpgradeCompleteAnimationCoroutine());
        }

        private System.Collections.IEnumerator UpgradeCompleteAnimationCoroutine()
        {
            if (buildingRenderer == null) yield break;

            float duration = 0.5f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                // 发光脉冲
                float brightness = 1f + Mathf.Sin(t * Mathf.PI * 2f) * 0.5f * (1f - t);
                buildingRenderer.SetBrightness(brightness);

                yield return null;
            }

            buildingRenderer.SetBrightness(1f);
        }
    }
}
