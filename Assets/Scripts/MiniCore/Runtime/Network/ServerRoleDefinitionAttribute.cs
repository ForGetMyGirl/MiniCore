using System;

namespace MiniCore.Model
{
    /// <summary>
    /// 为业务 Role 枚举字段提供稳定目录键和发布界面元数据。
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
    public sealed class ServerRoleDefinitionAttribute : Attribute
    {
        #region Public 公共成员

        /// <summary>
        /// 获取跨版本稳定且不可复用的 Role 键。
        /// </summary>
        public string Key { get; }

        /// <summary>
        /// 获取发布界面显示名称。
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// 获取该服务是否允许客户端通过 Coordinator 发现。
        /// </summary>
        public bool ClientDiscoverable { get; set; }

        /// <summary>
        /// 获取或设置客户端生成常量名称；仅 ClientDiscoverable 时使用。
        /// </summary>
        public string PublicName { get; set; } = string.Empty;

        /// <summary>
        /// 创建业务 Role 元数据。
        /// </summary>
        /// <param name="key">稳定 Role 键。</param>
        /// <param name="displayName">显示名称。</param>
        public ServerRoleDefinitionAttribute(string key, string displayName)
        {
            Key = string.IsNullOrWhiteSpace(key) ? throw new ArgumentException("Role 键不能为空。", nameof(key)) : key;
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? throw new ArgumentException("Role 显示名称不能为空。", nameof(displayName)) : displayName;
        }

        #endregion
    }
}
