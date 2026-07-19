using MiniCore.Model;
using UnityEngine;

namespace MiniCore.Core
{
    /// <summary>
    /// 统一管理指定 Unity 节点及其子节点中 <see cref="IListener"/> 的启动与停止。
    /// </summary>
    [ComponentCatalog("全局监听组件", Description = "集中注册指定节点及其子节点下的 IListener，并批量启动或停止全局监听。")]
    public class GlobalListenerComponent : AComponent
    {
        #region Private 私有成员

        private Transform listenersContent; // 当前已注册监听器所属的根节点。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 注册指定节点及其子节点内的全部监听器，并立即启动它们。
        /// </summary>
        /// <param name="listenersContent">承载待注册监听器的根节点。</param>
        public void RegisterAllListeners(Transform listenersContent)
        {
            this.listenersContent = listenersContent;
            IListener[] listeners = listenersContent.GetComponentsInChildren<IListener>();
            for (int index = 0; index < listeners.Length; index++)
            {
                listeners[index].StartListener();
            }
        }

        /// <summary>
        /// 停止当前已注册根节点及其子节点内的全部监听器。
        /// </summary>
        public void RemoveAllListeners()
        {
            IListener[] listeners = listenersContent.GetComponentsInChildren<IListener>();
            for (int index = 0; index < listeners.Length; index++)
            {
                listeners[index].StopListener();
            }
        }

        #endregion

    }
}
