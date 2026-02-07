using UnityEngine;
using PomodoroTimer.Map.Data;

namespace PomodoroTimer.Map.Sprite2D
{
    /// <summary>
    /// 全局时间管理器
    /// 控制游戏内时间和天气系统，影响所有建筑的环境响应
    /// </summary>
    public class GlobalTimeManager : MonoBehaviour
    {
        public static GlobalTimeManager Instance { get; private set; }

        [Header("时间设置")]
        [SerializeField] private float dayLengthInMinutes = 24f;
        [SerializeField] private float startTime = 0.5f; // 0.5 = 中午12点
        [SerializeField] private bool autoAdvanceTime = true;
        [SerializeField] private float timeScale = 1f;

        [Header("日夜设置")]
        [SerializeField] private float dawnStart = 0.2f;   // 4:48 AM
        [SerializeField] private float dawnEnd = 0.3f;     // 7:12 AM
        [SerializeField] private float duskStart = 0.7f;   // 4:48 PM
        [SerializeField] private float duskEnd = 0.8f;     // 7:12 PM

        [Header("天气设置")]
        [SerializeField] private WeatherType initialWeather = WeatherType.Clear;
        [SerializeField] private float weatherChangeChance = 0.1f;
        [SerializeField] private float weatherCheckInterval = 60f;

        [Header("环境光照")]
        [SerializeField] private Gradient dayNightGradient;
        [SerializeField] private Color nightAmbientColor = new Color(0.2f, 0.2f, 0.4f);
        [SerializeField] private Color dayAmbientColor = new Color(1f, 1f, 0.95f);

        // 时间状态
        private float normalizedTime;
        private float lastWeatherCheck;

        // 天气状态
        private WeatherType currentWeather;
        private float weatherIntensity;
        private WeatherType targetWeather;
        private float targetIntensity;
        private float weatherTransitionProgress;

        // 属性
        public float NormalizedTime => normalizedTime;
        public float TimeInHours => normalizedTime * 24f;
        public bool IsNight => normalizedTime < dawnStart || normalizedTime > duskEnd;
        public bool IsDawn => normalizedTime >= dawnStart && normalizedTime <= dawnEnd;
        public bool IsDusk => normalizedTime >= duskStart && normalizedTime <= duskEnd;
        public bool IsDay => normalizedTime > dawnEnd && normalizedTime < duskStart;
        public WeatherType CurrentWeather => currentWeather;
        public float WeatherIntensity => weatherIntensity;
        public float TimeScale
        {
            get => timeScale;
            set => timeScale = Mathf.Max(0f, value);
        }

        // 事件
        public event System.Action<float> OnTimeChanged;
        public event System.Action<WeatherType, float> OnWeatherChanged;
        public event System.Action OnDayNightTransition;
        public event System.Action OnDawnStarted;
        public event System.Action OnDuskStarted;
        public event System.Action OnNightStarted;
        public event System.Action OnDayStarted;

        private bool wasNight;
        private bool wasDawn;
        private bool wasDusk;

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

            InitializeTime();
            InitializeWeather();
            InitializeGradient();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void Update()
        {
            if (autoAdvanceTime)
            {
                AdvanceTime(Time.deltaTime);
            }

            UpdateWeatherTransition();
            CheckWeatherChange();
        }

        private void InitializeTime()
        {
            normalizedTime = Mathf.Clamp01(startTime);
            wasNight = IsNight;
            wasDawn = IsDawn;
            wasDusk = IsDusk;
        }

        private void InitializeWeather()
        {
            currentWeather = initialWeather;
            targetWeather = initialWeather;
            weatherIntensity = initialWeather == WeatherType.Clear ? 0f : 0.5f;
            targetIntensity = weatherIntensity;
            weatherTransitionProgress = 1f;
        }

