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
            
            // 2. TaskManager
            CreateManager<TaskManager>(taskManagerPrefab, "TaskManager");
            
            // 3. StatisticsManager
            CreateManager<StatisticsManager>(statisticsManagerPrefab, "StatisticsManager");
            
            // 4. PomodoroTimer
            CreateManager<PomodoroTimerCore>(pomodoroTimerPrefab, "PomodoroTimer");
            
            // 5. AudioManager
            CreateManager<AudioManager>(audioManagerPrefab, "AudioManager");
            
            Debug.Log("所有管理器初始化完成");
        }
        
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
