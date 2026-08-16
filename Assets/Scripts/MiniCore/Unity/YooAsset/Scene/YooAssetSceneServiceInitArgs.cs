using MiniCore.Model;

namespace MiniCore.Service
{
    /// <summary>
    /// YooAsset 场景服务的初始化参数。
    /// </summary>
    public sealed class YooAssetSceneServiceInitArgs : ComponentInitArgs
    {
        #region Public 公共成员

        /// <summary>
        /// 场景所属资源包名称。
        /// </summary>
        public string PackageName { get; set; } = "DefaultPackage";

        #endregion
    }
}
