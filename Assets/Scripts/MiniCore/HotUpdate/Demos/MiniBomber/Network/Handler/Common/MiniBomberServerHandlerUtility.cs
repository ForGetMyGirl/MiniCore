using MiniCore.Core;
using MiniCore.Model;
using MiniCore.Protocol.Generated;
using MiniCore.Threading;

namespace MiniCore.Demo.MiniBomber
{
    /// <summary>
    /// MiniBomber 服务端 Handler 获取运行时的公共辅助方法。
    /// </summary>
    internal static class MiniBomberServerHandlerUtility
    {
        #region Internal 内部成员

        /// <summary>
        /// 获取已经初始化的 Dedicated Server 运行时。
        /// </summary>
        /// <param name="owner">Global 引用所有者。</param>
        /// <param name="response">运行时不可用时写入错误的响应。</param>
        /// <param name="runtime">可用服务器运行时。</param>
        /// <returns>运行时存在且已经初始化时返回 true。</returns>
        internal static bool TryGetRuntime(object owner, IRpcResponse response, out MiniBomberServerRuntimeComponent runtime)
        {
            runtime = Global.Get<MiniBomberServerRuntimeComponent>(owner);
            if (runtime != null && runtime.IsInitialized)
            {
                return true;
            }

            response.Code = MiniBomberErrorCode.InvalidRoomState;
            response.Msg = "MiniBomber 服务器尚未就绪";
            Global.ReleaseAll(owner);
            return false;
        }

        #endregion
    }
}
