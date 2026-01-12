using Cysharp.Threading.Tasks;
using System;

namespace MiniCore.Model
{
    /// <summary>
    /// Base class for normal message handlers (non-RPC).
    /// NetworkMessageComponent will reflect HandleAsync to dispatch messages.
    /// </summary>
    public abstract class AMHandler<TMessage> where TMessage : IProtocol
    {
        /// <summary>
        /// Resolve opcode from the registry for this message type.
        /// </summary>
        public virtual uint Opcode
        {
            get
            {
                var msgType = typeof(TMessage);
                if (OpcodeRegistry.TryGetOpcodeByMessage(msgType, out uint code))
                {
                    return code;
                }
                throw new InvalidOperationException($"Missing opcode mapping for {msgType.FullName}.");
            }
        }

        /// <summary>
        /// Handle deserialized message.
        /// </summary>
        public abstract UniTask HandleAsync(NetworkSession session, TMessage message);
    }
}
