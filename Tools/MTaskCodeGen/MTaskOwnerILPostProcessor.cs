using System;
using System.Collections.Generic;
using System.IO;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Unity.CompilationPipeline.Common.ILPostProcessing;

namespace MiniCore.Editor.Threading
{
    /// <summary>
    /// 为 MTask Owner 的异步入口注入无感 Owner 上下文。
    /// </summary>
    internal sealed class MTaskOwnerILPostProcessor : ILPostProcessor
    {
        #region Private 私有成员

        private const string OwnerInterfaceName = "MiniCore.Threading.IMTaskOwner"; // MTask Owner 接口完整名称。
        private const string OwnerAttributeName = "MiniCore.Threading.MTaskOwnerAttribute"; // Owner 标记完整名称。
        private const string MTaskName = "MiniCore.Threading.MTask"; // 无返回值任务完整名称。
        private const string MTaskGenericName = "MiniCore.Threading.MTask`1"; // 带返回值任务完整名称。
        private const string RuntimeName = "MiniCore.Threading.MTaskRuntime"; // 运行时上下文类型完整名称。
        private const string OwnerContextName = "MiniCore.Threading.MTaskOwnerContext"; // Owner 恢复令牌完整名称。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 创建供 Unity 编译管线独立调用的后处理器实例。
        /// </summary>
        /// <returns>新的后处理器实例。</returns>
        public override ILPostProcessor GetInstance()
        {
            return new MTaskOwnerILPostProcessor();
        }

        /// <summary>
        /// 判断当前程序集是否可能包含 MiniCore MTask Owner。
        /// </summary>
        /// <param name="compiledAssembly">Unity 刚编译完成的程序集。</param>
        /// <returns>需要扫描时返回 true。</returns>
        public override bool WillProcess(ICompiledAssembly compiledAssembly)
        {
            string name = compiledAssembly?.Name;
            if (string.IsNullOrEmpty(name) || string.Equals(name, "MiniCore.MTask.CodeGen", StringComparison.Ordinal))
            {
                return false;
            }

            return name.StartsWith("MiniCore.", StringComparison.Ordinal)
                || name.StartsWith("Project.", StringComparison.Ordinal)
                || name.StartsWith("Assembly-CSharp", StringComparison.Ordinal);
        }

