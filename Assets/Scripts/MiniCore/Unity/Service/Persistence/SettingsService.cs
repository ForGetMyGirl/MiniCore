using System;
using System.Threading;
using MiniCore.Threading;
using MiniCore.Core;
using MiniCore.Model;
using MiniCore.Serialization;
using MiniCore.Service.Persistence.Generated;

namespace MiniCore.Service
{
    /// <summary>
    /// 将客户端偏好设置保存到独立加密槽位的应用服务。
    /// </summary>
    [AppService("客户端设置", typeof(ISettingsService), Description = "加载、保存并通知客户端偏好设置的变化。", RequiresServices = new[] { typeof(ISaveService) })]
    public sealed class SettingsService : AAppService, ISettingsService, IAsyncAppService
    {
        #region Private 私有成员

        private const string SettingsSlot = "client-settings"; // 与游戏进度隔离的设置槽位。
        private ISaveService saveService; // 加密存档服务。
        private ClientSettings current = new ClientSettings(); // 当前内存设置快照。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 当前设置发生保存或替换后触发。
        /// </summary>
        public event Action<ClientSettings> Changed;

        /// <summary>
        /// 获取当前客户端设置。
        /// </summary>
        public ClientSettings Current => current;

        #endregion

        #region Override 重写实现

        /// <summary>
        /// 获取加密存档服务引用。
        /// </summary>
        public override void Awake()
        {
            saveService = Global.GetService<ISaveService>(this);
        }

        /// <summary>
        /// 释放存档服务引用及事件订阅者。
        /// </summary>
        protected override void OnDispose()
        {
            Changed = null;
            saveService = null;
        }

        #endregion

        #region Interface 接口实现

        /// <summary>
        /// 供启动流程在依赖服务完成后加载设置。
        /// </summary>
        /// <returns>加载完成任务。</returns>
        public MTask InitializeAsync()
        {
            return LoadAsync();
        }

        /// <summary>
        /// 从独立设置槽位加载设置；首次运行保持默认设置。
        /// </summary>
        /// <returns>加载完成任务。</returns>
        public async MTask LoadAsync()
        {
            ClientSettingsSaveData loaded = await saveService.LoadProtobufAsync<ClientSettingsSaveData>(SettingsSlot);
            current = loaded == null ? new ClientSettings() : FromSaveData(loaded);
            Changed?.Invoke(current);
        }

        /// <summary>
        /// 替换当前设置并持久化到独立加密槽位。
        /// </summary>
        /// <param name="settings">待保存设置。</param>
        /// <returns>保存完成任务。</returns>
        public async MTask SaveAsync(ClientSettings settings)
        {
            current = settings ?? throw new ArgumentNullException(nameof(settings));
            await saveService.SaveProtobufAsync(SettingsSlot, ToSaveData(current));
            Changed?.Invoke(current);
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 将运行时设置映射为稳定的 Protobuf 持久化结构。
        /// </summary>
        /// <param name="settings">运行时设置。</param>
        /// <returns>可直接编码保存的 Protobuf 消息。</returns>
        private static ClientSettingsSaveData ToSaveData(ClientSettings settings)
        {
            return new ClientSettingsSaveData
            {
                Version = settings.Version,
                QualityLevel = settings.QualityLevel ?? string.Empty,
                TargetFrameRate = settings.TargetFrameRate,
                VSyncCount = settings.VSyncCount,
                ScreenWidth = settings.ScreenWidth,
                ScreenHeight = settings.ScreenHeight,
                FullScreen = settings.FullScreen,
                BgmVolume = settings.BgmVolume,
                SfxVolume = settings.SfxVolume,
                UiVolume = settings.UiVolume,
                VibrationEnabled = settings.VibrationEnabled,
                Language = settings.Language ?? string.Empty
            };
        }

        /// <summary>
        /// 将 Protobuf 持久化结构映射为运行时设置。
        /// </summary>
        /// <param name="data">已经解析的持久化消息。</param>
        /// <returns>独立运行时设置对象。</returns>
        private static ClientSettings FromSaveData(ClientSettingsSaveData data)
        {
            return new ClientSettings
            {
                Version = data.Version,
                QualityLevel = data.QualityLevel,
                TargetFrameRate = data.TargetFrameRate,
                VSyncCount = data.VSyncCount,
                ScreenWidth = data.ScreenWidth,
                ScreenHeight = data.ScreenHeight,
                FullScreen = data.FullScreen,
                BgmVolume = data.BgmVolume,
                SfxVolume = data.SfxVolume,
                UiVolume = data.UiVolume,
                VibrationEnabled = data.VibrationEnabled,
                Language = data.Language
            };
        }

        #endregion
    }
}
