using System;
using System.Collections.Generic;
using System.Threading;
using Google.Protobuf;
using MiniCore.Protocol.Generated;
using MiniCore.Threading;

namespace MiniCore.Demo.MiniBomber
{

    /// <summary>
    /// 使用短锁保护的固定容量环形队列。
    /// </summary>
    internal sealed class MiniBomberBoundedQueue<T>
    {
        #region Private 私有成员

        private readonly object gate = new object(); // 保护跨线程读写。
        private readonly T[] items; // 固定容量存储。
        private int head; // 下一读取下标。
        private int tail; // 下一写入下标。
        private int count; // 当前元素数量。

        #endregion

        #region Internal 内部成员

        /// <summary>
        /// 获取当前元素数量。
        /// </summary>
        internal int Count
        {
            get
            {
                lock (gate)
                {
                    return count;
                }
            }
        }

        /// <summary>
        /// 获取队列当前是否已满。
        /// </summary>
        internal bool IsFull
        {
            get
            {
                lock (gate)
                {
                    return count == items.Length;
                }
            }
        }

        /// <summary>
        /// 创建固定容量队列。
        /// </summary>
        /// <param name="capacity">大于零的容量。</param>
        internal MiniBomberBoundedQueue(int capacity)
        {
            items = new T[capacity > 0 ? capacity : throw new ArgumentOutOfRangeException(nameof(capacity))];
        }

        /// <summary>
        /// 尝试追加元素。
        /// </summary>
        /// <param name="item">待追加元素。</param>
        /// <returns>存在剩余容量时返回 true。</returns>
        internal bool TryEnqueue(T item)
        {
            lock (gate)
            {
                if (count == items.Length)
                {
                    return false;
                }

                items[tail] = item;
                tail = (tail + 1) % items.Length;
                count++;
                return true;
            }
        }

        /// <summary>
        /// 尝试取出队首元素。
        /// </summary>
        /// <param name="item">取出的元素。</param>
        /// <returns>队列非空时返回 true。</returns>
        internal bool TryDequeue(out T item)
        {
            lock (gate)
            {
                if (count == 0)
                {
                    item = default;
                    return false;
                }

                item = items[head];
                items[head] = default;
                head = (head + 1) % items.Length;
                count--;
                return true;
            }
        }

        /// <summary>
        /// 从最新元素开始替换第一个满足条件的元素。
        /// </summary>
        /// <param name="predicate">替换匹配条件。</param>
        /// <param name="replacement">新元素。</param>
        /// <returns>完成替换时返回 true。</returns>
        internal bool TryReplaceLatest(Predicate<T> predicate, T replacement)
        {
            lock (gate)
            {
                for (int offset = 1; offset <= count; offset++)
                {
                    int index = (tail - offset + items.Length) % items.Length;
                    if (predicate(items[index]))
                    {
                        items[index] = replacement;
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// 清空队列并释放元素引用。
        /// </summary>
        internal void Clear()
        {
            lock (gate)
            {
                Array.Clear(items, 0, items.Length);
                head = 0;
                tail = 0;
                count = 0;
            }
        }

        #endregion
    }
}
