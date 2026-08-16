using System;
using MiniCore.Model;

namespace MiniCore.Service
{
    /// <summary>
    /// AOT UIService 的启动配置参数。
    /// </summary>
    public sealed class UIServiceInitArgs : ComponentInitArgs
    {
        #region Public 公共成员

        /// <summary>
        /// 获取或设置 UIProjectProfile 的 YooAsset 地址。
        /// </summary>
        public string ProfileAddress { get; set; } = "UIProjectProfile";

        /// <summary>
        /// 使用默认 Profile 地址创建参数。
        /// </summary>
        public UIServiceInitArgs()
        {
        }

        /// <summary>
        /// 使用指定 Profile 地址创建参数。
        /// </summary>
        /// <param name="profileAddress">UIProjectProfile 的 YooAsset 地址。</param>
        public UIServiceInitArgs(string profileAddress)
        {
            if (string.IsNullOrWhiteSpace(profileAddress))
            {
                throw new ArgumentException("UI Profile 地址不能为空。", nameof(profileAddress));
            }

            ProfileAddress = profileAddress;
        }

        #endregion
    }
}
