using UnityEngine;

namespace MiniCore.Unity
{
    /// <summary>
    /// 在首个场景加载前创建全局 Runtime Driver，避免场景手工挂载依赖。
    /// </summary>
    internal static class UnityGlobalBootstrap
    {
        /// <summary>
        /// 创建由 Unity 生命周期托管的 Global Driver。
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CreateDriver()
        {
            GameObject driverObject = new GameObject("MiniCore.UnityGlobalDriver");
            driverObject.AddComponent<UnityGlobalDriver>();
        }
    }
}
