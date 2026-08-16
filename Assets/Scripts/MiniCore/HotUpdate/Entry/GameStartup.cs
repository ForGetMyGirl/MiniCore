using MiniCore.Core;
using MiniCore.Demo.MiniBomber;
using MiniCore.Model;
using MiniCore.Threading;

namespace MiniCore.HotUpdate
{
    /// <summary>
    /// 当前项目唯一的开发者自定义启动入口。
    /// 框架先装配项目启动配置中的服务与模块，再由此处选择运行形态和首个业务流程。
    /// </summary>
    public sealed class GameStartup : AGameStartup
    {
        #region Public 公共成员

        /// <summary>
        /// 进入 MiniBomber 客户端业务流程。
        /// </summary>
        /// <returns>选定业务入口初始化完成任务。</returns>
        public override async MTask StartAsync()
        {
            clientStartup = Global.GetOrAdd<MiniBomberClientStartupComponent>(this);
            await clientStartup.InitializeAsync();
        }

        #endregion

        #region Private 私有成员

        private MiniBomberClientStartupComponent clientStartup; // 普通客户端 Demo 启动组件。

        #endregion

        #region Override 重写实现

        /// <summary>
        /// 释放当前 GameStartup 持有的具体业务启动组件。
        /// </summary>
        protected override void OnDispose()
        {
            clientStartup = null;
            Global.ReleaseAll(this);
            base.OnDispose();
        }

        #endregion
    }
}
