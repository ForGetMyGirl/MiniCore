using System.Threading.Tasks;
using MiniCore.Core;

namespace MiniCore.Model
{
    /// <summary>
    /// 项目自定义启动入口的基础组件。
    /// 生成启动代码会在选定模块完成 Pin 后创建 GameStartup，并只调用一次 StartAsync。
    /// </summary>
    public abstract class AGameStartup : AComponent
    {
        #region Public 公共成员

        /// <summary>
        /// 执行项目自己的启动逻辑。
        /// 需要全局组件时应通过 Global.Get 获取，基类 Dispose 会统一释放该实例持有的引用。
        /// </summary>
        /// <returns>项目启动完成任务。</returns>
        public abstract Task StartAsync();

        #endregion

        #region Override 重写实现

        /// <summary>
        /// 释放项目启动入口持有的全部全局组件引用。
        /// </summary>
        public override void Dispose()
        {
            Global.ReleaseAll(this);
            base.Dispose();
        }

        #endregion
    }
}
