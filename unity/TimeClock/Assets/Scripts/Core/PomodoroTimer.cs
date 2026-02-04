using System;
using UnityEngine;
using PomodoroTimer.Data;
using PomodoroTimer.Utils;

namespace PomodoroTimer.Core
{
    /// <summary>
    /// 番茄钟计时器核心 - 优化版
    /// 只在时间段结束后才更新存档，减少频繁IO操作
    /// </summary>
    public class PomodoroTimer : MonoBehaviour
    {
        public static PomodoroTimer Instance { get; private set; }
        
        [Header("当前状态")]
        [SerializeField] private TimerState currentState = TimerState.Idle;
        [SerializeField] private PomodoroType currentType = PomodoroType.Focus;
        [SerializeField] private TimerMode currentMode = TimerMode.Countdown;
        [SerializeField] private int currentRound = 1;
        [SerializeField] private float elapsedSeconds = 0f;
        [SerializeField] private float targetSeconds = 0f;
        
        // 当前绑定的任务
        private TaskData currentTask;
        private PomodoroRecord currentRecord;
        
        // 使用真实时间计时（支持后台运行）
        private DateTime startTime;           // 计时开始时间
        private float pausedElapsedSeconds;   // 暂停时已经过的秒数
        
        // 事件
        public event Action<float, float> OnTimerTick;           // 参数: 当前秒数, 目标秒数
        public event Action<TimerState> OnStateChanged;           // 状态变化
        public event Action<PomodoroType> OnTypeChanged;          // 类型变化(专注/休息)
        public event Action<int> OnRoundChanged;                  // 轮次变化
        public event Action<PomodoroRecord> OnPomodoroCompleted;  // 番茄钟完成
        public event Action OnCountupTimeout;                     // 正计时超时
        
        // 属性访问器
        public TimerState CurrentState => currentState;
        public PomodoroType CurrentType => currentType;
        public TimerMode CurrentMode => currentMode;
        public int CurrentRound => currentRound;
        public float ElapsedSeconds => elapsedSeconds;
        public float TargetSeconds => targetSeconds;
        public TaskData CurrentTask => currentTask;
        public int TotalRounds => DataManager.Instance.Settings.roundsBeforeLongBreak;
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                Application.runInBackground = true;
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void Start()
        {
            SetTargetTime();
        }
        
        private void Update()
        {
            if (currentState != TimerState.Running) return;
            
            // 使用真实时间计算经过的秒数
            elapsedSeconds = pausedElapsedSeconds + (float)(DateTime.Now - startTime).TotalSeconds;
            
            // 倒计时模式
            if (currentMode == TimerMode.Countdown)
            {
                float remainingTime = targetSeconds - elapsedSeconds;
                OnTimerTick?.Invoke(remainingTime, targetSeconds);
                
                if (remainingTime <= 0)
                {
                    CompletePomodoro();
                }
            }
            // 正计时模式
            else
            {
                OnTimerTick?.Invoke(elapsedSeconds, targetSeconds);
                
                float maxSeconds = DataManager.Instance.Settings.countupMaxMinutes * 60f;
                if (elapsedSeconds >= maxSeconds)
                {
                    OnCountupTimeout?.Invoke();
                    CompletePomodoro();
                }
            }
            
            // 【优化】移除了每5秒保存的逻辑，改为只更新内存中的会话状态
            UpdateSessionInMemory();
        }
        
