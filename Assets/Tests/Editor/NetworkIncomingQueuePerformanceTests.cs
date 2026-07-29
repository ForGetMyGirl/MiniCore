using System;
using System.Threading;
using MiniCore.Core;
using MiniCore.Model;
using NUnit.Framework;
using Unity.PerformanceTesting;

namespace MiniCore.EditorTests
{
    /// <summary>
    /// 网络收包线程向主线程交接数据时的队列与缓冲池性能基线测试。
    /// 测试范围包含数据复制、入队、出队和缓冲区归还，不包含 JSON 反序列化、协议解析及业务 Handler。
    /// </summary>
    public sealed class NetworkIncomingQueuePerformanceTests
    {
        #region Private 私有成员

        private const int WarmupMeasurementCount = 5; // 不计入最终报告的预热组数。
        private const int ResultMeasurementCount = 20; // 计入最终报告的测量组数。
        private const int MeasurementInvocationCount = 1; // 单次测量调用中已完整处理一组业务包，避免与内部循环重复叠加。
        private const int PacketCountPerMeasurement = 10000; // 每组模拟从网络线程交接到主线程的业务包数量。
        private const int MediumPacketLength = 512; // 固定的中等业务包长度。
        private const int QueueMaximumPacketCount = 1024; // 与正式普通收包队列一致的单队列包数预算。
        private const int QueueMaximumByteCount = 1024 * 1024; // 与正式普通收包队列一致的单队列字节预算。
        private readonly FixedCapacityPacketQueue<NetworkIncomingPacket> incomingPackets = new FixedCapacityPacketQueue<NetworkIncomingPacket>(QueueMaximumPacketCount, QueueMaximumByteCount); // 模拟正式收包路径的预分配固定队列。
        private readonly byte[] sourcePacket = new byte[MediumPacketLength]; // 固定复用的传输层输入数据。
        private readonly byte[] copyDestinationPacket = new byte[MediumPacketLength]; // 仅测字节复制时复用的目标数组。
        private readonly ManualResetEventSlim producerStartSignal = new ManualResetEventSlim(false); // 唤醒后台网络生产线程开始下一组生产。
        private readonly ManualResetEventSlim producerCompletedSignal = new ManualResetEventSlim(false); // 标识后台网络生产线程已完成当前测量组。
        private int processedByteChecksum; // 汇总出队数据以验证队列确实被完整消费。
        private bool producerUsesPooledBuffers; // 当前测量组是否模拟正式路径的租用与复制。
        private volatile bool producerTerminationRequested; // 要求后台生产线程结束。
        private Exception producerFailure; // 后台生产线程遇到的异常，由测试线程重新抛出。
        private Thread producerThread; // 长驻的模拟网络收包线程，避免把线程创建计入测量。

        #endregion

        #region Public 公共成员

        /// <summary>
        /// 在每个测试开始前填充固定输入，并清理上一次未消费的测试包。
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            for (int index = 0; index < sourcePacket.Length; index++)
            {
                sourcePacket[index] = (byte)(index % byte.MaxValue);
            }

            ReturnAllQueuedBuffers();
            EnsureProducerThread();
            processedByteChecksum = 0;
        }

