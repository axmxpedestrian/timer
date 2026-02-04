using UnityEngine;
using PomodoroTimer.Core;

namespace PomodoroTimer.Utils
{
    /// <summary>
    /// 音效管理器
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }
        
        [Header("音效资源")]
        [SerializeField] private AudioClip completeSound;    // 计时完成
        [SerializeField] private AudioClip clickSound;       // 按钮点击
        [SerializeField] private AudioClip tickSound;        // 滴答声(可选)
        
        private AudioSource audioSource;
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                }
                
                // 尝试从Resources加载音效
                LoadSoundsFromResources();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        /// <summary>
        /// 从Resources加载音效
        /// </summary>
        private void LoadSoundsFromResources()
        {
            if (completeSound == null)
            {
                completeSound = Resources.Load<AudioClip>("Audio/timer_complete");
            }
            if (clickSound == null)
            {
                clickSound = Resources.Load<AudioClip>("Audio/button_click");
            }
            if (tickSound == null)
            {
                tickSound = Resources.Load<AudioClip>("Audio/tick");
            }
        }
        
        /// <summary>
        /// 检查是否启用声音
        /// </summary>
        private bool IsSoundEnabled()
        {
            return DataManager.Instance?.Settings.soundEnabled ?? true;
        }
        
        /// <summary>
        /// 获取音量
        /// </summary>
        private float GetVolume()
        {
            return DataManager.Instance?.Settings.soundVolume ?? 0.8f;
        }
        
        /// <summary>
        /// 播放音效
        /// </summary>
        private void PlaySound(AudioClip clip)
        {
            if (!IsSoundEnabled() || clip == null) return;
            
            audioSource.PlayOneShot(clip, GetVolume());
        }
        
        /// <summary>
        /// 播放完成音效
        /// </summary>
        public void PlayComplete()
        {
            PlaySound(completeSound);
        }
        
        /// <summary>
        /// 播放点击音效
        /// </summary>
        public void PlayClick()
        {
            PlaySound(clickSound);
        }
        
        /// <summary>
        /// 播放滴答声
        /// </summary>
        public void PlayTick()
        {
            PlaySound(tickSound);
        }
        
        /// <summary>
        /// 设置音量
        /// </summary>
        public void SetVolume(float volume)
        {
            if (DataManager.Instance != null)
            {
                DataManager.Instance.Settings.soundVolume = Mathf.Clamp01(volume);
            }
        }
        
        /// <summary>
        /// 切换静音
        /// </summary>
        public void ToggleMute()
        {
            if (DataManager.Instance != null)
            {
                DataManager.Instance.Settings.soundEnabled = !DataManager.Instance.Settings.soundEnabled;
            }
        }
        
        /// <summary>
        /// 预览音效(用于设置界面)
        /// </summary>
        public void PreviewCompleteSound()
        {
            if (completeSound != null)
            {
                audioSource.PlayOneShot(completeSound, GetVolume());
            }
        }
    }
}
