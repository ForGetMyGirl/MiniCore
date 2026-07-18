using System;
using MiniCore.Model;

namespace MiniCore.HotUpdate
{
    /// <summary>
    /// YooAsset 资源组件的初始化参数。
    /// 包名只在组件首次创建时使用，后续获取已有组件时不会重新应用。
    /// </summary>
    public sealed class YooAssetResourceComponentInitArgs : ComponentInitArgs
    {
        #region Public 公共成员

        /// <summary>
        /// 获取或设置 YooAsset 资源包名称。
        /// 留空时组件初始化会明确报错，避免在运行时静默绑定到错误资源包。
        /// </summary>
        public string PackageName { get; set; } = "DefaultPackage";

        /// <summary>
        /// 使用代码默认值创建初始化参数。
        /// 该无参构造函数供项目启动配置生成器使用。
        /// </summary>
        public YooAssetResourceComponentInitArgs()
        {
        }

        /// <summary>
        /// 使用资源包名称创建初始化参数。
        /// </summary>
        /// <param name="packageName">YooAsset 中已注册的资源包名称。</param>
        public YooAssetResourceComponentInitArgs(string packageName)
        {
            if (string.IsNullOrWhiteSpace(packageName))
            {
                throw new ArgumentException("资源包名称不能为空。", nameof(packageName));
            }

            PackageName = packageName;
        }

        #endregion
    }
}
