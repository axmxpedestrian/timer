using UnityEngine;
using System.Collections.Generic;
using PomodoroTimer.Map.Data;
using UnityEngine.Rendering.Universal;

namespace PomodoroTimer.Map.Sprite2D
{
    /// <summary>
    /// 建筑特效控制器
    /// 管理建筑的视觉特效（烟雾、灯光等），响应全局时间系统
    /// </summary>
    public class BuildingEffectController : MonoBehaviour
    {
        private ModularBuildingInstance buildingInstance;
        private List<EffectInstance> activeEffects = new List<EffectInstance>();

        /// <summary>
        /// 特效实例内部类
        /// </summary>
        private class EffectInstance
        {
            public BuildingEffectConfig config;
            public GameObject effectObject;
            public Light2D light2D;
            public ParticleSystem particleSystem;
            public SpriteRenderer spriteRenderer;
            public float targetAlpha;
            public float currentAlpha;
            public bool isActive;
        }

        private void Awake()
        {
            buildingInstance = GetComponent<ModularBuildingInstance>();
        }

        private void Start()
        {
            // 订阅全局时间系统事件
            if (GlobalTimeManager.Instance != null)
            {
                GlobalTimeManager.Instance.OnTimeChanged += OnTimeChanged;
                GlobalTimeManager.Instance.OnWeatherChanged += OnWeatherChanged;
            }
        }

        private void OnDestroy()
        {
            if (GlobalTimeManager.Instance != null)
            {
                GlobalTimeManager.Instance.OnTimeChanged -= OnTimeChanged;
                GlobalTimeManager.Instance.OnWeatherChanged -= OnWeatherChanged;
            }
            Clear();
        }

        private void Update()
        {
            UpdateEffectTransitions();
        }

        /// <summary>
        /// 初始化特效
        /// </summary>
        public void Initialize(BuildingBlueprint blueprint)
        {
            Clear();

            if (blueprint?.effects == null) return;

            foreach (var effectConfig in blueprint.effects)
            {
                CreateEffect(effectConfig);
            }

            // 立即更新状态
            UpdateEffectStates();
        }

        private void CreateEffect(BuildingEffectConfig config)
        {
            if (config.effectPrefab == null)
            {
                // 如果没有预制体，根据类型创建默认特效
                CreateDefaultEffect(config);
                return;
            }

            var effectObj = Instantiate(config.effectPrefab, transform);
            effectObj.transform.localPosition = config.localOffset;

            var instance = new EffectInstance
            {
                config = config,
                effectObject = effectObj,
                light2D = effectObj.GetComponent<Light2D>(),
                particleSystem = effectObj.GetComponent<ParticleSystem>(),
                spriteRenderer = effectObj.GetComponent<SpriteRenderer>(),
                targetAlpha = 1f,
                currentAlpha = 0f,
                isActive = false
            };

            activeEffects.Add(instance);
        }

        private void CreateDefaultEffect(BuildingEffectConfig config)
        {
            var effectObj = new GameObject($"Effect_{config.effectType}");
            effectObj.transform.SetParent(transform);
            effectObj.transform.localPosition = config.localOffset;

            var instance = new EffectInstance
            {
                config = config,
                effectObject = effectObj,
                targetAlpha = 1f,
                currentAlpha = 0f,
                isActive = false
            };

            switch (config.effectType)
            {
                case BuildingEffectType.Light:
                    CreateDefaultLight(instance);
                    break;
                case BuildingEffectType.Smoke:
                    CreateDefaultSmoke(instance);
                    break;
                case BuildingEffectType.Particle:
                    CreateDefaultParticle(instance);
                    break;
            }

            activeEffects.Add(instance);
        }

        private void CreateDefaultLight(EffectInstance instance)
        {
            // 创建简单的光晕Sprite作为2D灯光效果
            var sr = instance.effectObject.AddComponent<SpriteRenderer>();
            sr.sprite = CreateLightSprite();
            sr.color = new Color(1f, 0.9f, 0.7f, 0.5f);
            sr.sortingLayerName = IsometricSortingHelper.LAYER_EFFECTS;
            sr.sortingOrder = 100;
            instance.spriteRenderer = sr;

            // 如果有Light2D组件支持
            var light = instance.effectObject.AddComponent<Light2D>();
            if (light != null)
            {
                light.lightType = Light2D.LightType.Point;
                light.color = new Color(1f, 0.9f, 0.7f);
                light.intensity = 1f;
                light.pointLightOuterRadius = 2f;
                instance.light2D = light;
            }
        }

        private void CreateDefaultSmoke(EffectInstance instance)
        {
            var ps = instance.effectObject.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 3f;
            main.startSpeed = 0.5f;
            main.startSize = 0.3f;
            main.startColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 50;

            var emission = ps.emission;
            emission.rateOverTime = 5f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.1f;

            var velocityOverLifetime = ps.velocityOverLifetime;
            velocityOverLifetime.enabled = true;
            velocityOverLifetime.y = 0.5f;

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(Color.gray, 0f),
                    new GradientColorKey(Color.gray, 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(0.3f, 0f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            colorOverLifetime.color = gradient;

            var renderer = instance.effectObject.GetComponent<ParticleSystemRenderer>();
            renderer.sortingLayerName = IsometricSortingHelper.LAYER_EFFECTS;
            renderer.sortingOrder = 50;

            instance.particleSystem = ps;
        }

        private void CreateDefaultParticle(EffectInstance instance)
        {
            var ps = instance.effectObject.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 2f;
            main.startSpeed = 1f;
            main.startSize = 0.1f;
            main.startColor = Color.yellow;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 30;

            var emission = ps.emission;
            emission.rateOverTime = 10f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 15f;
            shape.radius = 0.1f;

            var renderer = instance.effectObject.GetComponent<ParticleSystemRenderer>();
            renderer.sortingLayerName = IsometricSortingHelper.LAYER_EFFECTS;
            renderer.sortingOrder = 60;

            instance.particleSystem = ps;
        }

        private Sprite CreateLightSprite()
        {
            int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;

            float center = size / 2f;
            float maxDist = center;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    float alpha = 1f - Mathf.Clamp01(dist / maxDist);
                    alpha = alpha * alpha; // 平方衰减
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f), 32f);
        }

