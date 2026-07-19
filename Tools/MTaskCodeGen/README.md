# MTask Unity 2021.3 CodeGen

这里保存 MTask Owner ILPostProcessor 的源码和可重复构建脚本。源码不放在 `Assets`，因此不会参与项目常规 C# 编译，也不会要求项目声明 Burst、Cecil 或 Unity CompilationPipeline 包依赖。

发布 Unity 2021.3 版本前，在 macOS 上执行：

```bash
bash Tools/MTaskCodeGen/BuildMTaskCodeGen.sh
```

其他 Unity 安装位置通过 `UNITY_EDITOR_ROOT` 指向对应 `Unity.app/Contents`。脚本只引用该编辑器内置的 `Unity.CompilationPipeline.Common.dll` 和 `Unity.Cecil.dll`，生成的 DLL 是 Editor-only 插件，不会进入 Player。

升级至新的 Unity LTS 时，需使用该 LTS 重新生成 DLL，并执行 MTask CodeGen 注入与 Unity 编译验证。
