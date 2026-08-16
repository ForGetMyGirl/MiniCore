using MiniCore.Service;
using MiniCore.Threading;
using UnityEngine;

namespace MiniCore.Pooling
{
    /// <summary>
    /// 按资源地址、组件类型和业务分组提供 GameObject 复用能力的按需应用模块。
    /// </summary>
    public interface IGameObjectPool : IAppModule
    {
        /// <summary>
        /// 从指定复合对象池租用实例。
        /// </summary>
        /// <typeparam name="T">预制体上实现池契约的组件类型。</typeparam>
        /// <param name="address">预制体资源地址。</param>
        /// <param name="parent">租用期间使用的父节点。</param>
        /// <param name="group">区分同一预制体不同业务用途的非空分组。</param>
        /// <returns>已初始化并激活的组件实例。</returns>
        MTask<T> RentAsync<T>(string address, Transform parent = null, string group = "Default")
            where T : MonoBehaviour, IPoolObject;

        /// <summary>
        /// 将实例归还到创建它的复合对象池。
        /// </summary>
        /// <param name="instance">不再使用的池对象。</param>
        /// <returns>实例属于当前模块且成功归还时返回 true。</returns>
        bool Return(IPoolObject instance);

        /// <summary>
        /// 将指定复合对象池预热到目标缓存数量。
        /// </summary>
        /// <typeparam name="T">预制体上实现池契约的组件类型。</typeparam>
        /// <param name="address">预制体资源地址。</param>
        /// <param name="count">希望保留的缓存数量。</param>
        /// <param name="group">业务分组。</param>
        /// <param name="maximumRetained">对象池允许保留的最大数量。</param>
        /// <returns>全部预热实例完成创建后的任务。</returns>
        MTask PrewarmAsync<T>(string address, int count, string group = "Default", int maximumRetained = 64)
            where T : MonoBehaviour, IPoolObject;

        /// <summary>
        /// 清空指定复合对象池当前缓存的实例，已经租出的实例保持有效。
        /// </summary>
        /// <typeparam name="T">池对象组件类型。</typeparam>
        /// <param name="address">预制体资源地址。</param>
        /// <param name="group">业务分组。</param>
        /// <returns>找到目标对象池时返回 true。</returns>
        bool Clear<T>(string address, string group = "Default")
            where T : MonoBehaviour, IPoolObject;
    }
}
