using UnityEngine;
using PomodoroTimer.Core;
using PomodoroTimer.Utils;

// 解决命名空间冲突：为计时器类创建别名
using PomodoroTimerCore = PomodoroTimer.Core.PomodoroTimer;

namespace PomodoroTimer
{
    /// <summary>
    /// 游戏初始化管理器 - 确保正确的启动顺序
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        
        [Header("管理器预制体")]
        [SerializeField] private GameObject dataManagerPrefab;
        [SerializeField] private GameObject taskManagerPrefab;
        [SerializeField] private GameObject statisticsManagerPrefab;
        [SerializeField] private GameObject pomodoroTimerPrefab;
        [SerializeField] private GameObject audioManagerPrefab;
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeManagers();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        /// <summary>
        /// 初始化所有管理器
        /// </summary>
        private void InitializeManagers()
        {
            // 创建顺序很重要
            // 1. DataManager 必须第一个创建(加载存档)
            CreateManager<DataManager>(dataManagerPrefab, "DataManager");

            // 2. LocalizationManager（依赖 DataManager 读取语言偏好）
            CreateManager<LocalizationManager>(null, "LocalizationManager");
            StartCoroutine(LocalizationManager.Instance.InitializeAsync());

            // 3. TaskManager
            CreateManager<TaskManager>(taskManagerPrefab, "TaskManager");

            // 4. StatisticsManager
            CreateManager<StatisticsManager>(statisticsManagerPrefab, "StatisticsManager");

            // 5. PomodoroTimer
            CreateManager<PomodoroTimerCore>(pomodoroTimerPrefab, "PomodoroTimer");

            // 6. AudioManager
            CreateManager<AudioManager>(audioManagerPrefab, "AudioManager");

            // 7. 应用保存的显示设置
            ApplyDisplaySettings();

            Debug.Log("所有管理器初始化完成");
        }

        /// <summary>
        /// 应用保存的显示设置
        /// </summary>
        private void ApplyDisplaySettings()
        {
            if (DataManager.Instance == null) return;

            var settings = DataManager.Instance.Settings;

            // 应用全屏设置
            if (settings.fullScreen)
            {
                Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
                Screen.fullScreen = true;
            }
            else
            {
                Screen.fullScreenMode = FullScreenMode.Windowed;
                Screen.fullScreen = false;
            }

            // 应用窗口置顶设置
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            if (settings.topMost)
            {
                SetWindowTopMost(true);
            }
#endif

            Debug.Log($"[GameManager] 应用显示设置 - 全屏: {settings.fullScreen}, 置顶: {settings.topMost}");
        }

#if UNITY_STANDALONE_WIN
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern System.IntPtr GetActiveWindow();

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool SetWindowPos(System.IntPtr hWnd, System.IntPtr hWndInsertAfter,
            int X, int Y, int cx, int cy, uint uFlags);

        private static readonly System.IntPtr HWND_TOPMOST = new System.IntPtr(-1);
        private static readonly System.IntPtr HWND_NOTOPMOST = new System.IntPtr(-2);
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;

        private void SetWindowTopMost(bool topMost)
        {
            var handle = GetActiveWindow();
            SetWindowPos(handle, topMost ? HWND_TOPMOST : HWND_NOTOPMOST,
                0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);
        }
#endif
        
        /// <summary>
        /// 创建管理器
        /// </summary>
        private void CreateManager<T>(GameObject prefab, string name) where T : MonoBehaviour
        {
            // 检查是否已存在
            if (FindObjectOfType<T>() != null) return;
            
            GameObject obj;
            if (prefab != null)
            {
                obj = Instantiate(prefab);
            }
            else
            {
                obj = new GameObject(name);
                obj.AddComponent<T>();
            }
            
            obj.name = name;
            DontDestroyOnLoad(obj);
        }
        
        /// <summary>
        /// 应用退出时保存
        /// </summary>
        private void OnApplicationQuit()
        {
            DataManager.Instance?.Save();
        }
    }
}
