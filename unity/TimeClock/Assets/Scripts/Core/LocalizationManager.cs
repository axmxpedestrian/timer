using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace PomodoroTimer.Core
{
    /// <summary>
    /// 本地化管理器 - 管理语言切换与持久化
    /// </summary>
    public class LocalizationManager : MonoBehaviour
    {
        public static LocalizationManager Instance { get; private set; }

        /// <summary>
        /// 语言切换事件，供代码中动态文本刷新
        /// </summary>
        public event Action<Locale> OnLocaleChanged;

        public bool IsInitialized { get; private set; } = false;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// 异步初始化：等待 Unity Localization 就绪，然后应用存档中的语言设置
        /// </summary>
        public IEnumerator InitializeAsync()
        {
            // 等待 Localization Settings 初始化完成
            yield return LocalizationSettings.InitializationOperation;

            // 先订阅事件，再设置 locale，否则初始 locale 变更事件会被遗漏
            LocalizationSettings.SelectedLocaleChanged += OnUnityLocaleChanged;

            // 读取存档中保存的语言代码
            string savedCode = DataManager.Instance?.Settings?.languageCode;

            if (!string.IsNullOrEmpty(savedCode))
            {
                Locale target = FindLocale(savedCode);
                if (target != null)
                {
                    LocalizationSettings.SelectedLocale = target;
                }
            }

            IsInitialized = true;
            Debug.Log($"[LocalizationManager] 初始化完成, 当前语言: {LocalizationSettings.SelectedLocale?.Identifier.Code}");

            // 初始化完成后，主动广播一次当前 locale，
            // 确保那些在 locale 设置之后才订阅事件的组件也能刷新
            OnLocaleChanged?.Invoke(LocalizationSettings.SelectedLocale);
        }

        /// <summary>
        /// 切换语言并自动持久化
        /// </summary>
        public void ChangeLocale(string localeCode)
        {
            if (!IsInitialized)
            {
                Debug.LogWarning("[LocalizationManager] 尚未初始化，无法切换语言");
                return;
            }

            Locale target = FindLocale(localeCode);
            if (target == null)
            {
                Debug.LogWarning($"[LocalizationManager] 未找到 Locale: {localeCode}");
                return;
            }

            LocalizationSettings.SelectedLocale = target;

            // 持久化到存档
            if (DataManager.Instance != null)
            {
                DataManager.Instance.Settings.languageCode = localeCode;
                DataManager.Instance.Save();
            }
        }

        /// <summary>
        /// 获取当前语言代码
        /// </summary>
        public string GetCurrentLocaleCode()
        {
            if (LocalizationSettings.SelectedLocale != null)
            {
                return LocalizationSettings.SelectedLocale.Identifier.Code;
            }
            return "";
        }

        /// <summary>
        /// Unity Localization 的 locale 变更回调
        /// </summary>
        private void OnUnityLocaleChanged(Locale newLocale)
        {
            OnLocaleChanged?.Invoke(newLocale);
        }

        /// <summary>
        /// 根据语言代码查找 Locale 资源
        /// </summary>
        private Locale FindLocale(string localeCode)
        {
            var locales = LocalizationSettings.AvailableLocales.Locales;
            for (int i = 0; i < locales.Count; i++)
            {
                if (locales[i].Identifier.Code == localeCode)
                {
                    return locales[i];
                }
            }
            return null;
        }

        private void OnDestroy()
        {
            LocalizationSettings.SelectedLocaleChanged -= OnUnityLocaleChanged;

            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
