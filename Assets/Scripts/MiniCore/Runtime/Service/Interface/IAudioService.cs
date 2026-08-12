using System;
using System.Collections.Generic;
using System.Threading;
using MiniCore.Threading;

namespace MiniCore.Service
{

    /// <summary>
    /// 提供按资源地址播放 BGM、音效与 UI 音效的系统服务契约。
    /// </summary>
    public interface IAudioService : IAppService
    {
        /// <summary>
        /// 从 Resources 路径切换当前背景音乐。
        /// 项目可通过 AudioService 的资源注册 API 替换默认 Resources 加载策略。
        /// </summary>
        /// <param name="resourcePath">AudioClip 的资源路径。</param>
        /// <param name="loop">是否循环播放。</param>
        void PlayBgm(string resourcePath, bool loop = true);

        /// <summary>
        /// 从 Resources 路径播放一次性音效。
        /// </summary>
        /// <param name="resourcePath">AudioClip 的资源路径。</param>
        void PlaySfx(string resourcePath);

        /// <summary>
        /// 从 Resources 路径播放一次性 UI 音效。
        /// </summary>
        /// <param name="resourcePath">AudioClip 的资源路径。</param>
        void PlayUi(string resourcePath);

        /// <summary>
        /// 设置指定混音分组的线性音量。
        /// </summary>
        /// <param name="group">分组名称：Master、BGM、SFX 或 UI。</param>
        /// <param name="volume">零到一之间的线性音量。</param>
        void SetVolume(string group, float volume);

        /// <summary>
        /// 设置指定混音分组的静音状态。
        /// </summary>
        /// <param name="group">分组名称。</param>
        /// <param name="muted">是否静音。</param>
        void SetMuted(string group, bool muted);

        /// <summary>
        /// 停止当前 BGM。
        /// </summary>
        void StopBgm();
    }
}
