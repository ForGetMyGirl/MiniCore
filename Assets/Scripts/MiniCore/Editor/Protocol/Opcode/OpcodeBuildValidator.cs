using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace MiniCore.EditorTools
{
    /// <summary>
    /// 在构建前验证 opcode 稳定清单、当前协议和生成映射的一致性。
    /// </summary>
    public sealed class OpcodeBuildValidator : IPreprocessBuildWithReport
    {
        #region Public 公共成员

        /// <summary>
        /// 在 HybridCLR 清理临时 AOT 输出前执行发布产物校验。
        /// </summary>
        public int callbackOrder => -100;

        /// <summary>
        /// 阻止使用过期 opcode 映射的构建继续执行。
        /// </summary>
        /// <param name="report">当前 Unity 构建报告。</param>
        public void OnPreprocessBuild(BuildReport report)
        {
            if (EditorUserBuildSettings.buildScriptsOnly)
            {
                return;
            }

            if (!ProtoBuildValidator.Validate(out string protoError))
            {
                throw new BuildFailedException(protoError);
            }

            if (!OpcodeRegistryGenerator.Validate(out string error))
            {
                throw new BuildFailedException(error);
            }
        }

        #endregion
    }
}
