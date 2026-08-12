using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace MiniCore.Threading
{

    /// <summary>
    /// 使用池化链表节点的多生产者单消费者续体队列。
    /// </summary>
    internal sealed class MTaskWorkQueue
    {
        #region Private 私有成员

        private readonly object gate = new object(); // 保护多线程入队和消费线程出队。
        private MTaskWorkItem head; // 当前队首节点。
        private MTaskWorkItem tail; // 当前队尾节点。

        #endregion

        #region Internal 内部成员

        /// <summary>
        /// 将续体放入队尾。
        /// </summary>
        /// <param name="continuation">待执行续体。</param>
        internal void Enqueue(Action continuation)
        {
            if (continuation == null)
            {
                throw new ArgumentNullException(nameof(continuation));
            }

            MTaskWorkItem item = MTaskWorkItem.Rent(continuation);
            lock (gate)
            {
                if (tail == null)
                {
                    head = item;
                    tail = item;
                }
                else
                {
                    tail.Next = item;
                    tail = item;
                }
            }
        }

        /// <summary>
        /// 尝试从队首取出一个续体。
        /// </summary>
        /// <param name="continuation">成功取出的续体。</param>
        /// <returns>队列非空时返回 true。</returns>
        internal bool TryDequeue(out Action continuation)
        {
            MTaskWorkItem item;
            lock (gate)
            {
                item = head;
                if (item == null)
                {
                    continuation = null;
                    return false;
                }

                head = item.Next;
                if (head == null)
                {
                    tail = null;
                }

                item.Next = null;
            }

            continuation = item.TakeContinuation();
            MTaskObjectPool<MTaskWorkItem>.Return(item);
            return true;
        }

        /// <summary>
        /// 丢弃当前队列中的全部续体并回收工作节点。
        /// </summary>
        internal void Clear()
        {
            while (TryDequeue(out _))
            {
            }
        }

        #endregion
    }
}
