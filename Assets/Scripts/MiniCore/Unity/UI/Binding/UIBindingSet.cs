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
    /// 保存一次窗口打开周期中的 UI 控件与事件解绑操作。
    /// </summary>
    public sealed class UIBindingSet : IDisposable
    {
        #region Private 私有成员

        private readonly List<Action> removers = new List<Action>(8); // 关闭窗口时顺序执行的解绑动作。
        private readonly List<EventSubscription> subscriptions = new List<EventSubscription>(4); // 强类型事件订阅。
        private bool disposed; // 当前绑定集合是否已经释放。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 登记 Button 点击监听并在释放时自动移除。
        /// </summary>
        /// <param name="button">目标按钮。</param>
        /// <param name="listener">命名点击监听器。</param>
        public void Add(Button button, UnityAction listener)
        {
            ThrowIfDisposed();
            if (button == null || listener == null)
            {
                return;
            }

            button.onClick.AddListener(listener);
            removers.Add(() => button.onClick.RemoveListener(listener));
        }

        /// <summary>
        /// 登记 Toggle 值变化监听并在释放时自动移除。
        /// </summary>
        /// <param name="toggle">目标开关。</param>
        /// <param name="listener">命名值变化监听器。</param>
        public void Add(Toggle toggle, UnityAction<bool> listener)
        {
            ThrowIfDisposed();
            if (toggle == null || listener == null)
            {
                return;
            }

            toggle.onValueChanged.AddListener(listener);
            removers.Add(() => toggle.onValueChanged.RemoveListener(listener));
        }

        /// <summary>
        /// 登记 TMP 输入框值变化监听并在释放时自动移除。
        /// </summary>
        /// <param name="input">目标输入框。</param>
        /// <param name="listener">命名值变化监听器。</param>
        public void Add(TMP_InputField input, UnityAction<string> listener)
        {
            ThrowIfDisposed();
            if (input == null || listener == null)
            {
                return;
            }

            input.onValueChanged.AddListener(listener);
            removers.Add(() => input.onValueChanged.RemoveListener(listener));
        }

        /// <summary>
        /// 登记强类型事件订阅并在释放时自动解除。
        /// </summary>
        /// <param name="subscription">事件订阅 token。</param>
        public void Add(EventSubscription subscription)
        {
            ThrowIfDisposed();
            subscriptions.Add(subscription);
        }

        /// <summary>
        /// 登记自定义解绑动作。
        /// </summary>
        /// <param name="remove">关闭窗口时执行的无异常解绑动作。</param>
        public void Add(Action remove)
        {
            ThrowIfDisposed();
            if (remove != null)
            {
                removers.Add(remove);
            }
        }

        /// <summary>
        /// 解除全部 UI 与强类型事件绑定。
        /// </summary>
        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            for (int i = removers.Count - 1; i >= 0; i--)
            {
                removers[i]?.Invoke();
            }

            for (int i = subscriptions.Count - 1; i >= 0; i--)
            {
                subscriptions[i].Dispose();
            }

            removers.Clear();
            subscriptions.Clear();
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 阻止已释放绑定集合继续登记监听。
        /// </summary>
        private void ThrowIfDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(UIBindingSet));
            }
        }

        #endregion
    }
}