        private void InitializeGradient()
        {
            if (dayNightGradient == null)
            {
                dayNightGradient = new Gradient();
                var colorKeys = new GradientColorKey[]
                {
                    new GradientColorKey(nightAmbientColor, 0f),      // 午夜
                    new GradientColorKey(nightAmbientColor, 0.2f),    // 黎明前
                    new GradientColorKey(new Color(1f, 0.8f, 0.6f), 0.25f), // 日出
                    new GradientColorKey(dayAmbientColor, 0.35f),     // 上午
                    new GradientColorKey(dayAmbientColor, 0.65f),     // 下午
                    new GradientColorKey(new Color(1f, 0.6f, 0.4f), 0.75f), // 日落
                    new GradientColorKey(nightAmbientColor, 0.85f),   // 黄昏后
                    new GradientColorKey(nightAmbientColor, 1f)       // 午夜
                };
                var alphaKeys = new GradientAlphaKey[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f)
                };
                dayNightGradient.SetKeys(colorKeys, alphaKeys);
            }
        }

        #region 时间控制

        /// <summary>
        /// 设置时间（归一化值，0-1）
        /// </summary>
        public void SetTime(float normalizedTime)
        {
            float oldTime = this.normalizedTime;
            this.normalizedTime = Mathf.Repeat(normalizedTime, 1f);

            CheckDayNightTransitions();
            OnTimeChanged?.Invoke(this.normalizedTime);
            UpdateAmbientLight();
        }

        /// <summary>
        /// 设置时间（小时，0-24）
        /// </summary>
        public void SetTimeInHours(float hours)
        {
            SetTime(hours / 24f);
        }

        /// <summary>
        /// 推进时间
        /// </summary>
        public void AdvanceTime(float deltaSeconds)
        {
            if (dayLengthInMinutes <= 0) return;

            float dayLengthInSeconds = dayLengthInMinutes * 60f;
            float timeAdvance = (deltaSeconds * timeScale) / dayLengthInSeconds;

            SetTime(normalizedTime + timeAdvance);
        }

        /// <summary>
        /// 推进时间（游戏内小时）
        /// </summary>
        public void AdvanceTimeByHours(float hours)
        {
            SetTime(normalizedTime + hours / 24f);
        }

        private void CheckDayNightTransitions()
        {
            bool isNightNow = IsNight;
            bool isDawnNow = IsDawn;
            bool isDuskNow = IsDusk;

            if (isNightNow != wasNight)
            {
                OnDayNightTransition?.Invoke();
                if (isNightNow)
                    OnNightStarted?.Invoke();
                else
                    OnDayStarted?.Invoke();
            }

            if (isDawnNow && !wasDawn)
            {
                OnDawnStarted?.Invoke();
            }

            if (isDuskNow && !wasDusk)
            {
                OnDuskStarted?.Invoke();
            }

            wasNight = isNightNow;
            wasDawn = isDawnNow;
            wasDusk = isDuskNow;
        }

        #endregion

        #region 天气控制

        /// <summary>
        /// 设置天气
        /// </summary>
        public void SetWeather(WeatherType weather, float intensity = 0.5f, bool immediate = false)
        {
            targetWeather = weather;
            targetIntensity = weather == WeatherType.Clear ? 0f : Mathf.Clamp01(intensity);

            if (immediate)
            {
                currentWeather = targetWeather;
                weatherIntensity = targetIntensity;
                weatherTransitionProgress = 1f;
                OnWeatherChanged?.Invoke(currentWeather, weatherIntensity);
            }
            else
            {
                weatherTransitionProgress = 0f;
            }
        }

        /// <summary>
        /// 随机改变天气
        /// </summary>
        public void RandomizeWeather()
        {
            var weathers = System.Enum.GetValues(typeof(WeatherType));
            var newWeather = (WeatherType)weathers.GetValue(Random.Range(0, weathers.Length));
            float newIntensity = Random.Range(0.3f, 1f);
            SetWeather(newWeather, newIntensity);
        }

        private void UpdateWeatherTransition()
        {
            if (weatherTransitionProgress >= 1f) return;

            weatherTransitionProgress += Time.deltaTime * 0.2f; // 5秒过渡
            weatherTransitionProgress = Mathf.Clamp01(weatherTransitionProgress);

            if (weatherTransitionProgress >= 1f)
            {
                currentWeather = targetWeather;
                weatherIntensity = targetIntensity;
                OnWeatherChanged?.Invoke(currentWeather, weatherIntensity);
            }
            else
            {
                // 平滑过渡强度
                weatherIntensity = Mathf.Lerp(weatherIntensity, targetIntensity, weatherTransitionProgress);
            }
        }

