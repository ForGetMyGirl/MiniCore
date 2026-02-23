using System.Threading.Tasks;
using System.Threading;
using BufferOwner = System.Buffers.IMemoryOwner<byte>;
using System.Buffers;

namespace System.Net.Sockets.Kcp
{
    
    
    
    public interface IKcpCallback
    {
        
        
        
        
        
        
        
        void Output(BufferOwner buffer, int avalidLength);
    }

    
    
    
    
    
    
    
    public interface IKcpOutputWriter : IBufferWriter<byte>
    {
        int UnflushedBytes { get; }
        void Flush();
    }

    
    
    
    public interface IRentable
    {
        
        
        
        BufferOwner RentBuffer(int length);
    }

    public interface IKcpSetting
    {
        int Interval(int interval);
        
        
        
        
        
        
        
        
        int NoDelay(int nodelay, int interval, int resend, int nc);
        
        
        
        
        
        
        
        
        
        int SetMtu(int mtu = 1400);
        
        
        
        
        
        
        
        
        
        int WndSize(int sndwnd = 32, int rcvwnd = 128);
    }

    public interface IKcpUpdate
    {
        void Update(in DateTimeOffset time);
    }

    public interface IKcpSendable
    {
        
        
        
        
        
        int Send(ReadOnlySpan<byte> span, object options = null);
        
        
        
        
        
        int Send(ReadOnlySequence<byte> span, object options = null);
    }

    public interface IKcpInputable
    {
        
        
        
        
        int Input(ReadOnlySpan<byte> span);
        
        
        
        
        int Input(ReadOnlySequence<byte> span);
    }

    
    
    
    public interface IKcpIO : IKcpSendable, IKcpInputable
    {
        
        
        
        
        ValueTask RecvAsync(IBufferWriter<byte> writer, object options = null);

        
        
        
        
        
        
        ValueTask<int> RecvAsync(ArraySegment<byte> buffer, object options = null);

        
        
        
        
        
        
        ValueTask OutputAsync(IBufferWriter<byte> writer, object options = null);
    }

}