        #region 时间/天气响应

        private void OnTimeChanged(float normalizedTime)
        {
            UpdateEffectStates();
        }

        private void OnWeatherChanged(WeatherType weather, float intensity)
        {
            UpdateEffectStates();
        }

        private void UpdateEffectStates()
        {
            float time = GlobalTimeManager.Instance?.NormalizedTime ?? 0.5f;
            WeatherType weather = GlobalTimeManager.Instance?.CurrentWeather ?? WeatherType.Clear;
            float weatherIntensity = GlobalTimeManager.Instance?.WeatherIntensity ?? 0f;

            foreach (var effect in activeEffects)
            {
                bool shouldBeActive = ShouldEffectBeActive(effect.config, time, weather, weatherIntensity);
                effect.targetAlpha = shouldBeActive ? 1f : 0f;

                if (shouldBeActive && !effect.isActive)
                {
                    ActivateEffect(effect);
                }
                else if (!shouldBeActive && effect.isActive && effect.currentAlpha <= 0.01f)
                {
                    DeactivateEffect(effect);
                }
            }
        }

        private bool ShouldEffectBeActive(BuildingEffectConfig config, float time,
            WeatherType weather, float weatherIntensity)
        {
            // 检查时间条件
            if (config.respondsToTime)
            {
                bool timeActive = IsTimeInRange(time, config.activeTimeRange);
                if (!timeActive) return false;
            }

            // 检查天气条件
            if (config.respondsToWeather)
            {
                // 这里可以扩展天气检查逻辑
            }

            return true;
        }

        private bool IsTimeInRange(float time, Vector2 range)
        {
            if (range.x > range.y)
            {
                // 跨午夜范围
                return time >= range.x || time <= range.y;
            }
            return time >= range.x && time <= range.y;
        }

        #endregion

        #region 特效激活/停用

        private void ActivateEffect(EffectInstance effect)
        {
            effect.isActive = true;
            effect.effectObject.SetActive(true);

            if (effect.particleSystem != null)
            {
                effect.particleSystem.Play();
            }
        }

        private void DeactivateEffect(EffectInstance effect)
        {
            effect.isActive = false;

            if (effect.particleSystem != null)
            {
                effect.particleSystem.Stop();
            }

            effect.effectObject.SetActive(false);
        }

        private void UpdateEffectTransitions()
        {
            float fadeSpeed = 2f;

            foreach (var effect in activeEffects)
            {
                if (Mathf.Abs(effect.currentAlpha - effect.targetAlpha) > 0.01f)
                {
                    effect.currentAlpha = Mathf.MoveTowards(effect.currentAlpha,
                        effect.targetAlpha, fadeSpeed * Time.deltaTime);

                    ApplyEffectAlpha(effect, effect.currentAlpha);
                }
            }
        }

        private void ApplyEffectAlpha(EffectInstance effect, float alpha)
        {
            if (effect.spriteRenderer != null)
            {
                var color = effect.spriteRenderer.color;
                color.a = alpha * 0.5f;
                effect.spriteRenderer.color = color;
            }

            if (effect.light2D != null)
            {
                effect.light2D.intensity = alpha;
            }

            if (effect.particleSystem != null)
            {
                var emission = effect.particleSystem.emission;
                emission.rateOverTimeMultiplier = alpha;
            }
        }

        #endregion

        /// <summary>
        /// 清除所有特效
        /// </summary>
        public void Clear()
        {
            foreach (var effect in activeEffects)
            {
                if (effect.effectObject != null)
                {
                    Destroy(effect.effectObject);
                }
            }
            activeEffects.Clear();
        }

        /// <summary>
        /// 手动触发特效
        /// </summary>
        public void TriggerEffect(BuildingEffectType type)
        {
            foreach (var effect in activeEffects)
            {
                if (effect.config.effectType == type)
                {
                    effect.targetAlpha = 1f;
                    ActivateEffect(effect);
                }
            }
        }

        /// <summary>
        /// 手动停止特效
        /// </summary>
        public void StopEffect(BuildingEffectType type)
        {
            foreach (var effect in activeEffects)
            {
                if (effect.config.effectType == type)
                {
                    effect.targetAlpha = 0f;
                }
            }
        }
    }

    /// <summary>
    /// Light2D占位类（如果项目没有使用URP）
    /// </summary>
#if !UNITY_2019_3_OR_NEWER
    public class Light2D : MonoBehaviour
    {
        public enum LightType { Point, Global, Sprite, Freeform }
        public LightType lightType;
        public Color color = Color.white;
        public float intensity = 1f;
        public float pointLightOuterRadius = 1f;
    }
#endif
}
