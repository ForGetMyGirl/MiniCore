using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MiniCore.Eventing.Diagnostics
{
    /// <summary>
    /// 为 MiniCore 事件订阅提供 lambda、丢弃 token 和双派发标记诊断。
    /// 该分析器只参与 Editor 编译，不进入 Player。
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class EventSubscriptionAnalyzer : DiagnosticAnalyzer
    {
        #region Private 私有成员

        private static readonly DiagnosticDescriptor LambdaRule = new DiagnosticDescriptor(
            "MCEVT001",
            "事件订阅不应直接使用 lambda",
            "事件订阅应使用命名方法或 IEventHandler/IAsyncEventHandler，避免闭包分配和难以解除订阅的匿名委托",
            "MiniCore.Eventing",
            DiagnosticSeverity.Warning,
            true);
        private static readonly DiagnosticDescriptor DiscardedTokenRule = new DiagnosticDescriptor(
            "MCEVT002",
            "事件订阅 token 被丢弃",
            "Subscribe/SubscribeAsync 的返回 EventSubscription 必须保存并在生命周期结束时 Dispose",
            "MiniCore.Eventing",
            DiagnosticSeverity.Warning,
            true);
        private static readonly DiagnosticDescriptor DualMarkerRule = new DiagnosticDescriptor(
            "MCEVT003",
            "事件不能同时声明同步和异步派发",
            "事件类型 {0} 同时实现 ISyncEvent 和 IAsyncEvent，请只保留一种派发语义",
            "MiniCore.Eventing",
            DiagnosticSeverity.Warning,
            true);

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 获取当前分析器支持的全部诊断描述。
        /// </summary>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(LambdaRule, DiscardedTokenRule, DualMarkerRule);

        /// <summary>
        /// 注册事件订阅调用和类型声明检查。
        /// </summary>
        /// <param name="context">Roslyn 分析器初始化上下文。</param>
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeInvocation, Microsoft.CodeAnalysis.CSharp.SyntaxKind.InvocationExpression);
            context.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 检查 Subscribe 或 SubscribeAsync 调用中的匿名函数及 token 丢弃。
        /// </summary>
        /// <param name="context">当前调用表达式分析上下文。</param>
        private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
        {
            InvocationExpressionSyntax invocation = (InvocationExpressionSyntax)context.Node;
            IMethodSymbol method = context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol as IMethodSymbol;
            if (method == null || (method.Name != "Subscribe" && method.Name != "SubscribeAsync") || !ImplementsEventBus(method.ContainingType))
            {
                return;
            }

            SeparatedSyntaxList<ArgumentSyntax> arguments = invocation.ArgumentList.Arguments;
            for (int index = 0; index < arguments.Count; index++)
            {
                if (arguments[index].Expression is AnonymousFunctionExpressionSyntax)
                {
                    context.ReportDiagnostic(Diagnostic.Create(LambdaRule, arguments[index].GetLocation()));
                    break;
                }
            }

            if (invocation.Parent is ExpressionStatementSyntax)
            {
                context.ReportDiagnostic(Diagnostic.Create(DiscardedTokenRule, invocation.GetLocation()));
            }
        }

        /// <summary>
        /// 检查事件类型是否同时实现同步和异步标记接口。
        /// </summary>
        /// <param name="context">当前命名类型分析上下文。</param>
        private static void AnalyzeNamedType(SymbolAnalysisContext context)
        {
            INamedTypeSymbol type = (INamedTypeSymbol)context.Symbol;
            bool hasSync = false;
            bool hasAsync = false;
            ImmutableArray<INamedTypeSymbol> interfaces = type.AllInterfaces;
            for (int index = 0; index < interfaces.Length; index++)
            {
                string displayName = interfaces[index].ToDisplayString();
                hasSync |= displayName == "MiniCore.Eventing.ISyncEvent";
                hasAsync |= displayName == "MiniCore.Eventing.IAsyncEvent";
            }

            if (hasSync && hasAsync)
            {
                context.ReportDiagnostic(Diagnostic.Create(DualMarkerRule, type.Locations[0], type.Name));
            }
        }

        /// <summary>
        /// 判断方法所属类型是否实现 MiniCore 的事件总线契约。
        /// </summary>
        /// <param name="type">待检查的方法所属类型。</param>
        /// <returns>实现 IEventBus 或其派生接口时返回 true。</returns>
        private static bool ImplementsEventBus(INamedTypeSymbol type)
        {
            if (type == null)
            {
                return false;
            }

            if (type.ToDisplayString() == "MiniCore.Eventing.IEventBus")
            {
                return true;
            }

            ImmutableArray<INamedTypeSymbol> interfaces = type.AllInterfaces;
            for (int index = 0; index < interfaces.Length; index++)
            {
                if (interfaces[index].ToDisplayString() == "MiniCore.Eventing.IEventBus")
                {
                    return true;
                }
            }

            return false;
        }

        #endregion
    }
}
