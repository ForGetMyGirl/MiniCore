using System.Threading.Tasks;
using System.Threading;
using BufferOwner = System.Buffers.IMemoryOwner<byte>;
using System.Buffers;

namespace System.Net.Sockets.Kcp
{
    
    
    
    /// <summary>
    /// 接收 KCP 输出分片的回调接口。
    /// </summary>
    public interface IKcpCallback
    {
        
        
        
        
        
        
        
        void Output(BufferOwner buffer, int avalidLength);
    }

    
    
    
    
    
    
    
    /// <summary>
    /// 支持查询未刷新字节数和主动刷新的 KCP 输出写入器。
    /// </summary>
    public interface IKcpOutputWriter : IBufferWriter<byte>
    {
        int UnflushedBytes { get; }
        void Flush();
    }

    
    
    
    /// <summary>
    /// 为 KCP Core 提供可回收字节缓冲区的接口。
    /// </summary>
    public interface IRentable
    {
        
        
        
        BufferOwner RentBuffer(int length);
    }

    /// <summary>
    /// KCP 传输参数配置接口。
    /// </summary>
    public interface IKcpSetting
    {
        int Interval(int interval);
        
        
        
        
        
        
        
        
        int NoDelay(int nodelay, int interval, int resend, int nc);
        
        
        
        
        
        
        
        
        
        int SetMtu(int mtu = 1400);
        
        
        
        
        
        
        
        
        
        int WndSize(int sndwnd = 32, int rcvwnd = 128);
    }

    /// <summary>
    /// KCP 定时更新接口。
    /// </summary>
    public interface IKcpUpdate
    {
        void Update(in DateTimeOffset time);
    }

    /// <summary>
    /// KCP 发送数据接口。
    /// </summary>
    public interface IKcpSendable
    {
        
        
        
        
        
        int Send(ReadOnlySpan<byte> span, object options = null);
        
        
        
        
        
        int Send(ReadOnlySequence<byte> span, object options = null);
    }

    /// <summary>
    /// KCP 输入数据报接口。
    /// </summary>
    public interface IKcpInputable
    {
        
        
        
        
        int Input(ReadOnlySpan<byte> span);
        
        
        
        
        int Input(ReadOnlySequence<byte> span);
    }

    
    
    
    /// <summary>
    /// 组合 KCP 收发能力的异步 I/O 接口。
    /// </summary>
    public interface IKcpIO : IKcpSendable, IKcpInputable
    {
        
        
        
        
        ValueTask RecvAsync(IBufferWriter<byte> writer, object options = null);

        
        
        
        
        
        
        ValueTask<int> RecvAsync(ArraySegment<byte> buffer, object options = null);

        
        
        
        
        
        
        ValueTask OutputAsync(IBufferWriter<byte> writer, object options = null);
    }

}



