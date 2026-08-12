using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MiniCore.UI;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.UI;
using YooAsset.Editor;

namespace MiniCore.EditorTools.UI
{

    /// <summary>
    /// 在 Player 构建前阻止过期或非法 UI View Authoring 进入发布产物。
    /// </summary>
    public sealed class UIWindowBuildValidator : IPreprocessBuildWithReport
    {
        #region Public 公共成员

        /// <summary>
        /// 在普通构建校验之前验证 UI 生成产物。
        /// </summary>
        public int callbackOrder => -110;

        /// <summary>
        /// 验证窗口身份、类型、Canvas、Driver 和生成表一致性。
        /// </summary>
        /// <param name="report">当前构建报告。</param>
        public void OnPreprocessBuild(UnityEditor.Build.Reporting.BuildReport report)
        {
            if (!UIWindowRegistryGenerator.Validate(out string error))
            {
                throw new BuildFailedException(error);
            }
        }

        #endregion
    }
}
