using System;
using MiniCore.Threading;
using UnityEngine;

namespace MiniCore.Unity
{
    /// <summary>
    /// 将 MonoBehaviour 生命周期绑定到 MTask 任务域的 Unity 基类。
    /// </summary>
    public abstract class AMTaskBehaviour : MonoBehaviour, IMTaskOwner
    {
        #region Private 私有成员

        private MTaskDomain mTaskDomain; // 首次启动 MTask 时延迟创建的对象生命周期域。
        private bool taskOwnerDestroyed; // 是否已经销毁任务域。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 获取与当前 Unity 对象生命周期绑定的 MTask 域。
        /// </summary>
        /// <returns>当前对象的任务域。</returns>
        public virtual MTaskDomain GetMTaskDomain()
        {
            if (taskOwnerDestroyed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }

            if (mTaskDomain == null)
            {
                mTaskDomain = new MTaskDomain(GetType().FullName, MTaskExecutors.Unity);
            }

            return mTaskDomain;
        }

        #endregion

        #region Protected 受保护成员

        /// <summary>
        /// 取消当前 Unity 对象的全部 MTask。
        /// 派生类重写 OnDestroy 时必须调用此基类实现。
        /// </summary>
        protected virtual void OnDestroy()
        {
            if (taskOwnerDestroyed)
            {
                return;
            }

            taskOwnerDestroyed = true;
            mTaskDomain?.Dispose();
            mTaskDomain = null;
        }

        /// <summary>
        /// 在当前 Unity 对象 Owner 上下文中启动一个同步入口。
        /// </summary>
        /// <returns>离开入口时自动恢复的上下文令牌。</returns>
        protected MTaskOwnerContext EnterMTaskOwner()
        {
            return MTaskRuntime.EnterOwner(this);
        }

        #endregion
    }
}
