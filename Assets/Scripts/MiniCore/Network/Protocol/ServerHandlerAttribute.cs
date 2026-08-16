using System;

namespace MiniCore.Model
{
    /// <summary>
    /// 标记仅由指定 Dedicated Server Role 注册的网络 Handler。
    /// 未标记的 Handler 视为客户端 Handler。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class ServerHandlerAttribute : Attribute
    {
        #region Public 公共成员

        /// <summary>
        /// 获取允许注册当前 Handler 的服务端 Role 集合。
        /// </summary>
        public DedicatedServerRole Roles { get; }

        /// <summary>
        /// 创建服务端 Handler 标记。
        /// </summary>
        /// <param name="roles">允许注册 Handler 的服务端 Role。</param>
        public ServerHandlerAttribute(DedicatedServerRole roles)
        {
            if (roles == DedicatedServerRole.None)
            {
                throw new ArgumentOutOfRangeException(nameof(roles), "服务端 Handler 必须至少归属于一个 Role。");
            }

            Roles = roles;
        }

        #endregion
    }
}
