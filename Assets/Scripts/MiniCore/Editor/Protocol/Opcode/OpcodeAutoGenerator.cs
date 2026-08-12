using UnityEditor;
using UnityEngine;

namespace MiniCore.EditorTools
{
    [InitializeOnLoad]
    internal static class OpcodeAutoGenerator
    {
        #region Private 私有成员

        private const string PendingProtocolSynchronizationKey = "MiniCore.Protocol.PendingOpcodeSynchronization";
        private static bool scheduled; // 防止同一域重载重复安排同步。

        /// <summary>
        /// 在 Unity 域重载后安排一次 opcode 同步。
        /// </summary>
        static OpcodeAutoGenerator()
        {
            ScheduleSynchronization();
        }

        #endregion

        #region Internal 内部成员

        /// <summary>
        /// 标记协议代码已重新生成，并安排在 Unity 编译完成后同步 Opcode 与 Handler 表。
        /// </summary>
        internal static void RequestSynchronization()
        {
            SessionState.SetBool(PendingProtocolSynchronizationKey, true);
            ScheduleSynchronization();
        }

        #endregion

        #region Private 私有成员

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

            bool pendingProtocolSynchronization = SessionState.GetBool(PendingProtocolSynchronizationKey, false);
            if (!OpcodeRegistryGenerator.Synchronize(true, out string log))
            {
                Debug.LogWarning(log);
                return;
            }

            if (pendingProtocolSynchronization)
            {
                SessionState.SetBool(PendingProtocolSynchronizationKey, false);
                Debug.Log($"协议一键生成完成。\n{log}");
            }
        }

        #endregion
    }
}
