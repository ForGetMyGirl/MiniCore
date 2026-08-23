using UnityEditor;
using UnityEngine;
using MiniCore.EditorTools.UI;

namespace MiniCore.EditorTools.Deploy
{
    /// <summary>
    /// 将现有协议、启动、Handler 和 UI 生成器组成可分阶段调用的 BatchMode 入口。
    /// </summary>
    public static class MiniCoreDeployGenerationCommand
    {
        #region Public 公共成员

        /// <summary>
        /// 第一阶段生成协议、启动和 UI 注册源码，完成后让 Unity 正常退出并重新编译。
        /// </summary>
        public static void GenerateSources()
        {
            ServerRoleCatalogGenerator.Generate();
            ProtoCodeGenerator.GenerateFromCommandLine();
            MiniCoreStartupCodeGenerator.GenerateFromCommandLine();
            UIWindowRegistryGenerator.Generate();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Debug.Log("MiniCore Deploy 第一阶段源码生成完成。");
        }

        /// <summary>
        /// 第二阶段在新脚本域中扫描 Handler 并生成直接注册代码。
        /// </summary>
        public static void GenerateHandlers()
        {
            OpcodeRegistryGenerator.SynchronizeFromCommandLine();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Debug.Log("MiniCore Deploy 第二阶段 Handler 生成完成。");
        }

        #endregion
    }
}