        /// <summary>
        /// 在测试结束后停止后台网络生产线程，并归还未被消费的池化缓冲区。
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            StopProducerThread();
            ReturnAllQueuedBuffers();
        }

        /// <summary>
        /// 测量中等大小网络包由后台收包线程复制入固定队列、主线程并发出队归还的总耗时与 GC 事件数。
        /// </summary>
        [Test, Performance]
        public void IncomingQueue_TransfersMediumPackets_BetweenNetworkAndMainThread()
        {
            Measure.Method(TransferIncomingPackets)
                .SampleGroup("Network.IncomingQueue.MediumPacket")
                .WarmupCount(WarmupMeasurementCount)
                .MeasurementCount(ResultMeasurementCount)
                .IterationsPerMeasurement(MeasurementInvocationCount)
                .GC()
                .Run();

            Assert.IsTrue(IsIncomingQueueEmpty(), "性能测试结束后仍有未消费的数据包，结果无效。");
            Assert.Greater(processedByteChecksum, 0, "性能测试未实际处理数据包，结果无效。");
        }

        /// <summary>
        /// 测量缓冲池租用、复制与归还的成本，用于排除固定队列对 GC 分配的影响。
        /// </summary>
        [Test, Performance]
        public void ByteBufferPool_CopiesMediumPackets_WithoutQueue()
        {
            Measure.Method(CopyPacketsWithoutQueue)
                .SampleGroup("Network.IncomingQueue.BufferCopyOnly.MediumPacket")
                .WarmupCount(WarmupMeasurementCount)
                .MeasurementCount(ResultMeasurementCount)
                .IterationsPerMeasurement(MeasurementInvocationCount)
                .GC()
                .Run();

            Assert.Greater(processedByteChecksum, 0, "性能测试未实际复制数据包，结果无效。");
        }

        /// <summary>
        /// 测量固定容量队列由后台生产线程与主线程并发交接收包结构体的成本。
        /// </summary>
        [Test, Performance]
        public void FixedCapacityQueue_TransfersMediumPackets_ConcurrentlyWithoutBufferCopy()
        {
            Measure.Method(TransferPacketsThroughQueueWithoutBufferCopy)
                .SampleGroup("Network.IncomingQueue.FixedCapacityQueueOnly.MediumPacket")
                .WarmupCount(WarmupMeasurementCount)
                .MeasurementCount(ResultMeasurementCount)
                .IterationsPerMeasurement(MeasurementInvocationCount)
                .GC()
                .Run();

            Assert.IsTrue(IsIncomingQueueEmpty(), "性能测试结束后仍有未消费的数据包，结果无效。");
            Assert.Greater(processedByteChecksum, 0, "性能测试未实际交接数据包，结果无效。");
        }

        /// <summary>
        /// 测量固定数组之间的纯字节复制成本，用于排除缓冲池容器操作造成的 GC 分配。
        /// </summary>
        [Test, Performance]
        public void BufferBlockCopy_CopiesMediumPackets_WithoutPool()
        {
            Measure.Method(CopyPacketsWithoutPool)
                .SampleGroup("Network.IncomingQueue.BlockCopyOnly.MediumPacket")
                .WarmupCount(WarmupMeasurementCount)
                .MeasurementCount(ResultMeasurementCount)
                .IterationsPerMeasurement(MeasurementInvocationCount)
                .GC()
                .Run();

            Assert.Greater(processedByteChecksum, 0, "性能测试未实际执行字节复制，结果无效。");
        }

        /// <summary>
        /// 测量缓冲池的租用和归还成本，用于定位池容器自身造成的 GC 分配。
        /// </summary>
        [Test, Performance]
        public void ByteBufferPool_RentsAndReturnsMediumPackets_WithoutCopy()
        {
            Measure.Method(RentAndReturnPacketsWithoutCopy)
                .SampleGroup("Network.IncomingQueue.BufferPoolOnly.MediumPacket")
                .WarmupCount(WarmupMeasurementCount)
                .MeasurementCount(ResultMeasurementCount)
                .IterationsPerMeasurement(MeasurementInvocationCount)
                .GC()
                .Run();

            Assert.Greater(processedByteChecksum, 0, "性能测试未实际执行缓冲区租用和归还，结果无效。");
        }

        #endregion

        #region Private 私有成员

        /// <summary>
        /// 模拟网络线程复制包体并入队，同时模拟 Unity 主线程逐条出队和归还缓冲区。
        /// </summary>
        private void TransferIncomingPackets()
        {
            TransferPacketsConcurrently(true);
        }

        /// <summary>
        /// 仅在两个固定数组之间复制数据，不经过缓冲池和跨线程队列。
        /// </summary>
        private void CopyPacketsWithoutPool()
        {
            for (int index = 0; index < PacketCountPerMeasurement; index++)
            {
                System.Buffer.BlockCopy(sourcePacket, 0, copyDestinationPacket, 0, MediumPacketLength);
                processedByteChecksum += copyDestinationPacket[MediumPacketLength - 1];
            }
        }

        /// <summary>
        /// 仅从缓冲池租用并归还数组，不执行字节复制和跨线程队列操作。
        /// </summary>
        private void RentAndReturnPacketsWithoutCopy()
        {
            for (int index = 0; index < PacketCountPerMeasurement; index++)
            {
                byte[] buffer = ByteBufferPool.Shared.Rent(MediumPacketLength);
                try
                {
                    processedByteChecksum += buffer.Length;
                }
                finally
                {
                    ByteBufferPool.Shared.Return(buffer);
                }
            }
        }

        /// <summary>
        /// 仅执行缓冲池租用、数据复制与归还，不经过跨线程队列。
        /// </summary>
        private void CopyPacketsWithoutQueue()
        {
            for (int index = 0; index < PacketCountPerMeasurement; index++)
            {
                byte[] buffer = ByteBufferPool.Shared.Rent(MediumPacketLength);
                try
                {
                    System.Buffer.BlockCopy(sourcePacket, 0, buffer, 0, MediumPacketLength);
                    processedByteChecksum += buffer[MediumPacketLength - 1];
                }
                finally
                {
                    ByteBufferPool.Shared.Return(buffer);
                }
            }
        }

        /// <summary>
        /// 仅执行收包结构体的并发入队与出队，复用固定输入数组以排除缓冲区租用和复制成本。
        /// </summary>
        private void TransferPacketsThroughQueueWithoutBufferCopy()
        {
            TransferPacketsConcurrently(false);
        }

        /// <summary>
        /// 由后台生产线程和当前主线程同时执行固定队列交接，完整模拟收包与消费并发关系。
        /// </summary>
        /// <param name="usePooledBuffers">是否模拟正式路径的缓冲池租用与字节复制。</param>
        private void TransferPacketsConcurrently(bool usePooledBuffers)
        {
            producerUsesPooledBuffers = usePooledBuffers;
            producerFailure = null;
            producerCompletedSignal.Reset();
            producerStartSignal.Set();

            int processedPacketCount = 0;
            while (processedPacketCount < PacketCountPerMeasurement)
            {
                if (incomingPackets.TryDequeue(out NetworkIncomingPacket packet, out _))
                {
                    ConsumeIncomingPacket(packet, usePooledBuffers);
                    processedPacketCount++;
                    continue;
                }

                ThrowIfProducerFailed();
                Thread.SpinWait(1);
            }

            producerCompletedSignal.Wait();
            ThrowIfProducerFailed();
        }

        /// <summary>
        /// 由后台线程等待测量请求并连续模拟网络收包生产。
        /// </summary>
        private void ProducerLoop()
        {
            while (!producerTerminationRequested)
            {
                producerStartSignal.Wait();
                producerStartSignal.Reset();
                if (producerTerminationRequested)
                {
                    return;
                }

                try
                {
                    ProduceIncomingPackets(producerUsesPooledBuffers);
                }
                catch (Exception exception)
                {
                    producerFailure = exception;
                }
                finally
                {
                    producerCompletedSignal.Set();
                }
            }
        }

        /// <summary>
        /// 模拟网络回调线程将固定输入复制到池化缓冲区或复用固定输入，并提交到正式同预算的固定队列。
        /// </summary>
        /// <param name="usePooledBuffers">是否租用池化缓冲区并执行字节复制。</param>
        private void ProduceIncomingPackets(bool usePooledBuffers)
        {
            int producedPacketCount = 0;
            while (producedPacketCount < PacketCountPerMeasurement && !producerTerminationRequested)
            {
                if (!incomingPackets.CanAccept(MediumPacketLength))
                {
                    Thread.SpinWait(1);
                    continue;
                }

                byte[] buffer = usePooledBuffers ? ByteBufferPool.Shared.Rent(MediumPacketLength) : sourcePacket;
                if (usePooledBuffers)
                {
                    System.Buffer.BlockCopy(sourcePacket, 0, buffer, 0, MediumPacketLength);
                }

                var packet = new NetworkIncomingPacket
                {
                    Buffer = buffer,
                    Length = MediumPacketLength
                };
                if (incomingPackets.TryEnqueue(packet, MediumPacketLength))
                {
                    producedPacketCount++;
                    continue;
                }

                if (usePooledBuffers)
                {
                    ByteBufferPool.Shared.Return(buffer);
                }
            }
        }

        /// <summary>
        /// 消费一个已从固定队列取出的数据包，并在需要时归还其池化缓冲区。
        /// </summary>
        /// <param name="packet">主线程即将处理的数据包。</param>
        /// <param name="usesPooledBuffers">该数据包是否持有需要归还的池化缓冲区。</param>
        private void ConsumeIncomingPacket(NetworkIncomingPacket packet, bool usesPooledBuffers)
        {
            try
            {
                processedByteChecksum += packet.Buffer[packet.Length - 1];
            }
            finally
            {
                if (usesPooledBuffers)
                {
                    ByteBufferPool.Shared.Return(packet.Buffer);
                }
            }
        }

        /// <summary>
        /// 启动一次测试复用的后台网络生产线程，避免把线程创建成本计入性能样本。
        /// </summary>
        private void EnsureProducerThread()
        {
            if (producerThread != null)
            {
                return;
            }

            producerTerminationRequested = false;
            producerThread = new Thread(ProducerLoop)
            {
                IsBackground = true,
                Name = "MiniCore.NetworkIncomingQueuePerformanceProducer"
            };
            producerThread.Start();
        }

        /// <summary>
        /// 请求后台网络生产线程退出，并等待其结束，避免线程遗留到其他 Editor 测试。
        /// </summary>
        private void StopProducerThread()
        {
            if (producerThread == null)
            {
                return;
            }

            producerTerminationRequested = true;
            producerStartSignal.Set();
            producerThread.Join();
            producerThread = null;
        }

        /// <summary>
        /// 若后台网络生产线程失败，则在测试线程中重新抛出原始异常。
        /// </summary>
        private void ThrowIfProducerFailed()
        {
            if (producerFailure != null)
            {
                throw new InvalidOperationException("固定队列后台生产线程失败。", producerFailure);
            }
        }

        /// <summary>
        /// 判断固定收包队列是否已被完全消费。
        /// </summary>
        /// <returns>没有待消费数据包时返回 true。</returns>
        private bool IsIncomingQueueEmpty()
        {
            incomingPackets.CaptureSnapshot(out long packetCount, out _, out _, out _, out _);
            return packetCount == 0;
        }

        /// <summary>
        /// 归还队列中遗留的池化缓冲区，避免测试之间持有池化数组。
        /// </summary>
        private void ReturnAllQueuedBuffers()
        {
            while (incomingPackets.TryDequeue(out NetworkIncomingPacket packet, out _))
            {
                if (packet.Buffer != null && !ReferenceEquals(packet.Buffer, sourcePacket))
                {
                    ByteBufferPool.Shared.Return(packet.Buffer);
                }
            }
        }

        #endregion
    }
}
