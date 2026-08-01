using System.IO;
using MiniCore.EditorTools;
using NUnit.Framework;
using UnityEngine;

namespace MiniCore.EditorTests
{
    /// <summary>
    /// 验证 MiniCore 开发导航资料的生成与稳定写入行为。
    /// </summary>
    public sealed class MiniCoreDevelopmentNavigationGeneratorTests
    {
        #region Public 公共成员

        /// <summary>
        /// 验证导航生成器输出三份资料，并在内容不变时不重复写入。
        /// </summary>
        [Test]
        public void Generate_CreatesExpectedFilesAndSkipsUnchangedContent()
        {
            Assert.IsTrue(MiniCoreDevelopmentNavigationGenerator.Generate(out string firstSummary), firstSummary);
            string root = Directory.GetParent(Application.dataPath).FullName;
            string directory = Path.Combine(root, ".codex/skills/minicore-development/references/generated");
            string treePath = Path.Combine(directory, "project-tree.generated.md");
            string assemblyPath = Path.Combine(directory, "assembly-dependencies.generated.md");
            string extensionPath = Path.Combine(directory, "extension-points.generated.md");
            Assert.IsTrue(File.Exists(treePath));
            Assert.IsTrue(File.Exists(assemblyPath));
            Assert.IsTrue(File.Exists(extensionPath));
            StringAssert.Contains("Assets/Scripts/MiniCore/Runtime", File.ReadAllText(treePath));
            StringAssert.Contains("MiniCore.Runtime", File.ReadAllText(assemblyPath));
            StringAssert.Contains("AppService", File.ReadAllText(extensionPath));
            Assert.IsTrue(MiniCoreDevelopmentNavigationGenerator.Generate(out string secondSummary), secondSummary);
            StringAssert.Contains("无需更新", secondSummary);
        }

        #endregion
    }
}
