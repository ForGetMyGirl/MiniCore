using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace MiniCore.EditorTools
{
    /// <summary>
    /// 在 Player 构建前独立同步并校验 HybridCLR、YooAsset 与 Bootstrap 热更新产物。
    /// </summary>
    public sealed class HybridClrProjectBuildPreprocessor : IPreprocessBuildWithReport
    {
        #region Public 公共成员

        /// <summary>
        /// 在普通业务构建校验之前执行热更新产物检查。
        /// </summary>
        public int callbackOrder => -110;

        /// <summary>
        /// 同步 HybridCLR 清单并阻止过期或缺失的多程序集产物进入 Player。
        /// </summary>
        /// <param name="report">当前 Unity 构建报告。</param>
        public void OnPreprocessBuild(BuildReport report)
        {
            HybridClrBuildValidator.EnsureConfigured();
            if (!HybridClrBuildValidator.Validate(out string error))
            {
                throw new BuildFailedException(error);
            }
        }

        #endregion
    }
}
