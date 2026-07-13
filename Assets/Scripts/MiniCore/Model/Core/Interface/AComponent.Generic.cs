using System;

namespace MiniCore.Model
{
    /// <summary>
    /// 带强类型初始化参数的组件基类。
    /// 由基类统一校验参数类型，具体组件只处理自身所需的参数对象。
    /// </summary>
    /// <typeparam name="TArgs">组件所需的初始化参数类型。</typeparam>
    public abstract class AComponent<TArgs> : AComponent where TArgs : ComponentInitArgs
    {
        #region Override 重写实现

        /// <summary>
        /// 校验通用初始化参数并转交给强类型初始化方法。
        /// </summary>
        /// <param name="args">调用方传入的组件初始化参数。</param>
        public sealed override void Awake(ComponentInitArgs args)
        {
            ThrowIfDisposed();
            TArgs typedArgs = args as TArgs;
            if (typedArgs == null)
            {
                throw new ArgumentException($"组件 {GetType().FullName} 需要 {typeof(TArgs).FullName} 类型的初始化参数。", nameof(args));
            }

            Awake(typedArgs);
        }

        #endregion

        #region Protected 受保护成员

        /// <summary>
        /// 使用强类型参数初始化组件。
        /// </summary>
        /// <param name="args">已通过类型校验的组件初始化参数。</param>
        protected abstract void Awake(TArgs args);

        #endregion
    }
}
