using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using PomodoroTimer.Data;
using PomodoroTimer.Resource;
using PomodoroTimer.Map.Data;
using PomodoroTimer.Map.Sprite2D;

namespace PomodoroTimer.Core
{
    /// <summary>
    /// 数据持久化管理器 - 优化版
    /// 减少日志输出，控制日志文件大小
    /// </summary>
    public class DataManager : MonoBehaviour
    {
        public static DataManager Instance { get; private set; }

        private const string SAVE_FILE_NAME = "pomodoro_save.json";

        // 【优化】日志控制 - 可在Inspector中调整
        [Header("日志设置")]
        [SerializeField] private bool enableVerboseLog = false;  // 详细日志开关

        private SaveData saveData;

        public SettingsData Settings => saveData.settings;
        public SessionData CurrentSession => saveData.currentSession;
        public StatisticsData Statistics => saveData.statistics;
        public ResourceSaveData ResourceData => saveData.resourceData;

        /// <summary>
        /// 获取建筑系统存档数据（供 ModularBuildingManager 主动加载）
        /// </summary>
        public BuildingSystemSaveData GetBuildingSystemSaveData() => saveData?.buildingSystem;

        /// <summary>
        /// 本次启动是否为全新存档（存档文件不存在时为 true）
        /// </summary>
        public bool IsNewSave { get; private set; }

        public event Action OnDataLoaded;
        public event Action OnDataSaved;

