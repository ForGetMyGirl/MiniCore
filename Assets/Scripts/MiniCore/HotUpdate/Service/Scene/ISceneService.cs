using MiniCore.Threading;
using UnityEngine;
using UnityEngine.U2D;

namespace MiniCore.Service
{
    /// <summary>
    /// 提供 YooAsset 单场景切换和场景句柄生命周期管理能力。
    /// </summary>
    public interface ISceneService : IAppService
    {
        /// <summary>
        /// 获取当前场景加载进度，空闲时为一。
        /// </summary>
        float Progress { get; }

        /// <summary>
        /// 场景加载进度变化事件，参数范围为零到一。
        /// </summary>
        event System.Action<float> ProgressChanged;

        /// <summary>
        /// 以 Single 模式切换到指定 YooAsset 场景。
        /// </summary>
        /// <param name="address">场景资源地址。</param>
        /// <returns>场景加载并激活完成任务。</returns>
        MTask LoadSingleAsync(string address);
    }

}
