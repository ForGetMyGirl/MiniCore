using System;
using System.Collections.Generic;
using MiniCore.Eventing;
using MiniCore.Threading;
using MiniCore.Unity;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace MiniCore.UI
{

    /// <summary>
    /// Presenter 在一次窗口会话中可访问的最小上下文。
    /// </summary>
    public sealed class UIWindowContext
    {
        #region Private 私有成员

        private readonly Action<object> submitResult; // 向当前会话提交强类型结果的入口。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 获取当前窗口句柄。
        /// </summary>
        public UIWindowHandle Handle { get; }

        /// <summary>
        /// 获取本次打开参数；无参数窗口返回 null。
        /// </summary>
        public object Arguments { get; }

        /// <summary>
        /// 获取当前窗口统一解绑集合。
        /// </summary>
        public UIBindingSet Bindings { get; }

        /// <summary>
        /// 获取当前会话拥有的任务域。
        /// </summary>
        public MTaskDomain Domain { get; }

        /// <summary>
        /// 获取窗口服务接口，用于关闭或聚焦当前窗口。
        /// </summary>
        public IUIService Service { get; }

        /// <summary>
        /// 创建只包含窗口生命周期能力的上下文。
        /// </summary>
        /// <param name="handle">当前窗口句柄。</param>
        /// <param name="arguments">本次打开参数。</param>
        /// <param name="bindings">统一解绑集合。</param>
        /// <param name="domain">窗口任务域。</param>
        /// <param name="service">窗口服务。</param>
        /// <param name="resultWriter">可选的窗口结果提交入口。</param>
        public UIWindowContext(UIWindowHandle handle, object arguments, UIBindingSet bindings, MTaskDomain domain, IUIService service, Action<object> resultWriter = null)
        {
            Handle = handle ?? throw new ArgumentNullException(nameof(handle));
            Arguments = arguments;
            Bindings = bindings ?? throw new ArgumentNullException(nameof(bindings));
            Domain = domain ?? throw new ArgumentNullException(nameof(domain));
            Service = service ?? throw new ArgumentNullException(nameof(service));
            submitResult = resultWriter;
        }

        /// <summary>
        /// 获取并验证强类型打开参数。
        /// </summary>
        /// <typeparam name="TArgs">期望参数类型。</typeparam>
        /// <returns>匹配的打开参数。</returns>
        public TArgs GetArguments<TArgs>()
        {
            if (Arguments is TArgs value)
            {
                return value;
            }

            throw new InvalidOperationException($"窗口参数类型不匹配，期望 {typeof(TArgs).FullName}，实际 {Arguments?.GetType().FullName ?? "<null>"}。");
        }

        /// <summary>
        /// 向 ShowAsync 调用方提交一次窗口结果。
        /// </summary>
        /// <typeparam name="TResult">窗口结果类型。</typeparam>
        /// <param name="result">要提交的业务结果。</param>
        public void SubmitResult<TResult>(TResult result)
        {
            if (submitResult == null)
            {
                throw new InvalidOperationException("当前窗口不是通过 ShowAsync 打开，不能提交结果。");
            }

            submitResult(result);
        }

        #endregion
    }
}