        /// <summary>
        /// 扫描 Owner 方法并注入 EnterOwner/Dispose try-finally。
        /// </summary>
        /// <param name="compiledAssembly">Unity 刚编译完成的程序集。</param>
        /// <returns>未修改时返回 null；修改后返回新的程序集字节。</returns>
        public override ILPostProcessResult Process(ICompiledAssembly compiledAssembly)
        {
            if (!WillProcess(compiledAssembly))
            {
                return null;
            }

            DefaultAssemblyResolver resolver = CreateResolver(compiledAssembly);
            bool readSymbols = compiledAssembly.InMemoryAssembly.PdbData != null
                && compiledAssembly.InMemoryAssembly.PdbData.Length > 0;
            ReaderParameters readerParameters = new ReaderParameters
            {
                AssemblyResolver = resolver,
                ReadSymbols = readSymbols,
                SymbolReaderProvider = readSymbols ? new PortablePdbReaderProvider() : null
            };

            using MemoryStream peStream = new MemoryStream(compiledAssembly.InMemoryAssembly.PeData, false);
            using MemoryStream pdbStream = readerParameters.ReadSymbols
                ? new MemoryStream(compiledAssembly.InMemoryAssembly.PdbData, false)
                : null;
            readerParameters.SymbolStream = pdbStream;

            using AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(peStream, readerParameters);
            ModuleDefinition module = assembly.MainModule;
            MethodReference enterOwner = FindMethod(module, RuntimeName, "EnterOwner", "System.Object");
            MethodReference disposeContext = FindMethod(module, OwnerContextName, "Dispose", 0);
            MethodReference disposeOwner = FindMethod(module, RuntimeName, "DisposeOwner", "System.Object");
            if (enterOwner == null || disposeContext == null)
            {
                return null;
            }

            bool changed = false;
            for (int i = 0; i < module.Types.Count; i++)
            {
                changed |= ProcessType(module.Types[i], module, enterOwner, disposeContext, disposeOwner);
            }

            if (!changed)
            {
                return null;
            }

            using MemoryStream outputPe = new MemoryStream();
            using MemoryStream outputPdb = readerParameters.ReadSymbols ? new MemoryStream() : null;
            WriterParameters writerParameters = new WriterParameters
            {
                WriteSymbols = readerParameters.ReadSymbols,
                SymbolWriterProvider = readerParameters.ReadSymbols ? new PortablePdbWriterProvider() : null,
                SymbolStream = outputPdb
            };
            assembly.Write(outputPe, writerParameters);
            return new ILPostProcessResult(new InMemoryAssembly(
                outputPe.ToArray(),
                outputPdb?.ToArray() ?? Array.Empty<byte>()));
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 为目标程序集引用目录创建 Cecil 解析器。
        /// </summary>
        /// <param name="compiledAssembly">目标程序集。</param>
        /// <returns>可解析项目依赖的 Cecil Resolver。</returns>
        private static DefaultAssemblyResolver CreateResolver(ICompiledAssembly compiledAssembly)
        {
            DefaultAssemblyResolver resolver = new DefaultAssemblyResolver();
            HashSet<string> directories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string[] references = compiledAssembly.References;
            for (int i = 0; i < references.Length; i++)
            {
                string directory = Path.GetDirectoryName(references[i]);
                if (!string.IsNullOrEmpty(directory) && directories.Add(directory))
                {
                    resolver.AddSearchDirectory(directory);
                }
            }

            return resolver;
        }

        /// <summary>
        /// 递归处理类型及其嵌套类型。
        /// </summary>
        /// <param name="type">待扫描类型。</param>
        /// <param name="module">目标 Cecil 模块。</param>
        /// <param name="enterOwner">EnterOwner 方法引用。</param>
        /// <param name="disposeContext">OwnerContext.Dispose 方法引用。</param>
        /// <param name="disposeOwner">外部 Owner 销毁方法引用。</param>
        /// <returns>至少修改一个方法时返回 true。</returns>
        private static bool ProcessType(
            TypeDefinition type,
            ModuleDefinition module,
            MethodReference enterOwner,
            MethodReference disposeContext,
            MethodReference disposeOwner)
        {
            bool changed = false;
            if (IsOwner(type))
            {
                for (int i = 0; i < type.Methods.Count; i++)
                {
                    MethodDefinition method = type.Methods[i];
                    if (ShouldWrap(method))
                    {
                        WrapMethod(method, module, enterOwner, disposeContext);
                        changed = true;
                    }
                }
            }

            if (disposeOwner != null && HasOwnerAttribute(type))
            {
                InjectOwnerDisposal(type, module, disposeOwner);
                changed = true;
            }

            for (int i = 0; i < type.NestedTypes.Count; i++)
            {
                changed |= ProcessType(type.NestedTypes[i], module, enterOwner, disposeContext, disposeOwner);
            }

            return changed;
        }

        /// <summary>
        /// 判断类型是否实现 IMTaskOwner 或声明 Owner 特性。
        /// </summary>
        /// <param name="type">待判断类型。</param>
        /// <returns>类型具有 Owner 生命周期时返回 true。</returns>
        private static bool IsOwner(TypeDefinition type)
        {
            TypeDefinition current = type;
            while (current != null)
            {
                for (int i = 0; i < current.Interfaces.Count; i++)
                {
                    if (string.Equals(current.Interfaces[i].InterfaceType.FullName, OwnerInterfaceName, StringComparison.Ordinal))
                    {
                        return true;
                    }
                }

                for (int i = 0; i < current.CustomAttributes.Count; i++)
                {
                    if (string.Equals(current.CustomAttributes[i].AttributeType.FullName, OwnerAttributeName, StringComparison.Ordinal))
                    {
                        return true;
                    }
                }

                try
                {
                    current = current.BaseType?.Resolve();
                }
                catch
                {
                    current = null;
                }
            }

            return false;
        }

        /// <summary>
        /// 判断当前类型或其基类是否声明 MTaskOwnerAttribute。
        /// </summary>
        /// <param name="type">待判断类型。</param>
        /// <returns>存在 Owner 特性时返回 true。</returns>
        private static bool HasOwnerAttribute(TypeDefinition type)
        {
            TypeDefinition current = type;
            while (current != null)
            {
                for (int i = 0; i < current.CustomAttributes.Count; i++)
                {
                    if (string.Equals(current.CustomAttributes[i].AttributeType.FullName, OwnerAttributeName, StringComparison.Ordinal))
                    {
                        return true;
                    }
                }

                try
                {
                    current = current.BaseType?.Resolve();
                }
                catch
                {
                    current = null;
                }
            }

            return false;
        }

        /// <summary>
        /// 判断方法是否是需要绑定 Owner 的 MTask 入口。
        /// </summary>
        /// <param name="method">待判断方法。</param>
        /// <returns>需要注入上下文时返回 true。</returns>
        private static bool ShouldWrap(MethodDefinition method)
        {
            if (method.IsStatic || method.IsAbstract || method.IsConstructor || !method.HasBody || method.Body.Instructions.Count == 0)
            {
                return false;
            }

            if (IsMTask(method.ReturnType))
            {
                return true;
            }

            for (int i = 0; i < method.Body.Instructions.Count; i++)
            {
                Instruction instruction = method.Body.Instructions[i];
                if ((instruction.OpCode == OpCodes.Call || instruction.OpCode == OpCodes.Callvirt)
                    && instruction.Operand is MethodReference called
                    && IsMTask(called.ReturnType))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 判断返回类型是否为 MTask 或 MTask&lt;T&gt;。
        /// </summary>
        /// <param name="type">待判断返回类型。</param>
        /// <returns>属于 MTask 类型时返回 true。</returns>
        private static bool IsMTask(TypeReference type)
        {
            return string.Equals(type.FullName, MTaskName, StringComparison.Ordinal)
                || string.Equals(type.GetElementType().FullName, MTaskGenericName, StringComparison.Ordinal);
        }

        /// <summary>
        /// 为方法体添加 Owner 上下文的 try-finally 恢复逻辑。
        /// </summary>
        /// <param name="method">待修改方法。</param>
        /// <param name="module">目标 Cecil 模块。</param>
        /// <param name="enterOwner">EnterOwner 方法。</param>
        /// <param name="disposeContext">OwnerContext.Dispose 方法。</param>
        private static void WrapMethod(MethodDefinition method, ModuleDefinition module, MethodReference enterOwner, MethodReference disposeContext)
        {
            MethodBody body = method.Body;
            body.InitLocals = true;
            ILProcessor il = body.GetILProcessor();
            Instruction originalFirst = body.Instructions[0];
            VariableDefinition contextVariable = new VariableDefinition(module.ImportReference(enterOwner.ReturnType));
            body.Variables.Add(contextVariable);

            VariableDefinition resultVariable = null;
            bool hasResult = method.ReturnType.MetadataType != MetadataType.Void;
            if (hasResult)
            {
                resultVariable = new VariableDefinition(module.ImportReference(method.ReturnType));
                body.Variables.Add(resultVariable);
            }

            Instruction loadOwner = il.Create(OpCodes.Ldarg_0);
            Instruction callEnter = il.Create(OpCodes.Call, module.ImportReference(enterOwner));
            Instruction storeContext = il.Create(OpCodes.Stloc, contextVariable);
            il.InsertBefore(originalFirst, loadOwner);
            il.InsertAfter(loadOwner, callEnter);
            il.InsertAfter(callEnter, storeContext);

            Instruction finallyStart = il.Create(OpCodes.Ldloca, contextVariable);
            Instruction callDispose = il.Create(OpCodes.Call, module.ImportReference(disposeContext));
            Instruction endFinally = il.Create(OpCodes.Endfinally);
            Instruction returnTarget = hasResult ? il.Create(OpCodes.Ldloc, resultVariable) : il.Create(OpCodes.Nop);
            Instruction finalReturn = il.Create(OpCodes.Ret);

            List<Instruction> returns = new List<Instruction>();
            for (int i = 0; i < body.Instructions.Count; i++)
            {
                if (body.Instructions[i].OpCode == OpCodes.Ret)
                {
                    returns.Add(body.Instructions[i]);
                }
            }

            for (int i = 0; i < returns.Count; i++)
            {
                Instruction returnInstruction = returns[i];
                if (hasResult)
                {
                    returnInstruction.OpCode = OpCodes.Stloc;
                    returnInstruction.Operand = resultVariable;
                    il.InsertAfter(returnInstruction, il.Create(OpCodes.Leave, returnTarget));
                }
                else
                {
                    returnInstruction.OpCode = OpCodes.Leave;
                    returnInstruction.Operand = returnTarget;
                }
            }

            il.Append(finallyStart);
            il.Append(callDispose);
            il.Append(endFinally);
            il.Append(returnTarget);
            il.Append(finalReturn);

            body.ExceptionHandlers.Add(new ExceptionHandler(ExceptionHandlerType.Finally)
            {
                TryStart = originalFirst,
                TryEnd = finallyStart,
                HandlerStart = finallyStart,
                HandlerEnd = returnTarget
            });
        }

        /// <summary>
        /// 为只标记 Owner 特性的类型生成或增强 OnDestroy 取消入口。
        /// </summary>
        /// <param name="type">目标 Owner 类型。</param>
        /// <param name="module">目标 Cecil 模块。</param>
        /// <param name="disposeOwner">MTaskRuntime.DisposeOwner 方法。</param>
        private static void InjectOwnerDisposal(TypeDefinition type, ModuleDefinition module, MethodReference disposeOwner)
        {
            MethodDefinition onDestroy = null;
            for (int i = 0; i < type.Methods.Count; i++)
            {
                MethodDefinition candidate = type.Methods[i];
                if (!candidate.IsStatic
                    && string.Equals(candidate.Name, "OnDestroy", StringComparison.Ordinal)
                    && candidate.Parameters.Count == 0
                    && candidate.ReturnType.MetadataType == MetadataType.Void)
                {
                    onDestroy = candidate;
                    break;
                }
            }

            if (onDestroy == null)
            {
                onDestroy = new MethodDefinition(
                    "OnDestroy",
                    MethodAttributes.Private | MethodAttributes.HideBySig,
                    module.TypeSystem.Void);
                type.Methods.Add(onDestroy);
                ILProcessor createdIl = onDestroy.Body.GetILProcessor();
                createdIl.Append(createdIl.Create(OpCodes.Ldarg_0));
                createdIl.Append(createdIl.Create(OpCodes.Call, module.ImportReference(disposeOwner)));
                createdIl.Append(createdIl.Create(OpCodes.Ret));
                return;
            }

            if (!onDestroy.HasBody || onDestroy.Body.Instructions.Count == 0)
            {
                return;
            }

            for (int i = 0; i < onDestroy.Body.Instructions.Count; i++)
            {
                if (onDestroy.Body.Instructions[i].Operand is MethodReference called
                    && string.Equals(called.DeclaringType.FullName, RuntimeName, StringComparison.Ordinal)
                    && string.Equals(called.Name, "DisposeOwner", StringComparison.Ordinal))
                {
                    return;
                }
            }

            ILProcessor il = onDestroy.Body.GetILProcessor();
            Instruction first = onDestroy.Body.Instructions[0];
            Instruction loadOwner = il.Create(OpCodes.Ldarg_0);
            il.InsertBefore(first, loadOwner);
            il.InsertAfter(loadOwner, il.Create(OpCodes.Call, module.ImportReference(disposeOwner)));
        }

        /// <summary>
        /// 在目标模块及其引用中解析指定方法。
        /// </summary>
        /// <param name="module">目标模块。</param>
        /// <param name="typeName">声明类型完整名称。</param>
        /// <param name="methodName">方法名称。</param>
        /// <param name="parameterCount">参数数量。</param>
        /// <returns>找到的方法引用；不存在时返回 null。</returns>
        private static MethodReference FindMethod(ModuleDefinition module, string typeName, string methodName, int parameterCount)
        {
            TypeDefinition type = module.GetType(typeName);
            if (type == null)
            {
                for (int i = 0; i < module.AssemblyReferences.Count && type == null; i++)
                {
                    try
                    {
                        type = module.AssemblyResolver.Resolve(module.AssemblyReferences[i])?.MainModule.GetType(typeName);
                    }
                    catch
                    {
                    }
                }
            }

            if (type == null)
            {
                return null;
            }

            for (int i = 0; i < type.Methods.Count; i++)
            {
                MethodDefinition method = type.Methods[i];
                if (string.Equals(method.Name, methodName, StringComparison.Ordinal) && method.Parameters.Count == parameterCount)
                {
                    return module.ImportReference(method);
                }
            }

            return null;
        }

        /// <summary>
        /// 在目标模块及其引用中按单一参数类型解析指定方法。
        /// </summary>
        /// <param name="module">目标模块。</param>
        /// <param name="typeName">声明类型完整名称。</param>
        /// <param name="methodName">方法名称。</param>
        /// <param name="parameterTypeName">唯一参数的完整类型名称。</param>
        /// <returns>找到的方法引用；不存在时返回 null。</returns>
        private static MethodReference FindMethod(
            ModuleDefinition module,
            string typeName,
            string methodName,
            string parameterTypeName)
        {
            TypeDefinition type = ResolveType(module, typeName);
            if (type == null)
            {
                return null;
            }

            for (int i = 0; i < type.Methods.Count; i++)
            {
                MethodDefinition method = type.Methods[i];
                if (string.Equals(method.Name, methodName, StringComparison.Ordinal)
                    && method.Parameters.Count == 1
                    && string.Equals(method.Parameters[0].ParameterType.FullName, parameterTypeName, StringComparison.Ordinal))
                {
                    return module.ImportReference(method);
                }
            }

            return null;
        }

        /// <summary>
        /// 在目标模块及其引用程序集中解析类型。
        /// </summary>
        /// <param name="module">目标模块。</param>
        /// <param name="typeName">类型完整名称。</param>
        /// <returns>解析到的类型；不存在时返回 null。</returns>
        private static TypeDefinition ResolveType(ModuleDefinition module, string typeName)
        {
            TypeDefinition type = module.GetType(typeName);
            if (type != null)
            {
                return type;
            }

            for (int i = 0; i < module.AssemblyReferences.Count; i++)
            {
                try
                {
                    type = module.AssemblyResolver.Resolve(module.AssemblyReferences[i])?.MainModule.GetType(typeName);
                    if (type != null)
                    {
                        return type;
                    }
                }
                catch
                {
                }
            }

            return null;
        }

        #endregion
    }
}
