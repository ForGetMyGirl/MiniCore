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
        [SerializeField] private HotUpdateAssemblyRuntimeTargets runtimeTargets = HotUpdateAssemblyRuntimeTargets.All; // 会包含该程序集的运行目标。
        [SerializeField] private HotUpdateAssemblyRuntimeTargets startupRuntimeTargets; // 会调用该程序集启动入口的运行目标。

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
        public bool IsStartup => startupRuntimeTargets != HotUpdateAssemblyRuntimeTargets.None || isStartup;

        /// <summary>
        /// 获取会包含该程序集的运行目标。
        /// </summary>
        public HotUpdateAssemblyRuntimeTargets RuntimeTargets => runtimeTargets;

        /// <summary>
        /// 获取启动类型完整名称；非启动程序集允许为空。
        /// </summary>
        public string StartupTypeName => startupTypeName;

        /// <summary>
        /// 获取启动静态方法名称；非启动程序集允许为空。
        /// </summary>
        public string StartupMethodName => startupMethodName;

        /// <summary>
        /// 判断程序集是否进入指定运行目标。
        /// </summary>
        public bool Supports(HotUpdateAssemblyRuntimeTargets target)
        {
            return (runtimeTargets & target) != 0;
        }

        /// <summary>
        /// 判断 Bootstrap 是否应在指定运行目标调用此入口。
        /// </summary>
        public bool IsStartupFor(HotUpdateAssemblyRuntimeTargets target)
        {
            HotUpdateAssemblyRuntimeTargets targets = startupRuntimeTargets != HotUpdateAssemblyRuntimeTargets.None
                ? startupRuntimeTargets
                : isStartup ? runtimeTargets : HotUpdateAssemblyRuntimeTargets.None;
            return (targets & target) != 0;
        }

        /// <summary>
        /// 创建一条热更新程序集登记记录。
        /// </summary>
        /// <param name="assemblyName">不含 DLL 后缀的程序集名称。</param>
        /// <param name="assemblyDefinitionPath">Assets 下对应 asmdef 路径。</param>
        /// <param name="loadOrder">依赖优先的稳定加载顺序。</param>
        /// <param name="isStartup">是否包含最终启动入口。</param>
        /// <param name="startupTypeName">启动类型完整名称。</param>
        /// <param name="startupMethodName">启动静态方法名称。</param>
        /// <param name="runtimeTargets">会包含该程序集的运行目标。</param>
        /// <param name="startupRuntimeTargets">会调用该程序集入口的运行目标。</param>
        public MiniCoreHotUpdateAssemblyEntry(
            string assemblyName,
            string assemblyDefinitionPath,
            int loadOrder,
            bool isStartup = false,
            string startupTypeName = null,
            string startupMethodName = null,
            HotUpdateAssemblyRuntimeTargets runtimeTargets = HotUpdateAssemblyRuntimeTargets.All,
            HotUpdateAssemblyRuntimeTargets startupRuntimeTargets = HotUpdateAssemblyRuntimeTargets.None)
        {
            this.assemblyName = assemblyName;
            this.assemblyDefinitionPath = assemblyDefinitionPath;
            this.loadOrder = loadOrder;
            this.isStartup = isStartup;
            this.startupTypeName = startupTypeName;
            this.startupMethodName = startupMethodName;
            this.runtimeTargets = runtimeTargets;
            this.startupRuntimeTargets = startupRuntimeTargets == HotUpdateAssemblyRuntimeTargets.None && isStartup
                ? runtimeTargets
                : startupRuntimeTargets;
        }

        #endregion
    }
}
