using UnityEngine;
using System.Collections.Generic;

namespace PomodoroTimer.Map.Sprite2D
{
    /// <summary>
    /// 建筑音效管理器
    /// 管理音效池，事件驱动播放
    /// </summary>
    public class BuildingAudioManager : MonoBehaviour
    {
        public static BuildingAudioManager Instance { get; private set; }

        [Header("音效设置")]
        [SerializeField] private int audioSourcePoolSize = 16;
        [SerializeField] private float defaultVolume = 1f;
        [SerializeField] private float spatialBlend = 0.5f;
        [SerializeField] private float minDistance = 1f;
        [SerializeField] private float maxDistance = 20f;

        [Header("环境音效")]
        [SerializeField] private float ambientVolume = 0.3f;
        [SerializeField] private float ambientFadeTime = 1f;

        // 音效池
        private List<AudioSource> audioSourcePool = new List<AudioSource>();
        private Dictionary<int, AudioSource> ambientSources = new Dictionary<int, AudioSource>();
        private Transform audioContainer;

        // 音量控制
        private float masterVolume = 1f;
        private float sfxVolume = 1f;
        private float ambientMasterVolume = 1f;

        public float MasterVolume
        {
            get => masterVolume;
            set
            {
                masterVolume = Mathf.Clamp01(value);
                UpdateAllVolumes();
            }
        }

        public float SFXVolume
        {
            get => sfxVolume;
            set
            {
                sfxVolume = Mathf.Clamp01(value);
            }
        }

        public float AmbientVolume
        {
            get => ambientMasterVolume;
            set
            {
                ambientMasterVolume = Mathf.Clamp01(value);
                UpdateAmbientVolumes();
            }
        }

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

            CreateAudioContainer();
            InitializePool();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void CreateAudioContainer()
        {
            var containerObj = new GameObject("AudioSources");
            containerObj.transform.SetParent(transform);
            audioContainer = containerObj.transform;
        }

        private void InitializePool()
        {
            for (int i = 0; i < audioSourcePoolSize; i++)
            {
                CreatePooledAudioSource();
            }
        }

        private AudioSource CreatePooledAudioSource()
        {
            var go = new GameObject("PooledAudioSource");
            go.transform.SetParent(audioContainer);

            var source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = spatialBlend;
            source.minDistance = minDistance;
            source.maxDistance = maxDistance;
            source.rolloffMode = AudioRolloffMode.Linear;

            audioSourcePool.Add(source);
            return source;
        }

        private AudioSource GetAvailableSource()
        {
            // 查找空闲的音源
            foreach (var source in audioSourcePool)
            {
                if (!source.isPlaying)
                    return source;
            }

            // 如果没有空闲的，创建新的
            if (audioSourcePool.Count < audioSourcePoolSize * 2)
            {
                return CreatePooledAudioSource();
            }

            // 如果池已满，返回最早的音源（会中断播放）
            return audioSourcePool[0];
        }

        #region 播放方法

        /// <summary>
        /// 播放一次性音效
        /// </summary>
        public void PlaySound(AudioClip clip, Vector3 position, float volumeScale = 1f)
        {
            if (clip == null) return;

            var source = GetAvailableSource();
            source.transform.position = position;
            source.clip = clip;
            source.volume = defaultVolume * volumeScale * sfxVolume * masterVolume;
            source.loop = false;
            source.Play();
        }

        /// <summary>
        /// 播放一次性音效（2D）
        /// </summary>
        public void PlaySound2D(AudioClip clip, float volumeScale = 1f)
        {
            if (clip == null) return;

            var source = GetAvailableSource();
            source.transform.position = Vector3.zero;
            source.clip = clip;
            source.volume = defaultVolume * volumeScale * sfxVolume * masterVolume;
            source.spatialBlend = 0f;
            source.loop = false;
            source.Play();

            // 播放完后恢复空间混合
            StartCoroutine(ResetSpatialBlend(source, clip.length));
        }

        private System.Collections.IEnumerator ResetSpatialBlend(AudioSource source, float delay)
        {
            yield return new WaitForSeconds(delay + 0.1f);
            source.spatialBlend = spatialBlend;
        }

        /// <summary>
        /// 播放建筑点击音效
        /// </summary>
        public void PlayBuildingClick(ModularBuildingInstance building)
        {
            if (building?.Blueprint?.clickSound != null)
            {
                PlaySound(building.Blueprint.clickSound, building.transform.position);
            }
        }

        /// <summary>
        /// 播放建筑放置音效
        /// </summary>
        public void PlayBuildingPlace(ModularBuildingInstance building)
        {
            if (building?.Blueprint?.placeSound != null)
            {
                PlaySound(building.Blueprint.placeSound, building.transform.position);
            }
        }

