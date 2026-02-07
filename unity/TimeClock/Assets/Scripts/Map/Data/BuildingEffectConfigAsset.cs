using UnityEngine;

namespace PomodoroTimer.Map.Data
{
    /// <summary>
    /// 建筑特效详细配置 - 扩展配置
    /// </summary>
    [CreateAssetMenu(fileName = "BuildingEffectConfig", menuName = "Map/Building Effect Config")]
    public class BuildingEffectConfigAsset : ScriptableObject
    {
        [Header("基础设置")]
        [Tooltip("特效类型")]
        public BuildingEffectType effectType;

        [Tooltip("特效预制体")]
        public GameObject effectPrefab;

        [Tooltip("相对于建筑原点的偏移")]
        public Vector3 localOffset;

        [Header("时间响应")]
        [Tooltip("是否响应时间系统")]
        public bool respondsToTime = true;

        [Tooltip("激活时间范围（归一化，0-1，0=午夜）")]
        public Vector2 activeTimeRange = new Vector2(0.75f, 0.25f);

        [Tooltip("淡入淡出时间（秒）")]
        public float fadeTime = 1f;

        [Header("天气响应")]
        [Tooltip("是否响应天气")]
        public bool respondsToWeather = false;

        [Tooltip("在哪些天气下激活")]
        public WeatherType[] activeWeathers;

        [Tooltip("天气强度阈值")]
        public float weatherIntensityThreshold = 0.5f;

        [Header("灯光特效设置")]
        [Tooltip("灯光颜色")]
        public Color lightColor = new Color(1f, 0.9f, 0.7f);

        [Tooltip("灯光强度")]
        public float lightIntensity = 1f;

        [Tooltip("灯光范围")]
        public float lightRange = 2f;

        [Tooltip("是否闪烁")]
        public bool flicker = false;

        [Tooltip("闪烁频率")]
        public float flickerFrequency = 5f;

        [Tooltip("闪烁幅度")]
        public float flickerAmplitude = 0.2f;

        [Header("烟雾特效设置")]
        [Tooltip("烟雾颜色")]
        public Color smokeColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);

        [Tooltip("烟雾发射速率")]
        public float smokeEmissionRate = 10f;

        [Tooltip("烟雾生命周期")]
        public float smokeLifetime = 3f;

        [Tooltip("烟雾大小")]
        public float smokeSize = 0.5f;

        [Header("粒子特效设置")]
        [Tooltip("粒子颜色")]
        public Gradient particleColorOverLifetime;

        [Tooltip("粒子发射速率")]
        public float particleEmissionRate = 20f;

        [Tooltip("粒子生命周期")]
        public float particleLifetime = 2f;

        /// <summary>
        /// 检查当前时间是否在激活范围内
        /// </summary>
        public bool IsActiveAtTime(float normalizedTime)
        {
            if (!respondsToTime)
                return true;

            // 处理跨午夜的时间范围
            if (activeTimeRange.x > activeTimeRange.y)
            {
                // 例如 0.75 到 0.25 表示晚上7点到早上6点
                return normalizedTime >= activeTimeRange.x || normalizedTime <= activeTimeRange.y;
            }
            else
            {
                return normalizedTime >= activeTimeRange.x && normalizedTime <= activeTimeRange.y;
            }
        }

        /// <summary>
        /// 检查当前天气是否激活
        /// </summary>
        public bool IsActiveInWeather(WeatherType weather, float intensity)
        {
            if (!respondsToWeather)
                return true;

            if (intensity < weatherIntensityThreshold)
                return false;

            if (activeWeathers == null || activeWeathers.Length == 0)
                return true;

            foreach (var w in activeWeathers)
            {
                if (w == weather)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 转换为内联配置
        /// </summary>
        public BuildingEffectConfig ToInlineConfig()
        {
            return new BuildingEffectConfig
            {
                effectType = effectType,
                effectPrefab = effectPrefab,
                localOffset = localOffset,
                respondsToTime = respondsToTime,
                activeTimeRange = activeTimeRange,
                respondsToWeather = respondsToWeather
            };
        }
    }

    /// <summary>
    /// 天气类型
    /// </summary>
    public enum WeatherType
    {
        Clear,  // 晴朗
        Rain,   // 雨天
        Snow,   // 雪天
        Fog     // 雾天
    }
}
