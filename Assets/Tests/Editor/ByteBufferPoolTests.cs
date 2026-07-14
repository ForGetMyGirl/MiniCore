using System;
using System.Threading.Tasks;
using MiniCore.Model;
using NUnit.Framework;

namespace MiniCore.EditorTests
{
    /// <summary>
    /// 字节数组缓冲池的复用与多线程安全测试。
    /// </summary>
    public sealed class ByteBufferPoolTests
    {
        #region Private 私有成员

        private const int BufferLength = 512; // 测试使用的固定缓冲区长度。
        private const int WorkerCount = 4; // 并发租用与归还的工作线程数量。
        private const int RentCountPerWorker = 5000; // 每个工作线程连续执行的租用与归还次数。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 验证同一大小的数组归还后能够被后续租用操作复用。
        /// </summary>
        [Test]
        public void RentAndReturn_ReusesReturnedBuffer()
        {
            ByteBufferPool pool = new ByteBufferPool();
            byte[] firstBuffer = pool.Rent(BufferLength);
            pool.Return(firstBuffer);
            byte[] secondBuffer = pool.Rent(BufferLength);

            Assert.AreSame(firstBuffer, secondBuffer, "同一大小的已归还数组应被缓冲池优先复用。");
        }

        /// <summary>
        /// 验证多个线程同时租用与归还同一大小数组时不会返回错误容量或抛出并发异常。
        /// </summary>
        [Test]
        public void RentAndReturn_IsThreadSafeForConcurrentCallers()
        {
            ByteBufferPool pool = new ByteBufferPool();

            Parallel.For(0, WorkerCount, workerIndex => RentAndReturnRepeatedly(pool, workerIndex));
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 在单个工作线程中重复验证租用数组的容量，并确保每次租用都会被归还。
        /// </summary>
        /// <param name="pool">由多个工作线程共享的待测缓冲池。</param>
        /// <param name="workerIndex">当前并发工作线程的标识，仅用于生成不同写入值。</param>
        private static void RentAndReturnRepeatedly(ByteBufferPool pool, int workerIndex)
        {
            for (int index = 0; index < RentCountPerWorker; index++)
            {
                byte[] buffer = pool.Rent(BufferLength);
                if (buffer == null || buffer.Length < BufferLength)
                {
                    throw new InvalidOperationException("缓冲池在并发租用时返回了无效数组。");
                }

                try
                {
                    buffer[0] = (byte)workerIndex;
                }
                finally
                {
                    pool.Return(buffer);
                }
            }
        }

        #endregion
    }
}
