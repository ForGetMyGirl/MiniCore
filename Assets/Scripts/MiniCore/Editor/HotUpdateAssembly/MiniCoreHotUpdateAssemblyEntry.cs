using System;
using UnityEngine;

namespace MiniCore.EditorTools
{
    /// <summary>
    /// 描述一个由 HybridCLR 编译、YooAsset 分发并由 Bootstrap 按序加载的程序集。
    /// </summary>
    [Serializable]
    public sealed class MiniCoreHotUpdateAssemblyEntry
    {
        #region Private 私有成员

        [SerializeField] private string assemblyName; // 不含 DLL 后缀的程序集名称。
        [SerializeField] private string assemblyDefinitionPath; // Assets 下对应 asmdef 路径。
        [SerializeField] private int loadOrder; // 依赖优先的稳定加载顺序。
        [SerializeField] private bool isStartup; // 是否由 Bootstrap 调用入口。
        [SerializeField] private string startupTypeName; // 启动类型完整名称。
        [SerializeField] private string startupMethodName; // 启动静态方法名称。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 获取不含 DLL 后缀的程序集名称。
        /// </summary>
        public string AssemblyName => assemblyName;

        /// <summary>
        /// 获取 Assets 下对应 asmdef 的项目相对路径。
        /// </summary>
        public string AssemblyDefinitionPath => assemblyDefinitionPath;

        /// <summary>
        /// 获取依赖优先的稳定加载顺序，数值较小的程序集先加载。
        /// </summary>
        public int LoadOrder => loadOrder;

        /// <summary>
        /// 获取该程序集是否包含 Bootstrap 最终调用的启动入口。
        /// </summary>
        public bool IsStartup => isStartup;

        /// <summary>
        /// 获取启动类型完整名称；非启动程序集允许为空。
        /// </summary>
        public string StartupTypeName => startupTypeName;

        /// <summary>
        /// 获取启动静态方法名称；非启动程序集允许为空。
        /// </summary>
        public string StartupMethodName => startupMethodName;

        /// <summary>
        /// 创建一条热更新程序集登记记录。
        /// </summary>
        /// <param name="assemblyName">不含 DLL 后缀的程序集名称。</param>
        /// <param name="assemblyDefinitionPath">Assets 下对应 asmdef 路径。</param>
        /// <param name="loadOrder">依赖优先的稳定加载顺序。</param>
        /// <param name="isStartup">是否包含最终启动入口。</param>
        /// <param name="startupTypeName">启动类型完整名称。</param>
        /// <param name="startupMethodName">启动静态方法名称。</param>
        public MiniCoreHotUpdateAssemblyEntry(
            string assemblyName,
            string assemblyDefinitionPath,
            int loadOrder,
            bool isStartup = false,
            string startupTypeName = null,
            string startupMethodName = null)
        {
            this.assemblyName = assemblyName;
            this.assemblyDefinitionPath = assemblyDefinitionPath;
            this.loadOrder = loadOrder;
            this.isStartup = isStartup;
            this.startupTypeName = startupTypeName;
            this.startupMethodName = startupMethodName;
        }

        #endregion
    }
}
