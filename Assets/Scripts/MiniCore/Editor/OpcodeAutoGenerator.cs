using UnityEditor;
using UnityEngine;

namespace MiniCore.EditorTools
{
    [InitializeOnLoad]
    internal static class OpcodeAutoGenerator
    {
        #region Private 私有成员

        private static bool scheduled; // 防止同一域重载重复安排同步。

        /// <summary>
        /// 在 Unity 域重载后安排一次 opcode 同步。
        /// </summary>
        static OpcodeAutoGenerator()
        {
            ScheduleSynchronization();
        }

        /// <summary>
        /// 执行 ScheduleSynchronization 相关处理。
        /// </summary>
        private static void ScheduleSynchronization()
        {
            if (scheduled)
            {
                return;
            }

            scheduled = true;
            EditorApplication.delayCall += SynchronizeAfterDomainReload;
        }

        /// <summary>
        /// 执行 SynchronizeAfterDomainReload 相关处理。
        /// </summary>
        private static void SynchronizeAfterDomainReload()
        {
            scheduled = false;
            if (EditorApplication.isCompiling || BuildPipeline.isBuildingPlayer)
            {
                ScheduleSynchronization();
                return;
            }

            if (!OpcodeRegistryGenerator.Synchronize(true, out string log))
            {
                Debug.LogWarning(log);
            }
        }

        #endregion
    }
}