        #endregion

        #region 环境音效

        /// <summary>
        /// 开始播放建筑环境音效
        /// </summary>
        public void StartAmbientSound(int buildingId, AudioClip clip, Vector3 position)
        {
            if (clip == null) return;

            // 如果已经在播放，先停止
            StopAmbientSound(buildingId);

            var go = new GameObject($"AmbientSound_{buildingId}");
            go.transform.SetParent(audioContainer);
            go.transform.position = position;

            var source = go.AddComponent<AudioSource>();
            source.clip = clip;
            source.loop = true;
            source.playOnAwake = false;
            source.spatialBlend = spatialBlend;
            source.minDistance = minDistance;
            source.maxDistance = maxDistance;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.volume = 0f;

            ambientSources[buildingId] = source;
            source.Play();

            // 淡入
            StartCoroutine(FadeAmbient(source, ambientVolume * ambientMasterVolume * masterVolume, ambientFadeTime));
        }

        /// <summary>
        /// 停止播放建筑环境音效
        /// </summary>
        public void StopAmbientSound(int buildingId)
        {
            if (ambientSources.TryGetValue(buildingId, out var source))
            {
                if (source != null)
                {
                    StartCoroutine(FadeOutAndDestroy(source, ambientFadeTime));
                }
                ambientSources.Remove(buildingId);
            }
        }

        /// <summary>
        /// 更新环境音效位置
        /// </summary>
        public void UpdateAmbientPosition(int buildingId, Vector3 position)
        {
            if (ambientSources.TryGetValue(buildingId, out var source))
            {
                if (source != null)
                {
                    source.transform.position = position;
                }
            }
        }

        private System.Collections.IEnumerator FadeAmbient(AudioSource source, float targetVolume, float duration)
        {
            float startVolume = source.volume;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                source.volume = Mathf.Lerp(startVolume, targetVolume, elapsed / duration);
                yield return null;
            }

            source.volume = targetVolume;
        }

        private System.Collections.IEnumerator FadeOutAndDestroy(AudioSource source, float duration)
        {
            float startVolume = source.volume;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                source.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
                yield return null;
            }

            Destroy(source.gameObject);
        }

        private void UpdateAmbientVolumes()
        {
            foreach (var kvp in ambientSources)
            {
                if (kvp.Value != null)
                {
                    kvp.Value.volume = ambientVolume * ambientMasterVolume * masterVolume;
                }
            }
        }

        private void UpdateAllVolumes()
        {
            UpdateAmbientVolumes();
        }

        #endregion

        #region 特殊音效

        /// <summary>
        /// 播放建造开始音效
        /// </summary>
        public void PlayConstructionStart(Vector3 position)
        {
            // 可以添加默认的建造音效
        }

        /// <summary>
        /// 播放建造完成音效
        /// </summary>
        public void PlayConstructionComplete(Vector3 position)
        {
            // 可以添加默认的完成音效
        }

        /// <summary>
        /// 播放建筑损坏音效
        /// </summary>
        public void PlayBuildingDamaged(Vector3 position)
        {
            // 可以添加默认的损坏音效
        }

        /// <summary>
        /// 播放建筑摧毁音效
        /// </summary>
        public void PlayBuildingDestroyed(Vector3 position)
        {
            // 可以添加默认的摧毁音效
        }

        /// <summary>
        /// 播放无效放置音效
        /// </summary>
        public void PlayInvalidPlacement()
        {
            // 可以添加默认的错误音效
        }

        #endregion

        #region 清理

        /// <summary>
        /// 停止所有音效
        /// </summary>
        public void StopAllSounds()
        {
            foreach (var source in audioSourcePool)
            {
                if (source != null)
                    source.Stop();
            }

            var ambientIds = new List<int>(ambientSources.Keys);
            foreach (var id in ambientIds)
            {
                StopAmbientSound(id);
            }
        }

        /// <summary>
        /// 暂停所有音效
        /// </summary>
        public void PauseAllSounds()
        {
            foreach (var source in audioSourcePool)
            {
                if (source != null && source.isPlaying)
                    source.Pause();
            }

            foreach (var kvp in ambientSources)
            {
                if (kvp.Value != null && kvp.Value.isPlaying)
                    kvp.Value.Pause();
            }
        }

        /// <summary>
        /// 恢复所有音效
        /// </summary>
        public void ResumeAllSounds()
        {
            foreach (var source in audioSourcePool)
            {
                if (source != null)
                    source.UnPause();
            }

            foreach (var kvp in ambientSources)
            {
                if (kvp.Value != null)
                    kvp.Value.UnPause();
            }
        }

        #endregion
    }
}