        private string SaveFilePath => Path.Combine(Application.persistentDataPath, SAVE_FILE_NAME);
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                Load();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void OnApplicationQuit()
        {
            Save();
        }
        
        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                Save();
            }
        }
        
        /// <summary>
        /// 条件日志输出
        /// </summary>
        private void Log(string message)
        {
            if (enableVerboseLog)
            {
                Debug.Log($"[DataManager] {message}");
            }
        }
        
        /// <summary>
        /// 加载存档
        /// </summary>
        public void Load()
        {
            try
            {
                if (File.Exists(SaveFilePath))
                {
                    IsNewSave = false;
                    string json = File.ReadAllText(SaveFilePath);
                    saveData = JsonUtility.FromJson<SaveData>(json);

                    if (saveData == null)
                    {
                        saveData = new SaveData();
                        IsNewSave = true;
                        Log("存档解析失败，创建新存档");
                    }
                    else
                    {
                        // 确保列表不为null
                        if (saveData.tasks == null)
                            saveData.tasks = new List<TaskData>();
                        if (saveData.pomodoroRecords == null)
                            saveData.pomodoroRecords = new List<PomodoroRecord>();
                        if (saveData.settings == null)
                            saveData.settings = new SettingsData();
                        if (saveData.currentSession == null)
                            saveData.currentSession = new SessionData();
                        if (saveData.statistics == null)
                            saveData.statistics = new StatisticsData();
                        if (saveData.resourceData == null)
                            saveData.resourceData = new ResourceSaveData();
                        if (saveData.buildingProducers == null)
                            saveData.buildingProducers = new List<BuildingProducerSaveData>();
                        if (saveData.buildingSystem == null)
                            saveData.buildingSystem = new BuildingSystemSaveData();

                        // 过滤掉无效的任务
                        saveData.tasks.RemoveAll(t => t == null || !t.IsValid());
                        
                        Log($"存档加载成功，任务数: {saveData.tasks.Count}");
                    }
                }
                else
                {
                    IsNewSave = true;
                    saveData = new SaveData();
                    Log("创建新存档");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"加载存档失败: {e.Message}");
                saveData = new SaveData();
            }
            
            StartCoroutine(InitializeManagersDelayed());
        }
        
        private System.Collections.IEnumerator InitializeManagersDelayed()
        {
            yield return null;

            if (TaskManager.Instance != null)
            {
                TaskManager.Instance.Initialize(saveData.tasks);
            }

            if (StatisticsManager.Instance != null)
            {
                StatisticsManager.Instance.Initialize(saveData.pomodoroRecords, saveData.statistics);
            }

            // 初始化资源管理器
            if (ResourceManager.Instance != null)
            {
                ResourceManager.Instance.Initialize(saveData.resourceData);

                // 旧存档迁移：如果 ResourceSaveData 中没有 Coin 记录，
                // 从 StatisticsData.totalCoins 迁移（仅执行一次，下次保存后 ResourceSaveData 会包含 Coin）
                if (ResourceManager.Instance.GetAmount(ResourceType.Coin) == 0
                    && saveData.statistics.totalCoins > 0)
                {
                    ResourceManager.Instance.AddResource(ResourceType.Coin,
                        saveData.statistics.totalCoins, "Migration");
                    Debug.Log($"[DataManager] 旧存档迁移: 从 StatisticsData 迁移 {saveData.statistics.totalCoins} Coin 到 ResourceManager");
                }
            }

            // 初始化建筑资源系统
            if (BuildingResourceSystemManager.Instance != null)
            {
                BuildingResourceSystemManager.Instance.LoadFromSaveData(saveData.buildingProducers);
            }

            OnDataLoaded?.Invoke();

            // 延迟加载建筑系统数据（需要等待 ModularBuildingManager 初始化完成）
            StartCoroutine(LoadBuildingSystemDelayed());
        }

        private System.Collections.IEnumerator LoadBuildingSystemDelayed()
        {
            // 等待 ModularBuildingManager 就绪（可能由 MapSystemInitializer 在 Start 中创建）
            float timeout = 5f;
            float elapsed = 0f;
            while (ModularBuildingManager.Instance == null && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (ModularBuildingManager.Instance != null && saveData.buildingSystem != null
                && saveData.buildingSystem.buildings != null && saveData.buildingSystem.buildings.Count > 0)
            {
                // 再等一帧，确保 ModularBuildingManager.Start() 已执行完毕（池初始化、蓝图字典等）
                yield return null;

                // 如果 TryLoadFromSaveData 已在 ModularBuildingManager.Start() 中加载了建筑，
                // 跳过重复加载——否则 RemoveAllBuildings() 会触发 RecalculateCapacities()，
                // 在建筑容量为 0 时将资源截断到 defaultBaseCapacities 上限
                if (ModularBuildingManager.Instance.GetActiveBuildingCount() == 0)
                {
                    ModularBuildingManager.Instance.LoadFromSaveData(saveData.buildingSystem);
                    Log($"建筑系统数据加载完成，共 {saveData.buildingSystem.buildings.Count} 个建筑");
                }
            }

            // 建筑加载完成后，重算容量并校验资源上限
            if (BuildingResourceSystemManager.Instance != null)
            {
                BuildingResourceSystemManager.Instance.RecalculateCapacities();
            }
            if (ResourceManager.Instance != null)
            {
                ResourceManager.Instance.ClampResourcesToCapacity();
            }
        }
        
        /// <summary>
        /// 保存存档
        /// </summary>
        public void Save()
        {
            try
            {
                // 更新任务列表引用
                if (TaskManager.Instance != null)
                {
                    saveData.tasks = new List<TaskData>(TaskManager.Instance.Tasks);
                }

                // 更新记录列表引用
                if (StatisticsManager.Instance != null)
                {
                    saveData.pomodoroRecords = StatisticsManager.Instance.GetAllRecords();
                    saveData.statistics = Statistics;
                }

                // 更新资源数据
                if (ResourceManager.Instance != null)
                {
                    saveData.resourceData = ResourceManager.Instance.CreateSaveData();
                }

                // 更新建筑生产器数据
                if (BuildingResourceSystemManager.Instance != null)
                {
                    saveData.buildingProducers = BuildingResourceSystemManager.Instance.CreateSaveData();
                }

                // 更新建筑系统数据（建筑实例的位置、状态等）
                if (ModularBuildingManager.Instance != null)
                {
                    saveData.buildingSystem = ModularBuildingManager.Instance.CreateSaveData();
                }

                saveData.UpdateSaveTime();

                string json = JsonUtility.ToJson(saveData, false); // 【优化】不使用格式化，减少文件大小
                File.WriteAllText(SaveFilePath, json);

                Log($"存档保存成功");

                OnDataSaved?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogError($"保存存档失败: {e.Message}");
            }
        }
        
        /// <summary>
        /// 添加番茄钟记录
        /// </summary>
        public void AddPomodoroRecord(PomodoroRecord record)
        {
            if (record == null) return;
            
            // 【修复】同时添加到 saveData 和 StatisticsManager
            saveData.pomodoroRecords.Add(record);
            StatisticsManager.Instance?.AddRecord(record);
            
            // 限制记录数量，防止文件过大
            const int MAX_RECORDS = 1000;
            if (saveData.pomodoroRecords.Count > MAX_RECORDS)
            {
                int removeCount = saveData.pomodoroRecords.Count - MAX_RECORDS;
                saveData.pomodoroRecords.RemoveRange(0, removeCount);
                Log($"记录数超过限制，已移除 {removeCount} 条旧记录");
            }
            
            Save();
        }
        
        public void UpdateSettings(SettingsData newSettings)
        {
            saveData.settings = newSettings;
            Save();
        }
        
        public void ResetAllData()
        {
            saveData = new SaveData();

            TaskManager.Instance?.Initialize(saveData.tasks);
            StatisticsManager.Instance?.Initialize(saveData.pomodoroRecords, saveData.statistics);
            ResourceManager.Instance?.Initialize(saveData.resourceData);

            Save();

            Log("所有数据已重置");
        }
        
        public string ExportData()
        {
            return JsonUtility.ToJson(saveData, true);
        }
        
        public bool ImportData(string json)
        {
            try
            {
                var importedData = JsonUtility.FromJson<SaveData>(json);
                if (importedData != null)
                {
                    saveData = importedData;

                    TaskManager.Instance?.Initialize(saveData.tasks);
                    StatisticsManager.Instance?.Initialize(saveData.pomodoroRecords, saveData.statistics);
                    ResourceManager.Instance?.Initialize(saveData.resourceData);

                    Save();

                    OnDataLoaded?.Invoke();
                    return true;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"导入数据失败: {e.Message}");
            }
            return false;
        }
        
        public string GetSaveInfo()
        {
            int resourceCount = saveData.resourceData?.resourceEntries?.Count ?? 0;
            return $"最后保存: {saveData.lastSaveTime}\n" +
                   $"任务数: {saveData.tasks.Count}\n" +
                   $"记录数: {saveData.pomodoroRecords.Count}\n" +
                   $"总番茄钟: {saveData.statistics.totalPomodorosCompleted}\n" +
                   $"资源类型: {resourceCount}";
        }
        
        public bool HasActiveSession()
        {
            return saveData.currentSession.hasActiveSession;
        }
        
        public void ClearHistory()
        {
            saveData.pomodoroRecords.Clear();
            saveData.statistics = new StatisticsData();
            
            foreach (var task in saveData.tasks)
            {
                task.completedPomodoros = 0;
                task.totalFocusTimeSeconds = 0;
            }
            
            StatisticsManager.Instance?.Initialize(saveData.pomodoroRecords, saveData.statistics);
            
            Save();
            
            Log("历史记录已清除");
        }
        
        public void ForceReload()
        {
            Load();
        }
    }
}
