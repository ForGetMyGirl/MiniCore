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
        /// 获取 YooAsset 资源包名称。
        /// </summary>
        public string PackageName { get; }

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
