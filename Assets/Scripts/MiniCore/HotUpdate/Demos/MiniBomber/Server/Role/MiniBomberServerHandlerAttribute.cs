using MiniCore.Model;

namespace MiniCore.Demo.MiniBomber
{
    /// <summary>
    /// 以 MiniBomber 业务枚举标记服务端 Handler，同时向框架传递通用位值。
    /// </summary>
    public sealed class MiniBomberServerHandlerAttribute : ServerHandlerAttribute
    {
        #region Public 公共成员

        /// <summary>
        /// 创建 MiniBomber 服务端 Handler 标记。
        /// </summary>
        /// <param name="roles">允许注册 Handler 的 MiniBomber 业务 Role。</param>
        public MiniBomberServerHandlerAttribute(MiniBomberServerRole roles)
            : base((ulong)roles)
        {
        }

        #endregion
    }
}