        /// <summary>
        /// 只更新内存中的会话状态，不写入磁盘
        /// </summary>
        private void UpdateSessionInMemory()
        {
            if (currentState == TimerState.Idle) return;
            var session = DataManager.Instance.CurrentSession;
            session.elapsedSeconds = elapsedSeconds;
            // 不调用 DataManager.Instance.Save()
        }
        
        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus && currentState == TimerState.Running)
            {
                OnTimerTick?.Invoke(
                    currentMode == TimerMode.Countdown ? targetSeconds - elapsedSeconds : elapsedSeconds,
                    targetSeconds
                );
            }
        }
        
        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                // 应用进入后台时保存会话状态
                if (currentState != TimerState.Idle)
                {
                    SaveSessionToDisk();
                }
            }
            else if (currentState == TimerState.Running)
            {
                OnTimerTick?.Invoke(
                    currentMode == TimerMode.Countdown ? targetSeconds - elapsedSeconds : elapsedSeconds,
                    targetSeconds
                );
            }
        }
        
        private void OnApplicationQuit()
        {
            // 应用退出时保存
            if (currentState != TimerState.Idle)
            {
                SaveSessionToDisk();
            }
        }
        
        /// <summary>
        /// 保存会话到磁盘（只在关键时刻调用）
        /// </summary>
        private void SaveSessionToDisk()
        {
            var session = DataManager.Instance.CurrentSession;
            session.elapsedSeconds = elapsedSeconds;
            DataManager.Instance.Save();
        }
        
        public void BindTask(TaskData task)
        {
            if (currentState == TimerState.Running)
            {
                Debug.LogWarning("计时进行中，无法切换任务");
                return;
            }
            currentTask = task;
        }
        
        public void StartCountdown()
        {
            if (currentTask == null)
            {
                Debug.LogWarning("请先选择任务");
                return;
            }
            
            currentMode = TimerMode.Countdown;
            SetTargetTime();
            StartTimer();
        }
        
        public void StartCountup()
        {
            if (currentTask == null)
            {
                Debug.LogWarning("请先选择任务");
                return;
            }
            
            currentMode = TimerMode.Countup;
            targetSeconds = DataManager.Instance.Settings.countupMaxMinutes * 60f;
            currentType = PomodoroType.Focus;
            StartTimer();
        }
        
        private void StartTimer()
        {
            startTime = DateTime.Now;
            pausedElapsedSeconds = 0;
            elapsedSeconds = 0;
            
            currentState = TimerState.Running;
            
            currentRecord = new PomodoroRecord(
                currentTask.id,
                currentTask.taskName,
                currentTask.colorIndex,
                currentType,
                currentMode
            );
            
            // 开始时保存会话信息
            var session = DataManager.Instance.CurrentSession;
            session.StartSession(currentTask.id, currentType, currentMode, currentRound);
            session.startTimestamp = startTime.Ticks;
            DataManager.Instance.Save();
            
            OnStateChanged?.Invoke(currentState);
            OnTypeChanged?.Invoke(currentType);
            OnTimerTick?.Invoke(
                currentMode == TimerMode.Countdown ? targetSeconds : 0,
                targetSeconds
            );
        }
        
        public void Pause()
        {
            if (currentState != TimerState.Running) return;
            
            pausedElapsedSeconds = elapsedSeconds;
            currentState = TimerState.Paused;
            
            // 暂停时保存
            SaveSessionToDisk();
            OnStateChanged?.Invoke(currentState);
        }
        
        public void Resume()
        {
            if (currentState != TimerState.Paused) return;
            
            startTime = DateTime.Now;
            currentState = TimerState.Running;
            OnStateChanged?.Invoke(currentState);
        }
        
        public void Stop()
        {
            if (currentState == TimerState.Idle) return;
            
            // 正计时模式下，检查是否达到最小时长
            if (currentMode == TimerMode.Countup && currentType == PomodoroType.Focus)
            {
                float minSeconds = DataManager.Instance.Settings.countupMinThreshold * 60f;
                if (elapsedSeconds >= minSeconds)
                {
                    CompleteCountupPomodoro();
                    return;
                }
            }
            
            currentState = TimerState.Idle;
            elapsedSeconds = 0;
            pausedElapsedSeconds = 0;
            
            DataManager.Instance.CurrentSession.Clear();
            DataManager.Instance.Save();
            
            SetTargetTime();
            OnStateChanged?.Invoke(currentState);
            OnTimerTick?.Invoke(targetSeconds, targetSeconds);
        }
        
        public void Skip()
        {
            if (currentState == TimerState.Idle) return;
            
            currentState = TimerState.Idle;
            elapsedSeconds = 0;
            pausedElapsedSeconds = 0;
            
            if (currentType == PomodoroType.Focus)
            {
                MoveToBreak();
            }
            else
            {
                MoveToNextFocus();
            }
            
            DataManager.Instance.CurrentSession.Clear();
            DataManager.Instance.Save();
            
            OnStateChanged?.Invoke(currentState);
            OnTimerTick?.Invoke(targetSeconds, targetSeconds);
        }
        
        private void CompleteCountupPomodoro()
        {
            currentState = TimerState.Idle;
            
            if (currentRecord != null)
            {
                currentRecord.Complete(elapsedSeconds, true);
                DataManager.Instance.AddPomodoroRecord(currentRecord);
                
                if (currentTask != null)
                {
                    TaskManager.Instance.AddPomodoroToTask(currentTask.id, elapsedSeconds);
                    StatisticsManager.Instance.AddCompletedPomodoro(elapsedSeconds, currentTask.id);
                }
                
                OnPomodoroCompleted?.Invoke(currentRecord);
            }
            
            AudioManager.Instance?.PlayComplete();
            
            currentType = PomodoroType.Focus;
            currentMode = TimerMode.Countdown;
            SetTargetTime();
            
            DataManager.Instance.CurrentSession.Clear();
            DataManager.Instance.Save();
            
            elapsedSeconds = 0;
            pausedElapsedSeconds = 0;
            
            OnStateChanged?.Invoke(currentState);
            OnTypeChanged?.Invoke(currentType);
            OnTimerTick?.Invoke(targetSeconds, targetSeconds);
        }
        
        private void CompletePomodoro()
        {
            PomodoroType completedType = currentType;
            bool wasCountup = (currentMode == TimerMode.Countup);
            
            currentState = TimerState.Idle;
            
            // 【关键】只在时间段完成时才保存记录
            if (currentRecord != null)
            {
                currentRecord.Complete(elapsedSeconds, true);
                DataManager.Instance.AddPomodoroRecord(currentRecord);
                
                if (completedType == PomodoroType.Focus && currentTask != null)
                {
                    TaskManager.Instance.AddPomodoroToTask(currentTask.id, elapsedSeconds);
                    StatisticsManager.Instance.AddCompletedPomodoro(elapsedSeconds, currentTask.id);
                }
                
                OnPomodoroCompleted?.Invoke(currentRecord);
            }
            
            AudioManager.Instance?.PlayComplete();
            
            elapsedSeconds = 0;
            pausedElapsedSeconds = 0;
            
            DataManager.Instance.CurrentSession.Clear();
            
            if (wasCountup)
            {
                currentType = PomodoroType.Focus;
                currentMode = TimerMode.Countdown;
                SetTargetTime();
                DataManager.Instance.Save();
                
                OnStateChanged?.Invoke(currentState);
                OnTypeChanged?.Invoke(currentType);
                OnTimerTick?.Invoke(targetSeconds, targetSeconds);
                return;
            }
            
            if (completedType == PomodoroType.Focus)
            {
                MoveToBreak();
            }
            else
            {
                MoveToNextFocus();
            }
            
            DataManager.Instance.Save();
            OnStateChanged?.Invoke(currentState);
        }
        
        private void MoveToBreak()
        {
            var settings = DataManager.Instance.Settings;
            
            if (currentRound >= settings.roundsBeforeLongBreak)
            {
                currentType = PomodoroType.LongBreak;
            }
            else
            {
                currentType = PomodoroType.ShortBreak;
            }
            
            currentMode = TimerMode.Countdown;
            SetTargetTime();
            
            OnTypeChanged?.Invoke(currentType);
            
            if (settings.autoStartBreak && currentTask != null)
            {
                StartTimer();
            }
            else
            {
                OnTimerTick?.Invoke(targetSeconds, targetSeconds);
            }
        }
        
        private void MoveToNextFocus()
        {
            var settings = DataManager.Instance.Settings;

            // 如果刚完成长休息，说明一个完整周期结束
            if (currentType == PomodoroType.LongBreak)
            {
                // 重置轮次，准备下一个周期
                currentRound = 1;
                currentType = PomodoroType.Focus;
                currentMode = TimerMode.Countdown;
                SetTargetTime();

                OnRoundChanged?.Invoke(currentRound);
                OnTypeChanged?.Invoke(currentType);

                // 长休息后不自动开始，让用户决定是否继续
                OnTimerTick?.Invoke(targetSeconds, targetSeconds);
                return;
            }

            // 短休息结束，进入下一轮
            if (currentType == PomodoroType.ShortBreak)
            {
                currentRound++;
            }

            currentType = PomodoroType.Focus;
            currentMode = TimerMode.Countdown;
            SetTargetTime();

            OnRoundChanged?.Invoke(currentRound);
            OnTypeChanged?.Invoke(currentType);

            if (settings.autoStartFocus && currentTask != null)
            {
                StartTimer();
            }
            else
            {
                OnTimerTick?.Invoke(targetSeconds, targetSeconds);
            }
        }
        
        private void SetTargetTime()
        {
            var settings = DataManager.Instance.Settings;
            
            switch (currentType)
            {
                case PomodoroType.Focus:
                    targetSeconds = settings.focusDurationMinutes * 60f;
                    break;
                case PomodoroType.ShortBreak:
                    targetSeconds = settings.shortBreakMinutes * 60f;
                    break;
                case PomodoroType.LongBreak:
                    targetSeconds = settings.longBreakMinutes * 60f;
                    break;
            }
        }
        
        public void RestoreSession()
        {
            var session = DataManager.Instance.CurrentSession;
            if (!session.hasActiveSession) return;
            
            var task = TaskManager.Instance.GetTaskById(session.currentTaskId);
            if (task == null)
            {
                session.Clear();
                return;
            }
            
            currentTask = task;
            currentRound = session.currentRound;
            currentType = session.currentType;
            currentMode = session.currentMode;
            
            pausedElapsedSeconds = session.elapsedSeconds;
            elapsedSeconds = pausedElapsedSeconds;
            startTime = DateTime.Now;
            
            SetTargetTime();
            
            currentState = TimerState.Paused;
            
            currentRecord = new PomodoroRecord(
                currentTask.id,
                currentTask.taskName,
                currentTask.colorIndex,
                currentType,
                currentMode
            );
            
            OnStateChanged?.Invoke(currentState);
            OnTypeChanged?.Invoke(currentType);
            OnRoundChanged?.Invoke(currentRound);
            
            float displayTime = currentMode == TimerMode.Countdown 
                ? targetSeconds - elapsedSeconds 
                : elapsedSeconds;
            OnTimerTick?.Invoke(displayTime, targetSeconds);
        }
        
        public string GetDisplayTime()
        {
            float displaySeconds;
            
            if (currentMode == TimerMode.Countdown)
            {
                displaySeconds = Mathf.Max(0, targetSeconds - elapsedSeconds);
            }
            else
            {
                displaySeconds = elapsedSeconds;
            }
            
            int minutes = (int)(displaySeconds / 60);
            int seconds = (int)(displaySeconds % 60);
            
            return $"{minutes:D2}:{seconds:D2}";
        }
        
        public string GetStateText()
        {
            switch (currentType)
            {
                case PomodoroType.Focus:
                    return "专注中";
                case PomodoroType.ShortBreak:
                    return "短休息";
                case PomodoroType.LongBreak:
                    return "长休息";
                default:
                    return "";
            }
        }
        
        public string GetModeText()
        {
            return currentMode == TimerMode.Countdown ? "倒计时" : "正计时";
        }
        
        public void Reset()
        {
            currentState = TimerState.Idle;
            currentType = PomodoroType.Focus;
            currentMode = TimerMode.Countdown;
            currentRound = 1;
            elapsedSeconds = 0;
            pausedElapsedSeconds = 0;
            currentTask = null;
            currentRecord = null;
            
            SetTargetTime();
            
            DataManager.Instance.CurrentSession.Clear();
            
            OnStateChanged?.Invoke(currentState);
            OnTypeChanged?.Invoke(currentType);
            OnRoundChanged?.Invoke(currentRound);
            OnTimerTick?.Invoke(targetSeconds, targetSeconds);
        }
        
        /// <summary>
        /// 刷新显示（设置更新后调用）
        /// 只在空闲状态下更新目标时间和显示
        /// </summary>
        public void RefreshDisplay()
        {
            // 只有空闲状态才刷新时间显示
            if (currentState == TimerState.Idle)
            {
                SetTargetTime();
                OnTimerTick?.Invoke(targetSeconds, targetSeconds);
                OnRoundChanged?.Invoke(currentRound);
            }
        }
    }
}
