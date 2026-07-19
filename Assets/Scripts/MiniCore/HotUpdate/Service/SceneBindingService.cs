using MiniCore.Model;
using UnityEngine;

namespace MiniCore.Service
{
    /// <summary>
    /// 延迟查找并缓存约定场景节点的服务。
    /// 为资产与 UI 服务提供 Canvas 和对象池根节点绑定。
    /// </summary>
    [AppService("场景绑定", typeof(ISceneBindingService), Description = "提供场景中的 UI Canvas 和对象池根节点绑定。")]
    public sealed class SceneBindingService : AAppService, ISceneBindingService
    {
        #region Private 私有成员

        private Transform preloadPool; // 预加载对象池根节点。
        private Transform mainCanvas; // 主 UI 画布。
        private Transform popupWindowCanvas; // 弹窗 UI 画布。
        private Transform topCanvas; // 顶层 UI 画布。
        private Transform usefulPoolObjects; // 可复用对象池根节点。
        private Transform errorCodeCanvas; // 错误码 UI 画布。
        private Transform bottomCanvas; // 底层 UI 画布。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 获取预加载对象池根节点。
        /// </summary>
        public Transform PreloadPool => preloadPool = preloadPool ?? GameObject.FindGameObjectWithTag("PreloadGameObjects_Pool").transform;

        /// <summary>
        /// 获取主 UI 画布节点。
        /// </summary>
        public Transform MainCanvas => mainCanvas = mainCanvas ?? GameObject.FindGameObjectWithTag("MainCanvas").transform;

        /// <summary>
        /// 获取弹窗 UI 画布节点。
        /// </summary>
        public Transform PopupWindowCanvas => popupWindowCanvas = popupWindowCanvas ?? GameObject.FindGameObjectWithTag("PopupWindowCanvas").transform;

        /// <summary>
        /// 获取顶层 UI 画布节点。
        /// </summary>
        public Transform TopCanvas => topCanvas = topCanvas ?? GameObject.FindGameObjectWithTag("TopCanvas").transform;

        /// <summary>
        /// 获取可复用对象池根节点。
        /// </summary>
        public Transform UsefulPoolObjects => usefulPoolObjects = usefulPoolObjects ?? GameObject.FindGameObjectWithTag("UsefulPoolObjects").transform;

        /// <summary>
        /// 获取错误码 UI 画布节点。
        /// </summary>
        public Transform ErrorCodeCanvas => errorCodeCanvas = errorCodeCanvas ?? GameObject.FindGameObjectWithTag("ErrorCodeCanvas").transform;

        /// <summary>
        /// 获取底层 UI 画布节点。
        /// </summary>
        public Transform BottomCanvas => bottomCanvas = bottomCanvas ?? GameObject.FindGameObjectWithTag("BottomCanvas").transform;

        #endregion
    }
}
