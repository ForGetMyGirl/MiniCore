using MiniCore.Core;
using MiniCore.Model;
using UnityEngine;

namespace MiniCore.Unity
{
    /// <summary>
    /// Unity 生命周期到纯 C# Global Runtime 的唯一适配入口。
    /// </summary>
    public sealed class UnityGlobalDriver : MonoBehaviour
    {
        #region Private 私有成员

        private static UnityGlobalDriver instance; // 当前持久化 Driver 实例。

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 初始化 Runtime，并保证仅保留一个跨场景 Driver。
        /// </summary>
        private void Awake()
        {
            if (instance != null && !ReferenceEquals(instance, this))
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            Global.Initialize(new UnityTimeProvider());
            LogSwitch.SetSink(new UnityLogSink());
        }

        /// <summary>
        /// 驱动当前激活组件的一次 Runtime Tick。
        /// </summary>
        private void Update()
        {
            Global.Tick();
        }

        /// <summary>
        /// 在应用退出时释放全部全局组件。
        /// </summary>
        private void OnApplicationQuit()
        {
            Global.Shutdown();
        }

        /// <summary>
        /// 在 Driver 销毁时清理静态实例引用。
        /// </summary>
        private void OnDestroy()
        {
            if (ReferenceEquals(instance, this))
            {
                instance = null;
            }
        }

        #endregion
    }
}
