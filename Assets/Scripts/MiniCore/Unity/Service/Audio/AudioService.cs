using System;
using System.Collections.Generic;
using MiniCore.Core;
using MiniCore.Model;
using UnityEngine;
using UnityEngine.Audio;

namespace MiniCore.Service
{
    /// <summary>
    /// 音频服务的可选启动参数。
    /// </summary>
    public sealed class AudioServiceInitArgs : ComponentInitArgs
    {
        /// <summary>
        /// 获取或设置项目的 AudioMixer；未提供时使用 AudioSource 线性音量回退。
        /// </summary>
        public AudioMixer Mixer { get; set; }

        /// <summary>
        /// 获取或设置同时保留的音效 AudioSource 数量。
        /// </summary>
        public int PoolSize { get; set; } = 12;
    }

    /// <summary>
    /// 基于持久化 AudioSource 池的 BGM、SFX 和 UI 音频应用服务。
    /// </summary>
    [AppService("音频", typeof(IAudioService), Description = "播放并管理 BGM、音效和 UI 音频。", InitArgsType = typeof(AudioServiceInitArgs))]
    public sealed class AudioService : AAppService, IAudioService
    {
        #region Private 私有成员

        private readonly List<AudioSource> sfxPool = new List<AudioSource>(); // 可复用的一次性音效源。
        private readonly Dictionary<string, float> volumes = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase); // 各逻辑分组线性音量。
        private readonly Dictionary<string, bool> muted = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase); // 各逻辑分组静音状态。
        private GameObject hostObject; // 持久化 Unity 音频根对象。
        private AudioSource bgmSource; // 当前唯一 BGM 音源。
        private AudioMixer mixer; // 项目可选配置的 Unity Mixer。
        private ISettingsService settingsService; // 可选客户端设置服务。
        private ITelemetryService telemetry; // 可选遥测服务。
        private int nextPoolIndex; // 下一个可复用 SFX 音源位置。

        #endregion

        #region Override 重写实现

        /// <summary>
        /// 使用默认音源池创建音频服务。
        /// </summary>
        public override void Awake()
        {
            Initialize(null);
        }

        /// <summary>
        /// 使用项目提供的 Mixer 与池大小创建音频服务。
        /// </summary>
        /// <param name="args">音频服务启动参数。</param>
        public override void Awake(ComponentInitArgs args)
        {
            if (!(args is AudioServiceInitArgs audioArgs))
            {
                throw new ArgumentException("音频服务必须使用 AudioServiceInitArgs 初始化。", nameof(args));
            }

            Initialize(audioArgs);
        }

        /// <summary>
        /// 解除设置订阅、释放全局服务引用并销毁 Unity 音频根对象。
        /// </summary>
        protected override void OnDispose()
        {
            if (settingsService != null)
            {
                settingsService.Changed -= ApplySettings;
            }

            if (hostObject != null)
            {
                UnityEngine.Object.Destroy(hostObject);
            }

            sfxPool.Clear();
            settingsService = null;
            telemetry = null;
        }

        #endregion

        #region Interface 接口实现

        /// <summary>
        /// 从 Resources 路径切换当前 BGM。
        /// </summary>
        /// <param name="resourcePath">AudioClip 资源路径。</param>
        /// <param name="loop">是否循环播放。</param>
        public void PlayBgm(string resourcePath, bool loop = true)
        {
            AudioClip clip = LoadClip(resourcePath);
            if (clip == null)
            {
                return;
            }

            bgmSource.clip = clip;
            bgmSource.loop = loop;
            bgmSource.volume = GetEffectiveVolume("BGM");
            bgmSource.mute = IsMuted("BGM");
            bgmSource.Play();
            telemetry?.Increment("audio.bgm_play");
        }

        /// <summary>
        /// 播放一次性游戏音效。
        /// </summary>
        /// <param name="resourcePath">AudioClip 资源路径。</param>
        public void PlaySfx(string resourcePath)
        {
            PlayOneShot(resourcePath, "SFX");
        }

        /// <summary>
        /// 播放一次性 UI 音效。
        /// </summary>
        /// <param name="resourcePath">AudioClip 资源路径。</param>
        public void PlayUi(string resourcePath)
        {
            PlayOneShot(resourcePath, "UI");
        }

        /// <summary>
        /// 设置指定逻辑分组的线性音量，并同步可选 AudioMixer 暴露参数。
        /// </summary>
        /// <param name="group">分组名称。</param>
        /// <param name="volume">零到一之间的线性音量。</param>
        public void SetVolume(string group, float volume)
        {
            if (string.IsNullOrWhiteSpace(group))
            {
                return;
            }

            volumes[group] = Mathf.Clamp01(volume);
            ApplyGroup(group);
        }

        /// <summary>
        /// 设置指定逻辑分组的静音状态。
        /// </summary>
        /// <param name="group">分组名称。</param>
        /// <param name="isMuted">是否静音。</param>
        public void SetMuted(string group, bool isMuted)
        {
            if (string.IsNullOrWhiteSpace(group))
            {
                return;
            }

            muted[group] = isMuted;
            ApplyGroup(group);
        }

        /// <summary>
        /// 停止当前背景音乐。
        /// </summary>
        public void StopBgm()
        {
            bgmSource?.Stop();
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 创建持久化音频根对象、音源池并读取可选设置服务。
        /// </summary>
        /// <param name="args">可选初始化参数。</param>
        private void Initialize(AudioServiceInitArgs args)
        {
            mixer = args?.Mixer;
            hostObject = new GameObject("MiniCore.AudioService");
            UnityEngine.Object.DontDestroyOnLoad(hostObject);
            bgmSource = hostObject.AddComponent<AudioSource>();
            bgmSource.playOnAwake = false;
            int poolSize = Mathf.Max(1, args?.PoolSize ?? 12);
            for (int index = 0; index < poolSize; index++)
            {
                AudioSource source = hostObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                sfxPool.Add(source);
            }

            SetVolume("Master", 1f);
            SetVolume("BGM", 1f);
            SetVolume("SFX", 1f);
            SetVolume("UI", 1f);
            Global.TryGetService(this, out settingsService);
            Global.TryGetService(this, out telemetry);
            if (settingsService != null)
            {
                settingsService.Changed += ApplySettings;
                ApplySettings(settingsService.Current);
            }
        }

        /// <summary>
        /// 从客户端设置更新音频分组音量。
        /// </summary>
        /// <param name="settings">当前客户端设置。</param>
        private void ApplySettings(ClientSettings settings)
        {
            if (settings == null)
            {
                return;
            }

            SetVolume("BGM", settings.BgmVolume);
            SetVolume("SFX", settings.SfxVolume);
            SetVolume("UI", settings.UiVolume);
        }

        /// <summary>
        /// 从 Resources 加载指定路径的音频资源。
        /// </summary>
        /// <param name="resourcePath">资源路径。</param>
        /// <returns>加载成功的音频资源；不存在时返回 null。</returns>
        private static AudioClip LoadClip(string resourcePath)
        {
            if (string.IsNullOrWhiteSpace(resourcePath))
            {
                return null;
            }

            return Resources.Load<AudioClip>(resourcePath);
        }

        /// <summary>
        /// 用池化 AudioSource 播放一次性逻辑分组音效。
        /// </summary>
        /// <param name="resourcePath">AudioClip 资源路径。</param>
        /// <param name="group">逻辑音频分组。</param>
        private void PlayOneShot(string resourcePath, string group)
        {
            AudioClip clip = LoadClip(resourcePath);
            if (clip == null || sfxPool.Count == 0)
            {
                return;
            }

            AudioSource source = sfxPool[nextPoolIndex++ % sfxPool.Count];
            source.loop = false;
            source.mute = IsMuted(group);
            source.volume = GetEffectiveVolume(group);
            source.PlayOneShot(clip);
            telemetry?.Increment("audio.oneshot_play");
        }

        /// <summary>
        /// 应用分组音量到 Mixer 暴露参数和当前 AudioSource。
        /// </summary>
        /// <param name="group">逻辑音频分组。</param>
        private void ApplyGroup(string group)
        {
            if (mixer != null)
            {
                float linear = IsMuted(group) ? 0f : GetEffectiveVolume(group);
                mixer.SetFloat(group + "Volume", linear <= 0.0001f ? -80f : Mathf.Log10(linear) * 20f);
            }

            if (string.Equals(group, "BGM", StringComparison.OrdinalIgnoreCase) && bgmSource != null)
            {
                bgmSource.volume = GetEffectiveVolume(group);
                bgmSource.mute = IsMuted(group);
            }
        }

        /// <summary>
        /// 获取分组与 Master 相乘后的线性音量。
        /// </summary>
        /// <param name="group">逻辑音频分组。</param>
        /// <returns>有效线性音量。</returns>
        private float GetEffectiveVolume(string group)
        {
            volumes.TryGetValue(group, out float groupVolume);
            volumes.TryGetValue("Master", out float masterVolume);
            return groupVolume * masterVolume;
        }

        /// <summary>
        /// 获取分组或 Master 是否静音。
        /// </summary>
        /// <param name="group">逻辑音频分组。</param>
        /// <returns>应静音时返回 true。</returns>
        private bool IsMuted(string group)
        {
            return (muted.TryGetValue(group, out bool groupMuted) && groupMuted) || (muted.TryGetValue("Master", out bool masterMuted) && masterMuted);
        }

        #endregion
    }
}
