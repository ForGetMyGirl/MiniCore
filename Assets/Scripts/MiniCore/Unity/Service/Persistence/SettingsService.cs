using System;
using System.Threading;
using System.Threading.Tasks;
using MiniCore.Core;
using MiniCore.Model;

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
        public override void Dispose()
        {
            Changed = null;
            Global.ReleaseAll(this);
            saveService = null;
            base.Dispose();
        }

        #endregion

        #region Interface 接口实现

        /// <summary>
        /// 供启动流程在依赖服务完成后加载设置。
        /// </summary>
        /// <param name="token">启动取消令牌。</param>
        /// <returns>加载完成任务。</returns>
        public Task InitializeAsync(CancellationToken token = default)
        {
            return LoadAsync(token);
        }

        /// <summary>
        /// 从独立设置槽位加载设置；首次运行保持默认设置。
        /// </summary>
        /// <param name="token">取消令牌。</param>
        /// <returns>加载完成任务。</returns>
        public async Task LoadAsync(CancellationToken token = default)
        {
            ClientSettings loaded = await saveService.LoadAsync<ClientSettings>(SettingsSlot, token);
            current = loaded ?? new ClientSettings();
            Changed?.Invoke(current);
        }

        /// <summary>
        /// 替换当前设置并持久化到独立加密槽位。
        /// </summary>
        /// <param name="settings">待保存设置。</param>
        /// <param name="token">取消令牌。</param>
        /// <returns>保存完成任务。</returns>
        public async Task SaveAsync(ClientSettings settings, CancellationToken token = default)
        {
            current = settings ?? throw new ArgumentNullException(nameof(settings));
            await saveService.SaveAsync(SettingsSlot, current, token);
            Changed?.Invoke(current);
        }

        #endregion
    }
}
