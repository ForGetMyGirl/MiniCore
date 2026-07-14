using System.Collections.Concurrent;
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
        private readonly ConcurrentQueue<NetworkIncomingPacket> incomingPackets = new ConcurrentQueue<NetworkIncomingPacket>(); // 模拟网络组件内部的跨线程收包队列。
        private readonly byte[] sourcePacket = new byte[MediumPacketLength]; // 固定复用的传输层输入数据。
        private readonly byte[] copyDestinationPacket = new byte[MediumPacketLength]; // 仅测字节复制时复用的目标数组。
        private int processedByteChecksum; // 汇总出队数据以验证队列确实被完整消费。

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
            processedByteChecksum = 0;
        }

        /// <summary>
        /// 测量中等大小网络包从收包线程复制入队到主线程出队归还的总耗时与 GC 事件数。
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

            Assert.IsTrue(incomingPackets.IsEmpty, "性能测试结束后仍有未消费的数据包，结果无效。");
            Assert.Greater(processedByteChecksum, 0, "性能测试未实际处理数据包，结果无效。");
        }

        /// <summary>
        /// 测量缓冲池租用、复制与归还的成本，用于排除 ConcurrentQueue 对 GC 分配的影响。
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
        /// 测量 ConcurrentQueue 对收包结构体的入队和出队成本，用于定位其内部 GC 分配。
        /// </summary>
        [Test, Performance]
        public void ConcurrentQueue_TransfersMediumPackets_WithoutBufferCopy()
        {
            Measure.Method(TransferPacketsThroughQueueWithoutBufferCopy)
                .SampleGroup("Network.IncomingQueue.ConcurrentQueueOnly.MediumPacket")
                .WarmupCount(WarmupMeasurementCount)
                .MeasurementCount(ResultMeasurementCount)
                .IterationsPerMeasurement(MeasurementInvocationCount)
                .GC()
                .Run();

            Assert.IsTrue(incomingPackets.IsEmpty, "性能测试结束后仍有未消费的数据包，结果无效。");
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
        /// 模拟网络线程复制包体并入队，再模拟 Unity 主线程逐条出队和归还缓冲区。
        /// </summary>
        private void TransferIncomingPackets()
        {
            EnqueueIncomingPackets();
            ProcessIncomingPackets();
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
        /// 仅执行收包结构体的入队与出队，复用固定输入数组以排除缓冲区租用和复制成本。
        /// </summary>
        private void TransferPacketsThroughQueueWithoutBufferCopy()
        {
            for (int index = 0; index < PacketCountPerMeasurement; index++)
            {
                incomingPackets.Enqueue(new NetworkIncomingPacket
                {
                    Buffer = sourcePacket,
                    Length = MediumPacketLength
                });
            }

            while (incomingPackets.TryDequeue(out NetworkIncomingPacket packet))
            {
                processedByteChecksum += packet.Buffer[packet.Length - 1];
            }
        }

        /// <summary>
        /// 模拟网络回调线程将传输层数据复制到池化缓冲区并提交给主线程。
        /// </summary>
        private void EnqueueIncomingPackets()
        {
            for (int index = 0; index < PacketCountPerMeasurement; index++)
            {
                byte[] buffer = ByteBufferPool.Shared.Rent(MediumPacketLength);
                System.Buffer.BlockCopy(sourcePacket, 0, buffer, 0, MediumPacketLength);
                incomingPackets.Enqueue(new NetworkIncomingPacket
                {
                    Buffer = buffer,
                    Length = MediumPacketLength
                });
            }
        }

        /// <summary>
        /// 模拟主线程逐条消费网络包，并在消费后将缓冲区归还给共享池。
        /// </summary>
        private void ProcessIncomingPackets()
        {
            while (incomingPackets.TryDequeue(out NetworkIncomingPacket packet))
            {
                try
                {
                    processedByteChecksum += packet.Buffer[packet.Length - 1];
                }
                finally
                {
                    ByteBufferPool.Shared.Return(packet.Buffer);
                }
            }
        }

        /// <summary>
        /// 归还队列中遗留的缓冲区，避免测试之间持有池化数组。
        /// </summary>
        private void ReturnAllQueuedBuffers()
        {
            while (incomingPackets.TryDequeue(out NetworkIncomingPacket packet))
            {
                ByteBufferPool.Shared.Return(packet.Buffer);
            }
        }

        #endregion
    }
}
