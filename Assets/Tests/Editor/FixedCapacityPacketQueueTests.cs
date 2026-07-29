using MiniCore.Model;
using NUnit.Framework;

namespace MiniCore.EditorTests
{
    /// <summary>
    /// 验证固定容量网络环形队列的顺序、预算与拒绝恢复行为。
    /// </summary>
    public sealed class FixedCapacityPacketQueueTests
    {
        #region Public 公共成员

        /// <summary>
        /// 验证队列按入队顺序返回数据，并在达到固定槽位数后拒绝新包。
        /// </summary>
        [Test]
        public void TryEnqueue_RespectsFixedPacketCapacityAndFifoOrder()
        {
            var queue = new FixedCapacityPacketQueue<int>(2, 128);

            Assert.IsTrue(queue.TryEnqueue(10, 16));
            Assert.IsTrue(queue.TryEnqueue(20, 16));
            Assert.IsFalse(queue.TryEnqueue(30, 16));

            Assert.IsTrue(queue.TryDequeue(out int first, out int firstLength));
            Assert.AreEqual(10, first);
            Assert.AreEqual(16, firstLength);
            Assert.IsTrue(queue.TryDequeue(out int second, out int secondLength));
            Assert.AreEqual(20, second);
            Assert.AreEqual(16, secondLength);
        }

        /// <summary>
        /// 验证字节预算拒绝不会占用槽位，已有数据出队后可以继续入队。
        /// </summary>
        [Test]
        public void TryEnqueue_RejectsByteBudgetAndRecoversAfterDequeue()
        {
            var queue = new FixedCapacityPacketQueue<int>(4, 32);

            Assert.IsTrue(queue.TryEnqueue(1, 24));
            Assert.IsFalse(queue.TryEnqueue(2, 16));
            Assert.IsTrue(queue.TryDequeue(out int packet, out int length));
            Assert.AreEqual(1, packet);
            Assert.AreEqual(24, length);
            Assert.IsTrue(queue.TryEnqueue(3, 16));
        }

        #endregion
    }
}
