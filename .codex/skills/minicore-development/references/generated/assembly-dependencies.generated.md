# 程序集依赖

> 自动生成，勿手改。结构变动后在 Unity 点击 `MiniCore/AI/Generate Development Navigation` 更新。

## MiniCore.HotUpdateAssembly.Editor
- 路径：`Assets/Scripts/MiniCore/Editor/HotUpdateAssembly/MiniCore.HotUpdateAssembly.Editor.asmdef`
- 引用：无
- 平台：`Editor`
- noEngineReferences：`False`；autoReferenced：`False`

## MiniCore.Editor
- 路径：`Assets/Scripts/MiniCore/Editor/MiniCore.Editor.asmdef`
- 引用：`MiniCore.Runtime`, `MiniCore.Protocol`, `MiniCore.Network`, `MiniCore.Unity`, `MiniCore.HotUpdate`, `MiniCore.HotUpdateAssembly.Editor`, `Unity.InputSystem`, `HybridCLR.Editor`, `YooAsset`, `YooAsset.Editor`
- 平台：`Editor`
- noEngineReferences：`False`；autoReferenced：`True`

## MiniCore.Performance.Editor
- 路径：`Assets/Scripts/MiniCore/Editor/Performance/MiniCore.Performance.Editor.asmdef`
- 引用：`Unity.PerformanceTesting`, `UnityEditor.TestRunner`, `UnityEngine.TestRunner`
- 平台：`Editor`
- noEngineReferences：`False`；autoReferenced：`True`

## MiniCore.Protocol.Editor
- 路径：`Assets/Scripts/MiniCore/Editor/Protocol/MiniCore.Protocol.Editor.asmdef`
- 引用：`MiniCore.HotUpdateAssembly.Editor`
- 平台：`Editor`
- noEngineReferences：`False`；autoReferenced：`False`

## MiniCore.Protocol.Handler.Editor
- 路径：`Assets/Scripts/MiniCore/Editor/Protocol/Opcode/MiniCore.Protocol.Handler.Editor.asmdef`
- 引用：`MiniCore.Protocol.Editor`, `MiniCore.HotUpdateAssembly.Editor`, `MiniCore.Runtime`, `MiniCore.Network`, `MiniCore.HotUpdate`
- 平台：`Editor`
- noEngineReferences：`False`；autoReferenced：`False`

## MiniCore.HotUpdate
- 路径：`Assets/Scripts/MiniCore/HotUpdate/MiniCore.HotUpdate.asmdef`
- 引用：`MiniCore.Runtime`, `MiniCore.Protocol`, `MiniCore.Serialization`, `MiniCore.Network`, `MiniCore.Unity`, `Unity.InputSystem`, `GUID:e34a5702dd353724aa315fb8011f08c3`, `GUID:6055be8ebefd69e48b49212b09b47b2f`
- 平台：无
- noEngineReferences：`False`；autoReferenced：`False`

## MiniCore.Network
- 路径：`Assets/Scripts/MiniCore/Network/MiniCore.Network.asmdef`
- 引用：`MiniCore.Runtime`, `MiniCore.Serialization`
- 平台：无
- noEngineReferences：`True`；autoReferenced：`True`

## MiniCore.Platform.Browser
- 路径：`Assets/Scripts/MiniCore/Platform/Browser/MiniCore.Platform.Browser.asmdef`
- 引用：`MiniCore.Runtime`, `MiniCore.Network`
- 平台：`WebGL`
- noEngineReferences：`False`；autoReferenced：`True`

## MiniCore.Protocol
- 路径：`Assets/Scripts/MiniCore/Protocol/MiniCore.Protocol.asmdef`
- 引用：`MiniCore.Runtime`, `MiniCore.Serialization`, `MiniCore.Network`
- 平台：无
- noEngineReferences：`True`；autoReferenced：`False`

## MiniCore.Runtime
- 路径：`Assets/Scripts/MiniCore/Runtime/MiniCore.Runtime.asmdef`
- 引用：无
- 平台：无
- noEngineReferences：`True`；autoReferenced：`True`

## MiniCore.Serialization
- 路径：`Assets/Scripts/MiniCore/Serialization/MiniCore.Serialization.asmdef`
- 引用：`MiniCore.Runtime`
- 平台：无
- noEngineReferences：`True`；autoReferenced：`True`

## MiniCore.Unity
- 路径：`Assets/Scripts/MiniCore/Unity/MiniCore.Unity.asmdef`
- 引用：`MiniCore.Runtime`, `MiniCore.Serialization`, `Unity.InputSystem`, `GUID:e34a5702dd353724aa315fb8011f08c3`, `GUID:6055be8ebefd69e48b49212b09b47b2f`
- 平台：无
- noEngineReferences：`False`；autoReferenced：`True`

## Project.Bootstrap
- 路径：`Assets/Scripts/Project/Bootstrap/Project.Bootstrap.asmdef`
- 引用：`MiniCore.Runtime`, `MiniCore.Unity`, `HybridCLR.Runtime`, `GUID:e34a5702dd353724aa315fb8011f08c3`
- 平台：无
- noEngineReferences：`False`；autoReferenced：`True`

## MiniCore.EditorTests
- 路径：`Assets/Tests/Editor/MiniCore.EditorTests.asmdef`
- 引用：`MiniCore.Runtime`, `MiniCore.Protocol`, `MiniCore.Serialization`, `MiniCore.Network`, `MiniCore.Unity`, `MiniCore.HotUpdate`, `MiniCore.Editor`, `Unity.PerformanceTesting`
- 平台：`Editor`
- noEngineReferences：`False`；autoReferenced：`False`

## MiniCore.UI.PlayModeTests
- 路径：`Assets/Tests/PlayMode/MiniCore.UI.PlayModeTests.asmdef`
- 引用：`MiniCore.Runtime`, `MiniCore.Unity`
- 平台：无
- noEngineReferences：`False`；autoReferenced：`False`

