using System;
using System.Collections.Concurrent;

namespace MiniCore.Model
{
    public sealed class ByteBufferPool
    {
        public static ByteBufferPool Shared { get; } = new ByteBufferPool();

        private readonly ConcurrentDictionary<int, ConcurrentStack<byte[]>> pools = new ConcurrentDictionary<int, ConcurrentStack<byte[]>>();

        public byte[] Rent(int size)
        {
            int bucketSize = GetBucketSize(size);
            var pool = pools.GetOrAdd(bucketSize, _ => new ConcurrentStack<byte[]>());
            if (pool.TryPop(out var buffer))
            {
                return buffer;
            }
            return new byte[bucketSize];
        }

        public void Return(byte[] buffer)
        {
            if (buffer == null)
            {
                return;
            }

            var pool = pools.GetOrAdd(buffer.Length, _ => new ConcurrentStack<byte[]>());
            pool.Push(buffer);
        }

        private static int GetBucketSize(int size)
        {
            if (size <= 0)
            {
                return 256;
            }

            int bucket = 256;
            while (bucket < size && bucket > 0)
            {
                bucket <<= 1;
            }

            if (bucket <= 0)
            {
                return size;
            }

            return bucket;
        }
    }
}
