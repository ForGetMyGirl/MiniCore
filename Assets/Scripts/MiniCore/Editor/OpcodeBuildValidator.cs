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
        /// 在其他默认构建预处理器之前执行 opcode 校验。
        /// </summary>
        public int callbackOrder => 0;

        /// <summary>
        /// 阻止使用过期 opcode 映射的构建继续执行。
        /// </summary>
        /// <param name="report">当前 Unity 构建报告。</param>
        public void OnPreprocessBuild(BuildReport report)
        {
            if (!OpcodeRegistryGenerator.Validate(out string error))
            {
                throw new BuildFailedException(error);
            }
        }

        #endregion
    }
}