        private void CheckWeatherChange()
        {
            if (Time.time - lastWeatherCheck < weatherCheckInterval) return;
            lastWeatherCheck = Time.time;

            if (Random.value < weatherChangeChance)
            {
                RandomizeWeather();
            }
        }

        #endregion

        #region 环境光照

        private void UpdateAmbientLight()
        {
            if (dayNightGradient == null) return;

            Color ambientColor = dayNightGradient.Evaluate(normalizedTime);

            // 应用天气影响
            if (currentWeather != WeatherType.Clear)
            {
                float weatherDarken = weatherIntensity * 0.3f;
                ambientColor = Color.Lerp(ambientColor, ambientColor * 0.7f, weatherDarken);

                if (currentWeather == WeatherType.Fog)
                {
                    ambientColor = Color.Lerp(ambientColor, Color.gray, weatherIntensity * 0.5f);
                }
            }

            // 设置Unity环境光（如果使用）
            RenderSettings.ambientLight = ambientColor;
        }

        /// <summary>
        /// 获取当前环境颜色
        /// </summary>
        public Color GetAmbientColor()
        {
            return dayNightGradient?.Evaluate(normalizedTime) ?? dayAmbientColor;
        }

        /// <summary>
        /// 获取当前光照强度（0-1）
        /// </summary>
        public float GetLightIntensity()
        {
            if (IsNight)
                return 0.3f;
            if (IsDawn || IsDusk)
                return 0.6f;
            return 1f;
        }

        #endregion

        #region 工具方法

        /// <summary>
        /// 获取格式化的时间字符串
        /// </summary>
        public string GetTimeString()
        {
            float hours = normalizedTime * 24f;
            int h = Mathf.FloorToInt(hours);
            int m = Mathf.FloorToInt((hours - h) * 60f);
            return $"{h:D2}:{m:D2}";
        }

        /// <summary>
        /// 获取时间段描述
        /// </summary>
        public string GetTimePeriodName()
        {
            if (IsNight) return "夜晚";
            if (IsDawn) return "黎明";
            if (IsDusk) return "黄昏";
            if (normalizedTime < 0.5f) return "上午";
            return "下午";
        }

        /// <summary>
        /// 获取天气描述
        /// </summary>
        public string GetWeatherName()
        {
            return currentWeather switch
            {
                WeatherType.Clear => "晴朗",
                WeatherType.Rain => "雨天",
                WeatherType.Snow => "雪天",
                WeatherType.Fog => "雾天",
                _ => "未知"
            };
        }

        /// <summary>
        /// 检查指定时间是否在范围内
        /// </summary>
        public bool IsTimeInRange(float startNormalized, float endNormalized)
        {
            if (startNormalized > endNormalized)
            {
                // 跨午夜
                return normalizedTime >= startNormalized || normalizedTime <= endNormalized;
            }
            return normalizedTime >= startNormalized && normalizedTime <= endNormalized;
        }

        #endregion

        #region 调试

#if UNITY_EDITOR
        [Header("调试")]
        [SerializeField] private bool showDebugInfo = false;

        private void OnGUI()
        {
            if (!showDebugInfo) return;

            GUILayout.BeginArea(new Rect(10, 10, 200, 150));
            GUILayout.Box($"时间: {GetTimeString()} ({GetTimePeriodName()})");
            GUILayout.Box($"天气: {GetWeatherName()} ({weatherIntensity:F2})");
            GUILayout.Box($"光照: {GetLightIntensity():F2}");

            if (GUILayout.Button("推进1小时"))
                AdvanceTimeByHours(1f);
            if (GUILayout.Button("随机天气"))
                RandomizeWeather();

            GUILayout.EndArea();
        }
#endif

        #endregion
    }
}
