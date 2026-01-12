using Cysharp.Threading.Tasks;
using System;

namespace MiniCore.Model
{
    /// <summary>
    /// Base class for client-side RPC handlers (handles incoming RPC requests).
    /// </summary>
    public abstract class ARpcHandler<TRequest, TResponse>
        where TRequest : IRequest
        where TResponse : IResponse
    {
        public virtual uint Opcode
        {
            get
            {
                var msgType = typeof(TRequest);
                if (OpcodeRegistry.TryGetOpcodeByMessage(msgType, out uint code))
                {
                    return code;
                }
                throw new InvalidOperationException($"Missing opcode mapping for {msgType.FullName}.");
            }
        }

        public abstract UniTask HandleAsync(NetworkSession session, TRequest request, TResponse response);
    }
}

